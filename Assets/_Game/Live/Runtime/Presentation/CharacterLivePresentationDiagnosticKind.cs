namespace StarNight.Character.Live.Presentation
{
    /// <summary>연출 이벤트 소비 진단.</summary>
    public enum CharacterLivePresentationDiagnosticKind
    {
        None,

        /// <summary>배치 내 내용 동등 중복 — 캐릭터 정규화가 1건만 남긴다.</summary>
        DuplicateEvent,

        /// <summary>알 수 없는 이벤트 타입 — 피드백 없이 건너뛴다.</summary>
        UnknownEvent,

        /// <summary>액터 범위 이벤트의 대상 불일치(세션 미시작 포함).</summary>
        MissingTarget,

        /// <summary>피드백 sink 부재 — 배치 전체 무소비.</summary>
        MissingSink
    }
}
