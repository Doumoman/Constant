#if LEGACY_DISABLED
using System;
using StarNight.Core.Flow;
using StarNight.Core.State;
using StarNight.Interaction.HandSlot;
using StarNight.Stage.Flow;
using UnityEngine;
using Yarn.Unity;

namespace StarNight.Narrative
{
    [DisallowMultipleComponent]
    public sealed class NarrativeCommandBridge : MonoBehaviour
    {
        private DialogueRunner runner;
        private NarrativeService service;
        private NarrativeUIState uiState;
        private RunManager runManager;
        private StageFlowController stageFlow;
        private PlayerHandSlot handSlot;
        private bool registered;

        public event Action<NarrativeRequestEvent> CameraFocusRequested;
        public event Action<NarrativeRequestEvent> SfxRequested;
        public event Action<NarrativeRequestEvent> BgmRequested;
        public event Action<NarrativeRequestEvent> RoomEventRequested;
        public event Action<NarrativeRequestEvent> ToolFocusRequested;
        public event Action<NarrativeRequestEvent> ControlHintRequested;

        public int InvalidRequestCount { get; private set; }
        public string LastInvalidRequest { get; private set; } = string.Empty;

        public void Configure(DialogueRunner dialogueRunner, NarrativeService narrativeService, NarrativeUIState state)
        {
            runner = dialogueRunner ?? throw new ArgumentNullException(nameof(dialogueRunner));
            service = narrativeService ?? throw new ArgumentNullException(nameof(narrativeService));
            uiState = state ?? throw new ArgumentNullException(nameof(state));
            ResolveAuthorities();
            RegisterActions();
        }

        public void ConfigureAuthoritiesForTests(RunManager manager, StageFlowController flow = null)
        {
            runManager = manager;
            stageFlow = flow;
        }

        public bool HasFlag(string flagId) => CurrentRun?.flags?.Contains(flagId) ?? false;
        public bool HasItem(string itemId) => CurrentRun?.items?.Contains(itemId) ?? false;
        public bool StageIs(string stageId) => string.Equals(CurrentRun?.currentStageId, stageId, StringComparison.Ordinal);
        public bool RouteIs(string routeId) => string.Equals(CurrentRun?.selectedRoute, routeId, StringComparison.Ordinal);
        public float Health() => CurrentRun?.health ?? 0;
        public float MoneyWon() => CurrentRun?.moneyWon ?? 0;
        public float BellPhase() => stageFlow?.RuntimeState == null ? 0f : (float)stageFlow.RuntimeState.bellPhase;
        public bool HasTool(string toolId)
        {
            ResolveAuthorities();
            if (handSlot != null)
            {
                return handSlot.CurrentItem is IHandSlotHudSource status
                    && status.IsHandTool
                    && string.Equals(status.StableItemId, toolId, StringComparison.Ordinal);
            }
            return string.Equals(CurrentRun?.handToolId, toolId, StringComparison.Ordinal);
        }

        public bool HandSlotEmpty()
        {
            ResolveAuthorities();
            return handSlot != null
                ? handSlot.IsEmpty
                : string.IsNullOrWhiteSpace(CurrentRun?.handToolId);
        }

        public float BombCount() => CurrentRun?.bombs ?? 0;
        public float RopeCount() => CurrentRun?.ropes ?? 0;

        public bool TryFocusTool(string toolId)
        {
            if (string.IsNullOrWhiteSpace(toolId))
            {
                return Invalid("focus_tool", toolId);
            }
            ToolFocusRequested?.Invoke(new NarrativeRequestEvent { Id = toolId });
            return true;
        }

        public bool TryShowControlHint(string actionId)
        {
            if (string.IsNullOrWhiteSpace(actionId))
            {
                return Invalid("show_control_hint", actionId);
            }
            ControlHintRequested?.Invoke(new NarrativeRequestEvent { Id = actionId });
            return true;
        }

