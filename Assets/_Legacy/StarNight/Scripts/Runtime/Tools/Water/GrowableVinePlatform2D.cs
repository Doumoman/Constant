#if LEGACY_DISABLED
using System;
using StarNight.Grid;
using UnityEngine;

namespace StarNight.Tools.Water
{
    [DisallowMultipleComponent]
    public sealed class GrowableVinePlatform2D : WaterReactiveCell2D
    {
        [SerializeField] private Collider2D platformCollider;
        [SerializeField] private SpriteRenderer dryVisual;
        [SerializeField] private SpriteRenderer grownVisual;
        [SerializeField] private bool startsGrown;

        public event Action Grown;

        public bool IsGrown { get; private set; }
        public override bool CanReceiveWater => !IsGrown;

        protected override void Awake()
        {
            base.Awake();
            if (platformCollider == null)
            {
                platformCollider = GetComponent<Collider2D>();
            }

            IsGrown = startsGrown;
            ApplyState();
        }

        public void Configure(
            WaterInteractionRegistry2D registry,
            GridWorld world,
            GridPos cell,
            Collider2D targetPlatformCollider,
            SpriteRenderer targetDryVisual = null,
            SpriteRenderer targetGrownVisual = null,
            bool isInitiallyGrown = false)
        {
            ConfigureCell(registry, world, cell);
            platformCollider = targetPlatformCollider;
            dryVisual = targetDryVisual;
            grownVisual = targetGrownVisual;
            startsGrown = isInitiallyGrown;
            IsGrown = startsGrown;
            ApplyState();
        }

        public override WaterReactionKind TryReceiveWater(
            WaterApplication application)
        {
            if (IsGrown || application.Cell != WaterCell)
            {
                return WaterReactionKind.None;
            }

            IsGrown = true;
            ApplyState();
            Grown?.Invoke();
            return WaterReactionKind.PlantGrown;
        }

        public void ResetDryForTests()
        {
            IsGrown = false;
            ApplyState();
        }

        private void ApplyState()
        {
            if (platformCollider != null)
            {
                platformCollider.enabled = IsGrown;
            }

            if (dryVisual != null)
            {
                dryVisual.enabled = !IsGrown;
            }

            if (grownVisual != null)
            {
                grownVisual.enabled = IsGrown;
            }
        }
    }
}

#endif
