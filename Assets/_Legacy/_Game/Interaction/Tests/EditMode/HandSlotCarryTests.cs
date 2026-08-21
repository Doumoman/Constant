#if LEGACY_DISABLED
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Interaction.Carry;
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.Input;
using StarNight.Interaction.Reactions;
using UnityEngine;

namespace StarNight.Interaction.Tests
{
    public sealed class HandSlotCarryTests
    {
        private readonly List<Object> cleanup = new List<Object>();
        private GameObject player;
        private HandSlotPresenter presenter;
        private PlayerHandSlot handSlot;

        [SetUp]
        public void SetUp()
        {
            player = new GameObject("HandSlotCarryTests_Player");
            cleanup.Add(player);
            presenter = player.AddComponent<HandSlotPresenter>();
            presenter.ConfigureForTests(player.transform);
            handSlot = player.AddComponent<PlayerHandSlot>();
            handSlot.ConfigureForTests(presenter);
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = cleanup.Count - 1; index >= 0; index--)
            {
                if (cleanup[index] != null)
                {
                    Object.DestroyImmediate(cleanup[index]);
                }
            }
            cleanup.Clear();
        }

        [TestCase(CarryWeightClass.Light, 0.5f, 1.00f, 1.00f, true, 1)]
        [TestCase(CarryWeightClass.Medium, 1.5f, 0.90f, 0.90f, true, 1)]
        [TestCase(CarryWeightClass.Heavy, 4.0f, 0.65f, 0.67f, false, 2)]
        public void WeightProfilesMatchApprovedContract(
            CarryWeightClass weight,
            float mass,
            float move,
            float jump,
            bool rope,
            int plateWeight)
        {
            CarryObjectDefinition definition = Definition(weight);

            Assert.That(definition.Mass, Is.EqualTo(mass).Within(0.001f));
            Assert.That(definition.MovementMultiplier, Is.EqualTo(move).Within(0.001f));
            Assert.That(definition.JumpHeightMultiplier, Is.EqualTo(jump).Within(0.001f));
            Assert.That(definition.CanClimbRope, Is.EqualTo(rope));
            Assert.That(definition.PlateWeight, Is.EqualTo(plateWeight));
        }

        [Test]
        public void PickupCompletesAfterPointOneSecondsAndEntersKinematicHeldState()
        {
            CarryableObject carryable = Carry(CarryWeightClass.Light, new Vector2(0.5f, 0f));

            Assert.That(handSlot.TryBeginPickup(carryable, 10f), Is.True);
            Assert.That(handSlot.TickPickup(10.099f), Is.False);
            Assert.That(handSlot.IsEmpty, Is.True);
            Assert.That(handSlot.TickPickup(10.10f), Is.True);
            Assert.That(handSlot.HeldCarryable, Is.EqualTo(carryable));
            Assert.That(carryable.RuntimeState, Is.EqualTo(CarryRuntimeState.Held));
            Assert.That(carryable.Body.bodyType, Is.EqualTo(RigidbodyType2D.Kinematic));
            Assert.That(carryable.gameObject.layer, Is.EqualTo(LayerMask.NameToLayer("HeldObject")));
        }

        [TestCase(CarryWeightClass.Light, false, 6.5f, 2.0f)]
        [TestCase(CarryWeightClass.Medium, false, 5.0f, 1.5f)]
        [TestCase(CarryWeightClass.Heavy, false, 2.7f, 0.5f)]
        [TestCase(CarryWeightClass.Light, true, 2.5f, 7.0f)]
        [TestCase(CarryWeightClass.Medium, true, 1.8f, 5.6f)]
        public void ThrowVelocityMatchesApprovedContract(
            CarryWeightClass weight,
            bool upward,
            float expectedX,
            float expectedY)
        {
            ThrowResolution result = new ThrowResolver().Resolve(weight, 1, upward);

            Assert.That(result.CanThrow, Is.True);
            Assert.That(result.Velocity.x, Is.EqualTo(expectedX).Within(0.001f));
            Assert.That(result.Velocity.y, Is.EqualTo(expectedY).Within(0.001f));
        }

        [Test]
        public void HeavyUpThrowFallsBackToPlacement()
        {
            ThrowResolution result = new ThrowResolver().Resolve(CarryWeightClass.Heavy, 1, true);

            Assert.That(result.CanThrow, Is.False);
            Assert.That(result.ShouldDropInstead, Is.True);
        }

        [TestCase(CarryWeightClass.Light)]
        [TestCase(CarryWeightClass.Medium)]
        public void OneByOneLightAndMediumSurvivePortalWithoutClearance(CarryWeightClass weight)
        {
            CarryableObject carryable = Carry(weight, Vector2.zero);
            Assert.That(handSlot.TryAttach(carryable), Is.True);

            Assert.That(handSlot.TrySuspendForPortal(null, out CarryObjectSnapshot snapshot), Is.True);
            Assert.That(snapshot.HeldInHandSlot, Is.True);
            Assert.That(snapshot.WeightClass, Is.EqualTo(weight));
            Assert.That(handSlot.RestoreAfterPortal(), Is.True);
            Assert.That(handSlot.HeldCarryable, Is.EqualTo(carryable));
            Assert.That(carryable.RuntimeState, Is.EqualTo(CarryRuntimeState.Held));
        }

