using UnityEngine;
using StarNight.Character.State;

namespace StarNight.Character.Movement
{
    /// <summary>
    /// 모터 입출력 상태 값 객체. velocity, facing, locomotion을 immutable로 전달한다.
    /// Animator, 사운드, 렌더 프레임 성공 여부에 의존하지 않는다.
    /// </summary>
    public readonly struct CharacterGroundMotorState
    {
        public CharacterGroundMotorState(
            Vector2 velocity,
            CharacterFacingDirection facing,
            CharacterLocomotionState locomotion)
        {
            Velocity = velocity;
            Facing = facing;
            Locomotion = locomotion;
        }

        public Vector2 Velocity { get; }
        public CharacterFacingDirection Facing { get; }
        public CharacterLocomotionState Locomotion { get; }

        public bool IsGrounded
        {
            get { return Locomotion == CharacterLocomotionState.Grounded; }
        }

        public static CharacterGroundMotorState GroundedIdle
        {
            get
            {
                return new CharacterGroundMotorState(
                    Vector2.zero,
                    CharacterFacingDirection.Right,
                    CharacterLocomotionState.Grounded);
            }
        }

        public CharacterGroundMotorState WithLocomotion(CharacterLocomotionState locomotion)
        {
            return new CharacterGroundMotorState(Velocity, Facing, locomotion);
        }

        public CharacterGroundMotorState WithVelocity(Vector2 velocity)
        {
            return new CharacterGroundMotorState(velocity, Facing, Locomotion);
        }
    }
}
