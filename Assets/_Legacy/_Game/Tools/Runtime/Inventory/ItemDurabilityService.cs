#if LEGACY_DISABLED
using StarNight.Core.Inventory;
using StarNight.Tools.Core;

namespace StarNight.Tools.Inventory
{
    public static class ItemDurabilityService
    {
        public static DurableEquipmentRecoveryResult ApplyDuplicatePickup(
            InventoryEntry entry,
            HandToolRuntime retainedRuntime)
        {
            if (entry == null || entry.MaxDurability <= 0)
            {
                return default;
            }

            int previousDurability = entry.CurrentDurability;
            bool wasBroken = entry.IsBroken;
            int selectionOrder = entry.SelectionOrder;

            retainedRuntime?.RepairFull();
            entry.CurrentDurability = entry.MaxDurability;
            entry.SelectionOrder = selectionOrder;
            return new DurableEquipmentRecoveryResult(
                entry.ItemId,
                previousDurability,
                entry.CurrentDurability,
                entry.MaxDurability,
                wasBroken,
                entry.SelectionOrder);
        }

        public static void Synchronize(InventoryEntry entry)
        {
            if (entry?.Runtime == null || entry.MaxDurability <= 0)
            {
                return;
            }

            entry.CurrentDurability = entry.Runtime.CurrentResource;
        }
    }
}

#endif
