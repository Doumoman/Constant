using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Microchunks;
using UnityEngine;

namespace StarNight.MapAuthoring.Boundaries
{
    public sealed class MoonpalaceBoundaryPreviewViewModel
    {
        private const string PairRulesPath =
            "_Game/Map/Data/WorldGeneration/Authoring/Boundary/biome_boundary_pair_rules.csv";
        private const string ProfilesPath =
            "_Game/Map/Data/WorldGeneration/Authoring/Boundary/biome_boundary_profiles.csv";
        private const string BoundaryCatalogPath =
            "_Game/Map/Data/WorldGeneration/Authoring/Boundary/boundary_chunk_catalog.csv";
        private const string MicrochunkCatalogPath =
            "_Game/Map/Data/WorldGeneration/Authoring/MicroChunk/microchunk_catalog.csv";
        private const string TileCellsPath =
            "_Game/Map/Data/WorldGeneration/Authoring/MicroChunk/microchunk_tile_cells.csv";
        private const string SocketsPath =
            "_Game/Map/Data/WorldGeneration/Authoring/MicroChunk/microchunk_sockets.csv";

        private static readonly object CacheSync = new object();
        private static SourceSnapshot cachedSource;

        private readonly MoonpalaceBoundaryCoverageReport coverageReport;
        private readonly IReadOnlyList<MoonpalaceBoundaryCoverageCandidateEvidence> candidates;
        private readonly IReadOnlyList<MoonpalaceBoundaryPreviewIssueView> loadIssues;

        public MoonpalaceBoundaryPreviewViewModel(
            MoonpalaceBoundaryCoverageReport coverageReport,
            IEnumerable<MoonpalaceBoundaryCoverageCandidateEvidence> candidates,
            IEnumerable<MoonpalaceBoundaryPreviewIssueView> loadIssues = null)
        {
            this.coverageReport = coverageReport;
            this.candidates = new ReadOnlyCollection<MoonpalaceBoundaryCoverageCandidateEvidence>(
                (candidates ?? Enumerable.Empty<MoonpalaceBoundaryCoverageCandidateEvidence>())
                .Where(value => value != null)
                .ToArray());
            this.loadIssues = new ReadOnlyCollection<MoonpalaceBoundaryPreviewIssueView>(
                (loadIssues ?? Enumerable.Empty<MoonpalaceBoundaryPreviewIssueView>())
                .Where(value => value != null)
                .OrderBy(value => value)
                .ToArray());

            var first = coverageReport == null ? null : coverageReport.PairReports.FirstOrDefault();
            Selection = first == null
                ? new MoonpalaceBoundaryPreviewSelection(
                    string.Empty,
                    MoonpalaceBoundaryPreviewSelection.HorizontalToken,
                    string.Empty,
                    -1,
                    MoonpalaceBoundaryRequestDirection.Forward)
                : new MoonpalaceBoundaryPreviewSelection(
                    first.PairRuleId,
                    MoonpalaceBoundaryPreviewSelection.HorizontalToken,
                    first.Requirement.DefaultProfileId,
                    -1,
                    MoonpalaceBoundaryRequestDirection.Forward);
            Overlays = MoonpalaceBoundaryPreviewOverlayToggle.All;
            Rebuild();
        }

        public MoonpalaceBoundaryCoverageReport CoverageReport => coverageReport;
        public IReadOnlyList<MoonpalaceBoundaryCoverageCandidateEvidence> SourceCandidates => candidates;
        public MoonpalaceBoundaryPreviewSelection Selection { get; private set; }
        public MoonpalaceBoundaryPreviewOverlayToggle Overlays { get; private set; }
        public MoonpalaceBoundaryPreviewReport CurrentReport { get; private set; }
        public bool Accepted => coverageReport != null && coverageReport.Accepted;

