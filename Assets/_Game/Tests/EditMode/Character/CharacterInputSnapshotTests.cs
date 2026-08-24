using System;
using NUnit.Framework;
using StarNight.Character.Input;

namespace StarNight.Character.Tests
{
    public sealed class CharacterInputSnapshotTests
    {
        [Test]
        public void CharacterActionId_DoesNotContainBasicAttack()
        {
            var names = Enum.GetNames(typeof(CharacterActionId));

            Assert.That(names, Does.Not.Contain("Attack"));
            Assert.That(names, Does.Not.Contain("BasicAttack"));
            Assert.That(names, Does.Not.Contain("Melee"));
            Assert.That(names, Does.Not.Contain("Shoot"));

            Assert.That(names, Does.Contain("Jump"));
            Assert.That(names, Does.Contain("Action"));
            Assert.That(names, Does.Contain("SafeDrop"));
            Assert.That(names, Does.Contain("Bomb"));
            Assert.That(names, Does.Contain("Rope"));
            Assert.That(names.Length, Is.EqualTo(5));
        }

        [Test]
        public void Snapshot_ReportsSafeDropWhenDownAndActionPressed()
        {
            var snapshot = CreateSnapshot(downHeld: true, actionPressed: true);

            Assert.That(snapshot.SafeDropPressedThisFrame, Is.True);
            Assert.That(snapshot.IsPressedThisFrame(CharacterActionId.SafeDrop), Is.True);
        }

        [Test]
        public void Snapshot_PrioritizesSafeDropOverPlainAction()
        {
            var combined = CreateSnapshot(downHeld: true, actionPressed: true);

            Assert.That(combined.SafeDropPressedThisFrame, Is.True);
            Assert.That(combined.PlainActionPressedThisFrame, Is.False);
            Assert.That(combined.IsPressedThisFrame(CharacterActionId.Action), Is.False);

            var plain = CreateSnapshot(downHeld: false, actionPressed: true);

            Assert.That(plain.SafeDropPressedThisFrame, Is.False);
            Assert.That(plain.PlainActionPressedThisFrame, Is.True);
            Assert.That(plain.IsPressedThisFrame(CharacterActionId.Action), Is.True);
        }

        [Test]
        public void Snapshot_KeepsJumpBombAndRopeAsSeparateActions()
        {
            var jumpOnly = CreateSnapshot(jumpPressed: true);

            Assert.That(jumpOnly.IsPressedThisFrame(CharacterActionId.Jump), Is.True);
            Assert.That(jumpOnly.IsPressedThisFrame(CharacterActionId.Bomb), Is.False);
            Assert.That(jumpOnly.IsPressedThisFrame(CharacterActionId.Rope), Is.False);
            Assert.That(jumpOnly.IsPressedThisFrame(CharacterActionId.Action), Is.False);

            var bombOnly = CreateSnapshot(bombPressed: true);

            Assert.That(bombOnly.IsPressedThisFrame(CharacterActionId.Bomb), Is.True);
            Assert.That(bombOnly.IsPressedThisFrame(CharacterActionId.Jump), Is.False);
            Assert.That(bombOnly.IsPressedThisFrame(CharacterActionId.Rope), Is.False);

            var ropeOnly = CreateSnapshot(ropePressed: true);

            Assert.That(ropeOnly.IsPressedThisFrame(CharacterActionId.Rope), Is.True);
            Assert.That(ropeOnly.IsPressedThisFrame(CharacterActionId.Jump), Is.False);
            Assert.That(ropeOnly.IsPressedThisFrame(CharacterActionId.Bomb), Is.False);
        }

        private static CharacterInputSnapshot CreateSnapshot(
            float horizontal = 0f,
            bool downHeld = false,
            bool jumpPressed = false,
            bool actionPressed = false,
            bool bombPressed = false,
            bool ropePressed = false,
            long tick = 0L)
        {
            return new CharacterInputSnapshot(
                horizontal,
                downHeld,
                Button(jumpPressed, tick),
                Button(actionPressed, tick),
                Button(bombPressed, tick),
                Button(ropePressed, tick));
        }

        private static CharacterButtonSnapshot Button(bool pressed, long tick)
        {
            return pressed
                ? CharacterButtonSnapshot.Pressed(tick)
                : CharacterButtonSnapshot.Idle(tick);
        }
    }
}
