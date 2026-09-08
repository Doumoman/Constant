#if UNITY_EDITOR
using NUnit.Framework;
using StarNight.Character.Live.Movement;
using UnityEngine;

namespace StarNight.Character.Tests.EditMode.Rmap05
{
    [Category("RMAP05")]
    public sealed class CharacterLiveFallDamageStateTests
    {
        [TestCase(5.999f, 0, false, false)]
        [TestCase(6f, 0, true, false)]
        [TestCase(9.999f, 0, true, false)]
        [TestCase(10f, 1, true, false)]
        [TestCase(14.999f, 1, true, false)]
        [TestCase(15f, 2, true, false)]
        [TestCase(20f, 3, true, false)]
        [TestCase(25f, 4, true, false)]
        [TestCase(29.999f, 4, true, false)]
        [TestCase(30f, 0, false, true)]
        public void P17_FallTableUsesContinuousBoundariesOwnedByMovementSettings(
            float distance,
            int damage,
            bool stun,
            bool fatal)
        {
            var settings = new CharacterLiveMovementSettings();
            CharacterLiveFallLandingResult result = settings.EvaluateFallLanding(distance);

            Assert.AreEqual(damage, result.Damage);
            Assert.AreEqual(stun, result.AppliesStun);
            Assert.AreEqual(fatal, result.IsFatal);
            Assert.AreEqual(5, settings.FallMaxHealth);
            Assert.AreEqual(0.5f, settings.FallStunDuration, 0.0001f);
        }

        [Test]
        public void ExistingHealthAndPlayerStateContractsProvideFiveHealthStunAndDeath()
        {
            var root = new GameObject("RMAP05_FallState", typeof(CharacterLiveFallDamageState));
            var settings = new CharacterLiveMovementSettings();
            CharacterLiveFallDamageState state = root.GetComponent<CharacterLiveFallDamageState>();
            state.ResetForSpawn(501, settings);

            state.ApplyLanding(9f, false, 1f, settings);
            Assert.AreEqual(5, state.MaxHealth);
            Assert.AreEqual(5, state.CurrentHealth);
            Assert.IsTrue(state.IsStunned);
            Assert.IsFalse(state.CanAcceptInput);
            state.Tick(1.499f);
            Assert.IsTrue(state.IsStunned);
            state.Tick(1.5f);
            Assert.IsFalse(state.IsStunned);

            state.ApplyLanding(30f, false, 2f, settings);
            Assert.AreEqual(0, state.CurrentHealth);
            Assert.IsTrue(state.IsDead);
            Assert.IsFalse(state.CanAcceptInput);
            Object.DestroyImmediate(root);
        }
    }
}
#endif
