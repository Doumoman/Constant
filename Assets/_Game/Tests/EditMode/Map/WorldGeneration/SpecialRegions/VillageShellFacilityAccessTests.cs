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
    [Category("MAP13_04")]
    public sealed class VillageShellFacilityAccessTests
    {
        [TestCase(VillageLayoutShape.OneByOne, 48, 32, 3)]
        [TestCase(VillageLayoutShape.TwoByOne, 96, 32, 4)]
        [TestCase(VillageLayoutShape.OneByTwo, 48, 64, 3)]
        public void ThreeShapesPublishExactBoundsRoadSeamsAndFacilityMatrix(
            VillageLayoutShape shape,
            int width,
            int height,
            int optionalCount)
        {
            var fixture = BuildFixture(shape, optionalCount);
            var result = Compile(fixture);

            AssertSuccess(result);
            Assert.That(result.Plan.WidthTiles, Is.EqualTo(width));
            Assert.That(result.Plan.HeightTiles, Is.EqualTo(height));
            Assert.That(result.Plan.RoadCells.All(value => value.RegionTile.X >= 0 && value.RegionTile.X < width &&
                value.RegionTile.Y >= 0 && value.RegionTile.Y < height), Is.True);
            Assert.That(result.Plan.RoadCells.All(value => value.HasProjection &&
                value.RegionTile == value.Placed.RegionTile), Is.True);
            Assert.That(result.Plan.FacilityBindings.Count, Is.EqualTo(optionalCount + 2));
            Assert.That(result.Plan.FacilityBindings.Count(value => value.Definition.Kind == VillageFacilityKind.Kitchen), Is.EqualTo(1));
            Assert.That(result.Plan.FacilityBindings.Count(value => value.Definition.Kind == VillageFacilityKind.Repair), Is.EqualTo(1));
            Assert.That(result.Plan.FacilityBindings.Count(value => value.Definition.Kind == VillageFacilityKind.Optional), Is.EqualTo(optionalCount));
            Assert.That(result.Plan.RoadCells.Select(value => new
            {
                X = value.RegionTile.X / WorldGenConstants.SectorWidthTiles,
                Y = value.RegionTile.Y / WorldGenConstants.SectorHeightTiles,
            }).Distinct().Count(), Is.EqualTo(shape == VillageLayoutShape.OneByOne ? 1 : 2));

            if (shape == VillageLayoutShape.TwoByOne)
                Assert.That(HasPair(result.Plan.RoadCells.Select(value => value.RegionTile),
                    new LocalTileCoord(47, 16), new LocalTileCoord(48, 16)), Is.True);
            if (shape == VillageLayoutShape.OneByTwo)
                Assert.That(HasPair(result.Plan.RoadCells.Select(value => value.RegionTile),
                    new LocalTileCoord(24, 31), new LocalTileCoord(24, 32)), Is.True);
        }

        [Test]
        public void EveryFacilityPublishesForwardAndReverseMandatoryNoToolWitness()
        {
            var result = Compile(BuildFixture(VillageLayoutShape.TwoByOne, 4));

            AssertSuccess(result);
            Assert.That(result.Plan.RoadAccess.AccessClass, Is.EqualTo(AccessClass.MandatoryNoTool));
            Assert.That(result.Plan.RoadAccess.Reverse.Select(value => value.RegionTile),
                Is.EqualTo(result.Plan.RoadAccess.Forward.Reverse().Select(value => value.RegionTile)));
            foreach (var binding in result.Plan.FacilityBindings)
            {
                Assert.That(binding.AccessClass, Is.EqualTo(AccessClass.MandatoryNoTool));
                Assert.That(binding.AccessCells.First(), Is.EqualTo(binding.Door));
                Assert.That(result.Plan.RoadCells.Select(value => value.RegionTile),
                    Does.Contain(binding.AccessCells.Last().RegionTile));
                Assert.That(binding.ReverseAccessCells, Is.EqualTo(binding.AccessCells.Reverse()));
                Assert.That(binding.ToolRequirementCount + binding.SyntheticEdgeCount +
                    binding.TeleportCount + binding.CarveCount, Is.Zero);
                Assert.That(binding.ClaimsRuntimePhysics, Is.False);
            }
        }

        [Test]
        public void RequiredClearIsRejectedWhileOptionalAssignedAndExplicitEmptyAreAccepted()
        {
            var valid = Compile(BuildFixture(VillageLayoutShape.OneByOne, 4));
            AssertSuccess(valid);
            Assert.That(valid.Plan.FacilityBindings.Where(value => value.Definition.Kind == VillageFacilityKind.Optional)
                .Any(value => value.IsAssigned), Is.True);
            Assert.That(valid.Plan.FacilityBindings.Where(value => value.Definition.Kind == VillageFacilityKind.Optional)
                .Any(value => value.IsExplicitlyEmpty), Is.True);

            var cleared = BuildFixture(VillageLayoutShape.OneByOne, 3, requiredClear: true);
            AssertFailure(Compile(cleared), VillageShellErrorCode.RequiredFacilityClear);
        }

        [Test]
        public void InvalidRoadVariantsFailAtomically()
        {
            var fixture = BuildFixture(VillageLayoutShape.OneByOne, 3);
            AssertFailure(Compile(fixture, Definition(fixture, road: Array.Empty<VillageRoadCell>())),
                VillageShellErrorCode.InvalidRoad);

            var disconnected = fixture.Definition.RoadCells.Select(value =>
                value.Order == 5 ? new VillageRoadCell(value.Order, new LocalTileCoord(20, 20)) : value).ToArray();
            AssertFailure(Compile(fixture, Definition(fixture, road: disconnected)),
                VillageShellErrorCode.DisconnectedRoad);

            var outside = fixture.Definition.RoadCells.Select(value =>
                value.Order == 5 ? new VillageRoadCell(value.Order, new LocalTileCoord(48, 16)) : value).ToArray();
            AssertFailure(Compile(fixture, Definition(fixture, road: outside)),
                VillageShellErrorCode.CoordinateOutOfRange);

            var collision = fixture.Definition.RoadCells.Select(value =>
                value.Order == 6 ? new VillageRoadCell(value.Order, fixture.FixedCollision) : value).ToArray();
            AssertFailure(Compile(fixture, Definition(fixture, road: collision)),
                VillageShellErrorCode.RoadCollision);
        }

        [Test]
        public void InvalidAccessAndDoorCollisionsFailAtomically()
        {
            var fixture = BuildFixture(VillageLayoutShape.OneByOne, 3);
            var kitchen = fixture.Definition.Facilities.Single(value => value.Kind == VillageFacilityKind.Kitchen);
            var collidingWitness = new VillageFacilityAccessWitness(
                "VILLAGE_ACCESS_KITCHEN", kitchen.DefinitionId,
                new[] { kitchen.DoorRegionTile, fixture.FixedCollision, new LocalTileCoord(6, 16) });
            var witnesses = fixture.Definition.AccessWitnesses
                .Select(value => value.FacilityDefinitionId == kitchen.DefinitionId ? collidingWitness : value).ToArray();
            AssertFailure(Compile(fixture, Definition(fixture, witnesses: witnesses)),
                VillageShellErrorCode.InvalidAccessWitness);

            var invalidDoorFacilities = fixture.Definition.Facilities.Select(value =>
                value.Kind == VillageFacilityKind.Kitchen
                    ? Copy(value, door: fixture.FixedCollision)
                    : value).ToArray();
            AssertFailure(Compile(fixture, Definition(fixture, facilities: invalidDoorFacilities)),
                VillageShellErrorCode.InvalidDoor);

            var missingRoadWitnesses = fixture.Definition.AccessWitnesses.Select(value =>
                value.FacilityDefinitionId == kitchen.DefinitionId
                    ? new VillageFacilityAccessWitness(value.WitnessId, value.FacilityDefinitionId,
                        new[] { kitchen.DoorRegionTile, new LocalTileCoord(6, 15) })
                    : value).ToArray();
            AssertFailure(Compile(fixture, Definition(fixture, witnesses: missingRoadWitnesses)),
                VillageShellErrorCode.FacilityCannotReturnToRoad);
        }

        [Test]
        public void NonVillageShapeMismatchMissingAndDuplicateFacilitiesFailAtomically()
        {
            AssertFailure(Compile(BuildFixture(VillageLayoutShape.OneByOne, 3,
                regionKind: SpecialRegionKind.CoreResource)), VillageShellErrorCode.NotVillage);

            var fixture = BuildFixture(VillageLayoutShape.TwoByOne, 3);
            AssertFailure(Compile(fixture, Definition(fixture, shape: VillageLayoutShape.OneByOne)),
                VillageShellErrorCode.ShapeMismatch);

            var noKitchen = fixture.Definition.Facilities
                .Where(value => value.Kind != VillageFacilityKind.Kitchen).ToArray();
            AssertFailure(Compile(fixture, Definition(fixture, facilities: noKitchen)),
                VillageShellErrorCode.MissingKitchen);

            var duplicate = fixture.Definition.Facilities.ToArray();
            duplicate[1] = new VillageFacilityDefinition(
                duplicate[0].DefinitionId, duplicate[1].Kind, duplicate[1].Requirement,
                duplicate[0].SlotId, duplicate[1].OccupantId, duplicate[1].DoorRegionTile);
            AssertFailure(Compile(fixture, Definition(fixture, facilities: duplicate)),
                VillageShellErrorCode.DuplicateFacility);
        }

        [Test]
        public void EnumerationCultureRepeatAndCollectionsAreCanonicalAndImmutable()
        {
            var previous = CultureInfo.CurrentCulture;
            var randomState = UnityEngine.Random.state;
            try
            {
                var forwardFixture = BuildFixture(VillageLayoutShape.OneByTwo, 4);
                var first = Compile(forwardFixture);
                var repeat = Compile(forwardFixture);
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                var reverse = Compile(BuildFixture(VillageLayoutShape.OneByTwo, 4, reverse: true));

                AssertSuccess(first);
                AssertSuccess(repeat);
                AssertSuccess(reverse);
                Assert.That(repeat.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));
                Assert.That(reverse.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));
                Assert.That(UnityEngine.Random.state, Is.EqualTo(randomState));
                Assert.That(first.Plan.RandomSelectionCount + first.Plan.WorldMutationCount +
                    first.Plan.TileMutationCount + first.Plan.PlacementWriteCount +
                    first.Plan.SpawnCount + first.Plan.DespawnCount, Is.Zero);
                Assert.Throws<NotSupportedException>(() =>
                    ((IList<VillageRoadCell>)first.Plan.RoadCells).Add(new VillageRoadCell(999, default(LocalTileCoord))));
                Assert.Throws<NotSupportedException>(() =>
                    ((IList<VillageFacilityBinding>)first.Plan.FacilityBindings).Clear());
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
                UnityEngine.Random.state = randomState;
            }
        }

        [Test]
        public void SourceDigestMismatchFailsWithNoPlanOrDigest()
        {
            var fixture = BuildFixture(VillageLayoutShape.OneByOne, 3);
            var request = new VillageShellCompileRequest(
                fixture.Bridge, "wrong", fixture.Entry, fixture.Entry.CanonicalDigest,
                fixture.FixedSlots, fixture.FixedSlots.CanonicalDigest, fixture.Definition);
            AssertFailure(VillageShellFacilityCompiler.Compile(request), VillageShellErrorCode.DigestMismatch);
        }

        [Test]
        public void ErrorsAreAccumulatedDeduplicatedAndStableSorted()
        {
            var fixture = BuildFixture(VillageLayoutShape.OneByOne, 3);
            var facilities = fixture.Definition.Facilities.Where(value => value.Kind == VillageFacilityKind.Optional).Reverse().ToArray();
            var invalid = Definition(fixture, road: Array.Empty<VillageRoadCell>(), facilities: facilities,
                witnesses: Array.Empty<VillageFacilityAccessWitness>());
            var first = Compile(fixture, invalid);
            var second = Compile(fixture, invalid);

            Assert.That(first.Success, Is.False);
            Assert.That(first.Plan, Is.Null);
            Assert.That(first.CanonicalDigest, Is.Empty);
            Assert.That(first.Errors.Count, Is.GreaterThan(3));
            Assert.That(first.Errors, Is.Ordered);
            Assert.That(first.Errors.Distinct().Count(), Is.EqualTo(first.Errors.Count));
            Assert.That(second.Errors.Select(value => value.ToString()),
                Is.EqualTo(first.Errors.Select(value => value.ToString())));
        }

        [Test]
        public void MissingInputAndUnsupportedShapeRemainAtomic()
        {
            AssertFailure(VillageShellFacilityCompiler.Compile(null), VillageShellErrorCode.MissingInput);
            var fixture = BuildFixture(VillageLayoutShape.OneByOne, 3);
            AssertFailure(Compile(fixture, Definition(fixture, shape: (VillageLayoutShape)99)),
                VillageShellErrorCode.UnsupportedShape);
        }

        private static VillageShellResult Compile(Fixture fixture, VillageShellDefinition definition = null)
            => VillageShellFacilityCompiler.Compile(new VillageShellCompileRequest(
                fixture.Bridge, fixture.Bridge.CanonicalDigest,
                fixture.Entry, fixture.Entry.CanonicalDigest,
                fixture.FixedSlots, fixture.FixedSlots.CanonicalDigest,
                definition ?? fixture.Definition));

        private static Fixture BuildFixture(
            VillageLayoutShape shape,
            int optionalCount,
            bool reverse = false,
            bool requiredClear = false,
            SpecialRegionKind regionKind = SpecialRegionKind.Village)
        {
            Dimensions(shape, out var width, out var height);
            var origin = new SectorCoord(5, 5);
            var regionId = new SpecialRegionId("SR_MAP13_04_VILLAGE");
            var reservationId = new SiteReservationId("SITE_MAP13_04_VILLAGE");
            var offsets = RectangleOffsets(width, height).ToArray();
            var sectors = offsets.Select((offset, index) => new SpecialRegionSiteSectorBinding(
                offset, offset, new SectorCoord(origin.X + offset.X, origin.Y + offset.Y),
                index, "VILLAGE_SECTOR_" + index)).ToArray();

            var facilityPositions = FacilityPositions(shape, optionalCount + 2).ToArray();
            var slotSources = new List<SpecialRegionSiteSlotBinding>();
            var slotBindings = new List<SpecialRegionReplaceableSlotBinding>();
            var facilityDefinitions = new List<VillageFacilityDefinition>();
            var accessWitnesses = new List<VillageFacilityAccessWitness>();
            for (var index = 0; index < facilityPositions.Length; index++)
            {
                var slotId = new SpecialRegionSlotId("SR_SLOT_VILLAGE_FACILITY_" + index);
                var placed = Place(origin, facilityPositions[index]);
                var source = new SpecialRegionAuthoredCoordinate(placed.SectorOffset, placed.LocalTile);
                var slotSource = new SpecialRegionSiteSlotBinding(
                    slotId, SpecialRegionSlotKind.Facility, false, SpecialPersistenceScope.Slot,
                    SpecialPersistenceKey.ForSlot(regionId, SpecialPersistenceScope.Slot, slotId), source, placed);
                slotSources.Add(slotSource);

                var kind = index == 0 ? VillageFacilityKind.Kitchen :
                    index == 1 ? VillageFacilityKind.Repair : VillageFacilityKind.Optional;
                var requirement = index < 2 ? VillageFacilityRequirement.Required : VillageFacilityRequirement.Optional;
                var assigned = index < 2 ? !requiredClear || index != 0 : index % 2 == 0;
                var occupant = assigned ? "VILLAGE_OCCUPANT_" + index : string.Empty;
                var replacement = assigned
                    ? SpecialRegionSlotReplacementIntent.Assign(slotId, SpecialRegionSlotKind.Facility, occupant)
                    : SpecialRegionSlotReplacementIntent.Clear(slotId);
                slotBindings.Add(CreateInternal<SpecialRegionReplaceableSlotBinding>(slotSource, replacement));

                var door = Door(shape, facilityPositions[index]);
                var road = RoadNeighbor(shape, facilityPositions[index]);
                var definitionId = index == 0 ? "VILLAGE_FACILITY_KITCHEN" :
                    index == 1 ? "VILLAGE_FACILITY_REPAIR" : "VILLAGE_FACILITY_OPTIONAL_" + (index - 2);
                facilityDefinitions.Add(new VillageFacilityDefinition(
                    definitionId, kind, requirement, slotId, occupant, door, "display text excluded"));
                accessWitnesses.Add(new VillageFacilityAccessWitness(
                    "VILLAGE_ACCESS_" + index, definitionId, new[] { door, road }));
            }

            var entryRegion = shape == VillageLayoutShape.OneByTwo
                ? new LocalTileCoord(24, 0) : new LocalTileCoord(0, 16);
            var returnRegion = shape == VillageLayoutShape.OneByTwo
                ? new LocalTileCoord(24, 63) : new LocalTileCoord(width * WorldGenConstants.SectorWidthTiles - 1, 16);
            var entrySide = shape == VillageLayoutShape.OneByTwo ? SiteEntrySide.D : SiteEntrySide.L;
            var returnSide = shape == VillageLayoutShape.OneByTwo ? SiteEntrySide.U : SiteEntrySide.R;
            var entryPort = PortSource(regionId, origin, "VILLAGE_PORT_ENTRY", "VILLAGE_ENTRY_SOCKET",
                new SpecialRegionSlotId("SR_SLOT_VILLAGE_ENTRY"), SpecialRegionSlotKind.Entry, entryRegion, entrySide);
            var returnPort = PortSource(regionId, origin, "VILLAGE_PORT_RETURN", "VILLAGE_RETURN_SOCKET",
                new SpecialRegionSlotId("SR_SLOT_VILLAGE_RETURN"), SpecialRegionSlotKind.Return, returnRegion, returnSide);

            var bridge = CreateInternal<SpecialRegionSiteBridge>(
                regionId, regionKind, reservationId, SiteReservationKind.Village, "SPECIAL_VILLAGE",
                origin, width, height, SiteFootprintTransform.R0,
                reverse ? offsets.Reverse() : offsets, reverse ? offsets.Reverse() : offsets,
                reverse ? sectors.Reverse() : sectors,
                Array.Empty<SpecialRegionSiteFixedShellBinding>(),
                reverse ? slotSources.AsEnumerable().Reverse() : slotSources,
                reverse ? new[] { returnPort, entryPort } : new[] { entryPort, returnPort },
                "RESERVATION_IDENTITY_MAP13_04", "CONTRACT_DIGEST_MAP13_04");
            SetCanonicalDigest(bridge, SpecialRegionSiteBridgeCanonicalDigest.Compute(bridge));

            var entryAnchor = Anchor(reservationId, entryPort, entrySide);
            var returnAnchor = Anchor(reservationId, returnPort, returnSide);
            var entryBinding = CreateInternal<SpecialRegionEntryPortBinding>(entryPort, entryAnchor, new[] { 1, 2, 3 });
            var returnBinding = CreateInternal<SpecialRegionEntryPortBinding>(returnPort, returnAnchor, new[] { 1, 2, 3 });
            var entryApron = Apron(entryPort.PortId, entryPort.Placed);
            var returnApron = Apron(returnPort.PortId, returnPort.Placed);
            var bidirectional = CreateInternal<SpecialRegionBidirectionalWitness>(
                new[] { "BEFORE", "ENTRY", "ROAD", "INTERIOR" },
                new[] { "INTERIOR", "ROAD", "RETURN", "AFTER" }, new[] { 1, 2, 3 }, new[] { 1, 2, 3 });
            var entryPlan = CreateInternal<SpecialRegionEntryBufferPlan>(
                bridge, entryBinding, returnBinding,
                reverse ? new[] { returnApron, entryApron } : new[] { entryApron, returnApron },
                Array.Empty<SpecialRegionQuietChunkBinding>(), bidirectional);
            SetCanonicalDigest(entryPlan, SpecialRegionEntryBufferCanonicalDigest.Compute(entryPlan));

            var fixedCollisionTile = shape == VillageLayoutShape.OneByTwo
                ? new LocalTileCoord(23, 6) : new LocalTileCoord(6, 15);
            var fixedPlaced = Place(origin, fixedCollisionTile);
            var fixedCollision = CreateInternal<SpecialRegionFixedCollisionCell>(
                "VILLAGE_FIXED_COLLISION", new SpecialRegionAuthoredCoordinate(
                    fixedPlaced.SectorOffset, fixedPlaced.LocalTile), fixedPlaced);
            var fixedPlan = CreateInternal<SpecialRegionFixedSlotLayerPlan>(
                regionId, regionKind, reservationId, "CONTRACT_DIGEST_MAP13_04",
                bridge.CanonicalDigest, entryPlan.CanonicalDigest, "COLLISION_DIGEST_MAP13_04",
                new[] { fixedCollision }, Array.Empty<SpecialRegionFixedAccessBinding>(),
                reverse ? slotBindings.AsEnumerable().Reverse() : slotBindings,
                Array.Empty<SpecialRegionOccupancyClaim>());

            var roadCells = Road(shape).Select((tile, order) => new VillageRoadCell(order, tile)).ToArray();
            var definition = new VillageShellDefinition(
                new VillageLayoutId("VILLAGE_LAYOUT_MAP13_04"), shape,
                reverse ? roadCells.Reverse() : roadCells,
                reverse ? facilityDefinitions.AsEnumerable().Reverse() : facilityDefinitions,
                reverse ? accessWitnesses.AsEnumerable().Reverse() : accessWitnesses,
                "display text excluded");
            return new Fixture(bridge, entryPlan, fixedPlan, definition, fixedCollisionTile);
        }

        private static SpecialRegionSitePortBinding PortSource(
            SpecialRegionId regionId,
            SectorCoord origin,
            string portId,
            string socketId,
            SpecialRegionSlotId slotId,
            SpecialRegionSlotKind kind,
            LocalTileCoord regionTile,
            SiteEntrySide side)
        {
            var placed = Place(origin, regionTile, side);
            var exterior = new SectorCoord(
                placed.WorldSector.X + SiteReservationTokenCodec.GetDeltaX(side),
                placed.WorldSector.Y + SiteReservationTokenCodec.GetDeltaY(side));
            return new SpecialRegionSitePortBinding(
                portId, slotId, kind, AccessClass.MandatoryNoTool,
                SpecialPersistenceKey.ForRegion(regionId), socketId, exterior,
                new SpecialRegionAuthoredCoordinate(placed.SectorOffset, placed.LocalTile, side), placed);
        }

        private static SiteEntryAnchor Anchor(
            SiteReservationId reservationId,
            SpecialRegionSitePortBinding port,
            SiteEntrySide side)
            => new SiteEntryAnchor(reservationId, port.EntrySocketId, port.Placed.WorldSector,
                side, new[] { 1, 2, 3 }, true, true);

        private static SpecialRegionEntryApron Apron(string portId, SpecialRegionPlacedCoordinate placed)
        {
            var coordinate = new SpecialRegionTileCoordinate(placed.WorldSector, placed.LocalTile);
            return new SpecialRegionEntryApron(portId, placed.WorldSector, placed.LocalTile, 1, 1, new[] { coordinate });
        }

        private static SpecialRegionPlacedCoordinate Place(
            SectorCoord origin,
            LocalTileCoord regionTile,
            SiteEntrySide? side = null)
        {
            var offset = new SpecialRegionSectorOffset(
                regionTile.X / WorldGenConstants.SectorWidthTiles,
                regionTile.Y / WorldGenConstants.SectorHeightTiles);
            var local = new LocalTileCoord(
                regionTile.X % WorldGenConstants.SectorWidthTiles,
                regionTile.Y % WorldGenConstants.SectorHeightTiles);
            return new SpecialRegionPlacedCoordinate(offset,
                new SectorCoord(origin.X + offset.X, origin.Y + offset.Y), local, regionTile, side);
        }

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

        private static void Dimensions(VillageLayoutShape shape, out int width, out int height)
        {
            width = shape == VillageLayoutShape.TwoByOne ? 2 : 1;
            height = shape == VillageLayoutShape.OneByTwo ? 2 : 1;
        }

        private static VillageShellDefinition Definition(
            Fixture fixture,
            VillageLayoutShape? shape = null,
            IEnumerable<VillageRoadCell> road = null,
            IEnumerable<VillageFacilityDefinition> facilities = null,
            IEnumerable<VillageFacilityAccessWitness> witnesses = null)
            => new VillageShellDefinition(
                fixture.Definition.LayoutId, shape ?? fixture.Definition.Shape,
                road ?? fixture.Definition.RoadCells,
                facilities ?? fixture.Definition.Facilities,
                witnesses ?? fixture.Definition.AccessWitnesses,
                fixture.Definition.DisplayText);

        private static VillageFacilityDefinition Copy(
            VillageFacilityDefinition value,
            LocalTileCoord? door = null)
            => new VillageFacilityDefinition(
                value.DefinitionId, value.Kind, value.Requirement, value.SlotId,
                value.OccupantId, door ?? value.DoorRegionTile, value.DisplayText);

        private static bool HasPair(
            IEnumerable<LocalTileCoord> values,
            LocalTileCoord first,
            LocalTileCoord second)
        {
            var cells = values.ToArray();
            for (var index = 1; index < cells.Length; index++)
                if ((cells[index - 1] == first && cells[index] == second) ||
                    (cells[index - 1] == second && cells[index] == first)) return true;
            return false;
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

        private static void AssertSuccess(VillageShellResult result)
        {
            Assert.That(result.Success, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Plan, Is.Not.Null);
            Assert.That(result.CanonicalDigest, Is.Not.Empty);
        }

        private static void AssertFailure(VillageShellResult result, VillageShellErrorCode code)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(code), string.Join("\n", result.Errors));
        }

        private sealed class Fixture
        {
            public Fixture(
                SpecialRegionSiteBridge bridge,
                SpecialRegionEntryBufferPlan entry,
                SpecialRegionFixedSlotLayerPlan fixedSlots,
                VillageShellDefinition definition,
                LocalTileCoord fixedCollision)
            {
                Bridge = bridge;
                Entry = entry;
                FixedSlots = fixedSlots;
                Definition = definition;
                FixedCollision = fixedCollision;
            }

            public SpecialRegionSiteBridge Bridge { get; }
            public SpecialRegionEntryBufferPlan Entry { get; }
            public SpecialRegionFixedSlotLayerPlan FixedSlots { get; }
            public VillageShellDefinition Definition { get; }
            public LocalTileCoord FixedCollision { get; }
        }
    }
}
