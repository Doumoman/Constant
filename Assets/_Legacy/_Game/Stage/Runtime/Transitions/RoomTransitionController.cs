#if LEGACY_DISABLED
using System;
using System.Collections;
using StarNight.Interaction.Carry;
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.Input;
using StarNight.Player.Motor;
using StarNight.Player.Safety;
using StarNight.Stage.Rooms;
using StarNight.Stage.Streaming;
using UnityEngine;

namespace StarNight.Stage.Transitions
{
    [DisallowMultipleComponent]
    public sealed class RoomTransitionController : MonoBehaviour
    {
        public const float HorizontalInputLockSeconds = 0.12f;

        [SerializeField] private RoomCameraController cameraController;
        [SerializeField] private PlayerMotor2D playerMotor;
        [SerializeField] private PlayerActionLock actionLock;
        [SerializeField] private PlayerOutOfBoundsGuard outOfBoundsGuard;
        [SerializeField] private PlayerHandSlot handSlot;
        [SerializeField] private RoomStreamingManager streamingManager;

        private RoomPortal2D previewedPortal;
        private bool externallyBlocked;
        private HandSlotItemRuntime suspendedPortalItem;
        private Transform nextPortalCarryRecoveryAnchor;

        public event Action<RoomChangedEvent> RoomChanged;

        public RoomRuntime CurrentRoom { get; private set; }
        public bool IsTransitioning { get; private set; }
        public RoomPortal2D PreviewedPortal => previewedPortal;
        public RoomCameraController CameraController => cameraController;
        public PlayerMotor2D PlayerMotor => playerMotor;
        public bool HasSuspendedPortalCarry => suspendedPortalItem != null;
        public RoomStreamingManager StreamingManager => streamingManager;

        public void Configure(
            RoomCameraController roomCameraController,
            PlayerMotor2D motor,
            PlayerActionLock playerActionLock,
            PlayerOutOfBoundsGuard guard,
            PlayerHandSlot configuredHandSlot = null,
            RoomStreamingManager configuredStreamingManager = null)
        {
            cameraController = roomCameraController;
            playerMotor = motor;
            actionLock = playerActionLock;
            outOfBoundsGuard = guard;
            handSlot = configuredHandSlot != null
                ? configuredHandSlot
                : playerMotor != null ? playerMotor.GetComponent<PlayerHandSlot>() : null;
            streamingManager = configuredStreamingManager != null
                ? configuredStreamingManager
                : GetComponent<RoomStreamingManager>();
            cameraController?.SetFollowTarget(playerMotor != null ? playerMotor.transform : null);
        }

        public void Begin(RoomRuntime startRoom)
        {
            CurrentRoom = startRoom;
            if (CurrentRoom == null)
            {
                return;
            }

            if (streamingManager == null || !streamingManager.Begin(CurrentRoom.RoomId))
            {
                CurrentRoom.SetSimulationState(RoomSimulationState.Active);
            }
            if (playerMotor != null && CurrentRoom.SpawnPoint != null)
            {
                playerMotor.SnapTo(CurrentRoom.SpawnPoint.position);
            }

            cameraController?.SnapToRoom(
                CurrentRoom,
                playerMotor != null ? playerMotor.Body.position : CurrentRoom.GetPrimarySafePosition());

            ConfigureSafety(CurrentRoom, playerMotor != null ? playerMotor.Body.position : CurrentRoom.GetPrimarySafePosition());
        }

        public void SetExternalBlock(bool blocked)
        {
            externallyBlocked = blocked;
        }

        public bool TryPreview(RoomPortal2D portal)
        {
            if (!CanUse(portal, false))
            {
                return false;
            }

            streamingManager?.RequestWarmLoad(portal.Destination.RoomId);

            if (previewedPortal != null && previewedPortal != portal)
            {
                RoomRuntime oldDestination = previewedPortal.Destination;
                if (oldDestination != null && oldDestination != CurrentRoom)
                {
                    if (streamingManager == null)
                    {
                        oldDestination.SetSimulationState(RoomSimulationState.Dormant);
                    }
                }
            }

            previewedPortal = portal;
            if (portal.IsReady)
            {
                portal.Destination.SetSimulationState(RoomSimulationState.NeighborPreview);
            }
            return true;
        }

        public bool TryCommit(RoomPortal2D portal)
        {
            if (IsTransitioning || !CanUse(portal))
            {
                return false;
            }

            if (previewedPortal != portal && !TryPreview(portal))
            {
                return false;
            }

            if (!PreparePortalCarry(portal))
            {
                return false;
            }

            StartCoroutine(TransitionRoutine(portal));
            return true;
        }

        public bool CommitImmediate(RoomPortal2D portal, Transform criticalCarryRecoveryAnchor = null)
        {
            nextPortalCarryRecoveryAnchor = criticalCarryRecoveryAnchor;
            if (IsTransitioning || !CanUse(portal))
            {
                nextPortalCarryRecoveryAnchor = null;
                return false;
            }

            if (!PreparePortalCarry(portal))
            {
                nextPortalCarryRecoveryAnchor = null;
                return false;
            }

            IsTransitioning = true;
            LockPlayer();
            portal.Destination.SetSimulationState(RoomSimulationState.TransitionTarget);
            Vector2 destinationFocus = portal.DestinationPortal != null && portal.DestinationPortal.EntryAnchor != null
                ? portal.DestinationPortal.EntryAnchor.position
                : portal.Destination.GetPrimarySafePosition();
            cameraController?.SnapToRoom(portal.Destination, destinationFocus);
            CompleteTransition(portal);
            return true;
        }

