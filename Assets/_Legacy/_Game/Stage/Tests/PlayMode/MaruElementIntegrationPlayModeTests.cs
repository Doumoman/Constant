#if LEGACY_DISABLED
using System.Collections;
using NUnit.Framework;
using StarNight.Core.Flow;
using StarNight.Core.State;
using StarNight.Map;
using StarNight.Stage.Data;
using StarNight.Stage.Flow;
using StarNight.Stage.Maru;
using StarNight.Stage.Rooms;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StarNight.Stage.Tests
{
    public sealed class MaruElementIntegrationPlayModeTests
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
        public IEnumerator SixMapElementEventsMutateMaruDirectorRunAndRoomState()
        {
            yield return StartRun();
            var bootstrap = GameBootstrap.Instance;
            var runs = bootstrap.Services.GetRequired<RunManager>();
            var stage = bootstrap.Services.GetRequired<StageFlowController>();
            var maru = bootstrap.Services.GetRequired<MaruDirector>();
            Assert.That(MaruElementEventHub.HasSink, Is.True);

            runs.Current.moneyWon = 0;
            var statue = maru.ApplyMaruElementEvent(new MaruElementEventRequest
            {
                EventType = MaruElementEventType.StatueBroken,
                ElementId = "MARU_ReturnStatue",
                SourceRuntimeId = "statue_test",
                RewardMoney = 500,
            });
            Assert.That(statue.Accepted, Is.True);
            Assert.That(statue.RewardGranted, Is.True);
            Assert.That(statue.PenaltyApplied, Is.True);
            Assert.That(runs.Current.moneyWon, Is.EqualTo(500));
            Assert.That(maru.CurrentBellPhase, Is.EqualTo(BellPhase.Second));

            var collar = maru.ApplyMaruElementEvent(new MaruElementEventRequest
            {
                EventType = MaruElementEventType.CollarCarryChanged,
                ElementId = "MARU_CollarFragment",
                SourceRuntimeId = "collar_test",
                Active = true,
                RateMultiplier = 1.15f,
            });
            Assert.That(collar.Accepted, Is.True);
            Assert.That(maru.MaruTimerRateMultiplier, Is.EqualTo(1.15f).Within(0.0001f));
            Assert.That(runs.Current.items, Does.Contain("OBJ_CollarFragment"));

            var elapsedBeforePawprint = stage.RuntimeState.elapsedTime;
            var pawprint = maru.ApplyMaruElementEvent(new MaruElementEventRequest
            {
                EventType = MaruElementEventType.PawprintPoolTriggered,
                ElementId = "MARU_PawprintPool",
                SourceRuntimeId = "paw_test",
                Seconds = 8f,
                GuidanceSeconds = 4f,
            });
            Assert.That(pawprint.Accepted, Is.True);
            Assert.That(stage.RuntimeState.elapsedTime, Is.EqualTo(elapsedBeforePawprint + 8f).Within(0.001f));
            Assert.That(maru.ForcedExitGuidanceRemaining, Is.EqualTo(4f).Within(0.001f));

            Assert.That(stage.RoomGraph.TryGetRoom(stage.RuntimeState.currentRoomId, out RoomRuntime room), Is.True);
            stage.CurrentPlayer.SnapTo(room.GetPrimarySafePosition() + Vector2.right * 2f);
            var marker = maru.ApplyMaruElementEvent(new MaruElementEventRequest
            {
                EventType = MaruElementEventType.ReturnMarkerUsed,
                ElementId = "MARU_ReturnMarker",
                SourceRuntimeId = "marker_test",
                MarkerCostType = MaruMarkerCostType.Money,
                MarkerCostValue = 50,
            });
            Assert.That(marker.Accepted, Is.True);
            Assert.That(runs.Current.moneyWon, Is.EqualTo(450));
            Assert.That(Vector2.Distance(stage.CurrentPlayer.transform.position, room.GetPrimarySafePosition()), Is.LessThan(0.01f));

            var casket = maru.ApplyMaruElementEvent(new MaruElementEventRequest
            {
                EventType = MaruElementEventType.RecordTravelerFreed,
                ElementId = "MARU_RecordCasket",
                SourceRuntimeId = "casket_test",
                RewardId = "record_traveler_freed",
                RecordGuideEffect = MaruRecordGuideEffect.ExitDirection,
                NoiseLevel = 0.15f,
            });
            Assert.That(casket.Accepted, Is.True);
            Assert.That(runs.Current.flags, Does.Contain("record_traveler_freed"));

            var jar = maru.ApplyMaruElementEvent(new MaruElementEventRequest
            {
                EventType = MaruElementEventType.BellJarBroken,
                ElementId = "MARU_ReturnBellJar",
                SourceRuntimeId = "jar_test",
                RewardMoney = 300,
                Seconds = 12f,
            });
            Assert.That(jar.Accepted, Is.True);
            Assert.That(runs.Current.moneyWon, Is.EqualTo(750));
            Assert.That(maru.ScheduledCurrentRoomEntryRemaining, Is.EqualTo(12f).Within(0.001f));
            maru.AdvanceMapElementEffectsForTests(12f);
            Assert.That(maru.CurrentBellPhase, Is.EqualTo(BellPhase.Maru));
            Assert.That(maru.LogicalRoomId, Is.EqualTo(stage.RuntimeState.currentRoomId));
            yield return null;
        }

        private static IEnumerator StartRun()
        {
            var bootstrap = GameBootstrap.Instance;
            if (bootstrap == null)
            {
                bootstrap = new GameObject(GameBootstrap.ServiceRootName).AddComponent<GameBootstrap>();
            }

            var flow = bootstrap.Services.GetRequired<GameFlowController>();
            Assert.That(flow.StartNewRun(), Is.True);
            var timeoutAt = Time.realtimeSinceStartup + 10f;
            while ((flow.State != GameApplicationState.Playing || flow.IsTransitioning) &&
                   Time.realtimeSinceStartup < timeoutAt)
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
