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
    public sealed class MoonPalaceMicroPatternRunTests
    {
        private const string CategoryName = "RUN01";
        private const string BuilderTypeName =
            "StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.RunGeneration.MoonPalaceMicroPatternRunSceneBuilder";

        private object publication;
        private MoonPalaceMicroPatternRunConfig config;
        private MoonPalaceMicroPatternRunGraph graph;
        private MoonPalaceMicroPatternRunComposition composition;
        private MoonPalaceMicroPatternRunValidation validation;

        [OneTimeSetUp]
        public void PublishRun01Once()
        {
            config = MoonPalaceMicroPatternRunConfig.CreateDefault();
            graph = MoonPalaceMicroPatternRunGraph.Create(config);
            composition = MoonPalaceMicroPatternRunComposer.Compose(config, graph);
            validation = MoonPalaceMicroPatternRunValidator.Validate(composition);
            publication = InvokeBuilder("Publish", ProjectRoot);
        }

        [Test, Category(CategoryName)]
        public void MicroPatternRunConfigDerivesTileSizeFromPatternGridNotSectorConstants()
        {
            Assert.That(config.PatternWidth, Is.EqualTo(4));
            Assert.That(config.PatternHeight, Is.EqualTo(4));
            Assert.That(config.PatternGridWidth, Is.EqualTo(40));
            Assert.That(config.PatternGridHeight, Is.EqualTo(12));
            Assert.That(config.TileWidth, Is.EqualTo(config.PatternGridWidth * config.PatternWidth).And.EqualTo(160));
            Assert.That(config.TileHeight, Is.EqualTo(config.PatternGridHeight * config.PatternHeight).And.EqualTo(48));
            Assert.That(config.Allow90DegreeRotation, Is.False);
            Assert.That(config.AllowSilentCarve, Is.False);
        }

        [Test, Category(CategoryName)]
        public void MicroPatternRunGraphCreatesLeftToRightMainPathWithBranchesInsideGrid()
        {
            Assert.That(graph.MainPathNodes.Count, Is.InRange(config.MainPathMinSteps, config.MainPathMaxSteps));
            Assert.That(graph.MainPathNodes.First().X, Is.EqualTo(0));
            Assert.That(graph.MainPathNodes.Last().X, Is.EqualTo(config.PatternGridWidth - 1));
            Assert.That(graph.Branches.Count, Is.InRange(config.BranchMinCount, config.BranchMaxCount));
            Assert.That(graph.Nodes.All(node => graph.Contains(node.Coordinate)), Is.True);
            Assert.That(graph.MainPathNodes.Distinct().Count(), Is.EqualTo(graph.MainPathNodes.Count));
            Assert.That(graph.Edges.All(edge => Math.Abs(edge.From.X - edge.To.X) + Math.Abs(edge.From.Y - edge.To.Y) == 1), Is.True);
            Assert.That(graph.Digest.Value, Has.Length.EqualTo(64));
        }

        [Test, Category(CategoryName)]
        public void MicroPatternRunComposerUses500CandidatePoolAndRecordsSelectionReasons()
        {
            Assert.That(composition.CandidateSet.RawMaskCount, Is.EqualTo(65536));
            Assert.That(composition.CandidateSet.Candidates.Count, Is.EqualTo(500));
            Assert.That(composition.Placements.All(placement => composition.CandidateSet.Candidates.Any(candidate => candidate.CandidateId == placement.Candidate.CandidateId)), Is.True);
            Assert.That(composition.Placements.All(placement => !string.IsNullOrEmpty(placement.SelectionReason) &&
                !string.IsNullOrEmpty(placement.Candidate.SocketSignature) && placement.Transform == "IDENTITY_NO_ROTATION"), Is.True);
            Assert.That(composition.Placements.All(placement => placement.PresentationFamilyLink == "MAP21_02_READ_ONLY_PRESENTATION_FAMILY"), Is.True);
            Assert.That(composition.SelectionAttemptCount, Is.GreaterThanOrEqualTo(composition.PlacementCount));
        }

        [Test, Category(CategoryName)]
        public void MicroPatternRunComposerRejectsSocketMismatchAndSilentCarve()
        {
            Assert.Throws<InvalidOperationException>(() => MoonPalaceMicroPatternRunComposer.RequireReciprocalSockets(null,
                composition.CandidateSet.Candidates[0], MoonPalaceRunDirection.East));
            Assert.Throws<InvalidOperationException>(() => MoonPalaceMicroPatternRunConfig.Create("MP_RUN_01", "MP_QA_01", 1924737067,
                4, 4, 40, 12, 500, 40, 56, 4, 8, false, true));
            Assert.That(composition.FallbackCarveCount, Is.Zero);
            Assert.That(composition.SilentRepairCount, Is.Zero);
        }

        [Test, Category(CategoryName)]
        public void MicroPatternRunComposerPlacesAll480PatternSlots()
        {
            Assert.That(composition.PlacementCount, Is.EqualTo(480));
            Assert.That(composition.Placements.Select(placement => placement.Coordinate).Distinct().Count(), Is.EqualTo(480));
            Assert.That(composition.Placements.Min(placement => placement.Coordinate.X), Is.EqualTo(0));
            Assert.That(composition.Placements.Max(placement => placement.Coordinate.X), Is.EqualTo(39));
            Assert.That(composition.Placements.Min(placement => placement.Coordinate.Y), Is.EqualTo(0));
            Assert.That(composition.Placements.Max(placement => placement.Coordinate.Y), Is.EqualTo(11));
            Assert.That(composition.RoutePlacementCount + composition.FillerDetailPlacementCount, Is.EqualTo(480));
        }

        [Test, Category(CategoryName)]
        public void MicroPatternRunValidatorProvesTileLevelStartToExitReachability()
        {
            Assert.That(validation.TileWidth, Is.EqualTo(160));
            Assert.That(validation.TileHeight, Is.EqualTo(48));
            Assert.That(validation.StartOpen, Is.True);
            Assert.That(validation.ExitOpen, Is.True);
            Assert.That(validation.StartToExitReachable, Is.True);
            Assert.That(validation.ReachableTiles, Has.Member(validation.StartTile));
            Assert.That(validation.ReachableTiles, Has.Member(validation.ExitTile));
        }

        [Test, Category(CategoryName)]
        public void MicroPatternRunValidatorReportsBranchesReachableFromMainPath()
        {
            Assert.That(validation.BranchEntryCount, Is.InRange(config.BranchMinCount, config.BranchMaxCount));
            Assert.That(validation.ReachableBranchEntryCount, Is.EqualTo(validation.BranchEntryCount));
            Assert.That(composition.BranchRouteTiles.Count, Is.GreaterThan(0));
            Assert.That(composition.BranchEntryTiles.All(tile => validation.ReachableTiles.Contains(tile)), Is.True);
        }

        [Test, Category(CategoryName)]
        public void MicroPatternRunValidatorKeepsFallbackCarveSilentRepairAndRouteFailuresZero()
        {
            Assert.That(validation.RouteSocketMismatchCount, Is.Zero);
            Assert.That(validation.OutOfBoundsPlacementCount, Is.Zero);
            Assert.That(validation.DuplicatePlacementCount, Is.Zero);
            Assert.That(validation.RouteFailureCount, Is.Zero);
            Assert.That(validation.FallbackCarveCount, Is.Zero);
            Assert.That(validation.SilentRepairCount, Is.Zero);
            Assert.That(validation.Passed, Is.True);
        }

        [Test, Category(CategoryName)]
        public void MicroPatternRunSceneBuilderCreatesIsolatedSceneWithStartExitLegendAndPatternGrid()
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
                Assert.That(tilemaps.Keys, Is.EquivalentTo(new[] { "Terrain_Solid_Open", "Route_Main_Branches", "Pattern_Boundaries_Sockets_Protection" }));
                Assert.That(tilemaps["Terrain_Solid_Open"].cellBounds.size.x, Is.EqualTo(160));
                Assert.That(tilemaps["Terrain_Solid_Open"].cellBounds.size.y, Is.EqualTo(48));
                Assert.That(CountTiles(tilemaps["Terrain_Solid_Open"]), Is.EqualTo(7680));
                Assert.That(CountTiles(tilemaps["Route_Main_Branches"]), Is.GreaterThan(0));
                Assert.That(root.transform.Find("StartMarker"), Is.Not.Null);
                Assert.That(root.transform.Find("ExitMarker"), Is.Not.Null);
                Assert.That(root.transform.Find("Legend"), Is.Not.Null);
                Assert.That(root.transform.Find("Metadata").GetComponent<TextMesh>().text, Does.Contain("MP_RUN_01").And.Contain(composition.MapDigest));
                Assert.That(root.GetComponentsInChildren<Camera>(true).Single(camera => camera.name == "RUN01_FullRunCamera").orthographic, Is.True);
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
        public void MicroPatternRunArtifactsAreDeterministicAcrossRepeatReverseAndCulture()
        {
            var culture = CultureInfo.CurrentCulture;
            var uiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var reverse = MoonPalaceMicroPatternRunComposer.Compose(config, graph, true);
                var repeat = MoonPalaceMicroPatternRunComposer.Compose(config, graph, false);
                var reversePublication = InvokeBuilder("BuildSnapshot", ProjectRoot, true);
                Assert.That(reverse.CompositionDigest, Is.EqualTo(composition.CompositionDigest));
                Assert.That(reverse.MapDigest, Is.EqualTo(composition.MapDigest));
                Assert.That(repeat.CompositionDigest, Is.EqualTo(composition.CompositionDigest));
                Assert.That(Property(reversePublication, "SceneManifestDigest"), Is.EqualTo(Property(publication, "SceneManifestDigest")));
                Assert.That(Property(reversePublication, "DigestManifestDigest"), Is.EqualTo(Property(publication, "DigestManifestDigest")));
                var reverseOutputs = (IReadOnlyDictionary<string, string>)Property(reversePublication, "OutputContents");
                var outputs = (IReadOnlyDictionary<string, string>)Property(publication, "OutputContents");
                Assert.That(reverseOutputs.Keys, Is.EqualTo(outputs.Keys));
                foreach (var path in outputs.Keys) Assert.That(reverseOutputs[path], Is.EqualTo(outputs[path]));
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = uiCulture;
            }
        }

        [Test, Category(CategoryName)]
        public void MicroPatternRunWritesOnlyRun01RootsAndDoesNotMutateExistingScenes()
        {
            var outputs = (IReadOnlyDictionary<string, string>)Property(publication, "OutputContents");
            Assert.That(outputs.Count, Is.EqualTo(11));
            Assert.That(outputs.Keys.Count(path => path.EndsWith(".csv", StringComparison.Ordinal)), Is.EqualTo(5));
            Assert.That(outputs.Keys.Count(path => path.EndsWith(".json", StringComparison.Ordinal)), Is.EqualTo(6));
            Assert.That(outputs.Keys.All(path => path.StartsWith("Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN01/", StringComparison.Ordinal) ||
                path.StartsWith("MapDesign/MCP/GENERATED/RUN01/", StringComparison.Ordinal)), Is.True);
            foreach (var output in outputs)
            {
                var bytes = File.ReadAllBytes(Resolve(output.Key));
                Assert.That(bytes.Take(3), Is.Not.EqualTo(new byte[] { 0xef, 0xbb, 0xbf }));
                var text = new System.Text.UTF8Encoding(false).GetString(bytes);
                Assert.That(text.Contains("\r"), Is.False);
                Assert.That(text.EndsWith("\n", StringComparison.Ordinal), Is.True);
                Assert.That(text.EndsWith("\n\n", StringComparison.Ordinal), Is.False);
                Assert.That(text, Is.EqualTo(output.Value));
            }
            Assert.That((IReadOnlyList<string>)InvokeBuilder("GetDebugTileAssetRelativePaths"), Is.All.StartWith("Assets/_Game/Map/Scenes/MoonPalace/RUN01/"));
            Assert.That(EditorBuildSettings.scenes.Any(scene => scene.path == Constant("SceneRelativePath")), Is.False);
        }

        [Test, Category(CategoryName)]
        public void MicroPatternRunDoesNotRunLegacyRegressionPlayModeBuildFullWorldOrRun02()
        {
            Assert.That(validation.Passed, Is.True);
            Assert.That(config.RunId, Is.EqualTo("MP_RUN_01"));
            Assert.That(Constant("SceneRelativePath"), Does.Contain("/RUN01/").And.Not.Contain("VIS03").And.Not.Contain("RUN02"));
            Assert.That(EditorApplication.isPlayingOrWillChangePlaymode, Is.False);
        }

        private static object InvokeBuilder(string methodName, params object[] arguments)
        {
            var type = Assembly.Load("MapAuthoring.Editor").GetType(BuilderTypeName, true);
            return type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static).Invoke(null, arguments);
        }

        private static object Property(object source, string name) => source.GetType().GetProperty(name).GetValue(source, null);
        private static string Constant(string name) => (string)Assembly.Load("MapAuthoring.Editor").GetType(BuilderTypeName, true)
            .GetField(name, BindingFlags.Public | BindingFlags.Static).GetValue(null);
        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        private static string Resolve(string relativePath) => Path.Combine(ProjectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        private static int CountTiles(Tilemap tilemap)
        {
            var count = 0;
            foreach (var position in tilemap.cellBounds.allPositionsWithin) if (tilemap.GetTile(position) != null) count++;
            return count;
        }
    }
}
