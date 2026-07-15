using System.Collections.Generic;
using System.IO;
using CardGameTemplate;
using CardGameTemplate.Deck;
using CardGameTemplate.DragDrop;
using CardGameTemplate.UI;
using CardGameTemplate.Zones;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using CardNS = CardGameTemplate.Card;

namespace CardGameTemplate.EditorTools
{
    /// <summary>
    /// 템플릿용 카드 프리팹/샘플 카드 데이터/데모 씬을 코드로 자동 생성하는 에디터 전용 빌더
    /// </summary>
    public static class CardGameTemplateBuilder
    {
        private const string PrefabFolder = "Assets/Prefab";
        private const string CardPrefabPath = PrefabFolder + "/Card.prefab";
        private const string DataFolder = "Assets/Data";
        private const string CardDataFolder = DataFolder + "/Cards";
        private const string GeneratedFolder = DataFolder + "/Generated";
        private const string PlaceholderSpritePath = GeneratedFolder + "/PlaceholderSquare.asset";
        private const string ScenePath = "Assets/Scenes/CardGameTemplate.unity";

        private static readonly Vector2 CardSize = new Vector2(1.4f, 2f);

        /// <summary>메뉴에서 실행하는 진입점. 프리팹, 샘플 카드, 데모 씬을 전부 생성한다</summary>
        [MenuItem("Card Game Template/Build Demo Scene")]
        public static void BuildDemoScene()
        {
            AssetDatabase.Refresh();
            ImportTmpEssentialsIfNeeded();
            EnsureFolders();

            // NewScene 호출은 이전에 로드해둔 에셋 참조를 무효화할 수 있으므로
            // 다른 에셋을 생성하기 전에 먼저 대상 씬을 열어 둔다
            Scene scene = OpenTargetScene();

            Sprite placeholder = GetOrCreatePlaceholderSprite();
            List<CardNS.CardData> sampleCards = CreateSampleCardData(placeholder);
            GameObject cardPrefab = BuildCardPrefab(placeholder);
            PopulateScene(scene, cardPrefab, sampleCards, placeholder);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CardGameTemplateBuilder] 데모 씬 생성 완료: {ScenePath}");
        }

        private static void ImportTmpEssentialsIfNeeded()
        {
            const string marker = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(marker) != null)
            {
                return;
            }

            string packagePath = Path.Combine(
                EditorApplication.applicationContentsPath,
                "Resources/PackageManager/BuiltInPackages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");

