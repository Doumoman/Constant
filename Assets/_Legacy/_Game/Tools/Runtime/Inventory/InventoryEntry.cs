#if LEGACY_DISABLED
using System;
using StarNight.Core.Inventory;
using StarNight.Tools.Core;
using UnityEngine;

namespace StarNight.Tools.Inventory
{
    [Serializable]
    public sealed class InventoryEntry
    {
        [SerializeField] private DurableEquipmentEntry durable = new DurableEquipmentEntry();
        public bool IsSelected;

        [NonSerialized] public HandToolRuntime Runtime;

        public DurableEquipmentEntry Durable => durable;
        public int ItemId
        {
            get => durable.ItemId;
            set => durable.ItemId = value;
        }
        public int CurrentDurability
        {
            get => durable.CurrentDurability;
            set => durable.SetCurrentDurability(value);
        }
        public int MaxDurability
        {
            get => durable.MaxDurability;
            set
            {
                durable.MaxDurability = Mathf.Max(0, value);
                durable.SetCurrentDurability(durable.CurrentDurability);
            }
        }
        public int SelectionOrder
        {
            get => durable.SelectionOrder;
            set => durable.SelectionOrder = value;
        }
        public string StableItemId => Runtime != null ? Runtime.StableItemId : string.Empty;
        public bool IsBroken
        {
            get
            {
                durable.RefreshBrokenState();
                return durable.IsBroken;
            }
        }
    }
}

#endif
