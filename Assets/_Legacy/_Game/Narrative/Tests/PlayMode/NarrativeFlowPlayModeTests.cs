#if LEGACY_DISABLED
using System.Collections;
using NUnit.Framework;
using StarNight.Core.Flow;
using StarNight.Interaction.Input;
using StarNight.Stage.Flow;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Yarn.Unity;

namespace StarNight.Narrative.Tests
{
    public sealed class NarrativeFlowPlayModeTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Scene cleanup = SceneManager.GetSceneByName("Core07Cleanup");
            if (!cleanup.IsValid()) cleanup = SceneManager.CreateScene("Core07Cleanup");
            SceneManager.SetActiveScene(cleanup);
            if (GameBootstrap.Instance != null)
            {
                Object.Destroy(GameBootstrap.Instance.gameObject);
                yield return null;
            }
            yield return Unload("11_Moon_1_1");
            yield return Unload("10_Prologue_0_1");
            yield return Unload("02_RunShell");
            yield return null;
        }

        [UnityTest]
        public IEnumerator BlockingAndBubbleModesRespectGameplayContractsAndRunnerSurvivesStageChange()
        {
            DisableAudioListeners();
            GameBootstrap bootstrap = GameBootstrap.Instance;
            if (bootstrap == null)
            {
                bootstrap = new GameObject(GameBootstrap.ServiceRootName).AddComponent<GameBootstrap>();
            }
            GameFlowController gameFlow = bootstrap.Services.GetRequired<GameFlowController>();
            Assert.That(gameFlow.StartNewRun(), Is.True);
            yield return WaitUntil(() => gameFlow.State == GameApplicationState.Playing && !gameFlow.IsTransitioning, 12f);

            NarrativeSystemController system = Object.FindFirstObjectByType<NarrativeSystemController>();
            Assert.That(system, Is.Not.Null);
            Assert.That(Object.FindObjectsByType<DialogueRunner>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            StageFlowController stageFlow = bootstrap.Services.GetRequired<StageFlowController>();
            PlayerActionLock actionLock = Object.FindFirstObjectByType<PlayerActionLock>();
            GameplayInputReader input = Object.FindFirstObjectByType<GameplayInputReader>();

            if (!system.Service.HasActiveRequest)
            {
                Assert.That(system.Service.TryRunNode("STG.MOON_1_1.Intro", NarrativeMode.Conversation, true, true), Is.True);
                yield return null;
            }
            Assert.That(actionLock.State, Is.EqualTo(PlayerActionState.DialogueLocked));
            Assert.That(input.Context, Is.EqualTo(PlayerInputContext.Dialogue));
            Assert.That(stageFlow.IsNarrativeTimerBlocked, Is.True);
            float blockedTime = stageFlow.RuntimeState.elapsedTime;
            yield return new WaitForSecondsRealtime(0.12f);
            Assert.That(stageFlow.RuntimeState.elapsedTime, Is.EqualTo(blockedTime).Within(0.01f));

            system.Service.StopDialogue();
            yield return WaitUntil(() => !system.Runner.IsDialogueRunning && !system.Service.HasActiveRequest, 3f);
            Assert.That(stageFlow.IsNarrativeTimerBlocked, Is.False);
            Assert.That(input.Context, Is.EqualTo(PlayerInputContext.Gameplay));

            Assert.That(system.Service.TryRunNode("NPC.Moon.Dabok.FirstMeet", NarrativeMode.Bubble, false, false), Is.True);
            yield return null;
            Assert.That(actionLock.State, Is.Not.EqualTo(PlayerActionState.DialogueLocked));
            Assert.That(input.Context, Is.EqualTo(PlayerInputContext.Gameplay));
            float bubbleStartTime = stageFlow.RuntimeState.elapsedTime;
            yield return new WaitForSecondsRealtime(0.12f);
            Assert.That(stageFlow.RuntimeState.elapsedTime, Is.GreaterThan(bubbleStartTime));
            Assert.That(stageFlow.RequestExit(), Is.True);
            yield return null;
            Assert.That(system.View.BubbleGroup.alpha, Is.EqualTo(0f), "Field bubbles must dismiss before the room/stage transition completes.");
            yield return WaitUntil(() => !stageFlow.IsStageTransitioning, 12f);
            Assert.That(stageFlow.CurrentDefinition.stageId, Is.EqualTo("1-1"));
            Assert.That(Object.FindObjectsByType<DialogueRunner>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DirectXFallbackFinishesTypewriterThenAdvancesToTheNextLine()
        {
            DisableAudioListeners();
            GameBootstrap bootstrap = GameBootstrap.Instance;
            if (bootstrap == null)
            {
                bootstrap = new GameObject(GameBootstrap.ServiceRootName).AddComponent<GameBootstrap>();
            }
            GameFlowController gameFlow = bootstrap.Services.GetRequired<GameFlowController>();
            Assert.That(gameFlow.StartNewRun(), Is.True);
            yield return WaitUntil(() => gameFlow.State == GameApplicationState.Playing && !gameFlow.IsTransitioning, 12f);

            NarrativeSystemController system = Object.FindFirstObjectByType<NarrativeSystemController>();
            Assert.That(system, Is.Not.Null);
            if (system.Runner.IsDialogueRunning)
            {
                system.Service.StopDialogue();
                yield return WaitUntil(() => !system.Runner.IsDialogueRunning, 3f);
            }

            Assert.That(system.Service.TryRunNode("STG.MOON_1_1.Intro", NarrativeMode.Conversation, true, true), Is.True);
            yield return WaitUntil(() => system.Runner.IsDialogueRunning && system.UIState.IsTypewriting, 3f);
            string firstLine = system.View.ConversationBody.text;

            Assert.That(system.InputRouter.ProcessAdvanceInput(false, true), Is.True, "The direct X fallback must reach the dialogue router.");
            yield return WaitUntil(() => !system.UIState.IsTypewriting, 3f);
            Assert.That(system.View.ConversationBody.text, Is.EqualTo(firstLine));
            Assert.That(system.View.ConversationBody.maxVisibleCharacters, Is.EqualTo(int.MaxValue));

            Assert.That(system.InputRouter.ProcessAdvanceInput(false, true), Is.True);
            yield return WaitUntil(() => system.View.ConversationBody.text != firstLine, 3f);
            Assert.That(system.View.ConversationBody.text, Is.Not.EqualTo(firstLine));
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float timeout)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(condition(), Is.True, "Timed out waiting for the narrative flow state.");
        }

        private static IEnumerator Unload(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (scene.IsValid() && scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        private static void DisableAudioListeners()
        {
            AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            foreach (AudioListener listener in listeners) listener.enabled = false;
        }
    }
}

#endif
