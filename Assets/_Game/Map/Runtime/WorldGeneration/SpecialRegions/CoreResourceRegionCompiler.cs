using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Pipeline;

namespace StarNight.Map.WorldGeneration.SpecialRegions
{
    public static class CoreResourceRegionCompiler
    {
        public static CoreResourceRegionResult Compile(CoreResourceRegionCompileRequest request)
        {
            if (request == null)
                return Failure(CoreResourceRegionErrorCode.MissingInput, "request", "Compile request is required.");

            var errors = new List<CoreResourceRegionError>();
            Require(request.Definition, "definition", errors);
            Require(request.Bridge, "bridge", errors);
            Require(request.EntryBufferPlan, "entryBufferPlan", errors);
            Require(request.CollisionPlan, "collisionPlan", errors);
            Require(request.FixedSlotLayerPlan, "fixedSlotLayerPlan", errors);
            Require(request.SafetyProof, "safetyProof", errors);
            if (errors.Count != 0)
                return new CoreResourceRegionResult(null, errors);

            ValidateSourceDigests(request, errors);
            ValidateIdentity(request, errors);
            ValidateFootprint(request, errors);
            ValidateDesignCanvas(request.Definition, errors);
            ValidateActiveChunks(request.Definition, errors);
            ValidateNodes(request, errors);
            ValidateEdges(request.Definition, errors);
            ValidateRoutes(request.Definition, errors);
            ValidateEnvironmentSolution(request.Definition, errors);
            ValidateRecovery(request.Definition, errors);
            ValidateReward(request, errors);
            ValidateSourcePublication(request, errors);

            if (request.Definition.SuppliedNullCount != 0)
                Add(errors, CoreResourceRegionErrorCode.NonCanonicalPublication,
                    "definition", "Null catalog members are not canonical.");
            if (errors.Count != 0)
                return new CoreResourceRegionResult(null, errors);

            var lowRoute = request.Definition.Routes.First(value => value.Kind == CoreResourceRouteKind.Low);
            var highRoute = request.Definition.Routes.First(value => value.Kind == CoreResourceRouteKind.High);
            var recoveryRoutes = request.Definition.Routes.Where(value =>
                value.Kind == CoreResourceRouteKind.Recovery).ToArray();
            var plan = new CoreResourceRegionPlan(
                request.Definition,
                BuildWitness(request.Definition, lowRoute),
                BuildWitness(request.Definition, highRoute),
                recoveryRoutes.Select(value => BuildWitness(request.Definition, value)),
                request.Bridge.CanonicalDigest,
                request.EntryBufferPlan.CanonicalDigest,
                request.CollisionPlan.CanonicalDigest,
                request.FixedSlotLayerPlan.CanonicalDigest,
                request.SafetyProof.CanonicalDigest);

            var digest = CoreResourceRegionCanonicalDigest.Compute(plan);
            if (!EqualsDigest(digest, plan.CanonicalDigest))
            {
                Add(errors, CoreResourceRegionErrorCode.NonCanonicalPublication,
                    "plan", "Published and recomputed plan digests must match.");
                return new CoreResourceRegionResult(null, errors);
            }

            return new CoreResourceRegionResult(plan, errors);
        }

        private static void ValidateSourceDigests(
            CoreResourceRegionCompileRequest request,
            ICollection<CoreResourceRegionError> errors)
        {
            ValidateDigest(
                SpecialRegionSiteBridgeCanonicalDigest.Compute(request.Bridge),
                request.Bridge.CanonicalDigest,
                request.ExpectedBridgeDigest,
                "bridge", errors);
            ValidateDigest(
                SpecialRegionEntryBufferCanonicalDigest.Compute(request.EntryBufferPlan),
                request.EntryBufferPlan.CanonicalDigest,
                request.ExpectedEntryBufferDigest,
                "entryBufferPlan", errors);
            ValidateDigest(
                SpecialRegionPlacementCollisionCanonicalDigest.Compute(request.CollisionPlan),
                request.CollisionPlan.CanonicalDigest,
                request.ExpectedCollisionDigest,
                "collisionPlan", errors);
            ValidateDigest(
                SpecialRegionFixedSlotLayerCanonicalDigest.Compute(request.FixedSlotLayerPlan),
                request.FixedSlotLayerPlan.CanonicalDigest,
                request.ExpectedFixedSlotLayerDigest,
                "fixedSlotLayerPlan", errors);
            ValidateDigest(
                SpecialRegionPersistenceSafetyCanonicalDigest.ComputeProof(request.SafetyProof),
                request.SafetyProof.CanonicalDigest,
                request.ExpectedSafetyProofDigest,
                "safetyProof", errors);
        }

