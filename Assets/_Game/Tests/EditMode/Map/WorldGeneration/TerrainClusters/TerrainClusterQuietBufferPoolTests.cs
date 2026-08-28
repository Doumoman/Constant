using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.Pipeline;
using UnityEngine;

namespace StarNight.Map.WorldGeneration.TerrainClusters.Tests
{
    [TestFixture]
    [Category("MAP11_06")]
    public sealed class TerrainClusterQuietBufferPoolTests
    {
        private const string Map1105ResultSha =
            "f2c93add171cb9b6ee1adeed16af43c1c32a71a8ab6c9b85a14e8dd2f3a93bcf";
        private const string Map1105TaskSha =
            "45bde171c3357c8c9c5f2776566f2e55f4a17cba2d3978323e0a05636a2623b8";
        private const string Map1105RepairSha =
            "aa7beb451be6169d4069c3d323c91207d3e53667bc53d1e276a0caa6697463fc";
        private const string CatalogSha =
            "f9d9e9cc60c4e4d7561c5aa6502228c18fc9566e3e0febab206ea3264b408267";
        private const string CellsSha =
            "e702ae5d02d7ec9d2cda129c1361699e37d942c280c8f9bd1f3200f155084381";
        private const string AuthoringManifestSha =
            "4415ae4af5196d6793f5d0152c0688e5bf35dc4ad23442791e45d3cfd81d0851";

        private Fixture fixture;

        [OneTimeSetUp]
        public void SetUpFixture()
        {
            fixture = BuildFixture(2, "TC_QUIET_BUFFER", false);
        }

        [Test]
        public void ExactUseKindsAndQuietBufferIdGrammarAreEnforced()
        {
            Assert.That(Enum.GetValues(typeof(TerrainClusterQuietBufferUse)), Is.EqualTo(new[]
            {
                TerrainClusterQuietBufferUse.BeforeLandmark,
                TerrainClusterQuietBufferUse.AfterLandmark,
                TerrainClusterQuietBufferUse.UnplacedSpace,
            }));
            AssertSuccess(Compile(Profile(fixture, "QBUF_EXACT_01")));
            AssertFailure(Compile(Profile(fixture, "qbuf-invalid")),
                TerrainClusterQuietBufferErrorCode.InvalidQuietBufferId);
        }

        [Test]
        public void RepairedMap1105AndPhysicalAuthorityStaticGatesAreExact()
        {
            Assert.That(Sha256(File.ReadAllBytes(FullPath(
                "MapDesign/MCP/REPORTS/MAP11_05_IMPLEMENT_CLUSTER_PATTERN_ZONES_AND_RENDERER_RESULT.md"))),
                Is.EqualTo(Map1105ResultSha));
            Assert.That(Sha256(File.ReadAllBytes(FullPath(
                "MapDesign/MCP/TASKS/MAP11_05_IMPLEMENT_CLUSTER_PATTERN_ZONES_AND_RENDERER.md"))),
                Is.EqualTo(Map1105TaskSha));
            Assert.That(Sha256(File.ReadAllBytes(FullPath(
                "MapDesign/MCP/TASKS/MAP11_05R_REPAIR_PATTERN_ZONE_RENDER_TARGET_CONTRACT.md"))),
                Is.EqualTo(Map1105RepairSha));
            Assert.That(Sha256(File.ReadAllBytes(FullPath(
                "MapDesign/MCP_ARCHIVE/MAP11_05R_REPAIR_PATTERN_ZONE_RENDER_TARGET_CONTRACT.md"))),
                Is.EqualTo(Map1105RepairSha));

            var catalog = FullPath("Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroPattern/micro_pattern_catalog_v2.csv");
            var cells = FullPath("Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroPattern/micro_pattern_cells_v2.csv");
            Assert.That(Sha256(File.ReadAllBytes(catalog)), Is.EqualTo(CatalogSha));
            Assert.That(Sha256(File.ReadAllBytes(cells)), Is.EqualTo(CellsSha));
            Assert.That(DataRowCount(catalog), Is.EqualTo(24));
            Assert.That(DataRowCount(cells), Is.EqualTo(453));
            var authoringRoot = FullPath("Assets/_Game/Map/Data/WorldGeneration/Authoring");
            var csv = Directory.GetFiles(authoringRoot, "*.csv", SearchOption.AllDirectories);
            Assert.That(csv, Has.Length.EqualTo(52));
            Assert.That(ComputeManifest(authoringRoot, csv), Is.EqualTo(AuthoringManifestSha));
            Assert.That(Directory.GetFiles(
                FullPath("Assets/_Game/Map/Data/WorldGeneration/Generated"), "*.csv", SearchOption.AllDirectories),
                Is.Empty);

            var metas = Directory.GetFiles(FullPath("Assets"), "*.meta", SearchOption.AllDirectories);
            var guids = metas.Select(ReadGuid).ToArray();
            Assert.That(metas, Has.Length.EqualTo(3935));
            Assert.That(guids.All(value => value.Length == 32 && value.All(IsLowerHex)), Is.True);
            Assert.That(guids.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(guids.Length));
        }

