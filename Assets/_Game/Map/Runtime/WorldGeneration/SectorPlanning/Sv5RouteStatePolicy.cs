using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.WorldData;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum Sv5RouteStateAnchorKind { ExistingGraphNode = 1, GeneralConnection = 2 }
    public enum Sv5RouteStateReadiness { Verified = 1, Pending = 2 }
    public enum Sv5RouteShortcutDecisionCode
    {
        Allowed = 1,
        DuplicateId = 2,
        UnknownAnchor = 3,
        InvalidGeneralAnchor = 4,
        WeakRequiredCondition = 5,
        ReverseOfOneWay = 6,
        CandidateSetGoalUnreachable = 7,
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
            RmapWorldGraphDirection direction, ulong requiredResourceMask, bool requiresForge,
            bool requiresSeal, bool requiresBossComplete, string sourceProvenance)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A candidate ID is required.", nameof(id));
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));
            if (!Enum.IsDefined(typeof(RmapWorldGraphDirection), direction)) throw new ArgumentOutOfRangeException(nameof(direction));
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
        public RmapWorldGraphDirection Direction { get; }
        public ulong RequiredResourceMask { get; }
        public bool RequiresForge { get; }
        public bool RequiresSeal { get; }
        public bool RequiresBossComplete { get; }
        public string SourceProvenance { get; }
    }

    public sealed class Sv5RouteStateReviewInput
    {
        private readonly ReadOnlyCollection<Sv5RouteStateReviewContact> contacts;
        private readonly ReadOnlyCollection<RmapSpecialWorldPoint> airWitness;

        public Sv5RouteStateReviewInput(IEnumerable<Sv5RouteStateReviewContact> sourceContacts,
            IEnumerable<RmapSpecialWorldPoint> sourceAirWitness)
        {
            contacts = new ReadOnlyCollection<Sv5RouteStateReviewContact>((sourceContacts ??
                Array.Empty<Sv5RouteStateReviewContact>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
            airWitness = new ReadOnlyCollection<RmapSpecialWorldPoint>((sourceAirWitness ??
                Array.Empty<RmapSpecialWorldPoint>()).ToArray());
        }

        public IReadOnlyList<Sv5RouteStateReviewContact> Contacts => contacts;
        public IReadOnlyList<RmapSpecialWorldPoint> AirWitness => airWitness;
    }

    public sealed class Sv5RouteStateReviewContact : IComparable<Sv5RouteStateReviewContact>
    {
        private readonly ReadOnlyCollection<string> routeIds;

        public Sv5RouteStateReviewContact(RmapSpecialWorldPoint world, IEnumerable<string> sourceRouteIds)
        {
            World = world;
            routeIds = new ReadOnlyCollection<string>((sourceRouteIds ?? Array.Empty<string>()).Where(value =>
                !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
            if (routeIds.Count < 2) throw new ArgumentException("A review contact needs two route IDs.", nameof(sourceRouteIds));
        }

        public RmapSpecialWorldPoint World { get; }
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
        internal Sv5RouteConditionBinding(Sv5CoreRouteReservation route, RmapWorldGraphEdge edge,
            RmapWorldGraphNode sourceNode, RmapWorldGraphNode targetNode, RmapSpecialAccess fromAccess,
            RmapSpecialAccess toAccess)
        {
            Route = route;
            Edge = edge;
            SourceNode = sourceNode;
            TargetNode = targetNode;
            FromAccess = fromAccess;
            ToAccess = toAccess;
        }

        public Sv5CoreRouteReservation Route { get; }
        public RmapWorldGraphEdge Edge { get; }
        public RmapWorldGraphNode SourceNode { get; }
        public RmapWorldGraphNode TargetNode { get; }
        public RmapSpecialAccess FromAccess { get; }
        public RmapSpecialAccess ToAccess { get; }
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

        internal Sv5RouteOrderProof(RmapWorldGraphProof source, IEnumerable<Sv5RouteStateTraceStep> sourceTrace)
        {
            Source = source;
            trace = new ReadOnlyCollection<Sv5RouteStateTraceStep>((sourceTrace ?? Array.Empty<Sv5RouteStateTraceStep>())
                .OrderBy(value => value).ToArray());
        }

        public RmapWorldGraphProof Source { get; }
        public IReadOnlyList<Sv5RouteStateTraceStep> Trace => trace;
        public bool Success => Source != null && Source.Success;
        public int CompareTo(Sv5RouteOrderProof other) => other == null ? 1 :
            string.Compare(Source.ProofId, other.Source.ProofId, StringComparison.Ordinal);
    }

    public sealed class Sv5RouteShortcutDecision : IComparable<Sv5RouteShortcutDecision>
    {
        internal Sv5RouteShortcutDecision(string id, Sv5RouteShortcutDecisionCode code, string detail)
        {
            Id = id ?? string.Empty;
            Code = code;
            Detail = detail ?? string.Empty;
        }

        public string Id { get; }
        public Sv5RouteShortcutDecisionCode Code { get; }
        public string Detail { get; }
        public bool IsAllowed => Code == Sv5RouteShortcutDecisionCode.Allowed;
        public Sv5RouteStateReadiness GeometryReadiness => Sv5RouteStateReadiness.Pending;
        public int CompareTo(Sv5RouteShortcutDecision other) => other == null ? 1 :
            string.Compare(Id, other.Id, StringComparison.Ordinal);
    }

    public sealed class Sv5RouteContactCheck : IComparable<Sv5RouteContactCheck>
    {
        private readonly ReadOnlyCollection<string> routeIds;

        internal Sv5RouteContactCheck(string id, string kind, RmapSpecialWorldPoint world,
            IEnumerable<string> sourceRouteIds, string classification, string detail)
        {
            Id = id;
            Kind = kind;
            World = world;
            routeIds = new ReadOnlyCollection<string>((sourceRouteIds ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
            Classification = classification;
            Detail = detail;
        }

        public string Id { get; }
        public string Kind { get; }
        public RmapSpecialWorldPoint World { get; }
        public IReadOnlyList<string> RouteIds => routeIds;
        public string Classification { get; }
        public string Detail { get; }
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
        private readonly ReadOnlyCollection<Sv5RouteShortcutDecision> shortcuts;
        private readonly ReadOnlyCollection<Sv5RouteContactCheck> contacts;
        private readonly ReadOnlyCollection<Sv5RouteStateObligation> obligations;

        internal Sv5RouteStateAnalysis(Sv5CoreReservationPlan corePlan,
            IEnumerable<Sv5RouteConditionBinding> sourceBindings, IEnumerable<Sv5RouteOrderProof> sourceProofs,
            IEnumerable<Sv5RouteShortcutDecision> sourceShortcuts, IEnumerable<Sv5RouteContactCheck> sourceContacts,
            IEnumerable<Sv5RouteStateObligation> sourceObligations)
        {
            CorePlan = corePlan;
            bindings = Freeze(sourceBindings);
            proofs = Freeze(sourceProofs);
            shortcuts = Freeze(sourceShortcuts);
            contacts = Freeze(sourceContacts);
            obligations = Freeze(sourceObligations);
            LogicalStateVerified = bindings.Count == 11 && proofs.Count == 6 && proofs.All(value => value.Success) &&
                shortcuts.All(value => value.IsAllowed || !string.Equals(value.Id, "CANDIDATE_SET", StringComparison.Ordinal));
            GeometryStateReady = false;
            PlayerVerified = false;
            Digest = RmapWorldDefinition.Hash(string.Join("\n", CanonicalLines()));
        }

        public Sv5CoreReservationPlan CorePlan { get; }
        public IReadOnlyList<Sv5RouteConditionBinding> ConditionBindings => bindings;
        public IReadOnlyList<Sv5RouteOrderProof> OrderProofs => proofs;
        public IReadOnlyList<Sv5RouteShortcutDecision> ShortcutDecisions => shortcuts;
        public IReadOnlyList<Sv5RouteContactCheck> ContactChecks => contacts;
        public IReadOnlyList<Sv5RouteStateObligation> Obligations => obligations;
        public bool LogicalStateVerified { get; }
        public bool GeometryStateReady { get; }
        public bool PlayerVerified { get; }
        public string Digest { get; }

        private IEnumerable<string> CanonicalLines()
        {
            yield return "SV5_ROUTE_STATE_ANALYSIS_V1";
            yield return CorePlan.Digest;
            yield return CorePlan.RouteSource.Graph.Digest;
            foreach (Sv5RouteConditionBinding value in bindings)
                yield return "binding|" + value.Route.RouteId + "|" + value.Edge.EdgeId + "|" + value.ToAccess.Condition;
            foreach (Sv5RouteOrderProof value in proofs)
                yield return "proof|" + value.Source.ProofId + "|" + value.Success;
            foreach (Sv5RouteShortcutDecision value in shortcuts)
                yield return "shortcut|" + value.Id + "|" + value.Code + "|" + value.Detail;
            foreach (Sv5RouteContactCheck value in contacts)
                yield return "contact|" + value.Id + "|" + value.Classification;
            foreach (Sv5RouteStateObligation value in obligations)
                yield return "obligation|" + value.Id + "|" + value.OwnerTask + "|" + value.Readiness;
        }

        private static ReadOnlyCollection<T> Freeze<T>(IEnumerable<T> values) where T : IComparable<T> =>
            new ReadOnlyCollection<T>((values ?? Array.Empty<T>()).Where(value => value != null).OrderBy(value => value).ToArray());
    }

    public static class Sv5RouteStatePolicy
    {
        private const ulong AllResources = 7;
        private static readonly RmapWorldGraphRole[] Resources =
        {
            RmapWorldGraphRole.MooncoreOre,
            RmapWorldGraphRole.CondensedCoefficientSap,
            RmapWorldGraphRole.DeepStarYeast,
        };

        public static Sv5RouteStateAnalysis Analyze(Sv5CoreReservationPlan corePlan,
            IEnumerable<Sv5RouteShortcutCandidate> sourceCandidates, Sv5RouteStateReviewInput review)
        {
            if (corePlan == null) throw new ArgumentNullException(nameof(corePlan));
            if (review == null) throw new ArgumentNullException(nameof(review));
            RmapWorldGraphPlan graph = corePlan.RouteSource.Graph;
            if (graph == null || !graph.Success) throw new ArgumentException("A passing RMAP13 graph is required.", nameof(corePlan));
            if (corePlan.Routes.Count != 11 || graph.Edges.Count != corePlan.Routes.Count)
                throw new ArgumentException("SV5_05 requires the current eleven-route source.", nameof(corePlan));

            Sv5RouteConditionBinding[] bindings = Bind(corePlan, graph).ToArray();
            Sv5RouteOrderProof[] proofs = ResourceOrders().Select(order => BuildProof(graph.Nodes, graph.Edges, order)).ToArray();
            var shortcutResult = EvaluateShortcuts(corePlan, graph, sourceCandidates);
            Sv5RouteContactCheck[] contacts = CollectContacts(corePlan, bindings, review).ToArray();
            Sv5RouteStateObligation[] obligations = BuildObligations(contacts).ToArray();
            return new Sv5RouteStateAnalysis(corePlan, bindings, proofs,
                shortcutResult, contacts, obligations);
        }

        public static RmapWorldGraphPlan EvaluateReturnPolicy(Sv5CoreReservationPlan corePlan,
            RmapWorldReturnShortcutPolicy policy)
        {
            if (corePlan == null) throw new ArgumentNullException(nameof(corePlan));
            return RmapWorldGraphPlanner.Plan(corePlan.RouteSource.Definition, policy);
        }

        private static IEnumerable<Sv5RouteConditionBinding> Bind(Sv5CoreReservationPlan corePlan,
            RmapWorldGraphPlan graph)
        {
            var nodes = graph.Nodes.ToDictionary(value => value.NodeId, value => value, StringComparer.Ordinal);
            var edges = graph.Edges.ToDictionary(value => value.EdgeId, value => value, StringComparer.Ordinal);
            foreach (Sv5CoreRouteReservation route in corePlan.Routes.OrderBy(value => value))
            {
                if (!edges.TryGetValue(route.RouteId, out RmapWorldGraphEdge edge))
                    throw new ArgumentException("Unknown RMAP13 edge for route " + route.RouteId + ".", nameof(corePlan));
                if (!nodes.TryGetValue(edge.SourceNodeId, out RmapWorldGraphNode sourceNode) ||
                    !nodes.TryGetValue(edge.TargetNodeId, out RmapWorldGraphNode targetNode))
                    throw new ArgumentException("Route edge has an unknown RMAP13 node.", nameof(corePlan));
                if (!string.Equals(route.Condition, edge.TraversalCondition, StringComparison.Ordinal))
                    throw new ArgumentException("Route label does not equal its RMAP13 edge condition.", nameof(corePlan));
                RmapSpecialAccess from = corePlan.Source.Accesses.Single(value => value.Id == route.FromPortId);
                RmapSpecialAccess to = corePlan.Source.Accesses.Single(value => value.Id == route.ToPortId);
                if (!ContainsNode(from.SourceNodeId, edge.SourceNodeId) || !ContainsNode(to.SourceNodeId, edge.TargetNodeId))
                    throw new ArgumentException("Route port source-node binding is inconsistent.", nameof(corePlan));
                if (!IsKnownPortCondition(from.Condition) || !IsKnownPortCondition(to.Condition))
                    throw new ArgumentException("Unknown route port condition.", nameof(corePlan));
                yield return new Sv5RouteConditionBinding(route, edge, sourceNode, targetNode, from, to);
            }
        }

        private static IReadOnlyList<Sv5RouteShortcutDecision> EvaluateShortcuts(Sv5CoreReservationPlan corePlan,
            RmapWorldGraphPlan graph, IEnumerable<Sv5RouteShortcutCandidate> sourceCandidates)
        {
            Sv5RouteShortcutCandidate[] candidates = (sourceCandidates ?? Array.Empty<Sv5RouteShortcutCandidate>())
                .Where(value => value != null).OrderBy(value => value.Id, StringComparer.Ordinal).ToArray();
            var decisions = new List<Sv5RouteShortcutDecision>();
            var graphNodeIds = new HashSet<string>(graph.Nodes.Select(value => value.NodeId), StringComparer.Ordinal);
            var generalIds = new HashSet<string>(StringComparer.Ordinal);
            var accepted = new List<Sv5RouteShortcutCandidate>();
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
                return decisions;
            }

            RmapWorldGraphEdge[] candidateEdges = accepted.Select(ToEdge).ToArray();
            RmapWorldGraphProof[] proofs = ResourceOrders().Select(order =>
                RmapWorldGraphPlanner.EvaluateWithAnalysisNodes(graph.Nodes, generalIds,
                    graph.Edges.Concat(candidateEdges), order)).ToArray();
            if (proofs.Any(value => !value.Success))
            {
                RmapWorldGraphProof failure = proofs.First(value => !value.Success);
                decisions.Add(new Sv5RouteShortcutDecision("CANDIDATE_SET",
                    Sv5RouteShortcutDecisionCode.CandidateSetGoalUnreachable,
                    failure.Failures.First().Code + ":" + failure.Failures.First().Detail));
            }
            else
            {
                decisions.Add(new Sv5RouteShortcutDecision("CANDIDATE_SET",
                    Sv5RouteShortcutDecisionCode.Allowed,
                    "All six RMAP13 resource orders retain required actions and the exit goal."));
            }
            return decisions;
        }

        private static Sv5RouteShortcutDecision ValidateCandidate(Sv5CoreReservationPlan corePlan,
            RmapWorldGraphPlan graph, ISet<string> graphNodeIds, ISet<string> generalIds,
            Sv5RouteShortcutCandidate candidate)
        {
            if (!AnchorIsKnown(candidate.From, graphNodeIds, generalIds) ||
                !AnchorIsKnown(candidate.To, graphNodeIds, generalIds))
                return new Sv5RouteShortcutDecision(candidate.Id, Sv5RouteShortcutDecisionCode.UnknownAnchor,
                    "Candidate anchors must be existing RMAP13 nodes or declared general analysis nodes.");
            if (string.Equals(candidate.From.Id, candidate.To.Id, StringComparison.Ordinal))
                return new Sv5RouteShortcutDecision(candidate.Id, Sv5RouteShortcutDecisionCode.InvalidGeneralAnchor,
                    "A shortcut must connect two distinct anchors.");
            if (graphNodeIds.Contains(candidate.To.Id))
            {
                RmapSpecialAccess target = TargetAccess(corePlan, graph, candidate.To.Id);
                PortRequirement requirement = ParsePortRequirement(target.Condition);
                if (!requirement.IsKnown || !HasRequirement(candidate, requirement))
                    return new Sv5RouteShortcutDecision(candidate.Id, Sv5RouteShortcutDecisionCode.WeakRequiredCondition,
                        "Candidate does not preserve target port condition " + target.Condition + ".");
            }
            bool hasDirectEdge = graph.Edges.Any(edge => edge.SourceNodeId == candidate.From.Id &&
                edge.TargetNodeId == candidate.To.Id);
            bool reversesOneWay = graph.Edges.Any(edge => edge.SourceNodeId == candidate.To.Id &&
                edge.TargetNodeId == candidate.From.Id && edge.SourceConnectionIsOneWay);
            if (reversesOneWay && !hasDirectEdge)
                return new Sv5RouteShortcutDecision(candidate.Id, Sv5RouteShortcutDecisionCode.ReverseOfOneWay,
                    "A reverse of an existing one-way connection needs its own declared RMAP13 route.");
            return new Sv5RouteShortcutDecision(candidate.Id, Sv5RouteShortcutDecisionCode.Allowed,
                "Logical proposal only; geometry and Player evidence remain pending.");
        }

        private static IEnumerable<Sv5RouteContactCheck> CollectContacts(Sv5CoreReservationPlan corePlan,
            IEnumerable<Sv5RouteConditionBinding> sourceBindings, Sv5RouteStateReviewInput review)
        {
            var bindings = sourceBindings.ToDictionary(value => value.Route.RouteId, value => value, StringComparer.Ordinal);
            var byPoint = corePlan.RouteCells.Where(value => value.Kind == Sv5CoreRouteReservationKind.Passage ||
                    value.Kind == Sv5CoreRouteReservationKind.Clearance).GroupBy(value => value.World)
                .ToDictionary(value => value.Key, value => value.Select(item => item.RouteId).Distinct(StringComparer.Ordinal)
                    .OrderBy(item => item, StringComparer.Ordinal).ToArray());
            var output = new Dictionary<string, Sv5RouteContactCheck>(StringComparer.Ordinal);
            foreach (KeyValuePair<RmapSpecialWorldPoint, string[]> entry in byPoint.Where(value => value.Value.Length > 1))
                AddContact(output, "SHARED", entry.Key, entry.Value, bindings);
            foreach (KeyValuePair<RmapSpecialWorldPoint, string[]> entry in byPoint)
            {
                foreach (RmapSpecialWorldPoint neighbor in Neighbors(entry.Key))
                {
                    if (!byPoint.TryGetValue(neighbor, out string[] other)) continue;
                    if (entry.Key.CompareTo(neighbor) >= 0 || entry.Value.Intersect(other, StringComparer.Ordinal).Any()) continue;
                    string[] routes = entry.Value.Concat(other).Distinct(StringComparer.Ordinal).OrderBy(value => value,
                        StringComparer.Ordinal).ToArray();
                    if (routes.Select(value => bindings[value].Edge.TraversalCondition).Distinct(StringComparer.Ordinal).Count() > 1)
                        AddContact(output, "FACE", entry.Key, routes, bindings);
                }
            }
            foreach (Sv5RouteStateReviewContact reviewContact in review.Contacts)
            {
                bool routesExist = reviewContact.RouteIds.All(bindings.ContainsKey);
                string id = ContactId("REVIEW", reviewContact.World, reviewContact.RouteIds);
                output[id] = new Sv5RouteContactCheck(id, "REVIEW_LABEL_CONTACT", reviewContact.World,
                    reviewContact.RouteIds, routesExist ? "LOGICAL_GUARD_PRESENT_GEOMETRY_PENDING" : "UNKNOWN_ROUTE_REJECTED",
                    routesExist ? "Review label mismatch is classified from actual RMAP13 predicates, not the label alone."
                        : "Review input references an unknown route ID.");
            }
            if (review.AirWitness.Count != 0)
            {
                bool contiguous = IsContiguous(review.AirWitness);
                bool endpoints = IsAccessEndpoint(corePlan, review.AirWitness.First(), "RMAP15_SITE_START_PORT_ENTRY") &&
                    IsAccessEndpoint(corePlan, review.AirWitness.Last(), "RMAP15_SITE_EXIT_PORT_ENTRY");
                string id = "AIR_WITNESS_109_EDGE";
                output[id] = new Sv5RouteContactCheck(id, "RAW_AIR_WITNESS", review.AirWitness.First(),
                    Array.Empty<string>(), contiguous && endpoints ? "STATIC_AIR_CONTACT_GEOMETRY_PENDING" : "INVALID_WITNESS",
                    contiguous && endpoints ? "Raw 4-neighbour AIR witness is diagnostic only; it is not Player proof."
                        : "Review witness did not retain its recorded cardinal endpoints.");
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
            yield return new Sv5RouteStateObligation("SV5_41_PLAYER_GEOMETRY", "SV5_41_COMPOSE",
                "Recheck final geometry contacts and Player traversal before ready promotion.", "PENDING");
        }

        private static Sv5RouteOrderProof BuildProof(IEnumerable<RmapWorldGraphNode> nodes,
            IEnumerable<RmapWorldGraphEdge> edges, RmapWorldGraphRole[] order)
        {
            RmapWorldGraphProof proof = RmapWorldGraphPlanner.Evaluate(nodes, edges, order);
            var trace = new List<Sv5RouteStateTraceStep>();
            if (!proof.Success) return new Sv5RouteOrderProof(proof, trace);
            var graphNodes = nodes.ToDictionary(value => value.NodeId, value => value, StringComparer.Ordinal);
            var graphEdges = edges.ToDictionary(value => value.EdgeId, value => value, StringComparer.Ordinal);
            string position = graphNodes.Values.Single(value => value.Role == RmapWorldGraphRole.Start).NodeId;
            ulong mask = 0;
            var cursor = 0;
            var forge = false;
            var seal = false;
            var boss = false;
            for (var index = 0; index < proof.Actions.Count; index++)
            {
                string before = TraceState(position, mask, cursor, forge, seal, boss);
                string action = proof.Actions[index];
                if (action.StartsWith("MOVE|", StringComparison.Ordinal) && graphEdges.TryGetValue(action.Substring(5), out RmapWorldGraphEdge edge))
                    position = edge.TargetNodeId;
                else if (action.StartsWith("ACQUIRE|", StringComparison.Ordinal) &&
                         Enum.TryParse(action.Substring(8), out RmapWorldGraphRole role))
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

        private static RmapWorldGraphEdge ToEdge(Sv5RouteShortcutCandidate candidate) => new RmapWorldGraphEdge(
            candidate.From.Id, candidate.To.Id, candidate.Direction, "SV5_05_SHORTCUT_" + candidate.Id,
            "SV5_05_CANDIDATE_" + candidate.Id, true, candidate.RequiredResourceMask,
            candidate.RequiresForge, candidate.RequiresSeal, candidate.RequiresBossComplete);

        private static RmapSpecialAccess TargetAccess(Sv5CoreReservationPlan corePlan, RmapWorldGraphPlan graph,
            string targetNodeId)
        {
            Sv5CoreRouteReservation route = corePlan.Routes.FirstOrDefault(value => value.Source.TargetNodeId == targetNodeId);
            if (route == null) throw new ArgumentException("No physical access binds the target RMAP13 node.", nameof(targetNodeId));
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

        private static bool IsKnownPortCondition(string condition) => ParsePortRequirement(condition).IsKnown;
        private static bool ContainsNode(string portNodeIds, string nodeId) => (portNodeIds ?? string.Empty)
            .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Contains(nodeId, StringComparer.Ordinal);
        private static bool IsAccessEndpoint(Sv5CoreReservationPlan plan, RmapSpecialWorldPoint point, string accessId) =>
            plan.Source.Accesses.Single(value => value.Id == accessId).OpenCells.Contains(point);
        private static bool IsContiguous(IReadOnlyList<RmapSpecialWorldPoint> points) => points.Count >= 2 &&
            points.Skip(1).Select((value, index) => Math.Abs(value.X - points[index].X) + Math.Abs(value.Y - points[index].Y))
                .All(value => value == 1);
        private static IEnumerable<RmapSpecialWorldPoint> Neighbors(RmapSpecialWorldPoint point)
        {
            yield return new RmapSpecialWorldPoint(point.X - 1, point.Y);
            yield return new RmapSpecialWorldPoint(point.X + 1, point.Y);
            yield return new RmapSpecialWorldPoint(point.X, point.Y - 1);
            yield return new RmapSpecialWorldPoint(point.X, point.Y + 1);
        }

        private static void AddContact(IDictionary<string, Sv5RouteContactCheck> output, string kind,
            RmapSpecialWorldPoint point, IEnumerable<string> routeIds,
            IReadOnlyDictionary<string, Sv5RouteConditionBinding> bindings)
        {
            string[] ids = routeIds.OrderBy(value => value, StringComparer.Ordinal).ToArray();
            bool guarded = ids.Select(value => bindings[value].Edge).Any(edge => edge.RequiredResourceMask != 0 ||
                edge.RequiresForge || edge.RequiresSeal || edge.RequiresBossComplete);
            string id = ContactId(kind, point, ids);
            output[id] = new Sv5RouteContactCheck(id, kind, point, ids,
                guarded ? "LOGICAL_GUARD_PRESENT_GEOMETRY_PENDING" : "SAME_STAGE_MERGE_GEOMETRY_PENDING",
                guarded ? "Actual RMAP13 edge predicate remains logical-only until geometry adds the required guard."
                    : "Same-stage route merge has no Player or geometry proof.");
        }

        private static string ContactId(string kind, RmapSpecialWorldPoint point, IEnumerable<string> routeIds) => kind + "_" +
            point.X.ToString(CultureInfo.InvariantCulture) + "_" + point.Y.ToString(CultureInfo.InvariantCulture) + "_" +
            RmapWorldDefinition.Hash(string.Join("|", routeIds.OrderBy(value => value, StringComparer.Ordinal))).Substring(0, 12);
        private static string TraceState(string position, ulong mask, int cursor, bool forge, bool seal, bool boss) =>
            "SV5_TRACE_RMAP13|" + position + "|" + mask.ToString(CultureInfo.InvariantCulture) + "|" +
            cursor.ToString(CultureInfo.InvariantCulture) + "|" + (forge ? "1" : "0") + "|" +
            (seal ? "1" : "0") + "|" + (boss ? "1" : "0");
        private static ulong Bit(RmapWorldGraphRole role) => role == RmapWorldGraphRole.MooncoreOre ? 1UL :
            role == RmapWorldGraphRole.CondensedCoefficientSap ? 2UL : role == RmapWorldGraphRole.DeepStarYeast ? 4UL : 0UL;
        private static IEnumerable<RmapWorldGraphRole[]> ResourceOrders() => Resources.SelectMany(first =>
            Resources.Where(second => second != first).SelectMany(second => Resources.Where(third => third != first &&
                third != second).Select(third => new[] { first, second, third })));

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

        public static string StateTracesCsv(Sv5RouteStateAnalysis analysis) => Lines(
            "proof_id,ordinal,action,state_before,state_after,predicate_passed",
            Require(analysis).OrderProofs.SelectMany(value => value.Trace).Select(value => Row(value.ProofId,
                value.Ordinal, value.Action, value.StateBefore, value.StateAfter, value.PredicatePassed)));

        public static string ShortcutDecisionsCsv(Sv5RouteStateAnalysis analysis) => Lines(
            "candidate_id,logical_allowed,decision_code,detail,geometry_state,player_state",
            Require(analysis).ShortcutDecisions.Select(value => Row(value.Id, value.IsAllowed, value.Code,
                value.Detail, value.GeometryReadiness, "PENDING")));

        public static string ContactChecksCsv(Sv5RouteStateAnalysis analysis) => Lines(
            "contact_id,kind,world_x,world_y,route_ids,classification,detail,geometry_state,player_state",
            Require(analysis).ContactChecks.Select(value => Row(value.Id, value.Kind, value.World.X, value.World.Y,
                string.Join("|", value.RouteIds), value.Classification, value.Detail, value.GeometryReadiness, "PENDING")));

        public static string ObligationsCsv(Sv5RouteStateAnalysis analysis) => Lines(
            "issue_id,owner_task,requirement,readiness",
            Require(analysis).Obligations.Select(value => Row(value.Id, value.OwnerTask, value.Requirement, value.Readiness)));

        public static string ManifestJson(Sv5RouteStateAnalysis analysis)
        {
            Sv5RouteStateAnalysis value = Require(analysis);
            return "{\n" +
                "  \"format\": \"SV5_05_ROUTE_STATE_V1\",\n" +
                "  \"core_reservation_digest\": \"" + value.CorePlan.Digest + "\",\n" +
                "  \"rmap13_graph_digest\": \"" + value.CorePlan.RouteSource.Graph.Digest + "\",\n" +
                "  \"analysis_digest\": \"" + value.Digest + "\",\n" +
                "  \"route_count\": " + value.ConditionBindings.Count.ToString(CultureInfo.InvariantCulture) + ",\n" +
                "  \"resource_order_count\": " + value.OrderProofs.Count.ToString(CultureInfo.InvariantCulture) + ",\n" +
                "  \"logical_state_verified\": " + Json(value.LogicalStateVerified) + ",\n" +
                "  \"geometry_state_ready\": false,\n" +
                "  \"player_verified\": false,\n" +
                "  \"physical_readiness\": \"PENDING_SV5_06_SV5_09_SV5_41\",\n" +
                "  \"consumer\": \"Sv5RouteStatePolicy.Analyze\",\n" +
                "  \"full_world_bake\": \"NOT_RUN\"\n" +
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
        private static int NormalReturnCount(Sv5RouteStateAnalysis analysis, RmapWorldGraphProof proof)
        {
            var conditions = Require(analysis).ConditionBindings.ToDictionary(value => value.Edge.EdgeId,
                value => value.Edge.TraversalCondition, StringComparer.Ordinal);
            return (proof.Actions ?? Array.Empty<string>()).Count(action => action.StartsWith("MOVE|", StringComparison.Ordinal) &&
                conditions.TryGetValue(action.Substring(5), out string condition) &&
                (condition == "NORMAL_RESOURCE_RETURN" || condition == "NORMAL_FORGE_RETURN"));
        }
    }
}
