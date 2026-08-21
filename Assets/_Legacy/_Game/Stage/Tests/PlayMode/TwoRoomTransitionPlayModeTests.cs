#if LEGACY_DISABLED
using System.Collections;
using System.Linq;
using NUnit.Framework;
using StarNight.Interaction.Carry;
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.Input;
using StarNight.Map;
using StarNight.Player.Motor;
using StarNight.Player.Safety;
using StarNight.Stage.Lab;
using StarNight.Stage.Rooms;
using StarNight.Stage.Secrets;
using StarNight.Stage.Streaming;
using StarNight.Stage.Transitions;
using StarNight.Stage.Visuals;
using StarNight.Tools.Compass;
using StarNight.Tools.Core;
using StarNight.Tools.Inventory;
using StarNight.Tools.Items;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace StarNight.Stage.Tests
{
    public sealed class TwoRoomTransitionPlayModeTests
    {
        private const int GroundLayer = 7;
        private readonly WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (SceneManager.GetSceneByName("10_Prologue_0_1").isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync("10_Prologue_0_1");
            }

            if (SceneManager.GetSceneByName("02_RunShell").isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync("02_RunShell");
            }

            DestroyNamed("Core04TestLab");
            DestroyNamed("Core04TestPlayer");
            DestroyNamed("Core04TestCamera");
            DestroyNamed("CriticalCarryTestObject");
            DestroyNamed("CriticalCarryTestAnchor");
            yield return null;
        }

        [UnityTest]
        public IEnumerator CameraTransitionActivatesDestinationAndFreezesPreviousRoom()
        {
            Core04TwoRoomLab lab = CreateLab(out PlayerMotor2D player, out Camera camera);
            yield return waitForFixedUpdate;

            Assert.That(lab.RoomA.SimulationState, Is.EqualTo(RoomSimulationState.Active));
            Assert.That(lab.RoomB.SimulationState, Is.EqualTo(RoomSimulationState.NeighborPreview));
            Assert.That(lab.StreamingManager.GetState("Room_A"), Is.EqualTo(RoomInstanceState.Active));
            Assert.That(lab.StreamingManager.GetState("Room_B"), Is.EqualTo(RoomInstanceState.WarmLoaded));
            int destinationInstanceId = lab.RoomB.GetInstanceID();
            Assert.That(lab.CameraController.IsViewportInside(lab.RoomA), Is.True);
            Assert.That(IsVisible(camera, player.Body.position), Is.True);

            float started = Time.unscaledTime;
            Assert.That(lab.TransitionController.TryCommit(lab.PortalAtoB), Is.True);
            while (lab.TransitionController.IsTransitioning)
            {
                yield return null;
            }
            float elapsed = Time.unscaledTime - started;

            Assert.That(elapsed, Is.InRange(0.20f, 0.45f));
            Assert.That(lab.TransitionController.CurrentRoom, Is.SameAs(lab.RoomB));
            Assert.That(lab.RoomA.SimulationState, Is.EqualTo(RoomSimulationState.Frozen));
            Assert.That(lab.RoomB.SimulationState, Is.EqualTo(RoomSimulationState.Active));
            Assert.That(lab.StreamingManager.GetState("Room_A"), Is.EqualTo(RoomInstanceState.FrozenVisited));
            Assert.That(lab.StreamingManager.GetState("Room_B"), Is.EqualTo(RoomInstanceState.Active));
            Assert.That(lab.RoomB.GetInstanceID(), Is.EqualTo(destinationInstanceId));
            Assert.That(lab.CameraController.IsViewportInside(lab.RoomB), Is.True);
            Assert.That(Vector2.Distance(player.Body.position, lab.PortalBtoA.EntryAnchor.position), Is.LessThan(0.02f));
            Assert.That(IsVisible(camera, player.Body.position), Is.True);
            Assert.That(player.GetComponent<PlayerActionLock>().State, Is.EqualTo(PlayerActionState.Free));

            Object.Destroy(lab.gameObject);
            Object.Destroy(player.gameObject);
            Object.Destroy(camera.gameObject);
        }

        [UnityTest]
        public IEnumerator OneHundredRoundTripsKeepStableEntryPositionAndSimulationOwnership()
        {
            Core04TwoRoomLab lab = CreateLab(out PlayerMotor2D player, out Camera camera);
            yield return waitForFixedUpdate;

            for (int index = 0; index < 100; index++)
            {
                Assert.That(lab.TransitionController.CommitImmediate(lab.PortalAtoB), Is.True, $"A to B failed at trip {index}.");
                Assert.That(Vector2.Distance(player.Body.position, lab.PortalBtoA.EntryAnchor.position), Is.LessThan(0.02f));
                Assert.That(IsVisible(camera, player.Body.position), Is.True);
                Assert.That(lab.TransitionController.CommitImmediate(lab.PortalBtoA), Is.True, $"B to A failed at trip {index}.");
                Assert.That(Vector2.Distance(player.Body.position, lab.PortalAtoB.EntryAnchor.position), Is.LessThan(0.02f));
                Assert.That(IsVisible(camera, player.Body.position), Is.True);
            }

            Assert.That(lab.TransitionController.CurrentRoom, Is.SameAs(lab.RoomA));
            Assert.That(lab.RoomA.SimulationState, Is.EqualTo(RoomSimulationState.Active));
            Assert.That(lab.RoomB.SimulationState, Is.EqualTo(RoomSimulationState.Frozen));
            Rigidbody2D[] previousRoomBodies = lab.RoomB.transform.Find("DynamicRoot").GetComponentsInChildren<Rigidbody2D>(true);
            Assert.That(previousRoomBodies, Is.Not.Empty);
            Assert.That(previousRoomBodies, Has.All.Matches<Rigidbody2D>(body => !body.simulated));
            Assert.That(lab.CameraController.IsViewportInside(lab.RoomA), Is.True);

            Object.Destroy(lab.gameObject);
            Object.Destroy(player.gameObject);
            Object.Destroy(camera.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PersistentCrateReturnsToCapturedPositionAfterRoomRoundTrip()
        {
            Core04TwoRoomLab lab = CreateLab(out PlayerMotor2D player, out Camera camera);
            yield return waitForFixedUpdate;
            RoomPersistentTransform2D persistent = lab.RoomA.GetComponentInChildren<RoomPersistentTransform2D>(true);
            Rigidbody2D crate = persistent.GetComponent<Rigidbody2D>();
            Vector2 savedPosition = new Vector2(-10.25f, -2f);
            crate.position = savedPosition;
            crate.transform.position = savedPosition;
            Physics2D.SyncTransforms();

            Assert.That(lab.TransitionController.CommitImmediate(lab.PortalAtoB), Is.True);
            crate.position = new Vector2(75f, 75f);
            Assert.That(lab.TransitionController.CommitImmediate(lab.PortalBtoA), Is.True);

            Assert.That(crate.position.x, Is.EqualTo(savedPosition.x).Within(0.001f));
            Assert.That(crate.position.y, Is.EqualTo(savedPosition.y).Within(0.001f));
            Assert.That(lab.RoomA.PersistentState.Revision, Is.GreaterThanOrEqualTo(1));

            Object.Destroy(lab.gameObject);
            Object.Destroy(player.gameObject);
            Object.Destroy(camera.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PortalSeamHasGroundSupportOnBothIndependentRoomSides()
        {
            Core04TwoRoomLab lab = CreateLab(out PlayerMotor2D player, out Camera camera);
            yield return waitForFixedUpdate;
            Physics2D.SyncTransforms();
            int mask = 1 << GroundLayer;

            RaycastHit2D left = Physics2D.Raycast(new Vector2(-0.01f, -2f), Vector2.down, 2f, mask);
            RaycastHit2D right = Physics2D.Raycast(new Vector2(0.01f, -2f), Vector2.down, 2f, mask);

            Assert.That(left.collider, Is.Not.Null);
            Assert.That(right.collider, Is.Not.Null);
            Assert.That(left.point.y, Is.EqualTo(right.point.y).Within(0.01f));

            Object.Destroy(lab.gameObject);
            Object.Destroy(player.gameObject);
            Object.Destroy(camera.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SecretAnchorCreatesOnceReturnsExactlyAndKeepsRoomState()
        {
            Core04TwoRoomLab lab = CreateLab(out PlayerMotor2D player, out Camera camera);
            yield return waitForFixedUpdate;
            SecretAnchor anchor = lab.PrototypeSecretAnchor;
            SecretDimensionController secrets = lab.SecretDimensionController;
            Assert.That(anchor, Is.Not.Null);
            Assert.That(secrets.SecretRoomCount, Is.Zero);
            Assert.That(secrets.PlannedSecretCount, Is.EqualTo(1));
            Assert.That(secrets.TimeContinues, Is.True);
            Assert.That(secrets.CanMaruEnterSecret(anchor.AnchorStableId), Is.False);
            Assert.That(secrets.TryGetPlan(anchor.AnchorStableId, out SecretDimensionPlan secretPlan), Is.True);
            Assert.That(secretPlan.SecretId, Is.EqualTo(anchor.SecretRoomId));
            Assert.That(anchor.StableSecretSeed, Is.EqualTo(
                SecretSeedUtility.Create(0x51A7, "Room_A", "ROOM_A_SECRET_01")));
            Assert.That(secretPlan.Seed, Is.EqualTo(anchor.StableSecretSeed));

            HandToolDefinition compassDefinition = ScriptableObject.CreateInstance<HandToolDefinition>();
            compassDefinition.Configure(
                "ITEM_MOON_EYE_COMPASS",
                "달눈 나침반",
                ToolTag.None,
                ToolResourceMode.Infinite,
                0,
                500,
                new ToolActionProfile(),
                new ToolActionProfile(),
                System.Array.Empty<Vector2Int>());
            compassDefinition.ConfigureItemContract(
                302,
                ItemUseCategory.PassiveDetector,
                0,
                true,
                true,
                false,
                302);
            GameObject compassObject = new GameObject("SecretTestCompass");
            MoonEyeCompassRuntime compass = compassObject.AddComponent<MoonEyeCompassRuntime>();
            compass.Configure(compassDefinition);
            EquipmentInventory inventory = player.GetComponent<EquipmentInventory>();
            Assert.That(inventory.ResolvePickup(compass), Is.EqualTo(EquipmentPickupResult.Added));
            SecretDetectorController detector = player.GetComponent<SecretDetectorController>();
            detector.RefreshDetection();
            Assert.That(detector.HasMoonEyeCompass, Is.True);
            Assert.That(detector.Band, Is.Not.EqualTo(SecretDetectionBand.None));

            ToolReactionResult reveal = anchor.TryReact(new ToolReactionContext
            {
                ActionId = 7001,
                Tags = ToolTag.Pickaxe,
            });
            Assert.That(reveal.Accepted, Is.True);
            Assert.That(reveal.ConsumeToolResource, Is.True);
            Assert.That(secrets.SecretRoomCount, Is.EqualTo(1));
            Assert.That(secrets.CanMaruEnterSecret(anchor.AnchorStableId), Is.True);
            Assert.That(secrets.TryGetSecretRoom(anchor.AnchorStableId, out RoomRuntime secretRoom), Is.True);
            Assert.That(secretRoom.Dimension, Is.EqualTo(RoomDimension.Secret));
            int secretInstanceId = secretRoom.GetInstanceID();

            Assert.That(secrets.TryUsePortal(anchor.RevealedPortal), Is.True);
            while (secrets.IsTransitioning)
            {
                yield return null;
            }
            Assert.That(lab.TransitionController.CurrentRoom, Is.SameAs(secretRoom));
            Assert.That(Vector2.Distance(
                player.Body.position,
                anchor.RevealedPortal.DestinationPortal.EntryAnchor.position), Is.LessThan(0.02f));

            RoomPersistentTransform2D persistent = secretRoom.GetComponentInChildren<RoomPersistentTransform2D>(true);
            Rigidbody2D crate = persistent.GetComponent<Rigidbody2D>();
            crate.bodyType = RigidbodyType2D.Kinematic;
            crate.gravityScale = 0f;
            crate.linearVelocity = Vector2.zero;
            Vector2 savedPosition = secretRoom.GetPrimarySafePosition() + Vector2.right * 2f;
            crate.position = savedPosition;
            crate.transform.position = savedPosition;
            RoomPortal2D returnPortal = anchor.RevealedPortal.DestinationPortal;
            Assert.That(secrets.TryUsePortal(returnPortal), Is.True);
            while (secrets.IsTransitioning)
            {
                yield return null;
            }
            Assert.That(lab.TransitionController.CurrentRoom, Is.SameAs(lab.RoomA));
            Assert.That(Vector2.Distance(player.Body.position, anchor.ReturnSafeCell.position), Is.LessThan(0.02f));
            SecretReturnMaruBiteImmunity immunity = player.GetComponent<SecretReturnMaruBiteImmunity>();
            Assert.That(immunity, Is.Not.Null);
            Assert.That(immunity.IsActive, Is.True);
            Assert.That(immunity.RemainingSeconds,
                Is.LessThanOrEqualTo(SecretDimensionRuntimeContract.ReturnMaruBiteImmunitySeconds));
            Assert.That(anchor.SourceRecoveryRack, Is.Not.Null);

            crate.position = new Vector2(999f, 999f);
            Assert.That(secrets.TryUsePortal(anchor.RevealedPortal), Is.True);
            while (secrets.IsTransitioning)
            {
                yield return null;
            }
            Assert.That(secretRoom.GetInstanceID(), Is.EqualTo(secretInstanceId));
            Assert.That(crate.position.x, Is.EqualTo(savedPosition.x).Within(0.001f));
            Assert.That(crate.position.y, Is.EqualTo(savedPosition.y).Within(0.001f));

            Object.Destroy(compassDefinition);
            Object.Destroy(lab.gameObject);
            Object.Destroy(player.gameObject);
            Object.Destroy(camera.gameObject);
        }

        [UnityTest]
        public IEnumerator CriticalCarryReturnsFromVoidWithoutDisappearing()
        {
            CarryObjectDefinition definition = ScriptableObject.CreateInstance<CarryObjectDefinition>();
            definition.ConfigureForTests(
                "CARRY_CRITICAL_TEST",
                CarryWeightClass.Light,
                Vector2Int.one,
                isCritical: true);
            GameObject anchorObject = new GameObject("CriticalCarryTestAnchor");
            anchorObject.transform.position = new Vector2(3f, 2f);
            GameObject carryObject = new GameObject("CriticalCarryTestObject");
            Rigidbody2D body = carryObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            carryObject.AddComponent<BoxCollider2D>();
            CarryableObject carryable = carryObject.AddComponent<CarryableObject>();
            carryable.ConfigureForTests(definition, body);
            CarryObjectOutOfBoundsGuard guard = carryObject.AddComponent<CarryObjectOutOfBoundsGuard>();
            guard.SetLastCriticalObjectAnchor(anchorObject.transform);

            guard.NotifyEnteredVoid();
            Assert.That(carryObject, Is.Not.Null);
            Assert.That(carryObject.activeSelf, Is.True);
            Assert.That(carryable.RuntimeState, Is.EqualTo(CarryRuntimeState.Recovering));
            Assert.That(body.simulated, Is.False);

            yield return new WaitForSeconds(CarryObjectOutOfBoundsGuard.VoidRecoverySeconds + 0.05f);

            Assert.That(carryObject, Is.Not.Null);
            Assert.That(carryObject.activeSelf, Is.True);
            Assert.That(carryable.RuntimeState, Is.EqualTo(CarryRuntimeState.World));
            Assert.That(body.simulated, Is.True);
            Assert.That(Vector2.Distance(carryObject.transform.position, anchorObject.transform.position),
                Is.LessThan(0.02f));

            Object.Destroy(definition);
            Object.Destroy(carryObject);
            Object.Destroy(anchorObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RunShellAndPrologueBuildTwoIndependentRoomsAroundOnePlayer()
        {
            AudioListener[] existingListeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            foreach (AudioListener listener in existingListeners)
            {
                listener.enabled = false;
            }

            yield return SceneManager.LoadSceneAsync("02_RunShell", LoadSceneMode.Additive);
            yield return SceneManager.LoadSceneAsync("10_Prologue_0_1", LoadSceneMode.Additive);
            yield return waitForFixedUpdate;
            yield return null;

            Core04TwoRoomLab lab = Object.FindAnyObjectByType<Core04TwoRoomLab>();
            RoomRuntime[] rooms = Object.FindObjectsByType<RoomRuntime>(FindObjectsSortMode.None);
            PlayerMotor2D[] players = Object.FindObjectsByType<PlayerMotor2D>(FindObjectsSortMode.None);

            Assert.That(lab, Is.Not.Null);
            Assert.That(rooms, Has.Length.EqualTo(2));
            Assert.That(players, Has.Length.EqualTo(1));
            Assert.That(lab.TransitionController, Is.Not.Null);
            Assert.That(lab.RoomA.transform.Find("GridLogic"), Is.Not.Null);
            Assert.That(lab.RoomB.transform.Find("GridLogic"), Is.Not.Null);
            RoomVisualBuilder[] visualBuilders = Object.FindObjectsByType<RoomVisualBuilder>(FindObjectsSortMode.None);
            float artReadyTimeout = Time.realtimeSinceStartup + 2f;
            while ((visualBuilders.Length != 2 || visualBuilders.Any(builder => !IsArtReady(builder))) &&
                   Time.realtimeSinceStartup < artReadyTimeout)
            {
                yield return null;
                visualBuilders = Object.FindObjectsByType<RoomVisualBuilder>(FindObjectsSortMode.None);
            }
            Assert.That(visualBuilders, Has.Length.EqualTo(2));
            for (int index = 0; index < visualBuilders.Length; index++)
            {
                RoomVisualBuilder builder = visualBuilders[index];
                Assert.That(builder.Profile, Is.Not.Null, builder.name + " did not receive the stage art profile.");
                Assert.That(builder.GeneratedRenderers.Count, Is.GreaterThanOrEqualTo(8), builder.name + " visual build is incomplete.");
                Assert.That(builder.GeneratedRenderers.All(renderer => renderer != null), Is.True, builder.name + " retained a destroyed generated renderer.");
                Assert.That(builder.HasGeneratedVisualInsideClearZone(), Is.False, builder.name + " obscures a gameplay clear zone.");
            }
            Assert.That(lab.RoomA.GridLogic.GetComponentsInChildren<SpriteRenderer>(true), Is.Empty);
            Assert.That(lab.RoomB.GridLogic.GetComponentsInChildren<SpriteRenderer>(true), Is.Empty);

            yield return SceneManager.UnloadSceneAsync("10_Prologue_0_1");
            yield return SceneManager.UnloadSceneAsync("02_RunShell");

            foreach (AudioListener listener in existingListeners)
            {
                if (listener != null)
                {
                    listener.enabled = true;
                }
            }
        }

        private static Core04TwoRoomLab CreateLab(out PlayerMotor2D player, out Camera camera)
        {
            GameObject cameraObject = new GameObject("Core04TestCamera");
            camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4f;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            GameObject playerObject = new GameObject("Core04TestPlayer");
            playerObject.layer = LayerMask.NameToLayer("Player");
            Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            playerObject.AddComponent<CapsuleCollider2D>();
            playerObject.AddComponent<PlayerActionLock>();
            player = playerObject.AddComponent<PlayerMotor2D>();
            player.ConfigureForTests((1 << 6) | (1 << GroundLayer));
            playerObject.AddComponent<PlayerOutOfBoundsGuard>();
            HandSlotPresenter presenter = playerObject.AddComponent<HandSlotPresenter>();
            presenter.ConfigureForTests(playerObject.transform);
            PlayerHandSlot handSlot = playerObject.AddComponent<PlayerHandSlot>();
            handSlot.ConfigureForTests(presenter);
            EquipmentInventory inventory = playerObject.AddComponent<EquipmentInventory>();
            inventory.ConfigureForTests(handSlot);

            GameObject labObject = new GameObject("Core04TestLab");
            Core04TwoRoomLab lab = labObject.AddComponent<Core04TwoRoomLab>();
            lab.BuildIfNeeded();
            lab.InitializePlayerAndCamera(player, camera);
            return lab;
        }

        private static bool IsVisible(Camera camera, Vector2 worldPosition)
        {
            Vector3 viewport = camera.WorldToViewportPoint(worldPosition);
            return viewport.z > 0f &&
                   viewport.x >= -0.001f && viewport.x <= 1.001f &&
                   viewport.y >= -0.001f && viewport.y <= 1.001f;
        }

        private static bool IsArtReady(RoomVisualBuilder builder)
        {
            return builder != null &&
                   builder.Profile != null &&
                   builder.GeneratedRenderers.Count >= 8 &&
                   builder.GeneratedRenderers.All(renderer => renderer != null);
        }

        private static void DestroyNamed(string objectName)
        {
            GameObject[] objects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            for (int index = 0; index < objects.Length; index++)
            {
                if (objects[index] != null && objects[index].name == objectName)
                {
                    Object.Destroy(objects[index]);
                }
            }
        }
    }
}

#endif
