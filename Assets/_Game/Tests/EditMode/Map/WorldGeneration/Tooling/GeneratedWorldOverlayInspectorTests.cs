using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Tooling;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Tooling
{
    [Category(CategoryName)]
    public sealed class GeneratedWorldOverlayInspectorTests
    {
        private const string CategoryName = "MAP20_02";
        private const string EditorAssemblyName = "MapAuthoring.Editor";
        private const string WindowTypeName =
            "StarNight.MapAuthoring.WorldGeneration.Tooling.GeneratedWorldOverlayInspectorWindow";
        private const string PublisherTypeName =
            "StarNight.MapAuthoring.WorldGeneration.Tooling.GeneratedWorldOverlaySamplePublisher";

        private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static string OutputRoot => ProjectPath("MapDesign/MCP/GENERATED/MAP20_02");

        [Test]
        public void WorldOverlayCatalogContainsExactTenReadOnlyLayers()
        {
            Assert.That(GeneratedWorldOverlayLayerCatalog.Tokens, Is.EqualTo(new[]
            {
                "Site", "Biome", "Route", "Boundary", "Pacing", "Cluster", "Activity",
                "Special", "Population", "Validation",
            }));
            Assert.That(GeneratedWorldOverlayLayerCatalog.Layers.Count, Is.EqualTo(10));
            Assert.That(GeneratedWorldOverlayLayerCatalog.Layers.All(value =>
                value.IsReadOnly && value.EnabledByDefault &&
                !string.IsNullOrWhiteSpace(value.ShortLabel) &&
                !string.IsNullOrWhiteSpace(value.OwnerCategory)), Is.True);
        }

        [Test]
        public void WorldOverlaySnapshotUsesThirteenByThirteenAndOneHundredSixtyNineSectors()
        {
            var snapshot = WorldSample("2026-09-06T00:00:00Z");

            Assert.That(snapshot.WorldWidthSectors, Is.EqualTo(WorldGenConstants.SectorColumns));
            Assert.That(snapshot.WorldHeightSectors, Is.EqualTo(WorldGenConstants.SectorRows));
            Assert.That(snapshot.SectorCount, Is.EqualTo(WorldGenConstants.SectorCount));
            Assert.That(snapshot.SectorRecords.Count, Is.EqualTo(WorldGenConstants.SectorCount));
            Assert.That(snapshot.SectorRecords.Select(value => value.RowMajorIndex), Is.EqualTo(
                Enumerable.Range(0, WorldGenConstants.SectorCount)));
            Assert.That(snapshot.SectorRecords.All(value =>
                value.LayerFacts.Count == GeneratedWorldOverlayLayerCatalog.Layers.Count), Is.True);
            Assert.That(snapshot.MissingDataRecords.Count, Is.EqualTo(10));
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(snapshot.CanonicalDigest), Is.True);
        }

        [Test]
        public void SectorCanvasInspectorUsesFortyEightByThirtyTwoAndFifteenThirtySixCells()
        {
            var inspection = SectorSample("2026-09-06T00:00:00Z");

            Assert.That(inspection.Width, Is.EqualTo(WorldGenConstants.SectorWidthTiles));
            Assert.That(inspection.Height, Is.EqualTo(WorldGenConstants.SectorHeightTiles));
            Assert.That(inspection.CellCount, Is.EqualTo(WorldGenConstants.TilesPerSector));
            Assert.That(inspection.CellRecords.Count, Is.EqualTo(WorldGenConstants.TilesPerSector));
            Assert.That(inspection.CellRecords.Select(value => value.RowMajorIndex), Is.EqualTo(
                Enumerable.Range(0, WorldGenConstants.TilesPerSector)));
            Assert.That(GeneratedSectorCanvasLayerCatalog.Tokens,
                Is.EqualTo(new[] { "Owner", "Spine", "Envelope", "Density" }));
            Assert.That(inspection.CellRecords.All(value => value.LocalX >= 0 &&
                value.LocalX < WorldGenConstants.SectorWidthTiles && value.LocalY >= 0 &&
                value.LocalY < WorldGenConstants.SectorHeightTiles), Is.True);
        }

        [Test]
        public void SectorCanvasSelectionReportsOwnerSpineEnvelopeDensityAndMissingData()
        {
            var inspection = SectorSample("2026-09-06T00:00:00Z");
            var cell = inspection.SelectedCellRecord;

            Assert.That(new[] { cell.SectorX, cell.SectorY }, Is.EqualTo(new[] { 6, 6 }));
            Assert.That(cell.HasWorldCellCoordinate, Is.True);
            Assert.That(cell.WorldX, Is.EqualTo(cell.SectorX * WorldGenConstants.SectorWidthTiles +
                                                cell.LocalX));
            Assert.That(cell.WorldY, Is.EqualTo(cell.SectorY * WorldGenConstants.SectorHeightTiles +
                                                cell.LocalY));
            Assert.That(cell.OwnerLayer, Is.EqualTo("MissingData"));
            Assert.That(cell.SourceOwner, Is.EqualTo("MissingData"));
            Assert.That(cell.ProvenanceId, Is.EqualTo("MissingData"));
            Assert.That(cell.SpineMovementKind, Is.EqualTo("MissingData"));
            Assert.That(cell.EnvelopeRelation, Is.EqualTo("MissingData"));
            Assert.That(cell.DensityMarker, Is.EqualTo("MissingData"));
            Assert.That(cell.ValidationMarker, Is.EqualTo("MissingData"));
            Assert.That(cell.MissingDataMarker, Is.Not.Empty);
            Assert.That(inspection.MissingDataRecords.Select(value => value.LayerToken),
                Is.EqualTo(new[] { "Owner", "Spine", "Envelope", "Density" }));
        }

        [Test]
        public void OverlayWindowOpensWithoutGenerationRollbackValidationOrCsvJump()
        {
            var type = EditorType(WindowTypeName);
            type.GetMethod("ResetInvocationDiagnostics", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, null);
            var window = type.GetMethod("Open", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, null);

            Assert.That((int)type.GetProperty("OpenInvocationCount").GetValue(null), Is.EqualTo(1));
            Assert.That((int)type.GetProperty("ExternalActionInvocationCount").GetValue(null),
                Is.EqualTo(0));
            Assert.That(type.GetProperty("WorldSnapshot").GetValue(window), Is.Not.Null);
            Assert.That(type.GetProperty("SectorInspection").GetValue(window), Is.Not.Null);
            Assert.That((string)type.GetProperty("RunArtifactPath").GetValue(window),
                Does.EndWith("run_artifact.json"));
            type.GetMethod("Close", BindingFlags.Public | BindingFlags.Instance).Invoke(window, null);
        }

        [Test]
        public void LayerTogglesChangeOnlyViewStateAndNotSnapshotDigest()
        {
            var type = EditorType(WindowTypeName);
            var window = type.GetMethod("Open", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, null);
            var worldDigest = (string)type.GetProperty("WorldSnapshotDigest").GetValue(window);
            var sectorDigest = (string)type.GetProperty("SectorInspectionDigest").GetValue(window);

            Invoke(window, "SetWorldLayerEnabled", "Site", false);
            Invoke(window, "SetCanvasLayerEnabled", "Density", false);
            Invoke(window, "SelectCell", 7, 9);

            Assert.That((bool)Invoke(window, "WorldLayerEnabled", "Site"), Is.False);
            Assert.That((bool)Invoke(window, "CanvasLayerEnabled", "Density"), Is.False);
            Assert.That((string)type.GetProperty("WorldSnapshotDigest").GetValue(window),
                Is.EqualTo(worldDigest));
            Assert.That((string)type.GetProperty("SectorInspectionDigest").GetValue(window),
                Is.EqualTo(sectorDigest));
            Assert.That((int)type.GetProperty("ViewStateMutationCount").GetValue(window),
                Is.EqualTo(3));
            Assert.That((int)type.GetProperty("ExternalActionInvocationCount").GetValue(null),
                Is.EqualTo(0));
            type.GetMethod("Close", BindingFlags.Public | BindingFlags.Instance).Invoke(window, null);
        }

        [Test]
        public void OverlaySnapshotsSerializeDeterministicallyAcrossRepeatCultureAndInputOrder()
        {
            var world = WorldSample("2026-09-06T00:00:00Z");
            var sector = SectorSample("2026-09-06T00:00:00Z");
            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var reorderedWorld = new GeneratedWorldOverlaySnapshot(
                    world.SourceRunArtifactDigest, world.Map2001HandoffDigest,
                    world.EnabledLayerTokens.Reverse(), world.SectorRecords.Reverse(),
                    world.MissingDataRecords.Reverse(), world.ValidationRecords.Reverse(),
                    "2099-12-31T23:59:59Z");
                var reorderedSector = new GeneratedSectorCanvasInspection(
                    sector.SectorX, sector.SectorY, sector.EnabledCanvasLayerTokens.Reverse(),
                    sector.CellRecords.Reverse(), sector.MissingDataRecords.Reverse(),
                    sector.SelectedCellRecord.LocalX, sector.SelectedCellRecord.LocalY,
                    "2099-12-31T23:59:59Z");

                Assert.That(reorderedWorld.CanonicalDigest, Is.EqualTo(world.CanonicalDigest));
                Assert.That(reorderedSector.CanonicalDigest, Is.EqualTo(sector.CanonicalDigest));
                Assert.That(world.Serialize(), Is.EqualTo(world.Serialize()));
                Assert.That(sector.Serialize(), Is.EqualTo(sector.Serialize()));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }

            var publication = InvokeStatic(EditorType(PublisherTypeName), "PublishSamples", ProjectRoot);
            var publicationType = publication.GetType();
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256((string)publicationType
                .GetProperty("ManifestDigest").GetValue(publication)), Is.True);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256((string)publicationType
                .GetProperty("Map2003HandoffDigest").GetValue(publication)), Is.True);
            Assert.That(File.Exists(Path.Combine(OutputRoot, "world_overlay_snapshot.json")), Is.True);
            Assert.That(File.Exists(Path.Combine(OutputRoot,
                "sector_canvas_inspection_sample.json")), Is.True);
            Assert.That(File.Exists(Path.Combine(OutputRoot, "overlay_digest_manifest.json")), Is.True);
            Assert.That(File.ReadAllText(Path.Combine(OutputRoot, "world_overlay_snapshot.json")),
                Does.Contain("\"sector_records\"").And.Contain("\"missing_data_records\"")
                    .And.Contain("\"created_utc_excluded_from_canonical_digest\": true"));
            Assert.That(File.ReadAllText(Path.Combine(OutputRoot,
                "sector_canvas_inspection_sample.json")), Does.Contain("\"cell_records\"")
                .And.Contain("\"selected_cell_record\"").And.Contain("\"source_owner\""));
        }

        [Test]
        public void OverlayInspectorDoesNotSelectLegacyRegressionPriorCategoriesPlayModeOrFullRegression()
        {
            var windowSource = File.ReadAllText(ProjectPath(
                "Assets/_Game/Editor/MapAuthoring/WorldGeneration/Tooling/GeneratedWorldOverlayInspectorWindow.cs"));
            var publisherSource = File.ReadAllText(ProjectPath(
                "Assets/_Game/Editor/MapAuthoring/WorldGeneration/Tooling/GeneratedWorldOverlaySamplePublisher.cs"));
            var combined = windowSource + "\n" + publisherSource;

            Assert.That(combined, Does.Not.Contain("19347"));
            Assert.That(combined, Does.Not.Contain("PlayMode"));
            Assert.That(combined, Does.Not.Contain("TestRunnerApi"));
            Assert.That(combined, Does.Not.Contain("run_tests"));
            Assert.That(combined, Does.Not.Contain("RunMap19_09ScaleAudit"));
            Assert.That(combined, Does.Not.Contain("GeneratedTerrainRunCoordinator"));
            Assert.That(windowSource, Does.Not.Contain("File.Write"));
            Assert.That(windowSource, Does.Not.Contain("Tilemap"));
            Assert.That(windowSource, Does.Not.Contain("Instantiate("));
            Assert.That(windowSource, Does.Not.Contain("Destroy("));
        }

        private static GeneratedWorldOverlaySnapshot WorldSample(string createdUtc) =>
            GeneratedWorldOverlaySnapshot.CreateMissingDataSample(
                GeneratedTerrainRunPreconditions.Map19ExitDigest,
                GeneratedWorldOverlayPreconditions.Map2001HandoffDigest,
                createdUtc);

        private static GeneratedSectorCanvasInspection SectorSample(string createdUtc) =>
            GeneratedSectorCanvasInspection.CreateMissingDataSample(6, 6, createdUtc);

        private static object Invoke(object target, string method, params object[] arguments)
        {
            try
            {
                return target.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.Instance)
                    .Invoke(target, arguments);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        private static object InvokeStatic(Type type, string method, params object[] arguments)
        {
            try
            {
                return type.GetMethod(method, BindingFlags.Public | BindingFlags.Static)
                    .Invoke(null, arguments);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        private static Type EditorType(string fullName)
        {
            var type = Type.GetType(fullName + ", " + EditorAssemblyName, false);
            Assert.That(type, Is.Not.Null, fullName + " must compile into " +
                EditorAssemblyName + ".");
            return type;
        }

        private static string ProjectPath(string relative) => Path.GetFullPath(Path.Combine(
            ProjectRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
    }
}
