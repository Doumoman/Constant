using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Character.Interaction
{
    /// <summary>
    /// 휴대 후보 선택 질의. 결정적 우선순위로 정확히 하나만 선택한다:
    /// 1) 도달 가능(IsReachable)하고 적격(IsEligibleForCarry: 휴대 가능 + 1×1 이하)인 후보만
    /// 2) 명시적 Priority 오름차순(낮을수록 우선)
    /// 3) 플레이어와의 거리 오름차순(제곱 거리)
    /// 4) 안정 Id 오름차순(타이브레이크)
    /// </summary>
    public static class CharacterCarryCandidateQuery
    {
        public static bool TrySelectCandidate(
            Vector2 playerPosition,
            IReadOnlyList<CharacterCarryCandidate> candidates,
            out CharacterCarryCandidate selected)
        {
            selected = default(CharacterCarryCandidate);
            bool found = false;

            if (candidates == null)
            {
                return false;
            }

            for (int index = 0; index < candidates.Count; index++)
            {
                CharacterCarryCandidate candidate = candidates[index];

                if (!candidate.IsReachable || !candidate.IsEligibleForCarry)
                {
                    continue;
                }

                if (!found || IsBetter(candidate, selected, playerPosition))
                {
                    selected = candidate;
                    found = true;
                }
            }

            return found;
        }

        private static bool IsBetter(
            in CharacterCarryCandidate candidate,
            in CharacterCarryCandidate current,
            Vector2 playerPosition)
        {
            if (candidate.Priority != current.Priority)
            {
                return candidate.Priority < current.Priority;
            }

            float candidateDistance = (candidate.Position - playerPosition).sqrMagnitude;
            float currentDistance = (current.Position - playerPosition).sqrMagnitude;

            if (candidateDistance != currentDistance)
            {
                return candidateDistance < currentDistance;
            }

            return candidate.Id < current.Id;
        }
    }
}
