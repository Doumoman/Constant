namespace StarNight.Character.Live.Tools
{
    /// <summary>
    /// 라이브 지형 명령 수신 계약(좁은 표면). MAP/Tilemap 실적용 소비자가
    /// 아직 없으므로 인메모리 큐 구현이 기본이며, 이후 배선 과제가 실제
    /// 적용 소비자로 교체한다.
    /// </summary>
    public interface ICharacterLiveTerrainCommandSink
    {
        void Enqueue(in CharacterLiveTerrainCommand command);
    }
}
