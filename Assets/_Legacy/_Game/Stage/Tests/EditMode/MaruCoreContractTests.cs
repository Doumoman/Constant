#if LEGACY_DISABLED
using NUnit.Framework;
using StarNight.Stage.Data;
using StarNight.Stage.Maru;
using StarNight.Stage.Rooms;
using UnityEngine;

namespace StarNight.Stage.Tests
{
    public sealed class MaruCoreContractTests
    {
        [Test]
        public void RoomGraphMovesOneAdjacentStepTowardTarget()
        {
            RoomRuntime roomA = CreateRoom("A", 0f);
            RoomRuntime roomB = CreateRoom("B", 24f);
            RoomRuntime roomC = CreateRoom("C", 48f);
            var graph = new StageRoomGraph();
            graph.AddRoom(roomA, true);
            graph.AddRoom(roomB);
            graph.AddRoom(roomC);
            graph.ConnectBidirectional("A", "B");
            graph.ConnectBidirectional("B", "C");

            string firstStep = graph.GetNextStepToward("A", "C");

            Assert.That(firstStep, Is.EqualTo("B"));
            Assert.That(graph.AreAdjacent("A", firstStep), Is.True);
            Assert.That(graph.AreAdjacent("A", "C"), Is.False);
            Object.DestroyImmediate(roomA.gameObject);
            Object.DestroyImmediate(roomB.gameObject);
            Object.DestroyImmediate(roomC.gameObject);
        }

        [Test]
        public void AccessibilityExtendsEveryBellThresholdByExactlyTwentyFivePercent()
        {
            StageDefinition definition = ScriptableObject.CreateInstance<StageDefinition>();
            definition.kind = StageKind.Exploration;
            definition.bell1Time = 120f;
            definition.bell2Time = 165f;
            definition.maruSpawnTime = 195f;

            Assert.That(MaruTimeline.Evaluate(definition, 119.99f, false), Is.EqualTo(BellPhase.None));
            Assert.That(MaruTimeline.Evaluate(definition, 120f, false), Is.EqualTo(BellPhase.First));
            Assert.That(MaruTimeline.Evaluate(definition, 149.99f, true), Is.EqualTo(BellPhase.None));
            Assert.That(MaruTimeline.Evaluate(definition, 150f, true), Is.EqualTo(BellPhase.First));
            Assert.That(MaruTimeline.Evaluate(definition, 206.25f, true), Is.EqualTo(BellPhase.Second));
            Assert.That(MaruTimeline.Evaluate(definition, 243.75f, true), Is.EqualTo(BellPhase.Maru));
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void TravelerAssistExtendsFirstBiteEscapeWindowToOnePointEightSeconds()
        {
            Assert.That(MaruDirector.GetEscapeDuration(false), Is.EqualTo(1.2f));
            Assert.That(MaruDirector.GetEscapeDuration(true), Is.EqualTo(1.8f));
        }

        private static RoomRuntime CreateRoom(string id, float x)
        {
            var owner = new GameObject("Room_" + id);
            owner.transform.position = new Vector3(x, 0f, 0f);
            RoomRuntime room = owner.AddComponent<RoomRuntime>();
            room.Configure(id, new Vector2Int(24, 8), RoomCameraMode.Fixed,
                null, null, null, null, null, null, null, null, null, null, null);
            return room;
        }
    }
}

#endif
