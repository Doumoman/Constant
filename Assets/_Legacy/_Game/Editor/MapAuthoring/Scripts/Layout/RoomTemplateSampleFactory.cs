#if LEGACY_DISABLED
using System.Collections.Generic;
using StarNight.Stage.Layout;
using StarNight.Stage.Rooms;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    public static class RoomTemplateSampleFactory
    {
        public const string SampleFolder = "Assets/_Game/Editor/MapAuthoring/Data/StageLayout/RoomTemplates";

        public static IReadOnlyList<RoomTemplate> EnsureSamples()
        {
            EnsureFolder(SampleFolder);
            var samples = new List<RoomTemplate>
            {
                EnsureTemplate("E08_Micro_Start", RoomSizeCatalog.Micro, RoomRole.Start),
                EnsureTemplate("E08_Wide_Main", RoomSizeCatalog.Wide, RoomRole.Main),
                EnsureTemplate("E08_Tall_Branch", RoomSizeCatalog.Tall, RoomRole.Branch),
                EnsureTemplate("E08_Large_Exit", RoomSizeCatalog.Large, RoomRole.Exit),
                EnsureTemplate("E09_LongHall_Rest", RoomSizeCatalog.LongHall, RoomRole.Rest),
                EnsureTemplate("E09_DeepShaft_Secret", RoomSizeCatalog.DeepShaft, RoomRole.Secret),
            };
            AssetDatabase.SaveAssets();
            return samples;
        }

        private static RoomTemplate EnsureTemplate(string roomId, Vector2Int size, RoomRole role)
        {
            string path = $"{SampleFolder}/{roomId}.asset";
            RoomTemplate template = AssetDatabase.LoadAssetAtPath<RoomTemplate>(path);
            if (template == null)
            {
                AssetDatabase.DeleteAsset(path);
                template = ScriptableObject.CreateInstance<RoomTemplate>();
                AssetDatabase.CreateAsset(template, path);
            }

            template.RoomId = roomId;
            template.Region = RegionId.Common;
            template.Role = role;
            template.SizeCells = size;
            template.CameraMode = RoomCameraMode.Fixed;
            template.Budget = template.Budget ?? new RoomBudget();
            template.ContentTags = new List<string> { "MAP-E08", role.ToString(), $"{size.x}x{size.y}" };
            template.GeometryHash = template.GeometryHash ?? new RoomGeometryHash();
            template.GeometryHash.Value = $"E08-{roomId}-{size.x}x{size.y}";
            template.Sockets = new List<RoomSocketDefinition>
            {
                CreateSocket("Left", CardinalDirection.Left, new Vector2Int(0, 2)),
                CreateSocket("Right", CardinalDirection.Right, new Vector2Int(size.x, 2)),
                CreateSocket("Up", CardinalDirection.Up, new Vector2Int(size.x / 2, size.y), TraversalType.Climb),
                CreateSocket("Down", CardinalDirection.Down, new Vector2Int(size.x / 2, 0), TraversalType.Climb),
            };
            EditorUtility.SetDirty(template);
            return template;
        }

        private static RoomSocketDefinition CreateSocket(
            string id,
            CardinalDirection side,
            Vector2Int localCell,
            TraversalType traversal = TraversalType.Walk)
        {
            return new RoomSocketDefinition
            {
                SocketGuid = id,
                Side = side,
                LocalCell = localCell,
                OpeningSizeCells = Vector2Int.one,
                Traversal = traversal,
                MainRouteAllowed = true,
                FloorHeightCell = traversal == TraversalType.Walk ? 2 : 0,
            };
        }

        private static void EnsureFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = $"{current}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }
                current = next;
            }
        }
    }
}

#endif
