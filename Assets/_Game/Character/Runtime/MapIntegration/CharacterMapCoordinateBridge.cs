using StarNight.Map.WorldGeneration.Domain;
using UnityEngine;

namespace StarNight.Character.MapIntegration
{
    /// <summary>
    /// 캐릭터 측 MAP 좌표 브리지. 좌표 수학과 경계 검증은 MAP 공용
    /// <see cref="WorldCoordinateUtility"/>에 위임하고 복제하지 않는다.
    /// 월드 스케일은 잠금 계약 `1 logical cell = 1 world unit`이며
    /// 이 상수가 캐릭터 런타임의 유일한 스케일 소스다(MAP은 타일 단위만 정의).
    /// 범위 밖 좌표는 clamp 없이 거부한다.
    /// </summary>
    public static class CharacterMapCoordinateBridge
    {
        /// <summary>잠금 계약: 1 logical cell = 1 world unit.</summary>
        public const float WorldUnitsPerCell = 1f;

        /// <summary>
        /// 월드 좌표 → MAP 월드 타일. 경계 검증은 MAP 공용 유틸리티에 위임하며
        /// 범위 밖이면 clamp 없이 false를 반환한다(tile은 default).
        /// </summary>
        public static bool TryGetTileCoordinate(Vector2 worldPosition, out WorldTileCoord tile)
        {
            int tileX = Mathf.FloorToInt(worldPosition.x / WorldUnitsPerCell);
            int tileY = Mathf.FloorToInt(worldPosition.y / WorldUnitsPerCell);
            return WorldCoordinateUtility.TryCreateWorldTile(tileX, tileY, out tile);
        }

        /// <summary>타일 → 셀 원점(좌하단) 월드 좌표.</summary>
        public static Vector2 GetCellOrigin(WorldTileCoord tile)
        {
            return new Vector2(tile.X * WorldUnitsPerCell, tile.Y * WorldUnitsPerCell);
        }

        /// <summary>타일 → 셀 중심 월드 좌표.</summary>
        public static Vector2 GetCellCenter(WorldTileCoord tile)
        {
            Vector2 origin = GetCellOrigin(tile);
            return new Vector2(
                origin.x + WorldUnitsPerCell * 0.5f,
                origin.y + WorldUnitsPerCell * 0.5f);
        }
    }
}
