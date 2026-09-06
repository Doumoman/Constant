using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.MoonPalace;
using UnityEngine;

namespace StarNight.Tests.EditMode.Map.WorldGeneration.MoonPalace
{
    public sealed class MoonPalaceQaSeedCompletionTests
    {
        private const string CategoryName = "MAP21_11";
        private const string PublisherTypeName = "StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.MoonPalaceQaSeedPublisher";
        private object sample;
        private IReadOnlyList<string> sourcePaths;
        private Dictionary<string, string> sourceBefore;
        private Dictionary<string, string> sourceAfter;

        [OneTimeSetUp]
        public void ExecuteExactlyThirtyFocusedQaSeedsOnce()
        {
            sourcePaths = (IReadOnlyList<string>)InvokePublisher("GetSourceReadRelativePaths");
            sourceBefore = sourcePaths.ToDictionary(x => x, FileSha, StringComparer.Ordinal);
            sample = InvokePublisher("ExecuteFocusedQaAndPublish", ProjectRoot, true);
            sourceAfter = sourcePaths.ToDictionary(x => x, FileSha, StringComparer.Ordinal);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceQaSeedSetPublishesExactThirtyDerivedSeeds()
        {
            var qa = Qa;
            Assert.That(qa.Seeds.Count, Is.EqualTo(30));
            Assert.That(qa.Seeds.Select(x => x.SeedId), Is.EqualTo(Enumerable.Range(1, 30).Select(x => "MP_QA_" + x.ToString("D2"))));
            Assert.That(qa.Seeds.Select(x => x.SeedValue), Is.EqualTo(MoonPalaceQaSeedSet.RequiredSeedValues));
            Assert.That(qa.Seeds.Select(x => x.SeedValue).Distinct().Count(), Is.EqualTo(30));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceQaSeedContentHashesAreStableUniqueAndLowerHex()
        {
            Assert.That(Qa.Seeds.Select(x => x.ContentHash).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(30));
            Assert.That(Qa.Seeds.All(x => BakingCanonicalDigest.IsLowerHexSha256(x.ContentHash)), Is.True);
            Assert.That(Qa.Seeds.All(x => x.ContentHash == MoonPalaceQaSeedRecord.Derive(x.ZeroBasedIndex).ContentHash), Is.True);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceCompletionScenariosReachAllMandatoryCheckpointsForThirtySeeds()
        {
            Assert.That(Qa.Telemetry.Count, Is.EqualTo(30));
            Assert.That(MoonPalaceQaSeedSet.RequiredCheckpoints.Length, Is.EqualTo(10));
            Assert.That(Qa.Telemetry.All(x => x.CompletionPass && x.MandatoryCheckpointCount == 10), Is.True);
            Assert.That(Qa.CompletionExecutedCount, Is.EqualTo(30));
            Assert.That(Qa.CompletionPassCount, Is.EqualTo(30));
            Assert.That(Qa.CompletionFailCount, Is.Zero);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceCompletionScenariosDoNotRequireVillageMerchantOrMaru()
        {
            Assert.That(Qa.Telemetry.Count(x => x.VillageRequiredForCompletion), Is.Zero);
            Assert.That(Qa.Telemetry.Count(x => x.MerchantRequiredForCompletion), Is.Zero);
            Assert.That(Qa.Telemetry.Count(x => x.MaruRequiredForCompletion), Is.Zero);
            Assert.That(Qa.OptionalContentBlockerCount, Is.Zero);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceCompletionTelemetryReportsTimeDistanceRevisitSeamAndDeathMetrics()
        {
            AssertRange(Qa.CompletionTimeRange); AssertRange(Qa.TotalDistanceRange);
            AssertRange(Qa.RevisitRange); AssertRange(Qa.SeamRange);
            Assert.That(Qa.Telemetry.All(x => x.TotalRouteDistanceTiles == x.MainRouteDistanceTiles + x.BranchRouteDistanceTiles), Is.True);
            Assert.That(Qa.Telemetry.All(x => x.SeamCrossingCount >= 8), Is.True);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceCompletionTelemetryHasNoDeathSoftlockBadSeamOrOptionalBlocker()
        {
            Assert.That(Qa.DeathCountTotal, Is.Zero); Assert.That(Qa.SoftlockCountTotal, Is.Zero);
            Assert.That(Qa.UnrecoverableFailureCount, Is.Zero); Assert.That(Qa.BadSeamCountTotal, Is.Zero);
            Assert.That(Qa.OptionalContentBlockerCount, Is.Zero); Assert.That(Qa.ContentHashMismatchCount, Is.Zero);
            Assert.That(Qa.ReplayMismatchCount, Is.Zero); Assert.That(Qa.HasFailure, Is.False);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceDensityMeasurementsStayInsideMap21_10Windows()
        {
            Assert.That(Qa.Telemetry.All(x => x.QuietRatio >= .50 && x.QuietRatio <= .60), Is.True);
            Assert.That(Qa.Telemetry.All(x => x.ClusterRatio >= .25 && x.ClusterRatio <= .35), Is.True);
            Assert.That(Qa.Telemetry.All(x => x.ActivityRatio >= .06 && x.ActivityRatio <= .12), Is.True);
            Assert.That(Qa.Telemetry.All(x => x.OverlayRatio >= .03 && x.OverlayRatio <= .08), Is.True);
            Assert.That(Qa.DensityWindowViolationCount, Is.Zero);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceRepetitionMeasurementsSatisfyMap21_10DistanceRules()
        {
            Assert.That(Qa.RepetitionMeasurements.Count, Is.EqualTo(8));
            Assert.That(Qa.RepetitionMeasurements.All(x => x.ObservedMinimumSeparation >= x.RequiredSeparation), Is.True);
            Assert.That(Qa.RepetitionMeasurements.Select(x => x.RequiredSeparation).OrderBy(x => x),
                Is.EqualTo(new[] { 2, 3, 3, 4, 4, 6, 6, 8 }));
            Assert.That(Qa.RepetitionRuleViolationCount, Is.Zero);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceFailureBundleUsesMap19SchemaAndPublishesOnlyOnFailure()
        {
            Assert.That(Qa.FailureBundleCount, Is.Zero);
            Assert.That(Qa.SerializeFailureIndexCsv(), Does.Contain("MAP19_FAILURE_BUNDLE_V1"));
            Assert.That(Qa.SerializeFailureIndexCsv(), Does.Contain(MoonPalaceQaPreconditions.SourceMap1908FailureSchemaDigest));
            Assert.That(Directory.Exists(Resolve(Constant("GeneratedDirectoryRelativePath") + "/failures")), Is.False);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceQaPublisherReadsSourcesWithoutRewriting()
        {
            Assert.That(sourcePaths.Count, Is.EqualTo(19));
            Assert.That(sourceAfter, Is.EqualTo(sourceBefore));
            var refreshed = (IReadOnlyList<MoonPalaceNamedDigest>)InvokePublisher("ReadSourceResultSnapshot", ProjectRoot);
            Assert.That(refreshed.Count, Is.EqualTo(13));
            Assert.That(refreshed.Single(x => x.Name.StartsWith("MAP21_10")).Digest,
                Is.EqualTo(MoonPalaceQaPreconditions.StrictMap2110ResultDigest));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceQaPublisherWritesOnlyMap21_11AuthoringAndGeneratedRoots()
        {
            var paths = WrittenPaths;
            var authoring = Constant("AuthoringDirectoryRelativePath") + "/";
            var generated = Constant("GeneratedDirectoryRelativePath") + "/";
            Assert.That(paths.Count, Is.EqualTo(11));
            Assert.That(paths.Count(x => x.StartsWith(authoring, StringComparison.Ordinal)), Is.EqualTo(5));
            Assert.That(paths.Count(x => x.StartsWith(generated, StringComparison.Ordinal)), Is.EqualTo(6));
            foreach (var path in paths)
            {
                var bytes = File.ReadAllBytes(Resolve(path));
                Assert.That(bytes.Take(3), Is.Not.EqualTo(new byte[] { 0xef, 0xbb, 0xbf }));
                var text = Encoding.UTF8.GetString(bytes);
                Assert.That(text.Contains("\r"), Is.False);
                Assert.That(text.EndsWith("\n", StringComparison.Ordinal), Is.True);
                Assert.That(text.EndsWith("\n\n", StringComparison.Ordinal), Is.False);
            }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceQaDigestsAreDeterministicReverseOrderRepeatAndCultureStable()
        {
            var culture = CultureInfo.CurrentCulture; var ui = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR"); CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var reverse = new MoonPalaceQaSeedSet(Qa.Seeds.Reverse(), Qa.Telemetry.Reverse(),
                    Qa.RepetitionMeasurements.Reverse(), Qa.CreatedUtc);
                Assert.That(AllCsv(reverse), Is.EqualTo(AllCsv(Qa)));
                Assert.That(AllJson(reverse), Is.EqualTo(AllJson(Qa)));
                var digest = new MoonPalaceQaDigestManifest(reverse, Digest.ObservedSourceResultDigests,
                    Digest.SemanticSourceDigests, Digest.CsvDigests, Digest.JsonDigests, Digest.CreatedUtc);
                Assert.That(digest.CanonicalDigest, Is.EqualTo(Digest.CanonicalDigest));
                Assert.That(digest.Serialize(), Is.EqualTo(Digest.Serialize()));
            }
            finally { CultureInfo.CurrentCulture = culture; CultureInfo.CurrentUICulture = ui; }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceQaRejectsSeedSubstitutionHashMismatchMissingCheckpointAndRuntimeSideEffects()
        {
            Assert.Throws<ArgumentException>(() => MoonPalaceQaSeedRecord.LockSuppliedValue(0, 1));
            var first = Qa.Seeds[0]; var wrongHash = Qa.Seeds[1].ContentHash;
            var mismatch = new MoonPalaceCompletionTelemetry(first, wrongHash);
            var missing = new MoonPalaceCompletionTelemetry(first, first.ContentHash, 9);
            Assert.That(mismatch.CompletionPass, Is.False); Assert.That(mismatch.ContentHashMismatchCount, Is.EqualTo(1));
            Assert.That(missing.CompletionPass, Is.False); Assert.That(missing.MandatoryCheckpointCount, Is.EqualTo(9));
            Assert.That(Qa.CanExecuteRuntimeSideEffects, Is.False);
            Assert.Throws<InvalidOperationException>(() => Qa.RequestRuntimeSideEffect());
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceQaDoesNotRunLegacyRegressionPriorCategoriesPlayModeUnfilteredOrFullRegression()
        {
            Assert.That(Counters.FocusedQaSeedRuns, Is.EqualTo(30));
            Assert.That(Counters.FocusedCompletionPlaytestRuns, Is.EqualTo(30));
            Assert.That(Counters.FocusedMetricEvaluations, Is.EqualTo(30));
            Assert.That(Counters.ForbiddenAllZero, Is.True);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceQaPublishesMap21_12HandoffOnlyAfterThirtyFocusedPasses()
        {
            var blocked = Assert.Throws<TargetInvocationException>(() =>
                InvokePublisher("ExecuteFocusedQaAndPublish", ProjectRoot, false));
            Assert.That(blocked.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(Qa.CompletionPassCount, Is.EqualTo(30)); Assert.That(Qa.FailureBundleCount, Is.Zero);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(Qa.Map2112HandoffDigest), Is.True);
            Assert.That(Digest.ContentHashes.Count, Is.EqualTo(30));
            Assert.That(Digest.CsvDigests.Count, Is.EqualTo(5)); Assert.That(Digest.JsonDigests.Count, Is.EqualTo(5));
        }

        private static void AssertRange(MoonPalaceMetricRange range)
        { Assert.That(range.Minimum, Is.LessThanOrEqualTo(range.Median)); Assert.That(range.Median, Is.LessThanOrEqualTo(range.Maximum)); }
        private static string AllCsv(MoonPalaceQaSeedSet x) => string.Join("|", new[]
        { x.SerializeSeedManifestCsv(), x.SerializeCompletionScenariosCsv(), x.SerializeTelemetryTargetsCsv(),
          x.SerializeFailureIndexCsv(), x.SerializeReleaseHandoffCsv() });
        private static string AllJson(MoonPalaceQaSeedSet x) => string.Join("|", new[]
        { x.SerializeSeedLockManifest(), x.SerializeCompletionManifest(), x.SerializeTelemetrySummary(),
          x.SerializeDensityRepetitionMeasurement(), x.SerializeFailureIndexManifest() });
        private MoonPalaceQaSeedSet Qa => (MoonPalaceQaSeedSet)Property(sample, "Qa");
        private MoonPalaceQaDigestManifest Digest => (MoonPalaceQaDigestManifest)Property(sample, "DigestManifest");
        private MoonPalaceQaExecutionCounters Counters => (MoonPalaceQaExecutionCounters)Property(sample, "Counters");
        private IReadOnlyList<string> WrittenPaths => (IReadOnlyList<string>)Property(sample, "WrittenRelativePaths");
        private static object Property(object instance, string name) => instance.GetType().GetProperty(name).GetValue(instance);
        private static object InvokePublisher(string name, params object[] args) => PublisherType.GetMethod(name, BindingFlags.Public | BindingFlags.Static).Invoke(null, args);
        private static string Constant(string name) => (string)PublisherType.GetField(name).GetRawConstantValue();
        private static Type PublisherType => Assembly.Load("MapAuthoring.Editor").GetType(PublisherTypeName, true);
        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        private static string Resolve(string path) => Path.Combine(ProjectRoot, path.Replace('/', Path.DirectorySeparatorChar));
        private static string FileSha(string path) { using (var stream = File.OpenRead(Resolve(path))) using (var sha = SHA256.Create()) return string.Concat(sha.ComputeHash(stream).Select(x => x.ToString("x2", CultureInfo.InvariantCulture))); }
    }
}
