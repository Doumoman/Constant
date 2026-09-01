using System;
using System.Collections.Generic;
using System.Linq;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public static class WorldPacingDensityPlanner
    {
        public const string ReferencePublicationLabel = "REFERENCE WORLD PACING DENSITY PLAN";
        public const int NewRngDrawCount = 0;

        private static readonly WorldPacingWindowKind[] RequiredWindowKinds =
        {
            WorldPacingWindowKind.Quiet,
            WorldPacingWindowKind.Cluster,
            WorldPacingWindowKind.Activity,
            WorldPacingWindowKind.Event,
            WorldPacingWindowKind.Landmark,
        };

        private static readonly WorldContentSignatureKind[] RequiredSignatureKinds =
        {
            WorldContentSignatureKind.Pattern,
            WorldContentSignatureKind.Cluster,
            WorldContentSignatureKind.Activity,
        };

        public static WorldPacingDensityResult Plan(WorldPacingDensityRequest request)
        {
            var failures = new List<WorldPacingDensityFailure>();
            ValidateUpstream(request, failures);
            if (request == null) return WorldPacingDensityResult.Fail(failures);

            Dictionary<WorldSectorId, WorldSectorNode> nodes;
            Dictionary<WorldSectorId, WorldSolveStep> steps;
            if (request.WorldPlan == null || request.SolveOrder == null)
            {
                nodes = new Dictionary<WorldSectorId, WorldSectorNode>();
                steps = new Dictionary<WorldSectorId, WorldSolveStep>();
            }
            else
            {
                nodes = request.WorldPlan.Nodes.GroupBy(value => value.Id)
                    .ToDictionary(group => group.Key, group => group.First());
                steps = request.SolveOrder.Steps.GroupBy(value => value.SectorId)
                    .ToDictionary(group => group.Key, group => group.First());
            }

            ValidateWindows(request, nodes, steps, failures);
            ValidateBudgets(request, nodes, failures);
            ValidateSignatures(request, nodes, steps, failures);
            var rules = ValidateRecentUseRules(request, failures);
            var constraints = ValidateActivityEventConstraints(request, failures);
            ValidateLandmarkCoverage(request, failures);

            if (failures.Count != 0) return WorldPacingDensityResult.Fail(failures);

            var violations = new List<WorldPacingDensityViolation>();
            AddWindowViolations(request.Windows, violations);
            AddBudgetViolations(request.Budgets, violations);
            AddActivityEventCapViolations(request.Windows, constraints, violations);
            var observations = BuildRecentUseObservations(request.Signatures, rules, nodes, violations);
            var outputDigest = WorldPacingDensityDigest.ComputeOutput(request, observations, violations);
            return WorldPacingDensityResult.Pass(new WorldPacingDensityPlan(
                request, observations, violations, outputDigest));
        }

        private static void ValidateUpstream(
            WorldPacingDensityRequest request,
            ICollection<WorldPacingDensityFailure> failures)
        {
            if (request == null)
            {
                failures.Add(Failure(WorldPacingDensityFailureCode.MissingInput, "REQUEST",
                    "World pacing-density request is required."));
                return;
            }

            if (request.WorldPlan == null || request.SolveOrder == null || request.IntersectorPlan == null ||
                request.ReservationPlan == null)
            {
                failures.Add(Failure(WorldPacingDensityFailureCode.MissingInput, "UPSTREAM",
                    "MAP15_01 world/solve, MAP15_02 edge plan and MAP15_03 reservation plan are required."));
                return;
            }

            if (!request.SolveOrder.Success)
                failures.Add(Failure(WorldPacingDensityFailureCode.UpstreamFailure, "MAP15_01",
                    "World solve order must be successful."));

            if (request.WorldPlan.Nodes.Count != WorldPacingDensityPlan.WorldSectorCount ||
                request.SolveOrder.Steps.Count != WorldPacingDensityPlan.WorldSectorCount)
                failures.Add(Failure(WorldPacingDensityFailureCode.WorldSectorCountMismatch, "WORLD",
                    "World plan and solve order must each publish exactly 169 sectors."));

            if (request.IntersectorPlan.Edges.Count != WorldPacingDensityPlan.InternalEdgeCount)
                failures.Add(Failure(WorldPacingDensityFailureCode.InternalEdgeCountMismatch, "INTERSECTOR",
                    "Intersector plan must publish exactly 312 internal edges."));

            ValidateDigest(request.WorldPlan.CanonicalDigest, "MAP15_01_INPUT", failures);
            ValidateDigest(request.SolveOrder.OutputDigest, "MAP15_01_OUTPUT", failures);
            ValidateDigest(request.IntersectorPlan.InputDigest, "MAP15_02_INPUT", failures);
            ValidateDigest(request.IntersectorPlan.OutputDigest, "MAP15_02_OUTPUT", failures);
            ValidateDigest(request.ReservationPlan.InputDigest, "MAP15_03_INPUT", failures);
            ValidateDigest(request.ReservationPlan.OutputDigest, "MAP15_03_OUTPUT", failures);
            ValidateDigest(request.Map10IdentityDigest, "MAP10_IDENTITY", failures);
            ValidateDigest(request.Map11IdentityDigest, "MAP11_IDENTITY", failures);
            ValidateDigest(request.Map12IdentityDigest, "MAP12_IDENTITY", failures);
            ValidateDigest(request.Map13IdentityDigest, "MAP13_IDENTITY", failures);
            ValidateDigest(request.Map14HandoffDigest, "MAP14_HANDOFF", failures);

            if (!string.Equals(request.WorldPlan.CanonicalDigest, request.SolveOrder.InputDigest,
                    StringComparison.Ordinal) ||
                !string.Equals(request.WorldPlan.CanonicalDigest,
                    request.IntersectorPlan.Request.WorldPlan.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(request.SolveOrder.OutputDigest,
                    request.IntersectorPlan.Request.SolveOrder.OutputDigest, StringComparison.Ordinal) ||
                !string.Equals(request.WorldPlan.CanonicalDigest,
                    request.ReservationPlan.Request.WorldPlan.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(request.SolveOrder.OutputDigest,
                    request.ReservationPlan.Request.SolveOrder.OutputDigest, StringComparison.Ordinal) ||
                !string.Equals(request.IntersectorPlan.OutputDigest,
                    request.ReservationPlan.Request.IntersectorPlan.OutputDigest, StringComparison.Ordinal) ||
                !string.Equals(request.Map14HandoffDigest, request.WorldPlan.Map14PhaseExitDigest,
                    StringComparison.Ordinal) ||
                !string.Equals(request.Map14HandoffDigest, request.ReservationPlan.Request.Map14HandoffDigest,
                    StringComparison.Ordinal))
                failures.Add(Failure(WorldPacingDensityFailureCode.UpstreamDigestMismatch, "UPSTREAM_CHAIN",
                    "MAP14/MAP15 world, solve, edge and reservation identities must form one immutable chain."));

            if (string.IsNullOrWhiteSpace(request.PublicationLabel))
                failures.Add(Failure(WorldPacingDensityFailureCode.InvalidDigest, "PUBLICATION",
                    "A pacing-density publication label is required."));

            if (HasMutationClaim(request))
                failures.Add(Failure(WorldPacingDensityFailureCode.MutationClaim, "NO_MUTATION",
                    "Pacing-density planning cannot reroll, carve, rerender, write files or mutate upstream artifacts."));
        }

        private static bool HasMutationClaim(WorldPacingDensityRequest request)
        {
            var world = request.WorldPlan;
            var solve = request.SolveOrder;
            var edge = request.IntersectorPlan;
            var reservation = request.ReservationPlan;
            return request.NewRngDrawCount != 0 || request.FallbackCarveCount != 0 ||
                   request.SectorRerenderCount != 0 || request.GeneratedFileWriteCount != 0 ||
                   request.TilemapMutationCount != 0 || request.SceneMutationCount != 0 ||
                   request.PrefabMutationCount != 0 || request.GameObjectMutationCount != 0 ||
                   request.GameplaySpawnCount != 0 || request.AuthoringMutationCount != 0 ||
                   request.WorldPlanMutationCount != 0 || request.IntersectorPlanMutationCount != 0 ||
                   request.ReservationPlanMutationCount != 0 ||
                   world.GeneratedFileWriteCount != 0 || world.TilemapMutationCount != 0 ||
                   world.SceneMutationCount != 0 || world.PrefabMutationCount != 0 ||
                   world.GameObjectMutationCount != 0 || world.GameplaySpawnCount != 0 ||
                   world.SectorPlannerMutationCount != 0 || solve.NewRngDrawCount != 0 ||
                   solve.FallbackCarveCount != 0 || solve.WholeWorldRerandom ||
                   edge.NewRngDrawCount != 0 || edge.FallbackCarveCount != 0 ||
                   edge.GeneratedFileWriteCount != 0 || edge.TilemapMutationCount != 0 ||
                   edge.SceneMutationCount != 0 || edge.PrefabMutationCount != 0 ||
                   edge.GameObjectMutationCount != 0 || edge.GameplaySpawnCount != 0 ||
                   edge.SectorPlannerMutationCount != 0 || edge.WorldPlanMutationCount != 0 ||
                   reservation.NewRngDrawCount != 0 || reservation.FallbackCarveCount != 0 ||
                   reservation.SectorRerenderCount != 0 || reservation.GeneratedFileWriteCount != 0 ||
                   reservation.TilemapMutationCount != 0 || reservation.SceneMutationCount != 0 ||
                   reservation.PrefabMutationCount != 0 || reservation.GameObjectMutationCount != 0 ||
                   reservation.GameplaySpawnCount != 0 || reservation.SpecialRegionMutationCount != 0 ||
                   reservation.SectorPlannerMutationCount != 0 || reservation.WorldPlanMutationCount != 0 ||
                   reservation.IntersectorPlanMutationCount != 0;
        }

        private static void ValidateWindows(
            WorldPacingDensityRequest request,
            IReadOnlyDictionary<WorldSectorId, WorldSectorNode> nodes,
            IReadOnlyDictionary<WorldSectorId, WorldSolveStep> steps,
            ICollection<WorldPacingDensityFailure> failures)
        {
            foreach (var duplicate in request.Windows.GroupBy(value => value.WindowId, StringComparer.Ordinal)
                         .Where(group => string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1))
                failures.Add(Failure(WorldPacingDensityFailureCode.DuplicateWindowId, duplicate.Key,
                    "Window ids must be non-empty and unique."));

            foreach (var window in request.Windows)
            {
                if (!Enum.IsDefined(typeof(WorldPacingWindowKind), window.Kind) ||
                    window.SectorIds.Count == 0 || window.FirstSolveStep < 0 ||
                    window.LastSolveStep < window.FirstSolveStep ||
                    window.MinimumCount < 0 || window.MaximumCount < window.MinimumCount ||
                    window.ObservedCount < 0 || string.IsNullOrWhiteSpace(window.Reason) ||
                    string.IsNullOrWhiteSpace(window.SourceOwner))
                {
                    failures.Add(Failure(WorldPacingDensityFailureCode.InvalidWindow, window.WindowId,
                        "Window kind, sectors, solve span, count envelope, reason and owner must be valid."));
                    continue;
                }

                foreach (var sector in window.SectorIds)
                {
                    if (!nodes.ContainsKey(sector) || !steps.TryGetValue(sector, out var step))
                    {
                        failures.Add(Failure(WorldPacingDensityFailureCode.MissingSector,
                            window.WindowId + ":" + sector, "Window references a sector absent from the world solve."));
                        continue;
                    }
                    if (step.StepIndex < window.FirstSolveStep || step.StepIndex > window.LastSolveStep)
                        failures.Add(Failure(WorldPacingDensityFailureCode.InvalidWindow,
                            window.WindowId + ":" + sector,
                            "Window solve-step span must contain every referenced sector solve step."));
                }
            }

            foreach (var kind in RequiredWindowKinds.Where(kind => request.Windows.All(value => value.Kind != kind)))
                failures.Add(Failure(WorldPacingDensityFailureCode.MissingWindowKind, kind.ToString(),
                    "All five world pacing window kinds are required."));
        }

        private static void ValidateBudgets(
            WorldPacingDensityRequest request,
            IReadOnlyDictionary<WorldSectorId, WorldSectorNode> nodes,
            ICollection<WorldPacingDensityFailure> failures)
        {
            foreach (var duplicate in request.Budgets.GroupBy(value => value.SectorId)
                         .Where(group => group.Count() > 1))
                failures.Add(Failure(WorldPacingDensityFailureCode.DuplicateBudgetSector,
                    duplicate.Key.ToString(), "Each world sector must have exactly one density budget."));

            foreach (var budget in request.Budgets)
            {
                if (!nodes.ContainsKey(budget.SectorId))
                    failures.Add(Failure(WorldPacingDensityFailureCode.MissingSector, budget.SectorId.ToString(),
                        "Density budget references a sector absent from the world plan."));
                if (!Enum.IsDefined(typeof(WorldDensityBudgetKind), budget.Kind) ||
                    budget.MinimumSolidBudget < 0 ||
                    budget.MaximumSolidBudget < budget.MinimumSolidBudget ||
                    budget.ObservedSolidBudget < 0 || budget.MinimumReachableBudget < 0 ||
                    budget.MaximumReachableBudget < budget.MinimumReachableBudget ||
                    budget.ObservedReachableBudget < 0 || string.IsNullOrWhiteSpace(budget.Reason) ||
                    string.IsNullOrWhiteSpace(budget.SourceOwner))
                    failures.Add(Failure(WorldPacingDensityFailureCode.InvalidBudget, budget.SectorId.ToString(),
                        "Abstract solid/reachable budget ranges, observations, reason and owner must be valid."));
            }

            foreach (var sector in nodes.Keys.Where(sector => request.Budgets.All(value => value.SectorId != sector)))
                failures.Add(Failure(WorldPacingDensityFailureCode.MissingBudgetSector, sector.ToString(),
                    "Every non-deferred world sector requires one abstract density budget."));
        }

        private static void ValidateSignatures(
            WorldPacingDensityRequest request,
            IReadOnlyDictionary<WorldSectorId, WorldSectorNode> nodes,
            IReadOnlyDictionary<WorldSectorId, WorldSolveStep> steps,
            ICollection<WorldPacingDensityFailure> failures)
        {
            foreach (var signature in request.Signatures)
            {
                if (!Enum.IsDefined(typeof(WorldContentSignatureKind), signature.Kind) ||
                    string.IsNullOrWhiteSpace(signature.SignatureId) ||
                    string.IsNullOrWhiteSpace(signature.SourceOwner) || !nodes.ContainsKey(signature.SectorId) ||
                    !steps.TryGetValue(signature.SectorId, out var step) || step.StepIndex != signature.SolveStep)
                    failures.Add(Failure(WorldPacingDensityFailureCode.InvalidSignature,
                        signature.Kind + ":" + signature.SignatureId,
                        "Signature identity, sector, solve step and source owner must match the world solve."));
            }

            foreach (var kind in RequiredSignatureKinds.Where(kind => request.Signatures.All(value => value.Kind != kind)))
                failures.Add(Failure(WorldPacingDensityFailureCode.InvalidSignature, kind.ToString(),
                    "Pattern, Cluster and Activity signature evidence are all required."));
        }

        private static Dictionary<WorldContentSignatureKind, WorldRecentUseRule> ValidateRecentUseRules(
            WorldPacingDensityRequest request,
            ICollection<WorldPacingDensityFailure> failures)
        {
            var result = new Dictionary<WorldContentSignatureKind, WorldRecentUseRule>();
            foreach (var group in request.RecentUseRules.GroupBy(value => value.Kind))
            {
                if (group.Count() != 1)
                {
                    failures.Add(Failure(WorldPacingDensityFailureCode.DuplicateRecentUseRule,
                        group.Key.ToString(), "Exactly one recent-use rule is allowed per signature kind."));
                    continue;
                }
                var rule = group.Single();
                if (!Enum.IsDefined(typeof(WorldContentSignatureKind), rule.Kind) ||
                    rule.MinimumSectorDistance <= 0 || rule.MinimumSolveStepDistance <= 0 ||
                    string.IsNullOrWhiteSpace(rule.Reason) || string.IsNullOrWhiteSpace(rule.SourceOwner))
                {
                    failures.Add(Failure(WorldPacingDensityFailureCode.InvalidRecentUseRule, rule.Kind.ToString(),
                        "Recent-use distances must be positive and provenance must be present."));
                    continue;
                }
                result.Add(rule.Kind, rule);
            }

            foreach (var kind in RequiredSignatureKinds.Where(kind => !result.ContainsKey(kind)))
                failures.Add(Failure(WorldPacingDensityFailureCode.MissingRecentUseRule, kind.ToString(),
                    "Pattern, Cluster and Activity recent-use rules are required."));
            return result;
        }

        private static Dictionary<WorldPacingWindowKind, WorldActivityEventConstraint>
            ValidateActivityEventConstraints(
                WorldPacingDensityRequest request,
                ICollection<WorldPacingDensityFailure> failures)
        {
            var result = new Dictionary<WorldPacingWindowKind, WorldActivityEventConstraint>();
            foreach (var group in request.ActivityEventConstraints.GroupBy(value => value.Kind))
            {
                if (group.Count() != 1 ||
                    (group.Key != WorldPacingWindowKind.Activity && group.Key != WorldPacingWindowKind.Event))
                {
                    failures.Add(Failure(WorldPacingDensityFailureCode.ActivityEventAuthorityContradiction,
                        group.Key.ToString(), "Exactly one Activity and one Event authority constraint are required."));
                    continue;
                }
                var constraint = group.Single();
                if (string.IsNullOrWhiteSpace(constraint.ConstraintId) || constraint.TargetPermille <= 0 ||
                    constraint.TargetPermille > 1000 || constraint.MaximumCount < 0 ||
                    !WorldSolveDigest.IsLowerHexSha256(constraint.AuthorityDigest) ||
                    string.IsNullOrWhiteSpace(constraint.SourceOwner))
                {
                    failures.Add(Failure(WorldPacingDensityFailureCode.ActivityEventAuthorityContradiction,
                        constraint.ConstraintId, "Activity/Event frequency and cap projection is invalid."));
                    continue;
                }
                result.Add(group.Key, constraint);
            }

            foreach (var kind in new[] { WorldPacingWindowKind.Activity, WorldPacingWindowKind.Event })
            {
                if (!result.TryGetValue(kind, out var constraint))
                {
                    failures.Add(Failure(WorldPacingDensityFailureCode.ActivityEventAuthorityContradiction,
                        kind.ToString(), "MAP12 Activity/Event authority projection is required."));
                    continue;
                }
                foreach (var window in request.Windows.Where(value => value.Kind == kind &&
                                                                      value.MaximumCount > constraint.MaximumCount))
                    failures.Add(Failure(WorldPacingDensityFailureCode.ActivityEventAuthorityContradiction,
                        window.WindowId, "Window maximum contradicts the MAP12-derived cap."));
            }
            return result;
        }

        private static void ValidateLandmarkCoverage(
            WorldPacingDensityRequest request,
            ICollection<WorldPacingDensityFailure> failures)
        {
            if (request.ReservationPlan == null) return;
            var landmarkSectors = new HashSet<WorldSectorId>(request.Windows
                .Where(value => value.Kind == WorldPacingWindowKind.Landmark)
                .SelectMany(value => value.SectorIds));
            foreach (var sector in request.ReservationPlan.Transactions.Where(value => !value.IsDeferred)
                         .SelectMany(value => value.SectorIds).Distinct().Where(value => !landmarkSectors.Contains(value)))
                failures.Add(Failure(WorldPacingDensityFailureCode.InvalidWindow, sector.ToString(),
                    "Landmark windows must preserve every fixed MAP15_03 Special reservation sector."));
        }

        private static void AddWindowViolations(
            IEnumerable<WorldPacingWindow> windows,
            ICollection<WorldPacingDensityViolation> violations)
        {
            foreach (var window in windows)
            {
                if (window.ObservedCount < window.MinimumCount)
                    violations.Add(new WorldPacingDensityViolation(
                        WorldPacingDensityViolationType.WindowUnderfilled, window.WindowId, null, string.Empty,
                        "Observed content count is below the pacing window minimum."));
                if (window.ObservedCount > window.MaximumCount)
                    violations.Add(new WorldPacingDensityViolation(
                        WorldPacingDensityViolationType.WindowOverfilled, window.WindowId, null, string.Empty,
                        "Observed content count exceeds the pacing window maximum."));
            }
        }

        private static void AddBudgetViolations(
            IEnumerable<WorldSectorDensityBudget> budgets,
            ICollection<WorldPacingDensityViolation> violations)
        {
            foreach (var budget in budgets)
            {
                if (budget.ObservedSolidBudget < budget.MinimumSolidBudget)
                    violations.Add(new WorldPacingDensityViolation(
                        WorldPacingDensityViolationType.DensityBelowMinimum, budget.SectorId.ToString(),
                        budget.SectorId, string.Empty, "Observed abstract solid budget is below minimum."));
                if (budget.ObservedSolidBudget > budget.MaximumSolidBudget)
                    violations.Add(new WorldPacingDensityViolation(
                        WorldPacingDensityViolationType.DensityAboveMaximum, budget.SectorId.ToString(),
                        budget.SectorId, string.Empty, "Observed abstract solid budget exceeds maximum."));
                if (budget.ObservedReachableBudget < budget.MinimumReachableBudget)
                    violations.Add(new WorldPacingDensityViolation(
                        WorldPacingDensityViolationType.ReachableBudgetBelowMinimum, budget.SectorId.ToString(),
                        budget.SectorId, string.Empty, "Observed abstract reachable budget is below minimum."));
                if (budget.ObservedReachableBudget > budget.MaximumReachableBudget)
                    violations.Add(new WorldPacingDensityViolation(
                        WorldPacingDensityViolationType.ReachableBudgetAboveMaximum, budget.SectorId.ToString(),
                        budget.SectorId, string.Empty, "Observed abstract reachable budget exceeds maximum."));
            }
        }

        private static void AddActivityEventCapViolations(
            IEnumerable<WorldPacingWindow> windows,
            IReadOnlyDictionary<WorldPacingWindowKind, WorldActivityEventConstraint> constraints,
            ICollection<WorldPacingDensityViolation> violations)
        {
            foreach (var window in windows.Where(value => value.Kind == WorldPacingWindowKind.Activity ||
                                                          value.Kind == WorldPacingWindowKind.Event))
            {
                var constraint = constraints[window.Kind];
                if (window.ObservedCount <= constraint.MaximumCount) continue;
                violations.Add(new WorldPacingDensityViolation(
                    window.Kind == WorldPacingWindowKind.Activity
                        ? WorldPacingDensityViolationType.ActivityCapExceeded
                        : WorldPacingDensityViolationType.EventCapExceeded,
                    window.WindowId, null, string.Empty,
                    "Observed count exceeds the MAP12-derived abstract cap " + constraint.MaximumCount + "."));
            }
        }

        private static WorldRecentUseObservation[] BuildRecentUseObservations(
            IEnumerable<WorldContentSignature> signatures,
            IReadOnlyDictionary<WorldContentSignatureKind, WorldRecentUseRule> rules,
            IReadOnlyDictionary<WorldSectorId, WorldSectorNode> nodes,
            ICollection<WorldPacingDensityViolation> violations)
        {
            var result = new List<WorldRecentUseObservation>();
            foreach (var kind in RequiredSignatureKinds)
            {
                var ordered = signatures.Where(value => value.Kind == kind)
                    .OrderBy(value => value.SolveStep).ThenBy(value => value.SectorId)
                    .ThenBy(value => value.SignatureId, StringComparer.Ordinal).ToArray();
                var rule = rules[kind];
                for (var index = 1; index < ordered.Length; index++)
                {
                    var earlier = ordered[index - 1];
                    var later = ordered[index];
                    nodes.TryGetValue(earlier.SectorId, out var earlierNode);
                    nodes.TryGetValue(later.SectorId, out var laterNode);
                    var graphDistanceAvailable = earlierNode != null && laterNode != null;
                    var graphDistance = graphDistanceAvailable
                        ? Math.Abs(earlierNode.Coordinate.X - laterNode.Coordinate.X) +
                          Math.Abs(earlierNode.Coordinate.Y - laterNode.Coordinate.Y)
                        : -1;
                    var solveDistance = Math.Abs(later.SolveStep - earlier.SolveStep);
                    var repeats = string.Equals(earlier.SignatureId, later.SignatureId, StringComparison.Ordinal);
                    var graphTooClose = repeats && graphDistanceAvailable && graphDistance < rule.MinimumSectorDistance;
                    var solveTooClose = repeats && solveDistance < rule.MinimumSolveStepDistance;
                    var missingRequiredGraph = repeats && rule.RequireGraphDistance && !graphDistanceAvailable;
                    var accepted = !graphTooClose && !solveTooClose && !missingRequiredGraph;
                    var reason = accepted ? string.Empty : string.Join(" ", new[]
                    {
                        "Recent", kind.ToString(), "signature repeated before minimum graph/solve-step distance."
                    });
                    var observation = new WorldRecentUseObservation(
                        "OBS_" + kind + "_" + earlier.SolveStep + "_" + later.SolveStep,
                        kind, earlier.SignatureId, later.SignatureId, earlier.SectorId, later.SectorId,
                        earlier.SolveStep, later.SolveStep, graphDistance, graphDistanceAvailable,
                        solveDistance, accepted, reason);
                    result.Add(observation);
                    if (!accepted)
                        violations.Add(new WorldPacingDensityViolation(
                            RecentViolation(kind), observation.ObservationId, later.SectorId,
                            later.SignatureId, reason));
                }
            }
            return result.OrderBy(value => value).ToArray();
        }

        private static WorldPacingDensityViolationType RecentViolation(WorldContentSignatureKind kind)
        {
            switch (kind)
            {
                case WorldContentSignatureKind.Pattern:
                    return WorldPacingDensityViolationType.RecentPatternRepeat;
                case WorldContentSignatureKind.Cluster:
                    return WorldPacingDensityViolationType.RecentClusterRepeat;
                default:
                    return WorldPacingDensityViolationType.RecentActivityRepeat;
            }
        }

        private static void ValidateDigest(
            string value,
            string subject,
            ICollection<WorldPacingDensityFailure> failures)
        {
            if (!WorldSolveDigest.IsLowerHexSha256(value))
                failures.Add(Failure(WorldPacingDensityFailureCode.InvalidDigest, subject,
                    "Digest must be a 64-character lowercase hexadecimal SHA-256 value."));
        }

        private static WorldPacingDensityFailure Failure(
            WorldPacingDensityFailureCode code,
            string subject,
            string reason) => new WorldPacingDensityFailure(code, subject, reason);
    }
}
