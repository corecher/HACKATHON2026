# 인벤토리 / 아이템 슬롯 시스템 템플릿

Unity 2D 프로젝트용 재사용 가능한 인벤토리 모듈. 아이템이 슬롯에 저장되고, 드래그로 이동/스택/장착되는 "틀"만 제공하며 실제 아이템 효과(회복, 버프 등)는 구현하지 않음 - 이후 이벤트를 구독해서 붙이도록 훅만 열어둠.

## 빠른 시작

1. Unity Editor에서 프로젝트 열기
2. 메뉴 `Inventory Template > Build Demo Scene` 실행 → `Assets/Scenes/InventoryDemo.unity` 자동 생성
3. 생성된 씬 열고 Play 버튼
4. (테스트용 단축 메뉴: `Inventory Template > Debug > Open And Play Demo Scene` - 씬 열기 + Play를 한 번에)

### Play 후 확인 가능한 것
- 화면 우측 하단: 4x5 메인 인벤토리 그리드, 시작하자마자 체력물약x5 / 마나물약x3 / 철검x1 / 가죽갑옷x1이 미리 채워져 있음
- 화면 좌측 하단: 장비 슬롯 2칸 (Weapon / Armor)
- 아이템을 드래그해서 슬롯 간 이동, 같은 아이템끼리 겹치면 스택 병합, 다른 아이템끼리는 위치 교체
- 철검을 Weapon 슬롯에 드래그하면 장착됨 (Console에 장착 이벤트 확인 가능)
- 가죽갑옷을 Weapon 슬롯에 드래그하면 타입이 안 맞아 장착 거부됨
- 슬롯 우클릭 → "사용"/"버리기" 메뉴 (사용은 로그만 출력, 버리기는 실제로 슬롯을 비움)

## 폴더 구조

```
Assets/Scripts/
├── Item/
│   ├── ItemType.cs          아이템 종류(ItemType), 장비 슬롯 종류(EquipmentSlotType) enum
│   └── ItemData.cs          아이템 정의 ScriptableObject
├── Inventory/
│   ├── InventorySlot.cs     슬롯 데이터 (순수 C# 클래스, MonoBehaviour 아님)
│   ├── InventoryGrid.cs     여러 슬롯을 격자로 관리, 런타임에 슬롯 UI 자동 생성
│   ├── InventoryManager.cs  싱글톤 - 아이템 추가/제거/개수 조회
│   └── InventoryDemoSeeder.cs  (데모 전용) 시작 시 아이템 몇 개 미리 채워 넣기
├── Equipment/
│   └── EquipmentSlot.cs     특정 장비 타입만 받는 슬롯 (InventorySlotUI 상속)
├── UI/
│   ├── InventorySlotUI.cs   슬롯 하나의 시각적 표현 (아이콘 + 스택 텍스트)
│   ├── SlotDragHandler.cs   드래그 시작/중/끝 - 최상단 레이어에 아이콘 복제해서 마우스 따라가게 함
│   ├── SlotDropHandler.cs   드롭 처리 - 이동/스택 합치기/교체 로직
│   ├── DragLayer.cs         드래그 중인 아이콘이 그려질 최상단 레이어
│   └── ContextMenuUI.cs     우클릭 메뉴 (사용/버리기)
└── Editor/
    └── InventorySceneBuilder.cs   데모 씬 전체를 자동 생성하는 에디터 툴
```

## 스크립트별 기능 설명

### ItemData (ScriptableObject)
아이템 하나의 정의. 필드: `itemId`, `itemName`, `description`, `icon`, `maxStackSize`, `itemType`(Weapon/Consumable/Material/Equipment), `equipSlotType`(None/Head/Weapon/Armor).
실제 효과는 구현하지 않고 `OnUseEffectHook` 이벤트 + `Use(GameObject)` 메서드만 제공 - 나중에 이 이벤트를 구독해서 회복/버프 등을 붙이면 됨.
인스펙터 메뉴: `Inventory Template/Item Data`로 새 아이템 애셋 생성 가능.

### InventorySlot (순수 데이터 클래스)
`Item`(ItemData 참조), `CurrentStack`(개수)만 들고 있음. `SetItem`, `Clear`, `CanStackWith`, `AddStack` 메서드 제공. MonoBehaviour가 아니라서 어디에도 붙이지 않고 그냥 `new InventorySlot()`으로 생성해서 사용.

