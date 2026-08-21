using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;

namespace StarNight.Map.Tests.WorldGeneration.Data
{
    public sealed class StaticDataAtomicPublisherTests
    {
        private StaticDataRegistry registryA;
        private StaticDataRegistry registryB;
        private ContentVersionHash hashA;
        private ContentVersionHash hashB;
        private StaticDataRegistryStore store;
        private StaticDataAtomicPublisher publisher;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            registryA = UninitializedRegistry();
            registryB = UninitializedRegistry();
            hashA = Hash(0x11);
            hashB = Hash(0x22);
        }

        [SetUp]
        public void SetUp()
        {
            store = new StaticDataRegistryStore();
            publisher = new StaticDataAtomicPublisher(store);
        }

        [Test]
        public void EmptyStore_HasNoCurrentSnapshot()
        {
            Assert.That(store.Current, Is.Null);
        }

        [Test]
        public void FirstSuccess_PublishesVersionOne()
        {
            var report = publisher.Publish(Request(registryA, hashA));

            Assert.That(report.Published, Is.True);
            Assert.That(report.PreviousVersion, Is.Zero);
            Assert.That(report.CurrentVersion, Is.EqualTo(1));
            Assert.That(store.Current.Version, Is.EqualTo(1));
        }

        [Test]
        public void RepeatedSuccess_IncrementsExactlyOncePerSubmission()
        {
            publisher.Publish(Request(registryA, hashA));
            publisher.Publish(Request(registryB, hashB));
            var report = publisher.Publish(Request(registryA, hashA));

            Assert.That(report.PreviousVersion, Is.EqualTo(2));
            Assert.That(report.CurrentVersion, Is.EqualTo(3));
            Assert.That(store.Current.Version, Is.EqualTo(3));
        }

        [Test]
        public void PublishedSnapshot_PreservesExactRegistryReference()
        {
            publisher.Publish(Request(registryA, hashA));

            Assert.That(store.Current.Registry, Is.SameAs(registryA));
        }

        [Test]
        public void PublishedSnapshot_PreservesExactHashReference()
        {
            publisher.Publish(Request(registryA, hashA));

            Assert.That(store.Current.ContentHash, Is.SameAs(hashA));
        }

        [Test]
        public void EverySuccess_ReplacesOneWholeSnapshotReference()
        {
            publisher.Publish(Request(registryA, hashA));
            var first = store.Current;
            publisher.Publish(Request(registryB, hashB));

            Assert.That(store.Current, Is.Not.SameAs(first));
            Assert.That(store.Current.Registry, Is.SameAs(registryB));
            Assert.That(store.Current.ContentHash, Is.SameAs(hashB));
        }

        [Test]
        public void NullRegistry_BlocksPublish()
        {
            var report = publisher.Publish(Request(null, hashA));

            AssertBlocked(report, "MISSING_REGISTRY");
        }

        [Test]
        public void NullHash_BlocksPublish()
        {
            var report = publisher.Publish(Request(registryA, null));

            AssertBlocked(report, "MISSING_CONTENT_HASH");
        }

        [Test]
        public void OneError_BlocksPublishAndIsPreserved()
        {
            var issue = Issue(CsvImportIssue.ErrorSeverity, "ONE");
            var report = publisher.Publish(Request(registryA, hashA, issue));

            Assert.That(report.Published, Is.False);
            Assert.That(report.Issues.Single(), Is.SameAs(issue));
        }

        [Test]
        public void MultipleErrors_AreAllPreservedWithoutShortCircuit()
        {
            var first = Issue(CsvImportIssue.ErrorSeverity, "A");
            var second = Issue(CsvImportIssue.ErrorSeverity, "B");
            var report = publisher.Publish(Request(registryA, hashA, second, first));

            Assert.That(report.ErrorCount, Is.EqualTo(2));
            Assert.That(report.Issues, Does.Contain(first));
            Assert.That(report.Issues, Does.Contain(second));
        }

        [Test]
        public void InvalidSeverity_BecomesBlockingPublisherIssue()
        {
            var report = publisher.Publish(Request(
                registryA,
                hashA,
                Issue("error", "BAD")));

            AssertBlocked(report, "INVALID_ISSUE_SEVERITY");
        }

