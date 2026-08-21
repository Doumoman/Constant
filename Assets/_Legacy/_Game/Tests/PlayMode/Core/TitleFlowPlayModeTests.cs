#if LEGACY_DISABLED
using System.Collections;
using System.Linq;
using NUnit.Framework;
using StarNight.Core.Flow;
using StarNight.Core.Save;
using StarNight.Core.State;
using StarNight.UI.Menus;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StarNight.Core.Tests
{
    public sealed class TitleFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator TitleBuildsExactlySixMenuItemsWithoutContinue()
        {
            yield return SceneManager.LoadSceneAsync(GameBootstrap.TitleSceneName, LoadSceneMode.Single);
            yield return null;

            TitleMenuController title = Object.FindAnyObjectByType<TitleMenuController>(FindObjectsInactive.Include);

            Assert.That(title, Is.Not.Null);
            Assert.That(title.MenuItemCount, Is.EqualTo(6));
            Assert.That(Enumerable.Range(0, title.MenuItemCount).Select(title.GetMenuLabel), Is.EqualTo(new[]
            {
                "새 여행",
                "설정",
                "조작법",
                "기록",
                "크레딧",
                "게임 종료",
            }));
            Assert.That(Enumerable.Range(0, title.MenuItemCount).Select(title.GetMenuLabel), Has.None.EqualTo("이어하기"));
        }

        [UnityTest]
        public IEnumerator NewJourneyLoadsRunShellAndPrologueThenRestartResetsRunOnly()
        {
            yield return SceneManager.LoadSceneAsync(GameBootstrap.TitleSceneName, LoadSceneMode.Single);
            yield return null;

            TitleMenuController title = Object.FindAnyObjectByType<TitleMenuController>(FindObjectsInactive.Include);
            Assert.That(title, Is.Not.Null);

            SettingsRepository settings = GameBootstrap.Instance.Services.GetRequired<SettingsRepository>();
            RunManager runManager = GameBootstrap.Instance.Services.GetRequired<RunManager>();
            GameFlowController flow = GameBootstrap.Instance.Services.GetRequired<GameFlowController>();

            title.InvokeMenuItem(0);
            yield return WaitUntilPlaying(flow);

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(GameFlowController.RunShellSceneName));
            Assert.That(SceneManager.GetSceneByName(GameFlowController.FirstStageSceneName).isLoaded, Is.True);
            Assert.That(runManager.HasActiveRun, Is.True);
            Assert.That(runManager.Current.currentStageId, Is.EqualTo("0-1"));

            runManager.Current.flags.Add("must-not-survive-restart");
            RunState firstRun = runManager.Current;

            Assert.That(flow.RestartRun(), Is.True);
            yield return WaitUntilPlaying(flow);

            Assert.That(runManager.Current, Is.Not.SameAs(firstRun));
            Assert.That(runManager.Current.flags, Is.Empty);
            Assert.That(GameBootstrap.Instance.Services.GetRequired<SettingsRepository>(), Is.SameAs(settings));
        }

        [UnityTest]
        public IEnumerator RecordMenuReportsSavedEndingStageAndBestTime()
        {
            yield return SceneManager.LoadSceneAsync(GameBootstrap.TitleSceneName, LoadSceneMode.Single);
            yield return null;

            TitleMenuController title = Object.FindAnyObjectByType<TitleMenuController>(FindObjectsInactive.Include);
            RunRecordRepository repository = GameBootstrap.Instance.Services.GetRequired<RunRecordRepository>();
            repository.PersistenceEnabled = false;
            repository.Save(new RunRecordData
            {
                viewedEndingIds = new System.Collections.Generic.List<string> { "memory_bell" },
                highestReachedStage = "5-3",
                bestClearedRunTime = 125f,
                completedRunCount = 1,
            });

            title.InvokeMenuItem(3);

            Assert.That(title.CurrentStatus, Does.Contain("본 엔딩 1"));
            Assert.That(title.CurrentStatus, Does.Contain("최고 도달 5-3"));
            Assert.That(title.CurrentStatus, Does.Contain("02:05"));
            repository.Save(RunRecordData.CreateDefault());
            repository.PersistenceEnabled = true;
        }

        private static IEnumerator WaitUntilPlaying(GameFlowController flow)
        {
            float timeoutAt = Time.realtimeSinceStartup + 8f;
            do
            {
                yield return null;
            }
            while ((flow.State != GameApplicationState.Playing || flow.IsTransitioning)
                && Time.realtimeSinceStartup < timeoutAt);

            Assert.That(flow.State, Is.EqualTo(GameApplicationState.Playing));
            Assert.That(flow.IsTransitioning, Is.False);
        }
    }
}

#endif
