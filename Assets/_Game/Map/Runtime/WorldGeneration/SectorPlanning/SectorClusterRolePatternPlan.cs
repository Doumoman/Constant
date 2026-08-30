using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum SectorClusterRoleCellKind
    {
        ClusterEntry,
        ClusterExit,
        ClusterCore,
        RouteShoulder,
        BoundaryApproach,
        SpecialApproach,
        RecoverySupport,
        QuietBuffer,
        PatternFill,
        ProtectedOpen,
    }

    public enum SectorPatternZoneKind
    {
        ClusterBody,
        ClusterEdge,
        RouteShoulder,
        BoundaryBlend,
        SpecialApproach,
        Recovery,
        QuietBuffer,
        Detail,
        ProtectedNoWrite,
    }

    public enum SectorPatternRenderLayer
    {
        Geometry = 1,
        Surface = 2,
        Affordance = 3,
        Material = 4,
        Hazard = 5,
        Marker = 6,
    }

    public enum SectorPatternRenderErrorCode
    {
        MissingInput,
        MissingSpineEnvelopePlan,
        MissingClusterPlacementPlan,
        SectorMismatch,
        RoleCellOutOfBounds,
        DuplicateRoleCell,
        PatternZoneOutOfBounds,
        PatternZoneOverlap,
        PatternZoneOutsideCluster,
        PatternZoneTouchesUnplacedFootprint,
        MissingPatternCandidate,
        ProtectedWriteAttempt,
        RendererConflict,
        RenderTargetMismatch,
        MicroPatternApplicationRejected,
        MicroPatternRendererRejected,
        RouteAccessMutationClaim,
        AnchorMutationClaim,
        ClusterMutationClaim,
        SpineEnvelopeMutationClaim,
        ActivityMutationClaim,
        OwnershipMutationClaim,
        SolverMutationClaim,
        RngMutationClaim,
        TileMutationClaim,
        NonCanonicalPublication,
    }

    public sealed class SectorPatternRenderError :
        IEquatable<SectorPatternRenderError>, IComparable<SectorPatternRenderError>
    {
        public SectorPatternRenderError(
            SectorPatternRenderErrorCode code,
            string subject,
            string detail)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public SectorPatternRenderErrorCode Code { get; }
        public string Subject { get; }
        public string Detail { get; }

        public int CompareTo(SectorPatternRenderError other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(SectorPatternRenderError other) => other != null && CompareTo(other) == 0;
        public override bool Equals(object obj) => Equals(obj as SectorPatternRenderError);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(ToString());
        public override string ToString() => Code + "|" + Subject + "|" + Detail;
    }

    public sealed class SectorPatternTileRect :
        IEquatable<SectorPatternTileRect>, IComparable<SectorPatternTileRect>
    {
        public SectorPatternTileRect(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
        public int XMaxExclusive => X + Width;
        public int YMaxExclusive => Y + Height;

        public bool Contains(LocalTileCoord coordinate) =>
            coordinate.X >= X && coordinate.X < XMaxExclusive &&
            coordinate.Y >= Y && coordinate.Y < YMaxExclusive;

        public bool IsInside(int width, int height) =>
            Width > 0 && Height > 0 && X >= 0 && Y >= 0 &&
            XMaxExclusive <= width && YMaxExclusive <= height;

        public bool Overlaps(SectorPatternTileRect other) =>
            other != null && X < other.XMaxExclusive && XMaxExclusive > other.X &&
            Y < other.YMaxExclusive && YMaxExclusive > other.Y;

        public int CompareTo(SectorPatternTileRect other)
        {
            if (other == null) return -1;
            var comparison = Y.CompareTo(other.Y);
            if (comparison != 0) return comparison;
            comparison = X.CompareTo(other.X);
            if (comparison != 0) return comparison;
            comparison = Height.CompareTo(other.Height);
            return comparison != 0 ? comparison : Width.CompareTo(other.Width);
        }

        public bool Equals(SectorPatternTileRect other) => other != null && CompareTo(other) == 0;
        public override bool Equals(object obj) => Equals(obj as SectorPatternTileRect);
        public override int GetHashCode() => (((X * 397) ^ Y) * 397 ^ Width) * 397 ^ Height;
        public override string ToString() => X + "," + Y + "," + Width + "," + Height;
    }

    public sealed class SectorClusterRoleCell : IComparable<SectorClusterRoleCell>
    {
        internal SectorClusterRoleCell(
            SectorCoord sectorCoordinate,
            int sectorIndex,
            TerrainClusterId clusterId,
            SpineVariantId variantId,
            SectorClusterFootprintCell footprintCell,
            string biomeId,
            PacingRole pacingRole,
            SectorClusterRoleCellKind kind,
            string sourceNodeId,
            string sourceEdgeId,
            string sourceAnchorId,
            bool isProtected,
            int protectedTileCount)
        {
            SectorCoordinate = sectorCoordinate;
            SectorIndex = sectorIndex;
            ClusterId = clusterId;
            VariantId = variantId;
            FootprintCell = footprintCell;
            BiomeId = biomeId ?? string.Empty;
            PacingRole = pacingRole;
            Kind = kind;
            SourceNodeId = sourceNodeId ?? string.Empty;
            SourceEdgeId = sourceEdgeId ?? string.Empty;
            SourceAnchorId = sourceAnchorId ?? string.Empty;
            IsProtected = isProtected;
            ProtectedTileCount = protectedTileCount;
        }

        public SectorCoord SectorCoordinate { get; }
        public int SectorIndex { get; }
        public TerrainClusterId ClusterId { get; }
        public SpineVariantId VariantId { get; }
        public SectorClusterFootprintCell FootprintCell { get; }
        public string BiomeId { get; }
        public PacingRole PacingRole { get; }
        public SectorClusterRoleCellKind Kind { get; }
        public string SourceNodeId { get; }
        public string SourceEdgeId { get; }
        public string SourceAnchorId { get; }
        public bool IsProtected { get; }
        public int ProtectedTileCount { get; }

        public SectorPatternTileRect TileRect => new SectorPatternTileRect(
            FootprintCell.X * WorldGenConstants.MicroChunkWidthTiles,
            FootprintCell.Y * WorldGenConstants.MicroChunkHeightTiles,
            WorldGenConstants.MicroChunkWidthTiles,
            WorldGenConstants.MicroChunkHeightTiles);

        public int CompareTo(SectorClusterRoleCell other)
        {
            if (other == null) return -1;
            var comparison = SectorIndex.CompareTo(other.SectorIndex);
            if (comparison != 0) return comparison;
            comparison = FootprintCell.CompareTo(other.FootprintCell);
            if (comparison != 0) return comparison;
            comparison = ClusterId.CompareTo(other.ClusterId);
            return comparison != 0 ? comparison : VariantId.CompareTo(other.VariantId);
        }
    }

    public sealed class SectorPatternZone : IComparable<SectorPatternZone>
    {
        internal SectorPatternZone(
            string zoneId,
            SectorCoord sectorCoordinate,
            int sectorIndex,
            TerrainClusterId clusterId,
            SpineVariantId variantId,
            SectorClusterFootprintCell ownerCell,
            string biomeId,
            PacingRole pacingRole,
            SectorClusterRoleCellKind ownerRole,
            SectorPatternZoneKind kind,
            SectorPatternTileRect tileRect,
            int protectedTileCount,
            string sourceIdentity)
        {
            ZoneId = zoneId ?? string.Empty;
            SectorCoordinate = sectorCoordinate;
            SectorIndex = sectorIndex;
            ClusterId = clusterId;
            VariantId = variantId;
            OwnerCell = ownerCell;
            BiomeId = biomeId ?? string.Empty;
            PacingRole = pacingRole;
            OwnerRole = ownerRole;
            Kind = kind;
            TileRect = tileRect;
            ProtectedTileCount = protectedTileCount;
            SourceIdentity = sourceIdentity ?? string.Empty;
        }

        public string ZoneId { get; }
        public SectorCoord SectorCoordinate { get; }
        public int SectorIndex { get; }
        public TerrainClusterId ClusterId { get; }
        public SpineVariantId VariantId { get; }
        public SectorClusterFootprintCell OwnerCell { get; }
        public string BiomeId { get; }
        public PacingRole PacingRole { get; }
        public SectorClusterRoleCellKind OwnerRole { get; }
        public SectorPatternZoneKind Kind { get; }
        public SectorPatternTileRect TileRect { get; }
        public int ProtectedTileCount { get; }
        public string SourceIdentity { get; }

        public int CompareTo(SectorPatternZone other)
        {
            if (other == null) return -1;
            var comparison = SectorIndex.CompareTo(other.SectorIndex);
            return comparison != 0
                ? comparison
                : string.Compare(ZoneId, other.ZoneId, StringComparison.Ordinal);
        }
    }

    public sealed class SectorPatternProtectionEvidence :
        IEquatable<SectorPatternProtectionEvidence>, IComparable<SectorPatternProtectionEvidence>
    {
        internal SectorPatternProtectionEvidence(
            SectorCoord sectorCoordinate,
            int sectorIndex,
            LocalTileCoord coordinate,
            MicroPatternProtectedSourceKind sourceKind,
            string map10SourceId,
            string sourceIdentity)
        {
            SectorCoordinate = sectorCoordinate;
            SectorIndex = sectorIndex;
            Coordinate = coordinate;
            SourceKind = sourceKind;
            Map10SourceId = map10SourceId ?? string.Empty;
            SourceIdentity = sourceIdentity ?? string.Empty;
        }

        public SectorCoord SectorCoordinate { get; }
        public int SectorIndex { get; }
        public LocalTileCoord Coordinate { get; }
        public MicroPatternProtectedSourceKind SourceKind { get; }
        public string Map10SourceId { get; }
        public string SourceIdentity { get; }

        public int CompareTo(SectorPatternProtectionEvidence other)
        {
            if (other == null) return -1;
            var comparison = SectorIndex.CompareTo(other.SectorIndex);
            if (comparison != 0) return comparison;
            comparison = Coordinate.Y.CompareTo(other.Coordinate.Y);
            if (comparison != 0) return comparison;
            comparison = Coordinate.X.CompareTo(other.Coordinate.X);
            if (comparison != 0) return comparison;
            comparison = SourceKind.CompareTo(other.SourceKind);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Map10SourceId, other.Map10SourceId, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(SourceIdentity, other.SourceIdentity, StringComparison.Ordinal);
        }

        public bool Equals(SectorPatternProtectionEvidence other) => other != null && CompareTo(other) == 0;
        public override bool Equals(object obj) => Equals(obj as SectorPatternProtectionEvidence);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(ToString());
        public override string ToString() => SectorIndex + "|" + Coordinate.X + "," + Coordinate.Y +
                                             "|" + SourceKind + "|" + Map10SourceId + "|" + SourceIdentity;
    }

    public sealed class SectorPatternSourceProjection
    {
        private readonly ReadOnlyCollection<SectorPatternZoneKind> compatibleZoneKinds;
        private readonly ReadOnlyCollection<SectorClusterRoleCellKind> compatibleRoleKinds;
        private readonly ReadOnlyCollection<PacingRole> compatiblePacingRoles;

        public SectorPatternSourceProjection(
            MicroPatternDefinition definition,
            MicroPatternTransform transform,
            IEnumerable<SectorPatternZoneKind> sourceCompatibleZoneKinds,
            IEnumerable<SectorClusterRoleCellKind> sourceCompatibleRoleKinds,
            IEnumerable<PacingRole> sourceCompatiblePacingRoles,
            string repetitionSignature,
            int catalogOrder)
        {
            Definition = definition;
            Transform = transform;
            compatibleZoneKinds = Copy(sourceCompatibleZoneKinds);
            compatibleRoleKinds = Copy(sourceCompatibleRoleKinds);
            compatiblePacingRoles = Copy(sourceCompatiblePacingRoles);
            RepetitionSignature = repetitionSignature ?? string.Empty;
            CatalogOrder = catalogOrder;
        }

        public MicroPatternDefinition Definition { get; }
        public MicroPatternTransform Transform { get; }
        public IReadOnlyList<SectorPatternZoneKind> CompatibleZoneKinds => compatibleZoneKinds;
        public IReadOnlyList<SectorClusterRoleCellKind> CompatibleRoleKinds => compatibleRoleKinds;
        public IReadOnlyList<PacingRole> CompatiblePacingRoles => compatiblePacingRoles;
        public string RepetitionSignature { get; }
        public int CatalogOrder { get; }

        private static ReadOnlyCollection<T> Copy<T>(IEnumerable<T> values) =>
            new ReadOnlyCollection<T>((values ?? Array.Empty<T>()).Distinct().OrderBy(value => value).ToArray());
    }

    public sealed class SectorPatternSelection : IComparable<SectorPatternSelection>
    {
        internal SectorPatternSelection(
            string zoneId,
            int sectorIndex,
            MicroPatternId patternId,
            string sourcePatternDigest,
            MicroPatternTransform transform,
            string repetitionSignature,
            string applicationPlanDigest,
            string rendererRequestId)
        {
            ZoneId = zoneId ?? string.Empty;
            SectorIndex = sectorIndex;
            PatternId = patternId;
            SourcePatternDigest = sourcePatternDigest ?? string.Empty;
            Transform = transform;
            RepetitionSignature = repetitionSignature ?? string.Empty;
            ApplicationPlanDigest = applicationPlanDigest ?? string.Empty;
            RendererRequestId = rendererRequestId ?? string.Empty;
        }

        public string ZoneId { get; }
        public int SectorIndex { get; }
        public MicroPatternId PatternId { get; }
        public string SourcePatternDigest { get; }
        public MicroPatternTransform Transform { get; }
        public string RepetitionSignature { get; }
        public string ApplicationPlanDigest { get; }
        public string RendererRequestId { get; }

        public int CompareTo(SectorPatternSelection other)
        {
            if (other == null) return -1;
            var comparison = SectorIndex.CompareTo(other.SectorIndex);
            return comparison != 0
                ? comparison
                : string.Compare(ZoneId, other.ZoneId, StringComparison.Ordinal);
        }
    }

    public sealed class SectorPatternRenderCell : IComparable<SectorPatternRenderCell>
    {
        internal SectorPatternRenderCell(
            SectorCoord sectorCoordinate,
            int sectorIndex,
            LocalTileCoord coordinate,
            bool solid,
            string surfaceId,
            string affordanceId,
            string materialId,
            string hazardId,
            string markerId,
            bool changed,
            int appliedWriteCount,
            int idempotentWriteCount,
            int provenanceCount)
        {
            SectorCoordinate = sectorCoordinate;
            SectorIndex = sectorIndex;
            Coordinate = coordinate;
            Solid = solid;
            SurfaceId = surfaceId ?? string.Empty;
            AffordanceId = affordanceId ?? string.Empty;
            MaterialId = materialId ?? string.Empty;
            HazardId = hazardId ?? string.Empty;
            MarkerId = markerId ?? string.Empty;
            Changed = changed;
            AppliedWriteCount = appliedWriteCount;
            IdempotentWriteCount = idempotentWriteCount;
            ProvenanceCount = provenanceCount;
        }

        public SectorCoord SectorCoordinate { get; }
        public int SectorIndex { get; }
        public LocalTileCoord Coordinate { get; }
        public bool Solid { get; }
        public string SurfaceId { get; }
        public string AffordanceId { get; }
        public string MaterialId { get; }
        public string HazardId { get; }
        public string MarkerId { get; }
        public bool Changed { get; }
        public int AppliedWriteCount { get; }
        public int IdempotentWriteCount { get; }
        public int ProvenanceCount { get; }

        public string SemanticValue(SectorPatternRenderLayer layer)
        {
            switch (layer)
            {
                case SectorPatternRenderLayer.Geometry: return Solid ? "SOLID" : "AIR";
                case SectorPatternRenderLayer.Surface: return SurfaceId;
                case SectorPatternRenderLayer.Affordance: return AffordanceId;
                case SectorPatternRenderLayer.Material: return MaterialId;
                case SectorPatternRenderLayer.Hazard: return HazardId;
                case SectorPatternRenderLayer.Marker: return MarkerId;
                default: return string.Empty;
            }
        }

        public int CompareTo(SectorPatternRenderCell other)
        {
            if (other == null) return -1;
            var comparison = SectorIndex.CompareTo(other.SectorIndex);
            if (comparison != 0) return comparison;
            comparison = Coordinate.Y.CompareTo(other.Coordinate.Y);
            return comparison != 0 ? comparison : Coordinate.X.CompareTo(other.Coordinate.X);
        }
    }

    public sealed class SectorClusterRoleZoneBuildRequest
    {
        private readonly ReadOnlyCollection<SectorPacingAssignment> assignments;
        private readonly ReadOnlyCollection<SectorPatternRenderErrorCode> referenceFaults;

        public SectorClusterRoleZoneBuildRequest(
            SectorPlannerInput input,
            IEnumerable<SectorPacingAssignment> sourceAssignments,
            SectorFixedAnchorPlan anchorPlan,
            SectorClusterPlacementPlan clusterPlacementPlan,
            SectorSpineEnvelopePlan spineEnvelopePlan,
            string publicationLabel,
            string expectedCanonicalDigest = "",
            IEnumerable<SectorPatternRenderErrorCode> sourceReferenceFaults = null,
            bool routeAccessMutationClaim = false,
            bool anchorMutationClaim = false,
            bool clusterMutationClaim = false,
            bool spineEnvelopeMutationClaim = false,
            bool activityMutationClaim = false,
            bool ownershipMutationClaim = false,
            int solverInvocationCount = 0,
            int randomDrawCount = 0,
            int retryCount = 0,
            int tileWriteCount = 0,
            int sceneMutationCount = 0)
        {
            Input = input;
            assignments = new ReadOnlyCollection<SectorPacingAssignment>(
                (sourceAssignments ?? Array.Empty<SectorPacingAssignment>()).Where(value => value != null).ToArray());
            AnchorPlan = anchorPlan;
            ClusterPlacementPlan = clusterPlacementPlan;
            SpineEnvelopePlan = spineEnvelopePlan;
            PublicationLabel = publicationLabel ?? string.Empty;
            ExpectedCanonicalDigest = expectedCanonicalDigest ?? string.Empty;
            referenceFaults = new ReadOnlyCollection<SectorPatternRenderErrorCode>(
                (sourceReferenceFaults ?? Array.Empty<SectorPatternRenderErrorCode>()).Distinct().OrderBy(value => value).ToArray());
            RouteAccessMutationClaim = routeAccessMutationClaim;
            AnchorMutationClaim = anchorMutationClaim;
            ClusterMutationClaim = clusterMutationClaim;
            SpineEnvelopeMutationClaim = spineEnvelopeMutationClaim;
            ActivityMutationClaim = activityMutationClaim;
            OwnershipMutationClaim = ownershipMutationClaim;
            SolverInvocationCount = solverInvocationCount;
            RandomDrawCount = randomDrawCount;
            RetryCount = retryCount;
            TileWriteCount = tileWriteCount;
            SceneMutationCount = sceneMutationCount;
        }

        public SectorPlannerInput Input { get; }
        public IReadOnlyList<SectorPacingAssignment> Assignments => assignments;
        public SectorFixedAnchorPlan AnchorPlan { get; }
        public SectorClusterPlacementPlan ClusterPlacementPlan { get; }
        public SectorSpineEnvelopePlan SpineEnvelopePlan { get; }
        public string PublicationLabel { get; }
        public string ExpectedCanonicalDigest { get; }
        public IReadOnlyList<SectorPatternRenderErrorCode> ReferenceFaults => referenceFaults;
        public bool RouteAccessMutationClaim { get; }
        public bool AnchorMutationClaim { get; }
        public bool ClusterMutationClaim { get; }
        public bool SpineEnvelopeMutationClaim { get; }
        public bool ActivityMutationClaim { get; }
        public bool OwnershipMutationClaim { get; }
        public int SolverInvocationCount { get; }
        public int RandomDrawCount { get; }
        public int RetryCount { get; }
        public int TileWriteCount { get; }
        public int SceneMutationCount { get; }
    }

    public sealed class SectorPatternRenderRequest
    {
        private readonly ReadOnlyCollection<SectorPatternSourceProjection> patternSources;
        private readonly ReadOnlyCollection<SectorPatternRenderErrorCode> referenceFaults;

        public SectorPatternRenderRequest(
            SectorClusterRolePatternPlan roleZonePlan,
            IEnumerable<SectorPatternSourceProjection> sourcePatternSources,
            string publicationLabel,
            string expectedCanonicalDigest = "",
            IEnumerable<SectorPatternRenderErrorCode> sourceReferenceFaults = null,
            bool routeAccessMutationClaim = false,
            bool anchorMutationClaim = false,
            bool clusterMutationClaim = false,
            bool spineEnvelopeMutationClaim = false,
            bool activityMutationClaim = false,
            bool ownershipMutationClaim = false,
            int solverInvocationCount = 0,
            int randomDrawCount = 0,
            int retryCount = 0,
            int tileWriteCount = 0,
            int sceneMutationCount = 0)
        {
            RoleZonePlan = roleZonePlan;
            patternSources = new ReadOnlyCollection<SectorPatternSourceProjection>(
                (sourcePatternSources ?? Array.Empty<SectorPatternSourceProjection>()).Where(value => value != null).ToArray());
            PublicationLabel = publicationLabel ?? string.Empty;
            ExpectedCanonicalDigest = expectedCanonicalDigest ?? string.Empty;
            referenceFaults = new ReadOnlyCollection<SectorPatternRenderErrorCode>(
                (sourceReferenceFaults ?? Array.Empty<SectorPatternRenderErrorCode>()).Distinct().OrderBy(value => value).ToArray());
            RouteAccessMutationClaim = routeAccessMutationClaim;
            AnchorMutationClaim = anchorMutationClaim;
            ClusterMutationClaim = clusterMutationClaim;
            SpineEnvelopeMutationClaim = spineEnvelopeMutationClaim;
            ActivityMutationClaim = activityMutationClaim;
            OwnershipMutationClaim = ownershipMutationClaim;
            SolverInvocationCount = solverInvocationCount;
            RandomDrawCount = randomDrawCount;
            RetryCount = retryCount;
            TileWriteCount = tileWriteCount;
            SceneMutationCount = sceneMutationCount;
        }

        public SectorClusterRolePatternPlan RoleZonePlan { get; }
        public IReadOnlyList<SectorPatternSourceProjection> PatternSources => patternSources;
        public string PublicationLabel { get; }
        public string ExpectedCanonicalDigest { get; }
        public IReadOnlyList<SectorPatternRenderErrorCode> ReferenceFaults => referenceFaults;
        public bool RouteAccessMutationClaim { get; }
        public bool AnchorMutationClaim { get; }
        public bool ClusterMutationClaim { get; }
        public bool SpineEnvelopeMutationClaim { get; }
        public bool ActivityMutationClaim { get; }
        public bool OwnershipMutationClaim { get; }
        public int SolverInvocationCount { get; }
        public int RandomDrawCount { get; }
        public int RetryCount { get; }
        public int TileWriteCount { get; }
        public int SceneMutationCount { get; }
    }

    public sealed class SectorClusterRolePatternPlan
    {
        private readonly ReadOnlyCollection<SectorClusterRoleCell> roleCells;
        private readonly ReadOnlyCollection<SectorPatternZone> patternZones;
        private readonly ReadOnlyCollection<SectorPatternProtectionEvidence> protectionEvidence;
        private readonly ReadOnlyDictionary<SectorClusterRoleCellKind, int> roleCellCountByKind;
        private readonly ReadOnlyDictionary<SectorPatternZoneKind, int> patternZoneCountByKind;

        internal SectorClusterRolePatternPlan(
            string publicationLabel,
            IEnumerable<SectorClusterRoleCell> sourceRoleCells,
            IEnumerable<SectorPatternZone> sourcePatternZones,
            IEnumerable<SectorPatternProtectionEvidence> sourceProtectionEvidence,
            int sectorCount,
            int clusterPlacementCount,
            string plannerInputDigest,
            string pacingAssignmentDigest,
            string anchorPlanDigest,
            string clusterPlacementPlanDigest,
            string spineEnvelopePlanDigest,
            string routeAccessIdentity,
            string externalSocketIdentity,
            string boundaryIdentity,
            string specialIdentity,
            string clusterIdentity,
            string protectedOpenIdentity,
            string canonicalDigest)
        {
            PublicationLabel = publicationLabel ?? string.Empty;
            roleCells = new ReadOnlyCollection<SectorClusterRoleCell>(
                (sourceRoleCells ?? Array.Empty<SectorClusterRoleCell>()).OrderBy(value => value).ToArray());
            patternZones = new ReadOnlyCollection<SectorPatternZone>(
                (sourcePatternZones ?? Array.Empty<SectorPatternZone>()).OrderBy(value => value).ToArray());
            protectionEvidence = new ReadOnlyCollection<SectorPatternProtectionEvidence>(
                (sourceProtectionEvidence ?? Array.Empty<SectorPatternProtectionEvidence>()).Distinct().OrderBy(value => value).ToArray());
            roleCellCountByKind = Counts(roleCells, value => value.Kind,
                Enum.GetValues(typeof(SectorClusterRoleCellKind)).Cast<SectorClusterRoleCellKind>());
            patternZoneCountByKind = Counts(patternZones, value => value.Kind,
                Enum.GetValues(typeof(SectorPatternZoneKind)).Cast<SectorPatternZoneKind>());
            SectorCount = sectorCount;
            ClusterPlacementCount = clusterPlacementCount;
            PlannerInputDigestBefore = PlannerInputDigestAfter = plannerInputDigest ?? string.Empty;
            PacingAssignmentDigestBefore = PacingAssignmentDigestAfter = pacingAssignmentDigest ?? string.Empty;
            AnchorPlanDigestBefore = AnchorPlanDigestAfter = anchorPlanDigest ?? string.Empty;
            ClusterPlacementPlanDigestBefore = ClusterPlacementPlanDigestAfter = clusterPlacementPlanDigest ?? string.Empty;
            SpineEnvelopePlanDigestBefore = SpineEnvelopePlanDigestAfter = spineEnvelopePlanDigest ?? string.Empty;
            RouteAccessIdentityBefore = RouteAccessIdentityAfter = routeAccessIdentity ?? string.Empty;
            ExternalSocketIdentityBefore = ExternalSocketIdentityAfter = externalSocketIdentity ?? string.Empty;
            BoundaryIdentityBefore = BoundaryIdentityAfter = boundaryIdentity ?? string.Empty;
            SpecialIdentityBefore = SpecialIdentityAfter = specialIdentity ?? string.Empty;
            ClusterIdentityBefore = ClusterIdentityAfter = clusterIdentity ?? string.Empty;
            ProtectedOpenIdentityBefore = ProtectedOpenIdentityAfter = protectedOpenIdentity ?? string.Empty;
            CanonicalDigest = canonicalDigest ?? string.Empty;
        }

        public string PublicationLabel { get; }
        public IReadOnlyList<SectorClusterRoleCell> RoleCells => roleCells;
        public IReadOnlyList<SectorPatternZone> PatternZones => patternZones;
        public IReadOnlyList<SectorPatternProtectionEvidence> ProtectionEvidence => protectionEvidence;
        public IReadOnlyDictionary<SectorClusterRoleCellKind, int> RoleCellCountByKind => roleCellCountByKind;
        public IReadOnlyDictionary<SectorPatternZoneKind, int> PatternZoneCountByKind => patternZoneCountByKind;
        public int SectorCount { get; }
        public int ClusterPlacementCount { get; }
        public int RoleCellCount => roleCells.Count;
        public int PatternZoneCount => patternZones.Count;
        public int ProtectedRoleCellCount => roleCells.Count(value => value.IsProtected);
        public int ProtectedEvidenceCount => protectionEvidence.Count;
        public int PatternZoneOverlapCount => 0;
        public int OutOfClusterZoneCount => 0;
        public string PlannerInputDigestBefore { get; }
        public string PlannerInputDigestAfter { get; }
        public string PacingAssignmentDigestBefore { get; }
        public string PacingAssignmentDigestAfter { get; }
        public string AnchorPlanDigestBefore { get; }
        public string AnchorPlanDigestAfter { get; }
        public string ClusterPlacementPlanDigestBefore { get; }
        public string ClusterPlacementPlanDigestAfter { get; }
        public string SpineEnvelopePlanDigestBefore { get; }
        public string SpineEnvelopePlanDigestAfter { get; }
        public string RouteAccessIdentityBefore { get; }
        public string RouteAccessIdentityAfter { get; }
        public string ExternalSocketIdentityBefore { get; }
        public string ExternalSocketIdentityAfter { get; }
        public string BoundaryIdentityBefore { get; }
        public string BoundaryIdentityAfter { get; }
        public string SpecialIdentityBefore { get; }
        public string SpecialIdentityAfter { get; }
        public string ClusterIdentityBefore { get; }
        public string ClusterIdentityAfter { get; }
        public string ProtectedOpenIdentityBefore { get; }
        public string ProtectedOpenIdentityAfter { get; }
        public string CanonicalDigest { get; }
        public bool Map14_05RenderReady => SectorCount > 0 && RoleCellCount > 0 &&
                                             PatternZoneCount == RoleCellCount * 6 &&
                                             PatternZoneOverlapCount == 0 && OutOfClusterZoneCount == 0;
        public int RouteAccessMutationCount => 0;
        public int AnchorMutationCount => 0;
        public int ClusterMutationCount => 0;
        public int SpineEnvelopeMutationCount => 0;
        public int ActivityEventPlacementCount => 0;
        public int CanvasOwnershipWriteCount => 0;
        public int SolverInvocationCount => 0;
        public int RandomDrawCount => 0;
        public int RetryCount => 0;
        public int TileWriteCount => 0;
        public int SceneMutationCount => 0;

        public int Count(SectorClusterRoleCellKind kind) => roleCellCountByKind[kind];
        public int Count(SectorPatternZoneKind kind) => patternZoneCountByKind[kind];

        private static ReadOnlyDictionary<TKey, int> Counts<TValue, TKey>(
            IEnumerable<TValue> values,
            Func<TValue, TKey> selector,
            IEnumerable<TKey> keys)
        {
            var result = new SortedDictionary<TKey, int>();
            foreach (var key in keys) result[key] = 0;
            foreach (var value in values) result[selector(value)]++;
            return new ReadOnlyDictionary<TKey, int>(result);
        }
    }

    public sealed class SectorPatternRenderPlan
    {
        private readonly ReadOnlyCollection<SectorPatternSelection> selections;
        private readonly ReadOnlyCollection<SectorPatternRenderCell> renderCells;
        private readonly ReadOnlyCollection<string> applicationPlanDigests;
        private readonly ReadOnlyCollection<string> rendererDeltaDigests;
        private readonly ReadOnlyDictionary<SectorPatternRenderLayer, int> writeCountByLayer;

        internal SectorPatternRenderPlan(
            SectorClusterRolePatternPlan roleZonePlan,
            string publicationLabel,
            string sourcePatternCatalogDigest,
            IEnumerable<SectorPatternSelection> sourceSelections,
            IEnumerable<SectorPatternRenderCell> sourceRenderCells,
            IEnumerable<string> sourceApplicationPlanDigests,
            IEnumerable<string> sourceRendererDeltaDigests,
            IDictionary<SectorPatternRenderLayer, int> sourceWriteCountByLayer,
            int rendererInvocationCount,
            int protectedMaskHitCount,
            int protectedPreventedWriteCount,
            int protectedWriteCount,
            int rendererConflictCount,
            string canonicalDigest)
        {
            RoleZonePlan = roleZonePlan;
            PublicationLabel = publicationLabel ?? string.Empty;
            SourcePatternCatalogDigestBefore = SourcePatternCatalogDigestAfter = sourcePatternCatalogDigest ?? string.Empty;
            selections = new ReadOnlyCollection<SectorPatternSelection>(
                (sourceSelections ?? Array.Empty<SectorPatternSelection>()).OrderBy(value => value).ToArray());
            renderCells = new ReadOnlyCollection<SectorPatternRenderCell>(
                (sourceRenderCells ?? Array.Empty<SectorPatternRenderCell>()).OrderBy(value => value).ToArray());
            applicationPlanDigests = new ReadOnlyCollection<string>(
                (sourceApplicationPlanDigests ?? Array.Empty<string>()).OrderBy(value => value, StringComparer.Ordinal).ToArray());
            rendererDeltaDigests = new ReadOnlyCollection<string>(
                (sourceRendererDeltaDigests ?? Array.Empty<string>()).OrderBy(value => value, StringComparer.Ordinal).ToArray());
            var layerCounts = new SortedDictionary<SectorPatternRenderLayer, int>();
            foreach (SectorPatternRenderLayer layer in Enum.GetValues(typeof(SectorPatternRenderLayer)))
                layerCounts[layer] = 0;
            foreach (var pair in sourceWriteCountByLayer ?? new Dictionary<SectorPatternRenderLayer, int>())
                layerCounts[pair.Key] = pair.Value;
            writeCountByLayer = new ReadOnlyDictionary<SectorPatternRenderLayer, int>(layerCounts);
            RendererInvocationCount = rendererInvocationCount;
            ProtectedMaskHitCount = protectedMaskHitCount;
            ProtectedPreventedWriteCount = protectedPreventedWriteCount;
            ProtectedWriteCount = protectedWriteCount;
            RendererConflictCount = rendererConflictCount;
            CanonicalDigest = canonicalDigest ?? string.Empty;
        }

        public SectorClusterRolePatternPlan RoleZonePlan { get; }
        public string PublicationLabel { get; }
        public string RoleZonePlanDigest => RoleZonePlan.CanonicalDigest;
        public string SourcePatternCatalogDigestBefore { get; }
        public string SourcePatternCatalogDigestAfter { get; }
        public IReadOnlyList<SectorPatternSelection> Selections => selections;
        public IReadOnlyList<SectorPatternRenderCell> RenderCells => renderCells;
        public IReadOnlyList<string> ApplicationPlanDigests => applicationPlanDigests;
        public IReadOnlyList<string> RendererDeltaDigests => rendererDeltaDigests;
        public IReadOnlyDictionary<SectorPatternRenderLayer, int> WriteCountByLayer => writeCountByLayer;
        public int SectorCount => RoleZonePlan.SectorCount;
        public int ClusterPlacementCount => RoleZonePlan.ClusterPlacementCount;
        public int RoleCellCount => RoleZonePlan.RoleCellCount;
        public int PatternZoneCount => RoleZonePlan.PatternZoneCount;
        public int SelectedPatternCount => selections.Count;
        public int ApplicationPlanCount => applicationPlanDigests.Count;
        public int RendererInvocationCount { get; }
        public int RenderTargetCellCount => renderCells.Count;
        public int RenderedChangedCellCount => renderCells.Count(value => value.Changed);
        public int IdempotentNoChangeCellCount => renderCells.Count(value => !value.Changed);
        public int AppliedWriteCount => writeCountByLayer.Values.Sum();
        public int IdempotentWriteCount => renderCells.Sum(value => value.IdempotentWriteCount);
        public int ProtectedMaskHitCount { get; }
        public int ProtectedPreventedWriteCount { get; }
        public int ProtectedWriteCount { get; }
        public int RendererConflictCount { get; }
        public int PatternZoneOverlapCount => RoleZonePlan.PatternZoneOverlapCount;
        public int OutOfClusterZoneCount => RoleZonePlan.OutOfClusterZoneCount;
        public string Map10ApplicationPlannerType => typeof(MicroPatternApplicationPlanner).FullName;
        public string Map10OrderedRendererType => typeof(MicroPatternOrderedRenderer).FullName;
        public string Map10RenderRulesetVersion => MicroPatternRenderDelta.RulesetVersion;
        public string CanonicalDigest { get; }
        public bool Map14_06HandoffReady => RoleZonePlan.Map14_05RenderReady &&
                                             SelectedPatternCount == PatternZoneCount &&
                                             ApplicationPlanCount == SelectedPatternCount &&
                                             RendererInvocationCount == SectorCount &&
                                             ProtectedWriteCount == 0 && RendererConflictCount == 0;
        public string PlannerInputDigestBefore => RoleZonePlan.PlannerInputDigestBefore;
        public string PlannerInputDigestAfter => RoleZonePlan.PlannerInputDigestAfter;
        public string PacingAssignmentDigestBefore => RoleZonePlan.PacingAssignmentDigestBefore;
        public string PacingAssignmentDigestAfter => RoleZonePlan.PacingAssignmentDigestAfter;
        public string AnchorPlanDigestBefore => RoleZonePlan.AnchorPlanDigestBefore;
        public string AnchorPlanDigestAfter => RoleZonePlan.AnchorPlanDigestAfter;
        public string ClusterPlacementPlanDigestBefore => RoleZonePlan.ClusterPlacementPlanDigestBefore;
        public string ClusterPlacementPlanDigestAfter => RoleZonePlan.ClusterPlacementPlanDigestAfter;
        public string SpineEnvelopePlanDigestBefore => RoleZonePlan.SpineEnvelopePlanDigestBefore;
        public string SpineEnvelopePlanDigestAfter => RoleZonePlan.SpineEnvelopePlanDigestAfter;
        public string RouteAccessIdentityBefore => RoleZonePlan.RouteAccessIdentityBefore;
        public string RouteAccessIdentityAfter => RoleZonePlan.RouteAccessIdentityAfter;
        public string ExternalSocketIdentityBefore => RoleZonePlan.ExternalSocketIdentityBefore;
        public string ExternalSocketIdentityAfter => RoleZonePlan.ExternalSocketIdentityAfter;
        public string BoundaryIdentityBefore => RoleZonePlan.BoundaryIdentityBefore;
        public string BoundaryIdentityAfter => RoleZonePlan.BoundaryIdentityAfter;
        public string SpecialIdentityBefore => RoleZonePlan.SpecialIdentityBefore;
        public string SpecialIdentityAfter => RoleZonePlan.SpecialIdentityAfter;
        public string ClusterIdentityBefore => RoleZonePlan.ClusterIdentityBefore;
        public string ClusterIdentityAfter => RoleZonePlan.ClusterIdentityAfter;
        public string ProtectedOpenIdentityBefore => RoleZonePlan.ProtectedOpenIdentityBefore;
        public string ProtectedOpenIdentityAfter => RoleZonePlan.ProtectedOpenIdentityAfter;
        public int RouteAccessMutationCount => 0;
        public int AnchorMutationCount => 0;
        public int ClusterMutationCount => 0;
        public int SpineEnvelopeMutationCount => 0;
        public int ActivityEventPlacementCount => 0;
        public int CanvasOwnershipWriteCount => 0;
        public int SolverInvocationCount => 0;
        public int RandomDrawCount => 0;
        public int RetryCount => 0;
        public int TileWriteCount => 0;
        public int SceneMutationCount => 0;
        public int PrefabMutationCount => 0;
        public int GameObjectMutationCount => 0;
        public int AssetWriteCount => 0;

        public int Count(SectorPatternRenderLayer layer) => writeCountByLayer[layer];
    }

    public sealed class SectorClusterRoleZoneBuildResult
    {
        private readonly ReadOnlyCollection<SectorPatternRenderError> errors;

        internal SectorClusterRoleZoneBuildResult(
            SectorClusterRolePatternPlan plan,
            IEnumerable<SectorPatternRenderError> sourceErrors)
        {
            var ordered = (sourceErrors ?? Array.Empty<SectorPatternRenderError>())
                .Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            errors = new ReadOnlyCollection<SectorPatternRenderError>(ordered);
            Plan = ordered.Length == 0 ? plan : null;
            CanonicalDigest = Plan == null ? string.Empty : Plan.CanonicalDigest;
        }

        public bool Success => Plan != null && errors.Count == 0;
        public SectorClusterRolePatternPlan Plan { get; }
        public string CanonicalDigest { get; }
        public IReadOnlyList<SectorPatternRenderError> Errors => errors;
    }

    public sealed class SectorPatternRenderBuildResult
    {
        private readonly ReadOnlyCollection<SectorPatternRenderError> errors;

        internal SectorPatternRenderBuildResult(
            SectorPatternRenderPlan plan,
            IEnumerable<SectorPatternRenderError> sourceErrors)
        {
            var ordered = (sourceErrors ?? Array.Empty<SectorPatternRenderError>())
                .Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            errors = new ReadOnlyCollection<SectorPatternRenderError>(ordered);
            Plan = ordered.Length == 0 ? plan : null;
            CanonicalDigest = Plan == null ? string.Empty : Plan.CanonicalDigest;
        }

        public bool Success => Plan != null && errors.Count == 0;
        public SectorPatternRenderPlan Plan { get; }
        public string CanonicalDigest { get; }
        public IReadOnlyList<SectorPatternRenderError> Errors => errors;
    }

    public static class SectorPatternRenderCanonicalDigest
    {
        public static string ComputeRoleZone(SectorClusterRolePatternPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var material = new StringBuilder();
            Append(material, "PUBLICATION", plan.PublicationLabel);
            Append(material, "IDENTITIES", plan.PlannerInputDigestBefore, plan.PacingAssignmentDigestBefore,
                plan.AnchorPlanDigestBefore, plan.ClusterPlacementPlanDigestBefore,
                plan.SpineEnvelopePlanDigestBefore, plan.RouteAccessIdentityBefore,
                plan.ExternalSocketIdentityBefore, plan.BoundaryIdentityBefore,
                plan.SpecialIdentityBefore, plan.ClusterIdentityBefore, plan.ProtectedOpenIdentityBefore);
            foreach (var cell in plan.RoleCells)
                Append(material, "ROLE", cell.SectorIndex, cell.ClusterId.Value, cell.VariantId.Value,
                    cell.FootprintCell.X, cell.FootprintCell.Y, cell.BiomeId, cell.PacingRole, cell.Kind,
                    cell.SourceNodeId, cell.SourceEdgeId, cell.SourceAnchorId,
                    cell.IsProtected, cell.ProtectedTileCount);
            foreach (var zone in plan.PatternZones)
                Append(material, "ZONE", zone.ZoneId, zone.SectorIndex, zone.ClusterId.Value,
                    zone.VariantId.Value, zone.OwnerCell.X, zone.OwnerCell.Y, zone.BiomeId,
                    zone.PacingRole, zone.OwnerRole, zone.Kind, zone.TileRect,
                    zone.ProtectedTileCount, zone.SourceIdentity);
            foreach (var evidence in plan.ProtectionEvidence)
                Append(material, "PROTECTED", evidence.SectorIndex, evidence.Coordinate.X,
                    evidence.Coordinate.Y, evidence.SourceKind, evidence.Map10SourceId,
                    evidence.SourceIdentity);
            return Hash(material.ToString());
        }

        public static string ComputeRender(SectorPatternRenderPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var material = new StringBuilder();
            Append(material, "PUBLICATION", plan.PublicationLabel, plan.RoleZonePlanDigest,
                plan.SourcePatternCatalogDigestBefore, plan.Map10ApplicationPlannerType,
                plan.Map10OrderedRendererType, plan.Map10RenderRulesetVersion,
                plan.RendererInvocationCount, plan.ProtectedMaskHitCount,
                plan.ProtectedPreventedWriteCount, plan.ProtectedWriteCount,
                plan.RendererConflictCount);
            foreach (var selection in plan.Selections)
                Append(material, "SELECTION", selection.ZoneId, selection.SectorIndex,
                    selection.PatternId.Value, selection.SourcePatternDigest, selection.Transform,
                    selection.RepetitionSignature, selection.ApplicationPlanDigest,
                    selection.RendererRequestId);
            foreach (var digest in plan.RendererDeltaDigests) Append(material, "DELTA", digest);
            foreach (var cell in plan.RenderCells)
                Append(material, "CELL", cell.SectorIndex, cell.Coordinate.X, cell.Coordinate.Y,
                    cell.Solid, cell.SurfaceId, cell.AffordanceId, cell.MaterialId,
                    cell.HazardId, cell.MarkerId, cell.Changed, cell.AppliedWriteCount,
                    cell.IdempotentWriteCount, cell.ProvenanceCount);
            foreach (var pair in plan.WriteCountByLayer.OrderBy(value => value.Key))
                Append(material, "LAYER", pair.Key, pair.Value);
            return Hash(material.ToString());
        }

        public static string ComputePatternCatalog(IEnumerable<SectorPatternSourceProjection> sources)
        {
            var material = new StringBuilder();
            foreach (var source in (sources ?? Array.Empty<SectorPatternSourceProjection>())
                         .Where(value => value != null)
                         .OrderBy(value => value.CatalogOrder)
                         .ThenBy(value => value.Definition == null ? string.Empty : value.Definition.Id.Value,
                             StringComparer.Ordinal)
                         .ThenBy(value => value.Transform))
            {
                Append(material, "PATTERN",
                    source.Definition == null ? string.Empty : source.Definition.Id.Value,
                    source.Definition == null ? string.Empty : source.Definition.ComputeStableDigest(),
                    source.Transform, source.RepetitionSignature, source.CatalogOrder,
                    string.Join(",", source.CompatibleZoneKinds),
                    string.Join(",", source.CompatibleRoleKinds),
                    string.Join(",", source.CompatiblePacingRoles));
            }
            return Hash(material.ToString());
        }

        public static string ComputeProtectedOpenIdentity(
            IEnumerable<SectorTraversalEnvelopeCell> cells)
        {
            var material = new StringBuilder();
            foreach (var cell in (cells ?? Array.Empty<SectorTraversalEnvelopeCell>()).OrderBy(value => value))
                Append(material, cell.SectorIndex, cell.Coordinate.X, cell.Coordinate.Y,
                    cell.Kind, cell.EdgeId, cell.SourceIdentity);
            return Hash(material.ToString());
        }

        internal static string Hash(string material)
        {
            using (var sha = SHA256.Create())
            {
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(material ?? string.Empty))
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        private static void Append(StringBuilder target, params object[] fields)
        {
            foreach (var field in fields)
            {
                var value = Convert.ToString(field, CultureInfo.InvariantCulture) ?? string.Empty;
                target.Append(value.Length.ToString(CultureInfo.InvariantCulture));
                target.Append(':');
                target.Append(value);
            }
            target.Append('\n');
        }
    }
}
