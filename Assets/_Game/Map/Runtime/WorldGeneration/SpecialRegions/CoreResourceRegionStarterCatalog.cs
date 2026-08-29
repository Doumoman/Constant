using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Pipeline;

namespace StarNight.Map.WorldGeneration.SpecialRegions
{
    public static class CoreResourceRegionStarterCatalog
    {
        private static readonly ReadOnlyCollection<CoreResourceRegionDefinition> Catalog =
            new ReadOnlyCollection<CoreResourceRegionDefinition>(new[]
            {
                CreateCassiaSap(),
                CreateMoonCore(),
                CreateStarNuruk(),
            }.OrderBy(value => value.RegionId).ToArray());

        public static IReadOnlyList<CoreResourceRegionDefinition> Entries => Catalog;

        public static string CanonicalDigest { get; } = ComputeCatalogDigest(Catalog);

        public static bool TryGetDefinition(
            SpecialRegionId regionId,
            out CoreResourceRegionDefinition definition)
        {
            definition = Catalog.SingleOrDefault(value => value.RegionId == regionId);
            return definition != null;
        }

        public static CoreResourceRegionDefinition GetDefinition(CoreResourceKind resource)
            => Catalog.Single(value => value.Resource == resource);

        private static CoreResourceRegionDefinition CreateMoonCore()
        {
            var regionId = new SpecialRegionId("SR_MOON_CORE_SITE_5");
            var slotId = new SpecialRegionSlotId("SR_SLOT_MOON_CORE_REWARD");
            const CoreResourceMechanismKind mechanism = CoreResourceMechanismKind.ImpactChain;
            var nodes = new[]
            {
                Node("CR_NODE_MOON_ENTRY", CoreResourceNodeRole.Entry, 0, 1),
                Node("CR_NODE_MOON_RECOVERY_JOIN", CoreResourceNodeRole.RecoveryJoin, 8, 16),
                Node("CR_NODE_MOON_BOULDER", CoreResourceNodeRole.EnvironmentTrigger, 12, 12,
                    CoreResourceMarkerKind.MoonBoulder, 1, required: true),
                Node("CR_NODE_MOON_MORTAR", CoreResourceNodeRole.EnvironmentTrigger, 18, 12,
                    CoreResourceMarkerKind.Mortar, 2, required: true),
                Node("CR_NODE_MOON_CHAINED_IMPACT", CoreResourceNodeRole.MasteryTrigger, 12, 21,
                    CoreResourceMarkerKind.ChainedImpact, 1),
                Node("CR_NODE_MOON_VEIN", CoreResourceNodeRole.MasteryTrigger, 17, 21,
                    CoreResourceMarkerKind.Vein, 2),
                Node("CR_NODE_MOON_ENEMY_CUE", CoreResourceNodeRole.MasteryTrigger, 21, 21,
                    CoreResourceMarkerKind.EnemyCue, 3),
                Node("CR_NODE_MOON_SECRET_POCKET", CoreResourceNodeRole.MasteryTrigger, 25, 21,
                    CoreResourceMarkerKind.SecretPocket, 4),
                Node("CR_NODE_MOON_IRON", CoreResourceNodeRole.OptionalBenefit, 29, 21),
                Node("CR_NODE_MOON_BATTERY", CoreResourceNodeRole.OptionalBenefit, 33, 21),
                Node("CR_NODE_MOON_REWARD", CoreResourceNodeRole.RequiredReward, 36, 16,
                    rewardSlotId: slotId),
                Node("CR_NODE_MOON_RETURN", CoreResourceNodeRole.Return, 47, 1),
                Node("CR_NODE_MOON_FAILURE", CoreResourceNodeRole.Failure, 20, 18),
                Node("CR_NODE_MOON_DEVICE_RESET", CoreResourceNodeRole.EnvironmentTrigger, 12, 18,
                    CoreResourceMarkerKind.DeviceReset, 1, required: true),
            };
            var edges = new[]
            {
                Low("CR_EDGE_MOON_LOW_01", "CR_NODE_MOON_ENTRY", "CR_NODE_MOON_RECOVERY_JOIN", 1, mechanism),
                Low("CR_EDGE_MOON_LOW_02", "CR_NODE_MOON_RECOVERY_JOIN", "CR_NODE_MOON_BOULDER", 2, mechanism),
                Low("CR_EDGE_MOON_LOW_03", "CR_NODE_MOON_BOULDER", "CR_NODE_MOON_MORTAR", 3, mechanism),
                Low("CR_EDGE_MOON_LOW_04", "CR_NODE_MOON_MORTAR", "CR_NODE_MOON_REWARD", 4, mechanism),
                Low("CR_EDGE_MOON_LOW_05", "CR_NODE_MOON_REWARD", "CR_NODE_MOON_RETURN", 5, mechanism),
                High("CR_EDGE_MOON_HIGH_01", "CR_NODE_MOON_ENTRY", "CR_NODE_MOON_CHAINED_IMPACT", 1, mechanism),
                High("CR_EDGE_MOON_HIGH_02", "CR_NODE_MOON_CHAINED_IMPACT", "CR_NODE_MOON_VEIN", 2, mechanism),
                High("CR_EDGE_MOON_HIGH_03", "CR_NODE_MOON_VEIN", "CR_NODE_MOON_ENEMY_CUE", 3, mechanism),
                High("CR_EDGE_MOON_HIGH_04", "CR_NODE_MOON_ENEMY_CUE", "CR_NODE_MOON_SECRET_POCKET", 4, mechanism),
                Hidden("CR_EDGE_MOON_HIGH_05", "CR_NODE_MOON_SECRET_POCKET", "CR_NODE_MOON_IRON", 5, mechanism),
                Hidden("CR_EDGE_MOON_HIGH_06", "CR_NODE_MOON_IRON", "CR_NODE_MOON_BATTERY", 6, mechanism),
                High("CR_EDGE_MOON_HIGH_07", "CR_NODE_MOON_BATTERY", "CR_NODE_MOON_REWARD", 7, mechanism),
                High("CR_EDGE_MOON_HIGH_08", "CR_NODE_MOON_REWARD", "CR_NODE_MOON_RETURN", 8, mechanism),
                High("CR_EDGE_MOON_FAILURE_BRANCH", "CR_NODE_MOON_CHAINED_IMPACT", "CR_NODE_MOON_FAILURE", 1, mechanism),
                Recovery("CR_EDGE_MOON_RECOVERY_01", "CR_NODE_MOON_FAILURE", "CR_NODE_MOON_DEVICE_RESET", 1, mechanism),
                Recovery("CR_EDGE_MOON_RECOVERY_02", "CR_NODE_MOON_DEVICE_RESET", "CR_NODE_MOON_RECOVERY_JOIN", 2, mechanism),
            };
            return Definition(
                regionId, CoreResourceKind.MoonCore, MoonpalaceBiomeId.MoonCrater, mechanism,
                new[]
                {
                    new CoreResourceDesignChunk(0, 0), new CoreResourceDesignChunk(1, 0),
                    new CoreResourceDesignChunk(2, 0), new CoreResourceDesignChunk(0, 1),
                    new CoreResourceDesignChunk(2, 1),
                }, nodes, edges,
                new[]
                {
                    Route("CR_ROUTE_MOON_LOW", CoreResourceRouteKind.Low, edges, "CR_EDGE_MOON_LOW_"),
                    Route("CR_ROUTE_MOON_HIGH", CoreResourceRouteKind.High, edges, "CR_EDGE_MOON_HIGH_"),
                    Route("CR_ROUTE_MOON_RECOVERY", CoreResourceRouteKind.Recovery, edges, "CR_EDGE_MOON_RECOVERY_"),
                },
                new[]
                {
                    new CoreResourceRecoveryDefinition(
                        "CR_RECOVERY_MOON_DEVICE_RESET", "CR_NODE_MOON_CHAINED_IMPACT",
                        "CR_NODE_MOON_FAILURE", "CR_EDGE_MOON_FAILURE_BRANCH",
                        "CR_ROUTE_MOON_RECOVERY", "CR_NODE_MOON_RECOVERY_JOIN"),
                },
                Reward("CR_REWARD_MOON_CORE", "CR_NODE_MOON_REWARD",
                    CoreResourceKind.MoonCore, regionId, slotId),
                new[]
                {
                    Benefit("CR_BENEFIT_MOON_IRON", "CR_NODE_MOON_IRON", CoreResourceOptionalBenefitKind.MoonIron),
                    Benefit("CR_BENEFIT_MOON_BATTERY", "CR_NODE_MOON_BATTERY", CoreResourceOptionalBenefitKind.AuxiliaryBattery),
                }, "MoonCore ImpactChain starter");
        }

