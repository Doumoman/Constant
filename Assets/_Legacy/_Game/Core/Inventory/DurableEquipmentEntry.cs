#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Core.Inventory
{
    [Serializable]
    public sealed class DurableEquipmentEntry
    {
        public int ItemId;
        public int CurrentDurability;
        public int MaxDurability;
        public bool IsBroken;
        public int SelectionOrder;

        public void Configure(
            int itemId,
            int currentDurability,
            int maxDurability,
            int selectionOrder)
        {
            ItemId = itemId;
            MaxDurability = Mathf.Max(0, maxDurability);
            CurrentDurability = MaxDurability > 0
                ? Mathf.Clamp(currentDurability, 0, MaxDurability)
                : Mathf.Max(0, currentDurability);
            SelectionOrder = selectionOrder;
            RefreshBrokenState();
        }

        public void SetCurrentDurability(int durability)
        {
            CurrentDurability = MaxDurability > 0
                ? Mathf.Clamp(durability, 0, MaxDurability)
                : Mathf.Max(0, durability);
            RefreshBrokenState();
        }

        public void RefreshBrokenState()
        {
            IsBroken = MaxDurability > 0 && CurrentDurability <= 0;
        }
    }
}

#endif
