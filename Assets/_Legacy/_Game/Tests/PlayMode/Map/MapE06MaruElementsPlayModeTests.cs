#if LEGACY_DISABLED
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Map.Placement;
using UnityEngine;
using UnityEngine.TestTools;

namespace StarNight.Map.Tests
{
    public sealed class MapE06MaruElementsPlayModeTests
    {
        private readonly List<Object> createdObjects = new List<Object>();
        private MapE06MaruSink sink;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            sink = new MapE06MaruSink();
            MaruElementEventHub.Bind(sink);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            MaruElementEventHub.Unbind(sink);
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
        public IEnumerator AllSixElementsExposeVisibleRewardPenaltyAndApprovedInteractions()
        {
            var statue = CreateElement(MaruElementKind.ReturnStatue);
            var firstHit = statue.Reactions.TryReact(Context(1, ToolTag.Pickaxe));
            Assert.That(firstHit.Accepted, Is.True);
            Assert.That(firstHit.ChangedState, Is.True);
            Assert.That(statue.Driver.VariantState, Is.EqualTo("Cracked"));
            Assert.That(statue.Driver.PenaltyVisible, Is.True);
            var secondHit = statue.Reactions.TryReact(Context(2, ToolTag.Pickaxe));
            Assert.That(secondHit.ChangedState, Is.True);
            Assert.That(statue.Instance.CurrentState, Is.EqualTo(MapElementState.Broken));
            Assert.That(sink.Events, Does.Contain(MaruElementEventType.StatueBroken));

            var bellJar = CreateElement(MaruElementKind.ReturnBellJar);
            var jarBreak = bellJar.Reactions.TryReact(Context(3, ToolTag.Bomb));
            Assert.That(jarBreak.Accepted, Is.True);
            Assert.That(bellJar.Instance.CurrentState, Is.EqualTo(MapElementState.Broken));
            Assert.That(sink.Events, Does.Contain(MaruElementEventType.BellJarBroken));

            var collar = CreateElement(MaruElementKind.CollarFragment);
            var carrier = CreateGameObject("StoryCarryCarrier");
            Assert.That(collar.Driver.PressureWeight, Is.EqualTo(2));
            Assert.That(collar.Driver.TryInteract(carrier), Is.True);
            Assert.That(collar.Driver.IsCarried, Is.True);
            Assert.That(collar.Root.transform.parent, Is.EqualTo(carrier.transform));
            Assert.That(collar.Driver.CommitAtExit(), Is.True);
            Assert.That(sink.Events, Does.Contain(MaruElementEventType.CollarCommittedAtExit));

            var marker = CreateElement(MaruElementKind.ReturnMarker);
            Assert.That(marker.Driver.InteractionPrompt, Does.Contain("Entry SafeCell"));
            Assert.That(marker.Driver.TryInteract(null), Is.True);
            Assert.That(marker.Driver.RewardVisible, Is.True);
            Assert.That(marker.Driver.PenaltyVisible, Is.True);

            var pawprint = CreateElement(MaruElementKind.PawprintPool);
            var target = CreateGameObject("PawprintTarget");
            var targetCollider = target.AddComponent<BoxCollider2D>();
            pawprint.Driver.NotifyTriggerEnter(targetCollider);
            Assert.That(pawprint.Driver.VariantState, Is.EqualTo("Activated"));
            Assert.That(pawprint.Driver.RewardVisible, Is.True);
            Assert.That(pawprint.Driver.PenaltyVisible, Is.True);

            var casket = CreateElement(MaruElementKind.RecordCasket);
            var forbiddenTool = casket.Reactions.TryReact(Context(4, ToolTag.Bomb));
            Assert.That(forbiddenTool.Accepted, Is.False);
            Assert.That(forbiddenTool.ConsumeToolResource, Is.False);
            Assert.That(casket.Driver.TryInteract(null), Is.True);
            Assert.That(casket.Driver.VariantState, Is.EqualTo("Unsealed"));
            Assert.That(casket.Driver.TryInteract(null), Is.True);
            Assert.That(casket.Driver.VariantState, Is.EqualTo("Opened"));
            Assert.That(sink.Events, Does.Contain(MaruElementEventType.RecordTravelerFreed));
            yield return null;
        }

