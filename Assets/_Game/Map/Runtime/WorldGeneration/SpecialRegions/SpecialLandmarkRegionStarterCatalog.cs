using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Pipeline;

namespace StarNight.Map.WorldGeneration.SpecialRegions
{
    public static class SpecialLandmarkRegionStarterCatalog
    {
        private static readonly ReadOnlyCollection<SpecialLandmarkRegionDefinition> entries =
            new ReadOnlyCollection<SpecialLandmarkRegionDefinition>(new[]
            {
                CreateBoss(),
                CreateForge(),
                CreateMaru(),
                CreateMerchant(),
            }.OrderBy(value => value.RegionId).ToArray());

        public static IReadOnlyList<SpecialLandmarkRegionDefinition> Entries => entries;
        public static string CanonicalDigest { get; } = ComputeCatalogDigest(entries);

        public static bool TryGetDefinition(
            SpecialLandmarkKind landmark,
            out SpecialLandmarkRegionDefinition definition)
        {
            definition = entries.SingleOrDefault(value => value.Landmark == landmark);
            return definition != null;
        }

        public static SpecialLandmarkRegionDefinition GetDefinition(SpecialLandmarkKind landmark)
            => entries.Single(value => value.Landmark == landmark);

        private static SpecialLandmarkRegionDefinition CreateForge()
        {
            var regionId = new SpecialRegionId("SR_MOON_SEAL_FORGE_9");
            var rewardSlot = new SpecialRegionSlotId("SR_SLOT_MOON_SEAL_REWARD");
            var nodes = new[]
            {
                Node("FORGE_ENTRY", SpecialLandmarkNodeRole.Entry, 1, 12),
                Node("FORGE_GRIND", SpecialLandmarkNodeRole.Workstation, 7, 12),
                Node("FORGE_MIX", SpecialLandmarkNodeRole.Workstation, 15, 12),
                Node("FORGE_PRESS", SpecialLandmarkNodeRole.Workstation, 24, 12),
                Node("FORGE_CURE", SpecialLandmarkNodeRole.Workstation, 33, 12),
                Node("FORGE_REWARD", SpecialLandmarkNodeRole.RequiredReward, 40, 12),
                Node("FORGE_RETURN", SpecialLandmarkNodeRole.Return, 46, 12),
                Node("FORGE_TIMING", SpecialLandmarkNodeRole.Mastery, 11, 18, false),
                Node("FORGE_ATTENTION", SpecialLandmarkNodeRole.Mastery, 29, 18, false),
                Node("FORGE_FAIL_GRIND", SpecialLandmarkNodeRole.Failure, 8, 7, false),
                Node("FORGE_FAIL_MIX", SpecialLandmarkNodeRole.Failure, 17, 7, false),
                Node("FORGE_FAIL_PRESS", SpecialLandmarkNodeRole.Failure, 26, 7, false),
                Node("FORGE_SAFE_CORRIDOR", SpecialLandmarkNodeRole.RecoveryJoin, 4, 7),
            };
            var edges = new[]
            {
                Edge("FORGE_LOW_01", "FORGE_ENTRY", "FORGE_GRIND", SpecialLandmarkRouteKind.Low, 1, true),
                Edge("FORGE_LOW_02", "FORGE_GRIND", "FORGE_MIX", SpecialLandmarkRouteKind.Low, 2, true),
                Edge("FORGE_LOW_03", "FORGE_MIX", "FORGE_PRESS", SpecialLandmarkRouteKind.Low, 3, true),
                Edge("FORGE_LOW_04", "FORGE_PRESS", "FORGE_CURE", SpecialLandmarkRouteKind.Low, 4, true),
                Edge("FORGE_LOW_05", "FORGE_CURE", "FORGE_REWARD", SpecialLandmarkRouteKind.Low, 5, true),
                Edge("FORGE_LOW_06", "FORGE_REWARD", "FORGE_RETURN", SpecialLandmarkRouteKind.Low, 6, true),
                Edge("FORGE_HIGH_01", "FORGE_ENTRY", "FORGE_GRIND", SpecialLandmarkRouteKind.High, 1, false),
                Edge("FORGE_HIGH_02", "FORGE_GRIND", "FORGE_TIMING", SpecialLandmarkRouteKind.High, 2, false),
                Edge("FORGE_HIGH_03", "FORGE_TIMING", "FORGE_MIX", SpecialLandmarkRouteKind.High, 3, false),
                Edge("FORGE_HIGH_04", "FORGE_MIX", "FORGE_PRESS", SpecialLandmarkRouteKind.High, 4, false),
                Edge("FORGE_HIGH_05", "FORGE_PRESS", "FORGE_ATTENTION", SpecialLandmarkRouteKind.High, 5, false),
                Edge("FORGE_HIGH_06", "FORGE_ATTENTION", "FORGE_CURE", SpecialLandmarkRouteKind.High, 6, false),
                Edge("FORGE_HIGH_07", "FORGE_CURE", "FORGE_REWARD", SpecialLandmarkRouteKind.High, 7, false),
                Edge("FORGE_HIGH_08", "FORGE_REWARD", "FORGE_RETURN", SpecialLandmarkRouteKind.High, 8, false),
                Edge("FORGE_FAIL_BRANCH_01", "FORGE_GRIND", "FORGE_FAIL_GRIND", SpecialLandmarkRouteKind.High, 101, false),
                Edge("FORGE_FAIL_BRANCH_02", "FORGE_MIX", "FORGE_FAIL_MIX", SpecialLandmarkRouteKind.High, 102, false),
                Edge("FORGE_FAIL_BRANCH_03", "FORGE_PRESS", "FORGE_FAIL_PRESS", SpecialLandmarkRouteKind.High, 103, false),
                Edge("FORGE_RECOVERY_01", "FORGE_FAIL_GRIND", "FORGE_SAFE_CORRIDOR", SpecialLandmarkRouteKind.Recovery, 1, true),
                Edge("FORGE_RECOVERY_02", "FORGE_FAIL_MIX", "FORGE_SAFE_CORRIDOR", SpecialLandmarkRouteKind.Recovery, 1, true),
                Edge("FORGE_RECOVERY_03", "FORGE_FAIL_PRESS", "FORGE_SAFE_CORRIDOR", SpecialLandmarkRouteKind.Recovery, 1, true),
                Edge("FORGE_RECOVERY_JOIN", "FORGE_SAFE_CORRIDOR", "FORGE_GRIND", SpecialLandmarkRouteKind.Recovery, 2, true),
                Edge("FORGE_RETURN", "FORGE_REWARD", "FORGE_RETURN", SpecialLandmarkRouteKind.Return, 1, true),
            };
            var routes = new[]
            {
                Route("FORGE_LOW", SpecialLandmarkRouteKind.Low, "FORGE_ENTRY", "FORGE_RETURN",
                    "FORGE_LOW_01", "FORGE_LOW_02", "FORGE_LOW_03", "FORGE_LOW_04", "FORGE_LOW_05", "FORGE_LOW_06"),
                Route("FORGE_HIGH", SpecialLandmarkRouteKind.High, "FORGE_ENTRY", "FORGE_RETURN",
                    "FORGE_HIGH_01", "FORGE_HIGH_02", "FORGE_HIGH_03", "FORGE_HIGH_04", "FORGE_HIGH_05",
                    "FORGE_HIGH_06", "FORGE_HIGH_07", "FORGE_HIGH_08"),
                Route("FORGE_RECOVERY_GRIND", SpecialLandmarkRouteKind.Recovery, "FORGE_FAIL_GRIND", "FORGE_GRIND",
                    "FORGE_RECOVERY_01", "FORGE_RECOVERY_JOIN"),
                Route("FORGE_RECOVERY_MIX", SpecialLandmarkRouteKind.Recovery, "FORGE_FAIL_MIX", "FORGE_GRIND",
                    "FORGE_RECOVERY_02", "FORGE_RECOVERY_JOIN"),
                Route("FORGE_RECOVERY_PRESS", SpecialLandmarkRouteKind.Recovery, "FORGE_FAIL_PRESS", "FORGE_GRIND",
                    "FORGE_RECOVERY_03", "FORGE_RECOVERY_JOIN"),
                Route("FORGE_RETURN", SpecialLandmarkRouteKind.Return, "FORGE_REWARD", "FORGE_RETURN", "FORGE_RETURN"),
            };
            var states = new List<SpecialLandmarkStateDefinition>
            {
                State("FORGE_READY", SpecialLandmarkStateRole.ForgeReady),
                State("FORGE_SUCCESS", SpecialLandmarkStateRole.ForgeSucceeded, true),
            };
            var ledgers = new List<SpecialLandmarkForgeLedgerDefinition>();
            var transitions = new List<SpecialLandmarkStateTransitionDefinition>();
            foreach (var resource in new[]
                     {
                         SpecialLandmarkForgeResource.MoonCore,
                         SpecialLandmarkForgeResource.CassiaSap,
                         SpecialLandmarkForgeResource.StarNuruk,
                     })
            {
                var token = resource.ToString().ToUpperInvariant();
                var available = "SL_STATE_FORGE_" + token + "_AVAILABLE";
                var reserved = "SL_STATE_FORGE_" + token + "_RESERVED";
                var consumed = "SL_STATE_FORGE_" + token + "_CONSUMED";
                var returned = "SL_STATE_FORGE_" + token + "_RETURNED";
                states.Add(new SpecialLandmarkStateDefinition(available, SpecialLandmarkStateRole.ResourceAvailable, false));
                states.Add(new SpecialLandmarkStateDefinition(reserved, SpecialLandmarkStateRole.ResourceReserved, false));
                states.Add(new SpecialLandmarkStateDefinition(consumed, SpecialLandmarkStateRole.ResourceConsumed, true));
                states.Add(new SpecialLandmarkStateDefinition(returned, SpecialLandmarkStateRole.ResourceReturned, false));
                ledgers.Add(new SpecialLandmarkForgeLedgerDefinition(resource, available, reserved, consumed, returned));
                transitions.Add(Transition("FORGE_" + token + "_RESERVE", available, reserved,
                    SpecialLandmarkTransitionTrigger.ReserveResource, 1));
                transitions.Add(Transition("FORGE_" + token + "_CONSUME", reserved, consumed,
                    SpecialLandmarkTransitionTrigger.ProcessSucceeded, 2));
                transitions.Add(Transition("FORGE_" + token + "_RETURN", reserved, returned,
                    SpecialLandmarkTransitionTrigger.ProcessFailed, 3));
            }
            var resets = new[]
            {
                Reset("FORGE_GRIND", SpecialLandmarkResetPolicy.ManualReset, "FORGE_FAIL_GRIND", "FORGE_SAFE_CORRIDOR", true),
                Reset("FORGE_MIX", SpecialLandmarkResetPolicy.ManualReset, "FORGE_FAIL_MIX", "FORGE_SAFE_CORRIDOR", true),
                Reset("FORGE_PRESS", SpecialLandmarkResetPolicy.ManualReset, "FORGE_FAIL_PRESS", "FORGE_SAFE_CORRIDOR", true),
            };
            var markers = new[]
            {
                Marker("FORGE_STEP_GRIND", SpecialLandmarkMarkerKind.ForgeProcessStep, "FORGE_GRIND", 1),
                Marker("FORGE_STEP_MIX", SpecialLandmarkMarkerKind.ForgeProcessStep, "FORGE_MIX", 2),
                Marker("FORGE_STEP_PRESS", SpecialLandmarkMarkerKind.ForgeProcessStep, "FORGE_PRESS", 3),
                Marker("FORGE_STEP_CURE", SpecialLandmarkMarkerKind.ForgeProcessStep, "FORGE_CURE", 4),
                Marker("FORGE_TIMING", SpecialLandmarkMarkerKind.TimingOptimization, "FORGE_TIMING", 1, false),
                Marker("FORGE_ATTENTION_REDUCTION", SpecialLandmarkMarkerKind.MaruAttentionReduction, "FORGE_ATTENTION", 2, false),
                Marker("FORGE_INPUT_MOON_CORE", SpecialLandmarkMarkerKind.ForgeInput, "FORGE_GRIND"),
                Marker("FORGE_INPUT_CASSIA_SAP", SpecialLandmarkMarkerKind.ForgeInput, "FORGE_MIX"),
                Marker("FORGE_INPUT_STAR_NURUK", SpecialLandmarkMarkerKind.ForgeInput, "FORGE_PRESS"),
                Marker("FORGE_OUTPUT_MOON_SEAL", SpecialLandmarkMarkerKind.MoonSealOutput, "FORGE_REWARD"),
                Marker("FORGE_BOSS_DIRECTION", SpecialLandmarkMarkerKind.BossDirection, "FORGE_RETURN"),
                Marker("FORGE_SAFE_CORRIDOR", SpecialLandmarkMarkerKind.SafeCorridor, "FORGE_SAFE_CORRIDOR"),
            };
            return Definition(
                regionId, SpecialLandmarkKind.MoonSealForge, SpecialRegionKind.Forge,
                SpecialLandmarkTheme.AbandonedMill, SpecialLandmarkBindingKind.PlacedMandatorySite,
                1, 1, new LocalTileCoord(0, 4), 48, 24,
                Chunks((0,0),(1,0),(2,0),(3,0),(0,1),(1,1),(2,1),(3,1),(2,2)),
                nodes, edges, routes, states, transitions, resets, markers, ledgers,
                new SpecialLandmarkRewardDefinition(
                    "SL_REWARD_MOON_SEAL", "SL_NODE_FORGE_REWARD", rewardSlot,
                    SpecialPersistenceKey.ForSlot(regionId, SpecialPersistenceScope.Reward, rewardSlot), 1, true),
                Array.Empty<SpecialLandmarkMerchantVariant>(), "MoonSeal Forge starter");
        }

