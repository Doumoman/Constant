#if LEGACY_DISABLED
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Map.Placement;
using UnityEngine;
using UnityEngine.TestTools;

namespace StarNight.Map.Tests
{
    public sealed class MapE07PostElementsPlayModeTests
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
        public IEnumerator AllEightPostElementsPerformParcelToolAndPairContracts()
        {
            var conveyor = CreateElement(PostElementKind.Conveyor);
            var heavyParcel = CreateParcel("HeavyParcel", "OBJ_ParcelHeavy", true, 2f);
            conveyor.Driver.NotifyTriggerEnter(heavyParcel.Collider);
            Assert.That(conveyor.Driver.ConveyorStopped, Is.True);
            conveyor.Driver.NotifyTriggerExit(heavyParcel.Collider);
            Assert.That(conveyor.Driver.ConveyorStopped, Is.False);

            var launcher = CreateElement(PostElementKind.ParcelLauncher);
            var launchParcel = CreateParcel("LaunchParcel", "OBJ_ParcelSmall", false, 1f);
            Assert.That(launcher.Driver.TryInsertParcel(launchParcel.Root), Is.True);
            Assert.That(launchParcel.Body.linearVelocity.x, Is.GreaterThan(0f));
            Assert.That(launchParcel.Body.linearVelocity.y, Is.GreaterThan(0f));
            var playerParcel = CreateParcel("PlayerParcel", "OBJ_ParcelSmall", false, 1f);
            playerParcel.Root.AddComponent<TestPostPlayerMarker>();
            Assert.That(launcher.Driver.TryInsertParcel(playerParcel.Root), Is.False);

            var stamp = CreateElement(PostElementKind.ReturnStamp, addBody: true);
            AssertAccepted(stamp.Reactions.TryReact(Context(1, ToolTag.Hook)));
            stamp.Driver.TickForTests(0.7f);
            Assert.That(stamp.Driver.StampActive, Is.True);
            var markedParcel = CreateParcel("MarkedParcel", "OBJ_ParcelSmall", false, 1f);
            stamp.Driver.NotifyTriggerStay(markedParcel.Collider);
            Assert.That(markedParcel.Payload.Postmark, Is.EqualTo("Return"));

            var sortingArm = CreateElement(PostElementKind.SortingArm, addBody: true);
            AssertAccepted(sortingArm.Reactions.TryReact(Context(2, ToolTag.Context)));
            Assert.That(sortingArm.Driver.SortingSequenceIndex, Is.EqualTo(1));

            var mailA = CreateElement(PostElementKind.MailTube, pairGuid: "PLAY_MAIL_PAIR");
            var mailB = CreateElement(PostElementKind.MailTube, pairGuid: "PLAY_MAIL_PAIR");
            mailA.Root.transform.position = Vector3.zero;
            mailB.Root.transform.position = new Vector3(8f, 0f, 0f);
            var mailParcel = CreateParcel("MailParcel", "OBJ_ParcelSmall", false, 1f);
            Assert.That(mailA.Driver.RegisteredPairCount, Is.EqualTo(2));
            Assert.That(mailA.Driver.TryInsertParcel(mailParcel.Root), Is.True);
            Assert.That(mailParcel.Root.transform.position.x, Is.GreaterThan(8f));

            var ink = CreateElement(PostElementKind.InkPool);
            var inkParcel = CreateParcel("InkParcel", "OBJ_ParcelSmall", false, 1f);
            inkParcel.Body.linearVelocity = new Vector2(10f, 0f);
            ink.Driver.NotifyTriggerEnter(inkParcel.Collider);
            Assert.That(inkParcel.Body.linearVelocity.x, Is.EqualTo(6f).Within(0.001f));
            Assert.That(inkParcel.Payload.FootprintsRevealed, Is.True);
            AssertAccepted(ink.Reactions.TryReact(Context(3, ToolTag.Water)));
            Assert.That(ink.Driver.InkDiluted, Is.True);

            var stack = CreateElement(PostElementKind.ParcelStack);
            AssertAccepted(stack.Reactions.TryReact(Context(4, ToolTag.Pound)));
            Assert.That(stack.Driver.ParcelStackFlattened, Is.True);
            AssertAccepted(stack.Reactions.TryReact(Context(5, ToolTag.Bomb)));
            Assert.That(stack.Instance.CurrentState, Is.EqualTo(MapElementState.Broken));

            var expressA = CreateElement(PostElementKind.ExpressTube, pairGuid: "PLAY_EXPRESS_PAIR");
            var expressB = CreateElement(PostElementKind.ExpressTube, pairGuid: "PLAY_EXPRESS_PAIR");
            expressA.Root.transform.position = new Vector3(0f, 4f, 0f);
            expressB.Root.transform.position = new Vector3(10f, 4f, 0f);
            var expressParcel = CreateParcel("ExpressParcel", "OBJ_ParcelExpress", false, 1f);
            Assert.That(expressA.Driver.TryInsertParcel(expressParcel.Root), Is.True);
            Assert.That(expressA.Driver.ExpressActive, Is.True);
            var reverseParcel = CreateParcel("ReverseParcel", "OBJ_ParcelExpress", false, 1f);
            expressB.Driver.ReceiveSignal("post.express.enabled", true);
            Assert.That(expressB.Driver.TryInsertParcel(reverseParcel.Root), Is.False);
            yield return null;
        }

