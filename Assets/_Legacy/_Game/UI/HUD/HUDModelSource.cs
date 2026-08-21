#if LEGACY_DISABLED
using System;
using StarNight.Core.Flow;
using StarNight.Core.State;
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.Input;
using StarNight.Interaction.Targeting;
using StarNight.Stage.Data;
using StarNight.Stage.Exit;
using StarNight.Stage.Flow;
using StarNight.Stage.Guidance;
using StarNight.Stage.Maru;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StarNight.UI.HUD
{
    [DisallowMultipleComponent]
    public sealed class HUDModelSource : MonoBehaviour
    {
        private readonly HUDModel model = new HUDModel();
        private RunManager runManager;
        private GameFlowController gameFlow;
        private StageFlowController stageFlow;
        private MaruDirector maruDirector;
        private GameplayInputReader inputReader;
        private PlayerHandSlot handSlot;
        private IEquipmentInventoryBridge equipmentInventory;
        private InteractionProbe interactionProbe;
        private StageRuntimeState observedStage;
        private object observedDefinition;
        private string observedRoomId = string.Empty;
        private int observedVisitedCount = -1;
        private bool observedExitDiscovered;
        private bool initialRunRead;
        private float moneyDeltaVisibleUntil;
        private float stageNameVisibleUntil;
        private float equipmentFeedbackVisibleUntil;

        public event Action<HUDModel> ModelChanged;

        public HUDModel Model => model;

        private void Awake()
        {
            model.visibility = HUDVisibility.Hidden;
            model.inputDevice = InputDisplayDevice.Keyboard;
            UpdateGlyphs();
        }

        private void Update()
        {
            ResolveDependencies();
            DetectLastUsedDevice();
            RefreshModel();
        }

        public void SetMapOpen(bool open)
        {
            bool allowed = open && stageFlow?.RuntimeState != null;
            if (model.mapOpen == allowed)
            {
                return;
            }

            model.mapOpen = allowed;
            Publish();
        }

        public void ForceInputDeviceForTests(InputDisplayDevice device)
        {
            SetInputDevice(device);
        }

        public void RefreshForTests()
        {
            ResolveDependencies();
            RefreshModel();
        }

        private void ResolveDependencies()
        {
            if (GameBootstrap.IsReady)
            {
                GameBootstrap.Instance.Services.TryGet(out runManager);
                GameBootstrap.Instance.Services.TryGet(out gameFlow);
                GameBootstrap.Instance.Services.TryGet(out stageFlow);
                GameBootstrap.Instance.Services.TryGet(out maruDirector);
            }

            if (inputReader == null)
            {
                inputReader = UnityEngine.Object.FindFirstObjectByType<GameplayInputReader>();
                UpdateGlyphs();
            }

            if (handSlot == null)
            {
                handSlot = UnityEngine.Object.FindFirstObjectByType<PlayerHandSlot>();
            }
            if (equipmentInventory == null && handSlot != null)
            {
                MonoBehaviour[] behaviours = handSlot.GetComponents<MonoBehaviour>();
                for (int index = 0; index < behaviours.Length; index++)
                {
                    if (behaviours[index] is IEquipmentInventoryBridge inventory)
                    {
                        equipmentInventory = inventory;
                        break;
                    }
                }
            }
            if (interactionProbe == null)
            {
                interactionProbe = UnityEngine.Object.FindFirstObjectByType<InteractionProbe>();
            }
        }

        private void DetectLastUsedDevice()
        {
            Gamepad gamepad = Gamepad.current;
            bool gamepadUsed = gamepad != null &&
                (gamepad.buttonWest.wasPressedThisFrame ||
                 gamepad.buttonSouth.wasPressedThisFrame ||
                 gamepad.buttonEast.wasPressedThisFrame ||
                 gamepad.buttonNorth.wasPressedThisFrame ||
                 gamepad.startButton.wasPressedThisFrame ||
                 gamepad.selectButton.wasPressedThisFrame ||
                 gamepad.leftStick.ReadValue().sqrMagnitude > 0.25f ||
                 gamepad.dpad.ReadValue().sqrMagnitude > 0.25f);
            bool keyboardUsed = Keyboard.current?.anyKey.wasPressedThisFrame == true;

            if (gamepadUsed)
            {
                SetInputDevice(InputDisplayDevice.Gamepad);
            }
            else if (keyboardUsed)
            {
                SetInputDevice(InputDisplayDevice.Keyboard);
            }
        }

        private void SetInputDevice(InputDisplayDevice device)
        {
            if (model.inputDevice == device)
            {
                return;
            }

            model.inputDevice = device;
            UpdateGlyphs();
            Publish();
        }

        private void UpdateGlyphs()
        {
            model.primaryGlyph = InputGlyphResolver.Resolve(inputReader, "PrimaryAction", model.InputDevice);
            model.downPrimaryGlyph = model.InputDevice == InputDisplayDevice.Keyboard
                ? "S+" + model.PrimaryGlyph
                : "D-PAD DOWN+" + model.PrimaryGlyph;
            model.mapGlyph = InputGlyphResolver.Resolve(inputReader, "OpenMap", model.InputDevice);
        }

        private void RefreshModel()
        {
            bool changed = false;
            RunState run = runManager?.Current;
            StageRuntimeState stage = stageFlow?.RuntimeState;

            int health = run?.health ?? 4;
            bool lantern = run?.lanternAvailable ?? true;
            int money = run?.moneyWon ?? 0;
            int ropes = run?.ropes ?? 4;
            int bombs = run?.bombs ?? 4;
            HandSlotItemRuntime handItem = handSlot != null ? handSlot.CurrentItem : null;
            IHandSlotHudSource handStatus = handItem as IHandSlotHudSource;
            string handTool = handStatus != null && handStatus.IsHandTool
                ? handStatus.StableItemId
                : handItem == null ? run?.handToolId ?? string.Empty : string.Empty;
            bool handOccupied = handItem != null || !string.IsNullOrWhiteSpace(handTool);
            string handDisplayName = handStatus?.DisplayName ?? handTool;
            Sprite handIcon = handStatus?.HudIcon;
            bool resourceVisible = handStatus?.ShowResource ?? false;
            int resourceCurrent = handStatus?.CurrentResource ?? 0;
            int resourceMaximum = handStatus?.MaximumResource ?? 0;
            string handAction = handStatus?.PrimaryActionLabel
                ?? (handOccupied ? "사용" : string.Empty);

            if (model.MoneyWon != money)
            {
                if (initialRunRead)
                {
                    model.moneyDelta = money - model.MoneyWon;
                    moneyDeltaVisibleUntil = Time.unscaledTime + 0.7f;
                }
                model.moneyWon = money;
                changed = true;
            }
            initialRunRead = run != null;

            changed |= Assign(ref model.health, health);
            changed |= Assign(ref model.lanternAvailable, lantern);
            changed |= Assign(ref model.ropes, ropes);
            changed |= Assign(ref model.bombs, bombs);
            changed |= Assign(ref model.handToolId, handTool);
            changed |= Assign(ref model.handSlotOccupied, handOccupied);
            changed |= Assign(ref model.handDisplayName, handDisplayName);
            if (model.handIcon != handIcon)
            {
                model.handIcon = handIcon;
                changed = true;
            }
            changed |= Assign(ref model.handResourceVisible, resourceVisible);
            changed |= Assign(ref model.handResourceCurrent, resourceCurrent);
            changed |= Assign(ref model.handResourceMaximum, resourceMaximum);
            changed |= Assign(ref model.handPrimaryActionLabel, handAction);
            changed |= SynchronizeEquipment();

            if (model.MoneyDelta != 0 && Time.unscaledTime >= moneyDeltaVisibleUntil)
            {
                model.moneyDelta = 0;
                changed = true;
            }

            BellPhase bellPhase = stage?.bellPhase ?? BellPhase.None;
            changed |= Assign(ref model.bellPhase, bellPhase);

            bool maruChasing = maruDirector?.IsChasing ?? false;
            changed |= Assign(ref model.maruChasing, maruChasing);
            Vector2Int approach = maruDirector?.ApproachDirection ?? Vector2Int.zero;
            if (model.MaruApproachDirection != approach)
            {
                model.maruApproachDirection = approach;
                changed = true;
            }
            int remainingSeconds = maruDirector == null ? 0 : Mathf.CeilToInt(maruDirector.RemainingSeconds);
            changed |= Assign(ref model.maruRemainingSeconds, remainingSeconds);
            bool showTimer = GameBootstrap.Instance?.Settings?.gameplay?.showTimerNumbers == true;
            bool visualBell = GameBootstrap.Instance?.Settings?.accessibility?.visualBellAlert ?? true;
            changed |= Assign(ref model.showMaruTimer, showTimer);
            changed |= Assign(ref model.visualBellAlert, visualBell);
            bool escapeActive = maruDirector?.IsEscapeActive ?? false;
            changed |= Assign(ref model.maruEscapeActive, escapeActive);
            float escapeProgress = maruDirector?.EscapeProgress ?? 0f;
            if (!Mathf.Approximately(model.MaruEscapeProgress, escapeProgress))
            {
                model.maruEscapeProgress = escapeProgress;
                changed = true;
            }
            float escapeRemaining = maruDirector?.EscapeRemainingSeconds ?? 0f;
            if (!Mathf.Approximately(model.MaruEscapeRemainingSeconds, escapeRemaining))
            {
                model.maruEscapeRemainingSeconds = escapeRemaining;
                changed = true;
            }

            ExitGuidance guidance = stageFlow?.CurrentGuidance ?? default;
            changed |= Assign(ref model.exitGuidanceValid, guidance.IsValid);
            changed |= Assign(ref model.exitInCurrentRoom, guidance.ExitInCurrentRoom);
            if (model.ExitDirection != guidance.Direction)
            {
                model.exitDirection = guidance.Direction;
                changed = true;
            }

            bool exitDiscovered = stage?.exitDiscovered ?? false;
            changed |= Assign(ref model.exitDiscovered, exitDiscovered);
            string currentRoom = stage?.currentRoomId ?? string.Empty;
            changed |= Assign(ref model.currentRoomId, currentRoom);

            bool bossActive = stageFlow?.CurrentDefinition != null &&
                              stageFlow.CurrentDefinition.kind == StageKind.Boss &&
                              stage != null &&
                              (stage.phase == StagePhase.BossIntro || stage.phase == StagePhase.BossBattle);
            changed |= Assign(ref model.bossActive, bossActive);
            changed |= Assign(ref model.visibility, ResolveVisibility(stage, bossActive));

            StageExitDoor exit = stageFlow?.CurrentExit;
            bool promptEnabled = GameBootstrap.Instance?.Settings?.gameplay?.showInteractionPrompt ?? true;
            string candidatePrompt = ResolveCandidatePrompt(interactionProbe?.SelectedCandidate);
            bool showExitPrompt = exit != null && exit.IsPlayerInRange && stageFlow.CanCommitExit;
            bool showPrompt = !model.MapOpen
                && promptEnabled
                && (showExitPrompt || !string.IsNullOrEmpty(candidatePrompt));
            changed |= Assign(ref model.showActionPrompt, showPrompt);
            changed |= Assign(
                ref model.actionLabel,
                showPrompt ? showExitPrompt ? "출항하기" : candidatePrompt : string.Empty);
            float progress = showPrompt && showExitPrompt && exit.IsHolding ? exit.HoldProgress : 0f;
            if (!Mathf.Approximately(model.ActionProgress, progress))
            {
                model.actionProgress = progress;
                changed = true;
            }

            object definition = stageFlow?.CurrentDefinition;
            if (!ReferenceEquals(observedDefinition, definition))
            {
                observedDefinition = definition;
                string stageName = stageFlow?.CurrentDefinition == null
                    ? string.Empty
                    : string.IsNullOrWhiteSpace(stageFlow.CurrentDefinition.displayNameKey)
                        ? stageFlow.CurrentDefinition.stageId
                        : stageFlow.CurrentDefinition.displayNameKey;
                model.stageName = stageName;
                stageNameVisibleUntil = Time.unscaledTime + 1.5f;
                model.stageNameVisible = !string.IsNullOrEmpty(stageName);
                changed = true;
            }
            else
            {
                bool nameVisible = !string.IsNullOrEmpty(model.StageName) && Time.unscaledTime < stageNameVisibleUntil;
                changed |= Assign(ref model.stageNameVisible, nameVisible);
            }

            float fade = stageFlow?.FadeOpacity ?? 0f;
            if (!Mathf.Approximately(model.FadeOpacity, fade))
            {
                model.fadeOpacity = fade;
                changed = true;
            }

            int visitedCount = stage?.visitedRoomIds?.Count ?? 0;
            if (!ReferenceEquals(observedStage, stage) ||
                observedVisitedCount != visitedCount ||
                observedRoomId != currentRoom ||
                observedExitDiscovered != exitDiscovered)
            {
                observedStage = stage;
                observedVisitedCount = visitedCount;
                observedRoomId = currentRoom;
                observedExitDiscovered = exitDiscovered;
                RebuildMap(stage);
                changed = true;
            }

            if (stage == null && model.MapOpen)
            {
                model.mapOpen = false;
                changed = true;
            }

            if (changed)
            {
                Publish();
            }
        }

        private HUDVisibility ResolveVisibility(StageRuntimeState stage, bool bossActive)
        {
            if (stage == null || gameFlow == null)
            {
                return HUDVisibility.Hidden;
            }

            if (gameFlow.State == GameApplicationState.RunResult || gameFlow.State == GameApplicationState.Title)
            {
                return HUDVisibility.Hidden;
            }

            if (gameFlow.State == GameApplicationState.Paused)
            {
                return HUDVisibility.Dimmed;
            }

            return bossActive ? HUDVisibility.Boss : HUDVisibility.Exploration;
        }

        private bool SynchronizeEquipment()
        {
            var source = equipmentInventory?.HudEntries;
            int sourceCount = source?.Count ?? 0;
            bool changed = model.MutableEquipment.Count != sourceCount;
            if (!changed)
            {
                for (int index = 0; index < sourceCount; index++)
                {
                    EquipmentInventoryHudEntry left = model.MutableEquipment[index];
                    EquipmentInventoryHudEntry right = source[index];
                    if (left.StableItemId != right.StableItemId
                        || left.CurrentDurability != right.CurrentDurability
                        || left.MaximumDurability != right.MaximumDurability
                        || left.IsBroken != right.IsBroken
                        || left.IsSelected != right.IsSelected
                        || left.UseKind != right.UseKind)
                    {
                        changed = true;
                        break;
                    }
                }
            }

            int feedbackRevision = equipmentInventory?.FeedbackRevision ?? 0;
            if (model.EquipmentFeedbackRevision != feedbackRevision)
            {
                model.equipmentFeedbackRevision = feedbackRevision;
                model.equipmentFeedbackMessage = equipmentInventory?.LatestFeedbackMessage ?? string.Empty;
                model.equipmentFeedbackVisible = !string.IsNullOrWhiteSpace(model.equipmentFeedbackMessage);
                equipmentFeedbackVisibleUntil = Time.unscaledTime + 1.5f;
                changed = true;
            }
            else if (model.EquipmentFeedbackVisible
                && Time.unscaledTime >= equipmentFeedbackVisibleUntil)
            {
                model.equipmentFeedbackVisible = false;
                changed = true;
            }

            if (!changed)
            {
                return false;
            }

            model.MutableEquipment.Clear();
            for (int index = 0; index < sourceCount; index++)
            {
                model.MutableEquipment.Add(source[index]);
            }
            return true;
        }

        private void RebuildMap(StageRuntimeState stage)
        {
            model.MutableRooms.Clear();
            model.MutableConnections.Clear();
            if (stage?.visitedRoomIds == null || stageFlow == null)
            {
                model.mapVersion++;
                return;
            }

            ExitGuidanceService guide = stageFlow.GuidanceService;
            foreach (string roomId in stage.visitedRoomIds)
            {
                if (!guide.TryGetRoomCenter(roomId, out Vector2 center))
                {
                    continue;
                }

                bool current = string.Equals(roomId, stage.currentRoomId, StringComparison.Ordinal);
                bool exit = stage.exitDiscovered && string.Equals(roomId, guide.ExitRoomId, StringComparison.Ordinal);
                model.MutableRooms.Add(new HUDMapRoomModel(roomId, center, current, exit));
            }

            for (int index = 0; index < model.MutableRooms.Count; index++)
            {
                string from = model.MutableRooms[index].RoomId;
                var neighbors = guide.GetMainRouteNeighbors(from);
                for (int neighborIndex = 0; neighborIndex < neighbors.Count; neighborIndex++)
                {
                    string to = neighbors[neighborIndex];
                    if (stage.visitedRoomIds.Contains(to) && string.CompareOrdinal(from, to) < 0)
                    {
                        model.MutableConnections.Add(new HUDMapConnectionModel(from, to));
                    }
                }
            }

            model.mapVersion++;
        }

        private void Publish()
        {
            model.revision++;
            ModelChanged?.Invoke(model);
        }

        private static bool Assign(ref int destination, int value)
        {
            if (destination == value) return false;
            destination = value;
            return true;
        }

        private static bool Assign(ref bool destination, bool value)
        {
            if (destination == value) return false;
            destination = value;
            return true;
        }

        private static bool Assign(ref string destination, string value)
        {
            value ??= string.Empty;
            if (destination == value) return false;
            destination = value;
            return true;
        }

        private static bool Assign(ref BellPhase destination, BellPhase value)
        {
            if (destination == value) return false;
            destination = value;
            return true;
        }

        private static bool Assign(ref HUDVisibility destination, HUDVisibility value)
        {
            if (destination == value) return false;
            destination = value;
            return true;
        }

        private static string ResolveCandidatePrompt(InteractionCandidate candidate)
        {
            if (candidate == null)
            {
                return string.Empty;
            }
            if (!string.IsNullOrWhiteSpace(candidate.PromptVerb))
            {
                return candidate.PromptVerb;
            }

            MonoBehaviour[] behaviours = candidate.GetComponentsInParent<MonoBehaviour>(true);
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IInteractionPromptSource source
                    && !string.IsNullOrWhiteSpace(source.PromptLabel))
                {
                    return source.PromptLabel;
                }
            }
            return string.Empty;
        }
    }
}

#endif
