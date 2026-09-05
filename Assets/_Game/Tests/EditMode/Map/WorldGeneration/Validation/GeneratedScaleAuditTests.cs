using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Validation;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Validation
{
    [TestFixture]
    [Category("MAP19_09")]
    public sealed class GeneratedScaleAuditTests
    {
        [Test]
        public void ScaleAuditBuildsNonOverlappingOneKTenKHundredKTiers()
        {
            var tiers = GeneratedScaleAuditTierCatalog.All;

            Assert.That(tiers.Select(value => value.TierId), Is.EqualTo(new[] { "A", "B", "C" }));
            Assert.That(tiers.Select(value => value.SeedStart),
                Is.EqualTo(new[] { 0UL, 1000UL, 10000UL }));
            Assert.That(tiers.Select(value => value.SeedCount),
                Is.EqualTo(new[] { 1000UL, 9000UL, 90000UL }));
            Assert.That(tiers.Select(value => value.SeedEndInclusive),
                Is.EqualTo(new[] { 999UL, 9999UL, 99999UL }));
            Assert.That(GeneratedScaleAuditTierCatalog.IsValid(tiers), Is.True);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                GeneratedScaleAuditTierCatalog.DefinitionsDigest), Is.True);
        }

        [Test]
        public void ScaleAuditConsumesMap19_08HandoffAndRejectsDigestMismatch()
        {
            var valid = Run(Preconditions(), SmallTiers());
            var invalid = Run(Preconditions(handoff: Hash("WRONG_HANDOFF")), SmallTiers());

            Assert.That(valid.Success, Is.True, valid.ToReportText());
            Assert.That(invalid.Status, Is.EqualTo(GeneratedScaleAuditStatus.BlockedPrecondition));
            Assert.That(invalid.ExecutedSeeds, Is.Zero);
            Assert.That(invalid.Map19ExitDigest, Is.Empty);
            Assert.That(invalid.Map20_01HandoffDigest, Is.Empty);
        }

        [Test]
        public void ScaleAuditStopsBeforeNextTierWhenFailureAppears()
        {
            var writes = new BundleCapture();
            var summary = Run(Preconditions(), SmallTiers(), writes.Write,
                (seed, digest) => seed == 1UL
                    ? GeneratedScaleAuditSeedResult.Fail(seed,
                        GeneratedScaleAuditFailureKind.MandatoryRoute, "MAP19_02",
                        "MANDATORY_ROUTE_FAILURE", "MANDATORY_ROUTE", "REACHABLE", "UNREACHABLE", digest)
                    : GeneratedScaleAuditSeedResult.Pass(seed, Hash(seed.ToString(CultureInfo.InvariantCulture))));

            Assert.That(summary.Status, Is.EqualTo(GeneratedScaleAuditStatus.Fail));
            Assert.That(summary.TierResults.Single(value => value.Tier.TierId == "A").Started, Is.True);
            Assert.That(summary.TierResults.Single(value => value.Tier.TierId == "B").Started, Is.False);
            Assert.That(summary.TierResults.Single(value => value.Tier.TierId == "C").Started, Is.False);
            Assert.That(summary.Map20_01HandoffDigest, Is.Empty);
            Assert.That(writes.Seeds, Is.EqualTo(new[] { 1UL }));
        }

        [Test]
        public void ScaleAuditWritesFailureBundlesForFirstDistinctFailuresOnly()
        {
            var writes = new BundleCapture();
            var tiers = new[]
            {
                new GeneratedScaleAuditTier("A", 0UL, 10UL, "PRECONDITIONS_PASS"),
                new GeneratedScaleAuditTier("B", 10UL, 1UL, "TIER_A_PASS"),
                new GeneratedScaleAuditTier("C", 11UL, 1UL, "TIER_B_PASS_AND_RUNTIME_BUDGET_ACCEPTED"),
            };
            var summary = Run(Preconditions(), tiers, writes.Write, (seed, digest) =>
                GeneratedScaleAuditSeedResult.Fail(seed,
                    GeneratedScaleAuditFailureKind.RepetitionRemoval, "MAP19_05",
                    "REPETITION_REMOVAL_FAILURE", "REPETITION_REMOVAL", "ZERO", "ONE", digest));

            Assert.That(summary.Status, Is.EqualTo(GeneratedScaleAuditStatus.Fail));
            Assert.That(summary.TierResults[0].Executed, Is.EqualTo(10UL));
            Assert.That(summary.FailureBundleCount,
                Is.EqualTo(GeneratedScaleAuditContract.MaximumFailureBundles));
            Assert.That(writes.Seeds, Is.EqualTo(new[] { 0UL, 1UL, 2UL, 3UL, 4UL }));
            Assert.That(writes.SchemaDigests.Distinct().Single(),
                Is.EqualTo(GeneratedScaleAuditContract.ExpectedFailureBundleSchemaDigest));
        }

        [Test]
        public void ScaleAuditPublishesNoMap20HandoffOnFailureOrBlockedRuntime()
        {
            var writes = new BundleCapture();
            var blocked = Run(Preconditions(), SmallTiers(), writes.Write, null,
                new StepClock(1000L).Next, 1000L, 1d);
            var failed = Run(Preconditions(), SmallTiers(), writes.Write, (seed, digest) =>
                GeneratedScaleAuditSeedResult.Fail(seed,
                    GeneratedScaleAuditFailureKind.Completion, "MAP19_03", "COMPLETION_FAILURE",
                    "COMPLETION", "SATISFIED", "UNSATISFIED", digest));

            Assert.That(blocked.Status, Is.EqualTo(GeneratedScaleAuditStatus.BlockedRuntimeBudget));
            Assert.That(blocked.TierResults.Single(value => value.Tier.TierId == "C").Started, Is.False);
            Assert.That(blocked.FailureBundleCount, Is.EqualTo(1));
            Assert.That(blocked.Map19ExitDigest, Is.Empty);
            Assert.That(blocked.Map20_01HandoffDigest, Is.Empty);
            Assert.That(failed.Map20_01HandoffDigest, Is.Empty);
        }

        [Test]
        public void ScaleAuditSummaryDigestIsStableAcrossRepeatCultureAndWorkerOrder()
        {
            var original = CultureInfo.CurrentCulture;
            try
            {
                var forward = Run(Preconditions(), SmallTiers(), null, null,
                    new StepClock(1L).Next, 1000L);
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                var reverseChain = ReverseChain();
                var reversed = GeneratedScaleAuditOrchestrator.Run(Preconditions(), reverseChain,
                    null, null, new StepClock(1L).Next, 1000L, SmallTiers(),
                    GeneratedScaleAuditContract.TierCRuntimeBudgetMilliseconds);
                var repeated = Run(Preconditions(), SmallTiers(), null, null,
                    new StepClock(1L).Next, 1000L);

                Assert.That(forward.Success, Is.True);
                Assert.That(reversed.Success, Is.True);
                Assert.That(forward.SummaryDigest, Is.EqualTo(reversed.SummaryDigest));
                Assert.That(forward.SummaryDigest, Is.EqualTo(repeated.SummaryDigest));
                Assert.That(forward.Map19ExitDigest, Is.EqualTo(repeated.Map19ExitDigest));
                Assert.That(forward.Map20_01HandoffDigest, Is.EqualTo(repeated.Map20_01HandoffDigest));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Test]
        public void ScaleAuditDoesNotSelectLegacyRegressionPriorCategoriesOrPlayMode()
        {
            Assert.That(GeneratedScaleAuditBoundary.Legacy19347Selections, Is.Zero);
            Assert.That(GeneratedScaleAuditBoundary.PriorTaskTestSelections, Is.Zero);
            Assert.That(GeneratedScaleAuditBoundary.PlayModeSelections, Is.Zero);
            Assert.That(GeneratedScaleAuditBoundary.UnfilteredTestSelections, Is.Zero);
            Assert.That(GeneratedScaleAuditBoundary.FullRegressionRuns, Is.Zero);
            Assert.That(GeneratedScaleAuditBoundary.ScenePrefabTilemapRuntimeMutations, Is.Zero);
            Assert.That(GeneratedScaleAuditBoundary.RealScreenshotCaptures, Is.Zero);
        }

        [Test]
        public void ScaleAuditReportsScriptResponsibilitiesAndNonOwnership()
        {
            var rows = GeneratedScaleAuditResponsibilities.All;

            Assert.That(rows.Count, Is.EqualTo(4));
            Assert.That(rows.All(value => !string.IsNullOrWhiteSpace(value.Path) &&
                !string.IsNullOrWhiteSpace(value.Responsibility) &&
                !string.IsNullOrWhiteSpace(value.NonOwnership)), Is.True);
            Assert.That(rows.Any(value => value.Path.EndsWith("GeneratedScaleAudit.cs",
                StringComparison.Ordinal)), Is.True);
            Assert.That(rows.Any(value => value.Path.EndsWith("GeneratedScaleAuditEditorRunner.cs",
                StringComparison.Ordinal)), Is.True);
            Assert.That(rows.Any(value => value.Path.EndsWith("GeneratedHeadlessValidationRunner.cs",
                StringComparison.Ordinal)), Is.True);
            Assert.That(rows.All(value => value.NonOwnership.IndexOf("No ",
                StringComparison.Ordinal) >= 0), Is.True);
        }

        private static GeneratedScaleAuditSummary Run(GeneratedScaleAuditPreconditions preconditions,
            IEnumerable<GeneratedScaleAuditTier> tiers,
            GeneratedScaleAuditFailureBundleWriter writer = null,
            GeneratedScaleAuditSeedEvaluator evaluator = null,
            GeneratedScaleAuditTimestampProvider clock = null, long frequency = 1000L,
            double budget = GeneratedScaleAuditContract.TierCRuntimeBudgetMilliseconds) =>
            GeneratedScaleAuditOrchestrator.Run(preconditions, GeneratedScaleAuditChain.Create(),
                writer, evaluator, clock ?? new StepClock(1L).Next, frequency, tiers, budget);

        private static GeneratedScaleAuditPreconditions Preconditions(string handoff = null) =>
            new GeneratedScaleAuditPreconditions(
                GeneratedScaleAuditContract.ExpectedMap19_08ResultSha256,
                GeneratedScaleAuditContract.ExpectedMap19_08InstalledTaskSha256,
                handoff ?? GeneratedScaleAuditContract.ExpectedMap19_08HandoffDigest,
                GeneratedFailureBundleDigest.Schema,
                GeneratedScaleAuditContract.ExpectedRunnerPlanDigest,
                GeneratedScaleAuditContract.ExpectedMap19_09InstalledTaskSha256);

        private static GeneratedScaleAuditTier[] SmallTiers() => new[]
        {
            new GeneratedScaleAuditTier("A", 0UL, 2UL, "PRECONDITIONS_PASS"),
            new GeneratedScaleAuditTier("B", 2UL, 2UL, "TIER_A_PASS"),
            new GeneratedScaleAuditTier("C", 4UL, 2UL,
                "TIER_B_PASS_AND_RUNTIME_BUDGET_ACCEPTED"),
        };

        private static GeneratedValidationChainSnapshot ReverseChain()
        {
            var source = GeneratedScaleAuditChain.Create();
            return new GeneratedValidationChainSnapshot(source.References.Reverse(),
                source.IncomingHandoffDigest);
        }

        private static string Hash(string value) =>
            BakingCanonicalDigest.HashCanonicalLines(new[] { value });

        private sealed class StepClock
        {
            private long value;
            private readonly long step;
            public StepClock(long stepValue) { step = stepValue; }
            public long Next() { value += step; return value; }
        }

        private sealed class BundleCapture
        {
            private readonly List<ulong> seeds = new List<ulong>();
            private readonly List<string> schemaDigests = new List<string>();
            public IReadOnlyList<ulong> Seeds => seeds;
            public IReadOnlyList<string> SchemaDigests => schemaDigests;
            public bool Write(GeneratedScaleAuditFailure failure,
                GeneratedFailureBundlePayload payload, out string outputPath)
            {
                seeds.Add(failure.Seed);
                schemaDigests.Add(GeneratedFailureBundleDigest.Schema);
                outputPath = "memory/seed-" + failure.Seed.ToString(CultureInfo.InvariantCulture);
                return payload != null;
            }
        }
    }
}
