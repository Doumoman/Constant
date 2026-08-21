#if LEGACY_DISABLED
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Map.Placement;
using UnityEngine;
using UnityEngine.TestTools;

namespace StarNight.Map.Tests
{
    public sealed class MapE07PalaceElementsPlayModeTests
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
        public IEnumerator AllEightPalaceElementsPerformWaterAndToolContracts()
        {
            var gate = CreateElement(PalaceElementKind.SluiceGate, addBody: true);
            AssertAccepted(gate.Reactions.TryReact(Context(1, ToolTag.Hook)));
            gate.Driver.TickForTests(2f);
            Assert.That(gate.Driver.GateOpenProgress, Is.EqualTo(1f).Within(0.001f));
            var bomb = gate.Reactions.TryReact(Context(2, ToolTag.Bomb));
            Assert.That(bomb.Accepted, Is.False);
            Assert.That(bomb.ConsumeToolResource, Is.False);

            var cannon = CreateElement(PalaceElementKind.BubbleCannon);
            var bubble = cannon.Driver.FireBubble();
            Assert.That(bubble, Is.Not.Null);
            createdObjects.Add(bubble);
            Assert.That(cannon.Driver.BubbleShotsFired, Is.EqualTo(1));

            var current = CreateElement(PalaceElementKind.CurrentVolume);
            AssertAccepted(current.Reactions.TryReact(Context(3, ToolTag.HeavyImpact)));
            Assert.That(current.Driver.CurrentPartiallyBlocked, Is.True);

            var turtle = CreateElement(PalaceElementKind.TurtlePlatform, addBody: true);
            var heavy = CreateDynamicBody("PalaceHeavy", 2f);
            turtle.Driver.NotifyTriggerEnter(heavy.GetComponent<Collider2D>());
            turtle.Driver.TickForTests(0.6f);
            Assert.That(turtle.Driver.SinkProgress, Is.GreaterThan(0f));

            var clam = CreateElement(PalaceElementKind.ClamBounce, addBody: true);
            var launched = CreateDynamicBody("ClamPassenger", 1f);
            Assert.That(clam.Driver.IsClamOpen, Is.True);
            clam.Driver.NotifyTriggerEnter(launched.GetComponent<Collider2D>());
            Assert.That(launched.GetComponent<Rigidbody2D>().linearVelocity.y, Is.GreaterThan(0f));

            var mirror = CreateElement(PalaceElementKind.WaterMirrorWall);
            AssertAccepted(mirror.Reactions.TryReact(Context(4, ToolTag.Context)));
            Assert.That(mirror.Driver.MirrorTransparent, Is.True);

            var drain = CreateElement(PalaceElementKind.DrainGrate);
            Assert.That(drain.Driver.MudBlocked, Is.True);
            AssertAccepted(drain.Reactions.TryReact(Context(5, ToolTag.Shovel)));
            AssertAccepted(drain.Reactions.TryReact(Context(6, ToolTag.Hook)));
            drain.Driver.TickForTests(2f);
            Assert.That(drain.Driver.DrainOpen, Is.True);
            Assert.That(drain.Driver.WaterLevelDeltaTotal, Is.LessThan(0f));

            var waterfall = CreateElement(PalaceElementKind.DragonGateWaterfall);
            Assert.That(waterfall.Driver.WaterfallActive, Is.True);
            AssertAccepted(waterfall.Reactions.TryReact(Context(7, ToolTag.WindGuard)));
            AssertAccepted(waterfall.Reactions.TryReact(Context(8, ToolTag.Water)));
            Assert.That(waterfall.Driver.VariantState, Is.EqualTo("WateringCanCharged"));
            yield return null;
        }

        private ElementRig CreateElement(PalaceElementKind kind, bool addBody = false)
        {
            var definition = ScriptableObject.CreateInstance<MapElementDefinition>();
            definition.ElementId = $"PALACE_Test_{kind}_{createdObjects.Count}";
            definition.AllowedRegions = RegionMask.Palace;
            definition.PalaceProfile.Kind = kind;
            definition.BehaviorProfile.InitialState = kind == PalaceElementKind.DragonGateWaterfall
                ? MapElementState.Active
                : MapElementState.Idle;
            ConfigureDefinition(definition, kind);
            createdObjects.Add(definition);

            var root = CreateGameObject($"PalaceElement_{kind}");
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
            var driver = root.AddComponent<PalaceElementDriver>();
            var reactions = root.AddComponent<ToolReactionReceiver>();
            instance.Configure(definition, null, $"e07_palace_{kind}_{createdObjects.Count}");
            instance.SetMapRoomState(MapRoomState.Active);
            driver.Rebind();
            return new ElementRig(instance, driver, reactions);
        }

        private static void ConfigureDefinition(MapElementDefinition definition, PalaceElementKind kind)
        {
            switch (kind)
            {
                case PalaceElementKind.SluiceGate:
                    definition.PalaceProfile.HeightCells = 3;
                    definition.PalaceProfile.MoveSpeedCellsPerSecond = 2f;
                    AddReaction(definition, ToolTag.Hook, ElementReactionType.Toggle);
                    break;
                case PalaceElementKind.CurrentVolume:
                    AddReaction(definition, ToolTag.HeavyImpact, ElementReactionType.Disable);
                    break;
                case PalaceElementKind.WaterMirrorWall:
                    AddReaction(definition, ToolTag.Context, ElementReactionType.SetState);
                    break;
                case PalaceElementKind.DrainGrate:
                    definition.PalaceProfile.StartsMudBlocked = true;
                    AddReaction(definition, ToolTag.Shovel, ElementReactionType.SetState);
                    AddReaction(definition, ToolTag.Hook, ElementReactionType.Toggle);
                    break;
                case PalaceElementKind.DragonGateWaterfall:
                    definition.PalaceProfile.StartsActive = true;
                    definition.PalaceProfile.CanRefillWateringCan = true;
                    AddReaction(definition, ToolTag.WindGuard, ElementReactionType.SetState);
                    AddReaction(definition, ToolTag.Water, ElementReactionType.SetState);
                    break;
            }
        }

        private GameObject CreateDynamicBody(string objectName, float mass)
        {
            var target = CreateGameObject(objectName);
            var body = target.AddComponent<Rigidbody2D>();
            body.mass = mass;
            body.gravityScale = 0f;
            target.AddComponent<BoxCollider2D>();
            return target;
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
                MapElementInstance instance,
                PalaceElementDriver driver,
                ToolReactionReceiver reactions)
            {
                Instance = instance;
                Driver = driver;
                Reactions = reactions;
            }

            public MapElementInstance Instance { get; }
            public PalaceElementDriver Driver { get; }
            public ToolReactionReceiver Reactions { get; }
        }
    }
}

#endif
