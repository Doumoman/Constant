using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class MagpieBridgeDepartureGate : MonoBehaviour, IStarNightInteractable
    {
        private bool transitioning;
        public string Prompt
        {
            get
            {
                StarNightChapterState chapter = StarNightRunState.Instance?.Chapter;
                if (chapter != null && chapter.GateLoopEnabled && !chapter.GateActivated)
                {
                    return chapter.GateReady
                        ? "별문 손잡이를 먼저 당기기"
                        : "잠든 별기차 살펴보기";
                }
                if (chapter != null && chapter.GateLoopEnabled &&
                    chapter.GateActivated &&
                    StarNightRunState.Instance != null &&
                    !StarNightRunState.Instance.GetFlag("CH2_HAECHI_RESOLVED"))
                {
                    return "해치의 떠날 권리를 먼저 결정하기";
                }
                if (chapter != null && chapter.GateLoopEnabled && chapter.GateClosing)
                {
                    return "흔들리는 별문으로 뛰어들기";
                }
                return "별기차에 올라 정거장 떠나기";
            }
        }

        public void Interact(StarNightPlayerAgent player)
        {
            if (transitioning)
            {
                return;
            }

            StarNightRunState run = StarNightRunState.Ensure();
            bool canDepart = run.Chapter.GateLoopEnabled
                ? run.Chapter.GateActivated && run.Chapter.DepartureOpen
                : run.Chapter.DepartureReady;
            if (!canDepart)
            {
                StarNightHUD.Instance?.Toast(run.Chapter.GateLoopEnabled
                    ? run.Chapter.GateReady
                        ? "닻 2/2가 준비됐다. 별문 허브의 손잡이를 직접 당겨야 별기차가 깨어난다."
                        : $"별문 닻이 부족하다. {run.Chapter.GateContributions}/{run.Chapter.GateRequired}"
                    : $"다리 닻이 부족하다. {run.Chapter.DepartureProgress}/{run.Chapter.RequiredDepartureProgress}");
                return;
            }
            if (run.GetFlag("magpie.temptation.open") && !run.GetFlag("magpie.temptation.resolved"))
            {
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.DepartedWithUnresolvedEvent,
                    actorId = "Player",
                    targetId = "EndlessStarLadder",
                    detail = "까마득한 별사다리의 기억 조각을 남긴 채 떠났다"
                });
            }
            if (!run.GetFlag("CH2_HAECHI_RESOLVED"))
            {
                if (run.Chapter.GateLoopEnabled)
                {
                    StarNightHUD.Instance?.Toast(
                        "해치는 다른 행성으로 떠나고 싶어 한다. 닻과 별개로, 문을 잠글지 길을 열지 직접 결정해야 한다.", 5f);
                    return;
                }

                run.SetFlag("CH2_HAECHI_RESOLVED");
                run.SetFlag("CH2_HAECHI_ALLOWED");
                run.SetNpcState("Haechi", StarNpcState.Autonomous);
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.NpcAllowedChoice,
                    actorId = "Player",
                    targetId = "Haechi",
                    detail = "출항문에 손대지 않은 채 해치의 선택을 남겨 두었다",
                    witnessed = true
                });
            }

            if (run.Chapter.GateLoopEnabled && run.Chapter.GateClosing)
            {
                run.SetFlag("CH2_NARROW_ESCAPE");
            }

            transitioning = true;
            StarChapterReport report = run.CompleteCurrentChapter();
            run.ConsequenceResolver.ResolveMagpieBridge();
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.ChapterTransitioned,
                actorId = "StarTrain",
                targetId = StarChapterId.CloudWhaleRanch.ToString(),
                detail = "별기차가 구름고래 목장으로 향했다"
            });
            StarNightHUD.Instance?.ShowEnding("별기차가 은하수 다리를 건넜다",
                report?.raniSummary ?? run.Watcher.ResolveRaniSummary(StarChapterId.MagpieBridge));
            if (player != null)
            {
                player.enabled = false;
            }
            StartCoroutine(LoadCloudWhaleRanch(player));
        }

        private IEnumerator LoadCloudWhaleRanch(StarNightPlayerAgent player)
        {
            yield return new WaitForSeconds(JourneyIntermissionFormatter.TransitionDelay);
            const string sceneName = "StarNight_CloudWhaleRanch";
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                transitioning = false;
                if (player != null)
                {
                    player.enabled = true;
                }
                StarNightHUD.Instance?.Toast("P2 목장 씬이 빌드 목록에 없다. P2 빌더를 실행해 주세요.", 6f);
                yield break;
            }
            SceneManager.LoadScene(sceneName);
        }
    }
}
