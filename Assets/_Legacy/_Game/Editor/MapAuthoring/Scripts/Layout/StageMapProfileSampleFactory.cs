#if LEGACY_DISABLED
using System.Collections.Generic;
using StarNight.Stage.Layout;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    public static class StageMapProfileSampleFactory
    {
        public const string ProfileFolder = "Assets/_Game/Editor/MapAuthoring/Data/StageLayout/Profiles";
        public const string SampleProfilePath = ProfileFolder + "/E09_ProceduralPreview.asset";

        public static StageMapProfile EnsureSample()
        {
            EnsureFolder(ProfileFolder);
            StageMapProfile profile = AssetDatabase.LoadAssetAtPath<StageMapProfile>(SampleProfilePath);
            if (profile == null)
            {
                AssetDatabase.DeleteAsset(SampleProfilePath);
                profile = ScriptableObject.CreateInstance<StageMapProfile>();
                AssetDatabase.CreateAsset(profile, SampleProfilePath);
            }

            profile.StageId = "MAP-E09-PREVIEW";
            profile.MinRooms = 6;
            profile.MaxRooms = 9;
            profile.MainRouteLengthRange = new Vector2Int(4, 6);
            profile.BranchCountRange = new Vector2Int(1, 3);
            profile.LoopCountRange = new Vector2Int(0, 1);
            profile.RequiredRoles = new List<RoomRoleRequirement>
            {
                new RoomRoleRequirement { Role = RoomRole.Start, MinCount = 1, MaxCount = 1 },
                new RoomRoleRequirement { Role = RoomRole.Exit, MinCount = 1, MaxCount = 1 },
                new RoomRoleRequirement { Role = RoomRole.Branch, MinCount = 1, MaxCount = 3 },
            };
            profile.SizeWeights = new RoomSizeWeights
            {
                Micro = 4,
                Wide = 5,
                Tall = 4,
                Large = 3,
                LongHall = 2,
                DeepShaft = 2,
            };
            profile.AllowedFamilies = new List<LayoutFamily>
            {
                LayoutFamily.LinearBend,
                LayoutFamily.VerticalSpine,
                LayoutFamily.TwinBranchMerge,
                LayoutFamily.BrokenSpiral,
                LayoutFamily.HubAndSpokes,
            };
            profile.Budget = new StageElementBudget
            {
                Threat = 12,
                Utility = 10,
                Event = 3,
                Shop = 1,
                MaxSlotsPerRoom = 3,
            };
            profile.GuaranteedEvents = new List<GuaranteedEventRule>
            {
                new GuaranteedEventRule { EventId = "ReturnStatue", TargetRole = RoomRole.Rest, MinimumCount = 1 },
            };
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static void EnsureFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = $"{current}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }
    }
}

#endif
