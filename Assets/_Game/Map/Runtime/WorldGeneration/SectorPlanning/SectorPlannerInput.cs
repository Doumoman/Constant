using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum SectorPlannerSide
    {
        Left,
        Right,
        Up,
        Down,
    }

    public enum SectorPlannerSpecialRegionKind
    {
        None,
        Village,
        CoreResource,
        Forge,
        Boss,
        Merchant,
        Maru,
    }

    public enum SectorPlannerSpecialRegionBinding
    {
        None,
        ReferenceOnly,
        ReservedMandatory,
        DeferredOptionalLocal,
    }

    public enum SectorPlannerLandmarkDistanceBucket
    {
        SameSector,
        Near,
        Medium,
        Far,
        Unknown,
    }

    public enum SectorPacingReason
    {
        BoundaryWarning,
        RouteRecoveryNeed,
        QuietBuffer,
        VillageReference,
        MandatoryResource,
        MandatoryLandmark,
        ForgeMachineryCompatibility,
        BossGate,
        ActivityCatalogAvailable,
        EventCatalogAvailable,
        DeferredOptionalRegion,
        NeighborPacingContext,
        FlowFallback,
    }

    public enum SectorPlannerInputErrorCode
    {
        MissingInput,
        DuplicateSector,
        SectorOutOfRange,
        MissingAuthorityDigest,
        InvalidBiomePatch,
        InvalidRouteSnapshot,
        InvalidBoundarySnapshot,
        InvalidSiteSnapshot,
        InvalidNeighborSnapshot,
        InvalidSpecialRegionSnapshot,
        InvalidOptionalRegionSnapshot,
        PacingRoleUndefined,
        PacingAccessCoupling,
        PacingRouteMutationClaim,
        LandmarkDistanceInvalid,
        WorldProgressInvalid,
        DigestMismatch,
        NonCanonicalPublication,
        MutationClaim,
    }

    public sealed class SectorPlannerInputError : IEquatable<SectorPlannerInputError>, IComparable<SectorPlannerInputError>
    {
        public SectorPlannerInputError(SectorPlannerInputErrorCode code, string subject, string detail)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public SectorPlannerInputErrorCode Code { get; }
        public string Subject { get; }
        public string Detail { get; }

        public int CompareTo(SectorPlannerInputError other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0 ? comparison : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(SectorPlannerInputError other) => other != null && CompareTo(other) == 0;
        public override bool Equals(object obj) => Equals(obj as SectorPlannerInputError);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(ToString());
        public override string ToString() => Code + "|" + Subject + "|" + Detail;
    }

    public sealed class SectorPlannerAuthorityDigestSnapshot
    {
        public SectorPlannerAuthorityDigestSnapshot(
            string foundationDigest,
            string generationLayerDigest,
            string microPatternDigest,
            int microPatternCount,
            string terrainClusterDigest,
            int terrainClusterCount,
            string activityDigest,
            int activityCount,
            string eventDigest,
            int eventCount,
            string specialRegionAuditDigest,
            string coreResourceCatalogDigest,
            int coreResourceCount,
            string specialLandmarkCatalogDigest,
            int specialLandmarkCount)
        {
            FoundationDigest = foundationDigest ?? string.Empty;
            GenerationLayerDigest = generationLayerDigest ?? string.Empty;
            MicroPatternDigest = microPatternDigest ?? string.Empty;
            MicroPatternCount = microPatternCount;
            TerrainClusterDigest = terrainClusterDigest ?? string.Empty;
            TerrainClusterCount = terrainClusterCount;
            ActivityDigest = activityDigest ?? string.Empty;
            ActivityCount = activityCount;
            EventDigest = eventDigest ?? string.Empty;
            EventCount = eventCount;
            SpecialRegionAuditDigest = specialRegionAuditDigest ?? string.Empty;
            CoreResourceCatalogDigest = coreResourceCatalogDigest ?? string.Empty;
            CoreResourceCount = coreResourceCount;
            SpecialLandmarkCatalogDigest = specialLandmarkCatalogDigest ?? string.Empty;
            SpecialLandmarkCount = specialLandmarkCount;
            CanonicalDigest = SectorPlannerInputCanonicalDigest.Hash(string.Join("\n", new[]
            {
                FoundationDigest, GenerationLayerDigest, MicroPatternDigest,
                MicroPatternCount.ToString(CultureInfo.InvariantCulture), TerrainClusterDigest,
                TerrainClusterCount.ToString(CultureInfo.InvariantCulture), ActivityDigest,
                ActivityCount.ToString(CultureInfo.InvariantCulture), EventDigest,
                EventCount.ToString(CultureInfo.InvariantCulture), SpecialRegionAuditDigest,
                CoreResourceCatalogDigest, CoreResourceCount.ToString(CultureInfo.InvariantCulture),
                SpecialLandmarkCatalogDigest, SpecialLandmarkCount.ToString(CultureInfo.InvariantCulture),
            }));
        }

        public string FoundationDigest { get; }
        public string GenerationLayerDigest { get; }
        public string MicroPatternDigest { get; }
        public int MicroPatternCount { get; }
        public string TerrainClusterDigest { get; }
        public int TerrainClusterCount { get; }
        public string ActivityDigest { get; }
        public int ActivityCount { get; }
        public string EventDigest { get; }
        public int EventCount { get; }
        public string SpecialRegionAuditDigest { get; }
        public string CoreResourceCatalogDigest { get; }
        public int CoreResourceCount { get; }
        public string SpecialLandmarkCatalogDigest { get; }
        public int SpecialLandmarkCount { get; }
        public string CanonicalDigest { get; }

        public static SectorPlannerAuthorityDigestSnapshot CaptureCurrentPublicAuthorities(
            string foundationDigest,
            string microPatternDigest,
            int microPatternCount,
            string terrainClusterDigest,
            int terrainClusterCount,
            string activityDigest,
            int activityCount,
            string eventDigest,
            int eventCount,
            string specialRegionAuditDigest)
        {
            return new SectorPlannerAuthorityDigestSnapshot(
                foundationDigest,
                GenerationLayerCatalog.StableDigest,
                microPatternDigest,
                microPatternCount,
                terrainClusterDigest,
                terrainClusterCount,
                activityDigest,
                activityCount,
                eventDigest,
                eventCount,
                specialRegionAuditDigest,
                CoreResourceRegionStarterCatalog.CanonicalDigest,
                CoreResourceRegionStarterCatalog.Entries.Count,
                SpecialLandmarkRegionStarterCatalog.CanonicalDigest,
                SpecialLandmarkRegionStarterCatalog.Entries.Count);
        }

        public IEnumerable<string> EnumerateDigests()
        {
            yield return FoundationDigest;
            yield return GenerationLayerDigest;
            yield return MicroPatternDigest;
            yield return TerrainClusterDigest;
            yield return ActivityDigest;
            yield return EventDigest;
            yield return SpecialRegionAuditDigest;
            yield return CoreResourceCatalogDigest;
            yield return SpecialLandmarkCatalogDigest;
        }
    }

    public sealed class SectorPlannerBiomeSnapshot
    {
        public SectorPlannerBiomeSnapshot(string patchId, string biomeId)
        {
            PatchId = patchId ?? string.Empty;
            BiomeId = biomeId ?? string.Empty;
        }

        public string PatchId { get; }
        public string BiomeId { get; }
    }

    public sealed class SectorPlannerRouteSnapshot
    {
        private readonly ReadOnlyCollection<string> externalSockets;

        public SectorPlannerRouteSnapshot(
            int routeType,
            AccessClass accessClass,
            IEnumerable<string> sourceExternalSockets,
            bool highRoute,
            bool recoveryNeeded)
        {
            RouteType = routeType;
            AccessClass = accessClass;
            externalSockets = CopyStrings(sourceExternalSockets);
            HighRoute = highRoute;
            RecoveryNeeded = recoveryNeeded;
        }

        public int RouteType { get; }
        public AccessClass AccessClass { get; }
        public IReadOnlyList<string> ExternalSockets => externalSockets;
        public bool HighRoute { get; }
        public bool RecoveryNeeded { get; }

        private static ReadOnlyCollection<string> CopyStrings(IEnumerable<string> source)
        {
            return new ReadOnlyCollection<string>((source ?? Array.Empty<string>())
                .Select(value => value ?? string.Empty)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray());
        }
    }

    public sealed class SectorPlannerBoundarySnapshot
    {
        public SectorPlannerBoundarySnapshot(
            SectorPlannerSide side,
            string pairId,
            string candidateId,
            int warningCount)
        {
            Side = side;
            PairId = pairId ?? string.Empty;
            CandidateId = candidateId ?? string.Empty;
            WarningCount = warningCount;
        }

        public SectorPlannerSide Side { get; }
        public string PairId { get; }
        public string CandidateId { get; }
        public int WarningCount { get; }
    }

    public sealed class SectorPlannerSiteSnapshot
    {
        public SectorPlannerSiteSnapshot(string siteId, string siteKind, string reservationId, bool mandatory)
        {
            SiteId = siteId ?? string.Empty;
            SiteKind = siteKind ?? string.Empty;
            ReservationId = reservationId ?? string.Empty;
            Mandatory = mandatory;
        }

        public string SiteId { get; }
        public string SiteKind { get; }
        public string ReservationId { get; }
        public bool Mandatory { get; }
    }

    public sealed class SectorPlannerSpecialRegionSnapshot
    {
        public SectorPlannerSpecialRegionSnapshot(
            string regionId,
            SectorPlannerSpecialRegionKind kind,
            SectorPlannerSpecialRegionBinding binding,
            string footprintId,
            bool reserved,
            bool placedOwnershipClaim,
            bool mandatoryProgressionDependency)
        {
            RegionId = regionId ?? string.Empty;
            Kind = kind;
            Binding = binding;
            FootprintId = footprintId ?? string.Empty;
            Reserved = reserved;
            PlacedOwnershipClaim = placedOwnershipClaim;
            MandatoryProgressionDependency = mandatoryProgressionDependency;
        }

        public string RegionId { get; }
        public SectorPlannerSpecialRegionKind Kind { get; }
        public SectorPlannerSpecialRegionBinding Binding { get; }
        public string FootprintId { get; }
        public bool Reserved { get; }
        public bool PlacedOwnershipClaim { get; }
        public bool MandatoryProgressionDependency { get; }

        public static SectorPlannerSpecialRegionSnapshot None { get; } =
            new SectorPlannerSpecialRegionSnapshot(
                string.Empty,
                SectorPlannerSpecialRegionKind.None,
                SectorPlannerSpecialRegionBinding.None,
                string.Empty,
                false,
                false,
                false);
    }

    public sealed class SectorPlannerOptionalRegionSnapshot
    {
        public SectorPlannerOptionalRegionSnapshot(
            string regionId,
            SectorPlannerSpecialRegionKind kind,
            bool available,
            bool deferredLocal,
            bool placedOwnershipClaim)
        {
            RegionId = regionId ?? string.Empty;
            Kind = kind;
            Available = available;
            DeferredLocal = deferredLocal;
            PlacedOwnershipClaim = placedOwnershipClaim;
        }

        public string RegionId { get; }
        public SectorPlannerSpecialRegionKind Kind { get; }
        public bool Available { get; }
        public bool DeferredLocal { get; }
        public bool PlacedOwnershipClaim { get; }
    }

    public sealed class SectorPlannerNeighborSnapshot
    {
        private readonly ReadOnlyCollection<string> externalSockets;

        public SectorPlannerNeighborSnapshot(
            SectorPlannerSide side,
            SectorCoord coordinate,
            int routeType,
            AccessClass accessClass,
            IEnumerable<string> sourceExternalSockets,
            PacingRole primaryRole)
        {
            Side = side;
            Coordinate = coordinate;
            RouteType = routeType;
            AccessClass = accessClass;
            externalSockets = new ReadOnlyCollection<string>((sourceExternalSockets ?? Array.Empty<string>())
                .Select(value => value ?? string.Empty)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray());
            PrimaryRole = primaryRole;
        }

        public SectorPlannerSide Side { get; }
        public SectorCoord Coordinate { get; }
        public int RouteType { get; }
        public AccessClass AccessClass { get; }
        public IReadOnlyList<string> ExternalSockets => externalSockets;
        public PacingRole PrimaryRole { get; }
    }

    public sealed class SectorPlannerWorldProgressSnapshot
    {
        public SectorPlannerWorldProgressSnapshot(
            int ordinal,
            string chapterBucket,
            string branchBucket,
            int nearestMandatoryLandmarkDistance,
            int nearestOptionalLandmarkDistance)
        {
            Ordinal = ordinal;
            ChapterBucket = chapterBucket ?? string.Empty;
            BranchBucket = branchBucket ?? string.Empty;
            NearestMandatoryLandmarkDistance = nearestMandatoryLandmarkDistance;
            NearestOptionalLandmarkDistance = nearestOptionalLandmarkDistance;
        }

        public int Ordinal { get; }
        public string ChapterBucket { get; }
        public string BranchBucket { get; }
        public int NearestMandatoryLandmarkDistance { get; }
        public int NearestOptionalLandmarkDistance { get; }
    }

    public sealed class SectorPlannerSectorSnapshot
    {
        private readonly ReadOnlyCollection<SectorPlannerBoundarySnapshot> boundaries;
        private readonly ReadOnlyCollection<SectorPlannerSiteSnapshot> sites;
        private readonly ReadOnlyCollection<SectorPlannerOptionalRegionSnapshot> optionalRegions;
        private readonly ReadOnlyCollection<SectorPlannerNeighborSnapshot> neighbors;
        private readonly ReadOnlyCollection<PacingRole> compatiblePacingRoles;

        public SectorPlannerSectorSnapshot(
            SectorCoord coordinate,
            int sectorIndex,
            int canvasWidth,
            int canvasHeight,
            SectorPlannerBiomeSnapshot biome,
            SectorPlannerRouteSnapshot route,
            IEnumerable<SectorPlannerBoundarySnapshot> sourceBoundaries,
            IEnumerable<SectorPlannerSiteSnapshot> sourceSites,
            SectorPlannerSpecialRegionSnapshot specialRegion,
            IEnumerable<SectorPlannerOptionalRegionSnapshot> sourceOptionalRegions,
            IEnumerable<SectorPlannerNeighborSnapshot> sourceNeighbors,
            SectorPlannerWorldProgressSnapshot worldProgress,
            IEnumerable<PacingRole> sourceCompatiblePacingRoles,
            bool quietCompatible,
            bool activityCatalogAvailable,
            bool eventCatalogAvailable)
        {
            Coordinate = coordinate;
            SectorIndex = sectorIndex;
            CanvasWidth = canvasWidth;
            CanvasHeight = canvasHeight;
            Biome = biome;
            Route = route;
            boundaries = new ReadOnlyCollection<SectorPlannerBoundarySnapshot>(
                (sourceBoundaries ?? Array.Empty<SectorPlannerBoundarySnapshot>()).Where(value => value != null)
                .OrderBy(value => value.Side)
                .ThenBy(value => value.PairId, StringComparer.Ordinal)
                .ThenBy(value => value.CandidateId, StringComparer.Ordinal)
                .ToArray());
            sites = new ReadOnlyCollection<SectorPlannerSiteSnapshot>(
                (sourceSites ?? Array.Empty<SectorPlannerSiteSnapshot>()).Where(value => value != null)
                .OrderBy(value => value.SiteId, StringComparer.Ordinal)
                .ThenBy(value => value.ReservationId, StringComparer.Ordinal)
                .ThenBy(value => value.SiteKind, StringComparer.Ordinal)
                .ToArray());
            SpecialRegion = specialRegion ?? SectorPlannerSpecialRegionSnapshot.None;
            optionalRegions = new ReadOnlyCollection<SectorPlannerOptionalRegionSnapshot>(
                (sourceOptionalRegions ?? Array.Empty<SectorPlannerOptionalRegionSnapshot>()).Where(value => value != null)
                .OrderBy(value => value.RegionId, StringComparer.Ordinal)
                .ThenBy(value => value.Kind)
                .ThenBy(value => value.DeferredLocal)
                .ToArray());
            neighbors = new ReadOnlyCollection<SectorPlannerNeighborSnapshot>(
                (sourceNeighbors ?? Array.Empty<SectorPlannerNeighborSnapshot>()).Where(value => value != null)
                .OrderBy(value => value.Side)
                .ThenBy(value => value.Coordinate.Y)
                .ThenBy(value => value.Coordinate.X)
                .ToArray());
            WorldProgress = worldProgress;
            compatiblePacingRoles = new ReadOnlyCollection<PacingRole>((sourceCompatiblePacingRoles ?? Array.Empty<PacingRole>())
                .Distinct().OrderBy(value => value).ToArray());
            QuietCompatible = quietCompatible;
            ActivityCatalogAvailable = activityCatalogAvailable;
            EventCatalogAvailable = eventCatalogAvailable;
        }

        public SectorCoord Coordinate { get; }
        public int SectorIndex { get; }
        public int CanvasWidth { get; }
        public int CanvasHeight { get; }
        public SectorPlannerBiomeSnapshot Biome { get; }
        public SectorPlannerRouteSnapshot Route { get; }
        public IReadOnlyList<SectorPlannerBoundarySnapshot> Boundaries => boundaries;
        public IReadOnlyList<SectorPlannerSiteSnapshot> Sites => sites;
        public SectorPlannerSpecialRegionSnapshot SpecialRegion { get; }
        public IReadOnlyList<SectorPlannerOptionalRegionSnapshot> OptionalRegions => optionalRegions;
        public IReadOnlyList<SectorPlannerNeighborSnapshot> Neighbors => neighbors;
        public SectorPlannerWorldProgressSnapshot WorldProgress { get; }
        public IReadOnlyList<PacingRole> CompatiblePacingRoles => compatiblePacingRoles;
        public bool QuietCompatible { get; }
        public bool ActivityCatalogAvailable { get; }
        public bool EventCatalogAvailable { get; }

    }

    public sealed class SectorPlannerInputRequest
    {
        private readonly ReadOnlyCollection<SectorPlannerSectorSnapshot> sectors;

        public SectorPlannerInputRequest(
            IEnumerable<SectorPlannerSectorSnapshot> sourceSectors,
            SectorPlannerAuthorityDigestSnapshot authority,
            string publicationLabel,
            string expectedCanonicalDigest = "",
            int csvReparseCount = 0,
            int generatedWriteCount = 0,
            int sceneMutationCount = 0,
            int assetMutationCount = 0,
            int solverInvocationCount = 0,
            int randomDrawCount = 0,
            bool pacingChangesAccess = false,
            bool pacingChangesRoute = false)
        {
            sectors = new ReadOnlyCollection<SectorPlannerSectorSnapshot>((sourceSectors ?? Array.Empty<SectorPlannerSectorSnapshot>()).ToArray());
            Authority = authority;
            PublicationLabel = publicationLabel ?? string.Empty;
            ExpectedCanonicalDigest = expectedCanonicalDigest ?? string.Empty;
            CsvReparseCount = csvReparseCount;
            GeneratedWriteCount = generatedWriteCount;
            SceneMutationCount = sceneMutationCount;
            AssetMutationCount = assetMutationCount;
            SolverInvocationCount = solverInvocationCount;
            RandomDrawCount = randomDrawCount;
            PacingChangesAccess = pacingChangesAccess;
            PacingChangesRoute = pacingChangesRoute;
        }

        public IReadOnlyList<SectorPlannerSectorSnapshot> Sectors => sectors;
        public SectorPlannerAuthorityDigestSnapshot Authority { get; }
        public string PublicationLabel { get; }
        public string ExpectedCanonicalDigest { get; }
        public int CsvReparseCount { get; }
        public int GeneratedWriteCount { get; }
        public int SceneMutationCount { get; }
        public int AssetMutationCount { get; }
        public int SolverInvocationCount { get; }
        public int RandomDrawCount { get; }
        public bool PacingChangesAccess { get; }
        public bool PacingChangesRoute { get; }
    }

    public sealed class SectorPlannerInput
    {
        private readonly ReadOnlyCollection<SectorPlannerSectorSnapshot> sectors;
        private readonly ReadOnlyDictionary<int, SectorPlannerSectorSnapshot> byIndex;

        internal SectorPlannerInput(
            IEnumerable<SectorPlannerSectorSnapshot> sourceSectors,
            SectorPlannerAuthorityDigestSnapshot authority,
            string publicationLabel,
            string canonicalDigest)
        {
            var ordered = sourceSectors.OrderBy(value => value.SectorIndex).ToArray();
            sectors = new ReadOnlyCollection<SectorPlannerSectorSnapshot>(ordered);
            byIndex = new ReadOnlyDictionary<int, SectorPlannerSectorSnapshot>(ordered.ToDictionary(value => value.SectorIndex));
            Authority = authority;
            PublicationLabel = publicationLabel;
            CanonicalDigest = canonicalDigest;
        }

        public IReadOnlyList<SectorPlannerSectorSnapshot> Sectors => sectors;
        public SectorPlannerAuthorityDigestSnapshot Authority { get; }
        public string PublicationLabel { get; }
        public string CanonicalDigest { get; }
        public int CsvReparseCount => 0;
        public int GeneratedWriteCount => 0;
        public int SceneMutationCount => 0;
        public int AssetMutationCount => 0;
        public int SolverInvocationCount => 0;
        public int RandomDrawCount => 0;

        public bool TryGetSector(SectorCoord coordinate, out SectorPlannerSectorSnapshot sector)
        {
            return byIndex.TryGetValue((coordinate.Y * WorldGenConstants.SectorColumns) + coordinate.X, out sector)
                && sector.Coordinate.X == coordinate.X
                && sector.Coordinate.Y == coordinate.Y;
        }
    }

    public sealed class SectorPlannerInputBuildResult
    {
        private readonly ReadOnlyCollection<SectorPlannerInputError> errors;

        internal SectorPlannerInputBuildResult(SectorPlannerInput input, IEnumerable<SectorPlannerInputError> sourceErrors)
        {
            var ordered = (sourceErrors ?? Array.Empty<SectorPlannerInputError>()).Where(value => value != null)
                .Distinct().OrderBy(value => value).ToArray();
            errors = new ReadOnlyCollection<SectorPlannerInputError>(ordered);
            Input = ordered.Length == 0 ? input : null;
            CanonicalDigest = Input == null ? string.Empty : Input.CanonicalDigest;
        }

        public bool Success => Input != null && errors.Count == 0;
        public SectorPlannerInput Input { get; }
        public string CanonicalDigest { get; }
        public IReadOnlyList<SectorPlannerInputError> Errors => errors;
    }

    public sealed class SectorPacingCandidate
    {
        internal SectorPacingCandidate(
            PacingRole role,
            int hardPriorityClass,
            int worldProgressSuitability,
            SectorPlannerLandmarkDistanceBucket landmarkDistanceBucket,
            SectorPacingReason reason)
        {
            Role = role;
            HardPriorityClass = hardPriorityClass;
            WorldProgressSuitability = worldProgressSuitability;
            LandmarkDistanceBucket = landmarkDistanceBucket;
            Reason = reason;
        }

        public PacingRole Role { get; }
        public int HardPriorityClass { get; }
        public int WorldProgressSuitability { get; }
        public SectorPlannerLandmarkDistanceBucket LandmarkDistanceBucket { get; }
        public SectorPacingReason Reason { get; }
    }

    public sealed class SectorPacingAssignment
    {
        private readonly ReadOnlyCollection<SectorPacingCandidate> candidates;
        private readonly ReadOnlyCollection<SectorPacingReason> reasons;

        internal SectorPacingAssignment(
            SectorCoord coordinate,
            PacingRole primaryRole,
            IEnumerable<SectorPacingCandidate> sourceCandidates,
            IEnumerable<SectorPacingReason> sourceReasons,
            string sourceIdentityDigest,
            string canonicalDigest)
        {
            Coordinate = coordinate;
            PrimaryRole = primaryRole;
            candidates = new ReadOnlyCollection<SectorPacingCandidate>(sourceCandidates.ToArray());
            reasons = new ReadOnlyCollection<SectorPacingReason>(sourceReasons.Distinct().OrderBy(value => value).ToArray());
            SourceIdentityDigest = sourceIdentityDigest;
            CanonicalDigest = canonicalDigest;
        }

        public SectorCoord Coordinate { get; }
        public PacingRole PrimaryRole { get; }
        public IReadOnlyList<SectorPacingCandidate> Candidates => candidates;
        public IReadOnlyList<SectorPacingReason> Reasons => reasons;
        public string SourceIdentityDigest { get; }
        public string CanonicalDigest { get; }
        public int RouteMutationCount => 0;
        public int AccessMutationCount => 0;
        public int SocketMutationCount => 0;
        public int BoundaryMutationCount => 0;
        public int SiteMutationCount => 0;
        public int CatalogMutationCount => 0;
        public int RandomDrawCount => 0;
        public int PlacementCount => 0;
        public int MarkerCount => 0;
        public int SpawnCount => 0;
    }

    public static class SectorPlannerInputCanonicalDigest
    {
        public static string Compute(SectorPlannerInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            return Compute(input.Sectors, input.Authority, input.PublicationLabel);
        }

        internal static string Compute(
            IEnumerable<SectorPlannerSectorSnapshot> sectors,
            SectorPlannerAuthorityDigestSnapshot authority,
            string publicationLabel)
        {
            var material = new StringBuilder();
            Append(material, "PUBLICATION", publicationLabel);
            Append(material, "AUTHORITY", authority == null ? string.Empty : authority.CanonicalDigest);
            foreach (var sector in sectors.OrderBy(value => value.SectorIndex))
            {
                Append(material, "SECTOR", sector.SectorIndex, sector.Coordinate.X, sector.Coordinate.Y,
                    sector.CanvasWidth, sector.CanvasHeight, sector.Biome?.PatchId, sector.Biome?.BiomeId,
                    sector.Route?.RouteType, sector.Route?.AccessClass, sector.Route?.HighRoute,
                    sector.Route?.RecoveryNeeded, sector.QuietCompatible, sector.ActivityCatalogAvailable,
                    sector.EventCatalogAvailable);
                foreach (var socket in sector.Route?.ExternalSockets ?? Array.Empty<string>()) Append(material, "SOCKET", socket);
                foreach (var boundary in sector.Boundaries) Append(material, "BOUNDARY", boundary.Side, boundary.PairId, boundary.CandidateId, boundary.WarningCount);
                foreach (var site in sector.Sites) Append(material, "SITE", site.SiteId, site.SiteKind, site.ReservationId, site.Mandatory);
                var special = sector.SpecialRegion;
                Append(material, "SPECIAL", special.RegionId, special.Kind, special.Binding, special.FootprintId,
                    special.Reserved, special.PlacedOwnershipClaim, special.MandatoryProgressionDependency);
                foreach (var optional in sector.OptionalRegions) Append(material, "OPTIONAL", optional.RegionId, optional.Kind, optional.Available, optional.DeferredLocal, optional.PlacedOwnershipClaim);
                foreach (var neighbor in sector.Neighbors)
                {
                    Append(material, "NEIGHBOR", neighbor.Side, neighbor.Coordinate.X, neighbor.Coordinate.Y,
                        neighbor.RouteType, neighbor.AccessClass, neighbor.PrimaryRole,
                        string.Join(",", neighbor.ExternalSockets));
                }
                var progress = sector.WorldProgress;
                Append(material, "PROGRESS", progress?.Ordinal, progress?.ChapterBucket, progress?.BranchBucket,
                    progress?.NearestMandatoryLandmarkDistance, progress?.NearestOptionalLandmarkDistance);
                foreach (var role in sector.CompatiblePacingRoles) Append(material, "ROLE", role);
            }

            return Hash(material.ToString());
        }

        internal static string ComputeIdentity(SectorPlannerSectorSnapshot sector)
        {
            var material = new StringBuilder();
            Append(material, "ROUTE", sector.Route.RouteType, sector.Route.AccessClass,
                string.Join(",", sector.Route.ExternalSockets));
            foreach (var boundary in sector.Boundaries) Append(material, "BOUNDARY", boundary.Side, boundary.PairId, boundary.CandidateId);
            foreach (var site in sector.Sites) Append(material, "SITE", site.SiteId, site.ReservationId);
            Append(material, "SPECIAL", sector.SpecialRegion.RegionId, sector.SpecialRegion.Binding,
                sector.SpecialRegion.FootprintId, sector.SpecialRegion.PlacedOwnershipClaim);
            return Hash(material.ToString());
        }

        internal static string Hash(string value)
        {
            using (var algorithm = SHA256.Create())
            {
                var bytes = algorithm.ComputeHash(new UTF8Encoding(false).GetBytes(value ?? string.Empty));
                return string.Concat(bytes.Select(item => item.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        internal static void Append(StringBuilder target, params object[] values)
        {
            foreach (var value in values)
            {
                var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                target.Append(text.Length.ToString(CultureInfo.InvariantCulture));
                target.Append(':');
                target.Append(text);
                target.Append('|');
            }
            target.Append('\n');
        }
    }
}
