using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.MapAuthoring.Microchunks
{
    public sealed class MicrochunkPreviewValidationOptions
    {
        public static MicrochunkPreviewValidationOptions All { get; } =
            new MicrochunkPreviewValidationOptions(true, true, true, true, true);

        public bool ValidateTileLayers { get; }
        public bool ValidateCoverage { get; }
        public bool ValidateSocketEdges { get; }
        public bool ValidateObjectSlots { get; }
        public bool ValidateReachability { get; }

        public MicrochunkPreviewValidationOptions(
            bool validateTileLayers,
            bool validateCoverage,
            bool validateSocketEdges,
            bool validateObjectSlots,
            bool validateReachability)
        {
            ValidateTileLayers = validateTileLayers;
            ValidateCoverage = validateCoverage;
            ValidateSocketEdges = validateSocketEdges;
            ValidateObjectSlots = validateObjectSlots;
            ValidateReachability = validateReachability;
        }
    }

    public sealed class MicrochunkPreviewRequest
    {
        private static readonly MicrochunkTransform[] ExactSupportedTransforms =
        {
            MicrochunkTransform.R0,
            MicrochunkTransform.MirrorX,
            MicrochunkTransform.MirrorY,
            MicrochunkTransform.R180
        };

        private readonly IReadOnlyList<MicrochunkTransform> selectedTransforms;
        private readonly IReadOnlyList<MicrochunkCsvImportIssue> importIssues;
        private readonly IReadOnlyList<MicrochunkCsvExportIssue> exportIssues;
        private readonly IReadOnlyDictionary<string, MicrochunkEdgeSignatureDefinition> signaturesById;

        public string SelectedMicrochunkId { get; }
        public MicrochunkSocketAndSlotEditorViewModel EditorState { get; }
        public IReadOnlyList<MicrochunkTransform> SelectedTransforms => selectedTransforms;
        public bool ShowTileOverlay { get; }
        public bool ShowSocketOverlay { get; }
        public bool ShowObjectSlotOverlay { get; }
        public bool ShowReachabilityOverlay { get; }
        public MicrochunkPreviewValidationOptions ValidationOptions { get; }
        public IReadOnlyList<MicrochunkCsvImportIssue> ImportIssues => importIssues;
        public IReadOnlyList<MicrochunkCsvExportIssue> ExportIssues => exportIssues;
        public IReadOnlyDictionary<string, MicrochunkEdgeSignatureDefinition> SignaturesById => signaturesById;
        public MicrochunkObjectSlotValidationPolicy ObjectSlotPolicy { get; }
        public MicrochunkReachabilityPolicy ReachabilityPolicy { get; }

        public static IReadOnlyList<MicrochunkTransform> SupportedTransforms { get; } =
            new ReadOnlyCollection<MicrochunkTransform>(ExactSupportedTransforms);

        public MicrochunkPreviewRequest(
            string selectedMicrochunkId,
            MicrochunkSocketAndSlotEditorViewModel editorState,
            IEnumerable<MicrochunkTransform> selectedTransforms = null,
            bool showTileOverlay = true,
            bool showSocketOverlay = true,
            bool showObjectSlotOverlay = true,
            bool showReachabilityOverlay = true,
            MicrochunkPreviewValidationOptions validationOptions = null,
            IEnumerable<MicrochunkCsvImportIssue> importIssues = null,
            IEnumerable<MicrochunkCsvExportIssue> exportIssues = null,
            IReadOnlyDictionary<string, MicrochunkEdgeSignatureDefinition> signaturesById = null,
            MicrochunkObjectSlotValidationPolicy objectSlotPolicy = null,
            MicrochunkReachabilityPolicy reachabilityPolicy = null)
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
            this.selectedTransforms = FreezeTransforms(selectedTransforms ?? ExactSupportedTransforms);
            ShowTileOverlay = showTileOverlay;
            ShowSocketOverlay = showSocketOverlay;
            ShowObjectSlotOverlay = showObjectSlotOverlay;
            ShowReachabilityOverlay = showReachabilityOverlay;
            ValidationOptions = validationOptions ?? MicrochunkPreviewValidationOptions.All;
            this.importIssues = FreezeImportIssues(importIssues);
            this.exportIssues = FreezeExportIssues(exportIssues);
            this.signaturesById = FreezeSignatures(
                signaturesById ?? EditorState.CreateAuthoringSignatureLookup());
            ObjectSlotPolicy = objectSlotPolicy ?? EditorState.CreateAuthoringSlotPolicy();
            ReachabilityPolicy = reachabilityPolicy ?? MicrochunkReachabilityPolicy.Default;
        }

        public static bool IsSupportedTransform(MicrochunkTransform transform)
        {
            return Array.IndexOf(ExactSupportedTransforms, transform) >= 0;
        }

        private static IReadOnlyList<MicrochunkTransform> FreezeTransforms(
            IEnumerable<MicrochunkTransform> source)
        {
            var values = source == null ? null : source.Distinct().OrderBy(value => value).ToList();
            if (values == null) throw new ArgumentNullException(nameof(source));
            if (values.Count == 0)
            {
                throw new ArgumentException("At least one preview transform is required.", nameof(source));
            }
            if (values.Any(value => !IsSupportedTransform(value)))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(source),
                    "Preview transforms are limited to R0, MIRROR_X, MIRROR_Y, and R180.");
            }
            return new ReadOnlyCollection<MicrochunkTransform>(values);
        }

        private static IReadOnlyList<MicrochunkCsvImportIssue> FreezeImportIssues(
            IEnumerable<MicrochunkCsvImportIssue> source)
        {
            var values = (source ?? Enumerable.Empty<MicrochunkCsvImportIssue>()).ToList();
            if (values.Any(value => value == null))
            {
                throw new ArgumentException("Import diagnostics cannot contain null.", nameof(source));
            }
            values.Sort();
            return new ReadOnlyCollection<MicrochunkCsvImportIssue>(values);
        }

        private static IReadOnlyList<MicrochunkCsvExportIssue> FreezeExportIssues(
            IEnumerable<MicrochunkCsvExportIssue> source)
        {
            var values = (source ?? Enumerable.Empty<MicrochunkCsvExportIssue>()).ToList();
            if (values.Any(value => value == null))
            {
                throw new ArgumentException("Export diagnostics cannot contain null.", nameof(source));
            }
            values.Sort();
            return new ReadOnlyCollection<MicrochunkCsvExportIssue>(values);
        }

        private static IReadOnlyDictionary<string, MicrochunkEdgeSignatureDefinition> FreezeSignatures(
            IReadOnlyDictionary<string, MicrochunkEdgeSignatureDefinition> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var values = new SortedDictionary<string, MicrochunkEdgeSignatureDefinition>(StringComparer.Ordinal);
            foreach (var pair in source)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null)
                {
                    throw new ArgumentException("Signature diagnostics require canonical IDs and values.", nameof(source));
                }
                values.Add(pair.Key, pair.Value);
            }
            return new ReadOnlyDictionary<string, MicrochunkEdgeSignatureDefinition>(values);
        }
    }
}
