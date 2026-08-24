using NUnit.Framework;
using StarNight.Character.State;

namespace StarNight.Character.Tests
{
    public sealed class CharacterPlayerStateTests
    {
        [Test]
        public void InputLocks_AreReasonSetAndClearIndependently()
        {
            var state = new CharacterPlayerState();

            Assert.That(state.CanAcceptInput, Is.True);

            state.Locks.Add("Dialogue");
            state.Locks.Add("Cutscene");

            Assert.That(state.Locks.Count, Is.EqualTo(2));
            Assert.That(state.CanAcceptInput, Is.False);

            // 사유 하나를 제거해도 다른 사유가 남아 있으면 잠금이 유지된다.
            state.Locks.Remove("Dialogue");

            Assert.That(state.Locks.IsLocked, Is.True);
            Assert.That(state.CanAcceptInput, Is.False);

            state.Locks.Remove("Cutscene");

            Assert.That(state.Locks.IsLocked, Is.False);
            Assert.That(state.CanAcceptInput, Is.True);
        }

        [Test]
        public void CameraRoomTransition_DoesNotCreateInputLock()
        {
            var state = new CharacterPlayerState();

            state.SetCameraRoomTransitionActive(true);

            Assert.That(state.CameraRoomTransitionActive, Is.True);
            Assert.That(state.Locks.Count, Is.EqualTo(0));
            Assert.That(state.CanAcceptInput, Is.True);

            state.SetCameraRoomTransitionActive(false);

            Assert.That(state.Locks.Count, Is.EqualTo(0));
            Assert.That(state.CanAcceptInput, Is.True);
        }

        [Test]
        public void StateSnapshot_TracksFacingLocomotionCarryStunAndDeath()
        {
            var state = new CharacterPlayerState();

            state.UpdateFacing(-1f);

            Assert.That(state.Facing, Is.EqualTo(CharacterFacingDirection.Left));

            // 수평 입력 0이면 기존 facing을 유지한다.
            state.UpdateFacing(0f);

            Assert.That(state.Facing, Is.EqualTo(CharacterFacingDirection.Left));

            state.SetLocomotion(CharacterLocomotionState.Airborne);
            state.SetCarrying(true);
            state.SetStunned(true);
            state.SetDead(true);

            var snapshot = state.CreateSnapshot(7L);

            Assert.That(snapshot.Facing, Is.EqualTo(CharacterFacingDirection.Left));
            Assert.That(snapshot.Locomotion, Is.EqualTo(CharacterLocomotionState.Airborne));
            Assert.That(snapshot.IsCarrying, Is.True);
            Assert.That(snapshot.IsStunned, Is.True);
            Assert.That(snapshot.IsDead, Is.True);
            Assert.That(snapshot.CanAcceptInput, Is.False);
            Assert.That(snapshot.LockReasonCount, Is.EqualTo(0));
            Assert.That(snapshot.Tick, Is.EqualTo(7L));
        }

        [Test]
        public void DeadOrStunnedState_CannotAcceptInput()
        {
            var stunned = new CharacterPlayerState();
            stunned.SetStunned(true);

            Assert.That(stunned.CanAcceptInput, Is.False);

            stunned.SetStunned(false);

            Assert.That(stunned.CanAcceptInput, Is.True);

            var dead = new CharacterPlayerState();
            dead.SetDead(true);

            Assert.That(dead.CanAcceptInput, Is.False);
        }
    }
}
