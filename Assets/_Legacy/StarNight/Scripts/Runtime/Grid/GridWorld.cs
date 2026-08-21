#if LEGACY_DISABLED
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace StarNight.Grid
{
    [DisallowMultipleComponent]
    public sealed class GridWorld : MonoBehaviour
    {
        public const float CellSize = 1f;

        [SerializeField] private GridLayout gridLayout;
        [SerializeField] private Tilemap terrainTilemap;
        [SerializeField] private Tilemap hazardTilemap;
        [SerializeField] private Vector2Int origin = Vector2Int.zero;
        [SerializeField] private Vector2Int size = new Vector2Int(30, 18);

        private readonly Dictionary<GridPos, Object> occupants = new Dictionary<GridPos, Object>();
        private readonly Dictionary<Object, HashSet<GridPos>> occupiedCellsByOwner =
            new Dictionary<Object, HashSet<GridPos>>();

        public GridLayout Layout => gridLayout;
        public Tilemap TerrainTilemap => terrainTilemap;
        public Tilemap HazardTilemap => hazardTilemap;
        public Vector2Int Origin => origin;
        public Vector2Int Size => size;
        public RectInt CellBounds => new RectInt(origin, size);

        public Rect WorldBounds
        {
            get
            {
                Vector3 min = gridLayout != null
                    ? gridLayout.CellToWorld(new Vector3Int(origin.x, origin.y, 0))
                    : new Vector3(origin.x, origin.y, 0f);
                Vector3 max = gridLayout != null
                    ? gridLayout.CellToWorld(new Vector3Int(origin.x + size.x, origin.y + size.y, 0))
                    : new Vector3(origin.x + size.x, origin.y + size.y, 0f);
                return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            }
        }

        public void Configure(
            GridLayout layout,
            Tilemap terrain,
            Tilemap hazards,
            Vector2Int worldOrigin,
            Vector2Int worldSize)
        {
            gridLayout = layout;
            terrainTilemap = terrain;
            hazardTilemap = hazards;
            origin = worldOrigin;
            size = worldSize;
        }

        public GridPos WorldToCell(Vector2 worldPosition)
        {
            if (gridLayout == null)
            {
                return new GridPos(
                    Mathf.FloorToInt(worldPosition.x),
                    Mathf.FloorToInt(worldPosition.y));
            }

            Vector3Int cell = gridLayout.WorldToCell(worldPosition);
            return new GridPos(cell.x, cell.y);
        }

        public Vector2 CellToWorldCenter(GridPos cell)
        {
            if (gridLayout == null)
            {
                return new Vector2(cell.X + 0.5f, cell.Y + 0.5f);
            }

            Vector3 bottomLeft = gridLayout.CellToWorld(new Vector3Int(cell.X, cell.Y, 0));
            return new Vector2(bottomLeft.x + 0.5f, bottomLeft.y + 0.5f);
        }

        public bool IsWithinBounds(GridPos cell)
        {
            return cell.X >= origin.x
                && cell.Y >= origin.y
                && cell.X < origin.x + size.x
                && cell.Y < origin.y + size.y;
        }

        public bool IsSolid(GridPos cell)
        {
            return terrainTilemap != null
                && terrainTilemap.HasTile(new Vector3Int(cell.X, cell.Y, 0));
        }

        public bool IsHazard(GridPos cell)
        {
            return hazardTilemap != null
                && hazardTilemap.HasTile(new Vector3Int(cell.X, cell.Y, 0));
        }

        public bool IsBlocked(GridPos cell)
        {
            return !IsWithinBounds(cell) || IsSolid(cell) || IsOccupied(cell);
        }

        public GridCellFlags GetCellFlags(GridPos cell)
        {
            if (!IsWithinBounds(cell))
            {
                return GridCellFlags.Solid;
            }

            GridCellFlags flags = GridCellFlags.None;
            if (IsSolid(cell))
            {
                flags |= GridCellFlags.Solid;
            }

            if (IsHazard(cell))
            {
                flags |= GridCellFlags.Hazard;
            }

            if (IsOccupied(cell))
            {
                flags |= GridCellFlags.Occupied;
            }

            if (CanStandAt(cell, new Vector2(0.72f, 0.90f)))
            {
                flags |= GridCellFlags.SafeCandidate;
            }

            return flags;
        }

        public bool CanStandAt(GridPos cell, Vector2 bodySize)
        {
            if (!IsWithinBounds(cell) || IsSolid(cell) || IsHazard(cell))
            {
                return false;
            }

            GridPos support = new GridPos(cell.X, cell.Y - 1);
            if (!IsSolid(support))
            {
                return false;
            }

            int requiredVerticalCells = Mathf.Max(1, Mathf.CeilToInt(bodySize.y - 0.001f));
            for (int offset = 0; offset < requiredVerticalCells; offset++)
            {
                if (IsSolid(new GridPos(cell.X, cell.Y + offset)))
                {
                    return false;
                }
            }

            return bodySize.x <= CellSize + 0.001f;
        }

        public bool TryOccupy(GridPos cell, Object occupant)
        {
            if (occupant == null)
            {
                return false;
            }

            if (occupants.TryGetValue(cell, out Object current))
            {
                return current == occupant;
            }

            if (!IsWithinBounds(cell) || IsSolid(cell))
            {
                return false;
            }

            occupants.Add(cell, occupant);
            if (!occupiedCellsByOwner.TryGetValue(
                    occupant,
                    out HashSet<GridPos> ownedCells))
            {
                ownedCells = new HashSet<GridPos>();
                occupiedCellsByOwner.Add(occupant, ownedCells);
            }

            ownedCells.Add(cell);
            return true;
        }

        public bool TryOccupy(
            IReadOnlyList<GridPos> cells,
            Object occupant)
        {
            if (occupant == null || cells == null || cells.Count == 0)
            {
                return false;
            }

            HashSet<GridPos> uniqueCells = new HashSet<GridPos>();
            for (int index = 0; index < cells.Count; index++)
            {
                GridPos cell = cells[index];
                if (!uniqueCells.Add(cell)
                    || !IsWithinBounds(cell)
                    || IsSolid(cell))
                {
                    return false;
                }

                if (occupants.TryGetValue(cell, out Object current)
                    && current != occupant)
                {
                    return false;
                }
            }

            ReleaseAll(occupant);
            foreach (GridPos cell in uniqueCells)
            {
                occupants[cell] = occupant;
            }

            occupiedCellsByOwner[occupant] = uniqueCells;
            return true;
        }

        public bool IsOccupied(GridPos cell)
        {
            return occupants.ContainsKey(cell);
        }

        public bool TryGetOccupant(GridPos cell, out Object occupant)
        {
            return occupants.TryGetValue(cell, out occupant);
        }

        public bool AnyOccupied(
            IReadOnlyList<GridPos> cells,
            Object ignoredOccupant = null)
        {
            if (cells == null)
            {
                return false;
            }

            for (int index = 0; index < cells.Count; index++)
            {
                if (occupants.TryGetValue(cells[index], out Object occupant)
                    && occupant != ignoredOccupant)
                {
                    return true;
                }
            }

            return false;
        }

        public bool Release(GridPos cell, Object occupant)
        {
            if (!occupants.TryGetValue(cell, out Object current) || current != occupant)
            {
                return false;
            }

            occupants.Remove(cell);
            if (occupiedCellsByOwner.TryGetValue(
                    occupant,
                    out HashSet<GridPos> ownedCells))
            {
                ownedCells.Remove(cell);
                if (ownedCells.Count == 0)
                {
                    occupiedCellsByOwner.Remove(occupant);
                }
            }

            return true;
        }

        public int ReleaseAll(Object occupant)
        {
            if (occupant == null
                || !occupiedCellsByOwner.TryGetValue(
                    occupant,
                    out HashSet<GridPos> ownedCells))
            {
                return 0;
            }

            int released = 0;
            foreach (GridPos cell in ownedCells)
            {
                if (occupants.TryGetValue(cell, out Object current)
                    && current == occupant)
                {
                    occupants.Remove(cell);
                    released++;
                }
            }

            occupiedCellsByOwner.Remove(occupant);
            return released;
        }

        public void ClearOccupancy()
        {
            occupants.Clear();
            occupiedCellsByOwner.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            Rect bounds = WorldBounds;
            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.7f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.15f);
            for (int x = 0; x <= size.x; x++)
            {
                float worldX = bounds.xMin + x;
                Gizmos.DrawLine(
                    new Vector3(worldX, bounds.yMin, 0f),
                    new Vector3(worldX, bounds.yMax, 0f));
            }

            for (int y = 0; y <= size.y; y++)
            {
                float worldY = bounds.yMin + y;
                Gizmos.DrawLine(
                    new Vector3(bounds.xMin, worldY, 0f),
                    new Vector3(bounds.xMax, worldY, 0f));
            }
        }
    }
}

#endif
