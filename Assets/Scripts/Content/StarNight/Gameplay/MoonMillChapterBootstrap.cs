using System.Collections.Generic;
using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class MoonMillChapterBootstrap : MonoBehaviour
    {
        [SerializeField] private int fixedSeed = 173;
        [SerializeField] private bool useRandomSeed;
        [SerializeField] private bool useGateLoopV02;

        public void ConfigureGateLoop(bool enabled)
        {
            useGateLoopV02 = enabled;
        }

        private void Awake()
        {
            StarNightRunState run = StarNightRunState.Ensure();
            if (!run.RunActive)
            {
                run.BeginNewRun(useRandomSeed ? null : fixedSeed);
            }
            StarChapterDefinition definition = new()
            {
                chapter = StarChapterId.MoonRabbitMill,
                displayName = useGateLoopV02
                    ? "제1장 · 달토끼 방앗간의 꺼진 별문"
                    : "제1장 · 달토끼 방앗간",
                coreVerb = FableVerb.Resize,
                oneSentenceRule = "크기를 바꾸면 길도 사고도 함께 커진다.",
                requiredDepartureItems = useGateLoopV02 ? 2 : 3,
                useGateLoop = useGateLoopV02,
                gateContributionRequired = 2,
                objectiveNoun = useGateLoopV02 ? "별문 길떡" : "별 연료 달떡",
                objectiveInstruction = useGateLoopV02
                    ? "방앗간·달광산·겨울 저장고 중 두 경로를 해결하고 길떡을 직접 장착하자."
                    : "방앗간을 고치거나 달떡의 크기를 바꿔 길을 만들자.",
                guaranteedRooms = new List<string>
                {
                    "달토끼 도착 마당", "작아지는 오솔길", "방앗간 앞뜰", "부서진 물레방",
                    "달김 굴뚝", "달떡 창고", "별가루 결정고", "겨울 달떡 저장고",
                    "별냄새 방울방", "달빛 승강장", "달배 선착장"
                },
                optionalRooms = new List<string>
                {
                    "달가루 다락", "매달린 자루 지름길", "멈춘 시계 서까래", "톡톡별 온실",
                    "잃어버린 소포굴", "별가루 깊은 저장고", "달 뒤편 창고", "방울지붕"
                }
            };

            if (useGateLoopV02)
            {
                definition.gateRoutes.Add(new GateRouteDefinition
                {
                    id = "CH1_ROUTE_MILL",
                    displayName = "A 안전·협력 · 방앗간 수리",
                    archetype = GateRouteArchetype.Cooperation,
                    contributionId = "CH1_PATH_CAKE_MILL",
                    contributionDisplayName = "새 길떡"
                });
                definition.gateRoutes.Add(new GateRouteDefinition
                {
                    id = "CH1_ROUTE_MINE",
                    displayName = "B 위험·탐색 · 달광산",
                    archetype = GateRouteArchetype.Exploration,
                    contributionId = "CH1_PATH_CAKE_MINE",
                    contributionDisplayName = "광산 길떡"
                });
                definition.gateRoutes.Add(new GateRouteDefinition
                {
                    id = "CH1_ROUTE_STORAGE",
                    displayName = "C 빠름·차용 · 겨울 저장고",
                    archetype = GateRouteArchetype.Appropriation,
                    contributionId = "CH1_PATH_CAKE_STORAGE",
                    contributionDisplayName = "저장 길떡"
                });
            }

            run.BeginChapter(definition);
            if (useGateLoopV02)
            {
                run.ChapterLoop.EnterRuleIntro();
                run.ChapterLoop.OpenRoutes();
            }
        }
    }
}
