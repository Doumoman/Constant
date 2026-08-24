using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Character.Integration;
using StarNight.Character.MapIntegration;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Tests.Integration
{
    public sealed class CharacterGeneratedRouteTests
    {
        private sealed class FakeReadinessSource : ICharacterRoomReadinessSource
        {
            private readonly Dictionary<CharacterRoomId, bool> rooms =
                new Dictionary<CharacterRoomId, bool>();

            public void SetRoom(CharacterRoomId room, bool isReady)
            {
                rooms[room] = isReady;
            }

            public bool TryGetRoomReadiness(CharacterRoomId room, out bool isReady)
            {
                return rooms.TryGetValue(room, out isReady);
            }
        }

        private static readonly CharacterRoomId RoomA =
            CharacterRoomId.FromWorldTile(new WorldTileCoord(0, 0));

        private static readonly CharacterRoomId RoomB =
            CharacterRoomId.FromWorldTile(new WorldTileCoord(12, 0));

        /// <summary>방 A(셀 [0..11])→방 B(셀 [12..23]) 오른쪽 경계 엣지.</summary>
        private static CharacterGeneratedRouteEdgeSnapshot EdgeAToB(
            int routeId = 3,
            CharacterRouteRequirement requirement =
                CharacterRouteRequirement.BasicMovement)
        {
            return new CharacterGeneratedRouteEdgeSnapshot(
                routeId, RoomA, RoomB, CharacterRouteBoundarySide.Right,
                new WorldTileCoord(11, 3), new WorldTileCoord(12, 3), requirement);
        }

        [Test]
        public void GeneratedRoute_DeclaredRouteCreatesTransitionRequest()
        {
            var declared = new List<CharacterGeneratedRouteEdgeSnapshot>
            {
                EdgeAToB()
            };
            var readiness = new FakeReadinessSource();
            readiness.SetRoom(RoomB, true);

            CharacterGeneratedRouteTransitionRequest request;
            CharacterIntegrationDiagnostic diagnostic;

            Assert.That(CharacterRouteIntegrationPolicy
                .TryCreateRouteTransitionRequestForRooms(
                    declared, RoomA, RoomB, readiness,
                    out request, out diagnostic), Is.True);

            Assert.That(request.RouteId, Is.EqualTo(3));
            Assert.That(request.SourceRoom.Equals(RoomA), Is.True);
            Assert.That(request.TargetRoom.Equals(RoomB), Is.True);
            Assert.That(request.BoundarySide,
                Is.EqualTo(CharacterRouteBoundarySide.Right));
            Assert.That(request.TargetEntryCell.X, Is.EqualTo(12));
            Assert.That(request.TargetEntryCell.Y, Is.EqualTo(3));
        }

        [Test]
        public void GeneratedRoute_UndeclaredRouteIsRejected()
        {
            var declared = new List<CharacterGeneratedRouteEdgeSnapshot>
            {
                EdgeAToB()
            };
            var readiness = new FakeReadinessSource();
            readiness.SetRoom(RoomB, true);

            // B→A 역방향은 선언되지 않았다 — 요청 없이 진단만.
            CharacterGeneratedRouteTransitionRequest request;
            CharacterIntegrationDiagnostic diagnostic;

            Assert.That(CharacterRouteIntegrationPolicy
                .TryCreateRouteTransitionRequestForRooms(
                    declared, RoomB, RoomA, readiness,
                    out request, out diagnostic), Is.False);
            Assert.That(diagnostic.Kind,
                Is.EqualTo(CharacterIntegrationDiagnosticKind.UndeclaredRouteEdge));

            // 빈 선언 목록도 동일하게 거부한다.
            Assert.That(CharacterRouteIntegrationPolicy
                .TryCreateRouteTransitionRequestForRooms(
                    new List<CharacterGeneratedRouteEdgeSnapshot>(),
                    RoomA, RoomB, readiness,
                    out request, out diagnostic), Is.False);
            Assert.That(diagnostic.Kind,
                Is.EqualTo(CharacterIntegrationDiagnosticKind.UndeclaredRouteEdge));
        }

        [Test]
        public void GeneratedRoute_RespectsRoomTransitionReadinessContract()
        {
            var edge = EdgeAToB();
            CharacterGeneratedRouteTransitionRequest request;
            CharacterIntegrationDiagnostic diagnostic;

            // (1) 도착 방 정보 없음 — CHAR03 게이트의 BlockedMissingRoom 계약.
            var emptyReadiness = new FakeReadinessSource();
            Assert.That(CharacterRouteIntegrationPolicy
                .TryCreateRouteTransitionRequest(
                    in edge, emptyReadiness, out request, out diagnostic), Is.False);
            Assert.That(diagnostic.Kind,
                Is.EqualTo(CharacterIntegrationDiagnosticKind.RouteBlockedMissingRoom));

            // (2) 도착 방 미준비 — BlockedUnpreparedRoom 계약.
            var unprepared = new FakeReadinessSource();
            unprepared.SetRoom(RoomB, false);
            Assert.That(CharacterRouteIntegrationPolicy
                .TryCreateRouteTransitionRequest(
                    in edge, unprepared, out request, out diagnostic), Is.False);
            Assert.That(diagnostic.Kind,
                Is.EqualTo(CharacterIntegrationDiagnosticKind.RouteBlockedUnpreparedRoom));

            // (3) 준비 완료 — 통과.
            var ready = new FakeReadinessSource();
            ready.SetRoom(RoomB, true);
            Assert.That(CharacterRouteIntegrationPolicy
                .TryCreateRouteTransitionRequest(
                    in edge, ready, out request, out diagnostic), Is.True);

            // (4) KEEP 계약의 구조적 보장: 전환 요청 표면에 입력/속도/카메라
            //     필드 자체가 없다 — 재작성이 불가능하다.
            var memberNames = typeof(CharacterGeneratedRouteTransitionRequest)
                .GetMembers()
                .Select(member => member.Name)
                .ToArray();

            Assert.That(memberNames, Has.None.Contains("Input"));
            Assert.That(memberNames, Has.None.Contains("Velocity"));
            Assert.That(memberNames, Has.None.Contains("Camera"));
        }
    }
}
