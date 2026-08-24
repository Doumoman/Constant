namespace StarNight.Character.RoomTransition
{
    /// <summary>전환 정책 평가 결과. 판정과(요청 발행 시) 요청만 담는 readonly 값 객체.</summary>
    public readonly struct CharacterRoomTransitionResult
    {
        public CharacterRoomTransitionResult(
            CharacterRoomTransitionDecision decision,
            bool hasRequest,
            CharacterRoomTransitionRequest request)
        {
            Decision = decision;
            HasRequest = hasRequest;
            Request = request;
        }

        public CharacterRoomTransitionDecision Decision { get; }
        public bool HasRequest { get; }
        public CharacterRoomTransitionRequest Request { get; }

        public static CharacterRoomTransitionResult Of(CharacterRoomTransitionDecision decision)
        {
            return new CharacterRoomTransitionResult(
                decision, false, default(CharacterRoomTransitionRequest));
        }

        public static CharacterRoomTransitionResult Requested(CharacterRoomTransitionRequest request)
        {
            return new CharacterRoomTransitionResult(
                CharacterRoomTransitionDecision.TransitionRequested, true, request);
        }
    }
}
