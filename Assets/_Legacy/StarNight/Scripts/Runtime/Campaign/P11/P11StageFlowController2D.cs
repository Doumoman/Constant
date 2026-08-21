#if LEGACY_DISABLED
using System;
using StarNight.Campaign.P10;
using StarNight.Explosions;
using StarNight.Maru.P8;
using StarNight.Player;
using StarNight.Tools;
using StarNight.Tools.Water;
using StarNight.World;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DefaultExecutionOrder(-900)]
    [DisallowMultipleComponent]
    public sealed class P11StageFlowController2D : MonoBehaviour
    {
        [SerializeField] private P11CampaignDirector2D director;
        [SerializeField] private P11StageNode2D[] stageNodes =
            Array.Empty<P11StageNode2D>();
        [SerializeField] private Transform persistentPlayer;
        [SerializeField] private Camera persistentCamera;
        [SerializeField] private Bomb2D bombPrefab;
        [SerializeField] private P8MaruStageController2D maruController;
        [SerializeField] private P11StageNode2D activeNode;

        private PlayerInputAdapter input;
        private PlayerMotor2D motor;
        private Rigidbody2D body;
        private CapsuleCollider2D capsule;
        private SafeCellTracker safeCells;
        private PlayerRecovery recovery;
        private StarNight.Objects.CarrySystem carry;
        private PlayerToolInventory2D inventory;
        private PlayerConsumableTools2D consumables;
        private GridBoundedCamera2D cameraFollow;

        public event Action<P11StageId> ActiveStageChanged;

        public P11CampaignDirector2D Director => director;
        public P11StageNode2D ActiveNode => activeNode;
        public Transform PersistentPlayer => persistentPlayer;
        public Camera PersistentCamera => persistentCamera;
        public P8MaruStageController2D MaruController =>
            maruController;
        public int StageNodeCount => stageNodes != null
            ? stageNodes.Length
            : 0;
        public int ActiveEnvironmentCount
        {
            get
            {
                int count = 0;
                if (stageNodes == null)
                {
                    return count;
                }

                for (int index = 0;
                     index < stageNodes.Length;
                     index++)
                {
                    P11StageEnvironment2D environment =
                        stageNodes[index] != null
                            ? stageNodes[index].Environment
                            : null;
                    if (environment != null
                        && environment.EnvironmentRoot != null
                        && environment.EnvironmentRoot.activeSelf)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
        public bool UsesOnePersistentPlayer =>
            persistentPlayer != null
            && persistentPlayer.GetComponent<PlayerMotor2D>() != null
            && persistentPlayer.GetComponent<PlayerInputAdapter>() != null;
        public bool UsesOnePersistentCamera =>
            persistentCamera != null
            && persistentCamera.CompareTag("MainCamera");
        public bool InventoryPersistsAcrossStages =>
            persistentPlayer != null
            && persistentPlayer.GetComponent<PlayerToolInventory2D>()
                != null
            && persistentPlayer.GetComponent<PlayerConsumableTools2D>()
                != null;
        public bool StoryStatePersistsAcrossStages =>
            director != null && director.StoryState != null;
        public bool MaruLifecyclePersistsAcrossStages =>
            maruController != null
            && maruController.gameObject.activeInHierarchy
            && maruController.Timeline != null
            && maruController.Pursuer != null
            && maruController.BiteController != null;

        private void Awake()
        {
            CachePlayerComponents();
        }

        private void Start()
        {
            if (activeNode == null)
            {
                return;
            }

            RebindPlayerTo(activeNode.Environment);
            BeginMaruForStage(activeNode);
        }

        public void Configure(
            P11CampaignDirector2D campaignDirector,
            P11StageNode2D[] nodes,
            Transform player,
            Camera stageCamera,
            Bomb2D runtimeBombPrefab,
            P8MaruStageController2D persistentMaruController)
        {
            director = campaignDirector;
            stageNodes = nodes ?? Array.Empty<P11StageNode2D>();
            persistentPlayer = player;
            persistentCamera = stageCamera;
            bombPrefab = runtimeBombPrefab;
            maruController = persistentMaruController;
            CachePlayerComponents();
            activeNode = null;
            SetOnlyEnvironmentActive(null);
        }

        public P11StageNode2D FindNode(P11StageId stageId)
        {
            for (int index = 0; index < stageNodes.Length; index++)
            {
                P11StageNode2D node = stageNodes[index];
                if (node != null && node.StageId == stageId)
                {
                    return node;
                }
            }

            return null;
        }

        public bool BeginFromP10Handoff()
        {
            return director != null
                && director.TryAcceptP10Handoff()
                && TryActivateStage(P11StageId.StarPostOffice31);
        }

        public bool BeginAtCommonRegionForTests()
        {
            if (director == null)
            {
                return false;
            }

            P10StageFlowController2D p10Flow =
                FindFirstObjectByType<P10StageFlowController2D>(
                    FindObjectsInactive.Include);
            p10Flow?.ResetFlowForTests();
            director.ResetCampaignForTests(true);
            return TryActivateStage(P11StageId.StarPostOffice31);
        }

        public bool TryActivateStage(P11StageId stageId)
        {
            P11StageNode2D next = FindNode(stageId);
            if (next == null || !next.TryEnter())
            {
                return false;
            }

            CachePlayerComponents();
            SetOnlyEnvironmentActive(next);
            activeNode = next;
            RebindPlayerTo(next.Environment);
            BeginMaruForStage(next);
            ActiveStageChanged?.Invoke(stageId);
            return true;
        }

        public bool TryCompleteActiveStage(bool advanceLinear = true)
        {
            if (activeNode == null)
            {
                return false;
            }

            P11StageId completed = activeNode.StageId;
            if (!activeNode.TryComplete())
            {
                return false;
            }

            maruController?.CompleteStage();
            activeNode = null;
            if (advanceLinear)
            {
                P11StageId next = LinearNextAfter(completed);
                if (next != P11StageId.None)
                {
                    return TryActivateStage(next);
                }
            }

            SetOnlyEnvironmentActive(null);
            return true;
        }

        public bool ResolveFinalEnding(P11EndingKind ending)
        {
            if (activeNode == null
                || activeNode.StageId
                    != P11StageId.PolarisObservatory53
                || director == null
                || !director.TryResolveEnding(ending))
            {
                return false;
            }

            activeNode.MarkBossDefeated();
            maruController?.CompleteStage();
            return true;
        }

        public void ResetFlowForTests(bool startAtCommonRegion = false)
        {
            director?.ResetCampaignForTests(startAtCommonRegion);
            maruController?.ResetStageForTests();
            for (int index = 0; index < stageNodes.Length; index++)
            {
                stageNodes[index]?.ResetForTests();
            }

            SetOnlyEnvironmentActive(null);
            activeNode = null;
        }

        private void CachePlayerComponents()
        {
            if (persistentPlayer == null)
            {
                return;
            }

            input = persistentPlayer.GetComponent<PlayerInputAdapter>();
            motor = persistentPlayer.GetComponent<PlayerMotor2D>();
            body = persistentPlayer.GetComponent<Rigidbody2D>();
            capsule =
                persistentPlayer.GetComponent<CapsuleCollider2D>();
            safeCells =
                persistentPlayer.GetComponent<SafeCellTracker>();
            recovery =
                persistentPlayer.GetComponent<PlayerRecovery>();
            carry =
                persistentPlayer.GetComponent<
                    StarNight.Objects.CarrySystem>();
            inventory =
                persistentPlayer.GetComponent<PlayerToolInventory2D>();
            consumables =
                persistentPlayer.GetComponent<
                    PlayerConsumableTools2D>();
            cameraFollow = persistentCamera != null
                ? persistentCamera.GetComponent<GridBoundedCamera2D>()
                : null;
            if (maruController == null)
            {
                maruController =
                    FindFirstObjectByType<P8MaruStageController2D>(
                        FindObjectsInactive.Include);
            }
        }

        private void BeginMaruForStage(P11StageNode2D node)
        {
            if (maruController == null
                || maruController.Timeline == null
                || node == null
                || node.Definition == null)
            {
                return;
            }

            P11StageEnvironment2D environment = node.Environment;
            if (environment != null
                && environment.MaruRoutingReady
                && maruController.Pursuer != null)
            {
                maruController.Pursuer.Configure(
                    environment.MaruRoomGraph,
                    environment.ReturnPile,
                    maruController.BiteController,
                    null);
                Vector2 maruEntry =
                    environment.MaruEntryAnchor.position;
                maruController.Pursuer.transform.position =
                    new Vector3(
                        maruEntry.x,
                        maruEntry.y,
                        maruController.Pursuer.transform.position.z);
            }

            P8MaruTimelineProfile profile =
                P8MaruTimelineProfile.Create(
                    node.Definition.StageSlot,
                    node.Definition.IsBossStage);
            maruController.Timeline.Configure(profile);
            maruController.ResetStageForTests();
            maruController.BeginStage();
        }

        private void SetOnlyEnvironmentActive(
            P11StageNode2D target)
        {
            if (stageNodes == null)
            {
                return;
            }

            for (int index = 0; index < stageNodes.Length; index++)
            {
                P11StageNode2D node = stageNodes[index];
                node?.Environment?.SetEnvironmentActive(
                    node == target);
            }
        }

        private void RebindPlayerTo(
            P11StageEnvironment2D environment)
        {
            if (environment == null
                || environment.GridWorld == null
                || persistentPlayer == null
                || body == null
                || capsule == null
                || motor == null)
            {
                return;
            }

            Vector2 entry = environment.EntryAnchor != null
                ? environment.EntryAnchor.position
                : Vector2.one;
            body.position = entry;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            persistentPlayer.position = new Vector3(
                entry.x,
                entry.y,
                persistentPlayer.position.z);
            safeCells?.Configure(
                environment.GridWorld,
                body,
                capsule,
                motor,
                motor.Tuning);
            safeCells?.SetSpawnFallback(entry);
            recovery?.Configure(
                environment.GridWorld,
                body,
                motor,
                safeCells,
                motor.Tuning);

            Transform holdAnchor =
                persistentPlayer.Find("CarryAnchor");
            carry?.Configure(
                input,
                body,
                holdAnchor,
                environment.GridWorld);
            inventory?.Configure(
                input,
                carry,
                motor,
                body,
                capsule,
                environment.GridWorld,
                holdAnchor,
                persistentCamera,
                environment.WaterRegistry,
                environment.PestleRegistry,
                Array.Empty<WaterSource2D>(),
                null);

            int ropes = consumables != null
                ? consumables.RopeStock
                : PlayerConsumableTools2D.DefaultRopeStock;
            int bombs = consumables != null
                ? consumables.BombStock
                : PlayerConsumableTools2D.DefaultBombStock;
            consumables?.Configure(
                input,
                body,
                environment.GridWorld,
                environment.RopeInstaller,
                environment.ExplosionService,
                bombPrefab,
                environment.SpawnedConsumablesRoot,
                null,
                ropes,
                bombs);

            if (cameraFollow == null && persistentCamera != null)
            {
                cameraFollow = persistentCamera.gameObject
                    .AddComponent<GridBoundedCamera2D>();
            }

            cameraFollow?.Configure(
                persistentCamera,
                persistentPlayer,
                body,
                environment.GridWorld,
                recovery);
            if (persistentCamera != null
                && environment.CameraAnchor != null)
            {
                Vector3 cameraPosition =
                    environment.CameraAnchor.position;
                cameraPosition.z = persistentCamera.transform.position.z;
                persistentCamera.transform.position = cameraPosition;
                persistentCamera.orthographicSize = 7f;
            }

            motor.ResetMotionAfterRecovery();
            Physics2D.SyncTransforms();
        }

        private static P11StageId LinearNextAfter(
            P11StageId completed)
        {
            switch (completed)
            {
                case P11StageId.StarPostOffice31:
                    return P11StageId.StarPostOffice32;
                case P11StageId.StarPostOffice32:
                    return P11StageId.StarPostOffice33;
                case P11StageId.StarPostOffice33:
                    return P11StageId.SunriseGarden41;
                case P11StageId.SunriseGarden41:
                    return P11StageId.SunriseGarden42;
                case P11StageId.SunriseGarden42:
                    return P11StageId.SunriseGarden43;
                case P11StageId.SunriseGarden43:
                    return P11StageId.PolarisObservatory51;
                case P11StageId.PolarisObservatory51:
                    return P11StageId.PolarisObservatory52;
                case P11StageId.PolarisObservatory52:
                    return P11StageId.PolarisObservatory53;
                default:
                    return P11StageId.None;
            }
        }
    }
}

#endif
