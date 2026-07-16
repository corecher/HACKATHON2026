# 해커톤 사용 가이드 - 인벤토리/아이템 슬롯 템플릿

이 문서는 "어떻게 만들었나"가 아니라 "해커톤에서 어떻게 빨리 갖다 쓰나"를 다룸. 구조/전체 기능 설명은 [`INVENTORY_SYSTEM.md`](./INVENTORY_SYSTEM.md) 참고.

## 1. 5분 안에 돌려보기

1. `Inventory Template > Build Demo Scene` 메뉴 실행
2. `Assets/Scenes/InventoryDemo.unity` 열고 Play
3. 드래그/스택/장착/우클릭 메뉴 한 번씩 눌러보고 "아, 이렇게 동작하는구나" 감 잡기
4. 여기서부터 우리 게임에 맞게 고쳐나가면 됨

씬을 밀고 다시 만들고 싶으면 메뉴를 다시 실행하면 됨 (기존 `InventoryDemo.unity`를 덮어씀). 직접 손댄 씬이면 먼저 복사해두고 실행할 것.

## 2. 새 아이템 추가하기

코드 안 건드려도 됨.

1. `Assets/Data/Items` 우클릭 → `Create > Inventory Template > Item Data`
2. 인스펙터에서 이름/설명/아이콘/최대 스택/타입/장비 슬롯 타입 채우기
3. `InventoryManager.Instance.AddItem(그아이템, 개수)` 호출하거나, 데모처럼 `InventoryDemoSeeder`의 `Seed Items` 배열에 등록

아이콘이 아직 없으면 데모 아이템들처럼 단색 스프라이트로 임시 채워도 됨 (나중에 진짜 아이콘으로 교체).

## 3. 새 장비 슬롯 타입 추가하기 (예: 반지, 신발)

1. `Assets/Scripts/Item/ItemType.cs`의 `EquipmentSlotType` enum에 항목 추가 (예: `Ring`)
2. 씬에서 `WeaponSlot`/`ArmorSlot` 오브젝트를 하나 복제, `EquipmentSlot` 컴포넌트의 `Slot Type`을 새 값으로 변경
3. (선택) `InventorySceneBuilder.BuildEquipmentPanel()`에도 같은 방식으로 `CreateEquipmentSlot(...)` 한 줄 추가하면 다음 "Build Demo Scene" 실행 때마다 자동으로 같이 생성됨

## 4. 실제 효과 붙이기 (여기가 제일 중요)

이 템플릿은 "사용/장착"이 실제로 뭘 하는지는 구현 안 해놨음. 아래 이벤트 3개만 구독하면 기존 코드 한 줄도 안 건드리고 우리 게임 로직을 붙일 수 있음.

```csharp
// 아이템 "사용" 시 실제 효과 (체력 회복, 버프 등)
itemData.OnUseEffectHook += (item, user) =>
{
    // 예: user.GetComponent<PlayerStats>().Heal(20);
};

// 장비 장착 시 스탯 적용
EquipmentSlot.OnItemEquipped += (item, slotType) =>
{
    // 예: playerStats.ApplyWeaponStats(item);
};

// 장비 해제 시 스탯 원복
EquipmentSlot.OnItemUnequipped += (item, slotType) =>
{
    // 예: playerStats.RemoveWeaponStats(item);
};

// 인벤토리 가득 참 알림 (토스트 UI, 사운드 등)
InventoryManager.Instance.OnInventoryFull += (item) =>
{
    // 예: Toast.Show($"{item.ItemName} 넣을 자리가 없습니다");
};
```

`ContextMenuUI`의 "사용" 버튼을 실제로 `ItemData.Use(player)`가 호출되도록 연결하려면 `ContextMenuUI.OnUseClicked()` 안의 `Debug.Log` 줄 위/아래에 `targetSlotUI.SlotData.Item.Use(플레이어GameObject);` 한 줄만 추가하면 됨.

## 5. 자주 바꾸는 값들 (인스펙터, 코드 수정 불필요)

| 바꾸고 싶은 것 | 어디서 |
|---|---|
| 인벤토리 칸 수 | `InventoryPanel`(`InventoryGrid`)의 `Columns`/`Rows` — 단, 씬 재생성 시 `InventorySceneBuilder`의 `GridColumns`/`GridRows` 상수도 같이 바꿔야 다음 빌드에 반영됨 |
| 슬롯 크기/간격 | `InventorySceneBuilder`의 `SlotSize`/`SlotSpacing` 상수 |
| 슬롯 하나 최대 스택 | 각 `ItemData` 애셋의 `Max Stack Size` |
| 인벤토리 패널 위치 | `InventoryPanel`의 RectTransform anchoredPosition (기본: 우측 하단) |

## 6. 다른 장르로 바꿔 쓰기

- **RPG/생존**: 그대로 사용. `EquipmentSlotType`에 슬롯 늘리고 `ItemType`에 재료/퀘스트아이템 등 추가
- **카드게임**: `ItemData`를 카드 데이터로, `InventoryGrid`를 손패로 재해석 가능. 스택 병합은 꺼야 하면 `maxStackSize=1`로 고정
- **시뮬레이션/상점**: `InventoryManager.RemoveItem`/`AddItem`을 거래 로직에서 그대로 호출

핵심 규칙: **이 템플릿의 기존 스크립트는 되도록 수정하지 말고, 이벤트 구독 + 새 스크립트 추가로 확장**할 것. 그래야 나중에 템플릿을 업데이트해도 충돌이 적음.

## 7. 자주 나는 문제 체크리스트

| 증상 | 원인/해결 |
|---|---|
| 슬롯이 아예 안 보임 | `InventoryGrid`의 `Slot UI Prefab`/`Grid Parent` 연결 빠짐 - 씬 다시 빌드하거나 인스펙터 확인 |
| 한글 텍스트가 □로 깨짐 | TMP 기본 폰트엔 한글 없음 - `Korean SDF` 폰트 애셋이 해당 텍스트에 할당돼 있는지 확인. 새로 만든 TMP 텍스트엔 직접 `Assets/Fonts/Korean SDF.asset`을 Font Asset으로 지정할 것 |
| 드래그해도 아이콘 하나도 안 따라옴 | 씬에 `DragLayer` 오브젝트 있는지, `DragLayer.Instance`가 null 아닌지 확인 (씬 재생성이 제일 빠름) |
| 드롭해도 아이템이 안 옮겨짐 | 대상 슬롯에 `SlotDropHandler` 컴포넌트 있는지, `EventSystem`이 씬에 있는지 확인 |
| 장비 슬롯에 아무거나 다 들어감 | `ItemData`의 `Equip Slot Type`이 `None`으로 되어있으면 아예 장착 대상이 아님 - 장비 아이템은 반드시 값 지정 |
| Play 눌러도 화면이 새까맣기만 함 | `Main Camera`가 씬에 있는지 확인 (없으면 배경이 검정) |
| 우클릭 메뉴가 이상한 곳에 뜸 | Game 탭이 아니라 Scene 탭 보고 있으면 위치가 다르게 보이는 게 정상 - Game 탭에서 확인할 것 |

## 8. 시간 없을 때 우선순위

1. **꼭 필요**: 아이템 추가(2번), 실제 효과 훅업(4번)
2. **여유 있으면**: 새 장비 슬롯(3번), UI 위치/크기 조정(5번)
3. **발표 직전엔 건드리지 말 것**: `InventorySceneBuilder.cs` 자체 로직, `SlotDropHandler`의 스왑/병합 로직 - 여기 손대면 리스크 큼, 이벤트 구독으로 우회할 방법부터 찾을 것
