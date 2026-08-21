#if LEGACY_DISABLED
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.MapAuthoring.Editor;
using StarNight.Stage.Layout;
using UnityEditor;

namespace StarNight.MapAuthoring.Tests
{
    public sealed class MapE11FiveHundredSeedValidationTests
    {
        [Test]
        public void FiveHundredSeedsHaveNoOuterFloorPortalOrRouteFailures()
        {
            StageMapProfile profile = StageMapProfileSampleFactory.EnsureSample();
            IReadOnlyList<RoomTemplate> templates = RoomTemplateSampleFactory.EnsureSamples();
            IReadOnlyList<int> seeds = StageSeedBatchValidator.CreateApprovalSeedSet(10801);
            Assert.That(seeds.Count, Is.EqualTo(500));
            Assert.That(new HashSet<int>(seeds).Count, Is.EqualTo(500));

            StageSeedValidationReport report = StageSeedBatchValidator.RunApproval(
                profile,
                templates,
                10801,
                false);

            Assert.That(report.SeedCount, Is.EqualTo(500));
            Assert.That(report.FixedRegressionSeedCount, Is.EqualTo(10));
            Assert.That(report.RandomSeedCount, Is.EqualTo(490));
            Assert.That(report.PassedSeedCount, Is.EqualTo(500));
            Assert.That(report.FailedSeedCount, Is.Zero);
            Assert.That(report.OuterEscapeFailureCount, Is.Zero);
            Assert.That(report.FloorGapFailureCount, Is.Zero);
            Assert.That(report.PortalGapFailureCount, Is.Zero);
            Assert.That(report.MainRouteFailureCount, Is.Zero);
            Assert.That(report.MaruRouteFailureCount, Is.Zero);
            Assert.That(report.UniqueValidationHashCount, Is.EqualTo(500));
            Assert.That(report.FamilyCounts.Count, Is.EqualTo(5));
            Assert.That(report.Failures.Count, Is.LessThanOrEqualTo(20));
            Assert.That(AssetDatabase.LoadMainAssetAtPath(report.JsonReportPath), Is.Not.Null);
            Assert.That(AssetDatabase.LoadMainAssetAtPath(report.CsvReportPath), Is.Not.Null);
        }
    }
}

#endif
