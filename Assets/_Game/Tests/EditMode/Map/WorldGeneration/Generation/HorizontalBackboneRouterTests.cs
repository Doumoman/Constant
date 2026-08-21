using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    [Category("MAP05_04")]
    public sealed class HorizontalBackboneRouterTests
    {
        private MandatoryConnectorTree tree;
        private MandatoryRouteMaskLookup lookup;
        private SiteReservationSnapshot site;
        private BiomePatchValidationPublication biome;
        private HorizontalBackboneRouter reused;
        private string expectedSignature;

        public static IEnumerable DeterminismCases
        {
            get
            {
                for (var index = 0; index < 100; index++)
                    yield return new TestCaseData(index).SetName("Build_DeterministicHorizontalBackbone_" + index.ToString("D3", CultureInfo.InvariantCulture));
            }
        }

        public static IEnumerable InvalidSegmentIds
        {
            get
            {
                yield return null;
                yield return string.Empty;
                yield return "HSEG_0_X";
                yield return "hseg_00_X";
                yield return "HSEG_000_X";
                yield return "HSEG_00_";
                yield return "HSEG_A0_X";
                yield return "HSEG_0A_X";
                yield return "HSEG_00_x";
                yield return "HSEG_00_A-B";
                yield return "HSEG00_X";
                yield return "HSEG_00_A B";
                yield return "HSEG_99_한글";
                yield return "HSEG_00_A/B";
                yield return "HSEG_00_A.B";
                yield return "HSEG__00_X";
            }
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var terminalFixture = new MandatoryTerminalBuilderTests();
            terminalFixture.OneTimeSetUp();
            site = GetField<SiteReservationSnapshot>(terminalFixture, typeof(MandatoryTerminalBuilderTests), "site");
            biome = GetField<BiomePatchValidationPublication>(terminalFixture, typeof(MandatoryTerminalBuilderTests), "biome");
            var terminalResult = new MandatoryTerminalBuilder().Build(site, biome);
            Assert.That(terminalResult.Status, Is.EqualTo(MandatoryTerminalBuildStatus.Completed));

            var buildStarter = typeof(MandatoryRouteMaskLookupBuilderTests).GetMethod("BuildStarter", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(buildStarter, Is.Not.Null);
            var lookupResult = (MandatoryRouteMaskLookupBuildResult)buildStarter.Invoke(null, null);
            Assert.That(lookupResult.Status, Is.EqualTo(MandatoryRouteMaskLookupBuildStatus.Completed));
            lookup = lookupResult.Lookup;

            var treeResult = new MandatoryConnectorTreeBuilder().Build(terminalResult.TerminalSet, lookup);
            Assert.That(treeResult.Status, Is.EqualTo(MandatoryConnectorTreeBuildStatus.Completed));
            tree = treeResult.Tree;
            reused = new HorizontalBackboneRouter();
            expectedSignature = Signature(Complete(reused));
        }

        [TestCaseSource(nameof(DeterminismCases))]
        public void Build_DeterministicHorizontalBackbone(int caseId)
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = (caseId & 1) == 0 ? CultureInfo.GetCultureInfo("en-US") : CultureInfo.GetCultureInfo("tr-TR");
                var router = (caseId & 2) == 0 ? new HorizontalBackboneRouter() : reused;
                Assert.That(Signature(Complete(router)), Is.EqualTo(expectedSignature));
            }
            finally { CultureInfo.CurrentCulture = previous; }
        }

        [TestCaseSource(nameof(InvalidSegmentIds))]
        public void SegmentIdRejectsNonCanonicalValues(string value)
        {
            Assert.That(HorizontalBackboneSegmentId.TryCreate(value, out var parsed), Is.False);
            Assert.That(parsed.IsValid, Is.False);
            if (value == null) Assert.Throws<ArgumentNullException>(() => new HorizontalBackboneSegmentId(value));
            else Assert.Throws<ArgumentException>(() => new HorizontalBackboneSegmentId(value));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        [TestCase(8)]
        public void RouteCellAcceptsExactFiniteCostLevels(int cost)
        {
            var cell = new HorizontalBackboneRouteCell(new SectorCoord(1, 1), 0, true, true, true, false, false, cost);
            Assert.That(cell.StepCost, Is.EqualTo(cost));
            Assert.That(cell.OpensLeft && cell.OpensRight, Is.True);
        }

        [Test]
        public void StarterBuildPublishesExactSixSegmentsAndDiagnostics()
        {
            var result = Complete(reused);
            Assert.That(result.Succeeded && !result.RetryRequired, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(new[] { result.Diagnostics.TreeEdgeCount, result.Diagnostics.SegmentCount }, Is.EqualTo(new[] { 6, 6 }));
            Assert.That(result.Diagnostics.SameRowSegmentCount + result.Diagnostics.GatewayPendingSegmentCount, Is.EqualTo(6));
            Assert.That(result.Diagnostics.GatewayPendingSegmentCount, Is.GreaterThan(0));
        }

        [Test]
        public void PlanPreservesAllFourExactSourceReferences()
        {
            var plan = Complete(reused).Plan;
            Assert.That(plan.SourceConnectorTree, Is.SameAs(tree));
            Assert.That(plan.SourceRouteMaskLookup, Is.SameAs(lookup));
            Assert.That(plan.SourceSiteSnapshot, Is.SameAs(site));
            Assert.That(plan.SourceBiomePublication, Is.SameAs(biome));
        }

        [Test]
        public void SegmentIdsUseTreeOrderAndSourceEdgeSuffix()
        {
            var segments = Complete(reused).Plan.Segments;
            for (var index = 0; index < segments.Count; index++)
            {
                var expected = "HSEG_" + index.ToString("D2", CultureInfo.InvariantCulture) + "_" + segments[index].SourceTreeEdgeId.Value.Substring(8);
                Assert.That(segments[index].SegmentId.Value, Is.EqualTo(expected));
            }
        }

        [Test]
        public void AllCellsPreserveHorizontalOpeningsAndNeverOpenUpDown()
        {
            var result = Complete(reused);
            Assert.That(result.Plan.Segments.SelectMany(value => value.Cells).All(value => value.OpensLeft && value.OpensRight), Is.True);
            Assert.That(result.Diagnostics.OpenUpDownCount, Is.Zero);
            Assert.That(result.Diagnostics.RouteGraphEdgeCount, Is.Zero);
            Assert.That(result.Diagnostics.GeneratedCsvRowCount, Is.Zero);
        }

        [Test]
        public void SameRowRunsAreDirectInclusiveAndGatewayFree()
        {
            var sameRows = Complete(reused).Plan.Segments.Where(value => value.IsSameRow).ToList();
            Assert.That(sameRows, Is.Not.Empty);
            foreach (var segment in sameRows)
            {
                var minimum = Math.Min(segment.FromApproachSector.X, segment.ToApproachSector.X);
                var maximum = Math.Max(segment.FromApproachSector.X, segment.ToApproachSector.X);
                Assert.That(segment.Cells.Select(value => value.Coord.X).OrderBy(value => value), Is.EqualTo(Enumerable.Range(minimum, maximum - minimum + 1)));
                Assert.That(segment.Cells.All(value => value.Coord.Y == segment.FromApproachSector.Y), Is.True);
                Assert.That(segment.RequiresVerticalGateway, Is.False);
                Assert.That(segment.HorizontalDistance, Is.EqualTo(maximum - minimum));
            }
        }

        [Test]
        public void DifferentRowRunsHaveTwoPendingAnchorsAndNoVerticalCells()
        {
            foreach (var segment in Complete(reused).Plan.Segments.Where(value => !value.IsSameRow))
            {
                var gateways = segment.Cells.Where(value => value.RequiresVerticalGateway).ToList();
                Assert.That(gateways, Has.Count.EqualTo(2));
                Assert.That(gateways.Select(value => value.Coord.X).Distinct().Count(), Is.EqualTo(1));
                Assert.That(gateways.Select(value => value.Coord.Y), Is.EquivalentTo(new[] { segment.FromApproachSector.Y, segment.ToApproachSector.Y }));
                Assert.That(segment.Cells.All(value => value.Coord.Y == segment.FromApproachSector.Y || value.Coord.Y == segment.ToApproachSector.Y), Is.True);
            }
        }

        [Test]
        public void GatewayTieBreakAndFreshReuseProduceOneExactSignature()
        {
            Assert.That(Signature(Complete(new HorizontalBackboneRouter())), Is.EqualTo(Signature(Complete(reused))));
        }

        [Test]
        public void PlanLookupReturnsExactSegmentInstances()
        {
            var plan = Complete(reused).Plan;
            foreach (var segment in plan.Segments)
            {
                Assert.That(plan.TryGetSegment(segment.SegmentId, out var found), Is.True);
                Assert.That(found, Is.SameAs(segment));
            }
            Assert.That(plan.TryGetSegment(new HorizontalBackboneSegmentId("HSEG_99_UNKNOWN"), out _), Is.False);
        }

        [Test]
        public void TerminalAdjacencyIsExactAndReadOnly()
        {
            var plan = Complete(reused).Plan;
            foreach (var terminal in tree.SourceTerminalSet.Terminals)
            {
                var segments = plan.GetSegmentsForTerminal(terminal.TerminalId);
                Assert.That(segments, Is.Not.Empty);
                Assert.That(segments.All(value => value.FromTerminalId == terminal.TerminalId || value.ToTerminalId == terminal.TerminalId), Is.True);
                Assert.Throws<NotSupportedException>(() => ((IList<HorizontalBackboneSegment>)segments).Add(plan.Segments[0]));
            }
        }

        [Test]
        public void SegmentAndPlanCostsAreCheckedSumsOfFiniteCellCosts()
        {
            var plan = Complete(reused).Plan;
            foreach (var segment in plan.Segments)
            {
                Assert.That(segment.Cells.All(value => value.StepCost == 1 || value.StepCost == 2 || value.StepCost == 4 || value.StepCost == 8), Is.True);
                Assert.That(segment.TotalCost, Is.EqualTo(segment.Cells.Sum(value => value.StepCost)));
            }
            Assert.That(plan.TotalCost, Is.EqualTo(plan.Segments.Sum(value => value.TotalCost)));
        }

        [Test]
        public void ReservationFootprintsNeverAppearAsMiddleCells()
        {
            var cells = Complete(reused).Plan.Segments.SelectMany(value => value.Cells).ToList();
            Assert.That(cells.Where(value => value.IsReserved).All(value => value.IsEndpoint), Is.True);
            Assert.That(cells.All(value => !site.GetSector(value.Coord).IsReserved || value.IsEndpoint), Is.True);
        }

        [Test]
        public void EveryPublishedCellIsInsideExactThirteenByThirteenWorld()
        {
            Assert.That(Complete(reused).Plan.Segments.SelectMany(value => value.Cells).All(value =>
                value.Coord.X >= 0 && value.Coord.X < WorldGenConstants.SectorColumns &&
                value.Coord.Y >= 0 && value.Coord.Y < WorldGenConstants.SectorRows), Is.True);
        }

        [Test]
        public void PlanAndCellCollectionsAreDefensiveReadOnlyViews()
        {
            var plan = Complete(reused).Plan;
            Assert.Throws<NotSupportedException>(() => ((IList<HorizontalBackboneSegment>)plan.Segments).Add(plan.Segments[0]));
            Assert.Throws<NotSupportedException>(() => ((IList<HorizontalBackboneRouteCell>)plan.Segments[0].Cells).Add(plan.Segments[0].Cells[0]));
        }

        [Test]
        public void NullInputsAccumulateFourSortedErrorsAtomically()
        {
            var result = new HorizontalBackboneRouter().Build(null, null, null, null);
            Assert.That(result.Status, Is.EqualTo(HorizontalBackboneBuildStatus.InvalidInput));
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.Diagnostics, Is.Null);
            Assert.That(result.Errors, Has.Count.EqualTo(4));
            Assert.That(result.Errors.All(value => value.Code == HorizontalBackboneBuildErrorCode.MissingInput), Is.True);
            Assert.That(result.RetryRequired, Is.False);
        }

        [Test]
        public void MissingAnySingleInputPublishesNothing()
        {
            Assert.That(reused.Build(null, lookup, site, biome).Succeeded, Is.False);
            Assert.That(reused.Build(tree, null, site, biome).Succeeded, Is.False);
            Assert.That(reused.Build(tree, lookup, null, biome).Succeeded, Is.False);
            Assert.That(reused.Build(tree, lookup, site, null).Succeeded, Is.False);
        }

        [Test]
        public void RouteCellRejectsNonHorizontalOrReservedMiddleOrInfiniteCost()
        {
            var coord = new SectorCoord(1, 1);
            Assert.Throws<ArgumentException>(() => new HorizontalBackboneRouteCell(coord, 0, false, true, true, false, false, 1));
            Assert.Throws<ArgumentException>(() => new HorizontalBackboneRouteCell(coord, 0, true, true, false, true, false, 8));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HorizontalBackboneRouteCell(coord, 0, true, true, true, false, false, int.MaxValue));
        }

        [Test]
        public void RouteCellRejectsWorldBoundsAndNegativeOrdinal()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HorizontalBackboneRouteCell(new SectorCoord(-1, 0), 0, true, true, true, false, false, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HorizontalBackboneRouteCell(new SectorCoord(0, 0), -1, true, true, true, false, false, 1));
        }

        [Test]
        public void SegmentIdHasStableOrdinalEqualityOrderAndHash()
        {
            var first = new HorizontalBackboneSegmentId("HSEG_00_TERM_A__TO__TERM_B");
            var same = new HorizontalBackboneSegmentId(new string(first.Value.ToCharArray()));
            var later = new HorizontalBackboneSegmentId("HSEG_01_TERM_A__TO__TERM_C");
            Assert.That(first == same && first.GetHashCode() == same.GetHashCode(), Is.True);
            Assert.That(first.CompareTo(later), Is.LessThan(0));
            Assert.That(default(HorizontalBackboneSegmentId).IsValid, Is.False);
        }

        [Test]
        public void SourceArtifactsRemainObservablyUnchanged()
        {
            var before = SourceSignature();
            Complete(reused);
            Assert.That(SourceSignature(), Is.EqualTo(before));
        }

        [Test]
        public void FreshReusedAndParallelRoutersHaveOneSignature()
        {
            var values = new string[12];
            Parallel.For(0, values.Length, index => values[index] = Signature(Complete((index & 1) == 0 ? new HorizontalBackboneRouter() : reused)));
            Assert.That(values.Distinct().Single(), Is.EqualTo(expectedSignature));
        }

        [Test]
        public void RuntimeSurfaceHasNoMutableStaticUnityEditorOrMap06_03PlusSymbols()
        {
            var types = new[]
            {
                typeof(HorizontalBackboneSegmentId), typeof(HorizontalBackboneRouteCell), typeof(HorizontalBackboneSegment),
                typeof(HorizontalBackbonePlan), typeof(HorizontalBackboneBuildError), typeof(HorizontalBackboneDiagnostics),
                typeof(HorizontalBackboneBuildResult), typeof(HorizontalBackboneRouter)
            };
            foreach (var type in types)
                Assert.That(type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Where(field => !field.IsLiteral && !field.IsInitOnly), Is.Empty, type.FullName);
            var assembly = typeof(HorizontalBackboneRouter).Assembly;
            Assert.That(assembly.GetReferencedAssemblies().Any(value => value.Name == "UnityEditor"), Is.False);
            var names = string.Join("|", assembly.GetTypes().Select(value => value.Name));
            foreach (var forbidden in new[]
            {
                "MandatoryRoutePass", "SectorRouteMaskAssigner",
                "OptionalReturnConnection", "OptionalClueAssigner", "OptionalRegionValidationOverlayWindow"
            })
                Assert.That(names, Does.Not.Contain(forbidden));
        }

        [Test]
        public void DiagnosticsFreezeZeroMutationRngGraphCsvAndViolations()
        {
            var diagnostics = Complete(reused).Diagnostics;
            Assert.That(new[] { diagnostics.ForbiddenReservedMiddleCellCount, diagnostics.WorldBoundsViolationCount,
                diagnostics.OpenUpDownCount, diagnostics.RouteGraphEdgeCount, diagnostics.GeneratedCsvRowCount,
                diagnostics.RngDrawCount, diagnostics.SourceMutationCount }, Is.EqualTo(new[] { 0, 0, 0, 0, 0, 0, 0 }));
        }

        private HorizontalBackboneBuildResult Complete(HorizontalBackboneRouter router)
        {
            var result = router.Build(tree, lookup, site, biome);
            Assert.That(result.Status, Is.EqualTo(HorizontalBackboneBuildStatus.Completed), FormatErrors(result));
            return result;
        }

        private string SourceSignature() =>
            tree.TreeEdgeCount + "|" + string.Join(",", tree.TreeEdges.Select(value => value.EdgeId.Value)) + "|" +
            lookup.Count + "|" + site.Sectors.Count + "|" + site.Reservations.Count + "|" +
            biome.Snapshot.Sectors.Count + "|" + biome.Snapshot.Patches.Count;

        private static string Signature(HorizontalBackboneBuildResult result) =>
            result.Status + "|" + string.Join(",", result.Errors.Select(value => value.Code + ":" + value.FirstId + ":" + value.SecondId + ":" + value.SectorIndex + ":" + value.Message)) + "|" +
            (result.Plan == null ? "null" : string.Join("/", result.Plan.Segments.Select(segment =>
                segment.SegmentId.Value + ":" + segment.SourceTreeEdgeId.Value + ":" + segment.TotalCost + ":" + segment.HorizontalDistance + ":" +
                string.Join(",", segment.Cells.Select(cell => cell.Ordinal + "@" + cell.Coord.X + ":" + cell.Coord.Y + ":" + cell.StepCost + ":" + (cell.RequiresVerticalGateway ? "G" : "-"))))) + "|" +
                result.Plan.TotalHorizontalCellCount + ":" + result.Plan.SameRowSegmentCount + ":" + result.Plan.GatewayPendingSegmentCount + ":" + result.Plan.TotalCost) + "|" +
            (result.Diagnostics == null ? "null" : result.Diagnostics.TreeEdgeCount + ":" + result.Diagnostics.SegmentCount + ":" + result.Diagnostics.TotalHorizontalCellCount + ":" + result.Diagnostics.RngDrawCount + ":" + result.Diagnostics.SourceMutationCount);

        private static string FormatErrors(HorizontalBackboneBuildResult result) => string.Join("\n", result.Errors.Select(value => value.Code + " " + value.Message));
        private static T GetField<T>(object target, Type type, string name) => (T)type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    }
}
