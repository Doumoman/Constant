using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Character.GeneratedRunValidation;
using StarNight.Character.Integration;
using StarNight.Character.MapIntegration;
using StarNight.Character.RunState;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Tests.GeneratedRunValidation
{
    public sealed class CharacterGeneratedItemsAndAffordanceTests
    {
        private static CharacterGeneratedRunValidationResult Validate(
            CharacterGeneratedRunSnapshot snapshot,
            CharacterRunInventoryState inventory,
            ICharacterRoomReadinessSource readiness)
        {
            return CharacterGeneratedRunValidationPolicy.Validate(
                snapshot, CharacterGeneratedRunFixtures.ActorId,
                in inventory, readiness);
        }

        private static CharacterGeneratedRunSnapshot RunWithItems(
            int seed,
            params CharacterGeneratedItemPlacementSnapshot[] items)
        {
            return CharacterGeneratedRunFixtures.Custom(
                seed, CharacterGeneratedRunFixtures.Start(seed),
                CharacterGeneratedRunFixtures.DefaultRooms(),
                CharacterGeneratedRunFixtures.DefaultMicrochunks(),
                new List<CharacterGeneratedRouteEdgeSnapshot>
                {
                    CharacterGeneratedRunFixtures.BasicRoute()
                },
                new List<CharacterGeneratedItemPlacementSnapshot>(items));
        }

        [Test]
        public void GeneratedItems_DoNotOccupySpawnEntryExitOrBlockedCells()
        {
            var inventory = new CharacterRunInventoryState(
                CharacterGeneratedRunFixtures.ActorId, 4, 4);
            var readiness = CharacterGeneratedRunFixtures.ReadyRooms();

            // 시드 20 → 시작 셀 (2 + 20%3, 3) = (4,3).
            var reservedCases = new[]
            {
                new WorldTileCoord(4, 3),   // 스폰 셀
                new WorldTileCoord(11, 3),  // 루트 이탈 셀
                new WorldTileCoord(12, 3),  // 루트 진입 셀
                new WorldTileCoord(6, 6)    // 명시 금지 셀
            };

            foreach (var cell in reservedCases)
            {
                var room = cell.X <= 11
                    ? CharacterGeneratedRunFixtures.RoomA
                    : CharacterGeneratedRunFixtures.RoomB;
                var result = Validate(
                    RunWithItems(20,
                        new CharacterGeneratedItemPlacementSnapshot(5, room, cell)),
                    inventory, CharacterGeneratedRunFixtures.ReadyRooms());

                var reserved = result.Diagnostics.Where(diagnostic =>
                    diagnostic.Kind == CharacterGeneratedRunValidationDiagnosticKind
                        .ItemOnReservedCell).ToList();
                Assert.That(reserved.Count, Is.EqualTo(1),
                    "cell " + cell.X + "," + cell.Y);

                // 진단은 아이템 ID·방·셀·사유를 식별한다.
                Assert.That(reserved[0].Subject, Does.Contain("item:5"));
                Assert.That(reserved[0].Subject,
                    Does.Contain("cell:" + cell.X + "," + cell.Y));
            }

            // 선언 방 밖 배치 → ItemOutsideRoomOrWorld; 미존재 방 → ItemRoomMissing.
            var outside = Validate(
                RunWithItems(21,
                    new CharacterGeneratedItemPlacementSnapshot(
                        6, CharacterGeneratedRunFixtures.RoomA,
                        new WorldTileCoord(20, 3))),
                inventory, readiness);
            Assert.That(outside.Diagnostics.Any(diagnostic =>
                diagnostic.Kind == CharacterGeneratedRunValidationDiagnosticKind
                    .ItemOutsideRoomOrWorld), Is.True);

            var missingRoom = Validate(
                RunWithItems(22,
                    new CharacterGeneratedItemPlacementSnapshot(
                        7, CharacterGeneratedRunFixtures.RoomMissing,
                        new WorldTileCoord(3, 10))),
                inventory, readiness);
            Assert.That(missingRoom.Diagnostics.Any(diagnostic =>
                diagnostic.Kind == CharacterGeneratedRunValidationDiagnosticKind
                    .ItemRoomMissing), Is.True);

            // 자유 셀 배치는 진단 없음.
            var free = Validate(
                RunWithItems(23,
                    new CharacterGeneratedItemPlacementSnapshot(
                        8, CharacterGeneratedRunFixtures.RoomA,
                        new WorldTileCoord(8, 2))),
                inventory, readiness);
            Assert.That(free.Diagnostics, Is.Empty);
        }

        [Test]
        public void GeneratedRun_BombAndRopeAffordancesMatchLockedCapabilities()
        {
            var readiness = CharacterGeneratedRunFixtures.ReadyRooms();

            CharacterGeneratedRunSnapshot RunWithRequirement(
                int seed, CharacterRouteRequirement requirement)
            {
                return CharacterGeneratedRunFixtures.Custom(
                    seed, CharacterGeneratedRunFixtures.Start(seed),
                    CharacterGeneratedRunFixtures.DefaultRooms(),
                    CharacterGeneratedRunFixtures.DefaultMicrochunks(),
                    new List<CharacterGeneratedRouteEdgeSnapshot>
                    {
                        CharacterGeneratedRunFixtures.BasicRoute(
                            30, requirement)
                    },
                    new List<CharacterGeneratedItemPlacementSnapshot>());
            }

            // 폭탄 지원 루트: 보유 있으면 요청 생성, 없으면 CHAR06_01 역량
            // 진단이 흘러온다.
            var stocked = new CharacterRunInventoryState(
                CharacterGeneratedRunFixtures.ActorId, 2, 1);
            var bombOk = Validate(
                RunWithRequirement(31, CharacterRouteRequirement.BombSupport),
                stocked, readiness);
            Assert.That(bombOk.RouteRequestCount, Is.EqualTo(1));
            Assert.That(bombOk.Diagnostics, Is.Empty);

            var noBombs = new CharacterRunInventoryState(
                CharacterGeneratedRunFixtures.ActorId, 0, 4);
            var bombRejected = Validate(
                RunWithRequirement(32, CharacterRouteRequirement.BombSupport),
                noBombs, readiness);
            Assert.That(bombRejected.RouteRequestCount, Is.EqualTo(0));
            Assert.That(bombRejected.Diagnostics.Any(diagnostic =>
                diagnostic.Subject.Contains("MissingBombSupport")), Is.True);

            // 로프 지원 루트도 동일 계약.
            var noRopes = new CharacterRunInventoryState(
                CharacterGeneratedRunFixtures.ActorId, 4, 0);
            var ropeRejected = Validate(
                RunWithRequirement(33, CharacterRouteRequirement.RopeSupport),
                noRopes, readiness);
            Assert.That(ropeRejected.RouteRequestCount, Is.EqualTo(0));
            Assert.That(ropeRejected.Diagnostics.Any(diagnostic =>
                diagnostic.Subject.Contains("MissingRopeSupport")), Is.True);

            // 잠금 밖 요구는 CHAR06_01 역량 정책 경유로 항상 거부.
            var unsupported = Validate(
                RunWithRequirement(34,
                    CharacterRouteRequirement.UnsupportedAdvancedMovement),
                stocked, readiness);
            Assert.That(unsupported.RouteRequestCount, Is.EqualTo(0));
            Assert.That(unsupported.Diagnostics.Any(diagnostic =>
                diagnostic.Subject.Contains("UnsupportedRouteRequirement")), Is.True);

            // 어포던스 판정은 인벤토리를 소모하지 않는다.
            Assert.That(stocked.BombCount, Is.EqualTo(2));
            Assert.That(stocked.RopeCount, Is.EqualTo(1));
        }
    }
}
