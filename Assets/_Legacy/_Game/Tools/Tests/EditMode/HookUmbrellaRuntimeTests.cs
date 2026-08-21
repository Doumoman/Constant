#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Interaction.Carry;
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.Input;
using StarNight.Interaction.Reactions;
using StarNight.Map;
using StarNight.Tools.Core;
using StarNight.Tools.HookLauncher;
using StarNight.Tools.Umbrella;
using UnityEditor;
using UnityEngine;

namespace StarNight.Tools.Tests
{
    public sealed class HookUmbrellaRuntimeTests
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
        public void Hook_FirstUseFiresUpSevenCells_SecondUsePullsPlayerAtTwelve()
        {
            GameObject anchor = Track(new GameObject("WorldAnchor"));
            anchor.transform.position = new Vector2(0f, 6f);
            HookTarget target = anchor.AddComponent<HookTarget>();
            target.ConfigureForTests(HookResponse.PullPlayerToTarget);
            var world = new HookWorldProbe
            {
                AcquireResult = true,
                Latch = target.CreateLatch(),
            };
            RuntimeRig rig = CreateRig<HookLauncherRuntime>(CreateHookDefinition(), world);

            Assert.That(rig.Tool.TryPrimaryUse(
                rig.HandSlot,
                new PlayerActionContext(801, 0f, 1f, false),
                -1,
                0), Is.True);
            rig.HookController.TickForTests(HookActionController.FireSeconds);

            Assert.That(world.LastQuery.Direction, Is.EqualTo(Vector2.up));
            Assert.That(world.LastQuery.MaximumDistance, Is.EqualTo(7f).Within(0.001f));
            Assert.That(rig.HookController.State, Is.EqualTo(HookRuntimeState.LatchedWorld));
            Assert.That(rig.ActionLock.State, Is.EqualTo(PlayerActionState.HookLatched));

            Assert.That(((HookLauncherRuntime)rig.Tool).TryPullHook(
                new PlayerActionContext(802, 0f, 0f, false)), Is.True);
            rig.HookController.ApplyMovementOverride(rig.Body, 0.1f);
            Assert.That(rig.HookController.State, Is.EqualTo(HookRuntimeState.PullingPlayer));
            Assert.That(rig.Body.linearVelocity.magnitude, Is.EqualTo(12f).Within(0.001f));
        }

        [Test]
        public void Hook_MissRetractsThenHonorsCooldownWithoutChangingResource()
        {
            var world = new HookWorldProbe { AcquireResult = false };
            RuntimeRig rig = CreateRig<HookLauncherRuntime>(CreateHookDefinition(), world);

            Assert.That(rig.Tool.TryPrimaryUse(
                rig.HandSlot,
                new PlayerActionContext(803, 0f, 0f, false),
                1,
                0), Is.True);
            rig.HookController.TickForTests(HookActionController.FireSeconds);
            Assert.That(rig.HookController.State, Is.EqualTo(HookRuntimeState.MissRetract));
            rig.HookController.TickForTests(HookActionController.MissRetractSeconds);
            Assert.That(rig.HookController.State, Is.EqualTo(HookRuntimeState.Cooldown));
            rig.HookController.TickForTests(HookActionController.CooldownSeconds);

            Assert.That(rig.HookController.State, Is.EqualTo(HookRuntimeState.Idle));
            Assert.That(rig.ActionLock.State, Is.EqualTo(PlayerActionState.Carrying));
            Assert.That(rig.Tool.ResourceState.IsInfinite, Is.True);
        }

        [Test]
        public void PhysicsHookWorld_RejectsOverRangeAndPortalOcclusion()
        {
            GameObject player = Track(new GameObject("Player"));
            GameObject targetObject = Track(new GameObject("HookTarget"));
            targetObject.transform.position = new Vector2(8f, 0f);
            SetLayer(targetObject, "Interaction");
            targetObject.AddComponent<BoxCollider2D>().size = Vector2.one * 0.5f;
            targetObject.AddComponent<HookTarget>().ConfigureForTests(HookResponse.PullPlayerToTarget);
            var world = new PhysicsHookWorld(null);
            var query = new HookFireQuery(
                Vector2.zero,
                Vector2.right,
                7f,
                player,
                default,
                Vector2.zero,
                1f);
            Physics2D.SyncTransforms();

            Assert.That(world.TryAcquire(query, out _), Is.False);

            targetObject.transform.position = new Vector2(6f, 0f);
            GameObject portal = Track(new GameObject("PortalBoundary"));
            portal.transform.position = new Vector2(3f, 0f);
            SetLayer(portal, "PortalBoundary");
            BoxCollider2D portalCollider = portal.AddComponent<BoxCollider2D>();
            portalCollider.size = new Vector2(0.25f, 2f);
            Physics2D.SyncTransforms();
            Assert.That(world.TryAcquire(query, out _), Is.False);

            portalCollider.enabled = false;
            Physics2D.SyncTransforms();
            Assert.That(world.TryAcquire(query, out HookLatch latch), Is.True);
            Assert.That(latch.Target, Is.EqualTo(targetObject));
        }

