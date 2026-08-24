using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.MapIntegration
{
    /// <summary>
    /// 캐릭터용 read-only 월드 질의 계약. 캐릭터는 Tilemap, scene object,
    /// CSV authoring, 마이크로청크 배치 내부, 생성 pass를 직접 읽지 않고
    /// 이 계약만 소비한다. 라이브 생성 맵 데이터 소스 연결은 CHAR06 소관이며
    /// 그 전까지는 결정적 fake 구현으로 검증한다.
    /// </summary>
    public interface ICharacterMapWorldQuery
    {
        /// <summary>
        /// 타일의 캐릭터 관점 셀 상태. 데이터가 없는(미생성/미로드) 타일이면
        /// false를 반환한다.
        /// </summary>
        bool TryGetCellState(WorldTileCoord tile, out CharacterMapCellState state);
    }
}
