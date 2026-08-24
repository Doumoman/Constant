using StarNight.Character.MapIntegration;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.GeneratedRunValidation
{
    /// <summary>생성 아이템 배치 스냅샷 — 아이템 ID·선언 방·셀 값 데이터.</summary>
    public readonly struct CharacterGeneratedItemPlacementSnapshot
    {
        public CharacterGeneratedItemPlacementSnapshot(
            int itemId,
            CharacterRoomId roomId,
            WorldTileCoord cell)
        {
            ItemId = itemId;
            RoomId = roomId;
            Cell = cell;
        }

        public int ItemId { get; }
        public CharacterRoomId RoomId { get; }
        public WorldTileCoord Cell { get; }
    }
}
