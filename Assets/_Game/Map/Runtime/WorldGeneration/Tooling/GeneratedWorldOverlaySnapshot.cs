using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Domain;
using UnityEngine;

namespace StarNight.Map.WorldGeneration.Tooling
{
    public static class GeneratedWorldOverlayPreconditions
    {
        public const string Map2001HandoffDigest =
            "ce4c0cb859a0b8cbd8b4179b5b2e346fa474b95ae2abbcd0142f34d396c46003";
    }

    public sealed class GeneratedWorldOverlayLayerDefinition
    {
        internal GeneratedWorldOverlayLayerDefinition(string token, string shortLabel,
            string ownerCategory, bool enabledByDefault)
        {
            Token = token;
            ShortLabel = shortLabel;
            OwnerCategory = ownerCategory;
            EnabledByDefault = enabledByDefault;
        }

        public string Token { get; }
        public string ShortLabel { get; }
        public string OwnerCategory { get; }
        public bool EnabledByDefault { get; }
        public bool IsReadOnly => true;
    }

    /// <summary>Single production catalog for the read-only world overlay.</summary>
    public static class GeneratedWorldOverlayLayerCatalog
    {
        private static readonly ReadOnlyCollection<GeneratedWorldOverlayLayerDefinition> layers =
            new ReadOnlyCollection<GeneratedWorldOverlayLayerDefinition>(new[]
            {
                Layer("Site", "SITE", "SiteReservation"),
                Layer("Biome", "BIO", "BiomePlanning"),
                Layer("Route", "RTE", "Traversal"),
                Layer("Boundary", "BND", "BoundarySockets"),
                Layer("Pacing", "PACE", "Pacing"),
                Layer("Cluster", "CLU", "TerrainCluster"),
                Layer("Activity", "ACT", "Activity"),
                Layer("Special", "SPC", "SpecialRegion"),
                Layer("Population", "POP", "Population"),
                Layer("Validation", "VAL", "MAP19Validation"),
            });

        public static IReadOnlyList<GeneratedWorldOverlayLayerDefinition> Layers => layers;
        public static IReadOnlyList<string> Tokens => new ReadOnlyCollection<string>(
            layers.Select(value => value.Token).ToArray());
        public static int WorldWidthSectors => WorldGenConstants.SectorColumns;
        public static int WorldHeightSectors => WorldGenConstants.SectorRows;
        public static int WorldSectorCount => WorldGenConstants.SectorCount;

        public static GeneratedWorldOverlayLayerDefinition Find(string token)
        {
            var layer = layers.FirstOrDefault(value => string.Equals(
                value.Token, token, StringComparison.Ordinal));
            if (layer == null) throw new ArgumentException("Unknown world overlay layer token.", nameof(token));
            return layer;
        }

        public static int IndexOf(string token)
        {
            for (var index = 0; index < layers.Count; index++)
                if (string.Equals(layers[index].Token, token, StringComparison.Ordinal)) return index;
            return -1;
        }

        internal static string[] NormalizeTokens(IEnumerable<string> tokens)
        {
            var requested = new HashSet<string>(tokens ?? layers.Select(value => value.Token),
                StringComparer.Ordinal);
            if (requested.Any(value => IndexOf(value) < 0))
                throw new ArgumentException("Enabled world overlay tokens must come from the catalog.",
                    nameof(tokens));
            return layers.Where(value => requested.Contains(value.Token))
                .Select(value => value.Token).ToArray();
        }

        private static GeneratedWorldOverlayLayerDefinition Layer(string token, string shortLabel,
            string ownerCategory) => new GeneratedWorldOverlayLayerDefinition(
                token, shortLabel, ownerCategory, true);
    }

    public sealed class GeneratedWorldOverlayLayerFact
    {
        public GeneratedWorldOverlayLayerFact(string layerToken, string summary, string marker,
            bool isMissingData)
        {
            var definition = GeneratedWorldOverlayLayerCatalog.Find(layerToken);
            LayerToken = definition.Token;
            ShortLabel = definition.ShortLabel;
            OwnerCategory = definition.OwnerCategory;
            Summary = summary ?? string.Empty;
            Marker = marker ?? string.Empty;
            IsMissingData = isMissingData;
        }

