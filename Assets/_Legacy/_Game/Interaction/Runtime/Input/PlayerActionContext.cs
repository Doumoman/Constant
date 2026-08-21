#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Interaction.Input
{
    public readonly struct PlayerActionContext
    {
        public PlayerActionContext(long actionId, float moveHorizontal, float lookVertical, bool downHeld)
        {
            ActionId = actionId;
            MoveHorizontal = moveHorizontal;
            LookVertical = lookVertical;
            DownHeld = downHeld;
        }

        public long ActionId { get; }
        public float MoveHorizontal { get; }
        public float LookVertical { get; }
        public bool DownHeld { get; }
    }

    public enum PlayerActionCommand
    {
        None,
        PrimaryAction,
        DropHandSlot,
        ContextAction,
        HandSlotPrimaryUse,
        WorldInteraction,
        HookPull,
        CloseUmbrella,
        PlaceBomb,
        PlaceRope,
    }

    public readonly struct RoutedPlayerAction
    {
        public RoutedPlayerAction(PlayerActionCommand command, PlayerActionContext context)
        {
            Command = command;
            Context = context;
        }

        public PlayerActionCommand Command { get; }
        public PlayerActionContext Context { get; }
    }

    public interface IPlayerMovementInputSink
    {
        void SetMoveInput(float horizontal);
        void SetLookInput(float vertical);
        void SetJumpHeld(bool held);
        void QueueJump();
        void ReleaseJump();
    }

    public interface IPlayerSpecialJumpExecutor
    {
        bool TryLaunchSpecialJump(float verticalVelocity, float requiredHeadClearance);
    }

    public interface IPlayerActionExecutor
    {
        bool HasHandSlotItem { get; }
        bool TryDropHandSlot(PlayerActionContext context);
        bool TryContextAction(PlayerActionContext context);
        bool TryHandSlotPrimaryUse(PlayerActionContext context);
        bool TryWorldInteraction(PlayerActionContext context);
        bool TryPlaceBomb(PlayerActionContext context);
        bool TryPlaceRope(PlayerActionContext context);
    }

    public interface IPlayerSpecialActionExecutor
    {
        bool TryPrepareHandSlotDrop(PlayerActionContext context);
        bool TryPullHook(PlayerActionContext context);
        bool TryCloseUmbrella(PlayerActionContext context);
    }

    public interface IPlayerBombActionExecutor
    {
        bool TryPlaceBomb(PlayerActionContext context);
    }

    public interface IPlayerRopeActionExecutor
    {
        bool TryPlaceRope(PlayerActionContext context);
    }

    public interface IPlayerRopeMovementHandler
    {
        bool IsRopeClimbing { get; }
        void SetRopeInput(float horizontal, float vertical);
        bool TryJumpExit();
    }

    public interface IPlayerMovementOverride
    {
        bool IsMovementOverrideActive { get; }
        void ApplyMovementOverride(Rigidbody2D body, float fixedDeltaTime);
    }

    public interface IPlayerMovementSpeedModifier
    {
        float MovementSpeedMultiplier { get; }
    }

    public interface IPlayerAirMovementModifier
    {
        bool IsAirMovementModifierActive { get; }
        float MaximumFallSpeed { get; }
        float MaximumHorizontalSpeed { get; }
        float AirAccelerationMultiplier { get; }
    }

    public interface IPlayerSpecialActionCancelHandler
    {
        bool TryCancelSpecialAction();
    }

    public interface IPlayerTraversalStateHandler
    {
        void PrepareForTraversal();
    }
}

#endif
