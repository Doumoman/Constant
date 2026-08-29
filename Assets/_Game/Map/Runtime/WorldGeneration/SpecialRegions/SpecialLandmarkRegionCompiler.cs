using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Map.WorldGeneration.Pipeline;

namespace StarNight.Map.WorldGeneration.SpecialRegions
{
    public static class SpecialLandmarkRegionCompiler
    {
        public static SpecialLandmarkResult Compile(SpecialLandmarkCompileRequest request)
        {
            var errors = new List<SpecialLandmarkError>();
            if (request == null)
            {
                Add(errors, SpecialLandmarkErrorCode.MissingInput, "request", "Compile request is required.");
                return Failure(errors);
            }

            var definition = request.Definition;
            if (definition == null)
            {
                Add(errors, SpecialLandmarkErrorCode.MissingInput, "definition", "Landmark definition is required.");
                return Failure(errors);
            }

            ValidateIdentity(definition, errors);
            ValidateDesign(definition, errors);
            ValidateGraph(definition, errors);
            ValidateState(definition, errors);
            ValidateMarkers(definition, errors);
            ValidateBinding(request, errors);
            ValidateLandmark(request, errors);
            Require(
                errors,
                string.Equals(definition.CanonicalDigest,
                    SpecialLandmarkCanonicalDigest.ComputeDefinition(definition), StringComparison.Ordinal),
                SpecialLandmarkErrorCode.NonCanonicalPublication,
                definition.RegionId.Value,
                "Definition digest must match canonical semantic publication.");

            if (errors.Count != 0) return Failure(errors);

            var witnesses = definition.Routes.Select(route => BuildWitness(definition, route)).ToArray();
            var placed = definition.Binding == SpecialLandmarkBindingKind.PlacedMandatorySite;
            var coreDigest = request.CoreResourceDefinitions.Count == 0
                ? string.Empty
                : CoreResourceRegionStarterCatalog.CanonicalDigest;
            var plan = new SpecialLandmarkRegionPlan(
                definition,
                placed ? SpecialLandmarkPlacementStatus.Placed : SpecialLandmarkPlacementStatus.DeferredToMAP14,
                witnesses,
                request.Bridge == null ? string.Empty : request.Bridge.CanonicalDigest,
                request.EntryBufferPlan == null ? string.Empty : request.EntryBufferPlan.CanonicalDigest,
                request.CollisionPlan == null ? string.Empty : request.CollisionPlan.CanonicalDigest,
                request.FixedSlotLayerPlan == null ? string.Empty : request.FixedSlotLayerPlan.CanonicalDigest,
                request.RewardSafetyProof == null ? string.Empty : request.RewardSafetyProof.CanonicalDigest,
                coreDigest);
            return new SpecialLandmarkResult(plan, Array.Empty<SpecialLandmarkError>());
        }

        private static void ValidateIdentity(
            SpecialLandmarkRegionDefinition definition,
            ICollection<SpecialLandmarkError> errors)
        {
            if (!TryExpectedIdentity(
                    definition.Landmark,
                    out var regionId,
                    out var regionKind,
                    out var theme,
                    out var binding,
                    out var reservedWidth,
                    out var reservedHeight,
                    out var designX,
                    out var designY,
                    out var designWidth,
                    out var designHeight,
                    out var activeChunks))
            {
                Add(errors, SpecialLandmarkErrorCode.KindMismatch,
                    definition.Landmark.ToString(), "Unknown landmark kind.");
                return;
            }

            Require(errors, definition.RegionId == new SpecialRegionId(regionId),
                SpecialLandmarkErrorCode.RegionIdentityMismatch, definition.RegionId.Value,
                "Region ID does not match the exact starter identity.");
            Require(errors, definition.RegionKind == regionKind,
                SpecialLandmarkErrorCode.KindMismatch, definition.RegionId.Value,
                "SpecialRegionKind does not match landmark kind.");
            Require(errors, definition.Theme == theme,
                SpecialLandmarkErrorCode.RegionIdentityMismatch, definition.RegionId.Value,
                "Theme does not match the exact starter matrix.");
            Require(errors, definition.Binding == binding,
                SpecialLandmarkErrorCode.InvalidBindingMode, definition.RegionId.Value,
                "Binding mode does not match the exact starter matrix.");
            Require(errors, definition.ReservedWidth == reservedWidth &&
                            definition.ReservedHeight == reservedHeight,
                SpecialLandmarkErrorCode.UnsupportedFootprint, definition.RegionId.Value,
                "Placed footprint must be exact and optional footprints must be absent.");
            Require(errors, definition.DesignOrigin.X == designX && definition.DesignOrigin.Y == designY &&
                            definition.DesignWidth == designWidth && definition.DesignHeight == designHeight,
                SpecialLandmarkErrorCode.InvalidDesignCanvas, definition.RegionId.Value,
                "Design origin or size does not match the starter matrix.");
            Require(errors, definition.ActiveDesignChunks.Count == activeChunks,
                SpecialLandmarkErrorCode.InvalidActiveChunk, definition.RegionId.Value,
                "Active design chunk count does not match the starter matrix.");
        }

