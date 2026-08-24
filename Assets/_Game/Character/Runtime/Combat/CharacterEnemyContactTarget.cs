namespace StarNight.Character.Combat
{
    /// <summary>
    /// 접촉 대상 적의 판정용 스냅샷 값 객체. 캐릭터는 적 내부 상태를 직접
    /// 수정하지 않고 이 스냅샷으로 판정만 한다.
    /// </summary>
    public readonly struct CharacterEnemyContactTarget
    {
        public CharacterEnemyContactTarget(
            int enemyId,
            bool isSmallEnemy,
            bool isHostile,
            bool isStunned)
        {
            EnemyId = enemyId;
            IsSmallEnemy = isSmallEnemy;
            IsHostile = isHostile;
            IsStunned = isStunned;
        }

        public int EnemyId { get; }
        public bool IsSmallEnemy { get; }
        public bool IsHostile { get; }
        public bool IsStunned { get; }
    }
}
