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
    [Category("MAP13_02")]
    public sealed class SpecialRegionEntryBufferCollisionTests
    {
        private EntryFixture fixture;

        [OneTimeSetUp]
        public void SetUp()
        {
            fixture = BuildEntryFixture(false);
        }

        [Test]
        public void BridgePortsMatchExactMap03AnchorsAndExteriorCoordinates()
        {
            var entry = fixture.Bridge.PortBindings.Single(value => value.Kind == SpecialRegionSlotKind.Entry);
            var returned = fixture.Bridge.PortBindings.Single(value => value.Kind == SpecialRegionSlotKind.Return);

            Assert.That(entry.EntrySocketId, Is.EqualTo(fixture.EntryAnchor.EntrySocketId));
            Assert.That(entry.Placed.WorldSector, Is.EqualTo(fixture.EntryAnchor.FootprintSector));
            Assert.That(entry.Placed.Side, Is.EqualTo(fixture.EntryAnchor.Side));
            Assert.That(entry.AnchorExteriorSector, Is.EqualTo(new SectorCoord(4, 5)));
            Assert.That(returned.EntrySocketId, Is.EqualTo(fixture.ReturnAnchor.EntrySocketId));
            Assert.That(returned.Placed.WorldSector, Is.EqualTo(fixture.ReturnAnchor.FootprintSector));
            Assert.That(returned.Placed.Side, Is.EqualTo(fixture.ReturnAnchor.Side));
            Assert.That(returned.AnchorExteriorSector, Is.EqualTo(new SectorCoord(6, 5)));
        }

        [Test]
        public void CallerSuppliedApronsAreRectangularClearAndConnected()
        {
            var result = CompileEntry(fixture);

            AssertEntrySuccess(result);
            Assert.That(result.Plan.Aprons, Has.Count.EqualTo(2));
            Assert.That(result.Plan.Aprons.SelectMany(value => value.Cells).Distinct().Count(), Is.EqualTo(192));
            Assert.That(result.Plan.Aprons.Single(value => value.PortId == "SR_PORT_ENTRY").Width, Is.EqualTo(4));
            Assert.That(result.Plan.Aprons.Single(value => value.PortId == "SR_PORT_ENTRY").Height, Is.EqualTo(4));
            Assert.That(result.Plan.Aprons.Single(value => value.PortId == "SR_PORT_RETURN").Width, Is.EqualTo(44));
            Assert.That(result.Plan.Aprons.Single(value => value.PortId == "SR_PORT_RETURN").Height, Is.EqualTo(4));
            Assert.That(result.Plan.Aprons.Single(value => value.PortId == "SR_PORT_ENTRY").Cells,
                Does.Contain(Tile(5, 5, 0, 1)));
            Assert.That(result.Plan.Aprons.Single(value => value.PortId == "SR_PORT_RETURN").Cells,
                Does.Contain(Tile(5, 5, 47, 1)));
        }

        [Test]
        public void ExactTwoActiveChunksArePreservedBeforeAndAfterWithoutSelection()
        {
            var result = CompileEntry(fixture);

            AssertEntrySuccess(result);
            Assert.That(result.Plan.QuietChunks, Has.Count.EqualTo(4));
            Assert.That(result.Plan.QuietChunks.Count(value => value.Role == SpecialRegionQuietChunkRole.Before),
                Is.EqualTo(2));
            Assert.That(result.Plan.QuietChunks.Count(value => value.Role == SpecialRegionQuietChunkRole.After),
                Is.EqualTo(2));
            Assert.That(result.Plan.QuietChunks.All(value => value.SolidCount > 0 && value.AirCount > 0 &&
                value.BaselineCoordinateCount > 0), Is.True);
            Assert.That(result.Plan.SelectedCandidateCount, Is.Zero);
            Assert.That(result.Plan.PlacementWriteCount, Is.Zero);
        }

        [Test]
        public void StaticBidirectionalWitnessHasNoSyntheticRuntimeClaims()
        {
            var result = CompileEntry(fixture);

            AssertEntrySuccess(result);
            Assert.That(result.Plan.Witness.ForwardSegments,
                Is.EqualTo(new[] { "BeforeQuiet", "EntrySocket", "EntryApron", "RegionInterior" }));
            Assert.That(result.Plan.Witness.ReturnSegments,
                Is.EqualTo(new[] { "RegionInterior", "ReturnApron", "ReturnSocket", "AfterQuiet" }));
            Assert.That(result.Plan.Witness.IsBidirectional, Is.True);
            Assert.That(result.Plan.Witness.SyntheticEdgeCount, Is.Zero);
            Assert.That(result.Plan.Witness.TeleportCount, Is.Zero);
            Assert.That(result.Plan.Witness.CarveCount, Is.Zero);
            Assert.That(result.Plan.Witness.ToolRequirementCount, Is.Zero);
            Assert.That(result.Plan.Witness.OneWayEdgeCount, Is.Zero);
            Assert.That(result.Plan.Witness.ClaimsRuntimePhysics, Is.False);
        }

        [Test]
        public void SevenLevelPriorityMatrixAlwaysAcceptsHigherAndRejectsLower()
        {
            var kinds = new[]
            {
                SpecialRegionPlacementOwnerKind.Boss,
                SpecialRegionPlacementOwnerKind.Forge,
                SpecialRegionPlacementOwnerKind.CoreResource,
                SpecialRegionPlacementOwnerKind.Village,
                SpecialRegionPlacementOwnerKind.RareRegion,
                SpecialRegionPlacementOwnerKind.TerrainCluster,
                SpecialRegionPlacementOwnerKind.ActivityStructure,
            };
            Assert.That(kinds.Select(SpecialRegionPlacementCollisionCompiler.GetPriority),
                Is.EqualTo(new[] { 700, 600, 500, 400, 300, 200, 100 }));

            var cases = 0;
            for (var higherIndex = 0; higherIndex < kinds.Length; higherIndex++)
            for (var lowerIndex = higherIndex + 1; lowerIndex < kinds.Length; lowerIndex++)
            {
                var higher = Claim("owner.higher." + higherIndex, kinds[higherIndex], Tile(10, 10, 2, 2));
                var lower = Claim("owner.lower." + lowerIndex, kinds[lowerIndex], Tile(10, 10, 2, 2));
                var result = CompileCollision(lower, higher);

                AssertCollisionSuccess(result);
                Assert.That(result.Plan.Decisions.Single().Kind,
                    Is.EqualTo(SpecialRegionCollisionKind.HigherPriorityWins));
                Assert.That(result.Plan.Decisions.Single().WinnerOwnerId, Is.EqualTo(higher.OwnerId));
                Assert.That(result.Plan.Decisions.Single().LoserOwnerId, Is.EqualTo(lower.OwnerId));
                Assert.That(result.Plan.AcceptedOwnerIds, Does.Contain(higher.OwnerId));
                Assert.That(result.Plan.RejectedOwnerIds, Does.Contain(lower.OwnerId));
                cases++;
            }
            Assert.That(cases, Is.EqualTo(21));
        }

        [Test]
        public void HardProtectedCollisionIsAtomicForEveryOwnerKind()
        {
            var hard = Claim("reservation.footprint", SpecialRegionPlacementOwnerKind.Boss,
                Tile(4, 4, 0, 0), true);
            var activity = Claim("activity.structure", SpecialRegionPlacementOwnerKind.ActivityStructure,
                Tile(4, 4, 0, 0));

            AssertCollisionFailure(CompileCollision(hard, activity),
                SpecialRegionPlacementCollisionErrorCode.HardProtectedCollision);
        }

        [Test]
        public void SamePriorityDifferentOwnersAreAmbiguousAndAtomic()
        {
            var left = Claim("rare.left", SpecialRegionPlacementOwnerKind.RareRegion, Tile(3, 3, 1, 1));
            var right = Claim("rare.right", SpecialRegionPlacementOwnerKind.RareRegion, Tile(3, 3, 1, 1));

            AssertCollisionFailure(CompileCollision(left, right),
                SpecialRegionPlacementCollisionErrorCode.AmbiguousSamePriority);
        }

        [Test]
        public void HigherPriorityAgainstCommittedLowerRequiresReplanWithoutDeletion()
        {
            var committed = Claim("activity.committed", SpecialRegionPlacementOwnerKind.ActivityStructure,
                Tile(3, 3, 1, 1), false, true);
            var boss = Claim("boss.new", SpecialRegionPlacementOwnerKind.Boss, Tile(3, 3, 1, 1));

            var result = CompileCollision(committed, boss);

            AssertCollisionSuccess(result);
            Assert.That(result.Plan.Decisions.Single().Kind, Is.EqualTo(SpecialRegionCollisionKind.RequiresReplan));
            Assert.That(result.Plan.RequiresReplanOwnerIds, Does.Contain(boss.OwnerId));
            Assert.That(result.Plan.AcceptedOwnerIds, Does.Contain(committed.OwnerId));
            Assert.That(result.Plan.RejectedOwnerIds, Is.Empty);
            Assert.That(result.Plan.RemovedPayloadCount, Is.Zero);
        }

        [Test]
        public void NonOverlappingClaimsRemainAcceptedWithoutGlobalLayerReorder()
        {
            var terrain = Claim("terrain.cluster", SpecialRegionPlacementOwnerKind.TerrainCluster,
                Tile(2, 2, 0, 0));
            var activity = Claim("activity.structure", SpecialRegionPlacementOwnerKind.ActivityStructure,
                Tile(2, 2, 1, 0));

            var result = CompileCollision(terrain, activity);

            AssertCollisionSuccess(result);
            Assert.That(result.Plan.Decisions.Single().Kind, Is.EqualTo(SpecialRegionCollisionKind.NoOverlap));
            Assert.That(result.Plan.AcceptedOwnerIds, Is.EquivalentTo(new[] { terrain.OwnerId, activity.OwnerId }));
            Assert.That(result.Plan.RejectedOwnerIds, Is.Empty);
            Assert.That(result.Plan.GlobalLayerReorderCount, Is.Zero);
        }

        [Test]
        public void ReverseRepeatCallerMutationAndTurkishCultureAreCanonical()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var cells = new List<SpecialRegionTileCoordinate>
                {
                    Tile(8, 8, 2, 1),
                    Tile(8, 8, 1, 1),
                };
                var boss = Claim("boss.owner", SpecialRegionPlacementOwnerKind.Boss, cells.ToArray());
                var activity = Claim("activity.owner", SpecialRegionPlacementOwnerKind.ActivityStructure,
                    cells.AsEnumerable().Reverse().ToArray());
                var first = CompileCollision(boss, activity);
                cells.Clear();
                var repeat = CompileCollision(boss, activity);
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var reverse = CompileCollision(activity, boss);

                AssertCollisionSuccess(first);
                Assert.That(repeat.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));
                Assert.That(reverse.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));

                var reverseFixture = BuildEntryFixture(true);
                Assert.That(CompileEntry(reverseFixture).CanonicalDigest, Is.EqualTo(CompileEntry(fixture).CanonicalDigest));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Test]
        public void InvalidApronQuietOverlapClaimsAndMissingInputsAreAtomic()
        {
            var invalidApron = new SpecialRegionEntryApron(
                "SR_PORT_ENTRY", new SectorCoord(5, 5), new LocalTileCoord(0, 0), 3, 4,
                Rectangle(5, 5, 0, 0, 3, 4));
            var invalidEntry = SpecialRegionEntryBufferCompiler.Compile(new SpecialRegionEntryBufferCompileRequest(
                fixture.Bridge, fixture.Bridge.CanonicalDigest,
                "SR_PORT_ENTRY", fixture.EntryAnchor, invalidApron,
                "SR_PORT_RETURN", fixture.ReturnAnchor, fixture.ReturnApron,
                fixture.Before, fixture.After));
            AssertEntryFailure(invalidEntry, SpecialRegionEntryBufferErrorCode.InvalidApron);

            var optionalBridge = BuildBridge(false, AccessClass.OptionalNoTool);
            var invalidAccess = SpecialRegionEntryBufferCompiler.Compile(new SpecialRegionEntryBufferCompileRequest(
                optionalBridge.Bridge, optionalBridge.Bridge.CanonicalDigest,
                "SR_PORT_ENTRY", optionalBridge.EntryAnchor, fixture.EntryApron,
                "SR_PORT_RETURN", optionalBridge.ReturnAnchor, fixture.ReturnApron,
                fixture.Before, fixture.After));
            AssertEntryFailure(invalidAccess, SpecialRegionEntryBufferErrorCode.InvalidMandatoryAccess);

            var beforeOnlyCandidate = BuildQuietCandidate(false, false);
            var unsupportedAfter = new SpecialRegionQuietBufferPlacement(
                "placement.after.unsupported", SpecialRegionQuietChunkRole.After, beforeOnlyCandidate,
                fixture.After.Chunks);
            var invalidCandidate = SpecialRegionEntryBufferCompiler.Compile(new SpecialRegionEntryBufferCompileRequest(
                fixture.Bridge, fixture.Bridge.CanonicalDigest,
                "SR_PORT_ENTRY", fixture.EntryAnchor, fixture.EntryApron,
                "SR_PORT_RETURN", fixture.ReturnAnchor, fixture.ReturnApron,
                fixture.Before, unsupportedAfter));
            AssertEntryFailure(invalidCandidate, SpecialRegionEntryBufferErrorCode.InvalidQuietCandidate);

            var oneChunkBefore = new SpecialRegionQuietBufferPlacement(
                "placement.before.one", SpecialRegionQuietChunkRole.Before, fixture.Candidate,
                fixture.Before.Chunks.Take(1));
            var invalidChunk = SpecialRegionEntryBufferCompiler.Compile(new SpecialRegionEntryBufferCompileRequest(
                fixture.Bridge, fixture.Bridge.CanonicalDigest,
                "SR_PORT_ENTRY", fixture.EntryAnchor, fixture.EntryApron,
                "SR_PORT_RETURN", fixture.ReturnAnchor, fixture.ReturnApron,
                oneChunkBefore, fixture.After));
            AssertEntryFailure(invalidChunk, SpecialRegionEntryBufferErrorCode.QuietChunkMismatch);

            var routeFourCandidate = BuildQuietCandidate(false, true, 4);
            var routeBefore = new SpecialRegionQuietBufferPlacement(
                "placement.before.route4", SpecialRegionQuietChunkRole.Before, routeFourCandidate,
                fixture.Before.Chunks);
            var routeAfter = new SpecialRegionQuietBufferPlacement(
                "placement.after.route4", SpecialRegionQuietChunkRole.After, routeFourCandidate,
                fixture.After.Chunks);
            var invalidRoute = SpecialRegionEntryBufferCompiler.Compile(new SpecialRegionEntryBufferCompileRequest(
                fixture.Bridge, fixture.Bridge.CanonicalDigest,
                "SR_PORT_ENTRY", fixture.EntryAnchor, fixture.EntryApron,
                "SR_PORT_RETURN", fixture.ReturnAnchor, fixture.ReturnApron,
                routeBefore, routeAfter));
            AssertEntryFailure(invalidRoute, SpecialRegionEntryBufferErrorCode.MissingBidirectionalWitness);

            var overlappingAfter = new SpecialRegionQuietBufferPlacement(
                "placement.after.overlap", SpecialRegionQuietChunkRole.After, fixture.Candidate,
                fixture.Before.Chunks);
            var overlap = SpecialRegionEntryBufferCompiler.Compile(new SpecialRegionEntryBufferCompileRequest(
                fixture.Bridge, fixture.Bridge.CanonicalDigest,
                "SR_PORT_ENTRY", fixture.EntryAnchor, fixture.EntryApron,
                "SR_PORT_RETURN", fixture.ReturnAnchor, fixture.ReturnApron,
                fixture.Before, overlappingAfter));
            AssertEntryFailure(overlap, SpecialRegionEntryBufferErrorCode.BufferOverlap);

            AssertEntryFailure(SpecialRegionEntryBufferCompiler.Compile(null),
                SpecialRegionEntryBufferErrorCode.MissingInput);
            AssertCollisionFailure(SpecialRegionPlacementCollisionCompiler.Compile(null),
                SpecialRegionPlacementCollisionErrorCode.MissingInput);
            AssertCollisionFailure(CompileCollision(new SpecialRegionOccupancyClaim(
                    "bad.claim", SpecialRegionPlacementOwnerKind.Village,
                    new[] { Tile(1, 1, 0, 0), Tile(1, 1, 0, 0) })),
                SpecialRegionPlacementCollisionErrorCode.InvalidClaim);
        }

        [Test]
        public void RuntimeSourcesContainNoSelectionRngFilesystemOrUnityLifecycleAuthority()
        {
            var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", ".."));
            var sources = new[]
            {
                "Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionEntryBuffer.cs",
                "Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionPlacementCollision.cs",
            };
            var text = string.Join("\n", sources.Select(path => File.ReadAllText(Path.Combine(root, path))));
            foreach (var forbidden in new[]
                     {
                         "UnityEditor", "UnityEngine.Random", "System.Random", "DateTime", "Time.deltaTime",
                         "File.", "Directory.", "MonoBehaviour", "ScriptableObject", "TerrainClusterQuietBufferQuery",
                     })
                Assert.That(text, Does.Not.Contain(forbidden), forbidden);
        }

        private static EntryFixture BuildEntryFixture(bool reverse)
        {
            var bridgeFixture = BuildBridge(reverse);
            var candidate = BuildQuietCandidate(reverse);
            var entryApron = new SpecialRegionEntryApron(
                "SR_PORT_ENTRY", new SectorCoord(5, 5), new LocalTileCoord(0, 0), 4, 4,
                reverse ? Rectangle(5, 5, 0, 0, 4, 4).Reverse() : Rectangle(5, 5, 0, 0, 4, 4));
            var returnApron = new SpecialRegionEntryApron(
                "SR_PORT_RETURN", new SectorCoord(5, 5), new LocalTileCoord(4, 0), 44, 4,
                reverse ? Rectangle(5, 5, 4, 0, 44, 4).Reverse() : Rectangle(5, 5, 4, 0, 44, 4));
            var beforeChunks = new[]
            {
                new SpecialRegionQuietChunkPlacement(new ClusterChunkCoord(0, 0), new SectorCoord(4, 5), new ClusterChunkCoord(2, 0)),
                new SpecialRegionQuietChunkPlacement(new ClusterChunkCoord(1, 0), new SectorCoord(4, 5), new ClusterChunkCoord(3, 0)),
            };
            var afterChunks = new[]
            {
                new SpecialRegionQuietChunkPlacement(new ClusterChunkCoord(0, 0), new SectorCoord(6, 5), new ClusterChunkCoord(0, 0)),
                new SpecialRegionQuietChunkPlacement(new ClusterChunkCoord(1, 0), new SectorCoord(6, 5), new ClusterChunkCoord(1, 0)),
            };
            var before = new SpecialRegionQuietBufferPlacement(
                "placement.before", SpecialRegionQuietChunkRole.Before, candidate,
                reverse ? beforeChunks.Reverse() : beforeChunks);
            var after = new SpecialRegionQuietBufferPlacement(
                "placement.after", SpecialRegionQuietChunkRole.After, candidate,
                reverse ? afterChunks.Reverse() : afterChunks);
            return new EntryFixture(
                bridgeFixture.Bridge, bridgeFixture.EntryAnchor, bridgeFixture.ReturnAnchor,
                candidate, entryApron, returnApron, before, after);
        }

        private static SpecialRegionEntryBufferResult CompileEntry(EntryFixture value)
            => SpecialRegionEntryBufferCompiler.Compile(new SpecialRegionEntryBufferCompileRequest(
                value.Bridge, value.Bridge.CanonicalDigest,
                "SR_PORT_ENTRY", value.EntryAnchor, value.EntryApron,
                "SR_PORT_RETURN", value.ReturnAnchor, value.ReturnApron,
                value.Before, value.After));

        private static BridgeFixture BuildBridge(
            bool reverse,
            AccessClass portAccess = AccessClass.MandatoryNoTool)
        {
            var reservationId = new SiteReservationId("RES_SPECIAL_SITE");
            var regionId = new SpecialRegionId("SR_SITE_ENTRY_BUFFER");
            var origin = new SectorCoord(5, 5);
            var entryAnchor = new SiteEntryAnchor(
                reservationId, "ENTRY_MAIN", origin, SiteEntrySide.L, new[] { 1, 2, 3 }, true, false);
            var returnAnchor = new SiteEntryAnchor(
                reservationId, "RETURN_MAIN", origin, SiteEntrySide.R, new[] { 1, 2, 3 }, true, true);
            var footprintCells = new[]
            {
                new SiteFootprintCell(0, 0, "SPECIAL", string.Empty, string.Empty,
                    new[] { SiteEntrySide.L, SiteEntrySide.R })
            };
            var anchors = reverse
                ? new[] { returnAnchor, entryAnchor }
                : new[] { entryAnchor, returnAnchor };
            var reservation = new SiteReservation(
                reservationId, SiteReservationKind.Village, "SPECIAL_SITE", origin,
                new SiteFootprint(1, 1, SiteFootprintTransform.R0, footprintCells),
                string.Empty, 1, anchors);

            var entryId = new SpecialRegionSlotId("SR_SLOT_ENTRY");
            var returnId = new SpecialRegionSlotId("SR_SLOT_RETURN");
            var rewardId = new SpecialRegionSlotId("SR_SLOT_REWARD");
            var rewardKey = SpecialPersistenceKey.ForSlot(regionId, SpecialPersistenceScope.Reward, rewardId);
            var slots = new[]
            {
                new SpecialRegionSlot(entryId, SpecialRegionSlotKind.Entry,
                    new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(0, 1), true,
                    default(SpecialPersistenceScope), default(SpecialPersistenceKey)),
                new SpecialRegionSlot(returnId, SpecialRegionSlotKind.Return,
                    new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(47, 1), true,
                    default(SpecialPersistenceScope), default(SpecialPersistenceKey)),
                new SpecialRegionSlot(rewardId, SpecialRegionSlotKind.Reward,
                    new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(24, 10), true,
                    SpecialPersistenceScope.Reward, rewardKey),
            };
            var ports = new[]
            {
                new SpecialRegionPort("SR_PORT_ENTRY", entryId, SpecialRegionSlotKind.Entry,
                    new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(0, 1),
                    SiteEntrySide.L, portAccess),
                new SpecialRegionPort("SR_PORT_RETURN", returnId, SpecialRegionSlotKind.Return,
                    new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(47, 1),
                    SiteEntrySide.R, portAccess),
            };
            var persistence = new[]
            {
                new SpecialPersistenceBinding(SpecialPersistenceKey.ForRegion(regionId),
                    SpecialPersistenceScope.Region, default(SpecialRegionSlotId), "INITIAL_UNCLAIMED"),
                new SpecialPersistenceBinding(rewardKey,
                    SpecialPersistenceScope.Reward, rewardId, "INITIAL_AVAILABLE"),
            };
            var contract = new SpecialRegionContract(
                regionId, SpecialRegionKind.Village, reservationId,
                new SpecialRegionFootprint(new[] { new SpecialRegionSectorOffset(0, 0) }),
                new[]
                {
                    new SpecialRegionFixedShellCell(
                        new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(20, 20), "SHELL_WALL")
                },
                reverse ? slots.Reverse() : slots,
                reverse ? ports.Reverse() : ports,
                reverse ? persistence.Reverse() : persistence,
                "MAP13_02 fixture");
            var validation = SpecialRegionValidator.Validate(contract, reservation);
            Assert.That(validation.IsValid, Is.True, string.Join("\n", validation.Errors));
            var snapshot = BuildSnapshot(reservation, reverse);
            var result = SpecialRegionSiteBridgeCompiler.Compile(snapshot, validation);
            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            return new BridgeFixture(result.Bridge, entryAnchor, returnAnchor);
        }

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
            return new SiteReservationSnapshot(1302UL, reservations, rows, Array.Empty<CoreBiomeSeed>());
        }

        private static TerrainClusterQuietBufferCandidate BuildQuietCandidate(
            bool reverse,
            bool supportsAfter = true,
            int exclusiveRouteType = 0)
        {
            var contract = CreateClusterContract(reverse, exclusiveRouteType);
            var validation = TerrainClusterContractValidator.Validate(contract);
            Assert.That(validation.IsValid, Is.True, string.Join("\n", validation.Errors));
            var canvasResult = TerrainClusterFootprintCompiler.Compile(
                new TerrainClusterFootprintCompileRequest(contract, ClusterFootprintTransform.R0));
            Assert.That(canvasResult.IsSuccess, Is.True, string.Join("\n", canvasResult.Errors));
            var canvas = canvasResult.LocalCanvas;
            var roleResult = TerrainClusterRoleSocketCompiler.Compile(
                new TerrainClusterRoleSocketCompileRequest(
                    contract, validation.CanonicalDigest, canvas, canvas.CanonicalDigest,
                    SocketEvidence(exclusiveRouteType)));
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
            Assert.That(catalog.TryGetDefinition(new MicroPatternId("MP_MAP13_02_NO_CHANGE"), out var definition), Is.True);
            var render = TerrainClusterPatternRenderer.Render(new TerrainClusterPatternRenderRequest(
                canvas, canvas.CanonicalDigest,
                traversalResult.Compilation, traversalResult.CanonicalDigest,
                witnessResult.Report, witnessResult.CanonicalDigest,
                catalog, catalog.StableDigest,
                Array.Empty<TerrainClusterPatternZoneCell>(),
                new[]
                {
                    new TerrainClusterPatternPlacementIntent(
                        "TCP_MAP13_02_NO_CHANGE", definition.Id, MicroPatternTransform.R0,
                        new LocalTileCoord(0, 4), definition.ComputeStableDigest())
                }));
            Assert.That(render.Success, Is.True, string.Join("\n", render.Errors));
            var profile = new TerrainClusterQuietBufferProfile(
                "QBUF_MAP13_02", MoonpalaceBiomeId.MoonCrater,
                supportsAfter
                    ? (reverse
                        ? new[] { TerrainClusterQuietBufferUse.AfterLandmark, TerrainClusterQuietBufferUse.BeforeLandmark }
                        : new[] { TerrainClusterQuietBufferUse.BeforeLandmark, TerrainClusterQuietBufferUse.AfterLandmark })
                    : new[] { TerrainClusterQuietBufferUse.BeforeLandmark },
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

        private static TerrainClusterContract CreateClusterContract(bool reverse, int exclusiveRouteType)
        {
            var roles = new[]
            {
                new ClusterRoleAnchor("ANCHOR_ENTRY", ClusterRoleKind.Entry, new LocalTileCoord(0, 1), "NODE_ENTRY"),
                new ClusterRoleAnchor("ANCHOR_BUILD_UP", ClusterRoleKind.BuildUp, new LocalTileCoord(4, 1), "NODE_BUILD_UP"),
                new ClusterRoleAnchor("ANCHOR_CORE", ClusterRoleKind.Core, new LocalTileCoord(10, 1), "NODE_CORE"),
                new ClusterRoleAnchor("ANCHOR_RECOVERY", ClusterRoleKind.Recovery, new LocalTileCoord(17, 1), "NODE_RECOVERY"),
                new ClusterRoleAnchor("ANCHOR_EXIT", ClusterRoleKind.Exit, new LocalTileCoord(23, 1), "NODE_EXIT"),
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
                    new LocalTileCoord(0, 1), ClusterPortSide.L,
                    exclusiveRouteType == 0 ? new[] { 0, 1, 2, 3, 4 } : new[] { exclusiveRouteType }),
                new ClusterPort("PORT_EXIT", ClusterPortKind.Exit, true, "ANCHOR_EXIT",
                    new LocalTileCoord(23, 1), ClusterPortSide.R,
                    exclusiveRouteType == 0 ? new[] { 1, 2, 3, 4 } : new[] { exclusiveRouteType }),
            };
            return new TerrainClusterContract(
                new TerrainClusterId("TC_MAP13_02"),
                new ClusterFootprint(new[] { new ClusterChunkCoord(0, 0), new ClusterChunkCoord(1, 0) }),
                reverse ? roles.Reverse() : roles,
                reverse ? ports.Reverse() : ports,
                new TerrainClusterTraversalContract(reverse ? variants.Reverse() : variants),
                "MAP13_02");
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

        private static ClusterSectorSocketEvidence[] SocketEvidence(int exclusiveRouteType)
            => new[]
            {
                new ClusterSectorSocketEvidence(
                    "SR_EXIT", "SOCKET_EXIT", ClusterPortSide.R,
                    exclusiveRouteType == 0 ? 3 : exclusiveRouteType, true, ClusterPortKind.Exit),
                new ClusterSectorSocketEvidence(
                    "SR_ENTRY", "SOCKET_ENTRY", ClusterPortSide.L,
                    exclusiveRouteType == 0 ? 2 : exclusiveRouteType, true, ClusterPortKind.Entry),
            };

        private static MicroPatternAuthoringCatalog BuildNoChangeCatalog()
        {
            var catalog = new[]
            {
                new MicroPatternCatalogRowV2(
                    "MP_MAP13_02_NO_CHANGE", "1", "MoonCrater", "R0", "FORCE_NO_CHANGE",
                    "catalog.csv", 2),
            };
            var cells = Enumerable.Range(0, 4).SelectMany(y => Enumerable.Range(0, 4)
                .Select((x, index) => new MicroPatternCellRowV2(
                    "MP_MAP13_02_NO_CHANGE", x.ToString(CultureInfo.InvariantCulture),
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
                yield return Tile(sectorX, sectorY, minimumX + x, minimumY + y);
        }

        private static SpecialRegionTileCoordinate Tile(
            int sectorX, int sectorY, int tileX, int tileY)
            => new SpecialRegionTileCoordinate(
                new SectorCoord(sectorX, sectorY), new LocalTileCoord(tileX, tileY));

        private static SpecialRegionOccupancyClaim Claim(
            string id,
            SpecialRegionPlacementOwnerKind kind,
            SpecialRegionTileCoordinate cell,
            bool hard = false,
            bool committed = false)
            => new SpecialRegionOccupancyClaim(id, kind, new[] { cell }, hard, committed);

        private static SpecialRegionOccupancyClaim Claim(
            string id,
            SpecialRegionPlacementOwnerKind kind,
            SpecialRegionTileCoordinate[] cells,
            bool hard = false,
            bool committed = false)
            => new SpecialRegionOccupancyClaim(id, kind, cells, hard, committed);

        private static SpecialRegionPlacementCollisionResult CompileCollision(
            params SpecialRegionOccupancyClaim[] claims)
            => SpecialRegionPlacementCollisionCompiler.Compile(
                new SpecialRegionPlacementCollisionCompileRequest(claims));

        private static void AssertEntrySuccess(SpecialRegionEntryBufferResult result)
        {
            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Plan, Is.Not.Null);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
        }

        private static void AssertEntryFailure(
            SpecialRegionEntryBufferResult result, SpecialRegionEntryBufferErrorCode code)
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(code), string.Join("\n", result.Errors));
            Assert.That(result.Errors, Is.Ordered);
            Assert.That(result.Errors.Distinct().Count(), Is.EqualTo(result.Errors.Count));
        }

        private static void AssertCollisionSuccess(SpecialRegionPlacementCollisionResult result)
        {
            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Plan, Is.Not.Null);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
        }

        private static void AssertCollisionFailure(
            SpecialRegionPlacementCollisionResult result, SpecialRegionPlacementCollisionErrorCode code)
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(code), string.Join("\n", result.Errors));
            Assert.That(result.Errors, Is.Ordered);
            Assert.That(result.Errors.Distinct().Count(), Is.EqualTo(result.Errors.Count));
        }

        private sealed class BridgeFixture
        {
            public BridgeFixture(SpecialRegionSiteBridge bridge, SiteEntryAnchor entryAnchor, SiteEntryAnchor returnAnchor)
            {
                Bridge = bridge;
                EntryAnchor = entryAnchor;
                ReturnAnchor = returnAnchor;
            }

            public SpecialRegionSiteBridge Bridge { get; }
            public SiteEntryAnchor EntryAnchor { get; }
            public SiteEntryAnchor ReturnAnchor { get; }
        }

        private sealed class EntryFixture
        {
            public EntryFixture(
                SpecialRegionSiteBridge bridge,
                SiteEntryAnchor entryAnchor,
                SiteEntryAnchor returnAnchor,
                TerrainClusterQuietBufferCandidate candidate,
                SpecialRegionEntryApron entryApron,
                SpecialRegionEntryApron returnApron,
                SpecialRegionQuietBufferPlacement before,
                SpecialRegionQuietBufferPlacement after)
            {
                Bridge = bridge;
                EntryAnchor = entryAnchor;
                ReturnAnchor = returnAnchor;
                Candidate = candidate;
                EntryApron = entryApron;
                ReturnApron = returnApron;
                Before = before;
                After = after;
            }

            public SpecialRegionSiteBridge Bridge { get; }
            public SiteEntryAnchor EntryAnchor { get; }
            public SiteEntryAnchor ReturnAnchor { get; }
            public TerrainClusterQuietBufferCandidate Candidate { get; }
            public SpecialRegionEntryApron EntryApron { get; }
            public SpecialRegionEntryApron ReturnApron { get; }
            public SpecialRegionQuietBufferPlacement Before { get; }
            public SpecialRegionQuietBufferPlacement After { get; }
        }
    }
}
