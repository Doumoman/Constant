using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.WorldGeneration.Validation
{
    public enum GeneratedWorstCaseScenarioKind
    {
        ZeroTool = 1,
        VillageSkipped = 2,
        VillageHostile = 3,
        VillageEvacuated = 4,
        DestructibleTileLoss = 5,
        MovingDeviceWorstPosition = 6,
        CombinedAdverseStaticShell = 7,
    }

    public enum GeneratedWorstCaseTransitionRole
    {
        StaticRequired = 1,
        OptionalTool = 2,
        OptionalShop = 3,
        OptionalFacility = 4,
        OptionalNpc = 5,
        OptionalVillageHostile = 6,
        OptionalVillageEvacuated = 7,
        OptionalActivityEvent = 8,
        MovingDevice = 9,
    }

    [Flags]
    public enum GeneratedWorstCaseProtectedCriticalSource
    {
        None = 0,
        MandatoryRoute = 1,
        TraversalEnvelope = 2,
        RequiredLanding = 4,
        RecoveryFloor = 8,
        BoundarySocket = 16,
    }

    public sealed class GeneratedWorstCaseTransitionBinding :
        IComparable<GeneratedWorstCaseTransitionBinding>
    {
        public GeneratedWorstCaseTransitionBinding(
            GeneratedCompletionStateTransition transition,
            GeneratedWorstCaseTransitionRole role,
            string sourceOwner,
            string sourceDigest)
        {
            Transition = transition;
            Role = role;
            SourceOwner = Normalize(sourceOwner);
            SourceDigest = Normalize(sourceDigest);
        }

        public GeneratedCompletionStateTransition Transition { get; }
        public GeneratedWorstCaseTransitionRole Role { get; }
        public string SourceOwner { get; }
        public string SourceDigest { get; }
        public string StableToken => string.Join("|", new[]
        {
            "MAP19_06_TRANSITION_BINDING_V1",
            Transition == null ? "MISSING_TRANSITION" : Transition.StableToken,
            Number((int)Role), SourceOwner, SourceDigest,
        });
        public int CompareTo(GeneratedWorstCaseTransitionBinding other) => other == null
            ? -1 : string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
        internal static string Normalize(string value) => string.IsNullOrWhiteSpace(value)
            ? "MISSING" : BakingCanonicalDigest.NormalizeLineEndingsToLf(value).Trim();
        internal static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedWorstCaseLogicalCellCandidate :
        IComparable<GeneratedWorstCaseLogicalCellCandidate>
    {
        public GeneratedWorstCaseLogicalCellCandidate(
            string overlayId,
            GeneratedSectorModificationTarget target,
            GeneratedSectorModificationKind modificationKind,
            GeneratedSectorModificationPayload payload,
            string sourceOwner,
            string sourceDigest,
            GeneratedWorstCaseProtectedCriticalSource protectedSources =
                GeneratedWorstCaseProtectedCriticalSource.None)
        {
            OverlayId = GeneratedWorstCaseTransitionBinding.Normalize(overlayId);
            Target = target;
            ModificationKind = modificationKind;
            Payload = payload;
            SourceOwner = GeneratedWorstCaseTransitionBinding.Normalize(sourceOwner);
            SourceDigest = GeneratedWorstCaseTransitionBinding.Normalize(sourceDigest);
            ProtectedSources = protectedSources;
        }

        public string OverlayId { get; }
        public GeneratedSectorModificationTarget Target { get; }
        public GeneratedSectorModificationKind ModificationKind { get; }
        public GeneratedSectorModificationPayload Payload { get; }
        public string SourceOwner { get; }
        public string SourceDigest { get; }
        public GeneratedWorstCaseProtectedCriticalSource ProtectedSources { get; }
        public bool IsDestructibleOrReplaceable =>
            ModificationKind == GeneratedSectorModificationKind.DestroyTile ||
            ModificationKind == GeneratedSectorModificationKind.ReplaceTile;
        public bool IsProtectedCritical => ProtectedSources !=
            GeneratedWorstCaseProtectedCriticalSource.None;
        public string StableToken => string.Join("|", new[]
        {
            "MAP19_06_LOGICAL_CELL_V1", OverlayId,
            Target == null ? "MISSING_TARGET" : Target.StableToken,
            GeneratedWorstCaseTransitionBinding.Number((int)ModificationKind),
            Payload == null ? "MISSING_PAYLOAD" : Payload.StableToken,
            SourceOwner, SourceDigest,
            GeneratedWorstCaseTransitionBinding.Number((int)ProtectedSources),
        });
        public int CompareTo(GeneratedWorstCaseLogicalCellCandidate other) => other == null
            ? -1 : string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
    }

    public sealed class GeneratedWorstCaseDeviceCandidate :
        IComparable<GeneratedWorstCaseDeviceCandidate>
    {
        private readonly ReadOnlyCollection<string> transitionIds;
        private readonly ReadOnlyCollection<string> fallbackTransitionIds;

        public GeneratedWorstCaseDeviceCandidate(
            string deviceId,
            GeneratedSectorModificationTarget target,
            GeneratedSectorModificationPayload worstStatePayload,
            string sourceOwner,
            string sourceDigest,
            bool isMovingOrStateful,
            bool providesOnlyMandatoryMovement,
            IEnumerable<string> sourceTransitionIds,
            IEnumerable<string> sourceFallbackTransitionIds)
        {
            DeviceId = GeneratedWorstCaseTransitionBinding.Normalize(deviceId);
            Target = target;
            WorstStatePayload = worstStatePayload;
            SourceOwner = GeneratedWorstCaseTransitionBinding.Normalize(sourceOwner);
            SourceDigest = GeneratedWorstCaseTransitionBinding.Normalize(sourceDigest);
            IsMovingOrStateful = isMovingOrStateful;
            ProvidesOnlyMandatoryMovement = providesOnlyMandatoryMovement;
            transitionIds = Freeze(sourceTransitionIds);
            fallbackTransitionIds = Freeze(sourceFallbackTransitionIds);
        }

        public string DeviceId { get; }
        public GeneratedSectorModificationTarget Target { get; }
        public GeneratedSectorModificationPayload WorstStatePayload { get; }
        public string SourceOwner { get; }
        public string SourceDigest { get; }
        public bool IsMovingOrStateful { get; }
        public bool ProvidesOnlyMandatoryMovement { get; }
        public IReadOnlyList<string> TransitionIds => transitionIds;
        public IReadOnlyList<string> FallbackTransitionIds => fallbackTransitionIds;
        public string StableToken => string.Join("|", new[]
        {
            "MAP19_06_DEVICE_V1", DeviceId,
            Target == null ? "MISSING_TARGET" : Target.StableToken,
            WorstStatePayload == null ? "MISSING_STATE" : WorstStatePayload.StableToken,
            SourceOwner, SourceDigest, IsMovingOrStateful ? "1" : "0",
            ProvidesOnlyMandatoryMovement ? "1" : "0",
            string.Join(",", transitionIds), string.Join(",", fallbackTransitionIds),
        });
        public int CompareTo(GeneratedWorstCaseDeviceCandidate other) => other == null
            ? -1 : string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);

        private static ReadOnlyCollection<string> Freeze(IEnumerable<string> values) =>
            new ReadOnlyCollection<string>((values ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim()).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
    }

    public sealed class GeneratedWorstCaseScenarioTransform :
        IComparable<GeneratedWorstCaseScenarioTransform>
    {
        private readonly ReadOnlyCollection<GeneratedWorstCaseTransitionRole> removedRoles;
        private readonly ReadOnlyCollection<string> logicalOverlayIds;
        private readonly ReadOnlyCollection<string> disabledDeviceIds;

        public GeneratedWorstCaseScenarioTransform(
            string scenarioId,
            GeneratedWorstCaseScenarioKind kind,
            string sourceOwner,
            string sourceDigest,
            IEnumerable<GeneratedWorstCaseTransitionRole> sourceRemovedRoles = null,
            IEnumerable<string> sourceLogicalOverlayIds = null,
            IEnumerable<string> sourceDisabledDeviceIds = null,
            ulong allowedToolMask = 0)
        {
            ScenarioId = GeneratedWorstCaseTransitionBinding.Normalize(scenarioId);
            Kind = kind;
            SourceOwner = GeneratedWorstCaseTransitionBinding.Normalize(sourceOwner);
            SourceDigest = GeneratedWorstCaseTransitionBinding.Normalize(sourceDigest);
            removedRoles = new ReadOnlyCollection<GeneratedWorstCaseTransitionRole>((
                sourceRemovedRoles ?? Array.Empty<GeneratedWorstCaseTransitionRole>())
                .Distinct().OrderBy(value => (int)value).ToArray());
            logicalOverlayIds = Strings(sourceLogicalOverlayIds);
            disabledDeviceIds = Strings(sourceDisabledDeviceIds);
            AllowedToolMask = allowedToolMask;
        }

        public string ScenarioId { get; }
        public GeneratedWorstCaseScenarioKind Kind { get; }
        public string SourceOwner { get; }
        public string SourceDigest { get; }
        public IReadOnlyList<GeneratedWorstCaseTransitionRole> RemovedRoles => removedRoles;
        public IReadOnlyList<string> LogicalOverlayIds => logicalOverlayIds;
        public IReadOnlyList<string> DisabledDeviceIds => disabledDeviceIds;
        public ulong AllowedToolMask { get; }
        public string StableToken => string.Join("|", new[]
        {
            "MAP19_06_SCENARIO_TRANSFORM_V1", ScenarioId,
            GeneratedWorstCaseTransitionBinding.Number((int)Kind), SourceOwner, SourceDigest,
            GeneratedWorstCaseTransitionBinding.Number((int)AllowedToolMask),
            string.Join(",", removedRoles.Select(value =>
                GeneratedWorstCaseTransitionBinding.Number((int)value))),
            string.Join(",", logicalOverlayIds), string.Join(",", disabledDeviceIds),
        });
        public int CompareTo(GeneratedWorstCaseScenarioTransform other) => other == null
            ? -1 : string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);

        private static ReadOnlyCollection<string> Strings(IEnumerable<string> values) =>
            new ReadOnlyCollection<string>((values ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim())
                .Distinct(StringComparer.Ordinal).OrderBy(value => value,
                    StringComparer.Ordinal).ToArray());
    }

    public sealed class GeneratedWorstCaseScenarioCatalog
    {
        private readonly ReadOnlyCollection<GeneratedWorstCaseScenarioTransform> scenarios;
        public GeneratedWorstCaseScenarioCatalog(
            IEnumerable<GeneratedWorstCaseScenarioTransform> sourceScenarios)
        {
            scenarios = new ReadOnlyCollection<GeneratedWorstCaseScenarioTransform>((
                sourceScenarios ?? Array.Empty<GeneratedWorstCaseScenarioTransform>())
                .Where(value => value != null).OrderBy(value => value).ToArray());
            Digest = BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MAP19_06_SCENARIO_CATALOG_V1" }.Concat(
                    scenarios.Select(value => value.StableToken)));
        }
        public IReadOnlyList<GeneratedWorstCaseScenarioTransform> Scenarios => scenarios;
        public string Digest { get; }
    }

    public sealed class GeneratedWorstCaseScenarioActionAudit
    {
        public GeneratedWorstCaseScenarioActionAudit(
            int graphMutationAttempts = 0,
            int proofRewriteAttempts = 0,
            int runtimeQueryAttempts = 0,
            int worldMutationAttempts = 0,
            int terrainMutationAttempts = 0,
            int physicsQueryAttempts = 0,
            int seedBatchAttempts = 0,
            bool map19_07Started = false)
        {
            GraphMutationAttempts = graphMutationAttempts;
            ProofRewriteAttempts = proofRewriteAttempts;
            RuntimeQueryAttempts = runtimeQueryAttempts;
            WorldMutationAttempts = worldMutationAttempts;
            TerrainMutationAttempts = terrainMutationAttempts;
            PhysicsQueryAttempts = physicsQueryAttempts;
            SeedBatchAttempts = seedBatchAttempts;
            Map19_07Started = map19_07Started;
        }
        public int GraphMutationAttempts { get; }
        public int ProofRewriteAttempts { get; }
        public int RuntimeQueryAttempts { get; }
        public int WorldMutationAttempts { get; }
        public int TerrainMutationAttempts { get; }
        public int PhysicsQueryAttempts { get; }
        public int SeedBatchAttempts { get; }
        public bool Map19_07Started { get; }
        public bool IsZero => GraphMutationAttempts == 0 && ProofRewriteAttempts == 0 &&
            RuntimeQueryAttempts == 0 && WorldMutationAttempts == 0 &&
            TerrainMutationAttempts == 0 && PhysicsQueryAttempts == 0 &&
            SeedBatchAttempts == 0 && !Map19_07Started;
        public string StableToken => string.Join("|", new[]
        {
            "MAP19_06_ACTION_AUDIT_V1", Number(GraphMutationAttempts),
            Number(ProofRewriteAttempts), Number(RuntimeQueryAttempts),
            Number(WorldMutationAttempts), Number(TerrainMutationAttempts),
            Number(PhysicsQueryAttempts), Number(SeedBatchAttempts),
            Map19_07Started ? "1" : "0",
        });
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedWorstCaseScenarioInput
    {
        private readonly ReadOnlyCollection<GeneratedWorstCaseTransitionBinding> transitions;
        private readonly ReadOnlyCollection<GeneratedWorstCaseLogicalCellCandidate> cells;
        private readonly ReadOnlyCollection<GeneratedWorstCaseDeviceCandidate> devices;
        private readonly ReadOnlyCollection<TraversalMovementKind> movements;

        public GeneratedWorstCaseScenarioInput(
            GeneratedTileMovementGraph graph,
            NakedTraversalSearchResult nakedSearch,
            GeneratedCompletionSearchResult completionSearch,
            ClusterRecoveryValidationResult clusterValidation,
            GeneratedRepetitionValidationResult repetitionValidation,
            VillageStateMarkerSetDefinition villageStateMarkers,
            GeneratedWorstCaseScenarioCatalog catalog,
            IEnumerable<GeneratedWorstCaseTransitionBinding> sourceTransitions,
            GeneratedCompletionGoal goal,
            IEnumerable<TraversalMovementKind> allowedMovementKinds,
            IEnumerable<GeneratedWorstCaseLogicalCellCandidate> logicalCellCandidates,
            IEnumerable<GeneratedWorstCaseDeviceCandidate> deviceCandidates,
            GeneratedWorstCaseScenarioActionAudit actionAudit = null,
            string declaredMap19_04CombinedDigest = null,
            string declaredMap19_05CombinedDigest = null,
            string declaredIncomingHandoffDigest = null)
        {
            Graph = graph;
            NakedSearch = nakedSearch;
            CompletionSearch = completionSearch;
            ClusterValidation = clusterValidation;
            RepetitionValidation = repetitionValidation;
            VillageStateMarkers = villageStateMarkers;
            Catalog = catalog;
            transitions = Freeze(sourceTransitions);
            Goal = goal;
            movements = new ReadOnlyCollection<TraversalMovementKind>((allowedMovementKinds ??
                Array.Empty<TraversalMovementKind>()).Distinct().OrderBy(value => (int)value)
                .ToArray());
            cells = Freeze(logicalCellCandidates);
            devices = Freeze(deviceCandidates);
            ActionAudit = actionAudit ?? new GeneratedWorstCaseScenarioActionAudit();
            DeclaredMap19_04CombinedDigest = declaredMap19_04CombinedDigest ??
                (clusterValidation == null ? "MISSING" : clusterValidation.CombinedSuccessDigest);
            DeclaredMap19_05CombinedDigest = declaredMap19_05CombinedDigest ??
                (repetitionValidation == null ? "MISSING" :
                    repetitionValidation.CombinedSuccessDigest);
            DeclaredIncomingHandoffDigest = declaredIncomingHandoffDigest ??
                (repetitionValidation == null ? "MISSING" :
                    repetitionValidation.Map19_06HandoffDigest);
        }

        public GeneratedTileMovementGraph Graph { get; }
        public NakedTraversalSearchResult NakedSearch { get; }
        public GeneratedCompletionSearchResult CompletionSearch { get; }
        public ClusterRecoveryValidationResult ClusterValidation { get; }
        public GeneratedRepetitionValidationResult RepetitionValidation { get; }
        public VillageStateMarkerSetDefinition VillageStateMarkers { get; }
        public GeneratedWorstCaseScenarioCatalog Catalog { get; }
        public IReadOnlyList<GeneratedWorstCaseTransitionBinding> Transitions => transitions;
        public GeneratedCompletionGoal Goal { get; }
        public IReadOnlyList<TraversalMovementKind> AllowedMovementKinds => movements;
        public IReadOnlyList<GeneratedWorstCaseLogicalCellCandidate> LogicalCellCandidates => cells;
        public IReadOnlyList<GeneratedWorstCaseDeviceCandidate> DeviceCandidates => devices;
        public GeneratedWorstCaseScenarioActionAudit ActionAudit { get; }
        public string DeclaredMap19_04CombinedDigest { get; }
        public string DeclaredMap19_05CombinedDigest { get; }
        public string DeclaredIncomingHandoffDigest { get; }

        public string ComputeDigest() => BakingCanonicalDigest.HashCanonicalLines(new[]
        {
            "MAP19_06_WORST_CASE_INPUT_V1",
            Graph == null ? "MISSING_GRAPH" : Graph.GraphDigest,
            NakedSearch == null ? "MISSING_NAKED" : NakedSearch.SuccessProofDigest,
            CompletionSearch == null ? "MISSING_COMPLETION" :
                CompletionSearch.SuccessProofDigest,
            ClusterValidation == null ? "MISSING_MAP19_04" :
                ClusterValidation.CombinedSuccessDigest,
            RepetitionValidation == null ? "MISSING_MAP19_05" :
                RepetitionValidation.CombinedSuccessDigest,
            DeclaredMap19_04CombinedDigest, DeclaredMap19_05CombinedDigest,
            DeclaredIncomingHandoffDigest,
            VillageStateMarkers == null ? "MISSING_VILLAGE" : VillageToken(VillageStateMarkers),
            Catalog == null ? "MISSING_CATALOG" : Catalog.Digest,
            Goal == null ? "MISSING_GOAL" : Goal.StableToken,
            "MOVEMENTS=" + string.Join(",", movements.Select(value =>
                GeneratedWorstCaseTransitionBinding.Number((int)value))),
            ActionAudit == null ? "MISSING_AUDIT" : ActionAudit.StableToken,
        }.Concat(transitions.Select(value => value.StableToken))
         .Concat(cells.Select(value => value.StableToken))
         .Concat(devices.Select(value => value.StableToken)));

        internal static string VillageToken(VillageStateMarkerSetDefinition value) =>
            BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_06_VILLAGE_STATE_BINDING_V1", value.IndividualHostileTargetMarkerId,
                string.Join(",", value.RequestedVariants.OrderBy(item => (int)item)
                    .Select(item => GeneratedWorstCaseTransitionBinding.Number((int)item))),
            }.Concat(value.NpcMarkers.OrderBy(item => item.MarkerId,
                    StringComparer.Ordinal).Select(item => "NPC|" + item.MarkerId + "|" +
                        item.FacilityBindingId))
             .Concat(value.InventoryMarkers.OrderBy(item => item.MarkerId,
                    StringComparer.Ordinal).Select(item => "SHOP|" + item.MarkerId + "|" +
                        item.FacilityBindingId))
             .Concat(value.DoorMarkers.OrderBy(item => item.MarkerId,
                    StringComparer.Ordinal).Select(item => "FACILITY|" + item.MarkerId + "|" +
                        item.FacilityBindingId)));

        private static ReadOnlyCollection<T> Freeze<T>(IEnumerable<T> source)
            where T : class, IComparable<T> => new ReadOnlyCollection<T>((source ??
                Array.Empty<T>()).Where(value => value != null).OrderBy(value => value).ToArray());
    }

    public sealed class GeneratedWorstCaseScenarioProof :
        IComparable<GeneratedWorstCaseScenarioProof>
    {
        private readonly ReadOnlyCollection<string> removedTransitionIds;
        private readonly ReadOnlyCollection<string> logicalOverlayIds;
        private readonly ReadOnlyCollection<string> retainedGoalIds;

        internal GeneratedWorstCaseScenarioProof(
            GeneratedWorstCaseScenarioTransform scenario,
            string scenarioInputDigest,
            IEnumerable<string> sourceRemovedTransitionIds,
            IEnumerable<string> sourceLogicalOverlayIds,
            IEnumerable<string> sourceRetainedGoalIds,
            GeneratedCompletionSearchResult completion)
        {
            ScenarioId = scenario.ScenarioId;
            Kind = scenario.Kind;
            SourceOwner = scenario.SourceOwner;
            SourceDigest = scenario.SourceDigest;
            ScenarioInputDigest = scenarioInputDigest;
            removedTransitionIds = Strings(sourceRemovedTransitionIds);
            logicalOverlayIds = Strings(sourceLogicalOverlayIds);
            retainedGoalIds = Strings(sourceRetainedGoalIds);
            Completion = completion;
            ProofDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_06_SCENARIO_PROOF_V1", ScenarioId,
                GeneratedWorstCaseTransitionBinding.Number((int)Kind), SourceOwner,
                SourceDigest, ScenarioInputDigest, string.Join(",", removedTransitionIds),
                string.Join(",", logicalOverlayIds), string.Join(",", retainedGoalIds),
                completion.Proof.InputDigest, completion.Proof.StateSpaceDigest,
                completion.Proof.ProofDigest,
                GeneratedWorstCaseTransitionBinding.Number(
                    completion.Proof.ShortestTransitionCount), "PASS",
            });
        }
        public string ScenarioId { get; }
        public GeneratedWorstCaseScenarioKind Kind { get; }
        public string SourceOwner { get; }
        public string SourceDigest { get; }
        public string ScenarioInputDigest { get; }
        public IReadOnlyList<string> RemovedTransitionIds => removedTransitionIds;
        public IReadOnlyList<string> LogicalOverlayIds => logicalOverlayIds;
        public IReadOnlyList<string> RetainedGoalIds => retainedGoalIds;
        public GeneratedCompletionSearchResult Completion { get; }
        public bool Passed => Completion != null && Completion.Success;
        public string ProofDigest { get; }
        public int CompareTo(GeneratedWorstCaseScenarioProof other) => other == null ? -1 :
            string.Compare(ScenarioId + "|" + ProofDigest, other.ScenarioId + "|" +
                other.ProofDigest, StringComparison.Ordinal);
        private static ReadOnlyCollection<string> Strings(IEnumerable<string> values) =>
            new ReadOnlyCollection<string>((values ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
    }

    public sealed class GeneratedWorstCaseScenarioFailure :
        IComparable<GeneratedWorstCaseScenarioFailure>
    {
        public GeneratedWorstCaseScenarioFailure(string owner, string reason,
            string scenarioId, string caseId, string offendingKey, string expected,
            string actual, string sourceDigest, string frontierOrOverlayEvidence)
        {
            Owner = Value(owner);
            Reason = Value(reason);
            ScenarioId = Value(scenarioId);
            CaseId = Value(caseId);
            OffendingKey = Value(offendingKey);
            Expected = Value(expected);
            Actual = Value(actual);
            SourceDigest = Value(sourceDigest);
            FrontierOrOverlayEvidence = Value(frontierOrOverlayEvidence);
        }
        public string Owner { get; }
        public string Reason { get; }
        public string ScenarioId { get; }
        public string CaseId { get; }
        public string OffendingKey { get; }
        public string Expected { get; }
        public string Actual { get; }
        public string SourceDigest { get; }
        public string FrontierOrOverlayEvidence { get; }
        public string StableToken => string.Join("|", new[] { Owner, Reason, ScenarioId,
            CaseId, OffendingKey, Expected, Actual, SourceDigest,
            FrontierOrOverlayEvidence });
        public int CompareTo(GeneratedWorstCaseScenarioFailure other) => other == null ? -1 :
            string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
        public override string ToString() => StableToken;
        private static string Value(string value) => string.IsNullOrWhiteSpace(value)
            ? "MISSING" : value.Trim();
    }

    public sealed class GeneratedWorstCaseScenarioSurface
    {
        private readonly ReadOnlyCollection<GeneratedWorstCaseScenarioProof> proofs;
        internal GeneratedWorstCaseScenarioSurface(GeneratedWorstCaseScenarioInput input,
            IEnumerable<GeneratedWorstCaseScenarioProof> sourceProofs,
            int destructibleCandidatesChecked, int protectedCriticalRejects,
            int overlaysApplied, int deviceCandidatesChecked, int worstPositionTransforms,
            int fallbackCompletionPasses)
        {
            proofs = new ReadOnlyCollection<GeneratedWorstCaseScenarioProof>((sourceProofs ??
                Array.Empty<GeneratedWorstCaseScenarioProof>()).OrderBy(value => value).ToArray());
            InputDigest = input.ComputeDigest();
            CatalogDigest = input.Catalog.Digest;
            DestructibleCandidatesChecked = destructibleCandidatesChecked;
            ProtectedCriticalRejects = protectedCriticalRejects;
            LogicalOverlaysApplied = overlaysApplied;
            DeviceCandidatesChecked = deviceCandidatesChecked;
            WorstPositionTransforms = worstPositionTransforms;
            FallbackCompletionPasses = fallbackCompletionPasses;
            DestructibleDeviceBoundaryDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_06_DESTRUCTIBLE_DEVICE_BOUNDARY_V1",
                Number(destructibleCandidatesChecked), Number(protectedCriticalRejects),
                Number(overlaysApplied), Number(deviceCandidatesChecked),
                Number(worstPositionTransforms), Number(fallbackCompletionPasses), "0",
            }.Concat(input.LogicalCellCandidates.Select(value => value.StableToken))
             .Concat(input.DeviceCandidates.Select(value => value.StableToken)));
            ScenarioProofDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
                { "MAP19_06_SCENARIO_PROOF_SET_V1", InputDigest, CatalogDigest }
                .Concat(proofs.Select(value => value.ProofDigest)));
            CombinedDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_06_WORST_CASE_COMBINED_V1", input.Graph.GraphDigest,
                input.CompletionSearch.SuccessProofDigest,
                input.ClusterValidation.CombinedSuccessDigest,
                input.RepetitionValidation.CombinedSuccessDigest,
                input.DeclaredIncomingHandoffDigest, InputDigest, CatalogDigest,
                ScenarioProofDigest, DestructibleDeviceBoundaryDigest,
            });
            Map19_07HandoffDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
            {
                "MAP19_07_WORST_CASE_HANDOFF_V1", CombinedDigest,
                ScenarioProofDigest, DestructibleDeviceBoundaryDigest,
                "MAP19_07_LOCKED=1|STARTED=0",
            });
        }
        public IReadOnlyList<GeneratedWorstCaseScenarioProof> Proofs => proofs;
        public int ScenarioKindsChecked => proofs.Select(value => value.Kind).Distinct().Count();
        public int ScenarioInstancesChecked => proofs.Count;
        public int ScenarioInstancesPassed => proofs.Count;
        public int CompletionSearches => proofs.Count;
        public int CompletionGoalsChecked => proofs.Count;
        public int CompletionGoalsSatisfied => proofs.Count;
        public int ShortestTransitionMinimum => proofs.Min(value =>
            value.Completion.Proof.ShortestTransitionCount);
        public int ShortestTransitionMaximum => proofs.Max(value =>
            value.Completion.Proof.ShortestTransitionCount);
        public int DestructibleCandidatesChecked { get; }
        public int ProtectedCriticalRejects { get; }
        public int LogicalOverlaysApplied { get; }
        public int DestructibleOverlayCompletionsPassed => proofs.Count(value =>
            value.Kind == GeneratedWorstCaseScenarioKind.DestructibleTileLoss ||
            value.Kind == GeneratedWorstCaseScenarioKind.CombinedAdverseStaticShell);
        public int DeviceCandidatesChecked { get; }
        public int WorstPositionTransforms { get; }
        public int FallbackCompletionPasses { get; }
        public int FallbackViolations => 0;
        public string InputDigest { get; }
        public string CatalogDigest { get; }
        public string ScenarioProofDigest { get; }
        public string DestructibleDeviceBoundaryDigest { get; }
        public string CombinedDigest { get; }
        public string Map19_07HandoffDigest { get; }
        public bool Map19_07Started => false;
        public int Passed(GeneratedWorstCaseScenarioKind kind) => proofs.Count(value =>
            value.Kind == kind && value.Passed);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class GeneratedWorstCaseScenarioResult
    {
        private readonly ReadOnlyCollection<GeneratedWorstCaseScenarioFailure> failures;
        internal GeneratedWorstCaseScenarioResult(GeneratedWorstCaseScenarioSurface surface,
            IEnumerable<GeneratedWorstCaseScenarioFailure> sourceFailures)
        {
            Surface = surface;
            failures = new ReadOnlyCollection<GeneratedWorstCaseScenarioFailure>((sourceFailures ??
                Array.Empty<GeneratedWorstCaseScenarioFailure>()).Where(value => value != null)
                .OrderBy(value => value).ToArray());
        }
        public bool Success => Surface != null && failures.Count == 0;
        public GeneratedWorstCaseScenarioSurface Surface { get; }
        public IReadOnlyList<GeneratedWorstCaseScenarioFailure> Failures => failures;
        public string WorstCaseInputDigest => Success ? Surface.InputDigest : string.Empty;
        public string ScenarioCatalogDigest => Success ? Surface.CatalogDigest : string.Empty;
        public string ScenarioProofDigest => Success ? Surface.ScenarioProofDigest : string.Empty;
        public string DestructibleDeviceBoundaryDigest => Success ?
            Surface.DestructibleDeviceBoundaryDigest : string.Empty;
        public string CombinedSuccessDigest => Success ? Surface.CombinedDigest : string.Empty;
        public string Map19_07HandoffDigest => Success ? Surface.Map19_07HandoffDigest : string.Empty;
    }
}
