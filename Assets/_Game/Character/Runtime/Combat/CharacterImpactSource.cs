using UnityEngine;

namespace StarNight.Character.Combat
{
    /// <summary>
    /// 임팩트 소스 스냅샷 값 객체. CHAR04_01 투척 요청의 소유자·유예 계약을
    /// 소비 측이 이 스냅샷으로 전달한다(투척 동작 재작성 없음).
    /// </summary>
    public readonly struct CharacterImpactSource
    {
        public CharacterImpactSource(
            int objectId,
            int ownerId,
            bool hasOwner,
            CharacterImpactSourceKind sourceKind,
            Vector2 velocity,
            float ownerGraceRemainingSeconds)
        {
            ObjectId = objectId;
            OwnerId = ownerId;
            HasOwner = hasOwner;
            SourceKind = sourceKind;
            Velocity = velocity;
            OwnerGraceRemainingSeconds = ownerGraceRemainingSeconds;
        }

        public int ObjectId { get; }
        public int OwnerId { get; }
        public bool HasOwner { get; }
        public CharacterImpactSourceKind SourceKind { get; }
        public Vector2 Velocity { get; }

        /// <summary>남은 소유자 충돌 유예(초). 0 이하이면 만료.</summary>
        public float OwnerGraceRemainingSeconds { get; }

        public bool IsOwnerGraceActive
        {
            get { return HasOwner && OwnerGraceRemainingSeconds > 0f; }
        }
    }
}
