#if LEGACY_DISABLED
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Campaign.P11;
using StarNight.Grid;
using StarNight.Player;
using StarNight.Stages.P5;
using StarNight.Tools.Water;
using UnityEngine;

namespace StarNight.Tests.EditMode
{
    public sealed class P11ExtendedRegionalMechanicsTests
    {
        private readonly List<GameObject> created =
            new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int index = created.Count - 1;
                 index >= 0;
                 index--)
            {
                if (created[index] != null)
                {
                    Object.DestroyImmediate(created[index]);
                }
            }

            created.Clear();
        }

        [Test]
        public void ShadowSeed_SlowsWithoutDamageAndLightConsumesIt()
        {
            GameObject flowerObject = Track("FlowerPlatform");
            BoxCollider2D flowerCollider =
                flowerObject.AddComponent<BoxCollider2D>();
            SpriteRenderer flowerVisual =
                flowerObject.AddComponent<SpriteRenderer>();
            P11LightReactivePlatform2D flower = flowerObject
                .AddComponent<P11LightReactivePlatform2D>();
            flower.Configure(flowerCollider, flowerVisual, false);

            GameObject rayObject = Track("RotatingRay");
            P11RotatingSunRay2D ray =
                rayObject.AddComponent<P11RotatingSunRay2D>();
            ray.Configure(
                12f,
                null,
                new P11LightReactivePlatform2D[0],
                10f,
                0.5f);

            GameObject seedObject = Track("ShadowSeed");
            seedObject.transform.position = new Vector3(3f, 0f, 0f);
            CircleCollider2D slowTrigger =
                seedObject.AddComponent<CircleCollider2D>();
            P11ShadowSeed2D seed =
                seedObject.AddComponent<P11ShadowSeed2D>();
            SpriteRenderer seedVisual =
                seedObject.AddComponent<SpriteRenderer>();
            seed.Configure(
                seed.GetComponent<StarNight.Objects.CarryableObject2D>(),
                slowTrigger,
                ray,
                flower,
                seedVisual,
                0.5f);

            GameObject playerObject = Track("Player");
            Rigidbody2D playerBody =
                playerObject.AddComponent<Rigidbody2D>();
            PlayerMotor2D motor =
                playerObject.AddComponent<PlayerMotor2D>();
            playerBody.linearVelocity = new Vector2(8f, 2f);

            Assert.That(seed.TryApplySlow(motor), Is.True);
            Assert.That(
                playerBody.linearVelocity,
                Is.EqualTo(new Vector2(4f, 2f)));
            Assert.That(seed.DealsDamage, Is.False);
            Assert.That(seed.CarryThrowCompatible, Is.True);

            Assert.That(seed.EvaluateIlluminationNow(), Is.True);
            Assert.That(seed.IsConsumed, Is.True);
            Assert.That(flower.Illuminated, Is.True);
            Assert.That(flowerCollider.enabled, Is.True);
            Assert.That(seed.GetComponent<Rigidbody2D>().simulated, Is.False);
            Assert.That(seedVisual.enabled, Is.False);
        }

        [Test]
        public void GrowingVine_WaterGrowsFiveCellsAndLightWidensCollider()
        {
            WaterInteractionRegistry2D registry = Track("WaterRegistry")
                .AddComponent<WaterInteractionRegistry2D>();
            GameObject rayObject = Track("VineRay");
            rayObject.transform.position = new Vector3(0f, 2.5f, 0f);
            P11RotatingSunRay2D ray =
                rayObject.AddComponent<P11RotatingSunRay2D>();
            ray.Configure(
                12f,
                null,
                new P11LightReactivePlatform2D[0],
                10f,
                0.5f);

            GameObject vineObject = Track("GrowingVine");
            vineObject.transform.position = new Vector3(3f, 0f, 0f);
            BoxCollider2D vineCollider =
                vineObject.AddComponent<BoxCollider2D>();
            SpriteRenderer grownVisual =
                vineObject.AddComponent<SpriteRenderer>();
            P11GrowingVine2D vine =
                vineObject.AddComponent<P11GrowingVine2D>();
            GridPos cell = new GridPos(3, 0);
            vine.Configure(
                registry,
                null,
                cell,
                vineCollider,
                null,
                grownVisual,
                ray,
                5,
                1.8f);

            Assert.That(vineCollider.enabled, Is.False);
            var reactions = new List<WaterReactionRecord>();
            int reactionCount = registry.ApplyWater(
                cell,
                new WaterApplication(
                    cell,
                    new GridPos(1, 0),
                    cell,
                    0,
                    registry),
                reactions);

            Assert.That(reactionCount, Is.EqualTo(1));
            Assert.That(vine.IsGrown, Is.True);
            Assert.That(vineCollider.enabled, Is.True);
            Assert.That(vineCollider.size.y, Is.EqualTo(5f));
            Assert.That(vine.RefreshIlluminationNow(), Is.True);
            Assert.That(vine.Illuminated, Is.True);
            Assert.That(vineCollider.size.x, Is.EqualTo(1.8f));
            Assert.That(vine.GeometryRevision, Is.EqualTo(2));
        }

        [Test]
        public void OverheatedPlatform_DamagesOnceThenWaterMakesItSafe()
        {
            WaterInteractionRegistry2D registry = Track("WaterRegistry")
                .AddComponent<WaterInteractionRegistry2D>();
            GameObject platformObject = Track("OverheatedPlatform");
            BoxCollider2D support =
                platformObject.AddComponent<BoxCollider2D>();
            CircleCollider2D heatTrigger =
                platformObject.AddComponent<CircleCollider2D>();
            SpriteRenderer visual =
                platformObject.AddComponent<SpriteRenderer>();
            P11OverheatedPlatform2D platform = platformObject
                .AddComponent<P11OverheatedPlatform2D>();
            GridPos cell = new GridPos(2, 1);
            platform.Configure(
                registry,
                null,
                cell,
                support,
                heatTrigger,
                visual);

            GameObject playerObject = Track("Player");
            PlayerRecovery recovery =
                playerObject.AddComponent<PlayerRecovery>();
            recovery.ResetHealthForTests();
            int healthBefore = recovery.CurrentHealth;

            Assert.That(
                platform.TryApplyStandingDamage(recovery),
                Is.True);
            Assert.That(
                recovery.CurrentHealth,
                Is.EqualTo(healthBefore - 1));
            Assert.That(support.enabled, Is.True);
            Assert.That(heatTrigger.enabled, Is.True);

            var reactions = new List<WaterReactionRecord>();
            Assert.That(
                registry.ApplyWater(
                    cell,
                    new WaterApplication(
                        cell,
                        new GridPos(1, 0),
                        cell,
                        0,
                        registry),
                    reactions),
                Is.EqualTo(1));
            Assert.That(platform.IsCooled, Is.True);
            Assert.That(platform.SafeToStand, Is.True);
            Assert.That(support.enabled, Is.True);
            Assert.That(heatTrigger.enabled, Is.False);
            Assert.That(
                platform.TryApplyStandingDamage(recovery),
                Is.False);
        }

        [Test]
        public void ConstellationReceiver_InteractionActivatesBridgeSegment()
        {
            GameObject bridgeObject = Track("ConstellationBridge");
            GameObject[] segments = new GameObject[2];
            for (int index = 0; index < segments.Length; index++)
            {
                segments[index] = new GameObject($"Segment_{index}");
                segments[index].transform.SetParent(
                    bridgeObject.transform,
                    false);
                segments[index].AddComponent<BoxCollider2D>();
            }

            P11ConstellationBridge2D bridge = bridgeObject
                .AddComponent<P11ConstellationBridge2D>();
            bridge.Configure(segments);
            GameObject receiverObject = Track("Receiver");
            SpriteRenderer visual =
                receiverObject.AddComponent<SpriteRenderer>();
            P11ConstellationReceiver2D receiver = receiverObject
                .AddComponent<P11ConstellationReceiver2D>();
            receiver.Configure(bridge, 0, visual);
            GameObject playerObject = Track("Player");
            var context = new P5PlayerInteractionContext(
                playerObject.transform,
                null,
                null,
                null,
                null);

            Assert.That(
                receiver.TryInteractForTests(context),
                Is.True);
            Assert.That(receiver.IsActivated, Is.True);
            Assert.That(receiver.ActivationCount, Is.EqualTo(1));
            Assert.That(bridge.IsReceiverActive(0), Is.True);
            Assert.That(bridge.IsSegmentColliderEnabled(0), Is.True);
            Assert.That(receiver.TryActivate(), Is.False);
        }

        [Test]
        public void MemoryBell_InteractionRevealsDirectionAndPulsesVisual()
        {
            GameObject bellObject = Track("MemoryBell");
            SpriteRenderer bellVisual =
                bellObject.AddComponent<SpriteRenderer>();
            GameObject directionReveal = new GameObject("DirectionReveal");
            directionReveal.transform.SetParent(
                bellObject.transform,
                false);
            GameObject target = Track("ExitTarget");
            target.transform.position = new Vector3(5f, 0f, 0f);
            P11MemoryBellDevice2D bell = bellObject
                .AddComponent<P11MemoryBellDevice2D>();
            bell.Configure(
                target.transform,
                bellVisual,
                directionReveal,
                0.5f);
            GameObject playerObject = Track("Player");
            var context = new P5PlayerInteractionContext(
                playerObject.transform,
                null,
                null,
                null,
                null);

            Assert.That(directionReveal.activeSelf, Is.False);
            Assert.That(bell.TryInteractForTests(context), Is.True);
            Assert.That(bell.DirectionRevealed, Is.True);
            Assert.That(directionReveal.activeSelf, Is.True);
            Assert.That(bell.RevealedDirection, Is.EqualTo(Vector2.right));
            Assert.That(bell.IsPulsing, Is.True);
            Assert.That(bell.RingCount, Is.EqualTo(1));
            Assert.That(bell.IsFinalStoryBell, Is.False);

            bell.TickForTests(0.5f);
            Assert.That(bell.IsPulsing, Is.False);
            Assert.That(
                bellVisual.transform.localScale,
                Is.EqualTo(Vector3.one));
            Assert.That(directionReveal.activeSelf, Is.True);
        }

        private GameObject Track(string name)
        {
            GameObject gameObject = new GameObject(name);
            created.Add(gameObject);
            return gameObject;
        }
    }
}

#endif
