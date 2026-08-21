using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryRouteLoopPlanner
    {
        public MandatoryRouteLoopBuildResult Build(
            MandatoryRouteTerminalSet terminalSet,
            MandatoryConnectorTree connectorTree,
            HorizontalBackbonePlan horizontalPlan,
            VerticalGatewayPlan verticalPlan,
            UpDownConflictResolutionPlan conflictPlan)
        {
            return BuildCore(terminalSet, connectorTree, horizontalPlan, verticalPlan, conflictPlan, null);
        }

        public MandatoryRouteLoopBuildResult Build(
            MandatoryRouteTerminalSet terminalSet,
            MandatoryConnectorTree connectorTree,
            HorizontalBackbonePlan horizontalPlan,
            VerticalGatewayPlan verticalPlan,
            UpDownConflictResolutionPlan conflictPlan,
            IEnumerable<MandatoryRouteLoopCandidate> syntheticCandidates)
        {
            if (syntheticCandidates == null) throw new ArgumentNullException(nameof(syntheticCandidates));
            return BuildCore(terminalSet, connectorTree, horizontalPlan, verticalPlan, conflictPlan, syntheticCandidates);
        }

        internal static int CompareCandidates(MandatoryRouteLoopCandidate left, MandatoryRouteLoopCandidate right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            var value = left.CheckedTotalCost.CompareTo(right.CheckedTotalCost);
            if (value != 0) return value;
            value = right.UniqueCellCount.CompareTo(left.UniqueCellCount);
            if (value != 0) return value;
            value = left.SharedCellCount.CompareTo(right.SharedCellCount);
            if (value != 0) return value;
            value = left.FirstSectorIndex.CompareTo(right.FirstSectorIndex);
            return value != 0 ? value : left.LoopId.CompareTo(right.LoopId);
        }

        private static MandatoryRouteLoopBuildResult BuildCore(
            MandatoryRouteTerminalSet terminalSet,
            MandatoryConnectorTree connectorTree,
            HorizontalBackbonePlan horizontalPlan,
            VerticalGatewayPlan verticalPlan,
            UpDownConflictResolutionPlan conflictPlan,
            IEnumerable<MandatoryRouteLoopCandidate> syntheticCandidates)
        {
            var errors = ValidateSources(terminalSet, connectorTree, horizontalPlan, verticalPlan, conflictPlan);
            if (errors.Count != 0) return Invalid(errors);
            var candidates = syntheticCandidates == null
                ? CreateStarterCandidates(terminalSet, connectorTree, horizontalPlan, verticalPlan, conflictPlan)
                : new List<MandatoryRouteLoopCandidate>(syntheticCandidates);
            candidates.Sort(CompareCandidates);
            var seenIds = new HashSet<MandatoryRouteLoopId>();
            foreach (var candidate in candidates)
            {
                if (candidate == null) continue;
                if (!seenIds.Add(candidate.LoopId))
                    errors.Add(new MandatoryRouteLoopBuildError(MandatoryRouteLoopBuildErrorCode.DuplicateLoopId, candidate.LoopId.Value, "Loop candidate IDs must be unique."));
            }
            if (errors.Count != 0) return Invalid(errors);

            var loops = new List<MandatoryRouteLoop>();
            var selectedInterior = new HashSet<SectorCoord>();
            var selectedPairs = new HashSet<string>(StringComparer.Ordinal);
            var overlapRejected = 0;
            foreach (var candidate in candidates)
            {
                if (loops.Count == MandatoryRouteLoopPlan.MinimumLoopCount) break;
                if (candidate == null || !candidate.IsEligible) continue;
                var pair = PairKey(candidate.StartTerminalId, candidate.EndTerminalId);
                var interior = candidate.OrderedCells.Skip(1).Take(Math.Max(0, candidate.OrderedCells.Count - 2)).ToArray();
                if (selectedPairs.Contains(pair) || interior.Any(selectedInterior.Contains))
                {
                    overlapRejected++;
                    continue;
                }
                selectedPairs.Add(pair);
                foreach (var cell in interior) selectedInterior.Add(cell);
                loops.Add(new MandatoryRouteLoop(candidate, "DISTINCT_TERMINAL_PAIR_AND_NO_SHARED_INTERIOR"));
            }

            var plan = new MandatoryRouteLoopPlan(terminalSet, connectorTree, horizontalPlan, verticalPlan, conflictPlan, candidates, loops);
            var diagnostics = new MandatoryRouteLoopDiagnostics(
                terminalSet.TerminalCount,
                connectorTree.TreeEdgeCount,
                candidates.Count,
                candidates.Count(value => value != null && value.IsEligible),
                plan.LoopCount,
                plan.IndependentLoopCount,
                candidates.Count(value => value != null && !value.IsInsideWorld),
                candidates.Count(value => value != null && value.HasReservationIntrusion),
                candidates.Count(value => value != null && value.HasInactiveIntrusion),
                candidates.Count(value => value != null && value.HasMandatoryPathIntrusion),
                overlapRejected,
                Math.Max(0, MandatoryRouteLoopPlan.MinimumLoopCount - plan.IndependentLoopCount));
            return new MandatoryRouteLoopBuildResult(MandatoryRouteLoopBuildStatus.Completed, plan, diagnostics, Array.Empty<MandatoryRouteLoopBuildError>());
        }

        private static List<MandatoryRouteLoopBuildError> ValidateSources(
            MandatoryRouteTerminalSet terminals,
            MandatoryConnectorTree tree,
            HorizontalBackbonePlan horizontal,
            VerticalGatewayPlan vertical,
            UpDownConflictResolutionPlan conflicts)
        {
            var errors = new List<MandatoryRouteLoopBuildError>();
            if (terminals == null) errors.Add(new MandatoryRouteLoopBuildError(MandatoryRouteLoopBuildErrorCode.MissingTerminalSet, string.Empty, "Terminal set is required."));
            if (tree == null) errors.Add(new MandatoryRouteLoopBuildError(MandatoryRouteLoopBuildErrorCode.MissingConnectorTree, string.Empty, "Connector tree is required."));
            if (horizontal == null) errors.Add(new MandatoryRouteLoopBuildError(MandatoryRouteLoopBuildErrorCode.MissingHorizontalBackbonePlan, string.Empty, "Horizontal backbone plan is required."));
            if (vertical == null) errors.Add(new MandatoryRouteLoopBuildError(MandatoryRouteLoopBuildErrorCode.MissingVerticalGatewayPlan, string.Empty, "Vertical gateway plan is required."));
            if (conflicts == null) errors.Add(new MandatoryRouteLoopBuildError(MandatoryRouteLoopBuildErrorCode.MissingConflictResolutionPlan, string.Empty, "Conflict resolution plan is required."));
            if (terminals != null && tree != null && horizontal != null && vertical != null && conflicts != null &&
                (!ReferenceEquals(tree.SourceTerminalSet, terminals) ||
                 !ReferenceEquals(horizontal.SourceConnectorTree, tree) ||
                 !ReferenceEquals(vertical.SourceHorizontalPlan, horizontal) ||
                 !ReferenceEquals(conflicts.SourceVerticalGatewayPlan, vertical) ||
                 !ReferenceEquals(tree.SourceRouteMaskLookup, horizontal.SourceRouteMaskLookup) ||
                 !ReferenceEquals(horizontal.SourceRouteMaskLookup, vertical.SourceRouteMaskLookup) ||
                 !ReferenceEquals(vertical.SourceRouteMaskLookup, conflicts.SourceRouteMaskLookup) ||
                 !ReferenceEquals(horizontal.SourceSiteSnapshot, vertical.SourceSiteSnapshot) ||
                 !ReferenceEquals(vertical.SourceSiteSnapshot, conflicts.SourceSiteSnapshot) ||
                 !ReferenceEquals(horizontal.SourceBiomePublication, vertical.SourceBiomePublication) ||
                 !ReferenceEquals(vertical.SourceBiomePublication, conflicts.SourceBiomePublication)))
                errors.Add(new MandatoryRouteLoopBuildError(MandatoryRouteLoopBuildErrorCode.SourceIdentityMismatch, string.Empty, "Input artifacts must preserve one exact source chain."));
            return errors;
        }

        private static List<MandatoryRouteLoopCandidate> CreateStarterCandidates(
            MandatoryRouteTerminalSet terminals,
            MandatoryConnectorTree tree,
            HorizontalBackbonePlan horizontal,
            VerticalGatewayPlan vertical,
            UpDownConflictResolutionPlan conflicts)
        {
            var mandatoryCells = new HashSet<SectorCoord>();
            foreach (var cell in horizontal.Segments.SelectMany(value => value.Cells)) mandatoryCells.Add(cell.Coord);
            foreach (var cell in vertical.GatewayPairs.SelectMany(value => value.SpanCells)) mandatoryCells.Add(cell);
            foreach (var cell in conflicts.Resolutions.SelectMany(value => value.InclusiveSpan)) mandatoryCells.Add(cell);
            var treePairs = new HashSet<string>(tree.TreeEdges.Select(value => PairKey(value.FromTerminalId, value.ToTerminalId)), StringComparer.Ordinal);
            var type4 = vertical.GatewayPairs.SelectMany(value => value.Type4JunctionCells).ToArray();
            var values = new List<MandatoryRouteLoopCandidate>();
            var ordinal = 0;
            foreach (var edge in tree.CandidateEdges.OrderBy(value => value.EdgeId))
            {
                if (treePairs.Contains(PairKey(edge.FromTerminalId, edge.ToTerminalId))) continue;
                terminals.TryGet(edge.FromTerminalId, out var from);
                terminals.TryGet(edge.ToTerminalId, out var to);
                var path = FindPath(from.ApproachSector, to.ApproachSector, mandatoryCells, horizontal.SourceSiteSnapshot);
                if (path == null) continue;
                var sourceSegments = horizontal.GetSegmentsForTerminal(edge.FromTerminalId)
                    .Concat(horizontal.GetSegmentsForTerminal(edge.ToTerminalId)).Select(value => value.SegmentId).Distinct().OrderBy(value => value).ToArray();
                var sourceSegmentSet = new HashSet<HorizontalBackboneSegmentId>(sourceSegments);
                var sourceGateways = vertical.GatewayPairs.Where(value => sourceSegmentSet.Contains(value.SourceSegmentId)).Select(value => value.GatewayId).OrderBy(value => value).ToArray();
                var shared = path.Count(value => mandatoryCells.Contains(value));
                values.Add(new MandatoryRouteLoopCandidate(
                    new MandatoryRouteLoopId("LOOP_" + ordinal.ToString("D2", CultureInfo.InvariantCulture) + "_" + edge.EdgeId.Value),
                    edge.FromTerminalId,
                    edge.ToTerminalId,
                    edge.EdgeId,
                    path,
                    sourceSegments,
                    sourceGateways,
                    type4,
                    from.ReservationId + "|" + to.ReservationId,
                    "BIOME_SEED_" + horizontal.SourceBiomePublication.Snapshot.Seed.ToString(CultureInfo.InvariantCulture),
                    checked(path.Count),
                    shared,
                    false,
                    false,
                    shared > 2,
                    shared <= 2));
                ordinal++;
            }
            return values;
        }

        private static List<SectorCoord> FindPath(SectorCoord start, SectorCoord end, ISet<SectorCoord> mandatoryCells, SiteReservationSnapshot site)
        {
            var queue = new Queue<SectorCoord>();
            var parent = new Dictionary<SectorCoord, SectorCoord>();
            var visited = new HashSet<SectorCoord> { start };
            queue.Enqueue(start);
            while (queue.Count != 0)
            {
                var current = queue.Dequeue();
                if (current == end) break;
                foreach (var next in Neighbors(current))
                {
                    if (next.X < 0 || next.X >= WorldGenConstants.SectorColumns || next.Y < 0 || next.Y >= WorldGenConstants.SectorRows || visited.Contains(next)) continue;
                    if (next != end && (mandatoryCells.Contains(next) || site.GetSector(next).IsReserved)) continue;
                    visited.Add(next);
                    parent.Add(next, current);
                    queue.Enqueue(next);
                }
            }
            if (!visited.Contains(end)) return null;
            var result = new List<SectorCoord>();
            var cursor = end;
            result.Add(cursor);
            while (cursor != start)
            {
                cursor = parent[cursor];
                result.Add(cursor);
            }
            result.Reverse();
            return result;
        }

        private static IEnumerable<SectorCoord> Neighbors(SectorCoord value)
        {
            yield return new SectorCoord(value.X - 1, value.Y);
            yield return new SectorCoord(value.X + 1, value.Y);
            yield return new SectorCoord(value.X, value.Y - 1);
            yield return new SectorCoord(value.X, value.Y + 1);
        }

        private static string PairKey(MandatoryRouteTerminalId left, MandatoryRouteTerminalId right) =>
            left.CompareTo(right) <= 0 ? left.Value + "\n" + right.Value : right.Value + "\n" + left.Value;

        private static MandatoryRouteLoopBuildResult Invalid(IEnumerable<MandatoryRouteLoopBuildError> errors) =>
            new MandatoryRouteLoopBuildResult(MandatoryRouteLoopBuildStatus.InvalidInput, null, null, errors);
    }
}
