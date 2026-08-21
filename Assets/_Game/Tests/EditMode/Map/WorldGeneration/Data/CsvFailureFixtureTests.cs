using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;

namespace StarNight.Map.Tests.WorldGeneration.Data
{
    [TestFixture]
    [Category("MAP01_16")]
    [Parallelizable(ParallelScope.None)]
    public sealed class CsvFailureFixtureTests
    {
        [Test]
        public void Factory_CopiesExactExpectedFiftyFilesOnly()
        {
            using (var fixture = CsvFailureFixtureFactory.Create())
            {
                var files = Directory.GetFiles(fixture.Root, "*", SearchOption.AllDirectories);
                Assert.That(files.Length, Is.EqualTo(50));
                Assert.That(files.Select(Path.GetFileName).OrderBy(item => item, StringComparer.Ordinal),
                    Is.EqualTo(CsvFailureFixtureFactory.ExpectedFileNames
                        .OrderBy(item => item, StringComparer.Ordinal)));
            }
        }

        [Test]
        public void Factory_ByteCopiesEveryAuthoringSource()
        {
            using (var fixture = CsvFailureFixtureFactory.Create())
            {
                var sourceByName = Directory.GetFiles(
                        fixture.SourceRoot, "*.csv", SearchOption.AllDirectories)
                    .ToDictionary(Path.GetFileName, StringComparer.Ordinal);
                foreach (var fileName in CsvFailureFixtureFactory.ExpectedFileNames)
                {
                    Assert.That(File.ReadAllBytes(fixture.ResolveFile(fileName)),
                        Is.EqualTo(File.ReadAllBytes(sourceByName[fileName])), fileName);
                }
            }
        }

        [Test]
        public void Factory_UsesUniqueOwnedTempRoots()
        {
            using (var left = CsvFailureFixtureFactory.Create())
            using (var right = CsvFailureFixtureFactory.Create())
            {
                Assert.That(left.Root, Is.Not.EqualTo(right.Root));
                Assert.That(left.Root, Does.StartWith(Path.GetTempPath()).IgnoreCase);
                Assert.That(right.Root, Does.StartWith(Path.GetTempPath()).IgnoreCase);
            }
        }

        [Test]
        public void Factory_RejectsTraversalAndUnknownFilenames()
        {
            using (var fixture = CsvFailureFixtureFactory.Create())
            {
                Assert.Throws<ArgumentException>(() => fixture.ResolveFile("../world_profiles.csv"));
                Assert.Throws<ArgumentException>(() => fixture.ResolveFile("unexpected.csv"));
                Assert.Throws<ArgumentException>(() => fixture.ResolveFile("Biome/biome_types.csv"));
            }
        }

