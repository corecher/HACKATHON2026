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
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureFolder(SpriteFolder);
            EnsureFolder(SpawnableFolder);
            EnsureFolder(WaveFolder);
            EnsureFolder(PrefabFolder);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject redSquarePrefab = CreateDemoPrefab("RedSquare", new Color(0.9f, 0.25f, 0.25f), false);
            GameObject blueCirclePrefab = CreateDemoPrefab("BlueCircle", new Color(0.25f, 0.45f, 0.95f), true);
            GameObject greenCirclePrefab = CreateDemoPrefab("GreenCircle", new Color(0.3f, 0.85f, 0.4f), true);

            SpawnableData sdRedSquare = CreateSpawnableData("SD_RedSquare", "Red Square", redSquarePrefab, SpawnableCategory.TypeA);
            SpawnableData sdBlueCircle = CreateSpawnableData("SD_BlueCircle", "Blue Circle", blueCirclePrefab, SpawnableCategory.TypeB);
            SpawnableData sdGreenCircle = CreateSpawnableData("SD_GreenCircle", "Green Circle", greenCirclePrefab, SpawnableCategory.TypeB);

            WaveData wave1 = CreateWaveData(
                "WD_Wave1_RedSquares",
                "Wave 1 - Red Squares",
                new[] { (sdRedSquare, 5) },
                1.0f,
                WaveEndCondition.AllQuantitySpawned,
                0f);

            WaveData wave2 = CreateWaveData(
                "WD_Wave2_BlueCircles",
                "Wave 2 - Blue Circles",
                new[] { (sdBlueCircle, 10) },
                0.5f,
                WaveEndCondition.AllQuantitySpawned,
                0f);

            WaveData wave3 = CreateWaveData(
                "WD_Wave3_Mixed",
                "Wave 3 - Mixed (Timed)",
                new[] { (sdRedSquare, 3), (sdBlueCircle, 3), (sdGreenCircle, 3) },
                0.4f,
                WaveEndCondition.FixedDuration,
                8f);

            WaveSequenceData sequence = CreateWaveSequenceData(
                "WSD_DemoStage",
                "Demo Stage",
                new[] { (wave1, 0f), (wave2, 2f), (wave3, 2f) });

            CreateCamera();
            PoolManager poolManager = CreatePoolManager(redSquarePrefab, blueCirclePrefab, greenCirclePrefab);
            SpawnPointGroup spawnPointGroup = CreateSpawnPointGroup();
            WaveSpawner waveSpawner = CreateWaveSpawner(sequence, spawnPointGroup);

            AttachDemoHelpers(waveSpawner);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath) ?? "Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log($"[Wave Spawner Template] 데모 씬 생성 완료: {ScenePath}");
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            string parent = assetPath.Substring(0, assetPath.LastIndexOf('/'));
            string folderName = assetPath.Substring(assetPath.LastIndexOf('/') + 1);

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

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            if (isCircle)
            {
                go.AddComponent<CircleCollider2D>();
            }
            else
            {
                go.AddComponent<BoxCollider2D>();
            }

            PooledObjectDemo demo = go.AddComponent<PooledObjectDemo>();
            SerializedObject demoSo = new SerializedObject(demo);
            demoSo.FindProperty("lifeTime").floatValue = 3f;
            demoSo.ApplyModifiedPropertiesWithoutUndo();

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
                        inside = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) <= radius;
                    }
                    else
                    {
                        inside = true;
                    }

                    texture.SetPixel(x, y, inside ? color : clear);
                }
            }

            texture.Apply();

            string path = $"{SpriteFolder}/{name}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

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
            GameObject cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);

            Camera camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
            camera.clearFlags = CameraClearFlags.SolidColor;

            cameraGo.AddComponent<AudioListener>();
        }

        private static PoolManager CreatePoolManager(params GameObject[] prefabs)
        {
            GameObject go = new GameObject("PoolManager");
            PoolManager poolManager = go.AddComponent<PoolManager>();

            SerializedObject so = new SerializedObject(poolManager);
            so.FindProperty("defaultInitialSize").intValue = 5;
            so.FindProperty("defaultMaxSize").intValue = 0;

            SerializedProperty presetsProp = so.FindProperty("presets");
            presetsProp.arraySize = prefabs.Length;

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
            so.FindProperty("advanceMode").enumValueIndex = (int)WaveAdvanceMode.OnAllSpawnedDespawned;
            so.FindProperty("autoStart").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            return spawner;
        }

        private static void AttachDemoHelpers(WaveSpawner waveSpawner)
        {
            WaveSpawnerEventLogger logger = waveSpawner.gameObject.AddComponent<WaveSpawnerEventLogger>();
            SerializedObject loggerSo = new SerializedObject(logger);
            loggerSo.FindProperty("waveSpawner").objectReferenceValue = waveSpawner;
            loggerSo.ApplyModifiedPropertiesWithoutUndo();

            WaveSpawnerDebugInput debugInput = waveSpawner.gameObject.AddComponent<WaveSpawnerDebugInput>();
            SerializedObject debugInputSo = new SerializedObject(debugInput);
            debugInputSo.FindProperty("waveSpawner").objectReferenceValue = waveSpawner;
            debugInputSo.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
