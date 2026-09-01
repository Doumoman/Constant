using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Pipeline;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class WorldBoundarySocketIntegrator
    {
        public const string ReferencePublicationLabel = "REFERENCE INTERSECTOR EDGE PLAN";
        public const int NewRngDrawCount = 0;

        public static WorldIntersectorBuildResult Integrate(WorldIntersectorBuildRequest request)
        {
            var failures = new List<WorldIntersectorFailure>();
            if (request == null)
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.MissingRequest,
                    "request",
                    "World intersector build request is required."));
                return WorldIntersectorBuildResult.Fail(failures);
            }

            ValidateRequest(request, failures);
            if (failures.Count != 0) return WorldIntersectorBuildResult.Fail(failures);

            var nodesById = request.WorldPlan.Nodes.ToDictionary(value => value.Id);
            var projections = IndexProjections(request.SocketProjections, failures);
            var bindings = IndexBindings(request.BoundaryBindings, failures);
            var expectedEdges = EnumerateInternalEdges().ToArray();

            if (expectedEdges.Length != WorldIntersectorEdgePlan.InternalEdgeCount)
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.EdgeCountMismatch,
                    "world",
                    "The 13x13 topology must publish exactly 312 internal edges."));
            }

            var expectedIds = new HashSet<WorldIntersectorEdgeId>(expectedEdges.Select(value => value.Id));
            foreach (var binding in bindings.Values.Where(value => !expectedIds.Contains(value.EdgeId)))
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.BoundaryBindingUnknownEdge,
                    binding.EdgeId.ToString(),
                    "Boundary binding does not identify an internal neighbor edge."));
            }

            var edges = new List<WorldIntersectorEdge>(WorldIntersectorEdgePlan.InternalEdgeCount);
            foreach (var expected in expectedEdges)
            {
                BuildEdge(expected, nodesById, projections, bindings, failures, edges);
            }

            if (failures.Count != 0) return WorldIntersectorBuildResult.Fail(failures);
            if (edges.Count != WorldIntersectorEdgePlan.InternalEdgeCount ||
                edges.Select(value => value.Id).Distinct().Count() != WorldIntersectorEdgePlan.InternalEdgeCount)
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.EdgeCountMismatch,
                    "world",
                    "The completed edge inventory must contain 312 unique edges."));
                return WorldIntersectorBuildResult.Fail(failures);
            }

            var outputDigest = WorldIntersectorDigest.ComputeOutput(request, edges);
            if (!WorldSolveDigest.IsLowerHexSha256(outputDigest))
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.EmptyEdgeSignature,
                    "world",
                    "The edge-plan output digest must be lower-hex SHA-256."));
                return WorldIntersectorBuildResult.Fail(failures);
            }

            return WorldIntersectorBuildResult.Pass(
                new WorldIntersectorEdgePlan(request, edges, outputDigest));
        }

        public static bool IsSideOpen(int routeType, WorldSectorSide side, bool explicitSocketEvidence)
        {
            if (explicitSocketEvidence) return true;
            switch (routeType)
            {
                case 1:
                    return side == WorldSectorSide.West || side == WorldSectorSide.East;
                case 2:
                    return side == WorldSectorSide.West || side == WorldSectorSide.East ||
                           side == WorldSectorSide.South;
                case 3:
                    return side == WorldSectorSide.West || side == WorldSectorSide.East ||
                           side == WorldSectorSide.North;
                case 4:
                    return side == WorldSectorSide.South || side == WorldSectorSide.North;
                default:
                    return false;
            }
        }

        public static WorldSectorSide Opposite(WorldSectorSide side)
        {
            switch (side)
            {
                case WorldSectorSide.West: return WorldSectorSide.East;
                case WorldSectorSide.East: return WorldSectorSide.West;
                case WorldSectorSide.South: return WorldSectorSide.North;
                case WorldSectorSide.North: return WorldSectorSide.South;
                default: throw new ArgumentOutOfRangeException(nameof(side));
            }
        }

        public static WorldTraversalApron BuildTraversalApron(
            WorldSectorSide side,
            WorldSocketAnchor anchor)
        {
            if (anchor == null) return null;
            var half = anchor.ApertureSize / 2;
            switch (side)
            {
                case WorldSectorSide.West:
                    return new WorldTraversalApron(
                        0,
                        Clamp(anchor.LocalY - half, 0, WorldPlanInput.SectorHeightTiles - anchor.ApertureSize),
                        3,
                        anchor.ApertureSize);
                case WorldSectorSide.East:
                    return new WorldTraversalApron(
                        WorldPlanInput.SectorWidthTiles - 3,
                        Clamp(anchor.LocalY - half, 0, WorldPlanInput.SectorHeightTiles - anchor.ApertureSize),
                        3,
                        anchor.ApertureSize);
                case WorldSectorSide.South:
                    return new WorldTraversalApron(
                        Clamp(anchor.LocalX - half, 0, WorldPlanInput.SectorWidthTiles - anchor.ApertureSize),
                        0,
                        anchor.ApertureSize,
                        3);
                case WorldSectorSide.North:
                    return new WorldTraversalApron(
                        Clamp(anchor.LocalX - half, 0, WorldPlanInput.SectorWidthTiles - anchor.ApertureSize),
                        WorldPlanInput.SectorHeightTiles - 3,
                        anchor.ApertureSize,
                        3);
                default:
                    return null;
            }
        }

        public static bool IsApprovedBoundaryBinding(WorldBoundaryBinding binding)
        {
            if (binding == null || string.IsNullOrEmpty(binding.CandidateId) ||
                string.IsNullOrEmpty(binding.SourceOwner) ||
                !TryGetBoundaryAuthority(binding.PairId, out var profiles, out var ownsCandidate) ||
                !profiles.Contains(binding.ProfileId, StringComparer.Ordinal) ||
                !ownsCandidate(binding.CandidateId) ||
                (string.Equals(binding.ProfileId, MoonpalaceBoundaryWarningRequirement.LayerProfileId,
                     StringComparison.Ordinal) && binding.EdgeId.Orientation != WorldEdgeOrientation.Vertical))
            {
                return false;
            }

            var warnings = new HashSet<MoonpalaceBoundaryWarningMarkerCategory>();
            foreach (var token in binding.WarningModalities)
            {
                if (!MoonpalaceBoundaryWarningMarkerCategory.TryParse(token, out var category)) return false;
                warnings.Add(category);
            }

            return warnings.Count >= MoonpalaceBiomePairDefinition.RequiredMinimumWarningMarkerCount;
        }

        private static void ValidateRequest(
            WorldIntersectorBuildRequest request,
            ICollection<WorldIntersectorFailure> failures)
        {
            var plan = request.WorldPlan;
            var solve = request.SolveOrder;
            if (plan == null || solve == null || !solve.Success || solve.Input == null ||
                solve.Steps.Count != WorldPlanInput.SectorCount ||
                !string.Equals(plan.CanonicalDigest, solve.InputDigest, StringComparison.Ordinal))
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.InvalidWorldPlan,
                    "world-plan",
                    "A successful 169-sector MAP15_01 world plan and solve order are required."));
                return;
            }

            if (!WorldSolveDigest.IsLowerHexSha256(plan.CanonicalDigest) ||
                !WorldSolveDigest.IsLowerHexSha256(solve.InputDigest) ||
                !WorldSolveDigest.IsLowerHexSha256(solve.OutputDigest) ||
                !WorldSolveDigest.IsLowerHexSha256(request.Map14HandoffDigest) ||
                !WorldSolveDigest.IsLowerHexSha256(request.BoundaryAuthorityDigest) ||
                !WorldSolveDigest.IsLowerHexSha256(request.CanonicalDigest))
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.InvalidDigest,
                    "digest",
                    "All handoff and canonical digests must be lower-hex SHA-256."));
            }

            var nodes = plan.Nodes;
            if (nodes.Count != WorldPlanInput.SectorCount ||
                nodes.Select(value => value.Id).Distinct().Count() != WorldPlanInput.SectorCount ||
                nodes.Select(value => value.Coordinate).Distinct().Count() != WorldPlanInput.SectorCount ||
                nodes.Any(value => !value.Coordinate.IsInBounds || value.Id != value.Coordinate.RowMajorId))
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.InvalidTopology,
                    "world-topology",
                    "World plan must expose each row-major coordinate in the 13x13 topology exactly once."));
            }

            if (request.FallbackCarveCount != 0 || solve.FallbackCarveCount != 0)
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.FallbackCarveRequired,
                    "fallback",
                    "Intersector integration cannot carve a fallback corridor."));
            }

            if (request.NewRngDrawCount != NewRngDrawCount || solve.NewRngDrawCount != 0 ||
                plan.GeneratedFileWriteCount != 0 || plan.TilemapMutationCount != 0 ||
                plan.SceneMutationCount != 0 || plan.PrefabMutationCount != 0 ||
                plan.GameObjectMutationCount != 0 || plan.GameplaySpawnCount != 0 ||
                plan.SectorPlannerMutationCount != 0 || request.GeneratedFileWriteCount != 0 ||
                request.TilemapMutationCount != 0 || request.SceneMutationCount != 0 ||
                request.PrefabMutationCount != 0 || request.GameObjectMutationCount != 0 ||
                request.GameplaySpawnCount != 0 || request.SectorPlannerMutationCount != 0 ||
                request.WorldPlanMutationCount != 0)
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.MutationClaim,
                    "mutation",
                    "MAP15_02 permits no RNG draw, asset write, world-plan mutation, or runtime mutation."));
            }
        }

        private static Dictionary<ProjectionKey, WorldSocketProjection> IndexProjections(
            IEnumerable<WorldSocketProjection> source,
            ICollection<WorldIntersectorFailure> failures)
        {
            var result = new Dictionary<ProjectionKey, WorldSocketProjection>();
            foreach (var projection in source)
            {
                var key = new ProjectionKey(projection.SectorId, projection.Side);
                if (!result.TryAdd(key, projection))
                {
                    failures.Add(Failure(
                        WorldIntersectorFailureCode.DuplicateSocketProjection,
                        projection.SectorId + ":" + projection.Side,
                        "A sector side can project at most one intersector socket."));
                }
            }
            return result;
        }

        private static Dictionary<WorldIntersectorEdgeId, WorldBoundaryBinding> IndexBindings(
            IEnumerable<WorldBoundaryBinding> source,
            ICollection<WorldIntersectorFailure> failures)
        {
            var result = new Dictionary<WorldIntersectorEdgeId, WorldBoundaryBinding>();
            foreach (var binding in source)
            {
                if (!result.TryAdd(binding.EdgeId, binding))
                {
                    failures.Add(Failure(
                        WorldIntersectorFailureCode.DuplicateBoundaryBinding,
                        binding.EdgeId.ToString(),
                        "An internal edge can have at most one boundary binding."));
                }
            }
            return result;
        }

        private static void BuildEdge(
            ExpectedEdge expected,
            IReadOnlyDictionary<WorldSectorId, WorldSectorNode> nodes,
            IReadOnlyDictionary<ProjectionKey, WorldSocketProjection> projections,
            IReadOnlyDictionary<WorldIntersectorEdgeId, WorldBoundaryBinding> bindings,
            ICollection<WorldIntersectorFailure> failures,
            ICollection<WorldIntersectorEdge> edges)
        {
            if (!nodes.TryGetValue(expected.FirstSector, out var firstNode) ||
                !nodes.TryGetValue(expected.SecondSector, out var secondNode))
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.MissingSector,
                    expected.Id.ToString(),
                    "Internal edge references a missing sector."));
                return;
            }

            if (!projections.TryGetValue(new ProjectionKey(expected.FirstSector, expected.FirstSide), out var first) ||
                !projections.TryGetValue(new ProjectionKey(expected.SecondSector, expected.SecondSide), out var second))
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.MissingCounterpartEndpoint,
                    expected.Id.ToString(),
                    "Internal edge requires both facing sector-side projections."));
                return;
            }

            var edgeFailureCount = failures.Count;
            ValidateProjection(expected.Id, first, expected.FirstSide, failures);
            ValidateProjection(expected.Id, second, expected.SecondSide, failures);

            if (Opposite(first.Side) != second.Side)
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.EndpointSideMismatch,
                    expected.Id.ToString(),
                    "The two endpoint sides must face one another."));
            }

            if (first.RequiresMandatoryContinuity != second.RequiresMandatoryContinuity ||
                first.RequiresBoundaryBinding != second.RequiresBoundaryBinding)
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.RouteFactMismatch,
                    expected.Id.ToString(),
                    "Mandatory-continuity and boundary-required facts must be symmetric."));
            }

            if (first.ExplicitSocketEvidence != second.ExplicitSocketEvidence)
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.ExternalSocketMismatch,
                    expected.Id.ToString(),
                    "External socket evidence must be present on both facing endpoints."));
            }

            var firstOpen = IsSideOpen(firstNode.RouteType, first.Side, first.ExplicitSocketEvidence);
            var secondOpen = IsSideOpen(secondNode.RouteType, second.Side, second.ExplicitSocketEvidence);
            if (firstOpen != secondOpen)
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.RouteSocketIncompatible,
                    expected.Id.ToString(),
                    "Facing endpoint route openings are incompatible."));
            }

            var mandatory = first.RequiresMandatoryContinuity && second.RequiresMandatoryContinuity;
            if (mandatory && (!firstOpen || !secondOpen ||
                              firstNode.AccessClass != AccessClass.MandatoryNoTool ||
                              secondNode.AccessClass != AccessClass.MandatoryNoTool))
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.MandatoryRouteBlocked,
                    expected.Id.ToString(),
                    "Mandatory route edges require two open MandatoryNoTool endpoints."));
            }

            var boundaryRequired = first.RequiresBoundaryBinding && second.RequiresBoundaryBinding;
            bindings.TryGetValue(expected.Id, out var boundary);
            if (boundaryRequired && boundary == null)
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.BoundaryBindingMissing,
                    expected.Id.ToString(),
                    "Boundary-required edge has no MAP08 binding."));
            }
            else if (!boundaryRequired && boundary != null)
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.BoundaryBindingUnknownEdge,
                    expected.Id.ToString(),
                    "Boundary binding was supplied for an edge that did not request one."));
            }
            else if (boundary != null)
            {
                ValidateBoundary(expected, boundary, failures);
            }

            var firstApron = BuildTraversalApron(first.Side, first.Anchor);
            var secondApron = BuildTraversalApron(second.Side, second.Anchor);
            if (firstApron == null || secondApron == null || !firstApron.IsInBounds || !secondApron.IsInBounds ||
                !firstApron.Contains(first.Anchor) || !secondApron.Contains(second.Anchor))
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.InvalidTraversalApron,
                    expected.Id.ToString(),
                    "Traversal aprons must be non-empty, in bounds, and contain their anchors."));
            }

            if (failures.Count != edgeFailureCount) return;

            var endpoints = new[]
            {
                CreateEndpoint(firstNode, first, firstApron, firstOpen),
                CreateEndpoint(secondNode, second, secondApron, secondOpen),
            };
            var external = first.ExplicitSocketEvidence && second.ExplicitSocketEvidence;
            var routeDigest = WorldIntersectorDigest.ComputeRouteSignature(
                expected.Id, endpoints, firstOpen == secondOpen, mandatory, external);
            if (!WorldSolveDigest.IsLowerHexSha256(routeDigest))
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.EmptyEdgeSignature,
                    expected.Id.ToString(),
                    "Edge route signature must be lower-hex SHA-256."));
                return;
            }

            edges.Add(new WorldIntersectorEdge(
                expected.Id,
                endpoints,
                boundary,
                new WorldEdgeRouteSignature(firstOpen == secondOpen, mandatory, external, routeDigest)));
        }

        private static void ValidateProjection(
            WorldIntersectorEdgeId edgeId,
            WorldSocketProjection projection,
            WorldSectorSide expectedSide,
            ICollection<WorldIntersectorFailure> failures)
        {
            if (projection.Side != expectedSide)
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.EndpointSideMismatch,
                    edgeId.ToString(),
                    "Endpoint side does not match its row-major neighbor direction."));
            }

            var anchor = projection.Anchor;
            if (anchor == null || !anchor.IsInBounds)
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.AnchorOutOfBounds,
                    edgeId.ToString(),
                    "Socket anchor must be inside the 48x32 sector frame."));
                return;
            }

            if (!anchor.IsOnSide(expectedSide))
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.AnchorNotOnSide,
                    edgeId.ToString(),
                    "Socket anchor must lie on its declared sector side."));
            }

            var maximum = expectedSide == WorldSectorSide.West || expectedSide == WorldSectorSide.East
                ? WorldPlanInput.SectorHeightTiles
                : WorldPlanInput.SectorWidthTiles;
            if (anchor.ApertureSize <= 0 || anchor.ApertureSize > maximum || anchor.ApertureSize % 2 == 0)
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.InvalidAperture,
                    edgeId.ToString(),
                    "Socket aperture must be positive, odd, and fit the corresponding sector side."));
            }

            if (string.IsNullOrEmpty(projection.SourceOwner))
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.RouteFactMismatch,
                    edgeId.ToString(),
                    "Socket projection must identify its public source owner."));
            }
        }

        private static void ValidateBoundary(
            ExpectedEdge expected,
            WorldBoundaryBinding boundary,
            ICollection<WorldIntersectorFailure> failures)
        {
            if (boundary.EdgeId != expected.Id || boundary.EdgeId.Orientation != expected.Id.Orientation)
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.BoundaryOrientationMismatch,
                    expected.Id.ToString(),
                    "Boundary binding orientation must match the world edge."));
                return;
            }

            if (!TryGetBoundaryAuthority(boundary.PairId, out var profiles, out var ownsCandidate))
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.BoundaryPairNotApproved,
                    expected.Id.ToString(),
                    "Boundary pair is not one of the six MAP08 public authorities."));
                return;
            }

            if (!profiles.Contains(boundary.ProfileId, StringComparer.Ordinal) ||
                !ownsCandidate(boundary.CandidateId) || string.IsNullOrEmpty(boundary.SourceOwner))
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.BoundaryProfileNotApproved,
                    expected.Id.ToString(),
                    "Boundary profile, candidate, and source owner must match the pair authority."));
            }

            if (string.Equals(boundary.ProfileId, MoonpalaceBoundaryWarningRequirement.LayerProfileId,
                    StringComparison.Ordinal) && expected.Id.Orientation != WorldEdgeOrientation.Vertical)
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.BoundaryOrientationMismatch,
                    expected.Id.ToString(),
                    "BOUND_LAYER is approved only for vertical boundary orientation."));
            }

            var parsed = new HashSet<MoonpalaceBoundaryWarningMarkerCategory>();
            foreach (var token in boundary.WarningModalities)
            {
                if (!MoonpalaceBoundaryWarningMarkerCategory.TryParse(token, out var category))
                {
                    failures.Add(Failure(
                        WorldIntersectorFailureCode.BoundaryWarningInsufficient,
                        expected.Id.ToString(),
                        "Boundary warning modality is not approved by MAP08."));
                    continue;
                }
                parsed.Add(category);
            }

            if (parsed.Count < MoonpalaceBiomePairDefinition.RequiredMinimumWarningMarkerCount)
            {
                failures.Add(Failure(
                    WorldIntersectorFailureCode.BoundaryWarningInsufficient,
                    expected.Id.ToString(),
                    "Boundary binding requires at least two distinct approved warning modalities."));
            }
        }

        private static WorldEdgeEndpoint CreateEndpoint(
            WorldSectorNode node,
            WorldSocketProjection projection,
            WorldTraversalApron apron,
            bool isOpen)
        {
            return new WorldEdgeEndpoint(
                node.Id,
                projection.Side,
                projection.Anchor,
                apron,
                node.RouteType,
                node.AccessClass,
                projection.ExplicitSocketEvidence,
                projection.RequiresMandatoryContinuity,
                isOpen,
                projection.SourceOwner);
        }

        private static IEnumerable<ExpectedEdge> EnumerateInternalEdges()
        {
            for (var y = 0; y < WorldPlanInput.SectorRows; y++)
            {
                for (var x = 0; x < WorldPlanInput.SectorColumns - 1; x++)
                {
                    yield return new ExpectedEdge(
                        new WorldSectorCoordinate(x, y).RowMajorId,
                        new WorldSectorCoordinate(x + 1, y).RowMajorId,
                        WorldSectorSide.East,
                        WorldSectorSide.West,
                        WorldEdgeOrientation.Horizontal);
                }
            }

            for (var y = 0; y < WorldPlanInput.SectorRows - 1; y++)
            {
                for (var x = 0; x < WorldPlanInput.SectorColumns; x++)
                {
                    yield return new ExpectedEdge(
                        new WorldSectorCoordinate(x, y).RowMajorId,
                        new WorldSectorCoordinate(x, y + 1).RowMajorId,
                        WorldSectorSide.North,
                        WorldSectorSide.South,
                        WorldEdgeOrientation.Vertical);
                }
            }
        }

        private static bool TryGetBoundaryAuthority(
            string pairId,
            out IReadOnlyList<string> profiles,
            out Func<string, bool> ownsCandidate)
        {
            if (string.Equals(pairId, MoonpalaceCraterRootBoundaryAuthoringContract.PairRuleId,
                    StringComparison.Ordinal))
            {
                profiles = MoonpalaceCraterRootBoundaryAuthoringContract.ProfileIds;
                ownsCandidate = MoonpalaceCraterRootBoundaryAuthoringContract.IsOwnedCandidate;
                return true;
            }
            if (string.Equals(pairId, MoonpalaceCraterMillBoundaryAuthoringContract.PairRuleId,
                    StringComparison.Ordinal))
            {
                profiles = MoonpalaceCraterMillBoundaryAuthoringContract.ProfileIds;
                ownsCandidate = MoonpalaceCraterMillBoundaryAuthoringContract.IsOwnedCandidate;
                return true;
            }
            if (string.Equals(pairId, MoonpalaceCraterDoughBoundaryAuthoringContract.PairRuleId,
                    StringComparison.Ordinal))
            {
                profiles = MoonpalaceCraterDoughBoundaryAuthoringContract.ProfileIds;
                ownsCandidate = MoonpalaceCraterDoughBoundaryAuthoringContract.IsOwnedCandidate;
                return true;
            }
            if (string.Equals(pairId, MoonpalaceRootMillBoundaryAuthoringContract.PairRuleId,
                    StringComparison.Ordinal))
            {
                profiles = MoonpalaceRootMillBoundaryAuthoringContract.ProfileIds;
                ownsCandidate = MoonpalaceRootMillBoundaryAuthoringContract.IsOwnedCandidate;
                return true;
            }
            if (string.Equals(pairId, MoonpalaceRootDoughBoundaryAuthoringContract.PairRuleId,
                    StringComparison.Ordinal))
            {
                profiles = MoonpalaceRootDoughBoundaryAuthoringContract.ProfileIds;
                ownsCandidate = MoonpalaceRootDoughBoundaryAuthoringContract.IsOwnedCandidate;
                return true;
            }
            if (string.Equals(pairId, MoonpalaceMillDoughBoundaryAuthoringContract.PairRuleId,
                    StringComparison.Ordinal))
            {
                profiles = MoonpalaceMillDoughBoundaryAuthoringContract.ProfileIds;
                ownsCandidate = MoonpalaceMillDoughBoundaryAuthoringContract.IsOwnedCandidate;
                return true;
            }

            profiles = Array.Empty<string>();
            ownsCandidate = _ => false;
            return false;
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static WorldIntersectorFailure Failure(
            WorldIntersectorFailureCode code,
            string subject,
            string reason)
        {
            return new WorldIntersectorFailure(code, subject, reason);
        }

        private readonly struct ProjectionKey : IEquatable<ProjectionKey>
        {
            public ProjectionKey(WorldSectorId sectorId, WorldSectorSide side)
            {
                SectorId = sectorId;
                Side = side;
            }

            public WorldSectorId SectorId { get; }
            public WorldSectorSide Side { get; }
            public bool Equals(ProjectionKey other) => SectorId == other.SectorId && Side == other.Side;
            public override bool Equals(object obj) => obj is ProjectionKey other && Equals(other);
            public override int GetHashCode()
            {
                unchecked
                {
                    return (SectorId.GetHashCode() * 397) ^ (int)Side;
                }
            }
        }

        private readonly struct ExpectedEdge
        {
            public ExpectedEdge(
                WorldSectorId firstSector,
                WorldSectorId secondSector,
                WorldSectorSide firstSide,
                WorldSectorSide secondSide,
                WorldEdgeOrientation orientation)
            {
                FirstSector = firstSector;
                SecondSector = secondSector;
                FirstSide = firstSide;
                SecondSide = secondSide;
                Id = new WorldIntersectorEdgeId(firstSector, secondSector, orientation);
            }

            public WorldSectorId FirstSector { get; }
            public WorldSectorId SecondSector { get; }
            public WorldSectorSide FirstSide { get; }
            public WorldSectorSide SecondSide { get; }
            public WorldIntersectorEdgeId Id { get; }
        }
    }
}
