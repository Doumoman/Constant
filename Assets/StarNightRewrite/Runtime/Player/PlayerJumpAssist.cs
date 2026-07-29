using UnityEngine;

namespace StarNight.Rewrite.Player
{
    public sealed class PlayerJumpAssist
    {
        private readonly float coyoteDuration;
        private readonly float bufferDuration;
        private float coyoteRemaining;
        private float bufferRemaining;

        public PlayerJumpAssist(float coyoteDuration, float bufferDuration)
        {
            this.coyoteDuration = Mathf.Max(0f, coyoteDuration);
            this.bufferDuration = Mathf.Max(0f, bufferDuration);
        }

        public bool HasBufferedJump => bufferRemaining > 0f;
        public bool HasCoyoteTime => coyoteRemaining > 0f;

        public void Tick(float deltaTime, bool isGrounded)
        {
            float step = Mathf.Max(0f, deltaTime);
            coyoteRemaining = isGrounded
                ? coyoteDuration
                : Mathf.Max(0f, coyoteRemaining - step);
            bufferRemaining = Mathf.Max(0f, bufferRemaining - step);
        }

        public void BufferJump()
        {
            bufferRemaining = bufferDuration;
        }

        public bool TryConsumeJump()
        {
            if (!HasBufferedJump || !HasCoyoteTime)
            {
                return false;
            }

            bufferRemaining = 0f;
            coyoteRemaining = 0f;
            return true;
        }

        public void ClearCoyoteTime()
        {
            coyoteRemaining = 0f;
        }
    }
}
