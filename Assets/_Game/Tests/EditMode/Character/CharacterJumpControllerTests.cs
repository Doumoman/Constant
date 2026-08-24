using NUnit.Framework;
using StarNight.Character.Movement;
using UnityEngine;

namespace StarNight.Character.Tests
{
    public sealed class CharacterJumpControllerTests
    {
        private static CharacterJumpController CreateController()
        {
            return new CharacterJumpController(CharacterJumpSettings.Default);
        }

        [Test]
        public void JumpBuffer_PressBeforeGroundedTriggersOnGroundedTick()
        {
            var controller = CreateController();
            var state = new CharacterJumpState();
            var velocity = new Vector2(0f, -3f);

            // 공중(코요테 없음)에서 press — 시작 불가, press는 보존된다.
            state.NoteJumpPressed(0.0d);

            Assert.That(
                controller.TryStartJump(state, false, 0.0d, ref velocity), Is.False);
            Assert.That(velocity.y, Is.EqualTo(-3f));

            // 버퍼 시간(0.12) 안에 grounded 획득 → 그 틱에 점프가 시작된다.
            state.NoteGrounded(0.05d);

            Assert.That(
                controller.TryStartJump(state, true, 0.05d, ref velocity), Is.True);
            Assert.That(velocity.y, Is.EqualTo(controller.Settings.JumpVelocity));

            // 버퍼 만료 후에는 시작되지 않는다.
            var expired = new CharacterJumpState();
            var expiredVelocity = Vector2.zero;
            expired.NoteJumpPressed(0.0d);
            expired.NoteGrounded(0.5d);

            Assert.That(
                controller.TryStartJump(expired, true, 0.5d, ref expiredVelocity), Is.False);
        }

        [Test]
        public void CoyoteTime_AllowsJumpShortlyAfterLeavingGround()
        {
            var controller = CreateController();
            var state = new CharacterJumpState();
            var velocity = Vector2.zero;

            // 지면 이탈 직후(코요테 0.10 안) press → 공중이어도 점프 허용.
            state.NoteGrounded(0.0d);
            state.NoteJumpPressed(0.05d);

            Assert.That(
                controller.TryStartJump(state, false, 0.05d, ref velocity), Is.True);
            Assert.That(velocity.y, Is.EqualTo(controller.Settings.JumpVelocity));

            // 코요테 창 밖에서는 허용되지 않는다.
            var late = new CharacterJumpState();
            var lateVelocity = Vector2.zero;
            late.NoteGrounded(0.0d);
            late.NoteJumpPressed(0.5d);

            Assert.That(
                controller.TryStartJump(late, false, 0.5d, ref lateVelocity), Is.False);
            Assert.That(lateVelocity.y, Is.EqualTo(0f));
        }

        [Test]
        public void Jump_IsConsumedOnceAndSetsUpwardVelocity()
        {
            var controller = CreateController();
            var state = new CharacterJumpState();
            var velocity = Vector2.zero;

            state.NoteGrounded(0.0d);
            state.NoteJumpPressed(0.0d);

            Assert.That(
                controller.TryStartJump(state, true, 0.0d, ref velocity), Is.True);
            Assert.That(velocity.y, Is.EqualTo(controller.Settings.JumpVelocity));
            Assert.That(state.JumpConsumed, Is.True);

            // 같은 press는 다시 소비되지 않는다.
            velocity = Vector2.zero;

            Assert.That(
                controller.TryStartJump(state, true, 0.0d, ref velocity), Is.False);
            Assert.That(velocity.y, Is.EqualTo(0f));
        }

        [Test]
        public void Jump_DoesNotAllowSecondJumpBeforeGroundedAgain()
        {
            var controller = CreateController();
            var state = new CharacterJumpState();
            var velocity = Vector2.zero;

            state.NoteGrounded(0.0d);
            state.NoteJumpPressed(0.0d);

            Assert.That(
                controller.TryStartJump(state, true, 0.0d, ref velocity), Is.True);

            // 공중에서 재입력 — 코요테 창 안이라도 두 번째 점프는 불가.
            state.NoteJumpPressed(0.05d);

            Assert.That(
                controller.TryStartJump(state, false, 0.05d, ref velocity), Is.False);

            // grounded 재획득 후에는 새 점프가 가능하다.
            state.NoteGrounded(1.0d);
            state.NoteJumpPressed(1.0d);
            velocity = Vector2.zero;

            Assert.That(
                controller.TryStartJump(state, true, 1.0d, ref velocity), Is.True);
            Assert.That(velocity.y, Is.EqualTo(controller.Settings.JumpVelocity));
        }
    }
}
