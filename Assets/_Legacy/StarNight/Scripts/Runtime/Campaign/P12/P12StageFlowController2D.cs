#if LEGACY_DISABLED
using System;
using StarNight.Campaign.P11;
using StarNight.Explosions;
using StarNight.Generation.P6;
using StarNight.Maru.P8;
using StarNight.Player;
using StarNight.Tools;
using StarNight.Tools.Water;
using StarNight.World;
using UnityEngine;

namespace StarNight.Campaign.P12
{
    [DefaultExecutionOrder(-880)]
    [DisallowMultipleComponent]
    public sealed class P12StageFlowController2D : MonoBehaviour
    {
        [SerializeField] private P12ChallengeDirector2D director;
        [SerializeField] private P12StageNode2D[] stageNodes =
            Array.Empty<P12StageNode2D>();
        [SerializeField] private Transform persistentPlayer;
        [SerializeField] private Camera persistentCamera;
        [SerializeField] private Bomb2D bombPrefab;
        [SerializeField] private P8MaruStageController2D maruController;
        [SerializeField] private P12ChallengeEntry2D challengeEntry;
        [SerializeField] private P12StageNode2D activeNode;

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
        private P11CampaignDirector2D subscribedP11Director;
        private P12ChallengeDirector2D subscribedDirector;

        public event Action<P12StageId> ActiveStageChanged;

        public P12ChallengeDirector2D Director => director;
        public P12StageNode2D ActiveNode => activeNode;
        public Transform PersistentPlayer => persistentPlayer;
        public Camera PersistentCamera => persistentCamera;
        public P8MaruStageController2D MaruController =>
            maruController;
        public P12ChallengeEntry2D ChallengeEntry => challengeEntry;
        public int StageNodeCount => stageNodes != null
            ? stageNodes.Length
            : 0;
        public bool UsesOnePersistentPlayer =>
            persistentPlayer != null
            && persistentPlayer.GetComponent<PlayerMotor2D>() != null
            && persistentPlayer.GetComponent<PlayerInputAdapter>() != null;
        public bool UsesOnePersistentCamera =>
            persistentCamera != null
            && persistentCamera.CompareTag("MainCamera");
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

