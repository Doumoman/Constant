using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.WorldGeneration.Validation
{
    public static class GeneratedClusterRecoveryDensityValidator
    {
        public const string ExpectedGraphDigest =
            "bf6b682dd5797e6dd8cb469289ca26fc8db3f10175a99f77d6316c5afe65d063";
        public const string ExpectedCompletionProofDigest =
            "6bc3bf96b766337011fd08d3436fd46e12b30ae7b0af5cfc0598646e8e338fca";
        public const string ExpectedIncomingHandoffDigest =
            "694a70da6977d6cb7948976b8f2a512fa6ecf6c2d15f5f9f83d56dada59c9b2e";
        private const int AirWindowWidth = 8;
        private const int AirWindowHeight = 6;
        private const string PitMarker = "ONE_WAY_PIT_CANDIDATE";
        private const string AuthoredLowCeilingMarker = "AUTHORED_LOW_CEILING";

        public static ClusterRecoveryValidationResult Validate(
            ClusterRecoveryValidationInput recoveryInput,
            ClusterDensityValidationInput densityInput)
        {
            var failures = new List<ClusterRecoveryFailure>();
            if (!ValidateCommon(recoveryInput, densityInput, failures))
                return Failure(failures);

            var graph = recoveryInput.Graph;
            var nodeIds = new HashSet<string>(graph.Nodes.Select(value => value.NodeId),
                StringComparer.Ordinal);
            ValidateRecoveryBindings(recoveryInput, densityInput, nodeIds, failures);
            ValidateDensityBindings(densityInput, recoveryInput, nodeIds, failures);
            if (failures.Count != 0)
                return Failure(failures);

            var recoveryProofs = ValidateRecoveryCases(recoveryInput, failures);
            var densityProofs = ValidateDensityClusters(densityInput, recoveryInput, failures);
            if (failures.Count != 0)
                return Failure(failures);

            var recovery = new ClusterRecoveryValidationSurface(recoveryInput, recoveryProofs);
            var density = new ClusterDensityValidationSurface(densityInput, densityProofs);
            var surface = new ClusterRecoveryDensityValidationSurface(recovery, density,
                graph.GraphDigest, recoveryInput.CompletionSearch.Proof.ProofDigest,
                recoveryInput.CompletionSearch.Map19_04HandoffDigest);
            return new ClusterRecoveryValidationResult(surface, failures);
        }

        private static bool ValidateCommon(
            ClusterRecoveryValidationInput recovery,
            ClusterDensityValidationInput density,
            ICollection<ClusterRecoveryFailure> failures)
        {
            if (recovery == null)
            {
                Add(failures, "ClusterRecovery", "MISSING_RECOVERY_INPUT", "MISSING",
                    "input", "NON_NULL", "NULL", "MISSING");
                return false;
            }
            if (density == null)
            {
                Add(failures, "ClusterDensity", "MISSING_DENSITY_INPUT", "MISSING",
                    "input", "NON_NULL", "NULL", "MISSING");
                return false;
            }
            if (recovery.Graph == null || density.Graph == null)
            {
                Add(failures, "MAP19_02", "MISSING_GRAPH_INPUT", "MISSING", "graph",
                    "NON_NULL_SHARED_GRAPH", "NULL", "MISSING");
                return false;
            }
            var graph = recovery.Graph;
            Digest(failures, "MAP19_02", "GRAPH_DIGEST_MISMATCH", "MISSING",
                "graph.digest", ExpectedGraphDigest, graph.GraphDigest, graph.GraphDigest);
            Digest(failures, "MAP19_02", "GRAPH_CANONICAL_DIGEST_MISMATCH", "MISSING",
                "graph.canonical", graph.ComputeGraphDigest(), graph.GraphDigest,
                graph.GraphDigest);
            if (!ReferenceEquals(graph, density.Graph))
                Add(failures, "MAP19_02", "GRAPH_BINDING_MISMATCH", "MISSING", "graph",
                    "SAME_READ_ONLY_INSTANCE", "DIFFERENT_INSTANCE", graph.GraphDigest);
            ValidateProofs(recovery.NakedSearch, recovery.CompletionSearch, graph,
                "recovery", failures);
            ValidateProofs(density.NakedSearch, density.CompletionSearch, graph,
                "density", failures);
            if (!ReferenceEquals(recovery.NakedSearch, density.NakedSearch) ||
                !ReferenceEquals(recovery.CompletionSearch, density.CompletionSearch))
                Add(failures, "MAP19_03", "PROOF_BINDING_MISMATCH", "MISSING", "proofs",
                    "SAME_READ_ONLY_INSTANCES", "DIFFERENT_INSTANCES", graph.GraphDigest);
            Digest(failures, "MAP19_03", "COMPLETION_PROOF_DIGEST_MISMATCH", "MISSING",
                "declared.completion", ExpectedCompletionProofDigest,
                recovery.DeclaredCompletionProofDigest, graph.GraphDigest);
            Digest(failures, "MAP19_03", "MAP19_03_HANDOFF_DIGEST_MISMATCH", "MISSING",
                "declared.handoff", ExpectedIncomingHandoffDigest,
                recovery.DeclaredIncomingHandoffDigest, graph.GraphDigest);
            if (recovery.ActionAudit == null || !recovery.ActionAudit.IsZero ||
                density.ActionAudit == null || !density.ActionAudit.IsZero)
                Add(failures, "ForbiddenAction", "FORBIDDEN_API_OR_MUTATION_ATTEMPT",
                    "MISSING", "action_audit", "ALL_ZERO_AND_MAP19_05_NOT_STARTED",
                    "NON_ZERO_OR_MISSING", graph.GraphDigest);
            return failures.Count == 0;
        }

        private static void ValidateProofs(
            NakedTraversalSearchResult naked,
            GeneratedCompletionSearchResult completion,
            GeneratedTileMovementGraph graph,
            string key,
            ICollection<ClusterRecoveryFailure> failures)
        {
            if (naked == null || !naked.Success || naked.Surface == null)
                Add(failures, "MAP19_03", "MISSING_MAP19_03_PROOF_INPUT", "MISSING", key,
                    "SUCCESSFUL_NAKED_PROOF", "MISSING_OR_FAILED", graph.GraphDigest);
            else if (!ReferenceEquals(naked.Surface.Graph, graph) ||
                     !string.Equals(naked.Surface.Graph.GraphDigest, graph.GraphDigest,
                         StringComparison.Ordinal))
                Add(failures, "MAP19_03", "NAKED_PROOF_GRAPH_MISMATCH", "MISSING", key,
                    graph.GraphDigest, naked.Surface.Graph.GraphDigest, graph.GraphDigest);
            if (completion == null || !completion.Success || completion.Proof == null)
            {
                Add(failures, "MAP19_03", "MISSING_MAP19_03_PROOF_INPUT", "MISSING", key,
                    "SUCCESSFUL_COMPLETION_PROOF", "MISSING_OR_FAILED", graph.GraphDigest);
                return;
            }
            Digest(failures, "MAP19_03", "COMPLETION_PROOF_DIGEST_MISMATCH", "MISSING",
                key + ".completion", ExpectedCompletionProofDigest,
                completion.Proof.ProofDigest, graph.GraphDigest);
            Digest(failures, "MAP19_03", "MAP19_03_HANDOFF_DIGEST_MISMATCH", "MISSING",
                key + ".handoff", ExpectedIncomingHandoffDigest,
                completion.Map19_04HandoffDigest, completion.Proof.ProofDigest);
        }

        private static void ValidateRecoveryBindings(
            ClusterRecoveryValidationInput input,
            ClusterDensityValidationInput density,
            ISet<string> nodeIds,
            ICollection<ClusterRecoveryFailure> failures)
        {
            if (input.Cases.Count == 0)
                Add(failures, "ClusterRecovery", "MISSING_CLUSTER_BINDING", "MISSING",
                    "cases", "AT_LEAST_ONE", "0", input.Graph.GraphDigest);
            foreach (var duplicate in input.Cases.GroupBy(value => value.ClusterId.Value + "|" +
                         value.CaseId, StringComparer.Ordinal).Where(value => value.Count() > 1))
                Add(failures, "ClusterRecovery", "DUPLICATE_RECOVERY_CASE", "MISSING",
                    duplicate.Key, "1", Number(duplicate.Count()), input.Graph.GraphDigest);
            var densityClusters = new HashSet<TerrainClusterId>(density.Clusters.Select(value =>
                value.ClusterId));
            foreach (var item in input.Cases)
            {
                var cluster = item.ClusterId.Value;
                if (item.RouteWitness == null || string.IsNullOrEmpty(cluster) ||
                    !densityClusters.Contains(item.ClusterId))
                    Add(failures, item.SourceOwner, "MISSING_CLUSTER_BINDING", cluster,
                        item.CaseId, "ROUTE_WITNESS_AND_DENSITY_CLUSTER", "MISSING",
                        item.SourceDigest);
                else if (!string.Equals(item.SourceDigest, item.RouteWitness.CanonicalDigest,
                             StringComparison.Ordinal) ||
                         !BakingCanonicalDigest.IsLowerHexSha256(item.SourceDigest))
                    Add(failures, item.SourceOwner, "CLUSTER_SOURCE_DIGEST_MISMATCH", cluster,
                        item.CaseId, item.RouteWitness.CanonicalDigest, item.SourceDigest,
                        item.RouteWitness.CanonicalDigest);
                ValidateWitnessKind(item, failures);
                if (!nodeIds.Contains(item.StartNodeId))
                    Add(failures, item.SourceOwner, "MISSING_RECOVERY_START", cluster,
                        item.CaseId, "EXISTING_GRAPH_NODE", item.StartNodeId, item.SourceDigest);
                if (item.AcceptedTargetNodeIds.Count == 0)
                    Add(failures, item.SourceOwner, "MISSING_RECOVERY_TARGET", cluster,
                        item.CaseId, "AT_LEAST_ONE_TARGET", "0", item.SourceDigest);
                foreach (var target in item.AcceptedTargetNodeIds.Where(value =>
                             !nodeIds.Contains(value)))
                    Add(failures, item.SourceOwner, "MISSING_RECOVERY_TARGET", cluster,
                        item.CaseId, "EXISTING_GRAPH_NODE", target, item.SourceDigest);
                if (item.AllowedMovementKinds.Count == 0 || item.AllowedMovementKinds.Any(value =>
                        !Enum.IsDefined(typeof(TraversalMovementKind), value)))
                    Add(failures, item.SourceOwner, "UNSUPPORTED_RECOVERY_MOVEMENT", cluster,
                        item.CaseId, "DEFINED_NON_EMPTY_MOVEMENT_SET",
                        string.Join(",", item.AllowedMovementKinds), item.SourceDigest);
                if (item.MaxRecoveryEdgeCount <= 0)
                    Add(failures, item.SourceOwner, "INVALID_RECOVERY_EDGE_BOUND", cluster,
                        item.CaseId, "POSITIVE", Number(item.MaxRecoveryEdgeCount),
                        item.SourceDigest);
                if (!item.IsTimed) continue;
                if (item.MinimumAcceptedCostMilliseconds <= 0 ||
                    item.MaximumAcceptedCostMilliseconds < item.MinimumAcceptedCostMilliseconds ||
                    item.RecoveryCostMilliseconds <= 0 ||
                    string.Equals(item.ExpectedWindowClass, "MISSING", StringComparison.Ordinal))
                    Add(failures, item.SourceOwner, "INVALID_RECOVERY_WINDOW_RULE", cluster,
                        item.CaseId, "VALID_DATA_DRIVEN_WINDOW_AND_COST", item.StableToken,
                        item.SourceDigest);
                if (item.RecoveryCostMilliseconds < item.MinimumAcceptedCostMilliseconds ||
                    item.RecoveryCostMilliseconds > item.MaximumAcceptedCostMilliseconds)
                    Add(failures, item.SourceOwner, "RECOVERY_COST_EXCEEDS_2_TO_5S_BOUND",
                        cluster, item.CaseId,
                        Number(item.MinimumAcceptedCostMilliseconds) + ".." +
                        Number(item.MaximumAcceptedCostMilliseconds),
                        Number(item.RecoveryCostMilliseconds), item.SourceDigest);
                if (item.RouteWitness != null && !item.RouteWitness.RecoveryRoutes.Any(value =>
                        value.TotalEstimatedDurationMilliseconds ==
                        item.RecoveryCostMilliseconds))
                    Add(failures, item.SourceOwner, "RECOVERY_COST_SOURCE_MISMATCH", cluster,
                        item.CaseId, "EXISTING_ROUTE_WITNESS_DURATION",
                        Number(item.RecoveryCostMilliseconds), item.SourceDigest);
            }
        }

        private static void ValidateWitnessKind(
            ClusterRecoveryCase item,
            ICollection<ClusterRecoveryFailure> failures)
        {
            if (item.RouteWitness == null) return;
            var present = true;
            switch (item.Kind)
            {
                case ClusterRecoveryCaseKind.BaseRouteRejoin:
                case ClusterRecoveryCaseKind.ClusterEntryToPrimarySpine:
                case ClusterRecoveryCaseKind.PrimarySpineToClusterExit:
                    present = item.RouteWitness.BaselineRoute != null;
                    break;
                case ClusterRecoveryCaseKind.HighRouteRecovery:
                    present = item.RouteWitness.HighRoutes.Count != 0;
                    break;
                case ClusterRecoveryCaseKind.ExplicitRecoveryRoute:
                case ClusterRecoveryCaseKind.TimedRecoveryWindow:
                    present = item.RouteWitness.RecoveryRoutes.Count != 0;
                    break;
                case ClusterRecoveryCaseKind.BoundarySocketRecovery:
                    present = item.RouteWitness.BaselineRoute != null &&
                        !string.IsNullOrEmpty(item.RouteWitness.BaselineRoute.EntryPortId) &&
                        !string.IsNullOrEmpty(item.RouteWitness.BaselineRoute.ExitPortId);
                    break;
                default:
                    present = false;
                    break;
            }
            if (!present)
                Add(failures, item.SourceOwner, "MISSING_RECOVERY_WITNESS_KIND",
                    item.ClusterId.Value, item.CaseId, item.Kind.ToString(), "MISSING",
                    item.SourceDigest);
        }

        private static void ValidateDensityBindings(
            ClusterDensityValidationInput input,
            ClusterRecoveryValidationInput recovery,
            ISet<string> nodeIds,
            ICollection<ClusterRecoveryFailure> failures)
        {
            if (input.Clusters.Count == 0)
                Add(failures, "ClusterDensity", "MISSING_CLUSTER_BINDING", "MISSING",
                    "clusters", "AT_LEAST_ONE", "0", input.Graph.GraphDigest);
            foreach (var duplicate in input.Clusters.GroupBy(value => value.ClusterId)
                         .Where(value => value.Count() > 1))
                Add(failures, "ClusterDensity", "DUPLICATE_DENSITY_CLUSTER",
                    duplicate.Key.Value, "cluster", "1", Number(duplicate.Count()),
                    input.Graph.GraphDigest);
            var recoveryClusters = new HashSet<TerrainClusterId>(recovery.Cases.Select(value =>
                value.ClusterId));
            foreach (var source in input.Clusters)
            {
                var cluster = source.ClusterId.Value;
                if (source.RouteWitness == null || !recoveryClusters.Contains(source.ClusterId))
                    Add(failures, source.SourceOwner, "MISSING_CLUSTER_BINDING", cluster,
                        "density", "ROUTE_WITNESS_AND_RECOVERY_CASE", "MISSING",
                        source.SourceDigest);
                else if (!string.Equals(source.SourceDigest,
                             source.RouteWitness.CanonicalDigest, StringComparison.Ordinal) ||
                         !BakingCanonicalDigest.IsLowerHexSha256(source.SourceDigest))
                    Add(failures, source.SourceOwner, "CLUSTER_SOURCE_DIGEST_MISMATCH", cluster,
                        "density", source.RouteWitness.CanonicalDigest, source.SourceDigest,
                        source.RouteWitness.CanonicalDigest);
                if (source.Canvas == null || source.Slices == null)
                {
                    Add(failures, source.SourceOwner, "MISSING_DENSITY_CELL_SOURCE", cluster,
                        "density", "CANVAS_AND_GENERATED_SLICES", "MISSING",
                        source.SourceDigest);
                    continue;
                }
                if (source.Canvas.ValidationStamp == null ||
                    source.Canvas.ValidationStamp.State != SectorCanvasValidationState.Validated)
                    Add(failures, source.SourceOwner, "UNVALIDATED_DENSITY_CANVAS", cluster,
                        "density", "VALIDATED", "UNVALIDATED", source.SourceDigest);
                if (source.Canvas.Id != source.Slices.SourceCanvasId ||
                    source.Slices.BoundaryRole != GeneratedSliceBoundaryRole.GeneratedOutput)
                    Add(failures, source.SourceOwner, "DENSITY_SLICE_BINDING_MISMATCH", cluster,
                        "density", source.Canvas.Id.Value + "/GENERATED_OUTPUT",
                        source.Slices.SourceCanvasId.Value + "/" + source.Slices.BoundaryRole,
                        source.SourceDigest);
                if (source.Slices.Slices.Count != 16 ||
                    source.Slices.Slices.Sum(value => value.Cells.Count) != 1536)
                    Add(failures, source.SourceOwner, "DENSITY_SLICE_COUNT_MISMATCH", cluster,
                        "density", "16/1536", source.Slices.Slices.Count + "/" +
                        source.Slices.Slices.Sum(value => value.Cells.Count),
                        source.SourceDigest);
                if (source.MinimumX < 0 || source.MinimumY < 0 ||
                    source.MaximumX >= source.Canvas.Width ||
                    source.MaximumY >= source.Canvas.Height || source.Width <= 0 ||
                    source.Height <= 0)
                    Add(failures, source.SourceOwner, "INVALID_CLUSTER_DENSITY_BOUNDS", cluster,
                        "density", "WITHIN_CANVAS", source.MinimumX + "," + source.MinimumY +
                        ".." + source.MaximumX + "," + source.MaximumY, source.SourceDigest);
                if (source.RequiredEightBySixAirWindows <= 0)
                    Add(failures, source.SourceOwner, "INVALID_AIR_WINDOW_REQUIREMENT", cluster,
                        "density", "POSITIVE", Number(source.RequiredEightBySixAirWindows),
                        source.SourceDigest);
                if (source.AcceptedRecoveryTargetNodeIds.Count == 0)
                    Add(failures, source.SourceOwner, "MISSING_RECOVERY_TARGET", cluster,
                        "density", "AT_LEAST_ONE_TARGET", "0", source.SourceDigest);
                foreach (var target in source.AcceptedRecoveryTargetNodeIds.Where(value =>
                             !nodeIds.Contains(value)))
                    Add(failures, source.SourceOwner, "MISSING_RECOVERY_TARGET", cluster,
                        "density", "EXISTING_GRAPH_NODE", target, source.SourceDigest);
            }
        }

        private static IReadOnlyList<ClusterRecoveryProof> ValidateRecoveryCases(
            ClusterRecoveryValidationInput input,
            ICollection<ClusterRecoveryFailure> failures)
        {
            var proofs = new List<ClusterRecoveryProof>();
            foreach (var item in input.Cases)
            {
                var adjacency = BuildAdjacency(input.Graph,
                    new HashSet<TraversalMovementKind>(item.AllowedMovementKinds));
                var search = Search(adjacency, item.StartNodeId);
                var reachableTargets = item.AcceptedTargetNodeIds.Where(search.Distance.ContainsKey)
                    .OrderBy(value => search.Distance[value])
                    .ThenBy(value => value, StringComparer.Ordinal).ToArray();
                if (reachableTargets.Length == 0)
                {
                    failures.Add(new ClusterRecoveryFailure(item.SourceOwner,
                        "UNREACHABLE_RECOVERY_TARGET", item.ClusterId.Value, item.CaseId,
                        string.Join(",", item.AcceptedTargetNodeIds), "REACHABLE_GRAPH_TARGET",
                        "UNREACHABLE", item.SourceDigest, Frontier(search, adjacency)));
                    continue;
                }
                var target = reachableTargets[0];
                var distance = search.Distance[target];
                if (distance > item.MaxRecoveryEdgeCount)
                {
                    failures.Add(new ClusterRecoveryFailure(item.SourceOwner,
                        "RECOVERY_EDGE_COST_EXCEEDS_BOUND", item.ClusterId.Value, item.CaseId,
                        target, "<= " + Number(item.MaxRecoveryEdgeCount), Number(distance),
                        item.SourceDigest, Frontier(search, adjacency)));
                    continue;
                }
                proofs.Add(new ClusterRecoveryProof(item, target, search.Distance.Count,
                    search.VisitedEdges, distance, input.Graph.GraphDigest,
                    input.CompletionSearch.Proof.ProofDigest));
            }
            return proofs;
        }

        private static IReadOnlyList<ClusterDensityProof> ValidateDensityClusters(
            ClusterDensityValidationInput input,
            ClusterRecoveryValidationInput recovery,
            ICollection<ClusterRecoveryFailure> failures)
        {
            var proofs = new List<ClusterDensityProof>();
            var adjacency = BuildAdjacency(input.Graph,
                new HashSet<TraversalMovementKind>(Enum.GetValues(typeof(TraversalMovementKind))
                    .Cast<TraversalMovementKind>()));
            var reachable = Search(adjacency, input.NakedSearch.Surface.StartNodeId).Distance;
            var standNodes = input.Graph.Nodes.Where(value =>
                value.Kind == GeneratedTileMovementNodeKind.Stand).ToArray();
            var standByCell = standNodes.GroupBy(value => value.Cell)
                .ToDictionary(value => value.Key, value => value.ToArray());
            var reachableGraphCells = new HashSet<LocalTileCoord>(input.Graph.Nodes.Where(value =>
                reachable.ContainsKey(value.NodeId)).Select(value => value.Cell));
            foreach (var source in input.Clusters)
            {
                var cells = source.Canvas.Cells.Where(value =>
                    value.Coordinate.X >= source.MinimumX && value.Coordinate.X <= source.MaximumX &&
                    value.Coordinate.Y >= source.MinimumY && value.Coordinate.Y <= source.MaximumY)
                    .OrderBy(value => value.Coordinate.X)
                    .ThenBy(value => value.Coordinate.Y).ToArray();
                var expectedCells = source.Width * source.Height;
                if (cells.Length != expectedCells || cells.Select(value => value.Coordinate)
                        .Distinct().Count() != expectedCells)
                {
                    Add(failures, source.SourceOwner, "DENSITY_CELL_COVERAGE_MISMATCH",
                        source.ClusterId.Value, "density", Number(expectedCells),
                        Number(cells.Length), source.SourceDigest);
                    continue;
                }
                var byCell = cells.ToDictionary(value => value.Coordinate);
                bool IsSolid(LocalTileCoord coordinate) => byCell.TryGetValue(coordinate,
                    out var cell) && Solid(cell);
                bool IsAir(LocalTileCoord coordinate) => byCell.TryGetValue(coordinate,
                    out var cell) && !Solid(cell);
                var solid = cells.Count(Solid);
                var airCells = cells.Where(value => !Solid(value)).ToArray();
                var reachableAirCells = FloodReachableAir(byCell, reachableGraphCells);
                var reachableAir = airCells.Count(value =>
                    reachableAirCells.Contains(value.Coordinate));
                var pockets = airCells.Where(value =>
                    !reachableAirCells.Contains(value.Coordinate)).ToArray();
                var windows = CountAirWindows(source, IsAir);
                var snags = airCells.Where(value => HeadSnag(value, IsAir, IsSolid)).ToArray();
                var pits = airCells.Where(value => string.Equals(value.Layers.Marker.StableId,
                        PitMarker, StringComparison.Ordinal) &&
                    IsOneWayPit(value.Coordinate, standByCell, input.Graph, reachableAirCells,
                        adjacency, source.AcceptedRecoveryTargetNodeIds)).ToArray();
                var hard = cells.Where(value => Solid(value) &&
                    value.Layers.Solid.StableId.IndexOf("HARD", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    standByCell.ContainsKey(value.Coordinate)).ToArray();
                var overprotected = cells.Where(OverprotectedEnvelope).ToArray();
                var socketCells = new HashSet<LocalTileCoord>(input.Graph.Nodes.Where(value =>
                    value.Kind == GeneratedTileMovementNodeKind.IntersectorSocket)
                    .Select(value => value.Cell));
                var boundary = cells.Where(value => Solid(value) &&
                    socketCells.Contains(value.Coordinate)).ToArray();
                AddDensityFailures(source, windows, pockets, snags, pits, hard, overprotected,
                    boundary, failures);
                proofs.Add(new ClusterDensityProof(source, cells.Length, solid, reachableAir,
                    airCells.Length, windows, pockets.Length, snags.Length, pits.Length,
                    hard.Length, overprotected.Length, boundary.Length, input.ComputeDigest()));
            }
            return proofs;
        }

        private static HashSet<LocalTileCoord> FloodReachableAir(
            IReadOnlyDictionary<LocalTileCoord, SectorCanvasCell> cells,
            IEnumerable<LocalTileCoord> graphCells)
        {
            var reachable = new HashSet<LocalTileCoord>();
            var frontier = new Queue<LocalTileCoord>();
            foreach (var coordinate in graphCells.OrderBy(value => value.X)
                         .ThenBy(value => value.Y))
            {
                if (!cells.TryGetValue(coordinate, out var cell) || Solid(cell) ||
                    !reachable.Add(coordinate))
                    continue;
                frontier.Enqueue(coordinate);
            }

            var offsets = new[]
            {
                new LocalTileCoord(-1, 0),
                new LocalTileCoord(0, -1),
                new LocalTileCoord(0, 1),
                new LocalTileCoord(1, 0),
            };
            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                foreach (var offset in offsets)
                {
                    var next = new LocalTileCoord(current.X + offset.X,
                        current.Y + offset.Y);
                    if (!cells.TryGetValue(next, out var cell) || Solid(cell) ||
                        !reachable.Add(next))
                        continue;
                    frontier.Enqueue(next);
                }
            }
            return reachable;
        }

        private static int CountAirWindows(
            ClusterDensitySource source,
            Func<LocalTileCoord, bool> isAir)
        {
            var count = 0;
            for (var y = source.MinimumY; y <= source.MaximumY - AirWindowHeight + 1; y++)
            for (var x = source.MinimumX; x <= source.MaximumX - AirWindowWidth + 1; x++)
            {
                var clear = true;
                for (var dy = 0; dy < AirWindowHeight && clear; dy++)
                for (var dx = 0; dx < AirWindowWidth; dx++)
                    if (!isAir(new LocalTileCoord(x + dx, y + dy)))
                    {
                        clear = false;
                        break;
                    }
                if (clear) count++;
            }
            return count;
        }

        private static bool HeadSnag(
            SectorCanvasCell cell,
            Func<LocalTileCoord, bool> isAir,
            Func<LocalTileCoord, bool> isSolid)
        {
            if (string.Equals(cell.Layers.Marker.StableId, AuthoredLowCeilingMarker,
                    StringComparison.Ordinal))
                return false;
            var at = cell.Coordinate;
            if (!isSolid(new LocalTileCoord(at.X, at.Y - 1)) ||
                !isSolid(new LocalTileCoord(at.X, at.Y + 1)))
                return false;
            return isAir(new LocalTileCoord(at.X - 1, at.Y)) &&
                       isAir(new LocalTileCoord(at.X - 1, at.Y + 1)) ||
                   isAir(new LocalTileCoord(at.X + 1, at.Y)) &&
                       isAir(new LocalTileCoord(at.X + 1, at.Y + 1));
        }

        private static bool IsOneWayPit(
            LocalTileCoord coordinate,
            IReadOnlyDictionary<LocalTileCoord, GeneratedTileMovementNode[]> standByCell,
            GeneratedTileMovementGraph graph,
            ISet<LocalTileCoord> reachableAirCells,
            IReadOnlyDictionary<string, List<Move>> adjacency,
            IReadOnlyList<string> acceptedTargets)
        {
            if (!standByCell.TryGetValue(coordinate, out var nodes)) return true;
            var nodeIds = new HashSet<string>(nodes.Select(value => value.NodeId),
                StringComparer.Ordinal);
            var nodesById = graph.Nodes.ToDictionary(value => value.NodeId,
                StringComparer.Ordinal);
            var hasReachableDrop = graph.Edges.Any(value =>
                value.MovementKind == TraversalMovementKind.Drop &&
                nodeIds.Contains(value.ToNodeId) &&
                nodesById.TryGetValue(value.FromNodeId, out var source) &&
                reachableAirCells.Contains(source.Cell));
            if (!hasReachableDrop) return false;
            return nodes.All(node => !acceptedTargets.Any(target =>
                Search(adjacency, node.NodeId).Distance.ContainsKey(target)));
        }

        private static bool OverprotectedEnvelope(SectorCanvasCell cell) =>
            cell.Provenance != null && cell.Provenance.Sources.Any(source => source != null &&
                source.IsProtected && source.Kind != CanvasSourceKind.TerrainCluster &&
                (source.OwnedLayers.Contains(SectorCanvasLayerKind.Solid) ||
                 source.OwnedLayers.Contains(SectorCanvasLayerKind.Hazard)));

        private static void AddDensityFailures(
            ClusterDensitySource source,
            int windows,
            IReadOnlyCollection<SectorCanvasCell> pockets,
            IReadOnlyCollection<SectorCanvasCell> snags,
            IReadOnlyCollection<SectorCanvasCell> pits,
            IReadOnlyCollection<SectorCanvasCell> hard,
            IReadOnlyCollection<SectorCanvasCell> overprotected,
            IReadOnlyCollection<SectorCanvasCell> boundary,
            ICollection<ClusterRecoveryFailure> failures)
        {
            var cluster = source.ClusterId.Value;
            if (windows < source.RequiredEightBySixAirWindows)
                Add(failures, source.SourceOwner, "MISSING_8X6_AIR_WINDOW", cluster,
                    "density.window", ">= " + Number(source.RequiredEightBySixAirWindows),
                    Number(windows), source.SourceDigest, Window(source));
            foreach (var cell in pockets)
                Add(failures, source.SourceOwner, "UNREACHABLE_AIR_POCKET", cluster,
                    "density.pocket", "GRAPH_REACHABLE_AIR", Coordinate(cell.Coordinate),
                    source.SourceDigest, Coordinate(cell.Coordinate));
            foreach (var cell in snags)
                Add(failures, source.SourceOwner, "HEAD_SNAG_CANDIDATE", cluster,
                    "density.head_snag", "CLEAR_PLAYER_HEIGHT", Coordinate(cell.Coordinate),
                    source.SourceDigest, Coordinate(cell.Coordinate));
            foreach (var cell in pits)
                Add(failures, source.SourceOwner, "ONE_WAY_PIT_CANDIDATE", cluster,
                    "density.one_way_pit", "GRAPH_BACKED_RECOVERY", Coordinate(cell.Coordinate),
                    source.SourceDigest, Coordinate(cell.Coordinate));
            foreach (var cell in hard)
                Add(failures, source.SourceOwner, "HARD_SOLID_OBSTRUCTION", cluster,
                    "density.hard_solid", "NO_GRAPH_NODE_IN_HARD_SOLID",
                    Coordinate(cell.Coordinate), source.SourceDigest,
                    Coordinate(cell.Coordinate));
            foreach (var cell in overprotected)
                Add(failures, source.SourceOwner, "OVERPROTECTED_ENVELOPE_OBSTRUCTION", cluster,
                    "density.overprotected_envelope", "NO_NON_CLUSTER_PROTECTED_BLOCKING_OWNER",
                    Coordinate(cell.Coordinate), source.SourceDigest,
                    Coordinate(cell.Coordinate));
            foreach (var cell in boundary)
                Add(failures, source.SourceOwner, "BOUNDARY_SOCKET_OBSTRUCTION", cluster,
                    "density.boundary_socket", "OPEN_SOCKET_CELL",
                    Coordinate(cell.Coordinate), source.SourceDigest,
                    Coordinate(cell.Coordinate));
        }

        private static Dictionary<string, List<Move>> BuildAdjacency(
            GeneratedTileMovementGraph graph,
            ISet<TraversalMovementKind> allowed)
        {
            var result = graph.Nodes.ToDictionary(value => value.NodeId,
                _ => new List<Move>(), StringComparer.Ordinal);
            foreach (var edge in graph.Edges.Where(value => allowed.Contains(value.MovementKind)))
                result[edge.FromNodeId].Add(new Move(edge.ToNodeId, edge.EdgeId));
            foreach (var link in graph.SocketLinks)
            {
                result[link.FromSocketNodeId].Add(new Move(link.ToSocketNodeId,
                    link.LinkId + ":F"));
                result[link.ToSocketNodeId].Add(new Move(link.FromSocketNodeId,
                    link.LinkId + ":R"));
            }
            foreach (var moves in result.Values) moves.Sort();
            return result;
        }

        private static SearchEvidence Search(
            IReadOnlyDictionary<string, List<Move>> adjacency,
            string start)
        {
            var queue = new Queue<string>();
            var distance = new Dictionary<string, int>(StringComparer.Ordinal);
            if (!adjacency.ContainsKey(start)) return new SearchEvidence(distance, 0);
            queue.Enqueue(start);
            distance[start] = 0;
            var visitedEdges = 0;
            while (queue.Count != 0)
            {
                var node = queue.Dequeue();
                foreach (var move in adjacency[node])
                {
                    visitedEdges++;
                    if (distance.ContainsKey(move.ToNodeId)) continue;
                    distance[move.ToNodeId] = distance[node] + 1;
                    queue.Enqueue(move.ToNodeId);
                }
            }
            return new SearchEvidence(distance, visitedEdges);
        }

        private static IReadOnlyList<ClusterRecoveryFrontierEvidence> Frontier(
            SearchEvidence search,
            IReadOnlyDictionary<string, List<Move>> adjacency) => search.Distance
            .OrderByDescending(value => value.Value).ThenBy(value => value.Key,
                StringComparer.Ordinal).Take(8).Select(value =>
                new ClusterRecoveryFrontierEvidence(value.Key, value.Value,
                    adjacency[value.Key].Count)).ToArray();

        private static bool Solid(SectorCanvasCell cell) => cell.Layers == null ||
            string.Equals(cell.Layers.Solid.StableId, "SOLID_BLOCKED",
                StringComparison.Ordinal) ||
            cell.Layers.Solid.StableId.IndexOf("HARD_SOLID",
                StringComparison.OrdinalIgnoreCase) >= 0 ||
            !cell.Layers.Hazard.IsExplicitEmpty;
        private static string Coordinate(LocalTileCoord value) =>
            Number(value.X) + "," + Number(value.Y);
        private static string Window(ClusterDensitySource source) =>
            source.MinimumX + "," + source.MinimumY + ".." + source.MaximumX + "," +
            source.MaximumY + "|REQUIRED=8x6";
        private static ClusterRecoveryValidationResult Failure(
            IEnumerable<ClusterRecoveryFailure> failures) =>
            new ClusterRecoveryValidationResult(null, failures);

        private static void Digest(
            ICollection<ClusterRecoveryFailure> failures,
            string owner,
            string reason,
            string cluster,
            string key,
            string expected,
            string actual,
            string sourceDigest)
        {
            if (!BakingCanonicalDigest.IsLowerHexSha256(actual) ||
                !string.Equals(actual, expected, StringComparison.Ordinal))
                Add(failures, owner, reason, cluster, key, expected,
                    string.IsNullOrEmpty(actual) ? "MISSING" : actual, sourceDigest);
        }

        private static void Add(
            ICollection<ClusterRecoveryFailure> failures,
            string owner,
            string reason,
            string cluster,
            string key,
            string expected,
            string actual,
            string sourceDigest,
            string window = "NONE") => failures.Add(new ClusterRecoveryFailure(owner, reason,
            cluster, key, key, expected, actual, sourceDigest, null, window));
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private sealed class Move : IComparable<Move>
        {
            public Move(string toNodeId, string sourceId)
            {
                ToNodeId = toNodeId;
                SourceId = sourceId;
            }
            public string ToNodeId { get; }
            public string SourceId { get; }
            public int CompareTo(Move other)
            {
                if (other == null) return -1;
                var target = string.Compare(ToNodeId, other.ToNodeId, StringComparison.Ordinal);
                return target != 0 ? target : string.Compare(SourceId, other.SourceId,
                    StringComparison.Ordinal);
            }
        }

        private sealed class SearchEvidence
        {
            public SearchEvidence(IReadOnlyDictionary<string, int> distance, int visitedEdges)
            {
                Distance = distance;
                VisitedEdges = visitedEdges;
            }
            public IReadOnlyDictionary<string, int> Distance { get; }
            public int VisitedEdges { get; }
        }
    }
}
