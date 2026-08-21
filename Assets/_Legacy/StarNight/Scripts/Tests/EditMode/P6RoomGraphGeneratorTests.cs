#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Generation.P6;
using StarNight.Rooms;
using UnityEditor;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace StarNight.Tests.EditMode
{
    public sealed class P6RoomGraphGeneratorTests
    {
        private const string MoonLibraryPath =
            "Assets/StarNight/Data/P4/P4_MoonRoomPrefabLibrary.asset";

        [Test]
        public void P4MoonLibrary_IsACompleteDeterministicP6InputCatalog()
        {
            RoomPrefabLibrary library =
                AssetDatabase.LoadAssetAtPath<RoomPrefabLibrary>(
                    MoonLibraryPath);
            Assert.That(
                library,
                Is.Not.Null,
                $"Missing P4 room library: {MoonLibraryPath}");
            Assert.That(library.Region, Is.EqualTo(RoomRegion.MoonPalace));
            Assert.That(library.RoomPrefabs.Count, Is.EqualTo(33));

            HashSet<string> roomIds =
                new HashSet<string>(StringComparer.Ordinal);
            int throughRoomCount = 0;
            int leafRoomCount = 0;

            for (int index = 0;
                 index < library.RoomPrefabs.Count;
                 index++)
            {
                GameObject prefab = library.RoomPrefabs[index];
                Assert.That(
                    prefab,
                    Is.Not.Null,
                    $"P4 library entry {index} is missing.");

                RoomTemplate2D template =
                    prefab.GetComponent<RoomTemplate2D>();
                Assert.That(
                    template,
                    Is.Not.Null,
                    $"{prefab.name} has no RoomTemplate2D.");
                Assert.That(
                    roomIds.Add(template.RoomId),
                    Is.True,
                    $"Duplicate generator prefab id: {template.RoomId}");
                Assert.That(template.Region, Is.EqualTo(RoomRegion.MoonPalace));
                Assert.That(
                    IsSupportedMacroSize(template.MacroSize),
                    Is.True,
                    $"{template.RoomId} has unsupported macro size "
                    + $"{template.MacroSize}.");
                Assert.That(
                    template.Sockets,
                    Is.Not.Null.And.Not.Empty,
                    $"{template.RoomId} has no authored socket.");

                P6RoomPrefabDescriptor generatedDescriptor =
                    P6RoomTraversalProofFactory.CreateDescriptor(template);
                P6RoomTraversalProof proof =
                    generatedDescriptor.TraversalProof;
                Assert.That(
                    generatedDescriptor.HasValidatedTraversalProof,
                    Is.True,
                    template.RoomId);
                Assert.That(
                    proof.Passed,
                    Is.True,
                    $"{template.RoomId} traversal proof failed: "
                    + string.Join(
                        Environment.NewLine,
                        proof.ValidationIssues));
                Assert.That(
                    proof.RoomPrefabValidationPassed,
                    Is.True,
                    template.RoomId);
                Assert.That(proof.ExitBlockProof, Is.True, template.RoomId);
                Assert.That(
                    proof.SocketAnchorsPassed,
                    Is.True,
                    template.RoomId);
                Assert.That(
                    proof.OneCellBodyClearancePassed,
                    Is.True,
                    template.RoomId);
                Assert.That(
                    proof.OneCellOpeningsPassed,
                    Is.True,
                    template.RoomId);
                Assert.That(
                    proof.JumpEnvelopePassed,
                    Is.True,
                    template.RoomId);
                Assert.That(
                    proof.MainSocketPairsReachable,
                    Is.True,
                    template.RoomId);
                Assert.That(
                    proof.OptionalReturnsReachable,
                    Is.True,
                    template.RoomId);
                Assert.That(
                    proof.Sockets.Count,
                    Is.EqualTo(template.Sockets.Count),
                    template.RoomId);

                bool isMaruStatue =
                    template.HasRole(RoomRole.MaruStatue);
                bool isLeaf =
                    template.HasRole(RoomRole.RecordRoom) || isMaruStatue;
                if (isMaruStatue)
                {
                    Assert.That(
                        template.Sockets.Count,
                        Is.InRange(1, 2),
                        $"{template.RoomId} may open one door per authored "
                        + "socket, up to two.");
                }
                else
                {
                    Assert.That(
                        template.Sockets.Count,
                        Is.EqualTo(isLeaf ? 1 : 2),
                        $"{template.RoomId} has the wrong P6 socket capacity.");
                }

                Assert.That(
                    proof.Links.Count,
                    Is.EqualTo(template.Sockets.Count == 1 ? 0 : 2),
                    $"{template.RoomId} directed main-pair proof count.");

                for (int socketIndex = 0;
                     socketIndex < template.Sockets.Count;
                     socketIndex++)
                {
                    RoomSocket2D socket = template.Sockets[socketIndex];
                    Assert.That(
                        socket,
                        Is.Not.Null,
                        $"{template.RoomId} socket {socketIndex} is missing.");
                    P6SocketTraversalProof socketProof =
                        proof.FindSocket(socket.SocketId);
                    Assert.That(
                        socket.MainRouteAllowed,
                        Is.True,
                        $"{template.RoomId}/{socket.SocketId} cannot be used "
                        + "on a tool-free main route.");
                    Assert.That(
                        socket.TraversalType,
                        Is.EqualTo(RoomTraversalType.Walk));
                    Assert.That(
                        socket.OpeningSize,
                        Is.EqualTo(Vector2Int.one),
                        $"{template.RoomId}/{socket.SocketId} is not a "
                        + "one-cell socket.");
                    Assert.That(
                        socketProof,
                        Is.Not.Null,
                        $"{template.RoomId}/{socket.SocketId}");
                    Assert.That(
                        socketProof.SocketId,
                        Is.EqualTo(socket.SocketId));
                    Assert.That(socketProof.Side, Is.EqualTo(socket.Side));
                    Assert.That(
                        socketProof.CellOffset,
                        Is.EqualTo(socket.CellOffset));
                    Assert.That(
                        socketProof.OpeningSize,
                        Is.EqualTo(socket.OpeningSize));
                    Assert.That(
                        socketProof.TraversalType,
                        Is.EqualTo(socket.TraversalType));
                    Assert.That(
                        socketProof.MainRouteAllowed,
                        Is.EqualTo(socket.MainRouteAllowed));
                    Assert.That(
                        socketProof.ValidationAnchor,
                        Is.EqualTo(socket.ValidationAnchor));
                    Assert.That(socketProof.OneCellOpening, Is.True);
                    Assert.That(socketProof.AnchorStandable, Is.True);
                    Assert.That(
                        socketProof.OneCellBodyClearance,
                        Is.True);
                }

                for (int linkIndex = 0;
                     linkIndex < proof.Links.Count;
                     linkIndex++)
                {
                    P6SocketLinkProof link = proof.Links[linkIndex];
                    Assert.That(
                        link.Kind,
                        Is.EqualTo(P6SocketLinkKind.MainRoutePair));
                    Assert.That(link.Passed, Is.True, template.RoomId);
                    Assert.That(link.Path, Is.Not.Empty, template.RoomId);
                    Assert.That(
                        link.Path[0],
                        Is.EqualTo(
                            proof.FindSocket(link.FromSocketId)
                                .ValidationAnchor));
                    Assert.That(
                        link.Path[link.Path.Count - 1],
                        Is.EqualTo(
                            proof.FindSocket(link.ToSocketId)
                                .ValidationAnchor));
                    Assert.That(
                        link.MaximumHorizontalStep,
                        Is.LessThanOrEqualTo(3));
                    Assert.That(link.MaximumRise, Is.LessThanOrEqualTo(2));
                    Assert.That(link.MaximumDrop, Is.LessThanOrEqualTo(12));
                }

                if (isLeaf)
                {
                    leafRoomCount++;
                }
                else
                {
                    throughRoomCount++;
                }
            }

            Assert.That(roomIds.Count, Is.EqualTo(33));
            Assert.That(throughRoomCount, Is.EqualTo(31));
            Assert.That(leafRoomCount, Is.EqualTo(2));
            Assert.That(
                library.GetTemplates().All(template => template != null),
                Is.True);
        }

        [Test]
        public void DeterministicRandom_ReplaysSequenceAndDerivesStableAttempts()
        {
            P6DeterministicRandom first =
                new P6DeterministicRandom(20260731);
            P6DeterministicRandom repeated =
                new P6DeterministicRandom(20260731);
            P6DeterministicRandom different =
                new P6DeterministicRandom(20260730);

            bool anyDifferent = false;
            for (int index = 0; index < 128; index++)
            {
                uint firstValue = first.NextUInt();
                uint repeatedValue = repeated.NextUInt();
                uint differentValue = different.NextUInt();
                Assert.That(
                    repeatedValue,
                    Is.EqualTo(firstValue),
                    $"Deterministic sequence diverged at draw {index}.");
                anyDifferent |= differentValue != firstValue;
            }

            Assert.That(
                anyDifferent,
                Is.True,
                "Different seeds unexpectedly produced the same sequence.");

            HashSet<int> derivedSeeds = new HashSet<int>();
            for (int attempt = 0; attempt < 96; attempt++)
            {
                int derived =
                    P6DeterministicRandom.DeriveSeed(20260731, attempt);
                Assert.That(
                    P6DeterministicRandom.DeriveSeed(20260731, attempt),
                    Is.EqualTo(derived));
                Assert.That(
                    derivedSeeds.Add(derived),
                    Is.True,
                    $"Attempt {attempt} reused derived seed {derived}.");
            }
        }

        [Test]
        public void MoonPalaceRequest_FixesCanvasRoomRangeLoopAndPrefabCap()
        {
            P6RoomPrefabDescriptor descriptor =
                new P6RoomPrefabDescriptor(
                    "TestRoom",
                    RoomArchetype.Standard,
                    RoomRole.Main,
                    RoomRegion.MoonPalace,
                    Vector2Int.one,
                    P6SocketSides.Left | P6SocketSides.Right,
                    true,
                    selectionWeight: 1,
                    maxUses: 999);
            P6GenerationRequest request =
                P6GenerationRequest.CreateMoonPalace(
                    73,
                    P6StageSlot.X2,
                    new[] { descriptor },
                    P6StageArchetype.Circuit);

            Assert.That(request.Seed, Is.EqualTo(73));
            Assert.That(request.StageSlot, Is.EqualTo(P6StageSlot.X2));
            Assert.That(
                request.ArchetypeOverride,
                Is.EqualTo(P6StageArchetype.Circuit));
            Assert.That(request.Region, Is.EqualTo(RoomRegion.MoonPalace));
            Assert.That(request.CanvasSize, Is.EqualTo(new Vector2Int(8, 6)));
            Assert.That(request.MinRooms, Is.EqualTo(9));
            Assert.That(request.MaxRooms, Is.EqualTo(14));
            Assert.That(request.MaxAttempts, Is.EqualTo(96));
            Assert.That(request.RequiresClosedLoop, Is.True);
            Assert.That(
                descriptor.MaxUses,
                Is.EqualTo(2),
                "The runtime descriptor must enforce the P6 max-two cap.");
        }

        [Test]
        public void MacroCostProfile_MatchesTheP6CorridorWeights()
        {
            P6MacroCostProfile costs = P6MacroCostProfile.Default;

            Assert.That(costs.ExistingCorridor, Is.EqualTo(1));
            Assert.That(costs.AdjacentToCorridor, Is.EqualTo(2));
            Assert.That(costs.Empty, Is.EqualTo(5));
            Assert.That(costs.RoomExterior, Is.EqualTo(20));
            Assert.That(costs.Forbidden, Is.EqualTo(int.MaxValue));
            Assert.That(
                costs.Cost(P6MacroCellKind.ExistingCorridor),
                Is.EqualTo(1));
            Assert.That(
                costs.Cost(P6MacroCellKind.AdjacentToCorridor),
                Is.EqualTo(2));
            Assert.That(
                costs.Cost(P6MacroCellKind.Empty),
                Is.EqualTo(5));
            Assert.That(
                costs.Cost(P6MacroCellKind.RoomExterior),
                Is.EqualTo(20));
            Assert.That(
                costs.Cost(P6MacroCellKind.Forbidden),
                Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void MacroAStar_UsesExactEndpointsReusesCorridorAndAvoidsForbidden()
        {
            RectInt bounds = new RectInt(0, 0, 7, 5);
            P6MacroRouteGrid grid = new P6MacroRouteGrid(bounds);
            Vector2Int start = new Vector2Int(0, 2);
            Vector2Int goal = new Vector2Int(6, 2);
            Vector2Int forbidden = new Vector2Int(3, 2);
            grid.MarkForbidden(forbidden);
            grid.AddCorridor(
                Enumerable.Range(1, 5)
                    .Select(x => new Vector2Int(x, 1)));

            P6MacroRouteResult first =
                P6MacroRouter.Route(grid, start, goal);
            P6MacroRouteResult repeated =
                P6MacroRouter.Route(grid, start, goal);

            Assert.That(first.Succeeded, Is.True);
            Assert.That(first.Cells, Is.Not.Empty);
            Assert.That(first.Cells[0], Is.EqualTo(start));
            Assert.That(
                first.Cells[first.Cells.Count - 1],
                Is.EqualTo(goal));
            CollectionAssert.DoesNotContain(
                first.Cells.ToArray(),
                forbidden);
            CollectionAssert.Contains(
                first.Cells.ToArray(),
                new Vector2Int(3, 1),
                "A* should prefer the authored low-cost corridor.");
            CollectionAssert.AreEqual(first.Cells, repeated.Cells);
            Assert.That(repeated.TotalCost, Is.EqualTo(first.TotalCost));

            int recomputedCost = 0;
            for (int index = 1; index < first.Cells.Count; index++)
            {
                Vector2Int previous = first.Cells[index - 1];
                Vector2Int current = first.Cells[index];
                int manhattan =
                    Mathf.Abs(current.x - previous.x)
                    + Mathf.Abs(current.y - previous.y);
                Assert.That(
                    manhattan,
                    Is.EqualTo(1),
                    $"A* path is not four-neighbour contiguous at {index}.");
                recomputedCost += current == goal
                    ? 1
                    : grid.GetTraversalCost(current);
            }

            Assert.That(first.TotalCost, Is.EqualTo(recomputedCost));

            P6MacroRouteGrid blocked =
                new P6MacroRouteGrid(bounds);
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                blocked.MarkForbidden(new Vector2Int(3, y));
            }

            P6MacroRouteResult noPath =
                P6MacroRouter.Route(blocked, start, goal);
            Assert.That(noPath.Succeeded, Is.False);
            Assert.That(noPath.Cells, Is.Empty);
        }

        [Test]
        public void MoonPalaceProfile_UsesTheAuthoredRoomWeights()
        {
            Assert.That(
                P6MoonPalaceProfile.CanvasSize,
                Is.EqualTo(new Vector2Int(8, 6)));
            Assert.That(
                P6MoonPalaceProfile.GetRoomWeight(RoomArchetype.Small),
                Is.EqualTo(25));
            Assert.That(
                P6MoonPalaceProfile.GetRoomWeight(RoomArchetype.Standard),
                Is.EqualTo(30));
            Assert.That(
                P6MoonPalaceProfile.GetRoomWeight(RoomArchetype.Wide),
                Is.EqualTo(20));
            Assert.That(
                P6MoonPalaceProfile.GetRoomWeight(RoomArchetype.Tall),
                Is.EqualTo(15));
            Assert.That(
                P6MoonPalaceProfile.GetRoomWeight(RoomArchetype.Large),
                Is.EqualTo(10));
        }

        [Test]
        public void EveryStageArchetype_ReplaysAnIdenticalAcceptedPlan()
        {
            P6RoomPrefabDescriptor[] prefabs = LoadMoonDescriptors();
            P6StageArchetype[] archetypes =
                (P6StageArchetype[])Enum.GetValues(
                    typeof(P6StageArchetype));

            for (int index = 0; index < archetypes.Length; index++)
            {
                P6StageArchetype archetype = archetypes[index];
                P6GenerationRequest request =
                    P6GenerationRequest.CreateMoonPalace(
                        7100 + index,
                        P6StageSlot.X2,
                        prefabs,
                        archetype);

                P6GenerationResult first =
                    P6RoomGraphGenerator.Generate(request);
                P6GenerationResult repeated =
                    P6RoomGraphGenerator.Generate(request);

                AssertAccepted(first, $"archetype={archetype}");
                AssertAccepted(repeated, $"repeated archetype={archetype}");
                Assert.That(
                    first.Plan.Archetype,
                    Is.EqualTo(archetype));
                Assert.That(
                    repeated.AcceptedSeed,
                    Is.EqualTo(first.AcceptedSeed));
                Assert.That(
                    repeated.Attempts,
                    Is.EqualTo(first.Attempts));
                Assert.That(
                    first.Fingerprint,
                    Is.Not.Null.And.Not.Empty);
                Assert.That(
                    repeated.Fingerprint,
                    Is.EqualTo(first.Fingerprint));
                AssertPlansEquivalent(first.Plan, repeated.Plan);
                AssertArchetypeStructure(first.Plan);
                AssertSemanticRoleContracts(
                    first.Plan,
                    $"archetype={archetype}");
            }
        }

        [TestCase(P6StageSlot.X1, 630101)]
        [TestCase(P6StageSlot.X2, 630202)]
        [TestCase(P6StageSlot.X3, 630303)]
        public void EveryStageSlot_GeneratesAnActualP4TraversalProofPlan(
            P6StageSlot stageSlot,
            int seed)
        {
            P6RoomPrefabDescriptor[] prefabs =
                LoadMoonDescriptors();
            P6GenerationResult result =
                P6RoomGraphGenerator.Generate(
                    P6GenerationRequest.CreateMoonPalace(
                        seed,
                        stageSlot,
                        prefabs,
                        P6StageArchetype.Traverse));

            AssertAccepted(result, $"stageSlot={stageSlot}");
            Assert.That(result.Plan.StageSlot, Is.EqualTo(stageSlot));
            Assert.That(
                result.Plan.Rooms,
                Is.Not.Empty,
                "The generated proof plan contains no selected room.");

            for (int index = 0;
                 index < result.Plan.Rooms.Count;
                 index++)
            {
                AssertSelectedRoomProof(
                    result.Plan.Rooms[index],
                    $"{stageSlot}/node={index}");
            }
            AssertSemanticRoleContracts(
                result.Plan,
                $"stageSlot={stageSlot}");

            int supplyRooms = result.Plan.Rooms.Count(room =>
                (room.Role & RoomRole.SupplyCorridor) != 0);
            if (stageSlot == P6StageSlot.X3)
            {
                Assert.That(
                    prefabs.Any(prefab =>
                        prefab.Supports(RoomRole.Boss)),
                    Is.False,
                    "The actual P4 Moon catalog must not imply a Boss prefab.");
                Assert.That(
                    prefabs.Any(prefab =>
                        prefab.Supports(RoomRole.Landmark)
                        && prefab.MacroSize
                            == new Vector2Int(2, 2)),
                    Is.True,
                    "The actual P4 Moon catalog must author a 2x2 "
                    + "Landmark descriptor.");
                Assert.That(
                    supplyRooms,
                    Is.EqualTo(1),
                    "X-3 must reserve exactly one pre-exit Supply Corridor.");
                Assert.That(
                    result.Plan.Rooms.Count(room =>
                        (room.Role & RoomRole.Landmark) != 0),
                    Is.EqualTo(1),
                    "X-3 must reserve exactly one authored Landmark.");
                Assert.That(
                    result.Plan.Rooms.Count(room =>
                        (room.Role & RoomRole.Boss) != 0),
                    Is.EqualTo(1),
                    "X-3 must reserve exactly one semantic Boss.");
                P6RoomNode supply = result.Plan.Rooms.Single(room =>
                    (room.Role & RoomRole.SupplyCorridor) != 0);
                P6RoomNode landmark = result.Plan.Rooms.Single(room =>
                    (room.Role & RoomRole.Landmark) != 0);
                P6RoomNode boss = result.Plan.Rooms.Single(room =>
                    (room.Role & RoomRole.Boss) != 0);
                Assert.That(
                    supply.OnMainPath,
                    Is.True,
                    "The X-3 Supply Corridor must be on the main route.");
                Assert.That(
                    landmark.OnMainPath,
                    Is.False,
                    "The authored X-3 Landmark must remain an optional "
                    + "off-main reservation.");
                Assert.That(boss.OnMainPath, Is.True);
                Assert.That(supply.Id, Is.Not.EqualTo(boss.Id));
                Assert.That(landmark.Id, Is.Not.EqualTo(boss.Id));
                Assert.That(landmark.Id, Is.Not.EqualTo(supply.Id));
                Assert.That(
                    landmark.MacroBounds.size,
                    Is.EqualTo(new Vector2Int(2, 2)));
                Assert.That(
                    boss.MacroBounds.size,
                    Is.EqualTo(new Vector2Int(2, 2)));
                Assert.That(
                    landmark.MacroBounds.Overlaps(boss.MacroBounds),
                    Is.False,
                    "The distinct X-3 Landmark and Boss reservations "
                    + "must not overlap.");
                Assert.That(
                    (landmark.SupportedRoles & RoomRole.Landmark) != 0,
                    Is.True,
                    "The X-3 Landmark must use an authored Landmark "
                    + "descriptor.");
                Assert.That(
                    boss.Archetype,
                    Is.EqualTo(RoomArchetype.Large));
                Assert.That(
                    (boss.SupportedRoles & RoomRole.Boss) != 0,
                    Is.False,
                    "The actual P4 X-3 Boss must remain a semantic "
                    + "placeholder over a generic Large descriptor.");
                P6RoomPrefabDescriptor selectedLandmarkPrefab =
                    prefabs.Single(prefab =>
                        prefab.PrefabId == landmark.PrefabId);
                Assert.That(
                    selectedLandmarkPrefab.Supports(RoomRole.Landmark),
                    Is.True);
                Assert.That(
                    selectedLandmarkPrefab.MacroSize,
                    Is.EqualTo(new Vector2Int(2, 2)));
                P6RoomPrefabDescriptor selectedBossPrefab =
                    prefabs.Single(prefab =>
                        prefab.PrefabId == boss.PrefabId);
                Assert.That(
                    selectedBossPrefab.HasValidatedTraversalProof,
                    Is.True);
                Assert.That(
                    selectedBossPrefab.Archetype,
                    Is.EqualTo(RoomArchetype.Large));
                AssertSelectedRoomProof(
                    supply,
                    "X-3 Supply Corridor");
                AssertSelectedRoomProof(
                    landmark,
                    "X-3 authored Landmark");
                AssertSelectedRoomProof(
                    boss,
                    "X-3 semantic Boss placeholder");
                int supplyIndex =
                    IndexOf(
                        result.Plan.MainPathNodeIds,
                        supply.Id);
                int bossIndex =
                    IndexOf(
                        result.Plan.MainPathNodeIds,
                        boss.Id);
                Assert.That(
                    supplyIndex,
                    Is.EqualTo(
                        result.Plan.MainPathNodeIds.Count - 3));
                Assert.That(bossIndex, Is.EqualTo(supplyIndex + 1));
                Assert.That(
                    result.Plan.MainPathNodeIds[bossIndex + 1],
                    Is.EqualTo(result.Plan.ExitNodeId));
                Assert.That(
                    FindEdge(
                        result.Plan,
                        supply.Id,
                        boss.Id),
                    Is.Not.Null);
                Assert.That(
                    FindEdge(
                        result.Plan,
                        boss.Id,
                        result.Plan.ExitNodeId),
                    Is.Not.Null);
            }
        }

        [Test]
        public void X3SyntheticBossCatalog_OrdersDistinctSupplyBossExitRooms()
        {
            P6RoomPrefabDescriptor[] actual =
                LoadMoonDescriptors();
            P6RoomPrefabDescriptor proofSource =
                actual.First(prefab =>
                    prefab.MacroSize == new Vector2Int(2, 2)
                    && prefab.MainRouteAllowed
                    && prefab.HasValidatedTraversalProof
                    && prefab.TraversalProof.Links.Any(link =>
                        link.Kind == P6SocketLinkKind.MainRoutePair
                        && link.Passed));
            P6RoomPrefabDescriptor syntheticBoss =
                new P6RoomPrefabDescriptor(
                    "Synthetic_P6_Boss_Proof",
                    RoomArchetype.Boss,
                    RoomRole.Boss | RoomRole.Main,
                    RoomRegion.MoonPalace,
                    proofSource.MacroSize,
                    proofSource.SocketSides,
                    mainRouteAllowed: true,
                    selectionWeight: 1,
                    maxUses: 1,
                    traversalProof: proofSource.TraversalProof);
            P6RoomPrefabDescriptor[] extended =
                actual.Concat(new[] { syntheticBoss }).ToArray();

            P6GenerationResult result =
                P6RoomGraphGenerator.Generate(
                    P6GenerationRequest.CreateMoonPalace(
                        630333,
                        P6StageSlot.X3,
                        extended,
                        P6StageArchetype.Traverse));

            AssertAccepted(result, "synthetic X-3 Boss catalog");
            AssertSemanticRoleContracts(
                result.Plan,
                "synthetic X-3 Boss catalog");
            Assert.That(
                result.Plan.Rooms.Count(room =>
                    (room.Role & RoomRole.SupplyCorridor) != 0),
                Is.EqualTo(1));
            Assert.That(
                result.Plan.Rooms.Count(room =>
                    (room.Role & RoomRole.Landmark) != 0),
                Is.EqualTo(1),
                "Synthetic X-3 must retain exactly one authored "
                + "Landmark reservation.");
            Assert.That(
                result.Plan.Rooms.Count(room =>
                    (room.Role & RoomRole.Boss) != 0),
                Is.EqualTo(1),
                "Synthetic X-3 must retain exactly one semantic Boss.");
            P6RoomNode supply = result.Plan.Rooms.Single(room =>
                (room.Role & RoomRole.SupplyCorridor) != 0);
            P6RoomNode landmark = result.Plan.Rooms.Single(room =>
                (room.Role & RoomRole.Landmark) != 0);
            P6RoomNode boss = result.Plan.Rooms.Single(room =>
                (room.Role & RoomRole.Boss) != 0);
            int supplyIndex =
                IndexOf(result.Plan.MainPathNodeIds, supply.Id);
            int bossIndex =
                IndexOf(result.Plan.MainPathNodeIds, boss.Id);

            Assert.That(supply.Id, Is.Not.EqualTo(boss.Id));
            Assert.That(landmark.Id, Is.Not.EqualTo(boss.Id));
            Assert.That(landmark.Id, Is.Not.EqualTo(supply.Id));
            Assert.That(
                landmark.OnMainPath,
                Is.False,
                "The synthetic catalog's authored Landmark must "
                + "remain off the main route.");
            Assert.That(boss.OnMainPath, Is.True);
            Assert.That(
                landmark.MacroBounds.size,
                Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(
                (landmark.SupportedRoles & RoomRole.Landmark) != 0,
                Is.True,
                "The Landmark reservation must select an authored "
                + "Landmark descriptor.");
            Assert.That(
                boss.PrefabId,
                Is.EqualTo(syntheticBoss.PrefabId));
            Assert.That(
                boss.MacroBounds.size,
                Is.EqualTo(new Vector2Int(2, 2)));
            Assert.That(
                (boss.SupportedRoles & RoomRole.Boss) != 0,
                Is.True,
                "The synthetic authored Boss must retain Boss support "
                + "on the selected node.");
            Assert.That(
                landmark.MacroBounds.Overlaps(boss.MacroBounds),
                Is.False,
                "The distinct authored Landmark and authored Boss "
                + "must not overlap.");
            Assert.That(
                supplyIndex,
                Is.EqualTo(result.Plan.MainPathNodeIds.Count - 3));
            Assert.That(bossIndex, Is.EqualTo(supplyIndex + 1));
            Assert.That(
                result.Plan.MainPathNodeIds[bossIndex + 1],
                Is.EqualTo(result.Plan.ExitNodeId));
            Assert.That(
                FindEdge(result.Plan, supply.Id, boss.Id),
                Is.Not.Null);
            Assert.That(
                FindEdge(result.Plan, boss.Id, result.Plan.ExitNodeId),
                Is.Not.Null);
        }

        [Test]
        public void GeneratedX2_PacksTenToFourteenVariableRoomsWithoutOverlap()
        {
            P6GenerationResult result =
                GenerateX2(314159, P6StageArchetype.Fork);
            AssertAccepted(result, "packing fixture");

            P6RoomGraphPlan plan = result.Plan;
            Assert.That(
                plan.CanvasBounds,
                Is.EqualTo(new RectInt(0, 0, 8, 6)));
            Assert.That(plan.Rooms.Count, Is.InRange(10, 14));
            Assert.That(result.Validation.PackingSatisfied, Is.True);

            HashSet<int> nodeIds = new HashSet<int>();
            Dictionary<string, int> prefabUses =
                new Dictionary<string, int>(StringComparer.Ordinal);
            bool hasVariableFootprint = false;
            HashSet<Vector2Int> logicalSizes =
                new HashSet<Vector2Int>();
            int occupiedMacroCells = 0;
            for (int first = 0; first < plan.Rooms.Count; first++)
            {
                P6RoomNode room = plan.Rooms[first];
                Assert.That(nodeIds.Add(room.Id), Is.True);
                Assert.That(
                    room.Id,
                    Is.EqualTo(first),
                    "Node ids must remain stable array indices.");
                Assert.That(
                    Contains(plan.CanvasBounds, room.MacroBounds),
                    Is.True,
                    $"Room {room.Id} exceeds the macro canvas.");
                Assert.That(
                    IsSupportedMacroSize(room.MacroBounds.size),
                    Is.True,
                    $"Room {room.Id} has unsupported footprint "
                    + $"{room.MacroBounds.size}.");
                hasVariableFootprint |=
                    room.MacroBounds.size != Vector2Int.one;
                logicalSizes.Add(room.TraversalProof.LogicalSize);
                occupiedMacroCells +=
                    room.MacroBounds.width
                    * room.MacroBounds.height;

                for (int second = first + 1;
                     second < plan.Rooms.Count;
                     second++)
                {
                    Assert.That(
                        room.MacroBounds.Overlaps(
                            plan.Rooms[second].MacroBounds),
                        Is.False,
                        $"Rooms {room.Id} and "
                        + $"{plan.Rooms[second].Id} overlap.");
                }

                prefabUses.TryGetValue(
                    room.PrefabId,
                    out int uses);
                prefabUses[room.PrefabId] = uses + 1;
            }

            Assert.That(
                hasVariableFootprint,
                Is.True,
                "P6 packing must exercise non-1x1 room footprints.");
            Assert.That(
                logicalSizes.Count,
                Is.GreaterThanOrEqualTo(3),
                "A generated stage must expose several authored room sizes.");
            Assert.That(
                occupiedMacroCells
                    / (float)(
                        plan.CanvasBounds.width
                        * plan.CanvasBounds.height),
                Is.LessThanOrEqualTo(0.7f),
                "Room packing must preserve space for physical corridors.");
            Assert.That(
                prefabUses.Values.All(uses => uses <= 2),
                Is.True,
                "A room prefab was selected more than twice.");

            P6RoomNode start = plan.Rooms[plan.StartNodeId];
            P6RoomNode exit = plan.Rooms[plan.ExitNodeId];
            Vector2Int startCenter = MacroCenter(start.MacroBounds);
            Vector2Int exitCenter = MacroCenter(exit.MacroBounds);
            Assert.That(
                Manhattan(startCenter, exitCenter),
                Is.GreaterThanOrEqualTo(2),
                "Start and Exit must occupy opposing macro areas.");
        }

        [Test]
        public void GeneratedGraph_UsesTheTrueMstAndOneToThreeExtraEdges()
        {
            P6GenerationResult result =
                GenerateX2(271828, P6StageArchetype.Circuit);
            AssertAccepted(result, "MST fixture");
            P6RoomGraphPlan plan = result.Plan;

            Assert.That(
                plan.MstEdges.Count,
                Is.EqualTo(plan.Rooms.Count - 1));
            Assert.That(plan.AdditionalEdges.Count, Is.InRange(1, 3));
            Assert.That(
                plan.Edges.Count,
                Is.EqualTo(
                    plan.MstEdges.Count
                    + plan.AdditionalEdges.Count));

            ReferenceMst expected =
                BuildReferenceMst(
                    plan.Rooms.Count,
                    plan.CandidateEdges);
            HashSet<string> actualMstKeys =
                new HashSet<string>(
                    plan.MstEdges.Select(edge =>
                        EdgeKey(
                            edge.FirstNodeId,
                            edge.SecondNodeId)),
                    StringComparer.Ordinal);
            Assert.That(
                actualMstKeys.SetEquals(expected.EdgeKeys),
                Is.True,
                "Selected MST differs from deterministic Kruskal.");
            Assert.That(
                plan.MstEdges.Sum(edge => edge.Weight),
                Is.EqualTo(expected.TotalWeight));

            HashSet<string> allEdgeKeys =
                new HashSet<string>(actualMstKeys, StringComparer.Ordinal);
            for (int index = 0;
                 index < plan.AdditionalEdges.Count;
                 index++)
            {
                P6GraphEdge edge = plan.AdditionalEdges[index];
                Assert.That(edge.Kind, Is.EqualTo(P6EdgeKind.Additional));
                Assert.That(
                    allEdgeKeys.Add(
                        EdgeKey(
                            edge.FirstNodeId,
                            edge.SecondNodeId)),
                    Is.True,
                    "An additional edge duplicates a selected edge.");
            }

            Assert.That(plan.HasClosedLoop, Is.True);
            Assert.That(IsConnected(plan), Is.True);
            AssertAdditionalEdgesAddPhysicalSegments(plan);
        }

        [Test]
        public void GeneratedX2_AssignsRequiredRolesAndToolFreeSixToNineRoomPath()
        {
            P6GenerationResult result =
                GenerateX2(1618033, P6StageArchetype.Fork);
            AssertAccepted(result, "role fixture");
            P6RoomGraphPlan plan = result.Plan;

            Assert.That(plan.MainPathNodeIds.Count, Is.InRange(6, 9));
            Assert.That(
                plan.MainPathNodeIds[0],
                Is.EqualTo(plan.StartNodeId));
            Assert.That(
                plan.MainPathNodeIds[
                    plan.MainPathNodeIds.Count - 1],
                Is.EqualTo(plan.ExitNodeId));
            Assert.That(
                plan.MainPathNodeIds.Distinct().Count(),
                Is.EqualTo(plan.MainPathNodeIds.Count));

            HashSet<string> graphEdges =
                new HashSet<string>(
                    plan.Edges.Select(edge =>
                        EdgeKey(
                            edge.FirstNodeId,
                            edge.SecondNodeId)),
                    StringComparer.Ordinal);
            for (int index = 0;
                 index < plan.MainPathNodeIds.Count;
                 index++)
            {
                int nodeId = plan.MainPathNodeIds[index];
                P6RoomNode room = plan.Rooms[nodeId];
                Assert.That(room.OnMainPath, Is.True);
                Assert.That(room.MainRouteAllowed, Is.True);
                Assert.That(
                    (room.Role & RoomRole.Main) != 0,
                    Is.True);
                if (index > 0)
                {
                    Assert.That(
                        graphEdges.Contains(
                            EdgeKey(
                                plan.MainPathNodeIds[index - 1],
                                nodeId)),
                        Is.True,
                        "Main-path sequence contains a missing graph edge.");
                }
            }

            Assert.That(
                plan.Rooms.Count(room =>
                    (room.Role & RoomRole.Start) != 0),
                Is.EqualTo(1));
            Assert.That(
                plan.Rooms.Count(room =>
                    (room.Role & RoomRole.Exit) != 0),
                Is.EqualTo(1));
            Assert.That(
                (plan.Rooms[plan.StartNodeId].Role
                    & RoomRole.Start) != 0,
                Is.True);
            Assert.That(
                (plan.Rooms[plan.ExitNodeId].Role
                    & RoomRole.Exit) != 0,
                Is.True);

            AssertRequiredX2Role(plan, RoomRole.Landmark);
            AssertRequiredX2Role(plan, RoomRole.ShopCorridor);
            AssertRequiredX2Role(plan, RoomRole.RecordRoom);
            AssertRequiredX2Role(plan, RoomRole.MaruStatue);

            P6RoomNode shop = plan.Rooms.First(room =>
                (room.Role & RoomRole.ShopCorridor) != 0);
            int shopPathIndex =
                IndexOf(plan.MainPathNodeIds, shop.Id);
            Assert.That(
                shopPathIndex,
                Is.GreaterThan(0).And.LessThan(
                    plan.MainPathNodeIds.Count - 1),
                "The X-2 shop corridor belongs in the middle "
                + "of the main path.");

            P6RoomNode landmark = plan.Rooms.First(room =>
                (room.Role & RoomRole.Landmark) != 0);
            Assert.That(
                landmark.MacroBounds.size,
                Is.EqualTo(new Vector2Int(2, 2)),
                "X-2 must reserve a large landmark room.");

            int[] degrees = CalculateDegrees(plan);
            foreach (P6RoomNode leaf in plan.Rooms.Where(room =>
                (room.Role
                    & (RoomRole.RecordRoom | RoomRole.MaruStatue))
                != 0))
            {
                Assert.That(
                    leaf.OnMainPath,
                    Is.False,
                    $"{leaf.Role} must remain optional.");
                if (P6RoomGraphValidator.HostsMaruStatue(leaf))
                {
                    continue;
                }

                Assert.That(
                    degrees[leaf.Id],
                    Is.EqualTo(1),
                    $"{leaf.Role} must remain a leaf role.");
            }

            AssertMaruStatueRoomsCanReachExit(plan);
        }

        [Test]
        public void GeneratedCorridors_UseExactAuthoredSocketsAtScaleTwoWithoutCrossingRooms()
        {
            P6GenerationResult result =
                GenerateX2(424242, P6StageArchetype.Traverse);
            AssertAccepted(result, "corridor fixture");
            P6RoomGraphPlan plan = result.Plan;

            Assert.That(result.Validation.CorridorNetworkConnected, Is.True);
            Assert.That(result.Validation.SocketClaimsUnique, Is.True);
            Assert.That(result.Validation.PhysicalTraversalSatisfied, Is.True);
            Assert.That(
                plan.CorridorNetwork.RoutingScale,
                Is.EqualTo(2));

            Dictionary<int, P6RoomAccess> accessById =
                plan.CorridorNetwork.RoomAccesses.ToDictionary(
                    access => access.AccessId);
            Assert.That(
                accessById.Count,
                Is.EqualTo(plan.CorridorNetwork.RoomAccesses.Count),
                "Corridor access ids must be unique.");
            Assert.That(
                plan.CorridorNetwork.RoomAccesses
                    .Select(access => access.NodeId)
                    .Distinct()
                    .Count(),
                Is.EqualTo(plan.Rooms.Count),
                "Every selected room must expose a routed access.");

            HashSet<Vector2Int> networkCells =
                new HashSet<Vector2Int>(
                    plan.CorridorNetwork.Cells);
            HashSet<string> authoredSocketClaims =
                new HashSet<string>(StringComparer.Ordinal);
            HashSet<int> referencedAccesses = new HashSet<int>();

            foreach (P6RoomAccess access
                     in plan.CorridorNetwork.RoomAccesses)
            {
                P6RoomNode room = plan.Rooms[access.NodeId];
                AssertSelectedRoomProof(
                    room,
                    $"routed node={room.Id}");
                P6SocketTraversalProof socket =
                    room.TraversalProof.FindSocket(access.SocketId);
                Assert.That(
                    socket,
                    Is.Not.Null,
                    $"Node {room.Id} does not author socket "
                    + $"{access.SocketId}.");
                Assert.That(
                    authoredSocketClaims.Add(
                        $"{access.NodeId}:{access.SocketId}"),
                    Is.True,
                    "A normalized node/socket access was duplicated.");
                Assert.That(
                    access.SocketSide,
                    Is.EqualTo(ToP6Side(socket.Side)));
                Assert.That(
                    access.CellOffset,
                    Is.EqualTo(socket.CellOffset));
                Assert.That(
                    access.OpeningSize,
                    Is.EqualTo(socket.OpeningSize));
                Assert.That(
                    access.TraversalType,
                    Is.EqualTo(socket.TraversalType));
                Assert.That(
                    access.MainRouteAllowed,
                    Is.EqualTo(socket.MainRouteAllowed));
                Assert.That(
                    access.ValidationAnchor,
                    Is.EqualTo(socket.ValidationAnchor));
                Assert.That(
                    ScaledRoomInterior(
                            room.MacroBounds,
                            plan.CorridorNetwork.RoutingScale)
                        .Contains(access.SocketCell),
                    Is.True);
                Assert.That(
                    Manhattan(
                        access.SocketCell,
                        access.PortalCell),
                    Is.EqualTo(1));
                Assert.That(
                    plan.CorridorNetwork.RoutingBounds.Contains(
                        access.PortalCell),
                    Is.True);
                Assert.That(
                    ExpectedPortalDelta(access.SocketSide),
                    Is.EqualTo(
                        access.PortalCell - access.SocketCell),
                    $"Node {room.Id}/{access.SocketId} portal side.");
                Assert.That(
                    ScaledRoomInterior(
                            room.MacroBounds,
                            plan.CorridorNetwork.RoutingScale)
                        .Contains(access.PortalCell),
                    Is.False);
                Assert.That(
                    (room.SocketSides & access.SocketSide) != 0,
                    Is.True);
            }

            foreach (P6GraphEdge edge in plan.Edges)
            {
                Assert.That(
                    accessById.ContainsKey(edge.FirstAccessId),
                    Is.True,
                    $"Missing first access for edge "
                    + $"{edge.FirstNodeId}-{edge.SecondNodeId}.");
                Assert.That(
                    accessById.ContainsKey(edge.SecondAccessId),
                    Is.True,
                    $"Missing second access for edge "
                    + $"{edge.FirstNodeId}-{edge.SecondNodeId}.");
                P6RoomAccess firstAccess =
                    accessById[edge.FirstAccessId];
                P6RoomAccess secondAccess =
                    accessById[edge.SecondAccessId];
                referencedAccesses.Add(firstAccess.AccessId);
                referencedAccesses.Add(secondAccess.AccessId);
                Assert.That(
                    firstAccess.NodeId,
                    Is.EqualTo(edge.FirstNodeId));
                Assert.That(
                    secondAccess.NodeId,
                    Is.EqualTo(edge.SecondNodeId));
                Assert.That(
                    firstAccess.AccessId,
                    Is.Not.EqualTo(secondAccess.AccessId));

                Assert.That(edge.CorridorCells.Count, Is.GreaterThan(0));
                Assert.That(
                    edge.CorridorCells[0],
                    Is.EqualTo(firstAccess.PortalCell));
                Assert.That(
                    edge.CorridorCells[
                        edge.CorridorCells.Count - 1],
                    Is.EqualTo(secondAccess.PortalCell));
                if (edge.CorridorCells.Count == 1)
                {
                    Assert.That(
                        firstAccess.PortalCell,
                        Is.EqualTo(secondAccess.PortalCell),
                        "A one-cell corridor is valid only when both "
                        + "room accesses share the same portal cell.");
                }

                for (int index = 0;
                     index < edge.CorridorCells.Count;
                     index++)
                {
                    Vector2Int cell = edge.CorridorCells[index];
                    Assert.That(
                        plan.CorridorNetwork.RoutingBounds.Contains(cell),
                        Is.True);
                    Assert.That(networkCells.Contains(cell), Is.True);
                    for (int roomIndex = 0;
                         roomIndex < plan.Rooms.Count;
                         roomIndex++)
                    {
                        Assert.That(
                            ScaledRoomInterior(
                                    plan.Rooms[roomIndex].MacroBounds,
                                    plan.CorridorNetwork.RoutingScale)
                                .Contains(cell),
                            Is.False,
                            $"Edge {edge.FirstNodeId}-"
                            + $"{edge.SecondNodeId} crosses room "
                            + $"{plan.Rooms[roomIndex].Id} at {cell}.");
                    }
                    if (index > 0)
                    {
                        Assert.That(
                            Manhattan(
                                edge.CorridorCells[index - 1],
                                cell),
                            Is.EqualTo(1),
                            $"Edge {edge.FirstNodeId}-"
                            + $"{edge.SecondNodeId} has a discontinuity.");
                    }
                }
            }

            Assert.That(
                referencedAccesses.SetEquals(accessById.Keys),
                Is.True,
                "The corridor network retained an unreferenced access.");
            AssertMainPathUsesDistinctDirectedSocketProofs(
                plan,
                accessById);
            AssertAdditionalEdgesAddPhysicalSegments(plan);
        }

        [Test]
        public void ImpossibleCandidate_RejectionIsDeterministic()
        {
            P6RoomPrefabDescriptor[] prefabs = LoadMoonDescriptors();
            P6GenerationRequest impossible =
                new P6GenerationRequest(
                    99,
                    P6StageSlot.X2,
                    P6StageArchetype.Circuit,
                    RoomRegion.MoonPalace,
                    prefabs,
                    new Vector2Int(2, 2),
                    minRooms: 14,
                    maxRooms: 14,
                    maxAttempts: 1);

            bool firstAccepted =
                P6RoomGraphGenerator.TryGenerateCandidate(
                    impossible,
                    123456,
                    out P6RoomGraphPlan firstPlan,
                    out P6GenerationFailure firstFailure,
                    out string firstReason);
            bool repeatedAccepted =
                P6RoomGraphGenerator.TryGenerateCandidate(
                    impossible,
                    123456,
                    out P6RoomGraphPlan repeatedPlan,
                    out P6GenerationFailure repeatedFailure,
                    out string repeatedReason);

            Assert.That(firstAccepted, Is.False);
            Assert.That(repeatedAccepted, Is.False);
            Assert.That(firstPlan, Is.Null);
            Assert.That(repeatedPlan, Is.Null);
            Assert.That(
                firstFailure,
                Is.EqualTo(P6GenerationFailure.PackingFailed));
            Assert.That(repeatedFailure, Is.EqualTo(firstFailure));
            Assert.That(repeatedReason, Is.EqualTo(firstReason));
            Assert.That(firstReason, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        [Timeout(120000)]
        public void OneThousandX2Seeds_MeetEveryP6GenerationGate()
        {
            AssertSeedGate(seedCount: 1000);
        }

        [Test]
        [Explicit("Manual P6 soak; the required routine gate is 1,000 seeds.")]
        [Category("Soak")]
        [Timeout(120000)]
        public void TenThousandX2Seeds_OptionalSoak()
        {
            AssertSeedGate(seedCount: 10000);
        }

        private static void AssertSeedGate(int seedCount)
        {
            P6RoomPrefabDescriptor[] prefabs = LoadMoonDescriptors();
            SeedGateSummary summary =
                new SeedGateSummary(seedCount);
            Stopwatch stopwatch = Stopwatch.StartNew();

            for (int seed = 0; seed < seedCount; seed++)
            {
                P6GenerationResult result =
                    P6RoomGraphGenerator.Generate(
                        P6GenerationRequest.CreateMoonPalace(
                            seed,
                            P6StageSlot.X2,
                            prefabs));
                summary.Observe(seed, result);
            }

            stopwatch.Stop();
            summary.ElapsedMilliseconds =
                stopwatch.Elapsed.TotalMilliseconds;
            TestContext.Progress.WriteLine(summary.ToString());

            Assert.That(
                summary.RejectedSeeds,
                Is.EqualTo(0),
                summary.ToString());
            Assert.That(
                summary.ObservedRoomCountTotal,
                Is.EqualTo(seedCount),
                summary.ToString());
            Assert.That(
                summary.MinimumRoomCount,
                Is.GreaterThanOrEqualTo(10),
                "X-2 reserves enough mandatory structure to use "
                + "at least ten rooms."
                + Environment.NewLine
                + summary);
            Assert.That(
                summary.MaximumRoomCount,
                Is.LessThanOrEqualTo(14),
                summary.ToString());
            Assert.That(
                summary.RoomCount(13),
                Is.GreaterThan(0),
                "The 1,000-seed cohort never exercised a 13-room plan."
                + Environment.NewLine
                + summary);
            Assert.That(
                summary.RoomCount(14),
                Is.GreaterThan(0),
                "The 1,000-seed cohort never exercised a 14-room plan."
                + Environment.NewLine
                + summary);
            Assert.That(
                summary.StartExitFailures,
                Is.EqualTo(0),
                summary.ToString());
            Assert.That(
                summary.PrefabRepeatSeeds * 100,
                Is.LessThan(seedCount),
                "Seeds containing any prefab three or more times "
                + "must remain below 1%."
                + Environment.NewLine
                + summary);
            Assert.That(
                summary.X2LoopFailures,
                Is.EqualTo(0),
                summary.ToString());
            Assert.That(
                summary.StructuralFailures,
                Is.EqualTo(0),
                summary.ToString());
            Assert.That(
                summary.RoleFailures,
                Is.EqualTo(0),
                summary.ToString());
            Assert.That(
                summary.OneCellOpeningFailures,
                Is.EqualTo(0),
                summary.ToString());
            Assert.That(
                summary.JumpEnvelopeFailures,
                Is.EqualTo(0),
                summary.ToString());
            Assert.That(
                summary.ToolFreeMainPathFailures,
                Is.EqualTo(0),
                summary.ToString());
            Assert.That(
                summary.ExitBlockProofFailures,
                Is.EqualTo(0),
                summary.ToString());
            Assert.That(
                summary.PhysicalTraversalFailures,
                Is.EqualTo(0),
                summary.ToString());
            Assert.That(
                summary.RiskyChoiceFlagFailures,
                Is.EqualTo(0),
                summary.ToString());
            Assert.That(
                summary.LandmarkPriorityFlagFailures,
                Is.EqualTo(0),
                summary.ToString());
            Assert.That(
                summary.X3RoleSequenceFlagFailures,
                Is.EqualTo(0),
                summary.ToString());
            Assert.That(
                summary.DirectRiskyChoiceFailures,
                Is.EqualTo(0),
                summary.ToString());
            Assert.That(
                summary.DirectLandmarkPriorityFailures,
                Is.EqualTo(0),
                summary.ToString());
            Assert.That(
                summary.Archetypes.Count,
                Is.EqualTo(
                    Enum.GetValues(
                        typeof(P6StageArchetype)).Length),
                "The seed cohort did not exercise every stage archetype."
                + Environment.NewLine
                + summary);
        }

        private static void AssertArchetypeStructure(
            P6RoomGraphPlan plan)
        {
            P6RoomNode start = plan.Rooms[plan.StartNodeId];
            P6RoomNode exit = plan.Rooms[plan.ExitNodeId];
            if (plan.Archetype == P6StageArchetype.Descent)
            {
                Assert.That(
                    start.MacroBounds.yMax,
                    Is.EqualTo(plan.CanvasBounds.yMax),
                    "Descent must start on the top terminal edge.");
                Assert.That(
                    exit.MacroBounds.yMin,
                    Is.EqualTo(plan.CanvasBounds.yMin),
                    "Descent must exit on the bottom terminal edge.");
            }
            else if (plan.Archetype == P6StageArchetype.Ascent)
            {
                Assert.That(
                    start.MacroBounds.yMin,
                    Is.EqualTo(plan.CanvasBounds.yMin),
                    "Ascent must start on the bottom terminal edge.");
                Assert.That(
                    exit.MacroBounds.yMax,
                    Is.EqualTo(plan.CanvasBounds.yMax),
                    "Ascent must exit on the top terminal edge.");
            }
            else
            {
                Assert.That(
                    start.MacroBounds.xMin,
                    Is.EqualTo(plan.CanvasBounds.xMin),
                    $"{plan.Archetype} must start on the left terminal edge.");
                Assert.That(
                    exit.MacroBounds.xMax,
                    Is.EqualTo(plan.CanvasBounds.xMax),
                    $"{plan.Archetype} must exit on the right terminal edge.");
            }

            if (plan.Archetype == P6StageArchetype.Circuit)
            {
                Vector2Int center = new Vector2Int(
                    plan.CanvasBounds.xMin
                        + plan.CanvasBounds.width / 2,
                    plan.CanvasBounds.yMin
                        + plan.CanvasBounds.height / 2);
                Assert.That(
                    plan.Rooms.Any(room =>
                        room.Degree >= 3
                        && Manhattan(
                            MacroCenter(room.MacroBounds),
                            center) <= 2),
                    Is.True,
                    "Circuit requires a central degree-3 hub.");
                Assert.That(plan.HasClosedLoop, Is.True);
                AssertAdditionalEdgesAddPhysicalSegments(plan);
                Assert.That(
                    ContainsPhysicalGridCycle(
                        plan.CorridorNetwork.Cells),
                    Is.True,
                    "Circuit requires a loop in the routed corridor lattice.");
            }
            else if (plan.Archetype == P6StageArchetype.Fork)
            {
                Assert.That(
                    plan.Rooms.Count(room => room.Degree >= 3),
                    Is.GreaterThanOrEqualTo(2),
                    "Fork requires separate branch and rejoin junctions.");
                Assert.That(plan.HasClosedLoop, Is.True);
            }
            else if (plan.Archetype == P6StageArchetype.Chase)
            {
                int longRooms = plan.Rooms.Count(room =>
                    room.Archetype == RoomArchetype.Wide
                    || room.Archetype == RoomArchetype.Tall
                    || room.Archetype
                        == RoomArchetype.HorizontalCorridor
                    || room.Archetype
                        == RoomArchetype.VerticalCorridor);
                Assert.That(
                    longRooms,
                    Is.GreaterThanOrEqualTo(3),
                    "Chase requires at least three long rooms.");
            }
        }

        private static void AssertSemanticRoleContracts(
            P6RoomGraphPlan plan,
            string context)
        {
            P6RoomNode[] riskyRooms = plan.Rooms
                .Where(room =>
                    (room.Role & RoomRole.RiskyChoice) != 0)
                .ToArray();
            Assert.That(
                riskyRooms.Length,
                Is.EqualTo(1),
                $"{context}: exactly one RiskyChoice is required.");
            P6RoomNode risky = riskyRooms[0];
            Assert.That(
                risky.OnMainPath,
                Is.False,
                $"{context}: RiskyChoice entered the main path.");
            Assert.That(
                (risky.Role & RoomRole.Main) != 0,
                Is.False,
                $"{context}: RiskyChoice carries Main.");
            Assert.That(
                risky.Degree,
                Is.EqualTo(1),
                $"{context}: RiskyChoice is not a degree-1 leaf.");

            int[] neighbours = plan.Edges
                .Where(edge =>
                    edge.FirstNodeId == risky.Id
                    || edge.SecondNodeId == risky.Id)
                .Select(edge =>
                    edge.FirstNodeId == risky.Id
                        ? edge.SecondNodeId
                        : edge.FirstNodeId)
                .ToArray();
            Assert.That(
                neighbours.Length,
                Is.EqualTo(1),
                $"{context}: RiskyChoice graph degree is not one.");
            int anchorIndex =
                IndexOf(
                    plan.MainPathNodeIds,
                    neighbours[0]);
            int exitDistance =
                plan.MainPathNodeIds.Count - 1 - anchorIndex;
            Assert.That(
                anchorIndex,
                Is.GreaterThanOrEqualTo(0),
                $"{context}: RiskyChoice anchor is not on the main path.");
            Assert.That(
                exitDistance,
                Is.InRange(2, 4),
                $"{context}: RiskyChoice anchor must be 2-4 rooms "
                + "before Exit.");

            for (int index = 0;
                 index < plan.Rooms.Count;
                 index++)
            {
                P6RoomNode room = plan.Rooms[index];
                bool landmarkJunction =
                    (room.Role & RoomRole.LandmarkJunction) != 0;
                Assert.That(
                    landmarkJunction,
                    Is.EqualTo(room.Degree >= 3),
                    $"{context}: node {room.Id} degree={room.Degree} "
                    + "has incorrect LandmarkJunction semantics.");
            }
        }

        private static bool DirectRiskyChoiceSatisfied(
            P6RoomGraphPlan plan)
        {
            P6RoomNode[] riskyRooms = plan.Rooms
                .Where(room =>
                    (room.Role & RoomRole.RiskyChoice) != 0)
                .ToArray();
            if (riskyRooms.Length != 1)
            {
                return false;
            }

            P6RoomNode risky = riskyRooms[0];
            if (risky.OnMainPath
                || (risky.Role & RoomRole.Main) != 0
                || risky.Degree != 1)
            {
                return false;
            }

            int[] neighbours = plan.Edges
                .Where(edge =>
                    edge.FirstNodeId == risky.Id
                    || edge.SecondNodeId == risky.Id)
                .Select(edge =>
                    edge.FirstNodeId == risky.Id
                        ? edge.SecondNodeId
                        : edge.FirstNodeId)
                .ToArray();
            if (neighbours.Length != 1)
            {
                return false;
            }

            int anchorIndex =
                IndexOf(plan.MainPathNodeIds, neighbours[0]);
            int exitDistance =
                plan.MainPathNodeIds.Count - 1 - anchorIndex;
            return anchorIndex >= 0
                && exitDistance >= 2
                && exitDistance <= 4;
        }

        private static bool DirectLandmarkPrioritySatisfied(
            P6RoomGraphPlan plan)
        {
            return plan.Rooms.All(room =>
                ((room.Role & RoomRole.LandmarkJunction) != 0)
                == (room.Degree >= 3));
        }

        private static void AssertSelectedRoomProof(
            P6RoomNode room,
            string context)
        {
            P6RoomTraversalProof proof = room.TraversalProof;
            Assert.That(
                room.HasValidatedTraversalProof,
                Is.True,
                context);
            Assert.That(proof, Is.Not.Null, context);
            Assert.That(proof.RoomId, Is.EqualTo(room.PrefabId), context);
            Assert.That(proof.Passed, Is.True, context);
            Assert.That(proof.ExitBlockProof, Is.True, context);
            Assert.That(proof.OneCellOpeningsPassed, Is.True, context);
            Assert.That(
                proof.OneCellBodyClearancePassed,
                Is.True,
                context);
            Assert.That(proof.JumpEnvelopePassed, Is.True, context);
            Assert.That(
                proof.Sockets.Any(socket =>
                    socket.IsToolFreeMainRouteSocket),
                Is.True,
                $"{context}: no tool-free traversal socket.");
            if (room.OnMainPath)
            {
                Assert.That(
                    proof.MainSocketPairsReachable,
                    Is.True,
                    $"{context}: directed main traversal proof failed.");
            }
        }

        private static void AssertAdditionalEdgesAddPhysicalSegments(
            P6RoomGraphPlan plan)
        {
            HashSet<string> occupiedSegments =
                new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < plan.MstEdges.Count; index++)
            {
                AddPhysicalSegments(
                    occupiedSegments,
                    plan.MstEdges[index].CorridorCells);
            }

            for (int index = 0;
                 index < plan.AdditionalEdges.Count;
                 index++)
            {
                P6GraphEdge edge = plan.AdditionalEdges[index];
                bool addsSegment = false;
                for (int cell = 1;
                     cell < edge.CorridorCells.Count;
                     cell++)
                {
                    if (!occupiedSegments.Contains(
                            PhysicalSegmentKey(
                                edge.CorridorCells[cell - 1],
                                edge.CorridorCells[cell])))
                    {
                        addsSegment = true;
                        break;
                    }
                }

                Assert.That(
                    addsSegment,
                    Is.True,
                    $"Additional edge {edge.FirstNodeId}-"
                    + $"{edge.SecondNodeId} reuses the physical network "
                    + "without creating a new segment.");
                AddPhysicalSegments(
                    occupiedSegments,
                    edge.CorridorCells);
            }
        }

        private static void AddPhysicalSegments(
            HashSet<string> destination,
            IReadOnlyList<Vector2Int> cells)
        {
            for (int index = 1; index < cells.Count; index++)
            {
                destination.Add(
                    PhysicalSegmentKey(
                        cells[index - 1],
                        cells[index]));
            }
        }

        private static string PhysicalSegmentKey(
            Vector2Int first,
            Vector2Int second)
        {
            if (first.x > second.x
                || (first.x == second.x && first.y > second.y))
            {
                Vector2Int swap = first;
                first = second;
                second = swap;
            }

            return $"{first.x},{first.y}>{second.x},{second.y}";
        }

        private static bool ContainsPhysicalGridCycle(
            IReadOnlyList<Vector2Int> source)
        {
            HashSet<Vector2Int> cells =
                new HashSet<Vector2Int>(source);
            if (cells.Count < 4)
            {
                return false;
            }

            int segments = 0;
            foreach (Vector2Int cell in cells)
            {
                if (cells.Contains(cell + Vector2Int.right))
                {
                    segments++;
                }
                if (cells.Contains(cell + Vector2Int.up))
                {
                    segments++;
                }
            }

            int components = 0;
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            foreach (Vector2Int origin in cells)
            {
                if (!visited.Add(origin))
                {
                    continue;
                }

                components++;
                Queue<Vector2Int> frontier = new Queue<Vector2Int>();
                frontier.Enqueue(origin);
                while (frontier.Count > 0)
                {
                    Vector2Int current = frontier.Dequeue();
                    Vector2Int[] neighbours =
                    {
                        current + Vector2Int.left,
                        current + Vector2Int.right,
                        current + Vector2Int.down,
                        current + Vector2Int.up
                    };
                    for (int index = 0;
                         index < neighbours.Length;
                         index++)
                    {
                        if (cells.Contains(neighbours[index])
                            && visited.Add(neighbours[index]))
                        {
                            frontier.Enqueue(neighbours[index]);
                        }
                    }
                }
            }

            return segments > cells.Count - components;
        }

        private static void AssertMainPathUsesDistinctDirectedSocketProofs(
            P6RoomGraphPlan plan,
            IReadOnlyDictionary<int, P6RoomAccess> accessById)
        {
            for (int index = 1;
                 index < plan.MainPathNodeIds.Count;
                 index++)
            {
                int fromNode = plan.MainPathNodeIds[index - 1];
                int toNode = plan.MainPathNodeIds[index];
                P6GraphEdge edge = FindEdge(plan, fromNode, toNode);
                Assert.That(
                    edge,
                    Is.Not.Null,
                    $"Main path edge {fromNode}-{toNode} is absent.");
                P6RoomAccess fromAccess =
                    AccessForNode(edge, fromNode, accessById);
                P6RoomAccess toAccess =
                    AccessForNode(edge, toNode, accessById);
                Assert.That(
                    fromAccess.IsToolFreeMainRouteAccess,
                    Is.True,
                    $"Main departure {fromNode}/{fromAccess.SocketId} "
                    + "is not tool-free.");
                Assert.That(
                    toAccess.IsToolFreeMainRouteAccess,
                    Is.True,
                    $"Main arrival {toNode}/{toAccess.SocketId} "
                    + "is not tool-free.");
            }

            for (int index = 1;
                 index < plan.MainPathNodeIds.Count - 1;
                 index++)
            {
                int previous = plan.MainPathNodeIds[index - 1];
                int current = plan.MainPathNodeIds[index];
                int next = plan.MainPathNodeIds[index + 1];
                P6GraphEdge incoming =
                    FindEdge(plan, previous, current);
                P6GraphEdge outgoing =
                    FindEdge(plan, current, next);
                P6RoomAccess entry =
                    AccessForNode(incoming, current, accessById);
                P6RoomAccess departure =
                    AccessForNode(outgoing, current, accessById);
                Assert.That(
                    entry.SocketId,
                    Is.Not.EqualTo(departure.SocketId),
                    $"Middle main room {current} reused one socket "
                    + "for arrival and departure.");

                P6RoomTraversalProof proof =
                    plan.Rooms[current].TraversalProof;
                P6SocketLinkProof directed = proof.Links.FirstOrDefault(
                    link =>
                        link.Kind == P6SocketLinkKind.MainRoutePair
                        && string.Equals(
                            link.FromSocketId,
                            entry.SocketId,
                            StringComparison.Ordinal)
                        && string.Equals(
                            link.ToSocketId,
                            departure.SocketId,
                            StringComparison.Ordinal));
                Assert.That(
                    directed,
                    Is.Not.Null,
                    $"Middle main room {current} lacks directed proof "
                    + $"{entry.SocketId}->{departure.SocketId}.");
                Assert.That(directed.Passed, Is.True);
            }
        }

        private static P6GraphEdge FindEdge(
            P6RoomGraphPlan plan,
            int firstNode,
            int secondNode)
        {
            string key = EdgeKey(firstNode, secondNode);
            return plan.Edges.FirstOrDefault(edge =>
                EdgeKey(
                    edge.FirstNodeId,
                    edge.SecondNodeId) == key);
        }

        private static P6RoomAccess AccessForNode(
            P6GraphEdge edge,
            int nodeId,
            IReadOnlyDictionary<int, P6RoomAccess> accessById)
        {
            Assert.That(edge, Is.Not.Null);
            int accessId;
            if (edge.FirstNodeId == nodeId)
            {
                accessId = edge.FirstAccessId;
            }
            else
            {
                Assert.That(
                    edge.SecondNodeId,
                    Is.EqualTo(nodeId));
                accessId = edge.SecondAccessId;
            }

            Assert.That(
                accessById.ContainsKey(accessId),
                Is.True,
                $"Edge access {accessId} is absent.");
            return accessById[accessId];
        }

        private static RectInt ScaledRoomInterior(
            RectInt macroBounds,
            int routingScale)
        {
            return new RectInt(
                macroBounds.xMin * routingScale + 1,
                macroBounds.yMin * routingScale + 1,
                macroBounds.width * routingScale - 1,
                macroBounds.height * routingScale - 1);
        }

        private static Vector2Int ExpectedPortalDelta(
            P6SocketSides side)
        {
            switch (side)
            {
                case P6SocketSides.Left:
                    return Vector2Int.left;
                case P6SocketSides.Right:
                    return Vector2Int.right;
                case P6SocketSides.Bottom:
                    return Vector2Int.down;
                case P6SocketSides.Top:
                    return Vector2Int.up;
                default:
                    Assert.Fail($"Access has invalid socket side {side}.");
                    return Vector2Int.zero;
            }
        }

        private static P6SocketSides ToP6Side(RoomSocketSide side)
        {
            switch (side)
            {
                case RoomSocketSide.Left:
                    return P6SocketSides.Left;
                case RoomSocketSide.Right:
                    return P6SocketSides.Right;
                case RoomSocketSide.Bottom:
                    return P6SocketSides.Bottom;
                case RoomSocketSide.Top:
                    return P6SocketSides.Top;
                default:
                    Assert.Fail($"Unknown authored socket side {side}.");
                    return P6SocketSides.None;
            }
        }

        private static P6GenerationResult GenerateX2(
            int seed,
            P6StageArchetype archetype)
        {
            return P6RoomGraphGenerator.Generate(
                P6GenerationRequest.CreateMoonPalace(
                    seed,
                    P6StageSlot.X2,
                    LoadMoonDescriptors(),
                    archetype));
        }

        private static void AssertAccepted(
            P6GenerationResult result,
            string context)
        {
            Assert.That(result, Is.Not.Null, context);
            Assert.That(
                result.Accepted,
                Is.True,
                $"{context}: {result.Failure} - "
                + $"{result.FailureReason}");
            Assert.That(result.Plan, Is.Not.Null);
            Assert.That(result.Validation, Is.Not.Null);
            Assert.That(
                result.Validation.Passed,
                Is.True,
                result.Validation.ToString());
            Assert.That(result.Validation.StartExitReachable, Is.True);
            Assert.That(result.Validation.OptionalRoomsReturnable, Is.True);
            Assert.That(result.Validation.ClosedLoopSatisfied, Is.True);
            Assert.That(result.Validation.MainPathLengthSatisfied, Is.True);
            Assert.That(result.Validation.PrefabUsageSatisfied, Is.True);
            Assert.That(result.Validation.PackingSatisfied, Is.True);
            Assert.That(
                result.Validation.CorridorNetworkConnected,
                Is.True);
            Assert.That(result.Validation.SocketClaimsUnique, Is.True);
            Assert.That(
                result.Validation.OneCellOpeningsSatisfied,
                Is.True);
            Assert.That(
                result.Validation.JumpEnvelopeSatisfied,
                Is.True);
            Assert.That(
                result.Validation.ToolFreeMainPathSatisfied,
                Is.True);
            Assert.That(
                result.Validation.ExitBlockProofSatisfied,
                Is.True);
            Assert.That(
                result.Validation.PhysicalTraversalSatisfied,
                Is.True);
            Assert.That(
                result.Validation.RiskyChoiceSatisfied,
                Is.True);
            Assert.That(
                result.Validation.LandmarkPrioritySatisfied,
                Is.True);
            Assert.That(
                result.Validation.X3RoleSequenceSatisfied,
                Is.True);
        }

        private static void AssertPlansEquivalent(
            P6RoomGraphPlan first,
            P6RoomGraphPlan repeated)
        {
            Assert.That(repeated.Seed, Is.EqualTo(first.Seed));
            Assert.That(
                repeated.StageSlot,
                Is.EqualTo(first.StageSlot));
            Assert.That(
                repeated.Archetype,
                Is.EqualTo(first.Archetype));
            Assert.That(
                repeated.CanvasBounds,
                Is.EqualTo(first.CanvasBounds));
            Assert.That(
                repeated.StartNodeId,
                Is.EqualTo(first.StartNodeId));
            Assert.That(
                repeated.ExitNodeId,
                Is.EqualTo(first.ExitNodeId));
            CollectionAssert.AreEqual(
                first.MainPathNodeIds,
                repeated.MainPathNodeIds);
            Assert.That(
                repeated.Rooms.Count,
                Is.EqualTo(first.Rooms.Count));
            for (int index = 0;
                 index < first.Rooms.Count;
                 index++)
            {
                P6RoomNode expected = first.Rooms[index];
                P6RoomNode actual = repeated.Rooms[index];
                Assert.That(actual.Id, Is.EqualTo(expected.Id));
                Assert.That(
                    actual.MacroBounds,
                    Is.EqualTo(expected.MacroBounds));
                Assert.That(
                    actual.PrefabId,
                    Is.EqualTo(expected.PrefabId));
                Assert.That(actual.Role, Is.EqualTo(expected.Role));
                Assert.That(
                    actual.OnMainPath,
                    Is.EqualTo(expected.OnMainPath));
            }

            AssertCandidateEdgesEqual(
                first.CandidateEdges,
                repeated.CandidateEdges);
            AssertGraphEdgesEqual(
                first.MstEdges,
                repeated.MstEdges);
            AssertGraphEdgesEqual(
                first.AdditionalEdges,
                repeated.AdditionalEdges);

            Assert.That(
                repeated.CorridorNetwork.RoutingScale,
                Is.EqualTo(
                    first.CorridorNetwork.RoutingScale));
            Assert.That(
                repeated.CorridorNetwork.RoutingBounds,
                Is.EqualTo(
                    first.CorridorNetwork.RoutingBounds));
            CollectionAssert.AreEqual(
                first.CorridorNetwork.Cells,
                repeated.CorridorNetwork.Cells);
            CollectionAssert.AreEqual(
                first.CorridorNetwork.JunctionCells,
                repeated.CorridorNetwork.JunctionCells);
            Assert.That(
                repeated.CorridorNetwork.RoomAccesses.Count,
                Is.EqualTo(
                    first.CorridorNetwork.RoomAccesses.Count));
            for (int index = 0;
                 index < first.CorridorNetwork.RoomAccesses.Count;
                 index++)
            {
                P6RoomAccess expected =
                    first.CorridorNetwork.RoomAccesses[index];
                P6RoomAccess actual =
                    repeated.CorridorNetwork.RoomAccesses[index];
                Assert.That(
                    actual.AccessId,
                    Is.EqualTo(expected.AccessId));
                Assert.That(actual.NodeId, Is.EqualTo(expected.NodeId));
                Assert.That(
                    actual.SocketId,
                    Is.EqualTo(expected.SocketId));
                Assert.That(
                    actual.SocketSide,
                    Is.EqualTo(expected.SocketSide));
                Assert.That(
                    actual.CellOffset,
                    Is.EqualTo(expected.CellOffset));
                Assert.That(
                    actual.OpeningSize,
                    Is.EqualTo(expected.OpeningSize));
                Assert.That(
                    actual.TraversalType,
                    Is.EqualTo(expected.TraversalType));
                Assert.That(
                    actual.MainRouteAllowed,
                    Is.EqualTo(expected.MainRouteAllowed));
                Assert.That(
                    actual.ValidationAnchor,
                    Is.EqualTo(expected.ValidationAnchor));
                Assert.That(
                    actual.SocketCell,
                    Is.EqualTo(expected.SocketCell));
                Assert.That(
                    actual.PortalCell,
                    Is.EqualTo(expected.PortalCell));
            }
        }

        private static void AssertCandidateEdgesEqual(
            IReadOnlyList<P6CandidateEdge> first,
            IReadOnlyList<P6CandidateEdge> repeated)
        {
            Assert.That(repeated.Count, Is.EqualTo(first.Count));
            for (int index = 0; index < first.Count; index++)
            {
                Assert.That(
                    repeated[index].FirstNodeId,
                    Is.EqualTo(first[index].FirstNodeId));
                Assert.That(
                    repeated[index].SecondNodeId,
                    Is.EqualTo(first[index].SecondNodeId));
                Assert.That(
                    repeated[index].Weight,
                    Is.EqualTo(first[index].Weight));
            }
        }

        private static void AssertGraphEdgesEqual(
            IReadOnlyList<P6GraphEdge> first,
            IReadOnlyList<P6GraphEdge> repeated)
        {
            Assert.That(repeated.Count, Is.EqualTo(first.Count));
            for (int index = 0; index < first.Count; index++)
            {
                Assert.That(
                    repeated[index].FirstNodeId,
                    Is.EqualTo(first[index].FirstNodeId));
                Assert.That(
                    repeated[index].SecondNodeId,
                    Is.EqualTo(first[index].SecondNodeId));
                Assert.That(
                    repeated[index].FirstAccessId,
                    Is.EqualTo(first[index].FirstAccessId));
                Assert.That(
                    repeated[index].SecondAccessId,
                    Is.EqualTo(first[index].SecondAccessId));
                Assert.That(
                    repeated[index].Weight,
                    Is.EqualTo(first[index].Weight));
                Assert.That(
                    repeated[index].Kind,
                    Is.EqualTo(first[index].Kind));
                CollectionAssert.AreEqual(
                    first[index].CorridorCells,
                    repeated[index].CorridorCells);
            }
        }

        private static ReferenceMst BuildReferenceMst(
            int nodeCount,
            IReadOnlyList<P6CandidateEdge> candidates)
        {
            P6CandidateEdge[] ordered = candidates
                .OrderBy(edge => edge.Weight)
                .ThenBy(edge => edge.FirstNodeId)
                .ThenBy(edge => edge.SecondNodeId)
                .ToArray();
            TestDisjointSet sets =
                new TestDisjointSet(nodeCount);
            HashSet<string> keys =
                new HashSet<string>(StringComparer.Ordinal);
            int totalWeight = 0;

            for (int index = 0;
                 index < ordered.Length
                    && keys.Count < nodeCount - 1;
                 index++)
            {
                P6CandidateEdge edge = ordered[index];
                if (!sets.Join(
                        edge.FirstNodeId,
                        edge.SecondNodeId))
                {
                    continue;
                }

                keys.Add(
                    EdgeKey(
                        edge.FirstNodeId,
                        edge.SecondNodeId));
                totalWeight += edge.Weight;
            }

            Assert.That(
                keys.Count,
                Is.EqualTo(nodeCount - 1),
                "Candidate graph has no spanning tree.");
            return new ReferenceMst(keys, totalWeight);
        }

        private static bool IsConnected(P6RoomGraphPlan plan)
        {
            List<int>[] adjacency =
                Enumerable.Range(0, plan.Rooms.Count)
                    .Select(_ => new List<int>())
                    .ToArray();
            foreach (P6GraphEdge edge in plan.Edges)
            {
                adjacency[edge.FirstNodeId].Add(edge.SecondNodeId);
                adjacency[edge.SecondNodeId].Add(edge.FirstNodeId);
            }

            HashSet<int> visited = new HashSet<int>();
            Queue<int> queue = new Queue<int>();
            visited.Add(plan.StartNodeId);
            queue.Enqueue(plan.StartNodeId);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                foreach (int next in adjacency[current])
                {
                    if (visited.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            return visited.Count == plan.Rooms.Count
                && visited.Contains(plan.ExitNodeId);
        }

        private static void AssertRequiredX2Role(
            P6RoomGraphPlan plan,
            RoomRole role)
        {
            P6RoomNode[] matches = plan.Rooms
                .Where(room => (room.Role & role) != 0)
                .ToArray();
            Assert.That(
                matches,
                Is.Not.Empty,
                $"X-2 is missing assigned role {role}.");
            foreach (P6RoomNode room in matches)
            {
                Assert.That(
                    (room.SupportedRoles & role) != 0,
                    Is.True,
                    $"{room.PrefabId} does not author role {role}.");
            }
        }

        private static void AssertMaruStatueRoomsCanReachExit(
            P6RoomGraphPlan plan)
        {
            int[] exitHops = CalculateExitHopDistances(plan);
            foreach (P6RoomNode statue in plan.Rooms.Where(
                P6RoomGraphValidator.HostsMaruStatue))
            {
                Assert.That(
                    statue.OnMainPath,
                    Is.False,
                    "MaruStatue must remain optional.");
                int accesses = plan.CorridorNetwork.RoomAccesses
                    .Count(access => access.NodeId == statue.Id);
                bool shortcut =
                    exitHops[statue.Id] >= 0
                    && exitHops[statue.Id]
                        <= P6RoomGraphValidator.MaruStatueShortcutMaxHops;
                Assert.That(
                    accesses
                        >= P6RoomGraphValidator.MaruStatueMinimumAccesses
                        || shortcut,
                    Is.True,
                    "MaruStatue needs two exits or one direct Exit "
                    + $"shortcut: {statue.PrefabId} accesses={accesses} "
                    + $"exitHops={exitHops[statue.Id]}");
            }
        }

        private static int[] CalculateExitHopDistances(
            P6RoomGraphPlan plan)
        {
            int[] distances = new int[plan.Rooms.Count];
            List<int>[] adjacency = new List<int>[plan.Rooms.Count];
            for (int index = 0; index < distances.Length; index++)
            {
                distances[index] = -1;
                adjacency[index] = new List<int>();
            }

            foreach (P6GraphEdge edge in plan.Edges)
            {
                adjacency[edge.FirstNodeId].Add(edge.SecondNodeId);
                adjacency[edge.SecondNodeId].Add(edge.FirstNodeId);
            }

            Queue<int> frontier = new Queue<int>();
            distances[plan.ExitNodeId] = 0;
            frontier.Enqueue(plan.ExitNodeId);
            while (frontier.Count > 0)
            {
                int current = frontier.Dequeue();
                foreach (int next in adjacency[current])
                {
                    if (distances[next] >= 0)
                    {
                        continue;
                    }

                    distances[next] = distances[current] + 1;
                    frontier.Enqueue(next);
                }
            }

            return distances;
        }

        private static int[] CalculateDegrees(
            P6RoomGraphPlan plan)
        {
            int[] degrees = new int[plan.Rooms.Count];
            foreach (P6GraphEdge edge in plan.Edges)
            {
                degrees[edge.FirstNodeId]++;
                degrees[edge.SecondNodeId]++;
            }

            return degrees;
        }

        private static int IndexOf(
            IReadOnlyList<int> values,
            int target)
        {
            for (int index = 0; index < values.Count; index++)
            {
                if (values[index] == target)
                {
                    return index;
                }
            }

            return -1;
        }

        private static P6RoomPrefabDescriptor[] LoadMoonDescriptors()
        {
            RoomPrefabLibrary library =
                AssetDatabase.LoadAssetAtPath<RoomPrefabLibrary>(
                    MoonLibraryPath);
            Assert.That(
                library,
                Is.Not.Null,
                $"Missing P4 room library: {MoonLibraryPath}");

            return library.GetTemplates()
                .Where(template => template != null)
                .OrderBy(
                    template => template.RoomId,
                    StringComparer.Ordinal)
                .Select(template =>
                    P6RoomTraversalProofFactory.CreateDescriptor(
                        template,
                        selectionWeight: 1,
                        maxUses: 2))
                .ToArray();
        }

        private static P6SocketSides GetSocketSides(
            RoomTemplate2D template)
        {
            P6SocketSides result = P6SocketSides.None;
            for (int index = 0;
                 index < template.Sockets.Count;
                 index++)
            {
                RoomSocket2D socket = template.Sockets[index];
                switch (socket.Side)
                {
                    case RoomSocketSide.Left:
                        result |= P6SocketSides.Left;
                        break;
                    case RoomSocketSide.Right:
                        result |= P6SocketSides.Right;
                        break;
                    case RoomSocketSide.Bottom:
                        result |= P6SocketSides.Bottom;
                        break;
                    case RoomSocketSide.Top:
                        result |= P6SocketSides.Top;
                        break;
                }
            }

            return result;
        }

        private static bool Contains(
            RectInt outer,
            RectInt inner)
        {
            return inner.xMin >= outer.xMin
                && inner.yMin >= outer.yMin
                && inner.xMax <= outer.xMax
                && inner.yMax <= outer.yMax;
        }

        private static Vector2Int MacroCenter(RectInt bounds)
        {
            return new Vector2Int(
                bounds.xMin + (bounds.width - 1) / 2,
                bounds.yMin + (bounds.height - 1) / 2);
        }

        private static int Manhattan(
            Vector2Int first,
            Vector2Int second)
        {
            return Mathf.Abs(first.x - second.x)
                + Mathf.Abs(first.y - second.y);
        }

        private static string EdgeKey(int first, int second)
        {
            int low = Mathf.Min(first, second);
            int high = Mathf.Max(first, second);
            return $"{low}:{high}";
        }

        private sealed class TestDisjointSet
        {
            private readonly int[] parent;
            private readonly byte[] rank;

            public TestDisjointSet(int count)
            {
                parent = new int[count];
                rank = new byte[count];
                for (int index = 0; index < count; index++)
                {
                    parent[index] = index;
                }
            }

            public bool Join(int first, int second)
            {
                first = Find(first);
                second = Find(second);
                if (first == second)
                {
                    return false;
                }

                if (rank[first] < rank[second])
                {
                    parent[first] = second;
                }
                else
                {
                    parent[second] = first;
                    if (rank[first] == rank[second])
                    {
                        rank[first]++;
                    }
                }

                return true;
            }

            private int Find(int value)
            {
                while (parent[value] != value)
                {
                    parent[value] = parent[parent[value]];
                    value = parent[value];
                }

                return value;
            }
        }

        private sealed class ReferenceMst
        {
            public ReferenceMst(
                HashSet<string> edgeKeys,
                int totalWeight)
            {
                EdgeKeys = edgeKeys;
                TotalWeight = totalWeight;
            }

            public HashSet<string> EdgeKeys { get; }
            public int TotalWeight { get; }
        }

        private sealed class SeedGateSummary
        {
            private readonly List<string> samples =
                new List<string>();
            private readonly SortedDictionary<int, int>
                roomCountHistogram =
                    new SortedDictionary<int, int>();
            private long totalAttempts;
            private int maxAttempts;

            public SeedGateSummary(int seedCount)
            {
                SeedCount = seedCount;
            }

            public int SeedCount { get; }
            public int RejectedSeeds { get; private set; }
            public int StartExitFailures { get; private set; }
            public int PrefabRepeatSeeds { get; private set; }
            public int X2LoopFailures { get; private set; }
            public int StructuralFailures { get; private set; }
            public int RoleFailures { get; private set; }
            public int OneCellOpeningFailures { get; private set; }
            public int JumpEnvelopeFailures { get; private set; }
            public int ToolFreeMainPathFailures { get; private set; }
            public int ExitBlockProofFailures { get; private set; }
            public int PhysicalTraversalFailures { get; private set; }
            public int RiskyChoiceFlagFailures { get; private set; }
            public int LandmarkPriorityFlagFailures { get; private set; }
            public int X3RoleSequenceFlagFailures { get; private set; }
            public int DirectRiskyChoiceFailures { get; private set; }
            public int DirectLandmarkPriorityFailures { get; private set; }
            public int MinimumRoomCount { get; private set; } =
                int.MaxValue;
            public int MaximumRoomCount { get; private set; } =
                int.MinValue;
            public int ObservedRoomCountTotal =>
                roomCountHistogram.Values.Sum();
            public HashSet<P6StageArchetype> Archetypes { get; } =
                new HashSet<P6StageArchetype>();
            public double ElapsedMilliseconds { get; set; }

            public int RoomCount(int roomCount)
            {
                roomCountHistogram.TryGetValue(
                    roomCount,
                    out int observed);
                return observed;
            }

            public void Observe(
                int seed,
                P6GenerationResult result)
            {
                if (result == null || !result.Accepted)
                {
                    RejectedSeeds++;
                    AddSample(
                        $"seed {seed}: rejected "
                        + $"{result?.Failure} "
                        + $"{result?.FailureReason}");
                    return;
                }

                totalAttempts += result.Attempts;
                maxAttempts = Mathf.Max(maxAttempts, result.Attempts);
                P6RoomGraphPlan plan = result.Plan;
                Archetypes.Add(plan.Archetype);
                MinimumRoomCount =
                    Mathf.Min(MinimumRoomCount, plan.Rooms.Count);
                MaximumRoomCount =
                    Mathf.Max(MaximumRoomCount, plan.Rooms.Count);
                roomCountHistogram.TryGetValue(
                    plan.Rooms.Count,
                    out int roomCount);
                roomCountHistogram[plan.Rooms.Count] =
                    roomCount + 1;

                if (!result.Validation.StartExitReachable)
                {
                    StartExitFailures++;
                    AddSample($"seed {seed}: Start cannot reach Exit");
                }

                if (plan.Rooms
                    .GroupBy(room => room.PrefabId)
                    .Any(group => group.Count() >= 3))
                {
                    PrefabRepeatSeeds++;
                    AddSample($"seed {seed}: prefab used 3+ times");
                }

                if (!plan.HasClosedLoop
                    || plan.AdditionalEdges.Count < 1)
                {
                    X2LoopFailures++;
                    AddSample($"seed {seed}: X-2 loop missing");
                }

                bool structural =
                    result.Validation.Passed
                    && result.Validation.PackingSatisfied
                    && result.Validation.OptionalRoomsReturnable
                    && result.Validation.MainPathLengthSatisfied
                    && result.Validation.PrefabUsageSatisfied
                    && result.Validation.CorridorNetworkConnected
                    && result.Validation.SocketClaimsUnique
                    && result.Validation.RiskyChoiceSatisfied
                    && result.Validation.LandmarkPrioritySatisfied
                    && result.Validation.X3RoleSequenceSatisfied
                    && plan.CorridorNetwork != null
                    && plan.CorridorNetwork.RoutingScale == 2
                    && plan.Rooms.All(room =>
                        room.HasValidatedTraversalProof)
                    && DirectRiskyChoiceSatisfied(plan)
                    && DirectLandmarkPrioritySatisfied(plan)
                    && plan.Rooms.Count >= 10
                    && plan.Rooms.Count <= 14
                    && plan.MstEdges.Count
                        == plan.Rooms.Count - 1
                    && plan.AdditionalEdges.Count >= 1
                    && plan.AdditionalEdges.Count <= 3
                    && plan.MainPathNodeIds.Count >= 6
                    && plan.MainPathNodeIds.Count <= 9;
                if (!structural)
                {
                    StructuralFailures++;
                    AddSample($"seed {seed}: structural contract failed");
                }

                RoomRole assignedRoles = plan.Rooms
                    .Aggregate(
                        RoomRole.None,
                        (roles, room) => roles | room.Role);
                RoomRole required =
                    RoomRole.Landmark
                    | RoomRole.ShopCorridor
                    | RoomRole.RecordRoom
                    | RoomRole.MaruStatue;
                if ((assignedRoles & required) != required)
                {
                    RoleFailures++;
                    AddSample($"seed {seed}: X-2 role missing");
                }

                bool proofOrPhysicalFailure = false;
                if (!result.Validation.OneCellOpeningsSatisfied)
                {
                    OneCellOpeningFailures++;
                    proofOrPhysicalFailure = true;
                }
                if (!result.Validation.JumpEnvelopeSatisfied)
                {
                    JumpEnvelopeFailures++;
                    proofOrPhysicalFailure = true;
                }
                if (!result.Validation.ToolFreeMainPathSatisfied)
                {
                    ToolFreeMainPathFailures++;
                    proofOrPhysicalFailure = true;
                }
                if (!result.Validation.ExitBlockProofSatisfied)
                {
                    ExitBlockProofFailures++;
                    proofOrPhysicalFailure = true;
                }
                if (!result.Validation.PhysicalTraversalSatisfied)
                {
                    PhysicalTraversalFailures++;
                    proofOrPhysicalFailure = true;
                }
                if (proofOrPhysicalFailure)
                {
                    AddSample(
                        $"seed {seed}: traversal/composite flag failed");
                }

                bool semanticFailure = false;
                if (!result.Validation.RiskyChoiceSatisfied)
                {
                    RiskyChoiceFlagFailures++;
                    semanticFailure = true;
                }
                if (!result.Validation.LandmarkPrioritySatisfied)
                {
                    LandmarkPriorityFlagFailures++;
                    semanticFailure = true;
                }
                if (!result.Validation.X3RoleSequenceSatisfied)
                {
                    X3RoleSequenceFlagFailures++;
                    semanticFailure = true;
                }
                if (!DirectRiskyChoiceSatisfied(plan))
                {
                    DirectRiskyChoiceFailures++;
                    semanticFailure = true;
                }
                if (!DirectLandmarkPrioritySatisfied(plan))
                {
                    DirectLandmarkPriorityFailures++;
                    semanticFailure = true;
                }
                if (semanticFailure)
                {
                    AddSample(
                        $"seed {seed}: semantic role contract failed");
                }
            }

            public override string ToString()
            {
                int accepted = SeedCount - RejectedSeeds;
                double averageAttempts = accepted > 0
                    ? totalAttempts / (double)accepted
                    : 0d;
                string sampleText = samples.Count == 0
                    ? "none"
                    : string.Join(" | ", samples);
                string histogramText =
                    roomCountHistogram.Count == 0
                        ? "none"
                        : string.Join(
                            ",",
                            roomCountHistogram.Select(pair =>
                                $"{pair.Key}:{pair.Value}"));
                return "P6 seed gate "
                    + $"count={SeedCount}, "
                    + $"elapsedMs={ElapsedMilliseconds:F1}, "
                    + $"rejected={RejectedSeeds}, "
                    + $"startExitFailures={StartExitFailures}, "
                    + $"prefab3PlusSeeds={PrefabRepeatSeeds}, "
                    + $"loopFailures={X2LoopFailures}, "
                    + $"structuralFailures={StructuralFailures}, "
                    + $"roleFailures={RoleFailures}, "
                    + $"oneCellFailures={OneCellOpeningFailures}, "
                    + $"jumpFailures={JumpEnvelopeFailures}, "
                    + $"toolFreeFailures={ToolFreeMainPathFailures}, "
                    + $"exitBlockFailures={ExitBlockProofFailures}, "
                    + $"physicalFailures={PhysicalTraversalFailures}, "
                    + $"riskyFlagFailures={RiskyChoiceFlagFailures}, "
                    + $"landmarkFlagFailures={LandmarkPriorityFlagFailures}, "
                    + $"x3SequenceFailures={X3RoleSequenceFlagFailures}, "
                    + $"directRiskyFailures={DirectRiskyChoiceFailures}, "
                    + $"directLandmarkFailures="
                    + $"{DirectLandmarkPriorityFailures}, "
                    + $"roomCountMin={MinimumRoomCount}, "
                    + $"roomCountMax={MaximumRoomCount}, "
                    + $"roomCountHistogram={histogramText}, "
                    + $"averageAttempts={averageAttempts:F3}, "
                    + $"maxAttempts={maxAttempts}, "
                    + $"archetypes={Archetypes.Count}, "
                    + $"samples={sampleText}";
            }

            private void AddSample(string sample)
            {
                if (samples.Count < 8)
                {
                    samples.Add(sample);
                }
            }
        }

        private static bool IsSupportedMacroSize(Vector2Int size)
        {
            return size == Vector2Int.one
                || size == new Vector2Int(2, 1)
                || size == new Vector2Int(1, 2)
                || size == new Vector2Int(2, 2);
        }
    }
}

#endif
