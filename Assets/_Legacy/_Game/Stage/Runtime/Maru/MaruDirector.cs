#if LEGACY_DISABLED
using System;
using System.Collections;
using System.Collections.Generic;
using StarNight.Core.Flow;
using StarNight.Core.State;
using StarNight.Interaction.Input;
using StarNight.Map;
using StarNight.Player.Motor;
using StarNight.Stage.Data;
using StarNight.Stage.Flow;
using StarNight.Stage.Rooms;
using StarNight.Stage.Secrets;
using UnityEngine;

namespace StarNight.Stage.Maru
{
    [DisallowMultipleComponent]
    public sealed class MaruDirector : MonoBehaviour, IMaruElementEventSink
    {
        public const float LogicalRoomStepSeconds = 2.4f;
        public const float RoomSpawnDelaySeconds = 0.18f;
        public const float NormalEscapeSeconds = 1.2f;
        public const float AssistedEscapeSeconds = 1.8f;
        public const int RequiredEscapePresses = 4;
        public const string MaruFailureReason = "maru_bite";

        private RunManager runManager;
        private GameFlowController gameFlow;
        private StageFlowController stageFlow;
        private GameplayInputReader inputReader;
        private PlayerActionLock actionLock;
        private MaruRoomAgent currentAgent;
        private BellPhase observedBellPhase;
        private string logicalRoomId = string.Empty;
        private string previousLogicalRoomId = string.Empty;
        private float logicalStepRemaining;
        private float spawnDelayRemaining = -1f;
        private bool escapeActive;
        private float escapeDuration;
        private float escapeRemaining;
        private int escapePresses;
        private bool failurePending;
        private bool initialized;
        private readonly Dictionary<string, float> collarTimerSources = new Dictionary<string, float>(StringComparer.Ordinal);
        private float scheduledCurrentRoomEntryRemaining = -1f;
        private float forcedExitGuidanceRemaining;

        public event Action<BellPhase> BellChanged;
        public event Action<string, string> LogicalRoomChanged;
        public event Action<MaruRoomAgent> AgentSpawned;
        public event Action EscapeStarted;
        public event Action<float> EscapeProgressChanged;
        public event Action EscapeSucceeded;
        public event Action FailureStarted;
        public event Action RunFailed;
        public event Action<MaruElementEventRequest, MaruElementEventResult> MapElementEventApplied;
        public event Action<float> ForcedExitGuidanceChanged;

        public BellPhase CurrentBellPhase => stageFlow?.RuntimeState?.bellPhase ?? BellPhase.None;
        public bool IsChasing => CurrentBellPhase == BellPhase.Maru && !failurePending;
        public string LogicalRoomId => logicalRoomId;
        public MaruRoomAgent CurrentAgent => currentAgent;
        public Vector2Int ApproachDirection { get; private set; }
        public float RemainingSeconds => MaruTimeline.GetRemainingSeconds(
            stageFlow?.CurrentDefinition,
            stageFlow?.RuntimeState?.elapsedTime ?? 0f,
            ExtendMaruTime);
        public bool IsEscapeActive => escapeActive;
        public float EscapeDuration => escapeDuration;
        public float EscapeRemainingSeconds => escapeRemaining;
        public float EscapeProgress => escapeActive ? Mathf.Clamp01((float)escapePresses / RequiredEscapePresses) : 0f;
        public bool IsTimerBlocked => stageFlow?.IsMaruTimerBlocked ?? true;
        public bool IsExitDiscovered => stageFlow?.RuntimeState?.exitDiscovered ?? false;
        public float MaruTimerRateMultiplier
        {
            get
            {
                var multiplier = 1f;
                foreach (var pair in collarTimerSources)
                {
                    multiplier = Mathf.Max(multiplier, pair.Value);
                }
                return multiplier;
            }
        }
        public float ScheduledCurrentRoomEntryRemaining => Mathf.Max(0f, scheduledCurrentRoomEntryRemaining);
        public float ForcedExitGuidanceRemaining => Mathf.Max(0f, forcedExitGuidanceRemaining);

        private bool ExtendMaruTime => GameBootstrap.Instance?.Settings?.accessibility?.extendMaruTime == true;
        private bool TravelerAssist => GameBootstrap.Instance?.Settings?.accessibility?.travelerAssist == true;

