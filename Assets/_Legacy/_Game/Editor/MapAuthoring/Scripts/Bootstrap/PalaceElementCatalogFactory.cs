#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Map;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    public static class PalaceElementCatalogFactory
    {
        public const string CatalogFolder =
            "Assets/_Game/Editor/MapAuthoring/SourceElements/Palace";

        public static readonly string[] CatalogIds =
        {
            "PALACE_SluiceGate",
            "PALACE_BubbleCannon",
            "PALACE_CurrentVolume",
            "PALACE_TurtlePlatform",
            "PALACE_ClamBounce",
            "PALACE_WaterMirrorWall",
            "PALACE_DrainGrate",
            "PALACE_DragonGateWaterfall",
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
                case "PALACE_SluiceGate": ConfigureSluiceGate(definition); break;
                case "PALACE_BubbleCannon": ConfigureBubbleCannon(definition); break;
                case "PALACE_CurrentVolume": ConfigureCurrentVolume(definition); break;
                case "PALACE_TurtlePlatform": ConfigureTurtlePlatform(definition); break;
                case "PALACE_ClamBounce": ConfigureClamBounce(definition); break;
                case "PALACE_WaterMirrorWall": ConfigureWaterMirrorWall(definition); break;
                case "PALACE_DrainGrate": ConfigureDrainGrate(definition); break;
                case "PALACE_DragonGateWaterfall": ConfigureDragonGateWaterfall(definition); break;
                default: throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown Palace element.");
            }
        }

        private static void ResetDefinition(MapElementDefinition definition, string id)
        {
            definition.ElementId = id;
            definition.DisplayName = id;
            definition.Category = ElementCategory.Utility;
            definition.AllowedRegions = RegionMask.Palace;
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

        private static void ConfigureSluiceGate(MapElementDefinition d)
        {
            d.DisplayName = "용궁 수문";
            d.Category = ElementCategory.Door;
            d.PalaceProfile.Kind = PalaceElementKind.SluiceGate;
            d.PalaceProfile.WidthCells = 1;
            d.PalaceProfile.HeightCells = 3;
            d.PalaceProfile.MoveSpeedCellsPerSecond = 2f;
            d.PalaceProfile.PreventPermanentLock = true;
            d.PlacementProfile.RequiredNeighborTags.Add("NonLockingAlternateRoute");
            SetFootprint(d, 1, 3);
            AddSolid(d, new Vector2(0f, 1f), new Vector2(0.94f, 2.96f));
            AddReaction(d, ToolTag.Hook, ElementReactionType.Toggle, "Active");
            d.BudgetProfile.ThreatCost = 2;
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.MotionCost = 1;
            d.VisualProfile.Tint = new Color(0.32f, 0.66f, 0.78f);
        }

        private static void ConfigureBubbleCannon(MapElementDefinition d)
        {
            d.DisplayName = "수포 분사기";
            d.Category = ElementCategory.Vent;
            d.PalaceProfile.Kind = PalaceElementKind.BubbleCannon;
            d.PalaceProfile.Direction = Vector2Int.right;
            d.PalaceProfile.IntervalSeconds = 1.8f;
            d.PalaceProfile.ProjectileSpeedCellsPerSecond = 5f;
            d.PalaceProfile.UmbrellaPushMultiplier = 0.5f;
            d.PlacementProfile.Surface = SurfaceRequirement.Wall;
            SetFootprint(d, 1, 2);
            AddSolid(d, new Vector2(0f, 0.25f), new Vector2(0.9f, 1.45f));
            AddReaction(d, ToolTag.WindGuard, ElementReactionType.SetState, "Active");
            d.BudgetProfile.ThreatCost = 1;
            d.BudgetProfile.UtilityValue = 2;
            d.BudgetProfile.MotionCost = 1;
            d.VisualProfile.Tint = new Color(0.48f, 0.82f, 0.96f);
        }

        private static void ConfigureCurrentVolume(MapElementDefinition d)
        {
            d.DisplayName = "급류";
            d.Category = ElementCategory.Vent;
            d.PalaceProfile.Kind = PalaceElementKind.CurrentVolume;
            d.PalaceProfile.VolumeSizeCells = new Vector2(4f, 2f);
            d.PalaceProfile.Direction = Vector2Int.right;
            d.PalaceProfile.ForceCellsPerSecond = 8f;
            d.PalaceProfile.Falloff = 0.25f;
            d.PalaceProfile.HeavyBlockMultiplier = 0.35f;
            d.PalaceProfile.ExitSafePocketCells = 2;
            d.PlacementProfile.RequiredNeighborTags.Add("ExitSafePocket2Cells");
            SetFootprint(d, 4, 2, trigger: true);
            AddTrigger(d, new Vector2(1.5f, 0.5f), new Vector2(3.96f, 1.96f));
            AddReaction(d, ToolTag.HeavyImpact, ElementReactionType.Disable, "Disabled");
            d.BudgetProfile.ThreatCost = 2;
            d.BudgetProfile.UtilityValue = 2;
            d.BudgetProfile.MotionCost = 1;
            d.VisualProfile.RenderMode = ElementVisualRenderMode.TiledSprite;
            d.VisualProfile.Tint = new Color(0.34f, 0.72f, 0.94f, 0.7f);
        }

        private static void ConfigureTurtlePlatform(MapElementDefinition d)
        {
            d.DisplayName = "자라 등껍질 발판";
            d.Category = ElementCategory.Platform;
            d.PalaceProfile.Kind = PalaceElementKind.TurtlePlatform;
            d.PalaceProfile.SinkDepthCells = 1f;
            d.PalaceProfile.WeightThreshold = 1;
            SetFootprint(d, 2, 1, trigger: true);
            AddSolid(d, new Vector2(0.5f, -0.12f), new Vector2(1.9f, 0.54f));
            AddTrigger(d, new Vector2(0.5f, 0.2f), new Vector2(1.86f, 0.5f));
            d.BudgetProfile.ThreatCost = 1;
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.MotionCost = 1;
            d.VisualProfile.Tint = new Color(0.36f, 0.68f, 0.54f);
        }

        private static void ConfigureClamBounce(MapElementDefinition d)
        {
            d.DisplayName = "조개 탄성대";
            d.Category = ElementCategory.Platform;
            d.PalaceProfile.Kind = PalaceElementKind.ClamBounce;
            d.PalaceProfile.CycleSeconds = 0.8f;
            d.PalaceProfile.LaunchHeightCells = 4f;
            d.PalaceProfile.ReflectProjectiles = true;
            SetFootprint(d, 2, 1, trigger: true);
            AddSolid(d, new Vector2(0.5f, -0.18f), new Vector2(1.9f, 0.46f));
            AddTrigger(d, new Vector2(0.5f, 0.18f), new Vector2(1.86f, 0.5f));
            d.BudgetProfile.ThreatCost = 1;
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.MotionCost = 1;
            d.VisualProfile.Tint = new Color(0.88f, 0.54f, 0.72f);
        }

        private static void ConfigureWaterMirrorWall(MapElementDefinition d)
        {
            d.DisplayName = "물거울 벽";
            d.Category = ElementCategory.Utility;
            d.PalaceProfile.Kind = PalaceElementKind.WaterMirrorWall;
            d.PalaceProfile.HeightCells = 3;
            d.PalaceProfile.NormalDirection = Vector2Int.left;
            d.PalaceProfile.TransparentOnSignal = true;
            d.PalaceProfile.TransparencyContextId = "yeouiju";
            d.PlacementProfile.Surface = SurfaceRequirement.Wall;
            SetFootprint(d, 1, 3);
            AddSolid(d, new Vector2(0f, 1f), new Vector2(0.42f, 2.94f));
            AddReaction(d, ToolTag.Context, ElementReactionType.SetState, "Active");
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.CognitiveCost = 2;
            d.VisualProfile.Tint = new Color(0.56f, 0.92f, 1f, 0.8f);
        }

        private static void ConfigureDrainGrate(MapElementDefinition d)
        {
            d.DisplayName = "배수 격자";
            d.Category = ElementCategory.Control;
            d.PalaceProfile.Kind = PalaceElementKind.DrainGrate;
            d.PalaceProfile.DrainRatePerSecond = 0.5f;
            d.PalaceProfile.StartsMudBlocked = true;
            d.PalaceProfile.KeepVoidRecoveryIndependent = true;
            d.PlacementProfile.Surface = SurfaceRequirement.Floor;
            d.PlacementProfile.RequiredNeighborTags.Add("VoidRecoveryWaterIndependent");
            SetFootprint(d, 2, 1, trigger: true);
            AddSolid(d, new Vector2(0.5f, -0.28f), new Vector2(1.96f, 0.34f));
            AddTrigger(d, new Vector2(0.5f, 0.12f), new Vector2(1.88f, 0.64f));
            AddReaction(d, ToolTag.Shovel, ElementReactionType.SetState, "Active");
            AddReaction(d, ToolTag.Hook, ElementReactionType.Toggle, "Active");
            d.BudgetProfile.ThreatCost = 1;
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.CognitiveCost = 1;
            d.VisualProfile.Tint = new Color(0.42f, 0.54f, 0.58f);
        }

        private static void ConfigureDragonGateWaterfall(MapElementDefinition d)
        {
            d.DisplayName = "용문 폭포";
            d.Category = ElementCategory.Vent;
            d.PalaceProfile.Kind = PalaceElementKind.DragonGateWaterfall;
            d.PalaceProfile.VolumeSizeCells = new Vector2(3f, 4f);
            d.PalaceProfile.ForceCellsPerSecond = 9f;
            d.PalaceProfile.StartsActive = true;
            d.PalaceProfile.UmbrellaLiftMultiplier = 1.4f;
            d.PalaceProfile.CloudSupportMultiplier = 1.5f;
            d.PalaceProfile.CanRefillWateringCan = true;
            d.BehaviorProfile.InitialState = MapElementState.Active;
            SetFootprint(d, 3, 4, trigger: true);
            AddTrigger(d, new Vector2(1f, 1.5f), new Vector2(2.96f, 3.96f));
            AddReaction(d, ToolTag.WindGuard, ElementReactionType.SetState, "Active");
            AddReaction(d, ToolTag.Water, ElementReactionType.SetState, "Active");
            d.BudgetProfile.ThreatCost = 1;
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.MotionCost = 2;
            d.VisualProfile.RenderMode = ElementVisualRenderMode.TiledSprite;
            d.VisualProfile.Tint = new Color(0.34f, 0.74f, 1f, 0.72f);
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