        private static void ValidateIdentity(
            CoreResourceRegionCompileRequest request,
            ICollection<CoreResourceRegionError> errors)
        {
            var definition = request.Definition;
            if (definition.RegionKind != SpecialRegionKind.CoreResource ||
                request.Bridge.RegionKind != SpecialRegionKind.CoreResource ||
                request.FixedSlotLayerPlan.RegionKind != SpecialRegionKind.CoreResource)
                Add(errors, CoreResourceRegionErrorCode.NotCoreResource,
                    "regionKind", "Definition, bridge, and fixed-slot layer must be CoreResource.");

            if (definition.RegionId != request.Bridge.RegionId ||
                definition.RegionId != request.EntryBufferPlan.RegionId ||
                definition.RegionId != request.FixedSlotLayerPlan.RegionId ||
                definition.RegionId != request.SafetyProof.RegionId)
                Add(errors, CoreResourceRegionErrorCode.RegionIdentityMismatch,
                    "regionId", "All source layers must preserve the authored region identity.");

            if (request.Bridge.ReservationId != request.EntryBufferPlan.ReservationId ||
                request.Bridge.ReservationId != request.FixedSlotLayerPlan.ReservationId)
                Add(errors, CoreResourceRegionErrorCode.RegionIdentityMismatch,
                    "reservationId", "Bridge, entry buffer, and layer reservation identity must match.");

            if (!TryExpectedIdentity(definition.Resource, out var regionId, out var biome, out var mechanism) ||
                definition.RegionId != regionId || definition.Biome != biome || definition.Mechanism != mechanism)
                Add(errors, CoreResourceRegionErrorCode.RegionIdentityMismatch,
                    "definition", "Resource, region, biome, and mechanism must match the exact starter matrix.");
        }

        private static void ValidateFootprint(
            CoreResourceRegionCompileRequest request,
            ICollection<CoreResourceRegionError> errors)
        {
            var definition = request.Definition;
            var bridge = request.Bridge;
            if (definition.ReservedWidth != 1 || definition.ReservedHeight != 1 ||
                bridge.Width != 1 || bridge.Height != 1 ||
                bridge.SourceFootprint.Count != 1 || bridge.PlacedFootprint.Count != 1 ||
                bridge.SourceFootprint[0] != new SpecialRegionSectorOffset(0, 0) ||
                bridge.PlacedFootprint[0] != new SpecialRegionSectorOffset(0, 0))
                Add(errors, CoreResourceRegionErrorCode.UnsupportedFootprint,
                    "footprint", "CoreResource starters require one exact 1x1 sector footprint.");
        }

        private static void ValidateDesignCanvas(
            CoreResourceRegionDefinition definition,
            ICollection<CoreResourceRegionError> errors)
        {
            if (definition.DesignOrigin != new LocalTileCoord(6, 8) ||
                definition.DesignWidth != 36 || definition.DesignHeight != 16 ||
                definition.DesignChunkWidth != 12 || definition.DesignChunkHeight != 8 ||
                definition.DesignGridWidth != 3 || definition.DesignGridHeight != 2)
                Add(errors, CoreResourceRegionErrorCode.InvalidDesignCanvas,
                    "designCanvas", "Design canvas must be origin 6,8, size 36x16, and grid 3x2 of 12x8 chunks.");
        }

        private static void ValidateActiveChunks(
            CoreResourceRegionDefinition definition,
            ICollection<CoreResourceRegionError> errors)
        {
            var values = definition.ActiveDesignChunks.ToArray();
            if (values.Length != 5 || values.Distinct().Count() != 5 ||
                values.Any(value => value.X < 0 || value.X >= 3 || value.Y < 0 || value.Y >= 2))
            {
                Add(errors, CoreResourceRegionErrorCode.InvalidActiveChunk,
                    "activeDesignChunks", "Exactly five unique chunks inside the 3x2 design grid are required.");
                return;
            }

            var set = new HashSet<CoreResourceDesignChunk>(values);
            var visited = new HashSet<CoreResourceDesignChunk>();
            var pending = new Queue<CoreResourceDesignChunk>();
            pending.Enqueue(values[0]);
            visited.Add(values[0]);
            while (pending.Count != 0)
            {
                var current = pending.Dequeue();
                foreach (var next in new[]
                         {
                             new CoreResourceDesignChunk(current.X - 1, current.Y),
                             new CoreResourceDesignChunk(current.X + 1, current.Y),
                             new CoreResourceDesignChunk(current.X, current.Y - 1),
                             new CoreResourceDesignChunk(current.X, current.Y + 1),
                         })
                    if (set.Contains(next) && visited.Add(next)) pending.Enqueue(next);
            }
            if (visited.Count != values.Length)
                Add(errors, CoreResourceRegionErrorCode.InvalidActiveChunk,
                    "activeDesignChunks", "Active chunks must be four-neighbor connected.");
        }

