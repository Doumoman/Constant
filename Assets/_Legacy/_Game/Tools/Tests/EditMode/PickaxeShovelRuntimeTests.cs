#if LEGACY_DISABLED
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.Input;
using StarNight.Interaction.Reactions;
using StarNight.Map;
using StarNight.Tools.Core;
using StarNight.Tools.Pickaxe;
using StarNight.Tools.Shovel;
using UnityEditor;
using UnityEngine;

namespace StarNight.Tools.Tests
{
    public sealed class PickaxeShovelRuntimeTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    Object.DestroyImmediate(createdObjects[index]);
                }
            }
            createdObjects.Clear();
        }

        [Test]
        public void AimResolver_PickaxeUsesUpOrFacing_AndShovelStaysFacing()
        {
            var pickaxe = new ToolActionProfile { AimMode = ToolAimMode.UpOrFacing };
            var shovel = new ToolActionProfile { AimMode = ToolAimMode.Facing };

            ToolAimSolution pickaxeUp = ToolAimResolver.Resolve(pickaxe, new Vector2Int(3, 4), -1, 1f);
            ToolAimSolution pickaxeFacing = ToolAimResolver.Resolve(pickaxe, new Vector2Int(3, 4), -1, 0f);
            ToolAimSolution shovelFacing = ToolAimResolver.Resolve(shovel, new Vector2Int(3, 4), -1, 1f);

            Assert.That(pickaxeUp.TargetCell, Is.EqualTo(new Vector2Int(3, 5)));
            Assert.That(pickaxeFacing.TargetCell, Is.EqualTo(new Vector2Int(2, 4)));
            Assert.That(shovelFacing.TargetCell, Is.EqualTo(new Vector2Int(2, 4)));
        }

        [Test]
        public void ToolResourceState_ConsumesOnlySuccessfulReaction()
        {
            var state = new ToolResourceState();
            state.ConfigureForTests(ToolResourceMode.Durability, 12, 12);

            Assert.That(state.TryConsumeForSuccessfulReaction(false), Is.False);
            Assert.That(state.Current, Is.EqualTo(12));
            Assert.That(state.TryConsumeForSuccessfulReaction(true), Is.True);
            Assert.That(state.Current, Is.EqualTo(11));
        }

        [Test]
        public void Timeline_DispatchesOneImpact_AndConsumesExactlyOneDurability()
        {
            RuntimeRig rig = CreateRig<PickaxeRuntime>(CreateDefinition(
                "TOOL_PICKAXE",
                ToolTag.Pickaxe | ToolTag.LightImpact,
                ToolAimMode.UpOrFacing,
                12,
                0.10f,
                0.12f,
                0.22f,
                0.55f),
                ToolDispatchReportForAcceptedMap());

            bool started = rig.Controller.TryStart(
                rig.Tool,
                rig.HandSlot,
                new PlayerActionContext(101, 0f, 1f, false),
                -1,
                true);
            rig.Controller.TickForTests(0.11f);
            Assert.That(rig.World.DispatchCount, Is.Zero);
            rig.Controller.TickForTests(0.02f);
            rig.Controller.TickForTests(1f);

            Assert.That(started, Is.True);
            Assert.That(rig.World.DispatchCount, Is.EqualTo(1));
            Assert.That(rig.Tool.CurrentResource, Is.EqualTo(11));
            Assert.That(rig.Controller.IsUsingTool, Is.False);
            Assert.That(rig.ActionLock.State, Is.EqualTo(PlayerActionState.Carrying));
        }

        [Test]
        public void ToolJudgementIsIdenticalAtThirtyAndSixtyFps()
        {
            TimelineOutcome atThirty = RunAcceptedTimeline(1f / 30f, 130);
            TimelineOutcome atSixty = RunAcceptedTimeline(1f / 60f, 160);

            Assert.That(atThirty.Started, Is.True);
            Assert.That(atSixty.Started, Is.True);
            Assert.That(atThirty.DispatchCount, Is.EqualTo(1));
            Assert.That(atSixty.DispatchCount, Is.EqualTo(atThirty.DispatchCount));
            Assert.That(atSixty.RemainingResource, Is.EqualTo(atThirty.RemainingResource));
            Assert.That(atSixty.IsUsingTool, Is.EqualTo(atThirty.IsUsingTool));
            Assert.That(atSixty.FinalState, Is.EqualTo(atThirty.FinalState));
        }

        [Test]
        public void EmptyOrRejectedCell_DoesNotConsumeDurability()
        {
            RuntimeRig rig = CreateRig<ShovelRuntime>(CreateDefinition(
                "TOOL_SHOVEL",
                ToolTag.Shovel | ToolTag.LightImpact,
                ToolAimMode.Facing,
                10,
                0.14f,
                0.15f,
                0.25f,
                1f),
                ToolDispatchReport.Rejected());

            Assert.That(rig.Controller.TryStart(
                rig.Tool,
                rig.HandSlot,
                new PlayerActionContext(201, 0f, 1f, false),
                1,
                true), Is.True);
            rig.Controller.TickForTests(1f);

            Assert.That(rig.World.DispatchCount, Is.EqualTo(1));
            Assert.That(rig.Tool.CurrentResource, Is.EqualTo(10));
        }

        [Test]
        public void UnbreakableAndDuplicateEntityHit_DoNotDoubleConsumeOrDamage()
        {
            GameObject dispatcherObject = Track(new GameObject("Dispatcher"));
            ToolReactionDispatcher dispatcher = dispatcherObject.AddComponent<ToolReactionDispatcher>();
            var request = new ToolDispatchRequest(
                301,
                ToolTag.Pickaxe | ToolTag.LightImpact,
                Vector2Int.zero,
                Vector2Int.right,
                Vector2Int.right,
                1f,
                dispatcherObject,
                dispatcherObject,
                Vector2.zero,
                1f);

            ToolDispatchReport blocked = dispatcher.DispatchDirect(request, null, null, true);
            var damageTarget = new DamageProbe();
            ToolDispatchReport first = dispatcher.DispatchDirect(request, null, damageTarget, false);
            ToolDispatchReport duplicate = dispatcher.DispatchDirect(request, null, damageTarget, false);

            Assert.That(blocked.Feedback, Is.EqualTo(FeedbackId.MetalFail));
            Assert.That(blocked.ConsumeToolResource, Is.False);
            Assert.That(first.ConsumeToolResource, Is.True);
            Assert.That(duplicate.ConsumeToolResource, Is.False);
            Assert.That(damageTarget.AcceptedCount, Is.EqualTo(1));
        }

        [Test]
        public void AuthoredPrefabsAndPlayerController_AreConnected()
        {
            GameObject pickaxe = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Tools/Prefabs/HandTools/Pickaxe.prefab");
            GameObject shovel = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Tools/Prefabs/HandTools/Shovel.prefab");
            GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Player/Prefabs/Player.prefab");

            Assert.That(pickaxe, Is.Not.Null);
            Assert.That(pickaxe.GetComponent<PickaxeRuntime>(), Is.Not.Null);
            Assert.That(shovel, Is.Not.Null);
            Assert.That(shovel.GetComponent<ShovelRuntime>(), Is.Not.Null);
            Assert.That(player.GetComponent<ToolReactionDispatcher>(), Is.Not.Null);
            Assert.That(player.GetComponent<ToolActionController>(), Is.Not.Null);
        }

        private RuntimeRig CreateRig<T>(HandToolDefinition definition, ToolDispatchReport report)
            where T : HandToolRuntime
        {
            GameObject player = Track(new GameObject("Player"));
            PlayerActionLock actionLock = player.AddComponent<PlayerActionLock>();
            HandSlotPresenter presenter = player.AddComponent<HandSlotPresenter>();
            PlayerHandSlot handSlot = player.AddComponent<PlayerHandSlot>();
            ToolActionController controller = player.AddComponent<ToolActionController>();
            GameObject socket = Track(new GameObject("CarrySocket"));
            socket.transform.SetParent(player.transform, false);
            presenter.ConfigureForTests(socket.transform);
            handSlot.ConfigureForTests(presenter);

            GameObject toolObject = Track(new GameObject(typeof(T).Name));
            T tool = toolObject.AddComponent<T>();
            tool.Configure(definition);
            Assert.That(handSlot.TryAttach(tool), Is.True);
            actionLock.SetState(PlayerActionState.Carrying);
            var world = new FakeReactionWorld(report);
            controller.ConfigureForTests(actionLock, world, Vector2.zero);
            return new RuntimeRig(controller, handSlot, actionLock, tool, world);
        }

        private TimelineOutcome RunAcceptedTimeline(float deltaSeconds, long actionId)
        {
            RuntimeRig rig = CreateRig<PickaxeRuntime>(CreateDefinition(
                "TOOL_PICKAXE_FPS_" + actionId,
                ToolTag.Pickaxe | ToolTag.LightImpact,
                ToolAimMode.UpOrFacing,
                12,
                0.10f,
                0.12f,
                0.22f,
                0.55f),
                ToolDispatchReportForAcceptedMap());
            bool started = rig.Controller.TryStart(
                rig.Tool,
                rig.HandSlot,
                new PlayerActionContext(actionId, 0f, 1f, false),
                1,
                true);
            int steps = Mathf.CeilToInt(1f / deltaSeconds);
            for (int index = 0; index < steps; index++)
            {
                rig.Controller.TickForTests(deltaSeconds);
            }
            return new TimelineOutcome(
                started,
                rig.World.DispatchCount,
                rig.Tool.CurrentResource,
                rig.Controller.IsUsingTool,
                rig.ActionLock.State);
        }

        private HandToolDefinition CreateDefinition(
            string id,
            ToolTag tags,
            ToolAimMode aim,
            int durability,
            float windup,
            float impact,
            float recovery,
            float movement)
        {
            HandToolDefinition definition = Track(ScriptableObject.CreateInstance<HandToolDefinition>());
            var profile = new ToolActionProfile
            {
                WindupSeconds = windup,
                ImpactSeconds = impact,
                ActiveSeconds = impact,
                RecoverySeconds = recovery,
                MovementMultiplier = movement,
                AimMode = aim,
            };
            definition.Configure(
                id,
                id,
                tags,
                ToolResourceMode.Durability,
                durability,
                0,
                profile,
                profile,
                new[] { Vector2Int.right });
            return definition;
        }

        private T Track<T>(T value) where T : Object
        {
            createdObjects.Add(value);
            return value;
        }

        private static ToolDispatchReport ToolDispatchReportForAcceptedMap() =>
            new ToolDispatchReport(true, false, true, FeedbackId.Break, 1, 0);

        private readonly struct RuntimeRig
        {
            public RuntimeRig(
                ToolActionController controller,
                PlayerHandSlot handSlot,
                PlayerActionLock actionLock,
                HandToolRuntime tool,
                FakeReactionWorld world)
            {
                Controller = controller;
                HandSlot = handSlot;
                ActionLock = actionLock;
                Tool = tool;
                World = world;
            }

            public ToolActionController Controller { get; }
            public PlayerHandSlot HandSlot { get; }
            public PlayerActionLock ActionLock { get; }
            public HandToolRuntime Tool { get; }
            public FakeReactionWorld World { get; }
        }

        private readonly struct TimelineOutcome
        {
            public TimelineOutcome(
                bool started,
                int dispatchCount,
                int remainingResource,
                bool isUsingTool,
                PlayerActionState finalState)
            {
                Started = started;
                DispatchCount = dispatchCount;
                RemainingResource = remainingResource;
                IsUsingTool = isUsingTool;
                FinalState = finalState;
            }

            public bool Started { get; }
            public int DispatchCount { get; }
            public int RemainingResource { get; }
            public bool IsUsingTool { get; }
            public PlayerActionState FinalState { get; }
        }

        private sealed class FakeReactionWorld : IToolReactionWorld
        {
            private readonly ToolDispatchReport report;

            public FakeReactionWorld(ToolDispatchReport configuredReport)
            {
                report = configuredReport;
            }

            public int DispatchCount { get; private set; }

            public ToolDispatchReport Dispatch(ToolDispatchRequest request)
            {
                DispatchCount++;
                return report;
            }
        }

        private sealed class DamageProbe : IToolDamageReceiver
        {
            private readonly HashSet<long> actionIds = new HashSet<long>();

            public int AcceptedCount { get; private set; }

            public bool TryReceiveToolDamage(ToolDamageEvent damageEvent)
            {
                if (!actionIds.Add(damageEvent.ActionId))
                {
                    return false;
                }
                AcceptedCount++;
                return true;
            }
        }
    }
}

#endif
