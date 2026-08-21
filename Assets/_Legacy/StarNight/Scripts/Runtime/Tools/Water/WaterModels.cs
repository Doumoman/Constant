#if LEGACY_DISABLED
using System.Collections.Generic;
using StarNight.Grid;

namespace StarNight.Tools.Water
{
    public enum WaterReactionKind
    {
        None,
        PlantGrown,
        FireExtinguished,
        DeviceCooled
    }

    public struct WaterApplication
    {
        public WaterApplication(
            GridPos origin,
            GridPos direction,
            GridPos cell,
            int streamIndex,
            UnityEngine.Object source)
        {
            Origin = origin;
            Direction = direction;
            Cell = cell;
            StreamIndex = streamIndex;
            Source = source;
        }

        public GridPos Origin { get; }
        public GridPos Direction { get; }
        public GridPos Cell { get; }
        public int StreamIndex { get; }
        public UnityEngine.Object Source { get; }
    }

    public interface IWaterReactive2D
    {
        GridPos WaterCell { get; }
        int WaterPriority { get; }
        bool CanReceiveWater { get; }
        UnityEngine.Object WaterTargetObject { get; }

        WaterReactionKind TryReceiveWater(WaterApplication application);
    }

    public struct WaterReactionRecord
    {
        public WaterReactionRecord(
            GridPos cell,
            IWaterReactive2D target,
            WaterReactionKind reaction)
        {
            Cell = cell;
            Target = target;
            Reaction = reaction;
        }

        public GridPos Cell { get; }
        public IWaterReactive2D Target { get; }
        public WaterReactionKind Reaction { get; }
    }

    public sealed class WaterUseReport
    {
        private static readonly WaterUseReport empty = new WaterUseReport(
            new GridPos[0],
            new WaterReactionRecord[0]);

        public WaterUseReport(
            IReadOnlyList<GridPos> wateredCells,
            IReadOnlyList<WaterReactionRecord> reactions)
        {
            WateredCells = wateredCells ?? new GridPos[0];
            Reactions = reactions ?? new WaterReactionRecord[0];
        }

        public static WaterUseReport Empty => empty;

        public IReadOnlyList<GridPos> WateredCells { get; }
        public IReadOnlyList<WaterReactionRecord> Reactions { get; }
        public int WateredCellCount => WateredCells.Count;
        public int ReactionCount => Reactions.Count;
    }
}

#endif
