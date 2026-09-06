using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Tooling;
using UnityEngine;

namespace StarNight.MapAuthoring.WorldGeneration.Tooling
{
    /// <summary>All fixed paths used solely to build the MAP20_04 read-only sample.</summary>
    public static class GeneratedCsvNavigationSamplePreconditions
    {
        public const string RelativeOutputRoot = "MapDesign/MCP/GENERATED/MAP20_04";
        public const string Map2003ManifestRelativePath =
            "MapDesign/MCP/GENERATED/MAP20_03/detail_digest_manifest.json";
        public const string Map2003SnapshotRelativePath =
            "MapDesign/MCP/GENERATED/MAP20_03/detail_inspection_snapshot.json";
        public const string Map2003DetailsRelativePath =
            "MapDesign/MCP/GENERATED/MAP20_03/pattern_cluster_special_slice_sample.json";
        public const string TileCsvRelativePath =
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroPattern/micro_pattern_cells_v2.csv";
        public const string PatternCsvRelativePath =
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroPattern/micro_pattern_catalog_v2.csv";
        public const string ClusterCsvRelativePath =
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/TerrainCluster/terrain_cluster_catalog_v2.csv";
        public const string SocketCsvRelativePath =
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroChunk/microchunk_sockets.csv";
        public const string SlotCsvRelativePath =
            "Assets/_Game/Map/Data/WorldGeneration/Authoring/Activity/activity_slots_v2.csv";
    }

    public sealed class GeneratedCsvNavigationReadOnlySample
    {
        internal GeneratedCsvNavigationReadOnlySample(GeneratedCsvNavigationIndex index,
            GeneratedValidationJumpSample jumpSample, string map2005HandoffDigest)
        {
            Index = index;
            JumpSample = jumpSample;
            Map2005HandoffDigest = map2005HandoffDigest;
        }

        public GeneratedCsvNavigationIndex Index { get; }
        public GeneratedValidationJumpSample JumpSample { get; }
        public string Map2005HandoffDigest { get; }
    }

    public sealed class GeneratedCsvNavigationPublicationResult
    {
        internal GeneratedCsvNavigationPublicationResult(string outputDirectory,
            GeneratedCsvNavigationReadOnlySample sample, string manifestDigest)
        {
            OutputDirectory = outputDirectory;
            Sample = sample;
            ManifestDigest = manifestDigest;
        }

        public string OutputDirectory { get; }
        public GeneratedCsvNavigationReadOnlySample Sample { get; }
        public string ManifestDigest { get; }
        public string Map2005HandoffDigest => Sample.Map2005HandoffDigest;
    }

    /// <summary>Reads source artifacts and writes only the three MAP20_04 sample JSON files.</summary>
    public static class GeneratedCsvNavigationSamplePublisher
    {
        public const string NavigationIndexFileName = "csv_navigation_index.json";
        public const string ValidationJumpFileName = "validation_jump_sample.json";
        public const string DigestManifestFileName = "source_navigation_digest_manifest.json";

        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private static int publicationInvocationCount;
        private static int artifactWriteCount;

        public static int PublicationInvocationCount => publicationInvocationCount;
        public static int ArtifactWriteCount => artifactWriteCount;
        public static int CsvAuthoringWriteCount => 0;

        public static void ResetDiagnostics()
        {
            publicationInvocationCount = 0;
            artifactWriteCount = 0;
        }

