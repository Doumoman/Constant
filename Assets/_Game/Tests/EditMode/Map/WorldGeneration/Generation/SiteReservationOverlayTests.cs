using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Diagnostics;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using UnityEngine;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    public sealed class SiteReservationOverlayTests
    {
        private static readonly OverlayFixture Canonical = CreateFixture(4660UL);

        private static IEnumerable<int> FirstHundredIndices => Enumerable.Range(0, 100);

        [TestCaseSource(nameof(FirstHundredIndices))]
        public void Snapshot_FirstHundredCellsPreserveExactGridIdentity(int index)
        {
            var cell = Canonical.Snapshot.GetCell(index);
            var coordinate = new SectorCoord(index % 13, index / 13);

            Assert.That(cell.Index, Is.EqualTo(index));
            Assert.That(cell.Coordinate, Is.EqualTo(coordinate));
            Assert.That(Canonical.Snapshot.GetCell(coordinate), Is.SameAs(cell));
            Assert.That(Canonical.Snapshot.TryGetCell(index, out var found), Is.True);
            Assert.That(found, Is.SameAs(cell));
        }

        [Test]
        public void Snapshot_RejectsEveryNullInput()
        {
            Assert.Throws<ArgumentNullException>(() => SiteReservationOverlaySnapshot.Create(
                null, Canonical.Search, Canonical.Capacity, Canonical.Village, Canonical.Validation));
            Assert.Throws<ArgumentNullException>(() => SiteReservationOverlaySnapshot.Create(
                Canonical.Publication, null, Canonical.Capacity, Canonical.Village, Canonical.Validation));
            Assert.Throws<ArgumentNullException>(() => SiteReservationOverlaySnapshot.Create(
                Canonical.Publication, Canonical.Search, null, Canonical.Village, Canonical.Validation));
            Assert.Throws<ArgumentNullException>(() => SiteReservationOverlaySnapshot.Create(
                Canonical.Publication, Canonical.Search, Canonical.Capacity, null, Canonical.Validation));
            Assert.Throws<ArgumentNullException>(() => SiteReservationOverlaySnapshot.Create(
                Canonical.Publication, Canonical.Search, Canonical.Capacity, Canonical.Village, null));
        }

        [Test]
        public void Snapshot_UsesExactStarterCountsAndImmutableCollections()
        {
            var snapshot = Canonical.Snapshot;

            Assert.That(snapshot.Seed, Is.EqualTo(4660UL));
            Assert.That(snapshot.Count, Is.EqualTo(169));
            Assert.That(snapshot.ReservationCount, Is.EqualTo(7));
            Assert.That(snapshot.ReservedSectorCount, Is.EqualTo(8));
            Assert.That(snapshot.EntryArrowCount, Is.EqualTo(6));
            Assert.That(snapshot.CoreWitnessCount, Is.EqualTo(4));
            Assert.That(snapshot.CoreWitnessSectorCount, Is.EqualTo(20));
            Assert.That(snapshot.PassedValidationRuleCount, Is.EqualTo(6));
            Assert.That(snapshot.DiagnosticRows, Has.Count.EqualTo(16));
            Assert.That(((ICollection<SiteReservationOverlayCell>)snapshot.Cells).IsReadOnly, Is.True);
            Assert.That(((ICollection<SiteReservationOverlayDiagnosticRow>)snapshot.DiagnosticRows).IsReadOnly, Is.True);
        }

        [Test]
        public void Snapshot_All169CellsPreserveAscendingIndexAndLogicalCoordinates()
        {
            for (var index = 0; index < 169; index++)
            {
                var cell = Canonical.Snapshot.GetCell(index);
                Assert.That(cell.Index, Is.EqualTo(index), "index " + index);
                Assert.That(cell.Coordinate, Is.EqualTo(new SectorCoord(index % 13, index / 13)),
                    "coordinate " + index);
            }
        }

        [Test]
        public void Snapshot_InvalidLookupsNeverClampOrWrap()
        {
            Assert.That(Canonical.Snapshot.TryGetCell(-1, out var below), Is.False);
            Assert.That(below, Is.Null);
            Assert.That(Canonical.Snapshot.TryGetCell(169, out var above), Is.False);
            Assert.That(above, Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() => Canonical.Snapshot.GetCell(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Canonical.Snapshot.GetCell(169));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Canonical.Snapshot.GetCell(new SectorCoord(13, 0)));
        }

        [Test]
        public void Snapshot_ProjectsExactEightReservedAnd161UnreservedCells()
        {
            var reserved = Canonical.Snapshot.Cells.Where(cell => cell.IsReserved).ToArray();
            var unreserved = Canonical.Snapshot.Cells.Where(cell => !cell.IsReserved).ToArray();

            Assert.That(reserved, Has.Length.EqualTo(8));
            Assert.That(unreserved, Has.Length.EqualTo(161));
            Assert.That(reserved.All(cell => cell.ReservationId.HasValue && cell.Kind.HasValue), Is.True);
            Assert.That(reserved.All(cell => cell.LocalX >= 0 && cell.LocalY >= 0), Is.True);
            Assert.That(reserved.All(cell => !string.IsNullOrEmpty(cell.LocalRole)), Is.True);
            Assert.That(unreserved.All(cell => !cell.ReservationId.HasValue && !cell.Kind.HasValue), Is.True);
            Assert.That(unreserved.All(cell => cell.LocalX == -1 && cell.LocalY == -1), Is.True);
            Assert.That(unreserved.All(cell => cell.SourceDefinitionId == string.Empty &&
                                              cell.LocalRole == string.Empty), Is.True);
        }

        [Test]
        public void Snapshot_ProjectsExactSevenSourcesAndGlyphs()
        {
            var identity = Canonical.Snapshot.Cells.Where(cell => cell.IsReserved)
                .GroupBy(cell => cell.SourceDefinitionId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().SiteGlyph,
                    StringComparer.Ordinal);

            Assert.That(identity, Is.EquivalentTo(new Dictionary<string, string>
            {
                { "WORLD_MOONPALACE_V1", "A" },
                { "SITE_MOON_BOSS_VAULT", "B" },
                { "SITE_MOON_SEAL_FORGE", "F" },
                { "SITE_CASSIA_SAP_HEART", "C" },
                { "SITE_DEEP_STAR_YEAST", "Y" },
                { "SITE_MOON_CORE_METEOR", "M" },
                { "SITE_PRIMARY_VILLAGE", "V" }
            }));
        }

        [TestCase("", 60, 60, 68, 220)]
        [TestCase("WORLD_MOONPALACE_V1", 40, 170, 240, 235)]
        [TestCase("SITE_MOON_BOSS_VAULT", 220, 70, 70, 235)]
        [TestCase("SITE_MOON_SEAL_FORGE", 240, 145, 45, 235)]
        [TestCase("SITE_CASSIA_SAP_HEART", 70, 185, 105, 235)]
        [TestCase("SITE_DEEP_STAR_YEAST", 235, 205, 70, 235)]
        [TestCase("SITE_MOON_CORE_METEOR", 155, 95, 220, 235)]
        [TestCase("SITE_PRIMARY_VILLAGE", 65, 125, 235, 235)]
        public void Gui_UsesExactFrozenSiteColors(
            string source,
            int red,
            int green,
            int blue,
            int alpha)
        {
            Assert.That(SiteReservationOverlayGui.GetSiteColor(source),
                Is.EqualTo(new Color32((byte)red, (byte)green, (byte)blue, (byte)alpha)));
        }

        [Test]
        public void Gui_RejectsNullAndUnknownSourceColors()
        {
            Assert.Throws<ArgumentNullException>(() => SiteReservationOverlayGui.GetSiteColor(null));
            Assert.Throws<ArgumentException>(() =>
                SiteReservationOverlayGui.GetSiteColor("SITE_UNKNOWN"));
        }

        [TestCase(SiteEntrySide.L, "L:<")]
        [TestCase(SiteEntrySide.R, "R:>")]
        [TestCase(SiteEntrySide.U, "U:^")]
        [TestCase(SiteEntrySide.D, "D:v")]
        public void Gui_UsesExactArrowTokens(SiteEntrySide side, string token)
        {
            Assert.That(SiteReservationOverlayGui.GetEntryArrowToken(side), Is.EqualTo(token));
        }

        [Test]
        public void Snapshot_ProjectsSixEntryArrowsOnOccupiedCells()
        {
            var arrowCells = Canonical.Snapshot.Cells.Where(cell => cell.EntrySides.Count > 0).ToArray();

            Assert.That(arrowCells.Sum(cell => cell.EntrySides.Count), Is.EqualTo(6));
            Assert.That(arrowCells.All(cell => cell.IsReserved), Is.True);
            Assert.That(arrowCells.SelectMany(cell => cell.EntrySides)
                .All(side => !string.IsNullOrEmpty(SiteReservationOverlayGui.GetEntryArrowToken(side))), Is.True);
        }

        [Test]
        public void Snapshot_ProjectsFourDisjointFiveSectorWitnesses()
        {
            var witnessCells = Canonical.Snapshot.Cells.Where(cell => cell.IsCoreWitness).ToArray();
            var ownerCounts = witnessCells.GroupBy(cell => cell.CoreWitnessOwnerId.Value)
                .Select(group => group.Count()).ToArray();

            Assert.That(witnessCells, Has.Length.EqualTo(20));
            Assert.That(ownerCounts, Is.EqualTo(new[] { 5, 5, 5, 5 }));
            Assert.That(witnessCells.Any(cell => cell.IsReserved && cell.CellLabel != "+"), Is.True);
            Assert.That(witnessCells.Any(cell => !cell.IsReserved && cell.CellLabel == "+"), Is.True);
        }

        [Test]
        public void Cell_LabelAndTooltipCarryNonColorIdentity()
        {
            var reserved = Canonical.Snapshot.Cells.First(cell => cell.IsReserved);
            var witnessOnly = Canonical.Snapshot.Cells.First(cell => cell.IsCoreWitness && !cell.IsReserved);
            var empty = Canonical.Snapshot.Cells.First(cell => !cell.IsReserved && !cell.IsCoreWitness);

            Assert.That(reserved.CellLabel, Does.Match("^[ABFCYMV]\\n[0-9]+,[0-9]+$"));
            Assert.That(reserved.Tooltip.Split('\n'), Has.Length.EqualTo(6));
            Assert.That(reserved.Tooltip, Does.Contain("Reservation: RSV_"));
            Assert.That(reserved.Tooltip, Does.Contain("Source/Kind: "));
            Assert.That(witnessOnly.CellLabel, Is.EqualTo("+"));
            Assert.That(witnessOnly.Tooltip, Does.Contain("Reservation: NONE"));
            Assert.That(witnessOnly.Tooltip, Does.Not.Contain("Core Witness: NONE"));
            Assert.That(empty.CellLabel, Is.Empty);
            Assert.That(empty.Tooltip, Does.Contain("Entry: NONE"));
            Assert.That(empty.Tooltip, Does.Contain("Core Witness: NONE"));
            Assert.That(reserved.Tooltip.EndsWith("\n", StringComparison.Ordinal), Is.False);
        }

        [Test]
        public void DiagnosticRows_UseExactFrozenOrderClassesAndKeys()
        {
            var rows = Canonical.Snapshot.DiagnosticRows;

            Assert.That(rows.Select(row => (int)row.Kind), Is.EqualTo(Enumerable.Range(0, 16)));
            Assert.That(rows.Take(12).All(row =>
                row.Class == SiteReservationOverlayDiagnosticClass.CandidateRejection), Is.True);
            Assert.That(rows.Skip(12).Take(2).All(row =>
                row.Class == SiteReservationOverlayDiagnosticClass.FinalGate), Is.True);
            Assert.That(rows.Skip(14).All(row =>
                row.Class == SiteReservationOverlayDiagnosticClass.SoftCost), Is.True);
            Assert.That(rows.Select(row => row.Key), Is.EqualTo(new[]
            {
                "SEARCH_FOOTPRINT_OVERLAP",
                "SEARCH_BLOCKS_EXISTING_ENTRY_APPROACH",
                "SEARCH_ENTRY_APPROACH_OCCUPIED",
                "SEARCH_DISTANCE_CONSTRAINT",
                "SEARCH_CORE_CLUSTER",
                "VILLAGE_ENTRY_OUTSIDE_WORLD",
                "VILLAGE_FOOTPRINT_OVERLAP",
                "VILLAGE_PROTECTED_CORE_WITNESS",
                "VILLAGE_BLOCKS_EXISTING_ENTRY_APPROACH",
                "VILLAGE_ENTRY_APPROACH_OCCUPIED",
                "VILLAGE_OTHER_SITE_DISTANCE",
                "VILLAGE_START_BUCKET_DISTANCE",
                "CAPACITY_SHORTFALL",
                "VALIDATION_VIOLATIONS",
                "SELECTED_ALTITUDE_SOFT_UNITS",
                "SELECTED_CAPACITY_FORECAST_SOFT_UNITS"
            }));
            Assert.That(rows.Skip(14).All(row =>
                row.Label.EndsWith("(SOFT COST, NOT REJECTION)", StringComparison.Ordinal)), Is.True);
        }

        [Test]
        public void DiagnosticRows_AggregateAllSixSearchGroups()
        {
            var expected = new long[5];
            for (var reason = 0; reason < expected.Length; reason++)
                foreach (var group in Canonical.Search.Groups)
                    expected[reason] += group.GetReasonCount((SiteReservationRejectionReason)reason);

            Assert.That(Canonical.Snapshot.DiagnosticRows.Take(5).Select(row => row.Value),
                Is.EqualTo(expected));
        }

        [Test]
        public void DiagnosticRows_KeepCandidateRejectionsSeparateFromFinalPass()
        {
            Assert.That(Canonical.Snapshot.DiagnosticRows.Take(5).Any(row => row.Value > 0), Is.True);
            Assert.That(Canonical.Snapshot.DiagnosticRows[12].Value, Is.Zero);
            Assert.That(Canonical.Snapshot.DiagnosticRows[13].Value, Is.Zero);
            Assert.That(Canonical.Snapshot.PassedValidationRuleCount, Is.EqualTo(6));
        }

        [Test]
        public void Gui_All169RectsUseFrozenOrientationAndDimensions()
        {
            for (var index = 0; index < 169; index++)
            {
                var x = index % 13;
                var y = index / 13;
                var expected = new Rect(24 + x * 44, 56 + (12 - y) * 44, 44, 44);
                Assert.That(SiteReservationOverlayGui.GetCellRect(new SectorCoord(x, y)),
                    Is.EqualTo(expected), "rect " + index);
            }
            Assert.That(SiteReservationOverlayGui.GetCellRect(new SectorCoord(0, 12)).position,
                Is.EqualTo(new Vector2(24, 56)));
            Assert.That(SiteReservationOverlayGui.GetCellRect(new SectorCoord(12, 0)).position,
                Is.EqualTo(new Vector2(552, 584)));
        }

        [Test]
        public void Gui_All169CellCentersHitExactLogicalCell()
        {
            for (var index = 0; index < 169; index++)
            {
                var expected = Canonical.Snapshot.GetCell(index);
                var center = SiteReservationOverlayGui.GetCellRect(expected.Coordinate).center;
                Assert.That(SiteReservationOverlayGui.TryHitTest(
                    Canonical.Snapshot, center, out var actual), Is.True, "hit " + index);
                Assert.That(actual, Is.SameAs(expected), "cell " + index);
            }
        }

        [Test]
        public void Gui_HitTestUsesInclusiveLeftTopExclusiveRightBottom()
        {
            Assert.That(SiteReservationOverlayGui.TryHitTest(
                Canonical.Snapshot, new Vector2(24, 56), out var topLeft), Is.True);
            Assert.That(topLeft.Coordinate, Is.EqualTo(new SectorCoord(0, 12)));
            Assert.That(SiteReservationOverlayGui.TryHitTest(
                Canonical.Snapshot, new Vector2(595.999f, 627.999f), out var bottomRight), Is.True);
            Assert.That(bottomRight.Coordinate, Is.EqualTo(new SectorCoord(12, 0)));
            Assert.That(SiteReservationOverlayGui.TryHitTest(
                Canonical.Snapshot, new Vector2(596, 56), out var right), Is.False);
            Assert.That(right, Is.Null);
            Assert.That(SiteReservationOverlayGui.TryHitTest(
                Canonical.Snapshot, new Vector2(24, 628), out var bottom), Is.False);
            Assert.That(bottom, Is.Null);
            Assert.That(SiteReservationOverlayGui.TryHitTest(
                Canonical.Snapshot, new Vector2(23.999f, 56), out _), Is.False);
            Assert.That(SiteReservationOverlayGui.TryHitTest(
                Canonical.Snapshot, new Vector2(24, 55.999f), out _), Is.False);
        }

        [Test]
        public void Gui_UsesExactFrozenPanelRegionsAndTexts()
        {
            Assert.That(SiteReservationOverlayGui.PanelRect, Is.EqualTo(new Rect(12, 12, 1000, 760)));
            Assert.That(SiteReservationOverlayGui.GridRect, Is.EqualTo(new Rect(24, 56, 572, 572)));
            Assert.That(SiteReservationOverlayGui.SidebarRect, Is.EqualTo(new Rect(608, 56, 392, 704)));
            Assert.That(SiteReservationOverlayGui.TooltipRect, Is.EqualTo(new Rect(24, 640, 572, 120)));
            Assert.That(SiteReservationOverlayGui.EmptyHoverText,
                Is.EqualTo("Hover a sector for reservation details."));
            Assert.That(SiteReservationOverlayGui.SmallViewportText,
                Is.EqualTo("Site reservation overlay requires 1024 x 784 pixels."));
            Assert.That(SiteReservationOverlayGui.CoreWitnessLegendText,
                Is.EqualTo("Core outline = minimum expected witness, not painted biome"));
        }

        [Test]
        public void Gui_IsStaticStatelessAndExposesOneSharedDrawMethod()
        {
            var type = typeof(SiteReservationOverlayGui);
            Assert.That(type.IsAbstract && type.IsSealed, Is.True);
            Assert.That(type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(field => !field.IsLiteral), Is.Empty);
            Assert.That(type.GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Count(method => method.Name == nameof(SiteReservationOverlayGui.Draw)), Is.EqualTo(1));
        }

        [Test]
        public void Component_HasExactAttributesAndSurface()
        {
            var type = typeof(SiteReservationOverlay);
            Assert.That(type.IsSealed, Is.True);
            Assert.That(type.GetCustomAttribute<ExecuteAlways>(), Is.Not.Null);
            Assert.That(type.GetCustomAttribute<DisallowMultipleComponent>(), Is.Not.Null);
            Assert.That(type.GetCustomAttribute<AddComponentMenu>().componentMenu,
                Is.EqualTo("WorldGen/Site Reservation Overlay"));
            Assert.That(type.GetProperty(nameof(SiteReservationOverlay.HasSnapshot)), Is.Not.Null);
            Assert.That(type.GetProperty(nameof(SiteReservationOverlay.Snapshot)), Is.Not.Null);
            Assert.That(type.GetMethod(nameof(SiteReservationOverlay.SetSnapshot)), Is.Not.Null);
            Assert.That(type.GetMethod(nameof(SiteReservationOverlay.ClearSnapshot)), Is.Not.Null);
            Assert.That(type.GetField("snapshot", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetCustomAttribute<NonSerializedAttribute>(), Is.Not.Null);
        }

        [Test]
        public void Component_SetFailureAndClearAreTransactional()
        {
            var gameObject = new GameObject("SiteReservationOverlayTests")
            {
                hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
            };
            try
            {
                var overlay = gameObject.AddComponent<SiteReservationOverlay>();
                Assert.That(overlay.HasSnapshot, Is.False);
                Assert.That(overlay.Snapshot, Is.Null);

                overlay.SetSnapshot(
                    Canonical.Publication,
                    Canonical.Search,
                    Canonical.Capacity,
                    Canonical.Village,
                    Canonical.Validation);
                var successful = overlay.Snapshot;
                Assert.That(successful, Is.Not.Null);

                Assert.Throws<ArgumentNullException>(() => overlay.SetSnapshot(
                    null,
                    Canonical.Search,
                    Canonical.Capacity,
                    Canonical.Village,
                    Canonical.Validation));
                Assert.That(overlay.Snapshot, Is.SameAs(successful));

                overlay.ClearSnapshot();
                Assert.That(overlay.HasSnapshot, Is.False);
                Assert.That(overlay.Snapshot, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ProductionOverlayTypesStayInRuntimeAssemblyWithoutMutableStatics()
        {
            var types = new[]
            {
                typeof(SiteReservationOverlayCell),
                typeof(SiteReservationOverlayDiagnosticRow),
                typeof(SiteReservationOverlaySnapshot),
                typeof(SiteReservationOverlayGui),
                typeof(SiteReservationOverlay)
            };
            Assert.That(types.Select(type => type.Assembly.GetName().Name).Distinct(),
                Is.EqualTo(new[] { "Game.Map.Runtime" }));
            foreach (var type in types)
            {
                Assert.That(type.FullName, Does.Not.Contain("UnityEditor"));
                Assert.That(type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(field => !field.IsLiteral && !field.IsInitOnly), Is.Empty, type.FullName);
            }
        }

        private static OverlayFixture CreateFixture(ulong seed)
        {
            var villageTestType = typeof(VillageReservationSelectorTests);
            var villageStarter = villageTestType.GetField(
                    "Starter", BindingFlags.Static | BindingFlags.NonPublic)
                .GetValue(null);
            var villageStarterType = villageStarter.GetType();
            var biomes = (IReadOnlyDictionary<string, BiomeTypeDefinition>)villageStarterType
                .GetProperty("Biomes").GetValue(villageStarter);
            var coreRules = (IReadOnlyDictionary<string, BiomePatchRuleDefinition>)villageStarterType
                .GetProperty("CoreRules").GetValue(villageStarter);
            var policy = (SiteDistancePolicy)villageStarterType
                .GetProperty("Policy").GetValue(villageStarter);
            var specialDefinitions = (SpecialVillageDefinitionSet)villageStarterType
                .GetProperty("SpecialDefinitions").GetValue(villageStarter);
            var maps = specialDefinitions.SpecialMaps.Values.ToArray();
            var cells = specialDefinitions.SpecialMapFootprintCells.ToList();
            var entries = specialDefinitions.SpecialMapEntrySockets.ToArray();
            var validatorStarter = typeof(SiteReservationValidatorTests).GetField(
                    "Starter", BindingFlags.Static | BindingFlags.NonPublic)
                .GetValue(null);
            var validatorCells = (IReadOnlyList<SpecialMapFootprintCellDefinition>)validatorStarter
                .GetType().GetProperty("Cells").GetValue(validatorStarter);
            cells.AddRange(validatorCells.Where(cell =>
                string.Equals(cell.SpecialMapId, "SITE_PRIMARY_VILLAGE", StringComparison.Ordinal)));
            var groups = (IReadOnlyList<SiteReservationSearchGroup>)villageStarterType
                .GetProperty("FullGroups").GetValue(villageStarter);
            var rng = (DeterministicRngStream)villageTestType.GetMethod(
                    "WorldSiteStream", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { seed });
            var searchResult = new SiteReservationBacktracker().Search(
                groups,
                policy,
                SiteCandidateCostWeights.Default,
                SiteReservationSearchLimits.Default,
                rng);
            if (!searchResult.Succeeded)
                throw new InvalidOperationException("Canonical overlay search fixture failed.");

            var requirements = new List<CoreCapacityRequirement>();
            for (var index = 2; index <= 5; index++)
            {
                var placement = searchResult.SelectionPlan.SelectedPlacements[index];
                var key = SitePlacementKey.FromPlacement(placement);
                var specialMap = specialDefinitions.SpecialMaps[key.SourceDefinitionId];
                var biome = biomes[specialMap.PrimaryBiomeId];
                requirements.Add(new CoreCapacityRequirement(
                    key,
                    placement,
                    specialMap,
                    biome,
                    coreRules[biome.BiomeId]));
            }
            var capacityResult = new CoreCapacityFloodChecker().Check(
                searchResult.SelectionPlan,
                requirements);
            if (!capacityResult.Succeeded)
                throw new InvalidOperationException("Canonical overlay capacity fixture failed.");

            var profile = (VillageProfileDefinition)villageStarterType
                .GetProperty("Profile").GetValue(villageStarter);
            var villageMap = (SpecialMapDefinition)villageStarterType
                .GetProperty("Village").GetValue(villageStarter);
            var villageEntries = (IReadOnlyList<SpecialMapEntrySocketDefinition>)villageStarterType
                .GetProperty("EntrySockets").GetValue(villageStarter);
            var layouts = (IReadOnlyList<VillageLayoutDefinition>)villageStarterType
                .GetProperty("Layouts").GetValue(villageStarter);
            var villageResult = new VillageReservationSelector().Reserve(
                capacityResult.Approval,
                profile,
                villageMap,
                villageEntries,
                layouts,
                rng);
            if (!villageResult.Succeeded)
                throw new InvalidOperationException(
                    "Canonical overlay Village fixture failed: " +
                    string.Join(" | ", villageResult.Errors.Select(error =>
                            error.Code + ":" + error.Message)
                        .Concat(villageResult.Rejections.Select(rejection =>
                            rejection.Reason + ":" + rejection.Message))));

            var validationResult = new SiteReservationValidator().ValidateAndPublish(
                seed,
                villageResult.Approval,
                maps,
                cells,
                entries);
            if (!validationResult.Succeeded)
                throw new InvalidOperationException(
                    "Canonical overlay validation fixture failed: " +
                    string.Join(" | ", validationResult.Errors.Select(error =>
                        error.Code + ":" + error.Message)) + " | " +
                    string.Join(" | ", validationResult.Violations.Select(violation =>
                        violation.Code + ":" + violation.Message)));

            var publication = validationResult.Publication;
            var snapshot = SiteReservationOverlaySnapshot.Create(
                publication,
                searchResult.Diagnostics,
                capacityResult.Diagnostics,
                villageResult.Diagnostics,
                validationResult.Diagnostics);
            return new OverlayFixture(
                publication,
                searchResult.Diagnostics,
                capacityResult.Diagnostics,
                villageResult.Diagnostics,
                validationResult.Diagnostics,
                snapshot);
        }

        private sealed class OverlayFixture
        {
            public OverlayFixture(
                SiteReservationPublication publication,
                SiteReservationSearchDiagnostics search,
                CoreCapacityFloodDiagnostics capacity,
                VillageReservationDiagnostics village,
                SiteReservationValidationDiagnostics validation,
                SiteReservationOverlaySnapshot snapshot)
            {
                Publication = publication;
                Search = search;
                Capacity = capacity;
                Village = village;
                Validation = validation;
                Snapshot = snapshot;
            }

            public SiteReservationPublication Publication { get; }
            public SiteReservationSearchDiagnostics Search { get; }
            public CoreCapacityFloodDiagnostics Capacity { get; }
            public VillageReservationDiagnostics Village { get; }
            public SiteReservationValidationDiagnostics Validation { get; }
            public SiteReservationOverlaySnapshot Snapshot { get; }
        }
    }
}