        [Test]
        public void Hook_PullStopsAtBlockedSafePosition_AndExternalHitCancelsLatch()
        {
            GameObject anchor = Track(new GameObject("WorldAnchor"));
            anchor.transform.position = new Vector2(5f, 0f);
            HookTarget target = anchor.AddComponent<HookTarget>();
            target.ConfigureForTests(HookResponse.PullPlayerToTarget);
            var world = new HookWorldProbe
            {
                AcquireResult = true,
                Latch = target.CreateLatch(),
                PathClear = false,
            };
            RuntimeRig rig = CreateRig<HookLauncherRuntime>(CreateHookDefinition(), world);
            StartAndLatchHook(rig, 804);
            Assert.That(((HookLauncherRuntime)rig.Tool).TryPullHook(
                new PlayerActionContext(805, 0f, 0f, false)), Is.True);

            rig.HookController.ApplyMovementOverride(rig.Body, 0.1f);
            Assert.That(rig.Body.position, Is.EqualTo(Vector2.zero));
            Assert.That(rig.Body.linearVelocity, Is.EqualTo(Vector2.zero));
            Assert.That(rig.HookController.State, Is.EqualTo(HookRuntimeState.Cooldown));

            rig.HookController.TickForTests(HookActionController.CooldownSeconds);
            StartAndLatchHook(rig, 806);
            Assert.That(rig.ActionLock.TryTransition(806, PlayerActionState.Hurt), Is.True);
            rig.HookController.TickForTests(0.01f);
            Assert.That(rig.HookController.State, Is.EqualTo(HookRuntimeState.Idle));
            Assert.That(rig.ActionLock.State, Is.EqualTo(PlayerActionState.Hurt));
        }

        [Test]
        public void Hook_ObjectResponseUsesWeightSpeed_AndTriggerActivatesOnce()
        {
            GameObject objectTarget = Track(new GameObject("MediumObject"));
            objectTarget.transform.position = new Vector2(5f, 0f);
            Rigidbody2D targetBody = objectTarget.AddComponent<Rigidbody2D>();
            objectTarget.AddComponent<BoxCollider2D>();
            CarryObjectDefinition objectDefinition = Track(ScriptableObject.CreateInstance<CarryObjectDefinition>());
            objectDefinition.ConfigureForTests(
                "MEDIUM",
                CarryWeightClass.Medium,
                Vector2Int.one,
                configuredHookResponse: HookResponse.PullToPlayer);
            CarryableObject carryable = objectTarget.AddComponent<CarryableObject>();
            carryable.ConfigureForTests(objectDefinition, targetBody);
            HookTarget hookTarget = objectTarget.AddComponent<HookTarget>();
            hookTarget.ConfigureForTests(HookResponse.PullToPlayer, targetBody);
            var world = new HookWorldProbe
            {
                AcquireResult = true,
                Latch = hookTarget.CreateLatch(),
            };
            RuntimeRig rig = CreateRig<HookLauncherRuntime>(CreateHookDefinition(), world);
            StartAndLatchHook(rig, 807);
            Assert.That(((HookLauncherRuntime)rig.Tool).TryPullHook(
                new PlayerActionContext(808, 0f, 0f, false)), Is.True);
            rig.HookController.FixedTickForTests(0.1f);
            Assert.That(world.LastDesired.x, Is.EqualTo(4.3f).Within(0.001f));

            rig.HookController.CancelHook();
            GameObject triggerObject = Track(new GameObject("RemoteTrigger"));
            triggerObject.transform.position = new Vector2(4f, 0f);
            HookTarget trigger = triggerObject.AddComponent<HookTarget>();
            trigger.ConfigureForTests(HookResponse.Trigger);
            world.Latch = trigger.CreateLatch();
            rig.ActionLock.SetState(PlayerActionState.Carrying);
            StartAndLatchHook(rig, 809);
            Assert.That(((HookLauncherRuntime)rig.Tool).TryPullHook(
                new PlayerActionContext(810, 0f, 0f, false)), Is.True);
            Assert.That(trigger.TriggerCount, Is.EqualTo(1));
        }

