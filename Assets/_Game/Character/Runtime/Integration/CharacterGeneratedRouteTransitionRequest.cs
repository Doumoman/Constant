using StarNight.Character.MapIntegration;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Integration
{
    /// <summary>
    /// 생성 루트 전환 요청 값 객체. 좌표·방 데이터만 담는다 — 카메라 이동·
    /// 씬 로드·플레이어 transform 이동·MAP 변조를 수행하지 않으며, 입력/속도
    /// 필드 자체가 없어 CHAR03 KEEP 계약을 구조적으로 침해할 수 없다
    /// (카메라 전환·hysteresis는 CHAR03_02 정책 소관 그대로).
    /// </summary>
    public readonly struct CharacterGeneratedRouteTransitionRequest
    {
        public CharacterGeneratedRouteTransitionRequest(
            int routeId,
            CharacterRoomId sourceRoom,
            CharacterRoomId targetRoom,
            CharacterRouteBoundarySide boundarySide,
            WorldTileCoord targetEntryCell)
        {
            RouteId = routeId;
            SourceRoom = sourceRoom;
            TargetRoom = targetRoom;
            BoundarySide = boundarySide;
            TargetEntryCell = targetEntryCell;
        }

        public int RouteId { get; }
        public CharacterRoomId SourceRoom { get; }
        public CharacterRoomId TargetRoom { get; }
        public CharacterRouteBoundarySide BoundarySide { get; }
        public WorldTileCoord TargetEntryCell { get; }
    }
}
