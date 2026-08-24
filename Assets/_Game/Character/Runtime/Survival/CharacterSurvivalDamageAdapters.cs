using StarNight.Character.Combat;
using StarNight.Character.Equipment;
using UnityEngine;

namespace StarNight.Character.Survival
{
    /// <summary>
    /// 기존 CHAR04/CHAR05 피해 후보 → 통합 생존 피해 요청 변환(Survival측
    /// 어댑터 — 기존 후보 파일은 수정하지 않는다). 접촉/플레이어 임팩트
    /// 후보는 방향 정보를 갖지 않으므로 Direction은 zero로 둔다(넉백 방향
    /// 해석은 후속 소비자 소관).
    /// </summary>
    public static class CharacterSurvivalDamageAdapters
    {
        /// <summary>적 접촉(측면/하단) 플레이어 피해 후보 → 통합 요청.</summary>
        public static CharacterSurvivalDamageRequest FromContact(
            in CharacterPlayerDamageCandidate candidate,
            int targetPlayerId)
        {
            return new CharacterSurvivalDamageRequest(
                CharacterDamageSourceKind.EnemyContact,
                candidate.SourceEnemyId,
                targetPlayerId,
                CharacterSurvivalTargetKind.Player,
                candidate.Amount,
                Vector2.zero,
                bypassInvulnerability: false);
        }

        /// <summary>투척물 임팩트 플레이어 피해 후보 → 통합 요청.</summary>
        public static CharacterSurvivalDamageRequest FromImpact(
            in CharacterPlayerImpactDamageCandidate candidate,
            int targetPlayerId)
        {
            return new CharacterSurvivalDamageRequest(
                CharacterDamageSourceKind.ThrownObject,
                candidate.SourceObjectId,
                targetPlayerId,
                CharacterSurvivalTargetKind.Player,
                candidate.Amount,
                Vector2.zero,
                bypassInvulnerability: false);
        }

        /// <summary>투척물 임팩트 적 피해 후보 → 통합 요청.</summary>
        public static CharacterSurvivalDamageRequest FromImpact(
            in CharacterEnemyImpactDamageCandidate candidate)
        {
            return new CharacterSurvivalDamageRequest(
                CharacterDamageSourceKind.ThrownObject,
                candidate.SourceObjectId,
                candidate.TargetEnemyId,
                CharacterSurvivalTargetKind.Enemy,
                candidate.Amount,
                candidate.ImpactDirection,
                bypassInvulnerability: false);
        }

        /// <summary>폭발 플레이어 피해 후보(자해 포함) → 통합 요청.</summary>
        public static CharacterSurvivalDamageRequest FromExplosion(
            in CharacterPlayerExplosionDamageCandidate candidate)
        {
            return new CharacterSurvivalDamageRequest(
                CharacterDamageSourceKind.Explosion,
                candidate.SourceExplosionId,
                candidate.TargetPlayerId,
                CharacterSurvivalTargetKind.Player,
                candidate.Amount,
                candidate.DirectionFromCenter,
                bypassInvulnerability: false);
        }

        /// <summary>폭발 적 피해 후보 → 통합 요청.</summary>
        public static CharacterSurvivalDamageRequest FromExplosion(
            in CharacterEnemyExplosionDamageCandidate candidate)
        {
            return new CharacterSurvivalDamageRequest(
                CharacterDamageSourceKind.Explosion,
                candidate.SourceExplosionId,
                candidate.TargetEnemyId,
                CharacterSurvivalTargetKind.Enemy,
                candidate.Amount,
                candidate.DirectionFromCenter,
                bypassInvulnerability: false);
        }
    }
}
