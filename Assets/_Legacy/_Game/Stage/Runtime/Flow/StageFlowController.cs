#if LEGACY_DISABLED
using System;
using System.Collections;
using StarNight.Core.Flow;
using StarNight.Core.State;
using StarNight.Interaction.Input;
using StarNight.Player.Motor;
using StarNight.Stage.Data;
using StarNight.Stage.Exit;
using StarNight.Stage.Guidance;
using StarNight.Stage.Lab;
using StarNight.Stage.Maru;
using StarNight.Stage.Rooms;
using StarNight.Stage.Secrets;
using UnityEngine;

namespace StarNight.Stage.Flow
{
    public readonly struct StageLoadedEvent
    {
        public StageLoadedEvent(string stageId, string sceneName)
        {
            StageId = stageId;
            SceneName = sceneName;
        }

        public string StageId { get; }
        public string SceneName { get; }
    }

    public readonly struct ExitDiscoveredEvent
    {
        public ExitDiscoveredEvent(string stageId, string roomId)
        {
            StageId = stageId;
            RoomId = roomId;
        }

        public string StageId { get; }
        public string RoomId { get; }
    }

    [DisallowMultipleComponent]
    public sealed class StageFlowController : MonoBehaviour
    {
        private sealed class PendingStage
        {
            public StageDefinition Definition;
            public Core04TwoRoomLab Lab;
            public PlayerMotor2D Player;
            public Camera Camera;
            public StageGuidanceOverlay Overlay;
        }

        private readonly StageAssembler assembler = new StageAssembler();
        private readonly ExitGuidanceService guidance = new ExitGuidanceService();
        private readonly StageRoomGraph roomGraph = new StageRoomGraph();
        private RunManager runManager;
        private SceneTransitionService sceneTransition;
        private GameFlowController gameFlow;
        private Core04TwoRoomLab currentLab;
        private PlayerMotor2D player;
        private Camera worldCamera;
        private PendingStage pendingStage;
        private Coroutine transitionRoutine;
        private bool narrativeTimerBlocked;
        private bool shopTimerBlocked;
        private StageEntrySnapshot stageEntrySnapshot;

        public event Action<StageLoadedEvent> StageLoaded;
        public event Action<RoomChangedEvent> RoomChanged;
        public event Action<ExitDiscoveredEvent> ExitDiscovered;
        public event Action<string> StageIntroRequested;
        public event Action StageTransitionStarted;

        public StageDefinition CurrentDefinition { get; private set; }
        public StageRuntimeState RuntimeState { get; private set; }
        public StageExitDoor CurrentExit { get; private set; }
        public ExitGuidanceService GuidanceService => guidance;
        public StageRoomGraph RoomGraph => roomGraph;
        public ExitGuidance CurrentGuidance => RuntimeState == null ? default : guidance.GetGuidance(RuntimeState.currentRoomId);
        public bool IsStageTransitioning => transitionRoutine != null;
        public bool IsRoomTransitioning => currentLab?.TransitionController?.IsTransitioning ?? false;
        public float FadeOpacity { get; private set; }
        public bool IsNarrativeTimerBlocked => narrativeTimerBlocked;
        public bool IsShopTimerBlocked => shopTimerBlocked;
        public bool IsMaruTimerBlocked => narrativeTimerBlocked || shopTimerBlocked ||
                                          CurrentDefinition == null || RuntimeState == null ||
                                          CurrentDefinition.kind == StageKind.Boss;
        public bool CanRestartCurrentStage => CurrentDefinition != null && stageEntrySnapshot != null && !IsStageTransitioning;
        public PlayerMotor2D CurrentPlayer => player;

