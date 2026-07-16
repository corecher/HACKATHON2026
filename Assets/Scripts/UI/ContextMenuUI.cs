using UnityEngine;
using UnityEngine.UI;

namespace InventoryTemplate.UI
{
    /// <summary>
    /// 슬롯 우클릭 시 뜨는 간단한 컨텍스트 메뉴 (사용 / 버리기)
    /// </summary>
    public class ContextMenuUI : MonoBehaviour
    {
        /// <summary>싱글톤 인스턴스</summary>
        public static ContextMenuUI Instance { get; private set; }

        [SerializeField] private RectTransform panelRect;
        /// <summary>메뉴 패널의 RectTransform (표시 위치 이동 + 클릭 영역 판정용)</summary>
        public RectTransform PanelRect => panelRect;

        [SerializeField] private Button useButton;
        /// <summary>사용 버튼</summary>
        public Button UseButton => useButton;

        [SerializeField] private Button discardButton;
        /// <summary>버리기 버튼</summary>
        public Button DiscardButton => discardButton;

        private InventorySlotUI targetSlotUI;

        private void Awake()
        {
            Instance = this;
            // 버튼 클릭 시 실행할 함수 등록
            useButton.onClick.AddListener(OnUseClicked);
            discardButton.onClick.AddListener(OnDiscardClicked);
            // 시작할 땐 메뉴가 안 보여야 정상 (우클릭했을 때만 Show()로 켜짐)
            panelRect.gameObject.SetActive(false);
        }

        private void Update()
        {
            // 메뉴가 안 떠있으면 검사할 필요 없음
            if (!panelRect.gameObject.activeSelf) return;

            // 메뉴 바깥을 좌클릭하면 닫는다
            if (Input.GetMouseButtonDown(0) &&
                !RectTransformUtility.RectangleContainsScreenPoint(panelRect, Input.mousePosition))
            {
                Hide();
            }
        }

        /// <summary>지정한 슬롯을 대상으로 화면 좌표 위치에 메뉴를 띄운다</summary>
        public void Show(InventorySlotUI slotUI, Vector2 screenPosition)
        {
            // 어느 슬롯에 대해 우클릭했는지 기억해둬야 Use/Discard 클릭 시 그 슬롯에 적용 가능
            targetSlotUI = slotUI;
            panelRect.position = screenPosition; // 우클릭한 화면 좌표로 메뉴 이동
            panelRect.gameObject.SetActive(true);
        }

        /// <summary>메뉴를 닫는다</summary>
        public void Hide()
        {
            targetSlotUI = null;
            panelRect.gameObject.SetActive(false);
        }

        private void OnUseClicked()
        {
            // 대상 슬롯이 유효하고(null 아니고) 비어있지 않을 때만 처리
            if (targetSlotUI?.SlotData != null && !targetSlotUI.SlotData.IsEmpty)
            {
                // 실제 사용 효과는 ItemData.Use()를 통해 이후 이 자리에서 훅업
                Debug.Log($"[사용] {targetSlotUI.SlotData.Item.ItemName} (실제 효과 없음, 로그만 출력)");
            }
            Hide(); // 클릭했으니 메뉴는 닫음
        }

        private void OnDiscardClicked()
        {
            if (targetSlotUI?.SlotData != null && !targetSlotUI.SlotData.IsEmpty)
            {
                // 그냥 해당 슬롯을 비워버림 (아이템 완전히 버려짐)
                targetSlotUI.ClearSlot();
            }
            Hide();
        }
    }
}
