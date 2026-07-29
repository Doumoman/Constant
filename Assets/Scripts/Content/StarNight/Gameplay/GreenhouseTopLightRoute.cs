using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class GreenhouseTopLightRoute : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private GateRouteObjective routeObjective;
        [SerializeField, Min(1)] private int requiredReflections = 2;
        [SerializeField] private int reflected;
        [SerializeField] private bool recovered;
        [SerializeField] private SunGrowthState overgrowthPlant;
        [SerializeField] private SunGrowthState awakenedCreature;
        [SerializeField] private GameObject escapeBlocker;

        public int Reflected => reflected;
        public bool Recovered => recovered;
        public string Prompt => recovered
            ? "비어 있는 온실 꼭대기 반사판"
            : $"우산으로 높은 햇빛 반사하기 ({reflected}/{requiredReflections})";

        public void Configure(GateRouteObjective objective, int reflections = 2)
        {
            routeObjective = objective;
            requiredReflections = Mathf.Max(1, reflections);
        }

        public void ConfigureHazards(SunGrowthState plant, SunGrowthState creature,
            GameObject blocker)
        {
            overgrowthPlant = plant;
            awakenedCreature = creature;
            escapeBlocker = blocker;
            if (escapeBlocker != null)
            {
                escapeBlocker.SetActive(false);
            }
        }

        public void Interact(StarNightPlayerAgent player)
        {
            if (recovered)
            {
                StarNightHUD.Instance?.Toast("높은 빛은 이미 길꽃 씨앗에 담겼다.");
                return;
            }

            StarNightRunState run = StarNightRunState.Ensure();
            reflected++;
            float heat = reflected < requiredReflections ? 8f : 18f;
            run.Heat.AddHeat(heat, "온실 꼭대기에서 빛 반사 각도를 맞춤", "GreenhouseTop");
            float scent = run.ConsequenceResolver.ModifyScent(reflected < requiredReflections ? 5f : 12f);
            run.Chapter.AddScent(scent, "온실 유리의 높은 빛이 정원 위로 번졌다", "GreenhouseTop");
            overgrowthPlant?.ApplySunlight(reflected < requiredReflections ? 1 : 2);
            awakenedCreature?.ApplySunlight();
            if (reflected < requiredReflections)
            {
                StarNightHUD.Instance?.Toast(
                    $"첫 반사 {reflected}/{requiredReflections} · 덩굴이 자라 탈출 발판이 좁아진다.");
                return;
            }

            recovered = true;
            run.SunSeeds.AddCharges(1);
            run.SetFlag("CH5_GREENHOUSE_TOP_LIGHT_RECOVERED");
            run.SetFlag("CH5_GREENHOUSE_OVERGROWN");
            run.SetFlag("CH5_GREENHOUSE_CREATURES_AWAKENED");
            run.SetFlag("CH5_GREENHOUSE_ESCAPE_BLOCKED");
            if (escapeBlocker != null)
            {
                escapeBlocker.SetActive(true);
            }
            bool completed = run.Chapter.GateLoopEnabled &&
                             routeObjective != null &&
                             routeObjective.Complete();
            if (completed)
            {
                run.SetFlag("CH5_ROUTE_GREENHOUSE_TOP_COMPLETE");
            }
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.SunlightCollected,
                actorId = "Player",
                targetId = "GreenhouseTop",
                routeId = completed ? routeObjective.RouteId : null,
                detail = "과성장 온실 꼭대기의 반사판을 맞춰 높은 빛과 주머니 해님을 회수했다",
                scentDelta = scent,
                causedAccident = true,
                witnessed = true
            });
            StarNightHUD.Instance?.Toast(completed
                ? "온실 꼭대기 완료 · 높은 빛을 얻었다. 화재 위험과 좁아진 탈출로가 남았다."
                : "높은 빛 +1 · 오래된 별길 나무에 심을 수 있다.", 5f);
        }
    }
}
