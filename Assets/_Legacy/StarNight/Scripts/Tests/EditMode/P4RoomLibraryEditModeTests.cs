#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Rooms;
using UnityEditor;
using UnityEngine;

namespace StarNight.Tests.EditMode
{
    public sealed class P4RoomLibraryEditModeTests
    {
        private const string RoomPrefabFolder =
            "Assets/StarNight/Prefabs/Rooms/P4";
        private const int RequiredRoomCount = 33;

        [Test]
        public void TraversalSolver_ThreeCellFlatJump_Succeeds()
        {
            RoomTraversalSnapshot snapshot =
                CreateIsolatedPlatforms(
                    new Vector2Int(1, 1),
                    new Vector2Int(4, 1));

            Assert.That(
                RoomTraversalSolver.CanReach(
                    snapshot,
                    new Vector2Int(1, 1),
                    new Vector2Int(4, 1)),
                Is.True,
                "The P1 movement contract permits about 3.2 cells of horizontal reach.");
        }

        [Test]
        public void TraversalSolver_FourCellFlatJump_Fails()
        {
            RoomTraversalSnapshot snapshot =
                CreateIsolatedPlatforms(
                    new Vector2Int(1, 1),
                    new Vector2Int(5, 1));

            Assert.That(
                RoomTraversalSolver.CanReach(
                    snapshot,
                    new Vector2Int(1, 1),
                    new Vector2Int(5, 1)),
                Is.False,
                "A four-cell gap exceeds the P1 horizontal jump contract.");
        }

        [Test]
        public void TraversalSolver_TwoAcrossTwoUp_Succeeds()
        {
            RoomTraversalSnapshot snapshot =
                CreateIsolatedPlatforms(
                    new Vector2Int(1, 1),
                    new Vector2Int(3, 3));

            Assert.That(
                RoomTraversalSolver.CanReach(
                    snapshot,
                    new Vector2Int(1, 1),
                    new Vector2Int(3, 3)),
                Is.True,
                "A two-cell rise at two cells across must fit the held-jump arc.");
        }

        [Test]
        public void TraversalSolver_ThreeAcrossOneUp_Fails()
        {
            RoomTraversalSnapshot snapshot =
                CreateIsolatedPlatforms(
                    new Vector2Int(1, 1),
                    new Vector2Int(4, 2));

            Assert.That(
                RoomTraversalSolver.CanReach(
                    snapshot,
                    new Vector2Int(1, 1),
                    new Vector2Int(4, 2)),
                Is.False,
                "Reaching three cells across while rising one cell requires more than the P1 run speed.");
        }

        [Test]
        public void TraversalSolver_OneCellHighTunnel_Succeeds()
        {
            RoomTraversalSnapshot snapshot =
                new RoomTraversalSnapshot(new Vector2Int(7, 4));
            for (int x = 0; x < 7; x++)
            {
                snapshot.AddTerrain(new Vector2Int(x, 0));
                snapshot.AddTerrain(new Vector2Int(x, 2));
            }

            Assert.That(
                RoomTraversalSolver.CanReach(
                    snapshot,
                    new Vector2Int(1, 1),
                    new Vector2Int(5, 1)),
                Is.True,
                "The 0.72 x 0.90-cell player must traverse a one-cell-high tunnel.");
        }

        [Test]
        public void TraversalSolver_BlockedJumpArc_Fails()
        {
            Vector2Int start = new Vector2Int(1, 1);
            Vector2Int destination = new Vector2Int(4, 1);
            RoomTraversalSnapshot snapshot =
                CreateIsolatedPlatforms(start, destination);
            for (int y = 1; y <= 4; y++)
            {
                snapshot.AddTerrain(new Vector2Int(2, y));
            }

            Assert.That(
                RoomTraversalSolver.CanReach(snapshot, start, destination),
                Is.False,
                "Solid terrain intersecting the capsule's jump arc must reject the route.");
        }

