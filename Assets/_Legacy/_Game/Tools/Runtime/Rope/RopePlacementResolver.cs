#if LEGACY_DISABLED
using StarNight.Interaction.State;
using StarNight.Map;
using UnityEngine;

namespace StarNight.Tools.Rope
{
    public enum RopeAnchorKind
    {
        Ceiling,
        CommonAnchor,
        StarKnot,
    }

    public enum RopePlacementFailure
    {
        None,
        InsufficientClearance,
        AnchorOutsideRoom,
        ActiveLaser,
        PortalBoundary,
        ExistingRopeColumn,
        NoSegments,
    }

    public readonly struct RopePlacementPlan
    {
        public RopePlacementPlan(
            RopeAnchorKind anchorKind,
            Vector2Int anchorCell,
            Vector2Int[] segmentCells)
        {
            AnchorKind = anchorKind;
            AnchorCell = anchorCell;
            SegmentCells = segmentCells ?? System.Array.Empty<Vector2Int>();
        }

        public RopeAnchorKind AnchorKind { get; }
        public Vector2Int AnchorCell { get; }
        public Vector2Int[] SegmentCells { get; }
    }

    public interface IRopePlacementWorld
    {
        bool IsInsideRoom(Vector2Int cell);
        bool IsSolid(Vector2Int cell);
        bool HasCommonRopeAnchor(Vector2Int cell);
        bool IsActiveLaser(Vector2Int cell);
        bool IsPortalBoundary(Vector2Int cell);
        bool HasRopeInColumn(int columnX);
    }

    public sealed class RopePlacementResolver
    {
        public bool TryResolve(
            Vector2Int playerHeadCell,
            RopeDefinition definition,
            IRopePlacementWorld world,
            out RopePlacementPlan plan,
            out RopePlacementFailure failure)
        {
            plan = default;
            failure = RopePlacementFailure.None;
            if (definition == null || world == null)
            {
                failure = RopePlacementFailure.NoSegments;
                return false;
            }

            if (world.HasRopeInColumn(playerHeadCell.x))
            {
                failure = RopePlacementFailure.ExistingRopeColumn;
                return false;
            }

            int emptyCells = 0;
            Vector2Int anchorCell = default;
            Vector2Int firstSegmentCell = default;
            RopeAnchorKind anchorKind = RopeAnchorKind.StarKnot;
            bool foundAnchor = false;
            for (int step = 1; step <= definition.MaximumScanCells; step++)
            {
                Vector2Int cell = playerHeadCell + Vector2Int.up * step;
                if (!world.IsInsideRoom(cell))
                {
                    failure = RopePlacementFailure.AnchorOutsideRoom;
                    return false;
                }

                if (world.HasCommonRopeAnchor(cell))
                {
                    if (emptyCells < 2)
                    {
                        failure = RopePlacementFailure.InsufficientClearance;
                        return false;
                    }
                    anchorKind = RopeAnchorKind.CommonAnchor;
                    anchorCell = cell;
                    firstSegmentCell = cell + Vector2Int.down;
                    foundAnchor = true;
                    break;
                }

                if (world.IsSolid(cell))
                {
                    if (emptyCells < 2)
                    {
                        failure = RopePlacementFailure.InsufficientClearance;
                        return false;
                    }
                    anchorKind = RopeAnchorKind.Ceiling;
                    anchorCell = cell + Vector2Int.down;
                    firstSegmentCell = anchorCell;
                    foundAnchor = true;
                    break;
                }

                emptyCells++;
                if (step == definition.MaximumScanCells)
                {
                    anchorKind = RopeAnchorKind.StarKnot;
                    anchorCell = cell;
                    firstSegmentCell = cell;
                    foundAnchor = true;
                }
            }

            if (!foundAnchor || emptyCells < 2)
            {
                failure = RopePlacementFailure.InsufficientClearance;
                return false;
            }
            if (!world.IsInsideRoom(anchorCell))
            {
                failure = RopePlacementFailure.AnchorOutsideRoom;
                return false;
            }
            if (world.IsActiveLaser(anchorCell))
            {
                failure = RopePlacementFailure.ActiveLaser;
                return false;
            }
            if (world.IsPortalBoundary(anchorCell))
            {
                failure = RopePlacementFailure.PortalBoundary;
                return false;
            }

            var cells = new System.Collections.Generic.List<Vector2Int>(definition.MaximumLengthCells);
            for (int index = 0; index < definition.MaximumLengthCells; index++)
            {
                Vector2Int cell = firstSegmentCell + Vector2Int.down * index;
                if (!world.IsInsideRoom(cell)
                    || world.IsSolid(cell)
                    || world.IsPortalBoundary(cell))
                {
                    break;
                }
                cells.Add(cell);
            }
            if (cells.Count == 0)
            {
                failure = RopePlacementFailure.NoSegments;
                return false;
            }

            plan = new RopePlacementPlan(anchorKind, anchorCell, cells.ToArray());
            return true;
        }
    }

