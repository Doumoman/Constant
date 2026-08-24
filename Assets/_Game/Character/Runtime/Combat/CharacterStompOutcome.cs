namespace StarNight.Character.Combat
{
    /// <summary>밟기의 적 측 결과. 일반 소형 적: 첫 밟기 기절 → 두 번째 밟기 제거.</summary>
    public enum CharacterStompOutcome
    {
        None,
        Stunned,
        Removed
    }
}
