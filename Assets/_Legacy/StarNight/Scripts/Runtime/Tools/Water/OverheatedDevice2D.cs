#if LEGACY_DISABLED
using System;
using StarNight.Grid;
using UnityEngine;

namespace StarNight.Tools.Water
{
    [DisallowMultipleComponent]
    public sealed class OverheatedDevice2D : WaterReactiveCell2D
    {
        [SerializeField] private Collider2D heatHazardCollider;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Color hotColor =
            new Color(1f, 0.30f, 0.12f, 1f);
        [SerializeField] private Color cooledColor =
            new Color(0.30f, 0.88f, 1f, 1f);
        [SerializeField] private bool startsCooled;

        public event Action Cooled;

        public bool IsCooled { get; private set; }
        public override bool CanReceiveWater => !IsCooled;

        protected override void Awake()
        {
            base.Awake();
            if (heatHazardCollider == null)
            {
                heatHazardCollider = GetComponent<Collider2D>();
            }

            if (visual == null)
            {
                visual = GetComponentInChildren<SpriteRenderer>();
            }

            IsCooled = startsCooled;
            ApplyState();
        }

        public void Configure(
            WaterInteractionRegistry2D registry,
            GridWorld world,
            GridPos cell,
            Collider2D targetHeatCollider,
            SpriteRenderer targetVisual,
            bool isInitiallyCooled = false)
        {
            ConfigureCell(registry, world, cell);
            heatHazardCollider = targetHeatCollider;
            visual = targetVisual;
            startsCooled = isInitiallyCooled;
            IsCooled = startsCooled;
            ApplyState();
        }

        public override WaterReactionKind TryReceiveWater(
            WaterApplication application)
        {
            if (IsCooled || application.Cell != WaterCell)
            {
                return WaterReactionKind.None;
            }

            IsCooled = true;
            ApplyState();
            Cooled?.Invoke();
            return WaterReactionKind.DeviceCooled;
        }

        public void ReheatForTests()
        {
            IsCooled = false;
            ApplyState();
        }

        private void ApplyState()
        {
            if (heatHazardCollider != null)
            {
                heatHazardCollider.enabled = !IsCooled;
            }

            if (visual != null)
            {
                visual.color = IsCooled ? cooledColor : hotColor;
            }
        }
    }
}

#endif
