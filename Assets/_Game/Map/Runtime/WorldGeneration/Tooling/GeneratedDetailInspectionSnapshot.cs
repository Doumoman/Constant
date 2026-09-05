using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.Tooling
{
    public static class GeneratedDetailInspectionPreconditions
    {
        public const string Map2003HandoffDigest =
            "684a3bf92737aa5073210304b0e3e9d8a8c3857cde45716496df771fe56a835a";
    }

    public sealed class GeneratedDetailTabDefinition
    {
        internal GeneratedDetailTabDefinition(string token, string shortLabel, string ownerCategory)
        {
            Token = token;
            ShortLabel = shortLabel;
            OwnerCategory = ownerCategory;
        }

        public string Token { get; }
        public string ShortLabel { get; }
        public string OwnerCategory { get; }
        public bool IsReadOnly => true;
    }

    /// <summary>The sole production catalog for detail-inspector tabs.</summary>
    public static class GeneratedDetailTabCatalog
    {
        private static readonly ReadOnlyCollection<GeneratedDetailTabDefinition> tabs =
            new ReadOnlyCollection<GeneratedDetailTabDefinition>(new[]
            {
                new GeneratedDetailTabDefinition("Pattern", "PAT", "MicroPattern"),
                new GeneratedDetailTabDefinition("Cluster", "CLU", "TerrainCluster"),
                new GeneratedDetailTabDefinition("Special", "SPC", "SpecialRegion"),
                new GeneratedDetailTabDefinition("Slice", "SLC", "GeneratedSlice"),
            });

        public static IReadOnlyList<GeneratedDetailTabDefinition> Tabs => tabs;
        public static IReadOnlyList<string> Tokens => new ReadOnlyCollection<string>(
            tabs.Select(value => value.Token).ToArray());

        public static int IndexOf(string token)
        {
            for (var index = 0; index < tabs.Count; index++)
                if (string.Equals(tabs[index].Token, token, StringComparison.Ordinal)) return index;
            return -1;
        }

        public static GeneratedDetailTabDefinition Find(string token)
        {
            var index = IndexOf(token);
            if (index < 0) throw new ArgumentException("Unknown detail tab token.", nameof(token));
            return tabs[index];
        }

        internal static GeneratedDetailTabSummary[] NormalizeSummaries(
            IEnumerable<GeneratedDetailTabSummary> summaries)
        {
            var supplied = (summaries ?? Array.Empty<GeneratedDetailTabSummary>())
                .Where(value => value != null).ToArray();
            if (supplied.GroupBy(value => value.TabToken, StringComparer.Ordinal)
                .Any(group => group.Count() != 1))
                throw new ArgumentException("Detail tab summaries must be unique.", nameof(summaries));
            if (supplied.Any(value => IndexOf(value.TabToken) < 0))
                throw new ArgumentException("Detail tab summaries must use catalog tokens.",
                    nameof(summaries));
            var byToken = supplied.ToDictionary(value => value.TabToken, StringComparer.Ordinal);
            return tabs.Select(tab => byToken.TryGetValue(tab.Token, out var summary)
                ? summary
                : GeneratedDetailTabSummary.Missing(tab.Token)).ToArray();
        }
    }

    public sealed class GeneratedDetailTabSummary
    {
        public GeneratedDetailTabSummary(string tabToken, int selectedRecordCount,
            int missingDataCount, string canonicalDigestContribution)
        {
            var definition = GeneratedDetailTabCatalog.Find(tabToken);
            if (selectedRecordCount < 0) throw new ArgumentOutOfRangeException(
                nameof(selectedRecordCount));
            if (missingDataCount < 0) throw new ArgumentOutOfRangeException(
                nameof(missingDataCount));
            TabToken = definition.Token;
            ShortLabel = definition.ShortLabel;
            SelectedRecordCount = selectedRecordCount;
            MissingDataCount = missingDataCount;
            CanonicalDigestContribution = canonicalDigestContribution ?? string.Empty;
        }

        public string TabToken { get; }
        public string ShortLabel { get; }
        public int SelectedRecordCount { get; }
        public int MissingDataCount { get; }
        public string CanonicalDigestContribution { get; }

        public static GeneratedDetailTabSummary Missing(string token) =>
            new GeneratedDetailTabSummary(token, 0, 1,
                BakingCanonicalDigest.HashCanonicalLines(new[] { token, "MissingData" }));

        internal string CanonicalLine => GeneratedInspectionCanonical.Join(TabToken, ShortLabel,
            SelectedRecordCount.ToString(CultureInfo.InvariantCulture),
            MissingDataCount.ToString(CultureInfo.InvariantCulture), CanonicalDigestContribution);
    }

    public sealed class GeneratedDetailMissingDataRecord
    {
        public GeneratedDetailMissingDataRecord(string tabToken, string recordId,
            string fieldName, string reason)
        {
            GeneratedDetailTabCatalog.Find(tabToken);
            TabToken = tabToken;
            RecordId = recordId ?? string.Empty;
            FieldName = fieldName ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public string TabToken { get; }
        public string RecordId { get; }
        public string FieldName { get; }
        public string Reason { get; }
        internal string CanonicalLine => GeneratedInspectionCanonical.Join(
            TabToken, RecordId, FieldName, Reason);
    }

    public sealed class GeneratedDetailValidationRecord
    {
        public GeneratedDetailValidationRecord(string owner, string state, string marker)
        {
            Owner = owner ?? string.Empty;
            State = state ?? string.Empty;
            Marker = marker ?? string.Empty;
        }

        public string Owner { get; }
        public string State { get; }
        public string Marker { get; }
        internal string CanonicalLine => GeneratedInspectionCanonical.Join(Owner, State, Marker);
    }

    /// <summary>Immutable context and tab summary for the read-only detail inspector.</summary>
    public sealed class GeneratedDetailInspectionSnapshot
    {
        public const string SchemaVersion = "map20_03.detail_inspection_snapshot.v1";
        public const string TaskId = "MAP20_03_CREATE_PATTERN_CLUSTER_SPECIAL_AND_SLICE_INSPECTORS";

        private readonly ReadOnlyCollection<string> detailTabTokens;
        private readonly ReadOnlyCollection<GeneratedDetailTabSummary> tabSummaries;
        private readonly ReadOnlyCollection<GeneratedDetailMissingDataRecord> missingDataRecords;
        private readonly ReadOnlyCollection<GeneratedDetailValidationRecord> validationRecords;

        public GeneratedDetailInspectionSnapshot(string sourceWorldOverlayDigest,
            string sourceSectorCanvasDigest, string map2003HandoffDigest,
            int selectedSectorX, int selectedSectorY, int selectedCellX, int selectedCellY,
            IEnumerable<GeneratedDetailTabSummary> summaries,
            IEnumerable<GeneratedDetailMissingDataRecord> missingData,
            IEnumerable<GeneratedDetailValidationRecord> validation, string createdUtc)
        {
            ValidateContext(selectedSectorX, selectedSectorY, selectedCellX, selectedCellY);
            SourceWorldOverlayDigest = sourceWorldOverlayDigest ?? string.Empty;
            SourceSectorCanvasDigest = sourceSectorCanvasDigest ?? string.Empty;
            Map2003HandoffDigest = map2003HandoffDigest ?? string.Empty;
            SelectedSectorX = selectedSectorX;
            SelectedSectorY = selectedSectorY;
            SelectedCellX = selectedCellX;
            SelectedCellY = selectedCellY;
            CreatedUtc = createdUtc ?? string.Empty;
            detailTabTokens = new ReadOnlyCollection<string>(
                GeneratedDetailTabCatalog.Tokens.ToArray());
            tabSummaries = new ReadOnlyCollection<GeneratedDetailTabSummary>(
                GeneratedDetailTabCatalog.NormalizeSummaries(summaries));
            missingDataRecords = new ReadOnlyCollection<GeneratedDetailMissingDataRecord>(
                (missingData ?? Array.Empty<GeneratedDetailMissingDataRecord>())
                .Where(value => value != null).OrderBy(value => value.CanonicalLine,
                    StringComparer.Ordinal).ToArray());
            validationRecords = new ReadOnlyCollection<GeneratedDetailValidationRecord>(
                (validation ?? Array.Empty<GeneratedDetailValidationRecord>())
                .Where(value => value != null).OrderBy(value => value.CanonicalLine,
                    StringComparer.Ordinal).ToArray());
            CanonicalDigest = ComputeCanonicalDigest();
        }

        public string SourceWorldOverlayDigest { get; }
        public string SourceSectorCanvasDigest { get; }
        public string Map2003HandoffDigest { get; }
        public int SelectedSectorX { get; }
        public int SelectedSectorY { get; }
        public int SelectedCellX { get; }
        public int SelectedCellY { get; }
        public IReadOnlyList<string> DetailTabTokens => detailTabTokens;
        public IReadOnlyList<GeneratedDetailTabSummary> TabSummaries => tabSummaries;
        public IReadOnlyList<GeneratedDetailMissingDataRecord> MissingDataRecords => missingDataRecords;
        public IReadOnlyList<GeneratedDetailValidationRecord> ValidationRecords => validationRecords;
        public string CreatedUtc { get; }
        public bool CreatedUtcExcludedFromCanonicalDigest => true;
        public string CanonicalDigest { get; }

        public GeneratedDetailTabSummary Summary(string tabToken)
        {
            var index = GeneratedDetailTabCatalog.IndexOf(tabToken);
            if (index < 0) throw new ArgumentException("Unknown detail tab token.", nameof(tabToken));
            return tabSummaries[index];
        }

        public string Serialize() => GeneratedInspectionCanonical.ToJson(ToDocument());

        public static GeneratedDetailInspectionSnapshot Create(
            string sourceWorldOverlayDigest, string sourceSectorCanvasDigest,
            int selectedSectorX, int selectedSectorY, int selectedCellX, int selectedCellY,
            GeneratedPatternClusterSpecialSliceInspection details, string createdUtc)
        {
            if (details == null) throw new ArgumentNullException(nameof(details));
            return new GeneratedDetailInspectionSnapshot(sourceWorldOverlayDigest,
                sourceSectorCanvasDigest, GeneratedDetailInspectionPreconditions.Map2003HandoffDigest,
                selectedSectorX, selectedSectorY, selectedCellX, selectedCellY,
                details.CreateTabSummaries(), details.MissingDataRecords,
                new[]
                {
                    new GeneratedDetailValidationRecord("MAP20_03", "ReadOnly",
                        "No validation execution is owned by this inspector."),
                }, createdUtc);
        }

        private string ComputeCanonicalDigest()
        {
            var lines = new List<string>
            {
                SchemaVersion, TaskId, SourceWorldOverlayDigest, SourceSectorCanvasDigest,
                Map2003HandoffDigest,
                SelectedSectorX.ToString(CultureInfo.InvariantCulture),
                SelectedSectorY.ToString(CultureInfo.InvariantCulture),
                SelectedCellX.ToString(CultureInfo.InvariantCulture),
                SelectedCellY.ToString(CultureInfo.InvariantCulture),
                string.Join("|", detailTabTokens), "created_utc_excluded=true",
            };
            lines.AddRange(tabSummaries.Select(value => "tab=" + value.CanonicalLine));
            lines.AddRange(missingDataRecords.Select(value => "missing=" + value.CanonicalLine));
            lines.AddRange(validationRecords.Select(value => "validation=" + value.CanonicalLine));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        private GeneratedDetailInspectionSnapshotDocument ToDocument() =>
            new GeneratedDetailInspectionSnapshotDocument
            {
                schema_version = SchemaVersion,
                task_id = TaskId,
                source_MAP20_02_world_overlay_digest = SourceWorldOverlayDigest,
                source_MAP20_02_sector_canvas_digest = SourceSectorCanvasDigest,
                MAP20_03_handoff_digest = Map2003HandoffDigest,
                selected_sector_coordinate = new GeneratedDetailCoordinateDocument
                    { x = SelectedSectorX, y = SelectedSectorY },
                selected_cell_coordinate = new GeneratedDetailCoordinateDocument
                    { x = SelectedCellX, y = SelectedCellY },
                detail_tab_tokens = detailTabTokens.ToArray(),
                tab_summaries = tabSummaries.Select(GeneratedDetailTabSummaryDocument.From).ToArray(),
                missing_data_records = missingDataRecords.Select(
                    GeneratedDetailMissingDataDocument.From).ToArray(),
                validation_records = validationRecords.Select(
                    GeneratedDetailValidationDocument.From).ToArray(),
                canonical_digest = CanonicalDigest,
                created_utc = CreatedUtc,
                created_utc_excluded_from_canonical_digest = true,
            };

        private static void ValidateContext(int sectorX, int sectorY, int cellX, int cellY)
        {
            if (sectorX < 0 || sectorX >= GeneratedWorldOverlayLayerCatalog.WorldWidthSectors)
                throw new ArgumentOutOfRangeException(nameof(sectorX));
            if (sectorY < 0 || sectorY >= GeneratedWorldOverlayLayerCatalog.WorldHeightSectors)
                throw new ArgumentOutOfRangeException(nameof(sectorY));
            if (cellX < 0 || cellX >= GeneratedSectorCanvasLayerCatalog.Width)
                throw new ArgumentOutOfRangeException(nameof(cellX));
            if (cellY < 0 || cellY >= GeneratedSectorCanvasLayerCatalog.Height)
                throw new ArgumentOutOfRangeException(nameof(cellY));
        }
    }

    [Serializable]
    internal sealed class GeneratedDetailInspectionSnapshotDocument
    {
        public string schema_version;
        public string task_id;
        public string source_MAP20_02_world_overlay_digest;
        public string source_MAP20_02_sector_canvas_digest;
        public string MAP20_03_handoff_digest;
        public GeneratedDetailCoordinateDocument selected_sector_coordinate;
        public GeneratedDetailCoordinateDocument selected_cell_coordinate;
        public string[] detail_tab_tokens;
        public GeneratedDetailTabSummaryDocument[] tab_summaries;
        public GeneratedDetailMissingDataDocument[] missing_data_records;
        public GeneratedDetailValidationDocument[] validation_records;
        public string canonical_digest;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
    }

    [Serializable]
    internal sealed class GeneratedDetailCoordinateDocument
    {
        public int x;
        public int y;
    }

    [Serializable]
    internal sealed class GeneratedDetailTabSummaryDocument
    {
        public string tab_token;
        public string short_label;
        public int selected_record_count;
        public int missing_data_count;
        public string canonical_digest_contribution;

        public static GeneratedDetailTabSummaryDocument From(GeneratedDetailTabSummary value) =>
            new GeneratedDetailTabSummaryDocument
            {
                tab_token = value.TabToken,
                short_label = value.ShortLabel,
                selected_record_count = value.SelectedRecordCount,
                missing_data_count = value.MissingDataCount,
                canonical_digest_contribution = value.CanonicalDigestContribution,
            };
    }

    [Serializable]
    internal sealed class GeneratedDetailMissingDataDocument
    {
        public string tab_token;
        public string record_id;
        public string field_name;
        public string reason;

        public static GeneratedDetailMissingDataDocument From(GeneratedDetailMissingDataRecord value) =>
            new GeneratedDetailMissingDataDocument
            {
                tab_token = value.TabToken,
                record_id = value.RecordId,
                field_name = value.FieldName,
                reason = value.Reason,
            };
    }

    [Serializable]
    internal sealed class GeneratedDetailValidationDocument
    {
        public string owner;
        public string state;
        public string marker;

        public static GeneratedDetailValidationDocument From(GeneratedDetailValidationRecord value) =>
            new GeneratedDetailValidationDocument
                { owner = value.Owner, state = value.State, marker = value.Marker };
    }
}
