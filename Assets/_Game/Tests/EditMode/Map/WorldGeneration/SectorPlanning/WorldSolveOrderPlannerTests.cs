using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.SectorPlanning;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.SectorPlanning
{
    [TestFixture]
    [Category("MAP15_01")]
    public sealed class WorldSolveOrderPlannerTests
    {
        private ReferenceWorldPlanFixture fixture;

        [SetUp]
        public void SetUp()
        {
            fixture = ReferenceWorldPlanFixture.Create();
        }

        [Test]
        public void WorldPlanInputPublishesExact169SectorTopologyAndDigests()
        {
            var input = fixture.Input();
            var result = WorldSolveOrderPlanner.Plan(input);

            Assert.That(WorldPlanInput.WorldWidthTiles, Is.EqualTo(624));
            Assert.That(WorldPlanInput.WorldHeightTiles, Is.EqualTo(416));
            Assert.That(WorldPlanInput.SectorWidthTiles, Is.EqualTo(48));
            Assert.That(WorldPlanInput.SectorHeightTiles, Is.EqualTo(32));
            Assert.That(WorldPlanInput.SectorColumns, Is.EqualTo(13));
            Assert.That(WorldPlanInput.SectorRows, Is.EqualTo(13));
            Assert.That(WorldPlanInput.SectorCount, Is.EqualTo(169));
            Assert.That(input.Nodes.Count, Is.EqualTo(169));
            Assert.That(input.Nodes.Select(value => value.Id).Distinct().Count(), Is.EqualTo(169));
            Assert.That(input.Nodes.Select(value => value.Coordinate).Distinct().Count(), Is.EqualTo(169));
            Assert.That(input.Nodes.Count(value => !value.Coordinate.IsInBounds), Is.Zero);
            Assert.That(input.Nodes.All(value => value.Id == value.Coordinate.RowMajorId), Is.True);
            Assert.That(input.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(result.OutputDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(
                () => ((IList<WorldSectorNode>)input.Nodes).Add(input.Nodes[0]),
                Throws.TypeOf<NotSupportedException>());

            TestContext.Progress.WriteLine("MAP15_01_INPUT_DIGEST=" + input.CanonicalDigest);
            TestContext.Progress.WriteLine("MAP15_01_OUTPUT_DIGEST=" + result.OutputDigest);
            TestContext.Progress.WriteLine("MAP15_01_EDGE_COUNTS=" + string.Join(",", input.Dependencies
                .GroupBy(value => value.Kind)
                .OrderBy(value => value.Key)
                .Select(value => value.Key + ":" + value.Count())));
            TestContext.Progress.WriteLine("MAP15_01_FIRST_CONSTRAINED=" + string.Join(",", result.Steps.Take(10)
                .Select(value => value.SectorId + ":" + value.Priority)));
        }

        [Test]
        public void SolveOrderContainsEachSectorExactlyOnce()
        {
            var result = fixture.Plan();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(result.Steps.Count, Is.EqualTo(169));
            Assert.That(result.Steps.Select(value => value.SectorId).Distinct().Count(), Is.EqualTo(169));
            Assert.That(result.Steps.Select(value => value.StepIndex), Is.EqualTo(Enumerable.Range(0, 169)));
            Assert.That(result.Steps.Select(value => value.SectorId.Value).OrderBy(value => value),
                Is.EqualTo(Enumerable.Range(0, 169)));
        }

        [Test]
        public void DependencyGraphIsAcyclicAndPrerequisitesPrecedeDependents()
        {
            var input = fixture.Input();
            var result = WorldSolveOrderPlanner.Plan(input);
            var order = result.Steps.ToDictionary(value => value.SectorId, value => value.StepIndex);

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(input.Dependencies.Count, Is.EqualTo(45));
            Assert.That(input.Dependencies.Count(value => order[value.FromSector] >= order[value.ToSector]), Is.Zero);
            Assert.That(result.Steps.Sum(value => value.PrerequisiteSectorIds.Count),
                Is.EqualTo(input.Dependencies.Select(value => new { value.FromSector, value.ToSector }).Distinct().Count()));
            Assert.That(result.Failures.Count(value => value.Code == WorldSolveFailureCode.CycleDetected), Is.Zero);
        }

        [Test]
        public void SpecialRouteBoundaryConstraintsHavePriorityReasons()
        {
            var input = fixture.Input();
            var result = WorldSolveOrderPlanner.Plan(input);
            var byId = input.Nodes.ToDictionary(value => value.Id);
            var specialSteps = result.Steps.Where(value => byId[value.SectorId].HasSpecialReservation).ToArray();
            var boundarySteps = result.Steps.Where(value => byId[value.SectorId].IsBoundaryPair).ToArray();
            var routeSteps = result.Steps.Where(value => byId[value.SectorId].IsMandatoryRoute).ToArray();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(specialSteps.Length, Is.EqualTo(3));
            Assert.That(specialSteps.All(value => value.Priority == WorldSolvePriority.FixedSpecial), Is.True);
            Assert.That(boundarySteps.Length, Is.EqualTo(6));
            Assert.That(boundarySteps.All(value => value.Priority == WorldSolvePriority.MandatoryRouteOrBoundary), Is.True);
            Assert.That(routeSteps.Length, Is.EqualTo(16));
            Assert.That(routeSteps.All(value => value.Priority <= WorldSolvePriority.MandatoryRouteOrBoundary), Is.True);
            Assert.That(result.Steps.Take(10).Any(value => value.Priority == WorldSolvePriority.FixedSpecial), Is.True);
            Assert.That(result.Steps.All(value => WorldSolveDigest.IsLowerHexSha256(value.ReasonDigest)), Is.True);

            Assert.That(Count(input, WorldDependencyKind.SpecialReservation), Is.EqualTo(3));
            Assert.That(Count(input, WorldDependencyKind.MandatoryRoute), Is.EqualTo(15));
            Assert.That(Count(input, WorldDependencyKind.BoundaryPair), Is.EqualTo(6));
            Assert.That(Count(input, WorldDependencyKind.ExternalSocket), Is.EqualTo(4));
            Assert.That(Count(input, WorldDependencyKind.NeighborContinuity), Is.EqualTo(8));
            Assert.That(Count(input, WorldDependencyKind.PacingWindow), Is.EqualTo(6));
            Assert.That(Count(input, WorldDependencyKind.RetryGuard), Is.EqualTo(3));
        }

        [Test]
        public void SolveOrderIsDeterministicAcrossRepeatReverseAndCulture()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var first = fixture.Plan();
                var repeat = fixture.Plan();
                var reversed = WorldSolveOrderPlanner.Plan(fixture.Input(
                    fixture.Nodes.Reverse(),
                    fixture.Edges.Reverse()));
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var culture = fixture.Plan();

                var results = new[] { first, repeat, reversed, culture };
                Assert.That(results.All(value => value.Success), Is.True,
                    string.Join(";", results.Select(Join)));
                Assert.That(results.Select(value => value.InputDigest).Distinct().Count(), Is.EqualTo(1));
                Assert.That(results.Select(value => value.OutputDigest).Distinct().Count(), Is.EqualTo(1));
                Assert.That(results.Select(value => string.Join(",", value.Steps.Select(step => step.SectorId.Value)))
                    .Distinct().Count(), Is.EqualTo(1));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Test]
        public void RetryEnvelopeDoesNotExecuteRngOrWholeWorldRerandom()
        {
            var result = fixture.Plan();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(result.RetryEnvelope.MaxSectorLocalAttemptsPerNode, Is.EqualTo(6));
            Assert.That(result.RetryEnvelope.DependencyRollbackRadius, Is.EqualTo(1));
            Assert.That(result.RetryEnvelope.AbortReason,
                Is.EqualTo(WorldSolveAbortReason.SectorLocalAttemptsExhausted));
            Assert.That(result.NewRngDrawCount, Is.Zero);
            Assert.That(result.WholeWorldRerandom, Is.False);
            Assert.That(result.FallbackCarveCount, Is.Zero);
            Assert.That(WorldSolveOrderPlanner.NewRngDrawCount, Is.Zero);
        }

        [Test]
        public void InvalidWorldInputsFailAtomicallyWithoutPartialPlan()
        {
            var tooFew = fixture.Input(fixture.Nodes.Take(168), fixture.Edges);
            var duplicateNodes = fixture.Nodes.Take(168).Concat(new[] { fixture.Nodes[0] });
            var duplicate = fixture.Input(duplicateNodes, fixture.Edges);
            var outOfBoundsNodes = fixture.Nodes.Take(168).Concat(new[]
            {
                fixture.Node(168, 13, 12, false, false, false),
            });
            var outOfBounds = fixture.Input(outOfBoundsNodes, fixture.Edges);
            var self = fixture.Input(fixture.Nodes, fixture.Edges.Concat(new[]
            {
                fixture.Edge(5, 5, WorldDependencyKind.NeighborContinuity, "SELF"),
            }));
            var missingEndpoint = fixture.Input(fixture.Nodes, fixture.Edges.Concat(new[]
            {
                fixture.Edge(0, 999, WorldDependencyKind.NeighborContinuity, "MISSING"),
            }));
            var cycle = fixture.Input(fixture.Nodes, fixture.Edges.Concat(new[]
            {
                fixture.Edge(1, 0, WorldDependencyKind.NeighborContinuity, "CYCLE"),
            }));
            var missingRequired = fixture.Input(fixture.Nodes,
                fixture.Edges.Where(value => !(value.ToSector.Value == 14 &&
                                                value.Kind == WorldDependencyKind.SpecialReservation)));
            var wholeWorld = fixture.Input(fixture.Nodes, fixture.Edges,
                new WorldRetryEnvelope(6, 1, WorldSolveAbortReason.SectorLocalAttemptsExhausted, true));

            var results = new[]
            {
                WorldSolveOrderPlanner.Plan(null),
                WorldSolveOrderPlanner.Plan(tooFew),
                WorldSolveOrderPlanner.Plan(duplicate),
                WorldSolveOrderPlanner.Plan(outOfBounds),
                WorldSolveOrderPlanner.Plan(self),
                WorldSolveOrderPlanner.Plan(missingEndpoint),
                WorldSolveOrderPlanner.Plan(cycle),
                WorldSolveOrderPlanner.Plan(missingRequired),
                WorldSolveOrderPlanner.Plan(wholeWorld),
            };

            Assert.That(results.All(value => !value.Success), Is.True);
            Assert.That(results.All(value => value.Input == null), Is.True);
            Assert.That(results.All(value => value.Steps.Count == 0), Is.True);
            Assert.That(results.All(value => value.InputDigest == string.Empty), Is.True);
            Assert.That(results.All(value => value.OutputDigest == string.Empty), Is.True);
            Assert.That(results.All(value => value.Failures.Count != 0), Is.True);
            Assert.That(results.SelectMany(value => value.Failures).Select(value => value.Code),
                Does.Contain(WorldSolveFailureCode.CycleDetected));
            Assert.That(results.SelectMany(value => value.Failures).Select(value => value.Code),
                Does.Contain(WorldSolveFailureCode.MissingRequiredDependency));
            Assert.That(results.SelectMany(value => value.Failures).Select(value => value.Code),
                Does.Contain(WorldSolveFailureCode.WholeWorldRerandomRequired));
        }

        [Test]
        public void WorldPlanDoesNotMutateSectorPlannerOrAuthoringAssets()
        {
            var input = fixture.Input();
            var beforeInputDigest = input.CanonicalDigest;
            var beforeNodes = input.Nodes.Select(NodeIdentity).ToArray();
            var beforeEdges = input.Dependencies.Select(value => value.ToString()).ToArray();
            var result = WorldSolveOrderPlanner.Plan(input);

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(input.CanonicalDigest, Is.EqualTo(beforeInputDigest));
            Assert.That(input.Nodes.Select(NodeIdentity), Is.EqualTo(beforeNodes));
            Assert.That(input.Dependencies.Select(value => value.ToString()), Is.EqualTo(beforeEdges));
            Assert.That(input.GeneratedFileWriteCount, Is.Zero);
            Assert.That(input.TilemapMutationCount, Is.Zero);
            Assert.That(input.SceneMutationCount, Is.Zero);
            Assert.That(input.PrefabMutationCount, Is.Zero);
            Assert.That(input.GameObjectMutationCount, Is.Zero);
            Assert.That(input.GameplaySpawnCount, Is.Zero);
            Assert.That(input.SectorPlannerMutationCount, Is.Zero);
        }

        [Test]
        public void Map15HandoffKeepsMap15_02Locked()
        {
            var result = fixture.Plan();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(WorldSolveOrderPlanner.DownstreamOwner,
                Is.EqualTo("MAP15_02_INTEGRATE_INTERSECTOR_SOCKETS_AND_BOUNDARIES"));
            Assert.That(WorldSolveOrderPlanner.OpensDownstreamTask, Is.False);
            Assert.That(fixture.Input().PublicationLabel,
                Is.EqualTo(WorldSolveOrderPlanner.ReferencePublicationLabel));
            Assert.That(result.Steps.Count, Is.EqualTo(169));
        }

        private static int Count(WorldPlanInput input, WorldDependencyKind kind) =>
            input.Dependencies.Count(value => value.Kind == kind);

        private static string NodeIdentity(WorldSectorNode value) => string.Join("|", new[]
        {
            value.Id.ToString(), value.Coordinate.ToString(), value.PrimaryBiome,
            value.RouteType.ToString(CultureInfo.InvariantCulture), value.AccessClass.ToString(),
            value.PacingRole.ToString(), value.HasSpecialReservation.ToString(),
            value.IsBoundaryPair.ToString(), value.HasExternalSocketObligation.ToString(),
            value.IsWorldStart.ToString(), value.SpecialReservationId,
        });

        private static string Join(WorldSolveOrderResult result) =>
            result == null ? "null" : string.Join(";", result.Failures.Select(value => value.ToString()));

        private sealed class ReferenceWorldPlanFixture
        {
            internal const string Map14PhaseExitDigest =
                "5b8ed6a3c8b0a20fe2f2d05eea0b7731522aff30e691ca606c638adcdbd62d82";

            private ReferenceWorldPlanFixture(WorldSectorNode[] nodes, WorldDependencyEdge[] edges)
            {
                Nodes = nodes;
                Edges = edges;
            }

            internal WorldSectorNode[] Nodes { get; }
            internal WorldDependencyEdge[] Edges { get; }

            internal static ReferenceWorldPlanFixture Create()
            {
                var nodes = Enumerable.Range(0, WorldPlanInput.SectorCount)
                    .Select(CreateNode)
                    .ToArray();
                var edges = new List<WorldDependencyEdge>();

                for (var id = 1; id <= 12; id++)
                    edges.Add(CreateEdge(id - 1, id, WorldDependencyKind.MandatoryRoute, "ROW_MAJOR_MANDATORY_ROUTE"));
                foreach (var id in new[] { 14, 28, 42 })
                {
                    edges.Add(CreateEdge(0, id, WorldDependencyKind.SpecialReservation, "FIXED_SPECIAL_ENTRY_RETURN"));
                    edges.Add(CreateEdge(0, id, WorldDependencyKind.MandatoryRoute, "SPECIAL_MANDATORY_ROUTE"));
                }
                foreach (var id in Enumerable.Range(56, 6))
                    edges.Add(CreateEdge(0, id, WorldDependencyKind.BoundaryPair, "APPROVED_BOUNDARY_PAIR"));
                foreach (var id in Enumerable.Range(70, 4))
                    edges.Add(CreateEdge(0, id, WorldDependencyKind.ExternalSocket, "EXTERNAL_SOCKET_OBLIGATION"));
                foreach (var id in Enumerable.Range(80, 8))
                    edges.Add(CreateEdge(0, id, WorldDependencyKind.NeighborContinuity, "NEIGHBOR_CONTEXT"));
                foreach (var id in Enumerable.Range(90, 6))
                    edges.Add(CreateEdge(0, id, WorldDependencyKind.PacingWindow, "PACING_LANDMARK_RESOURCE_WINDOW"));
                foreach (var id in Enumerable.Range(100, 3))
                    edges.Add(CreateEdge(0, id, WorldDependencyKind.RetryGuard, "SECTOR_LOCAL_RETRY_GUARD"));

                return new ReferenceWorldPlanFixture(nodes, edges.OrderBy(value => value).ToArray());
            }

            internal WorldPlanInput Input(
                IEnumerable<WorldSectorNode> nodes = null,
                IEnumerable<WorldDependencyEdge> edges = null,
                WorldRetryEnvelope retry = null,
                int generatedFileWriteCount = 0,
                int tilemapMutationCount = 0,
                int sceneMutationCount = 0,
                int prefabMutationCount = 0,
                int gameObjectMutationCount = 0,
                int gameplaySpawnCount = 0,
                int sectorPlannerMutationCount = 0)
            {
                return new WorldPlanInput(
                    nodes ?? Nodes,
                    edges ?? Edges,
                    retry ?? new WorldRetryEnvelope(
                        6, 1, WorldSolveAbortReason.SectorLocalAttemptsExhausted),
                    Map14PhaseExitDigest,
                    WorldSolveOrderPlanner.ReferencePublicationLabel,
                    generatedFileWriteCount,
                    tilemapMutationCount,
                    sceneMutationCount,
                    prefabMutationCount,
                    gameObjectMutationCount,
                    gameplaySpawnCount,
                    sectorPlannerMutationCount);
            }

            internal WorldSolveOrderResult Plan() => WorldSolveOrderPlanner.Plan(Input());

            internal WorldSectorNode Node(
                int id,
                int x,
                int y,
                bool special,
                bool boundary,
                bool socket) =>
                new WorldSectorNode(
                    new WorldSectorId(id), new WorldSectorCoordinate(x, y), "BIO_MOON_CRATER",
                    0, AccessClass.OptionalNoTool, PacingRole.Quiet, special, boundary, socket,
                    specialReservationId: special ? "SPECIAL_INVALID" : string.Empty);

            internal WorldDependencyEdge Edge(
                int from,
                int to,
                WorldDependencyKind kind,
                string reason) => CreateEdge(from, to, kind, reason);

            private static WorldSectorNode CreateNode(int id)
            {
                var x = id % WorldPlanInput.SectorColumns;
                var y = id / WorldPlanInput.SectorColumns;
                var special = id == 14 || id == 28 || id == 42;
                var boundary = id >= 56 && id <= 61;
                var socket = id >= 70 && id <= 73;
                var mandatory = id <= 12 || special;
                var role = PacingRole.Quiet;
                if (id == 14) role = PacingRole.Safe;
                else if (id == 28) role = PacingRole.Resource;
                else if (id == 42) role = PacingRole.Boss;
                else if (id == 90) role = PacingRole.Landmark;
                else if (id == 91) role = PacingRole.Resource;
                else if (id == 92) role = PacingRole.Discovery;
                else if (id == 93) role = PacingRole.Recovery;
                else if (id == 94) role = PacingRole.Reward;
                else if (id == 95) role = PacingRole.Boss;
                else if (mandatory) role = PacingRole.Traversal;

                return new WorldSectorNode(
                    new WorldSectorId(id),
                    new WorldSectorCoordinate(x, y),
                    Biome(id),
                    mandatory ? 1 : 0,
                    mandatory ? AccessClass.MandatoryNoTool : socket ? AccessClass.OptionalTool : AccessClass.OptionalNoTool,
                    role,
                    special,
                    boundary,
                    socket,
                    id == 0,
                    special ? Special(id) : string.Empty);
            }

            private static WorldDependencyEdge CreateEdge(
                int from,
                int to,
                WorldDependencyKind kind,
                string reason) =>
                new WorldDependencyEdge(
                    new WorldSectorId(from),
                    new WorldSectorId(to),
                    kind,
                    reason,
                    Owner(kind));

            private static string Owner(WorldDependencyKind kind)
            {
                switch (kind)
                {
                    case WorldDependencyKind.SpecialReservation: return "MAP13_SPECIAL_REGION";
                    case WorldDependencyKind.MandatoryRoute: return "MAP05_MANDATORY_ROUTE";
                    case WorldDependencyKind.BoundaryPair: return "MAP08_BOUNDARY";
                    case WorldDependencyKind.ExternalSocket: return "MAP14_EXTERNAL_SOCKET";
                    case WorldDependencyKind.NeighborContinuity: return "MAP14_NEIGHBOR_CONTEXT";
                    case WorldDependencyKind.PacingWindow: return "MAP14_PACING";
                    case WorldDependencyKind.RetryGuard: return "MAP14_RETRY";
                    default: throw new ArgumentOutOfRangeException(nameof(kind));
                }
            }

            private static string Biome(int id)
            {
                switch ((id / 13) % 4)
                {
                    case 0: return "BIO_MOON_CRATER";
                    case 1: return "BIO_CASSIA_ROOT";
                    case 2: return "BIO_ABANDONED_MILL";
                    default: return "BIO_MOON_DOUGH";
                }
            }

            private static string Special(int id)
            {
                if (id == 14) return "SPECIAL_VILLAGE";
                if (id == 28) return "SPECIAL_CORE_RESOURCE";
                return "SPECIAL_BOSS";
            }
        }
    }
}
