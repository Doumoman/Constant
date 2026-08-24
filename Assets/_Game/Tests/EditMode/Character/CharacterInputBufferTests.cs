using NUnit.Framework;
using StarNight.Character.Input;

namespace StarNight.Character.Tests
{
    public sealed class CharacterInputBufferTests
    {
        private const double DefaultWindow = 0.12d;

        [Test]
        public void PressedAction_SurvivesUntilFirstPhysicsTick()
        {
            var buffer = new CharacterInputBuffer(DefaultWindow);

            // 렌더 프레임(t=0.0)에서 수집된 press가
            buffer.RecordPress(CharacterActionId.Jump, 0.0d);

            // 다음 물리 틱(t=0.016)까지 소실되지 않는다.
            Assert.That(buffer.HasPending(CharacterActionId.Jump, 0.016d), Is.True);
            Assert.That(buffer.TryConsume(CharacterActionId.Jump, 1L, 0.016d), Is.True);
        }

        [Test]
        public void ConsumedAction_IsNotReturnedTwiceInSameTick()
        {
            var buffer = new CharacterInputBuffer(DefaultWindow);

            buffer.RecordPress(CharacterActionId.Jump, 0.0d);

            Assert.That(buffer.TryConsume(CharacterActionId.Jump, 1L, 0.016d), Is.True);
            Assert.That(buffer.TryConsume(CharacterActionId.Jump, 1L, 0.016d), Is.False);

            // 같은 틱 안에서 새 press가 기록돼도 같은 action은 다시 반환되지 않는다.
            buffer.RecordPress(CharacterActionId.Jump, 0.020d);

            Assert.That(buffer.TryConsume(CharacterActionId.Jump, 1L, 0.020d), Is.False);
            Assert.That(buffer.TryConsume(CharacterActionId.Jump, 2L, 0.033d), Is.True);
        }

        [Test]
        public void ExpiredAction_IsNotReturned()
        {
            var buffer = new CharacterInputBuffer(0.10d);

            buffer.RecordPress(CharacterActionId.Bomb, 0.0d);

            Assert.That(buffer.HasPending(CharacterActionId.Bomb, 0.5d), Is.False);
            Assert.That(buffer.TryConsume(CharacterActionId.Bomb, 1L, 0.5d), Is.False);
        }

        [Test]
        public void SafeDropConsumption_DoesNotAlsoReturnPlainAction()
        {
            var buffer = new CharacterInputBuffer(DefaultWindow);
            var downAndAction = new CharacterInputSnapshot(
                0f,
                true,
                CharacterButtonSnapshot.Idle(0L),
                CharacterButtonSnapshot.Pressed(0L),
                CharacterButtonSnapshot.Idle(0L),
                CharacterButtonSnapshot.Idle(0L));

            // Down+Action press는 SafeDrop으로만 기록된다.
            buffer.CaptureFrame(in downAndAction, 0.0d);

            Assert.That(buffer.TryConsume(CharacterActionId.SafeDrop, 1L, 0.016d), Is.True);

            // 같은 물리적 press가 단독 Action으로 중복 소비되지 않는다.
            Assert.That(buffer.TryConsume(CharacterActionId.Action, 1L, 0.016d), Is.False);
            Assert.That(buffer.TryConsume(CharacterActionId.Action, 2L, 0.033d), Is.False);
        }
    }
}