        private ElementRig CreateElement(
            PostElementKind kind,
            bool addBody = false,
            string pairGuid = null)
        {
            var definition = ScriptableObject.CreateInstance<MapElementDefinition>();
            definition.ElementId = $"POST_Test_{kind}_{createdObjects.Count}";
            definition.AllowedRegions = RegionMask.Post;
            definition.PostProfile.Kind = kind;
            definition.BehaviorProfile.InitialState = MapElementState.Idle;
            ConfigureDefinition(definition, kind, pairGuid);
            createdObjects.Add(definition);

            var root = CreateGameObject($"PostElement_{kind}");
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
            var driver = root.AddComponent<PostElementDriver>();
            var reactions = root.AddComponent<ToolReactionReceiver>();
            instance.Configure(definition, null, $"e07_post_{kind}_{createdObjects.Count}");
            instance.SetMapRoomState(MapRoomState.Active);
            driver.Rebind();
            return new ElementRig(root, instance, driver, reactions);
        }

        private static void ConfigureDefinition(
            MapElementDefinition definition,
            PostElementKind kind,
            string pairGuid)
        {
            switch (kind)
            {
                case PostElementKind.Conveyor:
                    definition.PostProfile.StopsOnHeavy = true;
                    definition.PostProfile.SurfaceSpeedCellsPerSecond = 2.5f;
                    break;
                case PostElementKind.ParcelLauncher:
                    definition.PostProfile.Direction = Vector2Int.right;
                    definition.PostProfile.LaunchPower = 10f;
                    definition.PostProfile.LaunchArc = 0.65f;
                    definition.PostProfile.RejectPlayerEntry = true;
                    break;
                case PostElementKind.ReturnStamp:
                    definition.PostProfile.WarningDelaySeconds = 0.7f;
                    definition.PostProfile.StampActiveSeconds = 0.15f;
                    definition.PostProfile.StampType = "Return";
                    AddReaction(definition, ToolTag.Hook, ElementReactionType.Toggle);
                    break;
                case PostElementKind.SortingArm:
                    AddReaction(definition, ToolTag.Context, ElementReactionType.Toggle);
                    break;
                case PostElementKind.MailTube:
                    definition.PostProfile.RequiresPair = true;
                    definition.PostProfile.PairGuid = pairGuid;
                    definition.PostProfile.OneWay = false;
                    break;
                case PostElementKind.InkPool:
                    definition.PostProfile.SlowRate = 0.4f;
                    definition.PostProfile.RevealsHiddenFootprints = true;
                    AddReaction(definition, ToolTag.Water, ElementReactionType.Disable);
                    break;
                case PostElementKind.ParcelStack:
                    AddReaction(definition, ToolTag.Pound, ElementReactionType.Move);
                    AddReaction(definition, ToolTag.Bomb, ElementReactionType.Break);
                    break;
                case PostElementKind.ExpressTube:
                    definition.PostProfile.RequiresPair = true;
                    definition.PostProfile.PairGuid = pairGuid;
                    definition.PostProfile.OneWay = true;
                    definition.PostProfile.RequiredParcelId = "OBJ_ParcelExpress";
                    definition.PostProfile.StartsActive = false;
                    break;
            }
        }

        private ParcelRig CreateParcel(string name, string parcelId, bool heavy, float mass)
        {
            var root = CreateGameObject(name);
            var body = root.AddComponent<Rigidbody2D>();
            body.mass = mass;
            body.gravityScale = 0f;
            var collider = root.AddComponent<BoxCollider2D>();
            var payload = root.AddComponent<TestPostParcelPayload>();
            payload.Configure(parcelId, heavy);
            return new ParcelRig(root, body, collider, payload);
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
                PostElementDriver driver,
                ToolReactionReceiver reactions)
            {
                Root = root;
                Instance = instance;
                Driver = driver;
                Reactions = reactions;
            }

            public GameObject Root { get; }
            public MapElementInstance Instance { get; }
            public PostElementDriver Driver { get; }
            public ToolReactionReceiver Reactions { get; }
        }

        private readonly struct ParcelRig
        {
            public ParcelRig(
                GameObject root,
                Rigidbody2D body,
                Collider2D collider,
                TestPostParcelPayload payload)
            {
                Root = root;
                Body = body;
                Collider = collider;
                Payload = payload;
            }

            public GameObject Root { get; }
            public Rigidbody2D Body { get; }
            public Collider2D Collider { get; }
            public TestPostParcelPayload Payload { get; }
        }
    }

    public sealed class TestPostParcelPayload : MonoBehaviour,
        IPostParcelPayload,
        IMapElementWeightSource,
        IPostMarkedParcel,
        IPostHiddenFootprintReceiver
    {
        public string ParcelId { get; private set; }
        public bool IsHeavyParcel { get; private set; }
        public int PressureWeight => IsHeavyParcel ? 2 : 1;
        public string Postmark { get; private set; }
        public bool FootprintsRevealed { get; private set; }

        public void Configure(string parcelId, bool heavy)
        {
            ParcelId = parcelId;
            IsHeavyParcel = heavy;
        }

        public void ApplyPostmark(string stampType) => Postmark = stampType;
        public void RevealHiddenFootprints(bool revealed) => FootprintsRevealed = revealed;
    }

    public sealed class TestPostPlayerMarker : MonoBehaviour, IPostPlayerMarker
    {
    }
}

#endif
