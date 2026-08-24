namespace StarNight.Character.GeneratedRunValidation
{
    /// <summary>
    /// 생성 런 검증 진단 값 객체 — Subject에 대상 식별자(item/room/route
    /// ID·셀)를 담는 데이터 전용 계약이다.
    /// </summary>
    public readonly struct CharacterGeneratedRunValidationDiagnostic
    {
        public CharacterGeneratedRunValidationDiagnostic(
            CharacterGeneratedRunValidationDiagnosticKind kind,
            string subject)
        {
            Kind = kind;
            Subject = subject;
        }

        public CharacterGeneratedRunValidationDiagnosticKind Kind { get; }
        public string Subject { get; }
    }
}
