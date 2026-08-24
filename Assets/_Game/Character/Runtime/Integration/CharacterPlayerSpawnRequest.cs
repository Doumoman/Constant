using StarNight.Character.MapIntegration;
using StarNight.Map.WorldGeneration.Domain;
using UnityEngine;

namespace StarNight.Character.Integration
{
    /// <summary>
    /// 플레이어 스폰 요청 값 객체 — GameObject 생성·이동·활성/비활성·상태
    /// 변조를 수행하지 않는다. 실제 스폰 적용은 라이브 통합 계층 소관이다.
    /// </summary>
    public readonly struct CharacterPlayerSpawnRequest
    {
        public CharacterPlayerSpawnRequest(
            int actorId,
            WorldTileCoord startCell,
            Vector2 worldCenter,
            CharacterRoomId startRoomId)
        {
            ActorId = actorId;
            StartCell = startCell;
            WorldCenter = worldCenter;
            StartRoomId = startRoomId;
        }

        public int ActorId { get; }
        public WorldTileCoord StartCell { get; }
        public Vector2 WorldCenter { get; }
        public CharacterRoomId StartRoomId { get; }
    }
}
