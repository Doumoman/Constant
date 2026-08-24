using System;

namespace StarNight.Character.Movement
{
    /// <summary>
    /// 점프 추적 상태. grounded 시각, 마지막 Jump press 시각, 점프 소비 여부,
    /// 가변 release 적용 여부를 추적한다.
    /// grounded를 다시 획득하면 점프 소비 상태가 reset된다.
    /// 공중에서 두 번째 점프는 허용되지 않는다(소비 플래그로 차단).
    /// </summary>
    public sealed class CharacterJumpState
    {
        public CharacterJumpState()
        {
            LastGroundedTime = double.NegativeInfinity;
            LastJumpPressTime = double.NegativeInfinity;
        }

        public double LastGroundedTime { get; private set; }
        public double LastJumpPressTime { get; private set; }
        public bool JumpConsumed { get; private set; }
        public bool ReleaseCutApplied { get; private set; }

        /// <summary>이번 틱 grounded 획득. 점프 소비·release 상태를 reset한다.</summary>
        public void NoteGrounded(double time)
        {
            LastGroundedTime = time;
            JumpConsumed = false;
            ReleaseCutApplied = false;
        }

        public void NoteJumpPressed(double time)
        {
            LastJumpPressTime = time;
        }

        public bool HasBufferedPress(double time, double bufferTime)
        {
            return time - LastJumpPressTime <= bufferTime;
        }

        public bool IsWithinCoyote(double time, double coyoteTime)
        {
            return time - LastGroundedTime <= coyoteTime;
        }

        /// <summary>점프 시작 시 press를 소비한다. 같은 press는 다시 소비되지 않는다.</summary>
        public void ConsumeJump()
        {
            LastJumpPressTime = double.NegativeInfinity;
            JumpConsumed = true;
        }

        public void MarkReleaseCutApplied()
        {
            ReleaseCutApplied = true;
        }

        /// <summary>버퍼 만료 등으로 press를 폐기한다.</summary>
        public void DiscardPress()
        {
            LastJumpPressTime = double.NegativeInfinity;
        }
    }
}
