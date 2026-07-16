#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 패턴형 피하기 게임 시스템을 PuriTestScene에 자동으로 세팅하는 에디터 전용 도구.
// 실행: Tools/선잠/패턴 시스템 자동 세팅, 또는 -executeMethod PatternSystemSetup.Run
public static class PatternSystemSetup
{
    private const string ScenePath = "Assets/Scenes/PuriTestScene.unity";
    private const string GeneratedFolder = "Assets/Generated";
    private const string PrefabFolder = "Assets/Prefab/Hazard";

    [MenuItem("Tools/선잠/패턴 시스템 자동 세팅")]
    public static void Run()
    {
        EditorSceneManager.OpenScene(ScenePath);

        RemoveLegacyScoreUI();

        Sprite squareSprite = GetOrCreateSquareSprite();

        GameObject dangerZonePrefab = GetOrCreatePrefab("DangerZone", squareSprite, new Color(1f, 0f, 0f, 0.5f), typeof(DangerZoneIndicator), false);
        GameObject fallingPrefab = GetOrCreatePrefab("FallingHazard", squareSprite, new Color(0.85f, 0.2f, 0.9f, 1f), typeof(FallingHazard), true);
        GameObject projectilePrefab = GetOrCreatePrefab("ProjectileHazard", squareSprite, new Color(1f, 0.6f, 0f, 1f), typeof(ProjectileHazard), true);
        GameObject risingPrefab = GetOrCreatePrefab("RisingHazard", squareSprite, new Color(0.9f, 0.1f, 0.1f, 1f), typeof(RisingHazard), true);
        GameObject sweepPrefab = GetOrCreatePrefab("SweepHazard", squareSprite, new Color(0.1f, 0.7f, 0.9f, 1f), typeof(SweepHazard), true);

        SetupPoolManager(dangerZonePrefab, fallingPrefab, projectilePrefab, risingPrefab, sweepPrefab);
        ArenaBounds arenaBounds = SetupArenaBounds();
        SetupPatternManager(arenaBounds);
        SetupClearUI();

        Sprite circleSprite = GetOrCreateCircleSprite();
        SetupPlayerExplosionAttack(circleSprite);

        Scene scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[PatternSystemSetup] 세팅 완료");
    }

