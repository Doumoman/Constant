#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Map;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    public static class MaruElementCatalogFactory
    {
        public const string CatalogFolder =
            "Assets/_Game/Editor/MapAuthoring/SourceElements/Maru";

        public static readonly string[] CatalogIds =
        {
            "MARU_ReturnStatue",
            "MARU_ReturnBellJar",
            "MARU_CollarFragment",
            "MARU_ReturnMarker",
            "MARU_PawprintPool",
            "MARU_RecordCasket",
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

        public static string GetAuthoringPath(string elementId)
        {
            return $"{CatalogFolder}/{elementId}.asset";
        }

        public static void Configure(MapElementDefinition definition, string id)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            ResetDefinition(definition, id);
            switch (id)
            {
                case "MARU_ReturnStatue": ConfigureReturnStatue(definition); break;
                case "MARU_ReturnBellJar": ConfigureReturnBellJar(definition); break;
                case "MARU_CollarFragment": ConfigureCollarFragment(definition); break;
                case "MARU_ReturnMarker": ConfigureReturnMarker(definition); break;
                case "MARU_PawprintPool": ConfigurePawprintPool(definition); break;
                case "MARU_RecordCasket": ConfigureRecordCasket(definition); break;
                default: throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown Maru element.");
            }
        }

        private static void ResetDefinition(MapElementDefinition definition, string id)
        {
            definition.ElementId = id;
            definition.DisplayName = id;
            definition.Category = ElementCategory.Utility;
            definition.AllowedRegions = RegionMask.All;
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

        private static void ConfigureReturnStatue(MapElementDefinition d)
        {
            d.DisplayName = "마루의 귀향상";
            d.Category = ElementCategory.Container;
            d.MaruProfile.Kind = MaruElementKind.ReturnStatue;
            d.MaruProfile.DurabilityStages = 2;
            d.MaruProfile.RewardMoney = 500;
            d.MaruProfile.RewardId = "currency_gold_ingot";
            d.MaruProfile.PreviewRewardText = "+500원 금 주괴";
            d.MaruProfile.PreviewPenaltyText = "방울 2단계 전진";
            d.MaruProfile.PressureWeight = 2;
            d.MaruProfile.MinimumExitRoomDistance = 3;
            d.MaruProfile.MaximumExitRoomDistance = 5;
            d.MaruProfile.ForbidExitRoom = true;
            d.PlacementProfile.AllowMainRoute = false;
            SetFootprint(d, 1, 2);
            AddSolid(d, new Vector2(0f, 0.5f), new Vector2(0.94f, 1.96f));
            AddReaction(d, ToolTag.Bomb, ElementReactionType.Break, 1);
            AddReaction(d, ToolTag.Pickaxe, ElementReactionType.Break, 2);
            AddReaction(d, ToolTag.Pound, ElementReactionType.Break, 2);
            AddReaction(d, ToolTag.HeavyImpact, ElementReactionType.Break, 2);
            AddReaction(d, ToolTag.Hook, ElementReactionType.Pull, 1);
            d.BudgetProfile.UtilityValue = 3;
            d.VisualProfile.Tint = new Color(0.72f, 0.58f, 0.25f);
        }

        private static void ConfigureReturnBellJar(MapElementDefinition d)
        {
            d.DisplayName = "귀환 방울단지";
            d.Category = ElementCategory.Container;
            d.MaruProfile.Kind = MaruElementKind.ReturnBellJar;
            d.MaruProfile.DurabilityStages = 1;
            d.MaruProfile.RewardMoney = 300;
            d.MaruProfile.RewardId = "currency_silver_ingot";
            d.MaruProfile.PreviewRewardText = "+300원 은 주괴";
            d.MaruProfile.PreviewPenaltyText = "12초 후 현재 방 진입";
            d.MaruProfile.ScheduledEntryDelaySeconds = 12f;
            d.MaruProfile.MinimumAutomaticHazardDistanceCells = 3;
            d.PlacementProfile.ForbiddenNeighborTags.Add("AutomaticHazard");
            d.PlacementProfile.MinimumSafeCellDistanceCells = 3;
            d.PlacementProfile.AllowMainRoute = false;
            SetFootprint(d, 1, 1);
            AddSolid(d, Vector2.zero, new Vector2(0.82f, 0.92f));
            AddReaction(d, ToolTag.Bomb | ToolTag.Pickaxe | ToolTag.LightImpact | ToolTag.HeavyImpact, ElementReactionType.Break, 1);
            d.BudgetProfile.UtilityValue = 2;
            d.BudgetProfile.ThreatCost = 2;
            d.VisualProfile.Tint = new Color(0.58f, 0.76f, 0.94f);
        }

        private static void ConfigureCollarFragment(MapElementDefinition d)
        {
            d.DisplayName = "별목줄 파편";
            d.Category = ElementCategory.Utility;
            d.MaruProfile.Kind = MaruElementKind.CollarFragment;
            d.MaruProfile.DurabilityStages = 1;
            d.MaruProfile.RewardId = "maru_clue_next_stage";
            d.MaruProfile.PreviewRewardText = "기록방·다음 스테이지 단서";
            d.MaruProfile.PreviewPenaltyText = "소지 중 방울 +15%";
            d.MaruProfile.TimerRateMultiplier = 1.15f;
            d.MaruProfile.PressureWeight = 2;
            d.PlacementProfile.AllowMainRoute = false;
            SetFootprint(d, 1, 1);
            AddSolid(d, Vector2.zero, new Vector2(0.74f, 0.36f));
            d.BudgetProfile.UtilityValue = 3;
            d.VisualProfile.Tint = new Color(0.88f, 0.72f, 0.32f);
        }

        private static void ConfigureReturnMarker(MapElementDefinition d)
        {
            d.DisplayName = "귀환 표식대";
            d.Category = ElementCategory.Control;
            d.MaruProfile.Kind = MaruElementKind.ReturnMarker;
            d.MaruProfile.DurabilityStages = 1;
            d.MaruProfile.MarkerCostType = MaruMarkerCostType.Money;
            d.MaruProfile.MarkerCostValue = 50;
            d.MaruProfile.PreviewRewardText = "Entry SafeCell 즉시 귀환";
            d.MaruProfile.PreviewPenaltyText = "소지금 -50원";
            SetFootprint(d, 1, 2);
            AddSolid(d, new Vector2(0f, 0.5f), new Vector2(0.70f, 1.94f));
            d.BudgetProfile.UtilityValue = 2;
            d.VisualProfile.Tint = new Color(0.42f, 0.82f, 0.88f);
        }

        private static void ConfigurePawprintPool(MapElementDefinition d)
        {
            d.DisplayName = "마루 발자국 웅덩이";
            d.Category = ElementCategory.Trigger;
            d.MaruProfile.Kind = MaruElementKind.PawprintPool;
            d.MaruProfile.DurabilityStages = 1;
            d.MaruProfile.GuidanceSeconds = 4f;
            d.MaruProfile.ShortenNextBellSeconds = 8f;
            d.MaruProfile.PreviewRewardText = "출구 방향 4초 표시";
            d.MaruProfile.PreviewPenaltyText = "다음 방울 -8초";
            d.CollisionProfile.IsTriggerOnly = true;
            SetFootprint(d, 2, 1, trigger: true);
            AddTrigger(d, new Vector2(0.5f, 0f), new Vector2(1.94f, 0.76f));
            d.BudgetProfile.UtilityValue = 1;
            d.BudgetProfile.ThreatCost = 1;
            d.VisualProfile.Tint = new Color(0.32f, 0.42f, 0.62f);
        }

        private static void ConfigureRecordCasket(MapElementDefinition d)
        {
            d.DisplayName = "별기록관의 관";
            d.Category = ElementCategory.Container;
            d.MaruProfile.Kind = MaruElementKind.RecordCasket;
            d.MaruProfile.DurabilityStages = 2;
            d.MaruProfile.RewardId = "record_traveler_freed";
            d.MaruProfile.PreviewRewardText = "기록 길손 1명 해방";
            d.MaruProfile.PreviewPenaltyText = "낮은 소음";
            d.MaruProfile.NoiseLevel = 0.15f;
            d.MaruProfile.RecordGuideEffect = MaruRecordGuideEffect.ExitDirection;
            d.PlacementProfile.AllowMainRoute = false;
            SetFootprint(d, 2, 2);
            AddSolid(d, new Vector2(0.5f, 0.5f), new Vector2(1.94f, 1.94f));
            d.BudgetProfile.UtilityValue = 3;
            d.BudgetProfile.CognitiveCost = 1;
            d.VisualProfile.Tint = new Color(0.42f, 0.30f, 0.58f);
        }

        private static void SetFootprint(MapElementDefinition d, int width, int height, bool trigger = false)
        {
            d.Footprint.BoundsSize = new Vector2Int(width, height);
            d.Footprint.PivotCell = Vector2Int.zero;
            d.Footprint.OccupiedCells.Clear();
            d.Footprint.TriggerCells.Clear();
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    d.Footprint.OccupiedCells.Add(cell);
                    if (trigger) d.Footprint.TriggerCells.Add(cell);
                }
            }
            d.VisualProfile.VisualSizeCells = new Vector2(width, height);
            d.VisualProfile.VisualOffsetCells = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
        }

        private static void AddSolid(MapElementDefinition d, Vector2 offset, Vector2 size)
        {
            d.CollisionProfile.IsSolid = true;
            d.CollisionProfile.SolidShapes.Add(new SerializedColliderShape
            {
                ShapeType = SerializedColliderShapeType.Box,
                OffsetCells = offset,
                SizeCells = size,
            });
        }

        private static void AddTrigger(MapElementDefinition d, Vector2 offset, Vector2 size)
        {
            d.CollisionProfile.TriggerShapes.Add(new SerializedColliderShape
            {
                ShapeType = SerializedColliderShapeType.Box,
                OffsetCells = offset,
                SizeCells = size,
            });
        }

        private static void AddReaction(
            MapElementDefinition d,
            ToolTag tool,
            ElementReactionType reaction,
            int strength)
        {
            d.ToolReactions.Entries.Add(new ToolReactionEntry
            {
                Tool = tool,
                Reaction = reaction,
                StrengthRequired = Mathf.Max(1, strength),
            });
        }
    }
}

#endif
