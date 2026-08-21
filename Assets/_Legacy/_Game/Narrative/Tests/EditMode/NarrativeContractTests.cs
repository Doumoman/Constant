#if LEGACY_DISABLED
using NUnit.Framework;
using StarNight.Core.State;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Yarn.Unity;

namespace StarNight.Narrative.Tests
{
    public sealed class NarrativeContractTests
    {
        private const string ProjectPath = "Assets/_Game/Narrative/Data/StarNightNarrative.yarnproject";

        [Test]
        public void YarnProjectCompilesExactlyThreeCoreSampleNodes()
        {
            YarnProject project = AssetDatabase.LoadAssetAtPath<YarnProject>(ProjectPath);
            Assert.That(project, Is.Not.Null);
            Assert.That(project.NodeNames, Is.EquivalentTo(new[]
            {
                "STG_MOON_1_1_Intro",
                "NPC_Moon_Dabok_FirstMeet",
                "OBS_MOON_1_2_StatueBroken",
            }));
        }

        [Test]
        public void FieldQueueDiscardsOldestNonessentialLine()
        {
            var queue = new NarrativeRequestQueue();
            Assert.That(queue.Enqueue(new NarrativeRequest("NPC.A", NarrativeMode.Bubble, false, false)), Is.True);
            Assert.That(queue.Enqueue(new NarrativeRequest("NPC.B", NarrativeMode.Bubble, false, false)), Is.True);
            Assert.That(queue.Count, Is.EqualTo(1));
            Assert.That(queue.Contains("NPC.A"), Is.False);
            Assert.That(queue.Contains("NPC.B"), Is.True);
        }

        [Test]
        public void CharacterDatabaseResolvesIdsAndKeepsLocalizationKeys()
        {
            CharacterDatabase database = ScriptableObject.CreateInstance<CharacterDatabase>();
            database.Configure(new[]
            {
                new CharacterPresentation { characterId = "DABOK", nameKey = "character.dabok", displayName = "다복" },
            });
            Assert.That(database.TryGet("DABOK", out CharacterPresentation character), Is.True);
            Assert.That(character.nameKey, Is.EqualTo("character.dabok"));
            Assert.That(database.ResolveDisplayName("DABOK"), Is.EqualTo("다복"));
            Object.DestroyImmediate(database);
        }

        [Test]
        public void CommandBridgeValidatesMoneyAndItemRequestsInCSharp()
        {
            var root = new GameObject("NarrativeCommandTest");
            DialogueRunner runner = root.AddComponent<DialogueRunner>();
            NarrativeUIState state = root.AddComponent<NarrativeUIState>();
            NarrativeService service = root.AddComponent<NarrativeService>();
            service.Configure(runner, state);
            NarrativeCommandBridge bridge = root.AddComponent<NarrativeCommandBridge>();
            bridge.Configure(runner, service, state);
            var manager = new RunManager(() => 77);
            RunState run = manager.StartNewRun();
            bridge.ConfigureAuthoritiesForTests(manager);

            Assert.That(bridge.TryRequestMoney(1500, "test.reward"), Is.True);
            Assert.That(run.moneyWon, Is.EqualTo(1500));
            LogAssert.Expect(LogType.Error, "Narrative command rejected: request_money:test.invalid");
            Assert.That(bridge.TryRequestMoney(-2000, "test.invalid"), Is.False);
            Assert.That(run.moneyWon, Is.EqualTo(1500));
            Assert.That(bridge.TryRequestGive("ITEM.STAR_DUST", 1), Is.True);
            Assert.That(bridge.HasItem("ITEM.STAR_DUST"), Is.True);
            LogAssert.Expect(LogType.Error, "Narrative command rejected: request_take:ITEM.UNKNOWN");
            Assert.That(bridge.TryRequestTake("ITEM.UNKNOWN", 1), Is.False);
            Assert.That(bridge.InvalidRequestCount, Is.EqualTo(2));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void ToolQueriesAndPresentationCommandsRemainReadOnly()
        {
            var root = new GameObject("NarrativeToolQueryTest");
            DialogueRunner runner = root.AddComponent<DialogueRunner>();
            NarrativeUIState state = root.AddComponent<NarrativeUIState>();
            NarrativeService service = root.AddComponent<NarrativeService>();
            service.Configure(runner, state);
            NarrativeCommandBridge bridge = root.AddComponent<NarrativeCommandBridge>();
            bridge.Configure(runner, service, state);
            var manager = new RunManager(() => 91);
            RunState run = manager.StartNewRun();
            run.handToolId = "TOOL_PICKAXE";
            run.bombs = 2;
            run.ropes = 3;
            run.moneyWon = 500;
            bridge.ConfigureAuthoritiesForTests(manager);

            string focused = string.Empty;
            string hinted = string.Empty;
            bridge.ToolFocusRequested += request => focused = request.Id;
            bridge.ControlHintRequested += request => hinted = request.Id;

            Assert.That(bridge.HasTool("TOOL_PICKAXE"), Is.True);
            Assert.That(bridge.HandSlotEmpty(), Is.False);
            Assert.That(bridge.BombCount(), Is.EqualTo(2));
            Assert.That(bridge.RopeCount(), Is.EqualTo(3));
            Assert.That(bridge.TryFocusTool("TOOL_SHOVEL"), Is.True);
            Assert.That(bridge.TryShowControlHint("PRIMARY_ACTION"), Is.True);
            Assert.That(focused, Is.EqualTo("TOOL_SHOVEL"));
            Assert.That(hinted, Is.EqualTo("PRIMARY_ACTION"));
            Assert.That(run.handToolId, Is.EqualTo("TOOL_PICKAXE"));
            Assert.That(run.moneyWon, Is.EqualTo(500));
            Assert.That(run.bombs, Is.EqualTo(2));
            Assert.That(run.ropes, Is.EqualTo(3));

            Object.DestroyImmediate(root);
        }
    }
}

#endif