        public void Initialize(RunManager manager, GameFlowController applicationFlow, StageFlowController flow)
        {
            if (initialized)
            {
                return;
            }

            runManager = manager ?? throw new ArgumentNullException(nameof(manager));
            gameFlow = applicationFlow ?? throw new ArgumentNullException(nameof(applicationFlow));
            stageFlow = flow ?? throw new ArgumentNullException(nameof(flow));
            stageFlow.StageLoaded += HandleStageLoaded;
            stageFlow.RoomChanged += HandleRoomChanged;
            stageFlow.StageTransitionStarted += HandleStageTransitionStarted;
            MaruElementEventHub.Bind(this);
            initialized = true;
            if (stageFlow.RuntimeState != null)
            {
                ResetForStage();
            }
        }

        private void OnDestroy()
        {
            if (stageFlow != null)
            {
                stageFlow.StageLoaded -= HandleStageLoaded;
                stageFlow.RoomChanged -= HandleRoomChanged;
                stageFlow.StageTransitionStarted -= HandleStageTransitionStarted;
            }
            SetInputReader(null);
            MaruElementEventHub.Unbind(this);
        }

        private void Update()
        {
            if (!initialized || stageFlow?.RuntimeState == null || gameFlow == null)
            {
                return;
            }

            ResolvePlayerInput();
            if (gameFlow.State != GameApplicationState.Playing || failurePending)
            {
                return;
            }

            BellPhase before = stageFlow.RuntimeState.bellPhase;
            BellPhase after = stageFlow.AdvanceMaruClock(
                Time.deltaTime * MaruTimerRateMultiplier,
                ExtendMaruTime);
            if (after != before || after != observedBellPhase)
            {
                HandleBellChanged(after);
            }

            TickMapElementEffects(Time.deltaTime);

            if (escapeActive)
            {
                TickEscape(Time.deltaTime);
            }
            else if (IsChasing)
            {
                TickChase(Time.deltaTime);
            }
        }

        public void SetShopOpen(bool open)
        {
            stageFlow?.SetShopTimerBlocked(open);
        }

        public MaruElementEventResult ApplyMaruElementEvent(MaruElementEventRequest request)
        {
            if (!initialized || stageFlow?.RuntimeState == null || runManager?.Current == null)
            {
                return MaruElementEventResult.Rejected("MaruDirector가 준비되지 않았습니다.");
            }

            MaruElementEventResult result;
            switch (request.EventType)
            {
                case MaruElementEventType.StatueWarning:
                    result = new MaruElementEventResult
                    {
                        Accepted = true,
                        PenaltyApplied = true,
                        PenaltyText = "귀향상 방울 경고",
                    };
                    break;

                case MaruElementEventType.StatueBroken:
                {
                    AddMoney(request.RewardMoney);
                    var advanced = ForceBellPhaseAtLeast(BellPhase.Second);
                    result = new MaruElementEventResult
                    {
                        Accepted = true,
                        RewardGranted = request.RewardMoney > 0,
                        PenaltyApplied = advanced,
                        RewardText = $"+{request.RewardMoney}원 금 주괴",
                        PenaltyText = advanced ? "방울 2단계 전진" : "이미 방울 2단계 이후",
                    };
                    break;
                }

                case MaruElementEventType.BellJarBroken:
                    AddMoney(request.RewardMoney);
                    scheduledCurrentRoomEntryRemaining = Mathf.Max(0f, request.Seconds);
                    result = new MaruElementEventResult
                    {
                        Accepted = true,
                        RewardGranted = request.RewardMoney > 0,
                        PenaltyApplied = true,
                        RewardText = $"+{request.RewardMoney}원 은 주괴",
                        PenaltyText = $"{request.Seconds:0}초 후 마루 현재 방 진입",
                    };
                    break;

                case MaruElementEventType.CollarCarryChanged:
                    result = ApplyCollarCarry(request);
                    break;

                case MaruElementEventType.CollarCommittedAtExit:
                    runManager.Current.flags.Add(string.IsNullOrWhiteSpace(request.RewardId)
                        ? "maru_clue_next_stage"
                        : request.RewardId);
                    result = new MaruElementEventResult
                    {
                        Accepted = true,
                        RewardGranted = true,
                        RewardText = "다음 스테이지 마루 단서 개방",
                    };
                    break;

                case MaruElementEventType.ReturnMarkerUsed:
                    result = ApplyReturnMarker(request);
                    break;

                case MaruElementEventType.PawprintPoolTriggered:
                    result = ApplyPawprintPool(request);
                    break;

                case MaruElementEventType.RecordTravelerFreed:
                {
                    var rewardId = string.IsNullOrWhiteSpace(request.RewardId)
                        ? "record_traveler_freed"
                        : request.RewardId;
                    runManager.Current.flags.Add(rewardId);
                    runManager.Current.flags.Add($"record_guide:{request.RecordGuideEffect}");
                    result = new MaruElementEventResult
                    {
                        Accepted = true,
                        RewardGranted = true,
                        PenaltyApplied = request.NoiseLevel > 0f,
                        RewardText = $"기록 길손 해방 · {GetRecordGuideText(request.RecordGuideEffect)}",
                        PenaltyText = request.NoiseLevel > 0f ? "낮은 소음 발생" : string.Empty,
                    };
                    break;
                }

                default:
                    result = MaruElementEventResult.Rejected("지원하지 않는 마루 요소 이벤트입니다.");
                    break;
            }

            MapElementEventApplied?.Invoke(request, result);
            return result;
        }