        private static CoreResourceRegionDefinition CreateCassiaSap()
        {
            var regionId = new SpecialRegionId("SR_CASSIA_SAP_SITE_5");
            var slotId = new SpecialRegionSlotId("SR_SLOT_CASSIA_SAP_REWARD");
            const CoreResourceMechanismKind mechanism = CoreResourceMechanismKind.WaterChannel;
            var nodes = new[]
            {
                Node("CR_NODE_CASSIA_ENTRY", CoreResourceNodeRole.Entry, 0, 1),
                Node("CR_NODE_CASSIA_RECOVERY_JOIN", CoreResourceNodeRole.RecoveryJoin, 8, 16),
                Node("CR_NODE_CASSIA_ROOT_01", CoreResourceNodeRole.EnvironmentTrigger, 12, 10,
                    CoreResourceMarkerKind.RootChannel, 1, required: true),
                Node("CR_NODE_CASSIA_ROOT_02", CoreResourceNodeRole.EnvironmentTrigger, 18, 10,
                    CoreResourceMarkerKind.RootChannel, 2, required: true),
                Node("CR_NODE_CASSIA_ROOT_03", CoreResourceNodeRole.EnvironmentTrigger, 24, 10,
                    CoreResourceMarkerKind.RootChannel, 3, required: true),
                Node("CR_NODE_CASSIA_SAP_PIPE", CoreResourceNodeRole.EnvironmentTrigger, 30, 10,
                    CoreResourceMarkerKind.SapPipe, 4, required: true),
                Node("CR_NODE_CASSIA_MASTERY_FLOW", CoreResourceNodeRole.MasteryTrigger, 12, 21,
                    CoreResourceMarkerKind.MasteryWaterFlow, 1),
                Node("CR_NODE_CASSIA_BONUS_ROOT", CoreResourceNodeRole.MasteryTrigger, 18, 21,
                    CoreResourceMarkerKind.BonusRoot, 2),
                Node("CR_NODE_CASSIA_SHORTCUT", CoreResourceNodeRole.MasteryTrigger, 24, 21,
                    CoreResourceMarkerKind.Shortcut, 3),
                Node("CR_NODE_CASSIA_RECOVERY_PICKUP", CoreResourceNodeRole.OptionalBenefit, 30, 21),
                Node("CR_NODE_CASSIA_HIDDEN_SEED", CoreResourceNodeRole.OptionalBenefit, 34, 21),
                Node("CR_NODE_CASSIA_REWARD", CoreResourceNodeRole.RequiredReward, 36, 16,
                    rewardSlotId: slotId),
                Node("CR_NODE_CASSIA_RETURN", CoreResourceNodeRole.Return, 47, 1),
                Node("CR_NODE_CASSIA_FAILURE", CoreResourceNodeRole.Failure, 20, 18),
                Node("CR_NODE_CASSIA_MANUAL_RESET", CoreResourceNodeRole.EnvironmentTrigger, 12, 18,
                    CoreResourceMarkerKind.ManualReset, 1, required: true),
            };
            var edges = new[]
            {
                Low("CR_EDGE_CASSIA_LOW_01", "CR_NODE_CASSIA_ENTRY", "CR_NODE_CASSIA_RECOVERY_JOIN", 1, mechanism),
                Low("CR_EDGE_CASSIA_LOW_02", "CR_NODE_CASSIA_RECOVERY_JOIN", "CR_NODE_CASSIA_ROOT_01", 2, mechanism),
                Low("CR_EDGE_CASSIA_LOW_03", "CR_NODE_CASSIA_ROOT_01", "CR_NODE_CASSIA_ROOT_02", 3, mechanism),
                Low("CR_EDGE_CASSIA_LOW_04", "CR_NODE_CASSIA_ROOT_02", "CR_NODE_CASSIA_ROOT_03", 4, mechanism),
                Low("CR_EDGE_CASSIA_LOW_05", "CR_NODE_CASSIA_ROOT_03", "CR_NODE_CASSIA_SAP_PIPE", 5, mechanism),
                Low("CR_EDGE_CASSIA_LOW_06", "CR_NODE_CASSIA_SAP_PIPE", "CR_NODE_CASSIA_REWARD", 6, mechanism),
                Low("CR_EDGE_CASSIA_LOW_07", "CR_NODE_CASSIA_REWARD", "CR_NODE_CASSIA_RETURN", 7, mechanism),
                High("CR_EDGE_CASSIA_HIGH_01", "CR_NODE_CASSIA_ENTRY", "CR_NODE_CASSIA_MASTERY_FLOW", 1, mechanism),
                High("CR_EDGE_CASSIA_HIGH_02", "CR_NODE_CASSIA_MASTERY_FLOW", "CR_NODE_CASSIA_BONUS_ROOT", 2, mechanism),
                High("CR_EDGE_CASSIA_HIGH_03", "CR_NODE_CASSIA_BONUS_ROOT", "CR_NODE_CASSIA_SHORTCUT", 3, mechanism),
                High("CR_EDGE_CASSIA_HIGH_04", "CR_NODE_CASSIA_SHORTCUT", "CR_NODE_CASSIA_RECOVERY_PICKUP", 4, mechanism),
                Hidden("CR_EDGE_CASSIA_HIGH_05", "CR_NODE_CASSIA_RECOVERY_PICKUP", "CR_NODE_CASSIA_HIDDEN_SEED", 5, mechanism),
                High("CR_EDGE_CASSIA_HIGH_06", "CR_NODE_CASSIA_HIDDEN_SEED", "CR_NODE_CASSIA_REWARD", 6, mechanism),
                High("CR_EDGE_CASSIA_HIGH_07", "CR_NODE_CASSIA_REWARD", "CR_NODE_CASSIA_RETURN", 7, mechanism),
                High("CR_EDGE_CASSIA_FAILURE_BRANCH", "CR_NODE_CASSIA_MASTERY_FLOW", "CR_NODE_CASSIA_FAILURE", 1, mechanism),
                Recovery("CR_EDGE_CASSIA_RECOVERY_01", "CR_NODE_CASSIA_FAILURE", "CR_NODE_CASSIA_MANUAL_RESET", 1, mechanism),
                Recovery("CR_EDGE_CASSIA_RECOVERY_02", "CR_NODE_CASSIA_MANUAL_RESET", "CR_NODE_CASSIA_RECOVERY_JOIN", 2, mechanism),
            };
            return Definition(
                regionId, CoreResourceKind.CassiaSap, MoonpalaceBiomeId.CassiaRoot, mechanism,
                new[]
                {
                    new CoreResourceDesignChunk(0, 0), new CoreResourceDesignChunk(1, 0),
                    new CoreResourceDesignChunk(0, 1), new CoreResourceDesignChunk(1, 1),
                    new CoreResourceDesignChunk(2, 1),
                }, nodes, edges,
                new[]
                {
                    Route("CR_ROUTE_CASSIA_LOW", CoreResourceRouteKind.Low, edges, "CR_EDGE_CASSIA_LOW_"),
                    Route("CR_ROUTE_CASSIA_HIGH", CoreResourceRouteKind.High, edges, "CR_EDGE_CASSIA_HIGH_"),
                    Route("CR_ROUTE_CASSIA_RECOVERY", CoreResourceRouteKind.Recovery, edges, "CR_EDGE_CASSIA_RECOVERY_"),
                },
                new[]
                {
                    new CoreResourceRecoveryDefinition(
                        "CR_RECOVERY_CASSIA_MANUAL_RESET", "CR_NODE_CASSIA_MASTERY_FLOW",
                        "CR_NODE_CASSIA_FAILURE", "CR_EDGE_CASSIA_FAILURE_BRANCH",
                        "CR_ROUTE_CASSIA_RECOVERY", "CR_NODE_CASSIA_RECOVERY_JOIN"),
                },
                Reward("CR_REWARD_CASSIA_SAP", "CR_NODE_CASSIA_REWARD",
                    CoreResourceKind.CassiaSap, regionId, slotId),
                new[]
                {
                    Benefit("CR_BENEFIT_CASSIA_RECOVERY", "CR_NODE_CASSIA_RECOVERY_PICKUP", CoreResourceOptionalBenefitKind.RecoveryPickup),
                    Benefit("CR_BENEFIT_CASSIA_SEED", "CR_NODE_CASSIA_HIDDEN_SEED", CoreResourceOptionalBenefitKind.HiddenSeed),
                }, "CassiaSap WaterChannel starter");
        }

