using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.MapAuthoring.Microchunks
{
    public sealed class MicrochunkCsvExportFilePlan
    {
        private readonly byte[] afterBytes;
        private readonly IReadOnlyList<string> headers;
        private readonly IReadOnlyList<string> finalRowOrder;

        public string FileName { get; }
        public string RelativeDirectory { get; }
        public IReadOnlyList<string> Headers => headers;
        public int RemovedRowCount { get; }
        public int InsertedRowCount { get; }
        public IReadOnlyList<string> FinalRowOrder => finalRowOrder;
        public string BeforeSha256 { get; }
        public string AfterSha256 { get; }
        public bool HasChanges => !string.Equals(BeforeSha256, AfterSha256, StringComparison.Ordinal);
        public byte[] AfterBytes => (byte[])afterBytes.Clone();

        internal MicrochunkCsvExportFilePlan(
            string fileName,
            string relativeDirectory,
            IEnumerable<string> headers,
            int removedRowCount,
            int insertedRowCount,
            IEnumerable<string> finalRowOrder,
            string beforeSha256,
            string afterSha256,
            byte[] afterBytes)
        {
            FileName = Require(fileName, nameof(fileName));
            RelativeDirectory = Require(relativeDirectory, nameof(relativeDirectory));
            if (headers == null) throw new ArgumentNullException(nameof(headers));
            if (removedRowCount < 0) throw new ArgumentOutOfRangeException(nameof(removedRowCount));
            if (insertedRowCount < 0) throw new ArgumentOutOfRangeException(nameof(insertedRowCount));
            if (finalRowOrder == null) throw new ArgumentNullException(nameof(finalRowOrder));
            BeforeSha256 = Require(beforeSha256, nameof(beforeSha256));
            AfterSha256 = Require(afterSha256, nameof(afterSha256));
            this.afterBytes = afterBytes == null
                ? throw new ArgumentNullException(nameof(afterBytes))
                : (byte[])afterBytes.Clone();
            this.headers = new ReadOnlyCollection<string>(headers.ToList());
            this.finalRowOrder = new ReadOnlyCollection<string>(finalRowOrder.ToList());
            RemovedRowCount = removedRowCount;
            InsertedRowCount = insertedRowCount;
        }

        private static string Require(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A non-blank value is required.", parameterName);
            }

            return value;
        }
    }

    public sealed class MicrochunkCsvExportPlan
    {
        private readonly IReadOnlyList<MicrochunkCsvExportFilePlan> files;
        private readonly IReadOnlyList<MicrochunkCsvExportIssue> issues;

        public MicrochunkCsvExportRequest Request { get; }
        public IReadOnlyList<MicrochunkCsvExportFilePlan> Files => files;
        public IReadOnlyList<MicrochunkCsvExportIssue> Issues => issues;
        public MicrochunkCsvImportValidationFeedback ValidationFeedback { get; }
        public bool HasValidationFeedback => ValidationFeedback != null;
        public bool Success => files.Count == 6 && issues.All(value => !value.IsError);
        public int ChangedFileCount => files.Count(value => value.HasChanges);
        public int TotalRemovedRows => files.Sum(value => value.RemovedRowCount);
        public int TotalInsertedRows => files.Sum(value => value.InsertedRowCount);

        internal MicrochunkCsvExportPlan(
            MicrochunkCsvExportRequest request,
            IEnumerable<MicrochunkCsvExportFilePlan> files,
            IEnumerable<MicrochunkCsvExportIssue> issues,
            MicrochunkCsvImportValidationFeedback validationFeedback)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            if (files == null) throw new ArgumentNullException(nameof(files));
            if (issues == null) throw new ArgumentNullException(nameof(issues));

            this.files = new ReadOnlyCollection<MicrochunkCsvExportFilePlan>(files
                .OrderBy(value => value.FileName, StringComparer.Ordinal)
                .ToList());
            var orderedIssues = issues.ToList();
            orderedIssues.Sort();
            this.issues = new ReadOnlyCollection<MicrochunkCsvExportIssue>(orderedIssues);
            ValidationFeedback = validationFeedback;
        }

        public MicrochunkCsvExportFilePlan GetFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name is required.", nameof(fileName));
            }

            var file = files.FirstOrDefault(value => string.Equals(
                value.FileName,
                fileName,
                StringComparison.Ordinal));
            if (file == null) throw new KeyNotFoundException("Export file plan was not found: " + fileName);
            return file;
        }
    }
}