        [Test]
        public void Umbrella_TogglesGlideValues_AndTraversalClosesIt()
        {
            RuntimeRig rig = CreateRig<WindUmbrellaRuntime>(CreateUmbrellaDefinition(), null);

            Assert.That(rig.Tool.TryPrimaryUse(
                rig.HandSlot,
                new PlayerActionContext(811, 0f, 0f, false),
                1,
                0), Is.True);
            rig.UmbrellaController.TickForTests(0.14f);
            Assert.That(rig.UmbrellaController.State, Is.EqualTo(UmbrellaRuntimeState.Opening));
            rig.UmbrellaController.TickForTests(0.01f);

            Assert.That(rig.UmbrellaController.IsUmbrellaOpen, Is.True);
            Assert.That(rig.UmbrellaController.MaximumFallSpeed, Is.EqualTo(3.2f));
            Assert.That(rig.UmbrellaController.MaximumHorizontalSpeed, Is.EqualTo(5f));
            Assert.That(rig.UmbrellaController.AirAccelerationMultiplier, Is.EqualTo(0.85f));
            Assert.That(rig.UmbrellaController.WindForceMultiplier, Is.EqualTo(1.8f));
            Assert.That(rig.UmbrellaController.WaterCurrentMultiplier, Is.EqualTo(0.7f));

            ((WindUmbrellaRuntime)rig.Tool).PrepareForTraversal();
            Assert.That(rig.UmbrellaController.State, Is.EqualTo(UmbrellaRuntimeState.Closed));
            Assert.That(rig.ActionLock.State, Is.EqualTo(PlayerActionState.Carrying));
        }

        [Test]
        public void Umbrella_DeflectsOnlyApprovedProjectile_AndAuthoredAssetsAreConnected()
        {
            RuntimeRig rig = CreateRig<WindUmbrellaRuntime>(CreateUmbrellaDefinition(), null);
            Assert.That(rig.Tool.TryPrimaryUse(
                rig.HandSlot,
                new PlayerActionContext(812, 0f, 0f, false),
                1,
                0), Is.True);
            rig.UmbrellaController.TickForTests(UmbrellaActionController.OpenSeconds);

            GameObject projectileObject = Track(new GameObject("Projectile"));
            CommonElementProjectile projectile = projectileObject.AddComponent<CommonElementProjectile>();
            projectile.Configure(Vector2Int.left, 15f, 1, projectileObject, 1);
            Assert.That(rig.UmbrellaController.TryDeflectCandidateForTests(
                projectileObject,
                new Vector2(1f, 0.2f),
                1), Is.True);
            Assert.That(projectile.Velocity.x, Is.GreaterThan(0f));
            Assert.That(projectile.Velocity.y, Is.GreaterThan(0f));
            Assert.That(projectile.Velocity.magnitude, Is.EqualTo(10f).Within(0.001f));

            GameObject enemy = Track(new GameObject("Enemy"));
            ToolDamageTarget damageTarget = enemy.AddComponent<ToolDamageTarget>();
            damageTarget.ConfigureForTests(ToolDamageTargetKind.Enemy, 1);
            Assert.That(((IMapElementDamageReceiver)damageTarget).ReceiveMapElementDamage(
                new MapElementDamageEvent(1, Vector2.right, projectileObject, 813)), Is.True);
            Assert.That(damageTarget.Defeated, Is.True);

            GameObject laser = Track(new GameObject("Laser"));
            Assert.That(rig.UmbrellaController.TryDeflectCandidateForTests(
                laser,
                new Vector2(1f, 0.2f),
                1), Is.False);

            GameObject hookPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Tools/Prefabs/HandTools/HookLauncher.prefab");
            GameObject umbrellaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Tools/Prefabs/HandTools/WindUmbrella.prefab");
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Player/Prefabs/Player.prefab");
            Assert.That(hookPrefab.GetComponent<HookLauncherRuntime>(), Is.Not.Null);
            Assert.That(umbrellaPrefab.GetComponent<WindUmbrellaRuntime>(), Is.Not.Null);
            Assert.That(playerPrefab.GetComponent<HookActionController>(), Is.Not.Null);
            Assert.That(playerPrefab.GetComponent<UmbrellaActionController>(), Is.Not.Null);
        }

