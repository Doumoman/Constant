using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    [Category("MAP06_08")]
    public sealed class InactiveBufferAssignerTests
    {
        private static readonly int[][] Directions =
        {
            new[] { -1, 0 }, new[] { 1, 0 }, new[] { 0, 1 }, new[] { 0, -1 }
        };

        private GeneratedWorldData world;
        private SiteReservationSnapshot site;
        private BiomePatchValidationPublication biome;
        private MandatoryRouteGraph graph;
        private MandatoryRouteValidationReport report;
        private Type0RouteMaskAssignmentResult type0;
        private OptionalReturnPolicyResult returns;
        private string graphDigest;
        private InactiveBufferAssignmentResult baseline;
        private HashSet<int> siteSectors;
        private HashSet<int> mandatorySectors;
        private HashSet<int> type0Sectors;
        private HashSet<int> protectedSectors;
        private string sourceSignature;

        public static IEnumerable<int> EnumSettingsCases => Enumerable.Range(0, 30);
        public static IEnumerable<int> WorldSourceCases => Enumerable.Range(0, 34);
        public static IEnumerable<int> OwnershipCases => Enumerable.Range(0, 38);
        public static IEnumerable<int> CompletenessCases => Enumerable.Range(0, 32);
        public static IEnumerable<int> ClassificationCases => Enumerable.Range(0, 30);
        public static IEnumerable<int> NeighborCases => Enumerable.Range(0, 30);
        public static IEnumerable<int> OpenEdgeCases => Enumerable.Range(0, 28);
        public static IEnumerable<int> DeterminismCases => Enumerable.Range(0, 26);
        public static IEnumerable<int> IntegrityCases => Enumerable.Range(0, 32);

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var fixture = new OptionalReturnPolicyResolverTests();
            fixture.OneTimeSetUp();
            type0 = GetField<Type0RouteMaskAssignmentResult>(fixture, "type0");
            returns = GetField<OptionalReturnPolicyResult>(fixture, "baseline");
            graph = GetField<MandatoryRouteGraph>(fixture, "graph");
            var validation = new MandatoryRouteGraphValidator().Validate(graph);
            Assert.That(validation.Status, Is.EqualTo(MandatoryRouteValidationStatus.Completed));
            Assert.That(validation.Succeeded, Is.True);
            report = validation.Report;
            world = graph.RouteStampedWorld;
            site = graph.SourceTerminalSet.SourceSiteSnapshot;
            biome = graph.SourceTerminalSet.SourceBiomePublication;
            graphDigest = type0.SourceSnapshot.SourceMandatoryGraphDigest;
            siteSectors = new HashSet<int>(site.Sectors.Where(value => value.IsReserved).Select(value => value.Index));
            mandatorySectors = new HashSet<int>(graph.Cells.Select(value => value.SectorIndex));
            type0Sectors = new HashSet<int>(type0.Assignments.Select(value => value.SectorIndex));
            protectedSectors = new HashSet<int>(siteSectors);
            protectedSectors.UnionWith(mandatorySectors);
            protectedSectors.UnionWith(type0Sectors);
            TestContext.WriteLine("MAP06_08_SOURCE site={0} mandatory={1} type0={2} siteMandatoryOverlap={3}",
                string.Join(",", siteSectors.OrderBy(value => value)),
                string.Join(",", mandatorySectors.OrderBy(value => value)),
                string.Join(",", type0Sectors.OrderBy(value => value)),
                string.Join(",", siteSectors.Intersect(mandatorySectors).OrderBy(value => value)));
            sourceSignature = SourceSignature();
            baseline = Assign();

            Assert.That(type0.IsSuccess && returns.IsSuccess, Is.True);
            Assert.That(baseline.IsSuccess, Is.True, FormatErrors(baseline));
            Assert.That(baseline.Assignments, Has.Count.EqualTo(78));
        }

        [TestCaseSource(nameof(EnumSettingsCases))]
        public void EnumSettingsAssignmentResultAndCollectionsAreImmutable(int caseId)
        {
            if (caseId < 15)
            {
                var settings = ApprovedSettings();
                Assert.That(settings.RequireFullWorldAccounting, Is.True);
                Assert.That(settings.RequireClosedInactiveBoundaries, Is.True);
                Assert.That(settings.ClassifyClaimAdjacentAsDecorativeBoundary, Is.True);
                Assert.That(Enum.GetValues(typeof(InactiveBufferAssignmentStatus)), Has.Length.EqualTo(6));
                Assert.That(Enum.GetValues(typeof(InactiveBufferAssignmentErrorCode)), Has.Length.EqualTo(15));
                Assert.That(Enum.GetValues(typeof(InactiveBufferKind)), Has.Length.EqualTo(2));
                Assert.That(typeof(InactiveBufferAssignmentSettings).GetProperties().All(value => !value.CanWrite), Is.True);
                Assert.That(typeof(InactiveBufferAssignment).GetProperties().All(value => !value.CanWrite), Is.True);
                Assert.That(typeof(InactiveBufferAssignmentDiagnostics).GetProperties().All(value => !value.CanWrite), Is.True);
                Assert.That(typeof(InactiveBufferAssignmentResult).GetProperties().All(value => !value.CanWrite), Is.True);
                return;
            }

            switch (caseId % 5)
            {
                case 0:
                    Assert.That(() => new InactiveBufferAssignmentSettings(false, true, true), Throws.TypeOf<ArgumentException>());
                    break;
                case 1:
                    Assert.That(() => new InactiveBufferAssignmentSettings(true, false, true), Throws.TypeOf<ArgumentException>());
                    break;
                case 2:
                    Assert.That(() => new InactiveBufferAssignmentSettings(true, true, false), Throws.TypeOf<ArgumentException>());
                    break;
                case 3:
                    Assert.That(() => ((IList<InactiveBufferAssignment>)baseline.Assignments).Add(baseline.Assignments[0]),
                        Throws.TypeOf<NotSupportedException>());
                    break;
                default:
                    var assignment = baseline.Assignments[caseId % baseline.Assignments.Count];
                    Assert.That(() => ((IList<int>)assignment.ProtectedNeighborSectorIndices).Add(0),
                        Throws.TypeOf<NotSupportedException>());
                    Assert.That(() => ((IList<int>)assignment.InactiveNeighborSectorIndices).Add(0),
                        Throws.TypeOf<NotSupportedException>());
                    break;
            }
        }

        [TestCaseSource(nameof(WorldSourceCases))]
        public void WorldSiteBiomeGraphType0AndReturnSourceChainIsExact(int caseId)
        {
            var index = caseId % WorldGenConstants.SectorCount;
            Assert.That(world.Cells, Has.Count.EqualTo(169));
            Assert.That(world.Cells[index].Index, Is.EqualTo(index));
            Assert.That(world.Cells[index].Coordinate, Is.EqualTo(WorldGridIndex.ToCoordinate(index)));
            Assert.That(site.Sectors, Has.Count.EqualTo(169));
            Assert.That(biome.WorldWithBiomeAssignments.Cells, Has.Count.EqualTo(169));
            Assert.That(report.IsValid, Is.True);
            Assert.That(report.SourceGraph, Is.SameAs(graph));
            Assert.That(report.SourceWorld, Is.SameAs(world));
            Assert.That(graph.NodeCount, Is.EqualTo(47));
            Assert.That(graph.DirectedEdgeCount, Is.EqualTo(96));
            Assert.That(graph.UndirectedEdgeCount, Is.EqualTo(48));
            Assert.That(graph.CellCount, Is.EqualTo(47));
            Assert.That(type0.Status, Is.EqualTo(Type0RouteMaskAssignmentStatus.Completed));
            Assert.That(returns.Status, Is.EqualTo(OptionalReturnPolicyResolutionStatus.Completed));
            Assert.That(returns.SourceType0AssignmentDigest, Is.EqualTo(type0.CanonicalDigest));
            Assert.That(returns.SourceGrowthDigest, Is.EqualTo(type0.SourceGrowthDigest));
            Assert.That(graphDigest, Is.EqualTo(type0.SourceSnapshot.SourceMandatoryGraphDigest));
            Assert.That(baseline.SourceMandatoryGraphDigest, Is.EqualTo(graphDigest));
            Assert.That(baseline.SourceType0AssignmentDigest, Is.EqualTo(type0.CanonicalDigest));
            Assert.That(baseline.SourceGrowthDigest, Is.EqualTo(type0.SourceGrowthDigest));
            Assert.That(baseline.SourceReturnPolicyDigest, Is.EqualTo(returns.CanonicalDigest));
        }

        [TestCaseSource(nameof(OwnershipCases))]
        public void ApprovedAdapterOverlapAndExclusiveProtectedOwnershipAreFullyAccounted(int caseId)
        {
            var approvedOverlap = siteSectors.Intersect(mandatorySectors).OrderBy(value => value).ToArray();
            Assert.That(siteSectors, Has.Count.EqualTo(8));
            Assert.That(mandatorySectors, Has.Count.EqualTo(47));
            Assert.That(type0Sectors, Has.Count.EqualTo(39));
            Assert.That(approvedOverlap, Is.EqualTo(new[] { 0, 28, 106 }));
            Assert.That(graph.Cells.Where(value => approvedOverlap.Contains(value.SectorIndex))
                .All(value => value.IsApprovedReservedAdapter), Is.True);
            Assert.That(siteSectors.Intersect(type0Sectors), Is.Empty);
            Assert.That(mandatorySectors.Intersect(type0Sectors), Is.Empty);
            Assert.That(protectedSectors, Has.Count.EqualTo(91));
            Assert.That(baseline.Diagnostics.ReservedSiteSectorCount, Is.EqualTo(siteSectors.Count));
            Assert.That(baseline.Diagnostics.MandatoryRouteCellCount, Is.EqualTo(mandatorySectors.Count));
            Assert.That(baseline.Diagnostics.MandatoryExclusiveSectorCount, Is.EqualTo(44));
            Assert.That(baseline.Diagnostics.Type0CellCount, Is.EqualTo(type0Sectors.Count));
            Assert.That(baseline.Diagnostics.SiteMandatoryOverlapCount, Is.EqualTo(3));
            Assert.That(baseline.Diagnostics.ApprovedReservedAdapterOverlapCount, Is.EqualTo(3));
            Assert.That(baseline.Diagnostics.ProtectedUnionCount, Is.EqualTo(protectedSectors.Count));
            Assert.That(protectedSectors.Contains(caseId % 169), Is.EqualTo(
                siteSectors.Contains(caseId % 169) || mandatorySectors.Contains(caseId % 169) ||
                type0Sectors.Contains(caseId % 169)));
            Assert.That(baseline.Diagnostics.IllegalOwnershipOverlapCount, Is.Zero);
            Assert.That(baseline.Diagnostics.DuplicateSectorCount, Is.Zero);
        }

        [TestCaseSource(nameof(CompletenessCases))]
        public void EveryUnclaimedSectorHasOneCanonicalInactiveAssignment(int caseId)
        {
            var expected = Enumerable.Range(0, 169).Where(value => !protectedSectors.Contains(value)).ToArray();
            Assert.That(expected, Has.Length.EqualTo(78));
            Assert.That(baseline.Assignments.Select(value => value.SectorIndex), Is.EqualTo(expected));
            var assignment = baseline.Assignments[caseId % baseline.Assignments.Count];
            Assert.That(protectedSectors.Contains(assignment.SectorIndex), Is.False);
            Assert.That(assignment.Coord, Is.EqualTo(WorldGridIndex.ToCoordinate(assignment.SectorIndex)));
            Assert.That(assignment.Role, Is.EqualTo(GeneratedSectorRole.InactiveBuffer));
            Assert.That(baseline.Diagnostics.AssignmentCount, Is.EqualTo(78));
            Assert.That(baseline.Diagnostics.UnassignedSectorCount, Is.Zero);
            Assert.That(baseline.Diagnostics.WorldSectorCount, Is.EqualTo(169));
            Assert.That(baseline.Diagnostics.ProtectedUnionCount + baseline.Diagnostics.AssignmentCount,
                Is.EqualTo(169));
        }

        [TestCaseSource(nameof(ClassificationCases))]
        public void DecorativeBoundaryIffProtectedCardinalNeighborExists(int caseId)
        {
            var assignment = baseline.Assignments[caseId % baseline.Assignments.Count];
            var expectedProtected = NeighborIndices(assignment.SectorIndex)
                .Where(value => value >= 0 && protectedSectors.Contains(value)).ToArray();
            var expectedKind = expectedProtected.Length == 0
                ? InactiveBufferKind.InteriorInactive
                : InactiveBufferKind.DecorativeBoundary;
            Assert.That(assignment.Kind, Is.EqualTo(expectedKind));
            Assert.That(assignment.ProtectedNeighborSectorIndices, Is.EqualTo(expectedProtected));

            var decorativeOracle = baseline.Assignments.Count(value =>
                NeighborIndices(value.SectorIndex).Any(index => index >= 0 && protectedSectors.Contains(index)));
            Assert.That(baseline.Diagnostics.DecorativeBoundaryCount, Is.EqualTo(decorativeOracle));
            Assert.That(baseline.Diagnostics.InteriorInactiveCount,
                Is.EqualTo(78 - decorativeOracle));
        }

        [TestCaseSource(nameof(NeighborCases))]
        public void NeighborListsWorldEdgesAndTopologyCountersMatchIndependentOracle(int caseId)
        {
            var assignment = baseline.Assignments[caseId % baseline.Assignments.Count];
            var expectedInactive = NeighborIndices(assignment.SectorIndex)
                .Where(value => value >= 0 && !protectedSectors.Contains(value)).ToArray();
            var expectedProtected = NeighborIndices(assignment.SectorIndex)
                .Where(value => value >= 0 && protectedSectors.Contains(value)).ToArray();
            Assert.That(assignment.InactiveNeighborSectorIndices, Is.EqualTo(expectedInactive));
            Assert.That(assignment.ProtectedNeighborSectorIndices, Is.EqualTo(expectedProtected));
            var coord = WorldGridIndex.ToCoordinate(assignment.SectorIndex);
            Assert.That(assignment.TouchesWorldEdge, Is.EqualTo(
                coord.X == 0 || coord.X == 12 || coord.Y == 0 || coord.Y == 12));

            var protectedEdges = baseline.Assignments.Sum(value => value.ProtectedNeighborSectorIndices.Count);
            var inactiveReferences = baseline.Assignments.Sum(value => value.InactiveNeighborSectorIndices.Count);
            var worldEdges = baseline.Assignments.Count(value => value.TouchesWorldEdge);
            Assert.That(baseline.Diagnostics.ProtectedToInactiveCardinalEdgeCount, Is.EqualTo(protectedEdges));
            Assert.That(baseline.Diagnostics.InactiveToInactiveUndirectedEdgeCount, Is.EqualTo(inactiveReferences / 2));
            Assert.That(baseline.Diagnostics.WorldEdgeInactiveCount, Is.EqualTo(worldEdges));
        }

        [TestCaseSource(nameof(OpenEdgeCases))]
        public void MandatoryOrType0OpenEdgeToInactiveFailsAtomically(int caseId)
        {
            var invalidType0 = CloneType0WithOpenEdgeToInactive(caseId);
            var result = new InactiveBufferAssigner().Assign(
                world, site, biome, graph, report, invalidType0, returns,
                graphDigest, ApprovedSettings());
            Assert.That(result.Status, Is.EqualTo(InactiveBufferAssignmentStatus.InvalidTopology), FormatErrors(result));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Assignments, Is.Empty);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Errors.Any(value => value.Code == InactiveBufferAssignmentErrorCode.OpenEdgeToInactive), Is.True);
            Assert.That(result.Diagnostics.AssignmentCount, Is.Zero);
            Assert.That(result.Diagnostics.UnassignedSectorCount, Is.Zero);
            Assert.That(result.Diagnostics.RngDrawCount, Is.Zero);
            Assert.That(result.Diagnostics.SourceMutationCount, Is.Zero);
        }

        [TestCaseSource(nameof(DeterminismCases))]
        public void CanonicalDigestIsCultureOrderAndServiceReuseIndependent(int caseId)
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = (caseId & 1) == 0
                    ? CultureInfo.GetCultureInfo("en-US")
                    : CultureInfo.GetCultureInfo("tr-TR");
                var service = new InactiveBufferAssigner();
                var first = service.Assign(world, site, biome, graph, report, type0, returns, graphDigest, ApprovedSettings());
                var second = service.Assign(world, site, biome, graph, report, type0, returns, graphDigest, ApprovedSettings());
                var fresh = Assign();
                Assert.That(first.CanonicalDigest, Is.EqualTo(baseline.CanonicalDigest));
                Assert.That(second.CanonicalDigest, Is.EqualTo(baseline.CanonicalDigest));
                Assert.That(fresh.CanonicalDigest, Is.EqualTo(baseline.CanonicalDigest));
                Assert.That(Signature(first), Is.EqualTo(Signature(baseline)));
                Assert.That(first.CanonicalDigest, Has.Length.EqualTo(64));
                Assert.That(first.CanonicalDigest.All(IsLowerHex), Is.True);
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [TestCaseSource(nameof(IntegrityCases))]
        public void SourceMutationRngType4AndPhaseBoundaryRemainFrozen(int caseId)
        {
            Assert.That(SourceSignature(), Is.EqualTo(sourceSignature));
            Assert.That(baseline.Diagnostics.RngDrawCount, Is.Zero);
            Assert.That(baseline.Diagnostics.SourceMutationCount, Is.Zero);
            Assert.That(baseline.RngDrawCount, Is.Zero);
            Assert.That(type0.Diagnostics.AttachmentBoundaryClosedCount, Is.EqualTo(12));
            Assert.That(type0.Diagnostics.MandatoryBoundaryBaseOpenCount, Is.Zero);
            Assert.That(returns.Diagnostics.ReturnableCellCount, Is.EqualTo(39));
            Assert.That(returns.Diagnostics.NonReturnableCellCount, Is.Zero);
            foreach (var node in graph.Nodes.Where(value =>
                         value.RouteMaskId.IndexOf("TYPE4", StringComparison.Ordinal) >= 0))
            {
                Assert.That(node.OpenUp, Is.True);
                Assert.That(node.OpenDown, Is.True);
            }

            var runtime = typeof(InactiveBufferAssigner).Assembly;
            foreach (var name in new[]
                     {
                         "InactiveBufferAssignmentStatus", "InactiveBufferAssignmentErrorCode",
                         "InactiveBufferKind", "InactiveBufferAssignmentSettings",
                         "InactiveBufferAssignment", "InactiveBufferAssignmentDiagnostics",
                         "InactiveBufferAssignmentError", "InactiveBufferAssignmentResult",
                         "InactiveBufferAssigner"
                     })
                Assert.That(runtime.GetType("StarNight.Map.WorldGeneration.Generation." + name, false), Is.Not.Null);
            foreach (var name in new[]
                     {
                         "OptionalRegionValidationOverlayWindow", "OptionalRegionOverlayRenderer",
                         "OptionalRegionOverlay", "GeneratedOptionalRegionCsvWriter"
                     })
                Assert.That(runtime.GetType("StarNight.Map.WorldGeneration.Generation." + name, false), Is.Null);
            Assert.That(typeof(InactiveBufferAssignerTests).Name, Is.EqualTo("InactiveBufferAssignerTests"));
            Assert.That(SourceSignature(), Is.EqualTo(sourceSignature));
        }

        [Test]
        public void ApprovedFixturePublishesCanonicalInactiveSummary()
        {
            foreach (var assignment in baseline.Assignments)
            {
                TestContext.WriteLine(
                    "MAP06_08_ASSIGNMENT sector={0} coord={1},{2} role=INACTIVE_BUFFER kind={3} protected={4} inactive={5} worldEdge={6}",
                    assignment.SectorIndex, assignment.Coord.X, assignment.Coord.Y, assignment.Kind,
                    string.Join(",", assignment.ProtectedNeighborSectorIndices),
                    string.Join(",", assignment.InactiveNeighborSectorIndices),
                    assignment.TouchesWorldEdge ? 1 : 0);
            }
            var d = baseline.Diagnostics;
            TestContext.WriteLine(
                "MAP06_08_SUMMARY world={0} reservations={1} source={2}/{3}/{4} overlap={5}/{6} exclusive={7}/{8}/{9}/{10} assignments={11} decorative={12} interior={13} worldEdge={14} protectedEdges={15} inactiveEdges={16} unassigned={17} illegalOverlap={18} duplicate={19} openToInactive={20} rng={21} mutation={22} mandatory={23} type0={24} growth={25} return={26} digest={27}",
                d.WorldSectorCount, d.SiteReservationCount, d.ReservedSiteSectorCount,
                d.MandatoryRouteCellCount, d.Type0CellCount, d.SiteMandatoryOverlapCount,
                d.ApprovedReservedAdapterOverlapCount, d.ReservedSiteSectorCount,
                d.MandatoryExclusiveSectorCount, d.Type0CellCount, d.ProtectedUnionCount,
                d.AssignmentCount, d.DecorativeBoundaryCount, d.InteriorInactiveCount,
                d.WorldEdgeInactiveCount, d.ProtectedToInactiveCardinalEdgeCount,
                d.InactiveToInactiveUndirectedEdgeCount, d.UnassignedSectorCount,
                d.IllegalOwnershipOverlapCount, d.DuplicateSectorCount, d.OpenEdgeToInactiveCount,
                d.RngDrawCount, d.SourceMutationCount, baseline.SourceMandatoryGraphDigest,
                baseline.SourceType0AssignmentDigest, baseline.SourceGrowthDigest,
                baseline.SourceReturnPolicyDigest, baseline.CanonicalDigest);

            Assert.That(new[]
            {
                d.WorldSectorCount, d.ReservedSiteSectorCount, d.MandatoryRouteCellCount,
                d.MandatoryExclusiveSectorCount, d.Type0CellCount,
                d.SiteMandatoryOverlapCount, d.ApprovedReservedAdapterOverlapCount,
                d.ProtectedUnionCount, d.AssignmentCount,
                d.UnassignedSectorCount, d.IllegalOwnershipOverlapCount, d.DuplicateSectorCount,
                d.OpenEdgeToInactiveCount, d.RngDrawCount, d.SourceMutationCount
            }, Is.EqualTo(new[] { 169, 8, 47, 44, 39, 3, 3, 91, 78, 0, 0, 0, 0, 0, 0 }));
        }

        private InactiveBufferAssignmentResult Assign()
        {
            return new InactiveBufferAssigner().Assign(
                world, site, biome, graph, report, type0, returns,
                graphDigest, ApprovedSettings());
        }

        private static InactiveBufferAssignmentSettings ApprovedSettings()
        {
            return new InactiveBufferAssignmentSettings(true, true, true);
        }

        private Type0RouteMaskAssignmentResult CloneType0WithOpenEdgeToInactive(int caseId)
        {
            var candidates = new List<OpenCandidate>();
            foreach (var assignment in type0.Assignments)
            {
                for (var direction = 0; direction < Directions.Length; direction++)
                {
                    var neighbor = NeighborIndices(assignment.SectorIndex)[direction];
                    if (neighbor < 0 || protectedSectors.Contains(neighbor) || IsOpen(assignment.OpenMask, direction)) continue;
                    if (direction == 0 && assignment.OpenMask.OpenRight) continue;
                    if (direction == 1 && assignment.OpenMask.OpenLeft) continue;
                    candidates.Add(new OpenCandidate(assignment, direction));
                }
            }
            Assert.That(candidates, Is.Not.Empty);
            var candidate = candidates[caseId % candidates.Count];
            var target = candidate.Assignment;
            var mask = new Type0RouteOpenMask(
                target.OpenMask.OpenLeft || candidate.Direction == 0,
                target.OpenMask.OpenRight || candidate.Direction == 1,
                target.OpenMask.OpenUp || candidate.Direction == 2,
                target.OpenMask.OpenDown || candidate.Direction == 3);
            var record = InvokeInternal<Type0RouteMaskRecord>(
                target.Mask.MaskId, target.Mask.RouteType, mask,
                target.Mask.MandatoryAllowed, target.Mask.Active,
                target.Mask.DescriptionKo, target.Mask.SourceDefinition);
            var sourceCell = type0.SourceSnapshot.Cells.Single(value => value.SectorIndex == target.SectorIndex);
            var replacement = InvokeInternal<Type0RouteMaskAssignment>(sourceCell, record);
            var assignments = type0.Assignments.Select(value =>
                value.SectorIndex == target.SectorIndex ? replacement : value).ToList();
            return InvokeInternal<Type0RouteMaskAssignmentResult>(
                Type0RouteMaskAssignmentStatus.Completed, type0.SourceSnapshot,
                type0.RegisteredMasks, assignments, type0.Diagnostics,
                Array.Empty<Type0RouteMaskAssignmentError>(), type0.SourceGrowthDigest,
                type0.SourceRouteMaskCatalogDigest, type0.CanonicalDigest);
        }

        private static int[] NeighborIndices(int sectorIndex)
        {
            return new[]
            {
                WorldGridIndex.GetLeftIndex(sectorIndex),
                WorldGridIndex.GetRightIndex(sectorIndex),
                WorldGridIndex.GetUpIndex(sectorIndex),
                WorldGridIndex.GetDownIndex(sectorIndex)
            };
        }

        private static bool IsOpen(Type0RouteOpenMask mask, int direction)
        {
            if (direction == 0) return mask.OpenLeft;
            if (direction == 1) return mask.OpenRight;
            if (direction == 2) return mask.OpenUp;
            return mask.OpenDown;
        }

        private string SourceSignature()
        {
            return world.Seed + "|" + site.Seed + "|" + graph.NodeCount + "|" + graph.DirectedEdgeCount + "|" +
                   type0.CanonicalDigest + "|" + returns.CanonicalDigest + "|" +
                   string.Join(",", graph.Cells.Select(value => value.SectorIndex + ":" + value.RouteMaskId)) + "|" +
                   string.Join(",", type0.Assignments.Select(value => value.SectorIndex + ":" + value.OpenMask));
        }

        private static string Signature(InactiveBufferAssignmentResult result)
        {
            return string.Join(";", result.Assignments.Select(value =>
                value.SectorIndex + "|" + value.Kind + "|" +
                string.Join(",", value.ProtectedNeighborSectorIndices) + "|" +
                string.Join(",", value.InactiveNeighborSectorIndices) + "|" +
                (value.TouchesWorldEdge ? "1" : "0")));
        }

        private static string FormatErrors(InactiveBufferAssignmentResult result)
        {
            return string.Join("; ", result.Errors.Select(value =>
                value.Code + ":" + value.SectorIndex + ":" + value.SourceOwner + ":" + value.Message));
        }

        private static bool IsLowerHex(char value)
        {
            return (value >= '0' && value <= '9') || (value >= 'a' && value <= 'f');
        }

        private static T GetField<T>(object target, string name)
        {
            return (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }

        private static T InvokeInternal<T>(params object[] arguments)
        {
            var constructor = typeof(T).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
            return (T)constructor.Invoke(arguments);
        }

        private sealed class OpenCandidate
        {
            public OpenCandidate(Type0RouteMaskAssignment assignment, int direction)
            {
                Assignment = assignment;
                Direction = direction;
            }

            public Type0RouteMaskAssignment Assignment { get; }
            public int Direction { get; }
        }
    }
}