        private static void ValidateNodes(
            CoreResourceRegionCompileRequest request,
            ICollection<CoreResourceRegionError> errors)
        {
            var definition = request.Definition;
            var groups = definition.Nodes.GroupBy(value => value.NodeId, StringComparer.Ordinal).ToArray();
            foreach (var group in groups.Where(value => value.Count() != 1))
                Add(errors, CoreResourceRegionErrorCode.DuplicateNode,
                    "nodes/" + group.Key, "Node IDs must be unique.");

            var fixedCells = new HashSet<SpecialRegionTileCoordinate>(
                request.FixedSlotLayerPlan.FixedCollision.Select(value => value.Coordinate));
            foreach (var node in definition.Nodes)
            {
                var path = "nodes/" + node.NodeId;
                if (!IsStableId(node.NodeId, "CR_NODE_") ||
                    !Enum.IsDefined(typeof(CoreResourceNodeRole), node.Role) ||
                    !Enum.IsDefined(typeof(CoreResourceMarkerKind), node.MarkerKind))
                    Add(errors, CoreResourceRegionErrorCode.NonCanonicalPublication,
                        path, "Node ID, role, and marker must be explicit canonical values.");

                var inRegion = node.Coordinate.X >= 0 && node.Coordinate.X < 48 &&
                               node.Coordinate.Y >= 0 && node.Coordinate.Y < 32;
                var inDesign = node.Coordinate.X >= definition.DesignOrigin.X &&
                               node.Coordinate.X < definition.DesignOrigin.X + definition.DesignWidth &&
                               node.Coordinate.Y >= definition.DesignOrigin.Y &&
                               node.Coordinate.Y < definition.DesignOrigin.Y + definition.DesignHeight;
                var approvedConnector = node.Role == CoreResourceNodeRole.Entry &&
                                        node.Coordinate == request.EntryBufferPlan.EntryPort.Placed.LocalTile ||
                                        node.Role == CoreResourceNodeRole.Return &&
                                        node.Coordinate == request.EntryBufferPlan.ReturnPort.Placed.LocalTile;
                if (!inRegion || (!inDesign && !approvedConnector))
                    Add(errors, CoreResourceRegionErrorCode.InvalidNodeCoordinate,
                        path, "Node must be inside the design canvas or match approved Entry/Return evidence.");

                var placed = new SpecialRegionTileCoordinate(request.Bridge.Origin, node.Coordinate);
                if (fixedCells.Contains(placed))
                    Add(errors, CoreResourceRegionErrorCode.InvalidNodeCoordinate,
                        path, "Solution nodes may not overlap FixedCollision.");

                if (node.Role == CoreResourceNodeRole.RequiredReward &&
                    (definition.RequiredReward == null || node.RewardSlotId != definition.RequiredReward.SlotId))
                    Add(errors, CoreResourceRegionErrorCode.RewardSlotMismatch,
                        path, "Required Reward node must explicitly reference the authored Reward slot.");
                if (node.Role != CoreResourceNodeRole.RequiredReward && node.RewardSlotId.Value.Length != 0)
                    Add(errors, CoreResourceRegionErrorCode.RewardSlotMismatch,
                        path, "Only the required Reward node may reference the Reward slot.");
            }

            if (definition.Nodes.Count(value => value.Role == CoreResourceNodeRole.Entry) != 1 ||
                definition.Nodes.Count(value => value.Role == CoreResourceNodeRole.Return) != 1)
                Add(errors, CoreResourceRegionErrorCode.InvalidRoute,
                    "nodes/connectors", "Exactly one Entry and one Return node are required.");
        }

        private static void ValidateEdges(
            CoreResourceRegionDefinition definition,
            ICollection<CoreResourceRegionError> errors)
        {
            var nodes = new HashSet<string>(definition.Nodes.Select(value => value.NodeId), StringComparer.Ordinal);
            foreach (var group in definition.Edges.GroupBy(value => value.EdgeId, StringComparer.Ordinal)
                         .Where(value => value.Count() != 1))
                Add(errors, CoreResourceRegionErrorCode.DuplicateEdge,
                    "edges/" + group.Key, "Edge IDs must be unique.");

            foreach (var edge in definition.Edges)
            {
                var path = "edges/" + edge.EdgeId;
                if (!IsStableId(edge.EdgeId, "CR_EDGE_") || edge.Order < 1 ||
                    !Enum.IsDefined(typeof(CoreResourceRouteKind), edge.RouteKind) ||
                    !Enum.IsDefined(typeof(AccessClass), edge.AccessClass) ||
                    !Enum.IsDefined(typeof(CoreResourceMechanismKind), edge.Mechanism) ||
                    !Enum.IsDefined(typeof(CoreResourceDependencyKind), edge.Dependency) ||
                    edge.Mechanism != definition.Mechanism ||
                    !nodes.Contains(edge.FromNodeId) || !nodes.Contains(edge.ToNodeId) ||
                    string.Equals(edge.FromNodeId, edge.ToNodeId, StringComparison.Ordinal))
                    Add(errors, CoreResourceRegionErrorCode.InvalidRoute,
                        path, "Edge endpoints, order, route kind, access, and mechanism must be explicit and valid.");

                if (edge.Dependency != CoreResourceDependencyKind.None)
                    Add(errors, CoreResourceRegionErrorCode.MandatoryToolDependency,
                        path, "CoreResource solution edges may not depend on tools, Village, or Inventory.");
                if (edge.RouteKind == CoreResourceRouteKind.Low &&
                    (!edge.Required || edge.AccessClass != AccessClass.MandatoryNoTool))
                    Add(errors, CoreResourceRegionErrorCode.MandatoryToolDependency,
                        path, "Every Low route edge must be required MandatoryNoTool.");
                if (edge.RouteKind == CoreResourceRouteKind.High && edge.Required)
                    Add(errors, CoreResourceRegionErrorCode.InvalidRoute,
                        path, "High route and failure branch edges must remain optional.");
                if (edge.RouteKind == CoreResourceRouteKind.Recovery &&
                    (!edge.Required || edge.AccessClass != AccessClass.MandatoryNoTool))
                    Add(errors, CoreResourceRegionErrorCode.UnrecoverableFailure,
                        path, "Recovery edges must be required MandatoryNoTool.");
            }
        }

