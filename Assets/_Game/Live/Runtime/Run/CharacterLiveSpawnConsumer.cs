using StarNight.Character.Integration;
using StarNight.Character.Live.Movement;
using StarNight.Character.Live.Player;
using UnityEngine;

namespace StarNight.Character.Live.Run
{
    /// <summary>
    /// 스폰 요청 소비자. CharacterPlayerSpawnRequest를 런 시작당 정확히
    /// 한 번 리그에 적용한다 — 위치 원천은 request.WorldCenter 하나뿐이다.
    /// </summary>
    public sealed class CharacterLiveSpawnConsumer
    {
        public bool HasConsumed { get; private set; }

        public bool TryConsume(
            in CharacterPlayerSpawnRequest request,
            CharacterLivePlayerRig rig,
            CharacterLiveMovementDriver movementDriver)
        {
            if (HasConsumed || rig == null || !rig.IsBound)
            {
                return false;
            }

            Vector2 spawnPosition = request.WorldCenter;
            rig.Body.position = spawnPosition;
            rig.transform.position = new Vector3(
                spawnPosition.x, spawnPosition.y, rig.transform.position.z);

            if (movementDriver != null)
            {
                movementDriver.ResetMotion();
            }

            HasConsumed = true;
            return true;
        }
    }
}
