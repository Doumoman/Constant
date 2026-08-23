using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public sealed class MicrochunkObjectSlotValidationPolicy
    {
        private static readonly IReadOnlyList<MicrochunkTileLayer> ContractBlockingLayers =
            new ReadOnlyCollection<MicrochunkTileLayer>(new[]
            {
                MicrochunkTileLayer.GroundSolid,
                MicrochunkTileLayer.Breakable,
                MicrochunkTileLayer.Hazard,
                MicrochunkTileLayer.Liquid
            });

        private readonly IReadOnlyDictionary<string, MicrochunkObjectSlotPoolDefinition> poolsById;
        private readonly IReadOnlyList<MicrochunkObjectSlotPoolDefinition> poolDefinitions;
        private readonly IReadOnlyList<string> allowedMarkerCodes;
        private readonly HashSet<string> allowedMarkerCodeLookup;

        public IReadOnlyDictionary<string, MicrochunkObjectSlotPoolDefinition> PoolsById => poolsById;
        public IReadOnlyList<MicrochunkObjectSlotPoolDefinition> PoolDefinitions => poolDefinitions;
        public IReadOnlyList<string> AllowedMarkerCodes => allowedMarkerCodes;
        public IReadOnlyList<MicrochunkTileLayer> BlockingLayers => ContractBlockingLayers;

        public MicrochunkObjectSlotValidationPolicy(
            IEnumerable<MicrochunkObjectSlotPoolDefinition> poolDefinitions,
            IEnumerable<string> allowedMarkerCodes)
        {
            if (poolDefinitions == null) throw new ArgumentNullException(nameof(poolDefinitions));
            if (allowedMarkerCodes == null) throw new ArgumentNullException(nameof(allowedMarkerCodes));

            var pools = new SortedDictionary<string, MicrochunkObjectSlotPoolDefinition>(StringComparer.Ordinal);
            foreach (var pool in poolDefinitions)
            {
                if (pool == null)
                {
                    throw new ArgumentException("Pool definitions cannot contain null.", nameof(poolDefinitions));
                }
                if (pools.ContainsKey(pool.PoolId))
                {
                    throw new ArgumentException("Pool IDs must be unique.", nameof(poolDefinitions));
                }
                pools.Add(pool.PoolId, pool);
            }

            var markers = new List<string>();
            allowedMarkerCodeLookup = new HashSet<string>(StringComparer.Ordinal);
            foreach (var markerCode in allowedMarkerCodes)
            {
                if (string.IsNullOrWhiteSpace(markerCode))
                {
                    throw new ArgumentException("Allowed marker codes cannot be null, empty, or whitespace.", nameof(allowedMarkerCodes));
                }
                if (!allowedMarkerCodeLookup.Add(markerCode))
                {
                    throw new ArgumentException("Allowed marker codes must be unique.", nameof(allowedMarkerCodes));
                }
                markers.Add(markerCode);
            }

            markers.Sort(StringComparer.Ordinal);
            poolsById = new ReadOnlyDictionary<string, MicrochunkObjectSlotPoolDefinition>(pools);
            this.poolDefinitions = new ReadOnlyCollection<MicrochunkObjectSlotPoolDefinition>(
                new List<MicrochunkObjectSlotPoolDefinition>(pools.Values));
            this.allowedMarkerCodes = new ReadOnlyCollection<string>(markers);
        }

        public bool TryGetPool(string poolId, out MicrochunkObjectSlotPoolDefinition pool)
        {
            if (poolId == null)
            {
                pool = null;
                return false;
            }
            return poolsById.TryGetValue(poolId, out pool);
        }

        public bool IsAllowedMarkerCode(string markerCode)
        {
            return !string.IsNullOrEmpty(markerCode) && allowedMarkerCodeLookup.Contains(markerCode);
        }

        public bool IsBlocking(MicrochunkTileCell cell)
        {
            if (cell == null) throw new ArgumentNullException(nameof(cell));
            var occupancy = MicrochunkTileLayerOccupancy.FromCell(cell);
            foreach (var layer in ContractBlockingLayers)
            {
                if (occupancy.IsOccupied(layer)) return true;
            }
            return false;
        }
    }
}
