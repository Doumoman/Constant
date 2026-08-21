using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    [Category("MAP05_01")]
    public sealed class MandatoryTerminalBuilderTests
    {
        private static readonly string[] ExpectedTerminalIds =
        {
            "TERM_00_START",
            "TERM_01_SITE_MOON_BOSS_VAULT_ENTRY_L",
            "TERM_02_SITE_MOON_SEAL_FORGE_ENTRY_L",
            "TERM_03_SITE_CASSIA_SAP_HEART_ENTRY_L",
            "TERM_04_SITE_DEEP_STAR_YEAST_ENTRY_L",
            "TERM_05_SITE_MOON_CORE_METEOR_ENTRY_L",
            "TERM_06_SITE_PRIMARY_VILLAGE_ENTRY_L"
        };

        private SiteReservationSnapshot site;
        private BiomePatchValidationPublication biome;
        private MandatoryTerminalBuilder reused;
        private string expectedSignature;

        public static IEnumerable<TestCaseData> CanonicalCases
        {
            get
            {
                for (var index = 0; index < 96; index++)
                    yield return new TestCaseData(index).SetName(
                        "Build_CanonicalDeterministicTerminalSet_" +
                        index.ToString("D3", CultureInfo.InvariantCulture));
            }
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var fixture = new BiomePatchValidatorTests();
            fixture.OneTimeSetUp();
            var type = typeof(BiomePatchValidatorTests);
            var export = GetField<BiomePatchExportResult>(fixture, type, "export");
            var biomes = GetField<BiomeTypeDefinition[]>(fixture, type, "biomes");
            var rules = GetField<BiomePatchRuleDefinition[]>(fixture, type, "rules");
            var profiles = GetField<BiomeBoundaryProfileDefinition[]>(fixture, type, "profiles");
            var pairs = GetField<BiomeBoundaryPairRuleDefinition[]>(fixture, type, "pairs");
            var result = new BiomePatchValidator().Validate(export, biomes, rules, profiles, pairs);
            Assert.That(result.Status, Is.EqualTo(BiomePatchValidationStatus.Completed));
            biome = result.Publication;
            var biomeSource = biome.SourceExport.SourceCleanup.SourceIntrusion.Publication;
            site = CreateApprovedEntrySnapshot(biomeSource.SourceSiteSnapshot);
            SetField(biomeSource, "<SourceSiteSnapshot>k__BackingField", site);
            Assert.That(site.Reservations, Has.Count.EqualTo(7));
            Assert.That(site.Sectors, Has.Count.EqualTo(169));
            Assert.That(site.EntryAnchors, Has.Count.EqualTo(6));
            Assert.That(site.CoreBiomeSeeds, Has.Count.EqualTo(4));
            reused = new MandatoryTerminalBuilder();
            var baseline = reused.Build(site, biome);
            Assert.That(baseline.Status, Is.EqualTo(MandatoryTerminalBuildStatus.Completed), ErrorSignature(baseline));
            expectedSignature = Signature(baseline);
        }

        [TestCaseSource(nameof(CanonicalCases))]
        public void Build_CanonicalDeterministicTerminalSet(int caseId)
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = (caseId & 1) == 0
                    ? CultureInfo.GetCultureInfo("en-US")
                    : CultureInfo.GetCultureInfo("tr-TR");
                var builder = (caseId & 2) == 0 ? new MandatoryTerminalBuilder() : reused;
                var result = builder.Build(site, biome);
                Assert.That(result.Status, Is.EqualTo(MandatoryTerminalBuildStatus.Completed), ErrorSignature(result));
                Assert.That(result.Succeeded, Is.True);
                Assert.That(result.RetryRequired, Is.False);
                Assert.That(result.Errors, Is.Empty);
                Assert.That(Signature(result), Is.EqualTo(expectedSignature));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Test]
        public void CompletedSetHasExactSevenIdsOrderAndLookups()
        {
            var set = Complete().TerminalSet;
            Assert.That(set.TerminalCount, Is.EqualTo(7));
            Assert.That(set.SiteEntryTerminalCount, Is.EqualTo(6));
            Assert.That(set.Terminals.Select(value => value.TerminalOrder), Is.EqualTo(Enumerable.Range(0, 7)));
            Assert.That(set.Terminals.Select(value => value.TerminalId.Value), Is.EqualTo(ExpectedTerminalIds));
            foreach (var terminal in set.Terminals)
            {
                Assert.That(set.TryGet(terminal.TerminalId, out var byId), Is.True);
                Assert.That(byId, Is.SameAs(terminal));
                Assert.That(set.TryGetByReservation(terminal.ReservationId, out var byReservation), Is.True);
                Assert.That(byReservation, Is.SameAs(terminal));
            }
        }

        [Test]
        public void StartTerminalPreservesExactP01Semantics()
        {
            var terminal = Complete().TerminalSet.StartTerminal;
            Assert.That(terminal.TerminalId.Value, Is.EqualTo("TERM_00_START"));
            Assert.That(terminal.Kind, Is.EqualTo(MandatoryRouteTerminalKind.Start));
            Assert.That(terminal.TerminalOrder, Is.Zero);
            Assert.That(terminal.ReservationKind, Is.EqualTo(SiteReservationKind.Start));
            Assert.That(terminal.EntrySocketId, Is.Empty);
            Assert.That(terminal.AnchorSector, Is.EqualTo(site.StartAnchor));
            Assert.That(terminal.ApproachSector, Is.EqualTo(site.StartAnchor));
            Assert.That(terminal.EntrySide, Is.Null);
            Assert.That(terminal.AllowedRouteTypes, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(terminal.Required && terminal.ReturnPathRequired, Is.True);
        }

        [Test]
        public void SixSiteEntriesPreserveSourceAnchorExteriorSideRoutesAndFlags()
        {
            var set = Complete().TerminalSet;
            foreach (var reservation in site.Reservations.Where(value => value.Kind != SiteReservationKind.Start))
            {
                Assert.That(set.TryGetByReservation(reservation.ReservationId, out var terminal), Is.True);
                var entry = reservation.EntryAnchors.Single();
                Assert.That(entry.TryGetExteriorSector(out var exterior), Is.True);
                Assert.That(terminal.Kind, Is.EqualTo(MandatoryRouteTerminalKind.SiteEntry));
                Assert.That(terminal.AnchorSector, Is.EqualTo(entry.FootprintSector));
                Assert.That(terminal.ApproachSector, Is.EqualTo(exterior));
                Assert.That(terminal.EntrySide, Is.EqualTo(entry.Side));
                Assert.That(terminal.EntrySocketId, Is.EqualTo(entry.EntrySocketId));
                Assert.That(terminal.AllowedRouteTypes, Is.EqualTo(new[] { 1, 2, 3 }));
                Assert.That(terminal.Required && terminal.ReturnPathRequired, Is.True);
                Assert.That(site.GetSector(exterior).IsReserved, Is.False);
            }
        }

        [Test]
        public void DiagnosticsAreExactAndSourcesAreReferencePreserved()
        {
            var result = Complete();
            var diagnostics = result.Diagnostics;
            Assert.That(result.TerminalSet.SourceSiteSnapshot, Is.SameAs(site));
            Assert.That(result.TerminalSet.SourceBiomePublication, Is.SameAs(biome));
            Assert.That(diagnostics.WorldSeed, Is.EqualTo(site.Seed));
            Assert.That(new[]
            {
                diagnostics.ReservationCount, diagnostics.ReservedSectorCount,
                diagnostics.BiomePatchCount, diagnostics.BiomeAssignedSectorCount,
                diagnostics.BiomeUnassignedSectorCount, diagnostics.TerminalCount,
                diagnostics.StartTerminalCount, diagnostics.SiteEntryTerminalCount,
                diagnostics.RequiredTerminalCount, diagnostics.ReturnPathRequiredTerminalCount,
                diagnostics.RngDrawCount, diagnostics.SourceMutationCount
            }, Is.EqualTo(new[] { 7, 8, 17, 165, 4, 7, 1, 6, 7, 7, 0, 0 }));
        }

        [Test]
        public void NullInputsAccumulateSortedErrorsAndPublishNothing()
        {
            var result = new MandatoryTerminalBuilder().Build(null, null);
            Assert.That(result.Status, Is.EqualTo(MandatoryTerminalBuildStatus.InvalidInput));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.RetryRequired, Is.False);
            Assert.That(result.TerminalSet, Is.Null);
            Assert.That(result.Diagnostics, Is.Null);
            Assert.That(result.Errors.Select(value => value.Code),
                Is.EqualTo(new[] { MandatoryTerminalBuildErrorCode.MissingInput, MandatoryTerminalBuildErrorCode.MissingInput }));
            Assert.That(result.Errors.Select(value => value.FirstId), Is.EqualTo(new[] { "BIOME_PUBLICATION", "SITE_SNAPSHOT" }));
        }

        [Test]
        public void MissingEitherInputIsAtomicInvalidInput()
        {
            var missingSite = new MandatoryTerminalBuilder().Build(null, biome);
            var missingBiome = new MandatoryTerminalBuilder().Build(site, null);
            Assert.That(missingSite.TerminalSet, Is.Null);
            Assert.That(missingBiome.TerminalSet, Is.Null);
            Assert.That(missingSite.Errors.Any(value => value.Code == MandatoryTerminalBuildErrorCode.MissingInput), Is.True);
            Assert.That(missingBiome.Errors.Any(value => value.Code == MandatoryTerminalBuildErrorCode.MissingInput), Is.True);
        }

        [Test]
        public void SameLogicalButDifferentSiteSnapshotReferenceIsRejected()
        {
            var clone = new SiteReservationSnapshot(site.Seed, site.Reservations, site.Sectors, site.CoreBiomeSeeds);
            var result = new MandatoryTerminalBuilder().Build(clone, biome);
            Assert.That(result.Status, Is.EqualTo(MandatoryTerminalBuildStatus.InvalidInput));
            Assert.That(result.Errors.Any(value => value.Code == MandatoryTerminalBuildErrorCode.SourceSnapshotMismatch), Is.True);
        }

        [Test]
        public void DifferentWorldSeedIsRejectedWithoutOutput()
        {
            var clone = new SiteReservationSnapshot(site.Seed + 1UL, site.Reservations, site.Sectors, site.CoreBiomeSeeds);
            var result = new MandatoryTerminalBuilder().Build(clone, biome);
            Assert.That(result.TerminalSet, Is.Null);
            Assert.That(result.Diagnostics, Is.Null);
            Assert.That(result.Errors.Any(value => value.Code == MandatoryTerminalBuildErrorCode.WorldSeedMismatch), Is.True);
            Assert.That(result.Errors.Any(value => value.Code == MandatoryTerminalBuildErrorCode.SourceSnapshotMismatch), Is.True);
        }

        [Test]
        public void MissingNonStartEntryAndStartEntryAreRejected()
        {
            var nonStart = site.Reservations.First(value => value.Kind != SiteReservationKind.Start);
            var missing = ReplaceReservation(nonStart, Array.Empty<SiteEntryAnchor>());
            var missingSnapshot = ReplaceSnapshotReservation(nonStart, missing);
            var missingResult = new MandatoryTerminalBuilder().Build(missingSnapshot, biome);
            Assert.That(missingResult.Errors.Any(value => value.Code == MandatoryTerminalBuildErrorCode.EntryCountMismatch), Is.True);

            var start = site.StartReservation;
            var anchor = new SiteEntryAnchor(start.ReservationId, "ENTRY_L", start.Origin,
                PickWorldBoundSide(start.Origin), new[] { 1, 2, 3 }, true, true);
            var startWithEntry = ReplaceReservation(start, new[] { anchor });
            var startSnapshot = ReplaceSnapshotReservation(start, startWithEntry);
            var startResult = new MandatoryTerminalBuilder().Build(startSnapshot, biome);
            Assert.That(startResult.Errors.Any(value => value.Code == MandatoryTerminalBuildErrorCode.EntryCountMismatch), Is.True);
        }

        [Test]
        public void ExtraOptionalAndNoReturnEntriesAreRejected()
        {
            var source = site.Reservations.First(value => value.Kind != SiteReservationKind.Start);
            var original = source.EntryAnchors.Single();
            var extra = new SiteEntryAnchor(source.ReservationId, "ENTRY_EXTRA", original.FootprintSector,
                original.Side, new[] { 1, 2, 3 }, true, true);
            var extraSnapshot = ReplaceSnapshotReservation(source, ReplaceReservation(source, new[] { original, extra }));
            Assert.That(new MandatoryTerminalBuilder().Build(extraSnapshot, biome).Errors.Any(
                value => value.Code == MandatoryTerminalBuildErrorCode.EntryCountMismatch), Is.True);

            var optional = new SiteEntryAnchor(source.ReservationId, original.EntrySocketId, original.FootprintSector,
                original.Side, new[] { 1, 2, 3 }, false, true);
            var optionalResult = new MandatoryTerminalBuilder().Build(
                ReplaceSnapshotReservation(source, ReplaceReservation(source, new[] { optional })), biome);
            Assert.That(optionalResult.Errors.Any(value => value.Code == MandatoryTerminalBuildErrorCode.EntryIdentityMismatch), Is.True);

            var noReturn = new SiteEntryAnchor(source.ReservationId, original.EntrySocketId, original.FootprintSector,
                original.Side, new[] { 1, 2, 3 }, true, false);
            var returnResult = new MandatoryTerminalBuilder().Build(
                ReplaceSnapshotReservation(source, ReplaceReservation(source, new[] { noReturn })), biome);
            Assert.That(returnResult.Errors.Any(value => value.Code == MandatoryTerminalBuildErrorCode.EntryIdentityMismatch), Is.True);
        }

        [Test]
        public void UnexpectedReservationIdentityIsRejected()
        {
            var source = site.Reservations.First(value => value.Kind == SiteReservationKind.Boss);
            var unexpectedId = new SiteReservationId("RSV_01_SITE_UNEXPECTED");
            var entry = source.EntryAnchors.Single();
            var unexpectedEntry = new SiteEntryAnchor(unexpectedId, entry.EntrySocketId,
                entry.FootprintSector, entry.Side, entry.AllowedRouteTypes, true, true);
            var replacement = new SiteReservation(
                unexpectedId, source.Kind, "SITE_UNEXPECTED", source.Origin, source.Footprint,
                source.PrimaryBiomeId, source.ReservationOrder, new[] { unexpectedEntry });
            var result = new MandatoryTerminalBuilder().Build(ReplaceSnapshotReservation(source, replacement), biome);
            Assert.That(result.Errors.Any(value => value.Code == MandatoryTerminalBuildErrorCode.ReservationIdentityMismatch), Is.True);
        }

        [Test]
        public void SourceModelsRejectDuplicateReservationsAndWrongFootprintEntries()
        {
            var duplicates = site.Reservations.Concat(new[] { site.Reservations[1] });
            Assert.Throws<ArgumentException>(() =>
                new SiteReservationSnapshot(site.Seed, duplicates, site.Sectors, site.CoreBiomeSeeds));
            var source = site.Reservations.First(value => value.Kind != SiteReservationKind.Start);
            var wrong = new SiteEntryAnchor(source.ReservationId, "ENTRY_L",
                site.StartAnchor, PickWorldBoundSide(site.StartAnchor), new[] { 1, 2, 3 }, true, true);
            Assert.Throws<ArgumentException>(() => ReplaceReservation(source, new[] { wrong }));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("lower")]
        [TestCase("HAS-DASH")]
        [TestCase("HAS SPACE")]
        [TestCase("Ä")]
        public void TerminalIdRejectsNonCanonicalValues(string value)
        {
            Assert.That(MandatoryRouteTerminalId.TryCreate(value, out var parsed), Is.False);
            Assert.That(parsed.IsValid, Is.False);
            if (value == null) Assert.Throws<ArgumentNullException>(() => new MandatoryRouteTerminalId(value));
            else Assert.Throws<ArgumentException>(() => new MandatoryRouteTerminalId(value));
        }

        [Test]
        public void TerminalIdDefaultEqualityOrderAndHashAreDeterministic()
        {
            var first = new MandatoryRouteTerminalId("TERM_A");
            var same = new MandatoryRouteTerminalId("TERM_A");
            var next = new MandatoryRouteTerminalId("TERM_B");
            Assert.That(default(MandatoryRouteTerminalId).IsValid, Is.False);
            Assert.That(first, Is.EqualTo(same));
            Assert.That(first == same && first != next, Is.True);
            Assert.That(first.CompareTo(next), Is.LessThan(0));
            Assert.That(first.GetHashCode(), Is.EqualTo(same.GetHashCode()));
            Assert.That(first.ToString(), Is.EqualTo("TERM_A"));
        }

        [Test]
        public void TerminalAndKindRejectUndefinedOrMismatchedFields()
        {
            var start = Complete().TerminalSet.StartTerminal;
            Assert.That(Enum.GetNames(typeof(MandatoryRouteTerminalKind)), Is.EqualTo(new[] { "Start", "SiteEntry" }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MandatoryRouteTerminal(
                start.TerminalId, (MandatoryRouteTerminalKind)99, 0, start.ReservationId,
                start.ReservationKind, start.SourceDefinitionId, string.Empty,
                start.AnchorSector, start.ApproachSector, null, new[] { 1, 2, 3 }, true, true));
            Assert.Throws<ArgumentException>(() => new MandatoryRouteTerminal(
                start.TerminalId, MandatoryRouteTerminalKind.Start, 1, start.ReservationId,
                start.ReservationKind, start.SourceDefinitionId, string.Empty,
                start.AnchorSector, start.ApproachSector, null, new[] { 1, 2, 3 }, true, true));
        }

        [Test]
        public void TerminalCopiesRoutesAndAllPublishedCollectionsAreReadOnly()
        {
            var start = Complete().TerminalSet.StartTerminal;
            var routes = new List<int> { 3, 1, 2 };
            var copy = new MandatoryRouteTerminal(
                start.TerminalId, start.Kind, start.TerminalOrder, start.ReservationId,
                start.ReservationKind, start.SourceDefinitionId, start.EntrySocketId,
                start.AnchorSector, start.ApproachSector, start.EntrySide, routes, true, true);
            routes.Clear();
            Assert.That(copy.AllowedRouteTypes, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(copy.AllowedRouteTypes, Is.InstanceOf<ReadOnlyCollection<int>>());
            Assert.That(Complete().TerminalSet.Terminals, Is.InstanceOf<ReadOnlyCollection<MandatoryRouteTerminal>>());
            Assert.That(Complete().Errors, Is.InstanceOf<ReadOnlyCollection<MandatoryTerminalBuildError>>());
        }

        [Test]
        public void FreshReuseAndThreadsProduceIdenticalResults()
        {
            var tasks = Enumerable.Range(0, 8).Select(index => Task.Run(() =>
                Signature((index & 1) == 0
                    ? new MandatoryTerminalBuilder().Build(site, biome)
                    : reused.Build(site, biome)))).ToArray();
            Task.WaitAll(tasks);
            Assert.That(tasks.Select(value => value.Result), Is.All.EqualTo(expectedSignature));
        }

        [Test]
        public void BuilderDoesNotMutateEitherSource()
        {
            var before = SourceSignature();
            var result = new MandatoryTerminalBuilder().Build(site, biome);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(SourceSignature(), Is.EqualTo(before));
            Assert.That(result.Diagnostics.SourceMutationCount, Is.Zero);
            Assert.That(result.Diagnostics.RngDrawCount, Is.Zero);
        }

        [Test]
        public void ProductionSurfaceHasNoUnityEditorReflectionFactoryOrStaticMutableState()
        {
            var types = new[]
            {
                typeof(MandatoryRouteTerminalId), typeof(MandatoryRouteTerminalKind),
                typeof(MandatoryRouteTerminal), typeof(MandatoryRouteTerminalSet),
                typeof(MandatoryTerminalBuildError), typeof(MandatoryTerminalBuildDiagnostics),
                typeof(MandatoryTerminalBuildResult), typeof(MandatoryTerminalBuilder)
            };
            Assert.That(types.SelectMany(value => value.GetFields(
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Where(field => !field.IsLiteral && !field.IsInitOnly), Is.Empty);
            Assert.That(types.SelectMany(value => value.GetFields(
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Any(field => field.FieldType.FullName != null &&
                    (field.FieldType.FullName.StartsWith("UnityEditor.", StringComparison.Ordinal) ||
                     field.FieldType.FullName.StartsWith("System.Reflection.", StringComparison.Ordinal))), Is.False);
            var names = string.Join("|", types.Select(value => value.FullName));
            Assert.That(names, Does.Not.Contain("RouteMask"));
            Assert.That(names, Does.Not.Contain("Connector"));
            Assert.That(names, Does.Not.Contain("RouteGraph"));
        }

        private MandatoryTerminalBuildResult Complete()
        {
            var result = reused.Build(site, biome);
            Assert.That(result.Status, Is.EqualTo(MandatoryTerminalBuildStatus.Completed), ErrorSignature(result));
            return result;
        }

        private SiteReservationSnapshot ReplaceSnapshotReservation(
            SiteReservation source,
            SiteReservation replacement)
        {
            var reservations = site.Reservations.Select(value =>
                value.ReservationId == source.ReservationId ? replacement : value).ToArray();
            return new SiteReservationSnapshot(site.Seed, reservations, BuildSectorRows(reservations), site.CoreBiomeSeeds);
        }

        private static SiteReservation ReplaceReservation(
            SiteReservation source,
            IEnumerable<SiteEntryAnchor> entries) =>
            new SiteReservation(
                source.ReservationId, source.Kind, source.SourceDefinitionId, source.Origin,
                source.Footprint, source.PrimaryBiomeId, source.ReservationOrder, entries);

        private static IReadOnlyList<SectorReservation> BuildSectorRows(
            IEnumerable<SiteReservation> reservations)
        {
            var occupied = new Dictionary<SectorCoord, Tuple<SiteReservation, SiteFootprintCell>>();
            foreach (var reservation in reservations)
                foreach (var coordinate in reservation.OccupiedSectors)
                {
                    reservation.TryGetFootprintCell(coordinate, out var cell);
                    occupied.Add(coordinate, Tuple.Create(reservation, cell));
                }
            var rows = new List<SectorReservation>();
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var coordinate = WorldGridIndex.ToCoordinate(index);
                if (occupied.TryGetValue(coordinate, out var binding))
                    rows.Add(SectorReservation.CreateReserved(
                        index, coordinate, binding.Item1.ReservationId, binding.Item1.Kind,
                        binding.Item2.LocalX, binding.Item2.LocalY, binding.Item2.LocalRole));
                else rows.Add(SectorReservation.CreateUnreserved(index, coordinate));
            }
            return rows;
        }

        private static SiteReservationSnapshot CreateApprovedEntrySnapshot(
            SiteReservationSnapshot source)
        {
            var reservations = new List<SiteReservation>();
            foreach (var reservation in source.Reservations)
            {
                if (reservation.Kind == SiteReservationKind.Start)
                {
                    reservations.Add(reservation);
                    continue;
                }
                var found = false;
                SiteEntryAnchor entry = null;
                foreach (var footprintSector in reservation.OccupiedSectors)
                {
                    foreach (var side in new[]
                    {
                        SiteEntrySide.L, SiteEntrySide.R, SiteEntrySide.U, SiteEntrySide.D
                    })
                    {
                        SiteReservationTokenCodec.GetDelta(side, out var deltaX, out var deltaY);
                        var exterior = new SectorCoord(
                            footprintSector.X + deltaX, footprintSector.Y + deltaY);
                        if (exterior.X < 0 || exterior.X >= WorldGenConstants.SectorColumns ||
                            exterior.Y < 0 || exterior.Y >= WorldGenConstants.SectorRows ||
                            source.GetSector(exterior).IsReserved) continue;
                        entry = new SiteEntryAnchor(
                            reservation.ReservationId, "ENTRY_L", footprintSector, side,
                            new[] { 1, 2, 3 }, true, true);
                        found = true;
                        break;
                    }
                    if (found) break;
                }
                if (!found) throw new InvalidOperationException("No approved exterior sector was available.");
                reservations.Add(ReplaceReservation(reservation, new[] { entry }));
            }
            return new SiteReservationSnapshot(
                source.Seed, reservations, BuildSectorRows(reservations), source.CoreBiomeSeeds);
        }

        private static SiteEntrySide PickWorldBoundSide(SectorCoord coordinate)
        {
            if (coordinate.X > 0) return SiteEntrySide.L;
            if (coordinate.X < WorldGenConstants.SectorColumns - 1) return SiteEntrySide.R;
            if (coordinate.Y > 0) return SiteEntrySide.D;
            return SiteEntrySide.U;
        }

        private string SourceSignature() =>
            string.Join("|", site.Reservations.Select(value =>
                value.ReservationId.Value + ":" + value.ReservationOrder + ":" +
                string.Join(",", value.EntryAnchors.Select(entry =>
                    entry.EntrySocketId + "@" + entry.FootprintSector + "/" + entry.Side)))) + "|" +
            site.Sectors.Count(value => value.IsReserved) + "|" +
            biome.Snapshot.Patches.Count + "|" + biome.Diagnostics.RuleResults.Count;

        private static string Signature(MandatoryTerminalBuildResult result) =>
            result.Status + "|" + string.Join("|", result.TerminalSet.Terminals.Select(value =>
                value.TerminalId.Value + ":" + value.TerminalOrder + ":" + value.ReservationId.Value + ":" +
                value.AnchorSector + ":" + value.ApproachSector + ":" +
                (value.EntrySide.HasValue ? value.EntrySide.Value.ToString() : "NONE"))) + "|" +
            result.Diagnostics.TerminalCount + ":" + result.Diagnostics.RngDrawCount + ":" +
            result.Diagnostics.SourceMutationCount;

        private static string ErrorSignature(MandatoryTerminalBuildResult result) =>
            string.Join("|", result.Errors.Select(value =>
                value.Code + ":" + value.FirstId + ":" + value.SecondId + ":" + value.SectorIndex));

        private static T GetField<T>(object target, Type type, string name)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(type.FullName, name);
            return (T)field.GetValue(target);
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(
                name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(target.GetType().FullName, name);
            field.SetValue(target, value);
        }
    }
}
