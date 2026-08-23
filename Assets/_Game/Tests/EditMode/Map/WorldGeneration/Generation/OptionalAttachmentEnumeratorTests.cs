using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    [Category("MAP06_02")]
    public sealed class OptionalAttachmentEnumeratorTests
    {
        private MandatoryRouteGraph graph;
        private MandatoryRouteValidationReport report;
        private GeneratedWorldData world;
        private SiteReservationSnapshot site;
        private BiomePatchValidationPublication biome;
        private OptionalAttachmentEnumerationResult baseline;
        private string sourceSignature;

        public static IEnumerable<int> CandidateIdCases => Enumerable.Range(0, 12);
        public static IEnumerable<int> CandidateIdOrderingCases => Enumerable.Range(0, 12);
        public static IEnumerable<int> ValidSettingsCases => Enumerable.Range(0, 8);
        public static IEnumerable<int> CandidateConstructorCases => Enumerable.Range(0, 12);
        public static IEnumerable<int> InvalidCandidateCases => Enumerable.Range(0, 10);
        public static IEnumerable<int> DeterminismCases => Enumerable.Range(0, 24);
        public static IEnumerable<int> FilterCases => Enumerable.Range(0, 36);
        public static IEnumerable<int> DigestCases => Enumerable.Range(0, 18);
        public static IEnumerable<int> MutationGuardCases => Enumerable.Range(0, 10);

        public static IEnumerable<string> InvalidCandidateIds => new[]
        {
            null, string.Empty, "OPT_ATTACH_000", "OPT_ATTACH_00000", "OPT_ATTACH_-001",
            "OPT_ATTACH_10000", "opt_attach_0000", "OPT_attach_0000", "OPT_ATTACH_00A0",
            "OPT_ATTACH_０00", " OPT_ATTACH_0000", "OPT_ATTACH_0000 ", "OPTATTACH_0000",
            "OPT_ATTACH0000", "OPT_ATTACH_0_00", "OPT_ATTACH_000/", "OPT_ATTACH_한글",
            "OPT_ATTACH_000\n"
        };

        public static IEnumerable<int> InvalidMaxCandidates => new[]
        {
            int.MinValue, -100, -1, 0, 10000, 10001, 20000, int.MaxValue
        };

        public static IEnumerable<string> CurrentRuntimeSymbols => new[]
        {
            "OptionalAttachmentCandidateId",
            "OptionalAttachmentCandidate",
            "OptionalAttachmentEnumerationSettings",
            "OptionalAttachmentEnumerationDiagnostics",
            "OptionalAttachmentEnumerationResult",
            "OptionalAttachmentEnumerator"
        };

        public static IEnumerable<string> FutureRuntimeSymbols => new[]
        {
            "OptionalRouteMaskLookup",
            "OptionalOverlayEdge",
            "OptionalReturnConnection",
            "OptionalClueAssigner",
            "MicrochunkObjectSlotValidator",
            "MicrochunkPreviewReport",
            "OptionalRegionOverlayRenderer",
            "OptionalRegionValidationOverlayWindow",
            "OptionalRegionOverlay",
            "GeneratedOptionalRegionCsvWriter"
        };

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var fixture = new MandatoryRouteGraphValidatorTests();
            fixture.OneTimeSetUp();
            graph = GetField<MandatoryRouteGraph>(fixture, typeof(MandatoryRouteGraphValidatorTests), "graph");
            var validation = new MandatoryRouteGraphValidator().Validate(graph);
            Assert.That(validation.Status, Is.EqualTo(MandatoryRouteValidationStatus.Completed));
            Assert.That(validation.Succeeded, Is.True);
            report = validation.Report;
            world = graph.RouteStampedWorld;
            site = graph.SourceTerminalSet.SourceSiteSnapshot;
            biome = graph.SourceTerminalSet.SourceBiomePublication;
            baseline = Enumerate(new OptionalAttachmentEnumerationSettings());
            Assert.That(baseline.Candidates, Is.Not.Empty);
            sourceSignature = SourceSignature();
        }

        [TestCaseSource(nameof(CandidateIdCases))]
        public void CandidateIdRoundTripsExactFourDigitOrdinal(int ordinal)
        {
            var id = OptionalAttachmentCandidateId.FromOrdinal(ordinal);
            Assert.That(OptionalAttachmentCandidateId.TryCreate(id.Value, out var parsed), Is.True);
            Assert.That(parsed, Is.EqualTo(id));
            Assert.That(parsed.TryGetOrdinal(out var parsedOrdinal), Is.True);
            Assert.That(parsedOrdinal, Is.EqualTo(ordinal));
            Assert.That(parsed.ToString(), Is.EqualTo("OPT_ATTACH_" + ordinal.ToString("D4", CultureInfo.InvariantCulture)));
        }

        [TestCaseSource(nameof(InvalidCandidateIds))]
        public void CandidateIdRejectsNonCanonicalInput(string value)
        {
            Assert.That(OptionalAttachmentCandidateId.TryCreate(value, out var parsed), Is.False);
            Assert.That(parsed.IsValid, Is.False);
            if (value == null)
                Assert.Throws<ArgumentNullException>(() => new OptionalAttachmentCandidateId(value));
            else
                Assert.Throws<ArgumentException>(() => new OptionalAttachmentCandidateId(value));
        }

        [TestCaseSource(nameof(CandidateIdOrderingCases))]
        public void CandidateIdOrderingEqualityAndHashAreOrdinal(int caseId)
        {
            var left = OptionalAttachmentCandidateId.FromOrdinal(caseId);
            var copy = new OptionalAttachmentCandidateId(new string(left.Value.ToCharArray()));
            var right = OptionalAttachmentCandidateId.FromOrdinal(11 - caseId);
            Assert.That(left, Is.EqualTo(copy));
            Assert.That(left.GetHashCode(), Is.EqualTo(copy.GetHashCode()));
            Assert.That(Math.Sign(left.CompareTo(right)), Is.EqualTo(Math.Sign(caseId.CompareTo(11 - caseId))));
            Assert.That(default(OptionalAttachmentCandidateId).IsValid, Is.False);
        }

        [Test]
        public void DefaultSettingsAreExactAndImmutable()
        {
            var settings = new OptionalAttachmentEnumerationSettings();
            Assert.That(settings.MaxCandidates, Is.EqualTo(9999));
            Assert.That(settings.ExcludeMandatoryTerminals, Is.True);
            Assert.That(settings.ExcludeSiteReservations, Is.True);
            Assert.That(settings.ExcludeBiomeReservedOrInactive, Is.True);
            Assert.That(settings.DeduplicateEntrySector, Is.True);
            Assert.That(typeof(OptionalAttachmentEnumerationSettings).GetProperties().All(value => !value.CanWrite), Is.True);
        }

        [TestCaseSource(nameof(ValidSettingsCases))]
        public void SettingsPreserveEveryExplicitFlag(int caseId)
        {
            var settings = new OptionalAttachmentEnumerationSettings(
                caseId + 1,
                (caseId & 1) != 0,
                (caseId & 2) != 0,
                (caseId & 4) != 0,
                (caseId & 1) == 0);
            Assert.That(settings.MaxCandidates, Is.EqualTo(caseId + 1));
            Assert.That(settings.ExcludeMandatoryTerminals, Is.EqualTo((caseId & 1) != 0));
            Assert.That(settings.ExcludeSiteReservations, Is.EqualTo((caseId & 2) != 0));
            Assert.That(settings.ExcludeBiomeReservedOrInactive, Is.EqualTo((caseId & 4) != 0));
            Assert.That(settings.DeduplicateEntrySector, Is.EqualTo((caseId & 1) == 0));
        }

        [TestCaseSource(nameof(InvalidMaxCandidates))]
        public void SettingsRejectMaxCandidatesOutsideExactRange(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new OptionalAttachmentEnumerationSettings(value, true, true, true, true));
        }

        [TestCaseSource(nameof(CandidateConstructorCases))]
        public void CandidateConstructorPreservesCardinalIndexCoordinateIdentity(int caseId)
        {
            var mandatory = new SectorCoord(2 + (caseId % 6), 2 + (caseId / 6));
            var directions = new[] { new[] { -1, 0 }, new[] { 1, 0 }, new[] { 0, 1 }, new[] { 0, -1 } };
            var direction = directions[caseId % directions.Length];
            var entry = new SectorCoord(mandatory.X + direction[0], mandatory.Y + direction[1]);
            var mandatoryIndex = WorldGridIndex.ToIndex(mandatory);
            var entryIndex = WorldGridIndex.ToIndex(entry);
            var candidate = new OptionalAttachmentCandidate(
                OptionalAttachmentCandidateId.FromOrdinal(caseId),
                caseId,
                mandatoryIndex,
                mandatory,
                NodeId(mandatoryIndex),
                entryIndex,
                entry,
                direction[0],
                direction[1],
                new OptionalRegionDepth(1));
            Assert.That(candidate.CandidateId.TryGetOrdinal(out var ordinal) && ordinal == caseId, Is.True);
            Assert.That(candidate.MandatoryRouteSectorIndex, Is.EqualTo(mandatoryIndex));
            Assert.That(candidate.EntrySectorIndex, Is.EqualTo(entryIndex));
            Assert.That(candidate.EntrySector.X - candidate.MandatoryRouteSector.X, Is.EqualTo(candidate.DirectionDx));
            Assert.That(candidate.EntrySector.Y - candidate.MandatoryRouteSector.Y, Is.EqualTo(candidate.DirectionDy));
            Assert.That(candidate.InitialDepth.Value, Is.EqualTo(1));
        }

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(-1, -1)]
        [TestCase(2, 0)]
        [TestCase(0, 2)]
        [TestCase(-2, 0)]
        [TestCase(0, -2)]
        [TestCase(2, 2)]
        public void CandidateConstructorRejectsNonCardinalDirections(int dx, int dy)
        {
            Assert.That(() => new OptionalAttachmentCandidate(
                OptionalAttachmentCandidateId.FromOrdinal(0), 0, 84, WorldGridIndex.ToCoordinate(84),
                NodeId(84), 85, WorldGridIndex.ToCoordinate(85), dx, dy, new OptionalRegionDepth(1)),
                Throws.Exception);
        }

        [TestCaseSource(nameof(InvalidCandidateCases))]
        public void CandidateConstructorRejectsInvalidIdentityInputs(int caseId)
        {
            var id = OptionalAttachmentCandidateId.FromOrdinal(0);
            var order = 0;
            var mandatoryIndex = 84;
            var mandatory = WorldGridIndex.ToCoordinate(84);
            var nodeId = NodeId(84);
            var entryIndex = 85;
            var entry = WorldGridIndex.ToCoordinate(85);
            var dx = 1;
            var dy = 0;
            var depth = new OptionalRegionDepth(1);
            switch (caseId)
            {
                case 0: id = default(OptionalAttachmentCandidateId); break;
                case 1: order = 1; break;
                case 2: mandatoryIndex = -1; break;
                case 3: mandatory = WorldGridIndex.ToCoordinate(83); break;
                case 4: nodeId = default(MandatoryRouteGraphNodeId); break;
                case 5: entryIndex = -1; break;
                case 6: entry = WorldGridIndex.ToCoordinate(86); break;
                case 7: dx = 0; dy = 1; break;
                case 8: depth = new OptionalRegionDepth(2); break;
                case 9: order = 9999; break;
            }
            Assert.That(() => new OptionalAttachmentCandidate(
                id, order, mandatoryIndex, mandatory, nodeId, entryIndex, entry, dx, dy, depth),
                Throws.Exception);
        }

        [TestCaseSource(nameof(DeterminismCases))]
        public void EnumerationIsCultureFreshReuseAndCanonical(int caseId)
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = (caseId & 1) == 0
                    ? CultureInfo.GetCultureInfo("en-US")
                    : CultureInfo.GetCultureInfo("tr-TR");
                var result = Enumerate(new OptionalAttachmentEnumerationSettings());
                Assert.That(result.CanonicalDigest, Is.EqualTo(baseline.CanonicalDigest));
                Assert.That(CandidateSignature(result), Is.EqualTo(CandidateSignature(baseline)));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [TestCaseSource(nameof(FilterCases))]
        public void FilterMatrixPreservesBoundsExclusionsAccountingAndUniqueEntries(int caseId)
        {
            var settings = new OptionalAttachmentEnumerationSettings(
                1 + (caseId % Math.Min(20, baseline.Candidates.Count)),
                (caseId & 1) != 0,
                (caseId & 2) != 0,
                (caseId & 4) != 0,
                (caseId & 8) != 0);
            var result = Enumerate(settings);
            var diagnostics = result.Diagnostics;
            var rejected = diagnostics.OutOfBoundsRejected + diagnostics.MandatoryRejected +
                diagnostics.TerminalRejected + diagnostics.SiteReservationRejected +
                diagnostics.BiomeReservedRejected + diagnostics.DuplicateEntryRejected;
            Assert.That(diagnostics.RawNeighborProbes, Is.EqualTo(rejected + diagnostics.AcceptedCount));
            Assert.That(result.Candidates, Has.Count.EqualTo(diagnostics.AcceptedCount));
            Assert.That(result.Candidates.Count, Is.LessThanOrEqualTo(settings.MaxCandidates));
            Assert.That(result.Candidates.Select(value => value.EntrySectorIndex).Distinct().Count(), Is.EqualTo(result.Candidates.Count));
            Assert.That(result.Candidates.All(value => !graph.Cells.Any(cell => cell.SectorIndex == value.EntrySectorIndex)), Is.True);
            Assert.That(result.Candidates.Select(value => value.AttachmentOrder), Is.EqualTo(Enumerable.Range(0, result.Candidates.Count)));
            Assert.That(diagnostics.RejectionCodes.All(IsKnownRejectionCode), Is.True);
            if (settings.ExcludeSiteReservations)
                Assert.That(result.Candidates.All(value => !site.GetSector(value.EntrySectorIndex).IsReserved), Is.True);
            if (settings.ExcludeBiomeReservedOrInactive)
                Assert.That(result.Candidates.All(value => IsActiveBiomeCell(biome.WorldWithBiomeAssignments.GetCell(value.EntrySectorIndex))), Is.True);
        }

        [TestCaseSource(nameof(DigestCases))]
        public void DiagnosticsAndCanonicalDigestAreStableFrozenAndComplete(int caseId)
        {
            var result = Enumerate(new OptionalAttachmentEnumerationSettings());
            Assert.That(result.CanonicalDigest, Is.EqualTo(baseline.CanonicalDigest));
            Assert.That(result.CanonicalDigest.Length, Is.EqualTo(64));
            Assert.That(result.CanonicalDigest.All(value => (value >= '0' && value <= '9') || (value >= 'a' && value <= 'f')), Is.True);
            Assert.That(result.Diagnostics.AcceptedCount, Is.EqualTo(result.Candidates.Count));
            Assert.That(result.MandatoryRouteGraphNodeCount, Is.EqualTo(47));
            Assert.That(result.MandatoryRouteGraphDirectedEdgeCount, Is.EqualTo(96));
            Assert.That(result.MandatoryRouteCellCount, Is.EqualTo(47));
            Assert.Throws<NotSupportedException>(() => ((IList<OptionalAttachmentCandidate>)result.Candidates).Add(result.Candidates[0]));
            if (result.Diagnostics.RejectionCodes.Count > 0)
                Assert.Throws<NotSupportedException>(() => ((IList<string>)result.Diagnostics.RejectionCodes).Add("OTHER"));
        }

        [TestCaseSource(nameof(MutationGuardCases))]
        public void EnumerationConsumesNoRngFilesystemUnityLifecycleOrSourceMutation(int caseId)
        {
            var before = SourceSignature();
            var result = Enumerate(new OptionalAttachmentEnumerationSettings());
            Assert.That(SourceSignature(), Is.EqualTo(before));
            Assert.That(SourceSignature(), Is.EqualTo(sourceSignature));
            Assert.That(result.CanonicalDigest, Is.EqualTo(baseline.CanonicalDigest));
            var types = new[]
            {
                typeof(OptionalAttachmentCandidateId), typeof(OptionalAttachmentCandidate),
                typeof(OptionalAttachmentEnumerationSettings), typeof(OptionalAttachmentEnumerationDiagnostics),
                typeof(OptionalAttachmentEnumerationResult), typeof(OptionalAttachmentEnumerator)
            };
            foreach (var type in types)
            {
                Assert.That(type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(value => !value.IsLiteral && !value.IsInitOnly), Is.Empty, type.FullName);
                var surface = string.Join("|", type.GetMembers().Select(value => value.Name));
                Assert.That(surface, Does.Not.Contain("Random"));
                Assert.That(surface, Does.Not.Contain("Rng"));
                Assert.That(surface, Does.Not.Contain("File"));
                Assert.That(surface, Does.Not.Contain("Directory"));
            }
            Assert.That(typeof(OptionalAttachmentEnumerator).Assembly.GetReferencedAssemblies()
                .Any(value => value.Name == "UnityEditor"), Is.False);
        }

        [TestCase(false, false, MandatoryRouteMaskFamily.Type4UdId, 0)]
        [TestCase(true, false, MandatoryRouteMaskFamily.Type4LudId, 1)]
        [TestCase(false, true, MandatoryRouteMaskFamily.Type4RudId, 2)]
        [TestCase(true, true, MandatoryRouteMaskFamily.Type4LrudId, 3)]
        [TestCase(false, false, MandatoryRouteMaskFamily.Type4UdId, 4)]
        [TestCase(true, false, MandatoryRouteMaskFamily.Type4LudId, 5)]
        [TestCase(false, true, MandatoryRouteMaskFamily.Type4RudId, 6)]
        [TestCase(true, true, MandatoryRouteMaskFamily.Type4LrudId, 7)]
        public void Type4AlwaysRequiresUpDownAndPreservesIndependentLeftRight(bool left, bool right, string expectedId, int caseId)
        {
            Assert.That(graph.MaskFamily.TryResolve(left, right, true, true, out var mask), Is.True);
            Assert.That(mask.MaskId, Is.EqualTo(expectedId));
            Assert.That(mask.OpenLeft, Is.EqualTo(left));
            Assert.That(mask.OpenRight, Is.EqualTo(right));
            Assert.That(mask.OpenUp && mask.OpenDown, Is.True);
            Assert.That(Enumerate(new OptionalAttachmentEnumerationSettings()).CanonicalDigest, Is.EqualTo(baseline.CanonicalDigest));
        }

        [TestCaseSource(nameof(CurrentRuntimeSymbols))]
        public void Map06_02RuntimeSymbolsArePresent(string typeName)
        {
            Assert.That(typeof(OptionalAttachmentEnumerator).Assembly.GetType(
                "StarNight.Map.WorldGeneration.Generation." + typeName, false), Is.Not.Null);
        }

        [TestCaseSource(nameof(FutureRuntimeSymbols))]
        public void Map06_04PlusRuntimeSymbolsRemainAbsent(string typeName)
        {
            Assert.That(typeof(OptionalAttachmentEnumerator).Assembly.GetType(
                "StarNight.Map.WorldGeneration.Generation." + typeName, false), Is.Null);
        }

        [Test]
        public void Map06_02TestSymbolIsPresentAndStarterSummaryIsCanonical()
        {
            Assert.That(typeof(OptionalAttachmentEnumeratorTests).Assembly.GetType(
                "StarNight.Map.Tests.WorldGeneration.Generation.OptionalAttachmentEnumeratorTests", false), Is.Not.Null);
            var d = baseline.Diagnostics;
            TestContext.WriteLine(
                "MAP06_02_SUMMARY raw={0} accepted={1} out={2} mandatory={3} terminal={4} site={5} biome={6} duplicate={7} digest={8}",
                d.RawNeighborProbes, d.AcceptedCount, d.OutOfBoundsRejected, d.MandatoryRejected,
                d.TerminalRejected, d.SiteReservationRejected, d.BiomeReservedRejected,
                d.DuplicateEntryRejected, baseline.CanonicalDigest);
            Assert.That(d.RawNeighborProbes, Is.EqualTo(188));
            Assert.That(baseline.Candidates.Select(value => value.CandidateId.Value), Is.Ordered);
        }

        private OptionalAttachmentEnumerationResult Enumerate(OptionalAttachmentEnumerationSettings settings)
        {
            return new OptionalAttachmentEnumerator().Enumerate(world, graph, report, site, biome, settings);
        }

        private string SourceSignature()
        {
            return graph.NodeCount + "/" + graph.DirectedEdgeCount + "/" + graph.CellCount + "|" +
                string.Join(",", graph.Nodes.Select(value => value.NodeId.Value + ":" + value.SectorIndex + ":" + value.RouteMaskId)) + "|" +
                string.Join(",", graph.Cells.Select(value => value.SectorIndex + ":" + value.RouteMaskId)) + "|" +
                world.Seed + ":" + world.Cells.Count + "|" + site.Seed + ":" + site.Reservations.Count + ":" + site.Sectors.Count + "|" +
                biome.Snapshot.Seed + ":" + biome.PatchRows.Count + "|" + report.PassId + ":" + report.Violations.Count;
        }

        private static string CandidateSignature(OptionalAttachmentEnumerationResult result)
        {
            return string.Join(",", result.Candidates.Select(value =>
                value.CandidateId.Value + ":" + value.MandatoryRouteSectorIndex + ">" + value.EntrySectorIndex +
                ":" + value.DirectionDx + ":" + value.DirectionDy)) + "|" + result.CanonicalDigest;
        }

        private static MandatoryRouteGraphNodeId NodeId(int sectorIndex)
        {
            return new MandatoryRouteGraphNodeId(
                "NODE_" + sectorIndex.ToString("D3", CultureInfo.InvariantCulture) + "_OPTIONAL");
        }

        private static bool IsKnownRejectionCode(string code)
        {
            return code == "OUT_OF_BOUNDS" || code == "MANDATORY" || code == "TERMINAL" ||
                code == "SITE_RESERVATION" || code == "BIOME_RESERVED" || code == "DUPLICATE_ENTRY";
        }

        private static bool IsActiveBiomeCell(SectorCell cell)
        {
            return !string.IsNullOrEmpty(cell.PrimaryBiomeId) && !string.IsNullOrEmpty(cell.PatchId);
        }

        private static T GetField<T>(object target, Type type, string name)
        {
            return (T)type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }
    }
}
