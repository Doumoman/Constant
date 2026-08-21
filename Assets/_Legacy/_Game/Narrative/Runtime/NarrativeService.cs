#if LEGACY_DISABLED
using System;
using System.Linq;
using StarNight.Core.Flow;
using StarNight.Interaction.Input;
using StarNight.Player.Motor;
using StarNight.Stage.Flow;
using UnityEngine;
using UnityEngine.Events;
using Yarn.Unity;

namespace StarNight.Narrative
{
    [DisallowMultipleComponent]
    public sealed class NarrativeService : MonoBehaviour
    {
        private readonly NarrativeRequestQueue queue = new();
        private DialogueRunner runner;
        private NarrativeUIState uiState;
        private NarrativeRequest activeRequest;
        private bool hasActiveRequest;
        private PlayerActionLock actionLock;
        private GameplayInputReader inputReader;
        private PlayerActionState previousActionState = PlayerActionState.Free;
        private PlayerInputContext previousInputContext = PlayerInputContext.Gameplay;
        private StageFlowController stageFlow;
        private StageFlowController subscribedStageFlow;
        private bool lockApplied;

        public event Action<NarrativeRequest> DialogueBegan;
        public event Action<NarrativeRequest> DialogueEnded;

        public DialogueRunner Runner => runner;
        public NarrativeUIState UIState => uiState;
        public bool HasActiveRequest => hasActiveRequest;
        public NarrativeRequest ActiveRequest => activeRequest;
        public int QueuedCount => queue.Count;

        public void Configure(DialogueRunner dialogueRunner, NarrativeUIState state)
        {
            runner = dialogueRunner ?? throw new ArgumentNullException(nameof(dialogueRunner));
            uiState = state ?? throw new ArgumentNullException(nameof(state));
            runner.onDialogueComplete ??= new UnityEvent();
            runner.onDialogueComplete.RemoveListener(HandleDialogueComplete);
            runner.onDialogueComplete.AddListener(HandleDialogueComplete);
            RefreshStageFlowSubscription();
        }

        private void Update()
        {
            if (subscribedStageFlow == null)
            {
                RefreshStageFlowSubscription();
            }
        }

        private void OnDestroy()
        {
            if (runner != null && runner.onDialogueComplete != null)
            {
                runner.onDialogueComplete.RemoveListener(HandleDialogueComplete);
            }
            if (subscribedStageFlow != null)
            {
                subscribedStageFlow.StageIntroRequested -= HandleStageIntroRequested;
                subscribedStageFlow.StageTransitionStarted -= HandleStageTransitionStarted;
            }
            ReleaseGameplayLock();
            queue.Clear();
        }

        public bool TryRunNode(
            string nodeName,
            NarrativeMode mode = NarrativeMode.Conversation,
            bool blocksGameplay = true,
            bool essential = true)
        {
            if (runner == null || runner.YarnProject == null || string.IsNullOrWhiteSpace(nodeName))
            {
                return false;
            }

            string compiledNodeName = ResolveCompiledNodeName(nodeName);
            if (compiledNodeName == null)
            {
                DevelopmentError($"Yarn node '{nodeName}' does not exist in the active project.");
                return false;
            }

            if ((hasActiveRequest && string.Equals(activeRequest.NodeName, nodeName, StringComparison.Ordinal)) || queue.Contains(nodeName))
            {
                return false;
            }

            var request = new NarrativeRequest(nodeName, mode, blocksGameplay, essential);
            if (hasActiveRequest || runner.IsDialogueRunning)
            {
                return queue.Enqueue(request);
            }

            Begin(request);
            return true;
        }

        public void SetMode(NarrativeMode mode)
        {
            uiState?.SetMode(mode);
        }

        public void SetBlocking(bool blocked)
        {
            if (!hasActiveRequest || activeRequest.BlocksGameplay == blocked)
            {
                return;
            }

            activeRequest = new NarrativeRequest(activeRequest.NodeName, activeRequest.Mode, blocked, activeRequest.Essential);
            uiState?.Begin(uiState.Mode, blocked);
            if (blocked)
            {
                ApplyGameplayLock();
            }
            else
            {
                ReleaseGameplayLock();
            }
        }

        public void StopDialogue()
        {
            if (runner != null && runner.IsDialogueRunning)
            {
                runner.Stop().Forget();
            }
        }

