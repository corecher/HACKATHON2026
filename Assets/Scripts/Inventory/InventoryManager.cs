using System;
using InventoryTemplate.Item;
using UnityEngine;

namespace InventoryTemplate.Inventory
{
    /// <summary>
    /// 인벤토리 전체를 관장하는 싱글톤 매니저 - 아이템 추가/제거/개수 조회 담당
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        /// <summary>싱글톤 인스턴스 (단일 씬 프로토타입이므로 DontDestroyOnLoad 사용 안 함)</summary>
        public static InventoryManager Instance { get; private set; }

        [SerializeField] private InventoryGrid mainGrid;
        /// <summary>아이템이 저장될 메인 인벤토리 그리드</summary>
        public InventoryGrid MainGrid => mainGrid;

        /// <summary>인벤토리가 가득 차서 추가에 실패했을 때 발생 (실패한 아이템)</summary>
        public event Action<ItemData> OnInventoryFull;

        private void Awake()
        {
            // 이미 다른 인스턴스가 있으면 이 오브젝트는 필요 없으니 파괴 (씬에 하나만 남게)
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>아이템을 count개 추가한다 - 기존 스택 우선 병합, 없으면 빈 슬롯 사용, 다 못 넣으면 false</summary>
        public bool AddItem(ItemData item, int count)
        {
            int remaining = count; // 아직 못 넣고 남은 개수

            while (remaining > 0)
            {
                // 1순위: 같은 아이템이 이미 있는 슬롯에 스택으로 합치기
                int stackIndex = mainGrid.FindStackableSlot(item);
                if (stackIndex >= 0)
                {
                    var slot = mainGrid.Slots[stackIndex];
                    int overflow = slot.AddStack(remaining); // 다 못 담고 넘친 개수
                    mainGrid.RefreshSlot(stackIndex);
                    remaining = overflow; // 넘친 만큼은 다음 루프에서 또 처리
                    continue;
                }

                // 2순위: 스택할 곳이 없으면 빈 슬롯에 새로 넣기
                int emptyIndex = mainGrid.FindEmptySlot();
                if (emptyIndex >= 0)
                {
                    var slot = mainGrid.Slots[emptyIndex];
                    int placeAmount = Mathf.Min(remaining, item.MaxStackSize); // 한 슬롯엔 최대 스택만큼만
                    slot.SetItem(item, placeAmount);
                    mainGrid.RefreshSlot(emptyIndex);
                    remaining -= placeAmount;
                    continue;
                }

                // 스택할 곳도, 빈 슬롯도 없음 - 더 이상 넣을 수 없음
                break;
            }

            if (remaining > 0)
            {
                // 끝까지 다 못 넣었으면 인벤토리 꽉 찬 것 - 이벤트로 알림
                OnInventoryFull?.Invoke(item);
                return false;
            }
            return true;
        }

        /// <summary>아이템을 count개 제거한다 - 보유 수량이 부족하면 아무것도 지우지 않고 false</summary>
        public bool RemoveItem(ItemData item, int count)
        {
            // 보유량이 요청량보다 적으면 아예 시작도 안 함 (일부만 지우는 어중간한 상태 방지)
            if (GetItemCount(item) < count) return false;

            int remaining = count;
            for (int i = 0; i < mainGrid.Slots.Count && remaining > 0; i++)
            {
                var slot = mainGrid.Slots[i];
                if (slot.IsEmpty || slot.Item != item) continue; // 해당 아이템 아니면 건너뜀

                int take = Mathf.Min(remaining, slot.CurrentStack); // 이 슬롯에서 뺄 수 있는 만큼만
                int leftInSlot = slot.CurrentStack - take;
                if (leftInSlot <= 0) slot.Clear(); // 다 빠지면 슬롯 자체를 비움
                else slot.SetItem(item, leftInSlot); // 남으면 줄어든 개수로 갱신

                mainGrid.RefreshSlot(i);
                remaining -= take;
            }
            return true;
        }

        /// <summary>메인 그리드에 있는 특정 아이템의 총 개수를 반환한다</summary>
        public int GetItemCount(ItemData item)
        {
            int total = 0;
            // 그리드 전체 슬롯을 돌면서 같은 아이템 개수를 다 더함
            foreach (var slot in mainGrid.Slots)
            {
                if (!slot.IsEmpty && slot.Item == item) total += slot.CurrentStack;
            }
            return total;
        }
    }
}
