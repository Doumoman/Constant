using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.SpecialRegions;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.SpecialRegions
{
    [TestFixture]
    [Category("MAP13_05")]
    public sealed class VillageStateVariantTests
    {
        [TestCase(VillageLayoutShape.OneByOne, 5, 48, 32)]
        [TestCase(VillageLayoutShape.TwoByOne, 6, 96, 32)]
        [TestCase(VillageLayoutShape.OneByTwo, 5, 48, 64)]
        public void ThreeVillageShapesPublishExactFiveVariants(
            VillageLayoutShape shape,
            int facilityCount,
            int width,
            int height)
        {
            var result = Compile(BuildFixture(shape, facilityCount));

            AssertSuccess(result);
            Assert.That(result.VariantSet.Variants.Select(value => value.StateKind),
                Is.EqualTo(new[]
                {
                    VillageStateKind.Normal,
                    VillageStateKind.Friendly,
                    VillageStateKind.IndividualHostile,
                    VillageStateKind.AllHostile,
                    VillageStateKind.Evacuation,
                }));
            Assert.That(result.VariantSet.WidthTiles, Is.EqualTo(width));
            Assert.That(result.VariantSet.HeightTiles, Is.EqualTo(height));
            Assert.That(result.VariantSet.FacilityBindingCount, Is.EqualTo(facilityCount));
        }

        [Test]
        public void ExactNpcInventoryAndDoorStateMatrixIsPublished()
        {
            var result = Compile(BuildFixture(VillageLayoutShape.TwoByOne, 6));
            AssertSuccess(result);

            AssertStates(result, VillageStateKind.Normal,
                VillageNpcMarkerState.Normal, VillageInventoryMarkerState.Standard, VillageDoorMarkerState.Standard);
            AssertStates(result, VillageStateKind.Friendly,
                VillageNpcMarkerState.Friendly, VillageInventoryMarkerState.FriendlyAccess, VillageDoorMarkerState.Welcome);
            AssertStates(result, VillageStateKind.AllHostile,
                VillageNpcMarkerState.Hostile, VillageInventoryMarkerState.Unavailable, VillageDoorMarkerState.Alert);
            AssertStates(result, VillageStateKind.Evacuation,
                VillageNpcMarkerState.Evacuated, VillageInventoryMarkerState.Evacuated, VillageDoorMarkerState.Evacuated);

            var individual = result.VariantSet.Variants.Single(value => value.StateKind == VillageStateKind.IndividualHostile);
            Assert.That(individual.InventoryMarkers.All(value => value.State == VillageInventoryMarkerState.Standard), Is.True);
            Assert.That(individual.DoorMarkers.All(value => value.State == VillageDoorMarkerState.Standard), Is.True);
        }

        [Test]
        public void IndividualHostileChangesExactlyTheExplicitTarget()
        {
            var fixture = BuildFixture(VillageLayoutShape.OneByOne, 5);
            var result = Compile(fixture);
            AssertSuccess(result);

            var individual = result.VariantSet.Variants.Single(value => value.StateKind == VillageStateKind.IndividualHostile);
            Assert.That(individual.IndividualHostileTargetMarkerId, Is.EqualTo(fixture.Definition.IndividualHostileTargetMarkerId));
            Assert.That(individual.NpcMarkers.Count(value => value.State == VillageNpcMarkerState.Hostile), Is.EqualTo(1));
            Assert.That(individual.NpcMarkers.Single(value => value.State == VillageNpcMarkerState.Hostile).MarkerId,
                Is.EqualTo(fixture.Definition.IndividualHostileTargetMarkerId));
            Assert.That(individual.NpcMarkers.Where(value => value.MarkerId != fixture.Definition.IndividualHostileTargetMarkerId)
                .All(value => value.State == VillageNpcMarkerState.Normal), Is.True);
        }

        [Test]
        public void EveryVariantPreservesMarkerFacilityCoordinateAndShellWitnessIdentity()
        {
            var fixture = BuildFixture(VillageLayoutShape.OneByTwo, 5);
            var result = Compile(fixture);
            AssertSuccess(result);
            var first = result.VariantSet.Variants[0];

            foreach (var snapshot in result.VariantSet.Variants)
            {
                Assert.That(snapshot.VillageShellDigest, Is.EqualTo(fixture.Shell.CanonicalDigest));
                Assert.That(snapshot.RoadDigest, Is.EqualTo(fixture.Shell.RoadDigest));
                Assert.That(snapshot.FacilityDigest, Is.EqualTo(fixture.Shell.FacilityDigest));
                Assert.That(snapshot.AccessDigest, Is.EqualTo(fixture.Shell.AccessDigest));
                Assert.That(snapshot.RoadWitnessDigest, Is.EqualTo(first.RoadWitnessDigest));
                Assert.That(snapshot.FacilityCoordinateDigest, Is.EqualTo(first.FacilityCoordinateDigest));
                Assert.That(snapshot.FacilityWitnessDigest, Is.EqualTo(first.FacilityWitnessDigest));
                Assert.That(Identity(snapshot.NpcMarkers), Is.EqualTo(Identity(first.NpcMarkers)));
                Assert.That(Identity(snapshot.InventoryMarkers), Is.EqualTo(Identity(first.InventoryMarkers)));
                Assert.That(Identity(snapshot.DoorMarkers), Is.EqualTo(Identity(first.DoorMarkers)));
            }
        }

        [Test]
        public void DoorStatesNeverClaimCollisionLockOpenCloseOrPathBlocking()
        {
            var result = Compile(BuildFixture(VillageLayoutShape.TwoByOne, 6));
            AssertSuccess(result);
            foreach (var marker in result.VariantSet.Variants.SelectMany(value => value.DoorMarkers))
            {
                Assert.That(marker.OwnsCollision || marker.OwnsLock || marker.OwnsOpenClose || marker.BlocksPath, Is.False);
                Assert.That(marker.CollisionWriteCount + marker.LockWriteCount + marker.PathBlockingWriteCount, Is.Zero);
            }
        }

        [Test]
        public void MissingDuplicateUnknownDoorMismatchAndInvalidTargetFailAtomically()
        {
            var fixture = BuildFixture(VillageLayoutShape.OneByOne, 5);
            AssertFailure(Compile(fixture, Definition(fixture, npc: Array.Empty<VillageNpcMarkerDefinition>())),
                VillageStateVariantErrorCode.MissingMarkerKind);

            var duplicateNpc = fixture.Definition.NpcMarkers.Concat(new[] { fixture.Definition.NpcMarkers[0] }).ToArray();
            AssertFailure(Compile(fixture, Definition(fixture, npc: duplicateNpc)),
                VillageStateVariantErrorCode.DuplicateMarker);

            var unknownInventory = fixture.Definition.InventoryMarkers.ToArray();
            unknownInventory[0] = new VillageInventoryMarkerDefinition("VILLAGE_INVENTORY_UNKNOWN", "UNKNOWN_FACILITY");
            AssertFailure(Compile(fixture, Definition(fixture, inventory: unknownInventory)),
                VillageStateVariantErrorCode.UnknownFacilityBinding);

            var doorMismatch = fixture.Definition.DoorMarkers.ToArray();
            doorMismatch[0] = new VillageDoorMarkerDefinition(
                doorMismatch[0].MarkerId, doorMismatch[0].FacilityBindingId, new LocalTileCoord(1, 1));
            AssertFailure(Compile(fixture, Definition(fixture, doors: doorMismatch)),
                VillageStateVariantErrorCode.DoorBindingMismatch);

            AssertFailure(Compile(fixture, Definition(fixture, target: "VILLAGE_NPC_UNKNOWN")),
                VillageStateVariantErrorCode.UnknownIndividualTarget);
        }

        [Test]
        public void DigestNonVillageAndIncompleteVariantRequestsFailAtomically()
        {
            var fixture = BuildFixture(VillageLayoutShape.OneByOne, 5);
            var digestMismatch = new VillageStateVariantCompileRequest(
                SpecialRegionKind.Village, fixture.Shell, "wrong", fixture.Definition);
            AssertFailure(VillageStateVariantCompiler.Compile(digestMismatch), VillageStateVariantErrorCode.DigestMismatch);

            var nonVillage = new VillageStateVariantCompileRequest(
                SpecialRegionKind.CoreResource, fixture.Shell, fixture.Shell.CanonicalDigest, fixture.Definition);
            AssertFailure(VillageStateVariantCompiler.Compile(nonVillage), VillageStateVariantErrorCode.NotVillage);

            var incomplete = new[] { VillageStateKind.Normal, VillageStateKind.Friendly };
            AssertFailure(Compile(fixture, Definition(fixture, variants: incomplete)),
                VillageStateVariantErrorCode.MissingVariant);

            var duplicate = new[]
            {
                VillageStateKind.Normal,
                VillageStateKind.Friendly,
                VillageStateKind.IndividualHostile,
                VillageStateKind.AllHostile,
                VillageStateKind.Evacuation,
                VillageStateKind.Normal,
            };
            AssertFailure(Compile(fixture, Definition(fixture, variants: duplicate)),
                VillageStateVariantErrorCode.DuplicateVariant);
        }

        [Test]
        public void ReverseRepeatCultureImmutabilityAndMutationCountersAreStable()
        {
            var previousCulture = CultureInfo.CurrentCulture;
            var randomState = UnityEngine.Random.state;
            try
            {
                var fixture = BuildFixture(VillageLayoutShape.TwoByOne, 6);
                var first = Compile(fixture);
                var repeat = Compile(fixture);
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                var reversed = Compile(BuildFixture(VillageLayoutShape.TwoByOne, 6, reverse: true));

                AssertSuccess(first);
                AssertSuccess(repeat);
                AssertSuccess(reversed);
                Assert.That(repeat.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));
                Assert.That(reversed.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));
                Assert.That(UnityEngine.Random.state, Is.EqualTo(randomState));
                Assert.That(MutationCount(first.VariantSet), Is.Zero);
                Assert.That(first.VariantSet.Variants.Sum(MutationCount), Is.Zero);
                Assert.Throws<NotSupportedException>(() =>
                    ((IList<VillageStateVariantSnapshot>)first.VariantSet.Variants).Clear());
                Assert.Throws<NotSupportedException>(() =>
                    ((IList<VillageNpcMarkerSnapshot>)first.VariantSet.Variants[0].NpcMarkers).Clear());
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                UnityEngine.Random.state = randomState;
            }
        }

        [Test]
        public void EmptyMarkerKindsInsufficientNpcAndMissingTargetAccumulateStableErrors()
        {
            var fixture = BuildFixture(VillageLayoutShape.OneByOne, 5);
            var invalid = new VillageStateMarkerSetDefinition(
                new[] { fixture.Definition.NpcMarkers[0] },
                Array.Empty<VillageInventoryMarkerDefinition>(),
                Array.Empty<VillageDoorMarkerDefinition>(), string.Empty);
            var first = Compile(fixture, invalid);
            var second = Compile(fixture, invalid);

            Assert.That(first.Success, Is.False);
            Assert.That(first.VariantSet, Is.Null);
            Assert.That(first.CanonicalDigest, Is.Empty);
            Assert.That(first.Errors.Select(value => value.Code), Does.Contain(VillageStateVariantErrorCode.InsufficientNpcMarkers));
            Assert.That(first.Errors.Select(value => value.Code), Does.Contain(VillageStateVariantErrorCode.MissingIndividualTarget));
            Assert.That(first.Errors, Is.Ordered);
            Assert.That(first.Errors.Distinct().Count(), Is.EqualTo(first.Errors.Count));
            Assert.That(second.Errors.Select(value => value.ToString()),
                Is.EqualTo(first.Errors.Select(value => value.ToString())));
        }

        [Test]
        public void NullRequestAndShellInvariantCorruptionRemainAtomic()
        {
            AssertFailure(VillageStateVariantCompiler.Compile(null), VillageStateVariantErrorCode.MissingInput);
            var fixture = BuildFixture(VillageLayoutShape.OneByOne, 5);
            SetBackingField(fixture.Shell, "RoadDigest", "tampered");
            AssertFailure(Compile(fixture), VillageStateVariantErrorCode.ShellInvariantViolation);
        }

        private static VillageStateVariantResult Compile(
            Fixture fixture,
            VillageStateMarkerSetDefinition definition = null)
            => VillageStateVariantCompiler.Compile(new VillageStateVariantCompileRequest(
                SpecialRegionKind.Village, fixture.Shell, fixture.Shell.CanonicalDigest,
                definition ?? fixture.Definition));

        private static Fixture BuildFixture(
            VillageLayoutShape shape,
            int facilityCount,
            bool reverse = false)
        {
            Dimensions(shape, out var width, out var height);
            var origin = new SectorCoord(5, 5);
            var regionId = new SpecialRegionId("SR_MAP13_05_VILLAGE");
            var reservationId = new SiteReservationId("SITE_MAP13_05_VILLAGE");
            var offsets = RectangleOffsets(width, height).ToArray();
            var sectors = offsets.Select((offset, index) => new SpecialRegionSiteSectorBinding(
                offset, offset, new SectorCoord(origin.X + offset.X, origin.Y + offset.Y),
                index, "VILLAGE_SECTOR_" + index)).ToArray();
            var bridge = CreateInternal<SpecialRegionSiteBridge>(
                regionId, SpecialRegionKind.Village, reservationId, SiteReservationKind.Village,
                "SPECIAL_VILLAGE", origin, width, height, SiteFootprintTransform.R0,
                reverse ? offsets.Reverse() : offsets, reverse ? offsets.Reverse() : offsets,
                reverse ? sectors.Reverse() : sectors,
                Array.Empty<SpecialRegionSiteFixedShellBinding>(),
                Array.Empty<SpecialRegionSiteSlotBinding>(), Array.Empty<SpecialRegionSitePortBinding>(),
                "RESERVATION_IDENTITY_MAP13_05", "CONTRACT_DIGEST_MAP13_05");
            SetCanonicalDigest(bridge, SpecialRegionSiteBridgeCanonicalDigest.Compute(bridge));

            var entry = CreateInternal<SpecialRegionEntryBufferPlan>(
                bridge, null, null, Array.Empty<SpecialRegionEntryApron>(),
                Array.Empty<SpecialRegionQuietChunkBinding>(), null);
            SetCanonicalDigest(entry, "ENTRY_BUFFER_DIGEST_MAP13_05");
            var fixedSlots = CreateInternal<SpecialRegionFixedSlotLayerPlan>(
                regionId, SpecialRegionKind.Village, reservationId, "CONTRACT_DIGEST_MAP13_05",
                bridge.CanonicalDigest, entry.CanonicalDigest, "COLLISION_DIGEST_MAP13_05",
                Array.Empty<SpecialRegionFixedCollisionCell>(), Array.Empty<SpecialRegionFixedAccessBinding>(),
                Array.Empty<SpecialRegionReplaceableSlotBinding>(), Array.Empty<SpecialRegionOccupancyClaim>());

            var roadTiles = Road(shape).ToArray();
            var road = roadTiles.Select((tile, order) => CreateInternal<VillageRoadCell>(
                order, tile, Place(origin, tile), true)).ToArray();
            var definitions = new List<VillageFacilityDefinition>();
            var witnesses = new List<VillageFacilityAccessWitness>();
            var bindings = new List<VillageFacilityBinding>();
            foreach (var pair in FacilityPositions(shape, facilityCount).Select((tile, index) => new { tile, index }))
            {
                var slotId = new SpecialRegionSlotId("SR_SLOT_VILLAGE_STATE_" + pair.index);
                var slotPlaced = Place(origin, pair.tile);
                var source = new SpecialRegionSiteSlotBinding(
                    slotId, SpecialRegionSlotKind.Facility, false, SpecialPersistenceScope.Slot,
                    SpecialPersistenceKey.ForSlot(regionId, SpecialPersistenceScope.Slot, slotId),
                    new SpecialRegionAuthoredCoordinate(slotPlaced.SectorOffset, slotPlaced.LocalTile), slotPlaced);
                var slot = CreateInternal<SpecialRegionReplaceableSlotBinding>(source,
                    SpecialRegionSlotReplacementIntent.Assign(
                        slotId, SpecialRegionSlotKind.Facility, "VILLAGE_OCCUPANT_" + pair.index));
                var kind = pair.index == 0 ? VillageFacilityKind.Kitchen :
                    pair.index == 1 ? VillageFacilityKind.Repair : VillageFacilityKind.Optional;
                var requirement = pair.index < 2
                    ? VillageFacilityRequirement.Required : VillageFacilityRequirement.Optional;
                var id = pair.index == 0 ? "VILLAGE_FACILITY_KITCHEN" :
                    pair.index == 1 ? "VILLAGE_FACILITY_REPAIR" : "VILLAGE_FACILITY_OPTIONAL_" + (pair.index - 2);
                var doorTile = Door(shape, pair.tile);
                var roadTile = RoadNeighbor(shape, pair.tile);
                var facility = new VillageFacilityDefinition(
                    id, kind, requirement, slotId, "VILLAGE_OCCUPANT_" + pair.index, doorTile);
                var witness = new VillageFacilityAccessWitness(
                    "VILLAGE_ACCESS_" + pair.index, id, new[] { doorTile, roadTile });
                definitions.Add(facility);
                witnesses.Add(witness);
                bindings.Add(CreateInternal<VillageFacilityBinding>(
                    facility, slot, Place(origin, doorTile), witness,
                    new[] { Place(origin, doorTile), Place(origin, roadTile) }));
            }

            var shellDefinition = new VillageShellDefinition(
                new VillageLayoutId("VILLAGE_LAYOUT_MAP13_05"), shape,
                reverse ? road.Select(value => new VillageRoadCell(value.Order, value.RegionTile)).Reverse() :
                    road.Select(value => new VillageRoadCell(value.Order, value.RegionTile)),
                reverse ? definitions.AsEnumerable().Reverse() : definitions,
                reverse ? witnesses.AsEnumerable().Reverse() : witnesses);
            var shell = CreateInternal<VillageShellPlan>(
                bridge, entry, fixedSlots, shellDefinition,
                reverse ? road.AsEnumerable().Reverse() : road,
                reverse ? bindings.AsEnumerable().Reverse() : bindings);

            var facilityIds = shell.FacilityBindings.Select(value => value.Definition.DefinitionId).ToArray();
            var npc = new[]
            {
                new VillageNpcMarkerDefinition("VILLAGE_NPC_0", facilityIds[0], "display excluded"),
                new VillageNpcMarkerDefinition("VILLAGE_NPC_1", facilityIds[1], "display excluded"),
                new VillageNpcMarkerDefinition("VILLAGE_NPC_2", facilityIds[2], "display excluded"),
            };
            var inventory = new[]
            {
                new VillageInventoryMarkerDefinition("VILLAGE_INVENTORY_0", facilityIds[0]),
                new VillageInventoryMarkerDefinition("VILLAGE_INVENTORY_1", facilityIds[1]),
            };
            var doors = shell.FacilityBindings.Select((binding, index) => new VillageDoorMarkerDefinition(
                "VILLAGE_DOOR_" + index, binding.Definition.DefinitionId, binding.Door.RegionTile)).ToArray();
            var variants = new[]
            {
                VillageStateKind.Normal,
                VillageStateKind.Friendly,
                VillageStateKind.IndividualHostile,
                VillageStateKind.AllHostile,
                VillageStateKind.Evacuation,
            };
            var markerSet = new VillageStateMarkerSetDefinition(
                reverse ? npc.Reverse() : npc,
                reverse ? inventory.Reverse() : inventory,
                reverse ? doors.Reverse() : doors,
                "VILLAGE_NPC_1", reverse ? variants.Reverse() : variants, "display excluded");
            return new Fixture(shell, markerSet);
        }

        private static VillageStateMarkerSetDefinition Definition(
            Fixture fixture,
            IEnumerable<VillageNpcMarkerDefinition> npc = null,
            IEnumerable<VillageInventoryMarkerDefinition> inventory = null,
            IEnumerable<VillageDoorMarkerDefinition> doors = null,
            string target = null,
            IEnumerable<VillageStateKind> variants = null)
            => new VillageStateMarkerSetDefinition(
                npc ?? fixture.Definition.NpcMarkers,
                inventory ?? fixture.Definition.InventoryMarkers,
                doors ?? fixture.Definition.DoorMarkers,
                target ?? fixture.Definition.IndividualHostileTargetMarkerId,
                variants ?? fixture.Definition.RequestedVariants,
                fixture.Definition.DisplayText);

        private static IEnumerable<SpecialRegionSectorOffset> RectangleOffsets(int width, int height)
        {
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                    yield return new SpecialRegionSectorOffset(x, y);
        }

        private static IEnumerable<LocalTileCoord> Road(VillageLayoutShape shape)
        {
            if (shape == VillageLayoutShape.OneByTwo)
            {
                for (var y = 0; y < 64; y++) yield return new LocalTileCoord(24, y);
                yield break;
            }
            var width = shape == VillageLayoutShape.TwoByOne ? 96 : 48;
            for (var x = 0; x < width; x++) yield return new LocalTileCoord(x, 16);
        }

        private static IEnumerable<LocalTileCoord> FacilityPositions(VillageLayoutShape shape, int count)
        {
            var values = count == 6 ? new[] { 5, 12, 20, 28, 36, 43 } : new[] { 5, 12, 20, 28, 36 };
            if (shape == VillageLayoutShape.TwoByOne)
                values = count == 6 ? new[] { 5, 20, 35, 55, 72, 88 } : new[] { 5, 20, 40, 60, 80 };
            foreach (var value in values)
                yield return shape == VillageLayoutShape.OneByTwo
                    ? new LocalTileCoord(22, value) : new LocalTileCoord(value, 14);
        }

        private static LocalTileCoord Door(VillageLayoutShape shape, LocalTileCoord slot)
            => shape == VillageLayoutShape.OneByTwo
                ? new LocalTileCoord(slot.X + 1, slot.Y) : new LocalTileCoord(slot.X, slot.Y + 1);
        private static LocalTileCoord RoadNeighbor(VillageLayoutShape shape, LocalTileCoord slot)
            => shape == VillageLayoutShape.OneByTwo
                ? new LocalTileCoord(slot.X + 2, slot.Y) : new LocalTileCoord(slot.X, slot.Y + 2);

        private static SpecialRegionPlacedCoordinate Place(SectorCoord origin, LocalTileCoord regionTile)
        {
            var offset = new SpecialRegionSectorOffset(
                regionTile.X / WorldGenConstants.SectorWidthTiles,
                regionTile.Y / WorldGenConstants.SectorHeightTiles);
            var local = new LocalTileCoord(
                regionTile.X % WorldGenConstants.SectorWidthTiles,
                regionTile.Y % WorldGenConstants.SectorHeightTiles);
            return new SpecialRegionPlacedCoordinate(offset,
                new SectorCoord(origin.X + offset.X, origin.Y + offset.Y), local, regionTile);
        }

        private static void Dimensions(VillageLayoutShape shape, out int width, out int height)
        {
            width = shape == VillageLayoutShape.TwoByOne ? 2 : 1;
            height = shape == VillageLayoutShape.OneByTwo ? 2 : 1;
        }

        private static string[] Identity(IEnumerable<VillageNpcMarkerSnapshot> markers)
            => markers.Select(value => value.MarkerId + "/" + value.FacilityBindingId + "/" +
                value.SourceCoordinate.RegionTile.X + "," + value.SourceCoordinate.RegionTile.Y).ToArray();
        private static string[] Identity(IEnumerable<VillageInventoryMarkerSnapshot> markers)
            => markers.Select(value => value.MarkerId + "/" + value.FacilityBindingId + "/" +
                value.SourceCoordinate.RegionTile.X + "," + value.SourceCoordinate.RegionTile.Y).ToArray();
        private static string[] Identity(IEnumerable<VillageDoorMarkerSnapshot> markers)
            => markers.Select(value => value.MarkerId + "/" + value.FacilityBindingId + "/" +
                value.SourceCoordinate.RegionTile.X + "," + value.SourceCoordinate.RegionTile.Y).ToArray();

        private static int MutationCount(VillageStateVariantSet value)
            => value.FixedCollisionWriteCount + value.FixedAccessWriteCount + value.GeometryWriteCount +
               value.AccessWriteCount + value.PersistenceWriteCount + value.RandomSelectionCount +
               value.WorldMutationCount + value.TileMutationCount + value.SceneMutationCount + value.PrefabMutationCount;
        private static int MutationCount(VillageStateVariantSnapshot value)
            => value.FixedCollisionWriteCount + value.FixedAccessWriteCount + value.RoadWriteCount +
               value.PathWriteCount + value.CarveWriteCount + value.FacilityCoordinateWriteCount +
               value.SlotOccupantWriteCount + value.PersistenceWriteCount + value.RandomSelectionCount +
               value.WorldMutationCount + value.TileMutationCount + value.SceneMutationCount + value.PrefabMutationCount;

        private static void AssertStates(
            VillageStateVariantResult result,
            VillageStateKind kind,
            VillageNpcMarkerState npc,
            VillageInventoryMarkerState inventory,
            VillageDoorMarkerState door)
        {
            var snapshot = result.VariantSet.Variants.Single(value => value.StateKind == kind);
            Assert.That(snapshot.NpcMarkers.All(value => value.State == npc), Is.True);
            Assert.That(snapshot.InventoryMarkers.All(value => value.State == inventory), Is.True);
            Assert.That(snapshot.DoorMarkers.All(value => value.State == door), Is.True);
        }

        private static T CreateInternal<T>(params object[] arguments)
        {
            var constructor = typeof(T).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Single(value => value.GetParameters().Length == arguments.Length);
            return (T)constructor.Invoke(arguments);
        }

        private static void SetCanonicalDigest(object target, string value)
            => target.GetType().GetProperty("CanonicalDigest", BindingFlags.Instance | BindingFlags.Public)
                .SetValue(target, value, null);

        private static void SetBackingField(object target, string propertyName, object value)
            => target.GetType().GetField("<" + propertyName + ">k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private static void AssertSuccess(VillageStateVariantResult result)
        {
            Assert.That(result.Success, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.VariantSet, Is.Not.Null);
            Assert.That(result.CanonicalDigest, Is.Not.Empty);
        }

        private static void AssertFailure(VillageStateVariantResult result, VillageStateVariantErrorCode code)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.VariantSet, Is.Null);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(code), string.Join("\n", result.Errors));
        }

        private sealed class Fixture
        {
            public Fixture(VillageShellPlan shell, VillageStateMarkerSetDefinition definition)
            {
                Shell = shell;
                Definition = definition;
            }

            public VillageShellPlan Shell { get; }
            public VillageStateMarkerSetDefinition Definition { get; }
        }
    }
}
