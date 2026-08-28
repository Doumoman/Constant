using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Activities;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.WorldGeneration.EventOverlays
{
    public enum EventMarkerTargetSourceKind
    {
        TerrainCluster = 1,
        Activity = 2,
        SpecialRegion = 3,
    }

    public enum EventSpecialOverlapKind
    {
        None = 0,
        ReplaceableSlot = 1,
    }

    public sealed class EventMarkerTargetEvidence
    {
        public EventMarkerTargetEvidence(
            EventMarkerId markerId,
            EventMarkerTargetSourceKind sourceKind,
            string sourceOwnerId,
            LocalTileCoord sourceCoordinate,
            LocalTileCoord compiledCoordinate,
            string owningSlotKind,
            string underlyingCanvasValueBefore,
            string underlyingCanvasValueAfter,
            string staticShellDigestBefore,
            string staticShellDigestAfter,
            string protectionDigestBefore,
            string protectionDigestAfter,
            SpecialPersistenceKey persistenceKey,
            string persistenceDigestBefore,
            string persistenceDigestAfter,
            int geometryMutationCount = 0,
            int collisionMutationCount = 0,
            int routeMutationCount = 0,
            int accessMutationCount = 0,
            int pacingMutationCount = 0,
            int envelopeMutationCount = 0)
        {
            MarkerId = markerId;
            SourceKind = sourceKind;
            SourceOwnerId = sourceOwnerId ?? string.Empty;
            SourceCoordinate = sourceCoordinate;
            CompiledCoordinate = compiledCoordinate;
            OwningSlotKind = owningSlotKind ?? string.Empty;
            UnderlyingCanvasValueBefore = underlyingCanvasValueBefore ?? string.Empty;
            UnderlyingCanvasValueAfter = underlyingCanvasValueAfter ?? string.Empty;
            StaticShellDigestBefore = staticShellDigestBefore ?? string.Empty;
            StaticShellDigestAfter = staticShellDigestAfter ?? string.Empty;
            ProtectionDigestBefore = protectionDigestBefore ?? string.Empty;
            ProtectionDigestAfter = protectionDigestAfter ?? string.Empty;
            PersistenceKey = persistenceKey;
            PersistenceDigestBefore = persistenceDigestBefore ?? string.Empty;
            PersistenceDigestAfter = persistenceDigestAfter ?? string.Empty;
            GeometryMutationCount = geometryMutationCount;
            CollisionMutationCount = collisionMutationCount;
            RouteMutationCount = routeMutationCount;
            AccessMutationCount = accessMutationCount;
            PacingMutationCount = pacingMutationCount;
            EnvelopeMutationCount = envelopeMutationCount;
        }

        public EventMarkerId MarkerId { get; }
        public EventMarkerTargetSourceKind SourceKind { get; }
        public string SourceOwnerId { get; }
        public LocalTileCoord SourceCoordinate { get; }
        public LocalTileCoord CompiledCoordinate { get; }
        public string OwningSlotKind { get; }
        public string UnderlyingCanvasValueBefore { get; }
        public string UnderlyingCanvasValueAfter { get; }
        public string StaticShellDigestBefore { get; }
        public string StaticShellDigestAfter { get; }
        public string ProtectionDigestBefore { get; }
        public string ProtectionDigestAfter { get; }
        public SpecialPersistenceKey PersistenceKey { get; }
        public string PersistenceDigestBefore { get; }
        public string PersistenceDigestAfter { get; }
        public int GeometryMutationCount { get; }
        public int CollisionMutationCount { get; }
        public int RouteMutationCount { get; }
        public int AccessMutationCount { get; }
        public int PacingMutationCount { get; }
        public int EnvelopeMutationCount { get; }
        public bool HasNonMarkerMutation => GeometryMutationCount != 0 || CollisionMutationCount != 0 ||
            RouteMutationCount != 0 || AccessMutationCount != 0 || PacingMutationCount != 0 ||
            EnvelopeMutationCount != 0;
    }

    public sealed class EventOverlayAssignmentProfile
    {
        private readonly ReadOnlyCollection<MoonpalaceBiomeId> compatibleBiomes;
        private readonly ReadOnlyCollection<PacingRole> compatiblePacingRoles;
        private readonly ReadOnlyCollection<AccessClass> compatibleAccessClasses;

        public EventOverlayAssignmentProfile(
            EventOverlayContract contract,
            int weight,
            int minimumProgressionGap,
            IEnumerable<MoonpalaceBiomeId> compatibleBiomes,
            IEnumerable<PacingRole> compatiblePacingRoles,
            IEnumerable<AccessClass> compatibleAccessClasses,
            ActivityStructureId? referencedActivityId = null)
            : this(contract, contract == null ? string.Empty : EventOverlayCanonicalDigest.Compute(contract),
                weight, minimumProgressionGap, compatibleBiomes, compatiblePacingRoles,
                compatibleAccessClasses, referencedActivityId)
        {
        }

        public EventOverlayAssignmentProfile(
            EventOverlayContract contract,
            string contractDigest,
            int weight,
            int minimumProgressionGap,
            IEnumerable<MoonpalaceBiomeId> compatibleBiomes,
            IEnumerable<PacingRole> compatiblePacingRoles,
            IEnumerable<AccessClass> compatibleAccessClasses,
            ActivityStructureId? referencedActivityId = null)
        {
            Contract = contract;
            ContractDigest = contractDigest ?? string.Empty;
            Weight = weight;
            MinimumProgressionGap = minimumProgressionGap;
            this.compatibleBiomes = Freeze(compatibleBiomes, CompareBiome);
            this.compatiblePacingRoles = Freeze(compatiblePacingRoles, CompareEnum);
            this.compatibleAccessClasses = Freeze(compatibleAccessClasses, CompareEnum);
            ReferencedActivityId = referencedActivityId;
        }

        public EventOverlayContract Contract { get; }
        public string ContractDigest { get; }
        public int Weight { get; }
        public int MinimumProgressionGap { get; }
        public IReadOnlyList<MoonpalaceBiomeId> CompatibleBiomes => compatibleBiomes;
        public IReadOnlyList<PacingRole> CompatiblePacingRoles => compatiblePacingRoles;
        public IReadOnlyList<AccessClass> CompatibleAccessClasses => compatibleAccessClasses;
        public ActivityStructureId? ReferencedActivityId { get; }

        private static ReadOnlyCollection<T> Freeze<T>(IEnumerable<T> source, Comparison<T> comparison)
        {
            var copy = source == null ? Array.Empty<T>() : source.Distinct().ToArray();
            Array.Sort(copy, comparison);
            return new ReadOnlyCollection<T>(copy);
        }

        private static int CompareBiome(MoonpalaceBiomeId left, MoonpalaceBiomeId right)
            => string.Compare(left.CanonicalId, right.CanonicalId, StringComparison.Ordinal);

        private static int CompareEnum<T>(T left, T right)
            => Convert.ToInt32(left).CompareTo(Convert.ToInt32(right));
    }

    public sealed class EventOverlayOpportunity
    {
        private readonly ReadOnlyCollection<EventMarkerTargetEvidence> markers;

        public EventOverlayOpportunity(
            string opportunityId,
            SectorCoord sector,
            BiomePatchId patchId,
            int progressionOrdinal,
            MoonpalaceBiomeId biome,
            PacingRole pacingRole,
            AccessClass accessClass,
            TerrainClusterId terrainClusterId,
            ActivityStructureId? selectedActivityId,
            string activityFrequencyPlanDigest,
            IEnumerable<EventMarkerTargetEvidence> markers,
            EventSpecialOverlapKind specialOverlapKind = EventSpecialOverlapKind.None,
            SpecialRegionContract specialRegion = null,
            string specialRegionDigest = "",
            SpecialRegionSlotId specialRegionSlotId = default(SpecialRegionSlotId))
        {
            OpportunityId = opportunityId ?? string.Empty;
            Sector = sector;
            PatchId = patchId;
            ProgressionOrdinal = progressionOrdinal;
            Biome = biome;
            PacingRole = pacingRole;
            AccessClass = accessClass;
            TerrainClusterId = terrainClusterId;
            SelectedActivityId = selectedActivityId;
            ActivityFrequencyPlanDigest = activityFrequencyPlanDigest ?? string.Empty;
            var copy = markers == null ? Array.Empty<EventMarkerTargetEvidence>() : markers.ToArray();
            Array.Sort(copy, (left, right) => string.Compare(
                left == null ? string.Empty : left.MarkerId.Value,
                right == null ? string.Empty : right.MarkerId.Value, StringComparison.Ordinal));
            this.markers = new ReadOnlyCollection<EventMarkerTargetEvidence>(copy);
            SpecialOverlapKind = specialOverlapKind;
            SpecialRegion = specialRegion;
            SpecialRegionDigest = specialRegionDigest ?? string.Empty;
            SpecialRegionSlotId = specialRegionSlotId;
        }

        public string OpportunityId { get; }
        public SectorCoord Sector { get; }
        public BiomePatchId PatchId { get; }
        public int ProgressionOrdinal { get; }
        public MoonpalaceBiomeId Biome { get; }
        public PacingRole PacingRole { get; }
        public AccessClass AccessClass { get; }
        public TerrainClusterId TerrainClusterId { get; }
        public ActivityStructureId? SelectedActivityId { get; }
        public string ActivityFrequencyPlanDigest { get; }
        public IReadOnlyList<EventMarkerTargetEvidence> Markers => markers;
        public EventSpecialOverlapKind SpecialOverlapKind { get; }
        public SpecialRegionContract SpecialRegion { get; }
        public string SpecialRegionDigest { get; }
        public SpecialRegionSlotId SpecialRegionSlotId { get; }
    }

    public enum EventOverlayCompatibilityRejectionCode
    {
        BiomeMismatch,
        PacingRoleMismatch,
        AccessClassMismatch,
        TerrainClusterMismatch,
        ActivityMismatch,
        MissingMarker,
        DuplicateMarker,
        InvalidMarkerOperation,
        InvalidSpecialOverlap,
        FixedShellOverlap,
        PersistenceProvenanceMismatch,
    }

    public sealed class EventOverlayCompatibilityRejection
    {
        public EventOverlayCompatibilityRejection(
            string opportunityId,
            EventOverlayId eventId,
            EventOverlayCompatibilityRejectionCode code,
            string path,
            string detail)
        {
            OpportunityId = opportunityId ?? string.Empty;
            EventId = eventId;
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public string OpportunityId { get; }
        public EventOverlayId EventId { get; }
        public EventOverlayCompatibilityRejectionCode Code { get; }
        public string Path { get; }
        public string Detail { get; }
    }

    public sealed class EventOverlayCandidate
    {
        internal EventOverlayCandidate(EventOverlayOpportunity opportunity, EventOverlayAssignmentProfile profile, string candidateKey)
        {
            Opportunity = opportunity;
            Profile = profile;
            CandidateKey = candidateKey ?? string.Empty;
        }

        public EventOverlayOpportunity Opportunity { get; }
        public EventOverlayAssignmentProfile Profile { get; }
        public string CandidateKey { get; }
        public string OpportunityId => Opportunity.OpportunityId;
        public EventOverlayId EventId => Profile.Contract.Id;
        public EventOverlayKind Kind => Profile.Contract.Kind;
        public int Weight => Profile.Weight;
        public int MinimumProgressionGap => Profile.MinimumProgressionGap;
        public bool IsEmpty => Kind == EventOverlayKind.Empty;
    }

    public enum EventOverlayScopeKind
    {
        World = 0,
        BiomePatch = 1,
        Sector = 2,
    }

    public enum EventOverlayAssignmentDecisionKind
    {
        Assigned = 1,
        Empty = 2,
    }

    public sealed class EventOverlayAssignmentPolicy
    {
        public EventOverlayAssignmentPolicy(int targetPermille) { TargetPermille = targetPermille; }
        public int TargetPermille { get; }
    }

    public enum EventOverlayAssignmentErrorCode
    {
        MissingInput,
        InvalidProfile,
        InvalidOpportunity,
        IdentityMismatch,
        ArtifactDigestMismatch,
        MissingMarker,
        DuplicateMarker,
        InvalidMarkerOperation,
        NonMarkerMutation,
        InvalidSpecialOverlap,
        FixedShellOverlap,
        PersistenceProvenanceMismatch,
        MissingEmptyVariant,
        DuplicateEmptyVariant,
        InvalidFrequencyPolicy,
        InvalidCooldown,
        CooldownMakesTargetUnsatisfiable,
        InvalidRngBinding,
        BudgetMismatch,
        NonCanonicalPublication,
    }

    public sealed class EventOverlayAssignmentError
    {
        public EventOverlayAssignmentError(EventOverlayAssignmentErrorCode code, string path, string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }
        public EventOverlayAssignmentErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }
        public override string ToString() => Code + "|" + Path + "|" + Detail;
    }
}
