using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class CloudGuruRestStation : MonoBehaviour, IStarNightInteractable
    {
        private bool used;

        public string Prompt => used
            ? "다시 잠든 구루의 숨소리 듣기"
            : "GateReady 전에 자장가 장치로 구루를 다시 재우기";

        public void Interact(StarNightPlayerAgent player)
        {
            if (used)
            {
                return;
            }

            StarNightRunState run = StarNightRunState.Ensure();
            GateRouteRuntimeState route =
                run.ChapterLoop.FindRoute("CH3_ROUTE_GURU_BREATH");
            bool canRest = run.GetFlag("CH3_GURU_AWAKENED_FORCEFULLY") &&
                           !run.Chapter.GateReady &&
                           route != null &&
                           route.state == GateRouteState.Complete;
            if (!canRest)
            {
                StarNightHUD.Instance?.Toast(run.Chapter.GateReady
                    ? "바람 2/2가 고정된 뒤에는 거센 숨결이 구루에게 되돌아가지 않는다."
                    : "구루가 깨어 있고 숨결이 아직 별문에 장착되지 않았을 때만 다시 재울 수 있다.");
                return;
            }

            used = true;
            run.SetFlag("CH3_GURU_RESTED_AFTER_WAKE");
            run.SetFlag("CH3_DAMAGE_REPAIRED");
            if (run.GetCounter("CH3_STORM_DAMAGE") > 0)
            {
                run.AddCounter("CH3_STORM_DAMAGE", -1);
            }
            run.SetNpcState("Guru", StarNpcState.Calm);
            run.Chapter.AddScent(-4f, "자장가 장치가 구루의 거친 호흡을 가라앉혔다", "GuruLullaby");
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.GuruReturned,
                actorId = "Player",
                targetId = "Guru",
                routeId = "CH3_ROUTE_GURU_BREATH",
                detail = "구루의 숨결을 얻은 뒤 GateReady 전에 자장가 장치로 다시 재웠다",
                helpedResident = true,
                witnessed = true
            });
            StarNightHUD.Instance?.Toast(
                "구루가 다시 잠들었다. 숨결은 남았지만 폭풍 피해는 수습됐다.", 5f);
        }
    }
}
