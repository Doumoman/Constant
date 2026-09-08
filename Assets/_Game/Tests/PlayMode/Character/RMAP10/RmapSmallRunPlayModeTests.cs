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

namespace StarNight.Character.Tests.PlayMode.Rmap10
{
    [Category("RMAP10")]
    public sealed class RmapSmallRunPlayModeTests
    {
        private const string ScenePath = "Assets/_Game/Map/Scenes/MoonPalace/RMAP10/MoonPalaceSmallRun_RMAP10.unity";
        private const string PlayerPrefabPath = "Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab";

        [UnityTest]
        public IEnumerator E05_E07_SavedSceneBakesTheCurrentSmallRunWithProductionComponents()
        {
            yield return LoadScene();
            Tilemap terrain = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Single(value => value.name == "RMAP10_BaseTerrain");
            Tilemap oneWay = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Single(value => value.name == "RMAP10_OneWayOverlay");
            Tilemap ladder = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Single(value => value.name == "RMAP10_LadderOverlay");
            RmapSmallRunPlan plan = RmapSmallRunHarness.Generate(new RmapSmallRunRequest(1107, 36, 24,
                RmapSmallRunRecipe.PortGalleryV1));
            Assert.That(plan.Success, Is.True, plan.FailureSummary);
            Assert.That(terrain.GetComponent<TilemapCollider2D>(), Is.Not.Null);
            Assert.That(oneWay.GetComponent<TilemapCollider2D>().usedByEffector, Is.True);
            Assert.That(oneWay.GetComponent<PlatformEffector2D>().useOneWay, Is.True);
            Assert.That(ladder.GetComponent<TilemapCollider2D>(), Is.Null);
            Assert.That(ladder.GetComponent<CharacterLiveClimbSurface>().IsUsable, Is.True);
            Assert.That(ladder.GetComponent<BoxCollider2D>().isTrigger, Is.True);
            Assert.That(Object.FindFirstObjectByType<CharacterLiveMapRunExit>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<CharacterLivePlayerRig>().name, Is.EqualTo("RMAP10_Player"));
            foreach (RmapSmallRunChunk chunk in plan.Chunks)
            for (var y = 0; y < 8; y++) for (var x = 0; x < 12; x++)
                Assert.That(terrain.GetTile(new Vector3Int(chunk.OriginX + x, chunk.OriginY + y, 0)) != null,
                    Is.EqualTo(chunk.GetBaseCell(x, y).ToString() == "Solid"), chunk.InstanceId + " base cell");
        }

        [UnityTest]
        public IEnumerator E05_ActualProductionPlayerDrivesFromStartToTheRealExitThroughTilemapCollider()
        {
            yield return LoadScene();
            CharacterLivePlayerRig saved = Object.FindFirstObjectByType<CharacterLivePlayerRig>();
            saved.gameObject.SetActive(false);
            CharacterLiveMapRunExit exit = Object.FindFirstObjectByType<CharacterLiveMapRunExit>();
            CharacterLivePlayerRig player = CreateActualPlayer(new Vector2(1.5f, 1f));
            CharacterLiveMovementDriver movement = player.GetComponent<CharacterLiveMovementDriver>();
            for (var index = 0; index < 520 && !exit.HasBeenReached; index++)
                yield return Feed(player, 1f);
            Assert.That(exit.HasBeenReached, Is.True, "Input-driven Production Player must enter RMAP10_Exit. position=" +
                player.Body.position + " grounded=" + movement.IsGroundedNow + " velocity=" + movement.Velocity);
            Assert.That(exit.ReachedBy, Is.EqualTo(player));
            Assert.That(player.Body.position.x, Is.GreaterThanOrEqualTo(33.5f));
            Assert.That(movement.IsDriving, Is.True);
            Assert.That(player.BodyCollider.size, Is.EqualTo(new Vector2(0.4f, 0.8f)));
            Object.Destroy(player.transform.parent.gameObject);
        }

        private static IEnumerator LoadScene()
        {
            EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return new WaitForFixedUpdate();
            Physics2D.SyncTransforms();
        }

        private static CharacterLivePlayerRig CreateActualPlayer(Vector2 feet)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            var host = new GameObject("RMAP10_ActualPhysicsPlayer");
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

        private static IEnumerator Feed(CharacterLivePlayerRig player, float horizontal)
        {
            var idle = new CharacterLiveButtonFrame(false, false, false);
            player.InputSource.Adapter.AccumulateFrame(horizontal, false, false, false, idle, idle, idle, idle);
            yield return new WaitForFixedUpdate();
        }
    }
}
#endif
