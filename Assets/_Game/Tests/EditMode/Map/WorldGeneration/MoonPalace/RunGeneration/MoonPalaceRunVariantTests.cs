using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.MoonPalace.RunGeneration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace StarNight.Tests.EditMode.Map.WorldGeneration.MoonPalace.RunGeneration
{
    public sealed class MoonPalaceRunVariantTests
    {
        private const string CategoryName = "RUN02";
        private const string BuilderTypeName = "StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.RunGeneration.MoonPalaceRunVariantSceneBuilder";
        private object publication;
        private IReadOnlyList<MoonPalaceRunVariantConfig> configs;
        private IReadOnlyList<RunVariantGraph> graphs;
        private IReadOnlyList<MoonPalaceRunVariantComposition> compositions;
        private MoonPalaceRunVariantAggregateValidation validation;

        [OneTimeSetUp]
        public void PublishRun02Once()
        {
            configs = MoonPalaceRunVariantConfig.CreateDefaults(); graphs = configs.Select(RunVariantGraph.Create).ToList();
            compositions = graphs.Select(graph => MoonPalaceRunVariantComposer.Compose(graph.Config, graph)).ToList();
            validation = MoonPalaceRunVariantValidator.ValidateAll(compositions); publication = InvokeBuilder("Publish", ProjectRoot);
        }

        [Test, Category(CategoryName)]
        public void RunVariantConfigDefinesThreePatternGridVariantsAndDerivesTileSizes()
        {
            Assert.That(configs.Select(config => config.VariantId), Is.EqualTo(new[] { "MP_RUN_02_A_BRANCHING_LOWLAND", "MP_RUN_02_B_VERTICAL_LOOP", "MP_RUN_02_C_MULTI_ROOM_SPLIT" }));
            Assert.That(configs.Select(config => config.PatternGridWidth + "x" + config.PatternGridHeight), Is.EqualTo(new[] { "48x14", "42x18", "60x16" }));
            Assert.That(configs.Select(config => config.TileWidth + "x" + config.TileHeight), Is.EqualTo(new[] { "192x56", "168x72", "240x64" }));
            Assert.That(configs.All(config => config.TileWidth == config.PatternGridWidth * 4 && config.TileHeight == config.PatternGridHeight * 4), Is.True);
        }

        [Test, Category(CategoryName)]
        public void RunVariantConfigRejectsSectorDimensionsRotationAndSilentCarve()
        {
            Assert.Throws<InvalidOperationException>(() => MoonPalaceRunVariantConfig.Create("SECTOR_COPY", "MP_QA_01", 1, 12, 8, 24, 40, 1, 2, 0, 0, MoonPalaceRunVariantTopology.BranchingLowland, false, false));
            Assert.Throws<InvalidOperationException>(() => MoonPalaceRunVariantConfig.Create("ROTATION", "MP_QA_01", 1, 48, 14, 48, 80, 6, 10, 0, 0, MoonPalaceRunVariantTopology.BranchingLowland, true, false));
            Assert.Throws<InvalidOperationException>(() => MoonPalaceRunVariantConfig.Create("SILENT_CARVE", "MP_QA_01", 1, 48, 14, 48, 80, 6, 10, 0, 0, MoonPalaceRunVariantTopology.BranchingLowland, false, true));
            Assert.That(configs.Select(config => config.VariantId).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(3));
        }

        [Test, Category(CategoryName)]
        public void RunRoomGraphCreatesBranchingLowlandVerticalLoopAndMultiRoomSplitVariants()
        {
            Assert.That(graphs[0].BranchCount, Is.InRange(6, 10)); Assert.That(graphs[0].SplitRejoinCount, Is.Zero);
            Assert.That(graphs[1].BranchCount, Is.InRange(8, 12)); Assert.That(graphs[1].SplitRejoinCount, Is.GreaterThanOrEqualTo(2));
            Assert.That(graphs[2].BranchCount, Is.InRange(10, 14)); Assert.That(graphs[2].SplitRejoinCount, Is.GreaterThanOrEqualTo(2));
            Assert.That(graphs.Select(graph => graph.Digest.Value).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(3));
        }

        [Test, Category(CategoryName)]
        public void RunRoomGraphKeepsNodesInsideGridAndRecordsBranchesSplitsRejoins()
        {
            foreach (var graph in graphs)
            {
                Assert.That(graph.Nodes.All(node => graph.Contains(node.Coordinate)), Is.True);
                Assert.That(graph.Nodes.Select(node => node.Coordinate).Distinct().Count(), Is.EqualTo(graph.Nodes.Count));
                Assert.That(graph.BranchEntrySlots.Count, Is.EqualTo(graph.BranchCount)); Assert.That(graph.Start.Slot.X, Is.Not.EqualTo(graph.Exit.Slot.X));
                Assert.That(graph.Edges.All(edge => Math.Abs(edge.From.X - edge.To.X) + Math.Abs(edge.From.Y - edge.To.Y) == 1), Is.True);
            }
            Assert.That(graphs[1].SplitNodes.Count, Is.GreaterThanOrEqualTo(2)); Assert.That(graphs[1].RejoinNodes.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(graphs[2].RoomRegions.Count, Is.GreaterThanOrEqualTo(3));
        }

        [Test, Category(CategoryName)]
        public void RunVariantComposerUses500CandidatePoolAndPlacesAll2388PatternSlots()
        {
            Assert.That(compositions.All(composition => composition.CandidateSet.RawMaskCount == 65536 && composition.CandidateSet.Candidates.Count == 500), Is.True);
            Assert.That(compositions.Select(composition => composition.PlacementCount), Is.EqualTo(new[] { 672, 756, 960 }));
            Assert.That(compositions.Sum(composition => composition.PlacementCount), Is.EqualTo(2388));
            Assert.That(compositions.All(composition => composition.Placements.All(placement => placement.Transform == "IDENTITY_NO_ROTATION" && !string.IsNullOrEmpty(placement.SelectionReason))), Is.True);
        }

        [Test, Category(CategoryName)]
        public void RunVariantComposerRejectsSocketMismatchStaticCopyAndSilentCarve()
        {
            Assert.Throws<InvalidOperationException>(() => MoonPalaceRunVariantComposer.RequireReciprocalSockets(null, compositions[0].CandidateSet.Candidates[0], MoonPalaceRunDirection.East));
            Assert.That(compositions.All(composition => composition.FallbackCarveCount == 0 && composition.SilentRepairCount == 0), Is.True);
            Assert.That(compositions.All(composition => composition.Placements.All(placement => placement.Candidate.CandidateId.StartsWith("VIS01_MP_", StringComparison.Ordinal))), Is.True);
        }

        [Test, Category(CategoryName)]
        public void RunVariantValidatorProvesStartToExitReachabilityForAllThreeVariants()
        {
            Assert.That(validation.VariantCount, Is.EqualTo(3)); Assert.That(validation.AllVariantBfsPassCount, Is.EqualTo(3));
            Assert.That(validation.Variants.All(result => result.StartOpen && result.ExitOpen && result.StartToExitReachable && result.Passed), Is.True);
        }

        [Test, Category(CategoryName)]
        public void RunVariantValidatorProvesBranchEntriesAndSplitRejoinsReachable()
        {
            Assert.That(validation.Variants.All(result => result.ReachableBranchEntryCount == result.BranchEntryCount), Is.True);
            Assert.That(validation.Variants.Where(result => result.Composition.Graph.SplitRejoinCount > 0).All(result => result.ReachableSplitRejoinWaypointCount == result.SplitRejoinWaypointCount), Is.True);
            Assert.That(validation.Variants.All(result => result.UnreachableRouteBranchIslandCount == 0 && result.RouteFailureCount == 0), Is.True);
        }

        [Test, Category(CategoryName)]
        public void RunVariantValidatorRecordsVerticalityRoomDensityAndStructuralDifference()
        {
            Assert.That(graphs[1].VerticalSpanRows, Is.GreaterThanOrEqualTo(8));
            Assert.That(graphs[2].RoomRegions.Select(room => room.IntendedDensity).Distinct().Count(), Is.GreaterThanOrEqualTo(3));
            Assert.That(graphs[2].RoomRegions.Select(room => room.Pacing).Distinct().Count(), Is.GreaterThanOrEqualTo(3));
            Assert.That(validation.TotalPatternPlacements, Is.EqualTo(2388));
        }

        [Test, Category(CategoryName)]
        public void RunVariantSceneBuilderCreatesComparisonSceneWithThreeVariantRoots()
        {
            var scenePath = Constant("SceneRelativePath"); Assert.That(File.Exists(Resolve(scenePath)), Is.True);
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                var root = scene.GetRootGameObjects().Single(gameObject => gameObject.name == Constant("SceneRootName"));
                Assert.That(root.transform.Find("Variant_A"), Is.Not.Null); Assert.That(root.transform.Find("Variant_B"), Is.Not.Null); Assert.That(root.transform.Find("Variant_C"), Is.Not.Null);
                Assert.That(root.GetComponentsInChildren<Camera>(true).Single(camera => camera.name == "RUN02_ComparisonCamera").orthographic, Is.True);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test, Category(CategoryName)]
        public void RunVariantSceneBuilderShowsPatternBoundariesRoutesBranchesLabelsAndLegend()
        {
            var scene = EditorSceneManager.OpenScene(Constant("SceneRelativePath"), OpenSceneMode.Additive);
            try
            {
                var root = scene.GetRootGameObjects().Single(gameObject => gameObject.name == Constant("SceneRootName"));
                foreach (var variant in new[] { "Variant_A", "Variant_B", "Variant_C" })
                {
                    var transform = root.transform.Find(variant); Assert.That(transform.Find("Grid/Terrain").GetComponent<Tilemap>(), Is.Not.Null);
                    Assert.That(transform.Find("Grid/RouteBranch").GetComponent<Tilemap>(), Is.Not.Null); Assert.That(transform.Find("Grid/PatternBoundarySocketMarker").GetComponent<Tilemap>(), Is.Not.Null);
                    Assert.That(transform.Find("Start"), Is.Not.Null); Assert.That(transform.Find("Exit"), Is.Not.Null); Assert.That(transform.Find("Label").GetComponent<TextMesh>().text, Does.Contain("MP_RUN_02_"));
                }
                Assert.That(root.transform.Find("Legend"), Is.Not.Null); Assert.That(root.transform.Find("Metadata").GetComponent<TextMesh>().text, Does.Contain("placements=2388"));
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test, Category(CategoryName)]
        public void RunVariantArtifactsAreDeterministicAcrossRepeatReverseAndCulture()
        {
            var culture = CultureInfo.CurrentCulture; var uiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR"); CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var reverse = MoonPalaceRunVariantComposer.ComposeDefaults(true); var repeat = MoonPalaceRunVariantComposer.ComposeDefaults(false); var reversePublication = InvokeBuilder("BuildSnapshot", ProjectRoot, true);
                Assert.That(reverse.Select(composition => composition.CompositionDigest), Is.EqualTo(compositions.Select(composition => composition.CompositionDigest)));
                Assert.That(repeat.Select(composition => composition.MapDigest), Is.EqualTo(compositions.Select(composition => composition.MapDigest)));
                Assert.That(Property(reversePublication, "SceneManifestDigest"), Is.EqualTo(Property(publication, "SceneManifestDigest")));
                var outputs = (IReadOnlyDictionary<string, string>)Property(publication, "OutputContents"); var reverseOutputs = (IReadOnlyDictionary<string, string>)Property(reversePublication, "OutputContents");
                Assert.That(reverseOutputs.Keys, Is.EqualTo(outputs.Keys)); foreach (var path in outputs.Keys) Assert.That(reverseOutputs[path], Is.EqualTo(outputs[path]));
            }
            finally { CultureInfo.CurrentCulture = culture; CultureInfo.CurrentUICulture = uiCulture; }
        }

        [Test, Category(CategoryName)]
        public void RunVariantWritesOnlyRun02RootsAndDoesNotMutateExistingScenes()
        {
            var outputs = (IReadOnlyDictionary<string, string>)Property(publication, "OutputContents");
            Assert.That(outputs.Count, Is.EqualTo(12)); Assert.That(outputs.Keys.Count(path => path.EndsWith(".csv", StringComparison.Ordinal)), Is.EqualTo(6)); Assert.That(outputs.Keys.Count(path => path.EndsWith(".json", StringComparison.Ordinal)), Is.EqualTo(6));
            Assert.That(outputs.Keys.All(path => path.StartsWith("Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN02/", StringComparison.Ordinal) || path.StartsWith("MapDesign/MCP/GENERATED/RUN02/", StringComparison.Ordinal)), Is.True);
            foreach (var output in outputs)
            {
                var bytes = File.ReadAllBytes(Resolve(output.Key)); Assert.That(bytes.Take(3), Is.Not.EqualTo(new byte[] { 0xef, 0xbb, 0xbf })); var text = new System.Text.UTF8Encoding(false).GetString(bytes);
                Assert.That(text.Contains("\r"), Is.False); Assert.That(text.EndsWith("\n", StringComparison.Ordinal), Is.True); Assert.That(text.EndsWith("\n\n", StringComparison.Ordinal), Is.False); Assert.That(text, Is.EqualTo(output.Value));
            }
            Assert.That((IReadOnlyList<string>)InvokeBuilder("GetDebugTileAssetRelativePaths"), Is.All.StartWith("Assets/_Game/Map/Scenes/MoonPalace/RUN02/"));
            Assert.That(EditorBuildSettings.scenes.Any(scene => scene.path == Constant("SceneRelativePath")), Is.False);
        }

        [Test, Category(CategoryName)]
        public void RunVariantWorkDoesNotRunLegacyRegressionPlayModeBuildFullWorldOrRun03()
        {
            Assert.That(validation.Passed, Is.True); Assert.That(Constant("SceneRelativePath"), Does.Contain("/RUN02/").And.Not.Contain("RUN03").And.Not.Contain("VIS03"));
            Assert.That(EditorApplication.isPlayingOrWillChangePlaymode, Is.False); Assert.That(validation.TotalRouteSocketMismatchCount, Is.Zero); Assert.That(validation.TotalFallbackCarveCount, Is.Zero); Assert.That(validation.TotalSilentRepairCount, Is.Zero);
        }

        private static object InvokeBuilder(string methodName, params object[] arguments) => Assembly.Load("MapAuthoring.Editor").GetType(BuilderTypeName, true).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static).Invoke(null, arguments);
        private static object Property(object source, string name) => source.GetType().GetProperty(name).GetValue(source, null);
        private static string Constant(string name) => (string)Assembly.Load("MapAuthoring.Editor").GetType(BuilderTypeName, true).GetField(name, BindingFlags.Public | BindingFlags.Static).GetValue(null);
        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        private static string Resolve(string relativePath) => Path.Combine(ProjectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
