using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum SectorFixedAnchorKind
    {
        ExternalRouteSocket,
        BoundaryFixedSlice,
        BoundaryWarning,
        SpecialFootprint,
        SpecialEntryReturn,
        SpecialApronBuffer,
        SiteReservation,
        ReferenceOnlyMarker,
    }

    public enum SectorFixedAnchorSource
    {
        RouteSnapshot,
        BoundarySnapshot,
        SiteSnapshot,
        SpecialRegionSnapshot,
        OptionalRegionSnapshot,
        PacingAssignment,
        ReferenceFixture,
    }

    public enum SectorFixedAnchorPriority
    {
        ReferenceOnly = 100,
        BoundaryWarning = 200,
        BoundaryFixedSlice = 300,
        ExternalRouteSocket = 400,
        SpecialTransition = 500,
        SpecialReservation = 600,
    }

    public enum SectorFixedAnchorErrorCode
    {
        MissingInput,
        MissingPacingAssignment,
        SectorMismatch,
        AnchorOutOfBounds,
        InvalidSideAnchor,
        InvalidBoundaryAnchor,
        InvalidSpecialAnchor,
        DeferredPlacedClaim,
        ReferenceLiveClaim,
        DuplicateAnchorId,
        IncompatibleOverlap,
        PriorityViolation,
        RouteAccessMutationClaim,
        BoundaryMutationClaim,
        SiteMutationClaim,
        SpecialMutationClaim,
        SolverMutationClaim,
        NonCanonicalPublication,
    }

    public sealed class SectorFixedAnchorRect : IEquatable<SectorFixedAnchorRect>, IComparable<SectorFixedAnchorRect>
    {
        public SectorFixedAnchorRect(int x, int y, int width, int height)
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

        public bool IsInside(int canvasWidth, int canvasHeight)
        {
            return Width > 0 && Height > 0 && X >= 0 && Y >= 0
                   && XMaxExclusive <= canvasWidth && YMaxExclusive <= canvasHeight;
        }

        public bool TouchesOnly(SectorPlannerSide side, int canvasWidth, int canvasHeight)
        {
            if (!IsInside(canvasWidth, canvasHeight)) return false;
            var left = X == 0;
            var right = XMaxExclusive == canvasWidth;
            var up = Y == 0;
            var down = YMaxExclusive == canvasHeight;
            var touchCount = (left ? 1 : 0) + (right ? 1 : 0) + (up ? 1 : 0) + (down ? 1 : 0);
            if (touchCount != 1) return false;
            switch (side)
            {
                case SectorPlannerSide.Left: return left;
                case SectorPlannerSide.Right: return right;
                case SectorPlannerSide.Up: return up;
                case SectorPlannerSide.Down: return down;
                default: return false;
            }
        }

        public bool Overlaps(SectorFixedAnchorRect other)
        {
            return other != null
                   && X < other.XMaxExclusive && XMaxExclusive > other.X
                   && Y < other.YMaxExclusive && YMaxExclusive > other.Y;
        }

        public int CompareTo(SectorFixedAnchorRect other)
        {
            if (other == null) return -1;
            var comparison = Y.CompareTo(other.Y);
            if (comparison != 0) return comparison;
            comparison = X.CompareTo(other.X);
            if (comparison != 0) return comparison;
            comparison = Height.CompareTo(other.Height);
            return comparison != 0 ? comparison : Width.CompareTo(other.Width);
        }

        public bool Equals(SectorFixedAnchorRect other) => other != null && CompareTo(other) == 0;
        public override bool Equals(object obj) => Equals(obj as SectorFixedAnchorRect);
        public override int GetHashCode() => ((X * 397) ^ Y) * 397 ^ Width * 31 ^ Height;
        public override string ToString() => string.Join(",", new[]
        {
            X.ToString(CultureInfo.InvariantCulture),
            Y.ToString(CultureInfo.InvariantCulture),
            Width.ToString(CultureInfo.InvariantCulture),
            Height.ToString(CultureInfo.InvariantCulture),
        });
    }

    public sealed class SectorFixedAnchorProjection
    {
        public SectorFixedAnchorProjection(
            string anchorId,
            SectorCoord sectorCoordinate,
            SectorFixedAnchorKind kind,
            SectorFixedAnchorSource source,
            SectorFixedAnchorPriority priority,
            SectorFixedAnchorRect rect,
            string sourceId,
            SectorPlannerSide? side = null,
            bool allowsCompatibleOverlap = false,
            string compatibilityGroup = "",
            bool placedOwnershipClaim = false,
            bool progressionBlockerClaim = false)
        {
            AnchorId = anchorId ?? string.Empty;
            SectorCoordinate = sectorCoordinate;
            Kind = kind;
            Source = source;
            Priority = priority;
            Rect = rect;
            SourceId = sourceId ?? string.Empty;
            Side = side;
            AllowsCompatibleOverlap = allowsCompatibleOverlap;
            CompatibilityGroup = compatibilityGroup ?? string.Empty;
            PlacedOwnershipClaim = placedOwnershipClaim;
            ProgressionBlockerClaim = progressionBlockerClaim;
        }

        public string AnchorId { get; }
        public SectorCoord SectorCoordinate { get; }
        public SectorFixedAnchorKind Kind { get; }
        public SectorFixedAnchorSource Source { get; }
        public SectorFixedAnchorPriority Priority { get; }
        public SectorFixedAnchorRect Rect { get; }
        public string SourceId { get; }
        public SectorPlannerSide? Side { get; }
        public bool AllowsCompatibleOverlap { get; }
        public string CompatibilityGroup { get; }
        public bool PlacedOwnershipClaim { get; }
        public bool ProgressionBlockerClaim { get; }
    }

    public sealed class SectorFixedAnchor
    {
        internal SectorFixedAnchor(SectorFixedAnchorProjection projection, int sectorIndex, string sourceIdentity)
        {
            AnchorId = projection.AnchorId;
            SectorCoordinate = projection.SectorCoordinate;
            SectorIndex = sectorIndex;
            Kind = projection.Kind;
            Source = projection.Source;
            Priority = projection.Priority;
            Rect = projection.Rect;
            SourceId = projection.SourceId;
            SourceIdentity = sourceIdentity ?? string.Empty;
            Side = projection.Side;
            AllowsCompatibleOverlap = projection.AllowsCompatibleOverlap;
            CompatibilityGroup = projection.CompatibilityGroup;
            PlacedOwnershipClaim = projection.PlacedOwnershipClaim;
            ProgressionBlockerClaim = projection.ProgressionBlockerClaim;
        }

        public string AnchorId { get; }
        public SectorCoord SectorCoordinate { get; }
        public int SectorIndex { get; }
        public SectorFixedAnchorKind Kind { get; }
        public SectorFixedAnchorSource Source { get; }
        public SectorFixedAnchorPriority Priority { get; }
        public SectorFixedAnchorRect Rect { get; }
        public string SourceId { get; }
        public string SourceIdentity { get; }
        public SectorPlannerSide? Side { get; }
        public bool AllowsCompatibleOverlap { get; }
        public string CompatibilityGroup { get; }
        public bool PlacedOwnershipClaim { get; }
        public bool ProgressionBlockerClaim { get; }
    }

    public sealed class SectorFixedAnchorBuildRequest
    {
        private readonly ReadOnlyCollection<SectorPacingAssignment> assignments;
        private readonly ReadOnlyCollection<SectorFixedAnchorProjection> projections;

        public SectorFixedAnchorBuildRequest(
            SectorPlannerInput input,
            IEnumerable<SectorPacingAssignment> sourceAssignments,
            IEnumerable<SectorFixedAnchorProjection> sourceProjections,
            string publicationLabel,
            string expectedCanonicalDigest = "",
            bool routeAccessMutationClaim = false,
            bool boundaryMutationClaim = false,
            bool siteMutationClaim = false,
            bool specialMutationClaim = false,
            int solverMutationCount = 0,
            int randomDrawCount = 0,
            int tileWriteCount = 0,
            int canvasMutationCount = 0,
            int assetMutationCount = 0)
        {
            Input = input;
            assignments = new ReadOnlyCollection<SectorPacingAssignment>(
                (sourceAssignments ?? Array.Empty<SectorPacingAssignment>()).Where(value => value != null).ToArray());
            projections = new ReadOnlyCollection<SectorFixedAnchorProjection>(
                (sourceProjections ?? Array.Empty<SectorFixedAnchorProjection>()).Where(value => value != null).ToArray());
            PublicationLabel = publicationLabel ?? string.Empty;
            ExpectedCanonicalDigest = expectedCanonicalDigest ?? string.Empty;
            RouteAccessMutationClaim = routeAccessMutationClaim;
            BoundaryMutationClaim = boundaryMutationClaim;
            SiteMutationClaim = siteMutationClaim;
            SpecialMutationClaim = specialMutationClaim;
            SolverMutationCount = solverMutationCount;
            RandomDrawCount = randomDrawCount;
            TileWriteCount = tileWriteCount;
            CanvasMutationCount = canvasMutationCount;
            AssetMutationCount = assetMutationCount;
        }

        public SectorPlannerInput Input { get; }
        public IReadOnlyList<SectorPacingAssignment> Assignments => assignments;
        public IReadOnlyList<SectorFixedAnchorProjection> Projections => projections;
        public string PublicationLabel { get; }
        public string ExpectedCanonicalDigest { get; }
        public bool RouteAccessMutationClaim { get; }
        public bool BoundaryMutationClaim { get; }
        public bool SiteMutationClaim { get; }
        public bool SpecialMutationClaim { get; }
        public int SolverMutationCount { get; }
        public int RandomDrawCount { get; }
        public int TileWriteCount { get; }
        public int CanvasMutationCount { get; }
        public int AssetMutationCount { get; }
    }

    public sealed class SectorFixedAnchorError : IEquatable<SectorFixedAnchorError>, IComparable<SectorFixedAnchorError>
    {
        public SectorFixedAnchorError(SectorFixedAnchorErrorCode code, string subject, string detail)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public SectorFixedAnchorErrorCode Code { get; }
        public string Subject { get; }
        public string Detail { get; }

        public int CompareTo(SectorFixedAnchorError other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0 ? comparison : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(SectorFixedAnchorError other) => other != null && CompareTo(other) == 0;
        public override bool Equals(object obj) => Equals(obj as SectorFixedAnchorError);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(ToString());
        public override string ToString() => Code + "|" + Subject + "|" + Detail;
    }

    public sealed class SectorFixedAnchorPlan
    {
        private readonly ReadOnlyCollection<SectorFixedAnchor> anchors;
        private readonly ReadOnlyDictionary<SectorFixedAnchorKind, int> countByKind;
        private readonly ReadOnlyDictionary<SectorFixedAnchorSource, int> countBySource;
        private readonly ReadOnlyDictionary<SectorFixedAnchorPriority, int> countByPriority;
        private readonly ReadOnlyDictionary<int, int> countBySectorIndex;

        internal SectorFixedAnchorPlan(
            SectorPlannerInput input,
            IEnumerable<SectorFixedAnchor> sourceAnchors,
            string publicationLabel,
            int compatibleOverlapCount,
            string assignmentDigest,
            string routeIdentityDigest,
            string boundaryIdentityDigest,
            string siteIdentityDigest,
            string specialIdentityDigest,
            string canonicalDigest)
        {
            var ordered = sourceAnchors
                .OrderBy(value => value.SectorIndex)
                .ThenByDescending(value => value.Priority)
                .ThenBy(value => value.Kind)
                .ThenBy(value => value.Rect)
                .ThenBy(value => value.AnchorId, StringComparer.Ordinal)
                .ToArray();
            anchors = new ReadOnlyCollection<SectorFixedAnchor>(ordered);
            countByKind = Counts(ordered, value => value.Kind, Enum.GetValues(typeof(SectorFixedAnchorKind)).Cast<SectorFixedAnchorKind>());
            countBySource = Counts(ordered, value => value.Source, Enum.GetValues(typeof(SectorFixedAnchorSource)).Cast<SectorFixedAnchorSource>());
            countByPriority = Counts(ordered, value => value.Priority, Enum.GetValues(typeof(SectorFixedAnchorPriority)).Cast<SectorFixedAnchorPriority>());
            var sectorCounts = input.Sectors.ToDictionary(value => value.SectorIndex, value => 0);
            foreach (var anchor in ordered) sectorCounts[anchor.SectorIndex]++;
            countBySectorIndex = new ReadOnlyDictionary<int, int>(new SortedDictionary<int, int>(sectorCounts));
            PublicationLabel = publicationLabel;
            PlannerInputDigest = input.CanonicalDigest;
            AssignmentDigest = assignmentDigest;
            SectorCount = input.Sectors.Count;
            CompatibleOverlapCount = compatibleOverlapCount;
            RouteIdentityBeforeDigest = routeIdentityDigest;
            RouteIdentityAfterDigest = routeIdentityDigest;
            BoundaryIdentityBeforeDigest = boundaryIdentityDigest;
            BoundaryIdentityAfterDigest = boundaryIdentityDigest;
            SiteIdentityBeforeDigest = siteIdentityDigest;
            SiteIdentityAfterDigest = siteIdentityDigest;
            SpecialIdentityBeforeDigest = specialIdentityDigest;
            SpecialIdentityAfterDigest = specialIdentityDigest;
            CanonicalDigest = canonicalDigest;
        }

        public string PublicationLabel { get; }
        public string PlannerInputDigest { get; }
        public string AssignmentDigest { get; }
        public int SectorCount { get; }
        public IReadOnlyList<SectorFixedAnchor> Anchors => anchors;
        public IReadOnlyDictionary<SectorFixedAnchorKind, int> CountByKind => countByKind;
        public IReadOnlyDictionary<SectorFixedAnchorSource, int> CountBySource => countBySource;
        public IReadOnlyDictionary<SectorFixedAnchorPriority, int> CountByPriority => countByPriority;
        public IReadOnlyDictionary<int, int> CountBySectorIndex => countBySectorIndex;
        public int CollisionCount => 0;
        public int CompatibleOverlapCount { get; }
        public string RouteIdentityBeforeDigest { get; }
        public string RouteIdentityAfterDigest { get; }
        public string BoundaryIdentityBeforeDigest { get; }
        public string BoundaryIdentityAfterDigest { get; }
        public string SiteIdentityBeforeDigest { get; }
        public string SiteIdentityAfterDigest { get; }
        public string SpecialIdentityBeforeDigest { get; }
        public string SpecialIdentityAfterDigest { get; }
        public string CanonicalDigest { get; }
        public bool Map14_03HandoffReady => true;
        public int RouteMutationCount => 0;
        public int AccessMutationCount => 0;
        public int SocketMutationCount => 0;
        public int BoundaryMutationCount => 0;
        public int SiteMutationCount => 0;
        public int SpecialMutationCount => 0;
        public int SolverInvocationCount => 0;
        public int RandomDrawCount => 0;
        public int TileWriteCount => 0;
        public int CanvasMutationCount => 0;
        public int AssetMutationCount => 0;
        public int ClusterCandidateCount => 0;
        public int ClusterPlacementCount => 0;
        public int ActivityPlacementCount => 0;
        public int EventMarkerCount => 0;
        public int GameplaySpawnCount => 0;
        public int PathEdgeCount => 0;

        public int Count(SectorFixedAnchorKind kind) => countByKind[kind];
        public int Count(SectorFixedAnchorSource source) => countBySource[source];
        public int Count(SectorFixedAnchorPriority priority) => countByPriority[priority];
        public int CountForSector(SectorCoord coordinate)
        {
            var index = (coordinate.Y * WorldGenConstants.SectorColumns) + coordinate.X;
            return countBySectorIndex.TryGetValue(index, out var count) ? count : 0;
        }

        private static ReadOnlyDictionary<TKey, int> Counts<TValue, TKey>(
            IEnumerable<TValue> values,
            Func<TValue, TKey> selector,
            IEnumerable<TKey> allKeys)
        {
            var result = new SortedDictionary<TKey, int>();
            foreach (var key in allKeys) result[key] = 0;
            foreach (var value in values) result[selector(value)]++;
            return new ReadOnlyDictionary<TKey, int>(result);
        }
    }

    public sealed class SectorFixedAnchorBuildResult
    {
        private readonly ReadOnlyCollection<SectorFixedAnchorError> errors;

        internal SectorFixedAnchorBuildResult(SectorFixedAnchorPlan plan, IEnumerable<SectorFixedAnchorError> sourceErrors)
        {
            var ordered = (sourceErrors ?? Array.Empty<SectorFixedAnchorError>()).Where(value => value != null)
                .Distinct().OrderBy(value => value).ToArray();
            errors = new ReadOnlyCollection<SectorFixedAnchorError>(ordered);
            Plan = ordered.Length == 0 ? plan : null;
            CanonicalDigest = Plan == null ? string.Empty : Plan.CanonicalDigest;
        }

        public bool Success => Plan != null && errors.Count == 0;
        public SectorFixedAnchorPlan Plan { get; }
        public string CanonicalDigest { get; }
        public IReadOnlyList<SectorFixedAnchorError> Errors => errors;
        public int MutationCount => 0;
        public int SolverInvocationCount => 0;
        public int RandomDrawCount => 0;
        public int TileWriteCount => 0;
    }

    public static class SectorFixedAnchorCanonicalDigest
    {
        public static string Compute(SectorFixedAnchorPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            return Compute(
                plan.PublicationLabel,
                plan.PlannerInputDigest,
                plan.AssignmentDigest,
                plan.Anchors,
                plan.CompatibleOverlapCount,
                plan.RouteIdentityBeforeDigest,
                plan.BoundaryIdentityBeforeDigest,
                plan.SiteIdentityBeforeDigest,
                plan.SpecialIdentityBeforeDigest);
        }

        internal static string Compute(
            string publicationLabel,
            string inputDigest,
            string assignmentDigest,
            IEnumerable<SectorFixedAnchor> anchors,
            int compatibleOverlapCount,
            string routeIdentityDigest,
            string boundaryIdentityDigest,
            string siteIdentityDigest,
            string specialIdentityDigest)
        {
            var material = new StringBuilder();
            SectorPlannerInputCanonicalDigest.Append(material, "PUBLICATION", publicationLabel);
            SectorPlannerInputCanonicalDigest.Append(material, "INPUT", inputDigest, assignmentDigest);
            SectorPlannerInputCanonicalDigest.Append(material, "IDENTITY", routeIdentityDigest,
                boundaryIdentityDigest, siteIdentityDigest, specialIdentityDigest);
            SectorPlannerInputCanonicalDigest.Append(material, "OVERLAP", compatibleOverlapCount);
            foreach (var anchor in anchors.OrderBy(value => value.SectorIndex)
                         .ThenByDescending(value => value.Priority)
                         .ThenBy(value => value.Kind)
                         .ThenBy(value => value.Rect)
                         .ThenBy(value => value.AnchorId, StringComparer.Ordinal))
            {
                SectorPlannerInputCanonicalDigest.Append(material, "ANCHOR", anchor.AnchorId,
                    anchor.SectorIndex, anchor.SectorCoordinate.X, anchor.SectorCoordinate.Y,
                    anchor.Kind, anchor.Source, anchor.Priority, anchor.Rect,
                    anchor.SourceId, anchor.SourceIdentity, anchor.Side,
                    anchor.AllowsCompatibleOverlap, anchor.CompatibilityGroup,
                    anchor.PlacedOwnershipClaim, anchor.ProgressionBlockerClaim);
            }

            return SectorPlannerInputCanonicalDigest.Hash(material.ToString());
        }

        internal static string ComputeAssignmentDigest(IEnumerable<SectorPacingAssignment> assignments)
        {
            var material = new StringBuilder();
            foreach (var assignment in assignments.OrderBy(value => value.Coordinate.Y).ThenBy(value => value.Coordinate.X))
            {
                SectorPlannerInputCanonicalDigest.Append(material, assignment.Coordinate.X,
                    assignment.Coordinate.Y, assignment.PrimaryRole, assignment.CanonicalDigest,
                    assignment.SourceIdentityDigest);
            }
            return SectorPlannerInputCanonicalDigest.Hash(material.ToString());
        }

        internal static string ComputeRouteIdentity(SectorPlannerInput input)
        {
            var material = new StringBuilder();
            foreach (var sector in input.Sectors.OrderBy(value => value.SectorIndex))
            {
                SectorPlannerInputCanonicalDigest.Append(material, sector.SectorIndex,
                    sector.Route.RouteType, sector.Route.AccessClass,
                    string.Join(",", sector.Route.ExternalSockets));
            }
            return SectorPlannerInputCanonicalDigest.Hash(material.ToString());
        }

        internal static string ComputeBoundaryIdentity(SectorPlannerInput input)
        {
            var material = new StringBuilder();
            foreach (var sector in input.Sectors.OrderBy(value => value.SectorIndex))
            foreach (var boundary in sector.Boundaries)
                SectorPlannerInputCanonicalDigest.Append(material, sector.SectorIndex, boundary.Side,
                    boundary.PairId, boundary.CandidateId, boundary.WarningCount);
            return SectorPlannerInputCanonicalDigest.Hash(material.ToString());
        }

        internal static string ComputeSiteIdentity(SectorPlannerInput input)
        {
            var material = new StringBuilder();
            foreach (var sector in input.Sectors.OrderBy(value => value.SectorIndex))
            foreach (var site in sector.Sites)
                SectorPlannerInputCanonicalDigest.Append(material, sector.SectorIndex, site.SiteId,
                    site.SiteKind, site.ReservationId, site.Mandatory);
            return SectorPlannerInputCanonicalDigest.Hash(material.ToString());
        }

        internal static string ComputeSpecialIdentity(SectorPlannerInput input)
        {
            var material = new StringBuilder();
            foreach (var sector in input.Sectors.OrderBy(value => value.SectorIndex))
            {
                var special = sector.SpecialRegion;
                SectorPlannerInputCanonicalDigest.Append(material, sector.SectorIndex, special.RegionId,
                    special.Kind, special.Binding, special.FootprintId, special.Reserved,
                    special.PlacedOwnershipClaim, special.MandatoryProgressionDependency);
                foreach (var optional in sector.OptionalRegions)
                    SectorPlannerInputCanonicalDigest.Append(material, sector.SectorIndex, optional.RegionId,
                        optional.Kind, optional.Available, optional.DeferredLocal, optional.PlacedOwnershipClaim);
            }
            return SectorPlannerInputCanonicalDigest.Hash(material.ToString());
        }
    }
}
