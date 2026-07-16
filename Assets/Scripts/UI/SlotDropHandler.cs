using InventoryTemplate.Inventory;
using InventoryTemplate.Item;
using UnityEngine;
using UnityEngine.EventSystems;

namespace InventoryTemplate.UI
{
    /// <summary>
    /// 드롭된 아이템을 받아 이동/합치기/교체 로직을 수행 (인벤토리 슬롯, 장비 슬롯 모두 대상)
    /// </summary>
    [RequireComponent(typeof(InventorySlotUI))]
    public class SlotDropHandler : MonoBehaviour, IDropHandler
    {
        private InventorySlotUI targetUI;

        private void Awake()
        {
            targetUI = GetComponent<InventorySlotUI>();
        }

        /// <summary>드래그 중이던 슬롯이 이 슬롯 위에서 드롭되었을 때 호출</summary>
        public void OnDrop(PointerEventData eventData)
        {
            // pointerDrag는 유니티 이벤트 시스템이 "드래그를 시작한 오브젝트"를 자동으로 넣어줌
            if (eventData.pointerDrag == null) return;

            InventorySlotUI sourceUI = eventData.pointerDrag.GetComponent<InventorySlotUI>();
            // 드래그 시작한 게 슬롯이 아니거나, 자기 자신에게 드롭한 경우면 할 게 없음
            if (sourceUI == null || sourceUI == targetUI) return;

            InventorySlot sourceData = sourceUI.SlotData;
            InventorySlot targetData = targetUI.SlotData;
            // 원본 슬롯이 비어있으면(=드래그할 아이템이 없었으면) 처리할 것 없음
            if (sourceData == null || sourceData.IsEmpty) return;

            ItemData movingItem = sourceData.Item;
            int movingCount = sourceData.CurrentStack;

            // 대상 슬롯이 이 아이템 종류를 받아들이지 않으면 (장비 슬롯 타입 불일치 등) 무시
            if (!targetUI.CanAcceptItem(movingItem)) return;

            if (targetData.IsEmpty)
            {
                // 케이스 1: 대상이 비어있음 - 그냥 옮기기
                targetUI.PlaceItem(movingItem, movingCount);
                sourceUI.ClearSlot();
            }
            else if (targetData.Item == movingItem && targetData.CurrentStack < movingItem.MaxStackSize)
            {
                // 케이스 2: 같은 아이템이고 아직 다 안 찼음 - 스택 합치기
                int overflow = targetData.AddStack(movingCount); // 다 못 합치고 넘친 개수
                targetUI.Refresh();
                if (overflow > 0) sourceUI.PlaceItem(movingItem, overflow); // 넘친 만큼은 원래 슬롯에 남김
                else sourceUI.ClearSlot(); // 다 합쳐졌으면 원래 슬롯은 비움
            }
            else
            {
                // 케이스 3: 서로 다른 아이템 - 자리 교체(스왑)
                // 원본 슬롯도 대상 아이템을 받아들일 수 있어야 교체 가능 (예: 장비 슬롯끼리 타입 다르면 스왑 불가)
                if (!sourceUI.CanAcceptItem(targetData.Item)) return;

                ItemData targetItem = targetData.Item;
                int targetCount = targetData.CurrentStack;

                // 먼저 둘 다 비우고(장비 슬롯이면 여기서 OnItemUnequipped 발생) 서로 반대 자리에 채워 넣음
                sourceUI.ClearSlot();
                targetUI.ClearSlot();
                targetUI.PlaceItem(movingItem, movingCount);
                sourceUI.PlaceItem(targetItem, targetCount);
            }
        }
    }
}