        public static GeneratedCsvNavigationReadOnlySample CreateReadOnlySample(string projectRoot,
            string createdUtc)
        {
            var root = ResolveProjectRoot(projectRoot);
            var map2003 = ReadMap2003Inputs(root);
            var sources = new List<GeneratedCsvSourceLocation>();
            var targets = new List<GeneratedValidationJumpTarget>();
            foreach (var spec in SampleSpecs())
            {
                var source = ReadSource(root, spec);
                sources.Add(source.Location);
                targets.Add(CreateTarget(spec, source, map2003));
            }

            var missing = CreateMap2003MissingTarget(map2003);
            sources.Add(missing.SourceLocation);
            targets.Add(missing);
            var index = new GeneratedCsvNavigationIndex(map2003.DetailSnapshotDigest,
                map2003.CombinedDetailDigest,
                GeneratedCsvNavigationPreconditions.Map2004HandoffDigest,
                sources, targets, createdUtc);
            var jumpSample = new GeneratedValidationJumpSample(targets, createdUtc);
            var map2005Handoff = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP20_05_HANDOFF_V1", index.CanonicalDigest, jumpSample.CanonicalDigest,
                GeneratedCsvNavigationPreconditions.Map2004HandoffDigest,
            });
            return new GeneratedCsvNavigationReadOnlySample(index, jumpSample, map2005Handoff);
        }

        public static GeneratedCsvNavigationPublicationResult PublishSamples(string projectRoot,
            bool focusedMap2004Passed)
        {
            if (!focusedMap2004Passed)
                throw new InvalidOperationException(
                    "MAP20_05 handoff publication requires an explicit focused MAP20_04 pass.");

            var root = ResolveProjectRoot(projectRoot);
            var createdUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            var sample = CreateReadOnlySample(root, createdUtc);
            var manifest = new GeneratedSourceNavigationDigestManifest
            {
                schema_version = "map20_04.source_navigation_digest_manifest.v1",
                task_id = GeneratedCsvNavigationPreconditions.TaskId,
                csv_navigation_index_digest = sample.Index.CanonicalDigest,
                validation_jump_sample_digest = sample.JumpSample.CanonicalDigest,
                MAP20_03_handoff_digest =
                    GeneratedCsvNavigationPreconditions.Map2004HandoffDigest,
                MAP20_05_handoff_digest = sample.Map2005HandoffDigest,
                created_utc = createdUtc,
                created_utc_excluded_from_canonical_digest = true,
            };
            var manifestJson = manifest.SealAndSerialize();
            var output = OutputDirectory(root);
            Directory.CreateDirectory(output);
            WriteArtifact(Path.Combine(output, NavigationIndexFileName), sample.Index.Serialize());
            WriteArtifact(Path.Combine(output, ValidationJumpFileName),
                sample.JumpSample.Serialize());
            WriteArtifact(Path.Combine(output, DigestManifestFileName), manifestJson);
            publicationInvocationCount++;
            return new GeneratedCsvNavigationPublicationResult(output, sample,
                manifest.canonical_digest);
        }

        public static string OutputDirectory(string projectRoot) => Path.GetFullPath(Path.Combine(
            ResolveProjectRoot(projectRoot),
            GeneratedCsvNavigationSamplePreconditions.RelativeOutputRoot.Replace('/',
                Path.DirectorySeparatorChar)));

        private static Map2003Inputs ReadMap2003Inputs(string projectRoot)
        {
            var manifestPath = ProjectPath(projectRoot,
                GeneratedCsvNavigationSamplePreconditions.Map2003ManifestRelativePath);
            var snapshotPath = ProjectPath(projectRoot,
                GeneratedCsvNavigationSamplePreconditions.Map2003SnapshotRelativePath);
            var detailsPath = ProjectPath(projectRoot,
                GeneratedCsvNavigationSamplePreconditions.Map2003DetailsRelativePath);
            if (!File.Exists(manifestPath) || !File.Exists(snapshotPath) || !File.Exists(detailsPath))
                throw new InvalidOperationException("MAP20_03 read-only handoff artifacts are missing.");

            var manifest = JsonUtility.FromJson<Map2003ManifestDocument>(
                File.ReadAllText(manifestPath));
            var snapshot = JsonUtility.FromJson<Map2003SnapshotDocument>(
                File.ReadAllText(snapshotPath));
            var details = JsonUtility.FromJson<Map2003CanonicalDocument>(
                File.ReadAllText(detailsPath));
            if (manifest == null || snapshot == null || details == null ||
                snapshot.selected_sector_coordinate == null ||
                snapshot.selected_cell_coordinate == null ||
                !BakingCanonicalDigest.IsLowerHexSha256(manifest.detail_snapshot_digest) ||
                !BakingCanonicalDigest.IsLowerHexSha256(manifest.combined_detail_sample_digest) ||
                !string.Equals(manifest.detail_snapshot_digest, snapshot.canonical_digest,
                    StringComparison.Ordinal) ||
                !string.Equals(manifest.combined_detail_sample_digest, details.canonical_digest,
                    StringComparison.Ordinal) ||
                !string.Equals(manifest.MAP20_04_handoff_digest,
                    GeneratedCsvNavigationPreconditions.Map2004HandoffDigest,
                    StringComparison.Ordinal))
                throw new InvalidOperationException("MAP20_03 read-only handoff digest mismatch.");

            var missing = (snapshot.missing_data_records ?? Array.Empty<Map2003MissingDocument>())
                .FirstOrDefault(value => value != null &&
                    string.Equals(value.tab_token, "Pattern", StringComparison.Ordinal)) ??
                (snapshot.missing_data_records ?? Array.Empty<Map2003MissingDocument>())
                .FirstOrDefault(value => value != null);
            return new Map2003Inputs
            {
                DetailSnapshotDigest = manifest.detail_snapshot_digest,
                CombinedDetailDigest = manifest.combined_detail_sample_digest,
                SectorX = snapshot.selected_sector_coordinate.x,
                SectorY = snapshot.selected_sector_coordinate.y,
                CellX = snapshot.selected_cell_coordinate.x,
                CellY = snapshot.selected_cell_coordinate.y,
                MissingTab = missing?.tab_token ?? "Pattern",
                MissingRecordId = missing?.record_id ?? "pattern:selected",
                MissingReason = missing?.reason ?? "MAP20_03 source detail is unavailable.",
            };
        }

        private static SourceReadResult ReadSource(string projectRoot, CsvSampleSpec spec)
        {
            var path = ProjectPath(projectRoot, spec.RelativePath);
            if (!File.Exists(path)) return SourceReadResult.Missing(spec,
                "CSV source file is unavailable.");
            try
            {
                var bytes = File.ReadAllBytes(path);
                var read = new Rfc4180CsvReader().Read(bytes, spec.RelativePath);
                if (!read.Success || read.Records.Count < 2)
                    return SourceReadResult.Missing(spec,
                        "CSV source cannot provide a header and data record.");
                var header = read.Records[0];
                var record = read.Records[1];
                var targetColumn = FindColumn(header, spec.TargetColumn);
                var idColumns = spec.IdColumns.Select(value => FindColumn(header, value)).ToArray();
                if (targetColumn < 0 || idColumns.Any(value => value < 0) ||
                    record.Fields.Count != header.Fields.Count)
                    return SourceReadResult.Missing(spec,
                        "CSV source does not match the expected read-only sample columns.");

                var identities = idColumns.Select(index => record.Fields[index].Value).ToArray();
                if (identities.Any(string.IsNullOrWhiteSpace))
                    return SourceReadResult.Missing(spec,
                        "CSV source record identity is empty.");
                var offset = read.HadUtf8Bom ? 3 : 0;
                var text = StrictUtf8.GetString(bytes, offset, bytes.Length - offset);
                var start = record.StartLocation.CharOffset;
                var length = record.EndLocationExclusive.CharOffset - start;
                var exactRecord = text.Substring(start, length);
                var location = new GeneratedCsvSourceLocation(
                    GeneratedCsvSourceKind.AuthoringCsv, spec.RelativePath, HashBytes(bytes),
                    record.Fields[targetColumn].StartLocation.PhysicalLine, targetColumn + 1,
                    header.Fields[targetColumn].Value, spec.TargetColumn,
                    string.Join("/", identities), spec.TargetKind.ToString(),
                    BakingCanonicalDigest.HashCanonicalText(exactRecord), true, string.Empty);
                return new SourceReadResult(location, identities);
            }
            catch (Exception exception) when (exception is IOException ||
                                               exception is DecoderFallbackException ||
                                               exception is ArgumentException)
            {
                return SourceReadResult.Missing(spec,
                    "CSV source is unavailable: " + exception.GetType().Name + ".");
            }
        }

        private static GeneratedValidationJumpTarget CreateTarget(CsvSampleSpec spec,
            SourceReadResult source, Map2003Inputs map2003)
        {
            var available = source.Location.SourceAvailable;
            var targetId = available && source.Identities.Length > spec.TargetIdentityOrdinal
                ? source.Identities[spec.TargetIdentityOrdinal]
                : string.Empty;
            var sector = new GeneratedNavigationCoordinate(map2003.SectorX, map2003.SectorY);
            var local = spec.TargetKind == GeneratedValidationJumpTargetKind.Tile
                ? new GeneratedNavigationCoordinate(map2003.CellX, map2003.CellY)
                : null;
            var world = spec.TargetKind == GeneratedValidationJumpTargetKind.Tile
                ? new GeneratedNavigationCoordinate(map2003.SectorX * 48 + map2003.CellX,
                    map2003.SectorY * 32 + map2003.CellY)
                : null;
            var inspectorRecord = available ? (spec.TargetKind ==
                GeneratedValidationJumpTargetKind.Tile
                    ? "cell:" + map2003.CellX.ToString(CultureInfo.InvariantCulture) + "," +
                      map2003.CellY.ToString(CultureInfo.InvariantCulture)
                    : targetId) : "MissingData";
            var selectionPath = spec.TargetKind == GeneratedValidationJumpTargetKind.Tile
                ? "GeneratedWorldOverlayInspector/Sector(" + map2003.SectorX + "," +
                  map2003.SectorY + ")/Cell(" + map2003.CellX + "," + map2003.CellY + ")"
                : "GeneratedDetailInspector/" + spec.InspectorTabToken + "/" + inspectorRecord;
            return new GeneratedValidationJumpTarget(spec.ValidationErrorId, "Error",
                spec.Owner, "Navigate to the exact read-only CSV source and inspector selection.",
                source.Location, spec.TargetKind, sector, local, world,
                spec.TargetKind == GeneratedValidationJumpTargetKind.Pattern ? targetId : string.Empty,
                spec.TargetKind == GeneratedValidationJumpTargetKind.Cluster ? targetId : string.Empty,
                spec.TargetKind == GeneratedValidationJumpTargetKind.Socket ? targetId : string.Empty,
                spec.TargetKind == GeneratedValidationJumpTargetKind.Slot ? targetId : string.Empty,
                spec.InspectorTabToken, inspectorRecord, selectionPath,
                "MAP20_03/" + map2003.DetailSnapshotDigest, available,
                available ? string.Empty : source.Location.MissingReason);
        }

        private static GeneratedValidationJumpTarget CreateMap2003MissingTarget(Map2003Inputs input)
        {
            var source = GeneratedCsvSourceLocation.Missing(input.MissingRecordId, "Pattern",
                input.MissingReason);
            return new GeneratedValidationJumpTarget("MAP20_04_PATTERN_MISSING_DATA", "Info",
                "MAP20_03", "The intended Pattern source remains explicit MissingData.", source,
                GeneratedValidationJumpTargetKind.Pattern,
                new GeneratedNavigationCoordinate(input.SectorX, input.SectorY), null, null,
                string.Empty, string.Empty, string.Empty, string.Empty,
                input.MissingTab, input.MissingRecordId,
                "GeneratedDetailInspector/" + input.MissingTab + "/" + input.MissingRecordId,
                "MAP20_03/" + input.DetailSnapshotDigest, false, input.MissingReason);
        }

        private static IEnumerable<CsvSampleSpec> SampleSpecs()
        {
            yield return new CsvSampleSpec("MAP20_04_TILE_SOURCE", "MicroPattern",
                GeneratedCsvNavigationSamplePreconditions.TileCsvRelativePath,
                GeneratedValidationJumpTargetKind.Tile,
                new[] { "pattern_id", "local_x", "local_y" }, "operation", 0, "WorldOverlay");
            yield return new CsvSampleSpec("MAP20_04_PATTERN_SOURCE", "MicroPattern",
                GeneratedCsvNavigationSamplePreconditions.PatternCsvRelativePath,
                GeneratedValidationJumpTargetKind.Pattern,
                new[] { "pattern_id" }, "selection_weight", 0, "Pattern");
            yield return new CsvSampleSpec("MAP20_04_CLUSTER_SOURCE", "TerrainCluster",
                GeneratedCsvNavigationSamplePreconditions.ClusterCsvRelativePath,
                GeneratedValidationJumpTargetKind.Cluster,
                new[] { "cluster_id" }, "pacing_role", 0, "Cluster");
            yield return new CsvSampleSpec("MAP20_04_SOCKET_SOURCE", "MicroChunk",
                GeneratedCsvNavigationSamplePreconditions.SocketCsvRelativePath,
                GeneratedValidationJumpTargetKind.Socket,
                new[] { "microchunk_id", "socket_id" }, "edge_signature_id", 1, "Slice");
            yield return new CsvSampleSpec("MAP20_04_SLOT_SOURCE", "ActivityStructure",
                GeneratedCsvNavigationSamplePreconditions.SlotCsvRelativePath,
                GeneratedValidationJumpTargetKind.Slot,
                new[] { "activity_id", "slot_id" }, "slot_kind", 1, "Cluster");
        }

        private static int FindColumn(CsvRecord header, string name)
        {
            for (var index = 0; index < header.Fields.Count; index++)
                if (string.Equals(header.Fields[index].Value, name, StringComparison.Ordinal))
                    return index;
            return -1;
        }

        private static string HashBytes(byte[] bytes)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(bytes).Select(value =>
                    value.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private static string ResolveProjectRoot(string projectRoot) =>
            string.IsNullOrWhiteSpace(projectRoot)
                ? Path.GetFullPath(Path.Combine(Application.dataPath, ".."))
                : Path.GetFullPath(projectRoot);

        private static string ProjectPath(string projectRoot, string relativePath)
        {
            var root = Path.GetFullPath(projectRoot);
            var path = Path.GetFullPath(Path.Combine(root,
                relativePath.Replace('/', Path.DirectorySeparatorChar)));
            var prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                         Path.DirectorySeparatorChar;
            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("MAP20_04 source path escaped the project root.");
            return path;
        }

        private static void WriteArtifact(string path, string contents)
        {
            File.WriteAllText(path, contents, BakingCanonicalDigest.Utf8NoBomEncoding);
            artifactWriteCount++;
        }
    }

    internal sealed class CsvSampleSpec
    {
        public CsvSampleSpec(string validationErrorId, string owner, string relativePath,
            GeneratedValidationJumpTargetKind targetKind, string[] idColumns,
            string targetColumn, int targetIdentityOrdinal, string inspectorTabToken)
        {
            ValidationErrorId = validationErrorId;
            Owner = owner;
            RelativePath = relativePath;
            TargetKind = targetKind;
            IdColumns = idColumns;
            TargetColumn = targetColumn;
            TargetIdentityOrdinal = targetIdentityOrdinal;
            InspectorTabToken = inspectorTabToken;
        }

        public string ValidationErrorId { get; }
        public string Owner { get; }
        public string RelativePath { get; }
        public GeneratedValidationJumpTargetKind TargetKind { get; }
        public string[] IdColumns { get; }
        public string TargetColumn { get; }
        public int TargetIdentityOrdinal { get; }
        public string InspectorTabToken { get; }
    }

    internal sealed class SourceReadResult
    {
        public SourceReadResult(GeneratedCsvSourceLocation location, string[] identities)
        {
            Location = location;
            Identities = identities ?? Array.Empty<string>();
        }

        public GeneratedCsvSourceLocation Location { get; }
        public string[] Identities { get; }

        public static SourceReadResult Missing(CsvSampleSpec spec, string reason) =>
            new SourceReadResult(GeneratedCsvSourceLocation.Missing(
                "source:" + spec.TargetKind, spec.TargetKind.ToString(), reason),
                Array.Empty<string>());
    }

    internal sealed class Map2003Inputs
    {
        public string DetailSnapshotDigest;
        public string CombinedDetailDigest;
        public int SectorX;
        public int SectorY;
        public int CellX;
        public int CellY;
        public string MissingTab;
        public string MissingRecordId;
        public string MissingReason;
    }

    [Serializable]
    internal sealed class Map2003CoordinateDocument
    {
        public int x;
        public int y;
    }

    [Serializable]
    internal sealed class Map2003MissingDocument
    {
        public string tab_token;
        public string record_id;
        public string reason;
    }

    [Serializable]
    internal sealed class Map2003SnapshotDocument
    {
        public Map2003CoordinateDocument selected_sector_coordinate;
        public Map2003CoordinateDocument selected_cell_coordinate;
        public Map2003MissingDocument[] missing_data_records;
        public string canonical_digest;
    }

    [Serializable]
    internal sealed class Map2003CanonicalDocument
    {
        public string canonical_digest;
    }

    [Serializable]
    internal sealed class Map2003ManifestDocument
    {
        public string detail_snapshot_digest;
        public string combined_detail_sample_digest;
        public string MAP20_04_handoff_digest;
    }

    [Serializable]
    internal sealed class GeneratedSourceNavigationDigestManifest
    {
        public string schema_version;
        public string task_id;
        public string csv_navigation_index_digest;
        public string validation_jump_sample_digest;
        public string MAP20_03_handoff_digest;
        public string MAP20_05_handoff_digest;
        public string canonical_digest;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;

        public string SealAndSerialize()
        {
            canonical_digest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                schema_version ?? string.Empty, task_id ?? string.Empty,
                csv_navigation_index_digest ?? string.Empty,
                validation_jump_sample_digest ?? string.Empty,
                MAP20_03_handoff_digest ?? string.Empty,
                MAP20_05_handoff_digest ?? string.Empty, "created_utc_excluded=true",
            });
            return BakingCanonicalDigest.NormalizeLineEndingsToLf(
                JsonUtility.ToJson(this, true)) + "\n";
        }
    }
}
