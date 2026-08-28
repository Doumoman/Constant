using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.TerrainClusters;
using StarNight.MapAuthoring.WorldGeneration.TerrainClusters;
using StarNight.MapAuthoring.WorldGeneration.Import;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests.EditMode.WorldGeneration.TerrainClusters
{
    [TestFixture]
    [Category("MAP11_08")]
    public sealed class TerrainClusterPreviewTests
    {
        private static readonly string[] Representatives =
        {
            "TC_CRATER_QUIET_RIM",
            "TC_ROOT_HOLLOW_POCKET",
            "TC_MILL_BROKEN_PILLAR",
            "TC_DOUGH_STICKY_RISE_RECOVERY",
        };

        private static readonly string[][] RepresentativePatterns =
        {
            new[] { "MP_CRATER_BOWL", "MP_CRATER_ROCK_SHELF" },
            new[] { "MP_ROOT_ARCH", "MP_ROOT_HOLLOW_POCKET" },
            new[] { "MP_MILL_BROKEN_PILLAR", "MP_MILL_ORTHOGONAL_CARVE" },
            new[] { "MP_DOUGH_BOUNCE_CUP", "MP_DOUGH_STICKY_SHELF" },
        };

        [Test]
        public void PhysicalSchemaAndCatalogAreExactlyThirteenTablesEightyNineColumnsAndSixteenClusters()
        {
            var tables = V2AuthoringSchemaRegistry.DescribeDefaultTables()
                .Where(value => value.Owner == V2AuthoringOwner.TerrainCluster).ToArray();
            Assert.That(tables, Has.Length.EqualTo(13));
            Assert.That(tables.Sum(value => value.Columns.Count), Is.EqualTo(89));
            Assert.That(TerrainClusterCsvImporterV2.ProjectRelativePaths, Has.Count.EqualTo(13));

            var import = new TerrainClusterPreviewModel().LoadCatalog();
            Assert.That(import.Success, Is.True, string.Join("\n", import.Errors));
            Assert.That(import.Catalog.Entries, Has.Count.EqualTo(16));
            Assert.That(import.StableDigest, Is.EqualTo(TerrainClusterPreviewModel.ApprovedCatalogDigest));
        }

        [Test]
        public void AllSixteenClustersAndBothVariantsPublishCompletePatternFreeEvidence()
        {
            var model = new TerrainClusterPreviewModel();
            var import = model.LoadCatalog();
            Assert.That(import.Success, Is.True, string.Join("\n", import.Errors));
            var built = 0;
            foreach (var entry in import.Catalog.Entries)
            foreach (var variant in entry.Contract.Traversal.Variants)
            {
                var result = model.Build(new TerrainClusterPreviewRequest(
                    entry.Id.Value, variant.Id.Value, TerrainClusterPreviewMode.PatternFree));
                Assert.That(result.Success, Is.True, Errors(result));
                var snapshot = result.Snapshot;
                Assert.That(snapshot.ClusterId, Is.EqualTo(entry.Id.Value));
                Assert.That(snapshot.VariantId, Is.EqualTo(variant.Id.Value));
                Assert.That(snapshot.Pattern.IsPatternFree, Is.True);
                Assert.That(snapshot.Pattern.TargetCount, Is.Zero);
                Assert.That(snapshot.Pattern.ChangedCount, Is.Zero);
                Assert.That(snapshot.Pattern.ProtectedWriteCount, Is.Zero);
                Assert.That(snapshot.Pattern.ProtectedValueChangeCount, Is.Zero);
                Assert.That(snapshot.Cells.Count(value => value.Active),
                    Is.EqualTo(snapshot.Density.ActiveCount));
                Assert.That(snapshot.Density.SolidCount + snapshot.Density.AirCount,
                    Is.EqualTo(snapshot.Density.ActiveCount));
                Assert.That(snapshot.Density.Chunks.Sum(value => value.ActiveCount),
                    Is.EqualTo(snapshot.Density.ActiveCount));
                Assert.That(snapshot.Anchors.Any(value => value.Token == "EN Entry"), Is.True);
                Assert.That(snapshot.Anchors.Any(value => value.Token == "EX Exit"), Is.True);
                Assert.That(snapshot.Segments.Any(value => value.Token == "SP Spine"), Is.True);
                Assert.That(snapshot.EnvelopeCoordinates, Is.Not.Empty);
                Assert.That(snapshot.AbsoluteProtectedCoordinates, Is.Not.Empty);
                Assert.That(snapshot.BaselineCoordinates, Is.Not.Empty);
                Assert.That(snapshot.HighRouteCoordinates, Is.Not.Empty);
                Assert.That(snapshot.RecoveryCoordinates, Is.Not.Empty);
                Assert.That(snapshot.RouteEvidence.Any(value => value.StartsWith("BASE|", StringComparison.Ordinal)), Is.True);
                Assert.That(snapshot.RouteEvidence.Any(value => value.StartsWith("HIGH|", StringComparison.Ordinal)), Is.True);
                Assert.That(snapshot.RouteEvidence.Any(value => value.StartsWith("RECOVERY|", StringComparison.Ordinal)), Is.True);
                Assert.That(snapshot.Cells.Where(value => value.Active).All(value =>
                    snapshot.SectorFrame.Contains(value.FrameCoordinate)), Is.True);
                Assert.That(snapshot.Cells.All(value => value.FrameCoordinate ==
                    snapshot.SectorFrame.Translate(value.LocalCoordinate)), Is.True);
                Assert.That(snapshot.SectorFrame.ActiveCoordinates.All(snapshot.SectorFrame.Contains), Is.True);
                Assert.That(snapshot.StableDigest, Does.Match("^[0-9a-f]{64}$"));
                if (entry.PacingRole == StarNight.Map.WorldGeneration.Pipeline.PacingRole.Quiet)
                {
                    Assert.That(snapshot.QuietEvidence.Any(value => value.StartsWith("QUIET_MATCH|", StringComparison.Ordinal)), Is.True);
                    Assert.That(snapshot.QuietEvidence, Does.Contain("RNG_DRAWS|0"));
                }
                else Assert.That(snapshot.QuietEvidence, Is.Empty);
                built++;
            }
            Assert.That(built, Is.EqualTo(32));
        }

        [Test]
        public void FourRepresentativePatternPairsUseActualRendererAndPreserveStructuralEvidence()
        {
            var model = new TerrainClusterPreviewModel();
            for (var index = 0; index < Representatives.Length; index++)
            {
                var clusterId = Representatives[index];
                var free = Build(model, clusterId, TerrainClusterPreviewMode.PatternFree);
                var first = Build(model, clusterId, TerrainClusterPreviewMode.PatternA);
                var second = Build(model, clusterId, TerrainClusterPreviewMode.PatternB);
                var snapshots = new[] { first, second };
                Assert.That(first.Pattern.PatternId, Is.EqualTo(RepresentativePatterns[index][0]));
                Assert.That(second.Pattern.PatternId, Is.EqualTo(RepresentativePatterns[index][1]));
                foreach (var snapshot in snapshots)
                {
                    Assert.That(snapshot.Pattern.IsPatternFree, Is.False);
                    Assert.That(snapshot.Pattern.PlacementId, Does.StartWith("TCP_"));
                    Assert.That(snapshot.Pattern.ApplicationPlanDigest, Does.Match("^[0-9a-f]{64}$"));
                    Assert.That(snapshot.Pattern.RenderDigest, Does.Match("^[0-9a-f]{64}$"));
                    Assert.That(snapshot.Pattern.TargetCount, Is.EqualTo(16));
                    Assert.That(snapshot.Pattern.ChangedCount, Is.GreaterThan(0));
                    Assert.That(snapshot.Pattern.ProtectedWriteCount, Is.Zero);
                    Assert.That(snapshot.Pattern.ProtectedValueChangeCount, Is.Zero);
                    Assert.That(snapshot.CanvasDigest, Is.EqualTo(free.CanvasDigest));
                    Assert.That(snapshot.RoleSocketDigest, Is.EqualTo(free.RoleSocketDigest));
                    Assert.That(snapshot.TraversalDigest, Is.EqualTo(free.TraversalDigest));
                    Assert.That(snapshot.RouteWitnessDigest, Is.EqualTo(free.RouteWitnessDigest));
                    Assert.That(snapshot.BaselineCoordinates, Is.EqualTo(free.BaselineCoordinates));
                    Assert.That(snapshot.HighRouteCoordinates, Is.EqualTo(free.HighRouteCoordinates));
                    Assert.That(snapshot.RecoveryCoordinates, Is.EqualTo(free.RecoveryCoordinates));
                    Assert.That(snapshot.AbsoluteProtectedCoordinates, Is.EqualTo(free.AbsoluteProtectedCoordinates));
                    Assert.That(snapshot.Pattern.ChangedCoordinates.All(value =>
                        !snapshot.AbsoluteProtectedCoordinates.Contains(value)), Is.True);
                    Assert.That(snapshot.Density.Chunks.Sum(value => value.ActiveCount),
                        Is.EqualTo(snapshot.Density.ActiveCount));
                }
                var repeatedA = Build(model, clusterId, TerrainClusterPreviewMode.PatternA);
                var repeatedB = Build(model, clusterId, TerrainClusterPreviewMode.PatternB);
                Assert.That(repeatedA.Pattern.Origin, Is.EqualTo(first.Pattern.Origin));
                Assert.That(repeatedB.Pattern.Origin, Is.EqualTo(second.Pattern.Origin));
                Assert.That(repeatedA.StableDigest, Is.EqualTo(first.StableDigest));
                Assert.That(repeatedB.StableDigest, Is.EqualTo(second.StableDigest));
            }
        }

        [Test]
        public void CultureRepeatAndCallerMutationCannotChangeCanonicalSnapshots()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var model = new TerrainClusterPreviewModel();
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var turkish = Build(model, Representatives[0], TerrainClusterPreviewMode.PatternA);
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ko-KR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ko-KR");
                var korean = Build(model, Representatives[0], TerrainClusterPreviewMode.PatternA);
                Assert.That(korean.StableDigest, Is.EqualTo(turkish.StableDigest));
                Assert.That(korean.Density.SolidRatio, Is.EqualTo(turkish.Density.SolidRatio));
                Assert.Throws<NotSupportedException>(() =>
                    ((IList<TerrainClusterPreviewCell>)korean.Cells).Add(null));
                Assert.Throws<NotSupportedException>(() =>
                    ((IList<LocalTileCoord>)korean.Pattern.ChangedCoordinates).Add(default));
                Assert.Throws<NotSupportedException>(() =>
                    ((IList<string>)korean.Cells.First().Tokens).Add("MUTATE"));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Test]
        public void WindowBindsExactSelectorsDefaultAndComparePanelsWithoutSceneOrGeneratedWrites()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var dirtyBefore = scene.isDirty;
            var rootsBefore = scene.GetRootGameObjects().Length;
            var generatedBefore = GeneratedCsvFiles();
            var method = typeof(TerrainClusterPreviewWindow).GetMethod(
                "Open", BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            Assert.That(method.GetCustomAttributes(typeof(MenuItem), false), Has.Length.EqualTo(1));
            Assert.That(TerrainClusterPreviewWindow.MenuPath,
                Is.EqualTo("Tools/MapDesign/TerrainCluster Preview"));
            Assert.That(TerrainClusterPreviewWindow.WindowTitle, Is.EqualTo("TerrainCluster Preview"));

            TerrainClusterPreviewWindow window = null;
            try
            {
                window = TerrainClusterPreviewWindow.Open();
                Assert.That(window.Reload(), Is.True, window.LastError);
                Assert.That(window.titleContent.text, Is.EqualTo("TerrainCluster Preview"));
                Assert.That(window.AllClusterIds, Has.Count.EqualTo(16));
                Assert.That(window.VariantIds, Has.Count.EqualTo(2));
                Assert.That(window.PanelCount, Is.EqualTo(5));
                Assert.That(window.CurrentSnapshot.Pattern.IsPatternFree, Is.True);
                Assert.That(window.TrySelectBiome("MoonCrater"), Is.True, window.LastError);
                Assert.That(window.ClusterIds, Has.Count.EqualTo(4));
                Assert.That(window.TrySelectCluster(Representatives[0]), Is.True, window.LastError);
                Assert.That(window.TrySelectViewMode(TerrainClusterPreviewViewMode.Compare), Is.True, window.LastError);
                Assert.That(window.CompareSnapshots, Has.Count.EqualTo(3));
                Assert.That(window.CompareSnapshots.Select(value => value.Pattern.IsPatternFree),
                    Is.EqualTo(new[] { true, false, false }));
                window.Repaint();
            }
            finally
            {
                if (window != null) window.Close();
            }

            Assert.That(EditorSceneManager.GetActiveScene().isDirty, Is.EqualTo(dirtyBefore));
            Assert.That(EditorSceneManager.GetActiveScene().GetRootGameObjects(), Has.Length.EqualTo(rootsBefore));
            Assert.That(GeneratedCsvFiles(), Is.EqualTo(generatedBefore));
        }

        private static TerrainClusterPreviewSnapshot Build(
            TerrainClusterPreviewModel model,
            string clusterId,
            TerrainClusterPreviewMode mode)
        {
            var result = model.Build(new TerrainClusterPreviewRequest(clusterId, string.Empty, mode));
            Assert.That(result.Success, Is.True, Errors(result));
            return result.Snapshot;
        }

        private static string Errors(TerrainClusterPreviewBuildResult result) =>
            string.Join("\n", result.Errors.Select(value => value.ToString()));

        private static string[] GeneratedCsvFiles()
        {
            var root = FullPath("Assets/_Game/Map/Data/WorldGeneration/Generated");
            return Directory.Exists(root)
                ? Directory.GetFiles(root, "*.csv", SearchOption.AllDirectories)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray()
                : Array.Empty<string>();
        }

        private static string FullPath(string projectRelativePath) => Path.GetFullPath(Path.Combine(
            Application.dataPath, "..", projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
    }
}
