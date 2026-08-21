#if LEGACY_DISABLED
using StarNight.Tools.Core;

namespace StarNight.Tools.Inventory
{
    public enum EquipmentPickupResult
    {
        Rejected,
        Added,
        DuplicateRepaired,
    }

    public static class ItemPickupResolver
    {
        public static EquipmentPickupResult Resolve(EquipmentInventory inventory, HandToolRuntime runtime)
        {
            return inventory != null
                ? inventory.ResolvePickup(runtime)
                : EquipmentPickupResult.Rejected;
        }
    }
}

#endif
