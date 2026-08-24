using System.Collections.Generic;
using StarNight.Character.MapIntegration;
using UnityEngine;

namespace StarNight.Character.Equipment
{
    /// <summary>
    /// 폭발 피해 후보 생성(순수·결정적). 폭발 중심 셀의 월드 중심에서
    /// 반경(셀=월드 단위) 이내의 대상만 피해 후보가 된다.
    /// 후보는 요청 값 객체이며 어떤 상태도 직접 변조하지 않는다.
    /// </summary>
    public static class CharacterExplosionDamagePolicy
    {
        public static void CreateDamageCandidates(
            in CharacterExplosionRequest explosion,
            IReadOnlyList<CharacterExplosionTargetSnapshot> targets,
            List<CharacterEnemyExplosionDamageCandidate> enemyCandidates,
            List<CharacterPlayerExplosionDamageCandidate> playerCandidates)
        {
            enemyCandidates.Clear();
            playerCandidates.Clear();

            if (targets == null)
            {
                return;
            }

            Vector2 center = CharacterMapCoordinateBridge.GetCellCenter(explosion.CenterCell);
            float radius = explosion.RadiusCells * CharacterMapCoordinateBridge.WorldUnitsPerCell;
            float radiusSquared = radius * radius;

            for (int index = 0; index < targets.Count; index++)
            {
                CharacterExplosionTargetSnapshot target = targets[index];
                Vector2 offset = target.Position - center;

                if (offset.sqrMagnitude > radiusSquared)
                {
                    continue;
                }

                Vector2 direction = offset.sqrMagnitude > 0f
                    ? offset.normalized
                    : Vector2.up;

                if (target.IsPlayer)
                {
                    playerCandidates.Add(new CharacterPlayerExplosionDamageCandidate(
                        target.TargetId,
                        explosion.ExplosionId,
                        explosion.DamageAmount,
                        direction));
                }
                else
                {
                    enemyCandidates.Add(new CharacterEnemyExplosionDamageCandidate(
                        target.TargetId,
                        explosion.ExplosionId,
                        explosion.DamageAmount,
                        direction));
                }
            }
        }
    }
}
