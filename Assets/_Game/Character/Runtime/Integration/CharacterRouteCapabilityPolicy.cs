using StarNight.Character.RunState;

namespace StarNight.Character.Integration
{
    /// <summary>
    /// 루트 역량 판정(순수·진단 전용). 잠금 이동 문법은 항상 수용, 폭탄/로프
    /// 요구는 보유 수량이 있을 때만 수용, 잠금 밖 요구는 항상 거부.
    /// 인벤토리를 소모하거나 상태를 변조하지 않는다.
    /// </summary>
    public static class CharacterRouteCapabilityPolicy
    {
        public static bool IsRouteSupported(
            CharacterRouteRequirement requirement,
            in CharacterRunInventoryState inventory,
            int routeId,
            out CharacterIntegrationDiagnostic diagnostic)
        {
            diagnostic = default;
            string subject = "route:" + routeId;

            switch (requirement)
            {
                case CharacterRouteRequirement.BasicMovement:
                    // 2셀 점프/2셀 틈 문법은 잠금 이동 프로필이 보장한다
                    // (CHAR02 코스 검증 완료).
                    return true;

                case CharacterRouteRequirement.BombSupport:
                    if (inventory.BombCount > 0)
                    {
                        return true;
                    }

                    diagnostic = new CharacterIntegrationDiagnostic(
                        CharacterIntegrationDiagnosticKind.MissingBombSupport,
                        subject);
                    return false;

                case CharacterRouteRequirement.RopeSupport:
                    if (inventory.RopeCount > 0)
                    {
                        return true;
                    }

                    diagnostic = new CharacterIntegrationDiagnostic(
                        CharacterIntegrationDiagnosticKind.MissingRopeSupport,
                        subject);
                    return false;

                case CharacterRouteRequirement.UnsupportedAdvancedMovement:
                case CharacterRouteRequirement.UnsupportedCombatAction:
                default:
                    // 잠금 밖 이동/공격 요구는 어떤 상태에서도 거부한다.
                    diagnostic = new CharacterIntegrationDiagnostic(
                        CharacterIntegrationDiagnosticKind.UnsupportedRouteRequirement,
                        subject);
                    return false;
            }
        }
    }
}
