using StarNight.Character.MapIntegration;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.GeneratedRunValidation
{
    /// <summary>생성 마이크로청크 스냅샷 — 소유 방과 셀 범위 값 데이터.</summary>
    public readonly struct CharacterGeneratedMicrochunkSnapshot
    {
        public CharacterGeneratedMicrochunkSnapshot(
            CharacterRoomId ownerRoomId,
            WorldTileCoord minCell,
            WorldTileCoord maxCell)
        {
            OwnerRoomId = ownerRoomId;
            MinCell = minCell;
            MaxCell = maxCell;
        }

        public CharacterRoomId OwnerRoomId { get; }
        public WorldTileCoord MinCell { get; }
        public WorldTileCoord MaxCell { get; }
    }
}
