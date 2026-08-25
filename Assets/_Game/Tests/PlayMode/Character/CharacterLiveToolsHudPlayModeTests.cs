using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Character.Interaction;
using StarNight.Character.Live.Adapters;
using StarNight.Character.Live.Hud;
using StarNight.Character.Live.Presentation;
using StarNight.Character.Live.Run;
using StarNight.Character.Live.Tools;
using StarNight.Character.Equipment;
using StarNight.Character.Integration;
using StarNight.Character.MapIntegration;
using StarNight.Character.Presentation;
using StarNight.Map.WorldGeneration.Domain;
using UnityEngine;

namespace StarNight.Character.Tests.PlayMode
{
    /// <summary>
    /// L03_01 도구 소비자 + L03_02 HUD/연출 스모크(인메모리 더블).
    /// 수락 경로 정확히 1회 소비, 거부·중복 무변조, 지형/로프 산출이
    /// 명령 데이터로만 남는지(Tilemap/씬 무접촉) 검증한다.
    /// </summary>
    public sealed class CharacterLiveToolsHudPlayModeTests
    {
        private sealed class FakeCarryTarget : ICharacterLiveCarryTarget
        {
            public int Id { get; set; }
            public CharacterCarryCandidateKind Kind { get; set; }
            public bool IsActive { get; set; } = true;
            public bool IsCarried { get; set; }
            public Vector2 Position { get; set; }
            public float WidthInCells { get; set; } = 1f;
            public float HeightInCells { get; set; } = 1f;
            public bool IsCarryable { get; set; } = true;
            public int Priority { get; set; }

            public int AttachCount;
            public int ReleaseCount;
            public Vector2 LastReleaseVelocity;

            public void AttachTo(int carrierId)
            {
                AttachCount++;
                IsCarried = true;
            }

            public void ReleaseAt(
                Vector2 position, Vector2 initialVelocity, float grace)
            {
                ReleaseCount++;
                LastReleaseVelocity = initialVelocity;
                IsCarried = false;
            }
        }

        private sealed class FakePlacementQuery : ICharacterPlacementSpaceQuery
        {
            public bool Free = true;

            public bool IsPlacementFree(Vector2 position)
            {
                return Free;
            }
        }

        private static CharacterLiveMapWorldQueryAdapter BuildWorld()
        {
            // 바닥 GroundSolid y=0 x0..11, 파괴 가능 (5,2)/(6,1), 나머지 생성-빈.
            var cells = new Dictionary<long, CharacterMapCellState>();
            for (int x = 0; x <= 11; x++)
            {
                cells[CharacterLiveMapWorldQueryAdapter.Key(x, 0)] =
                    new CharacterMapCellState(true, false, false, false, false);
                for (int y = 1; y <= 7; y++)
                {
                    cells[CharacterLiveMapWorldQueryAdapter.Key(x, y)] =
                        CharacterMapCellState.Empty;
                }
            }

            var breakable = new CharacterMapCellState(true, false, false, false, true);
            cells[CharacterLiveMapWorldQueryAdapter.Key(5, 2)] = breakable;
            cells[CharacterLiveMapWorldQueryAdapter.Key(6, 1)] = breakable;
            return new CharacterLiveMapWorldQueryAdapter(cells);
        }

        private static CharacterLiveRunSession StartSession()
        {
            WorldTileCoord startCell, minCell, maxCell;
            WorldCoordinateUtility.TryCreateWorldTile(5, 1, out startCell);
            WorldCoordinateUtility.TryCreateWorldTile(0, 0, out minCell);
            WorldCoordinateUtility.TryCreateWorldTile(11, 7, out maxCell);
            var startSnapshot = new CharacterGeneratedMapStartSnapshot(
                1, CharacterRoomId.FromWorldTile(minCell), true,
                startCell, minCell, maxCell);
            CharacterPlayerSpawnRequest spawnRequest;
            CharacterIntegrationDiagnostic diagnostic;
            Assert.IsTrue(CharacterSpawnIntegrationPolicy.TryCreateSpawnRequest(
                in startSnapshot, 1, out spawnRequest, out diagnostic));
            var session = new CharacterLiveRunSession();
            Assert.IsTrue(session.TryStartRun(in spawnRequest));
            return session;
        }