        public bool TrySetMode(string mode)
        {
            switch (mode?.Trim().ToLowerInvariant())
            {
                case "conversation": service.SetMode(NarrativeMode.Conversation); return true;
                case "bubble": service.SetMode(NarrativeMode.Bubble); return true;
                case "narration": service.SetMode(NarrativeMode.Narration); return true;
                default: return Invalid("ui_mode", mode);
            }
        }

        public bool TryRequestFlag(string flagId, bool enabled)
        {
            RunState run = CurrentRun;
            if (run?.flags == null || string.IsNullOrWhiteSpace(flagId))
            {
                return Invalid("request_flag", flagId);
            }
            if (enabled) run.flags.Add(flagId); else run.flags.Remove(flagId);
            return true;
        }

        public bool TryRequestGive(string itemId, int amount)
        {
            RunState run = CurrentRun;
            if (run?.items == null || string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            {
                return Invalid("request_give", itemId);
            }
            run.items.Add(itemId);
            return true;
        }

        public bool TryRequestTake(string itemId, int amount)
        {
            RunState run = CurrentRun;
            if (run?.items == null || string.IsNullOrWhiteSpace(itemId) || amount <= 0 || !run.items.Contains(itemId))
            {
                return Invalid("request_take", itemId);
            }
            run.items.Remove(itemId);
            return true;
        }

        public bool TryRequestMoney(int deltaWon, string reasonId)
        {
            RunState run = CurrentRun;
            if (run == null || string.IsNullOrWhiteSpace(reasonId) || (long)run.moneyWon + deltaWon < 0)
            {
                return Invalid("request_money", reasonId);
            }
            run.moneyWon += deltaWon;
            return true;
        }

        public bool TryRequestStageTransition(string stageId)
        {
            ResolveAuthorities();
            if (stageFlow == null || string.IsNullOrWhiteSpace(stageId) || !stageFlow.RequestExitTo(stageId))
            {
                return Invalid("request_stage_transition", stageId);
            }
            return true;
        }

        private RunState CurrentRun
        {
            get
            {
                ResolveAuthorities();
                return runManager?.Current;
            }
        }

        private void RegisterActions()
        {
            if (registered)
            {
                return;
            }
            registered = true;

            runner.AddFunction("has_flag", (Func<string, bool>)HasFlag);
            runner.AddFunction("has_item", (Func<string, bool>)HasItem);
            runner.AddFunction("stage_is", (Func<string, bool>)StageIs);
            runner.AddFunction("route_is", (Func<string, bool>)RouteIs);
            runner.AddFunction("health", (Func<float>)Health);
            runner.AddFunction("money_won", (Func<float>)MoneyWon);
            runner.AddFunction("bell_phase", (Func<float>)BellPhase);
            runner.AddFunction("has_tool", (Func<string, bool>)HasTool);
            runner.AddFunction("hand_slot_empty", (Func<bool>)HandSlotEmpty);
            runner.AddFunction("bomb_count", (Func<float>)BombCount);
            runner.AddFunction("rope_count", (Func<float>)RopeCount);

            runner.AddCommandHandler<string>("ui_mode", HandleMode);
            runner.AddCommandHandler<bool>("lock_player", HandlePlayerLock);
            runner.AddCommandHandler<string, float>("focus_camera", HandleFocusCamera);
            runner.AddCommandHandler<string>("play_sfx", HandlePlaySfx);
            runner.AddCommandHandler<string, float>("play_bgm", HandlePlayBgm);
            runner.AddCommandHandler<string, string>("set_expression", HandleSetExpression);
            runner.AddCommandHandler<string, bool>("request_flag", HandleRequestFlag);
            runner.AddCommandHandler<string, int>("request_give", HandleRequestGive);
            runner.AddCommandHandler<string, int>("request_take", HandleRequestTake);
            runner.AddCommandHandler<int, string>("request_money", HandleRequestMoney);
            runner.AddCommandHandler<string>("request_room_event", HandleRoomEvent);
            runner.AddCommandHandler<string>("request_stage_transition", HandleStageTransition);
            runner.AddCommandHandler<string>("focus_tool", HandleFocusTool);
            runner.AddCommandHandler<string>("show_control_hint", HandleShowControlHint);
            runner.AddCommandHandler<float>("wait_game", WaitGame);

            runner.onUnhandledCommand ??= new UnityEventString();
            runner.onUnhandledCommand.AddListener(HandleUnhandledCommand);
        }

        private void OnDestroy()
        {
            if (runner != null && runner.onUnhandledCommand != null)
            {
                runner.onUnhandledCommand.RemoveListener(HandleUnhandledCommand);
            }
        }

        private void HandleMode(string mode) => TrySetMode(mode);
        private void HandlePlayerLock(bool value) => service.SetBlocking(value);
        private void HandleRequestFlag(string id, bool value) => TryRequestFlag(id, value);
        private void HandleRequestGive(string id, int amount) => TryRequestGive(id, amount);
        private void HandleRequestTake(string id, int amount) => TryRequestTake(id, amount);
        private void HandleRequestMoney(int delta, string reason) => TryRequestMoney(delta, reason);
        private void HandleStageTransition(string id) => TryRequestStageTransition(id);
        private void HandleFocusTool(string id) => TryFocusTool(id);
        private void HandleShowControlHint(string id) => TryShowControlHint(id);

        private void HandleFocusCamera(string targetId, float duration)
        {
            if (string.IsNullOrWhiteSpace(targetId) || duration < 0f)
            {
                Invalid("focus_camera", targetId);
                return;
            }
            CameraFocusRequested?.Invoke(new NarrativeRequestEvent { Id = targetId, Value = duration });
        }

        private void HandlePlaySfx(string sfxId)
        {
            if (string.IsNullOrWhiteSpace(sfxId))
            {
                Invalid("play_sfx", sfxId);
                return;
            }
            SfxRequested?.Invoke(new NarrativeRequestEvent { Id = sfxId });
        }

        private void HandlePlayBgm(string bgmId, float fadeSeconds)
        {
            if (string.IsNullOrWhiteSpace(bgmId) || fadeSeconds < 0f)
            {
                Invalid("play_bgm", bgmId);
                return;
            }
            BgmRequested?.Invoke(new NarrativeRequestEvent { Id = bgmId, Value = fadeSeconds });
        }

        private void HandleSetExpression(string characterId, string expressionId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(expressionId))
            {
                Invalid("set_expression", characterId);
                return;
            }
            uiState.SetExpression(characterId, expressionId);
        }

        private void HandleRoomEvent(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                Invalid("request_room_event", eventId);
                return;
            }
            RoomEventRequested?.Invoke(new NarrativeRequestEvent { Id = eventId });
        }

        private async YarnTask WaitGame(float seconds)
        {
            if (seconds < 0f)
            {
                Invalid("wait_game", seconds.ToString());
                return;
            }
            await YarnTask.Delay(Mathf.RoundToInt(seconds * 1000f));
        }

        private void HandleUnhandledCommand(string command)
        {
            Invalid("unhandled", command);
        }

        private bool Invalid(string command, string argument)
        {
            InvalidRequestCount++;
            LastInvalidRequest = command + ":" + (argument ?? string.Empty);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError($"Narrative command rejected: {LastInvalidRequest}", this);
#endif
            return false;
        }

        private void ResolveAuthorities()
        {
            if (handSlot == null)
            {
                handSlot = FindFirstObjectByType<PlayerHandSlot>();
            }
            if (!GameBootstrap.IsReady)
            {
                return;
            }
            GameBootstrap.Instance.Services.TryGet(out runManager);
            GameBootstrap.Instance.Services.TryGet(out stageFlow);
        }
    }
}

#endif