        private MaruElementEventResult ApplyCollarCarry(MaruElementEventRequest request)
        {
            var sourceId = string.IsNullOrWhiteSpace(request.SourceRuntimeId)
                ? request.ElementId
                : request.SourceRuntimeId;
            if (request.Active)
            {
                collarTimerSources[sourceId] = Mathf.Max(1f, request.RateMultiplier);
                runManager.Current.items.Add("OBJ_CollarFragment");
            }
            else
            {
                collarTimerSources.Remove(sourceId);
                if (collarTimerSources.Count == 0)
                {
                    runManager.Current.items.Remove("OBJ_CollarFragment");
                }
            }

            return new MaruElementEventResult
            {
                Accepted = true,
                RewardGranted = request.Active,
                PenaltyApplied = request.Active,
                RewardText = request.Active ? "별목줄 파편 StoryCarry" : "별목줄 파편 내려놓음",
                PenaltyText = request.Active ? "소지 중 방울 타이머 +15%" : string.Empty,
            };
        }

        private MaruElementEventResult ApplyReturnMarker(MaruElementEventRequest request)
        {
            if (!stageFlow.RoomGraph.TryGetRoom(stageFlow.RuntimeState.currentRoomId, out RoomRuntime room) ||
                stageFlow.CurrentPlayer == null)
            {
                return MaruElementEventResult.Rejected("현재 방 Entry SafeCell을 찾을 수 없습니다.");
            }

            var run = runManager.Current;
            if (request.MarkerCostType == MaruMarkerCostType.Money)
            {
                if (run.moneyWon < request.MarkerCostValue)
                {
                    return MaruElementEventResult.Rejected("소지금이 부족합니다.");
                }
                run.moneyWon -= request.MarkerCostValue;
            }
            else
            {
                if (run.health <= request.MarkerCostValue)
                {
                    return MaruElementEventResult.Rejected("체력이 부족합니다.");
                }
                run.health -= request.MarkerCostValue;
            }

            stageFlow.CurrentPlayer.SnapTo(room.GetPrimarySafePosition());
            return new MaruElementEventResult
            {
                Accepted = true,
                RewardGranted = true,
                PenaltyApplied = request.MarkerCostValue > 0,
                RewardText = "Entry SafeCell 복귀",
                PenaltyText = request.MarkerCostType == MaruMarkerCostType.Money
                    ? $"-{request.MarkerCostValue}원"
                    : $"체력 -{request.MarkerCostValue}",
            };
        }

        private MaruElementEventResult ApplyPawprintPool(MaruElementEventRequest request)
        {
            if (stageFlow.RuntimeState.exitDiscovered)
            {
                return MaruElementEventResult.Rejected("출구를 이미 발견했습니다.");
            }

            stageFlow.RuntimeState.elapsedTime += Mathf.Max(0f, request.Seconds);
            BellPhase before = stageFlow.RuntimeState.bellPhase;
            BellPhase after = stageFlow.AdvanceMaruClock(0f, ExtendMaruTime);
            if (after != before || after != observedBellPhase)
            {
                HandleBellChanged(after);
            }

            forcedExitGuidanceRemaining = Mathf.Max(0f, request.GuidanceSeconds);
            ForcedExitGuidanceChanged?.Invoke(forcedExitGuidanceRemaining);
            return new MaruElementEventResult
            {
                Accepted = true,
                RewardGranted = true,
                PenaltyApplied = true,
                RewardText = $"출구 방향 {request.GuidanceSeconds:0.#}초 표시",
                PenaltyText = $"다음 방울 -{request.Seconds:0}초",
            };
        }

