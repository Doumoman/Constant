using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.MapAuthoring.Microchunks
{
    public sealed class MicrochunkPreviewTransformReport
    {
        private readonly IReadOnlyList<MicrochunkPreviewCellOverlay> cells;

        public MicrochunkTransform Transform { get; }
        public MicrochunkDefinition Definition { get; }
        public IReadOnlyList<MicrochunkPreviewCellOverlay> Cells => cells;
        public MicrochunkTileLayerRuleResult TileLayerResult { get; }
        public Microchunk96CellValidationResult CoverageResult { get; }
        public MicrochunkSocketEdgeValidationResult SocketResult { get; }
        public MicrochunkObjectSlotValidationResult ObjectSlotResult { get; }
        public MicrochunkReachabilityResult ReachabilityResult { get; }
        public IReadOnlyList<MicrochunkReachabilityPathWitness> MandatorySocketPairWitnesses =>
            ReachabilityResult == null
                ? Array.Empty<MicrochunkReachabilityPathWitness>()
                : ReachabilityResult.PathWitnesses;

        public MicrochunkPreviewTransformReport(
            MicrochunkTransform transform,
            MicrochunkDefinition definition,
            IEnumerable<MicrochunkPreviewCellOverlay> cells,
            MicrochunkTileLayerRuleResult tileLayerResult,
            Microchunk96CellValidationResult coverageResult,
            MicrochunkSocketEdgeValidationResult socketResult,
            MicrochunkObjectSlotValidationResult objectSlotResult,
            MicrochunkReachabilityResult reachabilityResult)
        {
            if (!MicrochunkPreviewRequest.IsSupportedTransform(transform))
                throw new ArgumentOutOfRangeException(nameof(transform));
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            var values = cells.OrderBy(value => value.Coordinate.RowMajorIndex).ToList();
            if (values.Count != MicrochunkConstants.CellCount ||
                values.Select(value => value.Coordinate).Distinct().Count() != MicrochunkConstants.CellCount)
            {
                throw new ArgumentException("Every transform preview requires exactly 96 unique cells.", nameof(cells));
            }
            if (values.Any(value => value.Transform != transform))
                throw new ArgumentException("Every cell must belong to the transform report.", nameof(cells));

            Transform = transform;
            this.cells = new ReadOnlyCollection<MicrochunkPreviewCellOverlay>(values);
            TileLayerResult = tileLayerResult;
            CoverageResult = coverageResult;
            SocketResult = socketResult;
            ObjectSlotResult = objectSlotResult;
            ReachabilityResult = reachabilityResult;
        }

        public MicrochunkPreviewCellOverlay GetCell(int x, int y)
        {
            return cells[new MicrochunkLocalCoord(x, y).RowMajorIndex];
        }
    }

    public sealed class MicrochunkPreviewReport
    {
        private readonly IReadOnlyList<MicrochunkPreviewTransformReport> transforms;
        private readonly IReadOnlyList<MicrochunkPreviewIssue> issues;

        public string SelectedMicrochunkId { get; }
        public IReadOnlyList<MicrochunkPreviewTransformReport> Transforms => transforms;
        public IReadOnlyList<MicrochunkPreviewIssue> Issues => issues;
        public int ErrorCount => issues.Count(value => value.IsError);
        public int WarningCount => issues.Count(value => value.Severity == MicrochunkPreviewIssueSeverity.Warning);
        public bool Success => ErrorCount == 0 && transforms.Count > 0;

        public MicrochunkPreviewReport(
            string selectedMicrochunkId,
            IEnumerable<MicrochunkPreviewTransformReport> transforms,
            IEnumerable<MicrochunkPreviewIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(selectedMicrochunkId))
                throw new ArgumentException("Selected microchunk ID is required.", nameof(selectedMicrochunkId));
            if (transforms == null) throw new ArgumentNullException(nameof(transforms));
            if (issues == null) throw new ArgumentNullException(nameof(issues));

            var transformValues = transforms.OrderBy(value => value.Transform).ToList();
            if (transformValues.Any(value => value == null) ||
                transformValues.Select(value => value.Transform).Distinct().Count() != transformValues.Count)
            {
                throw new ArgumentException("Transform reports must be non-null and unique.", nameof(transforms));
            }
            var issueValues = issues.ToList();
            if (issueValues.Any(value => value == null))
                throw new ArgumentException("Preview issues cannot contain null.", nameof(issues));
            issueValues.Sort();

            SelectedMicrochunkId = selectedMicrochunkId;
            this.transforms = new ReadOnlyCollection<MicrochunkPreviewTransformReport>(transformValues);
            this.issues = new ReadOnlyCollection<MicrochunkPreviewIssue>(issueValues);
        }

        public MicrochunkPreviewTransformReport GetTransform(MicrochunkTransform transform)
        {
            var value = transforms.FirstOrDefault(candidate => candidate.Transform == transform);
            if (value == null)
                throw new KeyNotFoundException("Preview transform was not generated: " + transform);
            return value;
        }
    }
}
