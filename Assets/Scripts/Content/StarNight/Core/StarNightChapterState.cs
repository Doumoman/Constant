using System;
using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class StarNightChapterState : MonoBehaviour
    {
        [SerializeField] private StarChapterDefinition definition;
        [SerializeField, Range(0f, 100f)] private float scent;
        [SerializeField] private StarScentStage scentStage;
        [SerializeField] private int departureProgress;
        [SerializeField] private bool departureReady;
        [SerializeField] private bool departed;
        [SerializeField] private bool gateLoopEnabled;
        [SerializeField] private ChapterLoopState loopState;
        [SerializeField] private int gateContributions;
        [SerializeField] private int gateRequired = 2;
        [SerializeField] private bool gateReady;
        [SerializeField] private bool gateActivated;
        [SerializeField] private StarBellPhase bellPhase;
        [SerializeField] private bool temptationOpen;
        [SerializeField] private bool departureOpen;
        [SerializeField] private bool gateClosing;
        [SerializeField] private float gateActivationScentBaseline;
        [SerializeField] private float postGateAlert;

        public StarChapterDefinition Definition => definition;
        public float Scent => scent;
        public StarScentStage ScentStage => scentStage;
        public int DepartureProgress => departureProgress;
        public bool DepartureReady => departureReady;
        public bool Departed => departed;
        public bool GateLoopEnabled => gateLoopEnabled;
        public ChapterLoopState LoopState => loopState;
        public int GateContributions => gateContributions;
        public int GateRequired => gateRequired;
        public bool GateReady => gateReady;
        public bool GateActivated => gateActivated;
        public StarBellPhase BellPhase => bellPhase;
        public bool TemptationOpen => temptationOpen;
        public bool DepartureOpen => departureOpen;
        public bool GateClosing => gateClosing;
        public float GateActivationScentBaseline => gateActivationScentBaseline;
        public float PostGateAlert => postGateAlert;

        public event Action<float, StarScentStage> ScentChanged;
        public event Action<int, int> DepartureProgressChanged;
        public event Action DepartureBecameReady;
        public event Action<ChapterLoopState> LoopStateChanged;
        public event Action<int, int> GateContributionChanged;
        public event Action<StarBellPhase> BellPhaseChanged;
        public event Action<float> GateAlertChanged;

        public void Begin(StarChapterDefinition chapterDefinition)
        {
            definition = chapterDefinition;
            scent = 0f;
            scentStage = StarScentStage.Quiet;
            departureProgress = 0;
            departureReady = false;
            departed = false;
            DisableGateLoop();
            ScentChanged?.Invoke(scent, scentStage);
            DepartureProgressChanged?.Invoke(departureProgress, RequiredDepartureProgress);
        }

        public int RequiredDepartureProgress => gateLoopEnabled
            ? gateRequired
            : definition != null ? Mathf.Max(1, definition.requiredDepartureItems) : 3;

        public void BeginGateLoop(int required)
        {
            gateLoopEnabled = true;
            loopState = ChapterLoopState.Arrival;
            gateContributions = 0;
            gateRequired = Mathf.Max(1, required);
            gateReady = false;
            gateActivated = false;
            bellPhase = StarBellPhase.None;
            temptationOpen = false;
            departureOpen = false;
            gateClosing = false;
            gateActivationScentBaseline = 0f;
            postGateAlert = 0f;
            departureProgress = 0;
            departureReady = false;
            GateContributionChanged?.Invoke(gateContributions, gateRequired);
            DepartureProgressChanged?.Invoke(departureProgress, gateRequired);
            GateAlertChanged?.Invoke(postGateAlert);
        }

        public void DisableGateLoop()
        {
            gateLoopEnabled = false;
            loopState = ChapterLoopState.Arrival;
            gateContributions = 0;
            gateRequired = 2;
            gateReady = false;
            gateActivated = false;
            bellPhase = StarBellPhase.None;
            temptationOpen = false;
            departureOpen = false;
            gateClosing = false;
            gateActivationScentBaseline = 0f;
            postGateAlert = 0f;
        }

        public void SetLoopState(ChapterLoopState state)
        {
            loopState = state;
            LoopStateChanged?.Invoke(state);
        }

        public void SetGateContributionCount(int count)
        {
            if (!gateLoopEnabled || gateReady)
            {
                return;
            }

            gateContributions = Mathf.Clamp(count, 0, gateRequired);
            departureProgress = gateContributions;
            gateReady = gateContributions >= gateRequired;
            departureReady = gateReady;
            GateContributionChanged?.Invoke(gateContributions, gateRequired);
            DepartureProgressChanged?.Invoke(departureProgress, gateRequired);
            if (gateReady)
            {
                DepartureBecameReady?.Invoke();
            }
        }

        public void ActivateGate(float scentBaseline)
        {
            if (!gateLoopEnabled || !gateReady || gateActivated)
            {
                return;
            }

            gateActivated = true;
            bellPhase = StarBellPhase.First;
            temptationOpen = true;
            departureOpen = true;
            gateActivationScentBaseline = Mathf.Clamp(scentBaseline, 0f, 100f);
            postGateAlert = 0f;
            GateAlertChanged?.Invoke(postGateAlert);
            BellPhaseChanged?.Invoke(bellPhase);
        }

        public void SetBellPhase(StarBellPhase phase)
        {
            if (!gateActivated || phase <= bellPhase)
            {
                return;
            }

            bellPhase = phase;
            BellPhaseChanged?.Invoke(phase);
        }

        public void SetGateClosing(bool closing)
        {
            gateClosing = gateActivated && closing;
        }

        public void AddScent(float amount, string reason, string sourceId = null)
        {
            if (Mathf.Approximately(amount, 0f))
            {
                return;
            }

            StarScentStage previous = scentStage;
            scent = Mathf.Clamp(scent + amount, 0f, 100f);
            scentStage = StarScentRules.FromValue(scent);
            if (gateLoopEnabled && gateActivated)
            {
                if (amount > 0f)
                {
                    AddGateAlert(amount);
                }
                else if (amount < 0f && bellPhase < StarBellPhase.Third)
                {
                    float minimum = StarGateAlertRules.MinimumAlertForPhase(bellPhase);
                    float next = Mathf.Max(minimum, postGateAlert + amount);
                    if (!Mathf.Approximately(next, postGateAlert))
                    {
                        postGateAlert = next;
                        GateAlertChanged?.Invoke(postGateAlert);
                    }
                }
            }
            ScentChanged?.Invoke(scent, scentStage);

            if (previous != scentStage && StarNightRunState.Instance != null)
            {
                StarNightRunState.Instance.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.ScentStageChanged,
                    actorId = "StarScent",
                    targetId = sourceId,
                    detail = $"{StarScentRules.DisplayName(previous)}에서 {StarScentRules.DisplayName(scentStage)}: {reason}",
                    scentDelta = amount
                });
            }
        }

        public void AddGateAlert(float amount)
        {
            if (!gateLoopEnabled || !gateActivated || amount <= 0f ||
                bellPhase >= StarBellPhase.Third)
            {
                return;
            }

            postGateAlert = Mathf.Min(StarGateAlertRules.ThirdBellThreshold,
                postGateAlert + amount);
            GateAlertChanged?.Invoke(postGateAlert);
        }

        public bool AddDepartureProgress(int amount, string sourceId)
        {
            if (gateLoopEnabled || departureReady || amount <= 0)
            {
                return false;
            }

            departureProgress = Mathf.Min(RequiredDepartureProgress, departureProgress + amount);
            DepartureProgressChanged?.Invoke(departureProgress, RequiredDepartureProgress);
            StarNightRunState.Instance?.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.FuelDelivered,
                actorId = "Player",
                targetId = sourceId,
                detail = $"별 연료 {departureProgress}/{RequiredDepartureProgress}"
            });

            if (departureProgress >= RequiredDepartureProgress)
            {
                departureReady = true;
                StarNightRunState.Instance?.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.DepartureReady,
                    actorId = "Chapter",
                    detail = "떠날 준비가 끝났다"
                });
                DepartureBecameReady?.Invoke();
            }

            return true;
        }

        public bool MarkDeparted()
        {
            if (!departureReady || departed ||
                (gateLoopEnabled && (!gateActivated || !departureOpen)))
            {
                return false;
            }

            departed = true;
            StarNightRunState.Instance?.SetFlag("chapter.departed");
            if (definition != null)
            {
                StarNightRunState.Instance?.SetFlag($"chapter.{definition.chapter}.departed");
            }
            return true;
        }
    }
}
