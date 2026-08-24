using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.MapAuthoring.Microchunks
{
    public sealed class MicrochunkCsvExportRequest
    {
        private readonly IReadOnlyList<MicrochunkCsvVariantMetadata> variants;

        public string SelectedMicrochunkId { get; }
        public MicrochunkSocketAndSlotEditorViewModel EditorState { get; }
        public MicrochunkCsvCatalogMetadata Catalog { get; }
        public IReadOnlyList<MicrochunkCsvVariantMetadata> Variants => variants;
        public bool AllowNewCatalogRow { get; }

        public MicrochunkCsvExportRequest(
            string selectedMicrochunkId,
            MicrochunkSocketAndSlotEditorViewModel editorState,
            MicrochunkCsvCatalogMetadata catalog,
            IEnumerable<MicrochunkCsvVariantMetadata> variants = null,
            bool allowNewCatalogRow = false)
        {
            if (string.IsNullOrWhiteSpace(selectedMicrochunkId))
            {
                throw new ArgumentException(
                    "Exactly one selected microchunk ID is required.",
                    nameof(selectedMicrochunkId));
            }
            if (!string.Equals(selectedMicrochunkId, selectedMicrochunkId.Trim(), StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Selected microchunk ID must be canonical and contain no surrounding whitespace.",
                    nameof(selectedMicrochunkId));
            }

            SelectedMicrochunkId = selectedMicrochunkId;
            EditorState = editorState ?? throw new ArgumentNullException(nameof(editorState));
            if (catalog != null && !string.Equals(
                    catalog.MicrochunkId,
                    selectedMicrochunkId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Catalog metadata must belong to the selected microchunk ID.",
                    nameof(catalog));
            }

            Catalog = catalog;
            AllowNewCatalogRow = allowNewCatalogRow;
            var copy = (variants ?? Enumerable.Empty<MicrochunkCsvVariantMetadata>()).ToList();
            if (copy.Any(value => value == null || !string.Equals(
                    value.MicrochunkId,
                    selectedMicrochunkId,
                    StringComparison.Ordinal)))
            {
                throw new ArgumentException(
                    "Every variant metadata row must belong to the selected microchunk ID.",
                    nameof(variants));
            }

            this.variants = new ReadOnlyCollection<MicrochunkCsvVariantMetadata>(copy
                .OrderBy(value => FieldSignature(value.Fields), StringComparer.Ordinal)
                .ThenBy(value => value.SourceRowNumber)
                .ToList());
        }

        public static MicrochunkCsvExportRequest FromImportResult(
            MicrochunkCsvImportResult importResult,
            bool allowNewCatalogRow = false)
        {
            if (importResult == null) throw new ArgumentNullException(nameof(importResult));
            return new MicrochunkCsvExportRequest(
                importResult.Request.SelectedMicrochunkId,
                importResult.EditorState,
                importResult.Catalog,
                importResult.Variants,
                allowNewCatalogRow);
        }

        private static string FieldSignature(IReadOnlyDictionary<string, string> fields)
        {
            return string.Join("\n", fields.OrderBy(value => value.Key, StringComparer.Ordinal)
                .Select(value => value.Key + "=" + value.Value));
        }
    }
}
