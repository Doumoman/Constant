using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StarFetchingNight
{
    public enum PolarisFinalePhase
    {
        Locked,
        RecordCorridor,
        Observatory,
        Pursuit,
        FinalChoice,
        Complete
    }

    public enum PolarisEndingType
    {
        None,
        PathCutter,
        NewLeash,
        ClosedUniverse,
        StarRoad
    }

    [DisallowMultipleComponent]
    public sealed class PolarisFinaleState : MonoBehaviour
    {
        private static readonly FableVerb[] RestorationOrder =
        {
            FableVerb.Resize,
            FableVerb.Link,
            FableVerb.Float,
            FableVerb.Deliver,
            FableVerb.Awaken
        };

        [SerializeField] private PolarisFinalePhase phase = PolarisFinalePhase.Locked;
        [SerializeField] private PolarisEndingType ending;
        [SerializeField] private int recordMask;
        [SerializeField] private int recordCount;
        [SerializeField] private int restorationStep;
        [SerializeField] private float pursuitDuration = 150f;
        [SerializeField] private float timeRemaining = 150f;
        [SerializeField] private bool countdownActive;
        [SerializeField] private FableVerb counterVerb = FableVerb.Resize;

        private StarNightRunState run;

        public PolarisFinalePhase Phase => phase;
        public PolarisEndingType Ending => ending;
        public int RecordCount => recordCount;
        public int RestorationStep => restorationStep;
        public int RestorationRequired => RestorationOrder.Length;
        public float PursuitDuration => pursuitDuration;
        public float TimeRemaining => timeRemaining;
        public bool CountdownActive => countdownActive;
        public FableVerb CounterVerb => counterVerb;
        public FableVerb ExpectedVerb =>
            restorationStep < RestorationOrder.Length ? RestorationOrder[restorationStep] : FableVerb.Awaken;
        public bool AccessGranted => run != null && run.RouteMap.RestoredGateCount >= RunRouteMap.GateCount;
        public bool StarRoadAvailable => run != null && HasStarRoadMemory(run) &&
                                         HasStarRoadConnection(run) &&
                                         HasStarRoadDelivery(run) &&
                                         HasStarRoadLight(run);

        public event Action Changed;
        public event Action<PolarisEndingType> EndingResolved;

        public void Begin(StarNightRunState targetRun)
        {
            run = targetRun;
            recordMask = 0;
            recordCount = 0;
            restorationStep = 0;
            ending = PolarisEndingType.None;
            countdownActive = false;
            counterVerb = CalculateCounterVerb(run);
            pursuitDuration = CalculatePursuitSeconds(run);
            timeRemaining = pursuitDuration;
            phase = AccessGranted ? PolarisFinalePhase.RecordCorridor : PolarisFinalePhase.Locked;
            run.SetFlag("POLARIS_ACCESS_GRANTED", AccessGranted);
            Changed?.Invoke();
        }

        private void Update()
        {
            if (countdownActive)
            {
                AdvanceTime(Time.deltaTime);
            }
        }

        public bool RegisterRecord(StarChapterId chapter)
        {
            if (phase != PolarisFinalePhase.RecordCorridor)
            {
                return false;
            }

            int index = RunRouteMap.GetGateIndex(chapter);
            if (index < 0)
            {
                return false;
            }

            int bit = 1 << index;
            if ((recordMask & bit) != 0)
            {
                return false;
            }

            recordMask |= bit;
            recordCount++;
            run.SetFlag($"POLARIS_RECORD_{chapter}_SEEN");
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.PolarisRecordReplayed,
                actorId = "RaniConstellation",
                targetId = chapter.ToString(),
                detail = GetRepresentativeAction(chapter),
                witnessed = true
            });

            if (recordCount >= RunRouteMap.GateCount)
            {
                phase = PolarisFinalePhase.Observatory;
                run.SetFlag("POLARIS_ALL_RECORDS_SEEN");
            }
            Changed?.Invoke();
            return true;
        }

        public bool InspectObservatory()
        {
            if (phase != PolarisFinalePhase.Observatory)
            {
                return false;
            }

            phase = PolarisFinalePhase.Pursuit;
            countdownActive = true;
            run.SetFlag("POLARIS_TRUTH_SEEN");
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.PolarisTruthRevealed,
                actorId = "Rani",
                targetId = "OriginalReturnCommand",
                detail = "라니가 ‘떠난 아이들을 모두 집으로 데려와’라는 최초 명령과 마루의 오해를 인정했다.",
                witnessed = true
            });
            Changed?.Invoke();
            return true;
        }

        public bool TryRestore(FableVerb verb)
        {
            if (phase != PolarisFinalePhase.Pursuit || restorationStep >= RestorationOrder.Length ||
                RestorationOrder[restorationStep] != verb)
            {
                return false;
            }

            string detail = RestorationDetail(verb);
            restorationStep++;
            run.SetFlag($"POLARIS_TOOL_{verb}_RESTORED");
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.PolarisToolRestored,
                actorId = "Player",
                targetId = $"PolarisNode{restorationStep}",
                tool = verb,
                detail = detail,
                witnessed = true
            });

            if (restorationStep >= RestorationOrder.Length)
            {
                countdownActive = false;
                phase = PolarisFinalePhase.FinalChoice;
                run.SetFlag("POLARIS_CENTER_STAR_REACHED");
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.PolarisCenterReached,
                    actorId = "Player",
                    targetId = "PolarisCore",
                    detail = "마루보다 먼저 중심별에 도달해 우주의 길을 결정할 시간을 확보했다.",
                    witnessed = true
                });
            }
            Changed?.Invoke();
            return true;
        }

        public void AdvanceTime(float seconds)
        {
            if (!countdownActive || seconds <= 0f)
            {
                return;
            }

            timeRemaining = Mathf.Max(0f, timeRemaining - seconds);
            if (timeRemaining <= 0f)
            {
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.MaruReachedPolaris,
                    actorId = "Maru",
                    targetId = "PolarisCore",
                    detail = "마루가 먼저 중심별을 물어 모든 항로를 집으로 접었다.",
                    witnessed = true
                });
                ResolveEnding(PolarisEndingType.ClosedUniverse, true);
                return;
            }
            Changed?.Invoke();
        }

        public bool TryChooseEnding(PolarisEndingType choice)
        {
            return ResolveEnding(choice, false);
        }

        private bool ResolveEnding(PolarisEndingType choice, bool forced)
        {
            if (ending != PolarisEndingType.None)
            {
                return false;
            }
            if (!forced && phase != PolarisFinalePhase.FinalChoice)
            {
                return false;
            }
            if (choice == PolarisEndingType.None)
            {
                return false;
            }
            if (choice == PolarisEndingType.StarRoad && !StarRoadAvailable)
            {
                return false;
            }
            if (choice == PolarisEndingType.NewLeash && !run.IsToolUnlocked(FableVerb.Link))
            {
                return false;
            }

            ending = choice;
            countdownActive = false;
            phase = PolarisFinalePhase.Complete;
            run.SetFlag($"ENDING_{choice.ToString().ToUpperInvariant()}");
            run.SetFlag("chapter.PolarisObservatory.completed");
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.PolarisEndingChosen,
                actorId = "Player",
                targetId = choice.ToString(),
                detail = EndingDecisionDetail(choice),
                witnessed = true
            });

            if (choice == PolarisEndingType.StarRoad)
            {
                run.SetFlag("POLARIS_RANI_DELIVERED");
                run.SetFlag("POLARIS_MARU_RELEASED");
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.RaniCommandWithdrawn,
                    actorId = "Rani",
                    targetId = "Maru",
                    detail = "라니가 전장으로 배송되어 ‘이제 돌아오지 않아도 돼’라고 직접 명령을 거두었다.",
                    witnessed = true
                });
            }

            run.EndRun(StarRunEndReason.JourneyComplete);
            Changed?.Invoke();
            EndingResolved?.Invoke(choice);
            return true;
        }

        public string GetRepresentativeAction(StarChapterId chapter)
        {
            StarActionRecord record = run?.Actions.SelectForRani(1, chapter).FirstOrDefault();
            return record != null && !string.IsNullOrWhiteSpace(record.detail)
                ? record.detail
                : $"{RunRouteMap.GetStationName(RunRouteMap.GetGateIndex(chapter))}에서 남긴 선택은 지워지지 않았다.";
        }

        public string BuildEvaluationAndRebuttal()
        {
            string evaluation = run != null && run.Actions.Records.Any(record => record.causedAccident)
                ? "라니 · 당신은 자주 사고를 냈고, 그래서 누군가 길을 정해 줘야 한다고 생각했습니다."
                : "라니 · 당신은 많은 것을 돌려보냈지만, 떠나는 선택까지 안전하다고는 말할 수 없습니다.";
            return evaluation + "\n" +
                   "나 · 당신이 왜 붙잡았는지는 이해해요.\n" +
                   "나 · 하지만 이해한다고 계속 붙잡게 둘 수는 없어요.\n" +
                   "나 · 놓아주는 말은 당신이 직접 해야 해요.";
        }

        public string BuildObjectiveText()
        {
            return phase switch
            {
                PolarisFinalePhase.Locked =>
                    $"관측소 진입 봉인 · 복구 별문 {run?.RouteMap.RestoredGateCount ?? 0}/{RunRouteMap.GateCount}",
                PolarisFinalePhase.RecordCorridor =>
                    $"기록 회랑 · 다섯 정거장의 대표 행동 확인 {recordCount}/{RunRouteMap.GateCount}",
                PolarisFinalePhase.Observatory =>
                    "닫힌 관측실 · 라니와 마루의 최초 임무 확인",
                PolarisFinalePhase.Pursuit =>
                    $"중심별 추격 · {VerbDisplayName(ExpectedVerb)} 사용 · {restorationStep}/{RestorationOrder.Length}",
                PolarisFinalePhase.FinalChoice =>
                    "중심별 선점 성공 · 우주의 길을 실제 행동으로 결정",
                PolarisFinalePhase.Complete =>
                    $"여행 완료 · {EndingTitle(ending)}",
                _ => "북극성 관측소"
            };
        }

        public string BuildStarRoadRequirements()
        {
            if (run == null)
            {
                return "별길 조건을 읽을 수 없다.";
            }
            return $"기억 {(HasStarRoadMemory(run) ? "◆" : "◇")}  " +
                   $"연결 {(HasStarRoadConnection(run) ? "◆" : "◇")}  " +
                   $"배송 {(HasStarRoadDelivery(run) ? "◆" : "◇")}  " +
                   $"빛 {(HasStarRoadLight(run) ? "◆" : "◇")}";
        }

        public static float CalculatePursuitSeconds(StarNightRunState targetRun)
        {
            float seconds = 150f;
            if (targetRun == null)
            {
                return seconds;
            }
            if (targetRun.GetFlag("CH5_HAOREUM_NATURAL_WAKE")) seconds += 15f;
            if (targetRun.GetFlag("CH5_GARDEN_RESTORED")) seconds += 15f;
            if (targetRun.GetFlag("CH5_FINAL_LIGHT_SUPPORT")) seconds += 20f;
            if (targetRun.GetFlag("CH5_STAR_PATH_TREE_STABLE")) seconds += 15f;
            if (targetRun.GetFlag("CH5_SUN_AWAKENED_FORCEFULLY")) seconds -= 15f;
            if (targetRun.GetFlag("CH5_GARDEN_FIRE") && !targetRun.GetFlag("CH5_GARDEN_RESTORED")) seconds -= 20f;
            if (targetRun.GetFlag("CH5_STAR_PATH_TREE_OVERGROWN")) seconds -= 12f;
            if (targetRun.GetFlag("CH5_STAR_PATH_TREE_BURNED")) seconds -= 25f;
            return Mathf.Clamp(seconds, 75f, 215f);
        }

        public static bool HasStarRoadMemory(StarNightRunState targetRun)
        {
            bool letterMemory = !targetRun.GetFlag("STARPATH_LETTER_DESTROYED") &&
                                (targetRun.GetFlag("STARPATH_LETTER_PRESERVED") ||
                                 targetRun.GetFlag("STARPATH_LETTER_CONTENT_KNOWN") ||
                                 targetRun.GetFlag("STARPATH_LAST_LETTER_DELIVERED") ||
                                 targetRun.GetFlag("CH4_LETTER_STATE_COPIED"));
            bool restoredMemory = targetRun.GetFlag("STARPATH_RANI_COMMAND_CONTEXT_KNOWN") &&
                                  targetRun.GetFlag("CH5_RANI_PRESERVED_POT_FOUND");
            return letterMemory || restoredMemory;
        }

        public static bool HasStarRoadConnection(StarNightRunState targetRun) =>
            targetRun.IsToolUnlocked(FableVerb.Link) &&
            targetRun.GetFlag("STARPATH_MARU_ORIGINAL_COMMAND_KNOWN");

        public static bool HasStarRoadDelivery(StarNightRunState targetRun) =>
            targetRun.IsToolUnlocked(FableVerb.Deliver) &&
            targetRun.GetFlag("STARPATH_RANI_CAN_BE_DELIVERED") &&
            targetRun.GetFlag("STARPATH_POLARIS_ROUTE_REGISTERED");

        public static bool HasStarRoadLight(StarNightRunState targetRun) =>
            targetRun.GetFlag("CH5_FINAL_LIGHT_SUPPORT");

        public static string EndingTitle(PolarisEndingType value) => value switch
        {
            PolarisEndingType.PathCutter => "길을 끊는 사람",
            PolarisEndingType.NewLeash => "새 목줄",
            PolarisEndingType.ClosedUniverse => "닫힌 우주",
            PolarisEndingType.StarRoad => "별길",
            _ => "아직 선택되지 않은 길"
        };

        public static string EndingBody(PolarisEndingType value) => value switch
        {
            PolarisEndingType.PathCutter =>
                "별지기의 가위가 마루와 중심별의 연결을 잘랐다.\n항로는 돌아왔지만 마루는 사라졌고, 라니는 자신의 명령을 거두지 않았다.",
            PolarisEndingType.NewLeash =>
                "붉은 실이 마루의 목줄을 아이에게 연결했다.\n우주는 안전해졌지만, 이제 떠날 수 있는 길을 아이가 결정한다.",
            PolarisEndingType.ClosedUniverse =>
                "마루가 중심별을 집으로 돌려보냈다.\n아무도 길을 잃지 않지만, 누구도 다시 떠날 수 없다.",
            PolarisEndingType.StarRoad =>
                "편지가 주소를 되찾고, 붉은 실이 두 공간을 잇고, 우편 도장이 라니를 전장으로 보냈다.\n" +
                "햇빛이 무너지는 별을 붙드는 동안 라니는 마루에게 직접 말했다.\n" +
                "“이제 돌아오지 않아도 돼.”\n길은 떠나는 사람과 돌아오는 사람 모두에게 남았다.",
            _ => string.Empty
        };

        private static FableVerb CalculateCounterVerb(StarNightRunState targetRun)
        {
            if (targetRun == null)
            {
                return FableVerb.Resize;
            }
            Dictionary<FableVerb, int> counts = new();
            foreach (StarActionRecord record in targetRun.Actions.Records)
            {
                if (record.actionType != StarActionType.ToolApplied &&
                    record.actionType != StarActionType.ToolOverloaded)
                {
                    continue;
                }
                counts.TryGetValue(record.tool, out int count);
                counts[record.tool] = count + 1;
            }
            return counts.Count == 0
                ? FableVerb.Resize
                : counts.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key).First().Key;
        }

        private static string RestorationDetail(FableVerb verb) => verb switch
        {
            FableVerb.Resize => "절구로 깨진 중심별 조각을 원래 크기로 복원했다.",
            FableVerb.Link => "붉은 실로 흩어진 별자리를 하나의 항로로 연결했다.",
            FableVerb.Float => "구름병으로 무거워진 별을 다시 하늘에 띄웠다.",
            FableVerb.Deliver => "별 우편 도장으로 길 잃은 별을 원래 행성에 배송했다.",
            FableVerb.Awaken => "햇빛 씨앗으로 식어 가는 중심별을 다시 점화했다.",
            _ => "별길을 복원했다."
        };

        private static string EndingDecisionDetail(PolarisEndingType value) => value switch
        {
            PolarisEndingType.PathCutter => "마루를 없애 항로를 되찾는 길을 선택했다.",
            PolarisEndingType.NewLeash => "마루의 명령권을 자신에게 연결해 여행을 통제하는 길을 선택했다.",
            PolarisEndingType.ClosedUniverse => "중심별을 마루에게 돌려보내 모든 여행을 끝냈다.",
            PolarisEndingType.StarRoad => "라니가 직접 명령을 거두게 해 마루와 별길을 함께 놓아주었다.",
            _ => string.Empty
        };

        public static string VerbDisplayName(FableVerb verb) => verb switch
        {
            FableVerb.Resize => "달토끼의 절구",
            FableVerb.Link => "까치의 붉은 실",
            FableVerb.Float => "구름병",
            FableVerb.Deliver => "별 우편 도장",
            FableVerb.Awaken => "햇빛 씨앗",
            _ => verb.ToString()
        };
    }
}
