using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class StarRouteStampRecovery : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private FableObject routeStamp;
        private bool recovered;
        public string Prompt => recovered ? "항로 도장이 등록되어 있다" : "북극성 항로 도장 회수하기";

        public void Configure(FableObject stamp)
        {
            routeStamp = stamp;
        }

        public void Interact(StarNightPlayerAgent player)
        {
            if (recovered)
            {
                return;
            }

            recovered = true;
            StarNightRunState run = StarNightRunState.Ensure();
            run.SetFlag("CH4_ROUTE_STAMP_RECOVERED");
            run.SetFlag("STARPATH_POLARIS_ROUTE_REGISTERED");
            run.Chapter.AddDepartureProgress(1, "PolarisRouteStamp");
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.RouteStampRecovered,
                actorId = "Player",
                targetId = "PolarisRouteStamp",
                detail = "수신자를 잃은 편지 보관소에서 북극성 항로 도장을 찾았다",
                helpedResident = true,
                witnessed = true
            });
            if (routeStamp != null)
            {
                routeStamp.SetStored(true);
            }
            StarNightHUD.Instance?.Toast("북극성 항로 등록 가능. 이제 떠나거나 반송 불가 보관소를 열 수 있다.", 6f);
        }
    }
}