        private static void ValidateDesign(
            SpecialLandmarkRegionDefinition definition,
            ICollection<SpecialLandmarkError> errors)
        {
            Require(errors, definition.DesignChunkWidth == 12 && definition.DesignChunkHeight == 8 &&
                            definition.DesignWidth > 0 && definition.DesignHeight > 0 &&
                            definition.DesignWidth % 12 == 0 && definition.DesignHeight % 8 == 0,
                SpecialLandmarkErrorCode.InvalidDesignCanvas, definition.RegionId.Value,
                "Design canvas must use explicit 12x8 logical chunks.");

            var width = definition.DesignWidth / 12;
            var height = definition.DesignHeight / 8;
            var unique = new HashSet<SpecialLandmarkDesignChunk>();
            foreach (var chunk in definition.ActiveDesignChunks)
            {
                Require(errors, unique.Add(chunk), SpecialLandmarkErrorCode.InvalidActiveChunk,
                    chunk.ToString(), "Active design chunk must be unique.");
                Require(errors, chunk.X >= 0 && chunk.Y >= 0 && chunk.X < width && chunk.Y < height,
                    SpecialLandmarkErrorCode.InvalidActiveChunk, chunk.ToString(),
                    "Active design chunk must be inside the logical grid.");
            }
            Require(errors, IsConnected(unique), SpecialLandmarkErrorCode.InvalidActiveChunk,
                definition.RegionId.Value, "Active design chunks must be 4-neighbor connected.");

            foreach (var node in definition.Nodes)
            {
                if (node == null) continue;
                var localX = node.Coordinate.X - definition.DesignOrigin.X;
                var localY = node.Coordinate.Y - definition.DesignOrigin.Y;
                Require(errors, localX >= 0 && localY >= 0 && localX < definition.DesignWidth &&
                                localY < definition.DesignHeight,
                    SpecialLandmarkErrorCode.InvalidDesignCanvas, node.NodeId,
                    "Every authored node coordinate must be inside the design canvas.");
            }
        }

        private static void ValidateGraph(
            SpecialLandmarkRegionDefinition definition,
            ICollection<SpecialLandmarkError> errors)
        {
            var nodes = new Dictionary<string, SpecialLandmarkShellNode>(StringComparer.Ordinal);
            foreach (var node in definition.Nodes)
            {
                if (node == null || !IsStableId(node.NodeId, "SL_NODE_"))
                {
                    Add(errors, SpecialLandmarkErrorCode.DuplicateNode,
                        node == null ? "null" : node.NodeId, "Node requires a stable SL_NODE_ ID.");
                    continue;
                }
                if (!nodes.TryAdd(node.NodeId, node))
                    Add(errors, SpecialLandmarkErrorCode.DuplicateNode, node.NodeId, "Node ID must be unique.");
            }

            var edges = new Dictionary<string, SpecialLandmarkShellEdge>(StringComparer.Ordinal);
            foreach (var edge in definition.Edges)
            {
                if (edge == null || !IsStableId(edge.EdgeId, "SL_EDGE_"))
                {
                    Add(errors, SpecialLandmarkErrorCode.InvalidEdge,
                        edge == null ? "null" : edge.EdgeId, "Edge requires a stable SL_EDGE_ ID.");
                    continue;
                }
                if (!edges.TryAdd(edge.EdgeId, edge))
                    Add(errors, SpecialLandmarkErrorCode.InvalidEdge, edge.EdgeId, "Edge ID must be unique.");
                Require(errors, nodes.ContainsKey(edge.FromNodeId) && nodes.ContainsKey(edge.ToNodeId) &&
                                !string.Equals(edge.FromNodeId, edge.ToNodeId, StringComparison.Ordinal),
                    SpecialLandmarkErrorCode.InvalidEdge, edge.EdgeId,
                    "Edge endpoints must be distinct authored nodes.");
                Require(errors, edge.Order > 0, SpecialLandmarkErrorCode.InvalidEdge, edge.EdgeId,
                    "Edge order must be positive and explicit.");
                Require(errors, edge.Dependency == SpecialLandmarkDependencyKind.None,
                    SpecialLandmarkErrorCode.MandatoryOptionalDependency, edge.EdgeId,
                    "Landmark routes may not depend on Village, tool, inventory, or another optional landmark.");
                var expectedAccess = ExpectedAccess(definition.Binding, edge.RouteKind);
                Require(errors, edge.AccessClass == expectedAccess,
                    SpecialLandmarkErrorCode.InvalidRoute, edge.EdgeId,
                    "Route edge access class does not match mandatory/optional binding.");
            }

            var routeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var route in definition.Routes)
            {
                if (route == null || !routeIds.Add(route.RouteId) || !IsStableId(route.RouteId, "SL_ROUTE_"))
                {
                    Add(errors, SpecialLandmarkErrorCode.InvalidRoute,
                        route == null ? "null" : route.RouteId, "Route ID must be stable and unique.");
                    continue;
                }
                ValidateRoute(route, nodes, edges, errors);
            }

            Require(errors, definition.Routes.Count(value => value.Kind == SpecialLandmarkRouteKind.Low) == 1,
                SpecialLandmarkErrorCode.InvalidRoute, definition.RegionId.Value,
                "Exactly one Low route is required.");
            Require(errors, definition.Routes.Count(value => value.Kind == SpecialLandmarkRouteKind.High) == 1,
                SpecialLandmarkErrorCode.InvalidRoute, definition.RegionId.Value,
                "Exactly one High route is required.");
            Require(errors, definition.Routes.Count(value => value.Kind == SpecialLandmarkRouteKind.Return) == 1,
                SpecialLandmarkErrorCode.MissingReturn, definition.RegionId.Value,
                "Exactly one explicit Return route is required.");

            var lowNodes = RouteNodes(definition,
                definition.Routes.FirstOrDefault(value => value.Kind == SpecialLandmarkRouteKind.Low));
            foreach (var failure in definition.Nodes.Where(value => value.Role == SpecialLandmarkNodeRole.Failure))
            {
                var reset = definition.Resets.FirstOrDefault(value =>
                    string.Equals(value.FailureNodeId, failure.NodeId, StringComparison.Ordinal));
                var recovery = definition.Routes.FirstOrDefault(route =>
                    route.Kind == SpecialLandmarkRouteKind.Recovery &&
                    string.Equals(route.StartNodeId, failure.NodeId, StringComparison.Ordinal));
                var recoveryNodes = RouteNodes(definition, recovery);
                Require(errors, reset != null && recovery != null &&
                                recoveryNodes.Contains(reset.RecoveryNodeId) &&
                                lowNodes.Contains(recovery.EndNodeId),
                    SpecialLandmarkErrorCode.UnrecoverableFailure, failure.NodeId,
                    "Every failure must pass its reset join and end at an existing Low route node.");
            }
        }

