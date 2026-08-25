using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.Character.Live.Adapters
{
    /// <summary>
    /// 생성 출력 1건: 월드에 배치된 마이크로청크(위치 + 공용 정의 + 변환).
    /// MAP 공용 런타임 계약만 운반한다.
    /// </summary>
    public readonly struct CharacterLivePlacedMicrochunk
    {
        public CharacterLivePlacedMicrochunk(
            SectorCoord sector,
            MicroChunkCoord chunk,
            MicrochunkDefinition definition,
            MicrochunkTransform transform)
        {
            Sector = sector;
            Chunk = chunk;
            Definition = definition;
            Transform = transform;
        }

        public SectorCoord Sector { get; }
        public MicroChunkCoord Chunk { get; }
        public MicrochunkDefinition Definition { get; }
        public MicrochunkTransform Transform { get; }
    }
}
