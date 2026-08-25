namespace StarNight.Character.Live.Tools
{
    /// <summary>도구 소비 시도 결과 값 객체. 수락이면 진단은 항상 None이다.</summary>
    public readonly struct CharacterLiveToolUseResult
    {
        public CharacterLiveToolUseResult(
            bool accepted,
            CharacterLiveToolDiagnosticKind diagnostic)
        {
            Accepted = accepted;
            Diagnostic = diagnostic;
        }

        public bool Accepted { get; }
        public CharacterLiveToolDiagnosticKind Diagnostic { get; }

        public static CharacterLiveToolUseResult Success()
        {
            return new CharacterLiveToolUseResult(
                true, CharacterLiveToolDiagnosticKind.None);
        }

        public static CharacterLiveToolUseResult Rejected(
            CharacterLiveToolDiagnosticKind diagnostic)
        {
            return new CharacterLiveToolUseResult(false, diagnostic);
        }
    }
}
