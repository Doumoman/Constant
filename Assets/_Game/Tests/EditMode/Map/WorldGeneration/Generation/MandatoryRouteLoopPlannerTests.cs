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
    [Category("MAP05_07")]
    public sealed class MandatoryRouteLoopPlannerTests
    {
        private MandatoryRouteTerminalSet terminalSet;
        private MandatoryConnectorTree tree;
        private HorizontalBackbonePlan horizontal;
        private VerticalGatewayPlan vertical;
        private UpDownConflictResolutionPlan conflicts;
        private MandatoryRouteMaskLookup lookup;
        private SiteReservationSnapshot site;
        private BiomePatchValidationPublication biome;
        private MandatoryRouteLoopPlanner reused;
        private string expectedSignature;
        private string sourceSignature;

        public static IEnumerable DeterminismCases
        {
            get
            {
                for (var index = 0; index < 180; index++)
                    yield return new TestCaseData(index).SetName("Build_DeterministicMandatoryLoops_" + index.ToString("D3", CultureInfo.InvariantCulture));
            }
        }

        public static IEnumerable InvalidIds => new[]
        {
            null, string.Empty, "LOOP_0_X", "loop_00_X", "LOOP_000_X", "LOOP_00_", "LOOP_A0_X", "LOOP_0A_X",
            "LOOP_00_x", "LOOP_00_A-B", "LOOP00_X", "LOOP_00_A B", "LOOP_99_한글", "LOOP_00_A/B", "LOOP_00_A.B"
        };

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var terminalFixture = new MandatoryTerminalBuilderTests();
            terminalFixture.OneTimeSetUp();
            site = GetField<SiteReservationSnapshot>(terminalFixture, typeof(MandatoryTerminalBuilderTests), "site");
            biome = GetField<BiomePatchValidationPublication>(terminalFixture, typeof(MandatoryTerminalBuilderTests), "biome");
            var terminalResult = new MandatoryTerminalBuilder().Build(site, biome);
            Assert.That(terminalResult.Succeeded, Is.True);
            terminalSet = terminalResult.TerminalSet;
            lookup = BuildStarterLookup();
            tree = new MandatoryConnectorTreeBuilder().Build(terminalSet, lookup).Tree;
            horizontal = new HorizontalBackboneRouter().Build(tree, lookup, site, biome).Plan;
            vertical = new VerticalGatewayPlanner().Build(horizontal, lookup, site, biome).Plan;
            conflicts = new UpDownConflictResolver().Build(vertical, lookup, site, biome).Plan;
            Assert.That(conflicts, Is.Not.Null);
            reused = new MandatoryRouteLoopPlanner();
            var baseline = Complete(reused);
            expectedSignature = Signature(baseline);
            sourceSignature = SourceSignature();
        }

        [TestCaseSource(nameof(DeterminismCases))]
        public void Build_DeterministicMandatoryLoops(int caseId)
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = (caseId & 1) == 0 ? CultureInfo.GetCultureInfo("en-US") : CultureInfo.GetCultureInfo("tr-TR");
                var planner = (caseId & 2) == 0 ? new MandatoryRouteLoopPlanner() : reused;
                var result = Complete(planner);
                Assert.That(Signature(result), Is.EqualTo(expectedSignature));
                Assert.That(SourceSignature(), Is.EqualTo(sourceSignature));
                Assert.That(result.Diagnostics.RngDrawCount + result.Diagnostics.FileWriteCount + result.Diagnostics.GraphWriteCount +
                    result.Diagnostics.GeneratedCsvRowCount + result.Diagnostics.RouteMaskWriteCount + result.Diagnostics.SourceMutationCount, Is.Zero);
            }
            finally { CultureInfo.CurrentCulture = previous; }
        }

        [TestCaseSource(nameof(InvalidIds))]
        public void LoopIdRejectsNonCanonicalValues(string value)
        {
            Assert.That(MandatoryRouteLoopId.TryCreate(value, out var parsed), Is.False);
            Assert.That(parsed.IsValid, Is.False);
            if (value == null) Assert.Throws<ArgumentNullException>(() => new MandatoryRouteLoopId(value));
            else Assert.Throws<ArgumentException>(() => new MandatoryRouteLoopId(value));
        }

        [TestCase("LOOP_00_ALPHA")]
        [TestCase("LOOP_09_TERM_A__TO__TERM_B")]
        [TestCase("LOOP_99_Z9")]
        public void LoopIdHasOrdinalValueEqualityOrderAndHash(string value)
        {
            var first = new MandatoryRouteLoopId(value);
            var copy = new MandatoryRouteLoopId(new string(value.ToCharArray()));
            Assert.That(first, Is.EqualTo(copy));
            Assert.That(first.CompareTo(copy), Is.Zero);
            Assert.That(first.GetHashCode(), Is.EqualTo(copy.GetHashCode()));
            Assert.That(first.Value, Is.EqualTo(value));
        }

        [Test]
        public void StarterPublishesAtLeastTwoIndependentLoopsWithDistinctPairs()
        {
            var plan = Complete(reused).Plan;
            Assert.That(MandatoryRouteLoopPlan.MinimumLoopCount, Is.EqualTo(2));
            Assert.That(plan.LoopCount, Is.GreaterThanOrEqualTo(2));
            Assert.That(plan.IndependentLoopCount, Is.GreaterThanOrEqualTo(2));
            Assert.That(plan.MeetsMinimum, Is.True);
            Assert.That(plan.Loops.Select(value => Pair(value.StartTerminalId, value.EndTerminalId)).Distinct().Count(), Is.EqualTo(plan.LoopCount));
            Assert.That(plan.Loops.Select(value => value.SourceConnectorEdgeId).Distinct().Count(), Is.EqualTo(plan.LoopCount));
            Assert.That(plan.Loops.All(value => value.IsIndependent && !string.IsNullOrEmpty(value.IndependenceWitness)), Is.True);
        }

        [Test]
        public void PlanPreservesExactFiveSourceArtifactIdentities()
        {
            var plan = Complete(reused).Plan;
            Assert.That(plan.SourceTerminalSet, Is.SameAs(terminalSet));
            Assert.That(plan.SourceConnectorTree, Is.SameAs(tree));
            Assert.That(plan.SourceHorizontalBackbonePlan, Is.SameAs(horizontal));
            Assert.That(plan.SourceVerticalGatewayPlan, Is.SameAs(vertical));
            Assert.That(plan.SourceConflictResolutionPlan, Is.SameAs(conflicts));
        }

        [Test]
        public void CandidateAndLoopLookupPreservePublishedInstances()
        {
            var plan = Complete(reused).Plan;
            foreach (var candidate in plan.Candidates)
            {
                Assert.That(plan.TryGetCandidate(candidate.LoopId, out var found), Is.True);
                Assert.That(found, Is.SameAs(candidate));
            }
            foreach (var loop in plan.Loops)
            {
                Assert.That(plan.TryGetLoop(loop.LoopId, out var found), Is.True);
                Assert.That(found, Is.SameAs(loop));
                Assert.That(loop.Candidate, Is.SameAs(plan.Candidates.Single(value => value.LoopId == loop.LoopId)));
            }
        }

        [Test]
        public void StarterCandidatesStayInBoundsAndAvoidReservedInactiveAndMandatoryIntrusions()
        {
            var result = Complete(reused);
            Assert.That(result.Plan.Candidates, Is.Not.Empty);
            Assert.That(result.Plan.Candidates.All(value => value.IsInsideWorld), Is.True);
            Assert.That(result.Plan.Candidates.All(value => !value.HasReservationIntrusion && !value.HasInactiveIntrusion && !value.HasMandatoryPathIntrusion), Is.True);
            Assert.That(result.Plan.Loops.SelectMany(value => value.InclusiveOrderedCells.Skip(1).Take(value.InclusiveOrderedCells.Count - 2))
                .All(value => !site.GetSector(value).IsReserved), Is.True);
        }

        [Test]
        public void CandidateOrderUsesCostCoverageOverlapFirstIndexAndId()
        {
            var values = new[]
            {
                Synthetic("LOOP_03_ORDER_D", 2, 3, new[] { new SectorCoord(5, 5), new SectorCoord(6, 5) }, 4, 1, false, false, false, true),
                Synthetic("LOOP_02_ORDER_C", 2, 4, new[] { new SectorCoord(4, 4), new SectorCoord(5, 4), new SectorCoord(6, 4) }, 3, 0, false, false, false, true),
                Synthetic("LOOP_01_ORDER_B", 0, 2, new[] { new SectorCoord(2, 2), new SectorCoord(3, 2), new SectorCoord(4, 2) }, 3, 0, false, false, false, true),
                Synthetic("LOOP_00_ORDER_A", 0, 1, new[] { new SectorCoord(1, 1), new SectorCoord(2, 1), new SectorCoord(3, 1), new SectorCoord(4, 1) }, 3, 0, false, false, false, true)
            };
            var result = Complete(new MandatoryRouteLoopPlanner(), values);
            Assert.That(result.Plan.Candidates.Select(value => value.LoopId.Value), Is.EqualTo(new[] { "LOOP_00_ORDER_A", "LOOP_01_ORDER_B", "LOOP_02_ORDER_C", "LOOP_03_ORDER_D" }));
        }

        [Test]
        public void SyntheticRejectionsAreDiagnosedWithoutPublishingInvalidLoops()
        {
            var values = new[]
            {
                Synthetic("LOOP_00_BOUNDS", 0, 1, new[] { new SectorCoord(-1, 0), new SectorCoord(0, 0) }, 2, 0, false, false, false, true),
                Synthetic("LOOP_01_RESERVED", 1, 2, new[] { new SectorCoord(0, 1), new SectorCoord(1, 1) }, 2, 0, true, false, false, true),
                Synthetic("LOOP_02_INACTIVE", 2, 3, new[] { new SectorCoord(0, 2), new SectorCoord(1, 2) }, 2, 0, false, true, false, true),
                Synthetic("LOOP_03_INTRUSION", 3, 4, new[] { new SectorCoord(0, 3), new SectorCoord(1, 3) }, 2, 0, false, false, true, true),
                Synthetic("LOOP_04_DEPENDENT", 4, 5, new[] { new SectorCoord(0, 4), new SectorCoord(1, 4) }, 2, 0, false, false, false, false)
            };
            var result = Complete(new MandatoryRouteLoopPlanner(), values);
            Assert.That(result.Plan.Loops, Is.Empty);
            Assert.That(result.Diagnostics.BoundsRejectedCount, Is.EqualTo(1));
            Assert.That(result.Diagnostics.ReservationRejectedCount, Is.EqualTo(1));
            Assert.That(result.Diagnostics.InactiveRejectedCount, Is.EqualTo(1));
            Assert.That(result.Diagnostics.MandatoryPathRejectedCount, Is.EqualTo(1));
            Assert.That(result.Diagnostics.UnresolvedLoopCount, Is.EqualTo(2));
        }

        [Test]
        public void SharedInteriorRejectsSecondSyntheticLoopAndReportsUnresolvedMinimum()
        {
            var first = Synthetic("LOOP_00_OVERLAP_A", 0, 1, new[] { new SectorCoord(0, 0), new SectorCoord(1, 0), new SectorCoord(2, 0) }, 3, 0, false, false, false, true);
            var second = Synthetic("LOOP_01_OVERLAP_B", 2, 3, new[] { new SectorCoord(0, 1), new SectorCoord(1, 1), new SectorCoord(1, 0), new SectorCoord(2, 0) }, 4, 0, false, false, false, true);
            var result = Complete(new MandatoryRouteLoopPlanner(), new[] { first, second });
            Assert.That(result.Plan.LoopCount, Is.EqualTo(1));
            Assert.That(result.Diagnostics.OverlapRejectedCount, Is.EqualTo(1));
            Assert.That(result.Diagnostics.UnresolvedLoopCount, Is.EqualTo(1));
        }

        [Test]
        public void DuplicateLoopIdReturnsInvalidInputWithoutPlan()
        {
            var first = Synthetic("LOOP_00_DUPLICATE", 0, 1, new[] { new SectorCoord(0, 0), new SectorCoord(1, 0) }, 2, 0, false, false, false, true);
            var second = Synthetic("LOOP_00_DUPLICATE", 2, 3, new[] { new SectorCoord(0, 2), new SectorCoord(1, 2) }, 2, 0, false, false, false, true);
            var result = reused.Build(terminalSet, tree, horizontal, vertical, conflicts, new[] { first, second });
            Assert.That(result.Status, Is.EqualTo(MandatoryRouteLoopBuildStatus.InvalidInput));
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(MandatoryRouteLoopBuildErrorCode.DuplicateLoopId));
        }

        [Test]
        public void MissingInputsReturnAllFiveDeterministicErrors()
        {
            var result = reused.Build(null, null, null, null, null);
            Assert.That(result.Status, Is.EqualTo(MandatoryRouteLoopBuildStatus.InvalidInput));
            Assert.That(result.Errors.Count, Is.EqualTo(5));
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.Diagnostics, Is.Null);
            Assert.That(result.RetryRequired, Is.False);
        }

        [Test]
        public void SourceIdentityMismatchIsRejected()
        {
            var otherTree = new MandatoryConnectorTreeBuilder().Build(terminalSet, lookup).Tree;
            Assert.That(otherTree, Is.Not.SameAs(tree));
            var result = reused.Build(terminalSet, otherTree, horizontal, vertical, conflicts);
            Assert.That(result.Status, Is.EqualTo(MandatoryRouteLoopBuildStatus.InvalidInput));
            Assert.That(result.Errors.Single().Code, Is.EqualTo(MandatoryRouteLoopBuildErrorCode.SourceIdentityMismatch));
        }

        [Test]
        public void Type4MandatoryUpDownAndAllFourLeftRightCombinationsRemainUnchanged()
        {
            var before = Type4Signature();
            var result = Complete(reused);
            Assert.That(Type4Signature(), Is.EqualTo(before));
            Assert.That(vertical.GatewayPairs.SelectMany(value => value.Type4JunctionCells).All(value => value.OpensUp && value.OpensDown), Is.True);
            Assert.That(vertical.GatewayPairs.SelectMany(value => value.Type4JunctionCells).Select(value => (value.OpensLeft ? "L" : "-") + (value.OpensRight ? "R" : "-")).Distinct(), Is.Not.Empty);
            Assert.That(result.Diagnostics.RouteMaskWriteCount, Is.Zero);
        }

        [Test]
        public void CandidateAndLoopCollectionsAreReadOnly()
        {
            var plan = Complete(reused).Plan;
            Assert.Throws<NotSupportedException>(() => ((IList<MandatoryRouteLoopCandidate>)plan.Candidates).Add(plan.Candidates[0]));
            Assert.Throws<NotSupportedException>(() => ((IList<MandatoryRouteLoop>)plan.Loops).Add(plan.Loops[0]));
            Assert.Throws<NotSupportedException>(() => ((IList<SectorCoord>)plan.Loops[0].InclusiveOrderedCells).Add(new SectorCoord(0, 0)));
            Assert.Throws<NotSupportedException>(() => ((IList<VerticalGatewayJunctionCell>)plan.Candidates[0].SourceType4Junctions).Add(vertical.GatewayPairs[0].Type4JunctionCells[0]));
        }

        [Test]
        public void FreshReusedAndParallelBuildsAreIdenticalAndMutationFree()
        {
            var values = new string[12];
            Parallel.For(0, values.Length, index => values[index] = Signature(Complete((index & 1) == 0 ? new MandatoryRouteLoopPlanner() : reused)));
            Assert.That(values.Distinct().Single(), Is.EqualTo(expectedSignature));
            Assert.That(SourceSignature(), Is.EqualTo(sourceSignature));
        }

        [Test]
        public void RuntimeSurfaceHasNoMutableStaticUnityEditorOrMap05_11PlusSymbols()
        {
            var types = new[]
            {
                typeof(MandatoryRouteLoopId), typeof(MandatoryRouteLoopCandidate), typeof(MandatoryRouteLoop),
                typeof(MandatoryRouteLoopPlan), typeof(MandatoryRouteLoopBuildError), typeof(MandatoryRouteLoopDiagnostics),
                typeof(MandatoryRouteLoopBuildResult), typeof(MandatoryRouteLoopPlanner)
            };
            foreach (var type in types)
                Assert.That(type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(field => !field.IsLiteral && !field.IsInitOnly), Is.Empty, type.FullName);
            var assembly = typeof(MandatoryRouteLoopPlanner).Assembly;
            Assert.That(assembly.GetReferencedAssemblies().Any(value => value.Name == "UnityEditor"), Is.False);
            var names = string.Join("|", assembly.GetTypes().Select(value => value.Name));
            foreach (var forbidden in new[] { "MandatoryRoutePass", "SectorRouteMaskAssigner" })
                Assert.That(names, Does.Not.Contain(forbidden));
        }

        private MandatoryRouteLoopBuildResult Complete(MandatoryRouteLoopPlanner planner)
        {
            var result = planner.Build(terminalSet, tree, horizontal, vertical, conflicts);
            Assert.That(result.Status, Is.EqualTo(MandatoryRouteLoopBuildStatus.Completed), FormatErrors(result));
            return result;
        }

        private MandatoryRouteLoopBuildResult Complete(MandatoryRouteLoopPlanner planner, IEnumerable<MandatoryRouteLoopCandidate> candidates)
        {
            var result = planner.Build(terminalSet, tree, horizontal, vertical, conflicts, candidates);
            Assert.That(result.Status, Is.EqualTo(MandatoryRouteLoopBuildStatus.Completed), FormatErrors(result));
            return result;
        }

        private MandatoryRouteLoopCandidate Synthetic(
            string id, int start, int end, IEnumerable<SectorCoord> cells, int cost, int shared,
            bool reserved, bool inactive, bool mandatoryIntrusion, bool independent)
        {
            return new MandatoryRouteLoopCandidate(
                new MandatoryRouteLoopId(id), terminalSet.Terminals[start].TerminalId, terminalSet.Terminals[end].TerminalId,
                tree.CandidateEdges[(start + end) % tree.CandidateEdges.Count].EdgeId, cells,
                Array.Empty<HorizontalBackboneSegmentId>(), Array.Empty<VerticalGatewayId>(),
                vertical.GatewayPairs.SelectMany(value => value.Type4JunctionCells),
                "SYNTHETIC_SITE", "SYNTHETIC_BIOME", cost, shared, reserved, inactive, mandatoryIntrusion, independent);
        }

        private string SourceSignature() =>
            terminalSet.TerminalCount + "|" + tree.TreeEdgeCount + "|" + tree.TotalTreeCost + "|" + horizontal.SegmentCount + "|" + horizontal.TotalCost + "|" +
            vertical.GatewayPairCount + "|" + vertical.Type4JunctionCellCount + "|" + vertical.TotalCost + "|" +
            conflicts.CandidateCount + "|" + conflicts.Resolutions.Count + "|" + site.Sectors.Count + "|" + biome.Snapshot.Sectors.Count;

        private string Type4Signature() => string.Join("/", vertical.GatewayPairs.SelectMany(value => value.Type4JunctionCells)
            .Select(value => value.Coord.X + ":" + value.Coord.Y + ":" + (value.OpensLeft ? "L" : "-") + (value.OpensRight ? "R" : "-") + "UD"));

        private static string Signature(MandatoryRouteLoopBuildResult result) =>
            result.Status + "|" + string.Join(",", result.Errors.Select(value => value.Code + ":" + value.SourceId + ":" + value.Message)) + "|" +
            (result.Plan == null ? "null" : string.Join("/", result.Plan.Candidates.Select(value => value.LoopId.Value + ":" + value.StartTerminalId.Value + ":" +
                value.EndTerminalId.Value + ":" + value.CheckedTotalCost + ":" + value.UniqueCellCount + ":" + value.SharedCellCount + ":" +
                string.Join(",", value.OrderedCells.Select(cell => cell.X + ":" + cell.Y)))) + "|" +
                string.Join("/", result.Plan.Loops.Select(value => value.LoopId.Value + ":" + value.IndependenceWitness)) + "|" +
                result.Plan.LoopCount + ":" + result.Plan.IndependentLoopCount + ":" + result.Plan.SharedCellCount + ":" + result.Plan.TotalCost) + "|" +
            (result.Diagnostics == null ? "null" : result.Diagnostics.CandidateCount + ":" + result.Diagnostics.EligibleCandidateCount + ":" +
                result.Diagnostics.AcceptedLoopCount + ":" + result.Diagnostics.IndependentLoopCount + ":" + result.Diagnostics.OverlapRejectedCount + ":" +
                result.Diagnostics.UnresolvedLoopCount + ":" + result.Diagnostics.RngDrawCount + ":" + result.Diagnostics.SourceMutationCount);

        private static string Pair(MandatoryRouteTerminalId left, MandatoryRouteTerminalId right) =>
            left.CompareTo(right) <= 0 ? left.Value + "|" + right.Value : right.Value + "|" + left.Value;

        private static MandatoryRouteMaskLookup BuildStarterLookup()
        {
            var method = typeof(MandatoryRouteMaskLookupBuilderTests).GetMethod("BuildStarter", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return ((MandatoryRouteMaskLookupBuildResult)method.Invoke(null, null)).Lookup;
        }

        private static string FormatErrors(MandatoryRouteLoopBuildResult result) => string.Join("\n", result.Errors.Select(value => value.Code + " " + value.Message));
        private static T GetField<T>(object target, Type type, string name) => (T)type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    }
}
