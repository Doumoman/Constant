#if LEGACY_DISABLED
using System.Collections;
using NUnit.Framework;
using StarNight.Core.Flow;
using StarNight.Core.State;
using StarNight.Interaction.Input;
using StarNight.Player.Motor;
using StarNight.Stage.Exit;
using StarNight.Stage.Flow;
using StarNight.Stage.Lab;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StarNight.Stage.Tests
{
    public sealed class StageFlowPlayModeTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Scene cleanup = SceneManager.GetSceneByName("Core05Cleanup");
            if (!cleanup.IsValid())
            {
                cleanup = SceneManager.CreateScene("Core05Cleanup");
            }
            SceneManager.SetActiveScene(cleanup);

            GameBootstrap bootstrap = GameBootstrap.Instance;
            if (bootstrap != null)
            {
                Object.Destroy(bootstrap.gameObject);
                yield return null;
            }

            yield return UnloadIfLoaded("11_Moon_1_1");
            yield return UnloadIfLoaded("10_Prologue_0_1");
            yield return UnloadIfLoaded("02_RunShell");
            DestroyNamed("Core05HoldPlayer");
            DestroyNamed("Core05ExitDoor");
            yield return null;
        }

        [UnityTest]
        public IEnumerator DepartureDoorCancelsOnReleaseAndCompletesAtHalfSecond()
        {
            GameObject playerObject = new GameObject("Core05HoldPlayer");
            PlayerMotor2D player = playerObject.AddComponent<PlayerMotor2D>();
            playerObject.AddComponent<PlayerActionLock>();
            StagePlayerActionExecutor executor = playerObject.AddComponent<StagePlayerActionExecutor>();
            PlayerActionRouter router = playerObject.AddComponent<PlayerActionRouter>();

            GameObject doorObject = new GameObject("Core05ExitDoor");
            doorObject.transform.position = player.transform.position;
            StageExitDoor door = doorObject.AddComponent<StageExitDoor>();
            door.Configure(player, null, null, null);
            executor.Configure(player, door);
            router.ConfigureForTests(null, playerObject.GetComponent<PlayerActionLock>(), player, executor);

            Assert.That(router.RoutePrimaryAction(false), Is.EqualTo(PlayerActionCommand.WorldInteraction));
            Assert.That(door.AdvanceHold(0.25f, true), Is.False);
            Assert.That(door.HoldProgress, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(door.AdvanceHold(0.01f, false), Is.False);
            Assert.That(door.IsHolding, Is.False);

            Assert.That(router.RoutePrimaryAction(false), Is.EqualTo(PlayerActionCommand.WorldInteraction));
            Assert.That(door.AdvanceHold(0.49f, true), Is.False);
            Assert.That(door.AdvanceHold(0.01f, true), Is.True);
            Assert.That(door.IsHolding, Is.False);
            Assert.That(door.HoldProgress, Is.EqualTo(1f).Within(0.001f));

            Object.Destroy(playerObject);
            Object.Destroy(doorObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PrologueExitTransitionsOnceToExactMoonEntryPoint()
        {
            AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            foreach (AudioListener listener in listeners)
            {
                listener.enabled = false;
            }

            GameBootstrap bootstrap = GameBootstrap.Instance;
            if (bootstrap == null)
            {
                bootstrap = new GameObject(GameBootstrap.ServiceRootName).AddComponent<GameBootstrap>();
            }
            GameFlowController gameFlow = bootstrap.Services.GetRequired<GameFlowController>();
            Assert.That(gameFlow.StartNewRun(), Is.True);

            float deadline = Time.realtimeSinceStartup + 10f;
            while ((gameFlow.IsTransitioning || gameFlow.State != GameApplicationState.Playing) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(gameFlow.State, Is.EqualTo(GameApplicationState.Playing));
            StageFlowController flow = bootstrap.Services.GetRequired<StageFlowController>();
            Assert.That(flow.CurrentDefinition.stageId, Is.EqualTo("0-1"));
            Assert.That(flow.RequestExit(), Is.True);
            Assert.That(flow.RequestExit(), Is.False, "A duplicate exit request must not schedule a second scene load.");

            deadline = Time.realtimeSinceStartup + 10f;
            while (flow.IsStageTransitioning && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(flow.IsStageTransitioning, Is.False);
            Assert.That(flow.CurrentDefinition.stageId, Is.EqualTo("1-1"));
            Assert.That(SceneManager.GetSceneByName("10_Prologue_0_1").isLoaded, Is.False);
            Assert.That(SceneManager.GetSceneByName("11_Moon_1_1").isLoaded, Is.True);
            Core04TwoRoomLab lab = Object.FindFirstObjectByType<Core04TwoRoomLab>();
            PlayerMotor2D player = Object.FindFirstObjectByType<PlayerMotor2D>();
            Assert.That(Vector2.Distance(player.Body.position, lab.RoomA.SpawnPoint.position), Is.LessThan(0.02f));
            Assert.That(bootstrap.Services.GetRequired<RunManager>().Current.currentStageId, Is.EqualTo("1-1"));
            Assert.That(gameFlow.State, Is.EqualTo(GameApplicationState.Playing));

            foreach (AudioListener listener in listeners)
            {
                if (listener != null)
                {
                    listener.enabled = true;
                }
            }
        }

        private static IEnumerator UnloadIfLoaded(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (scene.IsValid() && scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        private static void DestroyNamed(string objectName)
        {
            GameObject[] objects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            for (int index = 0; index < objects.Length; index++)
            {
                if (objects[index] != null && objects[index].name == objectName)
                {
                    Object.Destroy(objects[index]);
                }
            }
        }
    }
}

#endif
