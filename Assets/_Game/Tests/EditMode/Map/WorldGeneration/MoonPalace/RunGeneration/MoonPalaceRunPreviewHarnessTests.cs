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
    public sealed class MoonPalaceRunPreviewHarnessTests
    {
        private const string CategoryName = "RUN04";
        private const string BuilderTypeName = "StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.RunGeneration.MoonPalaceRunPreviewSceneBuilder";
        private const string Run03ResultRelativePath = "MapDesign/MCP/REPORTS/RUN03_CONNECT_RUN_VARIANTS_TO_CAMERA_ROOM_TRANSITIONS_RESULT.md";
        private const string Run03ResultSha256 = "089c7ef014ce3020f1485e6804360758b7131d9dc06d23367583e9fb4fce47f6";
        private MoonPalaceRunPreviewConfig config;
        private MoonPalaceRunPreviewGrid grid;
        private MoonPalaceRunPreviewValidation validation;
        private object publication;

        [OneTimeSetUp]
        public void PublishRun04Once()
        {
            config = MoonPalaceRunPreviewConfig.CreateDefault();
            grid = MoonPalaceRunPreviewGrid.Create(config);
            validation = MoonPalaceRunPreviewValidator.Validate(grid);
            publication = InvokeBuilder("Publish", ProjectRoot);
        }

        [Test, Category(CategoryName)]
        public void Run03PrerequisiteResultShaAndPassStatusAreRecognized()
        {
            var path = Resolve(Run03ResultRelativePath);
            Assert.That(File.Exists(path), Is.True);
            Assert.That(File.ReadAllText(path), Does.Contain("STATUS: PASS"));
            Assert.That(Hash(path), Is.EqualTo(Run03ResultSha256));
        }

        [Test, Category(CategoryName)]
        public void PreviewConfigUsesRun03PatternGridAndDerivesTileGrid()
        {
            Assert.That(config.PreviewId, Is.EqualTo("MP_RUN_PREVIEW_01"));
            Assert.That(config.SourceCourseId, Is.EqualTo("MP_CAMERA_RUN_01"));
            Assert.That(config.PatternGridWidth + "x" + config.PatternGridHeight, Is.EqualTo("80x24"));
            Assert.That(config.TileWidth + "x" + config.TileHeight, Is.EqualTo("320x96"));
            Assert.That(config.TileWidth, Is.EqualTo(config.PatternGridWidth * 4));
            Assert.That(config.TileHeight, Is.EqualTo(config.PatternGridHeight * 4));
        }

        [Test, Category(CategoryName)]
        public void PreviewGridHasExactly320By96Cells()
        {
            Assert.That(grid.TileWidth, Is.EqualTo(320));
            Assert.That(grid.TileHeight, Is.EqualTo(96));
            Assert.That(grid.CellCount, Is.EqualTo(320 * 96));
            Assert.That(grid.CanonicalDigest, Is.Not.Empty);
        }

        [Test, Category(CategoryName)]
        public void StartAndExitTilesAreInsideAndOpen()
        {
            Assert.That(grid.IsInside(grid.StartTile.x, grid.StartTile.y) && grid.IsOpen(grid.StartTile.x, grid.StartTile.y), Is.True);
            Assert.That(grid.IsInside(grid.ExitTile.x, grid.ExitTile.y) && grid.IsOpen(grid.ExitTile.x, grid.ExitTile.y), Is.True);
            Assert.That(grid.StartTile, Is.Not.EqualTo(grid.ExitTile));
        }

        [Test, Category(CategoryName)]
        public void CanStepAllowsACardinalOpenNeighbor()
        {
            var route = grid.FindRouteToExit();
            Assert.That(route.Count, Is.GreaterThan(1));
            Assert.That(grid.CanStep(route[0].x, route[0].y, route[1].x, route[1].y), Is.True);
        }

        [Test, Category(CategoryName)]
        public void CanStepRejectsBlockedTargetCells()
        {
            var pair = FindOpenBlockedNeighbor();
            Assert.That(pair.HasValue, Is.True);
            var value = pair.Value;
            Assert.That(grid.CanStep(value.open.x, value.open.y, value.blocked.x, value.blocked.y), Is.False);
        }

        [Test, Category(CategoryName)]
        public void CanStepRejectsDiagonalMovement()
        {
            var route = grid.FindRouteToExit();
            var source = route[0];
            Assert.That(grid.CanStep(source.x, source.y, source.x + 1, source.y + 1), Is.False);
        }

        [Test, Category(CategoryName)]
        public void BfsRouteFromStartToExitExists()
        {
            var route = grid.FindRouteToExit();
            Assert.That(route.First(), Is.EqualTo(grid.StartTile));
            Assert.That(route.Last(), Is.EqualTo(grid.ExitTile));
            Assert.That(route.Skip(1).Select((tile, index) => grid.CanStep(route[index].x, route[index].y, tile.x, tile.y)).All(value => value), Is.True);
            Assert.That(validation.RouteExists, Is.True);
        }

        [Test, Category(CategoryName)]
        public void AllTenRoomFramesArePresentAndInsideGrid()
        {
            Assert.That(grid.RoomFrames.Count, Is.EqualTo(10));
            Assert.That(grid.RoomFrames.All(frame => frame.TileMinX >= 0 && frame.TileMinY >= 0 && frame.TileMinX + frame.TileWidth <= grid.TileWidth && frame.TileMinY + frame.TileHeight <= grid.TileHeight), Is.True);
            Assert.That(validation.AllRoomFramesInside, Is.True);
        }

        [Test, Category(CategoryName)]
        public void AllNineConnectorTriggerRecordsArePresent()
        {
            Assert.That(grid.ConnectorTriggers.Count, Is.EqualTo(9));
            Assert.That(grid.ConnectorTriggers.Select(trigger => trigger.ConnectorId).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(9));
            Assert.That(grid.ConnectorTriggers.All(trigger => !string.IsNullOrWhiteSpace(trigger.Kind) && trigger.RequiredSocketBit == MoonPalaceCameraRoomPatternComposer.RouteSocketBit), Is.True);
        }

        [Test, Category(CategoryName)]
        public void ConnectorGateTilesAreOpenAndReciprocal()
        {
            Assert.That(grid.ConnectorTriggers.All(trigger => trigger.IsReciprocal && trigger.IsOpen && grid.IsOpen(trigger.FromGateTile.x, trigger.FromGateTile.y) && grid.IsOpen(trigger.ToGateTile.x, trigger.ToGateTile.y)), Is.True);
            Assert.That(validation.AllConnectorsReciprocal && validation.AllConnectorGateTilesOpen && validation.AllConnectorTransitionsReachable, Is.True);
        }

        [Test, Category(CategoryName)]
        public void ConnectorTransitionLookupChangesToExpectedTargetRoom()
        {
            var transition = FindRouteTransition();
            var cameraObject = new GameObject("RUN04_TestCamera", typeof(Camera), typeof(MoonPalaceRunPreviewCameraController));
            var playerObject = new GameObject("RUN04_TestPlayer", typeof(MoonPalaceRunPreviewController));
            try
            {
                var camera = cameraObject.GetComponent<MoonPalaceRunPreviewCameraController>();
                camera.Configure(cameraObject.GetComponent<Camera>(), false, 0.01f);
                camera.Initialize(grid, grid.GetRoomId(grid.StartTile.x, grid.StartTile.y));
                var controller = playerObject.GetComponent<MoonPalaceRunPreviewController>();
                controller.ConfigureVisuals(null, null, camera, null);
                controller.Initialize(grid);
                foreach (var tile in transition.route.Take(transition.index + 1).Skip(1)) Assert.That(controller.TryStep(tile.x - controller.CurrentTile.x, tile.y - controller.CurrentTile.y), Is.True);
                Assert.That(controller.EnteredConnectorId, Is.EqualTo(transition.trigger.ConnectorId));
                Assert.That(controller.CurrentRoomId, Is.EqualTo(transition.trigger.ToRoomId));
                Assert.That(camera.CurrentRoomId, Is.EqualTo(transition.trigger.ToRoomId));
            }
            finally { UnityEngine.Object.DestroyImmediate(playerObject); UnityEngine.Object.DestroyImmediate(cameraObject); }
        }

        [Test, Category(CategoryName)]
        public void CameraFrameCentersAndSizesDeriveFromRoomBounds()
        {
            foreach (var frame in grid.RoomFrames)
            {
                Assert.That(frame.Center.x, Is.EqualTo(frame.TileMinX + (frame.TileWidth * 0.5f)));
                Assert.That(frame.Center.y, Is.EqualTo(frame.TileMinY + (frame.TileHeight * 0.5f)));
                Assert.That(frame.OrthographicSize, Is.GreaterThanOrEqualTo((frame.TileHeight * 0.5f) + 2f));
                Assert.That(frame.OrthographicSize, Is.GreaterThanOrEqualTo((frame.TileWidth * 0.5f) + 2f));
            }
        }

        [Test, Category(CategoryName)]
        public void SceneContainsRequiredRootAndPreviewComponents()
        {
            var scenePath = Constant("SceneRelativePath");
            Assert.That(File.Exists(Resolve(scenePath)), Is.True);
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                var root = scene.GetRootGameObjects().Single(gameObject => gameObject.name == Constant("SceneRootName"));
                foreach (var child in new[] { "Terrain", "MainRouteOverlay", "BranchSplitOverlay", "RoomFrameOverlay", "ConnectorGateOverlay", "PreviewPlayer", "RouteGhost", "PreviewCamera", "Labels", "Metadata" }) Assert.That(root.transform.Find(child), Is.Not.Null, child);
                Assert.That(root.transform.Find("Terrain").GetComponent<Tilemap>(), Is.Not.Null);
                var previewPlayer = root.transform.Find("PreviewPlayer");
                Assert.That(previewPlayer.GetComponent<MoonPalaceRunPreviewController>(), Is.Not.Null);
                Assert.That(previewPlayer.position.x, Is.EqualTo(grid.StartTile.x + 0.5f));
                Assert.That(previewPlayer.position.y, Is.EqualTo(grid.StartTile.y + 0.5f));
                Assert.That(root.transform.Find("RouteGhost").GetComponent<MoonPalaceRunPreviewRouteGhost>(), Is.Not.Null);
                Assert.That(root.transform.Find("RouteGhost").GetComponent<Tilemap>(), Is.Not.Null);
                Assert.That(root.transform.Find("PreviewCamera").GetComponent<MoonPalaceRunPreviewCameraController>(), Is.Not.Null);
                Assert.That(root.transform.Find("Labels/ControlsLabel").GetComponent<TextMesh>().text, Does.Contain("WASD"));
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test, Category(CategoryName)]
        public void JsonAndCsvArtifactsHaveStableCountsDigestsAndBytes()
        {
            var repeat = InvokeBuilder("BuildSnapshot", ProjectRoot);
            Assert.That(Property(repeat, "SceneManifestDigest"), Is.EqualTo(Property(publication, "SceneManifestDigest")));
            Assert.That(Property(repeat, "DigestManifestDigest"), Is.EqualTo(Property(publication, "DigestManifestDigest")));
            var outputs = (IReadOnlyDictionary<string, string>)Property(publication, "OutputContents");
            Assert.That(outputs.Count, Is.EqualTo(13));
            Assert.That(outputs.Keys.Count(path => path.EndsWith(".csv", StringComparison.Ordinal)), Is.EqualTo(5));
            Assert.That(outputs.Keys.Count(path => path.EndsWith(".json", StringComparison.Ordinal)), Is.EqualTo(8));
            foreach (var output in outputs)
            {
                var bytes = File.ReadAllBytes(Resolve(output.Key));
                Assert.That(bytes.Take(3), Is.Not.EqualTo(new byte[] { 0xef, 0xbb, 0xbf }));
                var text = new System.Text.UTF8Encoding(false).GetString(bytes);
                Assert.That(text, Is.EqualTo(output.Value));
                Assert.That(text.Contains("\r"), Is.False);
                Assert.That(text.EndsWith("\n", StringComparison.Ordinal), Is.True);
                Assert.That(text.EndsWith("\n\n", StringComparison.Ordinal), Is.False);
            }
        }

        [Test, Category(CategoryName)]
        public void NoForbiddenSectorFallbackRepairPlayModeOrBuildSettingsMutationIsRecorded()
        {
            Assert.That(config.AllowSectorPrimaryInput || config.AllowFallbackCarve || config.AllowSilentRepair || config.Allow90DegreeRotation, Is.False);
            Assert.That(validation.UsesSectorPrimaryInput, Is.False);
            Assert.That(validation.FallbackCarveCount, Is.Zero);
            Assert.That(validation.SilentRepairCount, Is.Zero);
            Assert.That(EditorApplication.isPlayingOrWillChangePlaymode, Is.False);
            Assert.That(EditorBuildSettings.scenes.Any(scene => scene.path == Constant("SceneRelativePath")), Is.False);
            Assert.That(Constant("SceneRelativePath"), Does.Contain("/RUN04/").And.Not.Contain("RUN05"));
        }

        private (Vector2Int open, Vector2Int blocked)? FindOpenBlockedNeighbor()
        {
            var directions = new[] { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };
            for (var y = 0; y < grid.TileHeight; y++)
            for (var x = 0; x < grid.TileWidth; x++)
            {
                if (!grid.IsOpen(x, y)) continue;
                foreach (var direction in directions)
                {
                    var next = new Vector2Int(x, y) + direction;
                    if (grid.IsInside(next.x, next.y) && !grid.IsOpen(next.x, next.y)) return (new Vector2Int(x, y), next);
                }
            }
            return null;
        }

        private (IReadOnlyList<Vector2Int> route, int index, MoonPalaceRunPreviewConnectorTrigger trigger) FindRouteTransition()
        {
            var route = grid.FindRouteToExit();
            for (var index = 1; index < route.Count; index++)
            {
                var trigger = grid.ConnectorTriggers.FirstOrDefault(value => value.FromGateTile.Equals(route[index - 1]) && value.ToGateTile.Equals(route[index]));
                if (trigger != null) return (route, index, trigger);
            }
            throw new AssertionException("RUN04 expected the start-to-exit BFS route to cross a forward connector gate.");
        }

        private static object InvokeBuilder(string method, params object[] arguments) => Assembly.Load("MapAuthoring.Editor").GetType(BuilderTypeName, true).GetMethod(method, BindingFlags.Public | BindingFlags.Static).Invoke(null, arguments);
        private static object Property(object source, string name) => source.GetType().GetProperty(name).GetValue(source, null);
        private static string Constant(string name) => (string)Assembly.Load("MapAuthoring.Editor").GetType(BuilderTypeName, true).GetField(name, BindingFlags.Public | BindingFlags.Static).GetValue(null);
        private static string Hash(string path) { using (var sha = System.Security.Cryptography.SHA256.Create()) return string.Concat(sha.ComputeHash(File.ReadAllBytes(path)).Select(value => value.ToString("x2", CultureInfo.InvariantCulture))); }
        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        private static string Resolve(string relative) => Path.Combine(ProjectRoot, relative.Replace('/', Path.DirectorySeparatorChar));
    }
}
