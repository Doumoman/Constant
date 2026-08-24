using UnityEngine;

namespace StarNight.Character.Movement
{
    /// <summary>
    /// 수동 중력 모터. 상승 중에는 rise gravity, 하강 중에는 fall gravity를 적용하고
    /// 낙하 속도를 -maxFallSpeed로 clamp한다.
    /// grounded 상태에서는 중력을 적용하지 않아 불필요한 하강 누적을 만들지 않는다.
    /// </summary>
    public sealed class CharacterGravityMotor
    {
        private readonly CharacterGravitySettings settings;

        public CharacterGravityMotor(CharacterGravitySettings settings)
        {
            this.settings = settings;
        }

        public CharacterGravitySettings Settings
        {
            get { return settings; }
        }

        public Vector2 Step(Vector2 velocity, bool isGrounded, float deltaTime)
        {
            if (isGrounded)
            {
                return velocity;
            }

            float gravity = velocity.y > 0f ? settings.RiseGravity : settings.FallGravity;
            velocity.y -= gravity * deltaTime;

            if (velocity.y < -settings.MaxFallSpeed)
            {
                velocity.y = -settings.MaxFallSpeed;
            }

            return velocity;
        }
    }
}
