using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WaveSpawnerTemplate.Demo;
using WaveSpawnerTemplate.Pooling;
using WaveSpawnerTemplate.Spawning;
using WaveSpawnerTemplate.Wave;

namespace WaveSpawnerTemplate.EditorTools
{
    /// 웨이브 스포너 템플릿 데모 씬을 한 번에 구성해주는 에디터 도구
    public static class WaveSpawnerSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/WaveSpawnerDemo.unity"; // 생성될 데모 씬 경로
        private const string SpriteFolder = "Assets/Resource/Sprites"; // 생성될 데모 스프라이트 폴더
        private const string SpawnableFolder = "Assets/Data/Spawnables"; // SpawnableData 에셋 폴더
        private const string WaveFolder = "Assets/Data/Waves"; // WaveData/WaveSequenceData 에셋 폴더
        private const string PrefabFolder = "Assets/Prefab/WaveSpawner"; // 데모 프리팹 폴더

        private const int SpriteSize = 64; // 생성할 스프라이트 텍스처 한 변 픽셀 크기
        private const float PixelsPerUnit = 64f; // 스프라이트 1유닛당 픽셀 수 (오브젝트 크기 1유닛으로 통일)

        [MenuItem("Wave Spawner Template/Build Demo Scene")]
        public static void BuildDemoScene()
        {
            // 현재 씬에 저장 안 한 변경사항이 있으면 사용자에게 먼저 물어봄 (작업 중이던 씬을 실수로 날리지 않도록)
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            // 생성될 애셋들이 들어갈 폴더를 미리 준비
            EnsureFolder(SpriteFolder);
            EnsureFolder(SpawnableFolder);
            EnsureFolder(WaveFolder);
            EnsureFolder(PrefabFolder);

            // 완전히 새 빈 씬에서 시작 (기존 씬 내용은 버림)
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 데모용 프리팹 3종 - 실제 이미지 없이 코드로 색만 다른 스프라이트를 생성해서 붙임
            GameObject redSquarePrefab = CreateDemoPrefab("RedSquare", new Color(0.9f, 0.25f, 0.25f), false);
            GameObject blueCirclePrefab = CreateDemoPrefab("BlueCircle", new Color(0.25f, 0.45f, 0.95f), true);
            GameObject greenCirclePrefab = CreateDemoPrefab("GreenCircle", new Color(0.3f, 0.85f, 0.4f), true);

            // 각 프리팹을 감싸는 SpawnableData 애셋 - 카테고리(TypeA/TypeB)를 다르게 줘서 스폰 지점 필터링 데모에 사용
            SpawnableData sdRedSquare = CreateSpawnableData("SD_RedSquare", "Red Square", redSquarePrefab, SpawnableCategory.TypeA);
            SpawnableData sdBlueCircle = CreateSpawnableData("SD_BlueCircle", "Blue Circle", blueCirclePrefab, SpawnableCategory.TypeB);
            SpawnableData sdGreenCircle = CreateSpawnableData("SD_GreenCircle", "Green Circle", greenCirclePrefab, SpawnableCategory.TypeB);

            // 웨이브 1: 빨간 네모 5개, 1초 간격, 수량 다 스폰하면 종료
            WaveData wave1 = CreateWaveData(
                "WD_Wave1_RedSquares",
                "Wave 1 - Red Squares",
                new[] { (sdRedSquare, 5) },
                1.0f,
                WaveEndCondition.AllQuantitySpawned,
                0f);

            // 웨이브 2: 파란 원 10개, 0.5초 간격 (좀 더 빠르고 많이)
            WaveData wave2 = CreateWaveData(
                "WD_Wave2_BlueCircles",
                "Wave 2 - Blue Circles",
                new[] { (sdBlueCircle, 10) },
                0.5f,
                WaveEndCondition.AllQuantitySpawned,
                0f);

            // 웨이브 3: 세 종류 섞어서 8초 동안 계속 스폰 (FixedDuration 모드 데모)
            WaveData wave3 = CreateWaveData(
                "WD_Wave3_Mixed",
                "Wave 3 - Mixed (Timed)",
                new[] { (sdRedSquare, 3), (sdBlueCircle, 3), (sdGreenCircle, 3) },
                0.4f,
                WaveEndCondition.FixedDuration,
                8f);

            // 위 세 웨이브를 순서대로 묶은 시퀀스 (웨이브 사이 대기시간: 0초 → 2초 → 2초)
            WaveSequenceData sequence = CreateWaveSequenceData(
                "WSD_DemoStage",
                "Demo Stage",
                new[] { (wave1, 0f), (wave2, 2f), (wave3, 2f) });

            // 씬 하이어라키 구성 - 카메라 → 풀매니저 → 스폰 지점 그룹 → 웨이브 스포너 순서로 생성
            CreateCamera();
            PoolManager poolManager = CreatePoolManager(redSquarePrefab, blueCirclePrefab, greenCirclePrefab);
            SpawnPointGroup spawnPointGroup = CreateSpawnPointGroup();
            WaveSpawner waveSpawner = CreateWaveSpawner(sequence, spawnPointGroup);

            // 콘솔 로그 출력 + 디버그 단축키 같은 데모/테스트 편의 컴포넌트 추가
            AttachDemoHelpers(waveSpawner);

            // 새로 만든 애셋(스프라이트, 데이터, 프리팹)들을 디스크에 저장
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 씬 파일도 디스크에 저장해야 다음에 열었을 때 내용이 남아있음
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log($"[Wave Spawner Template] 데모 씬 생성 완료: {ScenePath}");
        }

