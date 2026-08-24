namespace StarNight.Character.Tests.MovementCourses
{
    /// <summary>
    /// test-only 코스 검증 상수. `1 logical cell = 1 world unit`은 fixture 검증 규약의
    /// 명시적 기록이며 runtime MAP 좌표 또는 셀 크기의 소스가 아니다.
    /// </summary>
    public static class CharacterMovementCourseConstants
    {
        /// <summary>검증 규약: 1 logical cell = 1 world unit.</summary>
        public const float WorldUnitsPerCell = 1f;

        /// <summary>고정 물리 틱(60Hz).</summary>
        public const float FixedDeltaTime = 1f / 60f;

        /// <summary>MOVEMENT_COURSE_SPEC 공통 규약의 위치 허용 오차(±0.05 world unit).</summary>
        public const float PositionTolerance = 0.05f;

        /// <summary>2셀 높이 코스의 목표 발판 상단 높이(시작 바닥 기준 +2셀).</summary>
        public const float TwoCellHeight = 2f * WorldUnitsPerCell;

        /// <summary>2셀 틈 코스의 틈 폭(2셀).</summary>
        public const float TwoCellGapWidth = 2f * WorldUnitsPerCell;

        /// <summary>3셀 틈 코스의 틈 폭(3셀) — 기본 이동으로 통과 불가가 잠금 규칙이다.</summary>
        public const float ThreeCellGapWidth = 3f * WorldUnitsPerCell;
    }
}
