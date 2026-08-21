#if LEGACY_DISABLED
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Campaign.P11;
using UnityEngine;

namespace StarNight.Tests.EditMode
{
    public sealed class P11RegionalMechanicsTests
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
        public void SortingGoal_CorrectParcelMarksStoryFact()
        {
            P11StoryState2D story = Track("Story")
                .AddComponent<P11StoryState2D>();
            story.Configure(null);
            P11AddressableParcel2D parcel = CreateParcel(
                "BirdParcel",
                P11ParcelLabel.Bird,
                new Vector2(2f, 3f));
            GameObject goalObject = Track("BirdSortingGoal");
            goalObject.AddComponent<BoxCollider2D>();
            P11SortingGoal2D goal =
                goalObject.AddComponent<P11SortingGoal2D>();
            goal.Configure(
                P11ParcelLabel.Bird,
                story,
                goalObject.transform);

            Assert.That(story.LostParcelSorted, Is.False);
            Assert.That(goal.TryAccept(parcel), Is.True);
            Assert.That(story.LostParcelSorted, Is.True);
            Assert.That(goal.Completed, Is.True);
            Assert.That(goal.AcceptedCount, Is.EqualTo(1));
            Assert.That(goal.AcceptedParcel, Is.SameAs(parcel));
            Assert.That(
                parcel.Body.position,
                Is.EqualTo((Vector2)goalObject.transform.position));
        }

        [Test]
        public void InkPool_TriggerApiMutatesParcelLabelAndState()
        {
            P11AddressableParcel2D parcel = CreateParcel(
                "MoonParcel",
                P11ParcelLabel.Moon,
                Vector2.zero);
            GameObject poolObject = Track("BirdInkPool");
            poolObject.AddComponent<BoxCollider2D>();
            P11InkPool2D pool =
                poolObject.AddComponent<P11InkPool2D>();
            pool.Configure(P11ParcelLabel.Bird, null);

            Assert.That(pool.TryApply(parcel), Is.True);
            Assert.That(parcel.Label, Is.EqualTo(P11ParcelLabel.Bird));
            Assert.That(
                parcel.PreviousLabel,
                Is.EqualTo(P11ParcelLabel.Moon));
            Assert.That(parcel.LabelChangeCount, Is.EqualTo(1));
            Assert.That(pool.ApplicationCount, Is.EqualTo(1));
            Assert.That(pool.LastParcel, Is.SameAs(parcel));
            Assert.That(pool.TryApply(parcel), Is.False);
        }

        [Test]
        public void ParcelLauncher_TriggerApiAppliesConfiguredVelocity()
        {
            P11AddressableParcel2D parcel = CreateParcel(
                "StarParcel",
                P11ParcelLabel.Star,
                Vector2.zero);
            GameObject launcherObject = Track("ParcelLauncher");
            launcherObject.AddComponent<BoxCollider2D>();
            P11ParcelLauncher2D launcher =
                launcherObject.AddComponent<P11ParcelLauncher2D>();
            launcher.Configure(
                Vector2.up,
                7.5f,
                P11ParcelLabel.Star);

            Assert.That(launcher.TryLaunch(parcel.Body), Is.True);
            Assert.That(
                parcel.Body.linearVelocity,
                Is.EqualTo(Vector2.up * 7.5f));
            Assert.That(
                launcher.LastLaunchVelocity,
                Is.EqualTo(Vector2.up * 7.5f));
            Assert.That(launcher.LastLaunchedBody, Is.SameAs(parcel.Body));
            Assert.That(launcher.LaunchCount, Is.EqualTo(1));
            Assert.That(launcher.HasLaunched, Is.True);
        }

