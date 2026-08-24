using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Microchunks;
using StarNight.MapAuthoring.Microchunks;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace StarNight.Map.Tests.WorldGeneration
{
    [Category("MAP07_08")]
    public sealed class MicrochunkAuthoringGridTests
    {
        private static readonly MicrochunkTileLayer[] ExpectedLayers =
        {
            MicrochunkTileLayer.GroundSolid,
            MicrochunkTileLayer.OneWay,
            MicrochunkTileLayer.Breakable,
            MicrochunkTileLayer.Hazard,
            MicrochunkTileLayer.Liquid,
            MicrochunkTileLayer.DecorationBack,
            MicrochunkTileLayer.DecorationFront,
            MicrochunkTileLayer.Marker
        };

        public static IEnumerable<int> Cases => Enumerable.Range(0, 320);

        [TestCaseSource(nameof(Cases))]
        public void AuthoringGridContractIsDeterministicAndEditorOnly(int caseId)
        {
            var viewModel = new MicrochunkAuthoringGridViewModel();
            var coordinateIndex = caseId % MicrochunkConstants.CellCount;
            var x = coordinateIndex % MicrochunkConstants.WidthTiles;
            var y = coordinateIndex / MicrochunkConstants.WidthTiles;
            var layer = ExpectedLayers[caseId % ExpectedLayers.Length];

            switch (caseId % 20)
            {
                case 0:
                    AssertGridShapeAndDefaults(viewModel.State);
                    break;
                case 1:
                    Assert.That(MicrochunkAuthoringGridLayer.OrderedLayers, Is.EqualTo(ExpectedLayers));
                    Assert.That(MicrochunkAuthoringGridLayer.Count, Is.EqualTo(8));
                    Assert.That(MicrochunkAuthoringGridLayer.At((int)layer), Is.EqualTo(layer));
                    Assert.That(MicrochunkAuthoringGridLayer.IndexOf(layer), Is.EqualTo((int)layer));
                    break;
                case 2:
                    AssertCoordinateAndLayerBoundsAreRejected(viewModel.State);
                    break;
                case 3:
                    viewModel.Palette.Select(layer, "CODE_" + caseId);
                    Assert.That(viewModel.Palette.SelectedLayer, Is.EqualTo(layer));
                    Assert.That(viewModel.Palette.SelectedTileCode, Is.EqualTo("CODE_" + caseId));
                    Assert.That(viewModel.Palette.AvailableLayers, Is.EqualTo(ExpectedLayers));
                    break;
                case 4:
                    PaintOneLayerAndAssertOthersRemainEmpty(viewModel, x, y, layer, "PAINT_" + caseId);
                    break;
                case 5:
                    viewModel.Palette.Select(layer, "PAINT");
                    viewModel.PaintCell(x, y);
                    viewModel.EraseCell(x, y);
                    Assert.That(viewModel.State.GetTileCode(x, y, layer), Is.EqualTo("NONE"));
                    AssertOtherLayersAreEmpty(viewModel.State.GetCell(x, y), layer);
                    break;
                case 6:
                    viewModel.Palette.Select(layer, "RECT_" + caseId);
                    var applied = viewModel.PaintRectangle(2, 1, 4, 3);
                    Assert.That(applied.Select(value => value.RowMajorIndex),
                        Is.EqualTo(new[] { 14, 15, 16, 26, 27, 28, 38, 39, 40 }));
                    Assert.That(applied.All(value =>
                        viewModel.State.GetCell(value).GetTileCode(layer) == "RECT_" + caseId), Is.True);
                    break;
                case 7:
                    viewModel.State.PaintCell(x, y, layer, "CLEAR_ME");
                    viewModel.State.PaintCell(x, y, NextLayer(layer), "KEEP_ME");
                    viewModel.Palette.SelectLayer(layer);
                    viewModel.ClearSelectedLayer();
                    Assert.That(viewModel.State.CellCount, Is.EqualTo(96));
                    Assert.That(viewModel.State.GetTileCode(x, y, layer), Is.EqualTo("NONE"));
                    Assert.That(viewModel.State.GetTileCode(x, y, NextLayer(layer)), Is.EqualTo("KEEP_ME"));
                    break;
                case 8:
                    foreach (var currentLayer in ExpectedLayers)
                    {
                        viewModel.State.PaintCell(x, y, currentLayer, "VALUE_" + (int)currentLayer);
                    }
                    viewModel.ClearAllLayers();
                    Assert.That(viewModel.State.CellCount, Is.EqualTo(96));
                    Assert.That(viewModel.State.Cells.SelectMany(cell => cell.TileCodes), Is.All.EqualTo("NONE"));
                    break;
                case 9:
                    var projected = viewModel.ProjectTileCells();
                    Assert.That(projected, Has.Count.EqualTo(96));
                    Assert.That(projected.Select(cell => cell.Coordinate.RowMajorIndex),
                        Is.EqualTo(Enumerable.Range(0, 96)));
                    Assert.That(projected.Select(CellSignature),
                        Is.All.EqualTo("NONE|NONE|NONE|NONE|NONE|NONE|NONE|NONE"));
                    break;
                case 10:
                    var records = viewModel.ProjectCoverageRecords();
                    var coverage = viewModel.ValidateCoverage();
                    Assert.That(records, Has.Count.EqualTo(96));
                    Assert.That(records.Select(record => record.SourceOrdinal), Is.EqualTo(Enumerable.Range(0, 96)));
                    Assert.That(coverage.Success, Is.True);
                    Assert.That(coverage.InRangeUniqueCoordinateCount, Is.EqualTo(96));
                    break;
                case 11:
                    viewModel.State.PaintCell(x, y, MicrochunkTileLayer.GroundSolid, "GROUND");
                    viewModel.State.PaintCell(x, y, MicrochunkTileLayer.Liquid, "LIQUID");
                    var layerResult = viewModel.ValidateTileLayers();
                    Assert.That(layerResult.TotalEvaluatedCells, Is.EqualTo(96));
                    Assert.That(layerResult.Success, Is.False);
                    Assert.That(layerResult.Violations.Any(value => value.Coordinate == new MicrochunkLocalCoord(x, y)), Is.True);
                    break;
                case 12:
                    viewModel.State.PaintCell(x, y, layer, "STABLE");
                    var before = StateSignature(viewModel.State);
                    var summary = viewModel.Validate();
                    Assert.That(summary.CoverageResult.Success, Is.True);
                    Assert.That(StateSignature(viewModel.State), Is.EqualTo(before));
                    Assert.That(viewModel.State.GetTileCode(x, y, layer), Is.EqualTo("STABLE"));
                    break;
                case 13:
                    AssertSnapshotsAreReadOnlyAndDetached(viewModel, x, y, layer);
                    break;
                case 14:
                    Assert.That(() => viewModel.Palette.SelectTileCode(null), Throws.TypeOf<ArgumentException>());
                    Assert.That(() => viewModel.Palette.SelectTileCode(" "), Throws.TypeOf<ArgumentException>());
                    Assert.That(() => viewModel.Palette.SelectTileCode(" PAD "), Throws.TypeOf<ArgumentException>());
                    Assert.That(() => viewModel.State.PaintCell(x, y, layer, string.Empty), Throws.TypeOf<ArgumentException>());
                    break;
                case 15:
                    Assert.That(typeof(EditorWindow).IsAssignableFrom(typeof(MicrochunkAuthoringGridWindow)), Is.True);
                    Assert.That(typeof(MicrochunkAuthoringGridWindow).GetMethod("Open", BindingFlags.Public | BindingFlags.Static), Is.Not.Null);
                    Assert.That(typeof(MicrochunkAuthoringGridWindow).Assembly.GetName().Name, Is.EqualTo("MapAuthoring.Editor"));
                    Assert.That(typeof(MicrochunkAuthoringGridState).Assembly, Is.EqualTo(typeof(MicrochunkAuthoringGridWindow).Assembly));
                    break;
                case 16:
                    AssertFutureProductionSymbolsRemainAbsent();
                    break;
                case 17:
                    AssertCommandsDoNotDirtyScene(viewModel, x, y, layer);
                    break;
                case 18:
                    Assert.That(viewModel.Palette.Swatches.First(), Is.EqualTo("NONE"));
                    Assert.That(viewModel.Palette.Swatches.Count(value => value == "NONE"), Is.EqualTo(1));
                    Assert.That(viewModel.Palette.Swatches.Distinct(StringComparer.Ordinal).Count(),
                        Is.EqualTo(viewModel.Palette.Swatches.Count));
                    viewModel.Palette.SelectErase();
                    Assert.That(viewModel.Palette.IsErasing, Is.True);
                    break;
                default:
                    viewModel.State.PaintCell(x, y, layer, "PROJECTED_" + caseId);
                    var definition = viewModel.ProjectDefinition();
                    Assert.That(definition.TileDataComplete, Is.True);
                    Assert.That(definition.TileCells, Has.Count.EqualTo(96));
                    Assert.That(definition.TileCells[coordinateIndex].Coordinate, Is.EqualTo(new MicrochunkLocalCoord(x, y)));
                    Assert.That(CodeFor(definition.TileCells[coordinateIndex], layer), Is.EqualTo("PROJECTED_" + caseId));
                    Assert.That(definition.Sockets, Is.Empty);
                    Assert.That(definition.ObjectSlots, Is.Empty);
                    break;
            }
        }

        private static void AssertGridShapeAndDefaults(MicrochunkAuthoringGridState state)
        {
            Assert.That(state.Width, Is.EqualTo(12));
            Assert.That(state.Height, Is.EqualTo(8));
            Assert.That(state.CellCount, Is.EqualTo(96));
            Assert.That(state.Cells.Select(cell => cell.Coordinate.RowMajorIndex),
                Is.EqualTo(Enumerable.Range(0, 96)));
            Assert.That(state.Cells.SelectMany(cell => cell.TileCodes), Is.All.EqualTo("NONE"));
        }

        private static void AssertCoordinateAndLayerBoundsAreRejected(MicrochunkAuthoringGridState state)
        {
            Assert.That(() => state.GetCell(-1, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => state.GetCell(12, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => state.GetCell(0, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => state.GetCell(0, 8), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => state.PaintCell(0, 0, (MicrochunkTileLayer)99, "VALUE"),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => MicrochunkAuthoringGridLayer.At(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => MicrochunkAuthoringGridLayer.At(8), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        private static void PaintOneLayerAndAssertOthersRemainEmpty(
            MicrochunkAuthoringGridViewModel viewModel,
            int x,
            int y,
            MicrochunkTileLayer layer,
            string code)
        {
            viewModel.Palette.Select(layer, code);
            viewModel.PaintCell(x, y);
            Assert.That(viewModel.State.GetTileCode(x, y, layer), Is.EqualTo(code));
            AssertOtherLayersAreEmpty(viewModel.State.GetCell(x, y), layer);
            Assert.That(viewModel.State.Cells.Count(cell => cell.TileCodes.Any(value => value != "NONE")), Is.EqualTo(1));
        }

        private static void AssertOtherLayersAreEmpty(
            MicrochunkAuthoringGridCell cell,
            MicrochunkTileLayer except)
        {
            foreach (var layer in ExpectedLayers.Where(value => value != except))
            {
                Assert.That(cell.GetTileCode(layer), Is.EqualTo("NONE"));
            }
        }

        private static void AssertSnapshotsAreReadOnlyAndDetached(
            MicrochunkAuthoringGridViewModel viewModel,
            int x,
            int y,
            MicrochunkTileLayer layer)
        {
            viewModel.State.PaintCell(x, y, layer, "SOURCE");
            var cells = viewModel.ProjectTileCells();
            var records = viewModel.ProjectCoverageRecords();
            Assert.That(() => ((IList<MicrochunkTileCell>)cells).Add(cells[0]), Throws.TypeOf<NotSupportedException>());
            Assert.That(() => ((IList<Microchunk96CellRecord>)records).RemoveAt(0), Throws.TypeOf<NotSupportedException>());
            viewModel.State.PaintCell(x, y, layer, "CHANGED");
            Assert.That(CodeFor(cells[new MicrochunkLocalCoord(x, y).RowMajorIndex], layer), Is.EqualTo("SOURCE"));
            Assert.That(CodeFor(records[new MicrochunkLocalCoord(x, y).RowMajorIndex].TileCell, layer), Is.EqualTo("SOURCE"));
        }

        private static void AssertFutureProductionSymbolsRemainAbsent()
        {
            var names = typeof(MicrochunkAuthoringGridState).Assembly.GetTypes().Select(type => type.Name).ToArray();
            foreach (var forbidden in new[]
                     {
                         "MicrochunkSocketEditor", "MicrochunkSlotEditor",
                         "MicrochunkCsvExporter", "MicrochunkPreviewReport",
                         "MicrochunkReachabilityHeatmap", "MicrochunkStarterCatalogRoundTrip",
                         "BoundaryChunkResolver", "SectorRecipeResolver", "GeneratedSectorMicrochunkWriter",
                         "PopulationSlotIndex", "StableSpawnId", "WorldTraversalValidator"
                     })
            {
                Assert.That(names, Does.Not.Contain(forbidden));
            }

            Assert.That(typeof(MicrochunkAuthoringGridState).Assembly, Is.Not.EqualTo(typeof(MicrochunkDefinition).Assembly));
            Assert.That(typeof(MicrochunkDefinition).Assembly.GetTypes().Any(type => type.Name == "MicrochunkAuthoringGrid"), Is.False);
        }

        private static void AssertCommandsDoNotDirtyScene(
            MicrochunkAuthoringGridViewModel viewModel,
            int x,
            int y,
            MicrochunkTileLayer layer)
        {
            var scene = EditorSceneManager.GetActiveScene();
            var dirtyBefore = scene.IsValid() && scene.isDirty;
            viewModel.Palette.Select(layer, "NO_IO");
            viewModel.PaintCell(x, y);
            viewModel.EraseCell(x, y);
            viewModel.PaintRectangle(0, 0, 1, 1);
            viewModel.ClearSelectedLayer();
            viewModel.ClearAllLayers();
            viewModel.Validate();
            var dirtyAfter = scene.IsValid() && scene.isDirty;
            Assert.That(dirtyAfter, Is.EqualTo(dirtyBefore));
            Assert.That(typeof(MicrochunkAuthoringGridViewModel).GetMethods()
                .Select(method => method.Name),
                Has.None.Contains("Import").And.None.Contains("Export").And.None.Contains("Save"));
        }

        private static MicrochunkTileLayer NextLayer(MicrochunkTileLayer layer)
        {
            return ExpectedLayers[((int)layer + 1) % ExpectedLayers.Length];
        }

        private static string StateSignature(MicrochunkAuthoringGridState state)
        {
            return string.Join("\n", state.Cells.Select(cell =>
                cell.Coordinate.RowMajorIndex + ":" + string.Join("|", cell.TileCodes)));
        }

        private static string CellSignature(MicrochunkTileCell cell)
        {
            return string.Join("|", ExpectedLayers.Select(layer => CodeFor(cell, layer)));
        }

        private static string CodeFor(MicrochunkTileCell cell, MicrochunkTileLayer layer)
        {
            switch (layer)
            {
                case MicrochunkTileLayer.GroundSolid: return cell.GroundCode;
                case MicrochunkTileLayer.OneWay: return cell.OneWayCode;
                case MicrochunkTileLayer.Breakable: return cell.BreakableCode;
                case MicrochunkTileLayer.Hazard: return cell.HazardCode;
                case MicrochunkTileLayer.Liquid: return cell.LiquidCode;
                case MicrochunkTileLayer.DecorationBack: return cell.DecorationBackCode;
                case MicrochunkTileLayer.DecorationFront: return cell.DecorationFrontCode;
                case MicrochunkTileLayer.Marker: return cell.MarkerCode;
                default: throw new ArgumentOutOfRangeException(nameof(layer));
            }
        }
    }
}
