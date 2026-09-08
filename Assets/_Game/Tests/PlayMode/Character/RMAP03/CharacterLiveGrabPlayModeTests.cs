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
                yield return FeedFixed(run.Player, horizontal: 1f, down: false,
                    jumpPressed: false, jumpHeld: false);

                Assert.IsTrue(run.Movement.IsGrabbing, kind + " must be a physical safe Grab case.");
                Assert.IsTrue(run.Movement.HasSafeGrabContact);
                Assert.AreEqual(new Vector2(0.4f, 0.8f), run.Player.BodyCollider.size);
                Assert.AreEqual(new Vector2(0f, 0.4f), run.Player.BodyCollider.offset);
                Assert.IsNotNull(run.SurfaceCollider);
                if (kind == CharacterLiveGrabSurface.SurfaceKind.StaticSafe)
                {
                    Assert.IsInstanceOf<TilemapCollider2D>(run.SurfaceCollider);
                    Assert.IsNotNull(run.SurfaceCollider.GetComponent<CompositeCollider2D>());
                }

                Object.Destroy(run.Root);
                yield return null;
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
                float startY = run.Player.Body.position.y;
                for (var index = 0; index < 4; index++)
                {
                    yield return FeedFixed(run.Player, horizontal: 1f, down: false,
                        jumpPressed: false, jumpHeld: false);
                }

                Assert.IsFalse(run.Movement.IsGrabbing, kind + " must not be a Grab anchor.");
                if (run.SurfaceCollider != null)
                {
                    Assert.IsTrue(run.SurfaceCollider.enabled,
                        "Forbidden classification must not disable its physical collision.");
                }
                Assert.Less(run.Player.Body.position.y, startY,
                    "Forbidden surface leaves the Player in normal physical fall.");

                Object.Destroy(run.Root);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator RMAP03_EntryAllowsDescentButSuppressesAscentAndDown()
        {
            GrabRun descent = CreateRun(CharacterLiveGrabSurface.SurfaceKind.StaticSafe);
            yield return FeedFixed(descent.Player, 1f, false, false, false);
            Assert.IsTrue(descent.Movement.IsGrabbing, "Horizontal/descent entry grabs the exposed corner.");
            Object.Destroy(descent.Root);
            yield return null;

            GrabRun ascent = CreateRun(CharacterLiveGrabSurface.SurfaceKind.StaticSafe);
            ascent.Player.Body.position = new Vector2(7.2f, 1f);
            ascent.Movement.ResetMotion();
            yield return FeedFixed(ascent.Player, 1f, false, true, true);
            for (var index = 0; index < 4; index++)
            {
                yield return FeedFixed(ascent.Player, 1f, false, false, true);
            }
            Assert.Greater(ascent.Player.Body.position.y, 1.1f,
                "The Player is physically ascending past the corner.");
            Assert.IsFalse(ascent.Movement.IsGrabbing,
                "An upward jump must retain the existing jump/collision flow instead of auto-Grab.");
            Object.Destroy(ascent.Root);
            yield return null;

            GrabRun down = CreateRun(CharacterLiveGrabSurface.SurfaceKind.StaticSafe);
            float downStartY = down.Player.Body.position.y;
            for (var index = 0; index < 4; index++)
            {
                yield return FeedFixed(down.Player, 1f, true, false, false);
            }
            Assert.IsFalse(down.Movement.IsGrabbing);
            Assert.Less(down.Player.Body.position.y, downStartY,
                "Down held takes priority and leaves the Player falling.");
            Object.Destroy(down.Root);
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
            yield return FeedFixed(run.Player, 1f, false, false, false);
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
            Object.Destroy(run.Root);
            yield return null;

            GrabRun reentry = CreateRun(CharacterLiveGrabSurface.SurfaceKind.StaticSafe);
            yield return FeedFixed(reentry.Player, 1f, false, false, false);
            Assert.IsTrue(reentry.Movement.IsGrabbing);
            yield return FeedFixed(reentry.Player, 0f, true, false, false);
            for (var index = 0; index < 5; index++)
            {
                yield return FeedFixed(reentry.Player, 0f, false, false, false);
            }
            Assert.IsFalse(reentry.Movement.IsGrabbing,
                "The 0.12s reentry delay preserves an explicit drop input.");
            Object.Destroy(reentry.Root);
        }

        [UnityTest]
        public IEnumerator RMAP03_MovingSafeSolid_CarriesGrabAndDangerTransitionReleases()
        {
            GrabRun run = CreateRun(CharacterLiveGrabSurface.SurfaceKind.MovingSafe);
            yield return FeedFixed(run.Player, 1f, false, false, false);
            Assert.IsTrue(run.Movement.IsGrabbing);
            float platformStart = run.SurfaceCollider.bounds.center.x;
            float playerStart = run.Player.Body.position.x;
            for (var index = 0; index < 30; index++)
            {
                yield return FeedFixed(run.Player, 0f, false, false, false);
            }

            Assert.Greater(run.SurfaceCollider.bounds.center.x, platformStart + 0.30f,
                "The fixture moves a Kinematic Rigidbody2D/Collider2D along its authored path.");
            Assert.Greater(run.Player.Body.position.x, playerStart + 0.30f,
                "The Player retains its local corner anchor while the real safe solid moves.");
            run.Surface.SetDangerous(true);
            yield return FeedFixed(run.Player, 0f, false, false, false);
            Assert.IsFalse(run.Movement.IsGrabbing,
                "A moving surface danger transition releases Grab immediately.");
            Object.Destroy(run.Root);
        }

        [UnityTest]
        public IEnumerator RMAP03_SavedScene_ContainsPlayerSurfaceClassificationAndMovingCollider()
        {
            EditorSceneManager.LoadSceneInPlayMode(ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return new WaitForFixedUpdate();
            CharacterLivePlayerRig player = Object.FindFirstObjectByType<CharacterLivePlayerRig>();
            CharacterLiveGrabSurface moving = Object.FindFirstObjectByType<CharacterLiveGrabSurface>(
                FindObjectsInactive.Exclude);
            CharacterLiveGrabMovingSolid mover = Object.FindFirstObjectByType<CharacterLiveGrabMovingSolid>();
            TilemapCollider2D tilemapCollider = Object.FindFirstObjectByType<TilemapCollider2D>();
            CharacterLiveCameraFollowDriver camera = Object.FindFirstObjectByType<CharacterLiveCameraFollowDriver>();

            Assert.IsNotNull(player);
            Assert.AreEqual(new Vector2(0.4f, 0.8f), player.BodyCollider.size);
            Assert.IsNotNull(tilemapCollider.GetComponent<CharacterLiveGrabSurface>());
            Assert.IsNotNull(tilemapCollider.GetComponent<CompositeCollider2D>());
            Assert.IsNotNull(mover.GetComponent<Rigidbody2D>());
            Assert.AreEqual(new Vector2(12f, 8f), camera.VisibleWorldSize);
            Assert.IsNotNull(moving);
        }

        private static GrabRun CreateRun(CharacterLiveGrabSurface.SurfaceKind kind)
        {
            var run = new GrabRun();
            run.Root = new GameObject("RMAP03_ActualGrabFixture", typeof(Grid));
            CreateGround(run.Root.transform);
            if (kind == CharacterLiveGrabSurface.SurfaceKind.StaticSafe)
            {
                CreateStaticSafeTilemap(run);
            }
            else
            {
                CreateBoxSurface(run, kind);
            }

            Physics2D.SyncTransforms();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            var playerHost = new GameObject("RMAP03_ActualPlayer");
            playerHost.transform.SetParent(run.Root.transform, false);
            playerHost.SetActive(false);
            GameObject playerObject = Object.Instantiate(prefab, playerHost.transform);
            run.Player = playerObject.GetComponent<CharacterLivePlayerRig>();
            run.Movement = playerObject.GetComponent<CharacterLiveMovementDriver>();
            run.Player.BodyCollider.size = new Vector2(0.4f, 0.8f);
            run.Player.BodyCollider.offset = new Vector2(0f, 0.4f);
            run.Movement.ConfigureRmap02(1);
            playerHost.SetActive(true);
            run.Player.Body.position = new Vector2(7.76f, 2.46f);
            run.Movement.ResetMotion();
            run.Player.InputSource.enabled = false;
            return run;
        }

        private static void CreateGround(Transform parent)
        {
            var ground = new GameObject("PhysicalGround", typeof(BoxCollider2D));
            ground.transform.SetParent(parent, false);
            ground.transform.position = new Vector2(3.75f, 0.5f);
            ground.GetComponent<BoxCollider2D>().size = new Vector2(7.5f, 1f);
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
            run.Surface.Configure(CharacterLiveGrabSurface.SurfaceKind.StaticSafe);
            run.SurfaceCollider = collider;
        }

        private static void CreateBoxSurface(GrabRun run, CharacterLiveGrabSurface.SurfaceKind kind)
        {
            var surface = new GameObject("RMAP03_" + kind, typeof(CharacterLiveGrabSurface));
            surface.transform.SetParent(run.Root.transform, false);
            surface.transform.position = new Vector2(8.5f, 2f);
            run.Surface = surface.GetComponent<CharacterLiveGrabSurface>();
            run.Surface.Configure(kind);
            if (kind != CharacterLiveGrabSurface.SurfaceKind.Decoration)
            {
                run.SurfaceCollider = surface.AddComponent<BoxCollider2D>();
                run.SurfaceCollider.GetComponent<BoxCollider2D>().size = new Vector2(1f, 2f);
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

        private sealed class GrabRun
        {
            public GameObject Root;
            public CharacterLivePlayerRig Player;
            public CharacterLiveMovementDriver Movement;
            public CharacterLiveGrabSurface Surface;
            public Collider2D SurfaceCollider;
        }
    }
}
#endif