### InventorySlotUI (MonoBehaviour)
슬롯 하나의 시각적 표현. `iconImage`(Image), `stackText`(TMP_Text) 두 개의 인스펙터 참조가 필요. `Bind()`로 데이터와 연결하고 `Refresh()`로 화면을 갱신. `CanAcceptItem`/`PlaceItem`/`ClearSlot`은 `virtual`이라 `EquipmentSlot`이 오버라이드해서 타입 체크 + 장착 이벤트를 추가함.
우클릭(`IPointerClickHandler`) 시 `ContextMenuUI.Instance.Show()` 호출.

### InventoryGrid (MonoBehaviour)
`columns`, `rows`, `slotUIPrefab`, `gridParent`를 인스펙터에서 설정. `Awake()`에서 `rows x columns`개만큼 `InventorySlot` 데이터 + `InventorySlotUI` 프리팹 인스턴스를 만들어 `Bind()`. `FindStackableSlot`, `FindEmptySlot`으로 빈 자리/합칠 자리를 찾아줌.

### InventoryManager (싱글톤 MonoBehaviour)
`mainGrid` 인스펙터 참조 하나만 필요. `AddItem(item, count)`은 기존 스택 우선 병합 → 빈 슬롯 사용 → 그래도 못 넣으면 `OnInventoryFull` 이벤트 발생 후 `false` 반환. `RemoveItem`, `GetItemCount` 유틸리티 제공. 씬에 하나만 있으면 되므로 `DontDestroyOnLoad`는 사용하지 않음(단일 씬 프로토타입이라 불필요).

### SlotDragHandler (MonoBehaviour)
`IBeginDragHandler`/`IDragHandler`/`IEndDragHandler` 구현. 드래그 시작 시 `DragLayer` 밑에 아이콘을 복제 생성해서 마우스를 따라가게 하고, 드래그 끝나면 복제 아이콘을 지움. 실제 아이템 이동 로직은 여기 없고 `SlotDropHandler`가 처리.

### SlotDropHandler (MonoBehaviour)
`IDropHandler` 구현. `eventData.pointerDrag`(유니티가 자동으로 넘겨주는 드래그 시작 오브젝트)를 받아서:
- 대상이 비어있으면 → 이동
- 같은 아이템이고 안 찼으면 → 스택 병합 (넘치는 만큼은 원본 슬롯에 남김)
- 다른 아이템이면 → 자리 교체 (단, 양쪽 다 서로를 받아들일 수 있어야 함 - 장비 슬롯 타입 체크 포함)

### EquipmentSlot (InventorySlotUI 상속)
`slotType`(Weapon/Armor/Head 등) 인스펙터 값 하나 추가. `CanAcceptItem`이 `item.EquipSlotType == slotType`인지 체크. `PlaceItem`/`ClearSlot`을 오버라이드해서 각각 정적 이벤트 `OnItemEquipped`/`OnItemUnequipped`를 발생시킴 - 스탯 시스템은 이 이벤트만 구독하면 됨.

### ContextMenuUI (싱글톤 MonoBehaviour)
`panelRect`, `useButton`, `discardButton` 인스펙터 참조 필요. `Show(slotUI, screenPosition)`으로 특정 슬롯 기준 특정 좌표에 메뉴를 띄움. 사용 버튼은 `Debug.Log`만 출력(실제 효과 없음), 버리기 버튼은 `ClearSlot()` 호출. 메뉴 바깥 좌클릭 시 자동으로 닫힘.

### DragLayer (MonoBehaviour)
드래그 중인 복제 아이콘이 그려질 `root`(RectTransform) 하나만 들고 있는 싱글톤. 캔버스의 가장 마지막 자식으로 둬서 다른 UI보다 항상 위에 그려지게 함.

### InventoryDemoSeeder (MonoBehaviour, 데모 전용)
`seedItems`(ItemData + count 배열)를 인스펙터에서 설정하면 `Start()`에서 `InventoryManager.AddItem()`을 호출해 미리 채워 넣음. 핵심 시스템이 아니라 데모/테스트 편의용.

### InventorySceneBuilder (에디터 전용 스크립트)
`Inventory Template > Build Demo Scene` 메뉴로 씬 전체를 코드로 생성:
1. 한글 폰트 애셋 생성 (OS의 한글 폰트 파일을 찾아 복사 → TMP Dynamic SDF 폰트로 변환)
2. 카메라 / EventSystem / Canvas 생성
3. 슬롯 프리팹 생성 (`Assets/Prefabs/SlotUI.prefab`)
4. InventoryManager, InventoryGrid(4x5), EquipmentPanel(Weapon/Armor), DragLayer, ContextMenu 생성 및 연결
5. 데모 아이템 6종(ItemData 애셋, `Assets/Data/Items/`) 생성 - 아이콘은 코드로 만든 단색 텍스처
6. DemoSeeder에 시작 아이템 4종 등록
7. 씬을 `Assets/Scenes/InventoryDemo.unity`로 저장

