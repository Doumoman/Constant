using NUnit.Framework;
using StarNight.Character.Movement;
using UnityEngine;

namespace StarNight.Character.Tests
{
    public sealed class CharacterAirAndGravityMotorTests
    {
        private const float DeltaTime = 1f / 60f;

        [Test]
        public void AirControl_AcceleratesHorizontallyOnlyWhileAirborne()
        {
            var motor = new CharacterAirControlMotor(CharacterAirControlSettings.Default);
            var velocity = new Vector2(0f, -2f);

            // 지상에서는 공중 제어를 적용하지 않는다(지상은 ground motor 소관).
            var groundedResult = motor.Step(velocity, true, 1f, DeltaTime);

            Assert.That(groundedResult.x, Is.EqualTo(0f));
            Assert.That(groundedResult.y, Is.EqualTo(-2f));

            // 공중에서는 입력 방향으로 수평 가속한다.
            var airborneResult = motor.Step(velocity, false, 1f, DeltaTime);

            Assert.That(airborneResult.x, Is.GreaterThan(0f));
            Assert.That(airborneResult.x,
                Is.LessThanOrEqualTo(motor.Settings.MaxAirSpeed));
        }

        [Test]
        public void AirControl_ClampsHorizontalIntentAndPreservesVerticalVelocity()
        {
            var motor = new CharacterAirControlMotor(CharacterAirControlSettings.Default);
            var maxAirSpeed = motor.Settings.MaxAirSpeed;

            // 큰 입력은 [-1, 1]로 clamp되어 큰 deltaTime에도 maxAirSpeed에 정확히 수렴한다.
            var result = motor.Step(new Vector2(0f, 5f), false, 9f, 100f);

            Assert.That(result.x, Is.EqualTo(maxAirSpeed));
            Assert.That(result.y, Is.EqualTo(5f));

            var negative = motor.Step(new Vector2(0f, -7f), false, -9f, 100f);

            Assert.That(negative.x, Is.EqualTo(-maxAirSpeed));
            Assert.That(negative.y, Is.EqualTo(-7f));
        }

        [Test]
        public void Gravity_UsesRiseGravityWhenAscendingAndFallGravityWhenDescending()
        {
            var motor = new CharacterGravityMotor(CharacterGravitySettings.Default);
            var settings = motor.Settings;
            const float dt = 0.1f;

            // 상승 중: rise gravity 적용.
            var rising = motor.Step(new Vector2(0f, 5f), false, dt);

            Assert.That(rising.y, Is.EqualTo(5f - settings.RiseGravity * dt).Within(1e-4f));

            // 하강 중: fall gravity 적용.
            var falling = motor.Step(new Vector2(0f, -1f), false, dt);

            Assert.That(falling.y, Is.EqualTo(-1f - settings.FallGravity * dt).Within(1e-4f));

            // grounded면 중력으로 하강을 누적하지 않는다.
            var grounded = motor.Step(new Vector2(0f, 0f), true, dt);

            Assert.That(grounded.y, Is.EqualTo(0f));
        }

        [Test]
        public void Gravity_ClampsToMaxFallSpeed()
        {
            var motor = new CharacterGravityMotor(CharacterGravitySettings.Default);
            var maxFall = motor.Settings.MaxFallSpeed;
            var velocity = new Vector2(0f, 0f);

            for (var step = 0; step < 300; step++)
            {
                velocity = motor.Step(velocity, false, DeltaTime);

                Assert.That(velocity.y, Is.GreaterThanOrEqualTo(-maxFall));
            }

            Assert.That(velocity.y, Is.EqualTo(-maxFall));
        }

        [Test]
        public void VariableJumpRelease_ReducesUpwardVelocityOnlyWhileAscending()
        {
            var controller = new CharacterJumpController(CharacterJumpSettings.Default);
            var cut = controller.Settings.ReleaseCutMultiplier;
            var state = new CharacterJumpState();
            var velocity = Vector2.zero;

            state.NoteGrounded(0.0d);
            state.NoteJumpPressed(0.0d);
            controller.TryStartJump(state, true, 0.0d, ref velocity);

            var jumpVelocity = controller.Settings.JumpVelocity;

            // 상승 중 release → cut 계수 1회 적용.
            var released = controller.ApplyJumpRelease(state, false, velocity);

            Assert.That(released.y, Is.EqualTo(jumpVelocity * cut).Within(1e-4f));

            // 같은 점프에서 두 번 적용되지 않는다.
            var again = controller.ApplyJumpRelease(state, false, released);

            Assert.That(again.y, Is.EqualTo(released.y));

            // 하강 중에는 적용되지 않는다.
            var landedState = new CharacterJumpState();
            landedState.NoteGrounded(0.0d);
            landedState.NoteJumpPressed(0.0d);
            var fallVelocity = Vector2.zero;
            controller.TryStartJump(landedState, true, 0.0d, ref fallVelocity);
            fallVelocity.y = -2f;

            var descending = controller.ApplyJumpRelease(landedState, false, fallVelocity);

            Assert.That(descending.y, Is.EqualTo(-2f));
        }
    }
}
