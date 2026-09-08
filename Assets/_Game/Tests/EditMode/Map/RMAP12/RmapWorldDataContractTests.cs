#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.MoonPalace.RunGeneration;
using StarNight.Map.WorldGeneration.WorldData;

namespace StarNight.Map.Tests.EditMode.Rmap12
{
    [Category("RMAP12")]
    public sealed class RmapWorldDataContractTests
    {
        [Test]
        public void W08_ActualRmap10RequestResultAndPoolInputsAreDeterministic()
        {
            var request = Request(1107);
            RmapWorldDefinition first = RmapWorldDataGenerator.Generate(request, RngStreams());
            RmapWorldDefinition second = RmapWorldDataGenerator.Generate(Request(1107), RngStreams());
            RmapSmallRunPlan direct = RmapSmallRunHarness.Generate(new RmapSmallRunRequest(
                1107, RmapSmallRunHarness.MinimumWidth, RmapSmallRunHarness.MinimumHeight,
                RmapSmallRunRecipe.PortGalleryV1));

            Assert.That(first.ActualRunPlan.Success, Is.True, first.ActualRunPlan.FailureSummary);
            Assert.That(first.ActualRunPlan.PlanDigest, Is.EqualTo(direct.PlanDigest));
            Assert.That(first.Request.RunWidth, Is.EqualTo(direct.Request.Width));
            Assert.That(first.Request.RunHeight, Is.EqualTo(direct.Request.Height));
            Assert.That(first.Request.Recipe, Is.EqualTo(direct.Request.Recipe));
            Assert.That(first.Digest, Is.EqualTo(second.Digest));
            Assert.That(first.PoolVersion, Does.StartWith("RMAP11_POOL500_V2_FIX25"));
            Assert.That(first.PoolDigest, Is.Not.Empty);
        }

        [Test]
        public void W09_SevenNamedBindingsUseExistingRngAndOneRetryCannotChangeAnotherStream()
        {
            var request = Request(77);
            var streams = RngStreams();
            RmapWorldDefinition definition = RmapWorldDataGenerator.Generate(request, streams);
            DeterministicRngStream populationBefore = RmapWorldDataGenerator.CreateStream(
                request, streams, RmapWorldRngStream.Population);
            ulong expectedPopulation = populationBefore.NextUInt64();

            DeterministicRngStream retriedRunGraph = RmapWorldDataGenerator.CreateStream(
                request, streams, RmapWorldRngStream.RunGraph, 1);
            for (var index = 0; index < 64; index++) retriedRunGraph.NextUInt64();

            DeterministicRngStream populationAfter = RmapWorldDataGenerator.CreateStream(
                request, streams, RmapWorldRngStream.Population);
            Assert.That(populationAfter.NextUInt64(), Is.EqualTo(expectedPopulation));
            Assert.That(definition.RngBindings.Count, Is.EqualTo(7));
            Assert.That(definition.RngBindings.Values.Select(value => value.InitialState)
                .Distinct().Count(), Is.EqualTo(7));
            Assert.That(definition.RngBindings[RmapWorldRngStream.RunGraph].SourceStreamId,
                Is.EqualTo(WorldGenerationRngStreams.RouteStreamId));
            Assert.That(definition.RngBindings[RmapWorldRngStream.Pattern].ScopeIdentity,
                Is.EqualTo("0,0"));
        }

        [Test]
        public void W10_ExistingSaveManifestRoundTripRestoresOnlyKnownStableSlotMutation()
        {
            RmapWorldDefinition definition = RmapWorldDataGenerator.Generate(Request(42), RngStreams());
            RmapWorldStableId target = definition.GetStableIds(RmapWorldStableIdKind.RewardChest)[0];
            GeneratedWorldSaveManifest manifest = ManifestFor(target, definition);
            GeneratedSaveManifestResult serialized = GeneratedSaveManifestSerializer.Serialize(manifest);
            Assert.That(serialized.Success, Is.True, Describe(serialized.Failures));
            GeneratedSaveManifestResult parsed = GeneratedSaveManifestSerializer.Parse(serialized.Payload);
            Assert.That(parsed.Success, Is.True, Describe(parsed.Failures));

            RmapWorldMutationState restored = RmapWorldMutationState.RestoreFromSaveManifest(
                parsed.Manifest, definition);
            Assert.That(restored.TryGetState(target, out string state), Is.True);
            Assert.That(state, Is.EqualTo("COLLECTED"));
            Assert.That(RmapWorldDataGenerator.Generate(Request(42), RngStreams()).Digest,
                Is.EqualTo(definition.Digest));
        }

        [Test]
        public void W11_StableIdsDistinguishMultipleActualChunksAndDeferredSlotsWithoutOrder()
        {
            RmapWorldDefinition first = RmapWorldDataGenerator.Generate(Request(203), RngStreams());
            RmapWorldDefinition second = RmapWorldDataGenerator.Generate(Request(203), RngStreams());
            IReadOnlyList<RmapWorldStableId> chunks = first.GetStableIds(RmapWorldStableIdKind.MicroChunk);
            IReadOnlyList<RmapWorldStableId> rewards = first.GetStableIds(RmapWorldStableIdKind.RewardChest);

            Assert.That(chunks.Count, Is.GreaterThan(1));
            Assert.That(rewards.Count, Is.EqualTo(2));
            Assert.That(chunks.Select(value => value.Value).Distinct().Count(), Is.EqualTo(chunks.Count));
            Assert.That(rewards.Select(value => value.Value).Distinct().Count(), Is.EqualTo(rewards.Count));
            foreach (RmapWorldStableId id in rewards)
                Assert.That(second.FindStableId(id.Kind, id.OwnerIdentity, id.SlotIdentity)?.Value,
                    Is.EqualTo(id.Value));

            var sameLocalOwner = "RMAP10_CHUNK|C0_0|0|0";
            var breakable = new RmapWorldStableId(RmapWorldStableIdKind.BreakableTile,
                sameLocalOwner, "SHARED_SLOT");
            var mechanism = new RmapWorldStableId(RmapWorldStableIdKind.Mechanism,
                sameLocalOwner, "SHARED_SLOT");
            Assert.That(mechanism.Value, Is.Not.EqualTo(breakable.Value));
        }

