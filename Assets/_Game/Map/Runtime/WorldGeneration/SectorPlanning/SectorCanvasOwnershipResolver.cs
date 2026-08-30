using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class SectorCanvasOwnershipResolver
    {
        public static SectorCanvasOwnershipBuildResult Resolve(
            SectorCanvasOwnershipBuildResult claimBuildResult)
        {
            var errors = new List<SectorCanvasOwnershipError>();
            if (claimBuildResult == null || !claimBuildResult.ClaimsReady || claimBuildResult.Request == null)
            {
                Add(errors, SectorCanvasOwnershipErrorCode.MissingInput,
                    "claimBuildResult", "A successful claim-build result is required.");
                return Failure(claimBuildResult == null ? null : claimBuildResult.Request, errors);
            }

            var request = claimBuildResult.Request;
            foreach (var fault in request.ReferenceFaults)
                Add(errors, fault, "referenceFaults", "Injected reference fault must fail atomically.");

            var winners = new List<SectorCanvasOwnershipClaim>();
            var evidence = new List<SectorCanvasOwnershipClaim>();
            var suppressed = new List<SectorCanvasSuppressedClaim>();
            var conflicts = new List<SectorCanvasConflict>();

            foreach (var group in claimBuildResult.Claims.GroupBy(PlaneKey)
                         .OrderBy(value => value.Key, StringComparer.Ordinal))
            {
                var claims = group.OrderBy(value => value).ToArray();
                if (claims[0].Plane == SectorCanvasOwnershipPlane.Evidence)
                {
                    evidence.AddRange(claims.Select(value =>
                        value.WithState(SectorCanvasClaimState.AllowedCoPlaneEvidence)));
                    continue;
                }

                ValidatePlaneKinds(claims, errors, conflicts);
                var active = claims.Where(value => value.State == SectorCanvasClaimState.Winner).ToArray();
                if (active.Length == 0)
                {
                    AddConflict(errors, conflicts,
                        SectorCanvasOwnershipErrorCode.SuppressedClaimWithoutWinner,
                        claims, "Suppressed claims require a published winner in the same plane.");
                    continue;
                }

                var bestPriority = active.Min(value => (int)value.Priority);
                var best = active.Where(value => (int)value.Priority == bestPriority).ToArray();
                if (best.Length != 1)
                {
                    AddConflict(errors, conflicts,
                        SectorCanvasOwnershipErrorCode.DoubleOwnerConflict,
                        best, "Equal-priority claims cannot select a unique same-plane owner.");
                    AddConflict(errors, conflicts, PlaneConflictCode(claims[0].Plane), best,
                        "The ownership plane has ambiguous equal-priority owners.");
                    continue;
                }

                var winner = best[0];
                var lower = active.Where(value => !ReferenceEquals(value, winner)).ToArray();
                if (ForbiddenSamePlane(winner, lower) || lower.Any(value => !value.AllowSuppression))
                {
                    AddConflict(errors, conflicts,
                        SectorCanvasOwnershipErrorCode.ForbiddenOverlap,
                        active, "Same-plane overlap is forbidden by owner or suppression policy.");
                    AddConflict(errors, conflicts, PlaneConflictCode(claims[0].Plane), active,
                        "The ownership plane contains a forbidden overlap.");
                    continue;
                }

                winners.Add(winner.WithState(SectorCanvasClaimState.Winner));
                foreach (var lowerClaim in lower)
                    suppressed.Add(new SectorCanvasSuppressedClaim(
                        winner, lowerClaim,
                        winner.OwnerKind + "(" + (int)winner.Priority + ") > " +
                        lowerClaim.OwnerKind + "(" + (int)lowerClaim.Priority + ")"));
            }

            ValidateCrossPlane(winners, errors, conflicts, out var coexistenceCount);
            ValidateRequiredSources(request, claimBuildResult.Claims, winners, errors, conflicts);
            ValidateSuppressionReferences(winners, suppressed, errors);
            var coverage = ValidateCoverage(request, claimBuildResult.Claims, winners,
                errors, conflicts, out var explicitNoTerrainCount);

            if (errors.Count != 0)
                return Failure(request, errors);

            var ownedCells = winners.Select(value => new SectorCanvasOwnedCell(value)).ToArray();
            if (ownedCells.Any(value => value.Coordinate.X < 0 || value.Coordinate.X >= 48 ||
                                        value.Coordinate.Y < 0 || value.Coordinate.Y >= 32))
            {
                Add(errors, SectorCanvasOwnershipErrorCode.OwnedCellOutOfBounds,
                    "ownedCells", "Resolved owned cells must remain inside 48x32.");
                return Failure(request, errors);
            }

            var provisional = new SectorCanvasOwnershipPlan(
                request, claimBuildResult.Claims, winners, evidence, ownedCells, suppressed,
                Array.Empty<SectorCanvasConflict>(), coexistenceCount,
                explicitNoTerrainCount, coverage, claimBuildResult.CanonicalDigest, string.Empty);
            var digest = SectorCanvasOwnershipCanonicalDigest.ComputePlan(provisional);
            if (!string.IsNullOrEmpty(request.ExpectedPlanDigest) &&
                !string.Equals(request.ExpectedPlanDigest, digest, StringComparison.Ordinal))
            {
                Add(errors, SectorCanvasOwnershipErrorCode.NonCanonicalPublication,
                    "expectedPlanDigest", "Published ownership plan digest did not match the expected digest.");
                return Failure(request, errors);
            }

            var plan = new SectorCanvasOwnershipPlan(
                request, claimBuildResult.Claims, winners, evidence, ownedCells, suppressed,
                Array.Empty<SectorCanvasConflict>(), coexistenceCount,
                explicitNoTerrainCount, coverage, claimBuildResult.CanonicalDigest, digest);
            return new SectorCanvasOwnershipBuildResult(
                request, claimBuildResult.Claims, plan, digest,
                Array.Empty<SectorCanvasOwnershipError>());
        }

        private static void ValidatePlaneKinds(
            IReadOnlyList<SectorCanvasOwnershipClaim> claims,
            ICollection<SectorCanvasOwnershipError> errors,
            ICollection<SectorCanvasConflict> conflicts)
        {
            foreach (var claim in claims)
            {
                if ((claim.OwnerKind == SectorCanvasOwnerKind.ActivityMarker ||
                     claim.OwnerKind == SectorCanvasOwnerKind.EventMarker) &&
                    claim.Plane != SectorCanvasOwnershipPlane.Marker)
                {
                    var code = claim.OwnerKind == SectorCanvasOwnerKind.ActivityMarker
                        ? SectorCanvasOwnershipErrorCode.ActivityMarkerMutationClaim
                        : SectorCanvasOwnershipErrorCode.EventMarkerMutationClaim;
                    AddConflict(errors, conflicts, code, new[] { claim },
                        "Activity/Event marker attempted to own a non-marker plane.");
                }
                if (claim.Plane == SectorCanvasOwnershipPlane.Marker && !claim.MarkerOnly)
                    AddConflict(errors, conflicts,
                        SectorCanvasOwnershipErrorCode.MarkerPlaneConflict,
                        new[] { claim }, "Marker-plane claims must be marker-only.");
            }
        }

        private static bool ForbiddenSamePlane(
            SectorCanvasOwnershipClaim winner,
            IEnumerable<SectorCanvasOwnershipClaim> lowerClaims)
        {
            var owners = lowerClaims.Select(value => value.OwnerKind).Append(winner.OwnerKind).ToArray();
            if (winner.Plane == SectorCanvasOwnershipPlane.Reservation &&
                owners.Contains(SectorCanvasOwnerKind.SpecialRegion) &&
                owners.Contains(SectorCanvasOwnerKind.Boundary))
                return true;
            if (winner.Plane == SectorCanvasOwnershipPlane.Protection &&
                owners.Contains(SectorCanvasOwnerKind.Spine) &&
                owners.Contains(SectorCanvasOwnerKind.Boundary))
                return true;
            return false;
        }

        private static void ValidateCrossPlane(
            IReadOnlyList<SectorCanvasOwnershipClaim> winners,
            ICollection<SectorCanvasOwnershipError> errors,
            ICollection<SectorCanvasConflict> conflicts,
            out int coexistenceCount)
        {
            coexistenceCount = 0;
            foreach (var group in winners.GroupBy(CoordinateKey)
                         .OrderBy(value => value.Key, StringComparer.Ordinal))
            {
                var values = group.OrderBy(value => value).ToArray();
                if (values.Select(value => value.Plane).Distinct().Count() > 1)
                    coexistenceCount++;
                var protection = values.FirstOrDefault(value => value.Plane == SectorCanvasOwnershipPlane.Protection);
                var terrain = values.FirstOrDefault(value => value.Plane == SectorCanvasOwnershipPlane.Terrain);
                if (protection != null && terrain != null && protection.NoWrite &&
                    (terrain.OwnerKind == SectorCanvasOwnerKind.MicroPattern ||
                     terrain.OwnerKind == SectorCanvasOwnerKind.Quiet))
                {
                    AddConflict(errors, conflicts,
                        SectorCanvasOwnershipErrorCode.ForbiddenOverlap,
                        new[] { protection, terrain },
                        "MicroPattern/Quiet terrain cannot write through ProtectedOpen no-write evidence.");
                    AddConflict(errors, conflicts,
                        SectorCanvasOwnershipErrorCode.ProtectionPlaneConflict,
                        new[] { protection, terrain },
                        "ProtectedOpen cross-plane no-write contract was violated.");
                }

                var reservation = values.FirstOrDefault(value => value.Plane == SectorCanvasOwnershipPlane.Reservation);
                if (reservation != null && terrain != null && !reservation.NoWrite &&
                    !string.Equals(reservation.SourceObjectId, terrain.SourceObjectId, StringComparison.Ordinal))
                    AddConflict(errors, conflicts,
                        SectorCanvasOwnershipErrorCode.ReservationPlaneConflict,
                        new[] { reservation, terrain },
                        "Reservation and terrain may coexist only for matching identity or explicit no-write.");
            }
        }

        private static void ValidateRequiredSources(
            SectorCanvasOwnershipBuildRequest request,
            IReadOnlyList<SectorCanvasOwnershipClaim> allClaims,
            IReadOnlyList<SectorCanvasOwnershipClaim> winners,
            ICollection<SectorCanvasOwnershipError> errors,
            ICollection<SectorCanvasConflict> conflicts)
        {
            foreach (var cell in request.QuietActivityEventPlan.QuietFillPlan.Cells)
            {
                if (cell.ProtectedNoWrite && !winners.Any(value =>
                        value.SectorIndex == cell.SectorIndex && value.Coordinate.Equals(cell.Coordinate) &&
                        value.Plane == SectorCanvasOwnershipPlane.Protection))
                    AddAt(errors, conflicts, SectorCanvasOwnershipErrorCode.MissingRequiredClaim,
                        cell.SectorCoordinate, cell.SectorIndex, cell.Coordinate,
                        SectorCanvasOwnershipPlane.Protection, "ProtectedOpen requires a protection winner.");
                if (cell.ReservedNoWrite && !winners.Any(value =>
                        value.SectorIndex == cell.SectorIndex && value.Coordinate.Equals(cell.Coordinate) &&
                        value.Plane == SectorCanvasOwnershipPlane.Reservation))
                    AddAt(errors, conflicts, SectorCanvasOwnershipErrorCode.MissingRequiredClaim,
                        cell.SectorCoordinate, cell.SectorIndex, cell.Coordinate,
                        SectorCanvasOwnershipPlane.Reservation, "Reserved coordinates require a reservation winner.");
            }

            foreach (var decision in request.QuietActivityEventPlan.ActivityDecisions
                         .Where(value => value.State == SectorActivityEventPlacementState.Selected))
                RequireDecisionClaim(allClaims, decision.Opportunity.SectorCoordinate,
                    decision.Opportunity.MarkerCoordinate, decision.OpportunityId,
                    SectorCanvasOwnerKind.ActivityMarker, errors, conflicts);
            foreach (var decision in request.QuietActivityEventPlan.EventDecisions
                         .Where(value => value.State == SectorActivityEventPlacementState.Assigned ||
                                         value.State == SectorActivityEventPlacementState.ExplicitEmpty))
                RequireDecisionClaim(allClaims, decision.Opportunity.SectorCoordinate,
                    decision.Opportunity.MarkerCoordinate, decision.OpportunityId,
                    decision.State == SectorActivityEventPlacementState.Assigned
                        ? SectorCanvasOwnerKind.EventMarker
                        : SectorCanvasOwnerKind.Empty,
                    errors, conflicts);
        }

        private static void RequireDecisionClaim(
            IEnumerable<SectorCanvasOwnershipClaim> claims,
            SectorCoord sector,
            LocalTileCoord coordinate,
            string opportunityId,
            SectorCanvasOwnerKind owner,
            ICollection<SectorCanvasOwnershipError> errors,
            ICollection<SectorCanvasConflict> conflicts)
        {
            if (claims.Any(value => value.OwnerKind == owner &&
                                    value.SectorCoordinate.Equals(sector) &&
                                    value.Coordinate.Equals(coordinate)))
                return;
            AddAt(errors, conflicts, SectorCanvasOwnershipErrorCode.MissingRequiredClaim,
                sector, (sector.Y * 13) + sector.X, coordinate,
                owner == SectorCanvasOwnerKind.Empty
                    ? SectorCanvasOwnershipPlane.Evidence
                    : SectorCanvasOwnershipPlane.Marker,
                "Activity/Event decision is missing from the marker/evidence plane: " + opportunityId);
        }

        private static void ValidateSuppressionReferences(
            IReadOnlyList<SectorCanvasOwnershipClaim> winners,
            IReadOnlyList<SectorCanvasSuppressedClaim> suppressed,
            ICollection<SectorCanvasOwnershipError> errors)
        {
            var winnerIds = new HashSet<string>(winners.Select(value => value.ClaimId), StringComparer.Ordinal);
            foreach (var value in suppressed)
                if (!winnerIds.Contains(value.WinnerClaimId))
                    Add(errors, SectorCanvasOwnershipErrorCode.SuppressedClaimWithoutWinner,
                        value.SuppressedClaimId, "Suppression references a missing winner claim.");
        }

        private static int ValidateCoverage(
            SectorCanvasOwnershipBuildRequest request,
            IReadOnlyList<SectorCanvasOwnershipClaim> allClaims,
            IReadOnlyList<SectorCanvasOwnershipClaim> winners,
            ICollection<SectorCanvasOwnershipError> errors,
            ICollection<SectorCanvasConflict> conflicts,
            out int explicitNoTerrainCount)
        {
            var terrain = new HashSet<string>(winners
                .Where(value => value.Plane == SectorCanvasOwnershipPlane.Terrain)
                .Select(CoordinateKey), StringComparer.Ordinal);
            var explicitNoTerrain = new HashSet<string>(allClaims
                .Where(value => value.NoWrite || value.Plane == SectorCanvasOwnershipPlane.Evidence ||
                                value.OwnerKind == SectorCanvasOwnerKind.Empty)
                .Select(CoordinateKey), StringComparer.Ordinal);
            var coverage = 0;
            explicitNoTerrainCount = 0;
            foreach (var sector in request.Input.Sectors.OrderBy(value => value.SectorIndex))
            for (var y = 0; y < 32; y++)
            for (var x = 0; x < 48; x++)
            {
                var coordinate = new LocalTileCoord(x, y);
                var key = CoordinateKey(sector.SectorIndex, coordinate);
                if (terrain.Contains(key))
                {
                    coverage++;
                    continue;
                }
                if (explicitNoTerrain.Contains(key))
                {
                    coverage++;
                    explicitNoTerrainCount++;
                    continue;
                }
                AddAt(errors, conflicts, SectorCanvasOwnershipErrorCode.CanvasCoverageMismatch,
                    sector.Coordinate, sector.SectorIndex, coordinate,
                    SectorCanvasOwnershipPlane.Terrain,
                    "Coordinate has neither a Terrain winner nor explicit no-terrain evidence.");
            }
            return coverage;
        }

        private static SectorCanvasOwnershipErrorCode PlaneConflictCode(
            SectorCanvasOwnershipPlane plane)
        {
            switch (plane)
            {
                case SectorCanvasOwnershipPlane.Terrain:
                    return SectorCanvasOwnershipErrorCode.TerrainPlaneConflict;
                case SectorCanvasOwnershipPlane.Protection:
                    return SectorCanvasOwnershipErrorCode.ProtectionPlaneConflict;
                case SectorCanvasOwnershipPlane.Reservation:
                    return SectorCanvasOwnershipErrorCode.ReservationPlaneConflict;
                case SectorCanvasOwnershipPlane.Marker:
                    return SectorCanvasOwnershipErrorCode.MarkerPlaneConflict;
                default:
                    return SectorCanvasOwnershipErrorCode.DoubleOwnerConflict;
            }
        }

        private static string PlaneKey(SectorCanvasOwnershipClaim claim) =>
            CoordinateKey(claim) + "|" + ((int)claim.Plane).ToString(CultureInfo.InvariantCulture);

        private static string CoordinateKey(SectorCanvasOwnershipClaim claim) =>
            CoordinateKey(claim.SectorIndex, claim.Coordinate);

        private static string CoordinateKey(int sectorIndex, LocalTileCoord coordinate) =>
            sectorIndex.ToString("D3", CultureInfo.InvariantCulture) + "|" +
            coordinate.X.ToString("D2", CultureInfo.InvariantCulture) + "," +
            coordinate.Y.ToString("D2", CultureInfo.InvariantCulture);

        private static void AddConflict(
            ICollection<SectorCanvasOwnershipError> errors,
            ICollection<SectorCanvasConflict> conflicts,
            SectorCanvasOwnershipErrorCode code,
            IReadOnlyList<SectorCanvasOwnershipClaim> claims,
            string detail)
        {
            var first = claims[0];
            var subject = PlaneKey(first) + "|" +
                          string.Join(";", claims.Select(value => value.ClaimId).OrderBy(value => value, StringComparer.Ordinal));
            Add(errors, code, subject, detail);
            conflicts.Add(new SectorCanvasConflict(
                code, first.SectorCoordinate, first.SectorIndex, first.Coordinate,
                first.Plane, claims.Select(value => value.ClaimId), detail));
        }

        private static void AddAt(
            ICollection<SectorCanvasOwnershipError> errors,
            ICollection<SectorCanvasConflict> conflicts,
            SectorCanvasOwnershipErrorCode code,
            SectorCoord sector,
            int sectorIndex,
            LocalTileCoord coordinate,
            SectorCanvasOwnershipPlane plane,
            string detail)
        {
            var subject = CoordinateKey(sectorIndex, coordinate) + "|" +
                          ((int)plane).ToString(CultureInfo.InvariantCulture);
            Add(errors, code, subject, detail);
            conflicts.Add(new SectorCanvasConflict(
                code, sector, sectorIndex, coordinate, plane,
                Array.Empty<string>(), detail));
        }

        private static SectorCanvasOwnershipBuildResult Failure(
            SectorCanvasOwnershipBuildRequest request,
            IEnumerable<SectorCanvasOwnershipError> errors) =>
            new SectorCanvasOwnershipBuildResult(
                request, Array.Empty<SectorCanvasOwnershipClaim>(), null,
                string.Empty, errors);

        private static void Add(
            ICollection<SectorCanvasOwnershipError> errors,
            SectorCanvasOwnershipErrorCode code,
            string subject,
            string detail) =>
            errors.Add(new SectorCanvasOwnershipError(code, subject, detail));
    }
}
