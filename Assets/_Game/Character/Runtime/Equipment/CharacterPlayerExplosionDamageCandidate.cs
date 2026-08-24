using UnityEngine;

namespace StarNight.Character.Equipment
{
    /// <summary>
    /// 플레이어 폭발 피해 후보 값 객체 — 체력/생존 적용은 CHAR05_03 소관이다.
    /// 자기 폭탄도 반경 안이면 피해 후보가 된다(잠금 전투 규칙의 공용 계약).
    /// </summary>
    public readonly struct CharacterPlayerExplosionDamageCandidate
    {
        public CharacterPlayerExplosionDamageCandidate(
            int targetPlayerId,
            int sourceExplosionId,
            int amount,
            Vector2 directionFromCenter)
        {
            TargetPlayerId = targetPlayerId;
            SourceExplosionId = sourceExplosionId;
            Amount = amount;
            DirectionFromCenter = directionFromCenter;
        }

        public int TargetPlayerId { get; }
        public int SourceExplosionId { get; }
        public int Amount { get; }
        public Vector2 DirectionFromCenter { get; }
    }
}
