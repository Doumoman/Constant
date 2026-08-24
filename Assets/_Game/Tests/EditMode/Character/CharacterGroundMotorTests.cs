using NUnit.Framework;
using StarNight.Character.Movement;
using StarNight.Character.State;
using UnityEngine;

namespace StarNight.Character.Tests
{
    public sealed class CharacterGroundMotorTests
    {
        private const float DeltaTime = 1f / 60f;

        private static CharacterGroundMotor CreateMotor()
        {
            return new CharacterGroundMotor(CharacterGroundMotorSettings.Default);
        }

        [Test]
        public void GroundMotor_AcceleratesTowardWalkSpeed()
        {
            var motor = CreateMotor();
            var state = CharacterGroundMotorState.GroundedIdle;
            var walkSpeed = motor.Settings.WalkSpeed;
            var previousX = state.Velocity.x;

            for (var step = 0; step < 240; step++)
            {
                state = motor.Step(in state, 1f, false, DeltaTime);

                Assert.That(state.Velocity.x, Is.GreaterThanOrEqualTo(previousX));
                Assert.That(state.Velocity.x, Is.LessThanOrEqualTo(walkSpeed));
                previousX = state.Velocity.x;
            }

            Assert.That(state.Velocity.x, Is.EqualTo(walkSpeed));
            Assert.That(state.Facing, Is.EqualTo(CharacterFacingDirection.Right));
        }

        [Test]
        public void GroundMotor_AcceleratesTowardRunSpeed()
        {
            var motor = CreateMotor();
            var state = CharacterGroundMotorState.GroundedIdle;
            var runSpeed = motor.Settings.RunSpeed;

            for (var step = 0; step < 240; step++)
            {
                state = motor.Step(in state, -1f, true, DeltaTime);

                Assert.That(Mathf.Abs(state.Velocity.x), Is.LessThanOrEqualTo(runSpeed));
            }

            Assert.That(state.Velocity.x, Is.EqualTo(-runSpeed));
            Assert.That(state.Facing, Is.EqualTo(CharacterFacingDirection.Left));
        }

        [Test]
        public void GroundMotor_DeceleratesTowardZeroWithoutInput()
        {
            var motor = CreateMotor();
            var state = CharacterGroundMotorState.GroundedIdle
                .WithVelocity(new Vector2(motor.Settings.RunSpeed, 0f));

            for (var step = 0; step < 240; step++)
            {
                var previousX = state.Velocity.x;
                state = motor.Step(in state, 0f, false, DeltaTime);

                // 감속은 0으로만 접근하고 부호가 뒤집히지 않는다.
                Assert.That(state.Velocity.x, Is.LessThanOrEqualTo(previousX));
                Assert.That(state.Velocity.x, Is.GreaterThanOrEqualTo(0f));
            }

            Assert.That(state.Velocity.x, Is.EqualTo(0f));

            // 입력 0이면 facing은 기존 값을 유지한다.
            Assert.That(state.Facing, Is.EqualTo(CharacterFacingDirection.Right));
        }

        [Test]
        public void GroundMotor_ClampsHorizontalIntentAndPreventsOvershoot()
        {
            var motor = CreateMotor();
            var walkSpeed = motor.Settings.WalkSpeed;

            // 큰 입력 값은 [-1, 1]로 clamp되어 목표 속도가 walkSpeed를 넘지 않는다.
            var state = CharacterGroundMotorState.GroundedIdle;
            state = motor.Step(in state, 5f, false, 10f);

            Assert.That(state.Velocity.x, Is.EqualTo(walkSpeed));

            // 반대 방향 큰 입력도 동일하게 clamp된다.
            state = CharacterGroundMotorState.GroundedIdle;
            state = motor.Step(in state, -7f, false, 10f);

            Assert.That(state.Velocity.x, Is.EqualTo(-walkSpeed));

            // 큰 deltaTime 한 번에 목표 속도를 정확히 넘지 않고 도달한다(overshoot 없음).
            state = CharacterGroundMotorState.GroundedIdle
                .WithVelocity(new Vector2(-motor.Settings.RunSpeed, 0f));
            state = motor.Step(in state, 1f, false, 100f);

            Assert.That(state.Velocity.x, Is.EqualTo(walkSpeed));
        }

        [Test]
        public void GroundMotor_PreservesVerticalVelocity()
        {
            var motor = CreateMotor();
            var state = CharacterGroundMotorState.GroundedIdle
                .WithVelocity(new Vector2(0f, -4.5f));

            state = motor.Step(in state, 1f, true, DeltaTime);

            Assert.That(state.Velocity.y, Is.EqualTo(-4.5f));

            state = motor.Step(in state, 0f, false, DeltaTime);

            Assert.That(state.Velocity.y, Is.EqualTo(-4.5f));
        }

        [Test]
        public void GroundMotor_DoesNotMoveWhenAirborne()
        {
            var motor = CreateMotor();
            var state = CharacterGroundMotorState.GroundedIdle
                .WithLocomotion(CharacterLocomotionState.Airborne)
                .WithVelocity(new Vector2(1.5f, -2f));

            state = motor.Step(in state, 1f, true, DeltaTime);

            // 공중에서는 수평 지상 가속·감속이 적용되지 않는다(공중 제어는 CHAR01_03 소관).
            Assert.That(state.Velocity.x, Is.EqualTo(1.5f));
            Assert.That(state.Velocity.y, Is.EqualTo(-2f));
            Assert.That(state.Locomotion, Is.EqualTo(CharacterLocomotionState.Airborne));
        }
    }
}
