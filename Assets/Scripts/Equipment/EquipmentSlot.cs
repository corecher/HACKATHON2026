using System;
using InventoryTemplate.Inventory;
using InventoryTemplate.Item;
using InventoryTemplate.UI;

namespace InventoryTemplate.Equipment
{
    /// <summary>
    /// 특정 장비 타입만 받아들이는 장비 슬롯 (InventorySlotUI 상속)
    /// </summary>
    public class EquipmentSlot : InventorySlotUI
    {
        [UnityEngine.SerializeField] private EquipmentSlotType slotType;
        /// <summary>이 슬롯이 받아들이는 장비 종류</summary>
        public EquipmentSlotType SlotType => slotType;

        /// <summary>아이템이 성공적으로 장착되었을 때 발생 (아이템, 슬롯 종류)</summary>
        public static event Action<ItemData, EquipmentSlotType> OnItemEquipped;

        /// <summary>아이템이 해제(탈착)되었을 때 발생 (아이템, 슬롯 종류)</summary>
        public static event Action<ItemData, EquipmentSlotType> OnItemUnequipped;

        private void Awake()
        {
            // 장비 슬롯은 그리드에 속하지 않으니 소속 그리드는 null, 인덱스는 -1로 자체 데이터만 생성
            Bind(new InventorySlot(), null, -1);
        }

        /// <summary>아이템의 장착 슬롯 타입이 이 슬롯과 일치하는지 확인</summary>
        public override bool CanAcceptItem(ItemData item)
        {
            // 예: 이 슬롯이 Weapon인데 item.EquipSlotType이 Armor면 false
            return item != null && item.EquipSlotType == slotType;
        }

        /// <summary>장비 장착 - 배치 후 OnItemEquipped 이벤트 발생</summary>
        public override void PlaceItem(ItemData item, int count)
        {
            // 기본 배치 로직(데이터 채우고 화면 갱신)은 부모 그대로 사용
            base.PlaceItem(item, count);
            // 그 후 "장착됐다"는 이벤트만 추가로 발생시킴 - 스탯 적용 등은 이 이벤트를 구독해서 처리
            OnItemEquipped?.Invoke(item, slotType);
        }

        /// <summary>장비 해제 - 비우기 전 아이템을 기억해 OnItemUnequipped 이벤트 발생</summary>
        public override void ClearSlot()
        {
            // base.ClearSlot()이 데이터를 지우기 전에 어떤 아이템이었는지 미리 저장해둬야 함
            ItemData previous = SlotData?.Item;
            base.ClearSlot();
            // 원래 아이템이 있었을 때만(빈 슬롯을 또 지운 게 아니라면) 탈착 이벤트 발생
            if (previous != null) OnItemUnequipped?.Invoke(previous, slotType);
        }
    }
}
