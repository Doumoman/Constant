#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StarNight.Map.Placement
{
    public readonly struct OccupancyConflict
    {
        public OccupancyConflict(
            GridCell cell,
            OccupancyLayer existingLayers,
            OccupancyLayer incomingLayers,
            GridOccupier existingOccupier,
            string reason)
        {
            Cell = cell;
            ExistingLayers = existingLayers;
            IncomingLayers = incomingLayers;
            ExistingOccupier = existingOccupier;
            Reason = reason ?? string.Empty;
        }

        public GridCell Cell { get; }
        public OccupancyLayer ExistingLayers { get; }
        public OccupancyLayer IncomingLayers { get; }
        public GridOccupier ExistingOccupier { get; }
        public string Reason { get; }
        public bool HasConflict => !string.IsNullOrEmpty(Reason);

        public override string ToString()
        {
            return Reason;
        }
    }

    [DisallowMultipleComponent]
    public sealed class RoomElementRegistry : MonoBehaviour
    {
        private sealed class CellEntry
        {
            public CellEntry(GridOccupier occupier, OccupancyLayer layers)
            {
                Occupier = occupier;
                Layers = layers;
            }

            public GridOccupier Occupier { get; }
            public OccupancyLayer Layers { get; }
        }

        private readonly Dictionary<GridCell, List<CellEntry>> entriesByCell =
            new Dictionary<GridCell, List<CellEntry>>();

        private readonly Dictionary<GridOccupier, GridCell[]> cellsByOccupier =
            new Dictionary<GridOccupier, GridCell[]>();

        public int RegisteredOccupierCount => cellsByOccupier.Count;
        public int OccupiedCellCount => entriesByCell.Count;

        public bool IsRegistered(GridOccupier occupier)
        {
            return occupier != null && cellsByOccupier.ContainsKey(occupier);
        }

        public bool CanRegister(GridOccupier occupier, out OccupancyConflict conflict)
        {
            if (occupier == null)
            {
                conflict = InvalidConflict("GridOccupier is missing.");
                return false;
            }

            if (cellsByOccupier.ContainsKey(occupier))
            {
                conflict = InvalidConflict($"{occupier.name} is already registered.");
                return false;
            }

            if (!occupier.TryGetClaims(out var incomingClaims, out var validationError))
            {
                conflict = InvalidConflict(validationError);
                return false;
            }

            foreach (var claim in incomingClaims)
            {
                if (!entriesByCell.TryGetValue(claim.Key, out var existingEntries))
                {
                    continue;
                }

                for (var index = 0; index < existingEntries.Count; index++)
                {
                    var existing = existingEntries[index];
                    if (OccupancyRules.CanOverlap(existing.Layers, claim.Value))
                    {
                        continue;
                    }

                    conflict = new OccupancyConflict(
                        claim.Key,
                        existing.Layers,
                        claim.Value,
                        existing.Occupier,
                        $"Cell {claim.Key} occupancy conflict: {existing.Layers} vs {claim.Value}.");
                    return false;
                }
            }

            foreach (var clearanceCell in occupier.GetClearanceRequiredRoomCells())
            {
                var blockingLayers = GetLayers(clearanceCell) & OccupancyRules.ClearanceBlockingLayers;
                if (blockingLayers == OccupancyLayer.None)
                {
                    continue;
                }

                conflict = new OccupancyConflict(
                    clearanceCell,
                    blockingLayers,
                    OccupancyLayer.None,
                    GetFirstOccupier(clearanceCell),
                    $"Clearance cell {clearanceCell} is blocked by {blockingLayers}.");
                return false;
            }

            conflict = default;
            return true;
        }

        public bool TryRegister(GridOccupier occupier, out OccupancyConflict conflict)
        {
            if (!CanRegister(occupier, out conflict))
            {
                return false;
            }

            if (!occupier.TryGetClaims(out var incomingClaims, out var validationError))
            {
                conflict = InvalidConflict(validationError);
                return false;
            }

            var registeredCells = new GridCell[incomingClaims.Count];
            var cellIndex = 0;
            foreach (var claim in incomingClaims)
            {
                if (!entriesByCell.TryGetValue(claim.Key, out var cellEntries))
                {
                    cellEntries = new List<CellEntry>();
                    entriesByCell.Add(claim.Key, cellEntries);
                }

                cellEntries.Add(new CellEntry(occupier, claim.Value));
                registeredCells[cellIndex++] = claim.Key;
            }

            cellsByOccupier.Add(occupier, registeredCells);
            conflict = default;
            return true;
        }

        public bool Unregister(GridOccupier occupier)
        {
            if (occupier == null || !cellsByOccupier.TryGetValue(occupier, out var cells))
            {
                return false;
            }

            for (var cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                var cell = cells[cellIndex];
                if (!entriesByCell.TryGetValue(cell, out var entries))
                {
                    continue;
                }

                entries.RemoveAll(entry => entry.Occupier == occupier);
                if (entries.Count == 0)
                {
                    entriesByCell.Remove(cell);
                }
            }

            cellsByOccupier.Remove(occupier);
            return true;
        }

        public OccupancyLayer GetLayers(GridCell cell)
        {
            if (!entriesByCell.TryGetValue(cell, out var entries))
            {
                return OccupancyLayer.None;
            }

            var layers = OccupancyLayer.None;
            for (var index = 0; index < entries.Count; index++)
            {
                layers |= entries[index].Layers;
            }

            return layers;
        }

        public IReadOnlyList<GridOccupier> GetOccupiers(GridCell cell)
        {
            if (!entriesByCell.TryGetValue(cell, out var entries))
            {
                return Array.Empty<GridOccupier>();
            }

            return entries.Select(entry => entry.Occupier).Distinct().ToArray();
        }

        public void Clear()
        {
            entriesByCell.Clear();
            cellsByOccupier.Clear();
        }

        private GridOccupier GetFirstOccupier(GridCell cell)
        {
            return entriesByCell.TryGetValue(cell, out var entries) && entries.Count > 0
                ? entries[0].Occupier
                : null;
        }

        private static OccupancyConflict InvalidConflict(string reason)
        {
            return new OccupancyConflict(
                default,
                OccupancyLayer.None,
                OccupancyLayer.None,
                null,
                reason);
        }
    }
}

#endif
