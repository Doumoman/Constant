#if LEGACY_DISABLED
using System;
using StarNight.Map;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    public enum MapElementLabPreset
    {
        Empty,
        Spike1x1,
        MovingPlatform2x1,
    }

    public static class MapElementDefinitionPresetFactory
    {
        public const string SampleFolder =
            "Assets/_Game/Editor/MapAuthoring/SourceElements/Samples";

        public const string SpikeSamplePath =
            SampleFolder + "/COMMON_Lab_Spike.asset";

        public const string MovingPlatformSamplePath =
            SampleFolder + "/COMMON_Lab_MovingPlatform.asset";

        public static MapElementDefinition CreatePreset(MapElementLabPreset preset)
        {
            var definition = ScriptableObject.CreateInstance<MapElementDefinition>();
            ApplyPreset(definition, preset);
            return definition;
        }

        public static MapElementDefinition CreatePresetAsset(MapElementLabPreset preset)
        {
            EnsureFolder(SampleFolder);
            var baseName = preset switch
            {
                MapElementLabPreset.Spike1x1 => "COMMON_Lab_Spike.asset",
                MapElementLabPreset.MovingPlatform2x1 => "COMMON_Lab_MovingPlatform.asset",
                _ => "COMMON_New_MapElement.asset",
            };
            var path = AssetDatabase.GenerateUniqueAssetPath($"{SampleFolder}/{baseName}");
            var definition = CreatePreset(preset);
            AssetDatabase.CreateAsset(definition, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = definition;
            return definition;
        }

        public static void EnsureLabSamples(
            out MapElementDefinition spike,
            out MapElementDefinition movingPlatform)
        {
            EnsureFolder(SampleFolder);
            spike = AssetDatabase.LoadAssetAtPath<MapElementDefinition>(SpikeSamplePath);
            if (spike == null)
            {
                spike = CreatePreset(MapElementLabPreset.Spike1x1);
                AssetDatabase.CreateAsset(spike, SpikeSamplePath);
            }

            movingPlatform = AssetDatabase.LoadAssetAtPath<MapElementDefinition>(MovingPlatformSamplePath);
            if (movingPlatform == null)
            {
                movingPlatform = CreatePreset(MapElementLabPreset.MovingPlatform2x1);
                AssetDatabase.CreateAsset(movingPlatform, MovingPlatformSamplePath);
            }

            AssetDatabase.SaveAssets();
        }

        public static void ApplyPreset(MapElementDefinition definition, MapElementLabPreset preset)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            definition.ElementId = "COMMON_New_MapElement";
            definition.DisplayName = "New Map Element";
            definition.Category = ElementCategory.Utility;
            definition.AllowedRegions = RegionMask.Common;
            definition.RuntimePrefab = null;
            definition.Footprint = new CellFootprint();
            definition.VisualProfile = new ElementVisualProfile
            {
                RenderMode = ElementVisualRenderMode.SingleSprite,
                VisualSizeCells = Vector2.one,
                VisualOffsetCells = Vector2.zero,
            };
            definition.CollisionProfile = new ElementCollisionProfile
            {
                Mode = ColliderAuthoringMode.MergedBoxes,
                IsSolid = true,
            };
            definition.BehaviorProfile = new ElementBehaviorProfile
            {
                InitialState = MapElementState.Idle,
            };
            definition.PlacementProfile = new ElementPlacementProfile();
            definition.BudgetProfile = new ElementBudgetProfile();
            definition.CommonProfile = new CommonElementRuntimeProfile();
            definition.MaruProfile = new MaruElementRuntimeProfile();
            definition.MoonProfile = new MoonElementRuntimeProfile();
            definition.BridgeProfile = new BridgeElementRuntimeProfile();
            definition.PalaceProfile = new PalaceElementRuntimeProfile();
            definition.PostProfile = new PostElementRuntimeProfile();
            definition.SunProfile = new SunElementRuntimeProfile();
            definition.PolarisProfile = new PolarisElementRuntimeProfile();
            definition.ToolReactions = new ToolReactionTable();
            definition.MaruReaction = new MaruReactionProfile();
            definition.BakeMetadata = new ElementBakeMetadata();

            switch (preset)
            {
                case MapElementLabPreset.Spike1x1:
                    ConfigureSpike(definition);
                    break;
                case MapElementLabPreset.MovingPlatform2x1:
                    ConfigureMovingPlatform(definition);
                    break;
                default:
                    definition.CollisionProfile.SolidShapes.Add(new SerializedColliderShape
                    {
                        ShapeType = SerializedColliderShapeType.Box,
                        SizeCells = new Vector2(0.98f, 0.98f),
                    });
                    break;
            }
        }

        private static void ConfigureSpike(MapElementDefinition definition)
        {
            definition.ElementId = "COMMON_Lab_Spike";
            definition.DisplayName = "1×1 Spike";
            definition.Category = ElementCategory.Hazard;
            definition.Footprint.BoundsSize = Vector2Int.one;
            definition.Footprint.PivotCell = Vector2Int.zero;
            definition.Footprint.OccupiedCells.Clear();
            definition.Footprint.OccupiedCells.Add(Vector2Int.zero);
            definition.Footprint.HazardCells.Add(Vector2Int.zero);
            definition.Footprint.TriggerCells.Add(Vector2Int.zero);
            definition.VisualProfile.RenderMode = ElementVisualRenderMode.SingleSprite;
            definition.VisualProfile.VisualSizeCells = Vector2.one;
            definition.VisualProfile.VisualOffsetCells = Vector2.zero;
            definition.CollisionProfile.IsSolid = true;
            definition.CollisionProfile.SolidShapes.Add(new SerializedColliderShape
            {
                ShapeType = SerializedColliderShapeType.Box,
                OffsetCells = new Vector2(0f, -0.38f),
                SizeCells = new Vector2(0.96f, 0.22f),
            });
            definition.CollisionProfile.TriggerShapes.Add(new SerializedColliderShape
            {
                ShapeType = SerializedColliderShapeType.Box,
                OffsetCells = new Vector2(0f, 0.06f),
                SizeCells = new Vector2(0.82f, 0.72f),
            });
            definition.BehaviorProfile.WarningSeconds = 0.35f;
            definition.BehaviorProfile.ActiveSeconds = 0.9f;
            definition.BehaviorProfile.CooldownSeconds = 0.6f;
            definition.BudgetProfile.ThreatCost = 2;
            definition.ToolReactions.Entries.Add(new ToolReactionEntry
            {
                Tool = ToolTag.Bomb,
                Reaction = ElementReactionType.Break,
                StrengthRequired = 1,
            });
        }

        private static void ConfigureMovingPlatform(MapElementDefinition definition)
        {
            definition.ElementId = "COMMON_Lab_MovingPlatform";
            definition.DisplayName = "2×1 Moving Platform";
            definition.Category = ElementCategory.Platform;
            definition.Footprint.BoundsSize = new Vector2Int(2, 1);
            definition.Footprint.PivotCell = Vector2Int.zero;
            definition.Footprint.OccupiedCells.Clear();
            definition.Footprint.OccupiedCells.Add(Vector2Int.zero);
            definition.Footprint.OccupiedCells.Add(Vector2Int.right);
            definition.Footprint.ClearanceRequiredCells.Add(new Vector2Int(0, 1));
            definition.Footprint.ClearanceRequiredCells.Add(new Vector2Int(1, 1));
            definition.VisualProfile.RenderMode = ElementVisualRenderMode.TiledSprite;
            definition.VisualProfile.VisualSizeCells = new Vector2(2f, 1f);
            definition.VisualProfile.VisualOffsetCells = new Vector2(0.5f, 0f);
            definition.CollisionProfile.IsSolid = true;
            definition.CollisionProfile.SolidShapes.Add(new SerializedColliderShape
            {
                ShapeType = SerializedColliderShapeType.Box,
                OffsetCells = new Vector2(0.5f, 0f),
                SizeCells = new Vector2(1.98f, 0.78f),
            });
            definition.BehaviorProfile.Path.Nodes.Add(Vector2.zero);
            definition.BehaviorProfile.Path.Nodes.Add(new Vector2(4f, 0f));
            definition.BehaviorProfile.Path.SpeedCellsPerSecond = 2f;
            definition.BehaviorProfile.Path.WaitSeconds = 0.25f;
            definition.BehaviorProfile.Path.PingPong = true;
            definition.BehaviorProfile.Path.StartForward = true;
            definition.BudgetProfile.MotionCost = 1;
        }

        private static void EnsureFolder(string folderPath)
        {
            var parts = folderPath.Split('/');
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
    }
}

#endif