        private static void ValidateRoutes(
            CoreResourceRegionDefinition definition,
            ICollection<CoreResourceRegionError> errors)
        {
            ValidateRoutePresence(definition, CoreResourceRouteKind.Low,
                CoreResourceRegionErrorCode.MissingLowRoute, errors);
            ValidateRoutePresence(definition, CoreResourceRouteKind.High,
                CoreResourceRegionErrorCode.MissingHighRoute, errors);
            ValidateRoutePresence(definition, CoreResourceRouteKind.Recovery,
                CoreResourceRegionErrorCode.MissingRecoveryRoute, errors);

            foreach (var group in definition.Routes.GroupBy(value => value.RouteId, StringComparer.Ordinal)
                         .Where(value => value.Count() != 1))
                Add(errors, CoreResourceRegionErrorCode.InvalidRoute,
                    "routes/" + group.Key, "Route IDs must be unique.");
            var edges = definition.Edges.GroupBy(value => value.EdgeId, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.First(), StringComparer.Ordinal);
            foreach (var route in definition.Routes)
            {
                var path = "routes/" + route.RouteId;
                if (!IsStableId(route.RouteId, "CR_ROUTE_") ||
                    !Enum.IsDefined(typeof(CoreResourceRouteKind), route.Kind) ||
                    route.EdgeIds.Count == 0 || route.EdgeIds.Distinct(StringComparer.Ordinal).Count() != route.EdgeIds.Count)
                {
                    Add(errors, CoreResourceRegionErrorCode.InvalidRoute,
                        path, "Route ID, kind, and unique explicit edge membership are required.");
                    continue;
                }
                var routeEdges = new List<CoreResourceSolutionEdge>();
                foreach (var edgeId in route.EdgeIds)
                {
                    if (!edges.TryGetValue(edgeId, out var edge) || edge.RouteKind != route.Kind)
                        Add(errors, CoreResourceRegionErrorCode.InvalidRoute,
                            path + "/" + edgeId, "Route edges must exist and preserve route kind.");
                    else routeEdges.Add(edge);
                }
                if (routeEdges.Count != route.EdgeIds.Count) continue;
                routeEdges.Sort((left, right) => left.Order.CompareTo(right.Order));
                if (routeEdges.Select(value => value.Order).Distinct().Count() != routeEdges.Count ||
                    !routeEdges.Select(value => value.Order).SequenceEqual(
                        Enumerable.Range(1, routeEdges.Count)))
                    Add(errors, CoreResourceRegionErrorCode.InvalidRoute,
                        path, "Route orders must be unique and contiguous from one.");
                for (var index = 1; index < routeEdges.Count; index++)
                    if (!string.Equals(routeEdges[index - 1].ToNodeId,
                            routeEdges[index].FromNodeId, StringComparison.Ordinal))
                        Add(errors, CoreResourceRegionErrorCode.InvalidRoute,
                            path, "Ordered route edges must form one explicit chain.");

                if (route.Kind == CoreResourceRouteKind.Low || route.Kind == CoreResourceRouteKind.High)
                    ValidateMainRoute(definition, route, routeEdges, errors);
                else ValidateRecoveryRoute(definition, route, routeEdges, errors);
            }

            var declared = new HashSet<string>(definition.Routes.SelectMany(value => value.EdgeIds),
                StringComparer.Ordinal);
            var failureBranches = new HashSet<string>(definition.Recoveries.Select(value => value.FailureEdgeId),
                StringComparer.Ordinal);
            foreach (var edge in definition.Edges.Where(value => !declared.Contains(value.EdgeId) &&
                                                                  !failureBranches.Contains(value.EdgeId)))
                Add(errors, CoreResourceRegionErrorCode.InvalidRoute,
                    "edges/" + edge.EdgeId, "Every edge must belong to a route or explicit failure branch.");
        }

        private static void ValidateMainRoute(
            CoreResourceRegionDefinition definition,
            CoreResourceRouteDefinition route,
            IReadOnlyList<CoreResourceSolutionEdge> edges,
            ICollection<CoreResourceRegionError> errors)
        {
            if (edges.Count == 0) return;
            var nodes = definition.Nodes.GroupBy(value => value.NodeId, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.First(), StringComparer.Ordinal);
            var path = "routes/" + route.RouteId;
            if (!nodes.TryGetValue(edges[0].FromNodeId, out var start) ||
                start.Role != CoreResourceNodeRole.Entry ||
                !nodes.TryGetValue(edges[edges.Count - 1].ToNodeId, out var end) ||
                end.Role != CoreResourceNodeRole.Return)
                Add(errors, CoreResourceRegionErrorCode.InvalidRoute,
                    path, "Low and High routes must start at Entry and end at Return.");
            var visited = new HashSet<string>(StringComparer.Ordinal) { edges[0].FromNodeId };
            foreach (var edge in edges) visited.Add(edge.ToNodeId);
            if (definition.RequiredReward == null || !visited.Contains(definition.RequiredReward.NodeId))
                Add(errors, CoreResourceRegionErrorCode.MissingRequiredReward,
                    path, "Low and High routes must pass the same required Reward node.");
            if (route.Kind == CoreResourceRouteKind.Low &&
                !visited.Any(value => nodes.TryGetValue(value, out var node) &&
                                      node.Role == CoreResourceNodeRole.EnvironmentTrigger))
                Add(errors, CoreResourceRegionErrorCode.MissingEnvironmentSolution,
                    path, "Low route needs an explicit environment trigger.");
            if (route.Kind == CoreResourceRouteKind.High &&
                (!visited.Any(value => nodes.TryGetValue(value, out var node) &&
                                       node.Role == CoreResourceNodeRole.MasteryTrigger) ||
                 !visited.Any(value => nodes.TryGetValue(value, out var node) &&
                                       node.Role == CoreResourceNodeRole.OptionalBenefit)))
                Add(errors, CoreResourceRegionErrorCode.MissingEnvironmentSolution,
                    path, "High route needs explicit mastery and optional benefit nodes.");
        }

