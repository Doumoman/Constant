using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class StarNightActionRecorder : MonoBehaviour
    {
        [SerializeField] private int capacity = 160;
        private readonly List<StarActionRecord> records = new();
        private int sequence;

        public IReadOnlyList<StarActionRecord> Records => records;
        public int LatestSequence => sequence;
        public event Action<StarActionRecord> Recorded;

        public StarActionRecord Record(StarActionContext context)
        {
            if (context == null)
            {
                return null;
            }

            StarActionRecord record = new()
            {
                sequence = ++sequence,
                time = Time.timeSinceLevelLoad,
                actionType = context.actionType,
                actorId = context.actorId,
                targetId = context.targetId,
                routeId = context.routeId,
                tool = context.tool,
                chapter = StarNightRunState.Instance != null ? StarNightRunState.Instance.CurrentChapter : StarChapterId.Prologue,
                detail = context.detail,
                scentDelta = context.scentDelta,
                causedAccident = context.causedAccident,
                helpedResident = context.helpedResident,
                witnessed = context.witnessed,
                gateContributions = context.gateContributions,
                gateReady = context.gateReady,
                gateActivated = context.gateActivated,
                bellPhase = context.bellPhase
            };
            records.Add(record);
            if (records.Count > Mathf.Max(20, capacity))
            {
                records.RemoveAt(0);
            }

            Recorded?.Invoke(record);
            return record;
        }

        public List<StarActionRecord> SelectForRani(int count, StarChapterId? chapter = null)
        {
            return records
                .Where(record => !chapter.HasValue || record.chapter == chapter.Value)
                .OrderByDescending(record => record.BasePriority)
                .ThenByDescending(record => record.sequence)
                .Take(Mathf.Max(0, count))
                .OrderBy(record => record.sequence)
                .ToList();
        }

        public void Clear()
        {
            records.Clear();
            sequence = 0;
        }
    }

    [DisallowMultipleComponent]
    public sealed class StarNightAccidentReportBuilder : MonoBehaviour
    {
        private readonly List<AccidentStep> steps = new();
        public IReadOnlyList<AccidentStep> Steps => steps;

        public void Add(string subject, string verb, string result, int actionSequence = 0)
        {
            StarNightChapterState chapter = StarNightRunState.Instance?.Chapter;
            steps.Add(new AccidentStep
            {
                subject = subject,
                verb = verb,
                result = result,
                actionSequence = actionSequence,
                time = Time.timeSinceLevelLoad,
                gateActivated = chapter?.GateActivated == true,
                bellPhase = chapter != null ? (int)chapter.BellPhase : 0
            });
            if (steps.Count > 8)
            {
                steps.RemoveAt(0);
            }
        }

        public string BuildReport()
        {
            if (steps.Count == 0)
            {
                return "아직 설명할 만한 사고는 없었다.";
            }

            StringBuilder builder = new();
            builder.AppendLine("오늘 밤의 사고 기록");
            for (int i = 0; i < steps.Count; i++)
            {
                AccidentStep step = steps[i];
                if (step.gateActivated || step.bellPhase > 0)
                {
                    builder.Append("[별문 가동 · 방울 ")
                        .Append(Mathf.Max(1, step.bellPhase))
                        .Append("] ");
                }
                builder.Append(i + 1).Append(". ").Append(step.subject).Append(' ')
                    .Append(step.verb).Append(' ').Append(step.result);
                if (i < steps.Count - 1)
                {
                    builder.AppendLine(" 때문에");
                }
            }

            return builder.ToString();
        }

        public void Clear() => steps.Clear();
    }

    [DisallowMultipleComponent]
    public sealed class StarNightWatcherResolver : MonoBehaviour
    {
        public string ResolveRaniSummary(StarChapterId? chapter = null)
        {
            StarNightRunState run = StarNightRunState.Instance;
            if (run == null)
            {
                return "라니는 아직 아무것도 적지 않았다.";
            }

            StarChapterId targetChapter = chapter ?? run.CurrentChapter;
            List<StarActionRecord> chosen = run.Actions.SelectForRani(3, targetChapter);
            if (chosen.Count == 0)
            {
                return "라니의 기록: 조용한 밤이었다. 지나치게 조용했다.";
            }

            StringBuilder builder = new("라니의 기록\n");
            foreach (StarActionRecord record in chosen)
            {
                builder.Append("• ").Append(Interpret(record, targetChapter)).AppendLine();
            }

            if (targetChapter == StarChapterId.MoonRabbitMill)
            {
                if (run.GetFlag("CH1_ROUTE_STORAGE_CONTRIBUTED"))
                {
                    builder.Append("결론: 별문에 필요했던 것은 길떡 두 개였고, 그중 하나로 겨울 식량을 사용했다.");
                }
                else if (run.GetFlag("CH1_STORAGE_CAKE_RETURNED"))
                {
                    builder.Append("결론: 저장 길떡을 빌렸지만, 별문에 장착하기 전에 돌려놓고 다른 길을 택했다.");
                }
                else
                {
                    builder.Append("결론: 주민의 겨울 식량을 별문에 고정하지 않고 두 길을 완성했다.");
                }
            }
            else if (targetChapter == StarChapterId.MagpieBridge)
            {
                builder.Append(run.GetFlag("CH2_HAECHI_FORCED")
                    ? "결론: 떠나지 못하게 한 것은 안전을 위한 책임이었다."
                    : "결론: 떠날 자유를 열어 둔 일은 아직 관찰이 필요하다.");
            }
            else if (targetChapter == StarChapterId.CloudWhaleRanch)
            {
                if (run.GetFlag("CH3_GURU_RELEASED") && !run.GetFlag("CH3_RAIN_SYSTEM_REBUILT"))
                {
                    builder.Append("결론: 밧줄만 보고 감옥이라 단정했고, 비가 멎은 뒤에는 떠났다.");
                }
                else if (run.GetFlag("CH3_RAIN_SYSTEM_REBUILT"))
                {
                    builder.Append("결론: 풀어 준 뒤에도 결과를 남겨 두지 않고 새 비의 길을 만들었다.");
                }
                else
                {
                    builder.Append("결론: 구루가 머문 이유를 먼저 지켜본 것은 드문 인내였다.");
                }
            }
            else if (targetChapter == StarChapterId.StarPostOffice)
            {
                if (run.GetFlag("CH4_LETTER_STATE_DISMANTLED"))
                {
                    builder.Append("결론: 타인의 마지막 말을 가장 빠른 이동 수단으로 바꾸었다.");
                }
                else if (run.GetFlag("CH4_LETTER_STATE_OPENED"))
                {
                    builder.Append("결론: 수신자가 없는 편지라고 해서 읽을 사람이 당신이 되는 것은 아니다.");
                }
                else if (run.GetFlag("CH4_LETTER_STATE_DELIVERED"))
                {
                    builder.Append("결론: 편지는 수신자에게 도착했다. 라니는 더 이상 대답하지 않는다.");
                }
                else
                {
                    builder.Append("결론: 알지 못하는 내용을 이용하지 않고 봉인을 남겨 두었다.");
                }
            }
            else if (targetChapter == StarChapterId.SleepingSunGarden)
            {
                if (run.GetFlag("CH5_SUN_AWAKENED_FORCEFULLY"))
                {
                    builder.Append("결론: 해오름이 깨어날 준비가 되었는지는 묻지 않았다. 출발할 준비만 확인했다.");
                }
                else if (run.GetFlag("CH5_WAITED_FOR_SUN"))
                {
                    builder.Append("결론: 기다림을 골랐고, 그동안 덩굴과 적도 자란다는 비용까지 지켜보았다.");
                }
                else if (run.GetFlag("CH5_GARDEN_RESTORED"))
                {
                    builder.Append("결론: 불을 낸 뒤 출항에 쓸 수 있던 희귀 씨앗을 포기해 정원을 되살렸다.");
                }
                else
                {
                    builder.Append("결론: 빛을 주는 일과 쉬게 두는 일 사이에서 아직 답을 정하지 않았다.");
                }
            }
            else if (run.Chapter.ScentStage >= StarScentStage.Bell)
            {
                builder.Append("결론: 이 아이는 혼자 두기에는 너무 밝다.");
            }
            else
            {
                builder.Append("결론: 아직 돌아올 수 있을 때 지켜봐야 한다.");
            }

            return builder.ToString().TrimEnd();
        }

        private static string Interpret(StarActionRecord record, StarChapterId chapter)
        {
            if (chapter == StarChapterId.MoonRabbitMill)
            {
                if (record.actionType == StarActionType.RouteObjectiveCompleted)
                {
                    return $"{record.targetId} 경로에서 별문에 쓸 길떡을 마련했다.";
                }
                if (record.actionType == StarActionType.GateContributionReturned)
                {
                    return "겨울 저장 길떡을 별문에 고정하기 전에 돌려놓았다.";
                }
                if (record.actionType == StarActionType.GateActivated)
                {
                    return "길떡 두 개를 확인한 뒤 자신의 손으로 별문을 켰다.";
                }
            }
            if (chapter == StarChapterId.MagpieBridge)
            {
                if (record.actionType == StarActionType.NpcForcedReturn)
                {
                    return "해치가 위험한 길로 떠나지 못하게 책임 있게 붙잡았다.";
                }
                if (record.actionType == StarActionType.NpcAllowedChoice)
                {
                    return "어린 까치가 위험한 떠남을 스스로 고르게 두었다.";
                }
                if (record.actionType == StarActionType.OldBridgeCut)
                {
                    return "필요한 물류 다리를 끊어 자신의 길을 짧게 만들었다.";
                }
                if (record.actionType == StarActionType.BridgeAnchorRestored)
                {
                    return "다리를 고쳤지만, 까치가 감당할 장력을 직접 정했다.";
                }
            }
            if (chapter == StarChapterId.CloudWhaleRanch)
            {
                if (record.actionType == StarActionType.CalfReturnedByMaru)
                {
                    return "마루가 길 잃은 새끼 고래를 물어 어미 곁으로 돌려보내는 장면을 보았다.";
                }
                if (record.actionType == StarActionType.GuruReleased)
                {
                    return "구루가 떠나 달라고 하지 않았는데도 밧줄을 보고 감옥이라고 판단했다.";
                }
                if (record.actionType == StarActionType.RainSystemRebuilt)
                {
                    return "고래를 풀어 준 뒤 비가 멎자, 떠나지 않고 새로운 수차를 만들었다.";
                }
                if (record.actionType == StarActionType.GuruAwakened)
                {
                    return "그가 잠들어 있다는 이유로 방울을 세 번 울렸다.";
                }
                if (record.actionType == StarActionType.CloudBottleOverpressured)
                {
                    return "사라지지 않는 무게를 한곳에 몰아 폭풍을 만들었다.";
                }
                if (record.actionType == StarActionType.RainCloudDelivered)
                {
                    return "무게를 다른 곳에 남기는 대가를 받아들이고 비구름을 내렸다.";
                }
            }
            if (chapter == StarChapterId.StarPostOffice)
            {
                if (record.actionType == StarActionType.LetterOpened)
                {
                    return "수신자가 없는 편지를 열고, 그 내용으로 라니를 판단했다.";
                }
                if (record.actionType == StarActionType.LetterDelivered)
                {
                    return "사적인 기억을 진행 수단으로 쓰지 않고 수신자에게 돌려보냈다.";
                }
                if (record.actionType == StarActionType.LetterDismantled)
                {
                    return "마지막 편지의 귀환 주소를 순간이동 코어로 뜯어냈다.";
                }
                if (record.actionType == StarActionType.LetterPreserved)
                {
                    return "편지를 읽지 않았지만 사라지지 않도록 봉인을 지켰다.";
                }
                if (record.actionType == StarActionType.LetterSealCopied)
                {
                    return "편지를 열지 않고 봉인의 북극성 주소만 복사했다.";
                }
                if (record.actionType == StarActionType.LetterSealDamaged)
                {
                    return "항로를 빨리 열기 위해 사적인 편지의 봉인을 별문에 찍어 훼손했다.";
                }
                if (record.actionType == StarActionType.ParcelMisdelivered)
                {
                    return "젖은 주소를 확인하지 않아 남의 소포를 다른 방으로 보냈다.";
                }
                if (record.actionType == StarActionType.SorterOverloaded)
                {
                    return "항로 도장을 빨리 찾으려 자동 분류기 전체를 폭주시켰다.";
                }
            }
            if (chapter == StarChapterId.SleepingSunGarden)
            {
                if (record.actionType == StarActionType.HaoreumForcedAwake)
                {
                    return "해오름이 깨어날 준비가 되었는지 묻지 않고 자신의 출발 시간을 앞당겼다.";
                }
                if (record.actionType == StarActionType.HaoreumNaturalAwake)
                {
                    return "정원이 더 위험해지는 것을 알면서도 해오름이 스스로 깨어날 시간을 남겨 두었다.";
                }
                if (record.actionType == StarActionType.GardenRestored)
                {
                    return "출항에 쓸 희귀 씨앗을 포기해 자신이 과열시킨 정원을 다시 살렸다.";
                }
                if (record.actionType == StarActionType.StarPathOvergrown)
                {
                    return "안정된 항로보다 빠른 지름길을 위해 별길 나무를 더 크게 키웠다.";
                }
                if (record.actionType == StarActionType.StarPathStabilized)
                {
                    return "더 키울 수 있었지만 가지를 잘라 오래 버틸 별길을 남겼다.";
                }
                if (record.actionType == StarActionType.PreservedPotFound)
                {
                    return "라니가 슬픔을 놓지 못해 시간과 꽃을 함께 멈춘 장소를 보았다.";
                }
            }

            if (record.causedAccident)
            {
                return $"{record.targetId}을 위험하게 바꾸었다.";
            }
            if (record.helpedResident)
            {
                return "주민을 도왔지만, 그 도구를 다시 위험한 곳에 가져갔다.";
            }
            if (record.actionType == StarActionType.EnteredTemptationRoom)
            {
                return "떠날 수 있었는데도 더 깊은 방을 열었다.";
            }
            if (record.actionType == StarActionType.DroppedItem)
            {
                return $"{record.targetId}을 뒤에 남겨 두었다.";
            }

            return string.IsNullOrWhiteSpace(record.detail) ? record.actionType.ToString() : record.detail;
        }
    }

    [DisallowMultipleComponent]
    public sealed class StarNightConsequenceResolver : MonoBehaviour
    {
        public void Register(string id, string description, float scentMultiplier = 1f, int chapterOffset = 0,
            StarChapterId sourceChapter = StarChapterId.Prologue, StarChapterId targetChapter = StarChapterId.Prologue)
        {
            StarNightRunState.Instance?.AddConsequence(new ConsequenceModifier
            {
                id = id,
                description = description,
                scentMultiplier = Mathf.Max(0f, scentMultiplier),
                chapterOffset = chapterOffset,
                sourceChapter = sourceChapter,
                targetChapter = targetChapter
            });
        }

        public float ModifyScent(float baseAmount)
        {
            StarNightRunState run = StarNightRunState.Instance;
            if (run == null)
            {
                return baseAmount;
            }

            float result = baseAmount;
            foreach (ConsequenceModifier modifier in run.Consequences)
            {
                if (modifier.targetChapter == StarChapterId.Prologue || modifier.targetChapter == run.CurrentChapter)
                {
                    result *= modifier.scentMultiplier;
                }
            }

            return result;
        }

        public float GetStartingScent(StarChapterId chapter)
        {
            StarNightRunState run = StarNightRunState.Instance;
            if (run == null)
            {
                return 0f;
            }

            return run.Consequences
                .Where(modifier => modifier.targetChapter == chapter)
                .Sum(modifier => modifier.chapterOffset);
        }

        public void ResolveMoonMill()
        {
            StarNightRunState run = StarNightRunState.Instance;
            if (run == null)
            {
                return;
            }

            if (run.GetFlag("moonmill.mill.repaired"))
            {
                run.SetFlag("CH1_MILL_REPAIRED");
                Register("moonmill.support", "달토끼 물류가 까치다리 정거장에 도착했다.", 0.85f, 0,
                    StarChapterId.MoonRabbitMill, StarChapterId.MagpieBridge);
            }
            else
            {
                run.SetFlag("CH1_MILL_DAMAGED");
                Register("moonmill.shortage", "고장 난 방앗간 때문에 정거장 물자가 줄었다.", 1.08f, 5,
                    StarChapterId.MoonRabbitMill, StarChapterId.MagpieBridge);
            }

            if (run.GetFlag("moonmill.temptation.open") && !run.GetFlag("moonmill.temptation.resolved"))
            {
                run.SetFlag("CH1_BACK_STORAGE_UNRESOLVED");
                Register("moonmill.back_storage", "달 뒤편 창고의 냄새가 다음 길까지 따라왔다.", 1.18f, 12,
                    StarChapterId.MoonRabbitMill, StarChapterId.MagpieBridge);
            }

            if (run.GetFlag("CH1_ROUTE_STORAGE_CONTRIBUTED"))
            {
                run.SetFlag("CH1_WINTER_FOOD_USED");
                Register("moonmill.winter_food", "별문에 사용한 저장 길떡만큼 까치다리행 겨울 보급이 줄었다.",
                    1.04f, 3, StarChapterId.MoonRabbitMill, StarChapterId.MagpieBridge);
            }
        }

        public void ResolveMagpieBridge()
        {
            StarNightRunState run = StarNightRunState.Instance;
            if (run == null)
            {
                return;
            }

            if (run.GetFlag("CH2_OLD_BRIDGE_CUT") && !run.GetFlag("CH2_OLD_BRIDGE_RESTORED"))
            {
                Register("magpie.supply_shortage", "끊긴 옛 다리 때문에 구름고래 목장의 물자가 줄었다.", 1.1f, 8,
                    StarChapterId.MagpieBridge, StarChapterId.CloudWhaleRanch);
            }
            if (run.GetFlag("CH2_MAGPIES_FORCED"))
            {
                Register("magpie.fatigue", "지친 까치들이 다음 하늘에서 구조를 돕기 어렵다.", 1.05f, 4,
                    StarChapterId.MagpieBridge, StarChapterId.CloudWhaleRanch);
            }
        }

        public void ResolveCloudWhaleRanch()
        {
            StarNightRunState run = StarNightRunState.Instance;
            if (run == null)
            {
                return;
            }

            if (run.GetFlag("CH3_GURU_RELEASED") && !run.GetFlag("CH3_RAIN_SYSTEM_REBUILT"))
            {
                run.SetFlag("CH3_DROUGHT");
                Register("cloud.drought", "구루가 떠난 뒤 별 우체국의 잉크가 말라붙었다.", 1.12f, 10,
                    StarChapterId.CloudWhaleRanch, StarChapterId.StarPostOffice);
            }
            if (run.GetCounter("CH3_STORM_DAMAGE") > 0 && !run.GetFlag("CH3_DAMAGE_REPAIRED"))
            {
                run.SetFlag("CH3_STORM_LEFT_UNREPAIRED");
                Register("cloud.wet_letters", "방치한 폭풍에 별 우체국의 목적지 표식이 번졌다.", 1.08f, 8,
                    StarChapterId.CloudWhaleRanch, StarChapterId.StarPostOffice);
            }
            if (run.GetFlag("CH3_GURU_CHOSE_RETURN"))
            {
                Register("cloud.rain_shortcut", "구루가 만든 비구름 지름길이 별 우체국까지 이어졌다.", 0.9f, -4,
                    StarChapterId.CloudWhaleRanch, StarChapterId.StarPostOffice);
            }
            if (run.GetFlag("CH3_RAIN_SYSTEM_REBUILT"))
            {
                Register("cloud.stamp", "복구된 목장이 구름 우표를 보내왔다.", 0.92f, 0,
                    StarChapterId.CloudWhaleRanch, StarChapterId.StarPostOffice);
            }
        }

        public void ResolveStarPostOffice()
        {
            StarNightRunState run = StarNightRunState.Instance;
            if (run == null)
            {
                return;
            }

            if (run.GetFlag("CH4_LETTER_STATE_DELIVERED"))
            {
                Register("post.rani_silence", "마지막 편지를 받은 라니가 해님 정원에서 통신을 줄였다.", 1f, 0,
                    StarChapterId.StarPostOffice, StarChapterId.SleepingSunGarden);
            }
            if (run.GetFlag("CH4_LETTER_STATE_OPENED"))
            {
                Register("post.rani_argument", "봉인이 뜯긴 편지를 두고 라니와의 논쟁이 이어진다.", 1.08f, 6,
                    StarChapterId.StarPostOffice, StarChapterId.SleepingSunGarden);
            }
            else if (run.GetFlag("CH4_LETTER_SEAL_DAMAGED"))
            {
                Register("post.rani_seal_argument", "항로에 찍혀 상한 편지 봉인을 두고 라니와의 불신이 남았다.",
                    1.04f, 3, StarChapterId.StarPostOffice, StarChapterId.SleepingSunGarden);
            }
            if (run.GetFlag("CH4_LETTER_STATE_DISMANTLED"))
            {
                Register("post.teleport_core", "편지에서 뜯은 귀환 주소 코어가 강한 순간이동을 제공한다.", 0.95f, 4,
                    StarChapterId.StarPostOffice, StarChapterId.SleepingSunGarden);
            }
            if (run.GetFlag("CH4_LETTER_STATE_LOST_TO_MARU"))
            {
                Register("post.maru_memory", "마루가 삼킨 마지막 편지를 되찾을 위험한 길이 열린다.", 1.15f, 12,
                    StarChapterId.StarPostOffice, StarChapterId.SleepingSunGarden);
            }
            if (run.GetFlag("CH4_LETTER_PRESERVED") && !run.GetFlag("CH4_LETTER_STATE_OPENED"))
            {
                Register("post.sealed_memory", "보존한 봉인이 해님 정원의 별길 단서를 지킨다.", 0.92f, 0,
                    StarChapterId.StarPostOffice, StarChapterId.SleepingSunGarden);
            }
            if (run.GetFlag("CH4_SORTER_OVERLOAD") && !run.GetFlag("CH4_SORTER_REPAIRED"))
            {
                Register("post.sorter_debris", "폭주 분류기의 잘못된 배송물이 해님 정원까지 쏟아진다.", 1.07f, 8,
                    StarChapterId.StarPostOffice, StarChapterId.SleepingSunGarden);
            }
        }

        public void ResolveSleepingSunGarden()
        {
            StarNightRunState run = StarNightRunState.Instance;
            if (run == null)
            {
                return;
            }

            if (run.GetFlag("CH5_HAOREUM_NATURAL_WAKE"))
            {
                Register("garden.stable_sun", "충분히 쉰 해오름이 관측소에 안정 광원과 회복 지점을 만든다.",
                    0.9f, 0, StarChapterId.SleepingSunGarden, StarChapterId.PolarisObservatory);
            }
            if (run.GetFlag("CH5_SUN_AWAKENED_FORCEFULLY"))
            {
                Register("garden.tired_sun", "피곤한 해오름의 광원이 관측소에서 주기적으로 과열된다.",
                    1.12f, 10, StarChapterId.SleepingSunGarden, StarChapterId.PolarisObservatory);
            }
            if (run.GetFlag("CH5_GARDEN_FIRE") && !run.GetFlag("CH5_GARDEN_RESTORED"))
            {
                Register("garden.dark_observatory", "방치한 정원 화재가 관측소 기록 일부를 태워 어두운 지름길을 남긴다.",
                    1.08f, 8, StarChapterId.SleepingSunGarden, StarChapterId.PolarisObservatory);
            }
            if (run.GetFlag("CH5_GARDEN_RESTORED"))
            {
                Register("garden.restored_pot", "되살아난 정원의 빛이 라니 동생의 화분 연출을 관측소까지 잇는다.",
                    0.92f, 0, StarChapterId.SleepingSunGarden, StarChapterId.PolarisObservatory);
            }
            if (run.GetFlag("CH5_STAR_PATH_TREE_STABLE"))
            {
                Register("garden.stable_route", "다듬은 별길 나무가 관측소까지 흔들리지 않는 항로를 만든다.",
                    0.95f, 0, StarChapterId.SleepingSunGarden, StarChapterId.PolarisObservatory);
            }
            if (run.GetFlag("CH5_STAR_PATH_TREE_OVERGROWN"))
            {
                Register("garden.overgrown_route", "과성장한 별길이 빠르지만 불안정한 관측소 진입로를 만든다.",
                    1.07f, 4, StarChapterId.SleepingSunGarden, StarChapterId.PolarisObservatory);
            }
            if (run.GetFlag("CH5_STAR_PATH_TREE_BURNED"))
            {
                Register("garden.burned_route", "타 버린 별길 가지 때문에 관측소 항로가 어둡고 끊겨 있다.",
                    1.15f, 12, StarChapterId.SleepingSunGarden, StarChapterId.PolarisObservatory);
            }
        }
    }
}
