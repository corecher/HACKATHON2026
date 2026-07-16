namespace InventoryTemplate.Item
{
    /// <summary>
    /// 아이템 종류 분류
    /// </summary>
    public enum ItemType
    {
        Weapon,
        Consumable,
        Material,
        Equipment
    }

    /// <summary>
    /// 장착 가능한 장비 슬롯 종류 (해당 없으면 None)
    /// </summary>
    public enum EquipmentSlotType
    {
        None,
        Head,
        Weapon,
        Armor
    }
}
