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
    [Category("MAP05_05")]
    public sealed class VerticalGatewayPlannerTests
    {
        private HorizontalBackbonePlan horizontalPlan;
        private MandatoryRouteMaskLookup lookup;
        private SiteReservationSnapshot site;
        private BiomePatchValidationPublication biome;
        private VerticalGatewayPlanner reused;
        private string expectedSignature;

        public static IEnumerable DeterminismCases
        {
            get
            {
                for (var index = 0; index < 100; index++)
                    yield return new TestCaseData(index).SetName("Build_DeterministicVerticalGateways_" + index.ToString("D3", CultureInfo.InvariantCulture));
            }
        }

        public static IEnumerable InvalidGatewayIds
        {
            get
            {
                yield return null;
                yield return string.Empty;
                yield return "VGW_0_X";
                yield return "vgw_00_X";
                yield return "VGW_000_X";
                yield return "VGW_00_";
                yield return "VGW_A0_X";
                yield return "VGW_0A_X";
                yield return "VGW_00_x";
                yield return "VGW_00_A-B";
                yield return "VGW00_X";
                yield return "VGW_00_A B";
                yield return "VGW_99_한글";
                yield return "VGW_00_A/B";
                yield return "VGW_00_A.B";
                yield return "VGW__00_X";
            }
        }

        public static IEnumerable ValidGatewayIds =>
            Enumerable.Range(0, 8).Select(index => "VGW_" + index.ToString("D2", CultureInfo.InvariantCulture) + "_TERM_A__TO__TERM_B");

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var terminalFixture = new MandatoryTerminalBuilderTests();
            terminalFixture.OneTimeSetUp();
            site = GetField<SiteReservationSnapshot>(terminalFixture, typeof(MandatoryTerminalBuilderTests), "site");
            biome = GetField<BiomePatchValidationPublication>(terminalFixture, typeof(MandatoryTerminalBuilderTests), "biome");
            var terminalResult = new MandatoryTerminalBuilder().Build(site, biome);
            Assert.That(terminalResult.Succeeded, Is.True);

            var buildStarter = typeof(MandatoryRouteMaskLookupBuilderTests).GetMethod("BuildStarter", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(buildStarter, Is.Not.Null);
            lookup = ((MandatoryRouteMaskLookupBuildResult)buildStarter.Invoke(null, null)).Lookup;
            var tree = new MandatoryConnectorTreeBuilder().Build(terminalResult.TerminalSet, lookup).Tree;
            horizontalPlan = new HorizontalBackboneRouter().Build(tree, lookup, site, biome).Plan;
            Assert.That(horizontalPlan, Is.Not.Null);
            reused = new VerticalGatewayPlanner();
            expectedSignature = Signature(Complete(reused));
        }

        [TestCaseSource(nameof(DeterminismCases))]
        public void Build_DeterministicVerticalGateways(int caseId)
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = (caseId & 1) == 0 ? CultureInfo.GetCultureInfo("en-US") : CultureInfo.GetCultureInfo("tr-TR");
                var planner = (caseId & 2) == 0 ? new VerticalGatewayPlanner() : reused;
                Assert.That(Signature(Complete(planner)), Is.EqualTo(expectedSignature));
            }
            finally { CultureInfo.CurrentCulture = previous; }
        }

        [TestCaseSource(nameof(InvalidGatewayIds))]
        public void GatewayIdRejectsNonCanonicalValues(string value)
        {
            Assert.That(VerticalGatewayId.TryCreate(value, out var parsed), Is.False);
            Assert.That(parsed.IsValid, Is.False);
            if (value == null) Assert.Throws<ArgumentNullException>(() => new VerticalGatewayId(value));
            else Assert.Throws<ArgumentException>(() => new VerticalGatewayId(value));
        }

        [TestCaseSource(nameof(ValidGatewayIds))]
        public void GatewayIdAcceptsCanonicalValues(string value)
        {
            Assert.That(VerticalGatewayId.TryCreate(value, out var parsed), Is.True);
            Assert.That(parsed.Value, Is.EqualTo(value));
            Assert.That(parsed, Is.EqualTo(new VerticalGatewayId(new string(value.ToCharArray()))));
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void Type4JunctionPreservesAllFourIndependentHorizontalCombinations(bool left, bool right)
        {
            var junction = new VerticalGatewayJunctionCell(new SectorCoord(3, 3), left, right);
            Assert.That(new[] { junction.OpensLeft, junction.OpensRight }, Is.EqualTo(new[] { left, right }));
            Assert.That(junction.OpensUp && junction.OpensDown, Is.True);
            Assert.That(junction.RouteType, Is.EqualTo(4));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        [TestCase(8)]
        public void AnchorAcceptsExactFiniteCostLevels(int cost)
        {
            var anchor = new VerticalGatewayAnchor(new SectorCoord(1, 2), true, true, false, true, false, cost);
            Assert.That(anchor.StepCost, Is.EqualTo(cost));
            Assert.That(anchor.OpensDown && !anchor.OpensUp, Is.True);
        }

        [Test]
        public void StarterBuildPublishesExactPairsAnchorsAndType4Count()
        {
            var result = Complete(reused);
            Assert.That(result.Plan.GatewayPairCount, Is.EqualTo(4));
            Assert.That(result.Plan.PendingSegmentCount, Is.EqualTo(4));
            Assert.That(new[] { result.Plan.UpperAnchorCount, result.Plan.LowerAnchorCount }, Is.EqualTo(new[] { 4, 4 }));
            Assert.That(result.Plan.Type4JunctionCellCount, Is.EqualTo(11));
            Assert.That(result.Plan.TotalVerticalSpanCellCount, Is.EqualTo(19));
        }

        [Test]
        public void PlanPreservesAllFourExactSourceReferences()
        {
            var plan = Complete(reused).Plan;
            Assert.That(plan.SourceHorizontalPlan, Is.SameAs(horizontalPlan));
            Assert.That(plan.SourceRouteMaskLookup, Is.SameAs(lookup));
            Assert.That(plan.SourceSiteSnapshot, Is.SameAs(site));
            Assert.That(plan.SourceBiomePublication, Is.SameAs(biome));
        }

        [Test]
        public void GatewayIdsUsePendingOrderAndSourceEdgeSuffix()
        {
            var pairs = Complete(reused).Plan.GatewayPairs;
            for (var index = 0; index < pairs.Count; index++)
            {
                Assert.That(horizontalPlan.TryGetSegment(pairs[index].SourceSegmentId, out var segment), Is.True);
                Assert.That(pairs[index].GatewayId.Value, Is.EqualTo("VGW_" + index.ToString("D2", CultureInfo.InvariantCulture) + "_" + segment.SourceTreeEdgeId.Value.Substring(8)));
            }
        }

        [Test]
        public void EveryPairHasSameColumnType2DownAndType3UpAnchors()
        {
            foreach (var pair in Complete(reused).Plan.GatewayPairs)
            {
                Assert.That(pair.Upper.Coord.X, Is.EqualTo(pair.Lower.Coord.X));
                Assert.That(pair.Upper.Coord.Y, Is.GreaterThan(pair.Lower.Coord.Y));
                Assert.That(pair.Upper.IsUpperAnchor && pair.Upper.OpensDown && !pair.Upper.OpensUp, Is.True);
                Assert.That(!pair.Lower.IsUpperAnchor && pair.Lower.OpensUp && !pair.Lower.OpensDown, Is.True);
            }
        }

        [Test]
        public void SpansAreInclusiveOrderedAndJunctionsCoverEveryInteriorCell()
        {
            foreach (var pair in Complete(reused).Plan.GatewayPairs)
            {
                Assert.That(pair.SpanCells.Count, Is.EqualTo(pair.VerticalDistance + 1));
                Assert.That(pair.Type4JunctionCells.Count, Is.EqualTo(pair.VerticalDistance - 1));
                for (var index = 0; index < pair.SpanCells.Count; index++)
                    Assert.That(pair.SpanCells[index], Is.EqualTo(new SectorCoord(pair.GatewayColumn, pair.Upper.Coord.Y - index)));
                Assert.That(pair.Type4JunctionCells.Select(value => value.Coord), Is.EqualTo(pair.SpanCells.Skip(1).Take(pair.SpanCells.Count - 2)));
            }
        }

        [Test]
        public void EveryJunctionGuaranteesUpDownAndPreservesComputedHorizontalAdjacency()
        {
            foreach (var junction in Complete(reused).Plan.GatewayPairs.SelectMany(value => value.Type4JunctionCells))
            {
                Assert.That(junction.OpensUp && junction.OpensDown, Is.True);
                Assert.That(junction.RouteType, Is.EqualTo(4));
                Assert.That(junction.OpensLeft, Is.EqualTo(HasAdjacency(junction.Coord, -1)));
                Assert.That(junction.OpensRight, Is.EqualTo(HasAdjacency(junction.Coord, 1)));
            }
        }

        [Test]
        public void SameRowSegmentsAreCarriedThroughWithoutPairs()
        {
            var sameRows = horizontalPlan.Segments.Where(value => value.IsSameRow).ToList();
            Assert.That(sameRows, Has.Count.EqualTo(2));
            foreach (var segment in sameRows)
                Assert.That(Complete(reused).Plan.GetPairsForSegment(segment.SegmentId), Is.Empty);
        }

        [Test]
        public void PairAndSegmentLookupsReturnExactInstances()
        {
            var plan = Complete(reused).Plan;
            foreach (var pair in plan.GatewayPairs)
            {
                Assert.That(plan.TryGetPair(pair.GatewayId, out var found), Is.True);
                Assert.That(found, Is.SameAs(pair));
                Assert.That(plan.GetPairsForSegment(pair.SourceSegmentId).Single(), Is.SameAs(pair));
            }
            Assert.That(plan.TryGetPair(new VerticalGatewayId("VGW_99_UNKNOWN"), out _), Is.False);
        }

        [Test]
        public void PlanSpanAndJunctionCollectionsAreDefensiveReadOnlyViews()
        {
            var plan = Complete(reused).Plan;
            var pair = plan.GatewayPairs[0];
            Assert.Throws<NotSupportedException>(() => ((IList<VerticalGatewayPair>)plan.GatewayPairs).Add(pair));
            Assert.Throws<NotSupportedException>(() => ((IList<SectorCoord>)pair.SpanCells).Add(pair.Upper.Coord));
            Assert.Throws<NotSupportedException>(() => ((IList<VerticalGatewayJunctionCell>)pair.Type4JunctionCells).Add(pair.Type4JunctionCells[0]));
        }

        [Test]
        public void NullInputsAccumulateFourSortedErrorsAtomically()
        {
            var result = new VerticalGatewayPlanner().Build(null, null, null, null);
            Assert.That(result.Status, Is.EqualTo(VerticalGatewayBuildStatus.InvalidInput));
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.Diagnostics, Is.Null);
            Assert.That(result.Errors, Has.Count.EqualTo(4));
            Assert.That(result.Errors.All(value => value.Code == VerticalGatewayBuildErrorCode.MissingInput), Is.True);
            Assert.That(result.RetryRequired, Is.False);
        }

        [Test]
        public void MissingAnySingleInputPublishesNothing()
        {
            Assert.That(reused.Build(null, lookup, site, biome).Succeeded, Is.False);
            Assert.That(reused.Build(horizontalPlan, null, site, biome).Succeeded, Is.False);
            Assert.That(reused.Build(horizontalPlan, lookup, null, biome).Succeeded, Is.False);
            Assert.That(reused.Build(horizontalPlan, lookup, site, null).Succeeded, Is.False);
        }

        [Test]
        public void AnchorRejectsWrongOrientationReservedMiddleBoundsAndInfiniteCost()
        {
            var coord = new SectorCoord(1, 1);
            Assert.Throws<ArgumentException>(() => new VerticalGatewayAnchor(coord, true, false, true, true, false, 1));
            Assert.Throws<ArgumentException>(() => new VerticalGatewayAnchor(coord, false, false, true, false, true, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new VerticalGatewayAnchor(new SectorCoord(-1, 0), true, true, false, true, false, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new VerticalGatewayAnchor(coord, true, true, false, true, false, int.MaxValue));
        }

        [Test]
        public void PairRejectsColumnMisalignmentAndWrongOrdering()
        {
            var upper = new VerticalGatewayAnchor(new SectorCoord(2, 3), true, true, false, true, false, 1);
            var wrongColumn = new VerticalGatewayAnchor(new SectorCoord(3, 1), false, false, true, true, false, 1);
            var wrongOrder = new VerticalGatewayAnchor(new SectorCoord(2, 4), false, false, true, true, false, 1);
            Assert.Throws<ArgumentException>(() => new VerticalGatewayPair(new VerticalGatewayId("VGW_00_A"), new HorizontalBackboneSegmentId("HSEG_00_A"), upper, wrongColumn, 3, false, Array.Empty<SectorCoord>(), Array.Empty<VerticalGatewayJunctionCell>()));
            Assert.Throws<ArgumentException>(() => new VerticalGatewayPair(new VerticalGatewayId("VGW_00_A"), new HorizontalBackboneSegmentId("HSEG_00_A"), upper, wrongOrder, 3, false, Array.Empty<SectorCoord>(), Array.Empty<VerticalGatewayJunctionCell>()));
        }

        [Test]
        public void JunctionRejectsWorldBoundsAndHasStableValueSemantics()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new VerticalGatewayJunctionCell(new SectorCoord(13, 0), false, false));
            var first = new VerticalGatewayJunctionCell(new SectorCoord(2, 2), true, false);
            var same = new VerticalGatewayJunctionCell(new SectorCoord(2, 2), true, false);
            Assert.That(first == same && first.GetHashCode() == same.GetHashCode(), Is.True);
        }

        [Test]
        public void GatewayIdHasStableOrdinalEqualityOrderHashAndDefault()
        {
            var first = new VerticalGatewayId("VGW_00_A");
            var same = new VerticalGatewayId(new string(first.Value.ToCharArray()));
            var later = new VerticalGatewayId("VGW_01_A");
            Assert.That(first == same && first.GetHashCode() == same.GetHashCode(), Is.True);
            Assert.That(first.CompareTo(later), Is.LessThan(0));
            Assert.That(default(VerticalGatewayId).IsValid, Is.False);
        }

        [Test]
        public void CostsAreCheckedSumsOfFiniteLevels()
        {
            var plan = Complete(reused).Plan;
            Assert.That(plan.GatewayPairs.All(value => value.TotalCost > 0), Is.True);
            Assert.That(plan.TotalCost, Is.EqualTo(plan.GatewayPairs.Sum(value => value.TotalCost)));
            Assert.That(plan.GatewayPairs.SelectMany(value => new[] { value.Upper.StepCost, value.Lower.StepCost }).All(value => value == 1 || value == 2 || value == 4 || value == 8), Is.True);
        }

        [Test]
        public void ReservationFootprintsNeverAppearAsType4MiddleCells()
        {
            var plan = Complete(reused).Plan;
            Assert.That(plan.GatewayPairs.SelectMany(value => value.Type4JunctionCells).All(value => !site.GetSector(value.Coord).IsReserved), Is.True);
            Assert.That(plan.GatewayPairs.SelectMany(value => new[] { value.Upper, value.Lower }).Where(value => value.IsReserved).All(value => value.IsEndpointAdapter), Is.True);
        }

        [Test]
        public void AllSpanCellsStayInsideExactThirteenByThirteenWorld()
        {
            Assert.That(Complete(reused).Plan.GatewayPairs.SelectMany(value => value.SpanCells).All(value =>
                value.X >= 0 && value.X < WorldGenConstants.SectorColumns && value.Y >= 0 && value.Y < WorldGenConstants.SectorRows), Is.True);
        }

        [Test]
        public void Type4ExpressibleUpDownIsNeverReportedAsConflict()
        {
            var plan = Complete(reused).Plan;
            Assert.That(plan.ConflictPendingCount, Is.Zero);
            Assert.That(plan.GatewayPairs.All(value => !value.RequiresUpDownConflictResolution), Is.True);
        }

        [Test]
        public void DiagnosticsFreezeZeroMutationRngGraphCsvMaskWritesAndViolations()
        {
            var diagnostics = Complete(reused).Diagnostics;
            Assert.That(new[] { diagnostics.ReservedMiddleCellCount, diagnostics.WorldBoundsViolationCount,
                diagnostics.ConflictPendingCount, diagnostics.RouteGraphEdgeCount, diagnostics.GeneratedCsvRowCount,
                diagnostics.SectorRouteMaskWriteCount, diagnostics.RngDrawCount, diagnostics.SourceMutationCount },
                Is.EqualTo(new[] { 0, 0, 0, 0, 0, 0, 0, 0 }));
            Assert.That(diagnostics.OpenUpCount, Is.EqualTo(diagnostics.LowerAnchorCount + diagnostics.Type4JunctionCellCount));
            Assert.That(diagnostics.OpenDownCount, Is.EqualTo(diagnostics.UpperAnchorCount + diagnostics.Type4JunctionCellCount));
        }

        [Test]
        public void SourceArtifactsRemainObservablyUnchanged()
        {
            var before = SourceSignature();
            Complete(reused);
            Assert.That(SourceSignature(), Is.EqualTo(before));
        }

        [Test]
        public void FreshReusedAndParallelPlannersHaveOneSignature()
        {
            var values = new string[12];
            Parallel.For(0, values.Length, index => values[index] = Signature(Complete((index & 1) == 0 ? new VerticalGatewayPlanner() : reused)));
            Assert.That(values.Distinct().Single(), Is.EqualTo(expectedSignature));
        }

        [Test]
        public void RuntimeSurfaceHasNoMutableStaticUnityEditorOrMap06_03PlusSymbols()
        {
            var types = new[]
            {
                typeof(VerticalGatewayId), typeof(VerticalGatewayAnchor), typeof(VerticalGatewayJunctionCell), typeof(VerticalGatewayPair),
                typeof(VerticalGatewayPlan), typeof(VerticalGatewayBuildError), typeof(VerticalGatewayDiagnostics),
                typeof(VerticalGatewayBuildResult), typeof(VerticalGatewayPlanner)
            };
            foreach (var type in types)
                Assert.That(type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Where(field => !field.IsLiteral && !field.IsInitOnly), Is.Empty, type.FullName);
            var assembly = typeof(VerticalGatewayPlanner).Assembly;
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
        public void CompletedResultPublishesNoErrorsAndNeverRequestsRetry()
        {
            var result = Complete(reused);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RetryRequired, Is.False);
        }

        private VerticalGatewayBuildResult Complete(VerticalGatewayPlanner planner)
        {
            var result = planner.Build(horizontalPlan, lookup, site, biome);
            Assert.That(result.Status, Is.EqualTo(VerticalGatewayBuildStatus.Completed), FormatErrors(result));
            return result;
        }

        private bool HasAdjacency(SectorCoord coord, int deltaX)
        {
            var neighbor = new SectorCoord(coord.X + deltaX, coord.Y);
            return horizontalPlan.Segments.Any(segment => segment.Cells.Any(cell => cell.Coord == coord) && segment.Cells.Any(cell => cell.Coord == neighbor));
        }

        private string SourceSignature() =>
            horizontalPlan.SegmentCount + "|" + horizontalPlan.TotalHorizontalCellCount + "|" + horizontalPlan.TotalCost + "|" +
            lookup.Count + "|" + site.Sectors.Count + "|" + site.Reservations.Count + "|" +
            biome.Snapshot.Sectors.Count + "|" + biome.Snapshot.Patches.Count;

        private static string Signature(VerticalGatewayBuildResult result) =>
            result.Status + "|" + string.Join(",", result.Errors.Select(value => value.Code + ":" + value.FirstId + ":" + value.SecondId + ":" + value.SectorIndex + ":" + value.Message)) + "|" +
            (result.Plan == null ? "null" : string.Join("/", result.Plan.GatewayPairs.Select(pair =>
                pair.GatewayId.Value + ":" + pair.SourceSegmentId.Value + ":" + pair.GatewayColumn + ":" + pair.VerticalDistance + ":" + pair.TotalCost + ":" +
                string.Join(",", pair.SpanCells.Select(value => value.X + ":" + value.Y)) + ":" +
                string.Join(",", pair.Type4JunctionCells.Select(value => value.Coord.X + ":" + value.Coord.Y + ":" + (value.OpensLeft ? "L" : "-") + (value.OpensRight ? "R" : "-") + "UD")))) + "|" +
                result.Plan.Type4JunctionCellCount + ":" + result.Plan.TotalVerticalSpanCellCount + ":" + result.Plan.TotalCost) + "|" +
            (result.Diagnostics == null ? "null" : result.Diagnostics.HorizontalSegmentCount + ":" + result.Diagnostics.PendingSegmentCount + ":" + result.Diagnostics.GatewayPairCount + ":" + result.Diagnostics.Type4JunctionCellCount + ":" + result.Diagnostics.RngDrawCount + ":" + result.Diagnostics.SourceMutationCount);

        private static string FormatErrors(VerticalGatewayBuildResult result) => string.Join("\n", result.Errors.Select(value => value.Code + " " + value.Message));
        private static T GetField<T>(object target, Type type, string name) => (T)type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    }
}
