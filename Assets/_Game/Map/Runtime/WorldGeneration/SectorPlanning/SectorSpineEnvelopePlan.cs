using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Pipeline;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum SectorSpineNodeKind
    {
        ExternalSocket,
        BoundaryBridge,
        ClusterEntry,
        ClusterExit,
        SpecialEntry,
        SpecialReturn,
        RecoveryJoin,
        OptionalBranch,
    }

    public enum SectorSpineEdgeKind
    {
        MandatoryLow,
        MandatorySpecialConnector,
        BoundaryConnector,
        ClusterConnector,
        OptionalHigh,
        Recovery,
        Return,
    }

    public enum SectorTraversalEnvelopeCellKind
    {
        Centerline,
        Floor,
        Clearance,
        Landing,
        Recovery,
        ProtectedOpen,
        ProtectedAnchorBridge,
    }

    public enum SectorSpineEndpointRole
    {
        Entry,
        Exit,
        Junction,
        Return,
        Rejoin,
        Branch,
        Evidence,
    }

    public enum SectorSpineEnvelopeErrorCode
    {
        MissingInput,
        MissingAnchorPlan,
        MissingClusterPlacementPlan,
        SectorMismatch,
        MissingEndpoint,
        DuplicateNode,
        DuplicateEdge,
        NodeOutOfBounds,
        EdgeOutOfBounds,
        EdgeCrossesBlockingAnchor,
        EdgeCrossesUnplacedCluster,
        MissingMandatoryRoute,
        MissingRecoveryRoute,
        MissingSpecialConnector,
        EnvelopeOutOfBounds,
        EnvelopeOverlapsBlockingAnchor,
        EnvelopeMissingClearance,
        EnvelopeMissingLanding,
        ProtectedSetMismatch,
        RouteAccessMutationClaim,
        AnchorMutationClaim,
        ClusterMutationClaim,
        PatternMutationClaim,
        ActivityMutationClaim,
        SolverMutationClaim,
        RngMutationClaim,
        TileMutationClaim,
        CanvasMutationClaim,
        SceneMutationClaim,
        PhysicsMutationClaim,
        NonCanonicalPublication,
    }

    public sealed class SectorSpineEnvelopeError : IEquatable<SectorSpineEnvelopeError>, IComparable<SectorSpineEnvelopeError>
    {
        public SectorSpineEnvelopeError(SectorSpineEnvelopeErrorCode code, string subject, string detail)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public SectorSpineEnvelopeErrorCode Code { get; }
        public string Subject { get; }
        public string Detail { get; }

        public int CompareTo(SectorSpineEnvelopeError other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0 ? comparison : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(SectorSpineEnvelopeError other) => other != null && CompareTo(other) == 0;
        public override bool Equals(object obj) => Equals(obj as SectorSpineEnvelopeError);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(ToString());
        public override string ToString() => Code + "|" + Subject + "|" + Detail;
    }

    public sealed class SectorSpineNode : IComparable<SectorSpineNode>
    {
        internal SectorSpineNode(
            string nodeId,
            SectorCoord sectorCoordinate,
            int sectorIndex,
            SectorSpineNodeKind kind,
            SectorSpineEndpointRole endpointRole,
            LocalTileCoord coordinate,
            int routeType,
            AccessClass accessClass,
            string sourceId,
            string sourceIdentity)
        {
            NodeId = nodeId ?? string.Empty;
            SectorCoordinate = sectorCoordinate;
            SectorIndex = sectorIndex;
            Kind = kind;
            EndpointRole = endpointRole;
            Coordinate = coordinate;
            RouteType = routeType;
            AccessClass = accessClass;
            SourceId = sourceId ?? string.Empty;
            SourceIdentity = sourceIdentity ?? string.Empty;
        }

        public string NodeId { get; }
        public SectorCoord SectorCoordinate { get; }
        public int SectorIndex { get; }
        public SectorSpineNodeKind Kind { get; }
        public SectorSpineEndpointRole EndpointRole { get; }
        public LocalTileCoord Coordinate { get; }
        public int RouteType { get; }
        public AccessClass AccessClass { get; }
        public string SourceId { get; }
        public string SourceIdentity { get; }

        public int CompareTo(SectorSpineNode other)
        {
            if (other == null) return -1;
            var comparison = SectorIndex.CompareTo(other.SectorIndex);
            if (comparison != 0) return comparison;
            comparison = Kind.CompareTo(other.Kind);
            if (comparison != 0) return comparison;
            return string.Compare(NodeId, other.NodeId, StringComparison.Ordinal);
        }
    }

    public sealed class SectorSpineEdge : IComparable<SectorSpineEdge>
    {
        private readonly ReadOnlyCollection<LocalTileCoord> centerlineCells;

        internal SectorSpineEdge(
            string edgeId,
            int sectorIndex,
            SectorSpineEdgeKind kind,
            string fromNodeId,
            string toNodeId,
            string routeClass,
            string movementEvidence,
            int clearanceHeight,
            IEnumerable<LocalTileCoord> sourceCenterlineCells,
            string sourceIdentity)
        {
            EdgeId = edgeId ?? string.Empty;
            SectorIndex = sectorIndex;
            Kind = kind;
            FromNodeId = fromNodeId ?? string.Empty;
            ToNodeId = toNodeId ?? string.Empty;
            RouteClass = routeClass ?? string.Empty;
            MovementEvidence = movementEvidence ?? string.Empty;
            ClearanceHeight = clearanceHeight;
            centerlineCells = new ReadOnlyCollection<LocalTileCoord>((sourceCenterlineCells ?? Array.Empty<LocalTileCoord>()).ToArray());
            SourceIdentity = sourceIdentity ?? string.Empty;
        }

        public string EdgeId { get; }
        public int SectorIndex { get; }
        public SectorSpineEdgeKind Kind { get; }
        public string FromNodeId { get; }
        public string ToNodeId { get; }
        public string RouteClass { get; }
        public string MovementEvidence { get; }
        public int ClearanceHeight { get; }
        public IReadOnlyList<LocalTileCoord> CenterlineCells => centerlineCells;
        public string SourceIdentity { get; }

        public int CompareTo(SectorSpineEdge other)
        {
            if (other == null) return -1;
            var comparison = SectorIndex.CompareTo(other.SectorIndex);
            if (comparison != 0) return comparison;
            comparison = Kind.CompareTo(other.Kind);
            if (comparison != 0) return comparison;
            return string.Compare(EdgeId, other.EdgeId, StringComparison.Ordinal);
        }
    }

    public sealed class SectorTraversalEnvelopeCell : IComparable<SectorTraversalEnvelopeCell>
    {
        internal SectorTraversalEnvelopeCell(
            int sectorIndex,
            LocalTileCoord coordinate,
            SectorTraversalEnvelopeCellKind kind,
            string edgeId,
            string sourceIdentity)
        {
            SectorIndex = sectorIndex;
            Coordinate = coordinate;
            Kind = kind;
            EdgeId = edgeId ?? string.Empty;
            SourceIdentity = sourceIdentity ?? string.Empty;
        }

        public int SectorIndex { get; }
        public LocalTileCoord Coordinate { get; }
        public SectorTraversalEnvelopeCellKind Kind { get; }
        public string EdgeId { get; }
        public string SourceIdentity { get; }

        public int CompareTo(SectorTraversalEnvelopeCell other)
        {
            if (other == null) return -1;
            var comparison = SectorIndex.CompareTo(other.SectorIndex);
            if (comparison != 0) return comparison;
            comparison = Coordinate.Y.CompareTo(other.Coordinate.Y);
            if (comparison != 0) return comparison;
            comparison = Coordinate.X.CompareTo(other.Coordinate.X);
            if (comparison != 0) return comparison;
            comparison = Kind.CompareTo(other.Kind);
            if (comparison != 0) return comparison;
            return string.Compare(EdgeId, other.EdgeId, StringComparison.Ordinal);
        }
    }

    public sealed class SectorSpineEnvelopeBuildRequest
    {
        private readonly ReadOnlyCollection<SectorPacingAssignment> assignments;
        private readonly ReadOnlyCollection<SectorSpineEnvelopeErrorCode> referenceFaults;

        public SectorSpineEnvelopeBuildRequest(
            SectorPlannerInput input,
            IEnumerable<SectorPacingAssignment> sourceAssignments,
            SectorFixedAnchorPlan anchorPlan,
            SectorClusterPlacementPlan clusterPlacementPlan,
            string graphPublicationLabel,
            string envelopePublicationLabel,
            string expectedCanonicalDigest = "",
            IEnumerable<SectorSpineEnvelopeErrorCode> sourceReferenceFaults = null,
            bool routeAccessMutationClaim = false,
            bool anchorMutationClaim = false,
            bool clusterMutationClaim = false,
            int microPatternRenderCount = 0,
            int activityEventPlacementCount = 0,
            int retryCount = 0,
            int solverInvocationCount = 0,
            int randomDrawCount = 0,
            int tileWriteCount = 0,
            int canvasOwnershipWriteCount = 0,
            int sceneMutationCount = 0,
            int physicsInvocationCount = 0)
        {
            Input = input;
            assignments = new ReadOnlyCollection<SectorPacingAssignment>((sourceAssignments ?? Array.Empty<SectorPacingAssignment>()).Where(value => value != null).ToArray());
            AnchorPlan = anchorPlan;
            ClusterPlacementPlan = clusterPlacementPlan;
            GraphPublicationLabel = graphPublicationLabel ?? string.Empty;
            EnvelopePublicationLabel = envelopePublicationLabel ?? string.Empty;
            ExpectedCanonicalDigest = expectedCanonicalDigest ?? string.Empty;
            referenceFaults = new ReadOnlyCollection<SectorSpineEnvelopeErrorCode>((sourceReferenceFaults ?? Array.Empty<SectorSpineEnvelopeErrorCode>()).Distinct().OrderBy(value => value).ToArray());
            RouteAccessMutationClaim = routeAccessMutationClaim;
            AnchorMutationClaim = anchorMutationClaim;
            ClusterMutationClaim = clusterMutationClaim;
            MicroPatternRenderCount = microPatternRenderCount;
            ActivityEventPlacementCount = activityEventPlacementCount;
            RetryCount = retryCount;
            SolverInvocationCount = solverInvocationCount;
            RandomDrawCount = randomDrawCount;
            TileWriteCount = tileWriteCount;
            CanvasOwnershipWriteCount = canvasOwnershipWriteCount;
            SceneMutationCount = sceneMutationCount;
            PhysicsInvocationCount = physicsInvocationCount;
        }

        public SectorPlannerInput Input { get; }
        public IReadOnlyList<SectorPacingAssignment> Assignments => assignments;
        public SectorFixedAnchorPlan AnchorPlan { get; }
        public SectorClusterPlacementPlan ClusterPlacementPlan { get; }
        public string GraphPublicationLabel { get; }
        public string EnvelopePublicationLabel { get; }
        public string ExpectedCanonicalDigest { get; }
        public IReadOnlyList<SectorSpineEnvelopeErrorCode> ReferenceFaults => referenceFaults;
        public bool RouteAccessMutationClaim { get; }
        public bool AnchorMutationClaim { get; }
        public bool ClusterMutationClaim { get; }
        public int MicroPatternRenderCount { get; }
        public int ActivityEventPlacementCount { get; }
        public int RetryCount { get; }
        public int SolverInvocationCount { get; }
        public int RandomDrawCount { get; }
        public int TileWriteCount { get; }
        public int CanvasOwnershipWriteCount { get; }
        public int SceneMutationCount { get; }
        public int PhysicsInvocationCount { get; }
    }

    public sealed class SectorSpineGraph
    {
        private readonly ReadOnlyCollection<SectorSpineNode> nodes;
        private readonly ReadOnlyCollection<SectorSpineEdge> edges;
        private readonly ReadOnlyDictionary<SectorSpineNodeKind, int> nodeCountByKind;
        private readonly ReadOnlyDictionary<SectorSpineEdgeKind, int> edgeCountByKind;

        internal SectorSpineGraph(
            string publicationLabel,
            string plannerInputDigest,
            string pacingAssignmentDigest,
            string anchorPlanDigest,
            string clusterPlacementPlanDigest,
            string routeAccessIdentityDigest,
            string externalSocketIdentityDigest,
            string boundaryIdentityDigest,
            string specialIdentityDigest,
            string clusterIdentityDigest,
            IEnumerable<SectorSpineNode> sourceNodes,
            IEnumerable<SectorSpineEdge> sourceEdges,
            string canonicalDigest)
        {
            var orderedNodes = (sourceNodes ?? Array.Empty<SectorSpineNode>()).OrderBy(value => value).ToArray();
            var orderedEdges = (sourceEdges ?? Array.Empty<SectorSpineEdge>()).OrderBy(value => value).ToArray();
            PublicationLabel = publicationLabel ?? string.Empty;
            PlannerInputDigest = plannerInputDigest ?? string.Empty;
            PacingAssignmentDigest = pacingAssignmentDigest ?? string.Empty;
            AnchorPlanDigest = anchorPlanDigest ?? string.Empty;
            ClusterPlacementPlanDigest = clusterPlacementPlanDigest ?? string.Empty;
            RouteAccessIdentityDigest = routeAccessIdentityDigest ?? string.Empty;
            ExternalSocketIdentityDigest = externalSocketIdentityDigest ?? string.Empty;
            BoundaryIdentityDigest = boundaryIdentityDigest ?? string.Empty;
            SpecialIdentityDigest = specialIdentityDigest ?? string.Empty;
            ClusterIdentityDigest = clusterIdentityDigest ?? string.Empty;
            nodes = new ReadOnlyCollection<SectorSpineNode>(orderedNodes);
            edges = new ReadOnlyCollection<SectorSpineEdge>(orderedEdges);
            nodeCountByKind = CountAll(orderedNodes, value => value.Kind, Enum.GetValues(typeof(SectorSpineNodeKind)).Cast<SectorSpineNodeKind>());
            edgeCountByKind = CountAll(orderedEdges, value => value.Kind, Enum.GetValues(typeof(SectorSpineEdgeKind)).Cast<SectorSpineEdgeKind>());
            CanonicalDigest = canonicalDigest ?? string.Empty;
        }

        public string PublicationLabel { get; }
        public string PlannerInputDigest { get; }
        public string PacingAssignmentDigest { get; }
        public string AnchorPlanDigest { get; }
        public string ClusterPlacementPlanDigest { get; }
        public string RouteAccessIdentityDigest { get; }
        public string ExternalSocketIdentityDigest { get; }
        public string BoundaryIdentityDigest { get; }
        public string SpecialIdentityDigest { get; }
        public string ClusterIdentityDigest { get; }
        public IReadOnlyList<SectorSpineNode> Nodes => nodes;
        public IReadOnlyList<SectorSpineEdge> Edges => edges;
        public IReadOnlyDictionary<SectorSpineNodeKind, int> NodeCountByKind => nodeCountByKind;
        public IReadOnlyDictionary<SectorSpineEdgeKind, int> EdgeCountByKind => edgeCountByKind;
        public int SectorCount => nodes.Select(value => value.SectorIndex).Distinct().Count();
        public string CanonicalDigest { get; }
        public int Count(SectorSpineNodeKind kind) => nodeCountByKind[kind];
        public int Count(SectorSpineEdgeKind kind) => edgeCountByKind[kind];

        private static ReadOnlyDictionary<TKey, int> CountAll<TValue, TKey>(IEnumerable<TValue> values, Func<TValue, TKey> selector, IEnumerable<TKey> keys)
        {
            var result = new SortedDictionary<TKey, int>();
            foreach (var key in keys) result[key] = 0;
            foreach (var value in values) result[selector(value)]++;
            return new ReadOnlyDictionary<TKey, int>(result);
        }
    }

    public sealed class SectorSpineGraphBuildResult
    {
        private readonly ReadOnlyCollection<SectorSpineEnvelopeError> errors;

        internal SectorSpineGraphBuildResult(SectorSpineGraph graph, IEnumerable<SectorSpineEnvelopeError> sourceErrors)
        {
            var ordered = (sourceErrors ?? Array.Empty<SectorSpineEnvelopeError>()).Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            errors = new ReadOnlyCollection<SectorSpineEnvelopeError>(ordered);
            Graph = ordered.Length == 0 ? graph : null;
            CanonicalDigest = Graph == null ? string.Empty : Graph.CanonicalDigest;
        }

        public bool Success => Graph != null && errors.Count == 0;
        public SectorSpineGraph Graph { get; }
        public string CanonicalDigest { get; }
        public IReadOnlyList<SectorSpineEnvelopeError> Errors => errors;
    }

    public sealed class SectorSpineEnvelopePlan
    {
        private readonly ReadOnlyCollection<SectorTraversalEnvelopeCell> envelopeCells;
        private readonly ReadOnlyCollection<SectorTraversalEnvelopeCell> protectedOpenCells;
        private readonly ReadOnlyDictionary<SectorTraversalEnvelopeCellKind, int> envelopeCellCountByKind;

        internal SectorSpineEnvelopePlan(
            SectorSpineGraph graph,
            string envelopePublicationLabel,
            IEnumerable<SectorTraversalEnvelopeCell> sourceEnvelopeCells,
            IEnumerable<SectorTraversalEnvelopeCell> sourceProtectedOpenCells,
            int anchorCompatibleOverlapCount,
            int blockingAnchorOverlapCount,
            string envelopeDigest,
            string canonicalDigest)
        {
            Graph = graph;
            EnvelopePublicationLabel = envelopePublicationLabel ?? string.Empty;
            envelopeCells = new ReadOnlyCollection<SectorTraversalEnvelopeCell>((sourceEnvelopeCells ?? Array.Empty<SectorTraversalEnvelopeCell>()).OrderBy(value => value).ToArray());
            protectedOpenCells = new ReadOnlyCollection<SectorTraversalEnvelopeCell>((sourceProtectedOpenCells ?? Array.Empty<SectorTraversalEnvelopeCell>()).OrderBy(value => value).ToArray());
            var counts = new SortedDictionary<SectorTraversalEnvelopeCellKind, int>();
            foreach (SectorTraversalEnvelopeCellKind kind in Enum.GetValues(typeof(SectorTraversalEnvelopeCellKind))) counts[kind] = 0;
            foreach (var cell in envelopeCells) counts[cell.Kind]++;
            envelopeCellCountByKind = new ReadOnlyDictionary<SectorTraversalEnvelopeCellKind, int>(counts);
            AnchorCompatibleOverlapCount = anchorCompatibleOverlapCount;
            BlockingAnchorOverlapCount = blockingAnchorOverlapCount;
            EnvelopeDigest = envelopeDigest ?? string.Empty;
            CanonicalDigest = canonicalDigest ?? string.Empty;
        }

        public SectorSpineGraph Graph { get; }
        public string EnvelopePublicationLabel { get; }
        public IReadOnlyList<SectorTraversalEnvelopeCell> EnvelopeCells => envelopeCells;
        public IReadOnlyList<SectorTraversalEnvelopeCell> ProtectedOpenCells => protectedOpenCells;
        public IReadOnlyDictionary<SectorTraversalEnvelopeCellKind, int> EnvelopeCellCountByKind => envelopeCellCountByKind;
        public int SectorCount => Graph.SectorCount;
        public int NodeCount => Graph.Nodes.Count;
        public int EdgeCount => Graph.Edges.Count;
        public int EnvelopeCellCount => envelopeCells.Count;
        public int ProtectedOpenCellCount => protectedOpenCells.Count;
        public int AnchorCompatibleOverlapCount { get; }
        public int BlockingAnchorOverlapCount { get; }
        public int ClusterConnectorCount => Graph.Count(SectorSpineEdgeKind.ClusterConnector);
        public int SpecialConnectorCount => Graph.Count(SectorSpineEdgeKind.MandatorySpecialConnector);
        public int MandatoryRouteCount => Graph.Edges.Count(value => value.Kind == SectorSpineEdgeKind.MandatoryLow
            || value.Kind == SectorSpineEdgeKind.MandatorySpecialConnector
            || value.Kind == SectorSpineEdgeKind.BoundaryConnector
            || value.Kind == SectorSpineEdgeKind.ClusterConnector
            || value.Kind == SectorSpineEdgeKind.Return);
        public int OptionalHighRecoveryRouteCount => Graph.Edges.Count(value => value.Kind == SectorSpineEdgeKind.OptionalHigh || value.Kind == SectorSpineEdgeKind.Recovery);
        public string SpineGraphDigest => Graph.CanonicalDigest;
        public string EnvelopeDigest { get; }
        public string CanonicalDigest { get; }
        public string PlannerInputDigestBefore => Graph.PlannerInputDigest;
        public string PlannerInputDigestAfter => Graph.PlannerInputDigest;
        public string PacingAssignmentDigestBefore => Graph.PacingAssignmentDigest;
        public string PacingAssignmentDigestAfter => Graph.PacingAssignmentDigest;
        public string AnchorPlanDigestBefore => Graph.AnchorPlanDigest;
        public string AnchorPlanDigestAfter => Graph.AnchorPlanDigest;
        public string ClusterPlacementPlanDigestBefore => Graph.ClusterPlacementPlanDigest;
        public string ClusterPlacementPlanDigestAfter => Graph.ClusterPlacementPlanDigest;
        public string RouteAccessIdentityBefore => Graph.RouteAccessIdentityDigest;
        public string RouteAccessIdentityAfter => Graph.RouteAccessIdentityDigest;
        public string ExternalSocketIdentityBefore => Graph.ExternalSocketIdentityDigest;
        public string ExternalSocketIdentityAfter => Graph.ExternalSocketIdentityDigest;
        public string BoundaryIdentityBefore => Graph.BoundaryIdentityDigest;
        public string BoundaryIdentityAfter => Graph.BoundaryIdentityDigest;
        public string SpecialIdentityBefore => Graph.SpecialIdentityDigest;
        public string SpecialIdentityAfter => Graph.SpecialIdentityDigest;
        public string ClusterIdentityBefore => Graph.ClusterIdentityDigest;
        public string ClusterIdentityAfter => Graph.ClusterIdentityDigest;
        public bool Map14_05HandoffReady => SectorCount > 0 && BlockingAnchorOverlapCount == 0 && ProtectedOpenCellCount > 0;
        public int SolverInvocationCount => 0;
        public int RandomDrawCount => 0;
        public int TileWriteCount => 0;
        public int MicroPatternRenderCount => 0;
        public int ActivityEventPlacementCount => 0;
        public int RetryCount => 0;
        public int CanvasOwnershipWriteCount => 0;
        public int SceneMutationCount => 0;
        public int PhysicsInvocationCount => 0;

        public int Count(SectorTraversalEnvelopeCellKind kind) => envelopeCellCountByKind[kind];
    }

    public sealed class SectorSpineEnvelopeBuildResult
    {
        private readonly ReadOnlyCollection<SectorSpineEnvelopeError> errors;

        internal SectorSpineEnvelopeBuildResult(SectorSpineEnvelopePlan plan, IEnumerable<SectorSpineEnvelopeError> sourceErrors)
        {
            var ordered = (sourceErrors ?? Array.Empty<SectorSpineEnvelopeError>()).Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            errors = new ReadOnlyCollection<SectorSpineEnvelopeError>(ordered);
            Plan = ordered.Length == 0 ? plan : null;
            CanonicalDigest = Plan == null ? string.Empty : Plan.CanonicalDigest;
        }

        public bool Success => Plan != null && errors.Count == 0;
        public SectorSpineEnvelopePlan Plan { get; }
        public string CanonicalDigest { get; }
        public IReadOnlyList<SectorSpineEnvelopeError> Errors => errors;
        public int MutationCount => 0;
        public int SolverInvocationCount => 0;
        public int RandomDrawCount => 0;
        public int TileWriteCount => 0;
    }

    public static class SectorSpineEnvelopeCanonicalDigest
    {
        public static string ComputeGraph(SectorSpineGraph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            return Hash(string.Join("\n", new[]
            {
                graph.PublicationLabel, graph.PlannerInputDigest, graph.PacingAssignmentDigest,
                graph.AnchorPlanDigest, graph.ClusterPlacementPlanDigest, graph.RouteAccessIdentityDigest,
                graph.ExternalSocketIdentityDigest, graph.BoundaryIdentityDigest, graph.SpecialIdentityDigest,
                graph.ClusterIdentityDigest,
                string.Join("\n", graph.Nodes.Select(NodeMaterial)),
                string.Join("\n", graph.Edges.Select(EdgeMaterial)),
            }));
        }

        public static string ComputeEnvelope(IEnumerable<SectorTraversalEnvelopeCell> cells, IEnumerable<SectorTraversalEnvelopeCell> protectedOpenCells)
        {
            return Hash(string.Join("\n", new[]
            {
                string.Join("\n", (cells ?? Array.Empty<SectorTraversalEnvelopeCell>()).OrderBy(value => value).Select(CellMaterial)),
                string.Join("\n", (protectedOpenCells ?? Array.Empty<SectorTraversalEnvelopeCell>()).OrderBy(value => value).Select(CellMaterial)),
            }));
        }

        public static string ComputePlan(SectorSpineEnvelopePlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            return Hash(string.Join("\n", new[]
            {
                plan.SpineGraphDigest, plan.EnvelopePublicationLabel, plan.EnvelopeDigest,
                plan.AnchorCompatibleOverlapCount.ToString(CultureInfo.InvariantCulture),
                plan.BlockingAnchorOverlapCount.ToString(CultureInfo.InvariantCulture),
                plan.ProtectedOpenCellCount.ToString(CultureInfo.InvariantCulture),
            }));
        }

        internal static string Hash(string material)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(material ?? string.Empty));
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes) builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        internal static string CoordinateMaterial(LocalTileCoord value)
            => value.X.ToString(CultureInfo.InvariantCulture) + "," + value.Y.ToString(CultureInfo.InvariantCulture);

        private static string NodeMaterial(SectorSpineNode value) => string.Join("|", new[]
        {
            value.SectorIndex.ToString(CultureInfo.InvariantCulture), value.NodeId, value.Kind.ToString(), value.EndpointRole.ToString(),
            CoordinateMaterial(value.Coordinate), value.RouteType.ToString(CultureInfo.InvariantCulture), value.AccessClass.ToString(),
            value.SourceId, value.SourceIdentity,
        });

        private static string EdgeMaterial(SectorSpineEdge value) => string.Join("|", new[]
        {
            value.SectorIndex.ToString(CultureInfo.InvariantCulture), value.EdgeId, value.Kind.ToString(), value.FromNodeId,
            value.ToNodeId, value.RouteClass, value.MovementEvidence, value.ClearanceHeight.ToString(CultureInfo.InvariantCulture),
            string.Join(";", value.CenterlineCells.Select(CoordinateMaterial)), value.SourceIdentity,
        });

        private static string CellMaterial(SectorTraversalEnvelopeCell value) => string.Join("|", new[]
        {
            value.SectorIndex.ToString(CultureInfo.InvariantCulture), CoordinateMaterial(value.Coordinate), value.Kind.ToString(),
            value.EdgeId, value.SourceIdentity,
        });
    }
}
