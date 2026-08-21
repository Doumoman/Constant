#if LEGACY_DISABLED
using System;
using StarNight.Grid;
using UnityEngine;

namespace StarNight.Tools.Water
{
    [DisallowMultipleComponent]
    public sealed class ExtinguishableFire2D : WaterReactiveCell2D
    {
        [SerializeField] private Collider2D hazardCollider;
        [SerializeField] private SpriteRenderer[] fireVisuals;
        [SerializeField] private bool startsLit = true;

        public event Action Extinguished;

        public bool IsLit { get; private set; }
        public override bool CanReceiveWater => IsLit;

        protected override void Awake()
        {
            base.Awake();
            if (hazardCollider == null)
            {
                hazardCollider = GetComponent<Collider2D>();
            }

            IsLit = startsLit;
            ApplyState();
        }

        public void Configure(
            WaterInteractionRegistry2D registry,
            GridWorld world,
            GridPos cell,
            Collider2D targetHazardCollider,
            SpriteRenderer[] targetVisuals,
            bool isInitiallyLit = true)
        {
            ConfigureCell(registry, world, cell);
            hazardCollider = targetHazardCollider;
            fireVisuals = targetVisuals;
            startsLit = isInitiallyLit;
            IsLit = startsLit;
            ApplyState();
        }

        public override WaterReactionKind TryReceiveWater(
            WaterApplication application)
        {
            if (!IsLit || application.Cell != WaterCell)
            {
                return WaterReactionKind.None;
            }

            IsLit = false;
            ApplyState();
            Extinguished?.Invoke();
            return WaterReactionKind.FireExtinguished;
        }

        public void RelightForTests()
        {
            IsLit = true;
            ApplyState();
        }

        private void ApplyState()
        {
            if (hazardCollider != null)
            {
                hazardCollider.enabled = IsLit;
            }

            if (fireVisuals == null)
            {
                return;
            }

            for (int index = 0; index < fireVisuals.Length; index++)
            {
                if (fireVisuals[index] != null)
                {
                    fireVisuals[index].enabled = IsLit;
                }
            }
        }
    }
}

#endif
