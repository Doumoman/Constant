#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using StarNight.Character.Live.Cameras;
using StarNight.Character.Live.Input;
using StarNight.Character.Live.Movement;
using StarNight.Character.Live.Player;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace StarNight.Character.Tests.PlayMode.Rmap02
{
    [Category("RMAP02")]
    public sealed class GeneratedTilemapPlayerRunPlayModeTests : InputTestFixture
    {
        private const string ScenePath =
            "Assets/_Game/Map/Scenes/MoonPalace/RMAP02/MoonPalacePlayerTilemapRun_RMAP02.unity";

        private Keyboard keyboard;

        public override void Setup()
        {
            base.Setup();
        }

        [UnityTest]
        public IEnumerator RMAP02_ActualPlayer_CollidesAndReachesExit()
        {
            PhysicalRun run = CreatePhysicalRun();
            CharacterLivePlayerRig player = run.Player;
            CharacterLiveMovementDriver movement = run.Movement;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.IsNotNull(run.TerrainCollider);
            Assert.IsNotNull(run.TerrainCollider.GetComponent<CompositeCollider2D>());
            Assert.AreEqual(new Vector2(0.4f, 0.8f), player.BodyCollider.size);
            Assert.AreEqual(new Vector2(0f, 0.4f), player.BodyCollider.offset);
            Assert.AreEqual(3f, player.Body.position.x, 0.001f);
            Assert.AreEqual(1f, player.Body.position.y, 0.02f);
            Assert.IsNotNull(run.TerrainCollider.GetComponent<Tilemap>().GetTile(
                new Vector3Int(12, 1, 0)));

            keyboard = InputSystem.AddDevice<Keyboard>();
            Press(keyboard.leftShiftKey);
            Press(keyboard.dKey);
            yield return null;
            float walkStart = player.Body.position.x;
            yield return WaitForFixedSteps(25);
            float walkDistance = player.Body.position.x - walkStart;

            keyboard = InputSystem.AddDevice<Keyboard>();
            Press(keyboard.dKey);
            yield return null;
            float runStart = player.Body.position.x;
            yield return WaitForFixedSteps(25);
            float runDistance = player.Body.position.x - runStart;
            Assert.Greater(runDistance, walkDistance + 0.35f,
                "Shift-held walking must remain observably slower than default running.");

            yield return WaitUntilX(player, 10.5f, 120);
            Assert.GreaterOrEqual(player.Body.position.x, 10.5f);
            keyboard = InputSystem.AddDevice<Keyboard>();
            Press(keyboard.dKey);
            Press(keyboard.spaceKey);
            yield return null;
            yield return WaitForFixedSteps(12);
            keyboard = InputSystem.AddDevice<Keyboard>();
            Press(keyboard.dKey);
            yield return null;
            yield return WaitUntilX(player, 15.8f, 160);
            Assert.GreaterOrEqual(player.Body.position.x, 15.8f,
                "The one-tile step requires and accepts a physical jump.");

            keyboard = InputSystem.AddDevice<Keyboard>();
            Press(keyboard.dKey);
            Press(keyboard.spaceKey);
            yield return null;
            float ceilingPeak = player.Body.position.y;
            for (var index = 0; index < 30; index++)
            {
                yield return new WaitForFixedUpdate();
                ceilingPeak = Mathf.Max(ceilingPeak, player.Body.position.y);
            }

            Assert.LessOrEqual(ceilingPeak, 1.25f,
                "The 0.8-tile Player must hit the real low ceiling rather than pass through it.");

            keyboard = InputSystem.AddDevice<Keyboard>();
            Press(keyboard.dKey);
            for (var index = 0; index < 320 && !run.Exit.HasBeenReached; index++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.IsTrue(run.Exit.HasBeenReached,
                "The real Player reaches the x=42 exit through the Tilemap course.");
            Assert.IsTrue(movement.IsDriving);
            Assert.AreEqual(new Vector2(12f, 8f), run.Follow.VisibleWorldSize);
            Camera camera = run.Follow.GetComponent<Camera>();
            Assert.IsNotNull(camera);
            Assert.AreEqual(4f, camera.orthographicSize, 0.001f);
            Assert.GreaterOrEqual(camera.transform.position.x, 6f);
            Assert.LessOrEqual(camera.transform.position.x, 54f);
            Assert.AreEqual(1.5f, camera.pixelRect.width / camera.pixelRect.height,
                0.01f, "The effective Game viewport remains exactly 12:8.");

            Object.Destroy(run.Root);
        }

        [UnityTest]
        public IEnumerator RMAP02_ActualPlayer_UsesCoyoteAndJumpBufferOnPhysicalGap()
        {
            GameObject fixture = new GameObject("RMAP02_CoyoteBuffer_PhysicalFixture",
                typeof(Grid));
            fixture.transform.position = new Vector3(0f, 10f, 0f);
            GameObject terrainObject = new GameObject("Terrain", typeof(Tilemap));
            terrainObject.transform.SetParent(fixture.transform, false);
            Tilemap terrain = terrainObject.GetComponent<Tilemap>();
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(
                "Assets/_Game/Live/Prefabs/RMAP02/Tiles/RMAP02_Terrain.asset");
            for (var x = 0; x <= 3; x++)
            {
                terrain.SetTile(new Vector3Int(x, 0, 0), tile);
            }

            for (var x = 5; x <= 12; x++)
            {
                terrain.SetTile(new Vector3Int(x, 0, 0), tile);
            }

            TilemapCollider2D terrainCollider = terrainObject.AddComponent<TilemapCollider2D>();
            terrainObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            terrainObject.AddComponent<CompositeCollider2D>();
            terrainCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            Physics2D.SyncTransforms();

            GameObject playerHost = new GameObject("RMAP02_CoyoteBuffer_PlayerHost");
            playerHost.SetActive(false);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab");
            GameObject playerObject = Object.Instantiate(prefab, playerHost.transform);
            playerObject.transform.position = new Vector3(3.5f, 11f, 0f);
            var player = playerObject.GetComponent<CharacterLivePlayerRig>();
            var movement = playerObject.GetComponent<CharacterLiveMovementDriver>();
            player.BodyCollider.size = new Vector2(0.4f, 0.8f);
            player.BodyCollider.offset = new Vector2(0f, 0.4f);
            movement.ConfigureRmap02(1);
            playerHost.SetActive(true);
            player.Body.position = new Vector2(3.5f, 11f);
            movement.ResetMotion();

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.AreEqual(0.08f, movement.Settings.CoyoteTime, 0.0001f);
            Assert.AreEqual(0.10f, movement.Settings.JumpBufferTime, 0.0001f);
            player.InputSource.enabled = false;

            for (var index = 0; index < 36 && (movement.IsGroundedNow ||
                player.Body.position.x < 4.05f); index++)
            {
                FeedInput(player, 1f, jumpPressed: false, jumpHeld: false);
                yield return new WaitForFixedUpdate();
            }

            Assert.IsFalse(movement.IsGroundedNow,
                "The Player has physically left the one-cell Terrain gap.");
            FeedInput(player, 1f, jumpPressed: true, jumpHeld: true);
            yield return new WaitForFixedUpdate();
            for (var index = 0; index < 5; index++)
            {
                FeedInput(player, 1f, jumpPressed: false, jumpHeld: true);
                yield return new WaitForFixedUpdate();
            }
            Assert.Greater(player.Body.position.y, 11.15f,
                "A jump pressed within 0.08s of the physical ledge uses coyote time.");

            Object.Destroy(playerHost);
            yield return null;

            playerHost = new GameObject("RMAP02_Buffer_PlayerHost");
            playerHost.SetActive(false);
            playerObject = Object.Instantiate(prefab, playerHost.transform);
            playerObject.transform.position = new Vector3(3.5f, 11f, 0f);
            player = playerObject.GetComponent<CharacterLivePlayerRig>();
            movement = playerObject.GetComponent<CharacterLiveMovementDriver>();
            player.BodyCollider.size = new Vector2(0.4f, 0.8f);
            player.BodyCollider.offset = new Vector2(0f, 0.4f);
            movement.ConfigureRmap02(1);
            playerHost.SetActive(true);
            player.Body.position = new Vector2(3.5f, 11f);
            movement.ResetMotion();
            yield return new WaitForFixedUpdate();
            player.InputSource.enabled = false;
            for (var index = 0; index < 48 && !(player.Body.position.x > 4.45f &&
                !movement.IsGroundedNow && player.Body.position.y < 10.96f); index++)
            {
                FeedInput(player, 1f, jumpPressed: false, jumpHeld: false);
                yield return new WaitForFixedUpdate();
            }

            Assert.IsFalse(movement.IsGroundedNow);
            Assert.Less(player.Body.position.y, 10.96f,
                "The buffer input is issued immediately before physical landing.");
            FeedInput(player, 1f, jumpPressed: true, jumpHeld: true);
            yield return new WaitForFixedUpdate();
            for (var index = 0; index < 11; index++)
            {
                FeedInput(player, 1f, jumpPressed: false, jumpHeld: true);
                yield return new WaitForFixedUpdate();
            }
            Assert.Greater(player.Body.position.y, 11.05f,
                "A 0.10s buffered jump starts on the physical landing frame.");

            Object.Destroy(playerHost);
            Object.Destroy(fixture);
        }

        [UnityTest]
        public IEnumerator RMAP02_SavedScene_UsesPlayerTilemapAndCameraComponents()
        {
            EditorSceneManager.LoadSceneInPlayMode(ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            CharacterLiveMapRunBootstrap bootstrap = Object.FindFirstObjectByType<
                CharacterLiveMapRunBootstrap>();
            CharacterLivePlayerRig player = Object.FindFirstObjectByType<
                CharacterLivePlayerRig>();
            TilemapCollider2D terrainCollider = Object.FindFirstObjectByType<TilemapCollider2D>();
            CharacterLiveCameraFollowDriver follow = Object.FindFirstObjectByType<
                CharacterLiveCameraFollowDriver>();

            Assert.IsTrue(bootstrap.HasSpawned);
            Assert.AreEqual(new Vector2(0.4f, 0.8f), player.BodyCollider.size);
            Assert.AreEqual(new Vector2(0f, 0.4f), player.BodyCollider.offset);
            Assert.IsNotNull(terrainCollider.GetComponent<CompositeCollider2D>());
            Assert.IsNotNull(terrainCollider.GetComponent<Tilemap>().GetTile(
                new Vector3Int(12, 1, 0)));
            Assert.AreEqual(new Vector2(12f, 8f), follow.VisibleWorldSize);
        }

        [UnityTest]
        public IEnumerator RMAP02_ActualCamera_PreservesTwelveByEightAtRequiredAspects()
        {
            PhysicalRun run = CreatePhysicalRun();
            Camera camera = run.Follow.GetComponent<Camera>();
            int previousWidth = Screen.width;
            int previousHeight = Screen.height;

            foreach (Vector2Int resolution in new[]
                     {
                         new Vector2Int(1500, 1000),
                         new Vector2Int(1600, 900),
                         new Vector2Int(2000, 900)
                     })
            {
                Screen.SetResolution(resolution.x, resolution.y, FullScreenMode.Windowed);
                yield return null;
                run.Follow.SendMessage("LateUpdate");
                Assert.AreEqual(1.5f, camera.pixelRect.width / camera.pixelRect.height, 0.01f,
                    $"{resolution.x}x{resolution.y} keeps the effective 12:8 viewport.");
                Assert.AreEqual(4f, camera.orthographicSize, 0.001f);
                Assert.AreEqual(12f, camera.orthographicSize * 2f * camera.aspect, 0.02f);
            }

            run.Player.transform.position = new Vector3(100f, 100f, 0f);
            run.Follow.SnapToTarget();
            Assert.AreEqual(54f, camera.transform.position.x, 0.001f);
            Assert.AreEqual(36f, camera.transform.position.y, 0.001f);
            run.Player.transform.position = new Vector3(-100f, -100f, 0f);
            run.Follow.SnapToTarget();
            Assert.AreEqual(6f, camera.transform.position.x, 0.001f);
            Assert.AreEqual(4f, camera.transform.position.y, 0.001f);

            Screen.SetResolution(previousWidth, previousHeight, FullScreenMode.Windowed);
            Object.Destroy(run.Root);
        }

        [UnityTest]
        public IEnumerator RMAP02_PhysicalJump_MeasuresHeightRunAndReleaseCut()
        {
            PhysicalRun run = CreatePhysicalRun();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            run.Player.InputSource.enabled = false;

            float startY = run.Player.Body.position.y;
            float startX = run.Player.Body.position.x;
            float fullPeak = startY;
            float landingX = startX;
            bool leftGround = false;
            for (var index = 0; index < 100; index++)
            {
                FeedInput(run.Player, 1f, jumpPressed: index == 0, jumpHeld: true);
                yield return new WaitForFixedUpdate();
                fullPeak = Mathf.Max(fullPeak, run.Player.Body.position.y);
                leftGround |= !run.Movement.IsGroundedNow;
                if (leftGround && run.Movement.IsGroundedNow && index > 4)
                {
                    landingX = run.Player.Body.position.x;
                    break;
                }
            }

            float fullHeight = fullPeak - startY;
            float runDistance = landingX - startX;
            Assert.That(fullHeight, Is.InRange(1.10f, 1.50f),
                "A held jump measures about 1.3 tiles from the actual feet pivot.");
            Assert.That(runDistance, Is.InRange(2.0f, 4.25f),
                "A held default-run jump stays within the authored roughly four-tile range.");

            run.Player.Body.position = new Vector2(3f, 1f);
            run.Movement.ResetMotion();
            yield return new WaitForFixedUpdate();
            float cutPeak = run.Player.Body.position.y;
            for (var index = 0; index < 60; index++)
            {
                FeedInput(run.Player, 0f, jumpPressed: index == 0, jumpHeld: false);
                yield return new WaitForFixedUpdate();
                cutPeak = Mathf.Max(cutPeak, run.Player.Body.position.y);
            }

            float cutHeight = cutPeak - 1f;
            Assert.Less(cutHeight, fullHeight - 0.30f,
                "Releasing Jump applies the RMAP02 variable-height cut on the real motor.");
            Debug.Log($"RMAP02 P02/P03 physical measure: fullHeight={fullHeight:F3}, " +
                $"runDistance={runDistance:F3}, releaseCutHeight={cutHeight:F3}");
            Object.Destroy(run.Root);
        }

        private static PhysicalRun CreatePhysicalRun()
        {
            var run = new PhysicalRun();
            run.Root = new GameObject("RMAP02_ActualPhysicsRun", typeof(Grid));
            GameObject terrainObject = new GameObject("Terrain", typeof(Tilemap));
            terrainObject.transform.SetParent(run.Root.transform, false);
            Tilemap terrain = terrainObject.GetComponent<Tilemap>();
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(
                "Assets/_Game/Live/Prefabs/RMAP02/Tiles/RMAP02_Terrain.asset");
            for (var x = 0; x < 60; x++)
            {
                terrain.SetTile(new Vector3Int(x, 0, 0), tile);
            }

            terrain.SetTile(new Vector3Int(12, 1, 0), tile);
            for (var x = 18; x <= 22; x++)
            {
                terrain.SetTile(new Vector3Int(x, 2, 0), tile);
            }

            for (var y = 1; y <= 6; y++)
            {
                terrain.SetTile(new Vector3Int(50, y, 0), tile);
            }

            run.TerrainCollider = terrainObject.AddComponent<TilemapCollider2D>();
            terrainObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            terrainObject.AddComponent<CompositeCollider2D>();
            run.TerrainCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

            var playerHost = new GameObject("RMAP02_ActualPhysicsPlayer");
            playerHost.transform.SetParent(run.Root.transform, false);
            playerHost.SetActive(false);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab");
            GameObject playerObject = Object.Instantiate(prefab, playerHost.transform);
            playerObject.transform.position = new Vector3(3f, 1f, 0f);
            run.Player = playerObject.GetComponent<CharacterLivePlayerRig>();
            run.Movement = playerObject.GetComponent<CharacterLiveMovementDriver>();
            run.Player.BodyCollider.size = new Vector2(0.4f, 0.8f);
            run.Player.BodyCollider.offset = new Vector2(0f, 0.4f);
            run.Movement.ConfigureRmap02(1);
            playerHost.SetActive(true);
            run.Player.Body.position = new Vector2(3f, 1f);
            run.Movement.ResetMotion();

            var exit = new GameObject("Exit", typeof(BoxCollider2D),
                typeof(CharacterLiveMapRunExit));
            exit.transform.SetParent(run.Root.transform, false);
            exit.transform.position = new Vector3(42.5f, 1.9f, 0f);
            BoxCollider2D exitCollider = exit.GetComponent<BoxCollider2D>();
            exitCollider.isTrigger = true;
            exitCollider.size = new Vector2(1f, 1.5f);
            run.Exit = exit.GetComponent<CharacterLiveMapRunExit>();

            var cameraObject = new GameObject("RMAP02_FollowCamera", typeof(Camera),
                typeof(CharacterLiveCameraFollowDriver));
            cameraObject.transform.SetParent(run.Root.transform, false);
            run.Follow = cameraObject.GetComponent<CharacterLiveCameraFollowDriver>();
            run.Follow.Configure(cameraObject.GetComponent<Camera>(), run.Player.transform,
                new Rect(0f, 0f, 60f, 40f), 12f, 8f, 0.08f);
            Physics2D.SyncTransforms();
            return run;
        }

        private sealed class PhysicalRun
        {
            public GameObject Root;
            public CharacterLivePlayerRig Player;
            public CharacterLiveMovementDriver Movement;
            public CharacterLiveMapRunExit Exit;
            public TilemapCollider2D TerrainCollider;
            public CharacterLiveCameraFollowDriver Follow;
        }

        private static IEnumerator WaitForFixedSteps(int count)
        {
            for (var index = 0; index < count; index++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        private static void FeedInput(
            CharacterLivePlayerRig player,
            float horizontal,
            bool jumpPressed,
            bool jumpHeld)
        {
            var jump = new CharacterLiveButtonFrame(
                pressedThisFrame: jumpPressed, releasedThisFrame: false, isHeld: jumpHeld);
            var idle = new CharacterLiveButtonFrame(
                pressedThisFrame: false, releasedThisFrame: false, isHeld: false);
            player.InputSource.Adapter.AccumulateFrame(
                horizontalAxis: horizontal,
                isDownHeld: false,
                jumpFrame: jump,
                actionFrame: idle,
                bombFrame: idle,
                ropeFrame: idle);
        }

        private static IEnumerator WaitUntilX(
            StarNight.Character.Live.Player.CharacterLivePlayerRig player,
            float targetX,
            int maximumSteps)
        {
            for (var index = 0; index < maximumSteps && player.Body.position.x < targetX; index++)
            {
                yield return new WaitForFixedUpdate();
            }
        }
    }
}
#endif
