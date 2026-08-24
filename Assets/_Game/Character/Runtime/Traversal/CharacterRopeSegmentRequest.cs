using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Traversal
{
    /// <summary>
    /// 로프 세그먼트 요청 값 객체 — 셀 하나가 등반 가능 구간임을 기술할 뿐
    /// MAP/Tilemap/씬/프리팹/물리 에셋을 변조하지 않는다.
    /// </summary>
    public readonly struct CharacterRopeSegmentRequest
    {
        public CharacterRopeSegmentRequest(
            int ropeId,
            WorldTileCoord cell,
            int indexFromOrigin)
        {
            RopeId = ropeId;
            Cell = cell;
            IndexFromOrigin = indexFromOrigin;
        }

        public int RopeId { get; }
        public WorldTileCoord Cell { get; }
        public int IndexFromOrigin { get; }
    }
}
