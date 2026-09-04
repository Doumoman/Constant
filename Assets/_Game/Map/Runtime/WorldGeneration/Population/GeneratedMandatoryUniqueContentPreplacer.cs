using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.WorldGeneration.Population
{
    public sealed class GeneratedMandatoryUniquePlacementRequest
    {
        private readonly ReadOnlyCollection<GeneratedMandatoryUniqueRule> rules;
        private readonly ReadOnlyCollection<CoreResourceRegionDefinition> coreDefinitions;
        private readonly ReadOnlyCollection<string> existingReservationKeys;
        private readonly ReadOnlyCollection<string> existingStableSpawnIds;

        public GeneratedMandatoryUniquePlacementRequest(
            GeneratedContentSlotIndex slotIndex,
            IEnumerable<GeneratedMandatoryUniqueRule> sourceRules,
            IEnumerable<CoreResourceRegionDefinition> sourceCoreDefinitions,
            string expectedSlotIndexDigest,
            string expectedStableIdSetDigest,
            int expectedSourceRecordCount,
            int expectedMandatoryUniqueCandidateCount,
            IEnumerable<string> sourceExistingReservationKeys = null,
            IEnumerable<string> sourceExistingStableSpawnIds = null,
            string expectedPlanDigest = null,
            bool attemptedPoolRoll = false,
            bool attemptedRuntimeSpawn = false)
        {
            SlotIndex = slotIndex;
            rules = Freeze(sourceRules, out var nullRules);
            coreDefinitions = Freeze(sourceCoreDefinitions, out var nullDefinitions);
            existingReservationKeys = FreezeStrings(sourceExistingReservationKeys);
            existingStableSpawnIds = FreezeStrings(sourceExistingStableSpawnIds);
            NullRuleCount = nullRules;
            NullCoreDefinitionCount = nullDefinitions;
            ExpectedSlotIndexDigest = Normalize(expectedSlotIndexDigest);
            ExpectedStableIdSetDigest = Normalize(expectedStableIdSetDigest);
            ExpectedSourceRecordCount = expectedSourceRecordCount;
            ExpectedMandatoryUniqueCandidateCount = expectedMandatoryUniqueCandidateCount;
            ExpectedPlanDigest = Normalize(expectedPlanDigest);
            AttemptedPoolRoll = attemptedPoolRoll;
            AttemptedRuntimeSpawn = attemptedRuntimeSpawn;
        }

        public GeneratedContentSlotIndex SlotIndex { get; }
        public IReadOnlyList<GeneratedMandatoryUniqueRule> Rules => rules;
        public IReadOnlyList<CoreResourceRegionDefinition> CoreDefinitions => coreDefinitions;
        public IReadOnlyList<string> ExistingReservationKeys => existingReservationKeys;
        public IReadOnlyList<string> ExistingStableSpawnIds => existingStableSpawnIds;
        public int NullRuleCount { get; }
        public int NullCoreDefinitionCount { get; }
        public string ExpectedSlotIndexDigest { get; }
        public string ExpectedStableIdSetDigest { get; }
        public int ExpectedSourceRecordCount { get; }
        public int ExpectedMandatoryUniqueCandidateCount { get; }
        public string ExpectedPlanDigest { get; }
        public bool AttemptedPoolRoll { get; }
        public bool AttemptedRuntimeSpawn { get; }

        private static ReadOnlyCollection<T> Freeze<T>(IEnumerable<T> source, out int nullCount)
            where T : class
        {
            var raw = (source ?? Array.Empty<T>()).ToArray();
            nullCount = raw.Count(value => value == null);
            return new ReadOnlyCollection<T>(raw.Where(value => value != null).ToArray());
        }

        private static ReadOnlyCollection<string> FreezeStrings(IEnumerable<string> source) =>
            new ReadOnlyCollection<string>((source ?? Array.Empty<string>())
                .Select(Normalize).Where(value => !string.IsNullOrEmpty(value))
                .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal)
                .ToArray());
        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? string.Empty : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
    }

    public enum GeneratedMandatoryUniquePlacementFailureCode
    {
        MissingRequest = 1,
        MissingSlotIndex = 2,
        MissingSlotIndexDigest = 3,
        SlotIndexDigestMismatch = 4,
        StableIdSetDigestMismatch = 5,
        SourceRecordCountMismatch = 6,
        MandatoryCandidateCountBelowMinimum = 7,
        MandatoryCandidateCountMismatch = 8,
        MissingRequiredProgressTriggerCandidate = 9,
        MissingMoonCoreCandidate = 10,
        MissingCassiaSapCandidate = 11,
        MissingStarNurukCandidate = 12,
        CoreResourceAuthoritativeIdentityMismatch = 13,
        LegacyShortPersistenceKeyAccepted = 14,
        DuplicateUniqueContentKey = 15,
        MaxWorldCountExceeded = 16,
        InvalidWorldUniqueRule = 17,
        ReservationKeyCollision = 18,
        StableSpawnIdCollision = 19,
        AttemptedPoolRollOrRuntimeSpawn = 20,
        PlanDigestMismatch = 21,
    }

    public sealed class GeneratedMandatoryUniquePlacementFailure :
        IComparable<GeneratedMandatoryUniquePlacementFailure>
    {
        public GeneratedMandatoryUniquePlacementFailure(
            GeneratedMandatoryUniquePlacementFailureCode code,
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
                Code.ToString().ToUpperInvariant(), Owner, OffendingKey,
                Expected, Actual, Reason,
            });
        }

        public GeneratedMandatoryUniquePlacementFailureCode Code { get; }
        public string Owner { get; }
        public string OffendingKey { get; }
        public string Expected { get; }
        public string Actual { get; }
        public string Reason { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedMandatoryUniquePlacementFailure other) => other == null
            ? -1 : string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
        public override string ToString() => StableToken;
    }

    public sealed class GeneratedMandatoryUniquePlacementResult
    {
        private readonly ReadOnlyCollection<GeneratedMandatoryUniquePlacementFailure> failures;

        internal GeneratedMandatoryUniquePlacementResult(
            GeneratedMandatoryUniquePlacementPlan plan,
            IEnumerable<GeneratedMandatoryUniquePlacementFailure> sourceFailures)
        {
            Plan = plan;
            failures = new ReadOnlyCollection<GeneratedMandatoryUniquePlacementFailure>(
                (sourceFailures ?? Array.Empty<GeneratedMandatoryUniquePlacementFailure>())
                .OrderBy(value => value).ToArray());
        }

        public bool Success => Plan != null && failures.Count == 0;
        public GeneratedMandatoryUniquePlacementPlan Plan { get; }
        public IReadOnlyList<GeneratedMandatoryUniquePlacementFailure> Failures => failures;
        public int PartialPlacementEntryCount => Success ? Plan.EntryCount : 0;
        public int PartialMutationCount => 0;
        public int RetryLoopCount => 0;
    }

    public static class GeneratedMandatoryUniqueContentPreplacer
    {
        public const string ExpectedSlotIndexDigest =
            "889c25815c9d0bffe6c6ea785b66c55e79f0e8e93631771f0ec30a0b39c2b6bd";
        public const string ExpectedStableIdSetDigest =
            "bfc341e0c62a62d8846580b9455874df9e30573bd4c5f6cc450d719c89464b8a";
        public const int ExpectedSourceRecordCount = 12;
        public const int ExpectedMandatoryUniqueCandidateCount = 5;
        public const int RequiredPlacementCount = 4;

        public static IReadOnlyList<GeneratedMandatoryUniqueRule> CreateDefaultRules()
            => new ReadOnlyCollection<GeneratedMandatoryUniqueRule>(
                GeneratedMandatoryContentCatalog.CreateAuthoritative()
                .Select(GeneratedMandatoryUniqueRule.CreateDefault)
                .OrderBy(value => value).ToArray());

        public static GeneratedMandatoryUniquePlacementResult Preplace(
            GeneratedMandatoryUniquePlacementRequest request)
        {
            var failures = new List<GeneratedMandatoryUniquePlacementFailure>();
            if (request == null)
            {
                failures.Add(Failure(GeneratedMandatoryUniquePlacementFailureCode.MissingRequest,
                    "MAP18_02", "REQUEST", "PRESENT", "MISSING", "Request is required."));
                return Result(null, failures);
            }

            ValidateIndex(request, failures);
            ValidateRulesAndCatalog(request, failures);
            if (request.AttemptedPoolRoll || request.AttemptedRuntimeSpawn)
                failures.Add(Failure(
                    GeneratedMandatoryUniquePlacementFailureCode.AttemptedPoolRollOrRuntimeSpawn,
                    "MAP18_03_OR_LATER", "SIDE_EFFECT_REQUEST", "0/0",
                    (request.AttemptedPoolRoll ? "1" : "0") + "/" +
                    (request.AttemptedRuntimeSpawn ? "1" : "0"),
                    "MAP18_02 creates only logical preplacement data."));
            if (failures.Count > 0) return Result(null, failures);

            var candidates = request.SlotIndex.MandatoryUniqueCandidates();
            var usedReservations = new HashSet<string>(StringComparer.Ordinal);
            var entries = new List<GeneratedMandatoryUniquePlacementEntry>();
            foreach (var rule in request.Rules.OrderBy(value => value))
            {
                var selected = candidates.Where(value => !usedReservations.Contains(value.ReservationKey) &&
                        rule.Suitability(value) != int.MaxValue)
                    .OrderBy(rule.Suitability)
                    .ThenBy(value => value.Address.Sector)
                    .ThenBy(value => value.Address.SliceIndex)
                    .ThenBy(value => value.Address.SectorLocalIndex)
                    .ThenBy(value => value.Address.SourceOwnerKind)
                    .ThenBy(value => value.Address.SourceSlotId, StringComparer.Ordinal)
                    .ThenBy(value => value.Address.PoolKey)
                    .FirstOrDefault();
                if (selected == null)
                {
                    failures.Add(MissingCandidate(rule.ContentKey));
                    continue;
                }
                if (request.ExistingReservationKeys.Contains(selected.ReservationKey,
                        StringComparer.Ordinal))
                    failures.Add(Failure(
                        GeneratedMandatoryUniquePlacementFailureCode.ReservationKeyCollision,
                        "MAP18_03_EXCLUSION_SURFACE", selected.ReservationKey,
                        "UNRESERVED", "PRE_RESERVED",
                        "Selected slot reservation already belongs to another consumer."));
                if (request.ExistingStableSpawnIds.Contains(selected.StableSpawnId.Value,
                        StringComparer.Ordinal))
                    failures.Add(Failure(
                        GeneratedMandatoryUniquePlacementFailureCode.StableSpawnIdCollision,
                        GeneratedStableSpawnIdFactory.Namespace, selected.StableSpawnId.Value,
                        "UNIQUE", "PRE_EXISTING",
                        "Selected stable spawn ID already belongs to another placement."));
                entries.Add(new GeneratedMandatoryUniquePlacementEntry(rule, selected));
                usedReservations.Add(selected.ReservationKey);
            }
            if (failures.Count > 0) return Result(null, failures);

            foreach (var group in entries.GroupBy(value => value.ContentKey.Kind)
                         .Where(value => value.Count() > 1))
                failures.Add(Failure(
                    GeneratedMandatoryUniquePlacementFailureCode.DuplicateUniqueContentKey,
                    "MAP18_02", group.Key.ToString(), "1", Number(group.Count()),
                    "World-unique content was selected more than once."));
            foreach (var entry in entries.Where(value =>
                         entries.Count(candidate => candidate.ContentKey.Kind ==
                             value.ContentKey.Kind) > value.Rule.MaxWorldCount))
                failures.Add(Failure(
                    GeneratedMandatoryUniquePlacementFailureCode.MaxWorldCountExceeded,
                    "MAP18_02", entry.ContentKey.Value, Number(entry.Rule.MaxWorldCount),
                    Number(entries.Count(value => value.ContentKey.Kind == entry.ContentKey.Kind)),
                    "Planned count exceeds the declared world maximum."));
            foreach (var group in entries.GroupBy(value => value.ReservationKey,
                         StringComparer.Ordinal).Where(value => value.Count() > 1))
                failures.Add(Failure(
                    GeneratedMandatoryUniquePlacementFailureCode.ReservationKeyCollision,
                    "MAP18_03_EXCLUSION_SURFACE", group.Key, "1", Number(group.Count()),
                    "Two mandatory entries occupy the same physical slot."));
            foreach (var group in entries.GroupBy(value => value.StableSpawnId.Value,
                         StringComparer.Ordinal).Where(value => value.Count() > 1))
                failures.Add(Failure(
                    GeneratedMandatoryUniquePlacementFailureCode.StableSpawnIdCollision,
                    GeneratedStableSpawnIdFactory.Namespace, group.Key, "1", Number(group.Count()),
                    "Two mandatory entries share one stable spawn ID."));
            if (failures.Count > 0) return Result(null, failures);

            var remaining = candidates.Where(value => !usedReservations.Contains(value.ReservationKey));
            var plan = new GeneratedMandatoryUniquePlacementPlan(request.SlotIndex, entries, remaining);
            if (!string.IsNullOrEmpty(request.ExpectedPlanDigest) &&
                !string.Equals(request.ExpectedPlanDigest, plan.Digest, StringComparison.Ordinal))
            {
                failures.Add(Failure(
                    GeneratedMandatoryUniquePlacementFailureCode.PlanDigestMismatch,
                    "MAP18_02", "PLAN_DIGEST", request.ExpectedPlanDigest, plan.Digest,
                    "Computed logical placement digest differs from the expected digest."));
                return Result(null, failures);
            }
            return Result(plan, failures);
        }

        private static void ValidateIndex(
            GeneratedMandatoryUniquePlacementRequest request,
            ICollection<GeneratedMandatoryUniquePlacementFailure> failures)
        {
            var index = request.SlotIndex;
            if (index == null)
            {
                failures.Add(Failure(GeneratedMandatoryUniquePlacementFailureCode.MissingSlotIndex,
                    "MAP18_01", "SLOT_INDEX", "PRESENT", "MISSING",
                    "Reviewed MAP18_01 slot index is required."));
                return;
            }
            if (string.IsNullOrEmpty(request.ExpectedSlotIndexDigest))
                failures.Add(Failure(
                    GeneratedMandatoryUniquePlacementFailureCode.MissingSlotIndexDigest,
                    "MAP18_01", "EXPECTED_SLOT_INDEX_DIGEST", ExpectedSlotIndexDigest,
                    "MISSING", "Expected source digest is required."));
            else if (!string.Equals(request.ExpectedSlotIndexDigest, index.Digest,
                         StringComparison.Ordinal) ||
                     !string.Equals(index.Digest, ExpectedSlotIndexDigest, StringComparison.Ordinal))
                failures.Add(Failure(
                    GeneratedMandatoryUniquePlacementFailureCode.SlotIndexDigestMismatch,
                    "MAP18_01", "SLOT_INDEX_DIGEST", ExpectedSlotIndexDigest,
                    request.ExpectedSlotIndexDigest + "/" + index.Digest,
                    "Slot index digest differs from reviewed MAP18_01 evidence."));
            if (!string.Equals(request.ExpectedStableIdSetDigest, index.StableIdSetDigest,
                    StringComparison.Ordinal) ||
                !string.Equals(index.StableIdSetDigest, ExpectedStableIdSetDigest,
                    StringComparison.Ordinal))
                failures.Add(Failure(
                    GeneratedMandatoryUniquePlacementFailureCode.StableIdSetDigestMismatch,
                    "MAP18_01", "STABLE_ID_SET_DIGEST", ExpectedStableIdSetDigest,
                    request.ExpectedStableIdSetDigest + "/" + index.StableIdSetDigest,
                    "Stable ID set digest differs from reviewed MAP18_01 evidence."));
            if (request.ExpectedSourceRecordCount != ExpectedSourceRecordCount ||
                index.Count != ExpectedSourceRecordCount)
                failures.Add(Failure(
                    GeneratedMandatoryUniquePlacementFailureCode.SourceRecordCountMismatch,
                    "MAP18_01", "SOURCE_RECORD_COUNT", Number(ExpectedSourceRecordCount),
                    Number(request.ExpectedSourceRecordCount) + "/" + Number(index.Count),
                    "Source record count differs from reviewed evidence."));
            var candidateCount = index.MandatoryUniqueCandidates().Count;
            if (candidateCount < RequiredPlacementCount)
                failures.Add(Failure(
                    GeneratedMandatoryUniquePlacementFailureCode.MandatoryCandidateCountBelowMinimum,
                    "MAP18_01", "MANDATORY_UNIQUE_CANDIDATES", ">=4",
                    Number(candidateCount), "At least four explicit candidates are required."));
            if (request.ExpectedMandatoryUniqueCandidateCount != ExpectedMandatoryUniqueCandidateCount ||
                candidateCount != ExpectedMandatoryUniqueCandidateCount)
                failures.Add(Failure(
                    GeneratedMandatoryUniquePlacementFailureCode.MandatoryCandidateCountMismatch,
                    "MAP18_01", "MANDATORY_UNIQUE_CANDIDATES",
                    Number(ExpectedMandatoryUniqueCandidateCount),
                    Number(request.ExpectedMandatoryUniqueCandidateCount) + "/" +
                    Number(candidateCount), "Candidate count differs from reviewed evidence."));
        }

        private static void ValidateRulesAndCatalog(
            GeneratedMandatoryUniquePlacementRequest request,
            ICollection<GeneratedMandatoryUniquePlacementFailure> failures)
        {
            if (request.NullRuleCount > 0 || request.NullCoreDefinitionCount > 0)
                failures.Add(Failure(
                    GeneratedMandatoryUniquePlacementFailureCode.CoreResourceAuthoritativeIdentityMismatch,
                    "MAP13_06", "NULL_INPUTS", "0/0",
                    Number(request.NullRuleCount) + "/" + Number(request.NullCoreDefinitionCount),
                    "Rule and CoreResource catalog inputs cannot contain nulls."));
            foreach (var group in request.Rules.Where(value => value.ContentKey != null)
                         .GroupBy(value => value.ContentKey.Kind).Where(value => value.Count() > 1))
                failures.Add(Failure(
                    GeneratedMandatoryUniquePlacementFailureCode.DuplicateUniqueContentKey,
                    "MAP18_02", group.Key.ToString(), "1", Number(group.Count()),
                    "Duplicate world-unique content rule."));

            foreach (GeneratedMandatoryContentKind kind in Enum.GetValues(
                         typeof(GeneratedMandatoryContentKind)))
            {
                var matches = request.Rules.Where(value => value.ContentKey != null &&
                    value.ContentKey.Kind == kind).ToArray();
                if (matches.Length == 0) failures.Add(MissingCandidate(
                    new GeneratedMandatoryContentKey(kind, kind.ToString(), null,
                        default(SpecialPersistenceKey))));
            }

            foreach (var rule in request.Rules)
            {
                if (rule.ContentKey == null || !rule.Required || !rule.ExactlyOne ||
                    !rule.WorldUnique || !rule.ExcludesDownstream || rule.CategoryPreference.Count == 0)
                    failures.Add(Failure(
                        GeneratedMandatoryUniquePlacementFailureCode.InvalidWorldUniqueRule,
                        "MAP18_02", rule.ContentKey == null ? "MISSING" : rule.ContentKey.Value,
                        "REQUIRED/EXACTLY_ONE/WORLD_UNIQUE/EXCLUDES/COMPATIBLE",
                        rule.StableToken, "Mandatory rule flags or category preference are invalid."));
                if (rule.MaxWorldCount != 1)
                    failures.Add(Failure(
                        GeneratedMandatoryUniquePlacementFailureCode.MaxWorldCountExceeded,
                        "MAP18_02", rule.ContentKey == null ? "MISSING" : rule.ContentKey.Value,
                        "1", Number(rule.MaxWorldCount), "World-unique max count must be one."));
                ValidateContentIdentity(rule.ContentKey, request.CoreDefinitions, failures);
            }
        }

        private static void ValidateContentIdentity(
            GeneratedMandatoryContentKey key,
            IEnumerable<CoreResourceRegionDefinition> definitions,
            ICollection<GeneratedMandatoryUniquePlacementFailure> failures)
        {
            if (key == null) return;
            if (key.Kind == GeneratedMandatoryContentKind.RequiredProgressTrigger)
            {
                if (key.Value != GeneratedMandatoryContentCatalog.RequiredProgressTriggerValue ||
                    key.CoreResource.HasValue || !string.IsNullOrEmpty(
                        key.AuthoritativePersistenceKey.Value))
                    failures.Add(Failure(
                        GeneratedMandatoryUniquePlacementFailureCode.CoreResourceAuthoritativeIdentityMismatch,
                        "MAP18_02", key.Value,
                        GeneratedMandatoryContentCatalog.RequiredProgressTriggerValue,
                        key.StableToken, "Required trigger key is not canonical."));
                return;
            }
            if (!key.CoreResource.HasValue)
            {
                failures.Add(Failure(
                    GeneratedMandatoryUniquePlacementFailureCode.CoreResourceAuthoritativeIdentityMismatch,
                    "MAP13_06", key.Value, "CORE_RESOURCE", "MISSING",
                    "CoreResource key lacks its authoritative resource kind."));
                return;
            }
            var definition = definitions.SingleOrDefault(value => value.Resource == key.CoreResource.Value);
            var reward = definition == null ? null : definition.RequiredReward;
            var expected = definition == null || reward == null
                ? default(SpecialPersistenceKey)
                : SpecialPersistenceKey.ForSlot(definition.RegionId,
                    SpecialPersistenceScope.Reward, reward.SlotId);
            var valid = definition != null && reward != null && reward.Required && reward.Amount == 1 &&
                reward.Resource == key.CoreResource.Value &&
                reward.PersistenceScope == SpecialPersistenceScope.Reward &&
                reward.PersistenceKey == expected && key.AuthoritativePersistenceKey == expected &&
                key.Kind == GeneratedMandatoryContentCatalog.Kind(key.CoreResource.Value);
            if (!valid)
                failures.Add(Failure(
                    GeneratedMandatoryUniquePlacementFailureCode.CoreResourceAuthoritativeIdentityMismatch,
                    "MAP13_06", key.Value, expected.Value,
                    key.AuthoritativePersistenceKey.Value,
                    "CoreResource identity must match the authoritative reward definition."));
            if (!key.AuthoritativePersistenceKey.Value.StartsWith("SR_STATE_",
                    StringComparison.Ordinal) ||
                key.AuthoritativePersistenceKey.Value.IndexOf("_REWARD_",
                    StringComparison.Ordinal) < 0)
                failures.Add(Failure(
                    GeneratedMandatoryUniquePlacementFailureCode.LegacyShortPersistenceKeyAccepted,
                    "MAP13_06_REPAIR", key.Value, expected.Value,
                    key.AuthoritativePersistenceKey.Value,
                    "Legacy short persistence keys are forbidden."));
        }

        private static GeneratedMandatoryUniquePlacementFailure MissingCandidate(
            GeneratedMandatoryContentKey key)
        {
            var code = key == null ?
                GeneratedMandatoryUniquePlacementFailureCode.MissingRequiredProgressTriggerCandidate :
                key.Kind == GeneratedMandatoryContentKind.MoonCore
                    ? GeneratedMandatoryUniquePlacementFailureCode.MissingMoonCoreCandidate
                    : key.Kind == GeneratedMandatoryContentKind.CassiaSap
                        ? GeneratedMandatoryUniquePlacementFailureCode.MissingCassiaSapCandidate
                        : key.Kind == GeneratedMandatoryContentKind.StarNuruk
                            ? GeneratedMandatoryUniquePlacementFailureCode.MissingStarNurukCandidate
                            : GeneratedMandatoryUniquePlacementFailureCode.MissingRequiredProgressTriggerCandidate;
            return Failure(code, "MAP18_01", key == null ? "MISSING" : key.Value,
                "ONE_COMPATIBLE_CANDIDATE", "0",
                "No unreserved candidate satisfies this mandatory content rule.");
        }

        private static GeneratedMandatoryUniquePlacementFailure Failure(
            GeneratedMandatoryUniquePlacementFailureCode code,
            string owner,
            string key,
            string expected,
            string actual,
            string reason) => new GeneratedMandatoryUniquePlacementFailure(
                code, owner, key, expected, actual, reason);
        private static GeneratedMandatoryUniquePlacementResult Result(
            GeneratedMandatoryUniquePlacementPlan plan,
            IEnumerable<GeneratedMandatoryUniquePlacementFailure> failures) =>
            new GeneratedMandatoryUniquePlacementResult(plan, failures);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
