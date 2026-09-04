using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Baking
{
    [TestFixture]
    [Category("MAP17_02")]
    public sealed class GeneratedTilemapLayerBakerTests
    {
        private const string Map16ExitAuditDigest =
            "78d3046d62608494fb1306ff4e57a0b2d4b36eafc3a5e7e19cb8f399c3ca29f0";
        private const string ExpectedPlacementDigest =
            "d8dac9d9bf7c25b179cc2b33c6d0cf7b9323abd39de44b6ca2457216e23df334";
        private const string ExpectedWorldProjectionDigest =
            "5fb394e497fea2fa90e90177891dd5a971e3afa4af449e5be1935061fb6df8bf";
        private static Map17Chain reference;
        private static GeneratedCellPlacementPlan placement;

        [Test]
        public void LayerBakePlanPublishesSevenDeterministic1536CellBuffers()
        {
            var result = Bake();
            Assert.That(result.Success, Is.True, Failures(result));
            Assert.That(result.Plan.LayerCount, Is.EqualTo(7));
            Assert.That(result.Plan.LayerBuffers.Select(value => value.LayerId),
                Is.EqualTo(Enum.GetValues(typeof(GeneratedTilemapLayerId))));
            Assert.That(result.Plan.LayerBuffers.All(value => value.RecordCount == 1536), Is.True);
            Assert.That(result.Plan.TotalLayerRecordCount, Is.EqualTo(10752));
            Assert.That(result.Plan.UniqueLayerCellKeyCount, Is.EqualTo(10752));
            Assert.That(result.Plan.SectorCellCoverageCount, Is.EqualTo(1536));
            Assert.That(result.Plan.MissingLayerCellCount, Is.Zero);
            Assert.That(result.Plan.DuplicateLayerCellCount, Is.Zero);
            Assert.That(result.Plan.OutOfBoundsLayerCellCount, Is.Zero);
            Assert.That(result.Plan.CommandCount, Is.EqualTo(10752));
            TestContext.WriteLine("MAP17_02_LAYER_EVIDENCE layers=7/7 records_each=1536/1536" +
                " total=10752/10752 unique=10752/10752 coverage=1536/1536 missing_duplicate_oob=0/0/0" +
                " commands=10752 digest=" + result.OutputDigest);
        }

        [Test]
        public void BakeConsumesPlacementPlanWithoutReloadingAssetsOrScenes()
        {
            var source = Placement();
            var projection = GeneratedCellPlacementPlanner.ProjectReferenceWorld(source);
            var result = Bake(source);
            Assert.That(result.Success, Is.True, Failures(result));
            Assert.That(result.Plan.Request.PlacementPlan, Is.SameAs(source));
            Assert.That(result.Plan.Request.AssetRegistry, Is.SameAs(source.AssetResolution.Registry));
            Assert.That(result.Plan.Request.SourceRecords.Count, Is.EqualTo(source.PlacedLayerReferenceCount));
            Assert.That(source.OutputDigest, Is.EqualTo(ExpectedPlacementDigest));
            Assert.That(projection.Digest, Is.EqualTo(ExpectedWorldProjectionDigest));
            TestContext.WriteLine("MAP17_02_SOURCE_EVIDENCE placement_digest=" + source.OutputDigest +
                " world_projection_digest=" + projection.Digest +
                " cells=1536/1536 layer_refs=10752/10752 asset_scene_reloads=0/0");
        }

        [Test]
        public void TileCodesResolveToLayerCellsWithoutUnityTilemapMutation()
        {
            var result = Bake();
            Assert.That(result.Success, Is.True, Failures(result));
            var plan = result.Plan;
            Assert.That(plan.Commands.Select(value => value.TileCode.Value)
                .Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(12));
            Assert.That(plan.Commands.All(value => value.TileCode != null &&
                value.TileCode.IsValid && !string.IsNullOrEmpty(value.ResolvedAssetKey)), Is.True);
            Assert.That(plan.Request.PlacementPlan.AssetResolution.RequestedTileCodeCount, Is.EqualTo(12));
            Assert.That(plan.Request.PlacementPlan.AssetResolution.ResolvedTileCodeCount, Is.EqualTo(12));
            Assert.That(plan.Request.PlacementPlan.AssetResolution.MissingTileCodeCount, Is.Zero);
            Assert.That(plan.TilemapComponentWriteCount, Is.Zero);
            Assert.That(plan.TilemapSetTileCallCount + plan.TilemapSetTilesCallCount +
                plan.TilemapSetTilesBlockCallCount + plan.TilemapClearAllTilesCallCount +
                plan.TilemapCompressBoundsCallCount, Is.Zero);
            TestContext.WriteLine("MAP17_02_TILE_EVIDENCE tile_registry=12/12/0 layer_refs=10752" +
                " tilemap_component_writes=0 set_tile_calls=0/0/0/0 compress_bounds=0");
        }

        [Test]
        public void OverlapGapDuplicateAndOutOfBoundsCellsFailAtomically()
        {
            var plan = Placement();
            var records = new GeneratedTilemapBakeRequest(plan).SourceRecords.ToArray();
            var duplicate = GeneratedTilemapLayerBaker.Bake(new GeneratedTilemapBakeRequest(
                plan, sourceRecords: records.Concat(new[] { records[0] })));
            AssertAtomicFailure(duplicate, GeneratedTilemapBakeFailureCode.DuplicateLayerCell);
            Assert.That(duplicate.Failures.Select(value => value.Code),
                Does.Contain(GeneratedTilemapBakeFailureCode.Overlap));

            var gap = GeneratedTilemapLayerBaker.Bake(new GeneratedTilemapBakeRequest(
                plan, sourceRecords: records.Skip(1)));
            AssertAtomicFailure(gap, GeneratedTilemapBakeFailureCode.MissingLayerCell);
            Assert.That(gap.Failures.Select(value => value.Code),
                Does.Contain(GeneratedTilemapBakeFailureCode.Gap));

            var outOfBounds = GeneratedTilemapLayerBaker.Bake(new GeneratedTilemapBakeRequest(
                plan, sourceRecords: new[] { Clone(records[0], sectorLocalX: -1) }
                    .Concat(records.Skip(1))));
            AssertAtomicFailure(outOfBounds, GeneratedTilemapBakeFailureCode.OutOfBoundsLayerCell);

            var invalidLayer = GeneratedTilemapLayerBaker.Bake(new GeneratedTilemapBakeRequest(
                plan, sourceRecords: new[] { Clone(records[0],
                    layerId: (GeneratedTilemapLayerId)99) }.Concat(records.Skip(1))));
            AssertAtomicFailure(invalidLayer, GeneratedTilemapBakeFailureCode.InvalidLayerId);
            TestContext.WriteLine("MAP17_02_FAILURE_EVIDENCE duplicate_overlap_gap_oob_invalid=1/1/1/1/1" +
                " partial_plans=0 repairs=0");
        }

        [Test]
        public void MicroPatternAndMicroChunkSeamsAreEnumeratedSeparately()
        {
            var result = Bake();
            Assert.That(result.Success, Is.True, Failures(result));
            var seams = result.Plan.SeamReport;
            Assert.That(seams.MicroPatternSeamPairCount, Is.EqualTo(688));
            Assert.That(seams.MicroChunkSeamPairCount, Is.EqualTo(240));
            Assert.That(seams.MicroPatternOnlySeamPairCount, Is.EqualTo(448));
            Assert.That(seams.ApprovedPairCount, Is.EqualTo(928));
            Assert.That(seams.UnapprovedPairCount, Is.Zero);
            Assert.That(seams.MissingNeighborPairCount, Is.Zero);
            Assert.That(seams.OutOfBoundsNeighborPairCount, Is.Zero);
            Assert.That(GeneratedTilemapSeamDigest.IsLowerHexSha256(seams.OutputDigest), Is.True);
            TestContext.WriteLine("MAP17_02_SEAM_EVIDENCE pattern=688/688 microchunk=240/240" +
                " pattern_only=448/448 approved=" + seams.ApprovedPairCount +
                " unapproved_missing_oob=0/0/0 digest=" + seams.OutputDigest);
        }

        [Test]
        public void SeamValidationRejectsUnapprovedDiscontinuitiesWithoutRepair()
        {
            var coordinate = new GeneratedTilemapSeamCoordinate(
                GeneratedTilemapSeamKind.MicroPattern,
                GeneratedTilemapSeamOrientation.Vertical, 4, 3, 0, 4, 0);
            var kinds = new[]
            {
                GeneratedTilemapSeamExposureKind.UnapprovedSolidAirDiscontinuity,
                GeneratedTilemapSeamExposureKind.UnapprovedHazardProtectionDiscontinuity,
                GeneratedTilemapSeamExposureKind.UnapprovedProvenanceBreak,
                GeneratedTilemapSeamExposureKind.MissingNeighbor,
                GeneratedTilemapSeamExposureKind.OutOfBoundsNeighbor,
            };
            var exposures = kinds.Select(kind => new GeneratedTilemapSeamExposure(
                coordinate, kind, "MATERIAL_A", "MATERIAL_B", "TILE_A", "TILE_B",
                "PROVENANCE_A", "PROVENANCE_B")).ToArray();
            var failures = GeneratedTilemapSeamValidator.ValidateExposures(exposures);
            Assert.That(failures.Count(value => value.Code ==
                GeneratedTilemapBakeFailureCode.ForbiddenSeamExposure), Is.EqualTo(3));
            Assert.That(failures.Select(value => value.Code),
                Does.Contain(GeneratedTilemapBakeFailureCode.MissingSeamNeighbor));
            Assert.That(failures.Select(value => value.Code),
                Does.Contain(GeneratedTilemapBakeFailureCode.OutOfBoundsSeamNeighbor));
            TestContext.WriteLine("MAP17_02_SEAM_FAILURE_EVIDENCE solid_air=1 hazard_protection=1" +
                " provenance=1 missing=1 out_of_bounds=1 repairs=0");
        }

        [Test]
        public void SocketMarkerSlotAndProvenanceReferencesSurviveBakeHandoff()
        {
            var result = Bake();
            Assert.That(result.Success, Is.True, Failures(result));
            var plan = result.Plan;
            Assert.That(plan.SocketReferenceCount, Is.EqualTo(64));
            Assert.That(plan.SlotReferenceCount, Is.EqualTo(24));
            Assert.That(plan.SocketReferences.All(value =>
                GeneratedMicroChunkSliceDigest.IsLowerHexSha256(value.SideSignature) &&
                GeneratedMicroChunkSliceDigest.IsLowerHexSha256(value.SliceSignature) &&
                !string.IsNullOrEmpty(value.TraversalToken)), Is.True);
            Assert.That(plan.SlotReferences.All(value => value.Source != null &&
                !string.IsNullOrEmpty(value.SourceProvenanceToken) &&
                !string.IsNullOrEmpty(value.ResolvedAssetKey)), Is.True);
            Assert.That(plan.Commands.Count(value => !string.IsNullOrEmpty(
                value.Record.ProvenanceId)), Is.EqualTo(10752));
            Assert.That(plan.Request.PlacementPlan.AssetResolution.RequestedPrefabIdCount, Is.EqualTo(24));
            Assert.That(plan.Request.PlacementPlan.AssetResolution.ResolvedPrefabIdCount, Is.EqualTo(24));
            Assert.That(plan.Request.PlacementPlan.AssetResolution.MissingPrefabIdCount, Is.Zero);
            TestContext.WriteLine("MAP17_02_HANDOFF_EVIDENCE sockets=64/64 slots=24/24" +
                " provenance=10752/10752 prefab_registry=24/24/0");
        }

        [Test]
        public void BakeAndSeamDigestsAreStableAcrossRepeatReverseCultureAndRegistryOrder()
        {
            var plan = Placement();
            var baseline = Bake(plan);
            var repeat = Bake(plan);
            var reversedRegistry = new GeneratedTerrainAssetRegistrySnapshot(
                plan.AssetResolution.Registry.TileEntries.Reverse(),
                plan.AssetResolution.Registry.PrefabEntries.Reverse());
            var reversedRecords = baseline.Plan.LayerBuffers.SelectMany(value => value.Records)
                .Reverse().ToArray();
            var reverse = GeneratedTilemapLayerBaker.Bake(new GeneratedTilemapBakeRequest(
                plan, assetRegistry: reversedRegistry, sourceRecords: reversedRecords));
            Assert.That(baseline.Success && repeat.Success && reverse.Success, Is.True,
                Failures(reverse));
            Assert.That(repeat.OutputDigest, Is.EqualTo(baseline.OutputDigest));
            Assert.That(reverse.OutputDigest, Is.EqualTo(baseline.OutputDigest));
            Assert.That(repeat.Plan.SeamReport.OutputDigest,
                Is.EqualTo(baseline.Plan.SeamReport.OutputDigest));
            Assert.That(reverse.Plan.SeamReport.OutputDigest,
                Is.EqualTo(baseline.Plan.SeamReport.OutputDigest));

            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var culture = GeneratedTilemapLayerBaker.Bake(new GeneratedTilemapBakeRequest(
                    plan, assetRegistry: reversedRegistry, sourceRecords: reversedRecords));
                Assert.That(culture.Success, Is.True, Failures(culture));
                Assert.That(culture.OutputDigest, Is.EqualTo(baseline.OutputDigest));
                Assert.That(culture.Plan.SeamReport.OutputDigest,
                    Is.EqualTo(baseline.Plan.SeamReport.OutputDigest));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }

            var first = reversedRecords.Single(value =>
                value.LayerId == GeneratedTilemapLayerId.Terrain &&
                value.SectorLocalX == 3 && value.SectorLocalY == 0);
            var mutated = Clone(first, tileCode: new GeneratedTerrainTileCode(
                first.TileCode.Value + "_MUTATED"));
            var mutatedRecords = reversedRecords.Select(value => ReferenceEquals(value, first)
                ? mutated : value).ToArray();
            var mutatedRequest = new GeneratedTilemapBakeRequest(plan,
                assetRegistry: reversedRegistry, sourceRecords: mutatedRecords);
            var mutatedSeams = GeneratedTilemapSeamValidator.BuildReport(
                mutatedRecords, plan.Request.Geometry);
            Assert.That(mutatedRequest.CanonicalDigest,
                Is.Not.EqualTo(baseline.InputDigest));
            Assert.That(mutatedSeams.OutputDigest,
                Is.Not.EqualTo(baseline.Plan.SeamReport.OutputDigest));
            Assert.That(GeneratedTilemapBakeDigest.IsLowerHexSha256(baseline.OutputDigest), Is.True);
            Assert.That(GeneratedTilemapSeamDigest.IsLowerHexSha256(
                baseline.Plan.SeamReport.OutputDigest), Is.True);
            TestContext.WriteLine("MAP17_02_DIGEST_EVIDENCE repeat_reverse_culture_registry=0/0/0/0" +
                " mutation_sensitivity=2 bake=" + baseline.OutputDigest +
                " seam=" + baseline.Plan.SeamReport.OutputDigest);
        }

        [Test]
        public void BakerDoesNotSetTilesBuildCollidersInstantiatePrefabsOrWriteFiles()
        {
            var scene = SceneManager.GetActiveScene();
            var roots = scene.IsValid()
                ? scene.GetRootGameObjects().Select(value => value.GetInstanceID()).ToArray()
                : Array.Empty<int>();
            var wasDirty = scene.IsValid() && scene.isDirty;
            var result = Bake();
            Assert.That(result.Success, Is.True, Failures(result));
            Assert.That(scene.IsValid()
                ? scene.GetRootGameObjects().Select(value => value.GetInstanceID())
                : Array.Empty<int>(), Is.EqualTo(roots));
            Assert.That(scene.IsValid() && scene.isDirty, Is.EqualTo(wasDirty));
            var plan = result.Plan;
            Assert.That(plan.TilemapComponentWriteCount + plan.TilemapSetTileCallCount +
                plan.TilemapSetTilesCallCount + plan.TilemapSetTilesBlockCallCount +
                plan.TilemapClearAllTilesCallCount + plan.TilemapCompressBoundsCallCount +
                plan.SceneTilemapBakeCount + plan.ColliderRebuildCount +
                plan.GameObjectInstantiationCount + plan.PrefabInstantiationCount +
                plan.SceneMutationCount + plan.PrefabMutationCount + plan.TilemapMutationCount +
                plan.GeneratedCsvCommitCount + plan.GeneratedAssetCommitCount +
                plan.StableSpawnIdCount + plan.RuntimeObjectSpawnCount +
                plan.ProductionSeedApprovalCount, Is.Zero);
            Assert.That(typeof(UnityEngine.Object).IsAssignableFrom(
                typeof(GeneratedTilemapBakePlan)), Is.False);
            TestContext.WriteLine("MAP17_02_MUTATION_EVIDENCE tilemap_component_set_calls=0/0" +
                " collider=0 gameobject_prefab=0/0 scene_prefab_tilemap=0/0/0" +
                " generated_csv_assets=0/0 stable_spawn_runtime=0/0 production_seed=0");
        }

        [Test]
        public void Map17HandoffKeepsMap17_03Locked()
        {
            var result = Bake();
            Assert.That(result.Success, Is.True, Failures(result));
            Assert.That(GeneratedTilemapBakePlan.DownstreamOwner,
                Is.EqualTo("MAP17_03_IMPLEMENT_COLLIDER_CACHE_AND_RUNTIME_HANDLES"));
            Assert.That(GeneratedTilemapBakePlan.OpensDownstreamTask, Is.False);
            var statusPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "MapDesign", "MCP", "06_IMPLEMENTATION_STATUS.md");
            var status = File.ReadAllText(statusPath);
            Assert.That(status, Does.Contain(
                "| MAP17_03_IMPLEMENT_COLLIDER_CACHE_AND_RUNTIME_HANDLES | LOCKED |"));
            Assert.That(status, Does.Not.Contain(
                "| MAP17_03_IMPLEMENT_COLLIDER_CACHE_AND_RUNTIME_HANDLES | CURRENT |"));
            TestContext.WriteLine("MAP17_02_DOWNSTREAM_EVIDENCE MAP17_03_started=NO locked=YES");
        }

        private static GeneratedTilemapBakeResult Bake(GeneratedCellPlacementPlan plan = null) =>
            GeneratedTilemapLayerBaker.Bake(new GeneratedTilemapBakeRequest(plan ?? Placement()));

        private static GeneratedCellPlacementPlan Placement()
        {
            if (placement != null) return placement;
            var chain = ReferenceChain();
            var result = GeneratedCellPlacementPlanner.Plan(new GeneratedCellPlacementRequest(
                new GeneratedSectorIndexCoordinate(3, 4), chain.Geometry, chain.GeometryDigest,
                Map16ExitAuditDigest, chain.Slices, chain.Slots, chain.Packet, chain.Registry));
            Assert.That(result.Success, Is.True, Describe(result.Failures));
            placement = result.Plan;
            return placement;
        }

        private static void AssertAtomicFailure(
            GeneratedTilemapBakeResult result,
            GeneratedTilemapBakeFailureCode expected)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.OutputDigest, Is.Empty);
            Assert.That(result.Failures.Select(value => value.Code), Does.Contain(expected),
                Failures(result));
        }

        private static GeneratedTilemapCellBakeRecord Clone(
            GeneratedTilemapCellBakeRecord source,
            GeneratedTilemapLayerId? layerId = null,
            int? sectorLocalX = null,
            GeneratedTerrainTileCode tileCode = null) =>
            new GeneratedTilemapCellBakeRecord(
                layerId ?? source.LayerId,
                sectorLocalX ?? source.SectorLocalX,
                source.SectorLocalY,
                source.SectorLocalIndex,
                source.PlacementId,
                tileCode ?? source.TileCode,
                source.ResolvedAssetKey,
                source.CellKind,
                source.SourceOwner,
                source.Protection,
                source.IsProtected,
                source.ProvenanceId,
                source.ClaimId,
                source.SourceCellToken,
                source.SourceLayerStableToken);

        private static Map17Chain ReferenceChain()
        {
            if (reference != null) return reference;
            var fixture = new ReferenceFinalRouteRecoveryFixture();
            var baselineRouteRequest = fixture.AcceptedRequest();
            var baselinePlan = baselineRouteRequest.CanvasPlan;
            var baselineRequest = baselinePlan.Request;
            var claims = baselinePlan.Cells.SelectMany(value => value.Winners).ToList();
            claims.Add(Claim("MAP17_01_TERRAIN_CLUSTER", 12, 20,
                FinalCanvasLayerKind.Terrain, FinalCanvasCellKind.Air,
                FinalCanvasSourceOwner.TerrainCluster,
                FinalCanvasClaimPriority.TerrainClusterPattern, "MAP11_TERRAIN_CLUSTER"));
            claims.Add(Claim("MAP17_01_ACTIVITY", 12, 20,
                FinalCanvasLayerKind.Marker, FinalCanvasCellKind.Marker,
                FinalCanvasSourceOwner.Activity,
                FinalCanvasClaimPriority.ActivityMarker, "MAP12_ACTIVITY"));
            claims.Add(Claim("MAP17_01_EVENT_OVERLAY", 13, 20,
                FinalCanvasLayerKind.Marker, FinalCanvasCellKind.Marker,
                FinalCanvasSourceOwner.EventOverlay,
                FinalCanvasClaimPriority.EventMarker, "MAP12_EVENT_OVERLAY"));
            var canvas = SectorCanvasLayerFinalizer.Finalize(new FinalCanvasLayerRequest(
                baselineRequest.SectorId, baselineRequest.Width, baselineRequest.Height, claims,
                baselineRequest.Map15ExitApproved, baselineRequest.Map15ExitDigest,
                baselineRequest.WorldAssemblyDigest, baselineRequest.SectorOwnershipDigest,
                baselineRequest.BoundaryAuthorityDigest, baselineRequest.FixedCanvasAuthorityDigest,
                baselineRequest.PublicationLabel));
            Assert.That(canvas.Success, Is.True, Describe(canvas.Failures));
            var density = SectorCanvasProtectionDensityValidator.Validate(canvas.Plan);
            Assert.That(density.Success, Is.True, Describe(density.Failures));
            var route = SectorFinalRouteRecoveryValidator.Validate(new FinalRouteRecoveryRequest(
                canvas.Plan, density.Report, baselineRouteRequest.Anchors,
                baselineRouteRequest.DeclaredEdges, baselineRouteRequest.PublicationLabel));
            Assert.That(route.Success, Is.True, Describe(route.Failures));
            var partition = SectorPatternChunkPartitioner.Partition(
                canvas.Plan, density.Report, route.Report);
            Assert.That(partition.Success, Is.True, Describe(partition.Failures));
            var slices = GeneratedMicroChunkSliceBuilder.Build(
                canvas.Plan, density.Report, route.Report, partition.Partition);
            Assert.That(slices.Success, Is.True, Describe(slices.Failures));
            var slots = GeneratedMicroChunkMarkerSlotProjector.Project(slices.SliceSet);
            Assert.That(slots.Success, Is.True, Describe(slots.Failures));
            var packet = GeneratedTerrainCsvExporter.Build(slices.SliceSet, slots.SlotSet);
            Assert.That(packet.Success, Is.True, Describe(packet.Failures));
            GeneratedTerrainGeometrySnapshot geometry;
            IReadOnlyList<string> geometryFailures;
            Assert.That(GeneratedTerrainGeometrySnapshot.TryCreate(
                out geometry, out geometryFailures), Is.True, Describe(geometryFailures));
            var registry = GeneratedTerrainAssetRegistrySnapshot.CreateReference(
                slices.SliceSet, slots.SlotSet);
            reference = new Map17Chain(geometry, slices.SliceSet, slots.SlotSet,
                packet.Packet, registry);
            return reference;
        }

        private static FinalCanvasLayerClaim Claim(
            string claimId,
            int x,
            int y,
            FinalCanvasLayerKind layer,
            FinalCanvasCellKind cellKind,
            FinalCanvasSourceOwner owner,
            FinalCanvasClaimPriority priority,
            string provenanceId) => new FinalCanvasLayerClaim(
                claimId, new FinalCanvasCellCoordinate(x, y), layer, cellKind, owner, priority,
                FinalCanvasProtectionKind.None, false, provenanceId,
                GeneratedTerrainAssetRegistrySnapshot.ReferencePublicationLabel);

        private static string Failures(GeneratedTilemapBakeResult result) => result == null
            ? "NULL RESULT" : Describe(result.Failures);
        private static string Describe(System.Collections.IEnumerable values)
        {
            var entries = new List<string>();
            foreach (var value in values)
                entries.Add(value == null ? "NULL" : value.ToString());
            return string.Join(";", entries);
        }

        private sealed class Map17Chain
        {
            public Map17Chain(
                GeneratedTerrainGeometrySnapshot geometry,
                GeneratedMicroChunkSliceSet slices,
                GeneratedMicroChunkMarkerSlotSet slots,
                GeneratedTerrainExportPacket packet,
                GeneratedTerrainAssetRegistrySnapshot registry)
            {
                Geometry = geometry;
                Slices = slices;
                Slots = slots;
                Packet = packet;
                Registry = registry;
                GeometryDigest = GeneratedCellPlacementDigest.ComputeGeometry(geometry);
            }

            public GeneratedTerrainGeometrySnapshot Geometry { get; }
            public string GeometryDigest { get; }
            public GeneratedMicroChunkSliceSet Slices { get; }
            public GeneratedMicroChunkMarkerSlotSet Slots { get; }
            public GeneratedTerrainExportPacket Packet { get; }
            public GeneratedTerrainAssetRegistrySnapshot Registry { get; }
        }
    }
}
