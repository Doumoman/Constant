using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Boundaries;
using UnityEngine;

namespace StarNight.Map.WorldGeneration.Tooling
{
    public static class GeneratedReplayAuthoringPreconditions
    {
        public const string TaskId =
            "MAP20_05_IMPLEMENT_REPLAY_AUTHORING_INTEGRATION_HUD_AND_EXPORT";
        public const string SourceMap2004ResultDigest =
            "f9bac092ce7e1177e9eea6ca61a46ea671330790b55355206c06027ac1e77235";
        public const string SourceMap2004TaskDigest =
            "f398401662bcb9134db8269fa6dbc4d42752ae90b1a442b16a6b8e6610cf9106";
        public const string SourceMap2005HandoffDigest =
            "729920113fefff78e3e5584d34c3ce682f3017a545e3e3bf8ae0bbdcc8b20f55";
        public const string ExecutionState = "RequestOnly";
        public const string MissingDigest = "NONE";
    }

    public enum GeneratedReplayRequestedAction
    {
        AuthorReplayRequest = 0,
        OpenFailure = 1,
        OpenSource = 2,
        OpenInspector = 3,
        ExportSeedBundle = 4,
    }

    public enum GeneratedFailureSourceKind
    {
        ActualFailureBundle = 0,
        FocusedFixture = 1,
        MissingData = 2,
    }

    public enum GeneratedContentOrigin
    {
        FixedMap07 = 0,
        GeneratedMap16Slice = 1,
        GeneratedMap17Runtime = 2,
        GeneratedMap18Population = 3,
        MissingData = 4,
    }

    /// <summary>
    /// Immutable authoring intent derived from a MAP20_04 jump target. It deliberately exposes
    /// no execute method and its execution state is permanently RequestOnly.
    /// </summary>
    public sealed class GeneratedReplayAuthoringRequest
    {
        public const string SchemaVersion = "map20_05.replay_authoring_request.v1";

        public GeneratedReplayAuthoringRequest(string sourceMap2004HandoffDigest, int seed,
            string worldId, GeneratedNavigationCoordinate sectorCoordinate,
            GeneratedNavigationCoordinate cellCoordinate, string validationErrorId,
            GeneratedValidationJumpTargetKind jumpTargetKind, string selectionPath,
            string failureBundleId, string authoringContextDigest,
            GeneratedReplayRequestedAction requestedAction, string createdUtc)
        {
            SourceMap2004HandoffDigest = RequireDigest(sourceMap2004HandoffDigest,
                nameof(sourceMap2004HandoffDigest));
            if (!string.Equals(SourceMap2004HandoffDigest,
                GeneratedReplayAuthoringPreconditions.SourceMap2005HandoffDigest,
                StringComparison.Ordinal))
                throw new ArgumentException("MAP20_05 handoff digest mismatch.",
                    nameof(sourceMap2004HandoffDigest));
            Seed = seed;
            WorldId = Require(worldId, nameof(worldId));
            SectorCoordinate = sectorCoordinate ?? throw new ArgumentNullException(
                nameof(sectorCoordinate));
            CellCoordinate = cellCoordinate ?? throw new ArgumentNullException(
                nameof(cellCoordinate));
            ValidationErrorId = Require(validationErrorId, nameof(validationErrorId));
            JumpTargetKind = jumpTargetKind;
            SelectionPath = Require(selectionPath, nameof(selectionPath));
            FailureBundleId = Clean(failureBundleId);
            AuthoringContextDigest = RequireDigest(authoringContextDigest,
                nameof(authoringContextDigest));
            RequestedAction = requestedAction;
            CreatedUtc = createdUtc ?? string.Empty;
            RequestId = "REQ-" + BakingCanonicalDigest.HashCanonicalLines(
                StableLines(false)).Substring(0, 24);
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(StableLines(true));
        }

        public string SchemaVersionValue => SchemaVersion;
        public string TaskId => GeneratedReplayAuthoringPreconditions.TaskId;
        public string SourceMap2004HandoffDigest { get; }
        public string RequestId { get; }
        public int Seed { get; }
        public string WorldId { get; }
        public GeneratedNavigationCoordinate SectorCoordinate { get; }
        public GeneratedNavigationCoordinate CellCoordinate { get; }
        public string ValidationErrorId { get; }
        public GeneratedValidationJumpTargetKind JumpTargetKind { get; }
        public string SelectionPath { get; }
        public string FailureBundleId { get; }
        public string AuthoringContextDigest { get; }
        public GeneratedReplayRequestedAction RequestedAction { get; }
        public string ExecutionState => GeneratedReplayAuthoringPreconditions.ExecutionState;
        public string CreatedUtc { get; }
        public bool CreatedUtcExcludedFromCanonicalDigest => true;
        public string CanonicalDigest { get; }
        public int ReplayExecutionCount => 0;

