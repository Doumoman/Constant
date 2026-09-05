using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.WorldGeneration.Validation
{
    public static class GeneratedWorstCaseScenarioValidator
    {
        public const string ExpectedGraphDigest =
            "bf6b682dd5797e6dd8cb469289ca26fc8db3f10175a99f77d6316c5afe65d063";
        public const string ExpectedCompletionProofDigest =
            "6bc3bf96b766337011fd08d3436fd46e12b30ae7b0af5cfc0598646e8e338fca";
        public const string ExpectedMap19_04CombinedDigest =
            "6e393133118683be5104af6e4a4f2eff39cee61dc2c1dc8c240fba969512b816";
        public const string ExpectedMap19_05CombinedDigest =
            "3a3e02bb35f99c052b18588fa4ed6a0bdcf01bc8cac860721f28908115c840fa";
        public const string ExpectedIncomingHandoffDigest =
            "37643ee61d7ccd85f95018542382e119b418be544ee0d4820551012413535907";

        public static GeneratedWorstCaseScenarioResult Validate(
            GeneratedWorstCaseScenarioInput input)
        {
            var failures = new List<GeneratedWorstCaseScenarioFailure>();
            if (!ValidateBindings(input, failures)) return Failure(failures);

            var graphBefore = input.Graph.GraphDigest;
            var nakedBefore = input.NakedSearch.SuccessProofDigest;
            var completionBefore = input.CompletionSearch.SuccessProofDigest;
            var clusterBefore = input.ClusterValidation.CombinedSuccessDigest;
            var repetitionBefore = input.RepetitionValidation.CombinedSuccessDigest;
            var proofs = new List<GeneratedWorstCaseScenarioProof>();
            var overlaysApplied = 0;
            var worstTransforms = 0;
            var fallbackPasses = 0;

            foreach (var scenario in input.Catalog.Scenarios)
            {
                var failureCount = failures.Count;
                var selectedCells = ResolveCells(input, scenario, failures);
                var selectedDevices = ResolveDevices(input, scenario, failures);
                var retained = FilterTransitions(input.Transitions, scenario,
                    selectedDevices, out var removed);
                ValidateFallbacks(scenario, selectedDevices, retained, failures);
                if (failures.Count != failureCount) continue;

                var completionInput = new GeneratedCompletionSearchInput(input.Graph,
                    input.NakedSearch, retained.Select(value => value.Transition), input.Goal,
                    input.AllowedMovementKinds, new GeneratedCompletionActionAudit());
                var completion = GeneratedCompletionSearch.Search(completionInput);
                if (!completion.Success)
                {
                    Add(failures, scenario.SourceOwner, CompletionReason(scenario.Kind),
                        scenario.ScenarioId, "COMPLETION_SEARCH", input.Goal.StableToken,
                        "GOAL_SATISFIED", "GOAL_MISSING", scenario.SourceDigest,
                        completion.Failures.Count == 0 ? "EMPTY_FRONTIER" :
                            string.Join(";", completion.Failures.Select(value =>
                                value.StableToken)));
                    continue;
                }

                var scenarioDigest = BakingCanonicalDigest.HashCanonicalLines(new[]
                {
                    "MAP19_06_SCENARIO_INPUT_V1", input.ComputeDigest(),
                    scenario.StableToken,
                    "RETAINED=" + string.Join(",", retained.Select(value =>
                        value.Transition.TransitionId)),
                    "REMOVED=" + string.Join(",", removed),
                    "OVERLAYS=" + string.Join(",", selectedCells.Select(value =>
                        value.OverlayId)),
                    "DEVICES=" + string.Join(",", selectedDevices.Select(value =>
                        value.DeviceId)),
                });
                proofs.Add(new GeneratedWorstCaseScenarioProof(scenario, scenarioDigest,
                    removed, selectedCells.Select(value => value.OverlayId),
                    new[] { input.Goal.StableToken }, completion));
                overlaysApplied += selectedCells.Count;
                worstTransforms += selectedDevices.Count;
                fallbackPasses += selectedDevices.Count(value =>
                    value.ProvidesOnlyMandatoryMovement);
            }

            if (failures.Count != 0) return Failure(failures);
            if (!string.Equals(graphBefore, input.Graph.GraphDigest, StringComparison.Ordinal) ||
                !string.Equals(nakedBefore, input.NakedSearch.SuccessProofDigest,
                    StringComparison.Ordinal) ||
                !string.Equals(completionBefore, input.CompletionSearch.SuccessProofDigest,
                    StringComparison.Ordinal) ||
                !string.Equals(clusterBefore, input.ClusterValidation.CombinedSuccessDigest,
                    StringComparison.Ordinal) ||
                !string.Equals(repetitionBefore,
                    input.RepetitionValidation.CombinedSuccessDigest,
                    StringComparison.Ordinal))
            {
                Add(failures, "MAP19_06", "UPSTREAM_SURFACE_MUTATION", "ALL",
                    "READ_ONLY_GUARD", "UPSTREAM_DIGEST", "UNCHANGED", "CHANGED",
                    graphBefore, "DIGEST_GUARD");
                return Failure(failures);
            }

            var protectedRejects = input.LogicalCellCandidates.Count(value =>
                value.IsProtectedCritical);
            var surface = new GeneratedWorstCaseScenarioSurface(input, proofs,
                input.LogicalCellCandidates.Count, protectedRejects, overlaysApplied,
                input.DeviceCandidates.Count, worstTransforms, fallbackPasses);
            return new GeneratedWorstCaseScenarioResult(surface,
                Array.Empty<GeneratedWorstCaseScenarioFailure>());
        }

        private static bool ValidateBindings(GeneratedWorstCaseScenarioInput input,
            ICollection<GeneratedWorstCaseScenarioFailure> failures)
        {
            if (input == null)
            {
                Add(failures, "MAP19_06", "MISSING_INPUT", "ALL", "INPUT",
                    "INPUT", "NON_NULL", "NULL", "MISSING", "NO_INPUT");
                return false;
            }
            if (input.RepetitionValidation == null ||
                !input.RepetitionValidation.Success ||
                string.IsNullOrEmpty(input.RepetitionValidation.Map19_06HandoffDigest))
                Add(failures, "MAP19_05", "MISSING_MAP19_05_HANDOFF", "ALL",
                    "MAP19_05_HANDOFF", "MAP19_05", ExpectedIncomingHandoffDigest,
                    "MISSING", "MISSING", "HANDOFF_BINDING");
            if (input.Graph == null || input.NakedSearch == null ||
                input.CompletionSearch == null || input.ClusterValidation == null)
                Add(failures, "MAP19_02_TO_MAP19_04", "MISSING_UPSTREAM_BINDING", "ALL",
                    "UPSTREAM", "GRAPH_NAKED_COMPLETION_CLUSTER", "NON_NULL", "NULL",
                    "MISSING", "READ_ONLY_BINDING");
            if (failures.Count != 0) return false;

            Check(failures, "MAP19_02", "GRAPH_DIGEST_MISMATCH", "GRAPH",
                ExpectedGraphDigest, input.Graph.GraphDigest);
            Check(failures, "MAP19_03", "COMPLETION_PROOF_DIGEST_MISMATCH",
                "COMPLETION", ExpectedCompletionProofDigest,
                input.CompletionSearch.SuccessProofDigest);
            Check(failures, "MAP19_04", "MAP19_04_COMBINED_DIGEST_MISMATCH",
                "MAP19_04", ExpectedMap19_04CombinedDigest,
                input.ClusterValidation.CombinedSuccessDigest);
            Check(failures, "MAP19_05", "MAP19_05_COMBINED_DIGEST_MISMATCH",
                "MAP19_05", ExpectedMap19_05CombinedDigest,
                input.RepetitionValidation.CombinedSuccessDigest);
            Check(failures, "MAP19_04", "DECLARED_MAP19_04_DIGEST_MISMATCH",
                "DECLARED_MAP19_04", input.ClusterValidation.CombinedSuccessDigest,
                input.DeclaredMap19_04CombinedDigest);
            Check(failures, "MAP19_05", "DECLARED_MAP19_05_DIGEST_MISMATCH",
                "DECLARED_MAP19_05", input.RepetitionValidation.CombinedSuccessDigest,
                input.DeclaredMap19_05CombinedDigest);
            Check(failures, "MAP19_05", "INCOMING_HANDOFF_DIGEST_MISMATCH",
                "INCOMING_HANDOFF", ExpectedIncomingHandoffDigest,
                input.DeclaredIncomingHandoffDigest);
            Check(failures, "MAP19_05", "INCOMING_HANDOFF_BINDING_MISMATCH",
                "ACTUAL_HANDOFF", input.RepetitionValidation.Map19_06HandoffDigest,
                input.DeclaredIncomingHandoffDigest);

            if (input.Catalog == null || input.Catalog.Scenarios.Count != 7 ||
                Enum.GetValues(typeof(GeneratedWorstCaseScenarioKind))
                    .Cast<GeneratedWorstCaseScenarioKind>().Any(kind =>
                        input.Catalog == null || input.Catalog.Scenarios.Count(value =>
                            value.Kind == kind) != 1))
                Add(failures, "MAP19_06", "MISSING_SCENARIO_BINDING", "CATALOG",
                    "REQUIRED_KINDS", "SCENARIO_KINDS", "SEVEN_EXACT_KINDS", input.Catalog == null ?
                    "MISSING" : Number(input.Catalog.Scenarios.Count),
                    input.Catalog == null ? "MISSING" : input.Catalog.Digest,
                    "CATALOG_BINDING");
            else if (input.Catalog.Scenarios.Any(value => value.AllowedToolMask != 0))
                Add(failures, "MAP19_06", "ZERO_TOOL_MASK_VIOLATION", "CATALOG",
                    "TOOL_MASK", "ALLOWED_TOOL_MASK", "0", "NON_ZERO", input.Catalog.Digest,
                    "PURE_DATA_TRANSFORM");

            if (!ValidVillage(input.VillageStateMarkers))
                Add(failures, "MAP13_MAP18", "MISSING_SCENARIO_BINDING", "VILLAGE",
                    "MARKERS", "VILLAGE_STATE_MARKERS", "HOSTILE_EVACUATED_SHOP_FACILITY_NPC",
                    "MISSING", "MISSING", "VILLAGE_BINDING");
            if (input.Goal == null || input.Transitions.Count == 0 ||
                input.Transitions.Any(value => value.Transition == null) ||
                input.Transitions.Where(value => value.Transition != null)
                    .GroupBy(value => value.Transition.TransitionId,
                        StringComparer.Ordinal).Any(value => value.Count() != 1))
                Add(failures, "MAP19_03", "MISSING_SCENARIO_BINDING", "COMPLETION",
                    "BINDINGS", "TRANSITIONS_GOAL", "UNIQUE_NON_NULL", "INVALID",
                    input.ComputeDigest(), "COMPLETION_BINDING");
            ValidateCells(input, failures);
            ValidateDevices(input, failures);
            if (input.ActionAudit == null || input.ActionAudit.GraphMutationAttempts != 0 ||
                input.ActionAudit.ProofRewriteAttempts != 0 ||
                input.ActionAudit.RuntimeQueryAttempts != 0 ||
                input.ActionAudit.WorldMutationAttempts != 0 ||
                input.ActionAudit.TerrainMutationAttempts != 0 ||
                input.ActionAudit.PhysicsQueryAttempts != 0 ||
                input.ActionAudit.SeedBatchAttempts != 0)
                Add(failures, "MAP19_06", "FORBIDDEN_RUNTIME_MUTATION", "ALL",
                    "AUDIT", "ACTION_AUDIT", "PURE_DATA_READ_ONLY", input.ActionAudit == null ?
                    "MISSING" : input.ActionAudit.StableToken, input.ComputeDigest(),
                    "NO_RUNTIME_QUERY_OR_MUTATION");
            if (input.ActionAudit != null && input.ActionAudit.Map19_07Started)
                Add(failures, "MAP19_07", "MAP19_07_START_ATTEMPT", "ALL",
                    "TASK_BOUNDARY", "MAP19_07_STARTED", "FALSE", "TRUE",
                    input.ComputeDigest(), "MAP19_07_LOCKED");
            return failures.Count == 0;
        }

        private static bool ValidVillage(VillageStateMarkerSetDefinition value) => value != null &&
            value.NpcMarkers.Count != 0 && value.InventoryMarkers.Count != 0 &&
            value.DoorMarkers.Count != 0 && value.RequestedVariants.Contains(
                VillageStateKind.AllHostile) && value.RequestedVariants.Contains(
                VillageStateKind.Evacuation);

        private static void ValidateCells(GeneratedWorstCaseScenarioInput input,
            ICollection<GeneratedWorstCaseScenarioFailure> failures)
        {
            foreach (var cell in input.LogicalCellCandidates)
                if (cell.Target == null || cell.Payload == null ||
                    !cell.IsDestructibleOrReplaceable ||
                    !cell.Payload.IsValidFor(cell.ModificationKind))
                    Add(failures, cell.SourceOwner, "INVALID_DESTRUCTIBLE_CANDIDATE",
                        "DESTRUCTIBLE", cell.OverlayId,
                        "DESTRUCTIBLE_OR_REPLACEABLE_LOGICAL_PAYLOAD", "VALID",
                        "INVALID", cell.SourceDigest, cell.StableToken);
        }

        private static void ValidateDevices(GeneratedWorstCaseScenarioInput input,
            ICollection<GeneratedWorstCaseScenarioFailure> failures)
        {
            foreach (var device in input.DeviceCandidates)
                if (device.Target == null || device.WorstStatePayload == null ||
                    !device.IsMovingOrStateful ||
                    !device.WorstStatePayload.IsValidFor(
                        GeneratedSectorModificationKind.ChangeDeviceState))
                    Add(failures, device.SourceOwner, "INVALID_DEVICE_CANDIDATE",
                        "DEVICE", device.DeviceId, "MOVING_OR_STATEFUL_DEVICE", "VALID",
                        "INVALID", device.SourceDigest, device.StableToken);
        }

        private static List<GeneratedWorstCaseLogicalCellCandidate> ResolveCells(
            GeneratedWorstCaseScenarioInput input, GeneratedWorstCaseScenarioTransform scenario,
            ICollection<GeneratedWorstCaseScenarioFailure> failures)
        {
            var result = new List<GeneratedWorstCaseLogicalCellCandidate>();
            foreach (var id in scenario.LogicalOverlayIds)
            {
                var cell = input.LogicalCellCandidates.SingleOrDefault(value =>
                    string.Equals(value.OverlayId, id, StringComparison.Ordinal));
                if (cell == null)
                    Add(failures, scenario.SourceOwner, "MISSING_SCENARIO_BINDING",
                        scenario.ScenarioId, id, "LOGICAL_OVERLAY", "BOUND", "MISSING",
                        scenario.SourceDigest, "OVERLAY_LOOKUP");
                else if (cell.IsProtectedCritical)
                    Add(failures, cell.SourceOwner, "PROTECTED_CRITICAL_DESTRUCTIBLE_OVERLAY",
                        scenario.ScenarioId, cell.OverlayId, "PROTECTED_CRITICAL_SOURCE",
                        "RETAINED", cell.ProtectedSources.ToString(), cell.SourceDigest,
                        cell.StableToken);
                else result.Add(cell);
            }
            return result;
        }

        private static List<GeneratedWorstCaseDeviceCandidate> ResolveDevices(
            GeneratedWorstCaseScenarioInput input, GeneratedWorstCaseScenarioTransform scenario,
            ICollection<GeneratedWorstCaseScenarioFailure> failures)
        {
            var result = new List<GeneratedWorstCaseDeviceCandidate>();
            foreach (var id in scenario.DisabledDeviceIds)
            {
                var device = input.DeviceCandidates.SingleOrDefault(value =>
                    string.Equals(value.DeviceId, id, StringComparison.Ordinal));
                if (device == null)
                    Add(failures, scenario.SourceOwner, "MISSING_SCENARIO_BINDING",
                        scenario.ScenarioId, id, "DEVICE", "BOUND", "MISSING",
                        scenario.SourceDigest, "DEVICE_LOOKUP");
                else result.Add(device);
            }
            return result;
        }

        private static List<GeneratedWorstCaseTransitionBinding> FilterTransitions(
            IEnumerable<GeneratedWorstCaseTransitionBinding> source,
            GeneratedWorstCaseScenarioTransform scenario,
            IEnumerable<GeneratedWorstCaseDeviceCandidate> devices,
            out string[] removed)
        {
            var deviceIds = new HashSet<string>(devices.SelectMany(value =>
                value.TransitionIds), StringComparer.Ordinal);
            var removedRoles = new HashSet<GeneratedWorstCaseTransitionRole>(
                scenario.RemovedRoles);
            var removedBindings = source.Where(value => removedRoles.Contains(value.Role) ||
                deviceIds.Contains(value.Transition.TransitionId)).ToArray();
            removed = removedBindings.Select(value => value.Transition.TransitionId)
                .Distinct(StringComparer.Ordinal).OrderBy(value => value,
                    StringComparer.Ordinal).ToArray();
            return source.Except(removedBindings).OrderBy(value => value).ToList();
        }

        private static void ValidateFallbacks(GeneratedWorstCaseScenarioTransform scenario,
            IEnumerable<GeneratedWorstCaseDeviceCandidate> devices,
            IEnumerable<GeneratedWorstCaseTransitionBinding> retained,
            ICollection<GeneratedWorstCaseScenarioFailure> failures)
        {
            var ids = new HashSet<string>(retained.Select(value =>
                value.Transition.TransitionId), StringComparer.Ordinal);
            foreach (var device in devices.Where(value =>
                value.ProvidesOnlyMandatoryMovement))
                if (device.FallbackTransitionIds.Count == 0 ||
                    !device.FallbackTransitionIds.Any(ids.Contains))
                    Add(failures, device.SourceOwner, "DEVICE_FALLBACK_MISSING",
                        scenario.ScenarioId, device.DeviceId,
                        "EXPLICIT_RETAINED_FALLBACK", "PRESENT", "MISSING",
                        device.SourceDigest, string.Join(",",
                            device.FallbackTransitionIds));
        }

        private static string CompletionReason(GeneratedWorstCaseScenarioKind kind)
        {
            switch (kind)
            {
                case GeneratedWorstCaseScenarioKind.ZeroTool:
                    return "ZERO_TOOL_COMPLETION_FAILURE";
                case GeneratedWorstCaseScenarioKind.VillageSkipped:
                    return "VILLAGE_SKIPPED_COMPLETION_FAILURE";
                case GeneratedWorstCaseScenarioKind.VillageHostile:
                    return "HOSTILE_VILLAGE_DEPENDENCY_FAILURE";
                case GeneratedWorstCaseScenarioKind.VillageEvacuated:
                    return "EVACUATED_VILLAGE_DEPENDENCY_FAILURE";
                case GeneratedWorstCaseScenarioKind.CombinedAdverseStaticShell:
                    return "COMBINED_ADVERSE_COMPLETION_FAILURE";
                case GeneratedWorstCaseScenarioKind.DestructibleTileLoss:
                    return "DESTRUCTIBLE_TILE_LOSS_COMPLETION_FAILURE";
                default:
                    return "DEVICE_WORST_POSITION_COMPLETION_FAILURE";
            }
        }

        private static void Check(ICollection<GeneratedWorstCaseScenarioFailure> failures,
            string owner, string reason, string key, string expected, string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                Add(failures, owner, reason, "ALL", key, "DIGEST", expected, actual,
                    string.IsNullOrEmpty(actual) ? "MISSING" : actual, "DIGEST_BINDING");
        }

        private static GeneratedWorstCaseScenarioResult Failure(
            IEnumerable<GeneratedWorstCaseScenarioFailure> failures) =>
            new GeneratedWorstCaseScenarioResult(null, failures);

        private static void Add(ICollection<GeneratedWorstCaseScenarioFailure> failures,
            string owner, string reason, string scenarioId, string caseId,
            string offendingKey, string expected, string actual, string sourceDigest,
            string evidence) => failures.Add(new GeneratedWorstCaseScenarioFailure(owner,
                reason, scenarioId, caseId, offendingKey, expected, actual, sourceDigest,
                evidence));

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
