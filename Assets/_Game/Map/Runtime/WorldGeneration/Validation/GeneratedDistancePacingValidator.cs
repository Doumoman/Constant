using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Validation
{
    public static class GeneratedDistancePacingValidator
    {
        public const string ExpectedGraphDigest =
            "bf6b682dd5797e6dd8cb469289ca26fc8db3f10175a99f77d6316c5afe65d063";
        public const string ExpectedCompletionProofDigest =
            "6bc3bf96b766337011fd08d3436fd46e12b30ae7b0af5cfc0598646e8e338fca";
        public const string ExpectedMap19_04CombinedDigest =
            "6e393133118683be5104af6e4a4f2eff39cee61dc2c1dc8c240fba969512b816";
        public const string ExpectedMap19_05CombinedDigest =
            "3a3e02bb35f99c052b18588fa4ed6a0bdcf01bc8cac860721f28908115c840fa";
        public const string ExpectedMap19_06CombinedDigest =
            "9f8e46d45071e7ed2e3b189a0467a1381aa7eb05a516471b4c4337f52ad6420d";
        public const string ExpectedIncomingHandoffDigest =
            "5b9ef46451a1bee65b41c82d5011d9b0df4ba9c5cd2d939d395acbc8a320ad25";

        public static GeneratedDistancePacingResult Validate(
            GeneratedDistancePacingInput input)
        {
            var failures = new List<GeneratedDistancePacingFailure>();
            if (!ValidateBindings(input, failures)) return Failure(failures);

            var graphBefore = input.Graph.GraphDigest;
            var completionBefore = input.CompletionSearch.SuccessProofDigest;
            var clusterBefore = input.ClusterValidation.CombinedSuccessDigest;
            var repetitionBefore = input.RepetitionValidation.CombinedSuccessDigest;
            var worstBefore = input.WorstCaseValidation.CombinedSuccessDigest;
            var routeProofs = new List<GeneratedDistanceRouteProof>();

            foreach (var route in input.Routes)
                ValidateRoute(input, route, routeProofs, failures);

            var markerProofs = ValidateMarkers(input, failures);
            if (failures.Count != 0) return Failure(failures);

            if (!string.Equals(graphBefore, input.Graph.GraphDigest,
                    StringComparison.Ordinal) ||
                !string.Equals(completionBefore,
                    input.CompletionSearch.SuccessProofDigest, StringComparison.Ordinal) ||
                !string.Equals(clusterBefore,
                    input.ClusterValidation.CombinedSuccessDigest, StringComparison.Ordinal) ||
                !string.Equals(repetitionBefore,
                    input.RepetitionValidation.CombinedSuccessDigest,
                    StringComparison.Ordinal) ||
                !string.Equals(worstBefore,
                    input.WorstCaseValidation.CombinedSuccessDigest,
                    StringComparison.Ordinal))
            {
                Add(failures, "MAP19_02_TO_MAP19_06", "UPSTREAM_SURFACE_MUTATION",
                    "READ_ONLY_GUARD", 0, "UPSTREAM_DIGESTS", "UNCHANGED", "CHANGED",
                    graphBefore, "GRAPH_COMPLETION_CLUSTER_REPETITION_WORST_CASE");
                return Failure(failures);
            }

            return new GeneratedDistancePacingResult(
                new GeneratedDistancePacingSurface(input, routeProofs, markerProofs),
                Array.Empty<GeneratedDistancePacingFailure>());
        }

        private static bool ValidateBindings(GeneratedDistancePacingInput input,
            ICollection<GeneratedDistancePacingFailure> failures)
        {
            if (input == null)
            {
                Add(failures, "MAP19_07", "MISSING_INPUT", "ALL", 0, "INPUT",
                    "NON_NULL", "NULL", "MISSING", "NO_INPUT");
                return false;
            }

            if (input.WorstCaseValidation == null ||
                !input.WorstCaseValidation.Success ||
                string.IsNullOrEmpty(input.WorstCaseValidation.Map19_07HandoffDigest))
                Add(failures, "MAP19_06", "MISSING_MAP19_06_HANDOFF", "HANDOFF", 0,
                    "MAP19_06_HANDOFF", ExpectedIncomingHandoffDigest, "MISSING",
                    "MISSING", "HANDOFF_BINDING");
            if (input.Graph == null || input.CompletionSearch == null ||
                input.ClusterValidation == null || input.RepetitionValidation == null)
                Add(failures, "MAP19_02_TO_MAP19_05", "MISSING_UPSTREAM_BINDING",
                    "UPSTREAM", 0, "READ_ONLY_SURFACES", "ALL_PRESENT", "MISSING",
                    "MISSING", "GRAPH_COMPLETION_CLUSTER_REPETITION");
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
            Check(failures, "MAP19_06", "MAP19_06_COMBINED_DIGEST_MISMATCH",
                "MAP19_06", ExpectedMap19_06CombinedDigest,
                input.WorstCaseValidation.CombinedSuccessDigest);
            Check(failures, "MAP19_04", "DECLARED_MAP19_04_DIGEST_MISMATCH",
                "DECLARED_MAP19_04", input.ClusterValidation.CombinedSuccessDigest,
                input.DeclaredMap19_04CombinedDigest);
            Check(failures, "MAP19_05", "DECLARED_MAP19_05_DIGEST_MISMATCH",
                "DECLARED_MAP19_05", input.RepetitionValidation.CombinedSuccessDigest,
                input.DeclaredMap19_05CombinedDigest);
            Check(failures, "MAP19_06", "DECLARED_MAP19_06_DIGEST_MISMATCH",
                "DECLARED_MAP19_06", input.WorstCaseValidation.CombinedSuccessDigest,
                input.DeclaredMap19_06CombinedDigest);
            Check(failures, "MAP19_06", "INCOMING_HANDOFF_DIGEST_MISMATCH",
                "INCOMING_HANDOFF", ExpectedIncomingHandoffDigest,
                input.DeclaredIncomingHandoffDigest);
            Check(failures, "MAP19_06", "INCOMING_HANDOFF_BINDING_MISMATCH",
                "ACTUAL_HANDOFF", input.WorstCaseValidation.Map19_07HandoffDigest,
                input.DeclaredIncomingHandoffDigest);

            if (input.ThresholdRegistry == null)
                Add(failures, "MAP19_07", "MISSING_THRESHOLD_REGISTRY", "REGISTRY", 0,
                    "SHARED_REGISTRY", "PRESENT", "MISSING", input.ComputeDigest(),
                    "DISTANCE_AND_REVISIT_THRESHOLDS");
            else
            {
                var locked = GeneratedDistancePacingThresholdRegistry.CreateLocked();
                Check(failures, "MAP19_07", "THRESHOLD_REGISTRY_MISMATCH",
                    "REGISTRY", locked.Digest, input.ThresholdRegistry.Digest);
            }

            var expectedClasses = Enum.GetValues(typeof(GeneratedDistanceRouteClass))
                .Cast<GeneratedDistanceRouteClass>().ToArray();
            foreach (var routeClass in expectedClasses)
                if (input.Routes.Count(value => value.RouteClass == routeClass) != 1)
                    Add(failures, "MAP19_07", "MISSING_ROUTE_CLASS",
                        "ROUTE_CLASS_" + routeClass, routeClass,
                        routeClass.ToString(), "EXACTLY_ONE",
                        Number(input.Routes.Count(value => value.RouteClass == routeClass)),
                        input.ComputeDigest(), "ROUTE_CLASS_BINDING");

            if (input.Markers.Count == 0)
                Add(failures, "MAP19_07", "MISSING_MARKER_BINDING", "MARKERS", 0,
                    "PACING_MARKERS", "NON_EMPTY", "EMPTY", input.ComputeDigest(),
                    "MARKER_SET");
            foreach (var markerKind in Enum.GetValues(typeof(GeneratedPacingMarkerKind))
                .Cast<GeneratedPacingMarkerKind>())
                if (input.Markers.Count(value => value.Kind == markerKind) == 0)
                    Add(failures, "MAP19_07", "MISSING_MARKER_BINDING",
                        "MARKER_KIND_" + markerKind, 0, markerKind.ToString(),
                        "AT_LEAST_ONE", "ZERO", input.ComputeDigest(),
                        "MARKER_KIND_BINDING");
            foreach (var duplicate in input.Markers.GroupBy(value => value.MarkerId,
                StringComparer.Ordinal).Where(value => value.Count() > 1))
                Add(failures, "MAP19_07", "DUPLICATE_MARKER_BINDING", "MARKERS", 0,
                    duplicate.Key, "UNIQUE_MARKER_ID", Number(duplicate.Count()),
                    input.ComputeDigest(), "MARKER_SET");
            if (input.ActionAudit == null || !input.ActionAudit.IsZero)
                Add(failures, "MAP19_07", "FORBIDDEN_ACTION_ATTEMPT", "AUDIT", 0,
                    "PURE_DATA_READ_ONLY", "ALL_ZERO", input.ActionAudit == null ?
                        "MISSING" : input.ActionAudit.StableToken, input.ComputeDigest(),
                    "NO_SEED_BATCH_FAILURE_BUNDLE_HEADLESS_RUNTIME_OR_MUTATION");
            if (input.ActionAudit != null && input.ActionAudit.Map19_08Started)
                Add(failures, "MAP19_08", "MAP19_08_START_ATTEMPT", "BOUNDARY", 0,
                    "MAP19_08_STARTED", "FALSE", "TRUE", input.ComputeDigest(),
                    "MAP19_08_LOCKED");
            return failures.Count == 0;
        }

        private static void ValidateRoute(GeneratedDistancePacingInput input,
            GeneratedDistanceRouteBinding route,
            ICollection<GeneratedDistanceRouteProof> proofs,
            ICollection<GeneratedDistancePacingFailure> failures)
        {
            var before = failures.Count;
            if (route.Steps.Count == 0)
            {
                Add(failures, route.SourceOwner, "EMPTY_ROUTE", route.RouteId,
                    route.RouteClass, "STEPS", "NON_EMPTY", "EMPTY",
                    route.SourceDigest, route.StableToken);
                return;
            }

            var graphEdges = input.Graph.Edges.ToDictionary(value => value.EdgeId,
                StringComparer.Ordinal);
            var graphLinks = input.Graph.SocketLinks.ToDictionary(value => value.LinkId,
                StringComparer.Ordinal);
            var graphNodes = new HashSet<string>(input.Graph.Nodes.Select(value =>
                value.NodeId), StringComparer.Ordinal);
            var visits = new List<string> { route.StartNodeId };
            var cursor = route.StartNodeId;
            foreach (var step in route.Steps)
            {
                var edgeBound = step.Edge != null && graphEdges.TryGetValue(
                    step.Edge.EdgeId, out var graphEdge) && string.Equals(
                    graphEdge.StableToken, step.Edge.StableToken,
                    StringComparison.Ordinal);
                var linkBound = step.SocketLink != null && graphLinks.TryGetValue(
                    step.SocketLink.LinkId, out var graphLink) && string.Equals(
                    graphLink.StableToken, step.SocketLink.StableToken,
                    StringComparison.Ordinal);
                if (edgeBound == linkBound)
                    Add(failures, step.SourceOwner, "MISSING_GRAPH_EDGE_BINDING",
                        route.RouteId, route.RouteClass, step.StepId,
                        "EXACTLY_ONE_GRAPH_EDGE_OR_SOCKET_LINK", step.TraversalId,
                        step.SourceDigest, step.StableToken);
                else if (!string.Equals(cursor, step.FromNodeId,
                    StringComparison.Ordinal))
                    Add(failures, step.SourceOwner, "DISCONTINUOUS_ROUTE",
                        route.RouteId, route.RouteClass, step.StepId, cursor,
                        step.FromNodeId, step.SourceDigest, step.StableToken);
                if (step.Ordinal < 0 || step.TileStepMultiplier <= 0m)
                    Add(failures, step.SourceOwner, "INVALID_ROUTE_STEP",
                        route.RouteId, route.RouteClass, step.StepId,
                        "NON_NEGATIVE_ORDINAL_AND_POSITIVE_MULTIPLIER",
                        step.Ordinal + "|" + GeneratedDistanceRange.Number(
                            step.TileStepMultiplier), step.SourceDigest, step.StableToken);
                if (edgeBound || linkBound)
                {
                    cursor = step.ToNodeId;
                    visits.Add(cursor);
                }
            }
            if (!graphNodes.Contains(route.StartNodeId) ||
                !graphNodes.Contains(route.ExitNodeId) ||
                !string.Equals(cursor, route.ExitNodeId, StringComparison.Ordinal))
                Add(failures, route.SourceOwner, "ROUTE_ENDPOINT_MISMATCH",
                    route.RouteId, route.RouteClass, "ENDPOINTS",
                    route.StartNodeId + "->" + route.ExitNodeId,
                    route.StartNodeId + "->" + cursor, route.SourceDigest,
                    route.StableToken);
            if (!string.Equals(route.CompletionProofDigest,
                    input.CompletionSearch.SuccessProofDigest, StringComparison.Ordinal))
                Add(failures, route.SourceOwner, "COMPLETION_BINDING_MISMATCH",
                    route.RouteId, route.RouteClass, "COMPLETION_PROOF",
                    input.CompletionSearch.SuccessProofDigest,
                    route.CompletionProofDigest, route.SourceDigest, route.StableToken);
            if (route.RouteClass == GeneratedDistanceRouteClass.WorstCaseCompletion &&
                (route.WorstCaseProof == null || !route.WorstCaseProof.Passed ||
                 !input.WorstCaseValidation.Surface.Proofs.Any(value =>
                     string.Equals(value.ProofDigest, route.WorstCaseProof.ProofDigest,
                         StringComparison.Ordinal))))
                Add(failures, route.SourceOwner, "WORST_CASE_PROOF_BINDING_MISMATCH",
                    route.RouteId, route.RouteClass, "WORST_CASE_PROOF", "BOUND_PASS",
                    route.WorstCaseProof == null ? "MISSING" :
                        route.WorstCaseProof.ProofDigest, route.SourceDigest,
                    route.StableToken);

            if (failures.Count != before) return;

            var distance = route.Steps.Sum(value => value.TileStepEquivalentCost);
            var range = input.ThresholdRegistry.Range(route.RouteClass);
            if (range != null && !range.Contains(distance))
                Add(failures, route.SourceOwner, DistanceReason(route.RouteClass),
                    route.RouteId, route.RouteClass, "DISTANCE",
                    range.StableToken, GeneratedDistanceRange.Number(distance),
                    route.SourceDigest, route.StableToken);

            var groupedCorridors = route.Steps.Where(value => !value.DeliberateReturnPath)
                .GroupBy(value => string.Join("|", new[] { value.CorridorSignature,
                    value.SourceOwner, value.LocalDirectionBand }), StringComparer.Ordinal);
            var repeated = groupedCorridors.Sum(group => Math.Max(0, group.Count() - 1));
            var repeatedRatio = GeneratedDistanceRouteProof.Ratio(repeated,
                route.Steps.Count);
            if (repeatedRatio > input.ThresholdRegistry.RepeatedCorridorMaximumBasisPoints)
                Add(failures, route.SourceOwner, "REPEATED_CORRIDOR_RATIO_EXCEEDED",
                    route.RouteId, route.RouteClass, "REPEATED_CORRIDOR_RATIO_BP",
                    "<=" + Number(input.ThresholdRegistry.
                        RepeatedCorridorMaximumBasisPoints), Number(repeatedRatio),
                    route.SourceDigest, string.Join(",", groupedCorridors.Where(group =>
                        group.Count() > 1).Select(group => group.Key)));

            if (failures.Count != before) return;
            var uniqueNodes = visits.Distinct(StringComparer.Ordinal).Count();
            var uniqueEdges = route.Steps.Select(value => value.TraversalId)
                .Distinct(StringComparer.Ordinal).Count();
            proofs.Add(new GeneratedDistanceRouteProof(route, distance, uniqueNodes,
                visits.Count, visits.Count - uniqueNodes, uniqueEdges, route.Steps.Count,
                repeated, repeatedRatio));
        }

        private static List<GeneratedPacingMarkerProof> ValidateMarkers(
            GeneratedDistancePacingInput input,
            ICollection<GeneratedDistancePacingFailure> failures)
        {
            var located = new List<LocatedMarker>();
            foreach (var marker in input.Markers)
            {
                var route = input.Routes.SingleOrDefault(value => string.Equals(
                    value.RouteId, marker.RouteId, StringComparison.Ordinal));
                var step = route == null ? null : route.Steps.SingleOrDefault(value =>
                    string.Equals(value.StepId, marker.StepId, StringComparison.Ordinal));
                if (route == null || step == null || marker.OffsetWithinStep < 0m ||
                    marker.OffsetWithinStep > (step == null ? 0m :
                        step.TileStepEquivalentCost))
                {
                    Add(failures, marker.SourceOwner, "MISSING_MARKER_BINDING",
                        marker.MarkerId, route == null ? 0 : route.RouteClass,
                        marker.StepId, "ROUTE_STEP_AND_VALID_OFFSET",
                        route == null ? "MISSING_ROUTE" : step == null ?
                            "MISSING_STEP" : GeneratedDistanceRange.Number(
                                marker.OffsetWithinStep), marker.SourceDigest,
                        marker.StableToken);
                    continue;
                }
                if (!ValidMarkerContract(input, marker))
                {
                    Add(failures, marker.SourceOwner, "MISSING_MARKER_BINDING",
                        marker.MarkerId, route.RouteClass, marker.ContractIdentity,
                        "UPSTREAM_CONTRACT_BINDING", "MISSING_OR_MISMATCH",
                        marker.SourceDigest, marker.StableToken);
                    continue;
                }
                var distance = route.Steps.TakeWhile(value => !string.Equals(value.StepId,
                        step.StepId, StringComparison.Ordinal))
                    .Sum(value => value.TileStepEquivalentCost) + marker.OffsetWithinStep;
                located.Add(new LocatedMarker(marker, route, distance));
            }

            foreach (var group in located.GroupBy(value => value.Route.RouteId,
                StringComparer.Ordinal))
            {
                foreach (var sameKind in group.GroupBy(value => value.Marker.Kind))
                {
                    var ordered = sameKind.OrderBy(value => value.Distance)
                        .ThenBy(value => value.Marker.MarkerId, StringComparer.Ordinal).ToArray();
                    for (var index = 1; index < ordered.Length; index++)
                    {
                        var gap = ordered[index].Distance - ordered[index - 1].Distance;
                        if (gap < input.ThresholdRegistry.SameKindPacingMinimumGap)
                            Add(failures, ordered[index].Marker.SourceOwner,
                                "PACING_MARKER_CLUSTER", ordered[index].Marker.MarkerId,
                                ordered[index].Route.RouteClass,
                                ordered[index].Marker.Kind.ToString(),
                                ">=" + GeneratedDistanceRange.Number(input.
                                    ThresholdRegistry.SameKindPacingMinimumGap),
                                GeneratedDistanceRange.Number(gap),
                                ordered[index].Marker.SourceDigest,
                                ordered[index - 1].Marker.MarkerId + "->" +
                                    ordered[index].Marker.MarkerId);
                    }
                }
            }

            if (failures.Count != 0) return new List<GeneratedPacingMarkerProof>();
            var proofs = new List<GeneratedPacingMarkerProof>();
            foreach (var group in located.GroupBy(value => value.Route.RouteId,
                StringComparer.Ordinal))
            {
                var ordered = group.OrderBy(value => value.Distance).ThenBy(value =>
                    value.Marker.MarkerId, StringComparer.Ordinal).ToArray();
                for (var index = 0; index < ordered.Length; index++)
                    proofs.Add(new GeneratedPacingMarkerProof(ordered[index].Marker,
                        ordered[index].Distance, index == 0 ? 0m :
                            ordered[index].Distance - ordered[index - 1].Distance,
                        index == ordered.Length - 1 ? 0m :
                            ordered[index + 1].Distance - ordered[index].Distance));
            }
            return proofs;
        }

        private static bool ValidMarkerContract(GeneratedDistancePacingInput input,
            GeneratedDistancePacingMarker marker)
        {
            switch (marker.Kind)
            {
                case GeneratedPacingMarkerKind.Activity:
                    return marker.RepetitionSignature != null &&
                        marker.RepetitionSignature.Kind == GeneratedRepetitionSourceKind.
                            ActivityStructure && !marker.RequiredForCompletion;
                case GeneratedPacingMarkerKind.EventOverlay:
                    return marker.RepetitionSignature != null &&
                        marker.RepetitionSignature.Kind == GeneratedRepetitionSourceKind.
                            EventOverlay && !marker.RequiredForCompletion;
                case GeneratedPacingMarkerKind.Village:
                    return marker.VillageStateMarkers != null &&
                        input.WorstCaseValidation.Surface.Proofs.Count != 0 &&
                        !marker.RequiredForCompletion;
                case GeneratedPacingMarkerKind.Exit:
                    return !marker.RequiredForCompletion;
                default:
                    return marker.CompletionTransition != null &&
                        input.CompletionSearch.Proof.ShortestActions.Contains(
                            "STATE|" + marker.CompletionTransition.TransitionId);
            }
        }

        private static string DistanceReason(GeneratedDistanceRouteClass routeClass)
        {
            switch (routeClass)
            {
                case GeneratedDistanceRouteClass.MinimumCritical:
                    return "MINIMUM_DISTANCE_OUTSIDE_RANGE";
                case GeneratedDistanceRouteClass.NormalCompletion:
                    return "NORMAL_DISTANCE_OUTSIDE_RANGE";
                default:
                    return "OPTIONAL_DISTANCE_OUTSIDE_RANGE";
            }
        }

        private static void Check(ICollection<GeneratedDistancePacingFailure> failures,
            string owner, string reason, string measurementId, string expected,
            string actual)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                Add(failures, owner, reason, measurementId, 0, measurementId,
                    expected, actual, actual, "DIGEST_BINDING");
        }

        private static void Add(ICollection<GeneratedDistancePacingFailure> failures,
            string owner, string reason, string measurementId,
            GeneratedDistanceRouteClass routeClass, string offendingKey,
            string expected, string actual, string sourceDigest, string evidence) =>
            failures.Add(new GeneratedDistancePacingFailure(owner, reason, measurementId,
                routeClass, offendingKey, expected, actual, sourceDigest, evidence));

        private static GeneratedDistancePacingResult Failure(
            IEnumerable<GeneratedDistancePacingFailure> failures) =>
            new GeneratedDistancePacingResult(null, failures);

        private static string Number(int value) =>
            value.ToString(CultureInfo.InvariantCulture);

        private sealed class LocatedMarker
        {
            public LocatedMarker(GeneratedDistancePacingMarker marker,
                GeneratedDistanceRouteBinding route, decimal distance)
            {
                Marker = marker;
                Route = route;
                Distance = distance;
            }
            public GeneratedDistancePacingMarker Marker { get; }
            public GeneratedDistanceRouteBinding Route { get; }
            public decimal Distance { get; }
        }
    }
}
