#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Grid;

namespace StarNight.Tools.Pestle
{
    [Flags]
    public enum PestleReactionKind
    {
        None = 0,
        StakeDriven = 1 << 0,
        SwitchActivated = 1 << 1,
        CompressionPlatePressed = 1 << 2,
        ElasticPlatformCreated = 1 << 3,
        ThinFloorBreakQueued = 1 << 4,
        EnemyStunned = 1 << 5
    }

    public struct PestleStrikeContext
    {
        public PestleStrikeContext(
            GridPos actorCell,
            GridPos strikeCell,
            float timestamp,
            UnityEngine.Object source)
        {
            ActorCell = actorCell;
            StrikeCell = strikeCell;
            Timestamp = timestamp;
            Source = source;
        }

        public GridPos ActorCell { get; }
        public GridPos StrikeCell { get; }
        public float Timestamp { get; }
        public UnityEngine.Object Source { get; }
    }

    public interface IPestleTarget2D
    {
        GridPos PestleCell { get; }
        int PestlePriority { get; }
        bool CanReceivePestle { get; }
        UnityEngine.Object PestleTargetObject { get; }

        PestleReactionKind TryReceivePestle(PestleStrikeContext context);
    }

    public struct PestleReactionRecord
    {
        public PestleReactionRecord(
            GridPos cell,
            IPestleTarget2D target,
            PestleReactionKind reaction)
        {
            Cell = cell;
            Target = target;
            Reaction = reaction;
        }

        public GridPos Cell { get; }
        public IPestleTarget2D Target { get; }
        public PestleReactionKind Reaction { get; }
    }

    public sealed class PestleStrikeReport
    {
        private static readonly PestleStrikeReport empty =
            new PestleStrikeReport(
                new GridPos(0, 0),
                new PestleReactionRecord[0]);

        public PestleStrikeReport(
            GridPos strikeCell,
            IReadOnlyList<PestleReactionRecord> reactions)
        {
            StrikeCell = strikeCell;
            Reactions = reactions ?? new PestleReactionRecord[0];
            PestleReactionKind combined = PestleReactionKind.None;
            for (int index = 0; index < Reactions.Count; index++)
            {
                combined |= Reactions[index].Reaction;
            }

            CombinedReaction = combined;
        }

        public static PestleStrikeReport Empty => empty;

        public GridPos StrikeCell { get; }
        public IReadOnlyList<PestleReactionRecord> Reactions { get; }
        public int ReactionCount => Reactions.Count;
        public PestleReactionKind CombinedReaction { get; }
    }
}

#endif
