namespace StarNight.Character.Combat
{
    /// <summary>
    /// 접촉 분류 결과. 유효 밟기 = 상단 접촉 ∧ 플레이어 수직 속도 하강 중.
    /// 상승·정지 중 상단 접촉은 밟기가 아니다(잠금 전투 규칙).
    /// </summary>
    public readonly struct CharacterContactClassification
    {
        public CharacterContactClassification(CharacterContactSide side, bool isValidStomp)
        {
            Side = side;
            IsValidStomp = isValidStomp;
        }

        public CharacterContactSide Side { get; }
        public bool IsValidStomp { get; }

        public static CharacterContactClassification None
        {
            get { return new CharacterContactClassification(CharacterContactSide.None, false); }
        }
    }
}
