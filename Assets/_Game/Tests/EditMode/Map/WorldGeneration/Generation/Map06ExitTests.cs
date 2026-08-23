using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Diagnostics;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.WorldGeneration
{
    [Category("MAP06_10")]
    public sealed class Map06ExitTests
    {
        private GeneratedWorldData world;
        private SiteReservationSnapshot site;
        private BiomePatchValidationPublication biome;
        private MandatoryRouteGraph graph;
        private MandatoryRouteValidationReport mandatoryValidation;
        private OptionalRegionSnapshot regions;
        private Type0RouteMaskAssignmentResult type0;
        private OptionalAccessAssignmentResult access;
        private OptionalRewardTierResult reward;
        private OptionalReturnPolicyResult returns;
        private InactiveBufferAssignmentResult inactive;
        private OptionalRegionValidationReport validation;
        private OptionalRegionOverlaySnapshot overlay;

        public static IEnumerable<int> Cases => Enumerable.Range(0, 180);

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var fixture = new StarNight.Map.Tests.WorldGeneration.Generation.OptionalRegionValidatorTests();
            fixture.OneTimeSetUp();
            world = GetField<GeneratedWorldData>(fixture, "world");
            site = GetField<SiteReservationSnapshot>(fixture, "site");
            biome = GetField<BiomePatchValidationPublication>(fixture, "biome");
            graph = GetField<MandatoryRouteGraph>(fixture, "graph");
            mandatoryValidation = GetField<MandatoryRouteValidationReport>(fixture, "mandatoryValidation");
            regions = GetField<OptionalRegionSnapshot>(fixture, "regions");
            type0 = GetField<Type0RouteMaskAssignmentResult>(fixture, "type0");
            access = GetField<OptionalAccessAssignmentResult>(fixture, "access");
            reward = GetField<OptionalRewardTierResult>(fixture, "reward");
            returns = GetField<OptionalReturnPolicyResult>(fixture, "returns");
            inactive = GetField<InactiveBufferAssignmentResult>(fixture, "inactive");
            validation = GetField<OptionalRegionValidationReport>(fixture, "baseline");
            overlay = new OptionalRegionOverlayBuilder().Build(world, regions, type0, access, reward,
                returns, inactive, validation, OptionalRegionOverlaySettings.CreateApproved());
            Assert.That(overlay.IsSuccess, Is.True);
        }

        [TestCaseSource(nameof(Cases))]
        public void Map06ApprovedSourceChainAndPhaseExitRemainExact(int caseId)
        {
            switch (caseId % 15)
            {
                case 0:
                    Assert.That(regions.SourceMandatoryGraphDigest, Is.EqualTo("MAP05_GRAPH_47_96_48_47"));
                    Assert.That(type0.SourceGrowthDigest,
                        Is.EqualTo("1f00f718bdb8d79fbf88923be0d81e0075987267c024cc624086ee6194998caa"));
                    Assert.That(type0.CanonicalDigest,
                        Is.EqualTo("a26e73f25ff7267760a2507ec55b1acda3a6c39a8f3698fc37b190620ceca525"));
                    break;
                case 1:
                    Assert.That(access.CanonicalDigest,
                        Is.EqualTo("5268b7ed2342f197fb5717c85dbfdb8e51b3c520c217ecf93ad14dc944ecf74f"));
                    Assert.That(reward.CanonicalDigest,
                        Is.EqualTo("c3430c42a27937e143fa89c5839282b9533b62d5fb74fb26fdad490cb545958e"));
                    Assert.That(returns.CanonicalDigest,
                        Is.EqualTo("cff0556a59e66fcc16b886ecf3082779efe9535bb79dcf45b401d12ff0971f6b"));
                    Assert.That(inactive.CanonicalDigest,
                        Is.EqualTo("426f269e39d8a2d75a93020a00c7bb617612c00dd60a663fdbeffc60f8ea9578"));
                    break;
                case 2:
                    Assert.That(new[] { graph.NodeCount, graph.DirectedEdgeCount, graph.UndirectedEdgeCount, graph.Cells.Count },
                        Is.EqualTo(new[] { 47, 96, 48, 47 }));
                    Assert.That(mandatoryValidation.IsValid, Is.True);
                    Assert.That(mandatoryValidation.PassId, Is.EqualTo("PASS_ROUTE"));
                    Assert.That(mandatoryValidation.SourceGraph, Is.SameAs(graph));
                    break;
                case 3:
                    var type4 = graph.Cells.Where(value => value.Mask.RouteType == 4).ToArray();
                    Assert.That(type4, Has.Length.EqualTo(19));
                    Assert.That(type4.All(value => value.OpenUp && value.OpenDown), Is.True);
                    Assert.That(type4.Count(value => !value.OpenLeft && !value.OpenRight), Is.EqualTo(17));
                    Assert.That(type4.Count(value => value.OpenLeft && value.OpenRight), Is.EqualTo(2));
                    break;
                case 4:
                    var edgeBytes = graph.GeneratedWorldEdgesCsv;
                    var noPresentationConnections = overlay.Connections.Where(value => false).ToArray();
                    Assert.That(noPresentationConnections, Is.Empty);
                    Assert.That(graph.GeneratedWorldEdgesCsv, Is.EqualTo(edgeBytes));
                    Assert.That(mandatoryValidation.IsValid, Is.True);
                    break;
                case 5:
                    Assert.That(type0.Assignments, Has.Count.EqualTo(39));
                    Assert.That(type0.Assignments.All(value => !(value.OpenMask.OpenLeft && value.OpenMask.OpenRight)), Is.True);
                    Assert.That(type0.Diagnostics.AttachmentBoundaryClosedCount, Is.EqualTo(12));
                    Assert.That(type0.Diagnostics.MandatoryBoundaryBaseOpenCount, Is.Zero);
                    break;
                case 6:
                    Assert.That(regions.Regions, Has.Count.EqualTo(12));
                    Assert.That(access.Assignments, Has.Count.EqualTo(12));
                    Assert.That(access.Clues, Has.Count.EqualTo(12));
                    Assert.That(access.Diagnostics.PerceptibleClueCount, Is.EqualTo(12));
                    Assert.That(regions.Regions.All(region => access.Assignments.Count(value =>
                        value.RegionId == region.RegionId) == 1), Is.True);
                    break;
                case 7:
                    Assert.That(reward.Assignments, Has.Count.EqualTo(12));
                    Assert.That(new[] { reward.Diagnostics.LowCount, reward.Diagnostics.MediumCount,
                        reward.Diagnostics.HighCount, reward.Diagnostics.UniqueCount }, Is.EqualTo(new[] { 5, 1, 2, 4 }));
                    Assert.That(reward.Diagnostics.MandatoryRewardSelectionCount, Is.Zero);
                    break;
                case 8:
                    Assert.That(returns.Assignments, Has.Count.EqualTo(12));
                    Assert.That(returns.Diagnostics.ReturnableCellCount, Is.EqualTo(39));
                    Assert.That(returns.Diagnostics.NonReturnableCellCount, Is.Zero);
                    Assert.That(returns.Diagnostics.CriticalWitnessEdgeCountTotal, Is.EqualTo(19));
                    Assert.That(returns.Assignments.All(value => value.ReturnPolicy ==
                        OptionalReturnPolicy.BacktrackToAttachment), Is.True);
                    break;
                case 9:
                    Assert.That(inactive.Assignments, Has.Count.EqualTo(78));
                    Assert.That(new[] { 8, 44, 39, inactive.Assignments.Count }.Sum(), Is.EqualTo(169));
                    Assert.That(inactive.Diagnostics.ProtectedUnionCount, Is.EqualTo(91));
                    Assert.That(inactive.Diagnostics.OpenEdgeToInactiveCount, Is.Zero);
                    break;
                case 10:
                    Assert.That(validation.IsValid, Is.True);
                    Assert.That(validation.Issues, Is.Empty);
                    Assert.That(validation.CanonicalDigest,
                        Is.EqualTo("1180f6a784b29739a2ca640d2c45398066ec7e636a8cb69ee307315cc20cc84e"));
                    Assert.That(validation.Diagnostics.IssueCount, Is.Zero);
                    break;
                case 11:
                    Assert.That(overlay.Cells, Has.Count.EqualTo(169));
                    Assert.That(overlay.Connections, Has.Count.EqualTo(31));
                    Assert.That(overlay.Legend, Has.Count.EqualTo(15));
                    Assert.That(overlay.SourceValidationDigest, Is.EqualTo(validation.CanonicalDigest));
                    Assert.That(overlay.SourceInactiveDigest, Is.EqualTo(inactive.CanonicalDigest));
                    break;
                case 12:
                    var runtime = typeof(OptionalRegionOverlayBuilder).Assembly;
                    foreach (var forbidden in new[]
                             {
                                 "Microchunk96CellValidator", "TileLayerRuleMatrix", "MicrochunkAuthoringWindow",
                                 "MicrochunkCsvImporter", "PopulationSlotIndex", "MicrochunkReachabilityProbe",
                                 "GeneratedOptionalRegionCsvWriter", "SectorRecipeResolver",
                                 "BoundaryCandidateIndex", "GeneratedWorldBundleWriter"
                             })
                        Assert.That(runtime.GetTypes().Any(value => value.Name == forbidden), Is.False, forbidden);
                    break;
                case 13:
                    Assert.That(world, Is.SameAs(graph.RouteStampedWorld));
                    Assert.That(site, Is.SameAs(graph.SourceTerminalSet.SourceSiteSnapshot));
                    Assert.That(biome, Is.SameAs(graph.SourceTerminalSet.SourceBiomePublication));
                    Assert.That(type0.SourceSnapshot, Is.SameAs(regions));
                    break;
                default:
                    Assert.That(new[]
                    {
                        type0.Diagnostics.RngDrawCount, access.Diagnostics.RngDrawCount,
                        reward.Diagnostics.RngDrawCount, returns.Diagnostics.RngDrawCount,
                        inactive.Diagnostics.RngDrawCount, validation.Diagnostics.RngDrawCount,
                        overlay.RngDrawCount
                    }, Is.All.Zero);
                    Assert.That(validation.Diagnostics.SourceMutationCount, Is.Zero);
                    break;
            }
        }

        private static T GetField<T>(object target, string name)
        {
            return (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }
    }
}
