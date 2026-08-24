namespace StarNight.Character.Equipment
{
    /// <summary>
    /// 폭탄 소모 요청 값 객체 — 인벤토리를 직접 변조하지 않고 요청만 발행한다
    /// (수량 반영은 인벤토리/런 상태 계층 소관, CHAR05_04 연계).
    /// </summary>
    public readonly struct CharacterBombSpendRequest
    {
        public CharacterBombSpendRequest(int actorId, int amount)
        {
            ActorId = actorId;
            Amount = amount;
        }

        public int ActorId { get; }
        public int Amount { get; }
    }
}
