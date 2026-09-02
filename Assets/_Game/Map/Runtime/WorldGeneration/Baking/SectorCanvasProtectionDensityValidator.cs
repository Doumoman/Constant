using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Baking
{
    public static class SectorCanvasProtectionDensityValidator
    {
        public const string ReferencePublicationLabel =
            "REFERENCE PROTECTION CLEANUP DENSITY REPORT";

        public static ProtectionDensityResult Validate(SectorFinalCanvasLayerPlan plan)
        {
            var failures = new List<ProtectionDensityFailure>();
            var intrusions = new List<SectorCanvasProtectionIntrusion>();
            if (plan == null)
            {
                Add(failures, ProtectionDensityFailureCode.MissingPlan, "plan",
                    "A successful MAP16_01 final canvas plan is required.");
                return Failed(null, failures, intrusions);
            }

            ValidatePlan(plan, failures);
            if (failures.Count > 0) return Failed(plan, failures, intrusions);

            var cells = plan.Cells.OrderBy(value => value).ToArray();
            var byIndex = cells.ToDictionary(value => value.RowMajorIndex);
            var authority = cells.ToDictionary(
                value => value.RowMajorIndex,
                ClassifyAuthority);

            DetectProtectionIntrusions(cells, authority, intrusions);
            var unownedRegions = BuildUnownedAirRegions(cells, byIndex, authority);
            var cleanupCandidates = BuildCleanupCandidates(
                cells, byIndex, authority, unownedRegions);
            var projection = BuildCleanupProjection(cleanupCandidates, authority);
            var solidCellCount = cells.Count(value =>
                value.Winner(FinalCanvasLayerKind.Terrain).CellKind == FinalCanvasCellKind.Solid);
            var reachableCellCount = CountAbstractReachableCells(cells, byIndex, authority);
            var solidPermille = Permille(solidCellCount);
            var reachablePermille = Permille(reachableCellCount);
            var largestWidth = unownedRegions.Count == 0 ? 0 : unownedRegions.Max(value => value.Width);
            var largestHeight = unownedRegions.Count == 0 ? 0 : unownedRegions.Max(value => value.Height);
            var largestArea = unownedRegions.Count == 0 ? 0 : unownedRegions.Max(value => value.Area);
            var unownedDimensionsSafe = largestWidth <= SectorCanvasProtectionDensityReport.UnownedAirMaximumWidth &&
                                        largestHeight <= SectorCanvasProtectionDensityReport.UnownedAirMaximumHeight;

            var budgets = new[]
            {
                new SectorCanvasDensityBudget(
                    DensityBudgetKind.SolidDensity, solidPermille,
                    SectorCanvasProtectionDensityReport.SolidMinimumPermille,
                    SectorCanvasProtectionDensityReport.SolidMaximumPermille),
                new SectorCanvasDensityBudget(
                    DensityBudgetKind.ReachableDensity, reachablePermille,
                    SectorCanvasProtectionDensityReport.ReachableMinimumPermille,
                    SectorCanvasProtectionDensityReport.ReachableMaximumPermille),
                new SectorCanvasDensityBudget(
                    DensityBudgetKind.UnownedAirMaxBox, largestArea, 0,
                    SectorCanvasProtectionDensityReport.UnownedAirMaximumArea,
                    unownedDimensionsSafe),
                new SectorCanvasDensityBudget(
                    DensityBudgetKind.ProtectionIntrusion, intrusions.Count, 0, 0),
                new SectorCanvasDensityBudget(
                    DensityBudgetKind.CleanupProjectionSafety,
                    projection.ProtectedAuthorityChangedCount, 0, 0),
            };

            foreach (var intrusion in intrusions.OrderBy(value => value))
                Add(failures, ProtectionDensityFailureCode.ProtectionIntrusion,
                    intrusion.Coordinate + ":" + intrusion.Kind, intrusion.Reason);
            foreach (var budget in budgets.Where(value =>
                         value.Verdict == DensityBudgetVerdict.Fail &&
                         (value.Kind == DensityBudgetKind.SolidDensity ||
                          value.Kind == DensityBudgetKind.ReachableDensity)))
                Add(failures, ProtectionDensityFailureCode.DensityOutOfRange,
                    budget.Kind.ToString(), "Observed density is outside its approved permille envelope.");
            foreach (var region in unownedRegions.Where(value =>
                         value.Kind == UnownedAirRegionKind.Oversized))
                Add(failures, ProtectionDensityFailureCode.UnownedAirTooLarge,
                    region.MinimumCoordinate.ToString(),
                    "Unowned AIR region exceeds width 8, height 6, or area 48.");
            if (!projection.IsSafe)
                Add(failures, ProtectionDensityFailureCode.UnsafeCleanupProjection,
                    "cleanupProjection",
                    "Cleanup projection cannot change protected, fixed, boundary, or Special cells.");

            var protectedOpenCount = authority.Count(pair => pair.Value.ProtectedOpen);
            var fixedCount = authority.Count(pair => pair.Value.Fixed);
            var boundaryCount = authority.Count(pair => pair.Value.Boundary);
            var specialCount = authority.Count(pair => pair.Value.SpecialEntrance);
            var outputDigest = ProtectionDensityDigest.ComputeOutput(
                plan, protectedOpenCount, fixedCount, boundaryCount, specialCount,
                solidCellCount, reachableCellCount, intrusions, cleanupCandidates,
                projection, budgets, unownedRegions);
            if (!ProtectionDensityDigest.IsLowerHexSha256(ProtectionDensityDigest.ComputeInput(plan)) ||
                !ProtectionDensityDigest.IsLowerHexSha256(outputDigest))
                Add(failures, ProtectionDensityFailureCode.InvalidDigest, "digest",
                    "Input and output digests must be lowercase SHA-256 values.");

            if (failures.Count > 0) return Failed(plan, failures, intrusions);

            var report = new SectorCanvasProtectionDensityReport(
                plan, protectedOpenCount, fixedCount, boundaryCount, specialCount,
                solidCellCount, reachableCellCount, intrusions, cleanupCandidates,
                projection, budgets, unownedRegions, outputDigest);
            return new ProtectionDensityResult(
                plan, report, Array.Empty<ProtectionDensityFailure>(),
                Array.Empty<SectorCanvasProtectionIntrusion>());
        }

        private static void ValidatePlan(
            SectorFinalCanvasLayerPlan plan,
            ICollection<ProtectionDensityFailure> failures)
        {
            if (plan.Request == null || plan.Request.PublicationLabel !=
                SectorCanvasLayerFinalizer.ReferencePublicationLabel)
                Add(failures, ProtectionDensityFailureCode.InvalidCanvas, "publication",
                    "MAP16_01 reference or approved final canvas publication is required.");
            if (plan.ObservedCellCount != SectorCanvasProtectionDensityReport.CellCount ||
                plan.UniqueCoordinateCount != SectorCanvasProtectionDensityReport.CellCount ||
                plan.OutOfBoundsCellCount != 0)
                Add(failures, ProtectionDensityFailureCode.InvalidCanvas, "coordinates",
                    "Final canvas must contain exactly 1536 unique in-bounds cells.");
            if (plan.RequiredLayerKindCount != SectorCanvasProtectionDensityReport.RequiredLayerCount ||
                plan.CoveredLayerKindCount != SectorCanvasProtectionDensityReport.RequiredLayerCount ||
                plan.MissingLayerKindCount != 0 ||
                plan.Cells.Any(cell => cell.Winners.Count !=
                                       SectorCanvasProtectionDensityReport.RequiredLayerCount ||
                                       cell.Winners.Select(value => value.Layer).Distinct().Count() !=
                                       SectorCanvasProtectionDensityReport.RequiredLayerCount))
                Add(failures, ProtectionDensityFailureCode.MissingLayerData, "layers",
                    "Every final canvas cell must publish all seven unique layers.");
            if (plan.WinningClaimsWithSourceOwnerCount != plan.WinningClaimCount ||
                plan.WinningClaimsWithProvenanceCount != plan.WinningClaimCount ||
                plan.Cells.SelectMany(value => value.Winners).Any(value =>
                    value.SourceOwner == FinalCanvasSourceOwner.Unknown ||
                    string.IsNullOrEmpty(value.ProvenanceId) ||
                    string.IsNullOrEmpty(value.ClaimId)))
                Add(failures, ProtectionDensityFailureCode.MissingSourceEvidence, "sourceEvidence",
                    "Every winning layer claim requires source owner, provenance, and claim identity.");
            if (!FinalCanvasLayerDigest.IsLowerHexSha256(plan.InputDigest) ||
                !FinalCanvasLayerDigest.IsLowerHexSha256(plan.OutputDigest))
                Add(failures, ProtectionDensityFailureCode.InvalidDigest, "sourceDigest",
                    "MAP16_01 input and output identities must be lowercase SHA-256 values.");

            var forbidden = new[]
            {
                plan.NewRngDrawCount, plan.SliceCreationCount, plan.GeneratedFileWriteCount,
                plan.TilemapMutationCount, plan.SceneMutationCount, plan.PrefabMutationCount,
                plan.GameObjectMutationCount, plan.GameplaySpawnCount,
                plan.ProductionSeedApprovalCount, plan.SectorRerollCount,
                plan.FallbackCarveCount, plan.FullRegressionCount,
            };
            if (forbidden.Any(value => value != 0))
                Add(failures, ProtectionDensityFailureCode.ForbiddenOperation, "operationCounters",
                    "Validation cannot draw RNG, slice, write, mutate, spawn, reroll, carve, approve production, or run regression.");
        }

        private static CellAuthority ClassifyAuthority(FinalCanvasCell cell)
        {
            var claims = cell.Winners;
            var protection = cell.Winner(FinalCanvasLayerKind.Protection);
            var protectedOpen = claims.Any(value =>
                value.Protection == FinalCanvasProtectionKind.MandatoryRouteProtectedOpen ||
                value.Priority == FinalCanvasClaimPriority.MandatoryRouteProtectedOpen) ||
                (protection.CellKind == FinalCanvasCellKind.ProtectedOpen &&
                 protection.SourceOwner == FinalCanvasSourceOwner.MandatoryRoute);
            var fixedCell = claims.Any(value =>
                value.Protection == FinalCanvasProtectionKind.FixedSlice ||
                value.Protection == FinalCanvasProtectionKind.SpecialFixedShell ||
                value.Priority == FinalCanvasClaimPriority.FixedSlice ||
                value.Priority == FinalCanvasClaimPriority.SpecialFixedShell ||
                value.SourceOwner == FinalCanvasSourceOwner.FixedSlice);
            var boundary = claims.Any(value =>
                value.Protection == FinalCanvasProtectionKind.BoundaryAperture ||
                value.Priority == FinalCanvasClaimPriority.BoundaryAperture ||
                value.SourceOwner == FinalCanvasSourceOwner.Boundary);
            var special = claims.Any(value =>
                value.Protection == FinalCanvasProtectionKind.SpecialEntranceBuffer ||
                value.Priority == FinalCanvasClaimPriority.SpecialEntranceBuffer) ||
                (claims.Any(value => value.SourceOwner == FinalCanvasSourceOwner.SpecialRegion) &&
                 protection.CellKind == FinalCanvasCellKind.ProtectedOpen);
            var protectedFact = protectedOpen || fixedCell || boundary || special ||
                                claims.Any(value => value.IsProtected ||
                                    value.Protection != FinalCanvasProtectionKind.None);
            var explicitProtection = protection.CellKind == FinalCanvasCellKind.ProtectedOpen &&
                                     protection.IsProtected &&
                                     protection.Protection != FinalCanvasProtectionKind.None;
            return new CellAuthority(
                protectedOpen, fixedCell, boundary, special,
                protectedFact, explicitProtection);
        }

        private static void DetectProtectionIntrusions(
            IEnumerable<FinalCanvasCell> cells,
            IReadOnlyDictionary<int, CellAuthority> authority,
            ICollection<SectorCanvasProtectionIntrusion> intrusions)
        {
            foreach (var cell in cells.OrderBy(value => value))
            {
                var facts = authority[cell.RowMajorIndex];
                var terrain = cell.Winner(FinalCanvasLayerKind.Terrain);
                var material = cell.Winner(FinalCanvasLayerKind.Material);
                var hazard = cell.Winner(FinalCanvasLayerKind.Hazard);
                var protection = cell.Winner(FinalCanvasLayerKind.Protection);

                if (facts.ProtectedOpen &&
                    (terrain.CellKind == FinalCanvasCellKind.Solid ||
                     terrain.CellKind == FinalCanvasCellKind.Blocked))
                    AddIntrusion(intrusions, ProtectionIntrusionKind.ProtectedOpenSolidIntrusion,
                        cell, terrain, "Protected-open terrain cannot be Solid or Blocked.");
                if (facts.ProtectedOpen && IsHazardBlocking(hazard.CellKind))
                    AddIntrusion(intrusions, ProtectionIntrusionKind.ProtectedOpenHazardIntrusion,
                        cell, hazard, "Protected-open cells cannot carry a blocking Hazard value.");
                if (facts.Boundary &&
                    (IsTerrainBlocking(terrain.CellKind) || IsHazardBlocking(hazard.CellKind)))
                    AddIntrusion(intrusions, ProtectionIntrusionKind.BoundaryApertureBlocked,
                        cell, IsTerrainBlocking(terrain.CellKind) ? terrain : hazard,
                        "Boundary aperture cannot be blocked or removed.");
                if (facts.Fixed && IsFixedLayerOverwritten(terrain, material, hazard, out var overwritten))
                    AddIntrusion(intrusions, ProtectionIntrusionKind.FixedSliceOverwritten,
                        cell, overwritten,
                        "Fixed slice terrain, material, or hazard authority was replaced by a weaker source.");
                if (facts.SpecialEntrance &&
                    (IsTerrainBlocking(terrain.CellKind) || IsHazardBlocking(hazard.CellKind)))
                    AddIntrusion(intrusions, ProtectionIntrusionKind.SpecialEntranceBlocked,
                        cell, IsTerrainBlocking(terrain.CellKind) ? terrain : hazard,
                        "Special entrance buffer cannot be blocked by Solid or Hazard.");
                if (facts.Protected && !facts.HasExplicitProtection)
                    AddIntrusion(intrusions, ProtectionIntrusionKind.ProtectionLayerMissing,
                        cell, protection,
                        "Protected authority requires an explicit protected Protection layer winner.");
            }
        }

        private static bool IsFixedLayerOverwritten(
            FinalCanvasLayerClaim terrain,
            FinalCanvasLayerClaim material,
            FinalCanvasLayerClaim hazard,
            out FinalCanvasLayerClaim overwritten)
        {
            var candidates = new[] { terrain, material, hazard };
            overwritten = candidates.FirstOrDefault(value =>
                value != null &&
                (value.Layer == FinalCanvasLayerKind.Terrain ||
                 value.CellKind != FinalCanvasCellKind.None) &&
                (value.SourceOwner != FinalCanvasSourceOwner.FixedSlice ||
                 value.Priority != FinalCanvasClaimPriority.FixedSlice));
            return overwritten != null;
        }

        private static List<SectorCanvasCleanupCandidate> BuildCleanupCandidates(
            IEnumerable<FinalCanvasCell> cells,
            IReadOnlyDictionary<int, FinalCanvasCell> byIndex,
            IReadOnlyDictionary<int, CellAuthority> authority,
            IEnumerable<SectorCanvasUnownedAirRegion> unownedRegions)
        {
            var candidates = new List<SectorCanvasCleanupCandidate>();
            foreach (var cell in cells.OrderBy(value => value))
            {
                if (!TryNeighbors(cell, byIndex, out var left, out var right, out var below, out var above))
                    continue;
                var terrain = cell.Winner(FinalCanvasLayerKind.Terrain);
                var solid = terrain.CellKind == FinalCanvasCellKind.Solid;
                var air = terrain.CellKind == FinalCanvasCellKind.Air;
                var leftSolid = IsSolid(left);
                var rightSolid = IsSolid(right);
                var belowSolid = IsSolid(below);
                var aboveSolid = IsSolid(above);

                if (solid && !leftSolid && !rightSolid && !belowSolid && !aboveSolid)
                    candidates.Add(Candidate(CleanupCandidateKind.SingleCellSolidNoise,
                        cell, FinalCanvasCellKind.Air,
                        "Isolated one-cell Solid is surrounded by non-solid terrain."));
                if (air && leftSolid && rightSolid && belowSolid && aboveSolid)
                    candidates.Add(Candidate(CleanupCandidateKind.SingleCellAirNoise,
                        cell, FinalCanvasCellKind.Solid,
                        "Isolated one-cell AIR is surrounded by Solid terrain."));
                if (solid && aboveSolid && !belowSolid && !leftSolid && !rightSolid)
                    candidates.Add(Candidate(CleanupCandidateKind.HeadSnag,
                        cell, FinalCanvasCellKind.Air,
                        "One-tile ceiling protrusion hangs into an abstract reachable corridor."));
                if (air && leftSolid && rightSolid && belowSolid && !aboveSolid)
                    candidates.Add(Candidate(CleanupCandidateKind.ShallowPit,
                        cell, FinalCanvasCellKind.Solid,
                        "One-cell AIR depression forms a shallow pit in the floor projection."));
                if (solid && belowSolid && !aboveSolid && leftSolid != rightSolid)
                    candidates.Add(Candidate(CleanupCandidateKind.OneCellLip,
                        cell, FinalCanvasCellKind.Air,
                        "One-cell Solid lip creates an avoidable abstract snag."));
            }

            foreach (var region in unownedRegions.OrderBy(value => value))
            {
                var cell = byIndex[region.MinimumCoordinate.RowMajorIndex];
                candidates.Add(Candidate(CleanupCandidateKind.UnownedAirPocket,
                    cell, FinalCanvasCellKind.Ground,
                    "AIR region has no route, boundary, Special, activity, event, marker, or protected purpose."));
            }
            return candidates.OrderBy(value => value).ToList();
        }

        private static SectorCanvasCleanupCandidate Candidate(
            CleanupCandidateKind kind,
            FinalCanvasCell cell,
            FinalCanvasCellKind projected,
            string reason)
        {
            var terrain = cell.Winner(FinalCanvasLayerKind.Terrain);
            return new SectorCanvasCleanupCandidate(
                kind, cell.Coordinate, terrain.CellKind, projected,
                terrain.SourceOwner, terrain.ClaimId, reason);
        }

        private static SectorCanvasCleanupProjection BuildCleanupProjection(
            IEnumerable<SectorCanvasCleanupCandidate> candidates,
            IReadOnlyDictionary<int, CellAuthority> authority)
        {
            var changed = new List<FinalCanvasCellCoordinate>();
            var rejected = 0;
            foreach (var candidate in candidates.OrderBy(value => value))
            {
                if (candidate.CurrentCellKind == candidate.ProjectedCellKind) continue;
                var facts = authority[candidate.Coordinate.RowMajorIndex];
                if (facts.Protected)
                {
                    rejected++;
                    continue;
                }
                changed.Add(candidate.Coordinate);
            }
            var uniqueChanged = changed.Distinct().ToArray();
            return new SectorCanvasCleanupProjection(
                uniqueChanged,
                uniqueChanged.Count(value => authority[value.RowMajorIndex].ProtectedOpen),
                uniqueChanged.Count(value => authority[value.RowMajorIndex].Fixed),
                uniqueChanged.Count(value => authority[value.RowMajorIndex].Boundary),
                uniqueChanged.Count(value => authority[value.RowMajorIndex].SpecialEntrance),
                rejected);
        }

        private static List<SectorCanvasUnownedAirRegion> BuildUnownedAirRegions(
            IEnumerable<FinalCanvasCell> cells,
            IReadOnlyDictionary<int, FinalCanvasCell> byIndex,
            IReadOnlyDictionary<int, CellAuthority> authority)
        {
            var remaining = new HashSet<int>(cells.Where(value =>
                value.Winner(FinalCanvasLayerKind.Terrain).CellKind == FinalCanvasCellKind.Air &&
                !HasPurpose(value, authority[value.RowMajorIndex]))
                .Select(value => value.RowMajorIndex));
            var regions = new List<SectorCanvasUnownedAirRegion>();
            while (remaining.Count > 0)
            {
                var start = remaining.Min();
                var queue = new Queue<int>();
                var indices = new List<int>();
                remaining.Remove(start);
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    indices.Add(current);
                    foreach (var neighbor in NeighborIndices(byIndex[current].Coordinate))
                    {
                        if (!remaining.Remove(neighbor)) continue;
                        queue.Enqueue(neighbor);
                    }
                }

                var coordinates = indices.Select(index => byIndex[index].Coordinate).ToArray();
                var minimum = new FinalCanvasCellCoordinate(
                    coordinates.Min(value => value.X), coordinates.Min(value => value.Y));
                var maximum = new FinalCanvasCellCoordinate(
                    coordinates.Max(value => value.X), coordinates.Max(value => value.Y));
                var width = maximum.X - minimum.X + 1;
                var height = maximum.Y - minimum.Y + 1;
                var kind = width <= SectorCanvasProtectionDensityReport.UnownedAirMaximumWidth &&
                           height <= SectorCanvasProtectionDensityReport.UnownedAirMaximumHeight &&
                           coordinates.Length <= SectorCanvasProtectionDensityReport.UnownedAirMaximumArea
                    ? UnownedAirRegionKind.Bounded
                    : UnownedAirRegionKind.Oversized;
                regions.Add(new SectorCanvasUnownedAirRegion(
                    minimum, maximum, coordinates.Length, kind));
            }
            return regions.OrderBy(value => value).ToList();
        }

        private static int CountAbstractReachableCells(
            IEnumerable<FinalCanvasCell> cells,
            IReadOnlyDictionary<int, FinalCanvasCell> byIndex,
            IReadOnlyDictionary<int, CellAuthority> authority)
        {
            var eligible = new HashSet<int>(cells.Where(value =>
                IsReachableCandidate(value, authority[value.RowMajorIndex]))
                .Select(value => value.RowMajorIndex));
            if (eligible.Count == 0) return 0;
            var seeds = cells.Where(value => eligible.Contains(value.RowMajorIndex) &&
                (authority[value.RowMajorIndex].ProtectedOpen ||
                 authority[value.RowMajorIndex].Boundary ||
                 authority[value.RowMajorIndex].SpecialEntrance))
                .Select(value => value.RowMajorIndex).OrderBy(value => value).ToArray();
            if (seeds.Length == 0) seeds = new[] { eligible.Min() };

            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            foreach (var seed in seeds)
            {
                if (!visited.Add(seed)) continue;
                queue.Enqueue(seed);
            }
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var neighbor in NeighborIndices(byIndex[current].Coordinate))
                {
                    if (!eligible.Contains(neighbor) || !visited.Add(neighbor)) continue;
                    queue.Enqueue(neighbor);
                }
            }
            return visited.Count;
        }

        private static bool IsReachableCandidate(FinalCanvasCell cell, CellAuthority authority)
        {
            var terrain = cell.Winner(FinalCanvasLayerKind.Terrain).CellKind;
            var affordance = cell.Winner(FinalCanvasLayerKind.Affordance).CellKind;
            return !IsTerrainBlocking(terrain) &&
                   (affordance == FinalCanvasCellKind.Traversable ||
                    authority.ProtectedOpen || authority.Boundary || authority.SpecialEntrance);
        }

        private static bool HasPurpose(FinalCanvasCell cell, CellAuthority authority)
        {
            if (authority.Protected) return true;
            if (cell.Winner(FinalCanvasLayerKind.Affordance).CellKind ==
                FinalCanvasCellKind.Traversable) return true;
            if (cell.Winner(FinalCanvasLayerKind.Marker).CellKind ==
                FinalCanvasCellKind.Marker) return true;
            return cell.Winners.Any(value =>
                value.SourceOwner == FinalCanvasSourceOwner.MandatoryRoute ||
                value.SourceOwner == FinalCanvasSourceOwner.Boundary ||
                value.SourceOwner == FinalCanvasSourceOwner.SpecialRegion ||
                value.SourceOwner == FinalCanvasSourceOwner.Activity ||
                value.SourceOwner == FinalCanvasSourceOwner.EventOverlay ||
                value.SourceOwner == FinalCanvasSourceOwner.FixedSlice);
        }

        private static bool TryNeighbors(
            FinalCanvasCell cell,
            IReadOnlyDictionary<int, FinalCanvasCell> byIndex,
            out FinalCanvasCell left,
            out FinalCanvasCell right,
            out FinalCanvasCell below,
            out FinalCanvasCell above)
        {
            var x = cell.Coordinate.X;
            var y = cell.Coordinate.Y;
            if (x <= 0 || x >= SectorCanvasProtectionDensityReport.SectorWidth - 1 ||
                y <= 0 || y >= SectorCanvasProtectionDensityReport.SectorHeight - 1)
            {
                left = right = below = above = null;
                return false;
            }
            left = byIndex[(y * SectorCanvasProtectionDensityReport.SectorWidth) + x - 1];
            right = byIndex[(y * SectorCanvasProtectionDensityReport.SectorWidth) + x + 1];
            below = byIndex[((y - 1) * SectorCanvasProtectionDensityReport.SectorWidth) + x];
            above = byIndex[((y + 1) * SectorCanvasProtectionDensityReport.SectorWidth) + x];
            return true;
        }

        private static IEnumerable<int> NeighborIndices(FinalCanvasCellCoordinate coordinate)
        {
            if (coordinate.X > 0) yield return coordinate.RowMajorIndex - 1;
            if (coordinate.X < SectorCanvasProtectionDensityReport.SectorWidth - 1)
                yield return coordinate.RowMajorIndex + 1;
            if (coordinate.Y > 0)
                yield return coordinate.RowMajorIndex - SectorCanvasProtectionDensityReport.SectorWidth;
            if (coordinate.Y < SectorCanvasProtectionDensityReport.SectorHeight - 1)
                yield return coordinate.RowMajorIndex + SectorCanvasProtectionDensityReport.SectorWidth;
        }

        private static bool IsSolid(FinalCanvasCell cell) =>
            cell.Winner(FinalCanvasLayerKind.Terrain).CellKind == FinalCanvasCellKind.Solid;
        private static bool IsTerrainBlocking(FinalCanvasCellKind kind) =>
            kind == FinalCanvasCellKind.Solid || kind == FinalCanvasCellKind.Blocked ||
            kind == FinalCanvasCellKind.Hazard;
        private static bool IsHazardBlocking(FinalCanvasCellKind kind) =>
            kind == FinalCanvasCellKind.Hazard || kind == FinalCanvasCellKind.Blocked;
        private static int Permille(int count) =>
            (count * 1000) / SectorCanvasProtectionDensityReport.CellCount;

        private static void AddIntrusion(
            ICollection<SectorCanvasProtectionIntrusion> intrusions,
            ProtectionIntrusionKind kind,
            FinalCanvasCell cell,
            FinalCanvasLayerClaim claim,
            string reason) => intrusions.Add(new SectorCanvasProtectionIntrusion(
                kind, cell.Coordinate, claim.Layer, claim.SourceOwner, claim.ClaimId, reason));

        private static void Add(
            ICollection<ProtectionDensityFailure> failures,
            ProtectionDensityFailureCode code,
            string subject,
            string reason) => failures.Add(new ProtectionDensityFailure(code, subject, reason));

        private static ProtectionDensityResult Failed(
            SectorFinalCanvasLayerPlan plan,
            IEnumerable<ProtectionDensityFailure> failures,
            IEnumerable<SectorCanvasProtectionIntrusion> intrusions) =>
            new ProtectionDensityResult(plan, null, failures, intrusions);

        private sealed class CellAuthority
        {
            public CellAuthority(
                bool protectedOpen,
                bool fixedCell,
                bool boundary,
                bool specialEntrance,
                bool protectedFact,
                bool hasExplicitProtection)
            {
                ProtectedOpen = protectedOpen;
                Fixed = fixedCell;
                Boundary = boundary;
                SpecialEntrance = specialEntrance;
                Protected = protectedFact;
                HasExplicitProtection = hasExplicitProtection;
            }

            public bool ProtectedOpen { get; }
            public bool Fixed { get; }
            public bool Boundary { get; }
            public bool SpecialEntrance { get; }
            public bool Protected { get; }
            public bool HasExplicitProtection { get; }
        }
    }
}
