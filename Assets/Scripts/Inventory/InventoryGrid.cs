using System.Collections.Generic;
using InventoryTemplate.Item;
using InventoryTemplate.UI;
using UnityEngine;

namespace InventoryTemplate.Inventory
{
    /// <summary>
    /// 여러 InventorySlot을 격자 형태로 관리하는 컨테이너 (런타임에 슬롯 UI 자동 생성)
    /// </summary>
    public class InventoryGrid : MonoBehaviour
    {
        [Header("격자 크기")]
        [SerializeField] private int columns = 4;
        /// <summary>격자 열 개수</summary>
        public int Columns => columns;

        [SerializeField] private int rows = 5;
        /// <summary>격자 행 개수</summary>
        public int Rows => rows;

        [Header("생성 참조")]
        [SerializeField] private InventorySlotUI slotUIPrefab;
        /// <summary>슬롯 하나를 표현할 프리팹</summary>
        public InventorySlotUI SlotUIPrefab => slotUIPrefab;

        [SerializeField] private RectTransform gridParent;
        /// <summary>슬롯 UI들이 배치될 부모 (GridLayoutGroup 포함)</summary>
        public RectTransform GridParent => gridParent;

        private readonly List<InventorySlot> slots = new List<InventorySlot>();
        private readonly List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();

        /// <summary>이 그리드가 가진 슬롯 데이터 목록 (읽기 전용)</summary>
        public IReadOnlyList<InventorySlot> Slots => slots;

        /// <summary>이 그리드가 가진 슬롯 UI 목록 (읽기 전용)</summary>
        public IReadOnlyList<InventorySlotUI> SlotUIs => slotUIs;

        private void Awake()
        {
            // 씬 시작하자마자 슬롯들을 생성 (인스펙터에서 미리 만들어두지 않고 런타임 자동 생성)
            BuildGrid();
        }

        /// <summary>rows x columns 개수만큼 슬롯 데이터와 UI를 생성해 바인딩한다</summary>
        public void BuildGrid()
        {
            // 이미 만들어져 있으면 중복 생성 방지
            if (slots.Count > 0) return;

            int total = columns * rows;
            for (int i = 0; i < total; i++)
            {
                // 데이터 쪽 슬롯 하나 생성
                var slotData = new InventorySlot();
                slots.Add(slotData);

                // 화면에 보일 UI 프리팹도 하나 생성해서 위 데이터와 연결(Bind)
                InventorySlotUI ui = Instantiate(slotUIPrefab, gridParent);
                ui.name = $"Slot_{i}";
                ui.Bind(slotData, this, i);
                slotUIs.Add(ui);
            }
        }

        /// <summary>화면에 표시된 모든 슬롯 UI를 새로고침한다</summary>
        public void RefreshAll()
        {
            // 슬롯 전체를 한 번에 갱신할 때 사용 (아이템 로드 등)
            foreach (var ui in slotUIs) ui.Refresh();
        }

        /// <summary>지정한 인덱스 슬롯의 UI만 새로고침한다</summary>
        public void RefreshSlot(int index)
        {
            // 범위 벗어난 인덱스면 무시 (방어 코드)
            if (index < 0 || index >= slotUIs.Count) return;
            slotUIs[index].Refresh();
        }

        /// <summary>동일 아이템을 스택 가능한 슬롯 인덱스를 찾는다 (없으면 -1)</summary>
        public int FindStackableSlot(ItemData item)
        {
            // 앞에서부터 순서대로 훑으면서 같은 아이템 + 아직 안 찬 슬롯을 찾음
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].CanStackWith(item)) return i;
            }
            return -1;
        }

        /// <summary>빈 슬롯 인덱스를 찾는다 (없으면 -1)</summary>
        public int FindEmptySlot()
        {
            // 스택할 곳이 없을 때 새로 넣을 빈 슬롯을 찾는 용도
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsEmpty) return i;
            }
            return -1;
        }
    }
}
