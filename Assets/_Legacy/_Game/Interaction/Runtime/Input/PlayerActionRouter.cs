#if LEGACY_DISABLED
using System;
using StarNight.Interaction.HandSlot;
using UnityEngine;

namespace StarNight.Interaction.Input
{
    [DisallowMultipleComponent]
    public sealed class PlayerActionRouter : MonoBehaviour
    {
        public const float LateDownChordSeconds = 0.03f;

        [SerializeField] private GameplayInputReader inputReader;
        [SerializeField] private PlayerActionLock actionLock;
        [SerializeField] private MonoBehaviour movementInputSinkComponent;
        [SerializeField] private MonoBehaviour actionExecutorComponent;

        private IPlayerMovementInputSink movementInputSink;
        private IPlayerActionExecutor actionExecutor;
        private IPlayerSpecialActionExecutor specialActionExecutor;
        private IPlayerBombActionExecutor bombActionExecutor;
        private IPlayerRopeActionExecutor ropeActionExecutor;
        private IPlayerRopeMovementHandler ropeMovementHandler;
        private IPlayerSpecialActionCancelHandler specialActionCancelHandler;
        private IPlayerTraversalStateHandler traversalStateHandler;
        private IEquipmentInventoryBridge equipmentInventory;
        private IPlayerInventoryActionExecutor inventoryActionExecutor;
        private long nextActionId = 1;
        private bool primaryPending;
        private bool downHeldWhenPrimaryPressed;
        private float primaryResolveAt;
        private bool mapOverlayOpen;

        public event Action<RoutedPlayerAction> ActionRouted;

        public PlayerActionLock ActionLock => actionLock;
        public bool HasPendingPrimaryAction => primaryPending;
        public bool IsMapOverlayOpen => mapOverlayOpen;
        public bool GameplayActionsAllowed => !mapOverlayOpen && (actionLock == null || actionLock.AllowsGameplayActions);

        private void Awake()
        {
            ResolveDependencies();
        }

        private void Update()
        {
            ResolveDependencies();
            if (inputReader == null)
            {
                return;
            }

            bool gameplayAllowed = GameplayActionsAllowed;
            bool ropeClimbing = ropeMovementHandler != null && ropeMovementHandler.IsRopeClimbing;
            if (movementInputSink != null)
            {
                movementInputSink.SetMoveInput(gameplayAllowed && !ropeClimbing ? inputReader.MoveHorizontal : 0f);
                movementInputSink.SetLookInput(gameplayAllowed && !ropeClimbing ? inputReader.LookVertical : 0f);
                movementInputSink.SetJumpHeld(gameplayAllowed && !ropeClimbing && inputReader.JumpHeld);
            }
            ropeMovementHandler?.SetRopeInput(
                gameplayAllowed ? inputReader.MoveHorizontal : 0f,
                gameplayAllowed ? inputReader.LookVertical : 0f);

            if (!gameplayAllowed)
            {
                FlushBufferedInput();
                return;
            }

            if (inputReader.ConsumeSelectPreviousPressed())
            {
                equipmentInventory?.TrySelectPrevious(Time.unscaledTime);
            }
            else if (inputReader.ConsumeSelectNextPressed())
            {
                equipmentInventory?.TrySelectNext(Time.unscaledTime);
            }

            if (inputReader.ConsumeJumpPressed())
            {
                bool cancelledSpecial = specialActionCancelHandler != null
                    && specialActionCancelHandler.TryCancelSpecialAction();
                if (!cancelledSpecial
                    && (ropeMovementHandler == null || !ropeMovementHandler.TryJumpExit()))
                {
                    if (!TryExecuteSelectedJumpModifier())
                    {
                        movementInputSink?.QueueJump();
                    }
                }
            }

            if (inputReader.ConsumeJumpReleased())
            {
                if (ropeMovementHandler == null || !ropeMovementHandler.IsRopeClimbing)
                {
                    movementInputSink?.ReleaseJump();
                }
            }

            if (inputReader.ConsumePrimaryPressed())
            {
                if (primaryPending)
                {
                    RoutePrimaryAction(downHeldWhenPrimaryPressed || inputReader.LookVertical < -0.5f);
                }

                primaryPending = true;
                downHeldWhenPrimaryPressed = inputReader.LookVertical < -0.5f;
                primaryResolveAt = Time.unscaledTime + LateDownChordSeconds;
            }

            if (primaryPending && Time.unscaledTime >= primaryResolveAt)
            {
                bool downHeld = downHeldWhenPrimaryPressed || inputReader.LookVertical < -0.5f;
                primaryPending = false;
                RoutePrimaryAction(downHeld);
            }

            if (inputReader.ConsumeBombPressed())
            {
                RouteBombAction();
            }

            if (inputReader.ConsumeRopePressed())
            {
                RouteRopeAction();
            }
        }