        public string LayerToken { get; }
        public string ShortLabel { get; }
        public string OwnerCategory { get; }
        public string Summary { get; }
        public string Marker { get; }
        public bool IsMissingData { get; }

        public static GeneratedWorldOverlayLayerFact Missing(string token, string reason) =>
            new GeneratedWorldOverlayLayerFact(token, reason, "MissingData", true);

        internal string CanonicalLine => GeneratedInspectionCanonical.Join(
            LayerToken, ShortLabel, OwnerCategory, Summary, Marker,
            IsMissingData ? "true" : "false");
    }

    public sealed class GeneratedWorldOverlaySectorRecord
    {
        private readonly ReadOnlyCollection<GeneratedWorldOverlayLayerFact> layerFacts;

        public GeneratedWorldOverlaySectorRecord(int sectorX, int sectorY,
            IEnumerable<GeneratedWorldOverlayLayerFact> facts)
        {
            if (sectorX < 0 || sectorX >= WorldGenConstants.SectorColumns)
                throw new ArgumentOutOfRangeException(nameof(sectorX));
            if (sectorY < 0 || sectorY >= WorldGenConstants.SectorRows)
                throw new ArgumentOutOfRangeException(nameof(sectorY));

            SectorX = sectorX;
            SectorY = sectorY;
            var byToken = (facts ?? Array.Empty<GeneratedWorldOverlayLayerFact>())
                .Where(value => value != null).ToArray();
            if (byToken.GroupBy(value => value.LayerToken, StringComparer.Ordinal)
                .Any(group => group.Count() != 1))
                throw new ArgumentException("A sector cannot contain duplicate overlay layer facts.",
                    nameof(facts));
            var dictionary = byToken.ToDictionary(value => value.LayerToken, StringComparer.Ordinal);
            var normalized = GeneratedWorldOverlayLayerCatalog.Layers.Select(definition =>
                dictionary.TryGetValue(definition.Token, out var fact)
                    ? fact
                    : GeneratedWorldOverlayLayerFact.Missing(definition.Token,
                        "No upstream fact was published for this sector layer."))
                .ToArray();
            layerFacts = new ReadOnlyCollection<GeneratedWorldOverlayLayerFact>(normalized);
        }

        public int SectorX { get; }
        public int SectorY { get; }
        public int RowMajorIndex => SectorY * WorldGenConstants.SectorColumns + SectorX;
        public IReadOnlyList<GeneratedWorldOverlayLayerFact> LayerFacts => layerFacts;
        public bool HasMissingData => layerFacts.Any(value => value.IsMissingData);

        public GeneratedWorldOverlayLayerFact Fact(string layerToken)
        {
            var index = GeneratedWorldOverlayLayerCatalog.IndexOf(layerToken);
            if (index < 0) throw new ArgumentException("Unknown world overlay layer token.",
                nameof(layerToken));
            return layerFacts[index];
        }

        public static GeneratedWorldOverlaySectorRecord Missing(int sectorX, int sectorY,
            string reason) => new GeneratedWorldOverlaySectorRecord(sectorX, sectorY,
                GeneratedWorldOverlayLayerCatalog.Tokens.Select(token =>
                    GeneratedWorldOverlayLayerFact.Missing(token, reason)));

        internal string CanonicalLine => GeneratedInspectionCanonical.Join(
            SectorX.ToString(CultureInfo.InvariantCulture),
            SectorY.ToString(CultureInfo.InvariantCulture),
            string.Join("|", layerFacts.Select(value => value.CanonicalLine)));
    }

    public sealed class GeneratedWorldOverlayMissingDataRecord
    {
        public GeneratedWorldOverlayMissingDataRecord(string scope, int sectorX, int sectorY,
            string layerToken, string reason)
        {
            Scope = scope ?? string.Empty;
            SectorX = sectorX;
            SectorY = sectorY;
            LayerToken = layerToken ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public string Scope { get; }
        public int SectorX { get; }
        public int SectorY { get; }
        public string LayerToken { get; }
        public string Reason { get; }
        internal string CanonicalLine => GeneratedInspectionCanonical.Join(Scope,
            SectorX.ToString(CultureInfo.InvariantCulture),
            SectorY.ToString(CultureInfo.InvariantCulture), LayerToken, Reason);
    }

