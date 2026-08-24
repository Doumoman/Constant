namespace StarNight.Character.Combat
{
    /// <summary>
    /// 접촉 전투 판정 정책(순수·결정적). 규칙:
    /// - 유효 밟기(하강 상단 접촉) + 일반 소형 적 → 기절 결과 + 플레이어 반동, 피해 후보 없음
    /// - 유효 밟기 + 기절한 소형 적 → 제거 결과 + 반동
    /// - 유효 밟기 + 비소형 적 → 반동만(기절/제거 흐름은 소형 적 전용 — 별도 계약은 미래 소관)
    /// - 측면/하단 접촉 + 적대 적 → 플레이어 피해 후보(체력 차감 아님)
    /// - 기절 등 비적대 대상 접촉 → 비피해(문서화된 동작)
    /// - 상승/정지 상단 접촉·분리 → 전투 이벤트 없음
    /// 적 결과와 플레이어 반동은 분리된 값 객체다.
    /// </summary>
    public sealed class CharacterContactCombatPolicy
    {
        private readonly CharacterContactCombatSettings settings;

        public CharacterContactCombatPolicy(CharacterContactCombatSettings settings)
        {
            this.settings = settings;
        }

        public CharacterContactCombatSettings Settings
        {
            get { return settings; }
        }

        public CharacterContactCombatResult Evaluate(
            in CharacterContactClassification classification,
            in CharacterEnemyContactTarget enemy)
        {
            if (classification.Side == CharacterContactSide.None)
            {
                return CharacterContactCombatResult.None;
            }

            if (classification.IsValidStomp)
            {
                return EvaluateStomp(in enemy);
            }

            if (classification.Side == CharacterContactSide.Side
                || classification.Side == CharacterContactSide.Bottom)
            {
                if (!enemy.IsHostile)
                {
                    // 기절한 휴대 가능 대상 등 비적대 접촉은 비피해.
                    return CharacterContactCombatResult.None;
                }

                var candidate = new CharacterPlayerDamageCandidate(
                    enemy.EnemyId,
                    classification.Side,
                    settings.ContactDamageAmount);
                return new CharacterContactCombatResult(
                    false, default(CharacterStompEnemyResult),
                    false, default(CharacterStompReboundRequest),
                    true, candidate);
            }

            // 상승/정지 중 상단 접촉: 밟기도 피해도 아님(중립).
            return CharacterContactCombatResult.None;
        }

        private CharacterContactCombatResult EvaluateStomp(
            in CharacterEnemyContactTarget enemy)
        {
            var rebound = new CharacterStompReboundRequest(settings.StompReboundVelocity);

            if (!enemy.IsSmallEnemy)
            {
                // 소형 적이 아니면 기절/제거 흐름 없이 반동만.
                return new CharacterContactCombatResult(
                    false, default(CharacterStompEnemyResult),
                    true, rebound,
                    false, default(CharacterPlayerDamageCandidate));
            }

            CharacterStompEnemyResult enemyResult = enemy.IsStunned
                ? new CharacterStompEnemyResult(
                    enemy.EnemyId, CharacterStompOutcome.Removed, 0f)
                : new CharacterStompEnemyResult(
                    enemy.EnemyId, CharacterStompOutcome.Stunned,
                    settings.StunDurationSeconds);

            return new CharacterContactCombatResult(
                true, enemyResult,
                true, rebound,
                false, default(CharacterPlayerDamageCandidate));
        }
    }
}
