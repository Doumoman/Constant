#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NUnit.Framework;
using StarNight.Character.Live.Cameras;
using StarNight.Character.Live.Input;
using StarNight.Character.Live.Movement;
using StarNight.Character.Live.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace StarNight.Character.Tests.PlayMode.Rmap03
{
    [Category("RMAP03")]
    public sealed class CharacterLiveGrabPlayModeTests : InputTestFixture
    {
        private const string PlayerPrefabPath = "Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab";
        private const string TerrainTilePath = "Assets/_Game/Live/Prefabs/RMAP02/Tiles/RMAP02_Terrain.asset";
        private const string ScenePath =
            "Assets/_Game/Map/Scenes/MoonPalace/RMAP03/MoonPalaceCornerGrab_RMAP03.unity";

        private readonly List<GameObject> testOwnedRoots = new List<GameObject>();
        private readonly List<Scene> testOwnedScenes = new List<Scene>();
        private readonly List<AsyncOperation> pendingSceneUnloads = new List<AsyncOperation>();

        [UnityTearDown]
        public IEnumerator DestroyTestOwnedFixtures()
        {
            foreach (GameObject root in testOwnedRoots)
            {
                if (root != null)
                {
                    Object.Destroy(root);
                }
            }

            foreach (AsyncOperation operation in pendingSceneUnloads)
            {
                if (operation != null && !operation.isDone)
                {
                    yield return operation;
                }
            }

            foreach (Scene scene in testOwnedScenes)
            {
                if (scene.IsValid() && scene.isLoaded)
                {
                    AsyncOperation operation = SceneManager.UnloadSceneAsync(scene);
                    if (operation != null)
                    {
                        yield return operation;
                    }
                }
            }

            yield return null;
            Physics2D.SyncTransforms();
            testOwnedRoots.Clear();
            testOwnedScenes.Clear();
            pendingSceneUnloads.Clear();
            AssertNoRmap03FixtureLeaks();
        }

        [UnityTest]
        public IEnumerator RMAP03_ActualPlayer_GrabsStaticDestructibleAndMovingSafeCorners()
        {
            foreach (CharacterLiveGrabSurface.SurfaceKind kind in new[]
                     {
                         CharacterLiveGrabSurface.SurfaceKind.StaticSafe,
                         CharacterLiveGrabSurface.SurfaceKind.DestructibleSafe,
                         CharacterLiveGrabSurface.SurfaceKind.MovingSafe
                     })
            {
                GrabRun run = CreateRun(kind);
                try
                {
                    yield return ApproachUntilGrab(run);

                    Assert.IsTrue(run.Movement.IsGrabbing, kind + " must be a physical safe Grab case.");
                    Assert.IsTrue(run.Movement.HasSafeGrabContact);
                    Assert.AreEqual(new Vector2(0.72f, 0.9f), run.Player.BodyCollider.size);
                    Assert.AreEqual(Vector2.zero, run.Player.BodyCollider.offset);
                    Assert.IsNotNull(run.SurfaceCollider);
                    if (kind == CharacterLiveGrabSurface.SurfaceKind.StaticSafe)
                    {
                        Assert.IsInstanceOf<TilemapCollider2D>(run.SurfaceCollider);
                        Assert.IsNotNull(run.SurfaceCollider.GetComponent<CompositeCollider2D>());
                    }
                }
                finally
                {
                    ScheduleFixtureDestroy(run);
                }

                yield return FinishFixtureDestroy(run);
            }
        }

        [UnityTest]
        public IEnumerator RMAP03_ActualPlayer_DoesNotGrabForbiddenPhysicalOrDecorationSurfaces()
        {
            foreach (CharacterLiveGrabSurface.SurfaceKind kind in new[]
                     {
                         CharacterLiveGrabSurface.SurfaceKind.OneWay,
                         CharacterLiveGrabSurface.SurfaceKind.Hazard,
                         CharacterLiveGrabSurface.SurfaceKind.Crushing,
                         CharacterLiveGrabSurface.SurfaceKind.Decoration
                     })
            {
                GrabRun run = CreateRun(kind);
                try
                {
                    Vector2 startPosition = run.Player.Body.position;
                    float startY = run.Player.Body.position.y;
                    for (var index = 0; index < 4; index++)
                    {
                        yield return FeedFixed(run.Player, horizontal: run.ApproachDirection, down: false,
                            jumpPressed: false, jumpHeld: false);
                        Assert.IsFalse(run.Movement.IsGrabbing,
                            kind + " must remain non-grabbable after entering the real probe range.");
                    }

                    Assert.IsFalse(run.Movement.IsGrabbing, kind + " must not be a Grab anchor.");
                    if (run.SurfaceCollider != null)
                    {
                        Assert.IsTrue(run.SurfaceCollider.enabled,
                            "Forbidden classification must not disable its physical collision.");
                    }
                    if (kind == CharacterLiveGrabSurface.SurfaceKind.OneWay)
                    {
                        PlatformEffector2D effector = run.SurfaceCollider.GetComponent<PlatformEffector2D>();
                        Assert.IsNotNull(effector);
                        Assert.IsTrue(effector.useOneWay);
                        Assert.IsTrue(run.SurfaceCollider.usedByEffector);
                        Assert.IsNotNull(run.SurfaceCollider.GetComponent<CharacterLiveOneWayPlatform>());
                    }
                    Assert.Less(run.Player.Body.position.y, startY,
                        "Forbidden surface leaves the Player in normal physical fall.");
                    Assert.Greater(Vector2.Distance(startPosition, run.Player.Body.position), CollisionSkin,
                        "Forbidden fixture must observe normal physical movement after START.");
                }
                finally
                {
                    ScheduleFixtureDestroy(run);
                }

                yield return FinishFixtureDestroy(run);
            }
        }

        [UnityTest]
        public IEnumerator RMAP03_EntryAllowsDescentButSuppressesAscentAndDown()
        {
            GrabRun descent = CreateRun(CharacterLiveGrabSurface.SurfaceKind.StaticSafe);
            try
            {
                yield return ApproachUntilGrab(descent);
                Assert.IsTrue(descent.Movement.IsGrabbing,
                    "Horizontal/descent entry grabs the exposed corner.");
            }
            finally
            {
                ScheduleFixtureDestroy(descent);
            }
            yield return FinishFixtureDestroy(descent);

            GrabRun ascent = CreateRun(CharacterLiveGrabSurface.SurfaceKind.StaticSafe,
                VerticalFixtureState.Ascending);
            try
            {
                yield return FeedFixed(ascent.Player, 0f, false, false, false);
                float ascentStartY = ascent.Player.Body.position.y;
                yield return FeedFixed(ascent.Player, ascent.ApproachDirection, false, true, true);
                for (var index = 0; index < 4; index++)
                {
                    yield return FeedFixed(ascent.Player, ascent.ApproachDirection, false, false, true);
                }
                Assert.Greater(ascent.Player.Body.position.y, ascentStartY + CollisionSkin,
                    "The Player is physically ascending past the corner.");
                Assert.IsFalse(ascent.Movement.IsGrabbing,
                    "An upward jump must retain the existing jump/collision flow instead of auto-Grab.");
            }
            finally
            {
                ScheduleFixtureDestroy(ascent);
            }
            yield return FinishFixtureDestroy(ascent);

            GrabRun down = CreateRun(CharacterLiveGrabSurface.SurfaceKind.StaticSafe);
            try
            {
                float downStartY = down.Player.Body.position.y;
                for (var index = 0; index < 4; index++)
                {
                    yield return FeedFixed(down.Player, 1f, true, false, false);
                }
                Assert.IsFalse(down.Movement.IsGrabbing);
                Assert.Less(down.Player.Body.position.y, downStartY,
                    "Down held takes priority and leaves the Player falling.");
            }
            finally
            {
                ScheduleFixtureDestroy(down);
            }
            yield return FinishFixtureDestroy(down);
        }

        [UnityTest]
        public IEnumerator RMAP03_ActualInputSystem_DownSuppressesCornerGrab()
        {
            GrabRun run = CreateRun(CharacterLiveGrabSurface.SurfaceKind.StaticSafe);
            run.Player.InputSource.enabled = true;
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Press(keyboard.dKey);
            Press(keyboard.sKey);
            yield return null;
            yield return new WaitForFixedUpdate();

            Assert.IsFalse(run.Movement.IsGrabbing,
                "S/Down travels through the Input System snapshot and suppresses Grab on the real Player.");
            Object.Destroy(run.Root);
        }

        [UnityTest]
        public IEnumerator RMAP03_GrabHoldsThenUsesJumpAirControlAndReentryDelay()
        {
            GrabRun run = CreateRun(CharacterLiveGrabSurface.SurfaceKind.StaticSafe);
            try
            {
                yield return ApproachUntilGrab(run);
                Assert.IsTrue(run.Movement.IsGrabbing);
                Vector2 heldFeet = run.Player.Body.position;
                for (var index = 0; index < 90; index++)
                {
                    yield return FeedFixed(run.Player, 0f, false, false, false);
                }
                Assert.Less(Vector2.Distance(heldFeet, run.Player.Body.position), 0.02f,
                    "No-input Grab remains on the same anchor without a time limit.");

                yield return FeedFixed(run.Player, 0f, false, true, true);
                Assert.IsFalse(run.Movement.IsGrabbing, "Jump explicitly exits Grab.");
                float peak = run.Player.Body.position.y;
                float exitX = run.Player.Body.position.x;
                for (var index = 0; index < 20; index++)
                {
                    yield return FeedFixed(run.Player, 1f, false, false, true);
                    peak = Mathf.Max(peak, run.Player.Body.position.y);
                }
                Assert.Greater(peak - heldFeet.y, 1.10f,
                    "Grab jump uses the existing approximately 1.3-tile jump velocity.");
                Assert.Greater(run.Player.Body.position.x, exitX + 0.05f,
                    "Post-jump horizontal input is existing air control, not a wall-kick state.");
            }
            finally
            {
                ScheduleFixtureDestroy(run);
            }
            yield return FinishFixtureDestroy(run);

            GrabRun reentry = CreateRun(CharacterLiveGrabSurface.SurfaceKind.StaticSafe);
            try
            {
                yield return ApproachUntilGrab(reentry);
                Assert.IsTrue(reentry.Movement.IsGrabbing);
                yield return FeedFixed(reentry.Player, 0f, true, false, false);
                for (var index = 0; index < 5; index++)
                {
                    yield return FeedFixed(reentry.Player, 0f, false, false, false);
                }
                Assert.IsFalse(reentry.Movement.IsGrabbing,
                    "The 0.12s reentry delay preserves an explicit drop input.");
            }
            finally
            {
                ScheduleFixtureDestroy(reentry);
            }
            yield return FinishFixtureDestroy(reentry);
        }

        [UnityTest]
        public IEnumerator RMAP03_ActualPrefabGrabSpaceExitEscapesOnlyReleasedColliderAndRecontactsIt()
        {
            GrabRun run = CreateRun(CharacterLiveGrabSurface.SurfaceKind.StaticSafe);
            var trace = new StringBuilder("RMAP03_RECONTACT_TRACE\n");
            try
            {
                Assert.AreEqual(new Vector2(0.72f, 0.9f), run.Player.BodyCollider.size);
                Assert.AreEqual(Vector2.zero, run.Player.BodyCollider.offset);
                float timeoutAt = Time.time + 6f;

                yield return ApproachUntilGrab(run);
                Assert.IsTrue(run.Movement.IsGrabbing,
                    "The unmodified actual prefab capsule must enter Grab before Space exit.");
                Collider2D releasedCollider = PrivateCollider(run.Movement, "grabbedCollider");
                Assert.IsNotNull(releasedCollider);
                Bounds surfaceBounds = releasedCollider.bounds;
                Bounds capsuleBounds = run.Player.BodyCollider.bounds;
                float surfaceTop = surfaceBounds.max.y;
                float landingClearance = Physics2D.defaultContactOffset + CollisionSkin;
                float safeLandingMinX = surfaceBounds.min.x + capsuleBounds.extents.x
                    + landingClearance;
                float safeLandingMaxX = surfaceBounds.max.x - capsuleBounds.extents.x
                    - landingClearance;
                trace.Append("landingClearance=").Append(FormatFloat(landingClearance))
                    .Append(" safeLandingMinX=").Append(FormatFloat(safeLandingMinX))
                    .Append(" safeLandingMaxX=").Append(FormatFloat(safeLandingMaxX))
                    .Append('\n');
                Assert.LessOrEqual(safeLandingMinX, safeLandingMaxX,
                    "PLATFORM_TOO_NARROW_FOR_ACTUAL_CAPSULE");
                float landingTargetX = Mathf.Clamp(surfaceBounds.center.x,
                    safeLandingMinX, safeLandingMaxX);
                var airControlSettings = run.Movement.Settings.CreateAirControlSettings();
                float actualBrakeAcceleration = airControlSettings.AirAcceleration;
                int surfaceDirection = releasedCollider.bounds.center.x >
                    run.Player.BodyCollider.bounds.center.x ? 1 : -1;
                trace.Append("landingTargetX=").Append(FormatFloat(landingTargetX))
                    .Append(" initialBodyX=").Append(FormatFloat(run.Player.Body.position.x))
                    .Append(" actualBrakeAcceleration=")
                    .Append(FormatFloat(actualBrakeAcceleration))
                    .Append('\n');
                GrabPhysicsSnapshot grabbedSnapshot = CaptureGrabPhysicsSnapshot(
                    run, releasedCollider, "RECONTACT_GRABBED");
                AssertValidSeparatedGrabSnapshot(grabbedSnapshot);
                trace.Append("grab surfaceTop=").Append(FormatFloat(surfaceTop))
                    .Append(" surfaceDirection=").Append(surfaceDirection)
                    .Append(" overlapCount=").Append(grabbedSnapshot.Overlaps.Length)
                    .Append(" distance=").Append(FormatFloat(grabbedSnapshot.TargetDistance.distance))
                    .Append('\n');

                yield return FeedFixed(run.Player, 0f, false, true, true);
                float grabSpaceExitVelocityY = run.Movement.Velocity.y;
                Assert.IsFalse(run.Movement.IsGrabbing, "Fresh Space must end Grab.");
                Assert.Greater(grabSpaceExitVelocityY, 0f,
                    "Fresh Space must retain the existing positive Jump impulse.");
                Collider2D upwardEscapeCollider =
                    PrivateCollider(run.Movement, "grabJumpEscapeCollider");
                Assert.IsTrue(upwardEscapeCollider == null || upwardEscapeCollider == releasedCollider,
                    "Only the just-released collider may occupy the upward escape slot.");
                Assert.IsFalse(Physics2D.GetIgnoreCollision(
                        run.Player.BodyCollider, releasedCollider),
                    "Sweep-local upward escape must not leave IgnoreCollision enabled.");
                trace.Append("GRAB_SPACE_EXIT vy=").Append(FormatFloat(grabSpaceExitVelocityY))
                    .Append(" upwardEscape=")
                    .Append(upwardEscapeCollider != null ? upwardEscapeCollider.name : "NONE")
                    .Append('\n');
                AppendSafeLandingStep(trace, "GRAB_SPACE_EXIT", run, releasedCollider,
                    safeLandingMinX, safeLandingMaxX, landingTargetX, surfaceTop,
                    actualBrakeAcceleration, 0f);

                while (Time.time < timeoutAt &&
                    run.Player.BodyCollider.bounds.min.y <= surfaceTop)
                {
                    yield return FeedFixed(run.Player, 0f, false, false, true);
                    AppendSafeLandingStep(trace, "CLEAR_SURFACE_TOP", run, releasedCollider,
                        safeLandingMinX, safeLandingMaxX, landingTargetX, surfaceTop,
                        actualBrakeAcceleration, 0f);
                }

                Assert.Greater(run.Player.BodyCollider.bounds.min.y, surfaceTop,
                    "The actual capsule must rise completely above the released surface top.");
                Assert.Greater(run.Movement.Velocity.y, 0f,
                    "The actual capsule must clear the surface top before descent begins.");
                Assert.IsNull(PrivateCollider(run.Movement, "grabJumpEscapeCollider"),
                    "Bounds separation must end the upward escape exclusion.");
                Assert.IsFalse(Physics2D.GetIgnoreCollision(
                        run.Player.BodyCollider, releasedCollider),
                    "The released collider must be restored before recontact.");
                trace.Append("aboveSurface capsuleMinY=")
                    .Append(FormatFloat(run.Player.BodyCollider.bounds.min.y))
                    .Append(" ignored=False\n");

                bool descentObserved = false;
                while (Time.time < timeoutAt && !run.Movement.IsGroundedNow)
                {
                    float bodyXBeforeStep = run.Player.Body.position.x;
                    float horizontalInput = ResolveSafeLandingHorizontalInput(
                        bodyXBeforeStep,
                        run.Movement.Velocity.x,
                        safeLandingMinX,
                        safeLandingMaxX,
                        landingTargetX,
                        actualBrakeAcceleration);
                    bool holdJump = run.Movement.Velocity.y > 0f;

                    yield return FeedFixed(run.Player, horizontalInput, false, false, holdJump);
                    AppendSafeLandingStep(trace, "AIR_STEER", run, releasedCollider,
                        safeLandingMinX, safeLandingMaxX, landingTargetX, surfaceTop,
                        actualBrakeAcceleration, horizontalInput);

                    if (!descentObserved && run.Movement.Velocity.y <= 0f)
                    {
                        descentObserved = true;
                        Assert.Greater(run.Player.BodyCollider.bounds.min.y, surfaceTop,
                            "Descent must begin only after the capsule has cleared the surface top.");
                        Assert.IsFalse(Physics2D.GetIgnoreCollision(
                                run.Player.BodyCollider, releasedCollider),
                            "Collision ignore must already be restored when descent begins.");
                        Assert.Greater(run.Player.Body.position.y, surfaceTop,
                            "The Player must begin descending above the released surface.");
                        Assert.Greater(HorizontalProjectionOverlap(
                                run.Player.BodyCollider.bounds, releasedCollider.bounds), 0f,
                            "The Player must begin descending over the released surface projection.");
                        trace.Append("descentStart capsuleMinY=")
                            .Append(FormatFloat(run.Player.BodyCollider.bounds.min.y))
                            .Append(" surfaceTop=").Append(FormatFloat(surfaceTop))
                            .Append(" bodyX=").Append(FormatFloat(run.Player.Body.position.x))
                            .Append(" velocityY=").Append(FormatFloat(run.Movement.Velocity.y))
                            .Append(" collisionIgnore=False\n");
                    }
                }

                Physics2D.SyncTransforms();
                float horizontalOverlap = HorizontalProjectionOverlap(
                    run.Player.BodyCollider.bounds, releasedCollider.bounds);
                Collider2D landingSupport = FindLandingSupport(run, releasedCollider);
                Assert.IsFalse(Physics2D.GetIgnoreCollision(
                        run.Player.BodyCollider, releasedCollider),
                    "The released collider must not remain ignored at landing.");
                Assert.IsTrue(run.Movement.IsGroundedNow,
                    "The Player must naturally land before the bounded timeout.");
                Assert.IsTrue(descentObserved,
                    "The Player must begin descending only after clearing the surface top.");
                Assert.That(run.Player.Body.position.x,
                    Is.InRange(safeLandingMinX, safeLandingMaxX),
                    "The landed capsule center must remain inside the actual safe landing range.");
                Assert.That(run.Player.BodyCollider.bounds.min.y,
                    Is.EqualTo(surfaceTop).Within(0.04f));
                Assert.Greater(horizontalOverlap, 0f,
                    "The landed capsule and released surface must overlap on the X projection.");
                Assert.AreSame(releasedCollider, landingSupport,
                    "The landing support must be the same collider that supplied Grab.");
                trace.Append("landed grounded=True capsuleMinY=")
                    .Append(FormatFloat(run.Player.BodyCollider.bounds.min.y))
                    .Append(" surfaceTop=").Append(FormatFloat(surfaceTop))
                    .Append(" bodyX=").Append(FormatFloat(run.Player.Body.position.x))
                    .Append(" projectionOverlap=").Append(FormatFloat(horizontalOverlap))
                    .Append(" support=").Append(landingSupport != null ? landingSupport.name : "NONE")
                    .Append('\n');
            }
            finally
            {
                Debug.Log(trace.ToString());
                ScheduleFixtureDestroy(run);
            }
        }

        [UnityTest]
        public IEnumerator RMAP03_GrabSpaceExitStillCollidesWithASeparateSolidCeiling()
        {
            GrabRun run = CreateRun(CharacterLiveGrabSurface.SurfaceKind.StaticSafe);
            BoxCollider2D ceiling = CreateCeiling(run);
            yield return ApproachUntilGrab(run);
            Assert.IsTrue(run.Movement.IsGrabbing);

            yield return FeedFixed(run.Player, 1f, false, true, true);
            Assert.IsFalse(run.Movement.IsGrabbing);
            Assert.Greater(run.Movement.Velocity.y, 0f,
                "The released Grab collider alone is excluded from the first upward sweep.");

            bool ceilingBlocked = false;
            for (var index = 0; index < 20; index++)
            {
                yield return FeedFixed(run.Player, 0f, false, false, true);
                if (Mathf.Abs(run.Movement.Velocity.y) < 0.001f)
                {
                    ceilingBlocked = true;
                    break;
                }
            }

            Assert.IsTrue(ceilingBlocked, "A distinct SOLID ceiling must still stop the Grab jump.");
            Assert.LessOrEqual(run.Player.BodyCollider.bounds.max.y, ceiling.bounds.min.y + 0.03f);
            Object.Destroy(run.Root);
        }

        [UnityTest]
        public IEnumerator RMAP03_DownAndHorizontalGrabExitsRemainNonJumpExits()
        {
            GrabRun down = CreateRun(CharacterLiveGrabSurface.SurfaceKind.StaticSafe);
            try
            {
                yield return ApproachUntilGrab(down);
                Assert.IsTrue(down.Movement.IsGrabbing);
                yield return FeedFixed(down.Player, 0f, true, false, false);
                Assert.IsFalse(down.Movement.IsGrabbing);
                Assert.Less(down.Movement.Velocity.y, 0f);
                Assert.IsNull(PrivateCollider(down.Movement, "grabJumpEscapeCollider"));
            }
            finally
            {
                ScheduleFixtureDestroy(down);
            }
            yield return FinishFixtureDestroy(down);

            GrabRun horizontal = CreateRun(CharacterLiveGrabSurface.SurfaceKind.StaticSafe);
            try
            {
                yield return ApproachUntilGrab(horizontal);
                Assert.IsTrue(horizontal.Movement.IsGrabbing);
                yield return FeedFixed(horizontal.Player, -1f, false, false, false);
                Assert.IsFalse(horizontal.Movement.IsGrabbing);
                Assert.Less(horizontal.Movement.Velocity.y, 0f);
                Assert.IsNull(PrivateCollider(horizontal.Movement, "grabJumpEscapeCollider"));
            }
            finally
            {
                ScheduleFixtureDestroy(horizontal);
            }
            yield return FinishFixtureDestroy(horizontal);
        }

        [UnityTest]
        public IEnumerator RMAP03_MovingSafeSolid_CarriesGrabAndDangerTransitionReleases()
        {
            GrabRun run = CreateRun(CharacterLiveGrabSurface.SurfaceKind.MovingSafe);
            yield return ApproachUntilGrab(run);
            Assert.IsTrue(run.Movement.IsGrabbing);
            float platformStart = GrabPhysicsOwnerX(run.SurfaceCollider);
            float playerStart = run.Player.Body.position.x;
            for (var index = 0; index < 30; index++)
            {
                yield return FeedFixed(run.Player, 0f, false, false, false);
            }

            float surfaceDeltaX = GrabPhysicsOwnerX(run.SurfaceCollider) - platformStart;
            float playerDeltaX = run.Player.Body.position.x - playerStart;
            Assert.Greater(surfaceDeltaX, 0.30f,
                "The fixture moves a Kinematic Rigidbody2D/Collider2D along its authored path.");
            Assert.That(playerDeltaX, Is.EqualTo(surfaceDeltaX).Within(0.03f),
                "The Player delta must match its grabbed physics owner's delta.");
            run.Surface.SetDangerous(true);
            yield return FeedFixed(run.Player, 0f, false, false, false);
            Assert.IsFalse(run.Movement.IsGrabbing,
                "A moving surface danger transition releases Grab immediately.");
            Object.Destroy(run.Root);
        }

        [UnityTest]
        public IEnumerator RMAP03_SavedScene_ContainsPlayerSurfaceClassificationAndMovingCollider()
        {
            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene loadedScene = default;
            AsyncOperation unloadOperation = null;
            try
            {
                AsyncOperation loadOperation = EditorSceneManager.LoadSceneAsyncInPlayMode(
                    ScenePath, new LoadSceneParameters(LoadSceneMode.Additive));
                yield return loadOperation;
                loadedScene = SceneManager.GetSceneByPath(ScenePath);
                Assert.IsTrue(loadedScene.IsValid() && loadedScene.isLoaded,
                    "RMAP03 SavedScene must load additively.");
                testOwnedScenes.Add(loadedScene);
                Assert.IsTrue(SceneManager.SetActiveScene(loadedScene));
                yield return null;
                yield return new WaitForFixedUpdate();

                CharacterLivePlayerRig player = FindFirstInScene<CharacterLivePlayerRig>(
                    loadedScene);
                CharacterLiveGrabSurface moving = FindFirstInScene<CharacterLiveGrabSurface>(
                    loadedScene);
                CharacterLiveGrabMovingSolid mover = FindFirstInScene<
                    CharacterLiveGrabMovingSolid>(loadedScene);
                TilemapCollider2D tilemapCollider = FindFirstInScene<TilemapCollider2D>(
                    loadedScene);
                CharacterLiveCameraFollowDriver camera = FindFirstInScene<
                    CharacterLiveCameraFollowDriver>(loadedScene);

                Assert.IsNotNull(player);
                Assert.IsNotNull(moving);
                Assert.IsNotNull(mover);
                Assert.IsNotNull(tilemapCollider);
                Assert.IsNotNull(camera);
                Assert.AreEqual(new Vector2(0.4f, 0.8f), player.BodyCollider.size);
                Assert.IsNotNull(tilemapCollider.GetComponent<CharacterLiveGrabSurface>());
                Assert.IsNotNull(tilemapCollider.GetComponent<CompositeCollider2D>());
                Assert.IsNotNull(mover.GetComponent<Rigidbody2D>());
                Assert.AreEqual(new Vector2(12f, 8f), camera.VisibleWorldSize);
            }
            finally
            {
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }

                unloadOperation = BeginSceneUnload(loadedScene);
            }

            yield return FinishSceneUnload(loadedScene, unloadOperation);
        }

        private GrabRun CreateRun(
            CharacterLiveGrabSurface.SurfaceKind kind,
            VerticalFixtureState verticalState = VerticalFixtureState.Descending,
            int approachDirection = 1,
            bool includeGround = true)
        {
            AssertNoRmap03FixtureLeaks();
            Assert.That(approachDirection, Is.EqualTo(1).Or.EqualTo(-1));
            var run = new GrabRun();
            run.Root = new GameObject("RMAP03_ActualGrabFixture", typeof(Grid));
            testOwnedRoots.Add(run.Root);
            if (includeGround)
            {
                run.GroundCollider = CreateGround(run.Root.transform);
            }
            if (kind == CharacterLiveGrabSurface.SurfaceKind.StaticSafe)
            {
                CreateStaticSafeTilemap(run);
            }
            else
            {
                CreateBoxSurface(run, kind);
            }

            TilemapCollider2D tilemapCollider = run.SurfaceCollider as TilemapCollider2D;
            if (tilemapCollider != null && tilemapCollider.hasTilemapChanges)
            {
                tilemapCollider.ProcessTilemapChanges();
            }
            Physics2D.SyncTransforms();
            run.SurfacePhysicsCollider = ResolveSurfacePhysicsCollider(run.SurfaceCollider);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            var playerHost = new GameObject("RMAP03_ActualPlayer");
            playerHost.transform.SetParent(run.Root.transform, false);
            playerHost.SetActive(false);
            GameObject playerObject = Object.Instantiate(prefab, playerHost.transform);
            run.Player = playerObject.GetComponent<CharacterLivePlayerRig>();
            run.Movement = playerObject.GetComponent<CharacterLiveMovementDriver>();
            run.Movement.ConfigureRmap02(1);
            playerHost.SetActive(true);
            Physics2D.SyncTransforms();
            PlaceFromActualBounds(run, kind, verticalState, approachDirection);
            run.Movement.ResetMotion();
            run.Player.InputSource.enabled = false;
            return run;
        }

        private static BoxCollider2D CreateGround(Transform parent)
        {
            var ground = new GameObject("PhysicalGround", typeof(BoxCollider2D));
            ground.transform.SetParent(parent, false);
            ground.transform.position = new Vector2(3.75f, 0.5f);
            BoxCollider2D collider = ground.GetComponent<BoxCollider2D>();
            collider.size = new Vector2(7.5f, 1f);
            return collider;
        }

        private static void CreateStaticSafeTilemap(GrabRun run)
        {
            var surface = new GameObject("StaticSafeTilemap", typeof(Tilemap), typeof(TilemapCollider2D),
                typeof(Rigidbody2D), typeof(CompositeCollider2D), typeof(CharacterLiveGrabSurface));
            surface.transform.SetParent(run.Root.transform, false);
            Tilemap tilemap = surface.GetComponent<Tilemap>();
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(TerrainTilePath);
            tilemap.SetTile(new Vector3Int(8, 1, 0), tile);
            tilemap.SetTile(new Vector3Int(8, 2, 0), tile);
            TilemapCollider2D collider = surface.GetComponent<TilemapCollider2D>();
            collider.compositeOperation = Collider2D.CompositeOperation.Merge;
            surface.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            run.Surface = surface.GetComponent<CharacterLiveGrabSurface>();
            run.SurfaceTransform = surface.transform;
            run.Surface.Configure(CharacterLiveGrabSurface.SurfaceKind.StaticSafe);
            run.SurfaceCollider = collider;
        }

        private static void CreateBoxSurface(GrabRun run, CharacterLiveGrabSurface.SurfaceKind kind)
        {
            var surface = new GameObject("RMAP03_" + kind);
            surface.transform.SetParent(run.Root.transform, false);
            surface.transform.position = new Vector2(8.5f, 2f);
            run.SurfaceTransform = surface.transform;
            if (kind != CharacterLiveGrabSurface.SurfaceKind.Decoration)
            {
                run.Surface = surface.AddComponent<CharacterLiveGrabSurface>();
                run.Surface.Configure(kind);
                run.SurfaceCollider = surface.AddComponent<BoxCollider2D>();
                run.SurfaceCollider.GetComponent<BoxCollider2D>().size = new Vector2(1f, 2f);
                if (kind == CharacterLiveGrabSurface.SurfaceKind.OneWay)
                {
                    PlatformEffector2D effector = surface.AddComponent<PlatformEffector2D>();
                    effector.useOneWay = true;
                    effector.surfaceArc = 170f;
                    run.SurfaceCollider.usedByEffector = true;
                    var oneWay = surface.AddComponent<CharacterLiveOneWayPlatform>();
                    oneWay.Configure(run.SurfaceCollider);
                }
            }

            if (kind == CharacterLiveGrabSurface.SurfaceKind.MovingSafe)
            {
                Rigidbody2D body = surface.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                var mover = surface.AddComponent<CharacterLiveGrabMovingSolid>();
                mover.Configure(body, new Vector2(8.5f, 2f), new Vector2(11.5f, 2f), 1.5f);
            }
        }

        private static IEnumerator FeedFixed(
            CharacterLivePlayerRig player,
            float horizontal,
            bool down,
            bool jumpPressed,
            bool jumpHeld)
        {
            var jump = new CharacterLiveButtonFrame(jumpPressed, false, jumpHeld);
            var idle = new CharacterLiveButtonFrame(false, false, false);
            player.InputSource.Adapter.AccumulateFrame(horizontal, down, jump, idle, idle, idle);
            yield return new WaitForFixedUpdate();
        }

        private static IEnumerator ApproachUntilGrab(GrabRun run)
        {
            Vector2 startPosition = run.Player.Body.position;
            for (var index = 0; index < 8 && !run.Movement.IsGrabbing; index++)
            {
                yield return FeedFixed(run.Player, run.ApproachDirection, false, false, false);
            }

            Assert.Greater(Vector2.Distance(startPosition, run.Player.Body.position), CollisionSkin,
                "The fixture must approach the surface through real MoveX physics after START.");
            Assert.IsTrue(run.Movement.IsGrabbing,
                "The actual Grab probe must detect the surface after the bounds-derived approach.");
            AssertGrabSeparation(run);
        }

        private static IEnumerator DiagnoseGrabMovePosition(
            GrabRun run,
            string label,
            bool observeMovingOwner)
        {
            Debug.Log(BuildGrabProbeTrace(run, label + "_START"));
            GrabPhysicsSnapshot beforeGrab = CaptureGrabPhysicsSnapshot(
                run, run.SurfacePhysicsCollider, label + "_BEFORE_GRAB");
            GrabPhysicsSnapshot grabObserved = null;

            for (var index = 0; index < 8 && !run.Movement.IsGrabbing; index++)
            {
                beforeGrab = CaptureGrabPhysicsSnapshot(
                    run, run.SurfacePhysicsCollider, label + "_BEFORE_GRAB_STEP_" + index);
                yield return FeedFixed(run.Player, run.ApproachDirection, false, false, false);
                if (run.Movement.IsGrabbing)
                {
                    Collider2D grabbedAtObservation = PrivateCollider(run.Movement, "grabbedCollider");
                    grabObserved = CaptureGrabPhysicsSnapshot(
                        run, grabbedAtObservation, label + "_GRAB_OBSERVED");
                }
            }

            Assert.IsTrue(run.Movement.IsGrabbing,
                label + " must enter Grab in its isolated fixture.");
            Assert.IsNotNull(grabObserved);
            Collider2D grabbed = PrivateCollider(run.Movement, "grabbedCollider");
            Assert.IsNotNull(grabbed);
            Assert.AreSame(run.SurfacePhysicsCollider, grabbed,
                label + " must Grab the intended physical collider.");

            Debug.Log(BuildGrabMovePositionTrace(run, beforeGrab, grabObserved,
                null, label, "MOVE_POSITION_SUBMITTED"));

            // The Grab-entering FixedUpdate submits HoldGrabAtAnchor's MovePosition.
            // One additional fixed update is required before judging the resulting
            // collider separation.
            yield return FeedFixed(run.Player, 0f, false, false, false);
            Physics2D.SyncTransforms();
            GrabPhysicsSnapshot nextPhysicsStep = CaptureGrabPhysicsSnapshot(
                run, grabbed, label + "_NEXT_PHYSICS_STEP");

            string classification = ClassifyGrabOverlap(run, grabObserved, nextPhysicsStep);
            Debug.Log(BuildGrabMovePositionTrace(run, beforeGrab, grabObserved,
                nextPhysicsStep, label, classification));
            Assert.AreEqual("E_VALID_SEPARATED_GRAB", classification,
                label + " must remain physically separated at Grab observation and after the next physics step.");
            AssertValidSeparatedGrabSnapshot(grabObserved);
            AssertValidSeparatedGrabSnapshot(nextPhysicsStep);

            if (observeMovingOwner)
            {
                Transform anchorOwner = PrivateTransform(run.Movement, "grabAnchorTransform");
                Transform attachedOwner = grabbed.attachedRigidbody != null
                    ? grabbed.attachedRigidbody.transform
                    : null;
                Vector2 surfaceBefore = GrabPhysicsOwnerPosition(grabbed);
                Vector2 playerBefore = run.Player.Body.position;
                ColliderDistance2D distanceBefore = run.Player.BodyCollider.Distance(grabbed);

                for (var index = 0; index < 12; index++)
                {
                    yield return FeedFixed(run.Player, 0f, false, false, false);
                }

                Physics2D.SyncTransforms();
                Vector2 surfaceAfter = GrabPhysicsOwnerPosition(grabbed);
                Vector2 playerAfter = run.Player.Body.position;
                ColliderDistance2D distanceAfter = run.Player.BodyCollider.Distance(grabbed);
                Vector2 surfaceDelta = surfaceAfter - surfaceBefore;
                Vector2 playerDelta = playerAfter - playerBefore;
                Debug.Log(new StringBuilder()
                    .Append("RMAP03_MOVING_OWNER_TRACE\n")
                    .Append("anchorOwner=").Append(anchorOwner != null ? anchorOwner.name : "NULL")
                    .Append(" attachedRigidbodyOwner=")
                    .Append(attachedOwner != null ? attachedOwner.name : "NULL").Append('\n')
                    .Append("surfaceBefore=").Append(FormatVector(surfaceBefore))
                    .Append(" surfaceAfter=").Append(FormatVector(surfaceAfter)).Append('\n')
                    .Append("playerBefore=").Append(FormatVector(playerBefore))
                    .Append(" playerAfter=").Append(FormatVector(playerAfter)).Append('\n')
                    .Append("surfaceDelta=").Append(FormatVector(surfaceDelta))
                    .Append(" playerDelta=").Append(FormatVector(playerDelta)).Append('\n')
                    .Append("distanceBefore=").Append(FormatColliderDistance(distanceBefore))
                    .Append("\ndistanceAfter=").Append(FormatColliderDistance(distanceAfter))
                    .ToString());

                Assert.IsNotNull(attachedOwner,
                    "MOVING_SAFE must be owned by an attached Rigidbody2D transform.");
                Assert.AreSame(attachedOwner, anchorOwner,
                    "The moving Grab anchor owner must be the attached Rigidbody2D transform.");
                Assert.IsTrue(run.Movement.IsGrabbing,
                    "MOVING_SAFE must remain grabbed while its physics owner moves.");
                Assert.Greater(surfaceDelta.x, 0.30f,
                    "The isolated MOVING_SAFE physics owner must move along its authored path.");
                Assert.That(playerDelta.x, Is.EqualTo(surfaceDelta.x).Within(0.03f),
                    "The isolated MOVING_SAFE Player delta must match the surface delta.");

                run.Surface.SetDangerous(true);
                yield return FeedFixed(run.Player, 0f, false, false, false);
                Assert.IsFalse(run.Movement.IsGrabbing,
                    "A MOVING_SAFE danger transition must release Grab immediately.");
            }
        }

        private static GrabPhysicsSnapshot CaptureGrabPhysicsSnapshot(
            GrabRun run,
            Collider2D target,
            string label)
        {
            Physics2D.SyncTransforms();
            CapsuleCollider2D capsule = run.Player.BodyCollider;
            Bounds targetBounds = target != null ? target.bounds : run.SurfaceBounds;
            float requiredSideDistance = run.Movement.IsGrabbing
                ? PrivateFloat(run.Movement, "grabRequiredSideDistance")
                : CalculateRequiredGrabSideDistance(run);
            Vector2 corner = run.Movement.IsGrabbing && run.Movement.HasSafeGrabContact
                ? run.Movement.LastSafeGrabAnchor
                : new Vector2(run.ApproachDirection > 0 ? targetBounds.min.x : targetBounds.max.x,
                    targetBounds.max.y);
            Vector2 desiredFeet = corner + new Vector2(
                -run.ApproachDirection * requiredSideDistance,
                -run.Movement.Settings.GrabHangOffset);
            var overlaps = new Collider2D[32];
            var filter = new ContactFilter2D();
            filter.SetLayerMask(run.Movement.Settings.SolidLayers);
            filter.useTriggers = false;
            int overlapCount = Physics2D.OverlapCollider(capsule, filter, overlaps);
            var overlapList = new List<Collider2D>();
            for (var index = 0; index < overlapCount; index++)
            {
                if (overlaps[index] != null)
                {
                    overlapList.Add(overlaps[index]);
                }
            }

            return new GrabPhysicsSnapshot
            {
                Label = label,
                BodyPosition = run.Player.Body.position,
                CapsuleBounds = capsule.bounds,
                TargetBounds = targetBounds,
                DesiredFeet = desiredFeet,
                RequiredSideDistance = requiredSideDistance,
                TargetDistance = target != null ? capsule.Distance(target) : default,
                Overlaps = overlapList.ToArray(),
                Target = target
            };
        }

        private static string BuildGrabMovePositionTrace(
            GrabRun run,
            GrabPhysicsSnapshot beforeGrab,
            GrabPhysicsSnapshot grabObserved,
            GrabPhysicsSnapshot nextPhysicsStep,
            string label,
            string classification)
        {
            var trace = new StringBuilder();
            trace.Append("RMAP03_MOVE_POSITION_TRACE ").Append(label).Append('\n');
            AppendGrabPhysicsSnapshot(trace, beforeGrab);
            AppendGrabPhysicsSnapshot(trace, grabObserved);
            if (nextPhysicsStep != null)
            {
                AppendGrabPhysicsSnapshot(trace, nextPhysicsStep);
            }
            trace.Append("holdMovePositionSubmitted=").Append(run.Movement.IsGrabbing).Append('\n');
            trace.Append("classification=").Append(classification);
            return trace.ToString();
        }

        private static void AppendGrabPhysicsSnapshot(
            StringBuilder trace,
            GrabPhysicsSnapshot snapshot)
        {
            trace.Append("snapshot=").Append(snapshot.Label).Append('\n')
                .Append("  bodyPosition=").Append(FormatVector(snapshot.BodyPosition)).Append('\n')
                .Append("  capsuleMin=").Append(FormatVector(snapshot.CapsuleBounds.min))
                .Append(" capsuleMax=").Append(FormatVector(snapshot.CapsuleBounds.max)).Append('\n')
                .Append("  grabbedMin=").Append(FormatVector(snapshot.TargetBounds.min))
                .Append(" grabbedMax=").Append(FormatVector(snapshot.TargetBounds.max)).Append('\n')
                .Append("  desiredFeet=").Append(FormatVector(snapshot.DesiredFeet))
                .Append(" requiredSideDistance=")
                .Append(FormatFloat(snapshot.RequiredSideDistance)).Append('\n')
                .Append("  bodyMinusDesired=")
                .Append(FormatVector(snapshot.BodyPosition - snapshot.DesiredFeet)).Append('\n')
                .Append("  colliderDistance=")
                .Append(FormatColliderDistance(snapshot.TargetDistance)).Append('\n')
                .Append("  overlaps=").Append(snapshot.Overlaps.Length).Append('\n');
            foreach (Collider2D overlap in snapshot.Overlaps)
            {
                trace.Append("    name=").Append(overlap.name)
                    .Append(" instanceId=").Append(overlap.GetInstanceID())
                    .Append(" grabbed=").Append(overlap == snapshot.Target)
                    .Append(" support=").Append(overlap.name == "PhysicalGround")
                    .Append('\n');
            }
        }

        private static string ClassifyGrabOverlap(
            GrabRun run,
            GrabPhysicsSnapshot grabObserved,
            GrabPhysicsSnapshot nextPhysicsStep)
        {
            if (HasRmap03FixtureLeaks(run.Root))
            {
                return "D_FIXTURE_LEAK";
            }

            if (HasUnrelatedOverlap(grabObserved) || HasUnrelatedOverlap(nextPhysicsStep))
            {
                return "C_UNRELATED_COLLIDER_IN_OVERLAP";
            }

            if (IsValidSeparatedGrabSnapshot(grabObserved) &&
                IsValidSeparatedGrabSnapshot(nextPhysicsStep))
            {
                return "E_VALID_SEPARATED_GRAB";
            }

            bool observedTargetOverlap = HasTargetOverlap(grabObserved);
            bool nextTargetOverlap = HasTargetOverlap(nextPhysicsStep);
            if (observedTargetOverlap && !nextTargetOverlap)
            {
                return "A_ASSERTED_BEFORE_MOVEPOSITION_APPLIED";
            }

            if (nextTargetOverlap || observedTargetOverlap ||
                grabObserved.TargetDistance.distance <= 0f ||
                nextPhysicsStep.TargetDistance.isOverlapped ||
                nextPhysicsStep.TargetDistance.distance <= 0f)
            {
                return "B_ANCHOR_WORLD_GAP_ERROR";
            }

            Assert.Fail(run.Surface.name +
                " produced no overlap at Grab observation or after the next physics step; " +
                "none of A-E applies to the isolated trace.");
            return null;
        }

        private static bool HasTargetOverlap(GrabPhysicsSnapshot snapshot)
        {
            if (snapshot.TargetDistance.isOverlapped || snapshot.TargetDistance.distance < 0f)
            {
                return true;
            }

            foreach (Collider2D overlap in snapshot.Overlaps)
            {
                if (overlap == snapshot.Target)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasUnrelatedOverlap(GrabPhysicsSnapshot snapshot)
        {
            foreach (Collider2D overlap in snapshot.Overlaps)
            {
                if (overlap != snapshot.Target)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsValidSeparatedGrabSnapshot(GrabPhysicsSnapshot snapshot)
        {
            return snapshot != null && snapshot.Overlaps.Length == 0 &&
                !snapshot.TargetDistance.isOverlapped && snapshot.TargetDistance.distance > 0f;
        }

        private static void AssertValidSeparatedGrabSnapshot(GrabPhysicsSnapshot snapshot)
        {
            Assert.IsTrue(IsValidSeparatedGrabSnapshot(snapshot),
                snapshot.Label + " must have positive Collider2D.Distance and zero overlaps.");
            float boundsSeparation = snapshot.BodyPosition.x < snapshot.TargetBounds.center.x
                ? snapshot.TargetBounds.min.x - snapshot.CapsuleBounds.max.x
                : snapshot.CapsuleBounds.min.x - snapshot.TargetBounds.max.x;
            Assert.That(boundsSeparation, Is.GreaterThanOrEqualTo(Physics2D.defaultContactOffset),
                snapshot.Label + " must preserve at least Physics2D.defaultContactOffset in bounds.");
        }

        private static void PlaceFromActualBounds(
            GrabRun run,
            CharacterLiveGrabSurface.SurfaceKind kind,
            VerticalFixtureState verticalState,
            int approachDirection)
        {
            CapsuleCollider2D capsule = run.Player.BodyCollider;
            Bounds capsuleBounds = capsule.bounds;
            Bounds surfaceBounds = GetSurfaceBounds(run, kind);
            Vector2 bodyPosition = run.Player.Body.position;
            Vector2 boundsCenterOffset = (Vector2)capsuleBounds.center - bodyPosition;
            float probeGap = run.Movement.Settings.GrabProbeDistance * 0.5f;
            Assert.Greater(probeGap, CollisionSkin);
            Assert.Less(probeGap, run.Movement.Settings.GrabProbeDistance - CollisionSkin);

            float bodyX;
            if (approachDirection > 0)
            {
                float centerX = surfaceBounds.min.x - capsuleBounds.extents.x
                    - CollisionSkin - probeGap;
                bodyX = centerX - boundsCenterOffset.x;
            }
            else
            {
                float centerX = surfaceBounds.max.x + capsuleBounds.extents.x
                    + CollisionSkin + probeGap;
                bodyX = centerX - boundsCenterOffset.x;
            }

            float hangBodyY = surfaceBounds.max.y - run.Movement.Settings.GrabHangOffset;
            float bodyY;
            if (verticalState == VerticalFixtureState.Ascending)
            {
                float bottomOfGrabWindow = hangBodyY - run.Movement.Settings.GrabVerticalWindow + CollisionSkin;
                float groundedBodyY = run.GroundCollider.bounds.max.y + capsuleBounds.extents.y
                    - boundsCenterOffset.y + CollisionSkin;
                bodyY = Mathf.Max(bottomOfGrabWindow, groundedBodyY);
            }
            else
            {
                float descendingOffset = Mathf.Min(
                    run.Movement.Settings.GrabVerticalWindow * 0.25f,
                    capsuleBounds.size.y * 0.125f);
                bodyY = hangBodyY + descendingOffset;
            }

            run.Player.Body.position = new Vector2(bodyX, bodyY);
            run.ApproachDirection = approachDirection;
            run.SurfaceBounds = surfaceBounds;
            Physics2D.SyncTransforms();
            AssertFixtureStart(run, kind != CharacterLiveGrabSurface.SurfaceKind.Decoration);
        }

        private static Bounds GetSurfaceBounds(
            GrabRun run,
            CharacterLiveGrabSurface.SurfaceKind kind)
        {
            if (run.SurfaceCollider != null)
            {
                Assert.IsNotNull(run.SurfacePhysicsCollider);
                return run.SurfacePhysicsCollider.bounds;
            }

            Assert.AreEqual(CharacterLiveGrabSurface.SurfaceKind.Decoration, kind);
            return new Bounds(run.SurfaceTransform.position, new Vector3(1f, 2f, 0f));
        }

        private static Collider2D ResolveSurfacePhysicsCollider(Collider2D authoredCollider)
        {
            if (authoredCollider == null)
            {
                return null;
            }

            CompositeCollider2D composite = authoredCollider.GetComponent<CompositeCollider2D>();
            return authoredCollider.compositeOperation != Collider2D.CompositeOperation.None && composite != null
                ? composite
                : authoredCollider;
        }

        private static void AssertFixtureStart(GrabRun run, bool hasPhysicalSurface)
        {
            CapsuleCollider2D capsule = run.Player.BodyCollider;
            Assert.AreEqual(new Vector2(0.72f, 0.9f), capsule.size,
                "RMAP03 fixtures must use the instantiated CharacterLivePlayer capsule.");
            Assert.AreEqual(Vector2.zero, capsule.offset,
                "RMAP03 fixtures must not override the prefab capsule offset.");

            Bounds capsuleBounds = capsule.bounds;
            float horizontalSeparation = run.ApproachDirection > 0
                ? run.SurfaceBounds.min.x - capsuleBounds.max.x
                : capsuleBounds.min.x - run.SurfaceBounds.max.x;
            Assert.Greater(horizontalSeparation, 0f,
                "FIXTURE_INITIAL_OVERLAP: capsule must begin outside the surface.");
            Assert.GreaterOrEqual(horizontalSeparation, CollisionSkin,
                "FIXTURE_INITIAL_OVERLAP: separation must preserve the collision skin.");
            Assert.LessOrEqual(horizontalSeparation, run.Movement.Settings.GrabProbeDistance,
                "Fixture begins too far outside the actual Grab approach range.");
            Assert.IsFalse(run.SurfaceBounds.Contains(run.Player.Body.position),
                "FIXTURE_INITIAL_OVERLAP: Player position must not begin inside the surface.");

            Debug.Log(BuildGrabProbeTrace(run, "FIXTURE_START"));

            if (!hasPhysicalSurface)
            {
                return;
            }

            var overlaps = new Collider2D[16];
            var filter = new ContactFilter2D();
            filter.SetLayerMask(run.Movement.Settings.SolidLayers);
            filter.useTriggers = false;
            int overlapCount = Physics2D.OverlapCollider(capsule, filter, overlaps);
            Assert.AreEqual(0, overlapCount,
                "FIXTURE_INITIAL_OVERLAP: actual capsule overlaps a SOLID collider before START.");
            Assert.IsFalse(capsuleBounds.Intersects(run.SurfaceBounds),
                "FIXTURE_INITIAL_OVERLAP: actual capsule bounds intersect the target surface.");

            Vector2 direction = Vector2.right * run.ApproachDirection;
            Vector2 probeOrigin = (Vector2)capsuleBounds.center
                + direction * (capsuleBounds.extents.x + CollisionSkin);
            Assert.IsFalse(capsule.OverlapPoint(probeOrigin),
                "Fixture Grab probe origin must begin outside the actual BodyCollider.");
            Assert.IsFalse(capsuleBounds.Contains(probeOrigin),
                "Fixture Grab probe origin must begin outside the actual capsule bounds.");

            RaycastHit2D[] hits = GetExternalProbeHits(run);
            Collider2D nearestExternal = hits.Length > 0 ? hits[0].collider : null;

            Assert.IsNotNull(nearestExternal,
                "Fixture must place the nearest external surface inside the real Grab probe range.");
            Assert.AreSame(run.Surface,
                nearestExternal.GetComponentInParent<CharacterLiveGrabSurface>(),
                "The nearest external obstacle must be the intended fixture surface.");
        }

        private static RaycastHit2D[] GetExternalProbeHits(GrabRun run)
        {
            Bounds capsuleBounds = run.Player.BodyCollider.bounds;
            Vector2 direction = Vector2.right * run.ApproachDirection;
            Vector2 probeOrigin = (Vector2)capsuleBounds.center
                + direction * (capsuleBounds.extents.x + CollisionSkin);
            RaycastHit2D[] allHits = Physics2D.RaycastAll(probeOrigin, direction,
                run.Movement.Settings.GrabProbeDistance, run.Movement.Settings.SolidLayers);
            System.Array.Sort(allHits, CompareRaycastHits);
            var external = new System.Collections.Generic.List<RaycastHit2D>();
            foreach (RaycastHit2D hit in allHits)
            {
                if (hit.collider == null || hit.collider == run.Player.BodyCollider || hit.collider.isTrigger)
                {
                    continue;
                }

                external.Add(hit);
            }

            return external.ToArray();
        }

        private static string BuildGrabProbeTrace(GrabRun run, string label)
        {
            Physics2D.SyncTransforms();
            CapsuleCollider2D capsule = run.Player.BodyCollider;
            Bounds capsuleBounds = capsule.bounds;
            Vector2 direction = Vector2.right * run.ApproachDirection;
            Vector2 probeOrigin = (Vector2)capsuleBounds.center
                + direction * (capsuleBounds.extents.x + CollisionSkin);
            Vector2 probeEnd = probeOrigin + direction * run.Movement.Settings.GrabProbeDistance;
            float nearFace = run.ApproachDirection > 0
                ? run.SurfaceBounds.min.x
                : run.SurfaceBounds.max.x;
            float nearFaceDistance = run.ApproachDirection > 0
                ? nearFace - probeOrigin.x
                : probeOrigin.x - nearFace;
            float requiredSideDistance = CalculateRequiredGrabSideDistance(run);
            Vector2 desiredFeet = new Vector2(
                nearFace - run.ApproachDirection * requiredSideDistance,
                run.SurfaceBounds.max.y - run.Movement.Settings.GrabHangOffset);
            Vector2 feetDelta = (Vector2)run.Player.Body.position - desiredFeet;
            RaycastHit2D[] allHits = Physics2D.RaycastAll(probeOrigin, direction,
                run.Movement.Settings.GrabProbeDistance, run.Movement.Settings.SolidLayers);
            System.Array.Sort(allHits, CompareRaycastHits);

            var trace = new StringBuilder();
            trace.Append("RMAP03_GRAB_TRACE ").Append(label).Append('\n');
            trace.Append("bodyPosition=").Append(FormatVector(run.Player.Body.position)).Append('\n');
            trace.Append("capsuleMin=").Append(FormatVector(capsuleBounds.min))
                .Append(" capsuleMax=").Append(FormatVector(capsuleBounds.max)).Append('\n');
            trace.Append("capsuleHalfWidth=").Append(FormatFloat(capsuleBounds.extents.x))
                .Append(" capsuleHalfHeight=").Append(FormatFloat(capsuleBounds.extents.y))
                .Append(" capsuleOffset=").Append(FormatVector(capsule.offset)).Append('\n');
            trace.Append("approachDirection=").Append(run.ApproachDirection).Append('\n');
            trace.Append("probeOrigin=").Append(FormatVector(probeOrigin))
                .Append(" probeEnd=").Append(FormatVector(probeEnd))
                .Append(" GrabProbeDistance=")
                .Append(FormatFloat(run.Movement.Settings.GrabProbeDistance)).Append('\n');
            trace.Append("surfaceCollider=")
                .Append(run.SurfacePhysicsCollider != null ? run.SurfacePhysicsCollider.name : "NONE")
                .Append(" surfaceMin=").Append(FormatVector(run.SurfaceBounds.min))
                .Append(" surfaceMax=").Append(FormatVector(run.SurfaceBounds.max)).Append('\n');
            trace.Append("probeOriginToNearFace=").Append(FormatFloat(nearFaceDistance)).Append('\n');
            trace.Append("desiredFeet=").Append(FormatVector(desiredFeet))
                .Append(" feetDelta=").Append(FormatVector(feetDelta))
                .Append(" GrabVerticalWindow=")
                .Append(FormatFloat(run.Movement.Settings.GrabVerticalWindow)).Append('\n');
            trace.Append("hits=").Append(allHits.Length).Append('\n');
            foreach (RaycastHit2D hit in allHits)
            {
                Collider2D hitCollider = hit.collider;
                CharacterLiveGrabSurface marker = hitCollider != null
                    ? hitCollider.GetComponentInParent<CharacterLiveGrabSurface>()
                    : null;
                trace.Append("  distance=").Append(FormatFloat(hit.distance))
                    .Append(" name=").Append(hitCollider != null ? hitCollider.name : "NULL")
                    .Append(" instanceId=").Append(hitCollider != null ? hitCollider.GetInstanceID() : 0)
                    .Append(" trigger=").Append(hitCollider != null && hitCollider.isTrigger)
                    .Append(" layer=").Append(hitCollider != null ? hitCollider.gameObject.layer : -1)
                    .Append(" bodyCollider=").Append(hitCollider == capsule)
                    .Append(" grabSurface=").Append(marker != null)
                    .Append(" isGrabAllowed=").Append(marker != null && marker.IsGrabAllowed)
                    .Append('\n');
            }
            trace.Append("tryBeginGrabOnSideBranch=").Append(DetermineGrabProbeBranch(run, allHits));
            return trace.ToString();
        }

        private static string DetermineGrabProbeBranch(GrabRun run, RaycastHit2D[] allHits)
        {
            RaycastHit2D selected = default;
            foreach (RaycastHit2D hit in allHits)
            {
                if (hit.collider == null || hit.collider == run.Player.BodyCollider || hit.collider.isTrigger)
                {
                    continue;
                }

                selected = hit;
                break;
            }

            if (selected.collider == null)
            {
                return "NO_EXTERNAL_PHYSICAL_OBSTACLE";
            }

            CharacterLiveGrabSurface surface =
                selected.collider.GetComponentInParent<CharacterLiveGrabSurface>();
            if (surface == null)
            {
                return "NO_CHARACTER_LIVE_GRAB_SURFACE";
            }
            if (!surface.IsGrabAllowed)
            {
                return "SURFACE_NOT_GRAB_ALLOWED";
            }

            Bounds bounds = selected.collider.bounds;
            Vector2 corner = new Vector2(run.ApproachDirection > 0 ? bounds.min.x : bounds.max.x,
                bounds.max.y);
            float requiredSideDistance = CalculateRequiredGrabSideDistance(run);
            Vector2 desiredFeet = corner + new Vector2(
                -run.ApproachDirection * requiredSideDistance,
                -run.Movement.Settings.GrabHangOffset);
            if (Mathf.Abs(run.Player.Body.position.x - desiredFeet.x) >
                run.Movement.Settings.GrabProbeDistance)
            {
                return "OUTSIDE_HORIZONTAL_GRAB_WINDOW";
            }
            if (Mathf.Abs(run.Player.Body.position.y - desiredFeet.y) >
                run.Movement.Settings.GrabVerticalWindow)
            {
                return "OUTSIDE_VERTICAL_GRAB_WINDOW";
            }

            Vector2 exposedOrigin = corner + new Vector2(-run.ApproachDirection * 0.08f, 0.02f);
            if (Physics2D.Raycast(exposedOrigin, Vector2.up, 0.12f,
                    run.Movement.Settings.SolidLayers).collider != null)
            {
                return "EXPOSED_CORNER_BLOCKED";
            }

            return "READY";
        }

        private static void AssertDecorationVicinityEmpty(GrabRun run)
        {
            float clearance = run.Movement.Settings.GrabProbeDistance
                + run.Player.BodyCollider.bounds.size.x;
            Vector2 size = (Vector2)run.SurfaceBounds.size + Vector2.one * clearance * 2f;
            Collider2D[] nearby = Physics2D.OverlapBoxAll(run.SurfaceBounds.center, size, 0f,
                run.Movement.Settings.SolidLayers);
            foreach (Collider2D collider in nearby)
            {
                if (collider == null || collider == run.Player.BodyCollider || collider.isTrigger)
                {
                    continue;
                }

                Assert.Fail("Decoration empty fixture contains external collider " + collider.name);
            }

            Assert.AreEqual(0, GetExternalProbeHits(run).Length,
                "Decoration fixture must begin with an empty external-hit list.");
        }

        private static string FormatVector(Vector2 value)
        {
            return "(" + FormatFloat(value.x) + "," + FormatFloat(value.y) + ")";
        }

        private static float GrabPhysicsOwnerX(Collider2D collider)
        {
            return collider.attachedRigidbody != null
                ? collider.attachedRigidbody.position.x
                : collider.transform.position.x;
        }

        private static Vector2 GrabPhysicsOwnerPosition(Collider2D collider)
        {
            return collider.attachedRigidbody != null
                ? collider.attachedRigidbody.position
                : (Vector2)collider.transform.position;
        }

        private static string FormatColliderDistance(ColliderDistance2D value)
        {
            return "isOverlapped=" + value.isOverlapped
                + " distance=" + FormatFloat(value.distance)
                + " pointA=" + FormatVector(value.pointA)
                + " pointB=" + FormatVector(value.pointB);
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static float CalculateRequiredGrabSideDistance(GrabRun run)
        {
            float actualHalfWidth = run.Player.BodyCollider.bounds.extents.x;
            float requiredClearance = Physics2D.defaultContactOffset + CollisionSkin;
            return Mathf.Max(run.Movement.Settings.GrabSideOffset,
                actualHalfWidth + requiredClearance);
        }

        private static void AssertGrabSeparation(GrabRun run)
        {
            Physics2D.SyncTransforms();
            Collider2D grabbed = PrivateCollider(run.Movement, "grabbedCollider");
            Assert.IsNotNull(grabbed);
            Bounds capsuleBounds = run.Player.BodyCollider.bounds;
            Bounds solidBounds = grabbed.bounds;
            float separation = run.ApproachDirection > 0
                ? solidBounds.min.x - capsuleBounds.max.x
                : capsuleBounds.min.x - solidBounds.max.x;
            Assert.IsFalse(capsuleBounds.Intersects(solidBounds),
                "GRAB_INITIAL_OVERLAP: Grab began with intersecting capsule/solid bounds.");
            Assert.That(separation, Is.GreaterThanOrEqualTo(Physics2D.defaultContactOffset),
                "GRAB_INITIAL_OVERLAP: Grab must preserve at least the physics contact offset.");

            ColliderDistance2D distance = run.Player.BodyCollider.Distance(grabbed);
            Assert.IsFalse(distance.isOverlapped,
                "GRAB_INITIAL_OVERLAP: Collider2D.Distance must report separated colliders.");
            Assert.Greater(distance.distance, 0f,
                "GRAB_INITIAL_OVERLAP: Collider2D.Distance must be positive.");

            var overlaps = new Collider2D[16];
            var filter = new ContactFilter2D();
            filter.SetLayerMask(run.Movement.Settings.SolidLayers);
            filter.useTriggers = false;
            Assert.AreEqual(0, Physics2D.OverlapCollider(run.Player.BodyCollider, filter, overlaps),
                "GRAB_INITIAL_OVERLAP: actual capsule overlaps a SOLID immediately after Grab.");
        }

        private static float HorizontalProjectionOverlap(Bounds first, Bounds second)
        {
            return Mathf.Max(0f,
                Mathf.Min(first.max.x, second.max.x) - Mathf.Max(first.min.x, second.min.x));
        }

        private static float ResolveSafeLandingHorizontalInput(
            float bodyX,
            float velocityX,
            float safeLandingMinX,
            float safeLandingMaxX,
            float landingTargetX,
            float actualBrakeAcceleration)
        {
            float projectedStopX = CalculateProjectedStopX(
                bodyX, velocityX, actualBrakeAcceleration);

            if (bodyX < safeLandingMinX && velocityX <= 0f)
            {
                return 1f;
            }

            if (bodyX > safeLandingMaxX && velocityX >= 0f)
            {
                return -1f;
            }

            if (projectedStopX < landingTargetX)
            {
                return 1f;
            }

            if (projectedStopX > landingTargetX)
            {
                return -1f;
            }

            return 0f;
        }

        private static float CalculateProjectedStopX(
            float bodyX,
            float velocityX,
            float actualBrakeAcceleration)
        {
            float safeBrakeAcceleration = Mathf.Max(
                actualBrakeAcceleration, Mathf.Epsilon);
            float stoppingDistance = velocityX * velocityX /
                (2f * safeBrakeAcceleration);
            return bodyX + Mathf.Sign(velocityX) * stoppingDistance;
        }

        private static void AppendSafeLandingStep(
            StringBuilder trace,
            string phase,
            GrabRun run,
            Collider2D releasedCollider,
            float safeLandingMinX,
            float safeLandingMaxX,
            float landingTargetX,
            float surfaceTop,
            float actualBrakeAcceleration,
            float horizontalInput)
        {
            Vector2 bodyPosition = run.Player.Body.position;
            Vector2 velocity = run.Movement.Velocity;
            float projectedStopX = CalculateProjectedStopX(
                bodyPosition.x, velocity.x, actualBrakeAcceleration);
            Collider2D landingCollider = FindLandingSupport(run, releasedCollider);
            trace.Append("step phase=").Append(phase)
                .Append(" body.x=").Append(FormatFloat(bodyPosition.x))
                .Append(" body.y=").Append(FormatFloat(bodyPosition.y))
                .Append(" velocity.x=").Append(FormatFloat(velocity.x))
                .Append(" velocity.y=").Append(FormatFloat(velocity.y))
                .Append(" safeLandingMinX=").Append(FormatFloat(safeLandingMinX))
                .Append(" safeLandingMaxX=").Append(FormatFloat(safeLandingMaxX))
                .Append(" landingTargetX=").Append(FormatFloat(landingTargetX))
                .Append(" projectedStopX=").Append(FormatFloat(projectedStopX))
                .Append(" capsuleMinY=")
                .Append(FormatFloat(run.Player.BodyCollider.bounds.min.y))
                .Append(" surfaceTop=").Append(FormatFloat(surfaceTop))
                .Append(" horizontalInput=").Append(FormatFloat(horizontalInput))
                .Append(" grounded=").Append(run.Movement.IsGroundedNow)
                .Append(" collisionIgnore=")
                .Append(Physics2D.GetIgnoreCollision(
                    run.Player.BodyCollider, releasedCollider))
                .Append(" landingCollider=")
                .Append(landingCollider != null ? landingCollider.name : "NONE")
                .Append('\n');
        }

        private static Collider2D FindLandingSupport(GrabRun run, Collider2D expectedSurface)
        {
            Bounds capsuleBounds = run.Player.BodyCollider.bounds;
            Bounds surfaceBounds = expectedSurface.bounds;
            float overlapMinX = Mathf.Max(capsuleBounds.min.x, surfaceBounds.min.x);
            float overlapMaxX = Mathf.Min(capsuleBounds.max.x, surfaceBounds.max.x);
            if (overlapMaxX <= overlapMinX)
            {
                return null;
            }

            Vector2 origin = new Vector2((overlapMinX + overlapMaxX) * 0.5f,
                capsuleBounds.center.y);
            float distance = capsuleBounds.extents.y + Physics2D.defaultContactOffset
                + CollisionSkin * 2f;
            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, distance,
                run.Movement.Settings.SolidLayers);
            System.Array.Sort(hits, CompareRaycastHits);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null || hit.collider == run.Player.BodyCollider ||
                    hit.collider.isTrigger || hit.normal.y < 0.5f)
                {
                    continue;
                }

                return hit.collider;
            }

            return null;
        }

        private static int CompareRaycastHits(RaycastHit2D left, RaycastHit2D right)
        {
            int distanceOrder = left.distance.CompareTo(right.distance);
            if (distanceOrder != 0)
            {
                return distanceOrder;
            }

            int leftId = left.collider != null ? left.collider.GetInstanceID() : int.MaxValue;
            int rightId = right.collider != null ? right.collider.GetInstanceID() : int.MaxValue;
            return leftId.CompareTo(rightId);
        }

        private static BoxCollider2D CreateCeiling(GrabRun run)
        {
            var ceiling = new GameObject("SeparateSolidCeiling", typeof(BoxCollider2D));
            ceiling.transform.SetParent(run.Root.transform, false);
            BoxCollider2D collider = ceiling.GetComponent<BoxCollider2D>();
            Bounds capsuleBounds = run.Player.BodyCollider.bounds;
            float ceilingHeight = capsuleBounds.extents.y;
            float ceilingBottom = run.SurfaceBounds.max.y
                + run.Movement.Settings.GrabVerticalWindow * 0.5f;
            float requiredSideDistance = CalculateRequiredGrabSideDistance(run);
            float anchorX = run.ApproachDirection > 0
                ? run.SurfaceBounds.min.x - requiredSideDistance
                : run.SurfaceBounds.max.x + requiredSideDistance;
            ceiling.transform.position = new Vector2(anchorX, ceilingBottom + ceilingHeight * 0.5f);
            collider.size = new Vector2(capsuleBounds.size.x + CollisionSkin * 2f, ceilingHeight);
            Physics2D.SyncTransforms();
            AssertFixtureStart(run, true);
            return collider;
        }

        private static float CollisionSkin
        {
            get
            {
                var field = typeof(CharacterLiveMovementDriver).GetField("Skin",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                Assert.IsNotNull(field, "Missing CharacterLiveMovementDriver collision skin.");
                return (float)field.GetRawConstantValue();
            }
        }

        private static Collider2D PrivateCollider(
            CharacterLiveMovementDriver movement,
            string fieldName)
        {
            var field = typeof(CharacterLiveMovementDriver).GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Missing movement field " + fieldName);
            return field.GetValue(movement) as Collider2D;
        }

        private static Transform PrivateTransform(
            CharacterLiveMovementDriver movement,
            string fieldName)
        {
            var field = typeof(CharacterLiveMovementDriver).GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Missing movement field " + fieldName);
            return field.GetValue(movement) as Transform;
        }

        private static float PrivateFloat(
            CharacterLiveMovementDriver movement,
            string fieldName)
        {
            var field = typeof(CharacterLiveMovementDriver).GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Missing movement field " + fieldName);
            return (float)field.GetValue(movement);
        }

        private AsyncOperation BeginSceneUnload(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            AsyncOperation operation = SceneManager.UnloadSceneAsync(scene);
            if (operation != null)
            {
                pendingSceneUnloads.Add(operation);
            }

            return operation;
        }

        private IEnumerator FinishSceneUnload(Scene scene, AsyncOperation operation)
        {
            if (operation != null && !operation.isDone)
            {
                yield return operation;
            }

            yield return null;
            Physics2D.SyncTransforms();
            pendingSceneUnloads.Remove(operation);
            testOwnedScenes.Remove(scene);
        }

        private static T FindFirstInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private void ScheduleFixtureDestroy(GrabRun run)
        {
            if (run == null || run.DestroyScheduled)
            {
                return;
            }

            run.DestroyScheduled = true;
            if (run.Root != null)
            {
                Object.Destroy(run.Root);
            }
        }

        private IEnumerator FinishFixtureDestroy(GrabRun run)
        {
            ScheduleFixtureDestroy(run);
            yield return null;
            Physics2D.SyncTransforms();
            if (run != null)
            {
                testOwnedRoots.Remove(run.Root);
            }
            AssertNoRmap03FixtureLeaks();
        }

        private static void AssertNoRmap03FixtureLeaks()
        {
            if (!HasRmap03FixtureLeaks(null))
            {
                return;
            }

            var trace = new StringBuilder("FIXTURE_LEAK_DETECTED");
            foreach (Collider2D collider in Object.FindObjectsByType<Collider2D>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (IsTestOwnedFixtureCollider(collider))
                {
                    trace.Append(" name=").Append(collider.name)
                        .Append(" instanceId=").Append(collider.GetInstanceID());
                }
            }

            Assert.Fail(trace.ToString());
        }

        private static bool HasRmap03FixtureLeaks(GameObject allowedRoot)
        {
            foreach (Collider2D collider in Object.FindObjectsByType<Collider2D>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!IsTestOwnedFixtureCollider(collider))
                {
                    continue;
                }

                if (allowedRoot != null && collider.transform.IsChildOf(allowedRoot.transform))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static bool IsTestOwnedFixtureCollider(Collider2D collider)
        {
            if (collider == null)
            {
                return false;
            }

            Transform current = collider.transform;
            while (current != null)
            {
                if (current.name == "RMAP03_ActualGrabFixture")
                {
                    return true;
                }
                current = current.parent;
            }

            return false;
        }

        private sealed class GrabPhysicsSnapshot
        {
            public string Label;
            public Vector2 BodyPosition;
            public Bounds CapsuleBounds;
            public Bounds TargetBounds;
            public Vector2 DesiredFeet;
            public float RequiredSideDistance;
            public ColliderDistance2D TargetDistance;
            public Collider2D[] Overlaps;
            public Collider2D Target;
        }

        private sealed class GrabRun
        {
            public GameObject Root;
            public CharacterLivePlayerRig Player;
            public CharacterLiveMovementDriver Movement;
            public CharacterLiveGrabSurface Surface;
            public Collider2D SurfaceCollider;
            public Collider2D SurfacePhysicsCollider;
            public Transform SurfaceTransform;
            public Collider2D GroundCollider;
            public Bounds SurfaceBounds;
            public int ApproachDirection;
            public bool DestroyScheduled;
        }

        private enum VerticalFixtureState
        {
            Descending,
            Ascending
        }
    }
}
#endif
