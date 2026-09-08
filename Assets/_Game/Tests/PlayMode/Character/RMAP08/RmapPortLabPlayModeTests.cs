#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using StarNight.Character.Live.Input;
using StarNight.Character.Live.Movement;
using StarNight.Character.Live.Player;
using StarNight.Map.WorldGeneration.MicroPatterns;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace StarNight.Character.Tests.PlayMode.Rmap08
{
    [Category("RMAP08")]
    public sealed class RmapPortLabPlayModeTests
    {
        private const string ScenePath =
            "Assets/_Game/Map/Scenes/MoonPalace/RMAP08/MoonPalacePortLab_RMAP08.unity";
        private const string PlayerPrefabPath = "Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab";

        [UnityTest]
        public IEnumerator A05_A18_SavedPortLab_ContainsActualPlayerAndPhysicalTilemapColliderForEveryChunk()
        {
            yield return LoadLab();
            RmapPortCatalogSnapshot catalog = RmapPortCatalog.BuildFixture();
            CharacterLivePlayerRig savedPlayer = Object.FindFirstObjectByType<CharacterLivePlayerRig>();
            Tilemap[] tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            Assert.IsNotNull(savedPlayer);
            Assert.AreEqual("RMAP08_Player", savedPlayer.name);
            Assert.AreEqual(new Vector2(0.4f, 0.8f), savedPlayer.BodyCollider.size);
            foreach (RmapPortChunk chunk in catalog.Chunks)
            {
                Tilemap tilemap = tilemaps.Single(value => value.name == "RMAP08_Chunk_" + chunk.ChunkId);
                TilemapCollider2D collider = tilemap.GetComponent<TilemapCollider2D>();
                Assert.IsNotNull(collider, chunk.ChunkId + " must expose actual TilemapCollider2D terrain.");
                Assert.IsNotNull(tilemap.GetComponent<Rigidbody2D>());
                Assert.IsTrue(tilemap.cellBounds.size.x > 0 || chunk.SpaceState == RmapPortSpaceState.SpecialReserved);
            }
        }

        [UnityTest]
        public IEnumerator A19_ActualPlayer_CrossesOpenBoundaryAndIsBlockedByInactiveSolidCollider()
        {
            yield return LoadLab();
            CharacterLivePlayerRig savedPlayer = DisableSavedPlayer();
            Assert.IsNotNull(savedPlayer);
            CharacterLivePlayerRig player = CreateActualPlayer(new Vector2(2f, 1.01f));
            CharacterLiveMovementDriver movement = player.GetComponent<CharacterLiveMovementDriver>();

            for (var index = 0; index < 140; index++) yield return Feed(player, 1f, false, false);
            Assert.Greater(player.Body.position.x, 12.5f,
                "The real Player crosses T1_A.R -> T1_B.L through matching R/L OpenCells.");

            player.Body.position = new Vector2(30f, 1.01f);
            movement.ResetMotion();
            Physics2D.SyncTransforms();
            for (var index = 0; index < 180; index++) yield return Feed(player, 1f, false, false);
            Tilemap blocked = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .Single(value => value.name == "RMAP08_Chunk_INACTIVE_SOLID_WALL");
            Assert.Less(player.Body.position.x, blocked.GetComponent<TilemapCollider2D>().bounds.min.x - 0.15f,
                "INACTIVE_SOLID has no EdgePort and its physical Tilemap collider stops the actual Player.");
            Object.Destroy(player.transform.parent.gameObject);
        }

        [UnityTest]
        public IEnumerator A20_ActualPlayer_UsesType2SideToDownAndType3UpwardOnlyFixturePaths()
        {
            yield return LoadLab();
            DisableSavedPlayer();
            CharacterLivePlayerRig player = CreateActualPlayer(new Vector2(62f, 1.01f));
            CharacterLiveMovementDriver movement = player.GetComponent<CharacterLiveMovementDriver>();
            bool crossedDropOpening = false;
            bool leftUpperFloor = false;
            for (var index = 0; index < 160; index++)
            {
                yield return Feed(player, 1f, false, false);
                crossedDropOpening |= player.Body.position.x >= 66.7f;
                leftUpperFloor |= crossedDropOpening && !movement.IsGroundedNow;
                if (leftUpperFloor) break;
            }
            Assert.IsTrue(crossedDropOpening, "Input-driven Player movement reaches T2's declared D-port opening.");
            Assert.IsTrue(leftUpperFloor, "The Player leaves the source floor through the actual 4-cell downward opening.");
            for (var index = 0; index < 120; index++) yield return Feed(player, 0f, false, false);
            Assert.IsTrue(movement.IsGroundedNow, "The Player lands on the physical T2 lower Tilemap landing.");
            Assert.Less(player.Body.position.y, -1.7f, "Type2's fixture reaches its lower D-side landing, not an implied reverse link.");
            Object.Destroy(player.transform.parent.gameObject);

            player = CreateActualPlayer(new Vector2(83.5f, 1.01f));
            movement = player.GetComponent<CharacterLiveMovementDriver>();
            bool enteredClimb = false;
            float peakY = player.Body.position.y;
            for (var index = 0; index < 150; index++)
            {
                yield return Feed(player, 0f, true, false);
                enteredClimb |= movement.IsClimbing;
                peakY = Mathf.Max(peakY, player.Body.position.y);
                if (enteredClimb && !movement.IsClimbing && peakY > 7.5f) break;
            }
            Assert.IsTrue(enteredClimb, "Type3 route begins only from the existing Player's vertical climb input.");
            Assert.Greater(peakY, 7.5f, "The actual Player exits the T3 Tilemap chunk through its U side.");
            Object.Destroy(player.transform.parent.gameObject);
        }

        private static IEnumerator LoadLab()
        {
            EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return new WaitForFixedUpdate();
            Physics2D.SyncTransforms();
        }

        private static CharacterLivePlayerRig DisableSavedPlayer()
        {
            CharacterLivePlayerRig saved = Object.FindFirstObjectByType<CharacterLivePlayerRig>();
            if (saved != null) saved.gameObject.SetActive(false);
            return saved;
        }

        private static CharacterLivePlayerRig CreateActualPlayer(Vector2 feet)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Assert.IsNotNull(prefab);
            var host = new GameObject("RMAP08_ActualPhysicsPlayer");
            host.SetActive(false);
            GameObject instance = Object.Instantiate(prefab, host.transform);
            CharacterLivePlayerRig player = instance.GetComponent<CharacterLivePlayerRig>();
            CharacterLiveMovementDriver movement = instance.GetComponent<CharacterLiveMovementDriver>();
            player.BodyCollider.size = new Vector2(0.4f, 0.8f);
            player.BodyCollider.offset = new Vector2(0f, 0.4f);
            movement.ConfigureRmap02(1);
            host.SetActive(true);
            player.Body.position = feet;
            movement.ResetMotion();
            player.InputSource.enabled = false;
            Physics2D.SyncTransforms();
            return player;
        }

        private static IEnumerator Feed(CharacterLivePlayerRig player, float horizontal, bool up, bool jumpPressed)
        {
            var jump = new CharacterLiveButtonFrame(jumpPressed, false, jumpPressed);
            var idle = new CharacterLiveButtonFrame(false, false, false);
            player.InputSource.Adapter.AccumulateFrame(horizontal, up, false, false, jump, idle, idle, idle);
            yield return new WaitForFixedUpdate();
        }
    }
}
#endif
