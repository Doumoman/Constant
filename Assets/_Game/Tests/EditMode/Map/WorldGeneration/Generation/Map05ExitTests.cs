using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Diagnostics;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    [Category("MAP05_11")]
    public sealed class Map05ExitTests
    {
        private SiteReservationSnapshot site;
        private BiomePatchValidationPublication biome;
        private IReadOnlyList<SectorRouteMaskDefinition> routeDefinitions;
        private AttemptServices reusedServices;
        private AttemptRecord knownVector;
        private string sourceDigest;

        public static IEnumerable DeterminismCases
        {
            get
            {
                for (var seed = 0; seed <= 101; seed++)
                    yield return new TestCaseData(seed).SetName(
                        "MandatoryAttempt_DeterministicSeed_" + seed.ToString("D3", CultureInfo.InvariantCulture));
            }
        }

        public static IEnumerable PriorResultChainCases
        {
            get
            {
                yield return Chain("MAP05_01_BUILD_MANDATORY_TERMINALS", "a5ea4a2a3e7ac29de825e45e4b75a816ae2d8f5a6d4824fabf6a0676d62b2069");
                yield return Chain("MAP05_02_IMPLEMENT_ROUTE_MASK_LOOKUP", "c053a0bfaa35967e2fe0afd0b3416f7e090c0238626f4e8ed632a3afd858b067");
                yield return Chain("MAP05_03_IMPLEMENT_MINIMUM_CONNECTOR_TREE", "3fd9078ab5a2f288c0e8e657f510f0d84f1d3d49409ebc731621b38086dfa74d");
                yield return Chain("MAP05_04_IMPLEMENT_HORIZONTAL_BACKBONE_ROUTER", "6fcb71658dbf3924c1335b8c10ad93f26fca1a62648571b1e9eb08d62d14a6c4");
                yield return Chain("MAP05_05_IMPLEMENT_VERTICAL_GATEWAY_PLANNER", "016cf5cdd79887252c60b2504cc8ba3f69e037e9af12589e2d3b9b40d038647e");
                yield return Chain("MAP05_06_RESOLVE_UP_DOWN_CONFLICTS", "430930f35e6bd3be0ee8ffc9bc4aa06daeb90cf2828c50ac4148368bc24fed79");
                yield return Chain("MAP05_07_ADD_MANDATORY_ROUTE_LOOPS", "cbe4f9a136d488df134a6eee676e13950d5dfd15238abf3188a81ce532fbdf65");
                yield return Chain("MAP05_08_BUILD_MANDATORY_ROUTE_GRAPH", "7c9820290ec5269222b8c145603a9ae53a2ea7f8d1df7b0ca6029e1be3647a99");
                yield return Chain("MAP05_09_VALIDATE_MANDATORY_ROUTE_GRAPH", "72df536b5d51c7db7ff364e74e7bd7141f0399465e38b3a75d366640a1d3b33a");
                yield return Chain("MAP05_10_CREATE_MANDATORY_ROUTE_OVERLAY", "2f8ef4e027c1abd8f93721f840b5a6ab43d812b1bcb9bd6ae71fd8d694823c6f");
            }
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var approvedFixture = new MandatoryRouteGraphBuilderTests();
            approvedFixture.OneTimeSetUp();
            site = GetField<SiteReservationSnapshot>(approvedFixture, "site");
            biome = GetField<BiomePatchValidationPublication>(approvedFixture, "biome");
            var approvedLookup = GetField<MandatoryRouteMaskLookup>(approvedFixture, "lookup");
            routeDefinitions = approvedLookup.Records.Select(value => value.SourceDefinition).ToArray();
            Assert.That(routeDefinitions.Count, Is.EqualTo(3));
            Assert.That(site.Sectors.Count, Is.EqualTo(169));
            Assert.That(site.Reservations.Count, Is.EqualTo(7));
            reusedServices = new AttemptServices();
            sourceDigest = SourceDigest();
            knownVector = RunMandatoryRouteAttempt(0UL, 0, reusedServices, DefinitionOrder.Canonical, 0);
            Assert.That(knownVector.Completed, Is.True, knownVector.FailureReason);
        }

        [TestCaseSource(nameof(PriorResultChainCases))]
        public void PriorResultChainEntryIsCanonical(string taskKey, string sha256)
        {
            Assert.That(taskKey, Does.StartWith("MAP05_"));
            Assert.That(sha256, Does.Match("^[0-9a-f]{64}$"));
        }

        [Test]
        public void ApprovedFixtureAndStageOrderAreExact()
        {
            Assert.That(knownVector.StageStatuses, Is.EqualTo(new[]
            {
                "Completed", "Completed", "Completed", "Completed", "Completed",
                "Completed", "Completed", "Completed", "Completed", "Completed"
            }));
            Assert.That(knownVector.AttemptOrdinal, Is.Zero);
            Assert.That(knownVector.RetryRequired, Is.False);
            Assert.That(knownVector.UnresolvedCount, Is.Zero);
            Assert.That(knownVector.InvalidCount, Is.Zero);
        }

        [TestCaseSource(nameof(DeterminismCases))]
        public void FreshReusedShuffledAndCultureAttemptsMatch(int seed)
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = (seed & 1) == 0
                    ? CultureInfo.GetCultureInfo("en-US")
                    : CultureInfo.GetCultureInfo("tr-TR");
                var fresh = RunMandatoryRouteAttempt((ulong)seed, 0, new AttemptServices(), DefinitionOrder.Canonical, 0);
                var reused = RunMandatoryRouteAttempt((ulong)seed, 0, reusedServices, DefinitionOrder.Reversed, 0);
                var shuffled = RunMandatoryRouteAttempt((ulong)seed, 0, new AttemptServices(), DefinitionOrder.Shuffled, 0);
                Assert.That(fresh.Completed && reused.Completed && shuffled.Completed, Is.True);
                Assert.That(reused.StablePayloadDigest, Is.EqualTo(fresh.StablePayloadDigest));
                Assert.That(shuffled.StablePayloadDigest, Is.EqualTo(fresh.StablePayloadDigest));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Test]
        public void ReverseEnumerationThenSeedSortMatchesForwardSeedsZeroThrough101()
        {
            var forward = new List<AttemptRecord>();
            var reverse = new List<AttemptRecord>();
            for (var seed = 0; seed <= 101; seed++)
                forward.Add(RunMandatoryRouteAttempt((ulong)seed, 0, reusedServices, DefinitionOrder.Canonical, 0));
            for (var seed = 101; seed >= 0; seed--)
                reverse.Add(RunMandatoryRouteAttempt((ulong)seed, 0, reusedServices, DefinitionOrder.Reversed, 0));
            reverse.Sort((left, right) => left.WorldSeed.CompareTo(right.WorldSeed));
            Assert.That(reverse.Select(value => value.StablePayloadDigest),
                Is.EqualTo(forward.Select(value => value.StablePayloadDigest)));
        }

        [Test]
        public void SameSeedRepeatedOneHundredTimesHasExactGraphCsvRuleAndOverlayDigest()
        {
            for (var index = 0; index < 100; index++)
            {
                var current = RunMandatoryRouteAttempt(73UL, 0,
                    (index & 1) == 0 ? reusedServices : new AttemptServices(),
                    (DefinitionOrder)(index % 3), 0);
                Assert.That(current.StablePayloadDigest, Is.EqualTo(knownVector.StablePayloadDigest));
                Assert.That(current.GraphEdgeDigest, Is.EqualTo(knownVector.GraphEdgeDigest));
                Assert.That(current.OverlayDigest, Is.EqualTo(knownVector.OverlayDigest));
            }
        }

        [Test]
        public void TenThousandSeedFullBatchConservesEveryExitCounter()
        {
            var before = SourceDigest();
            var aggregate = RunBatch(10000);
            Assert.That(aggregate.TotalWorlds, Is.EqualTo(10000));
            Assert.That(aggregate.Completed, Is.EqualTo(10000));
            Assert.That(aggregate.Retry, Is.Zero);
            Assert.That(aggregate.Unresolved, Is.Zero);
            Assert.That(aggregate.Invalid, Is.Zero);
            Assert.That(aggregate.TerminalReachabilityFailures, Is.Zero);
            Assert.That(aggregate.RouteMaskMismatches, Is.Zero);
            Assert.That(aggregate.Type4UdMissing, Is.Zero);
            Assert.That(aggregate.Type4LrMismatches, Is.Zero);
            Assert.That(aggregate.EdgeReciprocityFailures, Is.Zero);
            Assert.That(aggregate.GeneratedEdgeBijectionFailures, Is.Zero);
            Assert.That(aggregate.ValidationFailures, Is.Zero);
            Assert.That(aggregate.OverlaySnapshots, Is.EqualTo(10000));
            Assert.That(aggregate.UnexpectedDependencies, Is.Zero);
            Assert.That(aggregate.Type4UdTokens, Is.EqualTo(170000));
            Assert.That(aggregate.Type4LudTokens, Is.Zero);
            Assert.That(aggregate.Type4RudTokens, Is.Zero);
            Assert.That(aggregate.Type4LrudTokens, Is.EqualTo(20000));
            Assert.That(SourceDigest(), Is.EqualTo(before));
            TestContext.Out.WriteLine(aggregate.EvidenceLine);
        }

        [Test]
        public void KnownVectorMatchesApprovedGraphCsvValidationAndOverlay()
        {
            Assert.That(knownVector.GraphNodes, Is.EqualTo(47));
            Assert.That(knownVector.DirectedEdges, Is.EqualTo(96));
            Assert.That(knownVector.UndirectedEdges, Is.EqualTo(48));
            Assert.That(knownVector.RouteCells, Is.EqualTo(47));
            Assert.That(knownVector.MaskCounts, Is.EqualTo("20/4/4/17/0/0/2"));
            Assert.That(knownVector.ReachableTerminals, Is.EqualTo(7));
            Assert.That(knownVector.RepresentedLoops, Is.EqualTo(2));
            Assert.That(knownVector.GeneratedSectorBytes, Is.EqualTo(16838));
            Assert.That(knownVector.GeneratedEdgeBytes, Is.EqualTo(7094));
            Assert.That(knownVector.GeneratedEdgeRows, Is.EqualTo(96));
            Assert.That(knownVector.RuleSummary, Is.EqualTo("12/12/0/0/0"));
        }

        [TestCase(false, false, "T4-UD")]
        [TestCase(true, false, "T4-LUD")]
        [TestCase(false, true, "T4-RUD")]
        [TestCase(true, true, "T4-LRUD")]
        public void Type4AlwaysRequiresUpDownAndPreservesIndependentHorizontalSides(bool left, bool right, string token)
        {
            Assert.That(MandatoryRouteOverlayCell.GetDisplayTypeToken(4, left, right, true, true), Is.EqualTo(token));
            Assert.Throws<ArgumentException>(() => MandatoryRouteOverlayCell.GetDisplayTypeToken(4, left, right, true, false));
            Assert.Throws<ArgumentException>(() => MandatoryRouteOverlayCell.GetDisplayTypeToken(4, left, right, false, true));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(8)]
        [TestCase(9)]
        public void SyntheticFailureClassifiesAndShortCircuitsAtFirstIncompleteStage(int forcedStage)
        {
            var record = RunMandatoryRouteAttempt(9UL, 0, new AttemptServices(), DefinitionOrder.Canonical, forcedStage);
            Assert.That(record.Completed, Is.False);
            Assert.That(record.InvalidCount, Is.EqualTo(1));
            Assert.That(record.FailureStage, Is.EqualTo(forcedStage));
            Assert.That(record.StageStatuses[forcedStage - 1], Is.Not.EqualTo("Completed"));
            for (var index = forcedStage; index < record.StageStatuses.Count; index++)
                Assert.That(record.StageStatuses[index], Is.EqualTo("NOT_RUN"));
        }

        [Test]
        public void MissingReverseGeneratedEdgeIsRejectedWithoutGraphRepair()
        {
            var graph = knownVector.Graph;
            var rows = ToGeneratedRows(graph);
            rows.RemoveAt(rows.Count - 1);
            var result = new MandatoryRouteGraphValidator().Validate(graph, graph.RouteStampedWorld, rows,
                GeneratedWorldDataCsvSerializer.Serialize(graph.RouteStampedWorld),
                GeneratedWorldEdgesCsvSerializer.Serialize(rows), graph.SourceTerminalSet, graph.SourceLoopPlan);
            Assert.That(result.Status, Is.EqualTo(MandatoryRouteValidationStatus.Completed));
            Assert.That(result.Report.IsValid, Is.False);
            Assert.That(result.Report.Violations.Any(value =>
                value.RuleId.Value == MandatoryRouteGraphValidator.GeneratedEdgeCsvRule), Is.True);
            Assert.That(graph.DirectedEdgeCount, Is.EqualTo(96));
        }

        [Test]
        public void GeneratedEdgeRowsAreExactDirectedGraphBijection()
        {
            var graph = knownVector.Graph;
            var rows = ToGeneratedRows(graph);
            Assert.That(rows, Has.Count.EqualTo(graph.DirectedEdgeCount));
            Assert.That(GeneratedWorldEdgesCsvSerializer.Serialize(rows), Is.EqualTo(graph.GeneratedWorldEdgesCsv));
            Assert.That(graph.Edges.All(edge => graph.Edges.Any(reverse =>
                reverse.FromSectorIndex == edge.ToSectorIndex && reverse.ToSectorIndex == edge.FromSectorIndex &&
                reverse.Side == edge.ReverseSide && reverse.ReverseSide == edge.Side)), Is.True);
        }

        [Test]
        public void BrokenType4PublicFixtureIsRejectedBeforeOverlayCreation()
        {
            Assert.Throws<ArgumentException>(() =>
                MandatoryRouteOverlayCell.GetDisplayTypeToken(4, false, false, true, false));
            Assert.Throws<ArgumentException>(() =>
                MandatoryRouteOverlayCell.GetDisplayTypeToken(4, true, true, false, true));
            Assert.That(knownVector.OverlaySnapshot.Type4UdCount + knownVector.OverlaySnapshot.Type4LudCount +
                knownVector.OverlaySnapshot.Type4RudCount + knownVector.OverlaySnapshot.Type4LrudCount,
                Is.EqualTo(19));
        }

        [Test]
        public void OverlaySnapshotIsSharedDeterministicAndComplete()
        {
            var snapshot = knownVector.OverlaySnapshot;
            Assert.That(snapshot.ValidationBanner, Is.EqualTo("PASS_ROUTE 12/12 | V/E/W 0/0/0"));
            Assert.That(snapshot.Cells, Has.Count.EqualTo(47));
            Assert.That(snapshot.Edges, Has.Count.EqualTo(96));
            Assert.That(snapshot.ReachableTerminalCount + snapshot.RepresentedLoopCount, Is.EqualTo(9));
            Assert.That(snapshot.Cells.Any(value => value.TerminalRoleToken.Contains("START")), Is.True);
            Assert.That(snapshot.Cells.Any(value => value.IsLoop), Is.True);
            Assert.That(HashOverlay(snapshot), Is.EqualTo(knownVector.OverlayDigest));
        }

        [Test]
        public void SourceImmutabilityAndUnexpectedDependencyCountersRemainZero()
        {
            Assert.That(SourceDigest(), Is.EqualTo(sourceDigest));
            Assert.That(knownVector.SourceMutationCount, Is.Zero);
            Assert.That(knownVector.FileReadCount, Is.Zero);
            Assert.That(knownVector.FileWriteCount, Is.Zero);
            Assert.That(knownVector.ClockReadCount, Is.Zero);
            Assert.That(knownVector.RngDrawCount, Is.Zero);
            Assert.That(knownVector.Graph.GeneratedWorldEdgesCsv, Is.Not.SameAs(knownVector.Graph.GeneratedWorldEdgesCsv));
        }

        [Test]
        public void RuntimePhaseBoundaryAllowsMap06_02EnumerationAndForbidsMap06_03PlusSurface()
        {
            var runtimeAssembly = typeof(MandatoryRouteGraphBuilder).Assembly;
            var names = runtimeAssembly.GetTypes().Select(value => value.Name).ToArray();
            Assert.That(names, Does.Contain("MandatoryRouteOverlaySnapshot"));
            Assert.That(names, Does.Contain("MandatoryRouteOverlay"));
            Assert.That(names, Does.Contain("OptionalRegion"));
            Assert.That(names, Does.Contain("OptionalRegionSnapshot"));
            Assert.That(names, Does.Contain("OptionalAttachmentCandidateId"));
            Assert.That(names, Does.Contain("OptionalAttachmentCandidate"));
            Assert.That(names, Does.Contain("OptionalAttachmentEnumerationSettings"));
            Assert.That(names, Does.Contain("OptionalAttachmentEnumerationDiagnostics"));
            Assert.That(names, Does.Contain("OptionalAttachmentEnumerationResult"));
            Assert.That(names, Does.Contain("OptionalAttachmentEnumerator"));
            foreach (var forbidden in new[]
            {
                "GeneratedOptionalRegionCsvWriter",
                "OptionalOverlayEdge", "OptionalRouteMaskLookup", "OptionalReturnConnection",
                "OptionalClueAssigner",
                "OptionalRegionOverlayRenderer", "OptionalRegionValidationOverlayWindow", "OptionalRegionOverlay"
            })
                Assert.That(names, Does.Not.Contain(forbidden));
            Assert.That(names, Does.Not.Contain("MandatoryRoutePass"));
            Assert.That(runtimeAssembly.GetReferencedAssemblies().Any(value => value.Name == "UnityEditor"), Is.False);
            foreach (var type in new[]
            {
                typeof(MandatoryRouteGraphBuilder), typeof(MandatoryRouteGraphValidator),
                typeof(MandatoryRouteOverlaySnapshot), typeof(MandatoryRouteOverlay)
            })
                Assert.That(type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(field => !field.IsLiteral && !field.IsInitOnly), Is.Empty, type.FullName);
        }

        private BatchAggregate RunBatch(int worldCount)
        {
            var aggregate = new BatchAggregate(worldCount);
            for (var seed = 0; seed < worldCount; seed++)
                aggregate.Add(RunMandatoryRouteAttempt((ulong)seed, 0, reusedServices, DefinitionOrder.Canonical, 0));
            aggregate.Finish();
            return aggregate;
        }

        private AttemptRecord RunMandatoryRouteAttempt(ulong worldSeed, int attemptOrdinal, AttemptServices services,
            DefinitionOrder definitionOrder, int forcedFailureStage)
        {
            var stages = Enumerable.Repeat("NOT_RUN", 10).ToArray();

            var terminalResult = services.Terminals.Build(forcedFailureStage == 1 ? null : site, biome);
            stages[0] = terminalResult.Status.ToString();
            if (!terminalResult.Succeeded) return AttemptRecord.Failed(worldSeed, attemptOrdinal, stages, 1, "TERMINAL_INVALID");

            var definitions = OrderedDefinitions(worldSeed, definitionOrder);
            var maskResult = services.Masks.Build(forcedFailureStage == 2
                ? Array.Empty<SectorRouteMaskDefinition>() : definitions);
            stages[1] = maskResult.Status.ToString();
            if (!maskResult.Success) return AttemptRecord.Failed(worldSeed, attemptOrdinal, stages, 2, "MASK_INVALID");

            var treeResult = services.Tree.Build(forcedFailureStage == 3 ? null : terminalResult.TerminalSet, maskResult.Lookup);
            stages[2] = treeResult.Status.ToString();
            if (!treeResult.Succeeded) return AttemptRecord.Failed(worldSeed, attemptOrdinal, stages, 3, "TREE_INVALID");

            var horizontalResult = services.Horizontal.Build(forcedFailureStage == 4 ? null : treeResult.Tree,
                maskResult.Lookup, site, biome);
            stages[3] = horizontalResult.Status.ToString();
            if (!horizontalResult.Succeeded) return AttemptRecord.Failed(worldSeed, attemptOrdinal, stages, 4, "HORIZONTAL_INVALID");

            var verticalResult = services.Vertical.Build(forcedFailureStage == 5 ? null : horizontalResult.Plan,
                maskResult.Lookup, site, biome);
            stages[4] = verticalResult.Status.ToString();
            if (!verticalResult.Succeeded) return AttemptRecord.Failed(worldSeed, attemptOrdinal, stages, 5, "VERTICAL_INVALID");

            var conflictResult = services.Conflicts.Build(forcedFailureStage == 6 ? null : verticalResult.Plan,
                maskResult.Lookup, site, biome);
            stages[5] = conflictResult.Status.ToString();
            if (!conflictResult.Succeeded) return AttemptRecord.Failed(worldSeed, attemptOrdinal, stages, 6, "CONFLICT_INVALID");

            var loopResult = services.Loops.Build(terminalResult.TerminalSet, treeResult.Tree, horizontalResult.Plan,
                verticalResult.Plan, forcedFailureStage == 7 ? null : conflictResult.Plan);
            stages[6] = loopResult.Status.ToString();
            if (!loopResult.Succeeded) return AttemptRecord.Failed(worldSeed, attemptOrdinal, stages, 7, "LOOP_INVALID");

            var graphResult = services.Graph.Build(terminalResult.TerminalSet, maskResult.Lookup, treeResult.Tree,
                horizontalResult.Plan, verticalResult.Plan, conflictResult.Plan,
                forcedFailureStage == 8 ? null : loopResult.Plan);
            stages[7] = graphResult.Status.ToString();
            if (!graphResult.Succeeded) return AttemptRecord.Failed(worldSeed, attemptOrdinal, stages, 8, "GRAPH_INVALID");

            var validationResult = services.Validator.Validate(forcedFailureStage == 9 ? null : graphResult.Graph);
            stages[8] = validationResult.Status.ToString();
            if (!validationResult.Succeeded)
                return AttemptRecord.Failed(worldSeed, attemptOrdinal, stages, 9, "VALIDATION_INVALID");

            var overlay = MandatoryRouteOverlaySnapshot.Create(validationResult.Report);
            stages[9] = "Completed";
            var summary = validationResult.Report.Summary;
            var type4Ud = overlay.Cells.Count(value => value.DisplayTypeToken == "T4-UD");
            var type4Lud = overlay.Cells.Count(value => value.DisplayTypeToken == "T4-LUD");
            var type4Rud = overlay.Cells.Count(value => value.DisplayTypeToken == "T4-RUD");
            var type4Lrud = overlay.Cells.Count(value => value.DisplayTypeToken == "T4-LRUD");
            var sourceMutations = terminalResult.Diagnostics.SourceMutationCount + maskResult.Diagnostics.SourceMutationCount +
                treeResult.Diagnostics.SourceMutationCount + horizontalResult.Diagnostics.SourceMutationCount +
                verticalResult.Diagnostics.SourceMutationCount + conflictResult.Diagnostics.SourceMutationCount +
                loopResult.Diagnostics.SourceMutationCount + graphResult.Diagnostics.SourceMutationCount +
                validationResult.Diagnostics.SourceMutationCount;
            var rngDraws = terminalResult.Diagnostics.RngDrawCount + maskResult.Diagnostics.RngDrawCount +
                treeResult.Diagnostics.RngDrawCount + horizontalResult.Diagnostics.RngDrawCount +
                verticalResult.Diagnostics.RngDrawCount + conflictResult.Diagnostics.RngDrawCount +
                loopResult.Diagnostics.RngDrawCount + graphResult.Diagnostics.RngDrawCount +
                validationResult.Diagnostics.RngDrawCount;
            var fileWrites = conflictResult.Diagnostics.FileWriteCount + loopResult.Diagnostics.FileWriteCount +
                graphResult.Diagnostics.FileWriteCount + validationResult.Diagnostics.FileWriteCount;
            return AttemptRecord.CompletedRecord(worldSeed, attemptOrdinal, stages, graphResult.Graph, overlay,
                summary, conflictResult.Diagnostics.UnresolvedCount, sourceMutations,
                validationResult.Diagnostics.FileReadCount, fileWrites, validationResult.Diagnostics.ClockReadCount,
                rngDraws, type4Ud, type4Lud, type4Rud, type4Lrud);
        }

        private IReadOnlyList<SectorRouteMaskDefinition> OrderedDefinitions(ulong seed, DefinitionOrder order)
        {
            var values = new List<SectorRouteMaskDefinition>(routeDefinitions);
            if (order == DefinitionOrder.Reversed)
            {
                values.Reverse();
            }
            else if (order == DefinitionOrder.Shuffled)
            {
                var rotated = new List<SectorRouteMaskDefinition>(values.Count);
                var offset = (int)(seed % (ulong)values.Count);
                for (var index = 0; index < values.Count; index++)
                    rotated.Add(values[(index + offset + 1) % values.Count]);
                values = rotated;
            }
            return values;
        }

        private string SourceDigest()
        {
            var reservations = string.Join("/", site.Reservations.Select(value =>
                value.ReservationId.Value + ":" + value.ReservationOrder + ":" + value.Origin));
            return Hash(reservations + "|" +
                Hash(GeneratedWorldDataCsvSerializer.Serialize(biome.WorldWithBiomeAssignments)));
        }

        private static List<GeneratedWorldEdge> ToGeneratedRows(MandatoryRouteGraph graph)
        {
            return graph.Edges.Select(edge => new GeneratedWorldEdge(graph.RouteStampedWorld.Seed,
                WorldGridIndex.ToCoordinate(edge.FromSectorIndex), edge.Side,
                WorldGridIndex.ToCoordinate(edge.ToSectorIndex), edge.Layer, edge.TraversalKind,
                edge.Open, edge.EdgeSignatureId, edge.CostTiles)).ToList();
        }

        private static string HashOverlay(MandatoryRouteOverlaySnapshot snapshot)
        {
            return Hash(snapshot.ValidationBanner + "|" + string.Join("/", snapshot.Cells.Select(value =>
                value.Index + ":" + value.DisplayTypeToken + ":" + value.SideGlyph + ":" +
                value.DistanceFromStart + ":" + value.TerminalRoleToken + ":" + (value.IsLoop ? "1" : "0"))) +
                "|" + string.Join("/", snapshot.Edges.Select(value => value.EdgeId.Value)));
        }

        private static string HashGraphEdges(MandatoryRouteGraph graph)
        {
            return Hash(string.Join("/", graph.Edges.Select(value =>
                value.EdgeId.Value + ":" + value.FromSectorIndex + ":" + value.Side + ":" +
                value.ToSectorIndex + ":" + value.ReverseSide + ":" + value.SourceArtifactId)));
        }

        private static string Hash(string value) => Hash(Encoding.UTF8.GetBytes(value ?? string.Empty));

        private static string Hash(byte[] value)
        {
            using (var sha = SHA256.Create())
                return Hex(sha.ComputeHash(value));
        }

        private static string Hex(byte[] value) =>
            BitConverter.ToString(value).Replace("-", string.Empty).ToLowerInvariant();

        private static TestCaseData Chain(string taskKey, string sha256) =>
            new TestCaseData(taskKey, sha256).SetName("PriorResultChain_" + taskKey);

        private static T GetField<T>(object target, string name)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(target.GetType().FullName, name);
            return (T)field.GetValue(target);
        }

        private enum DefinitionOrder
        {
            Canonical,
            Reversed,
            Shuffled
        }

        private sealed class AttemptServices
        {
            public AttemptServices()
            {
                Terminals = new MandatoryTerminalBuilder();
                Masks = new MandatoryRouteMaskLookupBuilder();
                Tree = new MandatoryConnectorTreeBuilder();
                Horizontal = new HorizontalBackboneRouter();
                Vertical = new VerticalGatewayPlanner();
                Conflicts = new UpDownConflictResolver();
                Loops = new MandatoryRouteLoopPlanner();
                Graph = new MandatoryRouteGraphBuilder();
                Validator = new MandatoryRouteGraphValidator();
            }

            public MandatoryTerminalBuilder Terminals { get; }
            public MandatoryRouteMaskLookupBuilder Masks { get; }
            public MandatoryConnectorTreeBuilder Tree { get; }
            public HorizontalBackboneRouter Horizontal { get; }
            public VerticalGatewayPlanner Vertical { get; }
            public UpDownConflictResolver Conflicts { get; }
            public MandatoryRouteLoopPlanner Loops { get; }
            public MandatoryRouteGraphBuilder Graph { get; }
            public MandatoryRouteGraphValidator Validator { get; }
        }

        private sealed class AttemptRecord
        {
            private AttemptRecord(ulong worldSeed, int attemptOrdinal, IReadOnlyList<string> stages, bool completed,
                int failureStage, string failureReason, MandatoryRouteGraph graph,
                MandatoryRouteOverlaySnapshot overlay, MandatoryRouteValidationSummary summary,
                int unresolvedCount, int sourceMutationCount, int fileReadCount, int fileWriteCount,
                int clockReadCount, int rngDrawCount, int type4Ud, int type4Lud, int type4Rud, int type4Lrud)
            {
                WorldSeed = worldSeed;
                AttemptOrdinal = attemptOrdinal;
                StageStatuses = Array.AsReadOnly(stages.ToArray());
                Completed = completed;
                FailureStage = failureStage;
                FailureReason = failureReason ?? string.Empty;
                Graph = graph;
                OverlaySnapshot = overlay;
                UnresolvedCount = unresolvedCount;
                SourceMutationCount = sourceMutationCount;
                FileReadCount = fileReadCount;
                FileWriteCount = fileWriteCount;
                ClockReadCount = clockReadCount;
                RngDrawCount = rngDrawCount;
                Type4UdTokens = type4Ud;
                Type4LudTokens = type4Lud;
                Type4RudTokens = type4Rud;
                Type4LrudTokens = type4Lrud;
                InvalidCount = completed ? 0 : 1;
                RetryRequired = false;
                if (completed)
                {
                    GraphNodes = graph.NodeCount;
                    DirectedEdges = graph.DirectedEdgeCount;
                    UndirectedEdges = graph.UndirectedEdgeCount;
                    RouteCells = graph.CellCount;
                    MaskCounts = summary.Type1Count + "/" + summary.Type2Count + "/" + summary.Type3Count + "/" +
                        summary.Type4UdCount + "/" + summary.Type4LudCount + "/" + summary.Type4RudCount + "/" + summary.Type4LrudCount;
                    ReachableTerminals = summary.ReachableTerminalCount;
                    RepresentedLoops = summary.RepresentedLoopCount;
                    GeneratedSectorBytes = summary.GeneratedSectorCsvByteCount;
                    GeneratedEdgeBytes = summary.GeneratedEdgeCsvByteCount;
                    GeneratedEdgeRows = summary.GeneratedEdgeRowCount;
                    RuleSummary = summary.RuleCount + "/" + summary.PassedRuleCount + "/" + summary.ViolationCount + "/" +
                        summary.ErrorCount + "/" + summary.WarningCount;
                    GraphEdgeDigest = HashGraphEdges(graph);
                    OverlayDigest = HashOverlay(overlay);
                }
                else
                {
                    MaskCounts = string.Empty;
                    RuleSummary = string.Empty;
                    GraphEdgeDigest = string.Empty;
                    OverlayDigest = string.Empty;
                }
                StablePayloadDigest = Hash(StablePayloadCanonical());
            }

            public ulong WorldSeed { get; }
            public int AttemptOrdinal { get; }
            public IReadOnlyList<string> StageStatuses { get; }
            public bool Completed { get; }
            public bool RetryRequired { get; }
            public int InvalidCount { get; }
            public int FailureStage { get; }
            public string FailureReason { get; }
            public MandatoryRouteGraph Graph { get; }
            public MandatoryRouteOverlaySnapshot OverlaySnapshot { get; }
            public int GraphNodes { get; }
            public int DirectedEdges { get; }
            public int UndirectedEdges { get; }
            public int RouteCells { get; }
            public string MaskCounts { get; }
            public int ReachableTerminals { get; }
            public int RepresentedLoops { get; }
            public int GeneratedSectorBytes { get; }
            public int GeneratedEdgeBytes { get; }
            public int GeneratedEdgeRows { get; }
            public string RuleSummary { get; }
            public string GraphEdgeDigest { get; }
            public string OverlayDigest { get; }
            public int UnresolvedCount { get; }
            public int SourceMutationCount { get; }
            public int FileReadCount { get; }
            public int FileWriteCount { get; }
            public int ClockReadCount { get; }
            public int RngDrawCount { get; }
            public int Type4UdTokens { get; }
            public int Type4LudTokens { get; }
            public int Type4RudTokens { get; }
            public int Type4LrudTokens { get; }
            public string StablePayloadDigest { get; }

            public string Canonical => WorldSeed.ToString(CultureInfo.InvariantCulture) + "|" +
                AttemptOrdinal.ToString(CultureInfo.InvariantCulture) + "|" + StablePayloadCanonical();

            private string StablePayloadCanonical()
            {
                return string.Join(",", StageStatuses) + "|" + (Completed ? "1" : "0") + "|" +
                    FailureStage + "|" + FailureReason + "|" + GraphNodes + "/" + DirectedEdges + "/" +
                    UndirectedEdges + "/" + RouteCells + "|" + MaskCounts + "|" + GraphEdgeDigest + "|" +
                    ReachableTerminals + "/" + RepresentedLoops + "|" + GeneratedSectorBytes + "/" +
                    GeneratedEdgeBytes + "/" + GeneratedEdgeRows + "|" + RuleSummary + "|" + OverlayDigest + "|" +
                    SourceMutationCount + "/" + FileReadCount + "/" + FileWriteCount + "/" + ClockReadCount + "/" +
                    RngDrawCount + "|" + Type4UdTokens + "/" + Type4LudTokens + "/" + Type4RudTokens + "/" + Type4LrudTokens;
            }

            public static AttemptRecord Failed(ulong seed, int ordinal, IReadOnlyList<string> stages,
                int failureStage, string reason) =>
                new AttemptRecord(seed, ordinal, stages, false, failureStage, reason, null, null, null,
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

            public static AttemptRecord CompletedRecord(ulong seed, int ordinal, IReadOnlyList<string> stages,
                MandatoryRouteGraph graph, MandatoryRouteOverlaySnapshot overlay, MandatoryRouteValidationSummary summary,
                int unresolvedCount, int sourceMutationCount, int fileReadCount, int fileWriteCount,
                int clockReadCount, int rngDrawCount, int type4Ud, int type4Lud, int type4Rud, int type4Lrud) =>
                new AttemptRecord(seed, ordinal, stages, true, 0, string.Empty, graph, overlay, summary,
                    unresolvedCount, sourceMutationCount, fileReadCount, fileWriteCount, clockReadCount, rngDrawCount,
                    type4Ud, type4Lud, type4Rud, type4Lrud);
        }

        private sealed class BatchAggregate
        {
            private readonly IncrementalHash canonicalHash;
            private readonly IncrementalHash graphHash;
            private string stableOverlayDigest;

            public BatchAggregate(int totalWorlds)
            {
                TotalWorlds = totalWorlds;
                canonicalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                graphHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            }

            public int TotalWorlds { get; }
            public int Completed { get; private set; }
            public int Retry { get; private set; }
            public int Unresolved { get; private set; }
            public int Invalid { get; private set; }
            public int TerminalReachabilityFailures { get; private set; }
            public int RouteMaskMismatches { get; private set; }
            public int Type4UdMissing { get; private set; }
            public int Type4LrMismatches { get; private set; }
            public int EdgeReciprocityFailures { get; private set; }
            public int GeneratedEdgeBijectionFailures { get; private set; }
            public int ValidationFailures { get; private set; }
            public int OverlaySnapshots { get; private set; }
            public int UnexpectedDependencies { get; private set; }
            public long Type4UdTokens { get; private set; }
            public long Type4LudTokens { get; private set; }
            public long Type4RudTokens { get; private set; }
            public long Type4LrudTokens { get; private set; }
            public string GraphDigestAggregate { get; private set; }
            public string OverlayDigest { get; private set; }
            public string CanonicalDigest { get; private set; }

            public string EvidenceLine => "MAP05_11_BATCH total=" + TotalWorlds + " completed=" + Completed +
                " retry/unresolved/invalid=" + Retry + "/" + Unresolved + "/" + Invalid +
                " reach/type4/edge/csv/validation=" + TerminalReachabilityFailures + "/" + Type4UdMissing + "/" +
                EdgeReciprocityFailures + "/" + GeneratedEdgeBijectionFailures + "/" + ValidationFailures +
                " type4=" + Type4UdTokens + "/" + Type4LudTokens + "/" + Type4RudTokens + "/" + Type4LrudTokens +
                " graph=" + GraphDigestAggregate + " overlay=" + OverlayDigest + " canonical=" + CanonicalDigest;

            public void Add(AttemptRecord record)
            {
                if (record.Completed) Completed++; else Invalid++;
                if (record.RetryRequired) Retry++;
                Unresolved += record.UnresolvedCount;
                if (record.Completed && record.ReachableTerminals != 7) TerminalReachabilityFailures++;
                if (record.Completed && record.MaskCounts != "20/4/4/17/0/0/2") RouteMaskMismatches++;
                if (record.Completed && record.Type4UdTokens + record.Type4LudTokens +
                    record.Type4RudTokens + record.Type4LrudTokens != 19) Type4UdMissing++;
                if (record.Completed && (record.Type4LudTokens != 0 || record.Type4RudTokens != 0)) Type4LrMismatches++;
                if (record.Completed && (record.DirectedEdges != 96 || record.UndirectedEdges * 2 != record.DirectedEdges))
                    EdgeReciprocityFailures++;
                if (record.Completed && record.GeneratedEdgeRows != record.DirectedEdges) GeneratedEdgeBijectionFailures++;
                if (record.Completed && record.RuleSummary != "12/12/0/0/0") ValidationFailures++;
                if (record.OverlaySnapshot != null) OverlaySnapshots++;
                UnexpectedDependencies += record.SourceMutationCount + record.FileReadCount + record.FileWriteCount +
                    record.ClockReadCount + record.RngDrawCount;
                Type4UdTokens += record.Type4UdTokens;
                Type4LudTokens += record.Type4LudTokens;
                Type4RudTokens += record.Type4RudTokens;
                Type4LrudTokens += record.Type4LrudTokens;
                if (stableOverlayDigest == null) stableOverlayDigest = record.OverlayDigest;
                else if (!string.Equals(stableOverlayDigest, record.OverlayDigest, StringComparison.Ordinal)) ValidationFailures++;
                Append(canonicalHash, record.Canonical);
                Append(graphHash, record.GraphEdgeDigest);
            }

            public void Finish()
            {
                GraphDigestAggregate = Hex(graphHash.GetHashAndReset());
                OverlayDigest = stableOverlayDigest ?? string.Empty;
                CanonicalDigest = Hex(canonicalHash.GetHashAndReset());
                graphHash.Dispose();
                canonicalHash.Dispose();
            }

            private static void Append(IncrementalHash hash, string value)
            {
                hash.AppendData(Encoding.UTF8.GetBytes(value ?? string.Empty));
                hash.AppendData(new byte[] { 10 });
            }
        }
    }
}
