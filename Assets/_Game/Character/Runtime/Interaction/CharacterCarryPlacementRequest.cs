using UnityEngine;

namespace StarNight.Character.Interaction
{
    /// <summary>
    /// 안전 내려놓기 배치 요청 값 객체. 캐릭터는 Carryable 내부 상태를 직접
    /// 수정하지 않고 이 요청만 발행한다(소비는 오브젝트/월드 계층 소관).
    /// </summary>
    public readonly struct CharacterCarryPlacementRequest
    {
        public CharacterCarryPlacementRequest(
            int heldObjectId,
            int ownerId,
            Vector2 position,
            float ownerCollisionGraceSeconds)
        {
            HeldObjectId = heldObjectId;
            OwnerId = ownerId;
            Position = position;
            OwnerCollisionGraceSeconds = ownerCollisionGraceSeconds;
        }

        public int HeldObjectId { get; }
        public int OwnerId { get; }
        public Vector2 Position { get; }
        public float OwnerCollisionGraceSeconds { get; }
    }
}
