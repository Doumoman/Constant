using UnityEngine;

namespace StarNight.Character.Interaction
{
    /// <summary>
    /// 휴대 후보 값 객체(immutable). 안정 id, 셀 단위 크기(1 cell = 1 world unit),
    /// 종류, 휴대 가능·도달 가능 여부, 명시적 우선순위를 담는다.
    /// 잠금 규칙: 휴대 대상은 1×1 이하만 적격이다.
    /// </summary>
    public readonly struct CharacterCarryCandidate
    {
        private const float MaxCarryableSizeInCells = 1f;
        private const float SizeEpsilon = 0.0001f;

        public CharacterCarryCandidate(
            int id,
            CharacterCarryCandidateKind kind,
            Vector2 position,
            float widthInCells,
            float heightInCells,
            bool isCarryable,
            bool isReachable,
            int priority)
        {
            Id = id;
            Kind = kind;
            Position = position;
            WidthInCells = widthInCells;
            HeightInCells = heightInCells;
            IsCarryable = isCarryable;
            IsReachable = isReachable;
            Priority = priority;
        }

        /// <summary>안정 식별자(핸들).</summary>
        public int Id { get; }

        public CharacterCarryCandidateKind Kind { get; }
        public Vector2 Position { get; }
        public float WidthInCells { get; }
        public float HeightInCells { get; }
        public bool IsCarryable { get; }
        public bool IsReachable { get; }

        /// <summary>명시적 우선순위 — 낮을수록 먼저 선택된다.</summary>
        public int Priority { get; }

        /// <summary>휴대 적격: 휴대 가능하고 1×1 이하.</summary>
        public bool IsEligibleForCarry
        {
            get
            {
                return IsCarryable
                    && WidthInCells <= MaxCarryableSizeInCells + SizeEpsilon
                    && HeightInCells <= MaxCarryableSizeInCells + SizeEpsilon;
            }
        }
    }
}
