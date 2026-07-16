using System;
using InventoryTemplate.Item;
using UnityEngine;

namespace InventoryTemplate.Inventory
{
    /// <summary>
    /// 씬 시작 시 데모용 아이템 몇 개를 인벤토리에 미리 채워 넣는다 (데모 전용, 핵심 시스템 아님)
    /// </summary>
    public class InventoryDemoSeeder : MonoBehaviour
    {
        [Serializable]
        public struct SeedEntry
        {
            [SerializeField] private ItemData item;
            /// <summary>미리 채울 아이템 데이터</summary>
            public ItemData Item => item;

            [SerializeField] private int count;
            /// <summary>미리 채울 개수</summary>
            public int Count => count;
        }

        [SerializeField] private SeedEntry[] seedItems;
        /// <summary>시작 시 인벤토리에 추가할 아이템 목록</summary>
        public SeedEntry[] SeedItems => seedItems;

        private void Start()
        {
            // Awake 시점엔 InventoryGrid가 아직 슬롯을 다 안 만들었을 수도 있어서 Start에서 실행
            // (Unity는 모든 오브젝트의 Awake를 먼저 다 끝낸 뒤 Start를 실행하므로 이 시점엔 그리드 준비 완료)
            if (seedItems == null) return;

            foreach (var entry in seedItems)
            {
                if (entry.Item == null) continue; // 인스펙터에서 비워둔 슬롯은 건너뜀
                InventoryManager.Instance.AddItem(entry.Item, entry.Count);
            }
        }
    }
}
