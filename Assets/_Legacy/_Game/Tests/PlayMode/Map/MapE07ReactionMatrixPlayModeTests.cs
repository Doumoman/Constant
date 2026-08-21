#if LEGACY_DISABLED
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Map.Placement;
using UnityEngine;
using UnityEngine.TestTools;

namespace StarNight.Map.Tests
{
    public sealed class MapE07ReactionMatrixPlayModeTests
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
        public IEnumerator SoftSoilUsesFinalGlobalReactionContractAndRejectedActionsConsumeNothing()
        {
            var soil = CreateElement(CommonElementKind.SoftSoil);
            soil.Definition.ToolReactions.Entries.Add(new ToolReactionEntry
            {
                Tool = ToolTag.Shovel,
                Reaction = ElementReactionType.Break,
                StrengthRequired = 1,
            });
            soil.Definition.ToolReactions.Entries.Add(new ToolReactionEntry
            {
                Tool = ToolTag.Pickaxe,
                Reaction = ElementReactionType.SetState,
                StrengthRequired = 1,
                ResultState = "SoftSoil",
            });
            soil.Definition.ToolReactions.Entries.Add(new ToolReactionEntry
            {
                Tool = ToolTag.Bomb,
                Reaction = ElementReactionType.SetState,
                StrengthRequired = 1,
                ResultState = "AbsorbExplosion",
            });

            var firstPickaxe = soil.Reactions.TryReact(Context(1, ToolTag.Pickaxe));
            Assert.That(firstPickaxe.Accepted, Is.True);
            Assert.That(firstPickaxe.ChangedState, Is.False);
            Assert.That(firstPickaxe.ConsumeToolResource, Is.True);
            Assert.That(firstPickaxe.Feedback, Is.EqualTo(FeedbackId.Hit));

            var duplicate = soil.Reactions.TryReact(Context(1, ToolTag.Pickaxe));
            AssertRejected(duplicate, FeedbackId.DuplicateAction);

            var undefined = soil.Reactions.TryReact(Context(2, ToolTag.Hook));
            AssertRejected(undefined, FeedbackId.None);

            var bomb = soil.Reactions.TryReact(Context(3, ToolTag.Bomb | ToolTag.HeavyImpact));
            Assert.That(bomb.Accepted, Is.True);
            Assert.That(bomb.ChangedState, Is.False);
            Assert.That(bomb.ConsumeToolResource, Is.False);
            Assert.That(soil.Instance.CurrentState, Is.EqualTo(MapElementState.Idle));

            var shovel = soil.Reactions.TryReact(Context(4, ToolTag.Shovel));
            Assert.That(shovel.Accepted, Is.True);
            Assert.That(shovel.ChangedState, Is.True);
            Assert.That(shovel.ConsumeToolResource, Is.True);
            Assert.That(soil.Instance.CurrentState, Is.EqualTo(MapElementState.Broken));

            var busy = CreateElement(CommonElementKind.CrackedBlock);
            busy.Definition.ToolReactions.Entries.Add(new ToolReactionEntry
            {
                Tool = ToolTag.Bomb,
                Reaction = ElementReactionType.Break,
                StrengthRequired = 1,
            });
            busy.Reactions.SetTransitionBusy(true);
            AssertRejected(busy.Reactions.TryReact(Context(20, ToolTag.Bomb)), FeedbackId.Busy);

            var unbreakable = CreateElement(CommonElementKind.UnbreakableBlock);
            unbreakable.Definition.ToolReactions.Entries.Add(new ToolReactionEntry
            {
                Tool = ToolTag.Bomb,
                Reaction = ElementReactionType.Break,
                StrengthRequired = 1,
            });
            AssertRejected(unbreakable.Reactions.TryReact(Context(30, ToolTag.Bomb)),
                FeedbackId.MetalFail);
            yield return null;
        }

        private ElementRig CreateElement(CommonElementKind kind)
        {
            var definition = ScriptableObject.CreateInstance<MapElementDefinition>();
            definition.ElementId = $"COMMON_Matrix_{kind}_{createdObjects.Count}";
            definition.CommonProfile.Kind = kind;
            definition.BehaviorProfile.InitialState = MapElementState.Idle;
            createdObjects.Add(definition);

            var root = new GameObject($"Matrix_{kind}");
            createdObjects.Add(root);
            var occupier = root.AddComponent<GridOccupier>();
            occupier.Configure(Vector2Int.zero, definition.Footprint, OccupancyLayer.Fixture);
            root.AddComponent<ElementRuntimeId>();
            root.AddComponent<ElementStateMachine>();
            var instance = root.AddComponent<MapElementInstance>();
            var driver = root.AddComponent<CommonElementDriver>();
            var reactions = root.AddComponent<ToolReactionReceiver>();
            instance.Configure(definition, null, $"matrix_{kind}_{createdObjects.Count}");
            instance.SetMapRoomState(MapRoomState.Active);
            driver.Rebind();
            return new ElementRig(definition, instance, reactions);
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

        private static void AssertRejected(ToolReactionResult result, FeedbackId feedback)
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.ChangedState, Is.False);
            Assert.That(result.ConsumeToolResource, Is.False);
            Assert.That(result.Feedback, Is.EqualTo(feedback));
        }

        private readonly struct ElementRig
        {
            public ElementRig(MapElementDefinition definition, MapElementInstance instance,
                ToolReactionReceiver reactions)
            {
                Definition = definition;
                Instance = instance;
                Reactions = reactions;
            }

            public MapElementDefinition Definition { get; }
            public MapElementInstance Instance { get; }
            public ToolReactionReceiver Reactions { get; }
        }
    }
}

#endif