        public bool CanCommitExit
        {
            get
            {
                if (RuntimeState == null || CurrentDefinition == null || IsStageTransitioning)
                {
                    return false;
                }

                bool activePhase = RuntimeState.phase == StagePhase.Exploration ||
                                   RuntimeState.phase == StagePhase.Bell1 ||
                                   RuntimeState.phase == StagePhase.Bell2 ||
                                   RuntimeState.phase == StagePhase.MaruChase ||
                                   RuntimeState.phase == StagePhase.BossResolved;
                if (!activePhase || (CurrentDefinition.kind == StageKind.Boss && !RuntimeState.bossResolved))
                {
                    return false;
                }

                return SelectAvailableConnection() != null;
            }
        }

        public void Initialize(RunManager manager, SceneTransitionService transitions, GameFlowController applicationFlow)
        {
            runManager = manager ?? throw new ArgumentNullException(nameof(manager));
            sceneTransition = transitions ?? throw new ArgumentNullException(nameof(transitions));
            gameFlow = applicationFlow ?? throw new ArgumentNullException(nameof(applicationFlow));
        }

        public bool EnterStage(
            StageDefinition definition,
            Core04TwoRoomLab lab,
            PlayerMotor2D stagePlayer,
            Camera camera,
            StageGuidanceOverlay overlay)
        {
            if (definition == null || lab == null || stagePlayer == null || camera == null)
            {
                return false;
            }

            var stage = new PendingStage
            {
                Definition = definition,
                Lab = lab,
                Player = stagePlayer,
                Camera = camera,
                Overlay = overlay,
            };

            if (IsStageTransitioning)
            {
                pendingStage = stage;
                return true;
            }

            ActivateStage(stage);
            return true;
        }

        public bool MarkExitDiscovered()
        {
            if (RuntimeState == null || RuntimeState.exitDiscovered || !guidance.MarkExitDiscovered())
            {
                return false;
            }

            RuntimeState.exitDiscovered = true;
            ExitDiscovered?.Invoke(new ExitDiscoveredEvent(CurrentDefinition.stageId, RuntimeState.currentRoomId));
            return true;
        }

        public bool RequestExit()
        {
            StageConnection connection = SelectAvailableConnection();
            if (!CanCommitExit || connection == null || gameFlow == null || !gameFlow.TryBeginStageTransition())
            {
                return false;
            }

            RuntimeState.phase = StagePhase.ExitCommitted;
            PlayerActionLock actionLock = player != null ? player.GetComponent<PlayerActionLock>() : null;
            actionLock?.SetState(PlayerActionState.RoomTransitionLocked);
            player?.ClearBufferedInput();
            currentLab?.TransitionController?.SetExternalBlock(true);
            StageTransitionStarted?.Invoke();
            transitionRoutine = StartCoroutine(TransitionToNextStage(connection));
            return true;
        }

        public bool RequestExitTo(string targetStageId)
        {
            StageConnection connection = SelectAvailableConnection();
            return connection?.target != null &&
                   string.Equals(connection.target.stageId, targetStageId, StringComparison.Ordinal) &&
                   RequestExit();
        }

        public void ResolveBoss()
        {
            if (RuntimeState == null)
            {
                return;
            }

            RuntimeState.bossResolved = true;
            RuntimeState.phase = StagePhase.BossResolved;
        }

        public void SetNarrativeTimerBlocked(bool blocked)
        {
            narrativeTimerBlocked = blocked;
        }

        public void SetShopTimerBlocked(bool blocked)
        {
            shopTimerBlocked = blocked;
        }

        internal BellPhase AdvanceMaruClock(float deltaTime, bool extendTime)
        {
            if (RuntimeState == null || CurrentDefinition == null || IsStageTransitioning || IsMaruTimerBlocked ||
                (RuntimeState.phase != StagePhase.Exploration &&
                 RuntimeState.phase != StagePhase.Bell1 &&
                 RuntimeState.phase != StagePhase.Bell2 &&
                 RuntimeState.phase != StagePhase.MaruChase))
            {
                return RuntimeState?.bellPhase ?? BellPhase.None;
            }

            RuntimeState.elapsedTime += Mathf.Max(0f, deltaTime);
            BellPhase next = MaruTimeline.Evaluate(CurrentDefinition, RuntimeState.elapsedTime, extendTime);
            if ((int)next <= (int)RuntimeState.bellPhase)
            {
                return RuntimeState.bellPhase;
            }

            RuntimeState.bellPhase = next;
            RuntimeState.phase = next switch
            {
                BellPhase.First => StagePhase.Bell1,
                BellPhase.Second => StagePhase.Bell2,
                BellPhase.Maru => StagePhase.MaruChase,
                _ => RuntimeState.phase,
            };
            return next;
        }