        private static CoreResourceRegionDefinition CreateStarNuruk()
        {
            var regionId = new SpecialRegionId("SR_STAR_NURUK_SITE_5");
            var slotId = new SpecialRegionSlotId("SR_SLOT_STAR_NURUK_REWARD");
            const CoreResourceMechanismKind mechanism = CoreResourceMechanismKind.FermentationPressure;
            var nodes = new[]
            {
                Node("CR_NODE_NURUK_ENTRY", CoreResourceNodeRole.Entry, 0, 1),
                Node("CR_NODE_NURUK_RECOVERY_JOIN", CoreResourceNodeRole.RecoveryJoin, 8, 16),
                Node("CR_NODE_NURUK_VALVE_01", CoreResourceNodeRole.EnvironmentTrigger, 12, 12,
                    CoreResourceMarkerKind.Valve, 1, required: true),
                Node("CR_NODE_NURUK_SAFE_PLATFORM", CoreResourceNodeRole.EnvironmentTrigger, 18, 12,
                    CoreResourceMarkerKind.SafePlatform, 2, required: true),
                Node("CR_NODE_NURUK_VALVE_02", CoreResourceNodeRole.EnvironmentTrigger, 24, 12,
                    CoreResourceMarkerKind.Valve, 3, required: true),
                Node("CR_NODE_NURUK_GAS_WARNING", CoreResourceNodeRole.EnvironmentTrigger, 30, 12,
                    CoreResourceMarkerKind.GasWarning, 4, required: true),
                Node("CR_NODE_NURUK_PRESSURE_RELEASE", CoreResourceNodeRole.EnvironmentTrigger, 36, 16,
                    CoreResourceMarkerKind.PressureRelease, 5, required: true),
                Node("CR_NODE_NURUK_BOUNCE_01", CoreResourceNodeRole.MasteryTrigger, 14, 21,
                    CoreResourceMarkerKind.BounceChain, 1),
                Node("CR_NODE_NURUK_BOUNCE_02", CoreResourceNodeRole.MasteryTrigger, 22, 21,
                    CoreResourceMarkerKind.BounceChain, 2),
                Node("CR_NODE_NURUK_FUEL", CoreResourceNodeRole.OptionalBenefit, 30, 21),
                Node("CR_NODE_NURUK_RARE_ITEM", CoreResourceNodeRole.OptionalBenefit, 36, 21),
                Node("CR_NODE_NURUK_REWARD", CoreResourceNodeRole.RequiredReward, 40, 16,
                    rewardSlotId: slotId),
                Node("CR_NODE_NURUK_RETURN", CoreResourceNodeRole.Return, 47, 1),
                Node("CR_NODE_NURUK_FAILURE", CoreResourceNodeRole.Failure, 24, 18),
                Node("CR_NODE_NURUK_RECOVERY_ROOM", CoreResourceNodeRole.EnvironmentTrigger, 12, 18,
                    CoreResourceMarkerKind.RecoveryRoom, 1, required: true),
            };
            var edges = new[]
            {
                Low("CR_EDGE_NURUK_LOW_01", "CR_NODE_NURUK_ENTRY", "CR_NODE_NURUK_RECOVERY_JOIN", 1, mechanism),
                Low("CR_EDGE_NURUK_LOW_02", "CR_NODE_NURUK_RECOVERY_JOIN", "CR_NODE_NURUK_VALVE_01", 2, mechanism),
                Low("CR_EDGE_NURUK_LOW_03", "CR_NODE_NURUK_VALVE_01", "CR_NODE_NURUK_SAFE_PLATFORM", 3, mechanism),
                Low("CR_EDGE_NURUK_LOW_04", "CR_NODE_NURUK_SAFE_PLATFORM", "CR_NODE_NURUK_VALVE_02", 4, mechanism),
                Low("CR_EDGE_NURUK_LOW_05", "CR_NODE_NURUK_VALVE_02", "CR_NODE_NURUK_GAS_WARNING", 5, mechanism),
                Low("CR_EDGE_NURUK_LOW_06", "CR_NODE_NURUK_GAS_WARNING", "CR_NODE_NURUK_PRESSURE_RELEASE", 6, mechanism),
                Low("CR_EDGE_NURUK_LOW_07", "CR_NODE_NURUK_PRESSURE_RELEASE", "CR_NODE_NURUK_REWARD", 7, mechanism),
                Low("CR_EDGE_NURUK_LOW_08", "CR_NODE_NURUK_REWARD", "CR_NODE_NURUK_RETURN", 8, mechanism),
                High("CR_EDGE_NURUK_HIGH_01", "CR_NODE_NURUK_ENTRY", "CR_NODE_NURUK_BOUNCE_01", 1, mechanism),
                High("CR_EDGE_NURUK_HIGH_02", "CR_NODE_NURUK_BOUNCE_01", "CR_NODE_NURUK_BOUNCE_02", 2, mechanism),
                High("CR_EDGE_NURUK_HIGH_03", "CR_NODE_NURUK_BOUNCE_02", "CR_NODE_NURUK_PRESSURE_RELEASE", 3, mechanism),
                High("CR_EDGE_NURUK_HIGH_04", "CR_NODE_NURUK_PRESSURE_RELEASE", "CR_NODE_NURUK_FUEL", 4, mechanism),
                Hidden("CR_EDGE_NURUK_HIGH_05", "CR_NODE_NURUK_FUEL", "CR_NODE_NURUK_RARE_ITEM", 5, mechanism),
                High("CR_EDGE_NURUK_HIGH_06", "CR_NODE_NURUK_RARE_ITEM", "CR_NODE_NURUK_REWARD", 6, mechanism),
                High("CR_EDGE_NURUK_HIGH_07", "CR_NODE_NURUK_REWARD", "CR_NODE_NURUK_RETURN", 7, mechanism),
                High("CR_EDGE_NURUK_FAILURE_BRANCH", "CR_NODE_NURUK_BOUNCE_02", "CR_NODE_NURUK_FAILURE", 1, mechanism),
                Recovery("CR_EDGE_NURUK_RECOVERY_01", "CR_NODE_NURUK_FAILURE", "CR_NODE_NURUK_RECOVERY_ROOM", 1, mechanism),
                Recovery("CR_EDGE_NURUK_RECOVERY_02", "CR_NODE_NURUK_RECOVERY_ROOM", "CR_NODE_NURUK_RECOVERY_JOIN", 2, mechanism),
            };
            return Definition(
                regionId, CoreResourceKind.StarNuruk, MoonpalaceBiomeId.MoonDough, mechanism,
                new[]
                {
                    new CoreResourceDesignChunk(0, 0), new CoreResourceDesignChunk(1, 0),
                    new CoreResourceDesignChunk(2, 0), new CoreResourceDesignChunk(1, 1),
                    new CoreResourceDesignChunk(2, 1),
                }, nodes, edges,
                new[]
                {
                    Route("CR_ROUTE_NURUK_LOW", CoreResourceRouteKind.Low, edges, "CR_EDGE_NURUK_LOW_"),
                    Route("CR_ROUTE_NURUK_HIGH", CoreResourceRouteKind.High, edges, "CR_EDGE_NURUK_HIGH_"),
                    Route("CR_ROUTE_NURUK_RECOVERY", CoreResourceRouteKind.Recovery, edges, "CR_EDGE_NURUK_RECOVERY_"),
                },
                new[]
                {
                    new CoreResourceRecoveryDefinition(
                        "CR_RECOVERY_NURUK_LOWER_ROOM", "CR_NODE_NURUK_BOUNCE_02",
                        "CR_NODE_NURUK_FAILURE", "CR_EDGE_NURUK_FAILURE_BRANCH",
                        "CR_ROUTE_NURUK_RECOVERY", "CR_NODE_NURUK_RECOVERY_JOIN"),
                },
                Reward("CR_REWARD_STAR_NURUK", "CR_NODE_NURUK_REWARD",
                    CoreResourceKind.StarNuruk, regionId, slotId),
                new[]
                {
                    Benefit("CR_BENEFIT_NURUK_FUEL", "CR_NODE_NURUK_FUEL", CoreResourceOptionalBenefitKind.Fuel),
                    Benefit("CR_BENEFIT_NURUK_RARE", "CR_NODE_NURUK_RARE_ITEM", CoreResourceOptionalBenefitKind.RareFermentationItem),
                }, "StarNuruk FermentationPressure starter");
        }

