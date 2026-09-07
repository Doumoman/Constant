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
    public sealed class MoonPalaceCameraRoomCourseTests
    {
        private const string CategoryName = "RUN03";
        private const string BuilderTypeName = "StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.RunGeneration.MoonPalaceCameraRoomCourseSceneBuilder";
        private MoonPalaceCameraRoomCourseConfig config;
        private MoonPalaceCameraRoomGraph graph;
        private MoonPalaceCameraRoomConnectorPlan connectors;
        private MoonPalaceCameraRoomPatternComposition composition;
        private MoonPalaceCameraRoomCourseValidation validation;
        private object publication;

        [OneTimeSetUp]
        public void PublishRun03Once()
        {
            config = MoonPalaceCameraRoomCourseConfig.CreateDefault(); graph = MoonPalaceCameraRoomGraph.Create(config);
            connectors = MoonPalaceCameraRoomConnectorPlanner.Plan(graph); composition = MoonPalaceCameraRoomPatternComposer.Compose(config, graph, connectors);
            validation = MoonPalaceCameraRoomCourseValidator.Validate(composition); publication = InvokeBuilder("Publish", ProjectRoot);
        }

        [Test, Category(CategoryName)]
        public void CameraRoomCourseConfigDerivesCourseAndRoomTileBoundsFromPatternGrid()
        {
            Assert.That(config.CourseId, Is.EqualTo("MP_CAMERA_RUN_01")); Assert.That(config.PatternWidth + "x" + config.PatternHeight, Is.EqualTo("4x4"));
            Assert.That(config.CoursePatternGridWidth + "x" + config.CoursePatternGridHeight, Is.EqualTo("80x24")); Assert.That(config.TileWidth + "x" + config.TileHeight, Is.EqualTo("320x96"));
            Assert.That(config.TileWidth, Is.EqualTo(config.CoursePatternGridWidth * 4)); Assert.That(config.TileHeight, Is.EqualTo(config.CoursePatternGridHeight * 4));
            Assert.That(graph.Rooms.All(room => room.Bounds.TileWidth == room.Bounds.Width * 4 && room.Bounds.TileHeight == room.Bounds.Height * 4), Is.True);
        }

        [Test, Category(CategoryName)]
        public void CameraRoomCourseConfigRejectsSectorSizingRotationAndSilentCarve()
        {
            Assert.Throws<InvalidOperationException>(() => MoonPalaceCameraRoomCourseConfig.Create("SECTOR_COPY", "MP_QA_01", 1, 12, 8, 9, 12, 7, 9, 2, 4, 2, 3, false, false));
            Assert.Throws<InvalidOperationException>(() => MoonPalaceCameraRoomCourseConfig.Create("ROTATION", "MP_QA_01", 1, 80, 24, 9, 12, 7, 9, 2, 4, 2, 3, true, false));
            Assert.Throws<InvalidOperationException>(() => MoonPalaceCameraRoomCourseConfig.Create("SILENT", "MP_QA_01", 1, 80, 24, 9, 12, 7, 9, 2, 4, 2, 3, false, true));
            Assert.Throws<InvalidOperationException>(() => config.ValidateRoomBounds(new[] { new CameraRoomBoundsPattern("OUTSIDE", 0, 0, 80, 1) }));
        }

        [Test, Category(CategoryName)]
        public void CameraRoomGraphCreatesNineToTwelveRoomsWithMainBranchAndSplitRejoinRoutes()
        {
            Assert.That(graph.CameraRoomCount, Is.InRange(9, 12)); Assert.That(graph.MainRouteRoomCount, Is.InRange(7, 9)); Assert.That(graph.BranchRoomCount, Is.InRange(2, 4)); Assert.That(graph.SplitRejoinCount, Is.InRange(2, 3));
            Assert.That(graph.VerticalTransitionSpanPatternRows, Is.GreaterThanOrEqualTo(10)); Assert.That(graph.Start.Slot, Is.Not.EqualTo(graph.Exit.Slot));
            Assert.That(graph.Digest.Value, Is.Not.Empty);
        }

        [Test, Category(CategoryName)]
        public void CameraRoomGraphKeepsRoomBoundsInsideGridAndStructurallySeparated()
        {
            Assert.That(graph.Rooms.All(room => room.Bounds.IsInside(config.CoursePatternGridWidth, config.CoursePatternGridHeight) && room.Bounds.Width > 0 && room.Bounds.Height > 0), Is.True);
            for (var left = 0; left < graph.Rooms.Count; left++) for (var right = left + 1; right < graph.Rooms.Count; right++) Assert.That(graph.Rooms[left].Bounds.Overlaps(graph.Rooms[right].Bounds), Is.False);
            Assert.That(graph.MainRoute.RoomIds.First(), Is.EqualTo("R01_START")); Assert.That(graph.MainRoute.RoomIds.Last(), Is.EqualTo("R08_EXIT"));
            Assert.That(graph.Rooms.Single(room => room.RoomId == "R03_VERTICAL_ASCENT").Bounds.Height, Is.GreaterThanOrEqualTo(10));
        }

        [Test, Category(CategoryName)]
        public void CameraRoomConnectorPlannerCreatesReciprocalOpenRoomGates()
        {
            Assert.That(connectors.ConnectorCount, Is.EqualTo(9)); Assert.That(connectors.Gates.All(gate => MoonPalaceCameraRoomConnectorPlanner.IsReciprocal(graph, gate)), Is.True);
            Assert.That(connectors.Gates.All(gate => composition.IsOpen(gate.FromTileGateRect.X, gate.FromTileGateRect.Y) && composition.IsOpen(gate.ToTileGateRect.X, gate.ToTileGateRect.Y)), Is.True);
            Assert.That(connectors.Gates.Select(gate => gate.ConnectorId).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(connectors.ConnectorCount));
        }

        [Test, Category(CategoryName)]
        public void CameraRoomConnectorPlannerDoesNotUseSectorSeamLogic()
        {
            Assert.That(connectors.UsesSectorSeamLogic, Is.False); Assert.That(config.TileWidth + "x" + config.TileHeight, Is.Not.EqualTo("48x32"));
            Assert.That(connectors.Gates.All(gate => gate.FromTileGateRect.Width == 1 && gate.ToTileGateRect.Height == 1), Is.True);
            Assert.That(connectors.CanonicalDigest, Is.Not.Empty);
        }

        [Test, Category(CategoryName)]
        public void CameraRoomPatternComposerUses500CandidatePoolAndPlacesAll1920Slots()
        {
            Assert.That(composition.CandidateSet.RawMaskCount, Is.EqualTo(65536)); Assert.That(composition.CandidateSet.Candidates.Count, Is.EqualTo(500)); Assert.That(composition.PlacementCount, Is.EqualTo(1920));
            Assert.That(composition.Placements.All(placement => placement.Transform == "IDENTITY_NO_ROTATION" && placement.Candidate.CandidateId.StartsWith("VIS01_MP_", StringComparison.Ordinal)), Is.True);
            Assert.That(composition.RotationCount, Is.Zero);
        }

        [Test, Category(CategoryName)]
        public void CameraRoomPatternComposerRecordsRoomConnectorRouteOwnershipPerSlot()
        {
            Assert.That(composition.Placements.All(placement => !string.IsNullOrWhiteSpace(placement.RoomId) && !string.IsNullOrWhiteSpace(placement.RouteOwnership) && !string.IsNullOrWhiteSpace(placement.SelectionReason)), Is.True);
            Assert.That(composition.Placements.Where(placement => placement.ConnectorIds.Count > 0).All(placement => placement.RequiredDirections.Count > 0), Is.True);
            Assert.That(composition.Placements.Count(placement => placement.RouteOwnership == "MAIN"), Is.GreaterThan(0)); Assert.That(composition.Placements.Count(placement => placement.RouteOwnership == "BRANCH"), Is.GreaterThan(0)); Assert.That(composition.Placements.Count(placement => placement.RouteOwnership == "SPLIT_REJOIN"), Is.GreaterThan(0));
        }

        [Test, Category(CategoryName)]
        public void CameraRoomPatternComposerRejectsBlockedConnectorsStaticCopiesAndSilentCarve()
        {
            Assert.Throws<InvalidOperationException>(() => MoonPalaceCameraRoomPatternComposer.RequireReciprocalSockets(null, composition.CandidateSet.Candidates[0], MoonPalaceRunDirection.East));
            Assert.That(composition.FallbackCarveCount, Is.Zero); Assert.That(composition.SilentRepairCount, Is.Zero); Assert.That(composition.Placements.All(placement => placement.AttemptCount > 0), Is.True);
            Assert.That(composition.CompositionDigest, Is.Not.Empty);
        }

        [Test, Category(CategoryName)]
        public void CameraRoomCourseValidatorProvesStartToExitAndRoomToRoomReachability()
        {
            Assert.That(validation.StartOpen && validation.ExitOpen && validation.StartToExitReachable, Is.True); Assert.That(validation.AllRoomBoundsInside && validation.AllConnectorGatesReciprocal && validation.AllConnectorGatesOpen, Is.True);
            Assert.That(validation.AllMainRouteRoomsReachableInOrder, Is.True); Assert.That(validation.UnreachableRequiredRoomCount, Is.Zero); Assert.That(validation.RouteSocketMismatchCount, Is.Zero); Assert.That(validation.ConnectorBlockedCount, Is.Zero);
        }

        [Test, Category(CategoryName)]
        public void CameraRoomCourseValidatorProvesBranchAndSplitRejoinReachability()
        {
            Assert.That(validation.AllBranchEntriesReachable, Is.True); Assert.That(validation.AllSplitRejoinPathsReachable, Is.True); Assert.That(graph.BranchRoutes.All(route => route.PatternSlots.Any()), Is.True); Assert.That(graph.SplitRejoinRoutes.All(route => route.AlternateSlots.Count >= 2), Is.True);
            Assert.That(validation.Passed, Is.True);
        }

        [Test, Category(CategoryName)]
        public void CameraRoomSceneBuilderCreatesIsolatedSceneWithRoomFramesConnectorsLabelsAndLegend()
        {
            var scenePath = Constant("SceneRelativePath"); Assert.That(File.Exists(Resolve(scenePath)), Is.True);
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                var root = scene.GetRootGameObjects().Single(gameObject => gameObject.name == Constant("SceneRootName"));
                Assert.That(root.transform.Find("Grid/Terrain").GetComponent<Tilemap>(), Is.Not.Null); Assert.That(root.transform.Find("Grid/MainRoute").GetComponent<Tilemap>(), Is.Not.Null);
                Assert.That(root.transform.Find("Grid/BranchSplitRejoin").GetComponent<Tilemap>(), Is.Not.Null); Assert.That(root.transform.Find("Grid/RoomFrameConnector").GetComponent<Tilemap>(), Is.Not.Null);
                Assert.That(root.transform.Find("Start"), Is.Not.Null); Assert.That(root.transform.Find("Exit"), Is.Not.Null); Assert.That(root.transform.Find("CameraRoomLabels").childCount, Is.InRange(9, 12)); Assert.That(root.transform.Find("ConnectorLabels").childCount, Is.EqualTo(connectors.ConnectorCount));
                Assert.That(root.transform.Find("Legend"), Is.Not.Null); Assert.That(root.transform.Find("Metadata").GetComponent<TextMesh>().text, Does.Contain("320x96")); Assert.That(root.GetComponentsInChildren<Camera>(true).Single(camera => camera.name == "RUN03_CourseCamera").orthographic, Is.True);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test, Category(CategoryName)]
        public void CameraRoomArtifactsAreDeterministicAcrossRepeatReverseAndCulture()
        {
            var culture = CultureInfo.CurrentCulture; var uiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR"); CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var reverse = MoonPalaceCameraRoomPatternComposer.Compose(config, graph, connectors, true); var repeat = MoonPalaceCameraRoomPatternComposer.Compose(config, graph, connectors, false); var reversePublication = InvokeBuilder("BuildSnapshot", ProjectRoot, true);
                Assert.That(reverse.CompositionDigest, Is.EqualTo(composition.CompositionDigest)); Assert.That(repeat.MapDigest, Is.EqualTo(composition.MapDigest)); Assert.That(Property(reversePublication, "SceneManifestDigest"), Is.EqualTo(Property(publication, "SceneManifestDigest")));
                var outputs = (IReadOnlyDictionary<string, string>)Property(publication, "OutputContents"); var reverseOutputs = (IReadOnlyDictionary<string, string>)Property(reversePublication, "OutputContents"); Assert.That(reverseOutputs.Keys, Is.EqualTo(outputs.Keys)); foreach (var path in outputs.Keys) Assert.That(reverseOutputs[path], Is.EqualTo(outputs[path]));
            }
            finally { CultureInfo.CurrentCulture = culture; CultureInfo.CurrentUICulture = uiCulture; }
        }

        [Test, Category(CategoryName)]
        public void CameraRoomWritesOnlyRun03RootsAndDoesNotMutateExistingScenes()
        {
            var outputs = (IReadOnlyDictionary<string, string>)Property(publication, "OutputContents"); Assert.That(outputs.Count, Is.EqualTo(13)); Assert.That(outputs.Keys.Count(path => path.EndsWith(".csv", StringComparison.Ordinal)), Is.EqualTo(6)); Assert.That(outputs.Keys.Count(path => path.EndsWith(".json", StringComparison.Ordinal)), Is.EqualTo(7));
            Assert.That(outputs.Keys.All(path => path.StartsWith("Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/RUN03/", StringComparison.Ordinal) || path.StartsWith("MapDesign/MCP/GENERATED/RUN03/", StringComparison.Ordinal)), Is.True);
            foreach (var output in outputs)
            {
                var bytes = File.ReadAllBytes(Resolve(output.Key)); Assert.That(bytes.Take(3), Is.Not.EqualTo(new byte[] { 0xef, 0xbb, 0xbf })); var text = new System.Text.UTF8Encoding(false).GetString(bytes);
                Assert.That(text.Contains("\r"), Is.False); Assert.That(text.EndsWith("\n", StringComparison.Ordinal), Is.True); Assert.That(text.EndsWith("\n\n", StringComparison.Ordinal), Is.False); Assert.That(text, Is.EqualTo(output.Value));
            }
            Assert.That((IReadOnlyList<string>)InvokeBuilder("GetDebugTileAssetRelativePaths"), Is.All.StartWith("Assets/_Game/Map/Scenes/MoonPalace/RUN03/")); Assert.That(EditorBuildSettings.scenes.Any(scene => scene.path == Constant("SceneRelativePath")), Is.False);
        }

        [Test, Category(CategoryName)]
        public void CameraRoomWorkDoesNotRunLegacyRegressionPlayModeBuildFullWorldOrRun04()
        {
            Assert.That(validation.Passed, Is.True); Assert.That(Constant("SceneRelativePath"), Does.Contain("/RUN03/").And.Not.Contain("RUN04").And.Not.Contain("VIS03")); Assert.That(EditorApplication.isPlayingOrWillChangePlaymode, Is.False);
            Assert.That(validation.FallbackCarveCount, Is.Zero); Assert.That(validation.SilentRepairCount, Is.Zero); Assert.That(connectors.UsesSectorSeamLogic, Is.False);
        }

        private static object InvokeBuilder(string method, params object[] arguments) => Assembly.Load("MapAuthoring.Editor").GetType(BuilderTypeName, true).GetMethod(method, BindingFlags.Public | BindingFlags.Static).Invoke(null, arguments);
        private static object Property(object source, string name) => source.GetType().GetProperty(name).GetValue(source, null);
        private static string Constant(string name) => (string)Assembly.Load("MapAuthoring.Editor").GetType(BuilderTypeName, true).GetField(name, BindingFlags.Public | BindingFlags.Static).GetValue(null);
        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        private static string Resolve(string relative) => Path.Combine(ProjectRoot, relative.Replace('/', Path.DirectorySeparatorChar));
    }
}
