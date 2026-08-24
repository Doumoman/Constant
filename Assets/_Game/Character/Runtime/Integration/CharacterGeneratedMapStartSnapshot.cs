using StarNight.Character.MapIntegration;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Integration
{
    /// <summary>
    /// 생성 맵 시작 스냅샷 — 생성 결과에서 뽑은 값 데이터만 담는다.
    /// 라이브 생성기 연결(스냅샷 생산)은 후속 통합 계층 소관이다.
    /// </summary>
    public readonly struct CharacterGeneratedMapStartSnapshot
    {
        public CharacterGeneratedMapStartSnapshot(
            int mapRunId,
            CharacterRoomId startRoomId,
            bool hasStartCell,
            WorldTileCoord startCell,
            WorldTileCoord roomMinCell,
            WorldTileCoord roomMaxCell)
        {
            MapRunId = mapRunId;
            StartRoomId = startRoomId;
            HasStartCell = hasStartCell;
            StartCell = startCell;
            RoomMinCell = roomMinCell;
            RoomMaxCell = roomMaxCell;
        }

        public int MapRunId { get; }
        public CharacterRoomId StartRoomId { get; }
        public bool HasStartCell { get; }
        public WorldTileCoord StartCell { get; }

        /// <summary>시작 방 경계(포함, 최소/최대 셀).</summary>
        public WorldTileCoord RoomMinCell { get; }
        public WorldTileCoord RoomMaxCell { get; }
    }
}
