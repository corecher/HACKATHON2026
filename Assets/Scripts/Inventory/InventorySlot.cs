using InventoryTemplate.Item;

namespace InventoryTemplate.Inventory
{
    /// <summary>
    /// 슬롯 하나의 데이터 (아이템 참조 + 현재 개수) - MonoBehaviour 아닌 순수 데이터 클래스
    /// </summary>
    public class InventorySlot
    {
        /// <summary>이 슬롯에 담긴 아이템 데이터 (없으면 null)</summary>
        public ItemData Item { get; private set; }

        /// <summary>이 슬롯에 담긴 아이템 현재 개수</summary>
        public int CurrentStack { get; private set; }

        /// <summary>슬롯이 비어있는지 여부</summary>
        public bool IsEmpty => Item == null || CurrentStack <= 0;

        /// <summary>슬롯에 아이템을 새로 채운다 (기존 내용은 덮어씀)</summary>
        public void SetItem(ItemData item, int count)
        {
            // 이전에 뭐가 들어있었든 상관없이 그냥 덮어씀 (스왑/이동 로직은 호출하는 쪽에서 처리)
            Item = item;
            CurrentStack = count;
        }

        /// <summary>슬롯을 완전히 비운다</summary>
        public void Clear()
        {
            // 아이템 참조와 개수를 둘 다 초기화해야 IsEmpty가 true로 바뀜
            Item = null;
            CurrentStack = 0;
        }

        /// <summary>주어진 아이템이 이 슬롯과 같은 종류라 스택 합칠 수 있는지 확인</summary>
        public bool CanStackWith(ItemData otherItem)
        {
            // 빈 슬롯이 아니고, 같은 아이템이고, 아직 최대 스택에 안 찼을 때만 true
            return !IsEmpty && Item == otherItem && CurrentStack < Item.MaxStackSize;
        }

        /// <summary>스택에 개수를 더한다. maxStackSize를 넘는 만큼은 남겨서 반환한다</summary>
        public int AddStack(int amount)
        {
            int total = CurrentStack + amount;
            int max = Item.MaxStackSize;
            if (total > max)
            {
                // 최대치까지만 채우고 넘친 만큼은 호출자에게 돌려줘서 다른 슬롯에 남기게 함
                CurrentStack = max;
                return total - max;
            }
            // 넘치지 않으면 그대로 다 담고 남는 건 없음(0) 반환
            CurrentStack = total;
            return 0;
        }
    }
}
