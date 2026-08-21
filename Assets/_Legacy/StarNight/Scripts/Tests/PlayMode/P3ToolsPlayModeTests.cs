#if LEGACY_DISABLED
using System.Collections;
using NUnit.Framework;
using StarNight.Grid;
using StarNight.Player;
using StarNight.Tools;
using StarNight.Tools.Mining;
using StarNight.Tools.Rope;
using StarNight.Tools.Umbrella;
using StarNight.Tools.Water;
using UnityEngine;
using UnityEngine.TestTools;
using UnityGrid = UnityEngine.Grid;

namespace StarNight.Tests.PlayMode
{
    public sealed class P3ToolsPlayModeTests
    {
        [UnityTest]
        public IEnumerator HandToolPickup_PickupAndDropTogglePhysicalPickupState()
        {
            GameObject originalParent =
                new GameObject("P3_HandTool_OriginalParent");
            GameObject anchorObject =
                new GameObject("P3_HandTool_HoldAnchor");
            GameObject toolObject = new GameObject("P3_HandTool_Pickup");
            toolObject.transform.SetParent(originalParent.transform, false);
            BoxCollider2D pickupCollider =
                toolObject.AddComponent<BoxCollider2D>();
            HandToolPickup2D pickup =
                toolObject.AddComponent<HandToolPickup2D>();
            pickup.Configure(
                HandToolKind.Grapple,
                0,
                pickupCollider);

            int pickedUpEvents = 0;
            int droppedEvents = 0;
            pickup.PickedUp += _ => pickedUpEvents++;
            pickup.Dropped += _ => droppedEvents++;

            Assert.That(pickup.TryPickUp(anchorObject.transform), Is.True);
            Assert.That(pickup.IsHeld, Is.True);
            Assert.That(pickupCollider.enabled, Is.False);
            Assert.That(toolObject.transform.parent, Is.SameAs(anchorObject.transform));
            Assert.That(toolObject.transform.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(pickedUpEvents, Is.EqualTo(1));
            Assert.That(pickup.TryPickUp(anchorObject.transform), Is.False);

            Vector2 dropPosition = new Vector2(3.5f, 2.5f);
            Assert.That(pickup.Drop(dropPosition), Is.True);
            Assert.That(pickup.IsHeld, Is.False);
            Assert.That(pickupCollider.enabled, Is.True);
            Assert.That(toolObject.transform.parent, Is.SameAs(originalParent.transform));
            Assert.That(
                Vector2.Distance(toolObject.transform.position, dropPosition),
                Is.LessThan(0.0001f));
            Assert.That(droppedEvents, Is.EqualTo(1));
            Assert.That(pickup.Drop(dropPosition), Is.False);

            Object.Destroy(originalParent);
            Object.Destroy(anchorObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator WateringCan_HasSixChargesAndOnlyWaterSourceRechargesIt()
        {
            GameObject root = new GameObject("P3_WateringCan_Test");
            UnityGrid layout = root.AddComponent<UnityGrid>();
            GridWorld world = root.AddComponent<GridWorld>();
            world.Configure(
                layout,
                null,
                null,
                Vector2Int.zero,
                new Vector2Int(12, 8));

            WateringCanTool2D wateringCan =
                root.AddComponent<WateringCanTool2D>();
            wateringCan.Configure(
                world,
                null,
                WateringCanTool2D.Capacity);

            GameObject sourceObject = new GameObject("P3_WaterSource_Test");
            sourceObject.transform.SetParent(root.transform, false);
            WaterSource2D source = sourceObject.AddComponent<WaterSource2D>();
            GridPos origin = new GridPos(1, 4);
            source.Configure(world, origin, 1, true);

            int chargeEvents = 0;
            int useEvents = 0;
            int rechargeEvents = 0;
            int sourceEvents = 0;
            wateringCan.ChargesChanged += (charges, maximum) => chargeEvents++;
            wateringCan.Used += _ => useEvents++;
            wateringCan.Recharged += () => rechargeEvents++;
            source.CanRefilled += _ => sourceEvents++;

            Assert.That(wateringCan.MaxCharges, Is.EqualTo(6));
            for (int use = 0; use < WateringCanTool2D.Capacity; use++)
            {
                Assert.That(
                    wateringCan.TryUse(
                        origin,
                        new GridPos(1, 0),
                        out WaterUseReport report),
                    Is.True);
                Assert.That(report.WateredCellCount, Is.EqualTo(6));
                Assert.That(
                    report.WateredCells[0],
                    Is.EqualTo(new GridPos(2, 4)));
                Assert.That(
                    report.WateredCells[1],
                    Is.EqualTo(new GridPos(3, 4)));
                Assert.That(
                    report.WateredCells[2],
                    Is.EqualTo(new GridPos(4, 4)));
                Assert.That(
                    wateringCan.Charges,
                    Is.EqualTo(WateringCanTool2D.Capacity - use - 1));
            }

            Assert.That(wateringCan.HasWater, Is.False);
            Assert.That(
                wateringCan.TryUse(
                    origin,
                    new GridPos(1, 0),
                    out WaterUseReport emptyReport),
                Is.False);
            Assert.That(emptyReport.WateredCellCount, Is.EqualTo(0));
            Assert.That(useEvents, Is.EqualTo(6));
            Assert.That(chargeEvents, Is.EqualTo(6));

            Assert.That(
                source.TryRefill(
                    wateringCan,
                    new GridPos(origin.X + 1, origin.Y)),
                Is.True);
            Assert.That(wateringCan.Charges, Is.EqualTo(6));
            Assert.That(rechargeEvents, Is.EqualTo(1));
            Assert.That(sourceEvents, Is.EqualTo(1));
            Assert.That(
                source.TryRefill(wateringCan, origin),
                Is.False,
                "A full can must not emit another recharge.");

            wateringCan.SetChargesForTests(0);
            source.SetAvailable(false);
            Assert.That(
                source.TryRefill(wateringCan, origin),
                Is.False);
            Assert.That(wateringCan.Charges, Is.EqualTo(0));

            Object.Destroy(root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Pickaxe_StunsOnlyExactAdjacentCellAndConsumesOneUse()
        {
            GameObject root = new GameObject("P3_PickaxeStun_Test");
            UnityGrid layout = root.AddComponent<UnityGrid>();
            GridWorld world = root.AddComponent<GridWorld>();
            world.Configure(
                layout,
                null,
                null,
                Vector2Int.zero,
                new Vector2Int(6, 4));

            PickaxeTool2D pickaxe = root.AddComponent<PickaxeTool2D>();
            pickaxe.Configure(
                world,
                null,
                PickaxeTool2D.DefaultDurability,
                0f);

            GridPos origin = new GridPos(1, 1);
            GridPos targetCell = new GridPos(2, 1);
            GameObject target = new GameObject("Pickaxe_Stunnable_Target");
            target.transform.position = world.CellToWorldCenter(targetCell);
            target.AddComponent<BoxCollider2D>().size =
                Vector2.one * 0.75f;
            PickaxeStunnable2D stunnable =
                target.AddComponent<PickaxeStunnable2D>();
            Physics2D.SyncTransforms();

            MiningUseResult result =
                pickaxe.TryUseImmediatelyForTests(
                    origin,
                    Vector2Int.right);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.AffectedNonTerrainTarget, Is.True);
            Assert.That(result.Queued, Is.False);
            Assert.That(stunnable.IsStunned, Is.True);
            Assert.That(stunnable.StunCount, Is.EqualTo(1));
            Assert.That(
                stunnable.StunRemaining,
                Is.EqualTo(PickaxeTool2D.DefaultEnemyStunSeconds)
                    .Within(0.0001f));
            Assert.That(
                pickaxe.RemainingDurability,
                Is.EqualTo(PickaxeTool2D.DefaultDurability - 1));

            target.transform.position =
                world.CellToWorldCenter(new GridPos(3, 1));
            Physics2D.SyncTransforms();
            MiningUseResult miss =
                pickaxe.TryUseImmediatelyForTests(
                    origin,
                    Vector2Int.right);
            Assert.That(miss.Succeeded, Is.False);
            Assert.That(stunnable.StunCount, Is.EqualTo(1));
            Assert.That(
                pickaxe.RemainingDurability,
                Is.EqualTo(PickaxeTool2D.DefaultDurability - 1));

            Object.Destroy(root);
            Object.Destroy(target);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RopeFireSource_BreaksOverlappingStaticTriggerRope()
        {
            GameObject installationObject =
                new GameObject("P3_StaticFire_Rope");
            RopeInstallation2D installation =
                installationObject.AddComponent<RopeInstallation2D>();
            GameObject segmentObject =
                new GameObject("P3_StaticFire_Segment");
            segmentObject.transform.SetParent(
                installationObject.transform,
                false);
            BoxCollider2D segmentCollider =
                segmentObject.AddComponent<BoxCollider2D>();
            segmentCollider.isTrigger = true;
            RopeSegment2D segment =
                segmentObject.AddComponent<RopeSegment2D>();
            GridPos ropeCell = new GridPos(1, 1);
            RopeInstallPlan plan = new RopeInstallPlan(
                new GridPos(1, 0),
                ropeCell,
                RopeAnchorKind.Ring,
                new[] { ropeCell });
            segment.Configure(installation, ropeCell, segmentCollider);
            installation.Configure(null, plan, new[] { segment });

            GameObject fireObject =
                new GameObject("P3_StaticFire_Source");
            BoxCollider2D fireCollider =
                fireObject.AddComponent<BoxCollider2D>();
            fireCollider.isTrigger = true;
            RopeFireSource2D fire =
                fireObject.AddComponent<RopeFireSource2D>();
            fire.Configure(true, fireCollider);
            Physics2D.SyncTransforms();

            int broken = fire.BreakOverlappingRopesForTests();

            Assert.That(broken, Is.EqualTo(1));
            Assert.That(installation.IsBroken, Is.True);
            Assert.That(
                installation.LastDamageKind,
                Is.EqualTo(RopeDamageKind.Fire));
            Assert.That(segment.IsClimbable, Is.False);

            Object.Destroy(fireObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator WindUmbrella_OpenCapsFallCloseRestoresFallAndWindIsAmplified()
        {
            GameObject player = new GameObject("P3_WindUmbrella_Player");
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            player.AddComponent<CapsuleCollider2D>();
            PlayerMotor2D motor = player.AddComponent<PlayerMotor2D>();
            P1MovementTuning tuning =
                ScriptableObject.CreateInstance<P1MovementTuning>();
            motor.Configure(body, null, null, tuning);

            WindUmbrellaMotor2D umbrella =
                player.AddComponent<WindUmbrellaMotor2D>();
            umbrella.Configure(motor, body, 5.5f);

            Vector2 baselineNormalWind = WindField2D.SampleAcceleration(
                body.position,
                WindResponse.Normal);
            Vector2 baselineUmbrellaWind = WindField2D.SampleAcceleration(
                body.position,
                WindResponse.Umbrella);

            GameObject windObject = new GameObject("P3_WindZone_Test");
            BoxCollider2D windCollider =
                windObject.AddComponent<BoxCollider2D>();
            windCollider.size = new Vector2(20f, 20f);
            WindZone2D windZone = windObject.AddComponent<WindZone2D>();
            windZone.Configure(
                windCollider,
                Vector2.right,
                4f,
                2f,
                1);
            Physics2D.SyncTransforms();

            Vector2 normalWind = WindField2D.SampleAcceleration(
                body.position,
                WindResponse.Normal);
            Vector2 umbrellaWind = WindField2D.SampleAcceleration(
                body.position,
                WindResponse.Umbrella);
            Assert.That(
                normalWind - baselineNormalWind,
                Is.EqualTo(new Vector2(4f, 0f)));
            Assert.That(
                umbrellaWind - baselineUmbrellaWind,
                Is.EqualTo(new Vector2(8f, 0f)));

            umbrella.SetHeldAndOpen(true, true);
            Assert.That(motor.IsGrounded, Is.False);
            Assert.That(umbrella.IsHeld, Is.True);
            Assert.That(umbrella.OpenRequested, Is.True);
            Assert.That(umbrella.IsOpen, Is.True);

            body.linearVelocity = new Vector2(0f, -20f);
            umbrella.SimulatePostMotorStep(0.5f);
            Assert.That(
                body.linearVelocity.x,
                Is.EqualTo(umbrellaWind.x * 0.5f).Within(0.0001f));
            Assert.That(
                body.linearVelocity.y,
                Is.EqualTo(-5.5f + umbrellaWind.y * 0.5f).Within(0.0001f));

            umbrella.SetOpen(false);
            Assert.That(umbrella.OpenRequested, Is.False);
            Assert.That(umbrella.IsOpen, Is.False);
            body.linearVelocity = new Vector2(0f, -20f);
            umbrella.SimulatePostMotorStep(0.5f);
            Assert.That(
                body.linearVelocity.x,
                Is.EqualTo(normalWind.x * 0.5f).Within(0.0001f));
            Assert.That(
                body.linearVelocity.y,
                Is.EqualTo(-20f + normalWind.y * 0.5f).Within(0.0001f),
                "Closing must immediately restore uncapped normal falling.");

            umbrella.SetOpen(true);
            umbrella.SetHeld(false);
            Assert.That(umbrella.IsHeld, Is.False);
            Assert.That(umbrella.OpenRequested, Is.False);
            Assert.That(umbrella.IsOpen, Is.False);
            body.linearVelocity = Vector2.zero;
            umbrella.SimulatePostMotorStep(0.5f);
            Assert.That(
                body.linearVelocity,
                Is.EqualTo(Vector2.zero),
                "A dropped umbrella must stop affecting its former player.");

            Object.Destroy(player);
            Object.Destroy(windObject);
            Object.Destroy(tuning);
            yield return null;
        }
    }
}

#endif
