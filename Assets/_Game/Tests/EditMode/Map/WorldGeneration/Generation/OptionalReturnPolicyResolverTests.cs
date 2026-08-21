using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    [Category("MAP06_07")]
    public sealed class OptionalReturnPolicyResolverTests
    {
        private static readonly int[][] Directions =
        {
            new[] { -1, 0 }, new[] { 1, 0 }, new[] { 0, 1 }, new[] { 0, -1 }
        };

        private Type0RouteMaskAssignmentResult type0;
        private OptionalAccessAssignmentResult access;
        private OptionalRewardTierResult reward;
        private MandatoryRouteGraph graph;
        private OptionalReturnPolicyResult baseline;
        private string sourceSignature;
        private List<DirectedEdge> directedEdges;

        public static IEnumerable<int> EnumSettingsCases => Enumerable.Range(0, 30);
        public static IEnumerable<int> BaseEdgeCases => Enumerable.Range(0, 42);
        public static IEnumerable<int> ReturnabilityCases => Enumerable.Range(0, 44);
        public static IEnumerable<int> CriticalWitnessCases => Enumerable.Range(0, 34);
        public static IEnumerable<int> AccessReverseCases => Enumerable.Range(0, 34);
        public static IEnumerable<int> SourceChainCases => Enumerable.Range(0, 30);
        public static IEnumerable<int> AtomicCases => Enumerable.Range(0, 28);
        public static IEnumerable<int> DeterminismCases => Enumerable.Range(0, 24);
        public static IEnumerable<int> IntegrityCases => Enumerable.Range(0, 22);

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var fixture = new OptionalRewardTierCalculatorTests();
            fixture.OneTimeSetUp();
            type0 = GetField<Type0RouteMaskAssignmentResult>(fixture, "type0");
            access = GetField<OptionalAccessAssignmentResult>(fixture, "access");
            reward = GetField<OptionalRewardTierResult>(fixture, "baseline");
            graph = GetField<MandatoryRouteGraph>(fixture, "graph");
            sourceSignature = SourceSignature();
            directedEdges = BuildDirectedEdges(type0);
            baseline = new OptionalReturnPolicyResolver().Resolve(type0, access, reward, ApprovedSettings());

            Assert.That(type0.IsSuccess && access.IsSuccess && reward.IsSuccess, Is.True);
            Assert.That(baseline.IsSuccess, Is.True, FormatErrors(baseline));
            Assert.That(directedEdges, Has.Count.EqualTo(60));
            Assert.That(baseline.Assignments, Has.Count.EqualTo(12));
        }

        [TestCaseSource(nameof(EnumSettingsCases))]
        public void ExistingReturnEnumResolutionEnumsSettingsAndImmutabilityAreExact(int caseId)
        {
            if (caseId < 22)
            {
                var settings = ApprovedSettings();
                Assert.That(settings.MaximumBacktrackSectorCount, Is.EqualTo(6));
                Assert.That(settings.RequireAllCellsReturnable, Is.True);
                Assert.That(OptionalRegionTokenCodec.ToToken(OptionalReturnPolicy.BacktrackToAttachment), Is.EqualTo("BACKTRACK"));
                Assert.That(OptionalRegionTokenCodec.ToToken(OptionalReturnPolicy.ReturnGateToMandatory), Is.EqualTo("RETURN_GATE"));
                Assert.That(OptionalRegionTokenCodec.ToToken(OptionalReturnPolicy.SafeExitToMandatory), Is.EqualTo("SAFE_EXIT"));
                Assert.That(Enum.GetValues(typeof(OptionalReturnPolicyResolutionStatus)).Length, Is.EqualTo(6));
                Assert.That(Enum.GetValues(typeof(OptionalReturnPolicyResolutionErrorCode)).Length, Is.EqualTo(13));
                Assert.That(typeof(OptionalReturnPolicySettings).GetProperties().All(value => !value.CanWrite), Is.True);
                return;
            }

            switch (caseId % 4)
            {
                case 0:
                    Assert.That(() => new OptionalReturnPolicySettings(0, true), Throws.TypeOf<ArgumentOutOfRangeException>());
                    break;
                case 1:
                    Assert.That(() => new OptionalReturnPolicySettings(170, true), Throws.TypeOf<ArgumentOutOfRangeException>());
                    break;
                case 2:
                    Assert.That(() => new OptionalReturnPolicySettings(6, false), Throws.TypeOf<ArgumentException>());
                    break;
                default:
                    Assert.That(Enum.IsDefined(typeof(OptionalReturnPolicyResolutionStatus), 999), Is.False);
                    Assert.That(Enum.IsDefined(typeof(OptionalReturnPolicyResolutionErrorCode), 999), Is.False);
                    break;
            }
        }

        [TestCaseSource(nameof(BaseEdgeCases))]
        public void InternalBaseEdgesAreSameRegionReciprocalAndCanonical(int caseId)
        {
            if (caseId >= 40)
            {
                var invalid = new OptionalReturnPolicyResolver().Resolve(
                    CloneType0WithBrokenReciprocal(), access, reward, ApprovedSettings());
                AssertAtomicFailure(invalid, OptionalReturnPolicyResolutionStatus.InvalidTopology);
                Assert.That(invalid.Errors.Any(value =>
                    value.Code == OptionalReturnPolicyResolutionErrorCode.NonReciprocalBaseEdge), Is.True);
                return;
            }

            var edge = directedEdges[caseId % directedEdges.Count];
            Assert.That(edge.Source.RegionId, Is.EqualTo(edge.Destination.RegionId));
            Assert.That(IsOpen(edge.Source.OpenMask, edge.Dx, edge.Dy), Is.True);
            Assert.That(IsOpen(edge.Destination.OpenMask, -edge.Dx, -edge.Dy), Is.True);
            Assert.That(Math.Abs(edge.Source.Sector.X - edge.Destination.Sector.X) +
                        Math.Abs(edge.Source.Sector.Y - edge.Destination.Sector.Y), Is.EqualTo(1));
            Assert.That(baseline.Diagnostics.InternalUndirectedBaseEdgeCount, Is.EqualTo(30));
        }

        [TestCaseSource(nameof(ReturnabilityCases))]
        public void EveryRegionCellReturnsToAttachmentThroughCanonicalBfs(int caseId)
        {
            var region = type0.SourceSnapshot.Regions.OrderBy(value => value.RegionId)
                .ElementAt(caseId % type0.SourceSnapshot.Regions.Count);
            var reachable = ReachableFromAttachment(region, type0);
            Assert.That(reachable, Has.Count.EqualTo(region.Cells.Count));
            Assert.That(region.Cells.All(value => reachable.Contains(value.SectorIndex)), Is.True);
            Assert.That(region.Cells.All(value => !value.RequiresReturnConnection), Is.True);
            Assert.That(baseline.Diagnostics.ReturnableCellCount, Is.EqualTo(39));
            Assert.That(baseline.Diagnostics.NonReturnableCellCount, Is.Zero);
        }

        [TestCaseSource(nameof(CriticalWitnessCases))]
        public void CriticalSourceAndShortestWitnessAreCanonicalAndBounded(int caseId)
        {
            var assignment = baseline.Assignments[caseId % baseline.Assignments.Count];
            var region = type0.SourceSnapshot.Regions.Single(value => value.RegionId == assignment.RegionId);
            var expectedCritical = region.Cells.OrderByDescending(value => value.Depth.Value)
                .ThenBy(value => value.SectorIndex).First();

            Assert.That(assignment.CriticalSourceSectorIndex, Is.EqualTo(expectedCritical.SectorIndex));
            Assert.That(assignment.CriticalSourceDepth, Is.EqualTo(expectedCritical.Depth));
            Assert.That(assignment.CriticalReturnPathSectorIndices.First(), Is.EqualTo(expectedCritical.SectorIndex));
            Assert.That(assignment.CriticalReturnPathSectorIndices.Last(), Is.EqualTo(region.Attachment.EntrySectorIndex));
            Assert.That(assignment.CriticalReturnEdgeCount,
                Is.EqualTo(assignment.CriticalReturnPathSectorIndices.Count - 1));
            Assert.That(assignment.CriticalReturnPathSectorIndices.Count, Is.LessThanOrEqualTo(6));
            AssertPathEdgesAreOpenAndReciprocal(assignment);
        }

        [TestCaseSource(nameof(AccessReverseCases))]
        public void AccessBoundaryIsReusedInReverseWhileBaseMaskStaysClosed(int caseId)
        {
            var assignment = baseline.Assignments[caseId % baseline.Assignments.Count];
            var region = type0.SourceSnapshot.Regions.Single(value => value.RegionId == assignment.RegionId);
            var sourceAccess = access.Assignments.Single(value => value.RegionId == assignment.RegionId);
            var entryMask = type0.Assignments.Single(value =>
                value.SectorIndex == region.Attachment.EntrySectorIndex).OpenMask;

            Assert.That(assignment.AccessRule, Is.EqualTo(sourceAccess.AccessRule));
            Assert.That(assignment.AttachmentEntrySectorIndex, Is.EqualTo(sourceAccess.EntrySectorIndex));
            Assert.That(assignment.ReturnDestinationMandatorySectorIndex,
                Is.EqualTo(sourceAccess.MandatoryRouteSectorIndex));
            Assert.That(IsOpen(entryMask, -sourceAccess.EntrySideFromMandatoryDx,
                -sourceAccess.EntrySideFromMandatoryDy), Is.False);
            Assert.That(assignment.UsesSameOpenedAttachmentBoundary, Is.True);
            Assert.That(assignment.RequiresReturnDevice, Is.False);
            Assert.That(assignment.ReturnPolicy, Is.EqualTo(OptionalReturnPolicy.BacktrackToAttachment));
        }

        [TestCaseSource(nameof(SourceChainCases))]
        public void Type0AccessRewardAndRegionSourceChainIdentityIsExact(int caseId)
        {
            var assignment = baseline.Assignments[caseId % baseline.Assignments.Count];
            var region = type0.SourceSnapshot.Regions.Single(value => value.RegionId == assignment.RegionId);
            var sourceAccess = access.Assignments.Single(value => value.RegionId == assignment.RegionId);
            var sourceReward = reward.Assignments.Single(value => value.RegionId == assignment.RegionId);

            Assert.That(baseline.SourceType0AssignmentDigest, Is.EqualTo(type0.CanonicalDigest));
            Assert.That(baseline.SourceAccessAssignmentDigest, Is.EqualTo(access.CanonicalDigest));
            Assert.That(baseline.SourceRewardTierDigest, Is.EqualTo(reward.CanonicalDigest));
            Assert.That(baseline.SourceGrowthDigest, Is.EqualTo(type0.SourceGrowthDigest));
            Assert.That(assignment.AttachmentOrder, Is.EqualTo(region.Attachment.AttachmentOrder));
            Assert.That(assignment.AccessRule, Is.EqualTo(sourceAccess.AccessRule));
            Assert.That(assignment.RewardTier, Is.EqualTo(sourceReward.RewardTier));
            Assert.That(sourceReward.ClueId, Is.EqualTo(sourceAccess.Clue.ClueId));
        }

        [TestCaseSource(nameof(AtomicCases))]
        public void InvalidInputSourceTopologyAndUnsupportedRequirementsFailAtomically(int caseId)
        {
            OptionalReturnPolicyResult result;
            switch (caseId % 8)
            {
                case 0:
                    result = new OptionalReturnPolicyResolver().Resolve(null, access, reward, ApprovedSettings());
                    AssertAtomicFailure(result, OptionalReturnPolicyResolutionStatus.InvalidInput);
                    break;
                case 1:
                    result = new OptionalReturnPolicyResolver().Resolve(type0, null, reward, ApprovedSettings());
                    AssertAtomicFailure(result, OptionalReturnPolicyResolutionStatus.InvalidInput);
                    break;
                case 2:
                    result = new OptionalReturnPolicyResolver().Resolve(type0, access, null, ApprovedSettings());
                    AssertAtomicFailure(result, OptionalReturnPolicyResolutionStatus.InvalidInput);
                    break;
                case 3:
                    result = new OptionalReturnPolicyResolver().Resolve(type0, access, reward, null);
                    AssertAtomicFailure(result, OptionalReturnPolicyResolutionStatus.InvalidSettings);
                    break;
                case 4:
                    result = new OptionalReturnPolicyResolver().Resolve(
                        CloneType0(sourceRegionDelta: 1), access, reward, ApprovedSettings());
                    AssertAtomicFailure(result, OptionalReturnPolicyResolutionStatus.InvalidSource);
                    break;
                case 5:
                    result = new OptionalReturnPolicyResolver().Resolve(
                        type0, CloneAccess(sourceType0Digest: new string('f', 64)), reward, ApprovedSettings());
                    AssertAtomicFailure(result, OptionalReturnPolicyResolutionStatus.InvalidSource);
                    break;
                case 6:
                    result = new OptionalReturnPolicyResolver().Resolve(
                        type0, access, reward, new OptionalReturnPolicySettings(1, true));
                    AssertAtomicFailure(result, OptionalReturnPolicyResolutionStatus.InvalidTopology);
                    Assert.That(result.Errors.Any(value =>
                        value.Code == OptionalReturnPolicyResolutionErrorCode.PathLimitExceeded), Is.True);
                    break;
                default:
                    result = new OptionalReturnPolicyResolver().Resolve(
                        CloneType0WithReturnRequirement(), access, reward, ApprovedSettings());
                    AssertAtomicFailure(result, OptionalReturnPolicyResolutionStatus.UnsupportedReturnRequirement);
                    Assert.That(result.Errors.Any(value =>
                        value.Code == OptionalReturnPolicyResolutionErrorCode.UnsupportedReturnRequirement), Is.True);
                    break;
            }
        }

        [TestCaseSource(nameof(DeterminismCases))]
        public void CultureCallerOrderServiceReuseAndRepeatedRunsAreDeterministic(int caseId)
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo(caseId % 2 == 0 ? "tr-TR" : "fr-FR");
                var resolver = new OptionalReturnPolicyResolver();
                var reordered = resolver.Resolve(
                    CloneType0(reverse: true), CloneAccess(reverse: true), CloneReward(reverse: true), ApprovedSettings());
                var repeated = resolver.Resolve(type0, access, reward, ApprovedSettings());

                Assert.That(reordered.IsSuccess, Is.True, FormatErrors(reordered));
                Assert.That(repeated.IsSuccess, Is.True, FormatErrors(repeated));
                Assert.That(reordered.CanonicalDigest, Is.EqualTo(baseline.CanonicalDigest));
                Assert.That(repeated.CanonicalDigest, Is.EqualTo(baseline.CanonicalDigest));
                Assert.That(reordered.Assignments.Select(AssignmentSignature),
                    Is.EqualTo(baseline.Assignments.Select(AssignmentSignature)));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [TestCaseSource(nameof(IntegrityCases))]
        public void SourceMutationRngType4DevicesAndFutureBoundaryRemainFrozen(int caseId)
        {
            switch (caseId % 6)
            {
                case 0:
                    Assert.That(SourceSignature(), Is.EqualTo(sourceSignature));
                    Assert.That(baseline.Diagnostics.SourceMutationCount, Is.Zero);
                    Assert.That(baseline.RngDrawCount, Is.Zero);
                    break;
                case 1:
                    Assert.That(baseline.Diagnostics.ReturnGateCount, Is.Zero);
                    Assert.That(baseline.Diagnostics.SafeExitCount, Is.Zero);
                    Assert.That(baseline.Diagnostics.ReturnDeviceReservationCount, Is.Zero);
                    Assert.That(baseline.Diagnostics.ExtraSafeExitReservationCount, Is.Zero);
                    break;
                case 2:
                    var bits = caseId % 4;
                    var left = (bits & 1) != 0;
                    var right = (bits & 2) != 0;
                    Assert.That(graph.MaskFamily.TryResolve(left, right, true, true, out var mask), Is.True);
                    Assert.That(mask.OpenUp && mask.OpenDown, Is.True);
                    Assert.That(mask.OpenLeft, Is.EqualTo(left));
                    Assert.That(mask.OpenRight, Is.EqualTo(right));
                    break;
                case 3:
                    Assert.That(baseline.Diagnostics.AttachmentBoundaryBaseOpenCount, Is.Zero);
                    Assert.That(type0.Diagnostics.MandatoryBoundaryBaseOpenCount, Is.Zero);
                    Assert.That(type0.Assignments.All(value => !value.OpenMask.HasHorizontalThrough), Is.True);
                    break;
                case 4:
                    foreach (var name in new[]
                    {
                        "OptionalReturnPolicyResolutionStatus", "OptionalReturnPolicyResolutionErrorCode",
                        "OptionalReturnPolicySettings", "OptionalReturnPolicyAssignment",
                        "OptionalReturnPolicyDiagnostics", "OptionalReturnPolicyResolutionError",
                        "OptionalReturnPolicyResult", "OptionalReturnPolicyResolver"
                    })
                        Assert.That(typeof(OptionalReturnPolicyResolver).Assembly.GetType(
                            "StarNight.Map.WorldGeneration.Generation." + name, false), Is.Not.Null);
                    Assert.That(typeof(OptionalReturnPolicyResolverTests), Is.Not.Null);
                    break;
                default:
                    foreach (var name in new[]
                    {
                        "OptionalRegionOverlayRenderer", "OptionalRegionValidationOverlayWindow", "OptionalRegionOverlay",
                        "GeneratedOptionalRegionCsvWriter"
                    })
                        Assert.That(typeof(OptionalReturnPolicyResolver).Assembly.GetType(
                            "StarNight.Map.WorldGeneration.Generation." + name, false), Is.Null);
                    break;
            }
        }

        [Test]
        public void ApprovedFixturePublishesCanonicalReturnSummary()
        {
            foreach (var assignment in baseline.Assignments)
            {
                TestContext.WriteLine(
                    "MAP06_07_ASSIGNMENT region={0} ordinal={1} attachment={2} access={3} reward={4} policy={5} critical={6}/{7} entry={8} destination={9} path={10} edges={11} returnable={12} sameBoundary={13} device={14}",
                    assignment.RegionId.Value, assignment.RegionOrdinal, assignment.AttachmentOrder,
                    OptionalRegionTokenCodec.ToToken(assignment.AccessRule),
                    OptionalRegionTokenCodec.ToToken(assignment.RewardTier),
                    OptionalRegionTokenCodec.ToToken(assignment.ReturnPolicy),
                    assignment.CriticalSourceSectorIndex, assignment.CriticalSourceDepth.Value,
                    assignment.AttachmentEntrySectorIndex, assignment.ReturnDestinationMandatorySectorIndex,
                    string.Join(",", assignment.CriticalReturnPathSectorIndices),
                    assignment.CriticalReturnEdgeCount, assignment.ReturnableCellCount,
                    assignment.UsesSameOpenedAttachmentBoundary ? 1 : 0,
                    assignment.RequiresReturnDevice ? 1 : 0);
            }

            var diagnostics = baseline.Diagnostics;
            TestContext.WriteLine(
                "MAP06_07_SUMMARY source={0}/{1}/{2}/{3} assignments={4} policies={5}/{6}/{7} returnable={8}/{9} edges={10} witnesses={11}/{12}/{13} sameBoundary={14} devices={15} extraExit={16} baseOpen={17} rng={18} mutation={19} type0={20} access={21} reward={22} growth={23} digest={24}",
                diagnostics.SourceRegionCount, diagnostics.SourceType0CellAssignmentCount,
                diagnostics.SourceAccessAssignmentCount, diagnostics.SourceRewardTierAssignmentCount,
                diagnostics.AssignmentCount, diagnostics.BacktrackCount, diagnostics.ReturnGateCount,
                diagnostics.SafeExitCount, diagnostics.ReturnableCellCount, diagnostics.NonReturnableCellCount,
                diagnostics.InternalUndirectedBaseEdgeCount, diagnostics.CriticalWitnessSectorCountTotal,
                diagnostics.CriticalWitnessEdgeCountTotal, diagnostics.MaximumCriticalWitnessSectorCount,
                diagnostics.SameOpenedAttachmentReturnCount, diagnostics.ReturnDeviceReservationCount,
                diagnostics.ExtraSafeExitReservationCount, diagnostics.AttachmentBoundaryBaseOpenCount,
                diagnostics.RngDrawCount, diagnostics.SourceMutationCount,
                baseline.SourceType0AssignmentDigest, baseline.SourceAccessAssignmentDigest,
                baseline.SourceRewardTierDigest, baseline.SourceGrowthDigest, baseline.CanonicalDigest);

            Assert.That(diagnostics.SourceRegionCount, Is.EqualTo(12));
            Assert.That(diagnostics.SourceType0CellAssignmentCount, Is.EqualTo(39));
            Assert.That(diagnostics.SourceAccessAssignmentCount, Is.EqualTo(12));
            Assert.That(diagnostics.SourceRewardTierAssignmentCount, Is.EqualTo(12));
            Assert.That(diagnostics.InternalUndirectedBaseEdgeCount, Is.EqualTo(30));
            Assert.That(diagnostics.ReturnableCellCount, Is.EqualTo(39));
            Assert.That(diagnostics.NonReturnableCellCount, Is.Zero);
            Assert.That(diagnostics.BacktrackCount, Is.EqualTo(12));
            Assert.That(diagnostics.ReturnGateCount + diagnostics.SafeExitCount, Is.Zero);
            Assert.That(diagnostics.CriticalWitnessSectorCountTotal, Is.EqualTo(31));
            Assert.That(diagnostics.CriticalWitnessEdgeCountTotal, Is.EqualTo(19));
            Assert.That(diagnostics.MaximumCriticalWitnessSectorCount, Is.EqualTo(4));
            Assert.That(diagnostics.SameOpenedAttachmentReturnCount, Is.EqualTo(12));
            Assert.That(baseline.Assignments.Select(value => value.CriticalSourceDepth.Value),
                Is.EqualTo(new[] { 4, 1, 1, 3, 4, 1, 4, 1, 4, 1, 3, 4 }));
            Assert.That(baseline.CanonicalDigest, Has.Length.EqualTo(64));
        }

        private static OptionalReturnPolicySettings ApprovedSettings()
        {
            return new OptionalReturnPolicySettings(6, true);
        }

        private List<DirectedEdge> BuildDirectedEdges(Type0RouteMaskAssignmentResult source)
        {
            var result = new List<DirectedEdge>();
            foreach (var assignment in source.Assignments)
            {
                var region = source.SourceSnapshot.Regions.Single(value => value.RegionId == assignment.RegionId);
                foreach (var direction in Directions)
                {
                    if (!IsOpen(assignment.OpenMask, direction[0], direction[1])) continue;
                    var neighborCell = region.Cells.SingleOrDefault(value =>
                        value.Sector.X == assignment.Sector.X + direction[0] &&
                        value.Sector.Y == assignment.Sector.Y + direction[1]);
                    if (neighborCell == null) continue;
                    var neighbor = source.Assignments.Single(value => value.SectorIndex == neighborCell.SectorIndex);
                    result.Add(new DirectedEdge(assignment, neighbor, direction[0], direction[1]));
                }
            }
            return result;
        }

        private static HashSet<int> ReachableFromAttachment(
            OptionalRegion region,
            Type0RouteMaskAssignmentResult source)
        {
            var visited = new HashSet<int>();
            var queue = new Queue<OptionalRegionCell>();
            var root = region.Cells.Single(value => value.IsAttachmentCell);
            visited.Add(root.SectorIndex);
            queue.Enqueue(root);
            while (queue.Count != 0)
            {
                var cell = queue.Dequeue();
                var mask = source.Assignments.Single(value => value.SectorIndex == cell.SectorIndex).OpenMask;
                foreach (var direction in Directions)
                {
                    if (!IsOpen(mask, direction[0], direction[1])) continue;
                    var neighbor = region.Cells.SingleOrDefault(value =>
                        value.Sector.X == cell.Sector.X + direction[0] &&
                        value.Sector.Y == cell.Sector.Y + direction[1]);
                    if (neighbor != null && visited.Add(neighbor.SectorIndex)) queue.Enqueue(neighbor);
                }
            }
            return visited;
        }

        private void AssertPathEdgesAreOpenAndReciprocal(OptionalReturnPolicyAssignment assignment)
        {
            for (var index = 0; index + 1 < assignment.CriticalReturnPathSectorIndices.Count; index++)
            {
                var current = type0.Assignments.Single(value =>
                    value.SectorIndex == assignment.CriticalReturnPathSectorIndices[index]);
                var next = type0.Assignments.Single(value =>
                    value.SectorIndex == assignment.CriticalReturnPathSectorIndices[index + 1]);
                var dx = next.Sector.X - current.Sector.X;
                var dy = next.Sector.Y - current.Sector.Y;
                Assert.That(IsOpen(current.OpenMask, dx, dy), Is.True);
                Assert.That(IsOpen(next.OpenMask, -dx, -dy), Is.True);
            }
        }

        private Type0RouteMaskAssignmentResult CloneType0(
            bool reverse = false,
            int sourceRegionDelta = 0,
            string canonicalDigest = null,
            OptionalRegionSnapshot snapshot = null,
            IEnumerable<Type0RouteMaskAssignment> assignments = null)
        {
            var source = type0.Diagnostics;
            var diagnostics = new Type0RouteMaskAssignmentDiagnostics(
                source.SourceRouteMaskDefinitionCount, source.RegisteredType0MaskCount,
                source.IgnoredNonType0DefinitionCount, source.SourceRegionCount + sourceRegionDelta,
                source.SourceCellCount, source.AssignmentCount, source.InternalUndirectedEdgeCount,
                source.AttachmentBoundaryClosedCount, source.MandatoryBoundaryBaseOpenCount,
                source.ClosedCrossRegionAdjacencyCount, source.HorizontalThroughCount,
                source.UnsupportedRequiredMaskCount, source.RngDrawCount, source.SourceMutationCount);
            IEnumerable<Type0RouteMaskAssignment> values = assignments ?? type0.Assignments;
            if (reverse) values = values.Reverse();
            return InvokeInternal<Type0RouteMaskAssignmentResult>(
                Type0RouteMaskAssignmentStatus.Completed, snapshot ?? type0.SourceSnapshot,
                type0.RegisteredMasks, values, diagnostics,
                Array.Empty<Type0RouteMaskAssignmentError>(), type0.SourceGrowthDigest,
                type0.SourceRouteMaskCatalogDigest, canonicalDigest ?? type0.CanonicalDigest);
        }

        private OptionalAccessAssignmentResult CloneAccess(
            bool reverse = false,
            string sourceType0Digest = null)
        {
            IEnumerable<OptionalAccessAssignment> assignments = access.Assignments;
            IEnumerable<OptionalAccessClue> clues = access.Clues;
            if (reverse)
            {
                assignments = assignments.Reverse();
                clues = clues.Reverse();
            }
            return InvokeInternal<OptionalAccessAssignmentResult>(
                OptionalAccessAssignmentStatus.Completed, assignments, clues, access.Diagnostics,
                Array.Empty<OptionalAccessAssignmentError>(),
                sourceType0Digest ?? access.SourceType0AssignmentDigest,
                access.SourceGrowthDigest, access.CanonicalDigest);
        }

        private OptionalRewardTierResult CloneReward(bool reverse = false)
        {
            IEnumerable<OptionalRewardTierAssignment> assignments = reward.Assignments;
            if (reverse) assignments = assignments.Reverse();
            return InvokeInternal<OptionalRewardTierResult>(
                OptionalRewardTierCalculationStatus.Completed, assignments, reward.Diagnostics,
                Array.Empty<OptionalRewardTierCalculationError>(), reward.SourceType0AssignmentDigest,
                reward.SourceAccessAssignmentDigest, reward.SourceGrowthDigest, reward.CanonicalDigest);
        }

        private Type0RouteMaskAssignmentResult CloneType0WithBrokenReciprocal()
        {
            var edge = directedEdges[0];
            var target = edge.Destination;
            var brokenMask = new Type0RouteOpenMask(
                target.OpenMask.OpenLeft && !(-edge.Dx == -1 && -edge.Dy == 0),
                target.OpenMask.OpenRight && !(-edge.Dx == 1 && -edge.Dy == 0),
                target.OpenMask.OpenUp && !(-edge.Dx == 0 && -edge.Dy == 1),
                target.OpenMask.OpenDown && !(-edge.Dx == 0 && -edge.Dy == -1));
            var record = InvokeInternal<Type0RouteMaskRecord>(
                target.Mask.MaskId, target.Mask.RouteType, brokenMask,
                target.Mask.MandatoryAllowed, target.Mask.Active,
                target.Mask.DescriptionKo, target.Mask.SourceDefinition);
            var sourceCell = type0.SourceSnapshot.Cells.Single(value => value.SectorIndex == target.SectorIndex);
            var replacement = InvokeInternal<Type0RouteMaskAssignment>(sourceCell, record);
            var assignments = type0.Assignments.Select(value =>
                value.SectorIndex == target.SectorIndex ? replacement : value).ToList();
            return CloneType0(assignments: assignments);
        }

        private Type0RouteMaskAssignmentResult CloneType0WithReturnRequirement()
        {
            var targetSector = type0.SourceSnapshot.Cells[0].SectorIndex;
            var clonedRegions = new List<OptionalRegion>();
            var clonedCells = new List<OptionalRegionCell>();
            foreach (var region in type0.SourceSnapshot.Regions)
            {
                var cells = region.Cells.Select(value => new OptionalRegionCell(
                    value.RegionId, value.SectorIndex, value.Sector, value.Depth,
                    value.IsAttachmentCell, value.SectorIndex == targetSector || value.RequiresReturnConnection)).ToList();
                clonedCells.AddRange(cells);
                clonedRegions.Add(new OptionalRegion(
                    region.RegionId, region.Attachment, region.AccessRule, region.RewardTier,
                    region.ReturnPolicy, cells, region.MaxDepth));
            }
            var source = type0.SourceSnapshot;
            var snapshot = new OptionalRegionSnapshot(
                clonedRegions, clonedCells, source.MandatoryRouteSectorIndices,
                source.SourceMandatoryNodeCount, source.SourceMandatoryDirectedEdgeCount,
                source.SourceMandatoryRouteCellCount, source.SourceMandatoryGraphDigest);
            var assignments = type0.Assignments.Select(value =>
                InvokeInternal<Type0RouteMaskAssignment>(
                    clonedCells.Single(cell => cell.SectorIndex == value.SectorIndex), value.Mask)).ToList();
            return CloneType0(snapshot: snapshot, assignments: assignments);
        }

        private static T InvokeInternal<T>(params object[] arguments)
        {
            var constructor = typeof(T).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
            return (T)constructor.Invoke(arguments);
        }

        private static T GetField<T>(object target, string name)
        {
            return (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        }

        private static void AssertAtomicFailure(
            OptionalReturnPolicyResult result,
            OptionalReturnPolicyResolutionStatus expectedStatus)
        {
            Assert.That(result.Status, Is.EqualTo(expectedStatus));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Assignments, Is.Empty);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Errors, Is.Not.Empty);
            Assert.That(result.Diagnostics.AssignmentCount, Is.Zero);
            Assert.That(result.Diagnostics.ReturnDeviceReservationCount, Is.Zero);
            Assert.That(result.Diagnostics.ExtraSafeExitReservationCount, Is.Zero);
            Assert.That(result.Diagnostics.RngDrawCount, Is.Zero);
            Assert.That(result.Diagnostics.SourceMutationCount, Is.Zero);
        }

        private string SourceSignature()
        {
            return type0.CanonicalDigest + "|" + access.CanonicalDigest + "|" + reward.CanonicalDigest + "|" +
                   string.Join(",", type0.Assignments.Select(value =>
                       value.RegionId.Value + ":" + value.SectorIndex + ":" + value.OpenMask)) + "|" +
                   string.Join(",", access.Assignments.Select(value =>
                       value.RegionId.Value + ":" + value.AccessRule + ":" + value.Clue.ClueId.Value)) + "|" +
                   string.Join(",", reward.Assignments.Select(value =>
                       value.RegionId.Value + ":" + value.RewardScore + ":" + value.RewardTier));
        }

        private static string AssignmentSignature(OptionalReturnPolicyAssignment value)
        {
            return value.RegionId.Value + "|" + value.RegionOrdinal + "|" + value.AttachmentOrder + "|" +
                   value.AccessRule + "|" + value.RewardTier + "|" + value.ReturnPolicy + "|" +
                   value.CriticalSourceSectorIndex + "|" + value.CriticalSourceDepth.Value + "|" +
                   string.Join(",", value.CriticalReturnPathSectorIndices);
        }

        private static string FormatErrors(OptionalReturnPolicyResult result)
        {
            return string.Join("; ", result.Errors.Select(value =>
                value.Code + ":" + value.RegionId.Value + ":" + value.SectorIndex + ":" + value.Message));
        }

        private static bool IsOpen(Type0RouteOpenMask mask, int dx, int dy)
        {
            if (dx == -1 && dy == 0) return mask.OpenLeft;
            if (dx == 1 && dy == 0) return mask.OpenRight;
            if (dx == 0 && dy == 1) return mask.OpenUp;
            if (dx == 0 && dy == -1) return mask.OpenDown;
            return false;
        }

        private sealed class DirectedEdge
        {
            public DirectedEdge(
                Type0RouteMaskAssignment source,
                Type0RouteMaskAssignment destination,
                int dx,
                int dy)
            {
                Source = source;
                Destination = destination;
                Dx = dx;
                Dy = dy;
            }

            public Type0RouteMaskAssignment Source { get; }
            public Type0RouteMaskAssignment Destination { get; }
            public int Dx { get; }
            public int Dy { get; }
        }
    }
}