        private static CoreResourceRegionDefinition Definition(
            SpecialRegionId regionId,
            CoreResourceKind resource,
            MoonpalaceBiomeId biome,
            CoreResourceMechanismKind mechanism,
            IEnumerable<CoreResourceDesignChunk> chunks,
            IEnumerable<CoreResourceSolutionNode> nodes,
            IEnumerable<CoreResourceSolutionEdge> edges,
            IEnumerable<CoreResourceRouteDefinition> routes,
            IEnumerable<CoreResourceRecoveryDefinition> recoveries,
            CoreResourceRewardDefinition reward,
            IEnumerable<CoreResourceOptionalBenefitDefinition> benefits,
            string displayText)
            => new CoreResourceRegionDefinition(
                regionId, resource, biome, SpecialRegionKind.CoreResource, mechanism,
                1, 1, new LocalTileCoord(6, 8), 36, 16, 12, 8,
                chunks, nodes, edges, routes, recoveries, reward, benefits, displayText);

        private static CoreResourceSolutionNode Node(
            string id,
            CoreResourceNodeRole role,
            int x,
            int y,
            CoreResourceMarkerKind marker = CoreResourceMarkerKind.None,
            int order = 0,
            SpecialRegionSlotId rewardSlotId = default(SpecialRegionSlotId),
            bool required = false)
            => new CoreResourceSolutionNode(
                id, role, new LocalTileCoord(x, y), marker, order, rewardSlotId, required);

