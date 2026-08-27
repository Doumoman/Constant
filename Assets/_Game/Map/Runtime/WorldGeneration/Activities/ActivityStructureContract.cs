using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.WorldGeneration.Activities
{
    public readonly struct ActivityStructureId : IEquatable<ActivityStructureId>, IComparable<ActivityStructureId>
    {
        private readonly string value;

        public ActivityStructureId(string value) { this.value = value; }
        public string Value => value ?? string.Empty;
        public int CompareTo(ActivityStructureId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
        public bool Equals(ActivityStructureId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ActivityStructureId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(ActivityStructureId left, ActivityStructureId right) => left.Equals(right);
        public static bool operator !=(ActivityStructureId left, ActivityStructureId right) => !left.Equals(right);
    }

    public readonly struct ActivitySlotId : IEquatable<ActivitySlotId>, IComparable<ActivitySlotId>
    {
        private readonly string value;

        public ActivitySlotId(string value) { this.value = value; }
        public string Value => value ?? string.Empty;
        public int CompareTo(ActivitySlotId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
        public bool Equals(ActivitySlotId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ActivitySlotId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(ActivitySlotId left, ActivitySlotId right) => left.Equals(right);
        public static bool operator !=(ActivitySlotId left, ActivitySlotId right) => !left.Equals(right);
    }

    public enum ActivitySlotKind
    {
        Cue = 1,
        Trigger = 2,
        Device = 3,
        Hazard = 4,
        Projectile = 5,
        Reward = 6,
        Recovery = 7,
        Reset = 8,
        Npc = 9,
    }

    public sealed class ActivitySlot
    {
        public ActivitySlot(ActivitySlotId id, ActivitySlotKind kind, LocalTileCoord tile, string markerId)
        {
            Id = id;
            Kind = kind;
            Tile = tile;
            MarkerId = markerId ?? string.Empty;
        }

        public ActivitySlotId Id { get; }
        public ActivitySlotKind Kind { get; }
        public LocalTileCoord Tile { get; }
        public string MarkerId { get; }
    }

    public enum ActivityCueKind
    {
        Visual = 1,
        Audio = 2,
        Environment = 3,
        Motion = 4,
    }

    public sealed class ActivityCue
    {
        public ActivityCue(ActivityCueKind kind, ActivitySlotId slotId, bool detectableBeforeActivation)
        {
            Kind = kind;
            SlotId = slotId;
            DetectableBeforeActivation = detectableBeforeActivation;
        }

        public ActivityCueKind Kind { get; }
        public ActivitySlotId SlotId { get; }
        public bool DetectableBeforeActivation { get; }
    }

    public enum MechanismNodeKind
    {
        CueEmitter = 1,
        Trigger = 2,
        Device = 3,
        Hazard = 4,
        ProjectileEmitter = 5,
        RewardEmitter = 6,
        RecoveryController = 7,
        ResetController = 8,
    }

    public enum MechanismRelationKind
    {
        Activates = 1,
        Drives = 2,
        Emits = 3,
        Enables = 4,
        Disables = 5,
        Resets = 6,
    }

    public sealed class MechanismNode
    {
        public MechanismNode(
            string nodeId,
            MechanismNodeKind kind,
            ActivitySlotId slotId,
            TraversalGraphKind graphKind = TraversalGraphKind.Mechanism)
        {
            NodeId = nodeId ?? string.Empty;
            Kind = kind;
            SlotId = slotId;
            GraphKind = graphKind;
        }

        public string NodeId { get; }
        public MechanismNodeKind Kind { get; }
        public ActivitySlotId SlotId { get; }
        public TraversalGraphKind GraphKind { get; }
    }

    public sealed class MechanismEdge
    {
        public MechanismEdge(
            string edgeId,
            string fromNodeId,
            string toNodeId,
            MechanismRelationKind relation,
            TraversalGraphKind graphKind = TraversalGraphKind.Mechanism)
        {
            EdgeId = edgeId ?? string.Empty;
            FromNodeId = fromNodeId ?? string.Empty;
            ToNodeId = toNodeId ?? string.Empty;
            Relation = relation;
            GraphKind = graphKind;
        }

        public string EdgeId { get; }
        public string FromNodeId { get; }
        public string ToNodeId { get; }
        public MechanismRelationKind Relation { get; }
        public TraversalGraphKind GraphKind { get; }
    }

    public sealed class MechanismGraph
    {
        private readonly ReadOnlyCollection<MechanismNode> nodes;
        private readonly ReadOnlyCollection<MechanismEdge> edges;

        public MechanismGraph(
            IEnumerable<MechanismNode> nodes,
            IEnumerable<MechanismEdge> edges,
            TraversalGraphKind graphKind = TraversalGraphKind.Mechanism)
        {
            GraphKind = graphKind;
            var nodeCopy = nodes == null ? Array.Empty<MechanismNode>() : nodes.ToArray();
            Array.Sort(nodeCopy, CompareNodes);
            this.nodes = new ReadOnlyCollection<MechanismNode>(nodeCopy);
            var edgeCopy = edges == null ? Array.Empty<MechanismEdge>() : edges.ToArray();
            Array.Sort(edgeCopy, CompareEdges);
            this.edges = new ReadOnlyCollection<MechanismEdge>(edgeCopy);
        }

        public TraversalGraphKind GraphKind { get; }
        public IReadOnlyList<MechanismNode> Nodes => nodes;
        public IReadOnlyList<MechanismEdge> Edges => edges;

        private static int CompareNodes(MechanismNode left, MechanismNode right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            return string.Compare(left.NodeId, right.NodeId, StringComparison.Ordinal);
        }

        private static int CompareEdges(MechanismEdge left, MechanismEdge right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            return string.Compare(left.EdgeId, right.EdgeId, StringComparison.Ordinal);
        }
    }

    public enum ProgressionPhaseKind
    {
        Cue = 1,
        Activation = 2,
        Core = 3,
        Reward = 4,
        Recovery = 5,
        Reset = 6,
        Exit = 7,
    }

    public enum ProgressionEdgeKind
    {
        Advance = 1,
        Failure = 2,
        Reset = 3,
        Exit = 4,
    }

    public sealed class ProgressionNode
    {
        public ProgressionNode(
            string nodeId,
            ProgressionPhaseKind phase,
            TraversalGraphKind graphKind = TraversalGraphKind.Progression)
        {
            NodeId = nodeId ?? string.Empty;
            Phase = phase;
            GraphKind = graphKind;
        }

        public string NodeId { get; }
        public ProgressionPhaseKind Phase { get; }
        public TraversalGraphKind GraphKind { get; }
    }

    public sealed class ProgressionEdge
    {
        public ProgressionEdge(
            string edgeId,
            string fromNodeId,
            string toNodeId,
            ProgressionEdgeKind kind,
            TraversalGraphKind graphKind = TraversalGraphKind.Progression)
        {
            EdgeId = edgeId ?? string.Empty;
            FromNodeId = fromNodeId ?? string.Empty;
            ToNodeId = toNodeId ?? string.Empty;
            Kind = kind;
            GraphKind = graphKind;
        }

        public string EdgeId { get; }
        public string FromNodeId { get; }
        public string ToNodeId { get; }
        public ProgressionEdgeKind Kind { get; }
        public TraversalGraphKind GraphKind { get; }
    }

    public sealed class ProgressionGraph
    {
        private readonly ReadOnlyCollection<ProgressionNode> nodes;
        private readonly ReadOnlyCollection<ProgressionEdge> edges;

        public ProgressionGraph(
            string startNodeId,
            string terminalNodeId,
            IEnumerable<ProgressionNode> nodes,
            IEnumerable<ProgressionEdge> edges,
            TraversalGraphKind graphKind = TraversalGraphKind.Progression)
        {
            StartNodeId = startNodeId ?? string.Empty;
            TerminalNodeId = terminalNodeId ?? string.Empty;
            GraphKind = graphKind;
            var nodeCopy = nodes == null ? Array.Empty<ProgressionNode>() : nodes.ToArray();
            Array.Sort(nodeCopy, CompareNodes);
            this.nodes = new ReadOnlyCollection<ProgressionNode>(nodeCopy);
            var edgeCopy = edges == null ? Array.Empty<ProgressionEdge>() : edges.ToArray();
            Array.Sort(edgeCopy, CompareEdges);
            this.edges = new ReadOnlyCollection<ProgressionEdge>(edgeCopy);
        }

        public string StartNodeId { get; }
        public string TerminalNodeId { get; }
        public TraversalGraphKind GraphKind { get; }
        public IReadOnlyList<ProgressionNode> Nodes => nodes;
        public IReadOnlyList<ProgressionEdge> Edges => edges;

        private static int CompareNodes(ProgressionNode left, ProgressionNode right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            return string.Compare(left.NodeId, right.NodeId, StringComparison.Ordinal);
        }

        private static int CompareEdges(ProgressionEdge left, ProgressionEdge right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            return string.Compare(left.EdgeId, right.EdgeId, StringComparison.Ordinal);
        }
    }

    public sealed class ActivityRemovalSafety
    {
        private readonly ReadOnlyCollection<LocalTileCoord> safePocketTiles;
        private readonly ReadOnlyCollection<LocalTileCoord> recoveryTiles;
        private readonly ReadOnlyCollection<LocalTileCoord> permanentSolidWriteTiles;

        public ActivityRemovalSafety(
            SpineVariantId baselineSpineVariantId,
            string entryTraversalNodeId,
            string exitTraversalNodeId,
            IEnumerable<LocalTileCoord> safePocketTiles,
            IEnumerable<LocalTileCoord> recoveryTiles,
            bool preserveStaticTraversal,
            bool preserveAccessClass,
            bool permanentSolidMutationAllowed,
            bool mandatoryExitDestructionAllowed,
            int routeTypeBeforeRemoval,
            int routeTypeAfterRemoval,
            AccessClass accessClassBeforeRemoval,
            AccessClass accessClassAfterRemoval,
            string traversalDigestBeforeRemoval,
            string traversalDigestAfterRemoval,
            IEnumerable<LocalTileCoord> permanentSolidWriteTiles = null)
        {
            BaselineSpineVariantId = baselineSpineVariantId;
            EntryTraversalNodeId = entryTraversalNodeId ?? string.Empty;
            ExitTraversalNodeId = exitTraversalNodeId ?? string.Empty;
            this.safePocketTiles = CopyCoordinates(safePocketTiles);
            this.recoveryTiles = CopyCoordinates(recoveryTiles);
            PreserveStaticTraversal = preserveStaticTraversal;
            PreserveAccessClass = preserveAccessClass;
            PermanentSolidMutationAllowed = permanentSolidMutationAllowed;
            MandatoryExitDestructionAllowed = mandatoryExitDestructionAllowed;
            RouteTypeBeforeRemoval = routeTypeBeforeRemoval;
            RouteTypeAfterRemoval = routeTypeAfterRemoval;
            AccessClassBeforeRemoval = accessClassBeforeRemoval;
            AccessClassAfterRemoval = accessClassAfterRemoval;
            TraversalDigestBeforeRemoval = traversalDigestBeforeRemoval ?? string.Empty;
            TraversalDigestAfterRemoval = traversalDigestAfterRemoval ?? string.Empty;
            this.permanentSolidWriteTiles = CopyCoordinates(permanentSolidWriteTiles);
        }

        public SpineVariantId BaselineSpineVariantId { get; }
        public string EntryTraversalNodeId { get; }
        public string ExitTraversalNodeId { get; }
        public IReadOnlyList<LocalTileCoord> SafePocketTiles => safePocketTiles;
        public IReadOnlyList<LocalTileCoord> RecoveryTiles => recoveryTiles;
        public bool PreserveStaticTraversal { get; }
        public bool PreserveAccessClass { get; }
        public bool PermanentSolidMutationAllowed { get; }
        public bool MandatoryExitDestructionAllowed { get; }
        public int RouteTypeBeforeRemoval { get; }
        public int RouteTypeAfterRemoval { get; }
        public AccessClass AccessClassBeforeRemoval { get; }
        public AccessClass AccessClassAfterRemoval { get; }
        public string TraversalDigestBeforeRemoval { get; }
        public string TraversalDigestAfterRemoval { get; }
        public IReadOnlyList<LocalTileCoord> PermanentSolidWriteTiles => permanentSolidWriteTiles;

        private static ReadOnlyCollection<LocalTileCoord> CopyCoordinates(IEnumerable<LocalTileCoord> source)
        {
            var copy = source == null ? Array.Empty<LocalTileCoord>() : source.ToArray();
            Array.Sort(copy, CompareCoordinates);
            return new ReadOnlyCollection<LocalTileCoord>(copy);
        }

        private static int CompareCoordinates(LocalTileCoord left, LocalTileCoord right)
        {
            var comparison = left.Y.CompareTo(right.Y);
            return comparison != 0 ? comparison : left.X.CompareTo(right.X);
        }
    }

    public sealed class ActivityStructureContract
    {
        private readonly ReadOnlyCollection<ActivitySlot> slots;
        private readonly ReadOnlyCollection<ActivityCue> cues;
        private readonly ReadOnlyCollection<PacingRole> compatiblePacingRoles;
        private readonly ReadOnlyCollection<AccessClass> compatibleAccessClasses;

        public ActivityStructureContract(
            ActivityStructureId id,
            TerrainClusterId terrainClusterId,
            SpineVariantId compatibleSpineVariantId,
            IEnumerable<PacingRole> compatiblePacingRoles,
            IEnumerable<AccessClass> compatibleAccessClasses,
            IEnumerable<ActivitySlot> slots,
            IEnumerable<ActivityCue> cues,
            MechanismGraph mechanismGraph,
            ProgressionGraph progressionGraph,
            ActivityRemovalSafety removalSafety,
            string displayText = null)
        {
            Id = id;
            TerrainClusterId = terrainClusterId;
            CompatibleSpineVariantId = compatibleSpineVariantId;
            this.compatiblePacingRoles = CopyEnums(compatiblePacingRoles);
            this.compatibleAccessClasses = CopyEnums(compatibleAccessClasses);
            var slotCopy = slots == null ? Array.Empty<ActivitySlot>() : slots.ToArray();
            Array.Sort(slotCopy, CompareSlots);
            this.slots = new ReadOnlyCollection<ActivitySlot>(slotCopy);
            var cueCopy = cues == null ? Array.Empty<ActivityCue>() : cues.ToArray();
            Array.Sort(cueCopy, CompareCues);
            this.cues = new ReadOnlyCollection<ActivityCue>(cueCopy);
            MechanismGraph = mechanismGraph;
            ProgressionGraph = progressionGraph;
            RemovalSafety = removalSafety;
            DisplayText = displayText ?? string.Empty;
        }

        public ActivityStructureId Id { get; }
        public TerrainClusterId TerrainClusterId { get; }
        public SpineVariantId CompatibleSpineVariantId { get; }
        public IReadOnlyList<PacingRole> CompatiblePacingRoles => compatiblePacingRoles;
        public IReadOnlyList<AccessClass> CompatibleAccessClasses => compatibleAccessClasses;
        public IReadOnlyList<ActivitySlot> Slots => slots;
        public IReadOnlyList<ActivityCue> Cues => cues;
        public MechanismGraph MechanismGraph { get; }
        public ProgressionGraph ProgressionGraph { get; }
        public ActivityRemovalSafety RemovalSafety { get; }
        public string DisplayText { get; }

        public string GetCanonicalDigest(TerrainClusterContract staticShell)
        {
            var result = ActivityContractValidator.Validate(this, staticShell);
            if (!result.IsValid)
                throw new InvalidOperationException("Cannot compute a published digest for an invalid ActivityStructure contract.");
            return result.CanonicalDigest;
        }

        private static ReadOnlyCollection<T> CopyEnums<T>(IEnumerable<T> source)
        {
            var copy = source == null ? Array.Empty<T>() : source.ToArray();
            Array.Sort(copy, (left, right) => Convert.ToInt32(left).CompareTo(Convert.ToInt32(right)));
            return new ReadOnlyCollection<T>(copy);
        }

        private static int CompareSlots(ActivitySlot left, ActivitySlot right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            return left.Id.CompareTo(right.Id);
        }

        private static int CompareCues(ActivityCue left, ActivityCue right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            var comparison = ((int)left.Kind).CompareTo((int)right.Kind);
            return comparison != 0 ? comparison : left.SlotId.CompareTo(right.SlotId);
        }
    }
}