        [Test]
        public void TraversalSolver_HazardInJumpArc_Fails()
        {
            Vector2Int start = new Vector2Int(1, 1);
            Vector2Int destination = new Vector2Int(4, 1);
            RoomTraversalSnapshot snapshot =
                CreateIsolatedPlatforms(start, destination);
            snapshot.AddHazard(new Vector2Int(2, 2));

            Assert.That(
                RoomTraversalSolver.CanReach(snapshot, start, destination),
                Is.False,
                "An otherwise clear jump must not route the player through a hazard cell.");
        }

        [Test]
        public void TraversalSolver_RepeatedSearch_IsDeterministic()
        {
            RoomTraversalSnapshot snapshot =
                new RoomTraversalSnapshot(new Vector2Int(12, 6));
            for (int x = 0; x < 12; x++)
            {
                snapshot.AddTerrain(new Vector2Int(x, 0));
            }

            Vector2Int start = new Vector2Int(1, 1);
            Vector2Int destination = new Vector2Int(10, 1);
            Assert.That(
                RoomTraversalSolver.TryFindPath(
                    snapshot,
                    start,
                    destination,
                    out IReadOnlyList<Vector2Int> firstPath),
                Is.True);

            Vector2Int[] expected = firstPath.ToArray();
            for (int iteration = 0; iteration < 100; iteration++)
            {
                Assert.That(
                    RoomTraversalSolver.TryFindPath(
                        snapshot,
                        start,
                        destination,
                        out IReadOnlyList<Vector2Int> repeatedPath),
                    Is.True,
                    $"Iteration {iteration} unexpectedly failed.");
                CollectionAssert.AreEqual(
                    expected,
                    repeatedPath,
                    $"Iteration {iteration} returned a different path.");
            }
        }

        [Test]
        public void P4PrefabLibrary_HasExactRequiredCountsWithoutSpecialOverlap()
        {
            IReadOnlyList<RoomTemplate2D> rooms = LoadRoomLibrary();
            Assert.That(
                rooms.Count,
                Is.EqualTo(RequiredRoomCount),
                $"P4 must contain exactly {RequiredRoomCount} prefabs.");

            RoomCounts counts = CountRooms(rooms);
            Assert.That(counts.Small, Is.EqualTo(8));
            Assert.That(counts.Standard, Is.EqualTo(8));
            Assert.That(counts.Wide, Is.EqualTo(5));
            Assert.That(counts.Tall, Is.EqualTo(5));
            Assert.That(counts.Large, Is.EqualTo(3));
            Assert.That(counts.ShopCorridor, Is.EqualTo(1));
            Assert.That(counts.SupplyCorridor, Is.EqualTo(1));
            Assert.That(counts.RecordRoom, Is.EqualTo(1));
            Assert.That(counts.MaruStatue, Is.EqualTo(1));
            Assert.That(
                counts.Total,
                Is.EqualTo(RequiredRoomCount),
                "Special rooms must not also be counted toward the five base archetype quotas.");
        }

        [Test]
        public void P4PrefabLibrary_IdsAndCoreActionsAreUniqueSingleSentences()
        {
            IReadOnlyList<RoomTemplate2D> rooms = LoadRoomLibrary();
            HashSet<string> ids =
                new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> sentences =
                new HashSet<string>(StringComparer.Ordinal);

            foreach (RoomTemplate2D room in rooms)
            {
                Assert.That(room.RoomId, Is.Not.Null.And.Not.Empty);
                Assert.That(
                    ids.Add(room.RoomId),
                    Is.True,
                    $"Duplicate P4 room id: {room.RoomId}");
                Assert.That(
                    RoomPrefabValidator.IsSingleSentence(
                        room.CoreActionSentence),
                    Is.True,
                    $"[{room.RoomId}] Core action must be exactly one sentence.");
                Assert.That(
                    sentences.Add(room.CoreActionSentence),
                    Is.True,
                    $"[{room.RoomId}] Core action sentence is duplicated.");
            }
        }

