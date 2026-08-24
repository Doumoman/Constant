namespace StarNight.Character.Traversal
{
    /// <summary>
    /// 로프 등반 모터 요청 값 객체 — 수직 성분만 기술한다(수평 성분·추가
    /// 공중 제어 없음). 플레이어 상태/속도 실제 반영은 요청 소비자 소관이다.
    /// </summary>
    public readonly struct CharacterRopeClimbMotorRequest
    {
        public CharacterRopeClimbMotorRequest(
            int actorId,
            float verticalVelocity,
            float targetWorldY)
        {
            ActorId = actorId;
            VerticalVelocity = verticalVelocity;
            TargetWorldY = targetWorldY;
        }

        public int ActorId { get; }
        public float VerticalVelocity { get; }
        public float TargetWorldY { get; }
    }
}
