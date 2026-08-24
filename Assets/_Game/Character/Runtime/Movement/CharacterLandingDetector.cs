using UnityEngine;

namespace StarNight.Character.Movement
{
    /// <summary>
    /// 착지 감지. 이전 틱 airborne → 이번 틱 grounded 전환에서만 landing으로 판정한다.
    /// 착지 시 하강 속도를 0으로 정리하고 점프 소비 상태를 reset한다.
    /// Animator, 사운드, 렌더 프레임 성공 여부에 의존하지 않는다.
    /// </summary>
    public sealed class CharacterLandingDetector
    {
        /// <summary>airborne → grounded 전환만 landing이다.</summary>
        public bool DetectLanding(bool wasGroundedLastTick, bool isGroundedNow)
        {
            return !wasGroundedLastTick && isGroundedNow;
        }

        /// <summary>착지 시 잔여 하강 속도를 0으로 정리한다(상승 속도는 유지).</summary>
        public Vector2 SettleVelocityOnLanding(Vector2 velocity)
        {
            if (velocity.y < 0f)
            {
                velocity.y = 0f;
            }

            return velocity;
        }

        /// <summary>
        /// 한 틱 처리: 전환 감지 → 하강 속도 정리 + 점프 상태 reset(NoteGrounded).
        /// landing 여부를 반환한다.
        /// </summary>
        public bool Step(
            CharacterJumpState jumpState,
            bool wasGroundedLastTick,
            bool isGroundedNow,
            double time,
            ref Vector2 velocity)
        {
            bool landed = DetectLanding(wasGroundedLastTick, isGroundedNow);

            if (landed)
            {
                velocity = SettleVelocityOnLanding(velocity);
                jumpState.NoteGrounded(time);
            }

            return landed;
        }
    }
}
