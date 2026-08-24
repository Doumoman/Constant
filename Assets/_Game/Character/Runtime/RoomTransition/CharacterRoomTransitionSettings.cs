using System;

namespace StarNight.Character.RoomTransition
{
    /// <summary>
    /// 카메라룸 전환 hysteresis 설정. 잠금 기준선: 경계 침투 margin 0.25 world unit,
    /// 안정 판정 연속 샘플 2회 — 경계 왕복 떨림(핑퐁)으로 전환이 연사되는 것을 막는다.
    /// </summary>
    public readonly struct CharacterRoomTransitionSettings
    {
        public const float BaselineHysteresisMargin = 0.25f;
        public const int BaselineStableTargetSamples = 2;

        public CharacterRoomTransitionSettings(float hysteresisMargin, int stableTargetSamples)
        {
            if (hysteresisMargin < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(hysteresisMargin), "hysteresis margin은 0 이상이어야 한다.");
            }

            if (stableTargetSamples < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stableTargetSamples), "안정 샘플 수는 1 이상이어야 한다.");
            }

            HysteresisMargin = hysteresisMargin;
            StableTargetSamples = stableTargetSamples;
        }

        public float HysteresisMargin { get; }
        public int StableTargetSamples { get; }

        public static CharacterRoomTransitionSettings Default
        {
            get
            {
                return new CharacterRoomTransitionSettings(
                    BaselineHysteresisMargin,
                    BaselineStableTargetSamples);
            }
        }
    }
}