        private static void EnsureFolder(string assetPath)
        {
            // 이미 있으면 아무것도 안 함
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            string parent = assetPath.Substring(0, assetPath.LastIndexOf('/'));
            string folderName = assetPath.Substring(assetPath.LastIndexOf('/') + 1);

            // 부모 폴더도 없으면 재귀적으로 먼저 만듦 (예: Assets/A/B면 Assets/A부터)
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }

        // ------------------------------------------------------------------
        // 스프라이트 / 프리팹 생성
        // ------------------------------------------------------------------

        private static GameObject CreateDemoPrefab(string name, Color color, bool isCircle)
        {
            Sprite sprite = CreateDemoSprite(name, color, isCircle);

            GameObject go = new GameObject(name);
            SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;

            // 2D 물리 충돌 감지용 (실제 이동/충돌 로직은 데모에 없지만 트리거/충돌 판정을 붙일 수 있도록 기본 세팅)
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic; // 물리 힘을 안 받고 코드로만 움직이게 함
            rb.gravityScale = 0f; // 탑다운 시점이라 중력 불필요

            // 모양에 맞는 콜라이더 타입 선택
            if (isCircle)
            {
                go.AddComponent<CircleCollider2D>();
            }
            else
            {
                go.AddComponent<BoxCollider2D>();
            }

            // IPoolable 구현체 - 일정 시간 후 자동으로 풀에 반환되는 데모 동작 부여
            PooledObjectDemo demo = go.AddComponent<PooledObjectDemo>();
            SerializedObject demoSo = new SerializedObject(demo);
            demoSo.FindProperty("lifeTime").floatValue = 3f;
            demoSo.ApplyModifiedPropertiesWithoutUndo();

            // 씬에 만든 임시 오브젝트를 프리팹 애셋으로 저장한 뒤, 씬에 남은 임시본은 지움
            string prefabPath = $"{PrefabFolder}/{name}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath, out bool success);
            Object.DestroyImmediate(go);

            if (!success)
            {
                Debug.LogError($"[Wave Spawner Template] 프리팹 저장 실패: {prefabPath}");
            }

            return prefab;
        }

        private static Sprite CreateDemoSprite(string name, Color color, bool isCircle)
        {
            // 실제 아트 없이도 데모가 가능하도록 단색 정사각형/원을 픽셀 단위로 직접 그려 텍스처를 만듦
            Texture2D texture = new Texture2D(SpriteSize, SpriteSize, TextureFormat.RGBA32, false);
            Color clear = new Color(0f, 0f, 0f, 0f);
            float radius = SpriteSize * 0.5f;
            Vector2 center = new Vector2(radius, radius);

            for (int y = 0; y < SpriteSize; y++)
            {
                for (int x = 0; x < SpriteSize; x++)
                {
                    bool inside;

                    if (isCircle)
                    {
                        // 픽셀이 중심에서 반지름 이내에 있으면 색칠, 아니면 원 밖이므로 투명 처리 (둥근 모양 만들기)
                        inside = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) <= radius;
                    }
                    else
                    {
                        // 사각형은 그냥 전체를 다 칠함
                        inside = true;
                    }

                    texture.SetPixel(x, y, inside ? color : clear);
                }
            }

            texture.Apply();

