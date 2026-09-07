using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.MoonPalace.Visualization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace StarNight.Tests.EditMode.Map.WorldGeneration.MoonPalace.Visualization
{
    public sealed class MoonPalaceGrayboxExampleTests
    {
        private const string CategoryName = "VIS01";
        private const string BuilderTypeName =
            "StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.Visualization.MoonPalaceGrayboxExampleSceneBuilder";

        private object publication;
        private IReadOnlyList<string> sourcePaths;
        private Dictionary<string, string> sourceBefore;
        private Dictionary<string, string> sourceAfter;

        [OneTimeSetUp]
        public void PublishVis01Once()
        {
            sourcePaths = (IReadOnlyList<string>)InvokeBuilder("GetSourceReadRelativePaths");
            sourceBefore = sourcePaths.ToDictionary(path => path, FileSha, StringComparer.Ordinal);
            publication = InvokeBuilder("Publish", ProjectRoot);
            sourceAfter = sourcePaths.ToDictionary(path => path, FileSha, StringComparer.Ordinal);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceCatalogSnapshotLoadsActualMap21Sources()
        {
            var catalog = Generation.Catalog;
            Assert.That(sourcePaths.Count, Is.EqualTo(14));
            Assert.That(catalog.BiomeCount, Is.EqualTo(4));
            Assert.That(catalog.TileCodeCount, Is.EqualTo(10));
            Assert.That(catalog.ProductionMicroPatternCount, Is.EqualTo(24));
            Assert.That(catalog.ProductionPatternCellCount, Is.EqualTo(384));
            Assert.That(catalog.TerrainClusterCount, Is.EqualTo(48));
            Assert.That(catalog.ClusterSpineVariantCount, Is.EqualTo(96));
            Assert.That(catalog.ActivityProfileCount, Is.EqualTo(7));
            Assert.That(catalog.EventOverlayProfileCount, Is.EqualTo(5));
            Assert.That(catalog.BiomePairCount, Is.EqualTo(6));
            Assert.That(catalog.BoundaryCandidateCount, Is.EqualTo(48));
            Assert.That(catalog.CoreResourceRegionCount, Is.EqualTo(3));
            Assert.That(catalog.TuningDensityWindowCount, Is.EqualTo(4));
            Assert.That(catalog.QaSeedMpQa01, Is.EqualTo(1924737067));
            Assert.That(catalog.TypedLoadErrorCount + catalog.ForeignKeyErrorCount + catalog.DuplicateIdErrorCount, Is.Zero);
            Assert.That(catalog.Sources.All(source => source.IsValid && source.HeaderHash.Length == 64 && source.ContentSha256.Length == 64), Is.True);
            Assert.That(sourceAfter, Is.EqualTo(sourceBefore));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceMicroPatternCandidatesEnumerate65536AndSelect500Deterministically()
        {
            var repeat = MoonPalaceMicroPatternCandidateLibrary.Build();
            Assert.That(Generation.CandidateSet.RawMaskCount, Is.EqualTo(65536));
            Assert.That(Generation.CandidateSet.Candidates.Count, Is.EqualTo(500));
            Assert.That(Generation.CandidateSet.Candidates.Select(c => c.CandidateId).Distinct().Count(), Is.EqualTo(500));
            Assert.That(repeat.CanonicalDigest, Is.EqualTo(Generation.CandidateSet.CanonicalDigest));
            Assert.That(repeat.Candidates.Select(c => c.CandidateId),
                Is.EqualTo(Generation.CandidateSet.Candidates.Select(c => c.CandidateId)));
            Assert.That(Generation.CandidateSet.RejectionCounts["all_solid"], Is.EqualTo(1));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceMicroPatternCandidatesPreserveSocketDensitySilhouetteAndMirrorMetadata()
        {
            var candidates = Generation.CandidateSet.Candidates;
            Assert.That(candidates.Select(c => c.DensityBucket).Distinct().Count(), Is.GreaterThanOrEqualTo(4));
            Assert.That(candidates.Select(c => c.EdgeSocketBucket).Distinct().Count(), Is.GreaterThanOrEqualTo(4));
            Assert.That(candidates.Select(c => c.SilhouetteSignature).Distinct().Count(), Is.GreaterThan(100));
            Assert.That(candidates.Select(c => c.MirrorFamilySignature).Distinct().Count(), Is.GreaterThanOrEqualTo(250));
            Assert.That(candidates.Select(c => c.SupportAffordanceClass).Distinct().Count(), Is.GreaterThanOrEqualTo(2));
            Assert.That(candidates.Select(c => c.HazardDetailEligibility).Distinct().Count(), Is.EqualTo(2));
            Assert.That(candidates.All(c => c.MaskU16Hex.StartsWith("0x", StringComparison.Ordinal) &&
                                            !string.IsNullOrEmpty(c.SocketSignature) && !string.IsNullOrEmpty(c.RoleTags) &&
                                            c.Rank >= 1 && c.Rank <= 500), Is.True);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceReachableMicroChunkComposerBuildsSixPattern12x8Chunks()
        {
            Assert.That(Generation.MicroChunks.Count, Is.EqualTo(16));
            Assert.That(Generation.MicroChunks.All(chunk => chunk.PatternPlacements.Count == 6 && chunk.OpenCells.Count == 96), Is.True);
            Assert.That(Generation.MicroChunks.All(chunk => chunk.Validation.Reachable && chunk.EntrySockets.Count >= 1 &&
                                                            chunk.ExitSockets.Count >= 1 && chunk.ChunkDigest.Length == 64), Is.True);
            Assert.That(Generation.MicroChunks.SelectMany(chunk => chunk.PatternPlacements)
                .All(placement => placement.PatternX >= 0 && placement.PatternX < 3 && placement.PatternY >= 0 && placement.PatternY < 2), Is.True);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceReachableMicroChunkComposerRejectsUnreachableOrSilentCarvedChunks()
        {
            var closed = new bool[96];
            var before = (bool[])closed.Clone();
            var unreachable = MoonPalaceReachableMicroChunkComposer.Validate(closed,
                new[] { new MoonPalaceVisCellCoord(0, 3) }, new[] { new MoonPalaceVisCellCoord(11, 3) },
                new[] { new MoonPalaceVisCellCoord(0, 3) }, Array.Empty<MoonPalaceVisCellCoord>(),
                new[] { new MoonPalaceVisCellCoord(0, 3) }, 0, 0);
            Assert.That(unreachable.Reachable, Is.False);
            Assert.That(unreachable.FailureOwner, Is.EqualTo("REQUIRED_PATH_SOLID"));
            Assert.That(closed, Is.EqualTo(before), "Validation must not carve the provided cells.");

            var open = Enumerable.Repeat(true, 96).ToArray();
            var carvedClaim = MoonPalaceReachableMicroChunkComposer.Validate(open,
                new[] { new MoonPalaceVisCellCoord(0, 3) }, new[] { new MoonPalaceVisCellCoord(11, 3) },
                Array.Empty<MoonPalaceVisCellCoord>(), Array.Empty<MoonPalaceVisCellCoord>(),
                Array.Empty<MoonPalaceVisCellCoord>(), 1, 0);
            Assert.That(carvedClaim.Reachable, Is.False);
            Assert.That(carvedClaim.FailureOwner, Is.EqualTo("FALLBACK_CARVE_FORBIDDEN"));
            Assert.That(carvedClaim.FallbackCarveCount, Is.EqualTo(1));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceOneSectorGeneratorProduces1536UniqueCellsAnd96PatternPlacements()
        {
            Assert.That(Generation.Cells.Count, Is.EqualTo(1536));
            Assert.That(Generation.Cells.Select(cell => cell.X + ":" + cell.Y).Distinct().Count(), Is.EqualTo(1536));
            Assert.That(Generation.Cells.Min(cell => cell.X), Is.Zero);
            Assert.That(Generation.Cells.Max(cell => cell.X), Is.EqualTo(47));
            Assert.That(Generation.Cells.Min(cell => cell.Y), Is.Zero);
            Assert.That(Generation.Cells.Max(cell => cell.Y), Is.EqualTo(31));
            Assert.That(Generation.PatternPlacements.Count, Is.EqualTo(96));
            Assert.That(Generation.PatternPlacements.Select(p => p.PatternX + ":" + p.PatternY).Distinct().Count(), Is.EqualTo(96));
            Assert.That(Generation.PatternPlacements.All(p => p.Transform == "IDENTITY_NO_ROTATION"), Is.True);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceOneSectorGeneratorProduces16ReachableChunksAnd10752LayerRecords()
        {
            Assert.That(Generation.MicroChunks.Count, Is.EqualTo(16));
            Assert.That(Generation.MicroChunks.All(chunk => chunk.Validation.Reachable), Is.True);
            Assert.That(Generation.LogicalLayers.Count, Is.EqualTo(10752));
            Assert.That(Generation.LogicalLayers.GroupBy(layer => layer.LayerName).All(group => group.Count() == 1536), Is.True);
            Assert.That(Generation.Validation.RouteFailureCount, Is.Zero);
            Assert.That(Generation.Validation.RecoveryFailureCount, Is.Zero);
            Assert.That(Generation.Validation.SeamFailureCount, Is.Zero);
            Assert.That(Generation.Validation.FallbackCarveCount + Generation.Validation.SilentAutoRepairCount, Is.Zero);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceOneSectorGeneratorBindsSelectionsToCatalogAndCandidateIds()
        {
            var candidateIds = Generation.CandidateSet.Candidates.Select(c => c.CandidateId).ToHashSet(StringComparer.Ordinal);
            Assert.That(Generation.PatternPlacements.All(p => candidateIds.Contains(p.CandidateId)), Is.True);
            Assert.That(Generation.PatternPlacements.All(p => Generation.Catalog.ProductionPatternIds.Contains(p.ProductionPatternFamilyId)), Is.True);
            Assert.That(Generation.PatternPlacements.All(p => Generation.Catalog.BiomeIds.Contains(p.BiomeId)), Is.True);
            Assert.That(Generation.PatternPlacements.All(p => Generation.Catalog.TerrainClusterIds.Contains(p.TerrainClusterId)), Is.True);
            Assert.That(Generation.SelectionManifest.Any(record => record.Kind == "ACTIVITY"), Is.True);
            Assert.That(Generation.SelectionManifest.Any(record => record.Kind == "EVENT"), Is.True);
            Assert.That(Generation.SelectionManifest.Any(record => record.Kind == "BOUNDARY"), Is.True);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceGrayboxSceneBuilderCreatesIsolatedUnitySceneWithGridCameraLegend()
        {
            var scenePath = Constant("SceneRelativePath");
            Assert.That(File.Exists(Resolve(scenePath)), Is.True);
            var previous = SceneManager.GetActiveScene();
            var replaceUntitled = previous.IsValid() && string.IsNullOrEmpty(previous.path) && !previous.isDirty;
            var scene = EditorSceneManager.OpenScene(scenePath, replaceUntitled ? OpenSceneMode.Single : OpenSceneMode.Additive);
            try
            {
                var root = scene.GetRootGameObjects().Single(gameObject => gameObject.name == Constant("SceneRootName"));
                Assert.That(root.transform.Find("Grid"), Is.Not.Null);
                var tilemaps = root.GetComponentsInChildren<Tilemap>(true).ToDictionary(tilemap => tilemap.name, StringComparer.Ordinal);
                Assert.That(tilemaps.Keys,
                    Is.EquivalentTo(new[] { "Terrain_Solid_Open", "Route_Recovery_Overlay", "Marker_Protection_Boundaries" }));
                Assert.That(tilemaps["Terrain_Solid_Open"].cellBounds.size.x, Is.EqualTo(48));
                Assert.That(tilemaps["Terrain_Solid_Open"].cellBounds.size.y, Is.EqualTo(32));
                Assert.That(CountTiles(tilemaps["Terrain_Solid_Open"]), Is.EqualTo(1536));
                Assert.That(CountTiles(tilemaps["Route_Recovery_Overlay"]), Is.GreaterThan(0));
                Assert.That(CountTiles(tilemaps["Marker_Protection_Boundaries"]), Is.GreaterThan(0));
                var camera = root.GetComponentsInChildren<Camera>(true).Single(value => value.name == "VIS01_Camera");
                Assert.That(camera.orthographic, Is.True);
                Assert.That(root.transform.Find("Legend"), Is.Not.Null);
                var metadata = root.transform.Find("Metadata");
                Assert.That(metadata, Is.Not.Null);
                Assert.That(metadata.GetComponent<TextMesh>().text, Does.Contain("MP_QA_01").And.Contain(Generation.LogicalMapDigest));
            }
            finally
            {
                if (replaceUntitled) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                else
                {
                    EditorSceneManager.CloseScene(scene, true);
                    if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                }
            }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceGrayboxPublisherWritesCsvJsonAndSceneManifestOnlyInVis01Roots()
        {
            Assert.That(OutputContents.Count, Is.EqualTo(13));
            Assert.That(OutputContents.Keys.Count(path => path.EndsWith(".csv", StringComparison.Ordinal)), Is.EqualTo(7));
            Assert.That(OutputContents.Keys.Count(path => path.EndsWith(".json", StringComparison.Ordinal)), Is.EqualTo(6));
            Assert.That(OutputContents.Keys.All(path => path.StartsWith("Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/VIS01/", StringComparison.Ordinal) ||
                                                        path.StartsWith("MapDesign/MCP/GENERATED/VIS01/", StringComparison.Ordinal)), Is.True);
            foreach (var output in OutputContents)
            {
                var bytes = File.ReadAllBytes(Resolve(output.Key));
                Assert.That(bytes.Take(3), Is.Not.EqualTo(new byte[] { 0xef, 0xbb, 0xbf }));
                var text = new System.Text.UTF8Encoding(false).GetString(bytes);
                Assert.That(text.Contains("\r"), Is.False);
                Assert.That(text.EndsWith("\n", StringComparison.Ordinal), Is.True);
                Assert.That(text.EndsWith("\n\n", StringComparison.Ordinal), Is.False);
                Assert.That(text, Is.EqualTo(output.Value));
            }
            var debugTilePaths = (IReadOnlyList<string>)InvokeBuilder("GetDebugTileAssetRelativePaths");
            Assert.That(debugTilePaths.All(path => File.Exists(Resolve(path))), Is.True);
            var debugTiles = debugTilePaths.Select(AssetDatabase.LoadAssetAtPath<Tile>).ToArray();
            Assert.That(debugTiles.Length, Is.EqualTo(8));
            Assert.That(debugTiles.All(tile => tile.sprite != null), Is.True);
            Assert.That(sourceAfter, Is.EqualTo(sourceBefore));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceGrayboxDigestIsRepeatReverseAndCultureStable()
        {
            var culture = CultureInfo.CurrentCulture;
            var uiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var reverse = InvokeBuilder("BuildSnapshot", ProjectRoot, true);
                var reverseGeneration = (MoonPalaceOneSectorGrayboxResult)Property(reverse, "Generation");
                var reverseOutputs = (IReadOnlyDictionary<string, string>)Property(reverse, "OutputContents");
                Assert.That(reverseGeneration.LogicalMapDigest, Is.EqualTo(Generation.LogicalMapDigest));
                Assert.That(Property(reverse, "SceneManifestDigest"), Is.EqualTo(Property(publication, "SceneManifestDigest")));
                Assert.That(Property(reverse, "DigestManifestDigest"), Is.EqualTo(Property(publication, "DigestManifestDigest")));
                Assert.That(reverseOutputs.Keys, Is.EqualTo(OutputContents.Keys));
                foreach (var key in OutputContents.Keys) Assert.That(reverseOutputs[key], Is.EqualTo(OutputContents[key]));
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = uiCulture;
            }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceGrayboxWorkDoesNotRunLegacyRegressionPlayModeBuildOrFullWorld()
        {
            var v = Generation.Validation;
            Assert.That(v.PriorTaskTestSelectionCount + v.Legacy19347SelectionCount + v.PlayModeSelectionCount +
                        v.UnfilteredTestSelectionCount + v.FullRegressionRunCount + v.FullWorldGenerationRunCount +
                        v.ExistingScenePrefabMutationCount + v.PlayerBuildExecutionCount + v.Vis02FileCount + v.Vis02RunCount, Is.Zero);
            Assert.That(EditorBuildSettings.scenes.Any(scene => scene.path == Constant("SceneRelativePath")), Is.False);
            Assert.That(OutputContents.Keys.Any(path => path.IndexOf("VIS02", StringComparison.OrdinalIgnoreCase) >= 0), Is.False);
            Assert.That(Generation.SeedId, Is.EqualTo("MP_QA_01"));
            Assert.That(Generation.SeedValue, Is.EqualTo(1924737067));
            Assert.That(Generation.SectorX, Is.EqualTo(6));
            Assert.That(Generation.SectorY, Is.EqualTo(6));
        }

        private MoonPalaceOneSectorGrayboxResult Generation =>
            (MoonPalaceOneSectorGrayboxResult)Property(publication, "Generation");
        private IReadOnlyDictionary<string, string> OutputContents =>
            (IReadOnlyDictionary<string, string>)Property(publication, "OutputContents");
        private static object Property(object instance, string name) => instance.GetType().GetProperty(name).GetValue(instance);
        private static object InvokeBuilder(string name, params object[] args) =>
            BuilderType.GetMethod(name, BindingFlags.Public | BindingFlags.Static).Invoke(null, args);
        private static string Constant(string name) => (string)BuilderType.GetField(name).GetRawConstantValue();
        private static Type BuilderType => Assembly.Load("MapAuthoring.Editor").GetType(BuilderTypeName, true);
        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        private static string Resolve(string relative) => Path.Combine(ProjectRoot, relative.Replace('/', Path.DirectorySeparatorChar));
        private static int CountTiles(Tilemap tilemap)
        {
            var count = 0;
            foreach (var position in tilemap.cellBounds.allPositionsWithin)
                if (tilemap.HasTile(position)) count++;
            return count;
        }
        private static string FileSha(string relative)
        {
            using (var stream = File.OpenRead(Resolve(relative)))
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }
}
