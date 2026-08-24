namespace StarNight.Character.Combat
{
    /// <summary>
    /// 밟기의 적 측 결과 값 객체. 플레이어 속도를 직접 변조하지 않는다
    /// (플레이어 반동은 별도 요청으로 분리).
    /// </summary>
    public readonly struct CharacterStompEnemyResult
    {
        public CharacterStompEnemyResult(
            int enemyId,
            CharacterStompOutcome outcome,
            float stunDurationSeconds)
        {
            EnemyId = enemyId;
            Outcome = outcome;
            StunDurationSeconds = stunDurationSeconds;
        }

        public int EnemyId { get; }
        public CharacterStompOutcome Outcome { get; }
        public float StunDurationSeconds { get; }
    }
}
