using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Pipeline;

namespace StarNight.Map.WorldGeneration.SpecialRegions
{
    public enum SpecialLandmarkKind
    {
        MoonSealForge = 1,
        BossSealArena = 2,
        WanderingMerchantCave = 3,
        MaruTimeShrine = 4,
    }

    public enum SpecialLandmarkTheme
    {
        AbandonedMill = 1,
        MoonPalaceCommon = 2,
        Any = 3,
    }

    public enum SpecialLandmarkBindingKind
    {
        PlacedMandatorySite = 1,
        DeferredOptionalLocal = 2,
    }

    public enum SpecialLandmarkPlacementStatus
    {
        Placed = 1,
        DeferredToMAP14 = 2,
    }

    public enum SpecialLandmarkRouteKind
    {
        Low = 1,
        High = 2,
        Recovery = 3,
        Return = 4,
    }

    public enum SpecialLandmarkNodeRole
    {
        Entry = 1,
        Workstation = 2,
        Mastery = 3,
        Failure = 4,
        RecoveryJoin = 5,
        RequiredReward = 6,
        Gate = 7,
        Arena = 8,
        Observation = 9,
        Shop = 10,
        Storage = 11,
        Shrine = 12,
        SafeZone = 13,
        Return = 14,
    }

    public enum SpecialLandmarkStateRole
    {
        ForgeReady = 1,
        ResourceAvailable = 2,
        ResourceReserved = 3,
        ResourceConsumed = 4,
        ResourceReturned = 5,
        ForgeSucceeded = 6,
        GateLocked = 7,
        GateAccepted = 8,
        EncounterActive = 9,
        Defeated = 10,
        MerchantAvailable = 11,
        Visited = 12,
        Departed = 13,
        Offered = 14,
        Ignored = 15,
        ShortHint = 16,
        StrongHint = 17,
    }

    public enum SpecialLandmarkTransitionTrigger
    {
        ReserveResource = 1,
        ProcessSucceeded = 2,
        ProcessFailed = 3,
        ForgeCompleted = 4,
        PresentMoonSeal = 5,
        EnterEncounter = 6,
        EncounterFailed = 7,
        BossDefeated = 8,
        MerchantVisited = 9,
        MerchantDeparted = 10,
        IgnoreChoice = 11,
        ChooseShortHint = 12,
        ChooseStrongHint = 13,
    }

    public enum SpecialLandmarkResetPolicy
    {
        ManualReset = 1,
        SafeCorridor = 2,
        EncounterReset = 3,
        StableVisit = 4,
        PersistentChoice = 5,
        SafeReturn = 6,
    }

    public enum SpecialLandmarkMarkerKind
    {
        ForgeProcessStep = 1,
        TimingOptimization = 2,
        MaruAttentionReduction = 3,
        ForgeInput = 4,
        MoonSealOutput = 5,
        BossDirection = 6,
        SafeCorridor = 7,
        MoonSealRequirement = 8,
        LowerRecoveryZone = 9,
        UpperPlatform = 10,
        FallingObject = 11,
        PressureDevice = 12,
        EncounterPersistence = 13,
        SeparateMaruStateOwner = 14,
        ShopSafeZone = 15,
        EntranceCue = 16,
        Shop = 17,
        UpperStorage = 18,
        Information = 19,
        OptionalBenefit = 20,
        NonCombatSafeZone = 21,
        ChoicePreview = 22,
        ShortHint = 23,
        RareTerrainCompass = 24,
        MaruAttentionIncrease = 25,
    }

    public enum SpecialLandmarkDependencyKind
    {
        None = 0,
        Village = 1,
        Tool = 2,
        Inventory = 3,
        OptionalLandmark = 4,
    }

    public enum SpecialLandmarkMerchantVariant
    {
        Alien = 1,
        Rabbit = 2,
        Spacefarer = 3,
        Machine = 4,
    }

    public enum SpecialLandmarkForgeResource
    {
        MoonCore = 1,
        CassiaSap = 2,
        StarNuruk = 3,
    }