        public IReadOnlyList<string> AvailableProfiles
        {
            get
            {
                var pair = coverageReport == null
                    ? null
                    : coverageReport.PairReports.FirstOrDefault(value =>
                        string.Equals(value.PairRuleId, Selection.PairRuleId, StringComparison.Ordinal));
                return pair == null
                    ? Array.Empty<string>()
                    : pair.Requirement.AllowedProfileIds;
            }
        }

        public static MoonpalaceBoundaryPreviewViewModel LoadApprovedAuthoring(bool forceReload = false)
        {
            try
            {
                SourceSnapshot source;
                lock (CacheSync)
                {
                    if (forceReload || cachedSource == null) cachedSource = ReadApprovedSource();
                    source = cachedSource;
                }
                return new MoonpalaceBoundaryPreviewViewModel(source.Report, source.Candidates);
            }
            catch (Exception exception)
            {
                return new MoonpalaceBoundaryPreviewViewModel(
                    null,
                    Array.Empty<MoonpalaceBoundaryCoverageCandidateEvidence>(),
                    new[]
                    {
                        new MoonpalaceBoundaryPreviewIssueView(
                            MoonpalaceBoundaryPreviewIssueSeverity.Error,
                            "AUTHORING_LOAD_FAILED",
                            exception.GetType().Name + ": " + exception.Message),
                    });
            }
        }

        public void SelectPair(string pairRuleId)
        {
            var pair = coverageReport == null
                ? null
                : coverageReport.PairReports.FirstOrDefault(value =>
                    string.Equals(value.PairRuleId, pairRuleId, StringComparison.Ordinal));
            Selection = new MoonpalaceBoundaryPreviewSelection(
                pairRuleId,
                Selection.OrientationToken,
                pair == null ? string.Empty : pair.Requirement.DefaultProfileId,
                -1,
                Selection.Direction);
            Rebuild();
        }

        public void SelectOrientation(string orientationToken)
        {
            Selection = Selection.WithOrientation(orientationToken);
            Rebuild();
        }

        public void SelectProfile(string profileId)
        {
            Selection = Selection.WithProfile(profileId);
            Rebuild();
        }

        public bool SelectCandidateIndex(int candidateIndex)
        {
            Selection = Selection.WithCandidateIndex(candidateIndex);
            Rebuild();
            return CurrentReport.SelectedCandidate != null &&
                   CurrentReport.SelectedCandidate.SourceIndex == candidateIndex &&
                   CurrentReport.SelectedCandidate.Enabled;
        }

        public void SelectDirection(MoonpalaceBoundaryRequestDirection direction)
        {
            Selection = Selection.WithDirection(direction);
            Rebuild();
        }

        public void SetOverlay(MoonpalaceBoundaryPreviewOverlayToggle toggle, bool enabled)
        {
            if (enabled) Overlays |= toggle;
            else Overlays &= ~toggle;
            Rebuild();
        }