        [Test]
        public void NullIssueEntry_BecomesBlockingPublisherIssue()
        {
            var report = publisher.Publish(Request(registryA, hashA, (CsvImportIssue)null));

            AssertBlocked(report, "INVALID_ISSUE");
        }

        [Test]
        public void EmptyIssueEntry_BecomesBlockingPublisherIssue()
        {
            var report = publisher.Publish(Request(
                registryA,
                hashA,
                new CsvImportIssue(string.Empty, CsvImportIssue.ErrorSeverity, string.Empty, string.Empty)));

            AssertBlocked(report, "INVALID_ISSUE");
        }

        [Test]
        public void MissingIssueSequence_BecomesBlockingPublisherIssue()
        {
            var report = publisher.Publish(new StaticDataPublishRequest(
                registryA,
                hashA,
                null));

            AssertBlocked(report, "MISSING_ISSUE_SEQUENCE");
        }

        [Test]
        public void WarningOnly_PublishesAndRemainsInReport()
        {
            var warning = Issue(CsvImportIssue.WarningSeverity, "WARN");
            var report = publisher.Publish(Request(registryA, hashA, warning));

            Assert.That(report.Published, Is.True);
            Assert.That(report.ErrorCount, Is.Zero);
            Assert.That(report.WarningCount, Is.EqualTo(1));
            Assert.That(report.Issues.Single(), Is.SameAs(warning));
        }

        [Test]
        public void MixedWarningAndError_BlocksPublish()
        {
            var report = publisher.Publish(Request(
                registryA,
                hashA,
                Issue(CsvImportIssue.WarningSeverity, "WARN"),
                Issue(CsvImportIssue.ErrorSeverity, "ERROR")));

            Assert.That(report.Published, Is.False);
            Assert.That(report.ErrorCount, Is.EqualTo(1));
            Assert.That(report.WarningCount, Is.EqualTo(1));
            Assert.That(store.Current, Is.Null);
        }

        [Test]
        public void FailedFirstAttempt_LeavesStoreNullAndVersionZero()
        {
            var report = publisher.Publish(Request(null, hashA));

            Assert.That(store.Current, Is.Null);
            Assert.That(report.PreviousVersion, Is.Zero);
            Assert.That(report.CurrentVersion, Is.Zero);
            Assert.That(report.PreviousContentHash, Is.Null);
            Assert.That(report.CurrentContentHash, Is.Null);
        }

        [Test]
        public void FailedLaterAttempt_PreservesExactLastGoodReferenceAndVersion()
        {
            publisher.Publish(Request(registryA, hashA));
            var previous = store.Current;
            var report = publisher.Publish(Request(registryB, hashB,
                Issue(CsvImportIssue.ErrorSeverity, "BLOCK")));

            Assert.That(store.Current, Is.SameAs(previous));
            Assert.That(report.PreviousVersion, Is.EqualTo(previous.Version));
            Assert.That(report.CurrentVersion, Is.EqualTo(previous.Version));
            Assert.That(report.PreviousContentHash, Is.SameAs(hashA));
            Assert.That(report.CandidateContentHash, Is.SameAs(hashB));
            Assert.That(report.CurrentContentHash, Is.SameAs(hashA));
        }

        [Test]
        public void SameContentHash_StillPublishesNewVersion()
        {
            publisher.Publish(Request(registryA, hashA));
            var report = publisher.Publish(Request(registryB, hashA));

            Assert.That(report.Published, Is.True);
            Assert.That(report.CurrentVersion, Is.EqualTo(2));
            Assert.That(store.Current.Registry, Is.SameAs(registryB));
            Assert.That(store.Current.ContentHash, Is.SameAs(hashA));
        }

        [Test]
        public void CancellationMarker_BlocksAndPreservesLastGood()
        {
            publisher.Publish(Request(registryA, hashA));
            var previous = store.Current;
            var request = new StaticDataPublishRequest(
                registryB,
                hashB,
                Array.Empty<CsvImportIssue>(),
                "cancelled",
                true);
            var report = publisher.Publish(request);

            AssertBlocked(report, "CANCELLED");
            Assert.That(store.Current, Is.SameAs(previous));
        }

        [Test]
        public void DuplicateIssues_AreNotDeduplicated()
        {
            var duplicate = Issue(CsvImportIssue.ErrorSeverity, "DUP");
            var report = publisher.Publish(Request(registryA, hashA, duplicate, duplicate));

            Assert.That(report.Issues.Count, Is.EqualTo(2));
            Assert.That(report.Issues.All(item => ReferenceEquals(item, duplicate)), Is.True);
        }