        [Test]
        public void Map1101ThroughMap1105ArtifactIdentityAndDigestChainIsPreserved()
        {
            var result = Compile(Profile(fixture, "QBUF_CHAIN"));
            AssertSuccess(result);
            var candidate = result.Candidates.Single();
            Assert.That(candidate.SourceContractDigest, Is.EqualTo(fixture.RoleSocket.SourceContractDigest));
            Assert.That(candidate.LocalCanvasDigest, Is.EqualTo(fixture.Canvas.CanonicalDigest));
            Assert.That(candidate.RoleSocketContractDigest, Is.EqualTo(fixture.RoleSocket.CanonicalDigest));
            Assert.That(candidate.TraversalCompilationDigest, Is.EqualTo(fixture.Traversal.CanonicalDigest));
            Assert.That(candidate.RouteWitnessDigest, Is.EqualTo(fixture.Witness.CanonicalDigest));
            Assert.That(candidate.PatternRenderDigest, Is.EqualTo(fixture.Render.CanonicalDigest));
        }

        [Test]
        public void ExactlyTwoActiveChunksAreEligibleAndOneOrThreeAreRejected()
        {
            var success = Compile(Profile(fixture, "QBUF_TWO"));
            AssertSuccess(success);
            Assert.That(success.Candidates.Single().ActiveChunkCount, Is.EqualTo(2));

            var oneChunk = CreateContract(1, "TC_QUIET_ONE", false);
            var oneValidation = TerrainClusterContractValidator.Validate(oneChunk);
            Assert.That(oneValidation.IsValid, Is.False);
            Assert.That(oneValidation.Errors.Select(value => value.Code),
                Does.Contain(TerrainClusterValidationErrorCode.InvalidFootprintCount));

            var three = BuildFixture(3, "TC_QUIET_THREE", false);
            AssertFailure(Compile(Profile(three, "QBUF_THREE")),
                TerrainClusterQuietBufferErrorCode.InvalidFootprintSize);
        }

        [Test]
        public void EntryExitOwnDifferentChunksAndBaselineCoversBothWithoutSyntheticEvidence()
        {
            var candidate = SuccessCandidate("QBUF_COVERAGE");
            Assert.That(candidate.EntryPortId, Is.EqualTo("PORT_ENTRY"));
            Assert.That(candidate.ExitPortId, Is.EqualTo("PORT_EXIT"));
            Assert.That(candidate.BaselineCoveredChunks, Is.EqualTo(candidate.ActiveChunks));
            Assert.That(candidate.BaselineNodeIds, Is.EqualTo(
                fixture.Witness.BaselineRoute.OrderedNodeIds.OrderBy(value => value, StringComparer.Ordinal)));
            Assert.That(candidate.BaselineEdgeIds, Is.EqualTo(
                fixture.Witness.BaselineRoute.OrderedEdges.Select(value => value.EdgeId)
                    .OrderBy(value => value, StringComparer.Ordinal)));
            Assert.That(candidate.ChunkEvidence.All(value => value.BaselineCoordinateCount > 0), Is.True);
        }

        [Test]
        public void PacingRequiresQuietAndRejectsEveryStrongRole()
        {
            AssertSuccess(Compile(Profile(fixture, "QBUF_PACING_OK", pacing: new[]
                { PacingRole.Flow, PacingRole.Quiet, PacingRole.Recovery, PacingRole.Safe, PacingRole.Traversal })));
            AssertFailure(Compile(Profile(fixture, "QBUF_PACING_NO_QUIET", pacing: new[] { PacingRole.Traversal })),
                TerrainClusterQuietBufferErrorCode.InvalidPacingCompatibility);
            foreach (var strong in new[] { PacingRole.Discovery, PacingRole.Risk, PacingRole.Machinery,
                         PacingRole.Activity, PacingRole.Narrative, PacingRole.Reward, PacingRole.Landmark,
                         PacingRole.Resource, PacingRole.Boss, PacingRole.Integrated })
                AssertFailure(Compile(Profile(fixture, "QBUF_PACING_" + strong.ToString().ToUpperInvariant(),
                    pacing: new[] { PacingRole.Quiet, strong })),
                    TerrainClusterQuietBufferErrorCode.InvalidPacingCompatibility);
        }

        [Test]
        public void AccessRequiresMandatoryNoToolAndRejectsToolOrEnvironment()
        {
            AssertSuccess(Compile(Profile(fixture, "QBUF_ACCESS_OK", access: new[]
                { AccessClass.OptionalNoTool, AccessClass.MandatoryNoTool })));
            AssertFailure(Compile(Profile(fixture, "QBUF_ACCESS_NO_MANDATORY", access: new[] { AccessClass.OptionalNoTool })),
                TerrainClusterQuietBufferErrorCode.InvalidAccessCompatibility);
            AssertFailure(Compile(Profile(fixture, "QBUF_ACCESS_TOOL", access: new[]
                { AccessClass.MandatoryNoTool, AccessClass.OptionalTool })),
                TerrainClusterQuietBufferErrorCode.InvalidAccessCompatibility);
            AssertFailure(Compile(Profile(fixture, "QBUF_ACCESS_ENV", access: new[]
                { AccessClass.MandatoryNoTool, AccessClass.OptionalEnvironment })),
                TerrainClusterQuietBufferErrorCode.InvalidAccessCompatibility);
        }

