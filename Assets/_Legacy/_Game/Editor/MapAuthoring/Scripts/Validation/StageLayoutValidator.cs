#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Stage.Layout;
using StarNight.Stage.Layout.Authoring;
using StarNight.Stage.Transitions;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    public static class StageLayoutValidator
    {
        public static MapElementValidationReport ValidateCurrentScene()
        {
            StageRoomProxy[] rooms = UnityEngine.Object.FindObjectsByType<StageRoomProxy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            StageLayoutConnectionProxy[] connections = UnityEngine.Object.FindObjectsByType<StageLayoutConnectionProxy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            return Validate(rooms, connections);
        }

        public static MapElementValidationReport Validate(IReadOnlyList<StageRoomProxy> rooms, IReadOnlyList<StageLayoutConnectionProxy> connections)
        {
            var report = new MapElementValidationReport("MAP-E09 Stage Layout");
            var nodeGuids = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < rooms.Count; index++)
            {
                StageRoomProxy room = rooms[index];
                if (room == null) continue;

                if (string.IsNullOrWhiteSpace(room.NodeGuid) || !nodeGuids.Add(room.NodeGuid))
                    report.Add(ValidationSeverity.Error, "LAYOUT_NODE_GUID", "Room NodeGuid is missing or duplicated.", context: room);
                if (room.Template == null)
                {
                    report.Add(ValidationSeverity.Error, "LAYOUT_TEMPLATE", "Room Proxy requires a RoomTemplate.", context: room);
                    continue;
                }
                if (room.SizeCells.x <= 0 || room.SizeCells.y <= 0)
                    report.Add(ValidationSeverity.Error, "LAYOUT_SIZE", "Room size must be positive.", context: room.Template);
                if (room.PositionCells != StageLayoutGraphUtility.SnapToPlacementGrid(room.PositionCells))
                    report.Add(ValidationSeverity.Error, "LAYOUT_SNAP", "Room position must use the 2-cell placement grid.", context: room, autoFixable: true);
                ValidateSockets(room, report);
            }

            for (int first = 0; first < rooms.Count; first++)
            {
                if (rooms[first] == null || rooms[first].Template == null) continue;
                for (int second = first + 1; second < rooms.Count; second++)
                {
                    if (rooms[second] == null || rooms[second].Template == null) continue;
                    if (StageLayoutGraphUtility.RoomsOverlap(rooms[first].PositionCells, rooms[first].SizeCells, rooms[second].PositionCells, rooms[second].SizeCells))
                    {
                        report.Add(ValidationSeverity.Error, "LAYOUT_OVERLAP", $"{rooms[first].NodeGuid} overlaps {rooms[second].NodeGuid}.", context: rooms[second]);
                    }
                }
            }

            for (int index = 0; index < connections.Count; index++)
            {
                StageLayoutConnectionProxy connection = connections[index];
                if (connection == null) continue;
                SocketCompatibility compatibility = connection.GetCompatibility();
                if (compatibility != SocketCompatibility.Compatible)
                {
                    report.Add(ValidationSeverity.Error, "LAYOUT_SOCKET_INCOMPATIBLE", $"Connection {connection.ConnectionGuid} is incompatible: {compatibility}.", context: connection);
                    continue;
                }

                connection.SourceRoom.TryGetSocket(connection.SourceSocketGuid, out RoomSocketDefinition source);
                connection.TargetRoom.TryGetSocket(connection.TargetSocketGuid, out RoomSocketDefinition target);
                if (connection.VisualKind == StageConnectionVisualKind.MainRoute && (!source.MainRouteAllowed || !target.MainRouteAllowed))
                    report.Add(ValidationSeverity.Error, "LAYOUT_MAIN_ROUTE_SOCKET", "Main route uses a socket that does not allow the main route.", context: connection);
            }

            ValidateMainRoute(rooms, connections, report);

            if (rooms.Count == 0)
                report.Add(ValidationSeverity.Warning, "LAYOUT_EMPTY", "No Room Proxy exists in the layout.");
            else
                report.Add(ValidationSeverity.Info, "LAYOUT_SUMMARY", $"Rooms {rooms.Count}, Connections {connections.Count}.");
            return report;
        }

        public static void SnapAllRooms()
        {
            StageRoomProxy[] rooms = UnityEngine.Object.FindObjectsByType<StageRoomProxy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int index = 0; index < rooms.Length; index++) rooms[index].SetPositionCells(rooms[index].PositionCells);
        }

        private static void ValidateSockets(StageRoomProxy room, MapElementValidationReport report)
        {
            var socketGuids = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<RoomSocketDefinition> sockets = room.Template.Sockets;
            if (sockets == null || sockets.Count == 0)
            {
                report.Add(ValidationSeverity.Error, "LAYOUT_SOCKET_REQUIRED", "RoomTemplate requires at least one socket.", context: room.Template);
                return;
            }

            for (int index = 0; index < sockets.Count; index++)
            {
                RoomSocketDefinition socket = sockets[index];
                if (socket == null || string.IsNullOrWhiteSpace(socket.SocketGuid) || !socketGuids.Add(socket.SocketGuid))
                {
                    report.Add(ValidationSeverity.Error, "LAYOUT_SOCKET_GUID", "SocketGuid is missing or duplicated.", context: room.Template);
                    continue;
                }
                if (!StageLayoutGraphUtility.IsSocketOnBoundary(socket, room.SizeCells))
                    report.Add(ValidationSeverity.Error, "LAYOUT_SOCKET_BOUNDARY", $"Socket {socket.SocketGuid} is not on the {socket.Side} boundary.", context: room.Template);
                if (!RoomPortalContract.IsOneCellSocket(socket))
                    report.Add(ValidationSeverity.Error, "LAYOUT_SOCKET_SIZE", $"Socket {socket.SocketGuid} must be exactly 1x1 cell.", context: room.Template);
            }
        }

        private static void ValidateMainRoute(
            IReadOnlyList<StageRoomProxy> rooms,
            IReadOnlyList<StageLayoutConnectionProxy> connections,
            MapElementValidationReport report)
        {
            StageRoomProxy start = null;
            StageRoomProxy exit = null;
            for (int index = 0; index < rooms.Count; index++)
            {
                if (rooms[index] == null) continue;
                if (rooms[index].Role == RoomRole.Start) start = rooms[index];
                if (rooms[index].Role == RoomRole.Exit) exit = rooms[index];
            }
            if (start == null || exit == null)
            {
                report.Add(ValidationSeverity.Error, "LAYOUT_MAIN_ENDPOINT", "Start and Exit rooms are required for the Main Route.");
                return;
            }

            var visited = new HashSet<string>(StringComparer.Ordinal) { start.NodeGuid };
            var queue = new Queue<string>();
            queue.Enqueue(start.NodeGuid);
            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                for (int index = 0; index < connections.Count; index++)
                {
                    StageLayoutConnectionProxy edge = connections[index];
                    if (edge == null || edge.VisualKind != StageConnectionVisualKind.MainRoute || edge.SourceRoom == null || edge.TargetRoom == null) continue;
                    string next = string.Equals(edge.SourceRoom.NodeGuid, current, StringComparison.Ordinal)
                        ? edge.TargetRoom.NodeGuid
                        : string.Equals(edge.TargetRoom.NodeGuid, current, StringComparison.Ordinal)
                            ? edge.SourceRoom.NodeGuid
                            : null;
                    if (next != null && visited.Add(next)) queue.Enqueue(next);
                }
            }
            if (!visited.Contains(exit.NodeGuid))
                report.Add(ValidationSeverity.Error, "LAYOUT_MAIN_ROUTE", "Start cannot reach Exit through Main Route connections.", context: exit);
            else
                report.Add(ValidationSeverity.Info, "LAYOUT_MAIN_ROUTE", "Start to Exit Main Route is connected.");
        }
    }
}

#endif
