using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.WorldData;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum Sv5RouteStateAnchorKind { ExistingGraphNode = 1, GeneralConnection = 2 }
    public enum Sv5RouteStateReadiness { Verified = 1, Pending = 2 }
    public enum Sv5RouteContactCellKind { Passage = 1, Clearance = 2 }
    public enum Sv5RouteShortcutDecisionCode
    {
        Allowed = 1,
        DuplicateId = 2,
        UnknownAnchor = 3,
        InvalidGeneralAnchor = 4,
        WeakRequiredCondition = 5,
        ReverseOfOneWay = 6,
        CandidateSetGoalUnreachable = 7,
        CandidateSetUnsafeState = 8,
        ReservedCandidateId = 9,
    }

    public sealed class Sv5RouteStateAnchor
    {
        public Sv5RouteStateAnchor(string id, Sv5RouteStateAnchorKind kind)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("An anchor ID is required.", nameof(id));
            if (!Enum.IsDefined(typeof(Sv5RouteStateAnchorKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            Id = id.Trim();
            Kind = kind;
        }

        public string Id { get; }
        public Sv5RouteStateAnchorKind Kind { get; }
    }

    public sealed class Sv5RouteShortcutCandidate
    {
        public Sv5RouteShortcutCandidate(string id, Sv5RouteStateAnchor from, Sv5RouteStateAnchor to,
            Sv5WorldGraphDirection direction, ulong requiredResourceMask, bool requiresForge,
            bool requiresSeal, bool requiresBossComplete, string sourceProvenance)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A candidate ID is required.", nameof(id));
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));
            if (!Enum.IsDefined(typeof(Sv5WorldGraphDirection), direction)) throw new ArgumentOutOfRangeException(nameof(direction));
            if (string.IsNullOrWhiteSpace(sourceProvenance)) throw new ArgumentException("Source provenance is required.", nameof(sourceProvenance));
            Id = id.Trim();
            From = from;
            To = to;
            Direction = direction;
            RequiredResourceMask = requiredResourceMask;
            RequiresForge = requiresForge;
            RequiresSeal = requiresSeal;
            RequiresBossComplete = requiresBossComplete;
            SourceProvenance = sourceProvenance.Trim();
        }

        public string Id { get; }
        public Sv5RouteStateAnchor From { get; }
        public Sv5RouteStateAnchor To { get; }
        public Sv5WorldGraphDirection Direction { get; }
        public ulong RequiredResourceMask { get; }
        public bool RequiresForge { get; }
        public bool RequiresSeal { get; }
        public bool RequiresBossComplete { get; }
        public string SourceProvenance { get; }
        public string CanonicalPayload => Length(Id) + Length(From.Kind.ToString()) + Length(From.Id) +
            Length(To.Kind.ToString()) + Length(To.Id) + Length(Direction.ToString()) +
            RequiredResourceMask.ToString(CultureInfo.InvariantCulture) + "|" + Bool(RequiresForge) + "|" +
            Bool(RequiresSeal) + "|" + Bool(RequiresBossComplete) + "|" + Length(SourceProvenance);

        private static string Length(string value)
        {
            string text = value ?? string.Empty;
            return text.Length.ToString(CultureInfo.InvariantCulture) + ":" + text + "|";
        }

        private static string Bool(bool value) => value ? "1" : "0";
    }

    public sealed class Sv5RouteStateReviewInput
    {
        private readonly ReadOnlyCollection<Sv5RouteStateReviewContact> contacts;
        private readonly ReadOnlyCollection<Sv5SpecialWorldPoint> airWitness;

        public Sv5RouteStateReviewInput(IEnumerable<Sv5RouteStateReviewContact> sourceContacts,
            IEnumerable<Sv5SpecialWorldPoint> sourceAirWitness)
        {
            contacts = new ReadOnlyCollection<Sv5RouteStateReviewContact>((sourceContacts ??
                Array.Empty<Sv5RouteStateReviewContact>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
            airWitness = new ReadOnlyCollection<Sv5SpecialWorldPoint>((sourceAirWitness ??
                Array.Empty<Sv5SpecialWorldPoint>()).ToArray());
        }

        public IReadOnlyList<Sv5RouteStateReviewContact> Contacts => contacts;
        public IReadOnlyList<Sv5SpecialWorldPoint> AirWitness => airWitness;
    }

    /// <summary>One physical route reservation sample used by the shared
    /// contact-pair enumerator.  The type is deliberately independent from
    /// SV5 so focused fixtures can prove pair completeness.</summary>
    public sealed class Sv5RouteContactCell
    {
        public Sv5RouteContactCell(string routeId, Sv5SpecialWorldPoint world,
            Sv5RouteContactCellKind kind)
        {
            if (string.IsNullOrWhiteSpace(routeId))
                throw new ArgumentException("A route ID is required.", nameof(routeId));
            if (!Enum.IsDefined(typeof(Sv5RouteContactCellKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            RouteId = routeId.Trim();
            World = world;
            Kind = kind;
        }

        public string RouteId { get; }
        public Sv5SpecialWorldPoint World { get; }
        public Sv5RouteContactCellKind Kind { get; }
    }

    /// <summary>An unordered distinct route pair at one shared cell or one
    /// cardinal face.  Face orientation remains explicit while duplicate
    /// pair observations at the same physical contact are collapsed.</summary>
    public sealed class Sv5RouteContactPair : IComparable<Sv5RouteContactPair>
    {
        internal Sv5RouteContactPair(string kind, Sv5SpecialWorldPoint firstWorld,
            Sv5SpecialWorldPoint secondWorld, string routeA, string routeB,
            Sv5SpecialWorldPoint routeAWorld, Sv5SpecialWorldPoint routeBWorld,
            string firstKinds, string secondKinds, string routeAKinds, string routeBKinds)
        {
            Kind = kind;
            FirstWorld = firstWorld;
            SecondWorld = secondWorld;
            RouteA = routeA;
            RouteB = routeB;
            RouteAWorld = routeAWorld;
            RouteBWorld = routeBWorld;
            FirstKinds = firstKinds;
            SecondKinds = secondKinds;
            RouteAKinds = routeAKinds;
            RouteBKinds = routeBKinds;
            Direction = firstWorld.Equals(secondWorld) ? "SHARED" :
                secondWorld.X > firstWorld.X ? "RIGHT" : secondWorld.X < firstWorld.X ? "LEFT" :
                secondWorld.Y > firstWorld.Y ? "UP" : "DOWN";
            Id = kind + "_" + firstWorld.X.ToString(CultureInfo.InvariantCulture) + "_" +
                firstWorld.Y.ToString(CultureInfo.InvariantCulture) + "_" +
                secondWorld.X.ToString(CultureInfo.InvariantCulture) + "_" +
                secondWorld.Y.ToString(CultureInfo.InvariantCulture) + "_" +
                Sv5WorldDefinition.Hash(routeA + "|" + routeB).Substring(0, 12);
        }

        public string Id { get; }
        public string Kind { get; }
        public Sv5SpecialWorldPoint FirstWorld { get; }
        public Sv5SpecialWorldPoint SecondWorld { get; }
        public string RouteA { get; }
        public string RouteB { get; }
        public Sv5SpecialWorldPoint RouteAWorld { get; }
        public Sv5SpecialWorldPoint RouteBWorld { get; }
        public string Direction { get; }
        public string FirstKinds { get; }
        public string SecondKinds { get; }
        public string RouteAKinds { get; }
        public string RouteBKinds { get; }
        public string CanonicalPayload => Kind + "|" + FirstWorld + "|" + SecondWorld + "|" + RouteA + "|" +
            RouteAWorld + "|" + RouteAKinds + "|" + RouteB + "|" + RouteBWorld + "|" + RouteBKinds;
        public int CompareTo(Sv5RouteContactPair other) => other == null ? 1 :
            string.Compare(Id, other.Id, StringComparison.Ordinal);
    }

    public sealed class Sv5RouteStateReviewContact : IComparable<Sv5RouteStateReviewContact>
    {
        private readonly ReadOnlyCollection<string> routeIds;

        public Sv5RouteStateReviewContact(Sv5SpecialWorldPoint world, IEnumerable<string> sourceRouteIds)
        {
            World = world;
            routeIds = new ReadOnlyCollection<string>((sourceRouteIds ?? Array.Empty<string>()).Where(value =>
                !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
            if (routeIds.Count < 2) throw new ArgumentException("A review contact needs two route IDs.", nameof(sourceRouteIds));
        }

        public Sv5SpecialWorldPoint World { get; }
        public IReadOnlyList<string> RouteIds => routeIds;
        public int CompareTo(Sv5RouteStateReviewContact other)
        {
            if (other == null) return 1;
            int value = World.CompareTo(other.World);
            return value != 0 ? value : string.Compare(string.Join("|", RouteIds), string.Join("|", other.RouteIds), StringComparison.Ordinal);
        }
    }

    public sealed class Sv5RouteConditionBinding : IComparable<Sv5RouteConditionBinding>
    {
        internal Sv5RouteConditionBinding(Sv5CoreRouteReservation route, Sv5WorldGraphEdge edge,
            Sv5WorldGraphNode sourceNode, Sv5WorldGraphNode targetNode, Sv5SpecialAccess fromAccess,
            Sv5SpecialAccess toAccess)
        {
            Route = route;
            Edge = edge;
            SourceNode = sourceNode;
            TargetNode = targetNode;
            FromAccess = fromAccess;
            ToAccess = toAccess;
        }

        public Sv5CoreRouteReservation Route { get; }
        public Sv5WorldGraphEdge Edge { get; }
        public Sv5WorldGraphNode SourceNode { get; }
        public Sv5WorldGraphNode TargetNode { get; }
        public Sv5SpecialAccess FromAccess { get; }
        public Sv5SpecialAccess ToAccess { get; }
        public bool GeometryReady => false;
        public bool PlayerVerified => false;
        public int CompareTo(Sv5RouteConditionBinding other) => other == null ? 1 :
            string.Compare(Route.RouteId, other.Route.RouteId, StringComparison.Ordinal);
    }

    public sealed class Sv5RouteStateTraceStep : IComparable<Sv5RouteStateTraceStep>
    {
        internal Sv5RouteStateTraceStep(string proofId, int ordinal, string action, string before, string after,
            bool predicatePassed)
        {
            ProofId = proofId;
            Ordinal = ordinal;
            Action = action;
            StateBefore = before;
            StateAfter = after;
            PredicatePassed = predicatePassed;
        }

        public string ProofId { get; }
        public int Ordinal { get; }
        public string Action { get; }
        public string StateBefore { get; }
        public string StateAfter { get; }
        public bool PredicatePassed { get; }
        public int CompareTo(Sv5RouteStateTraceStep other)
        {
            if (other == null) return 1;
            int value = string.Compare(ProofId, other.ProofId, StringComparison.Ordinal);
            return value != 0 ? value : Ordinal.CompareTo(other.Ordinal);
        }
    }

    public sealed class Sv5RouteOrderProof : IComparable<Sv5RouteOrderProof>
    {
        private readonly ReadOnlyCollection<Sv5RouteStateTraceStep> trace;

        internal Sv5RouteOrderProof(Sv5WorldGraphProof source, IEnumerable<Sv5RouteStateTraceStep> sourceTrace)
        {
            Source = source;
            trace = new ReadOnlyCollection<Sv5RouteStateTraceStep>((sourceTrace ?? Array.Empty<Sv5RouteStateTraceStep>())
                .OrderBy(value => value).ToArray());
        }

        public Sv5WorldGraphProof Source { get; }
        public IReadOnlyList<Sv5RouteStateTraceStep> Trace => trace;
        public bool Success => Source != null && Source.Success;
        public int CompareTo(Sv5RouteOrderProof other) => other == null ? 1 :
            string.Compare(Source.ProofId, other.Source.ProofId, StringComparison.Ordinal);
    }

    public sealed class Sv5RouteShortcutDecision : IComparable<Sv5RouteShortcutDecision>
    {
        internal Sv5RouteShortcutDecision(string id, Sv5RouteShortcutDecisionCode code, string detail,
            Sv5WorldGraphState counterexampleBefore = null, Sv5WorldGraphState counterexampleAfter = null,
            string counterexampleAction = null, IEnumerable<string> sourceCounterexamplePrefix = null)
        {
            Id = id ?? string.Empty;
            Code = code;
            Detail = detail ?? string.Empty;
            CounterexampleBefore = counterexampleBefore;
            CounterexampleAfter = counterexampleAfter;
            CounterexampleAction = counterexampleAction ?? string.Empty;
            CounterexamplePrefix = new ReadOnlyCollection<string>((sourceCounterexamplePrefix ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value)).ToArray());
        }

        public string Id { get; }
        public Sv5RouteShortcutDecisionCode Code { get; }
        public string Detail { get; }
        public bool IsAllowed => Code == Sv5RouteShortcutDecisionCode.Allowed;
        public Sv5RouteStateReadiness GeometryReadiness => Sv5RouteStateReadiness.Pending;
        public Sv5WorldGraphState CounterexampleBefore { get; }
        public Sv5WorldGraphState CounterexampleAfter { get; }
        public string CounterexampleAction { get; }
        public IReadOnlyList<string> CounterexamplePrefix { get; }
        public int CompareTo(Sv5RouteShortcutDecision other) => other == null ? 1 :
            string.Compare(Id, other.Id, StringComparison.Ordinal);
    }

    public sealed class Sv5RouteContactCheck : IComparable<Sv5RouteContactCheck>
    {
        private readonly ReadOnlyCollection<string> routeIds;

        internal Sv5RouteContactCheck(string id, string kind, Sv5SpecialWorldPoint world,
            IEnumerable<string> sourceRouteIds, string classification, string detail,
            string requiredPredicate = "", bool logicalStateTransitionChecked = false)
        {
            Id = id;
            Kind = kind;
            World = world;
            routeIds = new ReadOnlyCollection<string>((sourceRouteIds ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
            Classification = classification;
            Detail = detail;
            RequiredPredicate = requiredPredicate ?? string.Empty;
            LogicalStateTransitionChecked = logicalStateTransitionChecked;
        }

        public string Id { get; }
        public string Kind { get; }
        public Sv5SpecialWorldPoint World { get; }
        public IReadOnlyList<string> RouteIds => routeIds;
        public string Classification { get; }
        public string Detail { get; }
        public string RequiredPredicate { get; }
        public bool LogicalStateTransitionChecked { get; }
        public Sv5RouteStateReadiness GeometryReadiness => Sv5RouteStateReadiness.Pending;
        public int CompareTo(Sv5RouteContactCheck other) => other == null ? 1 :
            string.Compare(Id, other.Id, StringComparison.Ordinal);
    }

    public sealed class Sv5RouteStateObligation : IComparable<Sv5RouteStateObligation>
    {
        internal Sv5RouteStateObligation(string id, string ownerTask, string requirement, string readiness)
        {
            Id = id;
            OwnerTask = ownerTask;
            Requirement = requirement;
            Readiness = readiness;
        }

        public string Id { get; }
        public string OwnerTask { get; }
        public string Requirement { get; }
        public string Readiness { get; }
        public int CompareTo(Sv5RouteStateObligation other) => other == null ? 1 :
            string.Compare(Id, other.Id, StringComparison.Ordinal);
    }

    public sealed class Sv5RouteStateAnalysis
    {
        private readonly ReadOnlyCollection<Sv5RouteConditionBinding> bindings;
        private readonly ReadOnlyCollection<Sv5RouteOrderProof> proofs;
        private readonly ReadOnlyCollection<Sv5RouteOrderProof> candidateSetProofs;
        private readonly ReadOnlyCollection<Sv5RouteShortcutCandidate> candidates;
        private readonly ReadOnlyCollection<Sv5RouteShortcutDecision> shortcuts;
        private readonly ReadOnlyCollection<Sv5RouteContactCheck> contacts;
        private readonly ReadOnlyCollection<Sv5RouteStateObligation> obligations;

        internal Sv5RouteStateAnalysis(Sv5CoreReservationPlan corePlan,
            IEnumerable<Sv5RouteConditionBinding> sourceBindings, IEnumerable<Sv5RouteOrderProof> sourceProofs,
            IEnumerable<Sv5RouteOrderProof> sourceCandidateSetProofs,
            IEnumerable<Sv5RouteShortcutCandidate> sourceCandidates,
            IEnumerable<Sv5RouteShortcutDecision> sourceShortcuts, IEnumerable<Sv5RouteContactCheck> sourceContacts,
            IEnumerable<Sv5RouteStateObligation> sourceObligations, Sv5RouteStateReviewInput review)
        {
            CorePlan = corePlan;
            bindings = Freeze(sourceBindings);
            proofs = Freeze(sourceProofs);
            candidateSetProofs = Freeze(sourceCandidateSetProofs);
            candidates = new ReadOnlyCollection<Sv5RouteShortcutCandidate>((sourceCandidates ??
                Array.Empty<Sv5RouteShortcutCandidate>()).Where(value => value != null)
                .OrderBy(value => value.CanonicalPayload, StringComparer.Ordinal).ToArray());
            shortcuts = Freeze(sourceShortcuts);
            contacts = Freeze(sourceContacts);
            obligations = Freeze(sourceObligations);
            Review = review ?? throw new ArgumentNullException(nameof(review));
            LogicalStateVerified = bindings.Count == 11 && proofs.Count == 6 && proofs.All(value => value.Success) &&
                candidateSetProofs.Count == 6 && candidateSetProofs.All(value => value.Success) &&
                shortcuts.All(value => value.IsAllowed);
            ContactStateVerified = contacts.Count != 0 && contacts.All(value =>
                value.LogicalStateTransitionChecked &&
                !string.Equals(value.Classification, "UNKNOWN_ROUTE_REJECTED", StringComparison.Ordinal) &&
                !string.Equals(value.Classification, "INVALID_WITNESS", StringComparison.Ordinal));
            GeometryStateReady = false;
            PlayerVerified = false;
            Digest = Sv5WorldDefinition.Hash(string.Join("\n", CanonicalLines()));
        }

        public Sv5CoreReservationPlan CorePlan { get; }
        public IReadOnlyList<Sv5RouteConditionBinding> ConditionBindings => bindings;
        public IReadOnlyList<Sv5RouteOrderProof> OrderProofs => proofs;
        public IReadOnlyList<Sv5RouteOrderProof> CandidateSetProofs => candidateSetProofs;
        public IReadOnlyList<Sv5RouteShortcutCandidate> Candidates => candidates;
        public IReadOnlyList<Sv5RouteShortcutDecision> ShortcutDecisions => shortcuts;
        public IReadOnlyList<Sv5RouteContactCheck> ContactChecks => contacts;
        public IReadOnlyList<Sv5RouteStateObligation> Obligations => obligations;
        public Sv5RouteStateReviewInput Review { get; }
        public bool LogicalStateVerified { get; }
        public bool ContactStateVerified { get; }
        public bool GeometryStateReady { get; }
        public bool PlayerVerified { get; }
        public string Digest { get; }

        private IEnumerable<string> CanonicalLines()
        {
            yield return "SV5_ROUTE_STATE_ANALYSIS_V2";
            yield return Record("core", CorePlan.Digest);
            yield return Record("graph", CorePlan.RouteSource.Graph.Digest);
            foreach (Sv5RouteConditionBinding value in bindings)
                yield return Record("binding", value.Route.RouteId, value.Edge.EdgeId, value.ToAccess.Condition);
            foreach (Sv5RouteOrderProof value in proofs)
                yield return ProofLine("baseline", value);
            foreach (Sv5RouteOrderProof value in candidateSetProofs)
                yield return ProofLine("candidate_set", value);
            foreach (Sv5RouteShortcutCandidate value in candidates)
                yield return "candidate|" + value.CanonicalPayload;
            foreach (Sv5RouteShortcutDecision value in shortcuts)
                yield return Record("shortcut", value.Id, value.Code.ToString(), value.Detail,
                    value.CounterexampleBefore == null ? "" : value.CounterexampleBefore.StableToken,
                    value.CounterexampleAfter == null ? "" : value.CounterexampleAfter.StableToken,
                    value.CounterexampleAction, string.Join("\u001f", value.CounterexamplePrefix));
            foreach (Sv5RouteStateReviewContact value in Review.Contacts)
                yield return Record("review_contact", value.World.X.ToString(CultureInfo.InvariantCulture),
                    value.World.Y.ToString(CultureInfo.InvariantCulture), string.Join("\u001f", value.RouteIds));
            for (int index = 0; index < Review.AirWitness.Count; index++)
            {
                Sv5SpecialWorldPoint value = Review.AirWitness[index];
                yield return Record("air_witness", index.ToString(CultureInfo.InvariantCulture),
                    value.X.ToString(CultureInfo.InvariantCulture), value.Y.ToString(CultureInfo.InvariantCulture));
            }
            foreach (Sv5RouteContactCheck value in contacts)
                yield return Record("contact", value.Id, value.Kind, value.World.X.ToString(CultureInfo.InvariantCulture),
                    value.World.Y.ToString(CultureInfo.InvariantCulture), string.Join("\u001f", value.RouteIds),
                    value.Classification, value.Detail, value.RequiredPredicate,
                    value.LogicalStateTransitionChecked ? "1" : "0");
            foreach (Sv5RouteStateObligation value in obligations)
                yield return Record("obligation", value.Id, value.OwnerTask, value.Requirement, value.Readiness);
        }

        private static string ProofLine(string scope, Sv5RouteOrderProof value) => Record("proof", scope,
            value.Source.ProofId, value.Success ? "1" : "0", string.Join("\u001f", value.Source.RequestedOrder),
            string.Join("\u001f", value.Source.Actions), value.Source.FinalState == null ? "" : value.Source.FinalState.StableToken,
            string.Join("\u001f", value.Trace.Select(step => step.Ordinal.ToString(CultureInfo.InvariantCulture) + ":" +
                step.Action + ":" + step.StateBefore + ":" + step.StateAfter + ":" + (step.PredicatePassed ? "1" : "0"))));
        private static string Record(string type, params string[] values) => type + "|" + string.Join("", (values ??
            Array.Empty<string>()).Select(value => Length(value)));
        private static string Length(string value)
        {
            string text = value ?? string.Empty;
            return text.Length.ToString(CultureInfo.InvariantCulture) + ":" + text + "|";
        }

        private static ReadOnlyCollection<T> Freeze<T>(IEnumerable<T> values) where T : IComparable<T> =>
            new ReadOnlyCollection<T>((values ?? Array.Empty<T>()).Where(value => value != null).OrderBy(value => value).ToArray());
    }

    public static class Sv5RouteStatePolicy
    {
        private const ulong AllResources = 7;
        private static readonly Sv5WorldGraphRole[] Resources =
        {
            Sv5WorldGraphRole.MooncoreOre,
            Sv5WorldGraphRole.CondensedCoefficientSap,
            Sv5WorldGraphRole.DeepStarYeast,
        };

        public static Sv5RouteStateAnalysis Analyze(Sv5CoreReservationPlan corePlan,
            IEnumerable<Sv5RouteShortcutCandidate> sourceCandidates, Sv5RouteStateReviewInput review)
        {
            if (corePlan == null) throw new ArgumentNullException(nameof(corePlan));
            if (review == null) throw new ArgumentNullException(nameof(review));
            Sv5WorldGraphPlan graph = corePlan.RouteSource.Graph;
            if (graph == null || !graph.Success) throw new ArgumentException("A passing SV5 graph is required.", nameof(corePlan));
            if (corePlan.Routes.Count != 11 || graph.Edges.Count != corePlan.Routes.Count)
                throw new ArgumentException("SV5_05 requires the current eleven-route source.", nameof(corePlan));

            Sv5RouteConditionBinding[] bindings = Bind(corePlan, graph).ToArray();
            Sv5RouteOrderProof[] proofs = ResourceOrders().Select(order => BuildProof(graph.Nodes, graph.Edges, order)).ToArray();
            ShortcutEvaluation shortcutResult = EvaluateShortcuts(corePlan, graph, sourceCandidates);
            Sv5RouteContactCheck[] contacts = CollectContacts(corePlan, bindings, review).ToArray();
            Sv5RouteStateObligation[] obligations = BuildObligations(contacts).ToArray();
            return new Sv5RouteStateAnalysis(corePlan, bindings, proofs,
                shortcutResult.CandidateSetProofs, shortcutResult.Candidates, shortcutResult.Decisions,
                contacts, obligations, review);
        }

        public static Sv5WorldGraphPlan EvaluateReturnPolicy(Sv5CoreReservationPlan corePlan,
            Sv5WorldReturnShortcutPolicy policy)
        {
            if (corePlan == null) throw new ArgumentNullException(nameof(corePlan));
            return Sv5WorldGraphPlanner.Plan(corePlan.RouteSource.Definition, policy);
        }

        /// <summary>Enumerates every unordered distinct route pair at shared
        /// cells and cardinal faces.  A common route never suppresses the
        /// remaining Cartesian-product pairs.</summary>
        public static IReadOnlyList<Sv5RouteContactPair> EnumerateContactPairs(
            IEnumerable<Sv5RouteContactCell> sourceCells)
        {
            var groups = (sourceCells ?? Array.Empty<Sv5RouteContactCell>()).Where(value => value != null)
                .GroupBy(value => value.World).ToDictionary(value => value.Key, value => value
                    .GroupBy(cell => cell.RouteId, StringComparer.Ordinal)
                    .ToDictionary(route => route.Key, route => string.Join("|", route.Select(cell => cell.Kind)
                        .Distinct().OrderBy(kind => kind).Select(kind => kind.ToString())), StringComparer.Ordinal));
            var output = new Dictionary<string, Sv5RouteContactPair>(StringComparer.Ordinal);
            foreach (KeyValuePair<Sv5SpecialWorldPoint, Dictionary<string, string>> entry in groups)
            {
                string[] routes = entry.Value.Keys.OrderBy(value => value, StringComparer.Ordinal).ToArray();
                for (var left = 0; left < routes.Length; left++)
                for (var right = left + 1; right < routes.Length; right++)
                    AddPair("SHARED", entry.Key, entry.Key, routes[left], routes[right], entry.Key, entry.Key,
                        entry.Value[routes[left]], entry.Value[routes[right]], entry.Value[routes[left]],
                        entry.Value[routes[right]]);

                foreach (Sv5SpecialWorldPoint neighbor in Neighbors(entry.Key))
                {
                    if (entry.Key.CompareTo(neighbor) >= 0 || !groups.TryGetValue(neighbor, out var other)) continue;
                    foreach (string firstRoute in routes)
                    foreach (string secondRoute in other.Keys.OrderBy(value => value, StringComparer.Ordinal))
                    {
                        if (string.Equals(firstRoute, secondRoute, StringComparison.Ordinal)) continue;
                        string routeA = string.Compare(firstRoute, secondRoute, StringComparison.Ordinal) < 0 ?
                            firstRoute : secondRoute;
                        string routeB = string.Equals(routeA, firstRoute, StringComparison.Ordinal) ?
                            secondRoute : firstRoute;
                        AddPair("FACE", entry.Key, neighbor, routeA, routeB,
                            string.Equals(routeA, firstRoute, StringComparison.Ordinal) ? entry.Key : neighbor,
                            string.Equals(routeA, firstRoute, StringComparison.Ordinal) ? neighbor : entry.Key,
                            entry.Value[firstRoute], other[secondRoute],
                            string.Equals(routeA, firstRoute, StringComparison.Ordinal) ? entry.Value[firstRoute] :
                                other[secondRoute],
                            string.Equals(routeA, firstRoute, StringComparison.Ordinal) ? other[secondRoute] :
                                entry.Value[firstRoute]);
                    }
                }
            }
            return new ReadOnlyCollection<Sv5RouteContactPair>(output.Values.OrderBy(value => value).ToArray());

            void AddPair(string kind, Sv5SpecialWorldPoint first, Sv5SpecialWorldPoint second,
                string routeA, string routeB, Sv5SpecialWorldPoint routeAWorld,
                Sv5SpecialWorldPoint routeBWorld, string firstKinds, string secondKinds,
                string routeAKinds, string routeBKinds)
            {
                var pair = new Sv5RouteContactPair(kind, first, second, routeA, routeB, routeAWorld, routeBWorld,
                    firstKinds, secondKinds, routeAKinds, routeBKinds);
                if (!output.ContainsKey(pair.Id)) output.Add(pair.Id, pair);
            }
        }

        private static IEnumerable<Sv5RouteConditionBinding> Bind(Sv5CoreReservationPlan corePlan,
            Sv5WorldGraphPlan graph)
        {
            var nodes = graph.Nodes.ToDictionary(value => value.NodeId, value => value, StringComparer.Ordinal);
            var edges = graph.Edges.ToDictionary(value => value.EdgeId, value => value, StringComparer.Ordinal);
            foreach (Sv5CoreRouteReservation route in corePlan.Routes.OrderBy(value => value))
            {
                if (!edges.TryGetValue(route.RouteId, out Sv5WorldGraphEdge edge))
                    throw new ArgumentException("Unknown SV5 edge for route " + route.RouteId + ".", nameof(corePlan));
                if (!nodes.TryGetValue(edge.SourceNodeId, out Sv5WorldGraphNode sourceNode) ||
                    !nodes.TryGetValue(edge.TargetNodeId, out Sv5WorldGraphNode targetNode))
                    throw new ArgumentException("Route edge has an unknown SV5 node.", nameof(corePlan));
                if (!string.Equals(route.Condition, edge.TraversalCondition, StringComparison.Ordinal))
                    throw new ArgumentException("Route label does not equal its SV5 edge condition.", nameof(corePlan));
                Sv5SpecialAccess from = corePlan.Source.Accesses.Single(value => value.Id == route.FromPortId);
                Sv5SpecialAccess to = corePlan.Source.Accesses.Single(value => value.Id == route.ToPortId);
                if (!ContainsNode(from.SourceNodeId, edge.SourceNodeId) || !ContainsNode(to.SourceNodeId, edge.TargetNodeId))
                    throw new ArgumentException("Route port source-node binding is inconsistent.", nameof(corePlan));
                if (!IsKnownPortCondition(from.Condition) || !IsKnownPortCondition(to.Condition))
                    throw new ArgumentException("Unknown route port condition.", nameof(corePlan));
                yield return new Sv5RouteConditionBinding(route, edge, sourceNode, targetNode, from, to);
            }
        }

        private static ShortcutEvaluation EvaluateShortcuts(Sv5CoreReservationPlan corePlan,
            Sv5WorldGraphPlan graph, IEnumerable<Sv5RouteShortcutCandidate> sourceCandidates)
        {
            Sv5RouteShortcutCandidate[] candidates = (sourceCandidates ?? Array.Empty<Sv5RouteShortcutCandidate>())
                .Where(value => value != null).OrderBy(value => value.CanonicalPayload, StringComparer.Ordinal).ToArray();
            var decisions = new List<Sv5RouteShortcutDecision>();
            var graphNodeIds = new HashSet<string>(graph.Nodes.Select(value => value.NodeId), StringComparer.Ordinal);
            var generalIds = new HashSet<string>(StringComparer.Ordinal);
            var accepted = new List<Sv5RouteShortcutCandidate>();
            if (candidates.Any(value => string.Equals(value.Id, "CANDIDATE_SET", StringComparison.Ordinal)))
                decisions.Add(new Sv5RouteShortcutDecision("CANDIDATE_SET", Sv5RouteShortcutDecisionCode.ReservedCandidateId,
                    "CANDIDATE_SET is reserved for the aggregate candidate-set decision."));
            foreach (IGrouping<string, Sv5RouteShortcutCandidate> duplicate in candidates.GroupBy(value => value.Id,
                         StringComparer.Ordinal).Where(value => value.Count() > 1))
                foreach (Sv5RouteShortcutCandidate value in duplicate)
                    decisions.Add(new Sv5RouteShortcutDecision(value.Id, Sv5RouteShortcutDecisionCode.DuplicateId,
                        "Candidate IDs must be unique."));

            var duplicateIds = new HashSet<string>(decisions.Select(value => value.Id), StringComparer.Ordinal);
            foreach (Sv5RouteShortcutCandidate candidate in candidates.Where(value => !duplicateIds.Contains(value.Id)))
            {
                Sv5RouteShortcutDecision decision = ValidateCandidate(corePlan, graph, graphNodeIds, generalIds, candidate);
                decisions.Add(decision);
                if (decision.IsAllowed) accepted.Add(candidate);
            }

            if (decisions.Any(value => !value.IsAllowed))
            {
                decisions.Add(new Sv5RouteShortcutDecision("CANDIDATE_SET",
                    Sv5RouteShortcutDecisionCode.CandidateSetGoalUnreachable,
                    "The candidate set contains a rejected member and was not applied."));
                return new ShortcutEvaluation(candidates, decisions, Array.Empty<Sv5RouteOrderProof>());
            }

            Sv5WorldGraphEdge[] candidateEdges = accepted.Select(ToEdge).ToArray();
            Sv5WorldGraphRole[][] orders = ResourceOrders().ToArray();
            Sv5WorldGraphProof[] proofs = orders.Select(order => Sv5WorldGraphPlanner.EvaluateWithAnalysisNodes(
                graph.Nodes, generalIds, graph.Edges.Concat(candidateEdges), order)).ToArray();
            Sv5RouteOrderProof[] candidateSetProofs = proofs.Select(value => ToRouteProof(value, graph.Nodes,
                graph.Edges.Concat(candidateEdges))).ToArray();
            if (proofs.Any(value => !value.Success))
            {
                Sv5WorldGraphProof failure = proofs.First(value => !value.Success);
                decisions.Add(new Sv5RouteShortcutDecision("CANDIDATE_SET",
                    Sv5RouteShortcutDecisionCode.CandidateSetGoalUnreachable,
                    failure.Failures.First().Code + ":" + failure.Failures.First().Detail));
            }
            else
            {
                Sv5RouteShortcutDecision unsafeDecision = FindUnsafeReachableState(graph, generalIds,
                    graph.Edges.Concat(candidateEdges), orders);
                decisions.Add(unsafeDecision ?? new Sv5RouteShortcutDecision("CANDIDATE_SET",
                    Sv5RouteShortcutDecisionCode.Allowed,
                    "All six SV5 resource orders retain required actions, goal recovery, and exit reachability."));
            }
            return new ShortcutEvaluation(candidates, decisions, candidateSetProofs);
        }

        private static Sv5RouteShortcutDecision ValidateCandidate(Sv5CoreReservationPlan corePlan,
            Sv5WorldGraphPlan graph, ISet<string> graphNodeIds, ISet<string> generalIds,
            Sv5RouteShortcutCandidate candidate)
        {
            if (!AnchorIsKnown(candidate.From, graphNodeIds, generalIds) ||
                !AnchorIsKnown(candidate.To, graphNodeIds, generalIds))
                return new Sv5RouteShortcutDecision(candidate.Id, Sv5RouteShortcutDecisionCode.UnknownAnchor,
                    "Candidate anchors must be existing SV5 nodes or declared general analysis nodes.");
            if (string.Equals(candidate.From.Id, candidate.To.Id, StringComparison.Ordinal))
                return new Sv5RouteShortcutDecision(candidate.Id, Sv5RouteShortcutDecisionCode.InvalidGeneralAnchor,
                    "A shortcut must connect two distinct anchors.");
            if (graphNodeIds.Contains(candidate.To.Id))
            {
                Sv5SpecialAccess target = TargetAccess(corePlan, graph, candidate.To.Id);
                PortRequirement requirement = ParsePortRequirement(target.Condition);
                if (!requirement.IsKnown || !HasRequirement(candidate, requirement))
                    return new Sv5RouteShortcutDecision(candidate.Id, Sv5RouteShortcutDecisionCode.WeakRequiredCondition,
                        "Candidate does not preserve target port condition " + target.Condition + ".");
                Sv5WorldGraphRole targetRole = graph.Nodes.Single(value => value.NodeId == candidate.To.Id).Role;
                PortRequirement logicalEntry = LogicalEntryRequirement(targetRole);
                if (!HasRequirement(candidate, logicalEntry))
                {
                    EntryCounterexample counterexample = FindMissingEntryCounterexample(graph, candidate, logicalEntry);
                    return new Sv5RouteShortcutDecision(candidate.Id, Sv5RouteShortcutDecisionCode.WeakRequiredCondition,
                        "Candidate does not preserve logical " + targetRole + " entry predicate " +
                        DescribeMissingRequirement(candidate, logicalEntry) + ".", counterexample == null ? null :
                        counterexample.Before, counterexample == null ? null : counterexample.After,
                        counterexample == null ? "" : counterexample.Action, counterexample == null ? Array.Empty<string>() :
                        counterexample.Prefix);
                }
            }
            bool hasDirectEdge = graph.Edges.Any(edge => edge.SourceNodeId == candidate.From.Id &&
                edge.TargetNodeId == candidate.To.Id);
            bool reversesOneWay = graph.Edges.Any(edge => edge.SourceNodeId == candidate.To.Id &&
                edge.TargetNodeId == candidate.From.Id && edge.SourceConnectionIsOneWay);
            if (reversesOneWay && !hasDirectEdge)
                return new Sv5RouteShortcutDecision(candidate.Id, Sv5RouteShortcutDecisionCode.ReverseOfOneWay,
                    "A reverse of an existing one-way connection needs its own declared SV5 route.");
            return new Sv5RouteShortcutDecision(candidate.Id, Sv5RouteShortcutDecisionCode.Allowed,
                "Logical proposal only; geometry and Player evidence remain pending.");
        }

        private static EntryCounterexample FindMissingEntryCounterexample(Sv5WorldGraphPlan graph,
            Sv5RouteShortcutCandidate candidate, PortRequirement requirement)
        {
            if (candidate.From.Kind != Sv5RouteStateAnchorKind.ExistingGraphNode ||
                candidate.To.Kind != Sv5RouteStateAnchorKind.ExistingGraphNode) return null;
            Sv5WorldGraphEdge edge = ToEdge(candidate);
            foreach (Sv5WorldGraphRole[] order in ResourceOrders())
            {
                Sv5WorldGraphExploration exploration = Sv5WorldGraphPlanner.ExploreWithAnalysisNodes(graph.Nodes,
                    Array.Empty<string>(), graph.Edges.Concat(new[] { edge }), order);
                Sv5WorldGraphTransition transition = exploration.Transitions.Where(value => value.Action == "MOVE|" +
                    edge.EdgeId && (!requirement.RequiresForge || value.Before.ForgeMade) &&
                    (!requirement.RequiresSeal || !value.Before.SealOpen) && (!requirement.RequiresBoss ||
                        !value.Before.BossComplete)).OrderBy(value => value).FirstOrDefault();
                if (transition != null) return new EntryCounterexample(transition.Before, transition.After, transition.Action,
                    PrefixTo(graph, exploration, transition.After));
            }
            return null;
        }

        private static Sv5RouteShortcutDecision FindUnsafeReachableState(Sv5WorldGraphPlan graph,
            IEnumerable<string> generalIds, IEnumerable<Sv5WorldGraphEdge> edges, IEnumerable<Sv5WorldGraphRole[]> orders)
        {
            foreach (Sv5WorldGraphRole[] order in orders ?? Array.Empty<Sv5WorldGraphRole[]>())
            {
                Sv5WorldGraphExploration exploration = Sv5WorldGraphPlanner.ExploreWithAnalysisNodes(graph.Nodes,
                    generalIds, edges, order);
                var goalStates = new HashSet<Sv5WorldGraphState>(exploration.States.Where(state => IsGoalState(graph, state,
                    order.Length)));
                var recoverable = new HashSet<Sv5WorldGraphState>(goalStates);
                var queue = new Queue<Sv5WorldGraphState>(goalStates);
                while (queue.Count != 0)
                {
                    Sv5WorldGraphState state = queue.Dequeue();
                    foreach (Sv5WorldGraphTransition transition in exploration.Transitions.Where(value => value.After.Equals(state)))
                        if (recoverable.Add(transition.Before)) queue.Enqueue(transition.Before);
                }
                Sv5WorldGraphState unsafeState = exploration.States.Where(state => !recoverable.Contains(state))
                    .OrderBy(state => state).FirstOrDefault();
                if (unsafeState == null) continue;
                Sv5WorldGraphTransition entering = exploration.Transitions.Where(value => value.After.Equals(unsafeState))
                    .OrderBy(value => value).FirstOrDefault();
                IReadOnlyList<string> prefix = PrefixTo(graph, exploration, unsafeState);
                return new Sv5RouteShortcutDecision("CANDIDATE_SET", Sv5RouteShortcutDecisionCode.CandidateSetUnsafeState,
                    "Reachable state cannot recover to the required EXIT goal for order " + string.Join(">", order) + ": " +
                    unsafeState.StableToken, entering == null ? null : entering.Before, unsafeState,
                    entering == null ? "" : entering.Action, prefix);
            }
            return null;
        }

        private static bool IsGoalState(Sv5WorldGraphPlan graph, Sv5WorldGraphState state, int orderLength) => state != null &&
            string.Equals(state.PositionNodeId, graph.Nodes.Single(value => value.Role == Sv5WorldGraphRole.Exit).NodeId,
                StringComparison.Ordinal) && state.ResourceMask == AllResources && state.OrderCursor == orderLength &&
            state.ForgeMade && state.SealOpen && state.BossComplete;

        private static IReadOnlyList<string> PrefixTo(Sv5WorldGraphPlan graph, Sv5WorldGraphExploration exploration,
            Sv5WorldGraphState target)
        {
            Sv5WorldGraphState start = exploration.States.Single(state => state.PositionNodeId == graph.Nodes.Single(value =>
                value.Role == Sv5WorldGraphRole.Start).NodeId && state.ResourceMask == 0 && state.OrderCursor == 0 &&
                !state.ForgeMade && !state.SealOpen && !state.BossComplete);
            var queue = new Queue<Sv5WorldGraphState>();
            var previous = new Dictionary<Sv5WorldGraphState, Sv5WorldGraphTransition>();
            queue.Enqueue(start);
            previous.Add(start, null);
            while (queue.Count != 0 && !previous.ContainsKey(target))
            {
                Sv5WorldGraphState state = queue.Dequeue();
                foreach (Sv5WorldGraphTransition transition in exploration.Transitions.Where(value => value.Before.Equals(state))
                             .OrderBy(value => value))
                    if (!previous.ContainsKey(transition.After))
                    {
                        previous.Add(transition.After, transition);
                        queue.Enqueue(transition.After);
                    }
            }
            if (!previous.ContainsKey(target)) return Array.Empty<string>();
            var output = new List<string>();
            for (Sv5WorldGraphState cursor = target; !cursor.Equals(start); cursor = previous[cursor].Before)
                output.Add(previous[cursor].Action);
            output.Reverse();
            return output;
        }

        private static IEnumerable<Sv5RouteContactCheck> CollectContacts(Sv5CoreReservationPlan corePlan,
            IEnumerable<Sv5RouteConditionBinding> sourceBindings, Sv5RouteStateReviewInput review)
        {
            var bindings = sourceBindings.ToDictionary(value => value.Route.RouteId, value => value, StringComparer.Ordinal);
            var output = new Dictionary<string, Sv5RouteContactCheck>(StringComparer.Ordinal);
            Sv5RouteContactCell[] cells = corePlan.RouteCells.Where(value =>
                    value.Kind == Sv5CoreRouteReservationKind.Passage ||
                    value.Kind == Sv5CoreRouteReservationKind.Clearance)
                .Select(value => new Sv5RouteContactCell(value.RouteId, value.World,
                    value.Kind == Sv5CoreRouteReservationKind.Passage ? Sv5RouteContactCellKind.Passage :
                    Sv5RouteContactCellKind.Clearance)).ToArray();
            foreach (Sv5RouteContactPair pair in EnumerateContactPairs(cells))
                AddContact(output, pair, bindings);
            foreach (Sv5RouteStateReviewContact reviewContact in review.Contacts)
            {
                bool routesExist = reviewContact.RouteIds.All(bindings.ContainsKey);
                string id = ContactId("REVIEW", reviewContact.World, reviewContact.RouteIds);
                output[id] = new Sv5RouteContactCheck(id, "REVIEW_LABEL_CONTACT", reviewContact.World,
                    reviewContact.RouteIds, routesExist ? "LOGICAL_GUARD_PRESENT_GEOMETRY_PENDING" : "UNKNOWN_ROUTE_REJECTED",
                    routesExist ? "Review label mismatch is classified from actual SV5 predicates, not the label alone."
                        : "Review input references an unknown route ID.", routesExist ?
                    RequiredPredicate(reviewContact.RouteIds, bindings) : "UNKNOWN_ROUTE", false);
            }
            if (review.AirWitness.Count != 0)
            {
                bool contiguous = IsContiguous(review.AirWitness);
                bool endpoints = IsAccessEndpoint(corePlan, review.AirWitness.First(), "SV5_SITE_START_PORT_ENTRY") &&
                    IsAccessEndpoint(corePlan, review.AirWitness.Last(), "SV5_SITE_EXIT_PORT_ENTRY");
                bool fixedAir = review.AirWitness.All(point => corePlan.CoreCells.Any(cell => cell.World.Equals(point) &&
                        cell.BaseCell == Sv5PatternBaseCell.Air) || corePlan.RouteCells.Any(cell => cell.World.Equals(point) &&
                        cell.RequiredBaseCell == Sv5PatternBaseCell.Air));
                bool excludesSealed = !corePlan.StateGeometry.Any(cell => string.Equals(cell.State, "SEALED",
                    StringComparison.Ordinal) && cell.BaseCell == Sv5PatternBaseCell.Solid && review.AirWitness.Contains(cell.World));
                bool validWitness = contiguous && endpoints && fixedAir && excludesSealed;
                string id = "AIR_WITNESS_109_EDGE";
                output[id] = new Sv5RouteContactCheck(id, "RAW_AIR_WITNESS", review.AirWitness.First(),
                    Array.Empty<string>(), validWitness ? "STATIC_AIR_CONTACT_GEOMETRY_PENDING" : "INVALID_WITNESS",
                    validWitness ? "Raw 4-neighbour fixed-AIR witness is diagnostic only; it is not Player proof."
                        : "Review witness must retain cardinal endpoints, fixed AIR cells, and SEALED exclusion.",
                    "CARDINAL_CONTIGUOUS && FIXED_AIR && EXCLUDES_SEALED", false);
            }
            return output.Values.OrderBy(value => value).ToArray();
        }

        private static IEnumerable<Sv5RouteStateObligation> BuildObligations(IEnumerable<Sv5RouteContactCheck> contacts)
        {
            yield return new Sv5RouteStateObligation("SV5_06_VILLAGE_PORTS", "SV5_06_SPACE_GRAPH",
                "Connect preserved Village ENTRY/EXIT optional access and return without making it mandatory.", "PENDING");
            yield return new Sv5RouteStateObligation("SV5_06_START_EXIT", "SV5_06_SPACE_GRAPH",
                "Declare use or non-use of the preserved Start EXIT port in generated geometry.", "PENDING");
            if ((contacts ?? Array.Empty<Sv5RouteContactCheck>()).Any(value => value.GeometryReadiness == Sv5RouteStateReadiness.Pending))
                yield return new Sv5RouteStateObligation("SV5_06_CONDITION_CONTACTS", "SV5_06_SPACE_GRAPH",
                    "Resolve or guard condition-crossing route contacts in final generated geometry.", "PENDING");
            yield return new Sv5RouteStateObligation("SV5_09_ROUTE_RECHECK", "SV5_09_LOOPS",
                "Recheck shortcut and condition boundaries after loop generation.", "PENDING");
            yield return new Sv5RouteStateObligation("SV5_41_COMPOSED_GEOMETRY", "SV5_41_COMPOSE",
                "Recheck final geometry contacts before geometry readiness promotion.", "PENDING");
            yield return new Sv5RouteStateObligation("SV5_44_WHOLE_WORLD_PLAYER", "SV5_44_WORLD_PLAYER",
                "Run whole-world Player traversal verification after composed geometry is available.", "PENDING");
        }

        private static Sv5RouteOrderProof BuildProof(IEnumerable<Sv5WorldGraphNode> nodes,
            IEnumerable<Sv5WorldGraphEdge> edges, Sv5WorldGraphRole[] order)
        {
            Sv5WorldGraphProof proof = Sv5WorldGraphPlanner.Evaluate(nodes, edges, order);
            return ToRouteProof(proof, nodes, edges);
        }

        private static Sv5RouteOrderProof ToRouteProof(Sv5WorldGraphProof proof,
            IEnumerable<Sv5WorldGraphNode> nodes, IEnumerable<Sv5WorldGraphEdge> edges)
        {
            var trace = new List<Sv5RouteStateTraceStep>();
            if (!proof.Success) return new Sv5RouteOrderProof(proof, trace);
            var graphNodes = nodes.ToDictionary(value => value.NodeId, value => value, StringComparer.Ordinal);
            var graphEdges = edges.ToDictionary(value => value.EdgeId, value => value, StringComparer.Ordinal);
            string position = graphNodes.Values.Single(value => value.Role == Sv5WorldGraphRole.Start).NodeId;
            ulong mask = 0;
            var cursor = 0;
            var forge = false;
            var seal = false;
            var boss = false;
            for (var index = 0; index < proof.Actions.Count; index++)
            {
                string before = TraceState(position, mask, cursor, forge, seal, boss);
                string action = proof.Actions[index];
                if (action.StartsWith("MOVE|", StringComparison.Ordinal) && graphEdges.TryGetValue(action.Substring(5), out Sv5WorldGraphEdge edge))
                    position = edge.TargetNodeId;
                else if (action.StartsWith("ACQUIRE|", StringComparison.Ordinal) &&
                         Enum.TryParse(action.Substring(8), out Sv5WorldGraphRole role))
                {
                    mask |= Bit(role);
                    cursor++;
                }
                else if (action == "FORGE|MAKE_SEAL") forge = true;
                else if (action == "SEAL|OPEN") seal = true;
                else if (action == "BOSS|PLANNED_COMPLETION_EVENT") boss = true;
                trace.Add(new Sv5RouteStateTraceStep(proof.ProofId, index, action, before,
                    TraceState(position, mask, cursor, forge, seal, boss), true));
            }
            return new Sv5RouteOrderProof(proof, trace);
        }

        private static bool AnchorIsKnown(Sv5RouteStateAnchor anchor, ISet<string> graphNodeIds, ISet<string> generalIds)
        {
            if (anchor == null) return false;
            if (anchor.Kind == Sv5RouteStateAnchorKind.ExistingGraphNode) return graphNodeIds.Contains(anchor.Id);
            if (anchor.Kind != Sv5RouteStateAnchorKind.GeneralConnection || graphNodeIds.Contains(anchor.Id)) return false;
            generalIds.Add(anchor.Id);
            return true;
        }

        private static Sv5WorldGraphEdge ToEdge(Sv5RouteShortcutCandidate candidate) => new Sv5WorldGraphEdge(
            candidate.From.Id, candidate.To.Id, candidate.Direction, "SV5_05_SHORTCUT_" + candidate.Id,
            "SV5_05_CANDIDATE_" + candidate.Id, true, candidate.RequiredResourceMask,
            candidate.RequiresForge, candidate.RequiresSeal, candidate.RequiresBossComplete);

        private static Sv5SpecialAccess TargetAccess(Sv5CoreReservationPlan corePlan, Sv5WorldGraphPlan graph,
            string targetNodeId)
        {
            Sv5CoreRouteReservation route = corePlan.Routes.FirstOrDefault(value => value.Source.TargetNodeId == targetNodeId);
            if (route == null) throw new ArgumentException("No physical access binds the target SV5 node.", nameof(targetNodeId));
            return corePlan.Source.Accesses.Single(value => value.Id == route.ToPortId);
        }

        private static PortRequirement ParsePortRequirement(string condition)
        {
            if (condition == "NONE" || condition == "OPTIONAL_VILLAGE") return new PortRequirement(true, 0, false, false, false);
            if (condition == "ALL_RESOURCES") return new PortRequirement(true, AllResources, false, false, false);
            if (condition == "ALL_RESOURCES_AND_FORGE") return new PortRequirement(true, AllResources, true, false, false);
            if (condition == "BOSS_COMPLETE") return new PortRequirement(true, 0, false, false, true);
            return new PortRequirement(false, 0, false, false, false);
        }

        private static bool HasRequirement(Sv5RouteShortcutCandidate candidate, PortRequirement requirement) =>
            (candidate.RequiredResourceMask & requirement.ResourceMask) == requirement.ResourceMask &&
            (!requirement.RequiresForge || candidate.RequiresForge) &&
            (!requirement.RequiresSeal || candidate.RequiresSeal) &&
            (!requirement.RequiresBoss || candidate.RequiresBossComplete);

        private static PortRequirement LogicalEntryRequirement(Sv5WorldGraphRole role) => role == Sv5WorldGraphRole.Boss
            ? new PortRequirement(true, AllResources, true, true, false) : role == Sv5WorldGraphRole.Exit
            ? new PortRequirement(true, AllResources, true, true, true) : new PortRequirement(true, 0, false, false, false);

        private static string DescribeMissingRequirement(Sv5RouteShortcutCandidate candidate, PortRequirement requirement)
        {
            var missing = new List<string>();
            if ((candidate.RequiredResourceMask & requirement.ResourceMask) != requirement.ResourceMask)
                missing.Add("AllResources");
            if (requirement.RequiresForge && !candidate.RequiresForge) missing.Add("ForgeMade");
            if (requirement.RequiresSeal && !candidate.RequiresSeal) missing.Add("SealOpen");
            if (requirement.RequiresBoss && !candidate.RequiresBossComplete) missing.Add("BossComplete");
            return string.Join("+", missing);
        }

        private static bool IsKnownPortCondition(string condition) => ParsePortRequirement(condition).IsKnown;
        private static bool ContainsNode(string portNodeIds, string nodeId) => (portNodeIds ?? string.Empty)
            .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Contains(nodeId, StringComparer.Ordinal);
        private static bool IsAccessEndpoint(Sv5CoreReservationPlan plan, Sv5SpecialWorldPoint point, string accessId) =>
            plan.Source.Accesses.Single(value => value.Id == accessId).OpenCells.Contains(point);
        private static bool IsContiguous(IReadOnlyList<Sv5SpecialWorldPoint> points) => points.Count >= 2 &&
            points.Skip(1).Select((value, index) => Math.Abs(value.X - points[index].X) + Math.Abs(value.Y - points[index].Y))
                .All(value => value == 1);
        private static IEnumerable<Sv5SpecialWorldPoint> Neighbors(Sv5SpecialWorldPoint point)
        {
            yield return new Sv5SpecialWorldPoint(point.X - 1, point.Y);
            yield return new Sv5SpecialWorldPoint(point.X + 1, point.Y);
            yield return new Sv5SpecialWorldPoint(point.X, point.Y - 1);
            yield return new Sv5SpecialWorldPoint(point.X, point.Y + 1);
        }

        private static void AddContact(IDictionary<string, Sv5RouteContactCheck> output, Sv5RouteContactPair pair,
            IReadOnlyDictionary<string, Sv5RouteConditionBinding> bindings)
        {
            string[] ids = { pair.RouteA, pair.RouteB };
            bool guarded = ids.Select(value => bindings[value].Edge).Any(edge => edge.RequiredResourceMask != 0 ||
                edge.RequiresForge || edge.RequiresSeal || edge.RequiresBossComplete);
            output[pair.Id] = new Sv5RouteContactCheck(pair.Id, pair.Kind, pair.FirstWorld, ids,
                guarded ? "LOGICAL_GUARD_PRESENT_GEOMETRY_PENDING" : "SAME_STAGE_MERGE_GEOMETRY_PENDING",
                guarded ? "Pair was completely enumerated; its SV5 predicates are classified but the mid-route switch is not yet evaluated."
                    : "Pair was completely enumerated; a split-node transition still requires the shared FSM evaluation.",
                RequiredPredicate(ids, bindings), false);
        }

        private static string RequiredPredicate(IEnumerable<string> routeIds,
            IReadOnlyDictionary<string, Sv5RouteConditionBinding> bindings) => string.Join(" && ", (routeIds ??
                Array.Empty<string>()).OrderBy(value => value, StringComparer.Ordinal).Select(value =>
                value + ":" + bindings[value].Edge.RequiredResourceMask.ToString(CultureInfo.InvariantCulture) + "/" +
                (bindings[value].Edge.RequiresForge ? "ForgeMade" : "-") + "/" +
                (bindings[value].Edge.RequiresSeal ? "SealOpen" : "-") + "/" +
                (bindings[value].Edge.RequiresBossComplete ? "BossComplete" : "-")));

        private static string ContactId(string kind, Sv5SpecialWorldPoint point, IEnumerable<string> routeIds) => kind + "_" +
            point.X.ToString(CultureInfo.InvariantCulture) + "_" + point.Y.ToString(CultureInfo.InvariantCulture) + "_" +
            Sv5WorldDefinition.Hash(string.Join("|", routeIds.OrderBy(value => value, StringComparer.Ordinal))).Substring(0, 12);
        private static string TraceState(string position, ulong mask, int cursor, bool forge, bool seal, bool boss) =>
            "SV5_TRACE_SV513|" + position + "|" + mask.ToString(CultureInfo.InvariantCulture) + "|" +
            cursor.ToString(CultureInfo.InvariantCulture) + "|" + (forge ? "1" : "0") + "|" +
            (seal ? "1" : "0") + "|" + (boss ? "1" : "0");
        private static ulong Bit(Sv5WorldGraphRole role) => role == Sv5WorldGraphRole.MooncoreOre ? 1UL :
            role == Sv5WorldGraphRole.CondensedCoefficientSap ? 2UL : role == Sv5WorldGraphRole.DeepStarYeast ? 4UL : 0UL;
        private static IEnumerable<Sv5WorldGraphRole[]> ResourceOrders() => Resources.SelectMany(first =>
            Resources.Where(second => second != first).SelectMany(second => Resources.Where(third => third != first &&
                third != second).Select(third => new[] { first, second, third })));

        private sealed class ShortcutEvaluation
        {
            public ShortcutEvaluation(IEnumerable<Sv5RouteShortcutCandidate> candidates,
                IEnumerable<Sv5RouteShortcutDecision> decisions, IEnumerable<Sv5RouteOrderProof> candidateSetProofs)
            {
                Candidates = (candidates ?? Array.Empty<Sv5RouteShortcutCandidate>()).ToArray();
                Decisions = (decisions ?? Array.Empty<Sv5RouteShortcutDecision>()).ToArray();
                CandidateSetProofs = (candidateSetProofs ?? Array.Empty<Sv5RouteOrderProof>()).ToArray();
            }

            public IReadOnlyList<Sv5RouteShortcutCandidate> Candidates { get; }
            public IReadOnlyList<Sv5RouteShortcutDecision> Decisions { get; }
            public IReadOnlyList<Sv5RouteOrderProof> CandidateSetProofs { get; }
        }

        private sealed class EntryCounterexample
        {
            public EntryCounterexample(Sv5WorldGraphState before, Sv5WorldGraphState after, string action,
                IReadOnlyList<string> prefix)
            { Before = before; After = after; Action = action; Prefix = prefix ?? Array.Empty<string>(); }
            public Sv5WorldGraphState Before { get; }
            public Sv5WorldGraphState After { get; }
            public string Action { get; }
            public IReadOnlyList<string> Prefix { get; }
        }

        private sealed class PortRequirement
        {
            public PortRequirement(bool known, ulong resourceMask, bool forge, bool seal, bool boss)
            { IsKnown = known; ResourceMask = resourceMask; RequiresForge = forge; RequiresSeal = seal; RequiresBoss = boss; }
            public bool IsKnown { get; }
            public ulong ResourceMask { get; }
            public bool RequiresForge { get; }
            public bool RequiresSeal { get; }
            public bool RequiresBoss { get; }
        }
    }

    public static class Sv5RouteStateExport
    {
        public static string ConditionBindingsCsv(Sv5RouteStateAnalysis analysis) => Lines(
            "route_id,edge_id,route_label,source_node_id,source_role,target_node_id,target_role,from_port_id,from_flow,from_condition,to_port_id,to_flow,to_condition,required_resource_mask,requires_forge,requires_seal,requires_boss_complete,geometry_state,player_state",
            Require(analysis).ConditionBindings.Select(value => Row(value.Route.RouteId, value.Edge.EdgeId,
                value.Route.Condition, value.Edge.SourceNodeId, value.SourceNode.Role, value.Edge.TargetNodeId,
                value.TargetNode.Role, value.FromAccess.Id, value.FromAccess.Flow, value.FromAccess.Condition,
                value.ToAccess.Id, value.ToAccess.Flow, value.ToAccess.Condition, value.Edge.RequiredResourceMask,
                value.Edge.RequiresForge, value.Edge.RequiresSeal, value.Edge.RequiresBossComplete,
                "PENDING", "PENDING")));

        public static string OrderProofsCsv(Sv5RouteStateAnalysis analysis) => Lines(
            "proof_id,resource_order,success,acquire_count,normal_return_count,forge_action,seal_action,boss_action,final_state",
            Require(analysis).OrderProofs.Select(value => Row(value.Source.ProofId,
                string.Join(">", value.Source.RequestedOrder), value.Success,
                value.Source.Actions.Count(action => action.StartsWith("ACQUIRE|", StringComparison.Ordinal)),
                NormalReturnCount(analysis, value.Source), value.Source.Actions.Contains("FORGE|MAKE_SEAL"),
                value.Source.Actions.Contains("SEAL|OPEN"), value.Source.Actions.Contains("BOSS|PLANNED_COMPLETION_EVENT"),
                value.Source.FinalState == null ? "" : value.Source.FinalState.StableToken)));

        public static string CandidateSetProofsCsv(Sv5RouteStateAnalysis analysis) => Lines(
            "proof_id,resource_order,success,action_count,final_state,proof_scope",
            Require(analysis).CandidateSetProofs.Select(value => Row(value.Source.ProofId,
                string.Join(">", value.Source.RequestedOrder), value.Success, value.Source.Actions.Count,
                value.Source.FinalState == null ? "" : value.Source.FinalState.StableToken, "CANDIDATE_SET_AUGMENTED")));

        public static string StateTracesCsv(Sv5RouteStateAnalysis analysis) => Lines(
            "proof_id,ordinal,action,state_before,state_after,predicate_passed",
            Require(analysis).OrderProofs.SelectMany(value => value.Trace).Select(value => Row(value.ProofId,
                value.Ordinal, value.Action, value.StateBefore, value.StateAfter, value.PredicatePassed)));

        public static string ShortcutDecisionsCsv(Sv5RouteStateAnalysis analysis) => Lines(
            "candidate_id,logical_allowed,decision_code,detail,counterexample_before,counterexample_after,counterexample_action,counterexample_prefix,geometry_state,player_state",
            Require(analysis).ShortcutDecisions.Select(value => Row(value.Id, value.IsAllowed, value.Code,
                value.Detail, value.CounterexampleBefore == null ? "" : value.CounterexampleBefore.StableToken,
                value.CounterexampleAfter == null ? "" : value.CounterexampleAfter.StableToken,
                value.CounterexampleAction, string.Join("|", value.CounterexamplePrefix), value.GeometryReadiness, "PENDING")));

        public static string ContactChecksCsv(Sv5RouteStateAnalysis analysis) => Lines(
            "contact_id,kind,world_x,world_y,route_ids,classification,required_predicate,logical_state_transition_checked,detail,geometry_state,player_state",
            Require(analysis).ContactChecks.Select(value => Row(value.Id, value.Kind, value.World.X, value.World.Y,
                string.Join("|", value.RouteIds), value.Classification, value.RequiredPredicate,
                value.LogicalStateTransitionChecked, value.Detail, value.GeometryReadiness, "PENDING")));

        public static string ObligationsCsv(Sv5RouteStateAnalysis analysis) => Lines(
            "issue_id,owner_task,requirement,readiness",
            Require(analysis).Obligations.Select(value => Row(value.Id, value.OwnerTask, value.Requirement, value.Readiness)));

        public static string ManifestJson(Sv5RouteStateAnalysis analysis)
        {
            Sv5RouteStateAnalysis value = Require(analysis);
            return "{\n" +
                "  \"format\": \"SV5_05_ROUTE_STATE_FIX01_V2\",\n" +
                "  \"core_reservation_digest\": \"" + value.CorePlan.Digest + "\",\n" +
                "  \"sv5_graph_digest\": \"" + value.CorePlan.RouteSource.Graph.Digest + "\",\n" +
                "  \"analysis_digest\": \"" + value.Digest + "\",\n" +
                "  \"route_count\": " + value.ConditionBindings.Count.ToString(CultureInfo.InvariantCulture) + ",\n" +
                "  \"resource_order_count\": " + value.OrderProofs.Count.ToString(CultureInfo.InvariantCulture) + ",\n" +
                "  \"candidate_set_proof_count\": " + value.CandidateSetProofs.Count.ToString(CultureInfo.InvariantCulture) + ",\n" +
                "  \"candidate_count\": " + value.Candidates.Count.ToString(CultureInfo.InvariantCulture) + ",\n" +
                "  \"logical_state_verified\": " + Json(value.LogicalStateVerified) + ",\n" +
                "  \"contact_state_verified\": " + Json(value.ContactStateVerified) + ",\n" +
                "  \"geometry_state_ready\": false,\n" +
                "  \"player_verified\": false,\n" +
                "  \"physical_readiness\": \"PENDING_SV5_06_SV5_09_SV5_41_SV5_44\",\n" +
                "  \"consumer\": \"Sv5RouteStatePolicy.Analyze\",\n" +
                "  \"full_world_bake\": \"NOT_RUN\"\n" +
                "}\n";
        }

        public static string AnalysisJson(Sv5RouteStateAnalysis analysis)
        {
            Sv5RouteStateAnalysis value = Require(analysis);
            return "{\n" +
                "  \"format\": \"SV5_05_FIX01_ANALYSIS_V2\",\n" +
                "  \"analysis_digest\": " + JsonText(value.Digest) + ",\n" +
                "  \"core_reservation_digest\": " + JsonText(value.CorePlan.Digest) + ",\n" +
                "  \"sv5_graph_digest\": " + JsonText(value.CorePlan.RouteSource.Graph.Digest) + ",\n" +
                "  \"candidates\": [" + string.Join(",", value.Candidates.Select(CandidateJson)) + "],\n" +
                "  \"review_contacts\": [" + string.Join(",", value.Review.Contacts.Select(ContactInputJson)) + "],\n" +
                "  \"air_witness\": [" + string.Join(",", value.Review.AirWitness.Select(PointJson)) + "],\n" +
                "  \"baseline_proofs\": [" + string.Join(",", value.OrderProofs.Select(value2 => ProofJson("BASELINE", value2))) + "],\n" +
                "  \"candidate_set_proofs\": [" + string.Join(",", value.CandidateSetProofs.Select(value2 => ProofJson("CANDIDATE_SET", value2))) + "],\n" +
                "  \"shortcut_decisions\": [" + string.Join(",", value.ShortcutDecisions.Select(DecisionJson)) + "],\n" +
                "  \"contact_checks\": [" + string.Join(",", value.ContactChecks.Select(ContactCheckJson)) + "],\n" +
                "  \"readiness\": {\"logical_state_verified\": " + Json(value.LogicalStateVerified) +
                    ", \"contact_state_verified\": " + Json(value.ContactStateVerified) +
                    ", \"geometry_state_ready\": false, \"player_verified\": false}\n" +
                "}\n";
        }

        private static Sv5RouteStateAnalysis Require(Sv5RouteStateAnalysis analysis)
        {
            if (analysis == null) throw new ArgumentNullException(nameof(analysis));
            return analysis;
        }
        private static string Lines(string header, IEnumerable<string> rows) => header + "\n" +
            string.Join("\n", rows ?? Array.Empty<string>()) + "\n";
        private static string Row(params object[] values) => string.Join(",", (values ?? Array.Empty<object>()).Select(value =>
            "\"" + (Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty).Replace("\"", "\"\"") + "\""));
        private static string Json(bool value) => value ? "true" : "false";
        private static string JsonText(string value) => "\"" + (value ?? string.Empty).Replace("\\", "\\\\")
            .Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\"";
        private static string CandidateJson(Sv5RouteShortcutCandidate value) => "{\"id\":" + JsonText(value.Id) +
            ",\"from\":{\"kind\":" + JsonText(value.From.Kind.ToString()) + ",\"id\":" + JsonText(value.From.Id) +
            "},\"to\":{\"kind\":" + JsonText(value.To.Kind.ToString()) + ",\"id\":" + JsonText(value.To.Id) +
            "},\"direction\":" + JsonText(value.Direction.ToString()) + ",\"required_resource_mask\":" +
            JsonText(value.RequiredResourceMask.ToString(CultureInfo.InvariantCulture)) + ",\"requires_forge\":" +
            Json(value.RequiresForge) + ",\"requires_seal\":" + Json(value.RequiresSeal) + ",\"requires_boss_complete\":" +
            Json(value.RequiresBossComplete) + ",\"source_provenance\":" + JsonText(value.SourceProvenance) +
            ",\"canonical_payload\":" + JsonText(value.CanonicalPayload) + "}";
        private static string ContactInputJson(Sv5RouteStateReviewContact value) => "{\"world\":" + PointJson(value.World) +
            ",\"route_ids\":[" + string.Join(",", value.RouteIds.Select(JsonText)) + "]}";
        private static string PointJson(Sv5SpecialWorldPoint value) => "[" + value.X.ToString(CultureInfo.InvariantCulture) +
            "," + value.Y.ToString(CultureInfo.InvariantCulture) + "]";
        private static string ProofJson(string scope, Sv5RouteOrderProof value) => "{\"scope\":" + JsonText(scope) +
            ",\"proof_id\":" + JsonText(value.Source.ProofId) + ",\"order\":[" +
            string.Join(",", value.Source.RequestedOrder.Select(role => JsonText(role.ToString()))) + "],\"success\":" +
            Json(value.Success) + ",\"actions\":[" + string.Join(",", value.Source.Actions.Select(JsonText)) +
            "],\"trace\":[" + string.Join(",", value.Trace.Select(step => "{\"ordinal\":" +
            step.Ordinal.ToString(CultureInfo.InvariantCulture) + ",\"action\":" + JsonText(step.Action) +
            ",\"before\":" + JsonText(step.StateBefore) + ",\"after\":" + JsonText(step.StateAfter) +
            ",\"predicate_passed\":" + Json(step.PredicatePassed) + "}")) + "]}";
        private static string DecisionJson(Sv5RouteShortcutDecision value) => "{\"id\":" + JsonText(value.Id) +
            ",\"code\":" + JsonText(value.Code.ToString()) + ",\"allowed\":" + Json(value.IsAllowed) +
            ",\"detail\":" + JsonText(value.Detail) + ",\"counterexample_before\":" +
            JsonText(value.CounterexampleBefore == null ? "" : value.CounterexampleBefore.StableToken) +
            ",\"counterexample_after\":" + JsonText(value.CounterexampleAfter == null ? "" : value.CounterexampleAfter.StableToken) +
            ",\"counterexample_action\":" + JsonText(value.CounterexampleAction) + ",\"counterexample_prefix\":[" +
            string.Join(",", value.CounterexamplePrefix.Select(JsonText)) + "]}";
        private static string ContactCheckJson(Sv5RouteContactCheck value) => "{\"id\":" + JsonText(value.Id) +
            ",\"kind\":" + JsonText(value.Kind) + ",\"world\":" + PointJson(value.World) + ",\"route_ids\":[" +
            string.Join(",", value.RouteIds.Select(JsonText)) + "],\"classification\":" + JsonText(value.Classification) +
            ",\"required_predicate\":" + JsonText(value.RequiredPredicate) + ",\"logical_state_transition_checked\":" +
            Json(value.LogicalStateTransitionChecked) + ",\"geometry_state\":\"PENDING\",\"player_state\":\"PENDING\"}";
        private static int NormalReturnCount(Sv5RouteStateAnalysis analysis, Sv5WorldGraphProof proof)
        {
            var conditions = Require(analysis).ConditionBindings.ToDictionary(value => value.Edge.EdgeId,
                value => value.Edge.TraversalCondition, StringComparer.Ordinal);
            return (proof.Actions ?? Array.Empty<string>()).Count(action => action.StartsWith("MOVE|", StringComparison.Ordinal) &&
                conditions.TryGetValue(action.Substring(5), out string condition) &&
                (condition == "NORMAL_RESOURCE_RETURN" || condition == "NORMAL_FORGE_RETURN"));
        }
    }
}