        public static GeneratedReplayAuthoringRequest FromNavigationTarget(
            GeneratedValidationJumpTarget target, int seed, string worldId,
            string failureBundleId, GeneratedReplayRequestedAction requestedAction,
            string createdUtc)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            var sector = target.SectorCoordinate ?? new GeneratedNavigationCoordinate(0, 0);
            var cell = target.LocalCellCoordinate ?? new GeneratedNavigationCoordinate(0, 0);
            var contextDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP20_05_AUTHORING_CONTEXT_V1", target.ValidationErrorId,
                target.CanonicalDigest, target.SourceLocation.CsvFileDigest,
                target.SelectionPath,
            });
            return new GeneratedReplayAuthoringRequest(
                GeneratedReplayAuthoringPreconditions.SourceMap2005HandoffDigest,
                seed, worldId, sector, cell, target.ValidationErrorId,
                target.JumpTargetKind, target.SelectionPath, failureBundleId,
                contextDigest, requestedAction, createdUtc);
        }

        public string Serialize() => GeneratedReplayAuthoringJson.ToJson(
            GeneratedReplayAuthoringRequestDocument.From(this));

        internal string CanonicalLine => GeneratedReplayAuthoringCanonical.Join(
            RequestId, string.Join("|", StableLines(false)));

        private IEnumerable<string> StableLines(bool includeRequestId)
        {
            yield return SchemaVersion;
            yield return GeneratedReplayAuthoringPreconditions.TaskId;
            yield return SourceMap2004HandoffDigest;
            if (includeRequestId) yield return RequestId;
            yield return Seed.ToString(CultureInfo.InvariantCulture);
            yield return WorldId;
            yield return GeneratedReplayAuthoringCanonical.Coordinate(SectorCoordinate);
            yield return GeneratedReplayAuthoringCanonical.Coordinate(CellCoordinate);
            yield return ValidationErrorId;
            yield return JumpTargetKind.ToString();
            yield return SelectionPath;
            yield return FailureBundleId;
            yield return AuthoringContextDigest;
            yield return RequestedAction.ToString();
            yield return GeneratedReplayAuthoringPreconditions.ExecutionState;
            yield return "created_utc_excluded=true";
        }

        internal static string RequireDigest(string value, string parameterName)
        {
            if (!BakingCanonicalDigest.IsLowerHexSha256(value))
                throw new ArgumentException("Value must be lower-hex SHA-256.", parameterName);
            return value;
        }

        internal static string Require(string value, string parameterName)
        {
            var result = Clean(value);
            if (result.Length == 0) throw new ArgumentException("Value is required.", parameterName);
            return result;
        }

        internal static string Clean(string value) => value == null ? string.Empty : value.Trim();
    }

    public sealed class GeneratedFailureBrowserRecord
    {
        public GeneratedFailureBrowserRecord(string failureRecordId,
            GeneratedFailureSourceKind sourceKind, string sourcePath, string sourceDigest,
            string ownerTask, string failureCategory, string severity, int seed,
            GeneratedNavigationCoordinate sectorCoordinate,
            GeneratedNavigationCoordinate cellCoordinate, string validationErrorId,
            string navigationSelectionPath, string replayRequestId,
            bool actualFailureAvailable, string fixtureKind, string missingReason)
        {
            SourceKind = sourceKind;
            SourcePath = GeneratedReplayAuthoringRequest.Clean(sourcePath).Replace('\\', '/');
            SourceDigest = GeneratedReplayAuthoringRequest.Clean(sourceDigest);
            OwnerTask = GeneratedReplayAuthoringRequest.Require(ownerTask, nameof(ownerTask));
            FailureCategory = GeneratedReplayAuthoringRequest.Require(failureCategory,
                nameof(failureCategory));
            Severity = GeneratedReplayAuthoringRequest.Require(severity, nameof(severity));
            Seed = seed;
            SectorCoordinate = sectorCoordinate ?? throw new ArgumentNullException(
                nameof(sectorCoordinate));
            CellCoordinate = cellCoordinate ?? throw new ArgumentNullException(
                nameof(cellCoordinate));
            ValidationErrorId = GeneratedReplayAuthoringRequest.Require(validationErrorId,
                nameof(validationErrorId));
            NavigationSelectionPath = GeneratedReplayAuthoringRequest.Require(
                navigationSelectionPath, nameof(navigationSelectionPath));
            ReplayRequestId = GeneratedReplayAuthoringRequest.Require(replayRequestId,
                nameof(replayRequestId));
            ActualFailureAvailable = actualFailureAvailable;
            FixtureKind = GeneratedReplayAuthoringRequest.Clean(fixtureKind);
            MissingReason = GeneratedReplayAuthoringRequest.Clean(missingReason);
            Validate();
            var identityDigest = BakingCanonicalDigest.HashCanonicalLines(StableLines(false));
            FailureRecordId = string.IsNullOrWhiteSpace(failureRecordId)
                ? "FAILURE-" + identityDigest.Substring(0, 24)
                : failureRecordId.Trim();
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(StableLines(true));
        }

        public string FailureRecordId { get; }
        public GeneratedFailureSourceKind SourceKind { get; }
        public string SourcePath { get; }
        public string SourceDigest { get; }
        public string OwnerTask { get; }
        public string FailureCategory { get; }
        public string Severity { get; }
        public int Seed { get; }
        public GeneratedNavigationCoordinate SectorCoordinate { get; }
        public GeneratedNavigationCoordinate CellCoordinate { get; }
        public string ValidationErrorId { get; }
        public string NavigationSelectionPath { get; }
        public string ReplayRequestId { get; }
        public bool ActualFailureAvailable { get; }
        public string FixtureKind { get; }
        public string MissingReason { get; }
        public string CanonicalDigest { get; }

        internal string CanonicalLine => GeneratedReplayAuthoringCanonical.Join(
            FailureRecordId, CanonicalDigest);

        private void Validate()
        {
            if (SourceKind == GeneratedFailureSourceKind.ActualFailureBundle)
            {
                if (!ActualFailureAvailable || SourcePath.Length == 0)
                    throw new ArgumentException("Actual failure records require an available path.");
                GeneratedReplayAuthoringRequest.RequireDigest(SourceDigest, nameof(SourceDigest));
                if (FixtureKind.Length != 0 || MissingReason.Length != 0)
                    throw new ArgumentException("Actual failure records cannot be fixture/missing records.");
                return;
            }
            if (ActualFailureAvailable)
                throw new ArgumentException("Only ActualFailureBundle can be actual.");
            if (SourceKind == GeneratedFailureSourceKind.FocusedFixture)
            {
                if (FixtureKind.Length == 0)
                    throw new ArgumentException("FocusedFixture requires fixture_kind.");
                GeneratedReplayAuthoringRequest.RequireDigest(SourceDigest, nameof(SourceDigest));
                if (MissingReason.Length != 0)
                    throw new ArgumentException("FocusedFixture cannot have a missing reason.");
                return;
            }
            if (!string.Equals(SourceDigest, GeneratedReplayAuthoringPreconditions.MissingDigest,
                    StringComparison.Ordinal) || MissingReason.Length == 0)
                throw new ArgumentException("MissingData requires NONE digest and a reason.");
        }

        private IEnumerable<string> StableLines(bool includeId)
        {
            if (includeId) yield return FailureRecordId;
            yield return SourceKind.ToString();
            yield return SourcePath;
            yield return SourceDigest;
            yield return OwnerTask;
            yield return FailureCategory;
            yield return Severity;
            yield return Seed.ToString(CultureInfo.InvariantCulture);
            yield return GeneratedReplayAuthoringCanonical.Coordinate(SectorCoordinate);
            yield return GeneratedReplayAuthoringCanonical.Coordinate(CellCoordinate);
            yield return ValidationErrorId;
            yield return NavigationSelectionPath;
            yield return ReplayRequestId;
            yield return ActualFailureAvailable ? "1" : "0";
            yield return FixtureKind;
            yield return MissingReason;
        }
    }

    public sealed class GeneratedFailureBrowserIndex
    {
        public const string SchemaVersion = "map20_05.failure_browser_index.v1";
        private readonly ReadOnlyCollection<string> sourceKindTokens;
        private readonly ReadOnlyCollection<GeneratedFailureBrowserRecord> records;

        public GeneratedFailureBrowserIndex(IEnumerable<GeneratedFailureBrowserRecord> records,
            string createdUtc)
        {
            var ordered = (records ?? throw new ArgumentNullException(nameof(records)))
                .Where(value => value != null)
                .OrderBy(value => value.FailureRecordId, StringComparer.Ordinal)
                .ThenBy(value => value.CanonicalDigest, StringComparer.Ordinal).ToArray();
            if (ordered.Length == 0)
                throw new ArgumentException("At least one explicit failure state is required.",
                    nameof(records));
            if (ordered.GroupBy(value => value.FailureRecordId, StringComparer.Ordinal)
                .Any(group => group.Count() != 1))
                throw new ArgumentException("Failure record ids must be unique.", nameof(records));
            this.records = new ReadOnlyCollection<GeneratedFailureBrowserRecord>(ordered);
            sourceKindTokens = new ReadOnlyCollection<string>(Enum
                .GetValues(typeof(GeneratedFailureSourceKind)).Cast<GeneratedFailureSourceKind>()
                .OrderBy(value => (int)value).Select(value => value.ToString()).ToArray());
            CreatedUtc = createdUtc ?? string.Empty;
            CanonicalDigest = ComputeCanonicalDigest();
        }

        public IReadOnlyList<string> SourceKindTokens => sourceKindTokens;
        public IReadOnlyList<GeneratedFailureBrowserRecord> Records => records;
        public int ActualFailureCount => records.Count(value => value.SourceKind ==
            GeneratedFailureSourceKind.ActualFailureBundle && value.ActualFailureAvailable);
        public int FocusedFixtureCount => records.Count(value => value.SourceKind ==
            GeneratedFailureSourceKind.FocusedFixture);
        public int MissingDataCount => records.Count(value => value.SourceKind ==
            GeneratedFailureSourceKind.MissingData);
        public int RepairRerunReplayActionCount => 0;
        public string CreatedUtc { get; }
        public bool CreatedUtcExcludedFromCanonicalDigest => true;
        public string CanonicalDigest { get; }

        public IReadOnlyList<GeneratedFailureBrowserRecord> Filter(string search,
            GeneratedFailureSourceKind? sourceKind)
        {
            var query = records.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(value => value.FailureRecordId.IndexOf(search,
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                    value.FailureCategory.IndexOf(search,
                        StringComparison.OrdinalIgnoreCase) >= 0);
            if (sourceKind.HasValue)
                query = query.Where(value => value.SourceKind == sourceKind.Value);
            return new ReadOnlyCollection<GeneratedFailureBrowserRecord>(query.ToArray());
        }

        public string Serialize() => GeneratedReplayAuthoringJson.ToJson(
            new GeneratedFailureBrowserIndexDocument
            {
                schema_version = SchemaVersion,
                task_id = GeneratedReplayAuthoringPreconditions.TaskId,
                source_kind_tokens = sourceKindTokens.ToArray(),
                failure_records = records.Select(GeneratedFailureBrowserRecordDocument.From)
                    .ToArray(),
                actual_failure_records = ActualFailureCount,
                focused_fixture_failure_records = FocusedFixtureCount,
                missing_failure_records = MissingDataCount,
                repair_rerun_replay_action_count = RepairRerunReplayActionCount,
                canonical_digest = CanonicalDigest,
                created_utc = CreatedUtc,
                created_utc_excluded_from_canonical_digest = true,
            });

        private string ComputeCanonicalDigest()
        {
            var lines = new List<string>
            {
                SchemaVersion, GeneratedReplayAuthoringPreconditions.TaskId,
                string.Join("|", sourceKindTokens), "created_utc_excluded=true",
            };
            lines.AddRange(records.Select(value => "failure=" + value.CanonicalLine));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }
    }

    public sealed class GeneratedContentOriginRecord
    {
        public GeneratedContentOriginRecord(string recordId, GeneratedContentOrigin origin,
            string ownerTask, string sourceIdentity, string sourceDigest,
            string readOnlyReference, string missingReason)
        {
            RecordId = GeneratedReplayAuthoringRequest.Require(recordId, nameof(recordId));
            Origin = origin;
            OwnerTask = GeneratedReplayAuthoringRequest.Require(ownerTask, nameof(ownerTask));
            SourceIdentity = GeneratedReplayAuthoringRequest.Require(sourceIdentity,
                nameof(sourceIdentity));
            SourceDigest = GeneratedReplayAuthoringRequest.Clean(sourceDigest);
            ReadOnlyReference = GeneratedReplayAuthoringRequest.Require(readOnlyReference,
                nameof(readOnlyReference)).Replace('\\', '/');
            MissingReason = GeneratedReplayAuthoringRequest.Clean(missingReason);
            if (origin == GeneratedContentOrigin.MissingData)
            {
                if (!string.Equals(SourceDigest, GeneratedReplayAuthoringPreconditions.MissingDigest,
                        StringComparison.Ordinal) || MissingReason.Length == 0)
                    throw new ArgumentException("Missing content requires NONE digest and a reason.");
            }
            else
            {
                GeneratedReplayAuthoringRequest.RequireDigest(SourceDigest, nameof(sourceDigest));
                if (MissingReason.Length != 0)
                    throw new ArgumentException("Available content cannot have a missing reason.");
            }
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                RecordId, Origin.ToString(), OwnerTask, SourceIdentity, SourceDigest,
                ReadOnlyReference, MissingReason,
            });
        }

        public string RecordId { get; }
        public GeneratedContentOrigin Origin { get; }
        public string OwnerTask { get; }
        public string SourceIdentity { get; }
        public string SourceDigest { get; }
        public string ReadOnlyReference { get; }
        public string MissingReason { get; }
        public string CanonicalDigest { get; }
        internal string CanonicalLine => GeneratedReplayAuthoringCanonical.Join(
            RecordId, CanonicalDigest);
    }

    public sealed class GeneratedMap07FixedGeneratedSplit
    {
        private readonly ReadOnlyCollection<string> originTokens;
        private readonly ReadOnlyCollection<GeneratedContentOriginRecord> records;

        public GeneratedMap07FixedGeneratedSplit(IEnumerable<GeneratedContentOriginRecord> records)
        {
            var ordered = (records ?? throw new ArgumentNullException(nameof(records)))
                .Where(value => value != null).OrderBy(value => value.RecordId,
                    StringComparer.Ordinal).ToArray();
            if (ordered.Length == 0)
                throw new ArgumentException("Origin records are required.", nameof(records));
            this.records = new ReadOnlyCollection<GeneratedContentOriginRecord>(ordered);
            originTokens = new ReadOnlyCollection<string>(Enum.GetValues(
                    typeof(GeneratedContentOrigin)).Cast<GeneratedContentOrigin>()
                .OrderBy(value => (int)value).Select(value => value.ToString()).ToArray());
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP20_05_MAP07_FIXED_GENERATED_SPLIT_V1",
                string.Join("|", originTokens),
            }.Concat(ordered.Select(value => value.CanonicalLine)));
        }

        public IReadOnlyList<string> OriginTokens => originTokens;
        public IReadOnlyList<GeneratedContentOriginRecord> Records => records;
        public int FixedOriginCount => records.Count(value => value.Origin ==
            GeneratedContentOrigin.FixedMap07);
        public int GeneratedOriginCount => records.Count(value => value.Origin ==
            GeneratedContentOrigin.GeneratedMap16Slice || value.Origin ==
            GeneratedContentOrigin.GeneratedMap17Runtime || value.Origin ==
            GeneratedContentOrigin.GeneratedMap18Population);
        public int MissingOriginCount => records.Count(value => value.Origin ==
            GeneratedContentOrigin.MissingData);
        public int SourceFilesRewrittenCount => 0;
        public string CanonicalDigest { get; }

        public static GeneratedMap07FixedGeneratedSplit CreateDefault()
        {
            return new GeneratedMap07FixedGeneratedSplit(new[]
            {
                Origin("MAP07_FIXED", GeneratedContentOrigin.FixedMap07,
                    "MAP07", "MAP07/FIXED_CONTENT", "MAP07/fixed-content"),
                Origin("MAP16_SLICE", GeneratedContentOrigin.GeneratedMap16Slice,
                    "MAP16", "MAP16/GENERATED_SLICE", "MAP16/generated-slice"),
                Origin("MAP17_RUNTIME", GeneratedContentOrigin.GeneratedMap17Runtime,
                    "MAP17", "MAP17/GENERATED_RUNTIME", "MAP17/generated-runtime"),
                Origin("MAP18_POPULATION", GeneratedContentOrigin.GeneratedMap18Population,
                    "MAP18", "MAP18/GENERATED_POPULATION", "MAP18/generated-population"),
                new GeneratedContentOriginRecord("MISSING_OPTIONAL_CONTENT",
                    GeneratedContentOrigin.MissingData, "MAP20_05", "MISSING",
                    GeneratedReplayAuthoringPreconditions.MissingDigest,
                    "MissingData/optional-content",
                    "No optional generated content was supplied to the read-only sample."),
            });
        }

        private static GeneratedContentOriginRecord Origin(string recordId,
            GeneratedContentOrigin origin, string ownerTask, string sourceIdentity,
            string reference)
        {
            var digest = BakingCanonicalDigest.HashCanonicalLines(new[]
                { recordId, origin.ToString(), ownerTask, sourceIdentity, reference });
            return new GeneratedContentOriginRecord(recordId, origin, ownerTask,
                sourceIdentity, digest, reference, string.Empty);
        }
    }

    public sealed class GeneratedMap08BoundaryLink
    {
        public GeneratedMap08BoundaryLink(string pairId, string sourceBiome,
            string targetBiome, string candidateId, string projectionId, string socketId,
            GeneratedNavigationCoordinate sectorCoordinate,
            GeneratedNavigationCoordinate cellCoordinate, string sourceDigest,
            string linkState)
        {
            PairId = GeneratedReplayAuthoringRequest.Require(pairId, nameof(pairId));
            SourceBiome = GeneratedReplayAuthoringRequest.Require(sourceBiome,
                nameof(sourceBiome));
            TargetBiome = GeneratedReplayAuthoringRequest.Require(targetBiome,
                nameof(targetBiome));
            CandidateId = GeneratedReplayAuthoringRequest.Require(candidateId,
                nameof(candidateId));
            ProjectionId = GeneratedReplayAuthoringRequest.Require(projectionId,
                nameof(projectionId));
            SocketId = GeneratedReplayAuthoringRequest.Require(socketId, nameof(socketId));
            SectorCoordinate = sectorCoordinate ?? throw new ArgumentNullException(
                nameof(sectorCoordinate));
            CellCoordinate = cellCoordinate ?? throw new ArgumentNullException(
                nameof(cellCoordinate));
            SourceDigest = GeneratedReplayAuthoringRequest.RequireDigest(sourceDigest,
                nameof(sourceDigest));
            LinkState = GeneratedReplayAuthoringRequest.Require(linkState, nameof(linkState));
            BoundaryLinkId = "BOUNDARY-LINK-" + BakingCanonicalDigest.HashCanonicalLines(
                StableLines(false)).Substring(0, 20);
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(StableLines(true));
        }

        public string BoundaryLinkId { get; }
        public string PairId { get; }
        public string SourceBiome { get; }
        public string TargetBiome { get; }
        public string CandidateId { get; }
        public string ProjectionId { get; }
        public string SocketId { get; }
        public GeneratedNavigationCoordinate SectorCoordinate { get; }
        public GeneratedNavigationCoordinate CellCoordinate { get; }
        public string SourceDigest { get; }
        public string LinkState { get; }
        public string CanonicalDigest { get; }
        internal string CanonicalLine => GeneratedReplayAuthoringCanonical.Join(
            BoundaryLinkId, CanonicalDigest);

        private IEnumerable<string> StableLines(bool includeId)
        {
            if (includeId) yield return BoundaryLinkId;
            yield return PairId;
            yield return SourceBiome;
            yield return TargetBiome;
            yield return CandidateId;
            yield return ProjectionId;
            yield return SocketId;
            yield return GeneratedReplayAuthoringCanonical.Coordinate(SectorCoordinate);
            yield return GeneratedReplayAuthoringCanonical.Coordinate(CellCoordinate);
            yield return SourceDigest;
            yield return LinkState;
        }
    }

    public sealed class GeneratedMap08BoundaryLinkCatalog
    {
        private readonly ReadOnlyCollection<GeneratedMap08BoundaryLink> links;

        public GeneratedMap08BoundaryLinkCatalog(IEnumerable<GeneratedMap08BoundaryLink> links)
        {
            var ordered = (links ?? throw new ArgumentNullException(nameof(links)))
                .Where(value => value != null).OrderBy(value => value.PairId,
                    StringComparer.Ordinal).ThenBy(value => value.CandidateId,
                    StringComparer.Ordinal).ToArray();
            if (ordered.Length != 6 || ordered.Select(value => value.PairId)
                .Distinct(StringComparer.Ordinal).Count() != 6)
                throw new ArgumentException("All six MAP08 approved pairs are required.",
                    nameof(links));
            this.links = new ReadOnlyCollection<GeneratedMap08BoundaryLink>(ordered);
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MAP20_05_MAP08_BOUNDARY_LINKS_V1", "approved_pair_capacity=6" }
                .Concat(ordered.Select(value => value.CanonicalLine)));
        }

        public IReadOnlyList<GeneratedMap08BoundaryLink> Links => links;
        public int ApprovedPairCapacity => 6;
        public int BoundaryRegenerationExecutionCount => 0;
        public bool PdfFourPairSubsetUsedAsSourceOfTruth => false;
        public string CanonicalDigest { get; }

        public static GeneratedMap08BoundaryLinkCatalog CreateDefault()
        {
            var links = new[]
            {
                Link(MoonpalaceCraterRootBoundaryAuthoringContract.PairRuleId,
                    MoonpalaceCraterRootBoundaryAuthoringContract.BiomeAId,
                    MoonpalaceCraterRootBoundaryAuthoringContract.BiomeBId,
                    MoonpalaceCraterRootBoundaryAuthoringContract.CandidateIds[0],
                    MoonpalaceCraterRootBoundaryAuthoringContract.MicrochunkIds[0], 0),
                Link(MoonpalaceCraterMillBoundaryAuthoringContract.PairRuleId,
                    MoonpalaceCraterMillBoundaryAuthoringContract.BiomeAId,
                    MoonpalaceCraterMillBoundaryAuthoringContract.BiomeBId,
                    MoonpalaceCraterMillBoundaryAuthoringContract.CandidateIds[0],
                    MoonpalaceCraterMillBoundaryAuthoringContract.MicrochunkIds[0], 1),
                Link(MoonpalaceCraterDoughBoundaryAuthoringContract.PairRuleId,
                    MoonpalaceCraterDoughBoundaryAuthoringContract.BiomeAId,
                    MoonpalaceCraterDoughBoundaryAuthoringContract.BiomeBId,
                    MoonpalaceCraterDoughBoundaryAuthoringContract.CandidateIds[0],
                    MoonpalaceCraterDoughBoundaryAuthoringContract.MicrochunkIds[0], 2),
                Link(MoonpalaceRootMillBoundaryAuthoringContract.PairRuleId,
                    MoonpalaceRootMillBoundaryAuthoringContract.BiomeAId,
                    MoonpalaceRootMillBoundaryAuthoringContract.BiomeBId,
                    MoonpalaceRootMillBoundaryAuthoringContract.CandidateIds[0],
                    MoonpalaceRootMillBoundaryAuthoringContract.MicrochunkIds[0], 3),
                Link(MoonpalaceRootDoughBoundaryAuthoringContract.PairRuleId,
                    MoonpalaceRootDoughBoundaryAuthoringContract.BiomeAId,
                    MoonpalaceRootDoughBoundaryAuthoringContract.BiomeBId,
                    MoonpalaceRootDoughBoundaryAuthoringContract.CandidateIds[0],
                    MoonpalaceRootDoughBoundaryAuthoringContract.MicrochunkIds[0], 4),
                Link(MoonpalaceMillDoughBoundaryAuthoringContract.PairRuleId,
                    MoonpalaceMillDoughBoundaryAuthoringContract.BiomeAId,
                    MoonpalaceMillDoughBoundaryAuthoringContract.BiomeBId,
                    MoonpalaceMillDoughBoundaryAuthoringContract.CandidateIds[0],
                    MoonpalaceMillDoughBoundaryAuthoringContract.MicrochunkIds[0], 5),
            };
            return new GeneratedMap08BoundaryLinkCatalog(links);
        }

        private static GeneratedMap08BoundaryLink Link(string pairId, string sourceBiome,
            string targetBiome, string candidateId, string microchunkId, int offset)
        {
            var projectionId = "MAP08/" + pairId + "/" + microchunkId;
            var socketId = microchunkId + "/SOCK_L";
            var sourceDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
                { pairId, sourceBiome, targetBiome, candidateId, projectionId, socketId });
            return new GeneratedMap08BoundaryLink(pairId, sourceBiome, targetBiome,
                candidateId, projectionId, socketId,
                new GeneratedNavigationCoordinate(6 + offset % 3, 5 + offset / 3),
                new GeneratedNavigationCoordinate(24, 16), sourceDigest,
                "ReadOnlyReference");
        }
    }

    public sealed class GeneratedSeedBundleExportSample
    {
        public const string SchemaVersion = "map20_05.seed_bundle_export_sample.v1";

        public GeneratedSeedBundleExportSample(int seed, string worldId,
            GeneratedNavigationCoordinate sectorCoordinate, string generatorVersion,
            string passDigestSummary, string csvNavigationIndexDigest,
            string validationJumpSampleDigest, GeneratedFailureBrowserIndex failureBrowser,
            GeneratedReplayAuthoringRequest replayRequest,
            GeneratedRuntimeDebugHudState runtimeHud,
            GeneratedMap07FixedGeneratedSplit fixedGeneratedSplit,
            GeneratedMap08BoundaryLinkCatalog boundaryLinks, string createdUtc)
        {
            Seed = seed;
            WorldId = GeneratedReplayAuthoringRequest.Require(worldId, nameof(worldId));
            SectorCoordinate = sectorCoordinate ?? throw new ArgumentNullException(
                nameof(sectorCoordinate));
            GeneratorVersion = GeneratedReplayAuthoringRequest.Require(generatorVersion,
                nameof(generatorVersion));
            PassDigestSummary = GeneratedReplayAuthoringRequest.RequireDigest(passDigestSummary,
                nameof(passDigestSummary));
            CsvNavigationIndexDigest = GeneratedReplayAuthoringRequest.RequireDigest(
                csvNavigationIndexDigest, nameof(csvNavigationIndexDigest));
            ValidationJumpSampleDigest = GeneratedReplayAuthoringRequest.RequireDigest(
                validationJumpSampleDigest, nameof(validationJumpSampleDigest));
            FailureBrowser = failureBrowser ?? throw new ArgumentNullException(nameof(failureBrowser));
            ReplayRequest = replayRequest ?? throw new ArgumentNullException(nameof(replayRequest));
            RuntimeHud = runtimeHud ?? throw new ArgumentNullException(nameof(runtimeHud));
            FixedGeneratedSplit = fixedGeneratedSplit ?? throw new ArgumentNullException(
                nameof(fixedGeneratedSplit));
            BoundaryLinks = boundaryLinks ?? throw new ArgumentNullException(nameof(boundaryLinks));
            CreatedUtc = createdUtc ?? string.Empty;
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(StableLines());
        }

        public string TaskId => GeneratedReplayAuthoringPreconditions.TaskId;
        public string SourceMap2004ResultDigest =>
            GeneratedReplayAuthoringPreconditions.SourceMap2004ResultDigest;
        public string SourceMap2004TaskDigest =>
            GeneratedReplayAuthoringPreconditions.SourceMap2004TaskDigest;
        public string SourceMap2005HandoffDigest =>
            GeneratedReplayAuthoringPreconditions.SourceMap2005HandoffDigest;
        public int Seed { get; }
        public string WorldId { get; }
        public GeneratedNavigationCoordinate SectorCoordinate { get; }
        public string GeneratorVersion { get; }
        public string PassDigestSummary { get; }
        public string CsvNavigationIndexDigest { get; }
        public string ValidationJumpSampleDigest { get; }
        public GeneratedFailureBrowserIndex FailureBrowser { get; }
        public GeneratedReplayAuthoringRequest ReplayRequest { get; }
        public GeneratedRuntimeDebugHudState RuntimeHud { get; }
        public GeneratedMap07FixedGeneratedSplit FixedGeneratedSplit { get; }
        public GeneratedMap08BoundaryLinkCatalog BoundaryLinks { get; }
        public string CreatedUtc { get; }
        public bool CreatedUtcExcludedFromCanonicalDigest => true;
        public string CanonicalDigest { get; }
        public int ProductionSeedApprovalCount => 0;
        public int GeneratorValidatorReplayRollbackExecutionCount => 0;

        public string Serialize() => GeneratedReplayAuthoringJson.ToJson(
            GeneratedSeedBundleExportSampleDocument.From(this));

        private IEnumerable<string> StableLines()
        {
            yield return SchemaVersion;
            yield return GeneratedReplayAuthoringPreconditions.TaskId;
            yield return SourceMap2004ResultDigest;
            yield return SourceMap2004TaskDigest;
            yield return SourceMap2005HandoffDigest;
            yield return Seed.ToString(CultureInfo.InvariantCulture);
            yield return WorldId;
            yield return GeneratedReplayAuthoringCanonical.Coordinate(SectorCoordinate);
            yield return GeneratorVersion;
            yield return PassDigestSummary;
            yield return CsvNavigationIndexDigest;
            yield return ValidationJumpSampleDigest;
            yield return FailureBrowser.CanonicalDigest;
            yield return ReplayRequest.CanonicalDigest;
            yield return RuntimeHud.CanonicalDigest;
            yield return FixedGeneratedSplit.CanonicalDigest;
            yield return BoundaryLinks.CanonicalDigest;
            yield return "created_utc_excluded=true";
        }
    }

    internal static class GeneratedReplayAuthoringCanonical
    {
        public static string Join(params string[] values) => string.Join("|",
            (values ?? Array.Empty<string>()).Select(value => value ?? string.Empty));

        public static string Coordinate(GeneratedNavigationCoordinate value) => value == null
            ? "NONE"
            : value.X.ToString(CultureInfo.InvariantCulture) + "," +
              value.Y.ToString(CultureInfo.InvariantCulture);
    }

    internal static class GeneratedReplayAuthoringJson
    {
        public static string ToJson(object document) => JsonUtility.ToJson(document, true) + "\n";
    }

    [Serializable]
    internal sealed class GeneratedReplayAuthoringCoordinateDocument
    {
        public int x;
        public int y;
        public static GeneratedReplayAuthoringCoordinateDocument From(
            GeneratedNavigationCoordinate value) => value == null ? null :
            new GeneratedReplayAuthoringCoordinateDocument { x = value.X, y = value.Y };
    }

    [Serializable]
    internal sealed class GeneratedReplayAuthoringRequestDocument
    {
        public string schema_version;
        public string task_id;
        public string source_MAP20_04_handoff_digest;
        public string request_id;
        public int seed;
        public string world_id;
        public GeneratedReplayAuthoringCoordinateDocument sector_coordinate;
        public GeneratedReplayAuthoringCoordinateDocument cell_coordinate;
        public string validation_error_id;
        public string jump_target_kind;
        public string selection_path;
        public string failure_bundle_id;
        public string authoring_context_digest;
        public string requested_action;
        public string execution_state;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
        public string canonical_digest;

        public static GeneratedReplayAuthoringRequestDocument From(
            GeneratedReplayAuthoringRequest value) => new GeneratedReplayAuthoringRequestDocument
        {
            schema_version = GeneratedReplayAuthoringRequest.SchemaVersion,
            task_id = value.TaskId,
            source_MAP20_04_handoff_digest = value.SourceMap2004HandoffDigest,
            request_id = value.RequestId,
            seed = value.Seed,
            world_id = value.WorldId,
            sector_coordinate = GeneratedReplayAuthoringCoordinateDocument.From(
                value.SectorCoordinate),
            cell_coordinate = GeneratedReplayAuthoringCoordinateDocument.From(value.CellCoordinate),
            validation_error_id = value.ValidationErrorId,
            jump_target_kind = value.JumpTargetKind.ToString(),
            selection_path = value.SelectionPath,
            failure_bundle_id = value.FailureBundleId,
            authoring_context_digest = value.AuthoringContextDigest,
            requested_action = value.RequestedAction.ToString(),
            execution_state = value.ExecutionState,
            created_utc = value.CreatedUtc,
            created_utc_excluded_from_canonical_digest = true,
            canonical_digest = value.CanonicalDigest,
        };
    }

    [Serializable]
    internal sealed class GeneratedFailureBrowserRecordDocument
    {
        public string failure_record_id;
        public string source_kind;
        public string source_path;
        public string source_digest;
        public string owner_task;
        public string failure_category;
        public string severity;
        public int seed;
        public GeneratedReplayAuthoringCoordinateDocument sector_coordinate;
        public GeneratedReplayAuthoringCoordinateDocument cell_coordinate;
        public string validation_error_id;
        public string navigation_selection_path;
        public string replay_request_id;
        public bool actual_failure_available;
        public string fixture_kind;
        public string missing_reason;
        public string canonical_digest;

        public static GeneratedFailureBrowserRecordDocument From(
            GeneratedFailureBrowserRecord value) => new GeneratedFailureBrowserRecordDocument
        {
            failure_record_id = value.FailureRecordId,
            source_kind = value.SourceKind.ToString(),
            source_path = value.SourcePath,
            source_digest = value.SourceDigest,
            owner_task = value.OwnerTask,
            failure_category = value.FailureCategory,
            severity = value.Severity,
            seed = value.Seed,
            sector_coordinate = GeneratedReplayAuthoringCoordinateDocument.From(
                value.SectorCoordinate),
            cell_coordinate = GeneratedReplayAuthoringCoordinateDocument.From(value.CellCoordinate),
            validation_error_id = value.ValidationErrorId,
            navigation_selection_path = value.NavigationSelectionPath,
            replay_request_id = value.ReplayRequestId,
            actual_failure_available = value.ActualFailureAvailable,
            fixture_kind = value.FixtureKind,
            missing_reason = value.MissingReason,
            canonical_digest = value.CanonicalDigest,
        };
    }

    [Serializable]
    internal sealed class GeneratedFailureBrowserIndexDocument
    {
        public string schema_version;
        public string task_id;
        public string[] source_kind_tokens;
        public GeneratedFailureBrowserRecordDocument[] failure_records;
        public int actual_failure_records;
        public int focused_fixture_failure_records;
        public int missing_failure_records;
        public int repair_rerun_replay_action_count;
        public string canonical_digest;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
    }

    [Serializable]
    internal sealed class GeneratedContentOriginRecordDocument
    {
        public string record_id;
        public string content_origin;
        public string owner_task;
        public string source_identity;
        public string source_digest;
        public string read_only_reference;
        public string missing_reason;
        public string canonical_digest;

        public static GeneratedContentOriginRecordDocument From(
            GeneratedContentOriginRecord value) => new GeneratedContentOriginRecordDocument
        {
            record_id = value.RecordId,
            content_origin = value.Origin.ToString(),
            owner_task = value.OwnerTask,
            source_identity = value.SourceIdentity,
            source_digest = value.SourceDigest,
            read_only_reference = value.ReadOnlyReference,
            missing_reason = value.MissingReason,
            canonical_digest = value.CanonicalDigest,
        };
    }

    [Serializable]
    internal sealed class GeneratedMap08BoundaryLinkDocument
    {
        public string boundary_link_id;
        public string pair_id;
        public string source_biome;
        public string target_biome;
        public string candidate_id;
        public string projection_id;
        public string socket_id;
        public GeneratedReplayAuthoringCoordinateDocument sector_coordinate;
        public GeneratedReplayAuthoringCoordinateDocument cell_coordinate;
        public string source_digest;
        public string link_state;
        public string canonical_digest;

        public static GeneratedMap08BoundaryLinkDocument From(GeneratedMap08BoundaryLink value) =>
            new GeneratedMap08BoundaryLinkDocument
            {
                boundary_link_id = value.BoundaryLinkId,
                pair_id = value.PairId,
                source_biome = value.SourceBiome,
                target_biome = value.TargetBiome,
                candidate_id = value.CandidateId,
                projection_id = value.ProjectionId,
                socket_id = value.SocketId,
                sector_coordinate = GeneratedReplayAuthoringCoordinateDocument.From(
                    value.SectorCoordinate),
                cell_coordinate = GeneratedReplayAuthoringCoordinateDocument.From(
                    value.CellCoordinate),
                source_digest = value.SourceDigest,
                link_state = value.LinkState,
                canonical_digest = value.CanonicalDigest,
            };
    }

    [Serializable]
    internal sealed class GeneratedSeedBundleExportSampleDocument
    {
        public string schema_version;
        public string task_id;
        public string source_MAP20_04_result_digest;
        public string source_MAP20_04_task_digest;
        public string source_MAP20_05_handoff_digest;
        public int seed;
        public string world_id;
        public GeneratedReplayAuthoringCoordinateDocument sector_coordinate;
        public string generator_version;
        public string pass_digest_summary;
        public string csv_navigation_index_digest;
        public string validation_jump_sample_digest;
        public string failure_browser_digest;
        public string replay_request_digest;
        public string runtime_hud_digest;
        public string map07_fixed_generated_split_digest;
        public string map08_boundary_link_digest;
        public string[] fixed_generated_origin_tokens;
        public GeneratedContentOriginRecordDocument[] fixed_generated_origin_records;
        public int map08_approved_pair_capacity;
        public GeneratedMap08BoundaryLinkDocument[] map08_boundary_links;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
        public string canonical_digest;

        public static GeneratedSeedBundleExportSampleDocument From(
            GeneratedSeedBundleExportSample value) => new GeneratedSeedBundleExportSampleDocument
        {
            schema_version = GeneratedSeedBundleExportSample.SchemaVersion,
            task_id = value.TaskId,
            source_MAP20_04_result_digest = value.SourceMap2004ResultDigest,
            source_MAP20_04_task_digest = value.SourceMap2004TaskDigest,
            source_MAP20_05_handoff_digest = value.SourceMap2005HandoffDigest,
            seed = value.Seed,
            world_id = value.WorldId,
            sector_coordinate = GeneratedReplayAuthoringCoordinateDocument.From(
                value.SectorCoordinate),
            generator_version = value.GeneratorVersion,
            pass_digest_summary = value.PassDigestSummary,
            csv_navigation_index_digest = value.CsvNavigationIndexDigest,
            validation_jump_sample_digest = value.ValidationJumpSampleDigest,
            failure_browser_digest = value.FailureBrowser.CanonicalDigest,
            replay_request_digest = value.ReplayRequest.CanonicalDigest,
            runtime_hud_digest = value.RuntimeHud.CanonicalDigest,
            map07_fixed_generated_split_digest = value.FixedGeneratedSplit.CanonicalDigest,
            map08_boundary_link_digest = value.BoundaryLinks.CanonicalDigest,
            fixed_generated_origin_tokens = value.FixedGeneratedSplit.OriginTokens.ToArray(),
            fixed_generated_origin_records = value.FixedGeneratedSplit.Records.Select(
                GeneratedContentOriginRecordDocument.From).ToArray(),
            map08_approved_pair_capacity = value.BoundaryLinks.ApprovedPairCapacity,
            map08_boundary_links = value.BoundaryLinks.Links.Select(
                GeneratedMap08BoundaryLinkDocument.From).ToArray(),
            created_utc = value.CreatedUtc,
            created_utc_excluded_from_canonical_digest = true,
            canonical_digest = value.CanonicalDigest,
        };
    }
}
