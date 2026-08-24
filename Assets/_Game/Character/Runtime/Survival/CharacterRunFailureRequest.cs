namespace StarNight.Character.Survival
{
    /// <summary>
    /// 런 실패 요청 값 객체. 복귀/재시도 목적지는 불투명 토큰 데이터일 뿐이며
    /// 씬 리로드·세이브 변조·UI·플레이어 transform 이동을 수행하지 않는다.
    /// (런 상태 HUD/연출 브리지는 CHAR05_04 소관.)
    /// </summary>
    public readonly struct CharacterRunFailureRequest
    {
        public CharacterRunFailureRequest(
            CharacterRunFailureReason reason,
            int actorId,
            string returnDestinationToken)
        {
            Reason = reason;
            ActorId = actorId;
            ReturnDestinationToken = returnDestinationToken;
        }

        public CharacterRunFailureReason Reason { get; }
        public int ActorId { get; }

        /// <summary>선택적 복귀 목적지 토큰(없으면 null/빈 문자열).</summary>
        public string ReturnDestinationToken { get; }

        public bool HasReturnDestination
        {
            get { return !string.IsNullOrEmpty(ReturnDestinationToken); }
        }
    }
}