    private static Sprite GetOrCreateSquareSprite()
    {
        string path = GeneratedFolder + "/square.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        EnsureFolder(GeneratedFolder);

        Texture2D tex = new Texture2D(4, 4);
        Color[] pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 4;
        importer.filterMode = FilterMode.Point;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Sprite GetOrCreateCircleSprite()
    {
        string path = GeneratedFolder + "/circle.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        EnsureFolder(GeneratedFolder);

        const int size = 32;
        Texture2D tex = new Texture2D(size, size);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                pixels[y * size + x] = dist <= radius ? Color.white : new Color(1f, 1f, 1f, 0f);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = size; // 텍스처 크기 = 스케일 1일 때 1유닛짜리 스프라이트
        importer.filterMode = FilterMode.Bilinear;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void SetupPlayerExplosionAttack(Sprite circleSprite)
    {
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            Debug.LogWarning("[PatternSystemSetup] PlayerController 없음, PlayerExplosionAttack 스킵");
            return;
        }

        PlayerExplosionAttack attack = player.GetComponent<PlayerExplosionAttack>();
        if (attack == null) attack = player.gameObject.AddComponent<PlayerExplosionAttack>();

        attack.effectSprite = circleSprite;
        EditorUtility.SetDirty(player.gameObject);
    }

    private static GameObject GetOrCreatePrefab(string name, Sprite sprite, Color color, System.Type scriptType, bool isHazard)
    {
        string path = $"{PrefabFolder}/{name}.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        EnsureFolder(PrefabFolder);

        GameObject go = new GameObject(name);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = isHazard ? 10 : 5;

        go.AddComponent(scriptType);

        if (isHazard)
        {
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        // SaveAsPrefabAsset 직후 반환값을 바로 쓰면 배치 모드에서 간헐적으로 GUID가
        // 아직 확정되지 않아 참조가 비어버리는 경우가 있어, 강제 동기 임포트 후 다시 로드한다.
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static void SetupPoolManager(GameObject dangerZone, GameObject falling, GameObject projectile, GameObject rising, GameObject sweep)
    {
        PoolManager poolManager = Object.FindFirstObjectByType<PoolManager>();
        if (poolManager == null)
        {
            Debug.LogWarning("[PatternSystemSetup] PoolManager 없음, 풀 등록 스킵");
            return;
        }

        if (poolManager.poolItems == null) poolManager.poolItems = new List<PoolItem>();

        AddPoolItemIfMissing(poolManager, "DangerZone", dangerZone, 10);
        AddPoolItemIfMissing(poolManager, "FallingHazard", falling, 15);
        AddPoolItemIfMissing(poolManager, "ProjectileHazard", projectile, 15);
        AddPoolItemIfMissing(poolManager, "RisingHazard", rising, 15);
        AddPoolItemIfMissing(poolManager, "SweepHazard", sweep, 5);

        EditorUtility.SetDirty(poolManager);
    }

    private static void AddPoolItemIfMissing(PoolManager pm, string poolName, GameObject prefab, int initCount)
    {
        foreach (var item in pm.poolItems)
        {
            if (item.poolName != poolName) continue;

            // 이전 실행에서 참조가 비어버린 채로 남은 항목이면 이번에 복구한다.
            if (item.prefab == null)
            {
                item.prefab = prefab;
                item.initCount = initCount;
            }
            return;
        }
        pm.poolItems.Add(new PoolItem { poolName = poolName, prefab = prefab, initCount = initCount });
    }

    private static ArenaBounds SetupArenaBounds()
    {
        ArenaBounds bounds = Object.FindFirstObjectByType<ArenaBounds>();
        if (bounds != null)
        {
            Debug.Log("[PatternSystemSetup] ArenaBounds 이미 있음, 스킵");
            return bounds;
        }

        GameObject boundsObj = new GameObject("ArenaBounds");
        bounds = boundsObj.AddComponent<ArenaBounds>();

        // 초기값은 카메라/바닥 오브젝트 기준으로 추정한 값일 뿐이며, 인스펙터에서 직접 튜닝하는 걸 전제로 한다.
        float ceiling = bounds.ceilingY;
        float left = bounds.leftX;
        float right = bounds.rightX;
        float floor = bounds.floorY;

        Camera cam = Camera.main;
        if (cam != null && cam.orthographic)
        {
            ceiling = cam.transform.position.y + cam.orthographicSize;
            left = cam.transform.position.x - cam.orthographicSize * cam.aspect;
            right = cam.transform.position.x + cam.orthographicSize * cam.aspect;
        }

        GameObject ground = GameObject.Find("Square");
        if (ground != null)
        {
            SpriteRenderer groundSr = ground.GetComponent<SpriteRenderer>();
            floor = groundSr != null ? groundSr.bounds.max.y : ground.transform.position.y;
        }

        bounds.floorY = floor;
        bounds.ceilingY = ceiling;
        bounds.leftX = left;
        bounds.rightX = right;

        EditorUtility.SetDirty(bounds);
        return bounds;
    }

    private static void SetupPatternManager(ArenaBounds arenaBounds)
    {
        PatternManager pm = Object.FindFirstObjectByType<PatternManager>();
        GameObject pmObj;

        if (pm == null)
        {
            pmObj = new GameObject("PatternManager");
            pm = pmObj.AddComponent<PatternManager>();
        }
        else
        {
            pmObj = pm.gameObject;
        }

        // 구 패턴 자식들이 남아있으면(재작업 이전 세팅) 전부 정리하고 3종으로 다시 채운다.
        for (int i = pmObj.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(pmObj.transform.GetChild(i).gameObject);
        }

        pm.arenaBounds = arenaBounds;

        ObstacleRushPattern obstacleRush = CreatePatternChild<ObstacleRushPattern>(pmObj, "ObstacleRushPattern");
        PillarPattern pillar = CreatePatternChild<PillarPattern>(pmObj, "PillarPattern");
        DropPattern drop = CreatePatternChild<DropPattern>(pmObj, "DropPattern");
        SweepChargePattern sweepCharge = CreatePatternChild<SweepChargePattern>(pmObj, "SweepChargePattern");
        SplitBurstPattern splitBurst = CreatePatternChild<SplitBurstPattern>(pmObj, "SplitBurstPattern");

        // 스펙 기본 게이팅: ObstacleRush 0초, DropPattern 15초, PillarPattern 30초, SweepCharge 45초, SplitBurst 90초.
        obstacleRush.unlockTime = 0f;
        drop.unlockTime = 15f;
        pillar.unlockTime = 30f;
        sweepCharge.unlockTime = 45f;
        splitBurst.unlockTime = 90f;

        pm.patterns = new List<PatternBase> { obstacleRush, pillar, drop, sweepCharge, splitBurst };

        EditorUtility.SetDirty(pm);
    }

    private static T CreatePatternChild<T>(GameObject parent, string name) where T : PatternBase
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform);
        return child.AddComponent<T>();
    }

    // 점수 시스템 제거에 따라 이전 세팅에서 만들어졌던 ScoreText(ScoreUI)를 씬에서 지운다.
    // 스크립트 자체가 삭제됐으므로 컴포넌트 타입이 아니라 이름으로 찾는다.
    private static void RemoveLegacyScoreUI()
    {
        GameObject scoreTextObj = GameObject.Find("ScoreText");
        if (scoreTextObj == null) return;

        Object.DestroyImmediate(scoreTextObj);
        Debug.Log("[PatternSystemSetup] 레거시 ScoreText 제거함");
    }

    private static void SetupClearUI()
    {
        UIManager uiManager = Object.FindFirstObjectByType<UIManager>();
        if (uiManager == null)
        {
            Debug.LogWarning("[PatternSystemSetup] UIManager 없음, ClearUI 스킵");
            return;
        }

        if (uiManager.clearPanel != null)
        {
            Debug.Log("[PatternSystemSetup] clearPanel 이미 있음, 스킵");
            return;
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[PatternSystemSetup] Canvas 없음, ClearUI 스킵");
            return;
        }

        GameObject panelObj = new GameObject("ClearPanel");
        panelObj.transform.SetParent(canvas.transform, false);
        RectTransform panelRt = panelObj.AddComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        GameObject textObj = new GameObject("ClearResultText");
        textObj.transform.SetParent(panelObj.transform, false);

        UnityEngine.UI.Text text = textObj.AddComponent<UnityEngine.UI.Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 48;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = "생존 성공!";

        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.5f, 0.5f);
        textRt.anchorMax = new Vector2(0.5f, 0.5f);
        textRt.pivot = new Vector2(0.5f, 0.5f);
        textRt.anchoredPosition = Vector2.zero;
        textRt.sizeDelta = new Vector2(600f, 200f);

        ClearResultUI clearResultUI = panelObj.AddComponent<ClearResultUI>();
        clearResultUI.resultText = text;

        panelObj.SetActive(false); // UIManager.UpdateUI가 GameState.Clear일 때만 켠다

        uiManager.clearPanel = panelObj;
        EditorUtility.SetDirty(uiManager);
        EditorUtility.SetDirty(panelObj);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string folderName = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folderName);
    }
}
#endif
