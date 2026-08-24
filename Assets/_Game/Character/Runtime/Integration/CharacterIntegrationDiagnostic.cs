namespace StarNight.Character.Integration
{
    /// <summary>통합 진단 값 객체 — 데이터 전용, 어떤 상태도 변조하지 않는다.</summary>
    public readonly struct CharacterIntegrationDiagnostic
    {
        public CharacterIntegrationDiagnostic(
            CharacterIntegrationDiagnosticKind kind,
            string subject)
        {
            Kind = kind;
            Subject = subject;
        }

        public CharacterIntegrationDiagnosticKind Kind { get; }

        /// <summary>대상 식별 문자열(예: "route:3", "cell:10,5").</summary>
        public string Subject { get; }
    }
}
