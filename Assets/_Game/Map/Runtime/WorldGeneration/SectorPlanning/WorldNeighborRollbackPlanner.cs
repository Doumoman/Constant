using System;
using System.Collections.Generic;
using System.Linq;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class WorldNeighborRollbackPlanner
    {
        public const string ReferencePublicationLabel = "REFERENCE NEIGHBOR ROLLBACK REPORT";

        public static WorldNeighborRollbackResult Plan(WorldRollbackPolicyRequest request)
        {
            var failures = new List<WorldNeighborRollbackFailure>();
            if (request == null)
            {
                Add(failures, WorldNeighborRollbackFailureCode.MissingRequest,
                    "request", "Rollback policy request is required.");
                return WorldNeighborRollbackResult.Fail(failures);
            }

            ValidateAuthorities(request, failures);
            ValidatePolicyCounters(request, failures);
            if (failures.Count != 0) return WorldNeighborRollbackResult.Fail(failures);

            var worldPlan = request.SolveOrder.Input;
            var nodes = worldPlan.Nodes.ToArray();
            var steps = request.SolveOrder.Steps.ToArray();
            if (nodes.GroupBy(value => value.Id).Any(group => group.Count() != 1) ||
                nodes.GroupBy(value => value.Coordinate).Any(group => group.Count() != 1) ||
                steps.GroupBy(value => value.SectorId).Any(group => group.Count() != 1))
            {
                Add(failures, WorldNeighborRollbackFailureCode.InvalidAuthorityLink,
                    "world", "World nodes, coordinates, and solve steps must be unique.");
                return WorldNeighborRollbackResult.Fail(failures);
            }

            var nodeById = nodes.ToDictionary(value => value.Id);
            var nodeByCoordinate = nodes.ToDictionary(value => value.Coordinate);
            var stepById = steps.ToDictionary(value => value.SectorId);
            ValidateFailedSector(request.FailedSectorId, nodeById, failures);
            if (failures.Count != 0) return WorldNeighborRollbackResult.Fail(failures);

            var failedNode = nodeById[request.FailedSectorId];
            var scope = BuildScope(failedNode, nodeByCoordinate, stepById, failures);
            if (failures.Count != 0) return WorldNeighborRollbackResult.Fail(failures);

            ValidateContradictions(request, nodeById, stepById, failures);
            ValidateEvidenceReferences(request, failures);
            if (failures.Count != 0) return WorldNeighborRollbackResult.Fail(failures);

            var first = request.Observations.OrderBy(value => value).FirstOrDefault();
            if (request.Observations.Count != 0 && first == null)
            {
                Add(failures, WorldNeighborRollbackFailureCode.FirstContradictionSelectionFailed,
                    "observations", "A non-empty observation set must select exactly one first contradiction.");
                return WorldNeighborRollbackResult.Fail(failures);
            }

            var report = new WorldFailureReport(request.Observations, first);
            var decision = Decide(request, scope, first);
            var outputDigest = WorldNeighborRollbackDigest.ComputeOutput(request, scope, report, decision);
            var plan = new WorldNeighborRollbackPlan(request, scope, report, decision, outputDigest);
            return WorldNeighborRollbackResult.Pass(plan);
        }

        public static string PacingBudgetEvidenceId(WorldSectorId sectorId) => "BUDGET/" + sectorId;

        private static void ValidateAuthorities(
            WorldRollbackPolicyRequest request,
            ICollection<WorldNeighborRollbackFailure> failures)
        {
            if (request.SolveOrder == null)
                Add(failures, WorldNeighborRollbackFailureCode.MissingWorldSolveOrder,
                    "solve-order", "MAP15_01 solve order is required.");
            else if (!request.SolveOrder.Success)
                Add(failures, WorldNeighborRollbackFailureCode.FailedWorldSolveOrder,
                    "solve-order", "MAP15_01 solve order must be successful.");

            if (request.IntersectorPlan == null)
                Add(failures, WorldNeighborRollbackFailureCode.MissingIntersectorPlan,
                    "intersector", "MAP15_02 intersector edge plan is required.");
            if (request.ReservationPlan == null)
                Add(failures, WorldNeighborRollbackFailureCode.MissingReservationPlan,
                    "reservation", "MAP15_03 reservation plan is required.");
            if (request.PacingDensityPlan == null)
                Add(failures, WorldNeighborRollbackFailureCode.MissingPacingDensityPlan,
                    "pacing-density", "MAP15_04 pacing-density plan is required.");
            if (failures.Count != 0) return;

            var worldPlan = request.SolveOrder.Input;
            if (worldPlan == null || worldPlan.Nodes.Count != WorldNeighborRollbackPlan.WorldSectorCount ||
                request.SolveOrder.Steps.Count != WorldNeighborRollbackPlan.WorldSectorCount)
                Add(failures, WorldNeighborRollbackFailureCode.InvalidWorldSectorCount,
                    "world", "World plan and solve order must both expose exactly 169 sectors.");
            if (request.IntersectorPlan.Edges.Count != WorldNeighborRollbackPlan.InternalEdgeCount)
                Add(failures, WorldNeighborRollbackFailureCode.InvalidInternalEdgeCount,
                    "intersector", "Intersector plan must expose exactly 312 internal edges.");
            if (worldPlan == null) return;

            var linksValid = request.IntersectorPlan.Request != null &&
                             request.IntersectorPlan.Request.WorldPlan != null &&
                             request.IntersectorPlan.Request.SolveOrder != null &&
                             request.ReservationPlan.Request != null &&
                             request.PacingDensityPlan.Request != null &&
                             EqualDigest(worldPlan.CanonicalDigest,
                                 request.IntersectorPlan.Request.WorldPlan.CanonicalDigest) &&
                             EqualDigest(request.SolveOrder.OutputDigest,
                                 request.IntersectorPlan.Request.SolveOrder.OutputDigest) &&
                             EqualDigest(worldPlan.CanonicalDigest,
                                 request.ReservationPlan.Request.WorldPlan.CanonicalDigest) &&
                             EqualDigest(request.SolveOrder.OutputDigest,
                                 request.ReservationPlan.Request.SolveOrder.OutputDigest) &&
                             EqualDigest(request.IntersectorPlan.OutputDigest,
                                 request.ReservationPlan.Request.IntersectorPlan.OutputDigest) &&
                             EqualDigest(worldPlan.CanonicalDigest,
                                 request.PacingDensityPlan.Request.WorldPlan.CanonicalDigest) &&
                             EqualDigest(request.SolveOrder.OutputDigest,
                                 request.PacingDensityPlan.Request.SolveOrder.OutputDigest) &&
                             EqualDigest(request.IntersectorPlan.OutputDigest,
                                 request.PacingDensityPlan.Request.IntersectorPlan.OutputDigest) &&
                             EqualDigest(request.ReservationPlan.OutputDigest,
                                 request.PacingDensityPlan.Request.ReservationPlan.OutputDigest);
            if (!linksValid)
                Add(failures, WorldNeighborRollbackFailureCode.InvalidAuthorityLink,
                    "authority-chain", "MAP15_01 through MAP15_04 public identities must form one authority chain.");

            var digests = new[]
            {
                worldPlan.CanonicalDigest,
                request.SolveOrder.OutputDigest,
                request.IntersectorPlan.InputDigest,
                request.IntersectorPlan.OutputDigest,
                request.ReservationPlan.InputDigest,
                request.ReservationPlan.OutputDigest,
                request.PacingDensityPlan.InputDigest,
                request.PacingDensityPlan.OutputDigest,
                request.Map14DebugRetryIdentity,
            };
            if (digests.Any(value => !IsLowerHexSha256(value)))
                Add(failures, WorldNeighborRollbackFailureCode.InvalidDigest,
                    "digest", "Every MAP14/MAP15 public identity must be a 64-character lowercase SHA-256 digest.");

            if (!EqualDigest(request.Map14DebugRetryIdentity, worldPlan.Map14PhaseExitDigest) ||
                !EqualDigest(request.Map14DebugRetryIdentity,
                    request.IntersectorPlan.Request.Map14HandoffDigest) ||
                !EqualDigest(request.Map14DebugRetryIdentity,
                    request.ReservationPlan.Request.Map14HandoffDigest) ||
                !EqualDigest(request.Map14DebugRetryIdentity,
                    request.PacingDensityPlan.Request.Map14HandoffDigest))
                Add(failures, WorldNeighborRollbackFailureCode.InvalidAuthorityLink,
                    "MAP14", "MAP14 retry/debug identity must match every installed public handoff.");

            if (string.IsNullOrWhiteSpace(request.PublicationLabel) || request.RetryAttemptCount < 0 ||
                request.RetryCap <= 0)
                Add(failures, WorldNeighborRollbackFailureCode.InvalidContradiction,
                    "policy", "Publication label, retry attempt, and positive retry cap are required.");
            if (request.PublicRetryLabels.Any(string.IsNullOrWhiteSpace))
                Add(failures, WorldNeighborRollbackFailureCode.UnknownRetryEvidence,
                    "retry-label", "Public MAP14 retry labels cannot be empty.");
        }

        private static void ValidatePolicyCounters(
            WorldRollbackPolicyRequest request,
            ICollection<WorldNeighborRollbackFailure> failures)
        {
            if (request.WholeWorldRerandomCount != 0 ||
                (request.SolveOrder != null && request.SolveOrder.WholeWorldRerandom))
                Add(failures, WorldNeighborRollbackFailureCode.WholeWorldRerandomForbidden,
                    "whole-world-rerandom", "Whole-world rerandom is forbidden.");
            if (request.FallbackCarveCount != 0 ||
                (request.SolveOrder != null && request.SolveOrder.FallbackCarveCount != 0) ||
                (request.IntersectorPlan != null && request.IntersectorPlan.FallbackCarveCount != 0) ||
                (request.ReservationPlan != null && request.ReservationPlan.FallbackCarveCount != 0) ||
                (request.PacingDensityPlan != null && request.PacingDensityPlan.FallbackCarveCount != 0))
                Add(failures, WorldNeighborRollbackFailureCode.FallbackCarveForbidden,
                    "fallback-carve", "Fallback corridor carve is forbidden.");
            if (request.SilentWideningCount != 0)
                Add(failures, WorldNeighborRollbackFailureCode.SilentWideningForbidden,
                    "silent-widening", "Rollback scope cannot widen silently.");
            if (request.SectorRerenderCount != 0 ||
                (request.ReservationPlan != null && request.ReservationPlan.SectorRerenderCount != 0) ||
                (request.PacingDensityPlan != null && request.PacingDensityPlan.SectorRerenderCount != 0))
                Add(failures, WorldNeighborRollbackFailureCode.SectorRerenderForbidden,
                    "sector-rerender", "This planner cannot rerender sectors.");

            var requestMutationClaim = new[]
            {
                request.NewRngDrawCount, request.GeneratedFileWriteCount, request.TilemapMutationCount,
                request.SceneMutationCount, request.PrefabMutationCount, request.GameObjectMutationCount,
                request.GameplaySpawnCount, request.AuthoringMutationCount, request.WorldPlanMutationCount,
                request.IntersectorPlanMutationCount, request.ReservationPlanMutationCount,
                request.PacingDensityPlanMutationCount,
            }.Any(value => value != 0);
            var mutationCount = request.NewRngDrawCount + request.GeneratedFileWriteCount +
                                request.TilemapMutationCount + request.SceneMutationCount +
                                request.PrefabMutationCount + request.GameObjectMutationCount +
                                request.GameplaySpawnCount + request.AuthoringMutationCount +
                                request.WorldPlanMutationCount + request.IntersectorPlanMutationCount +
                                request.ReservationPlanMutationCount + request.PacingDensityPlanMutationCount;
            if (request.SolveOrder != null) mutationCount += request.SolveOrder.NewRngDrawCount;
            if (request.IntersectorPlan != null)
                mutationCount += request.IntersectorPlan.NewRngDrawCount +
                                 request.IntersectorPlan.GeneratedFileWriteCount +
                                 request.IntersectorPlan.TilemapMutationCount + request.IntersectorPlan.SceneMutationCount +
                                 request.IntersectorPlan.PrefabMutationCount + request.IntersectorPlan.GameObjectMutationCount +
                                 request.IntersectorPlan.GameplaySpawnCount + request.IntersectorPlan.SectorPlannerMutationCount +
                                 request.IntersectorPlan.WorldPlanMutationCount;
            if (request.ReservationPlan != null)
                mutationCount += request.ReservationPlan.NewRngDrawCount +
                                 request.ReservationPlan.GeneratedFileWriteCount +
                                 request.ReservationPlan.TilemapMutationCount + request.ReservationPlan.SceneMutationCount +
                                 request.ReservationPlan.PrefabMutationCount + request.ReservationPlan.GameObjectMutationCount +
                                 request.ReservationPlan.GameplaySpawnCount + request.ReservationPlan.SpecialRegionMutationCount +
                                 request.ReservationPlan.SectorPlannerMutationCount + request.ReservationPlan.WorldPlanMutationCount +
                                 request.ReservationPlan.IntersectorPlanMutationCount;
            if (request.PacingDensityPlan != null)
                mutationCount += request.PacingDensityPlan.NewRngDrawCount +
                                 request.PacingDensityPlan.GeneratedFileWriteCount +
                                 request.PacingDensityPlan.TilemapMutationCount + request.PacingDensityPlan.SceneMutationCount +
                                 request.PacingDensityPlan.PrefabMutationCount + request.PacingDensityPlan.GameObjectMutationCount +
                                 request.PacingDensityPlan.GameplaySpawnCount + request.PacingDensityPlan.AuthoringMutationCount +
                                 request.PacingDensityPlan.WorldPlanMutationCount +
                                 request.PacingDensityPlan.IntersectorPlanMutationCount +
                                 request.PacingDensityPlan.ReservationPlanMutationCount;
            if (requestMutationClaim || mutationCount != 0)
                Add(failures, WorldNeighborRollbackFailureCode.MutationClaim,
                    "mutation", "Rollback reporting must not draw RNG, write assets, spawn gameplay, or mutate upstream plans.");
        }

        private static void ValidateFailedSector(
            WorldSectorId failedSectorId,
            IReadOnlyDictionary<WorldSectorId, WorldSectorNode> nodes,
            ICollection<WorldNeighborRollbackFailure> failures)
        {
            if (failedSectorId.Value < 0 || failedSectorId.Value >= WorldNeighborRollbackPlan.WorldSectorCount)
                Add(failures, WorldNeighborRollbackFailureCode.FailedSectorOutOfBounds,
                    failedSectorId.ToString(), "Failed sector must be inside the 13x13 world.");
            else if (!nodes.ContainsKey(failedSectorId))
                Add(failures, WorldNeighborRollbackFailureCode.MissingFailedSector,
                    failedSectorId.ToString(), "Failed sector is missing from the world plan.");
        }

        private static WorldRollbackScope BuildScope(
            WorldSectorNode failedNode,
            IReadOnlyDictionary<WorldSectorCoordinate, WorldSectorNode> nodes,
            IReadOnlyDictionary<WorldSectorId, WorldSolveStep> steps,
            ICollection<WorldNeighborRollbackFailure> failures)
        {
            if (!failedNode.Coordinate.IsInBounds)
            {
                Add(failures, WorldNeighborRollbackFailureCode.FailedSectorOutOfBounds,
                    failedNode.Id.ToString(), "Failed sector coordinate is outside the world.");
                return null;
            }

            var scopeSectors = new List<WorldRollbackSector>();
            for (var dy = -WorldRollbackScope.Radius; dy <= WorldRollbackScope.Radius; dy++)
            for (var dx = -WorldRollbackScope.Radius; dx <= WorldRollbackScope.Radius; dx++)
            {
                var coordinate = new WorldSectorCoordinate(
                    failedNode.Coordinate.X + dx, failedNode.Coordinate.Y + dy);
                if (!coordinate.IsInBounds) continue;
                WorldSectorNode node;
                WorldSolveStep step;
                if (!nodes.TryGetValue(coordinate, out node) || !steps.TryGetValue(
                        node == null ? default(WorldSectorId) : node.Id, out step))
                {
                    Add(failures, WorldNeighborRollbackFailureCode.MissingScopeSector,
                        coordinate.ToString(), "Every in-bounds Moore 1-ring coordinate must have a node and solve step.");
                    continue;
                }
                scopeSectors.Add(new WorldRollbackSector(
                    node.Id, coordinate, step.StepIndex, node.Id == failedNode.Id));
            }

            if (scopeSectors.Count > WorldRollbackScope.MaximumSectorCount)
                Add(failures, WorldNeighborRollbackFailureCode.ScopeExceedsLimit,
                    failedNode.Id.ToString(), "Rollback scope cannot exceed nine sectors.");
            var kind = ScopeKind(failedNode.Coordinate);
            var scope = new WorldRollbackScope(kind, failedNode.Id, failedNode.Coordinate, scopeSectors);
            if (scope.SectorCount != scope.ExpectedSectorCount || !scope.ContainsFailedSector)
                Add(failures, WorldNeighborRollbackFailureCode.MissingScopeSector,
                    failedNode.Id.ToString(), "Rollback scope must contain the failed sector and the complete in-bounds 1-ring.");
            return scope;
        }

        private static void ValidateContradictions(
            WorldRollbackPolicyRequest request,
            IReadOnlyDictionary<WorldSectorId, WorldSectorNode> nodes,
            IReadOnlyDictionary<WorldSectorId, WorldSolveStep> steps,
            ICollection<WorldNeighborRollbackFailure> failures)
        {
            if (request.NullObservationCount != 0)
                Add(failures, WorldNeighborRollbackFailureCode.InvalidContradiction,
                    "observations", "Contradiction observations cannot contain null entries.");
            if (request.Observations.GroupBy(value => value.StableContradictionId, StringComparer.Ordinal)
                .Any(group => group.Count() != 1))
                Add(failures, WorldNeighborRollbackFailureCode.InvalidContradiction,
                    "stable-id", "Contradiction stable ids must be unique.");

            foreach (var observation in request.Observations)
            {
                if (string.IsNullOrWhiteSpace(observation.StableContradictionId) ||
                    !Enum.IsDefined(typeof(WorldContradictionKind), observation.Kind) ||
                    !Enum.IsDefined(typeof(WorldContradictionSource), observation.Source))
                    Add(failures, WorldNeighborRollbackFailureCode.InvalidContradiction,
                        observation.StableContradictionId, "Contradiction id, kind, and source must be valid.");
                WorldSolveStep step;
                if (!nodes.ContainsKey(observation.SectorId) || !steps.TryGetValue(observation.SectorId, out step))
                    Add(failures, WorldNeighborRollbackFailureCode.ContradictionMissingSector,
                        observation.StableContradictionId, "Contradiction sector must exist in MAP15_01.");
                else if (step.StepIndex != observation.SolveStepIndex)
                    Add(failures, WorldNeighborRollbackFailureCode.InvalidContradiction,
                        observation.StableContradictionId, "Contradiction solve step must match MAP15_01.");
            }
        }

        private static void ValidateEvidenceReferences(
            WorldRollbackPolicyRequest request,
            ICollection<WorldNeighborRollbackFailure> failures)
        {
            var validEdges = new HashSet<WorldIntersectorEdgeId>(request.IntersectorPlan.Edges.Select(value => value.Id));
            var validReservations = new HashSet<string>(StringComparer.Ordinal);
            validReservations.UnionWith(request.ReservationPlan.Transactions.Select(value => value.TransactionId));
            validReservations.UnionWith(request.ReservationPlan.Claims.Select(value => value.ClaimId));
            validReservations.UnionWith(request.ReservationPlan.EdgeLocks.Select(value => value.LockId));

            var validPacing = new HashSet<string>(StringComparer.Ordinal);
            validPacing.UnionWith(request.PacingDensityPlan.Windows.Select(value => value.WindowId));
            validPacing.UnionWith(request.PacingDensityPlan.Budgets.Select(value => PacingBudgetEvidenceId(value.SectorId)));
            validPacing.UnionWith(request.PacingDensityPlan.Observations.Select(value => value.ObservationId));
            validPacing.UnionWith(request.PacingDensityPlan.Request.ActivityEventConstraints
                .Select(value => value.ConstraintId));

            var validCandidates = new HashSet<string>(
                request.PacingDensityPlan.Signatures.Select(value => value.SignatureId), StringComparer.Ordinal);
            validCandidates.UnionWith(request.ReservationPlan.ClusterPolicies.Select(value => value.ClusterId.Value));
            validCandidates.UnionWith(request.ReservationPlan.ClusterPolicies.Select(value => value.VariantId.Value));
            validCandidates.UnionWith(request.ReservationPlan.CrossSectorAllowances.Select(value => value.ClusterId.Value));
            validCandidates.UnionWith(request.ReservationPlan.CrossSectorAllowances.Select(value => value.VariantId.Value));
            var validRetries = new HashSet<string>(request.PublicRetryLabels, StringComparer.Ordinal);

            foreach (var observation in request.Observations)
            {
                if (observation.RelatedEdgeIds.Any(value => !validEdges.Contains(value)))
                    Add(failures, WorldNeighborRollbackFailureCode.UnknownEdgeEvidence,
                        observation.StableContradictionId, "Edge evidence must exist in MAP15_02.");
                if (observation.RelatedReservationIds.Any(value => string.IsNullOrWhiteSpace(value) ||
                                                                  !validReservations.Contains(value)))
                    Add(failures, WorldNeighborRollbackFailureCode.UnknownReservationEvidence,
                        observation.StableContradictionId, "Reservation evidence must exist in MAP15_03.");
                if (observation.RelatedPacingEvidenceIds.Any(value => string.IsNullOrWhiteSpace(value) ||
                                                                    !validPacing.Contains(value)))
                    Add(failures, WorldNeighborRollbackFailureCode.UnknownPacingEvidence,
                        observation.StableContradictionId, "Pacing evidence must exist in MAP15_04.");
                if (observation.RelatedCandidateIds.Any(value => string.IsNullOrWhiteSpace(value) ||
                                                               !validCandidates.Contains(value)))
                    Add(failures, WorldNeighborRollbackFailureCode.UnknownCandidateEvidence,
                        observation.StableContradictionId, "Candidate evidence must exist in public MAP15_03/04 values.");
                if (observation.RetryLabels.Any(value => string.IsNullOrWhiteSpace(value) ||
                                                       !validRetries.Contains(value)))
                    Add(failures, WorldNeighborRollbackFailureCode.UnknownRetryEvidence,
                        observation.StableContradictionId, "Retry evidence must exist in the MAP14 public projection.");
            }
        }

        private static WorldRollbackDecision Decide(
            WorldRollbackPolicyRequest request,
            WorldRollbackScope scope,
            WorldContradictionEvidence first)
        {
            if (first == null)
                return new WorldRollbackDecision(WorldRollbackDecisionKind.Abort,
                    "NO_CONTRADICTION_OBSERVATION", request.RetryAttemptCount, request.RetryCap);
            if (first.RequiresUpstreamOwnerRepair)
                return new WorldRollbackDecision(WorldRollbackDecisionKind.BlockedOwner,
                    "UPSTREAM_OWNER_REPAIR_REQUIRED", request.RetryAttemptCount, request.RetryCap);
            if (request.RetryAttemptCount >= request.RetryCap)
                return new WorldRollbackDecision(WorldRollbackDecisionKind.Abort,
                    "RETRY_CAP_EXHAUSTED", request.RetryAttemptCount, request.RetryCap);
            if (!first.RetryableWithinScope || !scope.Sectors.Any(value => value.SectorId == first.SectorId))
                return new WorldRollbackDecision(WorldRollbackDecisionKind.Abort,
                    "CONTRADICTION_OUTSIDE_BOUNDED_SCOPE", request.RetryAttemptCount, request.RetryCap);
            return new WorldRollbackDecision(WorldRollbackDecisionKind.BoundedRetry,
                "FAILED_SECTOR_PLUS_IN_BOUNDS_MOORE_ONE_RING", request.RetryAttemptCount, request.RetryCap);
        }

        private static WorldRollbackScopeKind ScopeKind(WorldSectorCoordinate coordinate)
        {
            var xEdge = coordinate.X == 0 || coordinate.X == WorldPlanInput.SectorColumns - 1;
            var yEdge = coordinate.Y == 0 || coordinate.Y == WorldPlanInput.SectorRows - 1;
            return xEdge && yEdge
                ? WorldRollbackScopeKind.Corner
                : xEdge || yEdge ? WorldRollbackScopeKind.Edge : WorldRollbackScopeKind.Interior;
        }

        private static bool EqualDigest(string left, string right) =>
            string.Equals(left, right, StringComparison.Ordinal);

        private static bool IsLowerHexSha256(string value) =>
            value != null && value.Length == 64 && value.All(character =>
                (character >= '0' && character <= '9') || (character >= 'a' && character <= 'f'));

        private static void Add(
            ICollection<WorldNeighborRollbackFailure> failures,
            WorldNeighborRollbackFailureCode code,
            string subject,
            string reason) => failures.Add(new WorldNeighborRollbackFailure(code, subject, reason));
    }
}