        private void AddMoney(int amount)
        {
            if (amount <= 0 || runManager?.Current == null)
            {
                return;
            }

            runManager.Current.moneyWon += amount;
            runManager.Current.peakMoney = Mathf.Max(runManager.Current.peakMoney, runManager.Current.moneyWon);
        }

        private bool ForceBellPhaseAtLeast(BellPhase phase)
        {
            if (stageFlow?.RuntimeState == null || (int)stageFlow.RuntimeState.bellPhase >= (int)phase)
            {
                return false;
            }

            stageFlow.RuntimeState.bellPhase = phase;
            stageFlow.RuntimeState.phase = phase == BellPhase.Maru ? StagePhase.MaruChase : StagePhase.Bell2;
            if (stageFlow.CurrentDefinition != null)
            {
                var threshold = phase == BellPhase.Maru
                    ? stageFlow.CurrentDefinition.maruSpawnTime
                    : stageFlow.CurrentDefinition.bell2Time;
                stageFlow.RuntimeState.elapsedTime = Mathf.Max(
                    stageFlow.RuntimeState.elapsedTime,
                    threshold * MaruTimeline.GetMultiplier(ExtendMaruTime));
            }
            HandleBellChanged(phase);
            return true;
        }

        private static string GetRecordGuideText(MaruRecordGuideEffect effect)
        {
            switch (effect)
            {
                case MaruRecordGuideEffect.ValuableRoom: return "귀중품 방 표시";
                case MaruRecordGuideEffect.ElementWeakness: return "요소 약점 시연";
                case MaruRecordGuideEffect.DisableOneTrap: return "함정 1개 비활성화";
                default: return "출구 방향 손짓";
            }
        }

        public static float GetEscapeDuration(bool travelerAssist)
        {
            return travelerAssist ? AssistedEscapeSeconds : NormalEscapeSeconds;
        }

        public bool TryBitePlayer(MaruRoomAgent source)
        {
            PlayerMotor2D player = stageFlow?.CurrentPlayer;
            if (failurePending || escapeActive || stageFlow?.RuntimeState == null ||
                stageFlow.RuntimeState.phase == StagePhase.ExitCommitted || stageFlow.IsStageTransitioning ||
                source == null || source != currentAgent ||
                player != null && player.TryGetComponent(out SecretReturnMaruBiteImmunity immunity) && immunity.IsActive)
            {
                return false;
            }

            if (stageFlow.RuntimeState.maruBiteCount >= 1)
            {
                stageFlow.RuntimeState.maruBiteCount++;
                source.SetBiting(true);
                BeginFailure();
                return true;
            }

            stageFlow.RuntimeState.maruBiteCount = 1;
            escapeActive = true;
            escapeDuration = GetEscapeDuration(TravelerAssist);
            escapeRemaining = escapeDuration;
            escapePresses = 0;
            source.SetBiting(true);
            LockPlayerForBite();
            EscapeStarted?.Invoke();
            EscapeProgressChanged?.Invoke(0f);
            return true;
        }

        public void RegisterEscapePress()
        {
            if (!escapeActive || failurePending)
            {
                return;
            }

            escapePresses = Mathf.Min(RequiredEscapePresses, escapePresses + 1);
            EscapeProgressChanged?.Invoke(EscapeProgress);
            if (escapePresses >= RequiredEscapePresses)
            {
                CompleteEscape();
            }
        }

        private void TickEscape(float deltaTime)
        {
            escapeRemaining = Mathf.Max(0f, escapeRemaining - Mathf.Max(0f, deltaTime));
            if (escapeRemaining <= 0f)
            {
                BeginFailure();
            }
        }