        private static void ValidateRecoveryRoute(
            CoreResourceRegionDefinition definition,
            CoreResourceRouteDefinition route,
            IReadOnlyList<CoreResourceSolutionEdge> edges,
            ICollection<CoreResourceRegionError> errors)
        {
            if (edges.Count == 0) return;
            var nodes = definition.Nodes.GroupBy(value => value.NodeId, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.First(), StringComparer.Ordinal);
            if (!nodes.TryGetValue(edges[0].FromNodeId, out var start) ||
                start.Role != CoreResourceNodeRole.Failure ||
                !nodes.TryGetValue(edges[edges.Count - 1].ToNodeId, out var end) ||
                end.Role != CoreResourceNodeRole.RecoveryJoin)
                Add(errors, CoreResourceRegionErrorCode.UnrecoverableFailure,
                    "routes/" + route.RouteId,
                    "Recovery route must start at Failure and end at an existing RecoveryJoin.");
        }

        private static void ValidateEnvironmentSolution(
            CoreResourceRegionDefinition definition,
            ICollection<CoreResourceRegionError> errors)
        {
            var markers = definition.Nodes.GroupBy(value => value.MarkerKind)
                .ToDictionary(value => value.Key, value => value.OrderBy(item => item.AuthoredOrder).ToArray());
            var benefits = new HashSet<CoreResourceOptionalBenefitKind>(
                definition.OptionalBenefits.Select(value => value.Kind));
            var valid = false;
            switch (definition.Resource)
            {
                case CoreResourceKind.MoonCore:
                    valid = Count(markers, CoreResourceMarkerKind.MoonBoulder) >= 1 &&
                            Count(markers, CoreResourceMarkerKind.Mortar) >= 1 &&
                            Count(markers, CoreResourceMarkerKind.ChainedImpact) >= 1 &&
                            Count(markers, CoreResourceMarkerKind.Vein) >= 1 &&
                            Count(markers, CoreResourceMarkerKind.EnemyCue) >= 1 &&
                            Count(markers, CoreResourceMarkerKind.SecretPocket) >= 1 &&
                            Count(markers, CoreResourceMarkerKind.DeviceReset) >= 1 &&
                            benefits.SetEquals(new[]
                            {
                                CoreResourceOptionalBenefitKind.MoonIron,
                                CoreResourceOptionalBenefitKind.AuxiliaryBattery,
                            });
                    break;
                case CoreResourceKind.CassiaSap:
                    valid = HasExactOrders(markers, CoreResourceMarkerKind.RootChannel, 1, 2, 3) &&
                            Count(markers, CoreResourceMarkerKind.SapPipe) >= 1 &&
                            Count(markers, CoreResourceMarkerKind.MasteryWaterFlow) >= 1 &&
                            Count(markers, CoreResourceMarkerKind.BonusRoot) >= 1 &&
                            Count(markers, CoreResourceMarkerKind.Shortcut) >= 1 &&
                            Count(markers, CoreResourceMarkerKind.ManualReset) >= 1 &&
                            benefits.SetEquals(new[]
                            {
                                CoreResourceOptionalBenefitKind.RecoveryPickup,
                                CoreResourceOptionalBenefitKind.HiddenSeed,
                            });
                    break;
                case CoreResourceKind.StarNuruk:
                    valid = Count(markers, CoreResourceMarkerKind.Valve) >= 2 &&
                            Count(markers, CoreResourceMarkerKind.SafePlatform) >= 1 &&
                            Count(markers, CoreResourceMarkerKind.GasWarning) >= 1 &&
                            Count(markers, CoreResourceMarkerKind.PressureRelease) >= 1 &&
                            Count(markers, CoreResourceMarkerKind.BounceChain) >= 1 &&
                            Count(markers, CoreResourceMarkerKind.RecoveryRoom) >= 1 &&
                            definition.Nodes.Any(value => value.MarkerKind == CoreResourceMarkerKind.GasWarning &&
                                                          value.RequiredMarker) &&
                            definition.Nodes.Any(value => value.MarkerKind == CoreResourceMarkerKind.RecoveryRoom &&
                                                          value.RequiredMarker) &&
                            benefits.SetEquals(new[]
                            {
                                CoreResourceOptionalBenefitKind.Fuel,
                                CoreResourceOptionalBenefitKind.RareFermentationItem,
                            });
                    break;
            }
            if (!valid || definition.OptionalBenefits.Count != 2)
                Add(errors, CoreResourceRegionErrorCode.MissingEnvironmentSolution,
                    "environmentSolution", "Starter-specific low, mastery, recovery, and benefit markers are incomplete.");

            var optionalNodeIds = new HashSet<string>(definition.Nodes.Where(value =>
                value.Role == CoreResourceNodeRole.OptionalBenefit).Select(value => value.NodeId),
                StringComparer.Ordinal);
            if (definition.OptionalBenefits.Any(value => !optionalNodeIds.Contains(value.NodeId) ||
                                                         value.OwnsPersistence || value.Required))
                Add(errors, CoreResourceRegionErrorCode.MissingEnvironmentSolution,
                    "optionalBenefits", "Optional benefits must reference optional marker nodes and own no persistence.");
        }

