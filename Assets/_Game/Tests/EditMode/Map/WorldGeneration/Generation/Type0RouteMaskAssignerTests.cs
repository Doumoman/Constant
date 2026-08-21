using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.Tests.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    [Category("MAP06_04")]
    public sealed class Type0RouteMaskAssignerTests
    {
        private const string GraphDigest = "MAP05_GRAPH_47_96_48_47";
        private static readonly string[] ExpectedIds =
        {
            "ROUTE_T0_NONE", "ROUTE_T0_L", "ROUTE_T0_R", "ROUTE_T0_U", "ROUTE_T0_D",
            "ROUTE_T0_LU", "ROUTE_T0_LD", "ROUTE_T0_RU", "ROUTE_T0_RD", "ROUTE_T0_UD",
            "ROUTE_T0_LUD", "ROUTE_T0_RUD"
        };

        private MandatoryRouteGraph graph;
        private OptionalRegionGrowthResult growth;
        private OptionalRegionGrowthResult unsupportedGrowth;
        private WorldRouteDefinitionSet definitionSet;
        private Type0RouteMaskAssignmentResult baseline;
        private string sourceSignature;

        public static IEnumerable<int> ValueContractCases => Enumerable.Range(0, 40);
        public static IEnumerable<int> CatalogCases => Enumerable.Range(0, 36);
        public static IEnumerable<int> CatalogRejectionCases => Enumerable.Range(0, 40);
        public static IEnumerable<int> PerCellCases => Enumerable.Range(0, 40);
        public static IEnumerable<int> BoundaryCases => Enumerable.Range(0, 32);
        public static IEnumerable<int> UnsupportedCases => Enumerable.Range(0, 24);
        public static IEnumerable<int> DigestCases => Enumerable.Range(0, 20);
        public static IEnumerable<int> IntegrityCases => Enumerable.Range(0, 24);

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var fixture = new MandatoryRouteGraphValidatorTests();
            fixture.OneTimeSetUp();
            graph = GetField<MandatoryRouteGraph>(fixture, typeof(MandatoryRouteGraphValidatorTests), "graph");
            var validation = new MandatoryRouteGraphValidator().Validate(graph);
            Assert.That(validation.Succeeded, Is.True);
            var report = validation.Report;
            var world = graph.RouteStampedWorld;
            var site = graph.SourceTerminalSet.SourceSiteSnapshot;
            var biome = graph.SourceTerminalSet.SourceBiomePublication;
            var attachments = new OptionalAttachmentEnumerator().Enumerate(
                world, graph, report, site, biome, new OptionalAttachmentEnumerationSettings());
            var settings = new OptionalRegionGrowthSettings(
                12, 6, new[]
                {
                    new OptionalRegionDepth(1), new OptionalRegionDepth(2),
                    new OptionalRegionDepth(3), new OptionalRegionDepth(4)
                });
            growth = new OptionalRegionGrower().Grow(
                world, graph, report, site, biome, attachments, GraphDigest, settings);
            definitionSet = BuildDefinitionSet(CreateValidRows());
            baseline = new Type0RouteMaskAssigner().Assign(growth, definitionSet);
            unsupportedGrowth = CreateHorizontalThroughGrowth();
            sourceSignature = SourceSignature();

            Assert.That(baseline.IsSuccess, Is.True, FormatErrors(baseline));
            Assert.That(growth.Snapshot.Regions, Has.Count.EqualTo(12));
            Assert.That(growth.Snapshot.Cells, Has.Count.EqualTo(39));
        }

        [TestCaseSource(nameof(ValueContractCases))]
        public void OpenMaskIdAndRecordValueContractsAreStable(int caseId)
        {
            if (caseId < 16)
            {
                var bits = caseId;
                var left = (bits & 1) != 0;
                var right = (bits & 2) != 0;
                if (left && right)
                {
                    Assert.Throws<ArgumentException>(() => new Type0RouteOpenMask(left, right, (bits & 4) != 0, (bits & 8) != 0));
                    return;
                }
                var first = new Type0RouteOpenMask(left, right, (bits & 4) != 0, (bits & 8) != 0);
                var second = new Type0RouteOpenMask(left, right, (bits & 4) != 0, (bits & 8) != 0);
                Assert.That(first, Is.EqualTo(second));
                Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
                Assert.That(first.CompareTo(second), Is.Zero);
                Assert.That(first.OpenCount, Is.EqualTo(CountBits(bits)));
                Assert.That(first.HasHorizontalThrough, Is.False);
                return;
            }

            if (caseId < 28)
            {
                var expected = ExpectedIds[caseId - 16];
                var id = new Type0RouteMaskId(expected);
                Assert.That(id.IsValid, Is.True);
                Assert.That(id.Value, Is.EqualTo(expected));
                Assert.That(Type0RouteMaskId.TryCreate(expected, out var parsed), Is.True);
                Assert.That(parsed, Is.EqualTo(id));
                Assert.That(parsed.GetHashCode(), Is.EqualTo(id.GetHashCode()));
                return;
            }

            if (caseId < 34)
            {
                var invalid = new[] { null, string.Empty, "ROUTE_T0_", "route_t0_L", "ROUTE_T1_L", " ROUTE_T0_L" }[caseId - 28];
                Assert.That(Type0RouteMaskId.TryCreate(invalid, out var parsed), Is.False);
                Assert.That(parsed.IsValid, Is.False);
                Assert.That(() => new Type0RouteMaskId(invalid), Throws.Exception);
                return;
            }

            var record = baseline.RegisteredMasks[(caseId - 34) % baseline.RegisteredMasks.Count];
            Assert.That(record.MaskId.IsValid, Is.True);
            Assert.That(record.RouteType, Is.Zero);
            Assert.That(record.MandatoryAllowed, Is.False);
            Assert.That(record.Active, Is.True);
            Assert.That(record.SourceDefinition, Is.Not.Null);
            Assert.That(record.MaskId.Value, Is.EqualTo(record.SourceDefinition.RouteMaskId));
        }

        [TestCaseSource(nameof(CatalogCases))]
        public void ExactTwelveRowCatalogPreservesMatrixOrderAndSourceIdentity(int caseId)
        {
            var ordinal = caseId % 12;
            var record = baseline.RegisteredMasks[ordinal];
            Assert.That(baseline.RegisteredMasks, Has.Count.EqualTo(12));
            Assert.That(record.MaskId.Value, Is.EqualTo(ExpectedIds[ordinal]));
            Assert.That(record.RouteType, Is.Zero);
            Assert.That(record.Active, Is.True);
            Assert.That(record.MandatoryAllowed, Is.False);
            Assert.That(record.OpenMask.HasHorizontalThrough, Is.False);
            Assert.That(record.SourceDefinition, Is.SameAs(definitionSet.RouteMasks[ExpectedIds[ordinal]]));
            Assert.That(Bits(record.OpenMask), Is.EqualTo(BitsForId(ExpectedIds[ordinal])));
            Assert.That(baseline.Diagnostics.RegisteredType0MaskCount, Is.EqualTo(12));
            Assert.That(baseline.Diagnostics.IgnoredNonType0DefinitionCount, Is.EqualTo(3));
        }

        [TestCaseSource(nameof(CatalogRejectionCases))]
        public void InvalidCatalogVariantsRejectAtomically(int caseId)
        {
            var definitions = CreateInvalidDefinitions(caseId);
            var result = new Type0RouteMaskAssigner().Assign(growth, definitions);
            Assert.That(result.Status, Is.EqualTo(Type0RouteMaskAssignmentStatus.InvalidCatalog));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Assignments, Is.Empty);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Errors, Is.Not.Empty);
            Assert.That(result.RngDrawCount, Is.Zero);
            Assert.That(result.Diagnostics.SourceMutationCount, Is.Zero);
            Assert.That(result.Errors, Is.Ordered.Using<Type0RouteMaskAssignmentError>(Comparer<Type0RouteMaskAssignmentError>.Create(
                (left, right) => string.Compare(left.Code, right.Code, StringComparison.Ordinal))));
        }

        [TestCaseSource(nameof(PerCellCases))]
        public void EveryCellUsesExactSameRegionRequiredShape(int caseId)
        {
            var assignment = baseline.Assignments[caseId % baseline.Assignments.Count];
            var owned = new HashSet<int>(growth.Snapshot.Regions
                .Single(value => value.RegionId == assignment.RegionId)
                .Cells.Select(value => value.SectorIndex));
            var expected = new[]
            {
                owned.Contains(WorldGridIndex.GetLeftIndex(assignment.SectorIndex)),
                owned.Contains(WorldGridIndex.GetRightIndex(assignment.SectorIndex)),
                owned.Contains(WorldGridIndex.GetUpIndex(assignment.SectorIndex)),
                owned.Contains(WorldGridIndex.GetDownIndex(assignment.SectorIndex))
            };
            Assert.That(new[]
            {
                assignment.OpenMask.OpenLeft, assignment.OpenMask.OpenRight,
                assignment.OpenMask.OpenUp, assignment.OpenMask.OpenDown
            }, Is.EqualTo(expected));
            Assert.That(assignment.MaskId, Is.EqualTo(assignment.Mask.MaskId));
            Assert.That(assignment.OpenMask, Is.EqualTo(assignment.Mask.OpenMask));
            var source = growth.Snapshot.Cells.Single(value => value.SectorIndex == assignment.SectorIndex);
            Assert.That(assignment.RegionId, Is.EqualTo(source.RegionId));
            Assert.That(assignment.Sector, Is.EqualTo(source.Sector));
            Assert.That(assignment.Depth, Is.EqualTo(source.Depth));
            Assert.That(assignment.IsAttachmentCell, Is.EqualTo(source.IsAttachmentCell));
        }

        [TestCaseSource(nameof(BoundaryCases))]
        public void InternalEdgesAreReciprocalAndAllExternalBaseBoundariesStayClosed(int caseId)
        {
            switch (caseId % 4)
            {
                case 0:
                {
                    var assignment = baseline.Assignments[caseId % baseline.Assignments.Count];
                    foreach (var side in Sides())
                    {
                        var neighborIndex = Neighbor(assignment.SectorIndex, side[0], side[1]);
                        var neighbor = baseline.Assignments.FirstOrDefault(value =>
                            value.RegionId == assignment.RegionId && value.SectorIndex == neighborIndex);
                        Assert.That(IsOpen(assignment.OpenMask, side[0], side[1]), Is.EqualTo(neighbor != null));
                        if (neighbor != null)
                            Assert.That(IsOpen(neighbor.OpenMask, -side[0], -side[1]), Is.True);
                    }
                    break;
                }
                case 1:
                {
                    var region = growth.Snapshot.Regions[caseId % growth.Snapshot.Regions.Count];
                    var assignment = baseline.Assignments.Single(value =>
                        value.SectorIndex == region.Attachment.EntrySectorIndex);
                    Assert.That(IsOpen(
                        assignment.OpenMask,
                        -region.Attachment.EntrySideFromMandatoryDx,
                        -region.Attachment.EntrySideFromMandatoryDy), Is.False);
                    break;
                }
                case 2:
                {
                    foreach (var assignment in baseline.Assignments)
                    foreach (var side in Sides())
                    {
                        var neighborIndex = Neighbor(assignment.SectorIndex, side[0], side[1]);
                        var other = baseline.Assignments.FirstOrDefault(value => value.SectorIndex == neighborIndex);
                        if (other != null && other.RegionId != assignment.RegionId)
                            Assert.That(IsOpen(assignment.OpenMask, side[0], side[1]), Is.False);
                    }
                    break;
                }
                default:
                    Assert.That(baseline.Diagnostics.InternalUndirectedEdgeCount, Is.EqualTo(CountInternalEdges()));
                    Assert.That(baseline.Diagnostics.AttachmentBoundaryClosedCount, Is.EqualTo(12));
                    Assert.That(baseline.Diagnostics.MandatoryBoundaryBaseOpenCount, Is.Zero);
                    Assert.That(baseline.Diagnostics.ClosedCrossRegionAdjacencyCount, Is.EqualTo(CountCrossRegionEdges()));
                    break;
            }
        }

        [TestCaseSource(nameof(UnsupportedCases))]
        public void HorizontalThroughTopologyRejectsWithoutPartialPublication(int caseId)
        {
            var source = (caseId & 1) == 0
                ? definitionSet.RouteMasks.Values
                : definitionSet.RouteMasks.Values.Reverse();
            var result = new Type0RouteMaskAssigner().Assign(unsupportedGrowth, source);
            Assert.That(result.Status, Is.EqualTo(Type0RouteMaskAssignmentStatus.UnsupportedTopology));
            Assert.That(result.Assignments, Is.Empty);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.SourceRouteMaskCatalogDigest, Has.Length.EqualTo(64));
            Assert.That(result.Diagnostics.HorizontalThroughCount, Is.EqualTo(1));
            Assert.That(result.Diagnostics.UnsupportedRequiredMaskCount, Is.EqualTo(1));
            Assert.That(result.Errors.Any(value => value.Code == "HORIZONTAL_THROUGH_UNSUPPORTED"), Is.True);
            Assert.That(result.RngDrawCount, Is.Zero);
            Assert.That(result.Diagnostics.SourceMutationCount, Is.Zero);
        }

        [TestCaseSource(nameof(DigestCases))]
        public void DigestsAreCultureOrderAndServiceReuseIndependent(int caseId)
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = (caseId % 3) == 0
                    ? CultureInfo.GetCultureInfo("tr-TR")
                    : (caseId % 3) == 1
                        ? CultureInfo.GetCultureInfo("de-DE")
                        : CultureInfo.GetCultureInfo("en-US");
                var definitions = (caseId & 1) == 0
                    ? definitionSet.RouteMasks.Values
                    : definitionSet.RouteMasks.Values.Reverse();
                var assigner = new Type0RouteMaskAssigner();
                var result = assigner.Assign(growth, definitions);
                var reused = assigner.Assign(growth, definitions.Reverse());
                Assert.That(result.CanonicalDigest, Is.EqualTo(baseline.CanonicalDigest));
                Assert.That(reused.CanonicalDigest, Is.EqualTo(baseline.CanonicalDigest));
                Assert.That(result.SourceRouteMaskCatalogDigest, Is.EqualTo(baseline.SourceRouteMaskCatalogDigest));
                Assert.That(result.SourceGrowthDigest, Is.EqualTo(growth.CanonicalDigest));
                Assert.That(result.CanonicalDigest, Has.Length.EqualTo(64));
                Assert.That(result.CanonicalDigest.All(IsLowerHex), Is.True);
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [TestCaseSource(nameof(IntegrityCases))]
        public void SourceRngType4AndPhaseBoundaryIntegrityRemainFrozen(int caseId)
        {
            switch (caseId % 6)
            {
                case 0:
                    Assert.That(SourceSignature(), Is.EqualTo(sourceSignature));
                    break;
                case 1:
                    Assert.That(baseline.RngDrawCount, Is.Zero);
                    Assert.That(baseline.Diagnostics.SourceMutationCount, Is.Zero);
                    Assert.Throws<NotSupportedException>(() =>
                        ((IList<Type0RouteMaskAssignment>)baseline.Assignments).Add(baseline.Assignments[0]));
                    break;
                case 2:
                {
                    var bits = caseId / 6;
                    var left = (bits & 1) != 0;
                    var right = (bits & 2) != 0;
                    Assert.That(graph.MaskFamily.TryResolve(left, right, true, true, out var mask), Is.True);
                    Assert.That(mask.OpenUp && mask.OpenDown, Is.True);
                    Assert.That(mask.OpenLeft, Is.EqualTo(left));
                    Assert.That(mask.OpenRight, Is.EqualTo(right));
                    break;
                }
                case 3:
                    foreach (var name in new[]
                    {
                        "Type0RouteOpenMask", "Type0RouteMaskId", "Type0RouteMaskRecord",
                        "Type0RouteMaskAssignment", "Type0RouteMaskAssignmentDiagnostics",
                        "Type0RouteMaskAssignmentResult", "Type0RouteMaskAssigner",
                        "OptionalAccessClueId", "OptionalAccessAssignmentEnums", "OptionalAccessClue",
                        "OptionalAccessAssignmentSettings", "OptionalAccessAssignment",
                        "OptionalAccessAssignmentDiagnostics", "OptionalAccessAssignmentResult",
                        "OptionalAccessRuleAssigner"
                    })
                        Assert.That(typeof(Type0RouteMaskAssigner).Assembly.GetType(
                            "StarNight.Map.WorldGeneration.Generation." + name, false), Is.Not.Null);
                    break;
                case 4:
                    foreach (var name in new[]
                    {
                        "OptionalReturnConnection", "OptionalClueAssigner",
                        "OptionalRegionOverlayRenderer", "OptionalRegionValidationOverlayWindow",
                        "OptionalRegionOverlay", "GeneratedOptionalRegionCsvWriter"
                    })
                        Assert.That(typeof(Type0RouteMaskAssigner).Assembly.GetType(
                            "StarNight.Map.WorldGeneration.Generation." + name, false), Is.Null);
                    break;
                default:
                    Assert.That(baseline.SourceSnapshot, Is.SameAs(growth.Snapshot));
                    Assert.That(typeof(Type0RouteMaskAssigner).Assembly.GetReferencedAssemblies()
                        .Any(value => value.Name == "UnityEditor"), Is.False);
                    Assert.That(baseline.Diagnostics.AssignmentCount, Is.EqualTo(39));
                    Assert.That(baseline.Assignments.All(value => !value.OpenMask.HasHorizontalThrough), Is.True);
                    break;
            }
        }

        [Test]
        public void ApprovedFixturePublishesCanonicalSummary()
        {
            var usage = baseline.Assignments
                .GroupBy(value => value.MaskId.Value, StringComparer.Ordinal)
                .OrderBy(value => value.Key, StringComparer.Ordinal)
                .Select(value => value.Key + "=" + value.Count());
            TestContext.WriteLine(
                "MAP06_04_SUMMARY masks={0} regions={1} cells={2} assignments={3} internal={4} attachmentClosed={5} mandatoryOpen={6} crossClosed={7} usage={8} catalog={9} digest={10}",
                baseline.RegisteredMasks.Count, baseline.Diagnostics.SourceRegionCount,
                baseline.Diagnostics.SourceCellCount, baseline.Diagnostics.AssignmentCount,
                baseline.Diagnostics.InternalUndirectedEdgeCount,
                baseline.Diagnostics.AttachmentBoundaryClosedCount,
                baseline.Diagnostics.MandatoryBoundaryBaseOpenCount,
                baseline.Diagnostics.ClosedCrossRegionAdjacencyCount,
                string.Join(",", usage), baseline.SourceRouteMaskCatalogDigest, baseline.CanonicalDigest);
            Assert.That(growth.CanonicalDigest,
                Is.EqualTo("1f00f718bdb8d79fbf88923be0d81e0075987267c024cc624086ee6194998caa"));
            Assert.That(baseline.Diagnostics.SourceRegionCount, Is.EqualTo(12));
            Assert.That(baseline.Diagnostics.SourceCellCount, Is.EqualTo(39));
            Assert.That(baseline.Diagnostics.AssignmentCount, Is.EqualTo(39));
            Assert.That(baseline.Diagnostics.AttachmentBoundaryClosedCount, Is.EqualTo(12));
            Assert.That(baseline.Diagnostics.MandatoryBoundaryBaseOpenCount, Is.Zero);
            Assert.That(baseline.Diagnostics.HorizontalThroughCount, Is.Zero);
            Assert.That(baseline.Diagnostics.UnsupportedRequiredMaskCount, Is.Zero);
        }

        private IEnumerable<SectorRouteMaskDefinition> CreateInvalidDefinitions(int caseId)
        {
            var kind = caseId % 8;
            var ordinal = (caseId / 8) % 5;
            if (kind == 1)
            {
                var list = definitionSet.RouteMasks.Values.ToList();
                list.Add(definitionSet.RouteMasks[ExpectedIds[ordinal]]);
                return list;
            }

            var rows = CreateValidRows();
            switch (kind)
            {
                case 0:
                    rows.RemoveAt(ordinal);
                    break;
                case 2:
                    rows[ordinal][8] = "0";
                    break;
                case 3:
                    rows.Add(new[] { "ROUTE_T0_UNEXPECTED_" + ordinal, "0", "0", "0", "0", "0", "0", "unexpected", "1" });
                    break;
                case 4:
                    rows[ordinal][2] = rows[ordinal][2] == "1" ? "0" : "1";
                    if (rows[ordinal][2] == "1" && rows[ordinal][3] == "1") rows[ordinal][3] = "0";
                    break;
                case 5:
                    rows[ordinal][6] = "1";
                    break;
                case 6:
                    rows[ordinal][1] = "1";
                    break;
                case 7:
                    rows[ordinal][2] = "1";
                    rows[ordinal][3] = "1";
                    break;
            }
            return BuildDefinitionSet(rows).RouteMasks.Values;
        }

        private static List<string[]> CreateValidRows()
        {
            return new List<string[]>
            {
                Row("ROUTE_T0_NONE", 0, false, false, false, false, false),
                Row("ROUTE_T0_L", 0, true, false, false, false, false),
                Row("ROUTE_T0_R", 0, false, true, false, false, false),
                Row("ROUTE_T0_U", 0, false, false, true, false, false),
                Row("ROUTE_T0_D", 0, false, false, false, true, false),
                Row("ROUTE_T0_LU", 0, true, false, true, false, false),
                Row("ROUTE_T0_LD", 0, true, false, false, true, false),
                Row("ROUTE_T0_RU", 0, false, true, true, false, false),
                Row("ROUTE_T0_RD", 0, false, true, false, true, false),
                Row("ROUTE_T0_UD", 0, false, false, true, true, false),
                Row("ROUTE_T0_LUD", 0, true, false, true, true, false),
                Row("ROUTE_T0_RUD", 0, false, true, true, true, false),
                Row("ROUTE_T1_LR", 1, true, true, false, false, true),
                Row("ROUTE_T2_LRD", 2, true, true, false, true, true),
                Row("ROUTE_T3_LRU", 3, true, true, true, false, true)
            };
        }

        private static string[] Row(
            string id, int routeType, bool left, bool right, bool up, bool down, bool mandatoryAllowed)
        {
            return new[]
            {
                id, routeType.ToString(CultureInfo.InvariantCulture), Bool(left), Bool(right),
                Bool(up), Bool(down), Bool(mandatoryAllowed), "DESC_" + id, "1"
            };
        }

        private static string Bool(bool value)
        {
            return value ? "1" : "0";
        }

        private static WorldRouteDefinitionSet BuildDefinitionSet(IReadOnlyList<string[]> routeRows)
        {
            var testType = typeof(WorldRouteDefinitionBuilderTests);
            var flags = BindingFlags.Static | BindingFlags.NonPublic;
            var sources = (List<WorldRouteDefinitionSource>)testType
                .GetMethod("StandardSources", flags).Invoke(null, null);
            var specs = (IEnumerable)testType.GetField("Specs", flags).GetValue(null);
            object routeSpec = null;
            foreach (var spec in specs)
            {
                var name = (string)spec.GetType().GetProperty("FileName").GetValue(spec, null);
                if (name == "sector_route_masks.csv") routeSpec = spec;
            }
            Assert.That(routeSpec, Is.Not.Null);
            var buildSource = testType.GetMethod("BuildSource", flags);
            var replacement = (WorldRouteDefinitionSource)buildSource.Invoke(
                null, new object[] { routeSpec, routeRows, true });
            sources.RemoveAll(value => value.FileName == "sector_route_masks.csv");
            sources.Add(replacement);
            var result = new WorldRouteDefinitionBuilder().Build(sources);
            Assert.That(result.Success, Is.True, string.Join("\n", result.Errors.Select(value => value.Message)));
            return result.DefinitionSet;
        }

        private OptionalRegionGrowthResult CreateHorizontalThroughGrowth()
        {
            var regionId = new OptionalRegionId("SYNTH_REGION");
            var attachment = new OptionalRegionAttachment(
                regionId, 0, 1, WorldGridIndex.ToCoordinate(1), graph.Nodes[0].NodeId,
                14, WorldGridIndex.ToCoordinate(14), 0, 1, new OptionalRegionDepth(1));
            var cells = new[]
            {
                new OptionalRegionCell(regionId, 14, WorldGridIndex.ToCoordinate(14), new OptionalRegionDepth(1), true, false),
                new OptionalRegionCell(regionId, 13, WorldGridIndex.ToCoordinate(13), new OptionalRegionDepth(2), false, false),
                new OptionalRegionCell(regionId, 15, WorldGridIndex.ToCoordinate(15), new OptionalRegionDepth(2), false, false)
            };
            var region = new OptionalRegion(
                regionId, attachment, OptionalRegionAccessRule.Basic, OptionalRewardTier.None,
                OptionalReturnPolicy.BacktrackToAttachment, cells, new OptionalRegionDepth(2));
            var mandatory = new List<int> { 1 };
            mandatory.AddRange(Enumerable.Range(50, 46));
            var snapshot = new OptionalRegionSnapshot(
                new[] { region }, cells, mandatory, 47, 96, 47, "SYNTH_GRAPH");
            var diagnostics = new OptionalRegionGrowthDiagnostics(
                1, 1, 1, 0, 0, 3,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 1, 0, 0, Array.Empty<string>());
            var settings = new OptionalRegionGrowthSettings(
                1, 3, new[] { new OptionalRegionDepth(2) });
            return new OptionalRegionGrowthResult(
                snapshot, diagnostics, "SYNTH_ATTACHMENT", "SYNTH_GRAPH", settings);
        }

        private int CountInternalEdges()
        {
            var count = 0;
            foreach (var region in growth.Snapshot.Regions)
            {
                var owned = new HashSet<int>(region.Cells.Select(value => value.SectorIndex));
                foreach (var cell in region.Cells)
                {
                    if (owned.Contains(WorldGridIndex.GetRightIndex(cell.SectorIndex))) count++;
                    if (owned.Contains(WorldGridIndex.GetUpIndex(cell.SectorIndex))) count++;
                }
            }
            return count;
        }

        private int CountCrossRegionEdges()
        {
            var cells = growth.Snapshot.Cells.ToDictionary(value => value.SectorIndex, value => value);
            var count = 0;
            foreach (var cell in growth.Snapshot.Cells)
            {
                foreach (var neighborIndex in new[]
                {
                    WorldGridIndex.GetRightIndex(cell.SectorIndex),
                    WorldGridIndex.GetUpIndex(cell.SectorIndex)
                })
                {
                    if (neighborIndex >= 0 && cells.TryGetValue(neighborIndex, out var neighbor) &&
                        neighbor.RegionId != cell.RegionId) count++;
                }
            }
            return count;
        }

        private string SourceSignature()
        {
            return growth.CanonicalDigest + "|" +
                   string.Join(",", growth.Snapshot.Cells.Select(value =>
                       value.RegionId.Value + ":" + value.SectorIndex + ":" + value.Depth.Value)) + "|" +
                   string.Join(",", definitionSet.RouteMasks.Values.Select(value =>
                       value.RouteMaskId + ":" + value.RouteType + ":" + value.Active)) + "|" +
                   graph.NodeCount + "/" + graph.DirectedEdgeCount + "/" + graph.UndirectedEdgeCount + "/" + graph.CellCount;
        }

        private static int CountBits(int bits)
        {
            return ((bits & 1) != 0 ? 1 : 0) + ((bits & 2) != 0 ? 1 : 0) +
                   ((bits & 4) != 0 ? 1 : 0) + ((bits & 8) != 0 ? 1 : 0);
        }

        private static string BitsForId(string id)
        {
            var suffix = id.Substring("ROUTE_T0_".Length);
            if (suffix == "NONE") suffix = string.Empty;
            return string.Concat(
                suffix.Contains("L") ? "1" : "0", suffix.Contains("R") ? "1" : "0",
                suffix.Contains("U") ? "1" : "0", suffix.Contains("D") ? "1" : "0");
        }

        private static string Bits(Type0RouteOpenMask mask)
        {
            return string.Concat(
                mask.OpenLeft ? "1" : "0", mask.OpenRight ? "1" : "0",
                mask.OpenUp ? "1" : "0", mask.OpenDown ? "1" : "0");
        }

        private static int[][] Sides()
        {
            return new[]
            {
                new[] { -1, 0 }, new[] { 1, 0 }, new[] { 0, 1 }, new[] { 0, -1 }
            };
        }

        private static int Neighbor(int sectorIndex, int dx, int dy)
        {
            if (dx == -1) return WorldGridIndex.GetLeftIndex(sectorIndex);
            if (dx == 1) return WorldGridIndex.GetRightIndex(sectorIndex);
            if (dy == 1) return WorldGridIndex.GetUpIndex(sectorIndex);
            return WorldGridIndex.GetDownIndex(sectorIndex);
        }

        private static bool IsOpen(Type0RouteOpenMask mask, int dx, int dy)
        {
            if (dx == -1) return mask.OpenLeft;
            if (dx == 1) return mask.OpenRight;
            if (dy == 1) return mask.OpenUp;
            return mask.OpenDown;
        }

        private static bool IsLowerHex(char value)
        {
            return (value >= '0' && value <= '9') || (value >= 'a' && value <= 'f');
        }

        private static string FormatErrors(Type0RouteMaskAssignmentResult result)
        {
            return string.Join("\n", result.Errors.Select(value => value.Code + ": " + value.Message));
        }

        private static T GetField<T>(object target, Type type, string name)
        {
            return (T)type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }
    }
}