        private ElementRig CreateElement(MaruElementKind kind)
        {
            var definition = ScriptableObject.CreateInstance<MapElementDefinition>();
            definition.ElementId = $"MARU_Test_{kind}_{createdObjects.Count}";
            definition.MaruProfile.Kind = kind;
            definition.MaruProfile.PreviewRewardText = "Reward";
            definition.MaruProfile.PreviewPenaltyText = "Penalty";
            definition.BehaviorProfile.InitialState = MapElementState.Idle;
            ConfigureDefinition(definition, kind);
            createdObjects.Add(definition);

            var root = CreateGameObject($"Element_{kind}");
            var occupier = root.AddComponent<GridOccupier>();
            occupier.Configure(Vector2Int.zero, definition.Footprint, OccupancyLayer.Fixture);
            root.AddComponent<ElementRuntimeId>();
            root.AddComponent<ElementStateMachine>();
            var instance = root.AddComponent<MapElementInstance>();
            var driver = root.AddComponent<MaruElementDriver>();
            var reactions = root.AddComponent<ToolReactionReceiver>();
            instance.Configure(definition, null, $"e06_{kind}_{createdObjects.Count}");
            instance.SetMapRoomState(MapRoomState.Active);
            driver.Rebind();
            return new ElementRig(root, instance, driver, reactions);
        }

        private static void ConfigureDefinition(MapElementDefinition definition, MaruElementKind kind)
        {
            switch (kind)
            {
                case MaruElementKind.ReturnStatue:
                    definition.MaruProfile.DurabilityStages = 2;
                    definition.MaruProfile.RewardMoney = 500;
                    definition.MaruProfile.PressureWeight = 2;
                    definition.ToolReactions.Entries.Add(new ToolReactionEntry
                    {
                        Tool = ToolTag.Pickaxe,
                        Reaction = ElementReactionType.Break,
                        StrengthRequired = 2,
                    });
                    break;
                case MaruElementKind.ReturnBellJar:
                    definition.MaruProfile.RewardMoney = 300;
                    definition.MaruProfile.ScheduledEntryDelaySeconds = 12f;
                    definition.ToolReactions.Entries.Add(new ToolReactionEntry
                    {
                        Tool = ToolTag.Bomb,
                        Reaction = ElementReactionType.Break,
                        StrengthRequired = 1,
                    });
                    break;
                case MaruElementKind.CollarFragment:
                    definition.MaruProfile.TimerRateMultiplier = 1.15f;
                    definition.MaruProfile.PressureWeight = 2;
                    definition.MaruProfile.RewardId = "maru_clue_next_stage";
                    break;
                case MaruElementKind.ReturnMarker:
                    definition.MaruProfile.MarkerCostType = MaruMarkerCostType.Money;
                    definition.MaruProfile.MarkerCostValue = 50;
                    break;
                case MaruElementKind.PawprintPool:
                    definition.MaruProfile.GuidanceSeconds = 4f;
                    definition.MaruProfile.ShortenNextBellSeconds = 8f;
                    break;
                case MaruElementKind.RecordCasket:
                    definition.MaruProfile.DurabilityStages = 2;
                    definition.MaruProfile.RewardId = "record_traveler_freed";
                    break;
            }
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
                MapElementInstance instance,
                MaruElementDriver driver,
                ToolReactionReceiver reactions)
            {
                Root = root;
                Instance = instance;
                Driver = driver;
                Reactions = reactions;
            }

            public GameObject Root { get; }
            public MapElementInstance Instance { get; }
            public MaruElementDriver Driver { get; }
            public ToolReactionReceiver Reactions { get; }
        }
    }

    public sealed class MapE06MaruSink : IMaruElementEventSink
    {
        public readonly List<MaruElementEventType> Events = new List<MaruElementEventType>();
        public bool IsExitDiscovered { get; set; }

        public MaruElementEventResult ApplyMaruElementEvent(MaruElementEventRequest request)
        {
            Events.Add(request.EventType);
            return new MaruElementEventResult
            {
                Accepted = true,
                RewardGranted = request.EventType != MaruElementEventType.StatueWarning,
                PenaltyApplied = request.EventType != MaruElementEventType.RecordTravelerFreed,
                RewardText = request.EventType == MaruElementEventType.StatueWarning ? string.Empty : "Reward visible",
                PenaltyText = request.EventType == MaruElementEventType.RecordTravelerFreed ? string.Empty : "Penalty visible",
            };
        }
    }
}

#endif
