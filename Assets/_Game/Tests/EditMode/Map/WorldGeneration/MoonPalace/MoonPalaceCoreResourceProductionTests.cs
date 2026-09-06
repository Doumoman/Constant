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
    public sealed class MoonPalaceCoreResourceProductionTests
    {
        private const string CategoryName = "MAP21_07";
        private const string PublisherTypeName = "StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.MoonPalaceCoreResourcePublisher";

        [Test, Category(CategoryName)]
        public void MoonPalaceCoreResourcesContainExactThreeRegionsAndAuthoritativeKeys()
        {
            var value = Production(Sample());
            Assert.That(value.Profiles.Count, Is.EqualTo(3));
            Assert.That(value.Profiles.Select(x => x.RegionId).OrderBy(x => x),
                Is.EqualTo(MoonPalaceCoreResourceProduction.RequiredRegionIds.OrderBy(x => x)));
            AssertProfile(value, "SR_MOON_CORE_SITE_5", "MoonCore", "MoonCrater",
                "ImpactChain", "SR_STATE_MOON_CORE_SITE_5_REWARD_MOON_CORE_REWARD");
            AssertProfile(value, "SR_CASSIA_SAP_SITE_5", "CassiaSap", "CassiaRoot",
                "WaterChannel", "SR_STATE_CASSIA_SAP_SITE_5_REWARD_CASSIA_SAP_REWARD");
            AssertProfile(value, "SR_STAR_NURUK_SITE_5", "StarNuruk", "MoonDough",
                "FermentationPressure", "SR_STATE_STAR_NURUK_SITE_5_REWARD_STAR_NURUK_REWARD");
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceCoreResourcesPublishThirtySixBySixteenCanvasAndFiveActiveChunksEach()
        {
            var value = Production(Sample());
            Assert.That(value.Chunks.Count, Is.EqualTo(15));
            foreach (var profile in value.Profiles)
            {
                Assert.That(profile.DesignCanvasWidth, Is.EqualTo(36));
                Assert.That(profile.DesignCanvasHeight, Is.EqualTo(16));
                var chunks = value.Chunks.Where(x => x.RegionId == profile.RegionId).ToArray();
                Assert.That(chunks.Length, Is.EqualTo(5));
                Assert.That(chunks.Select(x => x.ChunkX + "/" + x.ChunkY).Distinct().Count(), Is.EqualTo(5));
                Assert.That(chunks.All(x => x.Active && x.Width == 12 && x.Height == 8 &&
                    x.LocalOriginX >= 0 && x.LocalOriginX < 36 && x.LocalOriginY >= 0 &&
                    x.LocalOriginY < 16), Is.True);
            }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceCoreResourceGraphsPreserveApprovedNodeEdgeAndRouteCounts()
        {
            var value = Production(Sample());
            Assert.That(value.Nodes.Count, Is.EqualTo(44));
            Assert.That(value.Edges.Count, Is.EqualTo(51));
            AssertCounts(value, "SR_MOON_CORE_SITE_5", 14, 16, 5, 8);
            AssertCounts(value, "SR_CASSIA_SAP_SITE_5", 15, 17, 7, 7);
            AssertCounts(value, "SR_STAR_NURUK_SITE_5", 15, 18, 8, 7);
            Assert.That(value.Edges.Count(x => x.RouteKind == "Failure"), Is.EqualTo(3));
            Assert.That(value.Edges.Count(x => x.RouteKind == "Recovery"), Is.EqualTo(6));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceCoreResourceLowRoutesAreMandatoryNoToolAndReachRewardAndReturn()
        {
            var value = Production(Sample());
            foreach (var profile in value.Profiles)
            {
                var nodes = value.Nodes.Where(x => x.RegionId == profile.RegionId).ToDictionary(x => x.NodeId);
                var route = value.Edges.Where(x => x.RegionId == profile.RegionId && x.RouteKind == "Low").OrderBy(x => x.Order).ToArray();
                Assert.That(route.All(x => x.Required && x.AccessClass == "MandatoryNoTool" && x.DependencyKind == "None"), Is.True);
                Assert.That(nodes[route[0].FromNodeId].NodeRole, Is.EqualTo("Entry"));
                Assert.That(nodes[route[route.Length - 1].ToNodeId].NodeRole, Is.EqualTo("Return"));
                Assert.That(route.Any(x => nodes[x.ToNodeId].NodeRole == "RequiredReward"), Is.True);
                Assert.That(IsChain(route), Is.True);
            }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceCoreResourceHighRoutesAreOptionalAndConvergeWithoutBlockingCompletion()
        {
            var value = Production(Sample());
            foreach (var profile in value.Profiles)
            {
                var nodes = value.Nodes.Where(x => x.RegionId == profile.RegionId).ToDictionary(x => x.NodeId);
                var high = value.Edges.Where(x => x.RegionId == profile.RegionId && x.RouteKind == "High").OrderBy(x => x.Order).ToArray();
                var lowNodes = new HashSet<string>(value.Edges.Where(x => x.RegionId == profile.RegionId && x.RouteKind == "Low").SelectMany(x => new[] { x.FromNodeId, x.ToNodeId }), StringComparer.Ordinal);
                Assert.That(high.All(x => !x.Required && x.AccessClass.StartsWith("Optional", StringComparison.Ordinal)), Is.True);
                Assert.That(IsChain(high), Is.True);
                Assert.That(high.Any(x => nodes[x.ToNodeId].NodeRole == "RequiredReward" || lowNodes.Contains(x.ToNodeId)), Is.True);
                Assert.That(value.Edges.Where(x => x.RegionId == profile.RegionId && x.RouteKind == "Low").Any(), Is.True);
            }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceCoreResourceFailureBranchesRecoverToExistingLowRecoveryJoin()
        {
            var value = Production(Sample());
            foreach (var profile in value.Profiles)
            {
                var nodes = value.Nodes.Where(x => x.RegionId == profile.RegionId).ToDictionary(x => x.NodeId);
                var failure = value.Edges.Single(x => x.RegionId == profile.RegionId && x.RouteKind == "Failure");
                var recovery = value.Edges.Where(x => x.RegionId == profile.RegionId && x.RouteKind == "Recovery").OrderBy(x => x.Order).ToArray();
                var lowNodes = new HashSet<string>(value.Edges.Where(x => x.RegionId == profile.RegionId && x.RouteKind == "Low").SelectMany(x => new[] { x.FromNodeId, x.ToNodeId }), StringComparer.Ordinal);
                Assert.That(nodes[failure.ToNodeId].NodeRole, Is.EqualTo("Failure"));
                Assert.That(recovery.Length, Is.EqualTo(2));
                Assert.That(recovery[0].FromNodeId, Is.EqualTo(failure.ToNodeId));
                Assert.That(nodes[recovery[1].ToNodeId].NodeRole, Is.EqualTo("RecoveryJoin"));
                Assert.That(lowNodes.Contains(recovery[1].ToNodeId), Is.True);
            }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceCoreResourceRewardsPublishExactOneRequiredAndTwoOptionalBenefitsEach()
        {
            var value = Production(Sample());
            Assert.That(value.Rewards.Count(x => x.Required), Is.EqualTo(3));
            Assert.That(value.Rewards.Count(x => !x.Required), Is.EqualTo(6));
            foreach (var profile in value.Profiles)
            {
                var records = value.Rewards.Where(x => x.RegionId == profile.RegionId).ToArray();
                Assert.That(records.Count(x => x.Required), Is.EqualTo(1));
                Assert.That(records.Count(x => !x.Required), Is.EqualTo(2));
                Assert.That(records.Single(x => x.Required).PersistenceKeyOrNone,
                    Is.EqualTo(profile.RequiredRewardPersistenceKey));
                Assert.That(records.Where(x => !x.Required).All(x => x.PersistenceKeyOrNone == "NONE"), Is.True);
            }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceCoreResourcePersistencePublishesSevenCheckpointsAndRejectsLegacyShortKeys()
        {
            var value = Production(Sample());
            Assert.That(value.Checkpoints.Count, Is.EqualTo(21));
            foreach (var profile in value.Profiles)
            {
                var records = value.Checkpoints.Where(x => x.RegionId == profile.RegionId).ToArray();
                Assert.That(records.Length, Is.EqualTo(7));
                Assert.That(records.Select(x => x.CheckpointKind).OrderBy(x => x),
                    Is.EqualTo(MoonPalaceCoreResourceProduction.RequiredCheckpointStates.OrderBy(x => x)));
                Assert.That(records.All(x => !x.DuplicateRisk && !x.PermanentLossRisk &&
                    x.RequiredRewardPersistenceKey == profile.RequiredRewardPersistenceKey), Is.True);
            }
            var original = value.Profiles.Single(x => x.ResourceKind == "MoonCore");
            var invalid = CopyProfile(original, biome: original.BiomeId,
                rewardKey: "SR_STATE_MOON_CORE_REWARD");
            Assert.Throws<ArgumentException>(() => Rebuild(value, profiles:
                value.Profiles.Where(x => x.RegionId != original.RegionId).Concat(new[] { invalid })));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceCoreResourcePublisherReadsMap13Map18AndMap21SourcesWithoutRewriting()
        {
            var sample = Sample();
            var paths = (IReadOnlyList<string>)Property(sample, "SourceReadRelativePaths");
            var before = paths.ToDictionary(path => path, FileSha, StringComparer.Ordinal);
            Sample(true);
            var after = paths.ToDictionary(path => path, FileSha, StringComparer.Ordinal);
            Assert.That(paths.Count, Is.EqualTo(9));
            Assert.That(after, Is.EqualTo(before));
            Assert.That(paths.Count(path => path.Contains("MAP13") || path.Contains("SpecialRegions")), Is.EqualTo(4));
            Assert.That(paths.Count(path => path.Contains("MAP18") || path.Contains("SpecialState")), Is.EqualTo(2));
            Assert.That(paths.Count(path => path.Contains("MAP21_04") || path.Contains("MAP21_05") || path.Contains("MAP21_06")), Is.EqualTo(3));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceCoreResourcePublisherWritesOnlyMap21_07AuthoringAndGeneratedRoots()
        {
            var sample = InvokePublisher("PublishAuthoringAndSamples", ProjectRoot, true);
            var paths = (IReadOnlyList<string>)Property(sample, "WrittenRelativePaths");
            var authoring = Constant("AuthoringDirectoryRelativePath") + "/";
            var generated = Constant("GeneratedDirectoryRelativePath") + "/";
            Assert.That(paths.Count, Is.EqualTo(10));
            Assert.That(paths.Count(path => path.StartsWith(authoring, StringComparison.Ordinal)), Is.EqualTo(6));
            Assert.That(paths.Count(path => path.StartsWith(generated, StringComparison.Ordinal)), Is.EqualTo(4));
            foreach (var path in paths)
            {
                var bytes = File.ReadAllBytes(Resolve(path));
                Assert.That(bytes.Take(3), Is.Not.EqualTo(new byte[] { 0xef, 0xbb, 0xbf }));
                var text = Encoding.UTF8.GetString(bytes);
                Assert.That(text.Contains("\r"), Is.False);
                Assert.That(text.EndsWith("\n", StringComparison.Ordinal), Is.True);
            }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceCoreResourceDigestsAreDeterministicReverseOrderRepeatAndCultureStable()
        {
            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR"); CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
                var leftSample = Sample(false); var left = Production(leftSample);
                CultureInfo.CurrentCulture = new CultureInfo("ko-KR"); CultureInfo.CurrentUICulture = new CultureInfo("ko-KR");
                var rightSample = Sample(true); var right = Production(rightSample);
                Assert.That(left.SerializeProfilesCsv(), Is.EqualTo(right.SerializeProfilesCsv()));
                Assert.That(left.SerializeChunksCsv(), Is.EqualTo(right.SerializeChunksCsv()));
                Assert.That(left.SerializeNodesCsv(), Is.EqualTo(right.SerializeNodesCsv()));
                Assert.That(left.SerializeEdgesCsv(), Is.EqualTo(right.SerializeEdgesCsv()));
                Assert.That(left.SerializeRewardsCsv(), Is.EqualTo(right.SerializeRewardsCsv()));
                Assert.That(left.SerializePersistenceCsv(), Is.EqualTo(right.SerializePersistenceCsv()));
                Assert.That(left.SerializeResourceManifest(), Is.EqualTo(right.SerializeResourceManifest()));
                Assert.That(left.SerializeRouteManifest(), Is.EqualTo(right.SerializeRouteManifest()));
                Assert.That(left.SerializePersistenceManifest(), Is.EqualTo(right.SerializePersistenceManifest()));
                Assert.That(DigestManifest(leftSample).Serialize(), Is.EqualTo(DigestManifest(rightSample).Serialize()));
            }
            finally { CultureInfo.CurrentCulture = previousCulture; CultureInfo.CurrentUICulture = previousUiCulture; }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceCoreResourceRejectsDuplicateIdsBadCanvasBadGraphMissingRewardWrongBiomeAndRuntimeSideEffects()
        {
            var value = Production(Sample());
            Assert.Throws<ArgumentException>(() => Rebuild(value, profiles: value.Profiles.Concat(new[] { value.Profiles[0] })));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MoonPalaceCoreResourceChunk("SR_MOON_CORE_SITE_5", "BAD", 3, 0));
            Assert.Throws<ArgumentException>(() => Rebuild(value, edges: value.Edges.Skip(1)));
            Assert.Throws<ArgumentException>(() => Rebuild(value, rewards: value.Rewards.Where(x => !x.Required || x.RegionId != "SR_MOON_CORE_SITE_5")));
            var profile = value.Profiles[0];
            var wrongBiome = CopyProfile(profile, "WrongBiome", profile.RequiredRewardPersistenceKey);
            Assert.Throws<ArgumentException>(() => Rebuild(value, profiles: value.Profiles.Where(x => x.RegionId != profile.RegionId).Concat(new[] { wrongBiome })));
            Assert.That(value.CanExecuteRuntimeSideEffects, Is.False);
            Assert.Throws<InvalidOperationException>(() => value.RequestRuntimeExecution());
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceCoreResourceDoesNotRunGenerationRendererValidationReplayRollbackPlayModeOrLegacyRegression()
        {
            var counters = (MoonPalaceCoreResourceForbiddenOperationCounters)Property(Sample(), "Counters");
            Assert.That(counters.AllZero, Is.True);
            Assert.That(counters.DeviceMonoBehaviours + counters.PhysicsSimulations + counters.RewardGrants +
                counters.InventoryMutations + counters.SaveFileWrites + counters.SaveFileReads +
                counters.PlayerPrefsWrites + counters.PlayerPrefsReads + counters.WorldSectorPlacements +
                counters.GenerationRunnerExecutions + counters.RendererExecutions + counters.ValidationRunnerExecutions +
                counters.ReplayExecutions + counters.RollbackExecutions + counters.TilemapWrites +
                counters.RuntimeObjectSpawns + counters.ScenePrefabChanges + counters.ColliderAddressablesChanges +
                counters.PriorCategorySelections + counters.PlayModeSelections + counters.LegacyRegressionSelections +
                counters.UnfilteredOrFullRegressionSelections + counters.UpstreamRegenerationRuns, Is.Zero);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceCoreResourcePublishesMap21_08HandoffOnlyAfterFocusedPass()
        {
            var blocked = Assert.Throws<TargetInvocationException>(() => InvokePublisher("PublishAuthoringAndSamples", ProjectRoot, false));
            Assert.That(blocked.InnerException, Is.TypeOf<InvalidOperationException>());
            var sample = InvokePublisher("PublishAuthoringAndSamples", ProjectRoot, true);
            var manifest = DigestManifest(sample);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(manifest.Map2108HandoffDigest), Is.True);
            Assert.That(manifest.CsvDigests.Count, Is.EqualTo(6));
            Assert.That(manifest.JsonDigests.Count, Is.EqualTo(3));
            Assert.That(File.Exists(Resolve(Constant("GeneratedDirectoryRelativePath") + "/" + Constant("DigestManifestFileName"))), Is.True);
        }

        private static void AssertProfile(MoonPalaceCoreResourceProduction value,
            string region, string resource, string biome, string mechanism, string key)
        { var profile = value.Profiles.Single(x => x.RegionId == region); Assert.That(profile.ResourceKind, Is.EqualTo(resource)); Assert.That(profile.BiomeId, Is.EqualTo(biome)); Assert.That(profile.MechanismKind, Is.EqualTo(mechanism)); Assert.That(profile.RequiredRewardPersistenceKey, Is.EqualTo(key)); }
        private static void AssertCounts(MoonPalaceCoreResourceProduction value, string region,
            int nodes, int edges, int low, int high)
        { Assert.That(value.Nodes.Count(x => x.RegionId == region), Is.EqualTo(nodes)); Assert.That(value.Edges.Count(x => x.RegionId == region), Is.EqualTo(edges)); Assert.That(value.Edges.Count(x => x.RegionId == region && x.RouteKind == "Low"), Is.EqualTo(low)); Assert.That(value.Edges.Count(x => x.RegionId == region && x.RouteKind == "High"), Is.EqualTo(high)); }
        private static bool IsChain(IReadOnlyList<MoonPalaceCoreResourceEdge> values) { for (var index = 1; index < values.Count; index++) if (values[index - 1].ToNodeId != values[index].FromNodeId) return false; return true; }
        private static MoonPalaceCoreResourceProfile CopyProfile(MoonPalaceCoreResourceProfile value,
            string biome, string rewardKey) => new MoonPalaceCoreResourceProfile(value.RegionId,
                value.ResourceKind, biome, value.MechanismKind, value.LowRouteId, value.HighRouteId,
                value.FailureBranchId, value.RecoveryRouteId, value.RequiredRewardSlotId,
                rewardKey, value.ChunkDigest, value.GraphDigest, value.RewardDigest, value.PersistenceDigest);
        private static MoonPalaceCoreResourceProduction Rebuild(MoonPalaceCoreResourceProduction value,
            IEnumerable<MoonPalaceCoreResourceProfile> profiles = null,
            IEnumerable<MoonPalaceCoreResourceEdge> edges = null,
            IEnumerable<MoonPalaceCoreResourceReward> rewards = null) => new MoonPalaceCoreResourceProduction(
                profiles ?? value.Profiles, value.Chunks, value.Nodes, edges ?? value.Edges,
                rewards ?? value.Rewards, value.Checkpoints, value.CreatedUtc);
        private static object Sample(bool reverse = false) => InvokePublisher("CreateReadOnlySample", ProjectRoot, "2026-09-06T00:00:00.0000000Z", reverse);
        private static MoonPalaceCoreResourceProduction Production(object sample) => (MoonPalaceCoreResourceProduction)Property(sample, "Production");
        private static MoonPalaceCoreResourceDigestManifest DigestManifest(object sample) => (MoonPalaceCoreResourceDigestManifest)Property(sample, "DigestManifest");
        private static object Property(object instance, string name) => instance.GetType().GetProperty(name).GetValue(instance);
        private static object InvokePublisher(string methodName, params object[] arguments) => PublisherType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static).Invoke(null, arguments);
        private static string Constant(string name) => (string)PublisherType.GetField(name).GetRawConstantValue();
        private static Type PublisherType => Assembly.Load("MapAuthoring.Editor").GetType(PublisherTypeName, true);
        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        private static string Resolve(string path) => Path.Combine(ProjectRoot, path.Replace('/', Path.DirectorySeparatorChar));
        private static string FileSha(string path) { using (var stream = File.OpenRead(Resolve(path))) using (var sha = SHA256.Create()) return string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("x2", CultureInfo.InvariantCulture))); }
    }
}
