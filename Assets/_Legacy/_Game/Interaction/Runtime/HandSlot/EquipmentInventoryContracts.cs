#if LEGACY_DISABLED
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Interaction.HandSlot
{
    public enum EquipmentInventoryUseKind
    {
        Primary,
        Jump,
        Passive,
    }

    public readonly struct EquipmentInventoryHudEntry
    {
        public EquipmentInventoryHudEntry(
            string stableItemId,
            string displayName,
            Sprite icon,
            int currentDurability,
            int maximumDurability,
            bool selected,
            EquipmentInventoryUseKind useKind)
        {
            StableItemId = stableItemId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Icon = icon;
            CurrentDurability = currentDurability;
            MaximumDurability = maximumDurability;
            IsBroken = maximumDurability > 0 && currentDurability <= 0;
            IsSelected = selected;
            UseKind = useKind;
        }

        public string StableItemId { get; }
        public string DisplayName { get; }
        public Sprite Icon { get; }
        public int CurrentDurability { get; }
        public int MaximumDurability { get; }
        public bool IsBroken { get; }
        public bool IsSelected { get; }
        public EquipmentInventoryUseKind UseKind { get; }
    }

    public interface IEquipmentInventoryBridge
    {
        IReadOnlyList<EquipmentInventoryHudEntry> HudEntries { get; }
        HandSlotItemRuntime SelectedRuntime { get; }
        string LatestFeedbackMessage { get; }
        int FeedbackRevision { get; }
        bool IsInventoryItem(HandSlotItemRuntime item);
        bool TryPickupEquipment(HandSlotItemRuntime item);
        bool TryStowSelected();
        bool TryDropSelected(Vector2 worldPosition);
        bool TrySelectNext(float now);
        bool TrySelectPrevious(float now);
        void TryRestoreSelected();
    }

    public interface ISelectedEquipmentJumpModifier
    {
        bool TryExecuteSelectedJump(PlayerHandSlot owner);
    }

    public interface ICompassFocusDetector
    {
        bool TryFocusNearestSecret(float rangeCells, float durationSeconds);
    }

    public interface IPlayerInventoryActionExecutor
    {
        bool HasPhysicalCarryItem { get; }
        bool HasSelectedEquipment { get; }
    }
}

#endif
