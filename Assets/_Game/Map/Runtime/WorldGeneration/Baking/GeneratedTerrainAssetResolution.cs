using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Baking
{
    public sealed class GeneratedTerrainTileCode :
        IEquatable<GeneratedTerrainTileCode>, IComparable<GeneratedTerrainTileCode>
    {
        public GeneratedTerrainTileCode(string value)
        {
            Value = value ?? string.Empty;
        }

        public string Value { get; }
        public bool IsValid => GeneratedTerrainAssetKey.IsValid(Value);

        public static GeneratedTerrainTileCode FromLayer(GeneratedMicroChunkLayerRecord layer) =>
            layer == null
                ? new GeneratedTerrainTileCode(string.Empty)
                : new GeneratedTerrainTileCode(string.Join("/", new[]
                {
                    "FINAL", layer.Layer.ToString().ToUpperInvariant(),
                    layer.CellKind.ToString().ToUpperInvariant(),
                }));

        public int CompareTo(GeneratedTerrainTileCode other) => other == null
            ? -1 : string.Compare(Value, other.Value, StringComparison.Ordinal);
        public bool Equals(GeneratedTerrainTileCode other) => other != null &&
            string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => Equals(obj as GeneratedTerrainTileCode);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
    }

    public sealed class GeneratedTerrainPrefabId :
        IEquatable<GeneratedTerrainPrefabId>, IComparable<GeneratedTerrainPrefabId>
    {
        public GeneratedTerrainPrefabId(string value)
        {
            Value = value ?? string.Empty;
        }

        public string Value { get; }
        public bool IsValid => GeneratedTerrainAssetKey.IsValid(Value);

        public static GeneratedTerrainPrefabId FromSlot(GeneratedMarkerSlot slot)
        {
            if (slot == null || string.IsNullOrEmpty(slot.SourceKey))
                return new GeneratedTerrainPrefabId(string.Empty);
            return new GeneratedTerrainPrefabId(string.Join("/", new[]
            {
                "MARKER", slot.Kind.ToString().ToUpperInvariant(),
                slot.Owner.ToString().ToUpperInvariant(),
                BakingCanonicalDigest.HashCanonicalText(slot.SourceKey).Substring(0, 16),
            }));
        }

        public int CompareTo(GeneratedTerrainPrefabId other) => other == null
            ? -1 : string.Compare(Value, other.Value, StringComparison.Ordinal);
        public bool Equals(GeneratedTerrainPrefabId other) => other != null &&
            string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => Equals(obj as GeneratedTerrainPrefabId);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
    }

    public sealed class GeneratedTerrainTileRegistryEntry :
        IComparable<GeneratedTerrainTileRegistryEntry>
    {
        public GeneratedTerrainTileRegistryEntry(GeneratedTerrainTileCode code, string assetKey)
        {
            Code = code;
            AssetKey = assetKey ?? string.Empty;
        }

        public GeneratedTerrainTileCode Code { get; }
        public string AssetKey { get; }
        public bool IsValid => Code != null && Code.IsValid &&
            GeneratedTerrainAssetKey.IsValid(AssetKey);
        public string StableToken => string.Join("|", new[]
        {
            "TILE", Code == null ? "MISSING" : Code.Value, AssetKey,
        });
        public int CompareTo(GeneratedTerrainTileRegistryEntry other)
        {
            if (other == null) return -1;
            var comparison = Compare(Code, other.Code);
            return comparison != 0 ? comparison :
                string.Compare(AssetKey, other.AssetKey, StringComparison.Ordinal);
        }
        private static int Compare(GeneratedTerrainTileCode left, GeneratedTerrainTileCode right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            return right == null ? -1 : left.CompareTo(right);
        }
    }

    public sealed class GeneratedTerrainPrefabRegistryEntry :
        IComparable<GeneratedTerrainPrefabRegistryEntry>
    {
        public GeneratedTerrainPrefabRegistryEntry(GeneratedTerrainPrefabId id, string assetKey)
        {
            Id = id;
            AssetKey = assetKey ?? string.Empty;
        }

        public GeneratedTerrainPrefabId Id { get; }
        public string AssetKey { get; }
        public bool IsValid => Id != null && Id.IsValid &&
            GeneratedTerrainAssetKey.IsValid(AssetKey);
        public string StableToken => string.Join("|", new[]
        {
            "PREFAB", Id == null ? "MISSING" : Id.Value, AssetKey,
        });
        public int CompareTo(GeneratedTerrainPrefabRegistryEntry other)
        {
            if (other == null) return -1;
            var comparison = Compare(Id, other.Id);
            return comparison != 0 ? comparison :
                string.Compare(AssetKey, other.AssetKey, StringComparison.Ordinal);
        }
        private static int Compare(GeneratedTerrainPrefabId left, GeneratedTerrainPrefabId right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            return right == null ? -1 : left.CompareTo(right);
        }
    }

    public sealed class GeneratedTerrainAssetRegistrySnapshot
    {
        private readonly ReadOnlyCollection<GeneratedTerrainTileRegistryEntry> tileEntries;
        private readonly ReadOnlyCollection<GeneratedTerrainPrefabRegistryEntry> prefabEntries;

        public GeneratedTerrainAssetRegistrySnapshot(
            IEnumerable<GeneratedTerrainTileRegistryEntry> sourceTiles,
            IEnumerable<GeneratedTerrainPrefabRegistryEntry> sourcePrefabs,
            string publicationLabel = ReferencePublicationLabel)
        {
            var rawTiles = (sourceTiles ?? Array.Empty<GeneratedTerrainTileRegistryEntry>()).ToArray();
            var rawPrefabs = (sourcePrefabs ?? Array.Empty<GeneratedTerrainPrefabRegistryEntry>()).ToArray();
            NullTileEntryCount = rawTiles.Count(value => value == null);
            NullPrefabEntryCount = rawPrefabs.Count(value => value == null);
            tileEntries = new ReadOnlyCollection<GeneratedTerrainTileRegistryEntry>(rawTiles
                .Where(value => value != null).OrderBy(value => value).ToArray());
            prefabEntries = new ReadOnlyCollection<GeneratedTerrainPrefabRegistryEntry>(rawPrefabs
                .Where(value => value != null).OrderBy(value => value).ToArray());
            PublicationLabel = publicationLabel ?? string.Empty;
            Digest = GeneratedTerrainAssetRegistryDigest.Compute(this);
        }

        public const string PolicyVersion = "MAP17_01_ASSET_REGISTRY_V1";
        public const string ReferencePublicationLabel = "REFERENCE MAP17_01 ASSET REGISTRY";

        public IReadOnlyList<GeneratedTerrainTileRegistryEntry> TileEntries => tileEntries;
        public IReadOnlyList<GeneratedTerrainPrefabRegistryEntry> PrefabEntries => prefabEntries;
        public string PublicationLabel { get; }
        public string Digest { get; }
        public int NullTileEntryCount { get; }
        public int NullPrefabEntryCount { get; }
        public int InvalidTileEntryCount => tileEntries.Count(value => !value.IsValid);
        public int InvalidPrefabEntryCount => prefabEntries.Count(value => !value.IsValid);
        public int DuplicateTileCodeCount => tileEntries.Where(value => value.Code != null)
            .GroupBy(value => value.Code.Value, StringComparer.Ordinal)
            .Sum(group => Math.Max(0, group.Count() - 1));
        public int DuplicatePrefabIdCount => prefabEntries.Where(value => value.Id != null)
            .GroupBy(value => value.Id.Value, StringComparer.Ordinal)
            .Sum(group => Math.Max(0, group.Count() - 1));
        public bool IsValid => NullTileEntryCount == 0 && NullPrefabEntryCount == 0 &&
            InvalidTileEntryCount == 0 && InvalidPrefabEntryCount == 0 &&
            DuplicateTileCodeCount == 0 && DuplicatePrefabIdCount == 0 &&
            BakingCanonicalDigest.IsLowerHexSha256(Digest);
        public bool IsProductionAssetApproval => false;
        public int ProductionSeedApprovalCount => 0;

        public static GeneratedTerrainAssetRegistrySnapshot CreateReference(
            GeneratedMicroChunkSliceSet sliceSet,
            GeneratedMicroChunkMarkerSlotSet slotSet)
        {
            var tiles = sliceSet == null
                ? Array.Empty<GeneratedTerrainTileRegistryEntry>()
                : sliceSet.Slices.SelectMany(slice => slice.Cells)
                    .SelectMany(cell => cell.Layers)
                    .Select(GeneratedTerrainTileCode.FromLayer)
                    .Where(code => code.IsValid).Distinct().OrderBy(code => code)
                    .Select(code => new GeneratedTerrainTileRegistryEntry(
                        code, "REFERENCE_TILE/" + code.Value)).ToArray();
            var prefabs = slotSet == null
                ? Array.Empty<GeneratedTerrainPrefabRegistryEntry>()
                : slotSet.Slots.Select(GeneratedTerrainPrefabId.FromSlot)
                    .Where(id => id.IsValid).Distinct().OrderBy(id => id)
                    .Select(id => new GeneratedTerrainPrefabRegistryEntry(
                        id, "REFERENCE_PREFAB/" + id.Value)).ToArray();
            return new GeneratedTerrainAssetRegistrySnapshot(tiles, prefabs);
        }
    }

    public enum GeneratedTerrainAssetResolutionFailureCode
    {
        MissingRegistry = 1,
        InvalidRegistryEntry = 2,
        DuplicateTileCode = 3,
        DuplicatePrefabId = 4,
        InvalidTileCode = 5,
        InvalidPrefabId = 6,
        MissingTileCode = 7,
        MissingPrefabId = 8,
        InvalidRegistryDigest = 9,
    }

    public sealed class GeneratedTerrainAssetResolutionFailure :
        IComparable<GeneratedTerrainAssetResolutionFailure>,
        IEquatable<GeneratedTerrainAssetResolutionFailure>
    {
        public GeneratedTerrainAssetResolutionFailure(
            GeneratedTerrainAssetResolutionFailureCode code,
            string subject,
            string reason)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public GeneratedTerrainAssetResolutionFailureCode Code { get; }
        public string Subject { get; }
        public string Reason { get; }
        public string StableToken => Code + "|" + Subject + "|" + Reason;
        public int CompareTo(GeneratedTerrainAssetResolutionFailure other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0 ? comparison :
                string.Compare(Reason, other.Reason, StringComparison.Ordinal);
        }
        public bool Equals(GeneratedTerrainAssetResolutionFailure other) => other != null &&
            Code == other.Code && Subject == other.Subject && Reason == other.Reason;
        public override bool Equals(object obj) => Equals(obj as GeneratedTerrainAssetResolutionFailure);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableToken);
        public override string ToString() => StableToken;
    }

    public sealed class GeneratedTerrainAssetResolution
    {
        private readonly ReadOnlyCollection<GeneratedTerrainTileRegistryEntry> resolvedTiles;
        private readonly ReadOnlyCollection<GeneratedTerrainPrefabRegistryEntry> resolvedPrefabs;
        private readonly ReadOnlyCollection<GeneratedTerrainAssetResolutionFailure> failures;

        internal GeneratedTerrainAssetResolution(
            GeneratedTerrainAssetRegistrySnapshot registry,
            int requestedTileCodeCount,
            int requestedPrefabIdCount,
            IEnumerable<GeneratedTerrainTileRegistryEntry> sourceTiles,
            IEnumerable<GeneratedTerrainPrefabRegistryEntry> sourcePrefabs,
            IEnumerable<GeneratedTerrainAssetResolutionFailure> sourceFailures)
        {
            Registry = registry;
            RequestedTileCodeCount = requestedTileCodeCount;
            RequestedPrefabIdCount = requestedPrefabIdCount;
            failures = new ReadOnlyCollection<GeneratedTerrainAssetResolutionFailure>((sourceFailures ??
                Array.Empty<GeneratedTerrainAssetResolutionFailure>()).Distinct()
                .OrderBy(value => value).ToArray());
            resolvedTiles = new ReadOnlyCollection<GeneratedTerrainTileRegistryEntry>(failures.Count == 0
                ? (sourceTiles ?? Array.Empty<GeneratedTerrainTileRegistryEntry>())
                    .OrderBy(value => value).ToArray()
                : Array.Empty<GeneratedTerrainTileRegistryEntry>());
            resolvedPrefabs = new ReadOnlyCollection<GeneratedTerrainPrefabRegistryEntry>(failures.Count == 0
                ? (sourcePrefabs ?? Array.Empty<GeneratedTerrainPrefabRegistryEntry>())
                    .OrderBy(value => value).ToArray()
                : Array.Empty<GeneratedTerrainPrefabRegistryEntry>());
        }

        public GeneratedTerrainAssetRegistrySnapshot Registry { get; }
        public bool Success => Registry != null && failures.Count == 0;
        public int RequestedTileCodeCount { get; }
        public int RequestedPrefabIdCount { get; }
        public IReadOnlyList<GeneratedTerrainTileRegistryEntry> ResolvedTiles => resolvedTiles;
        public IReadOnlyList<GeneratedTerrainPrefabRegistryEntry> ResolvedPrefabs => resolvedPrefabs;
        public IReadOnlyList<GeneratedTerrainAssetResolutionFailure> Failures => failures;
        public int ResolvedTileCodeCount => resolvedTiles.Count;
        public int ResolvedPrefabIdCount => resolvedPrefabs.Count;
        public int MissingTileCodeCount => failures.Count(value =>
            value.Code == GeneratedTerrainAssetResolutionFailureCode.MissingTileCode);
        public int MissingPrefabIdCount => failures.Count(value =>
            value.Code == GeneratedTerrainAssetResolutionFailureCode.MissingPrefabId);
    }

    public static class GeneratedTerrainAssetResolver
    {
        public static GeneratedTerrainAssetResolution Resolve(
            GeneratedTerrainAssetRegistrySnapshot registry,
            IEnumerable<GeneratedTerrainTileCode> requiredTileCodes,
            IEnumerable<GeneratedTerrainPrefabId> requiredPrefabIds)
        {
            var failures = new List<GeneratedTerrainAssetResolutionFailure>();
            var rawTileCodes = (requiredTileCodes ?? Array.Empty<GeneratedTerrainTileCode>()).ToArray();
            var rawPrefabIds = (requiredPrefabIds ?? Array.Empty<GeneratedTerrainPrefabId>()).ToArray();
            var tileCodes = rawTileCodes.Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            var prefabIds = rawPrefabIds.Where(value => value != null).Distinct().OrderBy(value => value).ToArray();

            foreach (var code in rawTileCodes.Where(value => value == null || !value.IsValid))
                failures.Add(Failure(GeneratedTerrainAssetResolutionFailureCode.InvalidTileCode,
                    code == null ? "MISSING" : code.Value, "Tile codes must be non-empty control-free identifiers."));
            foreach (var id in rawPrefabIds.Where(value => value == null || !value.IsValid))
                failures.Add(Failure(GeneratedTerrainAssetResolutionFailureCode.InvalidPrefabId,
                    id == null ? "MISSING" : id.Value, "Prefab ids must be non-empty control-free identifiers."));

            if (registry == null)
                failures.Add(Failure(GeneratedTerrainAssetResolutionFailureCode.MissingRegistry,
                    "registry", "An immutable asset registry snapshot is required."));
            else
            {
                if (registry.NullTileEntryCount + registry.NullPrefabEntryCount +
                    registry.InvalidTileEntryCount + registry.InvalidPrefabEntryCount > 0)
                    failures.Add(Failure(GeneratedTerrainAssetResolutionFailureCode.InvalidRegistryEntry,
                        "registry", "Registry entries and asset keys must be valid identifiers."));
                if (registry.DuplicateTileCodeCount > 0)
                    failures.Add(Failure(GeneratedTerrainAssetResolutionFailureCode.DuplicateTileCode,
                        "tile_codes", "Tile codes must be unique."));
                if (registry.DuplicatePrefabIdCount > 0)
                    failures.Add(Failure(GeneratedTerrainAssetResolutionFailureCode.DuplicatePrefabId,
                        "prefab_ids", "Prefab ids must be unique."));
                if (!BakingCanonicalDigest.IsLowerHexSha256(registry.Digest) ||
                    !string.Equals(registry.Digest, GeneratedTerrainAssetRegistryDigest.Compute(registry),
                        StringComparison.Ordinal))
                    failures.Add(Failure(GeneratedTerrainAssetResolutionFailureCode.InvalidRegistryDigest,
                        "registry", "Registry digest is missing or stale."));
            }

            var resolvedTiles = new List<GeneratedTerrainTileRegistryEntry>();
            var resolvedPrefabs = new List<GeneratedTerrainPrefabRegistryEntry>();
            if (registry != null)
            {
                foreach (var code in tileCodes.Where(value => value.IsValid))
                {
                    var matches = registry.TileEntries.Where(value => value.Code != null &&
                        value.Code.Equals(code)).ToArray();
                    if (matches.Length == 0)
                        failures.Add(Failure(GeneratedTerrainAssetResolutionFailureCode.MissingTileCode,
                            code.Value, "The generated tile code is absent from the registry snapshot."));
                    else if (matches.Length == 1) resolvedTiles.Add(matches[0]);
                }
                foreach (var id in prefabIds.Where(value => value.IsValid))
                {
                    var matches = registry.PrefabEntries.Where(value => value.Id != null &&
                        value.Id.Equals(id)).ToArray();
                    if (matches.Length == 0)
                        failures.Add(Failure(GeneratedTerrainAssetResolutionFailureCode.MissingPrefabId,
                            id.Value, "The generated prefab id is absent from the registry snapshot."));
                    else if (matches.Length == 1) resolvedPrefabs.Add(matches[0]);
                }
            }

            return new GeneratedTerrainAssetResolution(registry, tileCodes.Length, prefabIds.Length,
                resolvedTiles, resolvedPrefabs, failures);
        }

        private static GeneratedTerrainAssetResolutionFailure Failure(
            GeneratedTerrainAssetResolutionFailureCode code, string subject, string reason) =>
            new GeneratedTerrainAssetResolutionFailure(code, subject, reason);
    }

    public static class GeneratedTerrainAssetRegistryDigest
    {
        public static string Compute(GeneratedTerrainAssetRegistrySnapshot registry)
        {
            if (registry == null) return string.Empty;
            var lines = new List<string>
            {
                "POLICY|" + GeneratedTerrainAssetRegistrySnapshot.PolicyVersion,
                "PUBLICATION|" + registry.PublicationLabel,
                "PRODUCTION_ASSET_APPROVAL|0",
                "NULLS|" + registry.NullTileEntryCount + "|" + registry.NullPrefabEntryCount,
            };
            lines.AddRange(registry.TileEntries.OrderBy(value => value).Select(value => value.StableToken));
            lines.AddRange(registry.PrefabEntries.OrderBy(value => value).Select(value => value.StableToken));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }
    }

    internal static class GeneratedTerrainAssetKey
    {
        public static bool IsValid(string value) => !string.IsNullOrWhiteSpace(value) &&
            value.All(character => !char.IsControl(character) && !char.IsWhiteSpace(character));
    }
}
