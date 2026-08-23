using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.MapAuthoring.Microchunks
{
    public sealed class MicrochunkAuthoringGridValidationSummary
    {
        public MicrochunkTileLayerRuleResult TileLayerResult { get; }
        public Microchunk96CellValidationResult CoverageResult { get; }
        public bool Success => TileLayerResult.Success && CoverageResult.Success;
        public int IssueCount => TileLayerResult.ViolationCount + CoverageResult.IssueCount;

        public MicrochunkAuthoringGridValidationSummary(
            MicrochunkTileLayerRuleResult tileLayerResult,
            Microchunk96CellValidationResult coverageResult)
        {
            TileLayerResult = tileLayerResult ?? throw new ArgumentNullException(nameof(tileLayerResult));
            CoverageResult = coverageResult ?? throw new ArgumentNullException(nameof(coverageResult));
        }
    }

    public sealed class MicrochunkAuthoringGridViewModel
    {
        public const string PreviewMicrochunkId = "MC_EDITOR_PREVIEW";

        private readonly MicrochunkId projectionId = new MicrochunkId(PreviewMicrochunkId);

        public MicrochunkAuthoringGridState State { get; }
        public MicrochunkAuthoringGridPalette Palette { get; }

        public MicrochunkAuthoringGridViewModel()
            : this(new MicrochunkAuthoringGridState(), new MicrochunkAuthoringGridPalette())
        {
        }

        public MicrochunkAuthoringGridViewModel(
            MicrochunkAuthoringGridState state,
            MicrochunkAuthoringGridPalette palette)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Palette = palette ?? throw new ArgumentNullException(nameof(palette));
        }

        public void PaintCell(int x, int y)
        {
            State.PaintCell(x, y, Palette.SelectedLayer, Palette.SelectedTileCode);
        }

        public void EraseCell(int x, int y)
        {
            State.PaintCell(
                x,
                y,
                Palette.SelectedLayer,
                MicrochunkAuthoringGridCell.EmptyTileCode);
        }

        public IReadOnlyList<MicrochunkLocalCoord> PaintRectangle(
            int minimumX,
            int minimumY,
            int maximumX,
            int maximumY)
        {
            return State.PaintRectangle(
                minimumX,
                minimumY,
                maximumX,
                maximumY,
                Palette.SelectedLayer,
                Palette.SelectedTileCode);
        }

        public void ClearSelectedLayer()
        {
            State.ClearLayer(Palette.SelectedLayer);
        }

        public void ClearAllLayers()
        {
            State.ClearAllLayers();
        }

        public IReadOnlyList<MicrochunkTileCell> ProjectTileCells()
        {
            var projected = new List<MicrochunkTileCell>(MicrochunkConstants.CellCount);
            foreach (var cell in State.Cells)
            {
                projected.Add(cell.ToRuntimeCell());
            }

            return new ReadOnlyCollection<MicrochunkTileCell>(projected);
        }

        public IReadOnlyList<Microchunk96CellRecord> ProjectCoverageRecords()
        {
            var projectedCells = ProjectTileCells();
            var records = new List<Microchunk96CellRecord>(MicrochunkConstants.CellCount);
            for (var index = 0; index < projectedCells.Count; index++)
            {
                var cell = projectedCells[index];
                records.Add(new Microchunk96CellRecord(
                    projectionId,
                    index,
                    cell.Coordinate.X,
                    cell.Coordinate.Y,
                    cell));
            }

            return new ReadOnlyCollection<Microchunk96CellRecord>(records);
        }

        public MicrochunkDefinition ProjectDefinition()
        {
            return new MicrochunkDefinition(
                projectionId,
                "Editor Preview",
                MicrochunkConstants.WidthTiles,
                MicrochunkConstants.HeightTiles,
                MicrochunkUsageClass.Traversal,
                Array.Empty<string>(),
                Array.Empty<string>(),
                new[] { MicrochunkTransform.R0 },
                1,
                0,
                0,
                0,
                true,
                "PREFAB_MC_GRAY",
                true,
                "In-memory editor projection only.",
                ProjectTileCells(),
                Array.Empty<MicrochunkSocketDefinition>(),
                Array.Empty<MicrochunkObjectSlotDefinition>());
        }

        public MicrochunkTileLayerRuleResult ValidateTileLayers()
        {
            var violations = new List<MicrochunkTileLayerRuleViolation>();
            var projected = ProjectTileCells();
            foreach (var cell in projected)
            {
                violations.AddRange(MicrochunkTileLayerRules.ValidateCell(cell).Violations);
            }

            return new MicrochunkTileLayerRuleResult(projected.Count, violations);
        }

        public Microchunk96CellValidationResult ValidateCoverage()
        {
            return new Microchunk96CellValidator().ValidateRecords(
                projectionId,
                ProjectCoverageRecords(),
                Microchunk96CellValidationPolicy.Complete);
        }

        public MicrochunkAuthoringGridValidationSummary Validate()
        {
            return new MicrochunkAuthoringGridValidationSummary(
                ValidateTileLayers(),
                ValidateCoverage());
        }
    }
}