        [Test]
        public void Factory_DisposeDeletesOnlyOwnedRoot()
        {
            var unrelatedRoot = Path.Combine(
                Path.GetTempPath(), "StarNightCsvFailureUnrelated_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(unrelatedRoot);
            var unrelatedFile = Path.Combine(unrelatedRoot, "keep.txt");
            File.WriteAllText(unrelatedFile, "keep");
            var fixture = CsvFailureFixtureFactory.Create();
            var ownedRoot = fixture.Root;
            fixture.Dispose();
            try
            {
                Assert.That(Directory.Exists(ownedRoot), Is.False);
                Assert.That(File.Exists(unrelatedFile), Is.True);
            }
            finally
            {
                Directory.Delete(unrelatedRoot, true);
            }
        }

        [TestCase(CsvFailureMutationKind.DuplicatePrimaryKey)]
        [TestCase(CsvFailureMutationKind.InvalidEnumToken)]
        [TestCase(CsvFailureMutationKind.InvalidInt)]
        [TestCase(CsvFailureMutationKind.InvalidFloat)]
        [TestCase(CsvFailureMutationKind.MissingSingleForeignKey)]
        [TestCase(CsvFailureMutationKind.MissingListForeignKey)]
        [TestCase(CsvFailureMutationKind.MissingUtf8Bom)]
        [TestCase(CsvFailureMutationKind.RowOrderReversed)]
        [TestCase(CsvFailureMutationKind.HeaderOrderChanged)]
        [TestCase(CsvFailureMutationKind.CompoundIndependentFailures)]
        public void MandatoryMutation_RecordsImmutableExactDescriptor(
            CsvFailureMutationKind kind)
        {
            using (var fixture = CsvFailureFixtureFactory.Create())
            {
                var descriptors = fixture.Apply(kind);
                Assert.That(descriptors.Count,
                    Is.EqualTo(kind == CsvFailureMutationKind.CompoundIndependentFailures ? 4 : 1));
                Assert.That(descriptors, Is.All.Matches<CsvFailureMutation>(descriptor =>
                    !string.IsNullOrEmpty(descriptor.MutationName) &&
                    CsvFailureFixtureFactory.ExpectedFileNames.Contains(descriptor.FileName) &&
                    !string.IsNullOrEmpty(descriptor.ColumnName) &&
                    descriptor.RecordNumber > 0 && descriptor.SourceLine > 0 &&
                    descriptor.BeforeSha256.Length == 64 && descriptor.AfterSha256.Length == 64 &&
                    !string.Equals(descriptor.BeforeSha256, descriptor.AfterSha256,
                        StringComparison.Ordinal)));
                Assert.Throws<NotSupportedException>(() =>
                    ((IList<CsvFailureMutation>)descriptors).Clear());
            }
        }

        [TestCase(CsvFailureMutationKind.DuplicatePrimaryKey)]
        [TestCase(CsvFailureMutationKind.InvalidEnumToken)]
        [TestCase(CsvFailureMutationKind.InvalidInt)]
        [TestCase(CsvFailureMutationKind.InvalidFloat)]
        [TestCase(CsvFailureMutationKind.MissingSingleForeignKey)]
        [TestCase(CsvFailureMutationKind.MissingListForeignKey)]
        [TestCase(CsvFailureMutationKind.MissingUtf8Bom)]
        [TestCase(CsvFailureMutationKind.RowOrderReversed)]
        [TestCase(CsvFailureMutationKind.HeaderOrderChanged)]
        [TestCase(CsvFailureMutationKind.CompoundIndependentFailures)]
        public void MandatoryMutation_IsDeterministicAcrossUniqueRoots(
            CsvFailureMutationKind kind)
        {
            using (var left = CsvFailureFixtureFactory.Create())
            using (var right = CsvFailureFixtureFactory.Create())
            {
                var leftProjection = Project(left.Apply(kind));
                var rightProjection = Project(right.Apply(kind));
                Assert.That(leftProjection, Is.EqualTo(rightProjection));
            }
        }

        [Test]
        public void MissingBom_RemovesOnlyExactLeadingThreeBytes()
        {
            using (var fixture = CsvFailureFixtureFactory.Create())
            {
                var descriptor = fixture.Apply(CsvFailureMutationKind.MissingUtf8Bom).Single();
                var sourcePath = Directory.GetFiles(
                        fixture.SourceRoot, descriptor.FileName, SearchOption.AllDirectories)
                    .Single();
                var source = File.ReadAllBytes(sourcePath);
                var mutated = File.ReadAllBytes(fixture.ResolveFile(descriptor.FileName));
                Assert.That(source.Take(3), Is.EqualTo(new byte[] { 0xef, 0xbb, 0xbf }));
                Assert.That(mutated, Is.EqualTo(source.Skip(3).ToArray()));
                var read = new Rfc4180CsvReader().Read(mutated, descriptor.FileName);
                Assert.That(read.Success, Is.True);
                Assert.That(read.HadUtf8Bom, Is.False);
            }
        }

        [TestCase(CsvFailureMutationKind.DuplicatePrimaryKey, "PRIMARY_KEYS", "DUPLICATE_PRIMARY_KEY")]
        [TestCase(CsvFailureMutationKind.InvalidEnumToken, "VALUE_PARSE", "InvalidEnum")]
        [TestCase(CsvFailureMutationKind.InvalidInt, "VALUE_PARSE", "InvalidInteger")]
        [TestCase(CsvFailureMutationKind.InvalidFloat, "VALUE_PARSE", "InvalidFloat")]
        [TestCase(CsvFailureMutationKind.MissingSingleForeignKey, "FOREIGN_KEYS", "MissingTargetRecord")]
        [TestCase(CsvFailureMutationKind.MissingListForeignKey, "FOREIGN_KEYS", "MissingTargetRecord")]
        [TestCase(CsvFailureMutationKind.MissingUtf8Bom, "READ", "MISSING_UTF8_BOM")]
        [TestCase(CsvFailureMutationKind.HeaderOrderChanged, "HEADER_FIELDS", "HeaderOrderMismatch")]
        public void FailureCase_ReturnsExactDiagnosticAndPreservesPreviousSnapshot(
            CsvFailureMutationKind kind,
            string expectedStage,
            string expectedCode)
        {
            var store = new StaticDataRegistryStore();
            using (var baselineFixture = CsvFailureFixtureFactory.Create())
            using (var failureFixture = CsvFailureFixtureFactory.Create())
            {
                var baseline = baselineFixture.Run(store, "baseline");
                Assert.That(baseline.Report.Published, Is.True, FormatIssues(baseline));
                var previous = store.Current;
                var descriptor = failureFixture.Apply(kind).Single();
                var failure = failureFixture.Run(store, "failure");
                Assert.That(failure.Report.Published, Is.False);
                Assert.That(store.Current, Is.SameAs(previous));
                Assert.That(store.Current.Registry, Is.SameAs(previous.Registry));
                Assert.That(store.Current.ContentHash, Is.SameAs(previous.ContentHash));
                Assert.That(store.Current.Version, Is.EqualTo(previous.Version));
                var diagnostic = failure.Issues.FirstOrDefault(issue =>
                    issue.Stage == expectedStage && issue.Code == expectedCode &&
                    issue.SourceFile == descriptor.FileName);
                Assert.That(diagnostic, Is.Not.Null, FormatIssues(failure));
                Assert.That(diagnostic.Line, Is.GreaterThanOrEqualTo(1));
                Assert.That(failure.Report.ErrorCount,
                    Is.EqualTo(failure.Issues.Count(issue => issue.Severity == "ERROR")));
            }
        }

        [Test]
        public void CompoundFailure_AccumulatesIndependentErrorsInDeterministicReportBytes()
        {
            byte[] firstBytes;
            byte[] secondBytes;
            using (var firstBaseline = CsvFailureFixtureFactory.Create())
            using (var firstFailure = CsvFailureFixtureFactory.Create())
            {
                var store = new StaticDataRegistryStore();
                Assert.That(firstBaseline.Run(store, "baseline").Report.Published, Is.True);
                var previous = store.Current;
                var descriptors = firstFailure.Apply(
                    CsvFailureMutationKind.CompoundIndependentFailures);
                var result = firstFailure.Run(store, "compound");
                Assert.That(result.Report.Published, Is.False);
                Assert.That(store.Current, Is.SameAs(previous));
                Assert.That(store.Current.Registry, Is.SameAs(previous.Registry));
                Assert.That(store.Current.ContentHash, Is.SameAs(previous.ContentHash));
                Assert.That(store.Current.Version, Is.EqualTo(previous.Version));
                Assert.That(descriptors.Select(item => item.FileName).Distinct().Count(), Is.EqualTo(4));
                Assert.That(result.Issues.Any(item => item.Code == "DUPLICATE_PRIMARY_KEY"),
                    Is.True, FormatIssues(result));
                Assert.That(result.Issues.Any(item => item.Code == "InvalidEnum"),
                    Is.True, FormatIssues(result));
                Assert.That(result.Issues.Any(item => item.Code == "InvalidInteger"),
                    Is.True, FormatIssues(result));
                Assert.That(result.Issues.Any(item => item.Code == "MissingTargetRecord"),
                    Is.True, FormatIssues(result));
                firstBytes = CsvImportReportJson.SerializeUtf8(result.Report);
            }

            using (var secondBaseline = CsvFailureFixtureFactory.Create())
            using (var secondFailure = CsvFailureFixtureFactory.Create())
            {
                var store = new StaticDataRegistryStore();
                Assert.That(secondBaseline.Run(store, "baseline").Report.Published, Is.True);
                secondFailure.Apply(CsvFailureMutationKind.CompoundIndependentFailures);
                var result = secondFailure.Run(store, "compound");
                secondBytes = CsvImportReportJson.SerializeUtf8(result.Report);
            }

            Assert.That(secondBytes, Is.EqualTo(firstBytes));
        }

        [Test]
        public void RowOrderReverse_ChangesRawHashButPublishesSameSemanticHash()
        {
            using (var baselineFixture = CsvFailureFixtureFactory.Create())
            using (var reversedFixture = CsvFailureFixtureFactory.Create())
            {
                var baseline = baselineFixture.Run(attemptId: "baseline");
                var descriptor = reversedFixture.Apply(CsvFailureMutationKind.RowOrderReversed).Single();
                var reversed = reversedFixture.Run(attemptId: "reversed");
                Assert.That(baseline.Report.Published, Is.True, FormatIssues(baseline));
                Assert.That(reversed.Report.Published, Is.True, FormatIssues(reversed));
                Assert.That(reversed.RawHashes[descriptor.FileName],
                    Is.Not.EqualTo(baseline.RawHashes[descriptor.FileName]));
                Assert.That(reversed.CandidateHash, Is.EqualTo(baseline.CandidateHash));
                Assert.That(reversed.Report.CurrentContentHash,
                    Is.EqualTo(baseline.Report.CurrentContentHash));
            }
        }

        [Test]
        public void FailureThenValid_RecoversWithoutSessionOrStoreLeakage()
        {
            var store = new StaticDataRegistryStore();
            using (var initialFixture = CsvFailureFixtureFactory.Create())
            using (var failureFixture = CsvFailureFixtureFactory.Create())
            using (var recoveryFixture = CsvFailureFixtureFactory.Create())
            {
                var initial = initialFixture.Run(store, "initial");
                Assert.That(initial.Report.Published, Is.True, FormatIssues(initial));
                failureFixture.Apply(CsvFailureMutationKind.InvalidEnumToken);
                var failure = failureFixture.Run(store, "failure");
                Assert.That(failure.Report.Published, Is.False);
                var recovery = recoveryFixture.Run(store, "recovery");
                Assert.That(recovery.Report.Published, Is.True, FormatIssues(recovery));
                Assert.That(recovery.Report.PreviousVersion, Is.EqualTo(1));
                Assert.That(recovery.Report.CurrentVersion, Is.EqualTo(2));
                Assert.That(recovery.Report.ErrorCount, Is.Zero);
                Assert.That(recovery.Report.Issues, Is.Empty);
                Assert.That(store.Current.Registry, Is.SameAs(recovery.CandidateRegistry));
                Assert.That(store.Current.ContentHash, Is.SameAs(recovery.CandidateHash));
            }
        }

        private static string[] Project(IEnumerable<CsvFailureMutation> descriptors)
        {
            return descriptors.Select(item => string.Join("\u001f", new[]
            {
                item.MutationName,
                item.FileName,
                item.ColumnName,
                item.RecordNumber.ToString(),
                item.SourceLine.ToString(),
                item.Before,
                item.After,
                item.BeforeSha256,
                item.AfterSha256
            })).ToArray();
        }

        private static string FormatIssues(CsvFixtureImportResult result)
        {
            return string.Join("\n", result.Issues.Select(issue =>
                issue.Stage + "/" + issue.Code + "/" + issue.SourceFile + "/" +
                issue.RecordNumber + ":" + issue.Message));
        }
    }
}
