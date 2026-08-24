namespace StarNight.Character.Survival
{
    /// <summary>
    /// 피해 적용 결과 값 객체. 입력 상태는 그대로이고 새 상태가 여기 담긴다.
    /// 치명 피해면 사망 요청이 함께 실린다(적용은 소비자 소관).
    /// </summary>
    public readonly struct CharacterDamageApplicationResult
    {
        public CharacterDamageApplicationResult(
            CharacterHealthState newState,
            int appliedAmount,
            bool wasSuppressedByInvulnerability,
            bool hasDeathRequest,
            CharacterDeathRequest deathRequest)
        {
            NewState = newState;
            AppliedAmount = appliedAmount;
            WasSuppressedByInvulnerability = wasSuppressedByInvulnerability;
            HasDeathRequest = hasDeathRequest;
            DeathRequest = deathRequest;
        }

        public CharacterHealthState NewState { get; }
        public int AppliedAmount { get; }
        public bool WasSuppressedByInvulnerability { get; }
        public bool HasDeathRequest { get; }
        public CharacterDeathRequest DeathRequest { get; }
    }
}
