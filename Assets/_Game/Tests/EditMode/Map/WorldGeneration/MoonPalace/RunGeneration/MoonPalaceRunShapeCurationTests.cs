using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.MoonPalace.RunGeneration;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarNight.Tests.EditMode.Map.WorldGeneration.MoonPalace.RunGeneration
{
    public sealed class MoonPalaceRunShapeCurationTests
    {
        private const string CategoryName = "RUN06";
        private const string BuilderTypeName = "StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.RunGeneration.MoonPalaceCuratedRunGallerySceneBuilder";
        private const string WindowTypeName = "StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.RunGeneration.MoonPalaceRunCurationWindow";
        private const string Run05ResultRelativePath = "MapDesign/MCP/REPORTS/RUN05_CREATE_SEED_REGENERATE_AND_VARIANT_SELECTION_TOOL_RESULT.md";
        private const string Run05ResultSha256 = "1ac68e5f06c5837f95ef572b5f65c62a59653cc87c20c23feeac6ffb827fe831";
        private MoonPalaceCuratedRunSet batch;
        private MoonPalaceCuratedRunSet repeat;
        private object publication;
        private string[] buildSettingsBefore;

        [OneTimeSetUp]
        public void GenerateBoundedCurationPublication()
        {
            buildSettingsBefore = EditorBuildSettings.scenes.Select(scene => scene.path).OrderBy(path => path, StringComparer.Ordinal).ToArray();
            batch = MoonPalaceRunCurationBatch.Build();
            repeat = MoonPalaceRunCurationBatch.Build();
            publication = InvokeBuilder("Publish", new[] { typeof(string), typeof(MoonPalaceCuratedRunSet) }, ProjectRoot, batch);
        }

        [Test, Category(CategoryName)]
        public void Run05PrerequisiteResultShaAndPassStatusAreRecognized()
        {
            var path = Resolve(Run05ResultRelativePath); Assert.That(File.Exists(path), Is.True); Assert.That(File.ReadAllText(path), Does.Contain("STATUS: PASS")); Assert.That(HashFile(path), Is.EqualTo(Run05ResultSha256));
        }

        [Test, Category(CategoryName)]
        public void QualityProfileContainsEveryRequiredRuleId()
        {
            Assert.That(MoonPalaceRunQualityRule.RequiredIds.Count, Is.EqualTo(10)); Assert.That(MoonPalaceRunQualityProfileCatalog.Profiles.Count, Is.EqualTo(3)); Assert.That(MoonPalaceRunQualityRule.RequiredIds.Distinct().Count(), Is.EqualTo(10)); Assert.That(MoonPalaceRunQualityProfileCatalog.CanonicalDigest, Is.Not.Empty);
        }

        [Test, Category(CategoryName)]
        public void EveryQualityFindingHasSeverityMeasurementThresholdAndReason()
        {
            Assert.That(batch.Records.All(record => record.Analysis.Findings.Count == 10 && record.Analysis.Findings.All(finding => MoonPalaceRunQualityRule.RequiredIds.Contains(finding.RuleId) && Enum.IsDefined(typeof(MoonPalaceRunQualitySeverity), finding.Severity) && !double.IsNaN(finding.MeasuredValue) && !double.IsNaN(finding.Threshold) && !string.IsNullOrWhiteSpace(finding.Reason))), Is.True);
        }

        [Test, Category(CategoryName)]
        public void CurationBatchGeneratesAtLeastThirtySixRuns()
        {
            Assert.That(batch.GeneratedCount, Is.EqualTo(36)); Assert.That(batch.Records.Count, Is.EqualTo(36));
        }

        [Test, Category(CategoryName)]
        public void BatchCoversAllThreeRun05Recipes()
        {
            Assert.That(batch.RecipeCount, Is.EqualTo(3)); Assert.That(batch.Records.Select(record => record.RecipeId).Distinct().OrderBy(value => value), Is.EqualTo(new[] { "COMPACT_SPLIT_RUN", "TALL_LOOP_RUN", "WIDE_BRANCH_RUN" }));
        }

        [Test, Category(CategoryName)]
        public void BatchCoversTheRequiredTwelveSeedRange()
        {
            var seeds = Enumerable.Range(MoonPalaceRunCurationBatch.FirstSeed, MoonPalaceRunCurationBatch.SeedsPerRecipe).ToArray(); Assert.That(batch.Records.Select(record => record.Seed).Distinct().OrderBy(seed => seed), Is.EqualTo(seeds)); Assert.That(MoonPalaceRunCurationBatch.LastSeed, Is.EqualTo(1924737078));
        }

        [Test, Category(CategoryName)]
        public void AtLeastNineAcceptedRunsExist()
        {
            Assert.That(batch.Accepted.Count, Is.GreaterThanOrEqualTo(9)); Assert.That(batch.Accepted.All(record => record.Status == MoonPalaceRunCurationStatus.Accepted), Is.True);
        }

        [Test, Category(CategoryName)]
        public void AtLeastSixRejectedRunsExist()
        {
            Assert.That(batch.Rejected.Count, Is.GreaterThanOrEqualTo(6)); Assert.That(batch.Rejected.All(record => record.Status == MoonPalaceRunCurationStatus.Rejected), Is.True);
        }

        [Test, Category(CategoryName)]
        public void AcceptedRunsPassTileLevelBfs()
        {
            Assert.That(batch.Accepted.All(record => record.Result.Validation.StartToExitReachable && record.Result.FindRouteToExit().First().Equals(record.Result.StartTile) && record.Result.FindRouteToExit().Last().Equals(record.Result.ExitTile)), Is.True);
        }

        [Test, Category(CategoryName)]
        public void AcceptedRunsHaveReciprocalOpenConnectors()
        {
            Assert.That(batch.Accepted.All(record => record.Result.Connectors.All(connector => connector.IsReciprocal && connector.IsOpen) && record.Result.Validation.AllConnectorsReciprocal && record.Result.Validation.AllConnectorGateTilesOpen), Is.True);
        }

        [Test, Category(CategoryName)]
        public void RejectedRunsKeepConcreteRejectionReasons()
        {
            Assert.That(batch.Rejected.All(record => record.PrimaryFinding.Severity == MoonPalaceRunQualitySeverity.Reject && !string.IsNullOrWhiteSpace(record.PrimaryFinding.RuleId) && !string.IsNullOrWhiteSpace(record.PrimaryFinding.Reason) && record.Analysis.Findings.Any(finding => finding.RuleId == record.PrimaryFinding.RuleId && finding.Severity == MoonPalaceRunQualitySeverity.Reject)), Is.True);
            Assert.That(batch.TopRejectionReasons, Does.Contain("ROUTE_TOO_STRAIGHT"));
        }

        [Test, Category(CategoryName)]
        public void RejectedRunsAreRetainedWithoutSilentRepair()
        {
            Assert.That(batch.Rejected.All(record => record.Result.FallbackCarveCount == 0 && record.Result.SilentRepairCount == 0 && record.Result.RotationCount == 0 && record.Result.Validation.Passed), Is.True);
        }

        [Test, Category(CategoryName)]
        public void RecipeTuningDeltaIsEmptyOrFullyJustified()
        {
            var delta = batch.TuningDelta; Assert.That(delta.IsEmpty || (!string.IsNullOrWhiteSpace(delta.RecipeId) && !string.IsNullOrWhiteSpace(delta.Field) && !string.IsNullOrWhiteSpace(delta.OldValue) && !string.IsNullOrWhiteSpace(delta.NewValue) && !string.IsNullOrWhiteSpace(delta.Reason)), Is.True); Assert.That(delta.CanonicalLine, Is.Not.Empty);
        }

        [Test, Category(CategoryName)]
        public void SameBatchInputProducesTheSameCurationDigest()
        {
            Assert.That(repeat.CurationDigest, Is.EqualTo(batch.CurationDigest)); Assert.That(repeat.Recommended.RecipeId + ":" + repeat.Recommended.Seed, Is.EqualTo(batch.Recommended.RecipeId + ":" + batch.Recommended.Seed));
        }

        [Test, Category(CategoryName)]
        public void SceneContainsAcceptedAndRejectedGalleryGroups()
        {
            var root = Run06SceneRoot(); var names = root.transform.Cast<Transform>().Select(child => child.name).ToArray(); Assert.That(names, Does.Contain("AcceptedGallery")); Assert.That(names, Does.Contain("RejectedGallery")); Assert.That(names, Does.Contain("QualityLegend")); Assert.That(names, Does.Contain("RuleOverlay")); Assert.That(names, Does.Contain("SeedLabels")); Assert.That(names, Does.Contain("Metadata"));
        }

        [Test, Category(CategoryName)]
        public void SceneContainsAtLeastNineAcceptedVisualEntries()
        {
            var gallery = Run06SceneRoot().transform.Find("AcceptedGallery"); Assert.That(gallery, Is.Not.Null); Assert.That(gallery.Cast<Transform>().Count(child => child.name.StartsWith("Accepted_", StringComparison.Ordinal)), Is.GreaterThanOrEqualTo(9));
        }

        [Test, Category(CategoryName)]
        public void SceneContainsAtLeastSixRejectedVisualEntries()
        {
            var gallery = Run06SceneRoot().transform.Find("RejectedGallery"); Assert.That(gallery, Is.Not.Null); Assert.That(gallery.Cast<Transform>().Count(child => child.name.StartsWith("Rejected_", StringComparison.Ordinal)), Is.GreaterThanOrEqualTo(6));
        }

        [Test, Category(CategoryName)]
        public void EditorWindowExposesBatchGalleryAndSelectionControls()
        {
            var type = EditorType(WindowTypeName); Assert.That((string)type.GetField("MenuPath", BindingFlags.Public | BindingFlags.Static).GetRawConstantValue(), Is.EqualTo("Tools/MoonPalace/Run Curation")); var window = (UnityEngine.Object)type.GetMethod("OpenForTests", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
            try { var labels = ((IEnumerable)type.GetProperty("ControlLabels", BindingFlags.Public | BindingFlags.Instance).GetValue(window)).Cast<string>().ToArray(); Assert.That(labels, Is.EquivalentTo(new[] { "Run Curation Batch", "Build Curation Gallery Scene", "Select Next Rejected", "Select Next Accepted" })); }
            finally { UnityEngine.Object.DestroyImmediate(window); }
        }

        [Test, Category(CategoryName)]
        public void JsonAndCsvArtifactCountsAndDigestsAreStable()
        {
            var outputs = GetOutputContents(publication); Assert.That(outputs.Count, Is.EqualTo(14)); Assert.That(outputs.Keys.Count(path => path.EndsWith(".json", StringComparison.Ordinal)), Is.EqualTo(8)); Assert.That(outputs.Keys.Count(path => path.EndsWith(".csv", StringComparison.Ordinal)), Is.EqualTo(6)); var snapshot = InvokeBuilder("BuildSnapshot", new[] { typeof(string), typeof(MoonPalaceCuratedRunSet) }, ProjectRoot, batch); var repeatedOutputs = GetOutputContents(snapshot); Assert.That(repeatedOutputs.Keys, Is.EqualTo(outputs.Keys));
            foreach (var output in outputs) { var bytes = File.ReadAllBytes(Resolve(output.Key)); Assert.That(bytes.Take(3).SequenceEqual(new byte[] { 0xEF, 0xBB, 0xBF }), Is.False); var text = File.ReadAllText(Resolve(output.Key)); Assert.That(text, Is.EqualTo(output.Value)); Assert.That(text, Is.EqualTo(repeatedOutputs[output.Key])); Assert.That(text.EndsWith("\n", StringComparison.Ordinal) && !text.EndsWith("\n\n", StringComparison.Ordinal) && !text.Contains("\r"), Is.True); }
        }

        [Test, Category(CategoryName)]
        public void NoForbiddenSelectionRepairBuildSettingsMutationOrRun07StartIsRecorded()
        {
            Assert.That(batch.Records.All(record => record.Result.FallbackCarveCount == 0 && record.Result.SilentRepairCount == 0 && record.Result.RotationCount == 0), Is.True); Assert.That(EditorApplication.isPlayingOrWillChangePlaymode, Is.False); Assert.That(EditorBuildSettings.scenes.Select(scene => scene.path).OrderBy(path => path, StringComparer.Ordinal).ToArray(), Is.EqualTo(buildSettingsBefore)); Assert.That(Directory.Exists(Resolve("MapDesign/MCP/GENERATED/RUN07")), Is.False);
            var analyzerSource = File.ReadAllText(Resolve("Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceRunQualityAnalyzer.cs")); var builderSource = File.ReadAllText(Resolve("Assets/_Game/Editor/MapAuthoring/WorldGeneration/MoonPalace/RunGeneration/MoonPalaceCuratedRunGallerySceneBuilder.cs")); Assert.That(analyzerSource, Does.Not.Contain("19347")); Assert.That(builderSource, Does.Not.Contain("PlayMode")); Assert.That(builderSource, Does.Not.Contain("RunAll"));
        }

        private static object InvokeBuilder(string method, Type[] parameterTypes, params object[] arguments) => EditorType(BuilderTypeName).GetMethod(method, BindingFlags.Public | BindingFlags.Static, null, parameterTypes, null).Invoke(null, arguments);
        private static Type EditorType(string name) => Assembly.Load("MapAuthoring.Editor").GetType(name, true);
        private GameObject Run06SceneRoot()
        {
            var path = (string)EditorType(BuilderTypeName).GetField("SceneRelativePath", BindingFlags.Public | BindingFlags.Static).GetRawConstantValue(); Assert.That(File.Exists(Resolve(path)), Is.True); var scene = SceneManager.GetSceneByPath(path); Assert.That(scene.IsValid(), Is.True); return scene.GetRootGameObjects().Single(gameObject => gameObject.name == "MoonPalace_CuratedRunGallery_RUN06");
        }
        private static Dictionary<string, string> GetOutputContents(object source) => ((IEnumerable)source.GetType().GetProperty("OutputContents").GetValue(source)).Cast<object>().ToDictionary(item => (string)item.GetType().GetProperty("Key").GetValue(item), item => (string)item.GetType().GetProperty("Value").GetValue(item), StringComparer.Ordinal);
        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName.Replace('\\', '/');
        private static string Resolve(string relative) => Path.Combine(ProjectRoot, relative.Replace('/', Path.DirectorySeparatorChar));
        private static string HashFile(string path) { using (var sha = System.Security.Cryptography.SHA256.Create()) return string.Concat(sha.ComputeHash(File.ReadAllBytes(path)).Select(value => value.ToString("x2"))); }
    }
}
