#if LEGACY_DISABLED
using System;
using System.Linq;
using NUnit.Framework;
using StarNight.Core.State;
using StarNight.Stage.Data;
using StarNight.Stage.Exit;
using StarNight.Stage.Guidance;
using UnityEngine;

namespace StarNight.Stage.Tests
{
    public sealed class StageFlowContractTests
    {
        private StageDefinition source;
        private StageDefinition target;

        [SetUp]
        public void SetUp()
        {
            source = ScriptableObject.CreateInstance<StageDefinition>();
            target = ScriptableObject.CreateInstance<StageDefinition>();
            target.stageId = "1-1";
            target.sceneName = "11_Moon_1_1";
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(source);
            UnityEngine.Object.DestroyImmediate(target);
        }

        [Test]
        public void StageDefinitionContainsCore05SerializedContract()
        {
            string[] expected =
            {
                "stageId", "displayNameKey", "sceneName", "regionId", "kind", "generationMode",
                "minRooms", "maxRooms", "bell1Time", "bell2Time", "maruSpawnTime", "introYarnNode",
                "exitYarnNode", "connections", "artProfile",
            };
            string[] fields = typeof(StageDefinition).GetFields().Select(field => field.Name).ToArray();

            CollectionAssert.IsSubsetOf(expected, fields);
            Assert.That(source.minRooms, Is.EqualTo(2));
            Assert.That(source.maxRooms, Is.EqualTo(2));
        }

        [Test]
        public void ConnectionConditionsRespectFlagItemAndBossState()
        {
            RunState run = RunState.CreateNew(7);
            StageRuntimeState stage = StageRuntimeState.Create(source, 7, "Room_A");
            var connection = new StageConnection { target = target, condition = ConnectionCondition.Always };
            Assert.That(StageConnectionEvaluator.IsAvailable(connection, run, stage), Is.True);

            connection.condition = ConnectionCondition.RequiredFlag;
            connection.requiredFlag = "route_open";
            Assert.That(StageConnectionEvaluator.IsAvailable(connection, run, stage), Is.False);
            run.flags.Add("route_open");
            Assert.That(StageConnectionEvaluator.IsAvailable(connection, run, stage), Is.True);

            connection.condition = ConnectionCondition.RequiredItem;
            connection.requiredItem = "moon_key";
            Assert.That(StageConnectionEvaluator.IsAvailable(connection, run, stage), Is.False);
            run.items.Add("moon_key");
            Assert.That(StageConnectionEvaluator.IsAvailable(connection, run, stage), Is.True);

            connection.condition = ConnectionCondition.BossResolved;
            Assert.That(StageConnectionEvaluator.IsAvailable(connection, run, stage), Is.False);
            stage.bossResolved = true;
            Assert.That(StageConnectionEvaluator.IsAvailable(connection, run, stage), Is.True);
        }

        [Test]
        public void ExitGuidanceUsesShortestMainRouteAndPointsToDoorInExitRoom()
        {
            var service = new ExitGuidanceService();
            service.Configure(
                new[]
                {
                    new StageRouteRoom("A", new Vector2(0f, 0f)),
                    new StageRouteRoom("B", new Vector2(1f, 0f)),
                    new StageRouteRoom("C", new Vector2(2f, 0f)),
                    new StageRouteRoom("D", new Vector2(0f, 1f)),
                },
                new[]
                {
                    new StageRouteEdge("A", "B"),
                    new StageRouteEdge("B", "C"),
                    new StageRouteEdge("A", "D"),
                    new StageRouteEdge("D", "C", false),
                },
                "C");

            ExitGuidance fromStart = service.GetGuidance("A");
            ExitGuidance atExit = service.GetGuidance("C");
            Assert.That(fromStart.IsValid, Is.True);
            Assert.That(fromStart.NextRoomId, Is.EqualTo("B"));
            Assert.That(fromStart.Direction, Is.EqualTo(Vector2Int.right));
            Assert.That(atExit.ExitInCurrentRoom, Is.True);
            Assert.That(service.MarkExitDiscovered(), Is.True);
            Assert.That(service.MarkExitDiscovered(), Is.False);
        }

        [Test]
        public void DepartureDoorConstantsMatchInteractionContract()
        {
            Assert.That(StageExitDoor.InteractionDistance, Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(StageExitDoor.HoldSeconds, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(StageExitDoor.ExitAnimationSeconds, Is.EqualTo(0.45f).Within(0.0001f));
            Assert.That(StageExitDoor.PromptText, Is.EqualTo("[X] 출항하기"));
        }
    }
}

#endif
