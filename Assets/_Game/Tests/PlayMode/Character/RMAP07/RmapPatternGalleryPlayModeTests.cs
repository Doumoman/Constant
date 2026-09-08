#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using StarNight.Character.Live.Input;
using StarNight.Character.Live.Movement;
using StarNight.Character.Live.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace StarNight.Character.Tests.PlayMode.Rmap07
{
    [Category("RMAP07")]
    public sealed class RmapPatternGalleryPlayModeTests
    {
        private const string ScenePath =
            "Assets/_Game/Map/Scenes/MoonPalace/RMAP07/MoonPalacePatternGallery_RMAP07.unity";

        [UnityTest]
        public IEnumerator A01_A15_SavedGallery_UsesActualPlayerAndTilemapColliderTerrain()
        {
            EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return new WaitForFixedUpdate();

            CharacterLivePlayerRig savedPlayer = Object.FindFirstObjectByType<CharacterLivePlayerRig>();
            Tilemap solidGallery = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None)
                .First(value => value.name == "RMAP07_SolidGallery");
            TilemapCollider2D solidCollider = solidGallery.GetComponent<TilemapCollider2D>();
            Assert.IsNotNull(savedPlayer);
            Assert.AreEqual("RMAP07_Player", savedPlayer.name);

            Assert.IsNotNull(solidCollider);
            Assert.IsNotNull(solidGallery.GetTile(new Vector3Int(0, 0, 0)),
                "The 1x1 Tilemap ground is physical, not an editor marker.");

            savedPlayer.gameObject.SetActive(false);
            CharacterLivePlayerRig player = CreateActualPlayer(new Vector2(2f, 9f));
            CharacterLiveMovementDriver movement = player.GetComponent<CharacterLiveMovementDriver>();
            player.InputSource.enabled = false;
            movement.ResetMotion();
            Physics2D.SyncTransforms();
            for (var index = 0; index < 140 && !movement.IsGroundedNow; index++)
            {
                yield return Feed(player, false, false);
            }

            Assert.IsTrue(movement.IsGroundedNow, "The actual Player lands on TilemapCollider2D terrain. " +
                "feet=" + player.Body.position + " ticks=" + movement.PhysicsTick +
                " driving=" + movement.IsDriving + " terrain=" + solidCollider.bounds);
            Assert.That(player.Body.position.y, Is.InRange(1.0f, 1.08f));
            Object.Destroy(player.transform.parent.gameObject);
        }

        [UnityTest]
        public IEnumerator A12_ActualPlayer_PassesUpwardAndLandsOnAllFourTilemapOneWayTransforms()
        {
            EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return new WaitForFixedUpdate();

            CharacterLivePlayerRig savedPlayer = Object.FindFirstObjectByType<CharacterLivePlayerRig>();
            Assert.IsNotNull(savedPlayer);
            savedPlayer.gameObject.SetActive(false);
            CharacterLivePlayerRig player = CreateActualPlayer(new Vector2(2f, 1f));
            CharacterLiveMovementDriver movement = player.GetComponent<CharacterLiveMovementDriver>();
            player.InputSource.enabled = false;
            CharacterLiveOneWayPlatform[] fixtures = Object.FindObjectsByType<CharacterLiveOneWayPlatform>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(value => value.name.StartsWith("RMAP07_OneWay_"))
                .OrderBy(value => value.name).ToArray();

            Assert.AreEqual(4, fixtures.Length);
            foreach (CharacterLiveOneWayPlatform fixture in fixtures)
            {
                TilemapCollider2D collider = fixture.GetComponent<TilemapCollider2D>();
                Assert.IsNotNull(collider, fixture.name + " must retain its TilemapCollider2D.");
                Assert.IsTrue(collider.usedByEffector);
                Assert.IsTrue(fixture.GetComponent<PlatformEffector2D>().useOneWay);
                Assert.IsFalse(fixture.GetComponent<CharacterLiveGrabSurface>().IsGrabAllowed,
                    "One-way cells are never converted into RMAP03 grab anchors.");

                player.Body.position = new Vector2(collider.bounds.center.x, 1.01f);
                movement.ResetMotion();
                Physics2D.SyncTransforms();
                bool passedAbove = false;
                for (var index = 0; index < 160; index++)
                {
                    yield return Feed(player, index == 0, true);
                    passedAbove |= player.Body.position.y > fixture.TopWorldY + 0.01f;
                    if (index > 40 && movement.IsGroundedNow &&
                        player.Body.position.y >= fixture.TopWorldY)
                    {
                        break;
                    }
                }

                Assert.IsTrue(passedAbove, fixture.name + " must pass the actual Player from below. " +
                    "feet=" + player.Body.position + " top=" + fixture.TopWorldY + " bounds=" + collider.bounds +
                    " ticks=" + movement.PhysicsTick);
                Assert.IsTrue(movement.IsGroundedNow, fixture.name + " must support the Player on its top face.");
                Assert.GreaterOrEqual(player.Body.position.y, fixture.TopWorldY - 0.03f);
            }
            Object.Destroy(player.transform.parent.gameObject);
        }

        private static CharacterLivePlayerRig CreateActualPlayer(Vector2 feet)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab");
            Assert.IsNotNull(prefab);
            var host = new GameObject("RMAP07_ActualPhysicsPlayer");
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
            Physics2D.SyncTransforms();
            return player;
        }

        private static IEnumerator Feed(CharacterLivePlayerRig player, bool jumpPressed, bool jumpHeld)
        {
            var jump = new CharacterLiveButtonFrame(jumpPressed, false, jumpHeld);
            var idle = new CharacterLiveButtonFrame(false, false, false);
            player.InputSource.Adapter.AccumulateFrame(0f, false, false, false, jump, idle, idle, idle);
            yield return new WaitForFixedUpdate();
        }
    }
}
#endif
