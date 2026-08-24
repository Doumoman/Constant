using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Equipment
{
    /// <summary>
    /// 폭탄 설치 판정 입력 스냅샷. 보유 수량과 대상 셀 질의 결과를 호출자가
    /// 스냅샷으로 제공한다(라이브 입력 배선·ActionId 추가 없음).
    /// 월드 범위 밖 좌표는 브리지 변환 실패로 HasValidTargetCell=false가 된다.
    /// </summary>
    public readonly struct CharacterBombPlacementInput
    {
        public CharacterBombPlacementInput(
            int actorId,
            bool hasValidTargetCell,
            WorldTileCoord targetCell,
            int availableBombCount,
            bool isTargetCellPlaceable)
        {
            ActorId = actorId;
            HasValidTargetCell = hasValidTargetCell;
            TargetCell = targetCell;
            AvailableBombCount = availableBombCount;
            IsTargetCellPlaceable = isTargetCellPlaceable;
        }

        public int ActorId { get; }
        public bool HasValidTargetCell { get; }
        public WorldTileCoord TargetCell { get; }
        public int AvailableBombCount { get; }
        public bool IsTargetCellPlaceable { get; }
    }
}
