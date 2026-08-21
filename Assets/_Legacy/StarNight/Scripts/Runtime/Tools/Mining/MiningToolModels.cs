#if LEGACY_DISABLED
using StarNight.Grid;
using UnityEngine;

namespace StarNight.Tools.Mining
{
    public enum MiningToolState
    {
        Ready = 0,
        WindingUp = 1
    }

    public enum MiningUseFailure
    {
        None = 0,
        Busy,
        NoDurability,
        MissingGridWorld,
        MissingMutationService,
        MissingTerrainTilemap,
        InvalidDirection,
        TargetOutOfBounds,
        NoTile,
        UndefinedTile,
        ProtectedTile,
        WrongTerrain,
        MutationRejected
    }

    public readonly struct MiningUseResult
    {
        public MiningUseResult(
            GridPos targetCell,
            MiningUseFailure failure,
            long mutationSequence,
            int remainingDurability,
            bool affectedNonTerrainTarget = false)
        {
            TargetCell = targetCell;
            Failure = failure;
            MutationSequence = mutationSequence;
            RemainingDurability = remainingDurability;
            AffectedNonTerrainTarget = affectedNonTerrainTarget;
        }

        public GridPos TargetCell { get; }
        public MiningUseFailure Failure { get; }
        public long MutationSequence { get; }
        public int RemainingDurability { get; }
        public bool AffectedNonTerrainTarget { get; }
        public bool Queued =>
            Failure == MiningUseFailure.None && MutationSequence > 0;
        public bool Succeeded => Queued || AffectedNonTerrainTarget;
    }

    public static class MiningTargetResolver
    {
        public static bool TryResolveAdjacent(
            GridPos originCell,
            Vector2 aim,
            int fallbackHorizontalFacing,
            out GridPos targetCell,
            out Vector2Int direction)
        {
            if (aim.sqrMagnitude <= 0.0001f)
            {
                int horizontal = fallbackHorizontalFacing < 0 ? -1 : 1;
                direction = new Vector2Int(horizontal, 0);
            }
            else if (Mathf.Abs(aim.y) > Mathf.Abs(aim.x))
            {
                direction = new Vector2Int(0, aim.y < 0f ? -1 : 1);
            }
            else
            {
                direction = new Vector2Int(aim.x < 0f ? -1 : 1, 0);
            }

            targetCell = new GridPos(
                originCell.X + direction.x,
                originCell.Y + direction.y);
            return true;
        }

        public static bool IsCardinalUnit(Vector2Int direction)
        {
            return Mathf.Abs(direction.x) + Mathf.Abs(direction.y) == 1;
        }
    }
}

#endif
