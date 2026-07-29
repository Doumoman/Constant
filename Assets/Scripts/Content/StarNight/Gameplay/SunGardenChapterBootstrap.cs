using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StarFetchingNight
{
    [DefaultExecutionOrder(-90)]
    public sealed class SunGardenChapterBootstrap : MonoBehaviour
    {
        [SerializeField] private int fallbackSeed = 51515;
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
                run.UnlockTool(FableVerb.Float);
                run.UnlockTool(FableVerb.Deliver);
            }

            StarChapterDefinition definition = new()
            {
                chapter = StarChapterId.SleepingSunGarden,
                displayName = useGateLoopV02
                    ? "제5장 · 잠든 해님의 정원의 꺼진 별문"
                    : "제5장 · 잠든 해님의 정원",
                coreVerb = FableVerb.Awaken,
                oneSentenceRule = "빛을 받은 것은 깨어나고 자란다. 너무 많은 빛은 생명과 구조물을 말려 태운다.",
                requiredDepartureItems = useGateLoopV02 ? 2 : 3,
                useGateLoop = useGateLoopV02,
                gateContributionRequired = 2,
                objectiveNoun = useGateLoopV02 ? "별문 길꽃" : "별길 나무의 빛마디",
                objectiveInstruction = useGateLoopV02
                    ? "저장 햇빛·온실 꼭대기·해오름 기상 중 서로 다른 두 경로를 해결하고 길꽃 빛을 별문에 직접 심자."
                    : "정원의 저장 햇빛을 모아 별길 나무에 세 번 심으세요.",
                guaranteedRooms = new List<string>
                {
                    "햇빛 씨앗 묘상", "잠든 문지기 온실", "해오름의 잠자리",
                    "별길 나무 뿌리", "관측소행 별가지"
                },
                optionalRooms = new List<string>
                {
                    "라니가 멈춘 화분", "그늘꽃 냉각실", "해바라기 꼭대기"
                }
            };
            if (useGateLoopV02)
            {
                definition.gateRoutes.Add(new GateRouteDefinition
                {
                    id = "CH5_ROUTE_STORED_SUNLIGHT",
                    displayName = "A 안전·협력 · 저장 햇빛 모으기",
                    archetype = GateRouteArchetype.Cooperation,
                    contributionId = "CH5_EVEN_LIGHT",
                    contributionDisplayName = "고른 빛"
                });
                definition.gateRoutes.Add(new GateRouteDefinition
                {
                    id = "CH5_ROUTE_GREENHOUSE_TOP",
                    displayName = "B 위험·탐색 · 온실 꼭대기",
                    archetype = GateRouteArchetype.Exploration,
                    contributionId = "CH5_HIGH_LIGHT",
                    contributionDisplayName = "높은 빛"
                });
                definition.gateRoutes.Add(new GateRouteDefinition
                {
                    id = "CH5_ROUTE_HAOREUM_WAKE",
                    displayName = "C 빠름·개입 · 해오름 강제 기상",
                    archetype = GateRouteArchetype.Appropriation,
                    contributionId = "CH5_HAOREUM_LIGHT",
                    contributionDisplayName = "해오름 빛"
                });
            }

            run.BeginChapter(definition);
            if (useGateLoopV02)
            {
                run.ChapterLoop.EnterRuleIntro();
                run.ChapterLoop.OpenRoutes();
            }

            run.UnlockTool(FableVerb.Resize);
            run.UnlockTool(FableVerb.Link);
            run.UnlockTool(FableVerb.Float);
            run.UnlockTool(FableVerb.Deliver);
            run.UnlockTool(FableVerb.Awaken);
            ApplyPostOfficeInheritance(run);
            float inheritedScent = run.ConsequenceResolver.GetStartingScent(StarChapterId.SleepingSunGarden);
            if (inheritedScent > 0f)
            {
                run.Chapter.AddScent(inheritedScent, "별 우체국에서 온 편지와 분류 기록이 정원까지 따라왔다",
                    "P3Inheritance");
            }
            run.SunSeeds.AddCharges(1);
        }

        private IEnumerator Start()
        {
            yield return null;
            StarNightHUD.Instance?.Toast(useGateLoopV02
                ? "길꽃 빛 2개를 별문에 심자. 해오름을 깨우는 빠른 길은 정원 과열과 피로를 남긴다."
                : "햇빛 씨앗은 생명을 깨우지만, 겹친 빛은 정원을 태운다.", 6f);
        }

        private static void ApplyPostOfficeInheritance(StarNightRunState run)
        {
            float initialHeat = 0f;
            if (run.GetFlag("CH4_LETTER_STATE_DELIVERED"))
            {
                run.SetFlag("CH5_RANI_SILENCE");
            }
            if (run.GetFlag("CH4_LETTER_STATE_OPENED"))
            {
                run.SetFlag("CH5_RANI_ARGUMENT_ECHO");
                initialHeat += 10f;
            }
            if (run.GetFlag("CH4_LETTER_STATE_DISMANTLED"))
            {
                run.SetFlag("CH5_TELEPORT_CORE_SHORTCUT");
            }
            if (run.GetFlag("CH4_LETTER_STATE_LOST_TO_MARU"))
            {
                run.SetFlag("CH5_MARU_KNOWS_LETTER");
            }
            if (run.GetFlag("CH4_LETTER_PRESERVED") &&
                !run.GetFlag("CH4_LETTER_STATE_OPENED"))
            {
                run.SetFlag("CH5_SHADE_GREENHOUSE");
                initialHeat -= 6f;
            }
            if (run.GetFlag("CH4_SORTER_OVERLOAD") && !run.GetFlag("CH4_SORTER_REPAIRED"))
            {
                run.SetFlag("CH5_SORTER_DEBRIS_FLAMMABLE");
                initialHeat += 15f;
            }
            run.Heat.SetInitialHeat(Mathf.Max(0f, initialHeat));
        }
    }
}
