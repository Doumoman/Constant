using System;
using UnityEngine;

namespace StarNight.Character.Interaction
{
    /// <summary>
    /// 단일 슬롯 휴대 상호작용. 들기/안전 내려놓기/방향 투척을 요청 값 객체로만
    /// 처리한다 — Carryable 내부 상태·씬·물리를 직접 수정하지 않는다.
    /// 슬롯은 수락된 drop/throw에서만 비워지고, 거부된 요청은 슬롯을 유지한다.
    /// </summary>
    public sealed class CharacterCarryInteraction
    {
        private readonly CharacterCarryInteractionSettings settings;
        private readonly ICharacterPlacementSpaceQuery placementQuery;
        private readonly int ownerId;

        public CharacterCarryInteraction(
            CharacterCarryInteractionSettings settings,
            ICharacterPlacementSpaceQuery placementQuery,
            int ownerId)
        {
            if (placementQuery == null)
            {
                throw new ArgumentNullException(nameof(placementQuery));
            }

            this.settings = settings;
            this.placementQuery = placementQuery;
            this.ownerId = ownerId;
        }

        public CharacterCarryInteractionSettings Settings
        {
            get { return settings; }
        }

        public int OwnerId
        {
            get { return ownerId; }
        }

        public bool IsCarrying { get; private set; }
        public int HeldObjectId { get; private set; }
        public CharacterCarryCandidateKind HeldKind { get; private set; }

        /// <summary>
        /// 들기. 빈 슬롯 + 적격 후보(휴대 가능, 1×1 이하, 도달 가능)만 수락한다.
        /// 슬롯이 차 있으면 기존 휴대물을 유지하고 거부한다.
        /// </summary>
        public bool TryPickUp(in CharacterCarryCandidate candidate)
        {
            if (IsCarrying)
            {
                return false;
            }

            if (!candidate.IsReachable || !candidate.IsEligibleForCarry)
            {
                return false;
            }

            IsCarrying = true;
            HeldObjectId = candidate.Id;
            HeldKind = candidate.Kind;
            return true;
        }

        /// <summary>
        /// 아래+행동 안전 내려놓기. 목적지 공간이 비어 있을 때만 배치 요청을
        /// 반환하고 슬롯을 비운다. 막힌 목적지는 겹침 배치 없이 거부하며
        /// 휴대 상태를 유지한다.
        /// </summary>
        public bool TryCreateSafeDrop(
            Vector2 playerFeetPosition,
            out CharacterCarryPlacementRequest request)
        {
            request = default(CharacterCarryPlacementRequest);

            if (!IsCarrying)
            {
                return false;
            }

            Vector2 dropPosition = playerFeetPosition + settings.SafeDropOffset;

            if (!placementQuery.IsPlacementFree(dropPosition))
            {
                return false;
            }

            request = new CharacterCarryPlacementRequest(
                HeldObjectId,
                ownerId,
                dropPosition,
                settings.OwnerCollisionGraceSeconds);
            ReleaseSlot();
            return true;
        }

        /// <summary>
        /// 방향 투척(위/좌/우+행동). 휴대 중일 때만 요청을 반환하고 슬롯을 비운다.
        /// 투척 임팩트 피해는 이 단계에서 적용하지 않는다.
        /// </summary>
        public bool TryCreateThrow(
            CharacterThrowDirection direction,
            out CharacterCarryThrowRequest request)
        {
            request = default(CharacterCarryThrowRequest);

            if (!IsCarrying)
            {
                return false;
            }

            request = new CharacterCarryThrowRequest(
                HeldObjectId,
                ownerId,
                direction,
                DirectionVectorOf(direction),
                settings.ThrowSpeed,
                settings.OwnerCollisionGraceSeconds);
            ReleaseSlot();
            return true;
        }

        private void ReleaseSlot()
        {
            IsCarrying = false;
            HeldObjectId = 0;
            HeldKind = default(CharacterCarryCandidateKind);
        }

        private static Vector2 DirectionVectorOf(CharacterThrowDirection direction)
        {
            switch (direction)
            {
                case CharacterThrowDirection.Up:
                    return Vector2.up;
                case CharacterThrowDirection.Left:
                    return Vector2.left;
                case CharacterThrowDirection.Right:
                default:
                    return Vector2.right;
            }
        }
    }
}
