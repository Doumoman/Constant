#if LEGACY_DISABLED
using System.Linq;
using NUnit.Framework;
using StarNight.Grid;
using StarNight.Player;
using StarNight.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityGrid = UnityEngine.Grid;

namespace StarNight.Tests.EditMode
{
    public sealed class P1GridWorldEditModeTests
    {
        private const string ScenePath = "Assets/StarNight/Scenes/Labs/P1_GridLab_30x18.unity";
        private const string PrefabPath = "Assets/StarNight/Prefabs/Gameplay/P1_Player.prefab";
        private const string TuningPath = "Assets/StarNight/Settings/P1_MovementTuning.asset";

        [Test]
        public void P1Contract_UsesLiteralGddValues()
        {
            P1MovementTuning tuning = AssetDatabase.LoadAssetAtPath<P1MovementTuning>(TuningPath);

            Assert.That(tuning, Is.Not.Null);
            Assert.That(tuning.ColliderSize.x, Is.EqualTo(0.72f).Within(0.0001f));
            Assert.That(tuning.ColliderSize.y, Is.EqualTo(0.90f).Within(0.0001f));
            Assert.That(tuning.CoyoteTime, Is.EqualTo(0.10f).Within(0.0001f));
            Assert.That(tuning.JumpBufferTime, Is.EqualTo(0.12f).Within(0.0001f));
            Assert.That(tuning.JumpHeight, Is.EqualTo(2.2f).Within(0.0001f));
            Assert.That(tuning.SafeCellDwellTime, Is.EqualTo(0.30f).Within(0.0001f));
            Assert.That(tuning.MaxHealth, Is.EqualTo(4));
        }

