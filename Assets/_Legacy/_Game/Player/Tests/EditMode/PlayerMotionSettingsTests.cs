#if LEGACY_DISABLED
using NUnit.Framework;
using StarNight.Core.Player;
using StarNight.Player.Motor;
using StarNight.Player.Safety;
using UnityEngine;

namespace StarNight.Player.Tests
{
    public sealed class PlayerMotionSettingsTests
    {
        [Test]
        public void DefaultsMatchGCore01Contract()
        {
            PlayerMotionSettings settings = new PlayerMotionSettings();

            Assert.That(settings.maximumMoveSpeed, Is.EqualTo(6f));
            Assert.That(settings.groundAcceleration, Is.EqualTo(48f));
            Assert.That(settings.groundDeceleration, Is.EqualTo(60f));
            Assert.That(settings.airAcceleration, Is.EqualTo(28f));
            Assert.That(settings.gravity, Is.EqualTo(28f));
            Assert.That(settings.baseJumpVelocity, Is.EqualTo(8.75f));
            Assert.That(settings.coyoteTime, Is.EqualTo(0.10f));
            Assert.That(settings.jumpBufferTime, Is.EqualTo(0.10f));
            Assert.That(settings.maximumFallSpeed, Is.EqualTo(12f));
            Assert.That(settings.jumpCutMultiplier, Is.EqualTo(0.55f));
            Assert.That(settings.groundCheckDepth, Is.EqualTo(0.08f));
            Assert.That(settings.wallSkin, Is.EqualTo(0.04f));
        }

        [Test]
        public void BaseJumpMatchesOneCellAndThreeCellGapContract()
        {
            PlayerMotionSettings settings = new PlayerMotionSettings();

            Assert.That(settings.BaseJumpApex, Is.EqualTo(1.37f).Within(0.01f));
            Assert.That(settings.SameHeightAirTime, Is.EqualTo(0.625f).Within(0.001f));
            Assert.That(settings.FullSpeedHorizontalDistance, Is.EqualTo(3.75f).Within(0.001f));
            Assert.That(
                settings.PredictDiscreteApexHeight(PlayerMotionSettings.RequiredFixedDeltaTime),
                Is.GreaterThan(1f).And.LessThan(2f));
        }

        [Test]
        public void RequiredColliderFitsInsideOneCell()
        {
            Assert.That(PlayerMotor2D.ColliderWidth, Is.EqualTo(0.72f));
            Assert.That(PlayerMotor2D.ColliderHeight, Is.EqualTo(0.92f));
            Assert.That(PlayerMotor2D.ColliderWidth, Is.LessThan(1f));
            Assert.That(PlayerMotor2D.ColliderHeight, Is.LessThan(1f));
        }

        [Test]
        public void SafeCellAndVoidRecoveryMatchCore01Contract()
        {
            Vector2 playerCenter = new Vector2(3.25f, 4f + PlayerGridContract.ColliderHeight * 0.5f);
            SafeCellState state = SafeCellState.FromPlayerCenter(playerCenter);

            Assert.That(state.IsValid, Is.True);
            Assert.That(state.Cell, Is.EqualTo(new Vector2Int(3, 4)));
            Assert.That(state.PlayerCenter, Is.EqualTo(playerCenter));
            Assert.That(
                PlayerOutOfBoundsGuard.RequiredRecoveryInvulnerabilitySeconds,
                Is.EqualTo(0.8f));
            Assert.That(PlayerMotionSettings.RequiredFixedDeltaTime, Is.EqualTo(1f / 60f));
        }
    }
}

#endif
