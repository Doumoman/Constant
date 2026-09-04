using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Baking
{
    internal static class GeneratedTerrainPerformanceHarness
    {
        private const string Map16ExitAuditDigest =
            "78d3046d62608494fb1306ff4e57a0b2d4b36eafc3a5e7e19cb8f399c3ca29f0";
        private const string GeneratorVersion = "MAP17_REFERENCE_GENERATOR_V1";
        private const string DataVersion = "MAP17_REFERENCE_DATA_V1";
        private const string SeedIdentity = "MAP17_REFERENCE_SEED";
        public const string ExpectedManifestDigest =
            "18bb9bd0ada73c2c84b9b400675d792a0e9c206f4ee5bd5eec897468154cd27a";
        public const string ExpectedPayloadDigest =
            "af88b4751877d4a03b0854eefea089bab70c542717d676d8eb52655b67ebac04";
        public const string ExpectedRegenerationApplyDigest =
            "13a1d61f92382f05460e7bc5c39f75b39c8e24850918bd7b94e8ace330504568";

        private static readonly GeneratedSectorCoordinate SourceSector =
            new GeneratedSectorCoordinate(3, 4);
        private static readonly Dictionary<int, GeneratedColliderRebuildPlan> Rebuilds =
            new Dictionary<int, GeneratedColliderRebuildPlan>();
        private static Map17Chain reference;
        private static GeneratedCellPlacementPlan placement;
        private static GeneratedTilemapBakePlan bake;
        private static GeneratedColliderCacheEntry[] entries;
        private static GeneratedSectorRuntimeHandle[] handles;
        private static GeneratedColliderCacheSnapshot cache;
        private static GeneratedSectorStreamingResult streaming;
        private static GeneratedSectorModificationAuthority authority;
        private static ModificationFixture baselineModification;
        private static GeneratedSaveManifestResult manifest;

        public static GeneratedTerrainPerformanceReport Run(bool reverseOperationOrder = false)
        {
            var operations = new[]
            {
                Operation(GeneratedTerrainPerformanceOperation.Placement, ObservePlacement),
                Operation(GeneratedTerrainPerformanceOperation.LayerBake, ObserveLayerBake),
                Operation(GeneratedTerrainPerformanceOperation.SeamValidation, ObserveSeamValidation),
                Operation(GeneratedTerrainPerformanceOperation.ColliderCache, ObserveColliderCache),
                Operation(GeneratedTerrainPerformanceOperation.StreamWindow, ObserveStreamWindow),
                Operation(GeneratedTerrainPerformanceOperation.Transition, ObserveTransition),
                Operation(GeneratedTerrainPerformanceOperation.ModificationStorage,
                    ObserveModificationStorage),
                Operation(GeneratedTerrainPerformanceOperation.SaveManifest, ObserveSaveManifest),
                Operation(GeneratedTerrainPerformanceOperation.RegenApply, ObserveRegenerationApply),
                Operation(GeneratedTerrainPerformanceOperation.HashMismatch, ObserveHashMismatch),
            };
            var ordered = reverseOperationOrder ? operations.Reverse() : operations.AsEnumerable();
            var samples = new List<GeneratedTerrainPerformanceSample>();
            foreach (var operation in ordered)
            {
                for (var warmup = 0; warmup < GeneratedTerrainPerformanceBudget.ReferenceWarmupIterations;
                     warmup++) operation.Value();
                for (var iteration = 0;
                     iteration < GeneratedTerrainPerformanceBudget.ReferenceMeasuredIterations;
                     iteration++)
                {
                    var stopwatch = Stopwatch.StartNew();
                    var observation = operation.Value();
                    stopwatch.Stop();
                    samples.Add(new GeneratedTerrainPerformanceSample(operation.Key, iteration,
                        observation.OperationCount, stopwatch.ElapsedTicks,
                        stopwatch.Elapsed.TotalMilliseconds, "MANAGED_ALLOCATION_DIAGNOSTIC_ONLY",
                        observation.StructuralDigest, observation.Metrics));
                }
            }
            return new GeneratedTerrainPerformanceReport(GeneratedTerrainPerformanceBudget.Reference,
                GeneratedTerrainPerformanceBudget.ReferenceWarmupIterations,
                GeneratedTerrainPerformanceBudget.ReferenceMeasuredIterations, samples);
        }

        public static GeneratedTerrainPerformanceReport RebuildReport(
            GeneratedTerrainPerformanceReport source,
            IEnumerable<GeneratedTerrainPerformanceSample> samples,
            int? warmupIterations = null) => new GeneratedTerrainPerformanceReport(source.Budget,
                warmupIterations ?? source.WarmupIterations, source.MeasuredIterations, samples);

        private static Observation ObservePlacement()
        {
            var chain = ReferenceChain();
            var result = GeneratedCellPlacementPlanner.Plan(new GeneratedCellPlacementRequest(
                SourceSector.ToRuntimeCoordinate(), chain.Geometry, chain.GeometryDigest,
                Map16ExitAuditDigest, chain.Slices, chain.Slots, chain.Packet, chain.Registry));
            Require(result.Success, Describe(result.Failures));
            var plan = result.Plan;
            return Observation.Create(plan.PlacedLayerReferenceCount, plan.OutputDigest,
                Metric("sector_cells", plan.PlacedCellCount),
                Metric("layer_references", plan.PlacedLayerReferenceCount),
                Metric("sector_coordinates", plan.UniqueSectorCoordinateCount),
                Metric("world_coordinates", plan.Records.Select(value => value.Coordinate.WorldRowMajorIndex)
                    .Distinct().Count()),
                Metric("missing_coordinates", plan.MissingSectorCoordinateCount),
                Metric("duplicate_coordinates", plan.DuplicateSectorCoordinateCount),
                Metric("out_of_bounds_coordinates", plan.OutOfBoundsCoordinateCount));
        }

        private static Observation ObserveLayerBake()
        {
            var result = GeneratedTilemapLayerBaker.Bake(new GeneratedTilemapBakeRequest(Placement()));
            Require(result.Success, Describe(result.Failures));
            var plan = result.Plan;
            return Observation.Create(plan.TotalLayerRecordCount, plan.OutputDigest,
                Metric("logical_layers", plan.LayerCount),
                Metric("layer_records", plan.TotalLayerRecordCount),
                Metric("gap_count", plan.MissingLayerCellCount),
                Metric("overlap_count", plan.DuplicateLayerCellCount),
                Metric("stale_asset_count", 0),
                Metric("tilemap_component_writes", plan.TilemapComponentWriteCount));
        }

        private static Observation ObserveSeamValidation()
        {
            var plan = Bake();
            var report = GeneratedTilemapSeamValidator.BuildReport(
                plan.LayerBuffers.SelectMany(value => value.Records), ReferenceChain().Geometry);
            var count = report.MicroPatternSeamPairCount + report.MicroChunkSeamPairCount +
                        report.MicroPatternOnlySeamPairCount;
            return Observation.Create(count, report.OutputDigest,
                Metric("seam_4x4", report.MicroPatternSeamPairCount),
                Metric("seam_12x8", report.MicroChunkSeamPairCount),
                Metric("seam_4x4_only", report.MicroPatternOnlySeamPairCount),
                Metric("unapproved_pairs", report.UnapprovedPairCount),
                Metric("missing_neighbors", report.MissingNeighborPairCount),
                Metric("out_of_bounds_neighbors", report.OutOfBoundsNeighborPairCount));
        }

        private static Observation ObserveColliderCache()
        {
            var rebuilt = GeneratedColliderRebuildPlanner.Build(
                new GeneratedColliderRebuildRequest(Bake()));
            Require(rebuilt.Success, Describe(rebuilt.Failures));
            var key = GeneratedColliderCacheKey.Create(rebuilt.Plan, GeneratorVersion, DataVersion);
            var entry = new GeneratedColliderCacheEntry(key, rebuilt.Plan);
            var cold = GeneratedColliderCacheSnapshot.Empty.Lookup(key);
            var warmSource = cold.Snapshot.Store(entry);
            var warm = warmSource.Lookup(key);
            var invalidated = warm.Snapshot.Invalidate(key);
            var other = Entry(new GeneratedSectorCoordinate(4, 4), 0);
            var evicted = GeneratedColliderCacheSnapshot.Empty.Store(entry).Store(other)
                .EvictToCapacity(1);
            return Observation.Create(4, Hash(rebuilt.Plan.OutputDigest, invalidated.Digest, evicted.Digest),
                Metric("cold_misses", cold.Snapshot.MissCount),
                Metric("warm_hits", warm.Snapshot.HitCount),
                Metric("invalidates", invalidated.InvalidatedCount),
                Metric("evicts", evicted.EvictedCount),
                Metric("rebuild_commands", rebuilt.Plan.AdapterCommandCount),
                Metric("collider_creations", rebuilt.Plan.ColliderCreationCount),
                Metric("physics_queries", rebuilt.Plan.PhysicsQueryCount));
        }

        private static Observation ObserveStreamWindow()
        {
            var center = PlanWindow(new GeneratedSectorCoordinate(6, 6));
            var edge = PlanWindow(new GeneratedSectorCoordinate(0, 6));
            var corner = PlanWindow(new GeneratedSectorCoordinate(0, 0));
            Require(center.Success && edge.Success && corner.Success,
                Describe(center.Failures.Concat(edge.Failures).Concat(corner.Failures)));
            return Observation.Create(center.Window.PreloadCount + edge.Window.PreloadCount +
                                      corner.Window.PreloadCount,
                Hash(center.Window.Digest, edge.Window.Digest, corner.Window.Digest),
                Metric("center_preload", center.Window.PreloadCount),
                Metric("center_active", center.Window.ActiveCount),
                Metric("edge_preload", edge.Window.PreloadCount),
                Metric("edge_active", edge.Window.ActiveCount),
                Metric("corner_preload", corner.Window.PreloadCount),
                Metric("corner_active", corner.Window.ActiveCount),
                Metric("active_subset_preload", center.Window.ActiveIsSubsetOfPreload &&
                    edge.Window.ActiveIsSubsetOfPreload && corner.Window.ActiveIsSubsetOfPreload ? 1 : 0),
                Metric("window_duplicates", center.Window.DuplicatePreloadMemberCount +
                    center.Window.DuplicateActiveMemberCount + edge.Window.DuplicatePreloadMemberCount +
                    edge.Window.DuplicateActiveMemberCount + corner.Window.DuplicatePreloadMemberCount +
                    corner.Window.DuplicateActiveMemberCount));
        }

        private static Observation ObserveTransition()
        {
            var previous = PlanWindow(new GeneratedSectorCoordinate(6, 6));
            Require(previous.Success, Describe(previous.Failures));
            var next = PlanWindow(new GeneratedSectorCoordinate(7, 6),
                previous.TransitionPlan.FinalHandles, previous.Window);
            Require(next.Success, Describe(next.Failures));
            var duplicateHandles = next.TransitionPlan.Records.GroupBy(value => value.Coordinate.ToString())
                .Count(group => group.Count() != 1);
            return Observation.Create(next.Diff.Changes.Count,
                Hash(next.Diff.Digest, next.TransitionPlan.Digest),
                Metric("shifted_window_diff", next.Diff.Changes.Count),
                Metric("transition_batch", next.TransitionPlan.RecordCount),
                Metric("duplicate_handle_changes", duplicateHandles),
                Metric("failed_transitions", next.TransitionPlan.FailedRecordCount),
                Metric("scene_activations", next.TransitionPlan.SceneActivationExecutionCount));
        }

        private static Observation ObserveModificationStorage()
        {
            var fixture = CreateModified();
            var store = Store();
            var compact = store.Compact(fixture.Storage);
            Require(compact.Success, Describe(compact.Failures));
            var apply = store.BuildApplyPlan(compact.Storage, SourceSector, ActiveHandle());
            Require(apply.Success, Describe(apply.Failures));
            return Observation.Create(fixture.Storage.TotalRecordCount,
                Hash(compact.Storage.Digest, apply.ApplyPlan.Digest),
                Metric("modified_sectors", fixture.Storage.ModifiedSectorCount),
                Metric("modification_records", fixture.Storage.TotalRecordCount),
                Metric("dirty_revision", fixture.Snapshot.DirtyRevision),
                Metric("compact_idempotent", compact.WasIdempotent ? 1 : 0),
                Metric("apply_commands", apply.ApplyPlan.CommandCount),
                Metric("in_place_mutations", apply.ApplyPlan.InPlaceInputMutationCount));
        }

        private static Observation ObserveSaveManifest()
        {
            var built = GeneratedSaveManifestService.Build(BaselineModification().Storage,
                SeedIdentity, GeneratorVersion, DataVersion, Placement().OutputDigest,
                WindowHandleDigest());
            Require(built.Success, Describe(built.Failures));
            var parsed = GeneratedSaveManifestSerializer.Parse(built.Payload);
            Require(parsed.Success, Describe(parsed.Failures));
            Require(string.Equals(built.Manifest.Digest, ExpectedManifestDigest,
                StringComparison.Ordinal), "MAP17_06 manifest digest changed.");
            Require(string.Equals(built.Payload.Digest, ExpectedPayloadDigest,
                StringComparison.Ordinal), "MAP17_06 payload digest changed.");
            return Observation.Create(built.Payload.Bytes.Count,
                Hash(built.Manifest.Digest, built.Payload.Digest),
                Metric("payload_bytes", built.Payload.Bytes.Count),
                Metric("modified_manifest_entries", built.Manifest.ModifiedSectorCount),
                Metric("unmodified_manifest_entries", 0),
                Metric("unmodified_sectors_omitted", built.Manifest.UnmodifiedSectorCount),
                Metric("serialized_records", built.Manifest.ModificationRecordCount),
                Metric("full_sector_serialization", built.Manifest.FullTileDataEntryCount),
                Metric("unity_object_ids", built.Manifest.UnityObjectIdCount),
                Metric("file_paths", built.Manifest.FilePathCount),
                Metric("timestamps", built.Manifest.TimestampCount),
                Metric("frame_counts", built.Manifest.FrameCountValueCount),
                Metric("population_spawn_ids", built.Manifest.PopulationStableSpawnIdCount));
        }

        private static Observation ObserveRegenerationApply()
        {
            var result = GeneratedSaveManifestService.PlanRegenerationApply(Request());
            Require(result.Success, Describe(result.Failures));
            Require(string.Equals(result.Plan.Digest, ExpectedRegenerationApplyDigest,
                StringComparison.Ordinal), "MAP17_06 regeneration apply digest changed.");
            return Observation.Create(result.Plan.CommandCount, result.Plan.Digest,
                Metric("modified_sector_plans", 1),
                Metric("regen_commands", result.Plan.CommandCount),
                Metric("destroy_tile", result.Plan.DestroyTileCommandCount),
                Metric("replace_tile", result.Plan.ReplaceTileCommandCount),
                Metric("collect_pickup", result.Plan.CollectPickupCommandCount),
                Metric("change_device_state", result.Plan.ChangeDeviceStateCommandCount),
                Metric("consume_slot", result.Plan.ConsumeSlotCommandCount),
                Metric("in_place_mutations", result.Plan.InputInPlaceMutationCount));
        }

        private static Observation ObserveHashMismatch()
        {
            var stale = Hash("MAP17_07_STALE_DIGEST");
            var probes = new[]
            {
                GeneratedSaveManifestService.PlanRegenerationApply(Request(geometry: stale)),
                GeneratedSaveManifestService.PlanRegenerationApply(Request(placementDigest: stale)),
                GeneratedSaveManifestService.PlanRegenerationApply(Request(bakeDigest: stale)),
                GeneratedSaveManifestService.PlanRegenerationApply(Request(cacheDigest: stale)),
                GeneratedSaveManifestService.PlanRegenerationApply(Request(windowHandleDigest: stale)),
                GeneratedSaveManifestService.PlanRegenerationApply(Request(storageDigest: stale)),
            };
            Require(probes.All(value => !value.Success && value.Plan == null &&
                value.PartialApplyMutationCount == 0), "Hash mismatch was not atomic.");
            return Observation.Create(probes.Length,
                Hash(probes.SelectMany(value => value.Failures).Select(value => value.StableToken).ToArray()),
                Metric("hash_mismatch_failures", probes.Count(value => !value.Success)),
                Metric("retry_loops", 0),
                Metric("partial_apply_mutations", probes.Sum(value => value.PartialApplyMutationCount)));
        }

        private static KeyValuePair<string, Func<Observation>> Operation(
            string name, Func<Observation> action) =>
            new KeyValuePair<string, Func<Observation>>(name, action);

        private static GeneratedTerrainPerformanceMetric Metric(string name, int value) =>
            new GeneratedTerrainPerformanceMetric(name, value);

        private static string Hash(params string[] values) =>
            BakingCanonicalDigest.HashCanonicalLines(values ?? Array.Empty<string>());

        private static GeneratedSaveManifestResult Manifest()
        {
            if (manifest != null) return manifest;
            manifest = GeneratedSaveManifestService.Build(BaselineModification().Storage,
                SeedIdentity, GeneratorVersion, DataVersion, Placement().OutputDigest,
                WindowHandleDigest());
            Require(manifest.Success, Describe(manifest.Failures));
            return manifest;
        }

        private static string WindowHandleDigest() =>
            GeneratedSaveManifestService.ComputeWindowHandleDigest(Authority().BaseDigests);

        private static GeneratedSectorRegenerationRequest Request(
            string geometry = null,
            string placementDigest = null,
            string bakeDigest = null,
            string cacheDigest = null,
            string windowHandleDigest = null,
            string storageDigest = null)
        {
            var value = Manifest().Manifest;
            return new GeneratedSectorRegenerationRequest(value, SourceSector, Authority(),
                value.Header.SeedIdentity, value.Header.Version.GeneratorVersion,
                value.Header.Version.DataVersion, geometry ?? value.Header.GeometryDigest,
                placementDigest ?? value.Header.PlacementDigest,
                bakeDigest ?? value.Header.BakeDigest, cacheDigest ?? value.Header.CacheDigest,
                windowHandleDigest ?? value.Header.WindowHandleDigest,
                storageDigest ?? value.Header.StorageDigest);
        }

        private static GeneratedSectorModificationStore Store() =>
            new GeneratedSectorModificationStore(Authority());

        private static GeneratedSectorModificationAuthority Authority()
        {
            if (authority != null) return authority;
            var source = Streaming();
            var records = Bake().LayerBuffers.SelectMany(value => value.Records).ToArray();
            authority = new GeneratedSectorModificationAuthority(
                ReferenceChain().Geometry, SourceSector, records, source.Window,
                source.Diff, source.TransitionPlan, ReferenceChain().GeometryDigest,
                Bake().OutputDigest, Cache().Digest, SeedIdentity, GeneratorVersion, DataVersion);
            Require(authority.IsValid, "Modification authority is invalid.");
            return authority;
        }

        private static ModificationFixture BaselineModification() =>
            baselineModification ?? (baselineModification = CreateModified());

        private static ModificationFixture CreateModified()
        {
            var store = Store();
            var storage = store.CreateEmpty();
            var records = new List<GeneratedSectorModificationRecord>();
            var definitions = new[]
            {
                new Definition(Target(GeneratedTilemapLayerId.Terrain, 0),
                    GeneratedSectorModificationKind.DestroyTile, DestroyPayload(0)),
                new Definition(Target(GeneratedTilemapLayerId.Terrain, 1),
                    GeneratedSectorModificationKind.ReplaceTile,
                    GeneratedSectorModificationPayload.Replace(Tile(GeneratedTilemapLayerId.Terrain, 1),
                        Source(GeneratedTilemapLayerId.Terrain, 1), "REPLACED_TILE", "PLAYER_REPAIR")),
                new Definition(Target(GeneratedTilemapLayerId.Marker, 2, "PICKUP_SLOT_002"),
                    GeneratedSectorModificationKind.CollectPickup,
                    GeneratedSectorModificationPayload.PickupCollected()),
                new Definition(Target(GeneratedTilemapLayerId.Marker, 3, "DEVICE_SLOT_003"),
                    GeneratedSectorModificationKind.ChangeDeviceState,
                    GeneratedSectorModificationPayload.DeviceState("door", "open")),
                new Definition(Target(GeneratedTilemapLayerId.Marker, 4, "CONSUME_SLOT_004"),
                    GeneratedSectorModificationKind.ConsumeSlot,
                    GeneratedSectorModificationPayload.SlotConsumed(
                        Source(GeneratedTilemapLayerId.Marker, 4))),
            };
            foreach (var definition in definitions)
            {
                var record = store.Author(storage, definition.Target, definition.Kind, definition.Payload);
                var result = store.Add(storage, record);
                Require(result.Success, Describe(result.Failures));
                storage = result.Storage;
                records.Add(record);
            }
            return new ModificationFixture(storage, storage.Find(SourceSector), records);
        }

        private static GeneratedSectorModificationTarget Target(
            GeneratedTilemapLayerId layer, int localIndex, string slotReference = null)
        {
            var record = Bake().LayerBuffers.Single(value => value.LayerId == layer)
                .Records.Single(value => value.SectorLocalIndex == localIndex);
            return new GeneratedSectorModificationTarget(SourceSector,
                new GeneratedSectorLocalCellIndex(localIndex), (int)layer,
                record.SourceLayerStableToken, slotReference);
        }

        private static GeneratedSectorModificationPayload DestroyPayload(int localIndex) =>
            GeneratedSectorModificationPayload.Destroy(Tile(GeneratedTilemapLayerId.Terrain, localIndex),
                Source(GeneratedTilemapLayerId.Terrain, localIndex));

        private static string Tile(GeneratedTilemapLayerId layer, int index)
        {
            var value = Bake().LayerBuffers.Single(buffer => buffer.LayerId == layer)
                .Records.Single(record => record.SectorLocalIndex == index).TileCode;
            return value == null ? "EMPTY" : value.Value;
        }

        private static string Source(GeneratedTilemapLayerId layer, int index) =>
            Bake().LayerBuffers.Single(buffer => buffer.LayerId == layer)
                .Records.Single(record => record.SectorLocalIndex == index).SourceLayerStableToken;

        private static GeneratedSectorRuntimeHandle ActiveHandle()
        {
            var entry = Entry(SourceSector, 0);
            var unloaded = GeneratedSectorRuntimeHandleLifecycle.CreateUnloaded(entry, SeedIdentity);
            var preloaded = GeneratedSectorRuntimeHandleLifecycle.Transition(
                new GeneratedSectorRuntimeTransitionRequest(unloaded,
                    GeneratedSectorRuntimeState.Preloaded, unloaded.Sector, Cache(), entry));
            Require(preloaded.Success, Describe(preloaded.Failures));
            var active = GeneratedSectorRuntimeHandleLifecycle.Transition(
                new GeneratedSectorRuntimeTransitionRequest(preloaded.Handle,
                    GeneratedSectorRuntimeState.Active, preloaded.Handle.Sector, preloaded.CacheSnapshot));
            Require(active.Success, Describe(active.Failures));
            return active.Handle;
        }

        private static GeneratedSectorStreamingResult Streaming()
        {
            if (streaming != null) return streaming;
            streaming = PlanWindow(new GeneratedSectorCoordinate(6, 6));
            Require(streaming.Success, Describe(streaming.Failures));
            return streaming;
        }

        private static GeneratedSectorStreamingResult PlanWindow(
            GeneratedSectorCoordinate center,
            IEnumerable<GeneratedSectorRuntimeHandle> sourceHandles = null,
            GeneratedSectorStreamingWindow previous = null) => GeneratedSectorWindowPlanner.Plan(
                new GeneratedSectorWindowRequest(center, 0.5d, 0.5d,
                    GeneratedSectorDirectionHint.None, GeneratedSectorPreactivationPolicy.Default,
                    sourceHandles ?? Handles(), Entries(), previous));

        private static GeneratedSectorRuntimeHandle[] Handles()
        {
            if (handles != null) return handles;
            handles = Entries().Select(value =>
                GeneratedSectorRuntimeHandleLifecycle.CreateUnloaded(value, SeedIdentity)).ToArray();
            return handles;
        }

        private static GeneratedColliderCacheSnapshot Cache()
        {
            if (cache != null) return cache;
            cache = Entries().Aggregate(GeneratedColliderCacheSnapshot.Empty,
                (snapshot, entry) => snapshot.Store(entry));
            return cache;
        }

        private static GeneratedColliderCacheEntry[] Entries()
        {
            if (entries != null) return entries;
            entries = Enumerable.Range(0, GeneratedTerrainGeometrySnapshot.CanonicalWorldSectorCount)
                .Select(index => new GeneratedSectorCoordinate(
                    index % GeneratedTerrainGeometrySnapshot.CanonicalWorldSectorColumns,
                    index / GeneratedTerrainGeometrySnapshot.CanonicalWorldSectorColumns))
                .Select(coordinate => Entry(coordinate, 0)).ToArray();
            return entries;
        }

        private static GeneratedColliderCacheEntry Entry(
            GeneratedSectorCoordinate coordinate, int revision)
        {
            var plan = Rebuild(revision);
            var source = GeneratedColliderCacheKey.Create(plan, GeneratorVersion, DataVersion);
            var key = new GeneratedColliderCacheKey(source.GeometryDigest, source.BakeDigest,
                source.SeamDigest, source.RegistryDigest, coordinate.ToRuntimeCoordinate(),
                source.GeneratorVersion, source.DataVersion, revision, source.CollisionPolicyVersion);
            return new GeneratedColliderCacheEntry(key, plan);
        }

        private static GeneratedColliderRebuildPlan Rebuild(int revision = 0)
        {
            GeneratedColliderRebuildPlan cached;
            if (Rebuilds.TryGetValue(revision, out cached)) return cached;
            var result = GeneratedColliderRebuildPlanner.Build(new GeneratedColliderRebuildRequest(
                Bake(), revision, revision == 0 ? "INITIAL_BAKE" : "PLAYER_MUTATION"));
            Require(result.Success, Describe(result.Failures));
            Rebuilds[revision] = result.Plan;
            return result.Plan;
        }

        private static GeneratedTilemapBakePlan Bake()
        {
            if (bake != null) return bake;
            var result = GeneratedTilemapLayerBaker.Bake(new GeneratedTilemapBakeRequest(Placement()));
            Require(result.Success, Describe(result.Failures));
            bake = result.Plan;
            return bake;
        }

        private static GeneratedCellPlacementPlan Placement()
        {
            if (placement != null) return placement;
            var chain = ReferenceChain();
            var result = GeneratedCellPlacementPlanner.Plan(new GeneratedCellPlacementRequest(
                SourceSector.ToRuntimeCoordinate(), chain.Geometry, chain.GeometryDigest,
                Map16ExitAuditDigest, chain.Slices, chain.Slots, chain.Packet, chain.Registry));
            Require(result.Success, Describe(result.Failures));
            placement = result.Plan;
            return placement;
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
            Require(canvas.Success, Describe(canvas.Failures));
            var density = SectorCanvasProtectionDensityValidator.Validate(canvas.Plan);
            Require(density.Success, Describe(density.Failures));
            var route = SectorFinalRouteRecoveryValidator.Validate(new FinalRouteRecoveryRequest(
                canvas.Plan, density.Report, baselineRouteRequest.Anchors,
                baselineRouteRequest.DeclaredEdges, baselineRouteRequest.PublicationLabel));
            Require(route.Success, Describe(route.Failures));
            var partition = SectorPatternChunkPartitioner.Partition(
                canvas.Plan, density.Report, route.Report);
            Require(partition.Success, Describe(partition.Failures));
            var slices = GeneratedMicroChunkSliceBuilder.Build(
                canvas.Plan, density.Report, route.Report, partition.Partition);
            Require(slices.Success, Describe(slices.Failures));
            var slots = GeneratedMicroChunkMarkerSlotProjector.Project(slices.SliceSet);
            Require(slots.Success, Describe(slots.Failures));
            var packet = GeneratedTerrainCsvExporter.Build(slices.SliceSet, slots.SlotSet);
            Require(packet.Success, Describe(packet.Failures));
            GeneratedTerrainGeometrySnapshot geometry;
            IReadOnlyList<string> geometryFailures;
            Require(GeneratedTerrainGeometrySnapshot.TryCreate(out geometry, out geometryFailures),
                Describe(geometryFailures));
            var registry = GeneratedTerrainAssetRegistrySnapshot.CreateReference(
                slices.SliceSet, slots.SlotSet);
            reference = new Map17Chain(geometry, slices.SliceSet, slots.SlotSet,
                packet.Packet, registry);
            return reference;
        }

        private static FinalCanvasLayerClaim Claim(
            string claimId, int x, int y, FinalCanvasLayerKind layer,
            FinalCanvasCellKind cellKind, FinalCanvasSourceOwner owner,
            FinalCanvasClaimPriority priority, string provenanceId) => new FinalCanvasLayerClaim(
                claimId, new FinalCanvasCellCoordinate(x, y), layer, cellKind, owner, priority,
                FinalCanvasProtectionKind.None, false, provenanceId,
                GeneratedTerrainAssetRegistrySnapshot.ReferencePublicationLabel);

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message ?? "MAP17_07 fixture failure.");
        }

        private static string Describe(System.Collections.IEnumerable values)
        {
            if (values == null) return "NULL";
            var output = new List<string>();
            foreach (var value in values) output.Add(value == null ? "NULL" : value.ToString());
            return string.Join(";", output);
        }

        private sealed class Observation
        {
            private static readonly string[] StructuralZeroKeys =
            {
                "full_sector_serialization", "unmodified_manifest_entries", "unity_object_ids",
                "file_paths", "timestamps", "frame_counts", "population_spawn_ids",
                "full_generator_executions", "retry_loops", "scene_mutations",
                "prefab_mutations", "tilemap_mutations",
            };

            private Observation(int operationCount, string structuralDigest,
                IReadOnlyList<GeneratedTerrainPerformanceMetric> metrics)
            {
                OperationCount = operationCount;
                StructuralDigest = structuralDigest;
                Metrics = metrics;
            }

            public int OperationCount { get; }
            public string StructuralDigest { get; }
            public IReadOnlyList<GeneratedTerrainPerformanceMetric> Metrics { get; }

            public static Observation Create(int operationCount, string structuralDigest,
                params GeneratedTerrainPerformanceMetric[] sourceMetrics)
            {
                var metrics = StructuralZeroKeys.ToDictionary(value => value, value => 0,
                    StringComparer.Ordinal);
                foreach (var metric in sourceMetrics ?? Array.Empty<GeneratedTerrainPerformanceMetric>())
                    if (metric != null) metrics[metric.Name] = metric.Value;
                return new Observation(operationCount, structuralDigest,
                    metrics.Select(value => Metric(value.Key, value.Value)).OrderBy(value => value).ToArray());
            }
        }

        private sealed class Definition
        {
            public Definition(GeneratedSectorModificationTarget target,
                GeneratedSectorModificationKind kind, GeneratedSectorModificationPayload payload)
            {
                Target = target;
                Kind = kind;
                Payload = payload;
            }

            public GeneratedSectorModificationTarget Target { get; }
            public GeneratedSectorModificationKind Kind { get; }
            public GeneratedSectorModificationPayload Payload { get; }
        }

        private sealed class ModificationFixture
        {
            public ModificationFixture(GeneratedSectorModificationStorage storage,
                GeneratedModifiedSectorSnapshot snapshot,
                IReadOnlyList<GeneratedSectorModificationRecord> records)
            {
                Storage = storage;
                Snapshot = snapshot;
                Records = records;
            }

            public GeneratedSectorModificationStorage Storage { get; }
            public GeneratedModifiedSectorSnapshot Snapshot { get; }
            public IReadOnlyList<GeneratedSectorModificationRecord> Records { get; }
        }

        private sealed class Map17Chain
        {
            public Map17Chain(GeneratedTerrainGeometrySnapshot geometry,
                GeneratedMicroChunkSliceSet slices, GeneratedMicroChunkMarkerSlotSet slots,
                GeneratedTerrainExportPacket packet, GeneratedTerrainAssetRegistrySnapshot registry)
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
