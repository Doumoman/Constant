using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Baking
{
    public static class SectorFinalRouteRecoveryValidator
    {
        public const string ReferencePublicationLabel =
            "REFERENCE FINAL ROUTE RECOVERY REPORT";

        public static FinalRouteRecoveryResult Validate(FinalRouteRecoveryRequest request)
        {
            var failures = new List<FinalRouteRecoveryFailure>();
            if (request == null)
            {
                failures.Add(Failure(FinalRouteFailureKind.MissingRequest,
                    "REQUEST", "Final route recovery request is required."));
                return Failed(null, failures);
            }

            var plan = request.CanvasPlan;
            var density = request.ProtectionDensityReport;
            ValidateSources(request, plan, density, failures);
            if (failures.Count > 0) return Failed(request, failures);

            var passability = BuildPassability(plan, failures);
            if (failures.Count > 0) return Failed(request, failures);

            ValidateAnchors(request, passability, failures);
            if (failures.Count > 0) return Failed(request, failures);

            var nodes = BuildNodes(request, plan, passability);
            var edges = BuildEdges(request, plan, passability, failures);
            if (failures.Count > 0) return Failed(request, failures);

            var adjacency = BuildAdjacency(nodes, edges);
            var witnesses = new List<FinalRouteWitness>();
            var recoveries = new List<FinalRecoveryWitness>();
            var softlocks = new List<FinalRouteSoftlockCandidate>();
            BuildWitnesses(request, passability, adjacency, witnesses, recoveries,
                softlocks, failures);
            ValidateOneWayRecovery(request, adjacency, softlocks, failures);
            ValidateWitnessCells(passability, witnesses, recoveries, failures);
            if (softlocks.Count > 0)
            {
                foreach (var candidate in softlocks.OrderBy(value => value))
                    failures.Add(Failure(FinalRouteFailureKind.StaticSoftlock,
                        candidate.StableId, candidate.Reason));
            }
            if (failures.Count > 0) return Failed(request, failures);

            var report = new SectorFinalRouteRecoveryReport(
                request, nodes, edges, witnesses, recoveries, softlocks, 0, 0, 0);
            if (!FinalRouteRecoveryDigest.IsLowerHexSha256(report.InputDigest) ||
                !FinalRouteRecoveryDigest.IsLowerHexSha256(report.OutputDigest) ||
                FinalRouteRecoveryDigest.ComputeOutput(report) != report.OutputDigest)
            {
                failures.Add(Failure(FinalRouteFailureKind.InvalidDigest,
                    "MAP16_03_DIGEST", "Final route recovery digest is not canonical lower-hex SHA-256."));
                return Failed(request, failures);
            }

            return new FinalRouteRecoveryResult(request, report,
                Array.Empty<FinalRouteRecoveryFailure>());
        }

        private static void ValidateSources(
            FinalRouteRecoveryRequest request,
            SectorFinalCanvasLayerPlan plan,
            SectorCanvasProtectionDensityReport density,
            ICollection<FinalRouteRecoveryFailure> failures)
        {
            if (plan == null)
                failures.Add(Failure(FinalRouteFailureKind.MissingCanvasPlan,
                    "MAP16_01", "A successful final canvas layer plan is required."));
            if (density == null)
                failures.Add(Failure(FinalRouteFailureKind.MissingProtectionDensityReport,
                    "MAP16_02", "A successful protection-density report is required."));
            if (plan == null || density == null) return;

            if (!ReferenceEquals(density.SourcePlan, plan) ||
                density.SourceInputDigest != plan.InputDigest ||
                density.SourceOutputDigest != plan.OutputDigest)
                failures.Add(Failure(FinalRouteFailureKind.SourceReportMismatch,
                    "MAP16_01_MAP16_02", "Protection-density report does not describe the supplied canvas plan."));

            if (plan.Request == null || plan.Request.Width != SectorFinalRouteRecoveryReport.SectorWidth ||
                plan.Request.Height != SectorFinalRouteRecoveryReport.SectorHeight)
                failures.Add(Failure(FinalRouteFailureKind.InvalidSectorDimensions,
                    "SECTOR_DIMENSIONS", "Final canvas must be exactly 48x32."));

            if (plan.ObservedCellCount != SectorFinalRouteRecoveryReport.CellCount ||
                plan.UniqueCoordinateCount != SectorFinalRouteRecoveryReport.CellCount ||
                plan.OutOfBoundsCellCount != 0 ||
                density.ObservedCellCount != SectorFinalRouteRecoveryReport.CellCount ||
                density.UniqueCoordinateCount != SectorFinalRouteRecoveryReport.CellCount)
                failures.Add(Failure(FinalRouteFailureKind.InvalidCellCount,
                    "SECTOR_CELLS", "Final canvas must contain 1536 unique in-bounds cells."));

            if (plan.MissingLayerKindCount != 0 ||
                plan.CoveredLayerKindCount != SectorFinalCanvasLayerPlan.RequiredLayerCount ||
                density.MissingLayerKindCount != 0)
                failures.Add(Failure(FinalRouteFailureKind.MissingLayer,
                    "FINAL_LAYERS", "All seven final canvas layers are required."));

            var digests = new[]
            {
                plan.InputDigest, plan.OutputDigest, density.InputDigest, density.OutputDigest,
            };
            if (digests.Any(value => !FinalRouteRecoveryDigest.IsLowerHexSha256(value)) ||
                FinalCanvasLayerDigest.ComputeOutput(plan) != plan.OutputDigest ||
                ProtectionDensityDigest.ComputeInput(plan) != density.InputDigest ||
                ProtectionDensityDigest.ComputeOutput(density) != density.OutputDigest ||
                !FinalRouteRecoveryDigest.IsLowerHexSha256(request.CanonicalDigest))
                failures.Add(Failure(FinalRouteFailureKind.InvalidDigest,
                    "UPSTREAM_DIGEST", "MAP16_01, MAP16_02 and request digests must be canonical lower-hex SHA-256."));

            if (density.ProtectionIntrusionCount != 0 ||
                density.DensityBudgetViolationCount != 0 ||
                density.UnownedAirViolationCount != 0 ||
                density.CleanupProjection == null ||
                density.CleanupProjection.ProtectedOpenChangedCount != 0 ||
                density.CleanupProjection.FixedChangedCount != 0 ||
                density.CleanupProjection.BoundaryChangedCount != 0 ||
                density.CleanupProjection.SpecialEntranceChangedCount != 0)
                failures.Add(Failure(FinalRouteFailureKind.ProtectionDensityRejected,
                    "MAP16_02_GATES", "Protection, cleanup, density and unowned-air gates must all pass."));

            var forbiddenCounts = new[]
            {
                plan.NewRngDrawCount, plan.SliceCreationCount, plan.GeneratedFileWriteCount,
                plan.TilemapMutationCount, plan.SceneMutationCount, plan.PrefabMutationCount,
                plan.GameObjectMutationCount, plan.GameplaySpawnCount,
                plan.ProductionSeedApprovalCount, plan.SectorRerollCount,
                plan.FallbackCarveCount, plan.FullRegressionCount,
                request.FallbackCarveCount, request.SilentWideningCount,
                request.SectorRerenderCount, request.WholeWorldRerandomCount,
                request.PlayerPhysicsSimulationCount, request.PlayModeRunCount,
                request.TilemapBakeCount, request.SliceCreationCount,
                request.GeneratedFileWriteCount, request.TilemapMutationCount,
                request.SceneMutationCount, request.PrefabMutationCount,
                request.GameObjectMutationCount, request.GameplaySpawnCount,
                request.FullRegressionCount, request.ProductionSeedApprovalCount,
            };
            if (forbiddenCounts.Any(value => value != 0))
                failures.Add(Failure(FinalRouteFailureKind.ForbiddenOperation,
                    "NO_MUTATION", "Validation cannot require random, carve, widening, rerender, physics, bake, slice, file, scene or gameplay operations."));
        }

        private static Dictionary<FinalCanvasCellCoordinate, Passability> BuildPassability(
            SectorFinalCanvasLayerPlan plan,
            ICollection<FinalRouteRecoveryFailure> failures)
        {
            var result = new Dictionary<FinalCanvasCellCoordinate, Passability>();
            foreach (var cell in plan.Cells.OrderBy(value => value))
            {
                if (cell == null || cell.Coordinate == null)
                {
                    failures.Add(Failure(FinalRouteFailureKind.InvalidCellCount,
                        "MISSING_CELL", "Every final canvas cell requires a coordinate."));
                    continue;
                }

                var winners = Enum.GetValues(typeof(FinalCanvasLayerKind))
                    .Cast<FinalCanvasLayerKind>()
                    .ToDictionary(kind => kind,
                        kind => cell.Winners.Where(value => value.Layer == kind).ToArray());
                if (winners.Any(pair => pair.Value.Length != 1))
                {
                    failures.Add(Failure(FinalRouteFailureKind.MissingLayer,
                        cell.Coordinate.ToString(), "Every cell requires exactly one winner in each final layer."));
                    continue;
                }

                var terrain = winners[FinalCanvasLayerKind.Terrain][0];
                var affordance = winners[FinalCanvasLayerKind.Affordance][0];
                var hazard = winners[FinalCanvasLayerKind.Hazard][0];
                var protection = winners[FinalCanvasLayerKind.Protection][0];
                var solid = terrain.CellKind == FinalCanvasCellKind.Solid;
                var hazardous = hazard.CellKind == FinalCanvasCellKind.Hazard;
                var protectionBlocked = protection.CellKind != FinalCanvasCellKind.None &&
                                        protection.CellKind != FinalCanvasCellKind.ProtectedOpen;
                var open = terrain.CellKind == FinalCanvasCellKind.Air ||
                           affordance.CellKind == FinalCanvasCellKind.Traversable ||
                           protection.CellKind == FinalCanvasCellKind.ProtectedOpen;
                result[cell.Coordinate] = new Passability(
                    open && !solid && !hazardous && !protectionBlocked,
                    solid, hazardous, protectionBlocked);
            }

            return result;
        }

        private static void ValidateAnchors(
            FinalRouteRecoveryRequest request,
            IReadOnlyDictionary<FinalCanvasCellCoordinate, Passability> passability,
            ICollection<FinalRouteRecoveryFailure> failures)
        {
            if (request.NullAnchorCount != 0)
                failures.Add(Failure(FinalRouteFailureKind.InvalidAnchor,
                    "NULL_ANCHOR", "Null route anchors are forbidden."));
            if (request.Anchors.Count(value => value.Kind == FinalRouteNodeKind.BaseEntry) != 1)
                failures.Add(Failure(FinalRouteFailureKind.MissingBaseEntry,
                    "BASE_ENTRY", "Exactly one base entry anchor is required."));
            if (request.Anchors.Count(value => value.Kind == FinalRouteNodeKind.BaseExit) != 1)
                failures.Add(Failure(FinalRouteFailureKind.MissingBaseExit,
                    "BASE_EXIT", "Exactly one base exit anchor is required."));
            if (request.Anchors.GroupBy(value => value.StableId, StringComparer.Ordinal)
                .Any(group => string.IsNullOrEmpty(group.Key) || group.Count() != 1))
                failures.Add(Failure(FinalRouteFailureKind.InvalidAnchor,
                    "ANCHOR_ID", "Anchor stable IDs must be non-empty and unique."));

            foreach (var anchor in request.Anchors)
            {
                if (anchor.Coordinate == null || !anchor.Coordinate.IsInBounds ||
                    anchor.SourceOwner == FinalCanvasSourceOwner.Unknown ||
                    ContainsForbiddenText(anchor.StableId))
                {
                    failures.Add(Failure(FinalRouteFailureKind.InvalidAnchor,
                        anchor.StableId, "Anchor identity, coordinate and source owner must be canonical."));
                    continue;
                }

                Passability state;
                if (!passability.TryGetValue(anchor.Coordinate, out state) || !state.IsPassable)
                {
                    failures.Add(Failure(FinalRouteFailureKind.BlockedAnchor,
                        anchor.StableId, "Anchor lies on Solid, Hazard, blocked Protection or missing-layer terrain."));
                    continue;
                }

                if (anchor.Protection != FinalCanvasProtectionKind.None)
                {
                    var cell = request.CanvasPlan.Cells.Single(value =>
                        value.Coordinate.Equals(anchor.Coordinate));
                    if (!cell.Winners.Any(value => value.SourceOwner == anchor.SourceOwner &&
                        value.Protection == anchor.Protection))
                        failures.Add(Failure(FinalRouteFailureKind.InvalidAnchor,
                            anchor.StableId, "Protected anchor is not backed by public final-canvas authority."));
                }
            }
        }

        private static List<FinalRouteNode> BuildNodes(
            FinalRouteRecoveryRequest request,
            SectorFinalCanvasLayerPlan plan,
            IReadOnlyDictionary<FinalCanvasCellCoordinate, Passability> passability)
        {
            var nodes = new List<FinalRouteNode>();
            foreach (var pair in passability.Where(value => value.Value.IsPassable)
                         .OrderBy(value => value.Key))
            {
                var anchor = request.Anchors.Where(value => value.Coordinate.Equals(pair.Key))
                    .OrderBy(value => value).FirstOrDefault();
                var cell = plan.Cells.Single(value => value.Coordinate.Equals(pair.Key));
                var ownerClaim = cell.Winners.Single(value =>
                    value.Layer == FinalCanvasLayerKind.SourceOwner);
                nodes.Add(new FinalRouteNode(
                    pair.Key,
                    anchor == null ? FinalRouteNodeKind.PassableCell : anchor.Kind,
                    anchor == null ? ownerClaim.SourceOwner : anchor.SourceOwner,
                    "CELL_" + pair.Key.RowMajorIndex.ToString("D4", CultureInfo.InvariantCulture)));
            }
            return nodes;
        }

        private static List<FinalRouteEdge> BuildEdges(
            FinalRouteRecoveryRequest request,
            SectorFinalCanvasLayerPlan plan,
            IReadOnlyDictionary<FinalCanvasCellCoordinate, Passability> passability,
            ICollection<FinalRouteRecoveryFailure> failures)
        {
            var edges = new List<FinalRouteEdge>();
            foreach (var pair in passability.Where(value => value.Value.IsPassable)
                         .OrderBy(value => value.Key))
            {
                foreach (var offset in new[] { new[] { 1, 0 }, new[] { 0, 1 } })
                {
                    var neighbor = new FinalCanvasCellCoordinate(
                        pair.Key.X + offset[0], pair.Key.Y + offset[1]);
                    Passability neighborState;
                    if (!passability.TryGetValue(neighbor, out neighborState) ||
                        !neighborState.IsPassable) continue;
                    edges.Add(new FinalRouteEdge(
                        pair.Key, neighbor, FinalRouteEdgeKind.OrthogonalPassable,
                        ResolveEdgeOwner(plan, pair.Key, neighbor),
                        "ORTHO_" + pair.Key.RowMajorIndex.ToString("D4", CultureInfo.InvariantCulture) +
                        "_" + neighbor.RowMajorIndex.ToString("D4", CultureInfo.InvariantCulture),
                        true));
                }
            }

            if (request.NullDeclaredEdgeCount != 0)
                failures.Add(Failure(FinalRouteFailureKind.InvalidDeclaredEdge,
                    "NULL_EDGE", "Null declared edges are forbidden."));
            if (request.DeclaredEdges.GroupBy(value => value.StableId, StringComparer.Ordinal)
                .Any(group => string.IsNullOrEmpty(group.Key) || group.Count() != 1))
                failures.Add(Failure(FinalRouteFailureKind.InvalidDeclaredEdge,
                    "EDGE_ID", "Declared edge stable IDs must be non-empty and unique."));

            foreach (var edge in request.DeclaredEdges.OrderBy(value => value))
            {
                Passability fromState;
                Passability toState;
                if (edge.From == null || edge.To == null ||
                    edge.Kind == FinalRouteEdgeKind.OrthogonalPassable ||
                    edge.SourceOwner == FinalCanvasSourceOwner.Unknown ||
                    ContainsForbiddenText(edge.StableId) ||
                    !passability.TryGetValue(edge.From, out fromState) || !fromState.IsPassable ||
                    !passability.TryGetValue(edge.To, out toState) || !toState.IsPassable)
                {
                    failures.Add(Failure(FinalRouteFailureKind.InvalidDeclaredEdge,
                        edge.StableId, "Declared edge endpoints must be canonical passable cells with explicit authority."));
                    continue;
                }
                edges.Add(edge);
            }

            return edges.OrderBy(value => value).ToList();
        }

        private static Dictionary<FinalCanvasCellCoordinate, List<GraphNeighbor>> BuildAdjacency(
            IEnumerable<FinalRouteNode> nodes,
            IEnumerable<FinalRouteEdge> edges)
        {
            var adjacency = nodes.ToDictionary(value => value.Coordinate,
                value => new List<GraphNeighbor>());
            foreach (var edge in edges.OrderBy(value => value))
            {
                adjacency[edge.From].Add(new GraphNeighbor(edge.To, edge));
                if (edge.IsBidirectional)
                    adjacency[edge.To].Add(new GraphNeighbor(edge.From, edge));
            }
            foreach (var neighbors in adjacency.Values)
                neighbors.Sort();
            return adjacency;
        }

        private static void BuildWitnesses(
            FinalRouteRecoveryRequest request,
            IReadOnlyDictionary<FinalCanvasCellCoordinate, Passability> passability,
            IReadOnlyDictionary<FinalCanvasCellCoordinate, List<GraphNeighbor>> adjacency,
            ICollection<FinalRouteWitness> witnesses,
            ICollection<FinalRecoveryWitness> recoveries,
            ICollection<FinalRouteSoftlockCandidate> softlocks,
            ICollection<FinalRouteRecoveryFailure> failures)
        {
            var entry = request.Anchors.Single(value => value.Kind == FinalRouteNodeKind.BaseEntry);
            var exit = request.Anchors.Single(value => value.Kind == FinalRouteNodeKind.BaseExit);
            var basePath = FindPath(entry.Coordinate,
                new HashSet<FinalCanvasCellCoordinate> { exit.Coordinate }, adjacency);
            if (basePath == null)
            {
                failures.Add(Failure(FinalRouteFailureKind.MissingBaseRoute,
                    entry.StableId + "_TO_" + exit.StableId,
                    "No deterministic static base entry-to-exit witness exists."));
                return;
            }

            witnesses.Add(new FinalRouteWitness(
                "BASE_ENTRY_TO_EXIT", FinalRouteWitnessKind.BaseEntryToExit,
                entry, exit, FinalRouteWitnessVerdict.Covered, basePath));
            var baseCoordinates = new HashSet<FinalCanvasCellCoordinate>(basePath);

            AddAnchorWitnesses(request, FinalRouteNodeKind.ExternalSocket,
                FinalRouteWitnessKind.ExternalSocketToBaseRoute,
                FinalRouteFailureKind.MissingExternalSocketWitness,
                baseCoordinates, adjacency, witnesses, softlocks, failures);
            AddAnchorWitnesses(request, FinalRouteNodeKind.BoundaryAperture,
                FinalRouteWitnessKind.BoundaryApertureToBaseRoute,
                FinalRouteFailureKind.MissingBoundaryApertureWitness,
                baseCoordinates, adjacency, witnesses, softlocks, failures);
            AddAnchorWitnesses(request, FinalRouteNodeKind.SpecialEntrance,
                FinalRouteWitnessKind.SpecialEntranceToBaseRoute,
                FinalRouteFailureKind.MissingSpecialEntranceWitness,
                baseCoordinates, adjacency, witnesses, softlocks, failures);
            AddAnchorWitnesses(request, FinalRouteNodeKind.HighRouteBranch,
                FinalRouteWitnessKind.HighRouteBranch,
                FinalRouteFailureKind.StaticSoftlock,
                baseCoordinates, adjacency, witnesses, softlocks, failures);

            foreach (var failureAnchor in request.Anchors.Where(value =>
                         value.Kind == FinalRouteNodeKind.FailureSample).OrderBy(value => value))
            {
                var path = FindPath(failureAnchor.Coordinate, baseCoordinates, adjacency);
                if (path == null)
                {
                    if (failureAnchor.Required)
                        failures.Add(Failure(FinalRouteFailureKind.MissingHighFailureRecovery,
                            failureAnchor.StableId,
                            "High-route failure sample has no static recovery witness to the base route."));
                    softlocks.Add(new FinalRouteSoftlockCandidate(
                        failureAnchor.Coordinate, FinalRouteFailureKind.MissingHighFailureRecovery,
                        failureAnchor.StableId, "Failure sample is isolated from the base route."));
                    continue;
                }

                var target = BaseTarget(path[path.Count - 1], failureAnchor.StableId);
                recoveries.Add(new FinalRecoveryWitness(
                    "RECOVERY_" + failureAnchor.StableId,
                    failureAnchor, target,
                    UsesDeclaredRecovery(path, request.DeclaredEdges)
                        ? FinalRouteRecoveryKind.DeclaredRecovery
                        : FinalRouteRecoveryKind.OrthogonalReturn,
                    FinalRouteWitnessVerdict.Covered, path));
                witnesses.Add(new FinalRouteWitness(
                    "HIGH_FAILURE_" + failureAnchor.StableId,
                    FinalRouteWitnessKind.HighFailureToBaseRecovery,
                    failureAnchor, target, FinalRouteWitnessVerdict.Covered, path));
            }
        }

        private static void AddAnchorWitnesses(
            FinalRouteRecoveryRequest request,
            FinalRouteNodeKind anchorKind,
            FinalRouteWitnessKind witnessKind,
            FinalRouteFailureKind failureKind,
            ISet<FinalCanvasCellCoordinate> baseCoordinates,
            IReadOnlyDictionary<FinalCanvasCellCoordinate, List<GraphNeighbor>> adjacency,
            ICollection<FinalRouteWitness> witnesses,
            ICollection<FinalRouteSoftlockCandidate> softlocks,
            ICollection<FinalRouteRecoveryFailure> failures)
        {
            foreach (var anchor in request.Anchors.Where(value => value.Kind == anchorKind)
                         .OrderBy(value => value))
            {
                var path = FindPath(anchor.Coordinate, baseCoordinates, adjacency);
                if (path == null)
                {
                    if (anchor.Required)
                        failures.Add(Failure(failureKind, anchor.StableId,
                            "Required anchor has no deterministic static witness to the base route."));
                    softlocks.Add(new FinalRouteSoftlockCandidate(
                        anchor.Coordinate, failureKind, anchor.StableId,
                        "Required route anchor is isolated from the base route."));
                    continue;
                }

                witnesses.Add(new FinalRouteWitness(
                    witnessKind.ToString().ToUpperInvariant() + "_" + anchor.StableId,
                    witnessKind, anchor, BaseTarget(path[path.Count - 1], anchor.StableId),
                    FinalRouteWitnessVerdict.Covered, path));
            }
        }

        private static void ValidateOneWayRecovery(
            FinalRouteRecoveryRequest request,
            IReadOnlyDictionary<FinalCanvasCellCoordinate, List<GraphNeighbor>> adjacency,
            ICollection<FinalRouteSoftlockCandidate> softlocks,
            ICollection<FinalRouteRecoveryFailure> failures)
        {
            foreach (var edge in request.DeclaredEdges.Where(value => !value.IsBidirectional)
                         .OrderBy(value => value))
            {
                var returnPath = FindPath(edge.To,
                    new HashSet<FinalCanvasCellCoordinate> { edge.From }, adjacency);
                if (returnPath != null) continue;
                softlocks.Add(new FinalRouteSoftlockCandidate(
                    edge.To, FinalRouteFailureKind.StaticSoftlock,
                    edge.StableId, "One-way declared link has no static return or recovery witness."));
                failures.Add(Failure(FinalRouteFailureKind.StaticSoftlock,
                    edge.StableId, "One-way declared link has no recovery witness."));
            }
        }

        private static void ValidateWitnessCells(
            IReadOnlyDictionary<FinalCanvasCellCoordinate, Passability> passability,
            IEnumerable<FinalRouteWitness> witnesses,
            IEnumerable<FinalRecoveryWitness> recoveries,
            ICollection<FinalRouteRecoveryFailure> failures)
        {
            var coordinates = witnesses.SelectMany(value => value.Path)
                .Concat(recoveries.SelectMany(value => value.Path)).Distinct().ToArray();
            foreach (var coordinate in coordinates)
            {
                Passability state;
                if (!passability.TryGetValue(coordinate, out state) || state.IsPassable) continue;
                failures.Add(Failure(FinalRouteFailureKind.RouteCrossesBlockedCell,
                    coordinate == null ? "MISSING" : coordinate.ToString(),
                    "Route witness crosses Solid, Hazard or blocked Protection."));
            }
        }

        private static List<FinalCanvasCellCoordinate> FindPath(
            FinalCanvasCellCoordinate start,
            ISet<FinalCanvasCellCoordinate> targets,
            IReadOnlyDictionary<FinalCanvasCellCoordinate, List<GraphNeighbor>> adjacency)
        {
            if (start == null || targets == null || !adjacency.ContainsKey(start)) return null;
            var queue = new Queue<FinalCanvasCellCoordinate>();
            var visited = new HashSet<FinalCanvasCellCoordinate>();
            var previous = new Dictionary<FinalCanvasCellCoordinate, FinalCanvasCellCoordinate>();
            queue.Enqueue(start);
            visited.Add(start);
            FinalCanvasCellCoordinate found = null;
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (targets.Contains(current))
                {
                    found = current;
                    break;
                }
                foreach (var neighbor in adjacency[current])
                {
                    if (!visited.Add(neighbor.Coordinate)) continue;
                    previous[neighbor.Coordinate] = current;
                    queue.Enqueue(neighbor.Coordinate);
                }
            }
            if (found == null) return null;
            var path = new List<FinalCanvasCellCoordinate> { found };
            while (!path[path.Count - 1].Equals(start))
                path.Add(previous[path[path.Count - 1]]);
            path.Reverse();
            return path;
        }

        private static FinalRouteAnchor BaseTarget(
            FinalCanvasCellCoordinate coordinate,
            string sourceId) => new FinalRouteAnchor(
                "BASE_ROUTE_TARGET_" + sourceId,
                FinalRouteNodeKind.PassableCell,
                coordinate,
                FinalCanvasSourceOwner.MandatoryRoute,
                FinalCanvasProtectionKind.None,
                false);

        private static bool UsesDeclaredRecovery(
            IReadOnlyList<FinalCanvasCellCoordinate> path,
            IEnumerable<FinalRouteEdge> declaredEdges)
        {
            var edges = declaredEdges.Where(value =>
                value.Kind == FinalRouteEdgeKind.DeclaredRecoveryLink).ToArray();
            for (var index = 1; index < path.Count; index++)
            {
                var from = path[index - 1];
                var to = path[index];
                if (edges.Any(edge => edge.From.Equals(from) && edge.To.Equals(to) ||
                    edge.IsBidirectional && edge.From.Equals(to) && edge.To.Equals(from)))
                    return true;
            }
            return false;
        }

        private static FinalCanvasSourceOwner ResolveEdgeOwner(
            SectorFinalCanvasLayerPlan plan,
            FinalCanvasCellCoordinate from,
            FinalCanvasCellCoordinate to)
        {
            var owners = plan.Cells.Where(value =>
                    value.Coordinate.Equals(from) || value.Coordinate.Equals(to))
                .Select(value => value.Winners.Single(claim =>
                    claim.Layer == FinalCanvasLayerKind.SourceOwner).SourceOwner)
                .Where(value => value != FinalCanvasSourceOwner.Unknown)
                .OrderByDescending(value => value).ToArray();
            return owners.Length == 0 ? FinalCanvasSourceOwner.QuietFiller : owners[0];
        }

        private static bool ContainsForbiddenText(string value) => string.IsNullOrEmpty(value) ||
            value.Contains("/") || value.Contains("\\") || value.Contains("\r") || value.Contains("\n");

        private static FinalRouteRecoveryFailure Failure(
            FinalRouteFailureKind code,
            string subject,
            string reason) => new FinalRouteRecoveryFailure(code, subject, reason);

        private static FinalRouteRecoveryResult Failed(
            FinalRouteRecoveryRequest request,
            IEnumerable<FinalRouteRecoveryFailure> failures) =>
            new FinalRouteRecoveryResult(request, null, failures);

        private sealed class Passability
        {
            public Passability(bool isPassable, bool solid, bool hazard, bool protectionBlocked)
            {
                IsPassable = isPassable;
                Solid = solid;
                Hazard = hazard;
                ProtectionBlocked = protectionBlocked;
            }

            public bool IsPassable { get; }
            public bool Solid { get; }
            public bool Hazard { get; }
            public bool ProtectionBlocked { get; }
        }

        private sealed class GraphNeighbor : IComparable<GraphNeighbor>
        {
            public GraphNeighbor(FinalCanvasCellCoordinate coordinate, FinalRouteEdge edge)
            {
                Coordinate = coordinate;
                Edge = edge;
            }

            public FinalCanvasCellCoordinate Coordinate { get; }
            public FinalRouteEdge Edge { get; }

            public int CompareTo(GraphNeighbor other)
            {
                if (other == null) return -1;
                var comparison = Coordinate.CompareTo(other.Coordinate);
                return comparison != 0 ? comparison : Edge.CompareTo(other.Edge);
            }
        }
    }
}
