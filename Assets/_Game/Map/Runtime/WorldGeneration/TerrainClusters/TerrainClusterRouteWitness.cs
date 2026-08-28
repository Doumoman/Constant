using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.TerrainClusters
{
    public sealed class TraversalEdgeDurationEvidence
    {
        public TraversalEdgeDurationEvidence(
            SpineVariantId variantId,
            string edgeId,
            int estimatedDurationMilliseconds,
            string rulesetId)
        {
            VariantId = variantId;
            EdgeId = edgeId ?? string.Empty;
            EstimatedDurationMilliseconds = estimatedDurationMilliseconds;
            RulesetId = rulesetId ?? string.Empty;
        }

        public SpineVariantId VariantId { get; }
        public string EdgeId { get; }
        public int EstimatedDurationMilliseconds { get; }
        public string RulesetId { get; }
        public string StableIdentity => VariantId.Value + "/" + EdgeId;
    }

    public sealed class TerrainClusterHighRouteDefinition
    {
        private readonly ReadOnlyCollection<string> orderedEdgeIds;
        private readonly ReadOnlyCollection<string> benefitIds;
        private readonly ReadOnlyCollection<string> failureNodeIds;

        public TerrainClusterHighRouteDefinition(
            string highRouteId,
            SpineVariantId variantId,
            string baseDivergenceNodeId,
            IEnumerable<string> orderedEdgeIds,
            string baseRejoinNodeId,
            string highPointNodeId,
            IEnumerable<string> benefitIds,
            IEnumerable<string> failureNodeIds)
        {
            HighRouteId = highRouteId ?? string.Empty;
            VariantId = variantId;
            BaseDivergenceNodeId = baseDivergenceNodeId ?? string.Empty;
            BaseRejoinNodeId = baseRejoinNodeId ?? string.Empty;
            HighPointNodeId = highPointNodeId ?? string.Empty;
            this.orderedEdgeIds = new ReadOnlyCollection<string>(
                (orderedEdgeIds ?? Array.Empty<string>()).Select(value => value ?? string.Empty).ToArray());
            this.benefitIds = new ReadOnlyCollection<string>(
                (benefitIds ?? Array.Empty<string>()).Select(value => value ?? string.Empty)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray());
            this.failureNodeIds = new ReadOnlyCollection<string>(
                (failureNodeIds ?? Array.Empty<string>()).Select(value => value ?? string.Empty)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        public string HighRouteId { get; }
        public SpineVariantId VariantId { get; }
        public string BaseDivergenceNodeId { get; }
        public IReadOnlyList<string> OrderedEdgeIds => orderedEdgeIds;
        public string BaseRejoinNodeId { get; }
        public string HighPointNodeId { get; }
        public IReadOnlyList<string> BenefitIds => benefitIds;
        public IReadOnlyList<string> FailureNodeIds => failureNodeIds;
    }

    public sealed class TerrainClusterRouteWitnessIntent
    {
        private readonly ReadOnlyCollection<TerrainClusterHighRouteDefinition> highRoutes;
        private readonly ReadOnlyCollection<TraversalEdgeDurationEvidence> durations;

        public TerrainClusterRouteWitnessIntent(
            SpineVariantId baselineVariantId,
            IEnumerable<TerrainClusterHighRouteDefinition> highRoutes,
            IEnumerable<TraversalEdgeDurationEvidence> edgeDurationEvidence)
        {
            BaselineVariantId = baselineVariantId;
            this.highRoutes = new ReadOnlyCollection<TerrainClusterHighRouteDefinition>(
                (highRoutes ?? Array.Empty<TerrainClusterHighRouteDefinition>())
                    .OrderBy(value => value == null ? string.Empty : value.HighRouteId, StringComparer.Ordinal).ToArray());
            durations = new ReadOnlyCollection<TraversalEdgeDurationEvidence>(
                (edgeDurationEvidence ?? Array.Empty<TraversalEdgeDurationEvidence>())
                    .OrderBy(value => value == null ? string.Empty : value.StableIdentity, StringComparer.Ordinal)
                    .ThenBy(value => value == null ? string.Empty : value.RulesetId, StringComparer.Ordinal).ToArray());
        }

        public SpineVariantId BaselineVariantId { get; }
        public IReadOnlyList<TerrainClusterHighRouteDefinition> HighRoutes => highRoutes;
        public IReadOnlyList<TraversalEdgeDurationEvidence> EdgeDurationEvidence => durations;
    }

    public sealed class TerrainClusterRouteWitnessEdge
    {
        internal TerrainClusterRouteWitnessEdge(CompiledTraversalEdge edge, int durationMilliseconds)
        {
            VariantId = edge.VariantId;
            EdgeId = edge.EdgeId;
            FromNodeId = edge.FromNodeId;
            ToNodeId = edge.ToNodeId;
            MovementKind = edge.MovementKind;
            CompiledStartCoordinate = edge.CompiledStartCoordinate;
            CompiledEndCoordinate = edge.CompiledEndCoordinate;
            EstimatedDurationMilliseconds = durationMilliseconds;
        }

        public SpineVariantId VariantId { get; }
        public string EdgeId { get; }
        public string FromNodeId { get; }
        public string ToNodeId { get; }
        public TraversalMovementKind MovementKind { get; }
        public LocalTileCoord CompiledStartCoordinate { get; }
        public LocalTileCoord CompiledEndCoordinate { get; }
        public int EstimatedDurationMilliseconds { get; }
    }

    public sealed class TerrainClusterBaselineRouteWitness
    {
        private readonly ReadOnlyCollection<string> nodeIds;
        private readonly ReadOnlyCollection<TerrainClusterRouteWitnessEdge> edges;
        private readonly ReadOnlyCollection<LocalTileCoord> coordinates;
        private readonly ReadOnlyCollection<LocalTileCoord> coveredProtectedTiles;
        private readonly ReadOnlyCollection<ClusterRoleKind> preservedMandatoryRoles;

        internal TerrainClusterBaselineRouteWitness(
            SpineVariantId variantId,
            string entryPortId,
            string entryRoleAnchorId,
            string entryNodeId,
            string exitNodeId,
            string exitRoleAnchorId,
            string exitPortId,
            IEnumerable<string> nodeIds,
            IEnumerable<TerrainClusterRouteWitnessEdge> edges,
            IEnumerable<LocalTileCoord> coordinates,
            IEnumerable<LocalTileCoord> coveredProtectedTiles,
            IEnumerable<ClusterRoleKind> preservedMandatoryRoles)
        {
            VariantId = variantId;
            EntryPortId = entryPortId ?? string.Empty;
            EntryRoleAnchorId = entryRoleAnchorId ?? string.Empty;
            EntryNodeId = entryNodeId ?? string.Empty;
            ExitNodeId = exitNodeId ?? string.Empty;
            ExitRoleAnchorId = exitRoleAnchorId ?? string.Empty;
            ExitPortId = exitPortId ?? string.Empty;
            this.nodeIds = Copy(nodeIds);
            this.edges = new ReadOnlyCollection<TerrainClusterRouteWitnessEdge>((edges ?? Array.Empty<TerrainClusterRouteWitnessEdge>()).ToArray());
            this.coordinates = new ReadOnlyCollection<LocalTileCoord>((coordinates ?? Array.Empty<LocalTileCoord>()).ToArray());
            this.coveredProtectedTiles = CopyCoordinates(coveredProtectedTiles);
            this.preservedMandatoryRoles = new ReadOnlyCollection<ClusterRoleKind>((preservedMandatoryRoles ?? Array.Empty<ClusterRoleKind>()).Distinct().OrderBy(value => value).ToArray());
        }

        public SpineVariantId VariantId { get; }
        public string EntryPortId { get; }
        public string EntryRoleAnchorId { get; }
        public string EntryNodeId { get; }
        public string ExitNodeId { get; }
        public string ExitRoleAnchorId { get; }
        public string ExitPortId { get; }
        public IReadOnlyList<string> OrderedNodeIds => nodeIds;
        public IReadOnlyList<TerrainClusterRouteWitnessEdge> OrderedEdges => edges;
        public IReadOnlyList<LocalTileCoord> CompiledCoordinates => coordinates;
        public IReadOnlyList<LocalTileCoord> CoveredProtectedTiles => coveredProtectedTiles;
        public IReadOnlyList<ClusterRoleKind> PreservedMandatoryRoles => preservedMandatoryRoles;
        public int TotalEstimatedDurationMilliseconds => edges.Sum(value => value.EstimatedDurationMilliseconds);
        public int PatternOperationCount => 0;

        private static ReadOnlyCollection<string> Copy(IEnumerable<string> source)
        {
            return new ReadOnlyCollection<string>((source ?? Array.Empty<string>()).Select(value => value ?? string.Empty).ToArray());
        }

        internal static ReadOnlyCollection<LocalTileCoord> CopyCoordinates(IEnumerable<LocalTileCoord> source)
        {
            return new ReadOnlyCollection<LocalTileCoord>((source ?? Array.Empty<LocalTileCoord>()).Distinct().OrderBy(value => value.Y).ThenBy(value => value.X).ToArray());
        }
    }

    public sealed class TerrainClusterHighRouteWitness
    {
        private readonly ReadOnlyCollection<string> nodeIds;
        private readonly ReadOnlyCollection<TerrainClusterRouteWitnessEdge> edges;
        private readonly ReadOnlyCollection<string> benefitIds;
        private readonly ReadOnlyCollection<string> failureNodeIds;
        private readonly ReadOnlyCollection<LocalTileCoord> coveredProtectedTiles;

        internal TerrainClusterHighRouteWitness(
            TerrainClusterHighRouteDefinition definition,
            IEnumerable<string> nodeIds,
            IEnumerable<TerrainClusterRouteWitnessEdge> edges,
            IEnumerable<LocalTileCoord> coveredProtectedTiles)
        {
            HighRouteId = definition.HighRouteId;
            VariantId = definition.VariantId;
            BaseDivergenceNodeId = definition.BaseDivergenceNodeId;
            BaseRejoinNodeId = definition.BaseRejoinNodeId;
            HighPointNodeId = definition.HighPointNodeId;
            this.nodeIds = new ReadOnlyCollection<string>((nodeIds ?? Array.Empty<string>()).ToArray());
            this.edges = new ReadOnlyCollection<TerrainClusterRouteWitnessEdge>((edges ?? Array.Empty<TerrainClusterRouteWitnessEdge>()).ToArray());
            benefitIds = new ReadOnlyCollection<string>(definition.BenefitIds.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
            failureNodeIds = new ReadOnlyCollection<string>(definition.FailureNodeIds.Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
            this.coveredProtectedTiles = TerrainClusterBaselineRouteWitness.CopyCoordinates(coveredProtectedTiles);
        }

        public string HighRouteId { get; }
        public SpineVariantId VariantId { get; }
        public string BaseDivergenceNodeId { get; }
        public string BaseRejoinNodeId { get; }
        public string HighPointNodeId { get; }
        public IReadOnlyList<string> OrderedNodeIds => nodeIds;
        public IReadOnlyList<TerrainClusterRouteWitnessEdge> OrderedEdges => edges;
        public IReadOnlyList<string> BenefitIds => benefitIds;
        public IReadOnlyList<string> FailureNodeIds => failureNodeIds;
        public IReadOnlyList<LocalTileCoord> CoveredProtectedTiles => coveredProtectedTiles;
    }

    public sealed class TerrainClusterRecoveryRouteWitness
    {
        private readonly ReadOnlyCollection<string> nodeIds;
        private readonly ReadOnlyCollection<TerrainClusterRouteWitnessEdge> edges;
        private readonly ReadOnlyCollection<LocalTileCoord> coordinates;
        private readonly ReadOnlyCollection<LocalTileCoord> coveredProtectedTiles;

        internal TerrainClusterRecoveryRouteWitness(
            string highRouteId,
            string failureNodeId,
            string targetBaselineNodeId,
            bool targetsRecoveryRole,
            IEnumerable<string> nodeIds,
            IEnumerable<TerrainClusterRouteWitnessEdge> edges,
            IEnumerable<LocalTileCoord> coordinates,
            IEnumerable<LocalTileCoord> coveredProtectedTiles)
        {
            HighRouteId = highRouteId ?? string.Empty;
            FailureNodeId = failureNodeId ?? string.Empty;
            TargetBaselineNodeId = targetBaselineNodeId ?? string.Empty;
            TargetsRecoveryRole = targetsRecoveryRole;
            this.nodeIds = new ReadOnlyCollection<string>((nodeIds ?? Array.Empty<string>()).ToArray());
            this.edges = new ReadOnlyCollection<TerrainClusterRouteWitnessEdge>((edges ?? Array.Empty<TerrainClusterRouteWitnessEdge>()).ToArray());
            this.coordinates = new ReadOnlyCollection<LocalTileCoord>((coordinates ?? Array.Empty<LocalTileCoord>()).ToArray());
            this.coveredProtectedTiles = TerrainClusterBaselineRouteWitness.CopyCoordinates(coveredProtectedTiles);
        }

        public string HighRouteId { get; }
        public string FailureNodeId { get; }
        public string TargetBaselineNodeId { get; }
        public string RejoinedBaselineNodeId => TargetBaselineNodeId;
        public bool TargetsRecoveryRole { get; }
        public IReadOnlyList<string> OrderedNodeIds => nodeIds;
        public IReadOnlyList<TerrainClusterRouteWitnessEdge> OrderedEdges => edges;
        public IReadOnlyList<LocalTileCoord> CompiledCoordinates => coordinates;
        public IReadOnlyList<LocalTileCoord> CoveredProtectedTiles => coveredProtectedTiles;
        public int TotalEstimatedDurationMilliseconds => edges.Sum(value => value.EstimatedDurationMilliseconds);
    }

    public sealed class TerrainClusterRouteWitnessReport
    {
        private readonly ReadOnlyCollection<TerrainClusterHighRouteWitness> highRoutes;
        private readonly ReadOnlyCollection<TerrainClusterRecoveryRouteWitness> recoveryRoutes;

        internal TerrainClusterRouteWitnessReport(
            TerrainClusterId clusterId,
            string rulesetId,
            string traversalCompilationDigest,
            TerrainClusterStaticShell staticShell,
            TerrainClusterBaselineRouteWitness baselineRoute,
            IEnumerable<TerrainClusterHighRouteWitness> highRoutes,
            IEnumerable<TerrainClusterRecoveryRouteWitness> recoveryRoutes,
            string canonicalDigest)
        {
            ClusterId = clusterId;
            RulesetId = rulesetId ?? string.Empty;
            TraversalCompilationDigest = traversalCompilationDigest ?? string.Empty;
            StaticShell = staticShell;
            BaselineRoute = baselineRoute;
            this.highRoutes = new ReadOnlyCollection<TerrainClusterHighRouteWitness>((highRoutes ?? Array.Empty<TerrainClusterHighRouteWitness>()).OrderBy(value => value.HighRouteId, StringComparer.Ordinal).ToArray());
            this.recoveryRoutes = new ReadOnlyCollection<TerrainClusterRecoveryRouteWitness>((recoveryRoutes ?? Array.Empty<TerrainClusterRecoveryRouteWitness>()).OrderBy(value => value.HighRouteId, StringComparer.Ordinal).ThenBy(value => value.FailureNodeId, StringComparer.Ordinal).ToArray());
            CanonicalDigest = canonicalDigest ?? string.Empty;
        }

        public TerrainClusterId ClusterId { get; }
        public string RulesetId { get; }
        public string TraversalCompilationDigest { get; }
        public TerrainClusterStaticShell StaticShell { get; }
        public TerrainClusterBaselineRouteWitness BaselineRoute { get; }
        public IReadOnlyList<TerrainClusterHighRouteWitness> HighRoutes => highRoutes;
        public IReadOnlyList<TerrainClusterRecoveryRouteWitness> RecoveryRoutes => recoveryRoutes;
        public string CanonicalDigest { get; }
        public int PatternOperationCount => 0;
    }

    public sealed class TerrainClusterRouteWitnessCompileRequest
    {
        public TerrainClusterRouteWitnessCompileRequest(
            TerrainClusterLocalCanvas localCanvas,
            string localCanvasCanonicalDigest,
            TerrainClusterRoleSocketContract roleSocketContract,
            string roleSocketContractCanonicalDigest,
            TerrainClusterTraversalCompilation traversalCompilation,
            string traversalCompilationCanonicalDigest,
            TerrainClusterRouteWitnessIntent intent)
        {
            LocalCanvas = localCanvas;
            LocalCanvasCanonicalDigest = localCanvasCanonicalDigest ?? string.Empty;
            RoleSocketContract = roleSocketContract;
            RoleSocketContractCanonicalDigest = roleSocketContractCanonicalDigest ?? string.Empty;
            TraversalCompilation = traversalCompilation;
            TraversalCompilationCanonicalDigest = traversalCompilationCanonicalDigest ?? string.Empty;
            Intent = intent;
        }

        public TerrainClusterLocalCanvas LocalCanvas { get; }
        public string LocalCanvasCanonicalDigest { get; }
        public TerrainClusterRoleSocketContract RoleSocketContract { get; }
        public string RoleSocketContractCanonicalDigest { get; }
        public TerrainClusterTraversalCompilation TraversalCompilation { get; }
        public string TraversalCompilationCanonicalDigest { get; }
        public TerrainClusterRouteWitnessIntent Intent { get; }
    }

    public enum TerrainClusterRouteWitnessCompileErrorCode
    {
        MissingInput = 1,
        ArtifactIdentityMismatch = 2,
        ArtifactDigestMismatch = 3,
        StaticShellConflict = 4,
        ShellCoverageMismatch = 5,
        InvalidBaselineVariant = 6,
        MissingBaselinePath = 7,
        DisconnectedBaselinePath = 8,
        InvalidDurationEvidence = 9,
        MissingHighRoute = 10,
        InvalidHighRouteId = 11,
        InvalidHighRoutePath = 12,
        HighRouteNotDistinct = 13,
        InvalidHighPoint = 14,
        InsufficientHighRouteBenefits = 15,
        InvalidFailureNode = 16,
        MissingRecoveryPath = 17,
        RecoveryTargetMismatch = 18,
        RecoveryTooShort = 19,
        RecoveryTooLong = 20,
        ShellRouteMismatch = 21,
        NonCanonicalPublication = 22,
    }

    public sealed class TerrainClusterRouteWitnessCompileError :
        IEquatable<TerrainClusterRouteWitnessCompileError>,
        IComparable<TerrainClusterRouteWitnessCompileError>
    {
        public TerrainClusterRouteWitnessCompileError(
            TerrainClusterRouteWitnessCompileErrorCode code,
            string path,
            string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public TerrainClusterRouteWitnessCompileErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(TerrainClusterRouteWitnessCompileError other)
        {
            if (other == null) return -1;
            var comparison = ((int)Code).CompareTo((int)other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return comparison != 0 ? comparison : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(TerrainClusterRouteWitnessCompileError other)
        {
            return other != null && Code == other.Code &&
                string.Equals(Path, other.Path, StringComparison.Ordinal) &&
                string.Equals(Detail, other.Detail, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) { return Equals(obj as TerrainClusterRouteWitnessCompileError); }
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Code;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Path);
                return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Detail);
            }
        }
        public override string ToString() { return Code + "|" + Path + "|" + Detail; }
    }

    public sealed class TerrainClusterRouteWitnessCompileResult
    {
        private readonly ReadOnlyCollection<TerrainClusterRouteWitnessCompileError> errors;

        internal TerrainClusterRouteWitnessCompileResult(
            TerrainClusterRouteWitnessReport report,
            IEnumerable<TerrainClusterRouteWitnessCompileError> errors)
        {
            var copy = (errors ?? Array.Empty<TerrainClusterRouteWitnessCompileError>())
                .Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            this.errors = new ReadOnlyCollection<TerrainClusterRouteWitnessCompileError>(copy);
            Report = copy.Length == 0 ? report : null;
        }

        public bool IsSuccess => Report != null && errors.Count == 0;
        public TerrainClusterRouteWitnessReport Report { get; }
        public TerrainClusterStaticShell StaticShell => Report == null ? null : Report.StaticShell;
        public TerrainClusterBaselineRouteWitness BaselineRoute => Report == null ? null : Report.BaselineRoute;
        public IReadOnlyList<TerrainClusterHighRouteWitness> HighRoutes => Report == null ? Array.Empty<TerrainClusterHighRouteWitness>() : Report.HighRoutes;
        public IReadOnlyList<TerrainClusterRecoveryRouteWitness> RecoveryRoutes => Report == null ? Array.Empty<TerrainClusterRecoveryRouteWitness>() : Report.RecoveryRoutes;
        public IReadOnlyList<TerrainClusterRouteWitnessCompileError> Errors => errors;
        public string CanonicalDigest => Report == null ? string.Empty : Report.CanonicalDigest;
    }
}
