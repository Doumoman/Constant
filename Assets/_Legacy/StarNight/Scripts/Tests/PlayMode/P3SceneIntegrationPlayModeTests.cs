#if LEGACY_DISABLED
using System.Collections;
using System.Linq;
using NUnit.Framework;
using StarNight.Debugging;
using StarNight.Explosions;
using StarNight.Grid;
using StarNight.Player;
using StarNight.Tiles;
using StarNight.Tools;
using StarNight.Tools.Grapple;
using StarNight.Tools.Mining;
using StarNight.Tools.Pestle;
using StarNight.Tools.Rope;
using StarNight.Tools.Umbrella;
using StarNight.Tools.Water;
using StarNight.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace StarNight.Tests.PlayMode
{
    public sealed class P3SceneIntegrationPlayModeTests
    {
        private const string ScenePath =
            "Assets/StarNight/Scenes/Labs/P3_StarwindToolGarden_72x30.unity";
        private const string GameplayPrefabFolder =
            "Assets/StarNight/Prefabs/Gameplay/";
        private const string ToolPrefabFolder =
            "Assets/StarNight/Prefabs/Gameplay/P3Tools/";

        private static readonly string[] RequiredSceneObjects =
        {
            "P3_StarwindToolGarden_72x30",
            "P3_Systems",
            "P3_ToolBays",
            "P3_ToolHUD",
            "Player",
            "01_Rope_Ring",
            "02_Bomb_VisualSample",
            "03_Pickaxe",
            "04_Shovel",
            "05_WateringCan",
            "05_WaterSource",
            "05_DryGrowableVine",
            "06_Pestle",
            "06_DrivenStake",
            "06_ThinFloorPestleTarget",
            "07_Grapple",
            "07_GrappleAnchor",
            "07_PullableWeight",
            "08_WindUmbrella",
            "08_Updraft",
            "ToolFreeExit",
            "FallRecovery",
            "Main Camera",
            "Directional Light"
        };

        [UnityTest]
        public IEnumerator StarwindToolGarden_HasCompleteP3Contract()
        {
            yield return LoadP3Scene();

            for (int index = 0; index < RequiredSceneObjects.Length; index++)
            {
                Assert.That(
                    GameObject.Find(RequiredSceneObjects[index]),
                    Is.Not.Null,
                    $"Missing P3 scene object: {RequiredSceneObjects[index]}");
            }

            GridWorld world = Object.FindFirstObjectByType<GridWorld>();
            TileMutationService mutation =
                Object.FindFirstObjectByType<TileMutationService>();
            ExplosionService2D explosions =
                Object.FindFirstObjectByType<ExplosionService2D>();
            RopeInstaller2D ropeInstaller =
                Object.FindFirstObjectByType<RopeInstaller2D>();
            WaterInteractionRegistry2D waterRegistry =
                Object.FindFirstObjectByType<WaterInteractionRegistry2D>();
            PestleInteractionRegistry2D pestleRegistry =
                Object.FindFirstObjectByType<PestleInteractionRegistry2D>();
            P3ToolDiscoveryTelemetry telemetry =
                Object.FindFirstObjectByType<P3ToolDiscoveryTelemetry>();
            PlayerToolInventory2D inventory =
                Object.FindFirstObjectByType<PlayerToolInventory2D>();
            PlayerConsumableTools2D consumables =
                Object.FindFirstObjectByType<PlayerConsumableTools2D>();
            P3ToolHud2D toolHud =
                Object.FindFirstObjectByType<P3ToolHud2D>();
            PlayerInputAdapter input =
                Object.FindFirstObjectByType<PlayerInputAdapter>();
            Light directional = Object.FindObjectsByType<Light>(
                    FindObjectsSortMode.None)
                .FirstOrDefault(light => light.type == LightType.Directional);

            Assert.That(world, Is.Not.Null);
            Assert.That(
                world.Size,
                Is.EqualTo(new Vector2Int(
                    P3ToolGardenContract.Width,
                    P3ToolGardenContract.Height)));
            Assert.That(mutation, Is.Not.Null);
            Assert.That(explosions, Is.Not.Null);
            Assert.That(ropeInstaller, Is.Not.Null);
            Assert.That(ropeInstaller.MaximumLength, Is.EqualTo(6));
            Assert.That(waterRegistry, Is.Not.Null);
            Assert.That(pestleRegistry, Is.Not.Null);
            Assert.That(telemetry, Is.Not.Null);
            Assert.That(inventory, Is.Not.Null);
            Assert.That(input, Is.Not.Null);
            Assert.That(consumables, Is.Not.Null);
            Assert.That(toolHud, Is.Not.Null);
            Assert.That(
                consumables.RopeStock,
                Is.EqualTo(PlayerConsumableTools2D.DefaultRopeStock));
            Assert.That(
                consumables.BombStock,
                Is.EqualTo(PlayerConsumableTools2D.DefaultBombStock));
            Assert.That(toolHud.VisibleRopeDots, Is.EqualTo(4));
            Assert.That(toolHud.VisibleBombDots, Is.EqualTo(4));
            Assert.That(toolHud.IsHeldToolIconVisible, Is.False);
            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(directional, Is.Not.Null);
            Assert.That(mutation.IsCurrentExitReachable(), Is.True);

            Assert.That(
                P3ToolGardenContract.ValidateToolFreeMainRoute(
                    world,
                    out GridPos firstFailure),
                Is.True,
                $"Tool-free main route failed at {firstFailure}.");

            AssertToolOrderAndTextlessCues();
            AssertHandToolSet();
            AssertToolDemonstrations();
            AssertNoVisibleTextComponents();
            consumables.SetStockForTests(2, 1);
            Assert.That(toolHud.VisibleRopeDots, Is.EqualTo(2));
            Assert.That(toolHud.VisibleBombDots, Is.EqualTo(1));
            HandToolPickup2D pickaxePickup =
                Object.FindObjectsByType<HandToolPickup2D>(
                        FindObjectsSortMode.None)
                    .Single(item => item.Kind == HandToolKind.Pickaxe);
            Assert.That(inventory.TryEquip(pickaxePickup), Is.True);
            Assert.That(toolHud.IsHeldToolIconVisible, Is.True);
            Assert.That(
                toolHud.VisibleHeldToolDots,
                Is.EqualTo(PickaxeTool2D.DefaultDurability));

#if UNITY_EDITOR
            AssertPrefabHas<PlayerToolInventory2D>(
                GameplayPrefabFolder + "P3_Player.prefab");
            AssertPrefabHas<PlayerConsumableTools2D>(
                GameplayPrefabFolder + "P3_Player.prefab");
            AssertPrefabHas<RopeClimber2D>(
                GameplayPrefabFolder + "P3_Player.prefab");
            AssertPrefabHas<Bomb2D>(
                GameplayPrefabFolder + "P3_Bomb.prefab");
            AssertPrefabHas<RopeExplosionBridge2D>(
                GameplayPrefabFolder + "P3_Bomb.prefab");

            AssertPrefabHas<PickaxeTool2D>(
                ToolPrefabFolder + "P3_Pickaxe.prefab");
            AssertPrefabHas<ShovelTool2D>(
                ToolPrefabFolder + "P3_Shovel.prefab");
            AssertPrefabHas<WateringCanTool2D>(
                ToolPrefabFolder + "P3_WateringCan.prefab");
            AssertPrefabHas<PestleTool2D>(
                ToolPrefabFolder + "P3_Pestle.prefab");
            AssertPrefabHas<GrappleLauncher2D>(
                ToolPrefabFolder + "P3_Grapple.prefab");
            AssertPrefabHas<WindUmbrellaMotor2D>(
                ToolPrefabFolder + "P3_WindUmbrella.prefab");
#endif
        }

        [UnityTest]
        public IEnumerator GrappleInGeneratedScene_HitsFixedAnchorAndIgnoresDynamicClutter()
        {
            yield return LoadP3Scene();

            PlayerToolInventory2D inventory =
                Object.FindFirstObjectByType<PlayerToolInventory2D>();
            Rigidbody2D playerBody = inventory != null
                ? inventory.GetComponent<Rigidbody2D>()
                : null;
            HandToolPickup2D grapplePickup =
                Object.FindObjectsByType<HandToolPickup2D>(
                        FindObjectsSortMode.None)
                    .SingleOrDefault(item =>
                        item.Kind == HandToolKind.Grapple);
            GrappleLauncher2D launcher = grapplePickup != null
                ? grapplePickup.GetComponent<GrappleLauncher2D>()
                : null;
            GameObject anchorObject = GameObject.Find("07_GrappleAnchor");
            Collider2D anchor = anchorObject != null
                ? anchorObject.GetComponent<Collider2D>()
                : null;

            Assert.That(inventory, Is.Not.Null);
            Assert.That(playerBody, Is.Not.Null);
            Assert.That(grapplePickup, Is.Not.Null);
            Assert.That(launcher, Is.Not.Null);
            Assert.That(anchor, Is.Not.Null);
            Assert.That(inventory.TryEquip(grapplePickup), Is.True);

            Transform muzzle = grapplePickup.transform.parent;
            Vector2 direction = Vector2.one.normalized;
            anchorObject.transform.position =
                (Vector2)muzzle.position + direction * 6f;
            Physics2D.SyncTransforms();
            Vector2 anchorCenter = anchor.bounds.center;

            GameObject clutter = new GameObject("Dynamic_Grapple_Clutter");
            clutter.transform.position =
                (Vector2)muzzle.position + direction * 2f;
            Rigidbody2D clutterBody = clutter.AddComponent<Rigidbody2D>();
            clutterBody.gravityScale = 0f;
            clutterBody.freezeRotation = true;
            BoxCollider2D clutterCollider =
                clutter.AddComponent<BoxCollider2D>();
            clutterCollider.size = Vector2.one * 0.5f;
            Physics2D.SyncTransforms();

            GrappleFireResult result = launcher.TryUse(direction);

            Assert.That(result.Fired, Is.True);
            Assert.That(
                result.TargetKind,
                Is.EqualTo(GrappleTargetKind.FixedTerrain));
            Assert.That(
                Vector2.Distance(result.HitPoint, anchorCenter),
                Is.LessThan(anchor.bounds.extents.magnitude + 0.05f));
            Assert.That(launcher.IsTravelling, Is.True);
            Object.Destroy(clutter);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PestleInGeneratedScene_BreaksRegisteredThinFloorBelow()
        {
            yield return LoadP3Scene();

            GridWorld world = Object.FindFirstObjectByType<GridWorld>();
            TileMutationService mutation =
                Object.FindFirstObjectByType<TileMutationService>();
            PestleTool2D pestle =
                Object.FindFirstObjectByType<PestleTool2D>();
            ThinFloorPestleTarget2D thinFloor =
                Object.FindFirstObjectByType<ThinFloorPestleTarget2D>();
            GridPos thinFloorCell = new GridPos(51, 2);

            Assert.That(world, Is.Not.Null);
            Assert.That(mutation, Is.Not.Null);
            Assert.That(pestle, Is.Not.Null);
            Assert.That(thinFloor, Is.Not.Null);
            Assert.That(
                world.TerrainTilemap.HasTile(
                    new Vector3Int(
                        thinFloorCell.X,
                        thinFloorCell.Y,
                        0)),
                Is.True);

            Assert.That(
                pestle.TryStrike(
                    new GridPos(
                        thinFloorCell.X,
                        thinFloorCell.Y + 1),
                    out PestleStrikeReport report),
                Is.True);
            Assert.That(report.ReactionCount, Is.EqualTo(1));
            Assert.That(
                report.CombinedReaction
                & PestleReactionKind.ThinFloorBreakQueued,
                Is.Not.EqualTo(PestleReactionKind.None));

            TileMutationBatchReport mutationReport =
                mutation.FlushPending();

            Assert.That(mutationReport.CommittedCount, Is.EqualTo(1));
            Assert.That(thinFloor.IsBroken, Is.True);
            Assert.That(
                world.TerrainTilemap.HasTile(
                    new Vector3Int(
                        thinFloorCell.X,
                        thinFloorCell.Y,
                        0)),
                Is.False);
            Assert.That(mutation.IsCurrentExitReachable(), Is.True);
            yield return null;
        }

        private static void AssertToolOrderAndTextlessCues()
        {
            NoTextToolCue2D[] cues =
                Object.FindObjectsByType<NoTextToolCue2D>(
                    FindObjectsSortMode.None);
            Assert.That(cues.Length, Is.EqualTo(8));

            P3ToolKind[] actual = cues
                .OrderBy(cue => cue.transform.position.x)
                .Select(cue => cue.ToolKind)
                .ToArray();
            Assert.That(actual, Is.EqualTo(P3ToolGardenContract.ToolOrder));
        }

        private static void AssertHandToolSet()
        {
            HandToolPickup2D[] pickups =
                Object.FindObjectsByType<HandToolPickup2D>(
                    FindObjectsSortMode.None);
            Assert.That(pickups.Length, Is.EqualTo(6));

            HandToolKind[] actual = pickups
                .OrderBy(pickup => pickup.transform.position.x)
                .Select(pickup => pickup.Kind)
                .ToArray();
            HandToolKind[] expected =
            {
                HandToolKind.Pickaxe,
                HandToolKind.Shovel,
                HandToolKind.WateringCan,
                HandToolKind.Pestle,
                HandToolKind.Grapple,
                HandToolKind.WindUmbrella
            };
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(
                pickups.Single(item => item.Kind == HandToolKind.Pickaxe)
                    .MaximumUses,
                Is.EqualTo(PickaxeTool2D.DefaultDurability));
            Assert.That(
                pickups.Single(item => item.Kind == HandToolKind.Shovel)
                    .MaximumUses,
                Is.EqualTo(ShovelTool2D.DefaultDurability));
            Assert.That(
                pickups.Single(item => item.Kind == HandToolKind.WateringCan)
                    .MaximumUses,
                Is.EqualTo(WateringCanTool2D.Capacity));
            Assert.That(
                pickups.Where(item =>
                        item.Kind == HandToolKind.Pestle
                        || item.Kind == HandToolKind.Grapple
                        || item.Kind == HandToolKind.WindUmbrella)
                    .All(item => !item.HasFiniteUses),
                Is.True);
        }

        private static void AssertToolDemonstrations()
        {
            Assert.That(
                Object.FindFirstObjectByType<RopeAnchor2D>(),
                Is.Not.Null);
            Assert.That(
                Object.FindFirstObjectByType<GrowableVinePlatform2D>(),
                Is.Not.Null);
            Assert.That(
                Object.FindFirstObjectByType<WaterSource2D>(),
                Is.Not.Null);
            Assert.That(
                Object.FindFirstObjectByType<DrivenStake2D>(),
                Is.Not.Null);
            Assert.That(
                Object.FindFirstObjectByType<ThinFloorPestleTarget2D>(),
                Is.Not.Null);
            Assert.That(
                Object.FindFirstObjectByType<GrapplePullable2D>(),
                Is.Not.Null);
            Assert.That(
                Object.FindFirstObjectByType<WindZone2D>(),
                Is.Not.Null);
        }

        private static void AssertNoVisibleTextComponents()
        {
            MonoBehaviour[] behaviours =
                Object.FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            string[] forbiddenTypeNames =
            {
                "UnityEngine.UI.Text",
                "TMPro.TextMeshPro",
                "TMPro.TextMeshProUGUI"
            };
            Assert.That(
                behaviours.Any(behaviour =>
                    behaviour != null
                    && forbiddenTypeNames.Contains(
                        behaviour.GetType().FullName)),
                Is.False,
                "P3 discovery cues must remain visual and text-free.");
            Assert.That(
                Object.FindObjectsByType<TextMesh>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None),
                Is.Empty,
                "P3 discovery cues must not use TextMesh.");
        }

#if UNITY_EDITOR
        private static void AssertPrefabHas<T>(string path)
            where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, $"Missing P3 prefab: {path}");
            Assert.That(
                prefab.GetComponentInChildren<T>(true),
                Is.Not.Null,
                $"{path} must contain {typeof(T).Name}.");
        }
#endif

        private static IEnumerator LoadP3Scene()
        {
#if UNITY_EDITOR
            Assert.That(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath),
                Is.Not.Null,
                $"Missing P3 integration scene: {ScenePath}");
            EditorSceneManager.LoadSceneInPlayMode(
                ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore(
                "P3 integration tests require the Unity Editor asset database.");
#endif
            yield return null;
            yield return new WaitForFixedUpdate();
            Physics2D.SyncTransforms();
        }
    }
}

#endif
