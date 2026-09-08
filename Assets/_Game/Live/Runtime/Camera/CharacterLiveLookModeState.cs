using StarNight.Character.Input;
using UnityEngine;

namespace StarNight.Character.Live.Cameras
{
    /// <summary>
    /// Player-local RMAP06 observation state. The movement driver supplies its
    /// real runtime eligibility every fixed step; this type only owns the
    /// Tab+direction hold, direction normalization, and input-lock lifetime.
    /// It has no world, save, room-camera, or generation state.
    /// </summary>
    public sealed class CharacterLiveLookModeState : MonoBehaviour
    {
        public const float HoldSeconds = 1f;
        public const float OffsetTiles = 3f;
        public const float EnterSeconds = 0.24f;
        public const float ReturnSeconds = 0.18f;

        private const float DirectionEpsilon = 0.01f;

        private Vector2 heldDirection;
        private float heldSeconds;

        public bool IsWaiting { get; private set; }
        public bool IsLooking { get; private set; }
        public bool IsInputLocked { get { return IsWaiting || IsLooking; } }
        public float HeldSeconds { get { return heldSeconds; } }
        public Vector2 TargetOffset { get; private set; }

        /// <summary>
        /// Called by the actual Player's fixed-step driver before movement,
        /// grab, climb, jump, and one-way input are consumed. A changed
        /// direction intentionally starts a new one-second wait; it never
        /// retargets an active camera immediately.
        /// </summary>
        public void Step(
            in CharacterInputSnapshot input,
            bool isEligible,
            float fixedDeltaSeconds)
        {
            Vector2 direction = GetNormalizedDirection(in input);
            if (!isEligible || !input.LookHeld || direction == Vector2.zero)
            {
                Cancel();
                return;
            }

            if (!IsWaiting && !IsLooking)
            {
                BeginWait(direction);
                return;
            }

            if (direction != heldDirection)
            {
                BeginWait(direction);
                return;
            }

            if (!IsWaiting)
            {
                return;
            }

            heldSeconds += Mathf.Max(0f, fixedDeltaSeconds);
            // Fixed-step accumulation of 0.02f can represent 1.00 as a
            // value infinitesimally below one. Preserve the specified
            // threshold instead of making the user wait an extra tick.
            if (heldSeconds >= HoldSeconds - 0.0001f)
            {
                heldSeconds = HoldSeconds;
                IsWaiting = false;
                IsLooking = true;
                TargetOffset = heldDirection * OffsetTiles;
            }
        }

        public void Cancel()
        {
            IsWaiting = false;
            IsLooking = false;
            heldSeconds = 0f;
            heldDirection = Vector2.zero;
            TargetOffset = Vector2.zero;
        }

        public static Vector2 GetNormalizedDirection(in CharacterInputSnapshot input)
        {
            float horizontal = input.Horizontal > DirectionEpsilon ? 1f :
                input.Horizontal < -DirectionEpsilon ? -1f : 0f;
            float vertical = (input.UpHeld ? 1f : 0f) - (input.DownHeld ? 1f : 0f);
            Vector2 direction = new Vector2(horizontal, vertical);
            return direction.sqrMagnitude > 1f ? direction.normalized : direction;
        }

        private void BeginWait(Vector2 direction)
        {
            heldDirection = direction;
            heldSeconds = 0f;
            TargetOffset = Vector2.zero;
            IsWaiting = true;
            IsLooking = false;
        }
    }
}
