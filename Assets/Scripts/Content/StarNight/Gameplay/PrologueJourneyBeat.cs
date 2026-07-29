using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarFetchingNight
{
    public enum PrologueBeatMode
    {
        CheckSign,
        CheckCompanion,
        ReturnCakeEngine,
        MaruRescue,
        GuideStarLoss,
        Departure
    }

    [DisallowMultipleComponent]
    public sealed class PrologueJourneyBeat : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private PrologueBeatMode mode;
        [SerializeField] private Transform ship;
        [SerializeField] private Transform maru;
        [SerializeField] private GameObject guideStar;
        [SerializeField] private GameObject blackout;
        private bool transitioning;

        public PrologueBeatMode Mode => mode;
        public string Prompt => mode switch
        {
            PrologueBeatMode.CheckSign => "달 표지판 읽기",
            PrologueBeatMode.CheckCompanion => "낯선 동행자 살피기",
            PrologueBeatMode.ReturnCakeEngine => "귀환떡을 우주선 엔진에 넣기",
            PrologueBeatMode.MaruRescue => "우주선을 문 개에게 다가가기",
            PrologueBeatMode.GuideStarLoss => "사라진 길잡이별과 여행 티켓 확인",
            PrologueBeatMode.Departure => "첫 별문으로 출발하기",
            _ => "살펴보기"
        };

        public void Configure(PrologueBeatMode value, Transform shipTransform = null,
            Transform maruTransform = null, GameObject guideStarObject = null, GameObject blackoutObject = null)
        {
            mode = value;
            ship = shipTransform;
            maru = maruTransform;
            guideStar = guideStarObject;
            blackout = blackoutObject;
        }

        public void Interact(StarNightPlayerAgent player)
        {
            Execute(player);
        }

        public bool ExecuteForTests() => Execute(null);

        private bool Execute(StarNightPlayerAgent player)
        {
            if (transitioning)
            {
                return false;
            }

            StarNightRunState run = StarNightRunState.Ensure();
            switch (mode)
            {
                case PrologueBeatMode.CheckSign:
                    if (run.GetFlag("PROLOGUE_CHECKED_SIGN")) return false;
                    run.SetFlag("PROLOGUE_CHECKED_SIGN");
                    Record(run, StarActionType.ObjectInspected, "MoonSign",
                        "표지판에는 ‘귀환떡은 잃어버린 것을 집으로 돌려보낸다’고 적혀 있었다.");
                    Say("표지판 · 귀환떡은 잃어버린 것을 집으로 돌려보낸다.");
                    return true;

                case PrologueBeatMode.CheckCompanion:
                    if (run.GetFlag("PROLOGUE_CHECKED_COMPANION")) return false;
                    run.SetFlag("PROLOGUE_CHECKED_COMPANION");
                    Record(run, StarActionType.ObjectInspected, "Rani",
                        "라니는 고장 난 우주선의 산소가 오래 버티지 못한다고 말했다.");
                    Say("라니 · 엔진이 멎었어. 산소가 다 떨어지기 전에 달에서 연료를 찾아야 해.");
                    return true;

                case PrologueBeatMode.ReturnCakeEngine:
                    if (run.GetFlag("PROLOGUE_USED_RETURN_CAKE")) return false;
                    if (!run.GetFlag("PROLOGUE_CHECKED_SIGN") || !run.GetFlag("PROLOGUE_CHECKED_COMPANION"))
                    {
                        Say("먼저 표지판과 라니의 진단을 확인하자.");
                        return false;
                    }
                    run.SetFlag("PROLOGUE_USED_RETURN_CAKE");
                    Record(run, StarActionType.ReturnCakeFueled, "ReturnCakeEngine",
                        "귀환떡을 연료로 넣자 우주선이 ‘집으로 돌아가려는’ 힘에 붙잡혀 폭주했다.");
                    if (ship != null)
                    {
                        ship.position = transform.position + new Vector3(9f, 3.2f, 0f);
                        ship.rotation = Quaternion.Euler(0f, 0f, 18f);
                    }
                    Say("우주선이 목적지가 아니라 ‘집’의 냄새를 따라 폭주한다!");
                    return true;

                case PrologueBeatMode.MaruRescue:
                    if (run.GetFlag("PROLOGUE_MARU_RESCUE_SEEN")) return false;
                    if (!run.GetFlag("PROLOGUE_USED_RETURN_CAKE"))
                    {
                        Say("아직 구조할 우주선이 떠오르지 않았다.");
                        return false;
                    }
                    run.SetFlag("PROLOGUE_MARU_RESCUE_SEEN");
                    Record(run, StarActionType.MaruRescuedShip, "Maru",
                        "마루가 폭주하는 우주선을 입에 물고 따라잡아 달바닥에 조심스럽게 내려놓았다.");
                    if (maru != null && ship != null)
                    {
                        ship.position = transform.position + new Vector3(-1.4f, -0.6f, 0f);
                        maru.position = ship.position + new Vector3(1.7f, 0.2f, 0f);
                        ship.rotation = Quaternion.identity;
                    }
                    Say("라니 · 저 개가 우리를 구했어. 물어뜯은 게 아니라, 돌아갈 곳에 내려놓은 거야.");
                    return true;

                case PrologueBeatMode.GuideStarLoss:
                    if (run.GetFlag("PROLOGUE_GUIDE_STAR_TAKEN")) return false;
                    if (!run.GetFlag("PROLOGUE_MARU_RESCUE_SEEN"))
                    {
                        Say("우주선을 붙잡은 존재를 먼저 확인하자.");
                        return false;
                    }
                    run.SetFlag("PROLOGUE_GUIDE_STAR_TAKEN");
                    run.SetFlag("TICKET_MAP_UNLOCKED");
                    run.SetFlag("PROLOGUE_FINAL_OBJECTIVE_HEARD");
                    run.Chapter.AddDepartureProgress(1, "PrologueGuideStar");
                    Record(run, StarActionType.GuideStarTaken, "GuideStar",
                        "마루가 길잡이별을 물어오자 다섯 항로와 별문이 차례로 꺼졌다.");
                    Record(run, StarActionType.TravelTicketUnlocked, "JourneyTicket",
                        "라니가 여행 티켓 위에 꺼진 다섯 별문과 북극성까지의 길을 드러냈다.");
                    if (guideStar != null) guideStar.SetActive(false);
                    if (blackout != null) blackout.SetActive(true);
                    Say("라니 · 마루는 잃어버린 것을 돌려보내지만 별길도 함께 물어와. " +
                        "북극성에 닿기 전에 티켓의 다섯 별문을 되살려야 해.", 6f);
                    return true;

                case PrologueBeatMode.Departure:
                    if (!run.GetFlag("TICKET_MAP_UNLOCKED") || !run.Chapter.DepartureReady)
                    {
                        Say("여행 티켓에 다섯 별문이 모두 나타나야 출발할 수 있다.");
                        return false;
                    }
                    transitioning = true;
                    StarChapterReport report = run.CompleteCurrentChapter();
                    if (report == null)
                    {
                        transitioning = false;
                        return false;
                    }
                    run.Actions.Record(new StarActionContext
                    {
                        actionType = StarActionType.ChapterTransitioned,
                        actorId = "RaniShip",
                        targetId = StarChapterId.MoonRabbitMill.ToString(),
                        detail = "수리한 우주선이 첫 번째 꺼진 별문, 달토끼 방앗간으로 향했다.",
                        witnessed = true
                    });
                    StarNightHUD.Instance?.ShowEnding("다섯 별문을 되찾는 여행",
                        report.raniSummary);
                    if (player != null) player.enabled = false;
                    if (Application.isPlaying)
                    {
                        StartCoroutine(LoadMoonMill(player));
                    }
                    return true;
            }
            return false;
        }

        private IEnumerator LoadMoonMill(StarNightPlayerAgent player)
        {
            yield return new WaitForSeconds(JourneyIntermissionFormatter.TransitionDelay);
            const string sceneName = "StarNight_MoonMill";
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                transitioning = false;
                if (player != null) player.enabled = true;
                Say("달토끼 방앗간 씬이 빌드 목록에 없습니다.", 6f);
                yield break;
            }
            SceneManager.LoadScene(sceneName);
        }

        private static void Record(StarNightRunState run, StarActionType action, string target, string detail)
        {
            run.Actions.Record(new StarActionContext
            {
                actionType = action,
                actorId = "Player",
                targetId = target,
                detail = detail,
                witnessed = true
            });
        }

        private static void Say(string message, float duration = 4f)
        {
            StarNightHUD.Instance?.Toast(message, duration);
        }
    }
}
