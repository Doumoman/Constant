#if LEGACY_DISABLED
using System.Collections;
using NUnit.Framework;
using StarNight.Core.Flow;
using StarNight.Core.State;
using StarNight.Interaction.Input;
using StarNight.Stage.Flow;
using StarNight.Stage.Maru;
using StarNight.UI.HUD;
using StarNight.UI.Menus;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StarNight.UI.Tests
{
    public sealed class PauseSettingsPlayModeTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            GameBootstrap bootstrap = GameBootstrap.Instance;
            if (bootstrap != null)
            {
                Object.Destroy(bootstrap.gameObject);
                yield return null;
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator PauseFreezesStageTimerAndRestoresDialogueInputContext()
        {
            yield return StartRun();
            PauseMenuController pause = Object.FindFirstObjectByType<PauseMenuController>(FindObjectsInactive.Include);
            GameplayInputReader input = Object.FindFirstObjectByType<GameplayInputReader>();
            StageFlowController stage = GameBootstrap.Instance.Services.GetRequired<StageFlowController>();
            GameFlowController flow = GameBootstrap.Instance.Services.GetRequired<GameFlowController>();
            Assert.That(pause, Is.Not.Null);
            Assert.That(input, Is.Not.Null);

            input.SetContext(PlayerInputContext.Dialogue);
            float elapsed = stage.RuntimeState.elapsedTime;
            Assert.That(pause.Open(), Is.True);
            yield return new WaitForSecondsRealtime(0.12f);

            Assert.That(flow.State, Is.EqualTo(GameApplicationState.Paused));
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(stage.RuntimeState.elapsedTime, Is.EqualTo(elapsed).Within(0.001f));
            Assert.That(input.Context, Is.EqualTo(PlayerInputContext.Menu));
            Assert.That(pause.Resume(), Is.True);
            Assert.That(flow.State, Is.EqualTo(GameApplicationState.Playing));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(input.Context, Is.EqualTo(PlayerInputContext.Dialogue));
        }

        [UnityTest]
        public IEnumerator TitleSettingsEntryOpensTheSharedFiveCategoryController()
        {
            yield return SceneManager.LoadSceneAsync(GameBootstrap.TitleSceneName, LoadSceneMode.Single);
            yield return null;
            if (GameBootstrap.Instance == null)
            {
                new GameObject(GameBootstrap.ServiceRootName).AddComponent<GameBootstrap>();
                yield return null;
            }
            TitleMenuController title = Object.FindFirstObjectByType<TitleMenuController>();
            Assert.That(title, Is.Not.Null);

            title.InvokeMenuItem(1);
            yield return null;
            SettingsController settingsController = Object.FindFirstObjectByType<SettingsController>(FindObjectsInactive.Include);
            Assert.That(settingsController, Is.Not.Null);
            Assert.That(settingsController.IsOpen, Is.True);
            settingsController.SelectCategory(4);
            Assert.That(settingsController.CategoryIndex, Is.EqualTo(4));
            settingsController.Close(false);
        }

        [UnityTest]
        public IEnumerator PauseMenuExposesDocumentedSixActionsAndAllSettingsCategories()
        {
            yield return StartRun();
            PauseMenuController pause = Object.FindFirstObjectByType<PauseMenuController>(FindObjectsInactive.Include);
            Assert.That(pause.MenuItemCount, Is.EqualTo(6));
            Assert.That(pause.GetMenuLabel(0), Is.EqualTo("계속하기"));
            Assert.That(pause.GetMenuLabel(3), Is.EqualTo("현재 스테이지 다시 시작"));
            Assert.That(pause.GetMenuLabel(4), Is.EqualTo("현재 여행 포기"));

            Assert.That(pause.Open(), Is.True);
            pause.InvokeMenuItem(2);
            yield return null;
            Assert.That(pause.Settings.IsOpen, Is.True);
            for (int index = 0; index < 5; index++)
            {
                pause.Settings.SelectCategory(index);
                Assert.That(pause.Settings.CategoryIndex, Is.EqualTo(index));
            }
            pause.Settings.Close(false);
            Assert.That(pause.Resume(), Is.True);
        }

        [UnityTest]
        public IEnumerator RestartStageRestoresEntryResourcesAndRecordsRestart()
        {
            yield return StartRun();
            PauseMenuController pause = Object.FindFirstObjectByType<PauseMenuController>(FindObjectsInactive.Include);
            GameFlowController flow = GameBootstrap.Instance.Services.GetRequired<GameFlowController>();
            RunState run = GameBootstrap.Instance.Services.GetRequired<RunManager>().Current;
            int entryHealth = run.health;
            int entryMoney = run.moneyWon;
            run.health = 1;
            run.moneyWon = 730;
            run.items.Add("temporary-stage-item");

            Assert.That(pause.Open(), Is.True);
            pause.InvokeMenuItem(3);
            float timeoutAt = Time.realtimeSinceStartup + 8f;
            while ((flow.State != GameApplicationState.Playing || flow.IsTransitioning) && Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }

            Assert.That(flow.State, Is.EqualTo(GameApplicationState.Playing));
            Assert.That(run.health, Is.EqualTo(entryHealth));
            Assert.That(run.moneyWon, Is.EqualTo(entryMoney));
            Assert.That(run.items, Has.None.EqualTo("temporary-stage-item"));
            Assert.That(run.stageRestartCount, Is.EqualTo(1));
            Assert.That(run.actionRecords[^1].actionId, Does.StartWith("stage_restart:"));
        }

        [UnityTest]
        public IEnumerator MaruChaseAndBiteAreVisibleInTheSharedHud()
        {
            yield return StartRun();
            MaruDirector maru = GameBootstrap.Instance.Services.GetRequired<MaruDirector>();
            HUDController hud = Object.FindFirstObjectByType<HUDController>(FindObjectsInactive.Include);
            Assert.That(hud, Is.Not.Null);

            maru.ForceStartChaseForTests("Room_A");
            yield return null;
            Assert.That(hud.IsMaruWarningVisible, Is.True);
            Assert.That(maru.ForceBiteForTests(), Is.True);
            yield return null;
            Assert.That(hud.IsMaruEscapeVisible, Is.True);

            for (int index = 0; index < MaruDirector.RequiredEscapePresses; index++)
            {
                maru.RegisterEscapePress();
            }
            yield return null;
            Assert.That(hud.IsMaruEscapeVisible, Is.False);
        }

        private static IEnumerator StartRun()
        {
            GameBootstrap bootstrap = GameBootstrap.Instance;
            if (bootstrap == null)
            {
                bootstrap = new GameObject(GameBootstrap.ServiceRootName).AddComponent<GameBootstrap>();
            }
            GameFlowController flow = bootstrap.Services.GetRequired<GameFlowController>();
            Assert.That(flow.StartNewRun(), Is.True);
            float timeoutAt = Time.realtimeSinceStartup + 10f;
            while ((flow.State != GameApplicationState.Playing || flow.IsTransitioning) && Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(GameFlowController.RunShellSceneName));
            Assert.That(flow.State, Is.EqualTo(GameApplicationState.Playing));
            yield return null;
        }
    }
}

#endif