        [Test]
        public void EveryActiveChunkPublishesSolidAirAndFullCanvasCoverage()
        {
            var candidate = SuccessCandidate("QBUF_TERRAIN");
            Assert.That(candidate.ChunkEvidence, Has.Count.EqualTo(2));
            Assert.That(candidate.ChunkEvidence.All(value => value.SolidCount >= 1 && value.AirCount >= 1), Is.True);
            Assert.That(candidate.PatternRenderReport.FinalWorkingCanvas.CoordinateCount,
                Is.EqualTo(candidate.LocalCanvas.TileCells.Count(value => value.State == ClusterChunkMaskState.Active)));
            Assert.That(candidate.ChunkEvidence.Sum(value => value.SolidCount + value.AirCount),
                Is.EqualTo(candidate.PatternRenderReport.FinalWorkingCanvas.CoordinateCount));
        }

        [Test]
        public void RewardMarkerAndHazardEvidenceIsExactlyZero()
        {
            var candidate = SuccessCandidate("QBUF_STRONG_ZERO");
            Assert.That(candidate.RewardRoleCount, Is.Zero);
            Assert.That(candidate.MarkerCount, Is.Zero);
            Assert.That(candidate.HazardCount, Is.Zero);
            Assert.That(candidate.RoleSocketContract.Roles.Count(value => value.Role == ClusterRoleKind.Reward), Is.Zero);
        }

        [Test]
        public void ProtectedWriteAndValueChangeEvidenceIsExactlyZero()
        {
            var candidate = SuccessCandidate("QBUF_PROTECTED_ZERO");
            Assert.That(candidate.ProtectedWriteCount, Is.Zero);
            Assert.That(candidate.ProtectedValueChangeCount, Is.Zero);
            Assert.That(candidate.PatternRenderReport.RendererDeltaCoordinateCount, Is.Zero);
        }

        [Test]
        public void PoolPublishesEveryTypedIndexInStableIdOrder()
        {
            var second = BuildFixture(2, "TC_QUIET_BUFFER_B", false);
            var result = TerrainClusterQuietBufferPoolCompiler.Compile(
                new TerrainClusterQuietBufferPoolCompileRequest(new[]
                {
                    Profile(second, "QBUF_B"), Profile(fixture, "QBUF_A"),
                }));
            AssertSuccess(result);
            var pool = result.Pool;
            Assert.That(pool.Candidates.Select(value => value.QuietBufferId), Is.EqualTo(new[] { "QBUF_A", "QBUF_B" }));
            AssertBucket(pool.ByBiome[MoonpalaceBiomeId.MoonCrater]);
            AssertBucket(pool.ByUse[TerrainClusterQuietBufferUse.BeforeLandmark]);
            AssertBucket(pool.ByEntrySide[ClusterPortSide.L]);
            AssertBucket(pool.ByExitSide[ClusterPortSide.R]);
            AssertBucket(pool.ByRouteType[2]);
            AssertBucket(pool.ByPacingRole[PacingRole.Quiet]);
            AssertBucket(pool.ByAccessClass[AccessClass.MandatoryNoTool]);
        }

        [Test]
        public void MultiConditionQueryReturnsAllMatchesInStableOrderWithoutSelection()
        {
            var second = BuildFixture(2, "TC_QUIET_BUFFER_QUERY_B", false);
            var pool = TerrainClusterQuietBufferPoolCompiler.Compile(
                new TerrainClusterQuietBufferPoolCompileRequest(new[]
                { Profile(second, "QBUF_QUERY_B"), Profile(fixture, "QBUF_QUERY_A") })).Pool;
            var query = Query(pool.CanonicalDigest, MoonpalaceBiomeId.MoonCrater,
                TerrainClusterQuietBufferUse.BeforeLandmark, 2, PacingRole.Quiet,
                AccessClass.MandatoryNoTool, 2);
            var result = pool.Query(query);
            Assert.That(result.IsSuccess, Is.True, ErrorText(result));
            Assert.That(result.QueryResult.MatchedCandidateIds,
                Is.EqualTo(new[] { "QBUF_QUERY_A", "QBUF_QUERY_B" }));
            Assert.That(result.QueryResult.MatchCount, Is.EqualTo(2));
            Assert.That(result.QueryResult.SelectionCount, Is.Zero);
            Assert.That(result.QueryResult.RngDrawCount, Is.Zero);
        }

