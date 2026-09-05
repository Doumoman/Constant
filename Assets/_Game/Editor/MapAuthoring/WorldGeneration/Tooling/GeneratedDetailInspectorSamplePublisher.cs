using System;
using System.Globalization;
using System.IO;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Tooling;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.WorldGeneration.Tooling
{
    public sealed class GeneratedDetailReadOnlySample
    {
        internal GeneratedDetailReadOnlySample(GeneratedDetailInspectionSnapshot snapshot,
            GeneratedPatternClusterSpecialSliceInspection details)
        {
            Snapshot = snapshot;
            Details = details;
        }

        public GeneratedDetailInspectionSnapshot Snapshot { get; }
        public GeneratedPatternClusterSpecialSliceInspection Details { get; }
    }

    public sealed class GeneratedDetailPublicationResult
    {
        internal GeneratedDetailPublicationResult(string outputDirectory,
            GeneratedDetailReadOnlySample sample, string manifestDigest,
            string map2004HandoffDigest)
        {
            OutputDirectory = outputDirectory;
            Sample = sample;
            ManifestDigest = manifestDigest;
            Map2004HandoffDigest = map2004HandoffDigest;
        }

        public string OutputDirectory { get; }
        public GeneratedDetailReadOnlySample Sample { get; }
        public string ManifestDigest { get; }
        public string Map2004HandoffDigest { get; }
    }

    /// <summary>Writes only MAP20_03 detail-inspection sample JSON.</summary>
    public static class GeneratedDetailInspectorSamplePublisher
    {
        public const string RelativeOutputRoot = "MapDesign/MCP/GENERATED/MAP20_03";
        public const string DetailSnapshotFileName = "detail_inspection_snapshot.json";
        public const string CombinedDetailFileName = "pattern_cluster_special_slice_sample.json";
        public const string DigestManifestFileName = "detail_digest_manifest.json";

        [MenuItem("Tools/MapDesign/Publish MAP20_03 Detail Inspector Samples")]
        public static void PublishSamplesFromMenu()
        {
            var result = PublishSamples(null);
            Debug.Log("MAP20_03 read-only detail samples published: " + result.OutputDirectory);
        }

        public static GeneratedDetailPublicationResult PublishSamples(string projectRoot)
        {
            var root = ResolveProjectRoot(projectRoot);
            var createdUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            var sample = LoadReadOnlySample(root, createdUtc);
            var map2004Handoff = ComputeMap2004HandoffDigest(
                sample.Snapshot.CanonicalDigest, sample.Details.CanonicalDigest,
                sample.Snapshot.SourceWorldOverlayDigest,
                sample.Snapshot.SourceSectorCanvasDigest);
            var manifest = new GeneratedDetailDigestManifest
            {
                schema_version = "map20_03.detail_digest_manifest.v1",
                task_id = GeneratedDetailInspectionSnapshot.TaskId,
                detail_snapshot_digest = sample.Snapshot.CanonicalDigest,
                combined_detail_sample_digest = sample.Details.CanonicalDigest,
                MAP20_02_handoff_digest =
                    GeneratedDetailInspectionPreconditions.Map2003HandoffDigest,
                MAP20_04_handoff_digest = map2004Handoff,
                created_utc = createdUtc,
                created_utc_excluded_from_canonical_digest = true,
            };
            var output = OutputDirectory(root);
            Directory.CreateDirectory(output);
            WriteExact(Path.Combine(output, DetailSnapshotFileName), sample.Snapshot.Serialize());
            WriteExact(Path.Combine(output, CombinedDetailFileName), sample.Details.Serialize());
            WriteExact(Path.Combine(output, DigestManifestFileName), manifest.SealAndSerialize());
            AssetDatabase.Refresh();
            return new GeneratedDetailPublicationResult(output, sample,
                manifest.canonical_digest, map2004Handoff);
        }

        public static GeneratedDetailReadOnlySample LoadReadOnlySample(string projectRoot,
            string createdUtc)
        {
            var root = ResolveProjectRoot(projectRoot);
            var source = ReadMap2002Context(root);
            var details = GeneratedPatternClusterSpecialSliceInspection.CreateMissingDataSample(
                source.sectorX, source.sectorY, source.cellX, source.cellY, createdUtc);
            var snapshot = GeneratedDetailInspectionSnapshot.Create(
                source.worldDigest, source.sectorDigest, source.sectorX, source.sectorY,
                source.cellX, source.cellY, details, createdUtc);
            return new GeneratedDetailReadOnlySample(snapshot, details);
        }

        public static string OutputDirectory(string projectRoot) => Path.GetFullPath(Path.Combine(
            ResolveProjectRoot(projectRoot),
            RelativeOutputRoot.Replace('/', Path.DirectorySeparatorChar)));

        public static string ComputeMap2004HandoffDigest(string detailSnapshotDigest,
            string combinedDetailDigest, string sourceWorldDigest, string sourceSectorDigest) =>
            BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP20_04_HANDOFF_V1",
                detailSnapshotDigest ?? string.Empty,
                combinedDetailDigest ?? string.Empty,
                sourceWorldDigest ?? string.Empty,
                sourceSectorDigest ?? string.Empty,
                GeneratedDetailInspectionPreconditions.Map2003HandoffDigest,
            });

        private static Map2002SourceContext ReadMap2002Context(string projectRoot)
        {
            var worldPath = SourcePath(projectRoot,
                GeneratedWorldOverlaySamplePublisher.WorldSnapshotFileName);
            var sectorPath = SourcePath(projectRoot,
                GeneratedWorldOverlaySamplePublisher.SectorInspectionFileName);
            var manifestPath = SourcePath(projectRoot,
                GeneratedWorldOverlaySamplePublisher.DigestManifestFileName);
            if (!File.Exists(worldPath) || !File.Exists(sectorPath) || !File.Exists(manifestPath))
                return Map2002SourceContext.Missing();
            try
            {
                var world = JsonUtility.FromJson<Map2002WorldDocument>(File.ReadAllText(worldPath));
                var sector = JsonUtility.FromJson<Map2002SectorDocument>(File.ReadAllText(sectorPath));
                var manifest = JsonUtility.FromJson<Map2002ManifestDocument>(
                    File.ReadAllText(manifestPath));
                if (world == null || sector == null || manifest == null ||
                    sector.sector_coordinate == null || sector.selected_cell_record == null ||
                    sector.selected_cell_record.local_cell_coordinate == null ||
                    !BakingCanonicalDigest.IsLowerHexSha256(world.canonical_digest) ||
                    !BakingCanonicalDigest.IsLowerHexSha256(sector.canonical_digest) ||
                    !string.Equals(manifest.MAP20_03_handoff_digest,
                        GeneratedDetailInspectionPreconditions.Map2003HandoffDigest,
                        StringComparison.Ordinal))
                    return Map2002SourceContext.Missing();
                return new Map2002SourceContext
                {
                    worldDigest = world.canonical_digest,
                    sectorDigest = sector.canonical_digest,
                    sectorX = sector.sector_coordinate.x,
                    sectorY = sector.sector_coordinate.y,
                    cellX = sector.selected_cell_record.local_cell_coordinate.x,
                    cellY = sector.selected_cell_record.local_cell_coordinate.y,
                };
            }
            catch (ArgumentException)
            {
                return Map2002SourceContext.Missing();
            }
        }

        private static string SourcePath(string projectRoot, string fileName) =>
            Path.GetFullPath(Path.Combine(projectRoot,
                GeneratedWorldOverlaySamplePublisher.RelativeOutputRoot.Replace('/',
                    Path.DirectorySeparatorChar), fileName));

        private static string ResolveProjectRoot(string projectRoot) =>
            string.IsNullOrWhiteSpace(projectRoot)
                ? Path.GetFullPath(Path.Combine(Application.dataPath, ".."))
                : Path.GetFullPath(projectRoot);

        private static void WriteExact(string path, string contents)
        {
            var parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
            File.WriteAllText(path, contents, BakingCanonicalDigest.Utf8NoBomEncoding);
        }
    }

    internal sealed class Map2002SourceContext
    {
        public string worldDigest;
        public string sectorDigest;
        public int sectorX;
        public int sectorY;
        public int cellX;
        public int cellY;

        public static Map2002SourceContext Missing() => new Map2002SourceContext
        {
            worldDigest = string.Empty,
            sectorDigest = string.Empty,
            sectorX = 0,
            sectorY = 0,
            cellX = 0,
            cellY = 0,
        };
    }

    [Serializable]
    internal sealed class Map2002CoordinateDocument
    {
        public int x;
        public int y;
    }

    [Serializable]
    internal sealed class Map2002WorldDocument
    {
        public string canonical_digest;
    }

    [Serializable]
    internal sealed class Map2002SelectedCellDocument
    {
        public Map2002CoordinateDocument local_cell_coordinate;
    }

    [Serializable]
    internal sealed class Map2002SectorDocument
    {
        public Map2002CoordinateDocument sector_coordinate;
        public Map2002SelectedCellDocument selected_cell_record;
        public string canonical_digest;
    }

    [Serializable]
    internal sealed class Map2002ManifestDocument
    {
        public string MAP20_03_handoff_digest;
    }

    [Serializable]
    internal sealed class GeneratedDetailDigestManifest
    {
        public string schema_version;
        public string task_id;
        public string detail_snapshot_digest;
        public string combined_detail_sample_digest;
        public string MAP20_02_handoff_digest;
        public string MAP20_04_handoff_digest;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
        public string canonical_digest;

        public string SealAndSerialize()
        {
            created_utc_excluded_from_canonical_digest = true;
            canonical_digest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                schema_version ?? string.Empty,
                task_id ?? string.Empty,
                detail_snapshot_digest ?? string.Empty,
                combined_detail_sample_digest ?? string.Empty,
                MAP20_02_handoff_digest ?? string.Empty,
                MAP20_04_handoff_digest ?? string.Empty,
                "created_utc_excluded=true",
            });
            return BakingCanonicalDigest.NormalizeLineEndingsToLf(JsonUtility.ToJson(this, true)) + "\n";
        }
    }
}
