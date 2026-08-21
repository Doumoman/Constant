#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Map;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    public sealed class CommonElementCatalogBakeReport
    {
        public readonly List<MapElementDefinition> Definitions = new List<MapElementDefinition>();
        public readonly List<MapElementBakeResult> Results = new List<MapElementBakeResult>();

        public int SuccessCount { get; internal set; }
        public int FailureCount => Results.Count - SuccessCount;
        public bool Success => Results.Count > 0 && FailureCount == 0;
    }

    public static class CommonElementCatalogFactory
    {
        public const string CatalogFolder =
            "Assets/_Game/Editor/MapAuthoring/SourceElements/Common";

        public static readonly string[] CatalogIds =
        {
            "COMMON_Block_Solid",
            "COMMON_Block_Unbreakable",
            "COMMON_Block_Cracked",
            "COMMON_Block_SoftSoil",
            "COMMON_Platform_OneWay",
            "COMMON_Floor_Fragile",
            "COMMON_Trigger_PressurePlate",
            "COMMON_Control_Lever",
            "COMMON_Door_Weight",
            "COMMON_Platform_MoveLinear",
            "COMMON_Platform_FallingStone",
            "COMMON_Hazard_PendulumBall",
            "COMMON_Hazard_Crusher",
            "COMMON_Platform_PulleyLift",
            "COMMON_Hazard_Spike",
            "COMMON_Hazard_TotemShooter",
            "COMMON_Hazard_LaserEmitter",
            "COMMON_Hazard_RollingBoulder",
            "COMMON_Vent_Wind",
            "COMMON_Vent_Water",
            "COMMON_BouncePad",
            "COMMON_Anchor_Rope",
            "COMMON_Anchor_Hook",
            "COMMON_Container_Breakable",
            "COMMON_Lantern_ExitGuide",
        };

        public static IReadOnlyList<MapElementDefinition> EnsureCatalog(bool overwriteExisting = false)
        {
            AssetPathUtility.EnsureFolder(CatalogFolder);
            var definitions = new List<MapElementDefinition>(CatalogIds.Length);
            for (var index = 0; index < CatalogIds.Length; index++)
            {
                var id = CatalogIds[index];
                var path = GetAuthoringPath(id);
                var existing = AssetDatabase.LoadAssetAtPath<MapElementDefinition>(path);
                if (existing == null)
                {
                    existing = ScriptableObject.CreateInstance<MapElementDefinition>();
                    Configure(existing, id);
                    existing.name = id;
                    AssetDatabase.CreateAsset(existing, path);
                }
                else if (overwriteExisting)
                {
                    Undo.RecordObject(existing, $"Refresh {id}");
                    Configure(existing, id);
                    existing.name = id;
                    EditorUtility.SetDirty(existing);
                }

                definitions.Add(existing);
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

        public static void Configure(MapElementDefinition definition, string elementId)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            ResetDefinition(definition, elementId);
            switch (elementId)
            {
                case "COMMON_Block_Solid": ConfigureSolid(definition); break;
                case "COMMON_Block_Unbreakable": ConfigureUnbreakable(definition); break;
                case "COMMON_Block_Cracked": ConfigureCracked(definition); break;
                case "COMMON_Block_SoftSoil": ConfigureSoftSoil(definition); break;
                case "COMMON_Platform_OneWay": ConfigureOneWay(definition); break;
                case "COMMON_Floor_Fragile": ConfigureFragile(definition); break;
                case "COMMON_Trigger_PressurePlate": ConfigurePressurePlate(definition); break;
                case "COMMON_Control_Lever": ConfigureLever(definition); break;
                case "COMMON_Door_Weight": ConfigureWeightDoor(definition); break;
                case "COMMON_Platform_MoveLinear": ConfigureMovingPlatform(definition); break;
                case "COMMON_Platform_FallingStone": ConfigureFallingStone(definition); break;
                case "COMMON_Hazard_PendulumBall": ConfigurePendulumBall(definition); break;
                case "COMMON_Hazard_Crusher": ConfigureCrusher(definition); break;
                case "COMMON_Platform_PulleyLift": ConfigurePulleyLift(definition); break;
                case "COMMON_Hazard_Spike": ConfigureSpike(definition); break;
                case "COMMON_Hazard_TotemShooter": ConfigureTotem(definition); break;
                case "COMMON_Hazard_LaserEmitter": ConfigureLaser(definition); break;
                case "COMMON_Hazard_RollingBoulder": ConfigureRollingBoulder(definition); break;
                case "COMMON_Vent_Wind": ConfigureWind(definition); break;
                case "COMMON_Vent_Water": ConfigureWater(definition); break;
                case "COMMON_BouncePad": ConfigureBounce(definition); break;
                case "COMMON_Anchor_Rope": ConfigureRopeAnchor(definition); break;
                case "COMMON_Anchor_Hook": ConfigureHookAnchor(definition); break;
                case "COMMON_Container_Breakable": ConfigureBreakableContainer(definition); break;
                case "COMMON_Lantern_ExitGuide": ConfigureExitGuideLantern(definition); break;
                default: throw new ArgumentOutOfRangeException(nameof(elementId), elementId, "Unknown common element.");
            }
        }

        private static void ResetDefinition(MapElementDefinition definition, string id)
        {
            definition.ElementId = id;
            definition.DisplayName = id;
            definition.Category = ElementCategory.Utility;
            definition.AllowedRegions = RegionMask.Common;
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
                PauseWhenRoomInactive = true,
                PersistBrokenState = true,
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

        private static void ConfigureSolid(MapElementDefinition d)
        {
            d.DisplayName = "일반 지형";
            d.Category = ElementCategory.Terrain;
            d.CommonProfile.Kind = CommonElementKind.SolidBlock;
            SetFootprint(d, 1, 1);
            AddSolidBox(d, Vector2.zero, new Vector2(0.98f, 0.98f));
            SetTint(d, new Color(0.34f, 0.48f, 0.60f));
        }

        private static void ConfigureUnbreakable(MapElementDefinition d)
        {
            ConfigureSolid(d);
            d.ElementId = "COMMON_Block_Unbreakable";
            d.DisplayName = "파괴 불가 지형";
            d.CommonProfile.Kind = CommonElementKind.UnbreakableBlock;
            d.BudgetProfile.UtilityValue = 1;
            SetTint(d, new Color(0.20f, 0.25f, 0.36f));
        }

        private static void ConfigureCracked(MapElementDefinition d)
        {
            ConfigureSolid(d);
            d.ElementId = "COMMON_Block_Cracked";
            d.DisplayName = "균열 블록";
            d.CommonProfile.Kind = CommonElementKind.CrackedBlock;
            d.CommonProfile.WeakHitsRequired = 3;
            d.PlacementProfile.AllowMainRoute = false;
            d.BudgetProfile.UtilityValue = 1;
            AddReaction(d, ToolTag.Bomb | ToolTag.Pickaxe | ToolTag.Pound | ToolTag.HeavyImpact | ToolTag.Projectile, ElementReactionType.Break, 1);
            AddReaction(d, ToolTag.LightImpact, ElementReactionType.Break, 3);
            SetTint(d, new Color(0.60f, 0.42f, 0.32f));
        }

        private static void ConfigureSoftSoil(MapElementDefinition d)
        {
            ConfigureSolid(d);
            d.ElementId = "COMMON_Block_SoftSoil";
            d.DisplayName = "부드러운 흙";
            d.CommonProfile.Kind = CommonElementKind.SoftSoil;
            d.BudgetProfile.UtilityValue = 2;
            d.PlacementProfile.AllowMainRoute = false;
            d.PlacementProfile.MinimumPortalDistanceCells = 2;
            d.PlacementProfile.ForbiddenNeighborTags.Add("UnbreakableBoundary");
            d.PlacementProfile.ForbiddenNeighborTags.Add("VoidRecoveryZone");
            AddReaction(d, ToolTag.Shovel, ElementReactionType.Break, 1);
            AddReaction(d, ToolTag.Pickaxe, ElementReactionType.SetState, 1, "SoftSoil");
            AddReaction(d, ToolTag.Bomb, ElementReactionType.SetState, 1, "AbsorbExplosion");
            AddReaction(d, ToolTag.LightImpact | ToolTag.HeavyImpact, ElementReactionType.SetState, 1, "CushionImpact");
            SetTint(d, new Color(0.52f, 0.34f, 0.22f));
        }

        private static void ConfigureOneWay(MapElementDefinition d)
        {
            d.DisplayName = "단방향 플랫폼";
            d.Category = ElementCategory.Platform;
            d.CommonProfile.Kind = CommonElementKind.OneWayPlatform;
            SetFootprint(d, 2, 1);
            d.VisualProfile.RenderMode = ElementVisualRenderMode.TiledSprite;
            d.CollisionProfile.IsSolid = true;
            d.CollisionProfile.IsOneWay = true;
            AddSolidBox(d, new Vector2(0.5f, 0.35f), new Vector2(1.98f, 0.24f));
            d.PlacementProfile.ForbiddenNeighborTags.Add("Portal");
            SetTint(d, new Color(0.30f, 0.68f, 0.78f));
        }

        private static void ConfigureFragile(MapElementDefinition d)
        {
            d.DisplayName = "붕괴 바닥";
            d.Category = ElementCategory.Platform;
            d.CommonProfile.Kind = CommonElementKind.FragileFloor;
            d.CommonProfile.TriggerDwellSeconds = 0.55f;
            d.BehaviorProfile.WarningSeconds = 0.25f;
            d.CollisionProfile.IsSolid = true;
            SetFootprint(d, 1, 1, trigger: true);
            AddSolidBox(d, Vector2.zero, new Vector2(0.98f, 0.70f));
            AddTriggerBox(d, new Vector2(0f, 0.35f), new Vector2(0.88f, 0.25f));
            AddReaction(d, ToolTag.Bomb | ToolTag.Pickaxe | ToolTag.Pound | ToolTag.HeavyImpact, ElementReactionType.Break, 1);
            SetTint(d, new Color(0.66f, 0.46f, 0.27f));
        }

        private static void ConfigurePressurePlate(MapElementDefinition d)
        {
            d.DisplayName = "압력판";
            d.Category = ElementCategory.Trigger;
            d.CommonProfile.Kind = CommonElementKind.PressurePlate;
            d.CommonProfile.WeightThreshold = 1;
            d.CommonProfile.SignalMode = CommonSignalMode.Hold;
            d.CommonProfile.SignalChannel = "LAB_WEIGHT_DOOR";
            SetFootprint(d, 1, 1, trigger: true);
            AddSolidBox(d, new Vector2(0f, -0.38f), new Vector2(0.92f, 0.18f));
            AddTriggerBox(d, new Vector2(0f, 0.05f), new Vector2(0.86f, 0.70f));
            d.BudgetProfile.CognitiveCost = 1;
            SetTint(d, new Color(0.92f, 0.72f, 0.22f));
        }

        private static void ConfigureLever(MapElementDefinition d)
        {
            d.DisplayName = "레버";
            d.Category = ElementCategory.Control;
            d.CommonProfile.Kind = CommonElementKind.Lever;
            d.CommonProfile.SignalMode = CommonSignalMode.Toggle;
            d.CommonProfile.SignalChannel = "LAB_WEIGHT_DOOR";
            SetFootprint(d, 1, 1);
            AddSolidBox(d, new Vector2(0f, -0.32f), new Vector2(0.62f, 0.30f));
            AddReaction(d, ToolTag.Hook, ElementReactionType.Toggle, 1, "Active");
            d.BudgetProfile.CognitiveCost = 1;
            SetTint(d, new Color(0.92f, 0.58f, 0.18f));
        }

        private static void ConfigureWeightDoor(MapElementDefinition d)
        {
            d.DisplayName = "무게 문";
            d.Category = ElementCategory.Door;
            d.CommonProfile.Kind = CommonElementKind.WeightDoor;
            d.CommonProfile.OpenSpeedCellsPerSecond = 2f;
            d.CommonProfile.SignalChannel = "LAB_WEIGHT_DOOR";
            SetFootprint(d, 1, 2);
            AddSolidBox(d, new Vector2(0f, 0.5f), new Vector2(0.94f, 1.98f));
            d.BudgetProfile.CognitiveCost = 1;
            SetTint(d, new Color(0.34f, 0.55f, 0.66f));
        }

        private static void ConfigureMovingPlatform(MapElementDefinition d)
        {
            d.DisplayName = "직선 이동 플랫폼";
            d.Category = ElementCategory.Platform;
            d.CommonProfile.Kind = CommonElementKind.MovingPlatform;
            SetFootprint(d, 2, 1);
            d.VisualProfile.RenderMode = ElementVisualRenderMode.TiledSprite;
            d.CollisionProfile.IsSolid = true;
            AddSolidBox(d, new Vector2(0.5f, 0f), new Vector2(1.98f, 0.72f));
            d.BehaviorProfile.Path.Nodes.Add(Vector2.zero);
            d.BehaviorProfile.Path.Nodes.Add(new Vector2(4f, 0f));
            d.BehaviorProfile.Path.SpeedCellsPerSecond = 2.2f;
            d.BehaviorProfile.Path.WaitSeconds = 0.3f;
            d.BehaviorProfile.Path.PingPong = true;
            d.BehaviorProfile.Path.ResetOnRoomReenter = true;
            AddReaction(d, ToolTag.Hook, ElementReactionType.Pull, 1, "HookAnchor");
            d.BudgetProfile.MotionCost = 1;
            SetTint(d, new Color(0.25f, 0.74f, 0.88f));
        }

        private static void ConfigureFallingStone(MapElementDefinition d)
        {
            d.DisplayName = "낙하석";
            d.Category = ElementCategory.Platform;
            d.CommonProfile.Kind = CommonElementKind.FallingStone;
            d.CommonProfile.Damage = 1;
            d.CommonProfile.TriggerDwellSeconds = 0.15f;
            d.CommonProfile.GravityScale = 2f;
            d.BehaviorProfile.WarningSeconds = 0.45f;
            SetFootprint(d, 1, 1, trigger: true);
            AddSolidBox(d, Vector2.zero, new Vector2(0.96f, 0.96f));
            AddTriggerBox(d, new Vector2(0f, -0.40f), new Vector2(0.82f, 0.18f));
            AddReaction(d, ToolTag.Bomb | ToolTag.HeavyImpact, ElementReactionType.SetState, 1, "Active");
            d.BudgetProfile.ThreatCost = 2;
            d.BudgetProfile.MotionCost = 1;
            SetTint(d, new Color(0.48f, 0.50f, 0.58f));
        }

        private static void ConfigurePendulumBall(MapElementDefinition d)
        {
            d.DisplayName = "Pendulum Ball";
            d.Category = ElementCategory.Hazard;
            d.CommonProfile.Kind = CommonElementKind.PendulumBall;
            d.CommonProfile.Damage = 1;
            d.CommonProfile.WeightThreshold = 2;
            d.CommonProfile.ChainLengthCells = 3;
            d.CommonProfile.SwingArcDegrees = 55f;
            d.CommonProfile.SwingPeriodSeconds = 2.4f;
            d.BehaviorProfile.WarningSeconds = 0.25f;
            SetFootprint(d, 1, 1, hazard: true, trigger: true);
            AddSolidBox(d, Vector2.zero, new Vector2(0.94f, 0.94f));
            AddTriggerBox(d, Vector2.zero, new Vector2(1.02f, 1.02f));
            AddReaction(d, ToolTag.Hook, ElementReactionType.Pull, 1, "TrajectoryChanged");
            AddReaction(d, ToolTag.Bomb, ElementReactionType.Push, 1, "TrajectoryChanged");
            d.PlacementProfile.ForbiddenNeighborTags.Add("EntrySafeZone");
            d.BudgetProfile.ThreatCost = 3;
            d.BudgetProfile.MotionCost = 1;
            SetTint(d, new Color(0.42f, 0.44f, 0.52f));
        }

        private static void ConfigureCrusher(MapElementDefinition d)
        {
            d.DisplayName = "Crusher";
            d.Category = ElementCategory.Hazard;
            d.CommonProfile.Kind = CommonElementKind.Crusher;
            d.CommonProfile.Damage = 1;
            d.CommonProfile.SignalMode = CommonSignalMode.Pulse;
            d.CommonProfile.SignalChannel = "COMMON_CRUSHER";
            d.CommonProfile.TravelCells = 3f;
            d.CommonProfile.MoveSpeedCellsPerSecond = 8f;
            d.CommonProfile.HoldSeconds = 0.4f;
            d.CommonProfile.ReturnSpeedCellsPerSecond = 3f;
            d.BehaviorProfile.WarningSeconds = 0.6f;
            SetFootprint(d, 1, 1, hazard: true, trigger: true);
            AddSolidBox(d, Vector2.zero, new Vector2(0.96f, 0.96f));
            AddTriggerBox(d, Vector2.zero, new Vector2(0.92f, 0.92f));
            AddReaction(d, ToolTag.Hook, ElementReactionType.SetState, 1, "Warning");
            d.PlacementProfile.RequiredNeighborTags.Add("EscapeCell");
            d.BudgetProfile.ThreatCost = 4;
            d.BudgetProfile.MotionCost = 1;
            SetTint(d, new Color(0.62f, 0.24f, 0.25f));
        }

        private static void ConfigurePulleyLift(MapElementDefinition d)
        {
            d.DisplayName = "Pulley Lift";
            d.Category = ElementCategory.Platform;
            d.CommonProfile.Kind = CommonElementKind.PulleyLift;
            d.CommonProfile.SignalMode = CommonSignalMode.Toggle;
            d.CommonProfile.SignalChannel = "COMMON_PULLEY_LIFT";
            d.CommonProfile.TravelCells = 4f;
            d.CommonProfile.MoveSpeedCellsPerSecond = 2f;
            SetFootprint(d, 2, 1);
            d.VisualProfile.RenderMode = ElementVisualRenderMode.TiledSprite;
            AddSolidBox(d, new Vector2(0.5f, 0f), new Vector2(1.98f, 0.72f));
            d.BehaviorProfile.Path.Nodes.Add(Vector2.zero);
            d.BehaviorProfile.Path.Nodes.Add(new Vector2(0f, 4f));
            d.BehaviorProfile.Path.SpeedCellsPerSecond = 2f;
            d.BehaviorProfile.Path.WaitSeconds = 0.4f;
            d.BehaviorProfile.Path.PingPong = true;
            AddReaction(d, ToolTag.Hook, ElementReactionType.Toggle, 1, "Active");
            d.BudgetProfile.CognitiveCost = 1;
            d.BudgetProfile.MotionCost = 1;
            SetTint(d, new Color(0.30f, 0.66f, 0.72f));
        }

        private static void ConfigureSpike(MapElementDefinition d)
        {
            d.DisplayName = "가시";
            d.Category = ElementCategory.Hazard;
            d.CommonProfile.Kind = CommonElementKind.Spike;
            d.CommonProfile.Damage = 1;
            d.CommonProfile.DamageCooldownSeconds = 0.35f;
            d.BehaviorProfile.WarningSeconds = 0.15f;
            d.PlacementProfile.MinimumPortalDistanceCells = 2;
            SetFootprint(d, 1, 1, hazard: true, trigger: true);
            AddSolidBox(d, new Vector2(0f, -0.39f), new Vector2(0.96f, 0.20f));
            AddTriggerBox(d, new Vector2(0f, 0.05f), new Vector2(0.82f, 0.70f));
            d.BudgetProfile.ThreatCost = 2;
            SetTint(d, new Color(0.88f, 0.24f, 0.30f));
        }

        private static void ConfigureTotem(MapElementDefinition d)
        {
            d.DisplayName = "투사체 토템";
            d.Category = ElementCategory.Hazard;
            d.CommonProfile.Kind = CommonElementKind.TotemShooter;
            d.CommonProfile.Damage = 1;
            d.CommonProfile.SightOrBeamRangeCells = 8f;
            d.CommonProfile.ProjectileSpeedCellsPerSecond = 7f;
            d.BehaviorProfile.WarningSeconds = 0.55f;
            d.BehaviorProfile.ActiveSeconds = 0.05f;
            d.BehaviorProfile.CooldownSeconds = 2.2f;
            d.BehaviorProfile.ProjectilePattern.SpeedCellsPerSecond = 7f;
            d.BehaviorProfile.ProjectilePattern.Direction = Vector2.right;
            SetFootprint(d, 1, 2);
            AddSolidBox(d, new Vector2(0f, 0.5f), new Vector2(0.92f, 1.94f));
            AddReaction(d, ToolTag.Bomb, ElementReactionType.Break, 1);
            AddReaction(d, ToolTag.Pickaxe, ElementReactionType.Break, 2);
            AddReaction(d, ToolTag.Pound, ElementReactionType.Disable, 1);
            d.BudgetProfile.ThreatCost = 3;
            SetTint(d, new Color(0.72f, 0.30f, 0.38f));
        }

        private static void ConfigureLaser(MapElementDefinition d)
        {
            d.DisplayName = "레이저 발사기";
            d.Category = ElementCategory.Hazard;
            d.CommonProfile.Kind = CommonElementKind.LaserEmitter;
            d.CommonProfile.Damage = 1;
            d.CommonProfile.SightOrBeamRangeCells = 8f;
            d.BehaviorProfile.WarningSeconds = 0.7f;
            d.BehaviorProfile.ActiveSeconds = 1.1f;
            d.BehaviorProfile.CooldownSeconds = 1.4f;
            d.PlacementProfile.MaxPerRoom = 2;
            SetFootprint(d, 1, 1, hazard: true);
            AddSolidBox(d, Vector2.zero, new Vector2(0.94f, 0.94f));
            AddReaction(d, ToolTag.Bomb, ElementReactionType.Disable, 1);
            AddReaction(d, ToolTag.Pickaxe, ElementReactionType.Disable, 2);
            AddReaction(d, ToolTag.HeavyImpact, ElementReactionType.Toggle, 1, "Rotate");
            d.BudgetProfile.ThreatCost = 3;
            SetTint(d, new Color(0.92f, 0.20f, 0.52f));
        }

        private static void ConfigureRollingBoulder(MapElementDefinition d)
        {
            d.DisplayName = "Rolling Boulder";
            d.Category = ElementCategory.Hazard;
            d.CommonProfile.Kind = CommonElementKind.RollingBoulder;
            d.CommonProfile.Damage = 1;
            d.CommonProfile.WeightThreshold = 2;
            d.CommonProfile.GravityScale = 2f;
            d.CommonProfile.MaximumSpeedCellsPerSecond = 6f;
            d.BehaviorProfile.WarningSeconds = 0.35f;
            SetFootprint(d, 1, 1, hazard: true, trigger: true);
            AddSolidBox(d, Vector2.zero, new Vector2(0.94f, 0.94f));
            AddTriggerBox(d, Vector2.zero, new Vector2(1.02f, 1.02f));
            AddReaction(d, ToolTag.Bomb, ElementReactionType.SetState, 1, "Active");
            AddReaction(d, ToolTag.Hook, ElementReactionType.Pull, 1, "TrajectoryChanged");
            d.PlacementProfile.RequiredNeighborTags.Add("StopPocketOrUnbreakableStopper");
            d.BudgetProfile.ThreatCost = 3;
            d.BudgetProfile.MotionCost = 1;
            SetTint(d, new Color(0.43f, 0.39f, 0.34f));
        }

        private static void ConfigureWind(MapElementDefinition d)
        {
            d.DisplayName = "바람 분출구";
            d.Category = ElementCategory.Vent;
            d.CommonProfile.Kind = CommonElementKind.WindVent;
            d.CommonProfile.ForceCellsPerSecond = 7f;
            d.CommonProfile.VolumeSizeCells = new Vector2(3f, 6f);
            d.CommonProfile.CycleOnSeconds = 1.5f;
            d.CommonProfile.CycleOffSeconds = 1f;
            SetFootprint(d, 1, 1, trigger: true);
            AddSolidBox(d, Vector2.zero, new Vector2(0.92f, 0.92f));
            AddTriggerBox(d, new Vector2(1.5f, 0f), new Vector2(3f, 6f));
            AddReaction(d, ToolTag.WindGuard, ElementReactionType.Move, 1, "WindAssist");
            d.BudgetProfile.MotionCost = 1;
            SetTint(d, new Color(0.38f, 0.78f, 0.92f));
        }

        private static void ConfigureWater(MapElementDefinition d)
        {
            d.DisplayName = "물 분출구";
            d.Category = ElementCategory.Vent;
            d.CommonProfile.Kind = CommonElementKind.WaterVent;
            d.CommonProfile.ForceCellsPerSecond = 5f;
            d.CommonProfile.VolumeSizeCells = new Vector2(2f, 4f);
            SetFootprint(d, 1, 1, trigger: true);
            AddSolidBox(d, Vector2.zero, new Vector2(0.92f, 0.92f));
            AddTriggerBox(d, new Vector2(1f, 0f), new Vector2(2f, 4f));
            AddReaction(d, ToolTag.Context, ElementReactionType.SetState, 1, "Active");
            d.BudgetProfile.UtilityValue = 1;
            SetTint(d, new Color(0.20f, 0.55f, 0.88f));
        }

        private static void ConfigureBounce(MapElementDefinition d)
        {
            d.DisplayName = "점프 패드";
            d.Category = ElementCategory.Utility;
            d.CommonProfile.Kind = CommonElementKind.BouncePad;
            d.CommonProfile.LaunchHeightCells = 3f;
            d.BehaviorProfile.CooldownSeconds = 0.25f;
            SetFootprint(d, 1, 1, trigger: true);
            AddSolidBox(d, new Vector2(0f, -0.36f), new Vector2(0.92f, 0.22f));
            AddTriggerBox(d, new Vector2(0f, 0.08f), new Vector2(0.82f, 0.68f));
            d.BudgetProfile.UtilityValue = 2;
            SetTint(d, new Color(0.48f, 0.88f, 0.42f));
        }

        private static void ConfigureRopeAnchor(MapElementDefinition d)
        {
            d.DisplayName = "Rope Anchor";
            d.Category = ElementCategory.Anchor;
            d.CommonProfile.Kind = CommonElementKind.RopeAnchor;
            SetFootprint(d, 1, 1);
            AddSolidBox(d, Vector2.zero, new Vector2(0.72f, 0.72f));
            AddReaction(d, ToolTag.Rope, ElementReactionType.SetState, 1, "Active");
            d.PlacementProfile.Surface = SurfaceRequirement.Wall;
            d.BudgetProfile.UtilityValue = 2;
            SetTint(d, new Color(0.78f, 0.62f, 0.34f));
        }

        private static void ConfigureHookAnchor(MapElementDefinition d)
        {
            d.DisplayName = "Hook Anchor";
            d.Category = ElementCategory.Anchor;
            d.CommonProfile.Kind = CommonElementKind.HookAnchor;
            SetFootprint(d, 1, 1);
            AddSolidBox(d, Vector2.zero, new Vector2(0.72f, 0.72f));
            AddReaction(d, ToolTag.Hook, ElementReactionType.Pull, 1, "PullPlayer");
            d.PlacementProfile.Surface = SurfaceRequirement.Wall;
            d.BudgetProfile.UtilityValue = 2;
            SetTint(d, new Color(0.34f, 0.72f, 0.88f));
        }

        private static void ConfigureBreakableContainer(MapElementDefinition d)
        {
            d.DisplayName = "Breakable Container";
            d.Category = ElementCategory.Container;
            d.CommonProfile.Kind = CommonElementKind.BreakableContainer;
            d.CommonProfile.ContentsId = "Empty";
            SetFootprint(d, 1, 1);
            AddSolidBox(d, Vector2.zero, new Vector2(0.88f, 0.94f));
            AddReaction(d, ToolTag.LightImpact | ToolTag.HeavyImpact | ToolTag.Pickaxe |
                ToolTag.Pound | ToolTag.Bomb, ElementReactionType.Break, 1, "DropContents");
            d.PlacementProfile.ForbiddenNeighborTags.Add("EntrySafeZoneExplosiveContent");
            d.BudgetProfile.UtilityValue = 1;
            SetTint(d, new Color(0.62f, 0.42f, 0.22f));
        }

        private static void ConfigureExitGuideLantern(MapElementDefinition d)
        {
            d.DisplayName = "Exit Guide Lantern";
            d.Category = ElementCategory.Utility;
            d.CommonProfile.Kind = CommonElementKind.ExitGuideLantern;
            d.CommonProfile.GuideDurationSeconds = 3f;
            SetFootprint(d, 1, 2, trigger: true);
            AddSolidBox(d, new Vector2(0f, 0.5f), new Vector2(0.72f, 1.82f));
            AddTriggerBox(d, new Vector2(0f, 0.5f), new Vector2(0.96f, 1.96f));
            AddReaction(d, ToolTag.Context, ElementReactionType.SetState, 1, "Active");
            d.BudgetProfile.UtilityValue = 2;
            SetTint(d, new Color(0.96f, 0.78f, 0.26f));
        }

        private static void SetFootprint(
            MapElementDefinition d,
            int width,
            int height,
            bool hazard = false,
            bool trigger = false)
        {
            d.Footprint.BoundsSize = new Vector2Int(width, height);
            d.Footprint.PivotCell = Vector2Int.zero;
            d.Footprint.OccupiedCells.Clear();
            d.Footprint.HazardCells.Clear();
            d.Footprint.TriggerCells.Clear();
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var cell = new Vector2Int(x, y);
                    d.Footprint.OccupiedCells.Add(cell);
                    if (hazard) d.Footprint.HazardCells.Add(cell);
                    if (trigger) d.Footprint.TriggerCells.Add(cell);
                }
            }

            d.VisualProfile.VisualSizeCells = new Vector2(width, height);
            d.VisualProfile.VisualOffsetCells = new Vector2((width - 1) * 0.5f, (height - 1) * 0.5f);
        }

        private static void AddSolidBox(MapElementDefinition d, Vector2 offset, Vector2 size)
        {
            d.CollisionProfile.IsSolid = true;
            d.CollisionProfile.SolidShapes.Add(new SerializedColliderShape
            {
                ShapeType = SerializedColliderShapeType.Box,
                OffsetCells = offset,
                SizeCells = size,
            });
        }

        private static void AddTriggerBox(MapElementDefinition d, Vector2 offset, Vector2 size)
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
            ToolTag tools,
            ElementReactionType reaction,
            int strength,
            string resultState = "")
        {
            d.ToolReactions.Entries.Add(new ToolReactionEntry
            {
                Tool = tools,
                Reaction = reaction,
                StrengthRequired = Mathf.Max(1, strength),
                ResultState = resultState,
            });
        }

        private static void SetTint(MapElementDefinition d, Color color)
        {
            d.VisualProfile.Tint = color;
        }
    }
}

#endif
