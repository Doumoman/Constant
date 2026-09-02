using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Baking
{
    public static class SectorCanvasLayerFinalizer
    {
        public const string ReferencePublicationLabel = "REFERENCE FINAL CANVAS LAYER PLAN";

        public static FinalCanvasLayerResult Finalize(FinalCanvasLayerRequest request)
        {
            var failures = new List<FinalCanvasLayerFailure>();
            var conflicts = new List<FinalCanvasConflict>();
            if (request == null)
            {
                Add(failures, FinalCanvasLayerFailureCode.MissingRequest, "request",
                    "A final canvas layer request is required.");
                return Failed(null, failures, conflicts);
            }

            ValidateRequest(request, failures);
            ValidateClaims(request, failures);
            if (failures.Count > 0) return Failed(request, failures, conflicts);

            var winners = new Dictionary<int, Dictionary<FinalCanvasLayerKind, FinalCanvasLayerClaim>>();
            foreach (var coordinateGroup in request.Claims.GroupBy(value => value.Coordinate.RowMajorIndex)
                         .OrderBy(value => value.Key))
            {
                var layerWinners = new Dictionary<FinalCanvasLayerKind, FinalCanvasLayerClaim>();
                foreach (var layerGroup in coordinateGroup.GroupBy(value => value.Layer)
                             .OrderBy(value => value.Key))
                {
                    var ordered = layerGroup.OrderByDescending(value => value.Priority)
                        .ThenByDescending(value => value.SourceOwner)
                        .ThenBy(value => value.ProvenanceId, StringComparer.Ordinal)
                        .ThenBy(value => value.ClaimId, StringComparer.Ordinal).ToArray();
                    var winner = ordered[0];
                    layerWinners.Add(layerGroup.Key, winner);
                    DetectSamePriorityConflicts(ordered, conflicts);
                    DetectPrecedenceOverwrite(winner, ordered.Skip(1), conflicts);
                }
                winners.Add(coordinateGroup.Key, layerWinners);
                DetectProtectedCellOverwrite(coordinateGroup.ToArray(), layerWinners, conflicts);
            }

            if (conflicts.Count > 0)
            {
                foreach (var conflict in conflicts.OrderBy(value => value))
                {
                    Add(failures,
                        conflict.Kind == FinalCanvasConflictKind.SamePriorityDifferentValue
                            ? FinalCanvasLayerFailureCode.SameLayerConflict
                            : FinalCanvasLayerFailureCode.ForbiddenOverwrite,
                        conflict.Coordinate + ":" + conflict.Layer, conflict.Reason);
                }
                return Failed(request, failures, conflicts);
            }

            var cells = winners.OrderBy(value => value.Key).Select(value =>
            {
                var coordinate = request.Claims.First(claim =>
                    claim.Coordinate.RowMajorIndex == value.Key).Coordinate;
                return new FinalCanvasCell(coordinate, value.Value.Values);
            }).ToArray();
            var summaries = Enum.GetValues(typeof(FinalCanvasLayerKind)).Cast<FinalCanvasLayerKind>()
                .Select(layer => new FinalCanvasLayerSummary(layer,
                    cells.Sum(cell => cell.Winners.Count(claim => claim.Layer == layer))))
                .ToArray();
            var outputDigest = FinalCanvasLayerDigest.ComputeOutput(
                request, cells, summaries, Array.Empty<FinalCanvasConflict>());
            if (!FinalCanvasLayerDigest.IsLowerHexSha256(request.CanonicalDigest) ||
                !FinalCanvasLayerDigest.IsLowerHexSha256(outputDigest))
            {
                Add(failures, FinalCanvasLayerFailureCode.InvalidDigest, "digest",
                    "Input and output digests must be lowercase SHA-256 values.");
                return Failed(request, failures, conflicts);
            }

            var plan = new SectorFinalCanvasLayerPlan(
                request, cells, summaries, Array.Empty<FinalCanvasConflict>(), outputDigest);
            return new FinalCanvasLayerResult(request, plan,
                Array.Empty<FinalCanvasLayerFailure>(), Array.Empty<FinalCanvasConflict>());
        }

        private static void ValidateRequest(
            FinalCanvasLayerRequest request,
            ICollection<FinalCanvasLayerFailure> failures)
        {
            if (!request.Map15ExitApproved)
                Add(failures, FinalCanvasLayerFailureCode.UpstreamExitNotApproved, "map15ExitApproved",
                    "MAP15_07 exit approval is required.");
            if (!SafeRequired(request.SectorId))
                Add(failures, FinalCanvasLayerFailureCode.InvalidSectorIdentity, "sectorId",
                    "A stable path-free sector identity is required.");
            if (request.Width != SectorFinalCanvasLayerPlan.SectorWidth ||
                request.Height != SectorFinalCanvasLayerPlan.SectorHeight)
                Add(failures, FinalCanvasLayerFailureCode.InvalidDimensions, "dimensions",
                    "Final canvas dimensions must be exactly 48x32.");
            if (!FinalCanvasLayerDigest.IsLowerHexSha256(request.Map15ExitDigest) ||
                !FinalCanvasLayerDigest.IsLowerHexSha256(request.WorldAssemblyDigest) ||
                !FinalCanvasLayerDigest.IsLowerHexSha256(request.SectorOwnershipDigest) ||
                !FinalCanvasLayerDigest.IsLowerHexSha256(request.BoundaryAuthorityDigest) ||
                !FinalCanvasLayerDigest.IsLowerHexSha256(request.FixedCanvasAuthorityDigest))
                Add(failures, FinalCanvasLayerFailureCode.MissingUpstreamIdentity, "upstreamDigests",
                    "MAP15_07, MAP15_06, MAP14, MAP08, and MAP07 lowercase SHA-256 identities are required.");
            if (!SafeRequired(request.PublicationLabel) ||
                request.PublicationLabel != ReferencePublicationLabel)
                Add(failures, FinalCanvasLayerFailureCode.InvalidSectorIdentity, "publicationLabel",
                    "The reference final canvas publication label is required.");

            var forbidden = new[]
            {
                request.NewRngDrawCount, request.SliceCreationCount, request.GeneratedFileWriteCount,
                request.TilemapMutationCount, request.SceneMutationCount, request.PrefabMutationCount,
                request.GameObjectMutationCount, request.GameplaySpawnCount,
                request.ProductionSeedApprovalCount, request.SectorRerollCount,
                request.FallbackCarveCount, request.FullRegressionCount,
            };
            if (forbidden.Any(value => value != 0))
                Add(failures, FinalCanvasLayerFailureCode.ForbiddenOperation, "operationCounters",
                    "Finalization cannot draw RNG, slice, write, mutate, spawn, reroll, carve, approve production, or run regression.");
        }

        private static void ValidateClaims(
            FinalCanvasLayerRequest request,
            ICollection<FinalCanvasLayerFailure> failures)
        {
            if (request.NullClaimCount > 0)
                Add(failures, FinalCanvasLayerFailureCode.InvalidClaim, "claims",
                    "Null claims are forbidden.");
            var duplicateClaimIds = request.Claims.GroupBy(value => value.ClaimId, StringComparer.Ordinal)
                .Where(value => value.Count() > 1).Select(value => value.Key).OrderBy(value => value).ToArray();
            foreach (var duplicate in duplicateClaimIds)
                Add(failures, FinalCanvasLayerFailureCode.InvalidClaim, duplicate,
                    "Claim ids must be unique.");

            foreach (var claim in request.Claims)
            {
                if (claim.Coordinate == null || !claim.Coordinate.IsInBounds)
                {
                    Add(failures, FinalCanvasLayerFailureCode.InvalidCoordinate, claim.ClaimId,
                        "Claim coordinates must be present and inside 48x32.");
                    continue;
                }
                if (!Enum.IsDefined(typeof(FinalCanvasLayerKind), claim.Layer) ||
                    !Enum.IsDefined(typeof(FinalCanvasCellKind), claim.CellKind) ||
                    claim.CellKind == FinalCanvasCellKind.Unknown ||
                    !Enum.IsDefined(typeof(FinalCanvasSourceOwner), claim.SourceOwner) ||
                    claim.SourceOwner == FinalCanvasSourceOwner.Unknown ||
                    !Enum.IsDefined(typeof(FinalCanvasClaimPriority), claim.Priority) ||
                    claim.Priority == FinalCanvasClaimPriority.Unknown ||
                    !Enum.IsDefined(typeof(FinalCanvasProtectionKind), claim.Protection) ||
                    !SafeRequired(claim.ClaimId) || !SafeRequired(claim.ProvenanceId) ||
                    !SafeRequired(claim.AuthorityReason) ||
                    (claim.IsProtected && claim.Protection == FinalCanvasProtectionKind.None))
                    Add(failures, FinalCanvasLayerFailureCode.InvalidClaim, claim.ClaimId,
                        "Layer, cell kind, source, priority, protection, provenance, and authority must be explicit and path-free.");
            }
            if (failures.Count > 0) return;

            var coordinateCount = request.Claims.Select(value => value.Coordinate.RowMajorIndex)
                .Distinct().Count();
            if (coordinateCount != SectorFinalCanvasLayerPlan.CellCount)
                Add(failures, FinalCanvasLayerFailureCode.InvalidCellCount, "coordinates",
                    "Claims must cover exactly 1536 unique final canvas coordinates.");

            var requiredLayers = Enum.GetValues(typeof(FinalCanvasLayerKind)).Cast<FinalCanvasLayerKind>().ToArray();
            var layerCoverage = request.Claims.Select(value => value.Layer).Distinct().Count();
            if (layerCoverage != SectorFinalCanvasLayerPlan.RequiredLayerCount)
                Add(failures, FinalCanvasLayerFailureCode.MissingLayerCoverage, "layers",
                    "All seven final canvas layers are required.");
            foreach (var coordinateGroup in request.Claims.GroupBy(value => value.Coordinate.RowMajorIndex))
            {
                var present = coordinateGroup.Select(value => value.Layer).Distinct().ToArray();
                if (requiredLayers.Any(layer => !present.Contains(layer)))
                    Add(failures, FinalCanvasLayerFailureCode.MissingLayerCoverage,
                        coordinateGroup.Key.ToString(CultureInfo.InvariantCulture),
                        "Every final cell must publish all seven layer winners.");
            }
        }

        private static void DetectSamePriorityConflicts(
            IEnumerable<FinalCanvasLayerClaim> claims,
            ICollection<FinalCanvasConflict> conflicts)
        {
            foreach (var priorityGroup in claims.GroupBy(value => value.Priority).OrderBy(value => value.Key))
            {
                var values = priorityGroup.Select(value => value.CellKind).Distinct().ToArray();
                if (values.Length <= 1 || priorityGroup.All(value => value.AllowsExplicitMerge)) continue;
                var ordered = priorityGroup.OrderByDescending(value => value.SourceOwner)
                    .ThenBy(value => value.ProvenanceId, StringComparer.Ordinal)
                    .ThenBy(value => value.ClaimId, StringComparer.Ordinal).ToArray();
                conflicts.Add(new FinalCanvasConflict(
                    FinalCanvasConflictKind.SamePriorityDifferentValue,
                    ordered[0].Coordinate, ordered[0].Layer, ordered[0], ordered[1],
                    "Same-priority claims publish different values without an explicit merge."));
            }
        }

        private static void DetectPrecedenceOverwrite(
            FinalCanvasLayerClaim winner,
            IEnumerable<FinalCanvasLayerClaim> suppressedClaims,
            ICollection<FinalCanvasConflict> conflicts)
        {
            foreach (var suppressed in suppressedClaims)
            {
                if (winner.Layer == FinalCanvasLayerKind.Terrain &&
                    winner.Priority == FinalCanvasClaimPriority.FixedSlice &&
                    suppressed.Priority < winner.Priority && suppressed.CellKind != winner.CellKind)
                    conflicts.Add(new FinalCanvasConflict(
                        FinalCanvasConflictKind.FixedSliceOverwrite, winner.Coordinate, winner.Layer,
                        winner, suppressed, "A weaker terrain claim cannot alter fixed-slice terrain."));

                if ((winner.Layer == FinalCanvasLayerKind.Terrain ||
                     winner.Layer == FinalCanvasLayerKind.Hazard) &&
                    winner.Priority == FinalCanvasClaimPriority.BoundaryAperture &&
                    suppressed.Priority < winner.Priority && suppressed.CellKind != winner.CellKind)
                    conflicts.Add(new FinalCanvasConflict(
                        FinalCanvasConflictKind.BoundaryApertureOverwrite, winner.Coordinate, winner.Layer,
                        winner, suppressed, "A weaker terrain or hazard claim cannot alter a boundary aperture."));
            }
        }

        private static void DetectProtectedCellOverwrite(
            IReadOnlyCollection<FinalCanvasLayerClaim> claims,
            IReadOnlyDictionary<FinalCanvasLayerKind, FinalCanvasLayerClaim> winners,
            ICollection<FinalCanvasConflict> conflicts)
        {
            var protectionKinds = claims.Where(value => value.IsProtected ||
                value.Protection != FinalCanvasProtectionKind.None)
                .Select(value => value.Protection).Distinct().ToArray();
            if (protectionKinds.Length == 0) return;

            var coordinate = claims.First().Coordinate;
            foreach (var claim in claims.Where(value =>
                         (value.Layer == FinalCanvasLayerKind.Terrain ||
                          value.Layer == FinalCanvasLayerKind.Hazard) && IsBlocking(value.CellKind)))
            {
                if (protectionKinds.Contains(FinalCanvasProtectionKind.MandatoryRouteProtectedOpen))
                    conflicts.Add(new FinalCanvasConflict(
                        FinalCanvasConflictKind.MandatoryRouteProtectedOpenBlocked,
                        coordinate, claim.Layer, winners[claim.Layer], claim,
                        "Solid, blocked, or hazard claims cannot fill a mandatory protected-open cell."));
                if (protectionKinds.Contains(FinalCanvasProtectionKind.SpecialEntranceBuffer))
                    conflicts.Add(new FinalCanvasConflict(
                        FinalCanvasConflictKind.SpecialEntranceBlocked,
                        coordinate, claim.Layer, winners[claim.Layer], claim,
                        "Solid, blocked, or hazard claims cannot fill a Special entrance buffer."));
                if (protectionKinds.Contains(FinalCanvasProtectionKind.BoundaryAperture))
                    conflicts.Add(new FinalCanvasConflict(
                        FinalCanvasConflictKind.BoundaryApertureOverwrite,
                        coordinate, claim.Layer, winners[claim.Layer], claim,
                        "Solid, blocked, or hazard claims cannot close a boundary aperture."));
            }

            if (winners.TryGetValue(FinalCanvasLayerKind.Protection, out var protectionWinner) &&
                protectionWinner.CellKind == FinalCanvasCellKind.None &&
                (protectionWinner.SourceOwner == FinalCanvasSourceOwner.Cleanup ||
                 protectionWinner.SourceOwner == FinalCanvasSourceOwner.QuietFiller))
                conflicts.Add(new FinalCanvasConflict(
                    FinalCanvasConflictKind.ProtectionRemoval, coordinate,
                    FinalCanvasLayerKind.Protection, protectionWinner, protectionWinner,
                    "Cleanup and quiet filler cannot remove an established protection layer."));
        }

        private static bool IsBlocking(FinalCanvasCellKind kind) =>
            kind == FinalCanvasCellKind.Solid || kind == FinalCanvasCellKind.Hazard ||
            kind == FinalCanvasCellKind.Blocked;

        private static bool SafeRequired(string value) => !string.IsNullOrWhiteSpace(value) &&
            value.IndexOf('/') < 0 && value.IndexOf('\\') < 0 &&
            value.IndexOf('\r') < 0 && value.IndexOf('\n') < 0;

        private static FinalCanvasLayerResult Failed(
            FinalCanvasLayerRequest request,
            IEnumerable<FinalCanvasLayerFailure> failures,
            IEnumerable<FinalCanvasConflict> conflicts) =>
            new FinalCanvasLayerResult(request, null, failures, conflicts);

        private static void Add(
            ICollection<FinalCanvasLayerFailure> failures,
            FinalCanvasLayerFailureCode code,
            string subject,
            string reason) => failures.Add(new FinalCanvasLayerFailure(code, subject, reason));
    }
}
