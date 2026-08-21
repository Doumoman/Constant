using System;
using System.Collections.Generic;
using System.Globalization;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryConnectorTreeBuilder
    {
        public MandatoryConnectorTreeBuildResult Build(MandatoryRouteTerminalSet terminalSet, MandatoryRouteMaskLookup routeMaskLookup)
        {
            var errors = new List<MandatoryConnectorTreeBuildError>();
            if (terminalSet == null) errors.Add(Error(MandatoryConnectorTreeBuildErrorCode.MissingInput, "TERMINAL_SET", string.Empty, -1, "Mandatory terminal set is required."));
            if (routeMaskLookup == null) errors.Add(Error(MandatoryConnectorTreeBuildErrorCode.MissingInput, "ROUTE_MASK_LOOKUP", string.Empty, -1, "Mandatory route mask lookup is required."));
            if (terminalSet != null) ValidateTerminals(terminalSet, errors);
            if (routeMaskLookup != null) ValidateLookup(routeMaskLookup, errors);
            if (errors.Count > 0) return Invalid(errors);

            var terminals = new List<MandatoryRouteTerminal>(terminalSet.Terminals);
            terminals.Sort(CompareTerminals);
            var candidates = new List<MandatoryConnectorCandidateEdge>();
            try
            {
                var ordinal = 0;
                for (var left = 0; left < terminals.Count; left++)
                    for (var right = left + 1; right < terminals.Count; right++)
                        candidates.Add(CreateCandidate(terminals[left], terminals[right], ordinal++));
            }
            catch (OverflowException)
            {
                errors.Add(Error(MandatoryConnectorTreeBuildErrorCode.InvalidEdgeCost, string.Empty, string.Empty, -1, "Connector edge cost overflowed checked arithmetic."));
            }
            catch (ArgumentException)
            {
                errors.Add(Error(MandatoryConnectorTreeBuildErrorCode.InvalidEdgeCost, string.Empty, string.Empty, -1, "Connector candidate construction failed structural validation."));
            }
            if (candidates.Count != 21) errors.Add(Error(MandatoryConnectorTreeBuildErrorCode.CandidateEdgeCountMismatch, string.Empty, string.Empty, -1, "Complete graph must contain exactly 21 candidates."));
            if (errors.Count > 0) return Invalid(errors);
            candidates.Sort(CompareCandidates);

            var parent = new int[7];
            for (var index = 0; index < parent.Length; index++) parent[index] = index;
            var selected = new List<MandatoryConnectorCandidateEdge>();
            foreach (var candidate in candidates)
            {
                var from = Find(parent, candidate.FromTerminalOrder);
                var to = Find(parent, candidate.ToTerminalOrder);
                if (from == to) continue;
                parent[to] = from;
                selected.Add(CopyAsTreeEdge(candidate));
                if (selected.Count == 6) break;
            }
            if (selected.Count != 6) errors.Add(Error(MandatoryConnectorTreeBuildErrorCode.TreeEdgeCountMismatch, string.Empty, string.Empty, -1, "Minimum connector tree must contain exactly six edges."));
            var roots = new HashSet<int>();
            for (var index = 0; index < parent.Length; index++) roots.Add(Find(parent, index));
            if (roots.Count != 1) errors.Add(Error(MandatoryConnectorTreeBuildErrorCode.DisconnectedTree, string.Empty, string.Empty, -1, "Minimum connector tree must be connected."));
            var covered = new HashSet<MandatoryRouteTerminalId>();
            foreach (var edge in selected) { covered.Add(edge.FromTerminalId); covered.Add(edge.ToTerminalId); }
            if (covered.Count != 7) errors.Add(Error(MandatoryConnectorTreeBuildErrorCode.MissingTerminalCoverage, string.Empty, string.Empty, -1, "Minimum connector tree must cover all terminals."));
            if (errors.Count > 0) return Invalid(errors);

            var tree = new MandatoryConnectorTree(terminalSet, routeMaskLookup, candidates, selected);
            var total = 0;
            var shared = 0;
            foreach (var edge in selected) total = checked(total + edge.Cost.TotalCost);
            foreach (var edge in candidates) if (edge.Cost.SharedApproachPenalty != 0) shared++;
            var diagnostics = new MandatoryConnectorTreeDiagnostics(7, 1, 6, 3, 21, 6, total, 1, 7, shared, 0, 0);
            return new MandatoryConnectorTreeBuildResult(MandatoryConnectorTreeBuildStatus.Completed, tree, diagnostics, Array.Empty<MandatoryConnectorTreeBuildError>());
        }

        internal static List<MandatoryConnectorTreeBuildError> SortAndDedupe(IEnumerable<MandatoryConnectorTreeBuildError> source)
        {
            var values = new List<MandatoryConnectorTreeBuildError>();
            foreach (var error in source) if (error != null) values.Add(error);
            values.Sort(MandatoryConnectorTreeBuildError.Compare);
            var result = new List<MandatoryConnectorTreeBuildError>();
            foreach (var error in values)
                if (result.Count == 0 || MandatoryConnectorTreeBuildError.Compare(result[result.Count - 1], error) != 0) result.Add(error);
            return result;
        }

        private static void ValidateTerminals(MandatoryRouteTerminalSet set, ICollection<MandatoryConnectorTreeBuildError> errors)
        {
            if (set.Terminals == null || set.TerminalCount != 7) errors.Add(Error(MandatoryConnectorTreeBuildErrorCode.TerminalCountMismatch, string.Empty, string.Empty, -1, "Terminal set must contain exactly seven terminals."));
            if (set.Terminals == null) return;
            var ids = new HashSet<MandatoryRouteTerminalId>();
            var orders = new HashSet<int>();
            var starts = 0;
            var sites = 0;
            foreach (var terminal in set.Terminals)
            {
                if (terminal == null) { errors.Add(Error(MandatoryConnectorTreeBuildErrorCode.InvalidTerminalSet, string.Empty, string.Empty, -1, "Terminal cannot be null.")); continue; }
                if (terminal.Kind == MandatoryRouteTerminalKind.Start) starts++; else if (terminal.Kind == MandatoryRouteTerminalKind.SiteEntry) sites++;
                if (!terminal.TerminalId.IsValid || !ids.Add(terminal.TerminalId) || !orders.Add(terminal.TerminalOrder)) errors.Add(Error(MandatoryConnectorTreeBuildErrorCode.TerminalIdentityMismatch, terminal.TerminalId.Value, string.Empty, -1, "Terminal identities and orders must be unique."));
                if (!terminal.Required || !terminal.ReturnPathRequired || !IsWorldSector(terminal.ApproachSector)) errors.Add(Error(MandatoryConnectorTreeBuildErrorCode.InvalidTerminalSet, terminal.TerminalId.Value, string.Empty, SectorIndex(terminal.ApproachSector), "Terminal mandatory flags and approach sector must be valid."));
            }
            for (var index = 0; index < 7; index++) if (!orders.Contains(index)) errors.Add(Error(MandatoryConnectorTreeBuildErrorCode.TerminalIdentityMismatch, index.ToString(CultureInfo.InvariantCulture), string.Empty, -1, "Terminal orders must be exact 0..6."));
            if (starts != 1 || sites != 6) errors.Add(Error(MandatoryConnectorTreeBuildErrorCode.TerminalCountMismatch, starts.ToString(CultureInfo.InvariantCulture), sites.ToString(CultureInfo.InvariantCulture), -1, "Terminal kinds must be one Start and six SiteEntry."));
        }

        private static void ValidateLookup(MandatoryRouteMaskLookup lookup, ICollection<MandatoryConnectorTreeBuildError> errors)
        {
            if (lookup.Records == null || lookup.Count != 3 || lookup.Type1 == null || lookup.Type2 == null || lookup.Type3 == null ||
                lookup.Type1.Kind != MandatoryRouteMaskKind.Type1 || lookup.Type1.OpenMask != MandatoryRouteOpenMask.Type1Horizontal ||
                lookup.Type2.Kind != MandatoryRouteMaskKind.Type2 || lookup.Type2.OpenMask != MandatoryRouteOpenMask.Type2Down ||
                lookup.Type3.Kind != MandatoryRouteMaskKind.Type3 || lookup.Type3.OpenMask != MandatoryRouteOpenMask.Type3Up)
                errors.Add(Error(MandatoryConnectorTreeBuildErrorCode.InvalidRouteMaskLookup, string.Empty, string.Empty, -1, "Route mask lookup must be exact Type1, Type2, Type3."));
        }

        private static MandatoryConnectorCandidateEdge CreateCandidate(MandatoryRouteTerminal first, MandatoryRouteTerminal second, int ordinal)
        {
            var from = CompareTerminals(first, second) <= 0 ? first : second;
            var to = ReferenceEquals(from, first) ? second : first;
            var idFirst = from.TerminalId.CompareTo(to.TerminalId) < 0 ? from.TerminalId.Value : to.TerminalId.Value;
            var idSecond = from.TerminalId.CompareTo(to.TerminalId) < 0 ? to.TerminalId.Value : from.TerminalId.Value;
            var edgeId = new MandatoryConnectorEdgeId("EDGE_" + ordinal.ToString("D2", CultureInfo.InvariantCulture) + "_" + idFirst + "__TO__" + idSecond);
            var distance = checked(Math.Abs(from.ApproachSector.X - to.ApproachSector.X) + Math.Abs(from.ApproachSector.Y - to.ApproachSector.Y));
            var spread = Math.Abs(from.TerminalOrder - to.TerminalOrder);
            var kind = from.Kind == MandatoryRouteTerminalKind.Start || to.Kind == MandatoryRouteTerminalKind.Start ? 0 : 3;
            var shared = from.ApproachSector == to.ApproachSector ? 100000 : 0;
            return new MandatoryConnectorCandidateEdge(edgeId, from.TerminalId, to.TerminalId, from.TerminalOrder, to.TerminalOrder, from.ApproachSector, to.ApproachSector, new MandatoryConnectorEdgeCost(distance, spread, kind, shared), false);
        }

        private static MandatoryConnectorCandidateEdge CopyAsTreeEdge(MandatoryConnectorCandidateEdge edge) =>
            new MandatoryConnectorCandidateEdge(edge.EdgeId, edge.FromTerminalId, edge.ToTerminalId, edge.FromTerminalOrder, edge.ToTerminalOrder, edge.FromApproachSector, edge.ToApproachSector, edge.Cost, true);
        private static int CompareTerminals(MandatoryRouteTerminal left, MandatoryRouteTerminal right)
        {
            var value = left.TerminalOrder.CompareTo(right.TerminalOrder);
            return value != 0 ? value : left.TerminalId.CompareTo(right.TerminalId);
        }
        private static int CompareCandidates(MandatoryConnectorCandidateEdge left, MandatoryConnectorCandidateEdge right)
        {
            var value = left.Cost.TotalCost.CompareTo(right.Cost.TotalCost);
            if (value != 0) return value;
            value = left.FromTerminalOrder.CompareTo(right.FromTerminalOrder);
            if (value != 0) return value;
            value = left.ToTerminalOrder.CompareTo(right.ToTerminalOrder);
            if (value != 0) return value;
            value = left.FromTerminalId.CompareTo(right.FromTerminalId);
            if (value != 0) return value;
            value = left.ToTerminalId.CompareTo(right.ToTerminalId);
            return value != 0 ? value : left.EdgeId.CompareTo(right.EdgeId);
        }
        private static int Find(int[] parent, int value) { while (parent[value] != value) { parent[value] = parent[parent[value]]; value = parent[value]; } return value; }
        private static bool IsWorldSector(SectorCoord value) => value.X >= 0 && value.X < WorldGenConstants.SectorColumns && value.Y >= 0 && value.Y < WorldGenConstants.SectorRows;
        private static int SectorIndex(SectorCoord value) => IsWorldSector(value) ? value.Y * WorldGenConstants.SectorColumns + value.X : -1;
        private static MandatoryConnectorTreeBuildError Error(MandatoryConnectorTreeBuildErrorCode code, string firstId, string secondId, int sectorIndex, string message) => new MandatoryConnectorTreeBuildError(code, firstId, secondId, sectorIndex, message);
        private static MandatoryConnectorTreeBuildResult Invalid(IEnumerable<MandatoryConnectorTreeBuildError> errors) => new MandatoryConnectorTreeBuildResult(MandatoryConnectorTreeBuildStatus.InvalidInput, null, null, SortAndDedupe(errors));
    }
}
