namespace StarNight.Character.State
{
    /// <summary>
    /// 프레임/틱 판정에 사용하는 readonly 상태 값 객체.
    /// Animator 이벤트, 사운드, 렌더 프레임 성공 여부에 의존하지 않는다.
    /// </summary>
    public readonly struct CharacterPlayerStateSnapshot
    {
        public CharacterPlayerStateSnapshot(
            CharacterFacingDirection facing,
            CharacterLocomotionState locomotion,
            bool isCarrying,
            bool isStunned,
            bool isDead,
            bool canAcceptInput,
            int lockReasonCount,
            bool cameraRoomTransitionActive,
            long tick)
        {
            Facing = facing;
            Locomotion = locomotion;
            IsCarrying = isCarrying;
            IsStunned = isStunned;
            IsDead = isDead;
            CanAcceptInput = canAcceptInput;
            LockReasonCount = lockReasonCount;
            CameraRoomTransitionActive = cameraRoomTransitionActive;
            Tick = tick;
        }

        public CharacterFacingDirection Facing { get; }
        public CharacterLocomotionState Locomotion { get; }
        public bool IsCarrying { get; }
        public bool IsStunned { get; }
        public bool IsDead { get; }
        public bool CanAcceptInput { get; }
        public int LockReasonCount { get; }
        public bool CameraRoomTransitionActive { get; }
        public long Tick { get; }
    }
}
