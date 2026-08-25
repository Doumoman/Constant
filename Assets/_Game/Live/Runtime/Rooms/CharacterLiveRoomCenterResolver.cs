using StarNight.Character.MapIntegration;
using StarNight.Map.WorldGeneration.Domain;
using UnityEngine;

namespace StarNight.Character.Live.Rooms
{
    /// <summary>
    /// 방 중심 좌표 해석(결정적). MAP 공용 좌표 계약(ToWorld/GetCellOrigin/
    /// WorldGenConstants)만 사용한다 — 상수 복제 없음.
    /// </summary>
    public static class CharacterLiveRoomCenterResolver
    {
        private const float RoomWidthWorld =
            WorldGenConstants.MicroChunkWidthTiles
            * CharacterMapCoordinateBridge.WorldUnitsPerCell;

        private const float RoomHeightWorld =
            WorldGenConstants.MicroChunkHeightTiles
            * CharacterMapCoordinateBridge.WorldUnitsPerCell;

        public static Vector2 GetRoomCenter(CharacterRoomId room)
        {
            WorldTileCoord originTile = WorldCoordinateUtility.ToWorld(
                room.Sector, room.MicroChunk, new LocalTileCoord(0, 0));
            Vector2 min = CharacterMapCoordinateBridge.GetCellOrigin(originTile);
            return new Vector2(
                min.x + RoomWidthWorld * 0.5f,
                min.y + RoomHeightWorld * 0.5f);
        }

        /// <summary>방 안의 유효 앵커 타일(방 최소 셀) — 정책 재정착용.</summary>
        public static WorldTileCoord GetRoomAnchorTile(CharacterRoomId room)
        {
            return WorldCoordinateUtility.ToWorld(
                room.Sector, room.MicroChunk, new LocalTileCoord(0, 0));
        }
    }
}
