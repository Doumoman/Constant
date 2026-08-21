#if LEGACY_DISABLED
using System;
using StarNight.Grid;
using StarNight.Tiles;
using UnityEngine;

namespace StarNight.Tools.Pestle
{
    [DisallowMultipleComponent]
    public sealed class ThinFloorPestleTarget2D : PestleTargetCell2D
    {
        [SerializeField] private TileMutationService tileMutationService;
        [SerializeField] private Collider2D floorCollider;
        [SerializeField] private SpriteRenderer[] floorVisuals;

        private bool subscribed;

        public event Action<long> BreakQueued;
        public event Action Broken;
        public event Action<TileMutationRejection> BreakRejected;

        public bool IsBreakQueued { get; private set; }
        public bool IsBroken { get; private set; }
        public long LastMutationSequence { get; private set; }
        public override bool CanReceivePestle =>
            !IsBreakQueued && !IsBroken && tileMutationService != null;

        protected override void Awake()
        {
            base.Awake();
            if (tileMutationService == null)
            {
                tileMutationService =
                    FindFirstObjectByType<TileMutationService>();
            }

            if (floorCollider == null)
            {
                floorCollider = GetComponent<Collider2D>();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Subscribe();
        }

        protected override void OnDisable()
        {
            Unsubscribe();
            base.OnDisable();
        }

        public void Configure(
            PestleInteractionRegistry2D registry,
            GridWorld world,
            GridPos cell,
            TileMutationService mutationService,
            Collider2D targetFloorCollider = null,
            SpriteRenderer[] targetVisuals = null)
        {
            Unsubscribe();
            ConfigureCell(registry, world, cell);
            tileMutationService = mutationService;
            floorCollider = targetFloorCollider;
            floorVisuals = targetVisuals;
            IsBreakQueued = false;
            IsBroken = false;
            LastMutationSequence = 0;
            ApplyState();
            Subscribe();
        }

        public override PestleReactionKind TryReceivePestle(
            PestleStrikeContext context)
        {
            if (!CanReceivePestle || context.StrikeCell != PestleCell)
            {
                return PestleReactionKind.None;
            }

            LastMutationSequence = tileMutationService.EnqueueDestroy(
                PestleCell,
                TileBreakMethod.System,
                this);
            IsBreakQueued = true;
            BreakQueued?.Invoke(LastMutationSequence);
            return PestleReactionKind.ThinFloorBreakQueued;
        }

        public void ResetForTests()
        {
            IsBreakQueued = false;
            IsBroken = false;
            LastMutationSequence = 0;
            ApplyState();
        }

        private void HandleBatchCommitted(TileMutationBatchReport report)
        {
            if (!IsBreakQueued || report == null)
            {
                return;
            }

            for (int index = 0; index < report.Records.Count; index++)
            {
                TileMutationRecord record = report.Records[index];
                if (record.Request.Sequence != LastMutationSequence)
                {
                    continue;
                }

                IsBreakQueued = false;
                if (record.Committed)
                {
                    IsBroken = true;
                    ApplyState();
                    Broken?.Invoke();
                }
                else
                {
                    BreakRejected?.Invoke(record.Rejection);
                }

                return;
            }
        }

        private void ApplyState()
        {
            if (floorCollider != null)
            {
                floorCollider.enabled = !IsBroken;
            }

            if (floorVisuals == null)
            {
                return;
            }

            for (int index = 0; index < floorVisuals.Length; index++)
            {
                if (floorVisuals[index] != null)
                {
                    floorVisuals[index].enabled = !IsBroken;
                }
            }
        }

        private void Subscribe()
        {
            if (subscribed || tileMutationService == null)
            {
                return;
            }

            tileMutationService.BatchCommitted += HandleBatchCommitted;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || tileMutationService == null)
            {
                subscribed = false;
                return;
            }

            tileMutationService.BatchCommitted -= HandleBatchCommitted;
            subscribed = false;
        }
    }
}

#endif
