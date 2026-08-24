using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Character.Integration;
using StarNight.Character.MapIntegration;
using StarNight.Character.RunState;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Tests.Integration
{
    public sealed class CharacterRouteCapabilityAndBatchTests
    {
        private const int ActorId = 777;

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

        private static readonly CharacterRoomId RoomC =
            CharacterRoomId.FromWorldTile(new WorldTileCoord(0, 8));

        private static CharacterGeneratedRouteEdgeSnapshot Edge(
            int routeId,
            CharacterRoomId target,
            WorldTileCoord exitCell,
            WorldTileCoord entryCell,
            CharacterRouteBoundarySide side,
            CharacterRouteRequirement requirement)
        {
            return new CharacterGeneratedRouteEdgeSnapshot(
                routeId, RoomA, target, side, exitCell, entryCell, requirement);
        }

        private static CharacterRunInventoryState Inventory(int bombs, int ropes)
        {
            return new CharacterRunInventoryState(ActorId, bombs, ropes);
        }

        [Test]
        public void RouteCapability_BasicMovementRouteIsAccepted()
        {
            CharacterIntegrationDiagnostic diagnostic;

            // 잠금 이동 문법 루트는 인벤토리와 무관하게 수용된다.
            Assert.That(CharacterRouteCapabilityPolicy.IsRouteSupported(
                CharacterRouteRequirement.BasicMovement, Inventory(0, 0), 1,
                out diagnostic), Is.True);
            Assert.That(CharacterRouteCapabilityPolicy.IsRouteSupported(
                CharacterRouteRequirement.BasicMovement, Inventory(4, 4), 1,
                out diagnostic), Is.True);
        }

        [Test]
        public void RouteCapability_ForbiddenMovementOrAttackRequirementsAreRejected()
        {
            CharacterIntegrationDiagnostic diagnostic;

            // 잠금 밖 고급 이동/공격 요구는 보유량과 무관하게 항상 거부된다
            // (대시/벽점프/이중점프/사격/일반공격류는 Unsupported 분류로 사상).
            Assert.That(CharacterRouteCapabilityPolicy.IsRouteSupported(
                CharacterRouteRequirement.UnsupportedAdvancedMovement,
                Inventory(4, 4), 2, out diagnostic), Is.False);
            Assert.That(diagnostic.Kind, Is.EqualTo(
                CharacterIntegrationDiagnosticKind.UnsupportedRouteRequirement));

            Assert.That(CharacterRouteCapabilityPolicy.IsRouteSupported(
                CharacterRouteRequirement.UnsupportedCombatAction,
                Inventory(4, 4), 3, out diagnostic), Is.False);
            Assert.That(diagnostic.Kind, Is.EqualTo(
                CharacterIntegrationDiagnosticKind.UnsupportedRouteRequirement));
        }

        [Test]
        public void RouteCapability_BombAndRopeRequirementsRequireAvailableSupport()
        {
            CharacterIntegrationDiagnostic diagnostic;
            var stocked = Inventory(2, 1);

            // 보유가 있으면 수용.
            Assert.That(CharacterRouteCapabilityPolicy.IsRouteSupported(
                CharacterRouteRequirement.BombSupport, in stocked, 4,
                out diagnostic), Is.True);
            Assert.That(CharacterRouteCapabilityPolicy.IsRouteSupported(
                CharacterRouteRequirement.RopeSupport, in stocked, 5,
                out diagnostic), Is.True);

            // 판정은 진단 전용이다 — 인벤토리를 소모하지 않는다.
            Assert.That(stocked.BombCount, Is.EqualTo(2));
            Assert.That(stocked.RopeCount, Is.EqualTo(1));

            // 보유가 없으면 종류별 진단과 함께 거부.
            Assert.That(CharacterRouteCapabilityPolicy.IsRouteSupported(
                CharacterRouteRequirement.BombSupport, Inventory(0, 4), 4,
                out diagnostic), Is.False);
            Assert.That(diagnostic.Kind, Is.EqualTo(
                CharacterIntegrationDiagnosticKind.MissingBombSupport));

            Assert.That(CharacterRouteCapabilityPolicy.IsRouteSupported(
                CharacterRouteRequirement.RopeSupport, Inventory(4, 0), 5,
                out diagnostic), Is.False);
            Assert.That(diagnostic.Kind, Is.EqualTo(
                CharacterIntegrationDiagnosticKind.MissingRopeSupport));
        }

        [Test]
        public void IntegrationBatch_IsDeterministicOrderedAndDeduplicated()
        {
            var start = new CharacterGeneratedMapStartSnapshot(
                1, RoomA, true, new WorldTileCoord(5, 3),
                new WorldTileCoord(0, 0), new WorldTileCoord(11, 7));

            var basicEdge = Edge(3, RoomB,
                new WorldTileCoord(11, 3), new WorldTileCoord(12, 3),
                CharacterRouteBoundarySide.Right,
                CharacterRouteRequirement.BasicMovement);
            var ropeEdge = Edge(7, RoomC,
                new WorldTileCoord(5, 7), new WorldTileCoord(5, 8),
                CharacterRouteBoundarySide.Up,
                CharacterRouteRequirement.RopeSupport);
            var forbiddenEdge = Edge(9, RoomB,
                new WorldTileCoord(11, 5), new WorldTileCoord(12, 5),
                CharacterRouteBoundarySide.Right,
                CharacterRouteRequirement.UnsupportedAdvancedMovement);

            // 동일 엣지 중복 + 거부 대상 혼재.
            var declared = new List<CharacterGeneratedRouteEdgeSnapshot>
            {
                basicEdge, ropeEdge, basicEdge, forbiddenEdge
            };

            var readiness = new FakeReadinessSource();
            readiness.SetRoom(RoomB, true);
            readiness.SetRoom(RoomC, true);
            var inventory = Inventory(4, 4);

            var spawnFirst = new List<CharacterPlayerSpawnRequest>();
            var routesFirst = new List<CharacterGeneratedRouteTransitionRequest>();
            var diagnosticsFirst = new List<CharacterIntegrationDiagnostic>();

            CharacterIntegrationBatchPolicy.BuildBatch(
                in start, ActorId, declared, in inventory, readiness,
                spawnFirst, routesFirst, diagnosticsFirst);

            // 스폰 1 + 루트 2(중복 basicEdge는 1회) + 거부 진단 1.
            Assert.That(spawnFirst.Count, Is.EqualTo(1));
            Assert.That(routesFirst.Count, Is.EqualTo(2));
            Assert.That(routesFirst[0].RouteId, Is.EqualTo(3));
            Assert.That(routesFirst[1].RouteId, Is.EqualTo(7));
            Assert.That(diagnosticsFirst.Count, Is.EqualTo(1));
            Assert.That(diagnosticsFirst[0].Kind, Is.EqualTo(
                CharacterIntegrationDiagnosticKind.UnsupportedRouteRequirement));

            // 같은 입력이면 반복 호출에도 완전히 같은 출력이다.
            var spawnSecond = new List<CharacterPlayerSpawnRequest>();
            var routesSecond = new List<CharacterGeneratedRouteTransitionRequest>();
            var diagnosticsSecond = new List<CharacterIntegrationDiagnostic>();

            CharacterIntegrationBatchPolicy.BuildBatch(
                in start, ActorId, declared, in inventory, readiness,
                spawnSecond, routesSecond, diagnosticsSecond);

            Assert.That(spawnSecond.Count, Is.EqualTo(spawnFirst.Count));
            Assert.That(routesSecond.Count, Is.EqualTo(routesFirst.Count));
            for (int index = 0; index < routesFirst.Count; index++)
            {
                Assert.That(routesSecond[index].RouteId,
                    Is.EqualTo(routesFirst[index].RouteId));
                Assert.That(routesSecond[index].TargetEntryCell.X,
                    Is.EqualTo(routesFirst[index].TargetEntryCell.X));
            }
            Assert.That(diagnosticsSecond.Count, Is.EqualTo(diagnosticsFirst.Count));

            // 유효하지 않은 시작(월드 밖)도 예외 없이 진단으로 흡수된다.
            var badStart = new CharacterGeneratedMapStartSnapshot(
                2, RoomA, true, new WorldTileCoord(700, 5),
                new WorldTileCoord(0, 0), new WorldTileCoord(11, 7));

            CharacterIntegrationBatchPolicy.BuildBatch(
                in badStart, ActorId, declared, in inventory, readiness,
                spawnSecond, routesSecond, diagnosticsSecond);
            Assert.That(spawnSecond, Is.Empty);
            Assert.That(diagnosticsSecond[0].Kind, Is.EqualTo(
                CharacterIntegrationDiagnosticKind.StartCellOutsideWorldBounds));
        }
    }
}
