namespace StarNight.Character.Equipment
{
    /// <summary>
    /// 폭탄 설치 판정(순수·결정적). 보유 수량 > 0 이고 대상 셀이 유효·설치
    /// 가능할 때만 설치 요청 + 소모 요청을 함께 발행한다. 수량 없음, 막힘,
    /// 점유, 범위 밖은 어떤 요청도 발행하지 않는다.
    /// </summary>
    public static class CharacterBombPlacementPolicy
    {
        public static bool TryCreatePlacement(
            in CharacterBombPlacementInput input,
            out CharacterBombPlacementRequest placementRequest,
            out CharacterBombSpendRequest spendRequest)
        {
            placementRequest = default(CharacterBombPlacementRequest);
            spendRequest = default(CharacterBombSpendRequest);

            if (input.AvailableBombCount <= 0)
            {
                return false;
            }

            if (!input.HasValidTargetCell || !input.IsTargetCellPlaceable)
            {
                return false;
            }

            placementRequest = new CharacterBombPlacementRequest(
                input.ActorId, input.TargetCell);
            spendRequest = new CharacterBombSpendRequest(input.ActorId, 1);
            return true;
        }
    }
}
