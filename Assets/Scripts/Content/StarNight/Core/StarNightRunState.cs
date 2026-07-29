using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class StarNightRunState : MonoBehaviour
    {
        public static StarNightRunState Instance { get; private set; }

        [SerializeField] private int seed;
        [SerializeField] private StarChapterId currentChapter;
        [SerializeField] private bool runActive;
        [SerializeField] private StarRunEndReason endReason;

        private readonly Dictionary<string, bool> flags = new();
        private readonly Dictionary<string, int> counters = new();
        private readonly Dictionary<string, StarNpcState> npcStates = new();
        private readonly List<ConsequenceModifier> consequences = new();
        private readonly HashSet<FableVerb> unlockedTools = new();
        private readonly List<StarChapterReport> chapterReports = new();

        public int Seed => seed;
        public StarChapterId CurrentChapter => currentChapter;
        public bool RunActive => runActive;
        public StarRunEndReason EndReason => endReason;
        public IReadOnlyList<ConsequenceModifier> Consequences => consequences;
        public IReadOnlyCollection<FableVerb> UnlockedTools => unlockedTools;
        public IReadOnlyList<StarChapterReport> ChapterReports => chapterReports;
        public StarNightChapterState Chapter { get; private set; }
        public StarNightActionRecorder Actions { get; private set; }
        public StarNightAccidentReportBuilder AccidentReport { get; private set; }
        public StarNightWatcherResolver Watcher { get; private set; }
        public StarNightConsequenceResolver ConsequenceResolver { get; private set; }
        public RedThreadSystem RedThread { get; private set; }
        public CloudBottleSystem CloudBottle { get; private set; }
        public StarDeliverySystem Delivery { get; private set; }
        public SunSeedSystem SunSeeds { get; private set; }
        public GardenHeatSystem Heat { get; private set; }
        public GateContributionInventory GateContributions { get; private set; }
        public ChapterLoopDirector ChapterLoop { get; private set; }
        public RunRouteMap RouteMap { get; private set; }
        public StarNightRunTelemetry Telemetry { get; private set; }

        public event Action RunStarted;
        public event Action<StarChapterDefinition> ChapterStarted;
        public event Action<StarRunEndReason> RunEnded;
        public event Action<string, bool> FlagChanged;
        public event Action<FableVerb> ToolUnlocked;

        private void Awake()
        {
            if (!EnsureInitialized())
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private bool EnsureInitialized()
        {
            if (Instance != null && Instance != this)
            {
                return false;
            }

            Instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
            CacheModules();
            return true;
        }

        private void CacheModules()
        {
            Chapter = GetOrAdd<StarNightChapterState>();
            Actions = GetOrAdd<StarNightActionRecorder>();
            AccidentReport = GetOrAdd<StarNightAccidentReportBuilder>();
            Watcher = GetOrAdd<StarNightWatcherResolver>();
            ConsequenceResolver = GetOrAdd<StarNightConsequenceResolver>();
            RedThread = GetOrAdd<RedThreadSystem>();
            CloudBottle = GetOrAdd<CloudBottleSystem>();
            Delivery = GetOrAdd<StarDeliverySystem>();
            SunSeeds = GetOrAdd<SunSeedSystem>();
            Heat = GetOrAdd<GardenHeatSystem>();
            GateContributions = GetOrAdd<GateContributionInventory>();
            ChapterLoop = GetOrAdd<ChapterLoopDirector>();
            RouteMap = GetOrAdd<RunRouteMap>();
            Telemetry = GetOrAdd<StarNightRunTelemetry>();
            Telemetry.Attach(this);
        }

        private T GetOrAdd<T>() where T : Component
        {
            T component = GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        public static StarNightRunState Ensure()
        {
            if (Instance != null)
            {
                return Instance;
            }

            StarNightRunState found = FindFirstObjectByType<StarNightRunState>();
            if (found != null)
            {
                found.EnsureInitialized();
                return found;
            }

            StarNightRunState created = new GameObject("@StarNightRun").AddComponent<StarNightRunState>();
            created.EnsureInitialized();
            return created;
        }

        public void BeginNewRun(int? requestedSeed = null)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            seed = requestedSeed ?? Environment.TickCount;
            currentChapter = StarChapterId.Prologue;
            runActive = true;
            endReason = StarRunEndReason.None;
            flags.Clear();
            counters.Clear();
            npcStates.Clear();
            consequences.Clear();
            chapterReports.Clear();
            unlockedTools.Clear();
            unlockedTools.Add(FableVerb.Resize);
            RedThread.ResetForChapter();
            CloudBottle.ResetForChapter();
            Delivery.ResetForChapter();
            SunSeeds.ResetForChapter();
            Heat.ResetForChapter();
            GateContributions.ResetForChapter();
            ChapterLoop.ResetForChapter();
            RouteMap.ResetForRun();
            Actions.Clear();
            AccidentReport.Clear();
            RunStarted?.Invoke();
        }

        public void BeginChapter(StarChapterDefinition definition)
        {
            if (!runActive)
            {
                BeginNewRun();
            }

            currentChapter = definition.chapter;
            Chapter.Begin(definition);
            RedThread.ResetForChapter();
            CloudBottle.ResetForChapter();
            Delivery.ResetForChapter();
            SunSeeds.ResetForChapter();
            Heat.ResetForChapter();
            GateContributions.ResetForChapter();
            ChapterLoop.Begin(definition);
            RouteMap.BeginChapter(definition.chapter);
            UnlockTool(definition.coreVerb);
            ChapterStarted?.Invoke(definition);
        }

        public StarChapterReport CompleteCurrentChapter()
        {
            if (!runActive || Chapter == null || !Chapter.DepartureReady || Chapter.Departed ||
                (Chapter.GateLoopEnabled && !ChapterLoop.CanDepart))
            {
                return null;
            }

            if (Chapter.GateLoopEnabled && !ChapterLoop.TryBeginDeparture())
            {
                return null;
            }
            if (!Chapter.MarkDeparted())
            {
                return null;
            }
            Actions.Record(new StarActionContext
            {
                actionType = StarActionType.ChapterDeparted,
                actorId = "Player",
                targetId = currentChapter.ToString(),
                detail = $"{Chapter.Definition?.displayName ?? currentChapter.ToString()}을 떠났다",
                gateContributions = Chapter.GateContributions,
                gateReady = Chapter.GateReady,
                gateActivated = Chapter.GateActivated,
                bellPhase = (int)Chapter.BellPhase,
                witnessed = true
            });
            StarChapterReport report = new()
            {
                chapter = currentChapter,
                raniSummary = Watcher.ResolveRaniSummary(currentChapter),
                finalActionSequence = Actions.LatestSequence
            };
            chapterReports.Add(report);
            SetFlag($"chapter.{currentChapter}.completed");
            if (Chapter.GateLoopEnabled)
            {
                RouteMap.RegisterGateRestored(currentChapter);
                ChapterLoop.EnterIntermission();
            }
            return report;
        }

        public void EndRun(StarRunEndReason reason)
        {
            if (!runActive)
            {
                return;
            }

            runActive = false;
            endReason = reason;
            RunEnded?.Invoke(reason);
        }

        public void SetFlag(string key, bool value = true)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            flags[key] = value;
            FlagChanged?.Invoke(key, value);
        }

        public bool GetFlag(string key) => !string.IsNullOrWhiteSpace(key) && flags.TryGetValue(key, out bool value) && value;

        public int AddCounter(string key, int amount = 1)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return 0;
            }

            counters.TryGetValue(key, out int value);
            value += amount;
            counters[key] = value;
            return value;
        }

        public int GetCounter(string key) => !string.IsNullOrWhiteSpace(key) && counters.TryGetValue(key, out int value) ? value : 0;

        public void SetNpcState(string npcId, StarNpcState state)
        {
            if (!string.IsNullOrWhiteSpace(npcId))
            {
                npcStates[npcId] = state;
            }
        }

        public StarNpcState GetNpcState(string npcId) =>
            !string.IsNullOrWhiteSpace(npcId) && npcStates.TryGetValue(npcId, out StarNpcState state)
                ? state
                : StarNpcState.Calm;

        public void AddConsequence(ConsequenceModifier consequence)
        {
            if (consequence == null || string.IsNullOrWhiteSpace(consequence.id))
            {
                return;
            }

            int existing = consequences.FindIndex(item => item.id == consequence.id);
            if (existing >= 0)
            {
                consequences[existing] = consequence;
            }
            else
            {
                consequences.Add(consequence);
            }
        }

        public void UnlockTool(FableVerb tool)
        {
            if (unlockedTools.Add(tool))
            {
                ToolUnlocked?.Invoke(tool);
            }
        }

        public bool IsToolUnlocked(FableVerb tool) => unlockedTools.Contains(tool);
    }
}