        private void Rebuild()
        {
            var issues = new List<MoonpalaceBoundaryPreviewIssueView>(loadIssues);
            var pairRows = coverageReport == null
                ? new List<MoonpalaceBoundaryPreviewPairView>()
                : coverageReport.PairReports.Select(value => new MoonpalaceBoundaryPreviewPairView(value)).ToList();

            if (coverageReport == null)
            {
                issues.Add(Issue("REPORT_NOT_AVAILABLE", "No MAP08_12 coverage report is available."));
            }
            else
            {
                issues.AddRange(coverageReport.Issues.Select(MoonpalaceBoundaryPreviewIssueView.FromCoverageIssue));
                if (!coverageReport.Accepted)
                {
                    issues.Add(Issue("REPORT_REJECTED", "The MAP08_12 coverage report is rejected."));
                }
            }

            var selectedPair = pairRows.FirstOrDefault(value =>
                string.Equals(value.PairRuleId, Selection.PairRuleId, StringComparison.Ordinal));
            if (selectedPair == null && coverageReport != null)
            {
                issues.Add(Issue(
                    "PAIR_NOT_FOUND",
                    "The selected pair report is missing: " + Selection.PairRuleId,
                    Selection.PairRuleId));
            }

            var orientationValid = Selection.TryGetOrientation(out var orientation);
            if (!orientationValid)
            {
                issues.Add(Issue(
                    "ORIENTATION_UNKNOWN",
                    "Unknown orientation: " + Selection.OrientationToken,
                    Selection.PairRuleId));
            }

            var profileValid = selectedPair != null &&
                               selectedPair.Profiles.Contains(Selection.ProfileId, StringComparer.Ordinal);
            if (selectedPair != null && !profileValid)
            {
                issues.Add(Issue(
                    "PROFILE_UNKNOWN",
                    "Unknown profile for selected pair: " + Selection.ProfileId,
                    Selection.PairRuleId));
            }

            var sourceRows = selectedPair == null
                ? new List<MoonpalaceBoundaryCoverageCandidateEvidence>()
                : candidates.Where(value => string.Equals(
                        value.PairRuleId, selectedPair.PairRuleId, StringComparison.Ordinal))
                    .OrderBy(value => selectedPair.Source.Requirement.GetProfileOrder(value.ProfileId))
                    .ThenBy(value => value.Orientation)
                    .ThenBy(value => value.CandidateId, StringComparer.Ordinal)
                    .ToList();
            if (selectedPair != null && sourceRows.Count == 0)
            {
                issues.Add(Issue(
                    "CANDIDATE_EVIDENCE_MISSING",
                    "No candidate evidence is available for the selected pair.",
                    selectedPair.PairRuleId));
            }

            var candidateRows = new List<MoonpalaceBoundaryPreviewCandidateView>();
            for (var index = 0; index < sourceRows.Count; index++)
            {
                var source = sourceRows[index];
                var disabledReason = CandidateDisabledReason(
                    source, orientationValid, orientation, profileValid, issues);
                candidateRows.Add(new MoonpalaceBoundaryPreviewCandidateView(
                    index,
                    source,
                    selectedPair,
                    string.IsNullOrEmpty(disabledReason),
                    disabledReason,
                    Selection.Direction));
            }

            var candidateIndex = Selection.CandidateIndex;
            if (candidateIndex < 0)
            {
                var firstEnabled = candidateRows.FirstOrDefault(value => value.Enabled);
                candidateIndex = firstEnabled == null ? -1 : firstEnabled.SourceIndex;
                if (candidateIndex >= 0) Selection = Selection.WithCandidateIndex(candidateIndex);
            }

            MoonpalaceBoundaryPreviewCandidateView selectedCandidate = null;
            if (candidateIndex >= 0 && candidateIndex < candidateRows.Count)
            {
                var candidate = candidateRows[candidateIndex];
                if (candidate.Enabled) selectedCandidate = candidate;
                else
                {
                    issues.Add(Issue(
                        "CANDIDATE_DISABLED",
                        candidate.DisabledReason,
                        Selection.PairRuleId,
                        candidate.CandidateId,
                        candidate.SourceMicrochunkId));
                }
            }
            else if (Selection.CandidateIndex >= 0)
            {
                issues.Add(Issue(
                    "CANDIDATE_INDEX_INVALID",
                    "Candidate index is outside the deterministic candidate list: " +
                    Selection.CandidateIndex.ToString(CultureInfo.InvariantCulture),
                    Selection.PairRuleId));
            }

            var cells = selectedCandidate == null
                ? Array.Empty<MoonpalaceBoundaryPreviewCell>()
                : BuildCells(selectedCandidate, issues.Count > 0);
            CurrentReport = new MoonpalaceBoundaryPreviewReport(
                coverageReport,
                pairRows,
                candidateRows,
                selectedCandidate,
                cells,
                issues,
                Selection,
                Overlays);
        }