        private static SpecialLandmarkRegionDefinition CreateBoss()
        {
            var regionId = new SpecialRegionId("SR_MOON_BOSS_SEAL_ARENA_12");
            var encounterSlot = new SpecialRegionSlotId("SR_SLOT_BOSS_ENCOUNTER");
            var nodes = new[]
            {
                Node("BOSS_ENTRY", SpecialLandmarkNodeRole.Entry, 1, 15),
                Node("BOSS_GATE", SpecialLandmarkNodeRole.Gate, 6, 15),
                Node("BOSS_LOWER_OBSERVE", SpecialLandmarkNodeRole.Observation, 13, 6),
                Node("BOSS_CENTRAL_RECOVERY", SpecialLandmarkNodeRole.RecoveryJoin, 23, 5),
                Node("BOSS_ARENA", SpecialLandmarkNodeRole.Arena, 37, 15),
                Node("BOSS_RETURN", SpecialLandmarkNodeRole.Return, 46, 15),
                Node("BOSS_UPPER_A", SpecialLandmarkNodeRole.Mastery, 13, 25, false),
                Node("BOSS_FALL_OBJECT", SpecialLandmarkNodeRole.Mastery, 23, 26, false),
                Node("BOSS_PRESSURE_DEVICE", SpecialLandmarkNodeRole.Mastery, 32, 25, false),
                Node("BOSS_FAIL_FALL", SpecialLandmarkNodeRole.Failure, 24, 11, false),
                Node("BOSS_FAIL_PRESSURE", SpecialLandmarkNodeRole.Failure, 32, 11, false),
                Node("BOSS_DEFEATED", SpecialLandmarkNodeRole.Arena, 40, 18),
            };
            var edges = new[]
            {
                Edge("BOSS_LOW_01", "BOSS_ENTRY", "BOSS_GATE", SpecialLandmarkRouteKind.Low, 1, true),
                Edge("BOSS_LOW_02", "BOSS_GATE", "BOSS_LOWER_OBSERVE", SpecialLandmarkRouteKind.Low, 2, true),
                Edge("BOSS_LOW_03", "BOSS_LOWER_OBSERVE", "BOSS_CENTRAL_RECOVERY", SpecialLandmarkRouteKind.Low, 3, true),
                Edge("BOSS_LOW_04", "BOSS_CENTRAL_RECOVERY", "BOSS_ARENA", SpecialLandmarkRouteKind.Low, 4, true),
                Edge("BOSS_LOW_05", "BOSS_ARENA", "BOSS_RETURN", SpecialLandmarkRouteKind.Low, 5, true),
                Edge("BOSS_HIGH_01", "BOSS_ENTRY", "BOSS_GATE", SpecialLandmarkRouteKind.High, 1, false),
                Edge("BOSS_HIGH_02", "BOSS_GATE", "BOSS_UPPER_A", SpecialLandmarkRouteKind.High, 2, false),
                Edge("BOSS_HIGH_03", "BOSS_UPPER_A", "BOSS_FALL_OBJECT", SpecialLandmarkRouteKind.High, 3, false),
                Edge("BOSS_HIGH_04", "BOSS_FALL_OBJECT", "BOSS_PRESSURE_DEVICE", SpecialLandmarkRouteKind.High, 4, false),
                Edge("BOSS_HIGH_05", "BOSS_PRESSURE_DEVICE", "BOSS_ARENA", SpecialLandmarkRouteKind.High, 5, false),
                Edge("BOSS_HIGH_06", "BOSS_ARENA", "BOSS_RETURN", SpecialLandmarkRouteKind.High, 6, false),
                Edge("BOSS_FAIL_BRANCH_01", "BOSS_FALL_OBJECT", "BOSS_FAIL_FALL", SpecialLandmarkRouteKind.High, 101, false),
                Edge("BOSS_FAIL_BRANCH_02", "BOSS_PRESSURE_DEVICE", "BOSS_FAIL_PRESSURE", SpecialLandmarkRouteKind.High, 102, false),
                Edge("BOSS_RECOVERY_01", "BOSS_FAIL_FALL", "BOSS_CENTRAL_RECOVERY", SpecialLandmarkRouteKind.Recovery, 1, true),
                Edge("BOSS_RECOVERY_02", "BOSS_FAIL_PRESSURE", "BOSS_CENTRAL_RECOVERY", SpecialLandmarkRouteKind.Recovery, 1, true),
                Edge("BOSS_RETURN", "BOSS_CENTRAL_RECOVERY", "BOSS_RETURN", SpecialLandmarkRouteKind.Return, 1, true),
            };
            var routes = new[]
            {
                Route("BOSS_LOW", SpecialLandmarkRouteKind.Low, "BOSS_ENTRY", "BOSS_RETURN",
                    "BOSS_LOW_01", "BOSS_LOW_02", "BOSS_LOW_03", "BOSS_LOW_04", "BOSS_LOW_05"),
                Route("BOSS_HIGH", SpecialLandmarkRouteKind.High, "BOSS_ENTRY", "BOSS_RETURN",
                    "BOSS_HIGH_01", "BOSS_HIGH_02", "BOSS_HIGH_03", "BOSS_HIGH_04", "BOSS_HIGH_05", "BOSS_HIGH_06"),
                Route("BOSS_RECOVERY_FALL", SpecialLandmarkRouteKind.Recovery, "BOSS_FAIL_FALL", "BOSS_CENTRAL_RECOVERY", "BOSS_RECOVERY_01"),
                Route("BOSS_RECOVERY_PRESSURE", SpecialLandmarkRouteKind.Recovery, "BOSS_FAIL_PRESSURE", "BOSS_CENTRAL_RECOVERY", "BOSS_RECOVERY_02"),
                Route("BOSS_RETURN", SpecialLandmarkRouteKind.Return, "BOSS_CENTRAL_RECOVERY", "BOSS_RETURN", "BOSS_RETURN"),
            };
            var states = new[]
            {
                State("BOSS_GATE_LOCKED", SpecialLandmarkStateRole.GateLocked, true),
                State("BOSS_GATE_ACCEPTED", SpecialLandmarkStateRole.GateAccepted, true),
                State("BOSS_ENCOUNTER_ACTIVE", SpecialLandmarkStateRole.EncounterActive, true),
                State("BOSS_DEFEATED", SpecialLandmarkStateRole.Defeated, true),
            };
            var transitions = new[]
            {
                Transition("BOSS_ACCEPT_SEAL", "SL_STATE_BOSS_GATE_LOCKED", "SL_STATE_BOSS_GATE_ACCEPTED", SpecialLandmarkTransitionTrigger.PresentMoonSeal, 1),
                Transition("BOSS_ENTER", "SL_STATE_BOSS_GATE_ACCEPTED", "SL_STATE_BOSS_ENCOUNTER_ACTIVE", SpecialLandmarkTransitionTrigger.EnterEncounter, 2),
                Transition("BOSS_FAIL", "SL_STATE_BOSS_ENCOUNTER_ACTIVE", "SL_STATE_BOSS_ENCOUNTER_ACTIVE", SpecialLandmarkTransitionTrigger.EncounterFailed, 3),
                Transition("BOSS_WIN", "SL_STATE_BOSS_ENCOUNTER_ACTIVE", "SL_STATE_BOSS_DEFEATED", SpecialLandmarkTransitionTrigger.BossDefeated, 4),
            };
            var resets = new[]
            {
                Reset("BOSS_FALL", SpecialLandmarkResetPolicy.SafeReturn, "BOSS_FAIL_FALL", "BOSS_CENTRAL_RECOVERY"),
                Reset("BOSS_PRESSURE", SpecialLandmarkResetPolicy.SafeReturn, "BOSS_FAIL_PRESSURE", "BOSS_CENTRAL_RECOVERY"),
                new SpecialLandmarkResetDefinition("SL_RESET_BOSS_ENCOUNTER", SpecialLandmarkResetPolicy.EncounterReset,
                    string.Empty, "SL_NODE_BOSS_CENTRAL_RECOVERY", "SL_STATE_BOSS_ENCOUNTER_ACTIVE",
                    "SL_STATE_BOSS_ENCOUNTER_ACTIVE", false, true, false),
            };
            var encounterKey = SpecialPersistenceKey.ForSlot(
                regionId, SpecialPersistenceScope.Encounter, encounterSlot);
            var markers = new[]
            {
                Marker("BOSS_SEAL_REQUIREMENT", SpecialLandmarkMarkerKind.MoonSealRequirement, "BOSS_GATE"),
                Marker("BOSS_LOWER_RECOVERY", SpecialLandmarkMarkerKind.LowerRecoveryZone, "BOSS_CENTRAL_RECOVERY"),
                Marker("BOSS_UPPER_PLATFORM", SpecialLandmarkMarkerKind.UpperPlatform, "BOSS_UPPER_A", 1, false),
                Marker("BOSS_FALLING_OBJECT", SpecialLandmarkMarkerKind.FallingObject, "BOSS_FALL_OBJECT", 2, false),
                Marker("BOSS_PRESSURE_DEVICE", SpecialLandmarkMarkerKind.PressureDevice, "BOSS_PRESSURE_DEVICE", 3, false),
                new SpecialLandmarkMarkerDefinition("SL_MARKER_BOSS_ENCOUNTER_PERSISTENCE",
                    SpecialLandmarkMarkerKind.EncounterPersistence, "SL_NODE_BOSS_DEFEATED", "SL_STATE_BOSS_DEFEATED",
                    4, true, SpecialLandmarkDependencyKind.None, SpecialPersistenceScope.Encounter, encounterKey),
                Marker("BOSS_MARU_SEPARATE_OWNER", SpecialLandmarkMarkerKind.SeparateMaruStateOwner, "BOSS_ARENA"),
            };
            return Definition(
                regionId, SpecialLandmarkKind.BossSealArena, SpecialRegionKind.Boss,
                SpecialLandmarkTheme.MoonPalaceCommon, SpecialLandmarkBindingKind.PlacedMandatorySite,
                1, 1, new LocalTileCoord(0, 0), 48, 32,
                Chunks((0,0),(1,0),(2,0),(3,0),(0,1),(1,1),(2,1),(3,1),(0,2),(1,2),(2,2),(1,3)),
                nodes, edges, routes, states, transitions, resets, markers,
                Array.Empty<SpecialLandmarkForgeLedgerDefinition>(), null,
                Array.Empty<SpecialLandmarkMerchantVariant>(), "Boss Seal Arena starter");
        }

