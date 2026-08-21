#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Player
{
    public struct JumpGraceState
    {
        private readonly float coyoteDuration;
        private readonly float bufferDuration;
        private float coyoteRemaining;
        private float bufferRemaining;
        private bool consumedSinceGrounded;

        public JumpGraceState(float coyoteDuration, float bufferDuration)
        {
            this.coyoteDuration = Mathf.Max(0f, coyoteDuration);
            this.bufferDuration = Mathf.Max(0f, bufferDuration);
            coyoteRemaining = 0f;
            bufferRemaining = 0f;
            consumedSinceGrounded = false;
        }

        public float CoyoteRemaining => coyoteRemaining;
        public float BufferRemaining => bufferRemaining;
        public bool HasBufferedJump => bufferRemaining > 0f;

        public void Tick(float deltaTime, bool grounded)
        {
            float step = Mathf.Max(0f, deltaTime);
            if (grounded)
            {
                coyoteRemaining = coyoteDuration;
                consumedSinceGrounded = false;
            }
            else
            {
                coyoteRemaining = Mathf.Max(0f, coyoteRemaining - step);
            }

            bufferRemaining = Mathf.Max(0f, bufferRemaining - step);
        }

        public void BufferJump()
        {
            bufferRemaining = bufferDuration;
        }

        public bool TryConsume()
        {
            if (bufferRemaining <= 0f || coyoteRemaining <= 0f || consumedSinceGrounded)
            {
                return false;
            }

            bufferRemaining = 0f;
            coyoteRemaining = 0f;
            consumedSinceGrounded = true;
            return true;
        }

        public void Reset()
        {
            coyoteRemaining = 0f;
            bufferRemaining = 0f;
            consumedSinceGrounded = false;
        }
    }
}

#endif