        public bool RequestRestartCurrentStage()
        {
            if (!CanRestartCurrentStage || runManager?.Current == null || gameFlow == null)
            {
                return false;
            }

            if (gameFlow.State == GameApplicationState.Paused && !gameFlow.TryResume())
            {
                return false;
            }
            if (!gameFlow.TryBeginStageTransition())
            {
                return false;
            }

            stageEntrySnapshot.RestoreInto(runManager.Current);
            runManager.Current.stageRestartCount++;
            runManager.Current.actionRecords.Add(new ActionRecord
            {
                actionId = "stage_restart:" + CurrentDefinition.stageId,
                stageId = CurrentDefinition.stageId,
                elapsedTime = 0f,
            });
            StageTransitionStarted?.Invoke();
            transitionRoutine = StartCoroutine(RestartCurrentStageRoutine(CurrentDefinition.sceneName));
            return true;
        }

        private void ActivateStage(PendingStage stage)
        {
            narrativeTimerBlocked = false;
            shopTimerBlocked = false;
            if (currentLab != null && currentLab.TransitionController != null)
            {
                currentLab.TransitionController.RoomChanged -= HandleRoomChanged;
            }
            if (currentLab != null && currentLab.SecretDimensionController != null)
            {
                currentLab.SecretDimensionController.SecretRoomCreated -= HandleSecretRoomCreated;
            }

            CurrentDefinition = stage.Definition;
            currentLab = stage.Lab;
            player = stage.Player;
            worldCamera = stage.Camera;
            StageAssemblyResult assembly = assembler.Assemble(currentLab);
            currentLab.ApplyArtProfile(CurrentDefinition.artProfile);
            roomGraph.Clear();
            for (int index = 0; index < assembly.Rooms.Count; index++)
            {
                roomGraph.AddRoom(assembly.Rooms[index], index == 0);
                if (index > 0)
                {
                    roomGraph.ConnectBidirectional(assembly.Rooms[index - 1].RoomId, assembly.Rooms[index].RoomId);
                }
            }
            currentLab.InitializePlayerAndCamera(player, worldCamera);
            currentLab.TransitionController.RoomChanged += HandleRoomChanged;
            currentLab.SecretDimensionController.SecretRoomCreated += HandleSecretRoomCreated;
            currentLab.TransitionController.SetExternalBlock(false);

            int seed = runManager?.Current?.seed ?? 0;
            RuntimeState = StageRuntimeState.Create(CurrentDefinition, seed, assembly.StartRoom.RoomId);
            for (int index = 0; index < assembly.Rooms.Count; index++)
            {
                RuntimeState.rooms[assembly.Rooms[index].RoomId] = assembly.Rooms[index].PersistentState;
            }

            bool hasExit = HasVisibleExitConnection();
            var routeRooms = new StageRouteRoom[assembly.Rooms.Count];
            var routeEdges = hasExit && assembly.Rooms.Count > 1
                ? new StageRouteEdge[assembly.Rooms.Count - 1]
                : Array.Empty<StageRouteEdge>();
            for (int index = 0; index < assembly.Rooms.Count; index++)
            {
                routeRooms[index] = new StageRouteRoom(assembly.Rooms[index].RoomId, assembly.Rooms[index].WorldBounds.center);
                if (index > 0 && hasExit)
                {
                    routeEdges[index - 1] = new StageRouteEdge(assembly.Rooms[index - 1].RoomId, assembly.Rooms[index].RoomId);
                }
            }
            guidance.Configure(routeRooms, routeEdges, hasExit ? assembly.ExitRoom.RoomId : string.Empty);

            CurrentExit = hasExit ? BuildExitDoor(assembly.ExitRoom) : null;
            StagePlayerActionExecutor executor = player.GetComponent<StagePlayerActionExecutor>();
            if (executor == null)
            {
                executor = player.gameObject.AddComponent<StagePlayerActionExecutor>();
            }
            executor.Configure(player, CurrentExit);

            if (player.Body != null)
            {
                player.Body.simulated = true;
            }
            player.GetComponent<PlayerActionLock>()?.ResetToFree();
            stage.Overlay?.Bind(this);

            RunState run = runManager?.Current;
            if (run != null)
            {
                run.currentStageId = CurrentDefinition.stageId;
                run.visitedStages?.Add(CurrentDefinition.stageId);
                stageEntrySnapshot = StageEntrySnapshot.Capture(run);
            }

            RuntimeState.phase = StagePhase.Intro;
            if (!string.IsNullOrWhiteSpace(CurrentDefinition.introYarnNode))
            {
                StageIntroRequested?.Invoke(CurrentDefinition.introYarnNode);
            }
            RuntimeState.phase = CurrentDefinition.kind == StageKind.Boss ? StagePhase.BossIntro : StagePhase.Exploration;
            FadeOpacity = 0f;
            StageLoaded?.Invoke(new StageLoadedEvent(CurrentDefinition.stageId, CurrentDefinition.sceneName));
        }

