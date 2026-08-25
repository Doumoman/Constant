using StarNight.Character.Input;
using UnityEngine;

namespace StarNight.Character.Live.Input
{
    /// <summary>
    /// 장치 관측값 → 잠금 캐릭터 입력 계약 변환기(순수, 장치 API 무의존).
    /// 렌더 프레임마다 AccumulateFrame으로 채우고, 고정 스텝에서
    /// ConsumeFixedSnapshot으로 CharacterInputSnapshot을 꺼낸다.
    /// SafeDrop/버퍼/코요테 판정은 기존 순수 계약 소유이므로 여기서는
    /// 값 공급만 한다. 신규 ActionId 없음.
    /// </summary>
    public sealed class CharacterLiveInputAdapter
    {
        private readonly CharacterLiveInputState jump = new CharacterLiveInputState();
        private readonly CharacterLiveInputState action = new CharacterLiveInputState();
        private readonly CharacterLiveInputState bomb = new CharacterLiveInputState();
        private readonly CharacterLiveInputState rope = new CharacterLiveInputState();

        private float horizontal;
        private bool downHeld;

        /// <summary>렌더 프레임 관측값 일괄 누적(Update에서 호출).</summary>
        public void AccumulateFrame(
            float horizontalAxis,
            bool isDownHeld,
            in CharacterLiveButtonFrame jumpFrame,
            in CharacterLiveButtonFrame actionFrame,
            in CharacterLiveButtonFrame bombFrame,
            in CharacterLiveButtonFrame ropeFrame)
        {
            horizontal = Mathf.Clamp(horizontalAxis, -1f, 1f);
            downHeld = isDownHeld;
            jump.AccumulateFrame(in jumpFrame);
            action.AccumulateFrame(in actionFrame);
            bomb.AccumulateFrame(in bombFrame);
            rope.AccumulateFrame(in ropeFrame);
        }

        /// <summary>
        /// 고정 스텝 소비(FixedUpdate에서 호출): 누적 에지를 담은 논리 입력
        /// 스냅샷을 반환하고 에지를 비운다. held/축 상태는 이어진다.
        /// </summary>
        public CharacterInputSnapshot ConsumeFixedSnapshot(long physicsTick)
        {
            return new CharacterInputSnapshot(
                horizontal,
                downHeld,
                jump.ConsumeSnapshot(physicsTick),
                action.ConsumeSnapshot(physicsTick),
                bomb.ConsumeSnapshot(physicsTick),
                rope.ConsumeSnapshot(physicsTick));
        }

        /// <summary>비활성화/재시작 시 전체 초기화.</summary>
        public void Reset()
        {
            horizontal = 0f;
            downHeld = false;
            jump.Reset();
            action.Reset();
            bomb.Reset();
            rope.Reset();
        }
    }
}