        private IEnumerator TransitionRoutine(RoomPortal2D portal)
        {
            IsTransitioning = true;
            LockPlayer();
            portal.Destination.SetSimulationState(RoomSimulationState.TransitionTarget);

            if (cameraController != null)
            {
                Vector2 destinationFocus = portal.DestinationPortal != null && portal.DestinationPortal.EntryAnchor != null
                    ? portal.DestinationPortal.EntryAnchor.position
                    : portal.Destination.GetPrimarySafePosition();
                yield return cameraController.MoveToRoom(portal.Destination, destinationFocus);
            }

            CompleteTransition(portal);
        }

        private void CompleteTransition(RoomPortal2D portal)
        {
            RoomRuntime previous = CurrentRoom;
            RoomRuntime destination = portal.Destination;
            RoomPortal2D destinationPortal = portal.DestinationPortal;

            if (playerMotor != null && destinationPortal != null && destinationPortal.EntryAnchor != null)
            {
                playerMotor.SnapTo(destinationPortal.EntryAnchor.position);
            }

            bool activatedByStreaming = streamingManager != null && streamingManager.Activate(destination.RoomId);
            if (!activatedByStreaming)
            {
                destination.SetSimulationState(RoomSimulationState.Active);
            }
            RestorePortalCarry(destinationPortal, destination);
            if (streamingManager == null)
            {
                previous.SetSimulationState(RoomSimulationState.ResidualSimulation);
            }
            CurrentRoom = destination;
            previewedPortal = null;
            ConfigureSafety(destination, playerMotor != null ? playerMotor.Body.position : destination.GetPrimarySafePosition());

            actionLock?.ResetToFree();
            IsTransitioning = false;
            RoomChanged?.Invoke(new RoomChangedEvent(previous.RoomId, destination.RoomId));
        }

        private bool CanUse(RoomPortal2D portal, bool requireReady = true)
        {
            if (externallyBlocked
                || portal == null
                || portal.Owner != CurrentRoom
                || !portal.HasDestination
                || requireReady && !portal.IsReady)
            {
                return false;
            }

            if (actionLock == null)
            {
                return true;
            }

            PlayerActionState state = actionLock.State;
            return state != PlayerActionState.DialogueLocked &&
                   state != PlayerActionState.RoomTransitionLocked &&
                   state != PlayerActionState.HookLatched &&
                   state != PlayerActionState.HookPulling &&
                   state != PlayerActionState.UsingTool &&
                   state != PlayerActionState.PickingUp &&
                   state != PlayerActionState.Placing &&
                   state != PlayerActionState.MaruBitten;
        }

        private void LockPlayer()
        {
            actionLock?.SetState(PlayerActionState.RoomTransitionLocked);
            playerMotor?.ClearBufferedInput();
            if (playerMotor != null)
            {
                playerMotor.SetMoveInput(0f);
            }
        }

        private void ConfigureSafety(RoomRuntime room, Vector2 safePosition)
        {
            outOfBoundsGuard?.Configure(room.WorldBounds, safePosition, false);
        }

        private bool PreparePortalCarry(RoomPortal2D portal)
        {
            suspendedPortalItem = null;
            if (handSlot == null || handSlot.IsEmpty)
            {
                return true;
            }

            HandSlotItemRuntime item = handSlot.CurrentItem;
            if (!handSlot.TrySuspendForPortal(portal.DestinationPortal, out _))
            {
                return false;
            }
            suspendedPortalItem = item;
            return true;
        }

        private void RestorePortalCarry(RoomPortal2D destinationPortal, RoomRuntime destination)
        {
            HandSlotItemRuntime item = suspendedPortalItem;
            suspendedPortalItem = null;
            Transform criticalRecoveryAnchor = nextPortalCarryRecoveryAnchor;
            nextPortalCarryRecoveryAnchor = null;
            if (item == null || handSlot == null || handSlot.CurrentItem != item)
            {
                return;
            }
            if (handSlot.RestoreAfterPortal())
            {
                return;
            }

            Vector2 entryPosition = destinationPortal != null && destinationPortal.EntryAnchor != null
                ? destinationPortal.EntryAnchor.position
                : destination.GetPrimarySafePosition();
            if (item is CarryableObject carryable
                && carryable.Definition != null
                && carryable.Definition.CriticalCarry)
            {
                handSlot.TryReleaseCurrent(item);
                CarryObjectOutOfBoundsGuard guard = carryable.GetComponent<CarryObjectOutOfBoundsGuard>();
                guard?.SetNextRoomEntryAnchor(criticalRecoveryAnchor != null
                    ? criticalRecoveryAnchor
                    : destinationPortal != null ? destinationPortal.EntryAnchor : destination.SpawnPoint);
                guard?.NotifyLostDuringRoomTransition();
                return;
            }

            handSlot.TryDropCurrent(entryPosition);
        }
    }
}

#endif
