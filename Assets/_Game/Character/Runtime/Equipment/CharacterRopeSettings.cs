using System;

namespace StarNight.Character.Equipment
{
    /// <summary>
    /// 로프 장비 중앙 설정. 최대 길이 6셀·등반 속도 4u/s는 레거시
    /// RopePlacementSolver/RopeClimber2D 선례를 따른 기준선이다.
    /// </summary>
    public readonly struct CharacterRopeSettings
    {
        public CharacterRopeSettings(
            int maxRopeLengthCells,
            float climbSpeedUnitsPerSecond)
        {
            if (maxRopeLengthCells <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxRopeLengthCells));
            }

            if (climbSpeedUnitsPerSecond <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(climbSpeedUnitsPerSecond));
            }

            MaxRopeLengthCells = maxRopeLengthCells;
            ClimbSpeedUnitsPerSecond = climbSpeedUnitsPerSecond;
        }

        public int MaxRopeLengthCells { get; }
        public float ClimbSpeedUnitsPerSecond { get; }

        public static CharacterRopeSettings Default
        {
            get { return new CharacterRopeSettings(6, 4f); }
        }
    }
}
