using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Presentation
{
    /// <summary>
    /// 연출 이벤트 요청 값 객체 — 오디오·애니메이션·파티클·카메라·UI·씬
    /// 어떤 효과도 직접 재생하지 않는다. SequenceId는 배치 정규화가 부여하는
    /// 결정적 순번이다.
    /// </summary>
    public readonly struct CharacterPresentationEventRequest
    {
        public CharacterPresentationEventRequest(
            CharacterPresentationEventType type,
            int actorOrSourceId,
            bool hasAmount,
            int amount,
            bool hasCell,
            WorldTileCoord cell,
            int sequenceId)
        {
            Type = type;
            ActorOrSourceId = actorOrSourceId;
            HasAmount = hasAmount;
            Amount = amount;
            HasCell = hasCell;
            Cell = cell;
            SequenceId = sequenceId;
        }

        public CharacterPresentationEventType Type { get; }
        public int ActorOrSourceId { get; }
        public bool HasAmount { get; }
        public int Amount { get; }
        public bool HasCell { get; }
        public WorldTileCoord Cell { get; }
        public int SequenceId { get; }
    }
}
