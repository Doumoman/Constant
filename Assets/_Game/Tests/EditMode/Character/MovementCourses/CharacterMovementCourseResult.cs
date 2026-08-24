namespace StarNight.Character.Tests.MovementCourses
{
    /// <summary>코스 시뮬레이션 결과 값 객체(readonly). REPORT 기록용 측정값을 담는다.</summary>
    public readonly struct CharacterMovementCourseResult
    {
        public CharacterMovementCourseResult(
            float peakBottomY,
            double peakBottomTime,
            float finalX,
            float finalBottomY,
            bool finalGrounded,
            float minBottomOverWatchRange,
            int jumpInputsUsed,
            int jumpStartsExecuted,
            int ticksSimulated,
            double elapsedTime)
        {
            PeakBottomY = peakBottomY;
            PeakBottomTime = peakBottomTime;
            FinalX = finalX;
            FinalBottomY = finalBottomY;
            FinalGrounded = finalGrounded;
            MinBottomOverWatchRange = minBottomOverWatchRange;
            JumpInputsUsed = jumpInputsUsed;
            JumpStartsExecuted = jumpStartsExecuted;
            TicksSimulated = ticksSimulated;
            ElapsedTime = elapsedTime;
        }

        /// <summary>collider bottom 기준 최고 높이(절대 Y).</summary>
        public float PeakBottomY { get; }

        /// <summary>최고 높이 도달 시각(초).</summary>
        public double PeakBottomTime { get; }

        public float FinalX { get; }
        public float FinalBottomY { get; }
        public bool FinalGrounded { get; }

        /// <summary>감시 구간(x 범위) 위를 지나는 동안의 collider bottom 최저값.</summary>
        public float MinBottomOverWatchRange { get; }

        public int JumpInputsUsed { get; }
        public int JumpStartsExecuted { get; }
        public int TicksSimulated { get; }
        public double ElapsedTime { get; }
    }
}
