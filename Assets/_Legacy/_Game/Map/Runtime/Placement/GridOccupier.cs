#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Map.Placement
{
    [DisallowMultipleComponent]
    public sealed class GridOccupier : MonoBehaviour
    {
        [SerializeField] private Vector2Int anchorCell;
        [SerializeField] private CellFootprint footprint = new CellFootprint();
        [SerializeField] private OccupancyLayer occupiedCellLayers = OccupancyLayer.Fixture;

        public Vector2Int AnchorCell => anchorCell;
        public CellFootprint Footprint => footprint;
        public OccupancyLayer OccupiedCellLayers => occupiedCellLayers;

        public void Configure(
            Vector2Int newAnchorCell,
            CellFootprint newFootprint,
            OccupancyLayer newOccupiedCellLayers)
        {
            anchorCell = newAnchorCell;
            footprint = newFootprint ?? throw new ArgumentNullException(nameof(newFootprint));
            occupiedCellLayers = newOccupiedCellLayers;
        }

        public bool TryGetClaims(out IReadOnlyDictionary<GridCell, OccupancyLayer> claims, out string error)
        {
            var mutableClaims = new Dictionary<GridCell, OccupancyLayer>();
            claims = mutableClaims;

            if (footprint == null)
            {
                error = "CellFootprint is missing.";
                return false;
            }

            if (!footprint.TryValidate(out error))
            {
                return false;
            }

            if (occupiedCellLayers == OccupancyLayer.None)
            {
                error = "OccupiedCellLayers cannot be None.";
                return false;
            }

            AddClaims(mutableClaims, footprint.OccupiedCells, occupiedCellLayers);
            AddClaims(mutableClaims, footprint.HazardCells, OccupancyLayer.Hazard);
            AddClaims(mutableClaims, footprint.TriggerCells, OccupancyLayer.Logic);

            error = string.Empty;
            return true;
        }

        public IEnumerable<GridCell> GetSupportRequiredRoomCells()
        {
            return TranslateCells(footprint?.SupportRequiredCells);
        }

        public IEnumerable<GridCell> GetClearanceRequiredRoomCells()
        {
            return TranslateCells(footprint?.ClearanceRequiredCells);
        }

        private void AddClaims(
            IDictionary<GridCell, OccupancyLayer> claims,
            IReadOnlyList<Vector2Int> localCells,
            OccupancyLayer layer)
        {
            for (var index = 0; index < localCells.Count; index++)
            {
                var roomCell = footprint.ToRoomCell(localCells[index], anchorCell);
                claims.TryGetValue(roomCell, out var currentLayers);
                claims[roomCell] = currentLayers | layer;
            }
        }

        private IEnumerable<GridCell> TranslateCells(IReadOnlyList<Vector2Int> localCells)
        {
            if (footprint == null || localCells == null)
            {
                yield break;
            }

            for (var index = 0; index < localCells.Count; index++)
            {
                yield return footprint.ToRoomCell(localCells[index], anchorCell);
            }
        }
    }
}

#endif
