using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StarNight.Map.Tests.PlayMode.WorldGeneration.TerrainClusters
{
    [TestFixture]
    [Category("MAP11_08")]
    public sealed class TerrainClusterGrayboxPlayModeTests
    {
        [UnityTest]
        public IEnumerator MoonCraterQuietTwoChunkFrame() => Run(FrameSpec.Create(
            "TC_CRATER_QUIET_RIM", 2, "Quiet", 5, 16, 8, 5));

        [UnityTest]
        public IEnumerator CassiaRootTraversalThreeChunkFrame() => Run(FrameSpec.Create(
            "TC_ROOT_HOLLOW_POCKET", 3, "Traversal", 5, 17, 12, 13));

        [UnityTest]
        public IEnumerator AbandonedMillDiscoveryFourChunkFrame() => Run(FrameSpec.Create(
            "TC_MILL_BROKEN_PILLAR", 4, "Discovery", 6, 19, 7, 13));

        [UnityTest]
        public IEnumerator MoonDoughRecoveryFiveChunkFrame() => Run(FrameSpec.Create(
            "TC_DOUGH_STICKY_RISE_RECOVERY", 5, "Recovery", 7, 23, 10, 13));

        private static IEnumerator Run(FrameSpec spec)
        {
            var scene = SceneManager.GetActiveScene();
            var sceneHandle = scene.handle;
            var scenePath = scene.path;
            var rootsBefore = scene.GetRootGameObjects().Length;
            var rootName = "MAP11_08_FRAME_" + spec.ClusterId;
            var layoutRepeat = FrameSpec.Create(spec.ClusterId, spec.ActiveChunkCount, spec.PacingRole,
                spec.RouteCount, spec.ProtectedCount, spec.PatternCount, spec.SolidCount);
            Assert.That(layoutRepeat.LayoutKeys, Is.EqualTo(spec.LayoutKeys));

            GameObject root = null;
            try
            {
                root = new GameObject(rootName);
                var cameraObject = Create(root.transform, "CAMERA_ORTHOGRAPHIC", new Vector3(23.5f, 15.5f, -10f));
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 16f;
                camera.aspect = 1.5f;

                for (var column = 0; column <= 4; column++)
                {
                    var line = Create(root.transform, "CH_GRID_V_" + column,
                        new Vector3(column * 12f, 16f, 0f));
                    line.transform.localScale = new Vector3(0.05f, 32f, 1f);
                }
                for (var row = 0; row <= 4; row++)
                {
                    var line = Create(root.transform, "CH_GRID_H_" + row,
                        new Vector3(24f, row * 8f, 0f));
                    line.transform.localScale = new Vector3(48f, 0.05f, 1f);
                }

                foreach (var coordinate in spec.ActiveCoordinates)
                    Create(root.transform, "FOOTPRINT_" + coordinate.X + "_" + coordinate.Y,
                        new Vector3(coordinate.X, coordinate.Y, 0f));
                CreateOverlay(root.transform, "B_ROUTE", spec.RouteCount, spec.ActiveCoordinates, -1f);
                CreateOverlay(root.transform, "AP_PROTECTED", spec.ProtectedCount, spec.ActiveCoordinates, -2f);
                CreateOverlay(root.transform, "P_PATTERN", spec.PatternCount, spec.ActiveCoordinates, -3f);
                CreateOverlay(root.transform, "D_SOLID", spec.SolidCount, spec.ActiveCoordinates, -4f);

                Assert.That(camera.orthographic, Is.True);
                Assert.That(camera.orthographicSize, Is.EqualTo(16f));
                Assert.That(root.transform.Cast<Transform>().Count(value => value.name.StartsWith("CH_GRID_", StringComparison.Ordinal)), Is.EqualTo(10));
                Assert.That(root.transform.Cast<Transform>().Count(value => value.name.StartsWith("FOOTPRINT_", StringComparison.Ordinal)), Is.EqualTo(spec.ActiveCoordinates.Count));
                Assert.That(root.transform.Cast<Transform>().Count(value => value.name.StartsWith("B_ROUTE_", StringComparison.Ordinal)), Is.EqualTo(spec.RouteCount));
                Assert.That(root.transform.Cast<Transform>().Count(value => value.name.StartsWith("AP_PROTECTED_", StringComparison.Ordinal)), Is.EqualTo(spec.ProtectedCount));
                Assert.That(root.transform.Cast<Transform>().Count(value => value.name.StartsWith("P_PATTERN_", StringComparison.Ordinal)), Is.EqualTo(spec.PatternCount));
                Assert.That(root.transform.Cast<Transform>().Count(value => value.name.StartsWith("D_SOLID_", StringComparison.Ordinal)), Is.EqualTo(spec.SolidCount));

                yield return null;

                Assert.That(spec.ActiveCoordinates.All(IsInsideFrame), Is.True,
                    spec.ClusterId + " translation-only footprint exceeds SEC 48x32: " +
                    spec.ActiveCoordinates.Min(value => value.X) + ".." + spec.ActiveCoordinates.Max(value => value.X));
                Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(sceneHandle));
                Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(scenePath));
            }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
            }

            Assert.That(GameObject.Find(rootName), Is.Null);
            Assert.That(SceneManager.GetActiveScene().GetRootGameObjects(), Has.Length.EqualTo(rootsBefore));
        }

        private static GameObject Create(Transform parent, string name, Vector3 position)
        {
            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            item.transform.position = position;
            return item;
        }

        private static void CreateOverlay(
            Transform parent,
            string prefix,
            int count,
            IReadOnlyList<FrameCoord> coordinates,
            float z)
        {
            for (var index = 0; index < count; index++)
            {
                var coordinate = coordinates[index % coordinates.Count];
                Create(parent, prefix + "_" + index,
                    new Vector3(coordinate.X, coordinate.Y, z));
            }
        }

        private static bool IsInsideFrame(FrameCoord value) =>
            value.X >= 0 && value.X < 48 && value.Y >= 0 && value.Y < 32;

        private readonly struct FrameCoord : IEquatable<FrameCoord>
        {
            public FrameCoord(int x, int y)
            {
                X = x;
                Y = y;
            }

            public int X { get; }
            public int Y { get; }
            public bool Equals(FrameCoord other) => X == other.X && Y == other.Y;
            public override bool Equals(object obj) => obj is FrameCoord other && Equals(other);
            public override int GetHashCode() => (X * 397) ^ Y;
            public override string ToString() => X + "," + Y;
        }

        private sealed class FrameSpec
        {
            private readonly ReadOnlyCollection<FrameCoord> activeCoordinates;
            private readonly ReadOnlyCollection<string> layoutKeys;

            private FrameSpec(
                string clusterId,
                int activeChunkCount,
                string pacingRole,
                int routeCount,
                int protectedCount,
                int patternCount,
                int solidCount,
                IEnumerable<FrameCoord> coordinates)
            {
                ClusterId = clusterId;
                ActiveChunkCount = activeChunkCount;
                PacingRole = pacingRole;
                RouteCount = routeCount;
                ProtectedCount = protectedCount;
                PatternCount = patternCount;
                SolidCount = solidCount;
                var copy = coordinates.OrderBy(value => value.Y).ThenBy(value => value.X).ToArray();
                activeCoordinates = new ReadOnlyCollection<FrameCoord>(copy);
                layoutKeys = new ReadOnlyCollection<string>(copy.Select(value =>
                    clusterId + "|" + pacingRole + "|" + value.X + "," + value.Y).ToArray());
            }

            public string ClusterId { get; }
            public int ActiveChunkCount { get; }
            public string PacingRole { get; }
            public int RouteCount { get; }
            public int ProtectedCount { get; }
            public int PatternCount { get; }
            public int SolidCount { get; }
            public IReadOnlyList<FrameCoord> ActiveCoordinates => activeCoordinates;
            public IReadOnlyList<string> LayoutKeys => layoutKeys;

            public static FrameSpec Create(
                string clusterId,
                int activeChunkCount,
                string pacingRole,
                int routeCount,
                int protectedCount,
                int patternCount,
                int solidCount)
            {
                var chunks = string.Equals(clusterId, "TC_DOUGH_STICKY_RISE_RECOVERY", StringComparison.Ordinal)
                    ? new[]
                    {
                        new FrameCoord(0, 0), new FrameCoord(0, 1),
                        new FrameCoord(1, 1), new FrameCoord(1, 2),
                        new FrameCoord(2, 2),
                    }
                    : Enumerable.Range(0, activeChunkCount)
                        .Select(value => new FrameCoord(value, 0)).ToArray();
                Assert.That(chunks, Has.Length.EqualTo(activeChunkCount), clusterId);
                var tileWidth = (chunks.Max(value => value.X) + 1) * 12;
                var tileHeight = (chunks.Max(value => value.Y) + 1) * 8;
                var offsetX = (48 - tileWidth) / 2;
                var offsetY = (32 - tileHeight) / 2;
                var coordinates = chunks
                    .SelectMany(chunk => Enumerable.Range(0, 8)
                        .SelectMany(localY => Enumerable.Range(0, 12)
                            .Select(localX => new FrameCoord(
                                offsetX + chunk.X * 12 + localX,
                                offsetY + chunk.Y * 8 + localY))));
                return new FrameSpec(clusterId, activeChunkCount, pacingRole,
                    routeCount, protectedCount, patternCount, solidCount, coordinates);
            }
        }
    }
}
