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
    public sealed class Tool10RegionalRuntimeConnectionTests
    {
        [Test]
        public void SixRegionalCatalogsBakeApprovedToolAndInteractionConnections()
        {
            var definitions = new List<MapElementDefinition>();
            definitions.AddRange(MoonElementCatalogFactory.EnsureCatalog());
            definitions.AddRange(BridgeElementCatalogFactory.EnsureCatalog());
            definitions.AddRange(PalaceElementCatalogFactory.EnsureCatalog());
            definitions.AddRange(PostElementCatalogFactory.EnsureCatalog());
            definitions.AddRange(SunElementCatalogFactory.EnsureCatalog());
            definitions.AddRange(PolarisElementCatalogFactory.EnsureCatalog());
            var byId = definitions.ToDictionary(definition => definition.ElementId);

            Assert.That(definitions, Has.Count.EqualTo(48));
            foreach (var definition in definitions)
            {
                var paths = AssetPathUtility.GetMapElementBakePaths(definition);
                var baked = AssetDatabase.LoadAssetAtPath<MapElementDefinition>(paths.Definition);
                Assert.That(baked, Is.Not.Null, definition.ElementId);
                var report = MapElementValidator.ValidateBakedDefinition(baked);
                Assert.That(report.ErrorCount, Is.Zero, report.CreateSummary());
            }

            AssertComponents(byId, new[]
            {
                "MOON_MoonIronBall", "MOON_CassiaRoot", "MOON_MillShaft",
                "BRIDGE_KnotPulley", "PALACE_SluiceGate", "PALACE_DrainGrate",
                "POST_ReturnStamp", "SUN_GrowthVine", "POLARIS_StarWeight",
                "POLARIS_GravityDial",
            }, "StarNight.Tools.HookLauncher.HookTarget");

            AssertComponents(byId, new[]
            {
                "MOON_MedicineMortar", "BRIDGE_Nest", "PALACE_WaterMirrorWall",
                "POST_ParcelLauncher", "POST_MailTube", "POST_ExpressTube",
                "SUN_CrowPerch", "POLARIS_ConstellationBridge",
            }, "StarNight.Interaction.Targeting.MapElementContextReceiver");

            AssertComponents(byId, new[]
            {
                "POLARIS_StarWeight", "POLARIS_GravityDial", "POLARIS_MemoryBell",
            }, "StarNight.Interaction.Targeting.MapElementWorldInteractionReceiver");

            AssertComponents(byId, new[]
            {
                "PALACE_DragonGateWaterfall", "SUN_DewDrop",
            }, "StarNight.Tools.Watering.ToolRechargeReceiver");
        }

        private static void AssertComponents(
            IReadOnlyDictionary<string, MapElementDefinition> byId,
            IEnumerable<string> ids,
            string componentTypeName)
        {
            foreach (var id in ids)
            {
                Assert.That(byId.ContainsKey(id), Is.True, id);
                var prefab = byId[id].RuntimePrefab;
                Assert.That(prefab, Is.Not.Null, id);
                var found = prefab.GetComponentsInChildren<MonoBehaviour>(true)
                    .Any(component => component != null && component.GetType().FullName == componentTypeName);
                Assert.That(found, Is.True, $"{id}: {componentTypeName}");
            }
        }
    }
}

#endif
