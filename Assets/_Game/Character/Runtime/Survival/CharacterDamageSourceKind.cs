namespace StarNight.Character.Survival
{
    /// <summary>
    /// 피해 원인. CHARACTER_DAMAGE_SCHEMA의 cause 잠금 9종과 정확히 일치하며
    /// 확장은 CHANGE CONTROL 소관이다(별도 일반 공격 cause 없음).
    /// </summary>
    public enum CharacterDamageSourceKind
    {
        Stomp,
        ThrownObject,
        Explosion,
        ToolHit,
        EnemyContact,
        Spike,
        Fall,
        Crush,
        Environment
    }
}
