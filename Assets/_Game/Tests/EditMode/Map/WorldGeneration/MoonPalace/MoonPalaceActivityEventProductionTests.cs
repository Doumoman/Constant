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

namespace StarNight.Map.Tests.WorldGeneration.MoonPalace
{
    public sealed class MoonPalaceActivityEventProductionTests
    {
        private const string CategoryName = "MAP21_05";
        private const string PublisherTypeName =
            "StarNight.MapAuthoring.Editor.WorldGeneration.MoonPalace.MoonPalaceActivityEventPublisher";

        [Test, Category(CategoryName)]
        public void MoonPalaceActivitiesContainExactSevenMap12IdsAndNoInventedActivityTypes()
        {
            var production = Production(Sample());
            var expectedKinds = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "ACT_CRATER_BOULDER_CHAIN", "BoulderChain" },
                { "ACT_CRATER_RICOCHET_MINE", "RicochetMine" },
                { "ACT_DOUGH_TIME_TRIAL", "TimeTrial" },
                { "ACT_MARU_REWIND_ANOMALY", "MaruRewindAnomaly" },
                { "ACT_MILL_ESCORT_CART", "EscortCart" },
                { "ACT_MILL_GEAR_GRID", "GearGrid" },
                { "ACT_MILL_PESTLE_WORKSHOP", "PestleWorkshop" },
            };

            Assert.That(production.Activities, Has.Count.EqualTo(7));
            Assert.That(production.Activities.Select(value => value.ActivityId),
                Is.EquivalentTo(expectedKinds.Keys));
            Assert.That(production.Activities.All(value =>
                value.SourceActivityId == value.ActivityId &&
                expectedKinds[value.ActivityId] == value.ActivityKind), Is.True);
            Assert.That(production.Activities.Any(value => value.BiomeId == "CassiaRoot"),
                Is.False);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceActivitiesBindOnlyToExistingSameBiomeTerrainProductionClusters()
        {
            var production = Production(Sample());
            var clusters = production.Clusters.ToDictionary(value => value.ClusterId,
                StringComparer.Ordinal);
            var expected = new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                { "ACT_CRATER_BOULDER_CHAIN", new[] { "TC_CRATER_PROD_ROCK_SHELF_CHAIN", "TC_CRATER_PROD_FALL_RECOVERY" } },
                { "ACT_CRATER_RICOCHET_MINE", new[] { "TC_CRATER_PROD_BROKEN_SLOPE", "TC_CRATER_PROD_BOWL_CROSS" } },
                { "ACT_DOUGH_TIME_TRIAL", new[] { "TC_DOUGH_PROD_BOUNCE_CUP", "TC_DOUGH_PROD_SQUISH_CROSS" } },
                { "ACT_MARU_REWIND_ANOMALY", new[] { "TC_DOUGH_PROD_RECOVERY_PAD_CHAIN", "TC_DOUGH_PROD_STICKY_SHELF" } },
                { "ACT_MILL_ESCORT_CART", new[] { "TC_MILL_PROD_BEAM_OVERHANG", "TC_MILL_PROD_RUST_LEDGE" } },
                { "ACT_MILL_GEAR_GRID", new[] { "TC_MILL_PROD_GEAR_GALLERY", "TC_MILL_PROD_BROKEN_PILLAR" } },
                { "ACT_MILL_PESTLE_WORKSHOP", new[] { "TC_MILL_PROD_ORTHOGONAL_SHAFT", "TC_MILL_PROD_FALL_RECOVERY" } },
            };