        private static void ValidateRoute(
            SpecialLandmarkRouteDefinition route,
            IReadOnlyDictionary<string, SpecialLandmarkShellNode> nodes,
            IReadOnlyDictionary<string, SpecialLandmarkShellEdge> edges,
            ICollection<SpecialLandmarkError> errors)
        {
            Require(errors, route.EdgeIds.Count > 0 && nodes.ContainsKey(route.StartNodeId) &&
                            nodes.ContainsKey(route.EndNodeId),
                SpecialLandmarkErrorCode.InvalidRoute, route.RouteId,
                "Route requires authored start/end nodes and at least one edge.");
            if (route.EdgeIds.Count == 0 || !nodes.ContainsKey(route.StartNodeId) ||
                !nodes.ContainsKey(route.EndNodeId)) return;
            var selected = new List<SpecialLandmarkShellEdge>();
            foreach (var edgeId in route.EdgeIds)
            {
                if (!edges.TryGetValue(edgeId, out var edge))
                {
                    Add(errors, SpecialLandmarkErrorCode.InvalidRoute, route.RouteId,
                        "Route references an unknown edge: " + edgeId);
                    continue;
                }
                selected.Add(edge);
                Require(errors, edge.RouteKind == route.Kind,
                    SpecialLandmarkErrorCode.InvalidRoute, route.RouteId,
                    "Route edge kind must match route kind.");
            }
            if (selected.Count != route.EdgeIds.Count || selected.Count == 0) return;
            selected = selected.OrderBy(value => value.Order).ThenBy(value => value.EdgeId, StringComparer.Ordinal).ToList();
            Require(errors, selected.Select(value => value.Order).SequenceEqual(
                    Enumerable.Range(1, selected.Count)),
                SpecialLandmarkErrorCode.InvalidRoute, route.RouteId,
                "Route edge orders must be exact contiguous 1..N.");
            var current = route.StartNodeId;
            foreach (var edge in selected)
            {
                if (!string.Equals(edge.FromNodeId, current, StringComparison.Ordinal))
                {
                    Add(errors, SpecialLandmarkErrorCode.InvalidRoute, route.RouteId,
                        "Ordered route edges must form one authored chain.");
                    return;
                }
                current = edge.ToNodeId;
            }
            Require(errors, string.Equals(current, route.EndNodeId, StringComparison.Ordinal),
                SpecialLandmarkErrorCode.InvalidRoute, route.RouteId,
                "Ordered route must end at the declared node.");
            if (route.Kind == SpecialLandmarkRouteKind.Low || route.Kind == SpecialLandmarkRouteKind.High)
            {
                Require(errors, nodes[route.StartNodeId].Role == SpecialLandmarkNodeRole.Entry &&
                                nodes[route.EndNodeId].Role == SpecialLandmarkNodeRole.Return,
                    SpecialLandmarkErrorCode.MissingReturn, route.RouteId,
                    "Low and High routes must connect explicit Entry to Return.");
            }
            if (route.Kind == SpecialLandmarkRouteKind.Return)
                Require(errors, nodes[route.EndNodeId].Role == SpecialLandmarkNodeRole.Return,
                    SpecialLandmarkErrorCode.MissingReturn, route.RouteId,
                    "Return witness must end at the exact Return node.");
        }

