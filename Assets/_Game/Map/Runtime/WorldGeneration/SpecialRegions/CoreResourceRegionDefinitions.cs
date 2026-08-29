using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.Pipeline;

namespace StarNight.Map.WorldGeneration.SpecialRegions
{
    public enum CoreResourceKind
    {
        MoonCore = 1,
        CassiaSap = 2,
        StarNuruk = 3,
    }

    public enum CoreResourceRouteKind
    {
        Low = 1,
        High = 2,
        Recovery = 3,
    }

    public enum CoreResourceMechanismKind
    {
        ImpactChain = 1,
        WaterChannel = 2,
        FermentationPressure = 3,
    }

    public enum CoreResourceNodeRole
    {
        Entry = 1,
        EnvironmentTrigger = 2,
        MasteryTrigger = 3,
        Failure = 4,
        RecoveryJoin = 5,
        RequiredReward = 6,
        OptionalBenefit = 7,
        Return = 8,
    }

    public enum CoreResourceMarkerKind
    {
        None = 0,
        MoonBoulder = 1,
        Mortar = 2,
        ChainedImpact = 3,
        Vein = 4,
        EnemyCue = 5,
        SecretPocket = 6,
        DeviceReset = 7,
        RootChannel = 8,
        SapPipe = 9,
        MasteryWaterFlow = 10,
        BonusRoot = 11,
        Shortcut = 12,
        ManualReset = 13,
        Valve = 14,
        SafePlatform = 15,
        GasWarning = 16,
        PressureRelease = 17,
        BounceChain = 18,
        RecoveryRoom = 19,
    }

    public enum CoreResourceOptionalBenefitKind
    {
        MoonIron = 1,
        AuxiliaryBattery = 2,
        RecoveryPickup = 3,
        HiddenSeed = 4,
        Fuel = 5,
        RareFermentationItem = 6,
    }

    public enum CoreResourceDependencyKind
    {
        None = 0,
        Pickaxe = 1,
        Explosive = 2,
        WateringCan = 3,
        Village = 4,
        Inventory = 5,
    }

