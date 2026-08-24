using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Equipment
{
    /// <summary>폭탄 설치 요청 값 객체 — 프리팹 생성·인벤토리 변조를 하지 않는다.</summary>
    public readonly struct CharacterBombPlacementRequest
    {
        public CharacterBombPlacementRequest(int actorId, WorldTileCoord targetCell)
        {
            ActorId = actorId;
            TargetCell = targetCell;
        }

        public int ActorId { get; }
        public WorldTileCoord TargetCell { get; }
    }
}
