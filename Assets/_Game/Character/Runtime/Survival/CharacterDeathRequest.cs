namespace StarNight.Character.Survival
{
    /// <summary>
    /// 사망 요청 값 객체 — GameObject 파괴·애니메이션·연출을 수행하지
    /// 않는다. 적용은 요청 소비자 소관이다.
    /// </summary>
    public readonly struct CharacterDeathRequest
    {
        public CharacterDeathRequest(
            int actorId,
            CharacterSurvivalTargetKind targetKind,
            CharacterDamageSourceKind cause,
            int sourceId)
        {
            ActorId = actorId;
            TargetKind = targetKind;
            Cause = cause;
            SourceId = sourceId;
        }

        public int ActorId { get; }
        public CharacterSurvivalTargetKind TargetKind { get; }
        public CharacterDamageSourceKind Cause { get; }

        /// <summary>원인 인스턴스 식별자(알 수 없으면 0).</summary>
        public int SourceId { get; }
    }
}
