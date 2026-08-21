#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Grid;
using StarNight.Tiles;

namespace StarNight.Tools.Rope
{
    public enum RopeAnchorKind
    {
        Ring = 0,
        Ceiling = 1
    }

    public enum RopeInstallFailure
    {
        None = 0,
        MissingGridWorld,
        ExitAlreadyUnreachable,
        UseCellOutOfBounds,
        NoAnchorWithinRange,
        NoClimbableCells,
        SolidCellInSpan,
        OccupiedCellInSpan,
        ProtectedRouteCell
    }

    public enum RopeDamageKind
    {
        Fire = 0,
        Explosion = 1
    }

    public sealed class RopeInstallPlan
    {
        public RopeInstallPlan(
            GridPos useCell,
            GridPos anchorCell,
            RopeAnchorKind anchorKind,
            IReadOnlyList<GridPos> climbableCells)
        {
            UseCell = useCell;
            AnchorCell = anchorCell;
            AnchorKind = anchorKind;
            ClimbableCells = climbableCells ?? Array.Empty<GridPos>();
        }

        public GridPos UseCell { get; }
        public GridPos AnchorCell { get; }
        public RopeAnchorKind AnchorKind { get; }
        public IReadOnlyList<GridPos> ClimbableCells { get; }
        public int Length => ClimbableCells.Count;
    }

    public static class RopePlacementSolver
    {
        public const int DefaultMaximumLength = 6;

        public static bool TryBuildPlan(
            GridWorld gridWorld,
            TileMutationService mutationService,
            GridPos useCell,
            int maximumLength,
            out RopeInstallPlan plan,
            out RopeInstallFailure failure)
        {
            plan = null;
            if (gridWorld == null)
            {
                failure = RopeInstallFailure.MissingGridWorld;
                return false;
            }

            if (mutationService != null
                && !mutationService.IsCurrentExitReachable())
            {
                failure = RopeInstallFailure.ExitAlreadyUnreachable;
                return false;
            }

            if (!gridWorld.IsWithinBounds(useCell))
            {
                failure = RopeInstallFailure.UseCellOutOfBounds;
                return false;
            }

            if (IsProtectedRouteCell(mutationService, useCell))
            {
                failure = RopeInstallFailure.ProtectedRouteCell;
                return false;
            }

            int clampedLength = Math.Min(
                DefaultMaximumLength,
                Math.Max(1, maximumLength));
            GridPos anchorCell = default;
            GridPos topClimbableCell = default;
            RopeAnchorKind anchorKind = RopeAnchorKind.Ring;
            bool foundAnchor = false;

            for (int offset = 1; offset <= clampedLength; offset++)
            {
                GridPos candidate = new GridPos(useCell.X, useCell.Y + offset);
                if (!gridWorld.IsWithinBounds(candidate))
                {
                    break;
                }

                if (RopeAnchor2D.TryFind(gridWorld, candidate, out RopeAnchor2D anchor))
                {
                    anchorCell = candidate;
                    anchorKind = anchor.AnchorKind;
                    topClimbableCell = anchor.AnchorKind == RopeAnchorKind.Ceiling
                        ? new GridPos(candidate.X, candidate.Y - 1)
                        : candidate;
                    foundAnchor = true;
                    break;
                }

                if (gridWorld.IsSolid(candidate))
                {
                    anchorCell = candidate;
                    anchorKind = RopeAnchorKind.Ceiling;
                    topClimbableCell = new GridPos(candidate.X, candidate.Y - 1);
                    foundAnchor = true;
                    break;
                }
            }

            if (!foundAnchor)
            {
                failure = RopeInstallFailure.NoAnchorWithinRange;
                return false;
            }

            if (topClimbableCell.Y < useCell.Y)
            {
                failure = RopeInstallFailure.NoClimbableCells;
                return false;
            }

            List<GridPos> cells = new List<GridPos>(
                topClimbableCell.Y - useCell.Y);
            for (int y = useCell.Y + 1; y <= topClimbableCell.Y; y++)
            {
                GridPos cell = new GridPos(useCell.X, y);
                if (gridWorld.IsSolid(cell))
                {
                    failure = RopeInstallFailure.SolidCellInSpan;
                    return false;
                }

                bool isRingAnchorCell =
                    anchorKind == RopeAnchorKind.Ring
                    && cell == anchorCell;
                if (gridWorld.IsOccupied(cell) && !isRingAnchorCell)
                {
                    failure = RopeInstallFailure.OccupiedCellInSpan;
                    return false;
                }

                if (IsProtectedRouteCell(mutationService, cell))
                {
                    failure = RopeInstallFailure.ProtectedRouteCell;
                    return false;
                }

                cells.Add(cell);
            }

            if (cells.Count == 0)
            {
                failure = RopeInstallFailure.NoClimbableCells;
                return false;
            }

            plan = new RopeInstallPlan(
                useCell,
                anchorCell,
                anchorKind,
                cells);
            failure = RopeInstallFailure.None;
            return true;
        }

        private static bool IsProtectedRouteCell(
            TileMutationService mutationService,
            GridPos cell)
        {
            if (mutationService == null)
            {
                return false;
            }

            return mutationService.IsProtectedCell(cell)
                || mutationService.RequiredStart == cell
                || mutationService.RequiredExit == cell;
        }
    }
}

#endif
