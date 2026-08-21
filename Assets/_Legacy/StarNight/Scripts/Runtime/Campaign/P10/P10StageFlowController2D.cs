#if LEGACY_DISABLED
using System;
using StarNight.Explosions;
using StarNight.Campaign.P11;
using StarNight.Folklore.P9;
using StarNight.Maru.P8;
using StarNight.Player;
using StarNight.Tools;
using StarNight.Tools.Water;
using StarNight.World;
using UnityEngine;

namespace StarNight.Campaign.P10
{
    [DefaultExecutionOrder(-930)]
    [DisallowMultipleComponent]
    public sealed class P10StageFlowController2D : MonoBehaviour
    {
        [SerializeField] private P10CampaignDirector2D director;
        [SerializeField] private P10StageNode2D[] stageNodes =
            Array.Empty<P10StageNode2D>();
        [SerializeField] private Transform persistentPlayer;
        [SerializeField] private Camera persistentCamera;
        [SerializeField] private Bomb2D bombPrefab;
        [SerializeField] private P8MaruStageController2D maruController;
        [SerializeField]
        private P11StageFlowController2D commonRegionContinuation;
        [SerializeField] private P10StageNode2D activeNode;

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

        public event Action<P10StageId> ActiveStageChanged;

        public P10CampaignDirector2D Director => director;
        public P10StageNode2D ActiveNode => activeNode;
        public Transform PersistentPlayer => persistentPlayer;
        public Camera PersistentCamera => persistentCamera;
        public P8MaruStageController2D MaruController =>
            maruController;
        public P11StageFlowController2D CommonRegionContinuation =>
            commonRegionContinuation;
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
                    P10StageEnvironment2D environment =
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
        public bool FolkloreStatePersistsAcrossStages =>
            director != null && director.FolkloreState != null;
        public bool MaruRuntimeActiveInHierarchy =>
            maruController != null
            && maruController.gameObject.activeInHierarchy
            && maruController.Timeline != null
            && maruController.Timeline.gameObject.activeInHierarchy
            && maruController.Pursuer != null
            && maruController.Pursuer.gameObject.activeInHierarchy
            && maruController.BiteController != null
            && maruController.BiteController.gameObject.activeInHierarchy;
        public bool MaruLifecyclePersistsAcrossStages =>
            MaruRuntimeActiveInHierarchy;
        public bool MaruIsAtActiveStageAnchor
        {
            get
            {
                if (activeNode == null
                    || activeNode.Environment == null
                    || maruController == null
                    || maruController.Pursuer == null)
                {
                    return false;
                }

                Transform anchor =
                    activeNode.Environment.MaruAnchor;
                return anchor != null
                    && ((Vector2)maruController.Pursuer
                            .transform.position
                        - (Vector2)anchor.position)
                    .sqrMagnitude <= 0.0001f;
            }
        }

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
            RebindMaruTo(activeNode.Environment);
            BeginMaruForStage(activeNode);
        }

        public void Configure(
            P10CampaignDirector2D campaignDirector,
            P10StageNode2D[] nodes,
            Transform player,
            Camera stageCamera,
            Bomb2D runtimeBombPrefab)
        {
            director = campaignDirector;
            stageNodes = nodes ?? Array.Empty<P10StageNode2D>();
            persistentPlayer = player;
            persistentCamera = stageCamera;
            bombPrefab = runtimeBombPrefab;
            CachePlayerComponents();
            ResolveAndDetachMaruFromP5Lifecycle();
            activeNode = null;
            for (int index = 0; index < stageNodes.Length; index++)
            {
                stageNodes[index]?.Environment
                    ?.SetEnvironmentActive(false);
            }
        }

        public void ConfigureCommonRegionContinuation(
            P11StageFlowController2D continuation)
        {
            commonRegionContinuation = continuation;
        }

        public P10StageNode2D FindNode(P10StageId stageId)
        {
            for (int index = 0; index < stageNodes.Length; index++)
            {
                P10StageNode2D node = stageNodes[index];
                if (node != null && node.StageId == stageId)
                {
                    return node;
                }
            }

            return null;
        }

        public bool TryActivateStage(P10StageId stageId)
        {
            P10StageNode2D next = FindNode(stageId);
            if (next == null || !next.TryEnter())
            {
                return false;
            }

            CachePlayerComponents();
            SetOnlyEnvironmentActive(next);
            activeNode = next;
            RebindPlayerTo(next.Environment);
            RebindMaruTo(next.Environment);
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

            P10StageId completed = activeNode.StageId;
            if (!activeNode.TryComplete())
            {
                return false;
            }

            maruController?.CompleteStage();
            if (advanceLinear)
            {
                P10StageId next = LinearNextAfter(completed);
                if (next != P10StageId.None)
                {
                    return TryActivateStage(next);
                }
            }

            return true;
        }

