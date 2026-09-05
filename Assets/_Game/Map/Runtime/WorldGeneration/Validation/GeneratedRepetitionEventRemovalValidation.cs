using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Activities;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.EventOverlays;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.WorldGeneration.Validation
{
    public enum GeneratedRepetitionSourceKind
    {
        MicroPattern = 1,
        TerrainCluster = 2,
        ActivityStructure = 3,
        EventOverlay = 4,
    }

    public enum GeneratedRepetitionWindowKind
    {
        PatternMirror = 1,
        TerrainCluster = 2,
        ActivityEvent = 3,
    }

    public enum GeneratedRemovalTransitionSourceKind
    {
        StaticMandatory = 1,
        StaticSpecial = 2,
        ActivityStructure = 3,
        EventOverlay = 4,
    }

    public sealed class GeneratedRepetitionSignature : IComparable<GeneratedRepetitionSignature>
    {
        public GeneratedRepetitionSignature(
            GeneratedRepetitionSourceKind kind,
            string sourceOwner,
            string signatureId,
            string sourceId,
            string clusterId,
            string localBand,
            string pacingBand,
            int sourceOrdinal,
            string mirrorPairKey,
            bool isMirror,
            string structuralTileSilhouette,
            string routeOrSpineRole,
            string movementAffordance,
            string activityOrEventRole,
            string provenanceKey,
            string visualOrMaterialToken,
            string sourceDigest,
            MicroPatternSilhouetteSignature microPatternSignature = null,
            TerrainClusterContract terrainCluster = null,
            ActivityStructureContract activityStructure = null,
            EventOverlayContract eventOverlay = null)
        {
            Kind = kind;
            SourceOwner = Normalize(sourceOwner);
            SignatureId = Normalize(signatureId);
            SourceId = Normalize(sourceId);
            ClusterId = Normalize(clusterId);
            LocalBand = Normalize(localBand);
            PacingBand = Normalize(pacingBand);
            SourceOrdinal = sourceOrdinal;
            MirrorPairKey = NormalizeOptional(mirrorPairKey);
            IsMirror = isMirror;
            StructuralTileSilhouette = Normalize(structuralTileSilhouette);
            RouteOrSpineRole = Normalize(routeOrSpineRole);
            MovementAffordance = Normalize(movementAffordance);
            ActivityOrEventRole = Normalize(activityOrEventRole);
            ProvenanceKey = Normalize(provenanceKey);
            VisualOrMaterialToken = Normalize(visualOrMaterialToken);
            SourceDigest = Normalize(sourceDigest);
            MicroPatternSignature = microPatternSignature;
            TerrainCluster = terrainCluster;
            ActivityStructure = activityStructure;
            EventOverlay = eventOverlay;
        }

        public GeneratedRepetitionSourceKind Kind { get; }
        public string SourceOwner { get; }
        public string SignatureId { get; }
        public string SourceId { get; }
        public string ClusterId { get; }
        public string LocalBand { get; }
        public string PacingBand { get; }
        public int SourceOrdinal { get; }
        public string MirrorPairKey { get; }
        public bool IsMirror { get; }
        public string StructuralTileSilhouette { get; }
        public string RouteOrSpineRole { get; }
        public string MovementAffordance { get; }
        public string ActivityOrEventRole { get; }
        public string ProvenanceKey { get; }
        public string VisualOrMaterialToken { get; }
        public string SourceDigest { get; }
        public MicroPatternSilhouetteSignature MicroPatternSignature { get; }
        public TerrainClusterContract TerrainCluster { get; }
        public ActivityStructureContract ActivityStructure { get; }
        public EventOverlayContract EventOverlay { get; }

        public string StructuralKey => string.Join("|", new[]
        {
            Number((int)Kind), StructuralTileSilhouette, RouteOrSpineRole,
            MovementAffordance, ActivityOrEventRole, SourceOwner, ProvenanceKey,
        });

        public string StableToken => string.Join("|", new[]
        {
            "MAP19_05_REPETITION_SIGNATURE_V1", Number((int)Kind), SourceOwner,
            SignatureId, SourceId, ClusterId, LocalBand, PacingBand, Number(SourceOrdinal),
            MirrorPairKey, IsMirror ? "1" : "0", StructuralTileSilhouette,
            RouteOrSpineRole, MovementAffordance, ActivityOrEventRole, ProvenanceKey,
            VisualOrMaterialToken, SourceDigest, ContractIdentity,
        });

        public int CompareTo(GeneratedRepetitionSignature other) => other == null ? -1 :
            string.Compare(SourceOwner + "|" + SignatureId + "|" + StableToken,
                other.SourceOwner + "|" + other.SignatureId + "|" + other.StableToken,
                StringComparison.Ordinal);

        internal string ContractIdentity
        {
            get
            {
                switch (Kind)
                {
                    case GeneratedRepetitionSourceKind.MicroPattern:
                        return MicroPatternSignature == null ? "MISSING_PATTERN" :
                            MicroPatternSignature.StableDigest;
                    case GeneratedRepetitionSourceKind.TerrainCluster:
                        return TerrainCluster == null ? "MISSING_CLUSTER" : TerrainCluster.Id.Value;
                    case GeneratedRepetitionSourceKind.ActivityStructure:
                        return ActivityStructure == null ? "MISSING_ACTIVITY" :
                            ActivityStructure.Id.Value;
                    case GeneratedRepetitionSourceKind.EventOverlay:
                        return EventOverlay == null ? "MISSING_EVENT" : EventOverlay.Id.Value;
                    default:
                        return "MISSING_CONTRACT";
                }
            }
        }

        internal static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ?
            "MISSING" : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
        internal static string NormalizeOptional(string value) => string.IsNullOrWhiteSpace(value) ?
            string.Empty : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
        internal static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedRepetitionWindow : IComparable<GeneratedRepetitionWindow>
    {
        public GeneratedRepetitionWindow(string windowId, GeneratedRepetitionWindowKind kind,
            int maximumOrdinalDistance)
        {
            WindowId = GeneratedRepetitionSignature.Normalize(windowId);
            Kind = kind;
            MaximumOrdinalDistance = maximumOrdinalDistance;
        }

        public string WindowId { get; }
        public GeneratedRepetitionWindowKind Kind { get; }
        public int MaximumOrdinalDistance { get; }
        public string StableToken => string.Join("|", new[]
        {
            "MAP19_05_REPETITION_WINDOW_V1", WindowId,
            GeneratedRepetitionSignature.Number((int)Kind),
            GeneratedRepetitionSignature.Number(MaximumOrdinalDistance),
        });
        public int CompareTo(GeneratedRepetitionWindow other) => other == null ? -1 :
            string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
    }

    public sealed class GeneratedRepetitionEventRemovalActionAudit
    {
        public GeneratedRepetitionEventRemovalActionAudit(
            int graphMutationAttempts = 0,
            int proofRewriteAttempts = 0,
            int runtimeStateReadAttempts = 0,
            int runtimeObjectMutationAttempts = 0,
            int terrainOrSlotMutationAttempts = 0,
            int implicitTeleportAttempts = 0,
            int forcedGrantAttempts = 0,
            int seedBatchAttempts = 0,
            bool map19_06Started = false)
        {
            GraphMutationAttempts = graphMutationAttempts;
            ProofRewriteAttempts = proofRewriteAttempts;
            RuntimeStateReadAttempts = runtimeStateReadAttempts;
            RuntimeObjectMutationAttempts = runtimeObjectMutationAttempts;
            TerrainOrSlotMutationAttempts = terrainOrSlotMutationAttempts;
            ImplicitTeleportAttempts = implicitTeleportAttempts;
            ForcedGrantAttempts = forcedGrantAttempts;
            SeedBatchAttempts = seedBatchAttempts;
            Map19_06Started = map19_06Started;
        }

        public int GraphMutationAttempts { get; }
        public int ProofRewriteAttempts { get; }
        public int RuntimeStateReadAttempts { get; }
        public int RuntimeObjectMutationAttempts { get; }
        public int TerrainOrSlotMutationAttempts { get; }
        public int ImplicitTeleportAttempts { get; }
        public int ForcedGrantAttempts { get; }
        public int SeedBatchAttempts { get; }
        public bool Map19_06Started { get; }
        public bool IsZero => GraphMutationAttempts == 0 && ProofRewriteAttempts == 0 &&
            RuntimeStateReadAttempts == 0 && RuntimeObjectMutationAttempts == 0 &&
            TerrainOrSlotMutationAttempts == 0 && ImplicitTeleportAttempts == 0 &&
            ForcedGrantAttempts == 0 && SeedBatchAttempts == 0 && !Map19_06Started;
        public string StableToken => string.Join("|", new[]
        {
            "MAP19_05_ACTION_AUDIT_V1", Number(GraphMutationAttempts),
            Number(ProofRewriteAttempts), Number(RuntimeStateReadAttempts),
            Number(RuntimeObjectMutationAttempts), Number(TerrainOrSlotMutationAttempts),
            Number(ImplicitTeleportAttempts), Number(ForcedGrantAttempts),
            Number(SeedBatchAttempts), Map19_06Started ? "1" : "0",
        });
        private static string Number(int value) =>
            value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedRepetitionValidationInput
    {
        private readonly ReadOnlyCollection<GeneratedRepetitionSignature> signatures;
        private readonly ReadOnlyCollection<GeneratedRepetitionWindow> windows;

        public GeneratedRepetitionValidationInput(
            GeneratedTileMovementGraph graph,
            NakedTraversalSearchResult nakedSearch,
            GeneratedCompletionSearchResult completionSearch,
            ClusterRecoveryValidationResult clusterValidation,
            SectorCanvasContract canvas,
            GeneratedSliceSet slices,
            IEnumerable<GeneratedRepetitionSignature> sourceSignatures,
            IEnumerable<GeneratedRepetitionWindow> sourceWindows,
            GeneratedRepetitionEventRemovalActionAudit actionAudit = null,
            string declaredMap19_04CombinedDigest = null,
            string declaredIncomingHandoffDigest = null)
        {
            Graph = graph;
            NakedSearch = nakedSearch;
            CompletionSearch = completionSearch;
            ClusterValidation = clusterValidation;
            Canvas = canvas;
            Slices = slices;
            signatures = new ReadOnlyCollection<GeneratedRepetitionSignature>((sourceSignatures ??
                Array.Empty<GeneratedRepetitionSignature>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
            windows = new ReadOnlyCollection<GeneratedRepetitionWindow>((sourceWindows ??
                Array.Empty<GeneratedRepetitionWindow>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
            ActionAudit = actionAudit ?? new GeneratedRepetitionEventRemovalActionAudit();
            DeclaredMap19_04CombinedDigest = declaredMap19_04CombinedDigest ??
                (clusterValidation == null ? "MISSING" : clusterValidation.CombinedSuccessDigest);
            DeclaredIncomingHandoffDigest = declaredIncomingHandoffDigest ??
                (clusterValidation == null ? "MISSING" : clusterValidation.Map19_05HandoffDigest);
        }

        public GeneratedTileMovementGraph Graph { get; }
        public NakedTraversalSearchResult NakedSearch { get; }
        public GeneratedCompletionSearchResult CompletionSearch { get; }
        public ClusterRecoveryValidationResult ClusterValidation { get; }
        public SectorCanvasContract Canvas { get; }
        public GeneratedSliceSet Slices { get; }
        public IReadOnlyList<GeneratedRepetitionSignature> Signatures => signatures;
        public IReadOnlyList<GeneratedRepetitionWindow> Windows => windows;
        public GeneratedRepetitionEventRemovalActionAudit ActionAudit { get; }
        public string DeclaredMap19_04CombinedDigest { get; }
        public string DeclaredIncomingHandoffDigest { get; }

        public string ComputeDigest()
        {
            var lines = new List<string>
            {
                "MAP19_05_REPETITION_INPUT_V1", Digest(Graph, value => value.GraphDigest),
                Digest(NakedSearch, value => value.SuccessProofDigest),
                Digest(CompletionSearch, value => value.SuccessProofDigest),
                Digest(ClusterValidation, value => value.CombinedSuccessDigest),
                DeclaredMap19_04CombinedDigest, DeclaredIncomingHandoffDigest,
                Canvas == null ? "MISSING_CANVAS" : Canvas.Id.Value,
                Canvas == null || Canvas.ValidationStamp == null ? "MISSING_STAMP" :
                    Canvas.ValidationStamp.StableDigest,
                Slices == null ? "MISSING_SLICES" : Slices.SourceCanvasId.Value,
                ActionAudit == null ? "MISSING_AUDIT" : ActionAudit.StableToken,
            };
            if (Canvas != null)
                lines.AddRange(Canvas.Cells.OrderBy(value => value.CanonicalIndex)
                    .Select(CellToken));
            if (Slices != null)
                lines.AddRange(Slices.Slices.OrderBy(value => value.Coordinate)
                    .Select(SliceToken));
            lines.AddRange(signatures.Select(value => value.StableToken));
            lines.AddRange(windows.Select(value => value.StableToken));
            return BakingCanonicalDigest.HashCanonicalLines(lines);
        }

        private static string Digest<T>(T value, Func<T, string> selector) where T : class =>
            value == null ? "MISSING" : selector(value);
        private static string CellToken(SectorCanvasCell cell) => string.Join("|", new[]
        {
            "REPETITION_CELL", Number(cell.Coordinate.X), Number(cell.Coordinate.Y),
            cell.Layers == null ? "MISSING_LAYERS" : cell.Layers.Solid.StableId,
            cell.Layers == null ? "MISSING_OWNER" : cell.Layers.Owner.StableId,
            cell.Provenance == null ? "MISSING_PROVENANCE" : string.Join(",",
                cell.Provenance.Sources.Select(value => value.Kind + ":" + value.StableId +
                    ":" + (value.IsProtected ? "1" : "0"))),
        });
        private static string SliceToken(GeneratedMicroChunkSlice slice) => string.Join("|", new[]
        {
            "REPETITION_SLICE", Number(slice.Coordinate.X), Number(slice.Coordinate.Y),
            slice.Provenance == null ? "MISSING_PROVENANCE" :
                slice.Provenance.SourceCanvasId.Value,
            slice.Provenance == null ? "MISSING_DIGEST" :
                slice.Provenance.SourceCanvasDigest,
            Number(slice.Cells.Count),
        });
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedRemovalTransitionBinding : IComparable<GeneratedRemovalTransitionBinding>
    {
        public GeneratedRemovalTransitionBinding(
            GeneratedCompletionStateTransition transition,
            GeneratedRemovalTransitionSourceKind sourceKind,
            string sourceOwner,
            string sourceDigest,
            ActivityStructureContract activityStructure = null,
            EventOverlayContract eventOverlay = null,
            bool declaresStaticDependency = false)
        {
            Transition = transition;
            SourceKind = sourceKind;
            SourceOwner = GeneratedRepetitionSignature.Normalize(sourceOwner);
            SourceDigest = GeneratedRepetitionSignature.Normalize(sourceDigest);
            ActivityStructure = activityStructure;
            EventOverlay = eventOverlay;
            DeclaresStaticDependency = declaresStaticDependency;
        }

        public GeneratedCompletionStateTransition Transition { get; }
        public GeneratedRemovalTransitionSourceKind SourceKind { get; }
        public string SourceOwner { get; }
        public string SourceDigest { get; }
        public ActivityStructureContract ActivityStructure { get; }
        public EventOverlayContract EventOverlay { get; }
        public bool DeclaresStaticDependency { get; }
        public bool IsRemoved => SourceKind == GeneratedRemovalTransitionSourceKind.ActivityStructure ||
            SourceKind == GeneratedRemovalTransitionSourceKind.EventOverlay;
        public string StableToken => string.Join("|", new[]
        {
            "MAP19_05_REMOVAL_BINDING_V1",
            Transition == null ? "MISSING_TRANSITION" : Transition.StableToken,
            GeneratedRepetitionSignature.Number((int)SourceKind), SourceOwner, SourceDigest,
            ActivityStructure == null ? "NO_ACTIVITY" : ActivityStructure.Id.Value,
            EventOverlay == null ? "NO_EVENT" : EventOverlay.Id.Value,
            DeclaresStaticDependency ? "1" : "0",
        });
        public int CompareTo(GeneratedRemovalTransitionBinding other) => other == null ? -1 :
            string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
    }

    public sealed class GeneratedEventRemovalValidationInput
    {
        private readonly ReadOnlyCollection<GeneratedRemovalTransitionBinding> transitions;
        private readonly ReadOnlyCollection<TraversalMovementKind> movements;

        public GeneratedEventRemovalValidationInput(
            GeneratedTileMovementGraph graph,
            NakedTraversalSearchResult nakedSearch,
            GeneratedCompletionSearchResult completionSearch,
            ClusterRecoveryValidationResult clusterValidation,
            IEnumerable<GeneratedRemovalTransitionBinding> sourceTransitions,
            GeneratedCompletionGoal goal,
            IEnumerable<TraversalMovementKind> allowedMovementKinds,
            GeneratedRepetitionEventRemovalActionAudit actionAudit = null,
            string declaredMap19_04CombinedDigest = null,
            string declaredIncomingHandoffDigest = null)
        {
            Graph = graph;
            NakedSearch = nakedSearch;
            CompletionSearch = completionSearch;
            ClusterValidation = clusterValidation;
            transitions = new ReadOnlyCollection<GeneratedRemovalTransitionBinding>((
                sourceTransitions ?? Array.Empty<GeneratedRemovalTransitionBinding>())
                .Where(value => value != null).OrderBy(value => value).ToArray());
            Goal = goal;
            movements = new ReadOnlyCollection<TraversalMovementKind>((allowedMovementKinds ??
                Array.Empty<TraversalMovementKind>()).OrderBy(value => (int)value).ToArray());
            ActionAudit = actionAudit ?? new GeneratedRepetitionEventRemovalActionAudit();
            DeclaredMap19_04CombinedDigest = declaredMap19_04CombinedDigest ??
                (clusterValidation == null ? "MISSING" : clusterValidation.CombinedSuccessDigest);
            DeclaredIncomingHandoffDigest = declaredIncomingHandoffDigest ??
                (clusterValidation == null ? "MISSING" : clusterValidation.Map19_05HandoffDigest);
        }

        public GeneratedTileMovementGraph Graph { get; }
        public NakedTraversalSearchResult NakedSearch { get; }
        public GeneratedCompletionSearchResult CompletionSearch { get; }
        public ClusterRecoveryValidationResult ClusterValidation { get; }
        public IReadOnlyList<GeneratedRemovalTransitionBinding> Transitions => transitions;
        public GeneratedCompletionGoal Goal { get; }
        public IReadOnlyList<TraversalMovementKind> AllowedMovementKinds => movements;
        public GeneratedRepetitionEventRemovalActionAudit ActionAudit { get; }
        public string DeclaredMap19_04CombinedDigest { get; }
        public string DeclaredIncomingHandoffDigest { get; }
        public string ComputeDigest() => BakingCanonicalDigest.HashCanonicalLines(new[]
        {
            "MAP19_05_EVENT_REMOVAL_INPUT_V1",
            Graph == null ? "MISSING_GRAPH" : Graph.GraphDigest,
            NakedSearch == null ? "MISSING_NAKED" : NakedSearch.SuccessProofDigest,
            CompletionSearch == null ? "MISSING_COMPLETION" : CompletionSearch.SuccessProofDigest,
            ClusterValidation == null ? "MISSING_MAP19_04" : ClusterValidation.CombinedSuccessDigest,
            DeclaredMap19_04CombinedDigest, DeclaredIncomingHandoffDigest,
            Goal == null ? "MISSING_GOAL" : Goal.StableToken,
            string.Join(",", movements.Select(value =>
                GeneratedRepetitionSignature.Number((int)value))),
            ActionAudit == null ? "MISSING_AUDIT" : ActionAudit.StableToken,
        }.Concat(transitions.Select(value => value.StableToken)));
    }

    public sealed class GeneratedRepetitionFailure : IComparable<GeneratedRepetitionFailure>
    {
        public GeneratedRepetitionFailure(string owner, string reason, string caseId,
            string offendingKey, string expected, string actual, string sourceDigest,
            string windowOrCompletionFrontierEvidence = "NONE")
        {
            Owner = GeneratedRepetitionSignature.Normalize(owner);
            Reason = GeneratedRepetitionSignature.Normalize(reason);
            CaseId = GeneratedRepetitionSignature.Normalize(caseId);
            OffendingKey = GeneratedRepetitionSignature.Normalize(offendingKey);
            Expected = GeneratedRepetitionSignature.Normalize(expected);
            Actual = GeneratedRepetitionSignature.Normalize(actual);
            SourceDigest = GeneratedRepetitionSignature.Normalize(sourceDigest);
            WindowOrCompletionFrontierEvidence = GeneratedRepetitionSignature.Normalize(
                windowOrCompletionFrontierEvidence);
        }

        public string Owner { get; }
        public string Reason { get; }
        public string CaseId { get; }
        public string OffendingKey { get; }
        public string Expected { get; }
        public string Actual { get; }
        public string SourceDigest { get; }
        public string WindowOrCompletionFrontierEvidence { get; }
        public string StableToken => string.Join("|", new[] { Owner, Reason, CaseId,
            OffendingKey, Expected, Actual, SourceDigest,
            WindowOrCompletionFrontierEvidence });
        public int CompareTo(GeneratedRepetitionFailure other) => other == null ? -1 :
            string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
        public override string ToString() => StableToken;
    }

    public sealed class GeneratedRepetitionValidationSurface
    {
        internal GeneratedRepetitionValidationSurface(GeneratedRepetitionValidationInput input,
            int patternSignatures, int mirrorPairs, int clusterSignatures,
            int clusterWindows, int activityEventSignatures, int activityEventWindows,
            int materialOnlyCollapses, IEnumerable<string> sourceStructuralTokens)
        {
            InputDigest = input.ComputeDigest();
            RepetitionSourcesChecked = input.Signatures.Count;
            PatternSignaturesChecked = patternSignatures;
            PatternMirrorPairsChecked = mirrorPairs;
            ClusterSignaturesChecked = clusterSignatures;
            ClusterRepetitionWindowsChecked = clusterWindows;
            ActivityEventSignaturesChecked = activityEventSignatures;
            ActivityEventRepetitionWindowsChecked = activityEventWindows;
            MaterialOnlyDuplicateCollapses = materialOnlyCollapses;
            var tokens = (sourceStructuralTokens ?? Array.Empty<string>())
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            UniqueStructuralSignatures = tokens.Length;
            ValidationDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_05_REPETITION_VALIDATION_V1", InputDigest,
                Number(RepetitionSourcesChecked), Number(PatternSignaturesChecked),
                Number(PatternMirrorPairsChecked), "0", Number(ClusterSignaturesChecked),
                Number(ClusterRepetitionWindowsChecked), "0",
                Number(ActivityEventSignaturesChecked),
                Number(ActivityEventRepetitionWindowsChecked), "0",
                Number(MaterialOnlyDuplicateCollapses), Number(UniqueStructuralSignatures),
            }.Concat(tokens));
        }

        public int RepetitionSourcesChecked { get; }
        public int PatternSignaturesChecked { get; }
        public int PatternMirrorPairsChecked { get; }
        public int PatternMirrorViolations => 0;
        public int ClusterSignaturesChecked { get; }
        public int ClusterRepetitionWindowsChecked { get; }
        public int ClusterRepetitionViolations => 0;
        public int ActivityEventSignaturesChecked { get; }
        public int ActivityEventRepetitionWindowsChecked { get; }
        public int ActivityEventRepetitionViolations => 0;
        public int MaterialOnlyDuplicateCollapses { get; }
        public int UniqueStructuralSignatures { get; }
        public string InputDigest { get; }
        public string ValidationDigest { get; }
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedEventRemovalValidationSurface
    {
        internal GeneratedEventRemovalValidationSurface(GeneratedEventRemovalValidationInput input,
            GeneratedCompletionSearchResult completion, int activityStructuresRemoved,
            int eventOverlaysRemoved, int staticTransitionsRetained, int removedTransitions)
        {
            Completion = completion;
            InputDigest = input.ComputeDigest();
            ActivityStructuresRemoved = activityStructuresRemoved;
            EventOverlaysRemoved = eventOverlaysRemoved;
            StaticTransitionsRetained = staticTransitionsRetained;
            RemovedTransitions = removedTransitions;
            ProofDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_05_EVENT_REMOVAL_PROOF_V1", InputDigest,
                completion.Proof.InputDigest, completion.Proof.StateSpaceDigest,
                completion.Proof.ProofDigest, Number(ActivityStructuresRemoved),
                Number(EventOverlaysRemoved), Number(StaticTransitionsRetained),
                Number(RemovedTransitions), Number(completion.Proof.ShortestTransitionCount),
            });
        }

        public GeneratedCompletionSearchResult Completion { get; }
        public int ActivityStructuresRemoved { get; }
        public int EventOverlaysRemoved { get; }
        public int StaticTransitionsRetained { get; }
        public int RemovedTransitions { get; }
        public int CompletionSearches => 1;
        public int GoalsSatisfied => Completion.Proof.GoalsSatisfied;
        public int GoalsMissing => Completion.Proof.GoalsMissing;
        public int ProofShortestTransitionCount => Completion.Proof.ShortestTransitionCount;
        public int RemovalDependencyViolations => 0;
        public string InputDigest { get; }
        public string ProofDigest { get; }
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedRepetitionEventRemovalSurface
    {
        internal GeneratedRepetitionEventRemovalSurface(
            GeneratedRepetitionValidationSurface repetition,
            GeneratedEventRemovalValidationSurface removal,
            string graphDigest, string completionProofDigest,
            string map19_04CombinedDigest, string incomingHandoffDigest)
        {
            Repetition = repetition;
            Removal = removal;
            CombinedDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_05_REPETITION_EVENT_REMOVAL_COMBINED_V1", graphDigest,
                completionProofDigest, map19_04CombinedDigest, incomingHandoffDigest,
                repetition.InputDigest, repetition.ValidationDigest,
                removal.InputDigest, removal.ProofDigest,
            });
            Map19_06HandoffDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_06_REPETITION_EVENT_REMOVAL_HANDOFF_V1", graphDigest,
                completionProofDigest, map19_04CombinedDigest, incomingHandoffDigest,
                CombinedDigest, "MAP19_06_LOCKED=1|STARTED=0",
            });
        }

        public GeneratedRepetitionValidationSurface Repetition { get; }
        public GeneratedEventRemovalValidationSurface Removal { get; }
        public string CombinedDigest { get; }
        public string Map19_06HandoffDigest { get; }
        public bool Map19_06Started => false;
    }

    public sealed class GeneratedRepetitionValidationResult
    {
        private readonly ReadOnlyCollection<GeneratedRepetitionFailure> failures;

        internal GeneratedRepetitionValidationResult(GeneratedRepetitionEventRemovalSurface surface,
            IEnumerable<GeneratedRepetitionFailure> sourceFailures)
        {
            Surface = surface;
            failures = new ReadOnlyCollection<GeneratedRepetitionFailure>((sourceFailures ??
                Array.Empty<GeneratedRepetitionFailure>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
        }

        public bool Success => Surface != null && failures.Count == 0;
        public GeneratedRepetitionEventRemovalSurface Surface { get; }
        public IReadOnlyList<GeneratedRepetitionFailure> Failures => failures;
        public string RepetitionSuccessDigest => Success ? Surface.Repetition.ValidationDigest :
            string.Empty;
        public string RemovalSuccessDigest => Success ? Surface.Removal.ProofDigest : string.Empty;
        public string CombinedSuccessDigest => Success ? Surface.CombinedDigest : string.Empty;
        public string Map19_06HandoffDigest => Success ? Surface.Map19_06HandoffDigest :
            string.Empty;
    }
}
