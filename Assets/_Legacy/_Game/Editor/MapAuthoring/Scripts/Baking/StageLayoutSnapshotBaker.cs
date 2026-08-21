#if LEGACY_DISABLED
using System;
using System.Linq;
using StarNight.Stage.Layout;
using StarNight.Stage.Layout.Authoring;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    public static class StageLayoutSnapshotBaker
    {
        public const string SnapshotFolder = "Assets/_Game/Map/Data/Stages";

        public static StageLayoutSnapshot BakeCurrentScene(
            StageMapProfile profile,
            int seed,
            string validationHash = null)
        {
            string stageId = profile != null && !string.IsNullOrWhiteSpace(profile.StageId)
                ? profile.StageId
                : "MAP-E10-PREVIEW";
            string path = GetSnapshotPath(stageId, seed);
            StageLayoutSnapshot snapshot = AssetDatabase.LoadAssetAtPath<StageLayoutSnapshot>(path);
            if (snapshot == null)
            {
                snapshot = ScriptableObject.CreateInstance<StageLayoutSnapshot>();
                AssetDatabase.CreateAsset(snapshot, path);
            }

            snapshot.StageId = stageId;
            snapshot.Seed = seed;
            snapshot.Rooms.Clear();
            snapshot.Connections.Clear();

            StageRoomProxy[] rooms = UnityEngine.Object.FindObjectsByType<StageRoomProxy>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OrderBy(room => room.NodeGuid, StringComparer.Ordinal)
                .ToArray();
            for (int index = 0; index < rooms.Length; index++)
            {
                StageRoomProxy room = rooms[index];
                snapshot.Rooms.Add(new RoomNodeSnapshot
                {
                    NodeGuid = room.NodeGuid,
                    Template = room.Template,
                    PositionCells = room.PositionCells,
                    Role = room.Role,
                    Locked = room.Locked,
                    MainRoute = room.MainRoute,
                });
            }

            StageLayoutConnectionProxy[] connections = UnityEngine.Object.FindObjectsByType<StageLayoutConnectionProxy>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OrderBy(connection => connection.ConnectionGuid, StringComparer.Ordinal)
                .ToArray();
            for (int index = 0; index < connections.Length; index++)
            {
                StageLayoutConnectionProxy connection = connections[index];
                TraversalType traversal = TraversalType.Walk;
                if (connection.SourceRoom != null && connection.SourceRoom.TryGetSocket(connection.SourceSocketGuid, out RoomSocketDefinition socket))
                    traversal = socket.Traversal;
                snapshot.Connections.Add(new RoomConnectionSnapshot
                {
                    ConnectionGuid = connection.ConnectionGuid,
                    SourceNodeGuid = connection.SourceRoom != null ? connection.SourceRoom.NodeGuid : string.Empty,
                    SourceSocketGuid = connection.SourceSocketGuid,
                    TargetNodeGuid = connection.TargetRoom != null ? connection.TargetRoom.NodeGuid : string.Empty,
                    TargetSocketGuid = connection.TargetSocketGuid,
                    Secret = connection.VisualKind == StageConnectionVisualKind.Secret,
                    MaruRoute = connection.VisualKind == StageConnectionVisualKind.Maru ||
                                connection.VisualKind == StageConnectionVisualKind.MainRoute,
                    Traversal = traversal,
                });
            }

            snapshot.ValidationHash = !string.IsNullOrWhiteSpace(validationHash)
                ? validationHash
                : BakeHashUtility.ComputeStringHash(CreateFingerprint(snapshot));
            EditorUtility.SetDirty(snapshot);
            AssetDatabase.SaveAssets();
            return snapshot;
        }

        public static string GetSnapshotPath(string stageId, int seed)
        {
            return $"{SnapshotFolder}/{SanitizeFileName(stageId)}_{seed}.asset";
        }

        private static string CreateFingerprint(StageLayoutSnapshot snapshot)
        {
            string rooms = string.Join("|", snapshot.Rooms.Select(room =>
                $"{room.NodeGuid}:{room.Template?.RoomId}:{room.PositionCells.x},{room.PositionCells.y}:{room.Role}:{room.Locked}:{room.MainRoute}"));
            string connections = string.Join("|", snapshot.Connections.Select(connection =>
                $"{connection.ConnectionGuid}:{connection.SourceNodeGuid}/{connection.SourceSocketGuid}>{connection.TargetNodeGuid}/{connection.TargetSocketGuid}:{connection.Secret}:{connection.MaruRoute}:{connection.Traversal}"));
            return $"{snapshot.StageId}:{snapshot.Seed}:{rooms}:{connections}";
        }

        internal static string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Stage";
            foreach (char invalid in System.IO.Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
            return value.Replace('/', '_').Replace('\\', '_').Trim();
        }
    }
}

#endif
