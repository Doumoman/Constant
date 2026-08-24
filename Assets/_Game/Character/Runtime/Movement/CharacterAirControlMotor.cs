using UnityEngine;

namespace StarNight.Character.Movement
{
    /// <summary>
    /// 공중 수평 제어 모터. airborne 상태에서만 horizontal input을 [-1, 1]로 clamp해
    /// 수평 속도를 목표 속도 방향으로 이동시킨다(MoveTowards — overshoot 없음).
    /// 지상 이동은 CharacterGroundMotor 소관이며 vertical velocity는 변경하지 않는다.
    /// wall jump, dash, double jump 관련 side effect가 없다.
    /// </summary>
    public sealed class CharacterAirControlMotor
    {
        private readonly CharacterAirControlSettings settings;

        public CharacterAirControlMotor(CharacterAirControlSettings settings)
        {
            this.settings = settings;
        }

        public CharacterAirControlSettings Settings
        {
            get { return settings; }
        }

        public Vector2 Step(
            Vector2 velocity,
            bool isGrounded,
            float horizontalInput,
            float deltaTime)
        {
            if (isGrounded)
            {
                return velocity;
            }

            float clamped = Mathf.Clamp(horizontalInput, -1f, 1f);
            float targetSpeed = clamped * settings.MaxAirSpeed;

            velocity.x = Mathf.MoveTowards(
                velocity.x,
                targetSpeed,
                settings.AirAcceleration * deltaTime);

            return velocity;
        }
    }
}
