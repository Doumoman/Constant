using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class HaoreumDecision : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private StarPathTreeController starPathTree;
        [SerializeField] private GateRouteObjective routeObjective;

        public string Prompt => "종과 햇빛 씨앗으로 해오름 즉시 깨우기";

        public void Configure(StarPathTreeController tree)
        {
            starPathTree = tree;
        }

        public void ConfigureRouteObjective(GateRouteObjective objective)
        {
            routeObjective = objective;
        }

        public void Interact(StarNightPlayerAgent player)
        {
            StarNightRunState run = StarNightRunState.Ensure();
            if (run.GetFlag("CH5_WAITED_FOR_SUN"))
            {
                StarNightHUD.Instance?.Toast("해오름은 이미 스스로 깨어났다.");
                return;
            }
            if (run.GetFlag("CH5_SUN_AWAKENED_FORCEFULLY"))
            {
                StarNightHUD.Instance?.Toast("해오름의 빛은 이미 너무 뜨겁다.");
                return;
            }
            if (!run.SunSeeds.ConsumeCharge())
            {
                StarNightHUD.Instance?.Toast("종에 심을 저장 햇빛이 필요하다.");
                return;
            }

            run.SetFlag("CH5_SUN_AWAKENED_FORCEFULLY");
            run.SetNpcState("Haoreum", StarNpcState.Tired);
            run.Heat.AddHeat(45f, "쉬지 못한 해오름을 종으로 깨움", "Haoreum");
            float scent = run.ConsequenceResolver.ModifyScent(22f);
            run.Chapter.AddScent(scent, "피곤한 작은 해가 정원 전체를 한꺼번에 비췄다", "Haoreum");
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.HaoreumForcedAwake,
                actorId = "Player",
                targetId = "Haoreum",
                detail = "해오름이 깨어날 준비가 되었는지 묻지 않고 종과 씨앗으로 즉시 깨웠다",
                scentDelta = scent,
                causedAccident = true,
                witnessed = true
            });

            bool completedRoute = run.Chapter.GateLoopEnabled &&
                                  routeObjective != null &&
                                  routeObjective.Complete();
            if (completedRoute)
            {
                run.SetFlag("CH5_ROUTE_HAOREUM_WAKE_COMPLETE");
            }

            if (!run.Chapter.GateLoopEnabled && starPathTree == null)
            {
                starPathTree = FindFirstObjectByType<StarPathTreeController>();
            }
            if (!run.Chapter.GateLoopEnabled)
            {
                starPathTree?.ForceGrowToReady();
            }
            foreach (SunGrowthState growth in FindObjectsByType<SunGrowthState>(FindObjectsSortMode.None))
            {
                if (growth.Kind == SunGrowthKind.SleepingCreature)
                {
                    growth.ApplySunlight();
                }
            }
            StarNightHUD.Instance?.Toast(completedRoute
                ? "해오름이 놀라 눈을 떴다! 해오름 빛을 얻었지만 정원 과열과 피로가 남았다."
                : "해오름이 놀라 눈을 떴다! 별길 나무는 즉시 자랐지만 정원 열과 가시성이 크게 올랐다.", 7f);
        }
    }
}
