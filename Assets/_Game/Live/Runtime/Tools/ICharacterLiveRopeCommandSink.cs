namespace StarNight.Character.Live.Tools
{
    /// <summary>
    /// 라이브 로프 명령 수신 계약(좁은 표면). 로프 프리팹/씬 실적용 소비자가
    /// 아직 없으므로 인메모리 큐 구현이 기본이며, 이후 배선 과제가 실제
    /// 적용 소비자로 교체한다.
    /// </summary>
    public interface ICharacterLiveRopeCommandSink
    {
        void Enqueue(in CharacterLiveRopeCommand command);
    }
}
