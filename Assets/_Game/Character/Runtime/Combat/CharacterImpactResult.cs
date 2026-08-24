namespace StarNight.Character.Combat
{
    /// <summary>
    /// 임팩트 평가 결과. 소스 오브젝트 요청·적 피해 후보·플레이어 피해 후보를
    /// 분리 슬롯으로 담는 readonly 값 객체다.
    /// </summary>
    public readonly struct CharacterImpactResult
    {
        public CharacterImpactResult(
            bool hasObjectStopRequest,
            CharacterObjectStopRequest objectStopRequest,
            bool hasEnemyDamageCandidate,
            CharacterEnemyImpactDamageCandidate enemyDamageCandidate,
            bool hasPlayerDamageCandidate,
            CharacterPlayerImpactDamageCandidate playerDamageCandidate)
        {
            HasObjectStopRequest = hasObjectStopRequest;
            ObjectStopRequest = objectStopRequest;
            HasEnemyDamageCandidate = hasEnemyDamageCandidate;
            EnemyDamageCandidate = enemyDamageCandidate;
            HasPlayerDamageCandidate = hasPlayerDamageCandidate;
            PlayerDamageCandidate = playerDamageCandidate;
        }

        public bool HasObjectStopRequest { get; }
        public CharacterObjectStopRequest ObjectStopRequest { get; }
        public bool HasEnemyDamageCandidate { get; }
        public CharacterEnemyImpactDamageCandidate EnemyDamageCandidate { get; }
        public bool HasPlayerDamageCandidate { get; }
        public CharacterPlayerImpactDamageCandidate PlayerDamageCandidate { get; }

        public static CharacterImpactResult None
        {
            get
            {
                return new CharacterImpactResult(
                    false, default(CharacterObjectStopRequest),
                    false, default(CharacterEnemyImpactDamageCandidate),
                    false, default(CharacterPlayerImpactDamageCandidate));
            }
        }
    }
}
