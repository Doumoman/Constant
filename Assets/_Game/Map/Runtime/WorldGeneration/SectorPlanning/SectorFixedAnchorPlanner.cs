using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Pipeline;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class SectorFixedAnchorPlanner
    {
        public const string ReferencePublicationLabel = "REFERENCE ANCHOR PLAN";

        public static SectorFixedAnchorBuildResult Build(SectorFixedAnchorBuildRequest request)
        {
            var errors = new List<SectorFixedAnchorError>();
            if (request == null)
            {
                Add(errors, SectorFixedAnchorErrorCode.MissingInput, "request", "Fixed anchor build request is required.");
                return new SectorFixedAnchorBuildResult(null, errors);
            }

            ValidatePublication(request, errors);
            ValidateMutationClaims(request, errors);
            var inputValid = ValidateInput(request.Input, errors);
            if (!inputValid)
            {
                return new SectorFixedAnchorBuildResult(null, errors);
            }

            ValidateAssignments(request.Input, request.Assignments, errors);
            var anchors = BuildAnchors(request.Input, request.Projections, errors);
            ValidateCoverage(request.Input, anchors, errors);
            var compatibleOverlapCount = ValidateOverlaps(anchors, errors);
            if (errors.Count != 0)
            {
                return new SectorFixedAnchorBuildResult(null, errors);
            }

            var assignmentDigest = SectorFixedAnchorCanonicalDigest.ComputeAssignmentDigest(request.Assignments);
            var routeIdentity = SectorFixedAnchorCanonicalDigest.ComputeRouteIdentity(request.Input);
            var boundaryIdentity = SectorFixedAnchorCanonicalDigest.ComputeBoundaryIdentity(request.Input);
            var siteIdentity = SectorFixedAnchorCanonicalDigest.ComputeSiteIdentity(request.Input);
            var specialIdentity = SectorFixedAnchorCanonicalDigest.ComputeSpecialIdentity(request.Input);
            var canonicalDigest = SectorFixedAnchorCanonicalDigest.Compute(
                request.PublicationLabel,
                request.Input.CanonicalDigest,
                assignmentDigest,
                anchors,
                compatibleOverlapCount,
                routeIdentity,
                boundaryIdentity,
                siteIdentity,
                specialIdentity);
            if (request.ExpectedCanonicalDigest.Length != 0
                && !string.Equals(request.ExpectedCanonicalDigest, canonicalDigest, StringComparison.Ordinal))
            {
                Add(errors, SectorFixedAnchorErrorCode.NonCanonicalPublication, "expected-digest",
                    "Expected anchor plan digest does not match canonical publication.");
                return new SectorFixedAnchorBuildResult(null, errors);
            }

            var plan = new SectorFixedAnchorPlan(
                request.Input,
                anchors,
                request.PublicationLabel,
                compatibleOverlapCount,
                assignmentDigest,
                routeIdentity,
                boundaryIdentity,
                siteIdentity,
                specialIdentity,
                canonicalDigest);
            return new SectorFixedAnchorBuildResult(plan, errors);
        }

        private static void ValidatePublication(
            SectorFixedAnchorBuildRequest request,
            ICollection<SectorFixedAnchorError> errors)
        {
            if (!string.Equals(request.PublicationLabel, ReferencePublicationLabel, StringComparison.Ordinal))
            {
                Add(errors, SectorFixedAnchorErrorCode.NonCanonicalPublication, "publication",
                    "Focused fixtures must be labeled REFERENCE ANCHOR PLAN.");
            }
            if (request.ExpectedCanonicalDigest.Length != 0 && !IsLowerHex64(request.ExpectedCanonicalDigest))
            {
                Add(errors, SectorFixedAnchorErrorCode.NonCanonicalPublication, "expected-digest",
                    "Expected digest must be 64-character lowercase hexadecimal.");
            }
        }

        private static void ValidateMutationClaims(
            SectorFixedAnchorBuildRequest request,
            ICollection<SectorFixedAnchorError> errors)
        {
            if (request.RouteAccessMutationClaim)
                Add(errors, SectorFixedAnchorErrorCode.RouteAccessMutationClaim, "route-access",
                    "Anchor publication cannot mutate RouteType, AccessClass, or external sockets.");
            if (request.BoundaryMutationClaim)
                Add(errors, SectorFixedAnchorErrorCode.BoundaryMutationClaim, "boundary",
                    "Anchor publication cannot mutate boundary pair/candidate identity.");
            if (request.SiteMutationClaim)
                Add(errors, SectorFixedAnchorErrorCode.SiteMutationClaim, "site",
                    "Anchor publication cannot mutate site/reservation identity.");
            if (request.SpecialMutationClaim)
                Add(errors, SectorFixedAnchorErrorCode.SpecialMutationClaim, "special",
                    "Anchor publication cannot mutate SpecialRegion binding.");
            if (request.SolverMutationCount != 0
                || request.RandomDrawCount != 0
                || request.TileWriteCount != 0
                || request.CanvasMutationCount != 0
                || request.AssetMutationCount != 0)
            {
                Add(errors, SectorFixedAnchorErrorCode.SolverMutationClaim, "side-effects",
                    "Solver, RNG, tile, canvas, and asset mutation counts must all be zero.");
            }
        }

        private static bool ValidateInput(
            SectorPlannerInput input,
            ICollection<SectorFixedAnchorError> errors)
        {
            if (input == null)
            {
                Add(errors, SectorFixedAnchorErrorCode.MissingInput, "planner-input",
                    "A published SectorPlannerInput is required.");
                return false;
            }

            if (!string.Equals(input.PublicationLabel, SectorPlannerInputBuilder.ReferencePublicationLabel, StringComparison.Ordinal)
                || !IsLowerHex64(input.CanonicalDigest)
                || !string.Equals(input.CanonicalDigest, SectorPlannerInputCanonicalDigest.Compute(input), StringComparison.Ordinal))
            {
                Add(errors, SectorFixedAnchorErrorCode.NonCanonicalPublication, "planner-input",
                    "SectorPlannerInput publication label or canonical digest is invalid.");
                return false;
            }
            return true;
        }

        private static void ValidateAssignments(
            SectorPlannerInput input,
            IReadOnlyList<SectorPacingAssignment> assignments,
            ICollection<SectorFixedAnchorError> errors)
        {
            if (assignments.Count == 0)
            {
                Add(errors, SectorFixedAnchorErrorCode.MissingPacingAssignment, "assignments",
                    "Matching PacingRole assignments are required.");
                return;
            }
            if (assignments.Count != input.Sectors.Count)
            {
                Add(errors, SectorFixedAnchorErrorCode.MissingPacingAssignment, "assignments",
                    "Assignment count must equal planner input sector count.");
            }

            foreach (var duplicate in assignments.GroupBy(value => SectorIndex(value.Coordinate))
                         .Where(group => group.Count() > 1).OrderBy(group => group.Key))
            {
                Add(errors, SectorFixedAnchorErrorCode.SectorMismatch,
                    duplicate.Key.ToString("D3", CultureInfo.InvariantCulture),
                    "Pacing assignment coordinate is duplicated.");
            }

            var expected = SectorPacingRolePlanner.Assign(input)
                .ToDictionary(value => SectorIndex(value.Coordinate));
            foreach (var assignment in assignments)
            {
                var index = SectorIndex(assignment.Coordinate);
                if (!expected.TryGetValue(index, out var expectedAssignment)
                    || expectedAssignment.Coordinate.X != assignment.Coordinate.X
                    || expectedAssignment.Coordinate.Y != assignment.Coordinate.Y)
                {
                    Add(errors, SectorFixedAnchorErrorCode.SectorMismatch,
                        index.ToString("D3", CultureInfo.InvariantCulture),
                        "Pacing assignment coordinate is not present in planner input.");
                    continue;
                }
                if (!string.Equals(expectedAssignment.CanonicalDigest, assignment.CanonicalDigest, StringComparison.Ordinal)
                    || !string.Equals(expectedAssignment.SourceIdentityDigest, assignment.SourceIdentityDigest, StringComparison.Ordinal))
                {
                    Add(errors, SectorFixedAnchorErrorCode.SectorMismatch,
                        index.ToString("D3", CultureInfo.InvariantCulture),
                        "Pacing assignment identity does not match the current planner input.");
                }
            }

            foreach (var sector in input.Sectors)
            {
                if (assignments.Count(value => value.Coordinate.X == sector.Coordinate.X
                                               && value.Coordinate.Y == sector.Coordinate.Y) != 1)
                {
                    Add(errors, SectorFixedAnchorErrorCode.MissingPacingAssignment,
                        sector.SectorIndex.ToString("D3", CultureInfo.InvariantCulture),
                        "Each planner sector requires exactly one matching assignment.");
                }
            }
        }

        private static List<SectorFixedAnchor> BuildAnchors(
            SectorPlannerInput input,
            IReadOnlyList<SectorFixedAnchorProjection> projections,
            ICollection<SectorFixedAnchorError> errors)
        {
            foreach (var duplicate in projections.GroupBy(value => value.AnchorId, StringComparer.Ordinal)
                         .Where(group => group.Key.Length == 0 || group.Count() > 1)
                         .OrderBy(group => group.Key, StringComparer.Ordinal))
            {
                Add(errors, SectorFixedAnchorErrorCode.DuplicateAnchorId, duplicate.Key,
                    "Anchor IDs must be non-empty and globally unique.");
            }

            var anchors = new List<SectorFixedAnchor>();
            foreach (var projection in projections)
            {
                var subject = projection.AnchorId.Length == 0 ? "<empty>" : projection.AnchorId;
                if (!input.TryGetSector(projection.SectorCoordinate, out var sector))
                {
                    Add(errors, SectorFixedAnchorErrorCode.SectorMismatch, subject,
                        "Anchor sector coordinate is not present in planner input.");
                    continue;
                }
                if (projection.Rect == null
                    || !projection.Rect.IsInside(WorldGenConstants.SectorWidthTiles, WorldGenConstants.SectorHeightTiles))
                {
                    Add(errors, SectorFixedAnchorErrorCode.AnchorOutOfBounds, subject,
                        "Anchor rect must be positive and inside the 48x32 canvas.");
                }
                if (projection.Priority != ExpectedPriority(projection.Kind))
                {
                    Add(errors, SectorFixedAnchorErrorCode.PriorityViolation, subject,
                        "Anchor priority does not match its fixed kind priority.");
                }

                var sourceIdentity = ResolveSourceIdentity(sector, projection, errors);
                if (sourceIdentity.Length != 0)
                    anchors.Add(new SectorFixedAnchor(projection, sector.SectorIndex, sourceIdentity));
            }
            return anchors;
        }

        private static string ResolveSourceIdentity(
            SectorPlannerSectorSnapshot sector,
            SectorFixedAnchorProjection projection,
            ICollection<SectorFixedAnchorError> errors)
        {
            switch (projection.Kind)
            {
                case SectorFixedAnchorKind.ExternalRouteSocket:
                    return ResolveRouteIdentity(sector, projection, errors);
                case SectorFixedAnchorKind.BoundaryFixedSlice:
                case SectorFixedAnchorKind.BoundaryWarning:
                    return ResolveBoundaryIdentity(sector, projection, errors);
                case SectorFixedAnchorKind.SiteReservation:
                    return ResolveSiteIdentity(sector, projection, errors);
                case SectorFixedAnchorKind.SpecialFootprint:
                case SectorFixedAnchorKind.SpecialEntryReturn:
                case SectorFixedAnchorKind.SpecialApronBuffer:
                case SectorFixedAnchorKind.ReferenceOnlyMarker:
                    return ResolveSpecialIdentity(sector, projection, errors);
                default:
                    Add(errors, SectorFixedAnchorErrorCode.NonCanonicalPublication, projection.AnchorId,
                        "Unknown fixed anchor kind.");
                    return string.Empty;
            }
        }

        private static string ResolveRouteIdentity(
            SectorPlannerSectorSnapshot sector,
            SectorFixedAnchorProjection projection,
            ICollection<SectorFixedAnchorError> errors)
        {
            if (projection.Source != SectorFixedAnchorSource.RouteSnapshot
                || !projection.Side.HasValue
                || !sector.Route.ExternalSockets.Contains(projection.SourceId, StringComparer.Ordinal)
                || projection.Rect == null
                || !projection.Rect.TouchesOnly(projection.Side.Value,
                    WorldGenConstants.SectorWidthTiles, WorldGenConstants.SectorHeightTiles))
            {
                Add(errors, SectorFixedAnchorErrorCode.InvalidSideAnchor, projection.AnchorId,
                    "Route anchor must map one public socket to one matching side-only rect.");
                return string.Empty;
            }
            return string.Join("|", new[]
            {
                "ROUTE",
                sector.Route.RouteType.ToString(CultureInfo.InvariantCulture),
                sector.Route.AccessClass.ToString(),
                projection.Side.Value.ToString(),
                projection.SourceId,
            });
        }

        private static string ResolveBoundaryIdentity(
            SectorPlannerSectorSnapshot sector,
            SectorFixedAnchorProjection projection,
            ICollection<SectorFixedAnchorError> errors)
        {
            var boundary = sector.Boundaries.SingleOrDefault(value =>
                string.Equals(value.CandidateId, projection.SourceId, StringComparison.Ordinal));
            if (projection.Source != SectorFixedAnchorSource.BoundarySnapshot
                || boundary == null
                || !projection.Side.HasValue
                || projection.Side.Value != boundary.Side
                || projection.Rect == null
                || !projection.Rect.TouchesOnly(boundary.Side,
                    WorldGenConstants.SectorWidthTiles, WorldGenConstants.SectorHeightTiles)
                || projection.Kind == SectorFixedAnchorKind.BoundaryWarning && boundary.WarningCount == 0)
            {
                Add(errors, SectorFixedAnchorErrorCode.InvalidBoundaryAnchor, projection.AnchorId,
                    "Boundary anchor must preserve side, pair/candidate, warning, and side alignment.");
                return string.Empty;
            }
            return string.Join("|", new[]
            {
                "BOUNDARY", boundary.Side.ToString(), boundary.PairId, boundary.CandidateId,
                boundary.WarningCount.ToString(CultureInfo.InvariantCulture),
            });
        }

        private static string ResolveSiteIdentity(
            SectorPlannerSectorSnapshot sector,
            SectorFixedAnchorProjection projection,
            ICollection<SectorFixedAnchorError> errors)
        {
            var site = sector.Sites.SingleOrDefault(value =>
                string.Equals(value.SiteId, projection.SourceId, StringComparison.Ordinal));
            if (projection.Source != SectorFixedAnchorSource.SiteSnapshot
                || projection.Side.HasValue
                || site == null
                || projection.PlacedOwnershipClaim != site.Mandatory)
            {
                Add(errors, SectorFixedAnchorErrorCode.InvalidSpecialAnchor, projection.AnchorId,
                    "SiteReservation must preserve public site/reservation/mandatory identity.");
                return string.Empty;
            }
            return string.Join("|", new[]
            {
                "SITE", site.SiteId, site.SiteKind, site.ReservationId, site.Mandatory ? "1" : "0",
            });
        }

        private static string ResolveSpecialIdentity(
            SectorPlannerSectorSnapshot sector,
            SectorFixedAnchorProjection projection,
            ICollection<SectorFixedAnchorError> errors)
        {
            var special = sector.SpecialRegion;
            if (projection.Source == SectorFixedAnchorSource.OptionalRegionSnapshot
                || special.Binding == SectorPlannerSpecialRegionBinding.DeferredOptionalLocal)
            {
                Add(errors, SectorFixedAnchorErrorCode.DeferredPlacedClaim, projection.AnchorId,
                    "Deferred Merchant/Maru facts cannot publish placed anchors.");
                return string.Empty;
            }
            if (projection.Source != SectorFixedAnchorSource.SpecialRegionSnapshot
                || projection.Side.HasValue
                || projection.Rect == null
                || !string.Equals(projection.SourceId, special.RegionId, StringComparison.Ordinal))
            {
                Add(errors, SectorFixedAnchorErrorCode.InvalidSpecialAnchor, projection.AnchorId,
                    "Special anchor must preserve the public SpecialRegion identity.");
                return string.Empty;
            }

            if (special.Binding == SectorPlannerSpecialRegionBinding.ReferenceOnly)
            {
                if (projection.Kind != SectorFixedAnchorKind.ReferenceOnlyMarker
                    || projection.PlacedOwnershipClaim
                    || projection.ProgressionBlockerClaim)
                {
                    Add(errors, SectorFixedAnchorErrorCode.ReferenceLiveClaim, projection.AnchorId,
                        "Village reference marker cannot claim placement or progression ownership.");
                    return string.Empty;
                }
            }
            else if (special.Binding == SectorPlannerSpecialRegionBinding.ReservedMandatory)
            {
                if (projection.Kind == SectorFixedAnchorKind.ReferenceOnlyMarker
                    || !projection.PlacedOwnershipClaim
                    || projection.ProgressionBlockerClaim != special.MandatoryProgressionDependency)
                {
                    Add(errors, SectorFixedAnchorErrorCode.InvalidSpecialAnchor, projection.AnchorId,
                        "Reserved mandatory anchor claims must match the public SpecialRegion binding.");
                    return string.Empty;
                }
            }
            else
            {
                Add(errors, SectorFixedAnchorErrorCode.InvalidSpecialAnchor, projection.AnchorId,
                    "None/deferred SpecialRegion facts cannot publish special anchors.");
                return string.Empty;
            }

            return string.Join("|", new[]
            {
                "SPECIAL", special.RegionId, special.Kind.ToString(), special.Binding.ToString(),
                special.FootprintId, projection.Rect.ToString(), special.Reserved ? "1" : "0",
                special.PlacedOwnershipClaim ? "1" : "0",
                special.MandatoryProgressionDependency ? "1" : "0",
            });
        }

        private static void ValidateCoverage(
            SectorPlannerInput input,
            IReadOnlyList<SectorFixedAnchor> anchors,
            ICollection<SectorFixedAnchorError> errors)
        {
            foreach (var sector in input.Sectors)
            {
                var sectorAnchors = anchors.Where(value => value.SectorIndex == sector.SectorIndex).ToArray();
                foreach (var socket in sector.Route.ExternalSockets)
                {
                    if (sectorAnchors.Count(value => value.Kind == SectorFixedAnchorKind.ExternalRouteSocket
                                                     && string.Equals(value.SourceId, socket, StringComparison.Ordinal)) != 1)
                    {
                        Add(errors, SectorFixedAnchorErrorCode.InvalidSideAnchor,
                            SectorSubject(sector) + "/" + socket,
                            "Each public external socket requires exactly one side anchor.");
                    }
                }

                foreach (var boundary in sector.Boundaries)
                {
                    if (sectorAnchors.Count(value => value.Kind == SectorFixedAnchorKind.BoundaryFixedSlice
                                                     && value.SourceId == boundary.CandidateId) != 1)
                    {
                        Add(errors, SectorFixedAnchorErrorCode.InvalidBoundaryAnchor,
                            SectorSubject(sector) + "/" + boundary.CandidateId,
                            "Each boundary summary requires exactly one fixed-slice reference anchor.");
                    }
                    var expectedWarnings = boundary.WarningCount > 0 ? 1 : 0;
                    if (sectorAnchors.Count(value => value.Kind == SectorFixedAnchorKind.BoundaryWarning
                                                     && value.SourceId == boundary.CandidateId) != expectedWarnings)
                    {
                        Add(errors, SectorFixedAnchorErrorCode.InvalidBoundaryAnchor,
                            SectorSubject(sector) + "/" + boundary.CandidateId,
                            "Boundary warning anchor count must match public warning evidence.");
                    }
                }

                foreach (var site in sector.Sites)
                {
                    if (sectorAnchors.Count(value => value.Kind == SectorFixedAnchorKind.SiteReservation
                                                     && value.SourceId == site.SiteId) != 1)
                    {
                        Add(errors, SectorFixedAnchorErrorCode.InvalidSpecialAnchor,
                            SectorSubject(sector) + "/" + site.SiteId,
                            "Each public site requires exactly one reservation anchor.");
                    }
                }

                ValidateSpecialCoverage(sector, sectorAnchors, errors);
                foreach (var optional in sector.OptionalRegions)
                {
                    if (sectorAnchors.Any(value => value.SourceId == optional.RegionId))
                    {
                        Add(errors, SectorFixedAnchorErrorCode.DeferredPlacedClaim,
                            SectorSubject(sector) + "/" + optional.RegionId,
                            "Optional deferred facts must publish zero anchors.");
                    }
                }
            }
        }

        private static void ValidateSpecialCoverage(
            SectorPlannerSectorSnapshot sector,
            IReadOnlyList<SectorFixedAnchor> anchors,
            ICollection<SectorFixedAnchorError> errors)
        {
            var special = sector.SpecialRegion;
            var owned = anchors.Where(value => value.SourceId == special.RegionId
                                               && value.Source == SectorFixedAnchorSource.SpecialRegionSnapshot).ToArray();
            if (special.Binding == SectorPlannerSpecialRegionBinding.ReservedMandatory)
            {
                var supported = special.Kind == SectorPlannerSpecialRegionKind.CoreResource
                                || special.Kind == SectorPlannerSpecialRegionKind.Forge
                                || special.Kind == SectorPlannerSpecialRegionKind.Boss;
                var exact = owned.Count(value => value.Kind == SectorFixedAnchorKind.SpecialFootprint) == 1
                            && owned.Count(value => value.Kind == SectorFixedAnchorKind.SpecialEntryReturn) == 1
                            && owned.Count(value => value.Kind == SectorFixedAnchorKind.SpecialApronBuffer) == 1
                            && owned.Length == 3;
                if (!supported || !exact)
                {
                    Add(errors, SectorFixedAnchorErrorCode.InvalidSpecialAnchor, SectorSubject(sector),
                        "Core/Forge/Boss reserved facts require exact footprint, entry-return, and apron-buffer anchors.");
                }
            }
            else if (special.Binding == SectorPlannerSpecialRegionBinding.ReferenceOnly)
            {
                if (special.Kind != SectorPlannerSpecialRegionKind.Village
                    || owned.Length != 1
                    || owned[0].Kind != SectorFixedAnchorKind.ReferenceOnlyMarker
                    || owned[0].PlacedOwnershipClaim
                    || owned[0].ProgressionBlockerClaim)
                {
                    Add(errors, SectorFixedAnchorErrorCode.ReferenceLiveClaim, SectorSubject(sector),
                        "Village must publish exactly one non-live reference marker.");
                }
            }
            else if (special.Binding == SectorPlannerSpecialRegionBinding.DeferredOptionalLocal && owned.Length != 0)
            {
                Add(errors, SectorFixedAnchorErrorCode.DeferredPlacedClaim, SectorSubject(sector),
                    "Deferred SpecialRegion must publish zero anchors.");
            }
            else if (special.Binding == SectorPlannerSpecialRegionBinding.None && owned.Length != 0)
            {
                Add(errors, SectorFixedAnchorErrorCode.InvalidSpecialAnchor, SectorSubject(sector),
                    "A sector without SpecialRegion facts cannot publish special anchors.");
            }
        }

        private static int ValidateOverlaps(
            IReadOnlyList<SectorFixedAnchor> anchors,
            ICollection<SectorFixedAnchorError> errors)
        {
            var compatible = 0;
            for (var leftIndex = 0; leftIndex < anchors.Count; leftIndex++)
            for (var rightIndex = leftIndex + 1; rightIndex < anchors.Count; rightIndex++)
            {
                var left = anchors[leftIndex];
                var right = anchors[rightIndex];
                if (left.SectorIndex != right.SectorIndex || !left.Rect.Overlaps(right.Rect)) continue;
                var allowed = left.Rect.Equals(right.Rect)
                              && left.AllowsCompatibleOverlap
                              && right.AllowsCompatibleOverlap
                              && left.CompatibilityGroup.Length != 0
                              && string.Equals(left.CompatibilityGroup, right.CompatibilityGroup, StringComparison.Ordinal)
                              && string.Equals(left.SourceIdentity, right.SourceIdentity, StringComparison.Ordinal);
                if (allowed)
                {
                    compatible++;
                }
                else
                {
                    Add(errors, SectorFixedAnchorErrorCode.IncompatibleOverlap,
                        left.AnchorId + "/" + right.AnchorId,
                        "Overlapping anchors cannot be shifted, shrunk, carved, or deleted automatically.");
                }
            }
            return compatible;
        }

        private static SectorFixedAnchorPriority ExpectedPriority(SectorFixedAnchorKind kind)
        {
            switch (kind)
            {
                case SectorFixedAnchorKind.SpecialFootprint:
                case SectorFixedAnchorKind.SiteReservation:
                    return SectorFixedAnchorPriority.SpecialReservation;
                case SectorFixedAnchorKind.SpecialEntryReturn:
                case SectorFixedAnchorKind.SpecialApronBuffer:
                    return SectorFixedAnchorPriority.SpecialTransition;
                case SectorFixedAnchorKind.ExternalRouteSocket:
                    return SectorFixedAnchorPriority.ExternalRouteSocket;
                case SectorFixedAnchorKind.BoundaryFixedSlice:
                    return SectorFixedAnchorPriority.BoundaryFixedSlice;
                case SectorFixedAnchorKind.BoundaryWarning:
                    return SectorFixedAnchorPriority.BoundaryWarning;
                case SectorFixedAnchorKind.ReferenceOnlyMarker:
                    return SectorFixedAnchorPriority.ReferenceOnly;
                default:
                    return 0;
            }
        }

        private static int SectorIndex(SectorCoord coordinate)
            => (coordinate.Y * WorldGenConstants.SectorColumns) + coordinate.X;

        private static string SectorSubject(SectorPlannerSectorSnapshot sector)
            => sector.SectorIndex.ToString("D3", CultureInfo.InvariantCulture) + "@"
               + sector.Coordinate.X.ToString(CultureInfo.InvariantCulture) + ","
               + sector.Coordinate.Y.ToString(CultureInfo.InvariantCulture);

        private static bool IsLowerHex64(string value)
            => value != null && value.Length == 64
               && value.All(character => character >= '0' && character <= '9'
                                         || character >= 'a' && character <= 'f');

        private static void Add(
            ICollection<SectorFixedAnchorError> errors,
            SectorFixedAnchorErrorCode code,
            string subject,
            string detail)
        {
            var error = new SectorFixedAnchorError(code, subject, detail);
            if (!errors.Contains(error)) errors.Add(error);
        }
    }
}
