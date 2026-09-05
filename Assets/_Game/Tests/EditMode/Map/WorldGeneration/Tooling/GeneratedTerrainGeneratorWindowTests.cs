using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.SectorPlanning;
using StarNight.Map.WorldGeneration.Tooling;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Tooling
{
    [Category(CategoryName)]
    public sealed class GeneratedTerrainGeneratorWindowTests
    {
        private const string CategoryName = "MAP20_01";
        private const string EditorAssemblyName = "MapAuthoring.Editor";
        private const string CoordinatorTypeName =
            "StarNight.MapAuthoring.WorldGeneration.Tooling.GeneratedTerrainRunCoordinator";
        private const string WindowTypeName =
            "StarNight.MapAuthoring.WorldGeneration.Tooling.GeneratedTerrainGeneratorWindow";
        private const string TestRootRelative =
            "MapDesign/MCP/GENERATED/MAP20_01/test_runs/focused";

        private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static string TestRoot => ProjectPath(TestRootRelative);

        [SetUp]
        public void SetUp()
        {
            if (Directory.Exists(TestRoot)) Directory.Delete(TestRoot, true);
            Directory.CreateDirectory(TestRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(TestRoot)) Directory.Delete(TestRoot, true);
        }

        [Test]
        public void GeneratorWindowOpensWithoutStartingGeneration()
        {
            var type = EditorType(WindowTypeName);
            type.GetMethod("ResetInvocationDiagnostics", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, null);
            var window = type.GetMethod("Open", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, null);

            Assert.That((int)type.GetProperty("OpenInvocationCount").GetValue(null), Is.EqualTo(1));
            Assert.That((int)type.GetProperty("RunRequestCount").GetValue(null), Is.EqualTo(0));
            Assert.That((bool)type.GetProperty("IsRunActive").GetValue(window), Is.False);
            Assert.That(type.GetProperty("LastArtifact").GetValue(window), Is.Null);
            type.GetMethod("Close", BindingFlags.Public | BindingFlags.Instance).Invoke(window, null);
        }

        [Test]
        public void GeneratorRunArtifactCapturesSeedVersionHashScopePassAndReplay()
        {
            var caseRoot = CaseRoot("artifact");
            var coordinator = Coordinator(caseRoot);
            var request = Request(caseRoot, "artifact-run", GeneratedTerrainRunScope.Pattern, 731);
            var artifact = Run(coordinator, request, GeneratedTerrainRunPlan.DryRun());

            Assert.That(artifact.pass_state, Is.EqualTo("PASS"));
            Assert.That(artifact.seed, Is.EqualTo("731"));
            Assert.That(artifact.generator_version, Is.EqualTo("generator-v1"));
            Assert.That(artifact.data_version, Is.EqualTo("data-v1"));
            Assert.That(artifact.scope, Is.EqualTo("Pattern"));
            Assert.That(artifact.input_digest, Is.EqualTo(request.InputDigest));
            Assert.That(artifact.replay_reference, Does.Contain("seed=731").And.Contain("scope=Pattern"));
            Assert.That(artifact.rollback_available, Is.True);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(artifact.canonical_digest), Is.True);
            Assert.That(artifact.created_utc_excluded_from_canonical_digest, Is.True);

            var first = artifact.canonical_digest;
            artifact.created_utc = "2099-12-31T23:59:59Z";
            Assert.That(artifact.ComputeCanonicalDigest(), Is.EqualTo(first));
            Assert.That(File.ReadAllText(Path.Combine(RunDirectory(coordinator), "run_artifact.json")),
                Does.Contain("\"failure_bundle_reference\"").And.Contain("\"MAP19_exit_digest\""));
        }

        [Test]
        public void GeneratorRunScopesAreExactlyPatternSectorOneRingAndWorld()
        {
            Assert.That(GeneratedTerrainRunScopeCatalog.Tokens,
                Is.EqualTo(new[] { "Pattern", "Sector", "OneRing", "World" }));
            Assert.That(GeneratedTerrainRunScopeCatalog.Scopes.Count, Is.EqualTo(4));
            Assert.That(Enum.GetNames(typeof(GeneratedTerrainRunScope)),
                Is.EqualTo(new[] { "Pattern", "Sector", "OneRing", "World" }));
        }

        [Test]
        public void OneRingScopeUsesInBoundsMooreRadiusOneAndMaxNineSectors()
        {
            var interior = GeneratedTerrainRunScopeCatalog.ResolveSectorCoordinates(
                GeneratedTerrainRunScope.OneRing, 6, 6);
            var corner = GeneratedTerrainRunScopeCatalog.ResolveSectorCoordinates(
                GeneratedTerrainRunScope.OneRing, 0, 0);

            Assert.That(WorldRollbackScope.Radius, Is.EqualTo(1));
            Assert.That(WorldRollbackScope.MaximumSectorCount, Is.EqualTo(9));
            Assert.That(interior.Count, Is.EqualTo(9));
            Assert.That(interior.All(value => Math.Abs(value.X - 6) <= 1 &&
                                              Math.Abs(value.Y - 6) <= 1), Is.True);
            Assert.That(corner.Select(value => value.ToString()),
                Is.EqualTo(new[] { "(0,0)", "(1,0)", "(0,1)", "(1,1)" }));
            Assert.That(corner.All(value => value.IsInBounds), Is.True);
        }

        [Test]
        public void RunCoordinatorCreatesRollbackSnapshotBeforeOutputMutation()
        {
            var caseRoot = CaseRoot("snapshot");
            var selectedFile = OutputPath(caseRoot, "patterns/pattern-a/terrain.txt");
            Write(selectedFile, "before");
            var coordinator = Coordinator(caseRoot);
            var request = Request(caseRoot, "snapshot-run", GeneratedTerrainRunScope.Pattern, 732);
            var plan = new GeneratedTerrainRunPlan(new[]
            {
                new GeneratedTerrainOutputMutation("patterns/pattern-a/terrain.txt", "after", false),
            });
            var artifact = Run(coordinator, request, plan);

            var runDirectory = RunDirectory(coordinator);
            var manifestJson = File.ReadAllText(Path.Combine(runDirectory,
                "rollback_snapshot_manifest.json"));
            var backup = Path.Combine(runDirectory, "snapshot_files", "patterns", "pattern-a",
                "terrain.txt");
            Assert.That(File.ReadAllText(backup), Is.EqualTo("before"));
            Assert.That(File.ReadAllText(selectedFile), Is.EqualTo("after"));
            Assert.That(manifestJson, Does.Contain(Hash("before")));
            Assert.That(artifact.pre_run_output_digest, Is.Not.EqualTo(artifact.post_run_output_digest));
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(artifact.rollback_snapshot_digest), Is.True);
        }

        [Test]
        public void RollbackRestoresOnlySelectedGeneratedOutputScope()
        {
            var caseRoot = CaseRoot("rollback");
            var selected = OutputPath(caseRoot, "sectors/x06_y06/terrain.txt");
            var sibling = OutputPath(caseRoot, "sectors/x07_y06/terrain.txt");
            Write(selected, "selected-before");
            Write(sibling, "sibling-before");
            var coordinator = Coordinator(caseRoot);
            var request = Request(caseRoot, "rollback-run", GeneratedTerrainRunScope.Sector, 733,
                sectorX: 6, sectorY: 6);
            var plan = new GeneratedTerrainRunPlan(new[]
            {
                new GeneratedTerrainOutputMutation("sectors/x06_y06/terrain.txt", "selected-after", false),
                new GeneratedTerrainOutputMutation("sectors/x06_y06/new.txt", "new", false),
            });
            Run(coordinator, request, plan);
            Assert.That(File.ReadAllText(selected), Is.EqualTo("selected-after"));

            var rollback = Rollback(coordinator);
            Assert.That(rollback.pass_state, Is.EqualTo("PASS"));
            Assert.That(File.ReadAllText(selected), Is.EqualTo("selected-before"));
            Assert.That(File.Exists(OutputPath(caseRoot, "sectors/x06_y06/new.txt")), Is.False);
            Assert.That(File.ReadAllText(sibling), Is.EqualTo("sibling-before"));
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(rollback.canonical_digest), Is.True);
            Assert.That(File.Exists(Path.Combine(RunDirectory(coordinator), "rollback_result.json")), Is.True);
        }

        [Test]
        public void FailureRunPublishesReplayAndNoMap20_02Handoff()
        {
            var caseRoot = CaseRoot("failure");
            var selected = OutputPath(caseRoot, "patterns/pattern-a/terrain.txt");
            Write(selected, "stable-before");
            var coordinator = Coordinator(caseRoot);
            var request = Request(caseRoot, "failure-run", GeneratedTerrainRunScope.Pattern, 734);
            var plan = new GeneratedTerrainRunPlan(new[]
            {
                new GeneratedTerrainOutputMutation("patterns/pattern-a/terrain.txt", "failed-write", false),
            }, "MAP19_08", "synthetic adapter failure", true,
                "map19_08://failure-bundle/failure-run");
            var artifact = Run(coordinator, request, plan);

            Assert.That(artifact.pass_state, Is.EqualTo("FAIL"));
            Assert.That(artifact.failure_owner, Is.EqualTo("MAP19_08"));
            Assert.That(artifact.failure_reason, Does.Contain("synthetic"));
            Assert.That(artifact.replay_reference, Is.Not.Empty);
            Assert.That(artifact.failure_bundle_reference, Does.StartWith("map19_08://"));
            Assert.That(artifact.rollback_applied, Is.True);
            Assert.That(File.ReadAllText(selected), Is.EqualTo("stable-before"));
            Assert.That(File.ReadAllText(Path.Combine(RunDirectory(coordinator), "run_artifact.json")),
                Does.Not.Contain("MAP20_02"));
            Assert.That(File.ReadAllText(ProjectPath("MapDesign/MCP/06_IMPLEMENTATION_STATUS.md")),
                Does.Contain("| MAP20_02_CREATE_WORLD_OVERLAYS_AND_SECTOR_CANVAS_INSPECTOR | LOCKED |"));
        }

        [Test]
        public void GeneratorWindowDoesNotSelectLegacyRegressionPriorCategoriesPlayModeOrFullRegression()
        {
            var windowSource = File.ReadAllText(ProjectPath(
                "Assets/_Game/Editor/MapAuthoring/WorldGeneration/Tooling/GeneratedTerrainGeneratorWindow.cs"));
            var coordinatorSource = File.ReadAllText(ProjectPath(
                "Assets/_Game/Editor/MapAuthoring/WorldGeneration/Tooling/GeneratedTerrainRunCoordinator.cs"));
            var combined = windowSource + "\n" + coordinatorSource;

            Assert.That(combined, Does.Not.Contain("19347"));
            Assert.That(combined, Does.Not.Contain("PlayMode"));
            Assert.That(combined, Does.Not.Contain("RunMap19_09ScaleAudit"));
            Assert.That(combined, Does.Not.Contain("TestRunnerApi"));
            Assert.That(combined, Does.Not.Contain("run_tests"));
            Assert.That(combined, Does.Not.Contain("MAP20_02"));
            Assert.That(windowSource, Does.Contain("worldConfirmed").And.Contain("ToggleLeft"));
        }

        private static object Coordinator(string caseRootRelative) => Activator.CreateInstance(
            EditorType(CoordinatorTypeName), ProjectRoot, caseRootRelative);

        private static GeneratedTerrainRunArtifact Run(object coordinator,
            GeneratedTerrainRunRequest request, GeneratedTerrainRunPlan plan) =>
            (GeneratedTerrainRunArtifact)Invoke(coordinator, "Run", request, plan);

        private static GeneratedTerrainRollbackResult Rollback(object coordinator) =>
            (GeneratedTerrainRollbackResult)Invoke(coordinator, "RollbackLastRun");

        private static object Invoke(object target, string method, params object[] arguments)
        {
            try
            {
                return target.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.Instance)
                    .Invoke(target, arguments);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        private static Type EditorType(string fullName)
        {
            var type = Type.GetType(fullName + ", " + EditorAssemblyName, false);
            Assert.That(type, Is.Not.Null, fullName + " must compile into " + EditorAssemblyName + ".");
            return type;
        }

        private static GeneratedTerrainRunRequest Request(string caseRootRelative, string runId,
            GeneratedTerrainRunScope scope, ulong seed, int sectorX = 6, int sectorY = 6)
        {
            var digest = GeneratedTerrainRunRequest.ComputeInputDigest(seed, scope,
                "generator-v1", "data-v1", "pattern-a", sectorX, sectorY);
            return new GeneratedTerrainRunRequest(runId, scope, seed, "generator-v1", "data-v1",
                digest, caseRootRelative + "/outputs", "pattern-a", sectorX, sectorY,
                true, false);
        }

        private static string CaseRoot(string name)
        {
            var relative = TestRootRelative + "/" + name;
            Directory.CreateDirectory(ProjectPath(relative));
            return relative;
        }

        private static string OutputPath(string caseRootRelative, string relative) =>
            ProjectPath(caseRootRelative + "/outputs/" + relative);

        private static string RunDirectory(object coordinator) =>
            (string)coordinator.GetType().GetProperty("LastRunDirectory").GetValue(coordinator);

        private static string ProjectPath(string relative) => Path.GetFullPath(Path.Combine(ProjectRoot,
            relative.Replace('/', Path.DirectorySeparatorChar)));

        private static void Write(string path, string contents)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, contents, BakingCanonicalDigest.Utf8NoBomEncoding);
        }

        private static string Hash(string contents)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
                return string.Concat(sha.ComputeHash(BakingCanonicalDigest.Utf8NoBomEncoding.GetBytes(contents))
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }
}
