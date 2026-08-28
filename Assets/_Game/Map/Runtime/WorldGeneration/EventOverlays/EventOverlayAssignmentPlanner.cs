using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.WorldGeneration.EventOverlays
{
    public sealed class EventOverlayScopeBudget
    {
        internal EventOverlayScopeBudget(
            EventOverlayScopeKind scopeKind,
            string scopeId,
            int eligibleCount,
            int targetCount,
            int assignedCount,
            int emptyCount,
            int lowerCount,
            int upperCount,
            bool bandFeasible,
            bool discreteApproximation)
        {
            ScopeKind = scopeKind;
            ScopeId = scopeId ?? string.Empty;
            EligibleCount = eligibleCount;
            TargetCount = targetCount;
            AssignedCount = assignedCount;
            EmptyCount = emptyCount;
            LowerCount = lowerCount;
            UpperCount = upperCount;
            ExactRateNumerator = assignedCount;
            ExactRateDenominator = eligibleCount;
            AchievedPermilleNumerator = assignedCount * 1000L;
            AchievedPermilleDenominator = eligibleCount;
            BandFeasible = bandFeasible;
            DiscreteApproximation = discreteApproximation;
        }

        public EventOverlayScopeKind ScopeKind { get; }
        public string ScopeId { get; }
        public int EligibleCount { get; }
        public int TargetCount { get; }
        public int AssignedCount { get; }
        public int EmptyCount { get; }
        public int LowerCount { get; }
        public int UpperCount { get; }
        public int ExactRateNumerator { get; }
        public int ExactRateDenominator { get; }
        public long AchievedPermilleNumerator { get; }
        public int AchievedPermilleDenominator { get; }
        public bool BandFeasible { get; }
        public bool DiscreteApproximation { get; }
    }

    public sealed class EventOverlayAssignmentDecision
    {
        private readonly ReadOnlyCollection<string> cooldownExclusionEvidence;

        internal EventOverlayAssignmentDecision(
            EventOverlayAssignmentDecisionKind decisionKind,
            EventOverlayCandidate candidate,
            string rngScopeIdentity,
            ulong priority,
            ulong priorityDrawCountBefore,
            ulong priorityDrawCountAfter,
            int totalWeight,
            int weightedTicket,
            ulong weightedDrawCountBefore,
            ulong weightedDrawCountAfter,
            int previousProgressionOrdinal,
            int currentProgressionOrdinal,
            int requiredProgressionGap,
            int actualProgressionGap,
            IEnumerable<string> cooldownExclusionEvidence)
        {
            DecisionKind = decisionKind;
            Candidate = candidate;
            RngScopeIdentity = rngScopeIdentity ?? string.Empty;
            Priority = priority;
            PriorityDrawCountBefore = priorityDrawCountBefore;
            PriorityDrawCountAfter = priorityDrawCountAfter;
            TotalWeight = totalWeight;
            WeightedTicket = weightedTicket;
            WeightedDrawCountBefore = weightedDrawCountBefore;
            WeightedDrawCountAfter = weightedDrawCountAfter;
            PreviousProgressionOrdinal = previousProgressionOrdinal;
            CurrentProgressionOrdinal = currentProgressionOrdinal;
            RequiredProgressionGap = requiredProgressionGap;
            ActualProgressionGap = actualProgressionGap;
            var copy = cooldownExclusionEvidence == null ? Array.Empty<string>() :
                cooldownExclusionEvidence.OrderBy(value => value, StringComparer.Ordinal).ToArray();
            this.cooldownExclusionEvidence = new ReadOnlyCollection<string>(copy);
        }

        public EventOverlayAssignmentDecisionKind DecisionKind { get; }
        public EventOverlayCandidate Candidate { get; }
        public string OpportunityId => Candidate.OpportunityId;
        public EventOverlayId EventId => Candidate.EventId;
        public EventOverlayKind EventKind => Candidate.Kind;
        public string CandidateKey => Candidate.CandidateKey;
        public string RngScopeIdentity { get; }
        public ulong Priority { get; }
        public ulong PriorityDrawCountBefore { get; }
        public ulong PriorityDrawCountAfter { get; }
        public int TotalWeight { get; }
        public int WeightedTicket { get; }
        public ulong WeightedDrawCountBefore { get; }
        public ulong WeightedDrawCountAfter { get; }
        public int PreviousProgressionOrdinal { get; }
        public int CurrentProgressionOrdinal { get; }
        public int RequiredProgressionGap { get; }
        public int ActualProgressionGap { get; }
        public IReadOnlyList<string> CooldownExclusionEvidence => cooldownExclusionEvidence;
    }

    public sealed class EventOverlayAssignmentPlan
    {
        private readonly ReadOnlyCollection<EventOverlayScopeBudget> budgets;
        private readonly ReadOnlyCollection<EventOverlayAssignmentDecision> decisions;

        internal EventOverlayAssignmentPlan(
            EventOverlayAssignmentPolicy policy,
            ulong worldSeed,
            int attemptOrdinal,
            string candidateIndexDigest,
            string activityFrequencyPlanDigest,
            IEnumerable<EventOverlayScopeBudget> budgets,
            IEnumerable<EventOverlayAssignmentDecision> decisions,
            int rngStreamCreationCount,
            ulong rngDrawCount,
            string canonicalDigest)
        {
            Policy = policy;
            WorldSeed = worldSeed;
            AttemptOrdinal = attemptOrdinal;
            CandidateIndexDigest = candidateIndexDigest ?? string.Empty;
            ActivityFrequencyPlanDigest = activityFrequencyPlanDigest ?? string.Empty;
            this.budgets = new ReadOnlyCollection<EventOverlayScopeBudget>(
                (budgets ?? Array.Empty<EventOverlayScopeBudget>()).ToArray());
            this.decisions = new ReadOnlyCollection<EventOverlayAssignmentDecision>(
                (decisions ?? Array.Empty<EventOverlayAssignmentDecision>())
                .OrderBy(value => value.CurrentProgressionOrdinal).ThenBy(value => value.OpportunityId, StringComparer.Ordinal).ToArray());
            RngStreamId = WorldGenerationRngStreams.PopulationStreamId;
            RngResetScope = RngResetScope.Spawn;
            RngStreamCreationCount = rngStreamCreationCount;
            RngDrawCount = rngDrawCount;
            CanonicalDigest = canonicalDigest ?? string.Empty;
        }

        public EventOverlayAssignmentPolicy Policy { get; }
        public ulong WorldSeed { get; }
        public int AttemptOrdinal { get; }
        public string CandidateIndexDigest { get; }
        public string ActivityFrequencyPlanDigest { get; }
        public string RngStreamId { get; }
        public RngResetScope RngResetScope { get; }
        public IReadOnlyList<EventOverlayScopeBudget> Budgets => budgets;
        public IReadOnlyList<EventOverlayAssignmentDecision> Decisions => decisions;
        public EventOverlayScopeBudget WorldBudget => budgets.First(value => value.ScopeKind == EventOverlayScopeKind.World);
        public IReadOnlyList<EventOverlayScopeBudget> PatchBudgets => budgets.Where(value => value.ScopeKind == EventOverlayScopeKind.BiomePatch).ToArray();
        public IReadOnlyList<EventOverlayScopeBudget> SectorBudgets => budgets.Where(value => value.ScopeKind == EventOverlayScopeKind.Sector).ToArray();
        public int RngStreamCreationCount { get; }
        public ulong RngDrawCount { get; }
        public int GeometryWriteCount => 0;
        public int CollisionWriteCount => 0;
        public int RouteWriteCount => 0;
        public int AccessWriteCount => 0;
        public int PacingWriteCount => 0;
        public int EnvelopeWriteCount => 0;
        public int CanvasMutationCount => 0;
        public int PrefabMutationCount => 0;
        public int SceneMutationCount => 0;
        public int TilemapMutationCount => 0;
        public string CanonicalDigest { get; }
    }

    public sealed class EventOverlayAssignmentPlanRequest
    {
        public EventOverlayAssignmentPlanRequest(
            EventOverlayCandidateIndex candidateIndex,
            EventOverlayAssignmentPolicy policy,
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

        public EventOverlayCandidateIndex CandidateIndex { get; }
        public EventOverlayAssignmentPolicy Policy { get; }
        public ulong WorldSeed { get; }
        public int AttemptOrdinal { get; }
        public DeterministicRngStreamFactory RngStreamFactory { get; }
    }

    public sealed class EventOverlayAssignmentPlanResult
    {
        private readonly ReadOnlyCollection<EventOverlayAssignmentError> errors;

        internal EventOverlayAssignmentPlanResult(
            EventOverlayAssignmentPlan plan,
            IEnumerable<EventOverlayAssignmentError> errors,
            int rngStreamCreationCount,
            ulong rngDrawCount)
        {
            Plan = plan;
            this.errors = new ReadOnlyCollection<EventOverlayAssignmentError>(
                EventOverlayAssignmentCanonical.SortErrors(errors).ToArray());
            RngStreamCreationCount = rngStreamCreationCount;
            RngDrawCount = rngDrawCount;
        }

        public bool Success => Plan != null && errors.Count == 0;
        public EventOverlayAssignmentPlan Plan { get; }
        public IReadOnlyList<EventOverlayAssignmentError> Errors => errors;
        public int RngStreamCreationCount { get; }
        public ulong RngDrawCount { get; }
    }

    public static class EventOverlayAssignmentPlanner
    {
        private const string RulesetVersion = "MAP12_04_EVENT_ASSIGNMENT_V1";
        private const int MinimumPermille = 30;
        private const int MaximumPermille = 80;

        public static EventOverlayAssignmentPlanResult Plan(EventOverlayAssignmentPlanRequest request)
        {
            var errors = new List<EventOverlayAssignmentError>();
            ValidateRequest(request, errors);
            if (errors.Count != 0) return Failure(errors, 0, 0);

            var groups = request.CandidateIndex.Candidates
                .GroupBy(value => value.OpportunityId, StringComparer.Ordinal)
                .Select(group => new OpportunityGroup(group.Key, group.ToArray()))
                .OrderBy(value => value.Opportunity.ProgressionOrdinal)
                .ThenBy(value => value.OpportunityId, StringComparer.Ordinal).ToArray();
            if (groups.Any(group => group.EmptyCandidates.Length != 1 || group.NonEmptyCandidates.Length == 0))
            {
                Add(errors, EventOverlayAssignmentErrorCode.NonCanonicalPublication, "candidateIndex",
                    "Every opportunity must publish exactly one Empty and at least one non-empty candidate.");
                return Failure(errors, 0, 0);
            }
            foreach (var group in groups)
            {
                long total = 0;
                try { checked { foreach (var value in group.NonEmptyCandidates) total += value.Weight; } }
                catch (OverflowException)
                {
                    Add(errors, EventOverlayAssignmentErrorCode.InvalidProfile,
                        "candidateWeights/" + group.OpportunityId, "Weight sum overflowed.");
                }
                if (total <= 0 || total > int.MaxValue)
                    Add(errors, EventOverlayAssignmentErrorCode.InvalidProfile,
                        "candidateWeights/" + group.OpportunityId, "Positive weight sum must fit Int32.");
            }
            if (errors.Count != 0) return Failure(errors, 0, 0);

            var worldTarget = TargetFor(groups.Length, request.Policy.TargetPermille);
            var patches = groups.GroupBy(value => value.Opportunity.PatchId).OrderBy(value => value.Key).ToArray();
            var patchAllocation = Allocate(worldTarget, groups.Length,
                patches.Select(value => new AllocationChild(value.Key.Value, value.Count())).ToArray());
            var sectorAllocation = new Dictionary<SectorBudgetKey, int>();
            foreach (var patch in patches)
            {
                var sectors = patch.GroupBy(value => value.Opportunity.Sector)
                    .OrderBy(value => value.Key.Y).ThenBy(value => value.Key.X).ToArray();
                var allocation = Allocate(patchAllocation[patch.Key.Value], patch.Count(),
                    sectors.Select(value => new AllocationChild(SectorId(value.Key), value.Count())).ToArray());
                foreach (var sector in sectors)
                    sectorAllocation[new SectorBudgetKey(patch.Key, sector.Key)] = allocation[SectorId(sector.Key)];
            }
            if (patchAllocation.Values.Sum() != worldTarget || sectorAllocation.Values.Sum() != worldTarget)
            {
                Add(errors, EventOverlayAssignmentErrorCode.BudgetMismatch, "budgets", "World, patch, and sector allocations must sum exactly.");
                return Failure(errors, 0, 0);
            }

            var priorityByOpportunity = new Dictionary<string, PrioritizedOpportunity>(StringComparer.Ordinal);
            var streamCount = 0;
            ulong drawCount = 0;
            foreach (var group in groups.OrderBy(value => value.Opportunity.PatchId)
                         .ThenBy(value => value.Opportunity.Sector.Y).ThenBy(value => value.Opportunity.Sector.X)
                         .ThenBy(value => value.OpportunityId, StringComparer.Ordinal))
            {
                var scopeIdentity = ScopeIdentity(group.Opportunity);
                DeterministicRngStream stream;
                try
                {
                    stream = request.RngStreamFactory.Create(WorldGenerationRngStreams.PopulationStreamId,
                        request.WorldSeed, RngStreamScope.Spawn(scopeIdentity, request.AttemptOrdinal));
                }
                catch (Exception exception)
                {
                    Add(errors, EventOverlayAssignmentErrorCode.InvalidRngBinding,
                        "rng/" + group.OpportunityId, exception.GetType().Name + ":" + exception.Message);
                    return Failure(errors, streamCount, drawCount);
                }
                streamCount++;
                var before = stream.DrawCount;
                var priority = stream.NextUInt64();
                priorityByOpportunity.Add(group.OpportunityId,
                    new PrioritizedOpportunity(group, stream, scopeIdentity, priority, before, stream.DrawCount));
            }

            var selected = new HashSet<string>(StringComparer.Ordinal);
            foreach (var patch in patches)
            foreach (var sector in patch.GroupBy(value => value.Opportunity.Sector)
                         .OrderBy(value => value.Key.Y).ThenBy(value => value.Key.X))
            {
                var quota = sectorAllocation[new SectorBudgetKey(patch.Key, sector.Key)];
                foreach (var value in sector.Select(group => priorityByOpportunity[group.OpportunityId])
                             .OrderBy(value => value.Priority).ThenBy(value => value.Group.OpportunityId, StringComparer.Ordinal)
                             .Take(quota))
                    selected.Add(value.Group.OpportunityId);
            }

            var decisions = new List<EventOverlayAssignmentDecision>();
            var lastOrdinalByEvent = new Dictionary<EventOverlayId, int>();
            foreach (var group in groups)
            {
                var prioritized = priorityByOpportunity[group.OpportunityId];
                if (!selected.Contains(group.OpportunityId))
                {
                    decisions.Add(new EventOverlayAssignmentDecision(
                        EventOverlayAssignmentDecisionKind.Empty, group.EmptyCandidates[0], prioritized.ScopeIdentity,
                        prioritized.Priority, prioritized.PriorityBefore, prioritized.PriorityAfter,
                        0, -1, prioritized.Stream.DrawCount, prioritized.Stream.DrawCount,
                        -1, group.Opportunity.ProgressionOrdinal, 0, -1, Array.Empty<string>()));
                    continue;
                }

                var exclusions = new List<string>();
                var allowed = new List<EventOverlayCandidate>();
                foreach (var candidate in group.NonEmptyCandidates)
                {
                    if (!lastOrdinalByEvent.TryGetValue(candidate.EventId, out var previous))
                    {
                        allowed.Add(candidate);
                        continue;
                    }
                    var actual = group.Opportunity.ProgressionOrdinal - previous;
                    if (actual >= candidate.MinimumProgressionGap)
                        allowed.Add(candidate);
                    else
                        exclusions.Add(candidate.EventId.Value + "@" + Number(previous) + ":" +
                            Number(candidate.MinimumProgressionGap) + ":" + Number(actual));
                }
                if (allowed.Count == 0)
                {
                    Add(errors, EventOverlayAssignmentErrorCode.CooldownMakesTargetUnsatisfiable,
                        "opportunities/" + group.OpportunityId,
                        "All non-empty candidates were excluded: " + string.Join(",", exclusions.OrderBy(value => value, StringComparer.Ordinal)));
                    drawCount = priorityByOpportunity.Values.Aggregate<PrioritizedOpportunity, ulong>(0,
                        (current, value) => current + value.Stream.DrawCount);
                    return Failure(errors, streamCount, drawCount);
                }
                allowed = allowed.OrderBy(value => value.EventId.Value, StringComparer.Ordinal)
                    .ThenBy(value => value.CandidateKey, StringComparer.Ordinal).ToList();
                var totalWeight = allowed.Sum(value => value.Weight);
                var weightedBefore = prioritized.Stream.DrawCount;
                var ticket = prioritized.Stream.NextInt(totalWeight);
                var remaining = ticket;
                var chosen = allowed[allowed.Count - 1];
                foreach (var candidate in allowed)
                {
                    if (remaining < candidate.Weight) { chosen = candidate; break; }
                    remaining -= candidate.Weight;
                }
                var previousOrdinal = lastOrdinalByEvent.TryGetValue(chosen.EventId, out var previousValue) ? previousValue : -1;
                var actualGap = previousOrdinal < 0 ? -1 : group.Opportunity.ProgressionOrdinal - previousOrdinal;
                lastOrdinalByEvent[chosen.EventId] = group.Opportunity.ProgressionOrdinal;
                decisions.Add(new EventOverlayAssignmentDecision(
                    EventOverlayAssignmentDecisionKind.Assigned, chosen, prioritized.ScopeIdentity,
                    prioritized.Priority, prioritized.PriorityBefore, prioritized.PriorityAfter,
                    totalWeight, ticket, weightedBefore, prioritized.Stream.DrawCount,
                    previousOrdinal, group.Opportunity.ProgressionOrdinal,
                    chosen.MinimumProgressionGap, actualGap, exclusions));
            }
            drawCount = priorityByOpportunity.Values.Aggregate<PrioritizedOpportunity, ulong>(0,
                (current, value) => current + value.Stream.DrawCount);

            var budgets = BuildBudgets(groups, patches, patchAllocation, sectorAllocation, decisions, worldTarget);
            if (decisions.Count != groups.Length || decisions.Count(value => value.DecisionKind == EventOverlayAssignmentDecisionKind.Assigned) != worldTarget ||
                budgets.First().AssignedCount != worldTarget ||
                budgets.Where(value => value.ScopeKind == EventOverlayScopeKind.BiomePatch).Sum(value => value.AssignedCount) != worldTarget ||
                budgets.Where(value => value.ScopeKind == EventOverlayScopeKind.Sector).Sum(value => value.AssignedCount) != worldTarget)
            {
                Add(errors, EventOverlayAssignmentErrorCode.BudgetMismatch, "publication",
                    "Published decisions and hierarchical budgets must sum exactly.");
                return Failure(errors, streamCount, drawCount);
            }

            var digest = ComputeDigest(request, budgets, decisions, streamCount, drawCount);
            var plan = new EventOverlayAssignmentPlan(request.Policy, request.WorldSeed, request.AttemptOrdinal,
                request.CandidateIndex.CanonicalDigest, request.CandidateIndex.ActivityFrequencyPlanDigest,
                budgets, decisions, streamCount, drawCount, digest);
            return new EventOverlayAssignmentPlanResult(plan, Array.Empty<EventOverlayAssignmentError>(), streamCount, drawCount);
        }

        private static void ValidateRequest(
            EventOverlayAssignmentPlanRequest request,
            ICollection<EventOverlayAssignmentError> errors)
        {
            if (request == null)
            {
                Add(errors, EventOverlayAssignmentErrorCode.MissingInput, "request", "Plan request is required.");
                return;
            }
            if (request.CandidateIndex == null || request.CandidateIndex.CandidateCount == 0 ||
                !EventOverlayAssignmentCanonical.IsDigest(request.CandidateIndex == null ? string.Empty : request.CandidateIndex.CanonicalDigest))
                Add(errors, EventOverlayAssignmentErrorCode.MissingInput, "candidateIndex", "A published candidate index is required.");
            if (request.Policy == null)
                Add(errors, EventOverlayAssignmentErrorCode.MissingInput, "policy", "Frequency policy is required.");
            else if (request.Policy.TargetPermille < MinimumPermille || request.Policy.TargetPermille > MaximumPermille)
                Add(errors, EventOverlayAssignmentErrorCode.InvalidFrequencyPolicy, "policy.targetPermille", "TargetPermille must be 30..80 inclusive.");
            if (request.AttemptOrdinal < 0)
                Add(errors, EventOverlayAssignmentErrorCode.InvalidRngBinding, "attemptOrdinal", "Attempt ordinal must be non-negative.");
            if (request.RngStreamFactory == null)
                Add(errors, EventOverlayAssignmentErrorCode.MissingInput, "rngStreamFactory", "DeterministicRngStreamFactory is required.");
            else
            {
                try
                {
                    var definition = request.RngStreamFactory.GetDefinition(WorldGenerationRngStreams.PopulationStreamId);
                    var scope = DeterministicRngSeedDeriver.ValidateDefinition(definition);
                    if (!definition.Active || scope != RngResetScope.Spawn)
                        Add(errors, EventOverlayAssignmentErrorCode.InvalidRngBinding,
                            "rngStreamFactory.RNG_POPULATION", "The required stream must be active with SPAWN reset scope.");
                }
                catch (Exception exception)
                {
                    Add(errors, EventOverlayAssignmentErrorCode.InvalidRngBinding,
                        "rngStreamFactory.RNG_POPULATION", exception.GetType().Name + ":" + exception.Message);
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

        private static int LowerCount(int eligible) => (eligible * MinimumPermille + 999) / 1000;
        private static int UpperCount(int eligible) => eligible * MaximumPermille / 1000;

        private static Dictionary<string, int> Allocate(int parentTarget, int parentEligible, IReadOnlyList<AllocationChild> children)
        {
            var result = children.ToDictionary(value => value.Id, value => 0, StringComparer.Ordinal);
            if (parentTarget == 0 || parentEligible == 0) return result;
            var shares = children.Select(value => new AllocationShare(value.Id,
                (int)((long)parentTarget * value.Eligible / parentEligible),
                (long)parentTarget * value.Eligible % parentEligible)).ToArray();
            foreach (var share in shares) result[share.Id] = share.Floor;
            var remainder = parentTarget - shares.Sum(value => value.Floor);
            foreach (var share in shares.OrderByDescending(value => value.Remainder)
                         .ThenBy(value => value.Id, StringComparer.Ordinal).Take(remainder))
                result[share.Id]++;
            return result;
        }

        private static IReadOnlyList<EventOverlayScopeBudget> BuildBudgets(
            IReadOnlyList<OpportunityGroup> groups,
            IEnumerable<IGrouping<BiomePatchId, OpportunityGroup>> patches,
            IReadOnlyDictionary<string, int> patchAllocation,
            IReadOnlyDictionary<SectorBudgetKey, int> sectorAllocation,
            IReadOnlyList<EventOverlayAssignmentDecision> decisions,
            int worldTarget)
        {
            var result = new List<EventOverlayScopeBudget>
            {
                Budget(EventOverlayScopeKind.World, "WORLD", groups.Count, worldTarget, decisions)
            };
            foreach (var patch in patches)
            {
                var patchDecisions = decisions.Where(value => value.Candidate.Opportunity.PatchId == patch.Key).ToArray();
                result.Add(Budget(EventOverlayScopeKind.BiomePatch, patch.Key.Value, patch.Count(),
                    patchAllocation[patch.Key.Value], patchDecisions));
                foreach (var sector in patch.GroupBy(value => value.Opportunity.Sector)
                             .OrderBy(value => value.Key.Y).ThenBy(value => value.Key.X))
                {
                    var sectorDecisions = patchDecisions.Where(value => value.Candidate.Opportunity.Sector == sector.Key).ToArray();
                    result.Add(Budget(EventOverlayScopeKind.Sector, SectorId(sector.Key), sector.Count(),
                        sectorAllocation[new SectorBudgetKey(patch.Key, sector.Key)], sectorDecisions));
                }
            }
            return result;
        }

        private static EventOverlayScopeBudget Budget(EventOverlayScopeKind kind, string id, int eligible, int target,
            IEnumerable<EventOverlayAssignmentDecision> decisions)
        {
            var copy = decisions.ToArray();
            var assigned = copy.Count(value => value.DecisionKind == EventOverlayAssignmentDecisionKind.Assigned);
            var lower = LowerCount(eligible);
            var upper = UpperCount(eligible);
            var feasible = lower <= upper;
            return new EventOverlayScopeBudget(kind, id, eligible, target, assigned, copy.Length - assigned,
                lower, upper, feasible, !feasible || target < lower || target > upper);
        }

        private static string ComputeDigest(
            EventOverlayAssignmentPlanRequest request,
            IEnumerable<EventOverlayScopeBudget> budgets,
            IEnumerable<EventOverlayAssignmentDecision> decisions,
            int streamCount,
            ulong drawCount)
        {
            var material = new StringBuilder();
            EventOverlayAssignmentCanonical.Append(material, "RULESET", RulesetVersion);
            EventOverlayAssignmentCanonical.Append(material, "INDEX", request.CandidateIndex.CanonicalDigest,
                request.CandidateIndex.ActivityFrequencyPlanDigest);
            EventOverlayAssignmentCanonical.Append(material, "POLICY", Number(request.Policy.TargetPermille));
            EventOverlayAssignmentCanonical.Append(material, "RNG", WorldGenerationRngStreams.PopulationStreamId,
                RngResetScopeToken.Format(RngResetScope.Spawn), request.WorldSeed.ToString(CultureInfo.InvariantCulture),
                Number(request.AttemptOrdinal), Number(streamCount), drawCount.ToString(CultureInfo.InvariantCulture));
            foreach (var budget in budgets)
                EventOverlayAssignmentCanonical.Append(material, "BUDGET", Number((int)budget.ScopeKind), budget.ScopeId,
                    Number(budget.EligibleCount), Number(budget.TargetCount), Number(budget.AssignedCount),
                    Number(budget.EmptyCount), Number(budget.LowerCount), Number(budget.UpperCount),
                    budget.BandFeasible ? "1" : "0", budget.DiscreteApproximation ? "1" : "0");
            foreach (var decision in decisions.OrderBy(value => value.CurrentProgressionOrdinal)
                         .ThenBy(value => value.OpportunityId, StringComparer.Ordinal))
                EventOverlayAssignmentCanonical.Append(material, "DECISION", Number((int)decision.DecisionKind),
                    decision.OpportunityId, decision.EventId.Value, Number((int)decision.EventKind), decision.CandidateKey,
                    decision.RngScopeIdentity, decision.Priority.ToString(CultureInfo.InvariantCulture),
                    decision.PriorityDrawCountBefore.ToString(CultureInfo.InvariantCulture),
                    decision.PriorityDrawCountAfter.ToString(CultureInfo.InvariantCulture), Number(decision.TotalWeight),
                    Number(decision.WeightedTicket), decision.WeightedDrawCountBefore.ToString(CultureInfo.InvariantCulture),
                    decision.WeightedDrawCountAfter.ToString(CultureInfo.InvariantCulture),
                    Number(decision.PreviousProgressionOrdinal), Number(decision.CurrentProgressionOrdinal),
                    Number(decision.RequiredProgressionGap), Number(decision.ActualProgressionGap),
                    string.Join(",", decision.CooldownExclusionEvidence));
            return EventOverlayAssignmentCanonical.Sha256(material.ToString());
        }

        private static string ScopeIdentity(EventOverlayOpportunity opportunity)
            => "EVENT|" + Number(opportunity.Sector.X) + "," + Number(opportunity.Sector.Y) + "|" + opportunity.OpportunityId;

        private static string SectorId(SectorCoord value)
            => value.X.ToString("D2", CultureInfo.InvariantCulture) + "," + value.Y.ToString("D2", CultureInfo.InvariantCulture);

        private static EventOverlayAssignmentPlanResult Failure(
            IEnumerable<EventOverlayAssignmentError> errors, int streams, ulong draws)
            => new EventOverlayAssignmentPlanResult(null, errors, streams, draws);

        private static void Add(ICollection<EventOverlayAssignmentError> errors,
            EventOverlayAssignmentErrorCode code, string path, string detail)
            => errors.Add(new EventOverlayAssignmentError(code, path, detail));

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private sealed class OpportunityGroup
        {
            public OpportunityGroup(string opportunityId, EventOverlayCandidate[] candidates)
            {
                OpportunityId = opportunityId;
                Candidates = candidates.OrderBy(value => value.EventId.Value, StringComparer.Ordinal)
                    .ThenBy(value => value.CandidateKey, StringComparer.Ordinal).ToArray();
                Opportunity = Candidates[0].Opportunity;
                EmptyCandidates = Candidates.Where(value => value.IsEmpty).ToArray();
                NonEmptyCandidates = Candidates.Where(value => !value.IsEmpty).ToArray();
            }
            public string OpportunityId { get; }
            public EventOverlayOpportunity Opportunity { get; }
            public EventOverlayCandidate[] Candidates { get; }
            public EventOverlayCandidate[] EmptyCandidates { get; }
            public EventOverlayCandidate[] NonEmptyCandidates { get; }
        }

        private sealed class PrioritizedOpportunity
        {
            public PrioritizedOpportunity(OpportunityGroup group, DeterministicRngStream stream,
                string scopeIdentity, ulong priority, ulong before, ulong after)
            {
                Group = group; Stream = stream; ScopeIdentity = scopeIdentity;
                Priority = priority; PriorityBefore = before; PriorityAfter = after;
            }
            public OpportunityGroup Group { get; }
            public DeterministicRngStream Stream { get; }
            public string ScopeIdentity { get; }
            public ulong Priority { get; }
            public ulong PriorityBefore { get; }
            public ulong PriorityAfter { get; }
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
            public override int GetHashCode() { unchecked { return (PatchId.GetHashCode() * 397) ^ Sector.GetHashCode(); } }
        }
    }
}
