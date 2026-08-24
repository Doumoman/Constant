using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Equipment
{
    /// <summary>
    /// 로프 설치 요청 값 객체 — 프리팹 생성·씬 배치·인벤토리 변조를
    /// 수행하지 않는다.
    /// </summary>
    public readonly struct CharacterRopePlacementRequest
    {
        public CharacterRopePlacementRequest(int actorId, WorldTileCoord originCell)
        {
            ActorId = actorId;
            OriginCell = originCell;
        }

        public int ActorId { get; }
        public WorldTileCoord OriginCell { get; }
    }
}
