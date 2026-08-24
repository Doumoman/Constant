using System.Collections.Generic;
using StarNight.Character.MapIntegration;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Equipment
{
    /// <summary>
    /// 폭발 기하 → 지형 변경 요청 생성(순수·결정적).
    /// 영향 셀 열거는 y 오름차순→x 오름차순으로 결정적이며 구성상 중복이 없다.
    /// 반경 내 셀만 고려하고, 범위 밖 셀은 MAP 공용 좌표 검증으로 제외한다.
    /// 파괴 가능(Breakable) 셀만 변경 요청을 만들고 비파괴 고체·빈 셀·데이터
    /// 없는 셀은 건너뛴다. MAP·Tilemap을 직접 변조하지 않는다.
    /// </summary>
    public static class CharacterExplosionTerrainPolicy
    {
        /// <summary>
        /// 폭발 영향 셀 열거. 셀 중심 간 유클리드 거리(dx²+dy² ≤ r²) 기준,
        /// 결정적 순서(y asc → x asc), 범위 밖 제외, 중복 없음.
        /// </summary>
        public static List<WorldTileCoord> EnumerateAffectedCells(
            WorldTileCoord centerCell,
            float radiusCells)
        {
            var cells = new List<WorldTileCoord>();
            int radiusBound = (int)radiusCells + 1;
            float radiusSquared = radiusCells * radiusCells;

            for (int offsetY = -radiusBound; offsetY <= radiusBound; offsetY++)
            {
                for (int offsetX = -radiusBound; offsetX <= radiusBound; offsetX++)
                {
                    if (offsetX * offsetX + offsetY * offsetY > radiusSquared)
                    {
                        continue;
                    }

                    WorldTileCoord cell;
                    if (!WorldCoordinateUtility.TryCreateWorldTile(
                        centerCell.X + offsetX, centerCell.Y + offsetY, out cell))
                    {
                        continue;
                    }

                    cells.Add(cell);
                }
            }

            return cells;
        }

        /// <summary>파괴 가능 셀에 대해서만 지형 변경 요청을 생성한다.</summary>
        public static List<CharacterTerrainMutationRequest> CreateTerrainMutationRequests(
            in CharacterExplosionRequest explosion,
            ICharacterMapWorldQuery worldQuery)
        {
            var requests = new List<CharacterTerrainMutationRequest>();
            List<WorldTileCoord> affectedCells =
                EnumerateAffectedCells(explosion.CenterCell, explosion.RadiusCells);

            for (int index = 0; index < affectedCells.Count; index++)
            {
                WorldTileCoord cell = affectedCells[index];

                CharacterMapCellState state;
                if (worldQuery == null || !worldQuery.TryGetCellState(cell, out state))
                {
                    // 데이터 없는(미생성) 셀은 건너뛴다.
                    continue;
                }

                if (!state.IsBreakable)
                {
                    // 비파괴 고체·빈 셀·위험 셀 등은 지형 변경 대상이 아니다.
                    continue;
                }

                requests.Add(new CharacterTerrainMutationRequest(
                    cell,
                    CharacterTerrainMutationIntent.DestroyBreakable,
                    explosion.ExplosionId));
            }

            return requests;
        }
    }
}