        private void TickChase(float deltaTime)
        {
            string playerRoomId = stageFlow.RuntimeState.currentRoomId;
            if (string.IsNullOrWhiteSpace(logicalRoomId))
            {
                BeginChase();
            }

            if (currentAgent != null && !string.Equals(currentAgent.RoomId, playerRoomId, StringComparison.Ordinal))
            {
                DestroyAgent();
            }

            if (string.Equals(logicalRoomId, playerRoomId, StringComparison.Ordinal))
            {
                UpdateApproachDirection();
                if (currentAgent == null)
                {
                    if (spawnDelayRemaining < 0f)
                    {
                        spawnDelayRemaining = RoomSpawnDelaySeconds;
                    }
                    spawnDelayRemaining -= Mathf.Max(0f, deltaTime);
                    if (spawnDelayRemaining <= 0f && !stageFlow.IsRoomTransitioning)
                    {
                        SpawnCurrentRoomAgent();
                    }
                }
                return;
            }

            DestroyAgent();
            logicalStepRemaining -= Mathf.Max(0f, deltaTime);
            if (logicalStepRemaining > 0f)
            {
                UpdateApproachDirection();
                return;
            }

            string next = stageFlow.RoomGraph.GetNextStepToward(logicalRoomId, playerRoomId);
            if (string.IsNullOrEmpty(next) || !stageFlow.RoomGraph.AreAdjacent(logicalRoomId, next))
            {
                logicalStepRemaining = LogicalRoomStepSeconds;
                return;
            }

            SetLogicalRoom(next);
            logicalStepRemaining = LogicalRoomStepSeconds;
            if (string.Equals(next, playerRoomId, StringComparison.Ordinal))
            {
                spawnDelayRemaining = RoomSpawnDelaySeconds;
            }
        }

        private void HandleBellChanged(BellPhase phase)
        {
            observedBellPhase = phase;
            BellChanged?.Invoke(phase);
            if (phase == BellPhase.Maru)
            {
                BeginChase();
            }
        }

        private void BeginChase()
        {
            if (stageFlow?.RuntimeState == null || stageFlow.CurrentDefinition?.kind == StageKind.Boss)
            {
                return;
            }

            string start = stageFlow.RoomGraph.StartRoomId;
            SetLogicalRoom(string.IsNullOrEmpty(start) ? stageFlow.RuntimeState.currentRoomId : start, true);
            logicalStepRemaining = LogicalRoomStepSeconds;
            spawnDelayRemaining = string.Equals(logicalRoomId, stageFlow.RuntimeState.currentRoomId, StringComparison.Ordinal)
                ? RoomSpawnDelaySeconds
                : -1f;
        }

        private void SetLogicalRoom(string roomId, bool allowInitial = false)
        {
            if (string.IsNullOrWhiteSpace(roomId) || string.Equals(logicalRoomId, roomId, StringComparison.Ordinal))
            {
                return;
            }
            if (!allowInitial && !string.IsNullOrEmpty(logicalRoomId) && !stageFlow.RoomGraph.AreAdjacent(logicalRoomId, roomId))
            {
                return;
            }

            string previous = logicalRoomId;
            previousLogicalRoomId = previous;
            logicalRoomId = roomId;
            stageFlow.RuntimeState.maruRoomId = roomId;
            UpdateApproachDirection();
            LogicalRoomChanged?.Invoke(previous, roomId);
        }

        private void SpawnCurrentRoomAgent()
        {
            spawnDelayRemaining = -1f;
            if (currentAgent != null || stageFlow.IsRoomTransitioning ||
                !stageFlow.RoomGraph.TryGetRoom(logicalRoomId, out RoomRuntime room) ||
                !string.Equals(logicalRoomId, stageFlow.RuntimeState.currentRoomId, StringComparison.Ordinal))
            {
                return;
            }

            PlayerMotor2D player = stageFlow.CurrentPlayer;
            if (player == null)
            {
                return;
            }

            Transform laneRoot = room.transform.Find("MaruLaneRoot");
            if (laneRoot == null)
            {
                laneRoot = new GameObject("MaruLaneRoot").transform;
                laneRoot.SetParent(room.transform, false);
            }
            MaruLane lane = laneRoot.GetComponent<MaruLane>();
            if (lane == null)
            {
                lane = laneRoot.gameObject.AddComponent<MaruLane>();
                lane.Configure(room);
            }

            Vector2Int entryDirection = !string.IsNullOrEmpty(previousLogicalRoomId)
                ? stageFlow.RoomGraph.GetDirection(previousLogicalRoomId, logicalRoomId)
                : stageFlow.CurrentGuidance.Direction;
            if (entryDirection == Vector2Int.zero)
            {
                entryDirection = Vector2Int.right;
            }

            var agentObject = new GameObject("MaruRoomAgent");
            agentObject.transform.SetParent(laneRoot, true);
            currentAgent = agentObject.AddComponent<MaruRoomAgent>();
            currentAgent.Configure(this, lane, player, lane.GetEntry(entryDirection));
            AgentSpawned?.Invoke(currentAgent);
            UpdateApproachDirection();
        }

