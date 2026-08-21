#if LEGACY_DISABLED
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Explosions;
using StarNight.Grid;
using StarNight.Objects;
using StarNight.Tiles;
using StarNight.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace StarNight.Tests.PlayMode
{
    public sealed class P2SceneIntegrationPlayModeTests
    {
        private const string ScenePath =
            "Assets/StarNight/Scenes/Labs/P2_MoonlitMineMutationLab_48x24.unity";
        private const string GameplayPrefabFolder =
            "Assets/StarNight/Prefabs/Gameplay/";

        private static readonly string[] RequiredSceneObjects =
        {
            "P2_MoonlitMineMutationLab_48x24",
            "P2_Systems",
            "P2_Objects",
            "Player",
            "Carryable_Crate",
            "Carryable_Rock",
            "Pressure_Plate",
            "Falling_Rock_Plate",
            "Falling_Rock",
            "Support_Bomb",
            "Chain_Bomb_01",
            "Chain_Bomb_02",
            "Chain_Bomb_03",
            "Chain_Bomb_04",
            "Exit_Protection_Zone",
            "Safe_Relocation_Anchor",
            "Exit_Blocker_Demonstration_Rock",
            "Main Camera",
            "Directional Light"
        };

        [UnityTest]
        public IEnumerator P2MoonlitMineScene_HasCompleteRuntimeAndPrefabContract()
        {
            yield return LoadP2Scene();

            for (int index = 0; index < RequiredSceneObjects.Length; index++)
            {
                Assert.That(
                    GameObject.Find(RequiredSceneObjects[index]),
                    Is.Not.Null,
                    $"Missing P2 scene object: {RequiredSceneObjects[index]}");
            }

            GridWorld world = Object.FindFirstObjectByType<GridWorld>();
            TileMutationService mutationService =
                Object.FindFirstObjectByType<TileMutationService>();
            ExplosionService2D explosionService =
                Object.FindFirstObjectByType<ExplosionService2D>();
            CarrySystem carrySystem = Object.FindFirstObjectByType<CarrySystem>();
            PressurePlate2D pressurePlate =
                Object.FindFirstObjectByType<PressurePlate2D>();
            FallingObject2D fallingObject =
                Object.FindFirstObjectByType<FallingObject2D>();
            ExitBlockerResolver2D resolver =
                Object.FindFirstObjectByType<ExitBlockerResolver2D>();
            CarryableObject2D[] carryables =
                Object.FindObjectsByType<CarryableObject2D>(
                    FindObjectsSortMode.None);
            Bomb2D[] bombs =
                Object.FindObjectsByType<Bomb2D>(FindObjectsSortMode.None);
            Light directional = Object.FindObjectsByType<Light>(
                    FindObjectsSortMode.None)
                .FirstOrDefault(light => light.type == LightType.Directional);

            Assert.That(world, Is.Not.Null);
            Assert.That(world.Size, Is.EqualTo(new Vector2Int(48, 24)));
            Assert.That(mutationService, Is.Not.Null);
            Assert.That(explosionService, Is.Not.Null);
            Assert.That(carrySystem, Is.Not.Null);
            Assert.That(pressurePlate, Is.Not.Null);
            Assert.That(fallingObject, Is.Not.Null);
            Assert.That(resolver, Is.Not.Null);
            Assert.That(carryables.Length, Is.GreaterThanOrEqualTo(3));
            Assert.That(bombs.Length, Is.GreaterThanOrEqualTo(5));
            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(directional, Is.Not.Null);
            Assert.That(mutationService.IsCurrentExitReachable(), Is.True);

            Dictionary<TileMaterialKind, TileDefinition> definitions =
                CollectSceneDefinitions(world, mutationService);
            Assert.That(
                definitions.ContainsKey(TileMaterialKind.Stone),
                Is.True,
                "The themed lab must visibly exercise stone.");
            Assert.That(
                definitions.ContainsKey(TileMaterialKind.Dirt),
                Is.True,
                "The themed lab must visibly exercise dirt.");
            Assert.That(
                definitions.ContainsKey(TileMaterialKind.CrackedWall),
                Is.True,
                "The themed lab must visibly exercise cracked walls.");

            TileDefinition stone = definitions[TileMaterialKind.Stone];
            TileDefinition dirt = definitions[TileMaterialKind.Dirt];
            TileDefinition cracked = definitions[TileMaterialKind.CrackedWall];
            Assert.That(stone.CanBreak(TileBreakMethod.Pickaxe), Is.True);
            Assert.That(stone.CanBreak(TileBreakMethod.Bomb), Is.False);
            Assert.That(dirt.CanBreak(TileBreakMethod.Bomb), Is.True);
            Assert.That(dirt.CanBreak(TileBreakMethod.Shovel), Is.True);
            Assert.That(cracked.CanBreak(TileBreakMethod.Bomb), Is.True);
            Assert.That(cracked.CanBreak(TileBreakMethod.Pickaxe), Is.True);

#if UNITY_EDITOR
            AssertPrefabHas<CarrySystem>("P2_Player.prefab");
            AssertPrefabHas<CarryableObject2D>("P2_Crate.prefab");
            AssertPrefabHas<CarryableObject2D>("P2_Rock.prefab");
            AssertPrefabHas<PressurePlate2D>("P2_PressurePlate.prefab");
            AssertPrefabHas<Bomb2D>("P2_Bomb.prefab");
            AssertPrefabHas<CarryableObject2D>("P2_Bomb.prefab");
            AssertPrefabHas<FallingObject2D>("P2_FallingRock.prefab");
#endif
        }

        [UnityTest]
        public IEnumerator TileMutationInP2Scene_RefreshesColliderAndPreservesExitRoute()
        {
            yield return LoadP2Scene();

            GridWorld world = Object.FindFirstObjectByType<GridWorld>();
            TileMutationService mutationService =
                Object.FindFirstObjectByType<TileMutationService>();
            Tilemap terrain = world != null ? world.TerrainTilemap : null;
            TilemapCollider2D tilemapCollider = terrain != null
                ? terrain.GetComponent<TilemapCollider2D>()
                : null;
            CompositeCollider2D composite = terrain != null
                ? terrain.GetComponent<CompositeCollider2D>()
                : null;
            Collider2D collisionGeometry = composite != null
                && composite.enabled
                ? composite
                : tilemapCollider;

            Assert.That(world, Is.Not.Null);
            Assert.That(mutationService, Is.Not.Null);
            Assert.That(terrain, Is.Not.Null);
            Assert.That(tilemapCollider, Is.Not.Null);
            Assert.That(collisionGeometry, Is.Not.Null);
            Assert.That(mutationService.IsCurrentExitReachable(), Is.True);

            Physics2D.SyncTransforms();
            bool committedCandidateFound = false;
            GridPos committedCell = default;
            int pathVersionBeforeCommit = mutationService.PathVersion;
            TileMaterialKind[] preferredKinds =
            {
                TileMaterialKind.CrackedWall,
                TileMaterialKind.Dirt,
                TileMaterialKind.Wood,
                TileMaterialKind.Fixture
            };

            for (int kindIndex = 0;
                kindIndex < preferredKinds.Length && !committedCandidateFound;
                kindIndex++)
            {
                TileMaterialKind preferredKind = preferredKinds[kindIndex];
                RectInt bounds = world.CellBounds;
                for (int y = bounds.yMin;
                    y < bounds.yMax && !committedCandidateFound;
                    y++)
                {
                    for (int x = bounds.xMin;
                        x < bounds.xMax && !committedCandidateFound;
                        x++)
                    {
                        GridPos cell = new GridPos(x, y);
                        TileBase tile = terrain.GetTile(new Vector3Int(x, y, 0));
                        if (tile == null
                            || mutationService.IsProtectedCell(cell)
                            || !mutationService.TryGetDefinition(
                                tile,
                                out TileDefinition definition)
                            || definition.MaterialKind != preferredKind
                            || !definition.CanBreak(TileBreakMethod.Bomb))
                        {
                            continue;
                        }

                        Vector2 center = world.CellToWorldCenter(cell);
                        if (!collisionGeometry.OverlapPoint(center))
                        {
                            continue;
                        }

                        int versionBeforeAttempt = mutationService.PathVersion;
                        mutationService.EnqueueDestroy(
                            cell,
                            TileBreakMethod.Bomb);
                        TileMutationBatchReport report =
                            mutationService.FlushPending();
                        if (report.CommittedCount == 0)
                        {
                            Assert.That(
                                mutationService.PathVersion,
                                Is.EqualTo(versionBeforeAttempt));
                            continue;
                        }

                        Assert.That(report.Records.Count, Is.EqualTo(1));
                        Assert.That(report.RejectedCount, Is.EqualTo(0));
                        Assert.That(
                            mutationService.PathVersion,
                            Is.EqualTo(versionBeforeAttempt + 1));
                        Assert.That(
                            terrain.GetTile(new Vector3Int(x, y, 0)),
                            Is.Null);
                        Assert.That(tilemapCollider.hasTilemapChanges, Is.False);
                        Assert.That(
                            collisionGeometry.OverlapPoint(center),
                            Is.False,
                            $"Collision remained at destroyed cell {cell}.");
                        Assert.That(
                            mutationService.IsCurrentExitReachable(),
                            Is.True);
                        committedCell = cell;
                        committedCandidateFound = true;
                    }
                }
            }

            Assert.That(
                committedCandidateFound,
                Is.True,
                "The P2 scene needs at least one bomb-breakable collidable cell whose removal preserves the exit route.");
            Assert.That(
                mutationService.PathVersion,
                Is.EqualTo(pathVersionBeforeCommit + 1),
                $"Unexpected path-version delta after mutating {committedCell}.");
            if (composite != null)
            {
                Assert.That(composite.pathCount, Is.GreaterThan(0));
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator P2SceneExitResolver_RelocatesObjectPlacedOnRequiredExit()
        {
            yield return LoadP2Scene();

            GridWorld world = Object.FindFirstObjectByType<GridWorld>();
            TileMutationService mutationService =
                Object.FindFirstObjectByType<TileMutationService>();
            ExitBlockerResolver2D resolver =
                Object.FindFirstObjectByType<ExitBlockerResolver2D>();
            GameObject demonstration =
                GameObject.Find("Exit_Blocker_Demonstration_Rock");
            CarryableObject2D demonstrationCarryable = demonstration != null
                ? demonstration.GetComponent<CarryableObject2D>()
                : null;

            Assert.That(world, Is.Not.Null);
            Assert.That(mutationService, Is.Not.Null);
            Assert.That(resolver, Is.Not.Null);
            Assert.That(demonstrationCarryable, Is.Not.Null);
            Assert.That(demonstrationCarryable.Body, Is.Not.Null);

            CarryableObject2D[] allCarryables =
                Object.FindObjectsByType<CarryableObject2D>(
                    FindObjectsSortMode.None);
            for (int index = 0; index < allCarryables.Length; index++)
            {
                if (allCarryables[index] != demonstrationCarryable)
                {
                    allCarryables[index].gameObject.SetActive(false);
                }
            }

            GridPos exit = mutationService.RequiredExit;
            Vector2 blockedPosition = world.CellToWorldCenter(exit);
            Rigidbody2D body = demonstrationCarryable.Body;
            demonstrationCarryable.transform.position =
                new Vector3(blockedPosition.x, blockedPosition.y, 0f);
            body.simulated = true;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.position = blockedPosition;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            Physics2D.SyncTransforms();

            int completionEvents = 0;
            resolver.ResolutionCompleted += _ => completionEvents++;
            resolver.RequestResolution();
            Assert.That(resolver.ResolutionRequested, Is.True);

            int relocated = resolver.ResolveNow();

            Assert.That(relocated, Is.EqualTo(1));
            Assert.That(resolver.LastRelocatedCount, Is.EqualTo(1));
            Assert.That(resolver.ExitRouteReachable, Is.True);
            Assert.That(resolver.ResolutionRequested, Is.False);
            Assert.That(completionEvents, Is.EqualTo(1));
            Assert.That(world.WorldToCell(body.position), Is.Not.EqualTo(exit));
            Assert.That(
                mutationService.IsCurrentExitReachable(),
                Is.True,
                "Static terrain path must remain valid after dynamic blocker recovery.");

            yield return null;
        }

        private static Dictionary<TileMaterialKind, TileDefinition>
            CollectSceneDefinitions(
                GridWorld world,
                TileMutationService mutationService)
        {
            Dictionary<TileMaterialKind, TileDefinition> found =
                new Dictionary<TileMaterialKind, TileDefinition>();
            RectInt bounds = world.CellBounds;
            Tilemap terrain = world.TerrainTilemap;
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    TileBase tile = terrain.GetTile(new Vector3Int(x, y, 0));
                    if (tile != null
                        && mutationService.TryGetDefinition(
                            tile,
                            out TileDefinition definition)
                        && !found.ContainsKey(definition.MaterialKind))
                    {
                        found.Add(definition.MaterialKind, definition);
                    }
                }
            }

            return found;
        }

#if UNITY_EDITOR
        private static void AssertPrefabHas<T>(string fileName)
            where T : Component
        {
            string path = GameplayPrefabFolder + fileName;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, $"Missing P2 prefab: {path}");
            Assert.That(
                prefab.GetComponentInChildren<T>(true),
                Is.Not.Null,
                $"{path} must contain {typeof(T).Name}.");
        }
#endif

        private static IEnumerator LoadP2Scene()
        {
#if UNITY_EDITOR
            Assert.That(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath),
                Is.Not.Null,
                $"Missing P2 integration scene: {ScenePath}");
            EditorSceneManager.LoadSceneInPlayMode(
                ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("P2 integration tests require the Unity Editor asset database.");
#endif
            yield return null;
            yield return new WaitForFixedUpdate();
            Physics2D.SyncTransforms();
        }
    }
}

#endif
