#if LEGACY_DISABLED
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Map.Placement;
using UnityEngine;
using UnityEngine.TestTools;

namespace StarNight.Map.Tests
{
    public sealed class MapE07BridgeElementsPlayModeTests
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
        public IEnumerator AllEightBridgeElementsPerformApprovedRuntimeAndToolReactions()
        {
            var threadBridge = CreateElement(BridgeElementKind.ThreadBridge);
            var heavyA = CreateHeavy("HeavyA");
            var heavyB = CreateHeavy("HeavyB");
            threadBridge.Driver.NotifyTriggerEnter(heavyA.GetComponent<Collider2D>());
            Assert.That(threadBridge.Driver.SagCells, Is.EqualTo(0.3f).Within(0.0001f));
            threadBridge.Driver.NotifyTriggerEnter(heavyB.GetComponent<Collider2D>());
            Assert.That(threadBridge.Instance.CurrentState, Is.EqualTo(MapElementState.Broken));

            var pulley = CreateElement(BridgeElementKind.KnotPulley, addBody: true);
            AssertAccepted(pulley.Reactions.TryReact(Context(1, ToolTag.Hook)));
            Assert.That(pulley.Driver.PulleyOffset, Is.EqualTo(-4f).Within(0.0001f));

            var banner = CreateElement(BridgeElementKind.WindBanner);
            var initialDirection = banner.Driver.WindDirection;
            banner.Driver.ReceiveSignal("bridge.wind", true);
            Assert.That(banner.Driver.WindDirection, Is.EqualTo(-initialDirection));
            AssertAccepted(banner.Reactions.TryReact(Context(2, ToolTag.Water)));
            Assert.That(banner.Driver.VariantState, Is.EqualTo("WetWeak"));

            var blade = CreateElement(BridgeElementKind.ThreadBlade, addBody: true);
            blade.Driver.TickForTests(1f);
            Assert.That(blade.Root.transform.localPosition.x, Is.EqualTo(3f).Within(0.01f));

            var magpie = CreateElement(BridgeElementKind.MagpiePlatform, addBody: true);
            Assert.That(magpie.Driver.TryInteract(null), Is.True);
            magpie.Driver.TickForTests(0.5f);
            Assert.That(magpie.Root.transform.localPosition.x, Is.GreaterThan(0f));
            AssertAccepted(magpie.Reactions.TryReact(Context(3, ToolTag.HeavyImpact)));
            Assert.That(magpie.Driver.VariantState, Is.EqualTo("HeavyDescending"));

            var updraft = CreateElement(BridgeElementKind.FeatherUpdraft);
            AssertAccepted(updraft.Reactions.TryReact(Context(4, ToolTag.WindGuard)));
            Assert.That(updraft.Driver.CurrentUpdraftMultiplier, Is.EqualTo(1.5f).Within(0.0001f));

            var panel = CreateElement(BridgeElementKind.BreakingStarPanel);
            var stepper = CreateGameObject("PanelStepper").AddComponent<BoxCollider2D>();
            panel.Driver.NotifyTriggerEnter(stepper);
            Assert.That(panel.Driver.LandingHitCount, Is.EqualTo(1));
            panel.Driver.NotifyTriggerEnter(stepper);
            Assert.That(panel.Instance.CurrentState, Is.EqualTo(MapElementState.Broken));

            var nest = CreateElement(BridgeElementKind.Nest);
            Assert.That(nest.Driver.TryInteract(null), Is.True);
            Assert.That(nest.Driver.TryInteract(null), Is.True);
            Assert.That(nest.Driver.TryInteract(null), Is.True);
            Assert.That(nest.Driver.RepairedPieces, Is.EqualTo(3));
            var bomb = nest.Reactions.TryReact(Context(5, ToolTag.Bomb));
            Assert.That(bomb.Accepted, Is.False);
            Assert.That(bomb.ConsumeToolResource, Is.False);
            AssertAccepted(nest.Reactions.TryReact(Context(6, ToolTag.Context)));
            Assert.That(nest.Driver.MoonCakeDelivered, Is.True);
            Assert.That(nest.Driver.VariantState, Is.EqualTo("MagpieSupportReady"));
            yield return null;
        }

