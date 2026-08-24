using UnityEngine;

namespace StarNight.Character.Combat
{
    /// <summary>
    /// 임팩트 판정 정책(순수·결정적). 규칙:
    /// - 정지·최소 속도 미만 소스 → 이벤트 없음
    /// - 소유자 유예 활성 + 소유자/자기 대상 → 억제(이벤트 없음)
    /// - 이동 중 투척물 + 적대 적 → 적 임팩트 피해 후보(HP·기절·제거 미적용)
    /// - 비적대 대상(기절 휴대물 등) → 명시적 적대가 아니면 피해 없음
    /// - 이동 중 투척물 + 고체 월드 → 오브젝트 정지 요청만(지형 불변)
    /// - 플레이어 피해 슬롯은 예약(현행 미발행 — 적 투척물은 미래 계약)
    /// 밟기 기절/제거 흐름(CHAR04_02)과 병합하지 않는다.
    /// Animator/물리 콜백은 판정 권한이 아니다.
    /// </summary>
    public sealed class CharacterImpactPolicy
    {
        private readonly CharacterImpactSettings settings;

        public CharacterImpactPolicy(CharacterImpactSettings settings)
        {
            this.settings = settings;
        }

        public CharacterImpactSettings Settings
        {
            get { return settings; }
        }

        public CharacterImpactResult Evaluate(
            in CharacterImpactSource source,
            in CharacterImpactTarget target)
        {
            // 정지 또는 최소 임팩트 속도 미만 — 이벤트 없음.
            if (source.Velocity.magnitude < settings.MinimumImpactSpeed)
            {
                return CharacterImpactResult.None;
            }

            // 소유자 충돌 유예: 유예 중 소유자/자기 대상 임팩트는 억제된다.
            if (source.IsOwnerGraceActive
                && target.TargetKind != CharacterImpactTargetKind.SolidWorld
                && target.TargetId == source.OwnerId)
            {
                return CharacterImpactResult.None;
            }

            if (target.TargetKind == CharacterImpactTargetKind.SolidWorld)
            {
                return new CharacterImpactResult(
                    true, new CharacterObjectStopRequest(source.ObjectId),
                    false, default(CharacterEnemyImpactDamageCandidate),
                    false, default(CharacterPlayerImpactDamageCandidate));
            }

            if (target.TargetKind == CharacterImpactTargetKind.Enemy)
            {
                if (!target.IsHostile)
                {
                    // 기절/비적대 휴대 대상은 명시적 적대가 아니면 피해가 아니다.
                    return CharacterImpactResult.None;
                }

                var candidate = new CharacterEnemyImpactDamageCandidate(
                    source.ObjectId,
                    target.TargetId,
                    source.Velocity.normalized,
                    settings.ThrownEnemyDamageAmount);
                return new CharacterImpactResult(
                    false, default(CharacterObjectStopRequest),
                    true, candidate,
                    false, default(CharacterPlayerImpactDamageCandidate));
            }

            // Player 대상: 플레이어 피해 슬롯은 예약 상태 — 현행 규칙에서 미발행.
            return CharacterImpactResult.None;
        }
    }
}
