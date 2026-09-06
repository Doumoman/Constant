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
    public sealed class MoonPalaceVillageProductionTests
    {
        private const string CategoryName = "MAP21_08";
        private const string PublisherTypeName = "StarNight.Editor.MapAuthoring.WorldGeneration.MoonPalace.MoonPalaceVillagePublisher";

        [Test, Category(CategoryName)]
        public void MoonPalaceVillageContainsExactThreeLayoutsAndApprovedShapes()
        {
            var value = Production(Sample());
            Assert.That(value.Profiles.Count, Is.EqualTo(3));
            Assert.That(value.Profiles.Select(item => item.VillageLayoutId).OrderBy(item => item),
                Is.EqualTo(MoonPalaceVillageProduction.RequiredLayoutIds.OrderBy(item => item)));
            AssertProfile(value, "VLG_MOONPALACE_1X1_OVERVIEW", "1x1", 48, 32, 1, 48, 5);
            AssertProfile(value, "VLG_MOONPALACE_2X1_MARKET", "2x1", 96, 32, 2, 96, 6);
            AssertProfile(value, "VLG_MOONPALACE_1X2_ASCENT", "1x2", 48, 64, 2, 64, 5);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceVillagePublishesFacilityCountsKitchenRepairAndOptionalSlots()
        {
            var value = Production(Sample());
            Assert.That(value.Facilities.Count, Is.EqualTo(16));
            Assert.That(value.Facilities.Count(item => item.FacilityKind == "Kitchen" && item.IsFixed), Is.EqualTo(3));
            Assert.That(value.Facilities.Count(item => item.FacilityKind == "Repair" && item.IsFixed), Is.EqualTo(3));
            Assert.That(value.Facilities.Count(item => item.IsOptional), Is.EqualTo(10));
            foreach (var profile in value.Profiles)
            {
                var facilities = value.Facilities.Where(item => item.VillageLayoutId == profile.VillageLayoutId).ToArray();
                Assert.That(facilities.Length, Is.EqualTo(profile.FacilityCount));
                Assert.That(facilities.Count(item => item.IsFixed), Is.EqualTo(2));
                Assert.That(facilities.Count(item => item.IsOptional), Is.EqualTo(profile.OptionalFacilityCount));
                Assert.That(facilities.Count(item => item.FacilityKind == "OptionalLore"),
                    Is.EqualTo(profile.SourceMap13Shape == "2x1" ? 1 : 0));
            }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceVillageRoadsDoorsAndReturnWitnessesPreserveShellAccessAndSeams()
        {
            var value = Production(Sample());
            Assert.That(value.Roads.Count, Is.EqualTo(208));
            Assert.That(value.Doors.Count, Is.EqualTo(16));
            Assert.That(value.Doors.Count(item => item.ForwardWitness), Is.EqualTo(16));
            Assert.That(value.Doors.Count(item => item.ReverseRoadReturnWitness), Is.EqualTo(16));
            Assert.That(value.Doors.All(item => item.MarkerOnly && !item.OwnsCollision && !item.OwnsLock && !item.PathBlocking), Is.True);
            var horizontal = value.Roads.Where(item => item.VillageLayoutId == "VLG_MOONPALACE_2X1_MARKET").ToArray();
            var vertical = value.Roads.Where(item => item.VillageLayoutId == "VLG_MOONPALACE_1X2_ASCENT").ToArray();
            Assert.That(Has(horizontal, 47, 16) && Has(horizontal, 48, 16), Is.True);
            Assert.That(Has(vertical, 24, 31) && Has(vertical, 24, 32), Is.True);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceVillagePublishesExactFiveStateVariantsPerLayout()
        {
            var value = Production(Sample());
            Assert.That(value.StateVariants.Count, Is.EqualTo(15));
            foreach (var profile in value.Profiles)
            {
                var variants = value.StateVariants.Where(item => item.VillageLayoutId == profile.VillageLayoutId)
                    .Select(item => item.VariantKind).OrderBy(item => item).ToArray();
                Assert.That(variants, Is.EqualTo(MoonPalaceVillageProduction.RequiredStateVariants.OrderBy(item => item)));
            }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceVillageMarkersPreserveNpcInventoryDoorStateMatrix()
        {
            var value = Production(Sample());
            Assert.That(value.Markers.Count(item => item.MarkerKind == "Npc"), Is.EqualTo(9));
            Assert.That(value.Markers.Count(item => item.MarkerKind == "Inventory"), Is.EqualTo(6));
            Assert.That(value.Markers.Count(item => item.IsShopkeeper), Is.EqualTo(3));
            Assert.That(value.StateVariants.Count(item => item.VariantKind == "Normal" &&
                item.NpcStates == "Normal|Normal|Normal" && item.InventoryStates == "Standard|Standard" && item.DoorState == "Standard"), Is.EqualTo(3));
            Assert.That(value.StateVariants.Count(item => item.VariantKind == "Friendly" &&
                item.NpcStates == "Friendly|Friendly|Friendly" && item.InventoryStates == "FriendlyAccess|FriendlyAccess" && item.DoorState == "Welcome"), Is.EqualTo(3));
            Assert.That(value.StateVariants.Count(item => item.VariantKind == "AllHostile" &&
                item.NpcStates == "Hostile|Hostile|Hostile" && item.InventoryStates == "Unavailable|Unavailable" && item.DoorState == "Alert"), Is.EqualTo(3));
            Assert.That(value.StateVariants.Count(item => item.VariantKind == "Evacuation" &&
                item.NpcStates == "Evacuated|Evacuated|Evacuated" && item.InventoryStates == "Evacuated|Evacuated" && item.DoorState == "Evacuated"), Is.EqualTo(3));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceVillageShopkeeperIsStaticMarkerOnlyAndDoesNotCreateShopRuntime()
        {
            var value = Production(Sample());
            var shopkeepers = value.Markers.Where(item => item.IsShopkeeper).ToArray();
            Assert.That(shopkeepers.Length, Is.EqualTo(3));
            Assert.That(shopkeepers.All(item => item.MarkerKind == "Npc" && item.Role == "Shopkeeper" &&
                item.StaticPayloadOnly && item.RuntimeControllerId == "NONE" &&
                item.InventoryOrPricePayload == "NONE"), Is.True);
            Assert.That(MoonPalaceVillageForbiddenOperationCounters.Zero.ShopTransactions, Is.Zero);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceVillageIndividualHostileTargetsExactlyOneNpcPerLayout()
        {
            var value = Production(Sample());
            var variants = value.StateVariants.Where(item => item.VariantKind == "IndividualHostile").ToArray();
            Assert.That(variants.Length, Is.EqualTo(3));
            foreach (var variant in variants)
            {
                var ids = variant.NpcMarkerIds.Split('|');
                var states = variant.NpcStates.Split('|');
                Assert.That(states.Count(item => item == "Hostile"), Is.EqualTo(1));
                Assert.That(states.Count(item => item == "Normal"), Is.EqualTo(2));
                Assert.That(ids[Array.IndexOf(states, "Hostile")], Is.EqualTo(variant.IndividualTargetOrNone));
            }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceVillageHostileAndEvacuationVariantsDoNotMutateRoadDoorCollisionOrAccess()
        {
            var value = Production(Sample());
            foreach (var variant in value.StateVariants.Where(item => item.VariantKind == "IndividualHostile" ||
                item.VariantKind == "AllHostile" || item.VariantKind == "Evacuation"))
            {
                var profile = value.Profiles.Single(item => item.VillageLayoutId == variant.VillageLayoutId);
                Assert.That(variant.RoadDigest, Is.EqualTo(profile.RoadDigest));
                Assert.That(variant.FacilityDigest, Is.EqualTo(profile.FacilityDigest));
                Assert.That(variant.DoorDigest, Is.EqualTo(profile.DoorDigest));
                Assert.That(variant.RoadMutationCount + variant.FacilityCoordinateMutationCount +
                    variant.DoorCoordinateMutationCount + variant.AccessWitnessMutationCount, Is.Zero);
            }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceVillageRemainsOptionalReferenceLocalWithoutProgressionOrRewardDependency()
        {
            var value = Production(Sample());
            Assert.That(value.Profiles.All(item => item.IsOptionalReferenceLocal && !item.IsProgressionBlocker &&
                !item.HasRequiredRewardDependency && !item.HasCoreResourceDependency &&
                !item.HasForgeBossDependency), Is.True);
            Assert.That(value.Roads.All(item => item.AccessClass == "MandatoryNoTool"), Is.True);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceVillagePublisherReadsMap13Map18AndMap21SourcesWithoutRewriting()
        {
            var sample = Sample();
            var paths = (IReadOnlyList<string>)Property(sample, "SourceReadRelativePaths");
            var before = paths.ToDictionary(path => path, FileSha, StringComparer.Ordinal);
            var second = Sample(true);
            var after = paths.ToDictionary(path => path, FileSha, StringComparer.Ordinal);
            Assert.That(paths.Count, Is.EqualTo(16));
            Assert.That(after, Is.EqualTo(before));
            var observed = DigestManifest(second).ObservedSourceResultDigests.ToDictionary(item => item.Name, item => item.Digest);
            Assert.That(observed["MAP13_04_RESULT"], Is.EqualTo("f5419f1218885ebe89a24d8106a481df93da80d9c25821f4398748f2ab96ab26"));
            Assert.That(observed["MAP13_05_RESULT"], Is.EqualTo("005ad4993c1db449b6199f1e0d2842d10465b8f48b2eec78a37542c5038a9fa6"));
            Assert.That(observed["MAP13_09_RESULT"], Is.EqualTo("637fec406f42bf845be5ae9313a036b3ec49f66467539a3552c1f94ad68bd5e2"));
            Assert.That(observed["MAP18_06_RESULT"], Is.EqualTo("ad2b88be043cb7e18289909a7ad44d76c9143a65e5228c11c24f1a60b86831fd"));
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceVillagePublisherWritesOnlyMap21_08AuthoringAndGeneratedRoots()
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
                Assert.That(text.EndsWith("\n\n", StringComparison.Ordinal), Is.False);
            }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceVillageDigestsAreDeterministicReverseOrderRepeatAndCultureStable()
        {
            var culture = CultureInfo.CurrentCulture;
            var uiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
                CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
                var leftSample = Sample(false);
                var left = Production(leftSample);
                CultureInfo.CurrentCulture = new CultureInfo("ko-KR");
                CultureInfo.CurrentUICulture = new CultureInfo("ko-KR");
                var rightSample = Sample(true);
                var right = Production(rightSample);
                Assert.That(left.SerializeProfilesCsv(), Is.EqualTo(right.SerializeProfilesCsv()));
                Assert.That(left.SerializeFacilitiesCsv(), Is.EqualTo(right.SerializeFacilitiesCsv()));
                Assert.That(left.SerializeRoadsCsv(), Is.EqualTo(right.SerializeRoadsCsv()));
                Assert.That(left.SerializeDoorsCsv(), Is.EqualTo(right.SerializeDoorsCsv()));
                Assert.That(left.SerializeMarkersCsv(), Is.EqualTo(right.SerializeMarkersCsv()));
                Assert.That(left.SerializeStateVariantsCsv(), Is.EqualTo(right.SerializeStateVariantsCsv()));
                Assert.That(left.SerializeVillageManifest(), Is.EqualTo(right.SerializeVillageManifest()));
                Assert.That(left.SerializeAccessManifest(), Is.EqualTo(right.SerializeAccessManifest()));
                Assert.That(left.SerializeStateManifest(), Is.EqualTo(right.SerializeStateManifest()));
                Assert.That(DigestManifest(leftSample).Serialize(), Is.EqualTo(DigestManifest(rightSample).Serialize()));
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = uiCulture;
            }
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceVillageRejectsDuplicateLayoutsBadFacilitiesMissingVariantsBadDoorsAndRuntimeSideEffects()
        {
            var value = Production(Sample());
            Assert.Throws<ArgumentException>(() => Rebuild(value,
                profiles: value.Profiles.Concat(new[] { value.Profiles[0] })));
            var facility = value.Facilities[0];
            var badFacility = new MoonPalaceVillageFacility(facility.VillageLayoutId,
                facility.FacilityId, "BadFacility", facility.Requirement, facility.SlotId,
                facility.LocalX, facility.LocalY);
            Assert.Throws<ArgumentException>(() => Rebuild(value, facilities:
                value.Facilities.Where(item => item != facility).Concat(new[] { badFacility })));
            Assert.Throws<ArgumentException>(() => Rebuild(value, states: value.StateVariants.Skip(1)));
            var door = value.Doors[0];
            var badDoor = new MoonPalaceVillageDoor(door.VillageLayoutId, door.DoorId,
                door.FacilityId, door.WitnessId, door.LocalX, door.LocalY,
                door.RoadReturnX, door.RoadReturnY, ownsCollision: true);
            Assert.Throws<ArgumentException>(() => Rebuild(value, doors:
                value.Doors.Where(item => item != door).Concat(new[] { badDoor })));
            Assert.That(value.CanExecuteRuntimeSideEffects, Is.False);
            Assert.Throws<InvalidOperationException>(() => value.RequestRuntimeExecution());
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceVillageDoesNotRunGenerationRendererValidationReplayRollbackPlayModeOrLegacyRegression()
        {
            var counters = (MoonPalaceVillageForbiddenOperationCounters)Property(Sample(), "Counters");
            Assert.That(counters.AllZero, Is.True);
            Assert.That(counters.NpcSpawns + counters.NpcAiExecutions + counters.CombatExecutions +
                counters.ShopTransactions + counters.DoorCollisionOrLockWrites +
                counters.HostileEvacuationRuntimeExecutions + counters.SaveFileWrites +
                counters.SaveFileReads + counters.PlayerPrefsWrites + counters.PlayerPrefsReads +
                counters.WorldSectorPlacements + counters.GenerationRunnerExecutions +
                counters.RendererExecutions + counters.ValidationRunnerExecutions +
                counters.ReplayExecutions + counters.RollbackExecutions + counters.TilemapWrites +
                counters.RuntimeObjectSpawns + counters.ScenePrefabChanges +
                counters.ColliderAddressablesChanges + counters.PriorCategorySelections +
                counters.PlayModeSelections + counters.LegacyRegressionSelections +
                counters.UnfilteredOrFullRegressionSelections + counters.UpstreamRegenerationRuns, Is.Zero);
        }

        [Test, Category(CategoryName)]
        public void MoonPalaceVillagePublishesMap21_09HandoffOnlyAfterFocusedPass()
        {
            var blocked = Assert.Throws<TargetInvocationException>(() =>
                InvokePublisher("PublishAuthoringAndSamples", ProjectRoot, false));
            Assert.That(blocked.InnerException, Is.TypeOf<InvalidOperationException>());
            var sample = InvokePublisher("PublishAuthoringAndSamples", ProjectRoot, true);
            var manifest = DigestManifest(sample);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(manifest.Map2109HandoffDigest), Is.True);
            Assert.That(manifest.ObservedSourceResultDigests.Count, Is.EqualTo(4));
            Assert.That(manifest.CsvDigests.Count, Is.EqualTo(6));
            Assert.That(manifest.JsonDigests.Count, Is.EqualTo(3));
            Assert.That(File.Exists(Resolve(Constant("GeneratedDirectoryRelativePath") + "/" +
                Constant("DigestManifestFileName"))), Is.True);
        }

        private static void AssertProfile(MoonPalaceVillageProduction value, string id,
            string shape, int width, int height, int sectors, int roads, int facilities)
        {
            var profile = value.Profiles.Single(item => item.VillageLayoutId == id);
            Assert.That(profile.SourceMap13Shape, Is.EqualTo(shape));
            Assert.That(profile.BoundsWidth, Is.EqualTo(width));
            Assert.That(profile.BoundsHeight, Is.EqualTo(height));
            Assert.That(profile.ActiveSectorCount, Is.EqualTo(sectors));
            Assert.That(profile.RoadCellCount, Is.EqualTo(roads));
            Assert.That(profile.FacilityCount, Is.EqualTo(facilities));
        }
        private static bool Has(IEnumerable<MoonPalaceVillageRoadCell> values, int x, int y) =>
            values.Any(item => item.LocalX == x && item.LocalY == y);
        private static MoonPalaceVillageProduction Rebuild(MoonPalaceVillageProduction value,
            IEnumerable<MoonPalaceVillageLayoutProfile> profiles = null,
            IEnumerable<MoonPalaceVillageFacility> facilities = null,
            IEnumerable<MoonPalaceVillageDoor> doors = null,
            IEnumerable<MoonPalaceVillageStateVariant> states = null) =>
            new MoonPalaceVillageProduction(profiles ?? value.Profiles,
                facilities ?? value.Facilities, value.Roads, doors ?? value.Doors,
                value.Markers, states ?? value.StateVariants, value.CreatedUtc);
        private static object Sample(bool reverse = false) => InvokePublisher(
            "CreateReadOnlySample", ProjectRoot, "2026-09-07T00:00:00.0000000Z", reverse);
        private static MoonPalaceVillageProduction Production(object sample) =>
            (MoonPalaceVillageProduction)Property(sample, "Production");
        private static MoonPalaceVillageDigestManifest DigestManifest(object sample) =>
            (MoonPalaceVillageDigestManifest)Property(sample, "DigestManifest");
        private static object Property(object instance, string name) =>
            instance.GetType().GetProperty(name).GetValue(instance);
        private static object InvokePublisher(string methodName, params object[] arguments) =>
            PublisherType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, arguments);
        private static string Constant(string name) =>
            (string)PublisherType.GetField(name).GetRawConstantValue();
        private static Type PublisherType => Assembly.Load("MapAuthoring.Editor")
            .GetType(PublisherTypeName, true);
        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        private static string Resolve(string path) => Path.Combine(ProjectRoot,
            path.Replace('/', Path.DirectorySeparatorChar));
        private static string FileSha(string path)
        {
            using (var stream = File.OpenRead(Resolve(path)))
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(stream).Select(value =>
                    value.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }
}