        [Test]
        public void HeavyPortalRequiresClearanceAndRemainsHeldWhenRejected()
        {
            CarryableObject carryable = Carry(CarryWeightClass.Heavy, Vector2.zero, new Vector2Int(1, 2));
            Assert.That(handSlot.TryAttach(carryable), Is.True);

            Assert.That(handSlot.TrySuspendForPortal(null, out _), Is.False);
            Assert.That(handSlot.HeldCarryable, Is.EqualTo(carryable));
            Assert.That(carryable.RuntimeState, Is.EqualTo(CarryRuntimeState.Held));

            Assert.That(handSlot.TrySuspendForPortal(new AllowAllClearance(), out _), Is.True);
            Assert.That(handSlot.RestoreAfterPortal(), Is.True);
            Assert.That(handSlot.HeldCarryable, Is.EqualTo(carryable));
        }

        [Test]
        public void DropUsesSafeFrontCellAndZeroVelocity()
        {
            CarryableObject carryable = Carry(CarryWeightClass.Medium, Vector2.zero);
            Assert.That(handSlot.TryAttach(carryable), Is.True);
            PlayerActionLock actionLock = player.AddComponent<PlayerActionLock>();
            HandSlotTransferService transfer = player.AddComponent<HandSlotTransferService>();
            transfer.ConfigureForTests(handSlot, null, actionLock, new AlwaysSafePlacementWorld());

            Assert.That(
                transfer.TryDropHandSlot(new PlayerActionContext(100, 0f, -1f, true)),
                Is.True);
            Assert.That(handSlot.IsEmpty, Is.True);
            Assert.That(carryable.transform.position, Is.EqualTo(new Vector3(1f, 0f, 0f)));
            Assert.That(carryable.Velocity, Is.EqualTo(Vector2.zero));
            Assert.That(carryable.RuntimeState, Is.EqualTo(CarryRuntimeState.World));
        }

        [Test]
        public void ImpactClassifierAndActionDeduplicationMatchContract()
        {
            Assert.That(
                ImpactClassifier.Classify(0.5f, 3f, CarryWeightClass.Light),
                Is.EqualTo(CarryImpactClass.None));
            Assert.That(
                ImpactClassifier.Classify(1.5f, 2f, CarryWeightClass.Medium),
                Is.EqualTo(CarryImpactClass.LightImpact));
            Assert.That(
                ImpactClassifier.Classify(4f, 3f, CarryWeightClass.Heavy),
                Is.EqualTo(CarryImpactClass.HeavyImpact));

            var deduplicator = new ImpactActionDeduplicator();
            Assert.That(deduplicator.ShouldApply(7, 42, 1f), Is.True);
            Assert.That(deduplicator.ShouldApply(7, 42, 1.149f), Is.False);
            Assert.That(deduplicator.ShouldApply(7, 42, 1.15f), Is.True);
        }

        private CarryObjectDefinition Definition(
            CarryWeightClass weight,
            Vector2Int? footprint = null)
        {
            CarryObjectDefinition definition = ScriptableObject.CreateInstance<CarryObjectDefinition>();
            definition.ConfigureForTests(
                "TEST_" + weight,
                weight,
                footprint ?? Vector2Int.one);
            cleanup.Add(definition);
            return definition;
        }

        private CarryableObject Carry(
            CarryWeightClass weight,
            Vector2 position,
            Vector2Int? footprint = null)
        {
            GameObject carryObject = new GameObject("Carry_" + weight);
            cleanup.Add(carryObject);
            carryObject.transform.position = position;
            carryObject.layer = LayerMask.NameToLayer("DynamicObject");
            Rigidbody2D body = carryObject.AddComponent<Rigidbody2D>();
            carryObject.AddComponent<BoxCollider2D>();
            CarryableObject carryable = carryObject.AddComponent<CarryableObject>();
            carryable.ConfigureForTests(Definition(weight, footprint), body);
            return carryable;
        }

        private sealed class AllowAllClearance : ICarryPortalClearance
        {
            public bool Allows(CarryObjectDefinition definition) => true;
        }

        private sealed class AlwaysSafePlacementWorld : ICarryPlacementWorld
        {
            public bool IsInsideRoom(RectInt footprint) => true;
            public bool IsFootprintClear(RectInt footprint) => true;
            public bool HasStableSupport(RectInt footprint) => true;
            public bool IsPortalGap(RectInt footprint) => false;
            public bool IsVoid(RectInt footprint) => false;
        }
    }
}

#endif
