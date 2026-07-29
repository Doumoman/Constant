using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class CloudRainDock : MonoBehaviour
    {
        [SerializeField] private string dockId = "A";
        [SerializeField] private FableObject rainCloud;
        [SerializeField] private float captureRadius = 1.8f;
        [SerializeField] private float requiredMass = 1.35f;
        [SerializeField] private GateRouteObjective routeObjective;
        [SerializeField] private string completionFlag;
        [SerializeField] private bool delivered;

        public bool Delivered => delivered;
        public FableObject RainCloud => rainCloud;

        public void Configure(string id, FableObject cloud, float massRequirement = 1.35f)
        {
            dockId = id;
            rainCloud = cloud;
            requiredMass = massRequirement;
        }

        public void ConfigureRouteObjective(GateRouteObjective objective, string flag)
        {
            routeObjective = objective;
            completionFlag = flag;
        }

        private void Update()
        {
            TryDeliver();
        }

        public bool TryDeliver()
        {
            if (delivered || rainCloud == null || rainCloud.Body == null)
            {
                return false;
            }

            float distance = Vector2.Distance(transform.position, rainCloud.transform.position);
            if (distance > captureRadius || rainCloud.Body.mass < requiredMass)
            {
                return false;
            }

            delivered = true;
            rainCloud.transform.position = transform.position;
            rainCloud.Body.linearVelocity = Vector2.zero;
            rainCloud.Body.angularVelocity = 0f;
            rainCloud.Body.bodyType = RigidbodyType2D.Kinematic;

            StarNightRunState run = StarNightRunState.Ensure();
            run.SetFlag($"CH3_RAIN_CLOUD_{dockId}_DELIVERED");
            bool completedRoute = false;
            if (run.Chapter.GateLoopEnabled)
            {
                completedRoute = routeObjective != null && routeObjective.Complete();
                if (completedRoute && !string.IsNullOrWhiteSpace(completionFlag))
                {
                    run.SetFlag(completionFlag);
                }
            }
            else
            {
                run.Chapter.AddDepartureProgress(1, $"RainCloudDock.{dockId}");
            }
            run.Chapter.AddScent(-2f, "비구름이 수차 안에서 조용히 비를 내렸다", rainCloud.ObjectId);
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.RainCloudDelivered,
                actorId = "Player",
                targetId = dockId,
                tool = FableVerb.Float,
                detail = $"{dockId}번 비구름을 무겁게 내려 수차에 고정했다",
                helpedResident = true,
                witnessed = true
            });
            StarNightHUD.Instance?.Toast(run.Chapter.GateLoopEnabled
                ? completedRoute
                    ? $"{routeObjective.RouteId} 완료 · 출항 돛에 넣을 바람을 확보했다."
                    : "보조 수차가 안정됐다. 목장의 비는 유지되지만 새 바람 조각은 생기지 않았다."
                : $"비구름 수차 {run.Chapter.DepartureProgress}/{run.Chapter.RequiredDepartureProgress}",
                4f);
            return true;
        }
    }
}
