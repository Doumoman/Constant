using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class WorldAssemblyOverlayExporter
    {
        public const string ReferencePublicationLabel = "REFERENCE WORLD ASSEMBLY OVERLAY EXPORT";

        public static WorldAssemblyOverlayResult Export(WorldAssemblyOverlayRequest request)
        {
            var failures = Validate(request);
            if (failures.Count > 0) return WorldAssemblyOverlayResult.Fail(failures);

            var sectors = BuildSectors(request);
            var edges = BuildEdges(request);
            var hashes = BuildHashRecords(request);
            var upperBounds = BuildUpperBounds(request);
            var report = BuildBatchReport(request, upperBounds);
            var layers = BuildLayers(request, sectors, edges, hashes);

            ValidateConstructedExport(request, sectors, edges, hashes, layers, report, failures);
            if (failures.Count > 0) return WorldAssemblyOverlayResult.Fail(failures);

            var outputDigest = WorldAssemblyOverlayDigest.ComputeOutput(
                request.CanonicalDigest, sectors, edges, layers, hashes, report);
            var export = new WorldAssemblyOverlayExport(
                request, sectors, edges, layers, hashes, report, outputDigest);
            return WorldAssemblyOverlayResult.Pass(export);
        }

        private static List<WorldAssemblyOverlayFailure> Validate(WorldAssemblyOverlayRequest request)
        {
            var failures = new List<WorldAssemblyOverlayFailure>();
            if (request == null)
            {
                Add(failures, WorldAssemblyOverlayFailureCode.MissingRequest,
                    "REQUEST", "Overlay export request is required.");
                return failures;
            }

            if (request.SolveOrder == null)
                Add(failures, WorldAssemblyOverlayFailureCode.MissingWorldSolveOrder,
                    "MAP15_01", "World solve order is required.");
            else if (!request.SolveOrder.Success)
                Add(failures, WorldAssemblyOverlayFailureCode.FailedWorldSolveOrder,
                    "MAP15_01", "World solve order must be successful.");
            if (request.IntersectorPlan == null)
                Add(failures, WorldAssemblyOverlayFailureCode.MissingIntersectorPlan,
                    "MAP15_02", "Intersector edge plan is required.");
            if (request.ReservationPlan == null)
                Add(failures, WorldAssemblyOverlayFailureCode.MissingReservationPlan,
                    "MAP15_03", "Reservation plan is required.");
            if (request.PacingDensityPlan == null)
                Add(failures, WorldAssemblyOverlayFailureCode.MissingPacingDensityPlan,
                    "MAP15_04", "Pacing density plan is required.");
            if (request.RollbackPlan == null)
                Add(failures, WorldAssemblyOverlayFailureCode.MissingRollbackPlan,
                    "MAP15_05", "Neighbor rollback plan is required.");
            if (failures.Count > 0) return failures;

            var world = request.SolveOrder.Input;
            if (world == null || WorldPlanInput.WorldWidthTiles != WorldAssemblyOverlayExport.WorldWidthTiles ||
                WorldPlanInput.WorldHeightTiles != WorldAssemblyOverlayExport.WorldHeightTiles ||
                WorldPlanInput.SectorWidthTiles != WorldAssemblyOverlayExport.SectorWidthTiles ||
                WorldPlanInput.SectorHeightTiles != WorldAssemblyOverlayExport.SectorHeightTiles ||
                WorldPlanInput.SectorColumns != WorldAssemblyOverlayExport.SectorColumns ||
                WorldPlanInput.SectorRows != WorldAssemblyOverlayExport.SectorRows)
            {
                Add(failures, WorldAssemblyOverlayFailureCode.InvalidWorldDimensions,
                    "624x416_48x32_13x13", "World and sector dimensions must match the locked MAP15 grid.");
            }

            if (world == null || world.Nodes.Count != WorldAssemblyOverlayExport.WorldSectorCount ||
                request.SolveOrder.Steps.Count != WorldAssemblyOverlayExport.WorldSectorCount ||
                world.Nodes.Select(value => value.Id).Distinct().Count() != WorldAssemblyOverlayExport.WorldSectorCount ||
                world.Nodes.Any(value => !value.Coordinate.IsInBounds || value.Coordinate.RowMajorId != value.Id))
            {
                Add(failures, WorldAssemblyOverlayFailureCode.InvalidWorldSectorCount,
                    "SECTORS", "Exactly 169 distinct row-major sectors and solve steps are required.");
            }

            if (request.IntersectorPlan.Edges.Count != WorldAssemblyOverlayExport.InternalEdgeCount ||
                request.IntersectorPlan.Edges.Select(value => value.Id).Distinct().Count() !=
                WorldAssemblyOverlayExport.InternalEdgeCount)
            {
                Add(failures, WorldAssemblyOverlayFailureCode.InvalidInternalEdgeCount,
                    "EDGES", "Exactly 312 distinct internal edges are required.");
            }

            if (request.IntersectorPlan.Edges.Sum(value => value.Endpoints.Count) !=
                    WorldAssemblyOverlayExport.EdgeEndpointCount ||
                request.IntersectorPlan.Edges.Any(value => value.Endpoints.Count != 2))
            {
                Add(failures, WorldAssemblyOverlayFailureCode.InvalidEndpointCount,
                    "ENDPOINTS", "Exactly 624 edge endpoints, two per edge, are required.");
            }

            ValidateAuthorityChain(request, failures);
            ValidateDigests(request, failures);
            ValidateBatchLabels(request, failures);
            ValidateForbiddenClaims(request, failures);
            return failures;
        }

        private static void ValidateAuthorityChain(
            WorldAssemblyOverlayRequest request,
            ICollection<WorldAssemblyOverlayFailure> failures)
        {
            var solve = request.SolveOrder;
            var edges = request.IntersectorPlan;
            var reservations = request.ReservationPlan;
            var pacing = request.PacingDensityPlan;
            var rollback = request.RollbackPlan;
            var valid = edges.Request != null && reservations.Request != null && pacing.Request != null &&
                        rollback.Request != null &&
                        edges.Request.SolveOrder != null &&
                        edges.Request.SolveOrder.OutputDigest == solve.OutputDigest &&
                        reservations.Request.SolveOrder != null &&
                        reservations.Request.SolveOrder.OutputDigest == solve.OutputDigest &&
                        reservations.Request.IntersectorPlan != null &&
                        reservations.Request.IntersectorPlan.OutputDigest == edges.OutputDigest &&
                        pacing.Request.SolveOrder != null &&
                        pacing.Request.SolveOrder.OutputDigest == solve.OutputDigest &&
                        pacing.Request.IntersectorPlan != null &&
                        pacing.Request.IntersectorPlan.OutputDigest == edges.OutputDigest &&
                        pacing.Request.ReservationPlan != null &&
                        pacing.Request.ReservationPlan.OutputDigest == reservations.OutputDigest &&
                        rollback.WorldSolveOrderDigest == solve.OutputDigest &&
                        rollback.IntersectorPlanDigest == edges.OutputDigest &&
                        rollback.ReservationPlanIdentity == reservations.OutputDigest &&
                        rollback.PacingDensityPlanIdentity == pacing.OutputDigest;
            if (!valid)
                Add(failures, WorldAssemblyOverlayFailureCode.InvalidAuthorityLink,
                    "MAP15_01_TO_MAP15_05", "Public upstream input/output authority links must form one chain.");
        }

        private static void ValidateDigests(
            WorldAssemblyOverlayRequest request,
            ICollection<WorldAssemblyOverlayFailure> failures)
        {
            var values = new[]
            {
                request.SolveOrder.InputDigest, request.SolveOrder.OutputDigest,
                request.IntersectorPlan.InputDigest, request.IntersectorPlan.OutputDigest,
                request.ReservationPlan.InputDigest, request.ReservationPlan.OutputDigest,
                request.PacingDensityPlan.InputDigest, request.PacingDensityPlan.OutputDigest,
                request.RollbackPlan.InputDigest, request.RollbackPlan.OutputDigest,
                request.CanonicalDigest,
            };
            if (values.Any(value => !IsLowerSha256(value)))
                Add(failures, WorldAssemblyOverlayFailureCode.InvalidDigest,
                    "HASH_CHAIN", "Every MAP15 input/output identity must be a lowercase SHA-256 digest.");
        }

        private static void ValidateBatchLabels(
            WorldAssemblyOverlayRequest request,
            ICollection<WorldAssemblyOverlayFailure> failures)
        {
            var labels = request.BatchCaseLabels;
            if (request.NullBatchCaseLabelCount != 0 || labels.Count !=
                    WorldAssemblyOverlayExport.RequiredBatchCaseCount ||
                labels.Distinct(StringComparer.Ordinal).Count() != labels.Count ||
                !labels.OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(
                    WorldAssemblyOverlayExport.RequiredBatchLabels.OrderBy(value => value, StringComparer.Ordinal),
                    StringComparer.Ordinal))
            {
                Add(failures, WorldAssemblyOverlayFailureCode.InvalidBatchLabels,
                    "REFERENCE_WORLD_CASES", "Exactly the four required abstract reference labels are required.");
            }

            if (request.ProductionSeedApprovalCount != 0 ||
                labels.Any(value => value.IndexOf("PRODUCTION", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                Add(failures, WorldAssemblyOverlayFailureCode.ProductionSeedApprovalForbidden,
                    "PRODUCTION_APPROVAL", "MAP15_06 cannot publish a production-seed approval claim.");
            }

            if (!string.Equals(request.PublicationLabel, ReferencePublicationLabel, StringComparison.Ordinal))
                Add(failures, WorldAssemblyOverlayFailureCode.InvalidAuthorityLink,
                    "PUBLICATION", "Reference overlay publication label is required.");
        }

        private static void ValidateForbiddenClaims(
            WorldAssemblyOverlayRequest request,
            ICollection<WorldAssemblyOverlayFailure> failures)
        {
            var rerandom = request.WholeWorldRerandomCount +
                           (request.SolveOrder.WholeWorldRerandom ? 1 : 0) +
                           request.RollbackPlan.WholeWorldRerandomCount;
            if (rerandom != 0)
                Add(failures, WorldAssemblyOverlayFailureCode.WholeWorldRerandomForbidden,
                    "WHOLE_WORLD_RERANDOM", "Whole-world rerandom is forbidden.");

            var carve = request.FallbackCarveCount + request.SolveOrder.FallbackCarveCount +
                        request.IntersectorPlan.FallbackCarveCount + request.ReservationPlan.FallbackCarveCount +
                        request.PacingDensityPlan.FallbackCarveCount + request.RollbackPlan.FallbackCarveCount;
            if (carve != 0)
                Add(failures, WorldAssemblyOverlayFailureCode.FallbackCarveForbidden,
                    "FALLBACK_CARVE", "Fallback carve is forbidden.");

            var widening = request.SilentWideningCount + request.RollbackPlan.SilentWideningCount;
            if (widening != 0)
                Add(failures, WorldAssemblyOverlayFailureCode.SilentWideningForbidden,
                    "SILENT_WIDENING", "Silent widening is forbidden.");

            if (TotalFileWrites(request) != 0)
                Add(failures, WorldAssemblyOverlayFailureCode.FileWriteForbidden,
                    "FILE_WRITE", "Runtime overlay export must remain in-memory.");

            if (TotalMutations(request) != 0 || request.GameplaySpawnCount != 0 ||
                request.AuthoringMutationCount != 0 || request.WorldPlanMutationCount != 0 ||
                request.IntersectorPlanMutationCount != 0 || request.ReservationPlanMutationCount != 0 ||
                request.PacingDensityPlanMutationCount != 0 || request.RollbackPlanMutationCount != 0 ||
                request.FullRegressionCount != 0)
            {
                Add(failures, WorldAssemblyOverlayFailureCode.MutationClaim,
                    "MUTATION", "Overlay export cannot mutate plans, authoring, tiles, scenes, prefabs, objects, or gameplay.");
            }
        }

        private static WorldAssemblyOverlaySector[] BuildSectors(WorldAssemblyOverlayRequest request)
        {
            var steps = request.SolveOrder.Steps.ToDictionary(value => value.SectorId, value => value.StepIndex);
            var rollback = new HashSet<WorldSectorId>(request.RollbackPlan.Scope.Sectors
                .Select(value => value.SectorId));
            var failures = request.RollbackPlan.FailureReport.Observations
                .GroupBy(value => value.SectorId).ToDictionary(value => value.Key, value => value.Count());
            return request.SolveOrder.Input.Nodes.OrderBy(value => value.Id).Select(node =>
            {
                var transactions = request.ReservationPlan.Transactions.Count(value =>
                    value.SectorIds.Contains(node.Id));
                var claims = request.ReservationPlan.Claims.Count(value => value.SectorId == node.Id);
                var windows = request.PacingDensityPlan.Windows.Count(value => value.SectorIds.Contains(node.Id));
                var budgets = request.PacingDensityPlan.Budgets.Count(value => value.SectorId == node.Id);
                var failureCount = failures.ContainsKey(node.Id) ? failures[node.Id] : 0;
                var token = string.Join("|", new[]
                {
                    "SECTOR", Number3(node.Id.Value), Number(node.Coordinate.X), Number(node.Coordinate.Y),
                    Number3(steps[node.Id]), Number(node.RouteType), node.AccessClass.ToString(),
                    node.PacingRole.ToString(), Number(node.HasSpecialReservation ? 1 : 0),
                    Number(transactions + claims), Number(windows), Number(budgets),
                    Number(rollback.Contains(node.Id) ? 1 : 0), Number(failureCount),
                });
                return new WorldAssemblyOverlaySector(
                    node.Id, node.Coordinate, steps[node.Id], node.RouteType, node.AccessClass, node.PacingRole,
                    node.HasSpecialReservation ? 1 : 0, transactions + claims, windows, budgets,
                    rollback.Contains(node.Id) ? 1 : 0, failureCount, token);
            }).ToArray();
        }

        private static WorldAssemblyOverlayEdge[] BuildEdges(WorldAssemblyOverlayRequest request) =>
            request.IntersectorPlan.Edges.OrderBy(value => value.Id).Select(edge =>
            {
                var route = edge.RouteSignature;
                var token = string.Join("|", new[]
                {
                    "EDGE", Number3(edge.Id.MinSector.Value), Number3(edge.Id.MaxSector.Value),
                    edge.Orientation.ToString(), Number(edge.Endpoints.Count),
                    route != null && route.Compatible ? "1" : "0", edge.IsBoundary ? "1" : "0",
                    route != null && route.MandatoryRoute ? "1" : "0",
                    route != null && route.ExternalSocket ? "1" : "0", edge.CanonicalDigest,
                });
                return new WorldAssemblyOverlayEdge(
                    edge.Id, edge.Id.MinSector, edge.Id.MaxSector, edge.Orientation, edge.Endpoints.Count,
                    route != null && route.Compatible, edge.IsBoundary,
                    route != null && route.MandatoryRoute, route != null && route.ExternalSocket,
                    edge.CanonicalDigest, token);
            }).ToArray();

        private static WorldAssemblyHashRecord[] BuildHashRecords(WorldAssemblyOverlayRequest request) => new[]
        {
            Hash("MAP15_01", WorldAssemblyHashKind.Input, request.SolveOrder.InputDigest),
            Hash("MAP15_01", WorldAssemblyHashKind.Output, request.SolveOrder.OutputDigest),
            Hash("MAP15_02", WorldAssemblyHashKind.Input, request.IntersectorPlan.InputDigest),
            Hash("MAP15_02", WorldAssemblyHashKind.Output, request.IntersectorPlan.OutputDigest),
            Hash("MAP15_03", WorldAssemblyHashKind.Input, request.ReservationPlan.InputDigest),
            Hash("MAP15_03", WorldAssemblyHashKind.Output, request.ReservationPlan.OutputDigest),
            Hash("MAP15_04", WorldAssemblyHashKind.Input, request.PacingDensityPlan.InputDigest),
            Hash("MAP15_04", WorldAssemblyHashKind.Output, request.PacingDensityPlan.OutputDigest),
            Hash("MAP15_05", WorldAssemblyHashKind.Input, request.RollbackPlan.InputDigest),
            Hash("MAP15_05", WorldAssemblyHashKind.Output, request.RollbackPlan.OutputDigest),
        };

        private static WorldSolverUpperBound[] BuildUpperBounds(WorldAssemblyOverlayRequest request)
        {
            var combinedMutations = TotalMutations(request);
            return new[]
            {
                Bound(WorldSolverUpperBoundKind.SolveSteps, request.SolveOrder.Steps.Count, 169, "MAP15_01"),
                Bound(WorldSolverUpperBoundKind.InternalEdges, request.IntersectorPlan.Edges.Count, 312, "MAP15_02"),
                Bound(WorldSolverUpperBoundKind.EdgeEndpoints,
                    request.IntersectorPlan.Edges.Sum(value => value.Endpoints.Count), 624, "MAP15_02"),
                Bound(WorldSolverUpperBoundKind.RollbackSectorsPerFailure,
                    request.RollbackPlan.Scope.SectorCount, 9, "MAP15_05"),
                Bound(WorldSolverUpperBoundKind.SectorLocalRetryAttempts,
                    request.SolveOrder.RetryEnvelope.MaxSectorLocalAttemptsPerNode, 6, "MAP15_01"),
                Bound(WorldSolverUpperBoundKind.WholeWorldRerandom,
                    request.WholeWorldRerandomCount + request.RollbackPlan.WholeWorldRerandomCount, 0, "MAP15_05"),
                Bound(WorldSolverUpperBoundKind.FallbackCarve,
                    request.FallbackCarveCount + request.RollbackPlan.FallbackCarveCount, 0, "MAP15_05"),
                Bound(WorldSolverUpperBoundKind.SilentWidening,
                    request.SilentWideningCount + request.RollbackPlan.SilentWideningCount, 0, "MAP15_05"),
                Bound(WorldSolverUpperBoundKind.FileWrites, TotalFileWrites(request), 0, "MAP15_01_TO_MAP15_06"),
                Bound(WorldSolverUpperBoundKind.ScenePrefabTilemapGameObjectMutations,
                    combinedMutations, 0, "MAP15_01_TO_MAP15_06"),
            };
        }

        private static WorldBatchPlanReport BuildBatchReport(
            WorldAssemblyOverlayRequest request,
            IEnumerable<WorldSolverUpperBound> bounds)
        {
            var boundArray = bounds.OrderBy(value => value).ToArray();
            var connectedComponents = ConnectedComponentCount(
                request.SolveOrder.Input.Nodes.Select(value => value.Id), request.IntersectorPlan.Edges);
            var duplicateIds = request.SolveOrder.Input.Nodes.Count -
                               request.SolveOrder.Input.Nodes.Select(value => value.Id).Distinct().Count() +
                               request.IntersectorPlan.Edges.Count -
                               request.IntersectorPlan.Edges.Select(value => value.Id).Distinct().Count();
            var missingBoundaryPairs = request.IntersectorPlan.Edges.Count(value => value.IsBoundary &&
                (value.Boundary == null || string.IsNullOrEmpty(value.Boundary.PairId) ||
                 string.IsNullOrEmpty(value.Boundary.ProfileId) || string.IsNullOrEmpty(value.Boundary.CandidateId)));
            var untypedConflicts = request.ReservationPlan.Conflicts.Count(value =>
                string.IsNullOrEmpty(value.Subject) || string.IsNullOrEmpty(value.Reason));
            var pacingViolations = request.PacingDensityPlan.Violations.Count;
            var upperPass = boundArray.All(value => value.Pass);
            var cases = WorldAssemblyOverlayExport.RequiredBatchLabels.Select(label => new WorldBatchPlanCase(
                label, connectedComponents, duplicateIds, missingBoundaryPairs, untypedConflicts,
                pacingViolations, request.RollbackPlan.Scope.SectorCount, upperPass, false));
            return new WorldBatchPlanReport(cases, boundArray);
        }

        private static WorldAssemblyOverlayLayer[] BuildLayers(
            WorldAssemblyOverlayRequest request,
            IEnumerable<WorldAssemblyOverlaySector> sourceSectors,
            IEnumerable<WorldAssemblyOverlayEdge> sourceEdges,
            IEnumerable<WorldAssemblyHashRecord> sourceHashes)
        {
            var sectors = sourceSectors.OrderBy(value => value).ToArray();
            var edges = sourceEdges.OrderBy(value => value).ToArray();
            var hashes = sourceHashes.OrderBy(value => value).ToArray();
            return new[]
            {
                Layer(WorldAssemblyOverlayLayerKind.Topology, new[]
                {
                    "TOPOLOGY|624|416|48|32|13|13|169|312|624",
                }),
                Layer(WorldAssemblyOverlayLayerKind.SolveOrder,
                    sectors.OrderBy(value => value.SolveStepIndex).ThenBy(value => value.SectorId)
                        .Select(value => "SOLVE|" + Number3(value.SolveStepIndex) + "|" +
                                         Number3(value.SectorId.Value))),
                Layer(WorldAssemblyOverlayLayerKind.IntersectorEdges,
                    edges.Select(value => value.StableToken)),
                Layer(WorldAssemblyOverlayLayerKind.BoundaryPairs,
                    request.IntersectorPlan.Edges.Where(value => value.IsBoundary).Select(value => string.Join("|",
                        new[] { "BOUNDARY", Number3(value.Id.MinSector.Value), Number3(value.Id.MaxSector.Value),
                            Safe(value.Boundary.PairId), Safe(value.Boundary.ProfileId), Safe(value.Boundary.CandidateId) })),
                    "NO_PUBLIC_BOUNDARY_PAIRS"),
                Layer(WorldAssemblyOverlayLayerKind.SpecialReservations,
                    request.ReservationPlan.Transactions.Select(value => string.Join("|", new[]
                    {
                        "SPECIAL", Safe(value.TransactionId), value.AuthorityKind.ToString(), value.State.ToString(),
                        Number(value.SectorIds.Count), Number(value.EdgeIds.Count),
                    })), "NO_PUBLIC_SPECIAL_RESERVATIONS"),
                Layer(WorldAssemblyOverlayLayerKind.ClusterReservations,
                    request.ReservationPlan.ClusterPolicies.Select(value => string.Join("|", new[]
                    {
                        "CLUSTER", Safe(value.PolicyId), Safe(value.ClusterId.Value), Safe(value.VariantId.Value),
                        value.SpanKind.ToString(), value.Accepted ? "1" : "0",
                    })), "NO_PUBLIC_CLUSTER_RESERVATIONS"),
                Layer(WorldAssemblyOverlayLayerKind.PacingDensity,
                    request.PacingDensityPlan.Windows.Select(value => string.Join("|", new[]
                    {
                        "PACING", Safe(value.WindowId), value.Kind.ToString(), Number(value.MinimumCount),
                        Number(value.MaximumCount), Number(value.ObservedCount),
                    })).Concat(request.PacingDensityPlan.Budgets.Select(value => string.Join("|", new[]
                    {
                        "BUDGET", Number(value.SectorId.Value), value.Kind.ToString(),
                        Number(value.ObservedSolidBudget), Number(value.ObservedReachableBudget),
                    }))), "NO_PUBLIC_PACING_DENSITY"),
                Layer(WorldAssemblyOverlayLayerKind.ActivityEventCaps,
                    request.PacingDensityPlan.Request.ActivityEventConstraints.Select(value => string.Join("|", new[]
                    {
                        "CAP", Safe(value.ConstraintId), value.Kind.ToString(), Number(value.TargetPermille),
                        Number(value.MaximumCount), Safe(value.AuthorityDigest),
                    })), "NO_PUBLIC_ACTIVITY_EVENT_CAPS"),
                Layer(WorldAssemblyOverlayLayerKind.RollbackScopes,
                    request.RollbackPlan.Scope.Sectors.Select(value => string.Join("|", new[]
                    {
                        "ROLLBACK", Number(value.SectorId.Value), Number(value.SolveStepIndex),
                        value.IsFailedSector ? "1" : "0",
                    })), "NO_PUBLIC_ROLLBACK_SCOPE"),
                Layer(WorldAssemblyOverlayLayerKind.FailureReports,
                    request.RollbackPlan.FailureReport.Observations.Select(value => string.Join("|", new[]
                    {
                        "FAILURE", Safe(value.StableContradictionId), value.Kind.ToString(), value.Source.ToString(),
                        Number(value.SectorId.Value), Number(value.SolveStepIndex),
                    })), "NO_PUBLIC_FAILURE_REPORT"),
                Layer(WorldAssemblyOverlayLayerKind.HashChain,
                    hashes.Select(value => value.StableToken)),
                Layer(WorldAssemblyOverlayLayerKind.MutationProof, new[]
                {
                    "MUTATION_PROOF|FILE0|TILEMAP0|SCENE0|PREFAB0|GAMEOBJECT0|GAMEPLAY0|AUTHORING0|PLAN0",
                }),
            };
        }

        private static void ValidateConstructedExport(
            WorldAssemblyOverlayRequest request,
            IReadOnlyCollection<WorldAssemblyOverlaySector> sectors,
            IReadOnlyCollection<WorldAssemblyOverlayEdge> edges,
            IReadOnlyCollection<WorldAssemblyHashRecord> hashes,
            IReadOnlyCollection<WorldAssemblyOverlayLayer> layers,
            WorldBatchPlanReport report,
            ICollection<WorldAssemblyOverlayFailure> failures)
        {
            if (sectors.Count != WorldAssemblyOverlayExport.WorldSectorCount)
                Add(failures, WorldAssemblyOverlayFailureCode.InvalidWorldSectorCount,
                    "OVERLAY_SECTORS", "Overlay must contain 169 sectors.");
            if (edges.Count != WorldAssemblyOverlayExport.InternalEdgeCount)
                Add(failures, WorldAssemblyOverlayFailureCode.InvalidInternalEdgeCount,
                    "OVERLAY_EDGES", "Overlay must contain 312 edges.");
            if (hashes.Count != WorldAssemblyOverlayExport.RequiredHashRecordCount ||
                hashes.Select(value => value.TaskId + "|" + value.Kind).Distinct().Count() !=
                WorldAssemblyOverlayExport.RequiredHashRecordCount)
                Add(failures, WorldAssemblyOverlayFailureCode.InvalidDigest,
                    "HASH_RECORDS", "Exactly ten MAP15_01 through MAP15_05 input/output hash records are required.");
            if (layers.Count != WorldAssemblyOverlayExport.RequiredLayerCount ||
                layers.Select(value => value.Kind).Distinct().Count() != WorldAssemblyOverlayExport.RequiredLayerCount ||
                layers.Any(value => value.ItemCount == 0 && !value.HasExplicitUnavailableReason))
                Add(failures, WorldAssemblyOverlayFailureCode.MissingRequiredLayer,
                    "LAYERS", "All twelve layers require evidence or an explicit unavailable reason.");
            if (report == null || !report.Pass)
                Add(failures, WorldAssemblyOverlayFailureCode.SolverUpperBoundExceeded,
                    "BATCH_REPORT", "All four abstract cases and ten upper bounds must pass.");

            var tokens = sectors.Select(value => value.StableToken)
                .Concat(edges.Select(value => value.StableToken))
                .Concat(hashes.Select(value => value.StableToken))
                .Concat(layers.SelectMany(value => value.Tokens))
                .Concat(report == null ? Array.Empty<string>() : report.Cases.Select(value => value.StableToken))
                .Concat(report == null ? Array.Empty<string>() : report.SolverUpperBounds.Select(value => value.StableToken));
            if (tokens.Any(value => string.IsNullOrEmpty(value) || value.Contains("/") || value.Contains("\\") ||
                                    value.Contains("\r") || value.Contains("\n")))
                Add(failures, WorldAssemblyOverlayFailureCode.InvalidOverlayToken,
                    "TOKENS", "Stable overlay tokens cannot contain path-dependent separators or newlines.");

            if (request.ProductionSeedApprovalCount != 0)
                Add(failures, WorldAssemblyOverlayFailureCode.ProductionSeedApprovalForbidden,
                    "PRODUCTION_APPROVAL", "Production-seed approval remains outside MAP15_06.");
        }

        private static int ConnectedComponentCount(
            IEnumerable<WorldSectorId> sourceSectors,
            IEnumerable<WorldIntersectorEdge> sourceEdges)
        {
            var sectors = sourceSectors.Distinct().OrderBy(value => value).ToArray();
            var neighbors = sectors.ToDictionary(value => value, value => new List<WorldSectorId>());
            foreach (var edge in sourceEdges.OrderBy(value => value.Id))
            {
                if (!neighbors.ContainsKey(edge.Id.MinSector) || !neighbors.ContainsKey(edge.Id.MaxSector)) continue;
                neighbors[edge.Id.MinSector].Add(edge.Id.MaxSector);
                neighbors[edge.Id.MaxSector].Add(edge.Id.MinSector);
            }

            var visited = new HashSet<WorldSectorId>();
            var components = 0;
            foreach (var sector in sectors)
            {
                if (visited.Contains(sector)) continue;
                components++;
                var pending = new Queue<WorldSectorId>();
                pending.Enqueue(sector);
                visited.Add(sector);
                while (pending.Count > 0)
                {
                    foreach (var neighbor in neighbors[pending.Dequeue()].OrderBy(value => value))
                    {
                        if (visited.Add(neighbor)) pending.Enqueue(neighbor);
                    }
                }
            }
            return components;
        }

        private static int TotalFileWrites(WorldAssemblyOverlayRequest request) =>
            request.GeneratedFileWriteCount + request.SolveOrder.Input.GeneratedFileWriteCount +
            request.IntersectorPlan.GeneratedFileWriteCount + request.ReservationPlan.GeneratedFileWriteCount +
            request.PacingDensityPlan.GeneratedFileWriteCount + request.RollbackPlan.GeneratedFileWriteCount;

        private static int TotalMutations(WorldAssemblyOverlayRequest request) =>
            request.TilemapMutationCount + request.SceneMutationCount + request.PrefabMutationCount +
            request.GameObjectMutationCount + request.SolveOrder.Input.TilemapMutationCount +
            request.SolveOrder.Input.SceneMutationCount + request.SolveOrder.Input.PrefabMutationCount +
            request.SolveOrder.Input.GameObjectMutationCount + request.IntersectorPlan.TilemapMutationCount +
            request.IntersectorPlan.SceneMutationCount + request.IntersectorPlan.PrefabMutationCount +
            request.IntersectorPlan.GameObjectMutationCount + request.ReservationPlan.TilemapMutationCount +
            request.ReservationPlan.SceneMutationCount + request.ReservationPlan.PrefabMutationCount +
            request.ReservationPlan.GameObjectMutationCount + request.PacingDensityPlan.TilemapMutationCount +
            request.PacingDensityPlan.SceneMutationCount + request.PacingDensityPlan.PrefabMutationCount +
            request.PacingDensityPlan.GameObjectMutationCount + request.RollbackPlan.TilemapMutationCount +
            request.RollbackPlan.SceneMutationCount + request.RollbackPlan.PrefabMutationCount +
            request.RollbackPlan.GameObjectMutationCount;

        private static bool IsLowerSha256(string value) => value != null && value.Length == 64 &&
            value.All(character => (character >= '0' && character <= '9') ||
                                   (character >= 'a' && character <= 'f'));

        private static WorldAssemblyHashRecord Hash(string task, WorldAssemblyHashKind kind, string digest) =>
            new WorldAssemblyHashRecord(task, kind, digest);

        private static WorldSolverUpperBound Bound(
            WorldSolverUpperBoundKind kind,
            int actual,
            int limit,
            string owner) => new WorldSolverUpperBound(kind, actual, limit, owner);

        private static WorldAssemblyOverlayLayer Layer(
            WorldAssemblyOverlayLayerKind kind,
            IEnumerable<string> tokens,
            string unavailableReason = "")
        {
            var array = (tokens ?? Array.Empty<string>()).ToArray();
            return new WorldAssemblyOverlayLayer(
                kind,
                array.Length == 0 ? WorldAssemblyOverlaySeverity.Warning : WorldAssemblyOverlaySeverity.Information,
                array,
                array.Length == 0 ? unavailableReason : string.Empty);
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static string Number3(int value) => value.ToString("D3", CultureInfo.InvariantCulture);
        private static string Safe(string value) => WorldAssemblyOverlayDigest.Token(value);

        private static void Add(
            ICollection<WorldAssemblyOverlayFailure> failures,
            WorldAssemblyOverlayFailureCode code,
            string subject,
            string reason) => failures.Add(new WorldAssemblyOverlayFailure(code, subject, reason));
    }
}
