using StarNight.Character.Input;

namespace StarNight.Character.Live.Input
{
    /// <summary>
    /// 버튼 1개의 프레임 누적기. Update에서 수집한 pressed/released 에지를
    /// 고정 스텝 소비자가 읽어 갈 때까지 보존하고(여러 렌더 프레임 누적),
    /// held는 프레임마다 최신 값으로 이어진다. 소비 시 에지만 초기화된다.
    /// </summary>
    public sealed class CharacterLiveInputState
    {
        private bool pendingPress;
        private bool pendingRelease;
        private bool held;

        /// <summary>렌더 프레임 관측값을 누적한다(에지 OR, held 최신화).</summary>
        public void AccumulateFrame(in CharacterLiveButtonFrame frame)
        {
            pendingPress = pendingPress || frame.PressedThisFrame;
            pendingRelease = pendingRelease || frame.ReleasedThisFrame;
            held = frame.IsHeld;
        }

        /// <summary>
        /// 고정 스텝 소비: 누적 에지를 담은 스냅샷을 만들고 에지를 비운다.
        /// held는 유지된다(연속 상태).
        /// </summary>
        public CharacterButtonSnapshot ConsumeSnapshot(long physicsTick)
        {
            var snapshot = new CharacterButtonSnapshot(
                pendingPress, held, pendingRelease, false, physicsTick);
            pendingPress = false;
            pendingRelease = false;
            return snapshot;
        }

        /// <summary>비활성화/재시작 시 전체 초기화.</summary>
        public void Reset()
        {
            pendingPress = false;
            pendingRelease = false;
            held = false;
        }
    }
}
