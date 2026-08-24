namespace StarNight.Character.Combat
{
    /// <summary>
    /// 고체 월드 임팩트의 소스 오브젝트 정지/안착 요청 값 객체.
    /// 지형을 변경하지 않고(지형 변경은 CHAR05_01 소관), 그 자체로
    /// 플레이어·적 피해나 연출을 만들지 않는다.
    /// </summary>
    public readonly struct CharacterObjectStopRequest
    {
        public CharacterObjectStopRequest(int objectId)
        {
            ObjectId = objectId;
        }

        public int ObjectId { get; }
    }
}