        private static SpecialLandmarkRegionDefinition CreateMerchant()
        {
            var nodes = new[]
            {
                Node("MERCHANT_ENTRY", SpecialLandmarkNodeRole.Entry, 1, 7),
                Node("MERCHANT_SAFE", SpecialLandmarkNodeRole.SafeZone, 5, 7),
                Node("MERCHANT_SHOP", SpecialLandmarkNodeRole.Shop, 10, 7),
                Node("MERCHANT_STORAGE", SpecialLandmarkNodeRole.Storage, 8, 13, false),
                Node("MERCHANT_INFO", SpecialLandmarkNodeRole.Mastery, 14, 13, false),
                Node("MERCHANT_BENEFIT", SpecialLandmarkNodeRole.Mastery, 18, 11, false),
                Node("MERCHANT_RETURN", SpecialLandmarkNodeRole.Return, 22, 7),
            };
            var edges = new[]
            {
                OptionalEdge("MERCHANT_LOW_01", "MERCHANT_ENTRY", "MERCHANT_SAFE", SpecialLandmarkRouteKind.Low, 1),
                OptionalEdge("MERCHANT_LOW_02", "MERCHANT_SAFE", "MERCHANT_SHOP", SpecialLandmarkRouteKind.Low, 2),
                OptionalEdge("MERCHANT_LOW_03", "MERCHANT_SHOP", "MERCHANT_RETURN", SpecialLandmarkRouteKind.Low, 3),
                OptionalEdge("MERCHANT_HIGH_01", "MERCHANT_ENTRY", "MERCHANT_STORAGE", SpecialLandmarkRouteKind.High, 1),
                OptionalEdge("MERCHANT_HIGH_02", "MERCHANT_STORAGE", "MERCHANT_INFO", SpecialLandmarkRouteKind.High, 2),
                OptionalEdge("MERCHANT_HIGH_03", "MERCHANT_INFO", "MERCHANT_BENEFIT", SpecialLandmarkRouteKind.High, 3),
                OptionalEdge("MERCHANT_HIGH_04", "MERCHANT_BENEFIT", "MERCHANT_RETURN", SpecialLandmarkRouteKind.High, 4),
                OptionalEdge("MERCHANT_RETURN", "MERCHANT_SAFE", "MERCHANT_RETURN", SpecialLandmarkRouteKind.Return, 1),
            };
            var routes = new[]
            {
                Route("MERCHANT_LOW", SpecialLandmarkRouteKind.Low, "MERCHANT_ENTRY", "MERCHANT_RETURN",
                    "MERCHANT_LOW_01", "MERCHANT_LOW_02", "MERCHANT_LOW_03"),
                Route("MERCHANT_HIGH", SpecialLandmarkRouteKind.High, "MERCHANT_ENTRY", "MERCHANT_RETURN",
                    "MERCHANT_HIGH_01", "MERCHANT_HIGH_02", "MERCHANT_HIGH_03", "MERCHANT_HIGH_04"),
                Route("MERCHANT_RETURN", SpecialLandmarkRouteKind.Return, "MERCHANT_SAFE", "MERCHANT_RETURN", "MERCHANT_RETURN"),
            };
            var states = new[]
            {
                State("MERCHANT_AVAILABLE", SpecialLandmarkStateRole.MerchantAvailable, true),
                State("MERCHANT_VISITED", SpecialLandmarkStateRole.Visited, true),
                State("MERCHANT_DEPARTED", SpecialLandmarkStateRole.Departed, true),
            };
            var transitions = new[]
            {
                Transition("MERCHANT_VISIT", "SL_STATE_MERCHANT_AVAILABLE", "SL_STATE_MERCHANT_VISITED", SpecialLandmarkTransitionTrigger.MerchantVisited, 1),
                Transition("MERCHANT_DEPART", "SL_STATE_MERCHANT_VISITED", "SL_STATE_MERCHANT_DEPARTED", SpecialLandmarkTransitionTrigger.MerchantDeparted, 2),
            };
            var resets = new[]
            {
                new SpecialLandmarkResetDefinition("SL_RESET_MERCHANT_STABLE_VISIT", SpecialLandmarkResetPolicy.StableVisit,
                    string.Empty, "SL_NODE_MERCHANT_SAFE", "SL_STATE_MERCHANT_VISITED", "SL_STATE_MERCHANT_VISITED",
                    false, false, true),
            };
            var markers = new[]
            {
                Marker("MERCHANT_SAFE_ZONE", SpecialLandmarkMarkerKind.ShopSafeZone, "MERCHANT_SAFE"),
                Marker("MERCHANT_CUE_LEFT", SpecialLandmarkMarkerKind.EntranceCue, "MERCHANT_ENTRY", 1),
                Marker("MERCHANT_CUE_LIGHT", SpecialLandmarkMarkerKind.EntranceCue, "MERCHANT_ENTRY", 2),
                Marker("MERCHANT_SHOP", SpecialLandmarkMarkerKind.Shop, "MERCHANT_SHOP"),
                Marker("MERCHANT_STORAGE", SpecialLandmarkMarkerKind.UpperStorage, "MERCHANT_STORAGE", 1, false),
                Marker("MERCHANT_INFORMATION", SpecialLandmarkMarkerKind.Information, "MERCHANT_INFO", 2, false),
                Marker("MERCHANT_OPTIONAL_BENEFIT", SpecialLandmarkMarkerKind.OptionalBenefit, "MERCHANT_BENEFIT", 3, false),
            };
            return Definition(
                new SpecialRegionId("SR_WANDERING_MERCHANT_CAVE_3"),
                SpecialLandmarkKind.WanderingMerchantCave, SpecialRegionKind.OptionalLandmark,
                SpecialLandmarkTheme.Any, SpecialLandmarkBindingKind.DeferredOptionalLocal,
                0, 0, new LocalTileCoord(0, 0), 24, 16,
                Chunks((0,0),(1,0),(0,1)), nodes, edges, routes, states, transitions, resets, markers,
                Array.Empty<SpecialLandmarkForgeLedgerDefinition>(), null,
                new[]
                {
                    SpecialLandmarkMerchantVariant.Alien,
                    SpecialLandmarkMerchantVariant.Rabbit,
                    SpecialLandmarkMerchantVariant.Spacefarer,
                    SpecialLandmarkMerchantVariant.Machine,
                }, "Wandering Merchant Cave starter");
        }