        [Test]
        public void GridWorld_All540CellCentersRoundTrip()
        {
            GameObject root = new GameObject("GridWorldTest");
            try
            {
                UnityGrid layout = root.AddComponent<UnityGrid>();
                GridWorld world = root.AddComponent<GridWorld>();
                world.Configure(layout, null, null, Vector2Int.zero, new Vector2Int(30, 18));

                for (int y = 0; y < 18; y++)
                {
                    for (int x = 0; x < 30; x++)
                    {
                        GridPos expected = new GridPos(x, y);
                        Assert.That(world.WorldToCell(world.CellToWorldCenter(expected)), Is.EqualTo(expected));
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GridWorld_BoundsAndNegativeCoordinatesAreExact()
        {
            GameObject root = new GameObject("GridWorldTest");
            try
            {
                UnityGrid layout = root.AddComponent<UnityGrid>();
                GridWorld world = root.AddComponent<GridWorld>();
                world.Configure(layout, null, null, Vector2Int.zero, new Vector2Int(30, 18));

                Assert.That(world.IsWithinBounds(new GridPos(0, 0)), Is.True);
                Assert.That(world.IsWithinBounds(new GridPos(29, 17)), Is.True);
                Assert.That(world.IsWithinBounds(new GridPos(-1, 0)), Is.False);
                Assert.That(world.IsWithinBounds(new GridPos(30, 0)), Is.False);
                Assert.That(world.IsWithinBounds(new GridPos(0, 18)), Is.False);
                Assert.That(world.WorldToCell(new Vector2(-0.01f, -0.01f)), Is.EqualTo(new GridPos(-1, -1)));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GridWorld_OccupancyRejectsDuplicatesAndReleasesOwnerOnly()
        {
            GameObject root = new GameObject("GridWorldTest");
            GameObject first = new GameObject("First");
            GameObject second = new GameObject("Second");
            try
            {
                UnityGrid layout = root.AddComponent<UnityGrid>();
                GridWorld world = root.AddComponent<GridWorld>();
                world.Configure(layout, null, null, Vector2Int.zero, new Vector2Int(30, 18));
                GridPos cell = new GridPos(5, 5);

                Assert.That(world.TryOccupy(cell, first), Is.True);
                Assert.That(world.TryOccupy(cell, first), Is.True);
                Assert.That(world.TryOccupy(cell, second), Is.False);
                Assert.That(world.Release(cell, second), Is.False);
                Assert.That(world.Release(cell, first), Is.True);
                Assert.That(world.IsOccupied(cell), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void JumpGrace_CoyoteAccepts099AndRejects101Seconds()
        {
            JumpGraceState accepted = new JumpGraceState(0.10f, 0.12f);
            accepted.Tick(0f, true);
            accepted.Tick(0.099f, false);
            accepted.BufferJump();
            Assert.That(accepted.TryConsume(), Is.True);
            Assert.That(accepted.TryConsume(), Is.False, "A grace window must be consumed only once.");

            JumpGraceState rejected = new JumpGraceState(0.10f, 0.12f);
            rejected.Tick(0f, true);
            rejected.Tick(0.101f, false);
            rejected.BufferJump();
            Assert.That(rejected.TryConsume(), Is.False);
        }

        [Test]
        public void JumpGrace_BufferAccepts119AndRejects121Seconds()
        {
            JumpGraceState accepted = new JumpGraceState(0.10f, 0.12f);
            accepted.BufferJump();
            accepted.Tick(0.119f, false);
            accepted.Tick(0f, true);
            Assert.That(accepted.TryConsume(), Is.True);

            JumpGraceState rejected = new JumpGraceState(0.10f, 0.12f);
            rejected.BufferJump();
            rejected.Tick(0.121f, false);
            rejected.Tick(0f, true);
            Assert.That(rejected.TryConsume(), Is.False);
        }

        [Test]
        public void PlayerPrefab_HasOneIndependentPhysicsRootAndVisualOnlyLegacyArt()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);

            Rigidbody2D[] bodies = prefab.GetComponentsInChildren<Rigidbody2D>(true);
            Collider2D[] solidColliders = prefab.GetComponentsInChildren<Collider2D>(true)
                .Where(collider => !collider.isTrigger)
                .ToArray();
            CapsuleCollider2D capsule = prefab.GetComponent<CapsuleCollider2D>();
            string[] behaviours = prefab.GetComponentsInChildren<MonoBehaviour>(true)
                .Where(component => component != null)
                .Select(component => component.GetType().FullName)
                .ToArray();

            Assert.That(bodies.Length, Is.EqualTo(1));
            Assert.That(solidColliders.Length, Is.EqualTo(1));
            Assert.That(capsule, Is.Not.Null);
            Assert.That(capsule.size.x, Is.EqualTo(0.72f).Within(0.0001f));
            Assert.That(capsule.size.y, Is.EqualTo(0.90f).Within(0.0001f));
            Assert.That(bodies[0].bodyType, Is.EqualTo(RigidbodyType2D.Dynamic));
            Assert.That(bodies[0].collisionDetectionMode, Is.EqualTo(CollisionDetectionMode2D.Continuous));
            Assert.That((bodies[0].constraints & RigidbodyConstraints2D.FreezeRotation) != 0, Is.True);
            Assert.That(behaviours.Any(type => type == "PlayerFSM" || type == "PlayerTalkTo"), Is.False);
            Assert.That(behaviours.All(type => type.StartsWith("StarNight.Player.")), Is.True);

            SpriteRenderer visual = prefab.transform.Find("Visual")?.GetComponent<SpriteRenderer>();
            Assert.That(visual, Is.Not.Null);
            Assert.That(
                AssetDatabase.GetAssetPath(visual.sprite),
                Is.EqualTo("Assets/StarNight/Art/Player/char_black_full.png"));
        }

        [Test]
        public void GridLabScene_HasExact30x18ContractAndOneCellTunnel()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GridWorld world = Object.FindFirstObjectByType<GridWorld>();
            PlayerMotor2D player = Object.FindFirstObjectByType<PlayerMotor2D>();
            Camera mainCamera = Camera.main;
            Light directional = Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                .FirstOrDefault(light => light.type == LightType.Directional);
            Tilemap terrain = world != null ? world.TerrainTilemap : null;
            CompositeCollider2D composite = terrain != null
                ? terrain.GetComponent<CompositeCollider2D>()
                : null;

            Assert.That(world, Is.Not.Null);
            Assert.That(world.Size, Is.EqualTo(new Vector2Int(30, 18)));
            Assert.That(world.WorldBounds.width, Is.EqualTo(30f).Within(0.0001f));
            Assert.That(world.WorldBounds.height, Is.EqualTo(18f).Within(0.0001f));
            Assert.That(player, Is.Not.Null);
            Assert.That(mainCamera, Is.Not.Null);
            Assert.That(directional, Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<RecoveryVolume2D>(), Is.Not.Null);
            Assert.That(composite, Is.Not.Null);
            Assert.That(composite.pathCount, Is.GreaterThan(0), "Visible terrain must also have generated collision geometry.");

            for (int x = 3; x < 10; x++)
            {
                Assert.That(terrain.HasTile(new Vector3Int(x, 0, 0)), Is.True, $"Missing tunnel floor at x={x}");
                Assert.That(terrain.HasTile(new Vector3Int(x, 1, 0)), Is.False, $"Tunnel cell blocked at x={x}");
                Assert.That(terrain.HasTile(new Vector3Int(x, 2, 0)), Is.True, $"Missing tunnel ceiling at x={x}");
            }

            Assert.That(terrain.HasTile(new Vector3Int(10, 1, 0)), Is.False, "Tunnel exit must remain clear.");
            Assert.That(terrain.HasTile(new Vector3Int(11, 1, 0)), Is.False, "Tunnel exit runway must remain clear.");
            Assert.That(world.HazardTilemap.HasTile(new Vector3Int(25, 0, 0)), Is.True);
            Assert.That(world.HazardTilemap.HasTile(new Vector3Int(27, 0, 0)), Is.True);
        }

        [Test]
        public void CameraClamp_StaysInside30x18WorldAtExtremeTargets()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GridBoundedCamera2D follow = Object.FindFirstObjectByType<GridBoundedCamera2D>();
            Camera camera = follow != null ? follow.GetComponent<Camera>() : null;
            GridWorld world = Object.FindFirstObjectByType<GridWorld>();

            Assert.That(follow, Is.Not.Null);
            Assert.That(camera, Is.Not.Null);
            float originalAspect = camera.aspect;
            camera.aspect = 16f / 9f;
            try
            {
                Vector2 minimum = follow.CalculateClampedPosition(new Vector2(-100f, -100f));
                Vector2 maximum = follow.CalculateClampedPosition(new Vector2(100f, 100f));
                float halfHeight = camera.orthographicSize;
                float halfWidth = halfHeight * camera.aspect;

                Assert.That(minimum.x, Is.EqualTo(world.WorldBounds.xMin + halfWidth).Within(0.001f));
                Assert.That(minimum.y, Is.EqualTo(world.WorldBounds.yMin + halfHeight).Within(0.001f));
                Assert.That(maximum.x, Is.EqualTo(world.WorldBounds.xMax - halfWidth).Within(0.001f));
                Assert.That(maximum.y, Is.EqualTo(world.WorldBounds.yMax - halfHeight).Within(0.001f));
            }
            finally
            {
                camera.aspect = originalAspect;
            }
        }
    }
}

#endif