            // 텍스처를 PNG 파일로 실제 디스크에 저장해야 Unity가 스프라이트로 임포트할 수 있음 (메모리상 텍스처만으론 부족)
            string path = $"{SpriteFolder}/{name}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            // 임포트 설정을 스프라이트용으로 지정 (기본값은 Texture라서 그대로 두면 SpriteRenderer에 못 씀)
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ------------------------------------------------------------------
        // 데이터 에셋 생성
        // ------------------------------------------------------------------

        private static SpawnableData CreateSpawnableData(string assetName, string displayName, GameObject prefab, SpawnableCategory category)
        {
            SpawnableData data = ScriptableObject.CreateInstance<SpawnableData>();

            // SpawnableData의 필드는 전부 private라서 SerializedObject를 거쳐야 값을 채울 수 있음
            SerializedObject so = new SerializedObject(data);
            so.FindProperty("spawnableName").stringValue = displayName;
            so.FindProperty("prefab").objectReferenceValue = prefab;
            so.FindProperty("category").enumValueIndex = (int)category;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(data, $"{SpawnableFolder}/{assetName}.asset");
            return data;
        }

        private static WaveData CreateWaveData(
            string assetName,
            string displayName,
            (SpawnableData data, int count)[] entries,
            float spawnInterval,
            WaveEndCondition endCondition,
            float fixedDuration)
        {
            WaveData wave = ScriptableObject.CreateInstance<WaveData>();

            SerializedObject so = new SerializedObject(wave);
            so.FindProperty("waveName").stringValue = displayName;
            so.FindProperty("spawnInterval").floatValue = spawnInterval;
            so.FindProperty("endCondition").enumValueIndex = (int)endCondition;
            so.FindProperty("fixedDuration").floatValue = fixedDuration;

            // entries 배열(List<WaveEntry>)은 크기부터 지정한 뒤 인덱스별로 값을 채워야 함
            SerializedProperty entriesProp = so.FindProperty("entries");
            entriesProp.arraySize = entries.Length;

            for (int i = 0; i < entries.Length; i++)
            {
                SerializedProperty elem = entriesProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("spawnableData").objectReferenceValue = entries[i].data;
                elem.FindPropertyRelative("count").intValue = entries[i].count;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(wave, $"{WaveFolder}/{assetName}.asset");
            return wave;
        }

        private static WaveSequenceData CreateWaveSequenceData(string assetName, string displayName, (WaveData wave, float delay)[] waves)
        {
            WaveSequenceData sequence = ScriptableObject.CreateInstance<WaveSequenceData>();

            SerializedObject so = new SerializedObject(sequence);
            so.FindProperty("sequenceName").stringValue = displayName;

            SerializedProperty wavesProp = so.FindProperty("waves");
            wavesProp.arraySize = waves.Length;

            for (int i = 0; i < waves.Length; i++)
            {
                SerializedProperty elem = wavesProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("waveData").objectReferenceValue = waves[i].wave;
                elem.FindPropertyRelative("delayBeforeStart").floatValue = waves[i].delay;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(sequence, $"{WaveFolder}/{assetName}.asset");
            return sequence;
        }

        // ------------------------------------------------------------------
        // 씬 하이어라키 구성
        // ------------------------------------------------------------------

        private static void CreateCamera()
        {
            // 2D 탑다운 시점 - z를 -10만큼 뒤로 빼야 z=0 평면의 스프라이트들이 카메라 앞에 보임
            GameObject cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);

            Camera camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true; // 2D니까 원근감 없는 직교 투영
            camera.orthographicSize = 6f; // 스폰 지점(±6)이 다 화면에 들어오도록 시야 범위 설정
            camera.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
            camera.clearFlags = CameraClearFlags.SolidColor;

            cameraGo.AddComponent<AudioListener>(); // 나중에 사운드 붙일 걸 대비해 기본으로 포함
        }

        private static PoolManager CreatePoolManager(params GameObject[] prefabs)
        {
            GameObject go = new GameObject("PoolManager");
            PoolManager poolManager = go.AddComponent<PoolManager>();

            SerializedObject so = new SerializedObject(poolManager);
            so.FindProperty("defaultInitialSize").intValue = 5;
            so.FindProperty("defaultMaxSize").intValue = 0;

            // 데모 프리팹 3종을 전부 미리 등록해둠 - 웨이브 시작 전에 이미 풀이 준비되어 첫 스폰부터 버벅임 없음
            SerializedProperty presetsProp = so.FindProperty("presets");
            presetsProp.arraySize = prefabs.Length;

            // 프리팹 순서(빨간네모/파란원/초록원)에 맞춰 각각 다른 초기/최대 크기 지정 (빨간네모10, 파란원15, 초록원15+최대20)
            int[] initialSizes = { 10, 15, 15 };
            int[] maxSizes = { 0, 0, 20 };

            for (int i = 0; i < prefabs.Length; i++)
            {
                SerializedProperty elem = presetsProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("prefab").objectReferenceValue = prefabs[i];
                elem.FindPropertyRelative("initialSize").intValue = initialSizes[i];
                elem.FindPropertyRelative("maxSize").intValue = maxSizes[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            return poolManager;
        }

        private static SpawnPointGroup CreateSpawnPointGroup()
        {
            GameObject groupGo = new GameObject("SpawnPointGroup");
            SpawnPointGroup group = groupGo.AddComponent<SpawnPointGroup>();

            // 화면 네 귀퉁이에 스폰 지점을 하나씩 배치
            Vector2[] positions =
            {
                new Vector2(-6f, 4f),
                new Vector2(6f, 4f),
                new Vector2(-6f, -4f),
                new Vector2(6f, -4f)
            };

            SpawnPoint[] points = new SpawnPoint[positions.Length];

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject pointGo = new GameObject($"SpawnPoint_{i}");
                pointGo.transform.SetParent(groupGo.transform);
                pointGo.transform.position = positions[i];
                points[i] = pointGo.AddComponent<SpawnPoint>();
            }

            // 마지막 스폰 지점(오른쪽 아래)은 원형(TypeB) 카테고리만 스폰하도록 제한 (필터링 데모)
            SerializedObject lastPointSo = new SerializedObject(points[points.Length - 1]);
            lastPointSo.FindProperty("restrictByCategory").boolValue = true;
            SerializedProperty categoriesProp = lastPointSo.FindProperty("allowedCategories");
            categoriesProp.arraySize = 1;
            categoriesProp.GetArrayElementAtIndex(0).enumValueIndex = (int)SpawnableCategory.TypeB;
            lastPointSo.ApplyModifiedPropertiesWithoutUndo();

            // 전략을 RoundRobin으로 지정 - 네 지점을 순서대로 돌아가며 스폰 (Random 데모와 차별화)
            SerializedObject groupSo = new SerializedObject(group);
            groupSo.FindProperty("strategy").enumValueIndex = (int)SpawnSelectionStrategy.RoundRobin;
            SerializedProperty pointsProp = groupSo.FindProperty("spawnPoints");
            pointsProp.arraySize = points.Length;

            for (int i = 0; i < points.Length; i++)
            {
                pointsProp.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
            }

            groupSo.ApplyModifiedPropertiesWithoutUndo();

            return group;
        }

        private static WaveSpawner CreateWaveSpawner(WaveSequenceData sequence, SpawnPointGroup spawnPointGroup)
        {
            GameObject go = new GameObject("WaveSpawner");
            WaveSpawner spawner = go.AddComponent<WaveSpawner>();

            SerializedObject so = new SerializedObject(spawner);
            so.FindProperty("waveSequence").objectReferenceValue = sequence;
            so.FindProperty("spawnPointGroup").objectReferenceValue = spawnPointGroup;
            // 스폰된 오브젝트가 전부 반환될 때까지 다음 웨이브로 안 넘어가게 - 데모에서 웨이브 구분이 눈에 잘 보이도록
            so.FindProperty("advanceMode").enumValueIndex = (int)WaveAdvanceMode.OnAllSpawnedDespawned;
            so.FindProperty("autoStart").boolValue = true; // Play 누르면 바로 시작
            so.ApplyModifiedPropertiesWithoutUndo();

            return spawner;
        }

        private static void AttachDemoHelpers(WaveSpawner waveSpawner)
        {
            // 콘솔에 웨이브 시작/종료/스폰 로그를 출력하는 이벤트 구독 예시 컴포넌트
            WaveSpawnerEventLogger logger = waveSpawner.gameObject.AddComponent<WaveSpawnerEventLogger>();
            SerializedObject loggerSo = new SerializedObject(logger);
            loggerSo.FindProperty("waveSpawner").objectReferenceValue = waveSpawner;
            loggerSo.ApplyModifiedPropertiesWithoutUndo();

            // P키(일시정지 토글)/N키(다음 웨이브 스킵) 같은 테스트용 단축키 입력 컴포넌트
            WaveSpawnerDebugInput debugInput = waveSpawner.gameObject.AddComponent<WaveSpawnerDebugInput>();
            SerializedObject debugInputSo = new SerializedObject(debugInput);
            debugInputSo.FindProperty("waveSpawner").objectReferenceValue = waveSpawner;
            debugInputSo.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
