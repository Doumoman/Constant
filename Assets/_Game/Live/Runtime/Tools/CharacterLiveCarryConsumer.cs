using System.Collections.Generic;
using StarNight.Character.Interaction;
using UnityEngine;

namespace StarNight.Character.Live.Tools
{
    /// <summary>
    /// 휴대(pickup)/안전 내려놓기(drop)/방향 투척(throw) 라이브 소비자.
    /// 단일 슬롯 소유권은 CharacterCarryInteraction이 소유하고(1 운반자 =
    /// 1 대상), 본 소비자는 적격 라이브 대상 연결과 해제 적용만 한다.
    /// 후보 선택은 CharacterCarryCandidateQuery, 낙하/투척 요청 값은
    /// 캐릭터 계약 요청을 그대로 사용한다. 거부·중복 요청은 라이브 상태를
    /// 변조하지 않으며, 수락된 요청만 대장에 정확히 한 번 기록된다.
    /// </summary>
    public sealed class CharacterLiveCarryConsumer
    {
        private readonly CharacterCarryInteraction interaction;
        private readonly CharacterLiveToolRequestLedger ledger;
        private readonly float pickupRangeCells;
        private readonly List<CharacterCarryCandidate> candidateBuffer;
        private readonly Dictionary<int, ICharacterLiveCarryTarget> targetById;

        private ICharacterLiveCarryTarget carriedTarget;

        public CharacterLiveCarryConsumer(
            CharacterCarryInteractionSettings settings,
            ICharacterPlacementSpaceQuery placementQuery,
            int ownerId,
            float pickupRangeCells,
            CharacterLiveToolRequestLedger ledger)
        {
            interaction = new CharacterCarryInteraction(
                settings, placementQuery, ownerId);
            this.ledger = ledger;
            this.pickupRangeCells = pickupRangeCells;
            candidateBuffer = new List<CharacterCarryCandidate>();
            targetById = new Dictionary<int, ICharacterLiveCarryTarget>();
        }

        public bool IsCarrying
        {
            get { return interaction.IsCarrying; }
        }

        public int HeldObjectId
        {
            get { return interaction.HeldObjectId; }
        }

        public int AcceptedCount { get; private set; }
        public int RejectedCount { get; private set; }
        public CharacterLiveToolDiagnosticKind LastDiagnostic { get; private set; }
        public CharacterCarryPlacementRequest LastPlacementRequest { get; private set; }
        public CharacterCarryThrowRequest LastThrowRequest { get; private set; }

        /// <summary>
        /// 들기 소비. 활성·미휴대·도달 범위 안 후보를 캐릭터 질의로 정확히
        /// 하나 선택해 부착한다. 부재/부적격/이미 휴대 중/이미 휴대됨은
        /// 결정적으로 거부한다.
        /// </summary>
        public CharacterLiveToolUseResult TryConsumeCarry(
            long requestId,
            Vector2 carrierPosition,
            IReadOnlyList<ICharacterLiveCarryTarget> targets)
        {
            if (ledger.IsConsumed(CharacterLiveToolChannel.Carry, requestId))
            {
                return Reject(CharacterLiveToolDiagnosticKind.DuplicateRequest);
            }

            if (interaction.IsCarrying)
            {
                return Reject(CharacterLiveToolDiagnosticKind.AlreadyCarrying);
            }

            candidateBuffer.Clear();
            targetById.Clear();
            bool sawCarriedInRange = false;
            bool sawIneligibleInRange = false;
            float rangeSquared = pickupRangeCells * pickupRangeCells;

            for (int index = 0; targets != null && index < targets.Count; index++)
            {
                ICharacterLiveCarryTarget target = targets[index];

                if (target == null || !target.IsActive)
                {
                    continue;
                }

                bool reachable =
                    (target.Position - carrierPosition).sqrMagnitude <= rangeSquared;

                if (!reachable)
                {
                    continue;
                }

                if (target.IsCarried)
                {
                    sawCarriedInRange = true;
                    continue;
                }

                var candidate = new CharacterCarryCandidate(
                    target.Id,
                    target.Kind,
                    target.Position,
                    target.WidthInCells,
                    target.HeightInCells,
                    target.IsCarryable,
                    true,
                    target.Priority);

                if (!candidate.IsEligibleForCarry)
                {
                    sawIneligibleInRange = true;
                    continue;
                }

                candidateBuffer.Add(candidate);
                targetById[target.Id] = target;
            }

            CharacterCarryCandidate selected;
            if (!CharacterCarryCandidateQuery.TrySelectCandidate(
                carrierPosition, candidateBuffer, out selected))
            {
                if (sawCarriedInRange)
                {
                    return Reject(
                        CharacterLiveToolDiagnosticKind.TargetAlreadyCarried);
                }

                if (sawIneligibleInRange)
                {
                    return Reject(
                        CharacterLiveToolDiagnosticKind.InvalidCarryTarget);
                }

                return Reject(CharacterLiveToolDiagnosticKind.NoCarryTarget);
            }

            if (!interaction.TryPickUp(in selected))
            {
                return Reject(CharacterLiveToolDiagnosticKind.InvalidCarryTarget);
            }

            carriedTarget = targetById[selected.Id];
            carriedTarget.AttachTo(interaction.OwnerId);
            ledger.TryMarkConsumed(CharacterLiveToolChannel.Carry, requestId);
            return Accept();
        }

