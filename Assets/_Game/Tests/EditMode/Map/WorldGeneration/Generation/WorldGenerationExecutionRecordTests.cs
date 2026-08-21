using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Generation
{
    public sealed class WorldGenerationExecutionRecordTests
    {
        private const string ProfileId = "GEN_RECORDED";
        private const string WorldId = "WORLD_RECORDED";
        private static readonly DateTimeOffset StartUtc = new DateTimeOffset(2026, 8, 12, 1, 2, 3, TimeSpan.Zero);
        private static readonly string[] Empty = Array.Empty<string>();

        [Test]
        public void SystemClock_IsStableSingleton()
        {
            Assert.That(SystemWorldGenerationClock.Instance, Is.SameAs(SystemWorldGenerationClock.Instance));
        }

        [Test]
        public void SystemClock_ReturnsUtcAndNonNegativeMonotonicElapsedTime()
        {
            var clock = SystemWorldGenerationClock.Instance;
            var start = clock.GetTimestamp();
            var utc = clock.GetUtcNow();
            var end = clock.GetTimestamp();
            Assert.That(utc.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(clock.GetElapsedTime(start, end), Is.GreaterThanOrEqualTo(TimeSpan.Zero));
        }

        [Test]
        public void AttemptRecord_ExposesExactFields()
        {
            var record = Attempt(
                ordinal: 2,
                retryScopeId: " 6,6 ",
                durationMilliseconds: 19,
                succeeded: false,
                failureCode: "NO_ROUTE",
                failureMessage: "no route",
                returnedRetryScopeId: " 7,7 ");
            Assert.That(record.PassId, Is.EqualTo("P"));
            Assert.That(record.PassOrder, Is.EqualTo(10));
            Assert.That(record.AttemptOrdinal, Is.EqualTo(2));
            Assert.That(record.RetryScopeId, Is.EqualTo(" 6,6 "));
            Assert.That(record.WorldSeed, Is.EqualTo(99UL));
            Assert.That(record.StartedUtc, Is.EqualTo(StartUtc));
            Assert.That(record.DurationMilliseconds, Is.EqualTo(19));
            Assert.That(record.Succeeded, Is.False);
            Assert.That(record.FailureCode, Is.EqualTo("NO_ROUTE"));
            Assert.That(record.FailureMessage, Is.EqualTo("no route"));
            Assert.That(record.ReturnedRetryScopeId, Is.EqualTo(" 7,7 "));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
        public void AttemptRecord_RejectsInvalidConstructorState(int mutation)
        {
            Assert.Catch<ArgumentException>(() => new WorldGenerationAttemptRecord(
                mutation == 0 ? null : mutation == 1 ? "" : "P",
                mutation == 2 ? -1 : 10,
                mutation == 3 ? -1 : 0,
                mutation == 4 ? null : "",
                99,
                mutation == 5 ? StartUtc.ToOffset(TimeSpan.FromHours(1)) : StartUtc,
                mutation == 6 ? -1 : 0,
                mutation >= 7 && mutation <= 9,
                mutation == 7 ? "BAD" : mutation == 10 ? null : mutation == 11 ? "" : "FAIL",
                mutation == 8 ? "BAD" : mutation == 10 ? null : "message",
                mutation == 9 ? "BAD" : mutation == 11 ? null : ""));
        }

        [TestCase(0L)]
        [TestCase(1L)]
        [TestCase(2L)]
        [TestCase(9L)]
        [TestCase(10L)]
        [TestCase(99L)]
        [TestCase(100L)]
        [TestCase(999L)]
        [TestCase(1000L)]
        [TestCase(long.MaxValue)]
        public void AttemptRecord_PreservesEveryNonNegativeDuration(long durationMilliseconds)
        {
            Assert.That(Attempt(durationMilliseconds: durationMilliseconds).DurationMilliseconds,
                Is.EqualTo(durationMilliseconds));
        }

        [Test]
        public void PassRecord_ExposesExactAggregateAndSnapshotsAttempts()
        {
            var source = new List<WorldGenerationAttemptRecord>
            {
                Attempt(succeeded: false, failureCode: "NO", failureMessage: "again"),
                Attempt(ordinal: 1)
            };
            var record = PassRecord(source, succeeded: true, attemptCount: 2, retryCount: 1);
            source.Clear();
            Assert.That(record.PassId, Is.EqualTo("P"));
            Assert.That(record.ClassName, Is.EqualTo("FakePass"));
            Assert.That(record.PassOrder, Is.EqualTo(10));
            Assert.That(record.FailurePolicyToken, Is.EqualTo("RETRY_PASS"));
            Assert.That(record.WorldSeed, Is.EqualTo(99UL));
            Assert.That(record.StartedUtc, Is.EqualTo(StartUtc));
            Assert.That(record.DurationMilliseconds, Is.EqualTo(5));
            Assert.That(record.Attempts.Count, Is.EqualTo(2));
            Assert.That(record.AttemptCount, Is.EqualTo(2));
            Assert.That(record.RetryCount, Is.EqualTo(1));
            Assert.That(record.Succeeded, Is.True);
            Assert.That(record.Terminal, Is.False);
            Assert.That(record.FailureCode, Is.Empty);
            Assert.That(record.FailureMessage, Is.Empty);
            Assert.That(record.FinalRetryScopeId, Is.Empty);
        }

        [Test]
        public void PassRecord_AttemptViewIsReadOnly()
        {
            var attempts = (IList<WorldGenerationAttemptRecord>)PassRecord(new[] { Attempt() }).Attempts;
            Assert.Throws<NotSupportedException>(() => attempts.Add(Attempt(ordinal: 1)));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
        public void PassRecord_RejectsInvalidConstructorState(int mutation)
        {
            var attempts = new List<WorldGenerationAttemptRecord> { Attempt() };
            if (mutation == 7) attempts[0] = null;
            if (mutation == 8) attempts[0] = Attempt(ordinal: 1);
            if (mutation == 9) attempts[0] = Attempt(passId: "OTHER");
            if (mutation == 10) attempts[0] = Attempt(startedUtc: StartUtc.AddSeconds(-1));

            Assert.Catch<ArgumentException>(() => new WorldGenerationPassExecutionRecord(
                mutation == 0 ? "" : "P",
                mutation == 1 ? "" : "FakePass",
                mutation == 2 ? -1 : 10,
                mutation == 3 ? "retry_pass" : "RETRY_PASS",
                99,
                mutation == 4 ? StartUtc.ToOffset(TimeSpan.FromHours(1)) : StartUtc,
                mutation == 5 ? -1 : 5,
                mutation == 6 ? null : attempts,
                mutation == 11 ? 2 : 1,
                0,
                mutation == 11,
                false,
                mutation == 11 ? "" : "FAIL",
                mutation == 11 ? "" : "message",
                ""));
        }

        [Test]
        public void ExecutionRecord_ExposesExactAggregateAndSnapshotsPasses()
        {
            var source = new List<WorldGenerationPassExecutionRecord> { PassRecord(new[] { Attempt() }) };
            var record = ExecutionRecord(source);
            source.Clear();
            Assert.That(record.GenerationProfileId, Is.EqualTo(ProfileId));
            Assert.That(record.WorldProfileId, Is.EqualTo(WorldId));
            Assert.That(record.WorldSeed, Is.EqualTo(99UL));
            Assert.That(record.InclusivePassId, Is.Empty);
            Assert.That(record.StartedUtc, Is.EqualTo(StartUtc));
            Assert.That(record.DurationMilliseconds, Is.EqualTo(9));
            Assert.That(record.PassCount, Is.EqualTo(1));
            Assert.That(record.AttemptCount, Is.EqualTo(1));
            Assert.That(record.RetryCountTotal, Is.Zero);
            Assert.That(record.Succeeded, Is.True);
            Assert.That(record.LastCompletedPassId, Is.EqualTo("P"));
            Assert.That(record.FailurePassId, Is.Empty);
            Assert.That(record.FailureCode, Is.Empty);
            Assert.That(record.FailureMessage, Is.Empty);
        }

        [Test]
        public void ExecutionRecord_PassViewIsReadOnly()
        {
            var passes = (IList<WorldGenerationPassExecutionRecord>)ExecutionRecord(
                new[] { PassRecord(new[] { Attempt() }) }).Passes;
            Assert.Throws<NotSupportedException>(() => passes.Clear());
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
        public void ExecutionRecord_RejectsInvalidConstructorState(int mutation)
        {
            var pass = PassRecord(new[] { Attempt() });
            var passes = mutation == 7
                ? new WorldGenerationPassExecutionRecord[] { null }
                : new[] { pass };
            Assert.Catch<ArgumentException>(() => new WorldGenerationExecutionRecord(
                mutation == 0 ? null : ProfileId,
                mutation == 1 ? null : WorldId,
                99,
                mutation == 2 ? null : "",
                mutation == 3 ? StartUtc.ToOffset(TimeSpan.FromHours(1)) : StartUtc,
                mutation == 4 ? -1 : 9,
                mutation == 5 ? null : passes,
                mutation == 6 ? 2 : 1,
                mutation == 8 ? 2 : 1,
                mutation == 9 ? 1 : 0,
                true,
                mutation == 10 ? "OTHER" : "P",
                mutation == 11 ? "P" : "",
                mutation == 11 ? "FAIL" : "",
                mutation == 11 ? "message" : ""));
        }

        [Test]
        public void Root_RecordedSuccessCapturesExactClockOrderAndCounts()
        {
            var clock = new ManualClock(StartUtc, TimeSpan.FromTicks(12345));
            var fixture = CreateFixture(new[] { Pass("P", 10, "FakePass", Empty, new[] { "A" }) });
            var implementation = Fake("P", "FakePass", _ => WorldGenerationPassResult.Success("A", "value"));
            var execution = Root(fixture, clock, implementation).ExecuteRecorded(ProfileId, 99);

            Assert.That(execution.Result.Succeeded, Is.True);
            Assert.That(execution.Result.Artifacts.Get<string>("A"), Is.EqualTo("value"));
            Assert.That(execution.ExecutionRecord.StartedUtc, Is.EqualTo(StartUtc));
            Assert.That(execution.ExecutionRecord.DurationMilliseconds, Is.EqualTo(6));
            Assert.That(execution.ExecutionRecord.PassCount, Is.EqualTo(1));
            Assert.That(execution.ExecutionRecord.AttemptCount, Is.EqualTo(1));
            Assert.That(execution.ExecutionRecord.RetryCountTotal, Is.Zero);
            Assert.That(execution.ExecutionRecord.Passes[0].StartedUtc, Is.EqualTo(StartUtc.AddSeconds(1)));
            Assert.That(execution.ExecutionRecord.Passes[0].DurationMilliseconds, Is.EqualTo(3));
            Assert.That(execution.ExecutionRecord.Passes[0].Attempts[0].StartedUtc, Is.EqualTo(StartUtc.AddSeconds(2)));
            Assert.That(execution.ExecutionRecord.Passes[0].Attempts[0].DurationMilliseconds, Is.EqualTo(1));
            Assert.That(clock.UtcCallCount, Is.EqualTo(3));
            Assert.That(clock.TimestampCallCount, Is.EqualTo(6));
            Assert.That(clock.ElapsedCallCount, Is.EqualTo(3));
        }

        [Test]
        public void Root_PlanFailureRecordsNoPassOrAttemptAndKnownWorldProfile()
        {
            var fixture = CreateFixture(new[] { Pass("P", 10, "FakePass", Empty, new[] { "A" }) }, profileActive: false);
            var execution = Root(fixture, new ManualClock(), Fake("P", "FakePass")).ExecuteRecorded(ProfileId, 7);
            Assert.That(execution.Result.Succeeded, Is.False);
            Assert.That(execution.ExecutionRecord.WorldProfileId, Is.EqualTo(WorldId));
            Assert.That(execution.ExecutionRecord.PassCount, Is.Zero);
            Assert.That(execution.ExecutionRecord.AttemptCount, Is.Zero);
            Assert.That(execution.ExecutionRecord.RetryCountTotal, Is.Zero);
            Assert.That(execution.ExecutionRecord.FailureCode, Is.EqualTo("INACTIVE_PROFILE"));
        }

        [Test]
        public void Root_MissingProfileRecordsEmptyWorldProfile()
        {
            var fixture = CreateFixture(new[] { Pass("P", 10, "FakePass", Empty, new[] { "A" }) });
            var execution = Root(fixture, new ManualClock(), Fake("P", "FakePass")).ExecuteRecorded("MISSING", 7);
            Assert.That(execution.ExecutionRecord.WorldProfileId, Is.Empty);
            Assert.That(execution.ExecutionRecord.FailureCode, Is.EqualTo("MISSING_PROFILE"));
        }

        [Test]
        public void Root_ExecuteThroughRecordedStopsAtExactInclusivePass()
        {
            var first = Pass("A", 10, "First", Empty, new[] { "ONE" });
            var second = Pass("B", 20, "Second", new[] { "ONE" }, new[] { "TWO" });
            var secondImplementation = Fake("B", "Second");
            var fixture = CreateFixture(new[] { first, second });
            var execution = Root(
                fixture,
                new ManualClock(),
                Fake("A", "First", _ => WorldGenerationPassResult.Success("ONE", new object())),
                secondImplementation).ExecuteThroughRecorded(ProfileId, 1, "A");
            Assert.That(execution.ExecutionRecord.InclusivePassId, Is.EqualTo("A"));
            Assert.That(execution.ExecutionRecord.Passes.Select(item => item.PassId), Is.EqualTo(new[] { "A" }));
            Assert.That(secondImplementation.InvocationCount, Is.Zero);
        }

        [Test]
        public void Root_RetrySuccessPreservesFailedAttemptAndAggregateSuccess()
        {
            var definition = Pass("P", 10, "FakePass", Empty, new[] { "A" }, "RETRY_PASS", 1);
            var implementation = Fake("P", "FakePass", context => context.AttemptOrdinal == 0
                ? WorldGenerationPassResult.Failure("ORIGINAL", "again", "ignored")
                : WorldGenerationPassResult.Success("A", new object()));
            var execution = ExecuteSingle(definition, implementation);
            var pass = execution.ExecutionRecord.Passes.Single();
            Assert.That(pass.Succeeded, Is.True);
            Assert.That(pass.AttemptCount, Is.EqualTo(2));
            Assert.That(pass.RetryCount, Is.EqualTo(1));
            Assert.That(pass.Attempts[0].FailureCode, Is.EqualTo("ORIGINAL"));
            Assert.That(pass.Attempts[0].ReturnedRetryScopeId, Is.EqualTo("ignored"));
            Assert.That(pass.Attempts[1].Succeeded, Is.True);
        }

        [Test]
        public void Root_RetryExhaustionPreservesOriginalAttemptFailureAndStableAggregateFailure()
        {
            var definition = Pass("P", 10, "FakePass", Empty, new[] { "A" }, "RETRY_PASS", 1);
            var execution = ExecuteSingle(definition, Fake("P", "FakePass", _ =>
                WorldGenerationPassResult.Failure("ORIGINAL", "again", "ignored")));
            var pass = execution.ExecutionRecord.Passes.Single();
            Assert.That(pass.Attempts.Select(item => item.FailureCode), Is.EqualTo(new[] { "ORIGINAL", "ORIGINAL" }));
            Assert.That(pass.FailureCode, Is.EqualTo("RETRY_EXHAUSTED"));
            Assert.That(pass.Terminal, Is.True);
            Assert.That(execution.ExecutionRecord.FailureCode, Is.EqualTo("RETRY_EXHAUSTED"));
        }

        [Test]
        public void Root_RetryScopeCarriesExactScopeIntoNextAttempt()
        {
            var definition = Pass("P", 10, "FakePass", Empty, new[] { "A" }, "RETRY_SCOPE", 1);
            var implementation = Fake("P", "FakePass", context => context.AttemptOrdinal == 0
                ? WorldGenerationPassResult.Failure("ORIGINAL", "again", " 6,6 ")
                : WorldGenerationPassResult.Success("A", new object()));
            var pass = ExecuteSingle(definition, implementation).ExecutionRecord.Passes.Single();
            Assert.That(pass.Attempts[0].RetryScopeId, Is.Empty);
            Assert.That(pass.Attempts[0].ReturnedRetryScopeId, Is.EqualTo(" 6,6 "));
            Assert.That(pass.Attempts[1].RetryScopeId, Is.EqualTo(" 6,6 "));
        }

        [Test]
        public void Root_MissingRetryScopeMapsStableTerminalCause()
        {
            var definition = Pass("P", 10, "FakePass", Empty, new[] { "A" }, "RETRY_SCOPE", 1);
            var execution = ExecuteSingle(definition, Fake("P", "FakePass", _ =>
                WorldGenerationPassResult.Failure("ORIGINAL", "again")));
            AssertFailure(execution, "MISSING_RETRY_SCOPE", "ORIGINAL");
        }

        [Test]
        public void Root_FailWorldMapsStableTerminalCause()
        {
            var definition = Pass("P", 10, "FakePass", Empty, new[] { "A" });
            var execution = ExecuteSingle(definition, Fake("P", "FakePass", _ =>
                WorldGenerationPassResult.Failure("ORIGINAL", "failed", "scope")));
            AssertFailure(execution, "PASS_FAILED", "ORIGINAL");
        }

        [Test]
        public void Root_ReportOnlyCompletionIsSuccessfulAndNonTerminal()
        {
            var definition = Pass("P", 10, "FakePass", Empty, new[] { "A" }, "REPORT_ONLY");
            var execution = ExecuteSingle(definition, Fake("P", "FakePass", _ =>
                WorldGenerationPassResult.Failure("ORIGINAL", "reported", "scope")));
            Assert.That(execution.Result.Succeeded, Is.True);
            Assert.That(execution.Result.Issues.Single().Terminal, Is.False);
            Assert.That(execution.ExecutionRecord.Succeeded, Is.True);
            Assert.That(execution.ExecutionRecord.FailureCode, Is.Empty);
            Assert.That(execution.ExecutionRecord.Passes.Single().Succeeded, Is.False);
            Assert.That(execution.ExecutionRecord.Passes.Single().Terminal, Is.False);
        }

        [Test]
        public void Root_NullPassResultMapsStableAttemptAndTerminalCause()
        {
            var definition = Pass("P", 10, "FakePass", Empty, new[] { "A" });
            AssertFailure(ExecuteSingle(definition, Fake("P", "FakePass", _ => null)),
                "NULL_PASS_RESULT", "NULL_PASS_RESULT");
        }

        [Test]
        public void Root_UnhandledPassExceptionMapsStableAttemptAndTerminalCause()
        {
            var definition = Pass("P", 10, "FakePass", Empty, new[] { "A" });
            AssertFailure(ExecuteSingle(definition, Fake("P", "FakePass", _ =>
                throw new InvalidOperationException("variable"))),
                "UNHANDLED_PASS_EXCEPTION", "UNHANDLED_PASS_EXCEPTION");
        }

        [TestCase("missing")]
        [TestCase("extra")]
        public void Root_OutputSetMismatchMapsStableAttemptAndTerminalCause(string mutation)
        {
            var definition = Pass("P", 10, "FakePass", Empty, new[] { "A" });
            var implementation = Fake("P", "FakePass", _ => mutation == "missing"
                ? WorldGenerationPassResult.Success(Array.Empty<KeyValuePair<string, object>>())
                : WorldGenerationPassResult.Success(new[]
                {
                    Pair("A", new object()),
                    Pair("B", new object())
                }));
            AssertFailure(ExecuteSingle(definition, implementation),
                "OUTPUT_SET_MISMATCH", "OUTPUT_SET_MISMATCH");
        }

        [Test]
        public void Root_MissingInputDoesNotCreateAnUninvokedPassRecord()
        {
            var first = Pass("A", 10, "First", Empty, new[] { "ONE" }, "REPORT_ONLY");
            var second = Pass("B", 20, "Second", new[] { "ONE" }, new[] { "TWO" });
            var secondImplementation = Fake("B", "Second");
            var fixture = CreateFixture(new[] { first, second });
            var execution = Root(
                fixture,
                new ManualClock(),
                Fake("A", "First", _ => WorldGenerationPassResult.Failure("NO", "reported")),
                secondImplementation).ExecuteRecorded(ProfileId, 1);
            Assert.That(execution.ExecutionRecord.Passes.Select(item => item.PassId), Is.EqualTo(new[] { "A" }));
            Assert.That(execution.ExecutionRecord.FailurePassId, Is.EqualTo("B"));
            Assert.That(execution.ExecutionRecord.FailureCode, Is.EqualTo("MISSING_INPUT_ARTIFACT"));
            Assert.That(secondImplementation.InvocationCount, Is.Zero);
        }

        [Test]
        public void Root_LegacyApiUsesRecordedCoreWithoutReexecution()
        {
            var fixture = CreateFixture(new[] { Pass("P", 10, "FakePass", Empty, new[] { "A" }) });
            var implementation = Fake("P", "FakePass", _ => WorldGenerationPassResult.Success("A", new object()));
            var result = Root(fixture, new ManualClock(), implementation).Execute(ProfileId, 1);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(implementation.InvocationCount, Is.EqualTo(1));
        }

        [Test]
        public void Root_ReusedInstanceReturnsIndependentRecordGraphs()
        {
            var fixture = CreateFixture(new[] { Pass("P", 10, "FakePass", Empty, new[] { "A" }) });
            var root = Root(
                fixture,
                new ManualClock(),
                Fake("P", "FakePass", _ => WorldGenerationPassResult.Success("A", new object())));
            var first = root.ExecuteRecorded(ProfileId, 1).ExecutionRecord;
            for (var iteration = 0; iteration < 100; iteration++)
            {
                var currentRoot = iteration % 2 == 0
                    ? root
                    : Root(
                        fixture,
                        new ManualClock(),
                        Fake("P", "FakePass", _ => WorldGenerationPassResult.Success("A", new object())));
                var current = currentRoot.ExecuteRecorded(ProfileId, 1).ExecutionRecord;
                Assert.That(current, Is.Not.SameAs(first));
                Assert.That(current.Passes, Is.Not.SameAs(first.Passes));
                Assert.That(current.Passes[0].Attempts, Is.Not.SameAs(first.Passes[0].Attempts));
                Assert.That(first.PassCount, Is.EqualTo(1));
                Assert.That(first.AttemptCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void Root_DifferentClockSchedulesDoNotAffectGenerationOutcome()
        {
            var fixture = CreateFixture(new[] { Pass("P", 10, "FakePass", Empty, new[] { "A" }) });
            var first = Root(
                fixture,
                new ManualClock(StartUtc, TimeSpan.FromTicks(10000)),
                Fake("P", "FakePass", _ => WorldGenerationPassResult.Success("A", "same")))
                .ExecuteRecorded(ProfileId, 0x1234);
            var second = Root(
                fixture,
                new ManualClock(StartUtc.AddYears(5), TimeSpan.FromTicks(987654)),
                Fake("P", "FakePass", _ => WorldGenerationPassResult.Success("A", "same")))
                .ExecuteRecorded(ProfileId, 0x1234);
            Assert.That(second.Result.Succeeded, Is.EqualTo(first.Result.Succeeded));
            Assert.That(second.Result.LastCompletedPassId, Is.EqualTo(first.Result.LastCompletedPassId));
            Assert.That(second.Result.Artifacts.Get<string>("A"), Is.EqualTo(first.Result.Artifacts.Get<string>("A")));
            Assert.That(second.Result.Issues.Select(item => item.Code), Is.EqualTo(first.Result.Issues.Select(item => item.Code)));
            Assert.That(second.ExecutionRecord.PassCount, Is.EqualTo(first.ExecutionRecord.PassCount));
            Assert.That(second.ExecutionRecord.AttemptCount, Is.EqualTo(first.ExecutionRecord.AttemptCount));
            Assert.That(second.ExecutionRecord.StartedUtc, Is.Not.EqualTo(first.ExecutionRecord.StartedUtc));
            Assert.That(second.ExecutionRecord.DurationMilliseconds, Is.Not.EqualTo(first.ExecutionRecord.DurationMilliseconds));
        }

        [Test]
        public void Root_DifferentClockSchedulesDoNotAffectGeneratedGridOrCsvBytes()
        {
            var definition = Pass("PASS_GRID", 0, "GridInitializationPass", Empty, new[] { "GRID" });
            var fixture = CreateFixture(new[] { definition });
            var first = Root(
                fixture,
                new ManualClock(StartUtc, TimeSpan.FromTicks(10000)),
                new GridInitializationPassAdapter()).ExecuteRecorded(ProfileId, 0x1234);
            var second = Root(
                fixture,
                new ManualClock(StartUtc.AddYears(5), TimeSpan.FromTicks(987654)),
                new GridInitializationPassAdapter()).ExecuteRecorded(ProfileId, 0x1234);
            var firstGrid = first.Result.Artifacts.Get<GridInitializationResult>("GRID");
            var secondGrid = second.Result.Artifacts.Get<GridInitializationResult>("GRID");
            CollectionAssert.AreEqual(
                GeneratedWorldDataCsvSerializer.Serialize(firstGrid.WorldData),
                GeneratedWorldDataCsvSerializer.Serialize(secondGrid.WorldData));
            Assert.That(secondGrid.Neighbors.Select(item => item.ValidNeighborCount),
                Is.EqualTo(firstGrid.Neighbors.Select(item => item.ValidNeighborCount)));
            Assert.That(second.ExecutionRecord.DurationMilliseconds,
                Is.Not.EqualTo(first.ExecutionRecord.DurationMilliseconds));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void Root_InjectedClockFailurePropagatesWithoutRetry(int failurePoint)
        {
            var fixture = CreateFixture(new[] { Pass("P", 10, "FakePass", Empty, new[] { "A" }, "RETRY_PASS", 3) });
            var implementation = Fake("P", "FakePass", _ => WorldGenerationPassResult.Success("A", new object()));
            var root = Root(fixture, new ThrowingClock(failurePoint), implementation);
            Assert.Throws<InvalidOperationException>(() => root.ExecuteRecorded(ProfileId, 1));
            Assert.That(implementation.InvocationCount, Is.LessThanOrEqualTo(1));
        }

        [Test]
        public void ExecutionResult_RejectsNullResult()
        {
            Assert.Throws<ArgumentNullException>(() => new WorldGenerationExecutionResult(null, null));
        }

        [Test]
        public void ExecutionResult_RejectsNullRecord()
        {
            var fixture = CreateFixture(new[] { Pass("P", 10, "FakePass", Empty, new[] { "A" }) });
            var result = Root(
                fixture,
                new ManualClock(),
                Fake("P", "FakePass", _ => WorldGenerationPassResult.Success("A", new object())))
                .Execute(ProfileId, 1);
            Assert.Throws<ArgumentNullException>(() => new WorldGenerationExecutionResult(result, null));
        }

        private static WorldGenerationAttemptRecord Attempt(
            string passId = "P",
            int ordinal = 0,
            string retryScopeId = "",
            DateTimeOffset? startedUtc = null,
            long durationMilliseconds = 1,
            bool succeeded = true,
            string failureCode = "",
            string failureMessage = "",
            string returnedRetryScopeId = "")
        {
            return new WorldGenerationAttemptRecord(
                passId,
                10,
                ordinal,
                retryScopeId,
                99,
                startedUtc ?? StartUtc,
                durationMilliseconds,
                succeeded,
                failureCode,
                failureMessage,
                returnedRetryScopeId);
        }

        private static WorldGenerationPassExecutionRecord PassRecord(
            IEnumerable<WorldGenerationAttemptRecord> attempts,
            bool succeeded = true,
            int attemptCount = 1,
            int retryCount = 0)
        {
            return new WorldGenerationPassExecutionRecord(
                "P",
                "FakePass",
                10,
                "RETRY_PASS",
                99,
                StartUtc,
                5,
                attempts,
                attemptCount,
                retryCount,
                succeeded,
                false,
                succeeded ? "" : "FAIL",
                succeeded ? "" : "message",
                "");
        }

        private static WorldGenerationExecutionRecord ExecutionRecord(
            IEnumerable<WorldGenerationPassExecutionRecord> passes)
        {
            return new WorldGenerationExecutionRecord(
                ProfileId,
                WorldId,
                99,
                "",
                StartUtc,
                9,
                passes,
                1,
                1,
                0,
                true,
                "P",
                "",
                "",
                "");
        }

        private static WorldGenerationExecutionResult ExecuteSingle(
            GenerationPassDefinition definition,
            ScriptedPass implementation)
        {
            var fixture = CreateFixture(new[] { definition });
            return Root(fixture, new ManualClock(), implementation).ExecuteRecorded(ProfileId, 1);
        }

        private static void AssertFailure(
            WorldGenerationExecutionResult execution,
            string aggregateCode,
            string attemptCode)
        {
            var pass = execution.ExecutionRecord.Passes.Single();
            Assert.That(execution.Result.Succeeded, Is.False);
            Assert.That(execution.ExecutionRecord.Succeeded, Is.False);
            Assert.That(execution.ExecutionRecord.FailureCode, Is.EqualTo(aggregateCode));
            Assert.That(pass.FailureCode, Is.EqualTo(aggregateCode));
            Assert.That(pass.Terminal, Is.True);
            Assert.That(pass.Attempts.Last().FailureCode, Is.EqualTo(attemptCode));
        }

        private static WorldGenerationRoot Root(
            Fixture fixture,
            IWorldGenerationClock clock,
            params IWorldGenerationPass[] passes)
        {
            return new WorldGenerationRoot(
                fixture.StaticData,
                new WorldGenerationPassRegistry(passes),
                clock);
        }

        private static Fixture CreateFixture(
            IEnumerable<GenerationPassDefinition> passDefinitions,
            bool profileActive = true)
        {
            var profile = Definition<GenerationProfileDefinition>(
                Pair("GenerationProfileId", (object)ProfileId),
                Pair("WorldProfileId", WorldId),
                Pair("Active", profileActive));
            var definitions = Construct<WorldRouteDefinitionSet>(
                new[] { Definition<WorldProfileDefinition>(Pair("WorldProfileId", (object)WorldId), Pair("Active", true)) },
                new[] { profile },
                passDefinitions.ToArray(),
                RequiredRngDefinitions(),
                Array.Empty<SectorRouteMaskDefinition>(),
                Array.Empty<SocketBandDefinition>(),
                Array.Empty<EdgeSignatureDefinition>(),
                Array.Empty<EdgeSignatureCompatibilityDefinition>(),
                Array.Empty<SectorRecipeDefinition>(),
                Array.Empty<SectorRecipeCellDefinition>(),
                Array.Empty<SectorRecipePathDefinition>(),
                Array.Empty<SectorExternalSocketDefinition>(),
                Array.Empty<SectorRecipePoolEntryDefinition>());
            var registry = (StaticDataRegistry)FormatterServices.GetUninitializedObject(typeof(StaticDataRegistry));
            SetAutoProperty(registry, "WorldRouteDefinitions", definitions);
            return new Fixture(registry);
        }

        private static GenerationPassDefinition Pass(
            string id,
            int order,
            string className,
            IReadOnlyList<string> inputs,
            IReadOnlyList<string> outputs,
            string policy = "FAIL_WORLD",
            int maxRetryCount = 0)
        {
            return Definition<GenerationPassDefinition>(
                Pair("GenerationProfileId", (object)ProfileId),
                Pair("PassOrder", order),
                Pair("PassId", id),
                Pair("ClassName", className),
                Pair("RngStreamId", ""),
                Pair("InputArtifacts", new ReadOnlyCollection<string>(inputs.ToList())),
                Pair("OutputArtifacts", new ReadOnlyCollection<string>(outputs.ToList())),
                Pair("FailurePolicy", policy),
                Pair("MaxRetryCount", maxRetryCount),
                Pair("Enabled", true),
                Pair("Notes", ""));
        }

        private static RngStreamDefinition[] RequiredRngDefinitions()
        {
            return new[]
            {
                Rng("RNG_WORLD_SITE", "A13C9E0B2F1044D1", "WORLD"),
                Rng("RNG_BIOME_PATCH", "B7A91D33E40C5F82", "PASS"),
                Rng("RNG_ROUTE", "C00FEE12AB341901", "PASS"),
                Rng("RNG_TYPE0", "D15EA5E007A4C883", "PASS"),
                Rng("RNG_SECTOR_RECIPE", "E9931A70C2D520F4", "SECTOR"),
                Rng("RNG_POPULATION", "F123456789ABCDEF", "SPAWN")
            };
        }

        private static RngStreamDefinition Rng(string id, string salt, string scope)
        {
            return Definition<RngStreamDefinition>(
                Pair("RngStreamId", (object)id),
                Pair("SaltHex", CreateHex(salt)),
                Pair("ResetScope", scope),
                Pair("DescriptionKo", "test"),
                Pair("Active", true));
        }

        private static CsvHexValue CreateHex(string value)
        {
            var bytes = Enumerable.Range(0, value.Length / 2)
                .Select(index => byte.Parse(value.Substring(index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture))
                .ToArray();
            var constructor = typeof(CsvHexValue).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(string), typeof(IEnumerable<byte>) },
                null);
            return (CsvHexValue)constructor.Invoke(new object[] { value, bytes });
        }

        private static T Definition<T>(params KeyValuePair<string, object>[] values)
        {
            var value = (T)FormatterServices.GetUninitializedObject(typeof(T));
            foreach (var pair in values) SetAutoProperty(value, pair.Key, pair.Value);
            return value;
        }

        private static T Construct<T>(params object[] arguments)
        {
            return (T)Activator.CreateInstance(
                typeof(T),
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                arguments,
                CultureInfo.InvariantCulture);
        }

        private static void SetAutoProperty(object target, string propertyName, object value)
        {
            var field = target.GetType().GetField(
                "<" + propertyName + ">k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, propertyName);
            field.SetValue(target, value);
        }

        private static ScriptedPass Fake(
            string passId,
            string className,
            Func<WorldGenerationPassContext, WorldGenerationPassResult> execute = null)
        {
            return new ScriptedPass(
                passId,
                className,
                execute ?? (_ => WorldGenerationPassResult.Success("A", new object())));
        }

        private static KeyValuePair<string, object> Pair(string key, object value)
        {
            return new KeyValuePair<string, object>(key, value);
        }

        private sealed class Fixture
        {
            public Fixture(StaticDataRegistry staticData)
            {
                StaticData = staticData;
            }

            public StaticDataRegistry StaticData { get; }
        }

        private sealed class ScriptedPass : IWorldGenerationPass
        {
            private readonly Func<WorldGenerationPassContext, WorldGenerationPassResult> execute;

            public ScriptedPass(
                string passId,
                string className,
                Func<WorldGenerationPassContext, WorldGenerationPassResult> execute)
            {
                PassId = passId;
                ClassName = className;
                this.execute = execute;
            }

            public string PassId { get; }
            public string ClassName { get; }
            public int InvocationCount { get; private set; }

            public WorldGenerationPassResult Execute(WorldGenerationPassContext context)
            {
                InvocationCount++;
                return execute(context);
            }
        }

        private sealed class ManualClock : IWorldGenerationClock
        {
            private readonly DateTimeOffset startUtc;
            private readonly TimeSpan elapsedPerTimestamp;

            public ManualClock()
                : this(StartUtc, TimeSpan.FromMilliseconds(1))
            {
            }

            public ManualClock(DateTimeOffset startUtc, TimeSpan elapsedPerTimestamp)
            {
                this.startUtc = startUtc;
                this.elapsedPerTimestamp = elapsedPerTimestamp;
            }

            public int UtcCallCount { get; private set; }
            public int TimestampCallCount { get; private set; }
            public int ElapsedCallCount { get; private set; }

            public DateTimeOffset GetUtcNow()
            {
                return startUtc.AddSeconds(UtcCallCount++);
            }

            public long GetTimestamp()
            {
                return TimestampCallCount++;
            }

            public TimeSpan GetElapsedTime(long startTimestamp, long endTimestamp)
            {
                ElapsedCallCount++;
                return TimeSpan.FromTicks((endTimestamp - startTimestamp) * elapsedPerTimestamp.Ticks);
            }
        }

        private sealed class ThrowingClock : IWorldGenerationClock
        {
            private readonly int failurePoint;

            public ThrowingClock(int failurePoint)
            {
                this.failurePoint = failurePoint;
            }

            public DateTimeOffset GetUtcNow()
            {
                if (failurePoint == 0) return StartUtc.ToOffset(TimeSpan.FromHours(1));
                return StartUtc;
            }

            public long GetTimestamp()
            {
                if (failurePoint == 1) throw new InvalidOperationException("clock timestamp failure");
                return 0;
            }

            public TimeSpan GetElapsedTime(long startTimestamp, long endTimestamp)
            {
                if (failurePoint == 2) return TimeSpan.FromTicks(-1);
                return TimeSpan.Zero;
            }
        }
    }
}
