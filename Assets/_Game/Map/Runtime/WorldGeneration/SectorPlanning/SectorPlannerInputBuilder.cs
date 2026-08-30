using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Pipeline;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class SectorPlannerInputBuilder
    {
        public const string ReferencePublicationLabel = "REFERENCE PLANNER INPUT";

        public static SectorPlannerInputBuildResult Build(SectorPlannerInputRequest request)
        {
            var errors = new List<SectorPlannerInputError>();
            if (request == null)
            {
                Add(errors, SectorPlannerInputErrorCode.MissingInput, "request", "Sector planner input request is required.");
                return new SectorPlannerInputBuildResult(null, errors);
            }

            ValidatePublication(request, errors);
            ValidateAuthority(request.Authority, errors);
            ValidateMutationClaims(request, errors);

            var sectors = request.Sectors.ToArray();
            if (sectors.Length == 0)
            {
                Add(errors, SectorPlannerInputErrorCode.MissingInput, "sectors", "At least one sector snapshot is required.");
            }

            foreach (var duplicate in sectors.Where(value => value != null)
                         .GroupBy(value => value.SectorIndex)
                         .Where(group => group.Count() > 1)
                         .OrderBy(group => group.Key))
            {
                Add(errors, SectorPlannerInputErrorCode.DuplicateSector,
                    duplicate.Key.ToString(), "Sector index is published more than once.");
            }

            foreach (var sector in sectors)
            {
                ValidateSector(sector, errors);
            }

            var canonicalDigest = errors.Count == 0
                ? SectorPlannerInputCanonicalDigest.Compute(sectors, request.Authority, request.PublicationLabel)
                : string.Empty;
            if (errors.Count == 0
                && request.ExpectedCanonicalDigest.Length != 0
                && !string.Equals(request.ExpectedCanonicalDigest, canonicalDigest, StringComparison.Ordinal))
            {
                Add(errors, SectorPlannerInputErrorCode.DigestMismatch, "input",
                    "Expected canonical digest does not match the immutable publication.");
            }

            if (errors.Count != 0)
            {
                return new SectorPlannerInputBuildResult(null, errors);
            }

            var input = new SectorPlannerInput(sectors, request.Authority, request.PublicationLabel, canonicalDigest);
            return new SectorPlannerInputBuildResult(input, errors);
        }

        private static void ValidatePublication(
            SectorPlannerInputRequest request,
            ICollection<SectorPlannerInputError> errors)
        {
            if (!string.Equals(request.PublicationLabel, ReferencePublicationLabel, StringComparison.Ordinal))
            {
                Add(errors, SectorPlannerInputErrorCode.NonCanonicalPublication, "publication",
                    "Focused fixtures must be labeled REFERENCE PLANNER INPUT.");
            }

            if (request.ExpectedCanonicalDigest.Length != 0 && !IsLowerHex64(request.ExpectedCanonicalDigest))
            {
                Add(errors, SectorPlannerInputErrorCode.DigestMismatch, "expected-digest",
                    "Expected digest must be 64-character lowercase hexadecimal.");
            }
        }

        private static void ValidateAuthority(
            SectorPlannerAuthorityDigestSnapshot authority,
            ICollection<SectorPlannerInputError> errors)
        {
            if (authority == null)
            {
                Add(errors, SectorPlannerInputErrorCode.MissingAuthorityDigest, "authority",
                    "Authority digest bundle is required.");
                return;
            }

            var names = new[]
            {
                "MAP00_08", "MAP09", "MAP10", "MAP11", "MAP12_ACTIVITY",
                "MAP12_EVENT", "MAP13_AUDIT", "MAP13_RESOURCE", "MAP13_LANDMARK",
            };
            var values = authority.EnumerateDigests().ToArray();
            for (var index = 0; index < values.Length; index++)
            {
                if (string.IsNullOrEmpty(values[index]))
                {
                    Add(errors, SectorPlannerInputErrorCode.MissingAuthorityDigest, names[index],
                        "Required public authority digest is missing.");
                }
                else if (!IsLowerHex64(values[index]))
                {
                    Add(errors, SectorPlannerInputErrorCode.DigestMismatch, names[index],
                        "Authority digest must be 64-character lowercase hexadecimal.");
                }
            }

            if (authority.MicroPatternCount < 1
                || authority.TerrainClusterCount < 1
                || authority.ActivityCount < 1
                || authority.EventCount < 1
                || authority.CoreResourceCount < 1
                || authority.SpecialLandmarkCount < 1)
            {
                Add(errors, SectorPlannerInputErrorCode.MissingAuthorityDigest, "authority-counts",
                    "Each published catalog summary must contain at least one entry.");
            }
        }

        private static void ValidateMutationClaims(
            SectorPlannerInputRequest request,
            ICollection<SectorPlannerInputError> errors)
        {
            if (request.CsvReparseCount != 0
                || request.GeneratedWriteCount != 0
                || request.SceneMutationCount != 0
                || request.AssetMutationCount != 0
                || request.SolverInvocationCount != 0
                || request.RandomDrawCount != 0)
            {
                Add(errors, SectorPlannerInputErrorCode.MutationClaim, "request-claims",
                    "CSV reparsing, writes, scene/asset mutation, solver calls, and random draws must all be zero.");
            }

            if (request.PacingChangesAccess)
            {
                Add(errors, SectorPlannerInputErrorCode.PacingAccessCoupling, "pacing-access",
                    "PacingRole is compatibility evidence and cannot change AccessClass.");
            }

            if (request.PacingChangesRoute)
            {
                Add(errors, SectorPlannerInputErrorCode.PacingRouteMutationClaim, "pacing-route",
                    "PacingRole cannot change RouteType, sockets, or route ownership.");
            }
        }

        private static void ValidateSector(
            SectorPlannerSectorSnapshot sector,
            ICollection<SectorPlannerInputError> errors)
        {
            if (sector == null)
            {
                Add(errors, SectorPlannerInputErrorCode.MissingInput, "sector", "Sector snapshot cannot be null.");
                return;
            }

            var key = SectorKey(sector);
            var coordinate = sector.Coordinate;
            var inRange = coordinate.X >= 0
                          && coordinate.X < WorldGenConstants.SectorColumns
                          && coordinate.Y >= 0
                          && coordinate.Y < WorldGenConstants.SectorRows;
            var expectedIndex = (coordinate.Y * WorldGenConstants.SectorColumns) + coordinate.X;
            if (!inRange || sector.SectorIndex != expectedIndex)
            {
                Add(errors, SectorPlannerInputErrorCode.SectorOutOfRange, key,
                    "Coordinate must be in the 13x13 world and index must equal y*13+x.");
            }

            if (sector.CanvasWidth != WorldGenConstants.SectorWidthTiles
                || sector.CanvasHeight != WorldGenConstants.SectorHeightTiles)
            {
                Add(errors, SectorPlannerInputErrorCode.SectorOutOfRange, key,
                    "Planner canvas must preserve the 48x32 sector constants.");
            }

            if (sector.Biome == null
                || string.IsNullOrWhiteSpace(sector.Biome.PatchId)
                || string.IsNullOrWhiteSpace(sector.Biome.BiomeId))
            {
                Add(errors, SectorPlannerInputErrorCode.InvalidBiomePatch, key,
                    "Biome patch and biome identity are required.");
            }

            ValidateRoute(sector.Route, key, errors);
            ValidateBoundaries(sector.Boundaries, key, errors);
            ValidateSites(sector.Sites, key, errors);
            ValidateSpecial(sector.SpecialRegion, key, errors);
            ValidateOptional(sector.OptionalRegions, key, errors);
            ValidateNeighbors(sector.Neighbors, key, errors);
            ValidateProgress(sector, key, errors);

            if (sector.CompatiblePacingRoles.Count == 0
                || sector.CompatiblePacingRoles.Any(value => !PacingRoleTokenCodec.IsPublished(value)))
            {
                Add(errors, SectorPlannerInputErrorCode.PacingRoleUndefined, key,
                    "At least one published MAP09 PacingRole compatibility value is required.");
            }

            ValidateRequiredRoleCompatibility(sector, key, errors);
        }

        private static void ValidateRequiredRoleCompatibility(
            SectorPlannerSectorSnapshot sector,
            string key,
            ICollection<SectorPlannerInputError> errors)
        {
            var required = new HashSet<PacingRole>();
            if (sector.SpecialRegion.Kind == SectorPlannerSpecialRegionKind.Boss
                && sector.SpecialRegion.Binding == SectorPlannerSpecialRegionBinding.ReservedMandatory)
                required.Add(PacingRole.Boss);
            if (sector.SpecialRegion.Kind == SectorPlannerSpecialRegionKind.CoreResource
                || sector.Sites.Any(value => value.Mandatory
                                             && value.SiteKind.IndexOf("RESOURCE", StringComparison.OrdinalIgnoreCase) >= 0))
                required.Add(PacingRole.Resource);
            if (sector.SpecialRegion.Kind == SectorPlannerSpecialRegionKind.Forge
                && sector.SpecialRegion.Binding == SectorPlannerSpecialRegionBinding.ReservedMandatory)
                required.Add(PacingRole.Landmark);
            if (sector.Boundaries.Any(value => value.WarningCount > 0)) required.Add(PacingRole.Traversal);
            if (sector.Route != null && (sector.Route.HighRoute || sector.Route.RecoveryNeeded)) required.Add(PacingRole.Recovery);
            if (sector.QuietCompatible) required.Add(PacingRole.Quiet);
            if (sector.Neighbors.Count > 0) required.Add(PacingRole.Traversal);
            if ((sector.ActivityCatalogAvailable || sector.EventCatalogAvailable)
                && sector.SpecialRegion.Binding != SectorPlannerSpecialRegionBinding.ReservedMandatory
                && !sector.Sites.Any(value => value.Mandatory))
                required.Add(PacingRole.Activity);
            if (sector.SpecialRegion.Binding == SectorPlannerSpecialRegionBinding.DeferredOptionalLocal
                || sector.OptionalRegions.Any(value => value.Available && value.DeferredLocal))
                required.Add(PacingRole.Discovery);

            foreach (var role in required.Where(value => !sector.CompatiblePacingRoles.Contains(value)).OrderBy(value => value))
            {
                Add(errors, SectorPlannerInputErrorCode.PacingRoleUndefined, key,
                    "Required pacing compatibility is missing: " + PacingRoleTokenCodec.ToToken(role) + ".");
            }

            if (sector.SpecialRegion.Binding == SectorPlannerSpecialRegionBinding.ReferenceOnly
                && sector.SpecialRegion.Kind == SectorPlannerSpecialRegionKind.Village
                && !sector.CompatiblePacingRoles.Contains(PacingRole.Safe)
                && !sector.CompatiblePacingRoles.Contains(PacingRole.Landmark))
            {
                Add(errors, SectorPlannerInputErrorCode.PacingRoleUndefined, key,
                    "Village reference requires Safe or Landmark compatibility without progression ownership.");
            }
        }

        private static void ValidateRoute(
            SectorPlannerRouteSnapshot route,
            string key,
            ICollection<SectorPlannerInputError> errors)
        {
            if (route == null
                || route.RouteType < 0
                || !AccessClassTokenCodec.IsPublished(route.AccessClass)
                || route.ExternalSockets.Any(string.IsNullOrWhiteSpace)
                || route.ExternalSockets.Distinct(StringComparer.Ordinal).Count() != route.ExternalSockets.Count)
            {
                Add(errors, SectorPlannerInputErrorCode.InvalidRouteSnapshot, key,
                    "RouteType, published AccessClass, and unique non-empty external sockets are required.");
            }
        }

        private static void ValidateBoundaries(
            IReadOnlyList<SectorPlannerBoundarySnapshot> boundaries,
            string key,
            ICollection<SectorPlannerInputError> errors)
        {
            if (boundaries.Any(value => !Enum.IsDefined(typeof(SectorPlannerSide), value.Side)
                                        || string.IsNullOrWhiteSpace(value.PairId)
                                        || string.IsNullOrWhiteSpace(value.CandidateId)
                                        || value.WarningCount < 0)
                || boundaries.GroupBy(value => value.Side).Any(group => group.Count() > 1))
            {
                Add(errors, SectorPlannerInputErrorCode.InvalidBoundarySnapshot, key,
                    "Boundary sides must be unique and preserve pair/candidate identity with non-negative warnings.");
            }
        }

        private static void ValidateSites(
            IReadOnlyList<SectorPlannerSiteSnapshot> sites,
            string key,
            ICollection<SectorPlannerInputError> errors)
        {
            if (sites.Any(value => string.IsNullOrWhiteSpace(value.SiteId)
                                   || string.IsNullOrWhiteSpace(value.SiteKind)
                                   || string.IsNullOrWhiteSpace(value.ReservationId))
                || sites.GroupBy(value => value.SiteId, StringComparer.Ordinal).Any(group => group.Count() > 1)
                || sites.GroupBy(value => value.ReservationId, StringComparer.Ordinal).Any(group => group.Count() > 1))
            {
                Add(errors, SectorPlannerInputErrorCode.InvalidSiteSnapshot, key,
                    "Site and reservation identities must be non-empty and unique.");
            }
        }

        private static void ValidateSpecial(
            SectorPlannerSpecialRegionSnapshot special,
            string key,
            ICollection<SectorPlannerInputError> errors)
        {
            if (special == null)
            {
                Add(errors, SectorPlannerInputErrorCode.InvalidSpecialRegionSnapshot, key,
                    "SpecialRegion snapshot is required, including an explicit None value.");
                return;
            }

            var validNone = special.Kind == SectorPlannerSpecialRegionKind.None
                            && special.Binding == SectorPlannerSpecialRegionBinding.None
                            && special.RegionId.Length == 0
                            && special.FootprintId.Length == 0
                            && !special.Reserved
                            && !special.PlacedOwnershipClaim
                            && !special.MandatoryProgressionDependency;
            var validReference = special.Binding == SectorPlannerSpecialRegionBinding.ReferenceOnly
                                 && special.Kind == SectorPlannerSpecialRegionKind.Village
                                 && special.RegionId.Length != 0
                                 && !special.Reserved
                                 && !special.PlacedOwnershipClaim
                                 && !special.MandatoryProgressionDependency;
            var validMandatory = special.Binding == SectorPlannerSpecialRegionBinding.ReservedMandatory
                                 && special.Kind != SectorPlannerSpecialRegionKind.None
                                 && special.Kind != SectorPlannerSpecialRegionKind.Merchant
                                 && special.Kind != SectorPlannerSpecialRegionKind.Maru
                                 && special.RegionId.Length != 0
                                 && special.FootprintId.Length != 0
                                 && special.Reserved
                                 && special.PlacedOwnershipClaim;
            var validDeferred = special.Binding == SectorPlannerSpecialRegionBinding.DeferredOptionalLocal
                                && (special.Kind == SectorPlannerSpecialRegionKind.Merchant
                                    || special.Kind == SectorPlannerSpecialRegionKind.Maru)
                                && special.RegionId.Length != 0
                                && special.FootprintId.Length == 0
                                && !special.Reserved
                                && !special.PlacedOwnershipClaim
                                && !special.MandatoryProgressionDependency;
            if (!validNone && !validReference && !validMandatory && !validDeferred)
            {
                Add(errors, SectorPlannerInputErrorCode.InvalidSpecialRegionSnapshot, key,
                    "SpecialRegion binding facts are inconsistent with none/reference/reserved/deferred ownership.");
            }
        }

        private static void ValidateOptional(
            IReadOnlyList<SectorPlannerOptionalRegionSnapshot> optionalRegions,
            string key,
            ICollection<SectorPlannerInputError> errors)
        {
            if (optionalRegions.Any(value => string.IsNullOrWhiteSpace(value.RegionId)
                                             || (value.Kind != SectorPlannerSpecialRegionKind.Merchant
                                                 && value.Kind != SectorPlannerSpecialRegionKind.Maru)
                                             || value.PlacedOwnershipClaim
                                             || (value.Available && !value.DeferredLocal))
                || optionalRegions.GroupBy(value => value.RegionId, StringComparer.Ordinal).Any(group => group.Count() > 1))
            {
                Add(errors, SectorPlannerInputErrorCode.InvalidOptionalRegionSnapshot, key,
                    "Optional Merchant/Maru availability is deferred-local only and cannot claim placement.");
            }
        }

        private static void ValidateNeighbors(
            IReadOnlyList<SectorPlannerNeighborSnapshot> neighbors,
            string key,
            ICollection<SectorPlannerInputError> errors)
        {
            if (neighbors.GroupBy(value => value.Side).Any(group => group.Count() > 1)
                || neighbors.Any(value => !Enum.IsDefined(typeof(SectorPlannerSide), value.Side)
                                          || value.Coordinate.X < 0
                                          || value.Coordinate.X >= WorldGenConstants.SectorColumns
                                          || value.Coordinate.Y < 0
                                          || value.Coordinate.Y >= WorldGenConstants.SectorRows
                                          || value.RouteType < 0
                                          || !AccessClassTokenCodec.IsPublished(value.AccessClass)
                                          || !PacingRoleTokenCodec.IsPublished(value.PrimaryRole)
                                          || value.ExternalSockets.Any(string.IsNullOrWhiteSpace)))
            {
                Add(errors, SectorPlannerInputErrorCode.InvalidNeighborSnapshot, key,
                    "Neighbor sides, coordinates, route/access, role, and sockets must be public valid values.");
            }
        }

        private static void ValidateProgress(
            SectorPlannerSectorSnapshot sector,
            string key,
            ICollection<SectorPlannerInputError> errors)
        {
            var progress = sector.WorldProgress;
            if (progress == null
                || progress.Ordinal < 0
                || string.IsNullOrWhiteSpace(progress.ChapterBucket)
                || string.IsNullOrWhiteSpace(progress.BranchBucket))
            {
                Add(errors, SectorPlannerInputErrorCode.WorldProgressInvalid, key,
                    "Non-negative world progress ordinal and chapter/branch buckets are required.");
                return;
            }

            if (progress.NearestMandatoryLandmarkDistance < 0
                || progress.NearestOptionalLandmarkDistance < -1
                || (sector.OptionalRegions.Any(value => value.Available)
                    && progress.NearestOptionalLandmarkDistance < 0)
                || (sector.SpecialRegion.Binding == SectorPlannerSpecialRegionBinding.ReservedMandatory
                    && progress.NearestMandatoryLandmarkDistance != 0))
            {
                Add(errors, SectorPlannerInputErrorCode.LandmarkDistanceInvalid, key,
                    "Mandatory distance is required, optional unknown is -1 only, and a reserved local landmark has distance zero.");
            }
        }

        private static bool IsLowerHex64(string value)
        {
            return value != null
                   && value.Length == 64
                   && value.All(character => character >= '0' && character <= '9'
                                             || character >= 'a' && character <= 'f');
        }

        private static string SectorKey(SectorPlannerSectorSnapshot sector)
        {
            return sector.SectorIndex.ToString("D3", CultureInfo.InvariantCulture) + "@"
                   + sector.Coordinate.X.ToString(CultureInfo.InvariantCulture) + ","
                   + sector.Coordinate.Y.ToString(CultureInfo.InvariantCulture);
        }

        private static void Add(
            ICollection<SectorPlannerInputError> errors,
            SectorPlannerInputErrorCode code,
            string subject,
            string detail)
        {
            var error = new SectorPlannerInputError(code, subject, detail);
            if (!errors.Contains(error)) errors.Add(error);
        }
    }
}
