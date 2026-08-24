using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.MapAuthoring.Microchunks
{
    public sealed class MicrochunkCsvCatalogMetadata
    {
        private readonly IReadOnlyDictionary<string, string> fields;

        public string MicrochunkId { get; }
        public int SourceRowNumber { get; }
        public bool TileDataComplete { get; }
        public IReadOnlyDictionary<string, string> Fields => fields;

        public MicrochunkCsvCatalogMetadata(
            string microchunkId,
            int sourceRowNumber,
            bool tileDataComplete,
            IEnumerable<KeyValuePair<string, string>> fields)
        {
            if (string.IsNullOrWhiteSpace(microchunkId))
            {
                throw new ArgumentException("Microchunk ID is required.", nameof(microchunkId));
            }
            if (sourceRowNumber < 1) throw new ArgumentOutOfRangeException(nameof(sourceRowNumber));
            if (fields == null) throw new ArgumentNullException(nameof(fields));

            MicrochunkId = microchunkId;
            SourceRowNumber = sourceRowNumber;
            TileDataComplete = tileDataComplete;
            var copy = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var field in fields)
            {
                copy.Add(field.Key, field.Value ?? string.Empty);
            }
            this.fields = new ReadOnlyDictionary<string, string>(copy);
        }
    }

    public sealed class MicrochunkCsvVariantMetadata
    {
        private readonly IReadOnlyDictionary<string, string> fields;

        public string MicrochunkId { get; }
        public int SourceRowNumber { get; }
        public IReadOnlyDictionary<string, string> Fields => fields;

        public MicrochunkCsvVariantMetadata(
            string microchunkId,
            int sourceRowNumber,
            IEnumerable<KeyValuePair<string, string>> fields)
        {
            if (string.IsNullOrWhiteSpace(microchunkId))
            {
                throw new ArgumentException("Microchunk ID is required.", nameof(microchunkId));
            }
            if (sourceRowNumber < 1) throw new ArgumentOutOfRangeException(nameof(sourceRowNumber));
            if (fields == null) throw new ArgumentNullException(nameof(fields));

            MicrochunkId = microchunkId;
            SourceRowNumber = sourceRowNumber;
            var copy = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var field in fields)
            {
                copy.Add(field.Key, field.Value ?? string.Empty);
            }
            this.fields = new ReadOnlyDictionary<string, string>(copy);
        }
    }

    public sealed class MicrochunkCsvReferenceMetadata
    {
        private readonly IReadOnlyDictionary<string, string> fields;

        public string FileName { get; }
        public int SourceRowNumber { get; }
        public IReadOnlyDictionary<string, string> Fields => fields;

        public MicrochunkCsvReferenceMetadata(
            string fileName,
            int sourceRowNumber,
            IEnumerable<KeyValuePair<string, string>> fields)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name is required.", nameof(fileName));
            }
            if (sourceRowNumber < 1) throw new ArgumentOutOfRangeException(nameof(sourceRowNumber));
            if (fields == null) throw new ArgumentNullException(nameof(fields));

            FileName = fileName;
            SourceRowNumber = sourceRowNumber;
            var copy = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var field in fields)
            {
                copy.Add(field.Key, field.Value ?? string.Empty);
            }
            this.fields = new ReadOnlyDictionary<string, string>(copy);
        }
    }

    public sealed class MicrochunkCsvImportValidationFeedback
    {
        public MicrochunkTileLayerRuleResult TileLayerResult { get; }
        public Microchunk96CellValidationResult CoverageResult { get; }
        public MicrochunkSocketEdgeValidationResult SocketResult { get; }
        public MicrochunkObjectSlotValidationResult ObjectSlotResult { get; }
        public bool Success =>
            TileLayerResult.Success && CoverageResult.Success &&
            SocketResult.Success && ObjectSlotResult.Success;
        public int IssueCount =>
            TileLayerResult.ViolationCount + CoverageResult.IssueCount +
            SocketResult.IssueCount + ObjectSlotResult.IssueCount;

        public MicrochunkCsvImportValidationFeedback(
            MicrochunkTileLayerRuleResult tileLayerResult,
            Microchunk96CellValidationResult coverageResult,
            MicrochunkSocketEdgeValidationResult socketResult,
            MicrochunkObjectSlotValidationResult objectSlotResult)
        {
            TileLayerResult = tileLayerResult ?? throw new ArgumentNullException(nameof(tileLayerResult));
            CoverageResult = coverageResult ?? throw new ArgumentNullException(nameof(coverageResult));
            SocketResult = socketResult ?? throw new ArgumentNullException(nameof(socketResult));
            ObjectSlotResult = objectSlotResult ?? throw new ArgumentNullException(nameof(objectSlotResult));
        }
    }

    public sealed class MicrochunkCsvImportResult
    {
        private readonly IReadOnlyList<MicrochunkCsvImportIssue> issues;
        private readonly IReadOnlyList<MicrochunkCsvVariantMetadata> variants;
        private readonly IReadOnlyList<MicrochunkCsvReferenceMetadata> referenceRows;

        public MicrochunkCsvImportRequest Request { get; }
        public MicrochunkCsvCatalogMetadata Catalog { get; }
        public MicrochunkSocketAndSlotEditorViewModel EditorState { get; }
        public MicrochunkAuthoringGridViewModel GridViewModel => EditorState.Grid;
        public MicrochunkAuthoringGridState GridState => EditorState.Grid.State;
        public IReadOnlyList<MicrochunkCsvImportIssue> Issues => issues;
        public IReadOnlyList<MicrochunkCsvVariantMetadata> Variants => variants;
        public IReadOnlyList<MicrochunkCsvReferenceMetadata> ReferenceRows => referenceRows;
        public MicrochunkCsvImportValidationFeedback ValidationFeedback { get; }
        public bool HasValidationFeedback => ValidationFeedback != null;
        public bool Success => Catalog != null && issues.All(issue => !issue.IsError);

        internal MicrochunkCsvImportResult(
            MicrochunkCsvImportRequest request,
            MicrochunkCsvCatalogMetadata catalog,
            MicrochunkSocketAndSlotEditorViewModel editorState,
            IEnumerable<MicrochunkCsvImportIssue> issues,
            IEnumerable<MicrochunkCsvVariantMetadata> variants,
            IEnumerable<MicrochunkCsvReferenceMetadata> referenceRows,
            MicrochunkCsvImportValidationFeedback validationFeedback)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Catalog = catalog;
            EditorState = editorState ?? throw new ArgumentNullException(nameof(editorState));
            if (issues == null) throw new ArgumentNullException(nameof(issues));
            if (variants == null) throw new ArgumentNullException(nameof(variants));
            if (referenceRows == null) throw new ArgumentNullException(nameof(referenceRows));

            var orderedIssues = issues.ToList();
            orderedIssues.Sort();
            this.issues = new ReadOnlyCollection<MicrochunkCsvImportIssue>(orderedIssues);
            this.variants = new ReadOnlyCollection<MicrochunkCsvVariantMetadata>(
                variants.OrderBy(value => value.SourceRowNumber).ToList());
            this.referenceRows = new ReadOnlyCollection<MicrochunkCsvReferenceMetadata>(
                referenceRows
                    .OrderBy(value => value.FileName, StringComparer.Ordinal)
                    .ThenBy(value => value.SourceRowNumber)
                    .ToList());
            ValidationFeedback = validationFeedback;
        }
    }
}