        [Test]
        public void Carry_AcceptOnce_DuplicateAndInvalidDoNotMutate()
        {
            var ledger = new CharacterLiveToolRequestLedger();
            var placement = new FakePlacementQuery();
            var carry = new CharacterLiveCarryConsumer(
                CharacterCarryInteractionSettings.Default, placement, 1, 1.5f, ledger);
            var targetA = new FakeCarryTarget
            { Id = 1, Position = new Vector2(5.5f, 1.5f) };
            var oversized = new FakeCarryTarget
            { Id = 2, Position = new Vector2(5.6f, 1.5f), WidthInCells = 2f };
            var carried = new FakeCarryTarget
            { Id = 3, Position = new Vector2(5.7f, 1.5f), IsCarried = true };
            var carrier = new Vector2(5.4f, 1.5f);
            var targets = new List<ICharacterLiveCarryTarget>
            { oversized, carried, targetA };

            Assert.IsTrue(carry.TryConsumeCarry(1, carrier, targets).Accepted);
            Assert.AreEqual(1, targetA.AttachCount);
            Assert.AreEqual(1, carry.HeldObjectId);

            var duplicate = carry.TryConsumeCarry(1, carrier, targets);
            Assert.IsFalse(duplicate.Accepted);
            Assert.AreEqual(
                CharacterLiveToolDiagnosticKind.DuplicateRequest,
                duplicate.Diagnostic);
            Assert.AreEqual(1, targetA.AttachCount);

            Assert.AreEqual(
                CharacterLiveToolDiagnosticKind.AlreadyCarrying,
                carry.TryConsumeCarry(2, carrier, targets).Diagnostic);

            // 투척: 캐릭터 계약 방향×속력 그대로, 중복은 두 번 던지지 않는다.
            Assert.IsTrue(carry.TryConsumeThrow(
                1, CharacterThrowDirection.Right, carrier).Accepted);
            Assert.AreEqual(1, targetA.ReleaseCount);
            Assert.AreEqual(new Vector2(7f, 0f), targetA.LastReleaseVelocity);
            Assert.IsFalse(carry.TryConsumeThrow(
                1, CharacterThrowDirection.Right, carrier).Accepted);
            Assert.AreEqual(1, targetA.ReleaseCount);

            // 부적격 거부 진단(무변조).
            Assert.AreEqual(
                CharacterLiveToolDiagnosticKind.TargetAlreadyCarried,
                carry.TryConsumeCarry(3, carrier,
                    new List<ICharacterLiveCarryTarget> { carried }).Diagnostic);
            Assert.AreEqual(
                CharacterLiveToolDiagnosticKind.InvalidCarryTarget,
                carry.TryConsumeCarry(4, carrier,
                    new List<ICharacterLiveCarryTarget> { oversized }).Diagnostic);
            Assert.AreEqual(
                CharacterLiveToolDiagnosticKind.NoCarryTarget,
                carry.TryConsumeCarry(5, carrier,
                    new List<ICharacterLiveCarryTarget>()).Diagnostic);

            // 내려놓기: 수락 1회 → 빈 슬롯 거부 → 막힌 목적지는 슬롯 유지.
            Assert.IsTrue(carry.TryConsumeCarry(6, carrier, targets).Accepted);
            Assert.IsTrue(carry.TryConsumeDrop(1, carrier).Accepted);
            Assert.AreEqual(2, targetA.ReleaseCount);
            Assert.AreEqual(Vector2.zero, targetA.LastReleaseVelocity);
            Assert.AreEqual(
                CharacterLiveToolDiagnosticKind.NoCarriedTarget,
                carry.TryConsumeDrop(2, carrier).Diagnostic);
            Assert.IsTrue(carry.TryConsumeCarry(7, carrier, targets).Accepted);
            placement.Free = false;
            Assert.AreEqual(
                CharacterLiveToolDiagnosticKind.BlockedDrop,
                carry.TryConsumeDrop(2, carrier).Diagnostic);
            Assert.IsTrue(carry.IsCarrying);
            Assert.AreEqual(2, targetA.ReleaseCount);
        }

