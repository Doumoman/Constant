using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class CloudWhaleChapterBootstrap : MonoBehaviour
    {
        [SerializeField] private int fallbackSeed = 31415;
        [SerializeField] private bool useGateLoopV02;
        private StarNightRunState run;

        public void ConfigureGateLoop(bool enabled)
        {
            useGateLoopV02 = enabled;
        }

        private void Awake()
        {
            run = StarNightRunState.Ensure();
            if (!run.RunActive)
            {
                run.BeginNewRun(fallbackSeed);
                run.UnlockTool(FableVerb.Link);
            }

            StarChapterDefinition definition = new()
            {
                chapter = StarChapterId.CloudWhaleRanch,
                displayName = useGateLoopV02
                    ? "제3장 · 구름고래 목장의 꺼진 별문"
                    : "제3장 · 구름고래 목장",
                coreVerb = FableVerb.Float,
                oneSentenceRule = "무게는 사라지지 않는다. 한쪽을 띄우면 그 무게를 다른 대상이나 구름에 남겨야 한다.",
                requiredDepartureItems = useGateLoopV02 ? 2 : 3,
                useGateLoop = useGateLoopV02,
                gateContributionRequired = 2,
                objectiveNoun = useGateLoopV02 ? "출항 돛의 바람" : "비구름 수차 충전",
                objectiveInstruction = useGateLoopV02
                    ? "목장 수차·폭풍 능선·구루의 숨결 중 서로 다른 두 경로를 해결하고 별문 출항 돛에 직접 장착하자."
                    : "R로 구름병을 고르고 E로 무게를 담은 뒤, 비구름에 옮겨 수차까지 내려보내자.",
                guaranteedRooms = new List<string>
                {
                    "까치 화물 도착장", "무게 보존 교실", "첫 비구름 논", "몽실의 바람 헛간",
                    "낮은 구름 수차", "떠오르는 달떡 길", "두 번째 비구름 목책", "구루의 닻터",
                    "고래등 낙서 언덕", "폭풍 하중 회랑", "세 번째 비구름 절벽", "출항 풍차"
                },
                optionalRooms = new List<string>
                {
                    "무지개 위쪽 목장", "마른 까치 보급고", "폭풍 사고 관측대",
                    "라니와 동생의 낙서", "이동식 비구름 작업장"
                }
            };
            if (useGateLoopV02)
            {
                definition.gateRoutes.Add(new GateRouteDefinition
                {
                    id = "CH3_ROUTE_RANCH_WHEEL",
                    displayName = "A 안전·협력 · 목장 수차 복구",
                    archetype = GateRouteArchetype.Cooperation,
                    contributionId = "CH3_CLEAR_WIND",
                    contributionDisplayName = "맑은 바람"
                });
                definition.gateRoutes.Add(new GateRouteDefinition
                {
                    id = "CH3_ROUTE_STORM_RIDGE",
                    displayName = "B 위험·탐색 · 폭풍 능선",
                    archetype = GateRouteArchetype.Exploration,
                    contributionId = "CH3_GALE_WIND",
                    contributionDisplayName = "거센 바람"
                });
                definition.gateRoutes.Add(new GateRouteDefinition
                {
                    id = "CH3_ROUTE_GURU_BREATH",
                    displayName = "C 빠름·개입 · 구루 강제 기상",
                    archetype = GateRouteArchetype.Appropriation,
                    contributionId = "CH3_GURU_BREATH",
                    contributionDisplayName = "구루의 숨결"
                });
            }

            run.BeginChapter(definition);
            if (useGateLoopV02)
            {
                run.ChapterLoop.EnterRuleIntro();
                run.ChapterLoop.OpenRoutes();
            }

            float inheritedScent = run.ConsequenceResolver.GetStartingScent(StarChapterId.CloudWhaleRanch);
            if (inheritedScent > 0f)
            {
                run.Chapter.AddScent(inheritedScent, "까치다리에서 남긴 결과가 바람을 타고 왔다", "Chapter2");
            }
            if (run.GetFlag("CH2_OLD_BRIDGE_CUT") && !run.GetFlag("CH2_OLD_BRIDGE_RESTORED"))
            {
                run.SetFlag("CH3_SUPPLY_SHORTAGE");
            }
            if (run.GetFlag("CH2_MAGPIES_FORCED"))
            {
                run.SetFlag("CH3_RESCUE_SUPPORT_REDUCED");
            }
        }

        private IEnumerator Start()
        {
            yield return null;
            if (run.GetFlag("CH3_SUPPLY_SHORTAGE"))
            {
                StarNightHUD.Instance?.Toast("끊긴 옛 다리 때문에 까치 문양 보급 상자가 비었다. 무거운 목장 추를 재활용해야 한다.", 6f);
            }
            else if (run.GetFlag("CH3_RESCUE_SUPPORT_REDUCED"))
            {
                StarNightHUD.Instance?.Toast("지친 까치들은 폭풍 구조를 한 번도 도울 수 없다.", 5f);
            }
            else
            {
                StarNightHUD.Instance?.Toast(useGateLoopV02
                    ? "세 바람 중 두 개를 출항 돛에 채우자. 구루의 닻을 풀지는 별문과 별개의 선택이다."
                    : "구름병은 무게를 없애지 않는다. 첫 대상에서 담아 두 번째 대상에 남긴다.", 6f);
            }
        }
    }
}
