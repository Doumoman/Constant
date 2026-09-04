using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.Population
{
    public sealed class GeneratedActivityEventRuntimeStateRequest
    {
        private readonly ReadOnlyCollection<GeneratedActivityRuntimeSource> activitySources;
        private readonly ReadOnlyCollection<GeneratedEventRuntimeSource> eventSources;
        private readonly ReadOnlyCollection<GeneratedActivityRuntimeTransition> transitions;
        private readonly ReadOnlyCollection<GeneratedEventRuntimeVariant> eventVariants;
        private readonly ReadOnlyCollection<string> existingRuntimeStateIds;
        private readonly ReadOnlyCollection<string> existingSaveKeys;

        public GeneratedActivityEventRuntimeStateRequest(
            GeneratedHazardEnemyPlacementPlan hazardEnemyPlan,
            IEnumerable<GeneratedActivityRuntimeSource> sourceActivities,
            IEnumerable<GeneratedEventRuntimeSource> sourceEvents,
            IEnumerable<GeneratedActivityRuntimeTransition> allowedTransitions,
            IEnumerable<GeneratedEventRuntimeVariant> requiredEventVariants,
            string worldSeed,
            string generatorVersion,
            string dataVersion,
            string expectedHazardEnemyPlanDigest,
            string expectedOccupiedSurfaceDigest,
            string expectedBudgetLedgerDigest,
            IEnumerable<string> sourceExistingRuntimeStateIds = null,
            IEnumerable<string> sourceExistingSaveKeys = null,
            string expectedSurfaceDigest = null,
            string expectedSaveKeySetDigest = null,
            string expectedExportSurfaceDigest = null,
            bool attemptedRuntimeSpawn = false,
            bool attemptedEventExecution = false,
            bool attemptedSaveWrite = false,
            bool attemptedRewardGrant = false,
            bool attemptedDamage = false,
            bool attemptedPhysics = false,
            bool attemptedAiHookup = false)
        {
            HazardEnemyPlan = hazardEnemyPlan;
            var activityArray = sourceActivities == null
                ? Array.Empty<GeneratedActivityRuntimeSource>()
                : sourceActivities.ToArray();
            var eventArray = sourceEvents == null
                ? Array.Empty<GeneratedEventRuntimeSource>()
                : sourceEvents.ToArray();
            var transitionArray = allowedTransitions == null
                ? Array.Empty<GeneratedActivityRuntimeTransition>()
                : allowedTransitions.ToArray();
            activitySources = new ReadOnlyCollection<GeneratedActivityRuntimeSource>(
                activityArray.Where(value => value != null).OrderBy(value => value).ToArray());
            eventSources = new ReadOnlyCollection<GeneratedEventRuntimeSource>(
                eventArray.Where(value => value != null).OrderBy(value => value).ToArray());
            transitions = new ReadOnlyCollection<GeneratedActivityRuntimeTransition>(
                transitionArray.Where(value => value != null).OrderBy(value => value).ToArray());
            eventVariants = new ReadOnlyCollection<GeneratedEventRuntimeVariant>(
                (requiredEventVariants ?? Array.Empty<GeneratedEventRuntimeVariant>())
                .OrderBy(value => value).ToArray());
            existingRuntimeStateIds = FreezeStrings(sourceExistingRuntimeStateIds);
            existingSaveKeys = FreezeStrings(sourceExistingSaveKeys);
            NullActivitySourceCount = activityArray.Count(value => value == null);
            NullEventSourceCount = eventArray.Count(value => value == null);
            NullTransitionCount = transitionArray.Count(value => value == null);
            WorldSeed = worldSeed ?? string.Empty;
            GeneratorVersion = generatorVersion ?? string.Empty;
            DataVersion = dataVersion ?? string.Empty;
            ExpectedHazardEnemyPlanDigest = expectedHazardEnemyPlanDigest ?? string.Empty;
            ExpectedOccupiedSurfaceDigest = expectedOccupiedSurfaceDigest ?? string.Empty;
            ExpectedBudgetLedgerDigest = expectedBudgetLedgerDigest ?? string.Empty;
            ExpectedSurfaceDigest = expectedSurfaceDigest ?? string.Empty;
            ExpectedSaveKeySetDigest = expectedSaveKeySetDigest ?? string.Empty;
            ExpectedExportSurfaceDigest = expectedExportSurfaceDigest ?? string.Empty;
            AttemptedRuntimeSpawn = attemptedRuntimeSpawn;
            AttemptedEventExecution = attemptedEventExecution;
            AttemptedSaveWrite = attemptedSaveWrite;
            AttemptedRewardGrant = attemptedRewardGrant;
            AttemptedDamage = attemptedDamage;
            AttemptedPhysics = attemptedPhysics;
            AttemptedAiHookup = attemptedAiHookup;
        }

        public GeneratedHazardEnemyPlacementPlan HazardEnemyPlan { get; }
        public IReadOnlyList<GeneratedActivityRuntimeSource> ActivitySources => activitySources;
        public IReadOnlyList<GeneratedEventRuntimeSource> EventSources => eventSources;
        public IReadOnlyList<GeneratedActivityRuntimeTransition> AllowedTransitions => transitions;
        public IReadOnlyList<GeneratedEventRuntimeVariant> RequiredEventVariants => eventVariants;
        public IReadOnlyList<string> ExistingRuntimeStateIds => existingRuntimeStateIds;
        public IReadOnlyList<string> ExistingSaveKeys => existingSaveKeys;
        public int NullActivitySourceCount { get; }
        public int NullEventSourceCount { get; }
        public int NullTransitionCount { get; }
        public string WorldSeed { get; }
        public string GeneratorVersion { get; }
        public string DataVersion { get; }
        public string ExpectedHazardEnemyPlanDigest { get; }
        public string ExpectedOccupiedSurfaceDigest { get; }
        public string ExpectedBudgetLedgerDigest { get; }
        public string ExpectedSurfaceDigest { get; }
        public string ExpectedSaveKeySetDigest { get; }
        public string ExpectedExportSurfaceDigest { get; }
        public bool AttemptedRuntimeSpawn { get; }
        public bool AttemptedEventExecution { get; }
        public bool AttemptedSaveWrite { get; }
        public bool AttemptedRewardGrant { get; }
        public bool AttemptedDamage { get; }
        public bool AttemptedPhysics { get; }
        public bool AttemptedAiHookup { get; }

        private static ReadOnlyCollection<string> FreezeStrings(IEnumerable<string> source) =>
            new ReadOnlyCollection<string>((source ?? Array.Empty<string>())
                .Select(value => value ?? string.Empty)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
    }

    public enum GeneratedActivityEventRuntimeStateFailureCode
    {
        MissingRequest = 1,
        MissingHazardEnemyPlan = 2,
        HazardEnemyPlanDigestMismatch = 3,
        OccupiedSurfaceDigestMismatch = 4,
        BudgetLedgerDigestMismatch = 5,
        MissingActivitySource = 6,
        MissingEventSource = 7,
        InvalidActivitySource = 8,
        InvalidEventSource = 9,
        InvalidActivityTransition = 10,
        InvalidEventVariant = 11,
        DuplicateRuntimeStateId = 12,
        DuplicateSaveKey = 13,
        OccupiedSurfaceConflict = 14,
        AttemptedRuntimeSideEffect = 15,
        RuntimeStateSurfaceDigestMismatch = 16,
        SaveKeySetDigestMismatch = 17,
        ExportSurfaceDigestMismatch = 18,
        InvalidIdentityAuthority = 19,
    }

    public sealed class GeneratedActivityEventRuntimeStateFailure :
        IComparable<GeneratedActivityEventRuntimeStateFailure>
    {
        internal GeneratedActivityEventRuntimeStateFailure(
            GeneratedActivityEventRuntimeStateFailureCode code,
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
                "MAP18_05_FAILURE_V1", Code.ToString(), Owner, OffendingKey,
                Expected, Actual, Reason,
            });
        }

        public GeneratedActivityEventRuntimeStateFailureCode Code { get; }
        public string Owner { get; }
        public string OffendingKey { get; }
        public string Expected { get; }
        public string Actual { get; }
        public string Reason { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedActivityEventRuntimeStateFailure other) => other == null
            ? -1 : string.Compare(StableToken, other.StableToken,
                StringComparison.Ordinal);
        public override string ToString() => StableToken;
    }

    public sealed class GeneratedActivityEventRuntimeStateResult
    {
        private readonly ReadOnlyCollection<GeneratedActivityEventRuntimeStateFailure> failures;

        internal GeneratedActivityEventRuntimeStateResult(
            GeneratedActivityEventRuntimeStateSurface surface,
            IEnumerable<GeneratedActivityEventRuntimeStateFailure> sourceFailures)
        {
            Surface = surface;
            failures = new ReadOnlyCollection<GeneratedActivityEventRuntimeStateFailure>(
                (sourceFailures ?? Array.Empty<GeneratedActivityEventRuntimeStateFailure>())
                .OrderBy(value => value).ToArray());
        }

        public bool Success => Surface != null && failures.Count == 0;
        public GeneratedActivityEventRuntimeStateSurface Surface { get; }
        public IReadOnlyList<GeneratedActivityEventRuntimeStateFailure> Failures => failures;
        public int PartialStateRecordCount => Success
            ? Surface.TotalRuntimeStateRecordCount : 0;
        public int PartialOccupiedMutationCount => 0;
        public int PartialBudgetMutationCount => 0;
        public int RetryLoopCount => 0;
    }

    public static class GeneratedActivityEventRuntimeStateInstantiator
    {
        public const string ExpectedHazardEnemyPlanDigest =
            "003b2ddc329d736945eda48b8b03df128bd0891c40910aa89e97c965ed3222ac";
        public const string ExpectedOccupiedSurfaceDigest =
            "39e530dde3a98191aee290916d536b4952034aa2c758cb7c35050d4e2f74b688";
        public const string ExpectedBudgetLedgerDigest =
            "08a4d128bb08324c99669c235101ead8d2c81d2f78d379b7b58fe59090bef52d";
        public const int ExpectedOccupiedSurfaceCount = 9;
        public const int ExpectedRemainingCandidateCount = 3;

        public static GeneratedActivityEventRuntimeStateResult Instantiate(
            GeneratedActivityEventRuntimeStateRequest request)
        {
            var failures = new List<GeneratedActivityEventRuntimeStateFailure>();
            if (request == null)
            {
                failures.Add(Failure(
                    GeneratedActivityEventRuntimeStateFailureCode.MissingRequest,
                    "MAP18_05", "REQUEST", "PRESENT", "MISSING",
                    "Runtime state request is required."));
                return Result(null, failures);
            }

            ValidateUpstream(request, failures);
            ValidateSources(request, failures);
            ValidateStatePolicy(request, failures);
            ValidateSideEffects(request, failures);
            if (failures.Count > 0) return Result(null, failures);

            var activities = request.ActivitySources.Select(source =>
            {
                var id = RuntimeId(request, "ACTIVITY", source.StableToken, "CYCLE");
                return new GeneratedActivityRuntimeStateRecord(source, id,
                    new GeneratedRuntimeSaveKey(id), request.AllowedTransitions);
            }).ToArray();
            var events = request.EventSources.SelectMany(source =>
                request.RequiredEventVariants.Select(variant =>
                {
                    var id = RuntimeId(request, "EVENT", source.StableToken,
                        variant.ToString().ToUpperInvariant());
                    return new GeneratedEventRuntimeStateRecord(source, variant, id,
                        new GeneratedRuntimeSaveKey(id));
                })).ToArray();

            ValidateIdentityCollisions(request,
                activities.Cast<IGeneratedRuntimeStateRecord>().Concat(events), failures);
            if (failures.Count > 0) return Result(null, failures);

            var surface = new GeneratedActivityEventRuntimeStateSurface(
                request.HazardEnemyPlan, activities, events);
            ValidateExpectedOutput(request, surface, failures);
            return failures.Count == 0 ? Result(surface, failures) : Result(null, failures);
        }

        private static void ValidateUpstream(
            GeneratedActivityEventRuntimeStateRequest request,
            ICollection<GeneratedActivityEventRuntimeStateFailure> failures)
        {
            var plan = request.HazardEnemyPlan;
            if (plan == null)
            {
                failures.Add(Failure(
                    GeneratedActivityEventRuntimeStateFailureCode.MissingHazardEnemyPlan,
                    "MAP18_04", "HAZARD_ENEMY_PLAN", "PRESENT", "MISSING",
                    "Reviewed hazard/enemy plan is required."));
                return;
            }
            ValidateDigest(failures,
                GeneratedActivityEventRuntimeStateFailureCode.HazardEnemyPlanDigestMismatch,
                "MAP18_04", "HAZARD_ENEMY_PLAN_DIGEST", ExpectedHazardEnemyPlanDigest,
                request.ExpectedHazardEnemyPlanDigest, plan.Digest,
                "MAP18_04 plan differs from reviewed evidence.");
            ValidateDigest(failures,
                GeneratedActivityEventRuntimeStateFailureCode.OccupiedSurfaceDigestMismatch,
                "MAP18_04_OCCUPIED", "OCCUPIED_SURFACE_DIGEST",
                ExpectedOccupiedSurfaceDigest, request.ExpectedOccupiedSurfaceDigest,
                plan.OccupiedSurfaceDigest,
                "Occupied surface must pass through without mutation.");
            ValidateDigest(failures,
                GeneratedActivityEventRuntimeStateFailureCode.BudgetLedgerDigestMismatch,
                "MAP18_04_BUDGET", "BUDGET_LEDGER_DIGEST",
                ExpectedBudgetLedgerDigest, request.ExpectedBudgetLedgerDigest,
                plan.BudgetLedger == null ? string.Empty : plan.BudgetLedger.Digest,
                "Budget ledger must pass through without mutation.");
            if (plan.OccupiedSurfaceCount != ExpectedOccupiedSurfaceCount ||
                plan.RemainingCandidateCount != ExpectedRemainingCandidateCount)
                failures.Add(Failure(
                    GeneratedActivityEventRuntimeStateFailureCode.OccupiedSurfaceDigestMismatch,
                    "MAP18_04_OCCUPIED", "OCCUPIED_REMAINING_COUNTS",
                    Number(ExpectedOccupiedSurfaceCount) + "/" +
                        Number(ExpectedRemainingCandidateCount),
                    Number(plan.OccupiedSurfaceCount) + "/" +
                        Number(plan.RemainingCandidateCount),
                    "All nine occupied entries and three remaining candidates are required."));
        }

        private static void ValidateSources(
            GeneratedActivityEventRuntimeStateRequest request,
            ICollection<GeneratedActivityEventRuntimeStateFailure> failures)
        {
            if (request.ActivitySources.Count == 0 || request.NullActivitySourceCount > 0)
                failures.Add(Failure(
                    GeneratedActivityEventRuntimeStateFailureCode.MissingActivitySource,
                    "MAP12_ACTIVITY", "ACTIVITY_SOURCES", ">=1_AND_NO_NULLS",
                    Number(request.ActivitySources.Count) + "/NULL=" +
                        Number(request.NullActivitySourceCount),
                    "Activity authoring sources must be explicit."));
            if (request.EventSources.Count == 0 || request.NullEventSourceCount > 0)
                failures.Add(Failure(
                    GeneratedActivityEventRuntimeStateFailureCode.MissingEventSource,
                    "MAP12_EVENT_OVERLAY", "EVENT_SOURCES", ">=1_AND_NO_NULLS",
                    Number(request.EventSources.Count) + "/NULL=" +
                        Number(request.NullEventSourceCount),
                    "Event overlay authoring sources must be explicit."));
            foreach (var source in request.ActivitySources)
                ValidateActivitySource(request, source, failures);
            foreach (var source in request.EventSources)
                ValidateEventSource(request, source, failures);
            if (string.IsNullOrWhiteSpace(request.WorldSeed) ||
                string.IsNullOrWhiteSpace(request.GeneratorVersion) ||
                string.IsNullOrWhiteSpace(request.DataVersion))
                failures.Add(Failure(
                    GeneratedActivityEventRuntimeStateFailureCode.InvalidIdentityAuthority,
                    "MAP18_05_IDENTITY", "SEED_GENERATOR_DATA", "NON_EMPTY",
                    request.WorldSeed + "/" + request.GeneratorVersion + "/" +
                        request.DataVersion,
                    "Stable runtime identities require explicit version authority."));
        }

        private static void ValidateActivitySource(
            GeneratedActivityEventRuntimeStateRequest request,
            GeneratedActivityRuntimeSource source,
            ICollection<GeneratedActivityEventRuntimeStateFailure> failures)
        {
            if (source.Contract == null || source.Sector == null ||
                !source.Sector.IsInWorld || string.IsNullOrWhiteSpace(source.SourceId) ||
                string.IsNullOrWhiteSpace(source.SourceDigest))
                failures.Add(Failure(
                    GeneratedActivityEventRuntimeStateFailureCode.InvalidActivitySource,
                    "MAP12_ACTIVITY", source.StableToken,
                    "CONTRACT_ID_SECTOR_DIGEST", "INVALID",
                    "Activity source identity is incomplete."));
            ValidateClaim(request, source.ClaimedReservationKey, source.SourceId, failures);
        }

        private static void ValidateEventSource(
            GeneratedActivityEventRuntimeStateRequest request,
            GeneratedEventRuntimeSource source,
            ICollection<GeneratedActivityEventRuntimeStateFailure> failures)
        {
            if (source.Contract == null || source.Sector == null ||
                !source.Sector.IsInWorld || string.IsNullOrWhiteSpace(source.SourceId) ||
                string.IsNullOrWhiteSpace(source.SourceDigest))
                failures.Add(Failure(
                    GeneratedActivityEventRuntimeStateFailureCode.InvalidEventSource,
                    "MAP12_EVENT_OVERLAY", source.StableToken,
                    "CONTRACT_ID_SECTOR_DIGEST", "INVALID",
                    "Event overlay source identity is incomplete."));
            ValidateClaim(request, source.ClaimedReservationKey, source.SourceId, failures);
        }

        private static void ValidateClaim(
            GeneratedActivityEventRuntimeStateRequest request,
            string claimedReservationKey,
            string sourceId,
            ICollection<GeneratedActivityEventRuntimeStateFailure> failures)
        {
            if (!string.IsNullOrEmpty(claimedReservationKey) &&
                request.HazardEnemyPlan != null &&
                request.HazardEnemyPlan.IsOccupied(claimedReservationKey))
                failures.Add(Failure(
                    GeneratedActivityEventRuntimeStateFailureCode.OccupiedSurfaceConflict,
                    "MAP18_04_OCCUPIED", claimedReservationKey, "UNCLAIMED",
                    "OCCUPIED_BY_UPSTREAM",
                    "Runtime source " + sourceId +
                    " cannot claim an occupied content reservation."));
        }

        private static void ValidateStatePolicy(
            GeneratedActivityEventRuntimeStateRequest request,
            ICollection<GeneratedActivityEventRuntimeStateFailure> failures)
        {
            var expectedTransitions = GeneratedActivityRuntimeTransitionCatalog.CreateAllowed()
                .Select(value => value.StableToken).OrderBy(value => value,
                    StringComparer.Ordinal).ToArray();
            var actualTransitions = request.AllowedTransitions.Select(value =>
                value.StableToken).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (request.NullTransitionCount > 0 ||
                !expectedTransitions.SequenceEqual(actualTransitions,
                    StringComparer.Ordinal))
                failures.Add(Failure(
                    GeneratedActivityEventRuntimeStateFailureCode.InvalidActivityTransition,
                    "MAP18_05_ACTIVITY_STATE_MACHINE", "TRANSITION_SET",
                    string.Join(",", expectedTransitions),
                    string.Join(",", actualTransitions) + "/NULL=" +
                        Number(request.NullTransitionCount),
                    "Only the Cue-Active-Resolved-Resettable cycle is allowed."));
            var expectedVariants = new[]
            {
                GeneratedEventRuntimeVariant.Empty,
                GeneratedEventRuntimeVariant.Active,
            };
            var actualVariants = request.RequiredEventVariants.ToArray();
            if (!expectedVariants.SequenceEqual(actualVariants) ||
                actualVariants.Distinct().Count() != expectedVariants.Length)
                failures.Add(Failure(
                    GeneratedActivityEventRuntimeStateFailureCode.InvalidEventVariant,
                    "MAP18_05_EVENT_VARIANT", "VARIANT_SET", "EMPTY,ACTIVE",
                    string.Join(",", actualVariants.Select(value => value.ToString())),
                    "Empty and Active variants are both required exactly once."));
        }

        private static void ValidateSideEffects(
            GeneratedActivityEventRuntimeStateRequest request,
            ICollection<GeneratedActivityEventRuntimeStateFailure> failures)
        {
            var actual = string.Join("/", new[]
            {
                Flag(request.AttemptedRuntimeSpawn),
                Flag(request.AttemptedEventExecution),
                Flag(request.AttemptedSaveWrite),
                Flag(request.AttemptedRewardGrant),
                Flag(request.AttemptedDamage),
                Flag(request.AttemptedPhysics),
                Flag(request.AttemptedAiHookup),
            });
            if (!string.Equals(actual, "0/0/0/0/0/0/0", StringComparison.Ordinal))
                failures.Add(Failure(
                    GeneratedActivityEventRuntimeStateFailureCode.AttemptedRuntimeSideEffect,
                    "MAP18_06_OR_LATER", "SPAWN_EVENT_SAVE_REWARD_DAMAGE_PHYSICS_AI",
                    "0/0/0/0/0/0/0", actual,
                    "MAP18_05 creates pure-data state records only."));
        }

        private static void ValidateIdentityCollisions(
            GeneratedActivityEventRuntimeStateRequest request,
            IEnumerable<IGeneratedRuntimeStateRecord> sourceStates,
            ICollection<GeneratedActivityEventRuntimeStateFailure> failures)
        {
            var states = sourceStates.ToArray();
            foreach (var group in request.ExistingRuntimeStateIds.Concat(states.Select(value =>
                         value.RuntimeStateId.Value)).GroupBy(value => value,
                         StringComparer.Ordinal).Where(value => value.Count() > 1))
                failures.Add(Failure(
                    GeneratedActivityEventRuntimeStateFailureCode.DuplicateRuntimeStateId,
                    "MAP18_05_IDENTITY", group.Key, "UNIQUE", Number(group.Count()),
                    "Runtime state IDs must be globally unique."));
            foreach (var group in request.ExistingSaveKeys.Concat(states.Select(value =>
                         value.SaveKey.Value)).GroupBy(value => value,
                         StringComparer.Ordinal).Where(value => value.Count() > 1))
                failures.Add(Failure(
                    GeneratedActivityEventRuntimeStateFailureCode.DuplicateSaveKey,
                    "MAP18_05_SAVE_KEY", group.Key, "UNIQUE", Number(group.Count()),
                    "Runtime save keys must be globally unique."));
        }

        private static void ValidateExpectedOutput(
            GeneratedActivityEventRuntimeStateRequest request,
            GeneratedActivityEventRuntimeStateSurface surface,
            ICollection<GeneratedActivityEventRuntimeStateFailure> failures)
        {
            ValidateOptionalOutputDigest(request.ExpectedSurfaceDigest, surface.Digest,
                GeneratedActivityEventRuntimeStateFailureCode.RuntimeStateSurfaceDigestMismatch,
                "RUNTIME_STATE_SURFACE_DIGEST", failures);
            ValidateOptionalOutputDigest(request.ExpectedSaveKeySetDigest,
                surface.SaveKeySetDigest,
                GeneratedActivityEventRuntimeStateFailureCode.SaveKeySetDigestMismatch,
                "SAVE_KEY_SET_DIGEST", failures);
            ValidateOptionalOutputDigest(request.ExpectedExportSurfaceDigest,
                surface.ExportSurfaceDigest,
                GeneratedActivityEventRuntimeStateFailureCode.ExportSurfaceDigestMismatch,
                "EXPORT_SURFACE_DIGEST", failures);
        }

        private static void ValidateOptionalOutputDigest(
            string expected,
            string actual,
            GeneratedActivityEventRuntimeStateFailureCode code,
            string key,
            ICollection<GeneratedActivityEventRuntimeStateFailure> failures)
        {
            if (!string.IsNullOrEmpty(expected) &&
                !string.Equals(expected, actual, StringComparison.Ordinal))
                failures.Add(Failure(code, "MAP18_05", key, expected, actual,
                    "Generated output differs from the reviewed deterministic digest."));
        }

        private static GeneratedRuntimeStateId RuntimeId(
            GeneratedActivityEventRuntimeStateRequest request,
            string kind,
            string sourceToken,
            string stateToken) => new GeneratedRuntimeStateId(new[]
        {
            "MAP18_05_RUNTIME_STATE_ID_V1",
            "WORLD=" + request.WorldSeed,
            "GENERATOR=" + request.GeneratorVersion,
            "DATA=" + request.DataVersion,
            "KIND=" + kind,
            "SOURCE=" + sourceToken,
            "STATE=" + stateToken,
        });

        private static void ValidateDigest(
            ICollection<GeneratedActivityEventRuntimeStateFailure> failures,
            GeneratedActivityEventRuntimeStateFailureCode code,
            string owner,
            string key,
            string authority,
            string expected,
            string actual,
            string reason)
        {
            if (!string.Equals(expected, authority, StringComparison.Ordinal) ||
                !string.Equals(actual, authority, StringComparison.Ordinal))
                failures.Add(Failure(code, owner, key, authority,
                    expected + "/" + actual, reason));
        }

        private static GeneratedActivityEventRuntimeStateFailure Failure(
            GeneratedActivityEventRuntimeStateFailureCode code,
            string owner,
            string key,
            string expected,
            string actual,
            string reason) => new GeneratedActivityEventRuntimeStateFailure(
                code, owner, key, expected, actual, reason);

        private static GeneratedActivityEventRuntimeStateResult Result(
            GeneratedActivityEventRuntimeStateSurface surface,
            IEnumerable<GeneratedActivityEventRuntimeStateFailure> failures) =>
            new GeneratedActivityEventRuntimeStateResult(surface, failures);

        private static string Number(int value) =>
            value.ToString(CultureInfo.InvariantCulture);
        private static string Flag(bool value) => value ? "1" : "0";
    }
}
