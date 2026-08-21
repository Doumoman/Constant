#if LEGACY_DISABLED
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Map.Placement;
using UnityEngine;
using UnityEngine.TestTools;

namespace StarNight.Map.Tests
{
    public sealed class MapE07MoonElementsPlayModeTests
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
        public IEnumerator AllEightMoonElementsPerformApprovedRuntimeAndToolReactions()
        {
            var ironBall = CreateElement(MoonElementKind.MoonIronBall);
            AssertAccepted(ironBall.Reactions.TryReact(Context(1, ToolTag.Hook)));
            Assert.That(ironBall.Driver.VariantState, Is.EqualTo("OrbitPulled"));

            var mortar = CreateElement(MoonElementKind.FallingMortar, addBody: true);
            AssertAccepted(mortar.Reactions.TryReact(Context(2, ToolTag.Bomb)));
            Assert.That(mortar.Instance.CurrentState, Is.EqualTo(MapElementState.Warning));
            mortar.Driver.TickForTests(0.76f);
            Assert.That(mortar.Instance.CurrentState, Is.EqualTo(MapElementState.Active));
            Assert.That(mortar.Driver.VariantState, Is.EqualTo("Falling"));

            var dough = CreateElement(MoonElementKind.DoughPlatform);
            AssertAccepted(dough.Reactions.TryReact(Context(3, ToolTag.Water)));
            Assert.That(dough.Driver.VariantState, Is.EqualTo("Sticky"));
            AssertAccepted(dough.Reactions.TryReact(Context(4, ToolTag.Pound)));
            Assert.That(dough.Driver.VariantState, Is.EqualTo("BouncePad"));

            var slab = CreateElement(MoonElementKind.CraterSlab, addBody: true);
            var stepper = CreateGameObject("CraterStepper").AddComponent<BoxCollider2D>();
            slab.Driver.NotifyTriggerEnter(stepper);
            Assert.That(slab.Instance.CurrentState, Is.EqualTo(MapElementState.Warning));
            slab.Driver.TickForTests(0.51f);
            Assert.That(slab.Driver.VariantState, Is.EqualTo("Falling"));

            var root = CreateElement(MoonElementKind.CassiaRoot);
            root.Driver.ReceiveSignal("moon.root", false);
            Assert.That(root.Driver.CurrentSegmentCount, Is.EqualTo(2));
            AssertAccepted(root.Reactions.TryReact(Context(5, ToolTag.Water)));
            Assert.That(root.Driver.CurrentSegmentCount, Is.EqualTo(4));
            AssertAccepted(root.Reactions.TryReact(Context(6, ToolTag.Hook)));
            Assert.That(root.Driver.CurrentSegmentCount, Is.EqualTo(3));

            var shaft = CreateElement(MoonElementKind.MillShaft, addBody: true);
            AssertAccepted(shaft.Reactions.TryReact(Context(7, ToolTag.Hook)));
            shaft.Driver.TickForTests(0.51f);
            Assert.That(shaft.Driver.VariantState, Is.EqualTo("Stepped90"));

            var medicine = CreateElement(MoonElementKind.MedicineMortar);
            Assert.That(medicine.Driver.TryInteract(null), Is.True);
            Assert.That(medicine.Driver.TryInteract(null), Is.True);
            AssertAccepted(medicine.Reactions.TryReact(Context(8, ToolTag.Pound)));
            Assert.That(medicine.Driver.OutputReady, Is.True);
            Assert.That(medicine.Driver.VariantState, Is.EqualTo("MedicineReady"));

            var vent = CreateElement(MoonElementKind.FlourVent);
            AssertAccepted(vent.Reactions.TryReact(Context(9, ToolTag.Water)));
            Assert.That(vent.Instance.CurrentState, Is.EqualTo(MapElementState.Disabled));
            vent.Driver.TickForTests(2.1f);
            Assert.That(vent.Instance.CurrentState, Is.EqualTo(MapElementState.Idle));
            Assert.That(vent.Driver.IsVentActive, Is.True);
            yield return null;
        }

        private ElementRig CreateElement(MoonElementKind kind, bool addBody = false)
        {
            var definition = ScriptableObject.CreateInstance<MapElementDefinition>();
            definition.ElementId = $"MOON_Test_{kind}_{createdObjects.Count}";
            definition.AllowedRegions = RegionMask.Moon;
            definition.MoonProfile.Kind = kind;
            definition.BehaviorProfile.InitialState = MapElementState.Idle;
            ConfigureDefinition(definition, kind);
            createdObjects.Add(definition);

            var root = CreateGameObject($"MoonElement_{kind}");
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
            var driver = root.AddComponent<MoonElementDriver>();
            var reactions = root.AddComponent<ToolReactionReceiver>();
            instance.Configure(definition, null, $"e07_moon_{kind}_{createdObjects.Count}");
            instance.SetMapRoomState(MapRoomState.Active);
            driver.Rebind();
            return new ElementRig(instance, driver, reactions);
        }

        private static void ConfigureDefinition(MapElementDefinition definition, MoonElementKind kind)
        {
            switch (kind)
            {
                case MoonElementKind.MoonIronBall:
                    AddReaction(definition, ToolTag.Hook, ElementReactionType.Pull);
                    break;
                case MoonElementKind.FallingMortar:
                    definition.MoonProfile.ShadowWarningSeconds = 0.75f;
                    AddReaction(definition, ToolTag.Bomb, ElementReactionType.SetState);
                    break;
                case MoonElementKind.DoughPlatform:
                    AddReaction(definition, ToolTag.Water, ElementReactionType.SetState);
                    AddReaction(definition, ToolTag.Pound, ElementReactionType.SetState);
                    break;
                case MoonElementKind.CraterSlab:
                    definition.MoonProfile.FallDelaySeconds = 0.5f;
                    break;
                case MoonElementKind.CassiaRoot:
                    definition.MoonProfile.MinimumSegmentCount = 2;
                    definition.MoonProfile.SegmentCount = 4;
                    AddReaction(definition, ToolTag.Water, ElementReactionType.SetState);
                    AddReaction(definition, ToolTag.Hook, ElementReactionType.Pull);
                    break;
                case MoonElementKind.MillShaft:
                    definition.MoonProfile.StepAngleDegrees = 90f;
                    definition.MoonProfile.RotationSpeedDegreesPerSecond = 180f;
                    AddReaction(definition, ToolTag.Hook, ElementReactionType.Toggle);
                    break;
                case MoonElementKind.MedicineMortar:
                    definition.MoonProfile.InputSlots = 2;
                    AddReaction(definition, ToolTag.Pound, ElementReactionType.SetState);
                    break;
                case MoonElementKind.FlourVent:
                    definition.MoonProfile.CycleOnSeconds = 1.2f;
                    definition.MoonProfile.CycleOffSeconds = 1f;
                    definition.MoonProfile.WaterDisableSeconds = 2f;
                    AddReaction(definition, ToolTag.Water, ElementReactionType.Disable);
                    break;
            }
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
                MoonElementDriver driver,
                ToolReactionReceiver reactions)
            {
                Instance = instance;
                Driver = driver;
                Reactions = reactions;
            }

            public MapElementInstance Instance { get; }
            public MoonElementDriver Driver { get; }
            public ToolReactionReceiver Reactions { get; }
        }
    }
}

#endif
