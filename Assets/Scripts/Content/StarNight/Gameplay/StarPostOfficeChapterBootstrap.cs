using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class StarPostOfficeChapterBootstrap : MonoBehaviour
    {
        [SerializeField] private int fallbackSeed = 42424;
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
            }

            StarChapterDefinition definition = new()
            {
                chapter = StarChapterId.StarPostOffice,
                displayName = useGateLoopV02
                    ? "제4장 · 주소를 잃은 별 우체국"
                    : "제4장 · 별 우체국",
                coreVerb = FableVerb.Deliver,
                oneSentenceRule = "도장을 받은 대상은 지정한 주소로 이동한다. 경로의 방해물과 마루는 배송을 가로챌 수 있다.",
                requiredDepartureItems = useGateLoopV02 ? 2 : 1,
                useGateLoop = useGateLoopV02,
                gateContributionRequired = 2,
                objectiveNoun = useGateLoopV02 ? "북극성 항로 주소" : "북극성 항로 도장",
                objectiveInstruction = useGateLoopV02
                    ? "정규 분류·반송 불가 주소·봉인 편지 인장 중 서로 다른 두 경로를 해결하고 주소 조각을 직접 장착하자."
                    : "R로 별 우편 도장을 고르고 E로 소포와 주소를 차례로 지정해 분실 우편 보관소에 도달하자.",
                guaranteedRooms = new List<string>
                {
                    "바람선 도착 우편대", "빈 주소 교실", "달 상자 실습실", "행성 우체통 회랑",
                    "마른 잉크 창구", "자동 분류기 앞뜰", "반송 우편 경사로", "수신자 없는 편지실",
                    "라니 수신함", "폭주 분류실", "북극성 항로 등록소", "정원행 발송대"
                },
                optionalRooms = new List<string>
                {
                    "비구름 특급 통로", "젖은 주소 복구실", "거대 새 둥지 오배송실",
                    "반송 불가 보관소", "우체국장 별비의 기록실"
                }
            };
            if (useGateLoopV02)
            {
                definition.gateRoutes.Add(new GateRouteDefinition
                {
                    id = "CH4_ROUTE_REGULAR_POST",
                    displayName = "A 안전·추론 · 정규 우편 분류",
                    archetype = GateRouteArchetype.Cooperation,
                    contributionId = "CH4_REGULAR_ADDRESS_FRAGMENT",
                    contributionDisplayName = "정규 주소 조각"
                });
                definition.gateRoutes.Add(new GateRouteDefinition
                {
                    id = "CH4_ROUTE_DEAD_LETTER",
                    displayName = "B 위험·배송 · 반송 불가 주소",
                    archetype = GateRouteArchetype.Exploration,
                    contributionId = "CH4_DEAD_ADDRESS_FRAGMENT",
                    contributionDisplayName = "폐기 주소 조각"
                });
                definition.gateRoutes.Add(new GateRouteDefinition
                {
                    id = "CH4_ROUTE_SEALED_LETTER",
                    displayName = "C 빠름·사생활 · 봉인 편지 인장",
                    archetype = GateRouteArchetype.Appropriation,
                    contributionId = "CH4_SEALED_ADDRESS_IMPRINT",
                    contributionDisplayName = "봉인 주소 인장"
                });
            }

            run.BeginChapter(definition);
            if (useGateLoopV02)
            {
                run.ChapterLoop.EnterRuleIntro();
                run.ChapterLoop.OpenRoutes();
            }

            if (run.GetFlag("CH3_DROUGHT"))
            {
                run.SetFlag("CH4_DRY_INK");
            }
            if (run.GetFlag("CH3_STORM_LEFT_UNREPAIRED"))
            {
                run.SetFlag("CH4_WET_INK");
            }
            if (run.GetFlag("CH3_GURU_CHOSE_RETURN"))
            {
                run.SetFlag("CH4_RAIN_SHORTCUT");
            }
            if (run.GetFlag("CH3_RAIN_SYSTEM_REBUILT"))
            {
                run.SetFlag("CH4_CLOUD_STAMP_AVAILABLE");
            }

            float inheritedScent = run.ConsequenceResolver.GetStartingScent(StarChapterId.StarPostOffice);
            if (inheritedScent > 0f)
            {
                run.Chapter.AddScent(inheritedScent, "목장에서 남긴 날씨가 우편 잉크에 스며들었다", "Chapter3");
            }
            else if (inheritedScent < 0f)
            {
                run.Chapter.AddScent(inheritedScent, "구루의 비구름 지름길이 냄새를 씻어 냈다", "Chapter3");
            }
            run.SetFlag("STARPATH_LAST_LETTER_EXISTS");
        }

        private IEnumerator Start()
        {
            yield return null;
            if (run.GetFlag("CH4_DRY_INK"))
            {
                StarNightHUD.Instance?.Toast("가뭄 때문에 별 도장 잉크가 말랐다. 첫 주소는 한 번 더 눌러야 한다.", 6f);
            }
            else if (run.GetFlag("CH4_WET_INK"))
            {
                StarNightHUD.Instance?.Toast("목장의 폭풍에 주소표가 젖었다. 첫 배송은 다른 우체통으로 번질 수 있다.", 6f);
            }
            else if (run.GetFlag("CH4_RAIN_SHORTCUT"))
            {
                StarNightHUD.Instance?.Toast("구루가 만든 비구름 특급 통로가 분실 우편 보관소까지 이어진다.", 6f);
            }
            else
            {
                StarNightHUD.Instance?.Toast(useGateLoopV02
                    ? "세 주소 경로 중 두 조각을 복구하자. 편지의 내용은 메인 목표가 아니며, 깊은 진실은 별문 가동 뒤 선택이다."
                    : "소포에 E, 목적지 우체통에 E. 주소가 정해지면 별빛으로 배송된다.", 6f);
            }
        }
    }
}