        [Test]
        public void CallerIssueOrder_DoesNotAffectReportOrderOrJson()
        {
            var first = Issue(CsvImportIssue.WarningSeverity, "A", stage: "A");
            var second = Issue(CsvImportIssue.WarningSeverity, "B", stage: "B");
            var left = new StaticDataAtomicPublisher(new StaticDataRegistryStore()).Publish(
                Request(registryA, hashA, second, first));
            var right = new StaticDataAtomicPublisher(new StaticDataRegistryStore()).Publish(
                Request(registryA, hashA, first, second));

            Assert.That(left.Issues[0], Is.SameAs(first));
            Assert.That(right.Issues[0], Is.SameAs(first));
            Assert.That(CsvImportReportJson.Serialize(left), Is.EqualTo(CsvImportReportJson.Serialize(right)));
        }

        [TestCase("severity")]
        [TestCase("stage")]
        [TestCase("source_file")]
        [TestCase("record")]
        [TestCase("field")]
        [TestCase("target_file")]
        [TestCase("target_column")]
        [TestCase("target_value")]
        [TestCase("code")]
        [TestCase("message")]
        [TestCase("line")]
        [TestCase("column")]
        [TestCase("offset")]
        public void Issues_SortDeterministicallyByContract(string dimension)
        {
            var pair = OrderedPair(dimension);
            var report = publisher.Publish(Request(registryA, hashA, pair[1], pair[0]));

            Assert.That(report.Issues[0], Is.SameAs(pair[0]), dimension);
            Assert.That(report.Issues[1], Is.SameAs(pair[1]), dimension);
        }

        [Test]
        public void SuccessReport_HasExactCountsHashesAndVersions()
        {
            var report = publisher.Publish(Request(
                registryA,
                hashA,
                Issue(CsvImportIssue.WarningSeverity, "WARN")));

            Assert.That(report.SchemaVersion, Is.EqualTo(1));
            Assert.That(report.Published, Is.True);
            Assert.That(report.PreviousVersion, Is.Zero);
            Assert.That(report.CurrentVersion, Is.EqualTo(1));
            Assert.That(report.PreviousContentHash, Is.Null);
            Assert.That(report.CandidateContentHash, Is.SameAs(hashA));
            Assert.That(report.CurrentContentHash, Is.SameAs(hashA));
            Assert.That(report.ErrorCount, Is.Zero);
            Assert.That(report.WarningCount, Is.EqualTo(1));
        }

        [Test]
        public void Json_UsesExactTopLevelPropertyOrderAndCompactForm()
        {
            var hash = Hash(0x00);
            var report = publisher.Publish(new StaticDataPublishRequest(
                registryA,
                hash,
                Array.Empty<CsvImportIssue>(),
                "ATTEMPT"));
            var expected =
                "{\"schema_version\":1,\"attempt_id\":\"ATTEMPT\",\"published\":true," +
                "\"previous_version\":0,\"current_version\":1," +
                "\"previous_content_hash\":null,\"candidate_content_hash\":\"" + hash.Hex +
                "\",\"current_content_hash\":\"" + hash.Hex +
                "\",\"error_count\":0,\"warning_count\":0,\"issues\":[]}\n";

            Assert.That(CsvImportReportJson.Serialize(report), Is.EqualTo(expected));
        }

        [Test]
        public void Json_EscapesStringsAccordingToJsonRules()
        {
            var warning = new CsvImportIssue(
                "STAGE",
                CsvImportIssue.WarningSeverity,
                "ESCAPE",
                "quote\" slash\\ line\n tab\t control\u0001");
            var json = CsvImportReportJson.Serialize(
                publisher.Publish(Request(registryA, hashA, warning)));

            Assert.That(json, Does.Contain("quote\\\" slash\\\\ line\\n tab\\t control\\u0001"));
            Assert.That(json.Substring(0, json.Length - 1), Does.Not.Contain("\n"));
        }

