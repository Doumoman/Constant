using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    [Category("MAP06_09")]
    public sealed class OptionalRegionValidatorTests
    {
        private GeneratedWorldData world;
        private SiteReservationSnapshot site;
        private BiomePatchValidationPublication biome;
        private MandatoryRouteGraph graph;
        private MandatoryRouteValidationReport mandatoryValidation;
        private OptionalRegionSnapshot regions;
        private Type0RouteMaskAssignmentResult type0;
        private OptionalAccessAssignmentResult access;
        private OptionalRewardTierResult reward;
        private OptionalReturnPolicyResult returns;
        private InactiveBufferAssignmentResult inactive;
        private OptionalRegionValidationReport baseline;
        private string sourceSignature;

        public static IEnumerable<int> ContractCases => Enumerable.Range(0, 36);
        public static IEnumerable<int> SourceCases => Enumerable.Range(0, 40);
        public static IEnumerable<int> RegionCases => Enumerable.Range(0, 38);
        public static IEnumerable<int> Type0Cases => Enumerable.Range(0, 36);
        public static IEnumerable<int> AccessRewardCases => Enumerable.Range(0, 38);
        public static IEnumerable<int> ReturnCases => Enumerable.Range(0, 36);
        public static IEnumerable<int> InactiveCases => Enumerable.Range(0, 36);
        public static IEnumerable<int> DeterminismCases => Enumerable.Range(0, 60);

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var fixture = new OptionalReturnPolicyResolverTests();
            fixture.OneTimeSetUp();
            type0 = GetField<Type0RouteMaskAssignmentResult>(fixture, "type0");
            access = GetField<OptionalAccessAssignmentResult>(fixture, "access");
            reward = GetField<OptionalRewardTierResult>(fixture, "reward");
            returns = GetField<OptionalReturnPolicyResult>(fixture, "baseline");
            graph = GetField<MandatoryRouteGraph>(fixture, "graph");
            regions = type0.SourceSnapshot;
            world = graph.RouteStampedWorld;
            site = graph.SourceTerminalSet.SourceSiteSnapshot;
            biome = graph.SourceTerminalSet.SourceBiomePublication;
            var validation = new MandatoryRouteGraphValidator().Validate(graph);
            Assert.That(validation.Succeeded, Is.True);
            mandatoryValidation = validation.Report;
            inactive = new InactiveBufferAssigner().Assign(
                world, site, biome, graph, mandatoryValidation, type0, returns,
                regions.SourceMandatoryGraphDigest,
                new InactiveBufferAssignmentSettings(true, true, true));
            Assert.That(inactive.IsSuccess, Is.True);
            sourceSignature = SourceSignature();
            baseline = Validate();
            Assert.That(baseline.IsValid, Is.True, FormatIssues(baseline));
        }

        [TestCaseSource(nameof(ContractCases))]
        public void EnumSettingsReportAndCollectionsAreImmutable(int caseId)
        {
            Assert.That(Enum.GetValues(typeof(OptionalRegionValidationStatus)), Has.Length.EqualTo(7));
            Assert.That(Enum.GetValues(typeof(OptionalRegionValidationIssueCode)), Has.Length.EqualTo(27));
            Assert.That(typeof(OptionalRegionValidationSettings).GetProperties().All(value => !value.CanWrite), Is.True);
            Assert.That(typeof(OptionalRegionValidationIssue).GetProperties().All(value => !value.CanWrite), Is.True);
            Assert.That(typeof(OptionalRegionValidationDiagnostics).GetProperties().All(value => !value.CanWrite), Is.True);
            Assert.That(typeof(OptionalRegionValidationReport).GetProperties().All(value => !value.CanWrite), Is.True);
            Assert.That(() => ((IList<OptionalRegionValidationIssue>)baseline.Issues)
                .Add(new OptionalRegionValidationIssue(OptionalRegionValidationIssueCode.SourceMismatch,
                    default(OptionalRegionId), -1, "Test", "Field", "Message.")),
                Throws.TypeOf<NotSupportedException>());
            Assert.That(ApprovedSettings().GetType().IsSealed, Is.True);
            Assert.That(caseId, Is.InRange(0, 35));
        }

        [TestCaseSource(nameof(SourceCases))]
        public void SourceStatusIdentityAndDigestChainAreExact(int caseId)
        {
            if (caseId < 12)
            {
                var report = ValidateWithNull(caseId);
                Assert.That(report.Status, Is.EqualTo(OptionalRegionValidationStatus.InvalidInput));
                Assert.That(report.IsValid, Is.False);
                Assert.That(report.CanonicalDigest, Is.Empty);
                Assert.That(report.Issues.Any(value => value.Code == OptionalRegionValidationIssueCode.NullInput), Is.True);
                Assert.That(report.RngDrawCount, Is.Zero);
                return;
            }
            if (caseId < 21)
            {
                var report = ValidateWithDisabledSetting(caseId - 12);
                Assert.That(report.Status, Is.EqualTo(OptionalRegionValidationStatus.InvalidSettings));
                Assert.That(report.IsValid, Is.False);
                Assert.That(report.CanonicalDigest, Is.Empty);
                Assert.That(report.Diagnostics.SourceMutationCount, Is.Zero);
                return;
            }

            Assert.That(type0.Status, Is.EqualTo(Type0RouteMaskAssignmentStatus.Completed));
            Assert.That(access.Status, Is.EqualTo(OptionalAccessAssignmentStatus.Completed));
            Assert.That(reward.Status, Is.EqualTo(OptionalRewardTierCalculationStatus.Completed));
            Assert.That(returns.Status, Is.EqualTo(OptionalReturnPolicyResolutionStatus.Completed));
            Assert.That(inactive.Status, Is.EqualTo(InactiveBufferAssignmentStatus.Completed));
            Assert.That(baseline.SourceMandatoryGraphDigest, Is.EqualTo("MAP05_GRAPH_47_96_48_47"));
            Assert.That(baseline.SourceGrowthDigest, Is.EqualTo("1f00f718bdb8d79fbf88923be0d81e0075987267c024cc624086ee6194998caa"));
            Assert.That(baseline.SourceType0AssignmentDigest, Is.EqualTo("a26e73f25ff7267760a2507ec55b1acda3a6c39a8f3698fc37b190620ceca525"));
            Assert.That(baseline.SourceAccessAssignmentDigest, Is.EqualTo("5268b7ed2342f197fb5717c85dbfdb8e51b3c520c217ecf93ad14dc944ecf74f"));
            Assert.That(baseline.SourceRewardTierDigest, Is.EqualTo("c3430c42a27937e143fa89c5839282b9533b62d5fb74fb26fdad490cb545958e"));
            Assert.That(baseline.SourceReturnPolicyDigest, Is.EqualTo("cff0556a59e66fcc16b886ecf3082779efe9535bb79dcf45b401d12ff0971f6b"));
            Assert.That(baseline.SourceInactiveAssignmentDigest, Is.EqualTo("426f269e39d8a2d75a93020a00c7bb617612c00dd60a663fdbeffc60f8ea9578"));
        }

        [TestCaseSource(nameof(RegionCases))]
        public void OptionalRegionIdentityIsOneToOneAcrossEveryArtifact(int caseId)
        {
            Assert.That(regions.Regions, Has.Count.EqualTo(12));
            Assert.That(regions.Cells, Has.Count.EqualTo(39));
            Assert.That(regions.Regions.Select(value => value.RegionId).Distinct().Count(), Is.EqualTo(12));
            Assert.That(regions.Cells.Select(value => value.SectorIndex).Distinct().Count(), Is.EqualTo(39));
            var region = regions.Regions[caseId % regions.Regions.Count];
            Assert.That(type0.Assignments.Count(value => value.RegionId == region.RegionId),
                Is.EqualTo(region.Cells.Count));
            Assert.That(access.Assignments.Count(value => value.RegionId == region.RegionId), Is.EqualTo(1));
            Assert.That(reward.Assignments.Count(value => value.RegionId == region.RegionId), Is.EqualTo(1));
            Assert.That(returns.Assignments.Count(value => value.RegionId == region.RegionId), Is.EqualTo(1));
            Assert.That(region.Cells.All(value => type0.Assignments.Any(mask =>
                mask.RegionId == region.RegionId && mask.SectorIndex == value.SectorIndex)), Is.True);
        }

        [TestCaseSource(nameof(Type0Cases))]
        public void Type0MasksPreserveClosedAttachmentAndNeverOpenLeftAndRight(int caseId)
        {
            var assignment = type0.Assignments[caseId % type0.Assignments.Count];
            Assert.That(assignment.OpenMask.OpenLeft && assignment.OpenMask.OpenRight, Is.False);
            Assert.That(type0.Diagnostics.SourceRegionCount, Is.EqualTo(12));
            Assert.That(type0.Diagnostics.SourceCellCount, Is.EqualTo(39));
            Assert.That(type0.Diagnostics.AssignmentCount, Is.EqualTo(39));
            Assert.That(type0.Diagnostics.AttachmentBoundaryClosedCount, Is.EqualTo(12));
            Assert.That(type0.Diagnostics.MandatoryBoundaryBaseOpenCount, Is.Zero);
            Assert.That(type0.Diagnostics.HorizontalThroughCount, Is.Zero);
            Assert.That(baseline.Diagnostics.Type0LeftRightOpenCount, Is.Zero);
        }

        [TestCaseSource(nameof(AccessRewardCases))]
        public void AccessClueAndRewardAssignmentsPreserveApprovedRules(int caseId)
        {
            var assignment = access.Assignments[caseId % access.Assignments.Count];
            Assert.That(assignment.Clue, Is.Not.Null);
            Assert.That(access.Clues.Contains(assignment.Clue), Is.True);
            Assert.That(access.Assignments, Has.Count.EqualTo(12));
            Assert.That(access.Clues, Has.Count.EqualTo(12));
            Assert.That(access.Diagnostics.PerceptibleClueCount, Is.EqualTo(12));
            Assert.That(baseline.Diagnostics.VisibleClueCount, Is.EqualTo(12));
            Assert.That(reward.Assignments, Has.Count.EqualTo(12));
            Assert.That(new[]
            {
                reward.Diagnostics.LowCount, reward.Diagnostics.MediumCount,
                reward.Diagnostics.HighCount, reward.Diagnostics.UniqueCount
            }, Is.EqualTo(new[] { 5, 1, 2, 4 }));
            Assert.That(reward.Diagnostics.MandatoryRewardSelectionCount, Is.Zero);
            Assert.That(baseline.Diagnostics.MandatoryRewardAssignmentCount, Is.Zero);
        }

        [TestCaseSource(nameof(ReturnCases))]
        public void EveryOptionalCellHasOneBacktrackReturnPolicy(int caseId)
        {
            var assignment = returns.Assignments[caseId % returns.Assignments.Count];
            var region = regions.Regions.Single(value => value.RegionId == assignment.RegionId);
            Assert.That(assignment.ReturnPolicy, Is.EqualTo(OptionalReturnPolicy.BacktrackToAttachment));
            Assert.That(assignment.ReturnableCellCount, Is.EqualTo(region.Cells.Count));
            Assert.That(assignment.UsesSameOpenedAttachmentBoundary, Is.True);
            Assert.That(assignment.RequiresReturnDevice, Is.False);
            Assert.That(returns.Diagnostics.AssignmentCount, Is.EqualTo(12));
            Assert.That(new[]
            {
                returns.Diagnostics.BacktrackCount, returns.Diagnostics.ReturnGateCount,
                returns.Diagnostics.SafeExitCount
            }, Is.EqualTo(new[] { 12, 0, 0 }));
            Assert.That(returns.Diagnostics.ReturnableCellCount, Is.EqualTo(39));
            Assert.That(returns.Diagnostics.NonReturnableCellCount, Is.Zero);
            Assert.That(baseline.Diagnostics.MissingReturnPolicyCount, Is.Zero);
        }

        [TestCaseSource(nameof(InactiveCases))]
        public void InactiveBuffersAndApprovedReservedAdaptersAreFullyAccounted(int caseId)
        {
            var assignment = inactive.Assignments[caseId % inactive.Assignments.Count];
            Assert.That(inactive.Assignments, Has.Count.EqualTo(78));
            Assert.That(new[]
            {
                inactive.Diagnostics.DecorativeBoundaryCount,
                inactive.Diagnostics.InteriorInactiveCount
            }, Is.EqualTo(new[] { 52, 26 }));
            Assert.That(inactive.Diagnostics.ProtectedUnionCount, Is.EqualTo(91));
            Assert.That(inactive.Diagnostics.ApprovedReservedAdapterOverlapCount, Is.EqualTo(3));
            var approvedOverlap = site.Sectors.Where(value => value.IsReserved).Select(value => value.Index)
                .Intersect(graph.Cells.Select(value => value.SectorIndex)).OrderBy(value => value).ToArray();
            Assert.That(approvedOverlap, Is.EqualTo(new[] { 0, 28, 106 }));
            Assert.That(approvedOverlap.All(index => graph.Cells.Single(value =>
                value.SectorIndex == index).IsApprovedReservedAdapter), Is.True);
            Assert.That(inactive.Diagnostics.OpenEdgeToInactiveCount, Is.Zero);
            Assert.That(inactive.Diagnostics.ProtectedUnionCount + inactive.Assignments.Count, Is.EqualTo(169));
            Assert.That(assignment.ProtectedNeighborSectorIndices.Intersect(
                assignment.InactiveNeighborSectorIndices), Is.Empty);
            Assert.That(baseline.Diagnostics.InactiveBufferAssignmentCount, Is.EqualTo(78));
        }

        [TestCaseSource(nameof(DeterminismCases))]
        public void DigestIssueOrderingMutationAndPhaseBoundaryAreDeterministic(int caseId)
        {
            var cultures = new[] { "en-US", "ko-KR", "tr-TR", "de-DE", "fr-FR" };
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUi = CultureInfo.CurrentUICulture;
            try
            {
                var culture = CultureInfo.GetCultureInfo(cultures[caseId % cultures.Length]);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                var service = new OptionalRegionValidator();
                var first = service.Validate(world, site, biome, graph, mandatoryValidation, regions,
                    type0, access, reward, returns, inactive, ApprovedSettings());
                var second = service.Validate(world, site, biome, graph, mandatoryValidation, regions,
                    type0, access, reward, returns, inactive, ApprovedSettings());
                Assert.That(first.CanonicalDigest, Is.EqualTo(baseline.CanonicalDigest));
                Assert.That(second.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));
                Assert.That(first.CanonicalDigest, Has.Length.EqualTo(64));
                Assert.That(first.CanonicalDigest.All(IsLowerHex), Is.True);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUi;
            }

            var invalid = ValidateWithDisabledSetting(caseId % 9);
            Assert.That(invalid.Issues, Is.Ordered.By("Code").Then.By("RegionId").Then.By("SectorIndex")
                .Then.By("Source").Then.By("Field").Then.By("Message"));
            Assert.That(invalid.CanonicalDigest, Is.Empty);
            Assert.That(SourceSignature(), Is.EqualTo(sourceSignature));
            Assert.That(baseline.RngDrawCount, Is.Zero);
            Assert.That(baseline.Diagnostics.SourceMutationCount, Is.Zero);

            var runtime = typeof(OptionalRegionValidator).Assembly;
            foreach (var name in new[]
                     {
                         "OptionalRegionValidationStatus", "OptionalRegionValidationIssueCode",
                         "OptionalRegionValidationSettings", "OptionalRegionValidationIssue",
                         "OptionalRegionValidationDiagnostics", "OptionalRegionValidationReport",
                         "OptionalRegionValidator"
                     })
                Assert.That(runtime.GetType("StarNight.Map.WorldGeneration.Generation." + name, false), Is.Not.Null);
            foreach (var name in new[]
                     {
                         "OptionalRegionOverlay", "GeneratedOptionalRegionCsvWriter",
                         "OptionalRegionOverlayRenderer", "OptionalRegionValidationOverlayWindow"
                     })
                Assert.That(runtime.GetType("StarNight.Map.WorldGeneration.Generation." + name, false), Is.Null);
            Assert.That(typeof(OptionalRegionValidatorTests).Name, Is.EqualTo("OptionalRegionValidatorTests"));
        }

        [Test]
        public void ApprovedFixturePublishesCanonicalOptionalValidationSummary()
        {
            var d = baseline.Diagnostics;
            TestContext.WriteLine(
                "MAP06_09_SUMMARY status={0} world={1} mandatory={2} regions={3} type0={4} access={5} clues={6} reward={7} mandatoryReward={8} returns={9} returnable={10} nonReturnable={11} inactive={12} decorative={13} interior={14} protected={15} adapters={16} openInactive={17} lr={18} missingClue={19} missingReturn={20} issues={21} rng={22} mutation={23} mandatoryDigest={24} growth={25} type0Digest={26} accessDigest={27} rewardDigest={28} returnDigest={29} inactiveDigest={30} canonical={31}",
                baseline.Status, d.WorldSectorCount, d.MandatoryRouteCellCount, d.OptionalRegionCount,
                d.Type0CellCount, d.AccessAssignmentCount, d.VisibleClueCount, d.RewardAssignmentCount,
                d.MandatoryRewardAssignmentCount, d.ReturnAssignmentCount, d.ReturnableCellCount,
                d.NonReturnableCellCount, d.InactiveBufferAssignmentCount, d.DecorativeBoundaryCount,
                d.InteriorInactiveCount, d.ProtectedUnionCount, d.ApprovedReservedAdapterOverlapCount,
                d.OpenEdgeToInactiveCount, d.Type0LeftRightOpenCount, d.MissingClueCount,
                d.MissingReturnPolicyCount, d.IssueCount, d.RngDrawCount, d.SourceMutationCount,
                baseline.SourceMandatoryGraphDigest, baseline.SourceGrowthDigest,
                baseline.SourceType0AssignmentDigest, baseline.SourceAccessAssignmentDigest,
                baseline.SourceRewardTierDigest, baseline.SourceReturnPolicyDigest,
                baseline.SourceInactiveAssignmentDigest, baseline.CanonicalDigest);
            Assert.That(baseline.IsValid, Is.True);
            Assert.That(new[]
            {
                d.WorldSectorCount, d.MandatoryRouteCellCount, d.OptionalRegionCount, d.Type0CellCount,
                d.AccessAssignmentCount, d.VisibleClueCount, d.RewardAssignmentCount,
                d.MandatoryRewardAssignmentCount, d.ReturnAssignmentCount, d.ReturnableCellCount,
                d.NonReturnableCellCount, d.InactiveBufferAssignmentCount, d.DecorativeBoundaryCount,
                d.InteriorInactiveCount, d.ProtectedUnionCount, d.ApprovedReservedAdapterOverlapCount,
                d.OpenEdgeToInactiveCount, d.Type0LeftRightOpenCount, d.MissingClueCount,
                d.MissingReturnPolicyCount, d.IssueCount, d.RngDrawCount, d.SourceMutationCount
            }, Is.EqualTo(new[]
            {
                169, 47, 12, 39, 12, 12, 12, 0, 12, 39, 0, 78, 52, 26, 91, 3,
                0, 0, 0, 0, 0, 0, 0
            }));
        }

        private OptionalRegionValidationReport Validate()
        {
            return new OptionalRegionValidator().Validate(world, site, biome, graph, mandatoryValidation,
                regions, type0, access, reward, returns, inactive, ApprovedSettings());
        }

        private OptionalRegionValidationReport ValidateWithNull(int index)
        {
            var arguments = new object[]
            {
                world, site, biome, graph, mandatoryValidation, regions,
                type0, access, reward, returns, inactive, ApprovedSettings()
            };
            arguments[index] = null;
            return (OptionalRegionValidationReport)typeof(OptionalRegionValidator)
                .GetMethod("Validate", BindingFlags.Instance | BindingFlags.Public)
                .Invoke(new OptionalRegionValidator(), arguments);
        }

        private OptionalRegionValidationReport ValidateWithDisabledSetting(int index)
        {
            var values = Enumerable.Repeat(true, 9).ToArray();
            values[index] = false;
            var settings = new OptionalRegionValidationSettings(values[0], values[1], values[2], values[3],
                values[4], values[5], values[6], values[7], values[8]);
            return new OptionalRegionValidator().Validate(world, site, biome, graph, mandatoryValidation,
                regions, type0, access, reward, returns, inactive, settings);
        }

        private static OptionalRegionValidationSettings ApprovedSettings()
        {
            return new OptionalRegionValidationSettings(true, true, true, true, true, true, true, true, true);
        }

        private string SourceSignature()
        {
            return world.Seed + "|" + site.Seed + "|" + graph.NodeCount + "|" + graph.DirectedEdgeCount + "|" +
                   type0.CanonicalDigest + "|" + access.CanonicalDigest + "|" + reward.CanonicalDigest + "|" +
                   returns.CanonicalDigest + "|" + inactive.CanonicalDigest + "|" +
                   string.Join(",", regions.Cells.Select(value => value.RegionId + ":" + value.SectorIndex + ":" + value.Depth)) + "|" +
                   string.Join(",", type0.Assignments.Select(value => value.SectorIndex + ":" + value.OpenMask));
        }

        private static string FormatIssues(OptionalRegionValidationReport report)
        {
            return string.Join("; ", report.Issues.Select(value => value.Code + ":" + value.RegionId + ":" +
                value.SectorIndex + ":" + value.Source + ":" + value.Field + ":" + value.Message));
        }

        private static bool IsLowerHex(char value)
        {
            return (value >= '0' && value <= '9') || (value >= 'a' && value <= 'f');
        }

        private static T GetField<T>(object target, string name)
        {
            return (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }
    }
}
