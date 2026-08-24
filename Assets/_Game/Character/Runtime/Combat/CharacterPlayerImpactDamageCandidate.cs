namespace StarNight.Character.Combat
{
    /// <summary>
    /// 플레이어 임팩트 피해 후보 슬롯(예약). 결과 구조의 분리 요구를 위해
    /// 존재하며, 현행 규칙(플레이어가 던진 물체)에서는 발행되지 않는다 —
    /// 적 투척물의 플레이어 피해는 미래 계약 소관이다.
    /// </summary>
    public readonly struct CharacterPlayerImpactDamageCandidate
    {
        public CharacterPlayerImpactDamageCandidate(int sourceObjectId, int amount)
        {
            SourceObjectId = sourceObjectId;
            Amount = amount;
        }

        public int SourceObjectId { get; }
        public int Amount { get; }
    }
}
