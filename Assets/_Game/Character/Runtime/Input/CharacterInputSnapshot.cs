namespace StarNight.Character.Input
{
    /// <summary>
    /// 장치 입력을 변환한 논리 입력 스냅샷. 캐릭터 로직은 이 스냅샷만 소비한다.
    /// Down+Action 조합은 SafeDrop으로 계산되며 같은 틱에서는 SafeDrop이
    /// 단독 Action보다 우선한다. 별도 일반 공격 intent는 노출하지 않는다.
    /// </summary>
    public readonly struct CharacterInputSnapshot
    {
        public CharacterInputSnapshot(
            float horizontal,
            bool downHeld,
            CharacterButtonSnapshot jump,
            CharacterButtonSnapshot action,
            CharacterButtonSnapshot bomb,
            CharacterButtonSnapshot rope)
        {
            Horizontal = Clamp(horizontal, -1f, 1f);
            DownHeld = downHeld;
            Jump = jump;
            Action = action;
            Bomb = bomb;
            Rope = rope;
        }

        /// <summary>수평 이동 intent. [-1, 1]로 클램프된다.</summary>
        public float Horizontal { get; }

        /// <summary>하강 축 유지 여부. SafeDrop 조합 계산에 사용한다.</summary>
        public bool DownHeld { get; }

        public CharacterButtonSnapshot Jump { get; }
        public CharacterButtonSnapshot Action { get; }
        public CharacterButtonSnapshot Bomb { get; }
        public CharacterButtonSnapshot Rope { get; }

        /// <summary>Down+Action 조합의 SafeDrop intent.</summary>
        public bool SafeDropPressedThisFrame
        {
            get { return DownHeld && Action.PressedThisFrame; }
        }

        /// <summary>
        /// 단독 Action intent. 같은 틱에 SafeDrop이 성립하면 SafeDrop이 우선하므로
        /// 단독 Action으로 보고하지 않는다.
        /// </summary>
        public bool PlainActionPressedThisFrame
        {
            get { return Action.PressedThisFrame && !DownHeld; }
        }

        public bool IsPressedThisFrame(CharacterActionId actionId)
        {
            switch (actionId)
            {
                case CharacterActionId.Jump:
                    return Jump.PressedThisFrame;
                case CharacterActionId.Action:
                    return PlainActionPressedThisFrame;
                case CharacterActionId.SafeDrop:
                    return SafeDropPressedThisFrame;
                case CharacterActionId.Bomb:
                    return Bomb.PressedThisFrame;
                case CharacterActionId.Rope:
                    return Rope.PressedThisFrame;
                default:
                    return false;
            }
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}
