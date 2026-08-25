namespace StarNight.Character.Live.Adapters
{
    /// <summary>어댑터 진단 값 객체 — 데이터 전용.</summary>
    public readonly struct CharacterLiveGeneratedMapDiagnostic
    {
        public CharacterLiveGeneratedMapDiagnostic(
            CharacterLiveGeneratedMapDiagnosticKind kind,
            string subject)
        {
            Kind = kind;
            Subject = subject;
        }

        public CharacterLiveGeneratedMapDiagnosticKind Kind { get; }
        public string Subject { get; }
    }
}
