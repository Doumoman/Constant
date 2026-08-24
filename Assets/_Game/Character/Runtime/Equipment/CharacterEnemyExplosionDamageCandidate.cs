using UnityEngine;

namespace StarNight.Character.Equipment
{
    /// <summary>
    /// 적 폭발 피해 후보 값 객체 — 요청일 뿐 HP·기절·제거·사망·점수·넉백·
    /// 연출을 적용하지 않는다.
    /// </summary>
    public readonly struct CharacterEnemyExplosionDamageCandidate
    {
        public CharacterEnemyExplosionDamageCandidate(
            int targetEnemyId,
            int sourceExplosionId,
            int amount,
            Vector2 directionFromCenter)
        {
            TargetEnemyId = targetEnemyId;
            SourceExplosionId = sourceExplosionId;
            Amount = amount;
            DirectionFromCenter = directionFromCenter;
        }

        public int TargetEnemyId { get; }
        public int SourceExplosionId { get; }
        public int Amount { get; }
        public Vector2 DirectionFromCenter { get; }
    }
}
