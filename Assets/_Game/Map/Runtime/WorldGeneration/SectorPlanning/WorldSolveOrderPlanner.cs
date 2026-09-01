using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Pipeline;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class WorldSolveOrderPlanner
    {
        public const string ReferencePublicationLabel = "REFERENCE WORLD PLAN";
        public const string DownstreamOwner = "MAP15_02_INTEGRATE_INTERSECTOR_SOCKETS_AND_BOUNDARIES";
        public const int NewRngDrawCount = 0;
        public const bool OpensDownstreamTask = false;

        public static WorldSolveOrderResult Plan(WorldPlanInput input)
        {
            var failures = Validate(input);
            if (failures.Count != 0) return WorldSolveOrderResult.Fail(failures);

            var nodes = input.Nodes.ToDictionary(value => value.Id);
            var originalDependencyCounts = nodes.Keys.ToDictionary(value => value, value => 0);
            var unresolvedDependencyCounts = nodes.Keys.ToDictionary(value => value, value => 0);
            var incoming = nodes.Keys.ToDictionary(value => value, value => new List<WorldDependencyEdge>());
            var outgoing = nodes.Keys.ToDictionary(value => value, value => new List<WorldDependencyEdge>());

            foreach (var edge in input.Dependencies)
            {
                incoming[edge.ToSector].Add(edge);
                outgoing[edge.FromSector].Add(edge);
                originalDependencyCounts[edge.ToSector]++;
                unresolvedDependencyCounts[edge.ToSector]++;
            }

            var ready = nodes.Values.Where(value => unresolvedDependencyCounts[value.Id] == 0).ToList();
            var steps = new List<WorldSolveStep>(WorldPlanInput.SectorCount);
            while (ready.Count != 0)
            {
                ready.Sort((left, right) => CompareReady(left, right, originalDependencyCounts));
                var node = ready[0];
                ready.RemoveAt(0);
                var nodeIncoming = incoming[node.Id];
                steps.Add(new WorldSolveStep(
                    steps.Count,
                    node.Id,
                    Priority(node),
                    nodeIncoming.Select(value => value.FromSector),
                    WorldSolveDigest.ComputeReason(node, nodeIncoming)));

                foreach (var edge in outgoing[node.Id].OrderBy(value => value))
                {
                    unresolvedDependencyCounts[edge.ToSector]--;
                    if (unresolvedDependencyCounts[edge.ToSector] == 0)
                        ready.Add(nodes[edge.ToSector]);
                }
            }

            if (steps.Count != WorldPlanInput.SectorCount)
            {
                var unresolved = unresolvedDependencyCounts
                    .Where(value => value.Value != 0)
                    .Select(value => value.Key.ToString())
                    .OrderBy(value => value, StringComparer.Ordinal);
                return WorldSolveOrderResult.Fail(new[]
                {
                    new WorldSolveFailure(
                        WorldSolveFailureCode.CycleDetected,
                        "DEPENDENCY_GRAPH",
                        string.Join(",", unresolved)),
                });
            }

            return WorldSolveOrderResult.Pass(input, steps, WorldSolveDigest.ComputeOutput(input, steps));
        }

        public static WorldSolvePriority Priority(WorldSectorNode node)
        {
            if (node == null) return WorldSolvePriority.OrdinaryTerrain;
            if (node.HasSpecialReservation) return WorldSolvePriority.FixedSpecial;
            if (node.IsMandatoryRoute || node.IsBoundaryPair)
                return WorldSolvePriority.MandatoryRouteOrBoundary;
            if (node.HasExternalSocketObligation) return WorldSolvePriority.ExternalSocket;
            if (node.PacingRole == PacingRole.Landmark ||
                node.PacingRole == PacingRole.Resource ||
                node.PacingRole == PacingRole.Boss ||
                node.PacingRole == PacingRole.Discovery ||
                node.PacingRole == PacingRole.Recovery)
                return WorldSolvePriority.PacingConstraint;
            return WorldSolvePriority.OrdinaryTerrain;
        }

        private static List<WorldSolveFailure> Validate(WorldPlanInput input)
        {
            var failures = new List<WorldSolveFailure>();
            if (input == null)
            {
                failures.Add(new WorldSolveFailure(WorldSolveFailureCode.MissingInput, "WORLD_PLAN", "Input is required."));
                return failures;
            }

            if (input.Nodes.Count != WorldPlanInput.SectorCount)
                failures.Add(new WorldSolveFailure(
                    WorldSolveFailureCode.SectorCountMismatch,
                    "WORLD_PLAN",
                    input.Nodes.Count.ToString(CultureInfo.InvariantCulture)));

            foreach (var group in input.Nodes.GroupBy(value => value.Id).Where(value => value.Count() != 1))
                failures.Add(new WorldSolveFailure(
                    WorldSolveFailureCode.DuplicateSectorId,
                    group.Key.ToString(),
                    group.Count().ToString(CultureInfo.InvariantCulture)));
            foreach (var group in input.Nodes.GroupBy(value => value.Coordinate).Where(value => value.Count() != 1))
                failures.Add(new WorldSolveFailure(
                    WorldSolveFailureCode.DuplicateCoordinate,
                    group.Key.ToString(),
                    group.Count().ToString(CultureInfo.InvariantCulture)));

            foreach (var node in input.Nodes)
            {
                if (node.Id.Value < 0 || node.Id.Value >= WorldPlanInput.SectorCount)
                    failures.Add(new WorldSolveFailure(
                        WorldSolveFailureCode.SectorIdOutOfRange,
                        node.Id.ToString(),
                        node.Id.Value.ToString(CultureInfo.InvariantCulture)));
                if (!node.Coordinate.IsInBounds)
                    failures.Add(new WorldSolveFailure(
                        WorldSolveFailureCode.CoordinateOutOfBounds,
                        node.Id.ToString(),
                        node.Coordinate.ToString()));
                if (node.Coordinate.IsInBounds && node.Coordinate.RowMajorId != node.Id)
                    failures.Add(new WorldSolveFailure(
                        WorldSolveFailureCode.SectorIdCoordinateMismatch,
                        node.Id.ToString(),
                        node.Coordinate.RowMajorId.ToString()));
                if (string.IsNullOrEmpty(node.PrimaryBiome) ||
                    node.RouteType < 0 || node.RouteType > 4 ||
                    !AccessClassTokenCodec.IsPublished(node.AccessClass) ||
                    !PacingRoleTokenCodec.IsPublished(node.PacingRole) ||
                    (node.HasSpecialReservation && string.IsNullOrEmpty(node.SpecialReservationId)))
                    failures.Add(new WorldSolveFailure(
                        WorldSolveFailureCode.InvalidNodeFact,
                        node.Id.ToString(),
                        node.StableConstraintKey));
            }

            if (!WorldSolveDigest.IsLowerHexSha256(input.Map14PhaseExitDigest))
                failures.Add(new WorldSolveFailure(
                    WorldSolveFailureCode.MissingMap14Handoff,
                    "MAP14_PHASE_EXIT",
                    input.Map14PhaseExitDigest));

            var ids = new HashSet<WorldSectorId>(input.Nodes.Select(value => value.Id));
            foreach (var edge in input.Dependencies)
            {
                if (edge.FromSector == edge.ToSector)
                    failures.Add(new WorldSolveFailure(
                        WorldSolveFailureCode.SelfDependency,
                        edge.FromSector.ToString(),
                        edge.Kind.ToString()));
                if (!ids.Contains(edge.FromSector) || !ids.Contains(edge.ToSector))
                    failures.Add(new WorldSolveFailure(
                        WorldSolveFailureCode.MissingDependencySector,
                        edge.ToString(),
                        "Dependency endpoint is not in the 169-sector plan."));
                if (string.IsNullOrEmpty(edge.Reason) || string.IsNullOrEmpty(edge.SourceOwner))
                    failures.Add(new WorldSolveFailure(
                        WorldSolveFailureCode.InvalidNodeFact,
                        edge.ToString(),
                        "Dependency reason and source owner are required."));
            }

            foreach (var group in input.Dependencies.GroupBy(value => value).Where(value => value.Count() != 1))
                failures.Add(new WorldSolveFailure(
                    WorldSolveFailureCode.DuplicateDependency,
                    group.Key.ToString(),
                    group.Count().ToString(CultureInfo.InvariantCulture)));

            foreach (var node in input.Nodes)
            {
                if (node.HasSpecialReservation && !HasIncoming(input, node.Id, WorldDependencyKind.SpecialReservation))
                    MissingRequired(failures, node, WorldDependencyKind.SpecialReservation);
                if (node.IsMandatoryRoute && !node.IsWorldStart && !HasIncoming(input, node.Id, WorldDependencyKind.MandatoryRoute))
                    MissingRequired(failures, node, WorldDependencyKind.MandatoryRoute);
                if (node.IsBoundaryPair && !HasIncoming(input, node.Id, WorldDependencyKind.BoundaryPair))
                    MissingRequired(failures, node, WorldDependencyKind.BoundaryPair);
            }

            if (input.RetryEnvelope == null ||
                input.RetryEnvelope.MaxSectorLocalAttemptsPerNode < 1 ||
                input.RetryEnvelope.DependencyRollbackRadius < 0 ||
                input.RetryEnvelope.DependencyRollbackRadius >= WorldPlanInput.SectorCount ||
                input.RetryEnvelope.AbortReason == WorldSolveAbortReason.None ||
                input.RetryEnvelope.NewRngDrawCount != NewRngDrawCount ||
                input.RetryEnvelope.FallbackCarveCount != 0)
                failures.Add(new WorldSolveFailure(
                    WorldSolveFailureCode.InvalidRetryEnvelope,
                    "RETRY_ENVELOPE",
                    "Retry must remain sector-local, typed, zero-draw, and carve-free."));
            if (input.RetryEnvelope != null && input.RetryEnvelope.RequiresWholeWorldRerandom)
                failures.Add(new WorldSolveFailure(
                    WorldSolveFailureCode.WholeWorldRerandomRequired,
                    "RETRY_ENVELOPE",
                    "Whole-world rerandom is forbidden."));

            if (input.GeneratedFileWriteCount != 0 || input.TilemapMutationCount != 0 ||
                input.SceneMutationCount != 0 || input.PrefabMutationCount != 0 ||
                input.GameObjectMutationCount != 0 || input.GameplaySpawnCount != 0 ||
                input.SectorPlannerMutationCount != 0)
                failures.Add(new WorldSolveFailure(
                    WorldSolveFailureCode.MutationClaim,
                    "WORLD_PLAN",
                    "MAP15_01 is an immutable in-memory solve-order contract."));

            return failures.Distinct().OrderBy(value => value).ToList();
        }

        private static bool HasIncoming(WorldPlanInput input, WorldSectorId id, WorldDependencyKind kind)
        {
            return input.Dependencies.Any(value => value.ToSector == id && value.Kind == kind);
        }

        private static void MissingRequired(
            ICollection<WorldSolveFailure> failures,
            WorldSectorNode node,
            WorldDependencyKind kind)
        {
            failures.Add(new WorldSolveFailure(
                WorldSolveFailureCode.MissingRequiredDependency,
                node.Id.ToString(),
                kind.ToString()));
        }

        private static int CompareReady(
            WorldSectorNode left,
            WorldSectorNode right,
            IReadOnlyDictionary<WorldSectorId, int> dependencyCounts)
        {
            var comparison = Priority(left).CompareTo(Priority(right));
            if (comparison != 0) return comparison;
            comparison = dependencyCounts[right.Id].CompareTo(dependencyCounts[left.Id]);
            if (comparison != 0) return comparison;
            comparison = string.Compare(left.StableConstraintKey, right.StableConstraintKey, StringComparison.Ordinal);
            return comparison != 0 ? comparison : left.Id.CompareTo(right.Id);
        }
    }
}