        /// <summary>
        /// 안전 내려놓기 소비. 목적지 공간이 빈 경우에만 캐릭터 배치 요청
        /// 지점에서 해제한다. 막힌 목적지는 슬롯·대상 유지(BlockedDrop).
        /// </summary>
        public CharacterLiveToolUseResult TryConsumeDrop(
            long requestId,
            Vector2 playerFeetPosition)
        {
            if (ledger.IsConsumed(CharacterLiveToolChannel.Drop, requestId))
            {
                return Reject(CharacterLiveToolDiagnosticKind.DuplicateRequest);
            }

            if (!interaction.IsCarrying || carriedTarget == null)
            {
                return Reject(CharacterLiveToolDiagnosticKind.NoCarriedTarget);
            }

            CharacterCarryPlacementRequest request;
            if (!interaction.TryCreateSafeDrop(playerFeetPosition, out request))
            {
                return Reject(CharacterLiveToolDiagnosticKind.BlockedDrop);
            }

            carriedTarget.ReleaseAt(
                request.Position,
                Vector2.zero,
                request.OwnerCollisionGraceSeconds);
            carriedTarget = null;
            LastPlacementRequest = request;
            ledger.TryMarkConsumed(CharacterLiveToolChannel.Drop, requestId);
            return Accept();
        }

        /// <summary>
        /// 방향 투척 소비. 캐릭터 투척 요청의 방향 벡터 × 속력을 초기
        /// 속도로 그대로 적용해 해제한다(방향 결정은 호출자가
        /// CharacterThrowDirectionResolver로 수행).
        /// </summary>
        public CharacterLiveToolUseResult TryConsumeThrow(
            long requestId,
            CharacterThrowDirection direction,
            Vector2 carrierPosition)
        {
            if (ledger.IsConsumed(CharacterLiveToolChannel.Throw, requestId))
            {
                return Reject(CharacterLiveToolDiagnosticKind.DuplicateRequest);
            }

            if (!interaction.IsCarrying || carriedTarget == null)
            {
                return Reject(CharacterLiveToolDiagnosticKind.NoCarriedTarget);
            }

            CharacterCarryThrowRequest request;
            if (!interaction.TryCreateThrow(direction, out request))
            {
                return Reject(CharacterLiveToolDiagnosticKind.NoCarriedTarget);
            }

            carriedTarget.ReleaseAt(
                carrierPosition,
                request.DirectionVector * request.Speed,
                request.OwnerCollisionGraceSeconds);
            carriedTarget = null;
            LastThrowRequest = request;
            ledger.TryMarkConsumed(CharacterLiveToolChannel.Throw, requestId);
            return Accept();
        }

        private CharacterLiveToolUseResult Accept()
        {
            AcceptedCount++;
            LastDiagnostic = CharacterLiveToolDiagnosticKind.None;
            return CharacterLiveToolUseResult.Success();
        }

        private CharacterLiveToolUseResult Reject(
            CharacterLiveToolDiagnosticKind diagnostic)
        {
            RejectedCount++;
            LastDiagnostic = diagnostic;
            return CharacterLiveToolUseResult.Rejected(diagnostic);
        }
    }
}
