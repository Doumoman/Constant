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
    public sealed class MoonPalaceTuningProfileTests
    {
        private const string CategoryName = "MAP21_10";
        private const string PublisherTypeName = "StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.MoonPalaceTuningPublisher";

        [Test, Category(CategoryName)]
        public void MoonPalaceTuningPublishesExactGlobalDensityWindows()
        {
            var value = Profile(Sample());
            Assert.That(value.DensityWindows.Count, Is.EqualTo(4));
            AssertWindow(value, "DENSITY_QUIET_RATIO", "sector/window", .50, .55, .60);
            AssertWindow(value, "DENSITY_CLUSTER_RATIO", "sector/window", .25, .30, .35);
            AssertWindow(value, "DENSITY_ACTIVITY_RATIO", "world/window", .06, .09, .12);
            AssertWindow(value, "DENSITY_OVERLAY_RATIO", "world/window", .03, .05, .08);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceTuningPublishesFourBiomeTargetsInsideGlobalWindows()
        {
            var value = Profile(Sample());
            Assert.That(value.BiomeTargets.Count, Is.EqualTo(4));
            AssertTarget(value, "MoonCrater", .51, .34, .10, .06, "more active impact terrain");
            AssertTarget(value, "CassiaRoot", .56, .30, .08, .05, "calmer recovery space");
            AssertTarget(value, "AbandonedMill", .52, .33, .11, .07, "mechanical activity pressure");
            AssertTarget(value, "MoonDough", .58, .27, .07, .04, "soft quiet-heavy traversal");
            Assert.That(value.BiomeTargets.All(x => x.TargetKind == "Biome"), Is.True);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceTuningPublishesSevenPacingRolesWithoutChangingSolverBehavior()
        {
            var value = Profile(Sample());
            Assert.That(value.PacingRoleTargets.Count, Is.EqualTo(7));
            Assert.That(value.PacingRoleTargets.Select(x => x.TargetId).OrderBy(x => x),
                Is.EqualTo(MoonPalaceTuningProfile.RequiredRoleIds.OrderBy(x => x)));
            Assert.That(value.PacingRoleTargets.All(x => x.TargetKind == "PacingRole" && !x.ChangesSolverBehavior), Is.True);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceTuningReferencesSourceInventoryWithoutRegeneratingContent()
        {
            var value = Profile(Sample());
            Assert.That(value.SourceInventory.Count, Is.EqualTo(9));
            Assert.That(value.SourceInventory.All(x => x.ReadOnlySource && !x.Regenerated), Is.True);
            Assert.That(value.SourceInventory.Single(x => x.TaskId.StartsWith("MAP21_02")).PrimaryRecordCount, Is.EqualTo(24));
            Assert.That(value.SourceInventory.Single(x => x.TaskId.StartsWith("MAP21_04")).PrimaryRecordCount, Is.EqualTo(48));
            Assert.That(value.SourceInventory.Single(x => x.TaskId.StartsWith("MAP21_05")).SecondaryRecordCount, Is.EqualTo(5));
            Assert.That(value.SourceInventory.Single(x => x.TaskId.StartsWith("MAP21_06")).SecondaryRecordCount, Is.EqualTo(96));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceTuningKeepsSpecialRegionsAndBoundariesOutOfFalseDensityCounts()
        {
            var value = Profile(Sample());
            Assert.That(value.SpecialRegionReservedCellsExcludedFromQuietCluster, Is.True);
            Assert.That(value.BoundaryCellsExcludedFromActivity, Is.True);
            Assert.That(value.DensityWindows.Where(x => x.WindowId.Contains("QUIET") || x.WindowId.Contains("CLUSTER"))
                .All(x => x.ExcludedPopulation == "SpecialRegion reserved cells"), Is.True);
            Assert.That(value.DensityWindows.Single(x => x.WindowId == "DENSITY_ACTIVITY_RATIO").ExcludedPopulation,
                Is.EqualTo("boundary cells"));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceTuningPublishesEightRepetitionDistanceRules()
        {
            var value = Profile(Sample());
            Assert.That(value.RepetitionDistanceRules.Count, Is.EqualTo(8));
            var expected = new Dictionary<string, int>
            {
                { "REPEAT_PATTERN_EXACT_ID", 3 }, { "REPEAT_PATTERN_MIRROR_FAMILY", 2 },
                { "REPEAT_CLUSTER_EXACT_ID", 6 }, { "REPEAT_CLUSTER_STRUCTURAL_SIGNATURE", 4 },
                { "REPEAT_CLUSTER_SILHOUETTE_SIGNATURE", 3 }, { "REPEAT_ACTIVITY_EXACT_ID", 8 },
                { "REPEAT_EVENT_NON_EMPTY_ID", 6 }, { "REPEAT_BOUNDARY_CANDIDATE_ID", 4 },
            };
            foreach (var pair in expected)
                Assert.That(value.RepetitionDistanceRules.Single(x => x.RuleId == pair.Key).MinimumSeparation, Is.EqualTo(pair.Value));
            Assert.That(value.RepetitionDistanceRules.All(x => x.EnforcementScope.StartsWith("per ", StringComparison.Ordinal)), Is.True);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceTuningSeparatesStructuralSignatureFromSilhouetteAndMaterialOnlyChanges()
        {
            var value = Profile(Sample());
            var structural = value.RepetitionDistanceRules.Single(x => x.RuleId == "REPEAT_CLUSTER_STRUCTURAL_SIGNATURE");
            var silhouette = value.RepetitionDistanceRules.Single(x => x.RuleId == "REPEAT_CLUSTER_SILHOUETTE_SIGNATURE");
            Assert.That(structural.IdentityField, Is.Not.EqualTo(silhouette.IdentityField));
            Assert.That(structural.MaterialColorAudioCountsAsStructural, Is.False);
            Assert.That(value.RepetitionDistanceRules.Single(x => x.RuleId == "REPEAT_PATTERN_MIRROR_FAMILY").MirrorVariantCountsInFamily, Is.True);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceTuningActivityAndOverlayWindowsRemainMarkerFrequencyOnly()
        {
            var value = Profile(Sample());
            Assert.That(value.DensityWindows.Single(x => x.WindowId == "DENSITY_ACTIVITY_RATIO").Scope, Is.EqualTo("world/window"));
            Assert.That(value.DensityWindows.Single(x => x.WindowId == "DENSITY_OVERLAY_RATIO").Meaning, Does.Contain("frequency"));
            Assert.That(value.CanGenerateOrApproveSeed || value.CanExecuteRuntimeSideEffects, Is.False);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceTuningPublisherReadsMap21SourcesWithoutRewriting()
        {
            var sample = Sample();
            var paths = SourcePaths(sample);
            Assert.That(paths.Count, Is.EqualTo(19));
            var before = paths.ToDictionary(x => x, FileSha, StringComparer.Ordinal);
            Sample(true);
            var after = paths.ToDictionary(x => x, FileSha, StringComparer.Ordinal);
            Assert.That(after, Is.EqualTo(before));
            var digest = Digest(sample);
            Assert.That(digest.ObservedSourceResultDigests.Count, Is.EqualTo(9));
            Assert.That(digest.SemanticSourceDigests.Count, Is.EqualTo(9));
            Assert.That(digest.ObservedSourceResultDigests.Single(x => x.Name.StartsWith("MAP21_09")).Digest,
                Is.EqualTo(MoonPalaceTuningPreconditions.StrictMap2109ResultDigest));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceTuningPublisherWritesOnlyMap21_10AuthoringAndGeneratedRoots()
        {
            var sample = InvokePublisher("PublishAuthoringAndSamples", ProjectRoot, true);
            var paths = WrittenPaths(sample);
            var authoring = Constant("AuthoringDirectoryRelativePath") + "/";
            var generated = Constant("GeneratedDirectoryRelativePath") + "/";
            Assert.That(paths.Count, Is.EqualTo(10));
            Assert.That(paths.Count(x => x.StartsWith(authoring, StringComparison.Ordinal)), Is.EqualTo(6));
            Assert.That(paths.Count(x => x.StartsWith(generated, StringComparison.Ordinal)), Is.EqualTo(4));
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
        public void MoonPalaceTuningDigestsAreDeterministicReverseOrderRepeatAndCultureStable()
        {
            var culture = CultureInfo.CurrentCulture; var ui = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR"); CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var leftSample = Sample(false); var left = Profile(leftSample);
                CultureInfo.CurrentCulture = new CultureInfo("ko-KR"); CultureInfo.CurrentUICulture = new CultureInfo("ko-KR");
                var rightSample = Sample(true); var right = Profile(rightSample);
                Assert.That(AllCsv(left), Is.EqualTo(AllCsv(right)));
                Assert.That(AllJson(left), Is.EqualTo(AllJson(right)));
                Assert.That(Digest(leftSample).Serialize(), Is.EqualTo(Digest(rightSample).Serialize()));
                Assert.That(Digest(leftSample).CanonicalDigest, Is.EqualTo(Digest(rightSample).CanonicalDigest));
            }
            finally { CultureInfo.CurrentCulture = culture; CultureInfo.CurrentUICulture = ui; }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceTuningRejectsBadWindowsDuplicateRolesMissingSourceAndRuntimeSideEffects()
        {
            var value = Profile(Sample());
            Assert.Throws<ArgumentOutOfRangeException>(() => new MoonPalaceDensityWindow(
                "BAD", "sector/window", .5, .7, .6, "bad", "none"));
            var duplicateRoles = value.PacingRoleTargets.Take(6).Concat(new[] { value.PacingRoleTargets[0] });
            Assert.Throws<ArgumentException>(() => Rebuild(value, roles: duplicateRoles));
            Assert.Throws<ArgumentException>(() => Rebuild(value, sources: value.SourceInventory.Take(8)));
            Assert.Throws<InvalidOperationException>(() => value.RequestGenerationOrSeedApproval());
            Assert.Throws<InvalidOperationException>(() => value.RequestRuntimeExecution());
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceTuningDoesNotRunGenerationRendererValidationReplayRollbackPlayModeOrLegacyRegression()
        {
            Assert.That(Counters(Sample()).AllZero, Is.True);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceTuningPublishesMap21_11HandoffOnlyAfterFocusedPass()
        {
            var blocked = Assert.Throws<TargetInvocationException>(() =>
                InvokePublisher("PublishAuthoringAndSamples", ProjectRoot, false));
            Assert.That(blocked.InnerException, Is.TypeOf<InvalidOperationException>());
            var sample = InvokePublisher("PublishAuthoringAndSamples", ProjectRoot, true);
            var profile = Profile(sample); var digest = Digest(sample);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(profile.Map2111HandoffDigest), Is.True);
            Assert.That(digest.CsvDigests.Count, Is.EqualTo(6));
            Assert.That(digest.JsonDigests.Count, Is.EqualTo(3));
            Assert.That(File.Exists(Resolve(Constant("GeneratedDirectoryRelativePath") + "/" + Constant("DigestManifestFileName"))), Is.True);
        }

        private static void AssertWindow(MoonPalaceTuningProfile value, string id, string scope,
            double minimum, double target, double maximum)
        {
            var row = value.DensityWindows.Single(x => x.WindowId == id);
            Assert.That(row.Scope, Is.EqualTo(scope)); Assert.That(row.Minimum, Is.EqualTo(minimum));
            Assert.That(row.Target, Is.EqualTo(target)); Assert.That(row.Maximum, Is.EqualTo(maximum));
        }
        private static void AssertTarget(MoonPalaceTuningProfile value, string id, double quiet,
            double cluster, double activity, double overlay, string note)
        {
            var row = value.BiomeTargets.Single(x => x.TargetId == id);
            Assert.That(row.QuietTarget, Is.EqualTo(quiet)); Assert.That(row.ClusterTarget, Is.EqualTo(cluster));
            Assert.That(row.ActivityTarget, Is.EqualTo(activity)); Assert.That(row.OverlayTarget, Is.EqualTo(overlay));
            Assert.That(row.PacingNote, Is.EqualTo(note));
        }
        private static MoonPalaceTuningProfile Rebuild(MoonPalaceTuningProfile value,
            IEnumerable<MoonPalaceTuningTarget> roles = null,
            IEnumerable<MoonPalaceTuningSourceInventory> sources = null) => new MoonPalaceTuningProfile(
                value.DensityWindows, value.BiomeTargets, roles ?? value.PacingRoleTargets,
                value.RepetitionDistanceRules, sources ?? value.SourceInventory, value.CreatedUtc);
        private static string AllCsv(MoonPalaceTuningProfile x) => string.Join("|", new[]
        { x.SerializeDensityWindowsCsv(), x.SerializeBiomeTargetsCsv(), x.SerializePacingRoleTargetsCsv(),
          x.SerializeRepetitionRulesCsv(), x.SerializeSourceInventoryCsv(), x.SerializeHandoffCsv() });
        private static string AllJson(MoonPalaceTuningProfile x) => string.Join("|", new[]
        { x.SerializeTuningProfileManifest(), x.SerializeDensityPacingManifest(), x.SerializeRepetitionPolicyManifest() });
        private static object Sample(bool reverse = false) => InvokePublisher("CreateReadOnlySample",
            ProjectRoot, "2026-09-07T00:00:00.0000000Z", reverse);
        private static MoonPalaceTuningProfile Profile(object sample) =>
            (MoonPalaceTuningProfile)Property(sample, "Profile");
        private static MoonPalaceTuningDigestManifest Digest(object sample) =>
            (MoonPalaceTuningDigestManifest)Property(sample, "DigestManifest");
        private static MoonPalaceTuningForbiddenOperationCounters Counters(object sample) =>
            (MoonPalaceTuningForbiddenOperationCounters)Property(sample, "Counters");
        private static IReadOnlyList<string> WrittenPaths(object sample) =>
            (IReadOnlyList<string>)Property(sample, "WrittenRelativePaths");
        private static IReadOnlyList<string> SourcePaths(object sample) =>
            (IReadOnlyList<string>)Property(sample, "SourceReadRelativePaths");
        private static object Property(object instance, string name) => instance.GetType().GetProperty(name).GetValue(instance);
        private static object InvokePublisher(string name, params object[] args) => PublisherType.GetMethod(name, BindingFlags.Public | BindingFlags.Static).Invoke(null, args);
        private static string Constant(string name) => (string)PublisherType.GetField(name).GetRawConstantValue();
        private static Type PublisherType => Assembly.Load("MapAuthoring.Editor").GetType(PublisherTypeName, true);
        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        private static string Resolve(string path) => Path.Combine(ProjectRoot, path.Replace('/', Path.DirectorySeparatorChar));
        private static string FileSha(string path) { using (var stream = File.OpenRead(Resolve(path))) using (var sha = SHA256.Create()) return string.Concat(sha.ComputeHash(stream).Select(x => x.ToString("x2", CultureInfo.InvariantCulture))); }
    }
}
