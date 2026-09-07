using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.MoonPalace.Visualization;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarNight.Tests.EditMode.Map.WorldGeneration.MoonPalace.Visualization
{
    public sealed class MoonPalaceGrayboxGeneratorWindowTests
    {
        private const string CategoryName = "VIS02";
        private const string CoordinatorTypeName =
            "StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.Visualization.MoonPalaceGrayboxRunCoordinator";
        private const string WindowTypeName =
            "StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.Visualization.MoonPalaceGrayboxGeneratorWindow";
        private const string HistoryPublisherTypeName =
            "StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.Visualization.MoonPalaceGrayboxRunHistoryPublisher";

        private MoonPalaceGrayboxRunResult dryRun;
        private MoonPalaceGrayboxRunResult actualRun;
        private IReadOnlyList<string> sourcePaths;
        private Dictionary<string, string> sourceBefore;

        [OneTimeSetUp]
        public void ExecuteVis02DryRunAndActualRun()
        {
            sourcePaths = (IReadOnlyList<string>)BuilderType.GetMethod("GetSourceReadRelativePaths", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, null);
            sourceBefore = sourcePaths.ToDictionary(path => path, FileSha, StringComparer.Ordinal);
            dryRun = (MoonPalaceGrayboxRunResult)InvokeCoordinator("DryRun");
            actualRun = (MoonPalaceGrayboxRunResult)InvokeCoordinator("Run");
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceGrayboxRunRequestDefaultsToMpQa01CenterSectorAnd500Candidates()
        {
            var dryRequest = MoonPalaceGrayboxRunRequest.CreateDefault(MoonPalaceGrayboxRunMode.DryRun);
            var runRequest = MoonPalaceGrayboxRunRequest.CreateDefault(MoonPalaceGrayboxRunMode.Run);
            Assert.That(dryRequest.SeedId, Is.EqualTo("MP_QA_01"));
            Assert.That(dryRequest.SeedValue, Is.EqualTo(1924737067));
            Assert.That(dryRequest.SectorX, Is.EqualTo(6));
            Assert.That(dryRequest.SectorY, Is.EqualTo(6));
            Assert.That(dryRequest.CandidateCount, Is.EqualTo(500));
            Assert.That(dryRequest.OutputScenePath, Is.EqualTo(MoonPalaceGrayboxRunRequest.RequiredOutputScenePath));
            Assert.That(dryRequest.AuthoringOutputRoot, Does.EndWith("/VIS01"));
            Assert.That(dryRequest.GeneratedOutputRoot, Does.EndWith("/VIS01"));
            Assert.That(dryRequest.RequestDigest, Is.Not.Empty.And.EqualTo(
                MoonPalaceGrayboxRunRequest.CreateDefault(MoonPalaceGrayboxRunMode.DryRun).RequestDigest));
            Assert.That(runRequest.RequestDigest, Is.Not.EqualTo(dryRequest.RequestDigest));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceGrayboxDryRunExecutesGenerationWithoutWritingScene()
        {
            Assert.That(dryRun.Success, Is.True);
            Assert.That(dryRun.Mode, Is.EqualTo(MoonPalaceGrayboxRunMode.DryRun));
            Assert.That(dryRun.SceneWritten, Is.False);
            Assert.That(dryRun.SceneDeletedBeforeWrite, Is.False);
            Assert.That(dryRun.SceneRecreated, Is.False);
            Assert.That(dryRun.CsvCount, Is.Zero);
            Assert.That(dryRun.JsonCount, Is.Zero);
            Assert.That(dryRun.SceneCount, Is.Zero);
            Assert.That(dryRun.Generation.Cells.Count, Is.EqualTo(1536));
            Assert.That(dryRun.LogicalMapDigest, Is.Not.Empty);
            Assert.That(dryRun.WriteScopeSummary.Vis01RefreshPaths, Is.Empty);
            Assert.That(dryRun.WriteScopeSummary.Vis02HistoryPaths, Is.EquivalentTo(new[]
            {
                "MapDesign/MCP/GENERATED/VIS02/moonpalace_vis02_dry_run_result.json"
            }));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceGrayboxRunRegeneratesIsolatedVis01SceneAndManifest()
        {
            Assert.That(actualRun.Success, Is.True);
            Assert.That(actualRun.Mode, Is.EqualTo(MoonPalaceGrayboxRunMode.Run));
            Assert.That(actualRun.SceneWritten, Is.True);
            Assert.That(actualRun.SceneRecreated, Is.True);
            Assert.That(actualRun.SceneDeletedBeforeWrite, Is.False, "VIS02 uses deterministic in-place SaveScene inside the allowed VIS01 root.");
            Assert.That(actualRun.SceneCount, Is.EqualTo(1));
            Assert.That(File.Exists(Resolve(MoonPalaceGrayboxRunRequest.RequiredOutputScenePath)), Is.True);
            Assert.That(actualRun.SceneManifestDigest.Length, Is.EqualTo(64));
            Assert.That(File.ReadAllText(Resolve(MoonPalaceGrayboxRunRequest.RequiredOutputScenePath)),
                Does.Contain("MoonPalace_Graybox_VIS01").And.Contain("Terrain_Solid_Open"));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceGrayboxRunCoordinatorReusesVis01GenerationFlowWithoutStaticMapCopy()
        {
            var coordinatorSource = File.ReadAllText(Resolve(
                "Assets/_Game/Editor/MapAuthoring/WorldGeneration/MoonPalace/Visualization/MoonPalaceGrayboxRunCoordinator.cs"));
            Assert.That(coordinatorSource, Does.Contain("MoonPalaceGrayboxExampleSceneBuilder.BuildSnapshot"));
            Assert.That(coordinatorSource, Does.Contain("MoonPalaceGrayboxExampleSceneBuilder.Publish"));
            Assert.That(coordinatorSource, Does.Not.Contain("new MoonPalaceGrayboxCellRecord"));
            Assert.That(coordinatorSource, Does.Not.Contain("staticMap"));
            Assert.That(dryRun.LogicalMapDigest, Is.EqualTo(actualRun.LogicalMapDigest));
            Assert.That(dryRun.CandidateDigest, Is.EqualTo(actualRun.CandidateDigest));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceGrayboxRunPreserves1536Cells96Patterns16ChunksAnd10752LayerRecords()
        {
            var generation = actualRun.Generation;
            Assert.That(generation.CandidateSet.RawMaskCount, Is.EqualTo(65536));
            Assert.That(generation.CandidateSet.Candidates.Count, Is.EqualTo(500));
            Assert.That(generation.Cells.Count, Is.EqualTo(1536));
            Assert.That(generation.PatternPlacements.Count, Is.EqualTo(96));
            Assert.That(generation.MicroChunks.Count, Is.EqualTo(16));
            Assert.That(generation.LogicalLayers.Count, Is.EqualTo(10752));
            Assert.That(actualRun.CsvCount, Is.EqualTo(7));
            Assert.That(actualRun.JsonCount, Is.EqualTo(6));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceGrayboxRunKeepsRouteRecoverySeamAndUnreachableFailuresZero()
        {
            var validation = actualRun.ValidationSummary;
            Assert.That(validation.RouteFailureCount, Is.Zero);
            Assert.That(validation.RecoveryFailureCount, Is.Zero);
            Assert.That(validation.SeamFailureCount, Is.Zero);
            Assert.That(validation.UnreachableMicroChunkCount, Is.Zero);
            Assert.That(validation.FallbackCarveCount, Is.Zero);
            Assert.That(validation.SilentAutoRepairCount, Is.Zero);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceGrayboxRunHistoryRecordsDryRunRunDigestsAndWriteScope()
        {
            var history = (IReadOnlyList<MoonPalaceGrayboxRunResult>)InvokeCoordinator("GetRunHistory");
            Assert.That(history.Count, Is.EqualTo(2));
            Assert.That(history.Select(item => item.Mode), Is.EqualTo(new[] { MoonPalaceGrayboxRunMode.DryRun, MoonPalaceGrayboxRunMode.Run }));
            var expected = (IReadOnlyList<string>)HistoryPublisherType.GetMethod("GetAllVis02RelativePaths", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, null);
            Assert.That(expected.Count, Is.EqualTo(5));
            foreach (var path in expected) Assert.That(File.Exists(Resolve(path)), Is.True, path);
            Assert.That(File.ReadAllText(Resolve(expected[0])), Does.Contain("\"mode\": \"DryRun\""));
            Assert.That(File.ReadAllText(Resolve(expected[1])), Does.Contain("\"mode\": \"Run\""));
            Assert.That(File.ReadAllText(Resolve(expected[2])), Does.Contain("DryRun").And.Contain("Run"));
            Assert.That(File.ReadAllText(Resolve(expected[3])), Does.Contain("VIS01_REFRESH").And.Contain("VIS02_HISTORY"));
            Assert.That(File.ReadAllText(Resolve(expected[4])), Does.Contain(actualRun.LogicalMapDigest));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceGrayboxWindowOrMenuEntrypointExistsAndReportsLastRunSummary()
        {
            var methods = WindowType.GetMethods(BindingFlags.Public | BindingFlags.Static).Select(method => method.Name).ToArray();
            Assert.That(methods, Does.Contain("OpenWindow").And.Contain("DryRunFromMenu").And.Contain("RunFromMenu"));
            Assert.That(methods, Does.Contain("OpenVis01SceneFromMenu").And.Contain("GetLastRunSummary"));
            var menuMethods = WindowType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.GetCustomAttributes(typeof(MenuItem), false).Length > 0).ToArray();
            Assert.That(menuMethods.Length, Is.GreaterThanOrEqualTo(4));
            var summary = (string)WindowType.GetMethod("GetLastRunSummary", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
            Assert.That(summary, Does.Contain("VIS02 Run").And.Contain(actualRun.LogicalMapDigest));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceGrayboxOpenSceneCommandTargetsOnlyVis01Scene()
        {
            InvokeCoordinator("OpenVis01Scene");
            var active = SceneManager.GetActiveScene();
            Assert.That(active.path, Is.EqualTo(MoonPalaceGrayboxRunRequest.RequiredOutputScenePath));
            Assert.That(active.name, Is.EqualTo("MoonPalaceGrayboxExample_VIS01"));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceGrayboxWriteScopeRejectsExistingScenePrefabBuildSettingsAndAddressablesMutation()
        {
            var scope = actualRun.WriteScopeSummary;
            Assert.That(scope.Vis01RefreshPaths.All(path =>
                (path.StartsWith("Assets/_Game/Map/", StringComparison.Ordinal) &&
                 path.IndexOf("/VIS01/", StringComparison.Ordinal) >= 0) ||
                path.StartsWith("MapDesign/MCP/GENERATED/VIS01/", StringComparison.Ordinal)), Is.True);
            Assert.That(scope.Vis02HistoryPaths.All(path => path.StartsWith("MapDesign/MCP/GENERATED/VIS02/", StringComparison.Ordinal)), Is.True);
            Assert.That(scope.ExistingScenePrefabMutationsOutsideVis01, Is.Zero);
            Assert.That(scope.BuildSettingsMutationCount, Is.Zero);
            Assert.That(scope.AddressablesMutationCount, Is.Zero);
            Assert.That(scope.FullWorldOutputWriteCount, Is.Zero);
            Assert.That(scope.FullWorldRunCount, Is.Zero);
            Assert.That(EditorBuildSettings.scenes.Any(scene => scene.path == MoonPalaceGrayboxRunRequest.RequiredOutputScenePath), Is.False);
            Assert.That(sourcePaths.ToDictionary(path => path, FileSha, StringComparer.Ordinal), Is.EqualTo(sourceBefore));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceGrayboxDigestIsRepeatReverseAndCultureStable()
        {
            var culture = CultureInfo.CurrentCulture;
            var uiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var reverse = MoonPalaceOneSectorGrayboxGenerator.Generate(ProjectRoot, true);
                Assert.That(reverse.LogicalMapDigest, Is.EqualTo(actualRun.LogicalMapDigest));
                Assert.That(reverse.CandidateSet.CanonicalDigest, Is.EqualTo(actualRun.CandidateDigest));
                Assert.That(MoonPalaceGrayboxRunRequest.CreateDefault(MoonPalaceGrayboxRunMode.Run).RequestDigest,
                    Is.EqualTo(MoonPalaceGrayboxRunRequest.CreateDefault(MoonPalaceGrayboxRunMode.Run).RequestDigest));
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = uiCulture;
            }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceGrayboxWorkDoesNotRunLegacyRegressionPlayModeBuildFullWorldOrVis03()
        {
            var validation = actualRun.ValidationSummary;
            Assert.That(validation.PriorTaskTestSelectionCount, Is.Zero);
            Assert.That(validation.Legacy19347SelectionCount, Is.Zero);
            Assert.That(validation.PlayModeSelectionCount, Is.Zero);
            Assert.That(validation.UnfilteredTestSelectionCount, Is.Zero);
            Assert.That(validation.FullRegressionRunCount, Is.Zero);
            Assert.That(validation.FullWorldGenerationRunCount, Is.Zero);
            Assert.That(validation.PlayerBuildExecutionCount, Is.Zero);
            Assert.That(actualRun.NonExecutionSummary, Does.Contain("full_world=0").And.Contain("player_build=0"));
            Assert.That(Directory.Exists(Resolve("MapDesign/MCP/GENERATED/VIS03")), Is.False);
            Assert.That(File.Exists(Resolve("MapDesign/MCP/REPORTS/VIS03_GENERATE_3X3_ONE_RING_GRAYBOX_SCENE_RESULT.md")), Is.False);
        }

        private static object InvokeCoordinator(string method, params object[] args)
        {
            return CoordinatorType.GetMethod(method, BindingFlags.Public | BindingFlags.Static).Invoke(null, args);
        }
        private static Type CoordinatorType => Assembly.Load("MapAuthoring.Editor").GetType(CoordinatorTypeName, true);
        private static Type WindowType => Assembly.Load("MapAuthoring.Editor").GetType(WindowTypeName, true);
        private static Type HistoryPublisherType => Assembly.Load("MapAuthoring.Editor").GetType(HistoryPublisherTypeName, true);
        private static Type BuilderType => Assembly.Load("MapAuthoring.Editor").GetType(
            "StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.Visualization.MoonPalaceGrayboxExampleSceneBuilder", true);
        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        private static string Resolve(string relative) => Path.Combine(ProjectRoot, relative.Replace('/', Path.DirectorySeparatorChar));
        private static string FileSha(string relative)
        {
            using (var stream = File.OpenRead(Resolve(relative)))
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }
}
