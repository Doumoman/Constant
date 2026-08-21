#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Interaction.Input
{
    public enum PlayerActionState
    {
        Free,
        PickingUp,
        Carrying,
        UsingTool,
        Throwing,
        Placing,
        RopeClimbing,
        HookLatched,
        HookPulling,
        UmbrellaOpen,
        Hurt,
        DialogueLocked,
        RoomTransitionLocked,
        MaruBitten,
    }

    [DisallowMultipleComponent]
    public sealed class PlayerActionLock : MonoBehaviour
    {
        [SerializeField] private PlayerActionState state = PlayerActionState.Free;
        [SerializeField] private long activeActionId;

        public PlayerActionState State => state;
        public long ActiveActionId => activeActionId;
        public bool HasActionAuthority => activeActionId > 0;

        public bool AllowsGameplayActions =>
            state != PlayerActionState.Hurt &&
            state != PlayerActionState.DialogueLocked &&
            state != PlayerActionState.RoomTransitionLocked &&
            state != PlayerActionState.MaruBitten;

        public void SetState(PlayerActionState nextState)
        {
            activeActionId = 0;
            state = nextState;
        }

        public bool TryAcquire(long actionId, PlayerActionState nextState)
        {
            if (actionId <= 0 || !AllowsGameplayActions)
            {
                return false;
            }

            if (activeActionId > 0 && activeActionId != actionId)
            {
                return false;
            }

            activeActionId = actionId;
            state = nextState;
            return true;
        }

        public bool TryTransition(long actionId, PlayerActionState nextState)
        {
            if (actionId <= 0 || activeActionId != actionId)
            {
                return false;
            }

            state = nextState;
            return true;
        }

        public bool TryRelease(long actionId, PlayerActionState finalState = PlayerActionState.Free)
        {
            if (actionId <= 0 || activeActionId != actionId)
            {
                return false;
            }

            activeActionId = 0;
            state = finalState;
            return true;
        }

        public void ResetToFree()
        {
            activeActionId = 0;
            state = PlayerActionState.Free;
        }
    }
}

#endif
