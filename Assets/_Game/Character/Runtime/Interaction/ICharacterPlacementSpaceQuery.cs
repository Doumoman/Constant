using UnityEngine;

namespace StarNight.Character.Interaction
{
    /// <summary>
    /// 내려놓기 목적지 공간 질의(read-only). 겹침 배치를 막기 위한 점유 확인만 한다.
    /// 라이브 소스(물리/맵 점유) 연결은 이후 통합 소관이며 테스트는 결정적 fake를 쓴다.
    /// </summary>
    public interface ICharacterPlacementSpaceQuery
    {
        bool IsPlacementFree(Vector2 position);
    }
}