        private void OnEnable()
        {
            Subscribe();
            RefreshChallengeEntry();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Start()
        {
            RefreshChallengeEntry();
            if (activeNode == null)
            {
                return;
            }

            RebindPlayerTo(activeNode.Environment);
            BeginMaruForStage(activeNode);
        }

        public void Configure(
            P12ChallengeDirector2D challengeDirector,
            P12StageNode2D[] nodes,
            Transform player,
            Camera stageCamera,
            Bomb2D runtimeBombPrefab,
            P8MaruStageController2D persistentMaruController,
            P12ChallengeEntry2D entry)
        {
            Unsubscribe();
            director = challengeDirector;
            stageNodes = nodes ?? Array.Empty<P12StageNode2D>();
            persistentPlayer = player;
            persistentCamera = stageCamera;
            bombPrefab = runtimeBombPrefab;
            maruController = persistentMaruController;
            challengeEntry = entry;
            CachePlayerComponents();
            activeNode = null;
            SetOnlyEnvironmentActive(null);
            Subscribe();
            RefreshChallengeEntry();
        }

        public P12StageNode2D FindNode(P12StageId stageId)
        {
            for (int index = 0; index < stageNodes.Length; index++)
            {
                P12StageNode2D node = stageNodes[index];
                if (node != null && node.StageId == stageId)
                {
                    return node;
                }
            }

            return null;
        }

        public bool BeginFromP11Handoff()
        {
            return director != null
                && director.TryAcceptP11Handoff()
                && TryActivateStage(P12StageId.StarlessSea01);
        }

        public bool BeginAtChallengeForTests()
        {
            if (director == null)
            {
                return false;
            }

            director.ResetChallengeForTests(true);
            return TryActivateStage(P12StageId.StarlessSea01);
        }

        public bool TryActivateStage(P12StageId stageId)
        {
            P12StageNode2D next = FindNode(stageId);
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

            P12StageId completed = activeNode.StageId;
            if (!activeNode.TryComplete())
            {
                return false;
            }

            maruController?.CompleteStage();
            activeNode = null;
            if (advanceLinear)
            {
                P12StageId next = LinearNextAfter(completed);
                if (next != P12StageId.None)
                {
                    return TryActivateStage(next);
                }
            }

            SetOnlyEnvironmentActive(null);
            return true;
        }

        public bool TryFailActiveStage(string cause = null)
        {
            if (activeNode == null
                || director == null
                || director.CurrentStage != activeNode.StageId)
            {
                return false;
            }

            maruController?.CompleteStage();
            activeNode = null;
            return director.TryFailCurrentStage(cause);
        }

        public void ResetFlowForTests()
        {
            director?.ResetChallengeForTests(false);
            maruController?.ResetStageForTests();
            SetOnlyEnvironmentActive(null);
            activeNode = null;
            RefreshChallengeEntry();
        }

        private void HandleP11EndingResolved(P11EndingKind ending)
        {
            RefreshChallengeEntry();
        }

        private void HandleSegmentRestarted(
            P12ChallengeSegment segment)
        {
            TryActivateStage(
                P12ChallengeDirector2D.FirstStageOf(segment));
        }

        private void HandleChallengeFailed()
        {
            maruController?.CompleteStage();
            activeNode = null;
            SetOnlyEnvironmentActive(null);
        }

        private void RefreshChallengeEntry()
        {
            if (challengeEntry == null)
            {
                return;
            }

            P11CampaignDirector2D p11 = director != null
                ? director.P11Director
                : null;
            challengeEntry.gameObject.SetActive(
                p11 != null
                && p11.Ending == P11EndingKind.Memory);
        }

        private void Subscribe()
        {
            Unsubscribe();
            subscribedDirector = director;
            if (subscribedDirector != null)
            {
                subscribedDirector.SegmentCheckpointRestarted +=
                    HandleSegmentRestarted;
                subscribedDirector.ChallengeFailed +=
                    HandleChallengeFailed;
                subscribedP11Director =
                    subscribedDirector.P11Director;
            }

            if (subscribedP11Director != null)
            {
                subscribedP11Director.EndingResolved +=
                    HandleP11EndingResolved;
            }
        }

        private void Unsubscribe()
        {
            if (subscribedDirector != null)
            {
                subscribedDirector.SegmentCheckpointRestarted -=
                    HandleSegmentRestarted;
                subscribedDirector.ChallengeFailed -=
                    HandleChallengeFailed;
                subscribedDirector = null;
            }

            if (subscribedP11Director != null)
            {
                subscribedP11Director.EndingResolved -=
                    HandleP11EndingResolved;
                subscribedP11Director = null;
            }
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

        private void BeginMaruForStage(P12StageNode2D node)
        {
            if (maruController == null
                || maruController.Timeline == null
                || node == null
                || node.Definition == null)
            {
                return;
            }

            P12StageEnvironment2D environment = node.Environment;
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
                P12ChallengeDifficulty.CreateShortenedProfile(
                    SlotOf(node.StageId),
                    node.Definition.EarlyMaru);
            maruController.Timeline.Configure(profile);
            maruController.ResetStageForTests();
            maruController.BeginStage();
        }

        private void SetOnlyEnvironmentActive(
            P12StageNode2D target)
        {
            if (stageNodes == null)
            {
                return;
            }

            for (int index = 0; index < stageNodes.Length; index++)
            {
                P12StageNode2D node = stageNodes[index];
                node?.Environment?.SetEnvironmentActive(
                    node == target);
            }
        }

        private void RebindPlayerTo(
            P12StageEnvironment2D environment)
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
                cameraPosition.z =
                    persistentCamera.transform.position.z;
                persistentCamera.transform.position = cameraPosition;
                persistentCamera.orthographicSize = 7f;
            }

            motor.ResetMotionAfterRecovery();
            Physics2D.SyncTransforms();
        }

        private static P6StageSlot SlotOf(P12StageId stageId)
        {
            int offset =
                (int)stageId - (int)P12StageId.StarlessSea01;
            return (P6StageSlot)(offset % 3 + 1);
        }

        private static P12StageId LinearNextAfter(
            P12StageId completed)
        {
            switch (completed)
            {
                case P12StageId.StarlessSea01:
                    return P12StageId.StarlessSea02;
                case P12StageId.StarlessSea02:
                    return P12StageId.StarlessSea03;
                case P12StageId.StarlessSea03:
                    return P12StageId.StarlessSea04;
                case P12StageId.StarlessSea04:
                    return P12StageId.StarlessSea05;
                case P12StageId.StarlessSea05:
                    return P12StageId.StarlessSea06;
                case P12StageId.StarlessSea06:
                    return P12StageId.StarlessSea07;
                case P12StageId.StarlessSea07:
                    return P12StageId.StarlessSea08;
                case P12StageId.StarlessSea08:
                    return P12StageId.StarlessSea09;
                case P12StageId.StarlessSea09:
                    return P12StageId.StarlessSea10;
                case P12StageId.StarlessSea10:
                    return P12StageId.StarlessSea11;
                case P12StageId.StarlessSea11:
                    return P12StageId.StarlessSea12;
                default:
                    return P12StageId.None;
            }
        }
    }
}

#endif
