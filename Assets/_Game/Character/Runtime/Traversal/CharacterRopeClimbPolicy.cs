using StarNight.Character.Equipment;
using UnityEngine;

namespace StarNight.Character.Traversal
{
    /// <summary>
    /// 로프 등반 판정(순수·결정적). 로프 겹침 + 등반 의도일 때만 모터 요청을
    /// 만들고, 목표 Y를 로프 상·하한으로 clamp한다. 축 0이면 속도 0으로
    /// 제자리 유지 요청을 만든다(고정 규칙의 hold 허용).
    /// </summary>
    public static class CharacterRopeClimbPolicy
    {
        public static bool TryCreateClimbRequest(
            in CharacterRopeClimbInput input,
            in CharacterRopeSettings settings,
            float deltaSeconds,
            out CharacterRopeClimbMotorRequest request)
        {
            request = default;

            if (!input.IsOverlappingRope || !input.HasClimbIntent)
            {
                return false;
            }

            float axis = Mathf.Clamp(input.VerticalAxis, -1f, 1f);
            float velocity = axis * settings.ClimbSpeedUnitsPerSecond;
            float delta = Mathf.Max(0f, deltaSeconds);

            float unclampedTarget = input.CurrentWorldY + velocity * delta;
            float target = Mathf.Clamp(
                unclampedTarget,
                input.RopeExtent.BottomWorldY,
                input.RopeExtent.TopWorldY);

            request = new CharacterRopeClimbMotorRequest(
                input.ActorId, velocity, target);
            return true;
        }
    }
}