        private static CoreResourceSolutionEdge Low(
            string id, string from, string to, int order, CoreResourceMechanismKind mechanism)
            => new CoreResourceSolutionEdge(
                id, from, to, order, CoreResourceRouteKind.Low,
                AccessClass.MandatoryNoTool, mechanism, true);

        private static CoreResourceSolutionEdge High(
            string id, string from, string to, int order, CoreResourceMechanismKind mechanism)
            => new CoreResourceSolutionEdge(
                id, from, to, order, CoreResourceRouteKind.High,
                AccessClass.OptionalEnvironment, mechanism, false);

        private static CoreResourceSolutionEdge Hidden(
            string id, string from, string to, int order, CoreResourceMechanismKind mechanism)
            => new CoreResourceSolutionEdge(
                id, from, to, order, CoreResourceRouteKind.High,
                AccessClass.OptionalHidden, mechanism, false);

        private static CoreResourceSolutionEdge Recovery(
            string id, string from, string to, int order, CoreResourceMechanismKind mechanism)
            => new CoreResourceSolutionEdge(
                id, from, to, order, CoreResourceRouteKind.Recovery,
                AccessClass.MandatoryNoTool, mechanism, true);

        private static CoreResourceRouteDefinition Route(
            string id,
            CoreResourceRouteKind kind,
            IEnumerable<CoreResourceSolutionEdge> edges,
            string prefix)
            => new CoreResourceRouteDefinition(
                id, kind, edges.Where(value => value.EdgeId.StartsWith(prefix, StringComparison.Ordinal))
                    .Select(value => value.EdgeId));

        private static CoreResourceRewardDefinition Reward(
            string rewardId,
            string nodeId,
            CoreResourceKind resource,
            SpecialRegionId regionId,
            SpecialRegionSlotId slotId)
            => new CoreResourceRewardDefinition(
                rewardId, nodeId, resource, slotId,
                SpecialPersistenceKey.ForSlot(regionId, SpecialPersistenceScope.Reward, slotId),
                SpecialPersistenceScope.Reward, 1, true);

        private static CoreResourceOptionalBenefitDefinition Benefit(
            string id,
            string nodeId,
            CoreResourceOptionalBenefitKind kind)
            => new CoreResourceOptionalBenefitDefinition(id, nodeId, kind);

        private static string ComputeCatalogDigest(IEnumerable<CoreResourceRegionDefinition> definitions)
        {
            var value = string.Join("\n", definitions.OrderBy(item => item.RegionId)
                .Select(CoreResourceRegionCanonicalDigest.ComputeDefinition));
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(new UTF8Encoding(false).GetBytes(value))
                    .Select(item => item.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }
}
