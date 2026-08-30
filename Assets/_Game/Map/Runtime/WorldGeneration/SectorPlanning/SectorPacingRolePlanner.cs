using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Pipeline;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class SectorPacingRolePlanner
    {
        public static IReadOnlyList<SectorPacingAssignment> Assign(SectorPlannerInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            return new ReadOnlyCollection<SectorPacingAssignment>(input.Sectors
                .OrderBy(value => value.SectorIndex)
                .Select(value => Assign(input, value))
                .ToArray());
        }

        public static SectorPacingAssignment Assign(SectorPlannerInput input, SectorCoord coordinate)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (!input.TryGetSector(coordinate, out var sector))
                throw new ArgumentOutOfRangeException(nameof(coordinate), "Coordinate is not present in the planner input.");
            return Assign(input, sector);
        }

        private static SectorPacingAssignment Assign(
            SectorPlannerInput input,
            SectorPlannerSectorSnapshot sector)
        {
            var byRole = new Dictionary<PacingRole, SectorPacingCandidate>();
            var reasons = new HashSet<SectorPacingReason>();

            if (sector.SpecialRegion.Kind == SectorPlannerSpecialRegionKind.Boss
                && sector.SpecialRegion.Binding == SectorPlannerSpecialRegionBinding.ReservedMandatory)
            {
                AddCandidate(sector, byRole, reasons, PacingRole.Boss, 100, SectorPacingReason.BossGate);
            }

            if (sector.SpecialRegion.Kind == SectorPlannerSpecialRegionKind.CoreResource
                || sector.Sites.Any(value => value.Mandatory
                                             && value.SiteKind.IndexOf("RESOURCE", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                AddCandidate(sector, byRole, reasons, PacingRole.Resource, 90, SectorPacingReason.MandatoryResource);
            }

            if (sector.SpecialRegion.Kind == SectorPlannerSpecialRegionKind.Forge
                && sector.SpecialRegion.Binding == SectorPlannerSpecialRegionBinding.ReservedMandatory)
            {
                AddCandidate(sector, byRole, reasons, PacingRole.Landmark, 80, SectorPacingReason.MandatoryLandmark);
                AddCandidate(sector, byRole, reasons, PacingRole.Machinery, 70, SectorPacingReason.ForgeMachineryCompatibility);
            }

            if (sector.SpecialRegion.Binding == SectorPlannerSpecialRegionBinding.ReservedMandatory
                && sector.SpecialRegion.Kind != SectorPlannerSpecialRegionKind.Boss
                && sector.SpecialRegion.Kind != SectorPlannerSpecialRegionKind.CoreResource
                && sector.SpecialRegion.Kind != SectorPlannerSpecialRegionKind.Forge)
            {
                AddCandidate(sector, byRole, reasons, PacingRole.Landmark, 80, SectorPacingReason.MandatoryLandmark);
            }

            if (sector.SpecialRegion.Kind == SectorPlannerSpecialRegionKind.Village
                && sector.SpecialRegion.Binding == SectorPlannerSpecialRegionBinding.ReferenceOnly)
            {
                AddCandidate(sector, byRole, reasons, PacingRole.Safe, 40, SectorPacingReason.VillageReference);
                AddCandidate(sector, byRole, reasons, PacingRole.Landmark, 40, SectorPacingReason.VillageReference);
            }

            if (sector.Boundaries.Any(value => value.WarningCount > 0))
            {
                AddCandidate(sector, byRole, reasons, PacingRole.Traversal, 60, SectorPacingReason.BoundaryWarning);
            }

            if (sector.Route.RecoveryNeeded || sector.Route.HighRoute)
            {
                AddCandidate(sector, byRole, reasons, PacingRole.Recovery, 50, SectorPacingReason.RouteRecoveryNeed);
            }

            if (sector.ActivityCatalogAvailable)
            {
                AddCandidate(sector, byRole, reasons, PacingRole.Activity, 20, SectorPacingReason.ActivityCatalogAvailable);
            }

            if (sector.EventCatalogAvailable)
            {
                reasons.Add(SectorPacingReason.EventCatalogAvailable);
            }

            if (sector.QuietCompatible)
            {
                AddCandidate(sector, byRole, reasons, PacingRole.Quiet, 10, SectorPacingReason.QuietBuffer);
            }

            if (sector.SpecialRegion.Binding == SectorPlannerSpecialRegionBinding.DeferredOptionalLocal
                || sector.OptionalRegions.Any(value => value.Available && value.DeferredLocal))
            {
                AddCandidate(sector, byRole, reasons, PacingRole.Discovery, 15, SectorPacingReason.DeferredOptionalRegion);
            }

            if (sector.Neighbors.Count > 0)
            {
                AddCandidate(sector, byRole, reasons, PacingRole.Traversal, 30, SectorPacingReason.NeighborPacingContext);
            }

            if (byRole.Count == 0)
            {
                AddCandidate(sector, byRole, reasons, PacingRole.Flow, 0, SectorPacingReason.FlowFallback);
            }

            var ordered = byRole.Values
                .OrderByDescending(value => value.HardPriorityClass)
                .ThenByDescending(value => value.WorldProgressSuitability)
                .ThenBy(value => value.LandmarkDistanceBucket)
                .ThenBy(value => value.Role)
                .ThenBy(value => sector.Coordinate.Y)
                .ThenBy(value => sector.Coordinate.X)
                .ToArray();
            var orderedReasons = reasons.OrderBy(value => value).ToArray();
            var sourceIdentityDigest = SectorPlannerInputCanonicalDigest.Hash(
                SectorPlannerInputCanonicalDigest.ComputeIdentity(sector) + "\n" + input.Authority.CanonicalDigest);
            var digestMaterial = new StringBuilder();
            SectorPlannerInputCanonicalDigest.Append(digestMaterial,
                sector.SectorIndex, sector.Coordinate.X, sector.Coordinate.Y, sourceIdentityDigest, ordered[0].Role);
            foreach (var candidate in ordered)
            {
                SectorPlannerInputCanonicalDigest.Append(digestMaterial,
                    candidate.Role,
                    candidate.HardPriorityClass,
                    candidate.WorldProgressSuitability,
                    candidate.LandmarkDistanceBucket,
                    candidate.Reason);
            }
            foreach (var reason in orderedReasons)
            {
                SectorPlannerInputCanonicalDigest.Append(digestMaterial, reason);
            }

            return new SectorPacingAssignment(
                sector.Coordinate,
                ordered[0].Role,
                ordered,
                orderedReasons,
                sourceIdentityDigest,
                SectorPlannerInputCanonicalDigest.Hash(digestMaterial.ToString()));
        }

        private static void AddCandidate(
            SectorPlannerSectorSnapshot sector,
            IDictionary<PacingRole, SectorPacingCandidate> byRole,
            ISet<SectorPacingReason> reasons,
            PacingRole role,
            int hardPriorityClass,
            SectorPacingReason reason)
        {
            reasons.Add(reason);
            if (!sector.CompatiblePacingRoles.Contains(role)) return;
            var distance = DistanceFor(role, sector.WorldProgress);
            var candidate = new SectorPacingCandidate(
                role,
                hardPriorityClass,
                Suitability(role, sector.WorldProgress.Ordinal),
                Bucket(distance),
                reason);
            if (!byRole.TryGetValue(role, out var current)
                || Compare(candidate, current) < 0)
            {
                byRole[role] = candidate;
            }
        }

        private static int Compare(SectorPacingCandidate left, SectorPacingCandidate right)
        {
            var comparison = right.HardPriorityClass.CompareTo(left.HardPriorityClass);
            if (comparison != 0) return comparison;
            comparison = right.WorldProgressSuitability.CompareTo(left.WorldProgressSuitability);
            if (comparison != 0) return comparison;
            comparison = left.LandmarkDistanceBucket.CompareTo(right.LandmarkDistanceBucket);
            if (comparison != 0) return comparison;
            return left.Role.CompareTo(right.Role);
        }

        private static int Suitability(PacingRole role, int ordinal)
        {
            switch (role)
            {
                case PacingRole.Safe:
                    return 100 - (Math.Abs(ordinal - 2) * 10);
                case PacingRole.Landmark:
                    return 100 - (Math.Abs(ordinal - 8) * 10);
                case PacingRole.Activity:
                    return 100 - (Math.Abs(ordinal - 5) * 5);
                case PacingRole.Quiet:
                    return 100 - (ordinal * 3);
                case PacingRole.Boss:
                case PacingRole.Resource:
                    return 70 + Math.Min(ordinal, 10);
                case PacingRole.Recovery:
                    return 90;
                case PacingRole.Traversal:
                    return 80;
                default:
                    return 60;
            }
        }

        private static int DistanceFor(PacingRole role, SectorPlannerWorldProgressSnapshot progress)
        {
            switch (role)
            {
                case PacingRole.Boss:
                case PacingRole.Resource:
                case PacingRole.Landmark:
                case PacingRole.Machinery:
                case PacingRole.Recovery:
                    return progress.NearestMandatoryLandmarkDistance;
                default:
                    return progress.NearestOptionalLandmarkDistance;
            }
        }

        private static SectorPlannerLandmarkDistanceBucket Bucket(int distance)
        {
            if (distance < 0) return SectorPlannerLandmarkDistanceBucket.Unknown;
            if (distance == 0) return SectorPlannerLandmarkDistanceBucket.SameSector;
            if (distance <= 2) return SectorPlannerLandmarkDistanceBucket.Near;
            if (distance <= 5) return SectorPlannerLandmarkDistanceBucket.Medium;
            return SectorPlannerLandmarkDistanceBucket.Far;
        }
    }
}
