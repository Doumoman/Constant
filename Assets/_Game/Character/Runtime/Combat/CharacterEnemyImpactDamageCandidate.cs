using UnityEngine;

namespace StarNight.Character.Combat
{
    /// <summary>
    /// 투척물의 적 임팩트 피해 후보 값 객체 — 요청일 뿐 적 HP·기절·제거·사망·
    /// 점수·연출을 적용하지 않는다(소비는 적/월드 계층과 후속 단계 소관).
    /// </summary>
    public readonly struct CharacterEnemyImpactDamageCandidate
    {
        public CharacterEnemyImpactDamageCandidate(
            int sourceObjectId,
            int targetEnemyId,
            Vector2 impactDirection,
            int amount)
        {
            SourceObjectId = sourceObjectId;
            TargetEnemyId = targetEnemyId;
            ImpactDirection = impactDirection;
            Amount = amount;
        }

        public int SourceObjectId { get; }
        public int TargetEnemyId { get; }
        public Vector2 ImpactDirection { get; }
        public int Amount { get; }
    }
}
