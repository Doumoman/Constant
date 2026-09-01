using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.SectorPlanning;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.SectorPlanning
{
    [TestFixture]
    [Category("MAP15_02")]
    public sealed class WorldBoundarySocketIntegratorTests
    {
        private ReferenceIntersectorFixture fixture;

        [SetUp]
        public void SetUp()
        {
            fixture = ReferenceIntersectorFixture.Create();
        }

        [Test]
        public void WorldIntersectorPlanPublishesExact312InternalEdgesAndDigests()
        {
            var result = fixture.Integrate();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(result.Plan.Edges.Count, Is.EqualTo(312));
            Assert.That(result.Plan.HorizontalCount, Is.EqualTo(156));
            Assert.That(result.Plan.VerticalCount, Is.EqualTo(156));
            Assert.That(result.Plan.EndpointActualCount, Is.EqualTo(624));
            Assert.That(result.Plan.Edges.Select(value => value.Id).Distinct().Count(), Is.EqualTo(312));
            Assert.That(result.Plan.InputDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(result.Plan.OutputDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(
                () => ((IList<WorldIntersectorEdge>)result.Plan.Edges).Add(result.Plan.Edges[0]),
                Throws.TypeOf<NotSupportedException>());

            TestContext.Progress.WriteLine("MAP15_02_INPUT_DIGEST=" + result.Plan.InputDigest);
            TestContext.Progress.WriteLine("MAP15_02_OUTPUT_DIGEST=" + result.Plan.OutputDigest);
            TestContext.Progress.WriteLine("MAP15_02_EDGE_COUNTS=" +
                result.Plan.HorizontalCount + "," + result.Plan.VerticalCount + "," +
                result.Plan.Edges.Count + "," + result.Plan.EndpointActualCount);
        }

        [Test]
        public void EveryInternalEdgeHasTwoFacingEndpointsAndSideAnchors()
        {
            var result = fixture.Integrate();
            var edges = result.Plan.Edges;

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(edges.Count(value => value.Endpoints.Count != 2), Is.Zero);
            Assert.That(edges.SelectMany(value => value.Endpoints).Count(), Is.EqualTo(624));
            Assert.That(edges.SelectMany(value => value.Endpoints)
                .Count(value => value.Anchor == null || !value.Anchor.IsInBounds), Is.Zero);
            Assert.That(edges.SelectMany(value => value.Endpoints)
                .Count(value => !value.Anchor.IsOnSide(value.Side)), Is.Zero);
            Assert.That(edges.Count(value =>
                WorldBoundarySocketIntegrator.Opposite(value.Endpoints[0].Side) != value.Endpoints[1].Side), Is.Zero);
            Assert.That(edges.Count(value => value.Endpoints.Select(endpoint => endpoint.SectorId).Distinct().Count() != 2),
                Is.Zero);
        }

        [Test]
        public void BoundaryPairsBindApprovedProfilesAndWarningEvidence()
        {
            var result = fixture.Integrate();
            var required = fixture.Projections.Count(value => value.RequiresBoundaryBinding) / 2;
            var boundaries = result.Plan.Edges.Where(value => value.IsBoundary).ToArray();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(required, Is.EqualTo(6));
            Assert.That(boundaries.Length, Is.EqualTo(required));
            Assert.That(boundaries.Count(value => value.Boundary == null), Is.Zero);
            Assert.That(boundaries.Count(value => !WorldBoundarySocketIntegrator.IsApprovedBoundaryBinding(value.Boundary)),
                Is.Zero);
            Assert.That(boundaries.Count(value => value.Boundary.WarningModalities.Distinct().Count() <
                                                   MoonpalaceBiomePairDefinition.RequiredMinimumWarningMarkerCount),
                Is.Zero);
            Assert.That(boundaries.Select(value => value.Boundary.PairId).Distinct().Count(), Is.EqualTo(6));
        }

        [Test]
        public void MandatoryRouteAndExternalSocketEdgesHaveCompatibleOpenings()
        {
            var result = fixture.Integrate();
            var mandatory = result.Plan.Edges.Where(value => value.RouteSignature.MandatoryRoute).ToArray();
            var external = result.Plan.Edges.Where(value => value.RouteSignature.ExternalSocket).ToArray();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(result.Plan.Edges.Count(value => !value.RouteSignature.Compatible), Is.Zero);
            Assert.That(mandatory.Length, Is.EqualTo(1));
            Assert.That(external.Length, Is.EqualTo(1));
            Assert.That(mandatory.SelectMany(value => value.Endpoints).All(value => value.IsOpen), Is.True);
            Assert.That(mandatory.SelectMany(value => value.Endpoints)
                .All(value => value.AccessClass == AccessClass.MandatoryNoTool), Is.True);
            Assert.That(external.SelectMany(value => value.Endpoints).All(value => value.IsOpen), Is.True);
        }

        [Test]
        public void Type4AndType0SocketRulesPreserveApprovedSemantics()
        {
            Assert.That(WorldBoundarySocketIntegrator.IsSideOpen(0, WorldSectorSide.West, false), Is.False);
            Assert.That(WorldBoundarySocketIntegrator.IsSideOpen(0, WorldSectorSide.North, false), Is.False);
            Assert.That(WorldBoundarySocketIntegrator.IsSideOpen(0, WorldSectorSide.West, true), Is.True);
            Assert.That(WorldBoundarySocketIntegrator.IsSideOpen(0, WorldSectorSide.North, true), Is.True);

            Assert.That(WorldBoundarySocketIntegrator.IsSideOpen(4, WorldSectorSide.North, false), Is.True);
            Assert.That(WorldBoundarySocketIntegrator.IsSideOpen(4, WorldSectorSide.South, false), Is.True);
            Assert.That(WorldBoundarySocketIntegrator.IsSideOpen(4, WorldSectorSide.West, false), Is.False);
            Assert.That(WorldBoundarySocketIntegrator.IsSideOpen(4, WorldSectorSide.East, false), Is.False);
            Assert.That(WorldBoundarySocketIntegrator.IsSideOpen(4, WorldSectorSide.West, true), Is.True);

            Assert.That(WorldBoundarySocketIntegrator.IsSideOpen(1, WorldSectorSide.West, false), Is.True);
            Assert.That(WorldBoundarySocketIntegrator.IsSideOpen(1, WorldSectorSide.North, false), Is.False);
            Assert.That(WorldBoundarySocketIntegrator.IsSideOpen(2, WorldSectorSide.South, false), Is.True);
            Assert.That(WorldBoundarySocketIntegrator.IsSideOpen(3, WorldSectorSide.North, false), Is.True);
        }

        [Test]
        public void TraversalApronsAndEdgeSignaturesAreStableAndNonEmpty()
        {
            var first = fixture.Integrate();
            var repeat = fixture.Integrate();

            Assert.That(first.Success, Is.True, Join(first));
            Assert.That(first.Plan.Edges.SelectMany(value => value.Endpoints)
                .Count(value => value.Apron == null || !value.Apron.IsInBounds ||
                                !value.Apron.Contains(value.Anchor) || value.Apron.CellCount == 0), Is.Zero);
            Assert.That(first.Plan.Edges.Count(value => !WorldSolveDigest.IsLowerHexSha256(value.CanonicalDigest)),
                Is.Zero);
            Assert.That(first.Plan.Edges.Count(value => !WorldSolveDigest.IsLowerHexSha256(
                value.RouteSignature.CanonicalDigest)), Is.Zero);
            Assert.That(first.Plan.Edges.Select(value => value.CanonicalDigest),
                Is.EqualTo(repeat.Plan.Edges.Select(value => value.CanonicalDigest)));
        }

        [Test]
        public void IntersectorIntegrationIsDeterministicAcrossRepeatReverseAndCulture()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var first = fixture.Integrate();
                var repeat = fixture.Integrate();
                var reversed = WorldBoundarySocketIntegrator.Integrate(
                    fixture.Request(fixture.Projections.Reverse(), fixture.Bindings.Reverse()));
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                var culture = fixture.Integrate();

                var results = new[] { first, repeat, reversed, culture };
                Assert.That(results.All(value => value.Success), Is.True,
                    string.Join(";", results.Select(Join)));
                Assert.That(results.Select(value => value.InputDigest).Distinct().Count(), Is.EqualTo(1));
                Assert.That(results.Select(value => value.OutputDigest).Distinct().Count(), Is.EqualTo(1));
                Assert.That(results.Select(value => string.Join(",", value.Plan.Edges.Select(edge => edge.Id)))
                    .Distinct().Count(), Is.EqualTo(1));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Test]
        public void InvalidEdgeInputsFailAtomicallyWithoutPartialPlan()
        {
            var missing = fixture.Projections.Skip(1).ToArray();
            var duplicate = fixture.Projections.Concat(new[] { fixture.Projections[0] }).ToArray();
            var badAnchor = ReplaceProjection(
                fixture.Projections,
                fixture.Projections[0],
                new WorldSocketProjection(
                    fixture.Projections[0].SectorId,
                    fixture.Projections[0].Side,
                    new WorldSocketAnchor(1, 16, 3),
                    fixture.Projections[0].ExplicitSocketEvidence,
                    fixture.Projections[0].RequiresMandatoryContinuity,
                    fixture.Projections[0].RequiresBoundaryBinding,
                    fixture.Projections[0].SourceOwner));
            var mandatoryProjection = fixture.Projections.Single(value =>
                value.SectorId == new WorldSectorId(1) && value.Side == WorldSectorSide.West);
            var asymmetricMandatory = ReplaceProjection(
                fixture.Projections,
                mandatoryProjection,
                new WorldSocketProjection(
                    mandatoryProjection.SectorId,
                    mandatoryProjection.Side,
                    mandatoryProjection.Anchor,
                    mandatoryProjection.ExplicitSocketEvidence,
                    false,
                    mandatoryProjection.RequiresBoundaryBinding,
                    mandatoryProjection.SourceOwner));
            var oneWarning = fixture.Bindings[0];
            var insufficientWarnings = fixture.Bindings.Skip(1).Concat(new[]
            {
                new WorldBoundaryBinding(
                    oneWarning.EdgeId,
                    oneWarning.PairId,
                    oneWarning.ProfileId,
                    oneWarning.CandidateId,
                    new[] { MoonpalaceBoundaryWarningMarkerCategory.Tile.Token },
                    oneWarning.SourceOwner),
            }).ToArray();

            var results = new[]
            {
                WorldBoundarySocketIntegrator.Integrate(null),
                WorldBoundarySocketIntegrator.Integrate(fixture.Request(missing, fixture.Bindings)),
                WorldBoundarySocketIntegrator.Integrate(fixture.Request(duplicate, fixture.Bindings)),
                WorldBoundarySocketIntegrator.Integrate(fixture.Request(badAnchor, fixture.Bindings)),
                WorldBoundarySocketIntegrator.Integrate(fixture.Request(asymmetricMandatory, fixture.Bindings)),
                WorldBoundarySocketIntegrator.Integrate(fixture.Request(fixture.Projections, fixture.Bindings.Skip(1))),
                WorldBoundarySocketIntegrator.Integrate(fixture.Request(fixture.Projections, insufficientWarnings)),
                WorldBoundarySocketIntegrator.Integrate(fixture.Request(
                    fixture.Projections, fixture.Bindings, map14HandoffDigest: "INVALID")),
                WorldBoundarySocketIntegrator.Integrate(fixture.Request(
                    fixture.Projections, fixture.Bindings, fallbackCarveCount: 1)),
                WorldBoundarySocketIntegrator.Integrate(fixture.Request(
                    fixture.Projections, fixture.Bindings, tilemapMutationCount: 1)),
            };

            Assert.That(results.All(value => !value.Success), Is.True);
            Assert.That(results.All(value => value.Plan == null), Is.True);
            Assert.That(results.All(value => value.InputDigest == string.Empty), Is.True);
            Assert.That(results.All(value => value.OutputDigest == string.Empty), Is.True);
            Assert.That(results.All(value => value.Failures.Count != 0), Is.True);
            Assert.That(results.SelectMany(value => value.Failures).Select(value => value.Code),
                Does.Contain(WorldIntersectorFailureCode.MissingCounterpartEndpoint));
            Assert.That(results.SelectMany(value => value.Failures).Select(value => value.Code),
                Does.Contain(WorldIntersectorFailureCode.BoundaryWarningInsufficient));
            Assert.That(results.SelectMany(value => value.Failures).Select(value => value.Code),
                Does.Contain(WorldIntersectorFailureCode.MutationClaim));
        }

        [Test]
        public void WorldEdgePlanDoesNotMutateSectorPlannerWorldPlanOrAuthoringAssets()
        {
            var worldInputDigest = fixture.WorldPlan.CanonicalDigest;
            var solveOutputDigest = fixture.SolveOrder.OutputDigest;
            var nodes = fixture.WorldPlan.Nodes.Select(NodeIdentity).ToArray();
            var dependencies = fixture.WorldPlan.Dependencies.Select(value => value.ToString()).ToArray();
            var boundarySignature = MoonpalaceBiomePairCatalog.Canonical.Signature;
            var result = fixture.Integrate();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(fixture.WorldPlan.CanonicalDigest, Is.EqualTo(worldInputDigest));
            Assert.That(fixture.SolveOrder.OutputDigest, Is.EqualTo(solveOutputDigest));
            Assert.That(fixture.WorldPlan.Nodes.Select(NodeIdentity), Is.EqualTo(nodes));
            Assert.That(fixture.WorldPlan.Dependencies.Select(value => value.ToString()), Is.EqualTo(dependencies));
            Assert.That(MoonpalaceBiomePairCatalog.Canonical.Signature, Is.EqualTo(boundarySignature));
            Assert.That(result.Plan.NewRngDrawCount, Is.Zero);
            Assert.That(result.Plan.FallbackCarveCount, Is.Zero);
            Assert.That(result.Plan.GeneratedFileWriteCount, Is.Zero);
            Assert.That(result.Plan.TilemapMutationCount, Is.Zero);
            Assert.That(result.Plan.SceneMutationCount, Is.Zero);
            Assert.That(result.Plan.PrefabMutationCount, Is.Zero);
            Assert.That(result.Plan.GameObjectMutationCount, Is.Zero);
            Assert.That(result.Plan.GameplaySpawnCount, Is.Zero);
            Assert.That(result.Plan.SectorPlannerMutationCount, Is.Zero);
            Assert.That(result.Plan.WorldPlanMutationCount, Is.Zero);
        }

        [Test]
        public void Map15HandoffKeepsMap15_03Locked()
        {
            var result = fixture.Integrate();

            Assert.That(result.Success, Is.True, Join(result));
            Assert.That(WorldIntersectorEdgePlan.DownstreamOwner,
                Is.EqualTo("MAP15_03_IMPLEMENT_MULTI_SECTOR_SPECIAL_AND_CLUSTER_POLICY"));
            Assert.That(WorldIntersectorEdgePlan.OpensDownstreamTask, Is.False);
            Assert.That(result.Plan.Request.PublicationLabel,
                Is.EqualTo(WorldBoundarySocketIntegrator.ReferencePublicationLabel));
            Assert.That(result.Plan.Edges.Count, Is.EqualTo(312));
        }

        private static WorldSocketProjection[] ReplaceProjection(
            IEnumerable<WorldSocketProjection> source,
            WorldSocketProjection original,
            WorldSocketProjection replacement) =>
            source.Where(value => !ReferenceEquals(value, original)).Concat(new[] { replacement }).ToArray();

        private static string NodeIdentity(WorldSectorNode value) => string.Join("|", new[]
        {
            value.Id.ToString(), value.Coordinate.ToString(), value.PrimaryBiome,
            value.RouteType.ToString(CultureInfo.InvariantCulture), value.AccessClass.ToString(),
            value.PacingRole.ToString(), value.HasSpecialReservation.ToString(),
            value.IsBoundaryPair.ToString(), value.HasExternalSocketObligation.ToString(),
            value.IsWorldStart.ToString(), value.SpecialReservationId,
        });

        private static string Join(WorldIntersectorBuildResult result) =>
            result == null ? "null" : string.Join(";", result.Failures.Select(value => value.ToString()));

        private sealed class ReferenceIntersectorFixture
        {
            internal const string Map14PhaseExitDigest =
                "5b8ed6a3c8b0a20fe2f2d05eea0b7731522aff30e691ca606c638adcdbd62d82";

            private ReferenceIntersectorFixture(
                WorldPlanInput worldPlan,
                WorldSolveOrderResult solveOrder,
                WorldSocketProjection[] projections,
                WorldBoundaryBinding[] bindings)
            {
                WorldPlan = worldPlan;
                SolveOrder = solveOrder;
                Projections = projections;
                Bindings = bindings;
            }

            internal WorldPlanInput WorldPlan { get; }
            internal WorldSolveOrderResult SolveOrder { get; }
            internal WorldSocketProjection[] Projections { get; }
            internal WorldBoundaryBinding[] Bindings { get; }

            internal static ReferenceIntersectorFixture Create()
            {
                var nodes = Enumerable.Range(0, WorldPlanInput.SectorCount)
                    .Select(id => new WorldSectorNode(
                        new WorldSectorId(id),
                        new WorldSectorCoordinate(id % WorldPlanInput.SectorColumns,
                            id / WorldPlanInput.SectorColumns),
                        Biome(id),
                        1,
                        id <= 1 ? AccessClass.MandatoryNoTool : AccessClass.OptionalNoTool,
                        id <= 1 ? PacingRole.Traversal : PacingRole.Quiet,
                        false,
                        false,
                        false,
                        id == 0))
                    .ToArray();
                var dependency = new WorldDependencyEdge(
                    new WorldSectorId(0),
                    new WorldSectorId(1),
                    WorldDependencyKind.MandatoryRoute,
                    "REFERENCE_INTERSECTOR_MANDATORY_ROUTE",
                    "MAP05_MANDATORY_ROUTE");
                var worldPlan = new WorldPlanInput(
                    nodes,
                    new[] { dependency },
                    new WorldRetryEnvelope(6, 1, WorldSolveAbortReason.SectorLocalAttemptsExhausted),
                    Map14PhaseExitDigest,
                    WorldSolveOrderPlanner.ReferencePublicationLabel);
                var solveOrder = WorldSolveOrderPlanner.Plan(worldPlan);
                if (!solveOrder.Success)
                {
                    throw new InvalidOperationException(string.Join(";", solveOrder.Failures));
                }

                var boundaryEdgeIds = Enumerable.Range(4, 6)
                    .Select(x => new WorldIntersectorEdgeId(
                        new WorldSectorId(x),
                        new WorldSectorId(x + 1),
                        WorldEdgeOrientation.Horizontal))
                    .ToArray();
                var projections = BuildProjections(boundaryEdgeIds);
                var bindings = BuildBindings(boundaryEdgeIds);
                return new ReferenceIntersectorFixture(worldPlan, solveOrder, projections, bindings);
            }

            internal WorldIntersectorBuildRequest Request(
                IEnumerable<WorldSocketProjection> projections,
                IEnumerable<WorldBoundaryBinding> bindings,
                string map14HandoffDigest = Map14PhaseExitDigest,
                int fallbackCarveCount = 0,
                int tilemapMutationCount = 0)
            {
                return new WorldIntersectorBuildRequest(
                    WorldPlan,
                    SolveOrder,
                    projections,
                    bindings,
                    map14HandoffDigest,
                    WorldIntersectorDigest.HashCanonicalText(MoonpalaceBiomePairCatalog.Canonical.Signature),
                    WorldBoundarySocketIntegrator.ReferencePublicationLabel,
                    fallbackCarveCount: fallbackCarveCount,
                    tilemapMutationCount: tilemapMutationCount);
            }

            internal WorldIntersectorBuildResult Integrate() =>
                WorldBoundarySocketIntegrator.Integrate(Request(Projections, Bindings));

            private static WorldSocketProjection[] BuildProjections(
                IReadOnlyCollection<WorldIntersectorEdgeId> boundaryEdgeIds)
            {
                var result = new List<WorldSocketProjection>(WorldIntersectorEdgePlan.EndpointCount);
                for (var y = 0; y < WorldPlanInput.SectorRows; y++)
                {
                    for (var x = 0; x < WorldPlanInput.SectorColumns - 1; x++)
                    {
                        var first = new WorldSectorId((y * WorldPlanInput.SectorColumns) + x);
                        var second = new WorldSectorId(first.Value + 1);
                        var edgeId = new WorldIntersectorEdgeId(first, second, WorldEdgeOrientation.Horizontal);
                        var mandatory = first.Value == 0 && second.Value == 1;
                        var external = first.Value == 1 && second.Value == 2;
                        var boundary = boundaryEdgeIds.Contains(edgeId);
                        result.Add(Projection(first, WorldSectorSide.East,
                            new WorldSocketAnchor(47, 16, 3), external, mandatory, boundary));
                        result.Add(Projection(second, WorldSectorSide.West,
                            new WorldSocketAnchor(0, 16, 3), external, mandatory, boundary));
                    }
                }

                for (var y = 0; y < WorldPlanInput.SectorRows - 1; y++)
                {
                    for (var x = 0; x < WorldPlanInput.SectorColumns; x++)
                    {
                        var first = new WorldSectorId((y * WorldPlanInput.SectorColumns) + x);
                        var second = new WorldSectorId(first.Value + WorldPlanInput.SectorColumns);
                        result.Add(Projection(first, WorldSectorSide.North,
                            new WorldSocketAnchor(24, 31, 3), false, false, false));
                        result.Add(Projection(second, WorldSectorSide.South,
                            new WorldSocketAnchor(24, 0, 3), false, false, false));
                    }
                }
                return result.ToArray();
            }

            private static WorldBoundaryBinding[] BuildBindings(IReadOnlyList<WorldIntersectorEdgeId> edgeIds)
            {
                return new[]
                {
                    Binding(edgeIds[0], MoonpalaceCraterRootBoundaryAuthoringContract.PairRuleId,
                        MoonpalaceCraterRootBoundaryAuthoringContract.ProfileIds[0],
                        MoonpalaceCraterRootBoundaryAuthoringContract.CandidateIds[0]),
                    Binding(edgeIds[1], MoonpalaceCraterMillBoundaryAuthoringContract.PairRuleId,
                        MoonpalaceCraterMillBoundaryAuthoringContract.ProfileIds[0],
                        MoonpalaceCraterMillBoundaryAuthoringContract.CandidateIds[0]),
                    Binding(edgeIds[2], MoonpalaceCraterDoughBoundaryAuthoringContract.PairRuleId,
                        MoonpalaceCraterDoughBoundaryAuthoringContract.ProfileIds[0],
                        MoonpalaceCraterDoughBoundaryAuthoringContract.CandidateIds[0]),
                    Binding(edgeIds[3], MoonpalaceRootMillBoundaryAuthoringContract.PairRuleId,
                        MoonpalaceRootMillBoundaryAuthoringContract.ProfileIds[0],
                        MoonpalaceRootMillBoundaryAuthoringContract.CandidateIds[0]),
                    Binding(edgeIds[4], MoonpalaceRootDoughBoundaryAuthoringContract.PairRuleId,
                        MoonpalaceRootDoughBoundaryAuthoringContract.ProfileIds[0],
                        MoonpalaceRootDoughBoundaryAuthoringContract.CandidateIds[0]),
                    Binding(edgeIds[5], MoonpalaceMillDoughBoundaryAuthoringContract.PairRuleId,
                        MoonpalaceMillDoughBoundaryAuthoringContract.ProfileIds[0],
                        MoonpalaceMillDoughBoundaryAuthoringContract.CandidateIds[0]),
                };
            }

            private static WorldSocketProjection Projection(
                WorldSectorId sector,
                WorldSectorSide side,
                WorldSocketAnchor anchor,
                bool external,
                bool mandatory,
                bool boundary) =>
                new WorldSocketProjection(
                    sector, side, anchor, external, mandatory, boundary,
                    external ? "MAP14_EXTERNAL_SOCKET" : boundary ? "MAP08_BOUNDARY" : "MAP15_01_WORLD_PLAN");

            private static WorldBoundaryBinding Binding(
                WorldIntersectorEdgeId edgeId,
                string pairId,
                string profileId,
                string candidateId) =>
                new WorldBoundaryBinding(
                    edgeId,
                    pairId,
                    profileId,
                    candidateId,
                    new[]
                    {
                        MoonpalaceBoundaryWarningMarkerCategory.Tile.Token,
                        MoonpalaceBoundaryWarningMarkerCategory.Background.Token,
                    },
                    "MAP08_BOUNDARY_AUTHORITY");

            private static string Biome(int id)
            {
                switch ((id / WorldPlanInput.SectorColumns) % 4)
                {
                    case 0: return MoonpalaceCraterRootBoundaryAuthoringContract.BiomeAId;
                    case 1: return MoonpalaceCraterRootBoundaryAuthoringContract.BiomeBId;
                    case 2: return MoonpalaceCraterMillBoundaryAuthoringContract.BiomeBId;
                    default: return MoonpalaceCraterDoughBoundaryAuthoringContract.BiomeBId;
                }
            }
        }
    }
}
