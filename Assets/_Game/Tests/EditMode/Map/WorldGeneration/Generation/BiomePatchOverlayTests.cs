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
using StarNight.Map.WorldGeneration.Diagnostics;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using UnityEngine;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    public sealed class BiomePatchOverlayTests
    {
        private const ulong ViableWorldSeed = 0x0123456789ABCDF9UL;
        private BiomePatchValidationPublication publication;
        private BiomePatchOverlaySnapshot snapshot;
        private string sourceSignature;
        private string overlaySignature;

        public static IEnumerable<TestCaseData> ProjectionCases
        {
            get
            {
                for (var index = 0; index < 100; index++)
                    yield return new TestCaseData(index).SetName(
                        "Projection_ExactImmutableCell_" + index.ToString("D3", CultureInfo.InvariantCulture));
            }
        }

        public static IEnumerable<TestCaseData> RepeatedCases
        {
            get
            {
                for (var index = 0; index < 12; index++)
                    yield return new TestCaseData(index).SetName(
                        "Create_RepeatedCultureFreeIdentity_" + index.ToString("D2", CultureInfo.InvariantCulture));
            }
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            publication = BuildPublication();
            sourceSignature = SourceSignature(publication);
            snapshot = BiomePatchOverlaySnapshot.Create(publication);
            overlaySignature = OverlaySignature(snapshot);
        }

        [TestCaseSource(nameof(ProjectionCases))]
        public void Projection_ExactImmutableCell(int index)
        {
            var cell = snapshot.GetCell(index);
            var ownership = publication.Snapshot.GetSector(index);
            Assert.That(cell.Index, Is.EqualTo(index));
            Assert.That(cell.Coordinate, Is.EqualTo(WorldGridIndex.ToCoordinate(index)));
            Assert.That(cell.IsAssigned, Is.EqualTo(ownership.IsAssigned));
            Assert.That(cell.PrimaryBiomeId, Is.EqualTo(ownership.PrimaryBiomeId));
            Assert.That(cell.PatchId, Is.EqualTo(ownership.PatchId));
            Assert.That(cell.CellLabel.StartsWith(
                cell.Coordinate.X.ToString(CultureInfo.InvariantCulture) + "," +
                cell.Coordinate.Y.ToString(CultureInfo.InvariantCulture) + "\n",
                StringComparison.Ordinal), Is.True);
            Assert.That(cell.Tooltip.EndsWith("\n", StringComparison.Ordinal), Is.False);
        }

        [Test]
        public void Snapshot_ExactViableCountersAndOrder()
        {
            Assert.That(snapshot.WorldSeed, Is.EqualTo(ViableWorldSeed));
            Assert.That(snapshot.Cells.Count, Is.EqualTo(169));
            Assert.That(snapshot.Patches.Count, Is.EqualTo(17));
            Assert.That(snapshot.AssignedCount, Is.EqualTo(165));
            Assert.That(snapshot.UnassignedCount, Is.EqualTo(4));
            Assert.That(snapshot.CoreCount, Is.EqualTo(4));
            Assert.That(snapshot.SatelliteCount, Is.EqualTo(10));
            Assert.That(snapshot.IntrusionCount, Is.EqualTo(3));
            Assert.That(snapshot.PassedValidationRuleCount, Is.EqualTo(15));
            Assert.That(snapshot.Patches.Select(value => value.PatchId.Value), Is.Ordered);
        }

        [Test]
        public void Snapshot_PatchRowsMatchIndependentSizePerimeterCompactnessAndMarkers()
        {
            var patchLookup = publication.Snapshot.Patches.ToDictionary(value => value.Id);
            foreach (var row in snapshot.Patches)
            {
                var patch = patchLookup[row.PatchId];
                var perimeter = IndependentPerimeter(patch);
                Assert.That(row.BiomeId, Is.EqualTo(patch.BiomeId));
                Assert.That(row.Role, Is.EqualTo(patch.Role));
                Assert.That(row.Size, Is.EqualTo(patch.SectorCount));
                Assert.That(row.Perimeter, Is.EqualTo(perimeter));
                Assert.That(row.CompactnessPermille,
                    Is.EqualTo(checked(16000 * patch.SectorCount / (perimeter * perimeter))));
                Assert.That(row.SeedCount, Is.EqualTo(patch.Seeds.Count));
                Assert.That(row.CoreSiteCellCount,
                    Is.EqualTo(publication.Snapshot.SiteBindings
                        .Where(value => value.PatchId == patch.Id)
                        .Sum(value => value.OccupiedSectorIndices.Count)));
            }
        }

        [Test]
        public void Snapshot_CellBoundariesMatchIndependentPatchIdComparison()
        {
            for (var index = 0; index < 169; index++)
            {
                var cell = snapshot.GetCell(index);
                Assert.That(cell.BorderLeft,
                    Is.EqualTo(IndependentBorder(index, WorldGridIndex.GetLeftIndex(index))));
                Assert.That(cell.BorderRight,
                    Is.EqualTo(IndependentBorder(index, WorldGridIndex.GetRightIndex(index))));
                Assert.That(cell.BorderUp,
                    Is.EqualTo(IndependentBorder(index, WorldGridIndex.GetUpIndex(index))));
                Assert.That(cell.BorderDown,
                    Is.EqualTo(IndependentBorder(index, WorldGridIndex.GetDownIndex(index))));
            }
        }

        [Test]
        public void Snapshot_SeedAndCoreSiteMarkersAreExactAndCoreSiteWins()
        {
            var seedIndices = new HashSet<int>(publication.Snapshot.Patches
                .SelectMany(value => value.Seeds).Select(value => value.SectorIndex));
            var coreSiteIndices = new HashSet<int>(publication.Snapshot.SiteBindings
                .SelectMany(value => value.OccupiedSectorIndices));
            foreach (var cell in snapshot.Cells)
            {
                Assert.That(cell.IsSeed, Is.EqualTo(seedIndices.Contains(cell.Index)));
                Assert.That(cell.IsCoreSiteCell, Is.EqualTo(coreSiteIndices.Contains(cell.Index)));
                if (cell.IsCoreSiteCell) Assert.That(cell.CellLabel, Does.EndWith("*"));
                else if (cell.IsSeed) Assert.That(cell.CellLabel, Does.EndWith("+"));
            }
        }

        [Test]
        public void Tooltip_HasExactSevenLinesAndNeutralUnassignedTokens()
        {
            foreach (var cell in snapshot.Cells)
                Assert.That(cell.Tooltip.Split('\n').Length, Is.EqualTo(7), cell.Index.ToString());
            var unassigned = snapshot.Cells.First(value => !value.IsAssigned);
            Assert.That(unassigned.CellLabel, Does.EndWith("--"));
            Assert.That(unassigned.Tooltip, Does.Contain("Biome: NONE"));
            Assert.That(unassigned.Tooltip, Does.Contain("PatchId: NONE"));
            Assert.That(unassigned.Tooltip, Does.Contain("Role: NONE"));
        }

        [TestCase("BIO_MOON_CRATER", 90, 145, 220, 235)]
        [TestCase("BIO_CASSIA_ROOT", 90, 180, 105, 235)]
        [TestCase("BIO_ABANDONED_MILL", 205, 135, 75, 235)]
        [TestCase("BIO_MOON_DOUGH", 190, 115, 205, 235)]
        public void Gui_FrozenBiomeColorsAreExact(string biomeId, int r, int g, int b, int a)
        {
            Assert.That(BiomePatchOverlayGui.GetBiomeColor(biomeId),
                Is.EqualTo(new Color32((byte)r, (byte)g, (byte)b, (byte)a)));
        }

        [Test]
        public void Gui_FrozenNeutralBoundaryAndMarkerColorsAreExact()
        {
            Assert.That(BiomePatchOverlayGui.UnassignedColor, Is.EqualTo(new Color32(60, 60, 68, 220)));
            Assert.That(BiomePatchOverlayGui.PatchBoundaryColor, Is.EqualTo(new Color32(20, 20, 24, 255)));
            Assert.That(BiomePatchOverlayGui.CoreSiteMarkerColor, Is.EqualTo(new Color32(255, 230, 80, 255)));
            Assert.That(BiomePatchOverlayGui.SeedMarkerColor, Is.EqualTo(new Color32(245, 245, 245, 255)));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("BIO_UNKNOWN")]
        public void Gui_RejectsUnknownBiomeInsteadOfFallback(string value)
        {
            Assert.That(() => BiomePatchOverlayGui.GetBiomeColor(value),
                Throws.InstanceOf<ArgumentException>());
        }

        [TestCase(BiomePatchRole.Core, "C")]
        [TestCase(BiomePatchRole.Satellite, "S")]
        [TestCase(BiomePatchRole.Intrusion, "I")]
        public void Gui_RoleGlyphsAreExact(BiomePatchRole role, string glyph)
        {
            Assert.That(BiomePatchOverlayGui.GetRoleGlyph(role), Is.EqualTo(glyph));
        }

        [Test]
        public void Gui_RejectsUndefinedRole()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                BiomePatchOverlayGui.GetRoleGlyph((BiomePatchRole)3));
        }

        [TestCase(1, "1/1000")]
        [TestCase(500, "500/1000")]
        [TestCase(1000, "1000/1000")]
        public void Gui_CompactnessFormattingIsInvariant(int value, string expected)
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                Assert.That(BiomePatchOverlayGui.FormatCompactness(value), Is.EqualTo(expected));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [TestCase(0)]
        [TestCase(1001)]
        public void Gui_RejectsInvalidCompactness(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                BiomePatchOverlayGui.FormatCompactness(value));
        }

        [TestCase(0, 24f, 584f)]
        [TestCase(12, 552f, 584f)]
        [TestCase(156, 24f, 56f)]
        [TestCase(168, 552f, 56f)]
        public void Gui_CellRectUsesExactUnflippedDataAndFlippedVisualRow(
            int index, float expectedX, float expectedY)
        {
            var rect = BiomePatchOverlayGui.GetCellRect(index);
            Assert.That(rect, Is.EqualTo(new Rect(expectedX, expectedY, 44f, 44f)));
        }

        [Test]
        public void Gui_HitTestIsInclusiveLeftTopExclusiveRightBottom()
        {
            Assert.That(BiomePatchOverlayGui.TryHitTest(snapshot, new Vector2(24f, 56f), out var topLeft), Is.True);
            Assert.That(topLeft.Coordinate, Is.EqualTo(new SectorCoord(0, 12)));
            Assert.That(BiomePatchOverlayGui.TryHitTest(snapshot, new Vector2(595.999f, 627.999f), out var bottomRight), Is.True);
            Assert.That(bottomRight.Coordinate, Is.EqualTo(new SectorCoord(12, 0)));
            Assert.That(BiomePatchOverlayGui.TryHitTest(snapshot, new Vector2(596f, 628f), out var outside), Is.False);
            Assert.That(outside, Is.Null);
            Assert.That(BiomePatchOverlayGui.TryHitTest(snapshot, new Vector2(23.999f, 56f), out _), Is.False);
            Assert.That(BiomePatchOverlayGui.TryHitTest(snapshot, new Vector2(24f, 55.999f), out _), Is.False);
        }

        [Test]
        public void Gui_LayoutAndSmallViewportContractAreExact()
        {
            Assert.That(BiomePatchOverlayGui.PanelRect, Is.EqualTo(new Rect(12, 12, 1200, 820)));
            Assert.That(BiomePatchOverlayGui.GridRect, Is.EqualTo(new Rect(24, 56, 572, 572)));
            Assert.That(BiomePatchOverlayGui.SidebarRect, Is.EqualTo(new Rect(612, 56, 564, 740)));
            Assert.That(BiomePatchOverlayGui.TooltipRect, Is.EqualTo(new Rect(24, 646, 572, 150)));
            Assert.That(BiomePatchOverlayGui.RequiredViewportWidth, Is.EqualTo(1224));
            Assert.That(BiomePatchOverlayGui.RequiredViewportHeight, Is.EqualTo(844));
            Assert.That(BiomePatchOverlayGui.SmallViewportText,
                Is.EqualTo("Biome patch overlay requires 1224 x 844 pixels."));
        }

        [Test]
        public void Snapshot_LookupsAreExactAndRejectMisses()
        {
            for (var index = 0; index < 169; index++)
            {
                Assert.That(snapshot.TryGetCell(index, out var cell), Is.True);
                Assert.That(cell, Is.SameAs(snapshot.GetCell(index)));
                Assert.That(snapshot.GetCell(cell.Coordinate), Is.SameAs(cell));
            }
            Assert.That(snapshot.TryGetCell(-1, out var before), Is.False);
            Assert.That(before, Is.Null);
            Assert.That(snapshot.TryGetCell(169, out var after), Is.False);
            Assert.That(after, Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() => snapshot.GetCell(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => snapshot.GetCell(169));
            Assert.Throws<ArgumentOutOfRangeException>(() => snapshot.GetCell(new SectorCoord(-1, 0)));
        }

        [Test]
        public void Snapshot_CollectionsAreDefensiveReadOnlyCopies()
        {
            AssertReadOnly(snapshot.Cells);
            AssertReadOnly(snapshot.Patches);
            Assert.That(snapshot.Cells, Is.Not.SameAs(publication.Snapshot.Sectors));
            Assert.That(snapshot.Patches, Is.Not.SameAs(publication.Snapshot.Patches));
        }

        [TestCaseSource(nameof(RepeatedCases))]
        public void Create_RepeatedCultureFreeIdentity(int caseId)
        {
            var previous = CultureInfo.CurrentCulture;
            var previousUi = CultureInfo.CurrentUICulture;
            try
            {
                var culture = (caseId & 1) == 0 ? "en-US" : (caseId & 2) == 0 ? "tr-TR" : "ko-KR";
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
                var actual = BiomePatchOverlaySnapshot.Create(publication);
                Assert.That(OverlaySignature(actual), Is.EqualTo(overlaySignature));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
                CultureInfo.CurrentUICulture = previousUi;
            }
        }

        [Test]
        public void Create_ParallelFreshCallsAreIdentical()
        {
            var jobs = Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
                BiomePatchOverlaySnapshot.Create(publication))).ToArray();
            Task.WaitAll(jobs);
            foreach (var job in jobs)
                Assert.That(OverlaySignature(job.Result), Is.EqualTo(overlaySignature));
        }

        [Test]
        public void Create_ShuffledPublicationRowsRemainCanonical()
        {
            var shuffled = Clone(publication);
            SetField(
                shuffled,
                "patchRows",
                new ReadOnlyCollection<GeneratedBiomePatchRow>(publication.PatchRows.Reverse().ToList()));
            var actual = BiomePatchOverlaySnapshot.Create(shuffled);
            Assert.That(OverlaySignature(actual), Is.EqualTo(overlaySignature));
        }

        [Test]
        public void Create_NullAndMismatchedDiagnosticsAreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => BiomePatchOverlaySnapshot.Create(null));
            var badDiagnostics = Clone(publication.Diagnostics);
            SetField(badDiagnostics, "<PatchCount>k__BackingField", 16);
            var badPublication = Clone(publication);
            SetField(badPublication, "<Diagnostics>k__BackingField", badDiagnostics);
            Assert.Throws<ArgumentException>(() => BiomePatchOverlaySnapshot.Create(badPublication));
        }

        [Test]
        public void Create_DoesNotMutateSourceOrConsumeRngOrWriteFiles()
        {
            var first = BiomePatchOverlaySnapshot.Create(publication);
            var second = BiomePatchOverlaySnapshot.Create(publication);
            Assert.That(SourceSignature(publication), Is.EqualTo(sourceSignature));
            Assert.That(first.WorldSeed, Is.EqualTo(second.WorldSeed));
            Assert.That(publication.Diagnostics.RngDrawCount, Is.Zero);
            Assert.That(publication.Diagnostics.SourceMutationCount, Is.Zero);
        }

        [Test]
        public void Component_AttributesAndTransactionalReplacementAreExact()
        {
            Assert.That(typeof(BiomePatchOverlay).GetCustomAttributes(typeof(ExecuteAlways), false), Is.Not.Empty);
            Assert.That(typeof(BiomePatchOverlay).GetCustomAttributes(typeof(DisallowMultipleComponent), false), Is.Not.Empty);
            var menu = (AddComponentMenu)typeof(BiomePatchOverlay)
                .GetCustomAttributes(typeof(AddComponentMenu), false).Single();
            Assert.That(menu.componentMenu, Is.EqualTo("WorldGen/Biome Patch Overlay"));

            var gameObject = new GameObject("MAP04_10_TEST_OVERLAY");
            try
            {
                var component = gameObject.AddComponent<BiomePatchOverlay>();
                Assert.That(component.HasSnapshot, Is.False);
                Assert.That(component.Snapshot, Is.Null);
                component.SetSnapshot(publication);
                var approved = component.Snapshot;
                Assert.That(component.HasSnapshot, Is.True);
                Assert.Throws<ArgumentNullException>(() => component.SetSnapshot(null));
                Assert.That(component.Snapshot, Is.SameAs(approved));
                component.ClearSnapshot();
                Assert.That(component.HasSnapshot, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Component_HasNoAwakeUpdatePollingOrAutomaticGenerationSurface()
        {
            var declared = typeof(BiomePatchOverlay).GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly).Select(value => value.Name).ToArray();
            Assert.That(declared, Does.Contain("SetSnapshot"));
            Assert.That(declared, Does.Contain("ClearSnapshot"));
            Assert.That(declared, Does.Contain("OnGUI"));
            Assert.That(declared, Does.Not.Contain("Awake"));
            Assert.That(declared, Does.Not.Contain("Start"));
            Assert.That(declared, Does.Not.Contain("Update"));
            Assert.That(declared.Any(value => value.Contains("Generate") || value.Contains("Validate") ||
                value.Contains("Repair") || value.Contains("Retry") || value.Contains("Save")), Is.False);
            var field = typeof(BiomePatchOverlay).GetField(
                "snapshot", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field.GetCustomAttributes(typeof(NonSerializedAttribute), false), Is.Not.Empty);
        }

        [Test]
        public void Gui_DrawAndComponentOnGuiBothContainProtectedFinallyPaths()
        {
            var draw = typeof(BiomePatchOverlayGui).GetMethod("Draw", BindingFlags.Public | BindingFlags.Static);
            var onGui = typeof(BiomePatchOverlay).GetMethod("OnGUI", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(draw.GetMethodBody().ExceptionHandlingClauses.Any(value =>
                value.Flags == ExceptionHandlingClauseOptions.Finally), Is.True);
            Assert.That(onGui, Is.Not.Null);
        }

        [Test]
        public void RuntimeOverlaySurfaceHasNoForbiddenOrMutableDependency()
        {
            var types = new[]
            {
                typeof(BiomePatchOverlayPatchRow), typeof(BiomePatchOverlayCell),
                typeof(BiomePatchOverlaySnapshot), typeof(BiomePatchOverlayGui),
                typeof(BiomePatchOverlay)
            };
            foreach (var type in types)
            {
                if (type.IsClass && !(type.IsAbstract && type.IsSealed))
                    Assert.That(type.IsSealed, Is.True, type.FullName);
                Assert.That(type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(field => !field.IsLiteral && !field.IsInitOnly), Is.Empty, type.FullName);
                Assert.That(type.GetProperties(BindingFlags.Public | BindingFlags.Instance |
                    BindingFlags.DeclaredOnly)
                    .All(property => property.SetMethod == null), Is.True, type.FullName);
                var surface = type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Select(value => value.ToString()).ToArray();
                Assert.That(surface.Any(value => value.Contains("UnityEditor") || value.Contains("System.IO") ||
                    value.Contains("System.Random") || value.Contains("UnityEngine.Random") ||
                    value.Contains("DateTime") || value.Contains("System.Reflection")), Is.False, type.FullName);
            }
        }

        [TestCase(15)]
        [TestCase(16)]
        [TestCase(17)]
        [TestCase(18)]
        [TestCase(19)]
        public void Create_VariableApprovedInventoryProjectsActualCounts(int expectedPatchCount)
        {
            var variablePublication = FindVariablePublication(expectedPatchCount);
            var actual = BiomePatchOverlaySnapshot.Create(variablePublication);
            Assert.That(actual.Patches.Count, Is.EqualTo(expectedPatchCount));
            Assert.That(actual.CoreCount + actual.SatelliteCount + actual.IntrusionCount,
                Is.EqualTo(expectedPatchCount));
            Assert.That(actual.CoreCount, Is.EqualTo(variablePublication.Diagnostics.CorePatchCount));
            Assert.That(actual.SatelliteCount, Is.EqualTo(variablePublication.Diagnostics.SatellitePatchCount));
            Assert.That(actual.IntrusionCount, Is.EqualTo(variablePublication.Diagnostics.IntrusionPatchCount));
            Assert.That(actual.AssignedCount, Is.EqualTo(165));
            Assert.That(actual.UnassignedCount, Is.EqualTo(4));
            Assert.That(actual.PassedValidationRuleCount, Is.EqualTo(15));
            Assert.That(OverlaySignature(BiomePatchOverlaySnapshot.Create(variablePublication)),
                Is.EqualTo(OverlaySignature(actual)));
        }

        private static readonly Dictionary<int, BiomePatchValidationPublication> VariablePublications =
            new Dictionary<int, BiomePatchValidationPublication>();

        private static BiomePatchValidationPublication FindVariablePublication(int patchCount)
        {
            lock (VariablePublications)
            {
                if (VariablePublications.TryGetValue(patchCount, out var cached)) return cached;
                var exitType = typeof(Map04ExitTests);
                var exit = new Map04ExitTests();
                exit.OneTimeSetUp();
                var servicesType = exitType.GetNestedType("PipelineServices", BindingFlags.NonPublic);
                var runWorld = exitType.GetMethod("RunWorld", BindingFlags.Instance | BindingFlags.NonPublic);
                for (ulong seed = 0; seed < 1000UL && VariablePublications.Count < 5; seed++)
                {
                    var services = Activator.CreateInstance(servicesType, true);
                    var worldResult = runWorld.Invoke(exit, new[] { (object)seed, services, false });
                    var final = Get(worldResult, "Final");
                    if (!(bool)Get(final, "Completed")) continue;
                    var count = (int)Get(final, "PatchCount");
                    if (count < 15 || count > 19 || VariablePublications.ContainsKey(count)) continue;
                    var validation = (BiomePatchValidationResult)Get(final, "Validation");
                    VariablePublications.Add(count, validation.Publication);
                }
                Assert.That(VariablePublications.ContainsKey(patchCount), Is.True,
                    "No approved variable publication was found for patch count " + patchCount);
                return VariablePublications[patchCount];
            }
        }

        private static BiomePatchValidationPublication BuildPublication()
        {
            var cleanup = (PatchCleanupResult)InvokePrivateStatic(
                typeof(BiomePatchExporterTests), "BuildCleanupResult", Array.Empty<object>());
            var world = (GeneratedWorldData)InvokePrivateStatic(
                typeof(BiomePatchExporterTests), "CreateSourceWorld",
                new object[] { ViableWorldSeed, false, false });
            var export = new BiomePatchExporter().Export(cleanup, world);
            Assert.That(export.Status, Is.EqualTo(BiomePatchExportStatus.Completed));

            var fixture = InvokePrivateStatic(
                typeof(IntrusionPlacerTests), "BuildFixture", new object[] { ViableWorldSeed, 24 });
            var definitions = Get(fixture, "Definitions");
            var biomes = (IEnumerable<BiomeTypeDefinition>)Get(definitions, "Biomes");
            var rules = (IEnumerable<BiomePatchRuleDefinition>)Get(definitions, "AllRules");
            var profiles = (IEnumerable<BiomeBoundaryProfileDefinition>)Get(definitions, "Profiles");
            var pairs = (IEnumerable<BiomeBoundaryPairRuleDefinition>)Get(definitions, "Pairs");
            var validation = new BiomePatchValidator().Validate(export, biomes, rules, profiles, pairs);
            Assert.That(validation.Status, Is.EqualTo(BiomePatchValidationStatus.Completed));
            return validation.Publication;
        }

        private bool IndependentBorder(int index, int neighborIndex)
        {
            if (neighborIndex < 0) return true;
            var left = publication.Snapshot.GetSector(index).PatchId;
            var right = publication.Snapshot.GetSector(neighborIndex).PatchId;
            return left.HasValue != right.HasValue ||
                   (left.HasValue && left.Value != right.Value);
        }

        private static int IndependentPerimeter(BiomePatch patch)
        {
            var sectors = new HashSet<int>(patch.SectorIndices);
            var result = 0;
            foreach (var index in sectors)
            {
                var coordinate = WorldGridIndex.ToCoordinate(index);
                if (coordinate.X == 0 || !sectors.Contains(index - 1)) result++;
                if (coordinate.X == 12 || !sectors.Contains(index + 1)) result++;
                if (coordinate.Y == 0 || !sectors.Contains(index - 13)) result++;
                if (coordinate.Y == 12 || !sectors.Contains(index + 13)) result++;
            }
            return result;
        }

        private static string OverlaySignature(BiomePatchOverlaySnapshot value)
        {
            return value.WorldSeed.ToString(CultureInfo.InvariantCulture) + "#" +
                   string.Join("|", value.Cells.Select(cell =>
                       cell.Index + ":" + cell.PrimaryBiomeId + ":" +
                       (cell.PatchId.HasValue ? cell.PatchId.Value.Value : "") + ":" +
                       cell.RoleToken + ":" + cell.PatchSize + ":" + cell.Perimeter + ":" +
                       cell.CompactnessPermille + ":" + cell.IsSeed + ":" + cell.IsCoreSiteCell + ":" +
                       cell.BorderLeft + cell.BorderRight + cell.BorderUp + cell.BorderDown + ":" +
                       cell.CellLabel + ":" + cell.Tooltip)) + "#" +
                   string.Join("|", value.Patches.Select(row =>
                       row.PatchId.Value + ":" + row.BiomeId + ":" + row.Role + ":" + row.Size + ":" +
                       row.Perimeter + ":" + row.CompactnessPermille + ":" + row.SeedCount + ":" +
                       row.CoreSiteCellCount));
        }

        private static string SourceSignature(BiomePatchValidationPublication value)
        {
            return string.Join("|", value.Snapshot.Patches.Select(patch =>
                       patch.Id.Value + ":" + patch.BiomeId + ":" + string.Join(",", patch.SectorIndices))) + "#" +
                   string.Join("|", value.Snapshot.Sectors.Select(cell =>
                       cell.SectorIndex + ":" + cell.PrimaryBiomeId + ":" + cell.SecondaryBiomeId + ":" +
                       (cell.PatchId.HasValue ? cell.PatchId.Value.Value : ""))) + "#" +
                   string.Join("|", value.PatchRows.Select(row =>
                       row.PatchInstanceId.Value + ":" + row.SectorCount + ":" + row.PerimeterEdges));
        }

        private static T Clone<T>(T source) where T : class
        {
            return (T)typeof(object).GetMethod(
                "MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(source, null);
        }

        private static void SetField(object target, string name, object value)
        {
            var field = FindField(target.GetType(), name);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private static FieldInfo FindField(Type type, string name)
        {
            for (var current = type; current != null; current = current.BaseType)
            {
                var field = current.GetField(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null) return field;
            }
            return null;
        }

        private static object InvokePrivateStatic(Type type, string name, object[] arguments)
        {
            var method = type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, name);
            return method.Invoke(null, arguments);
        }

        private static object Get(object instance, string propertyName)
        {
            var property = instance.GetType().GetProperty(
                propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, propertyName);
            return property.GetValue(instance);
        }

        private static void AssertReadOnly<T>(IReadOnlyList<T> values)
        {
            Assert.That(values, Is.InstanceOf<IList>());
            Assert.Throws<NotSupportedException>(() => ((IList)values).Add(default(T)));
        }
    }
}