    public readonly struct CoreResourceDesignChunk :
        IEquatable<CoreResourceDesignChunk>, IComparable<CoreResourceDesignChunk>
    {
        public CoreResourceDesignChunk(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        public int CompareTo(CoreResourceDesignChunk other)
        {
            var value = Y.CompareTo(other.Y);
            return value != 0 ? value : X.CompareTo(other.X);
        }

        public bool Equals(CoreResourceDesignChunk other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is CoreResourceDesignChunk other && Equals(other);
        public override int GetHashCode() { unchecked { return (X * 397) ^ Y; } }
        public override string ToString() => X + "," + Y;
        public static bool operator ==(CoreResourceDesignChunk left, CoreResourceDesignChunk right) => left.Equals(right);
        public static bool operator !=(CoreResourceDesignChunk left, CoreResourceDesignChunk right) => !left.Equals(right);
    }

    public sealed class CoreResourceSolutionNode
    {
        public CoreResourceSolutionNode(
            string nodeId,
            CoreResourceNodeRole role,
            LocalTileCoord coordinate,
            CoreResourceMarkerKind markerKind = CoreResourceMarkerKind.None,
            int authoredOrder = 0,
            SpecialRegionSlotId rewardSlotId = default(SpecialRegionSlotId),
            bool requiredMarker = false)
        {
            NodeId = nodeId ?? string.Empty;
            Role = role;
            Coordinate = coordinate;
            MarkerKind = markerKind;
            AuthoredOrder = authoredOrder;
            RewardSlotId = rewardSlotId;
            RequiredMarker = requiredMarker;
        }

        public string NodeId { get; }
        public CoreResourceNodeRole Role { get; }
        public LocalTileCoord Coordinate { get; }
        public CoreResourceMarkerKind MarkerKind { get; }
        public int AuthoredOrder { get; }
        public SpecialRegionSlotId RewardSlotId { get; }
        public bool RequiredMarker { get; }
    }

    public sealed class CoreResourceSolutionEdge
    {
        public CoreResourceSolutionEdge(
            string edgeId,
            string fromNodeId,
            string toNodeId,
            int order,
            CoreResourceRouteKind routeKind,
            AccessClass accessClass,
            CoreResourceMechanismKind mechanism,
            bool required,
            CoreResourceDependencyKind dependency = CoreResourceDependencyKind.None)
        {
            EdgeId = edgeId ?? string.Empty;
            FromNodeId = fromNodeId ?? string.Empty;
            ToNodeId = toNodeId ?? string.Empty;
            Order = order;
            RouteKind = routeKind;
            AccessClass = accessClass;
            Mechanism = mechanism;
            Required = required;
            Dependency = dependency;
        }

        public string EdgeId { get; }
        public string FromNodeId { get; }
        public string ToNodeId { get; }
        public int Order { get; }
        public CoreResourceRouteKind RouteKind { get; }
        public AccessClass AccessClass { get; }
        public CoreResourceMechanismKind Mechanism { get; }
        public bool Required { get; }
        public CoreResourceDependencyKind Dependency { get; }
    }

    public sealed class CoreResourceRouteDefinition
    {
        private readonly ReadOnlyCollection<string> edgeIds;

        public CoreResourceRouteDefinition(
            string routeId,
            CoreResourceRouteKind kind,
            IEnumerable<string> edgeIds)
        {
            RouteId = routeId ?? string.Empty;
            Kind = kind;
            this.edgeIds = new ReadOnlyCollection<string>((edgeIds ?? Array.Empty<string>())
                .Select(value => value ?? string.Empty)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        public string RouteId { get; }
        public CoreResourceRouteKind Kind { get; }
        public IReadOnlyList<string> EdgeIds => edgeIds;
    }

    public sealed class CoreResourceRecoveryDefinition
    {
        public CoreResourceRecoveryDefinition(
            string recoveryId,
            string sourceMasteryNodeId,
            string failureNodeId,
            string failureEdgeId,
            string recoveryRouteId,
            string recoveryJoinNodeId)
        {
            RecoveryId = recoveryId ?? string.Empty;
            SourceMasteryNodeId = sourceMasteryNodeId ?? string.Empty;
            FailureNodeId = failureNodeId ?? string.Empty;
            FailureEdgeId = failureEdgeId ?? string.Empty;
            RecoveryRouteId = recoveryRouteId ?? string.Empty;
            RecoveryJoinNodeId = recoveryJoinNodeId ?? string.Empty;
        }

        public string RecoveryId { get; }
        public string SourceMasteryNodeId { get; }
        public string FailureNodeId { get; }
        public string FailureEdgeId { get; }
        public string RecoveryRouteId { get; }
        public string RecoveryJoinNodeId { get; }
    }

    public sealed class CoreResourceRewardDefinition
    {
        public CoreResourceRewardDefinition(
            string rewardId,
            string nodeId,
            CoreResourceKind resource,
            SpecialRegionSlotId slotId,
            SpecialPersistenceKey persistenceKey,
            SpecialPersistenceScope persistenceScope,
            int amount,
            bool required)
        {
            RewardId = rewardId ?? string.Empty;
            NodeId = nodeId ?? string.Empty;
            Resource = resource;
            SlotId = slotId;
            PersistenceKey = persistenceKey;
            PersistenceScope = persistenceScope;
            Amount = amount;
            Required = required;
        }

        public string RewardId { get; }
        public string NodeId { get; }
        public CoreResourceKind Resource { get; }
        public SpecialRegionSlotId SlotId { get; }
        public SpecialPersistenceKey PersistenceKey { get; }
        public SpecialPersistenceScope PersistenceScope { get; }
        public int Amount { get; }
        public bool Required { get; }
    }

    public sealed class CoreResourceOptionalBenefitDefinition
    {
        public CoreResourceOptionalBenefitDefinition(
            string benefitId,
            string nodeId,
            CoreResourceOptionalBenefitKind kind)
        {
            BenefitId = benefitId ?? string.Empty;
            NodeId = nodeId ?? string.Empty;
            Kind = kind;
        }

        public string BenefitId { get; }
        public string NodeId { get; }
        public CoreResourceOptionalBenefitKind Kind { get; }
        public bool Required => false;
        public bool OwnsPersistence => false;
    }

    public sealed class CoreResourceRegionDefinition
    {
        private readonly ReadOnlyCollection<CoreResourceDesignChunk> activeDesignChunks;
        private readonly ReadOnlyCollection<CoreResourceSolutionNode> nodes;
        private readonly ReadOnlyCollection<CoreResourceSolutionEdge> edges;
        private readonly ReadOnlyCollection<CoreResourceRouteDefinition> routes;
        private readonly ReadOnlyCollection<CoreResourceRecoveryDefinition> recoveries;
        private readonly ReadOnlyCollection<CoreResourceOptionalBenefitDefinition> optionalBenefits;

        public CoreResourceRegionDefinition(
            SpecialRegionId regionId,
            CoreResourceKind resource,
            MoonpalaceBiomeId biome,
            SpecialRegionKind regionKind,
            CoreResourceMechanismKind mechanism,
            int reservedWidth,
            int reservedHeight,
            LocalTileCoord designOrigin,
            int designWidth,
            int designHeight,
            int designChunkWidth,
            int designChunkHeight,
            IEnumerable<CoreResourceDesignChunk> activeDesignChunks,
            IEnumerable<CoreResourceSolutionNode> nodes,
            IEnumerable<CoreResourceSolutionEdge> edges,
            IEnumerable<CoreResourceRouteDefinition> routes,
            IEnumerable<CoreResourceRecoveryDefinition> recoveries,
            CoreResourceRewardDefinition requiredReward,
            IEnumerable<CoreResourceOptionalBenefitDefinition> optionalBenefits,
            string displayText = "")
        {
            RegionId = regionId;
            Resource = resource;
            Biome = biome;
            RegionKind = regionKind;
            Mechanism = mechanism;
            ReservedWidth = reservedWidth;
            ReservedHeight = reservedHeight;
            DesignOrigin = designOrigin;
            DesignWidth = designWidth;
            DesignHeight = designHeight;
            DesignChunkWidth = designChunkWidth;
            DesignChunkHeight = designChunkHeight;
            this.activeDesignChunks = Freeze(activeDesignChunks, (left, right) => left.CompareTo(right), out var nullChunks);
            this.nodes = Freeze(nodes, CompareNode, out var nullNodes);
            this.edges = Freeze(edges, CompareEdge, out var nullEdges);
            this.routes = Freeze(routes, CompareRoute, out var nullRoutes);
            this.recoveries = Freeze(recoveries, CompareRecovery, out var nullRecoveries);
            this.optionalBenefits = Freeze(optionalBenefits, CompareBenefit, out var nullBenefits);
            RequiredReward = requiredReward;
            DisplayText = displayText ?? string.Empty;
            SuppliedNullCount = nullChunks + nullNodes + nullEdges + nullRoutes + nullRecoveries + nullBenefits;
        }

        public SpecialRegionId RegionId { get; }
        public CoreResourceKind Resource { get; }
        public MoonpalaceBiomeId Biome { get; }
        public SpecialRegionKind RegionKind { get; }
        public CoreResourceMechanismKind Mechanism { get; }
        public int ReservedWidth { get; }
        public int ReservedHeight { get; }
        public LocalTileCoord DesignOrigin { get; }
        public int DesignWidth { get; }
        public int DesignHeight { get; }
        public int DesignChunkWidth { get; }
        public int DesignChunkHeight { get; }
        public int DesignGridWidth => DesignChunkWidth == 0 ? 0 : DesignWidth / DesignChunkWidth;
        public int DesignGridHeight => DesignChunkHeight == 0 ? 0 : DesignHeight / DesignChunkHeight;
        public IReadOnlyList<CoreResourceDesignChunk> ActiveDesignChunks => activeDesignChunks;
        public IReadOnlyList<CoreResourceSolutionNode> Nodes => nodes;
        public IReadOnlyList<CoreResourceSolutionEdge> Edges => edges;
        public IReadOnlyList<CoreResourceRouteDefinition> Routes => routes;
        public IReadOnlyList<CoreResourceRecoveryDefinition> Recoveries => recoveries;
        public CoreResourceRewardDefinition RequiredReward { get; }
        public IReadOnlyList<CoreResourceOptionalBenefitDefinition> OptionalBenefits => optionalBenefits;
        public string DisplayText { get; }
        internal int SuppliedNullCount { get; }

        private static ReadOnlyCollection<T> Freeze<T>(
            IEnumerable<T> source,
            Comparison<T> comparison,
            out int nullCount)
        {
            var supplied = source == null ? Array.Empty<T>() : source.ToArray();
            nullCount = supplied.Count(value => ReferenceEquals(value, null));
            var values = supplied.Where(value => !ReferenceEquals(value, null)).ToArray();
            Array.Sort(values, comparison);
            return new ReadOnlyCollection<T>(values);
        }

        private static int CompareNode(CoreResourceSolutionNode left, CoreResourceSolutionNode right)
            => string.Compare(left.NodeId, right.NodeId, StringComparison.Ordinal);
        private static int CompareEdge(CoreResourceSolutionEdge left, CoreResourceSolutionEdge right)
            => string.Compare(left.EdgeId, right.EdgeId, StringComparison.Ordinal);
        private static int CompareRoute(CoreResourceRouteDefinition left, CoreResourceRouteDefinition right)
        {
            var value = left.Kind.CompareTo(right.Kind);
            return value != 0 ? value : string.Compare(left.RouteId, right.RouteId, StringComparison.Ordinal);
        }
        private static int CompareRecovery(CoreResourceRecoveryDefinition left, CoreResourceRecoveryDefinition right)
            => string.Compare(left.RecoveryId, right.RecoveryId, StringComparison.Ordinal);
        private static int CompareBenefit(CoreResourceOptionalBenefitDefinition left, CoreResourceOptionalBenefitDefinition right)
            => string.Compare(left.BenefitId, right.BenefitId, StringComparison.Ordinal);
    }

    public sealed class CoreResourceRouteWitness
    {
        private readonly ReadOnlyCollection<string> nodeIds;

        internal CoreResourceRouteWitness(
            string routeId,
            CoreResourceRouteKind kind,
            IEnumerable<string> nodeIds)
        {
            RouteId = routeId ?? string.Empty;
            Kind = kind;
            this.nodeIds = new ReadOnlyCollection<string>((nodeIds ?? Array.Empty<string>()).ToArray());
        }

        public string RouteId { get; }
        public CoreResourceRouteKind Kind { get; }
        public IReadOnlyList<string> NodeIds => nodeIds;
    }

    public sealed class CoreResourceRegionPlan
    {
        private readonly ReadOnlyCollection<CoreResourceDesignChunk> activeDesignChunks;
        private readonly ReadOnlyCollection<CoreResourceSolutionNode> nodes;
        private readonly ReadOnlyCollection<CoreResourceSolutionEdge> edges;
        private readonly ReadOnlyCollection<CoreResourceRouteDefinition> routes;
        private readonly ReadOnlyCollection<CoreResourceRecoveryDefinition> recoveries;
        private readonly ReadOnlyCollection<CoreResourceOptionalBenefitDefinition> optionalBenefits;
        private readonly ReadOnlyCollection<CoreResourceRouteWitness> recoveryWitnesses;

        internal CoreResourceRegionPlan(
            CoreResourceRegionDefinition definition,
            CoreResourceRouteWitness lowWitness,
            CoreResourceRouteWitness highWitness,
            IEnumerable<CoreResourceRouteWitness> recoveryWitnesses,
            string bridgeDigest,
            string entryBufferDigest,
            string collisionDigest,
            string fixedSlotLayerDigest,
            string safetyProofDigest)
        {
            RegionId = definition.RegionId;
            Resource = definition.Resource;
            Biome = definition.Biome;
            RegionKind = definition.RegionKind;
            Mechanism = definition.Mechanism;
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
            recoveries = Copy(definition.Recoveries);
            optionalBenefits = Copy(definition.OptionalBenefits);
            RequiredReward = definition.RequiredReward;
            LowWitness = lowWitness;
            HighWitness = highWitness;
            this.recoveryWitnesses = new ReadOnlyCollection<CoreResourceRouteWitness>(
                (recoveryWitnesses ?? Array.Empty<CoreResourceRouteWitness>())
                .OrderBy(value => value.RouteId, StringComparer.Ordinal).ToArray());
            BridgeDigest = bridgeDigest ?? string.Empty;
            EntryBufferDigest = entryBufferDigest ?? string.Empty;
            CollisionDigest = collisionDigest ?? string.Empty;
            FixedSlotLayerDigest = fixedSlotLayerDigest ?? string.Empty;
            SafetyProofDigest = safetyProofDigest ?? string.Empty;
            DesignDigest = CoreResourceRegionCanonicalDigest.ComputeDesign(this);
            GraphDigest = CoreResourceRegionCanonicalDigest.ComputeGraph(this);
            RewardDigest = CoreResourceRegionCanonicalDigest.ComputeReward(this);
            CanonicalDigest = CoreResourceRegionCanonicalDigest.Compute(this);
        }

        public SpecialRegionId RegionId { get; }
        public CoreResourceKind Resource { get; }
        public MoonpalaceBiomeId Biome { get; }
        public SpecialRegionKind RegionKind { get; }
        public CoreResourceMechanismKind Mechanism { get; }
        public int ReservedWidth { get; }
        public int ReservedHeight { get; }
        public LocalTileCoord DesignOrigin { get; }
        public int DesignWidth { get; }
        public int DesignHeight { get; }
        public int DesignChunkWidth { get; }
        public int DesignChunkHeight { get; }
        public IReadOnlyList<CoreResourceDesignChunk> ActiveDesignChunks => activeDesignChunks;
        public IReadOnlyList<CoreResourceSolutionNode> Nodes => nodes;
        public IReadOnlyList<CoreResourceSolutionEdge> Edges => edges;
        public IReadOnlyList<CoreResourceRouteDefinition> Routes => routes;
        public IReadOnlyList<CoreResourceRecoveryDefinition> Recoveries => recoveries;
        public CoreResourceRewardDefinition RequiredReward { get; }
        public IReadOnlyList<CoreResourceOptionalBenefitDefinition> OptionalBenefits => optionalBenefits;
        public CoreResourceRouteWitness LowWitness { get; }
        public CoreResourceRouteWitness HighWitness { get; }
        public IReadOnlyList<CoreResourceRouteWitness> RecoveryWitnesses => recoveryWitnesses;
        public string BridgeDigest { get; }
        public string EntryBufferDigest { get; }
        public string CollisionDigest { get; }
        public string FixedSlotLayerDigest { get; }
        public string SafetyProofDigest { get; }
        public string DesignDigest { get; }
        public string GraphDigest { get; }
        public string RewardDigest { get; }
        public string CanonicalDigest { get; }
        public bool HasEntryTriggerRewardReturnWitness => LowWitness != null && HighWitness != null;
        public bool HasReverseStaticGraphWitness => HasEntryTriggerRewardReturnWitness;
        public bool HasFailureRecoveryWitness => recoveryWitnesses.Count == recoveries.Count;
        public int MandatoryToolDependencyCount => 0;
        public int PermanentLossCount => 0;
        public int DuplicateRewardRiskCount => 0;
        public int SyntheticEdgeCount => 0;
        public int TeleportCount => 0;
        public int CarveCount => 0;
        public int AutoSearchCount => 0;
        public int RngSelectionCount => 0;
        public int PathfindingCount => 0;
        public int WorldMutationCount => 0;
        public int TileMutationCount => 0;
        public int SceneMutationCount => 0;
        public int PrefabMutationCount => 0;
        public int InventoryMutationCount => 0;
        public int RewardGrantCount => 0;
        public int SaveWriteCount => 0;

        private static ReadOnlyCollection<T> Copy<T>(IEnumerable<T> source)
            => new ReadOnlyCollection<T>((source ?? Array.Empty<T>()).ToArray());
    }

    public sealed class CoreResourceRegionCompileRequest
    {
        public CoreResourceRegionCompileRequest(
            CoreResourceRegionDefinition definition,
            SpecialRegionSiteBridge bridge,
            string expectedBridgeDigest,
            SpecialRegionEntryBufferPlan entryBufferPlan,
            string expectedEntryBufferDigest,
            SpecialRegionPlacementCollisionPlan collisionPlan,
            string expectedCollisionDigest,
            SpecialRegionFixedSlotLayerPlan fixedSlotLayerPlan,
            string expectedFixedSlotLayerDigest,
            SpecialRegionRequiredResourceSafetyProof safetyProof,
            string expectedSafetyProofDigest)
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
            SafetyProof = safetyProof;
            ExpectedSafetyProofDigest = expectedSafetyProofDigest ?? string.Empty;
        }

        public CoreResourceRegionDefinition Definition { get; }
        public SpecialRegionSiteBridge Bridge { get; }
        public string ExpectedBridgeDigest { get; }
        public SpecialRegionEntryBufferPlan EntryBufferPlan { get; }
        public string ExpectedEntryBufferDigest { get; }
        public SpecialRegionPlacementCollisionPlan CollisionPlan { get; }
        public string ExpectedCollisionDigest { get; }
        public SpecialRegionFixedSlotLayerPlan FixedSlotLayerPlan { get; }
        public string ExpectedFixedSlotLayerDigest { get; }
        public SpecialRegionRequiredResourceSafetyProof SafetyProof { get; }
        public string ExpectedSafetyProofDigest { get; }
    }

    public enum CoreResourceRegionErrorCode
    {
        MissingInput = 1,
        DigestMismatch = 2,
        NotCoreResource = 3,
        RegionIdentityMismatch = 4,
        UnsupportedFootprint = 5,
        InvalidDesignCanvas = 6,
        InvalidActiveChunk = 7,
        DuplicateNode = 8,
        InvalidNodeCoordinate = 9,
        DuplicateEdge = 10,
        InvalidRoute = 11,
        MissingLowRoute = 12,
        MissingHighRoute = 13,
        MissingRecoveryRoute = 14,
        MissingEnvironmentSolution = 15,
        MandatoryToolDependency = 16,
        UnrecoverableFailure = 17,
        MissingRequiredReward = 18,
        RewardSlotMismatch = 19,
        PersistenceMismatch = 20,
        RequiredResourcePermanentlyLost = 21,
        DuplicateRewardRisk = 22,
        NonCanonicalPublication = 23,
    }

    public sealed class CoreResourceRegionError :
        IEquatable<CoreResourceRegionError>, IComparable<CoreResourceRegionError>
    {
        public CoreResourceRegionError(CoreResourceRegionErrorCode code, string path, string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public CoreResourceRegionErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(CoreResourceRegionError other)
        {
            if (other == null) return -1;
            var value = Code.CompareTo(other.Code);
            if (value != 0) return value;
            value = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return value != 0 ? value : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(CoreResourceRegionError other)
            => other != null && Code == other.Code &&
               string.Equals(Path, other.Path, StringComparison.Ordinal) &&
               string.Equals(Detail, other.Detail, StringComparison.Ordinal);
        public override bool Equals(object obj) => Equals(obj as CoreResourceRegionError);
        public override int GetHashCode()
        {
            unchecked
            {
                var value = (int)Code;
                value = (value * 397) ^ StringComparer.Ordinal.GetHashCode(Path);
                return (value * 397) ^ StringComparer.Ordinal.GetHashCode(Detail);
            }
        }
        public override string ToString() => Code + "|" + Path + "|" + Detail;
    }

    public sealed class CoreResourceRegionResult
    {
        private readonly ReadOnlyCollection<CoreResourceRegionError> errors;

        internal CoreResourceRegionResult(
            CoreResourceRegionPlan plan,
            IEnumerable<CoreResourceRegionError> errors)
        {
            var values = (errors ?? Array.Empty<CoreResourceRegionError>())
                .Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            this.errors = new ReadOnlyCollection<CoreResourceRegionError>(values);
            Plan = values.Length == 0 ? plan : null;
            CanonicalDigest = Plan == null ? string.Empty : Plan.CanonicalDigest;
        }

        public bool Succeeded => Plan != null && errors.Count == 0 && CanonicalDigest.Length != 0;
        public CoreResourceRegionPlan Plan { get; }
        public IReadOnlyList<CoreResourceRegionError> Errors => errors;
        public string CanonicalDigest { get; }
    }

    public static class CoreResourceRegionCanonicalDigest
    {
        public static string ComputeDefinition(CoreResourceRegionDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            var value = new StringBuilder();
            AppendIdentity(value, definition.RegionId, definition.Resource, definition.Biome,
                definition.RegionKind, definition.Mechanism, definition.ReservedWidth,
                definition.ReservedHeight, definition.DesignOrigin, definition.DesignWidth,
                definition.DesignHeight, definition.DesignChunkWidth, definition.DesignChunkHeight);
            foreach (var chunk in definition.ActiveDesignChunks)
                Append(value, "chunk", Number(chunk.X) + "/" + Number(chunk.Y));
            AppendGraph(value, definition.Nodes, definition.Edges, definition.Routes, definition.Recoveries);
            AppendReward(value, definition.RequiredReward, definition.OptionalBenefits);
            return Sha256(value.ToString());
        }

        public static string Compute(CoreResourceRegionPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var value = new StringBuilder();
            AppendIdentity(value, plan.RegionId, plan.Resource, plan.Biome, plan.RegionKind,
                plan.Mechanism, plan.ReservedWidth, plan.ReservedHeight, plan.DesignOrigin,
                plan.DesignWidth, plan.DesignHeight, plan.DesignChunkWidth, plan.DesignChunkHeight);
            Append(value, "design", plan.DesignDigest);
            Append(value, "graph", plan.GraphDigest);
            Append(value, "reward", plan.RewardDigest);
            Append(value, "bridge", plan.BridgeDigest);
            Append(value, "entry", plan.EntryBufferDigest);
            Append(value, "collision", plan.CollisionDigest);
            Append(value, "layer", plan.FixedSlotLayerDigest);
            Append(value, "safety", plan.SafetyProofDigest);
            AppendWitness(value, "lowWitness", plan.LowWitness);
            AppendWitness(value, "highWitness", plan.HighWitness);
            foreach (var witness in plan.RecoveryWitnesses)
                AppendWitness(value, "recoveryWitness", witness);
            return Sha256(value.ToString());
        }

        public static string ComputeDesign(CoreResourceRegionPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var value = new StringBuilder();
            Append(value, "origin", Number(plan.DesignOrigin.X) + "/" + Number(plan.DesignOrigin.Y));
            Append(value, "size", Number(plan.DesignWidth) + "/" + Number(plan.DesignHeight));
            Append(value, "chunkSize", Number(plan.DesignChunkWidth) + "/" + Number(plan.DesignChunkHeight));
            foreach (var chunk in plan.ActiveDesignChunks)
                Append(value, "chunk", Number(chunk.X) + "/" + Number(chunk.Y));
            return Sha256(value.ToString());
        }

        public static string ComputeGraph(CoreResourceRegionPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var value = new StringBuilder();
            AppendGraph(value, plan.Nodes, plan.Edges, plan.Routes, plan.Recoveries);
            return Sha256(value.ToString());
        }

        public static string ComputeReward(CoreResourceRegionPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var value = new StringBuilder();
            AppendReward(value, plan.RequiredReward, plan.OptionalBenefits);
            return Sha256(value.ToString());
        }

        private static void AppendIdentity(
            StringBuilder value,
            SpecialRegionId regionId,
            CoreResourceKind resource,
            MoonpalaceBiomeId biome,
            SpecialRegionKind regionKind,
            CoreResourceMechanismKind mechanism,
            int reservedWidth,
            int reservedHeight,
            LocalTileCoord origin,
            int designWidth,
            int designHeight,
            int chunkWidth,
            int chunkHeight)
        {
            Append(value, "region", regionId.Value);
            Append(value, "resource", Number((int)resource));
            Append(value, "biome", biome.IsDefined ? biome.CanonicalId : string.Empty);
            Append(value, "kind", Number((int)regionKind));
            Append(value, "mechanism", Number((int)mechanism));
            Append(value, "reserved", Number(reservedWidth) + "/" + Number(reservedHeight));
            Append(value, "origin", Number(origin.X) + "/" + Number(origin.Y));
            Append(value, "design", Number(designWidth) + "/" + Number(designHeight));
            Append(value, "chunk", Number(chunkWidth) + "/" + Number(chunkHeight));
        }

        private static void AppendGraph(
            StringBuilder value,
            IEnumerable<CoreResourceSolutionNode> nodes,
            IEnumerable<CoreResourceSolutionEdge> edges,
            IEnumerable<CoreResourceRouteDefinition> routes,
            IEnumerable<CoreResourceRecoveryDefinition> recoveries)
        {
            foreach (var node in nodes.OrderBy(item => item.NodeId, StringComparer.Ordinal))
                Append(value, "node", node.NodeId + "/" + Number((int)node.Role) + "/" +
                    Number(node.Coordinate.X) + "/" + Number(node.Coordinate.Y) + "/" +
                    Number((int)node.MarkerKind) + "/" + Number(node.AuthoredOrder) + "/" +
                    node.RewardSlotId.Value + "/" + Bool(node.RequiredMarker));
            foreach (var edge in edges.OrderBy(item => item.EdgeId, StringComparer.Ordinal))
                Append(value, "edge", edge.EdgeId + "/" + edge.FromNodeId + "/" + edge.ToNodeId + "/" +
                    Number(edge.Order) + "/" + Number((int)edge.RouteKind) + "/" +
                    Number((int)edge.AccessClass) + "/" + Number((int)edge.Mechanism) + "/" +
                    Bool(edge.Required) + "/" + Number((int)edge.Dependency));
            foreach (var route in routes.OrderBy(item => item.Kind).ThenBy(item => item.RouteId, StringComparer.Ordinal))
                Append(value, "route", route.RouteId + "/" + Number((int)route.Kind) + "/" +
                    string.Join(",", route.EdgeIds.OrderBy(item => item, StringComparer.Ordinal)));
            foreach (var recovery in recoveries.OrderBy(item => item.RecoveryId, StringComparer.Ordinal))
                Append(value, "recovery", recovery.RecoveryId + "/" + recovery.SourceMasteryNodeId + "/" +
                    recovery.FailureNodeId + "/" + recovery.FailureEdgeId + "/" +
                    recovery.RecoveryRouteId + "/" + recovery.RecoveryJoinNodeId);
        }

        private static void AppendReward(
            StringBuilder value,
            CoreResourceRewardDefinition reward,
            IEnumerable<CoreResourceOptionalBenefitDefinition> benefits)
        {
            if (reward != null)
                Append(value, "reward", reward.RewardId + "/" + reward.NodeId + "/" +
                    Number((int)reward.Resource) + "/" + reward.SlotId.Value + "/" +
                    reward.PersistenceKey.Value + "/" + Number((int)reward.PersistenceScope) + "/" +
                    Number(reward.Amount) + "/" + Bool(reward.Required));
            foreach (var benefit in (benefits ?? Array.Empty<CoreResourceOptionalBenefitDefinition>())
                         .OrderBy(item => item.BenefitId, StringComparer.Ordinal))
                Append(value, "benefit", benefit.BenefitId + "/" + benefit.NodeId + "/" +
                    Number((int)benefit.Kind));
        }

        private static void AppendWitness(StringBuilder value, string name, CoreResourceRouteWitness witness)
        {
            if (witness != null)
                Append(value, name, witness.RouteId + "/" + Number((int)witness.Kind) + "/" +
                    string.Join(",", witness.NodeIds));
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Bool(bool value) => value ? "1" : "0";
        private static void Append(StringBuilder value, string name, string field)
            => value.Append(name).Append('=').Append(field ?? string.Empty).Append('\n');

        private static string Sha256(string material)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(new UTF8Encoding(false).GetBytes(material))
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }
}
