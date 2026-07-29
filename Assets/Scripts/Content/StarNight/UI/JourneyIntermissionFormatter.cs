using System.Linq;

namespace StarFetchingNight
{
    public static class JourneyIntermissionFormatter
    {
        public const float TransitionDelay = 5.2f;

        public static string Build(StarNightRunState run, string raniSummary)
        {
            if (run == null)
            {
                return raniSummary ?? string.Empty;
            }

            StarChapterId chapter = run.CurrentChapter;
            StarActionRecord replay = run.Actions.Records
                .Where(record => record.chapter == chapter && IsReplayable(record.actionType))
                .LastOrDefault();
            string replayText = replay != null && !string.IsNullOrWhiteSpace(replay.detail)
                ? replay.detail
                : "이번 정거장에서의 선택을 짧게 되감았다.";

            string stamp = chapter == StarChapterId.Prologue
                ? "여행 티켓의 다섯 별문이 처음 드러났다."
                : $"{RunRouteMap.GetStationName(RunRouteMap.GetGateIndex(chapter))} 도장 · " +
                  $"{run.RouteMap.RestoredGateCount}/{RunRouteMap.GateCount}";

            return
                $"1. 티켓 도장 · {stamp}\n" +
                $"2. 행동 되감기 · {replayText}\n" +
                $"3. 라니의 한 문장 · {Condense(raniSummary)}\n" +
                $"4. 나의 답 · {PlayerResponse(chapter)}\n" +
                $"5. 마루의 발자국 · {RunRouteMap.GetStationName(run.RouteMap.MaruStationIndex)} 쪽으로 이동\n" +
                $"6. 다음 목표 · {NextObjective(chapter)}";
        }

        private static bool IsReplayable(StarActionType action) =>
            action != StarActionType.ChapterDeparted &&
            action != StarActionType.ChapterTransitioned &&
            action != StarActionType.ChapterLoopStateChanged &&
            action != StarActionType.GateContributionAdded &&
            action != StarActionType.GateReady &&
            action != StarActionType.GateActivated &&
            action != StarActionType.GateClosing &&
            action != StarActionType.BellPhaseChanged;

        private static string Condense(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "기록되지 않은 선택도 다음 정거장에 흔적을 남긴다.";
            }
            return value.Replace("\r", " ").Replace("\n", " ").Trim();
        }

        private static string PlayerResponse(StarChapterId chapter) => chapter switch
        {
            StarChapterId.Prologue => "마루가 북극성에 닿기 전에 다섯 별문을 되살릴게.",
            StarChapterId.MoonRabbitMill => "목표만 맞으면 과정은 정말 괜찮은 걸까?",
            StarChapterId.MagpieBridge => "별문을 어떻게 열었는지도 기억해야 해.",
            StarChapterId.CloudWhaleRanch => "살아가는 데 쓰는 것을 함부로 가져가면 안 돼.",
            StarChapterId.StarPostOffice => "편지가 정말 물건처럼 다뤄져도 되는 걸까?",
            StarChapterId.SleepingSunGarden => "기다리는 동안에도 내가 할 수 있는 일이 있었어.",
            _ => "마지막 선택은 내가 직접 할 거야."
        };

        private static string NextObjective(StarChapterId chapter) => chapter switch
        {
            StarChapterId.Prologue => "달토끼 방앗간의 꺼진 별문 복구",
            StarChapterId.MoonRabbitMill => "까치다리 정거장의 끊어진 연결 복구",
            StarChapterId.MagpieBridge => "구름고래 목장의 멈춘 비 순환 복구",
            StarChapterId.CloudWhaleRanch => "별 우체국의 잃어버린 주소 복구",
            StarChapterId.StarPostOffice => "잠든 해의 정원에 별길 다시 심기",
            StarChapterId.SleepingSunGarden => "북극성 관측소에서 마루를 멈추기",
            _ => "여행의 결말 선택"
        };
    }
}
