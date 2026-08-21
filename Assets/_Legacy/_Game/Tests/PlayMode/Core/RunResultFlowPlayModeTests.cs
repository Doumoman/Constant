#if LEGACY_DISABLED
using System.Collections;
using NUnit.Framework;
using StarNight.Core.Flow;
using StarNight.Core.Save;
using StarNight.Core.State;
using StarNight.UI.Results;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StarNight.Core.Tests
{
    public sealed class RunResultFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator ResultScreenShowsSnapshotAndRestartLeavesNoPreviousStageObjects()
        {
            yield return SceneManager.LoadSceneAsync(GameBootstrap.TitleSceneName, LoadSceneMode.Single);
            yield return null;

            GameFlowController flow = GameBootstrap.Instance.Services.GetRequired<GameFlowController>();
            RunManager runs = GameBootstrap.Instance.Services.GetRequired<RunManager>();
            RunRecordRepository records = GameBootstrap.Instance.Services.GetRequired<RunRecordRepository>();
            records.PersistenceEnabled = false;

            Assert.That(flow.StartNewRun(), Is.True);
            yield return WaitUntil(() => flow.State == GameApplicationState.Playing && !flow.IsTransitioning, 10f);

            Scene previousStage = SceneManager.GetSceneByName(GameFlowController.FirstStageSceneName);
            Assert.That(previousStage.isLoaded, Is.True);
            GameObject previousStageRoot = previousStage.GetRootGameObjects()[0];
            RunState previousRun = runs.Current;
            previousRun.currentStageId = "3-2";
            previousRun.runTime = 83f;
            previousRun.moneyWon = 900;
            previousRun.peakMoney = 1400;
            Assert.That(runs.FailRun("maru_bite"), Is.True);
            Assert.That(flow.EnterRunResult(), Is.True);
            yield return null;

            RunResultController result = Object.FindFirstObjectByType<RunResultController>();
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsVisible, Is.True);
            Assert.That(result.ButtonCount, Is.EqualTo(2));
            Assert.That(result.FailureDisplay, Does.Contain("마루"));
            Assert.That(result.ReachedStageDisplay, Does.Contain("3-2"));
            Assert.That(result.RunTimeDisplay, Does.Contain("01:23"));
            Assert.That(Time.timeScale, Is.EqualTo(0f));

            result.InvokeButton(0);
            yield return WaitUntil(() => flow.State == GameApplicationState.Playing && !flow.IsTransitioning, 12f);

            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(runs.Current, Is.Not.SameAs(previousRun));
            Assert.That(runs.Current.flags, Is.Empty);
            Assert.That(previousStageRoot == null, Is.True, "The previous additive stage hierarchy must be destroyed before the new run starts.");
            Assert.That(SceneManager.GetSceneByName(GameFlowController.FirstStageSceneName).isLoaded, Is.True);
            records.PersistenceEnabled = true;
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float timeout)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(condition(), Is.True, "Timed out waiting for the run-result flow.");
        }
    }
}

#endif
