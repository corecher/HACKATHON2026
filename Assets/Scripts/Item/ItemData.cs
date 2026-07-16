using System;
using UnityEngine;

namespace InventoryTemplate.Item
{
    /// <summary>
    /// 아이템 하나를 정의하는 데이터 애셋 (ScriptableObject)
    /// </summary>
    [CreateAssetMenu(fileName = "NewItemData", menuName = "Inventory Template/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] private int itemId;
        /// <summary>아이템 고유 ID</summary>
        public int ItemId => itemId;

        [SerializeField] private string itemName;
        /// <summary>아이템 표시 이름</summary>
        public string ItemName => itemName;

        [SerializeField] [TextArea] private string description;
        /// <summary>아이템 설명 텍스트</summary>
        public string Description => description;

        [SerializeField] private Sprite icon;
        /// <summary>인벤토리에 표시할 아이콘 스프라이트</summary>
        public Sprite Icon => icon;

        [Header("스택/분류")]
        [SerializeField] private int maxStackSize = 1;
        /// <summary>슬롯 하나에 최대로 겹칠 수 있는 개수</summary>
        public int MaxStackSize => maxStackSize;

        [SerializeField] private ItemType itemType;
        /// <summary>아이템 종류 (무기/소모품/재료/장비)</summary>
        public ItemType Type => itemType;

        [Header("장비 전용")]
        [SerializeField] private EquipmentSlotType equipSlotType = EquipmentSlotType.None;
        /// <summary>장착 가능한 장비 슬롯 종류 (장착 불가면 None)</summary>
        public EquipmentSlotType EquipSlotType => equipSlotType;

        /// <summary>실제 효과(회복/버프 등)는 여기 이벤트를 구독해서 나중에 붙인다. 이 템플릿은 훅만 제공한다</summary>
        public event Action<ItemData, GameObject> OnUseEffectHook;

        /// <summary>아이템 사용 시 호출 - 실제 효과 로직은 이 자리에서 훅업</summary>
        public void Use(GameObject user)
        {
            // 구독자가 없으면(null) 아무 일도 안 일어남 - 실제 회복/버프 등은 나중에 이 이벤트를 구독해서 구현
            OnUseEffectHook?.Invoke(this, user);
        }
    }
}
