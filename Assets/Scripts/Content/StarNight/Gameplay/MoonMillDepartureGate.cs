using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class MoonMillDepartureGate : MonoBehaviour, IStarNightInteractable
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
                        : "꺼진 달배 살펴보기";
                }
                if (chapter != null && chapter.GateLoopEnabled && chapter.GateClosing)
                {
                    return "닫히는 별문으로 뛰어들기";
                }
                return "달배에 오르기";
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
                        ? "길떡 2/2가 준비됐다. 별문 허브의 손잡이를 직접 당겨야 달배가 깨어난다."
                        : $"별문 길떡이 부족하다. {run.Chapter.GateContributions}/{run.Chapter.GateRequired}"
                    : $"별 연료가 부족하다. 달떡 {run.Chapter.DepartureProgress}/{run.Chapter.RequiredDepartureProgress}");
                return;
            }

            if (run.GetFlag("moonmill.temptation.open") && !run.GetFlag("moonmill.temptation.resolved"))
            {
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.DepartedWithUnresolvedEvent,
                    actorId = "Player",
                    targetId = "MoonBackStorage",
                    detail = "뒤편 창고의 빛을 남긴 채 떠났다"
                });
            }

            if (run.Chapter.GateLoopEnabled && run.Chapter.GateClosing)
            {
                run.SetFlag("CH1_NARROW_ESCAPE");
            }

            transitioning = true;
            run.ConsequenceResolver.ResolveMoonMill();
            StarChapterReport report = run.CompleteCurrentChapter();
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.ChapterTransitioned,
                actorId = "MoonBoat",
                targetId = StarChapterId.MagpieBridge.ToString(),
                detail = "달배가 까치다리 정거장으로 향했다"
            });
            StarNightHUD.Instance?.ShowEnding("달배가 까치다리 정거장으로 떠난다",
                report?.raniSummary ?? run.Watcher.ResolveRaniSummary(StarChapterId.MoonRabbitMill));
            if (player != null)
            {
                player.enabled = false;
            }
            StartCoroutine(LoadMagpieBridge(player));
        }

        private IEnumerator LoadMagpieBridge(StarNightPlayerAgent player)
        {
            yield return new WaitForSeconds(JourneyIntermissionFormatter.TransitionDelay);
            const string sceneName = "StarNight_MagpieBridge";
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                transitioning = false;
                if (player != null)
                {
                    player.enabled = true;
                }
                StarNightHUD.Instance?.Toast("P1 정거장 씬이 빌드 목록에 없다. P1 빌더를 실행해 주세요.", 6f);
                yield break;
            }
            SceneManager.LoadScene(sceneName);
        }
    }
}