        private ElementRig CreateElement(BridgeElementKind kind, bool addBody = false)
        {
            var definition = ScriptableObject.CreateInstance<MapElementDefinition>();
            definition.ElementId = $"BRIDGE_Test_{kind}_{createdObjects.Count}";
            definition.AllowedRegions = RegionMask.Bridge;
            definition.BridgeProfile.Kind = kind;
            definition.BehaviorProfile.InitialState = MapElementState.Idle;
            ConfigureDefinition(definition, kind);
            createdObjects.Add(definition);

            var root = CreateGameObject($"BridgeElement_{kind}");
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
            var driver = root.AddComponent<BridgeElementDriver>();
            var reactions = root.AddComponent<ToolReactionReceiver>();
            instance.Configure(definition, null, $"e07_bridge_{kind}_{createdObjects.Count}");
            instance.SetMapRoomState(MapRoomState.Active);
            driver.Rebind();
            return new ElementRig(root, instance, driver, reactions);
        }

        private static void ConfigureDefinition(MapElementDefinition definition, BridgeElementKind kind)
        {
            switch (kind)
            {
                case BridgeElementKind.ThreadBridge:
                    definition.BridgeProfile.LengthCells = 4;
                    definition.BridgeProfile.MaxWeight = 2;
                    definition.BridgeProfile.SagCells = 0.3f;
                    break;
                case BridgeElementKind.KnotPulley:
                    definition.BridgeProfile.TravelCells = 4f;
                    definition.BridgeProfile.WeightRatio = 1f;
                    AddReaction(definition, ToolTag.Hook, ElementReactionType.Toggle);
                    break;
                case BridgeElementKind.WindBanner:
                    definition.BridgeProfile.Direction = Vector2Int.right;
                    definition.BridgeProfile.FlipOnSignal = true;
                    AddReaction(definition, ToolTag.Water, ElementReactionType.SetState);
                    break;
                case BridgeElementKind.ThreadBlade:
                    definition.BridgeProfile.PathSpeedCellsPerSecond = 3f;
                    definition.BehaviorProfile.Path.Nodes.Add(Vector2.zero);
                    definition.BehaviorProfile.Path.Nodes.Add(new Vector2(4f, 0f));
                    break;
                case BridgeElementKind.MagpiePlatform:
                    definition.BridgeProfile.HeavyDescentMultiplier = 2f;
                    definition.BehaviorProfile.Path.Nodes.Add(Vector2.zero);
                    definition.BehaviorProfile.Path.Nodes.Add(new Vector2(4f, 0f));
                    definition.BehaviorProfile.Path.SpeedCellsPerSecond = 2f;
                    AddReaction(definition, ToolTag.HeavyImpact, ElementReactionType.Move);
                    break;
                case BridgeElementKind.FeatherUpdraft:
                    definition.BridgeProfile.UmbrellaLiftMultiplier = 1.5f;
                    AddReaction(definition, ToolTag.WindGuard, ElementReactionType.SetState);
                    break;
                case BridgeElementKind.BreakingStarPanel:
                    definition.BridgeProfile.HitCount = 2;
                    definition.BridgeProfile.DwellBreakSeconds = 0.5f;
                    break;
                case BridgeElementKind.Nest:
                    definition.BridgeProfile.RequiredPieces = 3;
                    definition.BridgeProfile.CriticalObject = true;
                    AddReaction(definition, ToolTag.Context, ElementReactionType.SetState);
                    break;
            }
        }

        private GameObject CreateHeavy(string objectName)
        {
            var gameObject = CreateGameObject(objectName);
            var body = gameObject.AddComponent<Rigidbody2D>();
            body.mass = 2f;
            body.gravityScale = 0f;
            gameObject.AddComponent<BoxCollider2D>();
            return gameObject;
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

        private static ToolReactionContext Context(int actionId, ToolTag tags)
        {
            return new ToolReactionContext
            {
                ActionId = actionId,
                Tags = tags,
                Direction = Vector2Int.right,
                Magnitude = 4f,
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
                BridgeElementDriver driver,
                ToolReactionReceiver reactions)
            {
                Root = root;
                Instance = instance;
                Driver = driver;
                Reactions = reactions;
            }

            public GameObject Root { get; }
            public MapElementInstance Instance { get; }
            public BridgeElementDriver Driver { get; }
            public ToolReactionReceiver Reactions { get; }
        }
    }
}

#endif
