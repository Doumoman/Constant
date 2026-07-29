using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class SunSeedSystem : MonoBehaviour
    {
        private const string ChargeKey = "sunseed.charges";
        private const string RareChargeKey = "sunseed.rare_charges";

        [SerializeField] private int applications;

        public int Charges => StarNightRunState.Instance != null
            ? Mathf.Max(0, StarNightRunState.Instance.GetCounter(ChargeKey))
            : 0;
        public int RareCharges => StarNightRunState.Instance != null
            ? Mathf.Max(0, StarNightRunState.Instance.GetCounter(RareChargeKey))
            : 0;
        public int Applications => applications;

        public void AddCharges(int amount, bool rare = false)
        {
            if (amount <= 0)
            {
                return;
            }
            StarNightRunState.Ensure().AddCounter(rare ? RareChargeKey : ChargeKey, amount);
        }

        public bool ConsumeCharge(bool rare = false)
        {
            StarNightRunState run = StarNightRunState.Ensure();
            string key = rare ? RareChargeKey : ChargeKey;
            if (run.GetCounter(key) <= 0)
            {
                return false;
            }
            run.AddCounter(key, -1);
            return true;
        }

        public FableToolResult Use(FableObject target, string actorId = "Player")
        {
            if (target == null)
            {
                return FableToolResult.Fail("햇빛 씨앗을 심을 대상을 바라보세요.");
            }
            if (!target.Accepts(FableVerb.Awaken))
            {
                return FableToolResult.Fail($"{target.DisplayName}은 빛을 품을 수 없다.");
            }
            if (Charges <= 0)
            {
                return FableToolResult.Fail("심을 수 있는 저장 햇빛이 없다. 정원의 작은 햇빛을 모아야 한다.");
            }

            MaruSunTarget maruTarget = target.GetComponent<MaruSunTarget>();
            SunGrowthState growth = target.GetComponent<SunGrowthState>();
            if (maruTarget == null && growth == null)
            {
                return FableToolResult.Fail($"{target.DisplayName} 안에는 깨어날 생명이나 장치가 없다.");
            }
            ConsumeCharge();
            applications++;

            StarNightRunState run = StarNightRunState.Ensure();
            if (maruTarget != null)
            {
                maruTarget.Blind();
                float maruScent = run.ConsequenceResolver.ModifyScent(20f);
                run.Chapter.AddScent(maruScent, "마루의 눈앞에서 햇빛 씨앗이 터졌다", target.ObjectId);
                run.Heat.AddHeat(18f, "마루를 향해 씨앗을 터뜨림", target.ObjectId);
                run.SetFlag("CH5_MARU_BLINDED");
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.MaruBlinded,
                    actorId = actorId,
                    targetId = target.ObjectId,
                    tool = FableVerb.Awaken,
                    detail = "마루의 코앞에 햇빛 씨앗을 붙여 잠시 눈을 멀게 했다",
                    scentDelta = maruScent,
                    witnessed = true
                });
                return new FableToolResult
                {
                    success = true,
                    growthChanged = true,
                    scentAdded = maruScent,
                    sentence = "햇빛이 터졌다. 마루가 눈을 감고 멈췄지만 정원 전체가 냄새를 맡았다."
                };
            }

            SunGrowthResult result = growth.ApplySunlight();
            float rawHeat = growth.Kind switch
            {
                SunGrowthKind.SleepingCreature => 16f,
                SunGrowthKind.StarPathTree => 13f,
                SunGrowthKind.CoolingBloom => 5f,
                _ => target.HasTrait(FableTraits.Flammable) ? 17f : 10f
            };
            run.Heat.AddHeat(rawHeat, $"{growth.DisplayName}에 햇빛 씨앗을 심음", target.ObjectId);
            if (growth.Kind == SunGrowthKind.CoolingBloom &&
                growth.Stage >= SunGrowthStage.Blooming &&
                run.Heat.Heat > 0f)
            {
                run.Heat.AddHeat(-22f, "그늘꽃이 넓게 피어 빛을 나눔", target.ObjectId);
            }

            float scent = run.ConsequenceResolver.ModifyScent(
                result.overloaded ? 17f : 9f);
            run.Chapter.AddScent(scent, $"{growth.DisplayName}이 빛을 받아 {growth.Stage} 상태가 됐다",
                target.ObjectId);
            StarActionRecord record = run.Actions.Record(new StarActionContext
            {
                actionType = result.overloaded ? StarActionType.GrowthOverloaded : StarActionType.SunlightApplied,
                actorId = actorId,
                targetId = target.ObjectId,
                tool = FableVerb.Awaken,
                detail = result.stage switch
                {
                    SunGrowthStage.Burned => $"{growth.DisplayName}에 빛을 너무 많이 주어 말려 태웠다",
                    SunGrowthStage.Overgrown => $"{growth.DisplayName}이 통로를 덮을 만큼 과성장했다",
                    SunGrowthStage.Blooming => $"{growth.DisplayName}이 충분히 깨어 꽃과 길을 만들었다",
                    _ => $"{growth.DisplayName} 안의 생명과 장치를 깨웠다"
                },
                scentDelta = scent,
                causedAccident = result.overloaded,
                witnessed = true
            });
            if (result.overloaded)
            {
                run.AccidentReport.Add(growth.DisplayName, "햇빛을 너무 많이 받아",
                    result.stage == SunGrowthStage.Burned ? "말라붙어 불씨를 남겼다" : "퇴로까지 자라났다",
                    record?.sequence ?? 0);
            }

            return new FableToolResult
            {
                success = true,
                growthChanged = true,
                overloaded = result.overloaded,
                scentAdded = scent,
                sentence = result.stage switch
                {
                    SunGrowthStage.Burned => $"{growth.DisplayName}이 빛을 견디지 못하고 타 버렸다!",
                    SunGrowthStage.Overgrown => $"{growth.DisplayName}이 정원 구조를 삼킬 만큼 자랐다.",
                    SunGrowthStage.Blooming => $"{growth.DisplayName}이 활짝 깨어 길을 만들었다.",
                    _ => $"{growth.DisplayName} 안의 잠든 빛이 눈을 떴다."
                }
            };
        }

        public string Preview(FableObject target)
        {
            if (target == null)
            {
                return $"햇빛 대상 선택 · 저장 햇빛 {Charges}";
            }
            SunGrowthState growth = target.GetComponent<SunGrowthState>();
            if (target.GetComponent<MaruSunTarget>() != null)
            {
                return $"마루 눈부시기 · 저장 햇빛 {Charges}";
            }
            return growth != null
                ? $"{growth.DisplayName} 깨우기 ({growth.Stage}, 빛 {growth.LightExposure}) · 씨앗 {Charges}"
                : $"{target.DisplayName}: 빛 반응 없음";
        }

        public void ResetForChapter()
        {
            applications = 0;
            StarNightRunState run = StarNightRunState.Instance;
            if (run == null)
            {
                return;
            }
            run.AddCounter(ChargeKey, -run.GetCounter(ChargeKey));
            run.AddCounter(RareChargeKey, -run.GetCounter(RareChargeKey));
        }
    }
}
