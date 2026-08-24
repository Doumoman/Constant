namespace StarNight.Character.Survival
{
    /// <summary>
    /// 환경 위험 종류. Spike/Crush는 스키마 cause에 직접 대응하고,
    /// Fire/Generic은 cause `Environment`로 사상한다(cause 잠금 유지).
    /// Void는 피해가 아니라 치명(낙사/이탈) 경로다.
    /// </summary>
    public enum CharacterHazardKind
    {
        Spike,
        Crush,
        Fire,
        Generic,
        Void
    }
}
