#if LEGACY_DISABLED
using System;
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.Input;
using StarNight.Interaction.Targeting;
using UnityEngine;

namespace StarNight.Tools.Watering
{
    [DisallowMultipleComponent]
    public sealed class ToolRechargeReceiver : MonoBehaviour, IContextReceiver
    {
        public const float RechargeHoldSeconds = 0.50f;
        public const float MovementCancelDistance = 0.05f;

        private WateringCanRuntime activeTool;
        private PlayerHandSlot activeSlot;
        private PlayerActionLock activeLock;
        private GameplayInputReader activeInput;
        private Vector2 actorStartPosition;
        private long actionId;
        private float heldSeconds;

        public event Action<WateringCanRuntime> RechargeCompleted;
        public event Action RechargeCancelled;

        public int ContextPriority => 200;
        public bool IsRecharging => activeTool != null;
        public float HeldSeconds => heldSeconds;

        private void Update()
        {
            if (!IsRecharging)
            {
                return;
            }
            bool held = activeInput == null || activeInput.PrimaryHeld;
            Tick(Time.deltaTime, held, activeSlot != null ? (Vector2)activeSlot.transform.position : actorStartPosition);
        }

        private void OnDisable()
        {
            CancelRecharge();
        }

        public bool CanReceive(ContextReceiverQuery query)
        {
            return query.HandSlotItem is WateringCanRuntime watering
                && watering.ResourceState.Maximum > 0
                && watering.ResourceState.Current < watering.ResourceState.Maximum;
        }

        public ContextReceiverResult TryReceive(ContextReceiverRequest request)
        {
            if (IsRecharging
                || request.Actor == null
                || request.HandSlotItem is not WateringCanRuntime watering)
            {
                return ContextReceiverResult.Rejected("RechargeUnavailable");
            }

            PlayerHandSlot slot = request.Actor.GetComponent<PlayerHandSlot>();
            PlayerActionLock playerLock = request.Actor.GetComponent<PlayerActionLock>();
            if (slot == null
                || slot.CurrentItem != watering
                || watering.ResourceState.Current >= watering.ResourceState.Maximum
                || playerLock != null && !playerLock.TryAcquire(request.Action.ActionId, PlayerActionState.UsingTool))
            {
                return ContextReceiverResult.Rejected("RechargeUnavailable");
            }

            activeTool = watering;
            activeSlot = slot;
            activeLock = playerLock;
            activeInput = request.Actor.GetComponent<GameplayInputReader>();
            actorStartPosition = request.Actor.transform.position;
            actionId = request.Action.ActionId;
            heldSeconds = 0f;
            return new ContextReceiverResult(true, false, "RechargeStarted");
        }

        public void TickForTests(float deltaSeconds, bool primaryHeld, Vector2 actorPosition)
        {
            Tick(deltaSeconds, primaryHeld, actorPosition);
        }

        public bool CancelRecharge()
        {
            if (!IsRecharging)
            {
                return false;
            }
            PlayerActionLock playerLock = activeLock;
            long completedActionId = actionId;
            ClearActive();
            playerLock?.TryRelease(completedActionId, PlayerActionState.Carrying);
            RechargeCancelled?.Invoke();
            return true;
        }

        private void Tick(float deltaSeconds, bool primaryHeld, Vector2 actorPosition)
        {
            if (!IsRecharging || deltaSeconds <= 0f)
            {
                return;
            }
            bool authorityLost = activeLock != null && activeLock.ActiveActionId != actionId;
            bool moved = Vector2.Distance(actorStartPosition, actorPosition) > MovementCancelDistance;
            bool toolLost = activeSlot == null || activeSlot.CurrentItem != activeTool;
            if (!primaryHeld || authorityLost || moved || toolLost)
            {
                CancelRecharge();
                return;
            }

            heldSeconds += deltaSeconds;
            if (heldSeconds < RechargeHoldSeconds)
            {
                return;
            }

            WateringCanRuntime completedTool = activeTool;
            PlayerActionLock playerLock = activeLock;
            long completedActionId = actionId;
            completedTool.RepairFull();
            ClearActive();
            playerLock?.TryRelease(completedActionId, PlayerActionState.Carrying);
            RechargeCompleted?.Invoke(completedTool);
        }

        private void ClearActive()
        {
            activeTool = null;
            activeSlot = null;
            activeLock = null;
            activeInput = null;
            actionId = 0;
            heldSeconds = 0f;
        }
    }
}

#endif
