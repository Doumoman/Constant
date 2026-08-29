using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.SpecialRegions
{
    [TestFixture]
    [Category("MAP13_01")]
    public sealed class SpecialRegionSiteBridgeTests
    {
        [TestCase(1, 1, SiteFootprintTransform.R0)]
        [TestCase(1, 1, SiteFootprintTransform.MirrorX)]
        [TestCase(1, 1, SiteFootprintTransform.MirrorY)]
        [TestCase(1, 1, SiteFootprintTransform.R180)]
        [TestCase(2, 1, SiteFootprintTransform.R0)]
        [TestCase(2, 1, SiteFootprintTransform.MirrorX)]
        [TestCase(2, 1, SiteFootprintTransform.MirrorY)]
        [TestCase(2, 1, SiteFootprintTransform.R180)]
        [TestCase(1, 2, SiteFootprintTransform.R0)]
        [TestCase(1, 2, SiteFootprintTransform.MirrorX)]
        [TestCase(1, 2, SiteFootprintTransform.MirrorY)]
        [TestCase(1, 2, SiteFootprintTransform.R180)]
        public void SupportedShapesAndTransforms_CompileExactSectorRows(
            int width,
            int height,
            SiteFootprintTransform transform)
        {
            var fixture = Create(width, height, transform);

            var result = SpecialRegionSiteBridgeCompiler.Compile(fixture.Snapshot, fixture.Validation);

            Assert.That(result.Succeeded, Is.True, Join(result));
            Assert.That(result.Bridge.SectorBindings, Has.Count.EqualTo(width * height));
            Assert.That(result.Bridge.SourceFootprint, Has.Count.EqualTo(width * height));
            Assert.That(result.Bridge.PlacedFootprint, Has.Count.EqualTo(width * height));
            Assert.That(result.CanonicalDigest, Has.Length.EqualTo(64));
            foreach (var binding in result.Bridge.SectorBindings)
            {
                var row = fixture.Snapshot.GetSector(binding.SectorIndex);
                Assert.That(row.Coordinate, Is.EqualTo(binding.WorldSector));
                Assert.That(row.ReservationId, Is.EqualTo(fixture.Reservation.ReservationId));
                Assert.That(row.LocalX, Is.EqualTo(binding.PlacedOffset.X));
                Assert.That(row.LocalY, Is.EqualTo(binding.PlacedOffset.Y));
            }
        }

        [TestCase(SiteFootprintTransform.R0, 0, 0, 3, 5, SiteEntrySide.L)]
        [TestCase(SiteFootprintTransform.MirrorX, 1, 0, 44, 5, SiteEntrySide.R)]
        [TestCase(SiteFootprintTransform.MirrorY, 0, 0, 3, 26, SiteEntrySide.L)]
        [TestCase(SiteFootprintTransform.R180, 1, 0, 44, 26, SiteEntrySide.R)]
        public void CoordinateProjection_IsExactAndRoundTrips(
            SiteFootprintTransform transform,
            int expectedSectorX,
            int expectedSectorY,
            int expectedTileX,
            int expectedTileY,
            SiteEntrySide expectedSide)
        {
            var source = new SpecialRegionAuthoredCoordinate(
                new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(3, 5), SiteEntrySide.L);

            Assert.That(SpecialRegionSiteCoordinateTransformer.TryProject(
                2, 1, transform, new SectorCoord(5, 4), source, out var placed), Is.True);
            Assert.That(placed.SectorOffset,
                Is.EqualTo(new SpecialRegionSectorOffset(expectedSectorX, expectedSectorY)));
            Assert.That(placed.WorldSector,
                Is.EqualTo(new SectorCoord(5 + expectedSectorX, 4 + expectedSectorY)));
            Assert.That(placed.LocalTile, Is.EqualTo(new LocalTileCoord(expectedTileX, expectedTileY)));
            Assert.That(placed.RegionTile, Is.EqualTo(new LocalTileCoord(
                expectedSectorX * WorldGenConstants.SectorWidthTiles + expectedTileX,
                expectedSectorY * WorldGenConstants.SectorHeightTiles + expectedTileY)));
            Assert.That(placed.Side, Is.EqualTo(expectedSide));
            Assert.That(SpecialRegionSiteCoordinateTransformer.TryUnproject(
                2, 1, transform, new SectorCoord(5, 4), placed, out var roundTrip), Is.True);
            Assert.That(roundTrip, Is.EqualTo(source));
        }

        [TestCase(SiteReservationKind.Village, SpecialRegionKind.Village)]
        [TestCase(SiteReservationKind.CoreResource, SpecialRegionKind.CoreResource)]
        [TestCase(SiteReservationKind.Forge, SpecialRegionKind.Forge)]
        [TestCase(SiteReservationKind.Boss, SpecialRegionKind.Boss)]
        public void ExactKindMatrix_Compiles(
            SiteReservationKind reservationKind,
            SpecialRegionKind regionKind)
        {
            var fixture = Create(1, 1, SiteFootprintTransform.R0, reservationKind, regionKind);
            var result = SpecialRegionSiteBridgeCompiler.Compile(fixture.Snapshot, fixture.Validation);

            Assert.That(result.Succeeded, Is.True, Join(result));
            Assert.That(result.Bridge.ReservationKind, Is.EqualTo(reservationKind));
            Assert.That(result.Bridge.RegionKind, Is.EqualTo(regionKind));
        }

        [Test]
        public void Bindings_PreservePayloadAndSourcePlacedProvenance()
        {
            var fixture = Create(2, 1, SiteFootprintTransform.MirrorX);
            var result = SpecialRegionSiteBridgeCompiler.Compile(fixture.Snapshot, fixture.Validation);

            Assert.That(result.Succeeded, Is.True, Join(result));
            var shell = result.Bridge.FixedShellBindings.Single();
            Assert.That(shell.ShellId, Is.EqualTo("SHELL_WALL"));
            Assert.That(shell.Source.SectorOffset, Is.EqualTo(new SpecialRegionSectorOffset(0, 0)));
            Assert.That(shell.Placed.SectorOffset, Is.EqualTo(new SpecialRegionSectorOffset(1, 0)));
            Assert.That(shell.Placed.LocalTile, Is.EqualTo(new LocalTileCoord(39, 9)));

            var reward = result.Bridge.SlotBindings.Single(value => value.Kind == SpecialRegionSlotKind.Reward);
            Assert.That(reward.Required, Is.True);
            Assert.That(reward.PersistenceScope, Is.EqualTo(SpecialPersistenceScope.Reward));
            Assert.That(reward.PersistenceKey.Value, Does.StartWith("SR_STATE_"));
            Assert.That(result.Bridge.PortBindings.Select(value => value.AccessClass),
                Is.EqualTo(new[] { AccessClass.MandatoryNoTool, AccessClass.MandatoryNoTool }));
            Assert.That(result.Bridge.SectorBindings, Is.InstanceOf<ReadOnlyCollection<SpecialRegionSiteSectorBinding>>());
            Assert.That(result.Bridge.SlotBindings, Is.InstanceOf<ReadOnlyCollection<SpecialRegionSiteSlotBinding>>());
        }

        [TestCase(SiteFootprintTransform.R0, 5, SiteEntrySide.L, 0)]
        [TestCase(SiteFootprintTransform.MirrorX, 6, SiteEntrySide.R, 47)]
        [TestCase(SiteFootprintTransform.MirrorY, 5, SiteEntrySide.L, 0)]
        [TestCase(SiteFootprintTransform.R180, 6, SiteEntrySide.R, 47)]
        public void Ports_MatchTransformedMap03AnchorAndExteriorEdge(
            SiteFootprintTransform transform,
            int expectedWorldX,
            SiteEntrySide expectedSide,
            int expectedTileX)
        {
            var fixture = Create(2, 1, transform);
            var result = SpecialRegionSiteBridgeCompiler.Compile(fixture.Snapshot, fixture.Validation);

            Assert.That(result.Succeeded, Is.True, Join(result));
            foreach (var port in result.Bridge.PortBindings)
            {
                Assert.That(port.Placed.WorldSector.X, Is.EqualTo(expectedWorldX));
                Assert.That(port.Placed.Side, Is.EqualTo(expectedSide));
                Assert.That(port.Placed.LocalTile.X, Is.EqualTo(expectedTileX));
                Assert.That(port.EntrySocketId, Is.EqualTo("ENTRY_MAIN"));
                Assert.That(port.AnchorExteriorSector.X,
                    Is.EqualTo(expectedWorldX + (expectedSide == SiteEntrySide.L ? -1 : 1)));
            }
        }

        [Test]
        public void ReverseRepeatCultureAndCallerMutation_AreCanonical()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var first = Create(2, 1, SiteFootprintTransform.R180, reverse: false, mutateCaller: true);
                var reverse = Create(2, 1, SiteFootprintTransform.R180, reverse: true, mutateCaller: true);
                var firstResult = SpecialRegionSiteBridgeCompiler.Compile(first.Snapshot, first.Validation);
                var repeatResult = SpecialRegionSiteBridgeCompiler.Compile(first.Snapshot, first.Validation);
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var cultureResult = SpecialRegionSiteBridgeCompiler.Compile(reverse.Snapshot, reverse.Validation);

                Assert.That(firstResult.Succeeded, Is.True, Join(firstResult));
                Assert.That(repeatResult.CanonicalDigest, Is.EqualTo(firstResult.CanonicalDigest));
                Assert.That(cultureResult.CanonicalDigest, Is.EqualTo(firstResult.CanonicalDigest));
                Assert.That(cultureResult.Bridge.ContractDigest, Is.EqualTo(firstResult.Bridge.ContractDigest));
                Assert.That(cultureResult.Bridge.ReservationIdentityDigest,
                    Is.EqualTo(firstResult.Bridge.ReservationIdentityDigest));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Test]
        public void InvalidIdentityKindShapeCoordinateAnchorAndPort_AreAtomic()
        {
            var fixture = Create(2, 1, SiteFootprintTransform.R0);

            AssertFailure(
                SpecialRegionSiteBridgeCompiler.Compile(
                    (SiteReservationSnapshot)null, (SpecialRegionValidationResult)null),
                SpecialRegionSiteBridgeErrorCode.MissingInput);

            var invalidId = With(fixture.Contract, reservationId: default(SiteReservationId));
            AssertFailure(
                SpecialRegionSiteBridgeCompiler.Compile(fixture.Snapshot, invalidId),
                SpecialRegionSiteBridgeErrorCode.InvalidReservation);

            var optional = With(fixture.Contract, kind: SpecialRegionKind.OptionalLandmark);
            var optionalValidation = SpecialRegionValidator.Validate(optional, fixture.SourceReservation);
            Assert.That(optionalValidation.IsValid, Is.True);
            AssertFailure(
                SpecialRegionSiteBridgeCompiler.Compile(fixture.Snapshot, optionalValidation),
                SpecialRegionSiteBridgeErrorCode.UnsupportedKind);

            var sparse = With(fixture.Contract, footprint: new SpecialRegionFootprint(new[]
            {
                new SpecialRegionSectorOffset(0, 0),
                new SpecialRegionSectorOffset(2, 0),
            }));
            AssertFailure(
                SpecialRegionSiteBridgeCompiler.Compile(fixture.Snapshot, sparse),
                SpecialRegionSiteBridgeErrorCode.UnsupportedFootprint);

            var badShell = With(fixture.Contract, fixedShell: new[]
            {
                new SpecialRegionFixedShellCell(
                    new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(-1, 9), "SHELL_WALL")
            });
            AssertFailure(
                SpecialRegionSiteBridgeCompiler.Compile(fixture.Snapshot, badShell),
                SpecialRegionSiteBridgeErrorCode.CoordinateOutOfRange);

            var offEdge = ReplacePortTiles(fixture.Contract, 1);
            var offEdgeValidation = SpecialRegionValidator.Validate(offEdge, fixture.SourceReservation);
            Assert.That(offEdgeValidation.IsValid, Is.True);
            AssertFailure(
                SpecialRegionSiteBridgeCompiler.Compile(fixture.Snapshot, offEdgeValidation),
                SpecialRegionSiteBridgeErrorCode.PortNotOnExteriorEdge);

            var wrongAnchor = ReplaceReservationAnchor(fixture.Reservation, new SectorCoord(6, 5), SiteEntrySide.L);
            AssertFailure(
                SpecialRegionSiteBridgeCompiler.Compile(
                    BuildSnapshot(wrongAnchor, false), fixture.Validation),
                SpecialRegionSiteBridgeErrorCode.PortAnchorMismatch);
        }

        [Test]
        public void StartAndOptionalLandmark_AreExplicitlyUnsupported()
        {
            var fixture = Create(1, 1, SiteFootprintTransform.R0);
            var start = fixture.Snapshot.StartReservation;
            var startContract = With(fixture.Contract,
                reservationId: start.ReservationId,
                kind: SpecialRegionKind.Village,
                footprint: new SpecialRegionFootprint(new[] { new SpecialRegionSectorOffset(0, 0) }));
            AssertFailure(
                SpecialRegionSiteBridgeCompiler.Compile(fixture.Snapshot, startContract),
                SpecialRegionSiteBridgeErrorCode.UnsupportedKind);

            var optional = With(fixture.Contract, kind: SpecialRegionKind.OptionalLandmark);
            var validation = SpecialRegionValidator.Validate(optional, fixture.SourceReservation);
            AssertFailure(
                SpecialRegionSiteBridgeCompiler.Compile(fixture.Snapshot, validation),
                SpecialRegionSiteBridgeErrorCode.UnsupportedKind);
        }

        [Test]
        public void InvalidSectorRows_AreRejectedByMap03AuthorityBeforePublication()
        {
            var fixture = Create(1, 1, SiteFootprintTransform.R0);
            var rows = fixture.Snapshot.Sectors.ToList();
            var occupied = fixture.Reservation.OccupiedSectors.Single();
            var index = WorldGridIndex.ToIndex(occupied);
            rows[index] = SectorReservation.CreateUnreserved(index, occupied);

            Assert.That(() => new SiteReservationSnapshot(
                fixture.Snapshot.Seed, fixture.Snapshot.Reservations, rows, fixture.Snapshot.CoreBiomeSeeds),
                Throws.ArgumentException);
            Assert.That(fixture.Snapshot.GetSector(index).IsReserved, Is.True);
        }

        [Test]
        public void InvalidCoordinatesEnumsAndRedundantPlacedFields_DoNotClampOrWrap()
        {
            var fixture = Create(2, 1, SiteFootprintTransform.R0);
            var invalidTile = new SpecialRegionAuthoredCoordinate(
                new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(48, 0));
            Assert.That(SpecialRegionSiteCoordinateTransformer.TryProject(
                fixture.Reservation, invalidTile, out var invalidPlaced), Is.False);
            Assert.That(invalidPlaced, Is.EqualTo(default(SpecialRegionPlacedCoordinate)));

            var invalidSector = new SpecialRegionAuthoredCoordinate(
                new SpecialRegionSectorOffset(-1, 0), new LocalTileCoord(0, 0));
            Assert.That(SpecialRegionSiteCoordinateTransformer.TryProject(
                fixture.Reservation, invalidSector, out _), Is.False);
            Assert.That(SpecialRegionSiteCoordinateTransformer.TryProject(
                2, 1, (SiteFootprintTransform)99, fixture.Reservation.Origin,
                new SpecialRegionAuthoredCoordinate(
                    new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(0, 0)), out _), Is.False);

            var source = new SpecialRegionAuthoredCoordinate(
                new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(4, 7), SiteEntrySide.L);
            Assert.That(SpecialRegionSiteCoordinateTransformer.TryProject(
                fixture.Reservation, source, out var placed), Is.True);
            var forged = new SpecialRegionPlacedCoordinate(
                placed.SectorOffset, placed.WorldSector, placed.LocalTile,
                new LocalTileCoord(placed.RegionTile.X + 1, placed.RegionTile.Y), placed.Side);
            Assert.That(SpecialRegionSiteCoordinateTransformer.TryUnproject(
                fixture.Reservation, forged, out _), Is.False);
        }

        private static Fixture Create(
            int width,
            int height,
            SiteFootprintTransform transform,
            SiteReservationKind reservationKind = SiteReservationKind.Village,
            SpecialRegionKind regionKind = SpecialRegionKind.Village,
            bool reverse = false,
            bool mutateCaller = false)
        {
            var reservationId = new SiteReservationId("RES_SPECIAL_SITE");
            var regionId = new SpecialRegionId("SR_SITE_BRIDGE");
            var origin = new SectorCoord(5, 5);
            var sourceOffsets = new List<SpecialRegionSectorOffset>();
            var sourceCells = new List<SiteFootprintCell>();
            var placedCells = new List<SiteFootprintCell>();
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                sourceOffsets.Add(new SpecialRegionSectorOffset(x, y));
                var role = "CELL_" + x + "_" + y;
                sourceCells.Add(new SiteFootprintCell(x, y, role, string.Empty, string.Empty,
                    x == 0 && y == 0 ? new[] { SiteEntrySide.L } : Array.Empty<SiteEntrySide>()));
                SiteFootprintTransformer.TryTransformCoordinate(
                    width, height, transform, x, y, out var placedX, out var placedY);
                var sides = Array.Empty<SiteEntrySide>();
                if (x == 0 && y == 0)
                {
                    SiteFootprintTransformer.TryTransformSide(transform, SiteEntrySide.L, out var side);
                    sides = new[] { side };
                }
                placedCells.Add(new SiteFootprintCell(
                    placedX, placedY, role, string.Empty, string.Empty, sides));
            }

            SiteFootprintTransformer.TryTransformCoordinate(
                width, height, transform, 0, 0, out var anchorX, out var anchorY);
            SiteFootprintTransformer.TryTransformSide(transform, SiteEntrySide.L, out var anchorSide);
            var placedAnchor = new SiteEntryAnchor(reservationId, "ENTRY_MAIN",
                new SectorCoord(origin.X + anchorX, origin.Y + anchorY), anchorSide,
                new[] { 1, 2, 3 }, true, true);
            var reservation = new SiteReservation(reservationId, reservationKind,
                "SPECIAL_SITE", origin, new SiteFootprint(width, height, transform, placedCells),
                string.Empty, 1, new[] { placedAnchor });
            var sourceAnchor = new SiteEntryAnchor(reservationId, "ENTRY_MAIN", origin,
                SiteEntrySide.L, new[] { 1, 2, 3 }, true, true);
            var sourceReservation = new SiteReservation(reservationId, reservationKind,
                "SPECIAL_SITE", origin, new SiteFootprint(width, height, SiteFootprintTransform.R0, sourceCells),
                string.Empty, 1, new[] { sourceAnchor });

            var rewardId = new SpecialRegionSlotId("SR_SLOT_REWARD");
            var rewardKey = SpecialPersistenceKey.ForSlot(
                regionId, SpecialPersistenceScope.Reward, rewardId);
            var entryId = new SpecialRegionSlotId("SR_SLOT_ENTRY");
            var returnId = new SpecialRegionSlotId("SR_SLOT_RETURN");
            var slots = new List<SpecialRegionSlot>
            {
                new SpecialRegionSlot(entryId, SpecialRegionSlotKind.Entry,
                    new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(0, 5), true,
                    default(SpecialPersistenceScope), default(SpecialPersistenceKey)),
                new SpecialRegionSlot(returnId, SpecialRegionSlotKind.Return,
                    new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(0, 6), true,
                    default(SpecialPersistenceScope), default(SpecialPersistenceKey)),
                new SpecialRegionSlot(rewardId, SpecialRegionSlotKind.Reward,
                    sourceOffsets[sourceOffsets.Count - 1], new LocalTileCoord(10, 10), true,
                    SpecialPersistenceScope.Reward, rewardKey),
            };
            var ports = new List<SpecialRegionPort>
            {
                new SpecialRegionPort("SR_PORT_ENTRY", entryId, SpecialRegionSlotKind.Entry,
                    new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(0, 5),
                    SiteEntrySide.L, AccessClass.MandatoryNoTool),
                new SpecialRegionPort("SR_PORT_RETURN", returnId, SpecialRegionSlotKind.Return,
                    new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(0, 6),
                    SiteEntrySide.L, AccessClass.MandatoryNoTool),
            };
            var shell = new List<SpecialRegionFixedShellCell>
            {
                new SpecialRegionFixedShellCell(
                    new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(8, 9), "SHELL_WALL")
            };
            var persistence = new List<SpecialPersistenceBinding>
            {
                new SpecialPersistenceBinding(SpecialPersistenceKey.ForRegion(regionId),
                    SpecialPersistenceScope.Region, default(SpecialRegionSlotId), "INITIAL_UNCLAIMED"),
                new SpecialPersistenceBinding(rewardKey,
                    SpecialPersistenceScope.Reward, rewardId, "INITIAL_AVAILABLE"),
            };
            if (reverse)
            {
                sourceOffsets.Reverse();
                shell.Reverse();
                slots.Reverse();
                ports.Reverse();
                persistence.Reverse();
            }

            var contract = new SpecialRegionContract(
                regionId, regionKind, reservationId, new SpecialRegionFootprint(sourceOffsets),
                shell, slots, ports, persistence, "Display text is not bridge identity");
            var validation = SpecialRegionValidator.Validate(contract, sourceReservation);
            Assert.That(validation.IsValid, Is.True, string.Join("\n", validation.Errors));
            var snapshot = BuildSnapshot(reservation, reverse);

            if (mutateCaller)
            {
                sourceOffsets.Clear();
                shell.Clear();
                slots.Clear();
                ports.Clear();
                persistence.Clear();
                placedCells.Clear();
                sourceCells.Clear();
            }
            return new Fixture(contract, validation, reservation, sourceReservation, snapshot);
        }

        private static SiteReservationSnapshot BuildSnapshot(SiteReservation reservation, bool reverse)
        {
            var startId = new SiteReservationId("RES_START");
            var startOrigin = new SectorCoord(1, 1);
            var start = new SiteReservation(startId, SiteReservationKind.Start,
                "START_SITE", startOrigin,
                new SiteFootprint(1, 1, SiteFootprintTransform.R0, new[]
                {
                    new SiteFootprintCell(0, 0, "START", string.Empty, string.Empty,
                        Array.Empty<SiteEntrySide>())
                }), string.Empty, 0, Array.Empty<SiteEntryAnchor>());
            var reservations = new List<SiteReservation> { start, reservation };
            var occupied = new Dictionary<SectorCoord, Tuple<SiteReservation, SiteFootprintCell>>();
            foreach (var site in reservations)
            foreach (var sector in site.OccupiedSectors)
            {
                site.TryGetFootprintCell(sector, out var cell);
                occupied.Add(sector, Tuple.Create(site, cell));
            }

            var rows = new List<SectorReservation>();
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var coordinate = WorldGridIndex.ToCoordinate(index);
                if (occupied.TryGetValue(coordinate, out var binding))
                    rows.Add(SectorReservation.CreateReserved(index, coordinate,
                        binding.Item1.ReservationId, binding.Item1.Kind,
                        binding.Item2.LocalX, binding.Item2.LocalY, binding.Item2.LocalRole));
                else rows.Add(SectorReservation.CreateUnreserved(index, coordinate));
            }
            if (reverse)
            {
                reservations.Reverse();
                rows.Reverse();
            }
            return new SiteReservationSnapshot(123UL, reservations, rows, Array.Empty<CoreBiomeSeed>());
        }

        private static SiteReservation ReplaceReservationAnchor(
            SiteReservation source,
            SectorCoord sector,
            SiteEntrySide side)
        {
            var anchor = new SiteEntryAnchor(source.ReservationId, "ENTRY_MAIN", sector, side,
                new[] { 1, 2, 3 }, true, true);
            return new SiteReservation(source.ReservationId, source.Kind, source.SourceDefinitionId,
                source.Origin, source.Footprint, source.PrimaryBiomeId,
                source.ReservationOrder, new[] { anchor });
        }

        private static SpecialRegionContract ReplacePortTiles(SpecialRegionContract source, int tileX)
        {
            var slots = source.Slots.Select(slot =>
                slot.Kind == SpecialRegionSlotKind.Entry || slot.Kind == SpecialRegionSlotKind.Return
                    ? new SpecialRegionSlot(slot.Id, slot.Kind, slot.SectorOffset,
                        new LocalTileCoord(tileX, slot.Tile.Y), slot.Required,
                        slot.PersistenceScope, slot.PersistenceKey)
                    : slot).ToArray();
            var ports = source.Ports.Select(port => new SpecialRegionPort(
                port.PortId, port.SlotId, port.Kind, port.SectorOffset,
                new LocalTileCoord(tileX, port.Tile.Y), port.Side, port.AccessClass)).ToArray();
            return new SpecialRegionContract(source.Id, source.Kind, source.ReservationId,
                source.Footprint, source.FixedShell, slots, ports, source.Persistence, source.DisplayText);
        }

        private static SpecialRegionContract With(
            SpecialRegionContract source,
            SiteReservationId? reservationId = null,
            SpecialRegionKind? kind = null,
            SpecialRegionFootprint footprint = null,
            IEnumerable<SpecialRegionFixedShellCell> fixedShell = null)
            => new SpecialRegionContract(source.Id, kind ?? source.Kind,
                reservationId ?? source.ReservationId, footprint ?? source.Footprint,
                fixedShell ?? source.FixedShell, source.Slots, source.Ports,
                source.Persistence, source.DisplayText);

        private static void AssertFailure(
            SpecialRegionSiteBridgeResult result,
            SpecialRegionSiteBridgeErrorCode code)
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Bridge, Is.Null);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(code), Join(result));
        }

        private static string Join(SpecialRegionSiteBridgeResult result)
            => string.Join("\n", result.Errors.Select(value => value.ToString()));

        private sealed class Fixture
        {
            public Fixture(
                SpecialRegionContract contract,
                SpecialRegionValidationResult validation,
                SiteReservation reservation,
                SiteReservation sourceReservation,
                SiteReservationSnapshot snapshot)
            {
                Contract = contract;
                Validation = validation;
                Reservation = reservation;
                SourceReservation = sourceReservation;
                Snapshot = snapshot;
            }

            public SpecialRegionContract Contract { get; }
            public SpecialRegionValidationResult Validation { get; }
            public SiteReservation Reservation { get; }
            public SiteReservation SourceReservation { get; }
            public SiteReservationSnapshot Snapshot { get; }
        }
    }
}
