using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Validation;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Validation
{
    [TestFixture]
    [Category("MAP19_08")]
    public sealed class GeneratedFailureBundleHeadlessRunnerTests
    {
        [Test]
        public void FailureBundleCapturesSeedVersionHashPassCoordinatesProvenanceAndAttachments()
        {
            var chain = Chain();
            var result = GeneratedFailureBundleFactory.Create(Manifest(chain));

            Assert.That(result.Success, Is.True, FailureText(result));
            var manifest = result.Payload.Manifest;
            Assert.That(manifest.SeedIdentity.WorldSeed, Is.EqualTo(731UL));
            Assert.That(manifest.VersionIdentity.GeneratorVersion, Is.EqualTo("generator-19.8"));
            Assert.That(manifest.VersionIdentity.DataVersion, Is.EqualTo("data-19.8"));
            Assert.That(manifest.SeedIdentity.ContentHash, Is.EqualTo(
                GeneratedFailureBundleSeedIdentity.FromSaveManifest(731,
                    GeneratedFailureBundleSourceKind.FocusedFixture, SaveHeader(731)).ContentHash));
            Assert.That(manifest.PassSnapshots, Has.Count.EqualTo(2));
            Assert.That(manifest.FailureRecords, Has.Count.EqualTo(1));
            Assert.That(manifest.CoordinateRecords, Has.Count.EqualTo(2));
            Assert.That(manifest.ProvenanceRecords, Has.Count.EqualTo(2));
            Assert.That(manifest.AttachmentReferences, Has.Count.EqualTo(2));
            Assert.That(manifest.UpstreamDigests.Select(value => value.PhaseId),
                Is.EquivalentTo(GeneratedValidationChainSnapshot.RequiredPhaseIds));
        }

        [Test]
        public void FailureBundleSerializesManifestCsvAndScreenshotReferencesDeterministically()
        {
            var chain = Chain();
            var first = GeneratedFailureBundleFactory.Create(Manifest(chain, false, "2026-09-05T01:00:00Z"));
            var second = GeneratedFailureBundleFactory.Create(Manifest(chain, true, "2040-01-01T00:00:00Z"));

            Assert.That(first.Success && second.Success, Is.True);
            Assert.That(first.Payload.Manifest.ManifestDigest,
                Is.EqualTo(second.Payload.Manifest.ManifestDigest));
            Assert.That(first.Payload.PayloadDigest, Is.EqualTo(second.Payload.PayloadDigest),
                "The serialized audit timestamp is excluded from every canonical bundle digest.");
            Assert.That(first.Payload.ManifestJson, Does.Contain("canonicalBundleDigest"));
            Assert.That(first.Payload.PassSnapshotsCsv, Does.StartWith("pass_id,decision"));
            Assert.That(first.Payload.FailuresCsv, Does.Contain("MAP19_06"));
            Assert.That(first.Payload.CoordinatesCsv, Does.Contain("NODE-A"));
            Assert.That(first.Payload.ProvenanceCsv, Does.Contain("MAP19_07"));
            Assert.That(first.Payload.ScreenshotReferencesCsv, Does.Contain("overview"));
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(GeneratedFailureBundleDigest.Schema), Is.True);
        }

        [Test]
        public void HeadlessRunnerParsesReplayRangeWorkerAndOutputArguments()
        {
            var replay = GeneratedValidationRunnerArgumentParser.Parse(Args("replay", "out/replay",
                "--seed", "93", "--fail-fast"));
            var range = GeneratedValidationRunnerArgumentParser.Parse(Args("range", "out/range",
                "--seed-start", "100", "--seed-count", "9", "--worker-index", "1",
                "--worker-count", "3"));
            var plan = GeneratedValidationRunnerArgumentParser.Parse(Args("plan", "out/plan",
                "--seed-start", "10", "--seed-count", "2"));
            var replayPlan = GeneratedValidationRunnerPlanner.Build(replay.Arguments, Chain());

            Assert.That(replay.Success && range.Success && plan.Success && replayPlan.Success, Is.True);
            Assert.That(replay.Arguments.Mode, Is.EqualTo(GeneratedValidationRunnerMode.Replay));
            Assert.That(replay.Arguments.Seed, Is.EqualTo(93UL));
            Assert.That(replay.Arguments.FailFast, Is.True);
            Assert.That(replayPlan.Plan.ReplayRequest.Seed, Is.EqualTo(93UL));
            Assert.That(range.Arguments.WorkerIndex, Is.EqualTo(1));
            Assert.That(range.Arguments.WorkerCount, Is.EqualTo(3));
            Assert.That(plan.Arguments.OutputRoot, Is.EqualTo("out/plan"));
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                GeneratedValidationRunnerArgumentParser.SchemaDigest), Is.True);
        }

        [Test]
        public void HeadlessRunnerBuildsDeterministicWorkerPartitionsWithoutRunningScaleAudit()
        {
            var parsed = GeneratedValidationRunnerArgumentParser.Parse(Args("range", "out/range",
                "--seed-start", "100", "--seed-count", "8", "--worker-index", "1",
                "--worker-count", "3"));
            var first = GeneratedValidationRunnerPlanner.Build(parsed.Arguments, Chain());
            var second = GeneratedValidationRunnerPlanner.Build(parsed.Arguments, Chain(true));
            var all = GeneratedValidationRunnerPlanner.Partition(100, 8, 3);

            Assert.That(first.Success && second.Success, Is.True);
            Assert.That(first.Plan.PlanDigest, Is.EqualTo(second.Plan.PlanDigest));
            Assert.That(first.Plan.WorkerPartitions, Has.Count.EqualTo(1));
            Assert.That(first.Plan.WorkerPartitions[0].WorkerIndex, Is.EqualTo(1));
            Assert.That(first.Plan.WorkerPartitions[0].Range.SeedStart, Is.EqualTo(103UL));
            Assert.That(first.Plan.WorkerPartitions[0].Range.SeedCount, Is.EqualTo(3UL));
            Assert.That(all.Sum(value => (decimal)value.Range.SeedCount), Is.EqualTo(8m));
            Assert.That(first.Plan.PhaseOrder, Is.EqualTo(GeneratedValidationChainSnapshot.RequiredPhaseIds));
        }

        [Test]
        public void HeadlessRunnerWritesOnlyInsideExplicitOutputRootInFocusedDryRun()
        {
            var temporaryRoot = NewTemporaryRoot();
            try
            {
                var output = Path.Combine(temporaryRoot, "bundle");
                var parsed = GeneratedValidationRunnerArgumentParser.Parse(Args("plan", output,
                    "--seed-start", "100", "--seed-count", "4"));
                var result = RunFocused(parsed.Arguments, Chain(), plan => Manifest(plan.Chain),
                    temporaryRoot);

                Assert.That(result.Success, Is.True, FailureText(result));
                Assert.That(result.FilesWritten, Is.EqualTo(7));
                Assert.That(result.SyntheticCallbacks, Is.EqualTo(1));
                Assert.That(Directory.GetFiles(output).Select(Path.GetFileName).OrderBy(value => value),
                    Is.EqualTo(new[] { "coordinates.csv", "failures.csv", "manifest.json",
                        "pass_snapshots.csv", "provenance.csv", "runner_plan.json",
                        "screenshot_references.csv" }));
                Assert.That(Directory.GetDirectories(temporaryRoot), Has.Length.EqualTo(1));
            }
            finally
            {
                if (Directory.Exists(temporaryRoot)) Directory.Delete(temporaryRoot, true);
            }
        }

        [Test]
        public void FailureBundleRunnerConsumesMap19_02ToMap19_07SurfacesReadOnly()
        {
            var owners = Enumerable.Range(2, 6).Select(_ => new object()).ToArray();
            var chain = Chain(false, owners);
            var before = chain.Digest;
            var parsed = GeneratedValidationRunnerArgumentParser.Parse(Args("plan", "out/plan",
                "--seed-start", "1", "--seed-count", "2"));
            var result = GeneratedValidationRunnerPlanner.Build(parsed.Arguments, chain);

            Assert.That(result.Success, Is.True, FailureText(result));
            Assert.That(result.Plan.Chain, Is.SameAs(chain));
            Assert.That(result.Plan.Chain.References.Select(value => value.Surface), Is.EqualTo(owners));
            Assert.That(chain.Digest, Is.EqualTo(before));
            Assert.That(result.Plan.Chain.BundleDigests().Select(value => value.Digest),
                Is.EqualTo(ExpectedDigests()));
        }

        [Test]
        public void FailureBundleRunnerRejectsMissingDigestUnsafeOutputAndInvalidArguments()
        {
            var missing = Chain(digestOverride: Hash("WRONG-MAP19-07"));
            var missingHandoff = Chain(handoffOverride: string.Empty);
            var parsed = GeneratedValidationRunnerArgumentParser.Parse(Args("plan", "out/plan",
                "--seed-start", "1", "--seed-count", "2"));
            var planFailure = GeneratedValidationRunnerPlanner.Build(parsed.Arguments, missing);
            var handoffFailure = GeneratedValidationRunnerPlanner.Build(parsed.Arguments, missingHandoff);
            var invalid = GeneratedValidationRunnerArgumentParser.Parse(new[]
                { "--mode", "range", "--seed-count", "0", "--capture-screenshot" });
            var startAttempt = GeneratedValidationRunnerArgumentParser.Parse(Args("plan", "out/plan",
                "--seed-start", "1", "--seed-count", "1").Concat(new[]
                { "--start-map19-09", "true" }));
            string resolved;
            string reason;

            Assert.That(planFailure.Success, Is.False);
            Assert.That(planFailure.SuccessPlanDigest, Is.Empty);
            Assert.That(handoffFailure.Success, Is.False);
            Assert.That(handoffFailure.SuccessPlanDigest, Is.Empty);
            Assert.That(invalid.Success, Is.False);
            Assert.That(startAttempt.Success, Is.False);
            Assert.That(TryResolveSafeOutput("../outside", Path.GetTempPath(), out resolved,
                out reason), Is.False);
            Assert.That(reason, Is.EqualTo("PARENT_TRAVERSAL"));

            var temporaryRoot = NewTemporaryRoot();
            try
            {
                var scaleAttempt = GeneratedValidationRunnerArgumentParser.Parse(Args("range",
                    Path.Combine(temporaryRoot, "bundle"), "--seed-start", "1", "--seed-count", "9"));
                var scaleFailure = RunFocused(scaleAttempt.Arguments, Chain(),
                    plan => Manifest(plan.Chain), temporaryRoot);
                Assert.That(scaleFailure.Success, Is.False);
                Assert.That(scaleFailure.Failures.Single().Reason,
                    Is.EqualTo("PRODUCTION_RANGE_ATTEMPT_REJECTED"));
                Assert.That(scaleFailure.SyntheticCallbacks, Is.Zero);
            }
            finally
            {
                if (Directory.Exists(temporaryRoot)) Directory.Delete(temporaryRoot, true);
            }
        }

        [Test]
        public void FailureBundleRunnerFailuresAreAtomicAndReportOwnerReasonExpectedActual()
        {
            var invalidManifest = Manifest(Chain(), omitFailureOwner: true);
            var bundleFailure = GeneratedFailureBundleFactory.Create(invalidManifest);
            var identityFailure = GeneratedFailureBundleFactory.Create(Manifest(Chain(),
                missingIdentity: true));
            var coordinateFailure = GeneratedFailureBundleFactory.Create(Manifest(Chain(),
                invalidCoordinate: true));
            var provenanceFailure = GeneratedFailureBundleFactory.Create(Manifest(Chain(),
                invalidProvenance: true));
            Assert.That(bundleFailure.Success, Is.False);
            Assert.That(identityFailure.Success, Is.False);
            Assert.That(coordinateFailure.Success, Is.False);
            Assert.That(provenanceFailure.Success, Is.False);
            Assert.That(bundleFailure.SuccessManifestDigest, Is.Empty);
            Assert.That(bundleFailure.SuccessPayloadDigest, Is.Empty);
            Assert.That(bundleFailure.Failures.All(value => !string.IsNullOrEmpty(value.Owner) &&
                !string.IsNullOrEmpty(value.Reason) && !string.IsNullOrEmpty(value.Expected) &&
                !string.IsNullOrEmpty(value.Actual)), Is.True);

            var temporaryRoot = NewTemporaryRoot();
            try
            {
                var output = Path.Combine(temporaryRoot, "bundle");
                var parsed = GeneratedValidationRunnerArgumentParser.Parse(Args("plan", output,
                    "--seed-start", "1", "--seed-count", "1"));
                var rejectingWriter = new RejectingWriter();
                var result = RunFocused(parsed.Arguments, Chain(), plan => Manifest(plan.Chain),
                    temporaryRoot, CreateWriterDelegate(rejectingWriter));

                Assert.That(result.Success, Is.False);
                Assert.That(result.ExitCode, Is.EqualTo(GeneratedValidationRunnerExitCode.OutputWriteFailed));
                Assert.That(result.SuccessManifestDigest, Is.Empty);
                Assert.That(result.SuccessPayloadDigest, Is.Empty);
                Assert.That(result.SuccessPlanDigest, Is.Empty);
                Assert.That(result.SuccessHandoffDigest, Is.Empty);
                Assert.That(Directory.Exists(output), Is.False);
                Assert.That(Directory.GetDirectories(temporaryRoot), Is.Empty);
                Assert.That(result.Failures.Single().Reason, Is.EqualTo("OUTPUT_WRITE_FAILED"));
            }
            finally
            {
                if (Directory.Exists(temporaryRoot)) Directory.Delete(temporaryRoot, true);
            }
        }

        [Test]
        public void FailureBundleRunnerDigestIsStableAcrossRepeatReverseCultureAttachmentAndWorkerOrder()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            try
            {
                var normal = GeneratedFailureBundleFactory.Create(Manifest(Chain()));
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                var reversed = GeneratedFailureBundleFactory.Create(Manifest(Chain(true), true));
                var repeated = GeneratedFailureBundleFactory.Create(Manifest(Chain()));
                Assert.That(normal.Payload.Manifest.ManifestDigest,
                    Is.EqualTo(reversed.Payload.Manifest.ManifestDigest));
                Assert.That(normal.Payload.PayloadDigest, Is.EqualTo(reversed.Payload.PayloadDigest));
                Assert.That(normal.Payload.PayloadDigest, Is.EqualTo(repeated.Payload.PayloadDigest));

                var forwardWorkers = GeneratedValidationRunnerPlanner.Partition(50, 7, 3)
                    .Select(value => value.StableToken).OrderBy(value => value).ToArray();
                var reverseWorkers = GeneratedValidationRunnerPlanner.Partition(50, 7, 3)
                    .Reverse().Select(value => value.StableToken).OrderBy(value => value).ToArray();
                Assert.That(forwardWorkers, Is.EqualTo(reverseWorkers));

                var mutated = GeneratedFailureBundleFactory.Create(Manifest(Chain(), seed: 732));
                Assert.That(mutated.Payload.Manifest.ManifestDigest,
                    Is.Not.EqualTo(normal.Payload.Manifest.ManifestDigest));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        [Test]
        public void Map19HandoffKeepsMap19_09Locked()
        {
            var temporaryRoot = NewTemporaryRoot();
            try
            {
                var output = Path.Combine(temporaryRoot, "bundle");
                var parsed = GeneratedValidationRunnerArgumentParser.Parse(Args("plan", output,
                    "--seed-start", "1", "--seed-count", "2"));
                var result = RunFocused(parsed.Arguments, Chain(), plan => Manifest(plan.Chain),
                    temporaryRoot);

                Assert.That(result.Success, Is.True, FailureText(result));
                Assert.That(BakingCanonicalDigest.IsLowerHexSha256(result.SuccessHandoffDigest), Is.True);
                Assert.That(result.Plan.PhaseOrder.Last(), Is.EqualTo("MAP19_07"));
                Assert.That(EditorRunnerType.GetMethods()
                    .Any(method => method.Name.IndexOf("StartMap19_09", StringComparison.Ordinal) >= 0), Is.False);
                AssertForbiddenTokenBoundaries();
            }
            finally
            {
                if (Directory.Exists(temporaryRoot)) Directory.Delete(temporaryRoot, true);
            }
        }

        private static GeneratedValidationChainSnapshot Chain(bool reverse = false,
            object[] owners = null, string digestOverride = null, string handoffOverride = null)
        {
            var phaseIds = GeneratedValidationChainSnapshot.RequiredPhaseIds.ToArray();
            var digests = ExpectedDigests();
            owners = owners ?? Enumerable.Range(0, phaseIds.Length).Select(_ => new object()).ToArray();
            var references = phaseIds.Select((phase, index) => new GeneratedValidationSurfaceReference(
                phase, owners[index], index == 5 && digestOverride != null ? digestOverride : digests[index])).ToArray();
            return new GeneratedValidationChainSnapshot(reverse ? references.Reverse() : references,
                handoffOverride ?? GeneratedValidationChainSnapshot.Map19_08IncomingHandoffDigest);
        }

        private static GeneratedFailureBundleManifest Manifest(GeneratedValidationChainSnapshot chain,
            bool reverse = false, string createdAt = "2026-09-05T00:00:00Z", bool omitFailureOwner = false,
            ulong seed = 731, bool missingIdentity = false, bool invalidCoordinate = false,
            bool invalidProvenance = false)
        {
            var passes = new[]
            {
                new GeneratedFailureBundlePassSnapshot("MAP19_02_GRAPH",
                    GeneratedFailureBundlePassDecision.Pass, "graph accepted",
                    GeneratedValidationChainSnapshot.Map19_02GraphDigest),
                new GeneratedFailureBundlePassSnapshot("MAP19_07_PACING",
                    GeneratedFailureBundlePassDecision.Pass, "measurement accepted",
                    GeneratedValidationChainSnapshot.Map19_07CombinedDigest),
            };
            var failures = new[]
            {
                new GeneratedFailureBundleFailureRecord(omitFailureOwner ? "" : "MAP19_06",
                    "SYNTHETIC_BLOCKED_EDGE", "BUNDLE-731", "EDGE-7", "OPEN", "BLOCKED",
                    GeneratedValidationChainSnapshot.Map19_06CombinedDigest),
            };
            var coordinates = new[]
            {
                new GeneratedFailureBundleCoordinateRecord(invalidCoordinate ? -1 : 2, 3, 1, 0, 12, 8,
                    "NODE-A", "EDGE-7", "MARKER-A", "WORST-A"),
                new GeneratedFailureBundleCoordinateRecord(1, 4, 0, 1, 3, 9,
                    "NODE-B", "EDGE-8", "MARKER-B", "WORST-B"),
            };
            var provenance = new[]
            {
                new GeneratedFailureBundleProvenanceRecord("MAP19_07", "measurement", "route-A",
                    invalidProvenance ? "MISSING" : GeneratedValidationChainSnapshot.Map19_07CombinedDigest),
                new GeneratedFailureBundleProvenanceRecord("MAP19_02", "graph", "world-A",
                    GeneratedValidationChainSnapshot.Map19_02GraphDigest),
            };
            var attachments = new[]
            {
                new GeneratedFailureBundleAttachmentReference("overview", "later/overview.png",
                    "sector:2,3", "MAP19_09"),
                new GeneratedFailureBundleAttachmentReference("detail", "later/detail.png",
                    "cell:12,8", "MAP19_09"),
            };
            if (reverse)
            {
                Array.Reverse(passes);
                Array.Reverse(failures);
                Array.Reverse(coordinates);
                Array.Reverse(provenance);
                Array.Reverse(attachments);
            }
            var saveHeader = SaveHeader(seed);
            return new GeneratedFailureBundleManifest("BUNDLE-731",
                missingIdentity ? null : GeneratedFailureBundleSeedIdentity.FromSaveManifest(seed,
                    GeneratedFailureBundleSourceKind.FocusedFixture, saveHeader),
                missingIdentity ? null : GeneratedFailureBundleVersionIdentity.FromSaveManifest(saveHeader.Version),
                "MAP19_08", "MAP19_06", passes, failures,
                coordinates, provenance, attachments, chain.BundleDigests(), createdAt);
        }

        private static GeneratedSaveManifestHeader SaveHeader(ulong seed)
        {
            var digest = Hash("SAVE-HEADER");
            return new GeneratedSaveManifestHeader(new GeneratedSaveManifestVersion(
                GeneratedSaveManifestService.SchemaVersion, "generator-19.8", "data-19.8"),
                seed.ToString(CultureInfo.InvariantCulture), digest, digest, digest, digest,
                digest, digest, 0);
        }

        private static string[] Args(string mode, string output, params string[] extra) =>
            new[] { "--mode", mode, "--output", output, "--profile", "focused",
                "--expected-generator-version", "generator-19.8",
                "--expected-data-version", "data-19.8" }.Concat(extra).ToArray();

        private static string[] ExpectedDigests() => new[]
        {
            GeneratedValidationChainSnapshot.Map19_02GraphDigest,
            GeneratedValidationChainSnapshot.Map19_03CompletionDigest,
            GeneratedValidationChainSnapshot.Map19_04CombinedDigest,
            GeneratedValidationChainSnapshot.Map19_05CombinedDigest,
            GeneratedValidationChainSnapshot.Map19_06CombinedDigest,
            GeneratedValidationChainSnapshot.Map19_07CombinedDigest,
        };

        private static string NewTemporaryRoot()
        {
            var root = Path.Combine(Path.GetTempPath(), "map19_08_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return root;
        }

        private static string Hash(string value) => BakingCanonicalDigest.HashCanonicalLines(new[] { value });
        private static string FailureText(GeneratedFailureBundleCreationResult result) => string.Join("\n",
            result.Failures.Select(value => value.StableToken));
        private static string FailureText(GeneratedValidationRunnerResult result) => string.Join("\n",
            result.Failures.Select(value => value.StableToken));
        private static string FailureText(EditorRunSnapshot result) => string.Join("\n",
            result.Failures.Select(value => value.StableToken));

        private static Type EditorRunnerType => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(
                "StarNight.Map.Editor.WorldGeneration.Validation.GeneratedHeadlessValidationRunner"))
            .Single(type => type != null);

        private static EditorRunSnapshot RunFocused(GeneratedValidationRunnerArguments arguments,
            GeneratedValidationChainSnapshot chain,
            Func<GeneratedValidationRunnerPlan, GeneratedFailureBundleManifest> callback,
            string temporaryRoot, Delegate writer = null)
        {
            var method = EditorRunnerType.GetMethod("RunFocusedDryRun",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            var value = method.Invoke(null, new object[]
                { arguments, chain, callback, temporaryRoot, writer });
            return new EditorRunSnapshot(value);
        }

        private static bool TryResolveSafeOutput(string output, string temporaryRoot,
            out string resolved, out string reason)
        {
            var method = EditorRunnerType.GetMethod("TryResolveSafeOutput",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            var arguments = new object[] { output, temporaryRoot, null, null };
            var success = (bool)method.Invoke(null, arguments);
            resolved = (string)arguments[2];
            reason = (string)arguments[3];
            return success;
        }

        private static Delegate CreateWriterDelegate(RejectingWriter target)
        {
            var type = EditorRunnerType.Assembly.GetType(
                "StarNight.Map.Editor.WorldGeneration.Validation.GeneratedValidationTextWriter", true);
            return Delegate.CreateDelegate(type, target, typeof(RejectingWriter).GetMethod(
                nameof(RejectingWriter.Write), BindingFlags.Public | BindingFlags.Instance));
        }

        private sealed class RejectingWriter
        {
            private int writes;
            public bool Write(string path, string text) => ++writes < 2;
        }

        private sealed class EditorRunSnapshot
        {
            public EditorRunSnapshot(object value)
            {
                Success = Property<bool>(value, "Success");
                ExitCode = Property<GeneratedValidationRunnerExitCode>(value, "ExitCode");
                Plan = Property<GeneratedValidationRunnerPlan>(value, "Plan");
                FilesWritten = Property<int>(value, "FilesWritten");
                SyntheticCallbacks = Property<int>(value, "SyntheticCallbacks");
                SuccessManifestDigest = Property<string>(value, "SuccessManifestDigest");
                SuccessPayloadDigest = Property<string>(value, "SuccessPayloadDigest");
                SuccessPlanDigest = Property<string>(value, "SuccessPlanDigest");
                SuccessHandoffDigest = Property<string>(value, "SuccessHandoffDigest");
                Failures = Property<IReadOnlyList<GeneratedValidationRunnerFailure>>(value, "Failures");
            }

            public bool Success { get; }
            public GeneratedValidationRunnerExitCode ExitCode { get; }
            public GeneratedValidationRunnerPlan Plan { get; }
            public int FilesWritten { get; }
            public int SyntheticCallbacks { get; }
            public string SuccessManifestDigest { get; }
            public string SuccessPayloadDigest { get; }
            public string SuccessPlanDigest { get; }
            public string SuccessHandoffDigest { get; }
            public IReadOnlyList<GeneratedValidationRunnerFailure> Failures { get; }

            private static T Property<T>(object value, string name) =>
                (T)value.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
                    .GetValue(value, null);
        }

        private static void AssertForbiddenTokenBoundaries()
        {
            var runtimeFiles = new[]
            {
                "Assets/_Game/Map/Runtime/WorldGeneration/Validation/GeneratedFailureBundle.cs",
                "Assets/_Game/Map/Runtime/WorldGeneration/Validation/GeneratedValidationRunnerPlan.cs",
            };
            var runtimeForbidden = new[] { "UnityEngine", "UnityEditor", "System.IO", "Physics2D",
                "GameObject", "Transform", "MonoBehaviour", "Tilemap", "Collider", "Rigidbody",
                "NavMesh", "Scene", "Prefab", "Camera", "Addressables", "Resources", "AssetDatabase",
                "PlayerPrefs", "PlayerController", "SetTile", "Instantiate", "Destroy", "Process.Start" };
            foreach (var file in runtimeFiles)
            {
                var source = File.ReadAllText(file);
                Assert.That(runtimeForbidden.Where(source.Contains), Is.Empty, file);
            }

            var editorSource = File.ReadAllText(
                "Assets/_Game/Editor/MapAuthoring/WorldGeneration/Validation/GeneratedHeadlessValidationRunner.cs");
            var editorForbidden = new[] { "Physics2D", "GameObject", "Transform", "MonoBehaviour",
                "Tilemap", "Collider", "Rigidbody", "NavMesh", "SceneManager", "PrefabUtility", "Camera",
                "Addressables", "Resources.Load", "AssetDatabase.Refresh", "PlayerPrefs", "PlayerController",
                "SetTile", "Instantiate", "Destroy", "Process.Start" };
            Assert.That(editorForbidden.Where(editorSource.Contains), Is.Empty);
        }
    }
}
