using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalRegionGrower
    {
        public OptionalRegionGrowthResult Grow(
            GeneratedWorldData world,
            MandatoryRouteGraph graph,
            MandatoryRouteValidationReport validationReport,
            SiteReservationSnapshot siteReservations,
            BiomePatchValidationPublication biomePublication,
            OptionalAttachmentEnumerationResult attachments,
            string sourceMandatoryGraphDigest,
            OptionalRegionGrowthSettings settings)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            if (validationReport == null) throw new ArgumentNullException(nameof(validationReport));
            if (siteReservations == null) throw new ArgumentNullException(nameof(siteReservations));
            if (biomePublication == null) throw new ArgumentNullException(nameof(biomePublication));
            if (attachments == null) throw new ArgumentNullException(nameof(attachments));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (string.IsNullOrWhiteSpace(sourceMandatoryGraphDigest) ||
                !string.Equals(sourceMandatoryGraphDigest, sourceMandatoryGraphDigest.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Mandatory graph digest must be a canonical non-empty identity.", nameof(sourceMandatoryGraphDigest));

            ValidateSources(world, graph, validationReport, siteReservations, biomePublication, attachments);

            var mandatory = new HashSet<int>(graph.Cells.Select(value => value.SectorIndex));
            var candidates = new List<OptionalAttachmentCandidate>(attachments.Candidates);
            candidates.Sort((left, right) => left.AttachmentOrder.CompareTo(right.AttachmentOrder));
            ValidateCandidates(candidates, graph, attachments, mandatory);

            var regions = new List<OptionalRegion>();
            var publishedCells = new List<OptionalRegionCell>();
            var claimed = new HashSet<int>();
            var counters = new GrowthCounters();
            var rejectionCodes = new List<string>();

            foreach (var candidate in candidates)
            {
                if (regions.Count >= settings.MaxRegions)
                {
                    counters.RegionLimitSkipped++;
                    continue;
                }

                counters.AttemptedCandidates++;
                var targetDepth = settings.GetTargetDepth(candidate.AttachmentOrder).Value;
                if (!TryGrowRegion(
                    candidate, targetDepth, settings.MaxCellsPerRegion, mandatory, claimed,
                    siteReservations, biomePublication, counters, out var selected, out var depths,
                    out var rejectionCode))
                {
                    counters.RejectedCandidateCount++;
                    rejectionCodes.Add(rejectionCode);
                    continue;
                }

                var regionOrdinal = regions.Count;
                var regionId = new OptionalRegionId(
                    "OPT_REGION_" + regionOrdinal.ToString("D4", CultureInfo.InvariantCulture));
                var attachment = new OptionalRegionAttachment(
                    regionId,
                    candidate.AttachmentOrder,
                    candidate.MandatoryRouteSectorIndex,
                    candidate.MandatoryRouteSector,
                    candidate.MandatoryRouteNodeId,
                    candidate.EntrySectorIndex,
                    candidate.EntrySector,
                    candidate.DirectionDx,
                    candidate.DirectionDy,
                    candidate.InitialDepth);
                var cells = new List<OptionalRegionCell>();
                foreach (var sectorIndex in selected.OrderBy(value => value))
                {
                    var cell = new OptionalRegionCell(
                        regionId,
                        sectorIndex,
                        WorldGridIndex.ToCoordinate(sectorIndex),
                        new OptionalRegionDepth(depths[sectorIndex]),
                        sectorIndex == candidate.EntrySectorIndex,
                        false);
                    cells.Add(cell);
                    publishedCells.Add(cell);
                    claimed.Add(sectorIndex);
                }

                var maximumDepth = depths.Values.Max();
                var region = new OptionalRegion(
                    regionId,
                    attachment,
                    OptionalRegionAccessRule.Basic,
                    OptionalRewardTier.None,
                    OptionalReturnPolicy.BacktrackToAttachment,
                    cells,
                    new OptionalRegionDepth(maximumDepth));
                regions.Add(region);
                counters.AcceptedRegionCount++;
                counters.AcceptedCellCount += cells.Count;
                counters.IncrementDepth(maximumDepth);
            }

            var diagnostics = new OptionalRegionGrowthDiagnostics(
                candidates.Count,
                counters.AttemptedCandidates,
                counters.AcceptedRegionCount,
                counters.RejectedCandidateCount,
                counters.RegionLimitSkipped,
                counters.AcceptedCellCount,
                counters.RawCellProbes,
                counters.OutOfBoundsCellRejected,
                counters.MandatoryCellRejected,
                counters.AdditionalMandatoryBridgeRejected,
                counters.SiteReservationCellRejected,
                counters.BiomeReservedCellRejected,
                counters.ClaimedCellRejected,
                counters.DuplicateFrontierRejected,
                counters.HorizontalThroughCellRejected,
                counters.NoTargetDepthPathRejected,
                counters.Depth1RegionCount,
                counters.Depth2RegionCount,
                counters.Depth3RegionCount,
                counters.Depth4RegionCount,
                rejectionCodes);
            var snapshot = new OptionalRegionSnapshot(
                regions,
                publishedCells,
                mandatory,
                graph.NodeCount,
                graph.DirectedEdgeCount,
                graph.CellCount,
                sourceMandatoryGraphDigest);
            return new OptionalRegionGrowthResult(
                snapshot,
                diagnostics,
                attachments.CanonicalDigest,
                sourceMandatoryGraphDigest,
                settings);
        }

        private static bool TryGrowRegion(
            OptionalAttachmentCandidate candidate,
            int targetDepth,
            int maxCells,
            HashSet<int> mandatory,
            HashSet<int> claimed,
            SiteReservationSnapshot site,
            BiomePatchValidationPublication biome,
            GrowthCounters counters,
            out HashSet<int> selected,
            out Dictionary<int, int> depths,
            out string rejectionCode)
        {
            selected = null;
            depths = null;
            counters.RawCellProbes++;
            if (!TryValidateStaticCell(candidate.EntrySectorIndex, candidate.MandatoryRouteSectorIndex, true,
                mandatory, claimed, site, biome, counters, out rejectionCode))
                return false;

            var root = new PathNode(candidate.EntrySectorIndex, 1, null);
            PathNode target = root;
            if (targetDepth > 1)
            {
                target = FindTargetPath(
                    root, targetDepth, candidate.MandatoryRouteSectorIndex, mandatory, claimed,
                    site, biome, counters);
                if (target == null)
                {
                    counters.NoTargetDepthPathRejected++;
                    rejectionCode = "NO_TARGET_DEPTH_PATH";
                    return false;
                }
            }

            selected = ReconstructPath(target);
            depths = ComputeDepths(candidate.EntrySectorIndex, selected);
            FillRegion(
                candidate.EntrySectorIndex, candidate.MandatoryRouteSectorIndex, targetDepth, maxCells,
                selected, depths, mandatory, claimed, site, biome, counters);
            depths = ComputeDepths(candidate.EntrySectorIndex, selected);

            if (depths.Values.Max() != targetDepth ||
                CountMandatoryBridges(selected, mandatory) != 1 ||
                HasHorizontalThrough(selected))
            {
                selected = null;
                depths = null;
                counters.NoTargetDepthPathRejected++;
                rejectionCode = "NO_TARGET_DEPTH_PATH";
                return false;
            }

            rejectionCode = string.Empty;
            return true;
        }

        private static PathNode FindTargetPath(
            PathNode root,
            int targetDepth,
            int sourceMandatoryIndex,
            HashSet<int> mandatory,
            HashSet<int> claimed,
            SiteReservationSnapshot site,
            BiomePatchValidationPublication biome,
            GrowthCounters counters)
        {
            var current = new List<PathNode> { root };
            var discovered = new HashSet<int> { root.SectorIndex };
            for (var depth = 2; depth <= targetDepth; depth++)
            {
                var frontier = new List<Frontier>();
                foreach (var parent in current)
                    AddFrontiers(parent.SectorIndex, depth, frontier);
                frontier.Sort(CompareFrontiers);
                var next = new List<PathNode>();
                foreach (var probe in frontier)
                {
                    counters.RawCellProbes++;
                    if (probe.ChildSectorIndex < 0)
                    {
                        counters.OutOfBoundsCellRejected++;
                        continue;
                    }
                    if (!discovered.Add(probe.ChildSectorIndex))
                    {
                        counters.DuplicateFrontierRejected++;
                        continue;
                    }
                    var parent = current.Find(value => value.SectorIndex == probe.ParentSectorIndex);
                    var path = ReconstructPath(parent);
                    if (!TryValidateStaticCell(probe.ChildSectorIndex, sourceMandatoryIndex, false,
                        mandatory, claimed, site, biome, counters, out _))
                        continue;
                    if (WouldCreateHorizontalThrough(path, probe.ChildSectorIndex))
                    {
                        counters.HorizontalThroughCellRejected++;
                        continue;
                    }
                    var node = new PathNode(probe.ChildSectorIndex, depth, parent);
                    next.Add(node);
                    if (depth == targetDepth) return node;
                }
                if (next.Count == 0) return null;
                next.Sort((left, right) => left.SectorIndex.CompareTo(right.SectorIndex));
                current = next;
            }
            return null;
        }

        private static void FillRegion(
            int entrySectorIndex,
            int sourceMandatoryIndex,
            int targetDepth,
            int maxCells,
            HashSet<int> selected,
            Dictionary<int, int> depths,
            HashSet<int> mandatory,
            HashSet<int> claimed,
            SiteReservationSnapshot site,
            BiomePatchValidationPublication biome,
            GrowthCounters counters)
        {
            var processed = new HashSet<FrontierKey>();
            var seenChildren = new HashSet<int>(selected);
            while (selected.Count < maxCells)
            {
                var frontier = new List<Frontier>();
                foreach (var parentIndex in selected.OrderBy(value => value))
                {
                    if (depths[parentIndex] >= targetDepth) continue;
                    AddFrontiers(parentIndex, depths[parentIndex] + 1, frontier);
                }
                frontier.RemoveAll(value => processed.Contains(value.Key));
                if (frontier.Count == 0) break;
                frontier.Sort(CompareFrontiers);
                var probe = frontier[0];
                processed.Add(probe.Key);
                counters.RawCellProbes++;
                if (probe.ChildSectorIndex < 0)
                {
                    counters.OutOfBoundsCellRejected++;
                    continue;
                }
                if (!seenChildren.Add(probe.ChildSectorIndex))
                {
                    counters.DuplicateFrontierRejected++;
                    continue;
                }
                if (!TryValidateStaticCell(probe.ChildSectorIndex, sourceMandatoryIndex, false,
                    mandatory, claimed, site, biome, counters, out _))
                    continue;
                if (WouldCreateHorizontalThrough(selected, probe.ChildSectorIndex))
                {
                    counters.HorizontalThroughCellRejected++;
                    continue;
                }
                selected.Add(probe.ChildSectorIndex);
                depths = ComputeDepths(entrySectorIndex, selected);
            }
        }

        private static bool TryValidateStaticCell(
            int sectorIndex,
            int sourceMandatoryIndex,
            bool isEntry,
            HashSet<int> mandatory,
            HashSet<int> claimed,
            SiteReservationSnapshot site,
            BiomePatchValidationPublication biome,
            GrowthCounters counters,
            out string rejectionCode)
        {
            if (sectorIndex < 0 || sectorIndex >= WorldGenConstants.SectorCount)
            {
                counters.OutOfBoundsCellRejected++;
                rejectionCode = "OUT_OF_BOUNDS";
                return false;
            }
            if (mandatory.Contains(sectorIndex))
            {
                counters.MandatoryCellRejected++;
                rejectionCode = "MANDATORY";
                return false;
            }
            var mandatoryNeighbors = MandatoryNeighbors(sectorIndex, mandatory);
            var bridgeValid = isEntry
                ? mandatoryNeighbors.Count == 1 && mandatoryNeighbors[0] == sourceMandatoryIndex
                : mandatoryNeighbors.Count == 0;
            if (!bridgeValid)
            {
                counters.AdditionalMandatoryBridgeRejected++;
                rejectionCode = "ADDITIONAL_MANDATORY_BRIDGE";
                return false;
            }
            if (site.GetSector(sectorIndex).IsReserved)
            {
                counters.SiteReservationCellRejected++;
                rejectionCode = "SITE_RESERVATION";
                return false;
            }
            var biomeCell = biome.WorldWithBiomeAssignments.GetCell(sectorIndex);
            if (string.IsNullOrEmpty(biomeCell.PrimaryBiomeId) || string.IsNullOrEmpty(biomeCell.PatchId))
            {
                counters.BiomeReservedCellRejected++;
                rejectionCode = "BIOME_RESERVED";
                return false;
            }
            if (claimed.Contains(sectorIndex))
            {
                counters.ClaimedCellRejected++;
                rejectionCode = "CLAIMED";
                return false;
            }
            rejectionCode = string.Empty;
            return true;
        }

        private static void ValidateSources(
            GeneratedWorldData world,
            MandatoryRouteGraph graph,
            MandatoryRouteValidationReport report,
            SiteReservationSnapshot site,
            BiomePatchValidationPublication biome,
            OptionalAttachmentEnumerationResult attachments)
        {
            if (!ReferenceEquals(report.SourceGraph, graph) || !ReferenceEquals(report.SourceWorld, world) ||
                !ReferenceEquals(report.SourceTerminalSet, graph.SourceTerminalSet))
                throw new ArgumentException("Validation report must preserve the exact graph and world sources.", nameof(report));
            if (!ReferenceEquals(graph.RouteStampedWorld, world))
                throw new ArgumentException("World must be the route-stamped graph publication.", nameof(world));
            if (!ReferenceEquals(graph.SourceTerminalSet.SourceSiteSnapshot, site))
                throw new ArgumentException("Site snapshot must match the graph source.", nameof(site));
            if (!ReferenceEquals(graph.SourceTerminalSet.SourceBiomePublication, biome))
                throw new ArgumentException("Biome publication must match the graph source.", nameof(biome));
            if (!report.IsValid || report.Errors.Count != 0 || report.Warnings.Count != 0 ||
                report.Violations.Count != 0 || !string.Equals(report.PassId, "PASS_ROUTE", StringComparison.Ordinal))
                throw new ArgumentException("Mandatory route validation must be approved without violations.", nameof(report));
            if (graph.NodeCount != OptionalRegionSnapshot.RequiredMandatoryNodeCount ||
                graph.DirectedEdgeCount != OptionalRegionSnapshot.RequiredMandatoryDirectedEdgeCount ||
                graph.CellCount != OptionalRegionSnapshot.RequiredMandatoryRouteCellCount)
                throw new ArgumentException("Mandatory graph identity must remain 47/96/47.", nameof(graph));
            if (attachments.MandatoryRouteGraphNodeCount != graph.NodeCount ||
                attachments.MandatoryRouteGraphDirectedEdgeCount != graph.DirectedEdgeCount ||
                attachments.MandatoryRouteCellCount != graph.CellCount)
                throw new ArgumentException("Attachment graph counts must match the mandatory graph.", nameof(attachments));
            if (world.Seed != site.Seed || world.Seed != biome.Snapshot.Seed)
                throw new ArgumentException("All source seeds must match.");
        }

        private static void ValidateCandidates(
            IReadOnlyList<OptionalAttachmentCandidate> candidates,
            MandatoryRouteGraph graph,
            OptionalAttachmentEnumerationResult attachments,
            HashSet<int> mandatory)
        {
            var entries = new HashSet<int>();
            for (var index = 0; index < candidates.Count; index++)
            {
                var candidate = candidates[index];
                if (candidate == null || candidate.AttachmentOrder != index ||
                    !candidate.CandidateId.TryGetOrdinal(out var ordinal) || ordinal != index)
                    throw new ArgumentException("Attachment candidates must be canonical and contiguous.", nameof(attachments));
                if (!entries.Add(candidate.EntrySectorIndex) || mandatory.Contains(candidate.EntrySectorIndex))
                    throw new ArgumentException("Attachment entry sectors must be unique and optional.", nameof(attachments));
                if (!graph.TryGetNode(candidate.MandatoryRouteSectorIndex, out var node) ||
                    node.NodeId != candidate.MandatoryRouteNodeId || node.Coordinate != candidate.MandatoryRouteSector)
                    throw new ArgumentException("Attachment source node must belong to the mandatory graph.", nameof(attachments));
            }
            var reconstructed = new OptionalAttachmentEnumerationResult(
                candidates,
                attachments.Diagnostics,
                mandatory,
                graph.NodeCount,
                graph.DirectedEdgeCount,
                graph.CellCount);
            if (!string.Equals(reconstructed.CanonicalDigest, attachments.CanonicalDigest, StringComparison.Ordinal))
                throw new ArgumentException("Attachment candidate digest is inconsistent.", nameof(attachments));
        }

        private static HashSet<int> ReconstructPath(PathNode node)
        {
            var result = new HashSet<int>();
            while (node != null)
            {
                result.Add(node.SectorIndex);
                node = node.Parent;
            }
            return result;
        }

        private static Dictionary<int, int> ComputeDepths(int entrySectorIndex, HashSet<int> selected)
        {
            var result = new Dictionary<int, int> { { entrySectorIndex, 1 } };
            var queue = new Queue<int>();
            queue.Enqueue(entrySectorIndex);
            while (queue.Count > 0)
            {
                var parent = queue.Dequeue();
                foreach (var neighbor in CardinalNeighbors(parent))
                {
                    if (neighbor < 0 || !selected.Contains(neighbor) || result.ContainsKey(neighbor)) continue;
                    result.Add(neighbor, result[parent] + 1);
                    queue.Enqueue(neighbor);
                }
            }
            if (result.Count != selected.Count) throw new InvalidOperationException("Optional region must be connected.");
            return result;
        }

        private static bool WouldCreateHorizontalThrough(HashSet<int> selected, int child)
        {
            var left = WorldGridIndex.GetLeftIndex(child);
            var right = WorldGridIndex.GetRightIndex(child);
            if (left >= 0 && right >= 0 && selected.Contains(left) && selected.Contains(right)) return true;
            foreach (var existing in selected)
            {
                var existingLeft = WorldGridIndex.GetLeftIndex(existing);
                var existingRight = WorldGridIndex.GetRightIndex(existing);
                if (existingLeft == child && existingRight >= 0 && selected.Contains(existingRight)) return true;
                if (existingRight == child && existingLeft >= 0 && selected.Contains(existingLeft)) return true;
            }
            return false;
        }

        private static bool HasHorizontalThrough(HashSet<int> selected)
        {
            return selected.Any(index =>
            {
                var left = WorldGridIndex.GetLeftIndex(index);
                var right = WorldGridIndex.GetRightIndex(index);
                return left >= 0 && right >= 0 && selected.Contains(left) && selected.Contains(right);
            });
        }

        private static int CountMandatoryBridges(HashSet<int> selected, HashSet<int> mandatory)
        {
            var count = 0;
            foreach (var sector in selected)
                foreach (var neighbor in CardinalNeighbors(sector))
                    if (neighbor >= 0 && mandatory.Contains(neighbor)) count++;
            return count;
        }

        private static List<int> MandatoryNeighbors(int sectorIndex, HashSet<int> mandatory)
        {
            var result = new List<int>();
            foreach (var neighbor in CardinalNeighbors(sectorIndex))
                if (neighbor >= 0 && mandatory.Contains(neighbor)) result.Add(neighbor);
            result.Sort();
            return result;
        }

        private static int[] CardinalNeighbors(int sectorIndex)
        {
            return new[]
            {
                WorldGridIndex.GetLeftIndex(sectorIndex),
                WorldGridIndex.GetRightIndex(sectorIndex),
                WorldGridIndex.GetUpIndex(sectorIndex),
                WorldGridIndex.GetDownIndex(sectorIndex)
            };
        }

        private static void AddFrontiers(int parentSectorIndex, int depth, List<Frontier> target)
        {
            var coordinate = WorldGridIndex.ToCoordinate(parentSectorIndex);
            foreach (var direction in CreateDirections())
            {
                var x = coordinate.X + direction.X;
                var y = coordinate.Y + direction.Y;
                var child = x < 0 || x >= WorldGenConstants.SectorColumns || y < 0 || y >= WorldGenConstants.SectorRows
                    ? -1
                    : WorldGridIndex.ToIndex(new SectorCoord(x, y));
                target.Add(new Frontier(depth, parentSectorIndex, direction.Order, child));
            }
        }

        private static int CompareFrontiers(Frontier left, Frontier right)
        {
            var depth = left.Depth.CompareTo(right.Depth);
            if (depth != 0) return depth;
            var parent = left.ParentSectorIndex.CompareTo(right.ParentSectorIndex);
            if (parent != 0) return parent;
            var direction = left.DirectionOrder.CompareTo(right.DirectionOrder);
            return direction != 0 ? direction : left.ChildSectorIndex.CompareTo(right.ChildSectorIndex);
        }

        private static Direction[] CreateDirections()
        {
            return new[]
            {
                new Direction(-1, 0, 0),
                new Direction(1, 0, 1),
                new Direction(0, 1, 2),
                new Direction(0, -1, 3)
            };
        }

        private sealed class PathNode
        {
            public PathNode(int sectorIndex, int depth, PathNode parent)
            {
                SectorIndex = sectorIndex;
                Depth = depth;
                Parent = parent;
            }
            public int SectorIndex { get; }
            public int Depth { get; }
            public PathNode Parent { get; }
        }

        private readonly struct Direction
        {
            public Direction(int x, int y, int order) { X = x; Y = y; Order = order; }
            public int X { get; }
            public int Y { get; }
            public int Order { get; }
        }

        private readonly struct Frontier
        {
            public Frontier(int depth, int parentSectorIndex, int directionOrder, int childSectorIndex)
            {
                Depth = depth;
                ParentSectorIndex = parentSectorIndex;
                DirectionOrder = directionOrder;
                ChildSectorIndex = childSectorIndex;
            }
            public int Depth { get; }
            public int ParentSectorIndex { get; }
            public int DirectionOrder { get; }
            public int ChildSectorIndex { get; }
            public FrontierKey Key => new FrontierKey(ParentSectorIndex, DirectionOrder, ChildSectorIndex);
        }

        private readonly struct FrontierKey : IEquatable<FrontierKey>
        {
            public FrontierKey(int parent, int direction, int child) { Parent = parent; Direction = direction; Child = child; }
            public int Parent { get; }
            public int Direction { get; }
            public int Child { get; }
            public bool Equals(FrontierKey other) => Parent == other.Parent && Direction == other.Direction && Child == other.Child;
            public override bool Equals(object obj) => obj is FrontierKey other && Equals(other);
            public override int GetHashCode()
            {
                unchecked { return ((Parent * 397) ^ Direction) * 397 ^ Child; }
            }
        }

        private sealed class GrowthCounters
        {
            public int AttemptedCandidates;
            public int AcceptedRegionCount;
            public int RejectedCandidateCount;
            public int RegionLimitSkipped;
            public int AcceptedCellCount;
            public int RawCellProbes;
            public int OutOfBoundsCellRejected;
            public int MandatoryCellRejected;
            public int AdditionalMandatoryBridgeRejected;
            public int SiteReservationCellRejected;
            public int BiomeReservedCellRejected;
            public int ClaimedCellRejected;
            public int DuplicateFrontierRejected;
            public int HorizontalThroughCellRejected;
            public int NoTargetDepthPathRejected;
            public int Depth1RegionCount;
            public int Depth2RegionCount;
            public int Depth3RegionCount;
            public int Depth4RegionCount;

            public void IncrementDepth(int depth)
            {
                switch (depth)
                {
                    case 1: Depth1RegionCount++; break;
                    case 2: Depth2RegionCount++; break;
                    case 3: Depth3RegionCount++; break;
                    case 4: Depth4RegionCount++; break;
                    default: throw new ArgumentOutOfRangeException(nameof(depth));
                }
            }
        }
    }
}