            if (File.Exists(packagePath))
            {
                AssetDatabase.ImportPackage(packagePath, false);
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.LogWarning("[CardGameTemplateBuilder] TMP Essential Resources 패키지를 찾지 못했습니다. Window > TextMeshPro > Import TMP Essential Resources 로 수동 임포트하세요.");
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "Prefab");
            EnsureFolder("Assets", "Data");
            EnsureFolder(DataFolder, "Cards");
            EnsureFolder(DataFolder, "Generated");
            EnsureFolder("Assets", "Scenes");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string full = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(full))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static Sprite GetOrCreatePlaceholderSprite()
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
            if (existing != null)
            {
                return existing;
            }

            const int size = 8;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "PlaceholderSquareTexture"
            };

            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = "PlaceholderSquare";

            AssetDatabase.CreateAsset(texture, PlaceholderSpritePath);
            AssetDatabase.AddObjectToAsset(sprite, texture);
            AssetDatabase.SaveAssets();

            return AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
        }

        private static List<CardNS.CardData> CreateSampleCardData(Sprite icon)
        {
            string[] names = { "Warrior", "Mage", "Archer", "Shieldbearer", "Assassin", "Healer", "Rogue", "Knight", "Druid", "Berserker" };
            int[] costs = { 1, 2, 1, 3, 2, 2, 1, 3, 2, 3 };
            int[] powers = { 2, 1, 3, 1, 4, 0, 2, 3, 1, 5 };

            List<CardNS.CardData> result = new List<CardNS.CardData>();

            for (int i = 0; i < names.Length; i++)
            {
                string path = $"{CardDataFolder}/Card_{i:00}_{names[i]}.asset";
                CardNS.CardData data = AssetDatabase.LoadAssetAtPath<CardNS.CardData>(path);
                bool isNew = data == null;
                if (isNew)
                {
                    data = ScriptableObject.CreateInstance<CardNS.CardData>();
                }

                SerializedObject so = new SerializedObject(data);
                so.FindProperty("cardId").stringValue = $"card_{i:000}";
                so.FindProperty("cardName").stringValue = names[i];
                so.FindProperty("description").stringValue = $"{names[i]} 샘플 카드 (템플릿 테스트용)";
                so.FindProperty("icon").objectReferenceValue = icon;
                so.FindProperty("cost").intValue = costs[i];
                so.FindProperty("power").intValue = powers[i];
                so.ApplyModifiedPropertiesWithoutUndo();

                if (isNew)
                {
                    AssetDatabase.CreateAsset(data, path);
                }

                result.Add(data);
            }

            AssetDatabase.SaveAssets();
            return result;
        }

        private static GameObject BuildCardPrefab(Sprite placeholder)
        {
            GameObject root = new GameObject("Card");

            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.size = CardSize;

            Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            root.AddComponent<SortingGroup>();

            GameObject front = new GameObject("Front");
            front.transform.SetParent(root.transform, false);

            GameObject frame = new GameObject("Frame");
            frame.transform.SetParent(front.transform, false);
            SpriteRenderer frameSr = frame.AddComponent<SpriteRenderer>();
            frameSr.sprite = placeholder;
            frameSr.color = new Color(0.95f, 0.95f, 0.88f);
            frameSr.sortingOrder = 0;
            frame.transform.localScale = new Vector3(CardSize.x, CardSize.y, 1f);

            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(front.transform, false);
            SpriteRenderer iconSr = icon.AddComponent<SpriteRenderer>();
            iconSr.sprite = placeholder;
            iconSr.color = new Color(0.5f, 0.6f, 0.9f);
            iconSr.sortingOrder = 1;
            icon.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            icon.transform.localScale = new Vector3(0.75f, 0.75f, 1f);

            TextMeshPro nameText = CreateWorldText(front.transform, "NameText", new Vector3(0f, 0.85f, 0f),
                0.32f, TextAlignmentOptions.Center, Color.black, CardSize.x - 0.1f, 0.35f, 2);

            TextMeshPro descText = CreateWorldText(front.transform, "DescText", new Vector3(0f, -0.35f, 0f),
                0.18f, TextAlignmentOptions.Center, new Color(0.2f, 0.2f, 0.2f), CardSize.x - 0.2f, 0.6f, 2);

            TextMeshPro costText = CreateWorldText(front.transform, "CostText", new Vector3(-CardSize.x / 2f + 0.22f, CardSize.y / 2f - 0.22f, 0f),
                0.3f, TextAlignmentOptions.Center, new Color(0.15f, 0.35f, 0.75f), 0.4f, 0.4f, 3);

            TextMeshPro powerText = CreateWorldText(front.transform, "PowerText", new Vector3(CardSize.x / 2f - 0.22f, -CardSize.y / 2f + 0.22f, 0f),
                0.3f, TextAlignmentOptions.Center, new Color(0.75f, 0.2f, 0.15f), 0.4f, 0.4f, 3);

            GameObject back = new GameObject("Back");
            back.transform.SetParent(root.transform, false);
            SpriteRenderer backSr = back.AddComponent<SpriteRenderer>();
            backSr.sprite = placeholder;
            backSr.color = new Color(0.18f, 0.22f, 0.4f);
            backSr.sortingOrder = 0;
            back.transform.localScale = new Vector3(CardSize.x, CardSize.y, 1f);
            back.SetActive(false);

            CardNS.Card card = root.AddComponent<CardNS.Card>();
            CardNS.CardView cardView = root.GetComponent<CardNS.CardView>(); // Card의 RequireComponent가 이미 추가함
            CardDragHandler dragHandler = root.AddComponent<CardDragHandler>();

            SerializedObject soCard = new SerializedObject(card);
            soCard.FindProperty("frontRoot").objectReferenceValue = front;
            soCard.FindProperty("backRoot").objectReferenceValue = back;
            soCard.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject soView = new SerializedObject(cardView);
            soView.FindProperty("iconRenderer").objectReferenceValue = iconSr;
            soView.FindProperty("nameText").objectReferenceValue = nameText;
            soView.FindProperty("descriptionText").objectReferenceValue = descText;
            soView.FindProperty("costText").objectReferenceValue = costText;
            soView.FindProperty("powerText").objectReferenceValue = powerText;
            soView.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject soDrag = new SerializedObject(dragHandler);
            soDrag.FindProperty("dropCheckOffset").vector2Value = new Vector2(0f, -CardSize.y / 2f - 0.1f);
            soDrag.FindProperty("dropCheckRadius").floatValue = 0.35f;
            soDrag.ApplyModifiedPropertiesWithoutUndo();

            if (AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath) != null)
            {
                AssetDatabase.DeleteAsset(CardPrefabPath);
            }

            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath, out bool success);
            Object.DestroyImmediate(root);

            if (!success)
            {
                Debug.LogError("[CardGameTemplateBuilder] 카드 프리팹 저장 실패");
            }

            return prefabAsset;
        }

        private static TextMeshPro CreateWorldText(Transform parent, string name, Vector3 localPos, float fontSize,
            TextAlignmentOptions alignment, Color color, float width, float height, int sortingOrder)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;

            TextMeshPro tmp = obj.AddComponent<TextMeshPro>();
            tmp.text = name;
            tmp.fontSize = fontSize * 10f;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.rectTransform.sizeDelta = new Vector2(width, height);
            tmp.textWrappingMode = TextWrappingModes.Normal;

            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = sortingOrder;
            }

            return tmp;
        }

        private static Scene OpenTargetScene()
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ScenePath) != null)
            {
                AssetDatabase.DeleteAsset(ScenePath);
            }

            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static void PopulateScene(Scene scene, GameObject cardPrefab, List<CardNS.CardData> sampleCards, Sprite placeholder)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.12f, 0.16f);
            camObj.AddComponent<AudioListener>();

            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();

            GameObject deckObj = new GameObject("DeckManager");
            DeckManager deckManager = deckObj.AddComponent<DeckManager>();
            SerializedObject soDeck = new SerializedObject(deckManager);
            SerializedProperty listProp = soDeck.FindProperty("initialDeckList");
            listProp.arraySize = sampleCards.Count;
            for (int i = 0; i < sampleCards.Count; i++)
            {
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = sampleCards[i];
            }
            soDeck.ApplyModifiedPropertiesWithoutUndo();

            GameObject handContainerObj = new GameObject("HandContainer");
            handContainerObj.transform.position = new Vector3(0f, -3.6f, 0f);

            GameObject handObj = new GameObject("HandManager");
            HandManager handManager = handObj.AddComponent<HandManager>();
            SerializedObject soHand = new SerializedObject(handManager);
            soHand.FindProperty("cardPrefab").objectReferenceValue = cardPrefab.GetComponent<CardNS.Card>();
            soHand.FindProperty("handContainer").objectReferenceValue = handContainerObj.transform;
            soHand.ApplyModifiedPropertiesWithoutUndo();

            CreateDrawZone(new Vector3(-3.4f, 1.4f, 0f), cardPrefab, placeholder);
            CreatePlayZone(new Vector3(0f, 1.4f, 0f), placeholder);
            CreateDiscardZone(new Vector3(3.4f, 1.4f, 0f), placeholder);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        /// <summary>존 배경 스프라이트와 라벨 텍스트를 공통으로 생성한다</summary>
        private static void AddZoneBackdrop(GameObject zoneObj, Color tint, string labelText, Sprite placeholder)
        {
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(zoneObj.transform, false);
            SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = placeholder;
            sr.color = tint;
            sr.sortingOrder = -1;
            visual.transform.localScale = new Vector3(2.4f, 2.8f, 1f);

            TextMeshPro label = CreateWorldText(zoneObj.transform, "Label", new Vector3(0f, 1.55f, 0f), 0.28f,
                TextAlignmentOptions.Center, Color.white, 2.2f, 0.3f, 0);
            label.text = labelText;
        }

        private static PlayZone CreatePlayZone(Vector3 position, Sprite placeholder)
        {
            GameObject obj = new GameObject("PlayZone");
            obj.transform.position = position;

            BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(2.4f, 2.8f);

            AddZoneBackdrop(obj, new Color(0.3f, 0.7f, 0.4f, 0.35f), "Play", placeholder);

            return obj.AddComponent<PlayZone>();
        }

        private static DiscardZone CreateDiscardZone(Vector3 position, Sprite placeholder)
        {
            GameObject obj = new GameObject("DiscardZone");
            obj.transform.position = position;

            BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(2.4f, 2.8f);

            AddZoneBackdrop(obj, new Color(0.7f, 0.3f, 0.3f, 0.35f), "Discard", placeholder);

            return obj.AddComponent<DiscardZone>();
        }

        private static DrawZone CreateDrawZone(Vector3 position, GameObject cardPrefab, Sprite placeholder)
        {
            GameObject obj = new GameObject("DrawZone");
            obj.transform.position = position;

            AddZoneBackdrop(obj, new Color(0.75f, 0.65f, 0.2f, 0.35f), "Draw", placeholder);

            DrawZone zone = obj.AddComponent<DrawZone>();
            SerializedObject so = new SerializedObject(zone);
            so.FindProperty("cardPrefab").objectReferenceValue = cardPrefab.GetComponent<CardNS.Card>();
            so.ApplyModifiedPropertiesWithoutUndo();

            return zone;
        }
    }
}
