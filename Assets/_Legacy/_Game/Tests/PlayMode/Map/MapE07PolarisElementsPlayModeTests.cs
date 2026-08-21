#if LEGACY_DISABLED
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Map.Placement;
using UnityEngine;
using UnityEngine.TestTools;

namespace StarNight.Map.Tests
{
    public sealed class MapE07PolarisElementsPlayModeTests
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
        public IEnumerator AllEightPolarisElementsPerformObservatoryContracts()
        {
            var orbit = CreateElement(PolarisElementKind.OrbitPlatform);
            var orbitOrigin = orbit.Root.transform.position;
            orbit.Driver.TickForTests(0.5f);
            Assert.That(Vector3.Distance(orbit.Root.transform.position, orbitOrigin), Is.GreaterThan(0.1f));
            orbit.Driver.ReceiveSignal("gravity.dial", true);
            Assert.That(orbit.Driver.AlternateOrbit, Is.True);

            var receiver = CreateReceiver("PolarisBeamReceiver");
            var beam = CreateElement(PolarisElementKind.ObservationBeam);
            beam.Driver.TickForTests(0.25f);
            var originalBeamDirection = beam.Driver.BeamDirection;
            beam.Driver.NotifyTriggerStay(receiver.Collider);
            Assert.That(receiver.Component.Observed, Is.True);
            Assert.That(receiver.Component.ReturnMarked, Is.True);
            Assert.That(receiver.Component.DamageReceived, Is.EqualTo(1));
            var mirror = CreateGameObject("PolarisMirror");
            mirror.AddComponent<TestPolarisMirror>();
            Assert.That(beam.Driver.TryReflectBeam(mirror), Is.True);
            Assert.That(Vector2.Dot(originalBeamDirection.normalized, beam.Driver.BeamDirection.normalized),
                Is.LessThan(0f));
            var reflectedAngle = beam.Driver.BeamAngle;
            beam.Driver.ReceiveSignal("beam.reverse", true);
            beam.Driver.TickForTests(0.2f);
            Assert.That(beam.Driver.BeamAngle, Is.Not.EqualTo(reflectedAngle));

            var entryAnchor = CreateGameObject("EntryAnchor");
            entryAnchor.transform.position = new Vector3(5f, 3f, 0f);
            var returnTarget = CreateGameObject("ReturnTarget");
            returnTarget.transform.position = new Vector3(-2f, -1f, 0f);
            var returnCollider = returnTarget.AddComponent<BoxCollider2D>();
            var returnField = CreateElement(PolarisElementKind.ReturnField);
            returnField.Driver.ConfigureEntryAnchor(entryAnchor.transform);
            returnField.Driver.NotifyTriggerEnter(returnCollider);
            Assert.That(returnField.Driver.PendingReturnCount, Is.EqualTo(1));
            returnField.Driver.TickForTests(0.5f);
            Assert.That(returnTarget.transform.position, Is.EqualTo(entryAnchor.transform.position));

            var weight = CreateElement(PolarisElementKind.StarWeight);
            AssertAccepted(weight.Reactions.TryReact(Context(1, ToolTag.Context)));
            Assert.That(weight.Driver.StarWeightCarryReady, Is.True);
            var weightPosition = weight.Root.transform.position;
            AssertAccepted(weight.Reactions.TryReact(Context(2, ToolTag.Hook)));
            Assert.That(weight.Root.transform.position, Is.Not.EqualTo(weightPosition));
            Assert.That(weight.Driver.PressureWeight, Is.EqualTo(2));

            var gravityDial = CreateElement(PolarisElementKind.GravityDial);
            Assert.That(gravityDial.Driver.LowGravity, Is.False);
            AssertAccepted(gravityDial.Reactions.TryReact(Context(3, ToolTag.Context)));
            Assert.That(gravityDial.Driver.LowGravity, Is.True);
            Assert.That(gravityDial.Driver.CurrentGravityScale, Is.EqualTo(0.45f).Within(0.0001f));
            AssertAccepted(gravityDial.Reactions.TryReact(Context(4, ToolTag.Hook)));
            Assert.That(gravityDial.Driver.LowGravity, Is.False);

            var artifact = CreateGameObject("PolarisArtifact");
            artifact.AddComponent<TestPolarisArtifact>();
            var bridge = CreateElement(PolarisElementKind.ConstellationBridge);
            Assert.That(bridge.Driver.GenerateBridgeCell(null), Is.False);
            AssertAccepted(bridge.Reactions.TryReact(Context(5, ToolTag.Context, artifact)));
            Assert.That(bridge.Driver.GeneratedBridgeCells, Is.EqualTo(1));

            var bell = CreateElement(PolarisElementKind.MemoryBell);
            CollectionAssert.AreEqual(new[] { 0, 1, 0, 2 }, bell.Definition.PolarisProfile.RhythmPattern);
            foreach (var note in bell.Definition.PolarisProfile.RhythmPattern)
            {
                Assert.That(bell.Driver.SubmitRhythmInput(note), Is.True);
            }
            Assert.That(bell.Driver.MemoryReplayComplete, Is.True);

            var immutable = CreateElement(PolarisElementKind.ImmutableStarBlock);
            var rejected = immutable.Reactions.TryReact(Context(6, ToolTag.Bomb));
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.ConsumeToolResource, Is.False);
            Assert.That(immutable.Instance.CurrentState, Is.EqualTo(MapElementState.Idle));
            yield return null;
        }

        private ElementRig CreateElement(PolarisElementKind kind)
        {
            var definition = ScriptableObject.CreateInstance<MapElementDefinition>();
            definition.ElementId = $"POLARIS_Test_{kind}_{createdObjects.Count}";
            definition.AllowedRegions = RegionMask.Polaris;
            definition.PolarisProfile.Kind = kind;
            definition.BehaviorProfile.InitialState = MapElementState.Idle;
            ConfigureDefinition(definition, kind);
            createdObjects.Add(definition);

            var root = CreateGameObject($"PolarisElement_{kind}");
            var occupier = root.AddComponent<GridOccupier>();
            occupier.Configure(Vector2Int.zero, definition.Footprint, OccupancyLayer.Fixture);
            root.AddComponent<ElementRuntimeId>();
            root.AddComponent<ElementStateMachine>();
            var instance = root.AddComponent<MapElementInstance>();
            var driver = root.AddComponent<PolarisElementDriver>();
            var reactions = root.AddComponent<ToolReactionReceiver>();
            instance.Configure(definition, null, $"e07_polaris_{kind}_{createdObjects.Count}");
            instance.SetMapRoomState(MapRoomState.Active);
            driver.Rebind();
            return new ElementRig(root, definition, instance, driver, reactions);
        }

        private static void ConfigureDefinition(MapElementDefinition definition, PolarisElementKind kind)
        {
            definition.Footprint.BoundsSize = Vector2Int.one;
            definition.Footprint.OccupiedCells.Add(Vector2Int.zero);
            switch (kind)
            {
                case PolarisElementKind.OrbitPlatform:
                    definition.PolarisProfile.OrbitRadiusCells = new Vector2(3f, 2f);
                    definition.PolarisProfile.OrbitPeriodSeconds = 4f;
                    definition.PolarisProfile.DialOrbitMultiplier = 0.65f;
                    break;
                case PolarisElementKind.ObservationBeam:
                    definition.PolarisProfile.BeamRangeCells = 8f;
                    definition.PolarisProfile.SweepDegrees = 90f;
                    definition.PolarisProfile.SweepPeriodSeconds = 3f;
                    definition.PolarisProfile.Damage = 1;
                    definition.PolarisProfile.AppliesReturnMark = true;
                    definition.PolarisProfile.MirrorCanReflect = true;
                    definition.PolarisProfile.SignalChangesDirection = true;
                    break;
                case PolarisElementKind.ReturnField:
                    definition.PolarisProfile.ReturnDelaySeconds = 0.5f;
                    definition.PolarisProfile.DestinationAnchorId = "EntryAnchor";
                    break;
                case PolarisElementKind.StarWeight:
                    definition.PolarisProfile.PressureWeight = 2;
                    definition.PolarisProfile.GravityDirection = Vector2Int.down;
                    AddReaction(definition, ToolTag.Context, ElementReactionType.Move);
                    AddReaction(definition, ToolTag.Hook, ElementReactionType.Pull);
                    break;
                case PolarisElementKind.GravityDial:
                    definition.PolarisProfile.LowGravityScale = 0.45f;
                    definition.PolarisProfile.NormalGravityScale = 1f;
                    AddReaction(definition, ToolTag.Context, ElementReactionType.Toggle);
                    AddReaction(definition, ToolTag.Hook, ElementReactionType.Toggle);
                    break;
                case PolarisElementKind.ConstellationBridge:
                    definition.PolarisProfile.BridgeCellCount = 6;
                    AddReaction(definition, ToolTag.Context, ElementReactionType.SetState);
                    break;
                case PolarisElementKind.MemoryBell:
                    definition.PolarisProfile.RhythmPattern = new List<int> { 0, 1, 0, 2 };
                    AddReaction(definition, ToolTag.Context, ElementReactionType.SetState);
                    break;
                case PolarisElementKind.ImmutableStarBlock:
                    definition.PolarisProfile.IgnoreAllTools = true;
                    break;
            }
        }

        private ReceiverRig CreateReceiver(string objectName)
        {
            var root = CreateGameObject(objectName);
            var collider = root.AddComponent<BoxCollider2D>();
            var component = root.AddComponent<TestPolarisReceiver>();
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
                Magnitude = 2f,
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
            public ElementRig(GameObject root, MapElementDefinition definition,
                MapElementInstance instance, PolarisElementDriver driver,
                ToolReactionReceiver reactions)
            {
                Root = root;
                Definition = definition;
                Instance = instance;
                Driver = driver;
                Reactions = reactions;
            }

            public GameObject Root { get; }
            public MapElementDefinition Definition { get; }
            public MapElementInstance Instance { get; }
            public PolarisElementDriver Driver { get; }
            public ToolReactionReceiver Reactions { get; }
        }

        private readonly struct ReceiverRig
        {
            public ReceiverRig(GameObject root, Collider2D collider, TestPolarisReceiver component)
            {
                Root = root;
                Collider = collider;
                Component = component;
            }

            public GameObject Root { get; }
            public Collider2D Collider { get; }
            public TestPolarisReceiver Component { get; }
        }
    }

    public sealed class TestPolarisReceiver : MonoBehaviour,
        IMapElementDamageReceiver,
        IPolarisObservationReceiver,
        IPolarisReturnMarkReceiver
    {
        public int DamageReceived { get; private set; }
        public bool Observed { get; private set; }
        public bool ReturnMarked { get; private set; }

        public bool ReceiveMapElementDamage(MapElementDamageEvent damageEvent)
        {
            DamageReceived += damageEvent.Damage;
            return true;
        }

        public void ReceiveObservationBeam(bool active) => Observed = active;
        public void ApplyReturnMark() => ReturnMarked = true;
    }

    public sealed class TestPolarisMirror : MonoBehaviour, IPolarisBeamMirror
    {
        public Vector2 ReflectObservationBeam(Vector2 incomingDirection) => -incomingDirection;
    }

    public sealed class TestPolarisArtifact : MonoBehaviour, IPolarisArtifactPayload
    {
        public string ArtifactId => "artifact.north_star";
    }
}

#endif
