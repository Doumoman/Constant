#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Grid;
using UnityEngine;

namespace StarNight.Tools.Pestle
{
    [DisallowMultipleComponent]
    public sealed class PestleTool2D : MonoBehaviour
    {
        public const float DefaultRecoveryDuration = 0.8f;

        [SerializeField] private GridWorld gridWorld;
        [SerializeField] private PestleInteractionRegistry2D interactionRegistry;
        [SerializeField, Min(0.1f)] private float recoveryDuration =
            DefaultRecoveryDuration;

        private readonly List<PestleReactionRecord> reactions =
            new List<PestleReactionRecord>(4);
        private float readyAt;
        private bool recovering;

        public event Action<PestleStrikeReport> Struck;
        public event Action<float> RecoveryStarted;
        public event Action RecoveryCompleted;

        public bool UsesDurability => false;
        public float RecoveryDuration => recoveryDuration;
        public bool IsRecovering => recovering && Time.time < readyAt;
        public PestleStrikeReport LastReport { get; private set; } =
            PestleStrikeReport.Empty;

        private void Awake()
        {
            if (gridWorld == null)
            {
                gridWorld = FindFirstObjectByType<GridWorld>();
            }

            if (interactionRegistry == null)
            {
                interactionRegistry =
                    FindFirstObjectByType<PestleInteractionRegistry2D>();
            }

            recoveryDuration = Mathf.Max(0.1f, recoveryDuration);
        }

        private void Update()
        {
            if (recovering && Time.time >= readyAt)
            {
                recovering = false;
                RecoveryCompleted?.Invoke();
            }
        }

        public void Configure(
            GridWorld world,
            PestleInteractionRegistry2D registry,
            float deliberateRecoveryDuration = DefaultRecoveryDuration)
        {
            gridWorld = world;
            interactionRegistry = registry;
            recoveryDuration = Mathf.Max(0.1f, deliberateRecoveryDuration);
            readyAt = 0f;
            recovering = false;
        }

        public bool TryStrike(GridPos actorCell)
        {
            return TryStrike(actorCell, out _);
        }

        public bool TryStrike(
            GridPos actorCell,
            out PestleStrikeReport report)
        {
            return TryStrikeAt(actorCell, Time.time, out report);
        }

        public bool TryStrikeAt(
            GridPos actorCell,
            float timestamp,
            out PestleStrikeReport report)
        {
            report = PestleStrikeReport.Empty;
            GridPos strikeCell = new GridPos(actorCell.X, actorCell.Y - 1);
            if (gridWorld == null
                || !gridWorld.IsWithinBounds(strikeCell)
                || timestamp < readyAt)
            {
                return false;
            }

            reactions.Clear();
            PestleStrikeContext context = new PestleStrikeContext(
                actorCell,
                strikeCell,
                timestamp,
                this);
            interactionRegistry?.ApplyStrike(
                strikeCell,
                context,
                reactions);

            readyAt = timestamp + recoveryDuration;
            recovering = true;
            PestleReactionRecord[] stableReactions = reactions.ToArray();
            report = new PestleStrikeReport(strikeCell, stableReactions);
            LastReport = report;
            Struck?.Invoke(report);
            RecoveryStarted?.Invoke(recoveryDuration);
            return true;
        }

        public float GetRemainingRecoveryAt(float timestamp)
        {
            return Mathf.Max(0f, readyAt - timestamp);
        }

        public void ForceReadyForTests(float timestamp = 0f)
        {
            readyAt = timestamp;
            recovering = false;
        }
    }
}

#endif