        [Test]
        public void ZeroMatchIsAValidImmutableQueryResultWithNoDraw()
        {
            var pool = Compile(Profile(fixture, "QBUF_EMPTY_QUERY")).Pool;
            var result = pool.Query(Query(pool.CanonicalDigest, MoonpalaceBiomeId.MoonDough,
                TerrainClusterQuietBufferUse.AfterLandmark, 4, PacingRole.Safe,
                AccessClass.OptionalNoTool, null));
            Assert.That(result.IsSuccess, Is.True, ErrorText(result));
            Assert.That(result.QueryResult.Matches, Is.Empty);
            Assert.That(result.QueryResult.MatchedCandidateIds, Is.Empty);
            Assert.That(result.QueryResult.MatchedCandidateDigests, Is.Empty);
            Assert.That(result.QueryResult.RngDrawCount, Is.Zero);
            Assert.That(result.QueryResult.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<string>)result.QueryResult.MatchedCandidateIds).Add("QBUF_MUTATE"));
        }

        [Test]
        public void DuplicateAndInvalidCandidateCauseAtomicPoolFailure()
        {
            var profile = Profile(fixture, "QBUF_DUPLICATE");
            var referenceDuplicate = TerrainClusterQuietBufferPoolCompiler.Compile(
                new TerrainClusterQuietBufferPoolCompileRequest(new[] { profile, profile }));
            AssertFailure(referenceDuplicate, TerrainClusterQuietBufferErrorCode.DuplicateCandidateIdentity);
            AssertFailure(referenceDuplicate, TerrainClusterQuietBufferErrorCode.DuplicateQuietBufferId);

            var identityDuplicate = TerrainClusterQuietBufferPoolCompiler.Compile(
                new TerrainClusterQuietBufferPoolCompileRequest(new[]
                { Profile(fixture, "QBUF_IDENTITY_A"), Profile(fixture, "QBUF_IDENTITY_B") }));
            AssertFailure(identityDuplicate, TerrainClusterQuietBufferErrorCode.DuplicateCandidateIdentity);
            AssertAtomicFailure(identityDuplicate);
        }

        [Test]
        public void PublicationIsImmutableCanonicalAndDigestsAreDeterministic()
        {
            var forward = Compile(Profile(fixture, "QBUF_IMMUTABLE", uses: new[]
            {
                TerrainClusterQuietBufferUse.UnplacedSpace,
                TerrainClusterQuietBufferUse.BeforeLandmark,
            }));
            var reverse = Compile(Profile(fixture, "QBUF_IMMUTABLE", uses: new[]
            {
                TerrainClusterQuietBufferUse.BeforeLandmark,
                TerrainClusterQuietBufferUse.UnplacedSpace,
            }));
            AssertSuccess(forward);
            AssertSuccess(reverse);
            Assert.That(reverse.CanonicalDigest, Is.EqualTo(forward.CanonicalDigest));
            Assert.That(reverse.Candidates.Single().CanonicalDigest,
                Is.EqualTo(forward.Candidates.Single().CanonicalDigest));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<TerrainClusterQuietBufferUse>)forward.Candidates.Single().SupportedUses)
                    .Add(TerrainClusterQuietBufferUse.AfterLandmark));
            Assert.Throws<NotSupportedException>(() =>
                ((IDictionary<int, IReadOnlyList<TerrainClusterQuietBufferCandidate>>)forward.Pool.ByRouteType)
                    .Add(0, Array.Empty<TerrainClusterQuietBufferCandidate>()));
        }

        [Test]
        public void ReversedInputAndCultureAreStableWhileSemanticBiomeChangesDigest()
        {
            var reversedFixture = BuildFixture(2, "TC_QUIET_BUFFER", true);
            var originalCulture = CultureInfo.CurrentCulture;
            TerrainClusterQuietBufferResult reversed;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                reversed = Compile(Profile(reversedFixture, "QBUF_CULTURE"));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
            var forward = Compile(Profile(fixture, "QBUF_CULTURE"));
            AssertSuccess(reversed);
            Assert.That(reversed.CanonicalDigest, Is.EqualTo(forward.CanonicalDigest));

            var semantic = Compile(Profile(fixture, "QBUF_CULTURE", biome: MoonpalaceBiomeId.CassiaRoot));
            AssertSuccess(semantic);
            Assert.That(semantic.Candidates.Single().CanonicalDigest,
                Is.Not.EqualTo(forward.Candidates.Single().CanonicalDigest));
        }

        [Test]
        public void AccumulatedErrorsPublishNoPartialCandidatePoolIndexQueryOrDigest()
        {
            var invalid = new TerrainClusterQuietBufferProfile(
                "bad id", default, new[] { (TerrainClusterQuietBufferUse)99 },
                new[] { PacingRole.Risk }, new[] { AccessClass.OptionalTool },
                fixture.Canvas, "bad-canvas", fixture.RoleSocket, "bad-role",
                fixture.Traversal, "bad-traversal", fixture.Witness, "bad-witness",
                fixture.Render, "bad-render");
            var result = Compile(invalid);
            Assert.That(result.Errors.Count, Is.GreaterThanOrEqualTo(10), ErrorText(result));
            AssertAtomicFailure(result);

            var pool = Compile(Profile(fixture, "QBUF_QUERY_FAILURE")).Pool;
            var queryFailure = pool.Query(Query("bad-pool", default,
                (TerrainClusterQuietBufferUse)99, 5, (PacingRole)99,
                (AccessClass)99, 1));
            AssertFailure(queryFailure, TerrainClusterQuietBufferErrorCode.PoolDigestMismatch);
            AssertFailure(queryFailure, TerrainClusterQuietBufferErrorCode.InvalidQuery);
            AssertAtomicFailure(queryFailure);
        }

        [Test]
        public void RuntimeSourcesHaveNoRngPlacementCleanupStarterSectorOrTilemapSideEffects()
        {
            var sources = new[]
            {
                FullPath("Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterQuietBuffer.cs"),
                FullPath("Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterQuietBufferPool.cs"),
            };
            var text = string.Join("\n", sources.Select(File.ReadAllText));
            foreach (var symbol in new[]
                     {
                         "UnityEditor", "StageMapGenerator", "GridWorld", "RoomTemplate",
                         "RoomGridTransform", "TileMutationService", "SectorRecipeResolver",
                         "System.Random", "UnityEngine.Random", "DeterministicRngStreamFactory",
                         "Time.deltaTime", "Tilemap", "MicroPatternPlanner", "MicroPatternSelection",
                         "MicroPatternLocalCleanup", "Starter16", "SectorCanvas",
                     })
                Assert.That(text, Does.Not.Contain(symbol), symbol);
        }

        private TerrainClusterQuietBufferCandidate SuccessCandidate(string id)
        {
            return Compile(Profile(fixture, id)).Candidates.Single();
        }

        private static TerrainClusterQuietBufferResult Compile(TerrainClusterQuietBufferProfile profile)
        {
            return TerrainClusterQuietBufferPoolCompiler.Compile(
                new TerrainClusterQuietBufferPoolCompileRequest(new[] { profile }));
        }

        private static TerrainClusterQuietBufferProfile Profile(
            Fixture source,
            string id,
            MoonpalaceBiomeId? biome = null,
            IEnumerable<TerrainClusterQuietBufferUse> uses = null,
            IEnumerable<PacingRole> pacing = null,
            IEnumerable<AccessClass> access = null)
        {
            return new TerrainClusterQuietBufferProfile(
                id, biome ?? MoonpalaceBiomeId.MoonCrater,
                uses ?? new[] { TerrainClusterQuietBufferUse.BeforeLandmark, TerrainClusterQuietBufferUse.UnplacedSpace },
                pacing ?? new[] { PacingRole.Quiet, PacingRole.Traversal, PacingRole.Recovery },
                access ?? new[] { AccessClass.MandatoryNoTool, AccessClass.OptionalNoTool },
                source.Canvas, source.Canvas.CanonicalDigest,
                source.RoleSocket, source.RoleSocket.CanonicalDigest,
                source.Traversal, source.Traversal.CanonicalDigest,
                source.Witness, source.Witness.CanonicalDigest,
                source.Render, source.Render.CanonicalDigest);
        }

        private static TerrainClusterQuietBufferQuery Query(
            string poolDigest,
            MoonpalaceBiomeId biome,
            TerrainClusterQuietBufferUse use,
            int routeType,
            PacingRole pacing,
            AccessClass access,
            int? maximum)
        {
            return new TerrainClusterQuietBufferQuery(
                biome, use, ClusterPortSide.L, ClusterPortSide.R, routeType,
                pacing, access, maximum, poolDigest);
        }

        private static Fixture BuildFixture(int activeChunkCount, string clusterId, bool reverseInput)
        {
            var contract = CreateContract(activeChunkCount, clusterId, reverseInput);
            var validation = TerrainClusterContractValidator.Validate(contract);
            Assert.That(validation.IsValid, Is.True,
                string.Join("\n", validation.Errors.Select(value => value.ToString())));
            var canvasResult = TerrainClusterFootprintCompiler.Compile(
                new TerrainClusterFootprintCompileRequest(contract, ClusterFootprintTransform.R0));
            Assert.That(canvasResult.IsSuccess, Is.True,
                string.Join("\n", canvasResult.Errors.Select(value => value.ToString())));
            var canvas = canvasResult.LocalCanvas;
            var roleResult = TerrainClusterRoleSocketCompiler.Compile(
                new TerrainClusterRoleSocketCompileRequest(
                    contract, validation.CanonicalDigest, canvas, canvas.CanonicalDigest, SocketEvidence()));
            Assert.That(roleResult.IsSuccess, Is.True,
                string.Join("\n", roleResult.Errors.Select(value => value.ToString())));
            var traversalResult = TerrainClusterTraversalCompiler.Compile(
                new TerrainClusterTraversalCompileRequest(
                    contract, validation.CanonicalDigest, canvas, canvas.CanonicalDigest,
                    roleResult.Contract, roleResult.CanonicalDigest));
            Assert.That(traversalResult.IsSuccess, Is.True,
                string.Join("\n", traversalResult.Errors.Select(value => value.ToString())));
            var witnessResult = TerrainClusterRouteWitnessCompiler.Compile(
                new TerrainClusterRouteWitnessCompileRequest(
                    canvas, canvas.CanonicalDigest, roleResult.Contract, roleResult.CanonicalDigest,
                    traversalResult.Compilation, traversalResult.CanonicalDigest,
                    CreateWitnessIntent(traversalResult.Compilation, reverseInput)));
            Assert.That(witnessResult.IsSuccess, Is.True,
                string.Join("\n", witnessResult.Errors.Select(value => value.ToString())));

            var catalog = BuildNoChangeCatalog();
            Assert.That(catalog.TryGetDefinition(new MicroPatternId("MP_QUIET_NO_CHANGE"), out var definition), Is.True);
            var placement = new TerrainClusterPatternPlacementIntent(
                "TCP_QUIET_NO_CHANGE", definition.Id, MicroPatternTransform.R0,
                new LocalTileCoord(0, 4), definition.ComputeStableDigest());
            var renderResult = TerrainClusterPatternRenderer.Render(
                new TerrainClusterPatternRenderRequest(
                    canvas, canvas.CanonicalDigest,
                    traversalResult.Compilation, traversalResult.CanonicalDigest,
                    witnessResult.Report, witnessResult.CanonicalDigest,
                    catalog, catalog.StableDigest,
                    Array.Empty<TerrainClusterPatternZoneCell>(), new[] { placement }));
            Assert.That(renderResult.Success, Is.True,
                string.Join("\n", renderResult.Errors.Select(value => value.ToString())));
            Assert.That(renderResult.Report.RendererDeltaCoordinateCount, Is.Zero);
            return new Fixture(canvas, roleResult.Contract, traversalResult.Compilation,
                witnessResult.Report, renderResult.Report);
        }

        private static TerrainClusterContract CreateContract(
            int activeChunkCount,
            string clusterId,
            bool reverseInput)
        {
            var exitX = activeChunkCount * 12 - 1;
            var recoveryX = Math.Max(6, exitX - 6);
            var coreX = Math.Max(5, exitX / 2 - 1);
            var buildUpX = Math.Min(4, coreX - 2);
            var stepAX = buildUpX + 3;
            var stepBX = buildUpX + 2;
            var roles = new[]
            {
                new ClusterRoleAnchor("ANCHOR_ENTRY", ClusterRoleKind.Entry, new LocalTileCoord(0, 1), "NODE_ENTRY"),
                new ClusterRoleAnchor("ANCHOR_BUILD_UP", ClusterRoleKind.BuildUp, new LocalTileCoord(buildUpX, 1), "NODE_BUILD_UP"),
                new ClusterRoleAnchor("ANCHOR_CORE", ClusterRoleKind.Core, new LocalTileCoord(coreX, 1), "NODE_CORE"),
                new ClusterRoleAnchor("ANCHOR_RECOVERY", ClusterRoleKind.Recovery, new LocalTileCoord(recoveryX, 1), "NODE_RECOVERY"),
                new ClusterRoleAnchor("ANCHOR_EXIT", ClusterRoleKind.Exit, new LocalTileCoord(exitX, 1), "NODE_EXIT"),
            };
            var commonNodes = roles.Select(value => new TraversalNode(
                value.TraversalNodeId, value.Tile, true, value.AnchorId)).Concat(new[]
            {
                new TraversalNode("NODE_STEP_A", new LocalTileCoord(stepAX, 1), true, string.Empty),
                new TraversalNode("NODE_STEP_B", new LocalTileCoord(stepBX, 1), false, string.Empty),
            }).ToArray();
            var alternateNodes = commonNodes.Concat(new[]
            {
                new TraversalNode("NODE_HIGH", new LocalTileCoord(stepAX, 3), false, string.Empty),
                new TraversalNode("NODE_HIGH_END", new LocalTileCoord(stepAX + 2, 3), false, string.Empty),
            }).ToArray();
            var common = commonNodes.ToDictionary(value => value.NodeId, StringComparer.Ordinal);
            var alternate = alternateNodes.ToDictionary(value => value.NodeId, StringComparer.Ordinal);
            var baselineEdges = new[]
            {
                CreateEdge("EDGE_01_ENTRY", common["NODE_ENTRY"], common["NODE_BUILD_UP"], true),
                CreateEdge("EDGE_BASE_A1", common["NODE_BUILD_UP"], common["NODE_STEP_A"], true),
                CreateEdge("EDGE_BASE_A2", common["NODE_STEP_A"], common["NODE_CORE"], true),
                CreateEdge("EDGE_BASE_B1", common["NODE_BUILD_UP"], common["NODE_STEP_B"], false),
                CreateEdge("EDGE_BASE_B2", common["NODE_STEP_B"], common["NODE_CORE"], false),
                CreateEdge("EDGE_04_CORE", common["NODE_CORE"], common["NODE_RECOVERY"], true),
                CreateEdge("EDGE_05_RECOVERY", common["NODE_RECOVERY"], common["NODE_EXIT"], true),
            };
            var alternateEdges = baselineEdges.Select(value => CopyEdge(value, alternate)).Concat(new[]
            {
                CreateEdge("EDGE_HIGH_01", alternate["NODE_BUILD_UP"], alternate["NODE_HIGH"], false),
                CreateEdge("EDGE_HIGH_02", alternate["NODE_HIGH"], alternate["NODE_HIGH_END"], false),
                CreateEdge("EDGE_HIGH_03", alternate["NODE_HIGH_END"], alternate["NODE_CORE"], false),
                CreateEdge("EDGE_RECOVER", alternate["NODE_HIGH"], alternate["NODE_RECOVERY"], false),
            }).ToArray();
            var variants = new[]
            {
                new SpineVariant(new SpineVariantId("SPINE_BASELINE"), true, TraversalGraphKind.Traversal,
                    reverseInput ? commonNodes.Reverse() : commonNodes,
                    reverseInput ? baselineEdges.Reverse() : baselineEdges),
                new SpineVariant(new SpineVariantId("SPINE_ALTERNATE"), false, TraversalGraphKind.Traversal,
                    reverseInput ? alternateNodes.Reverse() : alternateNodes,
                    reverseInput ? alternateEdges.Reverse() : alternateEdges),
            };
            var ports = new[]
            {
                new ClusterPort("PORT_ENTRY", ClusterPortKind.Entry, true, "ANCHOR_ENTRY",
                    new LocalTileCoord(0, 1), ClusterPortSide.L, new[] { 0, 1, 2, 3, 4 }),
                new ClusterPort("PORT_EXIT", ClusterPortKind.Exit, true, "ANCHOR_EXIT",
                    new LocalTileCoord(exitX, 1), ClusterPortSide.R, new[] { 1, 2, 3, 4 }),
            };
            var chunks = Enumerable.Range(0, activeChunkCount)
                .Select(value => new ClusterChunkCoord(value, 0)).ToArray();
            return new TerrainClusterContract(
                new TerrainClusterId(clusterId), new ClusterFootprint(chunks),
                reverseInput ? roles.Reverse() : roles,
                reverseInput ? ports.Reverse() : ports,
                new TerrainClusterTraversalContract(reverseInput ? variants.Reverse() : variants),
                reverseInput ? "reversed display" : "display");
        }

        private static TraversalEdge CreateEdge(
            string id,
            TraversalNode from,
            TraversalNode to,
            bool mandatory)
        {
            var envelope = new TraversalEnvelope(
                new[] { from.Tile, to.Tile },
                new[] { new LocalTileCoord(from.Tile.X, 0) },
                new[] { new LocalTileCoord(from.Tile.X, 5) },
                Array.Empty<LocalTileCoord>(), Array.Empty<LocalTileCoord>(),
                new[] { to.Tile }, new[] { to.Tile });
            return new TraversalEdge(
                id, from.NodeId, to.NodeId, TraversalMovementKind.Walk,
                from.Tile, to.Tile, 1, 2, to.Tile, to.Tile, mandatory, envelope);
        }

        private static TraversalEdge CopyEdge(
            TraversalEdge edge,
            IDictionary<string, TraversalNode> nodes)
        {
            return CreateEdge(edge.EdgeId, nodes[edge.FromNodeId], nodes[edge.ToNodeId], edge.IsMandatory);
        }

        private static TerrainClusterRouteWitnessIntent CreateWitnessIntent(
            TerrainClusterTraversalCompilation traversal,
            bool reverseInput)
        {
            var high = new TerrainClusterHighRouteDefinition(
                "HIGH_ROUTE_ONE", new SpineVariantId("SPINE_ALTERNATE"), "NODE_BUILD_UP",
                new[] { "EDGE_HIGH_01", "EDGE_HIGH_02", "EDGE_HIGH_03" },
                "NODE_CORE", "NODE_HIGH",
                reverseInput ? new[] { "BENEFIT_REWARD_ACCESS", "BENEFIT_HEIGHT_ADVANTAGE" } :
                    new[] { "BENEFIT_HEIGHT_ADVANTAGE", "BENEFIT_REWARD_ACCESS" },
                new[] { "NODE_HIGH" });
            var durations = traversal.Edges.Select(value => new TraversalEdgeDurationEvidence(
                value.VariantId, value.EdgeId, value.EdgeId == "EDGE_RECOVER" ? 2000 : 3000,
                "RULESET_ROUTE_V1"));
            return new TerrainClusterRouteWitnessIntent(
                new SpineVariantId("SPINE_BASELINE"), new[] { high },
                reverseInput ? durations.Reverse() : durations);
        }

        private static ClusterSectorSocketEvidence[] SocketEvidence()
        {
            return new[]
            {
                new ClusterSectorSocketEvidence(
                    "SR_EXIT", "SOCKET_EXIT", ClusterPortSide.R, 3, true, ClusterPortKind.Exit),
                new ClusterSectorSocketEvidence(
                    "SR_ENTRY", "SOCKET_ENTRY", ClusterPortSide.L, 2, true, ClusterPortKind.Entry),
            };
        }

        private static MicroPatternAuthoringCatalog BuildNoChangeCatalog()
        {
            var catalog = new[]
            {
                new MicroPatternCatalogRowV2(
                    "MP_QUIET_NO_CHANGE", "1", "MoonCrater", "R0", "FORCE_NO_CHANGE",
                    "catalog.csv", 2),
            };
            var cells = Enumerable.Range(0, 4).SelectMany(y => Enumerable.Range(0, 4)
                .Select((x, index) => new MicroPatternCellRowV2(
                    "MP_QUIET_NO_CHANGE", x.ToString(CultureInfo.InvariantCulture),
                    y.ToString(CultureInfo.InvariantCulture), "NO_CHANGE", "GEOMETRY", string.Empty,
                    "cells.csv", y * 4 + index + 2))).ToArray();
            var result = new MicroPatternCellSchemaBuilder().Build(catalog, cells);
            Assert.That(result.Success, Is.True,
                string.Join("\n", result.Errors.Select(value => value.ToString())));
            return result.Catalog;
        }

        private static void AssertSuccess(TerrainClusterQuietBufferResult result)
        {
            Assert.That(result.IsSuccess, Is.True, ErrorText(result));
            Assert.That(result.Pool, Is.Not.Null);
            Assert.That(result.Candidates, Is.Not.Empty);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
        }

        private static void AssertFailure(
            TerrainClusterQuietBufferResult result,
            TerrainClusterQuietBufferErrorCode code)
        {
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(code), ErrorText(result));
        }

        private static void AssertAtomicFailure(TerrainClusterQuietBufferResult result)
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Pool, Is.Null);
            Assert.That(result.QueryResult, Is.Null);
            Assert.That(result.Candidates, Is.Empty);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Errors, Is.Ordered);
            Assert.That(result.Errors.Distinct().Count(), Is.EqualTo(result.Errors.Count));
        }

        private static void AssertBucket(IReadOnlyList<TerrainClusterQuietBufferCandidate> bucket)
        {
            Assert.That(bucket.Select(value => value.QuietBufferId),
                Is.EqualTo(bucket.Select(value => value.QuietBufferId)
                    .OrderBy(value => value, StringComparer.Ordinal)));
        }

        private static string ErrorText(TerrainClusterQuietBufferResult result)
        {
            return string.Join("\n", result.Errors.Select(value => value.ToString()));
        }

        private static int DataRowCount(string path)
        {
            return File.ReadAllLines(path, Encoding.UTF8).Count(value => !string.IsNullOrEmpty(value)) - 1;
        }

        private static string ComputeManifest(string root, IEnumerable<string> paths)
        {
            var noBom = new UTF8Encoding(false);
            var withBom = new UTF8Encoding(true);
            var records = paths.Select(path => new { Path = path, Relative = Relative(root, path) })
                .OrderBy(value => value.Relative, StringComparer.Ordinal)
                .Select(value =>
                {
                    var normalized = File.ReadAllText(value.Path, Encoding.UTF8)
                        .Replace("\r\n", "\n").Replace("\r", "\n");
                    return value.Relative + "\t" + Sha256(
                        withBom.GetPreamble().Concat(noBom.GetBytes(normalized)).ToArray());
                });
            return Sha256(noBom.GetBytes(string.Join("\n", records)));
        }

        private static string Relative(string root, string path)
        {
            return path.Substring(root.Length + 1).Replace('\\', '/');
        }

        private static string Sha256(byte[] bytes)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(bytes)
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private static string ReadGuid(string path)
        {
            var line = File.ReadLines(path).FirstOrDefault(value => value.StartsWith("guid: ", StringComparison.Ordinal));
            return line == null ? string.Empty : line.Substring(6).Trim();
        }

        private static bool IsLowerHex(char value)
        {
            return (value >= '0' && value <= '9') || (value >= 'a' && value <= 'f');
        }

        private static string FullPath(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(
                Application.dataPath, "..", projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private sealed class Fixture
        {
            public Fixture(
                TerrainClusterLocalCanvas canvas,
                TerrainClusterRoleSocketContract roleSocket,
                TerrainClusterTraversalCompilation traversal,
                TerrainClusterRouteWitnessReport witness,
                TerrainClusterPatternRenderReport render)
            {
                Canvas = canvas;
                RoleSocket = roleSocket;
                Traversal = traversal;
                Witness = witness;
                Render = render;
            }

            public TerrainClusterLocalCanvas Canvas { get; }
            public TerrainClusterRoleSocketContract RoleSocket { get; }
            public TerrainClusterTraversalCompilation Traversal { get; }
            public TerrainClusterRouteWitnessReport Witness { get; }
            public TerrainClusterPatternRenderReport Render { get; }
        }
    }
}
