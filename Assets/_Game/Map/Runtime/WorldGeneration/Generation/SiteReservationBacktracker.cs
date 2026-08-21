using System;
using System.Collections.Generic;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SiteReservationBacktracker
    {
        private const string WorldId = "WORLD_MOONPALACE_V1";
        private const string BossId = "SITE_MOON_BOSS_VAULT";
        private const string ForgeId = "SITE_MOON_SEAL_FORGE";
        private const string CassiaId = "SITE_CASSIA_SAP_HEART";
        private const string YeastId = "SITE_DEEP_STAR_YEAST";
        private const string MeteorId = "SITE_MOON_CORE_METEOR";

        public SiteReservationSearchResult Search(
            IEnumerable<SiteReservationSearchGroup> groups,
            SiteDistancePolicy distancePolicy,
            SiteCandidateCostWeights weights,
            SiteReservationSearchLimits limits,
            DeterministicRngStream siteRng)
        {
            var errors = new List<SiteReservationSearchError>();
            var orderedGroups = SnapshotGroups(groups, errors);
            ValidateDependencies(distancePolicy, weights, limits, siteRng, errors);
            ValidateGroups(orderedGroups, errors);
            if (errors.Count == 0)
            {
                ValidatePolicy(orderedGroups, distancePolicy, errors);
                if (errors.Count == 0)
                    ValidateOptions(orderedGroups, distancePolicy, weights, errors);
            }

            var counters = CreateCounters(orderedGroups);
            var initialState = siteRng == null ? 0UL : siteRng.InitialState;
            var drawBefore = siteRng == null ? 0UL : siteRng.DrawCount;
            if (errors.Count != 0)
            {
                return new SiteReservationSearchResult(
                    SiteReservationSearchStatus.InvalidInput,
                    null,
                    BuildDiagnostics(counters, 0, 0, initialState, drawBefore, 0,
                        siteRng == null ? 0UL : siteRng.DrawCount),
                    errors);
            }

            var tieBreaks = new ulong[orderedGroups.Count][];
            ulong tieBreakDrawCount = 0;
            for (var groupIndex = 0; groupIndex < orderedGroups.Count; groupIndex++)
            {
                var group = orderedGroups[groupIndex];
                tieBreaks[groupIndex] = new ulong[group.OptionCount];
                for (var optionIndex = 0; optionIndex < group.OptionCount; optionIndex++)
                {
                    tieBreaks[groupIndex][optionIndex] = siteRng.NextUInt64();
                    tieBreakDrawCount++;
                }
            }

            var state = new SearchState(
                orderedGroups,
                distancePolicy,
                weights,
                limits,
                tieBreaks,
                counters);
            var outcome = state.Visit(0);
            var diagnostics = BuildDiagnostics(
                counters,
                state.FailedCombinationCount,
                state.DeepestSelectedDepth,
                initialState,
                drawBefore,
                tieBreakDrawCount,
                siteRng.DrawCount);

            switch (outcome)
            {
                case SearchOutcome.Completed:
                    return new SiteReservationSearchResult(
                        SiteReservationSearchStatus.Completed,
                        state.CompletedPlan,
                        diagnostics,
                        Array.Empty<SiteReservationSearchError>());
                case SearchOutcome.NoSolution:
                    return new SiteReservationSearchResult(
                        SiteReservationSearchStatus.NoSolution,
                        null,
                        diagnostics,
                        Array.Empty<SiteReservationSearchError>());
                case SearchOutcome.LimitReached:
                    return new SiteReservationSearchResult(
                        SiteReservationSearchStatus.FailedCombinationLimitReached,
                        null,
                        diagnostics,
                        Array.Empty<SiteReservationSearchError>());
                default:
                    return new SiteReservationSearchResult(
                        SiteReservationSearchStatus.InvalidInput,
                        null,
                        diagnostics,
                        state.Errors);
            }
        }

        private static List<SiteReservationSearchGroup> SnapshotGroups(
            IEnumerable<SiteReservationSearchGroup> source,
            ICollection<SiteReservationSearchError> errors)
        {
            var result = new List<SiteReservationSearchGroup>();
            if (source == null)
            {
                Add(errors, SiteReservationSearchErrorCode.MissingGroups,
                    string.Empty, string.Empty, -1, "A search-group collection is required.");
                return result;
            }

            try
            {
                foreach (var group in source)
                {
                    if (group == null)
                    {
                        Add(errors, SiteReservationSearchErrorCode.NullGroup,
                            string.Empty, string.Empty, -1, "Search groups cannot contain null.");
                    }
                    else
                    {
                        result.Add(group);
                    }
                }
            }
            catch (Exception)
            {
                Add(errors, SiteReservationSearchErrorCode.InvalidGroup,
                    string.Empty, string.Empty, -1, "Search groups must be eagerly enumerable.");
            }
            result.Sort((left, right) => left.Key.CompareTo(right.Key));
            return result;
        }

        private static void ValidateDependencies(
            SiteDistancePolicy policy,
            SiteCandidateCostWeights weights,
            SiteReservationSearchLimits limits,
            DeterministicRngStream siteRng,
            ICollection<SiteReservationSearchError> errors)
        {
            if (policy == null)
                Add(errors, SiteReservationSearchErrorCode.MissingDistancePolicy,
                    string.Empty, string.Empty, -1, "A site-distance policy is required.");
            if (weights == null)
                Add(errors, SiteReservationSearchErrorCode.MissingWeights,
                    string.Empty, string.Empty, -1, "Candidate-cost weights are required.");
            else if (weights.AltitudePerSector < 0 || weights.EdgeClearanceDeficit < 0 ||
                     weights.DistanceDeficit < 0 || weights.FutureCoreCapacityShortfall < 0 ||
                     weights.CoreCluster < 0)
                Add(errors, SiteReservationSearchErrorCode.MissingWeights,
                    string.Empty, string.Empty, -1, "Candidate-cost weights must be valid.");
            if (limits == null)
                Add(errors, SiteReservationSearchErrorCode.MissingLimits,
                    string.Empty, string.Empty, -1, "Search limits are required.");
            else if (limits.MaxFailedCombinations < 1 ||
                     limits.MaxFailedCombinations > SiteReservationSearchLimits.ProductionMaximum)
                Add(errors, SiteReservationSearchErrorCode.InvalidLimits,
                    string.Empty, string.Empty, -1, "The failed-combination limit must be between one and two hundred.");
            if (siteRng == null)
                Add(errors, SiteReservationSearchErrorCode.MissingSiteRng,
                    string.Empty, string.Empty, -1, "A fresh site RNG stream is required.");
            else if (siteRng.DrawCount != 0)
                Add(errors, SiteReservationSearchErrorCode.SiteRngAlreadyConsumed,
                    string.Empty, string.Empty, -1, "The site RNG stream must be fresh.");
        }

        private static void ValidateGroups(
            IReadOnlyList<SiteReservationSearchGroup> groups,
            ICollection<SiteReservationSearchError> errors)
        {
            var expected = RequiredKeys();
            var seen = new HashSet<SitePlacementKey>();
            foreach (var group in groups)
            {
                if (!seen.Add(group.Key))
                    Add(errors, SiteReservationSearchErrorCode.DuplicateGroupKey,
                        group.Key.SourceDefinitionId, string.Empty, -1,
                        "Search group keys must be unique.");
                if (!Contains(expected, group.Key))
                    Add(errors, SiteReservationSearchErrorCode.UnexpectedGroup,
                        group.Key.SourceDefinitionId, string.Empty, -1,
                        "The search group is outside the exact required set.");
                if (group.OptionCount == 0)
                    Add(errors, SiteReservationSearchErrorCode.EmptyGroup,
                        group.Key.SourceDefinitionId, string.Empty, -1,
                        "Every required group needs at least one option.");

                var identities = new HashSet<OptionIdentity>();
                foreach (var option in group.Options)
                {
                    if (option == null)
                    {
                        Add(errors, SiteReservationSearchErrorCode.NullOption,
                            group.Key.SourceDefinitionId, string.Empty, -1,
                            "Search options cannot contain null.");
                        continue;
                    }
                    var placement = option.Placement;
                    if (placement == null || placement.Candidate == null || placement.Footprint == null)
                    {
                        Add(errors, SiteReservationSearchErrorCode.InvalidOption,
                            group.Key.SourceDefinitionId, string.Empty, -1,
                            "A search option requires a complete placement.");
                        continue;
                    }
                    var source = CanonicalOrEmpty(placement.Candidate.SourceDefinitionId);
                    if (SitePlacementKey.FromPlacement(placement) != group.Key ||
                        option.FutureCoreAvailableSectorCount < -1 ||
                        option.FutureCoreAvailableSectorCount > WorldGenConstants.SectorCount)
                    {
                        Add(errors, SiteReservationSearchErrorCode.InvalidOption,
                            group.Key.SourceDefinitionId, source,
                            ValidOrigin(placement.Candidate.OriginIndex),
                            "The search option identity or capacity is invalid.");
                    }
                    var identity = new OptionIdentity(
                        placement.Candidate.OriginIndex,
                        placement.Footprint.Transform);
                    if (!identities.Add(identity))
                        Add(errors, SiteReservationSearchErrorCode.DuplicateOptionIdentity,
                            group.Key.SourceDefinitionId, source,
                            ValidOrigin(placement.Candidate.OriginIndex),
                            "Option identities must be unique within a group.");
                }
            }

            foreach (var key in expected)
            {
                if (!seen.Contains(key))
                    Add(errors, SiteReservationSearchErrorCode.MissingRequiredGroup,
                        key.SourceDefinitionId, string.Empty, -1,
                        "An exact required search group is missing.");
            }
        }

        private static void ValidatePolicy(
            IReadOnlyList<SiteReservationSearchGroup> groups,
            SiteDistancePolicy policy,
            ICollection<SiteReservationSearchError> errors)
        {
            var expected = RequiredKeys();
            if (policy.Keys == null || policy.Keys.Count != expected.Count)
            {
                Add(errors, SiteReservationSearchErrorCode.PolicyKeyMismatch,
                    string.Empty, string.Empty, -1,
                    "The policy must contain the exact six required keys.");
                return;
            }
            for (var index = 0; index < expected.Count; index++)
            {
                if (policy.Keys[index] != expected[index])
                    Add(errors, SiteReservationSearchErrorCode.PolicyKeyMismatch,
                        expected[index].SourceDefinitionId, string.Empty, -1,
                        "The policy key set does not match the required search order.");
            }
            if (policy.ConstraintCount != 15 || policy.Constraints == null ||
                policy.Constraints.Count != 15)
            {
                Add(errors, SiteReservationSearchErrorCode.InvalidDistancePolicy,
                    string.Empty, string.Empty, -1,
                    "The required-site policy must contain exactly fifteen constraints.");
                return;
            }

            var byKey = new Dictionary<SitePlacementKey, SiteReservationSearchGroup>();
            foreach (var group in groups)
            {
                if (!byKey.ContainsKey(group.Key)) byKey.Add(group.Key, group);
            }
            for (var first = 0; first < expected.Count; first++)
            {
                for (var second = first + 1; second < expected.Count; second++)
                {
                    var firstKey = expected[first];
                    var secondKey = expected[second];
                    if (!policy.TryGetConstraint(firstKey, secondKey, out var constraint))
                    {
                        Add(errors, SiteReservationSearchErrorCode.InvalidDistancePolicy,
                            firstKey.SourceDefinitionId, secondKey.SourceDefinitionId, -1,
                            "A required policy pair is missing.");
                        continue;
                    }
                    if (!byKey.TryGetValue(firstKey, out var firstGroup) ||
                        !byKey.TryGetValue(secondKey, out var secondGroup)) continue;
                    var startPair = firstKey.Kind == SiteReservationKind.Start;
                    var expectedRule = startPair
                        ? SiteDistanceRuleKind.StartToRequiredSite
                        : SiteDistanceRuleKind.RequiredSiteToRequiredSite;
                    var expectedMinimum = startPair
                        ? secondGroup.SpecialMap.MinGraphDistanceFromStart
                        : Math.Max(firstGroup.SpecialMap.MinGraphDistanceToOtherCoreSites,
                            secondGroup.SpecialMap.MinGraphDistanceToOtherCoreSites);
                    if (constraint.RuleKind != expectedRule ||
                        constraint.MinimumDistance != expectedMinimum)
                    {
                        Add(errors, SiteReservationSearchErrorCode.InvalidDistancePolicy,
                            firstKey.SourceDefinitionId, secondKey.SourceDefinitionId, -1,
                            "A policy constraint does not match its typed definitions.");
                    }
                }
            }
        }

        private static void ValidateOptions(
            IReadOnlyList<SiteReservationSearchGroup> groups,
            SiteDistancePolicy policy,
            SiteCandidateCostWeights weights,
            ICollection<SiteReservationSearchError> errors)
        {
            var calculator = new SiteCandidateCostCalculator();
            foreach (var group in groups)
            {
                foreach (var option in group.Options)
                {
                    SiteCandidateCostResult result;
                    try
                    {
                        var context = new SiteCandidateCostContext(
                            policy,
                            Array.Empty<FootprintPlacement>(),
                            option.FutureCoreAvailableSectorCount);
                        result = calculator.Calculate(
                            option.Placement,
                            context,
                            group.SpecialMap,
                            group.PrimaryBiome,
                            group.CorePatchRule,
                            weights);
                    }
                    catch (Exception)
                    {
                        result = null;
                    }
                    if (result == null || !result.Succeeded)
                    {
                        Add(errors, SiteReservationSearchErrorCode.InvalidOption,
                            group.Key.SourceDefinitionId,
                            CanonicalOrEmpty(option.Placement.Candidate.SourceDefinitionId),
                            ValidOrigin(option.Placement.Candidate.OriginIndex),
                            "An option failed the typed candidate-cost preflight.");
                    }
                }
            }
        }

        private static GroupCounter[] CreateCounters(
            IReadOnlyList<SiteReservationSearchGroup> groups)
        {
            var counters = new GroupCounter[groups.Count];
            for (var index = 0; index < groups.Count; index++)
                counters[index] = new GroupCounter(groups[index].Key, groups[index].OptionCount);
            return counters;
        }

        private static SiteReservationSearchDiagnostics BuildDiagnostics(
            IReadOnlyList<GroupCounter> counters,
            int failedCombinations,
            int deepestDepth,
            ulong initialState,
            ulong drawBefore,
            ulong tieBreakDrawCount,
            ulong drawAfter)
        {
            var groups = new List<SiteReservationGroupDiagnostics>(counters.Count);
            foreach (var counter in counters) groups.Add(counter.Snapshot());
            return new SiteReservationSearchDiagnostics(
                groups,
                failedCombinations,
                deepestDepth,
                initialState,
                drawBefore,
                tieBreakDrawCount,
                drawAfter);
        }

        private static IReadOnlyList<SitePlacementKey> RequiredKeys() => new[]
        {
            new SitePlacementKey(SiteReservationKind.Start, WorldId, 0),
            new SitePlacementKey(SiteReservationKind.Boss, BossId, 0),
            new SitePlacementKey(SiteReservationKind.Forge, ForgeId, 0),
            new SitePlacementKey(SiteReservationKind.CoreResource, CassiaId, 0),
            new SitePlacementKey(SiteReservationKind.CoreResource, YeastId, 0),
            new SitePlacementKey(SiteReservationKind.CoreResource, MeteorId, 0)
        };

        private static bool Contains(IReadOnlyList<SitePlacementKey> keys, SitePlacementKey value)
        {
            foreach (var key in keys)
            {
                if (key == value) return true;
            }
            return false;
        }

        private static int ValidOrigin(int value) =>
            value >= 0 && value < WorldGenConstants.SectorCount ? value : -1;
        private static string CanonicalOrEmpty(string value) =>
            SitePlacementKey.IsCanonicalId(value) ? value : string.Empty;
        private static void Add(
            ICollection<SiteReservationSearchError> errors,
            SiteReservationSearchErrorCode code,
            string group,
            string candidate,
            int origin,
            string message) => errors.Add(new SiteReservationSearchError(
                code, CanonicalOrEmpty(group), CanonicalOrEmpty(candidate), origin, message));

        private enum SearchOutcome
        {
            Completed,
            NoSolution,
            LimitReached,
            InvalidInput
        }

        private sealed class SearchState
        {
            private readonly IReadOnlyList<SiteReservationSearchGroup> groups;
            private readonly SiteDistancePolicy policy;
            private readonly SiteCandidateCostWeights weights;
            private readonly SiteReservationSearchLimits limits;
            private readonly ulong[][] tieBreaks;
            private readonly GroupCounter[] counters;
            private readonly SitePlacementConflictDetector conflictDetector =
                new SitePlacementConflictDetector();
            private readonly SiteCandidateCostCalculator costCalculator =
                new SiteCandidateCostCalculator();
            private readonly List<FootprintPlacement> selected = new List<FootprintPlacement>();
            private readonly List<SiteReservationSelectionStep> steps =
                new List<SiteReservationSelectionStep>();
            private readonly List<SiteReservationSearchError> errors =
                new List<SiteReservationSearchError>();

            public SearchState(
                IReadOnlyList<SiteReservationSearchGroup> groups,
                SiteDistancePolicy policy,
                SiteCandidateCostWeights weights,
                SiteReservationSearchLimits limits,
                ulong[][] tieBreaks,
                GroupCounter[] counters)
            {
                this.groups = groups;
                this.policy = policy;
                this.weights = weights;
                this.limits = limits;
                this.tieBreaks = tieBreaks;
                this.counters = counters;
            }

            public int FailedCombinationCount { get; private set; }
            public int DeepestSelectedDepth { get; private set; }
            public SiteReservationSelectionPlan CompletedPlan { get; private set; }
            public IReadOnlyList<SiteReservationSearchError> Errors =>
                SiteReservationSearchResult.SnapshotErrors(errors);

            public SearchOutcome Visit(int depth)
            {
                var group = groups[depth];
                var counter = counters[depth];
                counter.StateVisitCount++;
                var viable = new List<RankedOption>();
                for (var optionIndex = 0; optionIndex < group.OptionCount; optionIndex++)
                {
                    var option = group.Options[optionIndex];
                    counter.CandidateEvaluationCount++;
                    var reasons = new List<SiteReservationRejectionReason>(
                        conflictDetector.Evaluate(option.Placement, selected));
                    if (reasons.Count == 0)
                    {
                        SiteCandidateCostResult cost;
                        try
                        {
                            cost = costCalculator.Calculate(
                                option.Placement,
                                new SiteCandidateCostContext(
                                    policy,
                                    selected,
                                    option.FutureCoreAvailableSectorCount),
                                group.SpecialMap,
                                group.PrimaryBiome,
                                group.CorePatchRule,
                                weights);
                        }
                        catch (Exception)
                        {
                            cost = null;
                        }
                        if (cost == null || !cost.Succeeded)
                        {
                            Add(errors, SiteReservationSearchErrorCode.CostEvaluationFailed,
                                group.Key.SourceDefinitionId,
                                option.Placement.Candidate.SourceDefinitionId,
                                option.Placement.Candidate.OriginIndex,
                                "Candidate-cost evaluation failed during search.");
                            return SearchOutcome.InvalidInput;
                        }
                        if (cost.Breakdown.DistanceUnits > 0)
                            reasons.Add(SiteReservationRejectionReason.DistanceConstraint);
                        if (cost.Breakdown.CoreClusterUnits > 0)
                            reasons.Add(SiteReservationRejectionReason.CoreCluster);
                        if (reasons.Count == 0)
                        {
                            viable.Add(new RankedOption(
                                option,
                                cost.Breakdown,
                                tieBreaks[depth][optionIndex],
                                optionIndex));
                        }
                    }

                    if (reasons.Count != 0) counter.Reject(reasons);
                }

                viable.Sort(RankedOption.Compare);
                foreach (var ranked in viable)
                {
                    selected.Add(ranked.Option.Placement);
                    steps.Add(new SiteReservationSelectionStep(
                        depth,
                        group.Key,
                        ranked.Option,
                        ranked.Cost,
                        ranked.RandomTieBreak,
                        ranked.CanonicalOptionOrdinal));
                    counter.SelectionPushCount++;
                    if (selected.Count > DeepestSelectedDepth) DeepestSelectedDepth = selected.Count;

                    SearchOutcome outcome;
                    if (depth == groups.Count - 1)
                    {
                        outcome = TryCompletePlan();
                    }
                    else
                    {
                        outcome = Visit(depth + 1);
                    }

                    if (outcome == SearchOutcome.Completed ||
                        outcome == SearchOutcome.InvalidInput ||
                        outcome == SearchOutcome.LimitReached)
                        return outcome;

                    selected.RemoveAt(selected.Count - 1);
                    steps.RemoveAt(steps.Count - 1);
                    counter.BacktrackPopCount++;
                    FailedCombinationCount++;
                    if (FailedCombinationCount >= limits.MaxFailedCombinations)
                        return SearchOutcome.LimitReached;
                }

                counter.ExhaustionCount++;
                return SearchOutcome.NoSolution;
            }

            private SearchOutcome TryCompletePlan()
            {
                var indexResult = new SiteDistanceIndexBuilder().Build(selected);
                if (!indexResult.Succeeded || indexResult.Index.PlacementCount != 6 ||
                    indexResult.Index.PairCount != 15)
                {
                    Add(errors, SiteReservationSearchErrorCode.FinalDistanceEvaluationFailed,
                        string.Empty, string.Empty, -1,
                        "The completed selection could not build the exact distance index.");
                    return SearchOutcome.InvalidInput;
                }
                var evaluation = indexResult.Index.Evaluate(policy);
                if (!evaluation.Succeeded || !evaluation.Satisfied ||
                    evaluation.Errors.Count != 0 || evaluation.Violations.Count != 0)
                {
                    Add(errors, SiteReservationSearchErrorCode.FinalDistanceEvaluationFailed,
                        string.Empty, string.Empty, -1,
                        "The completed selection failed its final distance policy.");
                    return SearchOutcome.InvalidInput;
                }
                try
                {
                    CompletedPlan = new SiteReservationSelectionPlan(steps);
                    return SearchOutcome.Completed;
                }
                catch (Exception)
                {
                    Add(errors, SiteReservationSearchErrorCode.InternalInvariantViolation,
                        string.Empty, string.Empty, -1,
                        "The completed selection violated a plan invariant.");
                    return SearchOutcome.InvalidInput;
                }
            }
        }

        private sealed class GroupCounter
        {
            private readonly int[] reasons = new int[5];

            public GroupCounter(SitePlacementKey key, int sourceOptionCount)
            {
                Key = key;
                SourceOptionCount = sourceOptionCount;
            }

            public SitePlacementKey Key { get; }
            public int SourceOptionCount { get; }
            public int StateVisitCount { get; set; }
            public int CandidateEvaluationCount { get; set; }
            public int SelectionPushCount { get; set; }
            public int BacktrackPopCount { get; set; }
            public int ExhaustionCount { get; set; }
            public int RejectedOptionEvaluationCount { get; private set; }

            public void Reject(IEnumerable<SiteReservationRejectionReason> values)
            {
                RejectedOptionEvaluationCount++;
                var seen = new bool[5];
                foreach (var value in values)
                {
                    if (Enum.IsDefined(typeof(SiteReservationRejectionReason), value))
                        seen[(int)value] = true;
                }
                for (var index = 0; index < seen.Length; index++)
                {
                    if (seen[index]) reasons[index]++;
                }
            }

            public SiteReservationGroupDiagnostics Snapshot() =>
                new SiteReservationGroupDiagnostics(
                    Key,
                    SourceOptionCount,
                    StateVisitCount,
                    CandidateEvaluationCount,
                    SelectionPushCount,
                    BacktrackPopCount,
                    ExhaustionCount,
                    RejectedOptionEvaluationCount,
                    reasons);
        }

        private sealed class RankedOption
        {
            public RankedOption(
                SiteReservationSearchOption option,
                SiteCandidateCostBreakdown cost,
                ulong randomTieBreak,
                int canonicalOptionOrdinal)
            {
                Option = option;
                Cost = cost;
                RandomTieBreak = randomTieBreak;
                CanonicalOptionOrdinal = canonicalOptionOrdinal;
            }

            public SiteReservationSearchOption Option { get; }
            public SiteCandidateCostBreakdown Cost { get; }
            public ulong RandomTieBreak { get; }
            public int CanonicalOptionOrdinal { get; }

            public static int Compare(RankedOption left, RankedOption right)
            {
                var cost = left.Cost.TotalCost.CompareTo(right.Cost.TotalCost);
                if (cost != 0) return cost;
                var random = left.RandomTieBreak.CompareTo(right.RandomTieBreak);
                if (random != 0) return random;
                var origin = left.Option.Placement.Candidate.OriginIndex.CompareTo(
                    right.Option.Placement.Candidate.OriginIndex);
                if (origin != 0) return origin;
                var transform = left.Option.Placement.Footprint.Transform.CompareTo(
                    right.Option.Placement.Footprint.Transform);
                return transform != 0
                    ? transform
                    : left.Option.Placement.Candidate.CandidateOrdinal.CompareTo(
                        right.Option.Placement.Candidate.CandidateOrdinal);
            }
        }

        private readonly struct OptionIdentity : IEquatable<OptionIdentity>
        {
            private readonly int originIndex;
            private readonly SiteFootprintTransform transform;

            public OptionIdentity(int originIndex, SiteFootprintTransform transform)
            {
                this.originIndex = originIndex;
                this.transform = transform;
            }

            public bool Equals(OptionIdentity other) =>
                originIndex == other.originIndex && transform == other.transform;
            public override bool Equals(object obj) => obj is OptionIdentity other && Equals(other);
            public override int GetHashCode() => (originIndex * 397) ^ (int)transform;
        }
    }
}
