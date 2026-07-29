using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class GardenRestBench : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField, Min(1)] private int waitsRequired = 3;

        public string Prompt
        {
            get
            {
                StarNightRunState run = StarNightRunState.Instance;
                int waited = run != null ? run.GetCounter("garden.wait_moments") : 0;
                return $"해오름을 깨우지 않고 정원 한 구역 기다리기 ({waited}/{waitsRequired})";
            }
        }

        public void Configure(int requiredMoments = 3)
        {
            waitsRequired = Mathf.Max(1, requiredMoments);
        }

        public void Interact(StarNightPlayerAgent player)
        {
            StarNightRunState run = StarNightRunState.Ensure();
            if (run.GetFlag("CH5_SUN_AWAKENED_FORCEFULLY"))
            {
                StarNightHUD.Instance?.Toast("해오름은 이미 억지로 깨어났다. 기다릴 밤이 남아 있지 않다.");
                return;
            }
            if (run.GetFlag("CH5_WAITED_FOR_SUN"))
            {
                StarNightHUD.Instance?.Toast("해오름이 스스로 눈을 떴다. 따뜻하지만 과하지 않은 빛이다.");
                return;
            }

            int moment = run.AddCounter("garden.wait_moments");
            foreach (SunGrowthState growth in FindObjectsByType<SunGrowthState>(FindObjectsSortMode.None))
            {
                growth.AdvanceNaturalGrowth();
            }
            run.Heat.AddHeat(8f, "기다리는 동안 덩굴과 잠든 생물도 함께 자람", "GardenWait");
            float scent = run.ConsequenceResolver.ModifyScent(5f);
            run.Chapter.AddScent(scent, "정원에서 시간을 보내며 별냄새가 오래 머물렀다", "GardenWait");
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.GardenWaited,
                actorId = "Player",
                targetId = moment.ToString(),
                detail = $"해오름을 깨우지 않고 기다렸다. 그동안 정원의 생물도 {moment}단계 자랐다",
                scentDelta = scent,
                witnessed = true
            });

            if (moment < waitsRequired)
            {
                StarNightHUD.Instance?.Toast(
                    $"기다림 {moment}/{waitsRequired} · 해오름의 숨은 고르지만 덩굴과 적도 자랐다.", 4f);
                return;
            }

            run.SetFlag("CH5_WAITED_FOR_SUN");
            run.SetFlag("CH5_HAOREUM_NATURAL_WAKE");
            run.SetNpcState("Haoreum", StarNpcState.Autonomous);
            run.SunSeeds.AddCharges(3);
            run.Heat.AddHeat(-12f, "완전히 쉰 해오름이 부드러운 빛을 나눔", "Haoreum");
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.HaoreumNaturalAwake,
                actorId = "Haoreum",
                targetId = "StarPathTree",
                detail = "충분히 쉰 해오름이 스스로 깨어 별길 나무에 안정된 빛을 나누었다",
                helpedResident = true,
                witnessed = true
            });
            StarNightHUD.Instance?.Toast(
                "해오름이 스스로 눈을 떴다. 저장 햇빛 +3 · 최종 구간에서 안정된 빛을 받을 수 있다.", 6f);
        }
    }
}
