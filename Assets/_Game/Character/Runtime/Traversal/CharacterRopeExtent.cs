using System.Collections.Generic;
using StarNight.Character.MapIntegration;

namespace StarNight.Character.Traversal
{
    /// <summary>
    /// 생성된 로프의 등반 가능 범위. 상·하한은 최하단/최상단 세그먼트 셀의
    /// 월드 중심 Y이며, 세그먼트는 경계 안에서만 생성되므로 이 범위를
    /// 벗어나지 않는 한 월드 경계도 넘지 않는다.
    /// </summary>
    public readonly struct CharacterRopeExtent
    {
        public CharacterRopeExtent(
            int columnX,
            int bottomCellY,
            int topCellY,
            float bottomWorldY,
            float topWorldY)
        {
            ColumnX = columnX;
            BottomCellY = bottomCellY;
            TopCellY = topCellY;
            BottomWorldY = bottomWorldY;
            TopWorldY = topWorldY;
        }

        public int ColumnX { get; }
        public int BottomCellY { get; }
        public int TopCellY { get; }
        public float BottomWorldY { get; }
        public float TopWorldY { get; }

        public static bool TryCreateFromSegments(
            IReadOnlyList<CharacterRopeSegmentRequest> segments,
            out CharacterRopeExtent extent)
        {
            extent = default;

            if (segments == null || segments.Count == 0)
            {
                return false;
            }

            int columnX = segments[0].Cell.X;
            int bottomCellY = segments[0].Cell.Y;
            int topCellY = segments[0].Cell.Y;
            var bottomCell = segments[0].Cell;
            var topCell = segments[0].Cell;

            for (int index = 1; index < segments.Count; index++)
            {
                var cell = segments[index].Cell;

                if (cell.Y < bottomCellY)
                {
                    bottomCellY = cell.Y;
                    bottomCell = cell;
                }

                if (cell.Y > topCellY)
                {
                    topCellY = cell.Y;
                    topCell = cell;
                }
            }

            extent = new CharacterRopeExtent(
                columnX,
                bottomCellY,
                topCellY,
                CharacterMapCoordinateBridge.GetCellCenter(bottomCell).y,
                CharacterMapCoordinateBridge.GetCellCenter(topCell).y);
            return true;
        }
    }
}
