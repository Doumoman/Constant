using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Baking
{
    [TestFixture]
    [Category("MAP17_04")]
    public sealed class GeneratedSectorWindowPlannerTests
    {
        private const string Map16ExitAuditDigest =
            "78d3046d62608494fb1306ff4e57a0b2d4b36eafc3a5e7e19cb8f399c3ca29f0";
        private const string ExpectedCacheKeyDigest =
            "e5804aba97511cf73c080ac325d7a428915732981944fcbe1e83c1b0b334c5ca";
        private const string ExpectedRebuildDigest =
            "2ab1b2fa4ca7f7c8e57dbf62456cc5c8f3faa43854c600e7b1c8f7a3ed02e599";
        private const string ExpectedRuntimeHandleDigest =
            "0c4ea997c35c04d9386d96e41611cffe9b5b3a9006a2b94222d5883cf8279331";
        private const string GeneratorVersion = "MAP17_REFERENCE_GENERATOR_V1";
        private const string DataVersion = "MAP17_REFERENCE_DATA_V1";
        private const string SeedIdentity = "MAP17_REFERENCE_SEED";
        private static Map17Chain reference;
        private static GeneratedCellPlacementPlan placement;
        private static GeneratedTilemapBakePlan bake;
        private static readonly Dictionary<int, GeneratedColliderRebuildPlan> Rebuilds =
            new Dictionary<int, GeneratedColliderRebuildPlan>();
        private static GeneratedColliderCacheEntry[] entries;
        private static GeneratedSectorRuntimeHandle[] handles;

        [Test]
        public void PreloadWindowPublishesSevenBySevenInBoundsSectorMembership()
        {
            var result = Plan(new GeneratedSectorCoordinate(6, 6));
            Assert.That(result.Success, Is.True, Failures(result));
            Assert.That(result.Window.PreloadCount, Is.EqualTo(49));
            Assert.That(result.Window.PreloadMembers, Is.Ordered);
            Assert.That(result.Window.PreloadMembers.All(value => value.Coordinate.IsInWorld &&
                value.Distance <= GeneratedSectorStreamingWindow.PreloadRadius), Is.True);
            Assert.That(result.Window.DuplicatePreloadMemberCount, Is.Zero);
            Assert.That(result.Window.OutOfWorldPreloadMemberCount, Is.Zero);
            Assert.That(result.Request.Handles.Count, Is.EqualTo(169));
            Assert.That(result.Request.CacheEntries.Count, Is.EqualTo(169));
            Assert.That(BaseEntry().Key.Digest, Is.EqualTo(ExpectedCacheKeyDigest));
            Assert.That(Rebuild().OutputDigest, Is.EqualTo(ExpectedRebuildDigest));
            Assert.That(GeneratedSectorRuntimeHandleLifecycle.CreateUnloaded(
                BaseEntry(), SeedIdentity).Digest, Is.EqualTo(ExpectedRuntimeHandleDigest));
            TestContext.WriteLine("MAP17_04_PRELOAD_EVIDENCE world=169/169 middle=49/49" +
                " radius=3 duplicate_oob=0/0 handles=169 cache_entries=169 key=" +
                BaseEntry().Key.Digest + " collider=" + Rebuild().OutputDigest +
                " source_handle=" + ExpectedRuntimeHandleDigest);
        }

        [Test]
        public void ActiveWindowPublishesFiveByFiveSubsetOfPreload()
        {
            var result = Plan(new GeneratedSectorCoordinate(6, 6));
            Assert.That(result.Success, Is.True, Failures(result));
            Assert.That(result.Window.ActiveCount, Is.EqualTo(25));
            Assert.That(result.Window.ActiveMembers, Is.Ordered);
            Assert.That(result.Window.ActiveMembers.All(value => value.Distance <=
                GeneratedSectorStreamingWindow.ActiveRadius), Is.True);
            Assert.That(result.Window.ActiveIsSubsetOfPreload, Is.True);
            Assert.That(result.Window.ActiveOutsidePreloadCount, Is.Zero);
            Assert.That(result.Window.DuplicateActiveMemberCount, Is.Zero);
            Assert.That(result.Window.OutOfWorldActiveMemberCount, Is.Zero);
            TestContext.WriteLine("MAP17_04_ACTIVE_EVIDENCE middle=25/25 radius=2" +
                " subset=YES active_outside_preload=0 duplicate_oob=0/0");
        }

        [Test]
        public void WorldEdgesAndCornersClampWindowsWithoutDuplicates()
        {
            var corner = Plan(new GeneratedSectorCoordinate(0, 0));
            var edge = Plan(new GeneratedSectorCoordinate(0, 6));
            var farCorner = Plan(new GeneratedSectorCoordinate(12, 12));
            Assert.That(corner.Success && edge.Success && farCorner.Success, Is.True);
            Assert.That(corner.Window.PreloadCount, Is.EqualTo(16));
            Assert.That(corner.Window.ActiveCount, Is.EqualTo(9));
            Assert.That(farCorner.Window.PreloadCount, Is.EqualTo(16));
            Assert.That(farCorner.Window.ActiveCount, Is.EqualTo(9));
            Assert.That(edge.Window.PreloadCount, Is.EqualTo(28));
            Assert.That(edge.Window.ActiveCount, Is.EqualTo(15));
            Assert.That(new[] { corner.Window, edge.Window, farCorner.Window }.All(window =>
                window.DuplicatePreloadMemberCount + window.DuplicateActiveMemberCount +
                window.OutOfWorldPreloadMemberCount + window.OutOfWorldActiveMemberCount == 0),
                Is.True);
            TestContext.WriteLine("MAP17_04_CLAMP_EVIDENCE corner_preload_active=16/9" +
                " edge_preload_active=28/15 opposite_corner=16/9 duplicates=0/0 oob=0/0");
        }

        [Test]
        public void PreactivationPolicyMarksNeighborCandidatesBeforeBoundaryCrossing()
        {
            var center = new GeneratedSectorCoordinate(6, 6);
            var result = Plan(center, progressX: 0.95d, progressY: 0.95d,
                direction: GeneratedSectorDirectionHint.Right);
            Assert.That(result.Success, Is.True, Failures(result));
            Assert.That(result.Window.PreactivationCandidateCount, Is.EqualTo(3));
            Assert.That(result.Window.PreactivationCandidates.Select(value => value.Coordinate),
                Is.EqualTo(new[]
                {
                    new GeneratedSectorCoordinate(7, 6),
                    new GeneratedSectorCoordinate(6, 7),
                    new GeneratedSectorCoordinate(7, 7),
                }.OrderBy(value => value)));
            Assert.That(result.Window.PreactivationCandidates.All(value =>
                value.IsInsideValidWindow && value.CacheKey != null &&
                value.ExpectedState == GeneratedSectorRuntimeState.Active), Is.True);
            Assert.That(result.Window.ExecutedSceneActivationCount, Is.Zero);

            var policy = GeneratedSectorPreactivationPolicy.Default;
            Assert.That(policy.Evaluate(center, 0.05d, 0.5d,
                GeneratedSectorDirectionHint.Left, false).Count, Is.EqualTo(1));
            Assert.That(policy.Evaluate(center, 0.95d, 0.5d,
                GeneratedSectorDirectionHint.Right, false).Count, Is.EqualTo(1));
            Assert.That(policy.Evaluate(center, 0.5d, 0.05d,
                GeneratedSectorDirectionHint.Down, false).Count, Is.EqualTo(1));
            Assert.That(policy.Evaluate(center, 0.5d, 0.95d,
                GeneratedSectorDirectionHint.Up, false).Count, Is.EqualTo(1));
            Assert.That(policy.Evaluate(center, 0.85d, 0.5d,
                GeneratedSectorDirectionHint.Right, false).Count, Is.Zero);
            Assert.That(policy.Evaluate(center, 0.85d, 0.5d,
                GeneratedSectorDirectionHint.Right, true).Count, Is.EqualTo(1));
            Assert.That(policy.Evaluate(new GeneratedSectorCoordinate(12, 12), 0.99d, 0.99d,
                GeneratedSectorDirectionHint.Right, false).Count, Is.Zero);
            TestContext.WriteLine("MAP17_04_PREACTIVATION_EVIDENCE thresholds=0.12/0.88" +
                " hysteresis=0.04 directions=4/4 diagonal_candidates=3/3 valid_window=3/3" +
                " world_edge_clamped=3 executed_scene_activations=0 camera_cinemachine=0/0");
        }

        [Test]
        public void WindowDiffReportsAddRemovePromoteDemotePreserveAndEvictDeterministically()
        {
            var previous = Plan(new GeneratedSectorCoordinate(6, 6));
            var next = Plan(new GeneratedSectorCoordinate(7, 6),
                sourceHandles: previous.TransitionPlan.FinalHandles,
                previous: previous.Window);
            Assert.That(previous.Success && next.Success, Is.True, Failures(next));
            Assert.That(next.Diff.AddPreloadCount, Is.EqualTo(7));
            Assert.That(next.Diff.RemovePreloadCount, Is.EqualTo(7));
            Assert.That(next.Diff.PromoteCount, Is.EqualTo(5));
            Assert.That(next.Diff.DemoteCount, Is.EqualTo(5));
            Assert.That(next.Diff.PreserveActiveCount, Is.EqualTo(20));
            Assert.That(next.Diff.PreservePreloadCount, Is.EqualTo(12));
            Assert.That(next.Diff.EvictCandidateCount, Is.EqualTo(7));
            Assert.That(next.Diff.Changes, Is.Ordered);
            Assert.That(next.TransitionPlan.RecordCount, Is.EqualTo(24));
            TestContext.WriteLine("MAP17_04_DIFF_EVIDENCE add_remove_promote_demote=" +
                "7/7/5/5 preserve_active_preload=20/12 evict=7 transitions=" +
                next.TransitionPlan.RecordCount + " diff=" + next.Diff.Digest);
        }

        [Test]
        public void SleepingModifiedSectorsPreserveDirtyRevisionAcrossWindowChanges()
        {
            var dirtyCoordinate = new GeneratedSectorCoordinate(3, 6);
            var allEntries = Entries().Concat(new[] { Entry(dirtyCoordinate, 1) }).ToArray();
            var allHandles = Handles().ToArray();
            var index = Array.FindIndex(allHandles, value => value.Sector.Equals(
                dirtyCoordinate.ToRuntimeCoordinate()));
            allHandles[index] = SleepingHandle(dirtyCoordinate);
            var previous = Plan(new GeneratedSectorCoordinate(6, 6), allHandles, allEntries);
            Assert.That(previous.Success, Is.True, Failures(previous));
            var next = Plan(new GeneratedSectorCoordinate(7, 6),
                previous.TransitionPlan.FinalHandles, allEntries, previous.Window);
            Assert.That(next.Success, Is.True, Failures(next));
            Assert.That(next.Diff.PreserveSleepingModifiedCount, Is.EqualTo(1));
            var preserved = next.TransitionPlan.FinalHandles.Single(value =>
                value.Sector.Equals(dirtyCoordinate.ToRuntimeCoordinate()));
            Assert.That(preserved.State, Is.EqualTo(GeneratedSectorRuntimeState.Unloaded));
            Assert.That(preserved.IsDirty, Is.True);
            Assert.That(preserved.MutationRevision, Is.EqualTo(1));
            Assert.That(preserved.DirtyReason, Is.EqualTo("PLAYER_MUTATION"));
            Assert.That(preserved.DurableSaveWriteCount, Is.Zero);
            TestContext.WriteLine("MAP17_04_SLEEPING_EVIDENCE preserved=1 revision=1" +
                " reason=PLAYER_MUTATION final_state=Unloaded durable_save_writes=0");
        }

        [Test]
        public void TransitionPlanUsesRuntimeHandleLifecycleWithoutSceneActivation()
        {
            var result = Plan(new GeneratedSectorCoordinate(6, 6));
            Assert.That(result.Success, Is.True, Failures(result));
            Assert.That(result.TransitionPlan.RecordCount, Is.EqualTo(74));
            Assert.That(result.TransitionPlan.SuccessfulRecordCount, Is.EqualTo(74));
            Assert.That(result.TransitionPlan.FailedRecordCount, Is.Zero);
            var activeTransitions = result.TransitionPlan.Records.Where(value =>
                value.LifecycleResult.Transition.To == GeneratedSectorRuntimeState.Active).ToArray();
            Assert.That(activeTransitions.Length, Is.EqualTo(25));
            Assert.That(activeTransitions.All(value =>
                value.LifecycleResult.Transition.From == GeneratedSectorRuntimeState.Preloaded ||
                value.LifecycleResult.Transition.From ==
                GeneratedSectorRuntimeState.SleepingModified), Is.True);
            Assert.That(result.TransitionPlan.FinalHandles.Count(value =>
                value.State == GeneratedSectorRuntimeState.Active), Is.EqualTo(25));
            Assert.That(result.TransitionPlan.FinalHandles.Count(value =>
                value.State == GeneratedSectorRuntimeState.Preloaded), Is.EqualTo(24));
            Assert.That(result.TransitionPlan.SceneActivationExecutionCount, Is.Zero);
            Assert.That(result.TransitionPlan.SceneMutationCount +
                result.TransitionPlan.GameObjectEnableCount, Is.Zero);
            var states = Enum.GetValues(typeof(GeneratedSectorRuntimeState))
                .Cast<GeneratedSectorRuntimeState>().ToArray();
            Assert.That(states.SelectMany(from => states.Select(to =>
                GeneratedSectorRuntimeHandleLifecycle.IsAllowed(from, to))).Count(value => value),
                Is.EqualTo(7));
            TestContext.WriteLine("MAP17_04_TRANSITION_EVIDENCE records=74 successful=74" +
                " failed=0 unloaded_preloaded=49 preloaded_active=25 final_active_preloaded=25/24" +
                " source_states=Unloaded/Preloaded/Active/SleepingModified allowed_reused=7/7" +
                " scene_activation_side_effects=0 transition=" + result.TransitionPlan.Digest);
        }

        [Test]
        public void WindowDigestsAreStableAcrossRepeatReverseCultureAndHandleOrder()
        {
            var center = new GeneratedSectorCoordinate(6, 6);
            var baseline = Plan(center);
            var repeat = Plan(center);
            var reverse = Plan(center, Handles().Reverse(), Entries().Reverse());
            Assert.That(baseline.Success && repeat.Success && reverse.Success, Is.True);
            AssertDigestSetEqual(baseline, repeat);
            AssertDigestSetEqual(baseline, reverse);

            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var culture = Plan(center, Handles().Reverse(), Entries().Reverse());
                Assert.That(culture.Success, Is.True, Failures(culture));
                AssertDigestSetEqual(baseline, culture);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }

            var changed = Plan(new GeneratedSectorCoordinate(7, 6));
            Assert.That(changed.Window.Digest, Is.Not.EqualTo(baseline.Window.Digest));
            Assert.That(changed.Diff.Digest, Is.Not.EqualTo(baseline.Diff.Digest));
            Assert.That(changed.TransitionPlan.Digest,
                Is.Not.EqualTo(baseline.TransitionPlan.Digest));
            Assert.That(GeneratedSectorWindowDigest.IsLowerHexSha256(
                baseline.Window.Digest), Is.True);
            Assert.That(GeneratedSectorWindowDigest.IsLowerHexSha256(
                baseline.Diff.Digest), Is.True);
            Assert.That(GeneratedSectorWindowDigest.IsLowerHexSha256(
                baseline.TransitionPlan.Digest), Is.True);
            TestContext.WriteLine("MAP17_04_DIGEST_EVIDENCE repeat_reverse_culture_handle_order=" +
                "0/0/0/0 mutation_sensitivity=3 window=" + baseline.Window.Digest +
                " diff=" + baseline.Diff.Digest + " transition=" +
                baseline.TransitionPlan.Digest);
        }

        [Test]
        public void PlannerRejectsInvalidCentersMissingCacheAndForbiddenTransitionsAtomically()
        {
            var invalid = Plan(new GeneratedSectorCoordinate(-1, 6));
            AssertAtomicFailure(invalid, GeneratedSectorStreamingFailureCode.InvalidCenter);
            var center = new GeneratedSectorCoordinate(6, 6);
            var missingCache = Plan(center, sourceEntries: Entries().Where(value =>
                !value.Key.Sector.Equals(center.ToRuntimeCoordinate())));
            AssertAtomicFailure(missingCache, GeneratedSectorStreamingFailureCode.MissingCache);
            var missingHandle = Plan(center, sourceHandles: Handles().Where(value =>
                !value.Sector.Equals(center.ToRuntimeCoordinate())));
            AssertAtomicFailure(missingHandle, GeneratedSectorStreamingFailureCode.MissingHandle);

            var unloaded = GeneratedSectorRuntimeHandleLifecycle.CreateUnloaded(BaseEntry(), SeedIdentity);
            var forbidden = GeneratedSectorRuntimeHandleLifecycle.Transition(
                new GeneratedSectorRuntimeTransitionRequest(unloaded,
                    GeneratedSectorRuntimeState.Active, unloaded.Sector,
                    GeneratedColliderCacheSnapshot.Empty.Store(BaseEntry()), BaseEntry()));
            Assert.That(forbidden.Success, Is.False);
            Assert.That(forbidden.Handle, Is.Null);
            Assert.That(forbidden.Failures.Select(value => value.Code),
                Does.Contain(GeneratedSectorRuntimeHandleFailureCode.ForbiddenTransition));
            var valid = Plan(center);
            Assert.That(valid.TransitionPlan.Records.Where(value => value.LifecycleResult.Transition.To ==
                GeneratedSectorRuntimeState.Active).All(value =>
                value.LifecycleResult.Transition.From == GeneratedSectorRuntimeState.Preloaded ||
                value.LifecycleResult.Transition.From ==
                GeneratedSectorRuntimeState.SleepingModified), Is.True);
            TestContext.WriteLine("MAP17_04_FAILURE_EVIDENCE invalid_center_missing_cache_handle=" +
                "1/1/1 forbidden_unloaded_active=1 partial_window_diff_transition=0/0/0");
        }

        [Test]
        public void Map17HandoffKeepsMap17_05Locked()
        {
            Assert.That(GeneratedSectorStreamingWindow.DownstreamOwner,
                Is.EqualTo("MAP17_05_IMPLEMENT_SECTOR_MODIFICATION_STORAGE"));
            Assert.That(GeneratedSectorStreamingWindow.OpensDownstreamTask, Is.False);
            var statusPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "MapDesign", "MCP", "06_IMPLEMENTATION_STATUS.md");
            var status = File.ReadAllText(statusPath);
            Assert.That(status, Does.Contain(
                "| MAP17_05_IMPLEMENT_SECTOR_MODIFICATION_STORAGE | LOCKED |"));
            Assert.That(status, Does.Not.Contain(
                "| MAP17_05_IMPLEMENT_SECTOR_MODIFICATION_STORAGE | CURRENT |"));
            var result = Plan(new GeneratedSectorCoordinate(6, 6));
            Assert.That(result.TransitionPlan.TilemapComponentWriteCount +
                result.TransitionPlan.ColliderCreationCount +
                result.TransitionPlan.RigidbodyCreationCount +
                result.TransitionPlan.PhysicsQueryCount +
                result.TransitionPlan.PhysicsSimulationCount +
                result.TransitionPlan.SceneMutationCount +
                result.TransitionPlan.PrefabMutationCount +
                result.TransitionPlan.TilemapMutationCount +
                result.TransitionPlan.GameObjectInstantiationCount +
                result.TransitionPlan.GameObjectEnableCount +
                result.TransitionPlan.GameObjectDisableCount +
                result.TransitionPlan.GameObjectDestroyCount +
                result.TransitionPlan.CameraReadCount + result.TransitionPlan.CameraWriteCount +
                result.TransitionPlan.CinemachineIntegrationCount +
                result.TransitionPlan.AddressablesLoadCount +
                result.TransitionPlan.ResourcesLoadCount +
                result.TransitionPlan.AssetDatabaseLoadCount +
                result.TransitionPlan.GeneratedCsvCommitCount +
                result.TransitionPlan.GeneratedAssetCommitCount +
                result.TransitionPlan.StableSpawnIdCount +
                result.TransitionPlan.RuntimeObjectSpawnCount +
                result.TransitionPlan.ProductionSeedApprovalCount +
                result.TransitionPlan.DurableSaveWriteCount, Is.Zero);
            TestContext.WriteLine("MAP17_04_DOWNSTREAM_EVIDENCE MAP17_05_started=NO locked=YES" +
                " tilemap_collider_rigidbody_physics=0/0/0/0 scene_prefab=0/0" +
                " gameobject_actions=0/0/0/0 camera_cinemachine=0/0 asset_loads=0/0/0" +
                " generated_csv_assets=0/0 stable_spawn_runtime=0/0 save_writes=0 seed=0");
        }

        private static GeneratedSectorStreamingResult Plan(
            GeneratedSectorCoordinate center,
            IEnumerable<GeneratedSectorRuntimeHandle> sourceHandles = null,
            IEnumerable<GeneratedColliderCacheEntry> sourceEntries = null,
            GeneratedSectorStreamingWindow previous = null,
            double progressX = 0.5d,
            double progressY = 0.5d,
            GeneratedSectorDirectionHint direction = GeneratedSectorDirectionHint.None,
            bool latched = false) => GeneratedSectorWindowPlanner.Plan(new GeneratedSectorWindowRequest(
                center, progressX, progressY, direction, GeneratedSectorPreactivationPolicy.Default,
                sourceHandles ?? Handles(), sourceEntries ?? Entries(), previous, latched));

        private static void AssertDigestSetEqual(
            GeneratedSectorStreamingResult expected,
            GeneratedSectorStreamingResult actual)
        {
            Assert.That(actual.Request.CanonicalDigest, Is.EqualTo(expected.Request.CanonicalDigest));
            Assert.That(actual.Window.Digest, Is.EqualTo(expected.Window.Digest));
            Assert.That(actual.Diff.Digest, Is.EqualTo(expected.Diff.Digest));
            Assert.That(actual.TransitionPlan.Digest,
                Is.EqualTo(expected.TransitionPlan.Digest));
        }

        private static void AssertAtomicFailure(
            GeneratedSectorStreamingResult result,
            GeneratedSectorStreamingFailureCode code)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Window, Is.Null);
            Assert.That(result.Diff, Is.Null);
            Assert.That(result.TransitionPlan, Is.Null);
            Assert.That(result.Failures.Select(value => value.Code), Does.Contain(code),
                Failures(result));
        }

        private static GeneratedSectorRuntimeHandle SleepingHandle(
            GeneratedSectorCoordinate coordinate)
        {
            var entry0 = Entry(coordinate, 0);
            var entry1 = Entry(coordinate, 1);
            var snapshot = GeneratedColliderCacheSnapshot.Empty.Store(entry0).Store(entry1);
            var unloaded = GeneratedSectorRuntimeHandleLifecycle.CreateUnloaded(entry0, SeedIdentity);
            var preloaded = Move(unloaded, GeneratedSectorRuntimeState.Preloaded, snapshot, entry0);
            var active = Move(preloaded.Handle, GeneratedSectorRuntimeState.Active,
                preloaded.CacheSnapshot);
            var sleeping = GeneratedSectorRuntimeHandleLifecycle.Transition(
                new GeneratedSectorRuntimeTransitionRequest(active.Handle,
                    GeneratedSectorRuntimeState.SleepingModified, active.Handle.Sector,
                    active.CacheSnapshot, mutationRevision: 1, dirtyReason: "PLAYER_MUTATION"));
            Assert.That(sleeping.Success, Is.True, Describe(sleeping.Failures));
            return sleeping.Handle;
        }

        private static GeneratedSectorRuntimeHandleResult Move(
            GeneratedSectorRuntimeHandle handle,
            GeneratedSectorRuntimeState target,
            GeneratedColliderCacheSnapshot snapshot,
            GeneratedColliderCacheEntry entry = null)
        {
            var result = GeneratedSectorRuntimeHandleLifecycle.Transition(
                new GeneratedSectorRuntimeTransitionRequest(handle, target,
                    handle.Sector, snapshot, entry));
            Assert.That(result.Success, Is.True, Describe(result.Failures));
            return result;
        }

        private static GeneratedSectorRuntimeHandle[] Handles()
        {
            if (handles != null) return handles;
            handles = Entries().Select(value =>
                GeneratedSectorRuntimeHandleLifecycle.CreateUnloaded(value, SeedIdentity)).ToArray();
            return handles;
        }

        private static GeneratedColliderCacheEntry[] Entries()
        {
            if (entries != null) return entries;
            entries = Enumerable.Range(0,
                    GeneratedTerrainGeometrySnapshot.CanonicalWorldSectorCount)
                .Select(index => new GeneratedSectorCoordinate(
                    index % GeneratedTerrainGeometrySnapshot.CanonicalWorldSectorColumns,
                    index / GeneratedTerrainGeometrySnapshot.CanonicalWorldSectorColumns))
                .Select(coordinate => Entry(coordinate, 0)).ToArray();
            return entries;
        }

        private static GeneratedColliderCacheEntry BaseEntry() =>
            Entry(new GeneratedSectorCoordinate(3, 4), 0);

        private static GeneratedColliderCacheEntry Entry(
            GeneratedSectorCoordinate coordinate,
            int revision)
        {
            var plan = Rebuild(revision);
            var source = GeneratedColliderCacheKey.Create(plan, GeneratorVersion, DataVersion);
            var key = new GeneratedColliderCacheKey(source.GeometryDigest, source.BakeDigest,
                source.SeamDigest, source.RegistryDigest, coordinate.ToRuntimeCoordinate(),
                source.GeneratorVersion, source.DataVersion, revision,
                source.CollisionPolicyVersion);
            return new GeneratedColliderCacheEntry(key, plan);
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

        private static string Failures(GeneratedSectorStreamingResult result) => result == null
            ? "NULL RESULT" : Describe(result.Failures);
        private static string Describe(System.Collections.IEnumerable values)
        {
            var output = new List<string>();
            foreach (var value in values) output.Add(value == null ? "NULL" : value.ToString());
            return string.Join(";", output);
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