        public bool TryChooseBranchAndEnter(P9BranchKind branch)
        {
            if (director == null || !director.ChooseFirstBranch(branch))
            {
                return false;
            }

            return TryActivateStage(
                branch == P9BranchKind.MagpieBridge
                    ? P10StageId.MagpieBridge21
                    : P10StageId.DragonPalace21);
        }

        public bool TryOpenCrossRouteAndEnter(
            P9BranchKind sourceBranch)
        {
            if (director == null
                || !director.TryOpenCrossRouteFrom(sourceBranch))
            {
                return false;
            }

            return TryActivateStage(
                sourceBranch == P9BranchKind.MagpieBridge
                    ? P10StageId.DragonPalace22
                    : P10StageId.MagpieBridge22);
        }

        public bool TryEnterCommonRegion()
        {
            if (director == null
                || !director.TryEnterCommonRegion())
            {
                return false;
            }

            maruController?.CompleteStage();
            SetOnlyEnvironmentActive(null);
            activeNode = null;
            return commonRegionContinuation == null
                || commonRegionContinuation.BeginFromP10Handoff();
        }

        public void ResetFlowForTests()
        {
            director?.ResetCampaignForTests();
            maruController?.ResetStageForTests();
            for (int index = 0; index < stageNodes.Length; index++)
            {
                stageNodes[index]?.ResetForTests();
                stageNodes[index]?.Environment
                    ?.SetEnvironmentActive(false);
            }

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
                    FindFirstObjectByType<P8MaruStageController2D>();
            }
        }

        private void ResolveAndDetachMaruFromP5Lifecycle()
        {
            if (maruController == null)
            {
                maruController =
                    FindFirstObjectByType<P8MaruStageController2D>();
            }

            if (maruController == null)
            {
                return;
            }

            maruController.Configure(
                maruController.Timeline,
                maruController.Pursuer,
                maruController.BiteController,
                maruController.Telemetry);
        }

        private void BeginMaruForStage(P10StageNode2D node)
        {
            if (maruController == null
                || maruController.Timeline == null
                || node == null
                || node.Definition == null)
            {
                return;
            }

            P8MaruTimelineProfile profile =
                P8MaruTimelineProfile.Create(
                    node.Definition.StageSlot,
                    node.Definition.IsBossStage);
            float multiplier =
                director != null && director.IsSecondBranchMode
                    ? director.SecondBranchBellIntervalMultiplier
                    : 1f;
            if (!Mathf.Approximately(multiplier, 1f))
            {
                profile = new P8MaruTimelineProfile(
                    profile.StageSlot,
                    profile.FirstBellSeconds * multiplier,
                    profile.SecondBellSeconds * multiplier,
                    profile.MaruDueSeconds * multiplier,
                    profile.PausedForBoss);
            }

            maruController.Timeline.Configure(profile);
            maruController.ResetStageForTests();
            maruController.BeginStage();
        }

        private void SetOnlyEnvironmentActive(
            P10StageNode2D target)
        {
            for (int index = 0; index < stageNodes.Length; index++)
            {
                P10StageNode2D node = stageNodes[index];
                node?.Environment?.SetEnvironmentActive(
                    node == target);
            }
        }

        private void RebindPlayerTo(
            P10StageEnvironment2D environment)
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
            }

            motor.ResetMotionAfterRecovery();
            Physics2D.SyncTransforms();
        }

        private void RebindMaruTo(
            P10StageEnvironment2D environment)
        {
            if (maruController == null || environment == null)
            {
                return;
            }

            P8MaruPursuer2D pursuer = maruController.Pursuer;
            if (pursuer == null)
            {
                return;
            }

            pursuer.StopHunt();
            Transform anchor = environment.MaruAnchor;
            Vector3 pursuerPosition = anchor != null
                ? anchor.position
                : environment.EntryAnchor != null
                    ? environment.EntryAnchor.position
                    : Vector3.zero;
            pursuerPosition.z = pursuer.transform.position.z;
            pursuer.transform.position = pursuerPosition;

            P8ReturnPile2D returnPile = pursuer.ReturnPile;
            if (returnPile != null
                && environment.EntryAnchor != null)
            {
                Vector3 pilePosition =
                    environment.EntryAnchor.position;
                pilePosition.z = returnPile.transform.position.z;
                returnPile.transform.position = pilePosition;
            }

            Physics2D.SyncTransforms();
        }

        private static P10StageId LinearNextAfter(
            P10StageId completed)
        {
            switch (completed)
            {
                case P10StageId.MoonPalace11:
                    return P10StageId.MoonPalace12;
                case P10StageId.MoonPalace12:
                    return P10StageId.MoonPalace13;
                case P10StageId.MagpieBridge21:
                    return P10StageId.MagpieBridge22;
                case P10StageId.MagpieBridge22:
                    return P10StageId.MagpieBridge23;
                case P10StageId.DragonPalace21:
                    return P10StageId.DragonPalace22;
                case P10StageId.DragonPalace22:
                    return P10StageId.DragonPalace23;
                default:
                    return P10StageId.None;
            }
        }
    }
}

#endif