    [DisallowMultipleComponent]
    public sealed class RopeLaserActiveVolume : MonoBehaviour
    {
        [SerializeField] private bool active = true;
        public bool Active => active;
        public void SetActive(bool value) => active = value;
    }

    public sealed class PhysicsRopePlacementWorld : IRopePlacementWorld
    {
        private readonly RectInt roomBounds;
        private readonly Vector2 gridOrigin;
        private readonly float cellSize;
        private readonly ProjectPhysicsProfile physicsProfile;

        public PhysicsRopePlacementWorld(
            RectInt bounds,
            Vector2 origin,
            float size,
            ProjectPhysicsProfile profile)
        {
            roomBounds = bounds;
            gridOrigin = origin;
            cellSize = Mathf.Max(0.01f, size);
            physicsProfile = profile;
        }

        public bool IsInsideRoom(Vector2Int cell)
        {
            return roomBounds.width <= 0 || roomBounds.height <= 0 || roomBounds.Contains(cell);
        }

        public bool IsSolid(Vector2Int cell)
        {
            int mask = LayerMask.GetMask("TerrainSolid", "TerrainOneWay", "UnbreakableBoundary");
            return Physics2D.OverlapBox(CellWorld(cell), Vector2.one * cellSize * 0.82f, 0f, mask) != null;
        }

        public bool HasCommonRopeAnchor(Vector2Int cell)
        {
            Collider2D[] overlaps = Physics2D.OverlapBoxAll(CellWorld(cell), Vector2.one * cellSize * 0.82f, 0f);
            for (int index = 0; index < overlaps.Length; index++)
            {
                if (overlaps[index] != null && overlaps[index].GetComponentInParent<RopeAnchorMarker>() != null)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsActiveLaser(Vector2Int cell)
        {
            Collider2D[] overlaps = Physics2D.OverlapBoxAll(CellWorld(cell), Vector2.one * cellSize * 0.82f, 0f);
            for (int index = 0; index < overlaps.Length; index++)
            {
                RopeLaserActiveVolume laser = overlaps[index] != null
                    ? overlaps[index].GetComponentInParent<RopeLaserActiveVolume>()
                    : null;
                if (laser != null && laser.Active)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsPortalBoundary(Vector2Int cell)
        {
            int mask = physicsProfile != null
                ? physicsProfile.PortalBoundaryMask.value
                : LayerMask.GetMask("PortalBoundary");
            return mask != 0
                && Physics2D.OverlapBox(CellWorld(cell), Vector2.one * cellSize * 0.82f, 0f, mask) != null;
        }

        public bool HasRopeInColumn(int columnX) => RopeInstallationRegistry.FindInColumn(columnX, roomBounds) != null;

        private Vector2 CellWorld(Vector2Int cell) => gridOrigin + (Vector2)cell * cellSize;
    }
}

#endif
