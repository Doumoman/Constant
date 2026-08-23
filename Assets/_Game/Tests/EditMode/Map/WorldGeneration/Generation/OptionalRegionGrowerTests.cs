using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    [Category("MAP06_03")]
    public sealed class OptionalRegionGrowerTests
    {
        private const string GraphDigest = "MAP05_GRAPH_47_96_48_47";
        private MandatoryRouteGraph graph;
        private MandatoryRouteValidationReport report;
        private GeneratedWorldData world;
        private SiteReservationSnapshot site;
        private BiomePatchValidationPublication biome;
        private OptionalAttachmentEnumerationResult attachments;
        private OptionalRegionGrowthSettings baselineSettings;
        private OptionalRegionGrowthResult baseline;
        private string sourceSignature;

        public static IEnumerable<int> SettingsCases => Enumerable.Range(0, 24);
        public static IEnumerable<int> RegionIdentityCases => Enumerable.Range(0, 24);
        public static IEnumerable<int> DepthCases => Enumerable.Range(0, 40);
        public static IEnumerable<int> BridgeCases => Enumerable.Range(0, 32);
        public static IEnumerable<int> FilterCases => Enumerable.Range(0, 32);
        public static IEnumerable<int> AccountingCases => Enumerable.Range(0, 20);
        public static IEnumerable<int> DigestCases => Enumerable.Range(0, 20);
        public static IEnumerable<int> MutationCases => Enumerable.Range(0, 12);
        public static IEnumerable<int> Type4Cases => Enumerable.Range(0, 8);
        public static IEnumerable<string> FutureRuntimeSymbols => new[]
        {
            "OptionalOverlayEdge", "OptionalReturnConnection", "OptionalClueAssigner",
            "MicrochunkObjectSlotValidator", "MicrochunkCsvExporter", "OptionalRegionOverlayRenderer",
            "OptionalRegionValidationOverlayWindow", "OptionalRegionOverlay", "GeneratedOptionalRegionCsvWriter",
            "OptionalRouteMaskLookup"
        };

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var fixture = new MandatoryRouteGraphValidatorTests();
            fixture.OneTimeSetUp();
            graph = GetField<MandatoryRouteGraph>(fixture, typeof(MandatoryRouteGraphValidatorTests), "graph");
            var validation = new MandatoryRouteGraphValidator().Validate(graph);
            Assert.That(validation.Status, Is.EqualTo(MandatoryRouteValidationStatus.Completed));
            Assert.That(validation.Succeeded, Is.True);
            report = validation.Report;
            world = graph.RouteStampedWorld;
            site = graph.SourceTerminalSet.SourceSiteSnapshot;
            biome = graph.SourceTerminalSet.SourceBiomePublication;
            attachments = new OptionalAttachmentEnumerator().Enumerate(
                world, graph, report, site, biome, new OptionalAttachmentEnumerationSettings());
            baselineSettings = Settings(12, 6, 1, 2, 3, 4);
            baseline = Grow(baselineSettings);
            Assert.That(attachments.Candidates, Has.Count.EqualTo(51));
            Assert.That(baseline.Snapshot.Regions, Is.Not.Empty);
            sourceSignature = SourceSignature();
        }

        [TestCaseSource(nameof(SettingsCases))]
        public void SettingsValidateCopyFreezeAndPatternMapping(int caseId)
        {
            if (caseId < 16)
            {
                var pattern = new List<OptionalRegionDepth>
                {
                    new OptionalRegionDepth(1 + (caseId % 4)),
                    new OptionalRegionDepth(1 + ((caseId + 1) % 4))
                };
                var expectedFirst = pattern[0];
                var settings = new OptionalRegionGrowthSettings(caseId + 1, 4 + (caseId % 13), pattern);
                pattern[0] = new OptionalRegionDepth(4);
                Assert.That(settings.MaxRegions, Is.EqualTo(caseId + 1));
                Assert.That(settings.TargetDepthPattern[0], Is.EqualTo(expectedFirst));
                Assert.That(settings.GetTargetDepth(caseId * 3), Is.EqualTo(
                    settings.TargetDepthPattern[(caseId * 3) % settings.TargetDepthPattern.Count]));
                Assert.Throws<NotSupportedException>(() =>
                    ((IList<OptionalRegionDepth>)settings.TargetDepthPattern).Add(new OptionalRegionDepth(1)));
            }
            else
            {
                var invalid = caseId - 16;
                Assert.That(() => CreateInvalidSettings(invalid), Throws.Exception);
            }
        }

        [TestCaseSource(nameof(RegionIdentityCases))]
        public void RegionIdsAreContiguousAndAttachmentsPreserveCanonicalSource(int caseId)
        {
            var region = baseline.Snapshot.Regions[caseId % baseline.Snapshot.Regions.Count];
            var ordinal = caseId % baseline.Snapshot.Regions.Count;
            var source = attachments.Candidates[region.Attachment.AttachmentOrder];
            Assert.That(region.RegionId.Value, Is.EqualTo("OPT_REGION_" + ordinal.ToString("D4", CultureInfo.InvariantCulture)));
            Assert.That(region.Attachment.MandatoryRouteSectorIndex, Is.EqualTo(source.MandatoryRouteSectorIndex));
            Assert.That(region.Attachment.MandatoryRouteNodeId, Is.EqualTo(source.MandatoryRouteNodeId));
            Assert.That(region.Attachment.EntrySectorIndex, Is.EqualTo(source.EntrySectorIndex));
            Assert.That(region.Attachment.EntrySideFromMandatoryDx, Is.EqualTo(source.DirectionDx));
            Assert.That(region.Attachment.EntrySideFromMandatoryDy, Is.EqualTo(source.DirectionDy));
            Assert.That(region.AccessRule, Is.EqualTo(OptionalRegionAccessRule.Basic));
            Assert.That(region.RewardTier, Is.EqualTo(OptionalRewardTier.None));
            Assert.That(region.ReturnPolicy, Is.EqualTo(OptionalReturnPolicy.BacktrackToAttachment));
            Assert.That(region.Cells.Count(value => value.IsAttachmentCell), Is.EqualTo(1));
            Assert.That(region.Cells.Single(value => value.IsAttachmentCell).SectorIndex, Is.EqualTo(source.EntrySectorIndex));
        }

        [TestCaseSource(nameof(DepthCases))]
        public void RegionsAreConnectedAndDepthsAreExactInternalShortestDistancePlusOne(int caseId)
        {
            var region = baseline.Snapshot.Regions[caseId % baseline.Snapshot.Regions.Count];
            var distances = Distances(region);
            var target = baselineSettings.GetTargetDepth(region.Attachment.AttachmentOrder).Value;
            Assert.That(distances.Count, Is.EqualTo(region.Cells.Count));
            Assert.That(region.Cells.All(value => value.Depth.Value == distances[value.SectorIndex] + 1), Is.True);
            Assert.That(region.MaxDepth.Value, Is.EqualTo(region.Cells.Max(value => value.Depth.Value)));
            Assert.That(region.MaxDepth.Value, Is.EqualTo(target));
            Assert.That(region.MaxDepth.Value, Is.InRange(1, 4));
            Assert.That(region.Cells.Count, Is.LessThanOrEqualTo(baselineSettings.MaxCellsPerRegion));
        }

        [TestCaseSource(nameof(BridgeCases))]
        public void EveryRegionHasExactlyOneMandatoryBridgeAndNoHorizontalThroughCell(int caseId)
        {
            var region = baseline.Snapshot.Regions[caseId % baseline.Snapshot.Regions.Count];
            var regionCells = new HashSet<int>(region.Cells.Select(value => value.SectorIndex));
            var mandatory = new HashSet<int>(graph.Cells.Select(value => value.SectorIndex));
            var bridges = regionCells.Sum(index => Neighbors(index).Count(mandatory.Contains));
            Assert.That(bridges, Is.EqualTo(1));
            Assert.That(mandatory.Contains(region.Attachment.MandatoryRouteSectorIndex), Is.True);
            Assert.That(Neighbors(region.Attachment.EntrySectorIndex).Count(mandatory.Contains), Is.EqualTo(1));
            Assert.That(Neighbors(region.Attachment.EntrySectorIndex).Single(mandatory.Contains),
                Is.EqualTo(region.Attachment.MandatoryRouteSectorIndex));
            Assert.That(regionCells.Any(index =>
                regionCells.Contains(WorldGridIndex.GetLeftIndex(index)) &&
                regionCells.Contains(WorldGridIndex.GetRightIndex(index))), Is.False);
        }

        [TestCaseSource(nameof(FilterCases))]
        public void CanonicalGrowthFiltersAccountForEveryActualProbe(int caseId)
        {
            var result = Grow(Settings(1 + (caseId % 9), 4 + (caseId % 3), 1, 2, 3, 4));
            var d = result.Diagnostics;
            var filters = new[]
            {
                d.OutOfBoundsCellRejected, d.MandatoryCellRejected, d.AdditionalMandatoryBridgeRejected,
                d.SiteReservationCellRejected, d.BiomeReservedCellRejected, d.ClaimedCellRejected,
                d.DuplicateFrontierRejected, d.HorizontalThroughCellRejected
            };
            Assert.That(filters.All(value => value >= 0), Is.True);
            Assert.That(d.RawCellProbes, Is.GreaterThanOrEqualTo(filters.Sum()));
            Assert.That(d.NoTargetDepthPathRejected, Is.LessThanOrEqualTo(d.RejectedCandidateCount));
            Assert.That(result.Snapshot.Cells.All(cell => !site.GetSector(cell.SectorIndex).IsReserved), Is.True);
            Assert.That(result.Snapshot.Cells.All(cell => IsActiveBiome(cell.SectorIndex)), Is.True);
            Assert.That(result.Snapshot.Cells.Select(cell => cell.SectorIndex).Distinct().Count(),
                Is.EqualTo(result.Snapshot.Cells.Count));
        }

        [TestCaseSource(nameof(AccountingCases))]
        public void RegionLimitRejectionsOverlapAndDiagnosticsAccountingStayExact(int caseId)
        {
            var maxRegions = 1 + (caseId % 8);
            var result = Grow(Settings(maxRegions, 4 + (caseId % 3), 1, 2, 3, 4));
            var d = result.Diagnostics;
            Assert.That(d.SourceCandidateCount, Is.EqualTo(d.AttemptedCandidates + d.RegionLimitSkipped));
            Assert.That(d.AttemptedCandidates, Is.EqualTo(d.AcceptedRegionCount + d.RejectedCandidateCount));
            Assert.That(d.AcceptedRegionCount, Is.EqualTo(
                d.Depth1RegionCount + d.Depth2RegionCount + d.Depth3RegionCount + d.Depth4RegionCount));
            Assert.That(d.AcceptedRegionCount, Is.LessThanOrEqualTo(maxRegions));
            Assert.That(d.AcceptedCellCount, Is.EqualTo(result.Snapshot.Cells.Count));
            Assert.That(d.RejectionCodes, Has.Count.EqualTo(d.RejectedCandidateCount));
            Assert.That(result.Snapshot.Cells.Select(value => value.SectorIndex).Distinct().Count(),
                Is.EqualTo(result.Snapshot.Cells.Count));
        }

        [TestCaseSource(nameof(DigestCases))]
        public void CanonicalDigestIsCultureOrderFreshAndServiceReuseStable(int caseId)
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = (caseId & 1) == 0
                    ? CultureInfo.GetCultureInfo("en-US")
                    : CultureInfo.GetCultureInfo("tr-TR");
                var source = attachments;
                if ((caseId & 2) != 0)
                {
                    source = new OptionalAttachmentEnumerationResult(
                        attachments.Candidates.Reverse(), attachments.Diagnostics,
                        graph.Cells.Select(value => value.SectorIndex), graph.NodeCount,
                        graph.DirectedEdgeCount, graph.CellCount);
                }
                var result = new OptionalRegionGrower().Grow(
                    world, graph, report, site, biome, source, GraphDigest, baselineSettings);
                Assert.That(result.CanonicalDigest, Is.EqualTo(baseline.CanonicalDigest));
                Assert.That(result.SourceAttachmentDigest, Is.EqualTo(attachments.CanonicalDigest));
                Assert.That(result.SourceMandatoryGraphDigest, Is.EqualTo(GraphDigest));
                Assert.That(result.CanonicalDigest.Length, Is.EqualTo(64));
                Assert.That(result.CanonicalDigest.All(IsLowerHex), Is.True);
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [TestCaseSource(nameof(MutationCases))]
        public void GrowthMutatesNoSourceConsumesNoRngAndPublishesFrozenCollections(int caseId)
        {
            var before = SourceSignature();
            var result = Grow(baselineSettings);
            Assert.That(SourceSignature(), Is.EqualTo(before));
            Assert.That(SourceSignature(), Is.EqualTo(sourceSignature));
            Assert.That(result.RngDrawCount, Is.Zero);
            Assert.That(result.Snapshot.SourceMandatoryNodeCount, Is.EqualTo(47));
            Assert.That(result.Snapshot.SourceMandatoryDirectedEdgeCount, Is.EqualTo(96));
            Assert.That(result.Snapshot.SourceMandatoryRouteCellCount, Is.EqualTo(47));
            Assert.That(result.Snapshot.Cells.All(value => !value.RequiresReturnConnection), Is.True);
            Assert.Throws<NotSupportedException>(() =>
                ((IList<OptionalRegion>)result.Snapshot.Regions).Add(result.Snapshot.Regions[0]));
        }

        [TestCaseSource(nameof(Type4Cases))]
        public void MandatoryType4StillRequiresUpDownAndKeepsLeftRightIndependent(int caseId)
        {
            var left = (caseId & 1) != 0;
            var right = (caseId & 2) != 0;
            Assert.That(graph.MaskFamily.TryResolve(left, right, true, true, out var mask), Is.True);
            Assert.That(mask.OpenUp && mask.OpenDown, Is.True);
            Assert.That(mask.OpenLeft, Is.EqualTo(left));
            Assert.That(mask.OpenRight, Is.EqualTo(right));
            Assert.That(mask.MaskId, Is.EqualTo(new[]
            {
                MandatoryRouteMaskFamily.Type4UdId, MandatoryRouteMaskFamily.Type4LudId,
                MandatoryRouteMaskFamily.Type4RudId, MandatoryRouteMaskFamily.Type4LrudId
            }[caseId & 3]));
        }

        [TestCaseSource(nameof(FutureRuntimeSymbols))]
        public void Map06_04PlusRuntimeSymbolsRemainAbsent(string typeName)
        {
            Assert.That(typeof(OptionalRegionGrower).Assembly.GetType(
                "StarNight.Map.WorldGeneration.Generation." + typeName, false), Is.Null);
        }

        [Test]
        public void Map06_03RuntimeAndTestSymbolsArePresentWithoutLaterSurfaces()
        {
            var runtime = typeof(OptionalRegionGrower).Assembly;
            foreach (var name in new[]
            {
                "OptionalRegionGrowthSettings", "OptionalRegionGrowthDiagnostics",
                "OptionalRegionGrowthResult", "OptionalRegionGrower"
            })
                Assert.That(runtime.GetType("StarNight.Map.WorldGeneration.Generation." + name, false), Is.Not.Null);
            foreach (var type in new[]
            {
                typeof(OptionalRegionGrowthSettings), typeof(OptionalRegionGrowthDiagnostics),
                typeof(OptionalRegionGrowthResult), typeof(OptionalRegionGrower)
            })
                Assert.That(type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(value => !value.IsLiteral && !value.IsInitOnly), Is.Empty, type.FullName);
            Assert.That(typeof(OptionalRegionGrowerTests).Assembly.GetType(
                "StarNight.Map.Tests.WorldGeneration.Generation.OptionalRegionGrowerTests", false), Is.Not.Null);
            Assert.That(runtime.GetReferencedAssemblies().Any(value => value.Name == "UnityEditor"), Is.False);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        public void RequiredReferenceInputsAndGraphDigestAreGuarded(int caseId)
        {
            Assert.That(() => GrowWithInvalidInput(caseId), Throws.Exception);
        }

        [Test]
        public void ApprovedFixturePublishesCanonicalSummary()
        {
            var d = baseline.Diagnostics;
            TestContext.WriteLine(
                "MAP06_03_SUMMARY source={0} attempted={1} accepted={2} rejected={3} skipped={4} cells={5} depths={6}/{7}/{8}/{9} raw={10} horizontal={11} digest={12}",
                d.SourceCandidateCount, d.AttemptedCandidates, d.AcceptedRegionCount, d.RejectedCandidateCount,
                d.RegionLimitSkipped, d.AcceptedCellCount, d.Depth1RegionCount, d.Depth2RegionCount,
                d.Depth3RegionCount, d.Depth4RegionCount, d.RawCellProbes,
                d.HorizontalThroughCellRejected, baseline.CanonicalDigest);
            Assert.That(attachments.CanonicalDigest,
                Is.EqualTo("68b438c523645c2f6721fa0c104c3cd4c282076292cd2e035cd20a2b272aaee6"));
            Assert.That(graph.NodeCount, Is.EqualTo(47));
            Assert.That(graph.DirectedEdgeCount, Is.EqualTo(96));
            Assert.That(graph.UndirectedEdgeCount, Is.EqualTo(48));
            Assert.That(graph.CellCount, Is.EqualTo(47));
            Assert.That(new[]
            {
                d.SourceCandidateCount, d.AttemptedCandidates, d.AcceptedRegionCount,
                d.RejectedCandidateCount, d.RegionLimitSkipped, d.AcceptedCellCount,
                d.Depth1RegionCount, d.Depth2RegionCount, d.Depth3RegionCount, d.Depth4RegionCount
            }, Is.EqualTo(new[] { 51, 32, 12, 20, 19, 39, 5, 0, 2, 5 }));
            Assert.That(new[]
            {
                d.RawCellProbes, d.OutOfBoundsCellRejected, d.MandatoryCellRejected,
                d.AdditionalMandatoryBridgeRejected, d.SiteReservationCellRejected,
                d.BiomeReservedCellRejected, d.ClaimedCellRejected,
                d.DuplicateFrontierRejected, d.HorizontalThroughCellRejected,
                d.NoTargetDepthPathRejected
            }, Is.EqualTo(new[] { 219, 3, 22, 65, 0, 0, 17, 50, 10, 8 }));
            Assert.That(baseline.CanonicalDigest,
                Is.EqualTo("1f00f718bdb8d79fbf88923be0d81e0075987267c024cc624086ee6194998caa"));
        }

        private OptionalRegionGrowthResult Grow(OptionalRegionGrowthSettings settings)
        {
            return new OptionalRegionGrower().Grow(
                world, graph, report, site, biome, attachments, GraphDigest, settings);
        }

        private void GrowWithInvalidInput(int caseId)
        {
            var grower = new OptionalRegionGrower();
            switch (caseId)
            {
                case 0: grower.Grow(null, graph, report, site, biome, attachments, GraphDigest, baselineSettings); break;
                case 1: grower.Grow(world, null, report, site, biome, attachments, GraphDigest, baselineSettings); break;
                case 2: grower.Grow(world, graph, null, site, biome, attachments, GraphDigest, baselineSettings); break;
                case 3: grower.Grow(world, graph, report, null, biome, attachments, GraphDigest, baselineSettings); break;
                case 4: grower.Grow(world, graph, report, site, null, attachments, GraphDigest, baselineSettings); break;
                case 5: grower.Grow(world, graph, report, site, biome, null, GraphDigest, baselineSettings); break;
                case 6: grower.Grow(world, graph, report, site, biome, attachments, null, baselineSettings); break;
                case 7: grower.Grow(world, graph, report, site, biome, attachments, string.Empty, baselineSettings); break;
                case 8: grower.Grow(world, graph, report, site, biome, attachments, " PADDED ", baselineSettings); break;
                case 9: grower.Grow(world, graph, report, site, biome, attachments, GraphDigest, null); break;
                default: throw new ArgumentOutOfRangeException(nameof(caseId));
            }
        }

        private static OptionalRegionGrowthSettings Settings(int maxRegions, int maxCells, params int[] depths)
        {
            return new OptionalRegionGrowthSettings(
                maxRegions, maxCells, depths.Select(value => new OptionalRegionDepth(value)));
        }

        private static void CreateInvalidSettings(int caseId)
        {
            switch (caseId)
            {
                case 0: new OptionalRegionGrowthSettings(0, 4, new[] { new OptionalRegionDepth(1) }); break;
                case 1: new OptionalRegionGrowthSettings(10000, 4, new[] { new OptionalRegionDepth(1) }); break;
                case 2: new OptionalRegionGrowthSettings(1, 0, new[] { new OptionalRegionDepth(1) }); break;
                case 3: new OptionalRegionGrowthSettings(1, 17, new[] { new OptionalRegionDepth(1) }); break;
                case 4: new OptionalRegionGrowthSettings(1, 4, null); break;
                case 5: new OptionalRegionGrowthSettings(1, 4, Array.Empty<OptionalRegionDepth>()); break;
                case 6: new OptionalRegionGrowthSettings(1, 1, new[] { new OptionalRegionDepth(2) }); break;
                case 7: new OptionalRegionGrowthSettings(1, 4, new[] { default(OptionalRegionDepth) }); break;
                default: throw new ArgumentOutOfRangeException(nameof(caseId));
            }
        }

        private static Dictionary<int, int> Distances(OptionalRegion region)
        {
            var owned = new HashSet<int>(region.Cells.Select(value => value.SectorIndex));
            var result = new Dictionary<int, int> { { region.Attachment.EntrySectorIndex, 0 } };
            var queue = new Queue<int>();
            queue.Enqueue(region.Attachment.EntrySectorIndex);
            while (queue.Count > 0)
            {
                var parent = queue.Dequeue();
                foreach (var neighbor in Neighbors(parent))
                {
                    if (!owned.Contains(neighbor) || result.ContainsKey(neighbor)) continue;
                    result.Add(neighbor, result[parent] + 1);
                    queue.Enqueue(neighbor);
                }
            }
            return result;
        }

        private static IEnumerable<int> Neighbors(int sectorIndex)
        {
            var values = new[]
            {
                WorldGridIndex.GetLeftIndex(sectorIndex), WorldGridIndex.GetRightIndex(sectorIndex),
                WorldGridIndex.GetUpIndex(sectorIndex), WorldGridIndex.GetDownIndex(sectorIndex)
            };
            return values.Where(value => value >= 0);
        }

        private bool IsActiveBiome(int sectorIndex)
        {
            var cell = biome.WorldWithBiomeAssignments.GetCell(sectorIndex);
            return !string.IsNullOrEmpty(cell.PrimaryBiomeId) && !string.IsNullOrEmpty(cell.PatchId);
        }

        private string SourceSignature()
        {
            return graph.NodeCount + "/" + graph.DirectedEdgeCount + "/" + graph.CellCount + "|" +
                string.Join(",", graph.Nodes.Select(value => value.NodeId.Value + ":" + value.SectorIndex + ":" + value.RouteMaskId)) + "|" +
                string.Join(",", graph.Cells.Select(value => value.SectorIndex + ":" + value.RouteMaskId)) + "|" +
                world.Seed + ":" + world.Cells.Count + "|" + site.Seed + ":" + site.Reservations.Count + ":" + site.Sectors.Count + "|" +
                biome.Snapshot.Seed + ":" + biome.PatchRows.Count + "|" + report.PassId + ":" + report.Violations.Count + "|" +
                attachments.CanonicalDigest;
        }

        private static bool IsLowerHex(char value)
        {
            return (value >= '0' && value <= '9') || (value >= 'a' && value <= 'f');
        }

        private static T GetField<T>(object target, Type type, string name)
        {
            return (T)type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }
    }
}
