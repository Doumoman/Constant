using UnityEngine;

namespace StarNight.Character.Movement
{
    /// <summary>
    /// 캐릭터 충돌 질의 추상화. 실제 Physics2D 어댑터와 테스트용 fake world를
    /// 교체할 수 있다. Tilemap, MAP 데이터 모델, scene object lookup에 의존하지 않는다.
    /// </summary>
    public interface ICharacterCollisionWorld
    {
        /// <summary>캡슐 swept 질의. direction 방향으로 distance만큼 캐스트한다.</summary>
        CharacterCollisionHit CapsuleCast(
            Vector2 origin,
            CharacterCapsuleGeometry capsule,
            Vector2 direction,
            float distance);
    }
}
