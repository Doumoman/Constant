namespace StarNight.Character.MapIntegration
{
    /// <summary>경계 통과 판정. 게이트는 판정만 반환하며 아무것도 변조하지 않는다.</summary>
    public enum CharacterBoundaryCrossDecision
    {
        /// <summary>같은 방 내부 이동 — 게이트 무영향.</summary>
        NotABoundaryCrossing,

        /// <summary>목적지 방이 준비됨 — 통과 허용.</summary>
        Allowed,

        /// <summary>목적지 방이 존재하나 준비되지 않음 — 통과 차단.</summary>
        BlockedUnpreparedRoom,

        /// <summary>목적지 방 정보 없음 — 통과 차단.</summary>
        BlockedMissingRoom
    }
}