        private void Begin(NarrativeRequest request)
        {
            activeRequest = request;
            hasActiveRequest = true;
            uiState.Begin(request.Mode, request.BlocksGameplay);
            if (request.BlocksGameplay)
            {
                ApplyGameplayLock();
            }
            DialogueBegan?.Invoke(request);
            string compiledNodeName = ResolveCompiledNodeName(request.NodeName);
            if (compiledNodeName == null)
            {
                HandleDialogueComplete();
                return;
            }
            runner.StartDialogue(compiledNodeName).Forget();
        }

        private void HandleDialogueComplete()
        {
            if (hasActiveRequest)
            {
                NarrativeRequest completed = activeRequest;
                ReleaseGameplayLock();
                uiState.Clear();
                hasActiveRequest = false;
                DialogueEnded?.Invoke(completed);
            }

            if (queue.TryDequeue(out NarrativeRequest next))
            {
                Begin(next);
            }
        }

        private void ApplyGameplayLock()
        {
            if (lockApplied)
            {
                return;
            }

            PlayerMotor2D player = FindFirstObjectByType<PlayerMotor2D>();
            actionLock = player != null ? player.GetComponent<PlayerActionLock>() : FindFirstObjectByType<PlayerActionLock>();
            inputReader = player != null ? player.GetComponent<GameplayInputReader>() : FindFirstObjectByType<GameplayInputReader>();
            if (actionLock != null)
            {
                previousActionState = actionLock.State;
                actionLock.SetState(PlayerActionState.DialogueLocked);
            }
            if (inputReader != null)
            {
                previousInputContext = inputReader.Context;
                inputReader.ClearBufferedButtons();
                inputReader.SetContext(PlayerInputContext.Dialogue);
            }

            ResolveStageFlow()?.SetNarrativeTimerBlocked(true);
            player?.ClearBufferedInput();
            lockApplied = true;
        }

        private void ReleaseGameplayLock()
        {
            if (!lockApplied)
            {
                return;
            }

            if (actionLock != null && actionLock.State == PlayerActionState.DialogueLocked)
            {
                actionLock.SetState(previousActionState);
            }
            if (inputReader != null && inputReader.Context == PlayerInputContext.Dialogue)
            {
                inputReader.ClearBufferedButtons();
                inputReader.SetContext(previousInputContext);
            }

            ResolveStageFlow()?.SetNarrativeTimerBlocked(false);
            actionLock = null;
            inputReader = null;
            lockApplied = false;
        }

        private StageFlowController ResolveStageFlow()
        {
            if (stageFlow != null)
            {
                return stageFlow;
            }
            if (GameBootstrap.IsReady && GameBootstrap.Instance.Services.TryGet(out StageFlowController registered))
            {
                stageFlow = registered;
            }
            else
            {
                stageFlow = FindFirstObjectByType<StageFlowController>();
            }
            return stageFlow;
        }

        private void RefreshStageFlowSubscription()
        {
            StageFlowController resolved = ResolveStageFlow();
            if (resolved == null || resolved == subscribedStageFlow)
            {
                return;
            }
            if (subscribedStageFlow != null)
            {
                subscribedStageFlow.StageIntroRequested -= HandleStageIntroRequested;
                subscribedStageFlow.StageTransitionStarted -= HandleStageTransitionStarted;
            }
            subscribedStageFlow = resolved;
            subscribedStageFlow.StageIntroRequested += HandleStageIntroRequested;
            subscribedStageFlow.StageTransitionStarted += HandleStageTransitionStarted;
        }

        private void HandleStageIntroRequested(string nodeName)
        {
            TryRunNode(nodeName, NarrativeMode.Conversation, true, true);
        }

        private void HandleStageTransitionStarted()
        {
            queue.Clear();
            if (hasActiveRequest)
            {
                StopDialogue();
            }
        }

        private string ResolveCompiledNodeName(string canonicalNodeName)
        {
            if (runner?.YarnProject == null)
            {
                return null;
            }
            string[] nodeNames = runner.YarnProject.NodeNames;
            if (nodeNames.Contains(canonicalNodeName, StringComparer.Ordinal))
            {
                return canonicalNodeName;
            }

            string yarnSafeName = canonicalNodeName?.Replace('.', '_');
            return nodeNames.Contains(yarnSafeName, StringComparer.Ordinal) ? yarnSafeName : null;
        }

        private static void DevelopmentError(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError(message);
#endif
        }
    }
}

#endif
