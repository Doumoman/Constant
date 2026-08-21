using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class UpDownConflictResolver
    {
        public UpDownConflictBuildResult Build(
            VerticalGatewayPlan verticalGatewayPlan,
            MandatoryRouteMaskLookup routeMaskLookup,
            SiteReservationSnapshot siteSnapshot,
            BiomePatchValidationPublication biomePublication)
        {
            return BuildCore(verticalGatewayPlan, routeMaskLookup, siteSnapshot, biomePublication, null);
        }

        public UpDownConflictBuildResult Build(
            VerticalGatewayPlan verticalGatewayPlan,
            MandatoryRouteMaskLookup routeMaskLookup,
            SiteReservationSnapshot siteSnapshot,
            BiomePatchValidationPublication biomePublication,
            IEnumerable<UpDownConflictCandidate> syntheticCandidates)
        {
            if (syntheticCandidates == null) throw new ArgumentNullException(nameof(syntheticCandidates));
            return BuildCore(verticalGatewayPlan, routeMaskLookup, siteSnapshot, biomePublication, syntheticCandidates);
        }

        private static UpDownConflictBuildResult BuildCore(
            VerticalGatewayPlan verticalGatewayPlan,
            MandatoryRouteMaskLookup routeMaskLookup,
            SiteReservationSnapshot siteSnapshot,
            BiomePatchValidationPublication biomePublication,
            IEnumerable<UpDownConflictCandidate> syntheticCandidates)
        {
            var errors = ValidateSources(verticalGatewayPlan, routeMaskLookup, siteSnapshot, biomePublication);
            if (errors.Count != 0) return Invalid(errors);

            var candidates = syntheticCandidates == null
                ? CreateStarterCandidates(verticalGatewayPlan, siteSnapshot)
                : new List<UpDownConflictCandidate>(syntheticCandidates);
            candidates.Sort((left, right) => CompareCandidates(left, right));
            var seen = new HashSet<UpDownConflictId>();
            foreach (var candidate in candidates)
            {
                if (candidate == null)
                {
                    errors.Add(new UpDownConflictBuildError(UpDownConflictBuildErrorCode.InvalidCandidate, string.Empty, "Candidates cannot contain null."));
                    continue;
                }
                if (!seen.Add(candidate.ConflictId))
                    errors.Add(new UpDownConflictBuildError(UpDownConflictBuildErrorCode.DuplicateConflictId, candidate.ConflictId.Value, "Conflict IDs must be unique."));
            }
            if (errors.Count != 0) return Invalid(errors);

            var resolutions = new List<UpDownConflictResolution>();
            var evaluations = 0;
            foreach (var candidate in candidates)
            {
                if (!candidate.IsConflict) continue;
                var options = new List<ResolutionOption>();
                EvaluateAdjacent(candidate, candidate.Coordinate.X - 1, siteSnapshot, options, ref evaluations);
                EvaluateAdjacent(candidate, candidate.Coordinate.X + 1, siteSnapshot, options, ref evaluations);
                options.Sort(ResolutionOption.Compare);
                if (options.Count != 0) resolutions.Add(options[0].Resolution);
            }

            var plan = new UpDownConflictResolutionPlan(
                verticalGatewayPlan, routeMaskLookup, siteSnapshot, biomePublication, candidates, resolutions);
            var diagnostics = new UpDownConflictDiagnostics(
                verticalGatewayPlan.GatewayPairCount,
                plan.CandidateCount,
                plan.Type4ExpressibleCount,
                plan.ConflictCount,
                plan.ResolvedCount,
                plan.UnresolvedCount,
                evaluations,
                candidates.Count(value => value.OpensLeft),
                candidates.Count(value => value.OpensRight));
            return new UpDownConflictBuildResult(UpDownConflictBuildStatus.Completed, plan, diagnostics, Array.Empty<UpDownConflictBuildError>());
        }

        private static List<UpDownConflictBuildError> ValidateSources(
            VerticalGatewayPlan plan,
            MandatoryRouteMaskLookup lookup,
            SiteReservationSnapshot site,
            BiomePatchValidationPublication biome)
        {
            var errors = new List<UpDownConflictBuildError>();
            if (plan == null) errors.Add(new UpDownConflictBuildError(UpDownConflictBuildErrorCode.MissingVerticalGatewayPlan, string.Empty, "Vertical gateway plan is required."));
            if (lookup == null) errors.Add(new UpDownConflictBuildError(UpDownConflictBuildErrorCode.MissingRouteMaskLookup, string.Empty, "Route mask lookup is required."));
            if (site == null) errors.Add(new UpDownConflictBuildError(UpDownConflictBuildErrorCode.MissingSiteSnapshot, string.Empty, "Site snapshot is required."));
            if (biome == null) errors.Add(new UpDownConflictBuildError(UpDownConflictBuildErrorCode.MissingBiomePublication, string.Empty, "Biome publication is required."));
            if (plan != null && lookup != null && site != null && biome != null &&
                (!ReferenceEquals(plan.SourceRouteMaskLookup, lookup) || !ReferenceEquals(plan.SourceSiteSnapshot, site) || !ReferenceEquals(plan.SourceBiomePublication, biome)))
                errors.Add(new UpDownConflictBuildError(UpDownConflictBuildErrorCode.SourceIdentityMismatch, string.Empty, "Input artifacts must be the exact source identities of the vertical gateway plan."));
            return errors;
        }

        private static List<UpDownConflictCandidate> CreateStarterCandidates(VerticalGatewayPlan plan, SiteReservationSnapshot site)
        {
            var values = new List<UpDownConflictCandidate>();
            var ordinal = 0;
            foreach (var pair in plan.GatewayPairs)
            {
                foreach (var junction in pair.Type4JunctionCells)
                {
                    var sector = site.GetSector(junction.Coord);
                    values.Add(new UpDownConflictCandidate(
                        new UpDownConflictId("UDC_" + ordinal.ToString("D2", CultureInfo.InvariantCulture) + "_" + pair.GatewayId.Value.Substring(7)),
                        pair.GatewayId,
                        junction.Coord,
                        junction.OpensUp,
                        junction.OpensDown,
                        junction.OpensLeft,
                        junction.OpensRight,
                        string.IsNullOrEmpty(sector.LocalRole),
                        sector.IsReserved,
                        sector.ReservationId.ToString(),
                        "BIOME_SECTOR_" + WorldGridIndex.ToIndex(junction.Coord).ToString("D3", CultureInfo.InvariantCulture),
                        StepCost(sector)));
                    ordinal++;
                }
            }
            return values;
        }

        private static void EvaluateAdjacent(
            UpDownConflictCandidate candidate,
            int adjacentX,
            SiteReservationSnapshot site,
            ICollection<ResolutionOption> options,
            ref int evaluations)
        {
            evaluations++;
            var upperY = candidate.Coordinate.Y + 1;
            var lowerY = candidate.Coordinate.Y - 1;
            if (adjacentX < 0 || adjacentX >= WorldGenConstants.SectorColumns || lowerY < 0 || upperY >= WorldGenConstants.SectorRows) return;
            var upperCoord = new SectorCoord(adjacentX, upperY);
            var middleCoord = new SectorCoord(adjacentX, candidate.Coordinate.Y);
            var lowerCoord = new SectorCoord(adjacentX, lowerY);
            var upperSector = site.GetSector(upperCoord);
            var middleSector = site.GetSector(middleCoord);
            var lowerSector = site.GetSector(lowerCoord);
            if (upperSector.IsReserved || middleSector.IsReserved || lowerSector.IsReserved) return;
            var upperCost = StepCost(upperSector);
            var middleCost = StepCost(middleSector);
            var lowerCost = StepCost(lowerSector);
            var totalCost = checked(checked(upperCost + middleCost) + lowerCost);
            var upper = new VerticalGatewayAnchor(upperCoord, true, true, false, true, false, upperCost);
            var lower = new VerticalGatewayAnchor(lowerCoord, false, false, true, true, false, lowerCost);
            var resolution = new UpDownConflictResolution(
                candidate.ConflictId,
                candidate.SourceGatewayId,
                upper,
                lower,
                new[] { upperCoord, middleCoord, lowerCoord },
                totalCost,
                "TYPE4_FORBIDDEN_ADJACENT_GATEWAY");
            options.Add(new ResolutionOption(resolution, adjacentX));
        }

        private static int CompareCandidates(UpDownConflictCandidate left, UpDownConflictCandidate right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            var id = left.ConflictId.CompareTo(right.ConflictId);
            if (id != 0) return id;
            var source = left.SourceGatewayId.CompareTo(right.SourceGatewayId);
            if (source != 0) return source;
            var y = left.Coordinate.Y.CompareTo(right.Coordinate.Y);
            return y != 0 ? y : left.Coordinate.X.CompareTo(right.Coordinate.X);
        }

        private static int StepCost(SectorReservation sector) => sector.IsReserved ? 8 : 1;

        private static UpDownConflictBuildResult Invalid(IEnumerable<UpDownConflictBuildError> errors) =>
            new UpDownConflictBuildResult(UpDownConflictBuildStatus.InvalidInput, null, null, errors);

        private sealed class ResolutionOption
        {
            public ResolutionOption(UpDownConflictResolution resolution, int adjacentX)
            {
                Resolution = resolution;
                AdjacentX = adjacentX;
            }

            public UpDownConflictResolution Resolution { get; }
            public int AdjacentX { get; }

            public static int Compare(ResolutionOption left, ResolutionOption right)
            {
                var cost = left.Resolution.CheckedCost.CompareTo(right.Resolution.CheckedCost);
                if (cost != 0) return cost;
                var span = left.Resolution.VerticalDistance.CompareTo(right.Resolution.VerticalDistance);
                if (span != 0) return span;
                var x = left.AdjacentX.CompareTo(right.AdjacentX);
                if (x != 0) return x;
                return left.Resolution.SourceGatewayId.CompareTo(right.Resolution.SourceGatewayId);
            }
        }
    }
}
