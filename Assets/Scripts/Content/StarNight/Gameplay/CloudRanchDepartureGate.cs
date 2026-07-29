using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class CloudRanchDepartureGate : MonoBehaviour, IStarNightInteractable
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
                        : "바람이 비어 있는 출항 돛 살펴보기";
                }
                if (chapter != null && chapter.GateLoopEnabled && chapter.GateClosing)
                {
                    return "뒤집힌 풍향을 뚫고 바람선 출항하기";
                }
                return "바람선을 타고 목장 떠나기";
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
                        ? "바람 2/2가 준비됐다. 별문 손잡이를 직접 당겨야 출항할 수 있다."
                        : $"출항 돛의 바람이 부족하다. {run.Chapter.GateContributions}/{run.Chapter.GateRequired}"
                    : $"비구름 수차가 부족하다. {run.Chapter.DepartureProgress}/{run.Chapter.RequiredDepartureProgress}");
                return;
            }

            if (!run.GetFlag("CH3_GURU_RELEASED") && !run.GetFlag("CH3_GURU_AWAKENED_FORCEFULLY"))
            {
                run.SetFlag("CH3_GURU_CHOSE_STAY");
                run.SetNpcState("Guru", StarNpcState.Calm);
            }
            if (run.GetFlag("CH3_GURU_RELEASED") && !run.GetFlag("CH3_RAIN_SYSTEM_REBUILT"))
            {
                run.SetFlag("CH3_DROUGHT");
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.DepartedWithUnresolvedEvent,
                    actorId = "Player",
                    targetId = "GuruRainSystem",
                    detail = "구루의 닻을 풀었지만 비를 대신 보낼 장치는 만들지 않고 떠났다"
                });
            }
            if (run.Chapter.GateLoopEnabled && run.Chapter.GateClosing)
            {
                run.SetFlag("CH3_NARROW_ESCAPE");
            }

            transitioning = true;
            StarChapterReport report = run.CompleteCurrentChapter();
            run.ConsequenceResolver.ResolveCloudWhaleRanch();
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.ChapterTransitioned,
                actorId = "Windship",
                targetId = StarChapterId.StarPostOffice.ToString(),
                detail = "바람선이 별 우체국으로 향했다"
            });
            StarNightHUD.Instance?.ShowEnding("바람선이 별 우체국 쪽 구름길로 떠난다",
                report?.raniSummary ?? run.Watcher.ResolveRaniSummary(StarChapterId.CloudWhaleRanch));
            if (player != null)
            {
                player.enabled = false;
            }
            StartCoroutine(LoadStarPostOffice(player));
        }

        private IEnumerator LoadStarPostOffice(StarNightPlayerAgent player)
        {
            yield return new WaitForSeconds(JourneyIntermissionFormatter.TransitionDelay);
            const string sceneName = "StarNight_StarPostOffice";
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                transitioning = false;
                if (player != null)
                {
                    player.enabled = true;
                }
                StarNightHUD.Instance?.Toast("P3 별 우체국 씬이 빌드 목록에 없다. P3 빌더를 실행해 주세요.", 6f);
                yield break;
            }
            SceneManager.LoadScene(sceneName);
        }
    }
}
