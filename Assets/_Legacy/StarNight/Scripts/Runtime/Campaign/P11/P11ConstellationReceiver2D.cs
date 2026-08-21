#if LEGACY_DISABLED
using System;
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    public sealed class P11ConstellationReceiver2D :
        P5ContextInteractable2D
    {
        [SerializeField] private P11ConstellationBridge2D bridge;
        [SerializeField, Min(0)] private int receiverIndex;
        [SerializeField] private SpriteRenderer receiverVisual;
        [SerializeField] private Color inactiveColor =
            new Color(0.28f, 0.38f, 0.58f, 0.72f);
        [SerializeField] private Color activeColor =
            new Color(0.68f, 0.94f, 1f, 1f);
        [SerializeField] private bool activated;
        [SerializeField] private int activationCount;

        public event Action<int> Activated;

        public P11ConstellationBridge2D Bridge => bridge;
        public int ReceiverIndex => receiverIndex;
        public bool IsActivated => activated;
        public int ActivationCount => activationCount;
        public bool IsConfigured =>
            bridge != null
            && receiverIndex >= 0
            && receiverIndex < bridge.SegmentCount;

        public void Configure(
            P11ConstellationBridge2D targetBridge,
            int index,
            SpriteRenderer visual = null,
            Transform interactionPoint = null,
            float interactionRadius = 1.5f,
            int interactionPriority = 70)
        {
            bridge = targetBridge;
            receiverIndex = Mathf.Max(0, index);
            receiverVisual = visual;
            activated = bridge != null
                && bridge.IsReceiverActive(receiverIndex);
            activationCount = 0;
            ConfigureInteraction(
                interactionPoint != null
                    ? interactionPoint
                    : transform,
                interactionRadius,
                interactionPriority);
            RefreshVisual();
        }

        public bool TryActivate()
        {
            if (!IsConfigured || activated)
            {
                return false;
            }

            if (!bridge.ActivateReceiver(receiverIndex))
            {
                activated = bridge.IsReceiverActive(receiverIndex);
                RefreshVisual();
                return false;
            }

            activated = true;
            activationCount++;
            RefreshVisual();
            Activated?.Invoke(receiverIndex);
            return true;
        }

        protected override bool CanInteract(
            P5PlayerInteractionContext context)
        {
            return isActiveAndEnabled && IsConfigured && !activated;
        }

        protected override bool TryInteract(
            P5PlayerInteractionContext context)
        {
            return TryActivate();
        }

        private void RefreshVisual()
        {
            if (receiverVisual != null)
            {
                receiverVisual.color = activated
                    ? activeColor
                    : inactiveColor;
            }
        }
    }
}

#endif
