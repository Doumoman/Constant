namespace StarNight.Character.Survival
{
    /// <summary>
    /// 환경 위험 판정(순수). 피해형 위험(스파이크/압착/화염/일반)은 통합
    /// 피해 요청으로, Void/월드 이탈은 치명 경로(사망 요청 + 플레이어에
    /// 한해 런 실패 요청)로 변환한다. 라이브 물리 질의·MAP/Tilemap 변조 없음.
    /// </summary>
    public static class CharacterHazardPolicy
    {
        /// <summary>피해형 위험 후보 → 통합 피해 요청(cause 잠금 사상).</summary>
        public static bool TryCreateDamageRequest(
            in CharacterHazardDamageCandidate candidate,
            out CharacterSurvivalDamageRequest request)
        {
            request = default;

            // Void는 피해가 아니라 치명 경로다 — 아래 전용 API를 쓴다.
            if (candidate.HazardKind == CharacterHazardKind.Void)
            {
                return false;
            }

            request = new CharacterSurvivalDamageRequest(
                MapCause(candidate.HazardKind),
                candidate.SourceHazardId,
                candidate.TargetId,
                candidate.TargetKind,
                candidate.Amount,
                candidate.Direction,
                bypassInvulnerability: false);
            return true;
        }

        /// <summary>Void/월드 이탈 — 대상이 누구든 사망 요청(cause Fall).</summary>
        public static CharacterDeathRequest CreateVoidDeathRequest(
            int actorId,
            CharacterSurvivalTargetKind targetKind,
            int sourceHazardId)
        {
            return new CharacterDeathRequest(
                actorId,
                targetKind,
                CharacterDamageSourceKind.Fall,
                sourceHazardId);
        }

        /// <summary>
        /// Void/월드 이탈 런 실패 — 플레이어만 런 실패를 만든다.
        /// 적/비플레이어의 낙사는 런 실패가 아니다.
        /// </summary>
        public static bool TryCreateVoidRunFailure(
            int actorId,
            CharacterSurvivalTargetKind targetKind,
            string returnDestinationToken,
            out CharacterRunFailureRequest runFailureRequest)
        {
            runFailureRequest = default;

            if (targetKind != CharacterSurvivalTargetKind.Player)
            {
                return false;
            }

            runFailureRequest = new CharacterRunFailureRequest(
                CharacterRunFailureReason.VoidOrOutOfBounds,
                actorId,
                returnDestinationToken);
            return true;
        }

        /// <summary>위험 종류 → 스키마 cause 잠금 9종 사상(확장 없음).</summary>
        private static CharacterDamageSourceKind MapCause(CharacterHazardKind kind)
        {
            switch (kind)
            {
                case CharacterHazardKind.Spike:
                    return CharacterDamageSourceKind.Spike;
                case CharacterHazardKind.Crush:
                    return CharacterDamageSourceKind.Crush;
                case CharacterHazardKind.Fire:
                case CharacterHazardKind.Generic:
                default:
                    return CharacterDamageSourceKind.Environment;
            }
        }
    }
}
