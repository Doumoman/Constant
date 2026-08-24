using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Equipment
{
    /// <summary>로프 설치 판정 입력 스냅샷. 라이브 입력 배선 없음.</summary>
    public readonly struct CharacterRopePlacementInput
    {
        public CharacterRopePlacementInput(
            int actorId,
            bool hasValidOriginCell,
            WorldTileCoord originCell,
            int availableRopeCount,
            bool isOriginPlaceable)
        {
            ActorId = actorId;
            HasValidOriginCell = hasValidOriginCell;
            OriginCell = originCell;
            AvailableRopeCount = availableRopeCount;
            IsOriginPlaceable = isOriginPlaceable;
        }

        public int ActorId { get; }
        public bool HasValidOriginCell { get; }
        public WorldTileCoord OriginCell { get; }
        public int AvailableRopeCount { get; }
        public bool IsOriginPlaceable { get; }
    }
}
