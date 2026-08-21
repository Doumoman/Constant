#if LEGACY_DISABLED
using System;
using StarNight.Grid;
using UnityEngine;

namespace StarNight.Tools.Pestle
{
    [DisallowMultipleComponent]
    public sealed class PestleSwitch2D : PestleTargetCell2D
    {
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Color idleColor =
            new Color(0.80f, 0.48f, 0.22f, 1f);
        [SerializeField] private Color activatedColor =
            new Color(1f, 0.90f, 0.32f, 1f);

        public event Action Activated;

        public bool IsActivated { get; private set; }
        public override bool CanReceivePestle => !IsActivated;

        protected override void Awake()
        {
            base.Awake();
            if (visual == null)
            {
                visual = GetComponentInChildren<SpriteRenderer>();
            }

            ApplyState();
        }

        public void Configure(
            PestleInteractionRegistry2D registry,
            GridWorld world,
            GridPos cell,
            SpriteRenderer targetVisual = null)
        {
            ConfigureCell(registry, world, cell);
            visual = targetVisual;
            IsActivated = false;
            ApplyState();
        }

        public override PestleReactionKind TryReceivePestle(
            PestleStrikeContext context)
        {
            if (IsActivated || context.StrikeCell != PestleCell)
            {
                return PestleReactionKind.None;
            }

            IsActivated = true;
            ApplyState();
            Activated?.Invoke();
            return PestleReactionKind.SwitchActivated;
        }

        public void ResetForTests()
        {
            IsActivated = false;
            ApplyState();
        }

        private void ApplyState()
        {
            if (visual != null)
            {
                visual.color = IsActivated ? activatedColor : idleColor;
            }
        }
    }
}

#endif
