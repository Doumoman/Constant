#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Interaction.Targeting;
using StarNight.Map;
using StarNight.Tools.HookLauncher;
using StarNight.Tools.Rope;
using StarNight.Tools.Watering;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    public static class MapElementValidator
    {
        private const float ColliderTolerance = 0.01f;
        private const float PathSnapTolerance = 0.05f;
        private static readonly Rect PreviewBounds = new Rect(-16f, -9f, 32f, 18f);

        public static MapElementValidationReport ValidateSourceForBake(
            MapElementDefinition definition,
            GameObject sourceRoot = null)
        {
            var subject = definition != null ? definition.ElementId : "Missing Definition";
            var report = new MapElementValidationReport(subject);
            if (definition == null)
            {
                report.Add(ValidationSeverity.Error, "ELEMENT_NULL", "MapElementDefinition이 없습니다.");
                return report;
            }

            var assetPath = AssetDatabase.GetAssetPath(definition);
            ValidateIdentity(definition, assetPath, report);
            ValidateProfiles(definition, assetPath, report);
            ValidateFootprint(definition, assetPath, report);
            ValidateCollision(definition, assetPath, report);
            ValidateBehavior(definition, assetPath, report);
            ValidateCommonElement(definition, assetPath, report);
            ValidateMaruElement(definition, assetPath, report);
            ValidateMoonElement(definition, assetPath, report);
            ValidateBridgeElement(definition, assetPath, report);
            ValidatePalaceElement(definition, assetPath, report);
            ValidatePostElement(definition, assetPath, report);
            ValidateSunElement(definition, assetPath, report);
            ValidatePolarisElement(definition, assetPath, report);
            ValidateToolReactions(definition, assetPath, report);
            ValidateSourceTransform(sourceRoot, definition, report);
            PixelScaleValidator.Validate(sourceRoot, definition, report);
            return report;
        }

        public static MapElementValidationReport ValidateBakedDefinition(
            MapElementDefinition definition)
        {
            var report = ValidateSourceForBake(definition);
            if (definition == null || !AssetPathUtility.IsSafeFileName(definition.ElementId))
            {
                return report;
            }

            var paths = AssetPathUtility.GetMapElementBakePaths(definition);
            if (definition.RuntimePrefab == null)
            {
                report.Add(
                    ValidationSeverity.Error,
                    "RUNTIME_PREFAB_MISSING",
                    "Runtime Prefab 참조가 없습니다.",
                    paths.Definition,
                    definition);
            }
            else if (!string.Equals(
                         AssetDatabase.GetAssetPath(definition.RuntimePrefab),
                         paths.RuntimePrefab,
                         StringComparison.OrdinalIgnoreCase))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "RUNTIME_PREFAB_PATH",
                    $"Runtime Prefab 경로가 규약과 다릅니다: {paths.RuntimePrefab}",
                    paths.Definition,
                    definition.RuntimePrefab);
            }

            if (definition.BakedVisualProfile == null)
            {
                report.Add(
                    ValidationSeverity.Error,
                    "VISUAL_PROFILE_MISSING",
                    "Baked Visual Profile 참조가 없습니다.",
                    paths.Definition,
                    definition);
            }

            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths.SourcePrefab);
            if (sourcePrefab == null)
            {
                report.Add(
                    ValidationSeverity.Error,
                    "SOURCE_PREFAB_MISSING",
                    $"Source Prefab이 없습니다: {paths.SourcePrefab}",
                    paths.SourcePrefab);
            }
            else
            {
                var currentHash = BakeHashUtility.ComputeAssetFileHash(paths.SourcePrefab);
                if (definition.BakeMetadata == null ||
                    string.IsNullOrWhiteSpace(definition.BakeMetadata.SourceHash) ||
                    !string.Equals(
                        currentHash,
                        definition.BakeMetadata.SourceHash,
                        StringComparison.OrdinalIgnoreCase))
                {
                    report.Add(
                        ValidationSeverity.Error,
                        "SOURCE_HASH_MISMATCH",
                        "Source Prefab과 기록된 Bake Hash가 일치하지 않습니다.",
                        paths.SourcePrefab,
                        sourcePrefab);
                }
            }

            ValidateRuntimePrefab(definition, paths, report);
            return report;
        }

        public static MapElementValidationReport ValidateAllDefinitions()
        {
            var report = new MapElementValidationReport("All Map Element Data");
            var guids = AssetDatabase.FindAssets($"t:{nameof(MapElementDefinition)}");
            for (var index = 0; index < guids.Length; index++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[index]);
                var definition = AssetDatabase.LoadAssetAtPath<MapElementDefinition>(path);
                report.Merge(path.StartsWith("Assets/_Game/Map/Data/Elements/", StringComparison.OrdinalIgnoreCase)
                    ? ValidateBakedDefinition(definition)
                    : ValidateSourceForBake(definition));
            }

            return report;
        }

        public static int ApplyAllowedAutoFixes(
            MapElementDefinition definition,
            GameObject sourceRoot = null)
        {
            if (definition == null)
            {
                return 0;
            }

            var fixCount = 0;
            Undo.RecordObject(definition, "Auto Fix Map Element");
            fixCount += SnapPathNodes(definition);
            fixCount += ClampSmallColliderDrift(definition);
            fixCount += NormalizeSpriteScale(definition, sourceRoot);
            if (fixCount > 0)
            {
                EditorUtility.SetDirty(definition);
                if (sourceRoot != null)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sourceRoot.scene);
                }
            }

            return fixCount;
        }

        private static void ValidateIdentity(
            MapElementDefinition definition,
            string assetPath,
            MapElementValidationReport report)
        {
            if (!AssetPathUtility.IsSafeFileName(definition.ElementId))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "ELEMENT_ID_INVALID",
                    "Element ID는 지역 Prefix와 '_'를 포함한 안전한 파일명이어야 합니다.",
                    assetPath,
                    definition);
                return;
            }

            var guids = AssetDatabase.FindAssets($"t:{nameof(MapElementDefinition)}");
            for (var index = 0; index < guids.Length; index++)
            {
                var otherPath = AssetDatabase.GUIDToAssetPath(guids[index]);
                var other = AssetDatabase.LoadAssetAtPath<MapElementDefinition>(otherPath);
                if (other == null || other == definition ||
                    !string.Equals(other.ElementId, definition.ElementId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (IsAuthoringBakePair(definition, assetPath, otherPath))
                {
                    continue;
                }

                report.Add(
                    ValidationSeverity.Error,
                    "ELEMENT_ID_DUPLICATE",
                    $"Element ID가 중복됩니다: {otherPath}",
                    assetPath,
                    definition);
                break;
            }
        }

        private static bool IsAuthoringBakePair(
            MapElementDefinition definition,
            string firstPath,
            string secondPath)
        {
            if (string.IsNullOrWhiteSpace(firstPath) || string.IsNullOrWhiteSpace(secondPath))
            {
                return false;
            }

            var paths = AssetPathUtility.GetMapElementBakePaths(definition);
            var firstIsAuthoring = firstPath.StartsWith(
                "Assets/_Game/Editor/MapAuthoring/SourceElements/",
                StringComparison.OrdinalIgnoreCase);
            var secondIsAuthoring = secondPath.StartsWith(
                "Assets/_Game/Editor/MapAuthoring/SourceElements/",
                StringComparison.OrdinalIgnoreCase);
            return (firstIsAuthoring && string.Equals(secondPath, paths.Definition, StringComparison.OrdinalIgnoreCase)) ||
                   (secondIsAuthoring && string.Equals(firstPath, paths.Definition, StringComparison.OrdinalIgnoreCase));
        }

        private static void ValidateProfiles(
            MapElementDefinition definition,
            string assetPath,
            MapElementValidationReport report)
        {
            if (definition.VisualProfile == null || definition.CollisionProfile == null ||
                definition.BehaviorProfile == null || definition.PlacementProfile == null ||
                definition.BudgetProfile == null || definition.CommonProfile == null ||
                definition.MaruProfile == null || definition.MoonProfile == null ||
                definition.BridgeProfile == null || definition.PalaceProfile == null ||
                definition.PostProfile == null ||
                definition.SunProfile == null ||
                definition.PolarisProfile == null ||
                definition.ToolReactions == null ||
                definition.MaruReaction == null || definition.BakeMetadata == null)
            {
                report.Add(
                    ValidationSeverity.Error,
                    "PROFILE_MISSING",
                    "필수 Map Element Profile 중 하나 이상이 없습니다.",
                    assetPath,
                    definition);
                return;
            }

            if (string.IsNullOrWhiteSpace(definition.VisualProfile.SortingLayerName) ||
                !SortingLayer.layers.Any(layer =>
                    string.Equals(layer.name, definition.VisualProfile.SortingLayerName, StringComparison.Ordinal)))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "SORTING_LAYER_MISSING",
                    $"Sorting Layer가 없습니다: {definition.VisualProfile.SortingLayerName}",
                    assetPath,
                    definition);
            }
        }

        private static void ValidateFootprint(
            MapElementDefinition definition,
            string assetPath,
            MapElementValidationReport report)
        {
            var footprint = definition.Footprint;
            if (footprint == null)
            {
                report.Add(ValidationSeverity.Error, "FOOTPRINT_MISSING", "Footprint가 없습니다.", assetPath, definition);
                return;
            }

            if (!footprint.TryValidate(out var error))
            {
                report.Add(ValidationSeverity.Error, "FOOTPRINT_INVALID", error, assetPath, definition);
            }
        }

        private static void ValidateCollision(
            MapElementDefinition definition,
            string assetPath,
            MapElementValidationReport report)
        {
            var profile = definition.CollisionProfile;
            if (profile == null || definition.Footprint == null)
            {
                return;
            }

            ValidateShapes(profile.SolidShapes, "Solid", definition, assetPath, report);
            ValidateShapes(profile.TriggerShapes, "Trigger", definition, assetPath, report);

            var visualSize = definition.VisualProfile != null
                ? definition.VisualProfile.VisualSizeCells
                : Vector2.one;
            for (var index = 0; index < profile.TriggerShapes.Count; index++)
            {
                var trigger = profile.TriggerShapes[index];
                if (trigger != null &&
                    (trigger.SizeCells.x > visualSize.x + 0.5f ||
                     trigger.SizeCells.y > visualSize.y + 0.5f))
                {
                    report.Add(
                        ValidationSeverity.Warning,
                        "TRIGGER_VISUAL_OVERSIZE",
                        $"Trigger {index}가 Visual보다 지나치게 큽니다.",
                        assetPath,
                        definition);
                }
            }
        }

        private static void ValidateShapes(
            IReadOnlyList<SerializedColliderShape> shapes,
            string label,
            MapElementDefinition definition,
            string assetPath,
            MapElementValidationReport report)
        {
            if (shapes == null)
            {
                report.Add(ValidationSeverity.Error, "COLLIDER_LIST_NULL", $"{label} Shape 목록이 없습니다.", assetPath, definition);
                return;
            }

            var footprint = definition.Footprint;
            var minimum = new Vector2(-footprint.PivotCell.x - 0.5f, -footprint.PivotCell.y - 0.5f);
            var maximum = minimum + footprint.BoundsSize;
            for (var index = 0; index < shapes.Count; index++)
            {
                var shape = shapes[index];
                if (shape == null || shape.SizeCells.x <= 0f || shape.SizeCells.y <= 0f)
                {
                    report.Add(
                        ValidationSeverity.Error,
                        "COLLIDER_SIZE_INVALID",
                        $"{label} Shape {index} 크기가 유효하지 않습니다.",
                        assetPath,
                        definition);
                    continue;
                }

                var half = shape.SizeCells * 0.5f;
                var shapeMinimum = shape.OffsetCells - half;
                var shapeMaximum = shape.OffsetCells + half;
                if (shapeMinimum.x < minimum.x - ColliderTolerance ||
                    shapeMinimum.y < minimum.y - ColliderTolerance ||
                    shapeMaximum.x > maximum.x + ColliderTolerance ||
                    shapeMaximum.y > maximum.y + ColliderTolerance)
                {
                    if (label == "Trigger" && IsExternalVolumeElement(definition))
                    {
                        continue;
                    }

                    report.Add(
                        ValidationSeverity.Error,
                        "COLLIDER_OUT_OF_BOUNDS",
                        $"{label} Shape {index}가 Footprint 경계를 0.01셀 넘었습니다.",
                        assetPath,
                        definition,
                        autoFixable: IsSmallColliderDrift(shapeMinimum, shapeMaximum, minimum, maximum));
                }
            }
        }

        private static bool IsExternalVolumeElement(MapElementDefinition definition)
        {
            var kind = definition.CommonProfile != null
                ? definition.CommonProfile.Kind
                : CommonElementKind.None;
            var bridgeKind = definition.BridgeProfile != null
                ? definition.BridgeProfile.Kind
                : BridgeElementKind.None;
            var sunKind = definition.SunProfile != null
                ? definition.SunProfile.Kind
                : SunElementKind.None;
            return kind == CommonElementKind.WindVent || kind == CommonElementKind.WaterVent ||
                   bridgeKind == BridgeElementKind.FeatherUpdraft ||
                   sunKind == SunElementKind.ShadowSeed;
        }

        private static void ValidateBehavior(
            MapElementDefinition definition,
            string assetPath,
            MapElementValidationReport report)
        {
            var behavior = definition.BehaviorProfile;
            if (behavior == null)
            {
                return;
            }

            if (definition.Category == ElementCategory.Hazard &&
                definition.CollisionProfile.TriggerShapes.Count > 0 &&
                behavior.WarningSeconds <= 0f)
            {
                report.Add(
                    ValidationSeverity.Error,
                    "HAZARD_WARNING_ZERO",
                    "직접 위험 요소의 Warning 시간은 0보다 커야 합니다.",
                    assetPath,
                    definition);
            }

            var nodes = behavior.Path?.Nodes;
            if (nodes == null)
            {
                return;
            }

            for (var index = 0; index < nodes.Count; index++)
            {
                var node = nodes[index];
                var snapped = new Vector2(
                    Mathf.Round(node.x * 2f) * 0.5f,
                    Mathf.Round(node.y * 2f) * 0.5f);
                var distance = Vector2.Distance(node, snapped);
                if (distance > PathSnapTolerance)
                {
                    report.Add(
                        ValidationSeverity.Error,
                        "PATH_NODE_OFF_SNAP",
                        $"Path Node {index}가 정수/0.5셀 스냅 허용치 밖입니다.",
                        assetPath,
                        definition);
                }
                else if (distance > 0.0001f)
                {
                    report.Add(
                        ValidationSeverity.Warning,
                        "PATH_NODE_SNAP_FIX",
                        $"Path Node {index}를 {snapped}로 자동 보정할 수 있습니다.",
                        assetPath,
                        definition,
                        autoFixable: true);
                }

                if (!PreviewBounds.Contains(node))
                {
                    report.Add(
                        ValidationSeverity.Error,
                        "PATH_SWEPT_BOUNDS",
                        $"Path Node {index}가 32×18 Preview Bounds 밖입니다.",
                        assetPath,
                        definition);
                }
            }
        }

        private static void ValidateToolReactions(
            MapElementDefinition definition,
            string assetPath,
            MapElementValidationReport report)
        {
            var entries = definition.ToolReactions?.Entries;
            if (entries == null)
            {
                return;
            }

            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                if (entry == null)
                {
                    continue;
                }

                if (entry.Tool == ToolTag.None ||
                    (entry.Tool & ~ToolReactionMatrix.KnownToolMask) != 0)
                {
                    report.Add(
                        ValidationSeverity.Error,
                        "TOOL_TAG_INVALID",
                        $"Tool Reaction {index} has no valid ToolTag.",
                        assetPath,
                        definition);
                }

                if (entry.Reaction == ElementReactionType.None)
                {
                    report.Add(
                        ValidationSeverity.Error,
                        "TOOL_REACTION_UNDEFINED",
                        $"Tool Reaction {index} stores an undefined response. Remove the row instead.",
                        assetPath,
                        definition);
                }

                if (entry.StrengthRequired < 1)
                {
                    report.Add(
                        ValidationSeverity.Error,
                        "TOOL_STRENGTH_INVALID",
                        $"Tool Reaction {index} requires at least one hit.",
                        assetPath,
                        definition);
                }

                if (entry.Reaction == ElementReactionType.SetState &&
                    !Enum.TryParse(entry.ResultState, true, out MapElementState _))
                {
                    report.Add(
                        ValidationSeverity.Error,
                        "TOOL_STATE_UNKNOWN",
                        $"Tool Reaction {index}의 State가 존재하지 않습니다: {entry.ResultState}",
                        assetPath,
                        definition);
                }

                if (entry.Reaction != ElementReactionType.None && entry.ReactionVfx == null)
                {
                    report.Add(
                        ValidationSeverity.Warning,
                        "TOOL_VFX_MISSING",
                        $"Tool Reaction {index}의 VFX 참조가 없습니다.",
                        assetPath,
                        definition);
                }
            }

            for (var toolIndex = 0; toolIndex < ToolReactionMatrix.AtomicTools.Length; toolIndex++)
            {
                var tool = ToolReactionMatrix.AtomicTools[toolIndex];
                var matchCount = entries.Count(entry => entry != null &&
                    entry.Reaction != ElementReactionType.None && (entry.Tool & tool) != 0);
                if (matchCount > 1)
                {
                    report.Add(
                        ValidationSeverity.Error,
                        "TOOL_REACTION_AMBIGUOUS",
                        $"{tool} is defined by {matchCount} reaction rows. One atomic tag must resolve to exactly one row.",
                        assetPath,
                        definition);
                }
            }

            ValidateRequiredReactionMatrix(definition, assetPath, report);
        }

        private static void ValidateRequiredReactionMatrix(
            MapElementDefinition definition,
            string assetPath,
            MapElementValidationReport report)
        {
            var common = definition.CommonProfile;
            if (common != null && common.Kind != CommonElementKind.None)
            {
                switch (common.Kind)
                {
                    case CommonElementKind.SolidBlock:
                    case CommonElementKind.UnbreakableBlock:
                    case CommonElementKind.OneWayPlatform:
                    case CommonElementKind.PressurePlate:
                    case CommonElementKind.WeightDoor:
                    case CommonElementKind.Spike:
                    case CommonElementKind.BouncePad:
                        RequireNoToolReaction(definition, assetPath, report, common.Kind.ToString());
                        break;

                    case CommonElementKind.CrackedBlock:
                        RequireReaction(definition, assetPath, report, ToolTag.Bomb,
                            ElementReactionType.Break, 1);
                        RequireReaction(definition, assetPath, report, ToolTag.Pickaxe,
                            ElementReactionType.Break, 1);
                        RequireReaction(definition, assetPath, report, ToolTag.Pound,
                            ElementReactionType.Break, 1);
                        RequireReaction(definition, assetPath, report, ToolTag.HeavyImpact,
                            ElementReactionType.Break, 1);
                        break;

                    case CommonElementKind.SoftSoil:
                        RequireReaction(definition, assetPath, report, ToolTag.Shovel,
                            ElementReactionType.Break, 1);
                        RequireReaction(definition, assetPath, report, ToolTag.Bomb,
                            ElementReactionType.SetState, 1, "AbsorbExplosion");
                        RequireReaction(definition, assetPath, report, ToolTag.Pickaxe,
                            ElementReactionType.SetState, 1, "SoftSoil");
                        RequireReaction(definition, assetPath, report, ToolTag.LightImpact,
                            ElementReactionType.SetState, 1, "CushionImpact");
                        RequireReaction(definition, assetPath, report, ToolTag.HeavyImpact,
                            ElementReactionType.SetState, 1, "CushionImpact");
                        break;

                    case CommonElementKind.FragileFloor:
                        RequireReaction(definition, assetPath, report, ToolTag.Bomb,
                            ElementReactionType.Break, 1);
                        RequireReaction(definition, assetPath, report, ToolTag.Pickaxe,
                            ElementReactionType.Break, 1);
                        RequireReaction(definition, assetPath, report, ToolTag.Pound,
                            ElementReactionType.Break, 1);
                        RequireReaction(definition, assetPath, report, ToolTag.HeavyImpact,
                            ElementReactionType.Break, 1);
                        break;

                    case CommonElementKind.Lever:
                        RequireReaction(definition, assetPath, report, ToolTag.Hook,
                            ElementReactionType.Toggle, 1);
                        break;

                    case CommonElementKind.MovingPlatform:
                        RequireReaction(definition, assetPath, report, ToolTag.Hook,
                            ElementReactionType.Pull, 1);
                        break;

                    case CommonElementKind.FallingStone:
                        RequireReaction(definition, assetPath, report, ToolTag.Bomb,
                            ElementReactionType.SetState, 1);
                        RequireReaction(definition, assetPath, report, ToolTag.HeavyImpact,
                            ElementReactionType.SetState, 1);
                        break;

                    case CommonElementKind.PendulumBall:
                        RequireReaction(definition, assetPath, report, ToolTag.Hook,
                            ElementReactionType.Pull, 1);
                        RequireReaction(definition, assetPath, report, ToolTag.Bomb,
                            ElementReactionType.Push, 1);
                        break;

                    case CommonElementKind.Crusher:
                        RequireReaction(definition, assetPath, report, ToolTag.Hook,
                            ElementReactionType.SetState, 1, "Warning");
                        break;

                    case CommonElementKind.PulleyLift:
                        RequireReaction(definition, assetPath, report, ToolTag.Hook,
                            ElementReactionType.Toggle, 1);
                        break;

                    case CommonElementKind.TotemShooter:
                        RequireReaction(definition, assetPath, report, ToolTag.Bomb,
                            ElementReactionType.Break, 1);
                        RequireReaction(definition, assetPath, report, ToolTag.Pickaxe,
                            ElementReactionType.Break, 2);
                        RequireReaction(definition, assetPath, report, ToolTag.Pound,
                            ElementReactionType.Disable, 1);
                        break;

                    case CommonElementKind.LaserEmitter:
                        RequireReaction(definition, assetPath, report, ToolTag.Bomb,
                            ElementReactionType.Disable, 1);
                        RequireReaction(definition, assetPath, report, ToolTag.Pickaxe,
                            ElementReactionType.Disable, 2);
                        RequireReaction(definition, assetPath, report, ToolTag.HeavyImpact,
                            ElementReactionType.Toggle, 1, "Rotate");
                        break;

                    case CommonElementKind.RollingBoulder:
                        RequireReaction(definition, assetPath, report, ToolTag.Bomb,
                            ElementReactionType.SetState, 1, "Active");
                        RequireReaction(definition, assetPath, report, ToolTag.Hook,
                            ElementReactionType.Pull, 1);
                        break;

                    case CommonElementKind.WindVent:
                        RequireReaction(definition, assetPath, report, ToolTag.WindGuard,
                            ElementReactionType.Move, 1, "WindAssist");
                        break;

                    case CommonElementKind.WaterVent:
                        RequireReaction(definition, assetPath, report, ToolTag.Context,
                            ElementReactionType.SetState, 1, "Active");
                        break;

                    case CommonElementKind.RopeAnchor:
                        RequireReaction(definition, assetPath, report, ToolTag.Rope,
                            ElementReactionType.SetState, 1, "Active");
                        break;

                    case CommonElementKind.HookAnchor:
                        RequireReaction(definition, assetPath, report, ToolTag.Hook,
                            ElementReactionType.Pull, 1);
                        break;

                    case CommonElementKind.BreakableContainer:
                        RequireReaction(definition, assetPath, report, ToolTag.LightImpact,
                            ElementReactionType.Break, 1);
                        RequireReaction(definition, assetPath, report, ToolTag.HeavyImpact,
                            ElementReactionType.Break, 1);
                        RequireReaction(definition, assetPath, report, ToolTag.Pickaxe,
                            ElementReactionType.Break, 1);
                        RequireReaction(definition, assetPath, report, ToolTag.Pound,
                            ElementReactionType.Break, 1);
                        RequireReaction(definition, assetPath, report, ToolTag.Bomb,
                            ElementReactionType.Break, 1);
                        break;

                    case CommonElementKind.ExitGuideLantern:
                        RequireReaction(definition, assetPath, report, ToolTag.Context,
                            ElementReactionType.SetState, 1, "Active");
                        break;
                }
            }

            var maru = definition.MaruProfile;
            if (maru == null || maru.Kind == MaruElementKind.None)
            {
                return;
            }

            switch (maru.Kind)
            {
                case MaruElementKind.ReturnStatue:
                    RequireReaction(definition, assetPath, report, ToolTag.Bomb,
                        ElementReactionType.Break, 1);
                    RequireReaction(definition, assetPath, report, ToolTag.Pickaxe,
                        ElementReactionType.Break, 2);
                    RequireReaction(definition, assetPath, report, ToolTag.Pound,
                        ElementReactionType.Break, 2);
                    RequireReaction(definition, assetPath, report, ToolTag.HeavyImpact,
                        ElementReactionType.Break, 2);
                    RequireReaction(definition, assetPath, report, ToolTag.Hook,
                        ElementReactionType.Pull, 1);
                    break;

                case MaruElementKind.ReturnBellJar:
                    RequireReaction(definition, assetPath, report, ToolTag.Bomb,
                        ElementReactionType.Break, 1);
                    RequireReaction(definition, assetPath, report, ToolTag.Pickaxe,
                        ElementReactionType.Break, 1);
                    RequireReaction(definition, assetPath, report, ToolTag.LightImpact,
                        ElementReactionType.Break, 1);
                    RequireReaction(definition, assetPath, report, ToolTag.HeavyImpact,
                        ElementReactionType.Break, 1);
                    break;

                case MaruElementKind.CollarFragment:
                case MaruElementKind.ReturnMarker:
                case MaruElementKind.PawprintPool:
                case MaruElementKind.RecordCasket:
                    RequireNoToolReaction(definition, assetPath, report, maru.Kind.ToString());
                    break;
            }
        }

        private static void RequireReaction(
            MapElementDefinition definition,
            string assetPath,
            MapElementValidationReport report,
            ToolTag tool,
            ElementReactionType reaction,
            int strength,
            string resultState = null)
        {
            if (definition.ToolReactions != null &&
                definition.ToolReactions.TryResolve(tool, out var entry, out _) &&
                entry.Reaction == reaction && entry.StrengthRequired == strength &&
                (resultState == null || string.Equals(
                    entry.ResultState, resultState, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            report.Add(
                ValidationSeverity.Error,
                "TOOL_REQUIRED_REACTION_MISSING",
                $"Required reaction is missing or mismatched: {tool} -> {reaction} x{strength}" +
                (resultState == null ? string.Empty : $" ({resultState})"),
                assetPath,
                definition);
        }

        private static void RequireNoToolReaction(
            MapElementDefinition definition,
            string assetPath,
            MapElementValidationReport report,
            string contractName)
        {
            if (definition.ToolReactions?.Entries == null ||
                definition.ToolReactions.Entries.All(entry => entry == null ||
                    entry.Reaction == ElementReactionType.None))
            {
                return;
            }

            report.Add(
                ValidationSeverity.Error,
                "TOOL_FORBIDDEN_REACTION_DEFINED",
                $"{contractName} must reject every direct ToolReaction.",
                assetPath,
                definition);
        }

        private static void ValidateCommonElement(
            MapElementDefinition definition,
            string assetPath,
            MapElementValidationReport report)
        {
            var profile = definition.CommonProfile;
            if (profile == null || profile.Kind == CommonElementKind.None)
            {
                return;
            }

            if ((profile.Kind == CommonElementKind.FallingStone ||
                 profile.Kind == CommonElementKind.Spike ||
                 profile.Kind == CommonElementKind.TotemShooter ||
                 profile.Kind == CommonElementKind.LaserEmitter ||
                 profile.Kind == CommonElementKind.PendulumBall ||
                 profile.Kind == CommonElementKind.Crusher ||
                 profile.Kind == CommonElementKind.RollingBoulder) &&
                profile.Damage != 1)
            {
                report.Add(
                    ValidationSeverity.Error,
                    "COMMON_DAMAGE_NOT_ONE",
                    "공용 위험 요소의 피해량은 정확히 1이어야 합니다.",
                    assetPath,
                    definition);
            }

            if ((profile.Kind == CommonElementKind.FragileFloor ||
                 profile.Kind == CommonElementKind.FallingStone ||
                 profile.Kind == CommonElementKind.TotemShooter ||
                 profile.Kind == CommonElementKind.LaserEmitter ||
                 profile.Kind == CommonElementKind.Crusher) &&
                definition.BehaviorProfile.WarningSeconds <= 0f)
            {
                report.Add(
                    ValidationSeverity.Error,
                    "COMMON_WARNING_MISSING",
                    "전조가 필요한 공용 요소에 Warning 시간이 없습니다.",
                    assetPath,
                    definition);
            }

            if ((profile.Kind == CommonElementKind.Lever ||
                 profile.Kind == CommonElementKind.PressurePlate ||
                 profile.Kind == CommonElementKind.WeightDoor ||
                 profile.Kind == CommonElementKind.Crusher ||
                 profile.Kind == CommonElementKind.PulleyLift) &&
                string.IsNullOrWhiteSpace(profile.SignalChannel))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "COMMON_SIGNAL_TARGET_MISSING",
                    "퍼즐 요소의 신호 대상 채널이 비어 있습니다.",
                    assetPath,
                    definition);
            }

            if (profile.Kind == CommonElementKind.OneWayPlatform &&
                !definition.CollisionProfile.IsOneWay)
            {
                report.Add(
                    ValidationSeverity.Error,
                    "COMMON_ONE_WAY_FLAG_MISSING",
                    "단방향 플랫폼에 IsOneWay가 설정되지 않았습니다.",
                    assetPath,
                    definition);
            }

            if (profile.Kind == CommonElementKind.SoftSoil &&
                (definition.PlacementProfile.AllowMainRoute ||
                 definition.PlacementProfile.MinimumPortalDistanceCells < 2 ||
                 !definition.PlacementProfile.ForbiddenNeighborTags.Contains("UnbreakableBoundary") ||
                 !definition.PlacementProfile.ForbiddenNeighborTags.Contains("VoidRecoveryZone")))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "COMMON_SOFT_SOIL_TERRAIN_SAFETY",
                    "Soft Soil must stay off the main route and remain at least two cells from portals, UnbreakableBoundary, and VoidRecoveryZone.",
                    assetPath,
                    definition);
            }

            if ((profile.Kind == CommonElementKind.WindVent ||
                 profile.Kind == CommonElementKind.WaterVent) &&
                (profile.VolumeSizeCells.x < 1f || profile.VolumeSizeCells.y < 2f))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "COMMON_VOLUME_TOO_SMALL",
                    "분출구 영향 볼륨은 최소 1x2 셀이어야 합니다.",
                    assetPath,
                    definition);
            }

            if (profile.Kind == CommonElementKind.LaserEmitter &&
                (profile.SightOrBeamRangeCells < 2f || profile.SightOrBeamRangeCells > 12f))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "COMMON_LASER_RANGE",
                    "레이저 사거리는 2~12 셀이어야 합니다.",
                    assetPath,
                    definition);
            }

            if (profile.Kind == CommonElementKind.PendulumBall &&
                (profile.ChainLengthCells < 2 || profile.ChainLengthCells > 5 ||
                 profile.SwingArcDegrees < 0f || profile.SwingArcDegrees > 55f ||
                 profile.SwingPeriodSeconds <= 0f))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "COMMON_PENDULUM_CONTRACT",
                    "Pendulum Ball requires a 2-5 cell chain, up to 55 degrees, and a positive period.",
                    assetPath,
                    definition);
            }

            if (profile.Kind == CommonElementKind.Crusher &&
                (Mathf.Abs(definition.BehaviorProfile.WarningSeconds - 0.6f) > 0.0001f ||
                 profile.MoveSpeedCellsPerSecond <= 0f ||
                 profile.HoldSeconds <= 0f ||
                 profile.ReturnSpeedCellsPerSecond <= 0f ||
                 !definition.PlacementProfile.RequiredNeighborTags.Contains("EscapeCell")))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "COMMON_CRUSHER_CONTRACT",
                    "Crusher requires its warning, movement/hold/return timing, and one escape cell.",
                    assetPath,
                    definition);
            }

            if (profile.Kind == CommonElementKind.PulleyLift &&
                (definition.Footprint.BoundsSize != new Vector2Int(2, 1) ||
                 profile.TravelCells < 3f || profile.TravelCells > 10f ||
                 profile.MoveSpeedCellsPerSecond <= 0f))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "COMMON_PULLEY_LIFT_CONTRACT",
                    "Pulley Lift requires a 2x1 platform, 3-10 cell travel, and positive speed.",
                    assetPath,
                    definition);
            }

            if (profile.Kind == CommonElementKind.RollingBoulder &&
                (profile.MaximumSpeedCellsPerSecond <= 0f ||
                 !definition.PlacementProfile.RequiredNeighborTags.Contains(
                     "StopPocketOrUnbreakableStopper")))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "COMMON_ROLLING_BOULDER_CONTRACT",
                    "Rolling Boulder requires a positive speed cap and a stop pocket or stopper.",
                    assetPath,
                    definition);
            }

            if (profile.Kind == CommonElementKind.ExitGuideLantern &&
                (definition.Footprint.BoundsSize != new Vector2Int(1, 2) ||
                 Mathf.Abs(profile.GuideDurationSeconds - 3f) > 0.0001f))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "COMMON_EXIT_GUIDE_CONTRACT",
                    "Exit Guide Lantern requires a 1x2 footprint and a 3 second guide.",
                    assetPath,
                    definition);
            }
        }

        private static void ValidateMaruElement(
            MapElementDefinition definition,
            string assetPath,
            MapElementValidationReport report)
        {
            var profile = definition.MaruProfile;
            if (profile == null || profile.Kind == MaruElementKind.None)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(profile.PreviewRewardText) ||
                string.IsNullOrWhiteSpace(profile.PreviewPenaltyText))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "MARU_OUTCOME_PREVIEW_MISSING",
                    "마루 요소는 보상과 패널티를 모두 사전에 표시해야 합니다.",
                    assetPath,
                    definition);
            }

            switch (profile.Kind)
            {
                case MaruElementKind.ReturnStatue:
                    if (profile.DurabilityStages != 2 || profile.RewardMoney != 500 ||
                        profile.MinimumExitRoomDistance != 3 || profile.MaximumExitRoomDistance != 5 ||
                        !profile.ForbidExitRoom)
                    {
                        report.Add(
                            ValidationSeverity.Error,
                            "MARU_STATUE_CONTRACT",
                            "귀향상은 내구 2, 500원, 출구 3~5방 거리, 출구방 금지 계약을 따라야 합니다.",
                            assetPath,
                            definition);
                    }
                    break;

                case MaruElementKind.ReturnBellJar:
                    if (profile.DurabilityStages != 1 || profile.RewardMoney != 300 ||
                        Mathf.Abs(profile.ScheduledEntryDelaySeconds - 12f) > 0.0001f ||
                        profile.MinimumAutomaticHazardDistanceCells < 3)
                    {
                        report.Add(
                            ValidationSeverity.Error,
                            "MARU_BELL_JAR_CONTRACT",
                            "방울단지는 HP 1, 300원, 12초 진입 예약, 자동 함정 3셀 이격이 필요합니다.",
                            assetPath,
                            definition);
                    }
                    break;

                case MaruElementKind.CollarFragment:
                    if (Mathf.Abs(profile.TimerRateMultiplier - 1.15f) > 0.0001f ||
                        profile.PressureWeight != 2)
                    {
                        report.Add(
                            ValidationSeverity.Error,
                            "MARU_COLLAR_CONTRACT",
                            "별목줄 파편은 Heavy(2)이며 소지 중 방울 타이머를 15% 가속해야 합니다.",
                            assetPath,
                            definition);
                    }
                    break;

                case MaruElementKind.ReturnMarker:
                    if (profile.MarkerCostValue <= 0)
                    {
                        report.Add(
                            ValidationSeverity.Error,
                            "MARU_MARKER_COST_MISSING",
                            "귀환 표식대에는 소지금 또는 체력 비용이 필요합니다.",
                            assetPath,
                            definition);
                    }
                    break;

                case MaruElementKind.PawprintPool:
                    if (definition.Footprint.BoundsSize != new Vector2Int(2, 1) ||
                        Mathf.Abs(profile.GuidanceSeconds - 4f) > 0.0001f ||
                        Mathf.Abs(profile.ShortenNextBellSeconds - 8f) > 0.0001f)
                    {
                        report.Add(
                            ValidationSeverity.Error,
                            "MARU_PAWPRINT_CONTRACT",
                            "발자국 웅덩이는 2x1, 출구 안내 4초, 다음 방울 8초 단축이어야 합니다.",
                            assetPath,
                            definition);
                    }
                    break;

                case MaruElementKind.RecordCasket:
                    if (profile.DurabilityStages != 2 || definition.PlacementProfile.AllowMainRoute)
                    {
                        report.Add(
                            ValidationSeverity.Error,
                            "MARU_CASKET_CONTRACT",
                            "별기록관의 관은 2단계 선택 요소이며 메인 경로 필수가 아니어야 합니다.",
                            assetPath,
                            definition);
                    }

                    var reactions = definition.ToolReactions?.Entries;
                    if (reactions != null && reactions.Any(entry => entry != null &&
                        (entry.Tool & (ToolTag.Bomb | ToolTag.Pickaxe)) != 0 &&
                        entry.Reaction != ElementReactionType.None))
                    {
                        report.Add(
                            ValidationSeverity.Error,
                            "MARU_CASKET_DESTRUCTIVE_TOOL",
                            "별기록관의 관은 폭탄·곡괭이로 열거나 파괴할 수 없습니다.",
                            assetPath,
                            definition);
                    }
                    break;
            }
        }

        private static void ValidateMoonElement(
            MapElementDefinition definition,
            string assetPath,
            MapElementValidationReport report)
        {
            var profile = definition.MoonProfile;
            if (profile == null || profile.Kind == MoonElementKind.None)
            {
                return;
            }

            if (definition.AllowedRegions != RegionMask.Moon)
            {
                report.Add(
                    ValidationSeverity.Error,
                    "MOON_REGION_CONTRACT",
                    "월궁 요소는 Moon 지역 전용이어야 합니다.",
                    assetPath,
                    definition);
            }

            if (profile.Damage != 1 &&
                (profile.Kind == MoonElementKind.MoonIronBall ||
                 profile.Kind == MoonElementKind.FallingMortar ||
                 profile.Kind == MoonElementKind.CassiaRoot ||
                 profile.Kind == MoonElementKind.MillShaft))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "MOON_DAMAGE_NOT_ONE",
                    "월궁 직접 위협 요소의 피해량은 1이어야 합니다.",
                    assetPath,
                    definition);
            }

            switch (profile.Kind)
            {
                case MoonElementKind.MoonIronBall:
                    if (profile.ChainLengthCells < 2 || profile.ChainLengthCells > 4 ||
                        profile.SwingArcDegrees > 50f ||
                        Mathf.Abs(profile.SwingPeriodSeconds - 2.6f) > 0.0001f ||
                        definition.PlacementProfile.MinimumSafeCellDistanceCells < profile.ChainLengthCells ||
                        !definition.PlacementProfile.ForbiddenNeighborTags.Contains("EntrySafeZone") ||
                        !HasReaction(definition, ToolTag.Hook, ElementReactionType.Pull))
                    {
                        AddMoonContractError(report, assetPath, definition,
                            "MOON_IRON_BALL_CONTRACT",
                            "달철구는 Chain 2~4, ±50도 이하, 2.6초 주기, EntrySafeZone 비침범, Hook 당김 계약이 필요합니다.");
                    }
                    break;

                case MoonElementKind.FallingMortar:
                    if (definition.Footprint.BoundsSize != new Vector2Int(2, 2) ||
                        Mathf.Abs(profile.ShadowWarningSeconds - 0.75f) > 0.0001f ||
                        !definition.PlacementProfile.ForbiddenNeighborTags.Contains("Crusher") ||
                        !HasReaction(definition, ToolTag.Bomb, ElementReactionType.SetState) ||
                        !HasReaction(definition, ToolTag.Pickaxe, ElementReactionType.SetState))
                    {
                        AddMoonContractError(report, assetPath, definition,
                            "MOON_FALLING_MORTAR_CONTRACT",
                            "낙하 절구는 2x2, 그림자 0.75초, Crusher 동실 금지, 폭탄·곡괭이 지지대 제거 계약이 필요합니다.");
                    }
                    break;

                case MoonElementKind.DoughPlatform:
                    if (profile.WidthCells < 1 || profile.WidthCells > 4 ||
                        definition.Footprint.BoundsSize.x != profile.WidthCells ||
                        Mathf.Abs(profile.CompressionCells - 0.4f) > 0.0001f ||
                        !definition.PlacementProfile.RequiredNeighborTags.Contains("FallLandingOrPuzzleResult") ||
                        !HasReaction(definition, ToolTag.Water, ElementReactionType.SetState) ||
                        !HasReaction(definition, ToolTag.Pound, ElementReactionType.SetState) ||
                        !HasReaction(definition, ToolTag.Bomb, ElementReactionType.Break))
                    {
                        AddMoonContractError(report, assetPath, definition,
                            "MOON_DOUGH_CONTRACT",
                            "달반죽은 1~4 폭, 0.4셀 압축, 착지점/퍼즐 결과 배치와 물·절굿공이·폭탄 반응이 필요합니다.");
                    }
                    break;

                case MoonElementKind.CraterSlab:
                    if (definition.Footprint.BoundsSize != new Vector2Int(2, 1) ||
                        Mathf.Abs(profile.FallDelaySeconds - 0.5f) > 0.0001f ||
                        !HasReaction(definition, ToolTag.HeavyImpact, ElementReactionType.SetState) ||
                        !HasReaction(definition, ToolTag.Bomb, ElementReactionType.SetState))
                    {
                        AddMoonContractError(report, assetPath, definition,
                            "MOON_CRATER_SLAB_CONTRACT",
                            "분화구 돌판은 2x1, 0.5초 기울기 후 낙하, HeavyImpact·폭탄 반응이 필요합니다.");
                    }
                    break;

                case MoonElementKind.CassiaRoot:
                    if (profile.MinimumSegmentCount != 2 || profile.SegmentCount < 2 || profile.SegmentCount > 8 ||
                        !definition.PlacementProfile.ForbiddenNeighborTags.Contains("Portal") ||
                        !HasReaction(definition, ToolTag.Water, ElementReactionType.SetState) ||
                        !HasReaction(definition, ToolTag.Pickaxe, ElementReactionType.Break) ||
                        !HasReaction(definition, ToolTag.Hook, ElementReactionType.Pull))
                    {
                        AddMoonContractError(report, assetPath, definition,
                            "MOON_CASSIA_ROOT_CONTRACT",
                            "계수나무 뿌리는 2~8 Segment, Portal 비차단, 물·곡괭이·Hook 반응이 필요합니다.");
                    }
                    break;

                case MoonElementKind.MillShaft:
                    if (definition.Footprint.BoundsSize != new Vector2Int(2, 2) ||
                        Mathf.Abs(profile.StepAngleDegrees - 90f) > 0.0001f ||
                        !HasReaction(definition, ToolTag.Hook, ElementReactionType.Toggle) ||
                        !HasReaction(definition, ToolTag.HeavyImpact, ElementReactionType.Disable))
                    {
                        AddMoonContractError(report, assetPath, definition,
                            "MOON_MILL_SHAFT_CONTRACT",
                            "방앗간 축은 2x2, 90도 회전, Hook Trigger와 Heavy 정지 반응이 필요합니다.");
                    }
                    break;

                case MoonElementKind.MedicineMortar:
                    if (definition.Footprint.BoundsSize != new Vector2Int(2, 2) ||
                        profile.InputSlots < 1 || string.IsNullOrWhiteSpace(profile.OutputId) ||
                        !HasReaction(definition, ToolTag.Context, ElementReactionType.SetState) ||
                        !HasReaction(definition, ToolTag.Pound, ElementReactionType.SetState))
                    {
                        AddMoonContractError(report, assetPath, definition,
                            "MOON_MEDICINE_MORTAR_CONTRACT",
                            "약절구는 2x2, 재료 슬롯·출력과 절굿공이 작동 계약이 필요합니다.");
                    }
                    break;

                case MoonElementKind.FlourVent:
                    if (definition.Footprint.BoundsSize != Vector2Int.one ||
                        Mathf.Abs(profile.CycleOnSeconds - 1.2f) > 0.0001f ||
                        Mathf.Abs(profile.CycleOffSeconds - 1f) > 0.0001f ||
                        profile.Direction == Vector2Int.zero || profile.ForceCellsPerSecond <= 0f ||
                        !HasReaction(definition, ToolTag.Water, ElementReactionType.Disable) ||
                        !HasReaction(definition, ToolTag.WindGuard, ElementReactionType.SetState))
                    {
                        AddMoonContractError(report, assetPath, definition,
                            "MOON_FLOUR_VENT_CONTRACT",
                            "밀가루 분출구는 1x1, 1.2초 분출/1.0초 정지, 방향·힘과 물 일시정지 계약이 필요합니다.");
                    }
                    break;
            }
        }

        private static void ValidateBridgeElement(
            MapElementDefinition definition,
            string assetPath,
            MapElementValidationReport report)
        {
            var profile = definition.BridgeProfile;
            if (profile == null || profile.Kind == BridgeElementKind.None)
            {
                return;
            }

            if (definition.AllowedRegions != RegionMask.Bridge)
            {
                report.Add(
                    ValidationSeverity.Error,
                    "BRIDGE_REGION_CONTRACT",
                    "오작교 요소는 Bridge 지역 전용이어야 합니다.",
                    assetPath,
                    definition);
            }

            switch (profile.Kind)
            {
                case BridgeElementKind.ThreadBridge:
                    if (profile.LengthCells < 2 || profile.LengthCells > 8 ||
                        definition.Footprint.BoundsSize != new Vector2Int(profile.LengthCells, 1) ||
                        Mathf.Abs(profile.SagCells - 0.3f) > 0.0001f || profile.MaxWeight < 1 ||
                        !definition.PlacementProfile.RequiredNeighborTags.Contains("AlternativeRouteOrVoidRecovery") ||
                        !HasReaction(definition, ToolTag.Pickaxe, ElementReactionType.Break) ||
                        !HasReaction(definition, ToolTag.Bomb, ElementReactionType.Break))
                    {
                        AddBridgeContractError(report, assetPath, definition,
                            "BRIDGE_THREAD_BRIDGE_CONTRACT",
                            "실다리는 2~8x1, 0.3셀 처짐, MaxWeight, 낙하 복구와 곡괭이·폭탄 절단 계약이 필요합니다.");
                    }
                    break;

                case BridgeElementKind.KnotPulley:
                    if (definition.Footprint.BoundsSize != new Vector2Int(2, 2) ||
                        profile.TravelCells <= 0f || profile.WeightRatio <= 0f ||
                        !HasReaction(definition, ToolTag.Hook, ElementReactionType.Toggle) ||
                        !HasReaction(definition, ToolTag.HeavyImpact, ElementReactionType.Move))
                    {
                        AddBridgeContractError(report, assetPath, definition,
                            "BRIDGE_KNOT_PULLEY_CONTRACT",
                            "매듭 도르래는 Control 1x2 + Platform 2x1, Travel·Weight Ratio, Hook·Heavy 균형 계약이 필요합니다.");
                    }
                    break;

                case BridgeElementKind.WindBanner:
                    if (definition.Footprint.BoundsSize != new Vector2Int(1, 2) ||
                        profile.Direction == Vector2Int.zero || !profile.FlipOnSignal ||
                        profile.WetForceMultiplier >= 1f || profile.UmbrellaAssistMultiplier <= 1f ||
                        !HasReaction(definition, ToolTag.Water, ElementReactionType.SetState) ||
                        !HasReaction(definition, ToolTag.WindGuard, ElementReactionType.SetState))
                    {
                        AddBridgeContractError(report, assetPath, definition,
                            "BRIDGE_WIND_BANNER_CONTRACT",
                            "바람깃발은 1x2, Signal 방향 전환, 물 약화와 우산 상승 보조 계약이 필요합니다.");
                    }
                    break;

                case BridgeElementKind.ThreadBlade:
                    if (definition.Footprint.BoundsSize != Vector2Int.one || profile.Damage != 1 ||
                        Mathf.Abs(profile.PathSpeedCellsPerSecond - 3f) > 0.0001f ||
                        profile.WarningSeconds <= 0f || profile.MinimumStrongCrosswindDistanceCells < 6 ||
                        !definition.PlacementProfile.ForbiddenNeighborTags.Contains("StrongCrosswindWithin6Cells") ||
                        definition.BehaviorProfile.Path?.Nodes == null ||
                        definition.BehaviorProfile.Path.Nodes.Count < 2)
                    {
                        AddBridgeContractError(report, assetPath, definition,
                            "BRIDGE_THREAD_BLADE_CONTRACT",
                            "실칼날은 1x1, 경로 3 cells/sec, 피해 1, 전조와 강한 횡풍 6셀 중첩 금지 계약이 필요합니다.");
                    }
                    break;

                case BridgeElementKind.MagpiePlatform:
                    if (profile.PlatformWidthCells < 1 || profile.PlatformWidthCells > 2 ||
                        definition.Footprint.BoundsSize != new Vector2Int(profile.PlatformWidthCells, 1) ||
                        profile.StopCount < 2 || profile.WaitTimeSeconds < 0f || profile.HeavyDescentMultiplier <= 1f ||
                        definition.BehaviorProfile.Path?.Nodes == null ||
                        definition.BehaviorProfile.Path.Nodes.Count < profile.StopCount ||
                        !definition.PlacementProfile.RequiredNeighborTags.Contains("BaseRouteFallback") ||
                        !HasReaction(definition, ToolTag.HeavyImpact, ElementReactionType.Move))
                    {
                        AddBridgeContractError(report, assetPath, definition,
                            "BRIDGE_MAGPIE_PLATFORM_CONTRACT",
                            "까치 발판은 1~2x1, 복수 정류장, 대기 시간, Heavy 빠른 하강과 기본 경로 계약이 필요합니다.");
                    }
                    break;

                case BridgeElementKind.FeatherUpdraft:
                    if (definition.Footprint.BoundsSize != Vector2Int.one ||
                        profile.VolumeSizeCells != new Vector2(2f, 4f) ||
                        profile.ForceCellsPerSecond <= 0f || profile.UmbrellaLiftMultiplier <= 1f ||
                        !HasReaction(definition, ToolTag.WindGuard, ElementReactionType.SetState))
                    {
                        AddBridgeContractError(report, assetPath, definition,
                            "BRIDGE_FEATHER_UPDRAFT_CONTRACT",
                            "별깃털 상승류는 Source 1x1/Volume 2x4, 지속 상승 Force와 우산 배율 계약이 필요합니다.");
                    }
                    break;

                case BridgeElementKind.BreakingStarPanel:
                    if (definition.Footprint.BoundsSize != Vector2Int.one || profile.HitCount != 2 ||
                        profile.DwellBreakSeconds <= 0f ||
                        !definition.PlacementProfile.RequiredNeighborTags.Contains("AlternativeRouteOrVoidRecovery") ||
                        !HasReaction(definition, ToolTag.HeavyImpact, ElementReactionType.Break))
                    {
                        AddBridgeContractError(report, assetPath, definition,
                            "BRIDGE_BREAKING_PANEL_CONTRACT",
                            "끊어지는 별판은 1x1, 착지 2회 또는 HeavyImpact·체류 붕괴와 낙하 복구 계약이 필요합니다.");
                    }
                    break;

                case BridgeElementKind.Nest:
                    var bombReaction = definition.ToolReactions?.Entries != null &&
                        definition.ToolReactions.Entries.Any(entry => entry != null &&
                            (entry.Tool & ToolTag.Bomb) != 0 && entry.Reaction != ElementReactionType.None);
                    if (definition.Footprint.BoundsSize != new Vector2Int(2, 2) ||
                        profile.RequiredPieces != 3 || !profile.CriticalObject ||
                        string.IsNullOrWhiteSpace(profile.SupportRewardId) || bombReaction ||
                        !HasReaction(definition, ToolTag.Context, ElementReactionType.SetState))
                    {
                        AddBridgeContractError(report, assetPath, definition,
                            "BRIDGE_NEST_CONTRACT",
                            "까치 둥지는 2x2, 실 3개 수리, 달떡 Context, 지원 보상과 폭탄 면역 Critical 계약이 필요합니다.");
                    }
                    break;
            }
        }

        private static bool HasReaction(
            MapElementDefinition definition,
            ToolTag tool,
            ElementReactionType reaction)
        {
            return definition.ToolReactions?.Entries != null &&
                   definition.ToolReactions.Entries.Any(entry => entry != null &&
                       (entry.Tool & tool) != 0 && entry.Reaction == reaction);
        }

        private static void ValidatePalaceElement(
            MapElementDefinition definition,
            string assetPath,
            MapElementValidationReport report)
        {
            var profile = definition.PalaceProfile;
            if (profile == null || profile.Kind == PalaceElementKind.None)
            {
                return;
            }

            if (definition.AllowedRegions != RegionMask.Palace)
            {
                AddPalaceContractError(report, assetPath, definition,
                    "PALACE_REGION_CONTRACT",
                    "Palace elements must be restricted to the Palace region.");
            }

            switch (profile.Kind)
            {
                case PalaceElementKind.SluiceGate:
                    var hasDirectDestruction = definition.ToolReactions?.Entries != null &&
                        definition.ToolReactions.Entries.Any(entry => entry != null &&
                            (entry.Tool & (ToolTag.Bomb | ToolTag.Pickaxe)) != 0 &&
                            entry.Reaction != ElementReactionType.None);
                    if (profile.WidthCells < 1 || profile.WidthCells > 2 || profile.HeightCells != 3 ||
                        definition.Footprint.BoundsSize != new Vector2Int(profile.WidthCells, profile.HeightCells) ||
                        profile.MoveSpeedCellsPerSecond <= 0f || !profile.PreventPermanentLock ||
                        !definition.PlacementProfile.RequiredNeighborTags.Contains("NonLockingAlternateRoute") ||
                        !HasReaction(definition, ToolTag.Hook, ElementReactionType.Toggle) || hasDirectDestruction)
                    {
                        AddPalaceContractError(report, assetPath, definition,
                            "PALACE_SLUICE_GATE_CONTRACT",
                            "Sluice gate requires a 1-2x3 footprint, Hook lever control, a non-locking alternate route, and no direct destruction.");
                    }
                    break;

                case PalaceElementKind.BubbleCannon:
                    if (definition.Footprint.BoundsSize != new Vector2Int(1, 2) ||
                        Mathf.Abs(profile.IntervalSeconds - 1.8f) > 0.0001f ||
                        profile.Direction == Vector2Int.zero || profile.ProjectileSpeedCellsPerSecond <= 0f ||
                        profile.UmbrellaPushMultiplier < 0f || profile.UmbrellaPushMultiplier >= 1f ||
                        !HasReaction(definition, ToolTag.WindGuard, ElementReactionType.SetState))
                    {
                        AddPalaceContractError(report, assetPath, definition,
                            "PALACE_BUBBLE_CANNON_CONTRACT",
                            "Bubble cannon requires 1x2, a 1.8 second interval, directed transport force, and umbrella push reduction.");
                    }
                    break;

                case PalaceElementKind.CurrentVolume:
                    var currentSize = new Vector2Int(
                        Mathf.RoundToInt(profile.VolumeSizeCells.x),
                        Mathf.RoundToInt(profile.VolumeSizeCells.y));
                    if (definition.Footprint.BoundsSize != currentSize ||
                        profile.Direction == Vector2Int.zero || profile.ForceCellsPerSecond <= 0f ||
                        profile.Falloff < 0f || profile.Falloff > 1f ||
                        profile.HeavyBlockMultiplier < 0f || profile.HeavyBlockMultiplier >= 1f ||
                        profile.ExitSafePocketCells < 2 ||
                        !definition.PlacementProfile.RequiredNeighborTags.Contains("ExitSafePocket2Cells") ||
                        !HasReaction(definition, ToolTag.HeavyImpact, ElementReactionType.Disable))
                    {
                        AddPalaceContractError(report, assetPath, definition,
                            "PALACE_CURRENT_VOLUME_CONTRACT",
                            "Current volume requires directed force, light/heavy response, and a two-cell exit safe pocket.");
                    }
                    break;

                case PalaceElementKind.TurtlePlatform:
                    if (definition.Footprint.BoundsSize != new Vector2Int(2, 1) ||
                        Mathf.Abs(profile.SinkDepthCells - 1f) > 0.0001f || profile.WeightThreshold < 1)
                    {
                        AddPalaceContractError(report, assetPath, definition,
                            "PALACE_TURTLE_PLATFORM_CONTRACT",
                            "Turtle platform requires a 2x1 footprint and one-cell weight-driven sinking.");
                    }
                    break;

                case PalaceElementKind.ClamBounce:
                    if (definition.Footprint.BoundsSize != new Vector2Int(2, 1) ||
                        Mathf.Abs(profile.CycleSeconds - 0.8f) > 0.0001f ||
                        profile.LaunchHeightCells <= 0f || !profile.ReflectProjectiles)
                    {
                        AddPalaceContractError(report, assetPath, definition,
                            "PALACE_CLAM_BOUNCE_CONTRACT",
                            "Clam bounce requires 2x1, a 0.8 second cycle, launch force, and projectile reflection.");
                    }
                    break;

                case PalaceElementKind.WaterMirrorWall:
                    var mirrorSize = definition.Footprint.BoundsSize;
                    if (mirrorSize.x != 1 || mirrorSize.y < 2 || mirrorSize.y > 4 ||
                        profile.NormalDirection == Vector2Int.zero || !profile.TransparentOnSignal ||
                        string.IsNullOrWhiteSpace(profile.TransparencyContextId) ||
                        !HasReaction(definition, ToolTag.Context, ElementReactionType.SetState))
                    {
                        AddPalaceContractError(report, assetPath, definition,
                            "PALACE_WATER_MIRROR_CONTRACT",
                            "Water mirror requires a 1x2-4 wall, a reflection normal, and Yeouiju Context or signal transparency.");
                    }
                    break;

                case PalaceElementKind.DrainGrate:
                    if (definition.Footprint.BoundsSize != new Vector2Int(2, 1) ||
                        profile.DrainRatePerSecond <= 0f || !profile.StartsMudBlocked ||
                        !profile.KeepVoidRecoveryIndependent ||
                        !definition.PlacementProfile.RequiredNeighborTags.Contains("VoidRecoveryWaterIndependent") ||
                        !HasReaction(definition, ToolTag.Shovel, ElementReactionType.SetState) ||
                        !HasReaction(definition, ToolTag.Hook, ElementReactionType.Toggle))
                    {
                        AddPalaceContractError(report, assetPath, definition,
                            "PALACE_DRAIN_GRATE_CONTRACT",
                            "Drain grate requires 2x1, Shovel mud clearing, Hook control, drainage, and water-independent void recovery.");
                    }
                    break;

                case PalaceElementKind.DragonGateWaterfall:
                    if (definition.Footprint.BoundsSize != new Vector2Int(3, 4) ||
                        profile.VolumeSizeCells != new Vector2(3f, 4f) ||
                        profile.ForceCellsPerSecond <= 0f || !profile.StartsActive ||
                        profile.UmbrellaLiftMultiplier <= 1f || profile.CloudSupportMultiplier <= 1f ||
                        !profile.CanRefillWateringCan ||
                        !HasReaction(definition, ToolTag.WindGuard, ElementReactionType.SetState) ||
                        !HasReaction(definition, ToolTag.Water, ElementReactionType.SetState))
                    {
                        AddPalaceContractError(report, assetPath, definition,
                            "PALACE_DRAGON_WATERFALL_CONTRACT",
                            "Dragon Gate waterfall requires a 3x4 active up-current, umbrella/cloud ascent support, and watering-can refill.");
                    }
                    break;
            }
        }

        private static void AddMoonContractError(
            MapElementValidationReport report,
            string assetPath,
            MapElementDefinition definition,
            string code,
            string message)
        {
            report.Add(ValidationSeverity.Error, code, message, assetPath, definition);
        }

        private static void AddBridgeContractError(
            MapElementValidationReport report,
            string assetPath,
            MapElementDefinition definition,
            string code,
            string message)
        {
            report.Add(ValidationSeverity.Error, code, message, assetPath, definition);
        }

        private static void AddPalaceContractError(
            MapElementValidationReport report,
            string assetPath,
            MapElementDefinition definition,
            string code,
            string message)
        {
            report.Add(ValidationSeverity.Error, code, message, assetPath, definition);
        }

        private static void ValidatePostElement(
            MapElementDefinition definition,
            string assetPath,
            MapElementValidationReport report)
        {
            var profile = definition.PostProfile;
            if (profile == null || profile.Kind == PostElementKind.None)
            {
                return;
            }

            if (definition.AllowedRegions != RegionMask.Post)
            {
                AddPostContractError(report, assetPath, definition,
                    "POST_REGION_CONTRACT",
                    "Post elements must be restricted to the Post region.");
            }

            switch (profile.Kind)
            {
                case PostElementKind.Conveyor:
                    if (profile.LengthCells < 2 || profile.LengthCells > 8 ||
                        definition.Footprint.BoundsSize != new Vector2Int(profile.LengthCells, 1) ||
                        profile.Direction == Vector2Int.zero ||
                        Mathf.Abs(profile.SurfaceSpeedCellsPerSecond - 2.5f) > 0.0001f ||
                        !profile.StopsOnHeavy || !profile.KeepPortalExitSafe ||
                        !definition.PlacementProfile.RequiredNeighborTags.Contains("PortalExitSafeDestination") ||
                        !HasReaction(definition, ToolTag.HeavyImpact, ElementReactionType.Disable))
                    {
                        AddPostContractError(report, assetPath, definition,
                            "POST_CONVEYOR_CONTRACT",
                            "Conveyor requires 2-8x1, 2.5 cells/sec transport, Heavy stopping, and a portal-safe destination.");
                    }
                    break;

                case PostElementKind.ParcelLauncher:
                    if (definition.Footprint.BoundsSize != new Vector2Int(1, 2) ||
                        profile.Direction == Vector2Int.zero || profile.LaunchArc <= 0f ||
                        profile.LaunchPower <= 0f || profile.CollisionDamage != 1 ||
                        !profile.RequiresParcelInsertion || !profile.RejectPlayerEntry ||
                        !HasReaction(definition, ToolTag.Context, ElementReactionType.SetState))
                    {
                        AddPostContractError(report, assetPath, definition,
                            "POST_PARCEL_LAUNCHER_CONTRACT",
                            "Parcel launcher requires 1x2, parcel Context insertion, directed arc/power, one collision damage, and player-entry rejection.");
                    }
                    break;

                case PostElementKind.ReturnStamp:
                    if (definition.Footprint.BoundsSize != new Vector2Int(2, 2) ||
                        Mathf.Abs(profile.WarningDelaySeconds - 0.7f) > 0.0001f ||
                        profile.StampActiveSeconds <= 0f || profile.StampDamage != 1 ||
                        string.IsNullOrWhiteSpace(profile.StampType) || profile.EscapeSpaceBelowCells < 1 ||
                        !definition.PlacementProfile.RequiredNeighborTags.Contains("EscapeSpaceBelow1Cell") ||
                        !HasReaction(definition, ToolTag.Hook, ElementReactionType.Toggle) ||
                        !HasReaction(definition, ToolTag.Pound, ElementReactionType.Toggle))
                    {
                        AddPostContractError(report, assetPath, definition,
                            "POST_RETURN_STAMP_CONTRACT",
                            "Return stamp requires 2x2, a 0.7 second warning, one-cell escape space, and Hook/Pound triggering.");
                    }
                    break;

                case PostElementKind.SortingArm:
                    if (definition.Footprint.BoundsSize != new Vector2Int(2, 2) ||
                        profile.RotationStepDegrees != 90 || profile.RotationSequenceDegrees == null ||
                        profile.RotationSequenceDegrees.Count < 2 || profile.PushForceCellsPerSecond <= 0f ||
                        !HasReaction(definition, ToolTag.Context, ElementReactionType.Toggle) ||
                        !HasReaction(definition, ToolTag.HeavyImpact, ElementReactionType.Toggle))
                    {
                        AddPostContractError(report, assetPath, definition,
                            "POST_SORTING_ARM_CONTRACT",
                            "Sorting arm requires 2x2, a 90-degree sequence, push force, and lever/pressure switching.");
                    }
                    break;

                case PostElementKind.MailTube:
                    if (definition.Footprint.BoundsSize != new Vector2Int(1, 2) ||
                        !profile.RequiresPair || string.IsNullOrWhiteSpace(profile.PairGuid) || profile.OneWay ||
                        !definition.PlacementProfile.RequiredNeighborTags.Contains("PairedTubeGuidRequired") ||
                        !HasReaction(definition, ToolTag.Context, ElementReactionType.SetState))
                    {
                        AddPostContractError(report, assetPath, definition,
                            "POST_MAIL_TUBE_CONTRACT",
                            "Mail tube save requires a non-empty Pair GUID, 1x2 footprint, and compatible Parcel Context insertion.");
                    }
                    break;

                case PostElementKind.InkPool:
                    var inkSize = definition.Footprint.BoundsSize;
                    var hasUmbrellaReaction = definition.ToolReactions?.Entries != null &&
                        definition.ToolReactions.Entries.Any(entry => entry != null &&
                            (entry.Tool & ToolTag.WindGuard) != 0 && entry.Reaction != ElementReactionType.None);
                    if (profile.WidthCells < 2 || profile.WidthCells > 6 ||
                        inkSize != new Vector2Int(profile.WidthCells, 1) ||
                        Mathf.Abs(profile.SlowRate - 0.4f) > 0.0001f ||
                        !profile.RevealsHiddenFootprints || !profile.WaterDilutes ||
                        profile.UmbrellaBlocksDrops || hasUmbrellaReaction ||
                        !HasReaction(definition, ToolTag.Water, ElementReactionType.Disable))
                    {
                        AddPostContractError(report, assetPath, definition,
                            "POST_INK_POOL_CONTRACT",
                            "Ink pool requires 2-6x1, 40% slowdown, footprint reveal, Water dilution, and no umbrella blocking.");
                    }
                    break;

                case PostElementKind.ParcelStack:
                    if (definition.Footprint.BoundsSize != new Vector2Int(2, 2) ||
                        profile.BoxCount != 4 || string.IsNullOrWhiteSpace(profile.StackPattern) ||
                        profile.FlattenedHeightMultiplier <= 0f || profile.FlattenedHeightMultiplier >= 1f ||
                        !HasReaction(definition, ToolTag.Pound, ElementReactionType.Move) ||
                        !HasReaction(definition, ToolTag.Bomb, ElementReactionType.Break))
                    {
                        AddPostContractError(report, assetPath, definition,
                            "POST_PARCEL_STACK_CONTRACT",
                            "Parcel stack requires four 1x1 boxes in 2x2, Pound flattening, and Bomb collapse.");
                    }
                    break;

                case PostElementKind.ExpressTube:
                    if (definition.Footprint.BoundsSize != new Vector2Int(1, 2) ||
                        !profile.RequiresPair || string.IsNullOrWhiteSpace(profile.PairGuid) || !profile.OneWay ||
                        string.IsNullOrWhiteSpace(profile.RequiredStoryFlag) ||
                        string.IsNullOrWhiteSpace(profile.RequiredParcelId) ||
                        !definition.PlacementProfile.RequiredNeighborTags.Contains("PairedTubeGuidRequired") ||
                        !HasReaction(definition, ToolTag.Context, ElementReactionType.SetState))
                    {
                        AddPostContractError(report, assetPath, definition,
                            "POST_EXPRESS_TUBE_CONTRACT",
                            "Express tube save requires a Pair GUID, one-way 1x2 transit, and story-flag or correct-parcel activation.");
                    }
                    break;
            }
        }

        private static void AddPostContractError(
            MapElementValidationReport report,
            string assetPath,
            MapElementDefinition definition,
            string code,
            string message)
        {
            report.Add(ValidationSeverity.Error, code, message, assetPath, definition);
        }

        private static void ValidateSunElement(
            MapElementDefinition definition,
            string assetPath,
            MapElementValidationReport report)
        {
            var profile = definition.SunProfile;
            if (profile == null || profile.Kind == SunElementKind.None)
            {
                return;
            }

            if (definition.AllowedRegions != RegionMask.Sun)
            {
                AddSunContractError(report, assetPath, definition,
                    "SUN_REGION_CONTRACT",
                    "Sun elements must be restricted to the Sun region.");
            }

            switch (profile.Kind)
            {
                case SunElementKind.RotatingSunbeam:
                    if (definition.Footprint.BoundsSize != Vector2Int.one ||
                        profile.ArcDegrees < 60f || profile.ArcDegrees > 180f ||
                        profile.RotationSpeedDegreesPerSecond <= 0f ||
                        profile.CycleOnSeconds <= 0f || profile.CycleOffSeconds <= 0f ||
                        profile.Damage != 1 || !profile.CausesOverheat ||
                        !profile.IgnoreSolidBlockers || !profile.IgnoreUmbrellaBlock ||
                        !profile.RotateOnSignal || !profile.PreventFullOverheatOverlap ||
                        !definition.PlacementProfile.ForbiddenNeighborTags.Contains(
                            "FullCycleOverlapWithOverheatPlatform"))
                    {
                        AddSunContractError(report, assetPath, definition,
                            "SUN_ROTATING_BEAM_CONTRACT",
                            "Rotating sunbeam requires a 1x1 emitter, 60-180 degree arc, one damage/overheat, signal rotation, no Solid/umbrella blocking, and overlap protection.");
                    }
                    break;

                case SunElementKind.ShadowSeed:
                    if (definition.Footprint.BoundsSize != Vector2Int.one ||
                        profile.ShadowSizeCells != new Vector2(2f, 2f) ||
                        profile.ShadowRadiusCells <= 0f || profile.ShadowLifetimeSeconds <= 0f ||
                        !profile.WaterSuppressesShadow || !profile.KeepExitMarkersVisible ||
                        !definition.PlacementProfile.RequiredNeighborTags.Contains("ExitMarkerVisibleInShadow") ||
                        !HasReaction(definition, ToolTag.Water, ElementReactionType.Disable))
                    {
                        AddSunContractError(report, assetPath, definition,
                            "SUN_SHADOW_SEED_CONTRACT",
                            "Shadow seed requires 1x1, a 2x2 shadow, Water suppression, finite lifetime, and visible exit markers.");
                    }
                    break;

                case SunElementKind.SunflowerPlatform:
                    if (profile.PlatformWidthCells < 1 || profile.PlatformWidthCells > 2 ||
                        definition.Footprint.BoundsSize != new Vector2Int(profile.PlatformWidthCells, 1) ||
                        profile.PlatformRotationStepDegrees != 90 ||
                        string.IsNullOrWhiteSpace(profile.LightSourceRef) ||
                        !profile.BloomsInLight || !profile.ClosesOnOverheat)
                    {
                        AddSunContractError(report, assetPath, definition,
                            "SUN_SUNFLOWER_PLATFORM_CONTRACT",
                            "Sunflower platform requires 1-2x1, 90-degree light-facing rotation, light bloom, and overheat closing.");
                    }
                    break;

                case SunElementKind.GrowthVine:
                    var vineSize = profile.GrowthDirection.x == 0
                        ? new Vector2Int(1, profile.MaxLengthCells)
                        : new Vector2Int(profile.MaxLengthCells, 1);
                    if (profile.StartLengthCells < 1 || profile.MaxLengthCells < profile.StartLengthCells ||
                        definition.Footprint.BoundsSize != vineSize || profile.GrowthDirection == Vector2Int.zero ||
                        !profile.StopAtUnbreakableBoundary ||
                        !definition.PlacementProfile.ForbiddenNeighborTags.Contains(
                            "UnbreakableBoundaryInGrowthPath") ||
                        !HasReaction(definition, ToolTag.Water, ElementReactionType.SetState) ||
                        !HasReaction(definition, ToolTag.Pickaxe, ElementReactionType.Break) ||
                        !HasReaction(definition, ToolTag.Shovel, ElementReactionType.Break) ||
                        !HasReaction(definition, ToolTag.Hook, ElementReactionType.Pull))
                    {
                        AddSunContractError(report, assetPath, definition,
                            "SUN_GROWTH_VINE_CONTRACT",
                            "Growth vine requires 1xN, one-cell Signal/Water growth, Pickaxe/Shovel removal, Hook pulling, and an Unbreakable boundary stop.");
                    }
                    break;

                case SunElementKind.DewDrop:
                    if (definition.Footprint.BoundsSize != Vector2Int.one ||
                        profile.FallIntervalSeconds <= 0f || !profile.CoolOnImpact ||
                        !profile.CanFullyRefillWateringCan || profile.ThrownWaterMagnitude <= 0f ||
                        !HasReaction(definition, ToolTag.Context, ElementReactionType.SetState))
                    {
                        AddSunContractError(report, assetPath, definition,
                            "SUN_DEW_DROP_CONTRACT",
                            "Dew drop requires a 1x1 ceiling source, fall interval, cooling impact, full watering-can refill, and a small thrown-water reaction.");
                    }
                    break;

                case SunElementKind.OverheatPlatform:
                    if (profile.OverheatPlatformWidthCells < 1 || profile.OverheatPlatformWidthCells > 2 ||
                        definition.Footprint.BoundsSize !=
                        new Vector2Int(profile.OverheatPlatformWidthCells, 1) ||
                        Mathf.Abs(profile.SafeSeconds - 2f) > 0.0001f ||
                        Mathf.Abs(profile.OverheatSeconds - 1f) > 0.0001f ||
                        profile.OverheatWarningSeconds <= 0f || profile.WaterCoolSeconds <= 0f ||
                        profile.Damage != 1 || !profile.PreventFullSunbeamOverlap ||
                        !definition.PlacementProfile.ForbiddenNeighborTags.Contains(
                            "FullCycleOverlapWithSunbeam") ||
                        !HasReaction(definition, ToolTag.Water, ElementReactionType.Disable))
                    {
                        AddSunContractError(report, assetPath, definition,
                            "SUN_OVERHEAT_PLATFORM_CONTRACT",
                            "Overheat platform requires 1-2x1, two safe seconds/one hot second, warning then one damage, Water cooling, and sunbeam overlap protection.");
                    }
                    break;

                case SunElementKind.SunsetFlower:
                    if (definition.Footprint.BoundsSize != new Vector2Int(2, 2) ||
                        !Enum.IsDefined(typeof(SunPhase), profile.InitialPhase))
                    {
                        AddSunContractError(report, assetPath, definition,
                            "SUN_SUNSET_FLOWER_CONTRACT",
                            "Sunset flower requires 2x2 and a valid initial Day/Shadow signal phase.");
                    }
                    break;

                case SunElementKind.CrowPerch:
                    if (definition.Footprint.BoundsSize != new Vector2Int(2, 1) ||
                        string.IsNullOrWhiteSpace(profile.EventId) || profile.AcceptedContextIds == null ||
                        !profile.AcceptedContextIds.Contains("letter") ||
                        !profile.AcceptedContextIds.Contains("sun_ember") ||
                        !HasReaction(definition, ToolTag.Context, ElementReactionType.SetState))
                    {
                        AddSunContractError(report, assetPath, definition,
                            "SUN_CROW_PERCH_CONTRACT",
                            "Crow perch requires a 2x1 event anchor and Letter/Sun Ember Context acceptance.");
                    }
                    break;
            }
        }

        private static void AddSunContractError(
            MapElementValidationReport report,
            string assetPath,
            MapElementDefinition definition,
            string code,
            string message)
        {
            report.Add(ValidationSeverity.Error, code, message, assetPath, definition);
        }

        private static void ValidatePolarisElement(
            MapElementDefinition definition,
            string assetPath,
            MapElementValidationReport report)
        {
            var profile = definition.PolarisProfile;
            if (profile == null || profile.Kind == PolarisElementKind.None)
            {
                return;
            }

            if (definition.AllowedRegions != RegionMask.Polaris)
            {
                AddPolarisContractError(report, assetPath, definition,
                    "POLARIS_REGION_CONTRACT",
                    "Polaris elements must be restricted to the Polaris region.");
            }

            switch (profile.Kind)
            {
                case PolarisElementKind.OrbitPlatform:
                    if (profile.PlatformWidthCells < 1 || profile.PlatformWidthCells > 2 ||
                        definition.Footprint.BoundsSize != new Vector2Int(profile.PlatformWidthCells, 1) ||
                        profile.OrbitRadiusCells.x <= 0f || profile.OrbitRadiusCells.y <= 0f ||
                        profile.OrbitPeriodSeconds <= 0f || profile.DialOrbitMultiplier <= 0f ||
                        profile.DialOrbitMultiplier > 1f || !profile.KeepOrbitInsideCamera ||
                        !definition.PlacementProfile.RequiredNeighborTags.Contains(
                            "OrbitPathInsideCameraBounds"))
                    {
                        AddPolarisContractError(report, assetPath, definition,
                            "POLARIS_ORBIT_PLATFORM_CONTRACT",
                            "Orbit platform requires 1-2x1, a positive circular/elliptical path, gravity-dial orbit response, and a camera-bounds placement guard.");
                    }
                    break;

                case PolarisElementKind.ObservationBeam:
                    if (definition.Footprint.BoundsSize != Vector2Int.one ||
                        profile.BeamRangeCells <= 0f || profile.SweepDegrees <= 0f ||
                        profile.SweepDegrees > 180f || profile.SweepPeriodSeconds <= 0f ||
                        profile.Damage != 1 || !profile.AppliesReturnMark ||
                        !profile.MirrorCanReflect || profile.UmbrellaCanReflect ||
                        !profile.SignalChangesDirection ||
                        HasReaction(definition, ToolTag.WindGuard, ElementReactionType.Toggle))
                    {
                        AddPolarisContractError(report, assetPath, definition,
                            "POLARIS_OBSERVATION_BEAM_CONTRACT",
                            "Observation beam requires a 1x1 emitter, finite sweep/range, one damage plus return mark, mirror/signal redirection, and no umbrella reflection.");
                    }
                    break;

                case PolarisElementKind.ReturnField:
                    var returnSize = new Vector2Int(
                        Mathf.RoundToInt(profile.ReturnFieldSizeCells.x),
                        Mathf.RoundToInt(profile.ReturnFieldSizeCells.y));
                    if (returnSize.x <= 0 || returnSize.y <= 0 ||
                        definition.Footprint.BoundsSize != returnSize ||
                        profile.ReturnDelaySeconds < 0f || !profile.RequiresEntryAnchor ||
                        string.IsNullOrWhiteSpace(profile.DestinationAnchorId) ||
                        !definition.PlacementProfile.RequiredNeighborTags.Contains("EntryAnchorRequired"))
                    {
                        AddPolarisContractError(report, assetPath, definition,
                            "POLARIS_RETURN_FIELD_CONTRACT",
                            "Return field requires a positive rectangular volume, delay, and a mandatory Entry Anchor destination; missing anchors block save.");
                    }
                    break;

                case PolarisElementKind.StarWeight:
                    if (definition.Footprint.BoundsSize != Vector2Int.one ||
                        !string.Equals(profile.MassTag, "Heavy", StringComparison.Ordinal) ||
                        profile.Mass < 2f || profile.GravityDirection == Vector2Int.zero ||
                        profile.CrushDamage != 1 || profile.PressureWeight != 2 ||
                        !profile.HeavyCarryAllowed || !profile.HookPullAllowed ||
                        !HasReaction(definition, ToolTag.Context, ElementReactionType.Move) ||
                        !HasReaction(definition, ToolTag.Hook, ElementReactionType.Pull))
                    {
                        AddPolarisContractError(report, assetPath, definition,
                            "POLARIS_STAR_WEIGHT_CONTRACT",
                            "Star weight requires 1x1 Heavy mass, gravity/crush behavior, pressure weight two, Heavy Carry, and Hook Pull.");
                    }
                    break;

                case PolarisElementKind.GravityDial:
                    if (definition.Footprint.BoundsSize != new Vector2Int(1, 2) ||
                        profile.LowGravityScale <= 0f ||
                        profile.LowGravityScale >= profile.NormalGravityScale ||
                        profile.MaxInstancesPerRoom != 1 ||
                        !definition.PlacementProfile.RequiredNeighborTags.Contains(
                            "UniqueGravityDialPerRoom") ||
                        !HasReaction(definition, ToolTag.Context, ElementReactionType.Toggle) ||
                        !HasReaction(definition, ToolTag.Hook, ElementReactionType.Toggle))
                    {
                        AddPolarisContractError(report, assetPath, definition,
                            "POLARIS_GRAVITY_DIAL_CONTRACT",
                            "Gravity dial requires 1x2, Low/Normal gravity, one instance per room, empty-hand X, and Hook Trigger.");
                    }
                    break;

                case PolarisElementKind.ConstellationBridge:
                    var nodeGuids = profile.NodeGuids ?? new List<string>();
                    if (definition.Footprint.BoundsSize != Vector2Int.one ||
                        nodeGuids.Count < 2 || nodeGuids.Any(string.IsNullOrWhiteSpace) ||
                        nodeGuids.Distinct(StringComparer.Ordinal).Count() != nodeGuids.Count ||
                        profile.BridgeCellCount <= 0 ||
                        !HasReaction(definition, ToolTag.Context, ElementReactionType.SetState))
                    {
                        AddPolarisContractError(report, assetPath, definition,
                            "POLARIS_CONSTELLATION_BRIDGE_CONTRACT",
                            "Constellation bridge requires 1x1 nodes, at least two unique node GUIDs, generated cells, and artifact Context activation.");
                    }
                    break;

                case PolarisElementKind.MemoryBell:
                    if (definition.Footprint.BoundsSize != new Vector2Int(1, 2) ||
                        profile.RhythmPattern == null || profile.RhythmPattern.Count == 0 ||
                        string.IsNullOrWhiteSpace(profile.MemoryId) ||
                        profile.InteractionClearanceCells != 3 ||
                        !definition.PlacementProfile.ForbiddenNeighborTags.Contains(
                            "OtherXInteractionWithin3Cells") ||
                        !HasReaction(definition, ToolTag.Context, ElementReactionType.SetState))
                    {
                        AddPolarisContractError(report, assetPath, definition,
                            "POLARIS_MEMORY_BELL_CONTRACT",
                            "Memory bell requires 1x2, a rhythm/memory payload, X interaction, and no other X target within three cells.");
                    }
                    break;

                case PolarisElementKind.ImmutableStarBlock:
                    var hasAnyToolReaction = definition.ToolReactions.Entries.Any(entry =>
                        entry != null && entry.Tool != ToolTag.None && entry.Reaction != ElementReactionType.None);
                    if (definition.Footprint.BoundsSize != Vector2Int.one ||
                        !profile.IgnoreAllTools || string.IsNullOrWhiteSpace(profile.VisualVariant) ||
                        hasAnyToolReaction)
                    {
                        AddPolarisContractError(report, assetPath, definition,
                            "POLARIS_IMMUTABLE_BLOCK_CONTRACT",
                            "Immutable star block requires 1x1, a visual variant, and no response to any tool.");
                    }
                    break;
            }
        }

        private static void AddPolarisContractError(
            MapElementValidationReport report,
            string assetPath,
            MapElementDefinition definition,
            string code,
            string message)
        {
            report.Add(ValidationSeverity.Error, code, message, assetPath, definition);
        }

        private static void ValidateSourceTransform(
            GameObject sourceRoot,
            MapElementDefinition definition,
            MapElementValidationReport report)
        {
            if (sourceRoot == null)
            {
                return;
            }

            var transforms = sourceRoot.GetComponentsInChildren<Transform>(true);
            for (var index = 0; index < transforms.Length; index++)
            {
                var scale = transforms[index].localScale;
                if (Vector3.SqrMagnitude(scale - Vector3.one) <= 0.00000001f)
                {
                    continue;
                }

                report.Add(
                    ValidationSeverity.Error,
                    "TRANSFORM_SCALE_NON_UNIT",
                    $"'{transforms[index].name}' Transform Scale이 (1,1,1)이 아닙니다.",
                    AssetDatabase.GetAssetPath(definition),
                    transforms[index],
                    autoFixable: transforms[index].GetComponent<SpriteRenderer>() != null);
            }
        }

        private static void ValidateRuntimePrefab(
            MapElementDefinition definition,
            MapElementBakePaths paths,
            MapElementValidationReport report)
        {
            if (definition.RuntimePrefab == null)
            {
                return;
            }

            var prefab = definition.RuntimePrefab;
            if (prefab.GetComponent<MapElementInstance>() == null ||
                prefab.GetComponent<ElementRuntimeId>() == null ||
                prefab.GetComponent<StarNight.Map.Placement.GridOccupier>() == null ||
                prefab.GetComponent<ElementStateMachine>() == null ||
                prefab.GetComponent<MapElementResettable>() == null)
            {
                report.Add(
                    ValidationSeverity.Error,
                    "RUNTIME_COMPONENT_MISSING",
                    "Runtime Prefab의 공용 컴포넌트 계층이 불완전합니다.",
                    paths.RuntimePrefab,
                    prefab);
            }

            if (definition.CommonProfile != null &&
                definition.CommonProfile.Kind != CommonElementKind.None &&
                (prefab.GetComponent<CommonElementDriver>() == null ||
                 prefab.GetComponent<ToolReactionReceiver>() == null ||
                 prefab.GetComponentInChildren<CommonElementPhysicsRelay>(true) == null))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "COMMON_RUNTIME_COMPONENT_MISSING",
                    "공용 요소 Runtime Prefab에 Driver, ToolReactionReceiver 또는 PhysicsRelay가 없습니다.",
                    paths.RuntimePrefab,
                    prefab);
            }

            var commonKind = definition.CommonProfile != null
                ? definition.CommonProfile.Kind
                : CommonElementKind.None;
            var hookConnected = commonKind == CommonElementKind.MovingPlatform ||
                                commonKind == CommonElementKind.PendulumBall ||
                                commonKind == CommonElementKind.Crusher ||
                                commonKind == CommonElementKind.PulleyLift ||
                                commonKind == CommonElementKind.RollingBoulder ||
                                commonKind == CommonElementKind.Lever ||
                                commonKind == CommonElementKind.HookAnchor;
            if (hookConnected && prefab.GetComponent<HookTarget>() == null)
            {
                report.Add(
                    ValidationSeverity.Error,
                    "COMMON_HOOK_TARGET_MISSING",
                    $"{commonKind} requires its approved Hook response on the runtime prefab.",
                    paths.RuntimePrefab,
                    prefab);
            }

            if (commonKind == CommonElementKind.RopeAnchor &&
                prefab.GetComponent<RopeAnchorMarker>() == null)
            {
                report.Add(
                    ValidationSeverity.Error,
                    "COMMON_ROPE_ANCHOR_MISSING",
                    "Rope Anchor requires a runtime rope anchor marker.",
                    paths.RuntimePrefab,
                    prefab);
            }

            if (commonKind == CommonElementKind.WaterVent &&
                (prefab.GetComponentInChildren<ToolRechargeReceiver>(true) == null ||
                 prefab.GetComponentInChildren<InteractionCandidate>(true) == null))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "COMMON_WATER_RECHARGE_MISSING",
                    "Water Vent requires a watering-can recharge receiver and interaction candidate.",
                    paths.RuntimePrefab,
                    prefab);
            }

            if ((commonKind == CommonElementKind.Lever ||
                 commonKind == CommonElementKind.ExitGuideLantern) &&
                (prefab.GetComponentInChildren<MapElementWorldInteractionReceiver>(true) == null ||
                 prefab.GetComponentInChildren<InteractionCandidate>(true) == null))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "COMMON_WORLD_INTERACTION_MISSING",
                    $"{commonKind} requires an empty-hand world interaction candidate.",
                    paths.RuntimePrefab,
                    prefab);
            }

            if (definition.MaruProfile != null &&
                definition.MaruProfile.Kind != MaruElementKind.None &&
                (prefab.GetComponent<MaruElementDriver>() == null ||
                 prefab.GetComponent<ToolReactionReceiver>() == null))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "MARU_RUNTIME_COMPONENT_MISSING",
                    "마루 요소 Runtime Prefab에 MaruElementDriver 또는 ToolReactionReceiver가 없습니다.",
                    paths.RuntimePrefab,
                    prefab);
            }

            if (definition.MoonProfile != null &&
                definition.MoonProfile.Kind != MoonElementKind.None &&
                (prefab.GetComponent<MoonElementDriver>() == null ||
                 prefab.GetComponent<ToolReactionReceiver>() == null ||
                 prefab.GetComponentInChildren<MoonElementPhysicsRelay>(true) == null))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "MOON_RUNTIME_COMPONENT_MISSING",
                    "월궁 Runtime Prefab에 MoonElementDriver 또는 ToolReactionReceiver가 없습니다.",
                    paths.RuntimePrefab,
                    prefab);
            }

            if (definition.BridgeProfile != null &&
                definition.BridgeProfile.Kind != BridgeElementKind.None &&
                (prefab.GetComponent<BridgeElementDriver>() == null ||
                 prefab.GetComponent<ToolReactionReceiver>() == null ||
                 prefab.GetComponentInChildren<BridgeElementPhysicsRelay>(true) == null))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "BRIDGE_RUNTIME_COMPONENT_MISSING",
                    "오작교 Runtime Prefab에 BridgeElementDriver 또는 ToolReactionReceiver가 없습니다.",
                    paths.RuntimePrefab,
                    prefab);
            }

            if (definition.PalaceProfile != null &&
                definition.PalaceProfile.Kind != PalaceElementKind.None &&
                (prefab.GetComponent<PalaceElementDriver>() == null ||
                 prefab.GetComponent<ToolReactionReceiver>() == null ||
                 prefab.GetComponentInChildren<PalaceElementPhysicsRelay>(true) == null))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "PALACE_RUNTIME_COMPONENT_MISSING",
                    "Palace Runtime Prefab requires PalaceElementDriver and ToolReactionReceiver.",
                    paths.RuntimePrefab,
                    prefab);
            }

            if (definition.PostProfile != null &&
                definition.PostProfile.Kind != PostElementKind.None &&
                (prefab.GetComponent<PostElementDriver>() == null ||
                 prefab.GetComponent<ToolReactionReceiver>() == null ||
                 prefab.GetComponentInChildren<PostElementPhysicsRelay>(true) == null))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "POST_RUNTIME_COMPONENT_MISSING",
                    "Post Runtime Prefab requires PostElementDriver and ToolReactionReceiver.",
                    paths.RuntimePrefab,
                    prefab);
            }

            if (definition.SunProfile != null &&
                definition.SunProfile.Kind != SunElementKind.None &&
                (prefab.GetComponent<SunElementDriver>() == null ||
                 prefab.GetComponent<ToolReactionReceiver>() == null ||
                 prefab.GetComponentInChildren<SunElementPhysicsRelay>(true) == null))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "SUN_RUNTIME_COMPONENT_MISSING",
                    "Sun Runtime Prefab requires SunElementDriver and ToolReactionReceiver.",
                    paths.RuntimePrefab,
                    prefab);
            }

            if (definition.PolarisProfile != null &&
                definition.PolarisProfile.Kind != PolarisElementKind.None &&
                (prefab.GetComponent<PolarisElementDriver>() == null ||
                 prefab.GetComponent<ToolReactionReceiver>() == null ||
                 prefab.GetComponentInChildren<PolarisElementPhysicsRelay>(true) == null))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "POLARIS_RUNTIME_COMPONENT_MISSING",
                    "Polaris Runtime Prefab requires PolarisElementDriver and ToolReactionReceiver.",
                    paths.RuntimePrefab,
                    prefab);
            }

            ValidateRegionalRuntimeToolContracts(definition, paths, report, prefab);

            if (definition.CommonProfile != null &&
                definition.CommonProfile.Kind == CommonElementKind.OneWayPlatform &&
                prefab.GetComponentInChildren<PlatformEffector2D>(true) == null)
            {
                report.Add(
                    ValidationSeverity.Error,
                    "COMMON_ONE_WAY_EFFECTOR_MISSING",
                    "단방향 플랫폼 Runtime Prefab에 PlatformEffector2D가 없습니다.",
                    paths.RuntimePrefab,
                    prefab);
            }

            var requiredRoots = new[]
            {
                "VisualRoot", "PhysicsRoot", "TriggerRoot", "PathRoot",
                "SignalPortRoot", "AudioRoot", "DebugRoot",
            };
            for (var index = 0; index < requiredRoots.Length; index++)
            {
                if (prefab.transform.Find(requiredRoots[index]) == null)
                {
                    report.Add(
                        ValidationSeverity.Error,
                        "RUNTIME_ROOT_MISSING",
                        $"Runtime Prefab에 {requiredRoots[index]}가 없습니다.",
                        paths.RuntimePrefab,
                        prefab);
                }
            }

            var instance = prefab.GetComponent<MapElementInstance>();
            if (instance != null && instance.Definition != definition)
            {
                report.Add(
                    ValidationSeverity.Error,
                    "RUNTIME_DEFINITION_LINK",
                    "Runtime Prefab의 Definition 참조가 Bake 결과와 다릅니다.",
                    paths.RuntimePrefab,
                    prefab);
            }
        }

        private static void ValidateRegionalRuntimeToolContracts(
            MapElementDefinition definition,
            MapElementBakePaths paths,
            MapElementValidationReport report,
            GameObject prefab)
        {
            var hookConnected =
                definition.MoonProfile != null &&
                (definition.MoonProfile.Kind == MoonElementKind.MoonIronBall ||
                 definition.MoonProfile.Kind == MoonElementKind.CassiaRoot ||
                 definition.MoonProfile.Kind == MoonElementKind.MillShaft) ||
                definition.BridgeProfile != null &&
                definition.BridgeProfile.Kind == BridgeElementKind.KnotPulley ||
                definition.PalaceProfile != null &&
                (definition.PalaceProfile.Kind == PalaceElementKind.SluiceGate ||
                 definition.PalaceProfile.Kind == PalaceElementKind.DrainGrate) ||
                definition.PostProfile != null &&
                definition.PostProfile.Kind == PostElementKind.ReturnStamp ||
                definition.SunProfile != null &&
                definition.SunProfile.Kind == SunElementKind.GrowthVine ||
                definition.PolarisProfile != null &&
                (definition.PolarisProfile.Kind == PolarisElementKind.StarWeight ||
                 definition.PolarisProfile.Kind == PolarisElementKind.GravityDial);
            if (hookConnected && prefab.GetComponent<HookTarget>() == null)
            {
                report.Add(
                    ValidationSeverity.Error,
                    "REGIONAL_HOOK_TARGET_MISSING",
                    "The regional element requires its approved Hook response on the runtime prefab.",
                    paths.RuntimePrefab,
                    prefab);
            }

            var contextConnected =
                definition.MoonProfile != null &&
                definition.MoonProfile.Kind == MoonElementKind.MedicineMortar ||
                definition.BridgeProfile != null &&
                definition.BridgeProfile.Kind == BridgeElementKind.Nest ||
                definition.PalaceProfile != null &&
                definition.PalaceProfile.Kind == PalaceElementKind.WaterMirrorWall ||
                definition.PostProfile != null &&
                (definition.PostProfile.Kind == PostElementKind.ParcelLauncher ||
                 definition.PostProfile.Kind == PostElementKind.MailTube ||
                 definition.PostProfile.Kind == PostElementKind.ExpressTube) ||
                definition.SunProfile != null &&
                definition.SunProfile.Kind == SunElementKind.CrowPerch ||
                definition.PolarisProfile != null &&
                definition.PolarisProfile.Kind == PolarisElementKind.ConstellationBridge;
            if (contextConnected &&
                (prefab.GetComponentInChildren<MapElementContextReceiver>(true) == null ||
                 prefab.GetComponentInChildren<InteractionCandidate>(true) == null))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "REGIONAL_CONTEXT_INTERACTION_MISSING",
                    "The regional Context element requires a hand-slot receiver and interaction candidate.",
                    paths.RuntimePrefab,
                    prefab);
            }

            var worldConnected = definition.PolarisProfile != null &&
                                 (definition.PolarisProfile.Kind == PolarisElementKind.StarWeight ||
                                  definition.PolarisProfile.Kind == PolarisElementKind.GravityDial ||
                                  definition.PolarisProfile.Kind == PolarisElementKind.MemoryBell);
            if (worldConnected &&
                (prefab.GetComponentInChildren<MapElementWorldInteractionReceiver>(true) == null ||
                 prefab.GetComponentInChildren<InteractionCandidate>(true) == null))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "REGIONAL_WORLD_INTERACTION_MISSING",
                    "The regional empty-hand X element requires a world interaction candidate.",
                    paths.RuntimePrefab,
                    prefab);
            }

            var rechargeConnected =
                definition.PalaceProfile != null &&
                definition.PalaceProfile.Kind == PalaceElementKind.DragonGateWaterfall ||
                definition.SunProfile != null &&
                definition.SunProfile.Kind == SunElementKind.DewDrop;
            if (rechargeConnected &&
                (prefab.GetComponentInChildren<ToolRechargeReceiver>(true) == null ||
                 prefab.GetComponentInChildren<InteractionCandidate>(true) == null))
            {
                report.Add(
                    ValidationSeverity.Error,
                    "REGIONAL_WATER_RECHARGE_MISSING",
                    "The regional water source requires a watering-can recharge receiver and interaction candidate.",
                    paths.RuntimePrefab,
                    prefab);
            }
        }

        private static bool IsSmallColliderDrift(
            Vector2 shapeMinimum,
            Vector2 shapeMaximum,
            Vector2 minimum,
            Vector2 maximum)
        {
            return shapeMinimum.x >= minimum.x - ColliderTolerance * 2f &&
                   shapeMinimum.y >= minimum.y - ColliderTolerance * 2f &&
                   shapeMaximum.x <= maximum.x + ColliderTolerance * 2f &&
                   shapeMaximum.y <= maximum.y + ColliderTolerance * 2f;
        }

        private static int SnapPathNodes(MapElementDefinition definition)
        {
            var nodes = definition.BehaviorProfile?.Path?.Nodes;
            if (nodes == null)
            {
                return 0;
            }

            var fixes = 0;
            for (var index = 0; index < nodes.Count; index++)
            {
                var snapped = new Vector2(
                    Mathf.Round(nodes[index].x * 2f) * 0.5f,
                    Mathf.Round(nodes[index].y * 2f) * 0.5f);
                var distance = Vector2.Distance(nodes[index], snapped);
                if (distance > 0.0001f && distance <= PathSnapTolerance)
                {
                    nodes[index] = snapped;
                    fixes++;
                }
            }

            return fixes;
        }

        private static int ClampSmallColliderDrift(MapElementDefinition definition)
        {
            if (definition.Footprint == null || definition.CollisionProfile == null)
            {
                return 0;
            }

            var fixes = 0;
            fixes += ClampShapes(definition.CollisionProfile.SolidShapes, definition.Footprint);
            fixes += ClampShapes(definition.CollisionProfile.TriggerShapes, definition.Footprint);
            return fixes;
        }

        private static int ClampShapes(IReadOnlyList<SerializedColliderShape> shapes, CellFootprint footprint)
        {
            if (shapes == null)
            {
                return 0;
            }

            var fixes = 0;
            var minimum = new Vector2(-footprint.PivotCell.x - 0.5f, -footprint.PivotCell.y - 0.5f);
            var maximum = minimum + footprint.BoundsSize;
            for (var index = 0; index < shapes.Count; index++)
            {
                var shape = shapes[index];
                if (shape == null)
                {
                    continue;
                }

                var half = shape.SizeCells * 0.5f;
                var shapeMinimum = shape.OffsetCells - half;
                var shapeMaximum = shape.OffsetCells + half;
                if (!IsSmallColliderDrift(shapeMinimum, shapeMaximum, minimum, maximum))
                {
                    continue;
                }

                var next = shape.OffsetCells;
                next.x = Mathf.Clamp(next.x, minimum.x + half.x, maximum.x - half.x);
                next.y = Mathf.Clamp(next.y, minimum.y + half.y, maximum.y - half.y);
                if (next != shape.OffsetCells)
                {
                    shape.OffsetCells = next;
                    fixes++;
                }
            }

            return fixes;
        }

        private static int NormalizeSpriteScale(
            MapElementDefinition definition,
            GameObject sourceRoot)
        {
            if (sourceRoot == null || definition.VisualProfile == null)
            {
                return 0;
            }

            var renderer = sourceRoot.GetComponentInChildren<SpriteRenderer>(true);
            if (renderer == null || renderer.transform.localScale == Vector3.one)
            {
                return 0;
            }

            Undo.RecordObject(renderer.transform, "Normalize Map Element Visual Scale");
            var scale = renderer.transform.localScale;
            definition.VisualProfile.VisualSizeCells = Vector2.Scale(
                definition.VisualProfile.VisualSizeCells,
                new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y)));
            renderer.transform.localScale = Vector3.one;
            EditorUtility.SetDirty(renderer.transform);
            return 1;
        }
    }
}

#endif
