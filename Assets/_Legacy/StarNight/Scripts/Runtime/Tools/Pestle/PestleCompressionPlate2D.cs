#if LEGACY_DISABLED
using System;
using StarNight.Grid;
using UnityEngine;

namespace StarNight.Tools.Pestle
{
    [DisallowMultipleComponent]
    public sealed class PestleCompressionPlate2D : PestleTargetCell2D
    {
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Color releasedColor =
            new Color(0.55f, 0.44f, 0.26f, 1f);
        [SerializeField] private Color pressedColor =
            new Color(0.40f, 1f, 0.62f, 1f);

        public event Action Pressed;
        public event Action Released;

        public bool IsPressed { get; private set; }
        public override bool CanReceivePestle => !IsPressed;

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
            IsPressed = false;
            ApplyState();
        }

        public override PestleReactionKind TryReceivePestle(
            PestleStrikeContext context)
        {
            if (IsPressed || context.StrikeCell != PestleCell)
            {
                return PestleReactionKind.None;
            }

            IsPressed = true;
            ApplyState();
            Pressed?.Invoke();
            return PestleReactionKind.CompressionPlatePressed;
        }

        public bool Release()
        {
            if (!IsPressed)
            {
                return false;
            }

            IsPressed = false;
            ApplyState();
            Released?.Invoke();
            return true;
        }

        private void ApplyState()
        {
            if (visual != null)
            {
                visual.color = IsPressed ? pressedColor : releasedColor;
            }
        }
    }
}

#endif
