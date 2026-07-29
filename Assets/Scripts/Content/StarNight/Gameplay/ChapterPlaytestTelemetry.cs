using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class ChapterPlaytestTelemetry : MonoBehaviour
    {
        [SerializeField] private bool logCompletedRun = true;
        [SerializeField] private float sessionStartedAt;
        [SerializeField] private float firstRouteCompletedAt = -1f;
        [SerializeField] private float gateReadyAt = -1f;
        [SerializeField] private float gateActivatedAt = -1f;
        [SerializeField] private float bell2At = -1f;
        [SerializeField] private float bell3At = -1f;
        [SerializeField] private float temptationEnteredAt = -1f;
        [SerializeField] private float runEndedAt = -1f;
        [SerializeField] private bool departed;
        [SerializeField] private bool caughtByMaru;
        [SerializeField] private bool finished;

        private readonly HashSet<string> completedRoutes = new();
        private readonly HashSet<string> contributedRoutes = new();
        private StarNightRunState run;
        private bool tracking;

        public float FirstRouteSeconds => Elapsed(firstRouteCompletedAt);
        public float GateReadySeconds => Elapsed(gateReadyAt);
        public float GateActivatedSeconds => Elapsed(gateActivatedAt);
        public float Bell2Seconds => Elapsed(bell2At);
        public float Bell3Seconds => Elapsed(bell3At);
        public float TemptationSeconds => Elapsed(temptationEnteredAt);
        public float RunEndSeconds => Elapsed(runEndedAt);
        public bool EnteredTemptation => temptationEnteredAt >= 0f;
        public bool Departed => departed;
        public bool CaughtByMaru => caughtByMaru;
        public IReadOnlyCollection<string> CompletedRoutes => completedRoutes;
        public IReadOnlyCollection<string> ContributedRoutes => contributedRoutes;
        public string RouteCombination => contributedRoutes.Count == 0
            ? "None"
            : string.Join("+", contributedRoutes.OrderBy(route => route));

        private void Start()
        {
            BeginTracking();
        }

        private void OnDestroy()
        {
            StopTracking();
        }

        public void BeginTracking()
        {
            StopTracking();
            run = StarNightRunState.Ensure();
            sessionStartedAt = Time.timeSinceLevelLoad;
            firstRouteCompletedAt = -1f;
            gateReadyAt = -1f;
            gateActivatedAt = -1f;
            bell2At = -1f;
            bell3At = -1f;
            temptationEnteredAt = -1f;
            runEndedAt = -1f;
            departed = false;
            caughtByMaru = false;
            finished = false;
            completedRoutes.Clear();
            contributedRoutes.Clear();

            run.ChapterLoop.RouteChanged += OnRouteChanged;
            run.ChapterLoop.StateChanged += OnLoopStateChanged;
            run.Actions.Recorded += OnActionRecorded;
            run.RunEnded += OnRunEnded;
            tracking = true;
        }

        public string BuildTechnicalReport()
        {
            string outcome = departed ? "Departed" : caughtByMaru ? "CaughtByMaru" : "InProgress";
            return $"routes={RouteCombination}; firstRoute={Format(FirstRouteSeconds)}; " +
                   $"ready={Format(GateReadySeconds)}; active={Format(GateActivatedSeconds)}; " +
                   $"bell2={Format(Bell2Seconds)}; bell3={Format(Bell3Seconds)}; " +
                   $"temptation={(EnteredTemptation ? Format(TemptationSeconds) : "No")}; " +
                   $"outcome={outcome}; end={Format(RunEndSeconds)}";
        }

        private void OnRouteChanged(GateRouteRuntimeState route)
        {
            if (route == null)
            {
                return;
            }
            if (route.state == GateRouteState.Complete)
            {
                completedRoutes.Add(route.id);
                CaptureFirst(ref firstRouteCompletedAt);
            }
            else if (route.state == GateRouteState.Contributed)
            {
                contributedRoutes.Add(route.id);
            }
            else if (route.state == GateRouteState.Available ||
                     route.state == GateRouteState.Invalidated)
            {
                completedRoutes.Remove(route.id);
                contributedRoutes.Remove(route.id);
            }
        }

        private void OnLoopStateChanged(ChapterLoopState state)
        {
            switch (state)
            {
                case ChapterLoopState.GateReady:
                    CaptureFirst(ref gateReadyAt);
                    break;
                case ChapterLoopState.GateActive:
                    CaptureFirst(ref gateActivatedAt);
                    break;
                case ChapterLoopState.Bell2:
                    CaptureFirst(ref bell2At);
                    break;
                case ChapterLoopState.Bell3:
                    CaptureFirst(ref bell3At);
                    break;
                case ChapterLoopState.Intermission:
                    departed = true;
                    Finish();
                    break;
                case ChapterLoopState.ForcedReturn:
                    caughtByMaru = true;
                    Finish();
                    break;
            }
        }

        private void OnActionRecorded(StarActionRecord record)
        {
            if (record == null)
            {
                return;
            }
            if (record.actionType == StarActionType.EnteredTemptationRoom)
            {
                CaptureFirst(ref temptationEnteredAt);
            }
            else if (record.actionType == StarActionType.PlayerCaught)
            {
                caughtByMaru = true;
                Finish();
            }
        }

        private void OnRunEnded(StarRunEndReason reason)
        {
            caughtByMaru |= reason == StarRunEndReason.ForcedReturnByMaru;
            Finish();
        }

        private void Finish()
        {
            if (finished)
            {
                return;
            }
            finished = true;
            CaptureFirst(ref runEndedAt);
            if (logCompletedRun)
            {
                Debug.Log($"[M2 Playtest] {BuildTechnicalReport()}");
            }
        }

        private void StopTracking()
        {
            if (!tracking || run == null)
            {
                tracking = false;
                return;
            }
            run.ChapterLoop.RouteChanged -= OnRouteChanged;
            run.ChapterLoop.StateChanged -= OnLoopStateChanged;
            run.Actions.Recorded -= OnActionRecorded;
            run.RunEnded -= OnRunEnded;
            tracking = false;
        }

        private void CaptureFirst(ref float timestamp)
        {
            if (timestamp < 0f)
            {
                timestamp = Time.timeSinceLevelLoad;
            }
        }

        private float Elapsed(float timestamp)
        {
            return timestamp < 0f ? -1f : Mathf.Max(0f, timestamp - sessionStartedAt);
        }

        private static string Format(float seconds)
        {
            return seconds < 0f ? "—" : $"{seconds:0.0}s";
        }
    }
}
