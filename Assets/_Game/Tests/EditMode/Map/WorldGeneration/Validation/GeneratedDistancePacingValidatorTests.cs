using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Validation;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Validation
{
    [TestFixture]
    [Category("MAP19_07")]
    public sealed class GeneratedDistancePacingValidatorTests
    {
        private static UpstreamRun cachedUpstream;

        [Test]
        public void DistancePacingValidatorMeasuresMinimumNormalOptionalAndWorstCaseRoutes()
        {
            var result = Run().Result;
            AssertSuccess(result);
            Assert.That(result.Surface.RouteClassesMeasured, Is.EqualTo(4));
            Assert.That(result.Surface.Route(GeneratedDistanceRouteClass.MinimumCritical)
                .Distance, Is.EqualTo(600m));
            Assert.That(result.Surface.Route(GeneratedDistanceRouteClass.NormalCompletion)
                .Distance, Is.EqualTo(1000m));
            Assert.That(result.Surface.Route(GeneratedDistanceRouteClass.OptionalCompletion)
                .Distance, Is.EqualTo(1800m));
            Assert.That(result.Surface.Route(GeneratedDistanceRouteClass.WorstCaseCompletion)
                .Distance, Is.EqualTo(800m));
            Assert.That(result.Surface.DistanceRangeViolations, Is.Zero);
        }

        [Test]
        public void DistancePacingValidatorUsesSharedThresholdRegistryWithoutDuplicateConstants()
        {
            var run = Run();
            AssertSuccess(run.Result);
            var registry = run.Input.ThresholdRegistry;
            Assert.That(registry.MinimumCritical.StableToken, Is.EqualTo("500..900"));
            Assert.That(registry.NormalCompletion.StableToken, Is.EqualTo("800..1400"));
            Assert.That(registry.OptionalCompletion.StableToken, Is.EqualTo("1500..2800"));
            Assert.That(registry.RepeatedCorridorMaximumBasisPoints, Is.EqualTo(3500));
            Assert.That(registry.Digest, Is.EqualTo(
                GeneratedDistancePacingThresholdRegistry.CreateLocked().Digest));
            var socket = Upstream().WorstInput.Graph.SocketLinks.First();
            var socketStep = new GeneratedDistanceRouteStep("SOCKET_FALLBACK", 0,
                socket, 1m, socket.LinkId, "MAP19_02_GRAPH", "SOCKET",
                false, socket.ProvenanceDigest);
            Assert.That(socketStep.TileStepEquivalentCost, Is.EqualTo(1m));
        }

        [Test]
        public void DistancePacingValidatorComputesRevisitAndRepeatedCorridorRatio()
        {
            var proof = Run().Result.Surface.RevisitReference;
            Assert.That(proof.UniqueRouteNodes, Is.EqualTo(9));
            Assert.That(proof.TotalRouteNodeVisits, Is.EqualTo(9));
            Assert.That(proof.RevisitedNodeCount, Is.Zero);
            Assert.That(proof.RevisitRatioBasisPoints, Is.Zero);
            Assert.That(proof.UniqueRouteEdges, Is.EqualTo(8));
            Assert.That(proof.TotalRouteEdgeVisits, Is.EqualTo(8));
            Assert.That(proof.RepeatedCorridorEdgeCount, Is.EqualTo(2));
            Assert.That(proof.RepeatedCorridorRatioBasisPoints, Is.EqualTo(2500));
            Assert.That(proof.RepeatedCorridorRatioBasisPoints, Is.LessThanOrEqualTo(3500));
        }

        [Test]
        public void DistancePacingValidatorBindsActivityEventSpecialAndMandatoryMarkersToRoute()
        {
            var run = Run();
            AssertSuccess(run.Result);
            var surface = run.Result.Surface;
            Assert.That(surface.PacingMarkersChecked, Is.EqualTo(12));
            Assert.That(surface.MandatoryResourceMarkers, Is.EqualTo(3));
            Assert.That(surface.ActivityMarkers, Is.EqualTo(1));
            Assert.That(surface.EventMarkers, Is.EqualTo(1));
            Assert.That(surface.SpecialMarkers, Is.EqualTo(2));
            Assert.That(surface.VillageForgeSealBossExitMarkers, Is.EqualTo(5));
            Assert.That(surface.MarkerBindingFailures, Is.Zero);
            Assert.That(surface.PacingClusterViolations, Is.Zero);
            Assert.That(run.Input.Markers.Where(value => value.Kind ==
                GeneratedPacingMarkerKind.Activity || value.Kind ==
                GeneratedPacingMarkerKind.EventOverlay).All(value =>
                    !value.RequiredForCompletion && value.RepetitionSignature != null),
                Is.True);
            Assert.That(run.Input.Markers.Where(value => value.RequiredForCompletion)
                .All(value => value.CompletionTransition != null), Is.True);
        }

        [Test]
        public void DistancePacingValidatorConsumesMap19_02ToMap19_06SurfacesReadOnly()
        {
            var upstream = Upstream();
            var before = UpstreamDigests(upstream);
            var run = Run();
            AssertSuccess(run.Result);
            Assert.That(run.Input.Graph, Is.SameAs(upstream.WorstInput.Graph));
            Assert.That(run.Input.CompletionSearch,
                Is.SameAs(upstream.WorstInput.CompletionSearch));
            Assert.That(run.Input.ClusterValidation,
                Is.SameAs(upstream.WorstInput.ClusterValidation));
            Assert.That(run.Input.RepetitionValidation,
                Is.SameAs(upstream.WorstInput.RepetitionValidation));
            Assert.That(run.Input.WorstCaseValidation, Is.SameAs(upstream.WorstResult));
            Assert.That(UpstreamDigests(upstream), Is.EqualTo(before));
            Assert.That(run.Input.ActionAudit.IsZero, Is.True);
        }

        [Test]
        public void DistancePacingValidatorRejectsMissingHandoffDigestMismatchAndMissingBindings()
        {
            AssertAtomic(Run(omitWorstCase: true).Result, "MISSING_MAP19_06_HANDOFF");
            AssertAtomic(Run(declaredHandoff: new string('f', 64)).Result,
                "INCOMING_HANDOFF_DIGEST_MISMATCH");
            var markers = Markers(Routes()).ToList();
            var source = markers[0];
            markers[0] = new GeneratedDistancePacingMarker(source.MarkerId, source.Kind,
                source.RouteId, "MISSING_STEP", source.OffsetWithinStep,
                source.PacingBand, source.SourceOwner, source.SourceDigest,
                source.RequiredForCompletion, source.CompletionTransition,
                source.RepetitionSignature, source.VillageStateMarkers);
            AssertAtomic(Run(markers: markers).Result, "MISSING_MARKER_BINDING");
        }

        [Test]
        public void DistancePacingValidatorFailuresAreAtomicAndReportOwnerReasonExpectedActual()
        {
            var badRoutes = Routes(minimumDistance: 100m);
            var distanceFailure = Run(routes: badRoutes,
                markers: Markers(badRoutes)).Result;
            AssertAtomic(distanceFailure, "MINIMUM_DISTANCE_OUTSIDE_RANGE");
            Assert.That(distanceFailure.Failures.All(value =>
                !string.IsNullOrEmpty(value.Owner) &&
                !string.IsNullOrEmpty(value.Reason) &&
                !string.IsNullOrEmpty(value.MeasurementId) &&
                !string.IsNullOrEmpty(value.OffendingKey) &&
                !string.IsNullOrEmpty(value.Expected) &&
                !string.IsNullOrEmpty(value.Actual) &&
                !string.IsNullOrEmpty(value.SourceDigest) &&
                !string.IsNullOrEmpty(value.RouteOrMarkerEvidence)), Is.True);

            var repeatedRoutes = Routes(optionalRepeatedCorridorPrefix: 4);
            AssertAtomic(Run(routes: repeatedRoutes,
                markers: Markers(repeatedRoutes)).Result,
                "REPEATED_CORRIDOR_RATIO_EXCEEDED");

            var clusteredMarkers = Markers(Routes()).ToList();
            var normal = Routes().Single(value => value.RouteClass ==
                GeneratedDistanceRouteClass.NormalCompletion);
            var clustered = clusteredMarkers[1];
            clusteredMarkers[1] = Marker(normal, "MANDATORY_2_CLUSTERED",
                GeneratedPacingMarkerKind.MandatoryResource, 70m, true,
                completion: clustered.CompletionTransition);
            AssertAtomic(Run(markers: clusteredMarkers).Result,
                "PACING_MARKER_CLUSTER");

            AssertAtomic(Run(actionAudit: new GeneratedDistancePacingActionAudit(
                seedBatchAttempts: 1, failureBundleAttempts: 1,
                headlessRunnerAttempts: 1)).Result, "FORBIDDEN_ACTION_ATTEMPT");
        }

        [Test]
        public void DistancePacingValidatorDigestIsStableAcrossRepeatReverseCultureAndMarkerOrder()
        {
            var first = Run();
            var repeat = Run();
            var reverseRoutes = Routes().Reverse().ToArray();
            var reverse = Run(routes: reverseRoutes,
                markers: Markers(reverseRoutes).Reverse());
            AssertSuccess(first.Result);
            AssertSuccess(repeat.Result);
            AssertSuccess(reverse.Result);
            Assert.That(repeat.Result.CombinedSuccessDigest,
                Is.EqualTo(first.Result.CombinedSuccessDigest));
            Assert.That(reverse.Result.CombinedSuccessDigest,
                Is.EqualTo(first.Result.CombinedSuccessDigest));

            var priorCulture = CultureInfo.CurrentCulture;
            var priorUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                Assert.That(Run().Result.CombinedSuccessDigest,
                    Is.EqualTo(first.Result.CombinedSuccessDigest));
            }
            finally
            {
                CultureInfo.CurrentCulture = priorCulture;
                CultureInfo.CurrentUICulture = priorUiCulture;
            }

            var mutatedRoutes = Routes(minimumDistance: 650m);
            var mutation = Run(routes: mutatedRoutes, markers: Markers(mutatedRoutes));
            AssertSuccess(mutation.Result);
            Assert.That(mutation.Result.Surface.InputDigest,
                Is.Not.EqualTo(first.Result.Surface.InputDigest));
            Assert.That(mutation.Result.DistanceSuccessDigest,
                Is.Not.EqualTo(first.Result.DistanceSuccessDigest));
            Assert.That(mutation.Result.Map19_08HandoffDigest,
                Is.Not.EqualTo(first.Result.Map19_08HandoffDigest));
        }

        [Test]
        public void DistancePacingValidatorPublishesMap19_08HandoffSurface()
        {
            var result = Run().Result;
            AssertSuccess(result);
            foreach (var digest in new[]
            {
                result.Surface.InputDigest, result.DistanceSuccessDigest,
                result.RevisitSuccessDigest, result.PacingSuccessDigest,
                result.CombinedSuccessDigest, result.Map19_08HandoffDigest,
            })
                Assert.That(BakingCanonicalDigest.IsLowerHexSha256(digest), Is.True,
                    digest);
        }

        [Test]
        public void Map19HandoffKeepsMap19_08Locked()
        {
            var run = Run();
            AssertSuccess(run.Result);
            Assert.That(run.Input.ActionAudit.Map19_08Started, Is.False);
            Assert.That(run.Result.Surface.Map19_08Started, Is.False);
            Assert.That(run.Result.Surface.WorldScaleSourceAvailable, Is.False);
            Assert.That(run.Result.Surface.FocusedFixtureSourceKind,
                Is.EqualTo("SCALED_GRAPH_BACKED_FOCUSED_ROUTE_FIXTURE"));
            Assert.That(run.Result.Surface.WorldScaleThresholdApprovalPerformed, Is.False);
            WriteEvidence(run.Result);
        }

        private static ValidationRun Run(
            IEnumerable<GeneratedDistanceRouteBinding> routes = null,
            IEnumerable<GeneratedDistancePacingMarker> markers = null,
            GeneratedDistancePacingActionAudit actionAudit = null,
            string declaredMap19_04 = null, string declaredMap19_05 = null,
            string declaredMap19_06 = null, string declaredHandoff = null,
            bool omitWorstCase = false)
        {
            var upstream = Upstream();
            var selectedRoutes = (routes ?? Routes()).ToArray();
            var input = new GeneratedDistancePacingInput(upstream.WorstInput.Graph,
                upstream.WorstInput.CompletionSearch,
                upstream.WorstInput.ClusterValidation,
                upstream.WorstInput.RepetitionValidation,
                omitWorstCase ? null : upstream.WorstResult,
                GeneratedDistancePacingThresholdRegistry.CreateLocked(), selectedRoutes,
                markers ?? Markers(selectedRoutes), actionAudit,
                declaredMap19_04, declaredMap19_05, declaredMap19_06, declaredHandoff);
            return new ValidationRun(input,
                GeneratedDistancePacingValidator.Validate(input));
        }

        private static GeneratedDistanceRouteBinding[] Routes(
            decimal minimumDistance = 600m,
            int optionalRepeatedCorridorPrefix = 3)
        {
            var upstream = Upstream();
            var graph = upstream.WorstInput.Graph;
            var forward = Enumerable.Range(0, 8).Select(index => Edge(graph,
                index, index + 1)).ToArray();
            var combined = upstream.WorstResult.Surface.Proofs.Single(value =>
                value.Kind == GeneratedWorstCaseScenarioKind.CombinedAdverseStaticShell);
            return new[]
            {
                Route(GeneratedDistanceRouteClass.MinimumCritical, "ROUTE_MINIMUM",
                    forward, minimumDistance),
                Route(GeneratedDistanceRouteClass.NormalCompletion, "ROUTE_NORMAL",
                    forward, 1000m),
                Route(GeneratedDistanceRouteClass.OptionalCompletion, "ROUTE_OPTIONAL",
                    forward, 1800m,
                    repeatedCorridorPrefix: optionalRepeatedCorridorPrefix),
                Route(GeneratedDistanceRouteClass.WorstCaseCompletion, "ROUTE_WORST",
                    forward, 800m, worstCaseProof: combined),
            };
        }

        private static GeneratedDistanceRouteBinding Route(
            GeneratedDistanceRouteClass routeClass, string routeId,
            IEnumerable<GeneratedTileMovementEdge> sourceEdges, decimal targetDistance,
            int repeatedCorridorPrefix = 0,
            GeneratedWorstCaseScenarioProof worstCaseProof = null)
        {
            var edges = sourceEdges.ToArray();
            var rawDistance = edges.Sum(value => value.Cost);
            var multiplier = targetDistance / rawDistance;
            var steps = edges.Select((edge, ordinal) => new GeneratedDistanceRouteStep(
                routeId, ordinal, edge, multiplier,
                ordinal < repeatedCorridorPrefix ? "OPTIONAL_SHARED_CORRIDOR" :
                    edge.EdgeId, "MAP19_02_GRAPH",
                ordinal < repeatedCorridorPrefix ? "FORWARD" :
                    edge.FromNodeId + "->" + edge.ToNodeId,
                false,
                Upstream().WorstInput.Graph.GraphDigest)).ToArray();
            return new GeneratedDistanceRouteBinding(routeClass, routeId,
                edges[0].FromNodeId, edges[edges.Length - 1].ToNodeId, steps,
                routeClass == GeneratedDistanceRouteClass.WorstCaseCompletion ?
                    "MAP19_06_WORST_CASE" : "MAP19_03_COMPLETION",
                routeClass == GeneratedDistanceRouteClass.WorstCaseCompletion ?
                    Upstream().WorstResult.CombinedSuccessDigest :
                    Upstream().WorstInput.CompletionSearch.SuccessProofDigest,
                Upstream().WorstInput.CompletionSearch.SuccessProofDigest,
                worstCaseProof);
        }

        private static GeneratedDistancePacingMarker[] Markers(
            IEnumerable<GeneratedDistanceRouteBinding> routes)
        {
            var route = routes.Single(value => value.RouteClass ==
                GeneratedDistanceRouteClass.NormalCompletion);
            var input = Upstream().WorstInput;
            var activityInput = Upstream().RepetitionInput;
            var required = input.Transitions.Select(value => value.Transition)
                .Where(value => input.CompletionSearch.Proof.ShortestActions.Contains(
                    "STATE|" + value.TransitionId)).ToArray();
            var resources = required.Where(value => value.Kind ==
                GeneratedCompletionTransitionKind.CollectMandatoryResource).ToArray();
            var activity = activityInput.Signatures.First(value => value.Kind ==
                GeneratedRepetitionSourceKind.ActivityStructure);
            var eventOverlay = activityInput.Signatures.First(value => value.Kind ==
                GeneratedRepetitionSourceKind.EventOverlay);
            return new[]
            {
                Marker(route, "MANDATORY_1", GeneratedPacingMarkerKind.MandatoryResource,
                    50m, true, completion: resources[0]),
                Marker(route, "MANDATORY_2", GeneratedPacingMarkerKind.MandatoryResource,
                    200m, true, completion: resources[1]),
                Marker(route, "MANDATORY_3", GeneratedPacingMarkerKind.MandatoryResource,
                    350m, true, completion: resources[2]),
                Marker(route, "ACTIVITY", GeneratedPacingMarkerKind.Activity, 440m,
                    false, repetition: activity),
                Marker(route, "EVENT", GeneratedPacingMarkerKind.EventOverlay, 520m,
                    false, repetition: eventOverlay),
                Marker(route, "SPECIAL_ENTRY", GeneratedPacingMarkerKind.SpecialEntry,
                    600m, true, completion: Transition(required,
                        GeneratedCompletionTransitionKind.EnterSpecial)),
                Marker(route, "SPECIAL_REWARD", GeneratedPacingMarkerKind.SpecialReward,
                    680m, true, completion: Transition(required,
                        GeneratedCompletionTransitionKind.ResolveSpecial)),
                Marker(route, "VILLAGE", GeneratedPacingMarkerKind.Village, 760m,
                    false, village: input.VillageStateMarkers),
                Marker(route, "FORGE", GeneratedPacingMarkerKind.Forge, 820m, true,
                    completion: Transition(required,
                        GeneratedCompletionTransitionKind.ActivateForge)),
                Marker(route, "SEAL", GeneratedPacingMarkerKind.Seal, 880m, true,
                    completion: Transition(required,
                        GeneratedCompletionTransitionKind.AcceptSeal)),
                Marker(route, "BOSS", GeneratedPacingMarkerKind.Boss, 940m, true,
                    completion: Transition(required,
                        GeneratedCompletionTransitionKind.DefeatBoss)),
                Marker(route, "EXIT", GeneratedPacingMarkerKind.Exit, 1000m, false),
            };
        }

        private static GeneratedDistancePacingMarker Marker(
            GeneratedDistanceRouteBinding route, string id,
            GeneratedPacingMarkerKind kind, decimal routeDistance, bool required,
            GeneratedCompletionStateTransition completion = null,
            GeneratedRepetitionSignature repetition = null,
            StarNight.Map.WorldGeneration.SpecialRegions.VillageStateMarkerSetDefinition
                village = null)
        {
            var elapsed = 0m;
            var step = route.Steps.Last();
            var offset = step.TileStepEquivalentCost;
            foreach (var candidate in route.Steps)
            {
                if (routeDistance <= elapsed + candidate.TileStepEquivalentCost)
                {
                    step = candidate;
                    offset = routeDistance - elapsed;
                    break;
                }
                elapsed += candidate.TileStepEquivalentCost;
            }
            return new GeneratedDistancePacingMarker("MARKER_" + id, kind,
                route.RouteId, step.StepId, offset, "PACING_" + kind.ToString().ToUpperInvariant(),
                repetition == null ? "MAP19_03_COMPLETION" : repetition.SourceOwner,
                repetition == null ? route.SourceDigest : repetition.SourceDigest,
                required, completion, repetition, village);
        }

        private static GeneratedCompletionStateTransition Transition(
            IEnumerable<GeneratedCompletionStateTransition> transitions,
            GeneratedCompletionTransitionKind kind) =>
            transitions.Single(value => value.Kind == kind);

        private static GeneratedTileMovementEdge Edge(GeneratedTileMovementGraph graph,
            int fromX, int toX)
        {
            var from = Node(graph, fromX).NodeId;
            var to = Node(graph, toX).NodeId;
            return graph.Edges.Where(value => value.FromNodeId == from &&
                value.ToNodeId == to).OrderBy(value => value.EdgeId,
                StringComparer.Ordinal).First();
        }

        private static GeneratedTileMovementNode Node(GeneratedTileMovementGraph graph,
            int x) => graph.Nodes.Single(value => value.Kind ==
                GeneratedTileMovementNodeKind.Stand && value.Cell.X == x &&
                value.Cell.Y == 0);

        private static UpstreamRun Upstream()
        {
            if (cachedUpstream != null) return cachedUpstream;
            var worstMethod = typeof(GeneratedWorstCaseScenarioValidatorTests).GetMethod(
                "Run", BindingFlags.Static | BindingFlags.NonPublic);
            var worstRaw = worstMethod.Invoke(null, new object[]
                { null, null, null, null, null, null, null, false });
            var worstInput = Property<GeneratedWorstCaseScenarioInput>(worstRaw, "Input");
            var worstResult = Property<GeneratedWorstCaseScenarioResult>(worstRaw, "Result");
            Assert.That(worstResult.Success, Is.True,
                string.Join("\n", worstResult.Failures));

            var repetitionMethod = typeof(
                GeneratedRepetitionEventRemovalValidatorTests).GetMethod("Run",
                    BindingFlags.Static | BindingFlags.NonPublic);
            var repetitionRaw = repetitionMethod.Invoke(null, new object[]
                { null, null, null, null, null, null, null, false });
            cachedUpstream = new UpstreamRun(worstInput, worstResult,
                Property<GeneratedRepetitionValidationInput>(repetitionRaw,
                    "RepetitionInput"));
            return cachedUpstream;
        }

        private static T Property<T>(object source, string name) => (T)source.GetType()
            .GetProperty(name, BindingFlags.Instance | BindingFlags.Public).GetValue(source);

        private static string[] UpstreamDigests(UpstreamRun value) => new[]
        {
            value.WorstInput.Graph.GraphDigest,
            value.WorstInput.CompletionSearch.SuccessProofDigest,
            value.WorstInput.ClusterValidation.CombinedSuccessDigest,
            value.WorstInput.RepetitionValidation.CombinedSuccessDigest,
            value.WorstResult.CombinedSuccessDigest,
            value.WorstResult.Map19_07HandoffDigest,
        };

        private static void AssertSuccess(GeneratedDistancePacingResult result)
        {
            Assert.That(result.Success, Is.True, string.Join("\n", result.Failures));
            Assert.That(result.Surface, Is.Not.Null);
            Assert.That(result.Failures, Is.Empty);
        }

        private static void AssertAtomic(GeneratedDistancePacingResult result,
            string reason)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Surface, Is.Null);
            Assert.That(result.DistanceSuccessDigest, Is.Empty);
            Assert.That(result.RevisitSuccessDigest, Is.Empty);
            Assert.That(result.PacingSuccessDigest, Is.Empty);
            Assert.That(result.CombinedSuccessDigest, Is.Empty);
            Assert.That(result.Map19_08HandoffDigest, Is.Empty);
            Assert.That(result.Failures.Select(value => value.Reason), Does.Contain(reason));
        }

        private static void WriteEvidence(GeneratedDistancePacingResult result)
        {
            var surface = result.Surface;
            TestContext.Out.WriteLine("ROUTE_CLASSES_MEASURED=" +
                surface.RouteClassesMeasured);
            foreach (var route in surface.Routes)
                TestContext.Out.WriteLine(string.Join("|", new[]
                {
                    "ROUTE=" + route.RouteClass,
                    "DISTANCE=" + route.Distance.ToString(CultureInfo.InvariantCulture),
                    "UNIQUE_NODES=" + route.UniqueRouteNodes,
                    "TOTAL_NODE_VISITS=" + route.TotalRouteNodeVisits,
                    "REVISITED_NODES=" + route.RevisitedNodeCount,
                    "REVISIT_RATIO_BP=" + route.RevisitRatioBasisPoints,
                    "UNIQUE_EDGES=" + route.UniqueRouteEdges,
                    "TOTAL_EDGE_VISITS=" + route.TotalRouteEdgeVisits,
                    "REPEATED_CORRIDOR_EDGES=" + route.RepeatedCorridorEdgeCount,
                    "REPEATED_CORRIDOR_RATIO_BP=" +
                        route.RepeatedCorridorRatioBasisPoints,
                }));
            TestContext.Out.WriteLine("PACING_MARKERS_CHECKED=" +
                surface.PacingMarkersChecked);
            TestContext.Out.WriteLine("MANDATORY_RESOURCE_MARKERS=" +
                surface.MandatoryResourceMarkers);
            TestContext.Out.WriteLine("ACTIVITY_MARKERS=" + surface.ActivityMarkers);
            TestContext.Out.WriteLine("EVENT_MARKERS=" + surface.EventMarkers);
            TestContext.Out.WriteLine("SPECIAL_MARKERS=" + surface.SpecialMarkers);
            TestContext.Out.WriteLine("VILLAGE_FORGE_SEAL_BOSS_EXIT_MARKERS=" +
                surface.VillageForgeSealBossExitMarkers);
            TestContext.Out.WriteLine("MINIMUM_MARKER_GAP=" +
                surface.MinimumMarkerGap.ToString(CultureInfo.InvariantCulture));
            TestContext.Out.WriteLine("MAXIMUM_MARKER_GAP=" +
                surface.MaximumMarkerGap.ToString(CultureInfo.InvariantCulture));
            TestContext.Out.WriteLine("WORLD_SCALE_SOURCE_AVAILABLE=NO");
            TestContext.Out.WriteLine("FOCUSED_FIXTURE_SOURCE_KIND=" +
                surface.FocusedFixtureSourceKind);
            TestContext.Out.WriteLine("WORLD_SCALE_THRESHOLD_APPROVAL_PERFORMED=NO");
            TestContext.Out.WriteLine("INPUT_DIGEST=" + surface.InputDigest);
            TestContext.Out.WriteLine("DISTANCE_MEASUREMENT_DIGEST=" +
                surface.DistanceMeasurementDigest);
            TestContext.Out.WriteLine("REVISIT_MEASUREMENT_DIGEST=" +
                surface.RevisitMeasurementDigest);
            TestContext.Out.WriteLine("PACING_MEASUREMENT_DIGEST=" +
                surface.PacingMeasurementDigest);
            TestContext.Out.WriteLine("COMBINED_DIGEST=" + surface.CombinedDigest);
            TestContext.Out.WriteLine("MAP19_08_HANDOFF_DIGEST=" +
                surface.Map19_08HandoffDigest);
        }

        private sealed class ValidationRun
        {
            public ValidationRun(GeneratedDistancePacingInput input,
                GeneratedDistancePacingResult result)
            {
                Input = input;
                Result = result;
            }
            public GeneratedDistancePacingInput Input { get; }
            public GeneratedDistancePacingResult Result { get; }
        }

        private sealed class UpstreamRun
        {
            public UpstreamRun(GeneratedWorstCaseScenarioInput worstInput,
                GeneratedWorstCaseScenarioResult worstResult,
                GeneratedRepetitionValidationInput repetitionInput)
            {
                WorstInput = worstInput;
                WorstResult = worstResult;
                RepetitionInput = repetitionInput;
            }
            public GeneratedWorstCaseScenarioInput WorstInput { get; }
            public GeneratedWorstCaseScenarioResult WorstResult { get; }
            public GeneratedRepetitionValidationInput RepetitionInput { get; }
        }
    }
}