        public PlayerActionCommand RoutePrimaryAction(bool downHeld)
        {
            if (!GameplayActionsAllowed)
            {
                return PlayerActionCommand.None;
            }

            PlayerActionContext context = CreateContext(downHeld);
            if (actionExecutor == null)
            {
                return Publish(PlayerActionCommand.PrimaryAction, context);
            }

            if (downHeld && actionExecutor.HasHandSlotItem)
            {
                if (specialActionExecutor != null
                    && !specialActionExecutor.TryPrepareHandSlotDrop(context))
                {
                    return PlayerActionCommand.None;
                }

                return actionExecutor.TryDropHandSlot(context)
                    ? Publish(PlayerActionCommand.DropHandSlot, context)
                    : PlayerActionCommand.None;
            }

            if (actionLock != null && actionLock.State == PlayerActionState.HookLatched)
            {
                return specialActionExecutor != null && specialActionExecutor.TryPullHook(context)
                    ? Publish(PlayerActionCommand.HookPull, context)
                    : PlayerActionCommand.None;
            }

            if (actionLock != null && actionLock.State == PlayerActionState.HookPulling)
            {
                return PlayerActionCommand.None;
            }

            if (actionLock != null && actionLock.State == PlayerActionState.UmbrellaOpen)
            {
                return specialActionExecutor != null && specialActionExecutor.TryCloseUmbrella(context)
                    ? Publish(PlayerActionCommand.CloseUmbrella, context)
                    : PlayerActionCommand.None;
            }

            if (actionExecutor.HasHandSlotItem && actionExecutor.TryContextAction(context))
            {
                return Publish(PlayerActionCommand.ContextAction, context);
            }

            if (actionExecutor.HasHandSlotItem)
            {
                if (actionExecutor.TryHandSlotPrimaryUse(context))
                {
                    return Publish(PlayerActionCommand.HandSlotPrimaryUse, context);
                }
                if (inventoryActionExecutor == null || inventoryActionExecutor.HasPhysicalCarryItem)
                {
                    return PlayerActionCommand.None;
                }
            }

            if (actionExecutor.TryWorldInteraction(context))
            {
                return Publish(PlayerActionCommand.WorldInteraction, context);
            }

            return PlayerActionCommand.None;
        }

        public bool TryExecuteSelectedJumpModifier()
        {
            ResolveDependencies();
            if (!GameplayActionsAllowed
                || equipmentInventory?.SelectedRuntime is not ISelectedEquipmentJumpModifier jumpModifier)
            {
                return false;
            }

            PlayerHandSlot owner = GetComponent<PlayerHandSlot>();
            return owner != null
                && owner.CurrentItem == equipmentInventory.SelectedRuntime
                && jumpModifier.TryExecuteSelectedJump(owner);
        }

        public PlayerActionCommand RouteBombAction()
        {
            if (!GameplayActionsAllowed)
            {
                return PlayerActionCommand.None;
            }

            PlayerActionContext context = CreateContext(false);
            bool accepted = bombActionExecutor != null
                ? bombActionExecutor.TryPlaceBomb(context)
                : actionExecutor == null || actionExecutor.TryPlaceBomb(context);
            if (!accepted)
            {
                return PlayerActionCommand.None;
            }

            return Publish(PlayerActionCommand.PlaceBomb, context);
        }

        public PlayerActionCommand RouteRopeAction()
        {
            if (!GameplayActionsAllowed)
            {
                return PlayerActionCommand.None;
            }

            PlayerActionContext context = CreateContext(false);
            traversalStateHandler?.PrepareForTraversal();
            bool accepted = ropeActionExecutor != null
                ? ropeActionExecutor.TryPlaceRope(context)
                : actionExecutor == null || actionExecutor.TryPlaceRope(context);
            if (!accepted)
            {
                return PlayerActionCommand.None;
            }

            return Publish(PlayerActionCommand.PlaceRope, context);
        }

        public void FlushBufferedInput()
        {
            primaryPending = false;
            downHeldWhenPrimaryPressed = false;
            inputReader?.ClearBufferedButtons();
        }

        public void SetMapOverlayOpen(bool open)
        {
            if (mapOverlayOpen == open)
            {
                return;
            }

            mapOverlayOpen = open;
            FlushBufferedInput();
            if (open && movementInputSink != null)
            {
                movementInputSink.SetMoveInput(0f);
                movementInputSink.SetLookInput(0f);
                movementInputSink.SetJumpHeld(false);
            }
        }

