namespace StarNight.Character.Combat
{
    /// <summary>
    /// 플레이어 피해 후보 값 객체 — 체력 차감이 아니라 요청이다.
    /// 체력/생존 적용은 CHAR05 소관이다.
    /// </summary>
    public readonly struct CharacterPlayerDamageCandidate
    {
        public CharacterPlayerDamageCandidate(
            int sourceEnemyId,
            CharacterContactSide contactSide,
            int amount)
        {
            SourceEnemyId = sourceEnemyId;
            ContactSide = contactSide;
            Amount = amount;
        }

        public int SourceEnemyId { get; }
        public CharacterContactSide ContactSide { get; }
        public int Amount { get; }
    }
}