        private static SpecialLandmarkRegionDefinition CreateMaru()
        {
            var nodes = new[]
            {
                Node("MARU_ENTRY", SpecialLandmarkNodeRole.Entry, 1, 8),
                Node("MARU_SAFE", SpecialLandmarkNodeRole.SafeZone, 5, 8),
                Node("MARU_PREVIEW", SpecialLandmarkNodeRole.Shrine, 9, 8),
                Node("MARU_CHOICE", SpecialLandmarkNodeRole.Shrine, 13, 8),
                Node("MARU_STRONG", SpecialLandmarkNodeRole.Mastery, 14, 17, false),
                Node("MARU_FAILURE", SpecialLandmarkNodeRole.Failure, 9, 17, false),
                Node("MARU_RETURN", SpecialLandmarkNodeRole.Return, 22, 8),
            };
            var edges = new[]
            {
                OptionalEdge("MARU_LOW_01", "MARU_ENTRY", "MARU_SAFE", SpecialLandmarkRouteKind.Low, 1),
                OptionalEdge("MARU_LOW_02", "MARU_SAFE", "MARU_PREVIEW", SpecialLandmarkRouteKind.Low, 2),
                OptionalEdge("MARU_LOW_03", "MARU_PREVIEW", "MARU_CHOICE", SpecialLandmarkRouteKind.Low, 3),
                OptionalEdge("MARU_LOW_04", "MARU_CHOICE", "MARU_RETURN", SpecialLandmarkRouteKind.Low, 4),
                OptionalEdge("MARU_HIGH_01", "MARU_ENTRY", "MARU_SAFE", SpecialLandmarkRouteKind.High, 1),
                OptionalEdge("MARU_HIGH_02", "MARU_SAFE", "MARU_PREVIEW", SpecialLandmarkRouteKind.High, 2),
                OptionalEdge("MARU_HIGH_03", "MARU_PREVIEW", "MARU_CHOICE", SpecialLandmarkRouteKind.High, 3),
                OptionalEdge("MARU_HIGH_04", "MARU_CHOICE", "MARU_STRONG", SpecialLandmarkRouteKind.High, 4),
                OptionalEdge("MARU_HIGH_05", "MARU_STRONG", "MARU_RETURN", SpecialLandmarkRouteKind.High, 5),
                OptionalEdge("MARU_FAIL_BRANCH", "MARU_STRONG", "MARU_FAILURE", SpecialLandmarkRouteKind.High, 101),
                OptionalEdge("MARU_RECOVERY", "MARU_FAILURE", "MARU_SAFE", SpecialLandmarkRouteKind.Recovery, 1),
                OptionalEdge("MARU_RETURN", "MARU_SAFE", "MARU_RETURN", SpecialLandmarkRouteKind.Return, 1),
            };
            var routes = new[]
            {
                Route("MARU_LOW", SpecialLandmarkRouteKind.Low, "MARU_ENTRY", "MARU_RETURN",
                    "MARU_LOW_01", "MARU_LOW_02", "MARU_LOW_03", "MARU_LOW_04"),
                Route("MARU_HIGH", SpecialLandmarkRouteKind.High, "MARU_ENTRY", "MARU_RETURN",
                    "MARU_HIGH_01", "MARU_HIGH_02", "MARU_HIGH_03", "MARU_HIGH_04", "MARU_HIGH_05"),
                Route("MARU_RECOVERY", SpecialLandmarkRouteKind.Recovery, "MARU_FAILURE", "MARU_SAFE", "MARU_RECOVERY"),
                Route("MARU_RETURN", SpecialLandmarkRouteKind.Return, "MARU_SAFE", "MARU_RETURN", "MARU_RETURN"),
            };
            var states = new[]
            {
                State("MARU_OFFERED", SpecialLandmarkStateRole.Offered, true),
                State("MARU_IGNORED", SpecialLandmarkStateRole.Ignored, true),
                State("MARU_SHORT_HINT", SpecialLandmarkStateRole.ShortHint, true),
                State("MARU_STRONG_HINT", SpecialLandmarkStateRole.StrongHint, true),
            };
            var transitions = new[]
            {
                Transition("MARU_IGNORE", "SL_STATE_MARU_OFFERED", "SL_STATE_MARU_IGNORED", SpecialLandmarkTransitionTrigger.IgnoreChoice, 1),
                Transition("MARU_SHORT", "SL_STATE_MARU_OFFERED", "SL_STATE_MARU_SHORT_HINT", SpecialLandmarkTransitionTrigger.ChooseShortHint, 2),
                Transition("MARU_STRONG", "SL_STATE_MARU_OFFERED", "SL_STATE_MARU_STRONG_HINT", SpecialLandmarkTransitionTrigger.ChooseStrongHint, 3),
            };
            var resets = new[]
            {
                new SpecialLandmarkResetDefinition("SL_RESET_MARU_PERSISTENT_CHOICE", SpecialLandmarkResetPolicy.PersistentChoice,
                    string.Empty, "SL_NODE_MARU_SAFE", "SL_STATE_MARU_STRONG_HINT", "SL_STATE_MARU_STRONG_HINT",
                    false, false, true),
                Reset("MARU_SAFE_RETURN", SpecialLandmarkResetPolicy.SafeReturn, "MARU_FAILURE", "MARU_SAFE"),
            };
            var markers = new[]
            {
                Marker("MARU_SAFE_ZONE", SpecialLandmarkMarkerKind.NonCombatSafeZone, "MARU_SAFE"),
                Marker("MARU_CHOICE_PREVIEW", SpecialLandmarkMarkerKind.ChoicePreview, "MARU_PREVIEW", 0),
                Marker("MARU_SHORT_HINT", SpecialLandmarkMarkerKind.ShortHint, "MARU_CHOICE", 1, false),
                Marker("MARU_RARE_TERRAIN_COMPASS", SpecialLandmarkMarkerKind.RareTerrainCompass, "MARU_STRONG", 2, false),
                Marker("MARU_ATTENTION_INCREASE", SpecialLandmarkMarkerKind.MaruAttentionIncrease, "MARU_STRONG", 3, false),
            };
            return Definition(
                new SpecialRegionId("SR_MARU_TIME_SHRINE_5"),
                SpecialLandmarkKind.MaruTimeShrine, SpecialRegionKind.OptionalLandmark,
                SpecialLandmarkTheme.MoonPalaceCommon, SpecialLandmarkBindingKind.DeferredOptionalLocal,
                0, 0, new LocalTileCoord(0, 0), 24, 24,
                Chunks((0,0),(1,0),(0,1),(1,1),(0,2)), nodes, edges, routes, states, transitions, resets, markers,
                Array.Empty<SpecialLandmarkForgeLedgerDefinition>(), null,
                Array.Empty<SpecialLandmarkMerchantVariant>(), "Maru Time Shrine starter");
        }

