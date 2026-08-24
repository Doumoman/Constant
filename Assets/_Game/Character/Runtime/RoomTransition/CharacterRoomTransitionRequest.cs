using StarNight.Character.MapIntegration;

namespace StarNight.Character.RoomTransition
{
    /// <summary>
    /// 카메라룸 전환 요청 값 객체. source/target 방 식별자만 담는다 —
    /// 카메라를 직접 움직이지 않고 플레이어 위치도 변조하지 않는다.
    /// </summary>
    public readonly struct CharacterRoomTransitionRequest
    {
        public CharacterRoomTransitionRequest(CharacterRoomId sourceRoom, CharacterRoomId targetRoom)
        {
            SourceRoom = sourceRoom;
            TargetRoom = targetRoom;
        }

        public CharacterRoomId SourceRoom { get; }
        public CharacterRoomId TargetRoom { get; }
    }
}
