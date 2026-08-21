#if LEGACY_DISABLED
using System.Collections;
using NUnit.Framework;
using StarNight.Core.Flow;
using StarNight.Core.State;
using StarNight.Interaction.Input;
using StarNight.Stage.Flow;
using StarNight.Stage.Lab;
using StarNight.UI.HUD;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace StarNight.UI.Tests
{
    public sealed class HUDPlayModeTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Scene cleanup = SceneManager.GetSceneByName("Core06Cleanup");
            if (!cleanup.IsValid())
            {
                cleanup = SceneManager.CreateScene("Core06Cleanup");
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
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator RunShellHudReflectsAuthoritativeStateWithoutMutatingIt()
        {
            yield return StartRun();
            HUDController hud = Object.FindFirstObjectByType<HUDController>(FindObjectsInactive.Include);
            Assert.That(hud, Is.Not.Null);
            yield return null;

            Assert.That(hud.Scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(hud.Scaler.referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));
            Assert.That(hud.Scaler.matchWidthOrHeight, Is.EqualTo(0.5f).Within(0.001f));

            RunState run = GameBootstrap.Instance.Services.GetRequired<RunManager>().Current;
            run.health = 3;
            run.moneyWon = 1260;
            run.ropes = 0;
            run.bombs = 3;
            run.handToolId = "곡괭이";
            hud.ModelSource.RefreshForTests();
            yield return null;

            Assert.That(hud.HealthDisplay, Is.EqualTo("♥ ♥ ♥ ♡"));
            Assert.That(hud.MoneyDisplay, Is.EqualTo("1,260원"));
            Assert.That(hud.ConsumableDisplay, Does.Contain("로프 ╱ 0"));
            Assert.That(hud.ConsumableDisplay, Does.Contain("폭탄 3"));

            int health = run.health;
            int money = run.moneyWon;
            int ropes = run.ropes;
            int bombs = run.bombs;
            string hand = run.handToolId;
            yield return null;
            yield return null;

            Assert.That(run.health, Is.EqualTo(health));
            Assert.That(run.moneyWon, Is.EqualTo(money));
            Assert.That(run.ropes, Is.EqualTo(ropes));
            Assert.That(run.bombs, Is.EqualTo(bombs));
            Assert.That(run.handToolId, Is.EqualTo(hand));
        }

        [UnityTest]
        public IEnumerator RoomMapBlocksPlayerActionsWithoutPausingAndTracksVisitedRooms()
        {
            yield return StartRun();
            HUDController hud = Object.FindFirstObjectByType<HUDController>(FindObjectsInactive.Include);
            PlayerActionRouter router = Object.FindFirstObjectByType<PlayerActionRouter>();
            Core04TwoRoomLab lab = Object.FindFirstObjectByType<Core04TwoRoomLab>();
            StageFlowController flow = GameBootstrap.Instance.Services.GetRequired<StageFlowController>();
            Assert.That(hud, Is.Not.Null);
            Assert.That(router, Is.Not.Null);
            Assert.That(lab, Is.Not.Null);

            hud.InputCoordinator.SetMapOpenForTests(true);
            yield return null;
            Assert.That(hud.IsMapVisible, Is.True);
            Assert.That(hud.MapNodeCount, Is.EqualTo(1));
            Assert.That(router.IsMapOverlayOpen, Is.True);
            Assert.That(router.GameplayActionsAllowed, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            Assert.That(lab.TransitionController.CommitImmediate(lab.PortalAtoB), Is.True);
            hud.ModelSource.RefreshForTests();
            yield return null;
            Assert.That(flow.RuntimeState.visitedRoomIds.Count, Is.EqualTo(2));
            Assert.That(hud.MapNodeCount, Is.EqualTo(2));

            hud.ModelSource.ForceInputDeviceForTests(InputDisplayDevice.Gamepad);
            yield return null;
            Assert.That(hud.ModelSource.Model.MapGlyph, Is.EqualTo("PAD VIEW"));
            Assert.That(hud.ModelSource.Model.PrimaryGlyph, Does.StartWith("PAD"));

            hud.InputCoordinator.SetMapOpenForTests(false);
            yield return null;
            Assert.That(hud.IsMapVisible, Is.False);
            Assert.That(router.IsMapOverlayOpen, Is.False);
            Assert.That(router.GameplayActionsAllowed, Is.True);
        }

        private static IEnumerator StartRun()
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

            GameFlowController flow = bootstrap.Services.GetRequired<GameFlowController>();
            Assert.That(flow.StartNewRun(), Is.True);
            float timeoutAt = Time.realtimeSinceStartup + 10f;
            while ((flow.State != GameApplicationState.Playing || flow.IsTransitioning) && Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }

            Assert.That(flow.State, Is.EqualTo(GameApplicationState.Playing));
            Assert.That(flow.IsTransitioning, Is.False);
            yield return null;
        }

        private static IEnumerator UnloadIfLoaded(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (scene.IsValid() && scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }
    }
}

#endif
