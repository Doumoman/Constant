using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.MapIntegration
{
    /// <summary>
    /// 카메라룸 식별자. 방 단위는 MAP 마이크로청크(12×8)이며 전역 식별은
    /// (Sector, MicroChunk) 쌍이다. 좌표 분해는 MAP 공용
    /// <see cref="WorldCoordinateUtility"/>에 위임한다(복제 금지).
    /// </summary>
    public readonly struct CharacterRoomId : IEquatable<CharacterRoomId>
    {
        public CharacterRoomId(SectorCoord sector, MicroChunkCoord microChunk)
        {
            Sector = sector;
            MicroChunk = microChunk;
        }

        public SectorCoord Sector { get; }
        public MicroChunkCoord MicroChunk { get; }

        public static CharacterRoomId FromWorldTile(WorldTileCoord tile)
        {
            return new CharacterRoomId(
                WorldCoordinateUtility.ToSector(tile),
                WorldCoordinateUtility.ToMicroChunk(tile));
        }

        public bool Equals(CharacterRoomId other)
        {
            return Sector.Equals(other.Sector) && MicroChunk.Equals(other.MicroChunk);
        }

        public override bool Equals(object obj)
        {
            return obj is CharacterRoomId other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Sector.GetHashCode() * 397) ^ MicroChunk.GetHashCode();
            }
        }
    }
}
