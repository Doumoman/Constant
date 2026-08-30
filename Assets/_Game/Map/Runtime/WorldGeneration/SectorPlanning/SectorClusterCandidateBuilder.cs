using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Pipeline;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class SectorClusterCandidateBuilder
    {
        public const string ReferenceCandidatePublicationLabel = "REFERENCE CLUSTER CANDIDATE";

        public static SectorClusterCandidateBuildResult Build(SectorClusterCandidateBuildRequest request)
        {
            var errors = new List<SectorClusterCandidateError>();
            ValidateRequest(request, errors);
            if (request == null)
                return Failure(errors);

            var validSources = ValidateCatalog(request.ClusterCatalog, errors);
            if (request.Input == null || request.AnchorPlan == null || validSources.Count == 0)
                return Failure(errors);

            var assignmentByIndex = ValidateAssignments(request.Input, request.Assignments, errors);
            var rejected = new Dictionary<SectorClusterCandidateErrorCode, int>();
            var candidates = new List<SectorClusterCandidate>();

            foreach (var sector in request.Input.Sectors.OrderBy(value => value.SectorIndex))
            {
                if (!assignmentByIndex.TryGetValue(sector.SectorIndex, out var assignment))
                    continue;

                var sectorCandidateCount = 0;
                var sectorAnchorRejectsBefore = Count(rejected, SectorClusterCandidateErrorCode.AnchorOverlap);
                foreach (var source in validSources)
                {
                    if (!string.Equals(sector.Biome.BiomeId, source.Biome.ToString(), StringComparison.Ordinal))
                    {
                        Increment(rejected, SectorClusterCandidateErrorCode.BiomeMismatch);
                        continue;
                    }

                    var primaryMatch = source.CompatiblePacingRoles.Contains(assignment.PrimaryRole);
                    var candidateMatch = assignment.Candidates.Any(value => source.CompatiblePacingRoles.Contains(value.Role));
                    if (!primaryMatch && !candidateMatch)
                    {
                        Increment(rejected, SectorClusterCandidateErrorCode.PacingMismatch);
                        continue;
                    }

                    if (!source.CompatibleRouteTypes.Contains(sector.Route.RouteType)
                        || sector.Route.ExternalSockets.Any(socket => !source.CompatibleSocketIds.Contains(socket, StringComparer.Ordinal)))
                    {
                        Increment(rejected, SectorClusterCandidateErrorCode.SocketMismatch);
                        continue;
                    }

                    if (!source.CompatibleAccessClasses.Contains(sector.Route.AccessClass))
                    {
                        Increment(rejected, SectorClusterCandidateErrorCode.AccessMismatch);
                        continue;
                    }

                    if (sector.QuietCompatible && !source.QuietPoolCompatible)
                    {
                        Increment(rejected, SectorClusterCandidateErrorCode.PacingMismatch);
                        continue;
                    }

                    if (sector.SpecialRegion.Binding != SectorPlannerSpecialRegionBinding.None
                        && !source.SpecialAdjacencyCompatible)
                    {
                        Increment(rejected, SectorClusterCandidateErrorCode.AnchorOverlap);
                        continue;
                    }

                    if (source.FootprintCells.Count < source.MinimumDensityCells
                        || source.FootprintCells.Count > source.MaximumDensityCells)
                    {
                        Increment(rejected, SectorClusterCandidateErrorCode.DensityOutOfPolicy);
                        continue;
                    }

                    var placements = BuildPlacements(sector, source, request.AnchorPlan, out var outOfBounds, out var anchorOverlap);
                    if (placements.Count == 0)
                    {
                        Increment(rejected, anchorOverlap > 0
                            ? SectorClusterCandidateErrorCode.AnchorOverlap
                            : SectorClusterCandidateErrorCode.FootprintOutOfBounds);
                        continue;
                    }

                    var reasons = new List<SectorClusterCandidateReason>
                    {
                        SectorClusterCandidateReason.BiomeCompatible,
                        primaryMatch ? SectorClusterCandidateReason.PacingPrimaryMatch : SectorClusterCandidateReason.PacingCandidateMatch,
                        SectorClusterCandidateReason.RouteSocketCompatible,
                        SectorClusterCandidateReason.AccessCompatible,
                        SectorClusterCandidateReason.FootprintFitsFreeGrid,
                        SectorClusterCandidateReason.AvoidsFixedAnchor,
                        SectorClusterCandidateReason.DensityWithinPolicy,
                    };
                    if (sector.QuietCompatible && source.QuietPoolCompatible)
                        reasons.Add(SectorClusterCandidateReason.QuietPoolCompatible);
                    if (sector.SpecialRegion.Binding != SectorPlannerSpecialRegionBinding.None && source.SpecialAdjacencyCompatible)
                        reasons.Add(SectorClusterCandidateReason.SpecialAdjacencyCompatible);
                    if (request.AnchorPlan.CountForSector(sector.Coordinate) > 0 || source.FootprintCells.Count >= 3)
                        reasons.Add(SectorClusterCandidateReason.ConstraintLargeFirst);

                    var deterministicScore = (primaryMatch ? 1000000 : 500000)
                                             + 100000 + 50000
                                             + (source.FootprintCells.Count * 1000)
                                             - (placements[0].AnchorProximityPenalty * 10)
                                             - source.CatalogOrder - source.VariantOrder;
                    candidates.Add(new SectorClusterCandidate(
                        sector, assignment, source, placements, reasons, deterministicScore));
                    sectorCandidateCount++;
                }

                if (sectorCandidateCount == 0)
                {
                    Add(errors, SectorClusterCandidateErrorCode.NoCandidateForSector, SectorSubject(sector),
                        "No compatible cluster candidate survived the hard gates.");
                    if (Count(rejected, SectorClusterCandidateErrorCode.AnchorOverlap) > sectorAnchorRejectsBefore)
                    {
                        Add(errors, SectorClusterCandidateErrorCode.AnchorOverlap, SectorSubject(sector),
                            "All otherwise compatible approved placements overlap fixed anchors.");
                    }
                }
            }

            if (errors.Count > 0)
                return Failure(errors);

            var provisional = new SectorClusterCandidateSet(
                request.PublicationLabel,
                request.Input.CanonicalDigest,
                request.AnchorPlan.CanonicalDigest,
                request.Input.Sectors.Count,
                candidates,
                rejected,
                string.Empty);
            var digest = SectorClusterCandidateCanonicalDigest.Compute(provisional);
            if (request.ExpectedCanonicalDigest.Length != 0
                && !string.Equals(request.ExpectedCanonicalDigest, digest, StringComparison.Ordinal))
            {
                Add(errors, SectorClusterCandidateErrorCode.NonCanonicalPublication, "digest",
                    "Candidate set digest does not match the expected canonical digest.");
                return Failure(errors);
            }

            var candidateSet = new SectorClusterCandidateSet(
                request.PublicationLabel,
                request.Input.CanonicalDigest,
                request.AnchorPlan.CanonicalDigest,
                request.Input.Sectors.Count,
                candidates,
                rejected,
                digest);
            return new SectorClusterCandidateBuildResult(candidateSet, Array.Empty<SectorClusterCandidateError>());
        }

        private static void ValidateRequest(
            SectorClusterCandidateBuildRequest request,
            ICollection<SectorClusterCandidateError> errors)
        {
            if (request == null)
            {
                Add(errors, SectorClusterCandidateErrorCode.MissingInput, "request", "Candidate build request is required.");
                return;
            }
            if (request.Input == null)
                Add(errors, SectorClusterCandidateErrorCode.MissingInput, "input", "SectorPlannerInput is required.");
            if (request.AnchorPlan == null)
                Add(errors, SectorClusterCandidateErrorCode.MissingAnchorPlan, "anchorPlan", "SectorFixedAnchorPlan is required.");
            if (request.Assignments.Count == 0)
                Add(errors, SectorClusterCandidateErrorCode.MissingAssignment, "assignments", "Matching pacing assignments are required.");
            if (request.ClusterCatalog.Count == 0)
                Add(errors, SectorClusterCandidateErrorCode.MissingClusterCatalog, "clusterCatalog", "A public cluster source projection is required.");
            if (!string.Equals(request.PublicationLabel, ReferenceCandidatePublicationLabel, StringComparison.Ordinal))
                Add(errors, SectorClusterCandidateErrorCode.NonCanonicalPublication, "publicationLabel", "Publication must be marked REFERENCE CLUSTER CANDIDATE.");
            if (request.SolverMutationCount != 0)
                Add(errors, SectorClusterCandidateErrorCode.SolverMutationClaim, "solver", "Candidate build cannot invoke or claim a solver mutation.");
            if (request.RandomDrawCount != 0)
                Add(errors, SectorClusterCandidateErrorCode.RngMutationClaim, "rng", "Candidate build cannot draw RNG.");
            if (request.TileWriteCount != 0)
                Add(errors, SectorClusterCandidateErrorCode.TileMutationClaim, "tile", "Candidate build cannot write tiles.");

            if (request.Input != null && request.AnchorPlan != null)
            {
                if (!string.Equals(request.AnchorPlan.PlannerInputDigest, request.Input.CanonicalDigest, StringComparison.Ordinal)
                    || request.AnchorPlan.SectorCount != request.Input.Sectors.Count
                    || !request.AnchorPlan.Map14_03HandoffReady
                    || !string.Equals(request.AnchorPlan.RouteIdentityBeforeDigest, request.AnchorPlan.RouteIdentityAfterDigest, StringComparison.Ordinal)
                    || !string.Equals(request.AnchorPlan.BoundaryIdentityBeforeDigest, request.AnchorPlan.BoundaryIdentityAfterDigest, StringComparison.Ordinal)
                    || !string.Equals(request.AnchorPlan.SiteIdentityBeforeDigest, request.AnchorPlan.SiteIdentityAfterDigest, StringComparison.Ordinal)
                    || !string.Equals(request.AnchorPlan.SpecialIdentityBeforeDigest, request.AnchorPlan.SpecialIdentityAfterDigest, StringComparison.Ordinal))
                {
                    Add(errors, SectorClusterCandidateErrorCode.SectorMismatch, "anchorPlan",
                        "Anchor plan must match the planner input and preserve all public source identities.");
                }
            }
        }

        private static IReadOnlyList<SectorClusterSourceProjection> ValidateCatalog(
            IReadOnlyList<SectorClusterSourceProjection> sources,
            ICollection<SectorClusterCandidateError> errors)
        {
            var valid = new List<SectorClusterSourceProjection>();
            var identities = new HashSet<string>(StringComparer.Ordinal);
            foreach (var source in sources ?? Array.Empty<SectorClusterSourceProjection>())
            {
                var subject = source.ClusterId.Value + "/" + source.VariantId.Value;
                var identity = subject;
                if (!identities.Add(identity))
                {
                    Add(errors, SectorClusterCandidateErrorCode.DuplicateCandidate, subject,
                        "Cluster and variant identity must be unique in the source projection.");
                    continue;
                }

                var footprintValid = source.ClusterId.Value.Length != 0
                                     && source.VariantId.Value.Length != 0
                                     && source.FootprintCells.Count >= 2
                                     && source.FootprintCells.Count <= 5
                                     && source.FootprintCells.Distinct().Count() == source.FootprintCells.Count
                                     && source.FootprintCells.All(value => value.X >= 0 && value.X < WorldGenConstants.MicroChunkColumnsPerSector
                                                                               && value.Y >= 0 && value.Y < WorldGenConstants.MicroChunkRowsPerSector)
                                     && source.ApprovedOrigins.Count > 0
                                     && IsConnected(source.FootprintCells)
                                     && source.MinimumDensityCells >= 2
                                     && source.MaximumDensityCells <= 5
                                     && source.MinimumDensityCells <= source.MaximumDensityCells
                                     && source.CatalogOrder >= 0
                                     && source.VariantOrder >= 0;
                if (!footprintValid)
                {
                    Add(errors, SectorClusterCandidateErrorCode.InvalidFootprint, subject,
                        "Footprint must contain 2..5 unique connected 4x4-grid cells and at least one approved origin.");
                    continue;
                }
                if (source.CompatiblePacingRoles.Count == 0
                    || source.CompatibleRouteTypes.Count == 0
                    || source.CompatibleAccessClasses.Count == 0)
                {
                    Add(errors, SectorClusterCandidateErrorCode.NonCanonicalPublication, subject,
                        "Biome/pacing/route/access compatibility evidence must be explicit.");
                    continue;
                }
                valid.Add(source);
            }
            return new ReadOnlyCollection<SectorClusterSourceProjection>(valid
                .OrderBy(value => value.CatalogOrder).ThenBy(value => value.VariantOrder)
                .ThenBy(value => value.ClusterId).ThenBy(value => value.VariantId).ToArray());
        }

        private static Dictionary<int, SectorPacingAssignment> ValidateAssignments(
            SectorPlannerInput input,
            IReadOnlyList<SectorPacingAssignment> assignments,
            ICollection<SectorClusterCandidateError> errors)
        {
            var result = new Dictionary<int, SectorPacingAssignment>();
            foreach (var assignment in assignments)
            {
                var index = (assignment.Coordinate.Y * WorldGenConstants.SectorColumns) + assignment.Coordinate.X;
                if (!input.TryGetSector(assignment.Coordinate, out var sector) || sector.SectorIndex != index)
                {
                    Add(errors, SectorClusterCandidateErrorCode.SectorMismatch, assignment.Coordinate.ToString(),
                        "Assignment coordinate does not match a published sector.");
                    continue;
                }
                if (result.ContainsKey(index))
                {
                    Add(errors, SectorClusterCandidateErrorCode.MissingAssignment, SectorSubject(sector),
                        "Pacing assignment must occur exactly once per sector.");
                    continue;
                }
                var expected = SectorPacingRolePlanner.Assign(input, assignment.Coordinate);
                if (assignment.PrimaryRole != expected.PrimaryRole
                    || !string.Equals(assignment.CanonicalDigest, expected.CanonicalDigest, StringComparison.Ordinal)
                    || !string.Equals(assignment.SourceIdentityDigest, expected.SourceIdentityDigest, StringComparison.Ordinal))
                {
                    Add(errors, SectorClusterCandidateErrorCode.MissingAssignment, SectorSubject(sector),
                        "Assignment must preserve the current public pacing publication.");
                    continue;
                }
                result.Add(index, assignment);
            }

            foreach (var sector in input.Sectors)
            {
                if (!result.ContainsKey(sector.SectorIndex))
                    Add(errors, SectorClusterCandidateErrorCode.MissingAssignment, SectorSubject(sector),
                        "Each published sector requires exactly one pacing assignment.");
            }
            return result;
        }

        private static IReadOnlyList<SectorClusterFootprintPlacement> BuildPlacements(
            SectorPlannerSectorSnapshot sector,
            SectorClusterSourceProjection source,
            SectorFixedAnchorPlan anchorPlan,
            out int outOfBounds,
            out int anchorOverlap)
        {
            outOfBounds = 0;
            anchorOverlap = 0;
            var result = new List<SectorClusterFootprintPlacement>();
            var blockingCells = SectorClusterAnchorUtility.BlockingCells(anchorPlan, sector.SectorIndex);
            foreach (var origin in source.ApprovedOrigins)
            {
                var cells = source.FootprintCells.Select(value =>
                    new SectorClusterFootprintCell(origin.X + value.X, origin.Y + value.Y)).ToArray();
                if (cells.Any(value => value.X < 0 || value.X >= WorldGenConstants.MicroChunkColumnsPerSector
                                                   || value.Y < 0 || value.Y >= WorldGenConstants.MicroChunkRowsPerSector))
                {
                    outOfBounds++;
                    continue;
                }
                if (cells.Any(blockingCells.Contains))
                {
                    anchorOverlap++;
                    continue;
                }
                var proximity = cells.Sum(cell => blockingCells.Count(blocked =>
                    Math.Abs(blocked.X - cell.X) + Math.Abs(blocked.Y - cell.Y) == 1));
                result.Add(new SectorClusterFootprintPlacement(origin.X, origin.Y, cells, proximity));
            }
            return new ReadOnlyCollection<SectorClusterFootprintPlacement>(result
                .OrderBy(value => value.AnchorProximityPenalty).ThenBy(value => value.OriginY).ThenBy(value => value.OriginX).ToArray());
        }

        private static bool IsConnected(IReadOnlyList<SectorClusterFootprintCell> cells)
        {
            if (cells == null || cells.Count == 0) return false;
            var set = new HashSet<SectorClusterFootprintCell>(cells);
            var visited = new HashSet<SectorClusterFootprintCell>();
            var queue = new Queue<SectorClusterFootprintCell>();
            queue.Enqueue(cells[0]);
            visited.Add(cells[0]);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var next in new[]
                {
                    new SectorClusterFootprintCell(current.X - 1, current.Y),
                    new SectorClusterFootprintCell(current.X + 1, current.Y),
                    new SectorClusterFootprintCell(current.X, current.Y - 1),
                    new SectorClusterFootprintCell(current.X, current.Y + 1),
                })
                {
                    if (set.Contains(next) && visited.Add(next)) queue.Enqueue(next);
                }
            }
            return visited.Count == set.Count;
        }

        private static int Count(IDictionary<SectorClusterCandidateErrorCode, int> counts, SectorClusterCandidateErrorCode code)
            => counts.TryGetValue(code, out var value) ? value : 0;

        private static void Increment(IDictionary<SectorClusterCandidateErrorCode, int> counts, SectorClusterCandidateErrorCode code)
            => counts[code] = Count(counts, code) + 1;

        private static string SectorSubject(SectorPlannerSectorSnapshot sector)
            => sector.SectorIndex.ToString("D3", CultureInfo.InvariantCulture) + "@"
               + sector.Coordinate.X.ToString(CultureInfo.InvariantCulture) + ","
               + sector.Coordinate.Y.ToString(CultureInfo.InvariantCulture);

        private static void Add(
            ICollection<SectorClusterCandidateError> errors,
            SectorClusterCandidateErrorCode code,
            string subject,
            string detail)
            => errors.Add(new SectorClusterCandidateError(code, subject, detail));

        private static SectorClusterCandidateBuildResult Failure(IEnumerable<SectorClusterCandidateError> errors)
            => new SectorClusterCandidateBuildResult(null, errors);
    }

    internal static class SectorClusterAnchorUtility
    {
        internal static bool IsBlocking(SectorFixedAnchor anchor)
        {
            return anchor.Kind != SectorFixedAnchorKind.ReferenceOnlyMarker
                   && anchor.Kind != SectorFixedAnchorKind.BoundaryWarning;
        }

        internal static HashSet<SectorClusterFootprintCell> BlockingCells(SectorFixedAnchorPlan plan, int sectorIndex)
        {
            var result = new HashSet<SectorClusterFootprintCell>();
            if (plan == null) return result;
            foreach (var anchor in plan.Anchors.Where(value => value.SectorIndex == sectorIndex && IsBlocking(value)))
            {
                for (var y = 0; y < WorldGenConstants.MicroChunkRowsPerSector; y++)
                for (var x = 0; x < WorldGenConstants.MicroChunkColumnsPerSector; x++)
                {
                    var cell = new SectorClusterFootprintCell(x, y);
                    if (cell.ToTileRect().Overlaps(anchor.Rect)) result.Add(cell);
                }
            }
            return result;
        }
    }
}
