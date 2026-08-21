#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Map;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    public static class PolarisElementCatalogFactory
    {
        public const string CatalogFolder =
            "Assets/_Game/Editor/MapAuthoring/SourceElements/Polaris";

        public static readonly string[] CatalogIds =
        {
            "POLARIS_OrbitPlatform",
            "POLARIS_ObservationBeam",
            "POLARIS_ReturnField",
            "POLARIS_StarWeight",
            "POLARIS_GravityDial",
            "POLARIS_ConstellationBridge",
            "POLARIS_MemoryBell",
            "POLARIS_ImmutableStarBlock",
        };

        public static IReadOnlyList<MapElementDefinition> EnsureCatalog(bool overwriteExisting = false)
        {
            AssetPathUtility.EnsureFolder(CatalogFolder);
            var definitions = new List<MapElementDefinition>(CatalogIds.Length);
            for (var index = 0; index < CatalogIds.Length; index++)
            {
                var id = CatalogIds[index];
                var path = GetAuthoringPath(id);
                var definition = AssetDatabase.LoadAssetAtPath<MapElementDefinition>(path);
                if (definition == null)
                {
                    definition = ScriptableObject.CreateInstance<MapElementDefinition>();
                    Configure(definition, id);
                    definition.name = id;
                    AssetDatabase.CreateAsset(definition, path);
                }
                else if (overwriteExisting)
                {
                    Undo.RecordObject(definition, $"Refresh {id}");
                    Configure(definition, id);
                    definition.name = id;
                    EditorUtility.SetDirty(definition);
                }
                definitions.Add(definition);
            }
            AssetDatabase.SaveAssets();
            return definitions;
        }

        public static CommonElementCatalogBakeReport BakeCatalog(bool overwriteExisting = false)
        {
            var report = new CommonElementCatalogBakeReport();
            var definitions = EnsureCatalog(overwriteExisting);
            for (var index = 0; index < definitions.Count; index++)
            {
                var definition = definitions[index];
                report.Definitions.Add(definition);
                var result = MapElementBakePipeline.Bake(definition);
                report.Results.Add(result);
                if (result.Success) report.SuccessCount++;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return report;
        }

        public static string GetAuthoringPath(string elementId) => $"{CatalogFolder}/{elementId}.asset";

        public static void Configure(MapElementDefinition definition, string id)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            ResetDefinition(definition, id);
            switch (id)
            {
                case "POLARIS_OrbitPlatform": ConfigureOrbitPlatform(definition); break;
                case "POLARIS_ObservationBeam": ConfigureObservationBeam(definition); break;
                case "POLARIS_ReturnField": ConfigureReturnField(definition); break;
                case "POLARIS_StarWeight": ConfigureStarWeight(definition); break;
                case "POLARIS_GravityDial": ConfigureGravityDial(definition); break;
                case "POLARIS_ConstellationBridge": ConfigureConstellationBridge(definition); break;
                case "POLARIS_MemoryBell": ConfigureMemoryBell(definition); break;
                case "POLARIS_ImmutableStarBlock": ConfigureImmutableStarBlock(definition); break;
                default: throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown Polaris element.");
            }
        }

        private static void ResetDefinition(MapElementDefinition definition, string id)
        {
            definition.ElementId = id;
            definition.DisplayName = id;
            definition.Category = ElementCategory.Utility;
            definition.AllowedRegions = RegionMask.Polaris;
            definition.RuntimePrefab = null;
            definition.BakedVisualProfile = null;
            definition.Footprint = new CellFootprint();
            definition.VisualProfile = new ElementVisualProfile
            {
                RenderMode = ElementVisualRenderMode.SingleSprite,
                VisualSizeCells = Vector2.one,
                SortingLayerName = "Default",
                Tint = Color.white,
            };
            definition.CollisionProfile = new ElementCollisionProfile { Mode = ColliderAuthoringMode.MergedBoxes };
            definition.BehaviorProfile = new ElementBehaviorProfile
            {
                InitialState = MapElementState.Idle,
                PersistBrokenState = true,
                PauseWhenRoomInactive = true,
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
        }

        private static void ConfigureOrbitPlatform(MapElementDefinition d)
        {
            d.DisplayName = "Orbit Platform";
            d.Category = ElementCategory.Platform;
            d.PolarisProfile.Kind = PolarisElementKind.OrbitPlatform;
            d.PolarisProfile.PlatformWidthCells = 2;
            d.PolarisProfile.OrbitRadiusCells = new Vector2(3f, 2f);
            d.PolarisProfile.OrbitPeriodSeconds = 4f;
            d.PolarisProfile.DialOrbitMultiplier = 0.65f;
            d.PolarisProfile.KeepOrbitInsideCamera = true;
            d.PlacementProfile.RequiredNeighborTags.Add("OrbitPathInsideCameraBounds");
            SetFootprint(d, 2, 1);
            AddSolid(d, new Vector2(0.5f, -0.08f), new Vector2(1.94f, 0.5f));
            d.BudgetProfile.ThreatCost = 2;
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.MotionCost = 2;
            d.VisualProfile.Tint = new Color(0.48f, 0.74f, 1f);
        }

        private static void ConfigureObservationBeam(MapElementDefinition d)
        {
            d.DisplayName = "Observation Beam";
            d.Category = ElementCategory.Hazard;
            d.PolarisProfile.Kind = PolarisElementKind.ObservationBeam;
            d.PolarisProfile.BeamRangeCells = 8f;
            d.PolarisProfile.SweepDegrees = 90f;
            d.PolarisProfile.SweepPeriodSeconds = 3f;
            d.PolarisProfile.Damage = 1;
            d.PolarisProfile.AppliesReturnMark = true;
            d.PolarisProfile.MirrorCanReflect = true;
            d.PolarisProfile.UmbrellaCanReflect = false;
            d.PolarisProfile.SignalChangesDirection = true;
            d.BehaviorProfile.WarningSeconds = 0.2f;
            SetFootprint(d, 1, 1, hazard: true, trigger: true);
            AddSolid(d, Vector2.zero, new Vector2(0.68f, 0.68f));
            AddTrigger(d, Vector2.zero, new Vector2(0.96f, 0.96f));
            d.BudgetProfile.ThreatCost = 3;
            d.BudgetProfile.UtilityValue = 2;
            d.BudgetProfile.MotionCost = 1;
            d.VisualProfile.Tint = new Color(0.48f, 0.9f, 1f);
        }

        private static void ConfigureReturnField(MapElementDefinition d)
        {
            d.DisplayName = "Return Field";
            d.Category = ElementCategory.Control;
            d.PolarisProfile.Kind = PolarisElementKind.ReturnField;
            d.PolarisProfile.ReturnFieldSizeCells = new Vector2(4f, 2f);
            d.PolarisProfile.ReturnDelaySeconds = 0.5f;
            d.PolarisProfile.DestinationAnchorId = "EntryAnchor";
            d.PolarisProfile.RequiresEntryAnchor = true;
            d.PlacementProfile.RequiredNeighborTags.Add("EntryAnchorRequired");
            SetFootprint(d, 4, 2, trigger: true);
            AddTrigger(d, new Vector2(1.5f, 0.5f), new Vector2(3.96f, 1.96f));
            d.BudgetProfile.ThreatCost = 3;
            d.BudgetProfile.CognitiveCost = 2;
            d.VisualProfile.Tint = new Color(0.42f, 0.34f, 0.78f, 0.8f);
        }

        private static void ConfigureStarWeight(MapElementDefinition d)
        {
            d.DisplayName = "Star Weight";
            d.Category = ElementCategory.Platform;
            d.PolarisProfile.Kind = PolarisElementKind.StarWeight;
            d.PolarisProfile.MassTag = "Heavy";
            d.PolarisProfile.Mass = 2f;
            d.PolarisProfile.GravityDirection = Vector2Int.down;
            d.PolarisProfile.CrushDamage = 1;
            d.PolarisProfile.PressureWeight = 2;
            d.PolarisProfile.HeavyCarryAllowed = true;
            d.PolarisProfile.HookPullAllowed = true;
            SetFootprint(d, 1, 1);
            AddSolid(d, Vector2.zero, new Vector2(0.9f, 0.9f));
            AddReaction(d, ToolTag.Context, ElementReactionType.Move, "Active");
            AddReaction(d, ToolTag.Hook, ElementReactionType.Pull, "Active");
            d.BudgetProfile.ThreatCost = 2;
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.MotionCost = 1;
            d.VisualProfile.Tint = new Color(0.78f, 0.72f, 0.96f);
        }

        private static void ConfigureGravityDial(MapElementDefinition d)
        {
            d.DisplayName = "Gravity Dial";
            d.Category = ElementCategory.Control;
            d.PolarisProfile.Kind = PolarisElementKind.GravityDial;
            d.PolarisProfile.LowGravityScale = 0.45f;
            d.PolarisProfile.NormalGravityScale = 1f;
            d.PolarisProfile.StartsLowGravity = false;
            d.PolarisProfile.MaxInstancesPerRoom = 1;
            d.PlacementProfile.RequiredNeighborTags.Add("UniqueGravityDialPerRoom");
            SetFootprint(d, 1, 2, trigger: true);
            AddSolid(d, new Vector2(0f, 0.5f), new Vector2(0.8f, 1.9f));
            AddTrigger(d, new Vector2(0f, 0.5f), new Vector2(0.96f, 1.96f));
            AddReaction(d, ToolTag.Context, ElementReactionType.Toggle, "Active");
            AddReaction(d, ToolTag.Hook, ElementReactionType.Toggle, "Active");
            d.BudgetProfile.UtilityValue = 4;
            d.BudgetProfile.CognitiveCost = 2;
            d.VisualProfile.Tint = new Color(0.62f, 0.72f, 1f);
        }

        private static void ConfigureConstellationBridge(MapElementDefinition d)
        {
            d.DisplayName = "Constellation Bridge Node";
            d.Category = ElementCategory.Control;
            d.PolarisProfile.Kind = PolarisElementKind.ConstellationBridge;
            d.PolarisProfile.NodeGuids = new List<string> { "POLARIS_NODE_A", "POLARIS_NODE_B" };
            d.PolarisProfile.BridgeCellCount = 6;
            d.PolarisProfile.StartsBridgeActive = false;
            SetFootprint(d, 1, 1, trigger: true);
            AddSolid(d, Vector2.zero, new Vector2(0.7f, 0.7f));
            AddTrigger(d, Vector2.zero, new Vector2(1f, 1f));
            AddReaction(d, ToolTag.Context, ElementReactionType.SetState, "Active");
            d.BudgetProfile.UtilityValue = 4;
            d.BudgetProfile.CognitiveCost = 3;
            d.VisualProfile.Tint = new Color(0.56f, 0.9f, 1f);
        }

        private static void ConfigureMemoryBell(MapElementDefinition d)
        {
            d.DisplayName = "Memory Bell";
            d.Category = ElementCategory.Event;
            d.PolarisProfile.Kind = PolarisElementKind.MemoryBell;
            d.PolarisProfile.RhythmPattern = new List<int> { 0, 1, 0, 2 };
            d.PolarisProfile.MemoryId = "memory.narae.bell";
            d.PolarisProfile.InteractionClearanceCells = 3;
            d.PlacementProfile.ForbiddenNeighborTags.Add("OtherXInteractionWithin3Cells");
            SetFootprint(d, 1, 2, trigger: true);
            AddSolid(d, new Vector2(0f, 0.5f), new Vector2(0.72f, 1.8f));
            AddTrigger(d, new Vector2(0f, 0.5f), new Vector2(0.96f, 1.96f));
            AddReaction(d, ToolTag.Context, ElementReactionType.SetState, "Active");
            d.BudgetProfile.UtilityValue = 4;
            d.BudgetProfile.CognitiveCost = 3;
            d.VisualProfile.Tint = new Color(0.88f, 0.78f, 1f);
        }

        private static void ConfigureImmutableStarBlock(MapElementDefinition d)
        {
            d.DisplayName = "Immutable Star Block";
            d.Category = ElementCategory.Terrain;
            d.PolarisProfile.Kind = PolarisElementKind.ImmutableStarBlock;
            d.PolarisProfile.IgnoreAllTools = true;
            d.PolarisProfile.VisualVariant = "PolarisImmutable";
            SetFootprint(d, 1, 1);
            AddSolid(d, Vector2.zero, Vector2.one);
            d.BudgetProfile.UtilityValue = 1;
            d.VisualProfile.Tint = new Color(0.34f, 0.4f, 0.66f);
        }

        private static void SetFootprint(MapElementDefinition definition, int width, int height,
            bool hazard = false, bool trigger = false)
        {
            definition.Footprint.BoundsSize = new Vector2Int(width, height);
            definition.Footprint.PivotCell = Vector2Int.zero;
            definition.Footprint.OccupiedCells.Clear();
            definition.Footprint.HazardCells.Clear();
            definition.Footprint.TriggerCells.Clear();
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var cell = new Vector2Int(x, y);
                definition.Footprint.OccupiedCells.Add(cell);
                if (hazard) definition.Footprint.HazardCells.Add(cell);
                if (trigger) definition.Footprint.TriggerCells.Add(cell);
            }
            definition.VisualProfile.VisualSizeCells = new Vector2(width, height);
            definition.VisualProfile.VisualOffsetCells = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
        }

        private static void AddSolid(MapElementDefinition definition, Vector2 offset, Vector2 size)
        {
            definition.CollisionProfile.IsSolid = true;
            definition.CollisionProfile.SolidShapes.Add(new SerializedColliderShape
            {
                ShapeType = SerializedColliderShapeType.Box,
                OffsetCells = offset,
                SizeCells = size,
            });
        }

        private static void AddTrigger(MapElementDefinition definition, Vector2 offset, Vector2 size)
        {
            definition.CollisionProfile.TriggerShapes.Add(new SerializedColliderShape
            {
                ShapeType = SerializedColliderShapeType.Box,
                OffsetCells = offset,
                SizeCells = size,
            });
        }

        private static void AddReaction(MapElementDefinition definition, ToolTag tool,
            ElementReactionType reaction, string resultState)
        {
            definition.ToolReactions.Entries.Add(new ToolReactionEntry
            {
                Tool = tool,
                Reaction = reaction,
                StrengthRequired = 1,
                ResultState = resultState,
            });
        }
    }
}

#endif
