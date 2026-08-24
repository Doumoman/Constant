using StarNight.Character.MapIntegration;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.GeneratedRunValidation
{
    /// <summary>생성 방 스냅샷 — 방 ID와 포함 경계(최소/최대 셀) 값 데이터.</summary>
    public readonly struct CharacterGeneratedRoomSnapshot
    {
        public CharacterGeneratedRoomSnapshot(
            CharacterRoomId roomId,
            WorldTileCoord minCell,
            WorldTileCoord maxCell)
        {
            RoomId = roomId;
            MinCell = minCell;
            MaxCell = maxCell;
        }

        public CharacterRoomId RoomId { get; }
        public WorldTileCoord MinCell { get; }
        public WorldTileCoord MaxCell { get; }

        public bool ContainsCell(WorldTileCoord cell)
        {
            return cell.X >= MinCell.X && cell.X <= MaxCell.X
                && cell.Y >= MinCell.Y && cell.Y <= MaxCell.Y;
        }
    }
}
