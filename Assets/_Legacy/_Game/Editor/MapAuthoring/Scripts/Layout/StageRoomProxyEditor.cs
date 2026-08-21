#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Stage.Layout;
using StarNight.Stage.Layout.Authoring;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    [CustomEditor(typeof(StageRoomProxy))]
    public sealed class StageRoomProxyEditor : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            StageRoomProxy proxy = (StageRoomProxy)target;
            if (proxy.Template == null) return;
            EditorGUI.BeginChangeCheck();
            Vector3 moved = Handles.PositionHandle(proxy.transform.position, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(proxy, "Move Room Proxy");
                proxy.SetPositionCells(new Vector2Int(
                    Mathf.RoundToInt(moved.x / StageRoomProxy.PreviewCellScale),
                    Mathf.RoundToInt(moved.y / StageRoomProxy.PreviewCellScale)));
                EditorUtility.SetDirty(proxy);
                StageLayoutWorkbenchWindow.RefreshValidationIfOpen();
            }

            IReadOnlyList<RoomSocketDefinition> sockets = proxy.Template.Sockets;
            for (int index = 0; index < sockets.Count; index++)
            {
                RoomSocketDefinition socket = sockets[index];
                if (socket == null) continue;
                SocketCompatibility compatibility = StageLayoutAuthoringSession.GetPendingCompatibility(proxy, socket);
                Handles.color = compatibility == SocketCompatibility.Compatible
                    ? new Color(0.2f, 1f, 0.5f)
                    : compatibility == SocketCompatibility.MissingSocket ? new Color(0.95f, 0.8f, 0.25f) : new Color(1f, 0.2f, 0.2f);
                Vector3 position = proxy.GetSocketWorldPosition(socket);
                if (Handles.Button(position, Quaternion.identity, 0.16f, 0.22f, Handles.RectangleHandleCap))
                {
                    StageLayoutAuthoringSession.ClickSocket(proxy, socket);
                    SceneView.RepaintAll();
                }
                Handles.Label(position + Vector3.up * 0.22f, socket.SocketGuid);
            }

            Vector3 label = proxy.transform.position + new Vector3(
                proxy.SizeCells.x * StageRoomProxy.PreviewCellScale * 0.5f,
                proxy.SizeCells.y * StageRoomProxy.PreviewCellScale * 0.5f,
                0f);
            Handles.color = Color.white;
            Handles.Label(label, $"{proxy.Template.RoomId}\n{proxy.SizeCells.x}x{proxy.SizeCells.y}");
        }
    }

    public static class StageLayoutAuthoringSession
    {
        private static StageRoomProxy pendingRoom;
        private static RoomSocketDefinition pendingSocket;

        public static SocketCompatibility GetPendingCompatibility(StageRoomProxy room, RoomSocketDefinition socket)
        {
            if (pendingRoom == null || pendingSocket == null) return SocketCompatibility.MissingSocket;
            return StageLayoutGraphUtility.GetCompatibility(pendingSocket, socket, pendingRoom == room);
        }

        public static void ClickSocket(StageRoomProxy room, RoomSocketDefinition socket)
        {
            if (pendingRoom == null || pendingSocket == null)
            {
                pendingRoom = room;
                pendingSocket = socket;
                return;
            }
            SocketCompatibility compatibility = StageLayoutGraphUtility.GetCompatibility(pendingSocket, socket, pendingRoom == room);
            if (compatibility != SocketCompatibility.Compatible)
            {
                Debug.LogWarning($"[MAP-E08] Socket connection rejected: {compatibility}", room);
                return;
            }
            GameObject root = GameObject.Find("GraphLineRoot");
            GameObject connectionObject = new GameObject($"Connection_{pendingRoom.NodeGuid}_{room.NodeGuid}");
            Undo.RegisterCreatedObjectUndo(connectionObject, "Connect Room Sockets");
            connectionObject.transform.SetParent(root != null ? root.transform : null, false);
            StageLayoutConnectionProxy connection = connectionObject.AddComponent<StageLayoutConnectionProxy>();
            StageConnectionVisualKind kind = pendingRoom.Role == RoomRole.Secret || room.Role == RoomRole.Secret
                ? StageConnectionVisualKind.Secret
                : pendingRoom.MainRoute && room.MainRoute ? StageConnectionVisualKind.MainRoute : StageConnectionVisualKind.Branch;
            connection.Configure(Guid.NewGuid().ToString("N"), pendingRoom, pendingSocket.SocketGuid, room, socket.SocketGuid, kind);
            pendingRoom = null;
            pendingSocket = null;
            EditorUtility.SetDirty(connection);
            StageLayoutWorkbenchWindow.RefreshValidationIfOpen();
        }

        public static void ClearPendingSocket()
        {
            pendingRoom = null;
            pendingSocket = null;
        }
    }
}

#endif
