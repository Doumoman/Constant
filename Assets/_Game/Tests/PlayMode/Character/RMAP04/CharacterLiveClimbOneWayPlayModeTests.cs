#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using StarNight.Character.Live.Input;
using StarNight.Character.Live.Movement;
using StarNight.Character.Live.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StarNight.Character.Tests.PlayMode.Rmap04
{
    [Category("RMAP04")]
    public sealed class CharacterLiveClimbOneWayPlayModeTests : InputTestFixture
    {
        private const string PlayerPrefabPath = "Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab";
        private const string ScenePath =
            "Assets/_Game/Map/Scenes/MoonPalace/RMAP04/MoonPalaceClimbOneWay_RMAP04.unity";

        [UnityTest]
        public IEnumerator P11_ActualPlayer_EntersLadderAndPoleOnlyWithVerticalIntent()
        {
            foreach (CharacterLiveClimbSurface.SurfaceKind kind in new[]
                     {
                         CharacterLiveClimbSurface.SurfaceKind.Ladder,
                         CharacterLiveClimbSurface.SurfaceKind.Pole
                     })
            {
                PhysicalRun run = CreateClimbRun(kind, 1.01f);
                yield return Feed(run.Player, 0f, false, false, false, false, false);
                Assert.IsFalse(run.Movement.IsClimbing,
                    "Mere " + kind + " trigger contact must not auto-enter climb.");

                float startY = run.Player.Body.position.y;
                yield return Feed(run.Player, 0f, true, false, false, false, false);
                Assert.IsTrue(run.Movement.IsClimbing);
                Assert.Greater(run.Player.Body.position.y, startY + 0.05f);
                Assert.IsTrue(run.AxisCollider.isTrigger,
                    "Climb axis stays a pass-through trigger instead of a solid wall/floor.");
                DestroyRun(run);
                yield return null;
            }

            PhysicalRun top = CreateClimbRun(CharacterLiveClimbSurface.SurfaceKind.Pole, 5.9f);
            bool leftPoleAxis = false;
            for (var index = 0; index < 48; index++)
            {
                yield return Feed(top.Player, 0f, true, false, false, false, false);
                if (!top.Movement.IsClimbing)
                {
                    leftPoleAxis = true;
                    break;
                }
            }

            Assert.IsTrue(leftPoleAxis,
                "Leaving the trigger at a pole top must not create a standable cap.");
            Assert.IsFalse(top.Movement.IsGroundedNow);
            DestroyRun(top);
            yield return null;
        }

        [UnityTest]
        public IEnumerator P12_ActualPlayer_UsesSharedFastSlowAndDownSettings()
        {
            PhysicalRun fast = CreateClimbRun(CharacterLiveClimbSurface.SurfaceKind.Ladder, 1.01f);
            float fastStart = fast.Player.Body.position.y;
            for (var index = 0; index < 10; index++)
            {
                yield return Feed(fast.Player, 0f, true, false, false, false, false);
            }

            float fastDistance = fast.Player.Body.position.y - fastStart;
            Assert.AreEqual(10L, fast.Movement.PhysicsTick,
                "Every supplied fixed snapshot must drive exactly one climb step.");
            float fastExpected = fast.Movement.Settings.ClimbSpeed *
                fast.Movement.LastFixedDeltaTime * fast.Movement.PhysicsTick;
            Assert.That(fastDistance, Is.InRange(fastExpected - 0.05f, fastExpected + 0.05f));
            Assert.AreEqual(4f, fast.Movement.Settings.ClimbSpeed, 0.0001f);
            Debug.Log($"RMAP04 P12 fast: dt={fast.Movement.LastFixedDeltaTime:F6}, " +
                $"ticks={fast.Movement.PhysicsTick}, distance={fastDistance:F3}");
            DestroyRun(fast);
            yield return null;

            PhysicalRun slow = CreateClimbRun(CharacterLiveClimbSurface.SurfaceKind.Pole, 1.01f);
            float slowStart = slow.Player.Body.position.y;
            for (var index = 0; index < 10; index++)
            {
                yield return Feed(slow.Player, 0f, true, false, true, false, false);
            }

            float slowDistance = slow.Player.Body.position.y - slowStart;
            float slowExpected = slow.Movement.Settings.ClimbSlowSpeed *
                slow.Movement.LastFixedDeltaTime * slow.Movement.PhysicsTick;
            Assert.That(slowDistance, Is.InRange(slowExpected - 0.05f, slowExpected + 0.05f));
            Assert.Greater(fastDistance, slowDistance * 1.8f);
            Debug.Log($"RMAP04 P12 shift: dt={slow.Movement.LastFixedDeltaTime:F6}, " +
                $"ticks={slow.Movement.PhysicsTick}, distance={slowDistance:F3}");
            DestroyRun(slow);
            yield return null;

            PhysicalRun down = CreateClimbRun(CharacterLiveClimbSurface.SurfaceKind.Ladder, 4f);
            float downStart = down.Player.Body.position.y;
            for (var index = 0; index < 10; index++)
            {
                yield return Feed(down.Player, 0f, false, true, false, false, false);
            }

            float downExpected = down.Movement.Settings.ClimbDownSpeed *
                down.Movement.LastFixedDeltaTime * down.Movement.PhysicsTick;
            Assert.That(downStart - down.Player.Body.position.y,
                Is.InRange(downExpected - 0.05f, downExpected + 0.05f));
            Assert.AreEqual(5f, down.Movement.Settings.ClimbDownSpeed, 0.0001f);
            Debug.Log($"RMAP04 P12 down: dt={down.Movement.LastFixedDeltaTime:F6}, " +
                $"ticks={down.Movement.PhysicsTick}, distance=" +
                $"{downStart - down.Player.Body.position.y:F3}");
            DestroyRun(down);
        }

        [UnityTest]
        public IEnumerator P13_ActualPlayer_ExitJumpReusesRunWalkAndAirControl()
        {
            ExitMeasurement fast = new ExitMeasurement();
            yield return MeasureExit(false, fast);
            Assert.That(fast.Height, Is.InRange(1.10f, 1.50f));
            Assert.That(fast.Distance, Is.InRange(2.0f, 4.25f));
            Assert.IsTrue(fast.ImmediateReentrySuppressed);

            ExitMeasurement slow = new ExitMeasurement();
            yield return MeasureExit(true, slow);
            Assert.That(slow.Height, Is.InRange(1.10f, 1.50f));
            Assert.That(slow.Distance, Is.InRange(1.25f, 2.35f));
            Assert.Less(slow.Distance, fast.Distance - 0.50f,
                "Shift exit uses the existing walk profile while ordinary air control remains active.");
            Debug.Log($"RMAP04 P13 exit: run height={fast.Height:F3}, distance={fast.Distance:F3}; " +
                $"shift height={slow.Height:F3}, distance={slow.Distance:F3}");
        }

        [UnityTest]
        public IEnumerator P15_ActualPlayer_PassesLandsAndDropsOnlySelectedOneWayPlatform()
        {
            PhysicalRun run = CreateOneWayRun();
            yield return Feed(run.Player, 0f, false, false, false, false, false);
            yield return Feed(run.Player, 0f, false, false, false, false, false);
            bool passedAbove = false;
            for (var index = 0; index < 120; index++)
            {
                yield return Feed(run.Player, 0f, false, false, false,
                    jumpPressed: index == 0, jumpHeld: true);
                passedAbove |= run.Player.Body.position.y > run.OneWay.TopWorldY + 0.05f;
                if (index > 30 && run.Movement.IsGroundedNow &&
                    run.Player.Body.position.y > run.OneWay.TopWorldY)
                {
                    break;
                }
            }

            Assert.IsTrue(passedAbove, "The actual Player must pass upward through the physical platform.");
            Assert.IsTrue(run.Movement.IsGroundedNow, "The same platform supports descent on its top face.");
            Assert.That(run.Player.Body.position.y, Is.InRange(run.OneWay.TopWorldY + 0.003f,
                run.OneWay.TopWorldY + 0.04f));

            // Position the real capsule at the outward edge; the OneWay marker must not become a Grab anchor.
            run.Player.Body.position = new Vector2(run.OneWay.PlatformCollider.bounds.max.x - 0.21f,
                run.OneWay.TopWorldY - 0.62f);
            run.Movement.ResetMotion();
            yield return Feed(run.Player, 1f, false, false, false, false, false);
            Assert.IsFalse(run.Movement.IsGrabbing);

            run.Player.Body.position = new Vector2(10f, run.OneWay.TopWorldY + 0.01f);
            run.Movement.ResetMotion();
            yield return Feed(run.Player, 0f, false, true, false, true, true);
            Assert.IsTrue(run.Movement.IsDroppingThroughOneWay);
            Assert.IsTrue(Physics2D.GetIgnoreCollision(run.Player.BodyCollider, run.OneWay.PlatformCollider));
            Assert.IsFalse(Physics2D.GetIgnoreCollision(run.Player.BodyCollider, run.Ground),
                "Drop-through never disables the unrelated physical ground.");

            for (var index = 0; index < 20; index++)
            {
                yield return Feed(run.Player, 0f, false, false, false, false, false);
            }

            Assert.IsFalse(Physics2D.GetIgnoreCollision(run.Player.BodyCollider, run.OneWay.PlatformCollider),
                "The selected platform is restored after the single 0.18-second ignore window.");
            Assert.That(run.Player.Body.position.y, Is.InRange(1.005f, 1.05f),
                "The Player falls through one-way but still lands on the real unrelated ground.");
            Debug.Log("RMAP04 P15: actual one-way passed upward, landed on top, and restored " +
                "the selected collision after 0.18 seconds.");
            DestroyRun(run);
        }

        [UnityTest]
        public IEnumerator ActualInputSystem_W_FlowsThroughSnapshotAndEntersClimb()
        {
            PhysicalRun run = CreateClimbRun(CharacterLiveClimbSurface.SurfaceKind.Ladder, 1.01f);
            run.Player.InputSource.enabled = true;
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Press(keyboard.wKey);
            yield return null;
            yield return new WaitForFixedUpdate();

            Assert.IsTrue(run.Movement.IsClimbing,
                "W must travel through Input System → snapshot → actual Player climb motor.");
            DestroyRun(run);
        }

        [UnityTest]
        public IEnumerator SavedScene_ContainsActualPlayerTriggersAndPhysicalOneWayPlatform()
        {
            EditorSceneManager.LoadSceneInPlayMode(ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return new WaitForFixedUpdate();
            CharacterLivePlayerRig player = Object.FindFirstObjectByType<CharacterLivePlayerRig>();
            CharacterLiveClimbSurface[] axes = Object.FindObjectsByType<CharacterLiveClimbSurface>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            CharacterLiveOneWayPlatform oneWay = Object.FindFirstObjectByType<CharacterLiveOneWayPlatform>();

            Assert.IsNotNull(player);
            Assert.AreEqual(new Vector2(0.4f, 0.8f), player.BodyCollider.size);
            Assert.AreEqual(2, axes.Length);
            Assert.IsTrue(axes[0].AxisTrigger.isTrigger && axes[1].AxisTrigger.isTrigger);
            Assert.IsNotNull(oneWay.GetComponent<PlatformEffector2D>());
            Assert.IsFalse(oneWay.GetComponent<CharacterLiveGrabSurface>().IsGrabAllowed);
        }

        private IEnumerator MeasureExit(bool walkHeld, ExitMeasurement measurement)
        {
            PhysicalRun run = CreateClimbRun(CharacterLiveClimbSurface.SurfaceKind.Ladder, 2f);
            yield return Feed(run.Player, 0f, true, false, walkHeld, false, false);
            Assert.IsTrue(run.Movement.IsClimbing);
            float startY = run.Player.Body.position.y;
            float startX = run.Player.Body.position.x;
            yield return Feed(run.Player, 1f, false, false, walkHeld, true, true);
            Assert.IsFalse(run.Movement.IsClimbing);
            float peak = run.Player.Body.position.y;
            float priorVelocity = run.Movement.Velocity.x;
            yield return Feed(run.Player, -1f, true, false, walkHeld, false, true);
            Assert.Less(run.Movement.Velocity.x, priorVelocity,
                "After exit the existing air-control motor accepts a new horizontal input.");
            measurement.ImmediateReentrySuppressed = !run.Movement.IsClimbing;

            bool returnedToLaunchHeight = false;
            for (var index = 0; index < 100; index++)
            {
                yield return Feed(run.Player, 1f, false, false, walkHeld, false, true);
                peak = Mathf.Max(peak, run.Player.Body.position.y);
                if (peak > startY + 0.05f && run.Player.Body.position.y <= startY)
                {
                    returnedToLaunchHeight = true;
                    break;
                }
            }

            Assert.IsTrue(returnedToLaunchHeight,
                "Exit measurement returns to its launch height before ground contact.");
            measurement.Height = peak - startY;
            measurement.Distance = run.Player.Body.position.x - startX;
            DestroyRun(run);
            yield return null;
        }

        private static PhysicalRun CreateClimbRun(
            CharacterLiveClimbSurface.SurfaceKind kind,
            float feetY)
        {
            var run = CreateBaseRun("RMAP04_ClimbFixture", new Vector2(5f, feetY));
            var axis = new GameObject("RMAP04_" + kind, typeof(BoxCollider2D),
                typeof(CharacterLiveClimbSurface));
            axis.transform.SetParent(run.Root.transform, false);
            axis.transform.position = new Vector2(5f, 3.5f);
            run.AxisCollider = axis.GetComponent<BoxCollider2D>();
            run.AxisCollider.size = new Vector2(0.7f, 5f);
            run.AxisCollider.isTrigger = true;
            axis.GetComponent<CharacterLiveClimbSurface>().Configure(kind, run.AxisCollider);
            Physics2D.SyncTransforms();
            return run;
        }

        private static PhysicalRun CreateOneWayRun()
        {
            var run = CreateBaseRun("RMAP04_OneWayFixture", new Vector2(10f, 1.01f));
            var platform = new GameObject("RMAP04_OneWay", typeof(BoxCollider2D),
                typeof(PlatformEffector2D), typeof(CharacterLiveOneWayPlatform),
                typeof(CharacterLiveGrabSurface));
            platform.transform.SetParent(run.Root.transform, false);
            platform.transform.position = new Vector2(10f, 2f);
            run.OneWay = platform.GetComponent<CharacterLiveOneWayPlatform>();
            BoxCollider2D collider = platform.GetComponent<BoxCollider2D>();
            collider.size = new Vector2(5f, 0.25f);
            collider.usedByEffector = true;
            run.OneWay.Configure(collider);
            platform.GetComponent<CharacterLiveGrabSurface>().Configure(
                CharacterLiveGrabSurface.SurfaceKind.OneWay);
            Physics2D.SyncTransforms();
            return run;
        }

        private static PhysicalRun CreateBaseRun(string name, Vector2 startFeet)
        {
            var run = new PhysicalRun();
            run.Root = new GameObject(name);
            var ground = new GameObject("Ground", typeof(BoxCollider2D));
            ground.transform.SetParent(run.Root.transform, false);
            ground.transform.position = new Vector2(10f, 0.5f);
            run.Ground = ground.GetComponent<BoxCollider2D>();
            run.Ground.size = new Vector2(30f, 1f);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            var playerHost = new GameObject("RMAP04_ActualPlayer");
            playerHost.transform.SetParent(run.Root.transform, false);
            playerHost.SetActive(false);
            GameObject playerObject = Object.Instantiate(prefab, playerHost.transform);
            run.Player = playerObject.GetComponent<CharacterLivePlayerRig>();
            run.Movement = playerObject.GetComponent<CharacterLiveMovementDriver>();
            run.Player.BodyCollider.size = new Vector2(0.4f, 0.8f);
            run.Player.BodyCollider.offset = new Vector2(0f, 0.4f);
            run.Movement.ConfigureRmap02(1);
            playerHost.SetActive(true);
            run.Player.Body.position = startFeet;
            run.Movement.ResetMotion();
            run.Player.InputSource.enabled = false;
            Physics2D.SyncTransforms();
            return run;
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

        private static void DestroyRun(PhysicalRun run)
        {
            if (run != null && run.Root != null)
            {
                Object.DestroyImmediate(run.Root);
                Physics2D.SyncTransforms();
            }
        }

        private sealed class PhysicalRun
        {
            public GameObject Root;
            public CharacterLivePlayerRig Player;
            public CharacterLiveMovementDriver Movement;
            public BoxCollider2D AxisCollider;
            public BoxCollider2D Ground;
            public CharacterLiveOneWayPlatform OneWay;
        }

        private sealed class ExitMeasurement
        {
            public float Height;
            public float Distance;
            public bool ImmediateReentrySuppressed;
        }
    }
}
#endif
