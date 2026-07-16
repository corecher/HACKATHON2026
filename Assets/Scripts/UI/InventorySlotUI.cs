using InventoryTemplate.Inventory;
using InventoryTemplate.Item;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InventoryTemplate.UI
{
    /// <summary>
    /// 슬롯 하나의 시각적 표현 (아이콘 이미지 + 스택 수량 텍스트)
    /// </summary>
    public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("참조")]
        [SerializeField] private Image iconImage;
        /// <summary>아이템 아이콘을 표시할 Image 컴포넌트</summary>
        public Image IconImage => iconImage;

        [SerializeField] private TMP_Text stackText;
        /// <summary>스택 개수를 표시할 TMP 텍스트</summary>
        public TMP_Text StackText => stackText;

        /// <summary>이 슬롯이 담고 있는 데이터 (그리드/장비 매니저가 주입)</summary>
        public InventorySlot SlotData { get; private set; }

        /// <summary>슬롯이 속한 그리드 내 인덱스 (그리드 슬롯이 아니면 -1)</summary>
        public int SlotIndex { get; private set; } = -1;

        /// <summary>이 슬롯이 속한 그리드 (없으면 null - 장비 슬롯 등)</summary>
        public InventoryGrid OwnerGrid { get; private set; }

        /// <summary>슬롯 데이터를 연결하고 화면을 갱신한다</summary>
        public void Bind(InventorySlot data, InventoryGrid ownerGrid, int slotIndex)
        {
            // 데이터/소속 그리드/인덱스를 저장해두고 바로 화면에 반영
            SlotData = data;
            OwnerGrid = ownerGrid;
            SlotIndex = slotIndex;
            Refresh();
        }

        /// <summary>슬롯 데이터 내용에 맞춰 아이콘/텍스트를 갱신한다</summary>
        public virtual void Refresh()
        {
            // 데이터가 없거나 비어있으면 empty 처리
            bool empty = SlotData == null || SlotData.IsEmpty;

            if (iconImage != null)
            {
                // 비어있으면 아이콘 숨기고 스프라이트도 비움
                iconImage.enabled = !empty;
                iconImage.sprite = empty ? null : SlotData.Item.Icon;
            }

            if (stackText != null)
            {
                // 1개일 땐 굳이 "1" 안 보여주고, 2개 이상일 때만 숫자 표시
                bool showCount = !empty && SlotData.CurrentStack > 1;
                stackText.enabled = showCount;
                stackText.text = showCount ? SlotData.CurrentStack.ToString() : string.Empty;
            }
        }

        /// <summary>우클릭 시 컨텍스트 메뉴를 연다 (좌클릭은 드래그 핸들러가 처리)</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            // 우클릭 아니면 무시 (좌클릭 드래그는 SlotDragHandler가 별도로 처리)
            if (eventData.button != PointerEventData.InputButton.Right) return;
            // 빈 슬롯 우클릭은 메뉴 띄울 필요 없음
            if (SlotData == null || SlotData.IsEmpty) return;

            // 컨텍스트 메뉴 싱글톤에게 "이 슬롯 기준으로, 이 화면 좌표에 떠라" 요청
            ContextMenuUI.Instance?.Show(this, eventData.position);
        }

        /// <summary>이 슬롯이 해당 아이템을 받아들일 수 있는지 (장비 슬롯 등에서 override)</summary>
        public virtual bool CanAcceptItem(ItemData item) => true; // 일반 인벤토리 슬롯은 뭐든 다 받음 (장비 슬롯은 타입 체크하도록 override)

        /// <summary>슬롯에 아이템을 배치하고 화면을 갱신한다</summary>
        public virtual void PlaceItem(ItemData item, int count)
        {
            // 데이터부터 채우고 그 다음 화면 갱신 (순서 중요 - Refresh가 SlotData를 읽음)
            SlotData.SetItem(item, count);
            Refresh();
        }

        /// <summary>슬롯을 비운다</summary>
        public virtual void ClearSlot()
        {
            SlotData.Clear();
            Refresh();
        }
    }
}
