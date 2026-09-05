using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.WorldGeneration.Validation
{
    public static class GeneratedRepetitionEventRemovalValidator
    {
        public const string ExpectedGraphDigest =
            "bf6b682dd5797e6dd8cb469289ca26fc8db3f10175a99f77d6316c5afe65d063";
        public const string ExpectedCompletionProofDigest =
            "6bc3bf96b766337011fd08d3436fd46e12b30ae7b0af5cfc0598646e8e338fca";
        public const string ExpectedRecoveryProofDigest =
            "b371b67691697eb719309f59131c4b85ec5c6402d64d7f183aec7d03ff1c92b3";
        public const string ExpectedDensityDigest =
            "a33ccb810d8ec39168fe222958850176821792525563e472d7eac3ad07b25b11";
        public const string ExpectedMap19_04CombinedDigest =
            "6e393133118683be5104af6e4a4f2eff39cee61dc2c1dc8c240fba969512b816";
        public const string ExpectedIncomingHandoffDigest =
            "0bdcc4ff5e24e91c11156a64b04acd18508cb42e4f26e72c90a5705793db7087";

        public static GeneratedRepetitionValidationResult Validate(
            GeneratedRepetitionValidationInput repetitionInput,
            GeneratedEventRemovalValidationInput removalInput)
        {
            var failures = new List<GeneratedRepetitionFailure>();
            if (!ValidateCommon(repetitionInput, removalInput, failures))
                return Failure(failures);

            var repetition = ValidateRepetition(repetitionInput, failures);
            if (failures.Count != 0)
                return Failure(failures);

            var removal = ValidateRemoval(removalInput, failures);
            if (failures.Count != 0)
                return Failure(failures);

            var surface = new GeneratedRepetitionEventRemovalSurface(repetition, removal,
                repetitionInput.Graph.GraphDigest,
                repetitionInput.CompletionSearch.SuccessProofDigest,
                repetitionInput.ClusterValidation.CombinedSuccessDigest,
                repetitionInput.ClusterValidation.Map19_05HandoffDigest);
            return new GeneratedRepetitionValidationResult(surface, failures);
        }

        private static bool ValidateCommon(
            GeneratedRepetitionValidationInput repetition,
            GeneratedEventRemovalValidationInput removal,
            ICollection<GeneratedRepetitionFailure> failures)
        {
            if (repetition == null)
            {
                Add(failures, "Repetition", "MISSING_REPETITION_INPUT", "input",
                    "repetition", "NON_NULL", "NULL", "MISSING");
                return false;
            }
            if (removal == null)
            {
                Add(failures, "EventRemoval", "MISSING_REMOVAL_INPUT", "input",
                    "removal", "NON_NULL", "NULL", "MISSING");
                return false;
            }
            if (repetition.Graph == null || removal.Graph == null)
            {
                Add(failures, "MAP19_02", "MISSING_GRAPH_INPUT", "graph", "graph",
                    "SAME_NON_NULL_GRAPH", "MISSING", "MISSING");
                return false;
            }
            var graph = repetition.Graph;
            Digest(failures, "MAP19_02", "GRAPH_DIGEST_MISMATCH", "graph",
                "graph.digest", ExpectedGraphDigest, graph.GraphDigest, graph.GraphDigest);
            Digest(failures, "MAP19_02", "GRAPH_CANONICAL_DIGEST_MISMATCH", "graph",
                "graph.canonical", graph.GraphDigest, graph.ComputeGraphDigest(),
                graph.GraphDigest);
            if (!ReferenceEquals(graph, removal.Graph))
                Add(failures, "MAP19_02", "GRAPH_BINDING_MISMATCH", "graph", "graph",
                    "SAME_READ_ONLY_INSTANCE", "DIFFERENT_INSTANCE", graph.GraphDigest);

            ValidateMap19_03(repetition.NakedSearch, repetition.CompletionSearch, graph,
                "repetition", failures);
            ValidateMap19_03(removal.NakedSearch, removal.CompletionSearch, graph,
                "removal", failures);
            if (!ReferenceEquals(repetition.NakedSearch, removal.NakedSearch) ||
                !ReferenceEquals(repetition.CompletionSearch, removal.CompletionSearch))
                Add(failures, "MAP19_03", "PROOF_BINDING_MISMATCH", "proofs", "proofs",
                    "SAME_READ_ONLY_INSTANCES", "DIFFERENT_INSTANCES", graph.GraphDigest);

            ValidateMap19_04(repetition.ClusterValidation,
                repetition.DeclaredMap19_04CombinedDigest,
                repetition.DeclaredIncomingHandoffDigest, "repetition", failures);
            ValidateMap19_04(removal.ClusterValidation,
                removal.DeclaredMap19_04CombinedDigest,
                removal.DeclaredIncomingHandoffDigest, "removal", failures);
            if (!ReferenceEquals(repetition.ClusterValidation, removal.ClusterValidation))
                Add(failures, "MAP19_04", "VALIDATION_BINDING_MISMATCH", "map19_04",
                    "cluster_validation", "SAME_READ_ONLY_INSTANCE", "DIFFERENT_INSTANCE",
                    graph.GraphDigest);

            if (repetition.Canvas == null || repetition.Slices == null)
                Add(failures, "MAP16_MAP17", "MISSING_CANVAS_OR_SLICE_INPUT", "geometry",
                    "canvas_slices", "NON_NULL", "MISSING", graph.GraphDigest);
            else
            {
                if (repetition.Canvas.Width != WorldGenConstants.SectorWidthTiles ||
                    repetition.Canvas.Height != WorldGenConstants.SectorHeightTiles ||
                    repetition.Canvas.Cells.Count != WorldGenConstants.TilesPerSector)
                    Add(failures, "MAP16", "CANVAS_SHAPE_MISMATCH", "geometry", "canvas",
                        WorldGenConstants.SectorWidthTiles + "x" +
                        WorldGenConstants.SectorHeightTiles + "/" +
                        WorldGenConstants.TilesPerSector,
                        repetition.Canvas.Width + "x" + repetition.Canvas.Height + "/" +
                        repetition.Canvas.Cells.Count, graph.GraphDigest);
                if (repetition.Slices.Slices.Count != WorldGenConstants.MicroChunksPerSector ||
                    repetition.Slices.Slices.Sum(value => value.Cells.Count) !=
                    WorldGenConstants.TilesPerSector ||
                    repetition.Slices.SourceCanvasId != repetition.Canvas.Id)
                    Add(failures, "MAP17", "SLICE_PROVENANCE_MISMATCH", "geometry", "slices",
                        WorldGenConstants.MicroChunksPerSector + "/" +
                        WorldGenConstants.TilesPerSector + "/" + repetition.Canvas.Id.Value,
                        repetition.Slices.Slices.Count + "/" +
                        repetition.Slices.Slices.Sum(value => value.Cells.Count) + "/" +
                        repetition.Slices.SourceCanvasId.Value, graph.GraphDigest);
            }

            if (repetition.ActionAudit == null || removal.ActionAudit == null ||
                !repetition.ActionAudit.IsZero || !removal.ActionAudit.IsZero)
                Add(failures, "ForbiddenAction", "FORBIDDEN_API_OR_MUTATION_ATTEMPT",
                    "action_audit", "action_audit",
                    "ALL_ZERO_AND_MAP19_06_NOT_STARTED", "NON_ZERO_OR_MISSING",
                    graph.GraphDigest);
            return failures.Count == 0;
        }

        private static void ValidateMap19_03(
            NakedTraversalSearchResult naked,
            GeneratedCompletionSearchResult completion,
            GeneratedTileMovementGraph graph,
            string caseId,
            ICollection<GeneratedRepetitionFailure> failures)
        {
            if (naked == null || !naked.Success || naked.Surface == null)
                Add(failures, "MAP19_03", "MISSING_NAKED_PROOF", caseId, "naked",
                    "SUCCESSFUL_PROOF", "MISSING_OR_FAILED", graph.GraphDigest);
            else if (!ReferenceEquals(naked.Surface.Graph, graph))
                Add(failures, "MAP19_03", "NAKED_GRAPH_BINDING_MISMATCH", caseId,
                    "naked.graph", "SAME_READ_ONLY_INSTANCE", "DIFFERENT_INSTANCE",
                    naked.SuccessProofDigest);
            if (completion == null || !completion.Success || completion.Proof == null)
            {
                Add(failures, "MAP19_03", "MISSING_COMPLETION_PROOF", caseId, "completion",
                    "SUCCESSFUL_PROOF", "MISSING_OR_FAILED", graph.GraphDigest);
                return;
            }
            Digest(failures, "MAP19_03", "COMPLETION_PROOF_DIGEST_MISMATCH", caseId,
                "completion.digest", ExpectedCompletionProofDigest,
                completion.SuccessProofDigest, graph.GraphDigest);
        }

        private static void ValidateMap19_04(
            ClusterRecoveryValidationResult result,
            string declaredCombined,
            string declaredHandoff,
            string caseId,
            ICollection<GeneratedRepetitionFailure> failures)
        {
            if (result == null || !result.Success || result.Surface == null)
            {
                Add(failures, "MAP19_04", "MISSING_MAP19_04_HANDOFF", caseId,
                    "cluster_validation", "SUCCESSFUL_RESULT", "MISSING_OR_FAILED",
                    "MISSING");
                return;
            }
            Digest(failures, "MAP19_04", "RECOVERY_PROOF_DIGEST_MISMATCH", caseId,
                "recovery.digest", ExpectedRecoveryProofDigest,
                result.RecoverySuccessDigest, result.CombinedSuccessDigest);
            Digest(failures, "MAP19_04", "DENSITY_DIGEST_MISMATCH", caseId,
                "density.digest", ExpectedDensityDigest, result.DensitySuccessDigest,
                result.CombinedSuccessDigest);
            Digest(failures, "MAP19_04", "COMBINED_DIGEST_MISMATCH", caseId,
                "combined.digest", ExpectedMap19_04CombinedDigest,
                result.CombinedSuccessDigest, result.CombinedSuccessDigest);
            Digest(failures, "MAP19_04", "DECLARED_COMBINED_DIGEST_MISMATCH", caseId,
                "declared.combined", ExpectedMap19_04CombinedDigest, declaredCombined,
                result.CombinedSuccessDigest);
            Digest(failures, "MAP19_04", "INCOMING_HANDOFF_DIGEST_MISMATCH", caseId,
                "declared.handoff", ExpectedIncomingHandoffDigest, declaredHandoff,
                result.CombinedSuccessDigest);
            Digest(failures, "MAP19_04", "ACTUAL_HANDOFF_DIGEST_MISMATCH", caseId,
                "actual.handoff", ExpectedIncomingHandoffDigest,
                result.Map19_05HandoffDigest, result.CombinedSuccessDigest);
        }

        private static GeneratedRepetitionValidationSurface ValidateRepetition(
            GeneratedRepetitionValidationInput input,
            ICollection<GeneratedRepetitionFailure> failures)
        {
            var signatures = input.Signatures;
            foreach (var signature in signatures)
                ValidateSignature(signature, failures);
            foreach (var duplicate in signatures.GroupBy(value => value.SourceOwner + "|" +
                         value.SignatureId, StringComparer.Ordinal).Where(value => value.Count() > 1))
                Add(failures, "Repetition", "DUPLICATE_SIGNATURE_ID", duplicate.Key,
                    duplicate.Key, "UNIQUE", Number(duplicate.Count()),
                    input.Graph.GraphDigest);

            var kinds = new[]
            {
                GeneratedRepetitionWindowKind.PatternMirror,
                GeneratedRepetitionWindowKind.TerrainCluster,
                GeneratedRepetitionWindowKind.ActivityEvent,
            };
            foreach (var kind in kinds.Where(kind => input.Windows.All(value => value.Kind != kind)))
                Add(failures, "Repetition", "MISSING_REPETITION_WINDOW", kind.ToString(),
                    "windows", "AT_LEAST_ONE", "0", input.Graph.GraphDigest);
            foreach (var window in input.Windows.Where(value => value.MaximumOrdinalDistance < 0))
                Add(failures, "Repetition", "INVALID_REPETITION_WINDOW", window.WindowId,
                    window.WindowId, "NON_NEGATIVE", Number(window.MaximumOrdinalDistance),
                    input.Graph.GraphDigest, window.StableToken);
            if (failures.Count != 0) return null;

            var pattern = signatures.Where(value => value.Kind ==
                GeneratedRepetitionSourceKind.MicroPattern).ToArray();
            var clusters = signatures.Where(value => value.Kind ==
                GeneratedRepetitionSourceKind.TerrainCluster).ToArray();
            var activityEvent = signatures.Where(value => value.Kind ==
                GeneratedRepetitionSourceKind.ActivityStructure || value.Kind ==
                GeneratedRepetitionSourceKind.EventOverlay).ToArray();

            var mirrorPairs = 0;
            var clusterWindows = 0;
            var activityWindows = 0;
            foreach (var window in input.Windows)
            {
                switch (window.Kind)
                {
                    case GeneratedRepetitionWindowKind.PatternMirror:
                        foreach (var pair in Pairs(pattern).Where(value =>
                                     !string.IsNullOrEmpty(value.Left.MirrorPairKey) &&
                                     string.Equals(value.Left.MirrorPairKey,
                                         value.Right.MirrorPairKey, StringComparison.Ordinal) &&
                                     value.Left.IsMirror != value.Right.IsMirror))
                        {
                            mirrorPairs++;
                            var distance = Distance(pair.Left, pair.Right);
                            var sameLocalBand = string.Equals(pair.Left.ClusterId,
                                                    pair.Right.ClusterId,
                                                    StringComparison.Ordinal) &&
                                                string.Equals(pair.Left.LocalBand,
                                                    pair.Right.LocalBand,
                                                    StringComparison.Ordinal);
                            if (distance <= window.MaximumOrdinalDistance || sameLocalBand)
                                RepetitionFailure(failures, "MicroPattern",
                                    "PATTERN_MIRROR_WINDOW_VIOLATION", window, pair.Left,
                                    pair.Right, distance);
                        }
                        break;
                    case GeneratedRepetitionWindowKind.TerrainCluster:
                        foreach (var pair in Pairs(clusters).Where(value =>
                                     string.Equals(value.Left.StructuralKey,
                                         value.Right.StructuralKey, StringComparison.Ordinal)))
                        {
                            clusterWindows++;
                            var distance = Distance(pair.Left, pair.Right);
                            if (distance <= window.MaximumOrdinalDistance)
                                RepetitionFailure(failures, "TerrainCluster",
                                    "CLUSTER_STRUCTURAL_REPETITION_VIOLATION", window,
                                    pair.Left, pair.Right, distance);
                        }
                        break;
                    case GeneratedRepetitionWindowKind.ActivityEvent:
                        foreach (var pair in Pairs(activityEvent).Where(value =>
                                     value.Left.Kind == value.Right.Kind &&
                                     string.Equals(value.Left.StructuralKey,
                                         value.Right.StructuralKey, StringComparison.Ordinal) &&
                                     string.Equals(value.Left.PacingBand,
                                         value.Right.PacingBand, StringComparison.Ordinal)))
                        {
                            activityWindows++;
                            var distance = Distance(pair.Left, pair.Right);
                            if (distance <= window.MaximumOrdinalDistance)
                                RepetitionFailure(failures, "ActivityEvent",
                                    "ACTIVITY_EVENT_REPETITION_VIOLATION", window,
                                    pair.Left, pair.Right, distance);
                        }
                        break;
                }
            }
            if (failures.Count != 0) return null;

            var collapsed = signatures.GroupBy(value =>
                    Number((int)value.Kind) + "|" + value.StructuralKey,
                    StringComparer.Ordinal)
                .Where(group => group.Select(value => value.VisualOrMaterialToken)
                    .Distinct(StringComparer.Ordinal).Count() > 1)
                .Sum(group => group.Count() - 1);
            var structuralTokens = signatures.GroupBy(value =>
                    Number((int)value.Kind) + "|" + value.StructuralKey,
                    StringComparer.Ordinal)
                .Select(group => group.Key + "|COUNT=" + Number(group.Count()));
            return new GeneratedRepetitionValidationSurface(input, pattern.Length, mirrorPairs,
                clusters.Length, clusterWindows, activityEvent.Length, activityWindows,
                collapsed, structuralTokens);
        }

        private static void ValidateSignature(GeneratedRepetitionSignature signature,
            ICollection<GeneratedRepetitionFailure> failures)
        {
            var validKind = Enum.IsDefined(typeof(GeneratedRepetitionSourceKind), signature.Kind);
            var contractValid = false;
            switch (signature.Kind)
            {
                case GeneratedRepetitionSourceKind.MicroPattern:
                    contractValid = signature.MicroPatternSignature != null &&
                        signature.TerrainCluster == null && signature.ActivityStructure == null &&
                        signature.EventOverlay == null;
                    break;
                case GeneratedRepetitionSourceKind.TerrainCluster:
                    contractValid = signature.MicroPatternSignature == null &&
                        signature.TerrainCluster != null && signature.ActivityStructure == null &&
                        signature.EventOverlay == null;
                    break;
                case GeneratedRepetitionSourceKind.ActivityStructure:
                    contractValid = signature.MicroPatternSignature == null &&
                        signature.TerrainCluster == null && signature.ActivityStructure != null &&
                        signature.EventOverlay == null;
                    break;
                case GeneratedRepetitionSourceKind.EventOverlay:
                    contractValid = signature.MicroPatternSignature == null &&
                        signature.TerrainCluster == null && signature.ActivityStructure == null &&
                        signature.EventOverlay != null;
                    break;
            }
            if (!validKind || !contractValid)
                Add(failures, signature.SourceOwner, "SIGNATURE_CONTRACT_BINDING_MISMATCH",
                    signature.SignatureId, signature.ContractIdentity,
                    "EXACT_SEMANTIC_OWNER_CONTRACT", "MISSING_OR_MULTIPLE",
                    signature.SourceDigest);
            if (signature.SourceOrdinal < 0)
                Add(failures, signature.SourceOwner, "INVALID_SOURCE_ORDINAL",
                    signature.SignatureId, "source_ordinal", "NON_NEGATIVE",
                    Number(signature.SourceOrdinal), signature.SourceDigest);
            if (!BakingCanonicalDigest.IsLowerHexSha256(signature.SourceDigest))
                Add(failures, signature.SourceOwner, "INVALID_SOURCE_DIGEST",
                    signature.SignatureId, "source_digest", "LOWER_HEX_SHA256",
                    signature.SourceDigest, signature.SourceDigest);
            if (signature.Kind == GeneratedRepetitionSourceKind.MicroPattern &&
                signature.MicroPatternSignature != null &&
                !string.Equals(signature.SourceDigest,
                    signature.MicroPatternSignature.StableDigest, StringComparison.Ordinal))
                Add(failures, signature.SourceOwner, "PATTERN_SOURCE_DIGEST_MISMATCH",
                    signature.SignatureId, signature.SourceId,
                    signature.MicroPatternSignature.StableDigest, signature.SourceDigest,
                    signature.MicroPatternSignature.StableDigest);
        }

        private static GeneratedEventRemovalValidationSurface ValidateRemoval(
            GeneratedEventRemovalValidationInput input,
            ICollection<GeneratedRepetitionFailure> failures)
        {
            if (input.Goal == null)
                Add(failures, "EventRemoval", "MISSING_COMPLETION_GOAL", "goal", "goal",
                    "NON_NULL", "NULL", input.Graph.GraphDigest);
            if (input.AllowedMovementKinds.Count == 0 || input.AllowedMovementKinds.Any(value =>
                    !Enum.IsDefined(typeof(TraversalMovementKind), value)))
                Add(failures, "EventRemoval", "INVALID_MOVEMENT_SET", "movements",
                    "movements", "DEFINED_NON_EMPTY", "EMPTY_OR_INVALID",
                    input.Graph.GraphDigest);
            if (input.Transitions.Count == 0)
                Add(failures, "EventRemoval", "MISSING_TRANSITIONS", "transitions",
                    "transitions", "NON_EMPTY", "0", input.Graph.GraphDigest);

            foreach (var duplicate in input.Transitions.Where(value => value.Transition != null)
                         .GroupBy(value => value.Transition.TransitionId, StringComparer.Ordinal)
                         .Where(value => value.Count() > 1))
                Add(failures, "EventRemoval", "DUPLICATE_TRANSITION_BINDING", duplicate.Key,
                    duplicate.Key, "1", Number(duplicate.Count()), input.Graph.GraphDigest);
            foreach (var binding in input.Transitions)
            {
                if (binding.Transition == null)
                {
                    Add(failures, binding.SourceOwner, "MISSING_TRANSITION", "transition",
                        "transition", "NON_NULL", "NULL", binding.SourceDigest);
                    continue;
                }
                if (!BakingCanonicalDigest.IsLowerHexSha256(binding.SourceDigest))
                    Add(failures, binding.SourceOwner, "INVALID_REMOVAL_SOURCE_DIGEST",
                        binding.Transition.TransitionId, "source_digest",
                        "LOWER_HEX_SHA256", binding.SourceDigest, binding.SourceDigest);
                var ownerMatches = binding.SourceKind ==
                        GeneratedRemovalTransitionSourceKind.ActivityStructure
                    ? binding.ActivityStructure != null && binding.EventOverlay == null
                    : binding.SourceKind == GeneratedRemovalTransitionSourceKind.EventOverlay
                        ? binding.EventOverlay != null && binding.ActivityStructure == null
                        : binding.ActivityStructure == null && binding.EventOverlay == null;
                if (!ownerMatches)
                    Add(failures, binding.SourceOwner, "REMOVAL_OWNER_BINDING_MISMATCH",
                        binding.Transition.TransitionId, "owner_contract",
                        "SOURCE_KIND_MATCH", "MISSING_OR_WRONG_CONTRACT", binding.SourceDigest);
                if (binding.IsRemoved && binding.DeclaresStaticDependency)
                    Add(failures, binding.SourceOwner,
                        "REMOVED_TRANSITION_DEPENDENCY_VIOLATION",
                        binding.Transition.TransitionId, binding.Transition.SourceKey,
                        "STATIC_SHELL_INDEPENDENT", "REMOVED_OWNER_REQUIRED",
                        binding.SourceDigest, binding.Transition.StableToken);
            }
            if (failures.Count != 0) return null;

            var retained = input.Transitions.Where(value => !value.IsRemoved)
                .Select(value => value.Transition).ToArray();
            var removed = input.Transitions.Where(value => value.IsRemoved).ToArray();
            var completion = GeneratedCompletionSearch.Search(new GeneratedCompletionSearchInput(
                input.Graph, input.NakedSearch, retained, input.Goal,
                input.AllowedMovementKinds));
            if (!completion.Success)
            {
                var evidence = string.Join(",", completion.Failures.Select(value =>
                    value.StableToken));
                Add(failures, "EventRemoval", "REMOVAL_COMPLETION_FAILURE",
                    "static_shell", input.Goal.ExitNodeId, "GOALS_SATISFIED",
                    "GOALS_MISSING", input.Graph.GraphDigest, evidence);
                return null;
            }
            Digest(failures, "MAP19_03", "REMOVAL_COMPLETION_PROOF_DIGEST_MISMATCH",
                "static_shell", "completion.proof", ExpectedCompletionProofDigest,
                completion.SuccessProofDigest, completion.Proof.StateSpaceDigest);
            if (failures.Count != 0) return null;

            var activityCount = removed.Where(value => value.SourceKind ==
                    GeneratedRemovalTransitionSourceKind.ActivityStructure)
                .Select(value => value.ActivityStructure.Id.Value)
                .Distinct(StringComparer.Ordinal).Count();
            var eventCount = removed.Where(value => value.SourceKind ==
                    GeneratedRemovalTransitionSourceKind.EventOverlay)
                .Select(value => value.EventOverlay.Id.Value)
                .Distinct(StringComparer.Ordinal).Count();
            return new GeneratedEventRemovalValidationSurface(input, completion, activityCount,
                eventCount, retained.Length, removed.Length);
        }

        private static IEnumerable<SignaturePair> Pairs(
            IReadOnlyList<GeneratedRepetitionSignature> values)
        {
            for (var left = 0; left < values.Count; left++)
            for (var right = left + 1; right < values.Count; right++)
                yield return new SignaturePair(values[left], values[right]);
        }

        private static int Distance(GeneratedRepetitionSignature left,
            GeneratedRepetitionSignature right) => Math.Abs(left.SourceOrdinal -
                right.SourceOrdinal);

        private static void RepetitionFailure(
            ICollection<GeneratedRepetitionFailure> failures,
            string owner,
            string reason,
            GeneratedRepetitionWindow window,
            GeneratedRepetitionSignature left,
            GeneratedRepetitionSignature right,
            int distance) => Add(failures, owner, reason, window.WindowId,
            left.SignatureId + "+" + right.SignatureId,
            "DISTANCE>" + Number(window.MaximumOrdinalDistance) +
            "_AND_DIFFERENT_LOCAL_BAND", Number(distance),
            left.SourceDigest, window.StableToken + "|" + left.StableToken + "|" +
            right.StableToken);

        private static void Digest(
            ICollection<GeneratedRepetitionFailure> failures,
            string owner,
            string reason,
            string caseId,
            string key,
            string expected,
            string actual,
            string sourceDigest)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                Add(failures, owner, reason, caseId, key, expected, actual, sourceDigest);
        }

        private static void Add(
            ICollection<GeneratedRepetitionFailure> failures,
            string owner,
            string reason,
            string caseId,
            string key,
            string expected,
            string actual,
            string sourceDigest,
            string evidence = "NONE") => failures.Add(new GeneratedRepetitionFailure(owner,
                reason, caseId, key, expected, actual, sourceDigest, evidence));

        private static GeneratedRepetitionValidationResult Failure(
            IEnumerable<GeneratedRepetitionFailure> failures) =>
            new GeneratedRepetitionValidationResult(null, failures);

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private sealed class SignaturePair
        {
            public SignaturePair(GeneratedRepetitionSignature left,
                GeneratedRepetitionSignature right)
            {
                Left = left;
                Right = right;
            }
            public GeneratedRepetitionSignature Left { get; }
            public GeneratedRepetitionSignature Right { get; }
        }
    }
}
