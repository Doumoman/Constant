#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Map;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    public static class PostElementCatalogFactory
    {
        public const string CatalogFolder =
            "Assets/_Game/Editor/MapAuthoring/SourceElements/Post";

        public static readonly string[] CatalogIds =
        {
            "POST_Conveyor",
            "POST_ParcelLauncher",
            "POST_ReturnStamp",
            "POST_SortingArm",
            "POST_MailTube",
            "POST_InkPool",
            "POST_ParcelStack",
            "POST_ExpressTube",
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
                case "POST_Conveyor": ConfigureConveyor(definition); break;
                case "POST_ParcelLauncher": ConfigureParcelLauncher(definition); break;
                case "POST_ReturnStamp": ConfigureReturnStamp(definition); break;
                case "POST_SortingArm": ConfigureSortingArm(definition); break;
                case "POST_MailTube": ConfigureMailTube(definition); break;
                case "POST_InkPool": ConfigureInkPool(definition); break;
                case "POST_ParcelStack": ConfigureParcelStack(definition); break;
                case "POST_ExpressTube": ConfigureExpressTube(definition); break;
                default: throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown Post element.");
            }
        }

        private static void ResetDefinition(MapElementDefinition definition, string id)
        {
            definition.ElementId = id;
            definition.DisplayName = id;
            definition.Category = ElementCategory.Utility;
            definition.AllowedRegions = RegionMask.Post;
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

        private static void ConfigureConveyor(MapElementDefinition d)
        {
            d.DisplayName = "소포 컨베이어";
            d.Category = ElementCategory.Platform;
            d.PostProfile.Kind = PostElementKind.Conveyor;
            d.PostProfile.LengthCells = 4;
            d.PostProfile.Direction = Vector2Int.right;
            d.PostProfile.SurfaceSpeedCellsPerSecond = 2.5f;
            d.PostProfile.StopsOnHeavy = true;
            d.PostProfile.KeepPortalExitSafe = true;
            d.PlacementProfile.Surface = SurfaceRequirement.Floor;
            d.PlacementProfile.RequiredNeighborTags.Add("PortalExitSafeDestination");
            SetFootprint(d, 4, 1, trigger: true);
            AddSolid(d, new Vector2(1.5f, -0.22f), new Vector2(3.96f, 0.42f));
            AddTrigger(d, new Vector2(1.5f, 0.14f), new Vector2(3.92f, 0.62f));
            AddReaction(d, ToolTag.HeavyImpact, ElementReactionType.Disable, "HeavyStopped");
            d.BudgetProfile.ThreatCost = 1;
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.MotionCost = 1;
            d.VisualProfile.RenderMode = ElementVisualRenderMode.TiledSprite;
            d.VisualProfile.Tint = new Color(0.48f, 0.34f, 0.62f);
        }

        private static void ConfigureParcelLauncher(MapElementDefinition d)
        {
            d.DisplayName = "소포 발사기";
            d.Category = ElementCategory.Vent;
            d.PostProfile.Kind = PostElementKind.ParcelLauncher;
            d.PostProfile.Direction = Vector2Int.right;
            d.PostProfile.LaunchArc = 0.65f;
            d.PostProfile.LaunchPower = 10f;
            d.PostProfile.CollisionDamage = 1;
            d.PostProfile.RequiresParcelInsertion = true;
            d.PostProfile.RejectPlayerEntry = true;
            d.PostProfile.CompatibleParcelId = "*";
            d.PlacementProfile.Surface = SurfaceRequirement.Floor;
            SetFootprint(d, 1, 2, trigger: true);
            AddSolid(d, new Vector2(0f, 0.35f), new Vector2(0.92f, 1.68f));
            AddTrigger(d, new Vector2(0f, 0.68f), new Vector2(0.62f, 0.72f));
            AddReaction(d, ToolTag.Context, ElementReactionType.SetState, "Active");
            d.BudgetProfile.ThreatCost = 2;
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.MotionCost = 1;
            d.VisualProfile.Tint = new Color(0.62f, 0.42f, 0.72f);
        }

        private static void ConfigureReturnStamp(MapElementDefinition d)
        {
            d.DisplayName = "반송 도장";
            d.Category = ElementCategory.Hazard;
            d.PostProfile.Kind = PostElementKind.ReturnStamp;
            d.PostProfile.WarningDelaySeconds = 0.7f;
            d.PostProfile.StampActiveSeconds = 0.15f;
            d.PostProfile.StampDamage = 1;
            d.PostProfile.StampType = "Return";
            d.PostProfile.EscapeSpaceBelowCells = 1;
            d.BehaviorProfile.WarningSeconds = 0.7f;
            d.PlacementProfile.Surface = SurfaceRequirement.Ceiling;
            d.PlacementProfile.RequiredNeighborTags.Add("EscapeSpaceBelow1Cell");
            SetFootprint(d, 2, 2, hazard: true, trigger: true);
            AddSolid(d, new Vector2(0.5f, 1.22f), new Vector2(1.92f, 0.5f));
            AddTrigger(d, new Vector2(0.5f, 0.5f), new Vector2(1.94f, 1.94f));
            AddReaction(d, ToolTag.Hook, ElementReactionType.Toggle, "Warning");
            AddReaction(d, ToolTag.Pound, ElementReactionType.Toggle, "Warning");
            d.BudgetProfile.ThreatCost = 2;
            d.BudgetProfile.UtilityValue = 2;
            d.BudgetProfile.CognitiveCost = 1;
            d.VisualProfile.Tint = new Color(0.72f, 0.28f, 0.38f);
        }

        private static void ConfigureSortingArm(MapElementDefinition d)
        {
            d.DisplayName = "분류 팔";
            d.Category = ElementCategory.Control;
            d.PostProfile.Kind = PostElementKind.SortingArm;
            d.PostProfile.RotationStepDegrees = 90;
            d.PostProfile.RotationSequenceDegrees = new List<int> { 0, 90, 180, 270 };
            d.PostProfile.PushForceCellsPerSecond = 6f;
            SetFootprint(d, 2, 2, trigger: true);
            AddSolid(d, new Vector2(0.5f, 0.5f), new Vector2(1.72f, 0.34f));
            AddTrigger(d, new Vector2(0.5f, 0.5f), new Vector2(1.92f, 1.92f));
            AddReaction(d, ToolTag.Context, ElementReactionType.Toggle, "RouteChanged");
            AddReaction(d, ToolTag.HeavyImpact, ElementReactionType.Toggle, "RouteChanged");
            d.BudgetProfile.ThreatCost = 1;
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.CognitiveCost = 2;
            d.VisualProfile.Tint = new Color(0.76f, 0.58f, 0.26f);
        }

        private static void ConfigureMailTube(MapElementDefinition d)
        {
            d.DisplayName = "우편관";
            d.Category = ElementCategory.Utility;
            d.PostProfile.Kind = PostElementKind.MailTube;
            d.PostProfile.RequiresPair = true;
            d.PostProfile.PairGuid = "POST_MAIL_TUBE_PAIR";
            d.PostProfile.OneWay = false;
            d.PostProfile.CompatibleParcelId = "*";
            d.PlacementProfile.Surface = SurfaceRequirement.Wall;
            d.PlacementProfile.RequiredNeighborTags.Add("PairedTubeGuidRequired");
            SetFootprint(d, 1, 2, trigger: true);
            AddSolid(d, new Vector2(0f, 0.5f), new Vector2(0.9f, 1.92f));
            AddTrigger(d, new Vector2(0f, 0.5f), new Vector2(0.62f, 1.52f));
            AddReaction(d, ToolTag.Context, ElementReactionType.SetState, "Active");
            d.BudgetProfile.ThreatCost = 1;
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.CognitiveCost = 1;
            d.VisualProfile.Tint = new Color(0.48f, 0.42f, 0.7f);
        }

        private static void ConfigureInkPool(MapElementDefinition d)
        {
            d.DisplayName = "잉크 웅덩이";
            d.Category = ElementCategory.Hazard;
            d.PostProfile.Kind = PostElementKind.InkPool;
            d.PostProfile.WidthCells = 4;
            d.PostProfile.SlowRate = 0.4f;
            d.PostProfile.RevealsHiddenFootprints = true;
            d.PostProfile.WaterDilutes = true;
            d.PostProfile.UmbrellaBlocksDrops = false;
            d.BehaviorProfile.WarningSeconds = 0.15f;
            d.PlacementProfile.Surface = SurfaceRequirement.Floor;
            SetFootprint(d, 4, 1, trigger: true);
            AddTrigger(d, new Vector2(1.5f, -0.18f), new Vector2(3.96f, 0.62f));
            AddReaction(d, ToolTag.Water, ElementReactionType.Disable, "Diluted");
            d.BudgetProfile.ThreatCost = 2;
            d.BudgetProfile.UtilityValue = 2;
            d.BudgetProfile.CognitiveCost = 1;
            d.VisualProfile.RenderMode = ElementVisualRenderMode.TiledSprite;
            d.VisualProfile.Tint = new Color(0.22f, 0.14f, 0.34f, 0.9f);
        }

        private static void ConfigureParcelStack(MapElementDefinition d)
        {
            d.DisplayName = "소포 더미";
            d.Category = ElementCategory.Terrain;
            d.PostProfile.Kind = PostElementKind.ParcelStack;
            d.PostProfile.BoxCount = 4;
            d.PostProfile.StackPattern = "2x2";
            d.PostProfile.FlattenedHeightMultiplier = 0.4f;
            d.PlacementProfile.Surface = SurfaceRequirement.Floor;
            SetFootprint(d, 2, 2);
            AddSolid(d, new Vector2(0.5f, 0.5f), new Vector2(1.96f, 1.96f));
            AddReaction(d, ToolTag.Pound, ElementReactionType.Move, "Flattened");
            AddReaction(d, ToolTag.Bomb, ElementReactionType.Break, "Collapsed");
            d.BudgetProfile.ThreatCost = 1;
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.CognitiveCost = 1;
            d.VisualProfile.Tint = new Color(0.54f, 0.34f, 0.22f);
        }

        private static void ConfigureExpressTube(MapElementDefinition d)
        {
            d.DisplayName = "특급 우편관";
            d.Category = ElementCategory.Utility;
            d.PostProfile.Kind = PostElementKind.ExpressTube;
            d.PostProfile.RequiresPair = true;
            d.PostProfile.PairGuid = "POST_EXPRESS_TUBE_PAIR";
            d.PostProfile.OneWay = true;
            d.PostProfile.Direction = Vector2Int.right;
            d.PostProfile.LaunchPower = 12f;
            d.PostProfile.CompatibleParcelId = "*";
            d.PostProfile.RequiredStoryFlag = "post.express.enabled";
            d.PostProfile.RequiredParcelId = "OBJ_ParcelExpress";
            d.PostProfile.StartsActive = false;
            d.PlacementProfile.Surface = SurfaceRequirement.Wall;
            d.PlacementProfile.RequiredNeighborTags.Add("PairedTubeGuidRequired");
            SetFootprint(d, 1, 2, trigger: true);
            AddSolid(d, new Vector2(0f, 0.5f), new Vector2(0.9f, 1.92f));
            AddTrigger(d, new Vector2(0f, 0.5f), new Vector2(0.62f, 1.52f));
            AddReaction(d, ToolTag.Context, ElementReactionType.SetState, "Active");
            d.BudgetProfile.ThreatCost = 2;
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.CognitiveCost = 2;
            d.VisualProfile.Tint = new Color(0.92f, 0.68f, 0.2f);
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
