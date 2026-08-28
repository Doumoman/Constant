using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.WorldGeneration.Activities
{
    public enum ActivityShellZoneKind
    {
        Cue = 1,
        Core = 2,
        Reward = 3,
        Recovery = 4,
    }

    public enum ActivitySlotSemanticKind
    {
        CueMarker = 1,
        PressurePlateTrigger = 2,
        DeviceAnchor = 3,
        ProjectileEmitter = 4,
        ChaseOrHazardSpawn = 5,
        RewardAnchor = 6,
        RecoveryAnchor = 7,
        ResetAnchor = 8,
        NpcAnchor = 9,
    }

    public sealed class ActivityShellZoneDefinition
    {
        private readonly ReadOnlyCollection<LocalTileCoord> sourceCoordinates;

        public ActivityShellZoneDefinition(
            ActivityShellZoneKind kind,
            IEnumerable<LocalTileCoord> sourceCoordinates)
        {
            Kind = kind;
            this.sourceCoordinates = new ReadOnlyCollection<LocalTileCoord>(
                (sourceCoordinates ?? Array.Empty<LocalTileCoord>())
                    .OrderBy(value => value.Y)
                    .ThenBy(value => value.X)
                    .ToArray());
        }

        public ActivityShellZoneKind Kind { get; }
        public IReadOnlyList<LocalTileCoord> SourceCoordinates => sourceCoordinates;
    }

    public sealed class ActivitySlotProjectionIntent
    {
        public ActivitySlotProjectionIntent(
            ActivitySlotId slotId,
            ActivitySlotSemanticKind semantic)
        {
            SlotId = slotId;
            Semantic = semantic;
        }

        public ActivitySlotId SlotId { get; }
        public ActivitySlotSemanticKind Semantic { get; }
    }

    public sealed class ProjectedActivityShellCell
    {
        private readonly ReadOnlyCollection<ClusterTraversalProtectedTileProvenance> protectedProvenance;

        internal ProjectedActivityShellCell(
            ActivityShellZoneKind zoneKind,
            LocalTileCoord sourceCoordinate,
            LocalTileCoord compiledCoordinate,
            ClusterChunkCoord owningCompiledChunk,
            TerrainClusterShellOccupancy occupancy,
            TerrainClusterShellOccupancy staticShellOccupancy,
            bool isAbsoluteProtected,
            IEnumerable<ClusterTraversalProtectedTileProvenance> protectedProvenance)
        {
            ZoneKind = zoneKind;
            SourceCoordinate = sourceCoordinate;
            CompiledCoordinate = compiledCoordinate;
            OwningCompiledChunk = owningCompiledChunk;
            Occupancy = occupancy;
            StaticShellOccupancy = staticShellOccupancy;
            IsAbsoluteProtected = isAbsoluteProtected;
            this.protectedProvenance = new ReadOnlyCollection<ClusterTraversalProtectedTileProvenance>(
                (protectedProvenance ?? Array.Empty<ClusterTraversalProtectedTileProvenance>())
                    .Where(value => value != null)
                    .Distinct()
                    .OrderBy(value => value)
                    .ToArray());
        }

        public ActivityShellZoneKind ZoneKind { get; }
        public LocalTileCoord SourceCoordinate { get; }
        public LocalTileCoord CompiledCoordinate { get; }
        public ClusterChunkCoord OwningCompiledChunk { get; }
        public TerrainClusterShellOccupancy Occupancy { get; }
        public TerrainClusterShellOccupancy StaticShellOccupancy { get; }
        public bool IsAbsoluteProtected { get; }
        public IReadOnlyList<ClusterTraversalProtectedTileProvenance> ProtectedProvenance => protectedProvenance;
    }

    public sealed class ProjectedActivitySlot
    {
        private readonly ReadOnlyCollection<ClusterTraversalProtectedTileProvenance> protectedProvenance;

        internal ProjectedActivitySlot(
            ActivitySlotId slotId,
            ActivitySlotKind slotKind,
            ActivitySlotSemanticKind semantic,
            ActivityShellZoneKind requiredZone,
            string markerId,
            LocalTileCoord sourceCoordinate,
            LocalTileCoord compiledCoordinate,
            ClusterChunkCoord owningCompiledChunk,
            TerrainClusterShellOccupancy occupancy,
            bool isAbsoluteProtected,
            IEnumerable<ClusterTraversalProtectedTileProvenance> protectedProvenance)
        {
            SlotId = slotId;
            SlotKind = slotKind;
            Semantic = semantic;
            RequiredZone = requiredZone;
            MarkerId = markerId ?? string.Empty;
            SourceCoordinate = sourceCoordinate;
            CompiledCoordinate = compiledCoordinate;
            OwningCompiledChunk = owningCompiledChunk;
            Occupancy = occupancy;
            IsAbsoluteProtected = isAbsoluteProtected;
            this.protectedProvenance = new ReadOnlyCollection<ClusterTraversalProtectedTileProvenance>(
                (protectedProvenance ?? Array.Empty<ClusterTraversalProtectedTileProvenance>())
                    .Where(value => value != null)
                    .Distinct()
                    .OrderBy(value => value)
                    .ToArray());
        }

        public ActivitySlotId SlotId { get; }
        public ActivitySlotKind SlotKind { get; }
        public ActivitySlotSemanticKind Semantic { get; }
        public ActivityShellZoneKind RequiredZone { get; }
        public string MarkerId { get; }
        public LocalTileCoord SourceCoordinate { get; }
        public LocalTileCoord CompiledCoordinate { get; }
        public ClusterChunkCoord OwningCompiledChunk { get; }
        public TerrainClusterShellOccupancy Occupancy { get; }
        public bool IsAbsoluteProtected { get; }
        public IReadOnlyList<ClusterTraversalProtectedTileProvenance> ProtectedProvenance => protectedProvenance;
    }

    public sealed class ActivityCueSlotBinding
    {
        internal ActivityCueSlotBinding(
            ActivityCueKind cueKind,
            ActivitySlotId slotId,
            ProjectedActivitySlot projectedSlot)
        {
            CueKind = cueKind;
            SlotId = slotId;
            ProjectedSlot = projectedSlot;
        }

        public ActivityCueKind CueKind { get; }
        public ActivitySlotId SlotId { get; }
        public ProjectedActivitySlot ProjectedSlot { get; }
    }

    public sealed class ActivityMechanismSlotBinding
    {
        internal ActivityMechanismSlotBinding(
            string mechanismNodeId,
            MechanismNodeKind mechanismNodeKind,
            ActivitySlotId slotId,
            ProjectedActivitySlot projectedSlot)
        {
            MechanismNodeId = mechanismNodeId ?? string.Empty;
            MechanismNodeKind = mechanismNodeKind;
            SlotId = slotId;
            ProjectedSlot = projectedSlot;
        }

        public string MechanismNodeId { get; }
        public MechanismNodeKind MechanismNodeKind { get; }
        public ActivitySlotId SlotId { get; }
        public ProjectedActivitySlot ProjectedSlot { get; }
    }

    public sealed class ActivityProgressionShellBinding
    {
        internal ActivityProgressionShellBinding(
            string progressionNodeId,
            ProgressionPhaseKind phase,
            ActivityShellZoneKind? zoneKind,
            ActivitySlotId slotId,
            string traversalNodeId)
        {
            ProgressionNodeId = progressionNodeId ?? string.Empty;
            Phase = phase;
            ZoneKind = zoneKind;
            SlotId = slotId;
            TraversalNodeId = traversalNodeId ?? string.Empty;
        }

        public string ProgressionNodeId { get; }
        public ProgressionPhaseKind Phase { get; }
        public ActivityShellZoneKind? ZoneKind { get; }
        public ActivitySlotId SlotId { get; }
        public string TraversalNodeId { get; }
        public bool UsesTerrainClusterExitWitness => Phase == ProgressionPhaseKind.Exit;
    }

    public sealed class ActivityShellCanvas
    {
        private readonly ReadOnlyCollection<ActivityShellZoneDefinition> zones;
        private readonly ReadOnlyCollection<ProjectedActivityShellCell> zoneCells;
        private readonly ReadOnlyCollection<ProjectedActivitySlot> slots;
        private readonly ReadOnlyCollection<ActivityCueSlotBinding> cueBindings;
        private readonly ReadOnlyCollection<ActivityMechanismSlotBinding> mechanismBindings;
        private readonly ReadOnlyCollection<ActivityProgressionShellBinding> progressionBindings;
        private readonly ReadOnlyDictionary<ActivitySlotId, ProjectedActivitySlot> slotsById;

        internal ActivityShellCanvas(
            ActivityStructureId activityId,
            TerrainClusterId clusterId,
            SpineVariantId variantId,
            string activityDigest,
            string sourceContractDigest,
            string localCanvasDigest,
            string roleSocketContractDigest,
            string traversalCompilationDigest,
            string routeWitnessDigest,
            string patternRenderDigest,
            string workingCanvasDigest,
            IEnumerable<ActivityShellZoneDefinition> zones,
            IEnumerable<ProjectedActivityShellCell> zoneCells,
            IEnumerable<ProjectedActivitySlot> slots,
            IEnumerable<ActivityCueSlotBinding> cueBindings,
            IEnumerable<ActivityMechanismSlotBinding> mechanismBindings,
            IEnumerable<ActivityProgressionShellBinding> progressionBindings,
            string canonicalDigest)
        {
            ActivityId = activityId;
            ClusterId = clusterId;
            VariantId = variantId;
            ActivityDigest = activityDigest ?? string.Empty;
            SourceContractDigest = sourceContractDigest ?? string.Empty;
            LocalCanvasDigest = localCanvasDigest ?? string.Empty;
            RoleSocketContractDigest = roleSocketContractDigest ?? string.Empty;
            TraversalCompilationDigest = traversalCompilationDigest ?? string.Empty;
            RouteWitnessDigest = routeWitnessDigest ?? string.Empty;
            PatternRenderDigest = patternRenderDigest ?? string.Empty;
            WorkingCanvasDigest = workingCanvasDigest ?? string.Empty;
            CanonicalDigest = canonicalDigest ?? string.Empty;

            this.zones = new ReadOnlyCollection<ActivityShellZoneDefinition>(
                (zones ?? Array.Empty<ActivityShellZoneDefinition>())
                    .OrderBy(value => value.Kind).ToArray());
            this.zoneCells = new ReadOnlyCollection<ProjectedActivityShellCell>(
                (zoneCells ?? Array.Empty<ProjectedActivityShellCell>())
                    .OrderBy(value => value.ZoneKind)
                    .ThenBy(value => value.CompiledCoordinate.Y)
                    .ThenBy(value => value.CompiledCoordinate.X)
                    .ToArray());
            var slotCopy = (slots ?? Array.Empty<ProjectedActivitySlot>())
                .OrderBy(value => value.SlotId).ToArray();
            this.slots = new ReadOnlyCollection<ProjectedActivitySlot>(slotCopy);
            slotsById = new ReadOnlyDictionary<ActivitySlotId, ProjectedActivitySlot>(
                slotCopy.ToDictionary(value => value.SlotId));
            this.cueBindings = new ReadOnlyCollection<ActivityCueSlotBinding>(
                (cueBindings ?? Array.Empty<ActivityCueSlotBinding>())
                    .OrderBy(value => value.CueKind)
                    .ThenBy(value => value.SlotId)
                    .ToArray());
            this.mechanismBindings = new ReadOnlyCollection<ActivityMechanismSlotBinding>(
                (mechanismBindings ?? Array.Empty<ActivityMechanismSlotBinding>())
                    .OrderBy(value => value.MechanismNodeId, StringComparer.Ordinal).ToArray());
            this.progressionBindings = new ReadOnlyCollection<ActivityProgressionShellBinding>(
                (progressionBindings ?? Array.Empty<ActivityProgressionShellBinding>())
                    .OrderBy(value => value.ProgressionNodeId, StringComparer.Ordinal).ToArray());
        }

        public ActivityStructureId ActivityId { get; }
        public TerrainClusterId ClusterId { get; }
        public SpineVariantId VariantId { get; }
        public string ActivityDigest { get; }
        public string SourceContractDigest { get; }
        public string LocalCanvasDigest { get; }
        public string RoleSocketContractDigest { get; }
        public string TraversalCompilationDigest { get; }
        public string RouteWitnessDigest { get; }
        public string PatternRenderDigest { get; }
        public string WorkingCanvasDigest { get; }
        public IReadOnlyList<ActivityShellZoneDefinition> Zones => zones;
        public IReadOnlyList<ProjectedActivityShellCell> ZoneCells => zoneCells;
        public IReadOnlyList<ProjectedActivitySlot> Slots => slots;
        public IReadOnlyList<ActivityCueSlotBinding> CueBindings => cueBindings;
        public IReadOnlyList<ActivityMechanismSlotBinding> MechanismBindings => mechanismBindings;
        public IReadOnlyList<ActivityProgressionShellBinding> ProgressionBindings => progressionBindings;
        public int GeometryWriteCount => 0;
        public int GeometryChangeCount => 0;
        public int RendererInvocationCount => 0;
        public int RngDrawCount => 0;
        public string CanonicalDigest { get; }

        public bool TryGetSlot(ActivitySlotId slotId, out ProjectedActivitySlot slot)
        {
            return slotsById.TryGetValue(slotId, out slot);
        }

        public IReadOnlyList<ProjectedActivityShellCell> GetZoneCells(ActivityShellZoneKind kind)
        {
            return zoneCells.Where(value => value.ZoneKind == kind).ToArray();
        }
    }
}
