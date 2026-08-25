using StarNight.Character.Interaction;
using UnityEngine;

namespace StarNight.Character.Live.Tools
{
    /// <summary>
    /// 라이브 휴대 대상 계약(read-mostly). 소비자는 수락된 pickup/drop/throw
    /// 에서만 AttachTo/ReleaseAt을 호출하고, 거부 경로에서는 어떤 메서드도
    /// 호출하지 않는다. 크기는 셀 단위(1 cell = 1 world unit)이며 적격
    /// 판정(1×1 이하)은 캐릭터 계약이 소유한다.
    /// </summary>
    public interface ICharacterLiveCarryTarget
    {
        int Id { get; }
        CharacterCarryCandidateKind Kind { get; }
        bool IsActive { get; }
        bool IsCarried { get; }
        Vector2 Position { get; }
        float WidthInCells { get; }
        float HeightInCells { get; }
        bool IsCarryable { get; }

        /// <summary>명시적 우선순위 — 낮을수록 먼저 선택된다(캐릭터 질의 규칙).</summary>
        int Priority { get; }

        /// <summary>수락된 pickup에서만 호출 — 대상을 운반자에 부착한다.</summary>
        void AttachTo(int carrierId);

        /// <summary>
        /// 수락된 drop/throw에서만 호출 — 요청 지점에서 요청 초기 속도로
        /// 해제한다(drop은 속도 0, throw는 캐릭터 계약의 방향×속력).
        /// </summary>
        void ReleaseAt(
            Vector2 position,
            Vector2 initialVelocity,
            float ownerCollisionGraceSeconds);
    }
}
