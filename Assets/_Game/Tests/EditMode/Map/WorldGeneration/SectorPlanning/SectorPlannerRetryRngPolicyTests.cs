using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Activities;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.EventOverlays;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.SectorPlanning
{
    [TestFixture]
    [Category("MAP14_08")]
    public sealed class SectorPlannerRetryRngPolicyTests
    {
        private static SectorCanvasOwnershipPlan canvas;
        private static SectorCanvasOwnershipPlan reverseCanvas;

        [OneTimeSetUp]
        public void BuildReferenceCanvas()
        {
            canvas = BuildCanvas(false);
            reverseCanvas = BuildCanvas(true);
        }

        [Test]
        public void FirstPassSuccessPublishesAcceptDecisionWithZeroRetryDraws()
        {
            var result = SectorPlannerRetryExecutor.Execute(Request(canvas));
            Assert.That(result.Success, Is.True, Errors(result.Errors));
            Assert.That(result.Plan, Is.Not.Null);
            Assert.That(result.Plan.TerminalDecision, Is.EqualTo(SectorPlannerRetryDecisionKind.AcceptFirstPass));
            Assert.That(result.Plan.FirstPassAcceptCount, Is.EqualTo(1));
            Assert.That(result.Plan.RetryNodeCount, Is.Zero);
            Assert.That(result.Plan.Map14RetryRngDrawCount, Is.Zero);
            Assert.That(result.Plan.Map14_09HandoffReady, Is.True);
            AssertLowerSha(result.CanonicalDigest);
            TestContext.WriteLine("FIRST_PASS_ACCEPT=1;RETRY_NODES=0;MAP14_DRAWS=0;DIGEST=" + result.CanonicalDigest);
        }

        [Test]
        public void RetryPolicyDeclaresOrderedPatternTransformClusterFootprintStages()
        {
            var policy = SectorPlannerRetryPolicy.CreateDefault();
            Assert.That(policy.RecoveryOrder, Is.EqualTo(new[]
            {
                SectorPlannerRetryStage.PatternCandidate,
                SectorPlannerRetryStage.PatternTransform,
                SectorPlannerRetryStage.ClusterVariant,
                SectorPlannerRetryStage.ClusterFootprint,
                SectorPlannerRetryStage.SectorAttempt,
                SectorPlannerRetryStage.Abort,
            }));
            Assert.That(policy.Limits.MaxPatternCandidateAttemptsPerZone, Is.EqualTo(3));
            Assert.That(policy.Limits.MaxPatternTransformAttemptsPerPattern, Is.EqualTo(2));
            Assert.That(policy.Limits.MaxClusterVariantAttemptsPerSector, Is.EqualTo(3));
            Assert.That(policy.Limits.MaxClusterFootprintAttemptsPerSector, Is.EqualTo(3));
            Assert.That(policy.Limits.MaxRetryNodesPerSector, Is.EqualTo(12));
            Assert.That(policy.Limits.MaxTotalLocalAttemptsPerSector, Is.EqualTo(8));
            Assert.That(policy.Limits.AllPositive, Is.True);
            Assert.That(policy.HasCanonicalOrder, Is.True);
            AssertLowerSha(policy.CanonicalDigest);
            Assert.That(SectorPlannerRetryPolicy.CreateDefault().CanonicalDigest, Is.EqualTo(policy.CanonicalDigest));
        }

        [Test]
        public void RecoverablePatternFailuresRetryPatternBeforeClusterOrFootprint()
        {
            var inputs = new[]
            {
                Attempt(0, 0, SectorPlannerRetryFailureOwner.PatternSelection, "MISSING_PATTERN", 0, false, "MP_B", "MP_A"),
                Attempt(1, 1, SectorPlannerRetryFailureOwner.PatternApplication, "PROTECTED_TRANSFORM_REJECT", 0, false, "R0", "FLIP_X"),
                Attempt(2, 2, SectorPlannerRetryFailureOwner.PatternRender, "MAP10_RENDER_REJECT", 0, true, "MP_RENDER_B", "MP_RENDER_A"),
            };
            var result = SectorPlannerRetryExecutor.Execute(Request(canvas, inputs));
            Assert.That(result.Success, Is.True, Errors(result.Errors));
            Assert.That(result.Plan.NodeTraces.Select(value => value.Stage), Is.EqualTo(new[]
            {
                SectorPlannerRetryStage.PatternCandidate,
                SectorPlannerRetryStage.PatternTransform,
                SectorPlannerRetryStage.PatternCandidate,
            }));
            Assert.That(result.Plan.Count(SectorPlannerRetryStage.ClusterVariant), Is.Zero);
            Assert.That(result.Plan.Count(SectorPlannerRetryStage.ClusterFootprint), Is.Zero);
            Assert.That(result.Plan.Map14RetryRngDrawCount, Is.EqualTo(3));
        }

        [Test]
        public void RecoverableClusterAndSpineFailuresRetryClusterVariantThenFootprint()
        {
            var inputs = new[]
            {
                Attempt(0, 0, SectorPlannerRetryFailureOwner.ClusterPlacement, "CANDIDATE_RANKING", 0, false, "VARIANT_B", "VARIANT_A"),
                Attempt(1, 1, SectorPlannerRetryFailureOwner.ClusterPlacement, "FOOTPRINT_OVERLAP", 0, false, "FOOTPRINT_B", "FOOTPRINT_A"),
                Attempt(2, 2, SectorPlannerRetryFailureOwner.SpineEnvelope, "CANNOT_CONNECT", 0, false, "SPINE_VARIANT_B", "SPINE_VARIANT_A"),
                Attempt(3, 3, SectorPlannerRetryFailureOwner.SpineEnvelope, "CANNOT_CONNECT", 1, true, "SPINE_FOOTPRINT_B", "SPINE_FOOTPRINT_A"),
            };
            var result = SectorPlannerRetryExecutor.Execute(Request(canvas, inputs));
            Assert.That(result.Success, Is.True, Errors(result.Errors));
            Assert.That(result.Plan.NodeTraces.Select(value => value.Stage), Is.EqualTo(new[]
            {
                SectorPlannerRetryStage.ClusterVariant,
                SectorPlannerRetryStage.ClusterFootprint,
                SectorPlannerRetryStage.ClusterVariant,
                SectorPlannerRetryStage.ClusterFootprint,
            }));
            Assert.That(result.Plan.Map14RetryRngDrawCount, Is.EqualTo(4));
        }

        [Test]
        public void CapsAbortDeterministicallyWithoutValidationRelaxation()
        {
            var cases = new[]
            {
                CapCase(new SectorPlannerRetryLimit(1, 5, 5, 5, 10, 10),
                    Attempt(0, 0, SectorPlannerRetryFailureOwner.PatternSelection, "MISSING", 0, false, "A", "B"),
                    Attempt(1, 1, SectorPlannerRetryFailureOwner.PatternSelection, "MISSING", 0, true, "A", "B")),
                CapCase(new SectorPlannerRetryLimit(5, 1, 5, 5, 10, 10),
                    Attempt(0, 0, SectorPlannerRetryFailureOwner.PatternApplication, "TRANSFORM_REJECT", 0, false, "A", "B"),
                    Attempt(1, 1, SectorPlannerRetryFailureOwner.PatternApplication, "TRANSFORM_REJECT", 0, true, "A", "B")),
                CapCase(new SectorPlannerRetryLimit(5, 5, 1, 5, 10, 10),
                    Attempt(0, 0, SectorPlannerRetryFailureOwner.ClusterPlacement, "RANKING", 0, false, "A", "B"),
                    Attempt(1, 1, SectorPlannerRetryFailureOwner.ClusterPlacement, "RANKING", 0, true, "A", "B")),
                CapCase(new SectorPlannerRetryLimit(5, 5, 5, 1, 10, 10),
                    Attempt(0, 0, SectorPlannerRetryFailureOwner.ClusterPlacement, "FOOTPRINT_OVERLAP", 0, false, "A", "B"),
                    Attempt(1, 1, SectorPlannerRetryFailureOwner.ClusterPlacement, "FOOTPRINT_OVERLAP", 0, true, "A", "B")),
                CapCase(new SectorPlannerRetryLimit(5, 5, 5, 5, 1, 10),
                    Attempt(0, 0, SectorPlannerRetryFailureOwner.PatternSelection, "MISSING", 0, false, "A", "B"),
                    Attempt(1, 1, SectorPlannerRetryFailureOwner.ClusterPlacement, "RANKING", 0, true, "A", "B")),
                CapCase(new SectorPlannerRetryLimit(5, 5, 5, 5, 10, 1),
                    Attempt(0, 0, SectorPlannerRetryFailureOwner.PatternSelection, "MISSING", 0, false, "A", "B"),
                    Attempt(1, 1, SectorPlannerRetryFailureOwner.ClusterPlacement, "RANKING", 0, true, "A", "B")),
            };

            foreach (var item in cases)
            {
                var first = SectorPlannerRetryExecutor.Execute(Request(canvas, item.Inputs, item.Policy));
                var repeat = SectorPlannerRetryExecutor.Execute(Request(canvas, item.Inputs.Reverse(), item.Policy));
                Assert.That(first.Success, Is.False);
                Assert.That(first.Plan, Is.Null);
                Assert.That(first.CanonicalDigest, Is.Empty);
                Assert.That(first.TerminalDecision, Is.EqualTo(SectorPlannerRetryDecisionKind.AbortCapReached));
                Assert.That(first.CapAbortCount, Is.EqualTo(1));
                Assert.That(first.Errors.Select(value => value.Code),
                    Has.Some.EqualTo(SectorPlannerRetryErrorCode.RetryCapExceeded)
                        .Or.Some.EqualTo(SectorPlannerRetryErrorCode.NodeCapExceeded));
                Assert.That(Errors(repeat.Errors), Is.EqualTo(Errors(first.Errors)));
                Assert.That(first.Map14RetryRngDrawCount, Is.EqualTo(1));
            }
            TestContext.WriteLine("CAP_CASES=6;CAP_ABORTS=6;DRAWS_BEFORE_ABORT_EACH=1;VALIDATION_RELAXATION=0");
        }

        [Test]
        public void ForbiddenFallbackCarveRerollSocketAndMaskRelaxationAbort()
        {
            var cases = new[]
            {
                Forbidden("CORRIDOR", SectorPlannerRetryErrorCode.SyntheticCorridorAttempt),
                Forbidden("VALIDATION", SectorPlannerRetryErrorCode.ValidationRelaxationAttempt),
                Forbidden("SECTOR_REROLL", SectorPlannerRetryErrorCode.WholeSectorRerandomAttempt),
                Forbidden("WORLD_REROLL", SectorPlannerRetryErrorCode.WholeWorldRerandomAttempt),
                Forbidden("SOCKET", SectorPlannerRetryErrorCode.SocketMutationAttempt),
                Forbidden("BOUNDARY", SectorPlannerRetryErrorCode.BoundaryMutationAttempt),
                Forbidden("SPECIAL", SectorPlannerRetryErrorCode.SpecialReservationMutationAttempt),
                Forbidden("PROTECTED", SectorPlannerRetryErrorCode.ProtectedMaskRelaxationAttempt),
            };
            foreach (var input in cases)
            {
                var result = SectorPlannerRetryExecutor.Execute(Request(canvas, new[] { input }));
                Assert.That(result.Success, Is.False);
                Assert.That(result.Plan, Is.Null);
                Assert.That(result.TerminalDecision, Is.EqualTo(SectorPlannerRetryDecisionKind.AbortForbiddenFallback));
                Assert.That(result.ForbiddenAbortCount, Is.EqualTo(1));
                Assert.That(result.Map14RetryRngDrawCount, Is.Zero);
                Assert.That(result.Errors.Select(value => value.Code), Does.Contain(input.Failure.ForbiddenErrorCode));
            }
            TestContext.WriteLine("FORBIDDEN_REQUESTS=8;REJECTED=8;MAP14_DRAWS=0;ARBITRARY_CARVE=0;VALIDATION_RELAXATION=0");
        }

        [Test]
        public void RngTraceUsesApprovedStreamsScopesAndDrawAccounting()
        {
            var input = Attempt(2, 7, SectorPlannerRetryFailureOwner.PatternSelection,
                "MISSING_PATTERN", 0, true, "MP_C", "MP_A", "MP_B");
            var result = SectorPlannerRetryExecutor.Execute(Request(canvas, new[] { input },
                initialAttemptOrdinal: 4, seed: 0x14080001UL));
            Assert.That(result.Success, Is.True, Errors(result.Errors));
            var trace = result.Plan.RngTraces.Single();
            Assert.That(trace.StreamId, Is.EqualTo(WorldGenerationRngStreams.SectorRecipeStreamId));
            Assert.That(trace.PassScope, Is.EqualTo(SectorPlannerRngPassScope.PatternCandidate));
            Assert.That(trace.ScopeLabel, Is.EqualTo("MAP14_PATTERN_CANDIDATE"));
            Assert.That(trace.WorldSeed, Is.EqualTo(0x14080001UL));
            Assert.That(trace.SectorCoordinate, Is.EqualTo(new SectorCoord(1, 1)));
            Assert.That(trace.AttemptOrdinal, Is.EqualTo(6));
            Assert.That(trace.NodeOrdinal, Is.EqualTo(7));
            Assert.That(trace.DrawOrdinalBefore, Is.Zero);
            Assert.That(trace.DrawOrdinalAfter - trace.DrawOrdinalBefore, Is.EqualTo(trace.DrawCount));
            Assert.That(trace.DrawCount, Is.GreaterThanOrEqualTo(1UL));
            Assert.That(trace.Ticket, Is.InRange(0, 2));
            Assert.That(new[] { "MP_A", "MP_B", "MP_C" }, Does.Contain(trace.ChosenCandidateId));
            AssertLowerSha(trace.InitialStateDigest);
            AssertLowerSha(trace.FinalStateDigest);
            AssertLowerSha(trace.CanonicalDigest);
            TestContext.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "RNG={0};SCOPE={1};SEED={2};SECTOR=1,1;ATTEMPT={3};NODE={4};BEFORE={5};AFTER={6};DRAWS={7};TICKET={8};CANDIDATES={9};CHOSEN={10};INITIAL={11};FINAL={12}",
                trace.StreamId, trace.ScopeLabel, trace.WorldSeed, trace.AttemptOrdinal, trace.NodeOrdinal,
                trace.DrawOrdinalBefore, trace.DrawOrdinalAfter, trace.DrawCount, trace.Ticket,
                trace.CandidateCount, trace.ChosenCandidateId, trace.InitialStateDigest, trace.FinalStateDigest));
        }

        [Test]
        public void RetryPlanIsDeterministicAcrossRepeatReverseAndTurkishCulture()
        {
            var inputs = MetricsInputs();
            var baseline = SectorPlannerRetryExecutor.Execute(Request(canvas, inputs));
            var repeat = SectorPlannerRetryExecutor.Execute(Request(canvas, inputs));
            var reversed = SectorPlannerRetryExecutor.Execute(Request(reverseCanvas, inputs.Reverse()));
            Assert.That(baseline.Success && repeat.Success && reversed.Success, Is.True);
            Assert.That(repeat.CanonicalDigest, Is.EqualTo(baseline.CanonicalDigest));
            Assert.That(reversed.CanonicalDigest, Is.EqualTo(baseline.CanonicalDigest));
            Assert.That(repeat.Plan.RngTraces.Select(value => value.CanonicalDigest),
                Is.EqualTo(baseline.Plan.RngTraces.Select(value => value.CanonicalDigest)));
            Assert.That(reversed.Plan.RngTraces.Select(value => value.CanonicalDigest),
                Is.EqualTo(baseline.Plan.RngTraces.Select(value => value.CanonicalDigest)));

            var originalCulture = CultureInfo.CurrentCulture;
            var originalUi = CultureInfo.CurrentUICulture;
            try
            {
                var turkish = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentCulture = turkish;
                CultureInfo.CurrentUICulture = turkish;
                var culture = SectorPlannerRetryExecutor.Execute(Request(canvas, inputs.Reverse()));
                Assert.That(culture.Success, Is.True, Errors(culture.Errors));
                Assert.That(culture.CanonicalDigest, Is.EqualTo(baseline.CanonicalDigest));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUi;
            }
        }

        [Test]
        public void SeedAndAttemptMutationChangeDrawnRetryDigestAndKeepUnrelatedStreamsIsolated()
        {
            var inputs = new[] { Attempt(0, 0, SectorPlannerRetryFailureOwner.ClusterPlacement,
                "RANKING", 0, true, "VARIANT_A", "VARIANT_B", "VARIANT_C") };
            var authority = RetryRngFactory();
            var populationBefore = authority.Create(WorldGenerationRngStreams.PopulationStreamId,
                0x14080002UL, RngStreamScope.Spawn("MAP14_UNRELATED", 0)).NextUInt64();
            var baseline = SectorPlannerRetryExecutor.Execute(Request(canvas, inputs,
                seed: 0x14080002UL, rng: authority));
            var seedChanged = SectorPlannerRetryExecutor.Execute(Request(canvas, inputs,
                seed: 0x14080003UL, rng: authority));
            var attemptChanged = SectorPlannerRetryExecutor.Execute(Request(canvas, inputs,
                initialAttemptOrdinal: 1, seed: 0x14080002UL, rng: authority));
            var populationAfter = authority.Create(WorldGenerationRngStreams.PopulationStreamId,
                0x14080002UL, RngStreamScope.Spawn("MAP14_UNRELATED", 0)).NextUInt64();

            Assert.That(baseline.Success && seedChanged.Success && attemptChanged.Success, Is.True);
            Assert.That(seedChanged.CanonicalDigest, Is.Not.EqualTo(baseline.CanonicalDigest));
            Assert.That(attemptChanged.CanonicalDigest, Is.Not.EqualTo(baseline.CanonicalDigest));
            Assert.That(populationAfter, Is.EqualTo(populationBefore));
        }

        [Test]
        public void InvalidInputDuplicateTraceNegativeAttemptAndMutationClaimsFailAtomically()
        {
            var failure = new SectorPlannerRetryFailure(
                SectorPlannerRetryFailureOwner.PatternSelection, "MISSING", "ZONE", "missing");
            var published = SectorPlannerAttemptTraceBuilder.Build(failure, 0, 0);
            var badRng = new SectorPlannerRngTrace(
                "RNG_WRONG", SectorPlannerRngPassScope.PatternCandidate, "MAP14_WRONG",
                1UL, new SectorCoord(1, 1), 0, 0, 0, 2, 1, 0, 1, "A",
                Hash("INITIAL"), Hash("FINAL"));
            var duplicateInput = Attempt(0, 0, SectorPlannerRetryFailureOwner.PatternSelection,
                "MISSING", 0, true, "A", "B");
            var invalid = new[]
            {
                SectorPlannerRetryExecutor.Execute(null),
                SectorPlannerRetryExecutor.Execute(Request(null)),
                SectorPlannerRetryExecutor.Execute(new SectorPlannerRetryBuildRequest(
                    canvas, null, RetryRngFactory(), 0x14080001UL, new SectorCoord(1, 1),
                    publicationLabel: SectorPlannerRetryExecutor.ReferencePublicationLabel)),
                SectorPlannerRetryExecutor.Execute(new SectorPlannerRetryBuildRequest(
                    canvas, SectorPlannerRetryPolicy.CreateDefault(), null, 0x14080001UL,
                    new SectorCoord(1, 1),
                    publicationLabel: SectorPlannerRetryExecutor.ReferencePublicationLabel)),
                SectorPlannerRetryExecutor.Execute(Request(canvas, initialAttemptOrdinal: -1)),
                SectorPlannerRetryExecutor.Execute(Request(canvas,
                    publishedAttempts: new[] { published, published })),
                SectorPlannerRetryExecutor.Execute(Request(canvas,
                    inputs: new[] { duplicateInput, duplicateInput })),
                SectorPlannerRetryExecutor.Execute(Request(canvas,
                    policy: Policy(new SectorPlannerRetryLimit(0, 1, 1, 1, 1, 1)))),
                SectorPlannerRetryExecutor.Execute(Request(canvas,
                    publishedRng: new[] { badRng })),
                SectorPlannerRetryExecutor.Execute(Request(canvas,
                    upstreamMutationClaim: true)),
            };
            foreach (var result in invalid)
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.Plan, Is.Null);
                Assert.That(result.CanonicalDigest, Is.Empty);
                Assert.That(result.Errors, Is.Not.Empty);
                Assert.That(result.Errors, Is.Ordered);
                Assert.That(result.Map14RetryRngDrawCount, Is.Zero);
            }
        }

        [Test]
        public void NoTilePhysicsSceneDebugExportOrGameplayMutation()
        {
            var result = SectorPlannerRetryExecutor.Execute(Request(canvas, MetricsInputs()));
            Assert.That(result.Success, Is.True, Errors(result.Errors));
            var plan = result.Plan;
            Assert.That(plan.SyntheticRetryCaseCount, Is.EqualTo(6));
            Assert.That(plan.RetryNodeCount, Is.EqualTo(6));
            Assert.That(plan.TerminalDecisionCount, Is.EqualTo(1));
            Assert.That(plan.TerminalDecision, Is.EqualTo(SectorPlannerRetryDecisionKind.AcceptRecovered));
            Assert.That(plan.Count(SectorPlannerRetryStage.PatternCandidate), Is.EqualTo(2));
            Assert.That(plan.Count(SectorPlannerRetryStage.PatternTransform), Is.EqualTo(1));
            Assert.That(plan.Count(SectorPlannerRetryStage.ClusterVariant), Is.EqualTo(2));
            Assert.That(plan.Count(SectorPlannerRetryStage.ClusterFootprint), Is.EqualTo(1));
            Assert.That(plan.Map14RetryRngDrawCount, Is.EqualTo(6));
            Assert.That(plan.Map12ActivityRngDrawCount, Is.EqualTo(10));
            Assert.That(plan.Map12EventRngDrawCount, Is.EqualTo(10));
            Assert.That(plan.AllUpstreamIdentitiesPreserved, Is.True);
            Assert.That(plan.FallbackCorridorCarveCount, Is.Zero);
            Assert.That(plan.ValidationRelaxationCount, Is.Zero);
            Assert.That(plan.WholeSectorRerandomCount, Is.Zero);
            Assert.That(plan.WholeWorldRerandomCount, Is.Zero);
            Assert.That(plan.FixedAnchorMutationCount, Is.Zero);
            Assert.That(plan.BoundarySocketMutationCount, Is.Zero);
            Assert.That(plan.SpecialReservationMutationCount, Is.Zero);
            Assert.That(plan.ProtectedMaskRelaxationCount, Is.Zero);
            Assert.That(plan.TilemapWriteCount, Is.Zero);
            Assert.That(plan.SceneMutationCount, Is.Zero);
            Assert.That(plan.PrefabMutationCount, Is.Zero);
            Assert.That(plan.GameObjectMutationCount, Is.Zero);
            Assert.That(plan.ActivityRuntimeSpawnCount, Is.Zero);
            Assert.That(plan.EventRuntimeSpawnCount, Is.Zero);
            Assert.That(plan.GameplayExecutionCount, Is.Zero);
            Assert.That(plan.DebugExportWriteCount, Is.Zero);
            TestContext.WriteLine(
                "SYNTHETIC_CASES=6;NODES=6;TERMINALS=1;PATTERN_CANDIDATE=2;PATTERN_TRANSFORM=1;CLUSTER_VARIANT=2;CLUSTER_FOOTPRINT=1;MAP14_DRAWS=6;MAP12_ACTIVITY_DRAWS=10;MAP12_EVENT_DRAWS=10;MUTATIONS=0");
        }

        private static SectorPlannerAttemptTraceInput[] MetricsInputs()
        {
            return new[]
            {
                Attempt(0, 0, SectorPlannerRetryFailureOwner.PatternSelection, "MISSING_PATTERN", 0, false, "MP_A", "MP_B"),
                Attempt(1, 1, SectorPlannerRetryFailureOwner.PatternApplication, "PROTECTED_TRANSFORM_REJECT", 0, false, "R0", "FLIP_X"),
                Attempt(2, 2, SectorPlannerRetryFailureOwner.ClusterPlacement, "CANDIDATE_RANKING", 0, false, "VARIANT_A", "VARIANT_B"),
                Attempt(3, 3, SectorPlannerRetryFailureOwner.ClusterPlacement, "FOOTPRINT_OVERLAP", 0, false, "FOOTPRINT_A", "FOOTPRINT_B"),
                Attempt(4, 4, SectorPlannerRetryFailureOwner.CanvasOwnership, "PATTERN_QUIET_OVERLAP", 0, false, "MP_OWN_A", "MP_OWN_B"),
                Attempt(5, 5, SectorPlannerRetryFailureOwner.CanvasOwnership, "PATTERN_QUIET_OVERLAP", 1, true, "CLUSTER_OWN_A", "CLUSTER_OWN_B"),
            };
        }

        private static SectorPlannerAttemptTraceInput Attempt(
            int attempt, int node, SectorPlannerRetryFailureOwner owner, string code,
            int sequence, bool recovered, params string[] candidates)
        {
            return new SectorPlannerAttemptTraceInput(
                attempt, node,
                new SectorPlannerRetryFailure(owner, code, code + "_SUBJECT", code + "_DETAIL", sequence),
                candidates, recovered);
        }

        private static SectorPlannerAttemptTraceInput Forbidden(
            string code, SectorPlannerRetryErrorCode errorCode)
        {
            return new SectorPlannerAttemptTraceInput(
                0, 0,
                new SectorPlannerRetryFailure(SectorPlannerRetryFailureOwner.ForbiddenFallback,
                    code, code + "_SUBJECT", code + "_DETAIL", 0, errorCode),
                Array.Empty<string>(), false);
        }

        private static CapFixture CapCase(
            SectorPlannerRetryLimit limits,
            params SectorPlannerAttemptTraceInput[] inputs)
        {
            return new CapFixture(Policy(limits), inputs);
        }

        private static SectorPlannerRetryPolicy Policy(SectorPlannerRetryLimit limits)
        {
            return new SectorPlannerRetryPolicy(limits, new[]
            {
                SectorPlannerRetryStage.PatternCandidate,
                SectorPlannerRetryStage.PatternTransform,
                SectorPlannerRetryStage.ClusterVariant,
                SectorPlannerRetryStage.ClusterFootprint,
                SectorPlannerRetryStage.SectorAttempt,
                SectorPlannerRetryStage.Abort,
            });
        }

        private static SectorPlannerRetryBuildRequest Request(
            SectorCanvasOwnershipPlan plan,
            IEnumerable<SectorPlannerAttemptTraceInput> inputs = null,
            SectorPlannerRetryPolicy policy = null,
            int initialAttemptOrdinal = 0,
            ulong seed = 0x14080001UL,
            DeterministicRngStreamFactory rng = null,
            IEnumerable<SectorPlannerAttemptTrace> publishedAttempts = null,
            IEnumerable<SectorPlannerRngTrace> publishedRng = null,
            bool upstreamMutationClaim = false)
        {
            return new SectorPlannerRetryBuildRequest(
                plan,
                policy ?? SectorPlannerRetryPolicy.CreateDefault(),
                rng ?? RetryRngFactory(),
                seed,
                new SectorCoord(1, 1),
                initialAttemptOrdinal: initialAttemptOrdinal,
                sourceAttemptInputs: inputs,
                sourcePublishedAttemptTraces: publishedAttempts,
                sourcePublishedRngTraces: publishedRng,
                publicationLabel: SectorPlannerRetryExecutor.ReferencePublicationLabel,
                upstreamMutationClaim: upstreamMutationClaim);
        }

        private static SectorCanvasOwnershipPlan BuildCanvas(bool reverse)
        {
            var fixture = Fixture.Create(reverse);
            var fill = fixture.Fill();
            Require(fill.Success, fill.Errors);
            var upstream = fixture.Place(fill.Plan, fixture.CreateAuthorities(fill.Plan, reverse));
            Require(upstream.Success, upstream.Errors);
            var request = new SectorCanvasOwnershipBuildRequest(
                fixture.Input, fixture.Assignments, fixture.AnchorPlan, fixture.PlacementPlan,
                fixture.SpineEnvelopePlan, fixture.RolePlan, fixture.RenderPlan, upstream.Plan,
                SectorCanvasOwnershipClaimBuilder.ReferencePublicationLabel);
            var claims = SectorCanvasOwnershipClaimBuilder.BuildClaims(request);
            Require(claims.Success, claims.Errors);
            var resolved = SectorCanvasOwnershipResolver.Resolve(claims);
            Require(resolved.Success, resolved.Errors);
            return resolved.Plan;
        }

        private static DeterministicRngStreamFactory RetryRngFactory()
        {
            var definitions = new SortedDictionary<string, RngStreamDefinition>(StringComparer.Ordinal)
            {
                { WorldGenerationRngStreams.SectorRecipeStreamId,
                    Definition(WorldGenerationRngStreams.SectorRecipeStreamId, "E9931A70C2D520F4", "SECTOR") },
                { WorldGenerationRngStreams.PopulationStreamId,
                    Definition(WorldGenerationRngStreams.PopulationStreamId, "A63D4078F9E21C55", "SPAWN") },
            };
            var set = (WorldRouteDefinitionSet)FormatterServices.GetUninitializedObject(typeof(WorldRouteDefinitionSet));
            SetAutoProperty(set, "RngStreams", new ReadOnlyDictionary<string, RngStreamDefinition>(definitions));
            return new DeterministicRngStreamFactory(set);
        }

        private static RngStreamDefinition Definition(string id, string salt, string scope)
        {
            var definition = (RngStreamDefinition)FormatterServices.GetUninitializedObject(typeof(RngStreamDefinition));
            SetAutoProperty(definition, "RngStreamId", id);
            SetAutoProperty(definition, "SaltHex", Hex(salt));
            SetAutoProperty(definition, "ResetScope", scope);
            SetAutoProperty(definition, "DescriptionKo", "MAP14_08 focused fixture");
            SetAutoProperty(definition, "Active", true);
            return definition;
        }

        private static CsvHexValue Hex(string value)
        {
            var bytes = Enumerable.Range(0, value.Length / 2)
                .Select(index => byte.Parse(value.Substring(index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray();
            var constructor = typeof(CsvHexValue).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(string), typeof(IEnumerable<byte>) }, null);
            Assert.That(constructor, Is.Not.Null);
            return (CsvHexValue)constructor.Invoke(new object[] { value, bytes });
        }

        private static void SetAutoProperty(object target, string property, object value)
        {
            var field = target.GetType().GetField("<" + property + ">k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, property);
            field.SetValue(target, value);
        }

        private static void Require<T>(bool success, IEnumerable<T> errors)
        {
            if (!success) throw new InvalidOperationException(string.Join(";", errors));
        }

        private static string Errors(IEnumerable<SectorPlannerRetryError> errors)
        {
            return string.Join(";", (errors ?? Array.Empty<SectorPlannerRetryError>())
                .Select(value => value.ToString()));
        }

        private static void AssertLowerSha(string value)
        {
            Assert.That(value, Does.Match("^[0-9a-f]{64}$"));
        }

        private static string Hash(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                var result = new StringBuilder(bytes.Length * 2);
                foreach (var item in bytes) result.Append(item.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }

        private sealed class CapFixture
        {
            internal CapFixture(SectorPlannerRetryPolicy policy, SectorPlannerAttemptTraceInput[] inputs)
            {
                Policy = policy;
                Inputs = inputs;
            }

            internal SectorPlannerRetryPolicy Policy { get; }
            internal SectorPlannerAttemptTraceInput[] Inputs { get; }
        }

        private sealed class Fixture
        {
            private static readonly SectorCoord Plain = new SectorCoord(1, 1);
            private static readonly SectorCoord Quiet = new SectorCoord(2, 1);
            private static readonly SectorCoord Village = new SectorCoord(3, 1);
            private static readonly SectorCoord Core = new SectorCoord(4, 1);
            private static readonly SectorCoord Forge = new SectorCoord(5, 1);
            private static readonly SectorCoord Boss = new SectorCoord(6, 1);
            private static readonly SectorCoord Activity = new SectorCoord(7, 1);
            private static readonly SectorCoord Deferred = new SectorCoord(8, 1);
            private static readonly SectorCoord Neighbor = new SectorCoord(9, 1);
            private static readonly string CatalogDigest = Hash("MAP11_CATALOG");
            private static readonly string SignatureDigest = Hash("MAP11_SIGNATURES");
            private static readonly string ManifestDigest = Hash("MAP12_MANIFEST");

            private Fixture(
                SectorPlannerInput input,
                IReadOnlyList<SectorPacingAssignment> assignments,
                SectorFixedAnchorPlan anchorPlan,
                SectorClusterPlacementPlan placementPlan,
                SectorSpineEnvelopePlan spineEnvelopePlan,
                SectorClusterRolePatternPlan rolePlan,
                SectorPatternRenderPlan renderPlan)
            {
                Input = input;
                Assignments = assignments;
                AnchorPlan = anchorPlan;
                PlacementPlan = placementPlan;
                SpineEnvelopePlan = spineEnvelopePlan;
                RolePlan = rolePlan;
                RenderPlan = renderPlan;
            }

            internal SectorPlannerInput Input { get; }
            internal IReadOnlyList<SectorPacingAssignment> Assignments { get; }
            internal SectorFixedAnchorPlan AnchorPlan { get; }
            internal SectorClusterPlacementPlan PlacementPlan { get; }
            internal SectorSpineEnvelopePlan SpineEnvelopePlan { get; }
            internal SectorClusterRolePatternPlan RolePlan { get; }
            internal SectorPatternRenderPlan RenderPlan { get; }

            internal static Fixture Create(bool reverse = false)
            {
                var sectors = CreateSectors();
                if (reverse) sectors.Reverse();
                var authority = SectorPlannerAuthorityDigestSnapshot.CaptureCurrentPublicAuthorities(
                    Hash("FOUNDATION"), Hash("LAYER"), 24, Hash("PATTERN"), 16,
                    Hash("CLUSTER"), 7, Hash("ACTIVITY"), 5, Hash("EVENT"));
                var input = SectorPlannerInputBuilder.Build(new SectorPlannerInputRequest(
                    sectors, authority, SectorPlannerInputBuilder.ReferencePublicationLabel));
                Require(input.Success, input.Errors);
                var assignments = SectorPacingRolePlanner.Assign(input.Input).ToList();
                var anchors = CreateAnchors();
                if (reverse) { assignments.Reverse(); anchors.Reverse(); }
                var anchor = SectorFixedAnchorPlanner.Build(new SectorFixedAnchorBuildRequest(
                    input.Input, assignments, anchors, SectorFixedAnchorPlanner.ReferencePublicationLabel));
                Require(anchor.Success, anchor.Errors);
                var catalog = CreateClusterCatalog();
                if (reverse) catalog.Reverse();
                var candidates = SectorClusterCandidateBuilder.Build(new SectorClusterCandidateBuildRequest(
                    input.Input, assignments, anchor.Plan, catalog,
                    SectorClusterCandidateBuilder.ReferenceCandidatePublicationLabel));
                Require(candidates.Success, candidates.Errors);
                var placement = SectorClusterPlacementPlanner.Place(new SectorClusterPlacementRequest(
                    candidates.CandidateSet, anchor.Plan,
                    SectorClusterPlacementPlanner.ReferencePlacementPublicationLabel));
                Require(placement.Success, placement.Errors);
                var spineRequest = new SectorSpineEnvelopeBuildRequest(
                    input.Input, assignments, anchor.Plan, placement.Plan,
                    SectorSpineGraphBuilder.ReferenceGraphPublicationLabel,
                    SectorTraversalEnvelopeBuilder.ReferenceEnvelopePublicationLabel);
                var graph = SectorSpineGraphBuilder.Build(spineRequest);
                Require(graph.Success, graph.Errors);
                var spine = SectorTraversalEnvelopeBuilder.Build(spineRequest, graph.Graph);
                Require(spine.Success, spine.Errors);
                var roles = SectorClusterRoleZoneBuilder.Build(new SectorClusterRoleZoneBuildRequest(
                    input.Input, assignments, anchor.Plan, placement.Plan, spine.Plan,
                    SectorClusterRoleZoneBuilder.ReferencePublicationLabel));
                Require(roles.Success, roles.Errors);
                var patterns = CreatePatternCatalog();
                if (reverse) patterns.Reverse();
                var render = SectorPatternRenderPlanner.Render(new SectorPatternRenderRequest(
                    roles.Plan, patterns, SectorPatternRenderPlanner.ReferencePublicationLabel));
                Require(render.Success, render.Errors);
                return new Fixture(input.Input, assignments, anchor.Plan, placement.Plan,
                    spine.Plan, roles.Plan, render.Plan);
            }

            internal SectorQuietFillBuildResult Fill() => SectorQuietFillPlanner.Fill(
                new SectorQuietActivityEventBuildRequest(
                    Input, Assignments, AnchorPlan, PlacementPlan, SpineEnvelopePlan,
                    RolePlan, RenderPlan, SectorQuietFillPlanner.ReferencePublicationLabel));

            internal SectorQuietActivityEventBuildResult Place(SectorQuietFillPlan fill) =>
                Place(fill, CreateAuthorities(fill));

            internal SectorQuietActivityEventBuildResult Place(
                SectorQuietFillPlan fill,
                AuthorityPackage package) =>
                SectorActivityEventPlacementPlanner.Place(package.Request(fill));

            internal AuthorityPackage CreateAuthorities(SectorQuietFillPlan fill, bool reverse = false)
            {
                var ownership = Ownership();
                var profiles = new List<ActivityPlacementProfile>();
                var projections = new List<SectorActivityOpportunityProjection>();
                foreach (var placement in PlacementPlan.Placements.OrderBy(value => value.SectorIndex))
                {
                    var sector = Input.Sectors.Single(value => value.SectorIndex == placement.SectorIndex);
                    var assignment = Assignments.Single(value => value.Coordinate == sector.Coordinate);
                    ownership.TryGetSector(sector.Coordinate, out var owned);
                    var rectangle = FindRectangle(fill, sector.Coordinate);
                    var activityId = new ActivityStructureId("ACTIVITY_MAP14_06_" + placement.SectorIndex.ToString("D3", CultureInfo.InvariantCulture));
                    var shell = Hash("SHELL|" + placement.ClusterId.Value);
                    var safety = Hash("SAFETY|" + placement.ClusterId.Value);
                    profiles.Add(new ActivityPlacementProfile(
                        activityId, placement.ClusterId, placement.VariantId,
                        Hash("ACTIVITY|" + activityId.Value), shell, safety,
                        new[] { Biome(sector) }, new[] { assignment.PrimaryRole },
                        new[] { sector.Route.AccessClass }, placement.Cells.Count, placement.Cells.Count,
                        2, 2, 100, ActivityStrengthClass.Strong));
                    var opportunity = new ActivityPlacementOpportunity(
                        "ACTIVITY_OPPORTUNITY_" + placement.SectorIndex.ToString("D3", CultureInfo.InvariantCulture),
                        sector.Coordinate, owned.PatchId.Value, Biome(sector),
                        placement.ClusterId, placement.VariantId, assignment.PrimaryRole,
                        sector.Route.AccessClass, placement.Cells.Count,
                        new ActivityPlacementClearanceEvidence(rectangle[0], 2, 2,
                            rectangle, rectangle, Array.Empty<LocalTileCoord>(), Array.Empty<LocalTileCoord>()),
                        CatalogDigest, SignatureDigest, ManifestDigest, shell, safety);
                    projections.Add(new SectorActivityOpportunityProjection(
                        opportunity, rectangle[0], MarkerForActivity(assignment.PrimaryRole), safety));
                }
                if (reverse) { profiles.Reverse(); projections.Reverse(); }
                var activityIndex = ActivityCandidateIndexCompiler.Compile(new ActivityCandidateIndexCompileRequest(
                    profiles, projections.Select(value => value.Authority), ownership,
                    CatalogDigest, SignatureDigest, ManifestDigest));
                Require(activityIndex.Success, activityIndex.Errors);
                var activityPlan = ActivityFrequencyPlanner.Plan(new ActivityFrequencyPlanRequest(
                    activityIndex.Index, new ActivityFrequencyPolicy(120, 1, 1, 1),
                    0x14060001UL, 0, RngFactory()));
                Require(activityPlan.Success, activityPlan.Errors);

                var eventProfiles = EventProfiles();
                var eventProjections = new List<SectorEventMarkerOpportunityProjection>();
                foreach (var activityProjection in projections.OrderBy(value => value.OpportunityId, StringComparer.Ordinal))
                {
                    var sector = Input.Sectors.Single(value => value.Coordinate == activityProjection.SectorCoordinate);
                    ownership.TryGetSector(sector.Coordinate, out var owned);
                    var markerKind = MarkerForEvent(sector);
                    var owner = sector.SpecialRegion.Kind == SectorPlannerSpecialRegionKind.None
                        ? PlacementPlan.Placements.Single(value => value.SectorCoordinate == sector.Coordinate).ClusterId.Value
                        : sector.SpecialRegion.RegionId;
                    var sourceKind = markerKind == SectorActivityEventMarkerKind.EventSpecial
                        ? EventMarkerTargetSourceKind.SpecialRegion
                        : markerKind == SectorActivityEventMarkerKind.EventActivity
                            ? EventMarkerTargetSourceKind.Activity
                            : EventMarkerTargetSourceKind.TerrainCluster;
                    var marker = new EventMarkerTargetEvidence(
                        new EventMarkerId("MARKER_MAP14_06"), sourceKind, owner,
                        activityProjection.MarkerCoordinate, activityProjection.MarkerCoordinate,
                        markerKind.ToString(), "QUIET", "QUIET", Hash("STATIC"), Hash("STATIC"),
                        Hash("PROTECTION"), Hash("PROTECTION"), default(SpecialPersistenceKey),
                        string.Empty, string.Empty);
                    var opportunity = new EventOverlayOpportunity(
                        "EVENT_OPP_" + sector.SectorIndex.ToString("D3", CultureInfo.InvariantCulture),
                        sector.Coordinate, owned.PatchId.Value, sector.SectorIndex,
                        Biome(sector), Assignments.Single(value => value.Coordinate == sector.Coordinate).PrimaryRole,
                        sector.Route.AccessClass, new TerrainClusterId("TC_MAP14_06_EVENT"), null,
                        activityPlan.Plan.CanonicalDigest, new[] { marker });
                    eventProjections.Add(new SectorEventMarkerOpportunityProjection(
                        opportunity, activityProjection.MarkerCoordinate, markerKind, owner));
                }
                if (reverse) { eventProfiles.Reverse(); eventProjections.Reverse(); }
                var eventIndex = EventOverlayCandidateIndexCompiler.Compile(new EventOverlayCandidateIndexRequest(
                    eventProfiles, eventProjections.Select(value => value.Authority), activityPlan.Plan.CanonicalDigest));
                Require(eventIndex.Success, eventIndex.Errors);
                var eventPlan = EventOverlayAssignmentPlanner.Plan(new EventOverlayAssignmentPlanRequest(
                    eventIndex.Index, new EventOverlayAssignmentPolicy(80),
                    0x14060002UL, 0, RngFactory()));
                Require(eventPlan.Success, eventPlan.Errors);
                return new AuthorityPackage(projections, activityIndex.Index, activityPlan.Plan,
                    eventProjections, eventIndex.Index, eventPlan.Plan);
            }

            private static List<SectorPlannerSectorSnapshot> CreateSectors() =>
                new List<SectorPlannerSectorSnapshot>
                {
                    Sector(Plain, MoonpalaceBiomeId.MoonCrater,
                        new[] { PacingRole.Traversal, PacingRole.Recovery },
                        route: new SectorPlannerRouteSnapshot(1, AccessClass.MandatoryNoTool,
                            new[] { "SOCKET_L", "SOCKET_R", "SOCKET_U", "SOCKET_D" }, true, true),
                        boundaries: new[] { new SectorPlannerBoundarySnapshot(
                            SectorPlannerSide.Right, "PAIR_CRATER_ROOT", "BOUNDARY_CRATER_ROOT", 1) }),
                    Sector(Quiet, MoonpalaceBiomeId.MoonCrater, new[] { PacingRole.Quiet }, quiet: true),
                    Sector(Village, MoonpalaceBiomeId.CassiaRoot, new[] { PacingRole.Safe, PacingRole.Landmark },
                        special: new SectorPlannerSpecialRegionSnapshot("REGION_VILLAGE",
                            SectorPlannerSpecialRegionKind.Village, SectorPlannerSpecialRegionBinding.ReferenceOnly,
                            "FP_VILLAGE_REFERENCE", false, false, false), ordinal: 2, optionalDistance: 0),
                    Sector(Core, MoonpalaceBiomeId.CassiaRoot, new[] { PacingRole.Resource },
                        sites: new[] { new SectorPlannerSiteSnapshot("SITE_CORE", "CORE_RESOURCE", "RES_CORE", true) },
                        special: Mandatory("REGION_CORE", SectorPlannerSpecialRegionKind.CoreResource, "FP_CORE"), mandatoryDistance: 0),
                    Sector(Forge, MoonpalaceBiomeId.AbandonedMill, new[] { PacingRole.Landmark, PacingRole.Machinery },
                        sites: new[] { new SectorPlannerSiteSnapshot("SITE_FORGE", "FORGE", "RES_FORGE", true) },
                        special: Mandatory("REGION_FORGE", SectorPlannerSpecialRegionKind.Forge, "FP_FORGE"), mandatoryDistance: 0),
                    Sector(Boss, MoonpalaceBiomeId.MoonDough, new[] { PacingRole.Boss },
                        sites: new[] { new SectorPlannerSiteSnapshot("SITE_BOSS", "BOSS_GATE", "RES_BOSS", true) },
                        special: Mandatory("REGION_BOSS", SectorPlannerSpecialRegionKind.Boss, "FP_BOSS"), mandatoryDistance: 0),
                    Sector(Activity, MoonpalaceBiomeId.MoonCrater, new[] { PacingRole.Activity },
                        activity: true, eventAvailable: true, ordinal: 5),
                    Sector(Deferred, MoonpalaceBiomeId.CassiaRoot, new[] { PacingRole.Discovery },
                        special: new SectorPlannerSpecialRegionSnapshot("REGION_MERCHANT",
                            SectorPlannerSpecialRegionKind.Merchant, SectorPlannerSpecialRegionBinding.DeferredOptionalLocal,
                            string.Empty, false, false, false),
                        optional: new[] { new SectorPlannerOptionalRegionSnapshot("REGION_MERCHANT",
                            SectorPlannerSpecialRegionKind.Merchant, true, true, false) }, optionalDistance: 1),
                    Sector(Neighbor, MoonpalaceBiomeId.AbandonedMill, new[] { PacingRole.Traversal },
                        neighbors: new[]
                        {
                            new SectorPlannerNeighborSnapshot(SectorPlannerSide.Left, Deferred, 1,
                                AccessClass.MandatoryNoTool, Array.Empty<string>(), PacingRole.Traversal),
                            new SectorPlannerNeighborSnapshot(SectorPlannerSide.Right, new SectorCoord(10, 1), 1,
                                AccessClass.MandatoryNoTool, Array.Empty<string>(), PacingRole.Traversal),
                        }),
                };

            private static SectorPlannerSectorSnapshot Sector(
                SectorCoord coordinate,
                MoonpalaceBiomeId biome,
                IEnumerable<PacingRole> roles,
                SectorPlannerRouteSnapshot route = null,
                IEnumerable<SectorPlannerBoundarySnapshot> boundaries = null,
                IEnumerable<SectorPlannerSiteSnapshot> sites = null,
                SectorPlannerSpecialRegionSnapshot special = null,
                IEnumerable<SectorPlannerOptionalRegionSnapshot> optional = null,
                IEnumerable<SectorPlannerNeighborSnapshot> neighbors = null,
                bool quiet = false,
                bool activity = false,
                bool eventAvailable = false,
                int ordinal = 4,
                int mandatoryDistance = 2,
                int optionalDistance = 3) =>
                new SectorPlannerSectorSnapshot(
                    coordinate, (coordinate.Y * 13) + coordinate.X, 48, 32,
                    new SectorPlannerBiomeSnapshot("PATCH_" + coordinate.X, biome.ToString()),
                    route ?? new SectorPlannerRouteSnapshot(1, AccessClass.MandatoryNoTool,
                        Array.Empty<string>(), false, false), boundaries, sites,
                    special ?? SectorPlannerSpecialRegionSnapshot.None, optional, neighbors,
                    new SectorPlannerWorldProgressSnapshot(ordinal, "CHAPTER_REFERENCE", "BRANCH_REFERENCE",
                        mandatoryDistance, optionalDistance), roles, quiet, activity, eventAvailable);

            private static SectorPlannerSpecialRegionSnapshot Mandatory(
                string id, SectorPlannerSpecialRegionKind kind, string footprint) =>
                new SectorPlannerSpecialRegionSnapshot(id, kind,
                    SectorPlannerSpecialRegionBinding.ReservedMandatory, footprint, true, true, true);

            private static List<SectorFixedAnchorProjection> CreateAnchors()
            {
                var result = new List<SectorFixedAnchorProjection>
                {
                    RouteAnchor("ANCHOR_SOCKET_L", "SOCKET_L", SectorPlannerSide.Left, new SectorFixedAnchorRect(0, 14, 1, 4)),
                    RouteAnchor("ANCHOR_SOCKET_R", "SOCKET_R", SectorPlannerSide.Right, new SectorFixedAnchorRect(47, 14, 1, 4)),
                    RouteAnchor("ANCHOR_SOCKET_U", "SOCKET_U", SectorPlannerSide.Up, new SectorFixedAnchorRect(22, 0, 4, 1)),
                    RouteAnchor("ANCHOR_SOCKET_D", "SOCKET_D", SectorPlannerSide.Down, new SectorFixedAnchorRect(22, 31, 4, 1)),
                    new SectorFixedAnchorProjection("ANCHOR_BOUNDARY_FIXED", Plain,
                        SectorFixedAnchorKind.BoundaryFixedSlice, SectorFixedAnchorSource.BoundarySnapshot,
                        SectorFixedAnchorPriority.BoundaryFixedSlice, new SectorFixedAnchorRect(47, 4, 1, 4),
                        "BOUNDARY_CRATER_ROOT", SectorPlannerSide.Right, true, "BOUNDARY_CRATER_ROOT"),
                    new SectorFixedAnchorProjection("ANCHOR_BOUNDARY_WARNING", Plain,
                        SectorFixedAnchorKind.BoundaryWarning, SectorFixedAnchorSource.BoundarySnapshot,
                        SectorFixedAnchorPriority.BoundaryWarning, new SectorFixedAnchorRect(47, 4, 1, 4),
                        "BOUNDARY_CRATER_ROOT", SectorPlannerSide.Right, true, "BOUNDARY_CRATER_ROOT"),
                    new SectorFixedAnchorProjection("ANCHOR_VILLAGE_REFERENCE", Village,
                        SectorFixedAnchorKind.ReferenceOnlyMarker, SectorFixedAnchorSource.SpecialRegionSnapshot,
                        SectorFixedAnchorPriority.ReferenceOnly, new SectorFixedAnchorRect(23, 15, 1, 1), "REGION_VILLAGE"),
                };
                AddSpecial(result, Core, "CORE", "REGION_CORE", "SITE_CORE");
                AddSpecial(result, Forge, "FORGE", "REGION_FORGE", "SITE_FORGE");
                AddSpecial(result, Boss, "BOSS", "REGION_BOSS", "SITE_BOSS");
                return result;
            }

            private static SectorFixedAnchorProjection RouteAnchor(
                string id, string source, SectorPlannerSide side, SectorFixedAnchorRect rect) =>
                new SectorFixedAnchorProjection(id, Plain, SectorFixedAnchorKind.ExternalRouteSocket,
                    SectorFixedAnchorSource.RouteSnapshot, SectorFixedAnchorPriority.ExternalRouteSocket,
                    rect, source, side);

            private static void AddSpecial(
                ICollection<SectorFixedAnchorProjection> result,
                SectorCoord coordinate,
                string token,
                string region,
                string site)
            {
                result.Add(new SectorFixedAnchorProjection("ANCHOR_" + token + "_FOOTPRINT", coordinate,
                    SectorFixedAnchorKind.SpecialFootprint, SectorFixedAnchorSource.SpecialRegionSnapshot,
                    SectorFixedAnchorPriority.SpecialReservation, new SectorFixedAnchorRect(18, 12, 12, 8),
                    region, placedOwnershipClaim: true, progressionBlockerClaim: true));
                result.Add(new SectorFixedAnchorProjection("ANCHOR_" + token + "_ENTRY", coordinate,
                    SectorFixedAnchorKind.SpecialEntryReturn, SectorFixedAnchorSource.SpecialRegionSnapshot,
                    SectorFixedAnchorPriority.SpecialTransition, new SectorFixedAnchorRect(16, 14, 2, 4),
                    region, placedOwnershipClaim: true, progressionBlockerClaim: true));
                result.Add(new SectorFixedAnchorProjection("ANCHOR_" + token + "_BUFFER", coordinate,
                    SectorFixedAnchorKind.SpecialApronBuffer, SectorFixedAnchorSource.SpecialRegionSnapshot,
                    SectorFixedAnchorPriority.SpecialTransition, new SectorFixedAnchorRect(30, 12, 2, 8),
                    region, placedOwnershipClaim: true, progressionBlockerClaim: true));
                result.Add(new SectorFixedAnchorProjection("ANCHOR_" + token + "_SITE", coordinate,
                    SectorFixedAnchorKind.SiteReservation, SectorFixedAnchorSource.SiteSnapshot,
                    SectorFixedAnchorPriority.SpecialReservation, new SectorFixedAnchorRect(20, 22, 4, 2),
                    site, placedOwnershipClaim: true));
            }

            private static List<SectorClusterSourceProjection> CreateClusterCatalog()
            {
                var access = new[] { AccessClass.MandatoryNoTool };
                var route = new[] { 1 };
                var sockets = new[] { "SOCKET_L", "SOCKET_R", "SOCKET_U", "SOCKET_D" };
                return new List<SectorClusterSourceProjection>
                {
                    Source("TC_REF_TRAVERSAL_BRIDGE", "SPINE_TRAVERSAL_R0", MoonpalaceBiomeId.MoonCrater, PacingRole.Traversal, route, access, sockets, H2(), Origins(2,1), false, false, 10),
                    Source("TC_REF_QUIET_BUFFER", "SPINE_QUIET_R0", MoonpalaceBiomeId.MoonCrater, PacingRole.Quiet, route, access, null, H2(), Origins(2,1), true, false, 20),
                    Source("TC_REF_VILLAGE_APPROACH", "SPINE_SAFE_R0", MoonpalaceBiomeId.CassiaRoot, PacingRole.Safe, route, access, null, H2(), Origins(2,1), false, true, 30),
                    Source("TC_REF_CORE_RESOURCE_RING", "SPINE_RESOURCE_R0", MoonpalaceBiomeId.CassiaRoot, PacingRole.Resource, route, access, null, H4(), Origins(4,1), false, true, 40),
                    Source("TC_REF_FORGE_MACHINERY", "SPINE_LANDMARK_R0", MoonpalaceBiomeId.AbandonedMill, PacingRole.Landmark, route, access, null, H4(), Origins(4,1), false, true, 50),
                    Source("TC_REF_BOSS_GATE", "SPINE_BOSS_R0", MoonpalaceBiomeId.MoonDough, PacingRole.Boss, route, access, null, Boss5(), Origins(4,2), false, true, 60),
                    Source("TC_REF_ACTIVITY_SHELL", "SPINE_ACTIVITY_R0", MoonpalaceBiomeId.MoonCrater, PacingRole.Activity, route, access, null, H2(), Origins(2,1), false, false, 70),
                    Source("TC_REF_DISCOVERY_PASSAGE", "SPINE_DISCOVERY_R0", MoonpalaceBiomeId.CassiaRoot, PacingRole.Discovery, route, access, null, H2(), Origins(2,1), false, true, 80),
                    Source("TC_REF_NEIGHBOR_FLOW", "SPINE_NEIGHBOR_R0", MoonpalaceBiomeId.AbandonedMill, PacingRole.Traversal, route, access, null, L3(), Origins(2,2), false, false, 90),
                };
            }

            private static SectorClusterSourceProjection Source(
                string cluster, string variant, MoonpalaceBiomeId biome, PacingRole pacing,
                IEnumerable<int> routes, IEnumerable<AccessClass> access, IEnumerable<string> sockets,
                IEnumerable<SectorClusterFootprintCell> cells, IEnumerable<SectorClusterFootprintCell> origins,
                bool quiet, bool special, int order) =>
                new SectorClusterSourceProjection(new TerrainClusterId(cluster), new SpineVariantId(variant),
                    ClusterFootprintTransform.R0, biome, new[] { pacing }, routes, access, sockets,
                    cells, origins, 2, 5, quiet, special, order, 0);

            private static List<SectorPatternSourceProjection> CreatePatternCatalog()
            {
                var roles = Enum.GetValues(typeof(SectorClusterRoleCellKind)).Cast<SectorClusterRoleCellKind>().ToArray();
                var pacing = Enum.GetValues(typeof(PacingRole)).Cast<PacingRole>().Where(value => value != PacingRole.None).ToArray();
                return new List<SectorPatternSourceProjection>
                {
                    Pattern("MP_REF_BODY", 1, new[] { SectorPatternZoneKind.ClusterBody }, roles, pacing, 10),
                    Pattern("MP_REF_EDGE", 2, new[] { SectorPatternZoneKind.ClusterEdge }, roles, pacing, 20),
                    Pattern("MP_REF_ROUTE", 3, new[] { SectorPatternZoneKind.RouteShoulder }, roles, pacing, 30),
                    Pattern("MP_REF_BOUNDARY", 4, new[] { SectorPatternZoneKind.BoundaryBlend }, roles, pacing, 40),
                    Pattern("MP_REF_SPECIAL", 5, new[] { SectorPatternZoneKind.SpecialApproach }, roles, pacing, 50),
                    Pattern("MP_REF_RECOVERY", 6, new[] { SectorPatternZoneKind.Recovery }, roles, pacing, 60),
                    Pattern("MP_REF_QUIET", 7, new[] { SectorPatternZoneKind.QuietBuffer }, roles, pacing, 70),
                    Pattern("MP_REF_DETAIL", 8, new[] { SectorPatternZoneKind.Detail }, roles, pacing, 80),
                    Pattern("MP_REF_PROTECTED", 9, new[] { SectorPatternZoneKind.ProtectedNoWrite }, roles, pacing, 90),
                };
            }

            private static SectorPatternSourceProjection Pattern(
                string id, int salt, IEnumerable<SectorPatternZoneKind> zones,
                IEnumerable<SectorClusterRoleCellKind> roles, IEnumerable<PacingRole> pacing, int order) =>
                new SectorPatternSourceProjection(PatternDefinition(id, salt), MicroPatternTransform.R0,
                    zones, roles, pacing, "SIG_" + id, order);

            private static MicroPatternDefinition PatternDefinition(string id, int salt)
            {
                var cells = new List<MicroPatternCell>();
                for (var y = 0; y < 4; y++)
                for (var x = 0; x < 4; x++)
                    cells.Add(new MicroPatternCell(new LocalTileCoord(x, y), new[]
                    {
                        new MicroPatternInstruction(MicroPatternLayer.Geometry,
                            (x + y + salt) % 3 == 0 ? MicroPatternOperation.AddSolid : MicroPatternOperation.CarveAir),
                        new MicroPatternInstruction(MicroPatternLayer.Surface, MicroPatternOperation.SetSurface, "SURFACE_" + id),
                        new MicroPatternInstruction(MicroPatternLayer.Material, MicroPatternOperation.SetMaterial, "MATERIAL_" + id),
                    }));
                return new MicroPatternDefinition(new MicroPatternId(id), 4, 4, cells, 10 + salt,
                    new[] { MoonpalaceBiomeId.MoonCrater, MoonpalaceBiomeId.CassiaRoot,
                        MoonpalaceBiomeId.AbandonedMill, MoonpalaceBiomeId.MoonDough },
                    new[] { MicroPatternTransform.R0 }, MicroPatternProtectedPolicy.ForceNoChange, id);
            }

            private static BiomePatchSnapshot Ownership()
            {
                var grouped = Enumerable.Range(0, WorldGenConstants.SectorCount)
                    .GroupBy(BiomeForIndex).OrderBy(value => value.Key.CanonicalId, StringComparer.Ordinal).ToArray();
                var patches = new List<BiomePatch>();
                var patchByBiome = new Dictionary<MoonpalaceBiomeId, BiomePatchId>();
                foreach (var group in grouped)
                {
                    var indices = group.ToArray();
                    var id = new BiomePatchId("PATCH_MAP14_06_" + BiomeToken(group.Key));
                    patchByBiome.Add(group.Key, id);
                    patches.Add(new BiomePatch(id, BiomeToken(group.Key), "RULE_MAP14_06",
                        BiomePatchRole.Satellite,
                        new[] { new BiomePatchSeed(indices[0], WorldGridIndex.ToCoordinate(indices[0]), BiomePatchRole.Satellite, null) },
                        indices));
                }
                var ownership = Enumerable.Range(0, WorldGenConstants.SectorCount).Select(index =>
                {
                    var biome = BiomeForIndex(index);
                    return new BiomeSectorOwnership(index, WorldGridIndex.ToCoordinate(index),
                        BiomeToken(biome), string.Empty, patchByBiome[biome]);
                }).ToArray();
                return new BiomePatchSnapshot(1406UL, patches, ownership, Array.Empty<BiomePatchSiteBinding>());
            }

            private static MoonpalaceBiomeId BiomeForIndex(int index)
            {
                if (index == 16 || index == 17 || index == 21) return MoonpalaceBiomeId.CassiaRoot;
                if (index == 18 || index == 22) return MoonpalaceBiomeId.AbandonedMill;
                if (index == 19) return MoonpalaceBiomeId.MoonDough;
                return MoonpalaceBiomeId.MoonCrater;
            }

            private static string BiomeToken(MoonpalaceBiomeId biome)
            {
                if (biome == MoonpalaceBiomeId.MoonCrater) return "BIO_MOON_CRATER";
                if (biome == MoonpalaceBiomeId.CassiaRoot) return "BIO_CASSIA_ROOT";
                if (biome == MoonpalaceBiomeId.AbandonedMill) return "BIO_ABANDONED_MILL";
                return "BIO_MOON_DOUGH";
            }

            private static MoonpalaceBiomeId Biome(SectorPlannerSectorSnapshot sector) =>
                sector.Coordinate == Village || sector.Coordinate == Core || sector.Coordinate == Deferred
                    ? MoonpalaceBiomeId.CassiaRoot
                    : sector.Coordinate == Forge || sector.Coordinate == Neighbor
                        ? MoonpalaceBiomeId.AbandonedMill
                        : sector.Coordinate == Boss ? MoonpalaceBiomeId.MoonDough : MoonpalaceBiomeId.MoonCrater;

            private static LocalTileCoord[] FindRectangle(SectorQuietFillPlan fill, SectorCoord sector)
            {
                var eligible = new HashSet<LocalTileCoord>(fill.Cells.Where(value => value.SectorCoordinate == sector && value.ActivityEligible)
                    .Select(value => value.Coordinate));
                for (var y = 0; y < 31; y++)
                for (var x = 0; x < 47; x++)
                {
                    var result = new[] { new LocalTileCoord(x, y), new LocalTileCoord(x + 1, y),
                        new LocalTileCoord(x, y + 1), new LocalTileCoord(x + 1, y + 1) };
                    if (result.All(eligible.Contains)) return result;
                }
                throw new InvalidOperationException("No eligible 2x2 Quiet rectangle in " + sector);
            }

            private static List<EventOverlayAssignmentProfile> EventProfiles()
            {
                var biomes = new[] { MoonpalaceBiomeId.MoonCrater, MoonpalaceBiomeId.CassiaRoot,
                    MoonpalaceBiomeId.AbandonedMill, MoonpalaceBiomeId.MoonDough };
                var pacing = Enum.GetValues(typeof(PacingRole)).Cast<PacingRole>().Where(value => value != PacingRole.None).ToArray();
                var access = new[] { AccessClass.MandatoryNoTool };
                var cluster = new TerrainClusterId("TC_MAP14_06_EVENT");
                var empty = new EventOverlayContract(new EventOverlayId("EVT_MAP14_06_EMPTY"),
                    EventOverlayKind.Empty, cluster, null, Array.Empty<EventMarkerAssignment>());
                var terrain = new EventOverlayContract(new EventOverlayId("EVT_MAP14_06_TERRAIN"),
                    EventOverlayKind.Cosmetic, cluster, null,
                    new[] { new EventMarkerAssignment(new EventMarkerId("MARKER_MAP14_06"),
                        EventMarkerOperation.EnableMarker, "MARKER_ONLY") });
                return new List<EventOverlayAssignmentProfile>
                {
                    new EventOverlayAssignmentProfile(empty, 0, 0, biomes, pacing, access),
                    new EventOverlayAssignmentProfile(terrain, 100, 2, biomes, pacing, access),
                };
            }

            private static SectorActivityEventMarkerKind MarkerForActivity(PacingRole role) =>
                role == PacingRole.Recovery ? SectorActivityEventMarkerKind.ActivityRecovery :
                role == PacingRole.Resource ? SectorActivityEventMarkerKind.ActivityReward :
                role == PacingRole.Activity ? SectorActivityEventMarkerKind.ActivityCore :
                SectorActivityEventMarkerKind.ActivityCue;

            private static SectorActivityEventMarkerKind MarkerForEvent(SectorPlannerSectorSnapshot sector) =>
                sector.SpecialRegion.Kind != SectorPlannerSpecialRegionKind.None
                    ? SectorActivityEventMarkerKind.EventSpecial
                    : sector.ActivityCatalogAvailable
                        ? SectorActivityEventMarkerKind.EventActivity
                        : SectorActivityEventMarkerKind.EventTerrain;

            private static DeterministicRngStreamFactory RngFactory()
            {
                var definitions = new SortedDictionary<string, RngStreamDefinition>(StringComparer.Ordinal)
                {
                    { WorldGenerationRngStreams.SectorRecipeStreamId,
                        Definition(WorldGenerationRngStreams.SectorRecipeStreamId, "E9931A70C2D520F4", "SECTOR") },
                    { WorldGenerationRngStreams.PopulationStreamId,
                        Definition(WorldGenerationRngStreams.PopulationStreamId, "A63D4078F9E21C55", "SPAWN") },
                };
                var set = (WorldRouteDefinitionSet)FormatterServices.GetUninitializedObject(typeof(WorldRouteDefinitionSet));
                SetAutoProperty(set, "RngStreams", new ReadOnlyDictionary<string, RngStreamDefinition>(definitions));
                return new DeterministicRngStreamFactory(set);
            }

            private static RngStreamDefinition Definition(string id, string salt, string scope)
            {
                var definition = (RngStreamDefinition)FormatterServices.GetUninitializedObject(typeof(RngStreamDefinition));
                SetAutoProperty(definition, "RngStreamId", id);
                SetAutoProperty(definition, "SaltHex", Hex(salt));
                SetAutoProperty(definition, "ResetScope", scope);
                SetAutoProperty(definition, "DescriptionKo", "MAP14_06 focused fixture");
                SetAutoProperty(definition, "Active", true);
                return definition;
            }

            private static CsvHexValue Hex(string value)
            {
                var bytes = Enumerable.Range(0, value.Length / 2)
                    .Select(index => byte.Parse(value.Substring(index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray();
                var constructor = typeof(CsvHexValue).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
                    null, new[] { typeof(string), typeof(IEnumerable<byte>) }, null);
                Assert.That(constructor, Is.Not.Null);
                return (CsvHexValue)constructor.Invoke(new object[] { value, bytes });
            }

            private static void SetAutoProperty(object target, string property, object value)
            {
                var field = target.GetType().GetField("<" + property + ">k__BackingField",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null, property);
                field.SetValue(target, value);
            }

            private static SectorClusterFootprintCell[] H2() => new[] { Cell(0, 0), Cell(1, 0) };
            private static SectorClusterFootprintCell[] H4() => new[] { Cell(0, 0), Cell(1, 0), Cell(2, 0), Cell(3, 0) };
            private static SectorClusterFootprintCell[] L3() => new[] { Cell(0, 0), Cell(1, 0), Cell(0, 1) };
            private static SectorClusterFootprintCell[] Boss5() => new[] { Cell(0, 0), Cell(1, 0), Cell(2, 0), Cell(3, 0), Cell(0, 1) };
            private static SectorClusterFootprintCell Cell(int x, int y) => new SectorClusterFootprintCell(x, y);
            private static SectorClusterFootprintCell[] Origins(int width, int height)
            {
                var result = new List<SectorClusterFootprintCell>();
                for (var y = 0; y <= 4 - height; y++)
                for (var x = 0; x <= 4 - width; x++) result.Add(Cell(x, y));
                return result.ToArray();
            }

            private static void Require<T>(bool success, IEnumerable<T> errors)
            {
                if (!success) throw new InvalidOperationException(string.Join(";", errors));
            }

            private static string Hash(string value)
            {
                using (var sha = SHA256.Create())
                {
                    var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                    var result = new StringBuilder(bytes.Length * 2);
                    foreach (var item in bytes) result.Append(item.ToString("x2", CultureInfo.InvariantCulture));
                    return result.ToString();
                }
            }
        }

        private sealed class AuthorityPackage
        {
            internal AuthorityPackage(
                IEnumerable<SectorActivityOpportunityProjection> activities,
                ActivityCandidateIndex activityIndex,
                ActivityFrequencyPlan activityPlan,
                IEnumerable<SectorEventMarkerOpportunityProjection> events,
                EventOverlayCandidateIndex eventIndex,
                EventOverlayAssignmentPlan eventPlan)
            {
                Activities = activities.ToArray();
                ActivityIndex = activityIndex;
                ActivityPlan = activityPlan;
                Events = events.ToArray();
                EventIndex = eventIndex;
                EventPlan = eventPlan;
            }

            internal SectorActivityOpportunityProjection[] Activities { get; }
            internal ActivityCandidateIndex ActivityIndex { get; }
            internal ActivityFrequencyPlan ActivityPlan { get; }
            internal SectorEventMarkerOpportunityProjection[] Events { get; }
            internal EventOverlayCandidateIndex EventIndex { get; }
            internal EventOverlayAssignmentPlan EventPlan { get; }

            internal SectorActivityEventPlacementRequest Request(
                SectorQuietFillPlan fill,
                IEnumerable<SectorQuietActivityEventErrorCode> referenceFaults = null,
                bool activityMarkerMutationClaim = false,
                bool eventMarkerMutationClaim = false,
                bool specialPersistenceMutationClaim = false,
                bool ownershipMutationClaim = false,
                int solverInvocationCount = 0,
                int map14RngDrawCount = 0,
                int retryCount = 0,
                int tileWriteCount = 0) =>
                new SectorActivityEventPlacementRequest(
                    fill, Activities, ActivityIndex, ActivityPlan, Events, EventIndex, EventPlan,
                    SectorActivityEventPlacementPlanner.ReferencePublicationLabel,
                    referenceFaults: referenceFaults,
                    activityMarkerMutationClaim: activityMarkerMutationClaim,
                    eventMarkerMutationClaim: eventMarkerMutationClaim,
                    specialPersistenceMutationClaim: specialPersistenceMutationClaim,
                    ownershipMutationClaim: ownershipMutationClaim,
                    solverInvocationCount: solverInvocationCount,
                    map14RngDrawCount: map14RngDrawCount,
                    retryCount: retryCount,
                    tileWriteCount: tileWriteCount);
        }
    }
}