        [Test]
        public void Json_EmitsEveryNullableFieldExplicitly()
        {
            var warning = Issue(CsvImportIssue.WarningSeverity, "NULLS");
            var json = CsvImportReportJson.Serialize(
                publisher.Publish(Request(registryA, hashA, warning)));

            Assert.That(json, Does.Contain("\"previous_content_hash\":null"));
            Assert.That(json, Does.Contain("\"source_file\":null"));
            Assert.That(json, Does.Contain("\"record_number\":null"));
            Assert.That(json, Does.Contain("\"source_field\":null"));
            Assert.That(json, Does.Contain("\"line\":null"));
            Assert.That(json, Does.Contain("\"column\":null"));
            Assert.That(json, Does.Contain("\"offset\":null"));
            Assert.That(json, Does.Contain("\"target_file\":null"));
            Assert.That(json, Does.Contain("\"target_column\":null"));
            Assert.That(json, Does.Contain("\"target_value\":null"));
        }

        [Test]
        public void Json_HasExactlyOneFinalLfAndNoCr()
        {
            var json = CsvImportReportJson.Serialize(
                publisher.Publish(Request(registryA, hashA)));

            Assert.That(json.EndsWith("\n", StringComparison.Ordinal), Is.True);
            Assert.That(json.EndsWith("\n\n", StringComparison.Ordinal), Is.False);
            Assert.That(json.Count(character => character == '\n'), Is.EqualTo(1));
            Assert.That(json, Does.Not.Contain("\r"));
        }

