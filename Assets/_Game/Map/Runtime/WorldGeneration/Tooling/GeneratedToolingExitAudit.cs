using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.Tooling
{
    public static class GeneratedToolingExitAuditPreconditions
    {
        public const string TaskId = "MAP20_06_MAP20_TOOLING_EXIT_TESTS";
        public const string SourceMap2005ResultDigest =
            "b2d74b83ce7f700ed9c726403576d5087ecb82d80be9bcef29f9c9ad0481f6bf";
        public const string SourceMap2005TaskDigest =
            "a8688dd6756ab7a750a6334ffbe60dafc5f50a5a7e0632b8e12a647dc445c459";
        public const string SourceMap2006HandoffDigest =
            "34d7e10a208547780d86c7aa985c4945d76956e8e9869a266d12a1cda27b13fb";
        public const string MissingData = "MissingData";
    }

    public sealed class GeneratedToolingSourceDigestRecord
    {
        public GeneratedToolingSourceDigestRecord(string ownerTask, string relativePath,
            string canonicalDigest, string physicalFileDigest)
        {
            OwnerTask = GeneratedToolingExitAuditValidation.Require(ownerTask,
                nameof(ownerTask));
            RelativePath = GeneratedToolingExitAuditValidation.Path(relativePath,
                nameof(relativePath));
            CanonicalDigest = GeneratedToolingExitAuditValidation.Digest(canonicalDigest,
                nameof(canonicalDigest));
            PhysicalFileDigest = GeneratedToolingExitAuditValidation.Digest(physicalFileDigest,
                nameof(physicalFileDigest));
        }

        public string OwnerTask { get; }
        public string RelativePath { get; }
        public string CanonicalDigest { get; }
        public string PhysicalFileDigest { get; }
        public bool ReadOnly => true;
        public string CanonicalLine => GeneratedInspectionCanonical.Join(OwnerTask,
            RelativePath, CanonicalDigest, PhysicalFileDigest, "ReadOnly");
    }

    public sealed class GeneratedCoordinateAuditRecord
    {
        public GeneratedCoordinateAuditRecord(string traceId, string ownerTask,
            string seedContext, string worldCoordinate, string sectorCoordinate,
            string cellCoordinate, string localCoordinate, string sourceLocation,
            string selectionPath, bool missingDataIsolated, string missingReason,
            string evidenceDigest)
        {
            TraceId = GeneratedToolingExitAuditValidation.Require(traceId, nameof(traceId));
            OwnerTask = GeneratedToolingExitAuditValidation.Require(ownerTask,
                nameof(ownerTask));
            SeedContext = GeneratedToolingExitAuditValidation.Require(seedContext,
                nameof(seedContext));
            WorldCoordinate = GeneratedToolingExitAuditValidation.Require(worldCoordinate,
                nameof(worldCoordinate));
            SectorCoordinate = GeneratedToolingExitAuditValidation.Require(sectorCoordinate,
                nameof(sectorCoordinate));
            CellCoordinate = GeneratedToolingExitAuditValidation.Require(cellCoordinate,
                nameof(cellCoordinate));
            LocalCoordinate = GeneratedToolingExitAuditValidation.Require(localCoordinate,
                nameof(localCoordinate));
            SourceLocation = GeneratedToolingExitAuditValidation.Require(sourceLocation,
                nameof(sourceLocation));
            SelectionPath = GeneratedToolingExitAuditValidation.Require(selectionPath,
                nameof(selectionPath));
            MissingDataIsolated = missingDataIsolated;
            MissingReason = GeneratedToolingExitAuditValidation.Clean(missingReason);
            if (MissingDataIsolated && MissingReason.Length == 0)
                throw new ArgumentException("Isolated MissingData requires a reason.",
                    nameof(missingReason));
            if (!MissingDataIsolated && MissingReason.Length != 0)
                throw new ArgumentException("Available coordinate traces cannot have a missing reason.",
                    nameof(missingReason));
            EvidenceDigest = GeneratedToolingExitAuditValidation.Digest(evidenceDigest,
                nameof(evidenceDigest));
        }

        public string TraceId { get; }
        public string OwnerTask { get; }
        public string SeedContext { get; }
        public string WorldCoordinate { get; }
        public string SectorCoordinate { get; }
        public string CellCoordinate { get; }
        public string LocalCoordinate { get; }
        public string SourceLocation { get; }
        public string SelectionPath { get; }
        public bool MissingDataIsolated { get; }
        public string MissingReason { get; }
        public string EvidenceDigest { get; }
        public string CanonicalLine => GeneratedInspectionCanonical.Join(TraceId, OwnerTask,
            SeedContext, WorldCoordinate, SectorCoordinate, CellCoordinate, LocalCoordinate,
            SourceLocation, SelectionPath, MissingDataIsolated ? "1" : "0", MissingReason,
            EvidenceDigest);
    }

    public sealed class GeneratedRollbackScopeAuditRecord
    {
        public GeneratedRollbackScopeAuditRecord(string scopeToken, string boundaryToken,
            int boundedTargetCount, int maximumTargetCount, bool explicitWorldConfirmation,
            string evidenceDigest)
        {
            ScopeToken = GeneratedToolingExitAuditValidation.Require(scopeToken,
                nameof(scopeToken));
            BoundaryToken = GeneratedToolingExitAuditValidation.Require(boundaryToken,
                nameof(boundaryToken));
            if (boundedTargetCount < 1 || maximumTargetCount < boundedTargetCount)
                throw new ArgumentOutOfRangeException(nameof(boundedTargetCount));
            BoundedTargetCount = boundedTargetCount;
            MaximumTargetCount = maximumTargetCount;
            ExplicitWorldConfirmation = explicitWorldConfirmation;
            EvidenceDigest = GeneratedToolingExitAuditValidation.Digest(evidenceDigest,
                nameof(evidenceDigest));
        }

        public string ScopeToken { get; }
        public string BoundaryToken { get; }
        public int BoundedTargetCount { get; }
        public int MaximumTargetCount { get; }
        public bool ExplicitWorldConfirmation { get; }
        public bool WholeProjectExpansionAllowed => false;
        public string AuditState => "ReadOnlyRequest";
        public int RollbackExecutionCount => 0;
        public string EvidenceDigest { get; }
        public string CanonicalLine => GeneratedInspectionCanonical.Join(ScopeToken,
            BoundaryToken, BoundedTargetCount.ToString(CultureInfo.InvariantCulture),
            MaximumTargetCount.ToString(CultureInfo.InvariantCulture),
            ExplicitWorldConfirmation ? "1" : "0", "NoProjectExpansion", "ReadOnlyRequest",
            "rollback_execution_count=0", EvidenceDigest);
    }

    public sealed class GeneratedSourceNavigationAuditRecord
    {
        public GeneratedSourceNavigationAuditRecord(string validationErrorId, string ownerTask,
            string jumpTargetKind, string csvFilePath, int rowNumber1Based,
            int columnNumber1Based, string columnName, string fieldName, string recordId,
            string selectionPath, bool sourceAvailable, string missingReason,
            string evidenceDigest)
        {
            ValidationErrorId = GeneratedToolingExitAuditValidation.Require(validationErrorId,
                nameof(validationErrorId));
            OwnerTask = GeneratedToolingExitAuditValidation.Require(ownerTask,
                nameof(ownerTask));
            JumpTargetKind = GeneratedToolingExitAuditValidation.Require(jumpTargetKind,
                nameof(jumpTargetKind));
            CsvFilePath = GeneratedToolingExitAuditValidation.Clean(csvFilePath)
                .Replace('\\', '/');
            RowNumber1Based = rowNumber1Based;
            ColumnNumber1Based = columnNumber1Based;
            ColumnName = GeneratedToolingExitAuditValidation.Clean(columnName);
            FieldName = GeneratedToolingExitAuditValidation.Clean(fieldName);
            RecordId = GeneratedToolingExitAuditValidation.Require(recordId, nameof(recordId));
            SelectionPath = GeneratedToolingExitAuditValidation.Require(selectionPath,
                nameof(selectionPath)).Replace('\\', '/');
            SourceAvailable = sourceAvailable;
            MissingReason = GeneratedToolingExitAuditValidation.Clean(missingReason);
            if (SourceAvailable)
            {
                if (CsvFilePath.Length == 0 || RowNumber1Based < 1 ||
                    ColumnNumber1Based < 1 || ColumnName.Length == 0 || FieldName.Length == 0)
                    throw new ArgumentException("Available navigation requires an exact source location.");
                if (MissingReason.Length != 0)
                    throw new ArgumentException("Available navigation cannot have a missing reason.");
            }
            else if (RowNumber1Based != 0 || ColumnNumber1Based != 0 ||
                     MissingReason.Length == 0)
            {
                throw new ArgumentException("Missing navigation must use zero coordinates and a reason.");
            }
            EvidenceDigest = GeneratedToolingExitAuditValidation.Digest(evidenceDigest,
                nameof(evidenceDigest));
        }

        public string ValidationErrorId { get; }
        public string OwnerTask { get; }
        public string JumpTargetKind { get; }
        public string CsvFilePath { get; }
        public int RowNumber1Based { get; }
        public int ColumnNumber1Based { get; }
        public string ColumnName { get; }
        public string FieldName { get; }
        public string RecordId { get; }
        public string SelectionPath { get; }
        public bool SourceAvailable { get; }
        public string MissingReason { get; }
        public string EvidenceDigest { get; }
        public string CanonicalLine => GeneratedInspectionCanonical.Join(ValidationErrorId,
            OwnerTask, JumpTargetKind, CsvFilePath,
            RowNumber1Based.ToString(CultureInfo.InvariantCulture),
            ColumnNumber1Based.ToString(CultureInfo.InvariantCulture), ColumnName, FieldName,
            RecordId, SelectionPath, SourceAvailable ? "1" : "0", MissingReason,
            EvidenceDigest);
    }

    public sealed class GeneratedReplayHashAuditRecord
    {
        public GeneratedReplayHashAuditRecord(string recordId, string ownerTask,
            string executionState, string canonicalDigest,
            bool repeatStable, bool cultureStable, bool inputOrderStable)
        {
            RecordId = GeneratedToolingExitAuditValidation.Require(recordId, nameof(recordId));
            OwnerTask = GeneratedToolingExitAuditValidation.Require(ownerTask,
                nameof(ownerTask));
            ExecutionState = GeneratedToolingExitAuditValidation.Require(executionState,
                nameof(executionState));
            CanonicalDigest = GeneratedToolingExitAuditValidation.Digest(canonicalDigest,
                nameof(canonicalDigest));
            RepeatStable = repeatStable;
            CultureStable = cultureStable;
            InputOrderStable = inputOrderStable;
            if (!RepeatStable || !CultureStable || !InputOrderStable)
                throw new ArgumentException("Published replay hash evidence must be deterministic.");
        }

        public string RecordId { get; }
        public string OwnerTask { get; }
        public string ExecutionState { get; }
        public string CanonicalDigest { get; }
        public bool RepeatStable { get; }
        public bool CultureStable { get; }
        public bool InputOrderStable { get; }
        public int ReplayExecutionCount => 0;
        public string CanonicalLine => GeneratedInspectionCanonical.Join(RecordId, OwnerTask,
            ExecutionState, CanonicalDigest, RepeatStable ? "1" : "0",
            CultureStable ? "1" : "0", InputOrderStable ? "1" : "0",
            "replay_execution_count=0");
    }

    public sealed class GeneratedHudStateAuditRecord
    {
        public GeneratedHudStateAuditRecord(string hudStateId, string selectedValidationErrorId,
            string selectedFailureRecordId, string selectedSelectionPath,
            string canonicalDigest)
        {
            HudStateId = GeneratedToolingExitAuditValidation.Require(hudStateId,
                nameof(hudStateId));
            SelectedValidationErrorId = GeneratedToolingExitAuditValidation.Require(
                selectedValidationErrorId, nameof(selectedValidationErrorId));
            SelectedFailureRecordId = GeneratedToolingExitAuditValidation.Require(
                selectedFailureRecordId, nameof(selectedFailureRecordId));
            SelectedSelectionPath = GeneratedToolingExitAuditValidation.Require(
                selectedSelectionPath, nameof(selectedSelectionPath));
            CanonicalDigest = GeneratedToolingExitAuditValidation.Digest(canonicalDigest,
                nameof(canonicalDigest));
        }

        public string HudStateId { get; }
        public string SelectedValidationErrorId { get; }
        public string SelectedFailureRecordId { get; }
        public string SelectedSelectionPath { get; }
        public string StateKind => "DisplayOnly";
        public int AutoSpawnPathCount => 0;
        public int RuntimeMutationCount => 0;
        public int ScenePrefabWiringCount => 0;
        public string CanonicalDigest { get; }
        public string CanonicalLine => GeneratedInspectionCanonical.Join(HudStateId,
            SelectedValidationErrorId, SelectedFailureRecordId, SelectedSelectionPath,
            StateKind, "auto_spawn=0", "runtime_mutation=0", "scene_prefab_wiring=0",
            CanonicalDigest);
    }

    public sealed class GeneratedToolingAccessPathRecord
    {
        private readonly ReadOnlyCollection<string> actions;

        public GeneratedToolingAccessPathRecord(string accessPathId, string entrySurface,
            string targetSurface, IEnumerable<string> actions, string ownerTask,
            string evidenceDigest)
        {
            AccessPathId = GeneratedToolingExitAuditValidation.Require(accessPathId,
                nameof(accessPathId));
            EntrySurface = GeneratedToolingExitAuditValidation.Require(entrySurface,
                nameof(entrySurface));
            TargetSurface = GeneratedToolingExitAuditValidation.Require(targetSurface,
                nameof(targetSurface));
            var values = (actions ?? throw new ArgumentNullException(nameof(actions)))
                .Select(value => GeneratedToolingExitAuditValidation.Require(value,
                    nameof(actions))).ToArray();
            if (values.Length < 1 || values.Length > 3)
                throw new ArgumentOutOfRangeException(nameof(actions),
                    "Access paths must contain one to three user actions.");
            this.actions = new ReadOnlyCollection<string>(values);
            OwnerTask = GeneratedToolingExitAuditValidation.Require(ownerTask,
                nameof(ownerTask));
            EvidenceDigest = GeneratedToolingExitAuditValidation.Digest(evidenceDigest,
                nameof(evidenceDigest));
        }

        public string AccessPathId { get; }
        public string EntrySurface { get; }
        public string TargetSurface { get; }
        public int UserActionCount => actions.Count;
        public IReadOnlyList<string> Actions => actions;
        public string OwnerTask { get; }
        public string EvidenceDigest { get; }
        public bool PassesThreeClickLimit => UserActionCount <= 3;
        public string CanonicalLine => GeneratedInspectionCanonical.Join(AccessPathId,
            EntrySurface, TargetSurface, UserActionCount.ToString(CultureInfo.InvariantCulture),
            string.Join("|", actions), OwnerTask, EvidenceDigest,
            PassesThreeClickLimit ? "PASS" : "FAIL");
    }

    public sealed class GeneratedExitInvariantRecord
    {
        public GeneratedExitInvariantRecord(string invariantId, string status,
            string evidenceSource, string evidenceDigest, string ownerTask)
        {
            InvariantId = GeneratedToolingExitAuditValidation.Require(invariantId,
                nameof(invariantId));
            Status = GeneratedToolingExitAuditValidation.Require(status, nameof(status));
            if (!string.Equals(Status, "PASS", StringComparison.Ordinal))
                throw new ArgumentException("Exit audit publication accepts PASS evidence only.",
                    nameof(status));
            EvidenceSource = GeneratedToolingExitAuditValidation.Require(evidenceSource,
                nameof(evidenceSource));
            EvidenceDigest = GeneratedToolingExitAuditValidation.Digest(evidenceDigest,
                nameof(evidenceDigest));
            OwnerTask = GeneratedToolingExitAuditValidation.Require(ownerTask,
                nameof(ownerTask));
        }

        public string InvariantId { get; }
        public string Status { get; }
        public string EvidenceSource { get; }
        public string EvidenceDigest { get; }
        public string OwnerTask { get; }
        public string CanonicalLine => GeneratedInspectionCanonical.Join(InvariantId, Status,
            EvidenceSource, EvidenceDigest, OwnerTask);
    }

    /// <summary>Immutable read-only approval snapshot over existing MAP20_01 through MAP20_05 output.</summary>
    public sealed class GeneratedToolingExitAudit
    {
        public const string SchemaVersion = "map20_06.tooling_exit_audit.v1";

        private readonly ReadOnlyCollection<GeneratedToolingSourceDigestRecord> map20ResultChain;
        private readonly ReadOnlyCollection<GeneratedToolingSourceDigestRecord> map20ArtifactChain;
        private readonly ReadOnlyCollection<GeneratedCoordinateAuditRecord> coordinateRecords;
        private readonly ReadOnlyCollection<GeneratedRollbackScopeAuditRecord> rollbackRecords;
        private readonly ReadOnlyCollection<GeneratedSourceNavigationAuditRecord> navigationRecords;
        private readonly ReadOnlyCollection<GeneratedReplayHashAuditRecord> replayRecords;
        private readonly ReadOnlyCollection<GeneratedHudStateAuditRecord> hudRecords;
        private readonly ReadOnlyCollection<GeneratedToolingAccessPathRecord> accessRecords;
        private readonly ReadOnlyCollection<GeneratedExitInvariantRecord> invariantRecords;

        public GeneratedToolingExitAudit(string sourceMap2005ResultDigest,
            string sourceMap2005TaskDigest, string sourceMap2006HandoffDigest,
            IEnumerable<GeneratedToolingSourceDigestRecord> resultChain,
            IEnumerable<GeneratedToolingSourceDigestRecord> artifactChain,
            IEnumerable<GeneratedCoordinateAuditRecord> coordinates,
            IEnumerable<GeneratedRollbackScopeAuditRecord> rollbackScopes,
            IEnumerable<GeneratedSourceNavigationAuditRecord> sourceNavigation,
            IEnumerable<GeneratedReplayHashAuditRecord> replayHashes,
            IEnumerable<GeneratedHudStateAuditRecord> hudStates,
            IEnumerable<GeneratedToolingAccessPathRecord> accessPaths,
            IEnumerable<GeneratedExitInvariantRecord> invariants, string createdUtc)
        {
            SourceMap2005ResultDigest = GeneratedToolingExitAuditValidation.Digest(
                sourceMap2005ResultDigest, nameof(sourceMap2005ResultDigest));
            SourceMap2005TaskDigest = GeneratedToolingExitAuditValidation.Digest(
                sourceMap2005TaskDigest, nameof(sourceMap2005TaskDigest));
            SourceMap2006HandoffDigest = GeneratedToolingExitAuditValidation.Digest(
                sourceMap2006HandoffDigest, nameof(sourceMap2006HandoffDigest));
            if (!string.Equals(SourceMap2005ResultDigest,
                    GeneratedToolingExitAuditPreconditions.SourceMap2005ResultDigest,
                    StringComparison.Ordinal) ||
                !string.Equals(SourceMap2005TaskDigest,
                    GeneratedToolingExitAuditPreconditions.SourceMap2005TaskDigest,
                    StringComparison.Ordinal) ||
                !string.Equals(SourceMap2006HandoffDigest,
                    GeneratedToolingExitAuditPreconditions.SourceMap2006HandoffDigest,
                    StringComparison.Ordinal))
                throw new ArgumentException("MAP20_06 source precondition digest mismatch.");

            map20ResultChain = Ordered(resultChain, value => value.CanonicalLine, 5,
                nameof(resultChain));
            map20ArtifactChain = Ordered(artifactChain, value => value.CanonicalLine, 1,
                nameof(artifactChain));
            coordinateRecords = Ordered(coordinates, value => value.CanonicalLine, 1,
                nameof(coordinates));
            rollbackRecords = Ordered(rollbackScopes, value => value.CanonicalLine, 4,
                nameof(rollbackScopes));
            navigationRecords = Ordered(sourceNavigation, value => value.CanonicalLine, 5,
                nameof(sourceNavigation));
            replayRecords = Ordered(replayHashes, value => value.CanonicalLine, 4,
                nameof(replayHashes));
            hudRecords = Ordered(hudStates, value => value.CanonicalLine, 1,
                nameof(hudStates));
            accessRecords = Ordered(accessPaths, value => value.CanonicalLine, 10,
                nameof(accessPaths));
            invariantRecords = Ordered(invariants, value => value.CanonicalLine, 12,
                nameof(invariants));
            if (invariantRecords.Count != 12 || invariantRecords.Any(value =>
                    !string.Equals(value.Status, "PASS", StringComparison.Ordinal)))
                throw new ArgumentException("Exactly twelve PASS exit invariants are required.",
                    nameof(invariants));
            CreatedUtc = createdUtc ?? string.Empty;
            CanonicalDigest = ComputeCanonicalDigest();
        }

        public string SourceMap2005ResultDigest { get; }
        public string SourceMap2005TaskDigest { get; }
        public string SourceMap2006HandoffDigest { get; }
        public IReadOnlyList<GeneratedToolingSourceDigestRecord> Map20ResultChain => map20ResultChain;
        public IReadOnlyList<GeneratedToolingSourceDigestRecord> Map20GeneratedArtifactChain => map20ArtifactChain;
        public IReadOnlyList<GeneratedCoordinateAuditRecord> CoordinateAuditRecords => coordinateRecords;
        public IReadOnlyList<GeneratedRollbackScopeAuditRecord> RollbackScopeRecords => rollbackRecords;
        public IReadOnlyList<GeneratedSourceNavigationAuditRecord> SourceNavigationRecords => navigationRecords;
        public IReadOnlyList<GeneratedReplayHashAuditRecord> ReplayHashRecords => replayRecords;
        public IReadOnlyList<GeneratedHudStateAuditRecord> HudStateRecords => hudRecords;
        public IReadOnlyList<GeneratedToolingAccessPathRecord> AccessPathRecords => accessRecords;
        public IReadOnlyList<GeneratedExitInvariantRecord> ExitInvariantRecords => invariantRecords;
        public string CreatedUtc { get; }
        public bool CreatedUtcExcludedFromCanonicalDigest => true;
        public string CanonicalDigest { get; }
        public int UnsafeActionButtonCount => 0;
        public int GeneratorExecutionCount => 0;
        public int ValidationRunnerExecutionCount => 0;
        public int ReplayExecutionCount => 0;
        public int RollbackExecutionCount => 0;
        public int CsvAuthoringWriteCount => 0;
        public int RuntimeObjectSpawnCount => 0;

        public string Serialize() => GeneratedInspectionCanonical.ToJson(
            GeneratedToolingExitAuditDocument.From(this));

        private string ComputeCanonicalDigest()
        {
            var lines = new List<string>
            {
                SchemaVersion, GeneratedToolingExitAuditPreconditions.TaskId,
                SourceMap2005ResultDigest, SourceMap2005TaskDigest,
                SourceMap2006HandoffDigest, "created_utc_excluded=true",
            };
            lines.AddRange(map20ResultChain.Select(value => "result=" + value.CanonicalLine));
            lines.AddRange(map20ArtifactChain.Select(value => "artifact=" + value.CanonicalLine));
            lines.AddRange(coordinateRecords.Select(value => "coordinate=" + value.CanonicalLine));
            lines.AddRange(rollbackRecords.Select(value => "rollback=" + value.CanonicalLine));
            lines.AddRange(navigationRecords.Select(value => "navigation=" + value.CanonicalLine));
            lines.AddRange(replayRecords.Select(value => "replay=" + value.CanonicalLine));
            lines.AddRange(hudRecords.Select(value => "hud=" + value.CanonicalLine));
            lines.AddRange(accessRecords.Select(value => "access=" + value.CanonicalLine));
            lines.AddRange(invariantRecords.Select(value => "invariant=" + value.CanonicalLine));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        private static ReadOnlyCollection<T> Ordered<T>(IEnumerable<T> values,
            Func<T, string> key, int minimumCount, string parameterName) where T : class
        {
            var ordered = (values ?? throw new ArgumentNullException(parameterName))
                .Where(value => value != null).OrderBy(key, StringComparer.Ordinal).ToArray();
            if (ordered.Length < minimumCount)
                throw new ArgumentException("Required audit evidence is missing.", parameterName);
            return new ReadOnlyCollection<T>(ordered);
        }
    }

    public sealed class GeneratedToolingAccessPathAudit
    {
        public const string SchemaVersion = "map20_06.access_path_audit.v1";
        private readonly ReadOnlyCollection<GeneratedToolingAccessPathRecord> records;

        public GeneratedToolingAccessPathAudit(IEnumerable<GeneratedToolingAccessPathRecord> values,
            string createdUtc)
        {
            records = new ReadOnlyCollection<GeneratedToolingAccessPathRecord>((values ??
                throw new ArgumentNullException(nameof(values))).Where(value => value != null)
                .OrderBy(value => value.AccessPathId, StringComparer.Ordinal).ToArray());
            if (records.Count != 10 || records.Any(value => !value.PassesThreeClickLimit))
                throw new ArgumentException("Exactly ten passing three-click paths are required.",
                    nameof(values));
            CreatedUtc = createdUtc ?? string.Empty;
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
                {
                    SchemaVersion, GeneratedToolingExitAuditPreconditions.TaskId,
                    GeneratedToolingExitAuditPreconditions.SourceMap2006HandoffDigest,
                    "created_utc_excluded=true",
                }.Concat(records.Select(value => value.CanonicalLine)));
        }

        public IReadOnlyList<GeneratedToolingAccessPathRecord> AccessPathRecords => records;
        public int MaximumUserActionCount => records.Max(value => value.UserActionCount);
        public string CreatedUtc { get; }
        public bool CreatedUtcExcludedFromCanonicalDigest => true;
        public string CanonicalDigest { get; }
        public string Serialize() => GeneratedInspectionCanonical.ToJson(
            GeneratedToolingAccessPathAuditDocument.From(this));
    }

    public sealed class GeneratedToolingDigestChainManifest
    {
        public const string SchemaVersion = "map20_06.digest_chain_manifest.v1";

        public GeneratedToolingDigestChainManifest(string toolingExitAuditDigest,
            string accessPathAuditDigest, string map20PhaseExitDigest,
            string map2101HandoffDigest, string createdUtc)
        {
            ToolingExitAuditDigest = GeneratedToolingExitAuditValidation.Digest(
                toolingExitAuditDigest, nameof(toolingExitAuditDigest));
            AccessPathAuditDigest = GeneratedToolingExitAuditValidation.Digest(
                accessPathAuditDigest, nameof(accessPathAuditDigest));
            Map20PhaseExitDigest = GeneratedToolingExitAuditValidation.Digest(
                map20PhaseExitDigest, nameof(map20PhaseExitDigest));
            Map2101HandoffDigest = GeneratedToolingExitAuditValidation.Digest(
                map2101HandoffDigest, nameof(map2101HandoffDigest));
            CreatedUtc = createdUtc ?? string.Empty;
            CanonicalDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                SchemaVersion, GeneratedToolingExitAuditPreconditions.TaskId,
                GeneratedToolingExitAuditPreconditions.SourceMap2005ResultDigest,
                GeneratedToolingExitAuditPreconditions.SourceMap2005TaskDigest,
                GeneratedToolingExitAuditPreconditions.SourceMap2006HandoffDigest,
                ToolingExitAuditDigest, AccessPathAuditDigest, Map20PhaseExitDigest,
                Map2101HandoffDigest, "created_utc_excluded=true",
            });
        }

        public string ToolingExitAuditDigest { get; }
        public string AccessPathAuditDigest { get; }
        public string Map20PhaseExitDigest { get; }
        public string Map2101HandoffDigest { get; }
        public string CreatedUtc { get; }
        public bool CreatedUtcExcludedFromCanonicalDigest => true;
        public string CanonicalDigest { get; }
        public string Serialize() => GeneratedInspectionCanonical.ToJson(
            GeneratedToolingDigestChainManifestDocument.From(this));
    }

    internal static class GeneratedToolingExitAuditValidation
    {
        public static string Clean(string value) => value == null ? string.Empty : value.Trim();

        public static string Require(string value, string parameterName)
        {
            var result = Clean(value);
            if (result.Length == 0) throw new ArgumentException("Value is required.", parameterName);
            return result;
        }

        public static string Path(string value, string parameterName) =>
            Require(value, parameterName).Replace('\\', '/');

        public static string Digest(string value, string parameterName)
        {
            if (!BakingCanonicalDigest.IsLowerHexSha256(value))
                throw new ArgumentException("Value must be lower-hex SHA-256.", parameterName);
            return value;
        }
    }

    [Serializable]
    internal sealed class GeneratedToolingSourceDigestDocument
    {
        public string owner_task;
        public string relative_path;
        public string canonical_digest;
        public string physical_file_digest;
        public bool read_only;

        public static GeneratedToolingSourceDigestDocument From(
            GeneratedToolingSourceDigestRecord value) => new GeneratedToolingSourceDigestDocument
        {
            owner_task = value.OwnerTask,
            relative_path = value.RelativePath,
            canonical_digest = value.CanonicalDigest,
            physical_file_digest = value.PhysicalFileDigest,
            read_only = true,
        };
    }

    [Serializable]
    internal sealed class GeneratedCoordinateAuditDocument
    {
        public string trace_id;
        public string owner_task;
        public string seed_context;
        public string world_coordinate;
        public string sector_coordinate;
        public string cell_coordinate;
        public string local_coordinate;
        public string source_location;
        public string selection_path;
        public bool missing_data_isolated;
        public string missing_reason;
        public string evidence_digest;

        public static GeneratedCoordinateAuditDocument From(GeneratedCoordinateAuditRecord value) =>
            new GeneratedCoordinateAuditDocument
            {
                trace_id = value.TraceId,
                owner_task = value.OwnerTask,
                seed_context = value.SeedContext,
                world_coordinate = value.WorldCoordinate,
                sector_coordinate = value.SectorCoordinate,
                cell_coordinate = value.CellCoordinate,
                local_coordinate = value.LocalCoordinate,
                source_location = value.SourceLocation,
                selection_path = value.SelectionPath,
                missing_data_isolated = value.MissingDataIsolated,
                missing_reason = value.MissingReason,
                evidence_digest = value.EvidenceDigest,
            };
    }

    [Serializable]
    internal sealed class GeneratedRollbackScopeAuditDocument
    {
        public string scope_token;
        public string boundary_token;
        public int bounded_target_count;
        public int maximum_target_count;
        public bool explicit_world_confirmation;
        public bool whole_project_expansion_allowed;
        public string audit_state;
        public int rollback_execution_count;
        public string evidence_digest;

        public static GeneratedRollbackScopeAuditDocument From(
            GeneratedRollbackScopeAuditRecord value) => new GeneratedRollbackScopeAuditDocument
        {
            scope_token = value.ScopeToken,
            boundary_token = value.BoundaryToken,
            bounded_target_count = value.BoundedTargetCount,
            maximum_target_count = value.MaximumTargetCount,
            explicit_world_confirmation = value.ExplicitWorldConfirmation,
            whole_project_expansion_allowed = value.WholeProjectExpansionAllowed,
            audit_state = value.AuditState,
            rollback_execution_count = value.RollbackExecutionCount,
            evidence_digest = value.EvidenceDigest,
        };
    }

    [Serializable]
    internal sealed class GeneratedSourceNavigationAuditDocument
    {
        public string validation_error_id;
        public string owner_task;
        public string jump_target_kind;
        public string csv_file_path;
        public int row_number_1_based;
        public int column_number_1_based;
        public string column_name;
        public string field_name;
        public string record_id;
        public string selection_path;
        public bool source_available;
        public string missing_reason;
        public string evidence_digest;

        public static GeneratedSourceNavigationAuditDocument From(
            GeneratedSourceNavigationAuditRecord value) => new GeneratedSourceNavigationAuditDocument
        {
            validation_error_id = value.ValidationErrorId,
            owner_task = value.OwnerTask,
            jump_target_kind = value.JumpTargetKind,
            csv_file_path = value.CsvFilePath,
            row_number_1_based = value.RowNumber1Based,
            column_number_1_based = value.ColumnNumber1Based,
            column_name = value.ColumnName,
            field_name = value.FieldName,
            record_id = value.RecordId,
            selection_path = value.SelectionPath,
            source_available = value.SourceAvailable,
            missing_reason = value.MissingReason,
            evidence_digest = value.EvidenceDigest,
        };
    }

    [Serializable]
    internal sealed class GeneratedReplayHashAuditDocument
    {
        public string record_id;
        public string owner_task;
        public string execution_state;
        public string canonical_digest;
        public bool repeat_stable;
        public bool culture_stable;
        public bool input_order_stable;
        public int replay_execution_count;

        public static GeneratedReplayHashAuditDocument From(GeneratedReplayHashAuditRecord value) =>
            new GeneratedReplayHashAuditDocument
        {
            record_id = value.RecordId,
            owner_task = value.OwnerTask,
            execution_state = value.ExecutionState,
            canonical_digest = value.CanonicalDigest,
            repeat_stable = value.RepeatStable,
            culture_stable = value.CultureStable,
            input_order_stable = value.InputOrderStable,
            replay_execution_count = value.ReplayExecutionCount,
        };
    }

    [Serializable]
    internal sealed class GeneratedHudStateAuditDocument
    {
        public string hud_state_id;
        public string selected_validation_error_id;
        public string selected_failure_record_id;
        public string selected_selection_path;
        public string state_kind;
        public int auto_spawn_path_count;
        public int runtime_mutation_count;
        public int scene_prefab_wiring_count;
        public string canonical_digest;

        public static GeneratedHudStateAuditDocument From(GeneratedHudStateAuditRecord value) =>
            new GeneratedHudStateAuditDocument
        {
            hud_state_id = value.HudStateId,
            selected_validation_error_id = value.SelectedValidationErrorId,
            selected_failure_record_id = value.SelectedFailureRecordId,
            selected_selection_path = value.SelectedSelectionPath,
            state_kind = value.StateKind,
            auto_spawn_path_count = value.AutoSpawnPathCount,
            runtime_mutation_count = value.RuntimeMutationCount,
            scene_prefab_wiring_count = value.ScenePrefabWiringCount,
            canonical_digest = value.CanonicalDigest,
        };
    }

    [Serializable]
    internal sealed class GeneratedToolingAccessPathDocument
    {
        public string access_path_id;
        public string entry_surface;
        public string target_surface;
        public int user_action_count;
        public string[] actions;
        public string owner_task;
        public string evidence_digest;
        public bool passes_three_click_limit;

        public static GeneratedToolingAccessPathDocument From(
            GeneratedToolingAccessPathRecord value) => new GeneratedToolingAccessPathDocument
        {
            access_path_id = value.AccessPathId,
            entry_surface = value.EntrySurface,
            target_surface = value.TargetSurface,
            user_action_count = value.UserActionCount,
            actions = value.Actions.ToArray(),
            owner_task = value.OwnerTask,
            evidence_digest = value.EvidenceDigest,
            passes_three_click_limit = value.PassesThreeClickLimit,
        };
    }

    [Serializable]
    internal sealed class GeneratedExitInvariantDocument
    {
        public string invariant_id;
        public string status;
        public string evidence_source;
        public string evidence_digest;
        public string owner_task;

        public static GeneratedExitInvariantDocument From(GeneratedExitInvariantRecord value) =>
            new GeneratedExitInvariantDocument
        {
            invariant_id = value.InvariantId,
            status = value.Status,
            evidence_source = value.EvidenceSource,
            evidence_digest = value.EvidenceDigest,
            owner_task = value.OwnerTask,
        };
    }

    [Serializable]
    internal sealed class GeneratedToolingExitAuditDocument
    {
        public string schema_version;
        public string task_id;
        public string source_MAP20_05_result_digest;
        public string source_MAP20_05_task_digest;
        public string source_MAP20_06_handoff_digest;
        public GeneratedToolingSourceDigestDocument[] map20_result_chain;
        public GeneratedToolingSourceDigestDocument[] map20_generated_artifact_chain;
        public GeneratedCoordinateAuditDocument[] coordinate_audit_records;
        public GeneratedRollbackScopeAuditDocument[] rollback_scope_records;
        public GeneratedSourceNavigationAuditDocument[] source_navigation_records;
        public GeneratedReplayHashAuditDocument[] replay_hash_records;
        public GeneratedHudStateAuditDocument[] hud_state_records;
        public GeneratedToolingAccessPathDocument[] access_path_records;
        public GeneratedExitInvariantDocument[] exit_invariant_records;
        public int unsafe_action_button_count;
        public int generator_execution_count;
        public int validation_runner_execution_count;
        public int replay_execution_count;
        public int rollback_execution_count;
        public int csv_authoring_write_count;
        public int runtime_object_spawn_count;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
        public string canonical_digest;

        public static GeneratedToolingExitAuditDocument From(GeneratedToolingExitAudit value) =>
            new GeneratedToolingExitAuditDocument
        {
            schema_version = GeneratedToolingExitAudit.SchemaVersion,
            task_id = GeneratedToolingExitAuditPreconditions.TaskId,
            source_MAP20_05_result_digest = value.SourceMap2005ResultDigest,
            source_MAP20_05_task_digest = value.SourceMap2005TaskDigest,
            source_MAP20_06_handoff_digest = value.SourceMap2006HandoffDigest,
            map20_result_chain = value.Map20ResultChain.Select(
                GeneratedToolingSourceDigestDocument.From).ToArray(),
            map20_generated_artifact_chain = value.Map20GeneratedArtifactChain.Select(
                GeneratedToolingSourceDigestDocument.From).ToArray(),
            coordinate_audit_records = value.CoordinateAuditRecords.Select(
                GeneratedCoordinateAuditDocument.From).ToArray(),
            rollback_scope_records = value.RollbackScopeRecords.Select(
                GeneratedRollbackScopeAuditDocument.From).ToArray(),
            source_navigation_records = value.SourceNavigationRecords.Select(
                GeneratedSourceNavigationAuditDocument.From).ToArray(),
            replay_hash_records = value.ReplayHashRecords.Select(
                GeneratedReplayHashAuditDocument.From).ToArray(),
            hud_state_records = value.HudStateRecords.Select(
                GeneratedHudStateAuditDocument.From).ToArray(),
            access_path_records = value.AccessPathRecords.Select(
                GeneratedToolingAccessPathDocument.From).ToArray(),
            exit_invariant_records = value.ExitInvariantRecords.Select(
                GeneratedExitInvariantDocument.From).ToArray(),
            unsafe_action_button_count = value.UnsafeActionButtonCount,
            generator_execution_count = value.GeneratorExecutionCount,
            validation_runner_execution_count = value.ValidationRunnerExecutionCount,
            replay_execution_count = value.ReplayExecutionCount,
            rollback_execution_count = value.RollbackExecutionCount,
            csv_authoring_write_count = value.CsvAuthoringWriteCount,
            runtime_object_spawn_count = value.RuntimeObjectSpawnCount,
            created_utc = value.CreatedUtc,
            created_utc_excluded_from_canonical_digest = true,
            canonical_digest = value.CanonicalDigest,
        };
    }

    [Serializable]
    internal sealed class GeneratedToolingAccessPathAuditDocument
    {
        public string schema_version;
        public string task_id;
        public string source_MAP20_06_handoff_digest;
        public GeneratedToolingAccessPathDocument[] access_path_records;
        public int maximum_user_action_count;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
        public string canonical_digest;

        public static GeneratedToolingAccessPathAuditDocument From(
            GeneratedToolingAccessPathAudit value) => new GeneratedToolingAccessPathAuditDocument
        {
            schema_version = GeneratedToolingAccessPathAudit.SchemaVersion,
            task_id = GeneratedToolingExitAuditPreconditions.TaskId,
            source_MAP20_06_handoff_digest =
                GeneratedToolingExitAuditPreconditions.SourceMap2006HandoffDigest,
            access_path_records = value.AccessPathRecords.Select(
                GeneratedToolingAccessPathDocument.From).ToArray(),
            maximum_user_action_count = value.MaximumUserActionCount,
            created_utc = value.CreatedUtc,
            created_utc_excluded_from_canonical_digest = true,
            canonical_digest = value.CanonicalDigest,
        };
    }

    [Serializable]
    internal sealed class GeneratedToolingDigestChainManifestDocument
    {
        public string schema_version;
        public string task_id;
        public string source_MAP20_05_result_digest;
        public string source_MAP20_05_task_digest;
        public string MAP20_06_handoff_digest;
        public string tooling_exit_audit_digest;
        public string access_path_audit_digest;
        public string map20_phase_exit_digest;
        public string MAP21_01_handoff_digest;
        public string created_utc;
        public bool created_utc_excluded_from_canonical_digest;
        public string canonical_digest;

        public static GeneratedToolingDigestChainManifestDocument From(
            GeneratedToolingDigestChainManifest value) =>
            new GeneratedToolingDigestChainManifestDocument
            {
                schema_version = GeneratedToolingDigestChainManifest.SchemaVersion,
                task_id = GeneratedToolingExitAuditPreconditions.TaskId,
                source_MAP20_05_result_digest =
                    GeneratedToolingExitAuditPreconditions.SourceMap2005ResultDigest,
                source_MAP20_05_task_digest =
                    GeneratedToolingExitAuditPreconditions.SourceMap2005TaskDigest,
                MAP20_06_handoff_digest =
                    GeneratedToolingExitAuditPreconditions.SourceMap2006HandoffDigest,
                tooling_exit_audit_digest = value.ToolingExitAuditDigest,
                access_path_audit_digest = value.AccessPathAuditDigest,
                map20_phase_exit_digest = value.Map20PhaseExitDigest,
                MAP21_01_handoff_digest = value.Map2101HandoffDigest,
                created_utc = value.CreatedUtc,
                created_utc_excluded_from_canonical_digest = true,
                canonical_digest = value.CanonicalDigest,
            };
    }
}
