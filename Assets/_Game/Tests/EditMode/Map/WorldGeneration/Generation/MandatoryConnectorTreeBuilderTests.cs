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
    [Category("MAP05_03")]
    public sealed class MandatoryConnectorTreeBuilderTests
    {
        private MandatoryRouteTerminalSet terminalSet;
        private MandatoryRouteMaskLookup routeMaskLookup;
        private MandatoryConnectorTreeBuilder reused;
        private string expectedSignature;

        public static IEnumerable DeterminismCases
        {
            get
            {
                for (var index = 0; index < 100; index++)
                    yield return new TestCaseData(index).SetName("Build_DeterministicKruskal_" + index.ToString("D3", CultureInfo.InvariantCulture));
            }
        }

        public static IEnumerable InvalidEdgeIds
        {
            get
            {
                yield return null;
                yield return string.Empty;
                yield return "EDGE_0_TERM_A__TO__TERM_B";
                yield return "EDGE_00_term_A__TO__TERM_B";
                yield return "EDGE_00_TERM_A_TO_TERM_B";
                yield return "EDGE_00_TERM_B__TO__TERM_A";
                yield return "EDGE_00_TERM_A__TO__TERM_A";
                yield return "EDGE_000_TERM_A__TO__TERM_B";
            }
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var terminalFixture = new MandatoryTerminalBuilderTests();
            terminalFixture.OneTimeSetUp();
            var fixtureType = typeof(MandatoryTerminalBuilderTests);
            var site = GetField<SiteReservationSnapshot>(terminalFixture, fixtureType, "site");
            var biome = GetField<BiomePatchValidationPublication>(terminalFixture, fixtureType, "biome");
            var terminalResult = new MandatoryTerminalBuilder().Build(site, biome);
            Assert.That(terminalResult.Status, Is.EqualTo(MandatoryTerminalBuildStatus.Completed));
            terminalSet = terminalResult.TerminalSet;

            var buildStarter = typeof(MandatoryRouteMaskLookupBuilderTests).GetMethod("BuildStarter", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(buildStarter, Is.Not.Null);
            var lookupResult = (MandatoryRouteMaskLookupBuildResult)buildStarter.Invoke(null, null);
            Assert.That(lookupResult.Status, Is.EqualTo(MandatoryRouteMaskLookupBuildStatus.Completed));
            routeMaskLookup = lookupResult.Lookup;
            reused = new MandatoryConnectorTreeBuilder();
            expectedSignature = Signature(reused.Build(terminalSet, routeMaskLookup));
        }

        [TestCaseSource(nameof(DeterminismCases))]
        public void Build_DeterministicKruskal(int caseId)
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = (caseId & 1) == 0 ? CultureInfo.GetCultureInfo("en-US") : CultureInfo.GetCultureInfo("tr-TR");
                var builder = (caseId & 2) == 0 ? new MandatoryConnectorTreeBuilder() : reused;
                Assert.That(Signature(builder.Build(terminalSet, routeMaskLookup)), Is.EqualTo(expectedSignature));
            }
            finally { CultureInfo.CurrentCulture = previous; }
        }

        [Test]
        public void StarterBuildPublishesExactTreeAndDiagnostics()
        {
            var result = Complete();
            Assert.That(result.Succeeded && !result.RetryRequired, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(new[] { result.Tree.NodeCount, result.Tree.CandidateEdgeCount, result.Tree.TreeEdgeCount }, Is.EqualTo(new[] { 7, 21, 6 }));
            Assert.That(result.Tree.IsConnected && result.Tree.IsAcyclic && result.Tree.CoversAllTerminals, Is.True);
            Assert.That(new[] { result.Diagnostics.TerminalCount, result.Diagnostics.StartTerminalCount, result.Diagnostics.SiteEntryTerminalCount, result.Diagnostics.RouteMaskCount, result.Diagnostics.CandidateEdgeCount, result.Diagnostics.TreeEdgeCount, result.Diagnostics.ConnectedComponentCount, result.Diagnostics.CoveredTerminalCount, result.Diagnostics.RngDrawCount, result.Diagnostics.SourceMutationCount }, Is.EqualTo(new[] { 7, 1, 6, 3, 21, 6, 1, 7, 0, 0 }));
        }

        [Test]
        public void SourcesArePreservedByReference()
        {
            var tree = Complete().Tree;
            Assert.That(tree.SourceTerminalSet, Is.SameAs(terminalSet));
            Assert.That(tree.SourceRouteMaskLookup, Is.SameAs(routeMaskLookup));
        }

        [Test]
        public void CandidateOrderMatchesFrozenTotalTieBreak()
        {
            var edges = Complete().Tree.CandidateEdges;
            Assert.That(edges, Has.Count.EqualTo(21));
            for (var index = 1; index < edges.Count; index++) Assert.That(Compare(edges[index - 1], edges[index]), Is.LessThanOrEqualTo(0));
            Assert.That(edges.All(edge => !edge.IsTreeEdge), Is.True);
        }

        [Test]
        public void TreeEdgesAreSixUniqueCandidateIdentitiesInSelectionOrder()
        {
            var tree = Complete().Tree;
            Assert.That(tree.TreeEdges.Select(edge => edge.EdgeId).Distinct().Count(), Is.EqualTo(6));
            Assert.That(tree.TreeEdges.All(edge => edge.IsTreeEdge), Is.True);
            Assert.That(tree.TreeEdges.All(edge => tree.CandidateEdges.Any(candidate => candidate.EdgeId == edge.EdgeId)), Is.True);
            Assert.That(tree.TotalTreeCost, Is.EqualTo(tree.TreeEdges.Sum(edge => edge.Cost.TotalCost)));
        }

        [Test]
        public void TreeLookupAndAdjacencyReturnStableReadOnlyViews()
        {
            var tree = Complete().Tree;
            foreach (var edge in tree.TreeEdges)
            {
                Assert.That(tree.TryGetTreeEdge(edge.EdgeId, out var found), Is.True);
                Assert.That(found, Is.SameAs(edge));
            }
            foreach (var terminal in terminalSet.Terminals)
            {
                var values = tree.GetTreeEdgesForTerminal(terminal.TerminalId);
                Assert.That(values, Is.Not.Empty);
                Assert.That(values.All(edge => edge.FromTerminalId == terminal.TerminalId || edge.ToTerminalId == terminal.TerminalId), Is.True);
                Assert.Throws<NotSupportedException>(() => ((IList<MandatoryConnectorCandidateEdge>)values).Add(tree.TreeEdges[0]));
            }
        }

        [Test]
        public void UnknownLookupKeysAreSafe()
        {
            var tree = Complete().Tree;
            Assert.That(tree.TryGetTreeEdge(new MandatoryConnectorEdgeId("EDGE_99_TERM_A__TO__TERM_B"), out _), Is.False);
            Assert.That(tree.GetTreeEdgesForTerminal(new MandatoryRouteTerminalId("UNKNOWN")), Is.Empty);
        }

        [TestCaseSource(nameof(InvalidEdgeIds))]
        public void EdgeIdRejectsInvalidGrammarOrEndpointOrder(string value)
        {
            Assert.That(MandatoryConnectorEdgeId.TryCreate(value, out var parsed), Is.False);
            Assert.That(parsed.IsValid, Is.False);
            if (value == null) Assert.Throws<ArgumentNullException>(() => new MandatoryConnectorEdgeId(value));
            else Assert.Throws<ArgumentException>(() => new MandatoryConnectorEdgeId(value));
        }

        [Test]
        public void EdgeIdHasStableValueOrderAndHash()
        {
            var first = new MandatoryConnectorEdgeId("EDGE_00_TERM_A__TO__TERM_B");
            var same = new MandatoryConnectorEdgeId(new string(first.Value.ToCharArray()));
            var later = new MandatoryConnectorEdgeId("EDGE_01_TERM_A__TO__TERM_C");
            Assert.That(first == same && first.GetHashCode() == same.GetHashCode(), Is.True);
            Assert.That(first.CompareTo(later), Is.LessThan(0));
            Assert.That(default(MandatoryConnectorEdgeId).IsValid, Is.False);
        }

        [Test]
        public void EdgeCostUsesExactCheckedFormula()
        {
            var cost = new MandatoryConnectorEdgeCost(12, 4, 3, 100000);
            Assert.That(new[] { cost.ManhattanDistance, cost.ReservationOrderSpread, cost.KindPenalty, cost.SharedApproachPenalty, cost.TotalCost }, Is.EqualTo(new[] { 12, 4, 3, 100000, 112043 }));
        }

        [TestCase(-1, 0, 0, 0)]
        [TestCase(0, -1, 0, 0)]
        [TestCase(0, 0, -1, 0)]
        [TestCase(0, 0, 0, -1)]
        public void EdgeCostRejectsNegativeComponents(int distance, int spread, int kind, int shared)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MandatoryConnectorEdgeCost(distance, spread, kind, shared));
        }

        [Test]
        public void EdgeCostRejectsOverflow()
        {
            Assert.Throws<OverflowException>(() => new MandatoryConnectorEdgeCost(int.MaxValue, 0, 0, 0));
            Assert.Throws<OverflowException>(() => new MandatoryConnectorEdgeCost(0, int.MaxValue, 0, 0));
        }

        [Test]
        public void CandidateRejectsSelfLoopAndNonCanonicalOrder()
        {
            var id = new MandatoryRouteTerminalId("TERM_A");
            var other = new MandatoryRouteTerminalId("TERM_B");
            var edgeId = new MandatoryConnectorEdgeId("EDGE_00_TERM_A__TO__TERM_B");
            var coordinate = new SectorCoord(1, 1);
            var cost = new MandatoryConnectorEdgeCost(0, 1, 0, 100000);
            Assert.Throws<ArgumentException>(() => new MandatoryConnectorCandidateEdge(edgeId, id, id, 0, 1, coordinate, coordinate, cost, false));
            Assert.Throws<ArgumentException>(() => new MandatoryConnectorCandidateEdge(edgeId, other, id, 2, 1, coordinate, coordinate, cost, false));
        }

        [Test]
        public void CandidatePreservesCanonicalImmutableFields()
        {
            var edge = new MandatoryConnectorCandidateEdge(
                new MandatoryConnectorEdgeId("EDGE_00_TERM_A__TO__TERM_B"),
                new MandatoryRouteTerminalId("TERM_A"), new MandatoryRouteTerminalId("TERM_B"),
                0, 1, new SectorCoord(1, 2), new SectorCoord(3, 4), new MandatoryConnectorEdgeCost(4, 1, 0, 0), false);
            Assert.That(edge.FromTerminalId.Value + ":" + edge.ToTerminalId.Value + ":" + edge.Cost.TotalCost, Is.EqualTo("TERM_A:TERM_B:4010"));
            Assert.That(edge.IsTreeEdge, Is.False);
        }

        [Test]
        public void SharedApproachPenaltyIsExactAndDeterministic()
        {
            var cost = new MandatoryConnectorEdgeCost(0, 2, 3, 100000);
            Assert.That(cost.TotalCost, Is.EqualTo(100023));
            Assert.That(cost, Is.EqualTo(new MandatoryConnectorEdgeCost(0, 2, 3, 100000)));
        }

        [Test]
        public void NullInputsAccumulateSortedErrorsAtomically()
        {
            var result = new MandatoryConnectorTreeBuilder().Build(null, null);
            Assert.That(result.Status, Is.EqualTo(MandatoryConnectorTreeBuildStatus.InvalidInput));
            Assert.That(result.Tree, Is.Null);
            Assert.That(result.Diagnostics, Is.Null);
            Assert.That(result.Errors, Has.Count.EqualTo(2));
            Assert.That(result.Errors.All(error => error.Code == MandatoryConnectorTreeBuildErrorCode.MissingInput), Is.True);
            Assert.That(result.RetryRequired, Is.False);
        }

        [Test]
        public void MissingEitherInputPublishesNothing()
        {
            Assert.That(new MandatoryConnectorTreeBuilder().Build(null, routeMaskLookup).Succeeded, Is.False);
            Assert.That(new MandatoryConnectorTreeBuilder().Build(terminalSet, null).Succeeded, Is.False);
        }

        [Test]
        public void SourceCollectionsRemainUnchanged()
        {
            var beforeTerminals = string.Join("|", terminalSet.Terminals.Select(value => value.TerminalId.Value + ":" + value.ApproachSector));
            var beforeMasks = string.Join("|", routeMaskLookup.Records.Select(value => value.MaskId.Value + ":" + value.OpenMask));
            Complete();
            Assert.That(string.Join("|", terminalSet.Terminals.Select(value => value.TerminalId.Value + ":" + value.ApproachSector)), Is.EqualTo(beforeTerminals));
            Assert.That(string.Join("|", routeMaskLookup.Records.Select(value => value.MaskId.Value + ":" + value.OpenMask)), Is.EqualTo(beforeMasks));
        }

        [Test]
        public void FreshReuseAndThreadsHaveOneSignature()
        {
            var values = new string[12];
            Parallel.For(0, values.Length, index => values[index] = Signature((index & 1) == 0 ? new MandatoryConnectorTreeBuilder().Build(terminalSet, routeMaskLookup) : reused.Build(terminalSet, routeMaskLookup)));
            Assert.That(values.Distinct().Single(), Is.EqualTo(expectedSignature));
        }

        [Test]
        public void RuntimeSurfaceHasNoMutableStaticUnityEditorOrMap05_10PlusSymbols()
        {
            var types = new[]
            {
                typeof(MandatoryConnectorEdgeId), typeof(MandatoryConnectorEdgeCost), typeof(MandatoryConnectorCandidateEdge),
                typeof(MandatoryConnectorTree), typeof(MandatoryConnectorTreeBuildError), typeof(MandatoryConnectorTreeDiagnostics),
                typeof(MandatoryConnectorTreeBuildResult), typeof(MandatoryConnectorTreeBuilder), typeof(MandatoryRouteGraphValidator)
            };
            foreach (var type in types)
                Assert.That(type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Where(field => !field.IsLiteral && !field.IsInitOnly), Is.Empty, type.FullName);
            Assert.That(typeof(MandatoryConnectorTreeBuilder).Assembly.GetReferencedAssemblies().Any(reference => reference.Name == "UnityEditor"), Is.False);
            var names = string.Join("|", typeof(MandatoryConnectorTreeBuilder).Assembly.GetTypes().Select(type => type.Name));
            Assert.That(names, Does.Not.Contain("MandatoryRouteGraphPlan"));
            Assert.That(names, Does.Not.Contain("RouteGateway"));
        }

        private MandatoryConnectorTreeBuildResult Complete()
        {
            var result = reused.Build(terminalSet, routeMaskLookup);
            Assert.That(result.Status, Is.EqualTo(MandatoryConnectorTreeBuildStatus.Completed), FormatErrors(result));
            return result;
        }

        private static string Signature(MandatoryConnectorTreeBuildResult result) =>
            result.Status + "|" + string.Join(",", result.Errors.Select(error => error.Code + ":" + error.FirstId + ":" + error.SecondId + ":" + error.SectorIndex + ":" + error.Message)) + "|" +
            (result.Tree == null ? "null" : string.Join(",", result.Tree.CandidateEdges.Select(edge => edge.EdgeId.Value + ":" + edge.Cost.TotalCost)) + "/" + string.Join(",", result.Tree.TreeEdges.Select(edge => edge.EdgeId.Value))) + "|" +
            (result.Diagnostics == null ? "null" : string.Join(",", result.Diagnostics.TerminalCount, result.Diagnostics.CandidateEdgeCount, result.Diagnostics.TreeEdgeCount, result.Diagnostics.TotalTreeCost, result.Diagnostics.ConnectedComponentCount, result.Diagnostics.CoveredTerminalCount, result.Diagnostics.SharedApproachCandidateCount, result.Diagnostics.RngDrawCount, result.Diagnostics.SourceMutationCount));

        private static string FormatErrors(MandatoryConnectorTreeBuildResult result) => string.Join("\n", result.Errors.Select(error => error.Code + " " + error.Message));
        private static int Compare(MandatoryConnectorCandidateEdge left, MandatoryConnectorCandidateEdge right)
        {
            var value = left.Cost.TotalCost.CompareTo(right.Cost.TotalCost);
            if (value != 0) return value;
            value = left.FromTerminalOrder.CompareTo(right.FromTerminalOrder);
            if (value != 0) return value;
            value = left.ToTerminalOrder.CompareTo(right.ToTerminalOrder);
            if (value != 0) return value;
            value = left.FromTerminalId.CompareTo(right.FromTerminalId);
            if (value != 0) return value;
            value = left.ToTerminalId.CompareTo(right.ToTerminalId);
            return value != 0 ? value : left.EdgeId.CompareTo(right.EdgeId);
        }
        private static T GetField<T>(object target, Type type, string name) => (T)type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    }
}
