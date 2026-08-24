using StarNight.Character.Input;

namespace StarNight.Character.State
{
    /// <summary>
    /// 플레이어 상태 모델. facing, locomotion, 휴대·기절·사망 플래그와
    /// 입력 잠금 reason set을 추적한다.
    /// 사망·기절·잠금 사유가 있으면 입력을 받을 수 없다.
    /// 카메라룸 전환은 잠금 사유가 아니며(전환 중 입력 KEEP) 별도 플래그로만 추적한다.
    /// </summary>
    public sealed class CharacterPlayerState
    {
        public CharacterPlayerState()
        {
            Facing = CharacterFacingDirection.Right;
            Locomotion = CharacterLocomotionState.Grounded;
            Locks = new CharacterInputLockSet();
        }

        public CharacterFacingDirection Facing { get; private set; }
        public CharacterLocomotionState Locomotion { get; private set; }
        public bool IsCarrying { get; private set; }
        public bool IsStunned { get; private set; }
        public bool IsDead { get; private set; }
        public bool CameraRoomTransitionActive { get; private set; }
        public CharacterInputLockSet Locks { get; }

        public bool CanAcceptInput
        {
            get { return !IsDead && !IsStunned && !Locks.IsLocked; }
        }

        /// <summary>수평 입력이 0이면 기존 facing을 유지한다.</summary>
        public void UpdateFacing(float horizontal)
        {
            if (horizontal > 0f)
            {
                Facing = CharacterFacingDirection.Right;
            }
            else if (horizontal < 0f)
            {
                Facing = CharacterFacingDirection.Left;
            }
        }

        public void SetLocomotion(CharacterLocomotionState locomotion)
        {
            Locomotion = locomotion;
        }

        public void SetCarrying(bool carrying)
        {
            IsCarrying = carrying;
        }

        public void SetStunned(bool stunned)
        {
            IsStunned = stunned;
        }

        public void SetDead(bool dead)
        {
            IsDead = dead;
        }

        /// <summary>
        /// 카메라룸 전환 상태를 추적한다. 전환은 입력 잠금 사유가 아니므로
        /// 어떤 lock reason도 추가하지 않고 CanAcceptInput에 영향을 주지 않는다.
        /// </summary>
        public void SetCameraRoomTransitionActive(bool active)
        {
            CameraRoomTransitionActive = active;
        }

        /// <summary>물리 틱 판정에 사용할 immutable snapshot을 만든다.</summary>
        public CharacterPlayerStateSnapshot CreateSnapshot(long tick)
        {
            return new CharacterPlayerStateSnapshot(
                Facing,
                Locomotion,
                IsCarrying,
                IsStunned,
                IsDead,
                CanAcceptInput,
                Locks.Count,
                CameraRoomTransitionActive,
                tick);
        }
    }
}