        private StageExitDoor BuildExitDoor(RoomRuntime exitRoom)
        {
            Transform parent = exitRoom.transform.Find("DynamicRoot");
            if (parent == null)
            {
                parent = exitRoom.transform;
            }

            Transform existing = parent.Find("StageExitDoor");
            GameObject doorObject = existing != null ? existing.gameObject : new GameObject("StageExitDoor");
            doorObject.transform.SetParent(parent, false);
            doorObject.transform.localPosition = new Vector3(20.5f, 1.46f, 0f);
            StageExitDoor door = doorObject.GetComponent<StageExitDoor>();
            if (door == null)
            {
                door = doorObject.AddComponent<StageExitDoor>();
            }
            door.Configure(player, player.GetComponent<GameplayInputReader>(), this, worldCamera);
            return door;
        }

        private IEnumerator TransitionToNextStage(StageConnection connection)
        {
            StageRuntimeState departingState = RuntimeState;
            StageDefinition departingDefinition = CurrentDefinition;
            PlayerActionLock actionLock = player != null ? player.GetComponent<PlayerActionLock>() : null;

            yield return new WaitForSecondsRealtime(StageExitDoor.ExitAnimationSeconds);
            CommitStageResult(departingDefinition, departingState);
            if (player != null && player.Body != null)
            {
                player.Body.linearVelocity = Vector2.zero;
                player.Body.simulated = false;
            }
            FadeOpacity = 1f;

            yield return sceneTransition.LoadAdditive(connection.target.sceneName);
            if (!sceneTransition.LastOperationSucceeded)
            {
                RestoreFailedTransition(departingState, actionLock);
                yield break;
            }

            yield return null;
            if (pendingStage == null)
            {
                RestoreFailedTransition(departingState, actionLock);
                yield break;
            }

            yield return sceneTransition.Unload(departingDefinition.sceneName);
            if (!sceneTransition.LastOperationSucceeded)
            {
                RestoreFailedTransition(departingState, actionLock);
                yield break;
            }

            departingState.phase = StagePhase.Complete;
            PendingStage next = pendingStage;
            pendingStage = null;
            ActivateStage(next);
            transitionRoutine = null;
            gameFlow.CompleteStageTransition(true);
        }

