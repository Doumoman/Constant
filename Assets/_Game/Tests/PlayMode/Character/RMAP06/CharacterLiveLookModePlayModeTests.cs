#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using StarNight.Character.Live.Cameras;
using StarNight.Character.Live.Input;
using StarNight.Character.Live.Movement;
using StarNight.Character.Live.Player;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace StarNight.Character.Tests.PlayMode.Rmap06
{
    [Category("RMAP06")]
    public sealed class CharacterLiveLookModePlayModeTests : InputTestFixture
    {
        private const string PlayerPrefabPath = "Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab";
        private const string ScenePath =
            "Assets/_Game/Map/Scenes/MoonPalace/RMAP06/MoonPalaceMovementLab_RMAP06.unity";

        [UnityTest]
        public IEnumerator C03_ActualInputSystemReachesAllEightNormalizedOffsetsAndReturns()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            LookRun run = CreateRun("RMAP06_C03", new Vector2(10f, 1.02f),
                new Rect(-20f, -10f, 60f, 40f));
            yield return WaitForGround(run);
            run.Player.InputSource.enabled = true;

            foreach (Direction direction in Direction.All)
            {
                SetKeyboardState(keyboard, direction, includeLook: true);
                yield return null;
                Assert.IsTrue(run.Player.InputSource.IsLookHeld,
                    "The actual Keyboard Tab binding must reach CharacterLiveInputSource.");
                yield return FixedFrames(1);
                Assert.IsFalse(run.Look.IsLooking,
                    "Tab+direction must not activate observation on its first fixed step.");
                yield return FixedFrames(HoldActivationTicks);
                yield return null;

                Assert.IsTrue(run.Look.IsLooking);
                Assert.That(run.Look.TargetOffset, Is.EqualTo(direction.Offset).Using(Vector2Comparer.Within(0.001f)));
                yield return new WaitForSecondsRealtime(0.28f);
                Assert.That(run.Camera.CurrentLookOffset, Is.EqualTo(direction.Offset).Using(Vector2Comparer.Within(0.08f)));

                SetKeyboardState(keyboard, Direction.None, includeLook: false);
                yield return null;
                yield return new WaitForSecondsRealtime(0.22f);
                Assert.IsFalse(run.Look.IsLooking);
                Assert.That(run.Camera.CurrentLookOffset, Is.EqualTo(Vector2.zero).Using(Vector2Comparer.Within(0.08f)));
            }

            Object.DestroyImmediate(run.Root);
        }

        [UnityTest]
        public IEnumerator C04C05_ActualPlayerStatesGateAndRestoreTheInputSystemPath()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            LookRun grounded = CreateRun("RMAP06_Ground", new Vector2(10f, 1.02f),
                new Rect(-20f, -10f, 60f, 40f));
            yield return WaitForGround(grounded);
            grounded.Player.InputSource.enabled = true;
            SetKeyboardState(keyboard, new Direction(1f, 0f), includeLook: true);
            yield return null;
            yield return FixedFrames(HoldActivationTicks);
            Assert.IsTrue(grounded.Look.IsLooking,
                $"held={grounded.Player.InputSource.IsLookHeld}, eligible={grounded.Movement.IsLookEligible}, " +
                $"waiting={grounded.Look.IsWaiting}, velocity={grounded.Movement.Velocity}");
            float lockedX = grounded.Player.Body.position.x;
            SetKeyboardState(keyboard, new Direction(1f, 0f), includeLook: true, includeJump: true);
            yield return null;
            yield return FixedFrames(5);
            Assert.That(grounded.Player.Body.position.x, Is.EqualTo(lockedX).Within(0.005f));
            Assert.IsTrue(grounded.Movement.IsGroundedNow,
                "Look must consume movement and Jump before the motor sees them.");

            SetKeyboardState(keyboard, Direction.None, includeLook: false);
            yield return null;
            yield return FixedFrames(3);
            SetKeyboardState(keyboard, new Direction(1f, 0f), includeLook: false);
            yield return null;
            yield return FixedFrames(6);
            Assert.Greater(grounded.Player.Body.position.x, lockedX + 0.01f,
                "After return, the original Input System → snapshot → motor path resumes.");
            SetKeyboardState(keyboard, Direction.None, includeLook: false);
            Object.DestroyImmediate(grounded.Root);
            yield return null;

            LookRun airborne = CreateRun("RMAP06_Air", new Vector2(10f, 25f),
                new Rect(-20f, -10f, 60f, 40f));
            airborne.Player.InputSource.enabled = true;
            SetKeyboardState(keyboard, new Direction(1f, 0f), includeLook: true);
            yield return null;
            yield return FixedFrames(HoldActivationTicks);
            Assert.IsFalse(airborne.Look.IsLooking);
            Assert.IsFalse(airborne.Movement.IsLookEligible,
                "An actual airborne Player cannot begin observation.");
            SetKeyboardState(keyboard, Direction.None, includeLook: false);
            Object.DestroyImmediate(airborne.Root);
            yield return null;

            LookRun stunned = CreateRun("RMAP06_Stun", new Vector2(10f, 1.02f),
                new Rect(-20f, -10f, 60f, 40f));
            yield return WaitForGround(stunned);
            stunned.Fall.ApplyLanding(6f, false, 0f, stunned.Movement.Settings);
            stunned.Player.InputSource.enabled = true;
            SetKeyboardState(keyboard, new Direction(1f, 0f), includeLook: true);
            yield return null;
            yield return FixedFrames(12);
            Assert.IsTrue(stunned.Fall.IsStunned);
            Assert.IsFalse(stunned.Look.IsInputLocked,
                "RMAP05 CanAcceptInput=false cancels the look wait instead of retaining it.");
            SetKeyboardState(keyboard, Direction.None, includeLook: false);
            Object.DestroyImmediate(stunned.Root);
            yield return null;

            LookRun dead = CreateRun("RMAP06_Death", new Vector2(10f, 1.02f),
                new Rect(-20f, -10f, 60f, 40f));
            yield return WaitForGround(dead);
            dead.Fall.ApplyLanding(30f, false, 0f, dead.Movement.Settings);
            dead.Player.InputSource.enabled = true;
            SetKeyboardState(keyboard, new Direction(1f, 0f), includeLook: true);
            yield return null;
            yield return FixedFrames(HoldActivationTicks);
            Assert.IsTrue(dead.Fall.IsDead);
            Assert.IsFalse(dead.Look.IsLooking);
            SetKeyboardState(keyboard, Direction.None, includeLook: false);
            Object.DestroyImmediate(dead.Root);
        }

        [UnityTest]
        public IEnumerator C04_StaticGrabAndClimbPermitLookButWorldEdgeClampsTheActualCamera()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            LookRun grab = CreateRun("RMAP06_Grab", new Vector2(7.76f, 20f),
                new Rect(-20f, -10f, 60f, 40f));
            CreateGrabSurface(grab.Root.transform, new Vector2(8.5f, 8f));
            Physics2D.SyncTransforms();
            for (int index = 0; index < 300 && !grab.Movement.IsGrabbing; index++)
            {
                yield return FeedAdapter(grab, 0f, false, false, false);
            }

            Assert.IsTrue(grab.Movement.IsGrabbing);
            grab.Player.InputSource.enabled = true;
            SetKeyboardState(keyboard, new Direction(1f, 0f), includeLook: true);
            yield return null;
            yield return FixedFrames(HoldActivationTicks);
            Assert.IsTrue(grab.Look.IsLooking);
            SetKeyboardState(keyboard, Direction.None, includeLook: false);
            Object.DestroyImmediate(grab.Root);
            yield return null;

            LookRun climb = CreateRun("RMAP06_Climb", new Vector2(10f, 1.02f),
                new Rect(-20f, -10f, 60f, 40f));
            CreateClimbAxis(climb.Root.transform, new Vector2(10f, 3.5f));
            Physics2D.SyncTransforms();
            yield return FeedAdapter(climb, 0f, true, false, false);
            yield return FeedAdapter(climb, 0f, false, false, false);
            Assert.IsTrue(climb.Movement.IsClimbing);
            climb.Player.InputSource.enabled = true;
            SetKeyboardState(keyboard, new Direction(0f, 1f), includeLook: true);
            yield return null;
            yield return FixedFrames(HoldActivationTicks);
            Assert.IsTrue(climb.Look.IsLooking);
            SetKeyboardState(keyboard, Direction.None, includeLook: false);
            Object.DestroyImmediate(climb.Root);
            yield return null;

            LookRun edge = CreateRun("RMAP06_Edge", new Vector2(35f, 1.02f),
                new Rect(0f, 0f, 40f, 16f));
            yield return WaitForGround(edge);
            edge.Player.InputSource.enabled = true;
            SetKeyboardState(keyboard, new Direction(1f, 0f), includeLook: true);
            yield return null;
            yield return FixedFrames(HoldActivationTicks);
            yield return new WaitForSecondsRealtime(0.28f);
            Assert.IsTrue(edge.Look.IsLooking);
            Assert.LessOrEqual(edge.Camera.transform.position.x, 34.001f,
                "The 12-tile viewport center clamps at worldBounds.xMax - 6.");
            SetKeyboardState(keyboard, Direction.None, includeLook: false);
            Object.DestroyImmediate(edge.Root);
        }

        [UnityTest]
        public IEnumerator E02_SavedMovementLabContainsTheActualRmap02To05FixturesAndLookPath()
        {
            EditorSceneManager.LoadSceneInPlayMode(ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return new WaitForFixedUpdate();
            CharacterLivePlayerRig player = Object.FindFirstObjectByType<CharacterLivePlayerRig>();
            CharacterLiveMovementDriver movement = Object.FindFirstObjectByType<CharacterLiveMovementDriver>();
            CharacterLiveCameraFollowDriver camera = Object.FindFirstObjectByType<CharacterLiveCameraFollowDriver>();
            CharacterLiveLookModeState look = Object.FindFirstObjectByType<CharacterLiveLookModeState>();
            CharacterLiveFallDamageState fall = Object.FindFirstObjectByType<CharacterLiveFallDamageState>();
            TilemapCollider2D terrain = Object.FindFirstObjectByType<TilemapCollider2D>();
            CharacterLiveGrabSurface[] grabs = Object.FindObjectsByType<CharacterLiveGrabSurface>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            CharacterLiveClimbSurface[] axes = Object.FindObjectsByType<CharacterLiveClimbSurface>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            CharacterLiveOneWayPlatform oneWay = Object.FindFirstObjectByType<CharacterLiveOneWayPlatform>();

            Assert.IsNotNull(player);
            Assert.AreEqual(new Vector2(0.4f, 0.8f), player.BodyCollider.size);
            Assert.IsNotNull(movement);
            Assert.IsNotNull(look);
            Assert.IsNotNull(fall);
            Assert.IsNotNull(camera);
            Assert.AreEqual(new Vector2(12f, 8f), camera.VisibleWorldSize);
            Assert.IsNotNull(terrain);
            Assert.GreaterOrEqual(grabs.Length, 3);
            Assert.AreEqual(2, axes.Length);
            Assert.IsNotNull(oneWay.GetComponent<PlatformEffector2D>());
        }

        private static LookRun CreateRun(string name, Vector2 startFeet, Rect cameraBounds)
        {
            var run = new LookRun { Root = new GameObject(name) };
            var ground = new GameObject("Ground", typeof(BoxCollider2D));
            ground.transform.SetParent(run.Root.transform, false);
            ground.transform.position = new Vector2(10f, 0.5f);
            ground.GetComponent<BoxCollider2D>().size = new Vector2(80f, 1f);

            var cameraObject = new GameObject("RMAP06_TestCamera", typeof(Camera),
                typeof(CharacterLiveCameraFollowDriver));
            cameraObject.transform.SetParent(run.Root.transform, false);
            run.Camera = cameraObject.GetComponent<CharacterLiveCameraFollowDriver>();

            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            var playerHost = new GameObject("RMAP06_ActualPlayer");
            playerHost.transform.SetParent(run.Root.transform, false);
            playerHost.SetActive(false);
            GameObject playerObject = Object.Instantiate(prefab, playerHost.transform);
            run.Player = playerObject.GetComponent<CharacterLivePlayerRig>();
            run.Movement = playerObject.GetComponent<CharacterLiveMovementDriver>();
            run.Fall = playerObject.GetComponent<CharacterLiveFallDamageState>() ??
                playerObject.AddComponent<CharacterLiveFallDamageState>();
            run.Look = playerObject.GetComponent<CharacterLiveLookModeState>() ??
                playerObject.AddComponent<CharacterLiveLookModeState>();
            run.Player.BodyCollider.size = new Vector2(0.4f, 0.8f);
            run.Player.BodyCollider.offset = new Vector2(0f, 0.4f);
            run.Movement.ConfigureRmap02(1);
            run.Movement.ConfigureRmap05Fall();
            run.Camera.Configure(cameraObject.GetComponent<Camera>(), run.Player.transform,
                cameraBounds, 12f, 8f, 0.08f);
            playerHost.SetActive(true);
            run.Player.Body.position = startFeet;
            run.Movement.ResetMotion();
            run.Player.InputSource.enabled = false;
            Physics2D.SyncTransforms();
            return run;
        }

        private static IEnumerator WaitForGround(LookRun run)
        {
            for (int index = 0; index < 80 && !run.Movement.IsGroundedNow; index++)
            {
                yield return FeedAdapter(run, 0f, false, false, false);
            }

            Assert.IsTrue(run.Movement.IsGroundedNow, "The actual Player must settle on its Collider2D ground.");
        }

        private static IEnumerator FeedAdapter(
            LookRun run,
            float horizontal,
            bool up,
            bool down,
            bool lookHeld)
        {
            var idle = new CharacterLiveButtonFrame(false, false, false);
            run.Player.InputSource.Adapter.AccumulateFrame(horizontal, up, down, false,
                lookHeld, idle, idle, idle, idle);
            yield return new WaitForFixedUpdate();
        }

        private static IEnumerator FixedFrames(int count)
        {
            for (int index = 0; index < count; index++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        private static int HoldActivationTicks
        {
            get
            {
                return Mathf.CeilToInt(CharacterLiveLookModeState.HoldSeconds /
                    Time.fixedDeltaTime) + 3;
            }
        }

        private static void CreateGrabSurface(Transform parent, Vector2 position)
        {
            var surface = new GameObject("RMAP06_SafeGrab", typeof(BoxCollider2D),
                typeof(CharacterLiveGrabSurface));
            surface.transform.SetParent(parent, false);
            surface.transform.position = position;
            surface.GetComponent<BoxCollider2D>().size = new Vector2(1f, 2f);
            surface.GetComponent<CharacterLiveGrabSurface>().Configure(
                CharacterLiveGrabSurface.SurfaceKind.StaticSafe);
        }

        private static void CreateClimbAxis(Transform parent, Vector2 position)
        {
            var axis = new GameObject("RMAP06_Ladder", typeof(BoxCollider2D),
                typeof(CharacterLiveClimbSurface));
            axis.transform.SetParent(parent, false);
            axis.transform.position = position;
            BoxCollider2D collider = axis.GetComponent<BoxCollider2D>();
            collider.size = new Vector2(0.7f, 5f);
            collider.isTrigger = true;
            axis.GetComponent<CharacterLiveClimbSurface>().Configure(
                CharacterLiveClimbSurface.SurfaceKind.Ladder, collider);
        }

        private static void SetKeyboardState(
            Keyboard keyboard,
            Direction direction,
            bool includeLook,
            bool includeJump = false)
        {
            var keys = new System.Collections.Generic.List<Key>();
            if (includeLook) keys.Add(Key.Tab);
            if (direction.Horizontal > 0f) keys.Add(Key.D);
            if (direction.Horizontal < 0f) keys.Add(Key.A);
            if (direction.Vertical > 0f) keys.Add(Key.W);
            if (direction.Vertical < 0f) keys.Add(Key.S);
            if (includeJump) keys.Add(Key.Space);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys.ToArray()));
            InputSystem.Update();
        }

        private sealed class LookRun
        {
            public GameObject Root;
            public CharacterLivePlayerRig Player;
            public CharacterLiveMovementDriver Movement;
            public CharacterLiveFallDamageState Fall;
            public CharacterLiveLookModeState Look;
            public CharacterLiveCameraFollowDriver Camera;
        }

        private readonly struct Direction
        {
            public static readonly Direction None = new Direction(0f, 0f);
            public static readonly Direction[] All =
            {
                new Direction(1f, 0f), new Direction(-1f, 0f),
                new Direction(0f, 1f), new Direction(0f, -1f),
                new Direction(1f, 1f), new Direction(-1f, 1f),
                new Direction(1f, -1f), new Direction(-1f, -1f)
            };

            public Direction(float horizontal, float vertical)
            {
                Horizontal = horizontal;
                Vertical = vertical;
                Offset = new Vector2(horizontal, vertical).normalized * 3f;
            }

            public float Horizontal { get; }
            public float Vertical { get; }
            public Vector2 Offset { get; }
        }

        private sealed class Vector2Comparer : IEqualityComparer
        {
            private readonly float tolerance;

            private Vector2Comparer(float tolerance)
            {
                this.tolerance = tolerance;
            }

            public static Vector2Comparer Within(float tolerance)
            {
                return new Vector2Comparer(tolerance);
            }

            public new bool Equals(object expected, object actual)
            {
                return expected is Vector2 first && actual is Vector2 second &&
                    Vector2.Distance(first, second) <= tolerance;
            }

            public int GetHashCode(object obj)
            {
                return obj != null ? obj.GetHashCode() : 0;
            }
        }
    }
}
#endif
