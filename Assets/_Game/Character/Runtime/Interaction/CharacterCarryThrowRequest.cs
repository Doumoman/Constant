using UnityEngine;

namespace StarNight.Character.Interaction
{
    /// <summary>
    /// 방향 투척 요청 값 객체. 방향/속도/소유자/휴대물 id와 중앙 관리되는
    /// 소유자 충돌 유예를 담는다. 투척 임팩트 피해는 본 요청의 소관이 아니다.
    /// </summary>
    public readonly struct CharacterCarryThrowRequest
    {
        public CharacterCarryThrowRequest(
            int heldObjectId,
            int ownerId,
            CharacterThrowDirection direction,
            Vector2 directionVector,
            float speed,
            float ownerCollisionGraceSeconds)
        {
            HeldObjectId = heldObjectId;
            OwnerId = ownerId;
            Direction = direction;
            DirectionVector = directionVector;
            Speed = speed;
            OwnerCollisionGraceSeconds = ownerCollisionGraceSeconds;
        }

        public int HeldObjectId { get; }
        public int OwnerId { get; }
        public CharacterThrowDirection Direction { get; }
        public Vector2 DirectionVector { get; }
        public float Speed { get; }
        public float OwnerCollisionGraceSeconds { get; }
    }
}
