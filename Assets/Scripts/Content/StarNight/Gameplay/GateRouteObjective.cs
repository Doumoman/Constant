using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class GateRouteObjective : MonoBehaviour
    {
        [SerializeField] private string routeId;
        [SerializeField] private bool completed;
        [SerializeField] private bool invalidated;

        public string RouteId => routeId;
        public bool Completed => completed;
        public bool Invalidated => invalidated;

        public void Configure(string id)
        {
            routeId = id;
        }

        public bool Complete()
        {
            if (completed || invalidated || string.IsNullOrWhiteSpace(routeId))
            {
                return false;
            }

            ChapterLoopDirector director = StarNightRunState.Instance?.ChapterLoop;
            if (director == null || !director.CompleteRoute(routeId))
            {
                return false;
            }

            completed = true;
            return true;
        }

        public bool Invalidate()
        {
            if (invalidated || string.IsNullOrWhiteSpace(routeId))
            {
                return false;
            }

            ChapterLoopDirector director = StarNightRunState.Instance?.ChapterLoop;
            if (director == null || !director.InvalidateRoute(routeId))
            {
                return false;
            }

            invalidated = true;
            completed = false;
            return true;
        }

        public bool ReturnContribution(string detail = null)
        {
            if (!completed || invalidated || string.IsNullOrWhiteSpace(routeId))
            {
                return false;
            }

            ChapterLoopDirector director = StarNightRunState.Instance?.ChapterLoop;
            if (director == null || !director.ReturnRouteContribution(routeId, detail))
            {
                return false;
            }

            completed = false;
            return true;
        }
    }
}
