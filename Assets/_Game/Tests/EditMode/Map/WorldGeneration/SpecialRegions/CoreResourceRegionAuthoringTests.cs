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
    [Category("MAP13_06")]
    public sealed class CoreResourceRegionAuthoringTests
    {
        private IReadOnlyDictionary<CoreResourceKind, RegionFixture> fixtures;

        [OneTimeSetUp]
        public void SetUp()
        {
            var quietCandidate = BuildQuietCandidate(false);
            fixtures = CoreResourceRegionStarterCatalog.Entries.ToDictionary(
                value => value.Resource,
                value => BuildFixture(value, false, quietCandidate));
        }

        [Test]
        public void CatalogPublishesExactThreeCanonicalStarterIdentities()
        {
            var entries = CoreResourceRegionStarterCatalog.Entries;
            Assert.That(entries, Has.Count.EqualTo(3));
            Assert.That(entries.Select(value => value.RegionId.Value), Is.EqualTo(new[]
            {
                "SR_CASSIA_SAP_SITE_5",
                "SR_MOON_CORE_SITE_5",
                "SR_STAR_NURUK_SITE_5",
            }));
            Assert.That(entries.Select(value => value.Resource), Is.EquivalentTo(new[]
            {
                CoreResourceKind.MoonCore,
                CoreResourceKind.CassiaSap,
                CoreResourceKind.StarNuruk,
            }));
            Assert.That(entries.Select(value => value.Biome), Is.EquivalentTo(new[]
            {
                MoonpalaceBiomeId.MoonCrater,
                MoonpalaceBiomeId.CassiaRoot,
                MoonpalaceBiomeId.MoonDough,
            }));
            Assert.That(entries.All(value => value.RegionKind == SpecialRegionKind.CoreResource &&
                value.ReservedWidth == 1 && value.ReservedHeight == 1 &&
                value.DesignWidth == 36 && value.DesignHeight == 16 &&
                value.ActiveDesignChunks.Count == 5), Is.True);
            Assert.That(CoreResourceRegionStarterCatalog.CanonicalDigest,
                Does.Match("^[0-9a-f]{64}$"));
            foreach (var entry in entries)
            {
                Assert.That(CoreResourceRegionStarterCatalog.TryGetDefinition(entry.RegionId, out var found), Is.True);
                Assert.That(found, Is.SameAs(entry));
                Assert.That(CoreResourceRegionStarterCatalog.GetDefinition(entry.Resource), Is.SameAs(entry));
            }
        }

        [Test]
        public void DesignCanvasHasExplicitFiveOfSixConnectedChunks()
        {
            foreach (var definition in CoreResourceRegionStarterCatalog.Entries)
            {
                Assert.That(definition.DesignOrigin, Is.EqualTo(new LocalTileCoord(6, 8)));
                Assert.That(definition.DesignGridWidth, Is.EqualTo(3));
                Assert.That(definition.DesignGridHeight, Is.EqualTo(2));
                Assert.That(definition.DesignChunkWidth, Is.EqualTo(12));
                Assert.That(definition.DesignChunkHeight, Is.EqualTo(8));
                Assert.That(definition.ActiveDesignChunks, Has.Count.EqualTo(5));
                Assert.That(definition.ActiveDesignChunks.Distinct().Count(), Is.EqualTo(5));
                Assert.That(definition.ActiveDesignChunks.All(value =>
                    value.X >= 0 && value.X < 3 && value.Y >= 0 && value.Y < 2), Is.True);
                Assert.That(IsConnected(definition.ActiveDesignChunks), Is.True);
            }
        }

        [Test]
        public void MoonCoreAuthorsImpactLowMasteryAndDeviceResetSolution()
        {
            var definition = CoreResourceRegionStarterCatalog.GetDefinition(CoreResourceKind.MoonCore);
            Assert.That(definition.Mechanism, Is.EqualTo(CoreResourceMechanismKind.ImpactChain));
            Assert.That(definition.Nodes, Has.Count.EqualTo(14));
            Assert.That(definition.Edges, Has.Count.EqualTo(16));
            AssertRouteCounts(definition, 1, 1, 1);
            Assert.That(definition.Nodes.Select(value => value.MarkerKind), Does.Contain(CoreResourceMarkerKind.MoonBoulder));
            Assert.That(definition.Nodes.Select(value => value.MarkerKind), Does.Contain(CoreResourceMarkerKind.Mortar));
            Assert.That(definition.Nodes.Select(value => value.MarkerKind), Does.Contain(CoreResourceMarkerKind.ChainedImpact));
            Assert.That(definition.Nodes.Select(value => value.MarkerKind), Does.Contain(CoreResourceMarkerKind.Vein));
            Assert.That(definition.Nodes.Select(value => value.MarkerKind), Does.Contain(CoreResourceMarkerKind.EnemyCue));
            Assert.That(definition.Nodes.Select(value => value.MarkerKind), Does.Contain(CoreResourceMarkerKind.SecretPocket));
            Assert.That(definition.Nodes.Select(value => value.MarkerKind), Does.Contain(CoreResourceMarkerKind.DeviceReset));
            Assert.That(definition.OptionalBenefits.Select(value => value.Kind), Is.EquivalentTo(new[]
            {
                CoreResourceOptionalBenefitKind.MoonIron,
                CoreResourceOptionalBenefitKind.AuxiliaryBattery,
            }));
        }

        [Test]
        public void CassiaSapAuthorsThreeOrderedRootsMasteryFlowAndManualReset()
        {
            var definition = CoreResourceRegionStarterCatalog.GetDefinition(CoreResourceKind.CassiaSap);
            Assert.That(definition.Mechanism, Is.EqualTo(CoreResourceMechanismKind.WaterChannel));
            Assert.That(definition.Nodes, Has.Count.EqualTo(15));
            Assert.That(definition.Edges, Has.Count.EqualTo(17));
            AssertRouteCounts(definition, 1, 1, 1);
            Assert.That(definition.Nodes.Where(value => value.MarkerKind == CoreResourceMarkerKind.RootChannel)
                .Select(value => value.AuthoredOrder), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(definition.Nodes.Select(value => value.MarkerKind), Does.Contain(CoreResourceMarkerKind.SapPipe));
            Assert.That(definition.Nodes.Select(value => value.MarkerKind), Does.Contain(CoreResourceMarkerKind.MasteryWaterFlow));
            Assert.That(definition.Nodes.Select(value => value.MarkerKind), Does.Contain(CoreResourceMarkerKind.BonusRoot));
            Assert.That(definition.Nodes.Select(value => value.MarkerKind), Does.Contain(CoreResourceMarkerKind.Shortcut));
            Assert.That(definition.Nodes.Select(value => value.MarkerKind), Does.Contain(CoreResourceMarkerKind.ManualReset));
            Assert.That(definition.OptionalBenefits.Select(value => value.Kind), Is.EquivalentTo(new[]
            {
                CoreResourceOptionalBenefitKind.RecoveryPickup,
                CoreResourceOptionalBenefitKind.HiddenSeed,
            }));
        }

        [Test]
        public void StarNurukAuthorsValvesSafePlatformsGasCueBounceAndRecoveryRoom()
        {
            var definition = CoreResourceRegionStarterCatalog.GetDefinition(CoreResourceKind.StarNuruk);
            Assert.That(definition.Mechanism, Is.EqualTo(CoreResourceMechanismKind.FermentationPressure));
            Assert.That(definition.Nodes, Has.Count.EqualTo(15));
            Assert.That(definition.Edges, Has.Count.EqualTo(18));
            AssertRouteCounts(definition, 1, 1, 1);
            Assert.That(definition.Nodes.Count(value => value.MarkerKind == CoreResourceMarkerKind.Valve),
                Is.EqualTo(2));
            Assert.That(definition.Nodes.Select(value => value.MarkerKind), Does.Contain(CoreResourceMarkerKind.SafePlatform));
            Assert.That(definition.Nodes.Single(value => value.MarkerKind == CoreResourceMarkerKind.GasWarning)
                .RequiredMarker, Is.True);
            Assert.That(definition.Nodes.Select(value => value.MarkerKind), Does.Contain(CoreResourceMarkerKind.PressureRelease));
            Assert.That(definition.Nodes.Count(value => value.MarkerKind == CoreResourceMarkerKind.BounceChain),
                Is.EqualTo(2));
            Assert.That(definition.Nodes.Single(value => value.MarkerKind == CoreResourceMarkerKind.RecoveryRoom)
                .RequiredMarker, Is.True);
            Assert.That(definition.OptionalBenefits.Select(value => value.Kind), Is.EquivalentTo(new[]
            {
                CoreResourceOptionalBenefitKind.Fuel,
                CoreResourceOptionalBenefitKind.RareFermentationItem,
            }));
        }

        [Test]
        public void AllThreeCompileThroughPublicMap13SourcesWithMandatoryNoToolLowRoutes()
        {
            foreach (var fixture in fixtures.Values)
            {
                var result = Compile(fixture);
                AssertSuccess(result);
                Assert.That(result.Plan.ActiveDesignChunks, Has.Count.EqualTo(5));
                Assert.That(result.Plan.Edges.Where(value => value.RouteKind == CoreResourceRouteKind.Low)
                    .All(value => value.Required && value.AccessClass == AccessClass.MandatoryNoTool &&
                                  value.Dependency == CoreResourceDependencyKind.None), Is.True);
                Assert.That(result.Plan.MandatoryToolDependencyCount, Is.Zero);
                Assert.That(result.Plan.BridgeDigest, Is.EqualTo(fixture.Bridge.CanonicalDigest));
                Assert.That(result.Plan.EntryBufferDigest, Is.EqualTo(fixture.EntryPlan.CanonicalDigest));
                Assert.That(result.Plan.CollisionDigest, Is.EqualTo(fixture.CollisionPlan.CanonicalDigest));
                Assert.That(result.Plan.FixedSlotLayerDigest, Is.EqualTo(fixture.LayerPlan.CanonicalDigest));
                Assert.That(result.Plan.SafetyProofDigest, Is.EqualTo(fixture.SafetyProof.CanonicalDigest));
            }
        }

        [Test]
        public void EntryTriggerRewardReturnAndReverseStaticWitnessesArePublished()
        {
            foreach (var fixture in fixtures.Values)
            {
                var plan = Compile(fixture).Plan;
                Assert.That(plan.HasEntryTriggerRewardReturnWitness, Is.True);
                Assert.That(plan.HasReverseStaticGraphWitness, Is.True);
                Assert.That(plan.LowWitness.NodeIds.First(), Is.EqualTo(
                    fixture.Definition.Nodes.Single(value => value.Role == CoreResourceNodeRole.Entry).NodeId));
                Assert.That(plan.LowWitness.NodeIds, Does.Contain(plan.RequiredReward.NodeId));
                Assert.That(plan.LowWitness.NodeIds.Last(), Is.EqualTo(
                    fixture.Definition.Nodes.Single(value => value.Role == CoreResourceNodeRole.Return).NodeId));
                Assert.That(plan.HighWitness.NodeIds.First(), Is.EqualTo(plan.LowWitness.NodeIds.First()));
                Assert.That(plan.HighWitness.NodeIds, Does.Contain(plan.RequiredReward.NodeId));
                Assert.That(plan.HighWitness.NodeIds.Last(), Is.EqualTo(plan.LowWitness.NodeIds.Last()));
            }
        }

        [Test]
        public void EveryFailureReturnsToExistingLowRecoveryJoinWithoutResourceLoss()
        {
            foreach (var fixture in fixtures.Values)
            {
                var result = Compile(fixture);
                AssertSuccess(result);
                var plan = result.Plan;
                Assert.That(plan.Nodes.Count(value => value.Role == CoreResourceNodeRole.Failure), Is.EqualTo(1));
                Assert.That(plan.Recoveries, Has.Count.EqualTo(1));
                Assert.That(plan.RecoveryWitnesses, Has.Count.EqualTo(1));
                Assert.That(plan.HasFailureRecoveryWitness, Is.True);
                Assert.That(plan.RecoveryWitnesses.Single().NodeIds.First(),
                    Is.EqualTo(plan.Recoveries.Single().FailureNodeId));
                Assert.That(plan.RecoveryWitnesses.Single().NodeIds.Last(),
                    Is.EqualTo(plan.Recoveries.Single().RecoveryJoinNodeId));
                Assert.That(plan.LowWitness.NodeIds, Does.Contain(plan.Recoveries.Single().RecoveryJoinNodeId));
                Assert.That(plan.PermanentLossCount, Is.Zero);
                Assert.That(plan.DuplicateRewardRiskCount, Is.Zero);
            }
        }

        [Test]
        public void AuthoritativeRewardKeysSlotsAndSevenCheckpointsMatchExactly()
        {
            foreach (var fixture in fixtures.Values)
            {
                var reward = fixture.Definition.RequiredReward;
                var expected = SpecialPersistenceKey.ForSlot(
                    fixture.Definition.RegionId, SpecialPersistenceScope.Reward, reward.SlotId);
                Assert.That(reward.PersistenceKey, Is.EqualTo(expected));
                Assert.That(fixture.LayerPlan.ReplaceableSlots.Single(value => value.Required).PersistenceKey,
                    Is.EqualTo(expected));
                Assert.That(fixture.SafetyProof.PersistenceKey, Is.EqualTo(expected));
                Assert.That(fixture.SafetyProof.Evidence, Has.Count.EqualTo(7));
                Assert.That(fixture.SafetyProof.InitialAvailable, Is.True);
                Assert.That(fixture.SafetyProof.RecoveryBranchesAvailable, Is.True);
                Assert.That(fixture.SafetyProof.ClaimStable, Is.True);
                Assert.That(fixture.SafetyProof.PermanentlyUnavailableCount, Is.Zero);
                Assert.That(fixture.SafetyProof.DuplicateRewardRiskCount, Is.Zero);
            }
        }

        [Test]
        public void InvalidIdentityFootprintCanvasChunkAndDigestFailAtomically()
        {
            var fixture = fixtures[CoreResourceKind.MoonCore];
            var definition = fixture.Definition;
            AssertFailure(CoreResourceRegionCompiler.Compile(null), CoreResourceRegionErrorCode.MissingInput);
            AssertFailure(Compile(fixture, expectedBridgeDigest: "bad"), CoreResourceRegionErrorCode.DigestMismatch);
            AssertFailure(Compile(fixture, Rebuild(definition,
                regionId: new SpecialRegionId("SR_WRONG_CORE_SITE_5"))),
                CoreResourceRegionErrorCode.RegionIdentityMismatch);
            AssertFailure(Compile(fixture, Rebuild(definition, reservedWidth: 2)),
                CoreResourceRegionErrorCode.UnsupportedFootprint);
            AssertFailure(Compile(fixture, Rebuild(definition, designOrigin: new LocalTileCoord(5, 8))),
                CoreResourceRegionErrorCode.InvalidDesignCanvas);
            AssertFailure(Compile(fixture, Rebuild(definition, chunks: new[]
            {
                new CoreResourceDesignChunk(0, 0), new CoreResourceDesignChunk(1, 0),
                new CoreResourceDesignChunk(2, 0), new CoreResourceDesignChunk(0, 1),
                new CoreResourceDesignChunk(3, 1),
            })), CoreResourceRegionErrorCode.InvalidActiveChunk);
        }

        [Test]
        public void InvalidGraphToolRecoveryRewardAndPersistenceFailAtomically()
        {
            var fixture = fixtures[CoreResourceKind.CassiaSap];
            var definition = fixture.Definition;
            AssertFailure(Compile(fixture, Rebuild(definition,
                nodes: definition.Nodes.Concat(new[] { definition.Nodes[0] }))),
                CoreResourceRegionErrorCode.DuplicateNode);

            var invalidNode = definition.Nodes.Select(value => value.Role != CoreResourceNodeRole.EnvironmentTrigger
                ? value
                : new CoreResourceSolutionNode(value.NodeId, value.Role, new LocalTileCoord(60, 60),
                    value.MarkerKind, value.AuthoredOrder, value.RewardSlotId, value.RequiredMarker)).ToArray();
            AssertFailure(Compile(fixture, Rebuild(definition, nodes: invalidNode)),
                CoreResourceRegionErrorCode.InvalidNodeCoordinate);
            AssertFailure(Compile(fixture, Rebuild(definition,
                edges: definition.Edges.Concat(new[] { definition.Edges[0] }))),
                CoreResourceRegionErrorCode.DuplicateEdge);
            AssertFailure(Compile(fixture, Rebuild(definition,
                routes: definition.Routes.Where(value => value.Kind != CoreResourceRouteKind.Low))),
                CoreResourceRegionErrorCode.MissingLowRoute);

            var toolEdges = definition.Edges.Select(value => value.RouteKind != CoreResourceRouteKind.Low
                ? value
                : new CoreResourceSolutionEdge(value.EdgeId, value.FromNodeId, value.ToNodeId,
                    value.Order, value.RouteKind, value.AccessClass, value.Mechanism,
                    value.Required, CoreResourceDependencyKind.WateringCan)).ToArray();
            AssertFailure(Compile(fixture, Rebuild(definition, edges: toolEdges)),
                CoreResourceRegionErrorCode.MandatoryToolDependency);
            AssertFailure(Compile(fixture, Rebuild(definition,
                recoveries: Array.Empty<CoreResourceRecoveryDefinition>())),
                CoreResourceRegionErrorCode.UnrecoverableFailure);

            var wrongReward = new CoreResourceRewardDefinition(
                definition.RequiredReward.RewardId, definition.RequiredReward.NodeId,
                definition.Resource, definition.RequiredReward.SlotId,
                new SpecialPersistenceKey("SR_STATE_WRONG_REWARD"), SpecialPersistenceScope.Reward, 1, true);
            AssertFailure(Compile(fixture, Rebuild(definition, reward: wrongReward)),
                CoreResourceRegionErrorCode.PersistenceMismatch);
        }

        [Test]
        public void ReverseRepeatCultureImmutabilityDigestAndZeroMutationRemainStable()
        {
            var fixture = fixtures[CoreResourceKind.StarNuruk];
            var source = fixture.Definition;
            var chunks = source.ActiveDesignChunks.Reverse().ToList();
            var nodes = source.Nodes.Reverse().ToList();
            var edges = source.Edges.Reverse().ToList();
            var routes = source.Routes.Reverse().Select(value => new CoreResourceRouteDefinition(
                value.RouteId, value.Kind, value.EdgeIds.Reverse())).ToList();
            var recoveries = source.Recoveries.Reverse().ToList();
            var benefits = source.OptionalBenefits.Reverse().ToList();
            var reversed = Rebuild(source, chunks, nodes, edges, routes, recoveries,
                optionalBenefits: benefits);
            chunks.Clear();
            nodes.Clear();
            edges.Clear();
            routes.Clear();
            recoveries.Clear();
            benefits.Clear();

            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var first = Compile(fixture);
                var repeat = Compile(fixture);
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var culture = Compile(fixture, reversed);
                AssertSuccess(first);
                AssertSuccess(repeat);
                AssertSuccess(culture);
                Assert.That(repeat.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));
                Assert.That(culture.CanonicalDigest, Is.EqualTo(first.CanonicalDigest));
                Assert.That(reversed.ActiveDesignChunks, Has.Count.EqualTo(5));
                Assert.That(reversed.Nodes, Has.Count.EqualTo(source.Nodes.Count));
                Assert.That(reversed.Edges, Has.Count.EqualTo(source.Edges.Count));

                var plan = first.Plan;
                Assert.That(plan.SyntheticEdgeCount + plan.TeleportCount + plan.CarveCount +
                    plan.AutoSearchCount + plan.RngSelectionCount + plan.PathfindingCount +
                    plan.WorldMutationCount + plan.TileMutationCount + plan.SceneMutationCount +
                    plan.PrefabMutationCount + plan.InventoryMutationCount + plan.RewardGrantCount +
                    plan.SaveWriteCount, Is.Zero);
                Assert.That(plan.DesignDigest, Does.Match("^[0-9a-f]{64}$"));
                Assert.That(plan.GraphDigest, Does.Match("^[0-9a-f]{64}$"));
                Assert.That(plan.RewardDigest, Does.Match("^[0-9a-f]{64}$"));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }

            var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", ".."));
            var runtimeSources = new[]
            {
                "Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/CoreResourceRegionDefinitions.cs",
                "Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/CoreResourceRegionCompiler.cs",
                "Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/CoreResourceRegionStarterCatalog.cs",
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

        private static CoreResourceRegionResult Compile(
            RegionFixture fixture,
            CoreResourceRegionDefinition definition = null,
            string expectedBridgeDigest = null)
            => CoreResourceRegionCompiler.Compile(new CoreResourceRegionCompileRequest(
                definition ?? fixture.Definition,
                fixture.Bridge, expectedBridgeDigest ?? fixture.Bridge.CanonicalDigest,
                fixture.EntryPlan, fixture.EntryPlan.CanonicalDigest,
                fixture.CollisionPlan, fixture.CollisionPlan.CanonicalDigest,
                fixture.LayerPlan, fixture.LayerPlan.CanonicalDigest,
                fixture.SafetyProof, fixture.SafetyProof.CanonicalDigest));

        private static RegionFixture BuildFixture(
            CoreResourceRegionDefinition definition,
            bool reverse,
            TerrainClusterQuietBufferCandidate quietCandidate)
        {
            var token = definition.Resource.ToString().ToUpperInvariant();
            var reservationId = new SiteReservationId("RES_MAP13_06_" + token);
            var origin = new SectorCoord(5, 5);
            var entryAnchor = new SiteEntryAnchor(
                reservationId, "ENTRY_MAIN", origin, SiteEntrySide.L,
                new[] { 1, 2, 3 }, true, false);
            var returnAnchor = new SiteEntryAnchor(
                reservationId, "RETURN_MAIN", origin, SiteEntrySide.R,
                new[] { 1, 2, 3 }, true, true);
            var reservation = new SiteReservation(
                reservationId, SiteReservationKind.CoreResource, "SPECIAL_CORE", origin,
                new SiteFootprint(1, 1, SiteFootprintTransform.R0, new[]
                {
                    new SiteFootprintCell(0, 0, "CORE_RESOURCE", string.Empty, string.Empty,
                        new[] { SiteEntrySide.L, SiteEntrySide.R })
                }), string.Empty, 1,
                reverse ? new[] { returnAnchor, entryAnchor } : new[] { entryAnchor, returnAnchor });

            var entryId = new SpecialRegionSlotId("SR_SLOT_" + token + "_ENTRY");
            var returnId = new SpecialRegionSlotId("SR_SLOT_" + token + "_RETURN");
            var reward = definition.RequiredReward;
            var rewardNode = definition.Nodes.Single(value => value.NodeId == reward.NodeId);
            var slots = new[]
            {
                new SpecialRegionSlot(entryId, SpecialRegionSlotKind.Entry,
                    new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(0, 1), true,
                    default(SpecialPersistenceScope), default(SpecialPersistenceKey)),
                new SpecialRegionSlot(returnId, SpecialRegionSlotKind.Return,
                    new SpecialRegionSectorOffset(0, 0), new LocalTileCoord(47, 1), true,
                    default(SpecialPersistenceScope), default(SpecialPersistenceKey)),
                new SpecialRegionSlot(reward.SlotId, SpecialRegionSlotKind.Reward,
                    new SpecialRegionSectorOffset(0, 0), rewardNode.Coordinate, true,
                    SpecialPersistenceScope.Reward, reward.PersistenceKey),
            };
            var ports = new[]
            {
                new SpecialRegionPort("SR_PORT_" + token + "_ENTRY", entryId,
                    SpecialRegionSlotKind.Entry, new SpecialRegionSectorOffset(0, 0),
                    new LocalTileCoord(0, 1), SiteEntrySide.L, AccessClass.MandatoryNoTool),
                new SpecialRegionPort("SR_PORT_" + token + "_RETURN", returnId,
                    SpecialRegionSlotKind.Return, new SpecialRegionSectorOffset(0, 0),
                    new LocalTileCoord(47, 1), SiteEntrySide.R, AccessClass.MandatoryNoTool),
            };
            var persistence = new[]
            {
                new SpecialPersistenceBinding(SpecialPersistenceKey.ForRegion(definition.RegionId),
                    SpecialPersistenceScope.Region, default(SpecialRegionSlotId), "INITIAL_UNCLAIMED"),
                new SpecialPersistenceBinding(reward.PersistenceKey, SpecialPersistenceScope.Reward,
                    reward.SlotId, "INITIAL_AVAILABLE"),
            };
            var contract = new SpecialRegionContract(
                definition.RegionId, SpecialRegionKind.CoreResource, reservationId,
                new SpecialRegionFootprint(new[] { new SpecialRegionSectorOffset(0, 0) }),
                new[]
                {
                    new SpecialRegionFixedShellCell(new SpecialRegionSectorOffset(0, 0),
                        new LocalTileCoord(42, 30), "SHELL_" + token + "_A"),
                    new SpecialRegionFixedShellCell(new SpecialRegionSectorOffset(0, 0),
                        new LocalTileCoord(43, 30), "SHELL_" + token + "_B"),
                },
                reverse ? slots.Reverse() : slots,
                reverse ? ports.Reverse() : ports,
                reverse ? persistence.Reverse() : persistence,
                "MAP13_06 source fixture");
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
            var beforeChunks = new[]
            {
                new SpecialRegionQuietChunkPlacement(
                    new ClusterChunkCoord(0, 0), new SectorCoord(4, 5), new ClusterChunkCoord(2, 0)),
                new SpecialRegionQuietChunkPlacement(
                    new ClusterChunkCoord(1, 0), new SectorCoord(4, 5), new ClusterChunkCoord(3, 0)),
            };
            var afterChunks = new[]
            {
                new SpecialRegionQuietChunkPlacement(
                    new ClusterChunkCoord(0, 0), new SectorCoord(6, 5), new ClusterChunkCoord(0, 0)),
                new SpecialRegionQuietChunkPlacement(
                    new ClusterChunkCoord(1, 0), new SectorCoord(6, 5), new ClusterChunkCoord(1, 0)),
            };
            var before = new SpecialRegionQuietBufferPlacement(
                "placement.before." + token, SpecialRegionQuietChunkRole.Before, quietCandidate,
                reverse ? beforeChunks.Reverse() : beforeChunks);
            var after = new SpecialRegionQuietBufferPlacement(
                "placement.after." + token, SpecialRegionQuietChunkRole.After, quietCandidate,
                reverse ? afterChunks.Reverse() : afterChunks);
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
                new SpecialRegionPlacementCollisionCompileRequest(reverse
                    ? new[]
                    {
                        new SpecialRegionOccupancyClaim("SR_FIXED_ACCESS_" + token,
                            SpecialRegionPlacementOwnerKind.CoreResource, accessCells.Reverse(), true),
                        new SpecialRegionOccupancyClaim("SR_FIXED_COLLISION_" + token,
                            SpecialRegionPlacementOwnerKind.CoreResource, fixedCells.Reverse(), true),
                    }
                    : new[]
                    {
                        new SpecialRegionOccupancyClaim("SR_FIXED_COLLISION_" + token,
                            SpecialRegionPlacementOwnerKind.CoreResource, fixedCells, true),
                        new SpecialRegionOccupancyClaim("SR_FIXED_ACCESS_" + token,
                            SpecialRegionPlacementOwnerKind.CoreResource, accessCells, true),
                    }));
            Assert.That(collisionResult.Succeeded, Is.True, string.Join("\n", collisionResult.Errors));

            var layerResult = SpecialRegionFixedSlotLayerCompiler.Compile(
                new SpecialRegionFixedSlotLayerCompileRequest(
                    validation, validation.CanonicalDigest,
                    bridgeResult.Bridge, bridgeResult.CanonicalDigest,
                    entryResult.Plan, entryResult.CanonicalDigest,
                    collisionResult.Plan, collisionResult.CanonicalDigest));
            Assert.That(layerResult.Succeeded, Is.True, string.Join("\n", layerResult.Errors));
            var evidence = Evidence(layerResult.Plan);
            var safetyResult = SpecialRegionPersistenceSafetyCompiler.Compile(
                new SpecialRegionPersistenceSafetyCompileRequest(
                    layerResult.Plan, layerResult.CanonicalDigest,
                    reverse ? evidence.Reverse() : evidence));
            Assert.That(safetyResult.Succeeded, Is.True, string.Join("\n", safetyResult.Errors));

            return new RegionFixture(
                definition, bridgeResult.Bridge, entryResult.Plan, collisionResult.Plan,
                layerResult.Plan, safetyResult.Proofs.Single());
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
            if (reverse)
            {
                reservations.Reverse();
                rows.Reverse();
            }
            return new SiteReservationSnapshot(1306UL, reservations, rows, Array.Empty<CoreBiomeSeed>());
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
                new MicroPatternId("MP_MAP13_06_NO_CHANGE"), out var definition), Is.True);
            var render = TerrainClusterPatternRenderer.Render(new TerrainClusterPatternRenderRequest(
                canvas, canvas.CanonicalDigest,
                traversalResult.Compilation, traversalResult.CanonicalDigest,
                witnessResult.Report, witnessResult.CanonicalDigest,
                catalog, catalog.StableDigest,
                Array.Empty<TerrainClusterPatternZoneCell>(),
                new[]
                {
                    new TerrainClusterPatternPlacementIntent(
                        "TCP_MAP13_06_NO_CHANGE", definition.Id, MicroPatternTransform.R0,
                        new LocalTileCoord(0, 4), definition.ComputeStableDigest())
                }));
            Assert.That(render.Success, Is.True, string.Join("\n", render.Errors));
            var profile = new TerrainClusterQuietBufferProfile(
                "QBUF_MAP13_06", MoonpalaceBiomeId.MoonCrater,
                reverse
                    ? new[] { TerrainClusterQuietBufferUse.AfterLandmark, TerrainClusterQuietBufferUse.BeforeLandmark }
                    : new[] { TerrainClusterQuietBufferUse.BeforeLandmark, TerrainClusterQuietBufferUse.AfterLandmark },
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
                new ClusterRoleAnchor("ANCHOR_ENTRY", ClusterRoleKind.Entry,
                    new LocalTileCoord(0, 1), "NODE_ENTRY"),
                new ClusterRoleAnchor("ANCHOR_BUILD_UP", ClusterRoleKind.BuildUp,
                    new LocalTileCoord(4, 1), "NODE_BUILD_UP"),
                new ClusterRoleAnchor("ANCHOR_CORE", ClusterRoleKind.Core,
                    new LocalTileCoord(10, 1), "NODE_CORE"),
                new ClusterRoleAnchor("ANCHOR_RECOVERY", ClusterRoleKind.Recovery,
                    new LocalTileCoord(17, 1), "NODE_RECOVERY"),
                new ClusterRoleAnchor("ANCHOR_EXIT", ClusterRoleKind.Exit,
                    new LocalTileCoord(23, 1), "NODE_EXIT"),
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
                new TerrainClusterId("TC_MAP13_06"),
                new ClusterFootprint(new[] { new ClusterChunkCoord(0, 0), new ClusterChunkCoord(1, 0) }),
                reverse ? roles.Reverse() : roles,
                reverse ? ports.Reverse() : ports,
                new TerrainClusterTraversalContract(reverse ? variants.Reverse() : variants),
                "MAP13_06");
        }

        private static TraversalEdge CreateEdge(
            string id, TraversalNode from, TraversalNode to, bool mandatory)
        {
            var envelope = new TraversalEnvelope(
                new[] { from.Tile, to.Tile },
                new[] { new LocalTileCoord(from.Tile.X, 0) },
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
                reverse ? new[] { "BENEFIT_REWARD_ACCESS", "BENEFIT_HEIGHT_ADVANTAGE" } :
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
                    "MP_MAP13_06_NO_CHANGE", "1", "MoonCrater", "R0", "FORCE_NO_CHANGE",
                    "catalog.csv", 2),
            };
            var cells = Enumerable.Range(0, 4).SelectMany(y => Enumerable.Range(0, 4)
                .Select((x, index) => new MicroPatternCellRowV2(
                    "MP_MAP13_06_NO_CHANGE", x.ToString(CultureInfo.InvariantCulture),
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

        private static bool IsConnected(IEnumerable<CoreResourceDesignChunk> source)
        {
            var values = new HashSet<CoreResourceDesignChunk>(source);
            var visited = new HashSet<CoreResourceDesignChunk>();
            var pending = new Queue<CoreResourceDesignChunk>();
            pending.Enqueue(values.First());
            visited.Add(values.First());
            while (pending.Count != 0)
            {
                var current = pending.Dequeue();
                foreach (var next in new[]
                         {
                             new CoreResourceDesignChunk(current.X - 1, current.Y),
                             new CoreResourceDesignChunk(current.X + 1, current.Y),
                             new CoreResourceDesignChunk(current.X, current.Y - 1),
                             new CoreResourceDesignChunk(current.X, current.Y + 1),
                         })
                    if (values.Contains(next) && visited.Add(next)) pending.Enqueue(next);
            }
            return visited.Count == values.Count;
        }

        private static CoreResourceRegionDefinition Rebuild(
            CoreResourceRegionDefinition source,
            IEnumerable<CoreResourceDesignChunk> chunks = null,
            IEnumerable<CoreResourceSolutionNode> nodes = null,
            IEnumerable<CoreResourceSolutionEdge> edges = null,
            IEnumerable<CoreResourceRouteDefinition> routes = null,
            IEnumerable<CoreResourceRecoveryDefinition> recoveries = null,
            CoreResourceRewardDefinition reward = null,
            IEnumerable<CoreResourceOptionalBenefitDefinition> optionalBenefits = null,
            SpecialRegionId? regionId = null,
            int? reservedWidth = null,
            LocalTileCoord? designOrigin = null)
            => new CoreResourceRegionDefinition(
                regionId ?? source.RegionId, source.Resource, source.Biome, source.RegionKind,
                source.Mechanism, reservedWidth ?? source.ReservedWidth, source.ReservedHeight,
                designOrigin ?? source.DesignOrigin, source.DesignWidth, source.DesignHeight,
                source.DesignChunkWidth, source.DesignChunkHeight,
                chunks ?? source.ActiveDesignChunks,
                nodes ?? source.Nodes,
                edges ?? source.Edges,
                routes ?? source.Routes,
                recoveries ?? source.Recoveries,
                reward ?? source.RequiredReward,
                optionalBenefits ?? source.OptionalBenefits,
                source.DisplayText);

        private static void AssertRouteCounts(
            CoreResourceRegionDefinition definition, int low, int high, int recovery)
        {
            Assert.That(definition.Routes.Count(value => value.Kind == CoreResourceRouteKind.Low), Is.EqualTo(low));
            Assert.That(definition.Routes.Count(value => value.Kind == CoreResourceRouteKind.High), Is.EqualTo(high));
            Assert.That(definition.Routes.Count(value => value.Kind == CoreResourceRouteKind.Recovery), Is.EqualTo(recovery));
            Assert.That(definition.Nodes.Count(value => value.Role == CoreResourceNodeRole.Failure), Is.EqualTo(1));
            Assert.That(definition.RequiredReward, Is.Not.Null);
            Assert.That(definition.RequiredReward.Amount, Is.EqualTo(1));
        }

        private static void AssertSuccess(CoreResourceRegionResult result)
        {
            Assert.That(result.Succeeded, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Plan, Is.Not.Null);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
        }

        private static void AssertFailure(
            CoreResourceRegionResult result,
            CoreResourceRegionErrorCode code)
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Plan, Is.Null);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(code),
                string.Join("\n", result.Errors));
            Assert.That(result.Errors, Is.Ordered);
            Assert.That(result.Errors.Distinct().Count(), Is.EqualTo(result.Errors.Count));
        }

        private sealed class RegionFixture
        {
            public RegionFixture(
                CoreResourceRegionDefinition definition,
                SpecialRegionSiteBridge bridge,
                SpecialRegionEntryBufferPlan entryPlan,
                SpecialRegionPlacementCollisionPlan collisionPlan,
                SpecialRegionFixedSlotLayerPlan layerPlan,
                SpecialRegionRequiredResourceSafetyProof safetyProof)
            {
                Definition = definition;
                Bridge = bridge;
                EntryPlan = entryPlan;
                CollisionPlan = collisionPlan;
                LayerPlan = layerPlan;
                SafetyProof = safetyProof;
            }

            public CoreResourceRegionDefinition Definition { get; }
            public SpecialRegionSiteBridge Bridge { get; }
            public SpecialRegionEntryBufferPlan EntryPlan { get; }
            public SpecialRegionPlacementCollisionPlan CollisionPlan { get; }
            public SpecialRegionFixedSlotLayerPlan LayerPlan { get; }
            public SpecialRegionRequiredResourceSafetyProof SafetyProof { get; }
        }
    }
}
