using System;

namespace StarNight.Character.Movement
{
    /// <summary>
    /// 접지 판정 설정. 잠금 기준선: probe distance 0.08,
    /// 상승 속도 임계값 0.05 (vy가 임계값 초과로 상승 중이면 grounded가 아니다).
    /// </summary>
    public readonly struct CharacterGroundProbeSettings
    {
        public const float BaselineProbeDistance = 0.08f;
        public const float BaselineRisingVelocityThreshold = 0.05f;

        public CharacterGroundProbeSettings(float probeDistance, float risingVelocityThreshold)
        {
            if (probeDistance < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(probeDistance), "probe distance는 0 이상이어야 한다.");
            }

            if (risingVelocityThreshold < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(risingVelocityThreshold), "상승 속도 임계값은 0 이상이어야 한다.");
            }

            ProbeDistance = probeDistance;
            RisingVelocityThreshold = risingVelocityThreshold;
        }

        public float ProbeDistance { get; }
        public float RisingVelocityThreshold { get; }

        public static CharacterGroundProbeSettings Default
        {
            get
            {
                return new CharacterGroundProbeSettings(
                    BaselineProbeDistance,
                    BaselineRisingVelocityThreshold);
            }
        }
    }
}