        private static RmapWorldDataRequest Request(ulong seed) => new RmapWorldDataRequest(
            seed, "CONTENT_V1", "GENERATOR_V1", RmapSmallRunHarness.MinimumWidth,
            RmapSmallRunHarness.MinimumHeight, RmapSmallRunRecipe.PortGalleryV1);

        private static GeneratedWorldSaveManifest ManifestFor(
            RmapWorldStableId target,
            RmapWorldDefinition definition)
        {
            var baseDigests = new GeneratedSectorModificationBaseDigests(
                Hash("geometry"), Hash("bake"), Hash("cache"), Hash("window"),
                Hash("window-diff"), Hash("transition"));
            var version = new GeneratedSaveManifestVersion(GeneratedSaveManifestService.SchemaVersion,
                definition.Request.GeneratorVersion, definition.PoolVersion);
            var header = new GeneratedSaveManifestHeader(version,
                definition.Request.Seed.ToString(CultureInfo.InvariantCulture), baseDigests.GeometryDigest,
                Hash("placement"), baseDigests.BakeDigest, baseDigests.CacheDigest,
                Hash("handle"), Hash("storage"), 1);
            var sector = new GeneratedSectorCoordinate(0, 0);
            var mutationTarget = new GeneratedSectorModificationTarget(sector,
                new GeneratedSectorLocalCellIndex(0), (int)GeneratedTilemapLayerId.Marker,
                "RMAP12_SAVE_MANIFEST_FIXTURE", target.Value);
            var id = new GeneratedSectorModificationStableId(header.SeedIdentity,
                header.Version.GeneratorVersion, header.Version.DataVersion, mutationTarget,
                GeneratedSectorModificationKind.CollectPickup,
                GeneratedSectorModificationStore.SchemaVersion);
            var record = new GeneratedSectorModificationRecord(id, mutationTarget,
                GeneratedSectorModificationKind.CollectPickup, 1,
                GeneratedSectorModificationPayload.PickupCollected(), baseDigests);
            var payload = new GeneratedSaveManifestRecordPayload(record);
            string setDigest = GeneratedSaveManifestService.ComputeModificationSetDigest(header,
                sector, 1, baseDigests, new[] { payload });
            return new GeneratedWorldSaveManifest(header, new[]
            {
                new GeneratedModifiedSectorManifestEntry(sector, 1, baseDigests, setDigest,
                    new[] { payload }),
            });
        }

        private static WorldGenerationRngStreams RngStreams()
        {
            var definitions = new SortedDictionary<string, RngStreamDefinition>(StringComparer.Ordinal)
            {
                { "RNG_WORLD_SITE", Rng("RNG_WORLD_SITE", "A13C9E0B2F1044D1", "WORLD") },
                { "RNG_BIOME_PATCH", Rng("RNG_BIOME_PATCH", "B7A91D33E40C5F82", "PASS") },
                { "RNG_ROUTE", Rng("RNG_ROUTE", "C00FEE12AB341901", "PASS") },
                { "RNG_TYPE0", Rng("RNG_TYPE0", "D15EA5E007A4C883", "PASS") },
                { "RNG_SECTOR_RECIPE", Rng("RNG_SECTOR_RECIPE", "E9931A70C2D520F4", "SECTOR") },
                { "RNG_POPULATION", Rng("RNG_POPULATION", "F123456789ABCDEF", "SPAWN") },
            };
            var set = (WorldRouteDefinitionSet)FormatterServices.GetUninitializedObject(
                typeof(WorldRouteDefinitionSet));
            SetAutoProperty(set, "RngStreams", new ReadOnlyDictionary<string, RngStreamDefinition>(definitions));
            return new WorldGenerationRngStreams(set);
        }

        private static RngStreamDefinition Rng(string id, string salt, string scope)
        {
            var definition = (RngStreamDefinition)FormatterServices.GetUninitializedObject(
                typeof(RngStreamDefinition));
            SetAutoProperty(definition, "RngStreamId", id);
            SetAutoProperty(definition, "SaltHex", CreateHex(salt));
            SetAutoProperty(definition, "ResetScope", scope);
            SetAutoProperty(definition, "DescriptionKo", "RMAP12 focused fixture");
            SetAutoProperty(definition, "Active", true);
            return definition;
        }

        private static CsvHexValue CreateHex(string value)
        {
            var bytes = Enumerable.Range(0, value.Length / 2).Select(index => byte.Parse(
                value.Substring(index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToArray();
            var constructor = typeof(CsvHexValue).GetConstructor(BindingFlags.Instance |
                BindingFlags.NonPublic, null, new[] { typeof(string), typeof(IEnumerable<byte>) }, null);
            Assert.That(constructor, Is.Not.Null);
            return (CsvHexValue)constructor.Invoke(new object[] { value, bytes });
        }

        private static void SetAutoProperty(object target, string name, object value)
        {
            var field = target.GetType().GetField("<" + name + ">k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private static string Hash(string value)
        {
            using (var sha = SHA256.Create()) return string.Concat(sha.ComputeHash(
                Encoding.UTF8.GetBytes(value)).Select(item => item.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private static string Describe(IEnumerable<GeneratedSaveManifestValidationFailure> failures) =>
            string.Join(";", failures.Select(value => value.ToString()));
    }
}
#endif