        public void ConfigureForTests(
            GameplayInputReader reader,
            PlayerActionLock playerActionLock,
            IPlayerMovementInputSink movementSink,
            IPlayerActionExecutor executor)
        {
            inputReader = reader;
            actionLock = playerActionLock;
            movementInputSink = movementSink;
            actionExecutor = executor;
            specialActionExecutor = executor as IPlayerSpecialActionExecutor;
            bombActionExecutor = executor as IPlayerBombActionExecutor;
            ropeActionExecutor = executor as IPlayerRopeActionExecutor;
            ropeMovementHandler = executor as IPlayerRopeMovementHandler;
            specialActionCancelHandler = executor as IPlayerSpecialActionCancelHandler;
            traversalStateHandler = executor as IPlayerTraversalStateHandler;
            inventoryActionExecutor = executor as IPlayerInventoryActionExecutor;
            movementInputSinkComponent = movementSink as MonoBehaviour;
            actionExecutorComponent = executor as MonoBehaviour;
        }

        private void ResolveDependencies()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<GameplayInputReader>();
            }

            if (actionLock == null)
            {
                actionLock = GetComponent<PlayerActionLock>();
            }

            movementInputSink = movementInputSinkComponent as IPlayerMovementInputSink;
            actionExecutor = actionExecutorComponent as IPlayerActionExecutor;
            specialActionExecutor = actionExecutorComponent as IPlayerSpecialActionExecutor;
            bombActionExecutor = actionExecutorComponent as IPlayerBombActionExecutor;
            ropeActionExecutor = actionExecutorComponent as IPlayerRopeActionExecutor;
            ropeMovementHandler = actionExecutorComponent as IPlayerRopeMovementHandler;
            specialActionCancelHandler = actionExecutorComponent as IPlayerSpecialActionCancelHandler;
            traversalStateHandler = actionExecutorComponent as IPlayerTraversalStateHandler;
            inventoryActionExecutor = actionExecutorComponent as IPlayerInventoryActionExecutor;
            equipmentInventory = null;

            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (movementInputSink == null && behaviours[index] is IPlayerMovementInputSink sink)
                {
                    movementInputSink = sink;
                    movementInputSinkComponent = behaviours[index];
                }

                if (actionExecutor == null && behaviours[index] is IPlayerActionExecutor executor)
                {
                    actionExecutor = executor;
                    actionExecutorComponent = behaviours[index];
                }

                if (specialActionExecutor == null
                    && behaviours[index] is IPlayerSpecialActionExecutor specialExecutor)
                {
                    specialActionExecutor = specialExecutor;
                }


                if (bombActionExecutor == null
                    && behaviours[index] is IPlayerBombActionExecutor bombExecutor)
                {
                    bombActionExecutor = bombExecutor;
                }

                if (ropeActionExecutor == null
                    && behaviours[index] is IPlayerRopeActionExecutor ropeExecutor)
                {
                    ropeActionExecutor = ropeExecutor;
                }

                if (ropeMovementHandler == null
                    && behaviours[index] is IPlayerRopeMovementHandler ropeHandler)
                {
                    ropeMovementHandler = ropeHandler;
                }

                if (specialActionCancelHandler == null
                    && behaviours[index] is IPlayerSpecialActionCancelHandler cancelHandler)
                {
                    specialActionCancelHandler = cancelHandler;
                }

                if (traversalStateHandler == null
                    && behaviours[index] is IPlayerTraversalStateHandler traversalHandler)
                {
                    traversalStateHandler = traversalHandler;
                }

                if (equipmentInventory == null && behaviours[index] is IEquipmentInventoryBridge inventory)
                {
                    equipmentInventory = inventory;
                }

                if (inventoryActionExecutor == null && behaviours[index] is IPlayerInventoryActionExecutor inventoryExecutor)
                {
                    inventoryActionExecutor = inventoryExecutor;
                }
            }
        }

        private PlayerActionContext CreateContext(bool downHeld)
        {
            float move = inputReader != null ? inputReader.MoveHorizontal : 0f;
            float look = inputReader != null ? inputReader.LookVertical : 0f;
            return new PlayerActionContext(nextActionId++, move, look, downHeld);
        }

        private PlayerActionCommand Publish(PlayerActionCommand command, PlayerActionContext context)
        {
            ActionRouted?.Invoke(new RoutedPlayerAction(command, context));
            return command;
        }
    }
}

#endif