        [Test]
        public void P4PrefabLibrary_UsesMultipleLogicalSizesInsideEachMacroFamily()
        {
            IReadOnlyList<RoomTemplate2D> rooms = LoadRoomLibrary();
            RoomRole specialRoles =
                RoomRole.RecordRoom
                | RoomRole.MaruStatue
                | RoomRole.ShopCorridor
                | RoomRole.SupplyCorridor;

            AssertLogicalVariety(
                rooms,
                RoomArchetype.Small,
                specialRoles,
                4);
            AssertLogicalVariety(
                rooms,
                RoomArchetype.Standard,
                specialRoles,
                4);
            AssertLogicalVariety(
                rooms,
                RoomArchetype.Wide,
                specialRoles,
                3);
            AssertLogicalVariety(
                rooms,
                RoomArchetype.Tall,
                specialRoles,
                3);
            AssertLogicalVariety(
                rooms,
                RoomArchetype.Large,
                specialRoles,
                2);
        }

        [Test]
        public void P4PrefabLibrary_RecordsTheKnownDocumentedContentShortfall()
        {
            IReadOnlyList<RoomTemplate2D> rooms = LoadRoomLibrary();
            Assert.That(
                RoomPrefabValidator.TryGetContentShortfalls(
                    rooms,
                    out IReadOnlyList<RoomContentQuotaEntry> shortfalls),
                Is.True,
                "월궁 1개 지역만 제작된 상태이므로 문서 22.1 미달 항목이 남아 있어야 한다.");

            string[] actual = shortfalls
                .OrderBy(entry => entry.Category)
                .Select(entry => $"{entry.Category}={entry.Shortfall}")
                .ToArray();
            string[] expected =
            {
                $"{RoomContentQuotaCategory.LongHallDeepShaftRoom}=1",
                $"{RoomContentQuotaCategory.LocalEventRoom}=2",
                $"{RoomContentQuotaCategory.MaruRoom}=1",
                $"{RoomContentQuotaCategory.SecretRoom}=3"
            };
            CollectionAssert.AreEqual(
                expected,
                actual,
                "문서 22.1 대비 부족분이 바뀌었다. 방을 더 만들었다면 이 목록을 갱신하고, "
                + "채워진 항목은 RoomContentQuota에서 LibraryHardRequirement로 승격할 것. "
                + "현재 부족분: "
                + string.Join(", ", actual));
        }

        [Test]
        public void P4PrefabLibrary_MeetsEveryHardValidatedDocumentQuota()
        {
            IReadOnlyList<RoomTemplate2D> rooms = LoadRoomLibrary();
            RoomContentQuotaReport quota =
                RoomPrefabValidator.BuildContentQuotaReport(rooms);

            Assert.That(
                quota.Entries.Count,
                Is.EqualTo(RoomContentQuota.Rules.Count),
                "부족분 리포트는 문서 22.1·22.2의 모든 항목을 담아야 한다.");

            foreach (RoomContentQuotaRule rule in RoomContentQuota.Rules)
            {
                if (!rule.LibraryHardRequirement)
                {
                    continue;
                }

                Assert.That(
                    quota.TryGetEntry(rule.Category, out RoomContentQuotaEntry entry),
                    Is.True,
                    $"{rule.Category} 항목이 리포트에서 빠졌다.");
                Assert.That(
                    entry.Current,
                    Is.GreaterThanOrEqualTo(rule.Minimum),
                    $"{rule.DisplayName}은(는) 하드 검증 대상이므로 문서 최소치 "
                    + $"{rule.Minimum}을(를) 만족해야 한다.");
            }
        }

        [Test]
        public void P4PrefabLibrary_ValidatorPassesEveryRoomAndSocket()
        {
            IReadOnlyList<RoomTemplate2D> rooms = LoadRoomLibrary();
            RoomValidationReport report =
                RoomPrefabValidator.ValidateLibrary(rooms);

            Assert.That(
                report.Passed,
                Is.True,
                report.ToString());
        }

