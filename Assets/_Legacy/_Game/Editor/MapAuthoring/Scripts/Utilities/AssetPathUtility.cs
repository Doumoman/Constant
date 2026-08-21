#if LEGACY_DISABLED
using System;
using System.IO;
using StarNight.Map;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    public readonly struct MapElementBakePaths
    {
        public MapElementBakePaths(
            string region,
            string sourcePrefab,
            string runtimePrefab,
            string definition,
            string visualProfile)
        {
            Region = region;
            SourcePrefab = sourcePrefab;
            RuntimePrefab = runtimePrefab;
            Definition = definition;
            VisualProfile = visualProfile;
        }

        public string Region { get; }
        public string SourcePrefab { get; }
        public string RuntimePrefab { get; }
        public string Definition { get; }
        public string VisualProfile { get; }
    }

    public static class AssetPathUtility
    {
        public static MapElementBakePaths GetMapElementBakePaths(MapElementDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            var elementId = definition.ElementId?.Trim();
            if (!IsSafeFileName(elementId))
            {
                throw new ArgumentException("Element ID must be a safe file name.", nameof(definition));
            }

            var region = GetRegionFolder(elementId, definition.AllowedRegions);
            return new MapElementBakePaths(
                region,
                $"Assets/_Game/Editor/MapAuthoring/SourceElements/{elementId}_Source.prefab",
                $"Assets/_Game/Map/Prefabs/Elements/{region}/{elementId}.prefab",
                $"Assets/_Game/Map/Data/Elements/{region}/{elementId}.asset",
                $"Assets/_Game/Map/VisualProfiles/{region}/{elementId}_Visual.asset");
        }

        public static string ToAbsolutePath(string assetPath)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        public static void EnsureParentFolder(string assetPath)
        {
            var folder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(folder))
            {
                return;
            }

            EnsureFolder(folder);
        }

        public static void EnsureFolder(string folderPath)
        {
            var normalized = folderPath.Replace('\\', '/').TrimEnd('/');
            var parts = normalized.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = $"{current}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }

        public static bool IsSafeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.IndexOf('_') <= 0)
            {
                return false;
            }

            return value.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 &&
                   value.IndexOf('/') < 0 &&
                   value.IndexOf('\\') < 0;
        }

        private static string GetRegionFolder(string elementId, RegionMask allowedRegions)
        {
            var prefix = elementId.Split('_')[0].ToUpperInvariant();
            switch (prefix)
            {
                case "MOON": return "Moon";
                case "BRIDGE": return "Bridge";
                case "PALACE": return "Palace";
                case "POST": return "Post";
                case "SUN": return "Sun";
                case "POLARIS": return "Polaris";
                case "MARU": return "Maru";
                case "COMMON": return "Common";
            }

            if ((allowedRegions & RegionMask.Moon) != 0) return "Moon";
            if ((allowedRegions & RegionMask.Bridge) != 0) return "Bridge";
            if ((allowedRegions & RegionMask.Palace) != 0) return "Palace";
            if ((allowedRegions & RegionMask.Post) != 0) return "Post";
            if ((allowedRegions & RegionMask.Sun) != 0) return "Sun";
            if ((allowedRegions & RegionMask.Polaris) != 0) return "Polaris";
            if ((allowedRegions & RegionMask.Maru) != 0) return "Maru";
            return "Common";
        }
    }
}

#endif
