using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.WorldGeneration.Validation
{
    public enum GeneratedDistanceRouteClass
    {
        MinimumCritical = 1,
        NormalCompletion = 2,
        OptionalCompletion = 3,
        WorstCaseCompletion = 4,
    }

    public enum GeneratedPacingMarkerKind
    {
        MandatoryResource = 1,
        Activity = 2,
        EventOverlay = 3,
        SpecialEntry = 4,
        SpecialReward = 5,
        Village = 6,
        Forge = 7,
        Seal = 8,
        Boss = 9,
        Exit = 10,
    }

    public sealed class GeneratedDistanceRange
    {
        public GeneratedDistanceRange(decimal minimum, decimal maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }
        public decimal Minimum { get; }
        public decimal Maximum { get; }
        public bool Contains(decimal value) => value >= Minimum && value <= Maximum;
        public string StableToken => Number(Minimum) + ".." + Number(Maximum);
        internal static string Number(decimal value) => value.ToString(
            "0.############################", CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedDistancePacingThresholdRegistry
    {
        private GeneratedDistancePacingThresholdRegistry()
        {
            MinimumCritical = new GeneratedDistanceRange(500m, 900m);
            NormalCompletion = new GeneratedDistanceRange(800m, 1400m);
            OptionalCompletion = new GeneratedDistanceRange(1500m, 2800m);
            RepeatedCorridorMaximumBasisPoints = 3500;
            SameKindPacingMinimumGap = 50m;
            Digest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_07_DISTANCE_PACING_THRESHOLD_REGISTRY_V1",
                MinimumCritical.StableToken, NormalCompletion.StableToken,
                OptionalCompletion.StableToken,
                RepeatedCorridorMaximumBasisPoints.ToString(CultureInfo.InvariantCulture),
                GeneratedDistanceRange.Number(SameKindPacingMinimumGap),
            });
        }
        public GeneratedDistanceRange MinimumCritical { get; }
        public GeneratedDistanceRange NormalCompletion { get; }
        public GeneratedDistanceRange OptionalCompletion { get; }
        public int RepeatedCorridorMaximumBasisPoints { get; }
        public decimal SameKindPacingMinimumGap { get; }
        public string Digest { get; }
        public static GeneratedDistancePacingThresholdRegistry CreateLocked() =>
            new GeneratedDistancePacingThresholdRegistry();
        public GeneratedDistanceRange Range(GeneratedDistanceRouteClass routeClass)
        {
            switch (routeClass)
            {
                case GeneratedDistanceRouteClass.MinimumCritical: return MinimumCritical;
                case GeneratedDistanceRouteClass.NormalCompletion: return NormalCompletion;
                case GeneratedDistanceRouteClass.OptionalCompletion: return OptionalCompletion;
                default: return null;
            }
        }
    }

    public sealed class GeneratedDistanceRouteStep : IComparable<GeneratedDistanceRouteStep>
    {
        public GeneratedDistanceRouteStep(string routeId, int ordinal,
            GeneratedTileMovementEdge edge, decimal tileStepMultiplier,
            string corridorSignature, string sourceOwner, string localDirectionBand,
            bool deliberateReturnPath, string sourceDigest)
            : this(routeId, ordinal, edge, null, tileStepMultiplier, corridorSignature,
                sourceOwner, localDirectionBand, deliberateReturnPath, sourceDigest)
        {
        }

        public GeneratedDistanceRouteStep(string routeId, int ordinal,
            GeneratedIntersectorSocketLink socketLink, decimal tileStepMultiplier,
            string corridorSignature, string sourceOwner, string localDirectionBand,
            bool deliberateReturnPath, string sourceDigest)
            : this(routeId, ordinal, null, socketLink, tileStepMultiplier,
                corridorSignature, sourceOwner, localDirectionBand,
                deliberateReturnPath, sourceDigest)
        {
        }

        private GeneratedDistanceRouteStep(string routeId, int ordinal,
            GeneratedTileMovementEdge edge, GeneratedIntersectorSocketLink socketLink,
            decimal tileStepMultiplier, string corridorSignature, string sourceOwner,
            string localDirectionBand, bool deliberateReturnPath, string sourceDigest)
        {
            RouteId = Normalize(routeId);
            Ordinal = ordinal;
            Edge = edge;
            SocketLink = socketLink;
            TileStepMultiplier = tileStepMultiplier;
            CorridorSignature = Normalize(corridorSignature);
            SourceOwner = Normalize(sourceOwner);
            LocalDirectionBand = Normalize(localDirectionBand);
            DeliberateReturnPath = deliberateReturnPath;
            SourceDigest = Normalize(sourceDigest);
            StepId = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_07_ROUTE_STEP_ID_V1", RouteId, Number(Ordinal),
                TraversalId,
                GeneratedDistanceRange.Number(TileStepMultiplier), CorridorSignature,
                SourceOwner, LocalDirectionBand, DeliberateReturnPath ? "1" : "0",
                SourceDigest,
            });
        }
        public string StepId { get; }
        public string RouteId { get; }
        public int Ordinal { get; }
        public GeneratedTileMovementEdge Edge { get; }
        public GeneratedIntersectorSocketLink SocketLink { get; }
        public decimal TileStepMultiplier { get; }
        public string CorridorSignature { get; }
        public string SourceOwner { get; }
        public string LocalDirectionBand { get; }
        public bool DeliberateReturnPath { get; }
        public string SourceDigest { get; }
        public string FromNodeId => Edge != null ? Edge.FromNodeId : SocketLink != null ?
            SocketLink.FromSocketNodeId : "MISSING";
        public string ToNodeId => Edge != null ? Edge.ToNodeId : SocketLink != null ?
            SocketLink.ToSocketNodeId : "MISSING";
        public string TraversalId => Edge != null ? Edge.EdgeId : SocketLink != null ?
            SocketLink.LinkId : "MISSING_TRAVERSAL";
        public decimal TileStepEquivalentCost => Edge != null ?
            Edge.Cost * TileStepMultiplier : SocketLink != null ?
                1m * TileStepMultiplier : 0m;
        public string StableToken => string.Join("|", new[]
        {
            "MAP19_07_ROUTE_STEP_V1", StepId, RouteId, Number(Ordinal),
            Edge != null ? Edge.StableToken : SocketLink != null ?
                SocketLink.StableToken : "MISSING_TRAVERSAL",
            GeneratedDistanceRange.Number(TileStepMultiplier), CorridorSignature,
            SourceOwner, LocalDirectionBand, DeliberateReturnPath ? "1" : "0",
            SourceDigest,
        });
        public int CompareTo(GeneratedDistanceRouteStep other)
        {
            if (other == null) return -1;
            var value = string.Compare(RouteId, other.RouteId, StringComparison.Ordinal);
            return value != 0 ? value : Ordinal.CompareTo(other.Ordinal);
        }
        internal static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? "MISSING" : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
        internal static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedDistanceRouteBinding :
        IComparable<GeneratedDistanceRouteBinding>
    {
        private readonly ReadOnlyCollection<GeneratedDistanceRouteStep> steps;
        public GeneratedDistanceRouteBinding(GeneratedDistanceRouteClass routeClass,
            string routeId, string startNodeId, string exitNodeId,
            IEnumerable<GeneratedDistanceRouteStep> sourceSteps, string sourceOwner,
            string sourceDigest, string completionProofDigest,
            GeneratedWorstCaseScenarioProof worstCaseProof = null)
        {
            RouteClass = routeClass;
            RouteId = GeneratedDistanceRouteStep.Normalize(routeId);
            StartNodeId = GeneratedDistanceRouteStep.Normalize(startNodeId);
            ExitNodeId = GeneratedDistanceRouteStep.Normalize(exitNodeId);
            steps = new ReadOnlyCollection<GeneratedDistanceRouteStep>((sourceSteps ??
                Array.Empty<GeneratedDistanceRouteStep>()).Where(value => value != null)
                .OrderBy(value => value.Ordinal).ThenBy(value => value.StepId,
                    StringComparer.Ordinal).ToArray());
            SourceOwner = GeneratedDistanceRouteStep.Normalize(sourceOwner);
            SourceDigest = GeneratedDistanceRouteStep.Normalize(sourceDigest);
            CompletionProofDigest = GeneratedDistanceRouteStep.Normalize(completionProofDigest);
            WorstCaseProof = worstCaseProof;
        }
        public GeneratedDistanceRouteClass RouteClass { get; }
        public string RouteId { get; }
        public string StartNodeId { get; }
        public string ExitNodeId { get; }
        public IReadOnlyList<GeneratedDistanceRouteStep> Steps => steps;
        public string SourceOwner { get; }
        public string SourceDigest { get; }
        public string CompletionProofDigest { get; }
        public GeneratedWorstCaseScenarioProof WorstCaseProof { get; }
        public string StableToken => string.Join("|", new[]
        {
            "MAP19_07_ROUTE_BINDING_V1",
            GeneratedDistanceRouteStep.Number((int)RouteClass), RouteId,
            StartNodeId, ExitNodeId, SourceOwner, SourceDigest,
            CompletionProofDigest, WorstCaseProof == null ? "NO_WORST_CASE" :
                WorstCaseProof.ProofDigest,
        }.Concat(steps.Select(value => value.StableToken)));
        public int CompareTo(GeneratedDistanceRouteBinding other) => other == null ? -1 :
            RouteClass != other.RouteClass ? RouteClass.CompareTo(other.RouteClass) :
            string.Compare(RouteId, other.RouteId, StringComparison.Ordinal);
    }

    public sealed class GeneratedDistancePacingMarker :
        IComparable<GeneratedDistancePacingMarker>
    {
        public GeneratedDistancePacingMarker(string markerId,
            GeneratedPacingMarkerKind kind, string routeId, string stepId,
            decimal offsetWithinStep, string pacingBand, string sourceOwner,
            string sourceDigest, bool requiredForCompletion,
            GeneratedCompletionStateTransition completionTransition = null,
            GeneratedRepetitionSignature repetitionSignature = null,
            VillageStateMarkerSetDefinition villageStateMarkers = null)
        {
            MarkerId = GeneratedDistanceRouteStep.Normalize(markerId);
            Kind = kind;
            RouteId = GeneratedDistanceRouteStep.Normalize(routeId);
            StepId = GeneratedDistanceRouteStep.Normalize(stepId);
            OffsetWithinStep = offsetWithinStep;
            PacingBand = GeneratedDistanceRouteStep.Normalize(pacingBand);
            SourceOwner = GeneratedDistanceRouteStep.Normalize(sourceOwner);
            SourceDigest = GeneratedDistanceRouteStep.Normalize(sourceDigest);
            RequiredForCompletion = requiredForCompletion;
            CompletionTransition = completionTransition;
            RepetitionSignature = repetitionSignature;
            VillageStateMarkers = villageStateMarkers;
        }
        public string MarkerId { get; }
        public GeneratedPacingMarkerKind Kind { get; }
        public string RouteId { get; }
        public string StepId { get; }
        public decimal OffsetWithinStep { get; }
        public string PacingBand { get; }
        public string SourceOwner { get; }
        public string SourceDigest { get; }
        public bool RequiredForCompletion { get; }
        public GeneratedCompletionStateTransition CompletionTransition { get; }
        public GeneratedRepetitionSignature RepetitionSignature { get; }
        public VillageStateMarkerSetDefinition VillageStateMarkers { get; }
        public string ContractIdentity => CompletionTransition != null
            ? CompletionTransition.TransitionId
            : RepetitionSignature != null ? RepetitionSignature.SignatureId
            : VillageStateMarkers != null
                ? GeneratedWorstCaseScenarioInput.VillageToken(VillageStateMarkers)
                : Kind == GeneratedPacingMarkerKind.Exit ? "COMPLETION_EXIT_GOAL" :
                    "MISSING_BINDING";
        public string StableToken => string.Join("|", new[]
        {
            "MAP19_07_PACING_MARKER_V1", MarkerId,
            GeneratedDistanceRouteStep.Number((int)Kind), RouteId, StepId,
            GeneratedDistanceRange.Number(OffsetWithinStep), PacingBand,
            SourceOwner, SourceDigest, RequiredForCompletion ? "1" : "0",
            ContractIdentity,
        });
        public int CompareTo(GeneratedDistancePacingMarker other) => other == null ? -1 :
            Kind != other.Kind ? Kind.CompareTo(other.Kind) :
            string.Compare(MarkerId, other.MarkerId, StringComparison.Ordinal);
    }

    public sealed class GeneratedDistancePacingActionAudit
    {
        public GeneratedDistancePacingActionAudit(int graphMutationAttempts = 0,
            int proofRewriteAttempts = 0, int runtimeQueryAttempts = 0,
            int runtimeMutationAttempts = 0, int seedBatchAttempts = 0,
            int failureBundleAttempts = 0, int headlessRunnerAttempts = 0,
            bool map19_08Started = false)
        {
            GraphMutationAttempts = graphMutationAttempts;
            ProofRewriteAttempts = proofRewriteAttempts;
            RuntimeQueryAttempts = runtimeQueryAttempts;
            RuntimeMutationAttempts = runtimeMutationAttempts;
            SeedBatchAttempts = seedBatchAttempts;
            FailureBundleAttempts = failureBundleAttempts;
            HeadlessRunnerAttempts = headlessRunnerAttempts;
            Map19_08Started = map19_08Started;
        }
        public int GraphMutationAttempts { get; }
        public int ProofRewriteAttempts { get; }
        public int RuntimeQueryAttempts { get; }
        public int RuntimeMutationAttempts { get; }
        public int SeedBatchAttempts { get; }
        public int FailureBundleAttempts { get; }
        public int HeadlessRunnerAttempts { get; }
        public bool Map19_08Started { get; }
        public bool IsZero => GraphMutationAttempts == 0 && ProofRewriteAttempts == 0 &&
            RuntimeQueryAttempts == 0 && RuntimeMutationAttempts == 0 &&
            SeedBatchAttempts == 0 && FailureBundleAttempts == 0 &&
            HeadlessRunnerAttempts == 0 && !Map19_08Started;
        public string StableToken => string.Join("|", new[]
        {
            "MAP19_07_ACTION_AUDIT_V1", Number(GraphMutationAttempts),
            Number(ProofRewriteAttempts), Number(RuntimeQueryAttempts),
            Number(RuntimeMutationAttempts), Number(SeedBatchAttempts),
            Number(FailureBundleAttempts), Number(HeadlessRunnerAttempts),
            Map19_08Started ? "1" : "0",
        });
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedDistancePacingInput
    {
        private readonly ReadOnlyCollection<GeneratedDistanceRouteBinding> routes;
        private readonly ReadOnlyCollection<GeneratedDistancePacingMarker> markers;
        public GeneratedDistancePacingInput(GeneratedTileMovementGraph graph,
            GeneratedCompletionSearchResult completionSearch,
            ClusterRecoveryValidationResult clusterValidation,
            GeneratedRepetitionValidationResult repetitionValidation,
            GeneratedWorstCaseScenarioResult worstCaseValidation,
            GeneratedDistancePacingThresholdRegistry thresholdRegistry,
            IEnumerable<GeneratedDistanceRouteBinding> sourceRoutes,
            IEnumerable<GeneratedDistancePacingMarker> sourceMarkers,
            GeneratedDistancePacingActionAudit actionAudit = null,
            string declaredMap19_04CombinedDigest = null,
            string declaredMap19_05CombinedDigest = null,
            string declaredMap19_06CombinedDigest = null,
            string declaredIncomingHandoffDigest = null)
        {
            Graph = graph;
            CompletionSearch = completionSearch;
            ClusterValidation = clusterValidation;
            RepetitionValidation = repetitionValidation;
            WorstCaseValidation = worstCaseValidation;
            ThresholdRegistry = thresholdRegistry;
            routes = new ReadOnlyCollection<GeneratedDistanceRouteBinding>((sourceRoutes ??
                Array.Empty<GeneratedDistanceRouteBinding>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
            markers = new ReadOnlyCollection<GeneratedDistancePacingMarker>((sourceMarkers ??
                Array.Empty<GeneratedDistancePacingMarker>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
            ActionAudit = actionAudit ?? new GeneratedDistancePacingActionAudit();
            DeclaredMap19_04CombinedDigest = declaredMap19_04CombinedDigest ?? Digest(
                clusterValidation, value => value.CombinedSuccessDigest);
            DeclaredMap19_05CombinedDigest = declaredMap19_05CombinedDigest ?? Digest(
                repetitionValidation, value => value.CombinedSuccessDigest);
            DeclaredMap19_06CombinedDigest = declaredMap19_06CombinedDigest ?? Digest(
                worstCaseValidation, value => value.CombinedSuccessDigest);
            DeclaredIncomingHandoffDigest = declaredIncomingHandoffDigest ?? Digest(
                worstCaseValidation, value => value.Map19_07HandoffDigest);
        }
        public GeneratedTileMovementGraph Graph { get; }
        public GeneratedCompletionSearchResult CompletionSearch { get; }
        public ClusterRecoveryValidationResult ClusterValidation { get; }
        public GeneratedRepetitionValidationResult RepetitionValidation { get; }
        public GeneratedWorstCaseScenarioResult WorstCaseValidation { get; }
        public GeneratedDistancePacingThresholdRegistry ThresholdRegistry { get; }
        public IReadOnlyList<GeneratedDistanceRouteBinding> Routes => routes;
        public IReadOnlyList<GeneratedDistancePacingMarker> Markers => markers;
        public GeneratedDistancePacingActionAudit ActionAudit { get; }
        public string DeclaredMap19_04CombinedDigest { get; }
        public string DeclaredMap19_05CombinedDigest { get; }
        public string DeclaredMap19_06CombinedDigest { get; }
        public string DeclaredIncomingHandoffDigest { get; }
        public string ComputeDigest() => BakingCanonicalDigest.HashCanonicalLines(new[]
        {
            "MAP19_07_DISTANCE_PACING_INPUT_V1",
            Graph == null ? "MISSING_GRAPH" : Graph.GraphDigest,
            CompletionSearch == null ? "MISSING_COMPLETION" :
                CompletionSearch.SuccessProofDigest,
            ClusterValidation == null ? "MISSING_MAP19_04" :
                ClusterValidation.CombinedSuccessDigest,
            RepetitionValidation == null ? "MISSING_MAP19_05" :
                RepetitionValidation.CombinedSuccessDigest,
            WorstCaseValidation == null ? "MISSING_MAP19_06" :
                WorstCaseValidation.CombinedSuccessDigest,
            DeclaredMap19_04CombinedDigest, DeclaredMap19_05CombinedDigest,
            DeclaredMap19_06CombinedDigest, DeclaredIncomingHandoffDigest,
            ThresholdRegistry == null ? "MISSING_REGISTRY" : ThresholdRegistry.Digest,
            ActionAudit == null ? "MISSING_AUDIT" : ActionAudit.StableToken,
        }.Concat(routes.Select(value => value.StableToken))
         .Concat(markers.Select(value => value.StableToken)));
        private static string Digest<T>(T value, Func<T, string> selector) where T : class =>
            value == null ? "MISSING" : selector(value);
    }

    public sealed class GeneratedDistanceRouteProof : IComparable<GeneratedDistanceRouteProof>
    {
        internal GeneratedDistanceRouteProof(GeneratedDistanceRouteBinding route,
            decimal distance, int uniqueNodes, int totalNodeVisits, int revisitedNodes,
            int uniqueEdges, int totalEdgeVisits, int repeatedEdges,
            int repeatedCorridorRatioBasisPoints)
        {
            RouteClass = route.RouteClass;
            RouteId = route.RouteId;
            Distance = distance;
            UniqueRouteNodes = uniqueNodes;
            TotalRouteNodeVisits = totalNodeVisits;
            RevisitedNodeCount = revisitedNodes;
            UniqueRouteEdges = uniqueEdges;
            TotalRouteEdgeVisits = totalEdgeVisits;
            RepeatedCorridorEdgeCount = repeatedEdges;
            RepeatedCorridorRatioBasisPoints = repeatedCorridorRatioBasisPoints;
            ProofDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_07_DISTANCE_ROUTE_PROOF_V1",
                GeneratedDistanceRouteStep.Number((int)RouteClass), RouteId,
                GeneratedDistanceRange.Number(Distance), Number(UniqueRouteNodes),
                Number(TotalRouteNodeVisits), Number(RevisitedNodeCount),
                Number(UniqueRouteEdges), Number(TotalRouteEdgeVisits),
                Number(RepeatedCorridorEdgeCount),
                Number(RepeatedCorridorRatioBasisPoints), route.StableToken,
            });
        }
        public GeneratedDistanceRouteClass RouteClass { get; }
        public string RouteId { get; }
        public decimal Distance { get; }
        public int UniqueRouteNodes { get; }
        public int TotalRouteNodeVisits { get; }
        public int RevisitedNodeCount { get; }
        public int RevisitRatioBasisPoints => Ratio(RevisitedNodeCount,
            TotalRouteNodeVisits);
        public int UniqueRouteEdges { get; }
        public int TotalRouteEdgeVisits { get; }
        public int RepeatedCorridorEdgeCount { get; }
        public int RepeatedCorridorRatioBasisPoints { get; }
        public string ProofDigest { get; }
        public int CompareTo(GeneratedDistanceRouteProof other) => other == null ? -1 :
            RouteClass != other.RouteClass ? RouteClass.CompareTo(other.RouteClass) :
            string.Compare(RouteId, other.RouteId, StringComparison.Ordinal);
        internal static int Ratio(int numerator, int denominator) => denominator <= 0 ? 0 :
            (int)Math.Round(numerator * 10000m / denominator, 0,
                MidpointRounding.AwayFromZero);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedPacingMarkerProof : IComparable<GeneratedPacingMarkerProof>
    {
        internal GeneratedPacingMarkerProof(GeneratedDistancePacingMarker marker,
            decimal routeDistance, decimal previousGap, decimal nextGap)
        {
            Marker = marker;
            RouteDistance = routeDistance;
            PreviousMarkerGap = previousGap;
            NextMarkerGap = nextGap;
            ProofDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_07_PACING_MARKER_PROOF_V1", marker.StableToken,
                GeneratedDistanceRange.Number(routeDistance),
                GeneratedDistanceRange.Number(previousGap),
                GeneratedDistanceRange.Number(nextGap),
            });
        }
        public GeneratedDistancePacingMarker Marker { get; }
        public decimal RouteDistance { get; }
        public decimal PreviousMarkerGap { get; }
        public decimal NextMarkerGap { get; }
        public string ProofDigest { get; }
        public int CompareTo(GeneratedPacingMarkerProof other) => other == null ? -1 :
            RouteDistance != other.RouteDistance ? RouteDistance.CompareTo(other.RouteDistance) :
            Marker.CompareTo(other.Marker);
    }

    public sealed class GeneratedDistancePacingFailure :
        IComparable<GeneratedDistancePacingFailure>
    {
        public GeneratedDistancePacingFailure(string owner, string reason,
            string measurementId, GeneratedDistanceRouteClass routeClass,
            string offendingKey, string expected, string actual, string sourceDigest,
            string routeOrMarkerEvidence)
        {
            Owner = Value(owner);
            Reason = Value(reason);
            MeasurementId = Value(measurementId);
            RouteClass = routeClass;
            OffendingKey = Value(offendingKey);
            Expected = Value(expected);
            Actual = Value(actual);
            SourceDigest = Value(sourceDigest);
            RouteOrMarkerEvidence = Value(routeOrMarkerEvidence);
        }
        public string Owner { get; }
        public string Reason { get; }
        public string MeasurementId { get; }
        public GeneratedDistanceRouteClass RouteClass { get; }
        public string OffendingKey { get; }
        public string Expected { get; }
        public string Actual { get; }
        public string SourceDigest { get; }
        public string RouteOrMarkerEvidence { get; }
        public string StableToken => string.Join("|", new[]
        {
            Owner, Reason, MeasurementId,
            GeneratedDistanceRouteStep.Number((int)RouteClass), OffendingKey,
            Expected, Actual, SourceDigest, RouteOrMarkerEvidence,
        });
        public int CompareTo(GeneratedDistancePacingFailure other) => other == null ? -1 :
            string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
        public override string ToString() => StableToken;
        private static string Value(string value) => string.IsNullOrWhiteSpace(value)
            ? "MISSING" : value.Trim();
    }

    public sealed class GeneratedDistancePacingSurface
    {
        private readonly ReadOnlyCollection<GeneratedDistanceRouteProof> routes;
        private readonly ReadOnlyCollection<GeneratedPacingMarkerProof> markers;
        internal GeneratedDistancePacingSurface(GeneratedDistancePacingInput input,
            IEnumerable<GeneratedDistanceRouteProof> sourceRoutes,
            IEnumerable<GeneratedPacingMarkerProof> sourceMarkers)
        {
            routes = new ReadOnlyCollection<GeneratedDistanceRouteProof>((sourceRoutes ??
                Array.Empty<GeneratedDistanceRouteProof>()).OrderBy(value => value).ToArray());
            markers = new ReadOnlyCollection<GeneratedPacingMarkerProof>((sourceMarkers ??
                Array.Empty<GeneratedPacingMarkerProof>()).OrderBy(value => value).ToArray());
            InputDigest = input.ComputeDigest();
            DistanceMeasurementDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_07_DISTANCE_MEASUREMENT_V1", InputDigest,
                input.ThresholdRegistry.Digest,
            }.Concat(routes.Select(value => value.ProofDigest)));
            RevisitMeasurementDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_07_REVISIT_MEASUREMENT_V1", InputDigest,
            }.Concat(routes.Select(value => string.Join("|", new[]
            {
                value.RouteId, value.UniqueRouteNodes.ToString(CultureInfo.InvariantCulture),
                value.TotalRouteNodeVisits.ToString(CultureInfo.InvariantCulture),
                value.RevisitedNodeCount.ToString(CultureInfo.InvariantCulture),
                value.UniqueRouteEdges.ToString(CultureInfo.InvariantCulture),
                value.TotalRouteEdgeVisits.ToString(CultureInfo.InvariantCulture),
                value.RepeatedCorridorEdgeCount.ToString(CultureInfo.InvariantCulture),
                value.RepeatedCorridorRatioBasisPoints.ToString(CultureInfo.InvariantCulture),
            }))));
            PacingMeasurementDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_07_PACING_MEASUREMENT_V1", InputDigest,
            }.Concat(markers.Select(value => value.ProofDigest)));
            CombinedDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_07_DISTANCE_REVISIT_PACING_COMBINED_V1",
                input.Graph.GraphDigest, input.CompletionSearch.SuccessProofDigest,
                input.ClusterValidation.CombinedSuccessDigest,
                input.RepetitionValidation.CombinedSuccessDigest,
                input.WorstCaseValidation.CombinedSuccessDigest,
                input.DeclaredIncomingHandoffDigest, InputDigest,
                DistanceMeasurementDigest, RevisitMeasurementDigest,
                PacingMeasurementDigest,
            });
            Map19_08HandoffDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_08_DISTANCE_PACING_HANDOFF_V1", CombinedDigest,
                DistanceMeasurementDigest, RevisitMeasurementDigest,
                PacingMeasurementDigest, "MAP19_08_LOCKED=1|STARTED=0",
            });
        }
        public IReadOnlyList<GeneratedDistanceRouteProof> Routes => routes;
        public IReadOnlyList<GeneratedPacingMarkerProof> Markers => markers;
        public int RouteClassesMeasured => routes.Select(value => value.RouteClass).Distinct().Count();
        public GeneratedDistanceRouteProof Route(GeneratedDistanceRouteClass routeClass) =>
            routes.Single(value => value.RouteClass == routeClass);
        public GeneratedDistanceRouteProof RevisitReference => Route(
            GeneratedDistanceRouteClass.OptionalCompletion);
        public int PacingMarkersChecked => markers.Count;
        public int MandatoryResourceMarkers => markers.Count(value => value.Marker.Kind ==
            GeneratedPacingMarkerKind.MandatoryResource);
        public int ActivityMarkers => markers.Count(value => value.Marker.Kind ==
            GeneratedPacingMarkerKind.Activity);
        public int EventMarkers => markers.Count(value => value.Marker.Kind ==
            GeneratedPacingMarkerKind.EventOverlay);
        public int SpecialMarkers => markers.Count(value => value.Marker.Kind ==
            GeneratedPacingMarkerKind.SpecialEntry || value.Marker.Kind ==
            GeneratedPacingMarkerKind.SpecialReward);
        public int VillageForgeSealBossExitMarkers => markers.Count(value =>
            value.Marker.Kind >= GeneratedPacingMarkerKind.Village);
        public decimal MinimumMarkerGap => markers.Where(value => value.PreviousMarkerGap > 0)
            .Min(value => value.PreviousMarkerGap);
        public decimal MaximumMarkerGap => markers.Max(value => value.NextMarkerGap);
        public int MarkerBindingFailures => 0;
        public int DistanceRangeViolations => 0;
        public int RepeatedCorridorViolations => 0;
        public int PacingClusterViolations => 0;
        public string InputDigest { get; }
        public string DistanceMeasurementDigest { get; }
        public string RevisitMeasurementDigest { get; }
        public string PacingMeasurementDigest { get; }
        public string CombinedDigest { get; }
        public string Map19_08HandoffDigest { get; }
        public bool WorldScaleSourceAvailable => false;
        public string FocusedFixtureSourceKind => "SCALED_GRAPH_BACKED_FOCUSED_ROUTE_FIXTURE";
        public bool WorldScaleThresholdApprovalPerformed => false;
        public bool Map19_08Started => false;
    }

    public sealed class GeneratedDistancePacingResult
    {
        private readonly ReadOnlyCollection<GeneratedDistancePacingFailure> failures;
        internal GeneratedDistancePacingResult(GeneratedDistancePacingSurface surface,
            IEnumerable<GeneratedDistancePacingFailure> sourceFailures)
        {
            Surface = surface;
            failures = new ReadOnlyCollection<GeneratedDistancePacingFailure>((sourceFailures ??
                Array.Empty<GeneratedDistancePacingFailure>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
        }
        public bool Success => Surface != null && failures.Count == 0;
        public GeneratedDistancePacingSurface Surface { get; }
        public IReadOnlyList<GeneratedDistancePacingFailure> Failures => failures;
        public string DistanceSuccessDigest => Success ? Surface.DistanceMeasurementDigest : string.Empty;
        public string RevisitSuccessDigest => Success ? Surface.RevisitMeasurementDigest : string.Empty;
        public string PacingSuccessDigest => Success ? Surface.PacingMeasurementDigest : string.Empty;
        public string CombinedSuccessDigest => Success ? Surface.CombinedDigest : string.Empty;
        public string Map19_08HandoffDigest => Success ? Surface.Map19_08HandoffDigest : string.Empty;
    }
}
