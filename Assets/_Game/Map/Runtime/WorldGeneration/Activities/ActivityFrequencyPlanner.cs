using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.WorldGeneration.Activities
{
    public enum ActivityScopeKind
    {
        World = 0,
        BiomePatch = 1,
        Sector = 2,
    }

    public sealed class ActivityFrequencyPolicy
    {
        public ActivityFrequencyPolicy(
            int targetPermille,
            int maxStrongPerWorld,
            int maxStrongPerPatch,
            int maxStrongPerSector)
        {
            TargetPermille = targetPermille;
            MaxStrongPerWorld = maxStrongPerWorld;
            MaxStrongPerPatch = maxStrongPerPatch;
            MaxStrongPerSector = maxStrongPerSector;
        }

        public int TargetPermille { get; }
        public int MaxStrongPerWorld { get; }
        public int MaxStrongPerPatch { get; }
        public int MaxStrongPerSector { get; }
    }

    public sealed class ActivityScopeBudget
    {
        internal ActivityScopeBudget(
            ActivityScopeKind scopeKind,
            string scopeId,
            int eligibleCount,
            int targetCount,
            int selectedCount,
            int ordinaryCount,
            int strongCount,
            int lowerCount,
            int upperCount,
            bool bandFeasible,
            bool discreteApproximation)
        {
            ScopeKind = scopeKind;
            ScopeId = scopeId ?? string.Empty;
            EligibleCount = eligibleCount;
            TargetCount = targetCount;
            SelectedCount = selectedCount;
            OrdinaryCount = ordinaryCount;
            StrongCount = strongCount;
            LowerCount = lowerCount;
            UpperCount = upperCount;
            ExactRateNumerator = selectedCount;
            ExactRateDenominator = eligibleCount;
            AchievedPermilleNumerator = selectedCount * 1000L;
            AchievedPermilleDenominator = eligibleCount;
            BandFeasible = bandFeasible;
            DiscreteApproximation = discreteApproximation;
        }

        public ActivityScopeKind ScopeKind { get; }
        public string ScopeId { get; }
        public int EligibleCount { get; }
        public int TargetCount { get; }
        public int SelectedCount { get; }
        public int OrdinaryCount { get; }
        public int StrongCount { get; }
        public int LowerCount { get; }
        public int UpperCount { get; }
        public int ExactRateNumerator { get; }
        public int ExactRateDenominator { get; }
        public long AchievedPermilleNumerator { get; }
        public int AchievedPermilleDenominator { get; }
        public bool BandFeasible { get; }
        public bool DiscreteApproximation { get; }
    }

    public sealed class ActivityPlacementDecision
    {
        internal ActivityPlacementDecision(
            ActivityPlacementCandidate candidate,
            ulong priority,
            ulong priorityDrawCountBefore,
            ulong priorityDrawCountAfter,
            int totalWeight,
            int weightedTicket,
            ulong weightedDrawCountBefore,
            ulong weightedDrawCountAfter,
            int worldStrongBefore,
            int worldStrongAfter,
            int patchStrongBefore,
            int patchStrongAfter,
            int sectorStrongBefore,
            int sectorStrongAfter)
        {
            Candidate = candidate;
            Priority = priority;
            PriorityDrawCountBefore = priorityDrawCountBefore;
            PriorityDrawCountAfter = priorityDrawCountAfter;
            TotalWeight = totalWeight;
            WeightedTicket = weightedTicket;
            WeightedDrawCountBefore = weightedDrawCountBefore;
            WeightedDrawCountAfter = weightedDrawCountAfter;
            WorldStrongBefore = worldStrongBefore;
            WorldStrongAfter = worldStrongAfter;
            PatchStrongBefore = patchStrongBefore;
            PatchStrongAfter = patchStrongAfter;
            SectorStrongBefore = sectorStrongBefore;
            SectorStrongAfter = sectorStrongAfter;
        }

        public ActivityPlacementCandidate Candidate { get; }
        public string OpportunityId => Candidate.OpportunityId;
        public ActivityStructureId ActivityId => Candidate.ActivityId;
        public string CandidateKey => Candidate.CandidateKey;
        public BiomePatchId PatchId => Candidate.Opportunity.PatchId;
        public SectorCoord Sector => Candidate.Opportunity.Sector;
        public ActivityStrengthClass Strength => Candidate.Strength;
        public int Weight => Candidate.Weight;
        public int TotalWeight { get; }
        public int WeightedTicket { get; }
        public ulong Priority { get; }
        public ulong PriorityDrawCountBefore { get; }
        public ulong PriorityDrawCountAfter { get; }
        public ulong WeightedDrawCountBefore { get; }
        public ulong WeightedDrawCountAfter { get; }
        public int WorldStrongBefore { get; }
        public int WorldStrongAfter { get; }
        public int PatchStrongBefore { get; }
        public int PatchStrongAfter { get; }
        public int SectorStrongBefore { get; }
        public int SectorStrongAfter { get; }
    }

    public sealed class ActivityFrequencyPlan
    {
        private readonly ReadOnlyCollection<ActivityScopeBudget> budgets;
        private readonly ReadOnlyCollection<ActivityPlacementDecision> decisions;

        internal ActivityFrequencyPlan(
            ActivityFrequencyPolicy policy,
            ulong worldSeed,
            int attemptOrdinal,
            IEnumerable<ActivityScopeBudget> budgets,
            IEnumerable<ActivityPlacementDecision> decisions,
            int rngStreamCreationCount,
            ulong rngDrawCount,
            string canonicalDigest)
        {
            Policy = policy;
            WorldSeed = worldSeed;
            AttemptOrdinal = attemptOrdinal;
            this.budgets = new ReadOnlyCollection<ActivityScopeBudget>((budgets ?? Array.Empty<ActivityScopeBudget>()).ToArray());
            this.decisions = new ReadOnlyCollection<ActivityPlacementDecision>((decisions ?? Array.Empty<ActivityPlacementDecision>()).ToArray());
            RngStreamId = WorldGenerationRngStreams.SectorRecipeStreamId;
            RngStreamCreationCount = rngStreamCreationCount;
            RngDrawCount = rngDrawCount;
            CanonicalDigest = canonicalDigest ?? string.Empty;
        }

        public ActivityFrequencyPolicy Policy { get; }
        public ulong WorldSeed { get; }
        public int AttemptOrdinal { get; }
        public string RngStreamId { get; }
        public IReadOnlyList<ActivityScopeBudget> Budgets => budgets;
        public IReadOnlyList<ActivityPlacementDecision> Decisions => decisions;
        public ActivityScopeBudget WorldBudget => budgets.First(value => value.ScopeKind == ActivityScopeKind.World);
        public IReadOnlyList<ActivityScopeBudget> PatchBudgets => budgets.Where(value => value.ScopeKind == ActivityScopeKind.BiomePatch).ToArray();
        public IReadOnlyList<ActivityScopeBudget> SectorBudgets => budgets.Where(value => value.ScopeKind == ActivityScopeKind.Sector).ToArray();
        public int RngStreamCreationCount { get; }
        public ulong RngDrawCount { get; }
        public int GeometryWriteCount => 0;
        public int CanvasMutationCount => 0;
        public int PrefabMutationCount => 0;
        public int SceneMutationCount => 0;
        public int TilemapMutationCount => 0;
        public string CanonicalDigest { get; }
    }

    public sealed class ActivityFrequencyPlanRequest
    {
        public ActivityFrequencyPlanRequest(
            ActivityCandidateIndex candidateIndex,
            ActivityFrequencyPolicy policy,
            ulong worldSeed,
            int attemptOrdinal,
            DeterministicRngStreamFactory rngStreamFactory)
        {
            CandidateIndex = candidateIndex;
            Policy = policy;
            WorldSeed = worldSeed;
            AttemptOrdinal = attemptOrdinal;
            RngStreamFactory = rngStreamFactory;
        }

        public ActivityCandidateIndex CandidateIndex { get; }
        public ActivityFrequencyPolicy Policy { get; }
        public ulong WorldSeed { get; }
        public int AttemptOrdinal { get; }
        public DeterministicRngStreamFactory RngStreamFactory { get; }
    }

    public sealed class ActivityFrequencyPlanResult
    {
        private readonly ReadOnlyCollection<ActivityCompatibilityError> errors;

        internal ActivityFrequencyPlanResult(
            ActivityFrequencyPlan plan,
            IEnumerable<ActivityCompatibilityError> errors,
            int rngStreamCreationCount,
            ulong rngDrawCount)
        {
            Plan = plan;
            this.errors = new ReadOnlyCollection<ActivityCompatibilityError>(
                ActivityCompatibilityCanonical.SortErrors(errors).ToArray());
            RngStreamCreationCount = rngStreamCreationCount;
            RngDrawCount = rngDrawCount;
        }

        public bool Success => Plan != null && errors.Count == 0;
        public ActivityFrequencyPlan Plan { get; }
        public IReadOnlyList<ActivityCompatibilityError> Errors => errors;
        public int RngStreamCreationCount { get; }
        public ulong RngDrawCount { get; }
    }

    public static class ActivityFrequencyPlanner
    {
        private const string RulesetVersion = "MAP12_03_FREQUENCY_V1";
        private const int MinimumPermille = 60;
        private const int MaximumPermille = 120;

        public static ActivityFrequencyPlanResult Plan(ActivityFrequencyPlanRequest request)
        {
            var errors = new List<ActivityCompatibilityError>();
            ValidateRequest(request, errors);
            if (errors.Count != 0) return Failure(errors, 0, 0);

            var opportunityGroups = request.CandidateIndex.Candidates
                .GroupBy(value => value.OpportunityId, StringComparer.Ordinal)
                .Select(group => new OpportunityGroup(group.Key, group.ToArray()))
                .OrderBy(value => value.Opportunity.Opportunity.PatchId)
                .ThenBy(value => value.Opportunity.Opportunity.Sector.Y)
                .ThenBy(value => value.Opportunity.Opportunity.Sector.X)
                .ThenBy(value => value.OpportunityId, StringComparer.Ordinal).ToArray();

            foreach (var group in opportunityGroups)
            {
                long total = 0;
                try
                {
                    checked { foreach (var candidate in group.Candidates) total += candidate.Weight; }
                }
                catch (OverflowException)
                {
                    Add(errors, ActivityCompatibilityErrorCode.InvalidProfile,
                        "candidateWeights[" + group.OpportunityId + "]", "Candidate weight sum overflowed.");
                }
                if (total <= 0 || total > int.MaxValue)
                    Add(errors, ActivityCompatibilityErrorCode.InvalidProfile,
                        "candidateWeights[" + group.OpportunityId + "]", "Candidate weight sum must fit a positive Int32.");
            }
            if (errors.Count != 0) return Failure(errors, 0, 0);

            var worldTarget = TargetFor(opportunityGroups.Length, request.Policy.TargetPermille);
            var patchGroups = opportunityGroups.GroupBy(value => value.Opportunity.Opportunity.PatchId)
                .OrderBy(value => value.Key).ToArray();
            var patchAllocation = Allocate(worldTarget, opportunityGroups.Length,
                patchGroups.Select(value => new AllocationChild(value.Key.Value, value.Count())).ToArray());
            var sectorAllocation = new Dictionary<SectorBudgetKey, int>();
            foreach (var patch in patchGroups)
            {
                var sectors = patch.GroupBy(value => value.Opportunity.Opportunity.Sector)
                    .OrderBy(value => value.Key.Y).ThenBy(value => value.Key.X).ToArray();
                var children = sectors.Select(value => new AllocationChild(SectorId(value.Key), value.Count())).ToArray();
                var allocation = Allocate(patchAllocation[patch.Key.Value], patch.Count(), children);
                foreach (var sector in sectors)
                    sectorAllocation[new SectorBudgetKey(patch.Key, sector.Key)] = allocation[SectorId(sector.Key)];
            }

            if (patchAllocation.Values.Sum() != worldTarget || sectorAllocation.Values.Sum() != worldTarget)
            {
                Add(errors, ActivityCompatibilityErrorCode.BudgetMismatch, "budgets", "World, patch, and sector target sums must be exact.");
                return Failure(errors, 0, 0);
            }

            var decisions = new List<ActivityPlacementDecision>();
            var worldStrong = 0;
            var patchStrong = new Dictionary<BiomePatchId, int>();
            var sectorStrong = new Dictionary<SectorBudgetKey, int>();
            var streamCount = 0;
            ulong drawCount = 0;
            var capBlocked = false;

            foreach (var patch in patchGroups)
            {
                foreach (var sector in patch.GroupBy(value => value.Opportunity.Opportunity.Sector)
                             .OrderBy(value => value.Key.Y).ThenBy(value => value.Key.X))
                {
                    var sectorKey = new SectorBudgetKey(patch.Key, sector.Key);
                    var quota = sectorAllocation[sectorKey];
                    DeterministicRngStream stream;
                    try
                    {
                        stream = request.RngStreamFactory.Create(
                            WorldGenerationRngStreams.SectorRecipeStreamId,
                            request.WorldSeed,
                            RngStreamScope.Sector(sector.Key, request.AttemptOrdinal));
                    }
                    catch (Exception exception)
                    {
                        Add(errors, ActivityCompatibilityErrorCode.InvalidRngBinding,
                            "rng[" + SectorId(sector.Key) + "]", exception.GetType().Name + ":" + exception.Message);
                        return Failure(errors, streamCount, drawCount);
                    }
                    streamCount++;
                    var prioritized = new List<PrioritizedOpportunity>();
                    foreach (var opportunity in sector.OrderBy(value => value.OpportunityId, StringComparer.Ordinal))
                    {
                        var before = stream.DrawCount;
                        var priority = stream.NextUInt64();
                        prioritized.Add(new PrioritizedOpportunity(opportunity, priority, before, stream.DrawCount));
                    }
                    prioritized.Sort(PrioritizedOpportunity.Compare);

                    var selectedInSector = 0;
                    foreach (var prioritizedOpportunity in prioritized)
                    {
                        if (selectedInSector >= quota) break;
                        if (!patchStrong.TryGetValue(patch.Key, out var currentPatchStrong)) currentPatchStrong = 0;
                        if (!sectorStrong.TryGetValue(sectorKey, out var currentSectorStrong)) currentSectorStrong = 0;
                        var capsPermitStrong = worldStrong < request.Policy.MaxStrongPerWorld &&
                                               currentPatchStrong < request.Policy.MaxStrongPerPatch &&
                                               currentSectorStrong < request.Policy.MaxStrongPerSector;
                        var allowed = prioritizedOpportunity.Group.Candidates
                            .Where(value => value.Strength == ActivityStrengthClass.Ordinary || capsPermitStrong)
                            .OrderBy(value => value.ActivityId.Value, StringComparer.Ordinal)
                            .ThenBy(value => value.CandidateKey, StringComparer.Ordinal).ToArray();
                        if (allowed.Length == 0)
                        {
                            if (prioritizedOpportunity.Group.Candidates.Any(value => value.Strength == ActivityStrengthClass.Strong))
                                capBlocked = true;
                            continue;
                        }

                        var totalWeight = allowed.Sum(value => value.Weight);
                        var weightedBefore = stream.DrawCount;
                        var ticket = stream.NextInt(totalWeight);
                        var remaining = ticket;
                        var chosen = allowed[allowed.Length - 1];
                        foreach (var candidate in allowed)
                        {
                            if (remaining < candidate.Weight) { chosen = candidate; break; }
                            remaining -= candidate.Weight;
                        }
                        var strong = chosen.Strength == ActivityStrengthClass.Strong;
                        var worldBefore = worldStrong;
                        var patchBefore = currentPatchStrong;
                        var sectorBefore = currentSectorStrong;
                        if (strong)
                        {
                            worldStrong++;
                            currentPatchStrong++;
                            currentSectorStrong++;
                            patchStrong[patch.Key] = currentPatchStrong;
                            sectorStrong[sectorKey] = currentSectorStrong;
                        }
                        decisions.Add(new ActivityPlacementDecision(
                            chosen, prioritizedOpportunity.Priority,
                            prioritizedOpportunity.DrawBefore, prioritizedOpportunity.DrawAfter,
                            totalWeight, ticket, weightedBefore, stream.DrawCount,
                            worldBefore, worldStrong, patchBefore, currentPatchStrong,
                            sectorBefore, currentSectorStrong));
                        selectedInSector++;
                    }
                    drawCount += stream.DrawCount;
                    if (selectedInSector != quota)
                    {
                        Add(errors,
                            capBlocked ? ActivityCompatibilityErrorCode.StrongCapUnsatisfiable : ActivityCompatibilityErrorCode.TargetUnsatisfied,
                            "sector[" + SectorId(sector.Key) + "]",
                            "Selected " + Number(selectedInSector) + " of target " + Number(quota) + ".");
                    }
                }
            }
            if (errors.Count != 0) return Failure(errors, streamCount, drawCount);

            var budgets = BuildBudgets(opportunityGroups, patchGroups, patchAllocation, sectorAllocation, decisions, worldTarget);
            if (budgets.First().SelectedCount != worldTarget ||
                budgets.Where(value => value.ScopeKind == ActivityScopeKind.BiomePatch).Sum(value => value.SelectedCount) != worldTarget ||
                budgets.Where(value => value.ScopeKind == ActivityScopeKind.Sector).Sum(value => value.SelectedCount) != worldTarget)
            {
                Add(errors, ActivityCompatibilityErrorCode.BudgetMismatch, "publication", "Published selection sums must equal the world target.");
                return Failure(errors, streamCount, drawCount);
            }

            var digest = ComputeDigest(request, budgets, decisions, streamCount, drawCount);
            var plan = new ActivityFrequencyPlan(request.Policy, request.WorldSeed, request.AttemptOrdinal,
                budgets, decisions, streamCount, drawCount, digest);
            return new ActivityFrequencyPlanResult(plan, Array.Empty<ActivityCompatibilityError>(), streamCount, drawCount);
        }

        private static void ValidateRequest(
            ActivityFrequencyPlanRequest request,
            ICollection<ActivityCompatibilityError> errors)
        {
            if (request == null)
            {
                Add(errors, ActivityCompatibilityErrorCode.MissingInput, "request", "Plan request is required.");
                return;
            }
            if (request.CandidateIndex == null || request.CandidateIndex.CandidateCount == 0)
                Add(errors, ActivityCompatibilityErrorCode.EmptyCandidateIndex, "candidateIndex", "A non-empty published candidate index is required.");
            if (request.Policy == null)
                Add(errors, ActivityCompatibilityErrorCode.MissingInput, "policy", "Frequency policy is required.");
            else
            {
                if (request.Policy.TargetPermille < MinimumPermille || request.Policy.TargetPermille > MaximumPermille)
                    Add(errors, ActivityCompatibilityErrorCode.InvalidFrequencyPolicy, "policy.targetPermille", "TargetPermille must be 60..120 inclusive.");
                if (request.Policy.MaxStrongPerWorld < 0 || request.Policy.MaxStrongPerPatch < 0 || request.Policy.MaxStrongPerSector < 0)
                    Add(errors, ActivityCompatibilityErrorCode.InvalidStrongCap, "policy.strongCaps", "Strong caps must be non-negative.");
            }
            if (request.AttemptOrdinal < 0)
                Add(errors, ActivityCompatibilityErrorCode.InvalidRngBinding, "attemptOrdinal", "Attempt ordinal must be non-negative.");
            if (request.RngStreamFactory == null)
                Add(errors, ActivityCompatibilityErrorCode.MissingInput, "rngStreamFactory", "DeterministicRngStreamFactory is required.");
            else
            {
                try
                {
                    var definition = request.RngStreamFactory.GetDefinition(WorldGenerationRngStreams.SectorRecipeStreamId);
                    var scope = DeterministicRngSeedDeriver.ValidateDefinition(definition);
                    if (!definition.Active || scope != RngResetScope.Sector)
                        Add(errors, ActivityCompatibilityErrorCode.InvalidRngBinding, "rngStreamFactory.RNG_SECTOR_RECIPE", "The required stream must be active with SECTOR reset scope.");
                }
                catch (Exception exception)
                {
                    Add(errors, ActivityCompatibilityErrorCode.InvalidRngBinding, "rngStreamFactory.RNG_SECTOR_RECIPE",
                        exception.GetType().Name + ":" + exception.Message);
                }
            }
        }

        private static int TargetFor(int eligible, int targetPermille)
        {
            var rounded = (eligible * targetPermille + 500) / 1000;
            var lower = LowerCount(eligible);
            var upper = UpperCount(eligible);
            return lower <= upper ? Math.Max(lower, Math.Min(upper, rounded)) : rounded;
        }

        private static Dictionary<string, int> Allocate(
            int parentTarget,
            int parentEligible,
            IReadOnlyList<AllocationChild> children)
        {
            var result = children.ToDictionary(value => value.Id, value => 0, StringComparer.Ordinal);
            if (parentTarget == 0 || parentEligible == 0) return result;
            var shares = children.Select(value => new AllocationShare(
                value.Id,
                (int)((long)parentTarget * value.Eligible / parentEligible),
                (long)parentTarget * value.Eligible % parentEligible)).ToArray();
            foreach (var share in shares) result[share.Id] = share.Floor;
            var remainder = parentTarget - shares.Sum(value => value.Floor);
            foreach (var share in shares.OrderByDescending(value => value.Remainder)
                         .ThenBy(value => value.Id, StringComparer.Ordinal).Take(remainder))
                result[share.Id]++;
            return result;
        }

        private static IReadOnlyList<ActivityScopeBudget> BuildBudgets(
            IReadOnlyList<OpportunityGroup> opportunities,
            IEnumerable<IGrouping<BiomePatchId, OpportunityGroup>> patchGroups,
            IReadOnlyDictionary<string, int> patchAllocation,
            IReadOnlyDictionary<SectorBudgetKey, int> sectorAllocation,
            IReadOnlyList<ActivityPlacementDecision> decisions,
            int worldTarget)
        {
            var budgets = new List<ActivityScopeBudget>();
            budgets.Add(Budget(ActivityScopeKind.World, "WORLD", opportunities.Count, worldTarget, decisions));
            foreach (var patch in patchGroups)
            {
                var selected = decisions.Where(value => value.PatchId == patch.Key).ToArray();
                budgets.Add(Budget(ActivityScopeKind.BiomePatch, patch.Key.Value, patch.Count(), patchAllocation[patch.Key.Value], selected));
                foreach (var sector in patch.GroupBy(value => value.Opportunity.Opportunity.Sector)
                             .OrderBy(value => value.Key.Y).ThenBy(value => value.Key.X))
                {
                    var key = new SectorBudgetKey(patch.Key, sector.Key);
                    var sectorSelected = selected.Where(value => value.Sector == sector.Key).ToArray();
                    budgets.Add(Budget(ActivityScopeKind.Sector, SectorId(sector.Key), sector.Count(), sectorAllocation[key], sectorSelected));
                }
            }
            return budgets;
        }

        private static ActivityScopeBudget Budget(
            ActivityScopeKind kind,
            string id,
            int eligible,
            int target,
            IEnumerable<ActivityPlacementDecision> decisions)
        {
            var copy = decisions.ToArray();
            var lower = LowerCount(eligible);
            var upper = UpperCount(eligible);
            var feasible = lower <= upper;
            return new ActivityScopeBudget(kind, id, eligible, target, copy.Length,
                copy.Count(value => value.Strength == ActivityStrengthClass.Ordinary),
                copy.Count(value => value.Strength == ActivityStrengthClass.Strong),
                lower, upper, feasible, !feasible || target < lower || target > upper);
        }

        private static int LowerCount(int eligible) => (eligible * MinimumPermille + 999) / 1000;
        private static int UpperCount(int eligible) => eligible * MaximumPermille / 1000;

        private static string ComputeDigest(
            ActivityFrequencyPlanRequest request,
            IEnumerable<ActivityScopeBudget> budgets,
            IEnumerable<ActivityPlacementDecision> decisions,
            int streamCount,
            ulong drawCount)
        {
            var material = new StringBuilder();
            ActivityCompatibilityCanonical.Append(material, "RULESET", RulesetVersion);
            ActivityCompatibilityCanonical.Append(material, "INDEX", request.CandidateIndex.CanonicalDigest);
            ActivityCompatibilityCanonical.Append(material, "POLICY", Number(request.Policy.TargetPermille),
                Number(request.Policy.MaxStrongPerWorld), Number(request.Policy.MaxStrongPerPatch),
                Number(request.Policy.MaxStrongPerSector));
            ActivityCompatibilityCanonical.Append(material, "RNG", WorldGenerationRngStreams.SectorRecipeStreamId,
                request.WorldSeed.ToString(CultureInfo.InvariantCulture), Number(request.AttemptOrdinal),
                Number(streamCount), drawCount.ToString(CultureInfo.InvariantCulture));
            foreach (var budget in budgets)
                ActivityCompatibilityCanonical.Append(material, "BUDGET", Number((int)budget.ScopeKind), budget.ScopeId,
                    Number(budget.EligibleCount), Number(budget.TargetCount), Number(budget.SelectedCount),
                    Number(budget.OrdinaryCount), Number(budget.StrongCount), Number(budget.LowerCount),
                    Number(budget.UpperCount), budget.BandFeasible ? "1" : "0", budget.DiscreteApproximation ? "1" : "0");
            foreach (var decision in decisions)
                ActivityCompatibilityCanonical.Append(material, "DECISION", decision.OpportunityId,
                    decision.ActivityId.Value, decision.CandidateKey, decision.PatchId.Value,
                    Number(decision.Sector.X), Number(decision.Sector.Y), Number((int)decision.Strength),
                    Number(decision.Weight), Number(decision.TotalWeight), Number(decision.WeightedTicket),
                    decision.Priority.ToString(CultureInfo.InvariantCulture),
                    decision.PriorityDrawCountBefore.ToString(CultureInfo.InvariantCulture),
                    decision.PriorityDrawCountAfter.ToString(CultureInfo.InvariantCulture),
                    decision.WeightedDrawCountBefore.ToString(CultureInfo.InvariantCulture),
                    decision.WeightedDrawCountAfter.ToString(CultureInfo.InvariantCulture),
                    Number(decision.WorldStrongBefore), Number(decision.WorldStrongAfter),
                    Number(decision.PatchStrongBefore), Number(decision.PatchStrongAfter),
                    Number(decision.SectorStrongBefore), Number(decision.SectorStrongAfter));
            return ActivityCompatibilityCanonical.Sha256(material.ToString());
        }

        private static ActivityFrequencyPlanResult Failure(
            IEnumerable<ActivityCompatibilityError> errors,
            int streams,
            ulong draws)
        {
            return new ActivityFrequencyPlanResult(null, errors, streams, draws);
        }

        private static void Add(
            ICollection<ActivityCompatibilityError> errors,
            ActivityCompatibilityErrorCode code,
            string path,
            string detail)
        {
            errors.Add(new ActivityCompatibilityError(code, path, detail));
        }

        private static string SectorId(SectorCoord value)
        {
            return value.X.ToString("D2", CultureInfo.InvariantCulture) + "," +
                   value.Y.ToString("D2", CultureInfo.InvariantCulture);
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private sealed class OpportunityGroup
        {
            public OpportunityGroup(string opportunityId, ActivityPlacementCandidate[] candidates)
            {
                OpportunityId = opportunityId;
                Candidates = candidates.OrderBy(value => value.ActivityId.Value, StringComparer.Ordinal)
                    .ThenBy(value => value.CandidateKey, StringComparer.Ordinal).ToArray();
                Opportunity = Candidates[0];
            }
            public string OpportunityId { get; }
            public ActivityPlacementCandidate[] Candidates { get; }
            public ActivityPlacementCandidate Opportunity { get; }
        }

        private readonly struct AllocationChild
        {
            public AllocationChild(string id, int eligible) { Id = id; Eligible = eligible; }
            public string Id { get; }
            public int Eligible { get; }
        }

        private readonly struct AllocationShare
        {
            public AllocationShare(string id, int floor, long remainder) { Id = id; Floor = floor; Remainder = remainder; }
            public string Id { get; }
            public int Floor { get; }
            public long Remainder { get; }
        }

        private readonly struct SectorBudgetKey : IEquatable<SectorBudgetKey>
        {
            public SectorBudgetKey(BiomePatchId patchId, SectorCoord sector) { PatchId = patchId; Sector = sector; }
            public BiomePatchId PatchId { get; }
            public SectorCoord Sector { get; }
            public bool Equals(SectorBudgetKey other) => PatchId == other.PatchId && Sector == other.Sector;
            public override bool Equals(object obj) => obj is SectorBudgetKey other && Equals(other);
            public override int GetHashCode() => (PatchId.GetHashCode() * 397) ^ Sector.GetHashCode();
        }

        private sealed class PrioritizedOpportunity
        {
            public PrioritizedOpportunity(OpportunityGroup group, ulong priority, ulong drawBefore, ulong drawAfter)
            { Group = group; Priority = priority; DrawBefore = drawBefore; DrawAfter = drawAfter; }
            public OpportunityGroup Group { get; }
            public ulong Priority { get; }
            public ulong DrawBefore { get; }
            public ulong DrawAfter { get; }
            public static int Compare(PrioritizedOpportunity left, PrioritizedOpportunity right)
            {
                var comparison = left.Priority.CompareTo(right.Priority);
                return comparison != 0 ? comparison : string.Compare(left.Group.OpportunityId, right.Group.OpportunityId, StringComparison.Ordinal);
            }
        }
    }
}
