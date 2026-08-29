using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.SpecialRegions
{
    [TestFixture]
    [Category("MAP13_07")]
    public sealed class SpecialLandmarkRegionAuthoringTests
    {
        private IReadOnlyDictionary<SpecialLandmarkKind, PlacedFixture> placedFixtures;
        private IReadOnlyDictionary<SpecialLandmarkKind, SpecialLandmarkResult> results;

        [OneTimeSetUp]
        public void SetUp()
        {
            var quietCandidate = BuildQuietCandidate(false);
            placedFixtures = SpecialLandmarkRegionStarterCatalog.Entries
                .Where(value => value.Binding == SpecialLandmarkBindingKind.PlacedMandatorySite)
                .ToDictionary(value => value.Landmark,
                    value => BuildPlacedFixture(value, false, quietCandidate));
            results = SpecialLandmarkRegionStarterCatalog.Entries.ToDictionary(
                value => value.Landmark, Compile);
            foreach (var result in results.Values) AssertSuccess(result);
        }

        [Test]
        public void CatalogPublishesExactFourCanonicalStarterMatrixAndCounts()
        {
            var entries = SpecialLandmarkRegionStarterCatalog.Entries;
            Assert.That(entries.Select(value => value.RegionId.Value), Is.EqualTo(new[]
            {
                "SR_MARU_TIME_SHRINE_5",
                "SR_MOON_BOSS_SEAL_ARENA_12",
                "SR_MOON_SEAL_FORGE_9",
                "SR_WANDERING_MERCHANT_CAVE_3",
            }));
            Assert.That(entries.Select(value => value.Landmark).Distinct().Count(), Is.EqualTo(4));
            Assert.That(SpecialLandmarkRegionStarterCatalog.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));

            AssertCounts(SpecialLandmarkKind.MoonSealForge, 9, 13, 22, 6, 14, 9, 3, 12);
            AssertCounts(SpecialLandmarkKind.BossSealArena, 12, 12, 16, 5, 4, 4, 3, 7);
            AssertCounts(SpecialLandmarkKind.WanderingMerchantCave, 3, 7, 8, 3, 3, 2, 1, 7);
            AssertCounts(SpecialLandmarkKind.MaruTimeShrine, 5, 7, 12, 4, 4, 3, 2, 5);
        }

        [Test]
        public void FourDesignGridsAreExactBoundedUniqueAndConnected()
        {
            var matrix = new Dictionary<SpecialLandmarkKind, Tuple<int, int, int, int, int>>
            {
                { SpecialLandmarkKind.MoonSealForge, Tuple.Create(4, 3, 9, 48, 24) },
                { SpecialLandmarkKind.BossSealArena, Tuple.Create(4, 4, 12, 48, 32) },
                { SpecialLandmarkKind.WanderingMerchantCave, Tuple.Create(2, 2, 3, 24, 16) },
                { SpecialLandmarkKind.MaruTimeShrine, Tuple.Create(2, 3, 5, 24, 24) },
            };
            foreach (var definition in SpecialLandmarkRegionStarterCatalog.Entries)
            {
                var expected = matrix[definition.Landmark];
                Assert.That(definition.DesignWidth / 12, Is.EqualTo(expected.Item1));
                Assert.That(definition.DesignHeight / 8, Is.EqualTo(expected.Item2));
                Assert.That(definition.ActiveDesignChunks, Has.Count.EqualTo(expected.Item3));
                Assert.That(definition.DesignWidth, Is.EqualTo(expected.Item4));
                Assert.That(definition.DesignHeight, Is.EqualTo(expected.Item5));
                Assert.That(definition.ActiveDesignChunks.Distinct().Count(), Is.EqualTo(expected.Item3));
                Assert.That(IsConnected(definition.ActiveDesignChunks), Is.True);
                Assert.That(definition.Nodes.All(node =>
                    node.Coordinate.X >= definition.DesignOrigin.X &&
                    node.Coordinate.Y >= definition.DesignOrigin.Y &&
                    node.Coordinate.X < definition.DesignOrigin.X + definition.DesignWidth &&
                    node.Coordinate.Y < definition.DesignOrigin.Y + definition.DesignHeight), Is.True);
            }
        }

        [Test]
        public void ForgePlacedSourcesAndLowHighRoutesPreserveExactFourStepOrder()
        {
            var definition = Definition(SpecialLandmarkKind.MoonSealForge);
            var fixture = placedFixtures[SpecialLandmarkKind.MoonSealForge];
            var plan = results[SpecialLandmarkKind.MoonSealForge].Plan;
            Assert.That(fixture.Bridge.RegionKind, Is.EqualTo(SpecialRegionKind.Forge));
            Assert.That(fixture.Bridge.Width, Is.EqualTo(1));
            Assert.That(fixture.Bridge.Height, Is.EqualTo(1));
            Assert.That(plan.PlacementStatus, Is.EqualTo(SpecialLandmarkPlacementStatus.Placed));
            Assert.That(plan.BridgeDigest, Is.EqualTo(fixture.Bridge.CanonicalDigest));
            Assert.That(plan.EntryBufferDigest, Is.EqualTo(fixture.EntryPlan.CanonicalDigest));
            Assert.That(plan.CollisionDigest, Is.EqualTo(fixture.CollisionPlan.CanonicalDigest));
            Assert.That(plan.FixedSlotLayerDigest, Is.EqualTo(fixture.LayerPlan.CanonicalDigest));
            var process = definition.Markers.Where(value =>
                    value.Kind == SpecialLandmarkMarkerKind.ForgeProcessStep)
                .OrderBy(value => value.Order).Select(value => value.NodeId).ToArray();
            Assert.That(process, Is.EqualTo(new[]
            {
                "SL_NODE_FORGE_GRIND", "SL_NODE_FORGE_MIX",
                "SL_NODE_FORGE_PRESS", "SL_NODE_FORGE_CURE",
            }));
            foreach (var kind in new[] { SpecialLandmarkRouteKind.Low, SpecialLandmarkRouteKind.High })
            {
                var witness = plan.Witnesses.Single(value => value.Kind == kind);
                var indices = process.Select(node => witness.NodeIds.ToList().IndexOf(node)).ToArray();
                Assert.That(indices, Is.Ordered);
                Assert.That(witness.NodeIds.First(), Is.EqualTo("SL_NODE_FORGE_ENTRY"));
                Assert.That(witness.NodeIds.Last(), Is.EqualTo("SL_NODE_FORGE_RETURN"));
            }
        }

        [Test]
        public void ForgeSuccessConsumesOnlyOnSuccessAndEveryFailureReturnsAllResources()
        {
            var definition = Definition(SpecialLandmarkKind.MoonSealForge);
            var plan = results[SpecialLandmarkKind.MoonSealForge].Plan;
            Assert.That(definition.ForgeLedgers.Select(value => value.Resource), Is.EqualTo(new[]
            {
                SpecialLandmarkForgeResource.MoonCore,
                SpecialLandmarkForgeResource.CassiaSap,
                SpecialLandmarkForgeResource.StarNuruk,
            }));
            Assert.That(definition.ForgeLedgers, Has.Count.EqualTo(3));
            Assert.That(definition.Resets, Has.Count.EqualTo(3));
            Assert.That(definition.Resets.All(value => value.ReturnsAllForgeInputs &&
                value.Policy == SpecialLandmarkResetPolicy.ManualReset &&
                value.RecoveryNodeId == "SL_NODE_FORGE_SAFE_CORRIDOR"), Is.True);
            Assert.That(plan.ForgePermanentLossCount, Is.Zero);
            Assert.That(definition.RequiredReward.Amount, Is.EqualTo(1));
            Assert.That(definition.RequiredReward.Required, Is.True);
            Assert.That(definition.RequiredReward.SlotId.Value, Is.EqualTo("SR_SLOT_MOON_SEAL_REWARD"));
            Assert.That(definition.RequiredReward.PersistenceKey, Is.EqualTo(
                SpecialPersistenceKey.ForSlot(definition.RegionId, SpecialPersistenceScope.Reward,
                    definition.RequiredReward.SlotId)));
            Assert.That(placedFixtures[SpecialLandmarkKind.MoonSealForge].SafetyProof.IsSafe, Is.True);
            Assert.That(plan.InventoryMutationCount + plan.RewardGrantCount + plan.SaveWriteCount, Is.Zero);
        }

        [Test]
        public void BossGateEncounterResetAndAllFallsRecoverToCentralLowerNode()
        {
            var definition = Definition(SpecialLandmarkKind.BossSealArena);
            var plan = results[SpecialLandmarkKind.BossSealArena].Plan;
            Assert.That(definition.States.Select(value => value.Role), Is.EquivalentTo(new[]
            {
                SpecialLandmarkStateRole.GateLocked,
                SpecialLandmarkStateRole.GateAccepted,
                SpecialLandmarkStateRole.EncounterActive,
                SpecialLandmarkStateRole.Defeated,
            }));
            Assert.That(definition.Transitions.OrderBy(value => value.Order).Select(value => value.Trigger),
                Is.EqualTo(new[]
                {
                    SpecialLandmarkTransitionTrigger.PresentMoonSeal,
                    SpecialLandmarkTransitionTrigger.EnterEncounter,
                    SpecialLandmarkTransitionTrigger.EncounterFailed,
                    SpecialLandmarkTransitionTrigger.BossDefeated,
                }));
            var failures = definition.Nodes.Where(value => value.Role == SpecialLandmarkNodeRole.Failure).ToArray();
            Assert.That(failures, Has.Length.EqualTo(2));
            Assert.That(failures.All(failure => definition.Resets.Any(reset =>
                reset.FailureNodeId == failure.NodeId &&
                reset.RecoveryNodeId == "SL_NODE_BOSS_CENTRAL_RECOVERY")), Is.True);
            Assert.That(definition.Resets.Single(value =>
                value.Policy == SpecialLandmarkResetPolicy.EncounterReset).PreservesSealAcceptance, Is.True);
            Assert.That(plan.Witnesses.Count(value => value.Kind == SpecialLandmarkRouteKind.Recovery), Is.EqualTo(2));
        }

        [Test]
        public void BossUsesExistingMovementAndKeepsMaruStateSeparateWithoutGameplayMutation()
        {
            var definition = Definition(SpecialLandmarkKind.BossSealArena);
            var plan = results[SpecialLandmarkKind.BossSealArena].Plan;
            Assert.That(definition.IntroducesNewMovementRule, Is.False);
            Assert.That(definition.Markers.Count(value =>
                value.Kind == SpecialLandmarkMarkerKind.SeparateMaruStateOwner), Is.EqualTo(1));
            Assert.That(definition.Markers.Count(value =>
                value.Kind == SpecialLandmarkMarkerKind.MoonSealRequirement), Is.EqualTo(1));
            Assert.That(definition.Markers.Count(value =>
                value.Kind == SpecialLandmarkMarkerKind.EncounterPersistence), Is.EqualTo(1));
            Assert.That(plan.GameplayExecutionCount + plan.InventoryMutationCount + plan.WorldMutationCount +
                        plan.TileMutationCount + plan.SaveWriteCount, Is.Zero);
        }

        [Test]
        public void MerchantDeferredShellHasSafeZoneTwoCuesLowHighAndShortReturn()
        {
            var definition = Definition(SpecialLandmarkKind.WanderingMerchantCave);
            var plan = results[SpecialLandmarkKind.WanderingMerchantCave].Plan;
            Assert.That(definition.Binding, Is.EqualTo(SpecialLandmarkBindingKind.DeferredOptionalLocal));
            Assert.That(plan.PlacementStatus, Is.EqualTo(SpecialLandmarkPlacementStatus.DeferredToMAP14));
            Assert.That(definition.Markers.Count(value =>
                value.Kind == SpecialLandmarkMarkerKind.ShopSafeZone), Is.EqualTo(1));
            Assert.That(definition.Markers.Count(value =>
                value.Kind == SpecialLandmarkMarkerKind.EntranceCue), Is.EqualTo(2));
            Assert.That(definition.Routes.Select(value => value.Kind), Is.EquivalentTo(new[]
            {
                SpecialLandmarkRouteKind.Low,
                SpecialLandmarkRouteKind.High,
                SpecialLandmarkRouteKind.Return,
            }));
            Assert.That(plan.Witnesses.All(value => value.NodeIds.Last() == "SL_NODE_MERCHANT_RETURN"), Is.True);
        }

        [Test]
        public void MerchantVariantsAreExactAndRngOrMandatoryDependencyRemainZero()
        {
            var definition = Definition(SpecialLandmarkKind.WanderingMerchantCave);
            var plan = results[SpecialLandmarkKind.WanderingMerchantCave].Plan;
            Assert.That(definition.MerchantVariants, Is.EqualTo(new[]
            {
                SpecialLandmarkMerchantVariant.Alien,
                SpecialLandmarkMerchantVariant.Rabbit,
                SpecialLandmarkMerchantVariant.Spacefarer,
                SpecialLandmarkMerchantVariant.Machine,
            }));
            Assert.That(definition.States.Select(value => value.Role), Is.EquivalentTo(new[]
            {
                SpecialLandmarkStateRole.MerchantAvailable,
                SpecialLandmarkStateRole.Visited,
                SpecialLandmarkStateRole.Departed,
            }));
            Assert.That(plan.RngSelectionCount, Is.Zero);
            Assert.That(plan.MandatoryOptionalDependencyCount, Is.Zero);
            Assert.That(definition.MandatoryProgressionDependency, Is.False);
        }

        [Test]
        public void MaruPreviewPrecedesChoiceAndPersistentRevisitCannotDuplicateBenefit()
        {
            var definition = Definition(SpecialLandmarkKind.MaruTimeShrine);
            var plan = results[SpecialLandmarkKind.MaruTimeShrine].Plan;
            var preview = definition.Markers.Single(value =>
                value.Kind == SpecialLandmarkMarkerKind.ChoicePreview);
            Assert.That(definition.Transitions.All(value => value.Order > preview.Order), Is.True);
            Assert.That(definition.States.Select(value => value.Role), Is.EquivalentTo(new[]
            {
                SpecialLandmarkStateRole.Offered,
                SpecialLandmarkStateRole.Ignored,
                SpecialLandmarkStateRole.ShortHint,
                SpecialLandmarkStateRole.StrongHint,
            }));
            Assert.That(definition.Markers.Any(value =>
                value.Kind == SpecialLandmarkMarkerKind.RareTerrainCompass), Is.True);
            Assert.That(definition.Markers.Any(value =>
                value.Kind == SpecialLandmarkMarkerKind.MaruAttentionIncrease), Is.True);
            Assert.That(definition.Resets.Any(value =>
                value.Policy == SpecialLandmarkResetPolicy.PersistentChoice && value.PreventsReroll), Is.True);
            Assert.That(plan.DuplicateBenefitRiskCount, Is.Zero);
            Assert.That(plan.Witnesses.Single(value =>
                value.Kind == SpecialLandmarkRouteKind.Recovery).NodeIds.Last(),
                Is.EqualTo("SL_NODE_MARU_SAFE"));
        }

        [Test]
        public void OptionalPlansPublishNoWorldReservationBridgeOrPlacedOwnershipClaims()
        {
            foreach (var kind in new[]
                     {
                         SpecialLandmarkKind.WanderingMerchantCave,
                         SpecialLandmarkKind.MaruTimeShrine,
                     })
            {
                var plan = results[kind].Plan;
                Assert.That(plan.PlacementStatus, Is.EqualTo(SpecialLandmarkPlacementStatus.DeferredToMAP14));
                Assert.That(plan.BridgeDigest + plan.EntryBufferDigest + plan.CollisionDigest +
                            plan.FixedSlotLayerDigest + plan.RewardSafetyDigest + plan.CoreResourceDigest,
                    Is.Empty);
                Assert.That(plan.WorldOriginCount + plan.ReservationClaimCount + plan.BridgeClaimCount +
                            plan.PlacedOwnershipClaimCount + plan.PlacementSolverCount, Is.Zero);
                Assert.That(plan.Nodes.All(value => value.Coordinate.X >= 0 && value.Coordinate.Y >= 0 &&
                    value.Coordinate.X < plan.DesignWidth && value.Coordinate.Y < plan.DesignHeight), Is.True);
            }
        }

        [Test]
        public void InvalidIdentityBindingChunkGraphStateResetResourceSealRecoveryAndDependencyFailAtomically()
        {
            var forge = Definition(SpecialLandmarkKind.MoonSealForge);
            var forgeFixture = placedFixtures[SpecialLandmarkKind.MoonSealForge];
            AssertFailure(Compile(Rebuild(forge, regionId: new SpecialRegionId("SR_WRONG"))),
                SpecialLandmarkErrorCode.RegionIdentityMismatch);
            AssertFailure(Compile(Rebuild(forge,
                    chunks: forge.ActiveDesignChunks.Concat(new[] { new SpecialLandmarkDesignChunk(9, 9) }))),
                SpecialLandmarkErrorCode.InvalidActiveChunk);

            var badEdge = forge.Edges.Select(value => value.EdgeId == "SL_EDGE_FORGE_LOW_01"
                ? new SpecialLandmarkShellEdge(value.EdgeId, value.FromNodeId, value.ToNodeId,
                    value.RouteKind, value.Order, AccessClass.OptionalTool, value.Required,
                    SpecialLandmarkDependencyKind.Tool)
                : value).ToArray();
            AssertFailure(Compile(Rebuild(forge, edges: badEdge)),
                SpecialLandmarkErrorCode.MandatoryOptionalDependency);

            var badStates = forge.States.Concat(new[]
            {
                new SpecialLandmarkStateDefinition(forge.States[0].StateId,
                    SpecialLandmarkStateRole.ForgeReady, false),
            });
            AssertFailure(Compile(Rebuild(forge, states: badStates)), SpecialLandmarkErrorCode.InvalidState);

            var badResets = forge.Resets.Where(value => value.ResetId != "SL_RESET_FORGE_MIX");
            AssertFailure(Compile(Rebuild(forge, resets: badResets)),
                SpecialLandmarkErrorCode.UnrecoverableFailure);

            var badReward = new SpecialLandmarkRewardDefinition(
                "SL_REWARD_MOON_SEAL", "SL_NODE_FORGE_REWARD",
                new SpecialRegionSlotId("SR_SLOT_WRONG"), default(SpecialPersistenceKey), 2, false);
            AssertFailure(Compile(Rebuild(forge, reward: badReward)),
                SpecialLandmarkErrorCode.InvalidSealReward);

            var missingResources = SpecialLandmarkRegionCompiler.Compile(Request(
                forge, forgeFixture, Array.Empty<CoreResourceRegionDefinition>()));
            AssertFailure(missingResources, SpecialLandmarkErrorCode.MissingInput);

            var merchant = Definition(SpecialLandmarkKind.WanderingMerchantCave);
            var worldClaim = SpecialLandmarkRegionCompiler.Compile(new SpecialLandmarkCompileRequest(
                merchant, forgeFixture.Bridge, forgeFixture.Bridge.CanonicalDigest,
                forgeFixture.EntryPlan, forgeFixture.EntryPlan.CanonicalDigest,
                forgeFixture.CollisionPlan, forgeFixture.CollisionPlan.CanonicalDigest,
                forgeFixture.LayerPlan, forgeFixture.LayerPlan.CanonicalDigest,
                null, string.Empty, CoreResourceRegionStarterCatalog.Entries));
            AssertFailure(worldClaim, SpecialLandmarkErrorCode.OptionalWorldBindingClaim);
        }

        [Test]
        public void ReverseRepeatCultureImmutabilityDigestAndAllMutationCountersRemainStable()
        {
            var source = Definition(SpecialLandmarkKind.BossSealArena);
            var chunks = source.ActiveDesignChunks.Reverse().ToList();
            var nodes = source.Nodes.Reverse().ToList();
            var edges = source.Edges.Reverse().ToList();
            var routes = source.Routes.Reverse().ToList();
            var states = source.States.Reverse().ToList();
            var transitions = source.Transitions.Reverse().ToList();
            var resets = source.Resets.Reverse().ToList();
            var markers = source.Markers.Reverse().ToList();
            var reversed = Rebuild(source, chunks, nodes, edges, routes, states, transitions, resets, markers);
            chunks.Clear(); nodes.Clear(); edges.Clear(); routes.Clear(); states.Clear();
            transitions.Clear(); resets.Clear(); markers.Clear();

            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var first = Compile(source);
                var repeat = Compile(source);
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var culture = Compile(reversed);
                AssertSuccess(first);
                AssertSuccess(repeat);
                AssertSuccess(culture);
                Assert.That(repeat.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));
                Assert.That(culture.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));
                Assert.That(reversed.ActiveDesignChunks, Has.Count.EqualTo(12));
                Assert.That(reversed.Nodes, Has.Count.EqualTo(source.Nodes.Count));
                var plan = first.Plan;
                Assert.That(plan.RngSelectionCount + plan.PathfindingCount + plan.CarveCount +
                            plan.TeleportCount + plan.WorldMutationCount + plan.TileMutationCount +
                            plan.InventoryMutationCount + plan.RewardGrantCount + plan.SaveWriteCount +
                            plan.PlacementSolverCount + plan.GameplayExecutionCount, Is.Zero);
                Assert.That(plan.DesignDigest, Does.Match("^[0-9a-f]{64}$"));
                Assert.That(plan.ShellDigest, Does.Match("^[0-9a-f]{64}$"));
                Assert.That(plan.StateDigest, Does.Match("^[0-9a-f]{64}$"));
                Assert.That(plan.MarkerDigest, Does.Match("^[0-9a-f]{64}$"));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }

            var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", ".."));
            var runtimeSources = new[]
            {
                "Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialLandmarkRegionDefinitions.cs",
                "Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialLandmarkRegionCompiler.cs",
                "Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialLandmarkRegionStarterCatalog.cs",
            };
            var text = string.Join("\n", runtimeSources.Select(path => File.ReadAllText(Path.Combine(root, path))));
            foreach (var forbidden in new[]
                     {
                         "UnityEditor", "UnityEngine.Random", "System.Random", "DateTime", "Time.deltaTime",
                         "File.", "Directory.", "MonoBehaviour", "ScriptableObject", "Tilemap",
                         "SaveData", "PlayerPrefs", "Instantiate(", "Destroy(",
                     })
                Assert.That(text, Does.Not.Contain(forbidden), forbidden);
        }

        private SpecialLandmarkResult Compile(SpecialLandmarkRegionDefinition definition)
        {
            if (definition.Binding == SpecialLandmarkBindingKind.DeferredOptionalLocal)
                return SpecialLandmarkRegionCompiler.Compile(new SpecialLandmarkCompileRequest(
                    definition, null, string.Empty, null, string.Empty, null, string.Empty,
                    null, string.Empty, null, string.Empty, null));
            return SpecialLandmarkRegionCompiler.Compile(Request(
                definition, placedFixtures[definition.Landmark], CoreResourceRegionStarterCatalog.Entries));
        }

        private static SpecialLandmarkCompileRequest Request(
            SpecialLandmarkRegionDefinition definition,
            PlacedFixture fixture,
            IEnumerable<CoreResourceRegionDefinition> resources)
            => new SpecialLandmarkCompileRequest(
                definition,
                fixture.Bridge, fixture.Bridge.CanonicalDigest,
                fixture.EntryPlan, fixture.EntryPlan.CanonicalDigest,
                fixture.CollisionPlan, fixture.CollisionPlan.CanonicalDigest,
                fixture.LayerPlan, fixture.LayerPlan.CanonicalDigest,
                fixture.SafetyProof,
                fixture.SafetyProof == null ? string.Empty : fixture.SafetyProof.CanonicalDigest,
                resources);

        private static PlacedFixture BuildPlacedFixture(
            SpecialLandmarkRegionDefinition definition,
            bool reverse,
            TerrainClusterQuietBufferCandidate quietCandidate)
        {
            var token = definition.Landmark.ToString().ToUpperInvariant();
            var reservationId = new SiteReservationId("RES_MAP13_07_" + token);
            var reservationKind = definition.RegionKind == SpecialRegionKind.Forge
                ? SiteReservationKind.Forge : SiteReservationKind.Boss;
            var ownerKind = definition.RegionKind == SpecialRegionKind.Forge
                ? SpecialRegionPlacementOwnerKind.Forge : SpecialRegionPlacementOwnerKind.Boss;
            var origin = new SectorCoord(5, 5);
            var entryAnchor = new SiteEntryAnchor(
                reservationId, "ENTRY_MAIN", origin, SiteEntrySide.L,
                new[] { 1, 2, 3 }, true, false);
            var returnAnchor = new SiteEntryAnchor(
                reservationId, "RETURN_MAIN", origin, SiteEntrySide.R,
                new[] { 1, 2, 3 }, true, true);
            var reservation = new SiteReservation(
                reservationId, reservationKind, "SPECIAL_" + token, origin,
                new SiteFootprint(1, 1, SiteFootprintTransform.R0, new[]
                {
                    new SiteFootprintCell(0, 0, token, string.Empty, string.Empty,
                        new[] { SiteEntrySide.L, SiteEntrySide.R })
                }), string.Empty, 1,
                reverse ? new[] { returnAnchor, entryAnchor } : new[] { entryAnchor, returnAnchor });

            var entryId = new SpecialRegionSlotId("SR_SLOT_" + token + "_ENTRY");
            var returnId = new SpecialRegionSlotId("SR_SLOT_" + token + "_RETURN");
            var slots = new List<SpecialRegionSlot>
            {
                new SpecialRegionSlot(entryId, SpecialRegionSlotKind.Entry,
                    new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(0, 1), true,
                    default(SpecialPersistenceScope), default(SpecialPersistenceKey)),
                new SpecialRegionSlot(returnId, SpecialRegionSlotKind.Return,
                    new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(47, 1), true,
                    default(SpecialPersistenceScope), default(SpecialPersistenceKey)),
            };
            var persistence = new List<SpecialPersistenceBinding>
            {
                new SpecialPersistenceBinding(SpecialPersistenceKey.ForRegion(definition.RegionId),
                    SpecialPersistenceScope.Region, default(SpecialRegionSlotId), "INITIAL"),
            };
            if (definition.RequiredReward != null)
            {
                var reward = definition.RequiredReward;
                var rewardNode = definition.Nodes.Single(value => value.NodeId == reward.NodeId);
                slots.Add(new SpecialRegionSlot(reward.SlotId, SpecialRegionSlotKind.Reward,
                    new SpecialRegionSectorOffset(0, 0), rewardNode.Coordinate, true,
                    SpecialPersistenceScope.Reward, reward.PersistenceKey));
                persistence.Add(new SpecialPersistenceBinding(reward.PersistenceKey,
                    SpecialPersistenceScope.Reward, reward.SlotId, "INITIAL_AVAILABLE"));
            }
            else
            {
                var encounterSlot = new SpecialRegionSlotId("SR_SLOT_BOSS_ENCOUNTER");
                var encounterKey = SpecialPersistenceKey.ForSlot(
                    definition.RegionId, SpecialPersistenceScope.Encounter, encounterSlot);
                slots.Add(new SpecialRegionSlot(encounterSlot, SpecialRegionSlotKind.Event,
                    new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(37, 15), true,
                    SpecialPersistenceScope.Encounter, encounterKey));
                persistence.Add(new SpecialPersistenceBinding(encounterKey,
                    SpecialPersistenceScope.Encounter, encounterSlot, "GATE_LOCKED"));
            }
            var ports = new[]
            {
                new SpecialRegionPort("SR_PORT_" + token + "_ENTRY", entryId,
                    SpecialRegionSlotKind.Entry, new SpecialRegionSectorOffset(0, 0),
                    new LocalTileCoord(0, 1), SiteEntrySide.L, AccessClass.MandatoryNoTool),
                new SpecialRegionPort("SR_PORT_" + token + "_RETURN", returnId,
                    SpecialRegionSlotKind.Return, new SpecialRegionSectorOffset(0, 0),
                    new LocalTileCoord(47, 1), SiteEntrySide.R, AccessClass.MandatoryNoTool),
            };
            var contract = new SpecialRegionContract(
                definition.RegionId, definition.RegionKind, reservationId,
                new SpecialRegionFootprint(new[] { new SpecialRegionSectorOffset(0, 0) }),
                new[]
                {
                    new SpecialRegionFixedShellCell(new SpecialRegionSectorOffset(0, 0),
                        new LocalTileCoord(42, 30), "SHELL_" + token + "_A"),
                    new SpecialRegionFixedShellCell(new SpecialRegionSectorOffset(0, 0),
                        new LocalTileCoord(43, 30), "SHELL_" + token + "_B"),
                },
                reverse ? slots.AsEnumerable().Reverse() : slots,
                reverse ? ports.Reverse() : ports,
                reverse ? persistence.AsEnumerable().Reverse() : persistence,
                "MAP13_07 placed source fixture");
            var validation = SpecialRegionValidator.Validate(contract, reservation);
            Assert.That(validation.IsValid, Is.True, string.Join("\n", validation.Errors));
            var bridgeResult = SpecialRegionSiteBridgeCompiler.Compile(
                BuildSnapshot(reservation, reverse), validation);
            Assert.That(bridgeResult.Succeeded, Is.True, string.Join("\n", bridgeResult.Errors));

            var entryPortId = "SR_PORT_" + token + "_ENTRY";
            var returnPortId = "SR_PORT_" + token + "_RETURN";
            var entryApron = new SpecialRegionEntryApron(
                entryPortId, origin, new LocalTileCoord(0, 0), 4, 4,
                reverse ? Rectangle(5, 5, 0, 0, 4, 4).Reverse() : Rectangle(5, 5, 0, 0, 4, 4));
            var returnApron = new SpecialRegionEntryApron(
                returnPortId, origin, new LocalTileCoord(4, 0), 44, 4,
                reverse ? Rectangle(5, 5, 4, 0, 44, 4).Reverse() : Rectangle(5, 5, 4, 0, 44, 4));
            var before = new SpecialRegionQuietBufferPlacement(
                "placement.before." + token, SpecialRegionQuietChunkRole.Before, quietCandidate,
                new[]
                {
                    new SpecialRegionQuietChunkPlacement(
                        new ClusterChunkCoord(0, 0), new SectorCoord(4, 5), new ClusterChunkCoord(2, 0)),
                    new SpecialRegionQuietChunkPlacement(
                        new ClusterChunkCoord(1, 0), new SectorCoord(4, 5), new ClusterChunkCoord(3, 0)),
                });
            var after = new SpecialRegionQuietBufferPlacement(
                "placement.after." + token, SpecialRegionQuietChunkRole.After, quietCandidate,
                new[]
                {
                    new SpecialRegionQuietChunkPlacement(
                        new ClusterChunkCoord(0, 0), new SectorCoord(6, 5), new ClusterChunkCoord(0, 0)),
                    new SpecialRegionQuietChunkPlacement(
                        new ClusterChunkCoord(1, 0), new SectorCoord(6, 5), new ClusterChunkCoord(1, 0)),
                });
            var entryResult = SpecialRegionEntryBufferCompiler.Compile(
                new SpecialRegionEntryBufferCompileRequest(
                    bridgeResult.Bridge, bridgeResult.CanonicalDigest,
                    entryPortId, entryAnchor, entryApron,
                    returnPortId, returnAnchor, returnApron,
                    before, after));
            Assert.That(entryResult.Succeeded, Is.True, string.Join("\n", entryResult.Errors));

            var fixedCells = bridgeResult.Bridge.FixedShellBindings.Select(value =>
                new SpecialRegionTileCoordinate(value.Placed.WorldSector, value.Placed.LocalTile));
            var accessCells = entryResult.Plan.Aprons.SelectMany(value => value.Cells).Distinct();
            var collisionResult = SpecialRegionPlacementCollisionCompiler.Compile(
                new SpecialRegionPlacementCollisionCompileRequest(new[]
                {
                    new SpecialRegionOccupancyClaim("SR_FIXED_COLLISION_" + token,
                        ownerKind, fixedCells, true),
                    new SpecialRegionOccupancyClaim("SR_FIXED_ACCESS_" + token,
                        ownerKind, accessCells, true),
                }));
            Assert.That(collisionResult.Succeeded, Is.True, string.Join("\n", collisionResult.Errors));
            var layerResult = SpecialRegionFixedSlotLayerCompiler.Compile(
                new SpecialRegionFixedSlotLayerCompileRequest(
                    validation, validation.CanonicalDigest,
                    bridgeResult.Bridge, bridgeResult.CanonicalDigest,
                    entryResult.Plan, entryResult.CanonicalDigest,
                    collisionResult.Plan, collisionResult.CanonicalDigest));
            Assert.That(layerResult.Succeeded, Is.True, string.Join("\n", layerResult.Errors));

            SpecialRegionRequiredResourceSafetyProof safetyProof = null;
            if (definition.RequiredReward != null)
            {
                var evidence = Evidence(layerResult.Plan);
                var safetyResult = SpecialRegionPersistenceSafetyCompiler.Compile(
                    new SpecialRegionPersistenceSafetyCompileRequest(
                        layerResult.Plan, layerResult.CanonicalDigest,
                        reverse ? evidence.Reverse() : evidence));
                Assert.That(safetyResult.Succeeded, Is.True, string.Join("\n", safetyResult.Errors));
                safetyProof = safetyResult.Proofs.Single();
            }
            return new PlacedFixture(
                bridgeResult.Bridge, entryResult.Plan, collisionResult.Plan,
                layerResult.Plan, safetyProof);
        }

        private static IEnumerable<SpecialRegionPersistenceCheckpointEvidence> Evidence(
            SpecialRegionFixedSlotLayerPlan plan)
        {
            var reward = plan.ReplaceableSlots.Single(value =>
                value.Kind == SpecialRegionSlotKind.Reward && value.Required);
            var states = new[]
            {
                Tuple.Create(SpecialRegionPersistenceCheckpoint.Initial, SpecialRegionRequiredResourceState.Available),
                Tuple.Create(SpecialRegionPersistenceCheckpoint.Active, SpecialRegionRequiredResourceState.TemporarilyUnavailable),
                Tuple.Create(SpecialRegionPersistenceCheckpoint.Interrupted, SpecialRegionRequiredResourceState.Available),
                Tuple.Create(SpecialRegionPersistenceCheckpoint.Failed, SpecialRegionRequiredResourceState.Available),
                Tuple.Create(SpecialRegionPersistenceCheckpoint.Regenerated, SpecialRegionRequiredResourceState.Available),
                Tuple.Create(SpecialRegionPersistenceCheckpoint.Claimed, SpecialRegionRequiredResourceState.Claimed),
                Tuple.Create(SpecialRegionPersistenceCheckpoint.Revisited, SpecialRegionRequiredResourceState.Claimed),
            };
            return states.Select(value => new SpecialRegionPersistenceCheckpointEvidence(
                plan.RegionId, reward.SlotId, reward.PersistenceKey, reward.PersistenceScope,
                value.Item1, value.Item2, reward.IdentityDigest));
        }

        private static SiteReservationSnapshot BuildSnapshot(SiteReservation reservation, bool reverse)
        {
            var start = new SiteReservation(
                new SiteReservationId("RES_START"), SiteReservationKind.Start, "START_SITE",
                new SectorCoord(1, 1),
                new SiteFootprint(1, 1, SiteFootprintTransform.R0, new[]
                {
                    new SiteFootprintCell(0, 0, "START", string.Empty, string.Empty,
                        Array.Empty<SiteEntrySide>())
                }), string.Empty, 0, Array.Empty<SiteEntryAnchor>());
            var reservations = new List<SiteReservation> { start, reservation };
            var occupied = new Dictionary<SectorCoord, Tuple<SiteReservation, SiteFootprintCell>>();
            foreach (var site in reservations)
            foreach (var sector in site.OccupiedSectors)
            {
                site.TryGetFootprintCell(sector, out var cell);
                occupied.Add(sector, Tuple.Create(site, cell));
            }
            var rows = new List<SectorReservation>();
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var coordinate = WorldGridIndex.ToCoordinate(index);
                if (occupied.TryGetValue(coordinate, out var binding))
                    rows.Add(SectorReservation.CreateReserved(
                        index, coordinate, binding.Item1.ReservationId, binding.Item1.Kind,
                        binding.Item2.LocalX, binding.Item2.LocalY, binding.Item2.LocalRole));
                else rows.Add(SectorReservation.CreateUnreserved(index, coordinate));
            }
            if (reverse) { reservations.Reverse(); rows.Reverse(); }
            return new SiteReservationSnapshot(1307UL, reservations, rows, Array.Empty<CoreBiomeSeed>());
        }

        private static TerrainClusterQuietBufferCandidate BuildQuietCandidate(bool reverse)
        {
            var contract = CreateClusterContract(reverse);
            var validation = TerrainClusterContractValidator.Validate(contract);
            Assert.That(validation.IsValid, Is.True, string.Join("\n", validation.Errors));
            var canvasResult = TerrainClusterFootprintCompiler.Compile(
                new TerrainClusterFootprintCompileRequest(contract, ClusterFootprintTransform.R0));
            Assert.That(canvasResult.IsSuccess, Is.True, string.Join("\n", canvasResult.Errors));
            var canvas = canvasResult.LocalCanvas;
            var roleResult = TerrainClusterRoleSocketCompiler.Compile(
                new TerrainClusterRoleSocketCompileRequest(
                    contract, validation.CanonicalDigest, canvas, canvas.CanonicalDigest,
                    SocketEvidence()));
            Assert.That(roleResult.IsSuccess, Is.True, string.Join("\n", roleResult.Errors));
            var traversalResult = TerrainClusterTraversalCompiler.Compile(
                new TerrainClusterTraversalCompileRequest(
                    contract, validation.CanonicalDigest, canvas, canvas.CanonicalDigest,
                    roleResult.Contract, roleResult.CanonicalDigest));
            Assert.That(traversalResult.IsSuccess, Is.True, string.Join("\n", traversalResult.Errors));
            var witnessResult = TerrainClusterRouteWitnessCompiler.Compile(
                new TerrainClusterRouteWitnessCompileRequest(
                    canvas, canvas.CanonicalDigest, roleResult.Contract, roleResult.CanonicalDigest,
                    traversalResult.Compilation, traversalResult.CanonicalDigest,
                    CreateWitnessIntent(traversalResult.Compilation, reverse)));
            Assert.That(witnessResult.IsSuccess, Is.True, string.Join("\n", witnessResult.Errors));
            var catalog = BuildNoChangeCatalog();
            Assert.That(catalog.TryGetDefinition(
                new MicroPatternId("MP_MAP13_07_NO_CHANGE"), out var definition), Is.True);
            var render = TerrainClusterPatternRenderer.Render(new TerrainClusterPatternRenderRequest(
                canvas, canvas.CanonicalDigest,
                traversalResult.Compilation, traversalResult.CanonicalDigest,
                witnessResult.Report, witnessResult.CanonicalDigest,
                catalog, catalog.StableDigest, Array.Empty<TerrainClusterPatternZoneCell>(),
                new[]
                {
                    new TerrainClusterPatternPlacementIntent(
                        "TCP_MAP13_07_NO_CHANGE", definition.Id, MicroPatternTransform.R0,
                        new LocalTileCoord(0, 4), definition.ComputeStableDigest())
                }));
            Assert.That(render.Success, Is.True, string.Join("\n", render.Errors));
            var profile = new TerrainClusterQuietBufferProfile(
                "QBUF_MAP13_07", MoonpalaceBiomeId.MoonCrater,
                new[] { TerrainClusterQuietBufferUse.BeforeLandmark, TerrainClusterQuietBufferUse.AfterLandmark },
                new[] { PacingRole.Quiet }, new[] { AccessClass.MandatoryNoTool },
                canvas, canvas.CanonicalDigest,
                roleResult.Contract, roleResult.CanonicalDigest,
                traversalResult.Compilation, traversalResult.CanonicalDigest,
                witnessResult.Report, witnessResult.CanonicalDigest,
                render.Report, render.CanonicalDigest);
            var pool = TerrainClusterQuietBufferPoolCompiler.Compile(
                new TerrainClusterQuietBufferPoolCompileRequest(new[] { profile }));
            Assert.That(pool.IsSuccess, Is.True, string.Join("\n", pool.Errors));
            return pool.Candidates.Single();
        }

        private static TerrainClusterContract CreateClusterContract(bool reverse)
        {
            var roles = new[]
            {
                new ClusterRoleAnchor("ANCHOR_ENTRY", ClusterRoleKind.Entry, new LocalTileCoord(0, 1), "NODE_ENTRY"),
                new ClusterRoleAnchor("ANCHOR_BUILD_UP", ClusterRoleKind.BuildUp, new LocalTileCoord(4, 1), "NODE_BUILD_UP"),
                new ClusterRoleAnchor("ANCHOR_CORE", ClusterRoleKind.Core, new LocalTileCoord(10, 1), "NODE_CORE"),
                new ClusterRoleAnchor("ANCHOR_RECOVERY", ClusterRoleKind.Recovery, new LocalTileCoord(17, 1), "NODE_RECOVERY"),
                new ClusterRoleAnchor("ANCHOR_EXIT", ClusterRoleKind.Exit, new LocalTileCoord(23, 1), "NODE_EXIT"),
            };
            var commonNodes = roles.Select(value => new TraversalNode(
                value.TraversalNodeId, value.Tile, true, value.AnchorId)).Concat(new[]
            {
                new TraversalNode("NODE_STEP_A", new LocalTileCoord(7, 1), true, string.Empty),
                new TraversalNode("NODE_STEP_B", new LocalTileCoord(6, 1), false, string.Empty),
            }).ToArray();
            var alternateNodes = commonNodes.Concat(new[]
            {
                new TraversalNode("NODE_HIGH", new LocalTileCoord(7, 3), false, string.Empty),
                new TraversalNode("NODE_HIGH_END", new LocalTileCoord(9, 3), false, string.Empty),
            }).ToArray();
            var common = commonNodes.ToDictionary(value => value.NodeId, StringComparer.Ordinal);
            var alternate = alternateNodes.ToDictionary(value => value.NodeId, StringComparer.Ordinal);
            var baselineEdges = new[]
            {
                CreateEdge("EDGE_01_ENTRY", common["NODE_ENTRY"], common["NODE_BUILD_UP"], true),
                CreateEdge("EDGE_BASE_A1", common["NODE_BUILD_UP"], common["NODE_STEP_A"], true),
                CreateEdge("EDGE_BASE_A2", common["NODE_STEP_A"], common["NODE_CORE"], true),
                CreateEdge("EDGE_BASE_B1", common["NODE_BUILD_UP"], common["NODE_STEP_B"], false),
                CreateEdge("EDGE_BASE_B2", common["NODE_STEP_B"], common["NODE_CORE"], false),
                CreateEdge("EDGE_04_CORE", common["NODE_CORE"], common["NODE_RECOVERY"], true),
                CreateEdge("EDGE_05_RECOVERY", common["NODE_RECOVERY"], common["NODE_EXIT"], true),
            };
            var alternateEdges = baselineEdges.Select(value => CopyEdge(value, alternate)).Concat(new[]
            {
                CreateEdge("EDGE_HIGH_01", alternate["NODE_BUILD_UP"], alternate["NODE_HIGH"], false),
                CreateEdge("EDGE_HIGH_02", alternate["NODE_HIGH"], alternate["NODE_HIGH_END"], false),
                CreateEdge("EDGE_HIGH_03", alternate["NODE_HIGH_END"], alternate["NODE_CORE"], false),
                CreateEdge("EDGE_RECOVER", alternate["NODE_HIGH"], alternate["NODE_RECOVERY"], false),
            }).ToArray();
            var variants = new[]
            {
                new SpineVariant(new SpineVariantId("SPINE_BASELINE"), true, TraversalGraphKind.Traversal,
                    reverse ? commonNodes.Reverse() : commonNodes,
                    reverse ? baselineEdges.Reverse() : baselineEdges),
                new SpineVariant(new SpineVariantId("SPINE_ALTERNATE"), false, TraversalGraphKind.Traversal,
                    reverse ? alternateNodes.Reverse() : alternateNodes,
                    reverse ? alternateEdges.Reverse() : alternateEdges),
            };
            var ports = new[]
            {
                new ClusterPort("PORT_ENTRY", ClusterPortKind.Entry, true, "ANCHOR_ENTRY",
                    new LocalTileCoord(0, 1), ClusterPortSide.L, new[] { 0, 1, 2, 3, 4 }),
                new ClusterPort("PORT_EXIT", ClusterPortKind.Exit, true, "ANCHOR_EXIT",
                    new LocalTileCoord(23, 1), ClusterPortSide.R, new[] { 1, 2, 3, 4 }),
            };
            return new TerrainClusterContract(
                new TerrainClusterId("TC_MAP13_07"),
                new ClusterFootprint(new[] { new ClusterChunkCoord(0, 0), new ClusterChunkCoord(1, 0) }),
                reverse ? roles.Reverse() : roles, reverse ? ports.Reverse() : ports,
                new TerrainClusterTraversalContract(reverse ? variants.Reverse() : variants), "MAP13_07");
        }

        private static TraversalEdge CreateEdge(
            string id, TraversalNode from, TraversalNode to, bool mandatory)
        {
            var envelope = new TraversalEnvelope(
                new[] { from.Tile, to.Tile }, new[] { new LocalTileCoord(from.Tile.X, 0) },
                new[] { new LocalTileCoord(from.Tile.X, 5) },
                Array.Empty<LocalTileCoord>(), Array.Empty<LocalTileCoord>(),
                new[] { to.Tile }, new[] { to.Tile });
            return new TraversalEdge(
                id, from.NodeId, to.NodeId, TraversalMovementKind.Walk,
                from.Tile, to.Tile, 1, 2, to.Tile, to.Tile, mandatory, envelope);
        }

        private static TraversalEdge CopyEdge(
            TraversalEdge edge, IDictionary<string, TraversalNode> nodes)
            => CreateEdge(edge.EdgeId, nodes[edge.FromNodeId], nodes[edge.ToNodeId], edge.IsMandatory);

        private static TerrainClusterRouteWitnessIntent CreateWitnessIntent(
            TerrainClusterTraversalCompilation traversal, bool reverse)
        {
            var high = new TerrainClusterHighRouteDefinition(
                "HIGH_ROUTE_ONE", new SpineVariantId("SPINE_ALTERNATE"), "NODE_BUILD_UP",
                new[] { "EDGE_HIGH_01", "EDGE_HIGH_02", "EDGE_HIGH_03" },
                "NODE_CORE", "NODE_HIGH",
                new[] { "BENEFIT_HEIGHT_ADVANTAGE", "BENEFIT_REWARD_ACCESS" },
                new[] { "NODE_HIGH" });
            var durations = traversal.Edges.Select(value => new TraversalEdgeDurationEvidence(
                value.VariantId, value.EdgeId, value.EdgeId == "EDGE_RECOVER" ? 2000 : 3000,
                "RULESET_ROUTE_V1"));
            return new TerrainClusterRouteWitnessIntent(
                new SpineVariantId("SPINE_BASELINE"), new[] { high },
                reverse ? durations.Reverse() : durations);
        }

        private static ClusterSectorSocketEvidence[] SocketEvidence()
            => new[]
            {
                new ClusterSectorSocketEvidence(
                    "SR_EXIT", "SOCKET_EXIT", ClusterPortSide.R, 3, true, ClusterPortKind.Exit),
                new ClusterSectorSocketEvidence(
                    "SR_ENTRY", "SOCKET_ENTRY", ClusterPortSide.L, 2, true, ClusterPortKind.Entry),
            };

        private static MicroPatternAuthoringCatalog BuildNoChangeCatalog()
        {
            var catalog = new[]
            {
                new MicroPatternCatalogRowV2(
                    "MP_MAP13_07_NO_CHANGE", "1", "MoonCrater", "R0", "FORCE_NO_CHANGE",
                    "catalog.csv", 2),
            };
            var cells = Enumerable.Range(0, 4).SelectMany(y => Enumerable.Range(0, 4)
                .Select((x, index) => new MicroPatternCellRowV2(
                    "MP_MAP13_07_NO_CHANGE", x.ToString(CultureInfo.InvariantCulture),
                    y.ToString(CultureInfo.InvariantCulture), "NO_CHANGE", "GEOMETRY", string.Empty,
                    "cells.csv", y * 4 + index + 2))).ToArray();
            var result = new MicroPatternCellSchemaBuilder().Build(catalog, cells);
            Assert.That(result.Success, Is.True, string.Join("\n", result.Errors));
            return result.Catalog;
        }

        private static IEnumerable<SpecialRegionTileCoordinate> Rectangle(
            int sectorX, int sectorY, int minimumX, int minimumY, int width, int height)
        {
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                yield return new SpecialRegionTileCoordinate(
                    new SectorCoord(sectorX, sectorY),
                    new LocalTileCoord(minimumX + x, minimumY + y));
        }

        private static bool IsConnected(IEnumerable<SpecialLandmarkDesignChunk> source)
        {
            var values = new HashSet<SpecialLandmarkDesignChunk>(source);
            var visited = new HashSet<SpecialLandmarkDesignChunk>();
            var pending = new Queue<SpecialLandmarkDesignChunk>();
            pending.Enqueue(values.First());
            visited.Add(values.First());
            while (pending.Count != 0)
            {
                var current = pending.Dequeue();
                foreach (var next in new[]
                         {
                             new SpecialLandmarkDesignChunk(current.X - 1, current.Y),
                             new SpecialLandmarkDesignChunk(current.X + 1, current.Y),
                             new SpecialLandmarkDesignChunk(current.X, current.Y - 1),
                             new SpecialLandmarkDesignChunk(current.X, current.Y + 1),
                         })
                    if (values.Contains(next) && visited.Add(next)) pending.Enqueue(next);
            }
            return visited.Count == values.Count;
        }

        private static SpecialLandmarkRegionDefinition Rebuild(
            SpecialLandmarkRegionDefinition source,
            IEnumerable<SpecialLandmarkDesignChunk> chunks = null,
            IEnumerable<SpecialLandmarkShellNode> nodes = null,
            IEnumerable<SpecialLandmarkShellEdge> edges = null,
            IEnumerable<SpecialLandmarkRouteDefinition> routes = null,
            IEnumerable<SpecialLandmarkStateDefinition> states = null,
            IEnumerable<SpecialLandmarkStateTransitionDefinition> transitions = null,
            IEnumerable<SpecialLandmarkResetDefinition> resets = null,
            IEnumerable<SpecialLandmarkMarkerDefinition> markers = null,
            IEnumerable<SpecialLandmarkForgeLedgerDefinition> ledgers = null,
            SpecialLandmarkRewardDefinition reward = null,
            IEnumerable<SpecialLandmarkMerchantVariant> variants = null,
            SpecialRegionId? regionId = null,
            SpecialLandmarkBindingKind? binding = null,
            bool? mandatoryDependency = null,
            bool? shellMutation = null)
            => new SpecialLandmarkRegionDefinition(
                regionId ?? source.RegionId, source.Landmark, source.RegionKind, source.Theme,
                binding ?? source.Binding, source.ReservedWidth, source.ReservedHeight,
                source.DesignOrigin, source.DesignWidth, source.DesignHeight,
                source.DesignChunkWidth, source.DesignChunkHeight,
                chunks ?? source.ActiveDesignChunks, nodes ?? source.Nodes, edges ?? source.Edges,
                routes ?? source.Routes, states ?? source.States, transitions ?? source.Transitions,
                resets ?? source.Resets, markers ?? source.Markers, ledgers ?? source.ForgeLedgers,
                reward ?? source.RequiredReward, variants ?? source.MerchantVariants,
                source.IntroducesNewMovementRule,
                mandatoryDependency ?? source.MandatoryProgressionDependency,
                shellMutation ?? source.StateMutatesShell, source.DisplayText);

        private static SpecialLandmarkRegionDefinition Definition(SpecialLandmarkKind kind)
            => SpecialLandmarkRegionStarterCatalog.GetDefinition(kind);

        private static void AssertCounts(
            SpecialLandmarkKind kind,
            int chunks,
            int nodes,
            int edges,
            int routes,
            int states,
            int transitions,
            int resets,
            int markers)
        {
            var definition = Definition(kind);
            Assert.That(definition.ActiveDesignChunks, Has.Count.EqualTo(chunks), kind + " chunks");
            Assert.That(definition.Nodes, Has.Count.EqualTo(nodes), kind + " nodes");
            Assert.That(definition.Edges, Has.Count.EqualTo(edges), kind + " edges");
            Assert.That(definition.Routes, Has.Count.EqualTo(routes), kind + " routes");
            Assert.That(definition.States, Has.Count.EqualTo(states), kind + " states");
            Assert.That(definition.Transitions, Has.Count.EqualTo(transitions), kind + " transitions");
            Assert.That(definition.Resets, Has.Count.EqualTo(resets), kind + " resets");
            Assert.That(definition.Markers, Has.Count.EqualTo(markers), kind + " markers");
        }

        private static void AssertSuccess(SpecialLandmarkResult result)
        {
            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Plan, Is.Not.Null);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
        }

        private static void AssertFailure(
            SpecialLandmarkResult result,
            SpecialLandmarkErrorCode code)
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(code),
                string.Join("\n", result.Errors));
            Assert.That(result.Errors, Is.Ordered);
            Assert.That(result.Errors.Distinct().Count(), Is.EqualTo(result.Errors.Count));
        }

        private sealed class PlacedFixture
        {
            public PlacedFixture(
                SpecialRegionSiteBridge bridge,
                SpecialRegionEntryBufferPlan entryPlan,
                SpecialRegionPlacementCollisionPlan collisionPlan,
                SpecialRegionFixedSlotLayerPlan layerPlan,
                SpecialRegionRequiredResourceSafetyProof safetyProof)
            {
                Bridge = bridge;
                EntryPlan = entryPlan;
                CollisionPlan = collisionPlan;
                LayerPlan = layerPlan;
                SafetyProof = safetyProof;
            }

            public SpecialRegionSiteBridge Bridge { get; }
            public SpecialRegionEntryBufferPlan EntryPlan { get; }
            public SpecialRegionPlacementCollisionPlan CollisionPlan { get; }
            public SpecialRegionFixedSlotLayerPlan LayerPlan { get; }
            public SpecialRegionRequiredResourceSafetyProof SafetyProof { get; }
        }
    }
}
