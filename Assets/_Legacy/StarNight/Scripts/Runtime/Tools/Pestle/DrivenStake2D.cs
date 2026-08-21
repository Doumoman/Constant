#if LEGACY_DISABLED
using System;
using StarNight.Grid;
using UnityEngine;

namespace StarNight.Tools.Pestle
{
    [DisallowMultipleComponent]
    public sealed class DrivenStake2D : PestleTargetCell2D
    {
        [SerializeField] private Transform movingVisual;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField, Min(0f)] private float drivenOffset = 0.35f;
        [SerializeField] private Color raisedColor =
            new Color(0.95f, 0.80f, 0.38f, 1f);
        [SerializeField] private Color drivenColor =
            new Color(0.50f, 0.92f, 0.62f, 1f);

        private Vector3 raisedLocalPosition;

        public event Action Driven;

        public bool IsDriven { get; private set; }
        public override bool CanReceivePestle => !IsDriven;

        protected override void Awake()
        {
            base.Awake();
            if (visual == null)
            {
                visual = GetComponentInChildren<SpriteRenderer>();
            }

            if (movingVisual != null)
            {
                raisedLocalPosition = movingVisual.localPosition;
            }

            ApplyState();
        }

        public void Configure(
            PestleInteractionRegistry2D registry,
            GridWorld world,
            GridPos cell,
            Transform targetMovingVisual = null,
            SpriteRenderer targetVisual = null,
            float downwardOffset = 0.35f)
        {
            ConfigureCell(registry, world, cell);
            movingVisual = targetMovingVisual;
            visual = targetVisual;
            drivenOffset = Mathf.Max(0f, downwardOffset);
            if (movingVisual != null)
            {
                raisedLocalPosition = movingVisual.localPosition;
            }

            IsDriven = false;
            ApplyState();
        }

        public override PestleReactionKind TryReceivePestle(
            PestleStrikeContext context)
        {
            if (IsDriven || context.StrikeCell != PestleCell)
            {
                return PestleReactionKind.None;
            }

            IsDriven = true;
            ApplyState();
            Driven?.Invoke();
            return PestleReactionKind.StakeDriven;
        }

        public void ResetRaisedForTests()
        {
            IsDriven = false;
            ApplyState();
        }

        private void ApplyState()
        {
            if (movingVisual != null)
            {
                movingVisual.localPosition = raisedLocalPosition
                    + Vector3.down * (IsDriven ? drivenOffset : 0f);
            }

            if (visual != null)
            {
                visual.color = IsDriven ? drivenColor : raisedColor;
            }
        }
    }
}

#endif
