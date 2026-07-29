using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class ChapterLoopDirector : MonoBehaviour
    {
        [SerializeField] private List<GateRouteRuntimeState> routes = new();

        private StarNightRunState run;

        public bool Enabled => run != null && run.Chapter.GateLoopEnabled;
        public ChapterLoopState State => run != null ? run.Chapter.LoopState : ChapterLoopState.Arrival;
        public IReadOnlyList<GateRouteRuntimeState> Routes => routes;
        public bool CanDepart => Enabled && run.Chapter.GateActivated && run.Chapter.DepartureOpen;

        public event Action<ChapterLoopState> StateChanged;
        public event Action<GateRouteRuntimeState> RouteChanged;

        public void ResetForChapter()
        {
            routes.Clear();
        }

        private void Update()
        {
            AdvanceAlertClock(Time.deltaTime);
        }

        public void Begin(StarChapterDefinition definition)
        {
            run = StarNightRunState.Instance;
            routes.Clear();
            if (run == null || definition == null || !definition.useGateLoop)
            {
                run?.Chapter.DisableGateLoop();
                return;
            }

            HashSet<string> routeIds = new();
            if (definition.gateRoutes != null)
            {
                foreach (GateRouteDefinition routeDefinition in definition.gateRoutes)
                {
                    if (routeDefinition == null ||
                        string.IsNullOrWhiteSpace(routeDefinition.id) ||
                        !routeIds.Add(routeDefinition.id))
                    {
                        continue;
                    }

                    routes.Add(GateRouteRuntimeState.FromDefinition(routeDefinition));
                }
            }

            run.Chapter.BeginGateLoop(definition.gateContributionRequired);
            run.Chapter.GateAlertChanged -= OnGateAlertChanged;
            run.Chapter.GateAlertChanged += OnGateAlertChanged;
            StateChanged?.Invoke(run.Chapter.LoopState);
        }

        private void OnDestroy()
        {
            if (run?.Chapter != null)
            {
                run.Chapter.GateAlertChanged -= OnGateAlertChanged;
            }
        }

        public void AdvanceAlertClock(float seconds)
        {
            if (!Enabled || seconds <= 0f ||
                (State != ChapterLoopState.Bell1 && State != ChapterLoopState.Bell2))
            {
                return;
            }
            run.Chapter.AddGateAlert(seconds * StarGateAlertRules.PassiveAlertPerSecond);
        }

        private void OnGateAlertChanged(float alert)
        {
            if (!Enabled || !run.Chapter.GateActivated)
            {
                return;
            }
            if (run.Chapter.BellPhase == StarBellPhase.First &&
                alert >= StarGateAlertRules.SecondBellThreshold)
            {
                TryAdvanceBell(StarBellPhase.Second);
            }
            if (run.Chapter.BellPhase == StarBellPhase.Second &&
                alert >= StarGateAlertRules.ThirdBellThreshold)
            {
                TryAdvanceBell(StarBellPhase.Third);
            }
        }

        public bool EnterRuleIntro()
        {
            if (!Enabled || State != ChapterLoopState.Arrival)
            {
                return false;
            }

            return ChangeState(ChapterLoopState.RuleIntro);
        }

        public bool OpenRoutes()
        {
            if (!Enabled || (State != ChapterLoopState.Arrival && State != ChapterLoopState.RuleIntro))
            {
                return false;
            }

            foreach (GateRouteRuntimeState route in routes)
            {
                if (route.state == GateRouteState.Locked)
                {
                    route.state = GateRouteState.Available;
                    RouteChanged?.Invoke(route);
                }
            }

            return ChangeState(ChapterLoopState.RouteOpen);
        }

        public bool CompleteRoute(string routeId)
        {
            GateRouteRuntimeState route = FindRoute(routeId);
            if (!Enabled ||
                route == null ||
                route.state != GateRouteState.Available ||
                (State != ChapterLoopState.RouteOpen && State != ChapterLoopState.RouteProgress))
            {
                return false;
            }

            GateContribution contribution = new()
            {
                id = route.contributionId,
                displayName = route.contributionDisplayName,
                routeId = route.id,
                gateValue = route.gateValue
            };
            if (!run.GateContributions.TryAdd(contribution))
            {
                return false;
            }

            route.state = GateRouteState.Complete;
            RouteChanged?.Invoke(route);
            run.SetFlag($"{route.id}_COMPLETE");
            ChangeState(ChapterLoopState.RouteProgress);
            Record(StarActionType.RouteObjectiveCompleted, route.id,
                $"{route.displayName} 경로를 완료해 {route.contributionDisplayName}을 확보했다");
            return true;
        }

        public bool InvalidateRoute(string routeId)
        {
            GateRouteRuntimeState route = FindRoute(routeId);
            if (!Enabled || route == null || route.state == GateRouteState.Contributed)
            {
                return false;
            }
            if (route.state != GateRouteState.Available && route.state != GateRouteState.Complete)
            {
                return false;
            }

            run.GateContributions.RemoveByRoute(route.id);
            route.state = GateRouteState.Invalidated;
            run.SetFlag($"{route.id}_COMPLETE", false);
            run.SetFlag($"{route.id}_INVALIDATED");
            RouteChanged?.Invoke(route);
            return true;
        }

        public bool ReturnRouteContribution(string routeId, string detail = null)
        {
            GateRouteRuntimeState route = FindRoute(routeId);
            if (!Enabled ||
                route == null ||
                route.state != GateRouteState.Complete ||
                !run.GateContributions.RemoveByRoute(routeId))
            {
                return false;
            }

            route.state = GateRouteState.Available;
            run.SetFlag($"{route.id}_COMPLETE", false);
            run.SetFlag($"{route.id}_RETURNED");
            RouteChanged?.Invoke(route);
            Record(StarActionType.GateContributionReturned, route.id,
                string.IsNullOrWhiteSpace(detail)
                    ? $"{route.contributionDisplayName}을 별문에 넣기 전에 돌려놓았다"
                    : detail);
            return true;
        }

        public bool TryContribute(string routeId)
        {
            GateRouteRuntimeState route = FindRoute(routeId);
            if (!Enabled ||
                route == null ||
                route.state != GateRouteState.Complete ||
                run.Chapter.GateReady ||
                !run.GateContributions.TryTakeByRoute(routeId, out GateContribution contribution))
            {
                return false;
            }

            route.state = GateRouteState.Contributed;
            run.SetFlag($"{route.id}_CONTRIBUTED");
            RouteChanged?.Invoke(route);
            int next = Mathf.Min(run.Chapter.GateRequired,
                run.Chapter.GateContributions + Mathf.Max(1, contribution.gateValue));
            run.Chapter.SetGateContributionCount(next);
            Record(StarActionType.GateContributionAdded, route.id,
                $"{contribution.displayName}을 별문에 장착했다 · {next}/{run.Chapter.GateRequired}");

            if (run.Chapter.GateReady)
            {
                ChangeState(ChapterLoopState.GateReady);
                Record(StarActionType.GateReady, "StarGate",
                    $"별문 기여 {next}/{run.Chapter.GateRequired} · 손잡이 가동 준비 완료");
            }
            return true;
        }

        public bool TryActivateGate()
        {
            if (!Enabled || State != ChapterLoopState.GateReady || !run.Chapter.GateReady)
            {
                return false;
            }

            run.Chapter.ActivateGate(run.Chapter.Scent);
            ChangeState(ChapterLoopState.GateActive);
            ChangeState(ChapterLoopState.Bell1);
            Record(StarActionType.GateActivated, "StarGate",
                "플레이어가 별문 손잡이를 당겼다 · 출구와 유혹 구역이 열렸다");
            Record(StarActionType.BellPhaseChanged, "MaruBell",
                "첫 번째 방울 · 돌아오는 길을 맡았다");
            return true;
        }

        public bool TryAdvanceBell(StarBellPhase phase)
        {
            if (run == null)
            {
                return false;
            }

            int requested = (int)phase;
            int current = (int)run.Chapter.BellPhase;
            if (!Enabled ||
                !run.Chapter.GateActivated ||
                requested < (int)StarBellPhase.Second ||
                requested > (int)StarBellPhase.Third ||
                requested != current + 1)
            {
                return false;
            }

            run.Chapter.SetBellPhase(phase);
            ChangeState(phase == StarBellPhase.Second
                ? ChapterLoopState.Bell2
                : ChapterLoopState.Bell3);
            Record(StarActionType.BellPhaseChanged, "MaruBell",
                phase == StarBellPhase.Second
                    ? "두 번째 방울 · 마루가 같은 정거장에 들어왔다"
                    : "세 번째 방울 · 귀가 시간");

            if (phase == StarBellPhase.Third)
            {
                run.Chapter.SetGateClosing(true);
                Record(StarActionType.GateClosing, "StarGate",
                    "세 번째 방울과 함께 별문이 닫히기 시작했다");
            }
            return true;
        }

        public bool TryBeginDeparture()
        {
            if (!CanDepart ||
                (State != ChapterLoopState.Bell1 &&
                 State != ChapterLoopState.Bell2 &&
                 State != ChapterLoopState.Bell3 &&
                 State != ChapterLoopState.GateActive))
            {
                return false;
            }

            return ChangeState(ChapterLoopState.Departure);
        }

        public bool EnterIntermission()
        {
            return Enabled && State == ChapterLoopState.Departure &&
                   ChangeState(ChapterLoopState.Intermission);
        }

        public GateRouteRuntimeState FindRoute(string routeId)
        {
            return string.IsNullOrWhiteSpace(routeId)
                ? null
                : routes.Find(route => route.id == routeId);
        }

        private bool ChangeState(ChapterLoopState state)
        {
            if (run == null || run.Chapter.LoopState == state)
            {
                return false;
            }

            run.Chapter.SetLoopState(state);
            StateChanged?.Invoke(state);
            Record(StarActionType.ChapterLoopStateChanged, "ChapterLoop", state.ToString());
            return true;
        }

        private void Record(StarActionType type, string targetId, string detail)
        {
            run.Actions.Record(new StarActionContext
            {
                actionType = type,
                actorId = "ChapterLoop",
                targetId = targetId,
                routeId = targetId,
                detail = detail,
                gateContributions = run.Chapter.GateContributions,
                gateReady = run.Chapter.GateReady,
                gateActivated = run.Chapter.GateActivated,
                bellPhase = (int)run.Chapter.BellPhase,
                witnessed = true
            });
        }
    }
}
