#if LEGACY_DISABLED
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Interaction.Carry;
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.Input;
using StarNight.Map;
using StarNight.Player.Motor;
using StarNight.Player.Safety;
using StarNight.Stage.Lab;
using StarNight.Stage.Rooms;
using StarNight.Tools.Bomb;
using StarNight.Tools.Core;
using StarNight.Tools.Pickaxe;
using UnityEngine;

namespace StarNight.Stage.Tests
{
    public sealed class Tool12RoomStateTests
    {
        private readonly List<Object> cleanup = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = cleanup.Count - 1; index >= 0; index--)
            {
                if (cleanup[index] != null)
                {
                    Object.DestroyImmediate(cleanup[index]);
                }
            }
            cleanup.Clear();
        }

        [Test]
        public void ToolSnapshotRestoresPositionAndRemainingResource()
        {
            RoomRuntime room = BuildRoom("SnapshotRoom", out Transform dynamicRoot, out _, out _);
            HandToolDefinition definition = CreateToolDefinition();
            GameObject toolObject = new GameObject("PersistentPickaxe");
            cleanup.Add(toolObject);
            toolObject.transform.SetParent(dynamicRoot, false);
            toolObject.transform.position = new Vector2(2f, 2f);
            PickaxeRuntime tool = toolObject.AddComponent<PickaxeRuntime>();
            tool.Configure(definition);
            tool.SetRoomPersistenceId("ROOM_PICKAXE");
            tool.ResourceState.TryConsumeForSuccessfulReaction(true);
            tool.ResourceState.TryConsumeForSuccessfulReaction(true);

            room.SetSimulationState(RoomSimulationState.Active);
            room.SetSimulationState(RoomSimulationState.Frozen);
            tool.RepairFull();
            toolObject.transform.position = new Vector2(8f, 6f);
            room.SetSimulationState(RoomSimulationState.Active);

            Assert.That(tool.CurrentResource, Is.EqualTo(10));
            Assert.That((Vector2)tool.transform.position, Is.EqualTo(new Vector2(2f, 2f)));
            Assert.That(room.PersistentState.Revision, Is.EqualTo(1));
        }

        [Test]
        public void ResidualSimulationRunsForAtMostThreeSecondsThenSnapshots()
        {
            RoomRuntime room = BuildRoom("ResidualRoom", out Transform dynamicRoot, out _, out _);
            BombDefinition definition = ScriptableObject.CreateInstance<BombDefinition>();
            cleanup.Add(definition);
            definition.ConfigureForTests(10f);
            GameObject bombObject = new GameObject("ResidualBomb");
            cleanup.Add(bombObject);
            bombObject.transform.SetParent(dynamicRoot, false);
            Rigidbody2D body = bombObject.AddComponent<Rigidbody2D>();
            bombObject.AddComponent<CircleCollider2D>();
            BombRuntime bomb = bombObject.AddComponent<BombRuntime>();
            bomb.ConfigureForTests(definition, body);
            bomb.SetRoomPersistenceId("ROOM_BOMB");
            Assert.That(bomb.Arm(definition, null, new Vector2(3f, 1f), 81), Is.True);

            room.SetSimulationState(RoomSimulationState.Active);
            room.SetSimulationState(RoomSimulationState.ResidualSimulation);
            Assert.That(room.SimulationState, Is.EqualTo(RoomSimulationState.ResidualSimulation));
            room.TickResidualForTests(3f);

            Assert.That(room.SimulationState, Is.EqualTo(RoomSimulationState.Frozen));
            Assert.That(room.ResidualElapsedSeconds, Is.EqualTo(3f).Within(0.001f));
            Assert.That(bomb.IsExploded, Is.False);
            Assert.That(bomb.RemainingFuse, Is.EqualTo(7f).Within(0.001f));
            Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
            Assert.That(room.PersistentState.Revision, Is.EqualTo(1));

            room.SetSimulationState(RoomSimulationState.Active);
            Assert.That(bomb.RemainingFuse, Is.EqualTo(7f).Within(0.001f));
            Assert.That(bomb.SimulationMode, Is.EqualTo(BombSimulationMode.Active));
        }

        [Test]
        public void RestoredOverlapMovesToNearestObjectRecoveryCell()
        {
            RoomRuntime room = BuildRoom("RecoveryRoom", out Transform dynamicRoot, out Transform gridLogic, out ObjectRecoveryCell recoveryCell);
            GameObject crate = new GameObject("RecoveryCrate");
            cleanup.Add(crate);
            crate.transform.SetParent(dynamicRoot, false);
            Rigidbody2D body = crate.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            crate.AddComponent<BoxCollider2D>();
            RoomPersistentTransform2D persistent = crate.AddComponent<RoomPersistentTransform2D>();
            persistent.Configure("RECOVERY_CRATE");

            room.SetSimulationState(RoomSimulationState.Active);
            room.SetSimulationState(RoomSimulationState.Frozen);

            GameObject blocker = new GameObject("RestoreBlocker");
            cleanup.Add(blocker);
            blocker.layer = LayerMask.NameToLayer("TerrainSolid");
            blocker.transform.SetParent(gridLogic, false);
            blocker.AddComponent<BoxCollider2D>().size = Vector2.one;
            body.position = new Vector2(6f, 2f);
            crate.transform.position = body.position;

            room.SetSimulationState(RoomSimulationState.Active);

            Assert.That(body.position, Is.EqualTo(recoveryCell.Position));
            Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void HeavyCarrySurvivesActualRoomTransition()
        {
            GameObject labObject = new GameObject("Tool12PortalLab");
            cleanup.Add(labObject);
            Core04TwoRoomLab lab = labObject.AddComponent<Core04TwoRoomLab>();
            lab.BuildIfNeeded();

            GameObject cameraObject = new GameObject("Tool12Camera");
            cleanup.Add(cameraObject);
            Camera camera = cameraObject.AddComponent<Camera>();
            GameObject player = new GameObject("Tool12Player");
            cleanup.Add(player);
            Rigidbody2D playerBody = player.AddComponent<Rigidbody2D>();
            playerBody.gravityScale = 0f;
            player.AddComponent<CapsuleCollider2D>();
            PlayerActionLock actionLock = player.AddComponent<PlayerActionLock>();
            PlayerMotor2D motor = player.AddComponent<PlayerMotor2D>();
            motor.ConfigureForTests(0);
            player.AddComponent<PlayerOutOfBoundsGuard>();
            HandSlotPresenter presenter = player.AddComponent<HandSlotPresenter>();
            presenter.ConfigureForTests(player.transform);
            PlayerHandSlot slot = player.AddComponent<PlayerHandSlot>();
            slot.ConfigureForTests(presenter);
            lab.InitializePlayerAndCamera(motor, camera);

            CarryObjectDefinition definition = ScriptableObject.CreateInstance<CarryObjectDefinition>();
            cleanup.Add(definition);
            definition.ConfigureForTests("HEAVY_PORTAL", CarryWeightClass.Heavy, new Vector2Int(1, 2));
            GameObject carryObject = new GameObject("HeavyPortalCarry");
            cleanup.Add(carryObject);
            Rigidbody2D carryBody = carryObject.AddComponent<Rigidbody2D>();
            carryObject.AddComponent<BoxCollider2D>();
            CarryableObject carryable = carryObject.AddComponent<CarryableObject>();
            carryable.ConfigureForTests(definition, carryBody);
            Assert.That(slot.TryAttach(carryable), Is.True);

            Assert.That(lab.TransitionController.CommitImmediate(lab.PortalAtoB), Is.True);
            Assert.That(slot.HeldCarryable, Is.SameAs(carryable));
            Assert.That(carryable.RuntimeState, Is.EqualTo(CarryRuntimeState.Held));
            Assert.That(lab.TransitionController.HasSuspendedPortalCarry, Is.False);
            Assert.That(actionLock.State, Is.EqualTo(PlayerActionState.Free));
        }

        private RoomRuntime BuildRoom(
            string roomId,
            out Transform dynamicRoot,
            out Transform gridLogic,
            out ObjectRecoveryCell recoveryCell)
        {
            GameObject roomObject = new GameObject(roomId);
            cleanup.Add(roomObject);
            RoomRuntime room = roomObject.AddComponent<RoomRuntime>();
            gridLogic = Node(roomObject.transform, "GridLogic");
            Transform gridVisual = Node(roomObject.transform, "GridVisual");
            Transform portalRoot = Node(roomObject.transform, "PortalRoot");
            dynamicRoot = Node(roomObject.transform, "DynamicRoot");
            Transform safeRoot = Node(roomObject.transform, "SafeCellRoot");
            Transform recoveryTransform = Node(safeRoot, "ObjectRecoveryCell_0");
            recoveryTransform.position = new Vector2(3f, 0f);
            recoveryCell = recoveryTransform.gameObject.AddComponent<ObjectRecoveryCell>();
            Transform recoveryRoot = Node(roomObject.transform, "VoidRecoveryRoot");
            Transform cameraAnchor = Node(roomObject.transform, "CameraAnchor");
            Transform spawn = Node(roomObject.transform, "Spawn");
            Transform cameraBounds = Node(roomObject.transform, "CameraBounds");
            BoxCollider2D cameraCollider = cameraBounds.gameObject.AddComponent<BoxCollider2D>();
            Transform voidZone = Node(recoveryRoot, "VoidZone");
            BoxCollider2D voidCollider = voidZone.gameObject.AddComponent<BoxCollider2D>();
            Transform failSafe = Node(recoveryRoot, "FailSafe");
            BoxCollider2D failSafeCollider = failSafe.gameObject.AddComponent<BoxCollider2D>();
            room.Configure(
                roomId,
                new Vector2Int(12, 8),
                RoomCameraMode.Fixed,
                gridLogic,
                gridVisual,
                portalRoot,
                dynamicRoot,
                safeRoot,
                recoveryRoot,
                cameraAnchor,
                spawn,
                cameraCollider,
                voidCollider,
                failSafeCollider);
            room.SetGeometryApproval(true);
            return room;
        }

        private HandToolDefinition CreateToolDefinition()
        {
            HandToolDefinition definition = ScriptableObject.CreateInstance<HandToolDefinition>();
            cleanup.Add(definition);
            definition.Configure(
                "TOOL_PICKAXE",
                "곡괭이",
                ToolTag.Pickaxe,
                ToolResourceMode.Durability,
                12,
                250,
                new ToolActionProfile(),
                new ToolActionProfile(),
                new[] { Vector2Int.right });
            return definition;
        }

        private static Transform Node(Transform parent, string name)
        {
            GameObject node = new GameObject(name);
            node.transform.SetParent(parent, false);
            return node.transform;
        }
    }
}

#endif
