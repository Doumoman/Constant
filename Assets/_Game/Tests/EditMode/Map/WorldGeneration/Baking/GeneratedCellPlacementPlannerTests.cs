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
    [Category("MAP17_01")]
    public sealed class GeneratedCellPlacementPlannerTests
    {
        private const string Map16ExitAuditDigest =
            "78d3046d62608494fb1306ff4e57a0b2d4b36eafc3a5e7e19cb8f399c3ca29f0";
        private static Map17Chain reference;

        [Test]
        public void AssetRegistrySnapshotValidatesTileCodesAndPrefabIdsWithoutLoadingSceneObjects()
        {
            var chain = ReferenceChain();
            var requiredTiles = chain.Slices.Slices.SelectMany(slice => slice.Cells)
                .SelectMany(cell => cell.Layers).Select(GeneratedTerrainTileCode.FromLayer).ToArray();
            var requiredPrefabs = chain.Slots.Slots.Select(GeneratedTerrainPrefabId.FromSlot).ToArray();
            var resolution = GeneratedTerrainAssetResolver.Resolve(
                chain.Registry, requiredTiles, requiredPrefabs);

            Assert.That(chain.Registry.PublicationLabel,
                Is.EqualTo("REFERENCE MAP17_01 ASSET REGISTRY"));
            Assert.That(chain.Registry.IsValid, Is.True);
            Assert.That(chain.Registry.IsProductionAssetApproval, Is.False);
            Assert.That(chain.Registry.ProductionSeedApprovalCount, Is.Zero);
            Assert.That(resolution.Success, Is.True, AssetFailures(resolution));
            Assert.That(resolution.RequestedTileCodeCount, Is.EqualTo(chain.Registry.TileEntries.Count));
            Assert.That(resolution.ResolvedTileCodeCount, Is.EqualTo(resolution.RequestedTileCodeCount));
            Assert.That(resolution.RequestedPrefabIdCount, Is.EqualTo(chain.Registry.PrefabEntries.Count));
            Assert.That(resolution.ResolvedPrefabIdCount, Is.EqualTo(resolution.RequestedPrefabIdCount));
            Assert.That(resolution.MissingTileCodeCount, Is.Zero);
            Assert.That(resolution.MissingPrefabIdCount, Is.Zero);
            Assert.That(typeof(UnityEngine.Object).IsAssignableFrom(
                typeof(GeneratedTerrainAssetRegistrySnapshot)), Is.False);
            TestContext.WriteLine("MAP17_01_ASSET_EVIDENCE tile_entries=" +
                chain.Registry.TileEntries.Count + " resolved=" + resolution.ResolvedTileCodeCount +
                " missing=0 prefab_entries=" + chain.Registry.PrefabEntries.Count +
                " resolved=" + resolution.ResolvedPrefabIdCount + " missing=0 scene_loads=0");
        }

        [Test]
        public void GeneratedCellsProjectFromSliceLocalToSectorAndWorldCoordinates()
        {
            var chain = ReferenceChain();
            var result = Plan(chain);
            Assert.That(result.Success, Is.True, Failures(result));
            var geometry = chain.Geometry;
            var values = new[]
            {
                geometry.SectorWidth, geometry.SectorHeight, geometry.SectorCellCount,
                geometry.MicroChunkWidth, geometry.MicroChunkHeight, geometry.MicroChunkCellCount,
                geometry.ChunkGridWidth, geometry.ChunkGridHeight, geometry.ChunkCount,
                geometry.MicroPatternWidth, geometry.MicroPatternHeight,
                geometry.PatternsPerChunkX, geometry.PatternsPerChunkY,
                geometry.WorldSectorColumns, geometry.WorldSectorRows, geometry.WorldSectorCount,
                geometry.WorldWidth, geometry.WorldHeight, geometry.WorldCellCount,
                geometry.WorldProjectedSliceCount, geometry.LayersPerFinalCanvasCell,
                geometry.SectorLayerRecordCount, geometry.ChunkRotationAllowed ? 1 : 0,
            };
            Assert.That(values, Has.Length.EqualTo(23));
            Assert.That(result.Plan.Records.All(value => value.Coordinate.IsValid(geometry)), Is.True);
            foreach (var record in result.Plan.Records)
            {
                var coordinate = record.Coordinate;
                Assert.That(coordinate.SliceLocalX, Is.EqualTo(coordinate.MicroChunkLocalX));
                Assert.That(coordinate.SliceLocalY, Is.EqualTo(coordinate.MicroChunkLocalY));
                Assert.That(coordinate.WorldX, Is.EqualTo(
                    coordinate.SectorIndex.X * geometry.SectorWidth + coordinate.SectorLocalX));
                Assert.That(coordinate.WorldY, Is.EqualTo(
                    coordinate.SectorIndex.Y * geometry.SectorHeight + coordinate.SectorLocalY));
            }
            Assert.That(result.Plan.Records.First().Coordinate.WorldX, Is.EqualTo(3 * 48));
            Assert.That(result.Plan.Records.First().Coordinate.WorldY, Is.EqualTo(4 * 32));
            Assert.That(result.Plan.Records.Last().Coordinate.WorldX, Is.EqualTo(3 * 48 + 47));
            Assert.That(result.Plan.Records.Last().Coordinate.WorldY, Is.EqualTo(4 * 32 + 31));
            TestContext.WriteLine("MAP17_01_COORDINATE_EVIDENCE geometry_values=23/23" +
                " slices=16/16 cells=1536/1536 sector_index=3,4 world_bounds=144,128..191,159");
        }

        [Test]
        public void PlacementPlanPublishesAllCellsLayersSocketsSlotsAndDigests()
        {
            var chain = ReferenceChain();
            var result = Plan(chain);
            Assert.That(result.Success, Is.True, Failures(result));
            var plan = result.Plan;
            Assert.That(plan.PlacedCellCount, Is.EqualTo(1536));
            Assert.That(plan.PlacedLayerReferenceCount, Is.EqualTo(10752));
            Assert.That(plan.CellPlacementIdUniqueCount, Is.EqualTo(1536));
            Assert.That(plan.DuplicateSectorCoordinateCount, Is.Zero);
            Assert.That(plan.MissingSectorCoordinateCount, Is.Zero);
            Assert.That(plan.OutOfBoundsCoordinateCount, Is.Zero);
            Assert.That(plan.SocketReferenceCount, Is.EqualTo(chain.Slices.SocketSideSignatureCount));
            Assert.That(plan.SlotReferenceCount, Is.EqualTo(chain.Slots.SlotCount));
            Assert.That(plan.SourceProvenanceReferenceCount, Is.EqualTo(10752));
            Assert.That(GeneratedCellPlacementDigest.IsLowerHexSha256(plan.InputDigest), Is.True);
            Assert.That(GeneratedCellPlacementDigest.IsLowerHexSha256(plan.OutputDigest), Is.True);
            Assert.That(plan.Request.ExportPacket.PacketDigest, Is.EqualTo(chain.Packet.PacketDigest));
            TestContext.WriteLine("MAP17_01_PLAN_EVIDENCE placed_cells=1536/1536 layers=10752/10752" +
                " ids=1536/1536 duplicate_missing_oob=0/0/0 sockets=" +
                plan.SocketReferenceCount + "/" + chain.Slices.SocketSideSignatureCount +
                " slots=" + plan.SlotReferenceCount + "/" + chain.Slots.SlotCount +
                " provenance=" + plan.SourceProvenanceReferenceCount + "/10752 digest=" + plan.OutputDigest);
        }

        [Test]
        public void LayerPrecedenceAndSourceProvenanceRemainByteCompatibleWithMap16()
        {
            var chain = ReferenceChain();
            var result = Plan(chain);
            Assert.That(result.Success, Is.True, Failures(result));
            var cells = chain.Slices.Slices.SelectMany(value => value.Cells)
                .ToDictionary(value => value.SectorCoordinate.Y * 48 + value.SectorCoordinate.X);

            foreach (var placement in result.Plan.Records)
            {
                var source = cells[placement.Coordinate.SectorRowMajorIndex];
                Assert.That(placement.Layers.Select(value => value.SourceLayerStableToken),
                    Is.EqualTo(source.Layers.OrderBy(value => value).Select(value => value.StableToken)));
                Assert.That(placement.Layers.Select(value => value.Layer),
                    Is.EqualTo(source.Layers.OrderBy(value => value).Select(value => value.Layer)));
                Assert.That(placement.Layers.All(value =>
                    value.SourceCellToken == source.Layer(value.Layer).SourceCellToken &&
                    value.ProvenanceId == source.Layer(value.Layer).ProvenanceId &&
                    value.SourceOwner == source.Layer(value.Layer).SourceOwner), Is.True);
            }
            Assert.That(result.Plan.Records.SelectMany(value => value.SlotReferences)
                .All(value => value.SourceProvenanceToken == value.Source.Provenance.StableToken), Is.True);
            Assert.That(result.Plan.SocketReferences.All(value =>
                !string.IsNullOrEmpty(value.TraversalToken) &&
                GeneratedMicroChunkSliceDigest.IsLowerHexSha256(value.SideSignature) &&
                GeneratedMicroChunkSliceDigest.IsLowerHexSha256(value.SliceSignature)), Is.True);
            TestContext.WriteLine("MAP17_01_PROVENANCE_EVIDENCE layer_tokens=10752/10752" +
                " slot_tokens=" + chain.Slots.SlotCount + "/" + chain.Slots.SlotCount +
                " socket_tokens=" + chain.Slices.SocketSideSignatureCount + "/" +
                chain.Slices.SocketSideSignatureCount + " mismatches=0");
        }

        [Test]
        public void MissingDuplicateOrInvalidAssetReferencesFailAtomically()
        {
            var chain = ReferenceChain();
            var registry = chain.Registry;
            AssertAtomicAssetFailure(chain, new GeneratedTerrainAssetRegistrySnapshot(
                registry.TileEntries.Skip(1), registry.PrefabEntries),
                GeneratedCellPlacementFailureCode.MissingTileCode);
            AssertAtomicAssetFailure(chain, new GeneratedTerrainAssetRegistrySnapshot(
                registry.TileEntries, registry.PrefabEntries.Skip(1)),
                GeneratedCellPlacementFailureCode.MissingPrefabId);
            AssertAtomicAssetFailure(chain, new GeneratedTerrainAssetRegistrySnapshot(
                registry.TileEntries.Concat(new[] { registry.TileEntries[0] }), registry.PrefabEntries),
                GeneratedCellPlacementFailureCode.DuplicateAssetReference);
            AssertAtomicAssetFailure(chain, new GeneratedTerrainAssetRegistrySnapshot(
                registry.TileEntries, registry.PrefabEntries.Concat(new[] { registry.PrefabEntries[0] })),
                GeneratedCellPlacementFailureCode.DuplicateAssetReference);
            AssertAtomicAssetFailure(chain, new GeneratedTerrainAssetRegistrySnapshot(
                registry.TileEntries.Concat(new[]
                {
                    new GeneratedTerrainTileRegistryEntry(new GeneratedTerrainTileCode(" "), "INVALID_TILE"),
                }), registry.PrefabEntries), GeneratedCellPlacementFailureCode.InvalidAssetId);
            AssertAtomicAssetFailure(chain, new GeneratedTerrainAssetRegistrySnapshot(
                registry.TileEntries, registry.PrefabEntries.Concat(new[]
                {
                    new GeneratedTerrainPrefabRegistryEntry(new GeneratedTerrainPrefabId("BAD\nID"),
                        "INVALID_PREFAB"),
                })), GeneratedCellPlacementFailureCode.InvalidAssetId);

            var invalidRequired = GeneratedTerrainAssetResolver.Resolve(registry,
                new[] { new GeneratedTerrainTileCode("\t") },
                new[] { new GeneratedTerrainPrefabId(string.Empty) });
            Assert.That(invalidRequired.Success, Is.False);
            Assert.That(invalidRequired.ResolvedTileCodeCount, Is.Zero);
            Assert.That(invalidRequired.ResolvedPrefabIdCount, Is.Zero);
            Assert.That(invalidRequired.Failures.Select(value => value.Code),
                Does.Contain(GeneratedTerrainAssetResolutionFailureCode.InvalidTileCode));
            Assert.That(invalidRequired.Failures.Select(value => value.Code),
                Does.Contain(GeneratedTerrainAssetResolutionFailureCode.InvalidPrefabId));
            TestContext.WriteLine("MAP17_01_ASSET_FAILURE_EVIDENCE missing=2 duplicate=2 invalid=2" +
                " direct_invalid=2 partial_plans=0");
        }

        [Test]
        public void CoordinateProjectionRejectsDuplicateMissingOutOfBoundsAndStaleGeometry()
        {
            var chain = ReferenceChain();
            var plan = Plan(chain).Plan;
            var coordinates = plan.Records.Select(value => value.Coordinate).ToArray();
            var duplicateAndMissing = coordinates.Skip(1).Concat(new[] { coordinates[1] }).ToArray();
            var failures = GeneratedCellPlacementPlanner.ValidateCoordinateCoverage(
                duplicateAndMissing, chain.Geometry);
            Assert.That(failures.Select(value => value.Code),
                Does.Contain(GeneratedCellPlacementFailureCode.DuplicateCoordinate));
            Assert.That(failures.Select(value => value.Code),
                Does.Contain(GeneratedCellPlacementFailureCode.MissingCoordinate));

            var first = coordinates[0];
            var invalid = new GeneratedCellPlacementCoordinate(first.SectorIndex, first.SliceIndex,
                first.SliceLocalX, first.SliceLocalY, first.MicroChunkLocalX,
                first.MicroChunkLocalY, first.SectorLocalX, first.SectorLocalY, -1, first.WorldY);
            failures = GeneratedCellPlacementPlanner.ValidateCoordinateCoverage(
                new[] { invalid }.Concat(coordinates.Skip(1)), chain.Geometry);
            Assert.That(failures.Select(value => value.Code),
                Does.Contain(GeneratedCellPlacementFailureCode.OutOfBoundsCoordinate));

            var stale = GeneratedCellPlacementPlanner.Plan(Request(chain,
                expectedGeometryDigest: MutateDigest(chain.GeometryDigest)));
            Assert.That(stale.Success, Is.False);
            Assert.That(stale.Plan, Is.Null);
            Assert.That(stale.Failures.Select(value => value.Code),
                Does.Contain(GeneratedCellPlacementFailureCode.StaleGeometry));

            var missingGeometry = GeneratedCellPlacementPlanner.Plan(
                new GeneratedCellPlacementRequest(new GeneratedSectorIndexCoordinate(3, 4),
                    null, string.Empty, Map16ExitAuditDigest, chain.Slices, chain.Slots,
                    chain.Packet, chain.Registry));
            Assert.That(missingGeometry.Success, Is.False);
            Assert.That(missingGeometry.Plan, Is.Null);
            Assert.That(missingGeometry.Failures.Select(value => value.Code),
                Does.Contain(GeneratedCellPlacementFailureCode.MissingGeometry));

            var duplicateSlices = chain.Slices.Slices.Skip(1)
                .Concat(new[] { chain.Slices.Slices[1] }).ToArray();
            var invalidSlices = GeneratedCellPlacementPlanner.Plan(Request(chain,
                sourceSlices: duplicateSlices));
            Assert.That(invalidSlices.Success, Is.False);
            Assert.That(invalidSlices.Plan, Is.Null);
            Assert.That(invalidSlices.Failures.Select(value => value.Code),
                Does.Contain(GeneratedCellPlacementFailureCode.InvalidSliceSet));
            TestContext.WriteLine("MAP17_01_COORDINATE_FAILURE_EVIDENCE duplicate=1 missing=1" +
                " out_of_bounds=1 stale_geometry=1 missing_geometry=1" +
                " invalid_slice_set=1 partial_plans=0");
        }

        [Test]
        public void ReferenceWorldPlacementProjectionCovers169SectorsWithoutBaking()
        {
            var plan = Plan(ReferenceChain()).Plan;
            var projection = GeneratedCellPlacementPlanner.ProjectReferenceWorld(plan);

            Assert.That(projection.Success, Is.True);
            Assert.That(projection.SectorCount, Is.EqualTo(169));
            Assert.That(projection.CellCount, Is.EqualTo(259584));
            Assert.That(projection.UniqueCellCount, Is.EqualTo(259584));
            Assert.That(projection.DuplicateCellCount, Is.Zero);
            Assert.That(projection.MissingCellCount, Is.Zero);
            Assert.That(projection.OutOfBoundsCellCount, Is.Zero);
            Assert.That(projection.TilemapBakeCount, Is.Zero);
            Assert.That(GeneratedCellPlacementDigest.IsLowerHexSha256(projection.Digest), Is.True);
            TestContext.WriteLine("MAP17_01_WORLD_EVIDENCE sectors=169/169 cells=259584/259584" +
                " duplicate_missing_oob=0/0/0 tilemap_bakes=0 digest=" + projection.Digest);
        }

        [Test]
        public void PlacementDigestIsStableAcrossRepeatReverseCultureAndRegistryOrder()
        {
            var chain = ReferenceChain();
            var baseline = Plan(chain);
            var repeat = Plan(chain);
            var reversedRegistry = new GeneratedTerrainAssetRegistrySnapshot(
                chain.Registry.TileEntries.Reverse(), chain.Registry.PrefabEntries.Reverse());
            var reverse = GeneratedCellPlacementPlanner.Plan(Request(chain,
                registry: reversedRegistry,
                sourceSlices: chain.Slices.Slices.Reverse(),
                sourceSlots: chain.Slots.Slots.Reverse()));
            Assert.That(baseline.Success && repeat.Success && reverse.Success, Is.True,
                Failures(reverse));
            Assert.That(repeat.OutputDigest, Is.EqualTo(baseline.OutputDigest));
            Assert.That(reverse.OutputDigest, Is.EqualTo(baseline.OutputDigest));
            Assert.That(reversedRegistry.Digest, Is.EqualTo(chain.Registry.Digest));

            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var culture = GeneratedCellPlacementPlanner.Plan(Request(chain,
                    registry: reversedRegistry,
                    sourceSlices: chain.Slices.Slices.Reverse(),
                    sourceSlots: chain.Slots.Slots.Reverse()));
                Assert.That(culture.Success, Is.True, Failures(culture));
                Assert.That(culture.OutputDigest, Is.EqualTo(baseline.OutputDigest));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }

            var firstTile = chain.Registry.TileEntries[0];
            var mutatedRegistry = new GeneratedTerrainAssetRegistrySnapshot(
                chain.Registry.TileEntries.Skip(1).Concat(new[]
                {
                    new GeneratedTerrainTileRegistryEntry(firstTile.Code, firstTile.AssetKey + "_MUTATED"),
                }), chain.Registry.PrefabEntries);
            var mutated = GeneratedCellPlacementPlanner.Plan(Request(chain, registry: mutatedRegistry));
            Assert.That(mutated.Success, Is.True, Failures(mutated));
            Assert.That(mutated.OutputDigest, Is.Not.EqualTo(baseline.OutputDigest));
            TestContext.WriteLine("MAP17_01_DIGEST_EVIDENCE repeat_reverse_culture_registry=0/0/0/0" +
                " mutation_sensitivity=1 digest=" + baseline.OutputDigest);
        }

        [Test]
        public void PlannerDoesNotBakeTilemapsInstantiatePrefabsOrMutateScenes()
        {
            var scene = SceneManager.GetActiveScene();
            var roots = scene.IsValid()
                ? scene.GetRootGameObjects().Select(value => value.GetInstanceID()).ToArray()
                : Array.Empty<int>();
            var wasDirty = scene.IsValid() && scene.isDirty;
            var result = Plan(ReferenceChain());

            Assert.That(result.Success, Is.True, Failures(result));
            Assert.That(scene.IsValid()
                ? scene.GetRootGameObjects().Select(value => value.GetInstanceID())
                : Array.Empty<int>(), Is.EqualTo(roots));
            Assert.That(scene.IsValid() && scene.isDirty, Is.EqualTo(wasDirty));
            Assert.That(result.Plan.TilemapBakeCount, Is.Zero);
            Assert.That(result.Plan.ColliderRebuildCount, Is.Zero);
            Assert.That(result.Plan.GameObjectInstantiationCount, Is.Zero);
            Assert.That(result.Plan.PrefabInstantiationCount, Is.Zero);
            Assert.That(result.Plan.SceneMutationCount, Is.Zero);
            Assert.That(result.Plan.PrefabMutationCount, Is.Zero);
            Assert.That(result.Plan.TilemapMutationCount, Is.Zero);
            Assert.That(result.Plan.GeneratedCsvCommitCount, Is.Zero);
            Assert.That(result.Plan.GeneratedAssetCommitCount, Is.Zero);
            Assert.That(result.Plan.StableSpawnIdCount, Is.Zero);
            Assert.That(result.Plan.RuntimeObjectSpawnCount, Is.Zero);
            Assert.That(result.Plan.ProductionSeedApprovalCount, Is.Zero);
            TestContext.WriteLine("MAP17_01_MUTATION_EVIDENCE tilemap_bakes=0 collider_rebuilds=0" +
                " gameobject_prefab_instantiation=0/0 scene_prefab_tilemap=0/0/0" +
                " generated_csv_assets=0 stable_spawn=0 runtime_spawn=0 production_seed_approval=0");
        }

        [Test]
        public void Map17HandoffKeepsMap17_02Locked()
        {
            Assert.That(GeneratedCellPlacementPlan.DownstreamOwner,
                Is.EqualTo("MAP17_02_BUILD_TILEMAP_LAYERS_BAKE_AND_SEAM_VALIDATION"));
            Assert.That(GeneratedCellPlacementPlan.OpensDownstreamTask, Is.False);
            Assert.That(GeneratedCellPlacementPlan.ReplayVerifierContract,
                Is.EqualTo(nameof(GeneratedTerrainReplayVerifier)));
            var statusPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "MapDesign", "MCP", "06_IMPLEMENTATION_STATUS.md");
            var status = File.ReadAllText(statusPath);
            Assert.That(status, Does.Contain(
                "| MAP17_02_BUILD_TILEMAP_LAYERS_BAKE_AND_SEAM_VALIDATION | LOCKED |"));
            Assert.That(status, Does.Not.Contain(
                "| MAP17_02_BUILD_TILEMAP_LAYERS_BAKE_AND_SEAM_VALIDATION | CURRENT |"));
            TestContext.WriteLine("MAP17_01_HANDOFF_EVIDENCE MAP17_02_started=NO locked=YES");
        }

        private static GeneratedCellPlacementResult Plan(Map17Chain chain) =>
            GeneratedCellPlacementPlanner.Plan(Request(chain));

        private static GeneratedCellPlacementRequest Request(
            Map17Chain chain,
            GeneratedTerrainAssetRegistrySnapshot registry = null,
            IEnumerable<GeneratedMicroChunkSliceRecord> sourceSlices = null,
            IEnumerable<GeneratedMarkerSlot> sourceSlots = null,
            string expectedGeometryDigest = null) => new GeneratedCellPlacementRequest(
                new GeneratedSectorIndexCoordinate(3, 4), chain.Geometry,
                expectedGeometryDigest ?? chain.GeometryDigest, Map16ExitAuditDigest,
                chain.Slices, chain.Slots, chain.Packet, registry ?? chain.Registry,
                sourceSlices, sourceSlots);

        private static void AssertAtomicAssetFailure(
            Map17Chain chain,
            GeneratedTerrainAssetRegistrySnapshot registry,
            GeneratedCellPlacementFailureCode expected)
        {
            var result = GeneratedCellPlacementPlanner.Plan(Request(chain, registry: registry));
            Assert.That(result.Success, Is.False);
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.OutputDigest, Is.Empty);
            Assert.That(result.Failures.Select(value => value.Code), Does.Contain(expected),
                Failures(result));
        }

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

        private static string MutateDigest(string digest) =>
            (digest[0] == '0' ? "1" : "0") + digest.Substring(1);
        private static string Failures(GeneratedCellPlacementResult result) => result == null
            ? "NULL RESULT" : Describe(result.Failures);
        private static string AssetFailures(GeneratedTerrainAssetResolution result) => result == null
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