        private static SpecialLandmarkRegionDefinition Definition(
            SpecialRegionId regionId,
            SpecialLandmarkKind landmark,
            SpecialRegionKind regionKind,
            SpecialLandmarkTheme theme,
            SpecialLandmarkBindingKind binding,
            int reservedWidth,
            int reservedHeight,
            LocalTileCoord designOrigin,
            int designWidth,
            int designHeight,
            IEnumerable<SpecialLandmarkDesignChunk> chunks,
            IEnumerable<SpecialLandmarkShellNode> nodes,
            IEnumerable<SpecialLandmarkShellEdge> edges,
            IEnumerable<SpecialLandmarkRouteDefinition> routes,
            IEnumerable<SpecialLandmarkStateDefinition> states,
            IEnumerable<SpecialLandmarkStateTransitionDefinition> transitions,
            IEnumerable<SpecialLandmarkResetDefinition> resets,
            IEnumerable<SpecialLandmarkMarkerDefinition> markers,
            IEnumerable<SpecialLandmarkForgeLedgerDefinition> ledgers,
            SpecialLandmarkRewardDefinition reward,
            IEnumerable<SpecialLandmarkMerchantVariant> variants,
            string display)
            => new SpecialLandmarkRegionDefinition(
                regionId, landmark, regionKind, theme, binding, reservedWidth, reservedHeight,
                designOrigin, designWidth, designHeight, 12, 8, chunks, nodes, edges, routes,
                states, transitions, resets, markers, ledgers, reward, variants,
                false, false, false, display);

