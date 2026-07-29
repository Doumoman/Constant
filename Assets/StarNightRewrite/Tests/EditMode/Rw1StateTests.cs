using NUnit.Framework;
using StarNight.Rewrite.Core;
using StarNight.Rewrite.Player;
using UnityEngine;

namespace StarNight.Rewrite.Tests
{
    public sealed class Rw1StateTests
    {
        [Test]
        public void NewRun_UsesLightweightStartingLoadout()
        {
            RunLoadout loadout = new RunLoadout();
            loadout.ResetForNewRun();

            Assert.That(loadout.Ropes, Is.EqualTo(3));
            Assert.That(loadout.Bombs, Is.EqualTo(3));
            Assert.That(loadout.Gold, Is.Zero);
            Assert.That(loadout.HandTool, Is.EqualTo(HandToolId.None));
            Assert.That(loadout.HasPromiseItem, Is.False);
        }

        [Test]
        public void Consumables_ClampAtSixAndCannotUnderflow()
        {
            RunLoadout loadout = new RunLoadout();
            loadout.ResetForNewRun();

            Assert.That(loadout.AddRopes(99), Is.EqualTo(3));
            Assert.That(loadout.AddBombs(99), Is.EqualTo(3));
            Assert.That(loadout.Ropes, Is.EqualTo(6));
            Assert.That(loadout.Bombs, Is.EqualTo(6));

            for (int index = 0; index < 6; index++)
            {
                Assert.That(loadout.TryConsumeRope(), Is.True);
                Assert.That(loadout.TryConsumeBomb(), Is.True);
            }

            Assert.That(loadout.TryConsumeRope(), Is.False);
            Assert.That(loadout.TryConsumeBomb(), Is.False);
        }

        [Test]
        public void HandTool_EquipReturnsPreviousSingleSlotTool()
        {
            RunLoadout loadout = new RunLoadout();
            loadout.ResetForNewRun();

            Assert.That(
                loadout.EquipHandTool(HandToolId.Pickaxe),
                Is.EqualTo(HandToolId.None));
            Assert.That(
                loadout.EquipHandTool(HandToolId.Umbrella),
                Is.EqualTo(HandToolId.Pickaxe));
            Assert.That(loadout.HandTool, Is.EqualTo(HandToolId.Umbrella));
        }

        [Test]
        public void PromiseItem_AllowsExactlyOneAttachedItem()
        {
            RunLoadout loadout = new RunLoadout();
            loadout.ResetForNewRun();

            Assert.That(loadout.TryAttachPromiseItem("RETURN_CAKE"), Is.True);
            Assert.That(loadout.TryAttachPromiseItem("RED_THREAD"), Is.False);
            Assert.That(loadout.DetachPromiseItem(), Is.EqualTo("RETURN_CAKE"));
            Assert.That(loadout.HasPromiseItem, Is.False);
        }

        [Test]
        public void Gold_CannotSpendMoreThanOwned()
        {
            RunLoadout loadout = new RunLoadout();
            loadout.ResetForNewRun();
            loadout.AddGold(7);

            Assert.That(loadout.TrySpendGold(8), Is.False);
            Assert.That(loadout.TrySpendGold(6), Is.True);
            Assert.That(loadout.Gold, Is.EqualTo(1));
        }

        [Test]
        public void Health_DamageUsesFourHeartsAndInvulnerability()
        {
            PlayerHealthState health = new PlayerHealthState(4);

            Assert.That(health.TryDamage(1, 1.3f), Is.True);
            Assert.That(health.Current, Is.EqualTo(3));
            Assert.That(health.TryDamage(1, 1.3f), Is.False);

            health.Tick(1.3f);

            Assert.That(health.TryDamage(3, 1.3f), Is.True);
            Assert.That(health.IsDepleted, Is.True);
        }

        [Test]
        public void Health_HealingClampsAtMaximum()
        {
            PlayerHealthState health = new PlayerHealthState(4);
            health.TryDamage(2, 0f);

            Assert.That(health.Heal(99), Is.EqualTo(2));
            Assert.That(health.Current, Is.EqualTo(4));
        }

        [Test]
        public void JumpAssist_AllowsBufferedCoyoteJump()
        {
            PlayerJumpAssist assist = new PlayerJumpAssist(0.12f, 0.12f);
            assist.Tick(0f, true);
            assist.Tick(0.05f, false);
            assist.BufferJump();

            Assert.That(assist.TryConsumeJump(), Is.True);
            Assert.That(assist.TryConsumeJump(), Is.False);
        }

        [Test]
        public void JumpAssist_ExpiresOldInputsAndCoyoteWindow()
        {
            PlayerJumpAssist assist = new PlayerJumpAssist(0.12f, 0.12f);
            assist.Tick(0f, true);
            assist.BufferJump();
            assist.Tick(0.13f, false);

            Assert.That(assist.HasBufferedJump, Is.False);
            Assert.That(assist.HasCoyoteTime, Is.False);
            Assert.That(assist.TryConsumeJump(), Is.False);
        }

        [Test]
        public void RaniLamp_CanRescueOnlyOnceUntilRecharged()
        {
            GameObject owner = new GameObject("Lamp Test");
            try
            {
                RaniLampController lamp = owner.AddComponent<RaniLampController>();

                Assert.That(lamp.TryConsumeRescue(), Is.True);
                Assert.That(lamp.TryConsumeRescue(), Is.False);
                lamp.RechargeForChapter();
                Assert.That(lamp.TryConsumeRescue(), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(owner);
            }
        }
    }
}
