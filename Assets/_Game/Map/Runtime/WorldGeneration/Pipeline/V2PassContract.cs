using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Pipeline
{
    public enum V2WorldGenerationPassId
    {
        Pacing,
        SpecialRegionReservation,
        TerrainClusterReservation,
        RouteSpine,
        TraversalEnvelope,
        MicroPattern,
        TerrainCleanup,
        ActivityEventOverlay,
        TileValidation,
        MicroChunkSlice,
    }

    public enum V2WorldGenerationArtifactId
    {
        ApprovedMapBaseline,
        PacingPlan,
        SpecialRegionReservationPlan,
        TerrainClusterPlacementPlan,
        RouteSpinePlan,
        TraversalEnvelopePlan,
        PatternApplicationPlan,
        CleanTerrainCanvas,
        ActivityEventPlacementPlan,
        ValidatedSectorCanvas,
        GeneratedMicroChunkSlices,
    }

    public enum V2FailureOwner
    {
        PacingPlanner,
        SpecialRegionPlanner,
        TerrainClusterPlanner,
        RouteSpinePlanner,
        TraversalEnvelopePlanner,
        MicroPatternPlanner,
        TerrainCleanupPlanner,
        ActivityEventPlanner,
        TileValidator,
        MicroChunkSlicer,
        CatalogConfiguration,
        AuthoringSchema,
        ApprovedBaseline,
    }

    public enum V2FailurePolicy
    {
        ImmediateFailure,
        ReselectWithinScope,
        OrderedEscalation,
    }

    public enum V2RetryScope
    {
        None,
        Pattern,
        Cluster,
        Footprint,
    }

    public enum V2RngStreamId
    {
        None,
        Pacing,
        SpecialRegionReservation,
        TerrainClusterReservation,
        RouteSpine,
        MicroPattern,
        ActivityEventOverlay,
    }

    public enum V2InfrastructureFailureKind
    {
        Configuration,
        Schema,
        Baseline,
    }

    public sealed class V2InfrastructureFailureRule
    {
        public V2InfrastructureFailureRule(
            V2InfrastructureFailureKind kind,
            V2FailureOwner owner)
        {
            Kind = kind;
            Owner = owner;
        }

        public V2InfrastructureFailureKind Kind { get; }
        public V2FailureOwner Owner { get; }
        public V2FailurePolicy Policy => V2FailurePolicy.ImmediateFailure;
        public V2RetryScope RetryScope => V2RetryScope.None;
        public bool AllowsSilentFallback => false;
    }

    public sealed class V2PassContract
    {
        private readonly ReadOnlyCollection<V2WorldGenerationArtifactId> inputArtifactIds;
        private readonly ReadOnlyCollection<V2WorldGenerationArtifactId> outputArtifactIds;
        private readonly ReadOnlyCollection<V2RetryScope> retryEscalation;

        public V2PassContract(
            V2WorldGenerationPassId passId,
            int order,
            IEnumerable<V2WorldGenerationArtifactId> inputs,
            IEnumerable<V2WorldGenerationArtifactId> outputs,
            V2FailureOwner failureOwner,
            V2FailurePolicy failurePolicy,
            V2RetryScope retryScope,
            IEnumerable<V2RetryScope> escalation,
            V2RngStreamId rngStream,
            string descriptionId,
            bool preservesValidatedCanvasOnFailure = false)
        {
            if (inputs == null) throw new ArgumentNullException(nameof(inputs));
            if (outputs == null) throw new ArgumentNullException(nameof(outputs));
            if (escalation == null) throw new ArgumentNullException(nameof(escalation));
            if (string.IsNullOrWhiteSpace(descriptionId))
                throw new ArgumentException("A runtime/editor-neutral description ID is required.", nameof(descriptionId));

            PassId = passId;
            Order = order;
            inputArtifactIds = new ReadOnlyCollection<V2WorldGenerationArtifactId>(inputs.ToArray());
            outputArtifactIds = new ReadOnlyCollection<V2WorldGenerationArtifactId>(outputs.ToArray());
            FailureOwner = failureOwner;
            FailurePolicy = failurePolicy;
            RetryScope = retryScope;
            retryEscalation = new ReadOnlyCollection<V2RetryScope>(escalation.ToArray());
            RngStream = rngStream;
            DescriptionId = descriptionId;
            PreservesValidatedCanvasOnFailure = preservesValidatedCanvasOnFailure;
        }

        public V2WorldGenerationPassId PassId { get; }
        public int Order { get; }
        public IReadOnlyList<V2WorldGenerationArtifactId> InputArtifactIds => inputArtifactIds;
        public IReadOnlyList<V2WorldGenerationArtifactId> OutputArtifactIds => outputArtifactIds;
        public V2FailureOwner FailureOwner { get; }
        public V2FailurePolicy FailurePolicy { get; }
        public V2RetryScope RetryScope { get; }
        public IReadOnlyList<V2RetryScope> RetryEscalation => retryEscalation;
        public V2RngStreamId RngStream { get; }
        public bool UsesDeterministicRng => RngStream != V2RngStreamId.None;
        public string DescriptionId { get; }
        public bool AllowsSilentFallback => false;
        public bool PreservesValidatedCanvasOnFailure { get; }

        public V2PassContract WithDescriptionId(string descriptionId)
        {
            return new V2PassContract(
                PassId,
                Order,
                inputArtifactIds,
                outputArtifactIds,
                FailureOwner,
                FailurePolicy,
                RetryScope,
                retryEscalation,
                RngStream,
                descriptionId,
                PreservesValidatedCanvasOnFailure);
        }
    }
}
