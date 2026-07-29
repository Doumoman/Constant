using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class SunGardenDepartureGate : MonoBehaviour, IStarNightInteractable
    {
        private bool departed;
        public string Prompt
        {
            get
            {
                StarNightChapterState chapter = StarNightRunState.Instance?.Chapter;
                if (chapter != null && chapter.GateLoopEnabled && !chapter.GateActivated)
                {
                    return chapter.GateReady
                        ? "별문 손잡이를 먼저 당기기"
                        : "접혀 있는 관측소행 길꽃 살펴보기";
                }
                if (chapter != null && chapter.GateLoopEnabled && chapter.GateClosing)
                {
                    return "모든 광원이 드러난 닫히는 별문으로 뛰어들기";
                }
                return "길꽃 별문을 타고 북극성 관측소로 출항하기";
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
                : run.Chapter.DepartureReady;
            if (!canDepart)
            {
                StarNightHUD.Instance?.Toast(run.Chapter.GateLoopEnabled
                    ? run.Chapter.GateReady
                        ? "길꽃 2/2가 준비됐다. 별문 손잡이를 직접 당겨야 출항할 수 있다."
                        : $"별문 길꽃이 부족하다. {run.Chapter.GateContributions}/{run.Chapter.GateRequired}"
                    : $"별길 나무의 빛마디가 부족하다. {run.Chapter.DepartureProgress}/{run.Chapter.RequiredDepartureProgress}");
                return;
            }
            if (run.Chapter.GateLoopEnabled &&
                !run.GetFlag("CH5_MARU_COMMAND_ECHO_HEARD"))
            {
                StarNightHUD.Instance?.Toast(
                    "정원 입구에서 마루가 반복한 명령을 먼저 들어야 한다.");
                return;
            }

            if (!run.Chapter.GateLoopEnabled && !HasTreeConclusion(run))
            {
                run.SetFlag("CH5_STAR_PATH_TREE_STABLE");
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.StarPathStabilized,
                    actorId = "Player",
                    targetId = "StarPathTree",
                    detail = "더 키우지 않고 현재 크기의 별길 나무를 안정된 항로로 남겼다",
                    helpedResident = true,
                    witnessed = true
                });
            }
            if (run.Chapter.GateLoopEnabled && run.Chapter.GateClosing)
            {
                run.SetFlag("CH5_NARROW_ESCAPE");
            }

            departed = true;
            StarChapterReport report = run.CompleteCurrentChapter();
            run.ConsequenceResolver.ResolveSleepingSunGarden();
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.ChapterTransitioned,
                actorId = "GardenStarGate",
                targetId = StarChapterId.PolarisObservatory.ToString(),
                detail = "길꽃 별문이 북극성 관측소 방향으로 열렸다"
            });
            StarNightHUD.Instance?.ShowEnding(run.Chapter.GateLoopEnabled
                    ? "길꽃 두 송이가 북극성 관측소로 향하는 별문을 밝혔다"
                    : "별길 나무가 북극성 관측소까지 가지를 뻗었다",
                report?.raniSummary ?? run.Watcher.ResolveRaniSummary(StarChapterId.SleepingSunGarden));
            if (player != null)
            {
                player.enabled = false;
            }
            StartCoroutine(LoadPolaris(player));
        }

        private IEnumerator LoadPolaris(StarNightPlayerAgent player)
        {
            yield return new WaitForSeconds(JourneyIntermissionFormatter.TransitionDelay);
            const string sceneName = "StarNight_PolarisObservatory";
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                departed = false;
                if (player != null)
                {
                    player.enabled = true;
                }
                StarNightHUD.Instance?.Toast(
                    "북극성 관측소 씬이 빌드 목록에 없습니다. M5 빌더를 실행해 주세요.", 6f);
                yield break;
            }
            SceneManager.LoadScene(sceneName);
        }

        private static bool HasTreeConclusion(StarNightRunState run)
        {
            return run.GetFlag("CH5_STAR_PATH_TREE_STABLE") ||
                   run.GetFlag("CH5_STAR_PATH_TREE_OVERGROWN") ||
                   run.GetFlag("CH5_STAR_PATH_TREE_BURNED");
        }
    }
}