    public sealed class GeneratedWorldOverlayValidationRecord
    {
        public GeneratedWorldOverlayValidationRecord(int sectorX, int sectorY, string owner,
            string state, string marker, string failureBundleReference)
        {
            SectorX = sectorX;
            SectorY = sectorY;
            Owner = owner ?? string.Empty;
            State = state ?? string.Empty;
            Marker = marker ?? string.Empty;
            FailureBundleReference = failureBundleReference ?? string.Empty;
        }

        public int SectorX { get; }
        public int SectorY { get; }
        public string Owner { get; }
        public string State { get; }
        public string Marker { get; }
        public string FailureBundleReference { get; }
        internal string CanonicalLine => GeneratedInspectionCanonical.Join(
            SectorX.ToString(CultureInfo.InvariantCulture),
            SectorY.ToString(CultureInfo.InvariantCulture), Owner, State, Marker,
            FailureBundleReference);
    }

    /// <summary>An immutable, deterministically serialized world overlay publication.</summary>
    public sealed class GeneratedWorldOverlaySnapshot
    {
        public const string SchemaVersion = "map20_02.world_overlay_snapshot.v1";
        public const string TaskId = "MAP20_02_CREATE_WORLD_OVERLAYS_AND_SECTOR_CANVAS_INSPECTOR";

        private readonly ReadOnlyCollection<string> enabledLayerTokens;
        private readonly ReadOnlyCollection<GeneratedWorldOverlaySectorRecord> sectorRecords;
        private readonly ReadOnlyCollection<GeneratedWorldOverlayMissingDataRecord> missingDataRecords;
        private readonly ReadOnlyCollection<GeneratedWorldOverlayValidationRecord> validationRecords;

        public GeneratedWorldOverlaySnapshot(string sourceRunArtifactDigest,
            string map2001HandoffDigest, IEnumerable<string> enabledTokens,
            IEnumerable<GeneratedWorldOverlaySectorRecord> sectors,
            IEnumerable<GeneratedWorldOverlayMissingDataRecord> missingData,
            IEnumerable<GeneratedWorldOverlayValidationRecord> validation,
            string createdUtc)
        {
            SourceRunArtifactDigest = sourceRunArtifactDigest ?? string.Empty;
            Map2001HandoffDigest = map2001HandoffDigest ?? string.Empty;
            CreatedUtc = createdUtc ?? string.Empty;
            enabledLayerTokens = new ReadOnlyCollection<string>(
                GeneratedWorldOverlayLayerCatalog.NormalizeTokens(enabledTokens));

            var supplied = (sectors ?? Array.Empty<GeneratedWorldOverlaySectorRecord>())
                .Where(value => value != null).ToArray();
            if (supplied.GroupBy(value => value.RowMajorIndex).Any(group => group.Count() != 1))
                throw new ArgumentException("World overlay sectors must have unique coordinates.",
                    nameof(sectors));
            var byIndex = supplied.ToDictionary(value => value.RowMajorIndex);
            var complete = new List<GeneratedWorldOverlaySectorRecord>(WorldGenConstants.SectorCount);
            for (var y = 0; y < WorldGenConstants.SectorRows; y++)
            for (var x = 0; x < WorldGenConstants.SectorColumns; x++)
            {
                var index = y * WorldGenConstants.SectorColumns + x;
                complete.Add(byIndex.TryGetValue(index, out var record)
                    ? record
                    : GeneratedWorldOverlaySectorRecord.Missing(x, y,
                        "No upstream sector record was published."));
            }
            sectorRecords = new ReadOnlyCollection<GeneratedWorldOverlaySectorRecord>(complete);
            missingDataRecords = new ReadOnlyCollection<GeneratedWorldOverlayMissingDataRecord>(
                (missingData ?? Array.Empty<GeneratedWorldOverlayMissingDataRecord>())
                .Where(value => value != null).OrderBy(value => value.CanonicalLine,
                    StringComparer.Ordinal).ToArray());
            validationRecords = new ReadOnlyCollection<GeneratedWorldOverlayValidationRecord>(
                (validation ?? Array.Empty<GeneratedWorldOverlayValidationRecord>())
                .Where(value => value != null).OrderBy(value => value.CanonicalLine,
                    StringComparer.Ordinal).ToArray());
            CanonicalDigest = ComputeCanonicalDigest();
        }

