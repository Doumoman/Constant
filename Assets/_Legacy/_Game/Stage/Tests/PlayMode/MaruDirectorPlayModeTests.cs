#if LEGACY_DISABLED
using System.Collections;
using NUnit.Framework;
using StarNight.Core.Flow;
using StarNight.Core.Save;
using StarNight.Core.State;
using StarNight.Interaction.Input;
using StarNight.Stage.Data;
using StarNight.Stage.Flow;
using StarNight.Stage.Maru;
using StarNight.Stage.Lab;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StarNight.Stage.Tests
{
    public sealed class MaruDirectorPlayModeTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            if (GameBootstrap.Instance != null)
            {
                Object.Destroy(GameBootstrap.Instance.gameObject);
                yield return null;
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator TimerStopsForNarrativeShopAndPause()
        {
            yield return StartRun();
            StageFlowController stage = GameBootstrap.Instance.Services.GetRequired<StageFlowController>();
            MaruDirector maru = GameBootstrap.Instance.Services.GetRequired<MaruDirector>();
            GameFlowController gameFlow = GameBootstrap.Instance.Services.GetRequired<GameFlowController>();

            float start = stage.RuntimeState.elapsedTime;
            yield return new WaitForSeconds(0.06f);
            Assert.That(stage.RuntimeState.elapsedTime, Is.GreaterThan(start));

            stage.SetNarrativeTimerBlocked(true);
            float narrative = stage.RuntimeState.elapsedTime;
            yield return new WaitForSecondsRealtime(0.08f);
            Assert.That(stage.RuntimeState.elapsedTime, Is.EqualTo(narrative).Within(0.002f));
            stage.SetNarrativeTimerBlocked(false);

            maru.SetShopOpen(true);
            float shop = stage.RuntimeState.elapsedTime;
            yield return new WaitForSecondsRealtime(0.08f);
            Assert.That(stage.RuntimeState.elapsedTime, Is.EqualTo(shop).Within(0.002f));
            maru.SetShopOpen(false);

            Assert.That(gameFlow.TryPause(), Is.True);
            float paused = stage.RuntimeState.elapsedTime;
            yield return new WaitForSecondsRealtime(0.08f);
            Assert.That(stage.RuntimeState.elapsedTime, Is.EqualTo(paused).Within(0.002f));
            Assert.That(gameFlow.TryResume(), Is.True);
        }

        [UnityTest]
        public IEnumerator ChaseMovesLogicallyByAdjacencyThenSpawnsOnlyInPlayersRoom()
        {
            yield return StartRun();
            StageFlowController stage = GameBootstrap.Instance.Services.GetRequired<StageFlowController>();
            MaruDirector maru = GameBootstrap.Instance.Services.GetRequired<MaruDirector>();
            Assert.That(stage.RuntimeState.currentRoomId, Is.EqualTo("Room_A"));

            maru.ForceStartChaseForTests("Room_B");
            Assert.That(maru.LogicalRoomId, Is.EqualTo("Room_B"));
            Assert.That(maru.CurrentAgent, Is.Null);
            yield return new WaitForSeconds(MaruDirector.LogicalRoomStepSeconds + MaruDirector.RoomSpawnDelaySeconds + 0.15f);

            Assert.That(maru.LogicalRoomId, Is.EqualTo("Room_A"));
            Assert.That(stage.RoomGraph.AreAdjacent("Room_B", maru.LogicalRoomId), Is.True);
            Assert.That(maru.CurrentAgent, Is.Not.Null);
            Assert.That(maru.CurrentAgent.RoomId, Is.EqualTo(stage.RuntimeState.currentRoomId));
        }

        [UnityTest]
        public IEnumerator FirstBiteEscapesWithHealthLossAndSecondBiteEndsRun()
        {
            yield return StartRun();
            GameBootstrap.Instance.Services.GetRequired<RunRecordRepository>().PersistenceEnabled = false;
            MaruDirector maru = GameBootstrap.Instance.Services.GetRequired<MaruDirector>();
            RunManager runs = GameBootstrap.Instance.Services.GetRequired<RunManager>();
            GameFlowController gameFlow = GameBootstrap.Instance.Services.GetRequired<GameFlowController>();
            PlayerActionLock actionLock = Object.FindFirstObjectByType<PlayerActionLock>();
            int startingHealth = runs.Current.health;

            maru.ForceStartChaseForTests("Room_A");
            Assert.That(maru.CurrentAgent, Is.Not.Null);
            Assert.That(maru.ForceBiteForTests(), Is.True);
            Assert.That(maru.IsEscapeActive, Is.True);
            Assert.That(actionLock.State, Is.EqualTo(PlayerActionState.MaruBitten));
            for (int index = 0; index < MaruDirector.RequiredEscapePresses; index++)
            {
                maru.RegisterEscapePress();
            }

            Assert.That(maru.IsEscapeActive, Is.False);
            Assert.That(runs.Current.health, Is.EqualTo(startingHealth - 1));
            Assert.That(maru.CurrentAgent.IsStunned, Is.True);
            Assert.That(actionLock.State, Is.EqualTo(PlayerActionState.Free));

            maru.CurrentAgent.Stun(0f);
            Assert.That(maru.TryBitePlayer(maru.CurrentAgent), Is.True);
            yield return new WaitForSecondsRealtime(0.85f);
            Assert.That(runs.Current.phase, Is.EqualTo(RunPhase.Failed));
            Assert.That(runs.Current.failureReason, Is.EqualTo(MaruDirector.MaruFailureReason));
            Assert.That(gameFlow.State, Is.EqualTo(GameApplicationState.RunResult));
        }

        [UnityTest]
        public IEnumerator RoomTransitionNeverSpawnsMaruInFrontOfPlayer()
        {
            yield return StartRun();
            MaruDirector maru = GameBootstrap.Instance.Services.GetRequired<MaruDirector>();
            StageFlowController stage = GameBootstrap.Instance.Services.GetRequired<StageFlowController>();
            Core04TwoRoomLab lab = Object.FindFirstObjectByType<Core04TwoRoomLab>();
            int spawnCount = 0;
            maru.AgentSpawned += _ => spawnCount++;
            maru.ForceStartChaseForTests("Room_B");

            Assert.That(lab.TransitionController.TryCommit(lab.PortalAtoB), Is.True);
            yield return new WaitForSeconds(0.1f);
            Assert.That(lab.TransitionController.IsTransitioning, Is.True);
            Assert.That(spawnCount, Is.Zero);
            Assert.That(maru.CurrentAgent, Is.Null);

            float timeoutAt = Time.realtimeSinceStartup + 2f;
            while ((lab.TransitionController.IsTransitioning || maru.CurrentAgent == null) && Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }
            Assert.That(stage.RuntimeState.currentRoomId, Is.EqualTo("Room_B"));
            Assert.That(spawnCount, Is.EqualTo(1));
            Assert.That(maru.CurrentAgent.transform.position.x, Is.LessThanOrEqualTo(stage.CurrentPlayer.transform.position.x));
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
