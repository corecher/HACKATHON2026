using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InventoryTemplate.UI
{
    /// <summary>
    /// 슬롯 아이콘을 마우스로 드래그할 때 최상단 레이어에 복제 아이콘을 띄워 따라다니게 한다
    /// </summary>
    [RequireComponent(typeof(InventorySlotUI))]
    public class SlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private InventorySlotUI slotUI;
        private GameObject dragIconGO;
        private RectTransform dragIconRect;

        private void Awake()
        {
            slotUI = GetComponent<InventorySlotUI>();
        }

        /// <summary>드래그 시작 - 아이콘을 최상단 레이어에 복제 생성</summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            // 빈 슬롯은 드래그할 게 없으니 무시
            if (slotUI.SlotData == null || slotUI.SlotData.IsEmpty) return;
            // 최상단 레이어가 씬에 없으면(설정 누락) 그냥 무시
            if (DragLayer.Instance == null) return;

            // 마우스를 따라다닐 복제 아이콘 오브젝트를 새로 생성
            dragIconGO = new GameObject("DragIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            // DragLayer(최상단)에 붙여서 다른 UI들보다 항상 위에 그려지게 함
            dragIconGO.transform.SetParent(DragLayer.Instance.Root, false);

            var image = dragIconGO.GetComponent<Image>();
            image.sprite = slotUI.SlotData.Item.Icon; // 원래 슬롯이랑 같은 아이콘으로
            image.raycastTarget = false; // 이 아이콘이 마우스 이벤트를 가로채면 드롭 판정이 안 되므로 꺼둠
            image.SetNativeSize(); // 스프라이트 원본 크기로

            dragIconRect = dragIconGO.GetComponent<RectTransform>();
            dragIconRect.position = eventData.position; // 시작하자마자 마우스 위치로 이동
        }

        /// <summary>드래그 중 - 복제 아이콘이 마우스 좌표를 따라간다</summary>
        public void OnDrag(PointerEventData eventData)
        {
            // 매 프레임 마우스 좌표로 따라오게만 하면 됨
            if (dragIconRect != null) dragIconRect.position = eventData.position;
        }

        /// <summary>드래그 종료 - 복제 아이콘 제거 (실제 이동/스왑은 SlotDropHandler.OnDrop이 처리)</summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            // 복제 아이콘은 시각 효과일 뿐이라 여기서 정리만 하면 끝
            // 실제 아이템 이동/합치기/교체는 드롭된 슬롯의 SlotDropHandler.OnDrop에서 처리됨
            if (dragIconGO != null) Destroy(dragIconGO);
        }
    }
}
