#if UNITY_EDITOR
using System.Collections;
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

namespace StarNight.Character.Tests.PlayMode.Rmap05
{
    [Category("RMAP05")]
    public sealed class CharacterLiveFallDamagePlayModeTests : InputTestFixture
    {
        private const string PlayerPrefabPath = "Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab";
        private const string ScenePath =
            "Assets/_Game/Map/Scenes/MoonPalace/RMAP05/MoonPalaceFallDamage_RMAP05.unity";

        [UnityTest]
        public IEnumerator P09_ActualGrabResetsObservedFallAndMeasuresTheNextDropAgain()
        {
            FallRun run = CreateRun("RMAP05_Grab", new Vector2(7.76f, 20f));
            CreateGrabSurface(run, new Vector2(8.5f, 8f));
            Physics2D.SyncTransforms();

            for (var index = 0; index < 300 && !run.Movement.IsGrabbing; index++)
            {
                yield return Feed(run.Player, 0f, false, false, false, false, false);
            }

            Assert.IsTrue(run.Movement.IsGrabbing,
                "The actual safe Collider2D corner must accept the falling Player.");
            Assert.AreEqual(CharacterLiveFallResetKind.Grab,
                run.Fall.LastTraversalResetKind);
            Assert.Greater(run.Fall.LastTraversalResetDistance, 6f);
            Assert.AreEqual(5, run.Fall.CurrentHealth,
                "Grab itself must not apply fall damage or stun.");
            Assert.IsFalse(run.Fall.IsStunned);

            yield return Feed(run.Player, 0f, false, true, false, false, false);
            // Move away from the same wall while dropping so the existing
            // RMAP03 reentry window cannot re-acquire its safe corner.
            yield return WaitForNextLanding(run, 0, false, -1f);
            Assert.That(run.Fall.LastProcessedFallDistance, Is.InRange(6f, 10f),
                "The fall after release is measured from the actual Grab reset anchor.");
            Assert.AreEqual(5, run.Fall.CurrentHealth);
            Assert.IsTrue(run.Fall.IsStunned);
            Debug.Log($"RMAP05 P09: grab reset={run.Fall.LastTraversalResetDistance:F3}, " +
                $"nextFall={run.Fall.LastProcessedFallDistance:F3}");
            DestroyRun(run);
        }

