using System;
using System.Globalization;
using System.IO;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Tooling;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.WorldGeneration.Tooling
{
    public sealed class GeneratedWorldOverlayPublicationResult
    {
        internal GeneratedWorldOverlayPublicationResult(string outputDirectory,
            GeneratedWorldOverlaySnapshot worldSnapshot,
            GeneratedSectorCanvasInspection sectorInspection,
            string manifestDigest, string map2003HandoffDigest)
        {
            OutputDirectory = outputDirectory;
            WorldSnapshot = worldSnapshot;
            SectorInspection = sectorInspection;
            ManifestDigest = manifestDigest;
            Map2003HandoffDigest = map2003HandoffDigest;
        }

        public string OutputDirectory { get; }
        public GeneratedWorldOverlaySnapshot WorldSnapshot { get; }
        public GeneratedSectorCanvasInspection SectorInspection { get; }
        public string ManifestDigest { get; }
        public string Map2003HandoffDigest { get; }
    }

    /// <summary>Writes only deterministic MAP20_02 sample inspection artifacts.</summary>
    public static class GeneratedWorldOverlaySamplePublisher
    {
        public const string RelativeOutputRoot = "MapDesign/MCP/GENERATED/MAP20_02";
        public const string WorldSnapshotFileName = "world_overlay_snapshot.json";
        public const string SectorInspectionFileName = "sector_canvas_inspection_sample.json";
        public const string DigestManifestFileName = "overlay_digest_manifest.json";
        public const string SourceRunArtifactRelative =
            "MapDesign/MCP/GENERATED/MAP20_01/runs/sample-pattern-731/run_artifact.json";
        public const string Map2001HandoffDigest =
            GeneratedWorldOverlayPreconditions.Map2001HandoffDigest;

        [MenuItem("Tools/MapDesign/Publish MAP20_02 Overlay Samples")]
        public static void PublishSamplesFromMenu()
        {
            var result = PublishSamples(null);
            Debug.Log("MAP20_02 read-only overlay samples published: " + result.OutputDirectory);
        }

        public static GeneratedWorldOverlayPublicationResult PublishSamples(string projectRoot)
        {
            var root = ResolveProjectRoot(projectRoot);
            var createdUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            var sourceDigest = ResolveSourceRunArtifactDigest(root, SourceRunArtifactRelative);
            var world = GeneratedWorldOverlaySnapshot.CreateMissingDataSample(
                sourceDigest, Map2001HandoffDigest, createdUtc);
            var sector = GeneratedSectorCanvasInspection.CreateMissingDataSample(
                GeneratedWorldOverlayLayerCatalog.WorldWidthSectors / 2,
                GeneratedWorldOverlayLayerCatalog.WorldHeightSectors / 2, createdUtc);
            var map2003Handoff = ComputeMap2003HandoffDigest(
                world.CanonicalDigest, sector.CanonicalDigest);
            var manifest = new GeneratedWorldOverlayDigestManifest
            {
                schema_version = "map20_02.overlay_digest_manifest.v1",
                task_id = GeneratedWorldOverlaySnapshot.TaskId,
                world_overlay_snapshot_path = WorldSnapshotFileName,
                world_overlay_snapshot_digest = world.CanonicalDigest,
                sector_canvas_inspection_path = SectorInspectionFileName,
                sector_canvas_inspection_digest = sector.CanonicalDigest,
                source_run_artifact_digest = sourceDigest,
                MAP19_exit_digest = GeneratedTerrainRunPreconditions.Map19ExitDigest,
                MAP20_01_handoff_digest = Map2001HandoffDigest,
                MAP20_03_handoff_digest = map2003Handoff,
                created_utc = createdUtc,
                created_utc_excluded_from_canonical_digest = true,
            };
            var manifestJson = manifest.SealAndSerialize();
            var output = ProjectPath(root, RelativeOutputRoot);
            Directory.CreateDirectory(output);
            WriteExact(Path.Combine(output, WorldSnapshotFileName), world.Serialize());
            WriteExact(Path.Combine(output, SectorInspectionFileName), sector.Serialize());
            WriteExact(Path.Combine(output, DigestManifestFileName), manifestJson);
            AssetDatabase.Refresh();
            return new GeneratedWorldOverlayPublicationResult(output, world, sector,
                manifest.canonical_digest, map2003Handoff);
        }

        public static string ResolveSourceRunArtifactDigest(string projectRoot,
            string runArtifactRelativePath)
        {
            var root = ResolveProjectRoot(projectRoot);
            var path = ProjectPath(root, string.IsNullOrWhiteSpace(runArtifactRelativePath)
                ? SourceRunArtifactRelative
                : runArtifactRelativePath);
            if (!File.Exists(path)) return string.Empty;
            try
            {
                var artifact = JsonUtility.FromJson<GeneratedTerrainRunArtifact>(
                    File.ReadAllText(path));
                return artifact != null && BakingCanonicalDigest.IsLowerHexSha256(
                    artifact.canonical_digest)
                    ? artifact.canonical_digest
                    : string.Empty;
            }
            catch (ArgumentException)
            {
                return string.Empty;
            }
        }

        public static string OutputDirectory(string projectRoot) =>
            ProjectPath(ResolveProjectRoot(projectRoot), RelativeOutputRoot);

        public static string ComputeMap2003HandoffDigest(string worldDigest,
            string sectorDigest) => BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP20_03_HANDOFF_V1",
                worldDigest ?? string.Empty,
                sectorDigest ?? string.Empty,
                Map2001HandoffDigest,
            });

        private static string ResolveProjectRoot(string projectRoot) =>
            string.IsNullOrWhiteSpace(projectRoot)
                ? Path.GetFullPath(Path.Combine(Application.dataPath, ".."))
                : Path.GetFullPath(projectRoot);

        private static string ProjectPath(string projectRoot, string relativePath) =>
            Path.GetFullPath(Path.Combine(projectRoot,
                (relativePath ?? string.Empty).Replace('/', Path.DirectorySeparatorChar)));

        private static void WriteExact(string path, string contents)
        {
            var parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
            File.WriteAllText(path, contents, BakingCanonicalDigest.Utf8NoBomEncoding);
        }
    }

    [Serializable]
    internal sealed class GeneratedWorldOverlayDigestManifest
    {
        public string schema_version;
        public string task_id;
        public string world_overlay_snapshot_path;
        public string world_overlay_snapshot_digest;
        public string sector_canvas_inspection_path;
        public string sector_canvas_inspection_digest;
        public string source_run_artifact_digest;
        public string MAP19_exit_digest;
        public string MAP20_01_handoff_digest;
        public string MAP20_03_handoff_digest;
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
                world_overlay_snapshot_path ?? string.Empty,
                world_overlay_snapshot_digest ?? string.Empty,
                sector_canvas_inspection_path ?? string.Empty,
                sector_canvas_inspection_digest ?? string.Empty,
                source_run_artifact_digest ?? string.Empty,
                MAP19_exit_digest ?? string.Empty,
                MAP20_01_handoff_digest ?? string.Empty,
                MAP20_03_handoff_digest ?? string.Empty,
                "created_utc_excluded=true",
            });
            return BakingCanonicalDigest.NormalizeLineEndingsToLf(JsonUtility.ToJson(this, true)) + "\n";
        }
    }
}
