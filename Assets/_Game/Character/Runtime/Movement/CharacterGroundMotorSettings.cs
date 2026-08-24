using System;

namespace StarNight.Character.Movement
{
    /// <summary>
    /// 지상 수평 이동 튜닝. 기본값은 레거시 선례(runSpeed 3.75, 가속 30, 감속 40)를
    /// 참고한 기준선이며 PASS 기준은 값 자체가 아니라 이동 문법 결과다.
    /// 검증: runSpeed > walkSpeed > 0, 가속/감속 > 0.
    /// </summary>
    public readonly struct CharacterGroundMotorSettings
    {
        public CharacterGroundMotorSettings(
            float walkSpeed,
            float runSpeed,
            float groundAcceleration,
            float groundDeceleration)
        {
            if (walkSpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(walkSpeed), "walkSpeed는 0보다 커야 한다.");
            }

            if (runSpeed <= walkSpeed)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(runSpeed), "runSpeed는 walkSpeed보다 커야 한다.");
            }

            if (groundAcceleration <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(groundAcceleration), "groundAcceleration은 0보다 커야 한다.");
            }

            if (groundDeceleration <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(groundDeceleration), "groundDeceleration은 0보다 커야 한다.");
            }

            WalkSpeed = walkSpeed;
            RunSpeed = runSpeed;
            GroundAcceleration = groundAcceleration;
            GroundDeceleration = groundDeceleration;
        }

        public float WalkSpeed { get; }
        public float RunSpeed { get; }
        public float GroundAcceleration { get; }
        public float GroundDeceleration { get; }

        public static CharacterGroundMotorSettings Default
        {
            get { return new CharacterGroundMotorSettings(2.2f, 3.75f, 30f, 40f); }
        }
    }
}