        private static void ValidateRecovery(
            CoreResourceRegionDefinition definition,
            ICollection<CoreResourceRegionError> errors)
        {
            var nodes = definition.Nodes.GroupBy(value => value.NodeId, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.First(), StringComparer.Ordinal);
            var edges = definition.Edges.GroupBy(value => value.EdgeId, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.First(), StringComparer.Ordinal);
            var routes = definition.Routes.GroupBy(value => value.RouteId, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.First(), StringComparer.Ordinal);
            var failures = definition.Nodes.Where(value => value.Role == CoreResourceNodeRole.Failure).ToArray();
            foreach (var failure in failures)
            {
                var mappings = definition.Recoveries.Where(value =>
                    string.Equals(value.FailureNodeId, failure.NodeId, StringComparison.Ordinal)).ToArray();
                if (mappings.Length != 1)
                {
                    Add(errors, CoreResourceRegionErrorCode.UnrecoverableFailure,
                        "nodes/" + failure.NodeId, "Every Failure node needs exactly one recovery definition.");
                    continue;
                }
                var recovery = mappings[0];
                var valid = nodes.TryGetValue(recovery.SourceMasteryNodeId, out var source) &&
                            source.Role == CoreResourceNodeRole.MasteryTrigger &&
                            nodes.TryGetValue(recovery.RecoveryJoinNodeId, out var join) &&
                            join.Role == CoreResourceNodeRole.RecoveryJoin &&
                            edges.TryGetValue(recovery.FailureEdgeId, out var failureEdge) &&
                            failureEdge.RouteKind == CoreResourceRouteKind.High && !failureEdge.Required &&
                            string.Equals(failureEdge.FromNodeId, recovery.SourceMasteryNodeId, StringComparison.Ordinal) &&
                            string.Equals(failureEdge.ToNodeId, recovery.FailureNodeId, StringComparison.Ordinal) &&
                            routes.TryGetValue(recovery.RecoveryRouteId, out var route) &&
                            route.Kind == CoreResourceRouteKind.Recovery;
                if (!valid)
                    Add(errors, CoreResourceRegionErrorCode.UnrecoverableFailure,
                        "recoveries/" + recovery.RecoveryId,
                        "Failure branch, Recovery route, and existing Low RecoveryJoin must bind explicitly.");
                else
                {
                    var lowRoute = definition.Routes.FirstOrDefault(value =>
                        value.Kind == CoreResourceRouteKind.Low);
                    if (lowRoute != null &&
                        !RouteNodeIds(definition, lowRoute).Contains(recovery.RecoveryJoinNodeId))
                        Add(errors, CoreResourceRegionErrorCode.UnrecoverableFailure,
                            "recoveries/" + recovery.RecoveryId,
                            "RecoveryJoin must already exist on the Low route.");
                }
            }
            if (failures.Length == 0 || definition.Recoveries.Count != failures.Length)
                Add(errors, CoreResourceRegionErrorCode.MissingRecoveryRoute,
                    "recoveries", "At least one exactly mapped Failure recovery is required.");
        }

