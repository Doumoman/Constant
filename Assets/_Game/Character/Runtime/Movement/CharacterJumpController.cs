using UnityEngine;

namespace StarNight.Character.Movement
{
    /// <summary>
    /// 단일 점프 제어. Jump press가 버퍼 시간 안에 있고 grounded 또는 coyote window
    /// 안이면 점프를 시작한다. 시작 시 vertical velocity를 jumpVelocity로 설정하고
    /// press를 소비한다(같은 press는 한 번만).
    /// 조건이 안 되면 press를 보존하고 buffer window 만료 후에는 소비되지 않는다.
    /// 상승 중 release는 가변 점프 cut 계수로 상승을 줄인다(점프당 1회).
    /// wall jump, dash jump, double jump는 구현하지 않는다.
    /// </summary>
    public sealed class CharacterJumpController
    {
        private readonly CharacterJumpSettings settings;

        public CharacterJumpController(CharacterJumpSettings settings)
        {
            this.settings = settings;
        }

        public CharacterJumpSettings Settings
        {
            get { return settings; }
        }

        /// <summary>
        /// 점프 시작 시도. 성공 시 velocity.y = jumpVelocity, press 소비, true 반환.
        /// </summary>
        public bool TryStartJump(
            CharacterJumpState state,
            bool isGrounded,
            double time,
            ref Vector2 velocity)
        {
            // 공중에서 두 번째 점프 금지: grounded 재획득 전에는 소비 상태가 유지된다.
            if (state.JumpConsumed)
            {
                return false;
            }

            if (!state.HasBufferedPress(time, settings.JumpBufferTime))
            {
                return false;
            }

            bool eligible = isGrounded || state.IsWithinCoyote(time, settings.CoyoteTime);
            if (!eligible)
            {
                // press는 보존한다. buffer window 만료는 HasBufferedPress가 처리한다.
                return false;
            }

            velocity.y = settings.JumpVelocity;
            state.ConsumeJump();
            return true;
        }

        /// <summary>
        /// 가변 점프 release. 점프로 상승 중이고 Jump가 release됐으면
        /// 상승 속도에 cut 계수를 1회 적용한다. 하강 중에는 적용하지 않는다.
        /// </summary>
        public Vector2 ApplyJumpRelease(
            CharacterJumpState state,
            bool jumpHeld,
            Vector2 velocity)
        {
            if (jumpHeld)
            {
                return velocity;
            }

            if (velocity.y <= 0f)
            {
                return velocity;
            }

            if (!state.JumpConsumed || state.ReleaseCutApplied)
            {
                return velocity;
            }

            velocity.y *= settings.ReleaseCutMultiplier;
            state.MarkReleaseCutApplied();
            return velocity;
        }
    }
}
