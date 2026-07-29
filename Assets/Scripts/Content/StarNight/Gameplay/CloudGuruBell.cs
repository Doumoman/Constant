using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class CloudGuruBell : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private int rings;
        [SerializeField] private GateRouteObjective routeObjective;

        public int Rings => rings;
        public string Prompt => rings < 3 ? $"구루의 방울 울리기 ({rings}/3)" : "지친 구루를 바라보기";

        public void ConfigureRouteObjective(GateRouteObjective objective)
        {
            routeObjective = objective;
        }

        public void Interact(StarNightPlayerAgent player)
        {
            StarNightRunState run = StarNightRunState.Ensure();
            if (run.GetFlag("CH3_GURU_AWAKENED_FORCEFULLY"))
            {
                StarNightHUD.Instance?.Toast("구루는 이미 거친 바람 속에서 깨어 있다.");
                return;
            }

            rings++;
            float scent = run.ConsequenceResolver.ModifyScent(5f + rings * 2f);
            run.Chapter.AddScent(scent, $"구루의 방울을 {rings}번 울렸다", "GuruBell");
            if (rings < 3)
            {
                StarNightHUD.Instance?.Toast($"딩— 구루가 몸을 뒤척였다. {rings}/3");
                return;
            }

            run.SetFlag("CH3_GURU_AWAKENED_FORCEFULLY");
            run.SetFlag("CH3_STORM_STARTED");
            run.SetNpcState("Guru", StarNpcState.Tired);
            run.AddCounter("CH3_STORM_DAMAGE");
            bool completedRoute = run.Chapter.GateLoopEnabled &&
                                  routeObjective != null &&
                                  routeObjective.Complete();
            if (completedRoute)
            {
                run.SetFlag("CH3_ROUTE_GURU_BREATH_COMPLETE");
            }
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.GuruAwakened,
                actorId = "Player",
                targetId = "Guru",
                detail = "잠들어 있던 구루에게 방울을 세 번 울려 강제로 깨웠다",
                causedAccident = true,
                witnessed = true
            });
            StarNightHUD.Instance?.Toast(completedRoute
                ? "구루가 놀라 깨어났다. 구루의 숨결을 얻었지만 목장 위로 폭풍 하중이 번진다!"
                : "구루가 놀라 깨어났다. 목장 위로 폭풍 하중이 번진다!", 5f);
        }
    }
}