        public int WorldWidthSectors => WorldGenConstants.SectorColumns;
        public int WorldHeightSectors => WorldGenConstants.SectorRows;
        public int SectorCount => WorldGenConstants.SectorCount;
        public string SourceRunArtifactDigest { get; }
        public string Map2001HandoffDigest { get; }
        public IReadOnlyList<string> EnabledLayerTokens => enabledLayerTokens;
        public IReadOnlyList<GeneratedWorldOverlaySectorRecord> SectorRecords => sectorRecords;
        public IReadOnlyList<GeneratedWorldOverlayMissingDataRecord> MissingDataRecords => missingDataRecords;
        public IReadOnlyList<GeneratedWorldOverlayValidationRecord> ValidationRecords => validationRecords;
        public string CreatedUtc { get; }
        public bool CreatedUtcExcludedFromCanonicalDigest => true;
        public string CanonicalDigest { get; }

        public GeneratedWorldOverlaySectorRecord Sector(int sectorX, int sectorY)
        {
            if (sectorX < 0 || sectorX >= WorldGenConstants.SectorColumns ||
                sectorY < 0 || sectorY >= WorldGenConstants.SectorRows)
                throw new ArgumentOutOfRangeException(nameof(sectorX));
            return sectorRecords[sectorY * WorldGenConstants.SectorColumns + sectorX];
        }

        public string Serialize() => GeneratedInspectionCanonical.ToJson(ToDocument());

        public static GeneratedWorldOverlaySnapshot CreateMissingDataSample(
            string sourceRunArtifactDigest, string map2001HandoffDigest, string createdUtc)
        {
            const string reason =
                "MAP20_01 run artifact has no published world-layer facts; inspection remains read-only.";
            var sectors = new List<GeneratedWorldOverlaySectorRecord>(WorldGenConstants.SectorCount);
            for (var y = 0; y < WorldGenConstants.SectorRows; y++)
            for (var x = 0; x < WorldGenConstants.SectorColumns; x++)
                sectors.Add(GeneratedWorldOverlaySectorRecord.Missing(x, y, reason));
            var missing = GeneratedWorldOverlayLayerCatalog.Tokens.Select(token =>
                new GeneratedWorldOverlayMissingDataRecord("World", -1, -1, token, reason));
            var validation = new[]
            {
                new GeneratedWorldOverlayValidationRecord(-1, -1, "MAP19",
                    "MissingData", "MAP19 sector markers were not embedded in the MAP20_01 artifact.",
                    string.Empty),
            };
            return new GeneratedWorldOverlaySnapshot(sourceRunArtifactDigest,
                map2001HandoffDigest, GeneratedWorldOverlayLayerCatalog.Tokens, sectors,
                missing, validation, createdUtc);
        }

        private string ComputeCanonicalDigest()
        {
            var lines = new List<string>
            {
                SchemaVersion,
                TaskId,
                WorldWidthSectors.ToString(CultureInfo.InvariantCulture),
                WorldHeightSectors.ToString(CultureInfo.InvariantCulture),
                SectorCount.ToString(CultureInfo.InvariantCulture),
                SourceRunArtifactDigest,
                Map2001HandoffDigest,
                string.Join("|", enabledLayerTokens),
                "created_utc_excluded=true",
            };
            lines.AddRange(sectorRecords.Select(value => "sector=" + value.CanonicalLine));
            lines.AddRange(missingDataRecords.Select(value => "missing=" + value.CanonicalLine));
            lines.AddRange(validationRecords.Select(value => "validation=" + value.CanonicalLine));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        private GeneratedWorldOverlaySnapshotDocument ToDocument() =>
            new GeneratedWorldOverlaySnapshotDocument
            {
                schema_version = SchemaVersion,
                task_id = TaskId,
                world_width_sectors = WorldWidthSectors,
                world_height_sectors = WorldHeightSectors,
                sector_count = SectorCount,
                source_run_artifact_digest = SourceRunArtifactDigest,
                MAP20_01_handoff_digest = Map2001HandoffDigest,
                enabled_layer_tokens = enabledLayerTokens.ToArray(),
                sector_records = sectorRecords.Select(GeneratedWorldOverlaySectorDocument.From).ToArray(),
                missing_data_records = missingDataRecords.Select(
                    GeneratedWorldOverlayMissingDataDocument.From).ToArray(),
                validation_records = validationRecords.Select(
                    GeneratedWorldOverlayValidationDocument.From).ToArray(),
                canonical_digest = CanonicalDigest,
                created_utc = CreatedUtc,
                created_utc_excluded_from_canonical_digest = true,
            };
    }