        private static void ValidateState(
            SpecialLandmarkRegionDefinition definition,
            ICollection<SpecialLandmarkError> errors)
        {
            var states = new Dictionary<string, SpecialLandmarkStateDefinition>(StringComparer.Ordinal);
            foreach (var state in definition.States)
            {
                if (state == null || !IsStableId(state.StateId, "SL_STATE_") || !states.TryAdd(state.StateId, state))
                    Add(errors, SpecialLandmarkErrorCode.InvalidState,
                        state == null ? "null" : state.StateId, "State ID must be stable and unique.");
            }
            var transitionIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var transition in definition.Transitions)
            {
                if (transition == null || !IsStableId(transition.TransitionId, "SL_TRANSITION_") ||
                    !transitionIds.Add(transition.TransitionId))
                {
                    Add(errors, SpecialLandmarkErrorCode.InvalidTransition,
                        transition == null ? "null" : transition.TransitionId,
                        "Transition ID must be stable and unique.");
                    continue;
                }
                Require(errors, states.ContainsKey(transition.FromStateId) && states.ContainsKey(transition.ToStateId) &&
                                transition.Order > 0,
                    SpecialLandmarkErrorCode.InvalidTransition, transition.TransitionId,
                    "Transition requires valid states and positive authored order.");
            }
            var resetIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var reset in definition.Resets)
            {
                if (reset == null || !IsStableId(reset.ResetId, "SL_RESET_") || !resetIds.Add(reset.ResetId))
                {
                    Add(errors, SpecialLandmarkErrorCode.InvalidResetPolicy,
                        reset == null ? "null" : reset.ResetId, "Reset ID must be stable and unique.");
                    continue;
                }
                Require(errors, string.IsNullOrEmpty(reset.FromStateId) || states.ContainsKey(reset.FromStateId),
                    SpecialLandmarkErrorCode.InvalidResetPolicy, reset.ResetId,
                    "Reset source state must be authored when supplied.");
                Require(errors, string.IsNullOrEmpty(reset.ToStateId) || states.ContainsKey(reset.ToStateId),
                    SpecialLandmarkErrorCode.InvalidResetPolicy, reset.ResetId,
                    "Reset target state must be authored when supplied.");
            }
            Require(errors, !definition.StateMutatesShell,
                SpecialLandmarkErrorCode.ShellMutation, definition.RegionId.Value,
                "State changes may not mutate shell, route, coordinate, collision, or slot identity.");
        }

        private static void ValidateMarkers(
            SpecialLandmarkRegionDefinition definition,
            ICollection<SpecialLandmarkError> errors)
        {
            var nodeIds = new HashSet<string>(definition.Nodes.Select(value => value.NodeId), StringComparer.Ordinal);
            var stateIds = new HashSet<string>(definition.States.Select(value => value.StateId), StringComparer.Ordinal);
            var markerIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var marker in definition.Markers)
            {
                if (marker == null || !IsStableId(marker.MarkerId, "SL_MARKER_") || !markerIds.Add(marker.MarkerId))
                {
                    Add(errors, SpecialLandmarkErrorCode.InvalidState,
                        marker == null ? "null" : marker.MarkerId, "Marker ID must be stable and unique.");
                    continue;
                }
                Require(errors, string.IsNullOrEmpty(marker.NodeId) || nodeIds.Contains(marker.NodeId),
                    SpecialLandmarkErrorCode.InvalidState, marker.MarkerId,
                    "Marker node must reference an authored shell node.");
                Require(errors, string.IsNullOrEmpty(marker.StateId) || stateIds.Contains(marker.StateId),
                    SpecialLandmarkErrorCode.InvalidState, marker.MarkerId,
                    "Marker state must reference an authored state.");
                Require(errors, marker.Dependency == SpecialLandmarkDependencyKind.None,
                    SpecialLandmarkErrorCode.MandatoryOptionalDependency, marker.MarkerId,
                    "Marker may not become a mandatory dependency.");
            }
            Require(errors, !definition.MandatoryProgressionDependency,
                SpecialLandmarkErrorCode.MandatoryOptionalDependency, definition.RegionId.Value,
                "Optional visits and optional benefits may not become mandatory progression dependencies.");
        }

        private static void ValidateBinding(
            SpecialLandmarkCompileRequest request,
            ICollection<SpecialLandmarkError> errors)
        {
            var definition = request.Definition;
            if (definition.Binding == SpecialLandmarkBindingKind.DeferredOptionalLocal)
            {
                var hasWorldSource = request.Bridge != null || request.EntryBufferPlan != null ||
                                     request.CollisionPlan != null || request.FixedSlotLayerPlan != null ||
                                     request.RewardSafetyProof != null || request.CoreResourceDefinitions.Count != 0 ||
                                     !string.IsNullOrEmpty(request.ExpectedBridgeDigest) ||
                                     !string.IsNullOrEmpty(request.ExpectedEntryBufferDigest) ||
                                     !string.IsNullOrEmpty(request.ExpectedCollisionDigest) ||
                                     !string.IsNullOrEmpty(request.ExpectedFixedSlotLayerDigest) ||
                                     !string.IsNullOrEmpty(request.ExpectedRewardSafetyDigest);
                Require(errors, !hasWorldSource,
                    SpecialLandmarkErrorCode.OptionalWorldBindingClaim, definition.RegionId.Value,
                    "Deferred optional landmarks must not claim world/reservation/bridge/layer authority.");
                Require(errors, definition.ReservedWidth == 0 && definition.ReservedHeight == 0,
                    SpecialLandmarkErrorCode.OptionalWorldBindingClaim, definition.RegionId.Value,
                    "Deferred optional landmarks must publish no placed footprint.");
                return;
            }

            Require(errors, request.Bridge != null && request.EntryBufferPlan != null &&
                            request.CollisionPlan != null && request.FixedSlotLayerPlan != null,
                SpecialLandmarkErrorCode.MissingInput, definition.RegionId.Value,
                "Placed landmarks require MAP13_01 bridge, MAP13_02 plans, and MAP13_03 layer.");
            if (request.Bridge != null)
            {
                ValidateDigest(errors, request.ExpectedBridgeDigest, request.Bridge.CanonicalDigest,
                    "bridge");
                Require(errors, request.Bridge.RegionId == definition.RegionId &&
                                request.Bridge.RegionKind == definition.RegionKind &&
                                request.Bridge.Width == 1 && request.Bridge.Height == 1,
                    SpecialLandmarkErrorCode.RegionIdentityMismatch, "bridge",
                    "Placed bridge identity, kind, and 1x1 footprint must match definition.");
            }
            if (request.EntryBufferPlan != null)
            {
                ValidateDigest(errors, request.ExpectedEntryBufferDigest,
                    request.EntryBufferPlan.CanonicalDigest, "entryBuffer");
                Require(errors, request.EntryBufferPlan.RegionId == definition.RegionId,
                    SpecialLandmarkErrorCode.RegionIdentityMismatch, "entryBuffer",
                    "Entry/buffer plan region must match definition.");
            }
            if (request.CollisionPlan != null)
                ValidateDigest(errors, request.ExpectedCollisionDigest,
                    request.CollisionPlan.CanonicalDigest, "collision");
            if (request.FixedSlotLayerPlan != null)
            {
                ValidateDigest(errors, request.ExpectedFixedSlotLayerDigest,
                    request.FixedSlotLayerPlan.CanonicalDigest, "fixedSlotLayer");
                Require(errors, request.FixedSlotLayerPlan.RegionId == definition.RegionId &&
                                request.FixedSlotLayerPlan.RegionKind == definition.RegionKind,
                    SpecialLandmarkErrorCode.RegionIdentityMismatch, "fixedSlotLayer",
                    "Fixed/slot layer identity must match definition.");
            }
            ValidateCoreResources(request, errors);
        }

        private static void ValidateCoreResources(
            SpecialLandmarkCompileRequest request,
            ICollection<SpecialLandmarkError> errors)
        {
            var expected = CoreResourceRegionStarterCatalog.Entries;
            Require(errors, request.CoreResourceDefinitions.Count == expected.Count,
                SpecialLandmarkErrorCode.MissingInput, "MAP13_06",
                "Placed Forge/Boss compilation requires exact three MAP13_06 resource definitions.");
            foreach (var item in expected)
            {
                var actual = request.CoreResourceDefinitions.FirstOrDefault(value => value.RegionId == item.RegionId);
                Require(errors, actual != null && string.Equals(
                                CoreResourceRegionCanonicalDigest.ComputeDefinition(actual),
                                CoreResourceRegionCanonicalDigest.ComputeDefinition(item),
                                StringComparison.Ordinal),
                    SpecialLandmarkErrorCode.DigestMismatch, item.RegionId.Value,
                    "MAP13_06 resource identity digest must match the canonical starter.");
                if (actual == null) continue;
                Require(errors, actual.RequiredReward != null && actual.RequiredReward.Amount == 1 &&
                                actual.Recoveries.Count != 0 &&
                                actual.Edges.Where(value => value.RouteKind == CoreResourceRouteKind.Low)
                                    .All(value => value.AccessClass == AccessClass.MandatoryNoTool &&
                                                  value.Dependency == CoreResourceDependencyKind.None),
                    SpecialLandmarkErrorCode.ResourceLossRisk, item.RegionId.Value,
                    "MAP13_06 source must retain exact reward, no-tool Low, and recovery semantics.");
            }
        }

        private static void ValidateLandmark(
            SpecialLandmarkCompileRequest request,
            ICollection<SpecialLandmarkError> errors)
        {
            switch (request.Definition.Landmark)
            {
                case SpecialLandmarkKind.MoonSealForge:
                    ValidateForge(request, errors);
                    break;
                case SpecialLandmarkKind.BossSealArena:
                    ValidateBoss(request.Definition, errors);
                    break;
                case SpecialLandmarkKind.WanderingMerchantCave:
                    ValidateMerchant(request.Definition, errors);
                    break;
                case SpecialLandmarkKind.MaruTimeShrine:
                    ValidateMaru(request.Definition, errors);
                    break;
                default:
                    Add(errors, SpecialLandmarkErrorCode.KindMismatch,
                        request.Definition.Landmark.ToString(), "Unsupported landmark kind.");
                    break;
            }
        }

        private static void ValidateForge(
            SpecialLandmarkCompileRequest request,
            ICollection<SpecialLandmarkError> errors)
        {
            var definition = request.Definition;
            var process = definition.Markers
                .Where(value => value.Kind == SpecialLandmarkMarkerKind.ForgeProcessStep)
                .OrderBy(value => value.Order).Select(value => value.NodeId).ToArray();
            var expected = new[] { "SL_NODE_FORGE_GRIND", "SL_NODE_FORGE_MIX", "SL_NODE_FORGE_PRESS", "SL_NODE_FORGE_CURE" };
            Require(errors, process.SequenceEqual(expected),
                SpecialLandmarkErrorCode.ForgeProcessOrderMismatch, definition.RegionId.Value,
                "Forge process must be authored Grind, Mix, Press, MoonlightCure.");
            foreach (var kind in new[] { SpecialLandmarkRouteKind.Low, SpecialLandmarkRouteKind.High })
            {
                var nodes = RouteNodes(definition,
                    definition.Routes.FirstOrDefault(value => value.Kind == kind));
                var indices = expected.Select(node => nodes.IndexOf(node)).ToArray();
                Require(errors, indices.All(value => value >= 0) && indices.SequenceEqual(indices.OrderBy(value => value)),
                    SpecialLandmarkErrorCode.ForgeProcessOrderMismatch, kind.ToString(),
                    "Low and High routes must preserve the four authored process steps.");
            }

            var resourceKinds = new[]
            {
                SpecialLandmarkForgeResource.MoonCore,
                SpecialLandmarkForgeResource.CassiaSap,
                SpecialLandmarkForgeResource.StarNuruk,
            };
            Require(errors, definition.ForgeLedgers.Select(value => value.Resource).SequenceEqual(resourceKinds),
                SpecialLandmarkErrorCode.ResourceLossRisk, definition.RegionId.Value,
                "Forge requires exact MoonCore, CassiaSap, and StarNuruk ledgers.");
            var states = definition.States.GroupBy(value => value.StateId, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.First(), StringComparer.Ordinal);
            foreach (var ledger in definition.ForgeLedgers)
            {
                Require(errors, HasStateRole(states, ledger.AvailableStateId, SpecialLandmarkStateRole.ResourceAvailable) &&
                                HasStateRole(states, ledger.ReservedStateId, SpecialLandmarkStateRole.ResourceReserved) &&
                                HasStateRole(states, ledger.ConsumedStateId, SpecialLandmarkStateRole.ResourceConsumed) &&
                                HasStateRole(states, ledger.ReturnedStateId, SpecialLandmarkStateRole.ResourceReturned),
                    SpecialLandmarkErrorCode.ResourceLossRisk, ledger.Resource.ToString(),
                    "Each Forge input requires Available, Reserved, Consumed, and Returned states.");
                Require(errors, HasTransition(definition, ledger.AvailableStateId, ledger.ReservedStateId) &&
                                HasTransition(definition, ledger.ReservedStateId, ledger.ConsumedStateId) &&
                                HasTransition(definition, ledger.ReservedStateId, ledger.ReturnedStateId),
                    SpecialLandmarkErrorCode.ResourceLossRisk, ledger.Resource.ToString(),
                    "Each Forge input requires reserve, success consume, and failure return transitions.");
            }
            Require(errors, definition.Resets.Count == 3 && definition.Resets.All(value =>
                    value.ReturnsAllForgeInputs && value.Policy == SpecialLandmarkResetPolicy.ManualReset &&
                    string.Equals(value.RecoveryNodeId, "SL_NODE_FORGE_SAFE_CORRIDOR", StringComparison.Ordinal)),
                SpecialLandmarkErrorCode.ResourceLossRisk, definition.RegionId.Value,
                "Every Forge failure stage must return all inputs through ManualReset to SafeCorridor.");

            var reward = definition.RequiredReward;
            var slot = new SpecialRegionSlotId("SR_SLOT_MOON_SEAL_REWARD");
            var key = SpecialPersistenceKey.ForSlot(
                definition.RegionId, SpecialPersistenceScope.Reward, slot);
            Require(errors, reward != null && reward.SlotId == slot && reward.PersistenceKey == key &&
                            reward.Required && reward.Amount == 1 &&
                            string.Equals(reward.NodeId, "SL_NODE_FORGE_REWARD", StringComparison.Ordinal),
                SpecialLandmarkErrorCode.InvalidSealReward, definition.RegionId.Value,
                "MoonSeal output must be exact one required authoritative Reward slot/key.");
            if (request.FixedSlotLayerPlan != null && reward != null)
                Require(errors, request.FixedSlotLayerPlan.ReplaceableSlots.Any(value =>
                        value.SlotId == reward.SlotId && value.Kind == SpecialRegionSlotKind.Reward &&
                        value.Required && value.PersistenceKey == reward.PersistenceKey),
                    SpecialLandmarkErrorCode.InvalidSealReward, "fixedSlotLayer",
                    "MAP13_03 layer must contain the exact MoonSeal Reward slot/key.");
            Require(errors, request.RewardSafetyProof != null,
                SpecialLandmarkErrorCode.MissingInput, "rewardSafety",
                "Forge MoonSeal output requires MAP13_03 persistence safety proof.");
            if (request.RewardSafetyProof != null && reward != null)
            {
                ValidateDigest(errors, request.ExpectedRewardSafetyDigest,
                    request.RewardSafetyProof.CanonicalDigest, "rewardSafety");
                Require(errors, request.RewardSafetyProof.RegionId == definition.RegionId &&
                                request.RewardSafetyProof.SlotId == reward.SlotId &&
                                request.RewardSafetyProof.PersistenceKey == reward.PersistenceKey &&
                                request.RewardSafetyProof.IsSafe,
                    SpecialLandmarkErrorCode.InvalidSealReward, "rewardSafety",
                    "MoonSeal safety proof must preserve exact key with no permanent loss or duplicate risk.");
            }
            Require(errors, definition.Markers.Any(value => value.Kind == SpecialLandmarkMarkerKind.BossDirection),
                SpecialLandmarkErrorCode.InvalidSealReward, definition.RegionId.Value,
                "Forge success must publish the Boss direction marker.");
        }

        private static void ValidateBoss(
            SpecialLandmarkRegionDefinition definition,
            ICollection<SpecialLandmarkError> errors)
        {
            var roles = definition.States.Select(value => value.Role).ToArray();
            foreach (var role in new[]
                     {
                         SpecialLandmarkStateRole.GateLocked,
                         SpecialLandmarkStateRole.GateAccepted,
                         SpecialLandmarkStateRole.EncounterActive,
                         SpecialLandmarkStateRole.Defeated,
                     })
                Require(errors, roles.Count(value => value == role) == 1,
                    SpecialLandmarkErrorCode.InvalidBossGate, role.ToString(),
                    "Boss gate/encounter state must appear exactly once.");
            Require(errors, definition.Markers.Any(value =>
                    value.Kind == SpecialLandmarkMarkerKind.MoonSealRequirement),
                SpecialLandmarkErrorCode.InvalidBossGate, definition.RegionId.Value,
                "GateAccepted requires an explicit MoonSeal marker without inventory consumption.");
            Require(errors, !definition.IntroducesNewMovementRule,
                SpecialLandmarkErrorCode.NewMovementRuleIntroduced, definition.RegionId.Value,
                "Boss arena may not introduce a new movement rule.");
            Require(errors, definition.Markers.Count(value =>
                    value.Kind == SpecialLandmarkMarkerKind.SeparateMaruStateOwner) == 1,
                SpecialLandmarkErrorCode.InvalidBossGate, definition.RegionId.Value,
                "Maru transition marker must remain a separate owner.");
            var fallFailures = definition.Nodes.Where(value => value.Role == SpecialLandmarkNodeRole.Failure).ToArray();
            Require(errors, fallFailures.Length == 2 && fallFailures.All(failure => definition.Resets.Any(reset =>
                    string.Equals(reset.FailureNodeId, failure.NodeId, StringComparison.Ordinal) &&
                    string.Equals(reset.RecoveryNodeId, "SL_NODE_BOSS_CENTRAL_RECOVERY", StringComparison.Ordinal))),
                SpecialLandmarkErrorCode.MissingFallRecovery, definition.RegionId.Value,
                "Every authored fall/failure must recover to the exact central lower node.");
            Require(errors, definition.Resets.Any(value =>
                    value.Policy == SpecialLandmarkResetPolicy.EncounterReset &&
                    value.PreservesSealAcceptance &&
                    string.Equals(value.FromStateId, "SL_STATE_BOSS_ENCOUNTER_ACTIVE", StringComparison.Ordinal) &&
                    string.Equals(value.ToStateId, "SL_STATE_BOSS_ENCOUNTER_ACTIVE", StringComparison.Ordinal)),
                SpecialLandmarkErrorCode.InvalidResetPolicy, definition.RegionId.Value,
                "Encounter reset must return to EncounterActive without rolling back seal acceptance.");
            Require(errors, definition.Markers.Any(value =>
                    value.Kind == SpecialLandmarkMarkerKind.EncounterPersistence &&
                    value.PersistenceScope == SpecialPersistenceScope.Encounter &&
                    !string.IsNullOrEmpty(value.PersistenceKey.Value)),
                SpecialLandmarkErrorCode.InvalidBossGate, definition.RegionId.Value,
                "Defeated state requires an encounter persistence marker.");
        }

        private static void ValidateMerchant(
            SpecialLandmarkRegionDefinition definition,
            ICollection<SpecialLandmarkError> errors)
        {
            Require(errors, definition.Markers.Count(value =>
                    value.Kind == SpecialLandmarkMarkerKind.ShopSafeZone) == 1,
                SpecialLandmarkErrorCode.MissingSafeZone, definition.RegionId.Value,
                "Merchant shell requires one exact shop safe zone.");
            Require(errors, definition.Markers.Count(value =>
                    value.Kind == SpecialLandmarkMarkerKind.EntranceCue) == 2,
                SpecialLandmarkErrorCode.MissingSafeZone, definition.RegionId.Value,
                "Merchant shell requires two distinct entrance cues.");
            var expected = new[]
            {
                SpecialLandmarkMerchantVariant.Alien,
                SpecialLandmarkMerchantVariant.Rabbit,
                SpecialLandmarkMerchantVariant.Spacefarer,
                SpecialLandmarkMerchantVariant.Machine,
            };
            Require(errors, definition.MerchantVariants.SequenceEqual(expected),
                SpecialLandmarkErrorCode.InvalidState, definition.RegionId.Value,
                "Merchant variants must be exact Alien/Rabbit/Spacefarer/Machine without RNG selection.");
            foreach (var role in new[]
                     {
                         SpecialLandmarkStateRole.MerchantAvailable,
                         SpecialLandmarkStateRole.Visited,
                         SpecialLandmarkStateRole.Departed,
                     })
                Require(errors, definition.States.Count(value => value.Role == role) == 1,
                    SpecialLandmarkErrorCode.InvalidState, role.ToString(),
                    "Merchant state must appear exactly once.");
        }

        private static void ValidateMaru(
            SpecialLandmarkRegionDefinition definition,
            ICollection<SpecialLandmarkError> errors)
        {
            Require(errors, definition.Markers.Count(value =>
                    value.Kind == SpecialLandmarkMarkerKind.NonCombatSafeZone) == 1,
                SpecialLandmarkErrorCode.MissingSafeZone, definition.RegionId.Value,
                "Maru shrine requires one exact non-combat safe zone.");
            var preview = definition.Markers.SingleOrDefault(value =>
                value.Kind == SpecialLandmarkMarkerKind.ChoicePreview);
            Require(errors, preview != null && definition.Transitions.All(value => value.Order > preview.Order),
                SpecialLandmarkErrorCode.MissingChoicePreview, definition.RegionId.Value,
                "Choice effect preview must be published before every choice transition.");
            foreach (var role in new[]
                     {
                         SpecialLandmarkStateRole.Offered,
                         SpecialLandmarkStateRole.Ignored,
                         SpecialLandmarkStateRole.ShortHint,
                         SpecialLandmarkStateRole.StrongHint,
                     })
                Require(errors, definition.States.Count(value => value.Role == role) == 1,
                    SpecialLandmarkErrorCode.InvalidState, role.ToString(),
                    "Maru choice state must appear exactly once.");
            Require(errors, definition.Markers.Any(value =>
                    value.Kind == SpecialLandmarkMarkerKind.RareTerrainCompass) &&
                            definition.Markers.Any(value =>
                    value.Kind == SpecialLandmarkMarkerKind.MaruAttentionIncrease),
                SpecialLandmarkErrorCode.InvalidState, definition.RegionId.Value,
                "StrongHint must publish compass and Maru-attention markers together.");
            Require(errors, definition.Resets.Any(value =>
                    value.Policy == SpecialLandmarkResetPolicy.PersistentChoice && value.PreventsReroll),
                SpecialLandmarkErrorCode.DuplicateBenefitRisk, definition.RegionId.Value,
                "PersistentChoice revisit must prevent reroll and duplicate benefit.");
        }

        private static SpecialLandmarkRouteWitness BuildWitness(
            SpecialLandmarkRegionDefinition definition,
            SpecialLandmarkRouteDefinition route)
        {
            var edges = definition.Edges.Where(value => route.EdgeIds.Contains(value.EdgeId))
                .OrderBy(value => value.Order).ThenBy(value => value.EdgeId, StringComparer.Ordinal).ToArray();
            var nodes = new List<string> { route.StartNodeId };
            nodes.AddRange(edges.Select(value => value.ToNodeId));
            return new SpecialLandmarkRouteWitness(route.RouteId, route.Kind, nodes);
        }

        private static List<string> RouteNodes(
            SpecialLandmarkRegionDefinition definition,
            SpecialLandmarkRouteDefinition route)
        {
            if (route == null) return new List<string>();
            var edges = definition.Edges.Where(value => route.EdgeIds.Contains(value.EdgeId))
                .OrderBy(value => value.Order).ThenBy(value => value.EdgeId, StringComparer.Ordinal).ToArray();
            var result = new List<string> { route.StartNodeId };
            result.AddRange(edges.Select(value => value.ToNodeId));
            return result;
        }

        private static bool IsConnected(IReadOnlyCollection<SpecialLandmarkDesignChunk> chunks)
        {
            if (chunks.Count == 0) return false;
            var remaining = new HashSet<SpecialLandmarkDesignChunk>(chunks);
            var pending = new Queue<SpecialLandmarkDesignChunk>();
            var first = remaining.First();
            remaining.Remove(first);
            pending.Enqueue(first);
            while (pending.Count != 0)
            {
                var current = pending.Dequeue();
                foreach (var next in new[]
                         {
                             new SpecialLandmarkDesignChunk(current.X - 1, current.Y),
                             new SpecialLandmarkDesignChunk(current.X + 1, current.Y),
                             new SpecialLandmarkDesignChunk(current.X, current.Y - 1),
                             new SpecialLandmarkDesignChunk(current.X, current.Y + 1),
                         })
                    if (remaining.Remove(next)) pending.Enqueue(next);
            }
            return remaining.Count == 0;
        }

        private static AccessClass ExpectedAccess(
            SpecialLandmarkBindingKind binding,
            SpecialLandmarkRouteKind routeKind)
        {
            if (binding == SpecialLandmarkBindingKind.DeferredOptionalLocal)
                return AccessClass.OptionalNoTool;
            return routeKind == SpecialLandmarkRouteKind.High
                ? AccessClass.OptionalNoTool
                : AccessClass.MandatoryNoTool;
        }

        private static bool HasStateRole(
            IReadOnlyDictionary<string, SpecialLandmarkStateDefinition> states,
            string stateId,
            SpecialLandmarkStateRole role)
            => states.TryGetValue(stateId, out var state) && state.Role == role;

        private static bool HasTransition(
            SpecialLandmarkRegionDefinition definition,
            string from,
            string to)
            => definition.Transitions.Any(value =>
                string.Equals(value.FromStateId, from, StringComparison.Ordinal) &&
                string.Equals(value.ToStateId, to, StringComparison.Ordinal));

        private static bool TryExpectedIdentity(
            SpecialLandmarkKind landmark,
            out string regionId,
            out SpecialRegionKind regionKind,
            out SpecialLandmarkTheme theme,
            out SpecialLandmarkBindingKind binding,
            out int reservedWidth,
            out int reservedHeight,
            out int designX,
            out int designY,
            out int designWidth,
            out int designHeight,
            out int activeChunks)
        {
            regionId = string.Empty;
            regionKind = default(SpecialRegionKind);
            theme = default(SpecialLandmarkTheme);
            binding = default(SpecialLandmarkBindingKind);
            reservedWidth = reservedHeight = designX = designY = designWidth = designHeight = activeChunks = 0;
            switch (landmark)
            {
                case SpecialLandmarkKind.MoonSealForge:
                    regionId = "SR_MOON_SEAL_FORGE_9";
                    regionKind = SpecialRegionKind.Forge;
                    theme = SpecialLandmarkTheme.AbandonedMill;
                    binding = SpecialLandmarkBindingKind.PlacedMandatorySite;
                    reservedWidth = reservedHeight = 1;
                    designX = 0; designY = 4; designWidth = 48; designHeight = 24; activeChunks = 9;
                    return true;
                case SpecialLandmarkKind.BossSealArena:
                    regionId = "SR_MOON_BOSS_SEAL_ARENA_12";
                    regionKind = SpecialRegionKind.Boss;
                    theme = SpecialLandmarkTheme.MoonPalaceCommon;
                    binding = SpecialLandmarkBindingKind.PlacedMandatorySite;
                    reservedWidth = reservedHeight = 1;
                    designX = designY = 0; designWidth = 48; designHeight = 32; activeChunks = 12;
                    return true;
                case SpecialLandmarkKind.WanderingMerchantCave:
                    regionId = "SR_WANDERING_MERCHANT_CAVE_3";
                    regionKind = SpecialRegionKind.OptionalLandmark;
                    theme = SpecialLandmarkTheme.Any;
                    binding = SpecialLandmarkBindingKind.DeferredOptionalLocal;
                    designX = designY = 0; designWidth = 24; designHeight = 16; activeChunks = 3;
                    return true;
                case SpecialLandmarkKind.MaruTimeShrine:
                    regionId = "SR_MARU_TIME_SHRINE_5";
                    regionKind = SpecialRegionKind.OptionalLandmark;
                    theme = SpecialLandmarkTheme.MoonPalaceCommon;
                    binding = SpecialLandmarkBindingKind.DeferredOptionalLocal;
                    designX = designY = 0; designWidth = 24; designHeight = 24; activeChunks = 5;
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsStableId(string value, string prefix)
            => !string.IsNullOrWhiteSpace(value) && value.StartsWith(prefix, StringComparison.Ordinal) &&
               value.All(character => char.IsUpper(character) || char.IsDigit(character) || character == '_');

        private static void ValidateDigest(
            ICollection<SpecialLandmarkError> errors,
            string expected,
            string actual,
            string owner)
            => Require(errors, !string.IsNullOrEmpty(expected) &&
                               string.Equals(expected, actual, StringComparison.Ordinal),
                SpecialLandmarkErrorCode.DigestMismatch, owner,
                "Expected source digest must exactly match the public source publication.");

        private static void Require(
            ICollection<SpecialLandmarkError> errors,
            bool condition,
            SpecialLandmarkErrorCode code,
            string owner,
            string message)
        {
            if (!condition) Add(errors, code, owner, message);
        }

        private static SpecialLandmarkResult Failure(IEnumerable<SpecialLandmarkError> errors)
            => new SpecialLandmarkResult(null, errors);

        private static void Add(
            ICollection<SpecialLandmarkError> errors,
            SpecialLandmarkErrorCode code,
            string owner,
            string message)
            => errors.Add(new SpecialLandmarkError(code, owner, message));
    }
}
