namespace StarNight.Character.MapIntegration
{
    /// <summary>
    /// 방 준비 상태 read-only 소스. 방 정보가 아예 없으면(미생성·미등록)
    /// false를 반환한다. 라이브 생성 맵 소스 연결은 CHAR06 소관이다.
    /// </summary>
    public interface ICharacterRoomReadinessSource
    {
        bool TryGetRoomReadiness(CharacterRoomId room, out bool isReady);
    }
}