        [Test]
        public void RotatingRay_IlluminatesAndPhysicallyTogglesPlatform()
        {
            GameObject platformObject = Track("LightPlatform");
            platformObject.transform.position = new Vector3(4f, 0f, 0f);
            BoxCollider2D platformCollider =
                platformObject.AddComponent<BoxCollider2D>();
            SpriteRenderer platformVisual =
                platformObject.AddComponent<SpriteRenderer>();
            P11LightReactivePlatform2D platform = platformObject
                .AddComponent<P11LightReactivePlatform2D>();
            platform.Configure(
                platformCollider,
                platformVisual,
                false);

            GameObject rayObject = Track("SunRay");
            P11RotatingSunRay2D ray =
                rayObject.AddComponent<P11RotatingSunRay2D>();
            ray.Configure(
                12f,
                null,
                new[] { platform },
                10f,
                0.5f);

            Assert.That(ray.EvaluateIlluminationNow(), Is.EqualTo(1));
            Assert.That(platform.Illuminated, Is.True);
            Assert.That(platform.PhysicalColliderEnabled, Is.True);

            platformObject.transform.position =
                new Vector3(0f, 4f, 0f);
            Assert.That(ray.EvaluateIlluminationNow(), Is.Zero);
            Assert.That(platform.Illuminated, Is.False);
            Assert.That(platform.PhysicalColliderEnabled, Is.False);
            Assert.That(platform.IlluminationChangeCount, Is.EqualTo(2));
            Assert.That(ray.EvaluationCount, Is.EqualTo(2));
        }

        [Test]
        public void ReturnField_RegistersPulsesAndUnregistersBody()
        {
            GameObject fieldObject = Track("ReturnField");
            BoxCollider2D fieldCollider =
                fieldObject.AddComponent<BoxCollider2D>();
            P11ReturnField2D field =
                fieldObject.AddComponent<P11ReturnField2D>();
            field.Configure(5f, null);

            GameObject bodyObject = Track("ReturnableBody");
            bodyObject.transform.position = new Vector3(2f, 3f, 0f);
            Rigidbody2D body = bodyObject.AddComponent<Rigidbody2D>();
            BoxCollider2D bodyCollider =
                bodyObject.AddComponent<BoxCollider2D>();

            Assert.That(fieldCollider.isTrigger, Is.True);
            Assert.That(field.HandleTriggerEnter(bodyCollider), Is.True);
            Assert.That(field.RegisteredBodyCount, Is.EqualTo(1));
            Assert.That(field.TryGetOrigin(body, out Vector2 origin), Is.True);
            Assert.That(origin, Is.EqualTo(new Vector2(2f, 3f)));

            body.position = new Vector2(8f, 9f);
            body.linearVelocity = new Vector2(3f, -2f);
            Assert.That(field.PulseNow(), Is.EqualTo(1));
            Assert.That(body.position, Is.EqualTo(origin));
            Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
            Assert.That(field.PulseCount, Is.EqualTo(1));
            Assert.That(field.LastPulseBodyCount, Is.EqualTo(1));

            Assert.That(field.HandleTriggerExit(bodyCollider), Is.True);
            Assert.That(field.RegisteredBodyCount, Is.Zero);
        }

        [Test]
        public void ConstellationBridge_ActivationEnablesPhysicalSegments()
        {
            GameObject bridgeObject = Track("ConstellationBridge");
            GameObject[] segments = new GameObject[3];
            for (int index = 0; index < segments.Length; index++)
            {
                segments[index] = Track($"StarSegment_{index}");
                segments[index].transform.SetParent(
                    bridgeObject.transform,
                    false);
                segments[index].AddComponent<BoxCollider2D>();
            }

            P11ConstellationBridge2D bridge = bridgeObject
                .AddComponent<P11ConstellationBridge2D>();
            bridge.Configure(segments);

            Assert.That(bridge.ActiveSegmentCount, Is.Zero);
            Assert.That(bridge.IsSegmentActive(0), Is.False);
            Assert.That(bridge.IsSegmentColliderEnabled(0), Is.False);
            Assert.That(bridge.ActivateNextSegment(), Is.True);
            Assert.That(bridge.ActiveSegmentCount, Is.EqualTo(1));
            Assert.That(bridge.IsSegmentActive(0), Is.True);
            Assert.That(bridge.IsSegmentColliderEnabled(0), Is.True);
            Assert.That(bridge.IsSegmentActive(1), Is.False);

            Assert.That(bridge.ActivateNextSegment(), Is.True);
            Assert.That(bridge.ActivateNextSegment(), Is.True);
            Assert.That(bridge.BridgeComplete, Is.True);
            Assert.That(bridge.ActiveSegmentCount, Is.EqualTo(3));
            Assert.That(bridge.StateRevision, Is.EqualTo(3));
        }

