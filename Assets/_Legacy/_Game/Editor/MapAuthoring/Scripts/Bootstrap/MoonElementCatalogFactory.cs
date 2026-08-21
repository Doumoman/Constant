#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Map;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    public static class MoonElementCatalogFactory
    {
        public const string CatalogFolder =
            "Assets/_Game/Editor/MapAuthoring/SourceElements/Moon";

        public static readonly string[] CatalogIds =
        {
            "MOON_MoonIronBall",
            "MOON_FallingMortar",
            "MOON_DoughPlatform",
            "MOON_CraterSlab",
            "MOON_CassiaRoot",
            "MOON_MillShaft",
            "MOON_MedicineMortar",
            "MOON_FlourVent",
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
                if (result.Success)
                {
                    report.SuccessCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return report;
        }

        public static string GetAuthoringPath(string elementId) =>
            $"{CatalogFolder}/{elementId}.asset";

        public static void Configure(MapElementDefinition definition, string id)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            ResetDefinition(definition, id);
            switch (id)
            {
                case "MOON_MoonIronBall": ConfigureMoonIronBall(definition); break;
                case "MOON_FallingMortar": ConfigureFallingMortar(definition); break;
                case "MOON_DoughPlatform": ConfigureDoughPlatform(definition); break;
                case "MOON_CraterSlab": ConfigureCraterSlab(definition); break;
                case "MOON_CassiaRoot": ConfigureCassiaRoot(definition); break;
                case "MOON_MillShaft": ConfigureMillShaft(definition); break;
                case "MOON_MedicineMortar": ConfigureMedicineMortar(definition); break;
                case "MOON_FlourVent": ConfigureFlourVent(definition); break;
                default: throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown Moon element.");
            }
        }

        private static void ResetDefinition(MapElementDefinition definition, string id)
        {
            definition.ElementId = id;
            definition.DisplayName = id;
            definition.Category = ElementCategory.Utility;
            definition.AllowedRegions = RegionMask.Moon;
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

        private static void ConfigureMoonIronBall(MapElementDefinition d)
        {
            d.DisplayName = "달철구";
            d.Category = ElementCategory.Hazard;
            d.MoonProfile.Kind = MoonElementKind.MoonIronBall;
            d.MoonProfile.ChainLengthCells = 3;
            d.MoonProfile.SwingArcDegrees = 50f;
            d.MoonProfile.SwingPeriodSeconds = 2.6f;
            d.MoonProfile.Damage = 1;
            d.PlacementProfile.Surface = SurfaceRequirement.Ceiling;
            d.PlacementProfile.MinimumSafeCellDistanceCells = 4;
            d.PlacementProfile.ForbiddenNeighborTags.Add("EntrySafeZone");
            SetFootprint(d, 1, 4, hazard: true);
            AddSolid(d, Vector2.zero, new Vector2(0.96f, 0.96f));
            AddReaction(d, ToolTag.Hook, ElementReactionType.Pull, "OrbitPulled");
            d.BudgetProfile.ThreatCost = 3;
            d.BudgetProfile.UtilityValue = 2;
            d.BudgetProfile.MotionCost = 2;
            d.VisualProfile.VisualSizeCells = new Vector2(1f, 4f);
            d.VisualProfile.Tint = new Color(0.62f, 0.68f, 0.82f);
        }

        private static void ConfigureFallingMortar(MapElementDefinition d)
        {
            d.DisplayName = "낙하 절구";
            d.Category = ElementCategory.Hazard;
            d.MoonProfile.Kind = MoonElementKind.FallingMortar;
            d.MoonProfile.FallHeightCells = 5f;
            d.MoonProfile.ShadowWarningSeconds = 0.75f;
            d.MoonProfile.Damage = 1;
            d.BehaviorProfile.WarningSeconds = 0.75f;
            d.PlacementProfile.Surface = SurfaceRequirement.Ceiling;
            d.PlacementProfile.ForbiddenNeighborTags.Add("Crusher");
            SetFootprint(d, 2, 2, hazard: true);
            AddSolid(d, new Vector2(0.5f, 0.5f), new Vector2(1.94f, 1.94f));
            AddReaction(d, ToolTag.Bomb | ToolTag.Pickaxe, ElementReactionType.SetState, "Warning");
            AddReaction(d, ToolTag.Pound | ToolTag.HeavyImpact, ElementReactionType.SetState, "Warning");
            d.BudgetProfile.ThreatCost = 3;
            d.BudgetProfile.UtilityValue = 2;
            d.BudgetProfile.MotionCost = 1;
            d.VisualProfile.VisualSizeCells = new Vector2(2f, 2f);
            d.VisualProfile.VisualOffsetCells = new Vector2(0.5f, 0.5f);
            d.VisualProfile.Tint = new Color(0.58f, 0.60f, 0.72f);
        }

        private static void ConfigureDoughPlatform(MapElementDefinition d)
        {
            d.DisplayName = "달반죽 발판";
            d.Category = ElementCategory.Platform;
            d.MoonProfile.Kind = MoonElementKind.DoughPlatform;
            d.MoonProfile.WidthCells = 2;
            d.MoonProfile.CompressionCells = 0.4f;
            d.MoonProfile.BounceHeightCells = 3f;
            d.MoonProfile.Softness = 0.65f;
            d.PlacementProfile.Surface = SurfaceRequirement.FloorOrOneWay;
            d.PlacementProfile.RequiredNeighborTags.Add("FallLandingOrPuzzleResult");
            SetFootprint(d, 2, 1, trigger: true);
            AddSolid(d, new Vector2(0.5f, -0.25f), new Vector2(1.96f, 0.46f));
            AddTrigger(d, new Vector2(0.5f, 0.18f), new Vector2(1.9f, 0.6f));
            AddReaction(d, ToolTag.Water, ElementReactionType.SetState, "Active");
            AddReaction(d, ToolTag.Pound, ElementReactionType.SetState, "Active");
            AddReaction(d, ToolTag.Bomb, ElementReactionType.Break, "Scattered");
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.CognitiveCost = 1;
            d.VisualProfile.VisualSizeCells = new Vector2(2f, 1f);
            d.VisualProfile.VisualOffsetCells = new Vector2(0.5f, 0f);
            d.VisualProfile.Tint = new Color(0.86f, 0.82f, 0.62f);
        }

        private static void ConfigureCraterSlab(MapElementDefinition d)
        {
            d.DisplayName = "분화구 돌판";
            d.Category = ElementCategory.Platform;
            d.MoonProfile.Kind = MoonElementKind.CraterSlab;
            d.MoonProfile.FallDelaySeconds = 0.5f;
            d.MoonProfile.TiltSide = MoonTiltSide.Right;
            d.BehaviorProfile.WarningSeconds = 0.5f;
            d.PlacementProfile.Surface = SurfaceRequirement.Floor;
            SetFootprint(d, 2, 1, trigger: true);
            AddSolid(d, new Vector2(0.5f, -0.18f), new Vector2(1.96f, 0.58f));
            AddTrigger(d, new Vector2(0.5f, 0.2f), new Vector2(1.86f, 0.5f));
            AddReaction(d, ToolTag.HeavyImpact | ToolTag.Bomb, ElementReactionType.SetState, "Warning");
            d.BudgetProfile.ThreatCost = 2;
            d.BudgetProfile.UtilityValue = 2;
            d.BudgetProfile.MotionCost = 1;
            d.VisualProfile.VisualSizeCells = new Vector2(2f, 1f);
            d.VisualProfile.VisualOffsetCells = new Vector2(0.5f, 0f);
            d.VisualProfile.Tint = new Color(0.50f, 0.52f, 0.60f);
        }

        private static void ConfigureCassiaRoot(MapElementDefinition d)
        {
            d.DisplayName = "계수나무 뿌리";
            d.Category = ElementCategory.Platform;
            d.MoonProfile.Kind = MoonElementKind.CassiaRoot;
            d.MoonProfile.MinimumSegmentCount = 2;
            d.MoonProfile.SegmentCount = 4;
            d.MoonProfile.Damage = 1;
            d.PlacementProfile.Surface = SurfaceRequirement.Any;
            d.PlacementProfile.ForbiddenNeighborTags.Add("Portal");
            SetFootprint(d, 4, 1);
            AddSolid(d, new Vector2(1.5f, 0f), new Vector2(3.96f, 0.76f));
            AddReaction(d, ToolTag.Water, ElementReactionType.SetState, "Active");
            AddReaction(d, ToolTag.Pickaxe, ElementReactionType.Break, "Cut");
            AddReaction(d, ToolTag.Hook, ElementReactionType.Pull, "Pulled");
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.CognitiveCost = 2;
            d.VisualProfile.RenderMode = ElementVisualRenderMode.TiledSprite;
            d.VisualProfile.VisualSizeCells = new Vector2(4f, 1f);
            d.VisualProfile.VisualOffsetCells = new Vector2(1.5f, 0f);
            d.VisualProfile.Tint = new Color(0.48f, 0.72f, 0.42f);
        }

        private static void ConfigureMillShaft(MapElementDefinition d)
        {
            d.DisplayName = "방앗간 회전축";
            d.Category = ElementCategory.Hazard;
            d.MoonProfile.Kind = MoonElementKind.MillShaft;
            d.MoonProfile.StepAngleDegrees = 90f;
            d.MoonProfile.RotationSpeedDegreesPerSecond = 180f;
            d.MoonProfile.Damage = 1;
            SetFootprint(d, 2, 2, hazard: true);
            AddSolid(d, new Vector2(0.5f, 0.5f), new Vector2(1.94f, 0.58f));
            AddSolid(d, new Vector2(0.5f, 0.5f), new Vector2(0.58f, 1.94f));
            AddReaction(d, ToolTag.Hook, ElementReactionType.Toggle, "Rotate90");
            AddReaction(d, ToolTag.HeavyImpact, ElementReactionType.Disable, "StoppedByHeavy");
            d.BudgetProfile.ThreatCost = 2;
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.MotionCost = 2;
            d.VisualProfile.VisualSizeCells = new Vector2(2f, 2f);
            d.VisualProfile.VisualOffsetCells = new Vector2(0.5f, 0.5f);
            d.VisualProfile.Tint = new Color(0.66f, 0.54f, 0.36f);
        }

        private static void ConfigureMedicineMortar(MapElementDefinition d)
        {
            d.DisplayName = "약절구";
            d.Category = ElementCategory.Utility;
            d.MoonProfile.Kind = MoonElementKind.MedicineMortar;
            d.MoonProfile.InputSlots = 2;
            d.MoonProfile.OutputId = "moon_medicine";
            d.MoonProfile.HealAmount = 1;
            d.PlacementProfile.Surface = SurfaceRequirement.Floor;
            SetFootprint(d, 2, 2, trigger: true);
            AddSolid(d, new Vector2(0.5f, 0.25f), new Vector2(1.88f, 1.45f));
            AddTrigger(d, new Vector2(0.5f, 0.5f), new Vector2(1.94f, 1.94f));
            AddReaction(d, ToolTag.Context, ElementReactionType.SetState, "Active");
            AddReaction(d, ToolTag.Pound, ElementReactionType.SetState, "Active");
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.CognitiveCost = 2;
            d.VisualProfile.VisualSizeCells = new Vector2(2f, 2f);
            d.VisualProfile.VisualOffsetCells = new Vector2(0.5f, 0.5f);
            d.VisualProfile.Tint = new Color(0.72f, 0.54f, 0.82f);
        }

        private static void ConfigureFlourVent(MapElementDefinition d)
        {
            d.DisplayName = "밀가루 분출구";
            d.Category = ElementCategory.Vent;
            d.MoonProfile.Kind = MoonElementKind.FlourVent;
            d.MoonProfile.Direction = Vector2Int.up;
            d.MoonProfile.ForceCellsPerSecond = 7f;
            d.MoonProfile.CycleOnSeconds = 1.2f;
            d.MoonProfile.CycleOffSeconds = 1f;
            d.MoonProfile.WaterDisableSeconds = 2f;
            d.PlacementProfile.Surface = SurfaceRequirement.Floor;
            SetFootprint(d, 1, 1, trigger: true);
            AddSolid(d, new Vector2(0f, -0.36f), new Vector2(0.94f, 0.22f));
            AddTrigger(d, new Vector2(0f, 0.12f), new Vector2(0.9f, 0.72f));
            AddReaction(d, ToolTag.Water, ElementReactionType.Disable, "WetStopped");
            AddReaction(d, ToolTag.WindGuard, ElementReactionType.SetState, "Active");
            d.BudgetProfile.ThreatCost = 1;
            d.BudgetProfile.UtilityValue = 2;
            d.BudgetProfile.MotionCost = 1;
            d.VisualProfile.Tint = new Color(0.92f, 0.88f, 0.70f);
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
