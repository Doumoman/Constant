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
    public sealed class CharacterGeneratedRoomValidationTests
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

        private static bool HasKind(
            CharacterGeneratedRunValidationResult result,
            CharacterGeneratedRunValidationDiagnosticKind kind)
        {
            return result.Diagnostics.Any(diagnostic => diagnostic.Kind == kind);
        }

        [Test]
        public void GeneratedRoom_MicrochunksStayWithinRoomAndWorldBounds()
        {
            var inventory = new CharacterRunInventoryState(
                CharacterGeneratedRunFixtures.ActorId, 4, 4);

            // (1) 유효 런: 방 2 + 정렬 마이크로청크 2 → 진단 0, 전부 통과.
            var valid = Validate(
                CharacterGeneratedRunFixtures.ValidRun(11), inventory,
                CharacterGeneratedRunFixtures.ReadyRooms());
            Assert.That(valid.Diagnostics, Is.Empty);
            Assert.That(valid.Passed, Is.True);

            // (2) 격자 비정렬 마이크로청크(min (1,0)) → MicrochunkMisaligned.
            var misaligned = CharacterGeneratedRunFixtures.Custom(
                12, CharacterGeneratedRunFixtures.Start(12),
                CharacterGeneratedRunFixtures.DefaultRooms(),
                new List<CharacterGeneratedMicrochunkSnapshot>
                {
                    new CharacterGeneratedMicrochunkSnapshot(
                        CharacterGeneratedRunFixtures.RoomA,
                        new WorldTileCoord(1, 0), new WorldTileCoord(12, 7))
                },
                new List<CharacterGeneratedRouteEdgeSnapshot>(),
                new List<CharacterGeneratedItemPlacementSnapshot>());
            Assert.That(HasKind(
                Validate(misaligned, inventory,
                    CharacterGeneratedRunFixtures.ReadyRooms()),
                CharacterGeneratedRunValidationDiagnosticKind.MicrochunkMisaligned),
                Is.True);

            // (3) 소유 방 경계 밖(방 A 소유인데 방 B 구역 셀) →
            //     MicrochunkOutsideOwnerRoom.
            var outsideOwner = CharacterGeneratedRunFixtures.Custom(
                13, CharacterGeneratedRunFixtures.Start(13),
                CharacterGeneratedRunFixtures.DefaultRooms(),
                new List<CharacterGeneratedMicrochunkSnapshot>
                {
                    new CharacterGeneratedMicrochunkSnapshot(
                        CharacterGeneratedRunFixtures.RoomA,
                        new WorldTileCoord(12, 0), new WorldTileCoord(23, 7))
                },
                new List<CharacterGeneratedRouteEdgeSnapshot>(),
                new List<CharacterGeneratedItemPlacementSnapshot>());
            Assert.That(HasKind(
                Validate(outsideOwner, inventory,
                    CharacterGeneratedRunFixtures.ReadyRooms()),
                CharacterGeneratedRunValidationDiagnosticKind.MicrochunkOutsideOwnerRoom),
                Is.True);

            // (4) 같은 방 중복 점유 → DuplicateMicrochunkOccupancy.
            var duplicated = CharacterGeneratedRunFixtures.Custom(
                14, CharacterGeneratedRunFixtures.Start(14),
                CharacterGeneratedRunFixtures.DefaultRooms(),
                new List<CharacterGeneratedMicrochunkSnapshot>
                {
                    new CharacterGeneratedMicrochunkSnapshot(
                        CharacterGeneratedRunFixtures.RoomA,
                        new WorldTileCoord(0, 0), new WorldTileCoord(11, 7)),
                    new CharacterGeneratedMicrochunkSnapshot(
                        CharacterGeneratedRunFixtures.RoomA,
                        new WorldTileCoord(0, 0), new WorldTileCoord(11, 7))
                },
                new List<CharacterGeneratedRouteEdgeSnapshot>(),
                new List<CharacterGeneratedItemPlacementSnapshot>());
            Assert.That(HasKind(
                Validate(duplicated, inventory,
                    CharacterGeneratedRunFixtures.ReadyRooms()),
                CharacterGeneratedRunValidationDiagnosticKind.DuplicateMicrochunkOccupancy),
                Is.True);

            // (5) 월드 밖 방 경계 → RoomOutsideWorldBounds; 방 ID 중복 →
            //     DuplicateRoomId.
            var badRooms = CharacterGeneratedRunFixtures.Custom(
                15, CharacterGeneratedRunFixtures.Start(15),
                new List<CharacterGeneratedRoomSnapshot>
                {
                    new CharacterGeneratedRoomSnapshot(
                        CharacterGeneratedRunFixtures.RoomA,
                        new WorldTileCoord(0, 0), new WorldTileCoord(11, 7)),
                    new CharacterGeneratedRoomSnapshot(
                        CharacterGeneratedRunFixtures.RoomA,
                        new WorldTileCoord(0, 0), new WorldTileCoord(700, 7))
                },
                new List<CharacterGeneratedMicrochunkSnapshot>(),
                new List<CharacterGeneratedRouteEdgeSnapshot>(),
                new List<CharacterGeneratedItemPlacementSnapshot>());
            var badRoomsResult = Validate(badRooms, inventory,
                CharacterGeneratedRunFixtures.ReadyRooms());
            Assert.That(HasKind(badRoomsResult,
                CharacterGeneratedRunValidationDiagnosticKind.DuplicateRoomId),
                Is.True);
            Assert.That(HasKind(badRoomsResult,
                CharacterGeneratedRunValidationDiagnosticKind.RoomOutsideWorldBounds),
                Is.True);
        }

        [Test]
        public void GeneratedRoom_RoutesReferenceExistingRoomsAndCreateCharacterRequests()
        {
            var inventory = new CharacterRunInventoryState(
                CharacterGeneratedRunFixtures.ActorId, 4, 4);

            // (1) 유효 루트 → CHAR06_01 위임으로 스폰 1 + 전환 요청 1 생성.
            var valid = Validate(
                CharacterGeneratedRunFixtures.ValidRun(11), inventory,
                CharacterGeneratedRunFixtures.ReadyRooms());
            Assert.That(valid.SpawnRequestCount, Is.EqualTo(1));
            Assert.That(valid.RouteRequestCount, Is.EqualTo(1));

            // (2) 존재하지 않는 방을 참조하는 루트 → RouteRoomMissing.
            var missingRoom = CharacterGeneratedRunFixtures.Custom(
                16, CharacterGeneratedRunFixtures.Start(16),
                CharacterGeneratedRunFixtures.DefaultRooms(),
                CharacterGeneratedRunFixtures.DefaultMicrochunks(),
                new List<CharacterGeneratedRouteEdgeSnapshot>
                {
                    new CharacterGeneratedRouteEdgeSnapshot(
                        8, CharacterGeneratedRunFixtures.RoomA,
                        CharacterGeneratedRunFixtures.RoomMissing,
                        CharacterRouteBoundarySide.Up,
                        new WorldTileCoord(5, 7), new WorldTileCoord(5, 8),
                        CharacterRouteRequirement.BasicMovement)
                },
                new List<CharacterGeneratedItemPlacementSnapshot>());
            Assert.That(HasKind(
                Validate(missingRoom, inventory,
                    CharacterGeneratedRunFixtures.ReadyRooms()),
                CharacterGeneratedRunValidationDiagnosticKind.RouteRoomMissing),
                Is.True);

            // (3) 선언 방 밖의 이탈 셀 → RouteCellOutsideDeclaredRoom.
            var badCell = CharacterGeneratedRunFixtures.Custom(
                17, CharacterGeneratedRunFixtures.Start(17),
                CharacterGeneratedRunFixtures.DefaultRooms(),
                CharacterGeneratedRunFixtures.DefaultMicrochunks(),
                new List<CharacterGeneratedRouteEdgeSnapshot>
                {
                    new CharacterGeneratedRouteEdgeSnapshot(
                        9, CharacterGeneratedRunFixtures.RoomA,
                        CharacterGeneratedRunFixtures.RoomB,
                        CharacterRouteBoundarySide.Right,
                        new WorldTileCoord(20, 3), new WorldTileCoord(12, 3),
                        CharacterRouteRequirement.BasicMovement)
                },
                new List<CharacterGeneratedItemPlacementSnapshot>());
            Assert.That(HasKind(
                Validate(badCell, inventory,
                    CharacterGeneratedRunFixtures.ReadyRooms()),
                CharacterGeneratedRunValidationDiagnosticKind.RouteCellOutsideDeclaredRoom),
                Is.True);

            // (4) 도착 방 미준비 → CHAR06_01 게이트 거부가 IntegrationRejected로
            //     흘러온다(위임 증거).
            var unprepared = new CharacterGeneratedRunFixtures.FakeReadinessSource();
            unprepared.SetRoom(CharacterGeneratedRunFixtures.RoomA, true);
            unprepared.SetRoom(CharacterGeneratedRunFixtures.RoomB, false);
            var gated = Validate(
                CharacterGeneratedRunFixtures.ValidRun(18), inventory, unprepared);
            Assert.That(gated.RouteRequestCount, Is.EqualTo(0));
            Assert.That(gated.Diagnostics.Any(diagnostic =>
                diagnostic.Kind == CharacterGeneratedRunValidationDiagnosticKind
                    .IntegrationRejected
                && diagnostic.Subject.Contains("RouteBlockedUnpreparedRoom")),
                Is.True);
        }
    }
}
