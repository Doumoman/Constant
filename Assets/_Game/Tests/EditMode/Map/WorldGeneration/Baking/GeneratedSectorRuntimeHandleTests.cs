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
    [Category("MAP17_03")]
    public sealed class GeneratedSectorRuntimeHandleTests
    {
        private const string Map16ExitAuditDigest =
            "78d3046d62608494fb1306ff4e57a0b2d4b36eafc3a5e7e19cb8f399c3ca29f0";
        private const string ExpectedBakeDigest =
            "139465f70d40e6b9a3fdd4bb55696c38e89d1856912f4bec2644edb4c6b47602";
        private const string ExpectedSeamDigest =
            "d1a1febd5c9c10481817e5e6c027071fe2890bf2ea79c34252c2a7caaedc7fda";
        private const string GeneratorVersion = "MAP17_REFERENCE_GENERATOR_V1";
        private const string DataVersion = "MAP17_REFERENCE_DATA_V1";
        private const string SeedIdentity = "MAP17_REFERENCE_SEED";
        private static Map17Chain reference;
        private static GeneratedCellPlacementPlan placement;
        private static GeneratedTilemapBakePlan bake;
        private static readonly Dictionary<int, GeneratedColliderRebuildPlan> Rebuilds =
            new Dictionary<int, GeneratedColliderRebuildPlan>();

        [Test]
        public void ColliderMasksAreDerivedFromLogicalBakeLayersWithoutUnityPhysics()
        {
            var plan = Rebuild();
            Assert.That(plan.SourceBakePlan.OutputDigest, Is.EqualTo(ExpectedBakeDigest));
            Assert.That(plan.SourceBakePlan.SeamReport.OutputDigest, Is.EqualTo(ExpectedSeamDigest));
            Assert.That(plan.SourceLayerCount, Is.EqualTo(7));
            Assert.That(plan.SourceLayerRecordCount, Is.EqualTo(10752));
            Assert.That(plan.SourceSectorCellCoverageCount, Is.EqualTo(1536));
            Assert.That(plan.SourceSeamPairCount, Is.EqualTo(928));
            Assert.That(plan.SourceSocketReferenceCount, Is.EqualTo(64));
            Assert.That(plan.SourceMarkerSlotCount, Is.EqualTo(24));
            Assert.That(plan.MaskKindCount, Is.EqualTo(5));
            Assert.That(plan.Masks.Select(value => value.Kind), Is.EqualTo(
                Enum.GetValues(typeof(GeneratedCollisionMaskKind))));
            Assert.That(plan.Masks.All(value => value.CellCount == 1536), Is.True);
            Assert.That(plan.DuplicateMaskCellCount, Is.Zero);
            Assert.That(plan.OutOfBoundsMaskCellCount, Is.Zero);
            Assert.That(plan.PhysicsQueryCount + plan.PhysicsSimulationCount +
                plan.ColliderCreationCount, Is.Zero);
            TestContext.WriteLine("MAP17_03_MASK_EVIDENCE layers=" + plan.SourceLayerCount +
                "/7 records=" + plan.SourceLayerRecordCount + "/10752 cells=" +
                plan.SourceSectorCellCoverageCount + "/1536 seams=" + plan.SourceSeamPairCount +
                "/928 sockets=" + plan.SourceSocketReferenceCount + "/64 slots=" +
                plan.SourceMarkerSlotCount + "/24 mask_kinds=" + plan.MaskKindCount +
                " solid=" + plan.SolidMaskCellCount + " platform=" + plan.PlatformMaskCellCount +
                " hazard=" + plan.HazardMaskCellCount + " protection=" +
                plan.ProtectionMaskCellCount + " debug=" + plan.DebugNonCollidingCellCount +
                " ignored_records=" + plan.NonCollidingRecordCount + " duplicate_oob=0/0");
        }

        [Test]
        public void ColliderSpansExactlyCoverSourceMasksAndStayInsideSectorBounds()
        {
            var plan = Rebuild();
            Assert.That(plan.SpanCellsExactlyMatchMasks, Is.True);
            Assert.That(plan.SpanOutOfBoundsCellCount, Is.Zero);
            Assert.That(plan.Spans, Is.Ordered);
            foreach (var mask in plan.Masks)
                Assert.That(plan.Spans.Where(value => value.MaskKind == mask.Kind)
                    .SelectMany(value => value.CellIndices).OrderBy(value => value),
                    Is.EqualTo(mask.OccupiedIndices));
            TestContext.WriteLine("MAP17_03_SPAN_EVIDENCE spans=" + plan.SpanCount +
                " span_cells=" + plan.SpanCellCount + " exact=YES out_of_bounds=0");
        }

        [Test]
        public void ColliderRebuildPlanPublishesDeterministicAdapterCommandsWithoutExecutingThem()
        {
            var plan = Rebuild();
            Assert.That(plan.AdapterCommands, Is.Ordered);
            Assert.That(plan.AdapterCommands.Select(value => value.Ordinal),
                Is.EqualTo(Enumerable.Range(0, plan.AdapterCommandCount)));
            Assert.That(plan.AdapterCommands.All(value => value.Span.MaskKind !=
                GeneratedCollisionMaskKind.NonCollidingDebug), Is.True);
            Assert.That(plan.ExecutedAdapterCommandCount, Is.Zero);
            Assert.That(GeneratedColliderRebuildDigest.IsLowerHexSha256(plan.OutputDigest), Is.True);
            TestContext.WriteLine("MAP17_03_COMMAND_EVIDENCE planned=" +
                plan.AdapterCommandCount + " executed=0 rebuild_digest=" + plan.OutputDigest);
        }

        [Test]
        public void ColliderCacheKeyChangesForBakeSeamMutationAndPolicyDigestChanges()
        {
            var key = Entry().Key;
            var changedBake = CopyKey(key, bakeDigest: Hash("CHANGED_BAKE"));
            var changedSeam = CopyKey(key, seamDigest: Hash("CHANGED_SEAM"));
            var changedRevision = CopyKey(key, mutationRevision: key.MutationRevision + 1);
            var changedPolicy = CopyKey(key, collisionPolicyVersion: "CHANGED_POLICY");
            Assert.That(key.IsValid, Is.True);
            Assert.That(new[] { changedBake, changedSeam, changedRevision, changedPolicy }
                .Select(value => value.Digest).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(4));
            Assert.That(new[] { changedBake, changedSeam, changedRevision, changedPolicy }
                .All(value => value.Digest != key.Digest), Is.True);
            Assert.That(GeneratedColliderRebuildDigest.IsLowerHexSha256(key.Digest), Is.True);
            TestContext.WriteLine("MAP17_03_CACHE_KEY_EVIDENCE lower_hex=YES digest=" + key.Digest +
                " bake_seam_revision_policy_sensitivity=1/1/1/1");
        }

        [Test]
        public void ColliderCacheSnapshotReportsHitMissEvictAndInvalidateDeterministically()
        {
            var first = Entry(0);
            var second = Entry(1);
            var snapshot = GeneratedColliderCacheSnapshot.Empty.Store(first);
            var hit = snapshot.Lookup(first.Key);
            Assert.That(hit.Hit, Is.True);
            var miss = hit.Snapshot.Lookup(second.Key);
            Assert.That(miss.Hit, Is.False);
            var invalidated = miss.Snapshot.Store(second).Invalidate(first.Key);
            var evicted = invalidated.EvictToCapacity(0);
            Assert.That(evicted.HitCount, Is.EqualTo(1));
            Assert.That(evicted.MissCount, Is.EqualTo(1));
            Assert.That(evicted.InvalidatedCount, Is.EqualTo(1));
            Assert.That(evicted.EvictedCount, Is.EqualTo(1));
            Assert.That(evicted.EntryCount, Is.Zero);
            var forward = GeneratedColliderCacheSnapshot.Empty.Store(first).Store(second);
            var reverse = GeneratedColliderCacheSnapshot.Empty.Store(second).Store(first);
            Assert.That(reverse.Digest, Is.EqualTo(forward.Digest));
            TestContext.WriteLine("MAP17_03_CACHE_EVIDENCE hit_miss_invalidate_evict=1/1/1/1" +
                " insertion_order_mismatches=0 snapshot_digest=" + forward.Digest);
        }

        [Test]
        public void RuntimeHandleLifecycleAllowsOnlyDocumentedStateTransitions()
        {
            var entry0 = Entry(0);
            var entry1 = Entry(1);
            var snapshot = GeneratedColliderCacheSnapshot.Empty.Store(entry0).Store(entry1);
            var unloaded = GeneratedSectorRuntimeHandleLifecycle.CreateUnloaded(entry0, SeedIdentity);
            var preloaded = Move(unloaded, GeneratedSectorRuntimeState.Preloaded, snapshot, entry0);
            var active = Move(preloaded.Handle, GeneratedSectorRuntimeState.Active,
                preloaded.CacheSnapshot);
            var backToPreloaded = Move(active.Handle, GeneratedSectorRuntimeState.Preloaded,
                active.CacheSnapshot);
            var backToUnloaded = Move(backToPreloaded.Handle, GeneratedSectorRuntimeState.Unloaded,
                backToPreloaded.CacheSnapshot);
            Assert.That(backToUnloaded.Success, Is.True);

            var sleeping = Move(active.Handle, GeneratedSectorRuntimeState.SleepingModified,
                active.CacheSnapshot, mutationRevision: 1, dirtyReason: "PLAYER_MUTATION");
            var reactivated = Move(sleeping.Handle, GeneratedSectorRuntimeState.Active,
                sleeping.CacheSnapshot, entry1);
            var sleepAgain = Move(active.Handle, GeneratedSectorRuntimeState.SleepingModified,
                active.CacheSnapshot, mutationRevision: 1, dirtyReason: "PLAYER_MUTATION");
            var sleepToUnloaded = Move(sleepAgain.Handle, GeneratedSectorRuntimeState.Unloaded,
                sleepAgain.CacheSnapshot);
            Assert.That(reactivated.Success && sleepToUnloaded.Success, Is.True);

            var forbidden = new[]
            {
                TryMove(unloaded, GeneratedSectorRuntimeState.Active, snapshot),
                TryMove(preloaded.Handle, GeneratedSectorRuntimeState.SleepingModified, snapshot,
                    mutationRevision: 1, dirtyReason: "PLAYER_MUTATION"),
                TryMove(sleeping.Handle, GeneratedSectorRuntimeState.Preloaded, snapshot),
                TryMove(active.Handle, GeneratedSectorRuntimeState.Active, snapshot, entry1),
                TryMove(active.Handle, GeneratedSectorRuntimeState.Preloaded, snapshot,
                    sector: new GeneratedSectorIndexCoordinate(active.Handle.Sector.X + 1,
                        active.Handle.Sector.Y)),
            };
            Assert.That(forbidden.All(value => !value.Success && value.Handle == null), Is.True);
            Assert.That(forbidden.SelectMany(value => value.Failures).Select(value => value.Code),
                Does.Contain(GeneratedSectorRuntimeHandleFailureCode.StaleCacheKey));
            Assert.That(forbidden.SelectMany(value => value.Failures).Select(value => value.Code),
                Does.Contain(GeneratedSectorRuntimeHandleFailureCode.SectorMismatch));
            var states = Enum.GetValues(typeof(GeneratedSectorRuntimeState))
                .Cast<GeneratedSectorRuntimeState>().ToArray();
            var allowedCount = states.SelectMany(from => states.Select(to =>
                GeneratedSectorRuntimeHandleLifecycle.IsAllowed(from, to))).Count(value => value);
            Assert.That(allowedCount, Is.EqualTo(7));
            TestContext.WriteLine("MAP17_03_TRANSITION_EVIDENCE states=" +
                string.Join("/", states.Select(value => value.ToString())) +
                " allowed=7/7 forbidden=5/5 stale_cache_failures=1 sector_failures=1");
        }

        [Test]
        public void SleepingModifiedPreservesDirtyRevisionWithoutWritingSaveData()
        {
            var entry0 = Entry(0);
            var snapshot = GeneratedColliderCacheSnapshot.Empty.Store(entry0);
            var unloaded = GeneratedSectorRuntimeHandleLifecycle.CreateUnloaded(entry0, SeedIdentity);
            var preloaded = Move(unloaded, GeneratedSectorRuntimeState.Preloaded, snapshot, entry0);
            var active = Move(preloaded.Handle, GeneratedSectorRuntimeState.Active,
                preloaded.CacheSnapshot);
            var sleeping = Move(active.Handle, GeneratedSectorRuntimeState.SleepingModified,
                active.CacheSnapshot, mutationRevision: 1, dirtyReason: "PLAYER_MUTATION");
            Assert.That(sleeping.Handle.State, Is.EqualTo(GeneratedSectorRuntimeState.SleepingModified));
            Assert.That(sleeping.Handle.MutationRevision, Is.EqualTo(1));
            Assert.That(sleeping.Handle.IsDirty, Is.True);
            Assert.That(sleeping.Handle.DirtyReason, Is.EqualTo("PLAYER_MUTATION"));
            Assert.That(sleeping.Handle.RetainsRuntimeCache, Is.False);
            Assert.That(sleeping.CacheSnapshot.InvalidatedCount, Is.EqualTo(1));
            Assert.That(sleeping.Handle.DurableSaveWriteCount, Is.Zero);
            TestContext.WriteLine("MAP17_03_DIRTY_EVIDENCE revision=1 reason=PLAYER_MUTATION" +
                " cache_invalidated=1 durable_save_writes=0");
        }

        [Test]
        public void HandleAndColliderDigestsAreStableAcrossRepeatReverseCultureAndCacheOrder()
        {
            var baseline = Rebuild();
            var repeat = GeneratedColliderRebuildPlanner.Build(new GeneratedColliderRebuildRequest(
                Bake(), records: baseline.Request.SourceRecords));
            var reverse = GeneratedColliderRebuildPlanner.Build(new GeneratedColliderRebuildRequest(
                Bake(), records: baseline.Request.SourceRecords.Reverse()));
            Assert.That(repeat.Success && reverse.Success, Is.True);
            Assert.That(repeat.OutputDigest, Is.EqualTo(baseline.OutputDigest));
            Assert.That(reverse.OutputDigest, Is.EqualTo(baseline.OutputDigest));

            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var culture = GeneratedColliderRebuildPlanner.Build(new GeneratedColliderRebuildRequest(
                    Bake(), records: baseline.Request.SourceRecords.Reverse()));
                Assert.That(culture.Success, Is.True);
                Assert.That(culture.OutputDigest, Is.EqualTo(baseline.OutputDigest));
                var handle = GeneratedSectorRuntimeHandleLifecycle.CreateUnloaded(
                    new GeneratedColliderCacheEntry(GeneratedColliderCacheKey.Create(culture.Plan,
                        GeneratorVersion, DataVersion), culture.Plan), SeedIdentity);
                Assert.That(handle.Digest, Is.EqualTo(
                    GeneratedSectorRuntimeHandleLifecycle.CreateUnloaded(Entry(), SeedIdentity).Digest));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }

            var entry0 = Entry(0);
            var entry1 = Entry(1);
            var forward = GeneratedColliderCacheSnapshot.Empty.Store(entry0).Store(entry1);
            var reversed = GeneratedColliderCacheSnapshot.Empty.Store(entry1).Store(entry0);
            Assert.That(reversed.Digest, Is.EqualTo(forward.Digest));
            Assert.That(Rebuild(1).OutputDigest, Is.Not.EqualTo(baseline.OutputDigest));
            var handleDigest = GeneratedSectorRuntimeHandleLifecycle.CreateUnloaded(
                entry0, SeedIdentity).Digest;
            Assert.That(GeneratedSectorRuntimeHandleDigest.IsLowerHexSha256(handleDigest), Is.True);
            TestContext.WriteLine("MAP17_03_DIGEST_EVIDENCE repeat_reverse_culture_cache_order=0/0/0/0" +
                " mutation_sensitivity=2 collider=" + baseline.OutputDigest +
                " runtime_handle=" + handleDigest);
        }

        [Test]
        public void RuntimeHandlesDoNotCreateTilemapsCollidersRigidbodiesGameObjectsOrFiles()
        {
            var scene = SceneManager.GetActiveScene();
            var roots = scene.IsValid()
                ? scene.GetRootGameObjects().Select(value => value.GetInstanceID()).ToArray()
                : Array.Empty<int>();
            var wasDirty = scene.IsValid() && scene.isDirty;
            var plan = Rebuild();
            var handle = GeneratedSectorRuntimeHandleLifecycle.CreateUnloaded(Entry(), SeedIdentity);
            Assert.That(scene.IsValid()
                ? scene.GetRootGameObjects().Select(value => value.GetInstanceID())
                : Array.Empty<int>(), Is.EqualTo(roots));
            Assert.That(scene.IsValid() && scene.isDirty, Is.EqualTo(wasDirty));
            Assert.That(plan.TilemapComponentWriteCount + plan.ColliderCreationCount +
                plan.RigidbodyCreationCount + plan.PhysicsQueryCount + plan.PhysicsSimulationCount +
                plan.SceneMutationCount + plan.PrefabMutationCount + plan.TilemapMutationCount +
                plan.GameObjectInstantiationCount + plan.PrefabInstantiationCount +
                plan.GeneratedCsvCommitCount + plan.GeneratedAssetCommitCount +
                plan.StableSpawnIdCount + plan.RuntimeObjectSpawnCount +
                plan.ProductionSeedApprovalCount, Is.Zero);
            Assert.That(handle.TilemapComponentWriteCount + handle.ColliderCreationCount +
                handle.RigidbodyCreationCount + handle.PhysicsQueryCount +
                handle.PhysicsSimulationCount + handle.SceneMutationCount +
                handle.PrefabMutationCount + handle.TilemapMutationCount +
                handle.GameObjectInstantiationCount + handle.PrefabInstantiationCount +
                handle.GeneratedFileWriteCount + handle.RuntimeObjectSpawnCount +
                handle.DurableSaveWriteCount, Is.Zero);
            Assert.That(typeof(UnityEngine.Object).IsAssignableFrom(
                typeof(GeneratedSectorRuntimeHandle)), Is.False);
            TestContext.WriteLine("MAP17_03_MUTATION_EVIDENCE tilemap=0 collider=0 rigidbody=0" +
                " physics=0/0 scene_prefab_tilemap=0/0/0 gameobject_prefab=0/0 files=0" +
                " stable_spawn_runtime=0/0 production_seed=0");
        }

        [Test]
        public void Map17HandoffKeepsMap17_04Locked()
        {
            Assert.That(GeneratedSectorRuntimeHandleLifecycle.DownstreamOwner,
                Is.EqualTo("MAP17_04_IMPLEMENT_PRELOAD_ACTIVE_AND_PREACTIVATION"));
            Assert.That(GeneratedSectorRuntimeHandleLifecycle.OpensDownstreamTask, Is.False);
            var statusPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "MapDesign", "MCP", "06_IMPLEMENTATION_STATUS.md");
            var status = File.ReadAllText(statusPath);
            Assert.That(status, Does.Contain(
                "| MAP17_04_IMPLEMENT_PRELOAD_ACTIVE_AND_PREACTIVATION | LOCKED |"));
            Assert.That(status, Does.Not.Contain(
                "| MAP17_04_IMPLEMENT_PRELOAD_ACTIVE_AND_PREACTIVATION | CURRENT |"));
            TestContext.WriteLine("MAP17_03_DOWNSTREAM_EVIDENCE MAP17_04_started=NO locked=YES");
        }

        private static GeneratedSectorRuntimeHandleResult Move(
            GeneratedSectorRuntimeHandle handle,
            GeneratedSectorRuntimeState target,
            GeneratedColliderCacheSnapshot snapshot,
            GeneratedColliderCacheEntry entry = null,
            int? mutationRevision = null,
            string dirtyReason = null)
        {
            var result = TryMove(handle, target, snapshot, entry, mutationRevision, dirtyReason);
            Assert.That(result.Success, Is.True, Describe(result.Failures));
            return result;
        }

        private static GeneratedSectorRuntimeHandleResult TryMove(
            GeneratedSectorRuntimeHandle handle,
            GeneratedSectorRuntimeState target,
            GeneratedColliderCacheSnapshot snapshot,
            GeneratedColliderCacheEntry entry = null,
            int? mutationRevision = null,
            string dirtyReason = null,
            GeneratedSectorIndexCoordinate sector = null) =>
            GeneratedSectorRuntimeHandleLifecycle.Transition(
                new GeneratedSectorRuntimeTransitionRequest(handle, target,
                    sector ?? handle.Sector, snapshot, entry, mutationRevision, dirtyReason));

        private static GeneratedColliderCacheEntry Entry(int revision = 0)
        {
            var plan = Rebuild(revision);
            return new GeneratedColliderCacheEntry(GeneratedColliderCacheKey.Create(
                plan, GeneratorVersion, DataVersion), plan);
        }

        private static GeneratedColliderRebuildPlan Rebuild(int revision = 0)
        {
            GeneratedColliderRebuildPlan cached;
            if (Rebuilds.TryGetValue(revision, out cached)) return cached;
            var result = GeneratedColliderRebuildPlanner.Build(new GeneratedColliderRebuildRequest(
                Bake(), revision, revision == 0 ? "INITIAL_BAKE" : "PLAYER_MUTATION"));
            Assert.That(result.Success, Is.True, Describe(result.Failures));
            Rebuilds[revision] = result.Plan;
            return result.Plan;
        }

        private static GeneratedColliderCacheKey CopyKey(
            GeneratedColliderCacheKey source,
            string bakeDigest = null,
            string seamDigest = null,
            int? mutationRevision = null,
            string collisionPolicyVersion = null) => new GeneratedColliderCacheKey(
                source.GeometryDigest, bakeDigest ?? source.BakeDigest,
                seamDigest ?? source.SeamDigest, source.RegistryDigest, source.Sector,
                source.GeneratorVersion, source.DataVersion,
                mutationRevision ?? source.MutationRevision,
                collisionPolicyVersion ?? source.CollisionPolicyVersion);

        private static string Hash(string value) =>
            BakingCanonicalDigest.HashCanonicalLines(new[] { value });

        private static GeneratedTilemapBakePlan Bake()
        {
            if (bake != null) return bake;
            var result = GeneratedTilemapLayerBaker.Bake(new GeneratedTilemapBakeRequest(Placement()));
            Assert.That(result.Success, Is.True, Describe(result.Failures));
            bake = result.Plan;
            return bake;
        }

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
