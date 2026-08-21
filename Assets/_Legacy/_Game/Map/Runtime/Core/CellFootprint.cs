#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Map
{
    [Serializable]
    public sealed class CellFootprint
    {
        public Vector2Int BoundsSize = Vector2Int.one;
        public Vector2Int PivotCell = Vector2Int.zero;
        public List<Vector2Int> OccupiedCells = new List<Vector2Int> { Vector2Int.zero };
        public List<Vector2Int> SupportRequiredCells = new List<Vector2Int>();
        public List<Vector2Int> ClearanceRequiredCells = new List<Vector2Int>();
        public List<Vector2Int> HazardCells = new List<Vector2Int>();
        public List<Vector2Int> TriggerCells = new List<Vector2Int>();

        public bool ContainsLocalCell(Vector2Int localCell)
        {
            return localCell.x >= 0 && localCell.y >= 0 &&
                   localCell.x < BoundsSize.x && localCell.y < BoundsSize.y;
        }

        public GridCell ToRoomCell(Vector2Int localCell, Vector2Int anchorCell)
        {
            return new GridCell(
                anchorCell.x + localCell.x - PivotCell.x,
                anchorCell.y + localCell.y - PivotCell.y);
        }

        public bool TryValidate(out string error)
        {
            if (BoundsSize.x <= 0 || BoundsSize.y <= 0)
            {
                error = "BoundsSize must be positive.";
                return false;
            }

            if (!ContainsLocalCell(PivotCell))
            {
                error = $"PivotCell {PivotCell} is outside BoundsSize {BoundsSize}.";
                return false;
            }

            if (OccupiedCells == null || OccupiedCells.Count == 0)
            {
                error = "OccupiedCells must contain at least one cell.";
                return false;
            }

            if (!TryValidateList(OccupiedCells, nameof(OccupiedCells), true, out error) ||
                !TryValidateList(SupportRequiredCells, nameof(SupportRequiredCells), false, out error) ||
                !TryValidateList(ClearanceRequiredCells, nameof(ClearanceRequiredCells), false, out error) ||
                !TryValidateList(HazardCells, nameof(HazardCells), false, out error) ||
                !TryValidateList(TriggerCells, nameof(TriggerCells), false, out error))
            {
                return false;
            }

            error = string.Empty;
            return true;
        }

        private bool TryValidateList(
            IReadOnlyList<Vector2Int> cells,
            string listName,
            bool requireInsideBounds,
            out string error)
        {
            if (cells == null)
            {
                error = $"{listName} cannot be null.";
                return false;
            }

            var uniqueCells = new HashSet<Vector2Int>();
            for (var index = 0; index < cells.Count; index++)
            {
                var cell = cells[index];
                if (requireInsideBounds && !ContainsLocalCell(cell))
                {
                    error = $"{listName} cell {cell} is outside BoundsSize {BoundsSize}.";
                    return false;
                }

                if (!uniqueCells.Add(cell))
                {
                    error = $"{listName} contains duplicate cell {cell}.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }
    }
}

#endif
