using StarNight.Character.Interaction;
using UnityEngine;

namespace StarNight.Character.Combat
{
    /// <summary>
    /// 기절한 소형 적을 CHAR04_01 휴대 후보 계약으로 노출하는 브리지.
    /// 잠금 규칙: 기절한 소형 적은 1×1 이하 휴대 대상이다.
    /// carry/drop/throw 동작 자체는 재작성하지 않는다.
    /// </summary>
    public static class CharacterStunnedEnemyCarryBridge
    {
        public static bool TryCreateCarryCandidate(
            in CharacterEnemyContactTarget enemy,
            Vector2 position,
            float widthInCells,
            float heightInCells,
            int priority,
            out CharacterCarryCandidate candidate)
        {
            candidate = default(CharacterCarryCandidate);

            // 기절한 소형 적만 휴대 후보가 된다.
            if (!enemy.IsSmallEnemy || !enemy.IsStunned)
            {
                return false;
            }

            candidate = new CharacterCarryCandidate(
                enemy.EnemyId,
                CharacterCarryCandidateKind.StunnedSmallEnemy,
                position,
                widthInCells,
                heightInCells,
                true,
                true,
                priority);
            return true;
        }
    }
}
