namespace StarNight.Character.Input
{
    /// <summary>
    /// 단일 버튼의 프레임 스냅샷. Update에서 수집되어 물리 틱에서 소비된다.
    /// 렌더 프레임 성공 여부가 아니라 수집 틱 값으로만 판정에 사용한다.
    /// </summary>
    public readonly struct CharacterButtonSnapshot
    {
        public CharacterButtonSnapshot(
            bool pressedThisFrame,
            bool held,
            bool releasedThisFrame,
            bool consumed,
            long tick)
        {
            PressedThisFrame = pressedThisFrame;
            Held = held;
            ReleasedThisFrame = releasedThisFrame;
            Consumed = consumed;
            Tick = tick;
        }

        public bool PressedThisFrame { get; }
        public bool Held { get; }
        public bool ReleasedThisFrame { get; }
        public bool Consumed { get; }
        public long Tick { get; }

        public static CharacterButtonSnapshot Idle(long tick)
        {
            return new CharacterButtonSnapshot(false, false, false, false, tick);
        }

        public static CharacterButtonSnapshot Pressed(long tick)
        {
            return new CharacterButtonSnapshot(true, true, false, false, tick);
        }

        public static CharacterButtonSnapshot Released(long tick)
        {
            return new CharacterButtonSnapshot(false, false, true, false, tick);
        }

        public CharacterButtonSnapshot AsConsumed()
        {
            return new CharacterButtonSnapshot(PressedThisFrame, Held, ReleasedThisFrame, true, Tick);
        }
    }
}
