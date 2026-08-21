using System;
using System.Collections.Generic;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalAttachmentEnumerator
    {
        public OptionalAttachmentEnumerationResult Enumerate(
            GeneratedWorldData world,
            MandatoryRouteGraph graph,
            MandatoryRouteValidationReport validationReport,
            SiteReservationSnapshot siteReservations,
            BiomePatchValidationPublication biomePublication,
            OptionalAttachmentEnumerationSettings settings)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            if (validationReport == null) throw new ArgumentNullException(nameof(validationReport));
            if (siteReservations == null) throw new ArgumentNullException(nameof(siteReservations));
            if (biomePublication == null) throw new ArgumentNullException(nameof(biomePublication));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            ValidateSources(world, graph, validationReport, siteReservations, biomePublication);

            var mandatoryIndices = new HashSet<int>();
            foreach (var cell in graph.Cells)
            {
                if (cell == null || !mandatoryIndices.Add(cell.SectorIndex))
                    throw new ArgumentException("Mandatory graph cells must be non-null and unique.", nameof(graph));
            }

            var terminalIndices = new HashSet<int>();
            foreach (var terminal in graph.SourceTerminalSet.Terminals)
            {
                terminalIndices.Add(WorldGridIndex.ToIndex(terminal.ApproachSector));
            }

            var nodes = new List<MandatoryRouteGraphNode>(graph.Nodes);
            nodes.Sort(CompareNodes);
            var directions = new[]
            {
                new Direction(-1, 0),
                new Direction(1, 0),
                new Direction(0, 1),
                new Direction(0, -1)
            };

            var accepted = new List<OptionalAttachmentCandidate>();
            var acceptedEntries = new HashSet<int>();
            var rejectionCodes = new List<string>();
            var rawNeighborProbes = 0;
            var outOfBoundsRejected = 0;
            var mandatoryRejected = 0;
            var terminalRejected = 0;
            var siteReservationRejected = 0;
            var biomeReservedRejected = 0;
            var duplicateEntryRejected = 0;

            foreach (var node in nodes)
            {
                if (accepted.Count >= settings.MaxCandidates) break;
                foreach (var direction in directions)
                {
                    if (accepted.Count >= settings.MaxCandidates) break;
                    rawNeighborProbes++;
                    var entry = new SectorCoord(
                        node.Coordinate.X + direction.X,
                        node.Coordinate.Y + direction.Y);
                    if (entry.X < 0 || entry.X >= WorldGenConstants.SectorColumns ||
                        entry.Y < 0 || entry.Y >= WorldGenConstants.SectorRows)
                    {
                        outOfBoundsRejected++;
                        rejectionCodes.Add("OUT_OF_BOUNDS");
                        continue;
                    }

                    var entryIndex = WorldGridIndex.ToIndex(entry);
                    if (mandatoryIndices.Contains(entryIndex))
                    {
                        mandatoryRejected++;
                        rejectionCodes.Add("MANDATORY");
                        continue;
                    }

                    if (settings.ExcludeMandatoryTerminals && terminalIndices.Contains(entryIndex))
                    {
                        terminalRejected++;
                        rejectionCodes.Add("TERMINAL");
                        continue;
                    }

                    if (settings.ExcludeSiteReservations && siteReservations.GetSector(entryIndex).IsReserved)
                    {
                        siteReservationRejected++;
                        rejectionCodes.Add("SITE_RESERVATION");
                        continue;
                    }

                    if (settings.ExcludeBiomeReservedOrInactive &&
                        IsBiomeReservedOrInactive(biomePublication.WorldWithBiomeAssignments.GetCell(entryIndex)))
                    {
                        biomeReservedRejected++;
                        rejectionCodes.Add("BIOME_RESERVED");
                        continue;
                    }

                    if (acceptedEntries.Contains(entryIndex))
                    {
                        duplicateEntryRejected++;
                        rejectionCodes.Add("DUPLICATE_ENTRY");
                        continue;
                    }

                    var order = accepted.Count;
                    accepted.Add(new OptionalAttachmentCandidate(
                        OptionalAttachmentCandidateId.FromOrdinal(order),
                        order,
                        node.SectorIndex,
                        node.Coordinate,
                        node.NodeId,
                        entryIndex,
                        entry,
                        direction.X,
                        direction.Y,
                        new OptionalRegionDepth(1)));
                    acceptedEntries.Add(entryIndex);
                }
            }

            var diagnostics = new OptionalAttachmentEnumerationDiagnostics(
                rawNeighborProbes,
                outOfBoundsRejected,
                mandatoryRejected,
                terminalRejected,
                siteReservationRejected,
                biomeReservedRejected,
                duplicateEntryRejected,
                accepted.Count,
                rejectionCodes);
            return new OptionalAttachmentEnumerationResult(
                accepted,
                diagnostics,
                mandatoryIndices,
                graph.NodeCount,
                graph.DirectedEdgeCount,
                graph.CellCount);
        }

        private static void ValidateSources(
            GeneratedWorldData world,
            MandatoryRouteGraph graph,
            MandatoryRouteValidationReport validationReport,
            SiteReservationSnapshot siteReservations,
            BiomePatchValidationPublication biomePublication)
        {
            if (!ReferenceEquals(validationReport.SourceGraph, graph) ||
                !ReferenceEquals(validationReport.SourceWorld, world) ||
                !ReferenceEquals(validationReport.SourceTerminalSet, graph.SourceTerminalSet))
                throw new ArgumentException("Validation report must preserve the exact graph and world sources.", nameof(validationReport));
            if (!ReferenceEquals(graph.RouteStampedWorld, world))
                throw new ArgumentException("World must be the exact route-stamped graph publication.", nameof(world));
            if (!ReferenceEquals(graph.SourceTerminalSet.SourceSiteSnapshot, siteReservations))
                throw new ArgumentException("Site reservation snapshot must match the graph source.", nameof(siteReservations));
            if (!ReferenceEquals(graph.SourceTerminalSet.SourceBiomePublication, biomePublication))
                throw new ArgumentException("Biome publication must match the graph source.", nameof(biomePublication));
            if (!validationReport.IsValid || validationReport.Errors.Count != 0 ||
                validationReport.Warnings.Count != 0 || validationReport.Violations.Count != 0 ||
                !string.Equals(validationReport.PassId, "PASS_ROUTE", StringComparison.Ordinal))
                throw new ArgumentException("Mandatory route validation must be approved without violations.", nameof(validationReport));
            if (graph.NodeCount != OptionalRegionSnapshot.RequiredMandatoryNodeCount ||
                graph.DirectedEdgeCount != OptionalRegionSnapshot.RequiredMandatoryDirectedEdgeCount ||
                graph.CellCount != OptionalRegionSnapshot.RequiredMandatoryRouteCellCount)
                throw new ArgumentException("Mandatory graph identity must remain 47/96/47.", nameof(graph));
            if (world.Seed != siteReservations.Seed || world.Seed != biomePublication.Snapshot.Seed)
                throw new ArgumentException("All source seeds must match.");
        }

        private static bool IsBiomeReservedOrInactive(SectorCell cell)
        {
            return string.IsNullOrEmpty(cell.PrimaryBiomeId) || string.IsNullOrEmpty(cell.PatchId);
        }

        private static int CompareNodes(MandatoryRouteGraphNode left, MandatoryRouteGraphNode right)
        {
            var distance = left.ShortestDistanceFromStart.CompareTo(right.ShortestDistanceFromStart);
            if (distance != 0) return distance;
            var sector = left.SectorIndex.CompareTo(right.SectorIndex);
            return sector != 0 ? sector : left.NodeId.CompareTo(right.NodeId);
        }

        private readonly struct Direction
        {
            public Direction(int x, int y)
            {
                X = x;
                Y = y;
            }

            public int X { get; }
            public int Y { get; }
        }
    }
}