        [Test]
        public void BombAndRope_SpendAndQueueExactlyOnce_CommandDataOnly()
        {
            var ledger = new CharacterLiveToolRequestLedger();
            var world = BuildWorld();
            var session = StartSession();

            var terrainQueue = new CharacterLiveTerrainCommandQueue();
            var bomb = new CharacterLiveBombConsumer(
                session, terrainQueue, world, CharacterBombSettings.Default, ledger);

            Assert.IsTrue(bomb.TryConsumeBomb(1, new Vector2(5.5f, 1.5f)).Accepted);
            Assert.AreEqual(3, session.RunState.Inventory.BombCount);
            Assert.AreEqual(1, bomb.ActiveFuseCount);
            Assert.IsFalse(bomb.TryConsumeBomb(1, new Vector2(5.5f, 1.5f)).Accepted);
            Assert.AreEqual(3, session.RunState.Inventory.BombCount);
            Assert.AreEqual(
                CharacterLiveToolDiagnosticKind.InvalidBombPlacement,
                bomb.TryConsumeBomb(2, new Vector2(30.5f, 3.5f)).Diagnostic);

            Assert.AreEqual(0, bomb.TickFuses(1.0f));
            Assert.AreEqual(1, bomb.TickFuses(1.5f));
            Assert.AreEqual(0, bomb.TickFuses(2.5f));
            Assert.AreEqual(1, terrainQueue.PendingCount);

            CharacterLiveTerrainCommand terrainCommand;
            Assert.IsTrue(terrainQueue.TryDequeue(out terrainCommand));
            Assert.AreEqual(2, terrainCommand.Mutations.Count);
            Assert.AreEqual(
                CharacterTerrainMutationIntent.DestroyBreakable,
                terrainCommand.Mutations[0].Intent);

            bomb.TryConsumeBomb(2, new Vector2(4.5f, 1.5f));
            bomb.TryConsumeBomb(3, new Vector2(3.5f, 1.5f));
            bomb.TryConsumeBomb(4, new Vector2(2.5f, 1.5f));
            Assert.AreEqual(0, session.RunState.Inventory.BombCount);
            Assert.AreEqual(
                CharacterLiveToolDiagnosticKind.NoBombStock,
                bomb.TryConsumeBomb(5, new Vector2(1.5f, 1.5f)).Diagnostic);
            Assert.AreEqual(
                CharacterLiveToolDiagnosticKind.MissingTerrainSink,
                new CharacterLiveBombConsumer(
                    session, null, world, CharacterBombSettings.Default, ledger)
                    .TryConsumeBomb(99, new Vector2(5.5f, 1.5f)).Diagnostic);

            var ropeQueue = new CharacterLiveRopeCommandQueue();
            var rope = new CharacterLiveRopeConsumer(
                session, ropeQueue, world, CharacterRopeSettings.Default, ledger);

            Assert.IsTrue(rope.TryConsumeRope(1, new Vector2(8.5f, 1.5f)).Accepted);
            Assert.AreEqual(3, session.RunState.Inventory.RopeCount);
            Assert.AreEqual(6, rope.LastSegmentCount);
            Assert.IsFalse(rope.TryConsumeRope(1, new Vector2(8.5f, 1.5f)).Accepted);
            Assert.AreEqual(1, ropeQueue.PendingCount);
            Assert.AreEqual(
                CharacterLiveToolDiagnosticKind.BlockedRopeAnchor,
                rope.TryConsumeRope(2, new Vector2(3.5f, 0.5f)).Diagnostic);
            Assert.AreEqual(
                CharacterLiveToolDiagnosticKind.InvalidRopeAnchor,
                rope.TryConsumeRope(2, new Vector2(30.5f, 3.5f)).Diagnostic);
            Assert.IsTrue(rope.TryConsumeRope(2, new Vector2(5.5f, 1.5f)).Accepted);
            Assert.AreEqual(1, rope.LastSegmentCount);

            rope.TryConsumeRope(3, new Vector2(8.5f, 2.5f));
            rope.TryConsumeRope(4, new Vector2(9.5f, 1.5f));
            Assert.AreEqual(0, session.RunState.Inventory.RopeCount);
            Assert.AreEqual(
                CharacterLiveToolDiagnosticKind.NoRopeStock,
                rope.TryConsumeRope(5, new Vector2(7.5f, 1.5f)).Diagnostic);
            Assert.AreEqual(
                CharacterLiveToolDiagnosticKind.MissingRopeSink,
                new CharacterLiveRopeConsumer(
                    session, null, world, CharacterRopeSettings.Default, ledger)
                    .TryConsumeRope(99, new Vector2(8.5f, 1.5f)).Diagnostic);

            CharacterLiveRopeCommand ropeCommand;
            Assert.IsTrue(ropeQueue.TryDequeue(out ropeCommand));
            Assert.AreEqual(6, ropeCommand.Segments.Count);
            Assert.AreEqual(8, ropeCommand.Segments[0].Cell.X);
            Assert.AreEqual(1, ropeCommand.Segments[0].Cell.Y);
        }