        [UnityTest]
        public IEnumerator P14_ActualLadderAndPoleEntryResetButMereTriggerContactDoesNot()
        {
            foreach (CharacterLiveClimbSurface.SurfaceKind kind in new[]
                     {
                         CharacterLiveClimbSurface.SurfaceKind.Ladder,
                         CharacterLiveClimbSurface.SurfaceKind.Pole
                     })
            {
                FallRun contact = CreateRun("RMAP05_Mere" + kind, new Vector2(5f, 19.9f));
                CreateClimbAxis(contact, kind, new Vector2(5f, 17.5f));
                Physics2D.SyncTransforms();
                yield return Feed(contact.Player, 0f, false, false, false, false, false);
                Assert.IsFalse(contact.Movement.IsClimbing);
                Assert.AreNotEqual(CharacterLiveFallResetKind.Climb,
                    contact.Fall.LastTraversalResetKind,
                    "A trigger overlap without Up/Down is not a climb reset.");
                DestroyRun(contact);
                yield return null;

                FallRun run = CreateRun("RMAP05_Climb" + kind, new Vector2(5f, 28f));
                CreateClimbAxis(run, kind, new Vector2(5f, 17.5f));
                Physics2D.SyncTransforms();
                for (var index = 0; index < 360 && !run.Movement.IsClimbing; index++)
                {
                    yield return Feed(run.Player, 0f, true, false, false, false, false);
                }

                Assert.IsTrue(run.Movement.IsClimbing);
                Assert.AreEqual(CharacterLiveFallResetKind.Climb,
                    run.Fall.LastTraversalResetKind);
                Assert.Greater(run.Fall.LastTraversalResetDistance, 6f);
                Assert.AreEqual(5, run.Fall.CurrentHealth,
                    "Actual climb entry resets before any fall damage can be applied.");
                Assert.IsFalse(run.Fall.IsStunned);
                Debug.Log($"RMAP05 P14 {kind}: reset={run.Fall.LastTraversalResetDistance:F3}");
                DestroyRun(run);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator P16_ActualOneWayLandingAppliesDamageBeforeResetAndDropThroughKeepsGround()
        {
            FallRun landing = CreateRun("RMAP05_OneWayLanding", new Vector2(10f, 26f));
            CreateOneWay(landing, new Vector2(10f, 10f));
            Physics2D.SyncTransforms();
            yield return WaitForNextLanding(landing, 0, true);

            Assert.IsTrue(landing.Movement.IsGroundedNow);
            Assert.IsTrue(landing.Fall.LastLandingWasOneWay);
            Assert.That(landing.Fall.LastProcessedFallDistance, Is.InRange(15f, 20f));
            Assert.AreEqual(2, landing.Fall.LastAppliedDamage,
                "The one-way top landing must use the normal fall table before reset.");
            Assert.AreEqual("fallDistance -> damage/stun/death -> reset",
                landing.Fall.LastLandingSequence);
            Assert.AreEqual(CharacterLiveFallResetKind.Landing,
                landing.Fall.LastTraversalResetKind);
            Assert.IsTrue(landing.Fall.IsStunned);
            Debug.Log($"RMAP05 P16 one-way: fall={landing.Fall.LastProcessedFallDistance:F3}, " +
                $"damage={landing.Fall.LastAppliedDamage}, sequence={landing.Fall.LastLandingSequence}");
            DestroyRun(landing);
            yield return null;

            FallRun drop = CreateRun("RMAP05_OneWayDrop", new Vector2(10f, 10.14f));
            CreateOneWay(drop, new Vector2(10f, 10f));
            Physics2D.SyncTransforms();
            yield return Feed(drop.Player, 0f, false, false, false, false, false);
            yield return Feed(drop.Player, 0f, false, false, false, false, false);
            int initialLandings = drop.Fall.LandingCount;
            yield return Feed(drop.Player, 0f, false, true, false, true, true);
            Assert.IsTrue(drop.Movement.IsDroppingThroughOneWay);
            Assert.IsFalse(Physics2D.GetIgnoreCollision(drop.Player.BodyCollider, drop.Ground),
                "Selected one-way drop-through must retain the actual unrelated ground collider.");
            yield return WaitForNextLanding(drop, initialLandings, false);
            Assert.IsFalse(drop.Fall.LastLandingWasOneWay);
            Assert.IsTrue(drop.Fall.LastProcessedFallDistance > 6f,
                "The drop-through descent still reaches the ordinary fall tracker and table.");
            DestroyRun(drop);
        }

        [UnityTest]
        public IEnumerator P18_ActualFootPivotStunLocksInputButKeepsPhysicsAndCamera()
        {
            FallRun run = CreateRun("RMAP05_Stun", new Vector2(10f, 8.5f));
            yield return WaitForNextLanding(run, 0, false);

            Assert.That(run.Fall.LastProcessedFallDistance, Is.InRange(6f, 10f));
            Assert.AreEqual(0, run.Fall.LastAppliedDamage);
            Assert.IsTrue(run.Fall.IsStunned);
            Assert.IsTrue(run.Player.Body.simulated);
            Assert.IsTrue(run.Player.BodyCollider.enabled);
            Assert.IsTrue(run.Camera.enabled);

            run.Player.InputSource.enabled = true;
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            float lockedX = run.Player.Body.position.x;
            Press(keyboard.dKey);
            yield return null;
            yield return new WaitForFixedUpdate();
            Assert.That(run.Player.Body.position.x, Is.EqualTo(lockedX).Within(0.005f),
                "The existing Input System snapshot reaches the driver but stun suppresses movement.");
            Assert.IsTrue(run.Fall.IsStunned);

            for (var index = 0; index < 32 && run.Fall.IsStunned; index++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.IsFalse(run.Fall.IsStunned, "0.5 second fixed-step stun must expire.");
            lockedX = run.Player.Body.position.x;
            yield return new WaitForFixedUpdate();
            Assert.Greater(run.Player.Body.position.x, lockedX + 0.01f,
                "The original Input System -> snapshot -> motor path resumes after stun.");
            DestroyRun(run);
        }

        [UnityTest]
        public IEnumerator P17_ActualThirtyTileFallSetsDeathAndKeepsColliderPhysicsActive()
        {
            FallRun run = CreateRun("RMAP05_Death", new Vector2(10f, 32f));
            yield return WaitForNextLanding(run, 0, false);

            Assert.GreaterOrEqual(run.Fall.LastProcessedFallDistance, 30f);
            Assert.AreEqual(0, run.Fall.CurrentHealth);
            Assert.IsTrue(run.Fall.IsDead);
            Assert.IsFalse(run.Fall.CanAcceptInput);
            Assert.IsTrue(run.Player.Body.simulated);
            Assert.IsTrue(run.Player.BodyCollider.enabled);
            DestroyRun(run);
        }

        [UnityTest]
        public IEnumerator SavedScene_ContainsActualPlayerColliderFallStateAndOneWayFixture()
        {
            EditorSceneManager.LoadSceneInPlayMode(ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return new WaitForFixedUpdate();
            CharacterLivePlayerRig player = Object.FindFirstObjectByType<CharacterLivePlayerRig>();
            CharacterLiveFallDamageState fall = Object.FindFirstObjectByType<CharacterLiveFallDamageState>();
            CharacterLiveOneWayPlatform oneWay = Object.FindFirstObjectByType<CharacterLiveOneWayPlatform>();
            CharacterLiveClimbSurface[] axes = Object.FindObjectsByType<CharacterLiveClimbSurface>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            CharacterLiveCameraFollowDriver camera =
                Object.FindFirstObjectByType<CharacterLiveCameraFollowDriver>();

            Assert.IsNotNull(player);
            Assert.AreEqual(new Vector2(0.4f, 0.8f), player.BodyCollider.size);
            Assert.IsNotNull(fall);
            Assert.AreEqual(5, fall.MaxHealth);
            Assert.IsNotNull(oneWay.GetComponent<PlatformEffector2D>());
            Assert.AreEqual(2, axes.Length);
            Assert.IsTrue(axes[0].AxisTrigger.isTrigger && axes[1].AxisTrigger.isTrigger);
            Assert.IsNotNull(camera);
        }

        private static FallRun CreateRun(string name, Vector2 startFeet)
        {
            var run = new FallRun();
            run.Root = new GameObject(name);
            var ground = new GameObject("Ground", typeof(BoxCollider2D));
            ground.transform.SetParent(run.Root.transform, false);
            ground.transform.position = new Vector2(10f, 0.5f);
            run.Ground = ground.GetComponent<BoxCollider2D>();
            run.Ground.size = new Vector2(60f, 1f);

            var cameraObject = new GameObject("RMAP05_TestCamera", typeof(Camera),
                typeof(CharacterLiveCameraFollowDriver));
            cameraObject.transform.SetParent(run.Root.transform, false);
            run.Camera = cameraObject.GetComponent<CharacterLiveCameraFollowDriver>();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            var playerHost = new GameObject("RMAP05_ActualPlayer");
            playerHost.transform.SetParent(run.Root.transform, false);
            playerHost.SetActive(false);
            GameObject playerObject = Object.Instantiate(prefab, playerHost.transform);
            run.Player = playerObject.GetComponent<CharacterLivePlayerRig>();
            run.Movement = playerObject.GetComponent<CharacterLiveMovementDriver>();
            run.Fall = playerObject.AddComponent<CharacterLiveFallDamageState>();
            run.Player.BodyCollider.size = new Vector2(0.4f, 0.8f);
            run.Player.BodyCollider.offset = new Vector2(0f, 0.4f);
            run.Movement.ConfigureRmap02(1);
            run.Movement.ConfigureRmap05Fall();
            run.Camera.Configure(cameraObject.GetComponent<Camera>(), run.Player.transform,
                new Rect(-20f, 0f, 60f, 36f), 12f, 8f, 0.08f);
            playerHost.SetActive(true);
            run.Player.Body.position = startFeet;
            run.Movement.ResetMotion();
            run.Player.InputSource.enabled = false;
            Physics2D.SyncTransforms();
            return run;
        }

        private static void CreateGrabSurface(FallRun run, Vector2 position)
        {
            var surface = new GameObject("RMAP05_GrabSurface", typeof(BoxCollider2D),
                typeof(CharacterLiveGrabSurface));
            surface.transform.SetParent(run.Root.transform, false);
            surface.transform.position = position;
            surface.GetComponent<BoxCollider2D>().size = new Vector2(1f, 2f);
            surface.GetComponent<CharacterLiveGrabSurface>().Configure(
                CharacterLiveGrabSurface.SurfaceKind.StaticSafe);
        }

        private static void CreateClimbAxis(
            FallRun run,
            CharacterLiveClimbSurface.SurfaceKind kind,
            Vector2 position)
        {
            var axis = new GameObject("RMAP05_" + kind, typeof(BoxCollider2D),
                typeof(CharacterLiveClimbSurface));
            axis.transform.SetParent(run.Root.transform, false);
            axis.transform.position = position;
            BoxCollider2D collider = axis.GetComponent<BoxCollider2D>();
            collider.size = new Vector2(0.7f, 5f);
            collider.isTrigger = true;
            axis.GetComponent<CharacterLiveClimbSurface>().Configure(kind, collider);
        }

        private static void CreateOneWay(FallRun run, Vector2 position)
        {
            var platform = new GameObject("RMAP05_OneWay", typeof(BoxCollider2D),
                typeof(PlatformEffector2D), typeof(CharacterLiveOneWayPlatform),
                typeof(CharacterLiveGrabSurface));
            platform.transform.SetParent(run.Root.transform, false);
            platform.transform.position = position;
            BoxCollider2D collider = platform.GetComponent<BoxCollider2D>();
            collider.size = new Vector2(5f, 0.25f);
            collider.usedByEffector = true;
            platform.GetComponent<PlatformEffector2D>().useOneWay = true;
            run.OneWay = platform.GetComponent<CharacterLiveOneWayPlatform>();
            run.OneWay.Configure(collider);
            platform.GetComponent<CharacterLiveGrabSurface>().Configure(
                CharacterLiveGrabSurface.SurfaceKind.OneWay);
        }

        private static IEnumerator WaitForNextLanding(
            FallRun run,
            int afterLandingCount,
            bool requireOneWay,
            float horizontal = 0f)
        {
            for (var index = 0; index < 480; index++)
            {
                yield return Feed(run.Player, horizontal, false, false, false, false, false);
                if (run.Fall.LandingCount > afterLandingCount &&
                    (!requireOneWay || run.Fall.LastLandingWasOneWay))
                {
                    yield break;
                }
            }

            Assert.Fail("The actual Player did not reach the expected physical landing in time.");
        }

        private static IEnumerator Feed(
            CharacterLivePlayerRig player,
            float horizontal,
            bool up,
            bool down,
            bool walk,
            bool jumpPressed,
            bool jumpHeld)
        {
            var jump = new CharacterLiveButtonFrame(jumpPressed, false, jumpHeld);
            var idle = new CharacterLiveButtonFrame(false, false, false);
            player.InputSource.Adapter.AccumulateFrame(horizontal, up, down, walk,
                jump, idle, idle, idle);
            yield return new WaitForFixedUpdate();
        }

        private static void DestroyRun(FallRun run)
        {
            if (run != null && run.Root != null)
            {
                Object.DestroyImmediate(run.Root);
                Physics2D.SyncTransforms();
            }
        }

        private sealed class FallRun
        {
            public GameObject Root;
            public CharacterLivePlayerRig Player;
            public CharacterLiveMovementDriver Movement;
            public CharacterLiveFallDamageState Fall;
            public BoxCollider2D Ground;
            public CharacterLiveOneWayPlatform OneWay;
            public CharacterLiveCameraFollowDriver Camera;
        }
    }
}
#endif