            Assert.That(production.Clusters, Has.Count.EqualTo(48));
            Assert.That(production.Bindings, Has.Count.EqualTo(7));
            foreach (var binding in production.Bindings)
            {
                Assert.That(new[] { binding.PrimaryClusterId, binding.FallbackClusterId },
                    Is.EqualTo(expected[binding.ActivityId]));
                Assert.That(clusters[binding.PrimaryClusterId].BiomeId,
                    Is.EqualTo(binding.BiomeId));
                Assert.That(clusters[binding.FallbackClusterId].BiomeId,
                    Is.EqualTo(binding.BiomeId));
                Assert.That(clusters[binding.PrimaryClusterId].PoolKind, Is.EqualTo("Terrain"));
                Assert.That(clusters[binding.FallbackClusterId].PoolKind, Is.EqualTo("Terrain"));
            }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceActivitySlotsPreserveCueActivationRewardRecoveryResetAndRemovalProofs()
        {
            var production = Production(Sample());
            Assert.That(production.Slots, Has.Count.EqualTo(66));
            foreach (var activity in production.Activities)
            {
                var semantics = production.Slots.Where(value =>
                    value.ActivityId == activity.ActivityId).Select(value =>
                    value.SlotSemantic).ToArray();
                Assert.That(semantics, Does.Contain("Cue"));
                Assert.That(semantics, Does.Contain("Trigger"));
                Assert.That(semantics, Does.Contain("Device"));
                Assert.That(semantics, Does.Contain("Reward"));
                Assert.That(semantics, Does.Contain("SafePocket"));
                Assert.That(semantics, Does.Contain("Recovery"));
                Assert.That(semantics, Does.Contain("Reset"));
                Assert.That(semantics, Does.Contain("ExitPreservation"));
            }
            Assert.That(production.Slots.Count(value => value.SlotSemantic == "Npc"),
                Is.EqualTo(1));
            Assert.That(production.Slots.Count(value => value.SlotSemantic == "Projectile"),
                Is.EqualTo(2));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceActivityRemovalSafetyPreservesStaticShellCriticalTargetsAndRecovery()
        {
            var production = Production(Sample());
            Assert.That(production.Removals, Has.Count.EqualTo(7));
            Assert.That(production.Removals.All(value =>
                value.StaticShellDigestBeforeRemoval == value.StaticShellDigestAfterRemoval &&
                value.CriticalTargetDigestBeforeRemoval ==
                    value.CriticalTargetDigestAfterRemoval &&
                value.SafePocketMarkerCount >= 1 && value.RecoveryMarkerCount >= 1 &&
                value.OverlayRemovedOnly && value.ResetExitPreserved &&
                value.StaticTileDeltaCount == 0 && value.RuntimeMutationCount == 0), Is.True);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceEventsContainMeteorMerchantRareCreatureMaruAndSingleEmptyVariant()
        {
            var production = Production(Sample());
            Assert.That(production.Events, Has.Count.EqualTo(5));
            Assert.That(production.Events.Select(value => value.EventId),
                Is.EquivalentTo(new[] { "EVT_METEOR_FALL", "EVT_WANDERING_MERCHANT",
                    "EVT_RARE_CREATURE", "EVT_MARU_INTERVENTION", "EVT_EMPTY" }));
            Assert.That(production.Events.Count(value => value.EmptyVariant), Is.EqualTo(1));
            var empty = production.Events.Single(value => value.EmptyVariant);
            Assert.That(new[] { empty.MarkerCount, empty.Weight, empty.CooldownGap },
                Is.EqualTo(new[] { 0, 0, 0 }));
            Assert.That(empty.Operation, Is.EqualTo("none"));
            Assert.That(empty.PayloadIdentity, Is.EqualTo("none"));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceEventsRemainMarkerOnlyAndRejectRuntimePayloadExecutionRequests()
        {
            var production = Production(Sample());
            Assert.That(production.Markers, Has.Count.EqualTo(4));
            Assert.That(production.Markers.All(value =>
                value.ExecutionPolicy == "MarkerOnlyNoExecution"), Is.True);
            Assert.That(production.Events.Where(value => !value.EmptyVariant).All(value =>
                value.MarkerCount == 1 && !production.CanExecutePayload(value.EventId)), Is.True);
            Assert.Throws<InvalidOperationException>(() =>
                production.RequestPayloadExecution("EVT_METEOR_FALL"));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceEventCompatibilityKeepsMerchantRareCreatureMaruScopedAndEmptyExplicit()
        {
            var production = Production(Sample());
            var meteor = production.Compatibility.Single(value =>
                value.EventId == "EVT_METEOR_FALL");
            Assert.That(meteor.ClusterScope, Is.EqualTo("Terrain:CORE"));
            Assert.That(meteor.ActivityId, Is.Empty);
            Assert.That(production.Compatibility.Where(value =>
                value.EventId == "EVT_WANDERING_MERCHANT" ||
                value.EventId == "EVT_RARE_CREATURE").All(value =>
                value.ActivityId == "ACT_MILL_ESCORT_CART" &&
                value.RequiredSlotSemantic == "Npc"), Is.True);
            var maru = production.Compatibility.Single(value =>
                value.EventId == "EVT_MARU_INTERVENTION");
            Assert.That(maru.ActivityId, Is.EqualTo("ACT_MARU_REWIND_ANOMALY"));
            Assert.That(maru.RequiredSlotSemantic, Is.EqualTo("Device"));
            var empty = production.Compatibility.Where(value =>
                value.EventId == "EVT_EMPTY").ToArray();
            Assert.That(empty.Where(value => value.ActivityId.Length != 0)
                .Select(value => value.ActivityId),
                Is.EquivalentTo(MoonPalaceActivityEventProduction.RequiredActivityIds));
            Assert.That(empty.Any(value => value.ActivityId.Length == 0 &&
                value.ClusterScope == "UnassignedClusters"), Is.True);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceActivityEventPublisherReadsMap12AndMap21ClusterArtifactsWithoutRewriting()
        {
            var paths = new[]
            {
                Constant("SourceMap12ActivityCatalogRelativePath"),
                Constant("SourceMap12ActivitySlotsRelativePath"),
                Constant("SourceMap12ActivitySafetyRelativePath"),
                Constant("SourceMap12ActivityCompatibilityRelativePath"),
                Constant("SourceMap12EventCatalogRelativePath"),
                Constant("SourceMap12EventMarkersRelativePath"),
                Constant("SourceMap2104AllBiomeManifestRelativePath"),
                Constant("SourceMap2104DigestManifestRelativePath"),
            };
            var before = paths.ToDictionary(value => value, value => FileSha(value),
                StringComparer.Ordinal);
            Sample();
            var after = paths.ToDictionary(value => value, value => FileSha(value),
                StringComparer.Ordinal);
            Assert.That(after, Is.EqualTo(before));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceActivityEventPublisherWritesOnlyMap21_05AuthoringAndGeneratedRoots()
        {
            var sample = InvokePublisher("PublishAuthoringAndSamples", ProjectRoot, true);
            var paths = (IReadOnlyList<string>)Property(sample, "WrittenRelativePaths");
            var authoring = Constant("AuthoringDirectoryRelativePath") + "/";
            var generated = Constant("GeneratedDirectoryRelativePath") + "/";
            Assert.That(paths, Has.Count.EqualTo(10));
            Assert.That(paths.Count(value => value.StartsWith(authoring,
                StringComparison.Ordinal)), Is.EqualTo(6));
            Assert.That(paths.Count(value => value.StartsWith(generated,
                StringComparison.Ordinal)), Is.EqualTo(4));
            foreach (var relativePath in paths)
            {
                var bytes = File.ReadAllBytes(Resolve(relativePath));
                Assert.That(bytes.Take(3), Is.Not.EqualTo(new byte[] { 0xef, 0xbb, 0xbf }));
                var text = Encoding.UTF8.GetString(bytes);
                Assert.That(text.Contains("\r"), Is.False);
                Assert.That(text.EndsWith("\n", StringComparison.Ordinal), Is.True);
            }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceActivityEventDigestsAreDeterministicReverseOrderRepeatAndCultureStable()
        {
            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
                CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
                var forward = Sample(false);
                CultureInfo.CurrentCulture = new CultureInfo("ko-KR");
                CultureInfo.CurrentUICulture = new CultureInfo("ko-KR");
                var reverse = Sample(true);
                var left = Production(forward);
                var right = Production(reverse);
                Assert.That(left.SerializeActivityProfilesCsv(),
                    Is.EqualTo(right.SerializeActivityProfilesCsv()));
                Assert.That(left.SerializeBindingsCsv(), Is.EqualTo(right.SerializeBindingsCsv()));
                Assert.That(left.SerializeSlotsCsv(), Is.EqualTo(right.SerializeSlotsCsv()));
                Assert.That(left.SerializeEventProfilesCsv(),
                    Is.EqualTo(right.SerializeEventProfilesCsv()));
                Assert.That(left.SerializeEventMarkersCsv(),
                    Is.EqualTo(right.SerializeEventMarkersCsv()));
                Assert.That(left.SerializeCompatibilityCsv(),
                    Is.EqualTo(right.SerializeCompatibilityCsv()));
                Assert.That(left.SerializeActivityEventManifest(),
                    Is.EqualTo(right.SerializeActivityEventManifest()));
                Assert.That(left.SerializeRemovalSafetyManifest(),
                    Is.EqualTo(right.SerializeRemovalSafetyManifest()));
                Assert.That(left.SerializeEventOverlayManifest(),
                    Is.EqualTo(right.SerializeEventOverlayManifest()));
                Assert.That(DigestManifest(forward).Serialize(),
                    Is.EqualTo(DigestManifest(reverse).Serialize()));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceActivityEventRejectsDuplicateIdsUnknownClustersWrongBiomeQuietBufferAndMissingEmpty()
        {
            var valid = Production(Sample());
            Assert.Throws<ArgumentException>(() => Rebuild(valid,
                activities: valid.Activities.Concat(new[] { valid.Activities[0] })));
            var unknownBinding = new MoonPalaceActivityClusterBinding(
                valid.Bindings[0].ActivityId, valid.Bindings[0].BiomeId,
                "TC_UNKNOWN", valid.Bindings[0].FallbackClusterId);
            Assert.Throws<ArgumentException>(() => Rebuild(valid,
                bindings: valid.Bindings.Skip(1).Concat(new[] { unknownBinding })));
            var primaryId = valid.Bindings[0].PrimaryClusterId;
            Assert.Throws<ArgumentException>(() => Rebuild(valid,
                clusters: ReplaceCluster(valid, primaryId, "WrongBiome", "Terrain")));
            Assert.Throws<ArgumentException>(() => Rebuild(valid,
                clusters: ReplaceCluster(valid, primaryId,
                    valid.Clusters.Single(value => value.ClusterId == primaryId).BiomeId,
                    "Quiet")));
            Assert.Throws<ArgumentException>(() => Rebuild(valid,
                events: valid.Events.Where(value => value.EventId != "EVT_EMPTY")));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceActivityEventDoesNotRunGenerationRendererValidationReplayRollbackPlayModeOrLegacyRegression()
        {
            var counters = (MoonPalaceActivityEventForbiddenOperationCounters)Property(
                Sample(), "Counters");
            Assert.That(counters.AllZero, Is.True);
            Assert.That(counters.ActivityStateMachineExecutions, Is.Zero);
            Assert.That(counters.EventPayloadExecutions, Is.Zero);
            Assert.That(counters.GenerationRunnerExecutions, Is.Zero);
            Assert.That(counters.RendererExecutions, Is.Zero);
            Assert.That(counters.ValidationRunnerExecutions, Is.Zero);
            Assert.That(counters.ReplayExecutions, Is.Zero);
            Assert.That(counters.RollbackExecutions, Is.Zero);
            Assert.That(counters.TilemapWrites, Is.Zero);
            Assert.That(counters.RuntimeObjectSpawns, Is.Zero);
            Assert.That(counters.ScenePrefabChanges, Is.Zero);
            Assert.That(counters.PriorCategorySelections, Is.Zero);
            Assert.That(counters.PlayModeSelections, Is.Zero);
            Assert.That(counters.LegacyRegressionSelections, Is.Zero);
            Assert.That(counters.UnfilteredOrFullRegressionSelections, Is.Zero);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceActivityEventPublishesMap21_06HandoffOnlyAfterFocusedPass()
        {
            var blocked = Assert.Throws<TargetInvocationException>(() =>
                InvokePublisher("PublishAuthoringAndSamples", ProjectRoot, false));
            Assert.That(blocked.InnerException, Is.TypeOf<InvalidOperationException>());
            var sample = InvokePublisher("PublishAuthoringAndSamples", ProjectRoot, true);
            var manifest = DigestManifest(sample);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(
                manifest.Map2106HandoffDigest), Is.True);
            Assert.That(manifest.CsvDigests, Has.Count.EqualTo(6));
            Assert.That(manifest.JsonDigests, Has.Count.EqualTo(3));
            Assert.That(File.Exists(Resolve(Constant("GeneratedDirectoryRelativePath") + "/" +
                Constant("DigestManifestFileName"))), Is.True);
        }

        private static IEnumerable<MoonPalaceActivityClusterDescriptor> ReplaceCluster(
            MoonPalaceActivityEventProduction production, string id, string biome,
            string poolKind) => production.Clusters.Select(value => value.ClusterId == id
                ? new MoonPalaceActivityClusterDescriptor(id, biome, poolKind) : value);

        private static MoonPalaceActivityEventProduction Rebuild(
            MoonPalaceActivityEventProduction value,
            IEnumerable<MoonPalaceActivityProfile> activities = null,
            IEnumerable<MoonPalaceActivityClusterBinding> bindings = null,
            IEnumerable<MoonPalaceEventOverlayProfile> events = null,
            IEnumerable<MoonPalaceActivityClusterDescriptor> clusters = null) =>
            new MoonPalaceActivityEventProduction(activities ?? value.Activities,
                bindings ?? value.Bindings, value.Slots, value.Removals,
                events ?? value.Events, value.Markers, value.Compatibility,
                clusters ?? value.Clusters, value.CreatedUtc);

        private static object Sample(bool reverse = false) => InvokePublisher(
            "CreateReadOnlySample", ProjectRoot, "2026-09-06T00:00:00.0000000Z", reverse);
        private static MoonPalaceActivityEventProduction Production(object sample) =>
            (MoonPalaceActivityEventProduction)Property(sample, "Production");
        private static MoonPalaceActivityEventDigestManifest DigestManifest(object sample) =>
            (MoonPalaceActivityEventDigestManifest)Property(sample, "DigestManifest");
        private static object Property(object instance, string name) =>
            instance.GetType().GetProperty(name).GetValue(instance);
        private static object InvokePublisher(string methodName, params object[] arguments) =>
            PublisherType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, arguments);
        private static string Constant(string name) => (string)PublisherType.GetField(name)
            .GetRawConstantValue();
        private static Type PublisherType => Assembly.Load("MapAuthoring.Editor")
            .GetType(PublisherTypeName, true);
        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        private static string Resolve(string relativePath) => Path.Combine(ProjectRoot,
            relativePath.Replace('/', Path.DirectorySeparatorChar));

        private static string FileSha(string relativePath)
        {
            using (var stream = File.OpenRead(Resolve(relativePath)))
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(stream).Select(value =>
                    value.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }
}
