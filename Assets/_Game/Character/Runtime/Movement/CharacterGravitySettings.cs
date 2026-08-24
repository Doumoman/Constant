using System;

namespace StarNight.Character.Movement
{
    /// <summary>
    /// 중력 튜닝. 상승/하강 중력을 분리하고 최대 낙하 속도로 clamp한다.
    /// 기본값은 레거시 선례(gravity 24, maxFallSpeed 18)를 참고한 기준선이다.
    /// </summary>
    public readonly struct CharacterGravitySettings
    {
        public CharacterGravitySettings(float riseGravity, float fallGravity, float maxFallSpeed)
        {
            if (riseGravity <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(riseGravity), "riseGravity는 0보다 커야 한다.");
            }

            if (fallGravity <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fallGravity), "fallGravity는 0보다 커야 한다.");
            }

            if (maxFallSpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxFallSpeed), "maxFallSpeed는 0보다 커야 한다.");
            }

            RiseGravity = riseGravity;
            FallGravity = fallGravity;
            MaxFallSpeed = maxFallSpeed;
        }

        public float RiseGravity { get; }
        public float FallGravity { get; }
        public float MaxFallSpeed { get; }

        public static CharacterGravitySettings Default
        {
            get { return new CharacterGravitySettings(24f, 30f, 18f); }
        }
    }
}
