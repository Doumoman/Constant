using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalAttachmentEnumerationResult
    {
        private readonly IReadOnlyList<OptionalAttachmentCandidate> candidates;

        public OptionalAttachmentEnumerationResult(
            IEnumerable<OptionalAttachmentCandidate> candidates,
            OptionalAttachmentEnumerationDiagnostics diagnostics,
            IEnumerable<int> mandatoryRouteSectorIndices,
            int mandatoryRouteGraphNodeCount,
            int mandatoryRouteGraphDirectedEdgeCount,
            int mandatoryRouteCellCount)
        {
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            if (mandatoryRouteSectorIndices == null) throw new ArgumentNullException(nameof(mandatoryRouteSectorIndices));
            if (mandatoryRouteGraphNodeCount != OptionalRegionSnapshot.RequiredMandatoryNodeCount)
                throw new ArgumentException("Mandatory graph node count must preserve the MAP05 identity.", nameof(mandatoryRouteGraphNodeCount));
            if (mandatoryRouteGraphDirectedEdgeCount != OptionalRegionSnapshot.RequiredMandatoryDirectedEdgeCount)
                throw new ArgumentException("Mandatory directed edge count must preserve the MAP05 identity.", nameof(mandatoryRouteGraphDirectedEdgeCount));
            if (mandatoryRouteCellCount != OptionalRegionSnapshot.RequiredMandatoryRouteCellCount)
                throw new ArgumentException("Mandatory route cell count must preserve the MAP05 identity.", nameof(mandatoryRouteCellCount));

            var mandatory = new HashSet<int>();
            foreach (var sectorIndex in mandatoryRouteSectorIndices)
            {
                if (sectorIndex < 0 || sectorIndex >= StarNight.Map.WorldGeneration.Domain.WorldGenConstants.SectorCount)
                    throw new ArgumentOutOfRangeException(nameof(mandatoryRouteSectorIndices));
                if (!mandatory.Add(sectorIndex))
                    throw new ArgumentException("Mandatory route sector indices must be unique.", nameof(mandatoryRouteSectorIndices));
            }
            if (mandatory.Count != mandatoryRouteCellCount)
                throw new ArgumentException("Mandatory route sector cardinality must match the graph.", nameof(mandatoryRouteSectorIndices));

            var values = new List<OptionalAttachmentCandidate>(candidates);
            if (values.Exists(value => value == null))
                throw new ArgumentException("Candidates cannot contain null.", nameof(candidates));
            values.Sort((left, right) => left.AttachmentOrder.CompareTo(right.AttachmentOrder));
            if (values.Count != diagnostics.AcceptedCount)
                throw new ArgumentException("Candidate count must match diagnostics.", nameof(candidates));

            var ids = new HashSet<OptionalAttachmentCandidateId>();
            var entries = new HashSet<int>();
            for (var index = 0; index < values.Count; index++)
            {
                var candidate = values[index];
                if (candidate.AttachmentOrder != index ||
                    !candidate.CandidateId.TryGetOrdinal(out var ordinal) || ordinal != index)
                    throw new ArgumentException("Candidate IDs and orders must be contiguous from zero.", nameof(candidates));
                if (!ids.Add(candidate.CandidateId))
                    throw new ArgumentException("Candidate IDs must be unique.", nameof(candidates));
                if (!entries.Add(candidate.EntrySectorIndex))
                    throw new ArgumentException("Candidate entry sectors must be unique.", nameof(candidates));
                if (mandatory.Contains(candidate.EntrySectorIndex))
                    throw new ArgumentException("Candidate entries cannot overlap mandatory route cells.", nameof(candidates));
            }

            this.candidates = new ReadOnlyCollection<OptionalAttachmentCandidate>(values);
            MandatoryRouteGraphNodeCount = mandatoryRouteGraphNodeCount;
            MandatoryRouteGraphDirectedEdgeCount = mandatoryRouteGraphDirectedEdgeCount;
            MandatoryRouteCellCount = mandatoryRouteCellCount;
            CanonicalDigest = ComputeDigest(values, diagnostics);
        }

        public IReadOnlyList<OptionalAttachmentCandidate> Candidates => candidates;
        public OptionalAttachmentEnumerationDiagnostics Diagnostics { get; }
        public int MandatoryRouteGraphNodeCount { get; }
        public int MandatoryRouteGraphDirectedEdgeCount { get; }
        public int MandatoryRouteCellCount { get; }
        public string CanonicalDigest { get; }

        private static string ComputeDigest(
            IEnumerable<OptionalAttachmentCandidate> values,
            OptionalAttachmentEnumerationDiagnostics diagnostics)
        {
            var text = new StringBuilder();
            foreach (var candidate in values)
            {
                text.Append(candidate.CandidateId.Value).Append('|')
                    .Append(candidate.AttachmentOrder.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(candidate.MandatoryRouteSectorIndex.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(candidate.MandatoryRouteNodeId.Value).Append('|')
                    .Append(candidate.EntrySectorIndex.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(candidate.DirectionDx.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(candidate.DirectionDy.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(candidate.InitialDepth.Value.ToString(CultureInfo.InvariantCulture)).Append('\n');
            }

            text.Append("D|")
                .Append(diagnostics.RawNeighborProbes.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(diagnostics.OutOfBoundsRejected.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(diagnostics.MandatoryRejected.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(diagnostics.TerminalRejected.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(diagnostics.SiteReservationRejected.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(diagnostics.BiomeReservedRejected.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(diagnostics.DuplicateEntryRejected.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(diagnostics.AcceptedCount.ToString(CultureInfo.InvariantCulture)).Append('\n');
            foreach (var code in diagnostics.RejectionCodes)
            {
                text.Append(code).Append('\n');
            }

            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()));
                var result = new StringBuilder(hash.Length * 2);
                foreach (var value in hash) result.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }
    }
}
