using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class StarPostOfficeDepartureGate : MonoBehaviour, IStarNightInteractable
    {
        private bool departed;
        public string Prompt
        {
            get
            {
                StarNightRunState run = StarNightRunState.Instance;
                StarNightChapterState chapter = run?.Chapter;
                if (chapter != null && chapter.GateLoopEnabled && !chapter.GateActivated)
                {
                    return chapter.GateReady
                        ? "별문 손잡이를 먼저 당기기"
                        : "주소를 잃은 우편선 살펴보기";
                }
                if (chapter != null && chapter.GateLoopEnabled &&
                    !run.GetFlag("CH4_RANI_COMMAND_FRAGMENT_READ"))
                {
                    return "메인 통신 기록을 먼저 확인하기";
                }
                if (chapter != null && chapter.GateLoopEnabled && chapter.GateClosing)
                {
                    return "이동하는 우체통을 붙잡고 출항하기";
                }
                return "해님 정원행 우편선에 항로 등록하기";
            }
        }

        public void Interact(StarNightPlayerAgent player)
        {
            if (departed)
            {
                return;
            }

            StarNightRunState run = StarNightRunState.Ensure();
            bool canDepart = run.Chapter.GateLoopEnabled
                ? run.Chapter.GateActivated && run.Chapter.DepartureOpen
                : run.Chapter.DepartureReady && run.GetFlag("CH4_ROUTE_STAMP_RECOVERED");
            if (!canDepart)
            {
                StarNightHUD.Instance?.Toast(run.Chapter.GateLoopEnabled
                    ? run.Chapter.GateReady
                        ? "주소 2/2가 준비됐다. 별문 손잡이를 직접 당겨야 우편선 항로가 열린다."
                        : $"북극성 주소가 부족하다. {run.Chapter.GateContributions}/{run.Chapter.GateRequired}"
                    : "북극성 항로 도장이 아직 등록되지 않았다.");
                return;
            }
            if (run.Chapter.GateLoopEnabled &&
                !run.GetFlag("CH4_RANI_COMMAND_FRAGMENT_READ"))
            {
                StarNightHUD.Instance?.Toast(
                    "메인 동선의 라니 통신 기록 일부를 확인해야 다음 별의 맥락을 이해할 수 있다.", 5f);
                return;
            }
            if (run.GetFlag("CH4_RETURN_VAULT_OPENED") &&
                !run.GetFlag("CH4_RANI_COMMAND_CONTEXT_READ"))
            {
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.DepartedWithUnresolvedEvent,
                    actorId = "Player",
                    targetId = "RaniCommandContext",
                    detail = "심층 보관소를 열었지만 라니 명령의 전체 맥락은 읽지 않고 떠났다"
                });
            }

            if (!HasResolvedLetter(run))
            {
                run.SetFlag("CH4_LETTER_STATE_SEALED");
                run.SetFlag("CH4_LETTER_PRESERVED");
                run.SetFlag("STARPATH_LETTER_PRESERVED");
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.LetterPreserved,
                    actorId = "Player",
                    targetId = "RaniLastLetter",
                    detail = "마지막 편지의 봉인을 건드리지 않고 우체국에 남겨 두었다",
                    witnessed = true
                });
            }

            if (run.Chapter.GateLoopEnabled && run.Chapter.GateClosing)
            {
                run.SetFlag("CH4_NARROW_ESCAPE");
            }

            departed = true;
            StarChapterReport report = run.CompleteCurrentChapter();
            run.ConsequenceResolver.ResolveStarPostOffice();
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.ChapterTransitioned,
                actorId = "Mailship",
                targetId = StarChapterId.SleepingSunGarden.ToString(),
                detail = "해님 정원행 우편선이 북극성 주소를 따라 출항했다"
            });
            StarNightHUD.Instance?.ShowEnding("해님 정원행 우편선에 북극성 항로가 찍혔다",
                report?.raniSummary ?? run.Watcher.ResolveRaniSummary(StarChapterId.StarPostOffice));
            if (player != null)
            {
                player.enabled = false;
            }
            StartCoroutine(LoadSunGarden(player));
        }

        private IEnumerator LoadSunGarden(StarNightPlayerAgent player)
        {
            yield return new WaitForSeconds(JourneyIntermissionFormatter.TransitionDelay);
            const string sceneName = "StarNight_SleepingSunGarden";
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                departed = false;
                if (player != null)
                {
                    player.enabled = true;
                }
                StarNightHUD.Instance?.Toast("P4 잠든 해님의 정원 씬이 빌드 목록에 없다. P4 빌더를 실행해 주세요.", 6f);
                yield break;
            }
            SceneManager.LoadScene(sceneName);
        }

        private static bool HasResolvedLetter(StarNightRunState run)
        {
            return run.GetFlag("CH4_LETTER_STATE_OPENED") ||
                   run.GetFlag("CH4_LETTER_STATE_DELIVERED") ||
                   run.GetFlag("CH4_LETTER_STATE_DISMANTLED") ||
                   run.GetFlag("CH4_LETTER_STATE_LOST_TO_MARU") ||
                   run.GetFlag("CH4_LETTER_STATE_COPIED") ||
                   run.GetFlag("CH4_LETTER_SEAL_DAMAGED") ||
                   run.GetFlag("CH4_LETTER_PRESERVED");
        }
    }
}