        [Test]
        public void InvariantStarBlock_RestoresCapturedPoseAndVelocity()
        {
            GameObject blockObject = Track("InvariantStarBlock");
            blockObject.transform.position = new Vector3(1f, 2f, 0f);
            Rigidbody2D body = blockObject.AddComponent<Rigidbody2D>();
            P11InvariantStarBlock2D block = blockObject
                .AddComponent<P11InvariantStarBlock2D>();
            block.Configure(body);

            body.position = new Vector2(7f, 8f);
            body.rotation = 35f;
            body.linearVelocity = new Vector2(4f, 2f);
            body.angularVelocity = 12f;

            Assert.That(block.RestoreNow(), Is.True);
            Assert.That(body.position, Is.EqualTo(new Vector2(1f, 2f)));
            Assert.That(body.rotation, Is.EqualTo(0f).Within(0.001f));
            Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
            Assert.That(body.angularVelocity, Is.EqualTo(0f));
            Assert.That(block.IsAtOrigin, Is.True);
            Assert.That(block.CorrectionCount, Is.EqualTo(1));
            Assert.That(block.ReturnFieldInvariant, Is.True);
        }

        [Test]
        public void GravityDial_RotatesAndAppliesLocalGravityOnlyWhileRegistered()
        {
            GameObject dialObject = Track("GravityDial");
            BoxCollider2D dialCollider =
                dialObject.AddComponent<BoxCollider2D>();
            P11GravityDial2D dial =
                dialObject.AddComponent<P11GravityDial2D>();
            dial.Configure(Vector2.down, 10f);

            GameObject bodyObject = Track("GravityBody");
            Rigidbody2D body = bodyObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 2f;

            Assert.That(dialCollider.isTrigger, Is.True);
            Assert.That(dial.Register(body), Is.True);
            Assert.That(dial.RegisteredBodyCount, Is.EqualTo(1));
            Assert.That(body.gravityScale, Is.EqualTo(0f));
            Assert.That(dial.RotateClockwise(), Is.True);
            Assert.That(dial.GravityDirection, Is.EqualTo(Vector2.left));
            Assert.That(dial.RotationCount, Is.EqualTo(1));

            Assert.That(dial.ApplyGravityStep(0.5f), Is.EqualTo(1));
            Assert.That(
                body.linearVelocity,
                Is.EqualTo(Vector2.left * 5f));
            Assert.That(dial.LastAppliedBodyCount, Is.EqualTo(1));

            Assert.That(dial.Unregister(body), Is.True);
            Assert.That(dial.RegisteredBodyCount, Is.Zero);
            Assert.That(body.gravityScale, Is.EqualTo(2f));
        }

        private P11AddressableParcel2D CreateParcel(
            string name,
            P11ParcelLabel label,
            Vector2 position)
        {
            GameObject parcelObject = Track(name);
            parcelObject.transform.position = position;
            parcelObject.AddComponent<Rigidbody2D>();
            parcelObject.AddComponent<BoxCollider2D>();
            P11AddressableParcel2D parcel = parcelObject
                .AddComponent<P11AddressableParcel2D>();
            parcel.Configure(label);
            return parcel;
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
