using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    [Category("MAP06_06")]
    public sealed class OptionalRewardTierCalculatorTests
    {
        private Type0RouteMaskAssignmentResult type0;
        private OptionalAccessAssignmentResult access;
        private MandatoryRouteGraph graph;
        private OptionalRewardTierSettings settings;
        private OptionalRewardTierResult baseline;
        private string sourceSignature;

        public static IEnumerable<int> SettingsAndTierCases => Enumerable.Range(0, 34);
        public static IEnumerable<int> FormulaCases => Enumerable.Range(0, 38);
        public static IEnumerable<int> ThresholdCases => Enumerable.Range(0, 34);
        public static IEnumerable<int> MatrixCases => Enumerable.Range(0, 36);
        public static IEnumerable<int> SourceChainCases => Enumerable.Range(0, 32);
        public static IEnumerable<int> ApprovedFixtureCases => Enumerable.Range(0, 32);
        public static IEnumerable<int> AtomicCases => Enumerable.Range(0, 26);
        public static IEnumerable<int> DeterminismCases => Enumerable.Range(0, 24);
        public static IEnumerable<int> IntegrityCases => Enumerable.Range(0, 22);

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var fixture = new OptionalAccessRuleAssignerTests();
            fixture.OneTimeSetUp();
            type0 = GetField<Type0RouteMaskAssignmentResult>(fixture, "type0");
            access = GetField<OptionalAccessAssignmentResult>(fixture, "baseline");
            graph = GetField<MandatoryRouteGraph>(fixture, "graph");
            settings = ApprovedSettings();
            sourceSignature = SourceSignature();
            baseline = new OptionalRewardTierCalculator().Calculate(type0, access, settings);

            Assert.That(type0.IsSuccess, Is.True);
            Assert.That(access.IsSuccess, Is.True);
            Assert.That(baseline.IsSuccess, Is.True, FormatErrors(baseline));
            Assert.That(baseline.Assignments, Has.Count.EqualTo(12));
        }

        [TestCaseSource(nameof(SettingsAndTierCases))]
        public void TierEnumTokensSettingsCopyAndValidationAreExact(int caseId)
        {
            if (caseId < 22)
            {
                var minimums = new List<int> { 0, 4, 8, 12 };
                var value = new OptionalRewardTierSettings(2, 10, minimums);
                minimums[1] = 999;

                Assert.That(value.DepthWeight, Is.EqualTo(2));
                Assert.That(value.ExplosiveFuelDivisor, Is.EqualTo(10));
                Assert.That(value.TierMinimumScores, Is.EqualTo(new[] { 0, 4, 8, 12 }));
                Assert.That(OptionalRegionTokenCodec.ToToken(OptionalRewardTier.None), Is.EqualTo("NONE"));
                Assert.That(OptionalRegionTokenCodec.ToToken(OptionalRewardTier.Low), Is.EqualTo("LOW"));
                Assert.That(OptionalRegionTokenCodec.ToToken(OptionalRewardTier.Medium), Is.EqualTo("MEDIUM"));
                Assert.That(OptionalRegionTokenCodec.ToToken(OptionalRewardTier.High), Is.EqualTo("HIGH"));
                Assert.That(OptionalRegionTokenCodec.ToToken(OptionalRewardTier.Unique), Is.EqualTo("UNIQUE"));
                return;
            }

            Assert.That(InvalidSettings(caseId - 22), Throws.InstanceOf<ArgumentException>());
        }

        [TestCaseSource(nameof(FormulaCases))]
        public void ScoreFormulaComponentsIntegerDivisionAndCheckedOverflowAreExact(int caseId)
        {
            if (caseId % 19 == 18)
            {
                var method = typeof(OptionalRewardTierCalculator).GetMethod(
                    "CheckedRewardScore", BindingFlags.Static | BindingFlags.NonPublic);
                var arguments = new object[] { int.MaxValue, 2, 0, 0, 10, 0, 0, 0, 0, 0 };
                Assert.Throws<OverflowException>(() => method.Invoke(null, arguments));
                return;
            }

            var value = baseline.Assignments[caseId % baseline.Assignments.Count];
            Assert.That(value.DepthScore, Is.EqualTo(value.MaxDepth * 2));
            Assert.That(value.ToolCostScore, Is.EqualTo(value.ToolCostTier));
            Assert.That(value.ExplosiveFuelScore, Is.EqualTo(value.ExplosiveFuelCost / 10));
            Assert.That(value.HiddenClueScore, Is.EqualTo(value.HiddenClueDifficulty));
            Assert.That(value.RewardScore, Is.EqualTo(
                value.MaxDepth * 2 + value.ToolCostTier +
                value.ExplosiveFuelCost / 10 + value.HiddenClueDifficulty));
        }

        [TestCaseSource(nameof(ThresholdCases))]
        public void TierThresholdBoundariesNoneRejectionAndUniqueSaturationAreExact(int caseId)
        {
            var scores = new[] { 0, 1, 3, 4, 5, 7, 8, 9, 11, 12, 13, 1000000, int.MaxValue };
            var score = scores[caseId % scores.Length];
            var method = typeof(OptionalRewardTierCalculator).GetMethod(
                "SelectTier", BindingFlags.Static | BindingFlags.NonPublic);
            var tier = (OptionalRewardTier)method.Invoke(null, new object[] { score, settings.TierMinimumScores });
            var expected = score >= 12 ? OptionalRewardTier.Unique :
                score >= 8 ? OptionalRewardTier.High :
                score >= 4 ? OptionalRewardTier.Medium : OptionalRewardTier.Low;

            Assert.That(tier, Is.EqualTo(expected));
            Assert.That(tier, Is.Not.EqualTo(OptionalRewardTier.None));
            Assert.That(baseline.Assignments.All(value => value.RewardTier != OptionalRewardTier.None), Is.True);
        }

        [TestCaseSource(nameof(MatrixCases))]
        public void FiveAccessRuleMatrixAndUnusedCostRejectionAreExact(int caseId)
        {
            if (caseId % 9 == 8)
            {
                AssertInvalidUnusedCostAssignment();
                return;
            }

            var reward = baseline.Assignments[caseId % baseline.Assignments.Count];
            var source = access.Assignments.Single(value => value.RegionId == reward.RegionId);
            Assert.That(reward.AccessRule, Is.EqualTo(source.AccessRule));
            Assert.That(reward.ClueId, Is.EqualTo(source.Clue.ClueId));
            Assert.That(reward.ToolCostTier, Is.EqualTo(source.ToolCostTier));
            Assert.That(reward.ExplosiveFuelCost, Is.EqualTo(source.ExplosiveFuelCost));
            Assert.That(reward.HiddenClueDifficulty, Is.EqualTo(source.HiddenClueDifficulty));
            Assert.That(reward.RequiresPartialRewardPreview, Is.EqualTo(source.RequiresPartialRewardPreview));

            switch (source.AccessRule)
            {
                case OptionalRegionAccessRule.Basic:
                case OptionalRegionAccessRule.Environment:
                    Assert.That(reward.ToolCostScore + reward.ExplosiveFuelScore + reward.HiddenClueScore, Is.Zero);
                    break;
                case OptionalRegionAccessRule.Tool:
                    Assert.That(reward.ToolCostScore, Is.InRange(1, 4));
                    Assert.That(reward.ExplosiveFuelScore + reward.HiddenClueScore, Is.Zero);
                    break;
                case OptionalRegionAccessRule.Explosive:
                    Assert.That(reward.ExplosiveFuelCost, Is.InRange(1, 100));
                    Assert.That(reward.RequiresPartialRewardPreview, Is.True);
                    Assert.That(reward.ToolCostScore + reward.HiddenClueScore, Is.Zero);
                    break;
                case OptionalRegionAccessRule.Hidden:
                    Assert.That(reward.HiddenClueScore, Is.InRange(1, 4));
                    Assert.That(reward.ToolCostScore + reward.ExplosiveFuelScore, Is.Zero);
                    break;
                default:
                    Assert.Fail("Undefined access rule reached a successful reward assignment.");
                    break;
            }
        }

        [TestCaseSource(nameof(SourceChainCases))]
        public void SourceChainJoinDigestAccountingAndCanonicalOrderAreExact(int caseId)
        {
            if (caseId % 8 == 6)
            {
                var mismatch = CloneAccess(sourceType0Digest: new string('0', 64));
                var result = new OptionalRewardTierCalculator().Calculate(type0, mismatch, settings);
                AssertAtomicFailure(result, OptionalRewardTierCalculationStatus.InvalidSource);
                Assert.That(result.Errors.Any(value => value.Code == OptionalRewardTierCalculationErrorCode.SourceMismatch), Is.True);
                return;
            }
            if (caseId % 8 == 7)
            {
                var mismatch = CloneAccess(sourceGrowthDigest: new string('0', 64));
                var result = new OptionalRewardTierCalculator().Calculate(type0, mismatch, settings);
                AssertAtomicFailure(result, OptionalRewardTierCalculationStatus.InvalidSource);
                Assert.That(result.Errors.Any(value => value.Code == OptionalRewardTierCalculationErrorCode.SourceMismatch), Is.True);
                return;
            }

            Assert.That(baseline.SourceType0AssignmentDigest, Is.EqualTo(type0.CanonicalDigest));
            Assert.That(baseline.SourceAccessAssignmentDigest, Is.EqualTo(access.CanonicalDigest));
            Assert.That(baseline.SourceGrowthDigest, Is.EqualTo(type0.SourceGrowthDigest));
            Assert.That(baseline.Assignments.Select(value => value.RegionId),
                Is.Ordered.Using<OptionalRegionId>((left, right) => left.CompareTo(right)));
            Assert.That(baseline.Assignments.Select(value => value.RegionId).Distinct().Count(), Is.EqualTo(12));
        }

        [TestCaseSource(nameof(ApprovedFixtureCases))]
        public void ApprovedFixturePerRegionScoreTierAndComponentEvidenceIsExact(int caseId)
        {
            var reward = baseline.Assignments[caseId % baseline.Assignments.Count];
            var region = type0.SourceSnapshot.Regions.Single(value => value.RegionId == reward.RegionId);
            var source = access.Assignments.Single(value => value.RegionId == reward.RegionId);

            Assert.That(reward.RegionOrdinal, Is.EqualTo(
                type0.SourceSnapshot.Regions.OrderBy(value => value.RegionId).ToList()
                    .FindIndex(value => value.RegionId == reward.RegionId)));
            Assert.That(reward.AttachmentOrder, Is.EqualTo(region.Attachment.AttachmentOrder));
            Assert.That(reward.MaxDepth, Is.EqualTo(region.MaxDepth.Value));
            Assert.That(reward.ClueId, Is.EqualTo(source.Clue.ClueId));
            Assert.That(reward.RewardScore, Is.EqualTo(
                reward.DepthScore + reward.ToolCostScore + reward.ExplosiveFuelScore + reward.HiddenClueScore));
            Assert.That(reward.RewardTier, Is.Not.EqualTo(OptionalRewardTier.None));
        }

        [TestCaseSource(nameof(AtomicCases))]
        public void InvalidInputSettingsSourceAndBoundaryFailuresAreAtomic(int caseId)
        {
            OptionalRewardTierResult result;
            switch (caseId % 6)
            {
                case 0:
                    result = new OptionalRewardTierCalculator().Calculate(null, access, settings);
                    AssertAtomicFailure(result, OptionalRewardTierCalculationStatus.InvalidInput);
                    break;
                case 1:
                    result = new OptionalRewardTierCalculator().Calculate(type0, null, settings);
                    AssertAtomicFailure(result, OptionalRewardTierCalculationStatus.InvalidInput);
                    break;
                case 2:
                    result = new OptionalRewardTierCalculator().Calculate(type0, access, null);
                    AssertAtomicFailure(result, OptionalRewardTierCalculationStatus.InvalidSettings);
                    break;
                case 3:
                    result = new OptionalRewardTierCalculator().Calculate(
                        CloneType0(sourceRegionDelta: 1), access, settings);
                    AssertAtomicFailure(result, OptionalRewardTierCalculationStatus.InvalidSource);
                    break;
                case 4:
                    result = new OptionalRewardTierCalculator().Calculate(
                        CloneType0(baseOpen: 1), access, settings);
                    AssertAtomicFailure(result, OptionalRewardTierCalculationStatus.InvalidSource);
                    Assert.That(result.Errors.Any(value =>
                        value.Code == OptionalRewardTierCalculationErrorCode.OpenAttachmentBoundary), Is.True);
                    break;
                default:
                    result = new OptionalRewardTierCalculator().Calculate(
                        type0, CloneAccess(sourceType0Digest: new string('f', 64)), settings);
                    AssertAtomicFailure(result, OptionalRewardTierCalculationStatus.InvalidSource);
                    break;
            }
        }

        [TestCaseSource(nameof(DeterminismCases))]
        public void CanonicalDigestCultureOrderAndServiceReuseAreDeterministic(int caseId)
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo(caseId % 2 == 0 ? "tr-TR" : "fr-FR");
                var reordered = CloneAccess(reverse: true);
                var calculator = new OptionalRewardTierCalculator();
                var first = calculator.Calculate(type0, reordered, ApprovedSettings());
                var second = calculator.Calculate(type0, access, ApprovedSettings());

                Assert.That(first.IsSuccess, Is.True, FormatErrors(first));
                Assert.That(second.IsSuccess, Is.True, FormatErrors(second));
                Assert.That(first.CanonicalDigest, Is.EqualTo(baseline.CanonicalDigest));
                Assert.That(second.CanonicalDigest, Is.EqualTo(baseline.CanonicalDigest));
                Assert.That(first.Assignments.Select(AssignmentSignature),
                    Is.EqualTo(baseline.Assignments.Select(AssignmentSignature)));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [TestCaseSource(nameof(IntegrityCases))]
        public void SourceMutationRngBaseClosedType4AndBoundaryAdvanceRemainFrozen(int caseId)
        {
            switch (caseId % 6)
            {
                case 0:
                    Assert.That(SourceSignature(), Is.EqualTo(sourceSignature));
                    Assert.That(baseline.RngDrawCount, Is.Zero);
                    Assert.That(baseline.Diagnostics.SourceMutationCount, Is.Zero);
                    break;
                case 1:
                    Assert.That(type0.Diagnostics.AttachmentBoundaryClosedCount, Is.EqualTo(12));
                    Assert.That(type0.Diagnostics.MandatoryBoundaryBaseOpenCount, Is.Zero);
                    Assert.That(access.Diagnostics.AttachmentBoundaryBaseOpenCount, Is.Zero);
                    Assert.That(type0.Assignments.All(value => !value.OpenMask.HasHorizontalThrough), Is.True);
                    break;
                case 2:
                    var bits = caseId % 4;
                    var left = (bits & 1) != 0;
                    var right = (bits & 2) != 0;
                    Assert.That(graph.MaskFamily.TryResolve(left, right, true, true, out var mask), Is.True);
                    Assert.That(mask.OpenUp && mask.OpenDown, Is.True);
                    Assert.That(mask.OpenLeft, Is.EqualTo(left));
                    Assert.That(mask.OpenRight, Is.EqualTo(right));
                    break;
                case 3:
                    Assert.That(baseline.Diagnostics.MandatoryRewardSelectionCount, Is.Zero);
                    Assert.That(typeof(OptionalRewardTierAssignment).GetProperties().Any(value =>
                        value.Name.Contains("RewardId") || value.Name.Contains("Item") ||
                        value.Name.Contains("Pool") || value.Name.Contains("Quantity") ||
                        value.Name.Contains("Spawn")), Is.False);
                    break;
                case 4:
                    foreach (var name in new[]
                    {
                        "OptionalRewardTierCalculationStatus", "OptionalRewardTierCalculationErrorCode",
                        "OptionalRewardTierSettings", "OptionalRewardTierAssignment",
                        "OptionalRewardTierDiagnostics", "OptionalRewardTierCalculationError",
                        "OptionalRewardTierResult", "OptionalRewardTierCalculator"
                    })
                        Assert.That(typeof(OptionalRewardTierCalculator).Assembly.GetType(
                            "StarNight.Map.WorldGeneration.Generation." + name, false), Is.Not.Null);
                    break;
                default:
                    foreach (var name in new[]
                    {
                        "OptionalReturnConnection", "OptionalRegionOverlayRenderer",
                        "OptionalRegionValidationOverlayWindow", "OptionalRegionOverlay", "GeneratedOptionalRegionCsvWriter"
                    })
                        Assert.That(typeof(OptionalRewardTierCalculator).Assembly.GetType(
                            "StarNight.Map.WorldGeneration.Generation." + name, false), Is.Null);
                    break;
            }
        }

        [Test]
        public void ApprovedFixturePublishesCanonicalSummary()
        {
            foreach (var assignment in baseline.Assignments)
            {
                TestContext.WriteLine(
                    "MAP06_06_ASSIGNMENT region={0} ordinal={1} attachment={2} clue={3} rule={4} depth={5} tool={6} fuel={7} hidden={8} depthScore={9} toolScore={10} fuelScore={11} hiddenScore={12} rewardScore={13} tier={14} preview={15}",
                    assignment.RegionId.Value, assignment.RegionOrdinal, assignment.AttachmentOrder,
                    assignment.ClueId.Value, OptionalRegionTokenCodec.ToToken(assignment.AccessRule),
                    assignment.MaxDepth, assignment.ToolCostTier, assignment.ExplosiveFuelCost,
                    assignment.HiddenClueDifficulty, assignment.DepthScore, assignment.ToolCostScore,
                    assignment.ExplosiveFuelScore, assignment.HiddenClueScore, assignment.RewardScore,
                    OptionalRegionTokenCodec.ToToken(assignment.RewardTier),
                    assignment.RequiresPartialRewardPreview ? 1 : 0);
            }
            var diagnostics = baseline.Diagnostics;
            TestContext.WriteLine(
                "MAP06_06_SUMMARY source={0}/{1}/{2} tiers={3}/{4}/{5}/{6} contributions={7}/{8}/{9}/{10} scores={11}/{12} previews={13} mandatoryReward={14} baseOpen={15} rng={16} mutation={17} type0={18} access={19} growth={20} digest={21}",
                diagnostics.SourceRegionCount, diagnostics.SourceType0CellAssignmentCount,
                diagnostics.SourceAccessAssignmentCount, diagnostics.LowCount, diagnostics.MediumCount,
                diagnostics.HighCount, diagnostics.UniqueCount, diagnostics.DepthContributionTotal,
                diagnostics.ToolContributionTotal, diagnostics.ExplosiveContributionTotal,
                diagnostics.HiddenContributionTotal, diagnostics.RewardScoreMinimum,
                diagnostics.RewardScoreMaximum, diagnostics.RewardPreviewReservationCount,
                diagnostics.MandatoryRewardSelectionCount,
                access.Diagnostics.AttachmentBoundaryBaseOpenCount, baseline.RngDrawCount,
                diagnostics.SourceMutationCount, baseline.SourceType0AssignmentDigest,
                baseline.SourceAccessAssignmentDigest, baseline.SourceGrowthDigest, baseline.CanonicalDigest);

            Assert.That(type0.CanonicalDigest,
                Is.EqualTo("a26e73f25ff7267760a2507ec55b1acda3a6c39a8f3698fc37b190620ceca525"));
            Assert.That(access.CanonicalDigest,
                Is.EqualTo("5268b7ed2342f197fb5717c85dbfdb8e51b3c520c217ecf93ad14dc944ecf74f"));
            Assert.That(type0.SourceGrowthDigest,
                Is.EqualTo("1f00f718bdb8d79fbf88923be0d81e0075987267c024cc624086ee6194998caa"));
            Assert.That(diagnostics.SourceRegionCount, Is.EqualTo(12));
            Assert.That(diagnostics.SourceType0CellAssignmentCount, Is.EqualTo(39));
            Assert.That(diagnostics.SourceAccessAssignmentCount, Is.EqualTo(12));
            Assert.That(diagnostics.TierAssignmentCount, Is.EqualTo(12));
            Assert.That(diagnostics.LowCount + diagnostics.MediumCount +
                        diagnostics.HighCount + diagnostics.UniqueCount, Is.EqualTo(12));
            Assert.That(diagnostics.RewardPreviewReservationCount, Is.EqualTo(2));
            Assert.That(diagnostics.MandatoryRewardSelectionCount, Is.Zero);
            Assert.That(baseline.CanonicalDigest, Has.Length.EqualTo(64));
            Assert.That(baseline.CanonicalDigest.All(IsLowerHex), Is.True);
        }

        private static OptionalRewardTierSettings ApprovedSettings()
        {
            return new OptionalRewardTierSettings(2, 10, new[] { 0, 4, 8, 12 });
        }

        private static TestDelegate InvalidSettings(int caseId)
        {
            return () =>
            {
                var depthWeight = 2;
                var divisor = 10;
                IReadOnlyList<int> thresholds = new[] { 0, 4, 8, 12 };
                switch (caseId % 12)
                {
                    case 0: depthWeight = 0; break;
                    case 1: depthWeight = 101; break;
                    case 2: divisor = 0; break;
                    case 3: divisor = 101; break;
                    case 4: thresholds = null; break;
                    case 5: thresholds = new[] { 0, 4, 8 }; break;
                    case 6: thresholds = new[] { 1, 4, 8, 12 }; break;
                    case 7: thresholds = new[] { 0, 4, 4, 12 }; break;
                    case 8: thresholds = new[] { 0, 8, 4, 12 }; break;
                    case 9: thresholds = new[] { 0, 4, 8, -1 }; break;
                    case 10: thresholds = new[] { 0, 4, 8, 1000001 }; break;
                    default: thresholds = Array.Empty<int>(); break;
                }
                new OptionalRewardTierSettings(depthWeight, divisor, thresholds);
            };
        }

        private void AssertInvalidUnusedCostAssignment()
        {
            var source = baseline.Assignments.First(value => value.AccessRule == OptionalRegionAccessRule.Basic);
            var constructor = typeof(OptionalRewardTierAssignment)
                .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
            Assert.That(() => constructor.Invoke(new object[]
            {
                source.RegionId, source.RegionOrdinal, source.AttachmentOrder, source.ClueId,
                source.AccessRule, source.MaxDepth, 1, 0, 0, source.DepthScore, 1, 0, 0,
                source.DepthScore + 1, source.RewardTier, false
            }), Throws.TypeOf<TargetInvocationException>().With.InnerException.InstanceOf<ArgumentException>());
        }

        private OptionalAccessAssignmentResult CloneAccess(
            bool reverse = false,
            string sourceType0Digest = null,
            string sourceGrowthDigest = null)
        {
            IEnumerable<OptionalAccessAssignment> assignments = access.Assignments;
            IEnumerable<OptionalAccessClue> clues = access.Clues;
            if (reverse)
            {
                assignments = assignments.Reverse();
                clues = clues.Reverse();
            }
            var constructor = typeof(OptionalAccessAssignmentResult)
                .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
            return (OptionalAccessAssignmentResult)constructor.Invoke(new object[]
            {
                OptionalAccessAssignmentStatus.Completed, assignments, clues, access.Diagnostics,
                Array.Empty<OptionalAccessAssignmentError>(), sourceType0Digest ?? access.SourceType0AssignmentDigest,
                sourceGrowthDigest ?? access.SourceGrowthDigest, access.CanonicalDigest
            });
        }

        private Type0RouteMaskAssignmentResult CloneType0(int baseOpen = 0, int sourceRegionDelta = 0)
        {
            var source = type0.Diagnostics;
            var diagnostics = new Type0RouteMaskAssignmentDiagnostics(
                source.SourceRouteMaskDefinitionCount, source.RegisteredType0MaskCount,
                source.IgnoredNonType0DefinitionCount, source.SourceRegionCount + sourceRegionDelta,
                source.SourceCellCount, source.AssignmentCount, source.InternalUndirectedEdgeCount,
                source.AttachmentBoundaryClosedCount, baseOpen,
                source.ClosedCrossRegionAdjacencyCount, source.HorizontalThroughCount,
                source.UnsupportedRequiredMaskCount, source.RngDrawCount, source.SourceMutationCount);
            var constructor = typeof(Type0RouteMaskAssignmentResult)
                .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
            return (Type0RouteMaskAssignmentResult)constructor.Invoke(new object[]
            {
                Type0RouteMaskAssignmentStatus.Completed, type0.SourceSnapshot, type0.RegisteredMasks,
                type0.Assignments, diagnostics, Array.Empty<Type0RouteMaskAssignmentError>(),
                type0.SourceGrowthDigest, type0.SourceRouteMaskCatalogDigest, type0.CanonicalDigest
            });
        }

        private static void AssertAtomicFailure(
            OptionalRewardTierResult result,
            OptionalRewardTierCalculationStatus status)
        {
            Assert.That(result.Status, Is.EqualTo(status));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Assignments, Is.Empty);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Errors, Is.Not.Empty);
            Assert.That(result.Diagnostics.TierAssignmentCount, Is.Zero);
            Assert.That(result.Diagnostics.MandatoryRewardSelectionCount, Is.Zero);
            Assert.That(result.Diagnostics.RngDrawCount, Is.Zero);
            Assert.That(result.Diagnostics.SourceMutationCount, Is.Zero);
        }

        private string SourceSignature()
        {
            return type0.CanonicalDigest + "|" + type0.SourceGrowthDigest + "|" +
                   access.CanonicalDigest + "|" +
                   string.Join(",", type0.Assignments.Select(value =>
                       value.RegionId.Value + ":" + value.SectorIndex + ":" + value.MaskId.Value)) + "|" +
                   string.Join(",", access.Assignments.Select(value =>
                       value.RegionId.Value + ":" + value.AccessRule + ":" + value.ToolCostTier + ":" +
                       value.ExplosiveFuelCost + ":" + value.HiddenClueDifficulty)) + "|" +
                   string.Join(",", type0.SourceSnapshot.Regions.Select(value =>
                       value.RegionId.Value + ":" + value.AccessRule + ":" + value.RewardTier + ":" + value.ReturnPolicy)) + "|" +
                   graph.NodeCount + "/" + graph.DirectedEdgeCount + "/" + graph.UndirectedEdgeCount + "/" + graph.CellCount;
        }

        private static string AssignmentSignature(OptionalRewardTierAssignment value)
        {
            return value.RegionId.Value + "|" + value.RegionOrdinal + "|" + value.RewardScore + "|" +
                   value.RewardTier + "|" + value.DepthScore + "|" + value.ToolCostScore + "|" +
                   value.ExplosiveFuelScore + "|" + value.HiddenClueScore;
        }

        private static bool IsLowerHex(char value)
        {
            return (value >= '0' && value <= '9') || (value >= 'a' && value <= 'f');
        }

        private static string FormatErrors(OptionalRewardTierResult result)
        {
            return string.Join("\n", result.Errors.Select(value => value.Code + ": " + value.Message));
        }

        private static T GetField<T>(object target, string name)
        {
            return (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }
    }
}
