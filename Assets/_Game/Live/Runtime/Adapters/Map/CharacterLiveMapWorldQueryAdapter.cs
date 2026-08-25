using System.Collections.Generic;
using StarNight.Character.MapIntegration;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Live.Adapters
{
    /// <summary>
    /// 생성 셀 상태의 라이브 월드 질의(불변, ICharacterMapWorldQuery 호환).
    /// 생성된 셀만 상태를 보고한다 — 미생성/미지 셀은 false를 반환하며
    /// 평범한 빈 통과 공간으로 취급하지 않는다(생성된 빈 셀은 Empty 상태로
    /// true 반환 — 구분 유지).
    /// </summary>
    public sealed class CharacterLiveMapWorldQueryAdapter : ICharacterMapWorldQuery
    {
        private readonly Dictionary<long, CharacterMapCellState> cells;

        public CharacterLiveMapWorldQueryAdapter(
            Dictionary<long, CharacterMapCellState> generatedCells)
        {
            cells = generatedCells ?? new Dictionary<long, CharacterMapCellState>();
        }

        public int GeneratedCellCount
        {
            get { return cells.Count; }
        }

        public bool TryGetCellState(WorldTileCoord tile, out CharacterMapCellState state)
        {
            return cells.TryGetValue(Key(tile.X, tile.Y), out state);
        }

        /// <summary>월드 타일 사전 키(투영기와 공유하는 유일 키 규약).</summary>
        public static long Key(int x, int y)
        {
            return ((long)y << 32) | (uint)x;
        }
    }
}
