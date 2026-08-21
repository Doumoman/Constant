#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Interaction.HandSlot
{
    /// <summary>
    /// Read-only hand-slot projection consumed by HUD and narrative layers.
    /// Gameplay ownership remains with the concrete runtime item.
    /// </summary>
    public interface IHandSlotHudSource
    {
        string StableItemId { get; }
        string DisplayName { get; }
        Sprite HudIcon { get; }
        bool ShowResource { get; }
        int CurrentResource { get; }
        int MaximumResource { get; }
        string PrimaryActionLabel { get; }
        bool IsHandTool { get; }
    }
}

#endif
