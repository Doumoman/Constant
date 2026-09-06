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
    public sealed class MoonPalaceLandmarkProductionTests
    {
        private const string CategoryName = "MAP21_09";
        private const string PublisherTypeName = "StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.MoonPalaceLandmarkPublisher";

        [Test, Category(CategoryName)]
        public void MoonPalaceLandmarksContainExactForgeBossMerchantMaruProfiles()
        {
            var value = Production(Sample());
            Assert.That(value.Profiles.Count, Is.EqualTo(4));
            Assert.That(value.Profiles.Select(x => x.LandmarkId).OrderBy(x => x),
                Is.EqualTo(MoonPalaceLandmarkProduction.RequiredLandmarkIds.OrderBy(x => x)));
            AssertProfile(value, "MoonSealForge", "PlacedMandatorySite", 9, 13, 22, 6, 14, 9, 3, 12);
            AssertProfile(value, "BossSealArena", "PlacedMandatorySite", 12, 12, 16, 5, 4, 4, 3, 7);
            AssertProfile(value, "WanderingMerchantCave", "DeferredOptionalLocal", 3, 7, 8, 3, 3, 2, 1, 7);
            AssertProfile(value, "MaruTimeShrine", "DeferredOptionalLocal", 5, 7, 12, 4, 4, 3, 2, 5);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceLandmarksPreserveMap13CountsBindingsAndDesignLocalCoordinates()
        {
            var value = Production(Sample());
            Assert.That(value.Chunks.Count, Is.EqualTo(29));
            Assert.That(value.Nodes.Count, Is.EqualTo(39));
            Assert.That(value.Edges.Count, Is.EqualTo(58));
            Assert.That(value.Routes.Count, Is.EqualTo(18));
            Assert.That(value.States.Count, Is.EqualTo(25));
            Assert.That(value.Transitions.Count, Is.EqualTo(18));
            Assert.That(value.Resets.Count, Is.EqualTo(9));
            Assert.That(value.Markers.Count, Is.EqualTo(31));
            Assert.That(value.Profiles.Count(x => x.Binding == "PlacedMandatorySite"), Is.EqualTo(2));
            Assert.That(value.Profiles.Count(x => x.Binding == "DeferredOptionalLocal"), Is.EqualTo(2));
            foreach (var node in value.Nodes)
            {
                var profile = value.Profiles.Single(x => x.LandmarkId == node.LandmarkId);
                Assert.That(node.LocalX, Is.InRange(0, profile.DesignWidth - 1));
                Assert.That(node.LocalY, Is.InRange(0, profile.DesignHeight - 1));
            }
            Assert.That(value.Profiles.Where(x => x.Binding == "DeferredOptionalLocal").All(x => !x.HasAnyPlacementClaim), Is.True);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceForgePublishesThreeResourceLedgerAndMoonSealReward()
        {
            var value = Production(Sample());
            Assert.That(value.ForgeLedgers.Select(x => x.Resource), Is.EqualTo(new[] { "CassiaSap", "MoonCore", "StarNuruk" }));
            Assert.That(value.ForgeLedgers.Select(x => x.SourceRewardKey), Does.Contain("SR_STATE_MOON_CORE_SITE_5_REWARD_MOON_CORE_REWARD"));
            Assert.That(value.ForgeLedgers.Select(x => x.SourceRewardKey), Does.Contain("SR_STATE_CASSIA_SAP_SITE_5_REWARD_CASSIA_SAP_REWARD"));
            Assert.That(value.ForgeLedgers.Select(x => x.SourceRewardKey), Does.Contain("SR_STATE_STAR_NURUK_SITE_5_REWARD_STAR_NURUK_REWARD"));
            Assert.That(MoonPalaceLandmarkPreconditions.MoonSealSlot, Is.EqualTo("SR_SLOT_MOON_SEAL_REWARD"));
            Assert.That(MoonPalaceLandmarkPreconditions.MoonSealRewardKey, Is.EqualTo("SR_STATE_MOON_SEAL_FORGE_9_REWARD_MOON_SEAL_REWARD"));
            Assert.That(MoonPalaceLandmarkProduction.ForgeProcessStages,
                Is.EqualTo(new[] { "Grind", "Mix", "Press", "MoonlightCure", "MoonSeal" }));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceForgeFailureReturnsAllInputsWithoutInventoryOrSaveMutation()
        {
            var value = Production(Sample());
            Assert.That(value.ForgeLedgers.Count, Is.EqualTo(3));
            Assert.That(value.ForgeLedgers.Count(x => x.ReturnsAllForgeInputs), Is.EqualTo(3));
            Assert.That(value.ForgeLedgers.Sum(x => x.PartialLossCount), Is.Zero);
            Assert.That(value.ForgeLedgers.Sum(x => x.PermanentLossCount), Is.Zero);
            Assert.That(value.Transitions.Count(x => x.LandmarkId == "MoonSealForge"), Is.EqualTo(9));
            Assert.That(value.Resets.Count(x => x.LandmarkId == "MoonSealForge" && x.Policy == "ManualReset" && x.ReturnsAllForgeInputs), Is.EqualTo(3));
            var counters = Counters(Sample());
            Assert.That(counters.ItemConsumes + counters.RewardGrants + counters.InventoryMutations +
                counters.ResourceMutations + counters.SaveFileWrites + counters.SaveFileReads, Is.Zero);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceBossGateAcceptsMoonSealMarkerWithoutConsumingInventory()
        {
            var value = Production(Sample());
            var roles = value.States.Where(x => x.LandmarkId == "BossSealArena").OrderBy(x => x.Order).Select(x => x.Role);
            Assert.That(roles, Is.EqualTo(new[] { "GateLocked", "GateAccepted", "EncounterActive", "Defeated" }));
            var accept = value.Transitions.Single(x => x.LandmarkId == "BossSealArena" && x.Trigger == "PresentMoonSeal");
            Assert.That(accept.FromStateId, Is.EqualTo("SL_STATE_BOSS_GATE_LOCKED"));
            Assert.That(accept.ToStateId, Is.EqualTo("SL_STATE_BOSS_GATE_ACCEPTED"));
            Assert.That(value.Markers.Count(x => x.LandmarkId == "BossSealArena" && x.Kind == "MoonSealRequirement"), Is.EqualTo(1));
            Assert.That(Counters(Sample()).ItemConsumes + Counters(Sample()).InventoryMutations, Is.Zero);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceBossPublishesEncounterRecoveryAndNoNewMovementRule()
        {
            var value = Production(Sample());
            var resets = value.Resets.Where(x => x.LandmarkId == "BossSealArena").ToArray();
            Assert.That(resets.Count(x => x.Policy == "SafeReturn" && x.RecoveryNodeId == "SL_NODE_BOSS_CENTRAL_RECOVERY"), Is.EqualTo(2));
            Assert.That(resets.Count(x => x.Policy == "EncounterReset" && x.FromStateId == "SL_STATE_BOSS_ENCOUNTER_ACTIVE" &&
                x.ToStateId == "SL_STATE_BOSS_ENCOUNTER_ACTIVE" && x.PreservesSealAcceptance), Is.EqualTo(1));
            Assert.That(value.Markers.Count(x => x.Kind == "FallingObject" || x.Kind == "PressureDevice"), Is.EqualTo(2));
            var counters = Counters(Sample());
            Assert.That(counters.BossAiExecutions + counters.CombatExecutions + counters.PhysicsExecutions, Is.Zero);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceMerchantRemainsDeferredLocalWithFourStaticVariants()
        {
            var value = Production(Sample());
            var profile = value.Profiles.Single(x => x.LandmarkId == "WanderingMerchantCave");
            Assert.That(profile.Binding, Is.EqualTo("DeferredOptionalLocal"));
            Assert.That(profile.DesignWidth, Is.EqualTo(24));
            Assert.That(profile.DesignHeight, Is.EqualTo(16));
            Assert.That(profile.HasAnyPlacementClaim, Is.False);
            Assert.That(value.OptionalVariants.Where(x => x.RecordKind == "MerchantVariant").OrderBy(x => x.Order).Select(x => x.Value),
                Is.EqualTo(new[] { "Alien", "Rabbit", "Spacefarer", "Machine" }));
            Assert.That(value.Markers.Count(x => x.LandmarkId == profile.LandmarkId && x.Kind == "ShopSafeZone"), Is.EqualTo(1));
            Assert.That(value.Markers.Count(x => x.LandmarkId == profile.LandmarkId && x.Kind == "EntranceCue"), Is.EqualTo(2));
            Assert.That(Counters(Sample()).ShopTransactions, Is.Zero);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceMaruPublishesPersistentChoiceWithoutRerollOrRuntimeSearch()
        {
            var value = Production(Sample());
            var choices = value.OptionalVariants.Where(x => x.LandmarkId == "MaruTimeShrine").OrderBy(x => x.Order).ToArray();
            Assert.That(choices.Select(x => x.Value), Is.EqualTo(new[] { "Ignored", "ShortHint", "StrongHint" }));
            Assert.That(choices.All(x => x.PersistentChoice && x.PreventsReroll && x.StaticPayloadOnly), Is.True);
            Assert.That(value.Markers.Count(x => x.LandmarkId == "MaruTimeShrine" && x.Kind == "ChoicePreview" && x.Order == 0), Is.EqualTo(1));
            Assert.That(value.Markers.Count(x => x.LandmarkId == "MaruTimeShrine" &&
                (x.Kind == "RareTerrainCompass" || x.Kind == "MaruAttentionIncrease")), Is.EqualTo(2));
            Assert.That(value.Resets.Count(x => x.LandmarkId == "MaruTimeShrine" && x.Policy == "PersistentChoice" && x.PreventsReroll), Is.EqualTo(1));
            Assert.That(Counters(Sample()).MaruRuntimeSearchExecutions, Is.Zero);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceLandmarkPublisherReadsSourcesWithoutRewriting()
        {
            var sample = Sample();
            var paths = SourcePaths(sample);
            var before = paths.ToDictionary(x => x, FileSha, StringComparer.Ordinal);
            var reverse = Sample(true);
            var after = paths.ToDictionary(x => x, FileSha, StringComparer.Ordinal);
            Assert.That(paths.Count, Is.EqualTo(15));
            Assert.That(after, Is.EqualTo(before));
            var observed = Digest(reverse).ObservedSourceResultDigests.ToDictionary(x => x.Name, x => x.Digest);
            Assert.That(observed["MAP13_07_RESULT"], Is.EqualTo("6098f22f0eab0f05342ef228edfdfea8039e37d86c957c3c6706d71d476f0ee9"));
            Assert.That(observed["MAP13_08_RESULT"], Is.EqualTo("fbf5c3181791cae9b25e92ed76d8e46828330a9b147ec82a246f7ea056664534"));
            Assert.That(observed["MAP13_09_RESULT"], Is.EqualTo("637fec406f42bf845be5ae9313a036b3ec49f66467539a3552c1f94ad68bd5e2"));
            Assert.That(observed["MAP18_06_RESULT"], Is.EqualTo("ad2b88be043cb7e18289909a7ad44d76c9143a65e5228c11c24f1a60b86831fd"));
            Assert.That(observed["MAP21_07_RESULT"], Is.EqualTo("68170bda01e3f2df4fbf14e9e841b04be58b3b61b1b18de9af3e5e2a3a486c8b"));
            Assert.That(observed["MAP21_08_RESULT"], Is.EqualTo(MoonPalaceLandmarkPreconditions.StrictMap2108ResultDigest));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceLandmarkPublisherWritesOnlyMap21_09AuthoringAndGeneratedRoots()
        {
            var sample = InvokePublisher("PublishAuthoringAndSamples", ProjectRoot, true);
            var paths = WrittenPaths(sample);
            var authoring = Constant("AuthoringDirectoryRelativePath") + "/";
            var generated = Constant("GeneratedDirectoryRelativePath") + "/";
            Assert.That(paths.Count, Is.EqualTo(15));
            Assert.That(paths.Count(x => x.StartsWith(authoring, StringComparison.Ordinal)), Is.EqualTo(10));
            Assert.That(paths.Count(x => x.StartsWith(generated, StringComparison.Ordinal)), Is.EqualTo(5));
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
        public void MoonPalaceLandmarkDigestsAreDeterministicReverseOrderRepeatAndCultureStable()
        {
            var culture = CultureInfo.CurrentCulture; var ui = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR"); CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var leftSample = Sample(false); var left = Production(leftSample);
                CultureInfo.CurrentCulture = new CultureInfo("ko-KR"); CultureInfo.CurrentUICulture = new CultureInfo("ko-KR");
                var rightSample = Sample(true); var right = Production(rightSample);
                Assert.That(AllCsv(left), Is.EqualTo(AllCsv(right)));
                Assert.That(AllJson(left), Is.EqualTo(AllJson(right)));
                Assert.That(Digest(leftSample).Serialize(), Is.EqualTo(Digest(rightSample).Serialize()));
                Assert.That(Digest(leftSample).CanonicalDigest, Is.EqualTo(Digest(rightSample).CanonicalDigest));
            }
            finally { CultureInfo.CurrentCulture = culture; CultureInfo.CurrentUICulture = ui; }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceLandmarksRejectBadResourceKeysGateOrderOptionalClaimsAndRuntimeSideEffects()
        {
            var value = Production(Sample());
            var ledger = value.ForgeLedgers[0];
            var badLedger = new MoonPalaceForgeResourceLedger(ledger.Resource, "BAD_KEY", ledger.AvailableStateId,
                ledger.ReservedStateId, ledger.ConsumedStateId, ledger.ReturnedStateId, ledger.FailureBranch, true);
            Assert.Throws<ArgumentException>(() => Rebuild(value, ledgers: value.ForgeLedgers.Where(x => x != ledger).Concat(new[] { badLedger })));
            var boss = value.States.Single(x => x.LandmarkId == "BossSealArena" && x.Role == "GateLocked");
            var badBoss = new MoonPalaceLandmarkState(boss.LandmarkId, boss.StateId, boss.Role, 3, boss.Persistent);
            Assert.Throws<ArgumentException>(() => Rebuild(value, states: value.States.Where(x => x != boss).Concat(new[] { badBoss })));
            var optional = value.Profiles.Single(x => x.LandmarkId == "MaruTimeShrine");
            var badOptional = new MoonPalaceLandmarkProfile(optional.LandmarkId, optional.RegionId, optional.Binding,
                optional.DesignWidth, optional.DesignHeight, optional.ChunkCount, optional.NodeCount, optional.EdgeCount,
                optional.RouteCount, optional.StateCount, optional.TransitionCount, optional.ResetCount, optional.MarkerCount,
                worldOriginClaim: true);
            Assert.Throws<ArgumentException>(() => Rebuild(value, profiles: value.Profiles.Where(x => x != optional).Concat(new[] { badOptional })));
            Assert.That(value.CanExecuteRuntimeSideEffects, Is.False);
            Assert.Throws<InvalidOperationException>(() => value.RequestRuntimeExecution());
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceLandmarksDoNotRunGenerationRendererValidationReplayRollbackPlayModeOrLegacyRegression()
        {
            var counters = Counters(Sample());
            Assert.That(counters.AllZero, Is.True);
            Assert.That(counters.GenerationRunnerExecutions + counters.RendererExecutions + counters.ValidationRunnerExecutions +
                counters.ReplayExecutions + counters.RollbackExecutions + counters.TilemapWrites + counters.RuntimeObjectSpawns +
                counters.ScenePrefabChanges + counters.ColliderAddressablesChanges + counters.PriorCategorySelections +
                counters.PlayModeSelections + counters.LegacyRegressionSelections + counters.UnfilteredOrFullRegressionSelections +
                counters.UpstreamRegenerationRuns, Is.Zero);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceLandmarksPublishMap21_10HandoffOnlyAfterFocusedPass()
        {
            var blocked = Assert.Throws<TargetInvocationException>(() => InvokePublisher(
                "PublishAuthoringAndSamples", ProjectRoot, false));
            Assert.That(blocked.InnerException, Is.TypeOf<InvalidOperationException>());
            var sample = InvokePublisher("PublishAuthoringAndSamples", ProjectRoot, true);
            var manifest = Digest(sample);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(manifest.Map2110HandoffDigest), Is.True);
            Assert.That(manifest.ObservedSourceResultDigests.Count, Is.EqualTo(6));
            Assert.That(manifest.CsvDigests.Count, Is.EqualTo(10));
            Assert.That(manifest.JsonDigests.Count, Is.EqualTo(4));
            Assert.That(File.Exists(Resolve(Constant("GeneratedDirectoryRelativePath") + "/" + Constant("DigestManifestFileName"))), Is.True);
        }

        private static void AssertProfile(MoonPalaceLandmarkProduction value, string id, string binding,
            int chunks, int nodes, int edges, int routes, int states, int transitions, int resets, int markers)
        {
            var profile = value.Profiles.Single(x => x.LandmarkId == id);
            Assert.That(profile.Binding, Is.EqualTo(binding)); Assert.That(profile.ChunkCount, Is.EqualTo(chunks));
            Assert.That(profile.NodeCount, Is.EqualTo(nodes)); Assert.That(profile.EdgeCount, Is.EqualTo(edges));
            Assert.That(profile.RouteCount, Is.EqualTo(routes)); Assert.That(profile.StateCount, Is.EqualTo(states));
            Assert.That(profile.TransitionCount, Is.EqualTo(transitions)); Assert.That(profile.ResetCount, Is.EqualTo(resets));
            Assert.That(profile.MarkerCount, Is.EqualTo(markers));
        }
        private static string AllCsv(MoonPalaceLandmarkProduction x) => string.Join("|", new[] { x.SerializeProfilesCsv(), x.SerializeChunksCsv(), x.SerializeNodesCsv(), x.SerializeEdgesCsv(), x.SerializeRoutesCsv(), x.SerializeForgeLedgerCsv(), x.SerializeBossGateEncounterCsv(), x.SerializeOptionalLandmarksCsv(), x.SerializeMarkersCsv(), x.SerializeStatePersistenceCsv() });
        private static string AllJson(MoonPalaceLandmarkProduction x) => string.Join("|", new[] { x.SerializeLandmarkManifest(), x.SerializeForgeManifest(), x.SerializeBossManifest(), x.SerializeOptionalManifest() });
        private static MoonPalaceLandmarkProduction Rebuild(MoonPalaceLandmarkProduction x,
            IEnumerable<MoonPalaceLandmarkProfile> profiles = null, IEnumerable<MoonPalaceLandmarkState> states = null,
            IEnumerable<MoonPalaceForgeResourceLedger> ledgers = null) => new MoonPalaceLandmarkProduction(
                profiles ?? x.Profiles, x.Chunks, x.Nodes, x.Edges, x.Routes, states ?? x.States,
                x.Transitions, x.Resets, x.Markers, ledgers ?? x.ForgeLedgers, x.OptionalVariants, x.CreatedUtc);
        private static object Sample(bool reverse = false) => InvokePublisher("CreateReadOnlySample",
            ProjectRoot, "2026-09-07T00:00:00.0000000Z", reverse);
        private static MoonPalaceLandmarkProduction Production(object sample) =>
            (MoonPalaceLandmarkProduction)Property(sample, "Production");
        private static MoonPalaceLandmarkDigestManifest Digest(object sample) =>
            (MoonPalaceLandmarkDigestManifest)Property(sample, "DigestManifest");
        private static MoonPalaceLandmarkForbiddenOperationCounters Counters(object sample) =>
            (MoonPalaceLandmarkForbiddenOperationCounters)Property(sample, "Counters");
        private static IReadOnlyList<string> WrittenPaths(object sample) => (IReadOnlyList<string>)Property(sample, "WrittenRelativePaths");
        private static IReadOnlyList<string> SourcePaths(object sample) => (IReadOnlyList<string>)Property(sample, "SourceReadRelativePaths");
        private static object Property(object instance, string name) => instance.GetType().GetProperty(name).GetValue(instance);
        private static object InvokePublisher(string name, params object[] args) => PublisherType.GetMethod(name, BindingFlags.Public | BindingFlags.Static).Invoke(null, args);
        private static string Constant(string name) => (string)PublisherType.GetField(name).GetRawConstantValue();
        private static Type PublisherType => Assembly.Load("MapAuthoring.Editor").GetType(PublisherTypeName, true);
        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        private static string Resolve(string path) => Path.Combine(ProjectRoot, path.Replace('/', Path.DirectorySeparatorChar));
        private static string FileSha(string path) { using (var stream = File.OpenRead(Resolve(path))) using (var sha = SHA256.Create()) return string.Concat(sha.ComputeHash(stream).Select(x => x.ToString("x2", CultureInfo.InvariantCulture))); }
    }
}
