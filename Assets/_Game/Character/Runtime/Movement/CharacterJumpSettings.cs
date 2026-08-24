using System;

namespace StarNight.Character.Movement
{
    /// <summary>
    /// 점프 튜닝. 기본값은 movement tuning schema와 레거시 선례(coyote 0.10,
    /// buffer 0.12, jumpHeight 2.2셀 상당의 초기 속도)를 따르는 기준선이며
    /// PASS 기준은 값 자체가 아니라 이동 문법 결과다(코스 검증은 CHAR02).
    /// </summary>
    public readonly struct CharacterJumpSettings
    {
        public CharacterJumpSettings(
            float jumpVelocity,
            double coyoteTime,
            double jumpBufferTime,
            float releaseCutMultiplier)
        {
            if (jumpVelocity <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(jumpVelocity), "jumpVelocity는 0보다 커야 한다.");
            }

            if (coyoteTime < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(coyoteTime), "coyoteTime은 0 이상이어야 한다.");
            }

            if (jumpBufferTime < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(jumpBufferTime), "jumpBufferTime은 0 이상이어야 한다.");
            }

            if (releaseCutMultiplier < 0f || releaseCutMultiplier >= 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(releaseCutMultiplier),
                    "release cut 계수는 [0, 1) 범위여야 한다.");
            }

            JumpVelocity = jumpVelocity;
            CoyoteTime = coyoteTime;
            JumpBufferTime = jumpBufferTime;
            ReleaseCutMultiplier = releaseCutMultiplier;
        }

        public float JumpVelocity { get; }
        public double CoyoteTime { get; }
        public double JumpBufferTime { get; }

        /// <summary>상승 중 release 시 상승 속도에 곱하는 cut 계수.</summary>
        public float ReleaseCutMultiplier { get; }

        public static CharacterJumpSettings Default
        {
            get { return new CharacterJumpSettings(10.28f, 0.10d, 0.12d, 0.5f); }
        }
    }
}
