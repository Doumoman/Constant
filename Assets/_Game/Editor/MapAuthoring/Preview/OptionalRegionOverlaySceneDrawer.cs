using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Diagnostics;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Preview
{
    public enum OptionalRegionOverlayDrawCommandKind
    {
        Cell,
        DepthLabel,
        AttachmentContact,
        ReturnWitness,
        RewardMarker,
        InactiveMarker,
        ValidationIssue,
        Legend
    }

    public sealed class OptionalRegionOverlayDrawCommand
    {
        public OptionalRegionOverlayDrawCommand(
            OptionalRegionOverlayDrawCommandKind kind,
            int order,
            int fromSectorIndex,
            int toSectorIndex,
            OptionalRegionOverlayColorToken colorToken,
            string label)
        {
            if (!Enum.IsDefined(typeof(OptionalRegionOverlayDrawCommandKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (order < 0) throw new ArgumentOutOfRangeException(nameof(order));
            if (fromSectorIndex < -1 || fromSectorIndex >= WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(fromSectorIndex));
            if (toSectorIndex < -1 || toSectorIndex >= WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(toSectorIndex));
            if (!Enum.IsDefined(typeof(OptionalRegionOverlayColorToken), colorToken))
                throw new ArgumentOutOfRangeException(nameof(colorToken));
            if (string.IsNullOrEmpty(label) || !string.Equals(label, label.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Draw labels must be canonical non-empty text.", nameof(label));

            Kind = kind;
            Order = order;
            FromSectorIndex = fromSectorIndex;
            ToSectorIndex = toSectorIndex;
            ColorToken = colorToken;
            Label = label;
        }

        public OptionalRegionOverlayDrawCommandKind Kind { get; }
        public int Order { get; }
        public int FromSectorIndex { get; }
        public int ToSectorIndex { get; }
        public OptionalRegionOverlayColorToken ColorToken { get; }
        public string Label { get; }
    }

    public static class OptionalRegionOverlaySceneDrawer
    {
        private static readonly IReadOnlyList<OptionalRegionOverlayDrawCommand> EmptyCommands =
            new ReadOnlyCollection<OptionalRegionOverlayDrawCommand>(Array.Empty<OptionalRegionOverlayDrawCommand>());

        public static IReadOnlyList<OptionalRegionOverlayDrawCommand> BuildDrawCommands(
            OptionalRegionOverlaySnapshot snapshot)
        {
            if (snapshot == null || !snapshot.IsSuccess) return EmptyCommands;

            var commands = new List<OptionalRegionOverlayDrawCommand>();
            foreach (var cell in snapshot.Cells)
            {
                Add(commands, OptionalRegionOverlayDrawCommandKind.Cell, cell.SectorIndex, -1,
                    cell.ColorToken, cell.Label);
                if (cell.Kind == OptionalRegionOverlayCellKind.Type0)
                {
                    Add(commands, OptionalRegionOverlayDrawCommandKind.DepthLabel, cell.SectorIndex, -1,
                        cell.ColorToken, cell.Label);
                    Add(commands, OptionalRegionOverlayDrawCommandKind.RewardMarker, cell.SectorIndex, -1,
                        RewardColor(cell.RewardTier), RewardLabel(cell.RewardTier));
                }
                else if (cell.Kind == OptionalRegionOverlayCellKind.InactiveInterior ||
                         cell.Kind == OptionalRegionOverlayCellKind.InactiveDecorative)
                {
                    Add(commands, OptionalRegionOverlayDrawCommandKind.InactiveMarker, cell.SectorIndex, -1,
                        cell.ColorToken, cell.Label);
                }
            }

            foreach (var connection in snapshot.Connections)
            {
                Add(commands,
                    connection.Kind == OptionalRegionOverlayConnectionKind.AttachmentContact
                        ? OptionalRegionOverlayDrawCommandKind.AttachmentContact
                        : OptionalRegionOverlayDrawCommandKind.ReturnWitness,
                    connection.FromSectorIndex,
                    connection.ToSectorIndex,
                    connection.Kind == OptionalRegionOverlayConnectionKind.AttachmentContact
                        ? AccessColor(connection.AccessRule)
                        : OptionalRegionOverlayColorToken.ReturnBacktrack,
                    connection.Label);
            }

            foreach (var entry in snapshot.Legend)
                Add(commands, OptionalRegionOverlayDrawCommandKind.Legend, -1, -1, entry.ColorToken, entry.Label);

            return new ReadOnlyCollection<OptionalRegionOverlayDrawCommand>(commands);
        }

        public static void DrawScene(OptionalRegionOverlaySnapshot snapshot, Vector3 origin, float scale)
        {
            if (scale <= 0f) throw new ArgumentOutOfRangeException(nameof(scale));
            foreach (var command in BuildDrawCommands(snapshot))
            {
                var color = ToColor(command.ColorToken);
                if (command.Kind == OptionalRegionOverlayDrawCommandKind.Legend ||
                    command.Kind == OptionalRegionOverlayDrawCommandKind.ValidationIssue)
                    continue;

                var from = command.FromSectorIndex >= 0
                    ? origin + SectorCenter(command.FromSectorIndex, scale)
                    : origin;
                if (command.Kind == OptionalRegionOverlayDrawCommandKind.Cell)
                {
                    var half = new Vector3(WorldGenConstants.SectorWidthTiles * scale * 0.46f,
                        WorldGenConstants.SectorHeightTiles * scale * 0.46f, 0f);
                    var corners = new[]
                    {
                        from + new Vector3(-half.x, -half.y),
                        from + new Vector3(-half.x, half.y),
                        from + new Vector3(half.x, half.y),
                        from + new Vector3(half.x, -half.y)
                    };
                    Handles.DrawSolidRectangleWithOutline(corners, new Color(color.r, color.g, color.b, 0.2f), color);
                }
                else if (command.ToSectorIndex >= 0)
                {
                    Handles.color = color;
                    Handles.DrawLine(from, origin + SectorCenter(command.ToSectorIndex, scale));
                }
                else
                {
                    Handles.color = color;
                    Handles.Label(from, command.Label);
                }
            }
        }

        public static Vector3 SectorCenter(int sectorIndex, float scale)
        {
            if (sectorIndex < 0 || sectorIndex >= WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(nameof(sectorIndex));
            if (scale <= 0f) throw new ArgumentOutOfRangeException(nameof(scale));
            var coord = StarNight.Map.WorldGeneration.Generation.WorldGridIndex.ToCoordinate(sectorIndex);
            return new Vector3(
                (coord.X + 0.5f) * WorldGenConstants.SectorWidthTiles * scale,
                (coord.Y + 0.5f) * WorldGenConstants.SectorHeightTiles * scale,
                0f);
        }

        public static Color ToColor(OptionalRegionOverlayColorToken token)
        {
            switch (token)
            {
                case OptionalRegionOverlayColorToken.Mandatory: return new Color(0.95f, 0.95f, 0.95f);
                case OptionalRegionOverlayColorToken.ReservedSite: return new Color(0.7f, 0.7f, 0.7f);
                case OptionalRegionOverlayColorToken.Type0Basic: return new Color(0.25f, 0.75f, 1f);
                case OptionalRegionOverlayColorToken.Type0Tool: return new Color(1f, 0.75f, 0.2f);
                case OptionalRegionOverlayColorToken.Type0Environment: return new Color(0.25f, 0.9f, 0.35f);
                case OptionalRegionOverlayColorToken.Type0Explosive: return new Color(1f, 0.3f, 0.2f);
                case OptionalRegionOverlayColorToken.Type0Hidden: return new Color(0.65f, 0.35f, 0.95f);
                case OptionalRegionOverlayColorToken.RewardLow: return new Color(0.55f, 0.75f, 0.55f);
                case OptionalRegionOverlayColorToken.RewardMedium: return new Color(0.35f, 0.75f, 0.9f);
                case OptionalRegionOverlayColorToken.RewardHigh: return new Color(0.95f, 0.55f, 0.15f);
                case OptionalRegionOverlayColorToken.RewardUnique: return new Color(1f, 0.25f, 0.85f);
                case OptionalRegionOverlayColorToken.ReturnBacktrack: return new Color(0.15f, 1f, 1f);
                case OptionalRegionOverlayColorToken.InactiveInterior: return new Color(0.18f, 0.18f, 0.18f);
                case OptionalRegionOverlayColorToken.InactiveDecorative: return new Color(0.35f, 0.35f, 0.35f);
                case OptionalRegionOverlayColorToken.ValidationIssue: return Color.red;
                default: throw new ArgumentOutOfRangeException(nameof(token));
            }
        }

        private static void Add(
            List<OptionalRegionOverlayDrawCommand> commands,
            OptionalRegionOverlayDrawCommandKind kind,
            int fromSectorIndex,
            int toSectorIndex,
            OptionalRegionOverlayColorToken colorToken,
            string label)
        {
            commands.Add(new OptionalRegionOverlayDrawCommand(
                kind, commands.Count, fromSectorIndex, toSectorIndex, colorToken, label));
        }

        private static OptionalRegionOverlayColorToken AccessColor(OptionalRegionAccessRule value)
        {
            switch (value)
            {
                case OptionalRegionAccessRule.Basic: return OptionalRegionOverlayColorToken.Type0Basic;
                case OptionalRegionAccessRule.Tool: return OptionalRegionOverlayColorToken.Type0Tool;
                case OptionalRegionAccessRule.Environment: return OptionalRegionOverlayColorToken.Type0Environment;
                case OptionalRegionAccessRule.Explosive: return OptionalRegionOverlayColorToken.Type0Explosive;
                case OptionalRegionAccessRule.Hidden: return OptionalRegionOverlayColorToken.Type0Hidden;
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        private static OptionalRegionOverlayColorToken RewardColor(OptionalRewardTier value)
        {
            switch (value)
            {
                case OptionalRewardTier.Low: return OptionalRegionOverlayColorToken.RewardLow;
                case OptionalRewardTier.Medium: return OptionalRegionOverlayColorToken.RewardMedium;
                case OptionalRewardTier.High: return OptionalRegionOverlayColorToken.RewardHigh;
                case OptionalRewardTier.Unique: return OptionalRegionOverlayColorToken.RewardUnique;
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        private static string RewardLabel(OptionalRewardTier value)
        {
            switch (value)
            {
                case OptionalRewardTier.Low: return "L";
                case OptionalRewardTier.Medium: return "M";
                case OptionalRewardTier.High: return "H";
                case OptionalRewardTier.Unique: return "U";
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
    }
}
