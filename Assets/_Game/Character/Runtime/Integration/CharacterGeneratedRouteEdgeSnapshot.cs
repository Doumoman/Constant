using StarNight.Character.MapIntegration;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Integration
{
    /// <summary>
    /// 생성 맵이 선언한 루트 엣지 스냅샷(값 데이터). 출발 방의 경계 이탈
    /// 셀과 도착 방의 진입 셀을 함께 기록해 CHAR03 경계 게이트를 그대로
    /// 재사용할 수 있게 한다.
    /// </summary>
    public readonly struct CharacterGeneratedRouteEdgeSnapshot
    {
        public CharacterGeneratedRouteEdgeSnapshot(
            int routeId,
            CharacterRoomId sourceRoom,
            CharacterRoomId targetRoom,
            CharacterRouteBoundarySide boundarySide,
            WorldTileCoord sourceExitCell,
            WorldTileCoord targetEntryCell,
            CharacterRouteRequirement requirement)
        {
            RouteId = routeId;
            SourceRoom = sourceRoom;
            TargetRoom = targetRoom;
            BoundarySide = boundarySide;
            SourceExitCell = sourceExitCell;
            TargetEntryCell = targetEntryCell;
            Requirement = requirement;
        }

        public int RouteId { get; }
        public CharacterRoomId SourceRoom { get; }
        public CharacterRoomId TargetRoom { get; }
        public CharacterRouteBoundarySide BoundarySide { get; }
        public WorldTileCoord SourceExitCell { get; }
        public WorldTileCoord TargetEntryCell { get; }
        public CharacterRouteRequirement Requirement { get; }
    }
}
