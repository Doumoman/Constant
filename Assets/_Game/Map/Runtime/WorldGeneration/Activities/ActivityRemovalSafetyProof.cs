using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.WorldGeneration.Activities
{
    public sealed class ActivityCueObservationEvidence
    {
        public ActivityCueObservationEvidence(
            string cueId,
            ActivityCueKind cueKind,
            ActivitySlotId slotId,
            string baselineWitnessObservationEdgeId,
            string activationBoundaryEdgeId,
            LocalTileCoord observationSourceCoordinate,
            int maximumObservationDistanceTiles)
        {
            CueId = cueId ?? string.Empty;
            CueKind = cueKind;
            SlotId = slotId;
            BaselineWitnessObservationEdgeId = baselineWitnessObservationEdgeId ?? string.Empty;
            ActivationBoundaryEdgeId = activationBoundaryEdgeId ?? string.Empty;
            ObservationSourceCoordinate = observationSourceCoordinate;
            MaximumObservationDistanceTiles = maximumObservationDistanceTiles;
        }

        public string CueId { get; }
        public ActivityCueKind CueKind { get; }
        public ActivitySlotId SlotId { get; }
        public string BaselineWitnessObservationEdgeId { get; }
        public string ActivationBoundaryEdgeId { get; }
        public LocalTileCoord ObservationSourceCoordinate { get; }
        public int MaximumObservationDistanceTiles { get; }
    }

    public sealed class ActivityCueObservationProof
    {
        private readonly ReadOnlyCollection<LocalTileCoord> supercoverCoordinates;

        internal ActivityCueObservationProof(
            ActivityCueObservationEvidence evidence,
            LocalTileCoord observationCompiledCoordinate,
            LocalTileCoord cueCompiledCoordinate,
            int observationEdgeOrdinal,
            int activationBoundaryEdgeOrdinal,
            int distanceTiles,
            bool usesDistanceOnly,
            IEnumerable<LocalTileCoord> supercoverCoordinates)
        {
            CueId = evidence.CueId;
            CueKind = evidence.CueKind;
            SlotId = evidence.SlotId;
            BaselineWitnessObservationEdgeId = evidence.BaselineWitnessObservationEdgeId;
            ActivationBoundaryEdgeId = evidence.ActivationBoundaryEdgeId;
            ObservationSourceCoordinate = evidence.ObservationSourceCoordinate;
            ObservationCompiledCoordinate = observationCompiledCoordinate;
            CueCompiledCoordinate = cueCompiledCoordinate;
            MaximumObservationDistanceTiles = evidence.MaximumObservationDistanceTiles;
            ObservationEdgeOrdinal = observationEdgeOrdinal;
            ActivationBoundaryEdgeOrdinal = activationBoundaryEdgeOrdinal;
            DistanceTiles = distanceTiles;
            UsesDistanceOnly = usesDistanceOnly;
            this.supercoverCoordinates = new ReadOnlyCollection<LocalTileCoord>(
                (supercoverCoordinates ?? Array.Empty<LocalTileCoord>()).ToArray());
        }

        public string CueId { get; }
        public ActivityCueKind CueKind { get; }
        public ActivitySlotId SlotId { get; }
        public string BaselineWitnessObservationEdgeId { get; }
        public string ActivationBoundaryEdgeId { get; }
        public LocalTileCoord ObservationSourceCoordinate { get; }
        public LocalTileCoord ObservationCompiledCoordinate { get; }
        public LocalTileCoord CueCompiledCoordinate { get; }
        public int MaximumObservationDistanceTiles { get; }
        public int ObservationEdgeOrdinal { get; }
        public int ActivationBoundaryEdgeOrdinal { get; }
        public int DistanceTiles { get; }
        public bool UsesDistanceOnly { get; }
        public IReadOnlyList<LocalTileCoord> SupercoverCoordinates => supercoverCoordinates;
        public int OccludingCoordinateCount => 0;
    }

    public enum ActivityOverlaySnapshotKind
    {
        Active = 1,
        Removed = 2,
    }

    public sealed class ActivityOverlayRemovalIntent
    {
        private readonly ReadOnlyCollection<string> removedOverlayIdentities;
        private readonly ReadOnlyCollection<string> residualOverlayIdentities;

        public ActivityOverlayRemovalIntent(
            IEnumerable<string> removedOverlayIdentities,
            IEnumerable<string> residualOverlayIdentities = null,
            bool permanentTileMutationDeclared = false,
            bool mandatoryExitDestructionDeclared = false,
            bool rewardDestructionDeclared = false,
            string staticShellDigestAfterRemovalDeclaration = null,
            string workingCanvasDigestAfterRemovalDeclaration = null,
            string traversalDigestAfterRemovalDeclaration = null,
            string routeWitnessDigestAfterRemovalDeclaration = null,
            int? routeTypeAfterRemovalDeclaration = null,
            AccessClass? accessClassAfterRemovalDeclaration = null)
        {
            this.removedOverlayIdentities = CopyStrings(removedOverlayIdentities);
            this.residualOverlayIdentities = CopyStrings(residualOverlayIdentities);
            PermanentTileMutationDeclared = permanentTileMutationDeclared;
            MandatoryExitDestructionDeclared = mandatoryExitDestructionDeclared;
            RewardDestructionDeclared = rewardDestructionDeclared;
            StaticShellDigestAfterRemovalDeclaration = staticShellDigestAfterRemovalDeclaration ?? string.Empty;
            WorkingCanvasDigestAfterRemovalDeclaration = workingCanvasDigestAfterRemovalDeclaration ?? string.Empty;
            TraversalDigestAfterRemovalDeclaration = traversalDigestAfterRemovalDeclaration ?? string.Empty;
            RouteWitnessDigestAfterRemovalDeclaration = routeWitnessDigestAfterRemovalDeclaration ?? string.Empty;
            RouteTypeAfterRemovalDeclaration = routeTypeAfterRemovalDeclaration;
            AccessClassAfterRemovalDeclaration = accessClassAfterRemovalDeclaration;
        }

        public IReadOnlyList<string> RemovedOverlayIdentities => removedOverlayIdentities;
        public IReadOnlyList<string> ResidualOverlayIdentities => residualOverlayIdentities;
        public bool PermanentTileMutationDeclared { get; }
        public bool MandatoryExitDestructionDeclared { get; }
        public bool RewardDestructionDeclared { get; }
        public string StaticShellDigestAfterRemovalDeclaration { get; }
        public string WorkingCanvasDigestAfterRemovalDeclaration { get; }
        public string TraversalDigestAfterRemovalDeclaration { get; }
        public string RouteWitnessDigestAfterRemovalDeclaration { get; }
        public int? RouteTypeAfterRemovalDeclaration { get; }
        public AccessClass? AccessClassAfterRemovalDeclaration { get; }

        private static ReadOnlyCollection<string> CopyStrings(IEnumerable<string> source)
        {
            return new ReadOnlyCollection<string>((source ?? Array.Empty<string>())
                .Select(value => value ?? string.Empty)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray());
        }
    }

    public sealed class ActivityOverlaySnapshot
    {
        private readonly ReadOnlyCollection<string> overlayIdentities;

        internal ActivityOverlaySnapshot(
            ActivityOverlaySnapshotKind kind,
            IEnumerable<string> overlayIdentities,
            string staticShellDigest,
            string workingCanvasDigest,
            string traversalDigest,
            string routeWitnessDigest,
            int routeType,
            AccessClass accessClass)
        {
            Kind = kind;
            this.overlayIdentities = new ReadOnlyCollection<string>(
                (overlayIdentities ?? Array.Empty<string>())
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray());
            StaticShellDigest = staticShellDigest ?? string.Empty;
            WorkingCanvasDigest = workingCanvasDigest ?? string.Empty;
            TraversalDigest = traversalDigest ?? string.Empty;
            RouteWitnessDigest = routeWitnessDigest ?? string.Empty;
            RouteType = routeType;
            AccessClass = accessClass;
        }

        public ActivityOverlaySnapshotKind Kind { get; }
        public IReadOnlyList<string> OverlayIdentities => overlayIdentities;
        public string StaticShellDigest { get; }
        public string WorkingCanvasDigest { get; }
        public string TraversalDigest { get; }
        public string RouteWitnessDigest { get; }
        public int RouteType { get; }
        public AccessClass AccessClass { get; }
        public int UnderlyingTileDeltaCount => 0;
        public int GeometryWriteCount => 0;
        public int GeometryCarveCount => 0;
        public int RendererInvocationCount => 0;
    }

    public sealed class ActivitySafePocketProof
    {
        internal ActivitySafePocketProof(
            LocalTileCoord sourceCoordinate,
            LocalTileCoord compiledCoordinate,
            string witnessKind,
            string witnessId)
        {
            SourceCoordinate = sourceCoordinate;
            CompiledCoordinate = compiledCoordinate;
            WitnessKind = witnessKind ?? string.Empty;
            WitnessId = witnessId ?? string.Empty;
        }

        public LocalTileCoord SourceCoordinate { get; }
        public LocalTileCoord CompiledCoordinate { get; }
        public string WitnessKind { get; }
        public string WitnessId { get; }
        public TerrainClusterShellOccupancy OccupancyBeforeRemoval => TerrainClusterShellOccupancy.Air;
        public TerrainClusterShellOccupancy OccupancyAfterRemoval => TerrainClusterShellOccupancy.Air;
        public bool ConnectedToPublishedOpenEvidence => WitnessKind.Length != 0 && WitnessId.Length != 0;
    }

    public sealed class ActivityRecoverySafetyProof
    {
        private readonly ReadOnlyCollection<string> sourceEdgeIds;

        internal ActivityRecoverySafetyProof(
            LocalTileCoord sourceCoordinate,
            LocalTileCoord compiledCoordinate,
            TerrainClusterRecoveryRouteWitness witness)
        {
            SourceCoordinate = sourceCoordinate;
            CompiledCoordinate = compiledCoordinate;
            HighRouteId = witness.HighRouteId;
            FailureNodeId = witness.FailureNodeId;
            TargetBaselineNodeId = witness.TargetBaselineNodeId;
            EstimatedDurationMilliseconds = witness.TotalEstimatedDurationMilliseconds;
            sourceEdgeIds = new ReadOnlyCollection<string>(witness.OrderedEdges
                .Select(value => value.EdgeId).ToArray());
        }

        public LocalTileCoord SourceCoordinate { get; }
        public LocalTileCoord CompiledCoordinate { get; }
        public string HighRouteId { get; }
        public string FailureNodeId { get; }
        public string TargetBaselineNodeId { get; }
        public IReadOnlyList<string> SourceEdgeIds => sourceEdgeIds;
        public int EstimatedDurationMilliseconds { get; }
        public bool UsesSourceEdgesOnly => true;
        public int SyntheticEdgeCount => 0;
        public int TeleportEdgeCount => 0;
    }

    public enum ActivityCriticalTargetKind
    {
        MandatoryExit = 1,
        Reward = 2,
    }

    public sealed class ActivityCriticalTargetEvidence
    {
        public ActivityCriticalTargetEvidence(
            ActivityCriticalTargetKind kind,
            string targetId,
            LocalTileCoord sourceCoordinate,
            string roleOrBindingId,
            string traversalNodeId)
        {
            Kind = kind;
            TargetId = targetId ?? string.Empty;
            SourceCoordinate = sourceCoordinate;
            RoleOrBindingId = roleOrBindingId ?? string.Empty;
            TraversalNodeId = traversalNodeId ?? string.Empty;
        }

        public ActivityCriticalTargetKind Kind { get; }
        public string TargetId { get; }
        public LocalTileCoord SourceCoordinate { get; }
        public string RoleOrBindingId { get; }
        public string TraversalNodeId { get; }
    }

    public sealed class ActivityCriticalPreservationProof
    {
        internal ActivityCriticalPreservationProof(
            ActivityCriticalTargetEvidence evidence,
            LocalTileCoord compiledCoordinate,
            string underlyingIdentityDigest)
        {
            Kind = evidence.Kind;
            TargetId = evidence.TargetId;
            SourceCoordinate = evidence.SourceCoordinate;
            CompiledCoordinate = compiledCoordinate;
            RoleOrBindingId = evidence.RoleOrBindingId;
            TraversalNodeId = evidence.TraversalNodeId;
            UnderlyingIdentityDigestBeforeRemoval = underlyingIdentityDigest ?? string.Empty;
            UnderlyingIdentityDigestAfterRemoval = underlyingIdentityDigest ?? string.Empty;
        }

        public ActivityCriticalTargetKind Kind { get; }
        public string TargetId { get; }
        public LocalTileCoord SourceCoordinate { get; }
        public LocalTileCoord CompiledCoordinate { get; }
        public string RoleOrBindingId { get; }
        public string TraversalNodeId { get; }
        public string UnderlyingIdentityDigestBeforeRemoval { get; }
        public string UnderlyingIdentityDigestAfterRemoval { get; }
        public bool IsPreserved => string.Equals(
            UnderlyingIdentityDigestBeforeRemoval,
            UnderlyingIdentityDigestAfterRemoval,
            StringComparison.Ordinal);
    }

    public sealed class ActivityRemovalSafetyProof
    {
        private readonly ReadOnlyCollection<ActivityCueObservationProof> cueProofs;
        private readonly ReadOnlyCollection<ActivitySafePocketProof> safePocketProofs;
        private readonly ReadOnlyCollection<ActivityRecoverySafetyProof> recoveryProofs;
        private readonly ReadOnlyCollection<ActivityCriticalPreservationProof> criticalTargetProofs;

        internal ActivityRemovalSafetyProof(
            ActivityStructureId activityId,
            TerrainClusterId clusterId,
            SpineVariantId variantId,
            string activityShellDigest,
            ActivityOverlaySnapshot activeSnapshot,
            ActivityOverlaySnapshot removedSnapshot,
            IEnumerable<ActivityCueObservationProof> cueProofs,
            IEnumerable<ActivitySafePocketProof> safePocketProofs,
            IEnumerable<ActivityRecoverySafetyProof> recoveryProofs,
            IEnumerable<ActivityCriticalPreservationProof> criticalTargetProofs,
            string canonicalDigest)
        {
            ActivityId = activityId;
            ClusterId = clusterId;
            VariantId = variantId;
            ActivityShellDigest = activityShellDigest ?? string.Empty;
            ActiveSnapshot = activeSnapshot;
            RemovedSnapshot = removedSnapshot;
            this.cueProofs = new ReadOnlyCollection<ActivityCueObservationProof>(
                (cueProofs ?? Array.Empty<ActivityCueObservationProof>())
                    .OrderBy(value => value.CueId, StringComparer.Ordinal).ToArray());
            this.safePocketProofs = new ReadOnlyCollection<ActivitySafePocketProof>(
                (safePocketProofs ?? Array.Empty<ActivitySafePocketProof>())
                    .OrderBy(value => value.SourceCoordinate.Y)
                    .ThenBy(value => value.SourceCoordinate.X).ToArray());
            this.recoveryProofs = new ReadOnlyCollection<ActivityRecoverySafetyProof>(
                (recoveryProofs ?? Array.Empty<ActivityRecoverySafetyProof>())
                    .OrderBy(value => value.SourceCoordinate.Y)
                    .ThenBy(value => value.SourceCoordinate.X).ToArray());
            this.criticalTargetProofs = new ReadOnlyCollection<ActivityCriticalPreservationProof>(
                (criticalTargetProofs ?? Array.Empty<ActivityCriticalPreservationProof>())
                    .OrderBy(value => value.Kind).ToArray());
            CanonicalDigest = canonicalDigest ?? string.Empty;
        }

        public ActivityStructureId ActivityId { get; }
        public TerrainClusterId ClusterId { get; }
        public SpineVariantId VariantId { get; }
        public string ActivityShellDigest { get; }
        public ActivityOverlaySnapshot ActiveSnapshot { get; }
        public ActivityOverlaySnapshot RemovedSnapshot { get; }
        public IReadOnlyList<ActivityCueObservationProof> CueProofs => cueProofs;
        public IReadOnlyList<ActivitySafePocketProof> SafePocketProofs => safePocketProofs;
        public IReadOnlyList<ActivityRecoverySafetyProof> RecoveryProofs => recoveryProofs;
        public IReadOnlyList<ActivityCriticalPreservationProof> CriticalTargetProofs => criticalTargetProofs;
        public int ResidualOverlayCount => RemovedSnapshot == null ? 0 : RemovedSnapshot.OverlayIdentities.Count;
        public int UnderlyingTileDeltaCount => 0;
        public int RendererInvocationCount => 0;
        public int GeometryWriteCount => 0;
        public int GeometryCarveCount => 0;
        public int RngDrawCount => 0;
        public string CanonicalDigest { get; }
    }
}