    public readonly struct SpecialLandmarkDesignChunk :
        IEquatable<SpecialLandmarkDesignChunk>, IComparable<SpecialLandmarkDesignChunk>
    {
        public SpecialLandmarkDesignChunk(int x, int y) { X = x; Y = y; }
        public int X { get; }
        public int Y { get; }
        public int CompareTo(SpecialLandmarkDesignChunk other)
        {
            var y = Y.CompareTo(other.Y);
            return y != 0 ? y : X.CompareTo(other.X);
        }
        public bool Equals(SpecialLandmarkDesignChunk other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is SpecialLandmarkDesignChunk other && Equals(other);
        public override int GetHashCode() => (X * 397) ^ Y;
        public override string ToString() => X + "," + Y;
    }

    public sealed class SpecialLandmarkShellNode
    {
        public SpecialLandmarkShellNode(
            string nodeId,
            SpecialLandmarkNodeRole role,
            LocalTileCoord coordinate,
            bool required)
        {
            NodeId = nodeId ?? string.Empty;
            Role = role;
            Coordinate = coordinate;
            Required = required;
        }

        public string NodeId { get; }
        public SpecialLandmarkNodeRole Role { get; }
        public LocalTileCoord Coordinate { get; }
        public bool Required { get; }
    }

    public sealed class SpecialLandmarkShellEdge
    {
        public SpecialLandmarkShellEdge(
            string edgeId,
            string fromNodeId,
            string toNodeId,
            SpecialLandmarkRouteKind routeKind,
            int order,
            AccessClass accessClass,
            bool required,
            SpecialLandmarkDependencyKind dependency)
        {
            EdgeId = edgeId ?? string.Empty;
            FromNodeId = fromNodeId ?? string.Empty;
            ToNodeId = toNodeId ?? string.Empty;
            RouteKind = routeKind;
            Order = order;
            AccessClass = accessClass;
            Required = required;
            Dependency = dependency;
        }

        public string EdgeId { get; }
        public string FromNodeId { get; }
        public string ToNodeId { get; }
        public SpecialLandmarkRouteKind RouteKind { get; }
        public int Order { get; }
        public AccessClass AccessClass { get; }
        public bool Required { get; }
        public SpecialLandmarkDependencyKind Dependency { get; }
    }

    public sealed class SpecialLandmarkRouteDefinition
    {
        private readonly ReadOnlyCollection<string> edgeIds;

        public SpecialLandmarkRouteDefinition(
            string routeId,
            SpecialLandmarkRouteKind kind,
            IEnumerable<string> edgeIds,
            string startNodeId,
            string endNodeId)
        {
            RouteId = routeId ?? string.Empty;
            Kind = kind;
            this.edgeIds = new ReadOnlyCollection<string>((edgeIds ?? Array.Empty<string>()).ToArray());
            StartNodeId = startNodeId ?? string.Empty;
            EndNodeId = endNodeId ?? string.Empty;
        }

        public string RouteId { get; }
        public SpecialLandmarkRouteKind Kind { get; }
        public IReadOnlyList<string> EdgeIds => edgeIds;
        public string StartNodeId { get; }
        public string EndNodeId { get; }
    }

    public sealed class SpecialLandmarkStateDefinition
    {
        public SpecialLandmarkStateDefinition(string stateId, SpecialLandmarkStateRole role, bool persistent)
        {
            StateId = stateId ?? string.Empty;
            Role = role;
            Persistent = persistent;
        }

        public string StateId { get; }
        public SpecialLandmarkStateRole Role { get; }
        public bool Persistent { get; }
    }

    public sealed class SpecialLandmarkStateTransitionDefinition
    {
        public SpecialLandmarkStateTransitionDefinition(
            string transitionId,
            string fromStateId,
            string toStateId,
            SpecialLandmarkTransitionTrigger trigger,
            int order)
        {
            TransitionId = transitionId ?? string.Empty;
            FromStateId = fromStateId ?? string.Empty;
            ToStateId = toStateId ?? string.Empty;
            Trigger = trigger;
            Order = order;
        }

        public string TransitionId { get; }
        public string FromStateId { get; }
        public string ToStateId { get; }
        public SpecialLandmarkTransitionTrigger Trigger { get; }
        public int Order { get; }
    }

    public sealed class SpecialLandmarkResetDefinition
    {
        public SpecialLandmarkResetDefinition(
            string resetId,
            SpecialLandmarkResetPolicy policy,
            string failureNodeId,
            string recoveryNodeId,
            string fromStateId,
            string toStateId,
            bool returnsAllForgeInputs,
            bool preservesSealAcceptance,
            bool preventsReroll)
        {
            ResetId = resetId ?? string.Empty;
            Policy = policy;
            FailureNodeId = failureNodeId ?? string.Empty;
            RecoveryNodeId = recoveryNodeId ?? string.Empty;
            FromStateId = fromStateId ?? string.Empty;
            ToStateId = toStateId ?? string.Empty;
            ReturnsAllForgeInputs = returnsAllForgeInputs;
            PreservesSealAcceptance = preservesSealAcceptance;
            PreventsReroll = preventsReroll;
        }

        public string ResetId { get; }
        public SpecialLandmarkResetPolicy Policy { get; }
        public string FailureNodeId { get; }
        public string RecoveryNodeId { get; }
        public string FromStateId { get; }
        public string ToStateId { get; }
        public bool ReturnsAllForgeInputs { get; }
        public bool PreservesSealAcceptance { get; }
        public bool PreventsReroll { get; }
    }

    public sealed class SpecialLandmarkMarkerDefinition
    {
        public SpecialLandmarkMarkerDefinition(
            string markerId,
            SpecialLandmarkMarkerKind kind,
            string nodeId,
            string stateId,
            int order,
            bool required,
            SpecialLandmarkDependencyKind dependency,
            SpecialPersistenceScope persistenceScope = default(SpecialPersistenceScope),
            SpecialPersistenceKey persistenceKey = default(SpecialPersistenceKey))
        {
            MarkerId = markerId ?? string.Empty;
            Kind = kind;
            NodeId = nodeId ?? string.Empty;
            StateId = stateId ?? string.Empty;
            Order = order;
            Required = required;
            Dependency = dependency;
            PersistenceScope = persistenceScope;
            PersistenceKey = persistenceKey;
        }

        public string MarkerId { get; }
        public SpecialLandmarkMarkerKind Kind { get; }
        public string NodeId { get; }
        public string StateId { get; }
        public int Order { get; }
        public bool Required { get; }
        public SpecialLandmarkDependencyKind Dependency { get; }
        public SpecialPersistenceScope PersistenceScope { get; }
        public SpecialPersistenceKey PersistenceKey { get; }
    }

    public sealed class SpecialLandmarkForgeLedgerDefinition
    {
        public SpecialLandmarkForgeLedgerDefinition(
            SpecialLandmarkForgeResource resource,
            string availableStateId,
            string reservedStateId,
            string consumedStateId,
            string returnedStateId)
        {
            Resource = resource;
            AvailableStateId = availableStateId ?? string.Empty;
            ReservedStateId = reservedStateId ?? string.Empty;
            ConsumedStateId = consumedStateId ?? string.Empty;
            ReturnedStateId = returnedStateId ?? string.Empty;
        }

        public SpecialLandmarkForgeResource Resource { get; }
        public string AvailableStateId { get; }
        public string ReservedStateId { get; }
        public string ConsumedStateId { get; }
        public string ReturnedStateId { get; }
    }

    public sealed class SpecialLandmarkRewardDefinition
    {
        public SpecialLandmarkRewardDefinition(
            string rewardId,
            string nodeId,
            SpecialRegionSlotId slotId,
            SpecialPersistenceKey persistenceKey,
            int amount,
            bool required)
        {
            RewardId = rewardId ?? string.Empty;
            NodeId = nodeId ?? string.Empty;
            SlotId = slotId;
            PersistenceKey = persistenceKey;
            Amount = amount;
            Required = required;
        }

        public string RewardId { get; }
        public string NodeId { get; }
        public SpecialRegionSlotId SlotId { get; }
        public SpecialPersistenceKey PersistenceKey { get; }
        public int Amount { get; }
        public bool Required { get; }
    }

    public sealed class SpecialLandmarkRegionDefinition
    {
        private readonly ReadOnlyCollection<SpecialLandmarkDesignChunk> activeDesignChunks;
        private readonly ReadOnlyCollection<SpecialLandmarkShellNode> nodes;
        private readonly ReadOnlyCollection<SpecialLandmarkShellEdge> edges;
        private readonly ReadOnlyCollection<SpecialLandmarkRouteDefinition> routes;
        private readonly ReadOnlyCollection<SpecialLandmarkStateDefinition> states;
        private readonly ReadOnlyCollection<SpecialLandmarkStateTransitionDefinition> transitions;
        private readonly ReadOnlyCollection<SpecialLandmarkResetDefinition> resets;
        private readonly ReadOnlyCollection<SpecialLandmarkMarkerDefinition> markers;
        private readonly ReadOnlyCollection<SpecialLandmarkForgeLedgerDefinition> forgeLedgers;
        private readonly ReadOnlyCollection<SpecialLandmarkMerchantVariant> merchantVariants;

        public SpecialLandmarkRegionDefinition(
            SpecialRegionId regionId,
            SpecialLandmarkKind landmark,
            SpecialRegionKind regionKind,
            SpecialLandmarkTheme theme,
            SpecialLandmarkBindingKind binding,
            int reservedWidth,
            int reservedHeight,
            LocalTileCoord designOrigin,
            int designWidth,
            int designHeight,
            int designChunkWidth,
            int designChunkHeight,
            IEnumerable<SpecialLandmarkDesignChunk> activeDesignChunks,
            IEnumerable<SpecialLandmarkShellNode> nodes,
            IEnumerable<SpecialLandmarkShellEdge> edges,
            IEnumerable<SpecialLandmarkRouteDefinition> routes,
            IEnumerable<SpecialLandmarkStateDefinition> states,
            IEnumerable<SpecialLandmarkStateTransitionDefinition> transitions,
            IEnumerable<SpecialLandmarkResetDefinition> resets,
            IEnumerable<SpecialLandmarkMarkerDefinition> markers,
            IEnumerable<SpecialLandmarkForgeLedgerDefinition> forgeLedgers,
            SpecialLandmarkRewardDefinition requiredReward,
            IEnumerable<SpecialLandmarkMerchantVariant> merchantVariants,
            bool introducesNewMovementRule,
            bool mandatoryProgressionDependency,
            bool stateMutatesShell,
            string displayText)
        {
            RegionId = regionId;
            Landmark = landmark;
            RegionKind = regionKind;
            Theme = theme;
            Binding = binding;
            ReservedWidth = reservedWidth;
            ReservedHeight = reservedHeight;
            DesignOrigin = designOrigin;
            DesignWidth = designWidth;
            DesignHeight = designHeight;
            DesignChunkWidth = designChunkWidth;
            DesignChunkHeight = designChunkHeight;
            this.activeDesignChunks = Freeze(activeDesignChunks, (left, right) => left.CompareTo(right));
            this.nodes = Freeze(nodes, (left, right) => string.Compare(left.NodeId, right.NodeId, StringComparison.Ordinal));
            this.edges = Freeze(edges, (left, right) => string.Compare(left.EdgeId, right.EdgeId, StringComparison.Ordinal));
            this.routes = Freeze(routes, (left, right) => string.Compare(left.RouteId, right.RouteId, StringComparison.Ordinal));
            this.states = Freeze(states, (left, right) => string.Compare(left.StateId, right.StateId, StringComparison.Ordinal));
            this.transitions = Freeze(transitions, (left, right) => string.Compare(left.TransitionId, right.TransitionId, StringComparison.Ordinal));
            this.resets = Freeze(resets, (left, right) => string.Compare(left.ResetId, right.ResetId, StringComparison.Ordinal));
            this.markers = Freeze(markers, (left, right) => string.Compare(left.MarkerId, right.MarkerId, StringComparison.Ordinal));
            this.forgeLedgers = Freeze(forgeLedgers, (left, right) => left.Resource.CompareTo(right.Resource));
            RequiredReward = requiredReward;
            this.merchantVariants = new ReadOnlyCollection<SpecialLandmarkMerchantVariant>(
                (merchantVariants ?? Array.Empty<SpecialLandmarkMerchantVariant>()).Distinct().OrderBy(value => value).ToArray());
            IntroducesNewMovementRule = introducesNewMovementRule;
            MandatoryProgressionDependency = mandatoryProgressionDependency;
            StateMutatesShell = stateMutatesShell;
            DisplayText = displayText ?? string.Empty;
            CanonicalDigest = SpecialLandmarkCanonicalDigest.ComputeDefinition(this);
        }

        public SpecialRegionId RegionId { get; }
        public SpecialLandmarkKind Landmark { get; }
        public SpecialRegionKind RegionKind { get; }
        public SpecialLandmarkTheme Theme { get; }
        public SpecialLandmarkBindingKind Binding { get; }
        public int ReservedWidth { get; }
        public int ReservedHeight { get; }
        public LocalTileCoord DesignOrigin { get; }
        public int DesignWidth { get; }
        public int DesignHeight { get; }
        public int DesignChunkWidth { get; }
        public int DesignChunkHeight { get; }
        public IReadOnlyList<SpecialLandmarkDesignChunk> ActiveDesignChunks => activeDesignChunks;
        public IReadOnlyList<SpecialLandmarkShellNode> Nodes => nodes;
        public IReadOnlyList<SpecialLandmarkShellEdge> Edges => edges;
        public IReadOnlyList<SpecialLandmarkRouteDefinition> Routes => routes;
        public IReadOnlyList<SpecialLandmarkStateDefinition> States => states;
        public IReadOnlyList<SpecialLandmarkStateTransitionDefinition> Transitions => transitions;
        public IReadOnlyList<SpecialLandmarkResetDefinition> Resets => resets;
        public IReadOnlyList<SpecialLandmarkMarkerDefinition> Markers => markers;
        public IReadOnlyList<SpecialLandmarkForgeLedgerDefinition> ForgeLedgers => forgeLedgers;
        public SpecialLandmarkRewardDefinition RequiredReward { get; }
        public IReadOnlyList<SpecialLandmarkMerchantVariant> MerchantVariants => merchantVariants;
        public bool IntroducesNewMovementRule { get; }
        public bool MandatoryProgressionDependency { get; }
        public bool StateMutatesShell { get; }
        public string DisplayText { get; }
        public string CanonicalDigest { get; }

        private static ReadOnlyCollection<T> Freeze<T>(IEnumerable<T> source, Comparison<T> comparison)
        {
            var values = (source ?? Array.Empty<T>()).ToArray();
            Array.Sort(values, comparison);
            return new ReadOnlyCollection<T>(values);
        }
    }

    public sealed class SpecialLandmarkRouteWitness
    {
        private readonly ReadOnlyCollection<string> nodeIds;

        internal SpecialLandmarkRouteWitness(
            string routeId,
            SpecialLandmarkRouteKind kind,
            IEnumerable<string> nodeIds)
        {
            RouteId = routeId ?? string.Empty;
            Kind = kind;
            this.nodeIds = new ReadOnlyCollection<string>((nodeIds ?? Array.Empty<string>()).ToArray());
        }

        public string RouteId { get; }
        public SpecialLandmarkRouteKind Kind { get; }
        public IReadOnlyList<string> NodeIds => nodeIds;
    }

    public sealed class SpecialLandmarkRegionPlan
    {
        private readonly ReadOnlyCollection<SpecialLandmarkDesignChunk> activeDesignChunks;
        private readonly ReadOnlyCollection<SpecialLandmarkShellNode> nodes;
        private readonly ReadOnlyCollection<SpecialLandmarkShellEdge> edges;
        private readonly ReadOnlyCollection<SpecialLandmarkRouteDefinition> routes;
        private readonly ReadOnlyCollection<SpecialLandmarkStateDefinition> states;
        private readonly ReadOnlyCollection<SpecialLandmarkStateTransitionDefinition> transitions;
        private readonly ReadOnlyCollection<SpecialLandmarkResetDefinition> resets;
        private readonly ReadOnlyCollection<SpecialLandmarkMarkerDefinition> markers;
        private readonly ReadOnlyCollection<SpecialLandmarkForgeLedgerDefinition> forgeLedgers;
        private readonly ReadOnlyCollection<SpecialLandmarkMerchantVariant> merchantVariants;
        private readonly ReadOnlyCollection<SpecialLandmarkRouteWitness> witnesses;

        internal SpecialLandmarkRegionPlan(
            SpecialLandmarkRegionDefinition definition,
            SpecialLandmarkPlacementStatus placementStatus,
            IEnumerable<SpecialLandmarkRouteWitness> witnesses,
            string bridgeDigest,
            string entryBufferDigest,
            string collisionDigest,
            string fixedSlotLayerDigest,
            string rewardSafetyDigest,
            string coreResourceDigest)
        {
            RegionId = definition.RegionId;
            Landmark = definition.Landmark;
            RegionKind = definition.RegionKind;
            Theme = definition.Theme;
            Binding = definition.Binding;
            PlacementStatus = placementStatus;
            ReservedWidth = definition.ReservedWidth;
            ReservedHeight = definition.ReservedHeight;
            DesignOrigin = definition.DesignOrigin;
            DesignWidth = definition.DesignWidth;
            DesignHeight = definition.DesignHeight;
            DesignChunkWidth = definition.DesignChunkWidth;
            DesignChunkHeight = definition.DesignChunkHeight;
            activeDesignChunks = Copy(definition.ActiveDesignChunks);
            nodes = Copy(definition.Nodes);
            edges = Copy(definition.Edges);
            routes = Copy(definition.Routes);
            states = Copy(definition.States);
            transitions = Copy(definition.Transitions);
            resets = Copy(definition.Resets);
            markers = Copy(definition.Markers);
            forgeLedgers = Copy(definition.ForgeLedgers);
            RequiredReward = definition.RequiredReward;
            merchantVariants = Copy(definition.MerchantVariants);
            IntroducesNewMovementRule = definition.IntroducesNewMovementRule;
            MandatoryProgressionDependency = definition.MandatoryProgressionDependency;
            this.witnesses = new ReadOnlyCollection<SpecialLandmarkRouteWitness>(
                (witnesses ?? Array.Empty<SpecialLandmarkRouteWitness>())
                .OrderBy(value => value.RouteId, StringComparer.Ordinal).ToArray());
            BridgeDigest = bridgeDigest ?? string.Empty;
            EntryBufferDigest = entryBufferDigest ?? string.Empty;
            CollisionDigest = collisionDigest ?? string.Empty;
            FixedSlotLayerDigest = fixedSlotLayerDigest ?? string.Empty;
            RewardSafetyDigest = rewardSafetyDigest ?? string.Empty;
            CoreResourceDigest = coreResourceDigest ?? string.Empty;
            DesignDigest = SpecialLandmarkCanonicalDigest.ComputeDesign(this);
            ShellDigest = SpecialLandmarkCanonicalDigest.ComputeShell(this);
            StateDigest = SpecialLandmarkCanonicalDigest.ComputeState(this);
            MarkerDigest = SpecialLandmarkCanonicalDigest.ComputeMarker(this);
            CanonicalDigest = SpecialLandmarkCanonicalDigest.Compute(this);
        }

        public SpecialRegionId RegionId { get; }
        public SpecialLandmarkKind Landmark { get; }
        public SpecialRegionKind RegionKind { get; }
        public SpecialLandmarkTheme Theme { get; }
        public SpecialLandmarkBindingKind Binding { get; }
        public SpecialLandmarkPlacementStatus PlacementStatus { get; }
        public int ReservedWidth { get; }
        public int ReservedHeight { get; }
        public LocalTileCoord DesignOrigin { get; }
        public int DesignWidth { get; }
        public int DesignHeight { get; }
        public int DesignChunkWidth { get; }
        public int DesignChunkHeight { get; }
        public IReadOnlyList<SpecialLandmarkDesignChunk> ActiveDesignChunks => activeDesignChunks;
        public IReadOnlyList<SpecialLandmarkShellNode> Nodes => nodes;
        public IReadOnlyList<SpecialLandmarkShellEdge> Edges => edges;
        public IReadOnlyList<SpecialLandmarkRouteDefinition> Routes => routes;
        public IReadOnlyList<SpecialLandmarkStateDefinition> States => states;
        public IReadOnlyList<SpecialLandmarkStateTransitionDefinition> Transitions => transitions;
        public IReadOnlyList<SpecialLandmarkResetDefinition> Resets => resets;
        public IReadOnlyList<SpecialLandmarkMarkerDefinition> Markers => markers;
        public IReadOnlyList<SpecialLandmarkForgeLedgerDefinition> ForgeLedgers => forgeLedgers;
        public SpecialLandmarkRewardDefinition RequiredReward { get; }
        public IReadOnlyList<SpecialLandmarkMerchantVariant> MerchantVariants => merchantVariants;
        public IReadOnlyList<SpecialLandmarkRouteWitness> Witnesses => witnesses;
        public bool IntroducesNewMovementRule { get; }
        public bool MandatoryProgressionDependency { get; }
        public string BridgeDigest { get; }
        public string EntryBufferDigest { get; }
        public string CollisionDigest { get; }
        public string FixedSlotLayerDigest { get; }
        public string RewardSafetyDigest { get; }
        public string CoreResourceDigest { get; }
        public string DesignDigest { get; }
        public string ShellDigest { get; }
        public string StateDigest { get; }
        public string MarkerDigest { get; }
        public string CanonicalDigest { get; }
        public int WorldOriginCount => Binding == SpecialLandmarkBindingKind.PlacedMandatorySite ? 1 : 0;
        public int ReservationClaimCount => Binding == SpecialLandmarkBindingKind.PlacedMandatorySite ? 1 : 0;
        public int BridgeClaimCount => Binding == SpecialLandmarkBindingKind.PlacedMandatorySite ? 1 : 0;
        public int PlacedOwnershipClaimCount => Binding == SpecialLandmarkBindingKind.PlacedMandatorySite ? 1 : 0;
        public int ForgePermanentLossCount => 0;
        public int DuplicateBenefitRiskCount => 0;
        public int MandatoryOptionalDependencyCount => 0;
        public int RngSelectionCount => 0;
        public int PathfindingCount => 0;
        public int CarveCount => 0;
        public int TeleportCount => 0;
        public int WorldMutationCount => 0;
        public int TileMutationCount => 0;
        public int InventoryMutationCount => 0;
        public int RewardGrantCount => 0;
        public int SaveWriteCount => 0;
        public int PlacementSolverCount => 0;
        public int GameplayExecutionCount => 0;

        private static ReadOnlyCollection<T> Copy<T>(IEnumerable<T> source)
            => new ReadOnlyCollection<T>((source ?? Array.Empty<T>()).ToArray());
    }

    public sealed class SpecialLandmarkCompileRequest
    {
        private readonly ReadOnlyCollection<CoreResourceRegionDefinition> coreResourceDefinitions;

        public SpecialLandmarkCompileRequest(
            SpecialLandmarkRegionDefinition definition,
            SpecialRegionSiteBridge bridge,
            string expectedBridgeDigest,
            SpecialRegionEntryBufferPlan entryBufferPlan,
            string expectedEntryBufferDigest,
            SpecialRegionPlacementCollisionPlan collisionPlan,
            string expectedCollisionDigest,
            SpecialRegionFixedSlotLayerPlan fixedSlotLayerPlan,
            string expectedFixedSlotLayerDigest,
            SpecialRegionRequiredResourceSafetyProof rewardSafetyProof,
            string expectedRewardSafetyDigest,
            IEnumerable<CoreResourceRegionDefinition> coreResourceDefinitions)
        {
            Definition = definition;
            Bridge = bridge;
            ExpectedBridgeDigest = expectedBridgeDigest ?? string.Empty;
            EntryBufferPlan = entryBufferPlan;
            ExpectedEntryBufferDigest = expectedEntryBufferDigest ?? string.Empty;
            CollisionPlan = collisionPlan;
            ExpectedCollisionDigest = expectedCollisionDigest ?? string.Empty;
            FixedSlotLayerPlan = fixedSlotLayerPlan;
            ExpectedFixedSlotLayerDigest = expectedFixedSlotLayerDigest ?? string.Empty;
            RewardSafetyProof = rewardSafetyProof;
            ExpectedRewardSafetyDigest = expectedRewardSafetyDigest ?? string.Empty;
            this.coreResourceDefinitions = new ReadOnlyCollection<CoreResourceRegionDefinition>(
                (coreResourceDefinitions ?? Array.Empty<CoreResourceRegionDefinition>())
                .Where(value => value != null).OrderBy(value => value.RegionId).ToArray());
        }

        public SpecialLandmarkRegionDefinition Definition { get; }
        public SpecialRegionSiteBridge Bridge { get; }
        public string ExpectedBridgeDigest { get; }
        public SpecialRegionEntryBufferPlan EntryBufferPlan { get; }
        public string ExpectedEntryBufferDigest { get; }
        public SpecialRegionPlacementCollisionPlan CollisionPlan { get; }
        public string ExpectedCollisionDigest { get; }
        public SpecialRegionFixedSlotLayerPlan FixedSlotLayerPlan { get; }
        public string ExpectedFixedSlotLayerDigest { get; }
        public SpecialRegionRequiredResourceSafetyProof RewardSafetyProof { get; }
        public string ExpectedRewardSafetyDigest { get; }
        public IReadOnlyList<CoreResourceRegionDefinition> CoreResourceDefinitions => coreResourceDefinitions;
    }

    public enum SpecialLandmarkErrorCode
    {
        MissingInput = 1,
        DigestMismatch = 2,
        RegionIdentityMismatch = 3,
        KindMismatch = 4,
        InvalidBindingMode = 5,
        UnsupportedFootprint = 6,
        InvalidDesignCanvas = 7,
        InvalidActiveChunk = 8,
        DuplicateNode = 9,
        InvalidEdge = 10,
        InvalidRoute = 11,
        MissingReturn = 12,
        UnrecoverableFailure = 13,
        InvalidState = 14,
        InvalidTransition = 15,
        InvalidResetPolicy = 16,
        ShellMutation = 17,
        ForgeProcessOrderMismatch = 18,
        ResourceLossRisk = 19,
        InvalidSealReward = 20,
        InvalidBossGate = 21,
        MissingFallRecovery = 22,
        NewMovementRuleIntroduced = 23,
        OptionalWorldBindingClaim = 24,
        MissingSafeZone = 25,
        MandatoryOptionalDependency = 26,
        MissingChoicePreview = 27,
        DuplicateBenefitRisk = 28,
        NonCanonicalPublication = 29,
    }

    public sealed class SpecialLandmarkError :
        IEquatable<SpecialLandmarkError>, IComparable<SpecialLandmarkError>
    {
        public SpecialLandmarkError(SpecialLandmarkErrorCode code, string ownerId, string message)
        {
            Code = code;
            OwnerId = ownerId ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public SpecialLandmarkErrorCode Code { get; }
        public string OwnerId { get; }
        public string Message { get; }
        public int CompareTo(SpecialLandmarkError other)
        {
            if (other == null) return 1;
            var value = Code.CompareTo(other.Code);
            if (value != 0) return value;
            value = string.Compare(OwnerId, other.OwnerId, StringComparison.Ordinal);
            return value != 0 ? value : string.Compare(Message, other.Message, StringComparison.Ordinal);
        }
        public bool Equals(SpecialLandmarkError other)
            => other != null && Code == other.Code && string.Equals(OwnerId, other.OwnerId, StringComparison.Ordinal)
               && string.Equals(Message, other.Message, StringComparison.Ordinal);
        public override bool Equals(object obj) => Equals(obj as SpecialLandmarkError);
        public override int GetHashCode() => ((int)Code * 397) ^ StringComparer.Ordinal.GetHashCode(OwnerId) ^
                                             StringComparer.Ordinal.GetHashCode(Message);
        public override string ToString() => Code + ":" + OwnerId + ":" + Message;
    }

    public sealed class SpecialLandmarkResult
    {
        internal SpecialLandmarkResult(
            SpecialLandmarkRegionPlan plan,
            IEnumerable<SpecialLandmarkError> errors)
        {
            Plan = plan;
            Errors = new ReadOnlyCollection<SpecialLandmarkError>(
                (errors ?? Array.Empty<SpecialLandmarkError>()).Distinct().OrderBy(value => value).ToArray());
            CanonicalDigest = plan == null ? string.Empty : plan.CanonicalDigest;
        }

        public bool Succeeded => Plan != null && Errors.Count == 0;
        public SpecialLandmarkRegionPlan Plan { get; }
        public IReadOnlyList<SpecialLandmarkError> Errors { get; }
        public string CanonicalDigest { get; }
    }

    public static class SpecialLandmarkCanonicalDigest
    {
        public static string ComputeDefinition(SpecialLandmarkRegionDefinition definition)
        {
            if (definition == null) return string.Empty;
            var value = new StringBuilder();
            AppendIdentity(value, definition.RegionId, definition.Landmark, definition.RegionKind,
                definition.Theme, definition.Binding, definition.ReservedWidth, definition.ReservedHeight,
                definition.DesignOrigin, definition.DesignWidth, definition.DesignHeight,
                definition.DesignChunkWidth, definition.DesignChunkHeight,
                definition.IntroducesNewMovementRule, definition.MandatoryProgressionDependency,
                definition.StateMutatesShell);
            AppendDesign(value, definition.ActiveDesignChunks);
            AppendShell(value, definition.Nodes, definition.Edges, definition.Routes);
            AppendState(value, definition.States, definition.Transitions, definition.Resets, definition.ForgeLedgers);
            AppendMarkers(value, definition.Markers, definition.RequiredReward, definition.MerchantVariants);
            return Sha256(value.ToString());
        }

        public static string Compute(SpecialLandmarkRegionPlan plan)
        {
            if (plan == null) return string.Empty;
            var value = new StringBuilder();
            Append(value, "region", plan.RegionId.Value);
            Append(value, "landmark", plan.Landmark.ToString());
            Append(value, "binding", plan.Binding.ToString());
            Append(value, "placement", plan.PlacementStatus.ToString());
            Append(value, "design", plan.DesignDigest);
            Append(value, "shell", plan.ShellDigest);
            Append(value, "state", plan.StateDigest);
            Append(value, "marker", plan.MarkerDigest);
            Append(value, "bridge", plan.BridgeDigest);
            Append(value, "entry", plan.EntryBufferDigest);
            Append(value, "collision", plan.CollisionDigest);
            Append(value, "layer", plan.FixedSlotLayerDigest);
            Append(value, "safety", plan.RewardSafetyDigest);
            Append(value, "resources", plan.CoreResourceDigest);
            foreach (var witness in plan.Witnesses)
                Append(value, "witness", witness.RouteId + ":" + witness.Kind + ":" + string.Join(",", witness.NodeIds));
            return Sha256(value.ToString());
        }

        public static string ComputeDesign(SpecialLandmarkRegionPlan plan)
        {
            var value = new StringBuilder();
            Append(value, "origin", plan.DesignOrigin.X + "," + plan.DesignOrigin.Y);
            Append(value, "size", plan.DesignWidth + "x" + plan.DesignHeight);
            Append(value, "chunk", plan.DesignChunkWidth + "x" + plan.DesignChunkHeight);
            AppendDesign(value, plan.ActiveDesignChunks);
            return Sha256(value.ToString());
        }

        public static string ComputeShell(SpecialLandmarkRegionPlan plan)
        {
            var value = new StringBuilder();
            AppendShell(value, plan.Nodes, plan.Edges, plan.Routes);
            return Sha256(value.ToString());
        }

        public static string ComputeState(SpecialLandmarkRegionPlan plan)
        {
            var value = new StringBuilder();
            AppendState(value, plan.States, plan.Transitions, plan.Resets, plan.ForgeLedgers);
            return Sha256(value.ToString());
        }

        public static string ComputeMarker(SpecialLandmarkRegionPlan plan)
        {
            var value = new StringBuilder();
            AppendMarkers(value, plan.Markers, plan.RequiredReward, plan.MerchantVariants);
            return Sha256(value.ToString());
        }

        private static void AppendIdentity(
            StringBuilder value,
            SpecialRegionId regionId,
            SpecialLandmarkKind landmark,
            SpecialRegionKind regionKind,
            SpecialLandmarkTheme theme,
            SpecialLandmarkBindingKind binding,
            int reservedWidth,
            int reservedHeight,
            LocalTileCoord origin,
            int designWidth,
            int designHeight,
            int chunkWidth,
            int chunkHeight,
            bool movement,
            bool dependency,
            bool shellMutation)
        {
            Append(value, "region", regionId.Value);
            Append(value, "landmark", landmark.ToString());
            Append(value, "kind", regionKind.ToString());
            Append(value, "theme", theme.ToString());
            Append(value, "binding", binding.ToString());
            Append(value, "reserved", reservedWidth + "x" + reservedHeight);
            Append(value, "origin", origin.X + "," + origin.Y);
            Append(value, "design", designWidth + "x" + designHeight);
            Append(value, "chunk", chunkWidth + "x" + chunkHeight);
            Append(value, "movement", Bool(movement));
            Append(value, "dependency", Bool(dependency));
            Append(value, "shellMutation", Bool(shellMutation));
        }

        private static void AppendDesign(StringBuilder value, IEnumerable<SpecialLandmarkDesignChunk> chunks)
        {
            foreach (var chunk in chunks.OrderBy(item => item))
                Append(value, "chunk", chunk.X + "," + chunk.Y);
        }

        private static void AppendShell(
            StringBuilder value,
            IEnumerable<SpecialLandmarkShellNode> nodes,
            IEnumerable<SpecialLandmarkShellEdge> edges,
            IEnumerable<SpecialLandmarkRouteDefinition> routes)
        {
            foreach (var node in nodes.OrderBy(item => item.NodeId, StringComparer.Ordinal))
                Append(value, "node", node.NodeId + ":" + node.Role + ":" + node.Coordinate.X + "," +
                                      node.Coordinate.Y + ":" + Bool(node.Required));
            foreach (var edge in edges.OrderBy(item => item.EdgeId, StringComparer.Ordinal))
                Append(value, "edge", edge.EdgeId + ":" + edge.FromNodeId + ":" + edge.ToNodeId + ":" +
                                      edge.RouteKind + ":" + Number(edge.Order) + ":" + edge.AccessClass + ":" +
                                      Bool(edge.Required) + ":" + edge.Dependency);
            foreach (var route in routes.OrderBy(item => item.RouteId, StringComparer.Ordinal))
                Append(value, "route", route.RouteId + ":" + route.Kind + ":" + route.StartNodeId + ":" +
                                       route.EndNodeId + ":" + string.Join(",", route.EdgeIds));
        }

        private static void AppendState(
            StringBuilder value,
            IEnumerable<SpecialLandmarkStateDefinition> states,
            IEnumerable<SpecialLandmarkStateTransitionDefinition> transitions,
            IEnumerable<SpecialLandmarkResetDefinition> resets,
            IEnumerable<SpecialLandmarkForgeLedgerDefinition> ledgers)
        {
            foreach (var state in states.OrderBy(item => item.StateId, StringComparer.Ordinal))
                Append(value, "state", state.StateId + ":" + state.Role + ":" + Bool(state.Persistent));
            foreach (var transition in transitions.OrderBy(item => item.TransitionId, StringComparer.Ordinal))
                Append(value, "transition", transition.TransitionId + ":" + transition.FromStateId + ":" +
                                            transition.ToStateId + ":" + transition.Trigger + ":" + Number(transition.Order));
            foreach (var reset in resets.OrderBy(item => item.ResetId, StringComparer.Ordinal))
                Append(value, "reset", reset.ResetId + ":" + reset.Policy + ":" + reset.FailureNodeId + ":" +
                                       reset.RecoveryNodeId + ":" + reset.FromStateId + ":" + reset.ToStateId + ":" +
                                       Bool(reset.ReturnsAllForgeInputs) + Bool(reset.PreservesSealAcceptance) +
                                       Bool(reset.PreventsReroll));
            foreach (var ledger in ledgers.OrderBy(item => item.Resource))
                Append(value, "ledger", ledger.Resource + ":" + ledger.AvailableStateId + ":" +
                                        ledger.ReservedStateId + ":" + ledger.ConsumedStateId + ":" +
                                        ledger.ReturnedStateId);
        }

        private static void AppendMarkers(
            StringBuilder value,
            IEnumerable<SpecialLandmarkMarkerDefinition> markers,
            SpecialLandmarkRewardDefinition reward,
            IEnumerable<SpecialLandmarkMerchantVariant> variants)
        {
            foreach (var marker in markers.OrderBy(item => item.MarkerId, StringComparer.Ordinal))
                Append(value, "marker", marker.MarkerId + ":" + marker.Kind + ":" + marker.NodeId + ":" +
                                      marker.StateId + ":" + Number(marker.Order) + ":" + Bool(marker.Required) + ":" +
                                      marker.Dependency + ":" + marker.PersistenceScope + ":" + marker.PersistenceKey.Value);
            if (reward != null)
                Append(value, "reward", reward.RewardId + ":" + reward.NodeId + ":" + reward.SlotId.Value + ":" +
                                      reward.PersistenceKey.Value + ":" + Number(reward.Amount) + ":" + Bool(reward.Required));
            foreach (var variant in variants.OrderBy(item => item)) Append(value, "variant", variant.ToString());
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Bool(bool value) => value ? "1" : "0";
        private static void Append(StringBuilder value, string name, string field)
            => value.Append(name).Append('=').Append(field ?? string.Empty).Append('\n');
        private static string Sha256(string material)
        {
            using (var algorithm = SHA256.Create())
            {
                var bytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(material ?? string.Empty));
                var result = new StringBuilder(bytes.Length * 2);
                foreach (var item in bytes) result.Append(item.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }
    }
}
