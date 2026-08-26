using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceBoundaryCoveragePairReport
    {
        internal MoonpalaceBoundaryCoveragePairReport(
            MoonpalaceBoundaryCoverageRequirement requirement,
            int candidateCount,
            int microchunkCount,
            int tileRowCount,
            int socketRowCount,
            IDictionary<string, int> orientationCoverage,
            IDictionary<string, int> profileCoverage,
            IEnumerable<MoonpalaceBoundaryCoverageIssue> issues)
        {
            Requirement = requirement ?? throw new ArgumentNullException(nameof(requirement));
            CandidateCount = candidateCount;
            MicrochunkCount = microchunkCount;
            TileRowCount = tileRowCount;
            SocketRowCount = socketRowCount;
            OrientationCoverage = Snapshot(orientationCoverage);
            ProfileCoverage = Snapshot(profileCoverage);
            Issues = new ReadOnlyCollection<MoonpalaceBoundaryCoverageIssue>(
                (issues ?? throw new ArgumentNullException(nameof(issues))).OrderBy(value => value).ToArray());
            StableDigest = MoonpalaceBoundaryCoverageReport.ComputeDigest(new[]
            {
                Requirement.PairRuleId,
                CandidateCount.ToString(),
                MicrochunkCount.ToString(),
                TileRowCount.ToString(),
                SocketRowCount.ToString(),
                string.Join(";", OrientationCoverage.OrderBy(value => value.Key, StringComparer.Ordinal)
                    .Select(value => value.Key + "=" + value.Value)),
                string.Join(";", ProfileCoverage.OrderBy(value => value.Key, StringComparer.Ordinal)
                    .Select(value => value.Key + "=" + value.Value)),
                string.Join("\n", Issues.Select(value => value.StableKey)),
            });
        }

        public MoonpalaceBoundaryCoverageRequirement Requirement { get; }
        public string PairRuleId => Requirement.PairRuleId;
        public bool Accepted => Issues.Count == 0;
        public int CandidateCount { get; }
        public int MicrochunkCount { get; }
        public int TileRowCount { get; }
        public int SocketRowCount { get; }
        public IReadOnlyDictionary<string, int> OrientationCoverage { get; }
        public IReadOnlyDictionary<string, int> ProfileCoverage { get; }
        public IReadOnlyList<MoonpalaceBoundaryCoverageIssue> Issues { get; }
        public string StableDigest { get; }

        private static IReadOnlyDictionary<string, int> Snapshot(IDictionary<string, int> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return new ReadOnlyDictionary<string, int>(
                new Dictionary<string, int>(source, StringComparer.Ordinal));
        }
    }
}