        private static RoomTraversalSnapshot CreateIsolatedPlatforms(
            Vector2Int start,
            Vector2Int destination)
        {
            RoomTraversalSnapshot snapshot =
                new RoomTraversalSnapshot(new Vector2Int(12, 8));
            snapshot.AddTerrain(start + Vector2Int.down);
            snapshot.AddTerrain(destination + Vector2Int.down);
            return snapshot;
        }

        private static IReadOnlyList<RoomTemplate2D> LoadRoomLibrary()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:Prefab",
                new[] { RoomPrefabFolder });
            string[] paths = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.That(
                paths.Length,
                Is.EqualTo(RequiredRoomCount),
                $"Expected exactly {RequiredRoomCount} prefab assets under {RoomPrefabFolder}.");

            List<RoomTemplate2D> rooms =
                new List<RoomTemplate2D>(paths.Length);
            for (int index = 0; index < paths.Length; index++)
            {
                GameObject prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(paths[index]);
                Assert.That(
                    prefab,
                    Is.Not.Null,
                    $"Could not load P4 room prefab: {paths[index]}");

                RoomTemplate2D room =
                    prefab.GetComponentInChildren<RoomTemplate2D>(true);
                Assert.That(
                    room,
                    Is.Not.Null,
                    $"P4 room prefab has no {nameof(RoomTemplate2D)}: {paths[index]}");
                rooms.Add(room);
            }

            return rooms;
        }

        private static void AssertLogicalVariety(
            IReadOnlyList<RoomTemplate2D> rooms,
            RoomArchetype archetype,
            RoomRole excludedRoles,
            int minimumDistinctSizes)
        {
            int distinct = rooms
                .Where(room =>
                    room.Archetype == archetype
                    && (room.Roles & excludedRoles) == 0)
                .Select(room => room.LogicalSize)
                .Distinct()
                .Count();
            Assert.That(
                distinct,
                Is.GreaterThanOrEqualTo(minimumDistinctSizes),
                $"{archetype} rooms need authored logical-size variants "
                + "inside their macro footprint.");
        }

        private static RoomCounts CountRooms(
            IReadOnlyList<RoomTemplate2D> rooms)
        {
            RoomCounts counts = default;
            for (int index = 0; index < rooms.Count; index++)
            {
                RoomTemplate2D room = rooms[index];
                int specialRoleCount = 0;
                if (room.HasRole(RoomRole.ShopCorridor))
                {
                    counts.ShopCorridor++;
                    specialRoleCount++;
                }

                if (room.HasRole(RoomRole.SupplyCorridor))
                {
                    counts.SupplyCorridor++;
                    specialRoleCount++;
                }

                if (room.HasRole(RoomRole.RecordRoom))
                {
                    counts.RecordRoom++;
                    specialRoleCount++;
                }

                if (room.HasRole(RoomRole.MaruStatue))
                {
                    counts.MaruStatue++;
                    specialRoleCount++;
                }

                Assert.That(
                    specialRoleCount,
                    Is.LessThanOrEqualTo(1),
                    $"[{room.RoomId}] A prefab cannot fill multiple P4 special-room quotas.");
                if (specialRoleCount != 0)
                {
                    continue;
                }

                switch (room.Archetype)
                {
                    case RoomArchetype.Small:
                        counts.Small++;
                        break;
                    case RoomArchetype.Standard:
                        counts.Standard++;
                        break;
                    case RoomArchetype.Wide:
                        counts.Wide++;
                        break;
                    case RoomArchetype.Tall:
                        counts.Tall++;
                        break;
                    case RoomArchetype.Large:
                        counts.Large++;
                        break;
                }
            }

            return counts;
        }

        private struct RoomCounts
        {
            public int Small;
            public int Standard;
            public int Wide;
            public int Tall;
            public int Large;
            public int ShopCorridor;
            public int SupplyCorridor;
            public int RecordRoom;
            public int MaruStatue;

            public int Total =>
                Small
                + Standard
                + Wide
                + Tall
                + Large
                + ShopCorridor
                + SupplyCorridor
                + RecordRoom
                + MaruStatue;
        }
    }
}

#endif