    internal static class GeneratedInspectionCanonical
    {
        public static string Join(params string[] values) => string.Join("/",
            (values ?? Array.Empty<string>()).Select(value =>
            {
                var text = value ?? string.Empty;
                return text.Length.ToString(CultureInfo.InvariantCulture) + ":" + text;
            }));

        public static string ToJson(object value) =>
            BakingCanonicalDigest.NormalizeLineEndingsToLf(JsonUtility.ToJson(value, true)) + "\n";
    }

    [Serializable]
    internal sealed class GeneratedWorldOverlaySnapshotDocument
    {
        public string schema_version;
        public string task_id;
        public int world_width_sectors;
        public int world_height_sectors;
        public int sector_count;
        public string source_run_artifact_digest;
        public string MAP20_01_handoff_digest;
        public string[] enabled_layer_tokens;
        public GeneratedWorldOverlaySectorDocument[] sector_records;
        public GeneratedWorldOverlayMissingDataDocument[] missing_data_records;
        public GeneratedWorldOverlayValidationDocument[] validation_records;
        public string canonical_digest;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
    }

    [Serializable]
    internal sealed class GeneratedWorldOverlaySectorDocument
    {
        public int sector_x;
        public int sector_y;
        public int row_major_index;
        public bool missing_data;
        public GeneratedWorldOverlayLayerFactDocument[] layer_records;

        public static GeneratedWorldOverlaySectorDocument From(GeneratedWorldOverlaySectorRecord value) =>
            new GeneratedWorldOverlaySectorDocument
            {
                sector_x = value.SectorX,
                sector_y = value.SectorY,
                row_major_index = value.RowMajorIndex,
                missing_data = value.HasMissingData,
                layer_records = value.LayerFacts.Select(GeneratedWorldOverlayLayerFactDocument.From).ToArray(),
            };
    }

    [Serializable]
    internal sealed class GeneratedWorldOverlayLayerFactDocument
    {
        public string layer_token;
        public string short_label;
        public string owner_category;
        public string summary;
        public string marker;
        public bool missing_data;

        public static GeneratedWorldOverlayLayerFactDocument From(GeneratedWorldOverlayLayerFact value) =>
            new GeneratedWorldOverlayLayerFactDocument
            {
                layer_token = value.LayerToken,
                short_label = value.ShortLabel,
                owner_category = value.OwnerCategory,
                summary = value.Summary,
                marker = value.Marker,
                missing_data = value.IsMissingData,
            };
    }

    [Serializable]
    internal sealed class GeneratedWorldOverlayMissingDataDocument
    {
        public string scope;
        public int sector_x;
        public int sector_y;
        public string layer_token;
        public string reason;

        public static GeneratedWorldOverlayMissingDataDocument From(
            GeneratedWorldOverlayMissingDataRecord value) =>
            new GeneratedWorldOverlayMissingDataDocument
            {
                scope = value.Scope,
                sector_x = value.SectorX,
                sector_y = value.SectorY,
                layer_token = value.LayerToken,
                reason = value.Reason,
            };
    }

    [Serializable]
    internal sealed class GeneratedWorldOverlayValidationDocument
    {
        public int sector_x;
        public int sector_y;
        public string owner;
        public string state;
        public string marker;
        public string failure_bundle_reference;

        public static GeneratedWorldOverlayValidationDocument From(
            GeneratedWorldOverlayValidationRecord value) =>
            new GeneratedWorldOverlayValidationDocument
            {
                sector_x = value.SectorX,
                sector_y = value.SectorY,
                owner = value.Owner,
                state = value.State,
                marker = value.Marker,
                failure_bundle_reference = value.FailureBundleReference,
            };
    }
}
