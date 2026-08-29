using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.SpecialRegions
{
    [TestFixture]
    [Category("MAP13_03")]
    public sealed class SpecialRegionFixedSlotPersistenceTests
    {
        private LayerFixture fixture;

        [OneTimeSetUp]
        public void SetUp()
        {
            fixture = BuildFixture(false);
        }

        [Test]
        public void FixedShellAndEntryReturnApronsProjectAsSeparateHardProtectedLayers()
        {
            var plan = fixture.Layer.Plan;

            AssertLayerSuccess(fixture.Layer);
            Assert.That(plan.FixedCollision, Has.Count.EqualTo(2));
            Assert.That(plan.FixedCollision.All(value => value.IsImmutable && value.IsHardProtected &&
                value.OwnsCollision && !value.OwnsAccess), Is.True);
            Assert.That(plan.FixedAccess, Has.Count.EqualTo(192));
            Assert.That(plan.FixedAccess.Count(value => value.AccessKind == SpecialRegionFixedAccessKind.Entry),
                Is.EqualTo(1));
            Assert.That(plan.FixedAccess.Count(value => value.AccessKind == SpecialRegionFixedAccessKind.Return),
                Is.EqualTo(1));
            Assert.That(plan.FixedAccess.Count(value => value.AccessKind == SpecialRegionFixedAccessKind.Apron),
                Is.EqualTo(190));
            Assert.That(plan.FixedAccess.All(value => value.IsImmutable && value.IsHardProtected &&
                value.OwnsAccess && !value.OwnsCollision), Is.True);
            Assert.That(plan.FixedCollision.Select(value => value.Coordinate)
                .Intersect(plan.FixedAccess.Select(value => value.Coordinate)), Is.Empty);
            Assert.That(plan.HardProtectedClaims, Has.Count.EqualTo(2));
            Assert.That(plan.HardProtectedClaims.All(value => value.IsHardProtected), Is.True);
            Assert.That(plan.FixedCollisionDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(plan.FixedAccessDigest, Does.Match("^[0-9a-f]{64}$"));
            Assert.That(plan.TileMutationCount, Is.Zero);

            var quietSectors = fixture.EntryPlan.QuietChunks.Select(value => value.WorldSector).Distinct().ToArray();
            Assert.That(plan.FixedCollision.Select(value => value.Coordinate.WorldSector)
                .Intersect(quietSectors), Is.Empty);
            Assert.That(plan.FixedAccess.Select(value => value.Coordinate.WorldSector)
                .Intersect(quietSectors), Is.Empty);
        }

        [Test]
        public void FiveReplaceableKindsExcludeEntryReturnAndOwnNoGeometryOrAccess()
        {
            var slots = fixture.Layer.Plan.ReplaceableSlots;

            Assert.That(slots, Has.Count.EqualTo(5));
            Assert.That(slots.Select(value => value.Kind), Is.EquivalentTo(new[]
            {
                SpecialRegionSlotKind.Facility,
                SpecialRegionSlotKind.Npc,
                SpecialRegionSlotKind.Enemy,
                SpecialRegionSlotKind.Event,
                SpecialRegionSlotKind.Reward,
            }));
            Assert.That(slots.Any(value => value.Kind == SpecialRegionSlotKind.Entry ||
                                           value.Kind == SpecialRegionSlotKind.Return), Is.False);
            Assert.That(slots.All(value => value.LayerKind == SpecialRegionLayerKind.ReplaceableSlot &&
                value.IsMarkerOnly && !value.OwnsSolid && !value.OwnsCollision &&
                !value.OwnsRoute && !value.OwnsAccess && !value.PerformsRuntimeMutation), Is.True);
            Assert.That(slots.Select(value => value.Coordinate).Distinct().Count(), Is.EqualTo(5));
            Assert.That(slots.Select(value => value.Coordinate).Intersect(
                fixture.Layer.Plan.FixedCollision.Select(value => value.Coordinate)), Is.Empty);
            Assert.That(slots.Select(value => value.Coordinate).Intersect(
                fixture.Layer.Plan.FixedAccess.Select(value => value.Coordinate)), Is.Empty);
        }

        [Test]
        public void AssignAndClearPreserveGeometryPersistenceAndUnderlyingInvariantDigest()
        {
            var intents = fixture.Layer.Plan.ReplaceableSlots.Select(value =>
                SpecialRegionSlotReplacementIntent.Assign(
                    value.SlotId, value.Kind, "OCC_" + value.Kind.ToString().ToUpperInvariant())).ToArray();
            var assigned = CompileLayer(fixture, intents);
            var cleared = CompileLayer(fixture, fixture.Layer.Plan.ReplaceableSlots.Select(value =>
                SpecialRegionSlotReplacementIntent.Clear(value.SlotId)));

            AssertLayerSuccess(assigned);
            AssertLayerSuccess(cleared);
            Assert.That(assigned.Plan.ReplaceableSlots.All(value => value.IsAssigned), Is.True);
            Assert.That(cleared.Plan.ReplaceableSlots.All(value => !value.IsAssigned), Is.True);
            Assert.That(assigned.Plan.FixedCollisionDigest, Is.EqualTo(cleared.Plan.FixedCollisionDigest));
            Assert.That(assigned.Plan.FixedAccessDigest, Is.EqualTo(cleared.Plan.FixedAccessDigest));
            Assert.That(assigned.Plan.ReplaceableSlotDigest, Is.EqualTo(cleared.Plan.ReplaceableSlotDigest));
            Assert.That(assigned.Plan.ImmutableLayerDigest, Is.EqualTo(cleared.Plan.ImmutableLayerDigest));
            Assert.That(assigned.Plan.AssignmentDigest, Is.Not.EqualTo(cleared.Plan.AssignmentDigest));
            Assert.That(assigned.Plan.CanonicalDigest, Is.Not.EqualTo(cleared.Plan.CanonicalDigest));
            Assert.That(assigned.Plan.ReplaceableSlots.Select(value => new
            {
                value.SlotId,
                value.Kind,
                value.Required,
                value.PersistenceScope,
                value.PersistenceKey,
                value.Coordinate,
                value.IdentityDigest,
            }), Is.EqualTo(cleared.Plan.ReplaceableSlots.Select(value => new
            {
                value.SlotId,
                value.Kind,
                value.Required,
                value.PersistenceScope,
                value.PersistenceKey,
                value.Coordinate,
                value.IdentityDigest,
            })));
            Assert.That(assigned.Plan.PlacementWriteCount + assigned.Plan.SpawnCount +
                assigned.Plan.DespawnCount, Is.Zero);
        }

        [Test]
        public void ExactKindAssignmentsKeepEventMarkerOnlyAndRejectWrongOccupantsAtomically()
        {
            foreach (var slot in fixture.Layer.Plan.ReplaceableSlots)
            {
                var result = CompileLayer(fixture, new[]
                {
                    SpecialRegionSlotReplacementIntent.Assign(
                        slot.SlotId, slot.Kind, "OCCUPANT_" + slot.Kind.ToString().ToUpperInvariant()),
                });
                AssertLayerSuccess(result);
                var assigned = result.Plan.ReplaceableSlots.Single(value => value.SlotId == slot.SlotId);
                Assert.That(assigned.OccupantKind, Is.EqualTo(slot.Kind));
                Assert.That(assigned.OccupantOwnsPersistence, Is.False);
                if (slot.Kind == SpecialRegionSlotKind.Event)
                    Assert.That(assigned.IsEventMarkerOnly, Is.True);
            }

            var eventSlot = fixture.Layer.Plan.ReplaceableSlots.Single(
                value => value.Kind == SpecialRegionSlotKind.Event);
            var invalid = CompileLayer(fixture, new[]
            {
                SpecialRegionSlotReplacementIntent.Assign(
                    eventSlot.SlotId, SpecialRegionSlotKind.Npc, "OCCUPANT_WRONG_KIND"),
            });
            AssertLayerFailure(invalid, SpecialRegionFixedSlotLayerErrorCode.ReplaceableKindMismatch);

            var entry = fixture.Bridge.SlotBindings.Single(value => value.Kind == SpecialRegionSlotKind.Entry);
            var entryReplacement = CompileLayer(fixture, new[]
            {
                SpecialRegionSlotReplacementIntent.Assign(
                    entry.SlotId, SpecialRegionSlotKind.Entry, "OCCUPANT_ENTRY"),
            });
            AssertLayerFailure(entryReplacement, SpecialRegionFixedSlotLayerErrorCode.ReplaceableKindMismatch);
        }

        [Test]
        public void RequiredRewardInterruptFailRegenerateBranchesRecoverAvailable()
        {
            var result = CompileSafety(fixture.Layer.Plan, Evidence(
                fixture.Layer.Plan, SpecialRegionRequiredResourceState.TemporarilyUnavailable));

            AssertSafetySuccess(result);
            Assert.That(result.Proofs, Has.Count.EqualTo(1));
            var proof = result.Proofs.Single();
            Assert.That(proof.Evidence, Has.Count.EqualTo(7));
            Assert.That(proof.InitialAvailable, Is.True);
            Assert.That(proof.RecoveryBranchesAvailable, Is.True);
            Assert.That(proof.PermanentlyUnavailableCount, Is.Zero);
            Assert.That(proof.RewardGrantCount + proof.SaveWriteCount, Is.Zero);
        }

        [Test]
        public void ClaimAndRevisitRemainClaimedWithoutDuplicateRewardRisk()
        {
            var result = CompileSafety(fixture.Layer.Plan, Evidence(fixture.Layer.Plan));

            AssertSafetySuccess(result);
            var proof = result.Proofs.Single();
            Assert.That(proof.ClaimStable, Is.True);
            Assert.That(proof.Evidence.Single(value =>
                value.Checkpoint == SpecialRegionPersistenceCheckpoint.Claimed).State,
                Is.EqualTo(SpecialRegionRequiredResourceState.Claimed));
            Assert.That(proof.Evidence.Single(value =>
                value.Checkpoint == SpecialRegionPersistenceCheckpoint.Revisited).State,
                Is.EqualTo(SpecialRegionRequiredResourceState.Claimed));
            Assert.That(proof.DuplicateRewardRiskCount, Is.Zero);
            Assert.That(result.RewardGrantCount + result.InventoryMutationCount + result.SaveWriteCount, Is.Zero);
        }

        [Test]
        public void PermanentLossMissingCheckpointAndKeyScopeDriftFailAtomically()
        {
            var baseline = Evidence(fixture.Layer.Plan).ToList();

            var permanent = ReplaceEvidence(
                baseline, SpecialRegionPersistenceCheckpoint.Regenerated,
                state: SpecialRegionRequiredResourceState.PermanentlyUnavailable);
            AssertSafetyFailure(CompileSafety(fixture.Layer.Plan, permanent),
                SpecialRegionPersistenceSafetyErrorCode.RequiredResourcePermanentlyLost);

            var missing = baseline.Where(value =>
                value.Checkpoint != SpecialRegionPersistenceCheckpoint.Failed).ToArray();
            AssertSafetyFailure(CompileSafety(fixture.Layer.Plan, missing),
                SpecialRegionPersistenceSafetyErrorCode.MissingCheckpoint);

            var keyDrift = ReplaceEvidence(
                baseline, SpecialRegionPersistenceCheckpoint.Active,
                key: new SpecialPersistenceKey("SR_STATE_DRIFT_REWARD"));
            AssertSafetyFailure(CompileSafety(fixture.Layer.Plan, keyDrift),
                SpecialRegionPersistenceSafetyErrorCode.PersistenceKeyMismatch);

            var scopeDrift = ReplaceEvidence(
                baseline, SpecialRegionPersistenceCheckpoint.Active,
                scope: SpecialPersistenceScope.Slot);
            AssertSafetyFailure(CompileSafety(fixture.Layer.Plan, scopeDrift),
                SpecialRegionPersistenceSafetyErrorCode.PersistenceScopeMismatch);
        }

        [Test]
        public void ClaimRollbackPublishesNoProofAndReportsDuplicateRewardRisk()
        {
            var rollback = ReplaceEvidence(
                Evidence(fixture.Layer.Plan), SpecialRegionPersistenceCheckpoint.Revisited,
                state: SpecialRegionRequiredResourceState.Available);
            var result = CompileSafety(fixture.Layer.Plan, rollback);

            AssertSafetyFailure(result, SpecialRegionPersistenceSafetyErrorCode.ClaimRollback);
            Assert.That(result.Errors.Select(value => value.Code),
                Does.Contain(SpecialRegionPersistenceSafetyErrorCode.DuplicateRewardRisk));
            Assert.That(result.Proofs, Is.Empty);
            Assert.That(result.AggregateSafetyPublished, Is.False);
        }

        [Test]
        public void MissingHardProtectedCoverageAndUpstreamOverlapsFailBeforePublication()
        {
            var fixedOnly = new SpecialRegionOccupancyClaim(
                "SR_FIXED_COLLISION_ONLY", SpecialRegionPlacementOwnerKind.CoreResource,
                fixture.Bridge.FixedShellBindings.Select(value => new SpecialRegionTileCoordinate(
                    value.Placed.WorldSector, value.Placed.LocalTile)), true);
            var collision = SpecialRegionPlacementCollisionCompiler.Compile(
                new SpecialRegionPlacementCollisionCompileRequest(new[] { fixedOnly }));
            Assert.That(collision.Succeeded, Is.True, string.Join("\n", collision.Errors));
            var uncovered = CompileLayer(fixture, null, collision.Plan);
            AssertLayerFailure(uncovered, SpecialRegionFixedSlotLayerErrorCode.DuplicateFixedOwner);

            var blockedEntryApron = new SpecialRegionEntryApron(
                "SR_PORT_ENTRY", new SectorCoord(5, 5), new LocalTileCoord(0, 0), 22, 22,
                Rectangle(5, 5, 0, 0, 22, 22));
            var blockedReturnApron = new SpecialRegionEntryApron(
                "SR_PORT_RETURN", new SectorCoord(5, 5), new LocalTileCoord(22, 0), 26, 22,
                Rectangle(5, 5, 22, 0, 26, 22));
            var upstream = SpecialRegionEntryBufferCompiler.Compile(
                new SpecialRegionEntryBufferCompileRequest(
                    fixture.Bridge, fixture.Bridge.CanonicalDigest,
                    "SR_PORT_ENTRY", fixture.EntryAnchor, blockedEntryApron,
                    "SR_PORT_RETURN", fixture.ReturnAnchor, blockedReturnApron,
                    fixture.Before, fixture.After));
            Assert.That(upstream.Succeeded, Is.False);
            Assert.That(upstream.Plan, Is.Null);
            Assert.That(upstream.CanonicalDigest, Is.Empty);
            Assert.That(upstream.Errors.Select(value => value.Code),
                Does.Contain(SpecialRegionEntryBufferErrorCode.ApronBlocked));
        }

        [Test]
        public void ReverseRepeatCultureAndCallerMutationRemainCanonicalAndImmutable()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var reverse = BuildFixture(true);
                Assert.That(reverse.Layer.CanonicalDigest, Is.EqualTo(fixture.Layer.CanonicalDigest));

                var intents = new List<SpecialRegionSlotReplacementIntent>
                {
                    SpecialRegionSlotReplacementIntent.Assign(
                        fixture.Layer.Plan.ReplaceableSlots.Single(value =>
                            value.Kind == SpecialRegionSlotKind.Reward).SlotId,
                        SpecialRegionSlotKind.Reward, "OCCUPANT_REWARD"),
                };
                var request = LayerRequest(fixture, intents);
                intents.Clear();
                var first = SpecialRegionFixedSlotLayerCompiler.Compile(request);
                var repeat = SpecialRegionFixedSlotLayerCompiler.Compile(request);
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var culture = CompileLayer(fixture, new[]
                {
                    SpecialRegionSlotReplacementIntent.Assign(
                        fixture.Layer.Plan.ReplaceableSlots.Single(value =>
                            value.Kind == SpecialRegionSlotKind.Reward).SlotId,
                        SpecialRegionSlotKind.Reward, "OCCUPANT_REWARD"),
                });

                AssertLayerSuccess(first);
                Assert.That(repeat.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));
                Assert.That(culture.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));

                var evidence = Evidence(first.Plan).ToList();
                var safetyRequest = new SpecialRegionPersistenceSafetyCompileRequest(
                    first.Plan, first.Plan.CanonicalDigest, evidence);
                evidence.Clear();
                var safety = SpecialRegionPersistenceSafetyCompiler.Compile(safetyRequest);
                var safetyReverse = CompileSafety(first.Plan, Evidence(first.Plan).Reverse());
                AssertSafetySuccess(safety);
                Assert.That(safetyReverse.CanonicalDigest, Is.EqualTo(safety.CanonicalDigest));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Test]
        public void RuntimeSourcesContainNoRngWorldTileSaveOrUnityLifecycleMutationAuthority()
        {
            var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", ".."));
            var sources = new[]
            {
                "Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionFixedSlotLayers.cs",
                "Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionPersistenceSafety.cs",
            };
            var text = string.Join("\n", sources.Select(path => File.ReadAllText(Path.Combine(root, path))));
            foreach (var forbidden in new[]
                     {
                         "UnityEditor", "UnityEngine.Random", "System.Random", "DateTime", "Time.deltaTime",
                         "File.", "Directory.", "MonoBehaviour", "ScriptableObject", "Tilemap",
                         "SaveData", "PlayerPrefs", "Instantiate(", "Destroy(",
                     })
                Assert.That(text, Does.Not.Contain(forbidden), forbidden);
        }

        private static LayerFixture BuildFixture(bool reverse)
        {
            var reservationId = new SiteReservationId("RES_MAP13_03_CORE");
            var regionId = new SpecialRegionId("SR_MAP13_03_CORE");
            var origin = new SectorCoord(5, 5);
            var entryAnchor = new SiteEntryAnchor(
                reservationId, "ENTRY_MAIN", origin, SiteEntrySide.L,
                new[] { 1, 2, 3 }, true, false);
            var returnAnchor = new SiteEntryAnchor(
                reservationId, "RETURN_MAIN", origin, SiteEntrySide.R,
                new[] { 1, 2, 3 }, true, true);
            var reservation = new SiteReservation(
                reservationId, SiteReservationKind.CoreResource, "SPECIAL_CORE", origin,
                new SiteFootprint(1, 1, SiteFootprintTransform.R0, new[]
                {
                    new SiteFootprintCell(0, 0, "CORE_RESOURCE", string.Empty, string.Empty,
                        new[] { SiteEntrySide.L, SiteEntrySide.R })
                }), string.Empty, 1,
                reverse ? new[] { returnAnchor, entryAnchor } : new[] { entryAnchor, returnAnchor });

            var entryId = new SpecialRegionSlotId("SR_SLOT_ENTRY");
            var returnId = new SpecialRegionSlotId("SR_SLOT_RETURN");
            var facilityId = new SpecialRegionSlotId("SR_SLOT_FACILITY");
            var npcId = new SpecialRegionSlotId("SR_SLOT_NPC");
            var enemyId = new SpecialRegionSlotId("SR_SLOT_ENEMY");
            var eventId = new SpecialRegionSlotId("SR_SLOT_EVENT");
            var rewardId = new SpecialRegionSlotId("SR_SLOT_REWARD");
            var bindings = new[]
            {
                Binding(regionId, facilityId, SpecialRegionSlotKind.Facility, SpecialPersistenceScope.Slot),
                Binding(regionId, npcId, SpecialRegionSlotKind.Npc, SpecialPersistenceScope.Slot),
                Binding(regionId, enemyId, SpecialRegionSlotKind.Enemy, SpecialPersistenceScope.Encounter),
                Binding(regionId, eventId, SpecialRegionSlotKind.Event, SpecialPersistenceScope.Encounter),
                Binding(regionId, rewardId, SpecialRegionSlotKind.Reward, SpecialPersistenceScope.Reward),
            };
            var slots = new[]
            {
                new SpecialRegionSlot(entryId, SpecialRegionSlotKind.Entry,
                    new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(0, 1), true,
                    default(SpecialPersistenceScope), default(SpecialPersistenceKey)),
                new SpecialRegionSlot(returnId, SpecialRegionSlotKind.Return,
                    new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(47, 1), true,
                    default(SpecialPersistenceScope), default(SpecialPersistenceKey)),
                Slot(facilityId, SpecialRegionSlotKind.Facility, 10, bindings[0]),
                Slot(npcId, SpecialRegionSlotKind.Npc, 12, bindings[1]),
                Slot(enemyId, SpecialRegionSlotKind.Enemy, 14, bindings[2]),
                Slot(eventId, SpecialRegionSlotKind.Event, 16, bindings[3]),
                Slot(rewardId, SpecialRegionSlotKind.Reward, 18, bindings[4], true),
            };
            var ports = new[]
            {
                new SpecialRegionPort("SR_PORT_ENTRY", entryId, SpecialRegionSlotKind.Entry,
                    new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(0, 1),
                    SiteEntrySide.L, AccessClass.MandatoryNoTool),
                new SpecialRegionPort("SR_PORT_RETURN", returnId, SpecialRegionSlotKind.Return,
                    new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(47, 1),
                    SiteEntrySide.R, AccessClass.MandatoryNoTool),
            };
            var persistence = new[]
            {
                new SpecialPersistenceBinding(SpecialPersistenceKey.ForRegion(regionId),
                    SpecialPersistenceScope.Region, default(SpecialRegionSlotId), "INITIAL_UNCLAIMED"),
            }.Concat(bindings).ToArray();
            var contract = new SpecialRegionContract(
                regionId, SpecialRegionKind.CoreResource, reservationId,
                new SpecialRegionFootprint(new[] { new SpecialRegionSectorOffset(0, 0) }),
                new[]
                {
                    new SpecialRegionFixedShellCell(
                        new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(20, 20), "SHELL_WALL_A"),
                    new SpecialRegionFixedShellCell(
                        new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(21, 20), "SHELL_WALL_B"),
                },
                reverse ? slots.Reverse() : slots,
                reverse ? ports.Reverse() : ports,
                reverse ? persistence.Reverse() : persistence,
                "MAP13_03 fixture");
            var validation = SpecialRegionValidator.Validate(contract, reservation);
            Assert.That(validation.IsValid, Is.True, string.Join("\n", validation.Errors));
            var bridgeResult = SpecialRegionSiteBridgeCompiler.Compile(
                BuildSnapshot(reservation, reverse), validation);
            Assert.That(bridgeResult.Succeeded, Is.True, string.Join("\n", bridgeResult.Errors));

            var candidate = BuildQuietCandidate(reverse);
            var entryApron = new SpecialRegionEntryApron(
                "SR_PORT_ENTRY", origin, new LocalTileCoord(0, 0), 4, 4,
                reverse ? Rectangle(5, 5, 0, 0, 4, 4).Reverse() : Rectangle(5, 5, 0, 0, 4, 4));
            var returnApron = new SpecialRegionEntryApron(
                "SR_PORT_RETURN", origin, new LocalTileCoord(4, 0), 44, 4,
                reverse ? Rectangle(5, 5, 4, 0, 44, 4).Reverse() : Rectangle(5, 5, 4, 0, 44, 4));
            var beforeChunks = new[]
            {
                new SpecialRegionQuietChunkPlacement(
                    new ClusterChunkCoord(0, 0), new SectorCoord(4, 5), new ClusterChunkCoord(2, 0)),
                new SpecialRegionQuietChunkPlacement(
                    new ClusterChunkCoord(1, 0), new SectorCoord(4, 5), new ClusterChunkCoord(3, 0)),
            };
            var afterChunks = new[]
            {
                new SpecialRegionQuietChunkPlacement(
                    new ClusterChunkCoord(0, 0), new SectorCoord(6, 5), new ClusterChunkCoord(0, 0)),
                new SpecialRegionQuietChunkPlacement(
                    new ClusterChunkCoord(1, 0), new SectorCoord(6, 5), new ClusterChunkCoord(1, 0)),
            };
            var before = new SpecialRegionQuietBufferPlacement(
                "placement.before", SpecialRegionQuietChunkRole.Before, candidate,
                reverse ? beforeChunks.Reverse() : beforeChunks);
            var after = new SpecialRegionQuietBufferPlacement(
                "placement.after", SpecialRegionQuietChunkRole.After, candidate,
                reverse ? afterChunks.Reverse() : afterChunks);
            var entryResult = SpecialRegionEntryBufferCompiler.Compile(
                new SpecialRegionEntryBufferCompileRequest(
                    bridgeResult.Bridge, bridgeResult.CanonicalDigest,
                    "SR_PORT_ENTRY", entryAnchor, entryApron,
                    "SR_PORT_RETURN", returnAnchor, returnApron,
                    before, after));
            Assert.That(entryResult.Succeeded, Is.True, string.Join("\n", entryResult.Errors));

            var fixedCells = bridgeResult.Bridge.FixedShellBindings.Select(value =>
                new SpecialRegionTileCoordinate(value.Placed.WorldSector, value.Placed.LocalTile));
            var accessCells = entryResult.Plan.Aprons.SelectMany(value => value.Cells).Distinct();
            var collisionResult = SpecialRegionPlacementCollisionCompiler.Compile(
                new SpecialRegionPlacementCollisionCompileRequest(reverse
                    ? new[]
                    {
                        new SpecialRegionOccupancyClaim(
                            "SR_FIXED_ACCESS", SpecialRegionPlacementOwnerKind.CoreResource,
                            accessCells.Reverse(), true),
                        new SpecialRegionOccupancyClaim(
                            "SR_FIXED_COLLISION", SpecialRegionPlacementOwnerKind.CoreResource,
                            fixedCells.Reverse(), true),
                    }
                    : new[]
                    {
                        new SpecialRegionOccupancyClaim(
                            "SR_FIXED_COLLISION", SpecialRegionPlacementOwnerKind.CoreResource,
                            fixedCells, true),
                        new SpecialRegionOccupancyClaim(
                            "SR_FIXED_ACCESS", SpecialRegionPlacementOwnerKind.CoreResource,
                            accessCells, true),
                    }));
            Assert.That(collisionResult.Succeeded, Is.True, string.Join("\n", collisionResult.Errors));

            var value = new LayerFixture(
                validation, bridgeResult.Bridge, entryResult.Plan, collisionResult.Plan,
                entryAnchor, returnAnchor, entryApron, returnApron, before, after);
            value.Layer = CompileLayer(value);
            AssertLayerSuccess(value.Layer);
            return value;
        }

        private static SpecialRegionSlot Slot(
            SpecialRegionSlotId id,
            SpecialRegionSlotKind kind,
            int x,
            SpecialPersistenceBinding binding,
            bool required = false)
            => new SpecialRegionSlot(
                id, kind, new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(x, 10),
                required, binding.Scope, binding.Key);

        private static SpecialPersistenceBinding Binding(
            SpecialRegionId regionId,
            SpecialRegionSlotId slotId,
            SpecialRegionSlotKind kind,
            SpecialPersistenceScope scope)
            => new SpecialPersistenceBinding(
                SpecialPersistenceKey.ForSlot(regionId, scope, slotId),
                scope, slotId, "INITIAL_" + kind.ToString().ToUpperInvariant());

        private static SpecialRegionFixedSlotLayerCompileRequest LayerRequest(
            LayerFixture value,
            IEnumerable<SpecialRegionSlotReplacementIntent> replacements = null,
            SpecialRegionPlacementCollisionPlan collisionPlan = null)
        {
            var collision = collisionPlan ?? value.CollisionPlan;
            return new SpecialRegionFixedSlotLayerCompileRequest(
                value.Validation, value.Validation.CanonicalDigest,
                value.Bridge, value.Bridge.CanonicalDigest,
                value.EntryPlan, value.EntryPlan.CanonicalDigest,
                collision, collision.CanonicalDigest,
                replacements);
        }

        private static SpecialRegionFixedSlotLayerResult CompileLayer(
            LayerFixture value,
            IEnumerable<SpecialRegionSlotReplacementIntent> replacements = null,
            SpecialRegionPlacementCollisionPlan collisionPlan = null)
            => SpecialRegionFixedSlotLayerCompiler.Compile(
                LayerRequest(value, replacements, collisionPlan));

        private static SpecialRegionPersistenceSafetyResult CompileSafety(
            SpecialRegionFixedSlotLayerPlan plan,
            IEnumerable<SpecialRegionPersistenceCheckpointEvidence> evidence)
            => SpecialRegionPersistenceSafetyCompiler.Compile(
                new SpecialRegionPersistenceSafetyCompileRequest(
                    plan, plan.CanonicalDigest, evidence));

        private static IEnumerable<SpecialRegionPersistenceCheckpointEvidence> Evidence(
            SpecialRegionFixedSlotLayerPlan plan,
            SpecialRegionRequiredResourceState active = SpecialRegionRequiredResourceState.Available)
        {
            var reward = plan.ReplaceableSlots.Single(value =>
                value.Kind == SpecialRegionSlotKind.Reward && value.Required);
            var states = new Dictionary<SpecialRegionPersistenceCheckpoint, SpecialRegionRequiredResourceState>
            {
                { SpecialRegionPersistenceCheckpoint.Initial, SpecialRegionRequiredResourceState.Available },
                { SpecialRegionPersistenceCheckpoint.Active, active },
                { SpecialRegionPersistenceCheckpoint.Interrupted, SpecialRegionRequiredResourceState.Available },
                { SpecialRegionPersistenceCheckpoint.Failed, SpecialRegionRequiredResourceState.Available },
                { SpecialRegionPersistenceCheckpoint.Regenerated, SpecialRegionRequiredResourceState.Available },
                { SpecialRegionPersistenceCheckpoint.Claimed, SpecialRegionRequiredResourceState.Claimed },
                { SpecialRegionPersistenceCheckpoint.Revisited, SpecialRegionRequiredResourceState.Claimed },
            };
            return states.Select(value => new SpecialRegionPersistenceCheckpointEvidence(
                plan.RegionId, reward.SlotId, reward.PersistenceKey, reward.PersistenceScope,
                value.Key, value.Value, reward.IdentityDigest));
        }

        private static IEnumerable<SpecialRegionPersistenceCheckpointEvidence> ReplaceEvidence(
            IEnumerable<SpecialRegionPersistenceCheckpointEvidence> source,
            SpecialRegionPersistenceCheckpoint checkpoint,
            SpecialRegionRequiredResourceState? state = null,
            SpecialPersistenceKey? key = null,
            SpecialPersistenceScope? scope = null)
            => source.Select(value => value.Checkpoint != checkpoint
                ? value
                : new SpecialRegionPersistenceCheckpointEvidence(
                    value.RegionId, value.SlotId,
                    key ?? value.PersistenceKey,
                    scope ?? value.PersistenceScope,
                    value.Checkpoint,
                    state ?? value.State,
                    value.SourceDigest));

        private static SiteReservationSnapshot BuildSnapshot(SiteReservation reservation, bool reverse)
        {
            var start = new SiteReservation(
                new SiteReservationId("RES_START"), SiteReservationKind.Start, "START_SITE",
                new SectorCoord(1, 1),
                new SiteFootprint(1, 1, SiteFootprintTransform.R0, new[]
                {
                    new SiteFootprintCell(0, 0, "START", string.Empty, string.Empty,
                        Array.Empty<SiteEntrySide>())
                }), string.Empty, 0, Array.Empty<SiteEntryAnchor>());
            var reservations = new List<SiteReservation> { start, reservation };
            var occupied = new Dictionary<SectorCoord, Tuple<SiteReservation, SiteFootprintCell>>();
            foreach (var site in reservations)
            foreach (var sector in site.OccupiedSectors)
            {
                site.TryGetFootprintCell(sector, out var cell);
                occupied.Add(sector, Tuple.Create(site, cell));
            }
            var rows = new List<SectorReservation>();
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var coordinate = WorldGridIndex.ToCoordinate(index);
                if (occupied.TryGetValue(coordinate, out var binding))
                    rows.Add(SectorReservation.CreateReserved(
                        index, coordinate, binding.Item1.ReservationId, binding.Item1.Kind,
                        binding.Item2.LocalX, binding.Item2.LocalY, binding.Item2.LocalRole));
                else rows.Add(SectorReservation.CreateUnreserved(index, coordinate));
            }
            if (reverse)
            {
                reservations.Reverse();
                rows.Reverse();
            }
            return new SiteReservationSnapshot(1303UL, reservations, rows, Array.Empty<CoreBiomeSeed>());
        }

        private static TerrainClusterQuietBufferCandidate BuildQuietCandidate(bool reverse)
        {
            var contract = CreateClusterContract(reverse);
            var validation = TerrainClusterContractValidator.Validate(contract);
            Assert.That(validation.IsValid, Is.True, string.Join("\n", validation.Errors));
            var canvasResult = TerrainClusterFootprintCompiler.Compile(
                new TerrainClusterFootprintCompileRequest(contract, ClusterFootprintTransform.R0));
            Assert.That(canvasResult.IsSuccess, Is.True, string.Join("\n", canvasResult.Errors));
            var canvas = canvasResult.LocalCanvas;
            var roleResult = TerrainClusterRoleSocketCompiler.Compile(
                new TerrainClusterRoleSocketCompileRequest(
                    contract, validation.CanonicalDigest, canvas, canvas.CanonicalDigest,
                    SocketEvidence()));
            Assert.That(roleResult.IsSuccess, Is.True, string.Join("\n", roleResult.Errors));
            var traversalResult = TerrainClusterTraversalCompiler.Compile(
                new TerrainClusterTraversalCompileRequest(
                    contract, validation.CanonicalDigest, canvas, canvas.CanonicalDigest,
                    roleResult.Contract, roleResult.CanonicalDigest));
            Assert.That(traversalResult.IsSuccess, Is.True, string.Join("\n", traversalResult.Errors));
            var witnessResult = TerrainClusterRouteWitnessCompiler.Compile(
                new TerrainClusterRouteWitnessCompileRequest(
                    canvas, canvas.CanonicalDigest, roleResult.Contract, roleResult.CanonicalDigest,
                    traversalResult.Compilation, traversalResult.CanonicalDigest,
                    CreateWitnessIntent(traversalResult.Compilation, reverse)));
            Assert.That(witnessResult.IsSuccess, Is.True, string.Join("\n", witnessResult.Errors));

            var catalog = BuildNoChangeCatalog();
            Assert.That(catalog.TryGetDefinition(
                new MicroPatternId("MP_MAP13_03_NO_CHANGE"), out var definition), Is.True);
            var render = TerrainClusterPatternRenderer.Render(new TerrainClusterPatternRenderRequest(
                canvas, canvas.CanonicalDigest,
                traversalResult.Compilation, traversalResult.CanonicalDigest,
                witnessResult.Report, witnessResult.CanonicalDigest,
                catalog, catalog.StableDigest,
                Array.Empty<TerrainClusterPatternZoneCell>(),
                new[]
                {
                    new TerrainClusterPatternPlacementIntent(
                        "TCP_MAP13_03_NO_CHANGE", definition.Id, MicroPatternTransform.R0,
                        new LocalTileCoord(0, 4), definition.ComputeStableDigest())
                }));
            Assert.That(render.Success, Is.True, string.Join("\n", render.Errors));
            var profile = new TerrainClusterQuietBufferProfile(
                "QBUF_MAP13_03", MoonpalaceBiomeId.MoonCrater,
                reverse
                    ? new[] { TerrainClusterQuietBufferUse.AfterLandmark, TerrainClusterQuietBufferUse.BeforeLandmark }
                    : new[] { TerrainClusterQuietBufferUse.BeforeLandmark, TerrainClusterQuietBufferUse.AfterLandmark },
                new[] { PacingRole.Quiet }, new[] { AccessClass.MandatoryNoTool },
                canvas, canvas.CanonicalDigest,
                roleResult.Contract, roleResult.CanonicalDigest,
                traversalResult.Compilation, traversalResult.CanonicalDigest,
                witnessResult.Report, witnessResult.CanonicalDigest,
                render.Report, render.CanonicalDigest);
            var pool = TerrainClusterQuietBufferPoolCompiler.Compile(
                new TerrainClusterQuietBufferPoolCompileRequest(new[] { profile }));
            Assert.That(pool.IsSuccess, Is.True, string.Join("\n", pool.Errors));
            return pool.Candidates.Single();
        }

        private static TerrainClusterContract CreateClusterContract(bool reverse)
        {
            var roles = new[]
            {
                new ClusterRoleAnchor("ANCHOR_ENTRY", ClusterRoleKind.Entry,
                    new LocalTileCoord(0, 1), "NODE_ENTRY"),
                new ClusterRoleAnchor("ANCHOR_BUILD_UP", ClusterRoleKind.BuildUp,
                    new LocalTileCoord(4, 1), "NODE_BUILD_UP"),
                new ClusterRoleAnchor("ANCHOR_CORE", ClusterRoleKind.Core,
                    new LocalTileCoord(10, 1), "NODE_CORE"),
                new ClusterRoleAnchor("ANCHOR_RECOVERY", ClusterRoleKind.Recovery,
                    new LocalTileCoord(17, 1), "NODE_RECOVERY"),
                new ClusterRoleAnchor("ANCHOR_EXIT", ClusterRoleKind.Exit,
                    new LocalTileCoord(23, 1), "NODE_EXIT"),
            };
            var commonNodes = roles.Select(value => new TraversalNode(
                value.TraversalNodeId, value.Tile, true, value.AnchorId)).Concat(new[]
            {
                new TraversalNode("NODE_STEP_A", new LocalTileCoord(7, 1), true, string.Empty),
                new TraversalNode("NODE_STEP_B", new LocalTileCoord(6, 1), false, string.Empty),
            }).ToArray();
            var alternateNodes = commonNodes.Concat(new[]
            {
                new TraversalNode("NODE_HIGH", new LocalTileCoord(7, 3), false, string.Empty),
                new TraversalNode("NODE_HIGH_END", new LocalTileCoord(9, 3), false, string.Empty),
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
                    reverse ? commonNodes.Reverse() : commonNodes,
                    reverse ? baselineEdges.Reverse() : baselineEdges),
                new SpineVariant(new SpineVariantId("SPINE_ALTERNATE"), false, TraversalGraphKind.Traversal,
                    reverse ? alternateNodes.Reverse() : alternateNodes,
                    reverse ? alternateEdges.Reverse() : alternateEdges),
            };
            var ports = new[]
            {
                new ClusterPort("PORT_ENTRY", ClusterPortKind.Entry, true, "ANCHOR_ENTRY",
                    new LocalTileCoord(0, 1), ClusterPortSide.L, new[] { 0, 1, 2, 3, 4 }),
                new ClusterPort("PORT_EXIT", ClusterPortKind.Exit, true, "ANCHOR_EXIT",
                    new LocalTileCoord(23, 1), ClusterPortSide.R, new[] { 1, 2, 3, 4 }),
            };
            return new TerrainClusterContract(
                new TerrainClusterId("TC_MAP13_03"),
                new ClusterFootprint(new[] { new ClusterChunkCoord(0, 0), new ClusterChunkCoord(1, 0) }),
                reverse ? roles.Reverse() : roles,
                reverse ? ports.Reverse() : ports,
                new TerrainClusterTraversalContract(reverse ? variants.Reverse() : variants),
                "MAP13_03");
        }

        private static TraversalEdge CreateEdge(
            string id, TraversalNode from, TraversalNode to, bool mandatory)
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
            TraversalEdge edge, IDictionary<string, TraversalNode> nodes)
            => CreateEdge(edge.EdgeId, nodes[edge.FromNodeId], nodes[edge.ToNodeId], edge.IsMandatory);

        private static TerrainClusterRouteWitnessIntent CreateWitnessIntent(
            TerrainClusterTraversalCompilation traversal, bool reverse)
        {
            var high = new TerrainClusterHighRouteDefinition(
                "HIGH_ROUTE_ONE", new SpineVariantId("SPINE_ALTERNATE"), "NODE_BUILD_UP",
                new[] { "EDGE_HIGH_01", "EDGE_HIGH_02", "EDGE_HIGH_03" },
                "NODE_CORE", "NODE_HIGH",
                reverse ? new[] { "BENEFIT_REWARD_ACCESS", "BENEFIT_HEIGHT_ADVANTAGE" } :
                    new[] { "BENEFIT_HEIGHT_ADVANTAGE", "BENEFIT_REWARD_ACCESS" },
                new[] { "NODE_HIGH" });
            var durations = traversal.Edges.Select(value => new TraversalEdgeDurationEvidence(
                value.VariantId, value.EdgeId, value.EdgeId == "EDGE_RECOVER" ? 2000 : 3000,
                "RULESET_ROUTE_V1"));
            return new TerrainClusterRouteWitnessIntent(
                new SpineVariantId("SPINE_BASELINE"), new[] { high },
                reverse ? durations.Reverse() : durations);
        }

        private static ClusterSectorSocketEvidence[] SocketEvidence()
            => new[]
            {
                new ClusterSectorSocketEvidence(
                    "SR_EXIT", "SOCKET_EXIT", ClusterPortSide.R, 3, true, ClusterPortKind.Exit),
                new ClusterSectorSocketEvidence(
                    "SR_ENTRY", "SOCKET_ENTRY", ClusterPortSide.L, 2, true, ClusterPortKind.Entry),
            };

        private static MicroPatternAuthoringCatalog BuildNoChangeCatalog()
        {
            var catalog = new[]
            {
                new MicroPatternCatalogRowV2(
                    "MP_MAP13_03_NO_CHANGE", "1", "MoonCrater", "R0", "FORCE_NO_CHANGE",
                    "catalog.csv", 2),
            };
            var cells = Enumerable.Range(0, 4).SelectMany(y => Enumerable.Range(0, 4)
                .Select((x, index) => new MicroPatternCellRowV2(
                    "MP_MAP13_03_NO_CHANGE", x.ToString(CultureInfo.InvariantCulture),
                    y.ToString(CultureInfo.InvariantCulture), "NO_CHANGE", "GEOMETRY", string.Empty,
                    "cells.csv", y * 4 + index + 2))).ToArray();
            var result = new MicroPatternCellSchemaBuilder().Build(catalog, cells);
            Assert.That(result.Success, Is.True, string.Join("\n", result.Errors));
            return result.Catalog;
        }

        private static IEnumerable<SpecialRegionTileCoordinate> Rectangle(
            int sectorX, int sectorY, int minimumX, int minimumY, int width, int height)
        {
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                yield return new SpecialRegionTileCoordinate(
                    new SectorCoord(sectorX, sectorY),
                    new LocalTileCoord(minimumX + x, minimumY + y));
        }

        private static void AssertLayerSuccess(SpecialRegionFixedSlotLayerResult result)
        {
            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Plan, Is.Not.Null);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
        }

        private static void AssertLayerFailure(
            SpecialRegionFixedSlotLayerResult result,
            SpecialRegionFixedSlotLayerErrorCode code)
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(code),
                string.Join("\n", result.Errors));
            Assert.That(result.Errors, Is.Ordered);
            Assert.That(result.Errors.Distinct().Count(), Is.EqualTo(result.Errors.Count));
        }

        private static void AssertSafetySuccess(SpecialRegionPersistenceSafetyResult result)
        {
            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.AggregateSafetyPublished, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
        }

        private static void AssertSafetyFailure(
            SpecialRegionPersistenceSafetyResult result,
            SpecialRegionPersistenceSafetyErrorCode code)
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.AggregateSafetyPublished, Is.False);
            Assert.That(result.Proofs, Is.Empty);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(code),
                string.Join("\n", result.Errors));
            Assert.That(result.Errors, Is.Ordered);
            Assert.That(result.Errors.Distinct().Count(), Is.EqualTo(result.Errors.Count));
        }

        private sealed class LayerFixture
        {
            public LayerFixture(
                SpecialRegionValidationResult validation,
                SpecialRegionSiteBridge bridge,
                SpecialRegionEntryBufferPlan entryPlan,
                SpecialRegionPlacementCollisionPlan collisionPlan,
                SiteEntryAnchor entryAnchor,
                SiteEntryAnchor returnAnchor,
                SpecialRegionEntryApron entryApron,
                SpecialRegionEntryApron returnApron,
                SpecialRegionQuietBufferPlacement before,
                SpecialRegionQuietBufferPlacement after)
            {
                Validation = validation;
                Bridge = bridge;
                EntryPlan = entryPlan;
                CollisionPlan = collisionPlan;
                EntryAnchor = entryAnchor;
                ReturnAnchor = returnAnchor;
                EntryApron = entryApron;
                ReturnApron = returnApron;
                Before = before;
                After = after;
            }

            public SpecialRegionValidationResult Validation { get; }
            public SpecialRegionSiteBridge Bridge { get; }
            public SpecialRegionEntryBufferPlan EntryPlan { get; }
            public SpecialRegionPlacementCollisionPlan CollisionPlan { get; }
            public SiteEntryAnchor EntryAnchor { get; }
            public SiteEntryAnchor ReturnAnchor { get; }
            public SpecialRegionEntryApron EntryApron { get; }
            public SpecialRegionEntryApron ReturnApron { get; }
            public SpecialRegionQuietBufferPlacement Before { get; }
            public SpecialRegionQuietBufferPlacement After { get; }
            public SpecialRegionFixedSlotLayerResult Layer { get; set; }
        }
    }
}
