#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Tools.Items
{
    public enum ItemUseCategory
    {
        ActiveTool,
        JumpModifier,
        PassiveDetector,
        Consumable,
        ContextItem,
    }

    public class ItemDefinition : ScriptableObject
    {
        [SerializeField, Min(0)] private int itemId;
        [SerializeField] private ItemUseCategory useCategory = ItemUseCategory.ActiveTool;
        [SerializeField, Min(0)] private int maxDurability;
        [SerializeField] private bool allowDuplicate = true;
        [SerializeField] private bool canDrop = true;
        [SerializeField] private bool tabSelectable = true;
        [SerializeField] private int selectionOrder;

        public virtual int ItemId => itemId;
        public virtual ItemUseCategory UseCategory => useCategory;
        public virtual int MaxDurability => maxDurability;
        public virtual bool AllowDuplicate => allowDuplicate;
        public virtual bool CanDrop => canDrop;
        public virtual bool TabSelectable => tabSelectable;
        public virtual int SelectionOrder => selectionOrder != 0 ? selectionOrder : ItemId;

        public void ConfigureItemContract(
            int id,
            ItemUseCategory category,
            int durability,
            bool duplicates,
            bool droppable,
            bool selectable,
            int order)
        {
            itemId = Mathf.Max(0, id);
            useCategory = category;
            maxDurability = Mathf.Max(0, durability);
            allowDuplicate = duplicates;
            canDrop = droppable;
            tabSelectable = selectable;
            selectionOrder = order;
        }
    }

    public static class ToolItemIdCatalog
    {
        public static int Resolve(string toolId)
        {
            return toolId switch
            {
                "TOOL_PICKAXE" => 201,
                "TOOL_SHOVEL" => 202,
                "TOOL_WATERING_CAN" => 203,
                "TOOL_POUNDER" => 204,
                "TOOL_HOOK_LAUNCHER" => 205,
                "TOOL_WIND_UMBRELLA" => 206,
                "EQUIPMENT_SPRING" => 301,
                "ITEM_MOON_EYE_COMPASS" => 302,
                _ => 0,
            };
        }
    }
}

#endif
