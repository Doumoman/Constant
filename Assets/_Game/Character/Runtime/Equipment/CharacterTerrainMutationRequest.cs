using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Equipment
{
    /// <summary>
    /// 지형 변경 요청 값 객체 — 셀 좌표와 변경 의도만 담는다.
    /// MAP·Tilemap·타일 자산을 직접 변조하지 않으며(잠금 규칙: 요청/결과 계약),
    /// 적용은 MAP 측 변경 계약 소비자의 소관이다.
    /// </summary>
    public readonly struct CharacterTerrainMutationRequest
    {
        public CharacterTerrainMutationRequest(
            WorldTileCoord cell,
            CharacterTerrainMutationIntent intent,
            int sourceExplosionId)
        {
            Cell = cell;
            Intent = intent;
            SourceExplosionId = sourceExplosionId;
        }

        public WorldTileCoord Cell { get; }
        public CharacterTerrainMutationIntent Intent { get; }
        public int SourceExplosionId { get; }
    }
}