        private RuntimeRig CreateRig<T>(HandToolDefinition definition, IHookWorld hookWorld)
            where T : HandToolRuntime
        {
            GameObject player = Track(new GameObject("Player"));
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            CapsuleCollider2D capsule = player.AddComponent<CapsuleCollider2D>();
            PlayerActionLock actionLock = player.AddComponent<PlayerActionLock>();
            HandSlotPresenter presenter = player.AddComponent<HandSlotPresenter>();
            PlayerHandSlot handSlot = player.AddComponent<PlayerHandSlot>();
            HookActionController hookController = player.AddComponent<HookActionController>();
            UmbrellaActionController umbrellaController = player.AddComponent<UmbrellaActionController>();
            hookController.ConfigureForTests(actionLock, body, capsule, hookWorld);
            umbrellaController.ConfigureForTests(actionLock);

            GameObject socket = Track(new GameObject("CarrySocket"));
            socket.transform.SetParent(player.transform, false);
            presenter.ConfigureForTests(socket.transform);
            handSlot.ConfigureForTests(presenter);

            GameObject toolObject = Track(new GameObject(typeof(T).Name));
            T tool = toolObject.AddComponent<T>();
            tool.Configure(definition);
            Assert.That(handSlot.TryAttach(tool), Is.True);
            actionLock.SetState(PlayerActionState.Carrying);
            return new RuntimeRig(
                player,
                body,
                capsule,
                handSlot,
                actionLock,
                tool,
                hookController,
                umbrellaController);
        }

        private HandToolDefinition CreateHookDefinition()
        {
            return CreateDefinition(
                "TOOL_HOOK_LAUNCHER",
                ToolTag.Hook,
                ToolAimMode.UpOrFacing,
                7,
                0f);
        }

        private HandToolDefinition CreateUmbrellaDefinition()
        {
            return CreateDefinition(
                "TOOL_WIND_UMBRELLA",
                ToolTag.WindGuard,
                ToolAimMode.Toggle,
                1,
                120f);
        }

        private HandToolDefinition CreateDefinition(
            string id,
            ToolTag tags,
            ToolAimMode aimMode,
            int range,
            float angle)
        {
            HandToolDefinition definition = Track(ScriptableObject.CreateInstance<HandToolDefinition>());
            var action = new ToolActionProfile
            {
                WindupSeconds = 0.12f,
                ImpactSeconds = 0.12f,
                ActiveSeconds = 0.12f,
                RecoverySeconds = 0.25f,
                AimMode = aimMode,
            };
            definition.Configure(
                id,
                id,
                tags,
                ToolResourceMode.Infinite,
                0,
                0,
                action,
                action,
                new[] { Vector2Int.right, Vector2Int.up },
                range,
                angle);
            return definition;
        }

        private static void StartAndLatchHook(RuntimeRig rig, long actionId)
        {
            Assert.That(rig.Tool.TryPrimaryUse(
                rig.HandSlot,
                new PlayerActionContext(actionId, 0f, 0f, false),
                1,
                0), Is.True);
            rig.HookController.TickForTests(HookActionController.FireSeconds);
            Assert.That(rig.ActionLock.State, Is.EqualTo(PlayerActionState.HookLatched));
        }

        private static void SetLayer(GameObject target, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            Assert.That(layer, Is.GreaterThanOrEqualTo(0), layerName);
            target.layer = layer;
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
                CapsuleCollider2D capsule,
                PlayerHandSlot handSlot,
                PlayerActionLock actionLock,
                HandToolRuntime tool,
                HookActionController hookController,
                UmbrellaActionController umbrellaController)
            {
                Player = player;
                Body = body;
                Capsule = capsule;
                HandSlot = handSlot;
                ActionLock = actionLock;
                Tool = tool;
                HookController = hookController;
                UmbrellaController = umbrellaController;
            }

            public GameObject Player { get; }
            public Rigidbody2D Body { get; }
            public CapsuleCollider2D Capsule { get; }
            public PlayerHandSlot HandSlot { get; }
            public PlayerActionLock ActionLock { get; }
            public HandToolRuntime Tool { get; }
            public HookActionController HookController { get; }
            public UmbrellaActionController UmbrellaController { get; }
        }

        private sealed class HookWorldProbe : IHookWorld
        {
            public bool AcquireResult;
            public HookLatch Latch;
            public bool PathClear = true;
            public HookFireQuery LastQuery;
            public Vector2 LastDesired;

            public bool TryAcquire(HookFireQuery query, out HookLatch latch)
            {
                LastQuery = query;
                latch = Latch;
                return AcquireResult;
            }

            public bool TryResolveStep(
                Vector2 current,
                Vector2 desired,
                Vector2 capsuleSize,
                GameObject mover,
                GameObject ignoredTarget,
                out Vector2 resolvedPosition)
            {
                LastDesired = desired;
                resolvedPosition = PathClear ? desired : current;
                return PathClear;
            }
        }
    }
}

#endif
