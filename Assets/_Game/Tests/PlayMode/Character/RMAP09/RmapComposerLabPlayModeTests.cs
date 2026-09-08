#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using StarNight.Character.Live.Input;
using StarNight.Character.Live.Movement;
using StarNight.Character.Live.Player;
using StarNight.Map.WorldGeneration.MoonPalace.RunGeneration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace StarNight.Character.Tests.PlayMode.Rmap09
{
    [Category("RMAP09")]
    public sealed class RmapComposerLabPlayModeTests
    {
        private const string ScenePath =
            "Assets/_Game/Map/Scenes/MoonPalace/RMAP09/MoonPalaceComposerLab_RMAP09.unity";
        private const string PlayerPrefabPath = "Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab";

        [UnityTest]
        public IEnumerator P04_A22_SavedComposerLabUsesAReal12x8TilemapColliderAndSeparateLadderOverlay()
        {
            yield return LoadLab();
            RmapComposerResult result = RmapComposer.Compose(RmapComposer.CreateFixtureRequest());
            Assert.That(result.Success, Is.True, result.FailureSummary);
            RmapComposerComposition composition = result.Composition;
            Tilemap terrain = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude,
                FindObjectsSortMode.None).Single(value => value.name == "RMAP09_BaseTerrain");
            Tilemap overlay = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude,
                FindObjectsSortMode.None).Single(value => value.name == "RMAP09_LadderOverlay");
            CharacterLivePlayerRig saved = Object.FindFirstObjectByType<CharacterLivePlayerRig>();

            Assert.That(saved, Is.Not.Null);
            Assert.That(saved.name, Is.EqualTo("RMAP09_Player"));
            Assert.That(saved.BodyCollider.size, Is.EqualTo(new Vector2(0.4f, 0.8f)));
            Assert.That(terrain.GetComponent<TilemapCollider2D>(), Is.Not.Null);
            Assert.That(terrain.GetComponent<Rigidbody2D>().bodyType, Is.EqualTo(RigidbodyType2D.Static));
            Assert.That(terrain.cellBounds.size.x, Is.EqualTo(RmapComposer.Width));
            Assert.That(overlay.GetComponent<TilemapCollider2D>(), Is.Null,
                "Traversal affordance remains an overlay and cannot replace base collision.");
            CharacterLiveClimbSurface ladder = overlay.GetComponent<CharacterLiveClimbSurface>();
            Assert.That(ladder, Is.Not.Null);
            Assert.That(ladder.IsUsable, Is.True);
            Assert.That(overlay.GetComponent<BoxCollider2D>().isTrigger, Is.True);
            Assert.That(CountTiles(overlay, new BoundsInt(0, 0, 0, RmapComposer.Width,
                RmapComposer.Height, 1)), Is.EqualTo(composition.Overlays.Count));
        }

        [UnityTest]
        public IEnumerator P04_P19_ActualPlayerTraversesTheProtectedSpineThenClimbsThroughTheDeclaredUpPort()
        {
            yield return LoadLab();
            DisableSavedPlayer();
            CharacterLivePlayerRig player = CreateActualPlayer(new Vector2(0.6f, 1.01f));
            CharacterLiveMovementDriver movement = player.GetComponent<CharacterLiveMovementDriver>();
            for (var index = 0; index < 100 && player.Body.position.x < 5.25f; index++)
                yield return Feed(player, 1f, false, false);
            Assert.That(player.Body.position.x, Is.GreaterThanOrEqualTo(5.15f),
                "The input-driven Player reaches the protected L->U route spine.");
            Assert.That(player.Body.position.x, Is.LessThan(5.95f));

            bool enteredClimb = false;
            float peak = player.Body.position.y;
            for (var index = 0; index < 240; index++)
            {
                yield return Feed(player, 0f, true, false);
                enteredClimb |= movement.IsClimbing;
                peak = Mathf.Max(peak, player.Body.position.y);
                if (enteredClimb && peak > 7.15f) break;
            }
            Assert.That(enteredClimb, Is.True,
                "The actual Player enters the RMAP04 ladder surface, rather than a preview marker route.");
            Assert.That(peak, Is.GreaterThan(7.15f),
                "The real Player exits through T3_CLIMB's physical U-side clearance.");
            Assert.That(RmapComposer.Compose(RmapComposer.CreateFixtureRequest()).Composition.OptionalRouteEvidence,
                Is.EqualTo("PROFILE_CONTEXT_UNKNOWN_NOT_PROMOTED_TO_REQUIRED_PASS"),
                "P19 keeps optional/unresolved evidence separate; it does not add health or recovery guarantees.");
            Object.Destroy(player.transform.parent.gameObject);
        }

        private static IEnumerator LoadLab()
        {
            EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return new WaitForFixedUpdate();
            Physics2D.SyncTransforms();
        }

        private static void DisableSavedPlayer()
        {
            CharacterLivePlayerRig saved = Object.FindFirstObjectByType<CharacterLivePlayerRig>();
            if (saved != null) saved.gameObject.SetActive(false);
        }

        private static CharacterLivePlayerRig CreateActualPlayer(Vector2 feet)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            var host = new GameObject("RMAP09_ActualPhysicsPlayer");
            host.SetActive(false);
            GameObject instance = Object.Instantiate(prefab, host.transform);
            CharacterLivePlayerRig player = instance.GetComponent<CharacterLivePlayerRig>();
            CharacterLiveMovementDriver movement = instance.GetComponent<CharacterLiveMovementDriver>();
            player.BodyCollider.size = new Vector2(0.4f, 0.8f);
            player.BodyCollider.offset = new Vector2(0f, 0.4f);
            movement.ConfigureRmap02(1 << LayerMask.NameToLayer("Default"));
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

        private static int CountTiles(Tilemap tilemap, BoundsInt bounds)
        {
            var count = 0;
            foreach (Vector3Int cell in bounds.allPositionsWithin)
                if (tilemap.GetTile(cell) != null) count++;
            return count;
        }
    }
}
#endif
