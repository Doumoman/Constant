using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceBoundaryCoverageReport
    {
        internal MoonpalaceBoundaryCoverageReport(
            IEnumerable<MoonpalaceBoundaryCoveragePairReport> pairReports,
            int candidateCountTotal,
            int microchunkCountTotal,
            int tileRowCountTotal,
            int socketRowCountTotal,
            IDictionary<string, int> orientationCoverage,
            IDictionary<string, int> profileCoverage,
            int generatedCsvCount,
            string authoringManifestSha256,
            IEnumerable<MoonpalaceBoundaryCoverageIssue> issues)
        {
            if (pairReports == null) throw new ArgumentNullException(nameof(pairReports));
            if (orientationCoverage == null) throw new ArgumentNullException(nameof(orientationCoverage));
            if (profileCoverage == null) throw new ArgumentNullException(nameof(profileCoverage));
            if (issues == null) throw new ArgumentNullException(nameof(issues));

            PairReports = new ReadOnlyCollection<MoonpalaceBoundaryCoveragePairReport>(
                pairReports.OrderBy(value => value.Requirement.PairOrder).ToArray());
            CandidateCountTotal = candidateCountTotal;
            MicrochunkCountTotal = microchunkCountTotal;
            TileRowCountTotal = tileRowCountTotal;
            SocketRowCountTotal = socketRowCountTotal;
            OrientationCoverage = new ReadOnlyDictionary<string, int>(
                new Dictionary<string, int>(orientationCoverage, StringComparer.Ordinal));
            ProfileCoverage = new ReadOnlyDictionary<string, int>(
                new Dictionary<string, int>(profileCoverage, StringComparer.Ordinal));
            GeneratedCsvCount = generatedCsvCount;
            AuthoringManifestSha256 = authoringManifestSha256 ?? string.Empty;
            Issues = new ReadOnlyCollection<MoonpalaceBoundaryCoverageIssue>(
                issues.OrderBy(value => value).ToArray());
            StableDigest = ComputeDigest(new[]
            {
                PairReportCount.ToString(),
                CandidateCountTotal.ToString(),
                MicrochunkCountTotal.ToString(),
                TileRowCountTotal.ToString(),
                SocketRowCountTotal.ToString(),
                GeneratedCsvCount.ToString(),
                AuthoringManifestSha256,
                string.Join(";", OrientationCoverage.OrderBy(value => value.Key, StringComparer.Ordinal)
                    .Select(value => value.Key + "=" + value.Value)),
                string.Join(";", ProfileCoverage.OrderBy(value => value.Key, StringComparer.Ordinal)
                    .Select(value => value.Key + "=" + value.Value)),
                string.Join(";", PairReports.Select(value => value.StableDigest)),
                string.Join("\n", Issues.Select(value => value.StableKey)),
            });
        }

        public bool Accepted => Issues.Count == 0 && PairReports.All(value => value.Accepted);
        public IReadOnlyList<MoonpalaceBoundaryCoveragePairReport> PairReports { get; }
        public int PairReportCount => PairReports.Count;
        public int CandidateCountTotal { get; }
        public int MicrochunkCountTotal { get; }
        public int TileRowCountTotal { get; }
        public int SocketRowCountTotal { get; }
        public IReadOnlyDictionary<string, int> OrientationCoverage { get; }
        public IReadOnlyDictionary<string, int> ProfileCoverage { get; }
        public int GeneratedCsvCount { get; }
        public string AuthoringManifestSha256 { get; }
        public IReadOnlyList<MoonpalaceBoundaryCoverageIssue> Issues { get; }
        public IReadOnlyList<MoonpalaceBoundaryCoverageIssue> IssueList => Issues;
        public string StableDigest { get; }

        public MoonpalaceBoundaryCoveragePairReport GetPairReport(string pairRuleId)
        {
            if (pairRuleId == null) throw new ArgumentNullException(nameof(pairRuleId));
            var report = PairReports.FirstOrDefault(value =>
                string.Equals(value.PairRuleId, pairRuleId, StringComparison.Ordinal));
            if (report == null) throw new KeyNotFoundException("Unknown pair report: " + pairRuleId);
            return report;
        }

        internal static string ComputeDigest(IEnumerable<string> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            var bytes = Encoding.UTF8.GetBytes(string.Join("\n", values));
            using (var sha256 = SHA256.Create())
            {
                return string.Concat(sha256.ComputeHash(bytes).Select(value => value.ToString("x2")));
            }
        }
    }
}
