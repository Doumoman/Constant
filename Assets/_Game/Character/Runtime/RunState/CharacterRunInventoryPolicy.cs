using StarNight.Character.Equipment;
using UnityEngine;

namespace StarNight.Character.RunState
{
    /// <summary>
    /// 소모 요청 → 런 인벤토리 적용(순수). CHAR05_01/CHAR05_02의 기존 소모
    /// 요청 타입을 그대로 입력으로 소비한다(RunState측 어댑터 — 기존 파일
    /// 무수정). 0 미만으로 내려가지 않고, 대상 불일치·비양수 요청은 무시한다.
    /// </summary>
    public static class CharacterRunInventoryPolicy
    {
        public static CharacterRunInventoryApplyResult ApplyBombSpend(
            in CharacterRunInventoryState state,
            in CharacterBombSpendRequest request)
        {
            int applied = ResolveApplied(
                state.ActorId, request.ActorId, request.Amount, state.BombCount);

            if (applied <= 0)
            {
                return new CharacterRunInventoryApplyResult(state, 0);
            }

            var newState = new CharacterRunInventoryState(
                state.ActorId, state.BombCount - applied, state.RopeCount);
            return new CharacterRunInventoryApplyResult(newState, applied);
        }

        public static CharacterRunInventoryApplyResult ApplyRopeSpend(
            in CharacterRunInventoryState state,
            in CharacterRopeSpendRequest request)
        {
            int applied = ResolveApplied(
                state.ActorId, request.ActorId, request.Amount, state.RopeCount);

            if (applied <= 0)
            {
                return new CharacterRunInventoryApplyResult(state, 0);
            }

            var newState = new CharacterRunInventoryState(
                state.ActorId, state.BombCount, state.RopeCount - applied);
            return new CharacterRunInventoryApplyResult(newState, applied);
        }

        private static int ResolveApplied(
            int stateActorId,
            int requestActorId,
            int requestedAmount,
            int availableCount)
        {
            if (requestActorId != stateActorId || requestedAmount <= 0)
            {
                return 0;
            }

            return Mathf.Min(requestedAmount, availableCount);
        }
    }
}
