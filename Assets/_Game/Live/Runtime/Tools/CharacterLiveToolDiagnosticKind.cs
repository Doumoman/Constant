namespace StarNight.Character.Live.Tools
{
    /// <summary>
    /// 라이브 도구 소비 진단. 거부 사유를 결정적으로 기술만 하며
    /// 어떤 거부도 라이브 상태를 변조하지 않는다.
    /// </summary>
    public enum CharacterLiveToolDiagnosticKind
    {
        None,

        /// <summary>같은 요청 id 재소비 시도 — 수락된 요청은 정확히 한 번만 소비된다.</summary>
        DuplicateRequest,

        // 휴대(carry/drop/throw)
        NoCarryTarget,
        InvalidCarryTarget,
        AlreadyCarrying,
        TargetAlreadyCarried,
        NoCarriedTarget,
        BlockedDrop,

        // 폭탄
        NoBombStock,
        InvalidBombPlacement,
        MissingTerrainSink,

        // 로프
        NoRopeStock,
        InvalidRopeAnchor,
        BlockedRopeAnchor,
        MissingRopeSink
    }
}