        private static SpecialLandmarkShellNode Node(
            string id, SpecialLandmarkNodeRole role, int x, int y, bool required = true)
            => new SpecialLandmarkShellNode("SL_NODE_" + id, role, new LocalTileCoord(x, y), required);

        private static SpecialLandmarkShellEdge Edge(
            string id, string from, string to, SpecialLandmarkRouteKind kind, int order, bool required)
            => new SpecialLandmarkShellEdge(
                "SL_EDGE_" + id, "SL_NODE_" + from, "SL_NODE_" + to, kind, order,
                kind == SpecialLandmarkRouteKind.High ? AccessClass.OptionalNoTool : AccessClass.MandatoryNoTool,
                required, SpecialLandmarkDependencyKind.None);

        private static SpecialLandmarkShellEdge OptionalEdge(
            string id, string from, string to, SpecialLandmarkRouteKind kind, int order)
            => new SpecialLandmarkShellEdge(
                "SL_EDGE_" + id, "SL_NODE_" + from, "SL_NODE_" + to, kind, order,
                AccessClass.OptionalNoTool, false, SpecialLandmarkDependencyKind.None);

        private static SpecialLandmarkRouteDefinition Route(
            string id, SpecialLandmarkRouteKind kind, string start, string end, params string[] edges)
            => new SpecialLandmarkRouteDefinition(
                "SL_ROUTE_" + id, kind, edges.Select(value => "SL_EDGE_" + value),
                "SL_NODE_" + start, "SL_NODE_" + end);

