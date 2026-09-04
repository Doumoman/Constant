using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.Population
{
    public enum GeneratedPopulationExitAuditSeverity
    {
        Error = 1,
        Risk = 2,
    }

    public enum GeneratedPopulationExitAuditStatus
    {
        Pass = 1,
        Fail = 2,
        Blocked = 3,
    }

    public sealed class GeneratedPopulationExitAuditOverride :
        IComparable<GeneratedPopulationExitAuditOverride>
    {
        public GeneratedPopulationExitAuditOverride(string invariant, string expected)
        {
            Invariant = invariant ?? string.Empty;
            Expected = expected ?? string.Empty;
        }

        public string Invariant { get; }
        public string Expected { get; }
        public int CompareTo(GeneratedPopulationExitAuditOverride other) => other == null
            ? -1 : string.Compare(Invariant, other.Invariant, StringComparison.Ordinal);
    }

    public sealed class GeneratedPopulationExitAuditFinding :
        IComparable<GeneratedPopulationExitAuditFinding>
    {
        internal GeneratedPopulationExitAuditFinding(
            GeneratedPopulationExitAuditSeverity severity,
            string owner,
            string invariant,
            string offendingKey,
            string expected,
            string actual,
            string reason)
        {
            Severity = severity;
            Owner = owner ?? string.Empty;
            Invariant = invariant ?? string.Empty;
            OffendingKey = offendingKey ?? string.Empty;
            Expected = expected ?? string.Empty;
            Actual = actual ?? string.Empty;
            Reason = reason ?? string.Empty;
            EvidenceDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP18_07_FINDING_V1", Severity.ToString(), Owner, Invariant,
                OffendingKey, Expected, Actual, Reason,
            });
            StableToken = string.Join("|", new[]
            {
                "MAP18_07_FINDING_V1", Severity.ToString(), Owner, Invariant,
                OffendingKey, Expected, Actual, Reason, EvidenceDigest,
            });
        }

        public GeneratedPopulationExitAuditSeverity Severity { get; }
        public string Owner { get; }
        public string Invariant { get; }
        public string OffendingKey { get; }
        public string Expected { get; }
        public string Actual { get; }
        public string Reason { get; }
        public string EvidenceDigest { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedPopulationExitAuditFinding other) => other == null
            ? -1 : string.Compare(StableToken, other.StableToken,
                StringComparison.Ordinal);
        public override string ToString() => StableToken;
    }

    public sealed class GeneratedPopulationExitAuditDigestEntry :
        IComparable<GeneratedPopulationExitAuditDigestEntry>
    {
        internal GeneratedPopulationExitAuditDigestEntry(
            string owner,
            string invariant,
            string expected,
            string actual)
        {
            Owner = owner ?? string.Empty;
            Invariant = invariant ?? string.Empty;
            Expected = expected ?? string.Empty;
            Actual = actual ?? string.Empty;
            StableToken = string.Join("|", new[]
            {
                "MAP18_07_DIGEST_V1", Owner, Invariant, Expected, Actual,
                Matches ? "MATCH=1" : "MATCH=0",
            });
        }

        public string Owner { get; }
        public string Invariant { get; }
        public string Expected { get; }
        public string Actual { get; }
        public bool Matches => string.Equals(Expected, Actual, StringComparison.Ordinal);
        public string StableToken { get; }
        public int CompareTo(GeneratedPopulationExitAuditDigestEntry other) => other == null
            ? -1 : string.Compare(Invariant, other.Invariant, StringComparison.Ordinal);
    }

    public sealed class GeneratedPopulationExitAuditCountEntry :
        IComparable<GeneratedPopulationExitAuditCountEntry>
    {
        internal GeneratedPopulationExitAuditCountEntry(
            string owner,
            string invariant,
            int expected,
            int actual)
        {
            Owner = owner ?? string.Empty;
            Invariant = invariant ?? string.Empty;
            Expected = expected;
            Actual = actual;
            StableToken = string.Join("|", new[]
            {
                "MAP18_07_COUNT_V1", Owner, Invariant, Number(Expected),
                Number(Actual), Matches ? "MATCH=1" : "MATCH=0",
            });
        }

        public string Owner { get; }
        public string Invariant { get; }
        public int Expected { get; }
        public int Actual { get; }
        public bool Matches => Expected == Actual;
        public string StableToken { get; }
        public int CompareTo(GeneratedPopulationExitAuditCountEntry other) => other == null
            ? -1 : string.Compare(Invariant, other.Invariant, StringComparison.Ordinal);

        private static string Number(int value) =>
            value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedPopulationExitAuditRoundTripMaterial
    {
        internal GeneratedPopulationExitAuditRoundTripMaterial(
            GeneratedSpecialStateExportSurface specialSurface,
            bool corruptMaterial)
        {
            var runtime = specialSurface.RuntimeStateSurface;
            var saveLines = runtime.RuntimeStateRecords.Select(value =>
                "SAVE|" + value.SaveKey.Value).ToArray();
            var runtimeLines = runtime.RuntimeStateRecords.Select(value =>
                "RUNTIME|" + value.StableToken).ToArray();
            var runtimeExportLines = runtime.Map18_06ExportRecords.Select(value =>
                "RUNTIME_EXPORT|" + value.StableToken).ToArray();
            var rowLines = specialSurface.Rows.Select(value =>
                "ROW|" + value.StableToken).ToArray();
            var lines = saveLines.Concat(runtimeLines).Concat(runtimeExportLines)
                .Concat(rowLines).ToArray();
            if (corruptMaterial && rowLines.Length > 0)
            {
                var firstRow = saveLines.Length + runtimeLines.Length +
                    runtimeExportLines.Length;
                lines[firstRow] += "|ROUND_TRIP_MUTATED";
            }

            Text = BakingCanonicalDigest.NormalizeLineEndingsToLf(
                string.Join("\n", lines) + "\n");
            Digest = BakingCanonicalDigest.HashCanonicalLines(new[] { Text });
            var parsed = Text.Split(new[] { '\n' },
                StringSplitOptions.RemoveEmptyEntries);
            var parsedSaveKeys = Values(parsed, "SAVE|");
            var parsedRuntime = Values(parsed, "RUNTIME|");
            var parsedRuntimeExport = Values(parsed, "RUNTIME_EXPORT|");
            var parsedRows = Values(parsed, "ROW|");

            SaveKeySetDigestAfterRoundTrip =
                BakingCanonicalDigest.HashCanonicalLines(parsedSaveKeys);
            var runtimeExportDigest =
                BakingCanonicalDigest.HashCanonicalLines(parsedRuntimeExport);
            RuntimeStateDigestAfterRoundTrip = BakingCanonicalDigest.HashCanonicalLines(
                new[]
                {
                    GeneratedActivityEventRuntimeStateSurface.PolicyVersion,
                    runtime.HazardEnemyPlan.Digest,
                    runtime.OccupiedSurfaceDigest,
                    runtime.BudgetLedgerDigest,
                }.Concat(parsedRuntime).Concat(new[]
                {
                    SaveKeySetDigestAfterRoundTrip,
                    runtimeExportDigest,
                }));
            ExportRowDigestAfterRoundTrip = BakingCanonicalDigest.HashCanonicalLines(
                new[]
                {
                    GeneratedSpecialStateExportSurface.PolicyVersion,
                    RuntimeStateDigestAfterRoundTrip,
                    SaveKeySetDigestAfterRoundTrip,
                    runtimeExportDigest,
                    runtime.OccupiedSurfaceDigest,
                    runtime.BudgetLedgerDigest,
                    specialSurface.CsvMaterial.Digest,
                }.Concat(parsedRows));
            MaterialRowCount = parsedRows.Count;
            RoundTripMismatchCount = new[]
            {
                string.Equals(SaveKeySetDigestAfterRoundTrip,
                    runtime.SaveKeySetDigest, StringComparison.Ordinal),
                string.Equals(RuntimeStateDigestAfterRoundTrip,
                    runtime.Digest, StringComparison.Ordinal),
                string.Equals(ExportRowDigestAfterRoundTrip,
                    specialSurface.Digest, StringComparison.Ordinal),
            }.Count(value => !value);
        }

        public string Text { get; }
        public string Digest { get; }
        public int MaterialRowCount { get; }
        public int RoundTripMismatchCount { get; }
        public string SaveKeySetDigestAfterRoundTrip { get; }
        public string RuntimeStateDigestAfterRoundTrip { get; }
        public string ExportRowDigestAfterRoundTrip { get; }
        public bool IsLfNormalized => !Text.Contains("\r");
        public int ActualFileWriteCount => 0;
        public int ActualFileReadCount => 0;
        public int PlayerPrefsWriteCount => 0;
        public int PlayerPrefsReadCount => 0;

        private static IReadOnlyList<string> Values(
            IEnumerable<string> lines,
            string prefix) => new ReadOnlyCollection<string>(lines
                .Where(value => value.StartsWith(prefix, StringComparison.Ordinal))
                .Select(value => value.Substring(prefix.Length)).ToArray());
    }

    public sealed class GeneratedPopulationExitAuditRequest
    {
        private readonly ReadOnlyCollection<GeneratedPopulationExitAuditOverride> overrides;
        private readonly ReadOnlyCollection<string> additionalOccupiedReservationKeys;
        private readonly ReadOnlyCollection<string> additionalReservedRequiredCoreKeys;
        private readonly ReadOnlyCollection<string> additionalStableSpawnIds;
        private readonly ReadOnlyCollection<string> additionalRuntimeStateIds;
        private readonly ReadOnlyCollection<string> additionalSaveKeys;
        private readonly ReadOnlyCollection<string> additionalPersistenceKeys;
        private readonly ReadOnlyCollection<string> additionalExportRowKeys;

        public GeneratedPopulationExitAuditRequest(
            GeneratedSpecialStateExportSurface specialStateExportSurface,
            IEnumerable<GeneratedPopulationExitAuditOverride> expectedOverrides = null,
            IEnumerable<string> sourceAdditionalOccupiedReservationKeys = null,
            IEnumerable<string> sourceAdditionalReservedRequiredCoreKeys = null,
            IEnumerable<string> sourceAdditionalStableSpawnIds = null,
            IEnumerable<string> sourceAdditionalRuntimeStateIds = null,
            IEnumerable<string> sourceAdditionalSaveKeys = null,
            IEnumerable<string> sourceAdditionalPersistenceKeys = null,
            IEnumerable<string> sourceAdditionalExportRowKeys = null,
            bool corruptRoundTripMaterial = false,
            bool attemptedRuntimeSideEffect = false,
            bool attemptedCsvFileIo = false,
            bool attemptedSaveFileIo = false,
            bool map19_01Unlocked = false,
            bool map19_01Started = false)
        {
            SpecialStateExportSurface = specialStateExportSurface;
            overrides = new ReadOnlyCollection<GeneratedPopulationExitAuditOverride>(
                (expectedOverrides ?? Array.Empty<GeneratedPopulationExitAuditOverride>())
                .Where(value => value != null).OrderBy(value => value).ToArray());
            additionalOccupiedReservationKeys = Freeze(
                sourceAdditionalOccupiedReservationKeys);
            additionalReservedRequiredCoreKeys = Freeze(
                sourceAdditionalReservedRequiredCoreKeys);
            additionalStableSpawnIds = Freeze(sourceAdditionalStableSpawnIds);
            additionalRuntimeStateIds = Freeze(sourceAdditionalRuntimeStateIds);
            additionalSaveKeys = Freeze(sourceAdditionalSaveKeys);
            additionalPersistenceKeys = Freeze(sourceAdditionalPersistenceKeys);
            additionalExportRowKeys = Freeze(sourceAdditionalExportRowKeys);
            CorruptRoundTripMaterial = corruptRoundTripMaterial;
            AttemptedRuntimeSideEffect = attemptedRuntimeSideEffect;
            AttemptedCsvFileIo = attemptedCsvFileIo;
            AttemptedSaveFileIo = attemptedSaveFileIo;
            Map19_01Unlocked = map19_01Unlocked;
            Map19_01Started = map19_01Started;
        }

        public GeneratedSpecialStateExportSurface SpecialStateExportSurface { get; }
        public IReadOnlyList<GeneratedPopulationExitAuditOverride> ExpectedOverrides => overrides;
        public IReadOnlyList<string> AdditionalOccupiedReservationKeys =>
            additionalOccupiedReservationKeys;
        public IReadOnlyList<string> AdditionalReservedRequiredCoreKeys =>
            additionalReservedRequiredCoreKeys;
        public IReadOnlyList<string> AdditionalStableSpawnIds => additionalStableSpawnIds;
        public IReadOnlyList<string> AdditionalRuntimeStateIds => additionalRuntimeStateIds;
        public IReadOnlyList<string> AdditionalSaveKeys => additionalSaveKeys;
        public IReadOnlyList<string> AdditionalPersistenceKeys => additionalPersistenceKeys;
        public IReadOnlyList<string> AdditionalExportRowKeys => additionalExportRowKeys;
        public bool CorruptRoundTripMaterial { get; }
        public bool AttemptedRuntimeSideEffect { get; }
        public bool AttemptedCsvFileIo { get; }
        public bool AttemptedSaveFileIo { get; }
        public bool Map19_01Unlocked { get; }
        public bool Map19_01Started { get; }

        public string Expected(string invariant, string fallback)
        {
            var matches = overrides.Where(value => string.Equals(value.Invariant,
                invariant, StringComparison.Ordinal)).ToArray();
            return matches.Length == 1 ? matches[0].Expected : fallback;
        }

        private static ReadOnlyCollection<string> Freeze(IEnumerable<string> source) =>
            new ReadOnlyCollection<string>((source ?? Array.Empty<string>())
                .Select(value => value ?? string.Empty)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
    }

    public sealed class GeneratedPopulationExitAuditSurface
    {
        private readonly ReadOnlyCollection<GeneratedPopulationExitAuditDigestEntry> digests;
        private readonly ReadOnlyCollection<GeneratedPopulationExitAuditCountEntry> counts;
        private readonly ReadOnlyCollection<string> riskNotes;

        internal GeneratedPopulationExitAuditSurface(
            GeneratedSpecialStateExportSurface specialStateExportSurface,
            IEnumerable<GeneratedPopulationExitAuditDigestEntry> sourceDigests,
            IEnumerable<GeneratedPopulationExitAuditCountEntry> sourceCounts,
            GeneratedPopulationExitAuditRoundTripMaterial roundTripMaterial,
            int occupiedSlotReuseCount,
            int reservedRequiredCoreSlotReuseCount,
            int stableSpawnIdDuplicateCount,
            int runtimeStateIdDuplicateCount,
            int saveKeyDuplicateCount,
            int activePersistenceKeyDuplicateCount,
            int exportRowKeyDuplicateCount,
            int legacyShortPersistenceKeyAcceptedCount,
            int coreResourceCanonicalKeyCheckCount,
            IEnumerable<string> sourceRiskNotes)
        {
            SpecialStateExportSurface = specialStateExportSurface;
            digests = new ReadOnlyCollection<GeneratedPopulationExitAuditDigestEntry>(
                sourceDigests.OrderBy(value => value).ToArray());
            counts = new ReadOnlyCollection<GeneratedPopulationExitAuditCountEntry>(
                sourceCounts.OrderBy(value => value).ToArray());
            RoundTripMaterial = roundTripMaterial;
            OccupiedSlotReuseCount = occupiedSlotReuseCount;
            ReservedRequiredCoreSlotReuseCount = reservedRequiredCoreSlotReuseCount;
            StableSpawnIdDuplicateCount = stableSpawnIdDuplicateCount;
            RuntimeStateIdDuplicateCount = runtimeStateIdDuplicateCount;
            SaveKeyDuplicateCount = saveKeyDuplicateCount;
            ActivePersistenceKeyDuplicateCount = activePersistenceKeyDuplicateCount;
            ExportRowKeyDuplicateCount = exportRowKeyDuplicateCount;
            LegacyShortPersistenceKeyAcceptedCount = legacyShortPersistenceKeyAcceptedCount;
            CoreResourceCanonicalKeyCheckCount = coreResourceCanonicalKeyCheckCount;
            riskNotes = new ReadOnlyCollection<string>((sourceRiskNotes ?? Array.Empty<string>())
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
            ApprovedAuditDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
                { PolicyVersion }
                .Concat(digests.Select(value => value.StableToken))
                .Concat(counts.Select(value => value.StableToken))
                .Concat(new[]
                {
                    "IDENTITY|" + string.Join("|", new[]
                    {
                        Number(OccupiedSlotReuseCount),
                        Number(ReservedRequiredCoreSlotReuseCount),
                        Number(StableSpawnIdDuplicateCount),
                        Number(RuntimeStateIdDuplicateCount),
                        Number(SaveKeyDuplicateCount),
                        Number(ActivePersistenceKeyDuplicateCount),
                        Number(ExportRowKeyDuplicateCount),
                        Number(LegacyShortPersistenceKeyAcceptedCount),
                        Number(CoreResourceCanonicalKeyCheckCount),
                    }),
                    "ROUND_TRIP|" + roundTripMaterial.Digest + "|" +
                        roundTripMaterial.SaveKeySetDigestAfterRoundTrip + "|" +
                        roundTripMaterial.RuntimeStateDigestAfterRoundTrip + "|" +
                        roundTripMaterial.ExportRowDigestAfterRoundTrip,
                }).Concat(riskNotes.Select(value => "RISK|" + value)));
            Map19_01HandoffDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_01_POPULATION_HANDOFF_V1", ApprovedAuditDigest,
                specialStateExportSurface.Map18_07AuditSurfaceDigest,
                string.Join("|", riskNotes),
            });
        }

        public const string PolicyVersion = "MAP18_07_POPULATION_EXIT_AUDIT_V1";
        public const string DownstreamOwner =
            "MAP19_01_LOCK_TRAVERSAL_PROFILE_AND_RULE_REGISTRY";
        public const bool OpensDownstreamTask = false;

        public GeneratedSpecialStateExportSurface SpecialStateExportSurface { get; }
        public IReadOnlyList<GeneratedPopulationExitAuditDigestEntry> HandoffDigests => digests;
        public IReadOnlyList<GeneratedPopulationExitAuditCountEntry> CountChecks => counts;
        public GeneratedPopulationExitAuditRoundTripMaterial RoundTripMaterial { get; }
        public IReadOnlyList<string> RiskNotes => riskNotes;
        public int AuditedMap18TaskSurfaceCount => 6;
        public int AuditedUpstreamDigestCount => digests.Count;
        public int HandoffDigestMismatchCount => digests.Count(value => !value.Matches);
        public int RequiredCountMismatchCount => counts.Count(value => !value.Matches);
        public int OccupiedSlotReuseCount { get; }
        public int ReservedRequiredCoreSlotReuseCount { get; }
        public int StableSpawnIdDuplicateCount { get; }
        public int RuntimeStateIdDuplicateCount { get; }
        public int SaveKeyDuplicateCount { get; }
        public int ActivePersistenceKeyDuplicateCount { get; }
        public int ExportRowKeyDuplicateCount { get; }
        public int LegacyShortPersistenceKeyAcceptedCount { get; }
        public int CoreResourceCanonicalKeyCheckCount { get; }
        public string ApprovedAuditDigest { get; }
        public string Map19_01HandoffDigest { get; }
        public bool Map19_01Unlocked => false;
        public bool Map19_01Started => false;

        public int RuntimeObjectSpawnCount => 0;
        public int GameObjectInstantiateCount => 0;
        public int GameObjectEnableCount => 0;
        public int GameObjectDisableCount => 0;
        public int GameObjectDestroyCount => 0;
        public int SystemIoFileWriteCount => 0;
        public int SystemIoFileReadCount => 0;
        public int DiskSaveFileCreateCount => 0;
        public int DiskLoadFileCreateCount => 0;
        public int ActualUserSaveSlotWriteCount => 0;
        public int PlatformSaveStorageWriteCount => 0;
        public int ActualCsvFileWriteCount => 0;
        public int ActualCsvFileReadCount => 0;
        public int GeneratedCsvFileCommitCount => 0;
        public int RuntimeSpecialRegionSpawnCount => 0;
        public int RuntimeVillageSpawnCount => 0;
        public int RuntimeResourceSpawnCount => 0;
        public int RuntimeForgeSpawnCount => 0;
        public int RuntimeBossSpawnCount => 0;
        public int RuntimeActivityPrefabSpawnCount => 0;
        public int RuntimeEventPrefabSpawnCount => 0;
        public int ActualEventActivationCount => 0;
        public int ActualShopTransactionCount => 0;
        public int RewardGrantCount => 0;
        public int InventoryMutationCount => 0;
        public int ResourceMutationCount => 0;
        public int DamageExecutionCount => 0;
        public int CombatExecutionCount => 0;
        public int EnemyAiExecutionCount => 0;
        public int PhysicsExecutionCount => 0;
        public int TilemapComponentWriteCount => 0;
        public int TilemapSetTileCallCount => 0;
        public int TilemapSetTilesCallCount => 0;
        public int TilemapSetTilesBlockCallCount => 0;
        public int TilemapClearAllTilesCallCount => 0;
        public int TilemapColliderCreationCount => 0;
        public int CompositeColliderCreationCount => 0;
        public int ColliderCreationCount => 0;
        public int RigidbodyCreationCount => 0;
        public int PhysicsQueryCount => 0;
        public int PhysicsSimulationCount => 0;
        public int NavMeshSetupCount => 0;
        public int PathfindingSetupCount => 0;
        public int SceneMutationCount => 0;
        public int PrefabMutationCount => 0;
        public int TilemapMutationCount => 0;
        public int CameraReadCount => 0;
        public int CameraWriteCount => 0;
        public int AddressablesLoadCount => 0;
        public int ResourcesLoadCount => 0;
        public int AssetDatabaseLoadCount => 0;
        public int ProductionSeedApprovalCount => 0;
        public int PriorTaskTestSelectionCount => 0;
        public int Legacy19347SelectionCount => 0;
        public int PlayModeSelectionCount => 0;
        public int UnfilteredTestSelectionCount => 0;
        public int FullRegressionRunCount => 0;

        private static string Number(int value) =>
            value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedPopulationExitAuditResult
    {
        private readonly ReadOnlyCollection<GeneratedPopulationExitAuditFinding> findings;

        internal GeneratedPopulationExitAuditResult(
            GeneratedPopulationExitAuditStatus status,
            GeneratedPopulationExitAuditSurface surface,
            IEnumerable<GeneratedPopulationExitAuditFinding> sourceFindings)
        {
            Status = status;
            Surface = surface;
            findings = new ReadOnlyCollection<GeneratedPopulationExitAuditFinding>(
                (sourceFindings ?? Array.Empty<GeneratedPopulationExitAuditFinding>())
                .OrderBy(value => value).ToArray());
        }

        public GeneratedPopulationExitAuditStatus Status { get; }
        public bool Success => Status == GeneratedPopulationExitAuditStatus.Pass &&
            Surface != null && findings.Count == 0;
        public GeneratedPopulationExitAuditSurface Surface { get; }
        public IReadOnlyList<GeneratedPopulationExitAuditFinding> Findings => findings;
        public int PartialAuditSurfaceCount => Success ? 1 : 0;
        public string ApprovedAuditDigest => Success ? Surface.ApprovedAuditDigest : string.Empty;
        public string Map19_01HandoffDigest => Success ? Surface.Map19_01HandoffDigest : string.Empty;
        public bool AtomicFailureApprovedDigestPublished =>
            !Success && !string.IsNullOrEmpty(ApprovedAuditDigest);
    }

    public static class GeneratedPopulationExitAuditRunner
    {
        public const string SlotIndexDigestInvariant = "MAP18_01_SLOT_INDEX_DIGEST";
        public const string SlotStableIdSetDigestInvariant = "MAP18_01_STABLE_ID_SET_DIGEST";
        public const string MandatoryPlacementDigestInvariant = "MAP18_02_PLACEMENT_DIGEST";
        public const string MandatoryStableIdSetDigestInvariant = "MAP18_02_STABLE_ID_SET_DIGEST";
        public const string PopulationPlanDigestInvariant = "MAP18_03_POPULATION_DIGEST";
        public const string PopulationOccupiedDigestInvariant = "MAP18_03_OCCUPIED_DIGEST";
        public const string HazardEnemyPlanDigestInvariant = "MAP18_04_PLAN_DIGEST";
        public const string HazardEnemyOccupiedDigestInvariant = "MAP18_04_OCCUPIED_DIGEST";
        public const string BudgetLedgerDigestInvariant = "MAP18_04_BUDGET_DIGEST";
        public const string RuntimeStateDigestInvariant = "MAP18_05_RUNTIME_DIGEST";
        public const string RuntimeSaveKeyDigestInvariant = "MAP18_05_SAVE_KEY_DIGEST";
        public const string RuntimeExportDigestInvariant = "MAP18_05_EXPORT_DIGEST";
        public const string SpecialExportDigestInvariant = "MAP18_06_SPECIAL_EXPORT_DIGEST";
        public const string CsvMaterialDigestInvariant = "MAP18_06_CSV_DIGEST";
        public const string DebugSnapshotDigestInvariant = "MAP18_06_DEBUG_DIGEST";
        public const string Map18_07AuditSurfaceDigestInvariant = "MAP18_06_AUDIT_SURFACE_DIGEST";

        public const string SlotSourceRecordCountInvariant = "MAP18_01_SLOT_SOURCE_COUNT";
        public const string MandatoryCandidateCountInvariant = "MAP18_01_MANDATORY_CANDIDATE_COUNT";
        public const string MandatoryPlacementCountInvariant = "MAP18_02_PLACEMENT_COUNT";
        public const string PopulationPlacementCountInvariant = "MAP18_03_PLACEMENT_COUNT";
        public const string HazardEnemyPlacementCountInvariant = "MAP18_04_PLACEMENT_COUNT";
        public const string OccupiedSurfaceCountInvariant = "MAP18_04_OCCUPIED_COUNT";
        public const string RuntimeStateRecordCountInvariant = "MAP18_05_RUNTIME_COUNT";
        public const string SpecialExportRowCountInvariant = "MAP18_06_EXPORT_ROW_COUNT";
        public const string CsvMaterialRowCountInvariant = "MAP18_06_CSV_ROW_COUNT";
        public const string DebugSnapshotSectionCountInvariant = "MAP18_06_DEBUG_SECTION_COUNT";

        public const string ExpectedSlotIndexDigest =
            "889c25815c9d0bffe6c6ea785b66c55e79f0e8e93631771f0ec30a0b39c2b6bd";
        public const string ExpectedSlotStableIdSetDigest =
            "bfc341e0c62a62d8846580b9455874df9e30573bd4c5f6cc450d719c89464b8a";
        public const string ExpectedMandatoryPlacementDigest =
            "eda7bf7aedb660223927d6e0b36e63f5dbe041761febf91da6fb855f413f200f";
        public const string ExpectedMandatoryStableIdSetDigest =
            "c4c1948c17d8e75e821e3eec4402832635e7773693c4b956bc18a53d7ca15a09";
        public const string ExpectedPopulationPlanDigest =
            "4fc87b1c2699802761b9956aaf58fdc9ebbfaf6f32f33bdc9b7a776752cd109e";
        public const string ExpectedPopulationOccupiedDigest =
            "f5556c9e609de1b71195c45473582009f99b5799cb03052da75682ed9c43e422";
        public const string ExpectedHazardEnemyPlanDigest =
            "003b2ddc329d736945eda48b8b03df128bd0891c40910aa89e97c965ed3222ac";
        public const string ExpectedHazardEnemyOccupiedDigest =
            "39e530dde3a98191aee290916d536b4952034aa2c758cb7c35050d4e2f74b688";
        public const string ExpectedBudgetLedgerDigest =
            "08a4d128bb08324c99669c235101ead8d2c81d2f78d379b7b58fe59090bef52d";
        public const string ExpectedRuntimeStateDigest =
            "2774cc515c4531ad90055afb2bdabb4a73439a0e64162194b4c9dbdd51db0f72";
        public const string ExpectedRuntimeSaveKeyDigest =
            "9c841116463551aff94fe77132c2b7b61d23b07840ee5aa29710799591b0d448";
        public const string ExpectedRuntimeExportDigest =
            "2f2665b46b054f408f8e7a1fb97c128ca355b829aa74d4aa7811b2792b9f6d73";
        public const string ExpectedSpecialExportDigest =
            "358ac8cfe78eec502db049f8940ed0c71458179b89bb451680e837b0797b77b5";
        public const string ExpectedCsvMaterialDigest =
            "03711cb7dcc5f576ca6fb6ff16fcbdbbd295a3838c9061761c201f089f8473bf";
        public const string ExpectedDebugSnapshotDigest =
            "59efb7fd30df9ec62014cadd04a111b222e7dd13e298789dbab88a661bea22ed";
        public const string ExpectedMap18_07AuditSurfaceDigest =
            "ca7cf633a9a2dbf7b5a85d5d847ece3f8fe2bf4c4071e546cbdb6f593397bc72";

        public const int ExpectedSlotSourceRecordCount = 12;
        public const int ExpectedMandatoryCandidateCount = 5;
        public const int ExpectedMandatoryPlacementCount = 4;
        public const int ExpectedPopulationPlacementCount = 3;
        public const int ExpectedHazardEnemyPlacementCount = 2;
        public const int ExpectedOccupiedSurfaceCount = 9;
        public const int ExpectedRuntimeStateRecordCount = 6;
        public const int ExpectedSpecialExportRowCount = 18;
        public const int ExpectedCsvMaterialRowCount = 18;
        public const int ExpectedDebugSnapshotSectionCount = 5;

        private static readonly string[] CanonicalCoreResourceKeys =
        {
            "SR_STATE_CASSIA_SAP_SITE_5_REWARD_CASSIA_SAP_REWARD",
            "SR_STATE_MOON_CORE_SITE_5_REWARD_MOON_CORE_REWARD",
            "SR_STATE_STAR_NURUK_SITE_5_REWARD_STAR_NURUK_REWARD",
        };

        public static GeneratedPopulationExitAuditResult Run(
            GeneratedPopulationExitAuditRequest request)
        {
            var findings = new List<GeneratedPopulationExitAuditFinding>();
            if (request == null)
            {
                findings.Add(Finding("MAP18_07", "REQUEST", "REQUEST", "PRESENT",
                    "MISSING", "Population exit audit request is required."));
                return Result(GeneratedPopulationExitAuditStatus.Blocked, null, findings);
            }
            if (request.SpecialStateExportSurface == null)
            {
                findings.Add(Finding("MAP18_06", Map18_07AuditSurfaceDigestInvariant,
                    "SPECIAL_STATE_EXPORT_SURFACE", "PRESENT", "MISSING",
                    "MAP18_06 audit surface is required for complete exit approval."));
                return Result(GeneratedPopulationExitAuditStatus.Blocked, null, findings);
            }

            ValidateOverrides(request, findings);
            var special = request.SpecialStateExportSurface;
            var runtime = special.RuntimeStateSurface;
            var hazardEnemy = runtime.HazardEnemyPlan;
            var population = hazardEnemy.PopulationPlan;
            var mandatory = population.MandatoryPlan;
            var index = population.SourceSlotIndex;
            var digests = BuildDigests(request, index, mandatory, population,
                hazardEnemy, runtime, special);
            var counts = BuildCounts(request, index, mandatory, population,
                hazardEnemy, runtime, special);
            ValidateDigestAndCountEntries(digests, counts, findings);
            ValidateResponsibilityChain(index, mandatory, population, hazardEnemy,
                runtime, special, findings);

            var occupiedKeys = hazardEnemy.OccupiedSurface.Select(value =>
                value.ReservationKey).Concat(request.AdditionalOccupiedReservationKeys);
            var occupiedReuse = DuplicateCount(occupiedKeys);
            var laterReservationKeys = population.Entries.Select(value => value.ReservationKey)
                .Concat(hazardEnemy.Entries.Select(value => value.ReservationKey))
                .Concat(request.AdditionalReservedRequiredCoreKeys).ToArray();
            var requiredKeys = new HashSet<string>(mandatory.Exclusions.Select(value =>
                value.ReservationKey), StringComparer.Ordinal);
            var requiredReuse = laterReservationKeys.Count(requiredKeys.Contains);
            var spawnDuplicates = DuplicateCount(hazardEnemy.OccupiedSurface.Select(value =>
                value.StableSpawnId.Value).Concat(request.AdditionalStableSpawnIds));
            var runtimeDuplicates = DuplicateCount(runtime.RuntimeStateRecords.Select(value =>
                value.RuntimeStateId.Value).Concat(request.AdditionalRuntimeStateIds));
            var saveDuplicates = DuplicateCount(runtime.RuntimeStateRecords.Select(value =>
                value.SaveKey.Value).Concat(request.AdditionalSaveKeys));
            var activePersistence = special.Rows.Where(value => value.HasPersistenceKey &&
                    value.SourceStatus == GeneratedSpecialStateSourceStatus.Active)
                .Select(value => value.PersistenceKey.Value)
                .Concat(request.AdditionalPersistenceKeys).ToArray();
            var persistenceDuplicates = DuplicateCount(activePersistence);
            var rowDuplicates = DuplicateCount(special.Rows.Select(value => value.RowKey)
                .Concat(request.AdditionalExportRowKeys));
            var legacyKeys = activePersistence.Count(value => !string.IsNullOrEmpty(value) &&
                !value.StartsWith("SR_STATE_", StringComparison.Ordinal));
            var actualCoreKeys = special.Rows.Where(value => value.Kind ==
                    GeneratedSpecialStateExportKind.CoreResource && value.HasPersistenceKey)
                .Select(value => value.PersistenceKey.Value)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var canonicalChecks = CanonicalCoreResourceKeys.Count(actualCoreKeys.Contains);

            ValidateZero("MAP18_01_06", "OCCUPIED_SLOT_REUSE", occupiedReuse, findings);
            ValidateZero("MAP18_02_04", "RESERVED_REQUIRED_CORE_REUSE",
                requiredReuse, findings);
            ValidateZero("MAP18_01_06", "STABLE_SPAWN_ID_DUPLICATES",
                spawnDuplicates, findings);
            ValidateZero("MAP18_05", "RUNTIME_STATE_ID_DUPLICATES",
                runtimeDuplicates, findings);
            ValidateZero("MAP18_05", "SAVE_KEY_DUPLICATES", saveDuplicates, findings);
            ValidateZero("MAP18_06", "ACTIVE_PERSISTENCE_KEY_DUPLICATES",
                persistenceDuplicates, findings);
            ValidateZero("MAP18_06", "EXPORT_ROW_KEY_DUPLICATES",
                rowDuplicates, findings);
            ValidateZero("MAP18_06", "LEGACY_SHORT_PERSISTENCE_KEYS",
                legacyKeys, findings);
            if (canonicalChecks != CanonicalCoreResourceKeys.Length ||
                actualCoreKeys.Length != CanonicalCoreResourceKeys.Length)
            {
                findings.Add(Finding("MAP18_06", "CORE_RESOURCE_CANONICAL_KEYS",
                    string.Join(",", actualCoreKeys),
                    string.Join(",", CanonicalCoreResourceKeys),
                    string.Join(",", actualCoreKeys),
                    "All three CoreResource exports must retain authoritative keys."));
            }

            ValidateRuntimeBoundary(request, special, hazardEnemy, population, findings);
            var roundTrip = new GeneratedPopulationExitAuditRoundTripMaterial(special,
                request.CorruptRoundTripMaterial);
            if (roundTrip.RoundTripMismatchCount != 0)
            {
                findings.Add(Finding("MAP18_05_06", "IN_MEMORY_ROUND_TRIP",
                    "ROUND_TRIP_MATERIAL", "0", Number(roundTrip.RoundTripMismatchCount),
                    "In-memory save/reload material did not reproduce all source digests."));
            }
            if (request.Map19_01Unlocked || request.Map19_01Started)
            {
                findings.Add(Finding("MAP19_01", "MAP19_01_REMAINS_LOCKED",
                    "MAP19_01", "UNLOCKED=0/STARTED=0",
                    "UNLOCKED=" + Bool(request.Map19_01Unlocked) + "/STARTED=" +
                        Bool(request.Map19_01Started),
                    "MAP18 exit audit cannot unlock or start MAP19_01."));
            }

            if (findings.Count > 0)
                return Result(GeneratedPopulationExitAuditStatus.Fail, null, findings);

            var riskNotes = new[]
            {
                "REFERENCE_FIXTURE_ONLY_NO_PRODUCTION_SEED_APPROVAL",
                "PRIVATE_PRIOR_TEST_FIXTURE_DUPLICATION_REMAINS_CLEANUP_CANDIDATE",
            };
            var surface = new GeneratedPopulationExitAuditSurface(special, digests,
                counts, roundTrip, occupiedReuse, requiredReuse, spawnDuplicates,
                runtimeDuplicates, saveDuplicates, persistenceDuplicates, rowDuplicates,
                legacyKeys, canonicalChecks, riskNotes);
            return Result(GeneratedPopulationExitAuditStatus.Pass, surface, findings);
        }

        private static IReadOnlyList<GeneratedPopulationExitAuditDigestEntry> BuildDigests(
            GeneratedPopulationExitAuditRequest request,
            GeneratedContentSlotIndex index,
            GeneratedMandatoryUniquePlacementPlan mandatory,
            GeneratedPopulationPlacementPlan population,
            GeneratedHazardEnemyPlacementPlan hazardEnemy,
            GeneratedActivityEventRuntimeStateSurface runtime,
            GeneratedSpecialStateExportSurface special) =>
            new ReadOnlyCollection<GeneratedPopulationExitAuditDigestEntry>(new[]
            {
                Digest(request, "MAP18_01", SlotIndexDigestInvariant,
                    ExpectedSlotIndexDigest, index.Digest),
                Digest(request, "MAP18_01", SlotStableIdSetDigestInvariant,
                    ExpectedSlotStableIdSetDigest, index.StableIdSetDigest),
                Digest(request, "MAP18_02", MandatoryPlacementDigestInvariant,
                    ExpectedMandatoryPlacementDigest, mandatory.Digest),
                Digest(request, "MAP18_02", MandatoryStableIdSetDigestInvariant,
                    ExpectedMandatoryStableIdSetDigest, mandatory.StableIdSetDigest),
                Digest(request, "MAP18_03", PopulationPlanDigestInvariant,
                    ExpectedPopulationPlanDigest, population.Digest),
                Digest(request, "MAP18_03", PopulationOccupiedDigestInvariant,
                    ExpectedPopulationOccupiedDigest, population.OccupiedSurfaceDigest),
                Digest(request, "MAP18_04", HazardEnemyPlanDigestInvariant,
                    ExpectedHazardEnemyPlanDigest, hazardEnemy.Digest),
                Digest(request, "MAP18_04", HazardEnemyOccupiedDigestInvariant,
                    ExpectedHazardEnemyOccupiedDigest, hazardEnemy.OccupiedSurfaceDigest),
                Digest(request, "MAP18_04", BudgetLedgerDigestInvariant,
                    ExpectedBudgetLedgerDigest, hazardEnemy.BudgetLedger.Digest),
                Digest(request, "MAP18_05", RuntimeStateDigestInvariant,
                    ExpectedRuntimeStateDigest, runtime.Digest),
                Digest(request, "MAP18_05", RuntimeSaveKeyDigestInvariant,
                    ExpectedRuntimeSaveKeyDigest, runtime.SaveKeySetDigest),
                Digest(request, "MAP18_05", RuntimeExportDigestInvariant,
                    ExpectedRuntimeExportDigest, runtime.ExportSurfaceDigest),
                Digest(request, "MAP18_06", SpecialExportDigestInvariant,
                    ExpectedSpecialExportDigest, special.Digest),
                Digest(request, "MAP18_06", CsvMaterialDigestInvariant,
                    ExpectedCsvMaterialDigest, special.CsvMaterial.Digest),
                Digest(request, "MAP18_06", DebugSnapshotDigestInvariant,
                    ExpectedDebugSnapshotDigest, special.DebugSnapshot.Digest),
                Digest(request, "MAP18_06", Map18_07AuditSurfaceDigestInvariant,
                    ExpectedMap18_07AuditSurfaceDigest,
                    special.Map18_07AuditSurfaceDigest),
            }.OrderBy(value => value).ToArray());

        private static IReadOnlyList<GeneratedPopulationExitAuditCountEntry> BuildCounts(
            GeneratedPopulationExitAuditRequest request,
            GeneratedContentSlotIndex index,
            GeneratedMandatoryUniquePlacementPlan mandatory,
            GeneratedPopulationPlacementPlan population,
            GeneratedHazardEnemyPlacementPlan hazardEnemy,
            GeneratedActivityEventRuntimeStateSurface runtime,
            GeneratedSpecialStateExportSurface special) =>
            new ReadOnlyCollection<GeneratedPopulationExitAuditCountEntry>(new[]
            {
                Count(request, "MAP18_01", SlotSourceRecordCountInvariant,
                    ExpectedSlotSourceRecordCount, index.Count),
                Count(request, "MAP18_01", MandatoryCandidateCountInvariant,
                    ExpectedMandatoryCandidateCount, index.MandatoryUniqueCandidates().Count),
                Count(request, "MAP18_02", MandatoryPlacementCountInvariant,
                    ExpectedMandatoryPlacementCount, mandatory.EntryCount),
                Count(request, "MAP18_03", PopulationPlacementCountInvariant,
                    ExpectedPopulationPlacementCount, population.EntryCount),
                Count(request, "MAP18_04", HazardEnemyPlacementCountInvariant,
                    ExpectedHazardEnemyPlacementCount, hazardEnemy.EntryCount),
                Count(request, "MAP18_04", OccupiedSurfaceCountInvariant,
                    ExpectedOccupiedSurfaceCount, hazardEnemy.OccupiedSurfaceCount),
                Count(request, "MAP18_05", RuntimeStateRecordCountInvariant,
                    ExpectedRuntimeStateRecordCount, runtime.TotalRuntimeStateRecordCount),
                Count(request, "MAP18_06", SpecialExportRowCountInvariant,
                    ExpectedSpecialExportRowCount, special.TotalExportRowCount),
                Count(request, "MAP18_06", CsvMaterialRowCountInvariant,
                    ExpectedCsvMaterialRowCount, special.CsvMaterial.RowCount),
                Count(request, "MAP18_06", DebugSnapshotSectionCountInvariant,
                    ExpectedDebugSnapshotSectionCount, special.DebugSnapshot.SectionCount),
            }.OrderBy(value => value).ToArray());

        private static GeneratedPopulationExitAuditDigestEntry Digest(
            GeneratedPopulationExitAuditRequest request,
            string owner,
            string invariant,
            string expected,
            string actual) => new GeneratedPopulationExitAuditDigestEntry(owner, invariant,
                request.Expected(invariant, expected), actual);

        private static GeneratedPopulationExitAuditCountEntry Count(
            GeneratedPopulationExitAuditRequest request,
            string owner,
            string invariant,
            int expected,
            int actual)
        {
            var requested = request.Expected(invariant, Number(expected));
            int parsed;
            return new GeneratedPopulationExitAuditCountEntry(owner, invariant,
                int.TryParse(requested, NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out parsed) ? parsed : int.MinValue, actual);
        }

        private static void ValidateOverrides(
            GeneratedPopulationExitAuditRequest request,
            ICollection<GeneratedPopulationExitAuditFinding> findings)
        {
            foreach (var group in request.ExpectedOverrides.GroupBy(value =>
                value.Invariant, StringComparer.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(group.Key) || group.Count() != 1)
                    findings.Add(Finding("MAP18_07", "EXPECTED_OVERRIDE_UNIQUE",
                        string.IsNullOrWhiteSpace(group.Key) ? "EMPTY" : group.Key,
                        "ONE_NAMED_OVERRIDE", Number(group.Count()),
                        "Expected override keys must be non-empty and unique."));
            }
        }

        private static void ValidateDigestAndCountEntries(
            IEnumerable<GeneratedPopulationExitAuditDigestEntry> digests,
            IEnumerable<GeneratedPopulationExitAuditCountEntry> counts,
            ICollection<GeneratedPopulationExitAuditFinding> findings)
        {
            foreach (var digest in digests.Where(value => !value.Matches))
                findings.Add(Finding(digest.Owner, digest.Invariant, digest.Invariant,
                    digest.Expected, digest.Actual,
                    "MAP18 handoff digest does not match the approved value."));
            foreach (var count in counts.Where(value => !value.Matches))
                findings.Add(Finding(count.Owner, count.Invariant, count.Invariant,
                    Number(count.Expected), Number(count.Actual),
                    "MAP18 required surface count does not match the approved value."));
        }

        private static void ValidateResponsibilityChain(
            GeneratedContentSlotIndex index,
            GeneratedMandatoryUniquePlacementPlan mandatory,
            GeneratedPopulationPlacementPlan population,
            GeneratedHazardEnemyPlacementPlan hazardEnemy,
            GeneratedActivityEventRuntimeStateSurface runtime,
            GeneratedSpecialStateExportSurface special,
            ICollection<GeneratedPopulationExitAuditFinding> findings)
        {
            var links = new[]
            {
                ReferenceEquals(index, mandatory.SourceIndex),
                ReferenceEquals(index, population.SourceSlotIndex),
                ReferenceEquals(mandatory, population.MandatoryPlan),
                ReferenceEquals(population, hazardEnemy.PopulationPlan),
                ReferenceEquals(hazardEnemy, runtime.HazardEnemyPlan),
                ReferenceEquals(runtime, special.RuntimeStateSurface),
            };
            if (links.Any(value => !value))
                findings.Add(Finding("MAP18_01_06", "RESPONSIBILITY_CHAIN",
                    "OBJECT_REFERENCE_CHAIN", "6/6", Number(links.Count(value => value)),
                    "MAP18 audit inputs must preserve the upstream owner chain."));
        }

        private static void ValidateRuntimeBoundary(
            GeneratedPopulationExitAuditRequest request,
            GeneratedSpecialStateExportSurface special,
            GeneratedHazardEnemyPlacementPlan hazardEnemy,
            GeneratedPopulationPlacementPlan population,
            ICollection<GeneratedPopulationExitAuditFinding> findings)
        {
            var upstreamSideEffects = new[]
            {
                special.RuntimeObjectSpawnCount,
                special.GameObjectInstantiateCount,
                special.GameObjectEnableCount,
                special.GameObjectDisableCount,
                special.GameObjectDestroyCount,
                special.SystemIoFileWriteCount,
                special.SystemIoFileReadCount,
                special.DiskSaveFileCreateCount,
                special.DiskLoadFileCreateCount,
                special.ActualCsvFileWriteCount,
                special.ActualCsvFileReadCount,
                special.GeneratedCsvFileCommitCount,
                special.SaveWriteCount,
                special.SaveReadCount,
                special.PlayerPrefsWriteCount,
                special.PlayerPrefsReadCount,
                special.RuntimeSpecialRegionSpawnCount,
                special.RuntimeVillageSpawnCount,
                special.RuntimeResourceSpawnCount,
                special.RuntimeForgeSpawnCount,
                special.RuntimeBossSpawnCount,
                special.RuntimeActivityPrefabSpawnCount,
                special.RuntimeEventPrefabSpawnCount,
                special.ActualEventActivationCount,
                special.RewardGrantCount,
                special.InventoryMutationCount,
                special.ResourceMutationCount,
                special.DamageExecutionCount,
                special.EnemyAiControllerHookupCount,
                population.ActualShopTransactionCount,
                hazardEnemy.ActualCombatEncounterCount,
                special.TilemapComponentWriteCount,
                special.TilemapSetTileCallCount,
                special.TilemapSetTilesCallCount,
                special.TilemapSetTilesBlockCallCount,
                special.TilemapClearAllTilesCallCount,
                special.TilemapColliderCreationCount,
                special.CompositeColliderCreationCount,
                special.ColliderCreationCount,
                special.RigidbodyCreationCount,
                special.PhysicsQueryCount,
                special.PhysicsSimulationCount,
                special.NavMeshSetupCount,
                special.PathfindingSetupCount,
                special.SceneMutationCount,
                special.PrefabMutationCount,
                special.TilemapMutationCount,
                special.CameraReadCount,
                special.CameraWriteCount,
                special.AddressablesLoadCount,
                special.ResourcesLoadCount,
                special.AssetDatabaseLoadCount,
                special.ProductionSeedApprovalCount,
                special.PriorTaskTestSelectionCount,
                special.Legacy19347SelectionCount,
                special.PlayModeSelectionCount,
                special.UnfilteredTestSelectionCount,
                special.FullRegressionRunCount,
            }.Sum();
            if (upstreamSideEffects != 0 || request.AttemptedRuntimeSideEffect ||
                request.AttemptedCsvFileIo || request.AttemptedSaveFileIo)
                findings.Add(Finding("MAP18_01_07", "RUNTIME_SIDE_EFFECT_BOUNDARY",
                    "SIDE_EFFECT_COUNTERS", "0",
                    Number(upstreamSideEffects) + "/RUNTIME=" +
                        Bool(request.AttemptedRuntimeSideEffect) + "/CSV_IO=" +
                        Bool(request.AttemptedCsvFileIo) + "/SAVE_IO=" +
                        Bool(request.AttemptedSaveFileIo),
                    "MAP18 exit audit is logical data validation only."));
        }

        private static void ValidateZero(
            string owner,
            string invariant,
            int actual,
            ICollection<GeneratedPopulationExitAuditFinding> findings)
        {
            if (actual != 0)
                findings.Add(Finding(owner, invariant, invariant, "0", Number(actual),
                    "Identity and reservation collisions are forbidden."));
        }

        private static int DuplicateCount(IEnumerable<string> values) => values
            .Where(value => !string.IsNullOrEmpty(value))
            .GroupBy(value => value, StringComparer.Ordinal)
            .Count(value => value.Count() > 1);

        private static GeneratedPopulationExitAuditFinding Finding(
            string owner,
            string invariant,
            string offendingKey,
            string expected,
            string actual,
            string reason) => new GeneratedPopulationExitAuditFinding(
                GeneratedPopulationExitAuditSeverity.Error, owner, invariant,
                offendingKey, expected, actual, reason);

        private static GeneratedPopulationExitAuditResult Result(
            GeneratedPopulationExitAuditStatus status,
            GeneratedPopulationExitAuditSurface surface,
            IEnumerable<GeneratedPopulationExitAuditFinding> findings) =>
            new GeneratedPopulationExitAuditResult(status, surface, findings);

        private static string Number(int value) =>
            value.ToString(CultureInfo.InvariantCulture);
        private static string Bool(bool value) => value ? "1" : "0";
    }
}