        private static void ValidateReward(
            CoreResourceRegionCompileRequest request,
            ICollection<CoreResourceRegionError> errors)
        {
            var definition = request.Definition;
            var reward = definition.RequiredReward;
            if (reward == null)
            {
                Add(errors, CoreResourceRegionErrorCode.MissingRequiredReward,
                    "requiredReward", "Exactly one required Reward definition is required.");
                return;
            }

            var expectedSlot = ExpectedRewardSlot(definition.Resource);
            var expectedKey = SpecialPersistenceKey.ForSlot(
                definition.RegionId, SpecialPersistenceScope.Reward, expectedSlot);
            if (!IsStableId(reward.RewardId, "CR_REWARD_") || reward.Resource != definition.Resource ||
                reward.SlotId != expectedSlot || reward.Amount != 1 || !reward.Required)
                Add(errors, CoreResourceRegionErrorCode.RewardSlotMismatch,
                    "requiredReward", "Reward identity, resource, slot, amount one, and required flag must be exact.");
            if (reward.PersistenceScope != SpecialPersistenceScope.Reward ||
                reward.PersistenceKey != expectedKey)
                Add(errors, CoreResourceRegionErrorCode.PersistenceMismatch,
                    "requiredReward/persistence", "Reward key must equal public ForSlot authority exactly.");

            var rewardNodes = definition.Nodes.Where(value =>
                value.Role == CoreResourceNodeRole.RequiredReward).ToArray();
            if (rewardNodes.Length != 1 ||
                !string.Equals(rewardNodes[0].NodeId, reward.NodeId, StringComparison.Ordinal) ||
                rewardNodes[0].RewardSlotId != reward.SlotId)
                Add(errors, CoreResourceRegionErrorCode.MissingRequiredReward,
                    "requiredReward/node", "Exactly one required Reward node must bind the Reward definition.");

            var layerRewards = request.FixedSlotLayerPlan.ReplaceableSlots.Where(value =>
                value.Kind == SpecialRegionSlotKind.Reward && value.Required).ToArray();
            if (layerRewards.Length != 1 || layerRewards[0].SlotId != reward.SlotId ||
                layerRewards[0].PersistenceScope != reward.PersistenceScope ||
                layerRewards[0].PersistenceKey != reward.PersistenceKey)
                Add(errors, CoreResourceRegionErrorCode.RewardSlotMismatch,
                    "fixedSlotLayerPlan/requiredReward",
                    "MAP13_03 layer must contain the exact required Reward slot/key/scope.");
            else if (rewardNodes.Length == 1 && rewardNodes[0].Coordinate != layerRewards[0].Source.LocalTile)
                Add(errors, CoreResourceRegionErrorCode.RewardSlotMismatch,
                    "requiredReward/coordinate", "Reward node coordinate must match the authored MAP13_03 slot.");

            var proof = request.SafetyProof;
            if (proof.SlotId != reward.SlotId || proof.PersistenceKey != reward.PersistenceKey ||
                proof.PersistenceScope != SpecialPersistenceScope.Reward || !proof.IsSafe ||
                layerRewards.Length != 1 ||
                (layerRewards.Length == 1 && !string.Equals(
                    proof.SourceDigest, layerRewards[0].IdentityDigest, StringComparison.Ordinal)))
                Add(errors, CoreResourceRegionErrorCode.PersistenceMismatch,
                    "safetyProof", "Safety proof must preserve the exact layer Reward identity and seven checkpoints.");
            if (proof.PermanentlyUnavailableCount != 0 || !proof.RecoveryBranchesAvailable)
                Add(errors, CoreResourceRegionErrorCode.RequiredResourcePermanentlyLost,
                    "safetyProof", "Interrupt, failure, and regeneration must restore availability.");
            if (proof.DuplicateRewardRiskCount != 0 || !proof.ClaimStable)
                Add(errors, CoreResourceRegionErrorCode.DuplicateRewardRisk,
                    "safetyProof", "Claim and revisit must remain claimed without duplicate risk.");
        }

        private static void ValidateSourcePublication(
            CoreResourceRegionCompileRequest request,
            ICollection<CoreResourceRegionError> errors)
        {
            if (!string.Equals(request.Bridge.ContractDigest,
                    request.FixedSlotLayerPlan.ContractDigest, StringComparison.Ordinal) ||
                !string.Equals(request.Bridge.CanonicalDigest,
                    request.EntryBufferPlan.BridgeDigest, StringComparison.Ordinal) ||
                !string.Equals(request.Bridge.CanonicalDigest,
                    request.FixedSlotLayerPlan.BridgeDigest, StringComparison.Ordinal) ||
                !string.Equals(request.EntryBufferPlan.CanonicalDigest,
                    request.FixedSlotLayerPlan.EntryBufferDigest, StringComparison.Ordinal) ||
                !string.Equals(request.CollisionPlan.CanonicalDigest,
                    request.FixedSlotLayerPlan.CollisionDigest, StringComparison.Ordinal))
                Add(errors, CoreResourceRegionErrorCode.NonCanonicalPublication,
                    "sources", "MAP13_01-03 source digests must form one unchanged publication chain.");

            if (request.EntryBufferPlan.EntryPort.AccessClass != AccessClass.MandatoryNoTool ||
                request.EntryBufferPlan.ReturnPort.AccessClass != AccessClass.MandatoryNoTool ||
                !request.EntryBufferPlan.Witness.IsBidirectional ||
                request.EntryBufferPlan.Witness.ToolRequirementCount != 0 ||
                request.EntryBufferPlan.Witness.SyntheticEdgeCount != 0 ||
                request.EntryBufferPlan.Witness.TeleportCount != 0 ||
                request.EntryBufferPlan.Witness.CarveCount != 0)
                Add(errors, CoreResourceRegionErrorCode.MandatoryToolDependency,
                    "entryBufferPlan", "Entry and Return evidence must be bidirectional MandatoryNoTool with zero synthesis.");

            if (request.CollisionPlan.RemovedPayloadCount != 0 ||
                request.CollisionPlan.GlobalLayerReorderCount != 0 ||
                request.FixedSlotLayerPlan.TileMutationCount != 0 ||
                request.SafetyProof.RewardGrantCount != 0 ||
                request.SafetyProof.SaveWriteCount != 0)
                Add(errors, CoreResourceRegionErrorCode.NonCanonicalPublication,
                    "mutationCounters", "Compilation inputs must preserve zero mutation authority.");
        }

        private static CoreResourceRouteWitness BuildWitness(
            CoreResourceRegionDefinition definition,
            CoreResourceRouteDefinition route)
        {
            var byId = definition.Edges.GroupBy(value => value.EdgeId, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.First(), StringComparer.Ordinal);
            var edges = route.EdgeIds.Select(value => byId[value]).OrderBy(value => value.Order).ToArray();
            var nodes = new List<string> { edges[0].FromNodeId };
            nodes.AddRange(edges.Select(value => value.ToNodeId));
            return new CoreResourceRouteWitness(route.RouteId, route.Kind, nodes);
        }

