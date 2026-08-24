using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.MapAuthoring.Microchunks
{
    public sealed class MicrochunkCsvExportResult
    {
        private readonly IReadOnlyList<MicrochunkCsvExportIssue> issues;

        public MicrochunkCsvExportPlan Plan { get; }
        public bool Applied { get; }
        public int WrittenFileCount { get; }
        public IReadOnlyList<MicrochunkCsvExportIssue> Issues => issues;
        public bool Success => Applied && Plan.Success && issues.All(value => !value.IsError);

        internal MicrochunkCsvExportResult(
            MicrochunkCsvExportPlan plan,
            bool applied,
            int writtenFileCount,
            IEnumerable<MicrochunkCsvExportIssue> issues)
        {
            Plan = plan ?? throw new ArgumentNullException(nameof(plan));
            if (writtenFileCount < 0) throw new ArgumentOutOfRangeException(nameof(writtenFileCount));
            if (issues == null) throw new ArgumentNullException(nameof(issues));
            Applied = applied;
            WrittenFileCount = writtenFileCount;
            var ordered = issues.ToList();
            ordered.Sort();
            this.issues = new ReadOnlyCollection<MicrochunkCsvExportIssue>(ordered);
        }
    }
}