        private static SpecialLandmarkStateDefinition State(
            string id, SpecialLandmarkStateRole role, bool persistent = false)
            => new SpecialLandmarkStateDefinition("SL_STATE_" + id, role, persistent);

        private static SpecialLandmarkStateTransitionDefinition Transition(
            string id, string from, string to, SpecialLandmarkTransitionTrigger trigger, int order)
            => new SpecialLandmarkStateTransitionDefinition(
                "SL_TRANSITION_" + id, from, to, trigger, order);

        private static SpecialLandmarkResetDefinition Reset(
            string id,
            SpecialLandmarkResetPolicy policy,
            string failure,
            string recovery,
            bool returnsAll = false)
            => new SpecialLandmarkResetDefinition(
                "SL_RESET_" + id, policy, "SL_NODE_" + failure, "SL_NODE_" + recovery,
                string.Empty, string.Empty, returnsAll, false, false);

        private static SpecialLandmarkMarkerDefinition Marker(
            string id,
            SpecialLandmarkMarkerKind kind,
            string node,
            int order = 0,
            bool required = true)
            => new SpecialLandmarkMarkerDefinition(
                "SL_MARKER_" + id, kind, "SL_NODE_" + node, string.Empty, order, required,
                SpecialLandmarkDependencyKind.None);

        private static SpecialLandmarkDesignChunk[] Chunks(
            params (int X, int Y)[] values)
            => values.Select(value => new SpecialLandmarkDesignChunk(value.X, value.Y)).ToArray();

        private static string ComputeCatalogDigest(IEnumerable<SpecialLandmarkRegionDefinition> definitions)
        {
            var material = string.Join("\n", definitions.OrderBy(value => value.RegionId)
                .Select(value => value.RegionId.Value + "=" + value.CanonicalDigest));
            using (var algorithm = SHA256.Create())
            {
                var bytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(material));
                var result = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes) result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }
    }
}
