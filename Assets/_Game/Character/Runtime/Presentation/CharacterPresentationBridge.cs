using System.Collections.Generic;
using StarNight.Character.Equipment;
using StarNight.Character.RunState;
using StarNight.Character.Survival;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Character.Presentation
{
    /// <summary>
    /// 연출 브리지(순수·결정적). 기존 Survival/Equipment/RunState 요청·결과를
    /// 연출 이벤트 요청으로 변환하고, 배치를 우선순위→입력 순서로 정렬·중복
    /// 제거·순번 부여한다. 요청만 만들 뿐 어떤 효과도 재생하지 않는다.
    /// </summary>
    public static class CharacterPresentationBridge
    {
        private const int UnassignedSequence = 0;

        /// <summary>피해 적용 결과 → 피해 이벤트(실제 적용분이 있을 때만).</summary>
        public static bool TryCreateDamageEvent(
            in CharacterDamageApplicationResult result,
            int actorId,
            out CharacterPresentationEventRequest request)
        {
            request = default;

            if (result.AppliedAmount <= 0)
            {
                return false;
            }

            request = new CharacterPresentationEventRequest(
                CharacterPresentationEventType.Damage,
                actorId, true, result.AppliedAmount,
                false, default, UnassignedSequence);
            return true;
        }

        public static CharacterPresentationEventRequest CreateDeathEvent(
            in CharacterDeathRequest deathRequest)
        {
            return new CharacterPresentationEventRequest(
                CharacterPresentationEventType.Death,
                deathRequest.ActorId, false, 0,
                false, default, UnassignedSequence);
        }

        public static CharacterPresentationEventRequest CreateRunFailureEvent(
            in CharacterRunFailureRequest runFailureRequest)
        {
            return new CharacterPresentationEventRequest(
                CharacterPresentationEventType.RunFailure,
                runFailureRequest.ActorId, false, 0,
                false, default, UnassignedSequence);
        }

        public static CharacterPresentationEventRequest CreateBombPlacedEvent(
            in CharacterBombPlacementRequest placementRequest)
        {
            return new CharacterPresentationEventRequest(
                CharacterPresentationEventType.BombPlaced,
                placementRequest.ActorId, false, 0,
                true, placementRequest.TargetCell, UnassignedSequence);
        }

        public static CharacterPresentationEventRequest CreateBombExplodedEvent(
            in CharacterExplosionRequest explosionRequest)
        {
            return new CharacterPresentationEventRequest(
                CharacterPresentationEventType.BombExploded,
                explosionRequest.ExplosionId, true, explosionRequest.DamageAmount,
                true, explosionRequest.CenterCell, UnassignedSequence);
        }

        public static CharacterPresentationEventRequest CreateRopePlacedEvent(
            in CharacterRopePlacementRequest placementRequest)
        {
            return new CharacterPresentationEventRequest(
                CharacterPresentationEventType.RopePlaced,
                placementRequest.ActorId, false, 0,
                true, placementRequest.OriginCell, UnassignedSequence);
        }

        /// <summary>소모 적용 결과 → 인벤토리 변화 이벤트(변화가 있을 때만).</summary>
        public static bool TryCreateInventoryChangedEvent(
            in CharacterRunInventoryApplyResult applyResult,
            out CharacterPresentationEventRequest request)
        {
            request = default;

            if (!applyResult.Changed)
            {
                return false;
            }

            request = new CharacterPresentationEventRequest(
                CharacterPresentationEventType.InventoryChanged,
                applyResult.NewState.ActorId, true, applyResult.AppliedAmount,
                false, default, UnassignedSequence);
            return true;
        }

        /// <summary>
        /// 단일 브리지 호출의 배치 정규화: 우선순위(런 실패→사망→피해→폭발→
        /// 설치→로프→인벤토리) → 입력 순서로 안정 정렬하고, 동등 이벤트는
        /// 한 번만 남기며, 출력 순서대로 SequenceId를 부여한다. 같은 입력이면
        /// 항상 같은 출력이다.
        /// </summary>
        public static void NormalizeBatch(
            IReadOnlyList<CharacterPresentationEventRequest> events,
            List<CharacterPresentationEventRequest> output)
        {
            output.Clear();

            if (events == null || events.Count == 0)
            {
                return;
            }

            // 우선순위 버킷을 오름차순으로 순회하며 입력 순서를 보존한다
            // (List.Sort의 불안정성 회피 — 결정적·안정적).
            for (int priority = 0; priority <= MaxPriority; priority++)
            {
                for (int index = 0; index < events.Count; index++)
                {
                    var candidate = events[index];

                    if (GetPriority(candidate.Type) != priority)
                    {
                        continue;
                    }

                    if (ContainsEquivalent(output, in candidate))
                    {
                        continue;
                    }

                    output.Add(new CharacterPresentationEventRequest(
                        candidate.Type,
                        candidate.ActorOrSourceId,
                        candidate.HasAmount,
                        candidate.Amount,
                        candidate.HasCell,
                        candidate.Cell,
                        output.Count));
                }
            }
        }

        private const int MaxPriority = 6;

        private static int GetPriority(CharacterPresentationEventType type)
        {
            switch (type)
            {
                case CharacterPresentationEventType.RunFailure:
                    return 0;
                case CharacterPresentationEventType.Death:
                    return 1;
                case CharacterPresentationEventType.Damage:
                    return 2;
                case CharacterPresentationEventType.BombExploded:
                    return 3;
                case CharacterPresentationEventType.BombPlaced:
                    return 4;
                case CharacterPresentationEventType.RopePlaced:
                    return 5;
                case CharacterPresentationEventType.InventoryChanged:
                default:
                    return 6;
            }
        }

        private static bool ContainsEquivalent(
            List<CharacterPresentationEventRequest> output,
            in CharacterPresentationEventRequest candidate)
        {
            for (int index = 0; index < output.Count; index++)
            {
                if (AreEquivalent(output[index], in candidate))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>SequenceId를 제외한 내용 동등성.</summary>
        private static bool AreEquivalent(
            CharacterPresentationEventRequest left,
            in CharacterPresentationEventRequest right)
        {
            return left.Type == right.Type
                && left.ActorOrSourceId == right.ActorOrSourceId
                && left.HasAmount == right.HasAmount
                && left.Amount == right.Amount
                && left.HasCell == right.HasCell
                && left.Cell.X == right.Cell.X
                && left.Cell.Y == right.Cell.Y;
        }
    }
}