        private static HashSet<string> RouteNodeIds(
            CoreResourceRegionDefinition definition,
            CoreResourceRouteDefinition route)
        {
            var byId = definition.Edges.GroupBy(value => value.EdgeId, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.First(), StringComparer.Ordinal);
            var values = new HashSet<string>(StringComparer.Ordinal);
            foreach (var edgeId in route.EdgeIds)
                if (byId.TryGetValue(edgeId, out var edge))
                {
                    values.Add(edge.FromNodeId);
                    values.Add(edge.ToNodeId);
                }
            return values;
        }

        private static void ValidateRoutePresence(
            CoreResourceRegionDefinition definition,
            CoreResourceRouteKind kind,
            CoreResourceRegionErrorCode code,
            ICollection<CoreResourceRegionError> errors)
        {
            if (!definition.Routes.Any(value => value.Kind == kind))
                Add(errors, code, "routes/" + kind, kind + " route is required.");
        }

        private static int Count(
            IReadOnlyDictionary<CoreResourceMarkerKind, CoreResourceSolutionNode[]> values,
            CoreResourceMarkerKind kind)
            => values.TryGetValue(kind, out var nodes) ? nodes.Length : 0;

        private static bool HasExactOrders(
            IReadOnlyDictionary<CoreResourceMarkerKind, CoreResourceSolutionNode[]> values,
            CoreResourceMarkerKind kind,
            params int[] orders)
            => values.TryGetValue(kind, out var nodes) &&
               nodes.Select(value => value.AuthoredOrder).SequenceEqual(orders);

        private static bool TryExpectedIdentity(
            CoreResourceKind resource,
            out SpecialRegionId regionId,
            out MoonpalaceBiomeId biome,
            out CoreResourceMechanismKind mechanism)
        {
            switch (resource)
            {
                case CoreResourceKind.MoonCore:
                    regionId = new SpecialRegionId("SR_MOON_CORE_SITE_5");
                    biome = MoonpalaceBiomeId.MoonCrater;
                    mechanism = CoreResourceMechanismKind.ImpactChain;
                    return true;
                case CoreResourceKind.CassiaSap:
                    regionId = new SpecialRegionId("SR_CASSIA_SAP_SITE_5");
                    biome = MoonpalaceBiomeId.CassiaRoot;
                    mechanism = CoreResourceMechanismKind.WaterChannel;
                    return true;
                case CoreResourceKind.StarNuruk:
                    regionId = new SpecialRegionId("SR_STAR_NURUK_SITE_5");
                    biome = MoonpalaceBiomeId.MoonDough;
                    mechanism = CoreResourceMechanismKind.FermentationPressure;
                    return true;
                default:
                    regionId = default(SpecialRegionId);
                    biome = default(MoonpalaceBiomeId);
                    mechanism = default(CoreResourceMechanismKind);
                    return false;
            }
        }

        private static SpecialRegionSlotId ExpectedRewardSlot(CoreResourceKind resource)
        {
            switch (resource)
            {
                case CoreResourceKind.MoonCore:
                    return new SpecialRegionSlotId("SR_SLOT_MOON_CORE_REWARD");
                case CoreResourceKind.CassiaSap:
                    return new SpecialRegionSlotId("SR_SLOT_CASSIA_SAP_REWARD");
                case CoreResourceKind.StarNuruk:
                    return new SpecialRegionSlotId("SR_SLOT_STAR_NURUK_REWARD");
                default:
                    return default(SpecialRegionSlotId);
            }
        }

        private static bool IsStableId(string value, string prefix)
        {
            if (string.IsNullOrEmpty(value) || !value.StartsWith(prefix, StringComparison.Ordinal)) return false;
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (!(character == '_' || character >= 'A' && character <= 'Z' ||
                      character >= '0' && character <= '9')) return false;
            }
            return true;
        }

        private static void ValidateDigest(
            string recomputed,
            string published,
            string expected,
            string path,
            ICollection<CoreResourceRegionError> errors)
        {
            if (!EqualsDigest(recomputed, published) || !EqualsDigest(recomputed, expected))
                Add(errors, CoreResourceRegionErrorCode.DigestMismatch,
                    path, "Expected, published, and recomputed SHA-256 digests must match.");
        }

        private static bool EqualsDigest(string left, string right)
            => !string.IsNullOrEmpty(left) && string.Equals(left, right, StringComparison.Ordinal);

        private static void Require(
            object value,
            string path,
            ICollection<CoreResourceRegionError> errors)
        {
            if (value == null)
                Add(errors, CoreResourceRegionErrorCode.MissingInput, path, "Required input is missing.");
        }

        private static CoreResourceRegionResult Failure(
            CoreResourceRegionErrorCode code,
            string path,
            string detail)
            => new CoreResourceRegionResult(null, new[] { new CoreResourceRegionError(code, path, detail) });

        private static void Add(
            ICollection<CoreResourceRegionError> errors,
            CoreResourceRegionErrorCode code,
            string path,
            string detail)
            => errors.Add(new CoreResourceRegionError(code, path, detail));
    }
}
