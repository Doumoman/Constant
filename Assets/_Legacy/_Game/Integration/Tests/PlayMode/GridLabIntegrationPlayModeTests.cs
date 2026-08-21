#if LEGACY_DISABLED
using System.Collections;
using NUnit.Framework;
using StarNight.Core.Flow;
using StarNight.Interaction.Input;
using StarNight.Narrative;
using StarNight.Player.Motor;
using StarNight.Stage.Data;
using StarNight.Stage.Exit;
using StarNight.Stage.Flow;
using StarNight.Stage.Lab;
using StarNight.Stage.Maru;
using StarNight.UI.HUD;
using StarNight.UI.Menus;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StarNight.Integration.Tests
{
    public sealed class GridLabIntegrationPlayModeTests
    {
        [UnityTest]
        public IEnumerator FourRoomsExerciseEveryCommonSystemAndAcceleratedThirtyMinuteSoak()
        {
            yield return SceneManager.LoadSceneAsync("99_GridLab", LoadSceneMode.Single);

            Core12GridLab gridLab = null;
            for (int attempt = 0; attempt < 120; attempt++)
            {
                gridLab = Object.FindFirstObjectByType<Core12GridLab>();
                if (gridLab != null && gridLab.IsReady && GameBootstrap.IsReady &&
                    GameBootstrap.Instance.Services.TryGet(out StageFlowController readyFlow) &&
                    readyFlow.RuntimeState != null)
                {
                    break;
                }
                yield return null;
            }

            Assert.That(gridLab, Is.Not.Null);
            Assert.That(gridLab.IsReady, Is.True);
            Core04TwoRoomLab lab = Object.FindFirstObjectByType<Core04TwoRoomLab>();
            PlayerMotor2D player = Object.FindFirstObjectByType<PlayerMotor2D>();
            HUDController hud = Object.FindFirstObjectByType<HUDController>();
            NarrativeSystemController narrative = Object.FindFirstObjectByType<NarrativeSystemController>();
            PauseMenuController pause = Object.FindFirstObjectByType<PauseMenuController>();
            GameplayInputReader input = player.GetComponent<GameplayInputReader>();
            GameFlowController applicationFlow = GameBootstrap.Instance.Services.GetRequired<GameFlowController>();
            StageFlowController stageFlow = GameBootstrap.Instance.Services.GetRequired<StageFlowController>();
            MaruDirector maru = GameBootstrap.Instance.Services.GetRequired<MaruDirector>();

            Assert.That(lab.Rooms, Has.Count.EqualTo(4));
            Assert.That(stageFlow.RoomGraph.RoomCount, Is.EqualTo(4));
            Assert.That(stageFlow.CurrentExit, Is.Not.Null);
            Assert.That(stageFlow.CurrentExit.GetComponentInParent<StarNight.Stage.Rooms.RoomRuntime>()?.RoomId, Is.EqualTo("Room_B"));
            Assert.That(hud.ModelSource, Is.Not.Null);
            Assert.That(narrative.Service, Is.Not.Null);
            Assert.That(pause.MenuItemCount, Is.GreaterThan(0));
            Assert.That(applicationFlow.State, Is.EqualTo(GameApplicationState.Playing));

            Assert.That(Time.timeScale, Is.EqualTo(1f));
            player.SnapTo(lab.RoomA.SpawnPoint.position);
            for (int attempt = 0; attempt < 12 && !player.IsGrounded; attempt++)
            {
                yield return new WaitForFixedUpdate();
            }
            Assert.That(player.IsGrounded, Is.True);
            float jumpStartY = player.Body.position.y;
            float jumpMaximumY = jumpStartY;
            player.SetJumpHeld(true);
            player.QueueJump();
            for (int index = 0; index < 8; index++)
            {
                yield return new WaitForFixedUpdate();
                jumpMaximumY = Mathf.Max(jumpMaximumY, player.Body.position.y);
            }
            player.ReleaseJump();
            Assert.That(jumpMaximumY, Is.GreaterThan(jumpStartY + 0.1f));

            yield return gridLab.RunAcceleratedTransitionSoak();
            Assert.That(gridLab.LastAcceleratedTransitionCount, Is.EqualTo(Core12GridLab.AcceleratedSoakSeconds));
            Assert.That(gridLab.LastAcceleratedSoakStable, Is.True,
                $"Managed growth was {gridLab.LastAcceleratedManagedGrowthBytes} bytes.");

            player.SnapTo(gridLab.InteractionStation.transform.position);
            Assert.That(gridLab.InteractionStation.IsPlayerInRange, Is.True);
            Assert.That(gridLab.InteractionStation.ActivateForTests(), Is.True);
            Assert.That(gridLab.InteractionStation.ActivationCount, Is.EqualTo(1));

            player.SnapTo(gridLab.NarrativeStation.transform.position);
            Assert.That(gridLab.NarrativeStation.ActivateForTests(), Is.True);
            yield return null;
            Assert.That(narrative.Service.HasActiveRequest, Is.True);
            Assert.That(input.Context, Is.EqualTo(PlayerInputContext.Dialogue));
            Assert.That(narrative.InputRouter.ProcessAdvanceInput(false, true), Is.True);
            narrative.Service.StopDialogue();
            for (int attempt = 0; attempt < 30 && narrative.Service.HasActiveRequest; attempt++)
            {
                yield return null;
            }
            Assert.That(input.Context, Is.EqualTo(PlayerInputContext.Gameplay));

            Assert.That(pause.Open(), Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(input.Context, Is.EqualTo(PlayerInputContext.Menu));
            Assert.That(pause.Resume(), Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            maru.AdvanceClockForTests(1800f);
            Assert.That(stageFlow.RuntimeState.bellPhase, Is.EqualTo(BellPhase.Maru));
            Assert.That(maru.CurrentBellPhase, Is.EqualTo(BellPhase.Maru));
        }
    }
}

#endif
