using System.IO;
using InventoryTemplate.Equipment;
using InventoryTemplate.Inventory;
using InventoryTemplate.Item;
using InventoryTemplate.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace InventoryTemplate.EditorTools
{
    /// <summary>
    /// "Inventory Template > Build Demo Scene" 메뉴 실행 시 데모 씬 전체를 자동 생성하는 에디터 툴
    /// </summary>
    public static class InventorySceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/InventoryDemo.unity";
        private const string ItemDataFolder = "Assets/Data/Items";
        private const string PrefabFolder = "Assets/Prefabs";
        private const string SlotPrefabPath = PrefabFolder + "/SlotUI.prefab";

        private const float SlotSize = 80f;
        private const float SlotSpacing = 8f;
        private const int GridColumns = 4;
        private const int GridRows = 5;

        private static TMP_FontAsset koreanFontAsset;

        // 개발 중 빠르게 테스트할 때 쓰는 도구용 메뉴 - 데모 씬 열고 바로 Play까지 한 번에 실행
        [MenuItem("Inventory Template/Debug/Open And Play Demo Scene")]
        public static void OpenAndPlayDemoScene()
        {
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Inventory Template/Build Demo Scene")]
        public static void BuildDemoScene()
        {
            // TMP 기본 리소스/폴더가 없으면 미리 준비 (없어도 진행은 되지만 경고만 남김)
            EnsureTMPEssentialResources();
            EnsureFolder(ItemDataFolder);
            EnsureFolder(PrefabFolder);

            // 완전히 새 빈 씬에서 시작 (기존 씬 내용은 버림)
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 이후 생성되는 모든 TMP 텍스트가 이 폰트를 쓰도록 미리 만들어둠 (한글 깨짐 방지)
            koreanFontAsset = CreateOrLoadKoreanFontAsset();

            // 씬의 기본 뼈대부터 순서대로 생성: 카메라 → 이벤트 시스템 → 캔버스
            CreateMainCamera();
            CreateEventSystem();
            Canvas rootCanvas = CreateRootCanvas();

            // 그리드에서 재사용할 슬롯 프리팹을 먼저 만들어둠
            InventorySlotUI slotPrefab = BuildSlotPrefabAsset();

            // 인벤토리 매니저 오브젝트 생성 (mainGrid 연결은 그리드 만든 다음에)
            GameObject managerGO = new GameObject("InventoryManager");
            InventoryManager manager = managerGO.AddComponent<InventoryManager>();

            // 메인 인벤토리 그리드(우측 하단) 생성
            InventoryGrid grid = BuildInventoryPanel(rootCanvas.transform, slotPrefab);

            // 장비 슬롯 패널(좌측 하단, Weapon/Armor) 생성
            BuildEquipmentPanel(rootCanvas.transform);

            // 드래그 아이콘이 다른 UI 위에 그려지도록 최상단 레이어 생성
            BuildDragLayer(rootCanvas.transform);

            // 우클릭 컨텍스트 메뉴(사용/버리기) 생성
            BuildContextMenu(rootCanvas.transform);

            // InventoryManager의 private 필드는 SerializedObject를 통해서만 인스펙터처럼 채울 수 있음
            SerializedObject managerSO = new SerializedObject(manager);
            managerSO.FindProperty("mainGrid").objectReferenceValue = grid;
            managerSO.ApplyModifiedPropertiesWithoutUndo();

            // 데모용 아이템 6종 생성 (소모품 3종 + 장비 3종)
            ItemData healthPotion = CreateDemoItem(1, "체력 물약", "HP를 회복하는 소모품 (실제 효과는 미구현)",
                new Color(0.85f, 0.2f, 0.2f), 10, ItemType.Consumable, EquipmentSlotType.None);
            ItemData manaPotion = CreateDemoItem(2, "마나 물약", "MP를 회복하는 소모품 (실제 효과는 미구현)",
                new Color(0.2f, 0.4f, 0.9f), 10, ItemType.Consumable, EquipmentSlotType.None);
            ItemData bread = CreateDemoItem(3, "빵", "허기를 채우는 소모품 (실제 효과는 미구현)",
                new Color(0.7f, 0.5f, 0.25f), 10, ItemType.Consumable, EquipmentSlotType.None);
            ItemData ironSword = CreateDemoItem(4, "철 검", "공격력을 올려주는 무기 (실제 효과는 미구현)",
                new Color(0.75f, 0.75f, 0.8f), 1, ItemType.Weapon, EquipmentSlotType.Weapon);
            ItemData leatherArmor = CreateDemoItem(5, "가죽 갑옷", "방어력을 올려주는 갑옷 (실제 효과는 미구현)",
                new Color(0.3f, 0.6f, 0.3f), 1, ItemType.Equipment, EquipmentSlotType.Armor);
            ItemData ironHelmet = CreateDemoItem(6, "철 투구", "방어력을 올려주는 투구 (실제 효과는 미구현)",
                new Color(0.8f, 0.7f, 0.2f), 1, ItemType.Equipment, EquipmentSlotType.Head);

            // 시작하자마자 인벤토리에 몇 개 미리 채워 넣을 데모 시더 생성
            GameObject seederGO = new GameObject("DemoSeeder");
            InventoryDemoSeeder seeder = seederGO.AddComponent<InventoryDemoSeeder>();
            SerializedObject seederSO = new SerializedObject(seeder);
            SerializedProperty seedArray = seederSO.FindProperty("seedItems");
            seedArray.arraySize = 4; // 체력물약5, 마나물약3, 철검1, 가죽갑옷1 - 총 4개 항목
            SetSeedEntry(seedArray, 0, healthPotion, 5);
            SetSeedEntry(seedArray, 1, manaPotion, 3);
            SetSeedEntry(seedArray, 2, ironSword, 1);
            SetSeedEntry(seedArray, 3, leatherArmor, 1);
            seederSO.ApplyModifiedPropertiesWithoutUndo();

            // 새로 만든 애셋(아이템 데이터, 폰트, 프리팹 등)을 디스크에 저장
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 씬도 "변경됨" 표시하고 디스크에 저장해야 다음에 열었을 때 내용이 남아있음
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log($"인벤토리 데모 씬 생성 완료: {ScenePath} (아이템 6종, 데모용 미리 채움 4종)");
        }

        // ── 폴더/리소스 준비 ──────────────────────────────

        private static void EnsureFolder(string path)
        {
            // 이미 있으면 아무것도 안 함
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            string folderName = Path.GetFileName(path);
            // 부모 폴더도 없으면 재귀적으로 먼저 만듦 (예: Assets/A/B면 Assets/A부터)
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private const string TMPEssentialMarker = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        /// <summary>TMP가 처음 임포트된 프로젝트라면 Essential Resources(기본 폰트)가 있는지 확인만 하고, 없으면 경고만 남긴다</summary>
        private static void EnsureTMPEssentialResources()
        {
            if (File.Exists(TMPEssentialMarker)) return;

            Debug.LogWarning("[InventorySceneBuilder] TMP Essential Resources가 아직 임포트되지 않음. " +
                "먼저 메뉴 Inventory Template > Import TMP Essentials 를 실행하거나 " +
                "Window > TextMeshPro > Import TMP Essential Resources를 수동 실행할 것. " +
                "임포트 전에는 텍스트에 기본 폰트가 비어 보일 수 있음.");
        }

        /// <summary>
        /// TMP Essential Resources를 임포트하고 에디터를 종료하는 배치모드 전용 진입점.
        /// AssetDatabase.ImportPackage는 비동기이므로 -quit과 함께 한 번에 실행하면 완료 전에 종료돼버린다.
        /// 그래서 이 메서드를 -quit 없이 먼저 실행해 임포트 완료 콜백에서 직접 종료한다.
        /// </summary>
        [MenuItem("Inventory Template/Import TMP Essentials")]
        public static void ImportTMPEssentialsAndExit()
        {
            if (File.Exists(TMPEssentialMarker))
            {
                Debug.Log("[InventorySceneBuilder] TMP Essential Resources 이미 존재함 - 건너뜀");
                EditorApplication.Exit(0);
                return;
            }

            // Unity 6부터 TMP 본체가 com.unity.ugui 패키지에 통합됨 (com.unity.textmeshpro는 빈 shim)
            string[] candidates = Directory.Exists("Library/PackageCache")
                ? Directory.GetDirectories("Library/PackageCache", "com.unity.ugui*")
                : new string[0];

            string pkgPath = candidates.Length > 0
                ? Path.Combine(candidates[0], "Package Resources", "TMP Essential Resources.unitypackage")
                : null;

            if (pkgPath == null || !File.Exists(pkgPath))
            {
                Debug.LogError($"[InventorySceneBuilder] TMP Essential Resources unitypackage를 찾을 수 없음: {pkgPath}");
                EditorApplication.Exit(1);
                return;
            }

            AssetDatabase.importPackageCompleted += OnTMPImportCompleted;
            AssetDatabase.importPackageFailed += OnTMPImportFailed;
            AssetDatabase.ImportPackage(pkgPath, false);
        }

        private static void OnTMPImportCompleted(string packageName)
        {
            Debug.Log("[InventorySceneBuilder] TMP Essential Resources 임포트 완료");
            AssetDatabase.SaveAssets();
            EditorApplication.Exit(0);
        }

        private static void OnTMPImportFailed(string packageName, string errorMessage)
        {
            Debug.LogError($"[InventorySceneBuilder] TMP Essential Resources 임포트 실패: {errorMessage}");
            EditorApplication.Exit(1);
        }

        // ── 씬 뼈대 (Camera / EventSystem / Canvas) ──────────────

        private static void CreateMainCamera()
        {
            // Screen Space Overlay UI는 카메라 없어도 그려지지만, 카메라가 없으면 Game 뷰 배경이
            // 새까맣게 나와서 보기 안 좋음 - 단색 배경용으로만 카메라 하나 둠
            GameObject camGO = new GameObject("Main Camera", typeof(Camera));
            camGO.tag = "MainCamera";
            Camera cam = camGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f, 1f);
            cam.orthographic = true;
        }

        private static void CreateEventSystem()
        {
            // uGUI 드래그/클릭 이벤트(IPointerClickHandler, IBeginDragHandler 등)가 동작하려면
            // 씬에 EventSystem이 반드시 하나 있어야 함. StandaloneInputModule은 레거시 Input 기반
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static readonly string[] KoreanFontSourceCandidates =
        {
            "/System/Library/Fonts/Supplemental/AppleGothic.ttf",
            "/System/Library/Fonts/Supplemental/NotoSansGothic-Regular.ttf",
            "C:/Windows/Fonts/malgun.ttf"
        };

        /// <summary>
        /// LiberationSans SDF(기본 TMP 폰트)는 한글이 없어 버튼 라벨 등이 □로 깨진다.
        /// OS에 설치된 한글 폰트 파일을 프로젝트로 복사해 임포트한 뒤, 필요한 글자만 정적으로 구워 TMP 폰트 애셋을 만든다.
        /// (Font.CreateDynamicFontFromOSFont + AtlasPopulationMode.DynamicOS는 배치모드에서 폰트 페이스를 못 읽어 실패함)
        /// </summary>
        private static TMP_FontAsset CreateOrLoadKoreanFontAsset()
        {
            const string assetPath = "Assets/Fonts/Korean SDF.asset";
            // 이미 만들어둔 게 있으면 재사용 (씬 다시 빌드할 때마다 새로 굽지 않도록)
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;

            // OS에 깔려있는 한글 지원 폰트 파일 후보들을 순서대로 찾아봄 (Mac/Windows 둘 다 커버)
            string sourceFontPath = null;
            foreach (string candidate in KoreanFontSourceCandidates)
            {
                if (File.Exists(candidate)) { sourceFontPath = candidate; break; }
            }

            if (sourceFontPath == null)
            {
                // 못 찾으면 그냥 기본 폰트로 진행 (한글은 깨지지만 빌드 자체는 안 막음)
                Debug.LogWarning("[InventorySceneBuilder] 한글 폰트 파일을 찾을 수 없음 - 기본 TMP 폰트 사용, 한글이 깨질 수 있음");
                return null;
            }

            // 유니티 프로젝트(Assets 폴더) 안으로 폰트 파일을 복사해야 AssetDatabase가 Font로 임포트해줌
            EnsureFolder("Assets/Fonts");
            string importedFontPath = "Assets/Fonts/" + Path.GetFileName(sourceFontPath);
            if (!File.Exists(importedFontPath))
            {
                File.Copy(sourceFontPath, importedFontPath);
                AssetDatabase.ImportAsset(importedFontPath);
            }

            Font importedFont = AssetDatabase.LoadAssetAtPath<Font>(importedFontPath);
            if (importedFont == null)
            {
                Debug.LogWarning($"[InventorySceneBuilder] 한글 폰트 임포트 실패: {importedFontPath} - 기본 TMP 폰트 사용");
                return null;
            }

            // CJK처럼 글자 수가 많은 폰트는 Static 사전 굽기보다 Dynamic(런타임에 필요한 글자만 그때그때 래스터라이즈)이 표준 방식
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                importedFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            fontAsset.name = "Korean SDF";

            // 폰트 애셋 저장 - 텍스처/머티리얼은 폰트 애셋의 하위 오브젝트로 같이 저장해야 나중에도 참조가 안 깨짐
            AssetDatabase.CreateAsset(fontAsset, assetPath);
            if (fontAsset.material != null) AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            if (fontAsset.atlasTexture != null) AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            AssetDatabase.SaveAssets();

            return fontAsset;
        }

        private static Canvas CreateRootCanvas()
        {
            // Screen Space Overlay - 인벤토리는 월드가 아니라 화면 위에 고정으로 그려지는 UI 레이어
            GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // 해상도가 달라져도 UI 크기 비율이 유지되도록 스케일 설정
            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f; // 너비/높이 중간값 기준으로 스케일 (가로세로 어느 한쪽만 안 따짐)

            return canvas;
        }

        // ── 슬롯 프리팹 ──────────────────────────────────

        private static InventorySlotUI BuildSlotPrefabAsset()
        {
            // 슬롯 하나의 모양(배경+아이콘+텍스트)을 만들고 필요한 컴포넌트를 붙임
            GameObject go = CreateSlotSkeleton("SlotUI", out Image icon, out TMP_Text text);
            InventorySlotUI slotUI = go.AddComponent<InventorySlotUI>();
            go.AddComponent<SlotDragHandler>(); // 드래그 시작/중/끝 처리
            go.AddComponent<SlotDropHandler>(); // 드롭 받았을 때 이동/합치기/교체 처리

            // private 필드(iconImage, stackText)는 SerializedObject로만 채울 수 있음
            SerializedObject so = new SerializedObject(slotUI);
            so.FindProperty("iconImage").objectReferenceValue = icon;
            so.FindProperty("stackText").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 씬에 만든 임시 오브젝트를 프리팹 애셋으로 저장한 뒤, 씬에 남은 임시본은 지움
            // (InventoryGrid가 런타임에 이 프리팹을 Instantiate해서 슬롯을 20개 찍어냄)
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, SlotPrefabPath);
            Object.DestroyImmediate(go);

            return prefab.GetComponent<InventorySlotUI>();
        }

        /// <summary>배경 + 아이콘 + 스택 텍스트로 구성된 슬롯 기본 골격을 생성한다 (부모 미지정 시 씬 루트에 생성)</summary>
        private static GameObject CreateSlotSkeleton(string name, out Image iconImage, out TMP_Text stackText)
        {
            // 배경 이미지 - 이 Image가 곧 슬롯 클릭/드래그/드롭 대상이 되는 Graphic
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Image bg = go.GetComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(SlotSize, SlotSize);

            // 아이콘 - 배경 안쪽에 약간 여백(6px)을 두고 꽉 채움
            GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGO.transform.SetParent(go.transform, false);
            iconImage = iconGO.GetComponent<Image>();
            iconImage.raycastTarget = false; // 아이콘이 클릭/드래그 이벤트를 가로채면 안 되므로 꺼둠
            iconImage.enabled = false; // 처음엔 빈 슬롯이므로 숨김 (InventorySlotUI.Refresh가 나중에 켬)
            RectTransform iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = Vector2.zero;
            iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = new Vector2(6, 6);
            iconRT.offsetMax = new Vector2(-6, -6);

            // 스택 개수 텍스트 - 우측 하단에 표시
            GameObject textGO = new GameObject("StackText", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            stackText = textGO.AddComponent<TextMeshProUGUI>();
            stackText.alignment = TextAlignmentOptions.BottomRight;
            stackText.fontSize = 22;
            stackText.raycastTarget = false;
            stackText.enabled = false; // 마찬가지로 처음엔 숨김
            if (koreanFontAsset != null) stackText.font = koreanFontAsset; // 한글 폰트 적용 (숫자만 표시되지만 통일)
            RectTransform textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(4, 2);
            textRT.offsetMax = new Vector2(-4, -2);

            return go;
        }

        // ── 인벤토리 패널 (그리드) ──────────────────────────

        private static InventoryGrid BuildInventoryPanel(Transform canvasTransform, InventorySlotUI slotPrefab)
        {
            // 반투명 검은 배경 패널 - 화면 우측 하단에 고정
            GameObject panelGO = new GameObject("InventoryPanel", typeof(RectTransform), typeof(Image));
            panelGO.transform.SetParent(canvasTransform, false);
            Image panelBg = panelGO.GetComponent<Image>();
            panelBg.color = new Color(0f, 0f, 0f, 0.5f);

            RectTransform panelRT = panelGO.GetComponent<RectTransform>();
            // anchor/pivot을 (1,0)으로 잡으면 화면의 우측 하단 기준으로 배치됨
            panelRT.anchorMin = new Vector2(1f, 0f);
            panelRT.anchorMax = new Vector2(1f, 0f);
            panelRT.pivot = new Vector2(1f, 0f);
            // 패널 크기 = 칸 크기*개수 + 칸 사이 간격들 (여백 포함해서 정확히 딱 맞게 계산)
            float panelWidth = GridColumns * SlotSize + (GridColumns + 1) * SlotSpacing;
            float panelHeight = GridRows * SlotSize + (GridRows + 1) * SlotSpacing;
            panelRT.sizeDelta = new Vector2(panelWidth, panelHeight);
            panelRT.anchoredPosition = new Vector2(-40, 40); // 화면 가장자리에서 40px 여백

            // 실제 슬롯들이 배치될 컨테이너 - GridLayoutGroup이 알아서 격자로 정렬해줌
            GameObject containerGO = new GameObject("SlotContainer", typeof(RectTransform), typeof(GridLayoutGroup));
            containerGO.transform.SetParent(panelGO.transform, false);
            RectTransform containerRT = containerGO.GetComponent<RectTransform>();
            containerRT.anchorMin = Vector2.zero;
            containerRT.anchorMax = Vector2.one;
            containerRT.offsetMin = Vector2.zero;
            containerRT.offsetMax = Vector2.zero;

            GridLayoutGroup layout = containerGO.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(SlotSize, SlotSize);
            layout.spacing = new Vector2(SlotSpacing, SlotSpacing);
            layout.padding = new RectOffset((int)SlotSpacing, (int)SlotSpacing, (int)SlotSpacing, (int)SlotSpacing);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; // 열 개수를 고정해서 줄바꿈되게 함
            layout.constraintCount = GridColumns;

            // InventoryGrid 컴포넌트를 붙이고 인스펙터 값들을 채워줌 (여기서 슬롯 프리팹/컨테이너를 연결)
            InventoryGrid grid = panelGO.AddComponent<InventoryGrid>();
            SerializedObject so = new SerializedObject(grid);
            so.FindProperty("columns").intValue = GridColumns;
            so.FindProperty("rows").intValue = GridRows;
            so.FindProperty("slotUIPrefab").objectReferenceValue = slotPrefab;
            so.FindProperty("gridParent").objectReferenceValue = containerRT;
            so.ApplyModifiedPropertiesWithoutUndo();

            return grid;
        }

        // ── 장비 슬롯 패널 ──────────────────────────────────

        private static void BuildEquipmentPanel(Transform canvasTransform)
        {
            // 화면 좌측 하단에 고정되는 패널, 슬롯 2개(Weapon/Armor)가 나란히 들어감
            GameObject panelGO = new GameObject("EquipmentPanel", typeof(RectTransform), typeof(Image));
            panelGO.transform.SetParent(canvasTransform, false);
            Image panelBg = panelGO.GetComponent<Image>();
            panelBg.color = new Color(0f, 0f, 0f, 0.5f);

            RectTransform panelRT = panelGO.GetComponent<RectTransform>();
            // anchor/pivot (0,0) = 화면 좌측 하단 기준
            panelRT.anchorMin = new Vector2(0f, 0f);
            panelRT.anchorMax = new Vector2(0f, 0f);
            panelRT.pivot = new Vector2(0f, 0f);
            panelRT.sizeDelta = new Vector2(SlotSize * 2 + SlotSpacing * 3, SlotSize + SlotSpacing * 2);
            panelRT.anchoredPosition = new Vector2(40, 40);

            // 이 패널은 GridLayoutGroup을 안 쓰고 좌표를 직접 지정 (슬롯이 딱 2개뿐이라 굳이 필요 없음)
            CreateEquipmentSlot(panelRT, "WeaponSlot", EquipmentSlotType.Weapon,
                new Vector2(SlotSpacing, SlotSpacing));
            CreateEquipmentSlot(panelRT, "ArmorSlot", EquipmentSlotType.Armor,
                new Vector2(SlotSpacing * 2 + SlotSize, SlotSpacing));
        }

        private static void CreateEquipmentSlot(Transform parent, string name, EquipmentSlotType slotType, Vector2 anchoredPos)
        {
            // 일반 슬롯과 똑같은 골격(배경+아이콘+텍스트)을 만든 뒤 EquipmentSlot 컴포넌트로 특화시킴
            GameObject go = CreateSlotSkeleton(name, out Image icon, out TMP_Text text);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = anchoredPos;

            // 일반 인벤토리 슬롯과 색을 다르게 해서 구분되게 함
            Image bg = go.GetComponent<Image>();
            bg.color = new Color(0.2f, 0.18f, 0.1f, 1f);

            // InventorySlotUI 대신 EquipmentSlot(상속 클래스)을 붙임 - CanAcceptItem이 타입 체크하도록 override됨
            EquipmentSlot equipSlot = go.AddComponent<EquipmentSlot>();
            go.AddComponent<SlotDragHandler>();
            go.AddComponent<SlotDropHandler>();

            SerializedObject so = new SerializedObject(equipSlot);
            so.FindProperty("iconImage").objectReferenceValue = icon;
            so.FindProperty("stackText").objectReferenceValue = text;
            so.FindProperty("slotType").enumValueIndex = (int)slotType; // 이 슬롯이 받아들일 장비 종류 지정
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── 드래그 최상단 레이어 ──────────────────────────────

        private static void BuildDragLayer(Transform canvasTransform)
        {
            // 캔버스의 "마지막 자식"으로 넣어야 형제 오브젝트들 중 제일 위에 그려짐
            // (다른 패널들을 이미 다 만든 다음에 이 함수를 호출하기 때문에 자동으로 마지막 순서가 됨)
            GameObject go = new GameObject("DragLayer", typeof(RectTransform));
            go.transform.SetParent(canvasTransform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            // 화면 전체를 덮도록 꽉 채움 (드래그 아이콘이 어디로 움직이든 이 안에서 그려지게)
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            DragLayer dragLayer = go.AddComponent<DragLayer>();
            SerializedObject so = new SerializedObject(dragLayer);
            so.FindProperty("root").objectReferenceValue = rt;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── 우클릭 컨텍스트 메뉴 ──────────────────────────────

        private static void BuildContextMenu(Transform canvasTransform)
        {
            // 버튼 2개(사용/버리기) + 위아래 패딩 + 버튼 사이 간격을 다 더해서 패널 높이를 정확히 계산
            // (하드코딩해서 버튼이 패널 밖으로 삐져나오는 실수를 방지)
            const float buttonHeight = 36f;
            const float buttonGap = 6f;
            const float padding = 8f;
            const float panelHeight = padding * 2 + buttonHeight * 2 + buttonGap;

            GameObject panelGO = new GameObject("ContextMenu", typeof(RectTransform), typeof(Image));
            panelGO.transform.SetParent(canvasTransform, false);
            Image bg = panelGO.GetComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            RectTransform panelRT = panelGO.GetComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0f, 0f);
            panelRT.anchorMax = new Vector2(0f, 0f);
            panelRT.pivot = new Vector2(0f, 1f); // 좌상단 기준 - Show()에서 우클릭 지점에 이 좌상단을 맞춤
            panelRT.sizeDelta = new Vector2(140, panelHeight);

            // 버튼 두 개를 위에서 아래로 순서대로 배치
            Button useButton = CreateMenuButton(panelRT, "UseButton", "사용", new Vector2(0, -padding));
            Button discardButton = CreateMenuButton(panelRT, "DiscardButton", "버리기",
                new Vector2(0, -(padding + buttonHeight + buttonGap)));

            ContextMenuUI menu = panelGO.AddComponent<ContextMenuUI>();
            SerializedObject so = new SerializedObject(menu);
            so.FindProperty("panelRect").objectReferenceValue = panelRT;
            so.FindProperty("useButton").objectReferenceValue = useButton;
            so.FindProperty("discardButton").objectReferenceValue = discardButton;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button CreateMenuButton(Transform parent, string name, string label, Vector2 anchoredPos)
        {
            // 버튼 배경 + Button 컴포넌트 (클릭 이벤트는 ContextMenuUI가 나중에 등록)
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(140, 36);
            rt.anchoredPosition = anchoredPos;

            // 버튼 위에 올라갈 라벨 텍스트 ("사용" / "버리기")
            GameObject textGO = new GameObject("Label", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            TMP_Text text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 20;
            text.raycastTarget = false; // 텍스트가 버튼 클릭을 가로채면 안 되므로 꺼둠
            if (koreanFontAsset != null) text.font = koreanFontAsset; // 한글 라벨이라 반드시 한글 폰트 필요
            RectTransform textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            return go.GetComponent<Button>();
        }

        // ── 데모 아이템 데이터 ──────────────────────────────

        private static ItemData CreateDemoItem(int id, string name, string description, Color color,
            int maxStackSize, ItemType type, EquipmentSlotType equipSlotType)
        {
            // ItemData 애셋을 하나 새로 만들어 디스크에 저장
            ItemData asset = ScriptableObject.CreateInstance<ItemData>();
            string path = $"{ItemDataFolder}/{name}.asset";
            AssetDatabase.CreateAsset(asset, path);

            // 실제 이미지 파일 없이도 데모가 가능하도록, 색만 다른 64x64 단색 텍스처를 코드로 직접 생성
            Texture2D tex = new Texture2D(64, 64);
            Color[] pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            tex.name = name + "_Tex";

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
            sprite.name = name + "_Icon";

            // 텍스처/스프라이트를 ItemData 애셋의 "하위 오브젝트"로 같이 저장 - 별도 파일 안 만들고 한 파일에 다 포함됨
            AssetDatabase.AddObjectToAsset(tex, asset);
            AssetDatabase.AddObjectToAsset(sprite, asset);

            // ItemData의 필드가 전부 private라서 SerializedObject를 거쳐야 값을 채울 수 있음
            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("itemId").intValue = id;
            so.FindProperty("itemName").stringValue = name;
            so.FindProperty("description").stringValue = description;
            so.FindProperty("icon").objectReferenceValue = sprite;
            so.FindProperty("maxStackSize").intValue = maxStackSize;
            so.FindProperty("itemType").enumValueIndex = (int)type;
            so.FindProperty("equipSlotType").enumValueIndex = (int)equipSlotType;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(asset); // 변경사항이 있다고 표시해야 SaveAssets 때 디스크에 반영됨
            return asset;
        }

        private static void SetSeedEntry(SerializedProperty array, int index, ItemData item, int count)
        {
            // InventoryDemoSeeder.SeedEntry 구조체 배열의 index번째 항목에 item/count를 채워 넣음
            SerializedProperty element = array.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("item").objectReferenceValue = item;
            element.FindPropertyRelative("count").intValue = count;
        }
    }
}
