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
using UnityEngine.TextCore.LowLevel;
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

        // 카드 설명 등 한글 텍스트를 표시할 TMP 폰트 (기본 LiberationSans SDF는 한글 글리프가 없어 □로 깨짐)
        private static TMP_FontAsset koreanFontAsset;

        private static readonly string[] KoreanFontSourceCandidates =
        {
            "/System/Library/Fonts/Supplemental/AppleGothic.ttf",
            "/System/Library/Fonts/Supplemental/NotoSansGothic-Regular.ttf",
            "C:/Windows/Fonts/malgun.ttf"
        };

        /// <summary>메뉴에서 실행하는 진입점. 프리팹, 샘플 카드, 데모 씬을 전부 생성한다</summary>
        [MenuItem("Card Game Template/Build Demo Scene")]
        public static void BuildDemoScene()
        {
            AssetDatabase.Refresh();
            ImportTmpEssentialsIfNeeded();
            EnsureFolders();

            // 카드 설명 텍스트에 한글이 들어가므로, 이후 생성되는 모든 카드 텍스트가 이 폰트를 쓰도록 미리 만들어둠
            koreanFontAsset = CreateOrLoadKoreanFontAsset();

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

        /// <summary>
        /// LiberationSans SDF(기본 TMP 폰트)는 한글이 없어 카드 설명 등이 □로 깨진다.
        /// OS에 설치된 한글 폰트 파일을 프로젝트로 복사해 임포트한 뒤, Dynamic 모드 TMP 폰트 애셋을 만든다
        /// (CJK처럼 글자 수가 많은 폰트는 Static 사전 굽기보다 런타임에 필요한 글자만 그때그때 그리는 Dynamic이 표준 방식)
        /// </summary>
        private static TMP_FontAsset CreateOrLoadKoreanFontAsset()
        {
            const string assetPath = "Assets/Fonts/Korean SDF.asset";
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            string sourceFontPath = null;
            foreach (string candidate in KoreanFontSourceCandidates)
            {
                if (File.Exists(candidate))
                {
                    sourceFontPath = candidate;
                    break;
                }
            }

            if (sourceFontPath == null)
            {
                Debug.LogWarning("[CardGameTemplateBuilder] 한글 폰트 파일을 찾을 수 없음 - 기본 TMP 폰트 사용, 한글이 깨질 수 있음");
                return null;
            }

            EnsureFolder("Assets", "Fonts");
            string importedFontPath = "Assets/Fonts/" + Path.GetFileName(sourceFontPath);
            if (!File.Exists(importedFontPath))
            {
                File.Copy(sourceFontPath, importedFontPath);
                AssetDatabase.ImportAsset(importedFontPath);
            }

            Font importedFont = AssetDatabase.LoadAssetAtPath<Font>(importedFontPath);
            if (importedFont == null)
            {
                Debug.LogWarning($"[CardGameTemplateBuilder] 한글 폰트 임포트 실패: {importedFontPath} - 기본 TMP 폰트 사용");
                return null;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                importedFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            fontAsset.name = "Korean SDF";

            // 텍스처/머티리얼은 폰트 애셋의 하위 오브젝트로 같이 저장해야 나중에도 참조가 안 깨짐
            AssetDatabase.CreateAsset(fontAsset, assetPath);
            if (fontAsset.material != null)
            {
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }
            if (fontAsset.atlasTexture != null)
            {
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            }
            AssetDatabase.SaveAssets();

            return fontAsset;
        }

        private static void ImportTmpEssentialsIfNeeded()
        {
            // 이미 임포트돼있으면(TMP Settings 애셋 존재) 건너뜀
            const string marker = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(marker) != null)
            {
                return;
            }

            // Unity 6부터 TMP 본체가 com.unity.ugui 패키지에 통합됨 - 필수 리소스 unitypackage도 그 안에 들어있음
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
            // 애셋 생성 전에 필요한 폴더들을 미리 준비
            EnsureFolder("Assets", "Prefab");
            EnsureFolder("Assets", "Data");
            EnsureFolder(DataFolder, "Cards");
            EnsureFolder(DataFolder, "Generated");
            EnsureFolder("Assets", "Scenes");
        }

        private static void EnsureFolder(string parent, string name)
        {
            // 이미 있으면 아무것도 안 함
            string full = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(full))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static Sprite GetOrCreatePlaceholderSprite()
        {
            // 이미 만들어둔 게 있으면 재사용 (씬 다시 빌드할 때마다 새로 만들지 않도록)
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
            if (existing != null)
            {
                return existing;
            }

            // 실제 아트 없이도 데모가 가능하도록 8x8 흰색 정사각형 텍스처를 코드로 직접 생성
            // (색상 틴트는 SpriteRenderer.color로 입히므로 텍스처 자체는 흰색 하나만 있으면 됨)
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

            // 스프라이트를 텍스처의 하위 오브젝트로 같이 저장 - 별도 파일 안 만들고 한 파일에 다 포함됨
            AssetDatabase.CreateAsset(texture, PlaceholderSpritePath);
            AssetDatabase.AddObjectToAsset(sprite, texture);
            AssetDatabase.SaveAssets();

            return AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
        }

        private static List<CardNS.CardData> CreateSampleCardData(Sprite icon)
        {
            // 데모용 카드 10장 - 이름/코스트/공격력을 인덱스로 매칭시켜 다양하게 구성
            string[] names = { "Warrior", "Mage", "Archer", "Shieldbearer", "Assassin", "Healer", "Rogue", "Knight", "Druid", "Berserker" };
            int[] costs = { 1, 2, 1, 3, 2, 2, 1, 3, 2, 3 };
            int[] powers = { 2, 1, 3, 1, 4, 0, 2, 3, 1, 5 };

            List<CardNS.CardData> result = new List<CardNS.CardData>();

            for (int i = 0; i < names.Length; i++)
            {
                // 이미 만들어둔 애셋이 있으면 덮어써서 재사용, 없으면 새로 생성 (씬 재빌드해도 기존 참조 유지)
                string path = $"{CardDataFolder}/Card_{i:00}_{names[i]}.asset";
                CardNS.CardData data = AssetDatabase.LoadAssetAtPath<CardNS.CardData>(path);
                bool isNew = data == null;
                if (isNew)
                {
                    data = ScriptableObject.CreateInstance<CardNS.CardData>();
                }

                // CardData의 필드는 전부 private라서 SerializedObject를 거쳐야 값을 채울 수 있음
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

            // OnMouseDown/Drag/Up(CardDragHandler)이 동작하려면 콜라이더가 필수 (레거시 OnMouse* 콜백은 콜라이더 기반)
            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.size = CardSize;

            // 물리 힘은 안 받고 코드로만 위치를 옮기므로 Kinematic + 중력 0
            Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            root.AddComponent<SortingGroup>(); // 드래그/호버 시 정렬 순서를 자식 렌더러들까지 한 번에 제어하기 위함

            // 앞면(Front) - 카드 프레임/아이콘/텍스트들이 여기 자식으로 들어감
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

            // 뒷면(Back) - 앞면과 배타적으로 하나만 활성화됨 (Card.SetFaceUp이 전환 담당)
            GameObject back = new GameObject("Back");
            back.transform.SetParent(root.transform, false);
            SpriteRenderer backSr = back.AddComponent<SpriteRenderer>();
            backSr.sprite = placeholder;
            backSr.color = new Color(0.18f, 0.22f, 0.4f);
            backSr.sortingOrder = 0;
            back.transform.localScale = new Vector3(CardSize.x, CardSize.y, 1f);
            back.SetActive(false); // 기본값은 앞면이 보이는 상태이므로 뒷면은 꺼둠

            CardNS.Card card = root.AddComponent<CardNS.Card>();
            CardNS.CardView cardView = root.GetComponent<CardNS.CardView>(); // Card의 RequireComponent가 이미 추가함
            CardDragHandler dragHandler = root.AddComponent<CardDragHandler>();

            // private 필드들은 SerializedObject로만 채울 수 있음 - 위에서 만든 오브젝트/텍스트 참조들을 여기서 연결
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

            // 드롭 판정 원이 카드 하단 바깥쪽에 오도록 카드 크기 기준으로 오프셋 계산
            SerializedObject soDrag = new SerializedObject(dragHandler);
            soDrag.FindProperty("dropCheckOffset").vector2Value = new Vector2(0f, -CardSize.y / 2f - 0.1f);
            soDrag.FindProperty("dropCheckRadius").floatValue = 0.35f;
            soDrag.ApplyModifiedPropertiesWithoutUndo();

            // 기존 프리팹이 있으면 지우고 새로 저장 (참조 GUID를 유지하려고 덮어쓰기가 아니라 삭제 후 재생성 방식 사용)
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath) != null)
            {
                AssetDatabase.DeleteAsset(CardPrefabPath);
            }

            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath, out bool success);
            Object.DestroyImmediate(root); // 씬에 남은 임시 인스턴스는 프리팹 저장 후 정리

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
            if (koreanFontAsset != null)
            {
                tmp.font = koreanFontAsset; // 한글 폰트 적용 (카드 설명 등 한글이 들어가는 텍스트 전부에 통일 적용)
            }

            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = sortingOrder;
            }

            return tmp;
        }

        private static Scene OpenTargetScene()
        {
            // 기존 씬 파일이 있으면 지우고 완전히 새 빈 씬으로 시작 (덮어쓰기가 아니라 삭제 후 재생성)
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ScenePath) != null)
            {
                AssetDatabase.DeleteAsset(ScenePath);
            }

            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static void PopulateScene(Scene scene, GameObject cardPrefab, List<CardNS.CardData> sampleCards, Sprite placeholder)
        {
            // 2D 탑다운 시점 카메라 - z를 -10만큼 뒤로 빼야 z=0 평면의 카드들이 카메라 앞에 보임
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

            // 덱 매니저에 샘플 카드 10장을 초기 덱 목록으로 등록
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

            // 손패 카드들이 배치될 부모 겸 기준점 - 화면 하단에 위치
            GameObject handContainerObj = new GameObject("HandContainer");
            handContainerObj.transform.position = new Vector3(0f, -3.6f, 0f);

            GameObject handObj = new GameObject("HandManager");
            HandManager handManager = handObj.AddComponent<HandManager>();
            SerializedObject soHand = new SerializedObject(handManager);
            soHand.FindProperty("cardPrefab").objectReferenceValue = cardPrefab.GetComponent<CardNS.Card>();
            soHand.FindProperty("handContainer").objectReferenceValue = handContainerObj.transform;
            soHand.ApplyModifiedPropertiesWithoutUndo();

            // 세 존을 가로로 나란히 배치: 왼쪽 Draw, 가운데 Play, 오른쪽 Discard
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
