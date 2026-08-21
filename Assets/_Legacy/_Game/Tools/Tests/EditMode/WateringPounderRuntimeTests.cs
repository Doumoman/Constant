#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.Input;
using StarNight.Interaction.Reactions;
using StarNight.Interaction.Targeting;
using StarNight.Map;
using StarNight.Tools.Core;
using StarNight.Tools.Pounder;
using StarNight.Tools.Watering;
using UnityEditor;
using UnityEngine;

namespace StarNight.Tools.Tests
{
    public sealed class WateringPounderRuntimeTests
    {
        private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObjects[index]);
                }
            }
            createdObjects.Clear();
        }

        [Test]
        public void WateringFacing_SpraysInOrder_StopsAtBlocker_AndConsumesOneCharge()
        {
            var world = new RecordingReactionWorld(request =>
            {
                if (request.TargetCell == Vector2Int.right)
                {
                    return new ToolDispatchReport(true, false, true, FeedbackId.WetMud, 1, 0);
                }
                if (request.TargetCell == Vector2Int.right * 2)
                {
                    return new ToolDispatchReport(false, false, false, FeedbackId.MetalFail, 0, 0, true);
                }
                return ToolDispatchReport.Rejected();
            });
            RuntimeRig rig = CreateRig<WateringCanRuntime>(CreateWateringDefinition(), world);

            Assert.That(rig.Controller.TryStart(
                rig.Tool,
                rig.HandSlot,
                new PlayerActionContext(701, 0f, 0f, false),
                1,
                true), Is.True);
            rig.Controller.TickForTests(0.09f);
            rig.Controller.TickForTests(1f);

            Assert.That(world.Requests.Count, Is.EqualTo(2));
            Assert.That(world.Requests[0].TargetCell, Is.EqualTo(Vector2Int.right));
            Assert.That(world.Requests[1].TargetCell, Is.EqualTo(Vector2Int.right * 2));
            Assert.That(((WateringCanRuntime)rig.Tool).LastSprayedCellCount, Is.EqualTo(2));
            Assert.That(rig.Tool.CurrentResource, Is.EqualTo(5));
        }

        [Test]
        public void WateringUp_UsesTwoCells_AndEmptySprayConsumesNothing()
        {
            var world = new RecordingReactionWorld(_ => ToolDispatchReport.Rejected());
            RuntimeRig rig = CreateRig<WateringCanRuntime>(CreateWateringDefinition(), world);

            Assert.That(rig.Controller.TryStart(
                rig.Tool,
                rig.HandSlot,
                new PlayerActionContext(702, 0f, 1f, false),
                -1,
                true), Is.True);
            rig.Controller.TickForTests(1f);

            Assert.That(world.Requests.Count, Is.EqualTo(2));
            Assert.That(world.Requests[0].TargetCell, Is.EqualTo(Vector2Int.up));
            Assert.That(world.Requests[1].TargetCell, Is.EqualTo(Vector2Int.up * 2));
            Assert.That(rig.Tool.CurrentResource, Is.EqualTo(6));

            GameObject dispatcherObject = Track(new GameObject("Dispatcher"));
            ToolReactionDispatcher dispatcher = dispatcherObject.AddComponent<ToolReactionDispatcher>();
            var damageProbe = new DamageProbe();
            ToolDispatchReport direct = dispatcher.DispatchDirect(
                new ToolDispatchRequest(
                    709,
                    ToolTag.Water,
                    Vector2Int.zero,
                    Vector2Int.right,
                    Vector2Int.right,
                    1f,
                    dispatcherObject,
                    dispatcherObject,
                    Vector2.zero,
                    1f),
                null,
                damageProbe);
            Assert.That(direct.EntityAccepted, Is.False);
            Assert.That(damageProbe.AcceptedCount, Is.Zero);
        }

        [Test]
        public void WaterRecharge_RequiresHalfSecond_AndMovementCancelsIt()
        {
            RuntimeRig rig = CreateRig<WateringCanRuntime>(
                CreateWateringDefinition(),
                new RecordingReactionWorld(_ => ToolDispatchReport.Rejected()));
            rig.Tool.ResourceState.ConfigureForTests(ToolResourceMode.Charge, 6, 2);
            GameObject source = Track(new GameObject("WaterRechargeSource"));
            ToolRechargeReceiver receiver = source.AddComponent<ToolRechargeReceiver>();

            ContextReceiverResult started = receiver.TryReceive(new ContextReceiverRequest(
                new PlayerActionContext(703, 0f, 0f, false),
                rig.Player,
                rig.Tool));
            receiver.TickForTests(0.49f, true, Vector2.zero);
            Assert.That(started.Accepted, Is.True);
            Assert.That(rig.Tool.CurrentResource, Is.EqualTo(2));
            receiver.TickForTests(0.01f, true, Vector2.zero);
            Assert.That(rig.Tool.CurrentResource, Is.EqualTo(6));
            Assert.That(rig.ActionLock.State, Is.EqualTo(PlayerActionState.Carrying));

            rig.Tool.ResourceState.ConfigureForTests(ToolResourceMode.Charge, 6, 2);
            Assert.That(receiver.TryReceive(new ContextReceiverRequest(
                new PlayerActionContext(704, 0f, 0f, false),
                rig.Player,
                rig.Tool)).Accepted, Is.True);
            receiver.TickForTests(0.10f, true, Vector2.right * 0.06f);
            Assert.That(receiver.IsRecharging, Is.False);
            Assert.That(rig.Tool.CurrentResource, Is.EqualTo(2));
        }

        [Test]
        public void GroundPound_HitsFacingCellWithHeavyImpact_AndConsumesOneDurability()
        {
            var world = new RecordingReactionWorld(_ =>
                new ToolDispatchReport(true, false, true, FeedbackId.Hit, 1, 0));
            RuntimeRig rig = CreateRig<PounderRuntime>(CreatePounderDefinition(), world);

            Assert.That(rig.Controller.TryStart(
                rig.Tool,
                rig.HandSlot,
                new PlayerActionContext(705, 0f, 0f, false),
                -1,
                true), Is.True);
            rig.Controller.TickForTests(1f);

            Assert.That(world.Requests.Count, Is.EqualTo(1));
            Assert.That(world.Requests[0].TargetCell, Is.EqualTo(Vector2Int.left));
            Assert.That(world.Requests[0].Tags & ToolTag.HeavyImpact, Is.Not.EqualTo(ToolTag.None));
            Assert.That(rig.Tool.CurrentResource, Is.EqualTo(7));
        }

        [Test]
        public void AirPound_DivesThenDispatchesCenterAndSides_AndAllowsOnlyOncePerJump()
        {
            var world = new RecordingReactionWorld(request =>
                request.TargetCell == Vector2Int.down
                    ? new ToolDispatchReport(true, false, true, FeedbackId.Hit, 1, 0)
                    : ToolDispatchReport.Rejected());
            RuntimeRig rig = CreateRig<PounderRuntime>(CreatePounderDefinition(), world);
            rig.Body.linearVelocity = new Vector2(8f, 3f);

            Assert.That(rig.Controller.TryStart(
                rig.Tool,
                rig.HandSlot,
                new PlayerActionContext(706, 0f, 0f, false),
                1,
                false), Is.True);
            rig.Controller.ApplyMovementOverride(rig.Body, 0.02f);
            Assert.That(rig.Body.linearVelocity.x, Is.EqualTo(2f).Within(0.001f));
            Assert.That(rig.Body.linearVelocity.y, Is.EqualTo(-10f).Within(0.001f));
            Assert.That(world.Requests, Is.Empty);

            Assert.That(rig.Controller.NotifyAirPoundCollisionForTests(), Is.True);
            Assert.That(world.Requests.Count, Is.EqualTo(3));
            Assert.That(world.Requests[0].TargetCell, Is.EqualTo(Vector2Int.down));
            Assert.That(world.Requests[1].TargetCell, Is.EqualTo(new Vector2Int(-1, -1)));
            Assert.That(world.Requests[2].TargetCell, Is.EqualTo(new Vector2Int(1, -1)));
            Assert.That(world.Requests[0].Tags & ToolTag.HeavyImpact, Is.Not.EqualTo(ToolTag.None));
            Assert.That(world.Requests[1].Tags, Is.EqualTo(ToolTag.LightImpact));
            Assert.That(rig.Tool.CurrentResource, Is.EqualTo(7));

            rig.Controller.TickForTests(0.18f);
            Assert.That(rig.Controller.IsUsingTool, Is.False);
            Assert.That(rig.Controller.TryStart(
                rig.Tool,
                rig.HandSlot,
                new PlayerActionContext(707, 0f, 0f, false),
                1,
                false), Is.False);
        }

        [Test]
        public void AirPound_SideOnlyReaction_DoesNotConsumeCenterDurability()
        {
            var world = new RecordingReactionWorld(request =>
                request.TargetCell == new Vector2Int(-1, -1)
                    ? new ToolDispatchReport(true, false, true, FeedbackId.Hit, 1, 0)
                    : ToolDispatchReport.Rejected());
            GameObject toolObject = Track(new GameObject("Pounder"));
            PounderRuntime pounder = toolObject.AddComponent<PounderRuntime>();
            ToolDispatchReport report = pounder.DispatchImpact(world, new ToolDispatchRequest(
                708,
                ToolTag.Pound | ToolTag.HeavyImpact,
                Vector2Int.zero,
                Vector2Int.down,
                Vector2Int.down,
                1f,
                toolObject,
                toolObject,
                Vector2.zero,
                1f));

            Assert.That(report.Accepted, Is.True);
            Assert.That(report.ConsumeToolResource, Is.False);
        }

        [Test]
        public void AuthoredWaterTargetsAndToolPrefabs_AreConnected()
        {
            AssertWaterReaction("Assets/_Game/Map/Data/Elements/Common/COMMON_Block_SoftSoil.asset");
            AssertWaterReaction("Assets/_Game/Map/Data/Elements/Sun/SUN_GrowthVine.asset");
            AssertWaterReaction("Assets/_Game/Map/Data/Elements/Sun/SUN_OverheatPlatform.asset");
            AssertWaterReaction("Assets/_Game/Map/Data/Elements/Post/POST_InkPool.asset");

            GameObject wateringCan = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Tools/Prefabs/HandTools/WateringCan.prefab");
            GameObject pounder = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Tools/Prefabs/HandTools/Pounder.prefab");
            GameObject waterVent = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Tools/Prefabs/Interaction/WaterRechargeSource.prefab");

            Assert.That(wateringCan, Is.Not.Null);
            Assert.That(wateringCan.GetComponent<WateringCanRuntime>(), Is.Not.Null);
            Assert.That(pounder, Is.Not.Null);
            Assert.That(pounder.GetComponent<PounderRuntime>(), Is.Not.Null);
            Assert.That(waterVent.GetComponentInChildren<ToolRechargeReceiver>(true), Is.Not.Null);
        }

        private RuntimeRig CreateRig<T>(HandToolDefinition definition, RecordingReactionWorld world)
            where T : HandToolRuntime
        {
            GameObject player = Track(new GameObject("Player"));
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
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
            controller.ConfigureForTests(actionLock, world, Vector2.zero);
            return new RuntimeRig(player, body, controller, handSlot, actionLock, tool);
        }

        private HandToolDefinition CreateWateringDefinition()
        {
            return CreateDefinition(
                "TOOL_WATERING_CAN",
                ToolTag.Water,
                ToolResourceMode.Charge,
                6,
                new ToolActionProfile
                {
                    WindupSeconds = 0.08f,
                    ImpactSeconds = 0.08f,
                    ActiveSeconds = 0.42f,
                    RecoverySeconds = 0.15f,
                    MovementMultiplier = 1f,
                    AimMode = ToolAimMode.UpOrFacing,
                },
                new ToolActionProfile
                {
                    WindupSeconds = 0.08f,
                    ImpactSeconds = 0.08f,
                    ActiveSeconds = 0.42f,
                    RecoverySeconds = 0.15f,
                    MovementMultiplier = 1f,
                    AimMode = ToolAimMode.UpOrFacing,
                });
        }

        private HandToolDefinition CreatePounderDefinition()
        {
            return CreateDefinition(
                "TOOL_POUNDER",
                ToolTag.Pound | ToolTag.HeavyImpact,
                ToolResourceMode.Durability,
                8,
                new ToolActionProfile
                {
                    WindupSeconds = 0.16f,
                    ImpactSeconds = 0.18f,
                    ActiveSeconds = 0.18f,
                    RecoverySeconds = 0.28f,
                    MovementMultiplier = 1f,
                    AimMode = ToolAimMode.Facing,
                },
                new ToolActionProfile
                {
                    WindupSeconds = 0.16f,
                    ImpactSeconds = 0.18f,
                    ActiveSeconds = 0.18f,
                    RecoverySeconds = 0.28f,
                    MovementMultiplier = 1f,
                    AimMode = ToolAimMode.DownAutomatic,
                });
        }

        private HandToolDefinition CreateDefinition(
            string id,
            ToolTag tags,
            ToolResourceMode mode,
            int resource,
            ToolActionProfile ground,
            ToolActionProfile air)
        {
            HandToolDefinition definition = Track(ScriptableObject.CreateInstance<HandToolDefinition>());
            definition.Configure(
                id,
                id,
                tags,
                mode,
                resource,
                0,
                ground,
                air,
                new[] { Vector2Int.right });
            return definition;
        }

        private static void AssertWaterReaction(string assetPath)
        {
            MapElementDefinition definition = AssetDatabase.LoadAssetAtPath<MapElementDefinition>(assetPath);
            Assert.That(definition, Is.Not.Null, assetPath);
            Assert.That(definition.ToolReactions.TryResolve(ToolTag.Water, out ToolReactionEntry entry, out _),
                Is.True,
                assetPath);
            Assert.That(entry.Reaction, Is.Not.EqualTo(ElementReactionType.None), assetPath);
        }

        private T Track<T>(T value) where T : UnityEngine.Object
        {
            createdObjects.Add(value);
            return value;
        }

        private sealed class RuntimeRig
        {
            public RuntimeRig(
                GameObject player,
                Rigidbody2D body,
                ToolActionController controller,
                PlayerHandSlot handSlot,
                PlayerActionLock actionLock,
                HandToolRuntime tool)
            {
                Player = player;
                Body = body;
                Controller = controller;
                HandSlot = handSlot;
                ActionLock = actionLock;
                Tool = tool;
            }

            public GameObject Player { get; }
            public Rigidbody2D Body { get; }
            public ToolActionController Controller { get; }
            public PlayerHandSlot HandSlot { get; }
            public PlayerActionLock ActionLock { get; }
            public HandToolRuntime Tool { get; }
        }

        private sealed class RecordingReactionWorld : IToolReactionWorld
        {
            private readonly Func<ToolDispatchRequest, ToolDispatchReport> resolver;

            public RecordingReactionWorld(Func<ToolDispatchRequest, ToolDispatchReport> configuredResolver)
            {
                resolver = configuredResolver;
            }

            public List<ToolDispatchRequest> Requests { get; } = new List<ToolDispatchRequest>();

            public ToolDispatchReport Dispatch(ToolDispatchRequest request)
            {
                Requests.Add(request);
                return resolver(request);
            }
        }

        private sealed class DamageProbe : IToolDamageReceiver
        {
            public int AcceptedCount { get; private set; }

            public bool TryReceiveToolDamage(ToolDamageEvent damageEvent)
            {
                AcceptedCount++;
                return true;
            }
        }
    }
}

#endif
