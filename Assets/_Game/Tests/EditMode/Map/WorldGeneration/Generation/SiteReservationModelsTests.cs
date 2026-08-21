using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    public sealed class SiteReservationModelsTests
    {
        [TestCase("A")]
        [TestCase("SITE_01")]
        [TestCase("0")]
        [TestCase("START_SITE")]
        [TestCase("ABCDEFGHIJKLMNOPQRSTUVWXYZ_0123456789")]
        public void ReservationId_AcceptsExactCanonicalGrammar(string text)
        {
            var id = new SiteReservationId(text);
            Assert.That(id.IsValid, Is.True);
            Assert.That(id.Value, Is.EqualTo(text));
            Assert.That(id.ToString(), Is.EqualTo(text));
            Assert.That(SiteReservationId.TryCreate(text, out var parsed), Is.True);
            Assert.That(parsed, Is.EqualTo(id));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("site")]
        [TestCase("Site")]
        [TestCase("SITE-1")]
        [TestCase("SITE 1")]
        [TestCase("SITÉ")]
        [TestCase("사이트")]
        [TestCase("SITE.1")]
        public void ReservationId_TryCreateRejectsNonCanonicalGrammar(string text)
        {
            Assert.That(SiteReservationId.TryCreate(text, out var id), Is.False);
            Assert.That(id.IsValid, Is.False);
        }

        [Test]
        public void ReservationId_DefaultIsInvalid()
        {
            var id = default(SiteReservationId);
            Assert.That(id.IsValid, Is.False);
            Assert.That(id.Value, Is.Empty);
            Assert.That(id.ToString(), Is.Empty);
        }

        [Test]
        public void ReservationId_UsesOrdinalValueSemanticsAndDeterministicHash()
        {
            var first = new SiteReservationId("SITE_2");
            var same = new SiteReservationId("SITE_2");
            var later = new SiteReservationId("SITE_20");
            Assert.That(first == same, Is.True);
            Assert.That(first != later, Is.True);
            Assert.That(first.Equals((object)same), Is.True);
            Assert.That(first.GetHashCode(), Is.EqualTo(same.GetHashCode()));
            Assert.That(first.CompareTo(later), Is.LessThan(0));
        }

        [TestCase("en-US")]
        [TestCase("tr-TR")]
        public void ReservationId_IsCultureInvariant(string cultureName)
        {
            WithCulture(cultureName, () =>
            {
                var values = new[] { new SiteReservationId("I_2"), new SiteReservationId("I_1") };
                Array.Sort(values);
                Assert.That(values.Select(value => value.Value), Is.EqualTo(new[] { "I_1", "I_2" }));
            });
        }

        [Test]
        public void Enums_HaveExactOrderedValues()
        {
            Assert.That(Enum.GetNames(typeof(SiteReservationKind)), Is.EqualTo(new[] { "Start", "CoreResource", "Forge", "Boss", "Village" }));
            Assert.That(Enum.GetNames(typeof(SiteFootprintTransform)), Is.EqualTo(new[] { "R0", "MirrorX", "MirrorY", "R180" }));
            Assert.That(Enum.GetNames(typeof(SiteEntrySide)), Is.EqualTo(new[] { "L", "R", "U", "D" }));
        }

        [TestCase(SiteReservationKind.Start, "START")]
        [TestCase(SiteReservationKind.CoreResource, "CORE_RESOURCE")]
        [TestCase(SiteReservationKind.Forge, "FORGE")]
        [TestCase(SiteReservationKind.Boss, "BOSS")]
        [TestCase(SiteReservationKind.Village, "VILLAGE")]
        public void KindCodec_RoundTripsExactTokens(SiteReservationKind value, string token)
        {
            Assert.That(SiteReservationTokenCodec.ToToken(value), Is.EqualTo(token));
            Assert.That(SiteReservationTokenCodec.TryParseKind(token, out var parsed), Is.True);
            Assert.That(parsed, Is.EqualTo(value));
        }

        [TestCase(SiteFootprintTransform.R0, "R0")]
        [TestCase(SiteFootprintTransform.MirrorX, "MIRROR_X")]
        [TestCase(SiteFootprintTransform.MirrorY, "MIRROR_Y")]
        [TestCase(SiteFootprintTransform.R180, "R180")]
        public void TransformCodec_RoundTripsExactTokens(SiteFootprintTransform value, string token)
        {
            Assert.That(SiteReservationTokenCodec.ToToken(value), Is.EqualTo(token));
            Assert.That(SiteReservationTokenCodec.TryParseTransform(token, out var parsed), Is.True);
            Assert.That(parsed, Is.EqualTo(value));
        }

        [TestCase(SiteEntrySide.L, "L")]
        [TestCase(SiteEntrySide.R, "R")]
        [TestCase(SiteEntrySide.U, "U")]
        [TestCase(SiteEntrySide.D, "D")]
        public void EntrySideCodec_RoundTripsExactTokens(SiteEntrySide value, string token)
        {
            Assert.That(SiteReservationTokenCodec.ToToken(value), Is.EqualTo(token));
            Assert.That(SiteReservationTokenCodec.TryParseEntrySide(token, out var parsed), Is.True);
            Assert.That(parsed, Is.EqualTo(value));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("start")]
        [TestCase("0")]
        [TestCase("UNKNOWN")]
        public void TokenCodecs_RejectNonExactTokens(string token)
        {
            Assert.That(SiteReservationTokenCodec.TryParseKind(token, out _), Is.False);
            Assert.That(SiteReservationTokenCodec.TryParseTransform(token, out _), Is.False);
            Assert.That(SiteReservationTokenCodec.TryParseEntrySide(token, out _), Is.False);
        }

        [Test]
        public void TokenCodecs_RejectUndefinedEnums()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SiteReservationTokenCodec.ToToken((SiteReservationKind)999));
            Assert.Throws<ArgumentOutOfRangeException>(() => SiteReservationTokenCodec.ToToken((SiteFootprintTransform)999));
            Assert.Throws<ArgumentOutOfRangeException>(() => SiteReservationTokenCodec.ToToken((SiteEntrySide)999));
        }

        [TestCase(SiteEntrySide.L, SiteEntrySide.R, -1, 0)]
        [TestCase(SiteEntrySide.R, SiteEntrySide.L, 1, 0)]
        [TestCase(SiteEntrySide.U, SiteEntrySide.D, 0, 1)]
        [TestCase(SiteEntrySide.D, SiteEntrySide.U, 0, -1)]
        public void EntrySide_ProvidesExactOppositeAndDelta(SiteEntrySide side, SiteEntrySide opposite, int x, int y)
        {
            Assert.That(SiteReservationTokenCodec.GetOpposite(side), Is.EqualTo(opposite));
            SiteReservationTokenCodec.GetDelta(side, out var deltaX, out var deltaY);
            Assert.That(deltaX, Is.EqualTo(x));
            Assert.That(deltaY, Is.EqualTo(y));
            Assert.That(SiteReservationTokenCodec.GetDeltaX(side), Is.EqualTo(x));
            Assert.That(SiteReservationTokenCodec.GetDeltaY(side), Is.EqualTo(y));
        }

        [Test]
        public void FootprintCell_PreservesFieldsAndCanonicalSideOrder()
        {
            var sides = new List<SiteEntrySide> { SiteEntrySide.D, SiteEntrySide.L, SiteEntrySide.U };
            var cell = new SiteFootprintCell(2, 1, "CORE", "BIOME_A", "RECIPE_A", sides);
            sides.Clear();
            Assert.That(cell.LocalX, Is.EqualTo(2));
            Assert.That(cell.LocalY, Is.EqualTo(1));
            Assert.That(cell.LocalRole, Is.EqualTo("CORE"));
            Assert.That(cell.RequiredPrimaryBiomeId, Is.EqualTo("BIOME_A"));
            Assert.That(cell.FixedSectorRecipeId, Is.EqualTo("RECIPE_A"));
            Assert.That(cell.RequiredOpenSides, Is.EqualTo(new[] { SiteEntrySide.L, SiteEntrySide.U, SiteEntrySide.D }));
            AssertReadOnly(cell.RequiredOpenSides);
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        public void FootprintCell_RejectsNegativeCoordinates(int x, int y)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SiteFootprintCell(x, y, "CELL", "", "", Array.Empty<SiteEntrySide>()));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("cell")]
        [TestCase("CELL-X")]
        public void FootprintCell_RejectsInvalidRequiredRole(string role)
        {
            Assert.Catch<ArgumentException>(() => new SiteFootprintCell(0, 0, role, "", "", Array.Empty<SiteEntrySide>()));
        }

        [Test]
        public void FootprintCell_RejectsDuplicateOrUndefinedSides()
        {
            Assert.Throws<ArgumentException>(() => Cell(0, 0, "A", new[] { SiteEntrySide.L, SiteEntrySide.L }));
            Assert.Throws<ArgumentOutOfRangeException>(() => Cell(0, 0, "A", new[] { (SiteEntrySide)999 }));
        }

        [TestCase(0, 1)]
        [TestCase(14, 1)]
        [TestCase(1, 0)]
        [TestCase(1, 14)]
        public void Footprint_RejectsInvalidDimensions(int width, int height)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SiteFootprint(width, height, SiteFootprintTransform.R0, new[] { Cell(0, 0, "A") }));
        }

        [Test]
        public void Footprint_RejectsInvalidCollectionsAndCoordinates()
        {
            Assert.Throws<ArgumentNullException>(() => new SiteFootprint(1, 1, SiteFootprintTransform.R0, null));
            Assert.Throws<ArgumentException>(() => new SiteFootprint(1, 1, SiteFootprintTransform.R0, Array.Empty<SiteFootprintCell>()));
            Assert.Throws<ArgumentException>(() => new SiteFootprint(1, 1, SiteFootprintTransform.R0, new SiteFootprintCell[] { null }));
            Assert.Throws<ArgumentException>(() => new SiteFootprint(1, 1, SiteFootprintTransform.R0, new[] { Cell(1, 0, "A") }));
            Assert.Throws<ArgumentException>(() => new SiteFootprint(1, 1, SiteFootprintTransform.R0, new[] { Cell(0, 0, "A"), Cell(0, 0, "B") }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SiteFootprint(1, 1, (SiteFootprintTransform)999, new[] { Cell(0, 0, "A") }));
        }

        [Test]
        public void Footprint_IsSparseSortedCopiedAndLookupStable()
        {
            var source = new List<SiteFootprintCell> { Cell(2, 1, "C"), Cell(0, 0, "A"), Cell(2, 0, "B") };
            var footprint = new SiteFootprint(3, 2, SiteFootprintTransform.MirrorX, source);
            source.Clear();
            Assert.That(footprint.Cells.Select(cell => cell.LocalRole), Is.EqualTo(new[] { "A", "B", "C" }));
            Assert.That(footprint.Transform, Is.EqualTo(SiteFootprintTransform.MirrorX));
            Assert.That(footprint.TryGetCell(2, 1, out var found), Is.True);
            Assert.That(found.LocalRole, Is.EqualTo("C"));
            Assert.That(footprint.TryGetCell(1, 0, out found), Is.False);
            Assert.That(found, Is.Null);
            AssertReadOnly(footprint.Cells);
        }

        [Test]
        public void EntryAnchor_SortsCopiesAndPreservesRouteContract()
        {
            var routes = new List<int> { 3, 1, 2 };
            var anchor = new SiteEntryAnchor(Id("SITE"), "ENTRY_A", new SectorCoord(5, 5), SiteEntrySide.R, routes, true, true);
            routes.Clear();
            Assert.That(anchor.AllowedRouteTypes, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(anchor.Required, Is.True);
            Assert.That(anchor.ReturnPathRequired, Is.True);
            AssertReadOnly(anchor.AllowedRouteTypes);
        }

        [TestCase(SiteEntrySide.L, 4, 5)]
        [TestCase(SiteEntrySide.R, 6, 5)]
        [TestCase(SiteEntrySide.U, 5, 6)]
        [TestCase(SiteEntrySide.D, 5, 4)]
        public void EntryAnchor_ComputesExteriorOnce(SiteEntrySide side, int x, int y)
        {
            var anchor = new SiteEntryAnchor(Id("SITE"), "ENTRY", new SectorCoord(5, 5), side, new[] { 1 }, false, false);
            Assert.That(anchor.TryGetExteriorSector(out var exterior), Is.True);
            Assert.That(exterior, Is.EqualTo(new SectorCoord(x, y)));
        }

        [TestCase(0, 5, SiteEntrySide.L)]
        [TestCase(12, 5, SiteEntrySide.R)]
        [TestCase(5, 12, SiteEntrySide.U)]
        [TestCase(5, 0, SiteEntrySide.D)]
        public void EntryAnchor_RejectsExteriorBeyondBoundaryWithoutClamp(int x, int y, SiteEntrySide side)
        {
            var anchor = new SiteEntryAnchor(Id("SITE"), "ENTRY", new SectorCoord(x, y), side, new[] { 1 }, false, false);
            Assert.That(anchor.TryGetExteriorSector(out var exterior), Is.False);
            Assert.That(exterior, Is.EqualTo(default(SectorCoord)));
        }

        [Test]
        public void EntryAnchor_RejectsInvalidIdentitySectorSideAndRoutes()
        {
            Assert.Throws<ArgumentException>(() => new SiteEntryAnchor(default(SiteReservationId), "ENTRY", new SectorCoord(0, 0), SiteEntrySide.L, new[] { 1 }, false, false));
            Assert.Throws<ArgumentException>(() => new SiteEntryAnchor(Id("SITE"), "entry", new SectorCoord(0, 0), SiteEntrySide.L, new[] { 1 }, false, false));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SiteEntryAnchor(Id("SITE"), "ENTRY", new SectorCoord(-1, 0), SiteEntrySide.L, new[] { 1 }, false, false));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SiteEntryAnchor(Id("SITE"), "ENTRY", new SectorCoord(0, 0), (SiteEntrySide)999, new[] { 1 }, false, false));
            Assert.Throws<ArgumentNullException>(() => new SiteEntryAnchor(Id("SITE"), "ENTRY", new SectorCoord(0, 0), SiteEntrySide.L, null, false, false));
            Assert.Throws<ArgumentException>(() => new SiteEntryAnchor(Id("SITE"), "ENTRY", new SectorCoord(0, 0), SiteEntrySide.L, Array.Empty<int>(), false, false));
            Assert.Throws<ArgumentException>(() => new SiteEntryAnchor(Id("SITE"), "ENTRY", new SectorCoord(0, 0), SiteEntrySide.L, new[] { 1, 1 }, false, false));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SiteEntryAnchor(Id("SITE"), "ENTRY", new SectorCoord(0, 0), SiteEntrySide.L, new[] { 0 }, false, false));
        }

        [Test]
        public void CoreBiomeSeed_PreservesImmutableFields()
        {
            var seed = new CoreBiomeSeed(Id("CORE"), "BIOME_A", "CORE_RULE", new SectorCoord(4, 5), 6, 1);
            Assert.That(seed.SourceReservationId, Is.EqualTo(Id("CORE")));
            Assert.That(seed.BiomeId, Is.EqualTo("BIOME_A"));
            Assert.That(seed.CorePatchRuleId, Is.EqualTo("CORE_RULE"));
            Assert.That(seed.SeedSector, Is.EqualTo(new SectorCoord(4, 5)));
            Assert.That(seed.MinimumCoreSectorCount, Is.EqualTo(6));
            Assert.That(seed.BufferRingSectors, Is.EqualTo(1));
        }

        [Test]
        public void CoreBiomeSeed_RejectsInvalidValues()
        {
            Assert.Throws<ArgumentException>(() => new CoreBiomeSeed(default(SiteReservationId), "BIOME", "RULE", new SectorCoord(0, 0), 1, 0));
            Assert.Throws<ArgumentException>(() => new CoreBiomeSeed(Id("CORE"), "biome", "RULE", new SectorCoord(0, 0), 1, 0));
            Assert.Throws<ArgumentException>(() => new CoreBiomeSeed(Id("CORE"), "BIOME", "", new SectorCoord(0, 0), 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CoreBiomeSeed(Id("CORE"), "BIOME", "RULE", new SectorCoord(13, 0), 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CoreBiomeSeed(Id("CORE"), "BIOME", "RULE", new SectorCoord(0, 0), 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CoreBiomeSeed(Id("CORE"), "BIOME", "RULE", new SectorCoord(0, 0), 1, -1));
        }

        [Test]
        public void SectorReservation_UsesExactReservedAndUnreservedShapes()
        {
            var coordinate = new SectorCoord(2, 3);
            var index = WorldGridIndex.ToIndex(coordinate);
            var empty = SectorReservation.CreateUnreserved(index, coordinate);
            Assert.That(empty.IsReserved, Is.False);
            Assert.That(empty.ReservationId, Is.Null);
            Assert.That(empty.Kind, Is.Null);
            Assert.That(empty.LocalX, Is.EqualTo(-1));
            Assert.That(empty.LocalY, Is.EqualTo(-1));
            Assert.That(empty.LocalRole, Is.Empty);
            var reserved = SectorReservation.CreateReserved(index, coordinate, Id("SITE"), SiteReservationKind.Boss, 1, 2, "BOSS_ROOM");
            Assert.That(reserved.IsReserved, Is.True);
            Assert.That(reserved.ReservationId, Is.EqualTo(Id("SITE")));
            Assert.That(reserved.Kind, Is.EqualTo(SiteReservationKind.Boss));
            Assert.That(reserved.LocalRole, Is.EqualTo("BOSS_ROOM"));
        }

        [Test]
        public void SectorReservation_RejectsGridMismatchAndInvalidReservedValues()
        {
            Assert.Throws<ArgumentException>(() => SectorReservation.CreateUnreserved(0, new SectorCoord(1, 0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => SectorReservation.CreateUnreserved(-1, new SectorCoord(0, 0)));
            Assert.Throws<ArgumentException>(() => SectorReservation.CreateReserved(0, new SectorCoord(0, 0), default(SiteReservationId), SiteReservationKind.Start, 0, 0, "START"));
            Assert.Throws<ArgumentOutOfRangeException>(() => SectorReservation.CreateReserved(0, new SectorCoord(0, 0), Id("SITE"), (SiteReservationKind)999, 0, 0, "START"));
            Assert.Throws<ArgumentOutOfRangeException>(() => SectorReservation.CreateReserved(0, new SectorCoord(0, 0), Id("SITE"), SiteReservationKind.Start, -1, 0, "START"));
            Assert.Throws<ArgumentException>(() => SectorReservation.CreateReserved(0, new SectorCoord(0, 0), Id("SITE"), SiteReservationKind.Start, 0, 0, "start"));
        }

        [Test]
        public void SiteReservation_MapsFinalCellsAndSortsAnchorsAndOccupiedSectors()
        {
            var footprint = new SiteFootprint(2, 2, SiteFootprintTransform.R180, new[] { Cell(1, 1, "C"), Cell(0, 0, "A"), Cell(1, 0, "B") });
            var id = Id("SITE");
            var anchors = new List<SiteEntryAnchor>
            {
                new SiteEntryAnchor(id, "Z_ENTRY", new SectorCoord(4, 4), SiteEntrySide.D, new[] { 2 }, false, false),
                new SiteEntryAnchor(id, "A_ENTRY", new SectorCoord(3, 3), SiteEntrySide.L, new[] { 1 }, true, true)
            };
            var site = new SiteReservation(id, SiteReservationKind.Boss, "BOSS_DEF", new SectorCoord(3, 3), footprint, "BIOME_A", 2, anchors);
            anchors.Clear();
            Assert.That(site.EntryAnchors.Select(anchor => anchor.EntrySocketId), Is.EqualTo(new[] { "A_ENTRY", "Z_ENTRY" }));
            Assert.That(site.OccupiedSectors, Is.EqualTo(new[] { new SectorCoord(3, 3), new SectorCoord(4, 3), new SectorCoord(4, 4) }));
            Assert.That(site.TryGetFootprintCell(new SectorCoord(4, 4), out var cell), Is.True);
            Assert.That(cell.LocalRole, Is.EqualTo("C"));
            AssertReadOnly(site.EntryAnchors);
            AssertReadOnly(site.OccupiedSectors);
        }

        [Test]
        public void SiteReservation_RejectsInvalidIdentityBoundsAndEntries()
        {
            var footprint = OneCellFootprint("CELL");
            Assert.Throws<ArgumentException>(() => new SiteReservation(default(SiteReservationId), SiteReservationKind.Start, "DEF", new SectorCoord(0, 0), footprint, "", 0, Array.Empty<SiteEntryAnchor>()));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SiteReservation(Id("SITE"), (SiteReservationKind)999, "DEF", new SectorCoord(0, 0), footprint, "", 0, Array.Empty<SiteEntryAnchor>()));
            Assert.Throws<ArgumentException>(() => new SiteReservation(Id("SITE"), SiteReservationKind.Start, "def", new SectorCoord(0, 0), footprint, "", 0, Array.Empty<SiteEntryAnchor>()));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SiteReservation(Id("SITE"), SiteReservationKind.Start, "DEF", new SectorCoord(-1, 0), footprint, "", 0, Array.Empty<SiteEntryAnchor>()));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SiteReservation(Id("SITE"), SiteReservationKind.Start, "DEF", new SectorCoord(0, 0), footprint, "", -1, Array.Empty<SiteEntryAnchor>()));
            Assert.Throws<ArgumentException>(() => new SiteReservation(Id("SITE"), SiteReservationKind.Start, "DEF", new SectorCoord(12, 12), new SiteFootprint(2, 1, SiteFootprintTransform.R0, new[] { Cell(1, 0, "CELL") }), "", 0, Array.Empty<SiteEntryAnchor>()));
            var wrongId = new SiteEntryAnchor(Id("OTHER"), "ENTRY", new SectorCoord(0, 0), SiteEntrySide.L, new[] { 1 }, false, false);
            Assert.Throws<ArgumentException>(() => new SiteReservation(Id("SITE"), SiteReservationKind.Start, "DEF", new SectorCoord(0, 0), footprint, "", 0, new[] { wrongId }));
            var outside = new SiteEntryAnchor(Id("SITE"), "ENTRY", new SectorCoord(1, 0), SiteEntrySide.L, new[] { 1 }, false, false);
            Assert.Throws<ArgumentException>(() => new SiteReservation(Id("SITE"), SiteReservationKind.Start, "DEF", new SectorCoord(0, 0), footprint, "", 0, new[] { outside }));
            var duplicate = new SiteEntryAnchor(Id("SITE"), "ENTRY", new SectorCoord(0, 0), SiteEntrySide.L, new[] { 1 }, false, false);
            Assert.Throws<ArgumentException>(() => new SiteReservation(Id("SITE"), SiteReservationKind.Start, "DEF", new SectorCoord(0, 0), footprint, "", 0, new[] { duplicate, duplicate }));
        }

        [Test]
        public void Snapshot_BuildsExactImmutableArtifactAndStableLookups()
        {
            var start = CreateSite("START", SiteReservationKind.Start, 5, 5, 1, 20);
            var core = CreateSite("CORE", SiteReservationKind.CoreResource, 8, 7, 0, 10);
            var reservations = new List<SiteReservation> { start, core };
            var sectors = CreateSectors(reservations);
            sectors.Reverse();
            var seeds = new List<CoreBiomeSeed> { new CoreBiomeSeed(core.ReservationId, "BIOME_A", "CORE_RULE", core.Origin, 5, 1) };
            var snapshot = new SiteReservationSnapshot(42, reservations, sectors, seeds);
            reservations.Clear();
            sectors.Clear();
            seeds.Clear();
            Assert.That(snapshot.Seed, Is.EqualTo(42UL));
            Assert.That(snapshot.StartReservation, Is.SameAs(start));
            Assert.That(snapshot.StartAnchor, Is.EqualTo(start.Origin));
            Assert.That(snapshot.Reservations.Select(item => item.ReservationId.Value), Is.EqualTo(new[] { "CORE", "START" }));
            Assert.That(snapshot.Sectors.Select(item => item.Index), Is.EqualTo(Enumerable.Range(0, 169)));
            Assert.That(snapshot.EntryAnchors.Select(item => item.ReservationId.Value), Is.EqualTo(new[] { "CORE", "START" }));
            Assert.That(snapshot.CoreBiomeSeeds.Single().SourceReservationId, Is.EqualTo(core.ReservationId));
            Assert.That(snapshot.GetSector(core.Origin).ReservationId, Is.EqualTo(core.ReservationId));
            Assert.That(snapshot.GetSector(WorldGridIndex.ToIndex(start.Origin)).ReservationId, Is.EqualTo(start.ReservationId));
            Assert.That(snapshot.TryGetReservation(Id("CORE"), out var found), Is.True);
            Assert.That(found, Is.SameAs(core));
            Assert.That(snapshot.TryGetReservation(Id("MISSING"), out found), Is.False);
            Assert.That(found, Is.Null);
            AssertReadOnly(snapshot.Reservations);
            AssertReadOnly(snapshot.Sectors);
            AssertReadOnly(snapshot.EntryAnchors);
            AssertReadOnly(snapshot.CoreBiomeSeeds);
        }

        [Test]
        public void Snapshot_RejectsMissingOrDuplicateStartAndDuplicateIdentityOrOrder()
        {
            var core = CreateSite("CORE", SiteReservationKind.CoreResource, 2, 2, 0, 0);
            Assert.Throws<ArgumentException>(() => new SiteReservationSnapshot(0, new[] { core }, CreateSectors(new[] { core }), Array.Empty<CoreBiomeSeed>()));
            var startA = CreateSite("START_A", SiteReservationKind.Start, 0, 0, 0, 0);
            var startB = CreateSite("START_B", SiteReservationKind.Start, 1, 0, 1, 1);
            Assert.Throws<ArgumentException>(() => new SiteReservationSnapshot(0, new[] { startA, startB }, CreateSectors(new[] { startA, startB }), Array.Empty<CoreBiomeSeed>()));
            var duplicateId = CreateSite("START_A", SiteReservationKind.Boss, 2, 0, 1, 2);
            Assert.Throws<ArgumentException>(() => new SiteReservationSnapshot(0, new[] { startA, duplicateId }, CreateSectors(new[] { startA, duplicateId }), Array.Empty<CoreBiomeSeed>()));
            var duplicateOrder = CreateSite("BOSS", SiteReservationKind.Boss, 2, 0, 0, 2);
            Assert.Throws<ArgumentException>(() => new SiteReservationSnapshot(0, new[] { startA, duplicateOrder }, CreateSectors(new[] { startA, duplicateOrder }), Array.Empty<CoreBiomeSeed>()));
        }

        [Test]
        public void Snapshot_RejectsMissingExtraWrongOrOrphanSectorState()
        {
            var start = CreateSite("START", SiteReservationKind.Start, 0, 0, 0, 0);
            var valid = CreateSectors(new[] { start });
            Assert.Throws<ArgumentException>(() => new SiteReservationSnapshot(0, new[] { start }, valid.Take(168), Array.Empty<CoreBiomeSeed>()));
            var duplicate = new List<SectorReservation>(valid) { valid[0] };
            Assert.Throws<ArgumentException>(() => new SiteReservationSnapshot(0, new[] { start }, duplicate, Array.Empty<CoreBiomeSeed>()));
            var wrong = new List<SectorReservation>(valid);
            wrong[0] = SectorReservation.CreateReserved(0, new SectorCoord(0, 0), Id("OTHER"), SiteReservationKind.Start, 0, 0, "CELL");
            Assert.Throws<ArgumentException>(() => new SiteReservationSnapshot(0, new[] { start }, wrong, Array.Empty<CoreBiomeSeed>()));
            var orphan = new List<SectorReservation>(valid);
            orphan[1] = SectorReservation.CreateReserved(1, new SectorCoord(1, 0), start.ReservationId, SiteReservationKind.Start, 1, 0, "CELL");
            Assert.Throws<ArgumentException>(() => new SiteReservationSnapshot(0, new[] { start }, orphan, Array.Empty<CoreBiomeSeed>()));
        }

        [Test]
        public void Snapshot_RejectsOverlapAndInvalidCoreSeedOwnership()
        {
            var start = CreateSite("START", SiteReservationKind.Start, 0, 0, 0, 0);
            var boss = CreateSite("BOSS", SiteReservationKind.Boss, 0, 0, 1, 1);
            Assert.Throws<ArgumentException>(() => new SiteReservationSnapshot(0, new[] { start, boss }, CreateSectors(new[] { start }), Array.Empty<CoreBiomeSeed>()));
            var validSectors = CreateSectors(new[] { start });
            var missing = new CoreBiomeSeed(Id("MISSING"), "BIOME", "RULE", new SectorCoord(0, 0), 1, 0);
            Assert.Throws<ArgumentException>(() => new SiteReservationSnapshot(0, new[] { start }, validSectors, new[] { missing }));
            var wrongKind = new CoreBiomeSeed(start.ReservationId, "BIOME", "RULE", start.Origin, 1, 0);
            Assert.Throws<ArgumentException>(() => new SiteReservationSnapshot(0, new[] { start }, validSectors, new[] { wrongKind }));
            var core = CreateSite("CORE", SiteReservationKind.CoreResource, 2, 2, 1, 1);
            var both = new[] { start, core };
            var seed = new CoreBiomeSeed(core.ReservationId, "BIOME", "RULE", core.Origin, 1, 0);
            Assert.Throws<ArgumentException>(() => new SiteReservationSnapshot(0, both, CreateSectors(both), new[] { seed, seed }));
        }

        [TestCase("en-US")]
        [TestCase("tr-TR")]
        public void Snapshot_OrderingIsCultureAndInsertionOrderInvariant(string cultureName)
        {
            WithCulture(cultureName, () =>
            {
                var start = CreateSite("I_START", SiteReservationKind.Start, 0, 0, 2, 2);
                var forge = CreateSite("I_FORGE", SiteReservationKind.Forge, 2, 2, 1, 1);
                var input = new[] { start, forge };
                var first = new SiteReservationSnapshot(1, input, CreateSectors(input), new[] { new CoreBiomeSeed(forge.ReservationId, "BIOME", "RULE", forge.Origin, 1, 0) });
                var reversed = input.Reverse().ToArray();
                var secondSectors = CreateSectors(reversed);
                secondSectors.Reverse();
                var second = new SiteReservationSnapshot(1, reversed, secondSectors, new[] { new CoreBiomeSeed(forge.ReservationId, "BIOME", "RULE", forge.Origin, 1, 0) });
                Assert.That(first.Reservations.Select(item => item.ReservationId.Value), Is.EqualTo(second.Reservations.Select(item => item.ReservationId.Value)));
                Assert.That(first.Sectors.Select(item => item.Index), Is.EqualTo(second.Sectors.Select(item => item.Index)));
            });
        }

        [Test]
        public void RuntimeModels_HaveNoPublicMutationSurfaceOrForbiddenDependencies()
        {
            var types = new[]
            {
                typeof(SiteReservationId), typeof(SiteReservationKind), typeof(SiteFootprintTransform),
                typeof(SiteEntrySide), typeof(SiteReservationTokenCodec), typeof(SiteFootprintCell),
                typeof(SiteFootprint), typeof(SiteEntryAnchor), typeof(CoreBiomeSeed),
                typeof(SectorReservation), typeof(SiteReservation), typeof(SiteReservationSnapshot)
            };
            foreach (var type in types)
            {
                if (!type.IsEnum)
                {
                    Assert.That(type.GetFields(BindingFlags.Instance | BindingFlags.Public), Is.Empty, type.FullName);
                }
                Assert.That(type.GetProperties(BindingFlags.Instance | BindingFlags.Public).All(property => property.SetMethod == null), Is.True, type.FullName);
                Assert.That(type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(field => !field.IsLiteral).ToArray(), Is.Empty, type.FullName);
                var surface = type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Select(member => member.ToString()).ToArray();
                Assert.That(surface.Any(value => value.Contains("UnityEditor") || value.Contains("UnityEngine.Object") || value.Contains("System.IO") || value.Contains("Random")), Is.False, type.FullName);
            }
        }

        private static SiteReservationId Id(string value)
        {
            return new SiteReservationId(value);
        }

        private static SiteFootprintCell Cell(int x, int y, string role, IEnumerable<SiteEntrySide> sides = null)
        {
            return new SiteFootprintCell(x, y, role, string.Empty, string.Empty, sides ?? Array.Empty<SiteEntrySide>());
        }

        private static SiteFootprint OneCellFootprint(string role)
        {
            return new SiteFootprint(1, 1, SiteFootprintTransform.R0, new[] { Cell(0, 0, role) });
        }

        private static SiteReservation CreateSite(string id, SiteReservationKind kind, int x, int y, int order, int socketOrdinal)
        {
            var reservationId = Id(id);
            var coordinate = new SectorCoord(x, y);
            var anchor = new SiteEntryAnchor(reservationId, "ENTRY_" + socketOrdinal.ToString(CultureInfo.InvariantCulture), coordinate, SiteEntrySide.R, new[] { 1, 2, 3 }, true, true);
            return new SiteReservation(reservationId, kind, id + "_DEF", coordinate, OneCellFootprint("CELL"), kind == SiteReservationKind.Start ? string.Empty : "BIOME_A", order, new[] { anchor });
        }

        private static List<SectorReservation> CreateSectors(IEnumerable<SiteReservation> reservations)
        {
            var bindings = new Dictionary<SectorCoord, Tuple<SiteReservation, SiteFootprintCell>>();
            foreach (var reservation in reservations)
            {
                foreach (var coordinate in reservation.OccupiedSectors)
                {
                    reservation.TryGetFootprintCell(coordinate, out var cell);
                    if (!bindings.ContainsKey(coordinate)) bindings.Add(coordinate, Tuple.Create(reservation, cell));
                }
            }
            var result = new List<SectorReservation>(WorldGenConstants.SectorCount);
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var coordinate = WorldGridIndex.ToCoordinate(index);
                if (bindings.TryGetValue(coordinate, out var binding))
                    result.Add(SectorReservation.CreateReserved(index, coordinate, binding.Item1.ReservationId, binding.Item1.Kind, binding.Item2.LocalX, binding.Item2.LocalY, binding.Item2.LocalRole));
                else
                    result.Add(SectorReservation.CreateUnreserved(index, coordinate));
            }
            return result;
        }

        private static void AssertReadOnly<T>(IReadOnlyList<T> values)
        {
            Assert.That(((IList)values).IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => ((IList)values).Add(default(T)));
        }

        private static void WithCulture(string name, Action action)
        {
            var original = CultureInfo.CurrentCulture;
            var originalUi = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo(name);
                CultureInfo.CurrentUICulture = new CultureInfo(name);
                action();
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
                CultureInfo.CurrentUICulture = originalUi;
            }
        }
    }
}
