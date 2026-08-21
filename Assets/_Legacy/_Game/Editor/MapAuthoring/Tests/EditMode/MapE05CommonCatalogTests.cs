#if LEGACY_DISABLED
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Map;
using StarNight.MapAuthoring.Editor;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests
{
    public sealed class MapE05CommonCatalogTests
    {
        [Test]
        public void CommonCatalogContainsAllTwentyFiveApprovedElementsAndBakedContracts()
        {
            var definitions = CommonElementCatalogFactory.EnsureCatalog();
            Assert.That(definitions.Count, Is.EqualTo(25));
            Assert.That(definitions.Select(item => item.ElementId).Distinct().Count(), Is.EqualTo(25));
            Assert.That(definitions.Select(item => item.CommonProfile.Kind).Distinct().Count(), Is.EqualTo(25));

            var byId = definitions.ToDictionary(item => item.ElementId);
            CollectionAssert.AreEquivalent(CommonElementCatalogFactory.CatalogIds, byId.Keys);

            Assert.That(byId["COMMON_Block_Cracked"].CommonProfile.WeakHitsRequired, Is.EqualTo(3));
            MapElementDefinition softSoil = byId["COMMON_Block_SoftSoil"];
            Assert.That(softSoil.ToolReactions.Entries.Any(entry =>
                (entry.Tool & ToolTag.Bomb) != 0 && entry.ResultState == "AbsorbExplosion"), Is.True);
            Assert.That(softSoil.PlacementProfile.AllowMainRoute, Is.False);
            Assert.That(softSoil.PlacementProfile.MinimumPortalDistanceCells, Is.EqualTo(2));
            Assert.That(byId["COMMON_Floor_Fragile"].CommonProfile.TriggerDwellSeconds, Is.EqualTo(0.55f).Within(0.0001f));
            Assert.That(byId["COMMON_Floor_Fragile"].BehaviorProfile.WarningSeconds, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(byId["COMMON_Platform_MoveLinear"].BehaviorProfile.Path.SpeedCellsPerSecond, Is.EqualTo(2.2f).Within(0.0001f));
            Assert.That(byId["COMMON_Platform_MoveLinear"].BehaviorProfile.Path.WaitSeconds, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(byId["COMMON_Platform_FallingStone"].CommonProfile.GravityScale, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(byId["COMMON_Hazard_TotemShooter"].CommonProfile.ProjectileSpeedCellsPerSecond, Is.EqualTo(7f).Within(0.0001f));
            Assert.That(byId["COMMON_Hazard_LaserEmitter"].BehaviorProfile.WarningSeconds, Is.EqualTo(0.7f).Within(0.0001f));
            Assert.That(byId["COMMON_Hazard_LaserEmitter"].BehaviorProfile.ActiveSeconds, Is.EqualTo(1.1f).Within(0.0001f));
            Assert.That(byId["COMMON_Hazard_LaserEmitter"].BehaviorProfile.CooldownSeconds, Is.EqualTo(1.4f).Within(0.0001f));
            Assert.That(byId["COMMON_Hazard_LaserEmitter"].PlacementProfile.MaxPerRoom, Is.EqualTo(2));
            Assert.That(byId["COMMON_Vent_Wind"].CommonProfile.ForceCellsPerSecond, Is.EqualTo(7f).Within(0.0001f));
            Assert.That(byId["COMMON_Vent_Water"].CommonProfile.ForceCellsPerSecond, Is.EqualTo(5f).Within(0.0001f));
            Assert.That(byId["COMMON_BouncePad"].CommonProfile.LaunchHeightCells, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(byId["COMMON_Hazard_PendulumBall"].CommonProfile.SwingPeriodSeconds,
                Is.EqualTo(2.4f).Within(0.0001f));
            Assert.That(byId["COMMON_Hazard_Crusher"].BehaviorProfile.WarningSeconds,
                Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(byId["COMMON_Platform_PulleyLift"].CommonProfile.TravelCells,
                Is.InRange(3f, 10f));
            Assert.That(byId["COMMON_Hazard_RollingBoulder"].CommonProfile.MaximumSpeedCellsPerSecond,
                Is.EqualTo(6f).Within(0.0001f));
            Assert.That(byId["COMMON_Lantern_ExitGuide"].CommonProfile.GuideDurationSeconds,
                Is.EqualTo(3f).Within(0.0001f));

            var damageKinds = new HashSet<CommonElementKind>
            {
                CommonElementKind.FallingStone,
                CommonElementKind.Spike,
                CommonElementKind.TotemShooter,
                CommonElementKind.LaserEmitter,
                CommonElementKind.PendulumBall,
                CommonElementKind.Crusher,
                CommonElementKind.RollingBoulder,
            };

            foreach (var definition in definitions)
            {
                var sourceReport = MapElementValidator.ValidateSourceForBake(definition);
                var sourceErrors = sourceReport.Issues
                    .Where(issue => issue.Severity == ValidationSeverity.Error)
                    .Select(issue => $"{issue.Code}:{issue.Message}")
                    .ToArray();
                if (sourceErrors.Length > 0)
                {
                    Assert.Fail($"{definition.ElementId}: {string.Join(" | ", sourceErrors)}");
                }
                Assert.That(definition.RuntimePrefab, Is.Not.Null, definition.ElementId);
                Assert.That(definition.RuntimePrefab.GetComponent<CommonElementDriver>(), Is.Not.Null, definition.ElementId);
                Assert.That(definition.RuntimePrefab.GetComponent<ToolReactionReceiver>(), Is.Not.Null, definition.ElementId);
                var paths = AssetPathUtility.GetMapElementBakePaths(definition);
                var baked = AssetDatabase.LoadAssetAtPath<MapElementDefinition>(paths.Definition);
                Assert.That(baked, Is.Not.Null, definition.ElementId);
                var bakedReport = MapElementValidator.ValidateBakedDefinition(baked);
                var bakedErrors = bakedReport.Issues
                    .Where(issue => issue.Severity == ValidationSeverity.Error)
                    .Select(issue => $"{issue.Code}:{issue.Message}")
                    .ToArray();
                if (bakedErrors.Length > 0)
                {
                    Assert.Fail($"{definition.ElementId}: {string.Join(" | ", bakedErrors)}");
                }
                if (damageKinds.Contains(definition.CommonProfile.Kind))
                {
                    Assert.That(definition.CommonProfile.Damage, Is.EqualTo(1), definition.ElementId);
                }
            }

            var oneWay = byId["COMMON_Platform_OneWay"].RuntimePrefab;
            Assert.That(oneWay.GetComponentInChildren<PlatformEffector2D>(true), Is.Not.Null);
            Assert.That(byId["COMMON_Control_Lever"].CommonProfile.SignalChannel, Is.Not.Empty);
            Assert.That(byId["COMMON_Door_Weight"].CommonProfile.SignalChannel,
                Is.EqualTo(byId["COMMON_Control_Lever"].CommonProfile.SignalChannel));
        }
    }
}

#endif
