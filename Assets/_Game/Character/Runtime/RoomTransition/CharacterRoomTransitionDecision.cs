namespace StarNight.Character.RoomTransition
{
    /// <summary>카메라룸 전환 정책 판정. 정책은 판정/요청만 내고 아무것도 변조하지 않는다.</summary>
    public enum CharacterRoomTransitionDecision
    {
        /// <summary>활성 방 내부(또는 평가 불가 위치) — 전환 없음.</summary>
        NoTransition,

        /// <summary>준비된 목표 방을 감지했으나 hysteresis 안정 조건 대기 중.</summary>
        PendingStabilization,

        /// <summary>전환 요청 발행(요청은 source/target 방 정보만 담는다).</summary>
        TransitionRequested,

        /// <summary>목표 방 미준비 — 기존 준비 게이트가 차단.</summary>
        BlockedUnpreparedRoom,

        /// <summary>목표 방 정보 없음 — 기존 준비 게이트가 차단.</summary>
        BlockedMissingRoom
    }
}
