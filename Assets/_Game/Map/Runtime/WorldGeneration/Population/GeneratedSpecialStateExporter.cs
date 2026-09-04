using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.WorldGeneration.Population
{
    public sealed class GeneratedSpecialStateExportRequest
    {
        private readonly ReadOnlyCollection<CoreResourceRegionDefinition> coreResources;
        private readonly ReadOnlyCollection<GeneratedDeclaredSpecialStateSource> declaredSources;
        private readonly ReadOnlyCollection<string> existingRowKeys;
        private readonly ReadOnlyCollection<string> existingPersistenceKeys;
        private readonly ReadOnlyCollection<string> existingRuntimeStateIds;
        private readonly ReadOnlyCollection<string> existingSaveKeys;
        private readonly ReadOnlyCollection<string> existingStableSpawnIds;

        public GeneratedSpecialStateExportRequest(
            GeneratedActivityEventRuntimeStateSurface runtimeStateSurface,
            IEnumerable<CoreResourceRegionDefinition> sourceCoreResources,
            IEnumerable<GeneratedDeclaredSpecialStateSource> sourceDeclaredSources,
            string expectedRuntimeStateSurfaceDigest,
            string expectedSaveKeySetDigest,
            string expectedRuntimeExportSurfaceDigest,
            string expectedOccupiedSurfaceDigest,
            string expectedBudgetLedgerDigest,
            string expectedCsvHeader,
            IEnumerable<string> sourceExistingRowKeys = null,
            IEnumerable<string> sourceExistingPersistenceKeys = null,
            IEnumerable<string> sourceExistingRuntimeStateIds = null,
            IEnumerable<string> sourceExistingSaveKeys = null,
            IEnumerable<string> sourceExistingStableSpawnIds = null,
            string expectedSpecialExportSurfaceDigest = null,
            string expectedCsvMaterialDigest = null,
            string expectedDebugSnapshotDigest = null,
            string expectedAuditSurfaceDigest = null,
            bool attemptedCsvFileWrite = false,
            bool attemptedCsvFileRead = false,
            bool attemptedSaveWrite = false,
            bool attemptedSaveRead = false,
            bool attemptedRuntimeSpawn = false,
            bool attemptedRewardGrant = false,
            bool attemptedDamage = false,
            bool attemptedPhysics = false,
            bool attemptedAiHookup = false,
            bool attemptedEventExecution = false)
        {
            RuntimeStateSurface = runtimeStateSurface;
            var coreArray = sourceCoreResources == null
                ? Array.Empty<CoreResourceRegionDefinition>()
                : sourceCoreResources.ToArray();
            var declaredArray = sourceDeclaredSources == null
                ? Array.Empty<GeneratedDeclaredSpecialStateSource>()
                : sourceDeclaredSources.ToArray();
            coreResources = new ReadOnlyCollection<CoreResourceRegionDefinition>(coreArray
                .Where(value => value != null).OrderBy(value => value.RegionId).ToArray());
            declaredSources = new ReadOnlyCollection<GeneratedDeclaredSpecialStateSource>(
                declaredArray.Where(value => value != null).OrderBy(value => value).ToArray());
            NullCoreResourceCount = coreArray.Count(value => value == null);
            NullDeclaredSourceCount = declaredArray.Count(value => value == null);
            ExistingRowKeys = existingRowKeys = Freeze(sourceExistingRowKeys);
            ExistingPersistenceKeys = existingPersistenceKeys =
                Freeze(sourceExistingPersistenceKeys);
            ExistingRuntimeStateIds = existingRuntimeStateIds =
                Freeze(sourceExistingRuntimeStateIds);
            ExistingSaveKeys = existingSaveKeys = Freeze(sourceExistingSaveKeys);
            ExistingStableSpawnIds = existingStableSpawnIds =
                Freeze(sourceExistingStableSpawnIds);
            ExpectedRuntimeStateSurfaceDigest = expectedRuntimeStateSurfaceDigest ?? string.Empty;
            ExpectedSaveKeySetDigest = expectedSaveKeySetDigest ?? string.Empty;
            ExpectedRuntimeExportSurfaceDigest = expectedRuntimeExportSurfaceDigest ?? string.Empty;
            ExpectedOccupiedSurfaceDigest = expectedOccupiedSurfaceDigest ?? string.Empty;
            ExpectedBudgetLedgerDigest = expectedBudgetLedgerDigest ?? string.Empty;
            ExpectedCsvHeader = expectedCsvHeader ?? string.Empty;
            ExpectedSpecialExportSurfaceDigest = expectedSpecialExportSurfaceDigest ?? string.Empty;
            ExpectedCsvMaterialDigest = expectedCsvMaterialDigest ?? string.Empty;
            ExpectedDebugSnapshotDigest = expectedDebugSnapshotDigest ?? string.Empty;
            ExpectedAuditSurfaceDigest = expectedAuditSurfaceDigest ?? string.Empty;
            AttemptedCsvFileWrite = attemptedCsvFileWrite;
            AttemptedCsvFileRead = attemptedCsvFileRead;
            AttemptedSaveWrite = attemptedSaveWrite;
            AttemptedSaveRead = attemptedSaveRead;
            AttemptedRuntimeSpawn = attemptedRuntimeSpawn;
            AttemptedRewardGrant = attemptedRewardGrant;
            AttemptedDamage = attemptedDamage;
            AttemptedPhysics = attemptedPhysics;
            AttemptedAiHookup = attemptedAiHookup;
            AttemptedEventExecution = attemptedEventExecution;
        }

        public GeneratedActivityEventRuntimeStateSurface RuntimeStateSurface { get; }
        public IReadOnlyList<CoreResourceRegionDefinition> CoreResources => coreResources;
        public IReadOnlyList<GeneratedDeclaredSpecialStateSource> DeclaredSources =>
            declaredSources;
        public IReadOnlyList<string> ExistingRowKeys { get; }
        public IReadOnlyList<string> ExistingPersistenceKeys { get; }
        public IReadOnlyList<string> ExistingRuntimeStateIds { get; }
        public IReadOnlyList<string> ExistingSaveKeys { get; }
        public IReadOnlyList<string> ExistingStableSpawnIds { get; }
        public int NullCoreResourceCount { get; }
        public int NullDeclaredSourceCount { get; }
        public string ExpectedRuntimeStateSurfaceDigest { get; }
        public string ExpectedSaveKeySetDigest { get; }
        public string ExpectedRuntimeExportSurfaceDigest { get; }
        public string ExpectedOccupiedSurfaceDigest { get; }
        public string ExpectedBudgetLedgerDigest { get; }
        public string ExpectedCsvHeader { get; }
        public string ExpectedSpecialExportSurfaceDigest { get; }
        public string ExpectedCsvMaterialDigest { get; }
        public string ExpectedDebugSnapshotDigest { get; }
        public string ExpectedAuditSurfaceDigest { get; }
        public bool AttemptedCsvFileWrite { get; }
        public bool AttemptedCsvFileRead { get; }
        public bool AttemptedSaveWrite { get; }
        public bool AttemptedSaveRead { get; }
        public bool AttemptedRuntimeSpawn { get; }
        public bool AttemptedRewardGrant { get; }
        public bool AttemptedDamage { get; }
        public bool AttemptedPhysics { get; }
        public bool AttemptedAiHookup { get; }
        public bool AttemptedEventExecution { get; }

        private static ReadOnlyCollection<string> Freeze(IEnumerable<string> source) =>
            new ReadOnlyCollection<string>((source ?? Array.Empty<string>())
                .Select(value => value ?? string.Empty)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
    }

    public enum GeneratedSpecialStateExportFailureCode
    {
        MissingRequest = 1,
        MissingRuntimeStateSurface = 2,
        RuntimeStateSurfaceDigestMismatch = 3,
        SaveKeySetDigestMismatch = 4,
        RuntimeExportSurfaceDigestMismatch = 5,
        OccupiedSurfaceDigestMismatch = 6,
        BudgetLedgerDigestMismatch = 7,
        MissingCoreResourcePersistenceKey = 8,
        LegacyShortPersistenceKey = 9,
        MissingDeclaredSpecialSource = 10,
        InvalidDeclaredSpecialSource = 11,
        DuplicateExportRowKey = 12,
        DuplicatePersistenceKey = 13,
        DuplicateRuntimeStateId = 14,
        DuplicateSaveKey = 15,
        DuplicateStableSpawnId = 16,
        InvalidCsvHeaderOrRowShape = 17,
        AttemptedFileSaveOrRuntimeSideEffect = 18,
        SpecialExportSurfaceDigestMismatch = 19,
        CsvMaterialDigestMismatch = 20,
        DebugSnapshotDigestMismatch = 21,
        AuditSurfaceDigestMismatch = 22,
    }

    public sealed class GeneratedSpecialStateExportFailure :
        IComparable<GeneratedSpecialStateExportFailure>
    {
        internal GeneratedSpecialStateExportFailure(
            GeneratedSpecialStateExportFailureCode code,
            string owner,
            string offendingKey,
            string expected,
            string actual,
            string reason)
        {
            Code = code;
            Owner = owner ?? string.Empty;
            OffendingKey = offendingKey ?? string.Empty;
            Expected = expected ?? string.Empty;
            Actual = actual ?? string.Empty;
            Reason = reason ?? string.Empty;
            StableToken = string.Join("|", new[]
            {
                "MAP18_06_FAILURE_V1", Code.ToString(), Owner, OffendingKey,
                Expected, Actual, Reason,
            });
        }

        public GeneratedSpecialStateExportFailureCode Code { get; }
        public string Owner { get; }
        public string OffendingKey { get; }
        public string Expected { get; }
        public string Actual { get; }
        public string Reason { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedSpecialStateExportFailure other) => other == null
            ? -1 : string.Compare(StableToken, other.StableToken,
                StringComparison.Ordinal);
        public override string ToString() => StableToken;
    }

    public sealed class GeneratedSpecialStateExportResult
    {
        private readonly ReadOnlyCollection<GeneratedSpecialStateExportFailure> failures;

        internal GeneratedSpecialStateExportResult(
            GeneratedSpecialStateExportSurface surface,
            IEnumerable<GeneratedSpecialStateExportFailure> sourceFailures)
        {
            Surface = surface;
            failures = new ReadOnlyCollection<GeneratedSpecialStateExportFailure>(
                (sourceFailures ?? Array.Empty<GeneratedSpecialStateExportFailure>())
                .OrderBy(value => value).ToArray());
        }

        public bool Success => Surface != null && failures.Count == 0;
        public GeneratedSpecialStateExportSurface Surface { get; }
        public IReadOnlyList<GeneratedSpecialStateExportFailure> Failures => failures;
        public int PartialExportRowCount => Success ? Surface.TotalExportRowCount : 0;
        public int PartialCsvMaterialCount => Success ? 1 : 0;
        public int PartialDebugSnapshotCount => Success ? 1 : 0;
        public int RetryLoopCount => 0;
    }

    public static class GeneratedSpecialStateExporter
    {
        public const string ExpectedRuntimeStateSurfaceDigest =
            "2774cc515c4531ad90055afb2bdabb4a73439a0e64162194b4c9dbdd51db0f72";
        public const string ExpectedSaveKeySetDigest =
            "9c841116463551aff94fe77132c2b7b61d23b07840ee5aa29710799591b0d448";
        public const string ExpectedRuntimeExportSurfaceDigest =
            "2f2665b46b054f408f8e7a1fb97c128ca355b829aa74d4aa7811b2792b9f6d73";
        public const string ExpectedOccupiedSurfaceDigest =
            "39e530dde3a98191aee290916d536b4952034aa2c758cb7c35050d4e2f74b688";
        public const string ExpectedBudgetLedgerDigest =
            "08a4d128bb08324c99669c235101ead8d2c81d2f78d379b7b58fe59090bef52d";
        public const int ExpectedRuntimeExportRecordCount = 6;
        public const int ExpectedOccupiedSurfaceCount = 9;

        public static GeneratedSpecialStateExportResult Export(
            GeneratedSpecialStateExportRequest request)
        {
            var failures = new List<GeneratedSpecialStateExportFailure>();
            if (request == null)
            {
                failures.Add(Failure(GeneratedSpecialStateExportFailureCode.MissingRequest,
                    "MAP18_06", "REQUEST", "PRESENT", "MISSING",
                    "Special state export request is required."));
                return Result(null, failures);
            }

            ValidateRuntimeSurface(request, failures);
            ValidateCoreResources(request, failures);
            ValidateDeclaredSources(request, failures);
            ValidateCsvHeader(request, failures);
            ValidateSideEffects(request, failures);
            if (failures.Count > 0) return Result(null, failures);

            var rows = CreateRows(request);
            ValidateRows(request, rows, failures);
            if (failures.Count > 0) return Result(null, failures);

            var csv = new GeneratedSpawnStateCsvMaterial(request.ExpectedCsvHeader, rows);
            if (csv.HeaderColumnCount != 11 || csv.RowCount != rows.Count ||
                !csv.IsLfNormalized || csv.HasUtf8Bom)
                failures.Add(Failure(
                    GeneratedSpecialStateExportFailureCode.InvalidCsvHeaderOrRowShape,
                    "MAP18_06_CSV", "CSV_MATERIAL", "11_COLUMNS_LF_UTF8_NO_BOM",
                    Number(csv.HeaderColumnCount) + "/" + Flag(csv.IsLfNormalized) +
                        "/" + Flag(!csv.HasUtf8Bom),
                    "CSV material must remain deterministic and in memory."));
            if (failures.Count > 0) return Result(null, failures);

            var surface = new GeneratedSpecialStateExportSurface(
                request.RuntimeStateSurface, rows, csv);
            ValidateExpectedOutput(request, surface, failures);
            return failures.Count == 0 ? Result(surface, failures) : Result(null, failures);
        }

        private static void ValidateRuntimeSurface(
            GeneratedSpecialStateExportRequest request,
            ICollection<GeneratedSpecialStateExportFailure> failures)
        {
            var surface = request.RuntimeStateSurface;
            if (surface == null)
            {
                failures.Add(Failure(
                    GeneratedSpecialStateExportFailureCode.MissingRuntimeStateSurface,
                    "MAP18_05", "RUNTIME_STATE_SURFACE", "PRESENT", "MISSING",
                    "Reviewed runtime state export surface is required."));
                return;
            }
            ValidateDigest(request.ExpectedRuntimeStateSurfaceDigest, surface.Digest,
                ExpectedRuntimeStateSurfaceDigest,
                GeneratedSpecialStateExportFailureCode.RuntimeStateSurfaceDigestMismatch,
                "MAP18_05", "RUNTIME_STATE_SURFACE_DIGEST", failures);
            ValidateDigest(request.ExpectedSaveKeySetDigest, surface.SaveKeySetDigest,
                ExpectedSaveKeySetDigest,
                GeneratedSpecialStateExportFailureCode.SaveKeySetDigestMismatch,
                "MAP18_05", "SAVE_KEY_SET_DIGEST", failures);
            ValidateDigest(request.ExpectedRuntimeExportSurfaceDigest,
                surface.ExportSurfaceDigest, ExpectedRuntimeExportSurfaceDigest,
                GeneratedSpecialStateExportFailureCode.RuntimeExportSurfaceDigestMismatch,
                "MAP18_05", "RUNTIME_EXPORT_SURFACE_DIGEST", failures);
            ValidateDigest(request.ExpectedOccupiedSurfaceDigest,
                surface.OccupiedSurfaceDigest, ExpectedOccupiedSurfaceDigest,
                GeneratedSpecialStateExportFailureCode.OccupiedSurfaceDigestMismatch,
                "MAP18_04", "OCCUPIED_SURFACE_DIGEST", failures);
            ValidateDigest(request.ExpectedBudgetLedgerDigest,
                surface.BudgetLedgerDigest, ExpectedBudgetLedgerDigest,
                GeneratedSpecialStateExportFailureCode.BudgetLedgerDigestMismatch,
                "MAP18_04", "BUDGET_LEDGER_DIGEST", failures);
            if (surface.Map18_06ExportSurfaceRecordCount !=
                    ExpectedRuntimeExportRecordCount ||
                surface.OccupiedSurfaceCount != ExpectedOccupiedSurfaceCount)
                failures.Add(Failure(
                    GeneratedSpecialStateExportFailureCode.RuntimeExportSurfaceDigestMismatch,
                    "MAP18_05", "RUNTIME_OCCUPIED_COUNTS",
                    Number(ExpectedRuntimeExportRecordCount) + "/" +
                        Number(ExpectedOccupiedSurfaceCount),
                    Number(surface.Map18_06ExportSurfaceRecordCount) + "/" +
                        Number(surface.OccupiedSurfaceCount),
                    "All runtime records and occupied reservations must be preserved."));
        }

        private static void ValidateCoreResources(
            GeneratedSpecialStateExportRequest request,
            ICollection<GeneratedSpecialStateExportFailure> failures)
        {
            var resources = request.CoreResources;
            if (request.NullCoreResourceCount > 0 || resources.Count != 3 ||
                resources.Select(value => value.Resource).Distinct().Count() != 3)
                failures.Add(Failure(
                    GeneratedSpecialStateExportFailureCode.MissingCoreResourcePersistenceKey,
                    "MAP13_CORE_RESOURCE", "CORE_RESOURCE_SET", "3_UNIQUE_NO_NULLS",
                    Number(resources.Count) + "/NULL=" +
                        Number(request.NullCoreResourceCount),
                    "MoonCore, CassiaSap, and StarNuruk authoring are required."));
            foreach (var resource in resources)
            {
                var actual = resource.RequiredReward == null
                    ? string.Empty : resource.RequiredReward.PersistenceKey.Value;
                var expected = ExpectedCoreResourceKey(resource.Resource);
                if (!string.Equals(actual, expected, StringComparison.Ordinal))
                    failures.Add(Failure(
                        GeneratedSpecialStateExportFailureCode
                            .MissingCoreResourcePersistenceKey,
                        "MAP13_CORE_RESOURCE", resource.Resource.ToString(), expected,
                        actual, "Authoritative CoreResource persistence key is required."));
                ValidateCanonicalKey(actual, resource.RegionId.Value, failures);
            }
        }

        private static void ValidateDeclaredSources(
            GeneratedSpecialStateExportRequest request,
            ICollection<GeneratedSpecialStateExportFailure> failures)
        {
            var required = new[]
            {
                GeneratedSpecialStateExportKind.Forge,
                GeneratedSpecialStateExportKind.Boss,
                GeneratedSpecialStateExportKind.Village,
            };
            if (request.NullDeclaredSourceCount > 0)
                failures.Add(Failure(
                    GeneratedSpecialStateExportFailureCode.MissingDeclaredSpecialSource,
                    "MAP13_SPECIAL", "NULL_DECLARED_SOURCES", "0",
                    Number(request.NullDeclaredSourceCount),
                    "Declared special sources cannot contain null."));
            foreach (var kind in required)
            {
                var matches = request.DeclaredSources.Where(value =>
                    value.Kind == kind).ToArray();
                if (matches.Length != 1)
                    failures.Add(Failure(
                        GeneratedSpecialStateExportFailureCode.MissingDeclaredSpecialSource,
                        "MAP13_SPECIAL", kind.ToString(), "1", Number(matches.Length),
                        "Each special export group requires one explicit source status."));
            }
            foreach (var source in request.DeclaredSources)
            {
                var validKind = required.Contains(source.Kind);
                var validCommon = !string.IsNullOrWhiteSpace(source.SourceOwner) &&
                    !string.IsNullOrWhiteSpace(source.RegionSiteId) &&
                    !string.IsNullOrWhiteSpace(source.StateKind) &&
                    !string.IsNullOrWhiteSpace(source.SourceDigest);
                var validStatus = source.Status == GeneratedSpecialStateSourceStatus.Active
                    ? !string.IsNullOrEmpty(source.PersistenceKey.Value)
                    : string.IsNullOrEmpty(source.PersistenceKey.Value);
                if (!validKind || !validCommon || !validStatus)
                    failures.Add(Failure(
                        GeneratedSpecialStateExportFailureCode.InvalidDeclaredSpecialSource,
                        source.SourceOwner, source.StableToken,
                        "KNOWN_KIND_ID_STATE_DIGEST_AND_STATUS_KEY_CONTRACT", "INVALID",
                        "Declared source is incomplete or owns an invalid persistence key."));
                ValidateCanonicalKey(source.PersistenceKey.Value,
                    source.RegionSiteId, failures);
            }
        }

        private static void ValidateCanonicalKey(
            string key,
            string owner,
            ICollection<GeneratedSpecialStateExportFailure> failures)
        {
            if (!string.IsNullOrEmpty(key) &&
                !key.StartsWith("SR_STATE_", StringComparison.Ordinal))
                failures.Add(Failure(
                    GeneratedSpecialStateExportFailureCode.LegacyShortPersistenceKey,
                    owner, key, "SR_STATE_*", key,
                    "Legacy short persistence keys are not accepted."));
        }

        private static void ValidateCsvHeader(
            GeneratedSpecialStateExportRequest request,
            ICollection<GeneratedSpecialStateExportFailure> failures)
        {
            if (!string.Equals(request.ExpectedCsvHeader,
                    GeneratedSpawnStateCsvMaterial.CanonicalHeader,
                    StringComparison.Ordinal))
                failures.Add(Failure(
                    GeneratedSpecialStateExportFailureCode.InvalidCsvHeaderOrRowShape,
                    "MAP18_06_CSV", "CSV_HEADER",
                    GeneratedSpawnStateCsvMaterial.CanonicalHeader,
                    request.ExpectedCsvHeader,
                    "CSV header and column order are versioned."));
        }

        private static void ValidateSideEffects(
            GeneratedSpecialStateExportRequest request,
            ICollection<GeneratedSpecialStateExportFailure> failures)
        {
            var actual = string.Join("/", new[]
            {
                Flag(request.AttemptedCsvFileWrite), Flag(request.AttemptedCsvFileRead),
                Flag(request.AttemptedSaveWrite), Flag(request.AttemptedSaveRead),
                Flag(request.AttemptedRuntimeSpawn), Flag(request.AttemptedRewardGrant),
                Flag(request.AttemptedDamage), Flag(request.AttemptedPhysics),
                Flag(request.AttemptedAiHookup), Flag(request.AttemptedEventExecution),
            });
            if (!string.Equals(actual, "0/0/0/0/0/0/0/0/0/0",
                    StringComparison.Ordinal))
                failures.Add(Failure(
                    GeneratedSpecialStateExportFailureCode
                        .AttemptedFileSaveOrRuntimeSideEffect,
                    "MAP18_07_OR_LATER", "CSV_SAVE_SPAWN_REWARD_DAMAGE_PHYSICS_AI_EVENT",
                    "0/0/0/0/0/0/0/0/0/0", actual,
                    "MAP18_06 publishes pure-data material only."));
        }

        private static List<GeneratedSpecialStateExportRow> CreateRows(
            GeneratedSpecialStateExportRequest request)
        {
            var rows = new List<GeneratedSpecialStateExportRow>();
            var runtime = request.RuntimeStateSurface;
            var mandatory = runtime.HazardEnemyPlan.PopulationPlan.MandatoryPlan;
            foreach (var resource in request.CoreResources)
            {
                var placement = mandatory.Entries.Single(value =>
                    value.ContentKey.CoreResource == resource.Resource);
                rows.Add(new GeneratedSpecialStateExportRow(
                    GeneratedSpecialStateExportKind.CoreResource,
                    resource.RegionId.Value, "MAP13_CORE_RESOURCE",
                    GeneratedSpecialStateSourceStatus.Active,
                    resource.RequiredReward.PersistenceKey,
                    placement.StableSpawnId.Value, string.Empty, string.Empty,
                    "REQUIRED_REWARD_AVAILABLE",
                    resource.RequiredReward.RewardId + "|" +
                        resource.RequiredReward.PersistenceKey.Value));
            }
            rows.AddRange(request.DeclaredSources.Select(source =>
                new GeneratedSpecialStateExportRow(source.Kind, source.RegionSiteId,
                    source.SourceOwner, source.Status, source.PersistenceKey,
                    string.Empty, string.Empty, string.Empty, source.StateKind,
                    source.SourceDigest)));
            rows.AddRange(runtime.Map18_06ExportRecords.Select(value =>
                new GeneratedSpecialStateExportRow(
                    GeneratedSpecialStateExportKind.ActivityEventRuntime,
                    value.SourceId, "MAP18_05_RUNTIME_STATE",
                    GeneratedSpecialStateSourceStatus.Active,
                    default(SpecialPersistenceKey), string.Empty,
                    value.RuntimeStateId.Value, value.SaveKey.Value, value.Kind,
                    value.StableToken)));

            var coreSpawnIds = new HashSet<string>(rows.Where(value =>
                value.Kind == GeneratedSpecialStateExportKind.CoreResource)
                .Select(value => value.StableSpawnId), StringComparer.Ordinal);
            rows.AddRange(runtime.OccupiedSurface.Where(value =>
                    !coreSpawnIds.Contains(value.StableSpawnId.Value))
                .Select(value => new GeneratedSpecialStateExportRow(
                    GeneratedSpecialStateExportKind.SpawnState,
                    value.ContentKey, value.Owner,
                    GeneratedSpecialStateSourceStatus.Active,
                    default(SpecialPersistenceKey), value.StableSpawnId.Value,
                    string.Empty, string.Empty, "AVAILABLE", value.StableToken)));
            return rows.OrderBy(value => value).ToList();
        }

        private static void ValidateRows(
            GeneratedSpecialStateExportRequest request,
            IReadOnlyCollection<GeneratedSpecialStateExportRow> rows,
            ICollection<GeneratedSpecialStateExportFailure> failures)
        {
            ValidateUnique(request.ExistingRowKeys, rows.Select(value => value.RowKey),
                GeneratedSpecialStateExportFailureCode.DuplicateExportRowKey,
                "MAP18_06_ROWS", failures);
            ValidateUnique(request.ExistingPersistenceKeys, rows.Where(value =>
                    value.HasPersistenceKey).Select(value => value.PersistenceKey.Value),
                GeneratedSpecialStateExportFailureCode.DuplicatePersistenceKey,
                "MAP18_06_PERSISTENCE", failures);
            ValidateUnique(request.ExistingRuntimeStateIds, rows.Where(value =>
                    value.HasRuntimeStateId).Select(value => value.RuntimeStateId),
                GeneratedSpecialStateExportFailureCode.DuplicateRuntimeStateId,
                "MAP18_05_RUNTIME_ID", failures);
            ValidateUnique(request.ExistingSaveKeys, rows.Where(value => value.HasSaveKey)
                    .Select(value => value.SaveKey),
                GeneratedSpecialStateExportFailureCode.DuplicateSaveKey,
                "MAP18_05_SAVE_KEY", failures);
            ValidateUnique(request.ExistingStableSpawnIds, rows.Where(value =>
                    value.HasStableSpawnId).Select(value => value.StableSpawnId),
                GeneratedSpecialStateExportFailureCode.DuplicateStableSpawnId,
                "MAP18_SPAWN_ID", failures);
            foreach (var row in rows)
                if (row.RowKey.Length != 64 || row.RowDigest.Length != 64 ||
                    string.IsNullOrWhiteSpace(row.RegionSiteId) ||
                    string.IsNullOrWhiteSpace(row.SourceOwner) ||
                    string.IsNullOrWhiteSpace(row.StateKind) || row.RowVersion != "V1")
                    failures.Add(Failure(
                        GeneratedSpecialStateExportFailureCode.InvalidCsvHeaderOrRowShape,
                        "MAP18_06_ROWS", row.StableToken,
                        "ID_OWNER_STATE_V1_AND_SHA256", "INVALID",
                        "Export row shape is incomplete."));
        }

        private static void ValidateUnique(
            IEnumerable<string> existing,
            IEnumerable<string> generated,
            GeneratedSpecialStateExportFailureCode code,
            string owner,
            ICollection<GeneratedSpecialStateExportFailure> failures)
        {
            foreach (var group in existing.Concat(generated).GroupBy(value => value,
                         StringComparer.Ordinal).Where(value => value.Count() > 1))
                failures.Add(Failure(code, owner, group.Key, "UNIQUE",
                    Number(group.Count()), "Export identities must be globally unique."));
        }

        private static void ValidateExpectedOutput(
            GeneratedSpecialStateExportRequest request,
            GeneratedSpecialStateExportSurface surface,
            ICollection<GeneratedSpecialStateExportFailure> failures)
        {
            ValidateOptional(request.ExpectedSpecialExportSurfaceDigest, surface.Digest,
                GeneratedSpecialStateExportFailureCode.SpecialExportSurfaceDigestMismatch,
                "SPECIAL_EXPORT_SURFACE_DIGEST", failures);
            ValidateOptional(request.ExpectedCsvMaterialDigest, surface.CsvMaterial.Digest,
                GeneratedSpecialStateExportFailureCode.CsvMaterialDigestMismatch,
                "CSV_MATERIAL_DIGEST", failures);
            ValidateOptional(request.ExpectedDebugSnapshotDigest,
                surface.DebugSnapshot.Digest,
                GeneratedSpecialStateExportFailureCode.DebugSnapshotDigestMismatch,
                "DEBUG_SNAPSHOT_DIGEST", failures);
            ValidateOptional(request.ExpectedAuditSurfaceDigest,
                surface.Map18_07AuditSurfaceDigest,
                GeneratedSpecialStateExportFailureCode.AuditSurfaceDigestMismatch,
                "MAP18_07_AUDIT_SURFACE_DIGEST", failures);
        }

        private static void ValidateOptional(
            string expected,
            string actual,
            GeneratedSpecialStateExportFailureCode code,
            string key,
            ICollection<GeneratedSpecialStateExportFailure> failures)
        {
            if (!string.IsNullOrEmpty(expected) &&
                !string.Equals(expected, actual, StringComparison.Ordinal))
                failures.Add(Failure(code, "MAP18_06", key, expected, actual,
                    "Generated output differs from reviewed deterministic evidence."));
        }

        private static void ValidateDigest(
            string expected,
            string actual,
            string authority,
            GeneratedSpecialStateExportFailureCode code,
            string owner,
            string key,
            ICollection<GeneratedSpecialStateExportFailure> failures)
        {
            if (!string.Equals(expected, authority, StringComparison.Ordinal) ||
                !string.Equals(actual, authority, StringComparison.Ordinal))
                failures.Add(Failure(code, owner, key, authority,
                    expected + "/" + actual,
                    "Upstream digest differs from reviewed evidence."));
        }

        private static string ExpectedCoreResourceKey(CoreResourceKind resource)
        {
            switch (resource)
            {
                case CoreResourceKind.MoonCore:
                    return "SR_STATE_MOON_CORE_SITE_5_REWARD_MOON_CORE_REWARD";
                case CoreResourceKind.CassiaSap:
                    return "SR_STATE_CASSIA_SAP_SITE_5_REWARD_CASSIA_SAP_REWARD";
                case CoreResourceKind.StarNuruk:
                    return "SR_STATE_STAR_NURUK_SITE_5_REWARD_STAR_NURUK_REWARD";
                default:
                    return string.Empty;
            }
        }

        private static GeneratedSpecialStateExportFailure Failure(
            GeneratedSpecialStateExportFailureCode code,
            string owner,
            string key,
            string expected,
            string actual,
            string reason) => new GeneratedSpecialStateExportFailure(
                code, owner, key, expected, actual, reason);

        private static GeneratedSpecialStateExportResult Result(
            GeneratedSpecialStateExportSurface surface,
            IEnumerable<GeneratedSpecialStateExportFailure> failures) =>
            new GeneratedSpecialStateExportResult(surface, failures);

        private static string Number(int value) =>
            value.ToString(CultureInfo.InvariantCulture);
        private static string Flag(bool value) => value ? "1" : "0";
    }
}
