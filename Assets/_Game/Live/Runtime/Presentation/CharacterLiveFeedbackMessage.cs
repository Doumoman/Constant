namespace StarNight.Character.Live.Presentation
{
    /// <summary>라이브 피드백 메시지 값 객체(표시용 데이터 전용).</summary>
    public readonly struct CharacterLiveFeedbackMessage
    {
        public CharacterLiveFeedbackMessage(
            CharacterLiveFeedbackCategory category,
            string text)
        {
            Category = category;
            Text = text ?? string.Empty;
        }

        public CharacterLiveFeedbackCategory Category { get; }
        public string Text { get; }
    }
}
