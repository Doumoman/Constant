using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.SpecialRegions;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.SpecialRegions
{
    [TestFixture]
    [Category("MAP09_06")]
    public sealed class SpecialRegionContractTests
    {
        [Test]
        public void ValidContractPublishesImmutableArtifactAndDigest()
        {
            var fixture = CreateFixture();
            var result = SpecialRegionValidator.Validate(fixture.Contract, fixture.Reservation);
            Assert.That(result.IsValid, Is.True, Join(result));
            Assert.That(result.Contract, Is.SameAs(fixture.Contract));
            Assert.That(result.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(fixture.Contract.Footprint.Offsets.Select(value => value.ToString()),
                Is.EqualTo(new[] { "0,0", "1,0" }));
            Assert.That(fixture.Contract.FixedShell.All(value => value.LayerKind == SpecialRegionLayerKind.FixedShell), Is.True);
            Assert.That(fixture.Contract.Slots.All(value => value.LayerKind == SpecialRegionLayerKind.ReplaceableSlot), Is.True);
        }

        [Test]
        public void PublishedEnumsAreExact()
        {
            Assert.That(Enum.GetNames(typeof(SpecialRegionKind)), Is.EqualTo(
                new[] { "Village", "CoreResource", "Forge", "Boss", "OptionalLandmark" }));
            Assert.That(Enum.GetNames(typeof(SpecialRegionLayerKind)), Is.EqualTo(
                new[] { "FixedShell", "ReplaceableSlot" }));
            Assert.That(Enum.GetNames(typeof(SpecialRegionSlotKind)), Is.EqualTo(
                new[] { "Facility", "Npc", "Enemy", "Event", "Reward", "Entry", "Return" }));
            Assert.That(Enum.GetNames(typeof(SpecialPersistenceScope)), Is.EqualTo(
                new[] { "Region", "Slot", "Reward", "Encounter" }));
        }

        [TestCase("SR_BAD-lower")]
        [TestCase("BAD")]
        [TestCase("")]
        public void InvalidRegionIdsAreRejected(string id)
        {
            var fixture = CreateFixture(id: new SpecialRegionId(id));
            AssertError(SpecialRegionValidator.Validate(fixture.Contract, fixture.Reservation),
                SpecialRegionValidationErrorCode.InvalidId);
        }

        [Test]
        public void MissingReservationAndFootprintMismatchAreRejected()
        {
            var fixture = CreateFixture();
            AssertError(SpecialRegionValidator.Validate(fixture.Contract, null),
                SpecialRegionValidationErrorCode.MissingReservation);

            var mismatched = CreateFixture(offsets: new[] { new SpecialRegionSectorOffset(0, 0) });
            AssertError(SpecialRegionValidator.Validate(mismatched.Contract, fixture.Reservation),
                SpecialRegionValidationErrorCode.FootprintMismatch);
        }

        [TestCase(0, 0, 2, 0)]
        [TestCase(1, 0, 2, 0)]
        [TestCase(0, 0, 1, 1)]
        public void UnsupportedOrNonNormalizedFootprintsAreRejected(int x0, int y0, int x1, int y1)
        {
            var fixture = CreateFixture(offsets: new[]
            {
                new SpecialRegionSectorOffset(x0, y0),
                new SpecialRegionSectorOffset(x1, y1),
            });
            AssertError(SpecialRegionValidator.Validate(fixture.Contract, fixture.Reservation),
                SpecialRegionValidationErrorCode.InvalidFootprint);
        }

        [Test]
        public void FixedShellAndSlotsCannotOverlap()
        {
            var fixture = CreateFixture();
            var overlap = new SpecialRegionSlot(new SpecialRegionSlotId("SR_SLOT_ENTRY"),
                SpecialRegionSlotKind.Entry, new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(1, 1),
                true, default(SpecialPersistenceScope), default(SpecialPersistenceKey));
            var contract = With(fixture.Contract,
                slots: new[] { overlap, fixture.Contract.Slots[1], fixture.Contract.Slots[2] });
            AssertError(SpecialRegionValidator.Validate(contract, fixture.Reservation),
                SpecialRegionValidationErrorCode.SlotShellOverlap);
        }

        [Test]
        public void EntryAndReturnPortsMustMatchSlotsReservationAndAccessAuthority()
        {
            var fixture = CreateFixture();
            var invalid = new SpecialRegionPort("SR_PORT_ENTRY", new SpecialRegionSlotId("SR_SLOT_ENTRY"),
                SpecialRegionSlotKind.Entry, new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(0, 5),
                SiteEntrySide.R, AccessClass.Unspecified);
            var contract = With(fixture.Contract, ports: new[] { invalid, fixture.Contract.Ports[1] });
            AssertError(SpecialRegionValidator.Validate(contract, fixture.Reservation),
                SpecialRegionValidationErrorCode.InvalidPort);
        }

        [Test]
        public void PersistenceKeysAreStableAndBoundToRegionScopeAndSlot()
        {
            var region = new SpecialRegionId("SR_LIVE_BASELINE");
            var slot = new SpecialRegionSlotId("SR_SLOT_REWARD");
            Assert.That(SpecialPersistenceKey.ForRegion(region).Value,
                Is.EqualTo("SR_STATE_LIVE_BASELINE_REGION"));
            Assert.That(SpecialPersistenceKey.ForSlot(region, SpecialPersistenceScope.Reward, slot).Value,
                Is.EqualTo("SR_STATE_LIVE_BASELINE_REWARD_REWARD"));
            Assert.That(SpecialPersistenceKey.ForSlot(region, SpecialPersistenceScope.Reward, slot),
                Is.EqualTo(SpecialPersistenceKey.ForSlot(region, SpecialPersistenceScope.Reward, slot)));
        }

        [Test]
        public void RequiredRewardWithoutPersistenceCannotPublish()
        {
            var fixture = CreateFixture();
            var reward = new SpecialRegionSlot(new SpecialRegionSlotId("SR_SLOT_REWARD"),
                SpecialRegionSlotKind.Reward, new SpecialRegionSectorOffset(1, 0), new LocalTileCoord(4, 4),
                true, SpecialPersistenceScope.Reward, default(SpecialPersistenceKey));
            var contract = With(fixture.Contract,
                slots: new[] { fixture.Contract.Slots[0], reward, fixture.Contract.Slots[2] });
            AssertError(SpecialRegionValidator.Validate(contract, fixture.Reservation),
                SpecialRegionValidationErrorCode.MissingPersistenceKey);
        }

        [Test]
        public void CrossRegionPersistenceCollisionIsRejected()
        {
            var fixture = CreateFixture();
            var other = CreateFixture(id: new SpecialRegionId("SR_OTHER"));
            var stolen = new SpecialPersistenceBinding(fixture.Contract.Persistence[0].Key,
                SpecialPersistenceScope.Region, default(SpecialRegionSlotId), "INITIAL_UNCLAIMED");
            var otherContract = With(other.Contract, persistence: new[] { stolen, other.Contract.Persistence[1] });
            var result = SpecialRegionValidator.Validate(fixture.Contract, fixture.Reservation, new[] { otherContract });
            AssertError(result, SpecialRegionValidationErrorCode.DuplicatePersistenceKey);
        }

        [Test]
        public void CollectionsAreDefensivelyCopiedAndDigestIgnoresInputOrderAndDisplayText()
        {
            var fixture = CreateFixture();
            var slots = fixture.Contract.Slots.Reverse().ToList();
            var changedDisplay = new SpecialRegionContract(fixture.Contract.Id, fixture.Contract.Kind,
                fixture.Contract.ReservationId, fixture.Contract.Footprint, fixture.Contract.FixedShell.Reverse(),
                slots, fixture.Contract.Ports.Reverse(), fixture.Contract.Persistence.Reverse(), "localized text");
            slots.Clear();
            var first = SpecialRegionValidator.Validate(fixture.Contract, fixture.Reservation);
            var second = SpecialRegionValidator.Validate(changedDisplay, fixture.Reservation);
            Assert.That(second.IsValid, Is.True, Join(second));
            Assert.That(changedDisplay.Slots, Has.Count.EqualTo(3));
            Assert.That(second.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));
            Assert.That(changedDisplay.Slots, Is.Not.InstanceOf<List<SpecialRegionSlot>>());
        }

        [Test]
        public void NegativeErrorsAreAccumulatedSortedDeduplicatedAndPublishNothing()
        {
            var fixture = CreateFixture(id: new SpecialRegionId("bad"), offsets: Array.Empty<SpecialRegionSectorOffset>());
            var result = SpecialRegionValidator.Validate(fixture.Contract, null);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Contract, Is.Null);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Errors, Has.Count.GreaterThan(2));
            Assert.That(result.Errors, Is.Ordered);
            Assert.That(result.Errors.Distinct().Count(), Is.EqualTo(result.Errors.Count));
        }

        [Test]
        public void ProductionScopeContainsNoMutableStateSolverContentOrUnityLifecycle()
        {
            var root = Path.GetFullPath(Path.Combine(Application.dataPath,
                "_Game/Map/Runtime/WorldGeneration/SpecialRegions"));
            var source = string.Join("\n", Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .OrderBy(value => value, StringComparer.Ordinal).Select(File.ReadAllText));
            foreach (var forbidden in new[]
                     {
                         "UnityEditor", "UnityEngine", "System.Random", "Random.", "MonoBehaviour", "ScriptableObject",
                         "SaveData", "ReservationSolver", "GeneralRouteAccess", "StageMapGenerator", "GridWorld",
                         "RoomTemplate", "RoomGridTransform", "TileMutationService", "SectorRecipeResolver",
                     })
                Assert.That(source, Does.Not.Contain(forbidden), forbidden);
        }

        private static Fixture CreateFixture(
            SpecialRegionId? id = null,
            IEnumerable<SpecialRegionSectorOffset> offsets = null)
        {
            var regionId = id ?? new SpecialRegionId("SR_LIVE_BASELINE");
            var reservationId = new SiteReservationId("RES_SPECIAL_VILLAGE");
            var siteFootprint = new SiteFootprint(2, 1, SiteFootprintTransform.R0, new[]
            {
                new SiteFootprintCell(0, 0, "ENTRY", "", "", new[] { SiteEntrySide.L }),
                new SiteFootprintCell(1, 0, "CORE", "", "", Array.Empty<SiteEntrySide>()),
            });
            var anchor = new SiteEntryAnchor(reservationId, "ENTRY_MAIN", new SectorCoord(2, 2),
                SiteEntrySide.L, new[] { 1, 2, 3 }, true, true);
            var reservation = new SiteReservation(reservationId, SiteReservationKind.Village,
                "VILLAGE_MOON", new SectorCoord(2, 2), siteFootprint, "BIOME_ROOT", 1, new[] { anchor });
            var rewardId = new SpecialRegionSlotId("SR_SLOT_REWARD");
            var rewardKey = SpecialPersistenceKey.ForSlot(regionId, SpecialPersistenceScope.Reward, rewardId);
            var slots = new[]
            {
                new SpecialRegionSlot(new SpecialRegionSlotId("SR_SLOT_ENTRY"), SpecialRegionSlotKind.Entry,
                    new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(0, 5), true,
                    default(SpecialPersistenceScope), default(SpecialPersistenceKey)),
                new SpecialRegionSlot(rewardId, SpecialRegionSlotKind.Reward,
                    new SpecialRegionSectorOffset(1, 0), new LocalTileCoord(4, 4), true,
                    SpecialPersistenceScope.Reward, rewardKey),
                new SpecialRegionSlot(new SpecialRegionSlotId("SR_SLOT_RETURN"), SpecialRegionSlotKind.Return,
                    new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(0, 6), true,
                    default(SpecialPersistenceScope), default(SpecialPersistenceKey)),
            };
            var ports = new[]
            {
                new SpecialRegionPort("SR_PORT_ENTRY", slots[0].Id, SpecialRegionSlotKind.Entry,
                    slots[0].SectorOffset, slots[0].Tile, SiteEntrySide.L, AccessClass.MandatoryNoTool),
                new SpecialRegionPort("SR_PORT_RETURN", slots[2].Id, SpecialRegionSlotKind.Return,
                    slots[2].SectorOffset, slots[2].Tile, SiteEntrySide.L, AccessClass.MandatoryNoTool),
            };
            var persistence = new[]
            {
                new SpecialPersistenceBinding(SpecialPersistenceKey.ForRegion(regionId),
                    SpecialPersistenceScope.Region, default(SpecialRegionSlotId), "INITIAL_UNCLAIMED"),
                new SpecialPersistenceBinding(rewardKey, SpecialPersistenceScope.Reward, rewardId, "INITIAL_AVAILABLE"),
            };
            var contract = new SpecialRegionContract(regionId, SpecialRegionKind.Village, reservationId,
                new SpecialRegionFootprint(offsets ?? new[]
                {
                    new SpecialRegionSectorOffset(0, 0),
                    new SpecialRegionSectorOffset(1, 0),
                }),
                new[]
                {
                    new SpecialRegionFixedShellCell(new SpecialRegionSectorOffset(0, 0),
                        new LocalTileCoord(1, 1), "SHELL_WALL"),
                }, slots, ports, persistence, "Fixture");
            return new Fixture(contract, reservation);
        }

        private static SpecialRegionContract With(
            SpecialRegionContract source,
            IEnumerable<SpecialRegionSlot> slots = null,
            IEnumerable<SpecialRegionPort> ports = null,
            IEnumerable<SpecialPersistenceBinding> persistence = null)
            => new SpecialRegionContract(source.Id, source.Kind, source.ReservationId, source.Footprint,
                source.FixedShell, slots ?? source.Slots, ports ?? source.Ports,
                persistence ?? source.Persistence, source.DisplayText);

        private static void AssertError(SpecialRegionValidationResult result, SpecialRegionValidationErrorCode code)
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(code), Join(result));
        }

        private static string Join(SpecialRegionValidationResult result)
            => string.Join("\n", result.Errors.Select(value => value.ToString()));

        private sealed class Fixture
        {
            public Fixture(SpecialRegionContract contract, SiteReservation reservation)
            {
                Contract = contract;
                Reservation = reservation;
            }
            public SpecialRegionContract Contract { get; }
            public SiteReservation Reservation { get; }
        }
    }
}
