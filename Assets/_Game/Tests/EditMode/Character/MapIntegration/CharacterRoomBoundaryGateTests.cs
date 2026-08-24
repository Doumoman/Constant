using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Character.Input;
using StarNight.Character.MapIntegration;
using StarNight.Map.WorldGeneration.Domain;
using UnityEngine;

namespace StarNight.Character.Tests.MapIntegration
{
    public sealed class CharacterRoomBoundaryGateTests
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

        // 마이크로청크는 12×8 — (11,1)→(12,1)은 방 경계 통과, (1,1)→(2,2)는 방 내부.
        private static readonly WorldTileCoord InsideRoomA = new WorldTileCoord(11, 1);
        private static readonly WorldTileCoord AlsoInsideRoomA = new WorldTileCoord(2, 2);
        private static readonly WorldTileCoord InsideRoomB = new WorldTileCoord(12, 1);

        private static WorldTileCoord Tile(int x, int y)
        {
            return new WorldTileCoord(x, y);
        }

        [Test]
        public void BoundaryGate_BlocksUnpreparedDestinationRoom()
        {
            var readiness = new FakeReadinessSource();
            readiness.SetRoom(CharacterRoomId.FromWorldTile(InsideRoomB), false);
            var gate = new CharacterRoomBoundaryGate(readiness);

            var decision = gate.Evaluate(InsideRoomA, InsideRoomB);

            Assert.That(decision,
                Is.EqualTo(CharacterBoundaryCrossDecision.BlockedUnpreparedRoom));
            Assert.That(CharacterRoomBoundaryGate.MayCross(decision), Is.False);
        }

        [Test]
        public void BoundaryGate_BlocksMissingDestinationRoom()
        {
            // 목적지 방 정보가 아예 없는 경우.
            var gate = new CharacterRoomBoundaryGate(new FakeReadinessSource());

            var decision = gate.Evaluate(InsideRoomA, InsideRoomB);

            Assert.That(decision,
                Is.EqualTo(CharacterBoundaryCrossDecision.BlockedMissingRoom));
            Assert.That(CharacterRoomBoundaryGate.MayCross(decision), Is.False);
        }

        [Test]
        public void BoundaryGate_AllowsPreparedDestinationRoom()
        {
            var readiness = new FakeReadinessSource();
            readiness.SetRoom(CharacterRoomId.FromWorldTile(InsideRoomB), true);
            var gate = new CharacterRoomBoundaryGate(readiness);

            var crossing = gate.Evaluate(InsideRoomA, InsideRoomB);

            Assert.That(crossing, Is.EqualTo(CharacterBoundaryCrossDecision.Allowed));
            Assert.That(CharacterRoomBoundaryGate.MayCross(crossing), Is.True);

            // 같은 방 내부 이동은 게이트 무영향(준비 상태 조회조차 불필요).
            var interior = gate.Evaluate(Tile(1, 1), AlsoInsideRoomA);

            Assert.That(interior,
                Is.EqualTo(CharacterBoundaryCrossDecision.NotABoundaryCrossing));
            Assert.That(CharacterRoomBoundaryGate.MayCross(interior), Is.True);
        }

        [Test]
        public void BoundaryGate_DoesNotMutateInputOrVelocity()
        {
            var readiness = new FakeReadinessSource();
            readiness.SetRoom(CharacterRoomId.FromWorldTile(InsideRoomB), false);
            var gate = new CharacterRoomBoundaryGate(readiness);

            // 판정 전후로 입력 스냅샷과 속도가 그대로다 — 게이트 시그니처는
            // 좌표만 받으므로 구조적으로도 변조가 불가능하다.
            var snapshot = new CharacterInputSnapshot(
                0.7f,
                true,
                CharacterButtonSnapshot.Pressed(3L),
                CharacterButtonSnapshot.Idle(3L),
                CharacterButtonSnapshot.Idle(3L),
                CharacterButtonSnapshot.Idle(3L));
            var velocity = new Vector2(3.1f, -4.2f);

            var decision = gate.Evaluate(InsideRoomA, InsideRoomB);

            Assert.That(decision,
                Is.EqualTo(CharacterBoundaryCrossDecision.BlockedUnpreparedRoom));
            Assert.That(snapshot.Horizontal, Is.EqualTo(0.7f));
            Assert.That(snapshot.DownHeld, Is.True);
            Assert.That(snapshot.Jump.PressedThisFrame, Is.True);
            Assert.That(velocity, Is.EqualTo(new Vector2(3.1f, -4.2f)));

            // Evaluate 시그니처가 좌표 외 상태를 받지 않음을 리플렉션으로 고정.
            var parameters = typeof(CharacterRoomBoundaryGate)
                .GetMethod("Evaluate")
                .GetParameters();

            Assert.That(parameters.Length, Is.EqualTo(2));

            foreach (var parameter in parameters)
            {
                Assert.That(parameter.ParameterType,
                    Is.EqualTo(typeof(WorldTileCoord)));
                Assert.That(parameter.ParameterType.IsByRef, Is.False);
            }
        }
    }
}
