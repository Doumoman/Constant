using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class SectorClusterPlacementPlanner
    {
        public const string ReferencePlacementPublicationLabel = "REFERENCE CLUSTER PLACEMENT";

        public static SectorClusterPlacementBuildResult Place(SectorClusterPlacementRequest request)
        {
            var errors = new List<SectorClusterCandidateError>();
            ValidateRequest(request, errors);
            if (request == null || request.CandidateSet == null || request.AnchorPlan == null)
                return Failure(errors);

            var selected = new List<SectorClusterPlacement>();
            foreach (var group in request.CandidateSet.Candidates.GroupBy(value => value.SectorIndex).OrderBy(value => value.Key))
            {
                var candidate = group.OrderBy(value => value, Comparer<SectorClusterCandidate>.Create(SectorClusterCandidate.Compare)).FirstOrDefault();
                if (candidate == null || candidate.ApprovedPlacements.Count == 0)
                {
                    Add(errors, SectorClusterCandidateErrorCode.NoCandidateForSector,
                        group.Key.ToString(CultureInfo.InvariantCulture), "A sector has no approved candidate placement.");
                    continue;
                }
                selected.Add(new SectorClusterPlacement(candidate, candidate.ApprovedPlacements[0], ConstraintClass(candidate.SectorIndex, request.AnchorPlan)));
            }

            if (selected.Count != request.CandidateSet.SectorCount)
            {
                Add(errors, SectorClusterCandidateErrorCode.NoCandidateForSector, "placement",
                    "Every candidate-set sector must publish exactly one selected placement.");
            }

            var ordered = selected
                .OrderBy(value => value.ConstraintClass)
                .ThenByDescending(value => value.Cells.Count)
                .ThenByDescending(value => value.PrimaryPacingMatch)
                .ThenBy(value => value.ClusterId)
                .ThenBy(value => value.VariantId)
                .ThenBy(value => value.SectorIndex)
                .ToArray();
            ValidatePlacements(ordered, request.AnchorPlan, errors);
            if (errors.Count > 0)
                return Failure(errors);

            var rejected = request.CandidateSet.RejectedCountByReason.ToDictionary(value => value.Key, value => value.Value);
            var lowerRanked = request.CandidateSet.CandidateCount - ordered.Length;
            if (lowerRanked > 0) rejected[SectorClusterCandidateErrorCode.LowerRankedCandidate] = lowerRanked;

            var blockingCellCount = request.AnchorPlan.Anchors.Select(value => value.SectorIndex).Distinct()
                .Sum(index => SectorClusterAnchorUtility.BlockingCells(request.AnchorPlan, index).Count);
            var placedCellCount = ordered.Sum(value => value.Cells.Count);
            var freeCellCount = (request.CandidateSet.SectorCount * WorldGenConstants.MicroChunksPerSector)
                                - blockingCellCount - placedCellCount;
            if (freeCellCount < 0)
            {
                Add(errors, SectorClusterCandidateErrorCode.PlacementOverlap, "freeCells",
                    "Placed and fixed footprint cells exceed the sector grid capacity.");
                return Failure(errors);
            }

            var provisional = new SectorClusterPlacementPlan(
                request.PublicationLabel,
                request.CandidateSet.CanonicalDigest,
                request.AnchorPlan.CanonicalDigest,
                ordered,
                rejected,
                blockingCellCount,
                freeCellCount,
                string.Empty);
            var digest = SectorClusterPlacementCanonicalDigest.Compute(provisional);
            if (request.ExpectedCanonicalDigest.Length != 0
                && !string.Equals(request.ExpectedCanonicalDigest, digest, StringComparison.Ordinal))
            {
                Add(errors, SectorClusterCandidateErrorCode.NonCanonicalPublication, "digest",
                    "Placement plan digest does not match the expected canonical digest.");
                return Failure(errors);
            }

            var plan = new SectorClusterPlacementPlan(
                request.PublicationLabel,
                request.CandidateSet.CanonicalDigest,
                request.AnchorPlan.CanonicalDigest,
                ordered,
                rejected,
                blockingCellCount,
                freeCellCount,
                digest);
            return new SectorClusterPlacementBuildResult(plan, Array.Empty<SectorClusterCandidateError>());
        }

        private static void ValidateRequest(
            SectorClusterPlacementRequest request,
            ICollection<SectorClusterCandidateError> errors)
        {
            if (request == null)
            {
                Add(errors, SectorClusterCandidateErrorCode.MissingInput, "request", "Placement request is required.");
                return;
            }
            if (request.CandidateSet == null)
                Add(errors, SectorClusterCandidateErrorCode.MissingInput, "candidateSet", "A successful candidate set is required.");
            if (request.AnchorPlan == null)
                Add(errors, SectorClusterCandidateErrorCode.MissingAnchorPlan, "anchorPlan", "A matching anchor plan is required.");
            if (!string.Equals(request.PublicationLabel, ReferencePlacementPublicationLabel, StringComparison.Ordinal))
                Add(errors, SectorClusterCandidateErrorCode.NonCanonicalPublication, "publicationLabel", "Publication must be marked REFERENCE CLUSTER PLACEMENT.");
            if (request.SolverMutationCount != 0)
                Add(errors, SectorClusterCandidateErrorCode.SolverMutationClaim, "solver", "Placement cannot invoke or claim solver mutation.");
            if (request.RandomDrawCount != 0)
                Add(errors, SectorClusterCandidateErrorCode.RngMutationClaim, "rng", "Placement cannot draw RNG.");
            if (request.TileWriteCount != 0)
                Add(errors, SectorClusterCandidateErrorCode.TileMutationClaim, "tile", "Placement cannot write tiles.");

            if (request.CandidateSet != null)
            {
                var rebuilt = SectorClusterCandidateCanonicalDigest.Compute(request.CandidateSet);
                if (!string.Equals(rebuilt, request.CandidateSet.CanonicalDigest, StringComparison.Ordinal))
                    Add(errors, SectorClusterCandidateErrorCode.NonCanonicalPublication, "candidateSet", "Candidate set digest must rebuild exactly.");
            }
            if (request.CandidateSet != null && request.AnchorPlan != null
                && (!string.Equals(request.CandidateSet.AnchorPlanDigest, request.AnchorPlan.CanonicalDigest, StringComparison.Ordinal)
                    || !string.Equals(request.CandidateSet.PlannerInputDigest, request.AnchorPlan.PlannerInputDigest, StringComparison.Ordinal)))
            {
                Add(errors, SectorClusterCandidateErrorCode.SectorMismatch, "anchorPlan", "Candidate set and anchor plan identities must match.");
            }
        }

        private static void ValidatePlacements(
            IReadOnlyList<SectorClusterPlacement> placements,
            SectorFixedAnchorPlan anchorPlan,
            ICollection<SectorClusterCandidateError> errors)
        {
            var occupied = new HashSet<string>(StringComparer.Ordinal);
            foreach (var placement in placements)
            {
                if (placement.Cells.Count == 0
                    || placement.Cells.Any(value => value.X < 0 || value.X >= WorldGenConstants.MicroChunkColumnsPerSector
                                                      || value.Y < 0 || value.Y >= WorldGenConstants.MicroChunkRowsPerSector)
                    || placement.TileRects.Any(value => !value.IsInside(WorldGenConstants.SectorWidthTiles, WorldGenConstants.SectorHeightTiles)))
                {
                    Add(errors, SectorClusterCandidateErrorCode.FootprintOutOfBounds, Subject(placement),
                        "Selected footprint cells and 12x8 tile rects must remain inside 4x4 / 48x32.");
                }

                var blocking = SectorClusterAnchorUtility.BlockingCells(anchorPlan, placement.SectorIndex);
                if (placement.Cells.Any(blocking.Contains))
                {
                    Add(errors, SectorClusterCandidateErrorCode.AnchorOverlap, Subject(placement),
                        "Selected placement overlaps a fixed route, boundary, site, or SpecialRegion anchor.");
                }

                foreach (var cell in placement.Cells)
                {
                    var key = placement.SectorIndex.ToString(CultureInfo.InvariantCulture) + ":" + cell;
                    if (!occupied.Add(key))
                        Add(errors, SectorClusterCandidateErrorCode.PlacementOverlap, Subject(placement),
                            "Selected cluster placements cannot overlap one another.");
                }
            }

            var sorted = placements
                .OrderBy(value => value.ConstraintClass)
                .ThenByDescending(value => value.Cells.Count)
                .ThenByDescending(value => value.PrimaryPacingMatch)
                .ThenBy(value => value.ClusterId)
                .ThenBy(value => value.VariantId)
                .ThenBy(value => value.SectorIndex)
                .ToArray();
            if (!placements.SequenceEqual(sorted))
                Add(errors, SectorClusterCandidateErrorCode.PlacementOrderViolation, "placementOrder",
                    "Placement order must remain constraint-large-first and ordinal.");
        }

        private static int ConstraintClass(int sectorIndex, SectorFixedAnchorPlan anchorPlan)
        {
            var anchors = anchorPlan.Anchors.Where(value => value.SectorIndex == sectorIndex).ToArray();
            if (anchors.Any(value => value.Kind == SectorFixedAnchorKind.SpecialFootprint
                                     || value.Kind == SectorFixedAnchorKind.SpecialEntryReturn
                                     || value.Kind == SectorFixedAnchorKind.SpecialApronBuffer
                                     || value.Kind == SectorFixedAnchorKind.SiteReservation)) return 0;
            if (anchors.Any(value => value.Kind == SectorFixedAnchorKind.ExternalRouteSocket
                                     || value.Kind == SectorFixedAnchorKind.BoundaryFixedSlice)) return 1;
            return 2;
        }

        private static string Subject(SectorClusterPlacement placement)
            => placement.SectorIndex.ToString("D3", CultureInfo.InvariantCulture) + "/"
               + placement.ClusterId.Value + "/" + placement.VariantId.Value;

        private static void Add(
            ICollection<SectorClusterCandidateError> errors,
            SectorClusterCandidateErrorCode code,
            string subject,
            string detail)
            => errors.Add(new SectorClusterCandidateError(code, subject, detail));

        private static SectorClusterPlacementBuildResult Failure(IEnumerable<SectorClusterCandidateError> errors)
            => new SectorClusterPlacementBuildResult(null, errors);
    }
}
