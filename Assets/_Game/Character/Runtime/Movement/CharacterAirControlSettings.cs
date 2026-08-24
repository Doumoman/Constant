using System;

namespace StarNight.Character.Movement
{
    /// <summary>
    /// 공중 수평 제어 튜닝. airAcceleration 기본값은 레거시 선례
    /// (지상 가속 30 × airControl 0.75)를 참고한 기준선이다.
    /// maxAirSpeed 기본값 3.1은 CHAR02_03 CHANGE CONTROL 교정값이다:
    /// 공중 수평 상한이 지상 runSpeed(3.75)와 같으면 코요테 지연 점프의
    /// 유효 도달 폭이 3.17~3.37u가 되어 잠금 3셀 규칙이 깨진다(감사 증거 x=3.171).
    /// 3.1로 캡하면 코요테 최대 활용 도달 폭 ≈ 2.84u &lt; 3.0(3셀 실패 여유 0.16u),
    /// 2셀 틈 착지 ≈ 2.23u ≥ 2.0(통과 여유 0.23u), 2셀 높이·지상 이동은 무영향이다.
    /// </summary>
    public readonly struct CharacterAirControlSettings
    {
        public CharacterAirControlSettings(float airAcceleration, float maxAirSpeed)
        {
            if (airAcceleration < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(airAcceleration), "airAcceleration은 0 이상이어야 한다.");
            }

            if (maxAirSpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxAirSpeed), "maxAirSpeed는 0보다 커야 한다.");
            }

            AirAcceleration = airAcceleration;
            MaxAirSpeed = maxAirSpeed;
        }

        public float AirAcceleration { get; }
        public float MaxAirSpeed { get; }

        public static CharacterAirControlSettings Default
        {
            get { return new CharacterAirControlSettings(22.5f, 3.1f); }
        }
    }
}
