#if LEGACY_DISABLED
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Map.Placement;
using UnityEngine;
using UnityEngine.TestTools;

namespace StarNight.Map.Tests
{
    public sealed class MapE05CommonElementsPlayModeTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var projectiles = Object.FindObjectsByType<CommonElementProjectile>(FindObjectsSortMode.None);
            for (var index = 0; index < projectiles.Length; index++)
            {
                if (projectiles[index] != null)
                {
                    Object.Destroy(projectiles[index].gameObject);
                }
            }

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
        public IEnumerator ToolReactionRejectsFailuresAndDuplicatesThenBreaksAtApprovedThreshold()
        {
            var cracked = CreateElement(CommonElementKind.CrackedBlock);
            cracked.Definition.ToolReactions.Entries.Add(new ToolReactionEntry
            {
                Tool = ToolTag.LightImpact,
                Reaction = ElementReactionType.Break,
                StrengthRequired = 3,
            });

            var first = cracked.Reactions.TryReact(Context(101, ToolTag.LightImpact));
            var duplicate = cracked.Reactions.TryReact(Context(101, ToolTag.LightImpact));
            var second = cracked.Reactions.TryReact(Context(102, ToolTag.LightImpact));
            var third = cracked.Reactions.TryReact(Context(103, ToolTag.LightImpact));

            Assert.That(first.Accepted, Is.True);
            Assert.That(first.ConsumeToolResource, Is.True);
            Assert.That(first.ChangedState, Is.False);
            Assert.That(duplicate.Accepted, Is.False);
            Assert.That(duplicate.ConsumeToolResource, Is.False);
            Assert.That(duplicate.Feedback, Is.EqualTo(FeedbackId.DuplicateAction));
            Assert.That(second.ChangedState, Is.False);
            Assert.That(third.ChangedState, Is.True);
            Assert.That(cracked.Instance.CurrentState, Is.EqualTo(MapElementState.Broken));

            var unbreakable = CreateElement(CommonElementKind.UnbreakableBlock);
            var refusal = unbreakable.Reactions.TryReact(Context(201, ToolTag.Bomb));
            Assert.That(refusal.Accepted, Is.False);
            Assert.That(refusal.ConsumeToolResource, Is.False);
            Assert.That(refusal.Feedback, Is.EqualTo(FeedbackId.MetalFail));

            unbreakable.Reactions.SetTransitionBusy(true);
            var busy = unbreakable.Reactions.TryReact(Context(202, ToolTag.Pickaxe));
            Assert.That(busy.Accepted, Is.False);
            Assert.That(busy.ConsumeToolResource, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator WarningMovementFallingAndSignalUtilitiesRunDeterministically()
        {
            var fragile = CreateElement(CommonElementKind.FragileFloor);
            fragile.Definition.CommonProfile.TriggerDwellSeconds = 0.55f;
            fragile.Definition.BehaviorProfile.WarningSeconds = 0.25f;
            fragile.Driver.SetFragileOccupancy(true);
            fragile.Driver.TickForTests(0.55f);
            Assert.That(fragile.Instance.CurrentState, Is.EqualTo(MapElementState.Warning));
            fragile.Driver.TickForTests(0.25f);
            Assert.That(fragile.Instance.CurrentState, Is.EqualTo(MapElementState.Broken));

            var moving = CreateElement(CommonElementKind.MovingPlatform);
            moving.Definition.BehaviorProfile.Path.Nodes.Add(Vector2.zero);
            moving.Definition.BehaviorProfile.Path.Nodes.Add(new Vector2(4f, 0f));
            moving.Definition.BehaviorProfile.Path.SpeedCellsPerSecond = 2.2f;
            moving.Definition.BehaviorProfile.Path.WaitSeconds = 0.3f;
            moving.Definition.BehaviorProfile.Path.PingPong = true;
            moving.Driver.Rebind();
            moving.Driver.TickForTests(1f);
            Assert.That(moving.Root.transform.localPosition.x, Is.EqualTo(2.2f).Within(0.01f));

            var falling = CreateElement(CommonElementKind.FallingStone, addBody: true);
            falling.Definition.CommonProfile.TriggerDwellSeconds = 0.15f;
            falling.Definition.CommonProfile.GravityScale = 2f;
            falling.Definition.BehaviorProfile.WarningSeconds = 0.45f;
            falling.Driver.SetFragileOccupancy(true);
            falling.Driver.TickForTests(0.15f);
            Assert.That(falling.Instance.CurrentState, Is.EqualTo(MapElementState.Warning));
            falling.Driver.TickForTests(0.45f);
            Assert.That(falling.Instance.CurrentState, Is.EqualTo(MapElementState.Active));
            Assert.That(falling.Body.bodyType, Is.EqualTo(RigidbodyType2D.Dynamic));
            Assert.That(falling.Body.gravityScale, Is.EqualTo(2f).Within(0.001f));

            var plate = CreateElement(CommonElementKind.PressurePlate);
            plate.Definition.CommonProfile.WeightThreshold = 1;
            plate.Definition.CommonProfile.SignalMode = CommonSignalMode.Hold;
            plate.Definition.CommonProfile.SignalChannel = "TEST_DOOR";
            var door = CreateElement(CommonElementKind.WeightDoor);
            door.Definition.CommonProfile.SignalChannel = "TEST_DOOR";
            door.Definition.CommonProfile.OpenSpeedCellsPerSecond = 2f;
            door.Definition.Footprint.BoundsSize = new Vector2Int(1, 2);
            plate.Driver.SignalChanged += door.Driver.ReceiveSignal;
            plate.Driver.SetPressureWeight(1, 1, true);
            door.Driver.TickForTests(0.5f);
            Assert.That(door.Driver.SignalActive, Is.True);
            Assert.That(door.Driver.DoorOpenProgress, Is.EqualTo(0.5f).Within(0.01f));

            var lever = CreateElement(CommonElementKind.Lever);
            lever.Definition.CommonProfile.SignalMode = CommonSignalMode.Toggle;
            lever.Definition.CommonProfile.SignalChannel = "TEST_DOOR";
            lever.Driver.SignalChanged += door.Driver.ReceiveSignal;
            Assert.That(lever.Driver.InteractionPrompt, Is.EqualTo("[X] 레버 당기기"));
            Assert.That(lever.Driver.TryInteract(null), Is.True);
            Assert.That(door.Driver.SignalActive, Is.True);
            Assert.That(lever.Driver.TryInteract(null), Is.True);
            Assert.That(door.Driver.SignalActive, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DamageWindWaterAndBounceUseOneDamageAndConfiguredForces()
        {
            var target = CreateGameObject("CommonTarget");
            var targetBody = target.AddComponent<Rigidbody2D>();
            targetBody.gravityScale = 1f;
            var targetCollider = target.AddComponent<BoxCollider2D>();
            var probe = target.AddComponent<MapE05TargetProbe>();

            var spike = CreateElement(CommonElementKind.Spike);
            spike.Definition.CommonProfile.Damage = 1;
            spike.Driver.NotifyTriggerEnter(targetCollider);
            Assert.That(probe.DamageCalls, Is.EqualTo(1));
            Assert.That(probe.LastDamage, Is.EqualTo(1));

            var wind = CreateElement(CommonElementKind.WindVent);
            wind.Definition.CommonProfile.ForceCellsPerSecond = 7f;
            wind.Driver.NotifyTriggerStay(targetCollider);
            Assert.That(probe.Wind.x, Is.GreaterThan(0f));

            var water = CreateElement(CommonElementKind.WaterVent);
            water.Definition.CommonProfile.ForceCellsPerSecond = 5f;
            water.Driver.NotifyTriggerStay(targetCollider);
            Assert.That(probe.Water.x, Is.GreaterThan(0f));

            var bounce = CreateElement(CommonElementKind.BouncePad);
            bounce.Definition.CommonProfile.LaunchHeightCells = 3f;
            bounce.Definition.BehaviorProfile.CooldownSeconds = 0.25f;
            bounce.Driver.NotifyTriggerEnter(targetCollider);
            Assert.That(targetBody.linearVelocity.y, Is.GreaterThan(0f));
            yield return null;
        }

        private ElementRig CreateElement(CommonElementKind kind, bool addBody = false)
        {
            var definition = ScriptableObject.CreateInstance<MapElementDefinition>();
            definition.ElementId = $"COMMON_Test_{kind}_{createdObjects.Count}";
            definition.CommonProfile.Kind = kind;
            definition.CommonProfile.Damage = 1;
            definition.BehaviorProfile.InitialState = MapElementState.Idle;
            createdObjects.Add(definition);

            var root = CreateGameObject($"Element_{kind}");
            var occupier = root.AddComponent<GridOccupier>();
            occupier.Configure(Vector2Int.zero, definition.Footprint, OccupancyLayer.Fixture);
            root.AddComponent<ElementRuntimeId>();
            var stateMachine = root.AddComponent<ElementStateMachine>();
            Rigidbody2D body = null;
            if (addBody)
            {
                body = root.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
            }

            var instance = root.AddComponent<MapElementInstance>();
            var driver = root.AddComponent<CommonElementDriver>();
            var reactions = root.AddComponent<ToolReactionReceiver>();
            instance.Configure(definition, null, $"test_{kind}_{createdObjects.Count}");
            instance.SetMapRoomState(MapRoomState.Active);
            driver.Rebind();
            return new ElementRig(root, definition, instance, stateMachine, driver, reactions, body);
        }

        private GameObject CreateGameObject(string objectName)
        {
            var gameObject = new GameObject(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static ToolReactionContext Context(int actionId, ToolTag tags)
        {
            return new ToolReactionContext
            {
                ActionId = actionId,
                Tags = tags,
                Direction = Vector2Int.right,
                Magnitude = 1f,
            };
        }

        private readonly struct ElementRig
        {
            public ElementRig(
                GameObject root,
                MapElementDefinition definition,
                MapElementInstance instance,
                ElementStateMachine stateMachine,
                CommonElementDriver driver,
                ToolReactionReceiver reactions,
                Rigidbody2D body)
            {
                Root = root;
                Definition = definition;
                Instance = instance;
                StateMachine = stateMachine;
                Driver = driver;
                Reactions = reactions;
                Body = body;
            }

            public GameObject Root { get; }
            public MapElementDefinition Definition { get; }
            public MapElementInstance Instance { get; }
            public ElementStateMachine StateMachine { get; }
            public CommonElementDriver Driver { get; }
            public ToolReactionReceiver Reactions { get; }
            public Rigidbody2D Body { get; }
        }
    }

    public sealed class MapE05TargetProbe : MonoBehaviour,
        IMapElementDamageReceiver,
        IMapElementEnvironmentalReceiver,
        IMapElementWeightSource
    {
        public int DamageCalls { get; private set; }
        public int LastDamage { get; private set; }
        public Vector2 Wind { get; private set; }
        public Vector2 Water { get; private set; }
        public int PressureWeight => 1;

        public bool ReceiveMapElementDamage(MapElementDamageEvent damageEvent)
        {
            DamageCalls++;
            LastDamage = damageEvent.Damage;
            return true;
        }

        public void ReceiveWind(Vector2 velocityDelta)
        {
            Wind += velocityDelta;
        }

        public void ReceiveWater(Vector2 velocityDelta)
        {
            Water += velocityDelta;
        }
    }
}

#endif
