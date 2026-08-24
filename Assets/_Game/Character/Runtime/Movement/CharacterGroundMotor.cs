using UnityEngine;
using StarNight.Character.State;

namespace StarNight.Character.Movement
{
    /// <summary>
    /// 지상 수평 이동 모터. 걷기/달리기 목표 속도로 가속하고 입력이 없으면 감속한다.
    /// MoveTowards 기반이라 목표 속도를 overshoot하지 않는다.
    /// vertical velocity는 변경하지 않으며 grounded가 아니면 수평 지상 가속을
    /// 적용하지 않는다(공중 제어·점프·중력·착지는 이후 단계 소관).
    /// </summary>
    public sealed class CharacterGroundMotor
    {
        private readonly CharacterGroundMotorSettings settings;

        public CharacterGroundMotor(CharacterGroundMotorSettings settings)
        {
            this.settings = settings;
        }

        public CharacterGroundMotorSettings Settings
        {
            get { return settings; }
        }

        public CharacterGroundMotorState Step(
            in CharacterGroundMotorState current,
            float horizontalInput,
            bool run,
            float deltaTime)
        {
            float clamped = Mathf.Clamp(horizontalInput, -1f, 1f);
            CharacterFacingDirection facing = current.Facing;

            // facing은 수평 입력이 0이 아닐 때만 갱신하고 0이면 기존 값을 유지한다.
            if (clamped > 0f)
            {
                facing = CharacterFacingDirection.Right;
            }
            else if (clamped < 0f)
            {
                facing = CharacterFacingDirection.Left;
            }

            Vector2 velocity = current.Velocity;

            if (current.IsGrounded)
            {
                float maxSpeed = run ? settings.RunSpeed : settings.WalkSpeed;
                float targetSpeed = clamped * maxSpeed;
                float rate = Mathf.Abs(clamped) > 0f
                    ? settings.GroundAcceleration
                    : settings.GroundDeceleration;

                velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, rate * deltaTime);
            }

            // vertical velocity는 보존한다. 공중에서는 수평 지상 가속을 적용하지 않는다.
            return new CharacterGroundMotorState(velocity, facing, current.Locomotion);
        }
    }
}
