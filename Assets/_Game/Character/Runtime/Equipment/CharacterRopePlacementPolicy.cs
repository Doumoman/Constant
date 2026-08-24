namespace StarNight.Character.Equipment
{
    /// <summary>
    /// 로프 설치 판정(순수·결정적). 보유 수량과 원점 셀 유효성을 스냅샷으로
    /// 받아 설치 요청 + 소모 요청 쌍을 만들거나 아무것도 만들지 않는다.
    /// </summary>
    public static class CharacterRopePlacementPolicy
    {
        public static bool TryCreatePlacement(
            in CharacterRopePlacementInput input,
            out CharacterRopePlacementRequest placementRequest,
            out CharacterRopeSpendRequest spendRequest)
        {
            placementRequest = default;
            spendRequest = default;

            if (input.AvailableRopeCount <= 0)
            {
                return false;
            }

            if (!input.HasValidOriginCell || !input.IsOriginPlaceable)
            {
                return false;
            }

            placementRequest = new CharacterRopePlacementRequest(
                input.ActorId, input.OriginCell);
            spendRequest = new CharacterRopeSpendRequest(input.ActorId, 1);
            return true;
        }
    }
}