        [Test]
        public void HudSnapshot_ProjectsRunData_AndPresentationFeedbackOnce()
        {
            var log = new CharacterLiveFeedbackLog();

            var empty = CharacterLiveHudSnapshotSource.Project(null, log);
            Assert.IsFalse(empty.HasRunData);
            Assert.AreEqual("NO RUN", empty.RunStatusLabel);
            Assert.AreEqual("-", empty.RoomLabel);
            Assert.AreEqual(string.Empty, empty.LatestFeedback);

            var session = StartSession();
            var snapshot = CharacterLiveHudSnapshotSource.Project(session, log);
            Assert.IsTrue(snapshot.HasRunData);
            Assert.AreEqual(4, snapshot.CurrentHealth);
            Assert.AreEqual(4, snapshot.MaxHealth);
            Assert.AreEqual(4, snapshot.BombCount);
            Assert.AreEqual(4, snapshot.RopeCount);
            Assert.AreEqual("Active", snapshot.RunStatusLabel);
            Assert.AreEqual("S0,0 C0,0", snapshot.RoomLabel);

            WorldTileCoord cell;
            WorldCoordinateUtility.TryCreateWorldTile(5, 1, out cell);
            var raw = new List<CharacterPresentationEventRequest>
            {
                new CharacterPresentationEventRequest(
                    CharacterPresentationEventType.BombPlaced,
                    1, false, 0, true, cell, 0),
                new CharacterPresentationEventRequest(
                    CharacterPresentationEventType.Damage,
                    1, true, 1, false, default(WorldTileCoord), 0),
                new CharacterPresentationEventRequest(
                    CharacterPresentationEventType.Damage,
                    1, true, 1, false, default(WorldTileCoord), 0),
                new CharacterPresentationEventRequest(
                    CharacterPresentationEventType.RunFailure,
                    1, false, 0, false, default(WorldTileCoord), 0)
            };
            var consumer = new CharacterLivePresentationEventConsumer(session, log);
            Assert.AreEqual(3, consumer.ConsumeBatch(raw));
            Assert.AreEqual(1, consumer.DuplicateEventCount);

            // 우선순위 순서(런 실패 → 피해 → 설치) + 중복 피해 1건.
            Assert.AreEqual("RUN FAILURE", log.GetMessage(0).Text);
            Assert.AreEqual("DAMAGE -1", log.GetMessage(1).Text);
            Assert.AreEqual("BOMB PLACED (5,1)", log.GetMessage(2).Text);
            Assert.AreEqual(3, log.Count);

            var withFeedback = CharacterLiveHudSnapshotSource.Project(session, log);
            Assert.AreEqual("BOMB PLACED (5,1)", withFeedback.LatestFeedback);
        }
    }
}
