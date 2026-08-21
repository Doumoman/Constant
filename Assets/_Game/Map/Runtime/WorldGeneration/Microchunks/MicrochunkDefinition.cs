using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public sealed class MicrochunkDefinition
    {
        private readonly IReadOnlyList<string> biomeIds;
        private readonly IReadOnlyList<string> routeRoles;
        private readonly IReadOnlyList<MicrochunkTransform> allowedTransforms;
        private readonly IReadOnlyList<MicrochunkTileCell> tileCells;
        private readonly IReadOnlyList<MicrochunkSocketDefinition> sockets;
        private readonly IReadOnlyList<MicrochunkObjectSlotDefinition> objectSlots;

        public MicrochunkId Id { get; }
        public string DisplayName { get; }
        public int WidthTiles { get; }
        public int HeightTiles { get; }
        public MicrochunkUsageClass UsageClass { get; }
        public IReadOnlyList<string> BiomeIds => biomeIds;
        public IReadOnlyList<string> RouteRoles => routeRoles;
        public IReadOnlyList<MicrochunkTransform> AllowedTransforms => allowedTransforms;
        public int SelectionWeight { get; }
        public int Threat { get; }
        public int Cognitive { get; }
        public int Chain { get; }
        public bool TileDataComplete { get; }
        public string PrefabId { get; }
        public bool Active { get; }
        public string Notes { get; }
        public IReadOnlyList<MicrochunkTileCell> TileCells => tileCells;
        public IReadOnlyList<MicrochunkSocketDefinition> Sockets => sockets;
        public IReadOnlyList<MicrochunkObjectSlotDefinition> ObjectSlots => objectSlots;

        public MicrochunkDefinition(
            MicrochunkId id,
            string displayName,
            int widthTiles,
            int heightTiles,
            MicrochunkUsageClass usageClass,
            IEnumerable<string> biomeIds,
            IEnumerable<string> routeRoles,
            IEnumerable<MicrochunkTransform> allowedTransforms,
            int selectionWeight,
            int threat,
            int cognitive,
            int chain,
            bool tileDataComplete,
            string prefabId,
            bool active,
            string notes,
            IEnumerable<MicrochunkTileCell> tileCells,
            IEnumerable<MicrochunkSocketDefinition> sockets,
            IEnumerable<MicrochunkObjectSlotDefinition> objectSlots)
        {
            if (!id.IsValid) throw new ArgumentException("A valid microchunk ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Display name is required.", nameof(displayName));
            if (widthTiles != MicrochunkConstants.WidthTiles) throw new ArgumentOutOfRangeException(nameof(widthTiles));
            if (heightTiles != MicrochunkConstants.HeightTiles) throw new ArgumentOutOfRangeException(nameof(heightTiles));
            if (!Enum.IsDefined(typeof(MicrochunkUsageClass), usageClass)) throw new ArgumentOutOfRangeException(nameof(usageClass));
            if (selectionWeight < 0) throw new ArgumentOutOfRangeException(nameof(selectionWeight));
            if (threat < 0) throw new ArgumentOutOfRangeException(nameof(threat));
            if (cognitive < 0) throw new ArgumentOutOfRangeException(nameof(cognitive));
            if (chain < 0) throw new ArgumentOutOfRangeException(nameof(chain));
            if (string.IsNullOrWhiteSpace(prefabId)) throw new ArgumentException("Prefab ID is required.", nameof(prefabId));

            Id = id;
            DisplayName = displayName;
            WidthTiles = widthTiles;
            HeightTiles = heightTiles;
            UsageClass = usageClass;
            this.biomeIds = FreezeStrings(biomeIds, nameof(biomeIds));
            this.routeRoles = FreezeStrings(routeRoles, nameof(routeRoles));
            this.allowedTransforms = FreezeTransforms(allowedTransforms);
            SelectionWeight = selectionWeight;
            Threat = threat;
            Cognitive = cognitive;
            Chain = chain;
            TileDataComplete = tileDataComplete;
            PrefabId = prefabId;
            Active = active;
            Notes = notes ?? string.Empty;
            this.tileCells = FreezeCells(tileCells, tileDataComplete);
            this.sockets = FreezeSockets(sockets);
            this.objectSlots = FreezeObjectSlots(objectSlots);
        }

        private static IReadOnlyList<string> FreezeStrings(IEnumerable<string> source, string parameterName)
        {
            if (source == null) throw new ArgumentNullException(parameterName);
            var values = new List<string>();
            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in source)
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Metadata IDs cannot be null, empty, or whitespace.", parameterName);
                if (!unique.Add(value)) throw new ArgumentException("Metadata IDs must be unique.", parameterName);
                values.Add(value);
            }
            values.Sort(StringComparer.Ordinal);
            return new ReadOnlyCollection<string>(values);
        }

        private static IReadOnlyList<MicrochunkTransform> FreezeTransforms(IEnumerable<MicrochunkTransform> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var values = new List<MicrochunkTransform>();
            var unique = new HashSet<MicrochunkTransform>();
            foreach (var value in source)
            {
                if (!Enum.IsDefined(typeof(MicrochunkTransform), value)) throw new ArgumentOutOfRangeException(nameof(source));
                if (!unique.Add(value)) throw new ArgumentException("Allowed transforms must be unique.", nameof(source));
                values.Add(value);
            }
            if (values.Count == 0) throw new ArgumentException("At least one transform is required.", nameof(source));
            values.Sort();
            return new ReadOnlyCollection<MicrochunkTransform>(values);
        }

        private static IReadOnlyList<MicrochunkTileCell> FreezeCells(
            IEnumerable<MicrochunkTileCell> source,
            bool tileDataComplete)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var values = new List<MicrochunkTileCell>();
            var unique = new HashSet<MicrochunkLocalCoord>();
            foreach (var value in source)
            {
                if (value == null) throw new ArgumentException("Tile cells cannot contain null.", nameof(source));
                if (!unique.Add(value.Coordinate)) throw new ArgumentException("Tile cell coordinates must be unique.", nameof(source));
                values.Add(value);
            }
            if (tileDataComplete && values.Count != MicrochunkConstants.CellCount)
            {
                throw new ArgumentException($"Complete tile data requires exactly {MicrochunkConstants.CellCount} cells.", nameof(source));
            }
            values.Sort((left, right) => left.Coordinate.CompareTo(right.Coordinate));
            return new ReadOnlyCollection<MicrochunkTileCell>(values);
        }

        private static IReadOnlyList<MicrochunkSocketDefinition> FreezeSockets(
            IEnumerable<MicrochunkSocketDefinition> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var values = new List<MicrochunkSocketDefinition>();
            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in source)
            {
                if (value == null) throw new ArgumentException("Sockets cannot contain null.", nameof(source));
                if (!unique.Add(value.SocketId)) throw new ArgumentException("Socket IDs must be unique.", nameof(source));
                values.Add(value);
            }
            values.Sort((left, right) => string.Compare(left.SocketId, right.SocketId, StringComparison.Ordinal));
            return new ReadOnlyCollection<MicrochunkSocketDefinition>(values);
        }

        private static IReadOnlyList<MicrochunkObjectSlotDefinition> FreezeObjectSlots(
            IEnumerable<MicrochunkObjectSlotDefinition> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var values = new List<MicrochunkObjectSlotDefinition>();
            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in source)
            {
                if (value == null) throw new ArgumentException("Object slots cannot contain null.", nameof(source));
                if (!unique.Add(value.SlotId)) throw new ArgumentException("Object slot IDs must be unique.", nameof(source));
                values.Add(value);
            }
            values.Sort((left, right) => string.Compare(left.SlotId, right.SlotId, StringComparison.Ordinal));
            return new ReadOnlyCollection<MicrochunkObjectSlotDefinition>(values);
        }
    }
}
