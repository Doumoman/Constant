using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class MagpieBridgeChapterBootstrap : MonoBehaviour
    {
        [SerializeField] private int fallbackSeed = 2718;
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
            }

            StarChapterDefinition definition = new()
            {
                chapter = StarChapterId.MagpieBridge,
                displayName = useGateLoopV02
                    ? "제2장 · 까치다리 정거장의 꺼진 별문"
                    : "제2장 · 까치다리 정거장",
                coreVerb = FableVerb.Link,
                oneSentenceRule = "연결된 두 대상은 힘을 나누고, 팽팽할수록 강하게 당기지만 끊어질 위험도 커진다.",
                requiredDepartureItems = useGateLoopV02 ? 2 : 3,
                useGateLoop = useGateLoopV02,
                gateContributionRequired = 2,
                objectiveNoun = useGateLoopV02 ? "별문 다리 닻" : "다리 닻 복구",
                objectiveInstruction = useGateLoopV02
                    ? "새 닻·폭풍탑 예비 닻·옛 물류 닻 중 서로 다른 두 경로를 해결하고 별문에 직접 연결하자."
                    : "R로 붉은 실을 고르고, E로 두 끝점을 차례로 연결하자. 절구로 무게를 바꾸면 당겨지는 속도도 달라진다.",
                guaranteedRooms = new List<string>
                {
                    "달떡 물류 승강장", "첫 매듭 교실", "흔들상자 선로", "제1 닻",
                    "까치 휴게 둥지", "장력 계단", "제2 닻", "은하수 환승 홀",
                    "옛 물류 다리", "제3 닻", "해치의 출항문", "별기차 선착장"
                },
                optionalRooms = new List<string>
                {
                    "끊어진 짐칸", "붉은 실 실패 보관소", "까마득한 별사다리",
                    "라니 통신 기록실", "다리지기 가람의 휴게실"
                }
            };
            if (useGateLoopV02)
            {
                definition.gateRoutes.Add(new GateRouteDefinition
                {
                    id = "CH2_ROUTE_NEW_ANCHOR",
                    displayName = "A 안전·협력 · 까치들과 새 닻 설치",
                    archetype = GateRouteArchetype.Cooperation,
                    contributionId = "CH2_NEW_ANCHOR",
                    contributionDisplayName = "새 닻"
                });
                definition.gateRoutes.Add(new GateRouteDefinition
                {
                    id = "CH2_ROUTE_STORM_ANCHOR",
                    displayName = "B 위험·탐색 · 폭풍탑 예비 닻",
                    archetype = GateRouteArchetype.Exploration,
                    contributionId = "CH2_STORM_ANCHOR",
                    contributionDisplayName = "예비 닻"
                });
                definition.gateRoutes.Add(new GateRouteDefinition
                {
                    id = "CH2_ROUTE_OLD_BRIDGE",
                    displayName = "C 빠름·전용 · 옛 물류 다리",
                    archetype = GateRouteArchetype.Appropriation,
                    contributionId = "CH2_OLD_ANCHOR",
                    contributionDisplayName = "낡은 닻"
                });
            }

            run.BeginChapter(definition);
            if (useGateLoopV02)
            {
                run.ChapterLoop.EnterRuleIntro();
                run.ChapterLoop.OpenRoutes();
            }

            float inheritedScent = run.ConsequenceResolver.GetStartingScent(StarChapterId.MagpieBridge);
            if (inheritedScent > 0f)
            {
                run.Chapter.AddScent(inheritedScent, "이전 밤의 결과가 정거장까지 따라왔다", "Chapter1");
            }
            if (run.GetFlag("CH1_MILL_REPAIRED"))
            {
                run.RedThread.Reinforce(1.12f);
            }
        }

        private IEnumerator Start()
        {
            yield return null;
            if (run.GetFlag("CH1_MILL_REPAIRED"))
            {
                StarNightHUD.Instance?.Toast("고쳐진 방앗간에서 보낸 물류 상자가 도착했다. 붉은 실이 조금 더 튼튼하다.", 5f);
            }
            else if (run.GetFlag("CH1_MILL_DAMAGED"))
            {
                StarNightHUD.Instance?.Toast("달토끼 물류가 끊겼다. 정거장에는 무거운 대체 부품만 남아 있다.", 5f);
            }
            else
            {
                StarNightHUD.Instance?.Toast(useGateLoopV02
                    ? "세 경로 중 두 닻을 확보해 별문에 연결하자. 해치의 선택은 닻과 별개의 사건이다."
                    : "붉은 실은 두 대상을 차례로 골라 연결한다. R로 도구를 바꾸자.", 5f);
            }
        }
    }
}
