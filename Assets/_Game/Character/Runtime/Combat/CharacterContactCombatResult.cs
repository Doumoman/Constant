namespace StarNight.Character.Combat
{
    /// <summary>
    /// 접촉 전투 평가 결과. 적 결과·플레이어 반동·피해 후보를 분리해 담는
    /// readonly 값 객체다.
    /// </summary>
    public readonly struct CharacterContactCombatResult
    {
        public CharacterContactCombatResult(
            bool hasEnemyResult,
            CharacterStompEnemyResult enemyResult,
            bool hasRebound,
            CharacterStompReboundRequest rebound,
            bool hasPlayerDamageCandidate,
            CharacterPlayerDamageCandidate playerDamageCandidate)
        {
            HasEnemyResult = hasEnemyResult;
            EnemyResult = enemyResult;
            HasRebound = hasRebound;
            Rebound = rebound;
            HasPlayerDamageCandidate = hasPlayerDamageCandidate;
            PlayerDamageCandidate = playerDamageCandidate;
        }

        public bool HasEnemyResult { get; }
        public CharacterStompEnemyResult EnemyResult { get; }
        public bool HasRebound { get; }
        public CharacterStompReboundRequest Rebound { get; }
        public bool HasPlayerDamageCandidate { get; }
        public CharacterPlayerDamageCandidate PlayerDamageCandidate { get; }

        public static CharacterContactCombatResult None
        {
            get
            {
                return new CharacterContactCombatResult(
                    false, default(CharacterStompEnemyResult),
                    false, default(CharacterStompReboundRequest),
                    false, default(CharacterPlayerDamageCandidate));
            }
        }
    }
}
