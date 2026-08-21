#if LEGACY_DISABLED
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Map.Placement;
using UnityEngine;
using UnityEngine.TestTools;

namespace StarNight.Map.Tests
{
    public sealed class MapE07SunElementsPlayModeTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (var index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null) Object.Destroy(createdObjects[index]);
            }
            createdObjects.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator AllEightSunElementsPerformLightWaterAndContextContracts()
        {
            var receiver = CreateReceiver("SunReceiver");

            var sunbeam = CreateElement(SunElementKind.RotatingSunbeam);
            var initialAngle = sunbeam.Driver.SunbeamAngle;
            sunbeam.Driver.ReceiveSignal("sun.rotate", true);
            Assert.That(sunbeam.Driver.SunbeamAngle, Is.Not.EqualTo(initialAngle));
            Assert.That(sunbeam.Driver.SunbeamActive, Is.True);
            sunbeam.Driver.NotifyTriggerStay(receiver.Collider);
            Assert.That(receiver.Component.SunlightReceived, Is.True);
            Assert.That(receiver.Component.DamageReceived, Is.EqualTo(1));

            var shadow = CreateElement(SunElementKind.ShadowSeed);
            shadow.Driver.NotifyTriggerStay(receiver.Collider);
            Assert.That(receiver.Component.ShadowReceived, Is.True);
            AssertAccepted(shadow.Reactions.TryReact(Context(1, ToolTag.Water)));
            Assert.That(shadow.Driver.ShadowActive, Is.False);

            var sunflower = CreateElement(SunElementKind.SunflowerPlatform, addBody: true);
            sunflower.Driver.ReceiveSignal("sun.light", true);
            Assert.That(sunflower.Driver.SunflowerBloomed, Is.True);
            Assert.That(sunflower.Driver.SunflowerRotationSteps, Is.EqualTo(1));
            sunflower.Driver.ReceiveSignal("sun.overheat", true);
            Assert.That(sunflower.Driver.SunflowerBloomed, Is.False);

            var vine = CreateElement(SunElementKind.GrowthVine);
            Assert.That(vine.Driver.VineLengthCells, Is.EqualTo(1));
            AssertAccepted(vine.Reactions.TryReact(Context(2, ToolTag.Water)));
            Assert.That(vine.Driver.VineLengthCells, Is.EqualTo(2));
            AssertAccepted(vine.Reactions.TryReact(Context(3, ToolTag.Hook)));
            Assert.That(vine.Driver.VineLengthCells, Is.EqualTo(3));
            AssertAccepted(vine.Reactions.TryReact(Context(4, ToolTag.Pickaxe)));
            Assert.That(vine.Instance.CurrentState, Is.EqualTo(MapElementState.Broken));

            var dew = CreateElement(SunElementKind.DewDrop);
            var dewObject = dew.Driver.SpawnDewDrop();
            Assert.That(dewObject, Is.Not.Null);
            createdObjects.Add(dewObject);
            Assert.That(dewObject.GetComponent<SunDewDropProjectile>().ApplyTo(receiver.Root), Is.True);
            Assert.That(receiver.Component.WateringCanFull, Is.True);
            Assert.That(receiver.Component.CoolingReceived, Is.GreaterThan(0f));

            var overheat = CreateElement(SunElementKind.OverheatPlatform);
            overheat.Driver.TickForTests(2.1f);
            Assert.That(overheat.Driver.OverheatActive, Is.True);
            overheat.Driver.NotifyTriggerStay(receiver.Collider);
            Assert.That(receiver.Component.DamageReceived, Is.EqualTo(2));
            AssertAccepted(overheat.Reactions.TryReact(Context(5, ToolTag.Water)));
            Assert.That(overheat.Driver.OverheatActive, Is.False);

            var sunset = CreateElement(SunElementKind.SunsetFlower);
            sunset.Driver.ReceiveSignal("sun.shadow", true);
            Assert.That(sunset.Driver.SunsetPhase, Is.EqualTo(SunPhase.Shadow));
            sunset.Driver.ReceiveSignal("sun.light", true);
            Assert.That(sunset.Driver.SunsetPhase, Is.EqualTo(SunPhase.Day));

            var perch = CreateElement(SunElementKind.CrowPerch);
            var letter = CreateGameObject("SunLetter");
            letter.AddComponent<TestSunPerchOffering>().ContextIdValue = "letter";
            AssertAccepted(perch.Reactions.TryReact(Context(6, ToolTag.Context, letter)));
            Assert.That(perch.Driver.AcceptedPerchOffering, Is.EqualTo("letter"));
            yield return null;
        }

        private ElementRig CreateElement(SunElementKind kind, bool addBody = false)
        {
            var definition = ScriptableObject.CreateInstance<MapElementDefinition>();
            definition.ElementId = $"SUN_Test_{kind}_{createdObjects.Count}";
            definition.AllowedRegions = RegionMask.Sun;
            definition.SunProfile.Kind = kind;
            definition.BehaviorProfile.InitialState = MapElementState.Idle;
            ConfigureDefinition(definition, kind);
            createdObjects.Add(definition);

            var root = CreateGameObject($"SunElement_{kind}");
            if (addBody)
            {
                var body = root.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
            }
            var occupier = root.AddComponent<GridOccupier>();
            occupier.Configure(Vector2Int.zero, definition.Footprint, OccupancyLayer.Fixture);
            root.AddComponent<ElementRuntimeId>();
            root.AddComponent<ElementStateMachine>();
            var instance = root.AddComponent<MapElementInstance>();
            var driver = root.AddComponent<SunElementDriver>();
            var reactions = root.AddComponent<ToolReactionReceiver>();
            instance.Configure(definition, null, $"e07_sun_{kind}_{createdObjects.Count}");
            instance.SetMapRoomState(MapRoomState.Active);
            driver.Rebind();
            return new ElementRig(root, instance, driver, reactions);
        }

        private static void ConfigureDefinition(MapElementDefinition definition, SunElementKind kind)
        {
            switch (kind)
            {
                case SunElementKind.RotatingSunbeam:
                    definition.SunProfile.ArcDegrees = 120f;
                    definition.SunProfile.CycleOnSeconds = 2f;
                    definition.SunProfile.CycleOffSeconds = 1f;
                    definition.SunProfile.Damage = 1;
                    break;
                case SunElementKind.ShadowSeed:
                    definition.SunProfile.ShadowLifetimeSeconds = 6f;
                    AddReaction(definition, ToolTag.Water, ElementReactionType.Disable);
                    break;
                case SunElementKind.SunflowerPlatform:
                    definition.SunProfile.PlatformRotationStepDegrees = 90;
                    definition.SunProfile.BloomsInLight = true;
                    definition.SunProfile.ClosesOnOverheat = true;
                    break;
                case SunElementKind.GrowthVine:
                    definition.SunProfile.StartLengthCells = 1;
                    definition.SunProfile.MaxLengthCells = 6;
                    AddReaction(definition, ToolTag.Water, ElementReactionType.SetState);
                    AddReaction(definition, ToolTag.Hook, ElementReactionType.Pull);
                    AddReaction(definition, ToolTag.Pickaxe, ElementReactionType.Break);
                    break;
                case SunElementKind.DewDrop:
                    definition.SunProfile.CoolOnImpact = true;
                    definition.SunProfile.CanFullyRefillWateringCan = true;
                    definition.SunProfile.ThrownWaterMagnitude = 1f;
                    break;
                case SunElementKind.OverheatPlatform:
                    definition.SunProfile.SafeSeconds = 2f;
                    definition.SunProfile.OverheatSeconds = 1f;
                    definition.SunProfile.Damage = 1;
                    definition.SunProfile.WaterCoolSeconds = 3f;
                    AddReaction(definition, ToolTag.Water, ElementReactionType.Disable);
                    break;
                case SunElementKind.SunsetFlower:
                    definition.SunProfile.InitialPhase = SunPhase.Day;
                    break;
                case SunElementKind.CrowPerch:
                    definition.SunProfile.AcceptedContextIds = new List<string> { "letter", "sun_ember" };
                    AddReaction(definition, ToolTag.Context, ElementReactionType.SetState);
                    break;
            }
        }

        private ReceiverRig CreateReceiver(string objectName)
        {
            var root = CreateGameObject(objectName);
            var collider = root.AddComponent<BoxCollider2D>();
            var component = root.AddComponent<TestSunReceiver>();
            return new ReceiverRig(root, collider, component);
        }

        private static void AddReaction(
            MapElementDefinition definition,
            ToolTag tool,
            ElementReactionType reaction)
        {
            definition.ToolReactions.Entries.Add(new ToolReactionEntry
            {
                Tool = tool,
                Reaction = reaction,
                StrengthRequired = 1,
            });
        }

        private GameObject CreateGameObject(string objectName)
        {
            var gameObject = new GameObject(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static ToolReactionContext Context(int actionId, ToolTag tags, GameObject source = null)
        {
            return new ToolReactionContext
            {
                ActionId = actionId,
                Tags = tags,
                Direction = Vector2Int.right,
                Magnitude = 4f,
                Source = source,
            };
        }

        private static void AssertAccepted(ToolReactionResult result)
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.ChangedState, Is.True);
            Assert.That(result.ConsumeToolResource, Is.True);
        }

        private readonly struct ElementRig
        {
            public ElementRig(
                GameObject root,
                MapElementInstance instance,
                SunElementDriver driver,
                ToolReactionReceiver reactions)
            {
                Root = root;
                Instance = instance;
                Driver = driver;
                Reactions = reactions;
            }

            public GameObject Root { get; }
            public MapElementInstance Instance { get; }
            public SunElementDriver Driver { get; }
            public ToolReactionReceiver Reactions { get; }
        }

        private readonly struct ReceiverRig
        {
            public ReceiverRig(GameObject root, Collider2D collider, TestSunReceiver component)
            {
                Root = root;
                Collider = collider;
                Component = component;
            }

            public GameObject Root { get; }
            public Collider2D Collider { get; }
            public TestSunReceiver Component { get; }
        }
    }

    public sealed class TestSunReceiver : MonoBehaviour,
        IMapElementDamageReceiver,
        IMapElementEnvironmentalReceiver,
        ISunLightSensitive,
        ISunShadowReceiver,
        ISunCoolingReceiver,
        ISunWateringCanReceiver
    {
        public int DamageReceived { get; private set; }
        public bool SunlightReceived { get; private set; }
        public bool ShadowReceived { get; private set; }
        public float CoolingReceived { get; private set; }
        public bool WateringCanFull { get; private set; }

        public bool ReceiveMapElementDamage(MapElementDamageEvent damageEvent)
        {
            DamageReceived += damageEvent.Damage;
            return true;
        }

        public void ReceiveWind(Vector2 velocityDelta) { }
        public void ReceiveWater(Vector2 velocityDelta) => CoolingReceived += velocityDelta.magnitude;
        public void ReceiveSunlight(bool active, float intensity) => SunlightReceived = active && intensity > 0f;
        public void ReceiveShadow(bool active) => ShadowReceived = active;
        public void ReceiveCooling(float amount) => CoolingReceived += amount;
        public void RefillWateringCanFully() => WateringCanFull = true;
    }

    public sealed class TestSunPerchOffering : MonoBehaviour, ISunPerchOffering
    {
        public string ContextIdValue;
        public string ContextId => ContextIdValue;
    }
}

#endif
