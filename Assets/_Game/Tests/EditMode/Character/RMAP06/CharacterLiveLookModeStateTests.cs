#if UNITY_EDITOR
using NUnit.Framework;
using StarNight.Character.Input;
using StarNight.Character.Live.Cameras;
using UnityEngine;

namespace StarNight.Character.Tests.EditMode.Rmap06
{
    [Category("RMAP06")]
    public sealed class CharacterLiveLookModeStateTests
    {
        [TestCase(1f, false, false, 3f, 0f)]
        [TestCase(-1f, false, false, -3f, 0f)]
        [TestCase(0f, true, false, 0f, 3f)]
        [TestCase(0f, false, true, 0f, -3f)]
        [TestCase(1f, true, false, 2.12132f, 2.12132f)]
        [TestCase(-1f, true, false, -2.12132f, 2.12132f)]
        [TestCase(1f, false, true, 2.12132f, -2.12132f)]
        [TestCase(-1f, false, true, -2.12132f, -2.12132f)]
        public void C03_EightDirectionsWaitOneSecondAndNormalizeToThreeTiles(
            float horizontal,
            bool up,
            bool down,
            float expectedX,
            float expectedY)
        {
            var root = new GameObject("RMAP06_LookState");
            CharacterLiveLookModeState state = root.AddComponent<CharacterLiveLookModeState>();
            CharacterInputSnapshot input = Snapshot(horizontal, up, down, lookHeld: true);

            state.Step(in input, true, 0.02f);
            for (int index = 0; index < 49; index++)
            {
                state.Step(in input, true, 0.02f);
            }

            Assert.IsTrue(state.IsWaiting);
            Assert.IsFalse(state.IsLooking, "A sub-one-second hold must not move the camera.");
            state.Step(in input, true, 0.02f);
            Assert.IsTrue(state.IsLooking);
            Assert.AreEqual(CharacterLiveLookModeState.OffsetTiles,
                state.TargetOffset.magnitude, 0.0001f);
            Assert.That(state.TargetOffset.x, Is.EqualTo(expectedX).Within(0.0002f));
            Assert.That(state.TargetOffset.y, Is.EqualTo(expectedY).Within(0.0002f));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void C04C05_IneligibleStateAndDirectionChangeCancelWithoutRetainingInputLock()
        {
            var root = new GameObject("RMAP06_LookEligibility");
            CharacterLiveLookModeState state = root.AddComponent<CharacterLiveLookModeState>();
            CharacterInputSnapshot east = Snapshot(1f, false, false, lookHeld: true);
            for (int index = 0; index <= 50; index++)
            {
                state.Step(in east, true, 0.02f);
            }

            Assert.IsTrue(state.IsLooking);
            state.Step(in east, false, 0.02f);
            Assert.IsFalse(state.IsLooking);
            Assert.IsFalse(state.IsInputLocked);
            Assert.AreEqual(Vector2.zero, state.TargetOffset);

            state.Step(in east, true, 0.02f);
            CharacterInputSnapshot north = Snapshot(0f, true, false, lookHeld: true);
            state.Step(in north, true, 0.02f);
            Assert.IsTrue(state.IsWaiting);
            Assert.AreEqual(0f, state.HeldSeconds, 0.0001f,
                "A changed direction starts its own one-second hold.");
            Object.DestroyImmediate(root);
        }

        [Test]
        public void C03_TimingConstantsAreTheSpecifiedEnterAndReturnDurations()
        {
            Assert.AreEqual(1f, CharacterLiveLookModeState.HoldSeconds, 0.0001f);
            Assert.AreEqual(3f, CharacterLiveLookModeState.OffsetTiles, 0.0001f);
            Assert.AreEqual(0.24f, CharacterLiveLookModeState.EnterSeconds, 0.0001f);
            Assert.AreEqual(0.18f, CharacterLiveLookModeState.ReturnSeconds, 0.0001f);
        }

        private static CharacterInputSnapshot Snapshot(
            float horizontal,
            bool up,
            bool down,
            bool lookHeld)
        {
            return new CharacterInputSnapshot(horizontal, up, down, false, lookHeld,
                default, default, default, default);
        }
    }
}
#endif
