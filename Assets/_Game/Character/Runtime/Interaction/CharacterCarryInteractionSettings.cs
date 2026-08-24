using System;
using UnityEngine;

namespace StarNight.Character.Interaction
{
    /// <summary>
    /// 휴대 상호작용 튜닝. owner collision grace는 여기서만 중앙 관리되며
    /// drop/throw 요청에 항상 포함된다(물리 레이어를 직접 수정하지 않는다).
    /// 기본값은 기준선이며 최종 수치 검증은 이후 코스 검증 소관이다.
    /// </summary>
    public readonly struct CharacterCarryInteractionSettings
    {
        public CharacterCarryInteractionSettings(
            Vector2 safeDropOffset,
            float throwSpeed,
            float ownerCollisionGraceSeconds)
        {
            if (throwSpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(throwSpeed), "throwSpeed는 0보다 커야 한다.");
            }

            if (ownerCollisionGraceSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ownerCollisionGraceSeconds),
                    "owner collision grace는 0 이상이어야 한다.");
            }

            SafeDropOffset = safeDropOffset;
            ThrowSpeed = throwSpeed;
            OwnerCollisionGraceSeconds = ownerCollisionGraceSeconds;
        }

        /// <summary>발밑 기준 안전 내려놓기 오프셋.</summary>
        public Vector2 SafeDropOffset { get; }

        public float ThrowSpeed { get; }

        /// <summary>중앙 관리되는 소유자 충돌 유예(초).</summary>
        public float OwnerCollisionGraceSeconds { get; }

        public static CharacterCarryInteractionSettings Default
        {
            get
            {
                return new CharacterCarryInteractionSettings(
                    Vector2.zero, 7f, 0.25f);
            }
        }
    }
}