        private void ResolvePlayerInput()
        {
            GameplayInputReader next = stageFlow?.CurrentPlayer != null
                ? stageFlow.CurrentPlayer.GetComponent<GameplayInputReader>()
                : FindFirstObjectByType<GameplayInputReader>();
            if (next != inputReader)
            {
                SetInputReader(next);
            }
            actionLock = stageFlow?.CurrentPlayer != null
                ? stageFlow.CurrentPlayer.GetComponent<PlayerActionLock>()
                : null;
        }

        private void SetInputReader(GameplayInputReader reader)
        {
            if (inputReader != null)
            {
                inputReader.PrimaryActionPressed -= RegisterEscapePress;
            }
            inputReader = reader;
            if (inputReader != null)
            {
                inputReader.PrimaryActionPressed += RegisterEscapePress;
            }
        }

        private void LockPlayerForBite()
        {
            ResolvePlayerInput();
            actionLock?.SetState(PlayerActionState.MaruBitten);
            PlayerMotor2D player = stageFlow?.CurrentPlayer;
            if (player != null)
            {
                player.ClearBufferedInput();
                player.SetMoveInput(0f);
                if (player.Body != null)
                {
                    player.Body.linearVelocity = Vector2.zero;
                }
            }
        }

        private void CompleteEscape()
        {
            escapeActive = false;
            escapeRemaining = 0f;
            RunState run = runManager?.Current;
            if (run != null)
            {
                run.health = Mathf.Max(0, run.health - 1);
                if (run.health <= 0 && run.lanternAvailable)
                {
                    run.lanternAvailable = false;
                    run.health = 1;
                }
            }

            if (actionLock?.State == PlayerActionState.MaruBitten)
            {
                actionLock.ResetToFree();
            }
            currentAgent?.Stun(MaruRoomAgent.EscapeStunSeconds);
            inputReader?.ClearBufferedButtons();
            EscapeSucceeded?.Invoke();
            if (run != null && run.health <= 0)
            {
                BeginFailure();
            }
        }

        private void BeginFailure()
        {
            if (failurePending)
            {
                return;
            }
            failurePending = true;
            escapeActive = false;
            currentAgent?.SetBiting(true);
            LockPlayerForBite();
            FailureStarted?.Invoke();
            StartCoroutine(FailureRoutine());
        }

        private IEnumerator FailureRoutine()
        {
            yield return new WaitForSecondsRealtime(0.8f);
            runManager?.FailRun(MaruFailureReason);
            gameFlow?.EnterRunResult();
            DestroyAgent();
            RunFailed?.Invoke();
        }

        private void TickMapElementEffects(float deltaTime)
        {
            if (stageFlow == null || stageFlow.IsMaruTimerBlocked)
            {
                return;
            }

            if (forcedExitGuidanceRemaining > 0f)
            {
                forcedExitGuidanceRemaining = Mathf.Max(0f, forcedExitGuidanceRemaining - Mathf.Max(0f, deltaTime));
                ForcedExitGuidanceChanged?.Invoke(forcedExitGuidanceRemaining);
            }

            if (scheduledCurrentRoomEntryRemaining < 0f)
            {
                return;
            }

            scheduledCurrentRoomEntryRemaining -= Mathf.Max(0f, deltaTime);
            if (scheduledCurrentRoomEntryRemaining > 0f)
            {
                return;
            }

            scheduledCurrentRoomEntryRemaining = -1f;
            ForceBellPhaseAtLeast(BellPhase.Maru);
            var currentRoomId = stageFlow.RuntimeState.currentRoomId;
            SetLogicalRoom(currentRoomId, true);
            spawnDelayRemaining = RoomSpawnDelaySeconds;
        }

