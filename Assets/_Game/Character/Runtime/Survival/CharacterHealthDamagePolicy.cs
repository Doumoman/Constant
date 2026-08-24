using UnityEngine;

namespace StarNight.Character.Survival
{
    /// <summary>
    /// 피해 적용 정책(순수·결정적). 입력 상태를 변조하지 않고 결과 값
    /// (새 상태 + 사망 요청 여부)만 반환한다. HUD·점수·넉백·연출 없음.
    /// </summary>
    public static class CharacterHealthDamagePolicy
    {
        public static CharacterDamageApplicationResult ApplyDamage(
            in CharacterHealthState state,
            in CharacterSurvivalDamageRequest request,
            in CharacterSurvivalSettings settings)
        {
            // 대상 불일치·0 이하 피해량은 변화 없음.
            if (request.TargetId != state.ActorId
                || request.TargetKind != state.TargetKind
                || request.Amount <= 0
                || state.IsDepleted)
            {
                return Unchanged(in state, suppressed: false);
            }

            // 무적 중이면 명시적 bypass 요청만 관통한다(스키마 기본 false).
            if (state.IsInvulnerable && !request.BypassInvulnerability)
            {
                return Unchanged(in state, suppressed: true);
            }

            int newCurrent = Mathf.Max(0, state.CurrentHealth - request.Amount);
            int applied = state.CurrentHealth - newCurrent;
            bool lethal = newCurrent == 0;

            // 비치명 피격 플레이어에게만 피격 후 무적을 부여한다
            // (적 경직은 CHAR04 기절 계약 소관).
            float invulnerability = 0f;
            if (!lethal && state.TargetKind == CharacterSurvivalTargetKind.Player)
            {
                invulnerability = settings.PostHitInvulnerabilitySeconds;
            }

            var newState = new CharacterHealthState(
                state.ActorId,
                state.TargetKind,
                newCurrent,
                state.MaxHealth,
                invulnerability);

            if (!lethal)
            {
                return new CharacterDamageApplicationResult(
                    newState, applied, false, false, default);
            }

            var deathRequest = new CharacterDeathRequest(
                state.ActorId,
                state.TargetKind,
                request.SourceKind,
                request.SourceId);

            return new CharacterDamageApplicationResult(
                newState, applied, false, true, deathRequest);
        }

        private static CharacterDamageApplicationResult Unchanged(
            in CharacterHealthState state,
            bool suppressed)
        {
            return new CharacterDamageApplicationResult(
                state, 0, suppressed, false, default);
        }
    }
}
