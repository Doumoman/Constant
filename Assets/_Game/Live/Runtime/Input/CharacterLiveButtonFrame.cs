namespace StarNight.Character.Live.Input
{
    /// <summary>렌더 프레임 1회분의 버튼 관측값(장치 계층 → 누적기 전달용).</summary>
    public readonly struct CharacterLiveButtonFrame
    {
        public CharacterLiveButtonFrame(
            bool pressedThisFrame,
            bool releasedThisFrame,
            bool isHeld)
        {
            PressedThisFrame = pressedThisFrame;
            ReleasedThisFrame = releasedThisFrame;
            IsHeld = isHeld;
        }

        public bool PressedThisFrame { get; }
        public bool ReleasedThisFrame { get; }
        public bool IsHeld { get; }
    }
}