        private void HandleStageLoaded(StageLoadedEvent _)
        {
            ResetForStage();
        }

        private void ResetForStage()
        {
            DestroyAgent();
            escapeActive = false;
            failurePending = false;
            logicalRoomId = string.Empty;
            previousLogicalRoomId = string.Empty;
            logicalStepRemaining = LogicalRoomStepSeconds;
            spawnDelayRemaining = -1f;
            scheduledCurrentRoomEntryRemaining = -1f;
            forcedExitGuidanceRemaining = 0f;
            collarTimerSources.Clear();
            observedBellPhase = stageFlow.RuntimeState?.bellPhase ?? BellPhase.None;
            ApproachDirection = Vector2Int.zero;
            ResolvePlayerInput();
        }

        private void HandleRoomChanged(RoomChangedEvent change)
        {
            DestroyAgent();
            if (string.Equals(logicalRoomId, change.To, StringComparison.Ordinal))
            {
                spawnDelayRemaining = RoomSpawnDelaySeconds;
            }
            UpdateApproachDirection();
        }

        private void HandleStageTransitionStarted()
        {
            if (collarTimerSources.Count > 0 && runManager?.Current != null)
            {
                runManager.Current.flags.Add("maru_clue_next_stage");
                runManager.Current.items.Remove("OBJ_CollarFragment");
                collarTimerSources.Clear();
            }
            escapeActive = false;
            if (actionLock?.State == PlayerActionState.MaruBitten)
            {
                actionLock.ResetToFree();
            }
            DestroyAgent();
        }

        private void UpdateApproachDirection()
        {
            if (stageFlow?.RuntimeState == null || string.IsNullOrEmpty(logicalRoomId))
            {
                ApproachDirection = Vector2Int.zero;
                return;
            }

            string playerRoom = stageFlow.RuntimeState.currentRoomId;
            if (!string.Equals(logicalRoomId, playerRoom, StringComparison.Ordinal))
            {
                ApproachDirection = stageFlow.RoomGraph.GetDirection(playerRoom, logicalRoomId);
            }
            else if (currentAgent != null && stageFlow.CurrentPlayer != null)
            {
                float delta = currentAgent.transform.position.x - stageFlow.CurrentPlayer.transform.position.x;
                ApproachDirection = new Vector2Int(delta < 0f ? -1 : 1, 0);
            }
            else if (!string.IsNullOrEmpty(previousLogicalRoomId))
            {
                ApproachDirection = stageFlow.RoomGraph.GetDirection(playerRoom, previousLogicalRoomId);
            }
            else
            {
                Vector2Int exitDirection = stageFlow.CurrentGuidance.Direction;
                ApproachDirection = exitDirection == Vector2Int.zero ? Vector2Int.left : -exitDirection;
            }
        }

        private void DestroyAgent()
        {
            if (currentAgent == null)
            {
                return;
            }
            if (Application.isPlaying)
            {
                Destroy(currentAgent.gameObject);
            }
            else
            {
                DestroyImmediate(currentAgent.gameObject);
            }
            currentAgent = null;
        }

#if UNITY_EDITOR
        public void AdvanceMapElementEffectsForTests(float seconds)
        {
            TickMapElementEffects(seconds);
        }

        public void AdvanceClockForTests(float seconds)
        {
            BellPhase before = stageFlow.RuntimeState.bellPhase;
            BellPhase after = stageFlow.AdvanceMaruClock(seconds, ExtendMaruTime);
            if (after != before || after != observedBellPhase)
            {
                HandleBellChanged(after);
            }
        }

        public void ForceStartChaseForTests(string roomId = null)
        {
            stageFlow.RuntimeState.bellPhase = BellPhase.Maru;
            stageFlow.RuntimeState.phase = StagePhase.MaruChase;
            observedBellPhase = BellPhase.Maru;
            SetLogicalRoom(string.IsNullOrEmpty(roomId) ? stageFlow.RoomGraph.StartRoomId : roomId, true);
            spawnDelayRemaining = 0f;
            TickChase(0f);
        }

        public bool ForceBiteForTests()
        {
            if (currentAgent == null)
            {
                SpawnCurrentRoomAgent();
            }
            return TryBitePlayer(currentAgent);
        }
#endif
    }
}

#endif