부가 메뉴 `Inventory Template > Import TMP Essentials`: TMP 기본 폰트(LiberationSans SDF)가 아직 프로젝트에 없을 때 임포트해주는 배치모드 전용 도구.

## 씬 오브젝트 계층 요약

```
Main Camera                  (배경색만 담당, UI엔 안 씀)
EventSystem                  (StandaloneInputModule - 클릭/드래그 이벤트 처리)
Canvas (Screen Space Overlay)
├── InventoryPanel           InventoryGrid 컴포넌트
│   └── SlotContainer        GridLayoutGroup, 여기 아래 Slot_0 ~ Slot_19 자동 생성
├── EquipmentPanel
│   ├── WeaponSlot           EquipmentSlot(slotType=Weapon)
│   └── ArmorSlot            EquipmentSlot(slotType=Armor)
├── DragLayer                드래그 중 아이콘이 임시로 생기는 곳
└── ContextMenu               ContextMenuUI, UseButton/DiscardButton
InventoryManager              InventoryManager(mainGrid = InventoryPanel)
DemoSeeder                    InventoryDemoSeeder(시작 아이템 4종)
```

## 실제 효과/스탯 시스템을 붙이는 지점

기존 스크립트를 수정하지 않고 아래 이벤트만 구독하면 됨:

| 훅 지점 | 시그니처 | 언제 발생 |
|---|---|---|
| `ItemData.OnUseEffectHook` | `Action<ItemData, GameObject>` | `ItemData.Use(user)` 호출 시 (컨텍스트 메뉴 "사용" 버튼에서 호출하도록 연결하면 됨) |
| `EquipmentSlot.OnItemEquipped` | `static Action<ItemData, EquipmentSlotType>` | 장비 슬롯에 아이템이 배치될 때 |
| `EquipmentSlot.OnItemUnequipped` | `static Action<ItemData, EquipmentSlotType>` | 장비 슬롯이 비워질 때 (교체로 인한 것도 포함) |
| `InventoryManager.OnInventoryFull` | `Action<ItemData>` | `AddItem`이 끝까지 못 넣고 실패했을 때 |

## 한글 관련 참고사항

- **TMP 한글 폰트**: 기본 TMP 폰트(LiberationSans SDF)는 한글 글리프가 없어 한글 텍스트가 □로 깨짐. `InventorySceneBuilder`가 OS에 설치된 한글 폰트 파일(macOS: `AppleGothic.ttf` 등, Windows: `malgun.ttf`)을 찾아 `Assets/Fonts/Korean SDF.asset`을 `AtlasPopulationMode.Dynamic`으로 생성하고, 씬에서 만드는 모든 TMP 텍스트에 자동 적용함.
- **git 한글 파일명 깨짐 방지**: 이 저장소에 아래 설정을 로컬로 적용해둠 (다른 컴퓨터에서 새로 clone하면 각자 한 번씩 실행 필요):
  ```
  git config core.quotepath false        # git status/log에서 한글 파일명이 escape 안 되고 그대로 보이게
  git config core.precomposeunicode true # macOS 파일시스템의 한글 자모분리(NFD) 문제 방지
  git config i18n.commitencoding utf-8
  git config i18n.logoutputencoding utf-8
  ```
  `.gitattributes`에 텍스트 파일 line ending 정규화 규칙도 추가해둠 (내용 인코딩은 원래 UTF-8 그대로 저장되므로 별도 처리 불필요).

## 코드 스타일

- 모든 public 필드/함수에 한글 한 줄 설명 주석 + 주요 로직 블록마다 한글 설명 주석 추가
- 매직 넘버 없이 `[SerializeField]`로 인스펙터에서 조정 가능하게 함
- 싱글톤은 단순 static 인스턴스 패턴 (`DontDestroyOnLoad` 미사용 - 단일 씬 프로토타입이라 불필요)
- 이벤트/콜백 기반 설계 - 실제 게임 로직은 기존 스크립트 수정 없이 이벤트 구독만으로 확장 가능