        private string CandidateDisabledReason(
            MoonpalaceBoundaryCoverageCandidateEvidence candidate,
            bool orientationValid,
            MoonpalaceBoundaryOrientation orientation,
            bool profileValid,
            IEnumerable<MoonpalaceBoundaryPreviewIssueView> issues)
        {
            var coverageIssue = issues.FirstOrDefault(value =>
                !string.IsNullOrEmpty(value.CandidateId) &&
                string.Equals(value.CandidateId, candidate.CandidateId, StringComparison.Ordinal));
            if (coverageIssue != null) return "Coverage issue " + coverageIssue.Code + ": " + coverageIssue.Message;
            if (!candidate.Active || !candidate.MicrochunkActive) return "Candidate or microchunk is inactive.";
            if (!candidate.TileDataComplete || candidate.TileCells.Count != 96)
                return "Candidate does not contain complete 12x8 evidence.";
            if (!orientationValid) return "Unknown orientation filter.";
            if (!profileValid) return "Unknown profile filter.";
            if (candidate.Orientation != orientation) return "Filtered out by orientation.";
            if (!string.Equals(candidate.ProfileId, Selection.ProfileId, StringComparison.Ordinal))
                return "Filtered out by profile.";
            return string.Empty;
        }

        private MoonpalaceBoundaryPreviewCell[] BuildCells(
            MoonpalaceBoundaryPreviewCandidateView selected,
            bool hasIssues)
        {
            var transform = MoonpalaceBoundaryTransformPolicy.Create(
                Selection.Direction,
                selected.Source.Orientation).Transform;
            var values = new List<MoonpalaceBoundaryPreviewCell>();
            foreach (var source in selected.Source.TileCells)
            {
                if (!MicrochunkLocalCoord.TryCreate(source.LocalX, source.LocalY, out var sourceCoordinate))
                    continue;
                var coordinate = MicrochunkTransformUtility.TransformCoordinate(sourceCoordinate, transform);
                var foregroundEvidence = IsEvidence(source.GroundCode);
                var backgroundEvidence = IsEvidence(source.DecorBackCode);
                values.Add(new MoonpalaceBoundaryPreviewCell(
                    coordinate.X,
                    coordinate.Y,
                    source.LocalX,
                    source.LocalY,
                    source.GroundCode,
                    source.DecorBackCode,
                    source.MarkerCode,
                    HasOverlay(MoonpalaceBoundaryPreviewOverlayToggle.Foreground) && foregroundEvidence,
                    HasOverlay(MoonpalaceBoundaryPreviewOverlayToggle.Background) && backgroundEvidence,
                    HasOverlay(MoonpalaceBoundaryPreviewOverlayToggle.Route) &&
                    string.Equals(source.MarkerCode, "M_ROUTE_MAIN", StringComparison.Ordinal),
                    HasOverlay(MoonpalaceBoundaryPreviewOverlayToggle.Sockets) &&
                    string.Equals(source.MarkerCode, "M_SOCKET", StringComparison.Ordinal),
                    HasOverlay(MoonpalaceBoundaryPreviewOverlayToggle.Warnings) &&
                    (foregroundEvidence || backgroundEvidence),
                    HasOverlay(MoonpalaceBoundaryPreviewOverlayToggle.BoundaryLayer) &&
                    string.Equals(selected.ProfileId, "BOUND_LAYER", StringComparison.Ordinal),
                    HasOverlay(MoonpalaceBoundaryPreviewOverlayToggle.Issues) &&
                    hasIssues && coordinate.X == 0 && coordinate.Y == 0));
            }
            return values.OrderBy(value => value.RowMajorIndex).ToArray();
        }

        private bool HasOverlay(MoonpalaceBoundaryPreviewOverlayToggle toggle)
        {
            return (Overlays & toggle) == toggle;
        }

        private static bool IsEvidence(string value)
        {
            return !string.IsNullOrEmpty(value) && !string.Equals(value, "NONE", StringComparison.Ordinal);
        }

        private static MoonpalaceBoundaryPreviewIssueView Issue(
            string code,
            string message,
            string pairRuleId = "",
            string candidateId = "",
            string microchunkId = "")
        {
            return new MoonpalaceBoundaryPreviewIssueView(
                MoonpalaceBoundaryPreviewIssueSeverity.Error,
                code,
                message,
                pairRuleId,
                candidateId,
                microchunkId);
        }

