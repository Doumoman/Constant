using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Population;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.WorldGeneration.Validation
{
    public enum GeneratedForgeValidationState
    {
        NotPresent = 0,
        NotReached = 1,
        Available = 2,
        Activated = 3,
    }

    public enum GeneratedSealValidationState
    {
        NotPresent = 0,
        NotReached = 1,
        Opened = 2,
        Accepted = 3,
    }

    public enum GeneratedBossValidationState
    {
        NotPresent = 0,
        NotReached = 1,
        Available = 2,
        Defeated = 3,
    }

    public enum GeneratedSpecialValidationState
    {
        NotPresent = 0,
        None = 1,
        Entered = 2,
        Resolved = 3,
        Exited = 4,
    }

    public enum GeneratedCompletionTransitionKind
    {
        CollectMandatoryResource = 1,
        MakeForgeAvailable = 2,
        ActivateForge = 3,
        OpenSeal = 4,
        AcceptSeal = 5,
        MakeBossAvailable = 6,
        DefeatBoss = 7,
        EnterSpecial = 8,
        ResolveSpecial = 9,
        ExitSpecial = 10,
    }

    public enum GeneratedCompletionBindingSourceKind
    {
        MandatoryPopulationSlot = 1,
        SpecialRegionState = 2,
    }

    public sealed class GeneratedCompletionStateKey :
        IEquatable<GeneratedCompletionStateKey>, IComparable<GeneratedCompletionStateKey>
    {
        public GeneratedCompletionStateKey(
            string positionNodeId,
            ulong resourceMask,
            GeneratedForgeValidationState forge,
            GeneratedSealValidationState seal,
            GeneratedBossValidationState boss,
            GeneratedSpecialValidationState specialState)
        {
            PositionNodeId = Normalize(positionNodeId);
            ResourceMask = resourceMask;
            Forge = forge;
            Seal = seal;
            Boss = boss;
            SpecialState = specialState;
            StableToken = string.Join("|", new[]
            {
                "COMPLETION_STATE_V1", PositionNodeId, Number(ResourceMask),
                Number((int)Forge), Number((int)Seal), Number((int)Boss),
                Number((int)SpecialState),
            });
        }

        public string PositionNodeId { get; }
        public ulong ResourceMask { get; }
        public GeneratedForgeValidationState Forge { get; }
        public GeneratedSealValidationState Seal { get; }
        public GeneratedBossValidationState Boss { get; }
        public GeneratedSpecialValidationState SpecialState { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedCompletionStateKey other) => other == null
            ? -1 : string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
        public bool Equals(GeneratedCompletionStateKey other) => other != null &&
            string.Equals(StableToken, other.StableToken, StringComparison.Ordinal);
        public override bool Equals(object obj) => Equals(obj as GeneratedCompletionStateKey);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(StableToken);
        public override string ToString() => StableToken;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? "MISSING" : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Number(ulong value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedCompletionStateTransition :
        IComparable<GeneratedCompletionStateTransition>
    {
        public GeneratedCompletionStateTransition(
            string nodeId,
            GeneratedCompletionTransitionKind kind,
            GeneratedCompletionBindingSourceKind sourceKind,
            string sourceKey,
            string sourceDigest,
            ulong resourceBit = 0,
            string declaredTransitionId = null)
        {
            NodeId = Normalize(nodeId);
            Kind = kind;
            SourceKind = sourceKind;
            SourceKey = Normalize(sourceKey);
            SourceDigest = Normalize(sourceDigest);
            ResourceBit = resourceBit;
            TransitionId = declaredTransitionId ?? BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_03_COMPLETION_STATE_TRANSITION_ID_V1", NodeId, Number((int)Kind),
                Number((int)SourceKind), SourceKey, SourceDigest, Number(ResourceBit),
            });
        }

        public string TransitionId { get; }
        public string NodeId { get; }
        public GeneratedCompletionTransitionKind Kind { get; }
        public GeneratedCompletionBindingSourceKind SourceKind { get; }
        public string SourceKey { get; }
        public string SourceDigest { get; }
        public ulong ResourceBit { get; }
        public string StableToken => string.Join("|", new[]
        {
            "STATE_TRANSITION", TransitionId, NodeId, Number((int)Kind),
            Number((int)SourceKind), SourceKey, SourceDigest, Number(ResourceBit),
        });
        public int CompareTo(GeneratedCompletionStateTransition other) => other == null
            ? -1 : string.Compare(TransitionId, other.TransitionId, StringComparison.Ordinal);

        public static GeneratedCompletionStateTransition FromMandatoryPopulation(
            string nodeId,
            GeneratedMandatoryContentKey content,
            ulong resourceBit)
        {
            var token = content == null ? "MISSING" : content.StableToken;
            return new GeneratedCompletionStateTransition(nodeId,
                GeneratedCompletionTransitionKind.CollectMandatoryResource,
                GeneratedCompletionBindingSourceKind.MandatoryPopulationSlot,
                token, BakingCanonicalDigest.HashCanonicalLines(new[]
                    { "MAP18_MANDATORY_POPULATION_BINDING_V1", token }), resourceBit);
        }

        public static GeneratedCompletionStateTransition FromDeclaredSpecialState(
            string nodeId,
            GeneratedDeclaredSpecialStateSource source,
            GeneratedCompletionTransitionKind kind) =>
            new GeneratedCompletionStateTransition(nodeId, kind,
                GeneratedCompletionBindingSourceKind.SpecialRegionState,
                source == null ? "MISSING" : source.StableToken,
                source == null ? "MISSING" : source.SourceDigest);

        internal bool TryApply(
            GeneratedCompletionStateKey state,
            out GeneratedCompletionStateKey next)
        {
            next = null;
            if (state == null || !string.Equals(state.PositionNodeId, NodeId,
                    StringComparison.Ordinal))
                return false;
            switch (Kind)
            {
                case GeneratedCompletionTransitionKind.CollectMandatoryResource:
                    if (ResourceBit == 0 || (state.ResourceMask & ResourceBit) != 0) return false;
                    next = New(state, resources: state.ResourceMask | ResourceBit);
                    return true;
                case GeneratedCompletionTransitionKind.MakeForgeAvailable:
                    if (state.Forge != GeneratedForgeValidationState.NotReached) return false;
                    next = New(state, forge: GeneratedForgeValidationState.Available);
                    return true;
                case GeneratedCompletionTransitionKind.ActivateForge:
                    if (state.Forge != GeneratedForgeValidationState.Available) return false;
                    next = New(state, forge: GeneratedForgeValidationState.Activated);
                    return true;
                case GeneratedCompletionTransitionKind.OpenSeal:
                    if (state.Seal != GeneratedSealValidationState.NotReached ||
                        state.Forge != GeneratedForgeValidationState.Activated) return false;
                    next = New(state, seal: GeneratedSealValidationState.Opened);
                    return true;
                case GeneratedCompletionTransitionKind.AcceptSeal:
                    if (state.Seal != GeneratedSealValidationState.Opened) return false;
                    next = New(state, seal: GeneratedSealValidationState.Accepted);
                    return true;
                case GeneratedCompletionTransitionKind.MakeBossAvailable:
                    if (state.Boss != GeneratedBossValidationState.NotReached ||
                        state.Seal != GeneratedSealValidationState.Accepted) return false;
                    next = New(state, boss: GeneratedBossValidationState.Available);
                    return true;
                case GeneratedCompletionTransitionKind.DefeatBoss:
                    if (state.Boss != GeneratedBossValidationState.Available) return false;
                    next = New(state, boss: GeneratedBossValidationState.Defeated);
                    return true;
                case GeneratedCompletionTransitionKind.EnterSpecial:
                    if (state.SpecialState != GeneratedSpecialValidationState.None) return false;
                    next = New(state, special: GeneratedSpecialValidationState.Entered);
                    return true;
                case GeneratedCompletionTransitionKind.ResolveSpecial:
                    if (state.SpecialState != GeneratedSpecialValidationState.Entered) return false;
                    next = New(state, special: GeneratedSpecialValidationState.Resolved);
                    return true;
                case GeneratedCompletionTransitionKind.ExitSpecial:
                    if (state.SpecialState != GeneratedSpecialValidationState.Resolved) return false;
                    next = New(state, special: GeneratedSpecialValidationState.Exited);
                    return true;
                default:
                    return false;
            }
        }

        private static GeneratedCompletionStateKey New(
            GeneratedCompletionStateKey source,
            ulong? resources = null,
            GeneratedForgeValidationState? forge = null,
            GeneratedSealValidationState? seal = null,
            GeneratedBossValidationState? boss = null,
            GeneratedSpecialValidationState? special = null) =>
            new GeneratedCompletionStateKey(source.PositionNodeId,
                resources ?? source.ResourceMask, forge ?? source.Forge,
                seal ?? source.Seal, boss ?? source.Boss, special ?? source.SpecialState);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? "MISSING" : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Number(ulong value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedCompletionGoal
    {
        public GeneratedCompletionGoal(
            string exitNodeId,
            ulong requiredResourceMask,
            GeneratedForgeValidationState forge,
            GeneratedSealValidationState seal,
            GeneratedBossValidationState boss,
            GeneratedSpecialValidationState specialState)
        {
            ExitNodeId = Normalize(exitNodeId);
            RequiredResourceMask = requiredResourceMask;
            Forge = forge;
            Seal = seal;
            Boss = boss;
            SpecialState = specialState;
        }

        public string ExitNodeId { get; }
        public ulong RequiredResourceMask { get; }
        public GeneratedForgeValidationState Forge { get; }
        public GeneratedSealValidationState Seal { get; }
        public GeneratedBossValidationState Boss { get; }
        public GeneratedSpecialValidationState SpecialState { get; }
        public string StableToken => string.Join("|", new[]
        {
            "COMPLETION_GOAL_V1", ExitNodeId, Number(RequiredResourceMask), Number((int)Forge),
            Number((int)Seal), Number((int)Boss), Number((int)SpecialState),
        });

        public bool IsSatisfied(GeneratedCompletionStateKey state) => state != null &&
            string.Equals(state.PositionNodeId, ExitNodeId, StringComparison.Ordinal) &&
            (state.ResourceMask & RequiredResourceMask) == RequiredResourceMask &&
            state.Forge == Forge && state.Seal == Seal && state.Boss == Boss &&
            state.SpecialState == SpecialState;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? "MISSING" : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Number(ulong value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedCompletionActionAudit
    {
        public GeneratedCompletionActionAudit(
            int implicitActionAttempts = 0,
            int externalStateMutationAttempts = 0,
            int terrainMutationAttempts = 0,
            int seedBatchAttempts = 0,
            bool map19_04Started = false)
        {
            ImplicitActionAttempts = implicitActionAttempts;
            ExternalStateMutationAttempts = externalStateMutationAttempts;
            TerrainMutationAttempts = terrainMutationAttempts;
            SeedBatchAttempts = seedBatchAttempts;
            Map19_04Started = map19_04Started;
        }

        public int ImplicitActionAttempts { get; }
        public int ExternalStateMutationAttempts { get; }
        public int TerrainMutationAttempts { get; }
        public int SeedBatchAttempts { get; }
        public bool Map19_04Started { get; }
        public bool IsZero => ImplicitActionAttempts == 0 && ExternalStateMutationAttempts == 0 &&
            TerrainMutationAttempts == 0 && SeedBatchAttempts == 0 && !Map19_04Started;
    }

    public sealed class GeneratedCompletionSearchInput
    {
        private readonly ReadOnlyCollection<GeneratedCompletionStateTransition> transitions;
        private readonly ReadOnlyCollection<TraversalMovementKind> allowedMovementKinds;

        public GeneratedCompletionSearchInput(
            GeneratedTileMovementGraph graph,
            NakedTraversalSearchResult nakedSearch,
            IEnumerable<GeneratedCompletionStateTransition> sourceTransitions,
            GeneratedCompletionGoal goal,
            IEnumerable<TraversalMovementKind> sourceAllowedMovementKinds,
            GeneratedCompletionActionAudit actionAudit = null)
        {
            Graph = graph;
            NakedSearch = nakedSearch;
            transitions = new ReadOnlyCollection<GeneratedCompletionStateTransition>((
                    sourceTransitions ?? Array.Empty<GeneratedCompletionStateTransition>())
                .Where(value => value != null).OrderBy(value => value).ToArray());
            Goal = goal;
            allowedMovementKinds = new ReadOnlyCollection<TraversalMovementKind>((
                    sourceAllowedMovementKinds ?? Array.Empty<TraversalMovementKind>())
                .OrderBy(value => (int)value).ToArray());
            ActionAudit = actionAudit ?? new GeneratedCompletionActionAudit();
        }

        public GeneratedTileMovementGraph Graph { get; }
        public NakedTraversalSearchResult NakedSearch { get; }
        public IReadOnlyList<GeneratedCompletionStateTransition> Transitions => transitions;
        public GeneratedCompletionGoal Goal { get; }
        public IReadOnlyList<TraversalMovementKind> AllowedMovementKinds => allowedMovementKinds;
        public GeneratedCompletionActionAudit ActionAudit { get; }

        public string ComputeDigest() => BakingCanonicalDigest.HashCanonicalLines(new[]
        {
            "MAP19_03_COMPLETION_SEARCH_INPUT_V1",
            Graph == null ? "MISSING_GRAPH" : Graph.GraphDigest,
            NakedSearch == null ? "MISSING_NAKED" : NakedSearch.SuccessProofDigest,
            Goal == null ? "MISSING_GOAL" : Goal.StableToken,
            "MOVEMENTS=" + string.Join(",", allowedMovementKinds.Select(value =>
                ((int)value).ToString(CultureInfo.InvariantCulture))),
        }.Concat(transitions.Select(value => value.StableToken)));
    }

    public sealed class GeneratedCompletionFrontierEvidence :
        IComparable<GeneratedCompletionFrontierEvidence>
    {
        public GeneratedCompletionFrontierEvidence(
            GeneratedCompletionStateKey state, int transitionDepth, string blockingReason)
        {
            State = state;
            TransitionDepth = transitionDepth;
            BlockingReason = string.IsNullOrWhiteSpace(blockingReason)
                ? "MISSING" : blockingReason.Trim();
        }
        public GeneratedCompletionStateKey State { get; }
        public int TransitionDepth { get; }
        public string BlockingReason { get; }
        public string StableToken => "COMPLETION_FRONTIER|" +
            (State == null ? "MISSING" : State.StableToken) + "|" +
            TransitionDepth.ToString(CultureInfo.InvariantCulture) + "|" + BlockingReason;
        public int CompareTo(GeneratedCompletionFrontierEvidence other) => other == null
            ? -1 : string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
    }

    public sealed class GeneratedCompletionSearchFailure :
        IComparable<GeneratedCompletionSearchFailure>
    {
        private readonly ReadOnlyCollection<GeneratedCompletionFrontierEvidence> frontier;

        public GeneratedCompletionSearchFailure(
            string owner,
            string reason,
            string offendingKey,
            string expected,
            string actual,
            string sourceDigest,
            IEnumerable<GeneratedCompletionFrontierEvidence> sourceFrontier = null)
        {
            Owner = Normalize(owner);
            Reason = Normalize(reason);
            OffendingKey = Normalize(offendingKey);
            Expected = Normalize(expected);
            Actual = Normalize(actual);
            SourceDigest = Normalize(sourceDigest);
            frontier = new ReadOnlyCollection<GeneratedCompletionFrontierEvidence>((sourceFrontier ??
                    Array.Empty<GeneratedCompletionFrontierEvidence>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
        }
        public string Owner { get; }
        public string Reason { get; }
        public string OffendingKey { get; }
        public string Expected { get; }
        public string Actual { get; }
        public string SourceDigest { get; }
        public IReadOnlyList<GeneratedCompletionFrontierEvidence> Frontier => frontier;
        public string StableToken => string.Join("|", new[]
        {
            "COMPLETION_FAILURE", Owner, Reason, OffendingKey, Expected, Actual, SourceDigest,
            string.Join(",", frontier.Select(value => value.StableToken)),
        });
        public int CompareTo(GeneratedCompletionSearchFailure other) => other == null
            ? -1 : string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
        public override string ToString() => StableToken;
        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? "MISSING" : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
    }

    public sealed class GeneratedCompletionSearchProof
    {
        private readonly ReadOnlyCollection<string> shortestActions;

        internal GeneratedCompletionSearchProof(
            GeneratedCompletionStateKey initialState,
            GeneratedCompletionStateKey finalState,
            int statesVisited,
            int transitionsEvaluated,
            int moveTransitions,
            int stateTransitions,
            int shortestTransitionCount,
            IEnumerable<string> sourceShortestActions,
            string inputDigest,
            string stateSpaceDigest,
            string nakedProofDigest,
            string graphDigest)
        {
            InitialState = initialState;
            FinalState = finalState;
            StatesVisited = statesVisited;
            TransitionsEvaluated = transitionsEvaluated;
            MoveTransitions = moveTransitions;
            StateTransitions = stateTransitions;
            ShortestTransitionCount = shortestTransitionCount;
            shortestActions = new ReadOnlyCollection<string>((sourceShortestActions ??
                Array.Empty<string>()).ToArray());
            InputDigest = inputDigest;
            StateSpaceDigest = stateSpaceDigest;
            ProofDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_03_COMPLETION_PROOF_V1", initialState.StableToken,
                finalState.StableToken, Number(StatesVisited), Number(TransitionsEvaluated),
                Number(MoveTransitions), Number(StateTransitions), Number(ShortestTransitionCount),
                InputDigest, StateSpaceDigest, nakedProofDigest, graphDigest,
            }.Concat(shortestActions));
            Map19_04HandoffDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_04_COMPLETION_SEARCH_HANDOFF_V1", graphDigest, nakedProofDigest,
                InputDigest, StateSpaceDigest, ProofDigest, "MAP19_04_LOCKED=1|STARTED=0",
            });
        }

        public GeneratedCompletionStateKey InitialState { get; }
        public GeneratedCompletionStateKey FinalState { get; }
        public int StateDimensionCount => 6;
        public int InitialStateCount => 1;
        public int GoalStateCount => 1;
        public int StatesVisited { get; }
        public int TransitionsEvaluated { get; }
        public int MoveTransitions { get; }
        public int StateTransitions { get; }
        public int GoalsSatisfied => 1;
        public int GoalsMissing => 0;
        public int ShortestTransitionCount { get; }
        public IReadOnlyList<string> ShortestActions => shortestActions;
        public string InputDigest { get; }
        public string StateSpaceDigest { get; }
        public string ProofDigest { get; }
        public string Map19_04HandoffDigest { get; }
        public bool Map19_04Started => false;
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedCompletionSearchResult
    {
        private readonly ReadOnlyCollection<GeneratedCompletionSearchFailure> failures;
        internal GeneratedCompletionSearchResult(
            GeneratedCompletionSearchProof proof,
            IEnumerable<GeneratedCompletionSearchFailure> sourceFailures)
        {
            Proof = proof;
            failures = new ReadOnlyCollection<GeneratedCompletionSearchFailure>((sourceFailures ??
                    Array.Empty<GeneratedCompletionSearchFailure>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
        }
        public bool Success => Proof != null && failures.Count == 0;
        public GeneratedCompletionSearchProof Proof { get; }
        public IReadOnlyList<GeneratedCompletionSearchFailure> Failures => failures;
        public string SuccessProofDigest => Success ? Proof.ProofDigest : string.Empty;
        public string Map19_04HandoffDigest => Success ? Proof.Map19_04HandoffDigest : string.Empty;
    }

    public static class GeneratedCompletionSearch
    {
        public static GeneratedCompletionSearchResult Search(GeneratedCompletionSearchInput input)
        {
            var failures = new List<GeneratedCompletionSearchFailure>();
            if (!Validate(input, failures))
                return Failure(failures);

            var graph = input.Graph;
            var initial = new GeneratedCompletionStateKey(input.NakedSearch.Surface.StartNodeId, 0,
                Initial(input.Goal.Forge), Initial(input.Goal.Seal), Initial(input.Goal.Boss),
                Initial(input.Goal.SpecialState));
            var adjacency = BuildAdjacency(graph,
                new HashSet<TraversalMovementKind>(input.AllowedMovementKinds));
            var transitions = input.Transitions.GroupBy(value => value.NodeId,
                    StringComparer.Ordinal).ToDictionary(value => value.Key,
                    value => value.OrderBy(item => item).ToArray(), StringComparer.Ordinal);
            var queue = new Queue<GeneratedCompletionStateKey>();
            var depth = new Dictionary<GeneratedCompletionStateKey, int>();
            var predecessor = new Dictionary<GeneratedCompletionStateKey, Previous>();
            queue.Enqueue(initial);
            depth[initial] = 0;
            GeneratedCompletionStateKey found = null;
            var moveEvaluations = 0;
            var stateEvaluations = 0;
            while (queue.Count != 0)
            {
                var state = queue.Dequeue();
                if (input.Goal.IsSatisfied(state))
                {
                    found = state;
                    break;
                }
                if (adjacency.TryGetValue(state.PositionNodeId, out var moves))
                {
                    foreach (var move in moves)
                    {
                        moveEvaluations++;
                        var next = new GeneratedCompletionStateKey(move.ToNodeId,
                            state.ResourceMask, state.Forge, state.Seal, state.Boss,
                            state.SpecialState);
                        AddState(state, next, "MOVE|" + move.SourceId);
                    }
                }
                if (!transitions.TryGetValue(state.PositionNodeId, out var bound))
                    continue;
                foreach (var transition in bound)
                {
                    stateEvaluations++;
                    if (transition.TryApply(state, out var next))
                        AddState(state, next, "STATE|" + transition.TransitionId);
                }
            }

            if (found == null)
            {
                var frontier = depth.OrderByDescending(value => value.Value)
                    .ThenBy(value => value.Key).Take(8)
                    .Select(value => new GeneratedCompletionFrontierEvidence(value.Key,
                        value.Value, MissingGoal(input.Goal, value.Key))).ToArray();
                failures.Add(new GeneratedCompletionSearchFailure("CompletionGoal",
                    "COMPLETION_GOAL_UNREACHABLE", input.Goal.ExitNodeId,
                    input.Goal.StableToken, "NO_SATISFYING_STATE", graph.GraphDigest, frontier));
                return Failure(failures);
            }

            var actions = new List<string>();
            var cursor = found;
            while (!cursor.Equals(initial))
            {
                var previous = predecessor[cursor];
                actions.Add(previous.ActionToken);
                cursor = previous.State;
            }
            actions.Reverse();
            var inputDigest = input.ComputeDigest();
            var stateSpaceDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MAP19_03_COMPLETION_STATE_SPACE_V1", inputDigest }
                .Concat(depth.Keys.OrderBy(value => value).Select(value => value.StableToken)));
            var proof = new GeneratedCompletionSearchProof(initial, found, depth.Count,
                moveEvaluations + stateEvaluations, moveEvaluations, stateEvaluations,
                depth[found], actions, inputDigest, stateSpaceDigest,
                input.NakedSearch.Surface.ProofDigest, graph.GraphDigest);
            return new GeneratedCompletionSearchResult(proof, failures);

            void AddState(
                GeneratedCompletionStateKey from,
                GeneratedCompletionStateKey next,
                string action)
            {
                if (depth.ContainsKey(next)) return;
                depth[next] = depth[from] + 1;
                predecessor[next] = new Previous(from, action);
                queue.Enqueue(next);
            }
        }

        private static bool Validate(
            GeneratedCompletionSearchInput input,
            ICollection<GeneratedCompletionSearchFailure> failures)
        {
            if (input == null)
            {
                Add(failures, "CompletionSearch", "MISSING_SEARCH_INPUT", "input",
                    "NON_NULL", "NULL", "MISSING");
                return false;
            }
            var graph = input.Graph;
            if (graph == null)
            {
                Add(failures, "MAP19_02", "MISSING_GRAPH_INPUT", "graph",
                    "NON_NULL", "NULL", "MISSING");
                return false;
            }
            Digest(failures, "MAP19_02", "GRAPH_DIGEST_MISMATCH", graph.GraphDigest,
                GeneratedNakedTraversalSearch.ExpectedGraphDigest, graph.GraphDigest);
            Digest(failures, "MAP19_02", "INCOMING_HANDOFF_DIGEST_MISMATCH",
                graph.Map19_03HandoffDigest,
                GeneratedNakedTraversalSearch.ExpectedIncomingHandoffDigest, graph.GraphDigest);
            if (input.NakedSearch == null || !input.NakedSearch.Success ||
                input.NakedSearch.Surface == null)
                Add(failures, "NakedSearch", "MISSING_NAKED_SUCCESS_PROOF", "naked_search",
                    "SUCCESSFUL_TOOL_ZERO_PROOF", "MISSING_OR_FAILED", graph.GraphDigest);
            else if (!string.Equals(input.NakedSearch.Surface.Graph.GraphDigest,
                         graph.GraphDigest, StringComparison.Ordinal))
                Add(failures, "NakedSearch", "NAKED_GRAPH_DIGEST_MISMATCH", "naked.graph",
                    graph.GraphDigest, input.NakedSearch.Surface.Graph.GraphDigest,
                    graph.GraphDigest);
            if (input.Goal == null)
            {
                Add(failures, "CompletionGoal", "MISSING_COMPLETION_GOAL", "goal",
                    "NON_NULL", "NULL", graph.GraphDigest);
                return false;
            }
            var nodeIds = new HashSet<string>(graph.Nodes.Select(value => value.NodeId),
                StringComparer.Ordinal);
            if (!nodeIds.Contains(input.Goal.ExitNodeId))
                Add(failures, "CompletionGoal", "MISSING_EXIT_TARGET", input.Goal.ExitNodeId,
                    "EXISTING_GRAPH_NODE", "MISSING", graph.GraphDigest);
            var nakedTargets = input.NakedSearch != null && input.NakedSearch.Success
                ? new HashSet<string>(input.NakedSearch.Surface.Proofs.Select(value =>
                    value.TargetNodeId), StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);
            if (input.NakedSearch != null && input.NakedSearch.Success &&
                !nakedTargets.Contains(input.Goal.ExitNodeId))
                Add(failures, "CompletionGoal", "DANGLING_COMPLETION_TARGET_BINDING",
                    input.Goal.ExitNodeId, "NAKED_REACHABILITY_TARGET", "NOT_BOUND",
                    graph.GraphDigest);
            foreach (var duplicate in input.Transitions.GroupBy(value => value.TransitionId,
                         StringComparer.Ordinal).Where(value => value.Count() > 1))
                Add(failures, "StateTransition", "DUPLICATE_STATE_TRANSITION_ID",
                    duplicate.Key, "1", Number(duplicate.Count()), graph.GraphDigest);
            foreach (var transition in input.Transitions)
            {
                if (!nodeIds.Contains(transition.NodeId))
                    Add(failures, "StateTransition", "DANGLING_COMPLETION_TARGET_BINDING",
                        transition.TransitionId, "EXISTING_GRAPH_NODE", transition.NodeId,
                        transition.SourceDigest);
                else if (input.NakedSearch != null && input.NakedSearch.Success &&
                         !nakedTargets.Contains(transition.NodeId) &&
                         !string.Equals(input.NakedSearch.Surface.StartNodeId,
                             transition.NodeId, StringComparison.Ordinal))
                    Add(failures, "StateTransition", "STATE_TRANSITION_POSITION_MISMATCH",
                        transition.TransitionId, "NAKED_TARGET_BINDING", transition.NodeId,
                        transition.SourceDigest);
                if (!Enum.IsDefined(typeof(GeneratedCompletionTransitionKind), transition.Kind) ||
                    !Enum.IsDefined(typeof(GeneratedCompletionBindingSourceKind),
                        transition.SourceKind))
                    Add(failures, "StateTransition", "UNSUPPORTED_STATE_TRANSITION",
                        transition.TransitionId, "DEFINED_TRANSITION_AND_SOURCE_KIND",
                        Number((int)transition.Kind) + "/" + Number((int)transition.SourceKind),
                        transition.SourceDigest);
                if (!BakingCanonicalDigest.IsLowerHexSha256(transition.SourceDigest))
                    Add(failures, "StateTransition", "INVALID_TRANSITION_SOURCE_DIGEST",
                        transition.TransitionId, "LOWER_HEX_SHA256", transition.SourceDigest,
                        graph.GraphDigest);
                var expectedId = new GeneratedCompletionStateTransition(transition.NodeId,
                    transition.Kind, transition.SourceKind, transition.SourceKey,
                    transition.SourceDigest, transition.ResourceBit).TransitionId;
                if (!BakingCanonicalDigest.IsLowerHexSha256(transition.TransitionId) ||
                    !string.Equals(transition.TransitionId, expectedId,
                        StringComparison.Ordinal))
                    Add(failures, "StateTransition", "TRANSITION_ID_SEMANTIC_MISMATCH",
                        transition.TransitionId, expectedId, transition.TransitionId,
                        transition.SourceDigest);
                var isResource = transition.Kind ==
                    GeneratedCompletionTransitionKind.CollectMandatoryResource;
                if (isResource != (transition.ResourceBit != 0 &&
                    (transition.ResourceBit & (transition.ResourceBit - 1)) == 0))
                    Add(failures, "StateTransition", "INVALID_RESOURCE_BIT",
                        transition.TransitionId, isResource ? "SINGLE_BIT" : "0",
                        transition.ResourceBit.ToString(CultureInfo.InvariantCulture),
                        transition.SourceDigest);
            }
            foreach (var movement in input.AllowedMovementKinds.Where(value =>
                         !Enum.IsDefined(typeof(TraversalMovementKind), value)))
                Add(failures, "MovementKindSupport", "UNSUPPORTED_MOVEMENT_KIND",
                    Number((int)movement), "DEFINED_MAP19_01_MOVEMENT", Number((int)movement),
                    graph.GraphDigest);
            if (input.AllowedMovementKinds.Count == 0)
                Add(failures, "MovementKindSupport", "MISSING_ALLOWED_MOVEMENTS", "movements",
                    "MAP19_01_MOVEMENT_SET", "0", graph.GraphDigest);
            RequiredTransitions(input, failures);
            if (input.ActionAudit == null || !input.ActionAudit.IsZero)
                Add(failures, "ForbiddenAction", "IMPLICIT_OR_FORBIDDEN_ACTION_ATTEMPT",
                    "action_audit", "ALL_ZERO_AND_MAP19_04_NOT_STARTED",
                    input.ActionAudit == null ? "NULL" : "NON_ZERO", graph.GraphDigest);
            return failures.Count == 0;
        }

        private static void RequiredTransitions(
            GeneratedCompletionSearchInput input,
            ICollection<GeneratedCompletionSearchFailure> failures)
        {
            var requiredMask = input.Goal.RequiredResourceMask;
            for (ulong bit = 1; bit != 0 && bit <= requiredMask; bit <<= 1)
                if ((requiredMask & bit) != 0 && !input.Transitions.Any(value =>
                        value.Kind == GeneratedCompletionTransitionKind.CollectMandatoryResource &&
                        value.ResourceBit == bit))
                    Add(failures, "CompletionGoal", "MISSING_RESOURCE_TRANSITION",
                        bit.ToString(CultureInfo.InvariantCulture), "EXPLICIT_POPULATION_BINDING",
                        "MISSING", input.Graph.GraphDigest);
            RequireChain(input, failures, input.Goal.Forge != GeneratedForgeValidationState.NotPresent,
                GeneratedCompletionTransitionKind.MakeForgeAvailable,
                GeneratedCompletionTransitionKind.ActivateForge);
            RequireChain(input, failures, input.Goal.Seal != GeneratedSealValidationState.NotPresent,
                GeneratedCompletionTransitionKind.OpenSeal,
                GeneratedCompletionTransitionKind.AcceptSeal);
            RequireChain(input, failures, input.Goal.Boss != GeneratedBossValidationState.NotPresent,
                GeneratedCompletionTransitionKind.MakeBossAvailable,
                GeneratedCompletionTransitionKind.DefeatBoss);
            RequireChain(input, failures,
                input.Goal.SpecialState != GeneratedSpecialValidationState.NotPresent,
                GeneratedCompletionTransitionKind.EnterSpecial,
                GeneratedCompletionTransitionKind.ResolveSpecial,
                GeneratedCompletionTransitionKind.ExitSpecial);
        }

        private static void RequireChain(
            GeneratedCompletionSearchInput input,
            ICollection<GeneratedCompletionSearchFailure> failures,
            bool required,
            params GeneratedCompletionTransitionKind[] kinds)
        {
            if (!required) return;
            foreach (var kind in kinds.Where(kind => !input.Transitions.Any(value =>
                         value.Kind == kind)))
                Add(failures, "CompletionGoal", "MISSING_STATE_TRANSITION", kind.ToString(),
                    "EXPLICIT_TRANSITION", "MISSING", input.Graph.GraphDigest);
        }

        private static GeneratedForgeValidationState Initial(GeneratedForgeValidationState goal) =>
            goal == GeneratedForgeValidationState.NotPresent
                ? GeneratedForgeValidationState.NotPresent : GeneratedForgeValidationState.NotReached;
        private static GeneratedSealValidationState Initial(GeneratedSealValidationState goal) =>
            goal == GeneratedSealValidationState.NotPresent
                ? GeneratedSealValidationState.NotPresent : GeneratedSealValidationState.NotReached;
        private static GeneratedBossValidationState Initial(GeneratedBossValidationState goal) =>
            goal == GeneratedBossValidationState.NotPresent
                ? GeneratedBossValidationState.NotPresent : GeneratedBossValidationState.NotReached;
        private static GeneratedSpecialValidationState Initial(GeneratedSpecialValidationState goal) =>
            goal == GeneratedSpecialValidationState.NotPresent
                ? GeneratedSpecialValidationState.NotPresent : GeneratedSpecialValidationState.None;

        private static Dictionary<string, List<Move>> BuildAdjacency(
            GeneratedTileMovementGraph graph,
            ISet<TraversalMovementKind> allowed)
        {
            var result = graph.Nodes.ToDictionary(value => value.NodeId,
                _ => new List<Move>(), StringComparer.Ordinal);
            foreach (var edge in graph.Edges.Where(value => allowed.Contains(value.MovementKind)))
                result[edge.FromNodeId].Add(new Move(edge.ToNodeId, edge.EdgeId));
            foreach (var link in graph.SocketLinks)
            {
                result[link.FromSocketNodeId].Add(new Move(link.ToSocketNodeId, link.LinkId + ":F"));
                result[link.ToSocketNodeId].Add(new Move(link.FromSocketNodeId, link.LinkId + ":R"));
            }
            foreach (var moves in result.Values) moves.Sort();
            return result;
        }

        private static string MissingGoal(
            GeneratedCompletionGoal goal,
            GeneratedCompletionStateKey state)
        {
            var missing = new List<string>();
            if ((state.ResourceMask & goal.RequiredResourceMask) != goal.RequiredResourceMask)
                missing.Add("RESOURCE");
            if (state.Forge != goal.Forge) missing.Add("FORGE");
            if (state.Seal != goal.Seal) missing.Add("SEAL");
            if (state.Boss != goal.Boss) missing.Add("BOSS");
            if (state.SpecialState != goal.SpecialState) missing.Add("SPECIAL");
            if (!string.Equals(state.PositionNodeId, goal.ExitNodeId, StringComparison.Ordinal))
                missing.Add("EXIT");
            return string.Join(",", missing);
        }

        private static GeneratedCompletionSearchResult Failure(
            IEnumerable<GeneratedCompletionSearchFailure> failures) =>
            new GeneratedCompletionSearchResult(null, failures);
        private static void Digest(
            ICollection<GeneratedCompletionSearchFailure> failures,
            string owner,
            string reason,
            string actual,
            string expected,
            string sourceDigest)
        {
            if (!BakingCanonicalDigest.IsLowerHexSha256(actual) ||
                !string.Equals(actual, expected, StringComparison.Ordinal))
                Add(failures, owner, reason, reason.ToLowerInvariant(), expected,
                    string.IsNullOrEmpty(actual) ? "MISSING" : actual, sourceDigest);
        }
        private static void Add(
            ICollection<GeneratedCompletionSearchFailure> failures,
            string owner,
            string reason,
            string key,
            string expected,
            string actual,
            string sourceDigest) => failures.Add(new GeneratedCompletionSearchFailure(
            owner, reason, key, expected, actual, sourceDigest));
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private sealed class Move : IComparable<Move>
        {
            public Move(string toNodeId, string sourceId)
            { ToNodeId = toNodeId; SourceId = sourceId; }
            public string ToNodeId { get; }
            public string SourceId { get; }
            public int CompareTo(Move other)
            {
                if (other == null) return -1;
                var to = string.Compare(ToNodeId, other.ToNodeId, StringComparison.Ordinal);
                return to != 0 ? to : string.Compare(SourceId, other.SourceId,
                    StringComparison.Ordinal);
            }
        }

        private sealed class Previous
        {
            public Previous(GeneratedCompletionStateKey state, string actionToken)
            { State = state; ActionToken = actionToken; }
            public GeneratedCompletionStateKey State { get; }
            public string ActionToken { get; }
        }
    }
}
