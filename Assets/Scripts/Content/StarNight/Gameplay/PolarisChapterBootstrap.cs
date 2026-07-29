using System.Collections.Generic;
using UnityEngine;

namespace StarFetchingNight
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class PolarisChapterBootstrap : MonoBehaviour
    {
        [SerializeField] private int directEntrySeed = 173;

        private void Awake()
        {
            StarNightRunState run = StarNightRunState.Ensure();
            bool directEntry = !run.RunActive;
            if (directEntry)
            {
                run.BeginNewRun(directEntrySeed);
                GrantDirectEntryRoute(run);
                run.SetFlag("POLARIS_DIRECT_DEBUG_RUN");
            }

            run.BeginChapter(CreateDefinition());
            if (run.RouteMap.RestoredGateCount >= RunRouteMap.GateCount)
            {
                run.UnlockTool(FableVerb.Resize);
                run.UnlockTool(FableVerb.Link);
                run.UnlockTool(FableVerb.Float);
                run.UnlockTool(FableVerb.Deliver);
                run.UnlockTool(FableVerb.Awaken);
            }

            PolarisFinaleState finale = run.GetComponent<PolarisFinaleState>();
            if (finale == null)
            {
                finale = run.gameObject.AddComponent<PolarisFinaleState>();
            }
            finale.Begin(run);
        }

        public static StarChapterDefinition CreateDefinition()
        {
            return new StarChapterDefinition
            {
                chapter = StarChapterId.PolarisObservatory,
                displayName = "제6장 · 북극성 관측소",
                coreVerb = FableVerb.Awaken,
                oneSentenceRule = "마루보다 먼저 중심별에 도달하고, 우주의 길을 어떻게 남길지 실제 행동으로 결정한다.",
                requiredDepartureItems = 1,
                useGateLoop = false,
                objectiveNoun = "중심별 복구",
                objectiveInstruction = "다섯 기록을 확인하고 생활 도구를 순서대로 사용해 중심별에 먼저 도달하자.",
                guaranteedRooms = new List<string>
                {
                    "다섯 도장의 입구", "기록 회랑", "닫힌 관측실", "중심별 추격로",
                    "깨진 별 복원대", "별자리 연결대", "부유 천구", "반송 항로", "재점화 정원",
                    "중심별 선택실"
                },
                optionalRooms = new List<string>
                {
                    "라니의 마지막 편지 보관함", "마루 최초 임무 기록", "별길 연결실"
                }
            };
        }

        private static void GrantDirectEntryRoute(StarNightRunState run)
        {
            StarChapterId[] gates =
            {
                StarChapterId.MoonRabbitMill,
                StarChapterId.MagpieBridge,
                StarChapterId.CloudWhaleRanch,
                StarChapterId.StarPostOffice,
                StarChapterId.SleepingSunGarden
            };
            foreach (StarChapterId gate in gates)
            {
                run.RouteMap.RegisterGateRestored(gate);
            }
            run.SetFlag("TICKET_MAP_UNLOCKED");
        }
    }
}
