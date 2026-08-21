#if LEGACY_DISABLED
using StarNight.Grid;
using UnityEngine;

namespace StarNight.Tools.Mining
{
    public readonly struct PickaxeStunContext
    {
        public PickaxeStunContext(
            PickaxeTool2D source,
            GridPos targetCell,
            float durationSeconds)
        {
            Source = source;
            TargetCell = targetCell;
            DurationSeconds = Mathf.Max(0f, durationSeconds);
        }

        public PickaxeTool2D Source { get; }
        public GridPos TargetCell { get; }
        public float DurationSeconds { get; }
    }

    public interface IPickaxeStunnable
    {
        Object PickaxeStunTargetObject { get; }
        bool CanReceivePickaxeStun { get; }
        bool TryReceivePickaxeStun(PickaxeStunContext context);
    }
}

#endif
