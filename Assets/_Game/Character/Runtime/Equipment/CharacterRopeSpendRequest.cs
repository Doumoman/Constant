namespace StarNight.Character.Equipment
{
    /// <summary>
    /// 로프 소모 요청 값 객체 — ropeCount 실제 차감은 요청 소비자
    /// (후속 단계) 소관이며 여기서는 수량을 변조하지 않는다.
    /// </summary>
    public readonly struct CharacterRopeSpendRequest
    {
        public CharacterRopeSpendRequest(int actorId, int amount)
        {
            ActorId = actorId;
            Amount = amount;
        }

        public int ActorId { get; }
        public int Amount { get; }
    }
}