        private static SourceSnapshot ReadApprovedSource()
        {
            var pairRows = ReadCsv(PairRulesPath);
            var profileRows = ReadCsv(ProfilesPath)
                .ToDictionary(value => value["boundary_profile_id"], StringComparer.Ordinal);
            var boundaryRows = ReadCsv(BoundaryCatalogPath);
            var microchunkRows = ReadCsv(MicrochunkCatalogPath)
                .ToDictionary(value => value["microchunk_id"], StringComparer.Ordinal);
            var tileRows = ReadCsv(TileCellsPath)
                .GroupBy(value => value["microchunk_id"], StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
            var socketRows = ReadCsv(SocketsPath)
                .GroupBy(value => value["microchunk_id"], StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

            var requirements = pairRows.Select(row =>
            {
                var expected = MoonpalaceBoundaryCoverageRequirement.Canonical.Single(value =>
                    string.Equals(value.PairRuleId, row["boundary_pair_rule_id"], StringComparison.Ordinal));
                return new MoonpalaceBoundaryCoverageRequirement(
                    expected.PairOrder,
                    row["boundary_pair_rule_id"],
                    row["biome_a_id"],
                    row["biome_b_id"],
                    Split(row["allowed_boundary_profile_ids"]),
                    Split(row["boundary_profile_weights"]).Select(ParseInt),
                    row["default_boundary_profile_id"],
                    expected.ExpectedCandidateCount,
                    expected.ExpectedMicrochunkCount,
                    expected.ExpectedTileRowCount,
                    expected.ExpectedSocketRowCount,
                    ParseBool(row["active"]));
            }).OrderBy(value => value.PairOrder).ToArray();
            var pairByBiomes = requirements.ToDictionary(
                value => value.BiomeAId + "|" + value.BiomeBId,
                StringComparer.Ordinal);

            var candidates = boundaryRows.Select(row =>
            {
                var microchunk = microchunkRows[row["microchunk_id"]];
                var profile = profileRows[row["boundary_profile_id"]];
                var requirement = pairByBiomes[row["biome_a_id"] + "|" + row["biome_b_id"]];
                var tiles = tileRows[row["microchunk_id"]].Select(value =>
                    new MoonpalaceBoundaryCoverageCandidateEvidence.TileCell(
                        ParseInt(value["local_x"]),
                        ParseInt(value["local_y"]),
                        value["ground_code"],
                        value["decor_back_code"],
                        value["marker_code"]));
                var sockets = socketRows[row["microchunk_id"]].Select(value =>
                    new MoonpalaceBoundaryCoverageCandidateEvidence.Socket(
                        value["socket_id"],
                        value["side"],
                        value["traversal_kind"],
                        ParseBool(value["mandatory_allowed"]),
                        value["tool_requirement"],
                        value["edge_signature_id"],
                        value["route_layer"],
                        ParseInt(value["minimum_safe_tiles"])));
                return new MoonpalaceBoundaryCoverageCandidateEvidence(
                    row["boundary_chunk_id"],
                    row["microchunk_id"],
                    requirement.PairRuleId,
                    row["biome_a_id"],
                    row["biome_b_id"],
                    row["boundary_profile_id"],
                    ParseOrientation(row["orientation"]),
                    ParseInt(row["route_type"]),
                    row["entry_edge_signature_id"],
                    row["exit_edge_signature_id"],
                    ParseInt(row["weight"]),
                    ParseBool(row["reversible"]),
                    ParseBool(row["active"]),
                    ParseBool(profile["mandatory_route_allowed"]),
                    profile["tool_requirement"],
                    ParseInt(microchunk["width_tiles"]),
                    ParseInt(microchunk["height_tiles"]),
                    microchunk["usage_class"],
                    microchunk["biome_ids"],
                    microchunk["route_roles"],
                    ParseBool(microchunk["tile_data_complete"]),
                    ParseBool(microchunk["active"]),
                    tiles,
                    sockets);
            }).OrderBy(value => requirementOrder(value.PairRuleId))
              .ThenBy(value => value.CandidateId, StringComparer.Ordinal)
              .ToArray();

            var sourceChain = new MoonpalaceBoundaryCoverageValidator.SourceChain(
                MoonpalaceBoundaryCoverageValidator.ExpectedAuthoringManifestSha256,
                MoonpalaceBoundaryCoverageValidator.ExpectedPreviousTaskSha256,
                0,
                0);
            var report = new MoonpalaceBoundaryCoverageValidator().Validate(
                requirements,
                candidates,
                sourceChain);
            return new SourceSnapshot(report, candidates);
        }

        private static int requirementOrder(string pairRuleId)
        {
            return MoonpalaceBoundaryCoverageRequirement.TryGetCanonical(pairRuleId, out var requirement)
                ? requirement.PairOrder
                : int.MaxValue;
        }

        private static List<Dictionary<string, string>> ReadCsv(string assetsRelativePath)
        {
            var path = Path.Combine(
                Application.dataPath,
                assetsRelativePath.Replace('/', Path.DirectorySeparatorChar));
            var read = new Rfc4180CsvReader().Read(File.ReadAllBytes(path), Path.GetFileName(path));
            if (!read.Success)
            {
                throw new InvalidDataException(string.Join(
                    "; ", read.Errors.Select(value => value.ToString()).OrderBy(value => value, StringComparer.Ordinal)));
            }
            if (read.Records.Count == 0) throw new InvalidDataException(assetsRelativePath + " has no header.");

            var headers = read.Records[0].Fields.Select(value => value.Value).ToArray();
            if (headers.Length != headers.Distinct(StringComparer.Ordinal).Count())
                throw new InvalidDataException(assetsRelativePath + " has duplicate headers.");
            var rows = new List<Dictionary<string, string>>();
            for (var rowIndex = 1; rowIndex < read.Records.Count; rowIndex++)
            {
                var record = read.Records[rowIndex];
                if (record.Fields.Count != headers.Length)
                    throw new InvalidDataException(assetsRelativePath + " has a row width mismatch.");
                var row = new Dictionary<string, string>(StringComparer.Ordinal);
                for (var fieldIndex = 0; fieldIndex < headers.Length; fieldIndex++)
                    row.Add(headers[fieldIndex], record.Fields[fieldIndex].Value);
                rows.Add(row);
            }
            return rows;
        }

        private static string[] Split(string value)
        {
            return value.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static bool ParseBool(string value)
        {
            if (string.Equals(value, "1", StringComparison.Ordinal)) return true;
            if (string.Equals(value, "0", StringComparison.Ordinal)) return false;
            throw new FormatException("Unknown bool token: " + value);
        }

        private static int ParseInt(string value)
        {
            return int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        private static MoonpalaceBoundaryOrientation ParseOrientation(string value)
        {
            if (string.Equals(value, MoonpalaceBoundaryPreviewSelection.HorizontalToken, StringComparison.Ordinal))
                return MoonpalaceBoundaryOrientation.Horizontal;
            if (string.Equals(value, MoonpalaceBoundaryPreviewSelection.VerticalToken, StringComparison.Ordinal))
                return MoonpalaceBoundaryOrientation.Vertical;
            throw new FormatException("Unknown boundary orientation: " + value);
        }

        private sealed class SourceSnapshot
        {
            public SourceSnapshot(
                MoonpalaceBoundaryCoverageReport report,
                IEnumerable<MoonpalaceBoundaryCoverageCandidateEvidence> candidates)
            {
                Report = report;
                Candidates = new ReadOnlyCollection<MoonpalaceBoundaryCoverageCandidateEvidence>(
                    candidates.ToArray());
            }

            public MoonpalaceBoundaryCoverageReport Report { get; }
            public IReadOnlyList<MoonpalaceBoundaryCoverageCandidateEvidence> Candidates { get; }
        }
    }
}
