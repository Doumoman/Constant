using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.WorldGeneration.Validation
{
    public enum ClusterRecoveryCaseKind
    {
        BaseRouteRejoin = 1,
        HighRouteRecovery = 2,
        ExplicitRecoveryRoute = 3,
        TimedRecoveryWindow = 4,
        ClusterEntryToPrimarySpine = 5,
        PrimarySpineToClusterExit = 6,
        BoundarySocketRecovery = 7,
    }

    public sealed class ClusterRecoveryCase : IComparable<ClusterRecoveryCase>
    {
        private readonly ReadOnlyCollection<string> acceptedTargetNodeIds;
        private readonly ReadOnlyCollection<TraversalMovementKind> allowedMovementKinds;

        public ClusterRecoveryCase(
            TerrainClusterRouteWitnessReport routeWitness,
            string sourceOwner,
            string caseId,
            ClusterRecoveryCaseKind kind,
            string startNodeId,
            IEnumerable<string> sourceAcceptedTargetNodeIds,
            IEnumerable<TraversalMovementKind> sourceAllowedMovementKinds,
            int maxRecoveryEdgeCount,
            string expectedWindowClass,
            int recoveryCostMilliseconds = 0,
            int minimumAcceptedCostMilliseconds = 0,
            int maximumAcceptedCostMilliseconds = 0)
        {
            RouteWitness = routeWitness;
            ClusterId = routeWitness == null ? default(TerrainClusterId) : routeWitness.ClusterId;
            SourceOwner = Normalize(sourceOwner);
            SourceDigest = routeWitness == null ? "MISSING" : Normalize(routeWitness.CanonicalDigest);
            CaseId = Normalize(caseId);
            Kind = kind;
            StartNodeId = Normalize(startNodeId);
            acceptedTargetNodeIds = ReadOnlyStrings(sourceAcceptedTargetNodeIds);
            allowedMovementKinds = new ReadOnlyCollection<TraversalMovementKind>((
                    sourceAllowedMovementKinds ?? Array.Empty<TraversalMovementKind>())
                .OrderBy(value => (int)value).ToArray());
            MaxRecoveryEdgeCount = maxRecoveryEdgeCount;
            ExpectedWindowClass = Normalize(expectedWindowClass);
            RecoveryCostMilliseconds = recoveryCostMilliseconds;
            MinimumAcceptedCostMilliseconds = minimumAcceptedCostMilliseconds;
            MaximumAcceptedCostMilliseconds = maximumAcceptedCostMilliseconds;
        }

        public TerrainClusterRouteWitnessReport RouteWitness { get; }
        public TerrainClusterId ClusterId { get; }
        public string SourceOwner { get; }
        public string SourceDigest { get; }
        public string CaseId { get; }
        public ClusterRecoveryCaseKind Kind { get; }
        public string StartNodeId { get; }
        public IReadOnlyList<string> AcceptedTargetNodeIds => acceptedTargetNodeIds;
        public IReadOnlyList<TraversalMovementKind> AllowedMovementKinds => allowedMovementKinds;
        public int MaxRecoveryEdgeCount { get; }
        public string ExpectedWindowClass { get; }
        public int RecoveryCostMilliseconds { get; }
        public int MinimumAcceptedCostMilliseconds { get; }
        public int MaximumAcceptedCostMilliseconds { get; }
        public bool IsTimed => Kind == ClusterRecoveryCaseKind.TimedRecoveryWindow;
        public string StableToken => string.Join("|", new[]
        {
            "CLUSTER_RECOVERY_CASE_V1", ClusterId.Value, SourceOwner, SourceDigest, CaseId,
            Number((int)Kind), StartNodeId, string.Join(",", acceptedTargetNodeIds),
            string.Join(",", allowedMovementKinds.Select(value => Number((int)value))),
            Number(MaxRecoveryEdgeCount), ExpectedWindowClass,
            Number(RecoveryCostMilliseconds), Number(MinimumAcceptedCostMilliseconds),
            Number(MaximumAcceptedCostMilliseconds),
        });

        public int CompareTo(ClusterRecoveryCase other)
        {
            if (other == null) return -1;
            var cluster = ClusterId.CompareTo(other.ClusterId);
            return cluster != 0 ? cluster : string.Compare(CaseId, other.CaseId,
                StringComparison.Ordinal);
        }

        private static ReadOnlyCollection<string> ReadOnlyStrings(IEnumerable<string> source) =>
            new ReadOnlyCollection<string>((source ?? Array.Empty<string>()).Select(Normalize)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
        internal static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? "MISSING" : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
        internal static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class ClusterDensitySource : IComparable<ClusterDensitySource>
    {
        private readonly ReadOnlyCollection<string> acceptedRecoveryTargetNodeIds;

        public ClusterDensitySource(
            TerrainClusterRouteWitnessReport routeWitness,
            string sourceOwner,
            SectorCanvasContract canvas,
            GeneratedSliceSet slices,
            int minimumX,
            int minimumY,
            int maximumX,
            int maximumY,
            int requiredEightBySixAirWindows,
            IEnumerable<string> sourceAcceptedRecoveryTargetNodeIds)
        {
            RouteWitness = routeWitness;
            ClusterId = routeWitness == null ? default(TerrainClusterId) : routeWitness.ClusterId;
            SourceOwner = ClusterRecoveryCase.Normalize(sourceOwner);
            SourceDigest = routeWitness == null ? "MISSING" :
                ClusterRecoveryCase.Normalize(routeWitness.CanonicalDigest);
            Canvas = canvas;
            Slices = slices;
            MinimumX = minimumX;
            MinimumY = minimumY;
            MaximumX = maximumX;
            MaximumY = maximumY;
            RequiredEightBySixAirWindows = requiredEightBySixAirWindows;
            acceptedRecoveryTargetNodeIds = new ReadOnlyCollection<string>((
                    sourceAcceptedRecoveryTargetNodeIds ?? Array.Empty<string>())
                .Select(ClusterRecoveryCase.Normalize)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        public TerrainClusterRouteWitnessReport RouteWitness { get; }
        public TerrainClusterId ClusterId { get; }
        public string SourceOwner { get; }
        public string SourceDigest { get; }
        public SectorCanvasContract Canvas { get; }
        public GeneratedSliceSet Slices { get; }
        public int MinimumX { get; }
        public int MinimumY { get; }
        public int MaximumX { get; }
        public int MaximumY { get; }
        public int RequiredEightBySixAirWindows { get; }
        public IReadOnlyList<string> AcceptedRecoveryTargetNodeIds =>
            acceptedRecoveryTargetNodeIds;
        public int Width => MaximumX - MinimumX + 1;
        public int Height => MaximumY - MinimumY + 1;
        public string StableIdentity => ClusterId.Value + "|" + SourceOwner + "|" + SourceDigest;
        public int CompareTo(ClusterDensitySource other) => other == null ? -1 :
            ClusterId.CompareTo(other.ClusterId);
    }

    public sealed class GeneratedClusterValidationActionAudit
    {
        public GeneratedClusterValidationActionAudit(
            int graphGenerationAttempts = 0,
            int graphMutationAttempts = 0,
            int proofRewriteAttempts = 0,
            int runtimeStateReadAttempts = 0,
            int terrainMutationAttempts = 0,
            int seedBatchAttempts = 0,
            bool map19_05Started = false)
        {
            GraphGenerationAttempts = graphGenerationAttempts;
            GraphMutationAttempts = graphMutationAttempts;
            ProofRewriteAttempts = proofRewriteAttempts;
            RuntimeStateReadAttempts = runtimeStateReadAttempts;
            TerrainMutationAttempts = terrainMutationAttempts;
            SeedBatchAttempts = seedBatchAttempts;
            Map19_05Started = map19_05Started;
        }

        public int GraphGenerationAttempts { get; }
        public int GraphMutationAttempts { get; }
        public int ProofRewriteAttempts { get; }
        public int RuntimeStateReadAttempts { get; }
        public int TerrainMutationAttempts { get; }
        public int SeedBatchAttempts { get; }
        public bool Map19_05Started { get; }
        public bool IsZero => GraphGenerationAttempts == 0 && GraphMutationAttempts == 0 &&
            ProofRewriteAttempts == 0 && RuntimeStateReadAttempts == 0 &&
            TerrainMutationAttempts == 0 && SeedBatchAttempts == 0 && !Map19_05Started;
        public string StableToken => string.Join("|", new[]
        {
            "CLUSTER_VALIDATION_ACTION_AUDIT_V1",
            ClusterRecoveryCase.Number(GraphGenerationAttempts),
            ClusterRecoveryCase.Number(GraphMutationAttempts),
            ClusterRecoveryCase.Number(ProofRewriteAttempts),
            ClusterRecoveryCase.Number(RuntimeStateReadAttempts),
            ClusterRecoveryCase.Number(TerrainMutationAttempts),
            ClusterRecoveryCase.Number(SeedBatchAttempts), Map19_05Started ? "1" : "0",
        });
    }

    public sealed class ClusterRecoveryValidationInput
    {
        private readonly ReadOnlyCollection<ClusterRecoveryCase> cases;

        public ClusterRecoveryValidationInput(
            GeneratedTileMovementGraph graph,
            NakedTraversalSearchResult nakedSearch,
            GeneratedCompletionSearchResult completionSearch,
            IEnumerable<ClusterRecoveryCase> sourceCases,
            GeneratedClusterValidationActionAudit actionAudit = null,
            string declaredCompletionProofDigest = null,
            string declaredIncomingHandoffDigest = null)
        {
            Graph = graph;
            NakedSearch = nakedSearch;
            CompletionSearch = completionSearch;
            cases = new ReadOnlyCollection<ClusterRecoveryCase>((sourceCases ??
                    Array.Empty<ClusterRecoveryCase>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
            ActionAudit = actionAudit ?? new GeneratedClusterValidationActionAudit();
            DeclaredCompletionProofDigest = declaredCompletionProofDigest ??
                (completionSearch == null ? "MISSING" : completionSearch.SuccessProofDigest);
            DeclaredIncomingHandoffDigest = declaredIncomingHandoffDigest ??
                (completionSearch == null ? "MISSING" : completionSearch.Map19_04HandoffDigest);
        }

        public GeneratedTileMovementGraph Graph { get; }
        public NakedTraversalSearchResult NakedSearch { get; }
        public GeneratedCompletionSearchResult CompletionSearch { get; }
        public IReadOnlyList<ClusterRecoveryCase> Cases => cases;
        public GeneratedClusterValidationActionAudit ActionAudit { get; }
        public string DeclaredCompletionProofDigest { get; }
        public string DeclaredIncomingHandoffDigest { get; }
        public string ComputeDigest() => BakingCanonicalDigest.HashCanonicalLines(new[]
        {
            "MAP19_04_CLUSTER_RECOVERY_INPUT_V1",
            Graph == null ? "MISSING_GRAPH" : Graph.GraphDigest,
            NakedSearch == null ? "MISSING_NAKED" : NakedSearch.SuccessProofDigest,
            CompletionSearch == null ? "MISSING_COMPLETION" : CompletionSearch.SuccessProofDigest,
            CompletionSearch == null ? "MISSING_HANDOFF" : CompletionSearch.Map19_04HandoffDigest,
            DeclaredCompletionProofDigest, DeclaredIncomingHandoffDigest,
            ActionAudit == null ? "MISSING_AUDIT" : ActionAudit.StableToken,
        }.Concat(cases.Select(value => value.StableToken)));
    }

    public sealed class ClusterDensityValidationInput
    {
        private readonly ReadOnlyCollection<ClusterDensitySource> clusters;

        public ClusterDensityValidationInput(
            GeneratedTileMovementGraph graph,
            NakedTraversalSearchResult nakedSearch,
            GeneratedCompletionSearchResult completionSearch,
            IEnumerable<ClusterDensitySource> sourceClusters,
            GeneratedClusterValidationActionAudit actionAudit = null)
        {
            Graph = graph;
            NakedSearch = nakedSearch;
            CompletionSearch = completionSearch;
            clusters = new ReadOnlyCollection<ClusterDensitySource>((sourceClusters ??
                    Array.Empty<ClusterDensitySource>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
            ActionAudit = actionAudit ?? new GeneratedClusterValidationActionAudit();
        }

        public GeneratedTileMovementGraph Graph { get; }
        public NakedTraversalSearchResult NakedSearch { get; }
        public GeneratedCompletionSearchResult CompletionSearch { get; }
        public IReadOnlyList<ClusterDensitySource> Clusters => clusters;
        public GeneratedClusterValidationActionAudit ActionAudit { get; }

        public string ComputeDigest()
        {
            var lines = new List<string>
            {
                "MAP19_04_CLUSTER_DENSITY_INPUT_V1",
                Graph == null ? "MISSING_GRAPH" : Graph.GraphDigest,
                NakedSearch == null ? "MISSING_NAKED" : NakedSearch.SuccessProofDigest,
                CompletionSearch == null ? "MISSING_COMPLETION" : CompletionSearch.SuccessProofDigest,
                CompletionSearch == null ? "MISSING_HANDOFF" : CompletionSearch.Map19_04HandoffDigest,
                ActionAudit == null ? "MISSING_AUDIT" : ActionAudit.StableToken,
            };
            foreach (var cluster in clusters)
            {
                lines.Add(string.Join("|", new[]
                {
                    "DENSITY_CLUSTER", cluster.StableIdentity,
                    cluster.Canvas == null ? "MISSING_CANVAS" : cluster.Canvas.Id.Value,
                    cluster.Canvas == null || cluster.Canvas.ValidationStamp == null ?
                        "MISSING_STAMP" : cluster.Canvas.ValidationStamp.StableDigest,
                    cluster.Slices == null ? "MISSING_SLICES" : cluster.Slices.SourceCanvasId.Value,
                    ClusterRecoveryCase.Number(cluster.MinimumX),
                    ClusterRecoveryCase.Number(cluster.MinimumY),
                    ClusterRecoveryCase.Number(cluster.MaximumX),
                    ClusterRecoveryCase.Number(cluster.MaximumY),
                    ClusterRecoveryCase.Number(cluster.RequiredEightBySixAirWindows),
                    string.Join(",", cluster.AcceptedRecoveryTargetNodeIds),
                }));
                if (cluster.Canvas != null)
                    lines.AddRange(cluster.Canvas.Cells.OrderBy(value => value.CanonicalIndex)
                        .Select(value => DensityCellToken(cluster.ClusterId, value)));
                if (cluster.Slices != null)
                    lines.AddRange(cluster.Slices.Slices.OrderBy(value => value.Coordinate)
                        .Select(SliceToken));
            }
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        internal static string DensityCellToken(TerrainClusterId clusterId, SectorCanvasCell cell) =>
            string.Join("|", new[]
            {
                "DENSITY_CELL", clusterId.Value,
                ClusterRecoveryCase.Number(cell.Coordinate.X),
                ClusterRecoveryCase.Number(cell.Coordinate.Y),
                cell.Layers == null ? "MISSING_LAYERS" : cell.Layers.Solid.StableId,
                cell.Layers != null && cell.Layers.Solid.IsExplicitEmpty ? "1" : "0",
                cell.Layers == null ? "MISSING_MARKER" : cell.Layers.Marker.StableId,
                cell.Layers == null ? "MISSING_OWNER" : cell.Layers.Owner.StableId,
                cell.Provenance == null ? "MISSING_PROVENANCE" : string.Join(",",
                    cell.Provenance.Sources.Select(value => value.Kind + ":" + value.StableId +
                        ":" + (value.IsProtected ? "1" : "0"))),
            });

        private static string SliceToken(GeneratedMicroChunkSlice slice) => string.Join("|", new[]
        {
            "DENSITY_SLICE", ClusterRecoveryCase.Number(slice.Coordinate.X),
            ClusterRecoveryCase.Number(slice.Coordinate.Y),
            slice.Provenance == null ? "MISSING_PROVENANCE" : slice.Provenance.SourceCanvasId.Value,
            slice.Provenance == null ? "MISSING_DIGEST" : slice.Provenance.SourceCanvasDigest,
            slice.Provenance == null ? "MISSING_STAMP" : slice.Provenance.SourceValidationStampDigest,
            ClusterRecoveryCase.Number(slice.Cells.Count),
        });
    }

    public sealed class ClusterRecoveryFrontierEvidence : IComparable<ClusterRecoveryFrontierEvidence>
    {
        public ClusterRecoveryFrontierEvidence(string nodeId, int distance, int outgoingSteps)
        {
            NodeId = ClusterRecoveryCase.Normalize(nodeId);
            Distance = distance;
            OutgoingSteps = outgoingSteps;
        }
        public string NodeId { get; }
        public int Distance { get; }
        public int OutgoingSteps { get; }
        public string StableToken => NodeId + "|" + ClusterRecoveryCase.Number(Distance) + "|" +
            ClusterRecoveryCase.Number(OutgoingSteps);
        public int CompareTo(ClusterRecoveryFrontierEvidence other) => other == null ? -1 :
            string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
    }

    public sealed class ClusterRecoveryFailure : IComparable<ClusterRecoveryFailure>
    {
        private readonly ReadOnlyCollection<ClusterRecoveryFrontierEvidence> frontier;

        public ClusterRecoveryFailure(
            string owner,
            string reason,
            string clusterId,
            string caseId,
            string offendingKey,
            string expected,
            string actual,
            string sourceDigest,
            IEnumerable<ClusterRecoveryFrontierEvidence> sourceFrontier = null,
            string windowEvidence = "NONE")
        {
            Owner = ClusterRecoveryCase.Normalize(owner);
            Reason = ClusterRecoveryCase.Normalize(reason);
            ClusterId = ClusterRecoveryCase.Normalize(clusterId);
            CaseId = ClusterRecoveryCase.Normalize(caseId);
            OffendingKey = ClusterRecoveryCase.Normalize(offendingKey);
            Expected = ClusterRecoveryCase.Normalize(expected);
            Actual = ClusterRecoveryCase.Normalize(actual);
            SourceDigest = ClusterRecoveryCase.Normalize(sourceDigest);
            WindowEvidence = ClusterRecoveryCase.Normalize(windowEvidence);
            frontier = new ReadOnlyCollection<ClusterRecoveryFrontierEvidence>((sourceFrontier ??
                    Array.Empty<ClusterRecoveryFrontierEvidence>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
        }

        public string Owner { get; }
        public string Reason { get; }
        public string ClusterId { get; }
        public string CaseId { get; }
        public string OffendingKey { get; }
        public string Expected { get; }
        public string Actual { get; }
        public string SourceDigest { get; }
        public IReadOnlyList<ClusterRecoveryFrontierEvidence> Frontier => frontier;
        public string WindowEvidence { get; }
        public string StableToken => string.Join("|", new[]
        {
            Owner, Reason, ClusterId, CaseId, OffendingKey, Expected, Actual, SourceDigest,
            WindowEvidence, string.Join(",", frontier.Select(value => value.StableToken)),
        });
        public int CompareTo(ClusterRecoveryFailure other) => other == null ? -1 :
            string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
        public override string ToString() => StableToken;
    }

    public sealed class ClusterRecoveryProof : IComparable<ClusterRecoveryProof>
    {
        internal ClusterRecoveryProof(
            ClusterRecoveryCase sourceCase,
            string targetNodeId,
            int visitedNodes,
            int visitedEdges,
            int shortestEdgeCount,
            string graphDigest,
            string completionProofDigest)
        {
            ClusterId = sourceCase.ClusterId;
            CaseId = sourceCase.CaseId;
            Kind = sourceCase.Kind;
            StartNodeId = sourceCase.StartNodeId;
            TargetNodeId = targetNodeId;
            VisitedNodes = visitedNodes;
            VisitedEdges = visitedEdges;
            ShortestEdgeCount = shortestEdgeCount;
            RecoveryCostMilliseconds = sourceCase.RecoveryCostMilliseconds;
            GraphDigest = graphDigest;
            CompletionProofDigest = completionProofDigest;
            ProofDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_04_CLUSTER_RECOVERY_CASE_PROOF_V1", sourceCase.StableToken,
                TargetNodeId, ClusterRecoveryCase.Number(VisitedNodes),
                ClusterRecoveryCase.Number(VisitedEdges),
                ClusterRecoveryCase.Number(ShortestEdgeCount), GraphDigest,
                CompletionProofDigest,
            });
        }

        public TerrainClusterId ClusterId { get; }
        public string CaseId { get; }
        public ClusterRecoveryCaseKind Kind { get; }
        public string StartNodeId { get; }
        public string TargetNodeId { get; }
        public int VisitedNodes { get; }
        public int VisitedEdges { get; }
        public int ShortestEdgeCount { get; }
        public int RecoveryCostMilliseconds { get; }
        public string GraphDigest { get; }
        public string CompletionProofDigest { get; }
        public string ProofDigest { get; }
        public string StableToken => ClusterId.Value + "|" + CaseId + "|" + ProofDigest;
        public int CompareTo(ClusterRecoveryProof other) => other == null ? -1 :
            string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
    }

    public sealed class ClusterDensityProof : IComparable<ClusterDensityProof>
    {
        internal ClusterDensityProof(
            ClusterDensitySource source,
            int totalCells,
            int solidCells,
            int reachableAirCells,
            int airCells,
            int airWindows,
            int unreachablePockets,
            int headSnags,
            int oneWayPits,
            int hardSolidObstructions,
            int overprotectedEnvelopeObstructions,
            int boundarySocketObstructions,
            string inputDigest)
        {
            ClusterId = source.ClusterId;
            TotalCells = totalCells;
            SolidCells = solidCells;
            ReachableAirCells = reachableAirCells;
            AirCells = airCells;
            SolidRatioBasisPoints = Ratio(solidCells, totalCells);
            ReachableAirRatioBasisPoints = Ratio(reachableAirCells, airCells);
            RequiredEightBySixAirWindows = source.RequiredEightBySixAirWindows;
            EightBySixAirWindowsFound = airWindows;
            UnreachablePockets = unreachablePockets;
            HeadSnagCandidates = headSnags;
            OneWayPitCandidates = oneWayPits;
            HardSolidObstructions = hardSolidObstructions;
            OverprotectedEnvelopeObstructions = overprotectedEnvelopeObstructions;
            BoundarySocketObstructions = boundarySocketObstructions;
            ProofDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_04_CLUSTER_DENSITY_PROOF_V1", source.StableIdentity,
                ClusterRecoveryCase.Number(totalCells), ClusterRecoveryCase.Number(solidCells),
                ClusterRecoveryCase.Number(reachableAirCells), ClusterRecoveryCase.Number(airCells),
                ClusterRecoveryCase.Number(SolidRatioBasisPoints),
                ClusterRecoveryCase.Number(ReachableAirRatioBasisPoints),
                ClusterRecoveryCase.Number(RequiredEightBySixAirWindows),
                ClusterRecoveryCase.Number(EightBySixAirWindowsFound),
                ClusterRecoveryCase.Number(UnreachablePockets),
                ClusterRecoveryCase.Number(HeadSnagCandidates),
                ClusterRecoveryCase.Number(OneWayPitCandidates),
                ClusterRecoveryCase.Number(HardSolidObstructions),
                ClusterRecoveryCase.Number(OverprotectedEnvelopeObstructions),
                ClusterRecoveryCase.Number(BoundarySocketObstructions), inputDigest,
            });
        }

        public TerrainClusterId ClusterId { get; }
        public int TotalCells { get; }
        public int SolidCells { get; }
        public int ReachableAirCells { get; }
        public int AirCells { get; }
        public int SolidRatioBasisPoints { get; }
        public int ReachableAirRatioBasisPoints { get; }
        public int RequiredEightBySixAirWindows { get; }
        public int EightBySixAirWindowsFound { get; }
        public int UnreachablePockets { get; }
        public int HeadSnagCandidates { get; }
        public int OneWayPitCandidates { get; }
        public int HardSolidObstructions { get; }
        public int OverprotectedEnvelopeObstructions { get; }
        public int BoundarySocketObstructions { get; }
        public string ProofDigest { get; }
        public string StableToken => ClusterId.Value + "|" + ProofDigest;
        public int CompareTo(ClusterDensityProof other) => other == null ? -1 :
            string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
        private static int Ratio(int numerator, int denominator) => denominator <= 0 ? 0 :
            (int)Math.Round(numerator * 10000m / denominator, 0,
                MidpointRounding.AwayFromZero);
    }

    public sealed class ClusterRecoveryValidationSurface
    {
        private readonly ReadOnlyCollection<ClusterRecoveryProof> proofs;

        internal ClusterRecoveryValidationSurface(
            ClusterRecoveryValidationInput input,
            IEnumerable<ClusterRecoveryProof> sourceProofs)
        {
            proofs = new ReadOnlyCollection<ClusterRecoveryProof>((sourceProofs ??
                Array.Empty<ClusterRecoveryProof>()).OrderBy(value => value).ToArray());
            InputDigest = input.ComputeDigest();
            ProofDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_04_CLUSTER_RECOVERY_PROOF_SET_V1", InputDigest,
                input.Graph.GraphDigest, input.CompletionSearch.Proof.ProofDigest,
            }.Concat(proofs.Select(value => value.StableToken)));
        }

        public IReadOnlyList<ClusterRecoveryProof> Proofs => proofs;
        public int ClustersChecked => proofs.Select(value => value.ClusterId).Distinct().Count();
        public int RecoveryCasesChecked => proofs.Count;
        public int BaseRouteRecoveryCases => Count(ClusterRecoveryCaseKind.BaseRouteRejoin);
        public int HighRouteRecoveryCases => Count(ClusterRecoveryCaseKind.HighRouteRecovery);
        public int ExplicitRecoveryCases => Count(ClusterRecoveryCaseKind.ExplicitRecoveryRoute);
        public int TimedRecoveryCases => Count(ClusterRecoveryCaseKind.TimedRecoveryWindow);
        public int RecoveryCasesPassed => proofs.Count;
        public int RecoveryCasesFailed => 0;
        public decimal AverageRecoveryEdgeCount => proofs.Count == 0 ? 0m :
            proofs.Average(value => (decimal)value.ShortestEdgeCount);
        public int MaxRecoveryEdgeCount => proofs.Count == 0 ? 0 :
            proofs.Max(value => value.ShortestEdgeCount);
        public string InputDigest { get; }
        public string ProofDigest { get; }
        private int Count(ClusterRecoveryCaseKind kind) => proofs.Count(value => value.Kind == kind);
    }

    public sealed class ClusterDensityValidationSurface
    {
        private readonly ReadOnlyCollection<ClusterDensityProof> proofs;

        internal ClusterDensityValidationSurface(
            ClusterDensityValidationInput input,
            IEnumerable<ClusterDensityProof> sourceProofs)
        {
            proofs = new ReadOnlyCollection<ClusterDensityProof>((sourceProofs ??
                Array.Empty<ClusterDensityProof>()).OrderBy(value => value).ToArray());
            InputDigest = input.ComputeDigest();
            ValidationDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_04_CLUSTER_DENSITY_PROOF_SET_V1", InputDigest,
                input.Graph.GraphDigest, input.CompletionSearch.Proof.ProofDigest,
            }.Concat(proofs.Select(value => value.StableToken)));
        }

        public IReadOnlyList<ClusterDensityProof> Proofs => proofs;
        public int ClustersChecked => proofs.Count;
        public int SolidRatioMinimumBasisPoints => proofs.Count == 0 ? 0 :
            proofs.Min(value => value.SolidRatioBasisPoints);
        public int SolidRatioMaximumBasisPoints => proofs.Count == 0 ? 0 :
            proofs.Max(value => value.SolidRatioBasisPoints);
        public int ReachableAirRatioMinimumBasisPoints => proofs.Count == 0 ? 0 :
            proofs.Min(value => value.ReachableAirRatioBasisPoints);
        public int ReachableAirRatioMaximumBasisPoints => proofs.Count == 0 ? 0 :
            proofs.Max(value => value.ReachableAirRatioBasisPoints);
        public int RequiredEightBySixAirWindows => proofs.Sum(value =>
            value.RequiredEightBySixAirWindows);
        public int EightBySixAirWindowsFound => proofs.Sum(value =>
            value.EightBySixAirWindowsFound);
        public int UnreachablePockets => proofs.Sum(value => value.UnreachablePockets);
        public int HeadSnagCandidates => proofs.Sum(value => value.HeadSnagCandidates);
        public int OneWayPitCandidates => proofs.Sum(value => value.OneWayPitCandidates);
        public int HardSolidObstructions => proofs.Sum(value => value.HardSolidObstructions);
        public int OverprotectedEnvelopeObstructions => proofs.Sum(value =>
            value.OverprotectedEnvelopeObstructions);
        public int BoundarySocketObstructions => proofs.Sum(value =>
            value.BoundarySocketObstructions);
        public string InputDigest { get; }
        public string ValidationDigest { get; }
    }

    public sealed class ClusterRecoveryDensityValidationSurface
    {
        internal ClusterRecoveryDensityValidationSurface(
            ClusterRecoveryValidationSurface recovery,
            ClusterDensityValidationSurface density,
            string graphDigest,
            string completionProofDigest,
            string incomingHandoffDigest)
        {
            Recovery = recovery;
            Density = density;
            CombinedDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_04_CLUSTER_RECOVERY_DENSITY_COMBINED_V1", graphDigest,
                completionProofDigest, incomingHandoffDigest, recovery.InputDigest,
                recovery.ProofDigest, density.InputDigest, density.ValidationDigest,
            });
            Map19_05HandoffDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_05_CLUSTER_VALIDATION_HANDOFF_V1", graphDigest,
                completionProofDigest, incomingHandoffDigest, CombinedDigest,
                "MAP19_05_LOCKED=1|STARTED=0",
            });
        }

        public ClusterRecoveryValidationSurface Recovery { get; }
        public ClusterDensityValidationSurface Density { get; }
        public string CombinedDigest { get; }
        public string Map19_05HandoffDigest { get; }
        public bool Map19_05Started => false;
    }

    public sealed class ClusterRecoveryValidationResult
    {
        private readonly ReadOnlyCollection<ClusterRecoveryFailure> failures;

        internal ClusterRecoveryValidationResult(
            ClusterRecoveryDensityValidationSurface surface,
            IEnumerable<ClusterRecoveryFailure> sourceFailures)
        {
            Surface = surface;
            failures = new ReadOnlyCollection<ClusterRecoveryFailure>((sourceFailures ??
                    Array.Empty<ClusterRecoveryFailure>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
        }

        public bool Success => Surface != null && failures.Count == 0;
        public ClusterRecoveryDensityValidationSurface Surface { get; }
        public IReadOnlyList<ClusterRecoveryFailure> Failures => failures;
        public string RecoverySuccessDigest => Success ? Surface.Recovery.ProofDigest : string.Empty;
        public string DensitySuccessDigest => Success ? Surface.Density.ValidationDigest : string.Empty;
        public string CombinedSuccessDigest => Success ? Surface.CombinedDigest : string.Empty;
        public string Map19_05HandoffDigest => Success ? Surface.Map19_05HandoffDigest : string.Empty;
    }
}
