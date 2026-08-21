#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Map;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    public static class BridgeElementCatalogFactory
    {
        public const string CatalogFolder =
            "Assets/_Game/Editor/MapAuthoring/SourceElements/Bridge";

        public static readonly string[] CatalogIds =
        {
            "BRIDGE_ThreadBridge",
            "BRIDGE_KnotPulley",
            "BRIDGE_WindBanner",
            "BRIDGE_ThreadBlade",
            "BRIDGE_MagpiePlatform",
            "BRIDGE_FeatherUpdraft",
            "BRIDGE_BreakingStarPanel",
            "BRIDGE_Nest",
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

        public static string GetAuthoringPath(string elementId) =>
            $"{CatalogFolder}/{elementId}.asset";

        public static void Configure(MapElementDefinition definition, string id)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            ResetDefinition(definition, id);
            switch (id)
            {
                case "BRIDGE_ThreadBridge": ConfigureThreadBridge(definition); break;
                case "BRIDGE_KnotPulley": ConfigureKnotPulley(definition); break;
                case "BRIDGE_WindBanner": ConfigureWindBanner(definition); break;
                case "BRIDGE_ThreadBlade": ConfigureThreadBlade(definition); break;
                case "BRIDGE_MagpiePlatform": ConfigureMagpiePlatform(definition); break;
                case "BRIDGE_FeatherUpdraft": ConfigureFeatherUpdraft(definition); break;
                case "BRIDGE_BreakingStarPanel": ConfigureBreakingStarPanel(definition); break;
                case "BRIDGE_Nest": ConfigureNest(definition); break;
                default: throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown Bridge element.");
            }
        }

        private static void ResetDefinition(MapElementDefinition definition, string id)
        {
            definition.ElementId = id;
            definition.DisplayName = id;
            definition.Category = ElementCategory.Utility;
            definition.AllowedRegions = RegionMask.Bridge;
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
            definition.CollisionProfile = new ElementCollisionProfile
            {
                Mode = ColliderAuthoringMode.MergedBoxes,
            };
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

        private static void ConfigureThreadBridge(MapElementDefinition d)
        {
            d.DisplayName = "실다리";
            d.Category = ElementCategory.Platform;
            d.BridgeProfile.Kind = BridgeElementKind.ThreadBridge;
            d.BridgeProfile.LengthCells = 4;
            d.BridgeProfile.MaxWeight = 2;
            d.BridgeProfile.SagCells = 0.3f;
            d.PlacementProfile.Surface = SurfaceRequirement.Any;
            d.PlacementProfile.RequiredNeighborTags.Add("AlternativeRouteOrVoidRecovery");
            SetFootprint(d, 4, 1, trigger: true);
            AddSolid(d, new Vector2(1.5f, -0.15f), new Vector2(3.96f, 0.48f));
            AddTrigger(d, new Vector2(1.5f, 0.2f), new Vector2(3.9f, 0.52f));
            AddReaction(d, ToolTag.Pickaxe | ToolTag.Bomb, ElementReactionType.Break, "Broken");
            d.BudgetProfile.ThreatCost = 2;
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.CognitiveCost = 1;
            d.VisualProfile.RenderMode = ElementVisualRenderMode.TiledSprite;
            d.VisualProfile.Tint = new Color(0.86f, 0.78f, 0.92f);
        }

        private static void ConfigureKnotPulley(MapElementDefinition d)
        {
            d.DisplayName = "매듭 도르래";
            d.Category = ElementCategory.Platform;
            d.BridgeProfile.Kind = BridgeElementKind.KnotPulley;
            d.BridgeProfile.TravelCells = 4f;
            d.BridgeProfile.WeightRatio = 1f;
            SetFootprint(d, 2, 2, trigger: true);
            AddSolid(d, new Vector2(0.5f, -0.18f), new Vector2(1.96f, 0.48f));
            AddSolid(d, new Vector2(0f, 0.5f), new Vector2(0.32f, 1.94f));
            AddTrigger(d, new Vector2(0.5f, 0.15f), new Vector2(1.9f, 0.52f));
            AddReaction(d, ToolTag.Hook, ElementReactionType.Toggle, "Active");
            AddReaction(d, ToolTag.HeavyImpact, ElementReactionType.Move, "Active");
            d.BudgetProfile.ThreatCost = 1;
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.MotionCost = 2;
            d.VisualProfile.Tint = new Color(0.72f, 0.58f, 0.84f);
        }

        private static void ConfigureWindBanner(MapElementDefinition d)
        {
            d.DisplayName = "바람깃발";
            d.Category = ElementCategory.Control;
            d.BridgeProfile.Kind = BridgeElementKind.WindBanner;
            d.BridgeProfile.Direction = Vector2Int.right;
            d.BridgeProfile.FlipOnSignal = true;
            d.BridgeProfile.WetForceMultiplier = 0.5f;
            d.BridgeProfile.UmbrellaAssistMultiplier = 1.25f;
            d.PlacementProfile.Surface = SurfaceRequirement.Floor;
            SetFootprint(d, 1, 2);
            AddSolid(d, new Vector2(0f, 0.25f), new Vector2(0.34f, 1.45f));
            AddReaction(d, ToolTag.Water, ElementReactionType.SetState, "Active");
            AddReaction(d, ToolTag.WindGuard, ElementReactionType.SetState, "Active");
            d.BudgetProfile.UtilityValue = 2;
            d.VisualProfile.Tint = new Color(0.56f, 0.82f, 0.94f);
        }

        private static void ConfigureThreadBlade(MapElementDefinition d)
        {
            d.DisplayName = "실칼날";
            d.Category = ElementCategory.Hazard;
            d.BridgeProfile.Kind = BridgeElementKind.ThreadBlade;
            d.BridgeProfile.PathSpeedCellsPerSecond = 3f;
            d.BridgeProfile.WarningSeconds = 0.35f;
            d.BridgeProfile.MinimumStrongCrosswindDistanceCells = 6;
            d.BehaviorProfile.WarningSeconds = 0.35f;
            d.BehaviorProfile.Path.Nodes.Add(Vector2.zero);
            d.BehaviorProfile.Path.Nodes.Add(new Vector2(4f, 0f));
            d.BehaviorProfile.Path.SpeedCellsPerSecond = 3f;
            d.BehaviorProfile.Path.PingPong = true;
            d.PlacementProfile.ForbiddenNeighborTags.Add("StrongCrosswindWithin6Cells");
            SetFootprint(d, 1, 1, hazard: true, trigger: true);
            AddTrigger(d, Vector2.zero, new Vector2(0.82f, 0.82f));
            d.BudgetProfile.ThreatCost = 3;
            d.BudgetProfile.UtilityValue = 2;
            d.BudgetProfile.MotionCost = 2;
            d.VisualProfile.Tint = new Color(0.94f, 0.44f, 0.64f);
        }

        private static void ConfigureMagpiePlatform(MapElementDefinition d)
        {
            d.DisplayName = "까치 발판";
            d.Category = ElementCategory.Platform;
            d.BridgeProfile.Kind = BridgeElementKind.MagpiePlatform;
            d.BridgeProfile.PlatformWidthCells = 2;
            d.BridgeProfile.StopCount = 2;
            d.BridgeProfile.WaitTimeSeconds = 0.75f;
            d.BridgeProfile.HeavyDescentMultiplier = 2f;
            d.BehaviorProfile.Path.Nodes.Add(Vector2.zero);
            d.BehaviorProfile.Path.Nodes.Add(new Vector2(4f, 0f));
            d.BehaviorProfile.Path.SpeedCellsPerSecond = 2f;
            d.BehaviorProfile.Path.WaitSeconds = 0.75f;
            d.BehaviorProfile.Path.PingPong = true;
            d.PlacementProfile.RequiredNeighborTags.Add("BaseRouteFallback");
            SetFootprint(d, 2, 1, trigger: true);
            AddSolid(d, new Vector2(0.5f, -0.15f), new Vector2(1.94f, 0.46f));
            AddTrigger(d, new Vector2(0.5f, 0.18f), new Vector2(1.88f, 0.5f));
            AddReaction(d, ToolTag.HeavyImpact, ElementReactionType.Move, "Active");
            d.BudgetProfile.ThreatCost = 1;
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.MotionCost = 2;
            d.VisualProfile.Tint = new Color(0.42f, 0.62f, 0.82f);
        }

        private static void ConfigureFeatherUpdraft(MapElementDefinition d)
        {
            d.DisplayName = "별깃털 상승류";
            d.Category = ElementCategory.Vent;
            d.BridgeProfile.Kind = BridgeElementKind.FeatherUpdraft;
            d.BridgeProfile.VolumeSizeCells = new Vector2(2f, 4f);
            d.BridgeProfile.ForceCellsPerSecond = 8f;
            d.BridgeProfile.UmbrellaLiftMultiplier = 1.5f;
            d.PlacementProfile.Surface = SurfaceRequirement.Floor;
            SetFootprint(d, 1, 1, trigger: true);
            AddSolid(d, new Vector2(0f, -0.36f), new Vector2(0.92f, 0.22f));
            AddTrigger(d, new Vector2(0.5f, 1.5f), new Vector2(2f, 4f));
            AddReaction(d, ToolTag.WindGuard, ElementReactionType.SetState, "Active");
            d.BudgetProfile.ThreatCost = 1;
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.MotionCost = 1;
            d.VisualProfile.Tint = new Color(0.80f, 0.90f, 1f);
        }

        private static void ConfigureBreakingStarPanel(MapElementDefinition d)
        {
            d.DisplayName = "끊어지는 별판";
            d.Category = ElementCategory.Platform;
            d.BridgeProfile.Kind = BridgeElementKind.BreakingStarPanel;
            d.BridgeProfile.HitCount = 2;
            d.BridgeProfile.DwellBreakSeconds = 0.5f;
            d.BehaviorProfile.WarningSeconds = 0.2f;
            d.PlacementProfile.RequiredNeighborTags.Add("AlternativeRouteOrVoidRecovery");
            SetFootprint(d, 1, 1, trigger: true);
            AddSolid(d, new Vector2(0f, -0.17f), new Vector2(0.96f, 0.5f));
            AddTrigger(d, new Vector2(0f, 0.18f), new Vector2(0.9f, 0.5f));
            AddReaction(d, ToolTag.HeavyImpact, ElementReactionType.Break, "Broken");
            d.BudgetProfile.ThreatCost = 2;
            d.BudgetProfile.UtilityValue = 1;
            d.VisualProfile.Tint = new Color(0.88f, 0.82f, 0.46f);
        }

        private static void ConfigureNest(MapElementDefinition d)
        {
            d.DisplayName = "까치 둥지";
            d.Category = ElementCategory.Event;
            d.BridgeProfile.Kind = BridgeElementKind.Nest;
            d.BridgeProfile.RequiredPieces = 3;
            d.BridgeProfile.SupportRewardId = "magpie_next_room_support";
            d.BridgeProfile.CriticalObject = true;
            d.PlacementProfile.Surface = SurfaceRequirement.Floor;
            d.PlacementProfile.AllowMainRoute = false;
            SetFootprint(d, 2, 2, trigger: true);
            AddSolid(d, new Vector2(0.5f, 0.1f), new Vector2(1.88f, 1.2f));
            AddTrigger(d, new Vector2(0.5f, 0.5f), new Vector2(1.94f, 1.94f));
            AddReaction(d, ToolTag.Context, ElementReactionType.SetState, "Active");
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.CognitiveCost = 2;
            d.VisualProfile.Tint = new Color(0.62f, 0.48f, 0.34f);
        }

        private static void SetFootprint(
            MapElementDefinition definition,
            int width,
            int height,
            bool hazard = false,
            bool trigger = false)
        {
            definition.Footprint.BoundsSize = new Vector2Int(width, height);
            definition.Footprint.PivotCell = Vector2Int.zero;
            definition.Footprint.OccupiedCells.Clear();
            definition.Footprint.HazardCells.Clear();
            definition.Footprint.TriggerCells.Clear();
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    definition.Footprint.OccupiedCells.Add(cell);
                    if (hazard) definition.Footprint.HazardCells.Add(cell);
                    if (trigger) definition.Footprint.TriggerCells.Add(cell);
                }
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

        private static void AddReaction(
            MapElementDefinition definition,
            ToolTag tool,
            ElementReactionType reaction,
            string resultState)
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
