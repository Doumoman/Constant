namespace StarNight.Character.Survival
{
    /// <summary>
    /// 런 실패 판정(순수). 플레이어 사망만 런 실패를 만들고, 적/비플레이어
    /// 사망은 절대 런 실패를 만들지 않는다.
    /// </summary>
    public static class CharacterRunFailurePolicy
    {
        public static bool TryCreateFromDeath(
            in CharacterDeathRequest deathRequest,
            string returnDestinationToken,
            out CharacterRunFailureRequest runFailureRequest)
        {
            runFailureRequest = default;

            if (deathRequest.TargetKind != CharacterSurvivalTargetKind.Player)
            {
                return false;
            }

            runFailureRequest = new CharacterRunFailureRequest(
                CharacterRunFailureReason.PlayerDeath,
                deathRequest.ActorId,
                returnDestinationToken);
            return true;
        }
    }
}