        private IEnumerator RestartCurrentStageRoutine(string sceneName)
        {
            if (player != null && player.Body != null)
            {
                player.Body.linearVelocity = Vector2.zero;
                player.Body.simulated = false;
            }
            player?.GetComponent<PlayerActionLock>()?.SetState(PlayerActionState.RoomTransitionLocked);
            currentLab?.TransitionController?.SetExternalBlock(true);
            FadeOpacity = 1f;
            narrativeTimerBlocked = false;
            shopTimerBlocked = false;

            yield return new WaitForSecondsRealtime(0.08f);
            yield return sceneTransition.Unload(sceneName);
            if (!sceneTransition.LastOperationSucceeded)
            {
                transitionRoutine = null;
                gameFlow.CompleteStageTransition(false);
                yield break;
            }

            CurrentDefinition = null;
            RuntimeState = null;
            CurrentExit = null;
            roomGraph.Clear();
            currentLab = null;
            player = null;
            worldCamera = null;

            yield return sceneTransition.LoadAdditive(sceneName);
            if (!sceneTransition.LastOperationSucceeded)
            {
                transitionRoutine = null;
                gameFlow.ReturnToTitle();
                yield break;
            }

            yield return null;
            if (pendingStage == null)
            {
                transitionRoutine = null;
                gameFlow.ReturnToTitle();
                yield break;
            }

            PendingStage reloaded = pendingStage;
            pendingStage = null;
            ActivateStage(reloaded);
            transitionRoutine = null;
            gameFlow.CompleteStageTransition(true);
        }

        private void RestoreFailedTransition(StageRuntimeState departingState, PlayerActionLock actionLock)
        {
            pendingStage = null;
            if (player != null && player.Body != null)
            {
                player.Body.simulated = true;
            }
            actionLock?.ResetToFree();
            currentLab?.TransitionController?.SetExternalBlock(false);
            departingState.phase = StagePhase.Exploration;
            FadeOpacity = 0f;
            transitionRoutine = null;
            gameFlow.CompleteStageTransition(false);
        }

        private void HandleRoomChanged(RoomChangedEvent change)
        {
            if (RuntimeState != null)
            {
                RuntimeState.currentRoomId = change.To;
                RuntimeState.visitedRoomIds?.Add(change.To);
            }
            RoomChanged?.Invoke(change);
        }

        private void HandleSecretRoomCreated(SecretAnchor anchor, RoomRuntime secretRoom)
        {
            if (anchor == null || anchor.SourceRoom == null || secretRoom == null)
            {
                return;
            }

            roomGraph.AddRoom(secretRoom);
            roomGraph.ConnectBidirectional(anchor.SourceRoom.RoomId, secretRoom.RoomId);
            if (RuntimeState != null)
            {
                RuntimeState.rooms[secretRoom.RoomId] = secretRoom.PersistentState;
            }
        }

        private StageConnection SelectAvailableConnection()
        {
            if (CurrentDefinition?.connections == null)
            {
                return null;
            }

            RunState run = runManager?.Current;
            for (int index = 0; index < CurrentDefinition.connections.Length; index++)
            {
                StageConnection connection = CurrentDefinition.connections[index];
                if (StageConnectionEvaluator.IsAvailable(connection, run, RuntimeState) && connection.target.HasScene)
                {
                    return connection;
                }
            }

            return null;
        }

        private bool HasVisibleExitConnection()
        {
            if (CurrentDefinition?.connections == null)
            {
                return false;
            }

            RunState run = runManager?.Current;
            for (int index = 0; index < CurrentDefinition.connections.Length; index++)
            {
                StageConnection connection = CurrentDefinition.connections[index];
                if (connection != null && connection.target != null &&
                    (StageConnectionEvaluator.IsAvailable(connection, run, RuntimeState) || connection.visibleWhenLocked))
                {
                    return true;
                }
            }

            return false;
        }

        private void CommitStageResult(StageDefinition definition, StageRuntimeState state)
        {
            RunState run = runManager?.Current;
            if (run?.actionRecords == null)
            {
                return;
            }

            run.actionRecords.Add(new ActionRecord
            {
                actionId = "stage_exit:" + definition.stageId,
                stageId = definition.stageId,
                elapsedTime = state.elapsedTime,
            });
        }
    }
}

#endif
