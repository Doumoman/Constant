using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.MoonPalace;
using UnityEngine;

namespace StarNight.Tests.EditMode.Map.WorldGeneration.MoonPalace
{
    public sealed class MoonPalaceVerticalSliceReleaseAuditTests
    {
        private const string CategoryName = "MAP21_12";
        private const string PublisherTypeName =
            "StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.MoonPalaceVerticalSliceReleaseAuditPublisher";
        private object sample;
        private IReadOnlyList<string> sourcePaths;
        private Dictionary<string, string> sourceBefore;
        private Dictionary<string, string> sourceAfter;

        [OneTimeSetUp]
        public void PublishReadOnlyReleaseAuditOnce()
        {
            sourcePaths = (IReadOnlyList<string>)InvokePublisher("GetSourceReadRelativePaths");
            sourceBefore = sourcePaths.ToDictionary(x => x, FileSha, StringComparer.Ordinal);
            sample = InvokePublisher("Publish", ProjectRoot);
            sourceAfter = sourcePaths.ToDictionary(x => x, FileSha, StringComparer.Ordinal);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceReleaseAuditBindsImmediateQaPassStrictly()
        {
            var source = Audit.Sources.Single(x => x.TaskId == "MAP21_11_LOCK_QA_SEEDS_AND_RUN_COMPLETION_PLAYTESTS");
            Assert.That(source.ResultDigest, Is.EqualTo(MoonPalaceReleaseAuditPreconditions.StrictMap2111ResultDigest));
            Assert.That(source.SemanticDigest, Is.EqualTo(MoonPalaceReleaseAuditPreconditions.Map2111QaDigest));
            Assert.That(Digest.SemanticSourceDigests.Single(x => x.Name == source.TaskId).Digest,
                Is.EqualTo(MoonPalaceReleaseAuditPreconditions.Map2111QaDigest));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceReleaseAuditAggregatesMap17RuntimeBakeStreamingSaveReadiness()
        {
            Assert.That(Audit.Sources.Single(x => x.TaskId.StartsWith("MAP17_08", StringComparison.Ordinal)).Status, Is.EqualTo("PASS"));
            var gate = Audit.Gates.Single(x => x.GateId == "GATE_01_RUNTIME_BAKE_STREAM_SAVE");
            Assert.That(gate.Passed, Is.True);
            Assert.That(gate.AllowedWarningCount, Is.EqualTo(2));
            Assert.That(Audit.Warnings.Count(x => x.SourceTaskId.StartsWith("MAP17_08", StringComparison.Ordinal)), Is.EqualTo(6));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceReleaseAuditAggregatesMap18PopulationSpecialStateReadiness()
        {
            Assert.That(Audit.Sources.Single(x => x.TaskId.StartsWith("MAP18_07", StringComparison.Ordinal)).SemanticDigest,
                Is.EqualTo(MoonPalaceReleaseAuditPreconditions.Map1807AuditDigest));
            Assert.That(Audit.Gates.Single(x => x.GateId == "GATE_02_POPULATION_SPECIAL_STATE").Passed, Is.True);
            Assert.That(Audit.Metrics.MandatoryCompletionFailureCount, Is.Zero);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceReleaseAuditAggregatesMap19ScaleAndFailureBundleReadiness()
        {
            Assert.That(Audit.Gates.Single(x => x.GateId == "GATE_03_VALIDATION_SCALE").Passed, Is.True);
            Assert.That(Audit.Gates.Single(x => x.GateId == "GATE_04_FAILURE_SCHEMA").Passed, Is.True);
            Assert.That(Audit.Sources.Single(x => x.TaskId.StartsWith("MAP19_08", StringComparison.Ordinal)).SemanticDigest,
                Is.EqualTo(MoonPalaceReleaseAuditPreconditions.Map1908FailureSchemaDigest));
            Assert.That(Audit.Sources.Single(x => x.TaskId.StartsWith("MAP19_09", StringComparison.Ordinal)).SemanticDigest,
                Is.EqualTo(MoonPalaceReleaseAuditPreconditions.Map1909ExitDigest));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceReleaseAuditAggregatesMap20ToolingReadinessWithoutExecution()
        {
            Assert.That(Audit.Gates.Single(x => x.GateId == "GATE_05_TOOLING_DEBUG").Passed, Is.True);
            Assert.That(Audit.Sources.Single(x => x.TaskId.StartsWith("MAP20_06", StringComparison.Ordinal)).SemanticDigest,
                Is.EqualTo(MoonPalaceReleaseAuditPreconditions.Map2006ExitDigest));
            Assert.That(Audit.Metrics.ValidationRunnerExecutionCountOutsideMap2112, Is.Zero);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceReleaseAuditAggregatesMap21ProductionContentAndTuning()
        {
            Assert.That(Audit.Sources.Count(x => x.TaskId.StartsWith("MAP21_", StringComparison.Ordinal)), Is.EqualTo(11));
            Assert.That(Audit.Gates.Single(x => x.GateId == "GATE_06_PRODUCTION_CONTENT").Passed, Is.True);
            Assert.That(Audit.Sources.Single(x => x.TaskId.StartsWith("MAP21_10", StringComparison.Ordinal)).SemanticDigest,
                Is.EqualTo(MoonPalaceReleaseAuditPreconditions.Map2110TuningDigest));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceReleaseAuditApprovesThirtyQaSeedsCompletionTelemetryAndZeroFailures()
        {
            Assert.That(Audit.Metrics.QaSeedRecords, Is.EqualTo(30));
            Assert.That(Audit.Metrics.CompletionPassCount, Is.EqualTo(30));
            Assert.That(Audit.Metrics.CompletionFailCount, Is.Zero);
            Assert.That(Audit.Metrics.DensityViolationCount, Is.Zero);
            Assert.That(Audit.Metrics.RepetitionViolationCount, Is.Zero);
            Assert.That(Audit.Metrics.DeathTotal + Audit.Metrics.SoftlockTotal + Audit.Metrics.BadSeamTotal, Is.Zero);
            Assert.That(Audit.Gates.Single(x => x.GateId == "GATE_07_QA_COMPLETION").Passed, Is.True);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceReleaseAuditClassifiesAllowedWarningsAndRejectsBlocksFails()
        {
            Assert.That(Audit.Metrics.AllowedWarningCount, Is.EqualTo(5));
            Assert.That(Audit.Metrics.DeferredItemCount, Is.EqualTo(6));
            Assert.That(Audit.Warnings.All(x => x.Allowed && !x.BlocksRelease), Is.True);
            Assert.That(Audit.Metrics.DisallowedWarningCount, Is.Zero);
            var gates = Audit.Gates.ToArray();
            gates[0] = new ReleaseGateRecord("GATE_01_RUNTIME_BAKE_STREAM_SAVE", "runtime", "negative probe", "FAIL", 1, 1, 0);
            var rejected = ReleaseAuditVerdict.Evaluate(Audit.Sources, gates, Audit.Warnings, Audit.Metrics);
            Assert.That(rejected.Passed, Is.False);
            Assert.That(rejected.Status, Is.EqualTo("BLOCKED"));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceReleaseAuditPublisherReadsSourcesWithoutRewriting()
        {
            Assert.That(sourcePaths.Count, Is.EqualTo(26));
            Assert.That(sourceAfter, Is.EqualTo(sourceBefore));
            Assert.That(Audit.Sources.Count, Is.EqualTo(16));
            Assert.That(Audit.Sources.All(x => x.ReadOnlySource), Is.True);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceReleaseAuditPublisherWritesOnlyMap21_12Roots()
        {
            Assert.That(OutputContents.Count, Is.EqualTo(8));
            Assert.That(OutputContents.Keys.Count(x => x.StartsWith(Constant("AuthoringDirectoryRelativePath") + "/", StringComparison.Ordinal)), Is.EqualTo(5));
            Assert.That(OutputContents.Keys.Count(x => x.StartsWith(Constant("GeneratedDirectoryRelativePath") + "/", StringComparison.Ordinal)), Is.EqualTo(3));
            foreach (var output in OutputContents)
            {
                var bytes = File.ReadAllBytes(Resolve(output.Key));
                Assert.That(bytes.Take(3), Is.Not.EqualTo(new byte[] { 0xef, 0xbb, 0xbf }));
                var text = BakingCanonicalDigest.Utf8NoBomEncoding.GetString(bytes);
                Assert.That(text, Is.EqualTo(output.Value));
                Assert.That(text.Contains("\r"), Is.False);
                Assert.That(text.EndsWith("\n", StringComparison.Ordinal), Is.True);
                Assert.That(text.EndsWith("\n\n", StringComparison.Ordinal), Is.False);
            }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceReleaseAuditDigestsAreDeterministicReverseOrderRepeatAndCultureStable()
        {
            var culture = CultureInfo.CurrentCulture;
            var uiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var reverse = InvokePublisher("BuildSnapshot", ProjectRoot, Digest.CreatedUtc, true);
                var reverseAudit = (MoonPalaceVerticalSliceReleaseAudit)Property(reverse, "Audit");
                var reverseDigest = (ReleaseDigestManifest)Property(reverse, "DigestManifest");
                var reverseOutputs = (IReadOnlyDictionary<string, string>)Property(reverse, "OutputContents");
                Assert.That(reverseAudit.CanonicalDigest, Is.EqualTo(Audit.CanonicalDigest));
                Assert.That(reverseDigest.CanonicalDigest, Is.EqualTo(Digest.CanonicalDigest));
                Assert.That(reverseOutputs.Keys, Is.EqualTo(OutputContents.Keys));
                foreach (var key in OutputContents.Keys) Assert.That(reverseOutputs[key], Is.EqualTo(OutputContents[key]));
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = uiCulture;
            }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceReleaseAuditDoesNotRunLegacyRegressionSeedReplayPlayModeBuildOrFullRegression()
        {
            var metrics = Audit.Metrics;
            Assert.That(metrics.ForbiddenExecutionCountsAreZero, Is.True);
            Assert.That(metrics.GeneratorExecutionCount + metrics.RendererExecutionCount + metrics.ReplayExecutionCount +
                        metrics.RollbackExecutionCount + metrics.QaSeedRerunCount + metrics.CompletionPlaytestRerunCount, Is.Zero);
            Assert.That(metrics.PlayModeSelectionCount + metrics.Legacy19347SelectionCount +
                        metrics.PriorCategorySelectionCount + metrics.UnfilteredSelectionCount +
                        metrics.FullRegressionRunCount + metrics.PlayerBuildExecutionCount, Is.Zero);
            Assert.That(metrics.BuildReadinessEvidenceRecorded, Is.True);
            Assert.That(Audit.Gates.Single(x => x.GateId == "GATE_08_RELEASE_BOUNDARY").Passed, Is.True);
            Assert.That(Audit.Verdict.Status, Is.EqualTo("PASS"));
        }

        private MoonPalaceVerticalSliceReleaseAudit Audit =>
            (MoonPalaceVerticalSliceReleaseAudit)Property(sample, "Audit");
        private ReleaseDigestManifest Digest => (ReleaseDigestManifest)Property(sample, "DigestManifest");
        private IReadOnlyDictionary<string, string> OutputContents =>
            (IReadOnlyDictionary<string, string>)Property(sample, "OutputContents");
        private static object Property(object instance, string name) =>
            instance.GetType().GetProperty(name).GetValue(instance);
        private static object InvokePublisher(string name, params object[] args) =>
            PublisherType.GetMethod(name, BindingFlags.Public | BindingFlags.Static).Invoke(null, args);
        private static string Constant(string name) => (string)PublisherType.GetField(name).GetRawConstantValue();
        private static Type PublisherType => Assembly.Load("MapAuthoring.Editor").GetType(PublisherTypeName, true);
        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        private static string Resolve(string relative) =>
            Path.Combine(ProjectRoot, relative.Replace('/', Path.DirectorySeparatorChar));
        private static string FileSha(string relative)
        {
            using (var stream = File.OpenRead(Resolve(relative)))
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(stream).Select(x => x.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }
}
