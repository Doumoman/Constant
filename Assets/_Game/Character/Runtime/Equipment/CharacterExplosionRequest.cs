using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Equipment
{
    /// <summary>
    /// 폭발 요청 값 객체. 이펙트 생성·오브젝트 파괴·연출을 하지 않는다 —
    /// 소비(지형 변경 적용·피해 적용)는 각 계층 소관이다.
    /// </summary>
    public readonly struct CharacterExplosionRequest
    {
        public CharacterExplosionRequest(
            int explosionId,
            int ownerId,
            WorldTileCoord centerCell,
            float radiusCells,
            int damageAmount)
        {
            ExplosionId = explosionId;
            OwnerId = ownerId;
            CenterCell = centerCell;
            RadiusCells = radiusCells;
            DamageAmount = damageAmount;
        }

        public int ExplosionId { get; }
        public int OwnerId { get; }
        public WorldTileCoord CenterCell { get; }
        public float RadiusCells { get; }
        public int DamageAmount { get; }
    }
}
