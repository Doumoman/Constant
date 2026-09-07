using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.WorldGeneration.MoonPalace.RunGeneration
{
    /// <summary>Assigns reciprocal pattern/tile gate pairs without sector seam rules.</summary>
    public sealed class MoonPalaceCameraRoomConnectorPlan
    {
        internal MoonPalaceCameraRoomConnectorPlan(MoonPalaceCameraRoomGraph graph, IEnumerable<CameraRoomConnectorGate> gates, string digest)
        {
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            Gates = new ReadOnlyCollection<CameraRoomConnectorGate>((gates ?? Array.Empty<CameraRoomConnectorGate>()).ToList());
            CanonicalDigest = digest ?? string.Empty;
        }
        public MoonPalaceCameraRoomGraph Graph { get; }
        public IReadOnlyList<CameraRoomConnectorGate> Gates { get; }
        public int ConnectorCount => Gates.Count;
        public bool UsesSectorSeamLogic => false;
        public string CanonicalDigest { get; }
    }

    public static class MoonPalaceCameraRoomConnectorPlanner
    {
        public static MoonPalaceCameraRoomConnectorPlan Plan(MoonPalaceCameraRoomGraph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            var gates = graph.Transitions.OrderBy(transition => transition.ConnectorId, StringComparer.Ordinal).Select(transition =>
                new CameraRoomConnectorGate(transition, GateRect(transition.FromPatternSlot, transition.Direction), GateRect(transition.ToPatternSlot, Opposite(transition.Direction)))).ToList();
            if (gates.Count == 0 || gates.Select(gate => gate.ConnectorId).Distinct(StringComparer.Ordinal).Count() != gates.Count || gates.Any(gate => !IsReciprocal(graph, gate)))
                throw new InvalidOperationException("RUN03 rejects non-reciprocal connector gates before composing a single tile.");
            var lines = new List<string> { "RUN03_CONNECTORS", graph.Digest.Value, "uses_sector_seam_logic=0" };
            lines.AddRange(gates.Select(gate => "GATE|" + gate.ConnectorId + "|" + gate.FromRoomId + "|" + gate.ToRoomId + "|" + gate.Direction + "|" + gate.FromPatternSlot + "|" + gate.ToPatternSlot + "|" + gate.FromTileGateRect + "|" + gate.ToTileGateRect + "|" + gate.TransitionKind + "|" + (gate.IsRequiredForCompletion ? "1" : "0")));
            return new MoonPalaceCameraRoomConnectorPlan(graph, gates, BakingCanonicalDigest.HashCanonicalLines(lines));
        }

        public static bool IsReciprocal(MoonPalaceCameraRoomGraph graph, CameraRoomConnectorGate gate)
        {
            if (graph == null || gate == null || string.IsNullOrWhiteSpace(gate.FromRoomId) || string.IsNullOrWhiteSpace(gate.ToRoomId) || string.Equals(gate.FromRoomId, gate.ToRoomId, StringComparison.Ordinal)) return false;
            var from = graph.Room(gate.FromRoomId); var to = graph.Room(gate.ToRoomId);
            if (!from.Bounds.IsEdge(gate.FromPatternSlot) || !to.Bounds.IsEdge(gate.ToPatternSlot)) return false;
            if (CameraRoomEdge.DirectionFrom(gate.FromPatternSlot, gate.ToPatternSlot) != gate.Direction) return false;
            var expectedFrom = GateRect(gate.FromPatternSlot, gate.Direction); var expectedTo = GateRect(gate.ToPatternSlot, Opposite(gate.Direction));
            return gate.FromTileGateRect.X == expectedFrom.X && gate.FromTileGateRect.Y == expectedFrom.Y && gate.ToTileGateRect.X == expectedTo.X && gate.ToTileGateRect.Y == expectedTo.Y &&
                gate.RequiredSocketFrom == MoonPalaceCameraRoomPatternComposer.RouteSocketBit && gate.RequiredSocketTo == MoonPalaceCameraRoomPatternComposer.RouteSocketBit;
        }

        public static CameraRoomTileGateRect GateRect(PatternSlotCoordinate slot, MoonPalaceRunDirection direction)
        {
            var x = slot.X * MoonPalaceCameraRoomCourseConfig.PatternSize;
            var y = slot.Y * MoonPalaceCameraRoomCourseConfig.PatternSize;
            if (direction == MoonPalaceRunDirection.North) return new CameraRoomTileGateRect(x + 1, y + 3, 1, 1);
            if (direction == MoonPalaceRunDirection.South) return new CameraRoomTileGateRect(x + 1, y, 1, 1);
            if (direction == MoonPalaceRunDirection.East) return new CameraRoomTileGateRect(x + 3, y + 1, 1, 1);
            return new CameraRoomTileGateRect(x, y + 1, 1, 1);
        }

        public static MoonPalaceRunDirection Opposite(MoonPalaceRunDirection direction)
        {
            return direction == MoonPalaceRunDirection.North ? MoonPalaceRunDirection.South : direction == MoonPalaceRunDirection.South ? MoonPalaceRunDirection.North :
                direction == MoonPalaceRunDirection.East ? MoonPalaceRunDirection.West : MoonPalaceRunDirection.East;
        }
    }
}
