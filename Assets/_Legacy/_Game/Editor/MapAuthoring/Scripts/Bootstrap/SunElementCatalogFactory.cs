#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Map;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    public static class SunElementCatalogFactory
    {
        public const string CatalogFolder =
            "Assets/_Game/Editor/MapAuthoring/SourceElements/Sun";

        public static readonly string[] CatalogIds =
        {
            "SUN_RotatingSunbeam",
            "SUN_ShadowSeed",
            "SUN_SunflowerPlatform",
            "SUN_GrowthVine",
            "SUN_DewDrop",
            "SUN_OverheatPlatform",
            "SUN_SunsetFlower",
            "SUN_CrowPerch",
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
                case "SUN_RotatingSunbeam": ConfigureRotatingSunbeam(definition); break;
                case "SUN_ShadowSeed": ConfigureShadowSeed(definition); break;
                case "SUN_SunflowerPlatform": ConfigureSunflowerPlatform(definition); break;
                case "SUN_GrowthVine": ConfigureGrowthVine(definition); break;
                case "SUN_DewDrop": ConfigureDewDrop(definition); break;
                case "SUN_OverheatPlatform": ConfigureOverheatPlatform(definition); break;
                case "SUN_SunsetFlower": ConfigureSunsetFlower(definition); break;
                case "SUN_CrowPerch": ConfigureCrowPerch(definition); break;
                default: throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown Sun element.");
            }
        }

        private static void ResetDefinition(MapElementDefinition definition, string id)
        {
            definition.ElementId = id;
            definition.DisplayName = id;
            definition.Category = ElementCategory.Utility;
            definition.AllowedRegions = RegionMask.Sun;
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

        private static void ConfigureRotatingSunbeam(MapElementDefinition d)
        {
            d.DisplayName = "회전 해살";
            d.Category = ElementCategory.Hazard;
            d.SunProfile.Kind = SunElementKind.RotatingSunbeam;
            d.SunProfile.ArcDegrees = 120f;
            d.SunProfile.RotationSpeedDegreesPerSecond = 60f;
            d.SunProfile.CycleOnSeconds = 2f;
            d.SunProfile.CycleOffSeconds = 1f;
            d.SunProfile.Damage = 1;
            d.SunProfile.CausesOverheat = true;
            d.SunProfile.IgnoreSolidBlockers = true;
            d.SunProfile.IgnoreUmbrellaBlock = true;
            d.SunProfile.RotateOnSignal = true;
            d.SunProfile.PreventFullOverheatOverlap = true;
            d.BehaviorProfile.WarningSeconds = 0.2f;
            d.PlacementProfile.Surface = SurfaceRequirement.Wall;
            d.PlacementProfile.ForbiddenNeighborTags.Add("FullCycleOverlapWithOverheatPlatform");
            SetFootprint(d, 1, 1, hazard: true, trigger: true);
            AddSolid(d, Vector2.zero, new Vector2(0.68f, 0.68f));
            AddTrigger(d, Vector2.zero, new Vector2(0.96f, 0.96f));
            d.BudgetProfile.ThreatCost = 3;
            d.BudgetProfile.UtilityValue = 2;
            d.BudgetProfile.MotionCost = 1;
            d.VisualProfile.Tint = new Color(1f, 0.72f, 0.18f);
        }

        private static void ConfigureShadowSeed(MapElementDefinition d)
        {
            d.DisplayName = "그림자 씨앗";
            d.Category = ElementCategory.Utility;
            d.SunProfile.Kind = SunElementKind.ShadowSeed;
            d.SunProfile.ShadowSizeCells = new Vector2(2f, 2f);
            d.SunProfile.ShadowRadiusCells = 1f;
            d.SunProfile.ShadowLifetimeSeconds = 6f;
            d.SunProfile.WaterSuppressesShadow = true;
            d.SunProfile.KeepExitMarkersVisible = true;
            d.PlacementProfile.RequiredNeighborTags.Add("ExitMarkerVisibleInShadow");
            SetFootprint(d, 1, 1, trigger: true);
            AddTrigger(d, new Vector2(0.5f, 0.5f), new Vector2(2f, 2f));
            AddReaction(d, ToolTag.Water, ElementReactionType.Disable, "Disabled");
            d.BudgetProfile.ThreatCost = 1;
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.CognitiveCost = 2;
            d.VisualProfile.Tint = new Color(0.3f, 0.22f, 0.48f, 0.85f);
        }

        private static void ConfigureSunflowerPlatform(MapElementDefinition d)
        {
            d.DisplayName = "햇꽃 발판";
            d.Category = ElementCategory.Platform;
            d.SunProfile.Kind = SunElementKind.SunflowerPlatform;
            d.SunProfile.PlatformWidthCells = 2;
            d.SunProfile.PlatformRotationStepDegrees = 90;
            d.SunProfile.LightSourceRef = "RoomSun";
            d.SunProfile.BloomsInLight = true;
            d.SunProfile.ClosesOnOverheat = true;
            d.PlacementProfile.Surface = SurfaceRequirement.FloorOrOneWay;
            SetFootprint(d, 2, 1);
            AddSolid(d, new Vector2(0.5f, -0.08f), new Vector2(1.92f, 0.5f));
            d.BudgetProfile.ThreatCost = 1;
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.MotionCost = 1;
            d.VisualProfile.Tint = new Color(1f, 0.76f, 0.24f);
        }

        private static void ConfigureGrowthVine(MapElementDefinition d)
        {
            d.DisplayName = "성장 덩굴";
            d.Category = ElementCategory.Platform;
            d.SunProfile.Kind = SunElementKind.GrowthVine;
            d.SunProfile.StartLengthCells = 1;
            d.SunProfile.MaxLengthCells = 6;
            d.SunProfile.GrowthDirection = Vector2Int.up;
            d.SunProfile.StopAtUnbreakableBoundary = true;
            d.PlacementProfile.Surface = SurfaceRequirement.Floor;
            d.PlacementProfile.ForbiddenNeighborTags.Add("UnbreakableBoundaryInGrowthPath");
            SetFootprint(d, 1, 6, trigger: true);
            AddSolid(d, new Vector2(0f, 2.5f), new Vector2(0.46f, 5.96f));
            AddTrigger(d, new Vector2(0f, 2.5f), new Vector2(0.72f, 5.96f));
            AddReaction(d, ToolTag.Water, ElementReactionType.SetState, "Active");
            AddReaction(d, ToolTag.Pickaxe, ElementReactionType.Break, "Broken");
            AddReaction(d, ToolTag.Shovel, ElementReactionType.Break, "Broken");
            AddReaction(d, ToolTag.Hook, ElementReactionType.Pull, "Active");
            d.BudgetProfile.ThreatCost = 1;
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.CognitiveCost = 2;
            d.VisualProfile.RenderMode = ElementVisualRenderMode.TiledSprite;
            d.VisualProfile.Tint = new Color(0.34f, 0.7f, 0.28f);
        }

        private static void ConfigureDewDrop(MapElementDefinition d)
        {
            d.DisplayName = "이슬방울";
            d.Category = ElementCategory.Utility;
            d.SunProfile.Kind = SunElementKind.DewDrop;
            d.SunProfile.FallIntervalSeconds = 2.5f;
            d.SunProfile.CoolOnImpact = true;
            d.SunProfile.CanFullyRefillWateringCan = true;
            d.SunProfile.ThrownWaterMagnitude = 1f;
            d.PlacementProfile.Surface = SurfaceRequirement.Ceiling;
            SetFootprint(d, 1, 1);
            AddSolid(d, Vector2.zero, new Vector2(0.48f, 0.48f));
            AddReaction(d, ToolTag.Context, ElementReactionType.SetState, "Active");
            d.BudgetProfile.ThreatCost = 1;
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.MotionCost = 1;
            d.VisualProfile.Tint = new Color(0.48f, 0.84f, 1f);
        }

        private static void ConfigureOverheatPlatform(MapElementDefinition d)
        {
            d.DisplayName = "과열 발판";
            d.Category = ElementCategory.Hazard;
            d.SunProfile.Kind = SunElementKind.OverheatPlatform;
            d.SunProfile.OverheatPlatformWidthCells = 2;
            d.SunProfile.SafeSeconds = 2f;
            d.SunProfile.OverheatSeconds = 1f;
            d.SunProfile.OverheatWarningSeconds = 0.25f;
            d.SunProfile.WaterCoolSeconds = 3f;
            d.SunProfile.Damage = 1;
            d.SunProfile.PreventFullSunbeamOverlap = true;
            d.BehaviorProfile.WarningSeconds = 0.25f;
            d.PlacementProfile.Surface = SurfaceRequirement.FloorOrOneWay;
            d.PlacementProfile.ForbiddenNeighborTags.Add("FullCycleOverlapWithSunbeam");
            SetFootprint(d, 2, 1, hazard: true, trigger: true);
            AddSolid(d, new Vector2(0.5f, -0.12f), new Vector2(1.94f, 0.5f));
            AddTrigger(d, new Vector2(0.5f, 0.16f), new Vector2(1.9f, 0.5f));
            AddReaction(d, ToolTag.Water, ElementReactionType.Disable, "Disabled");
            d.BudgetProfile.ThreatCost = 2;
            d.BudgetProfile.UtilityValue = 2;
            d.BudgetProfile.CognitiveCost = 1;
            d.VisualProfile.Tint = new Color(0.92f, 0.42f, 0.16f);
        }

        private static void ConfigureSunsetFlower(MapElementDefinition d)
        {
            d.DisplayName = "해넘이 꽃";
            d.Category = ElementCategory.Control;
            d.SunProfile.Kind = SunElementKind.SunsetFlower;
            d.SunProfile.InitialPhase = SunPhase.Day;
            SetFootprint(d, 2, 2, trigger: true);
            AddTrigger(d, new Vector2(0.5f, 0.5f), new Vector2(1.92f, 1.92f));
            d.BudgetProfile.ThreatCost = 1;
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.CognitiveCost = 2;
            d.VisualProfile.Tint = new Color(0.96f, 0.52f, 0.28f);
        }

        private static void ConfigureCrowPerch(MapElementDefinition d)
        {
            d.DisplayName = "삼족오 횃대";
            d.Category = ElementCategory.Event;
            d.SunProfile.Kind = SunElementKind.CrowPerch;
            d.SunProfile.EventId = "sun.crow.rescue";
            d.SunProfile.AcceptedContextIds = new List<string> { "letter", "sun_ember" };
            d.PlacementProfile.Surface = SurfaceRequirement.Floor;
            SetFootprint(d, 2, 1, trigger: true);
            AddSolid(d, new Vector2(0.5f, -0.18f), new Vector2(1.92f, 0.32f));
            AddTrigger(d, new Vector2(0.5f, 0.12f), new Vector2(1.86f, 0.62f));
            AddReaction(d, ToolTag.Context, ElementReactionType.SetState, "Active");
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.CognitiveCost = 1;
            d.VisualProfile.Tint = new Color(0.34f, 0.24f, 0.16f);
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