        [Test]
        public void JsonUtf8_IsBomFreeStrictAndMatchesString()
        {
            var report = publisher.Publish(new StaticDataPublishRequest(
                registryA,
                hashA,
                new[] { Issue(CsvImportIssue.WarningSeverity, "UTF8", message: "달빛") },
                "시도"));
            var text = CsvImportReportJson.Serialize(report);
            var bytes = CsvImportReportJson.SerializeUtf8(report);

            Assert.That(bytes.Take(3), Is.Not.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }));
            Assert.That(new UTF8Encoding(false, true).GetString(bytes), Is.EqualTo(text));
        }

        [Test]
        public void Json_ContainsNoEnvironmentOrRuntimeLeakage()
        {
            var json = CsvImportReportJson.Serialize(
                publisher.Publish(Request(registryA, hashA)));

            Assert.That(json, Does.Not.Contain("C:\\"));
            Assert.That(json, Does.Not.Contain("/Users/"));
            Assert.That(json, Does.Not.Contain("stack"));
            Assert.That(json, Does.Not.Contain("timestamp"));
            Assert.That(json, Does.Not.Contain("machine"));
            Assert.That(json, Does.Not.Contain("unity_instance"));
        }

        [Test]
        public void IssueJson_UsesExactFieldOrder()
        {
            var issue = new CsvImportIssue(
                "S", CsvImportIssue.WarningSeverity, "C", "M",
                "source.csv", 2, "field", 3, 4, 5,
                "target.csv", "target_id", "VALUE");
            var json = CsvImportReportJson.Serialize(
                publisher.Publish(Request(registryA, hashA, issue)));
            var names = new[]
            {
                "\"stage\"", "\"severity\"", "\"code\"", "\"message\"",
                "\"source_file\"", "\"record_number\"", "\"source_field\"",
                "\"line\"", "\"column\"", "\"offset\"", "\"target_file\"",
                "\"target_column\"", "\"target_value\""
            };
            var previousIndex = -1;
            foreach (var name in names)
            {
                var index = json.IndexOf(name, previousIndex + 1, StringComparison.Ordinal);
                Assert.That(index, Is.GreaterThan(previousIndex), name);
                previousIndex = index;
            }
        }

        [Test]
        public void Json_NumbersAndBooleansIgnoreCurrentCulture()
        {
            var original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
                var json = CsvImportReportJson.Serialize(
                    publisher.Publish(Request(registryA, hashA)));

                Assert.That(json, Does.Contain("\"published\":true"));
                Assert.That(json, Does.Contain("\"previous_version\":0"));
                Assert.That(json, Does.Contain("\"current_version\":1"));
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Test]
        public void ReportFilename_IsExactContract()
        {
            Assert.That(CsvImportReport.FileName, Is.EqualTo("CsvImportReport.json"));
        }

        [Test]
        public void Request_CopiesAndProtectsIssueSequence()
        {
            var source = new List<CsvImportIssue> { Issue(CsvImportIssue.WarningSeverity, "A") };
            var request = new StaticDataPublishRequest(registryA, hashA, source);
            source.Add(Issue(CsvImportIssue.WarningSeverity, "B"));

            Assert.That(request.Issues.Count, Is.EqualTo(1));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<CsvImportIssue>)request.Issues).Add(Issue(CsvImportIssue.WarningSeverity, "C")));
        }

        [Test]
        public void Report_IssueViewIsReadOnly()
        {
            var report = publisher.Publish(Request(
                registryA,
                hashA,
                Issue(CsvImportIssue.WarningSeverity, "WARN")));

            Assert.Throws<NotSupportedException>(() =>
                ((IList<CsvImportIssue>)report.Issues).Clear());
        }

        [Test]
        public void Snapshot_HasNoPublicMutationSurface()
        {
            var properties = typeof(PublishedStaticDataSnapshot).GetProperties(
                BindingFlags.Instance | BindingFlags.Public);

            Assert.That(properties.Select(item => item.Name),
                Is.EquivalentTo(new[] { "Registry", "ContentHash", "Version" }));
            Assert.That(properties.All(item => item.SetMethod == null), Is.True);
            Assert.That(typeof(PublishedStaticDataSnapshot).GetConstructors(), Is.Empty);
        }

        [Test]
        public void Store_HasNoPublicSetterClearResetOrSingleton()
        {
            var type = typeof(StaticDataRegistryStore);
            var currentProperty = type.GetProperty("Current");
            var forbidden = type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(item => item.Name.IndexOf("Clear", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               item.Name.IndexOf("Reset", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               item.Name.IndexOf("Instance", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();

            Assert.That(currentProperty, Is.Not.Null);
            Assert.That(currentProperty.SetMethod, Is.Null);
            Assert.That(forbidden, Is.Empty);
        }

        [Test]
        public void ConcurrentSuccessfulPublishes_IncrementWithoutLostVersions()
        {
            Parallel.For(0, 64, index =>
            {
                var report = publisher.Publish(index % 2 == 0
                    ? Request(registryA, hashA)
                    : Request(registryB, hashB));
                Assert.That(report.Published, Is.True);
            });

            Assert.That(store.Current.Version, Is.EqualTo(64));
        }

        [Test]
        public void ConcurrentReaders_NeverObserveTornRegistryHashPairs()
        {
            publisher.Publish(Request(registryA, hashA));
            var tornReads = 0;
            var writer = Task.Run(() =>
            {
                for (var index = 0; index < 500; index++)
                {
                    publisher.Publish(index % 2 == 0
                        ? Request(registryB, hashB)
                        : Request(registryA, hashA));
                }
            });
            var reader = Task.Run(() =>
            {
                for (var index = 0; index < 100000; index++)
                {
                    var snapshot = store.Current;
                    var validA = ReferenceEquals(snapshot.Registry, registryA) &&
                                 ReferenceEquals(snapshot.ContentHash, hashA);
                    var validB = ReferenceEquals(snapshot.Registry, registryB) &&
                                 ReferenceEquals(snapshot.ContentHash, hashB);
                    if (!validA && !validB)
                    {
                        Interlocked.Increment(ref tornReads);
                    }
                }
            });

            Task.WaitAll(writer, reader);
            Assert.That(tornReads, Is.Zero);
        }

        [Test]
        public void NullRequest_ReturnsFailureReportWithoutThrowing()
        {
            CsvImportReport report = null;

            Assert.DoesNotThrow(() => report = publisher.Publish(null));
            AssertBlocked(report, "MISSING_REQUEST");
        }

        [Test]
        public void InvalidUtf16AttemptId_BlocksWithoutLeakingInvalidText()
        {
            var report = publisher.Publish(new StaticDataPublishRequest(
                registryA,
                hashA,
                Array.Empty<CsvImportIssue>(),
                "\uD800"));

            AssertBlocked(report, "INVALID_ATTEMPT_ID");
            Assert.That(report.AttemptId, Is.Null);
            Assert.DoesNotThrow(() => CsvImportReportJson.SerializeUtf8(report));
        }

        [Test]
        public void InvalidUtf16Issue_BecomesSafeBlockingIssue()
        {
            var report = publisher.Publish(Request(
                registryA,
                hashA,
                Issue(CsvImportIssue.WarningSeverity, "BAD_UTF16", message: "\uD800")));

            AssertBlocked(report, "INVALID_ISSUE");
            Assert.DoesNotThrow(() => CsvImportReportJson.SerializeUtf8(report));
        }

        [Test]
        public void Publisher_NullStoreIsProgrammerMisuseAndThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new StaticDataAtomicPublisher(null));
        }

        private StaticDataPublishRequest Request(
            StaticDataRegistry registry,
            ContentVersionHash hash,
            params CsvImportIssue[] issues)
        {
            return new StaticDataPublishRequest(
                registry,
                hash,
                issues ?? Array.Empty<CsvImportIssue>(),
                "ATTEMPT");
        }

        private static CsvImportIssue Issue(
            string severity,
            string code,
            string stage = "BUILD",
            string message = "message")
        {
            return new CsvImportIssue(stage, severity, code, message);
        }

        private static CsvImportIssue[] OrderedPair(string dimension)
        {
            var earlySeverity = CsvImportIssue.ErrorSeverity;
            var lateSeverity = dimension == "severity"
                ? CsvImportIssue.WarningSeverity
                : CsvImportIssue.ErrorSeverity;
            var earlyStage = dimension == "stage" ? "A" : "S";
            var lateStage = dimension == "stage" ? "B" : "S";
            var earlySource = dimension == "source_file" ? "a.csv" : "source.csv";
            var lateSource = dimension == "source_file" ? "b.csv" : "source.csv";
            var earlyRecord = dimension == "record" ? 1 : 7;
            var lateRecord = dimension == "record" ? 2 : 7;
            var earlyField = dimension == "field" ? "A" : "field";
            var lateField = dimension == "field" ? "B" : "field";
            var earlyTargetFile = dimension == "target_file" ? "a.csv" : "target.csv";
            var lateTargetFile = dimension == "target_file" ? "b.csv" : "target.csv";
            var earlyTargetColumn = dimension == "target_column" ? "A" : "target_id";
            var lateTargetColumn = dimension == "target_column" ? "B" : "target_id";
            var earlyTargetValue = dimension == "target_value" ? "A" : "value";
            var lateTargetValue = dimension == "target_value" ? "B" : "value";
            var earlyCode = dimension == "code" ? "A" : "CODE";
            var lateCode = dimension == "code" ? "B" : "CODE";
            var earlyMessage = dimension == "message" ? "A" : "message";
            var lateMessage = dimension == "message" ? "B" : "message";
            var earlyLine = dimension == "line" ? 1 : 8;
            var lateLine = dimension == "line" ? 2 : 8;
            var earlyColumn = dimension == "column" ? 1 : 9;
            var lateColumn = dimension == "column" ? 2 : 9;
            var earlyOffset = dimension == "offset" ? 1 : 10;
            var lateOffset = dimension == "offset" ? 2 : 10;
            return new[]
            {
                new CsvImportIssue(
                    earlyStage, earlySeverity, earlyCode, earlyMessage,
                    earlySource, earlyRecord, earlyField, earlyLine, earlyColumn, earlyOffset,
                    earlyTargetFile, earlyTargetColumn, earlyTargetValue),
                new CsvImportIssue(
                    lateStage, lateSeverity, lateCode, lateMessage,
                    lateSource, lateRecord, lateField, lateLine, lateColumn, lateOffset,
                    lateTargetFile, lateTargetColumn, lateTargetValue)
            };
        }

        private static void AssertBlocked(CsvImportReport report, string code)
        {
            Assert.That(report, Is.Not.Null);
            Assert.That(report.Published, Is.False);
            Assert.That(report.ErrorCount, Is.GreaterThan(0));
            Assert.That(report.Issues.Any(item => item.Code == code), Is.True,
                string.Join("\n", report.Issues.Select(item => item.Code + ": " + item.Message)));
        }

        private static ContentVersionHash Hash(byte value)
        {
            return (ContentVersionHash)Activator.CreateInstance(
                typeof(ContentVersionHash),
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new object[] { Enumerable.Repeat(value, ContentVersionHash.DigestLength).ToArray() },
                CultureInfo.InvariantCulture);
        }

        private static StaticDataRegistry UninitializedRegistry()
        {
            return (StaticDataRegistry)FormatterServices.GetUninitializedObject(
                typeof(StaticDataRegistry));
        }
    }
}
