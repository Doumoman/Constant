#if LEGACY_DISABLED
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Map.Placement;
using StarNight.Stage.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace StarNight.Map.Tests
{
    public sealed class MapE02RuntimePlayModeTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (var index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    Object.Destroy(createdObjects[index]);
                }
            }

            createdObjects.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator DormantRoomStopsAnimatorPhysicsAndTimerThenResumesExactState()
        {
            var rig = CreateRoomRig("pause_resume", 2f);
            rig.Room.SetSimulationState(RoomSimulationState.Active);
            yield return null;

            Assert.That(rig.Element.CurrentState, Is.EqualTo(MapElementState.Idle));
            Assert.That(rig.StateMachine.IsTicking, Is.True);
            Assert.That(rig.Body.simulated, Is.True);
            Assert.That(rig.Animator.speed, Is.EqualTo(1f));

            rig.Element.TrySetState(MapElementState.Warning);
            rig.StateMachine.Tick(0.25f);
            var pausedElapsed = rig.StateMachine.ElapsedSeconds;

            rig.Room.SetSimulationState(RoomSimulationState.Dormant);
            Assert.That(rig.Element.CurrentState, Is.EqualTo(MapElementState.Dormant));
            Assert.That(rig.StateMachine.IsTicking, Is.False);
            Assert.That(rig.Body.simulated, Is.False);
            Assert.That(rig.Animator.speed, Is.Zero);

            rig.StateMachine.Tick(10f);
            Assert.That(rig.StateMachine.ElapsedSeconds, Is.EqualTo(pausedElapsed).Within(0.001f));

            rig.Room.SetSimulationState(RoomSimulationState.Active);
            Assert.That(rig.Element.CurrentState, Is.EqualTo(MapElementState.Warning));
            Assert.That(rig.StateMachine.ElapsedSeconds, Is.EqualTo(pausedElapsed).Within(0.001f));
            Assert.That(rig.StateMachine.IsTicking, Is.True);
            Assert.That(rig.Body.simulated, Is.True);
            Assert.That(rig.Animator.speed, Is.EqualTo(1f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RevisitRestoresBrokenAndMovedStateAndKeepsOccupancyReleased()
        {
            var rig = CreateRoomRig("broken_moved", 1f);
            rig.Room.SetSimulationState(RoomSimulationState.Active);
            yield return null;

            var movedPosition = new Vector3(4.5f, 2.25f, 0f);
            rig.Element.transform.localPosition = movedPosition;
            Assert.That(rig.Element.TrySetState(MapElementState.Broken), Is.True);
            Assert.That(rig.Registry.IsRegistered(rig.Occupier), Is.False);

            rig.Room.SetSimulationState(RoomSimulationState.Dormant);
            rig.Element.transform.localPosition = Vector3.zero;
            rig.Room.SetSimulationState(RoomSimulationState.Active);
            yield return null;

            Assert.That(rig.Element.CurrentState, Is.EqualTo(MapElementState.Broken));
            Assert.That(rig.Element.transform.localPosition, Is.EqualTo(movedPosition));
            Assert.That(rig.Registry.IsRegistered(rig.Occupier), Is.False);
        }

        private RoomRig CreateRoomRig(string suffix, float warningSeconds)
        {
            var roomObject = CreateGameObject($"Room_{suffix}");
            var room = roomObject.AddComponent<RoomRuntime>();
            var registry = roomObject.AddComponent<RoomElementRegistry>();

            var gridLogic = CreateChild(roomObject.transform, "GridLogic");
            var gridVisual = CreateChild(roomObject.transform, "GridVisual");
            var portalRoot = CreateChild(roomObject.transform, "PortalRoot");
            var dynamicRoot = CreateChild(roomObject.transform, "DynamicRoot");
            var safeCellRoot = CreateChild(roomObject.transform, "SafeCellRoot");
            var recoveryRoot = CreateChild(roomObject.transform, "VoidRecoveryRoot");
            var cameraAnchor = CreateChild(roomObject.transform, "CameraAnchor");
            var spawnPoint = CreateChild(roomObject.transform, "SpawnPoint");
            var cameraBounds = CreateChild(roomObject.transform, "CameraBounds").gameObject.AddComponent<BoxCollider2D>();
            var recoveryZone = recoveryRoot.gameObject.AddComponent<BoxCollider2D>();
            var failSafe = CreateChild(roomObject.transform, "HardFailSafe").gameObject.AddComponent<BoxCollider2D>();

            room.Configure(
                $"Room_{suffix}",
                new Vector2Int(12, 8),
                RoomCameraMode.Fixed,
                gridLogic,
                gridVisual,
                portalRoot,
                dynamicRoot,
                safeCellRoot,
                recoveryRoot,
                cameraAnchor,
                spawnPoint,
                cameraBounds,
                recoveryZone,
                failSafe);

            var definition = ScriptableObject.CreateInstance<MapElementDefinition>();
            definition.ElementId = $"COMMON_Test_{suffix}";
            definition.BehaviorProfile.InitialState = MapElementState.Idle;
            definition.BehaviorProfile.WarningSeconds = warningSeconds;
            definition.BehaviorProfile.PersistBrokenState = true;
            createdObjects.Add(definition);

            var elementObject = CreateGameObject($"Element_{suffix}");
            elementObject.transform.SetParent(gridLogic, false);
            var occupier = elementObject.AddComponent<GridOccupier>();
            occupier.Configure(Vector2Int.zero, new CellFootprint(), OccupancyLayer.Fixture);
            elementObject.AddComponent<ElementRuntimeId>();
            var stateMachine = elementObject.AddComponent<ElementStateMachine>();

            var visualRoot = CreateChild(elementObject.transform, "VisualRoot");
            var animator = visualRoot.gameObject.AddComponent<Animator>();
            var physicsRoot = CreateChild(elementObject.transform, "PhysicsRoot");
            var body = physicsRoot.gameObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            physicsRoot.gameObject.AddComponent<BoxCollider2D>();
            var triggerRoot = CreateChild(elementObject.transform, "TriggerRoot");
            var trigger = triggerRoot.gameObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;

            var element = elementObject.AddComponent<MapElementInstance>();
            element.BindAuthoringRoots(visualRoot, physicsRoot, triggerRoot);
            element.Configure(definition, registry, $"map_e02_{suffix}");

            return new RoomRig(room, registry, element, occupier, stateMachine, animator, body);
        }

        private GameObject CreateGameObject(string objectName)
        {
            var gameObject = new GameObject(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static Transform CreateChild(Transform parent, string childName)
        {
            var child = new GameObject(childName).transform;
            child.SetParent(parent, false);
            return child;
        }

        private readonly struct RoomRig
        {
            public RoomRig(
                RoomRuntime room,
                RoomElementRegistry registry,
                MapElementInstance element,
                GridOccupier occupier,
                ElementStateMachine stateMachine,
                Animator animator,
                Rigidbody2D body)
            {
                Room = room;
                Registry = registry;
                Element = element;
                Occupier = occupier;
                StateMachine = stateMachine;
                Animator = animator;
                Body = body;
            }

            public RoomRuntime Room { get; }
            public RoomElementRegistry Registry { get; }
            public MapElementInstance Element { get; }
            public GridOccupier Occupier { get; }
            public ElementStateMachine StateMachine { get; }
            public Animator Animator { get; }
            public Rigidbody2D Body { get; }
        }
    }
}

#endif
