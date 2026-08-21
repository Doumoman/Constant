using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class WorldGenerationPassExecutionRecord
    {
        public WorldGenerationPassExecutionRecord(
            string passId,
            string className,
            int passOrder,
            string failurePolicyToken,
            ulong worldSeed,
            DateTimeOffset startedUtc,
            long durationMilliseconds,
            IEnumerable<WorldGenerationAttemptRecord> attempts,
            int attemptCount,
            int retryCount,
            bool succeeded,
            bool terminal,
            string failureCode,
            string failureMessage,
            string finalRetryScopeId)
        {
            WorldGenerationExecutionRecordValidation.RequireNonEmpty(passId, nameof(passId));
            WorldGenerationExecutionRecordValidation.RequireNonEmpty(className, nameof(className));
            if (passOrder < 0) throw new ArgumentOutOfRangeException(nameof(passOrder));
            WorldGenerationExecutionRecordValidation.RequireNonEmpty(failurePolicyToken, nameof(failurePolicyToken));
            WorldGenerationFailurePolicyToken.Parse(failurePolicyToken);
            WorldGenerationExecutionRecordValidation.RequireUtc(startedUtc, nameof(startedUtc));
            WorldGenerationExecutionRecordValidation.RequireDuration(durationMilliseconds, nameof(durationMilliseconds));
            if (attempts == null) throw new ArgumentNullException(nameof(attempts));
            if (failureCode == null) throw new ArgumentNullException(nameof(failureCode));
            if (failureMessage == null) throw new ArgumentNullException(nameof(failureMessage));
            if (finalRetryScopeId == null) throw new ArgumentNullException(nameof(finalRetryScopeId));

            var copy = new List<WorldGenerationAttemptRecord>(attempts);
            if (copy.Count == 0) throw new ArgumentException("A started pass must contain an attempt.", nameof(attempts));
            if (attemptCount != copy.Count) throw new ArgumentException("Attempt count does not match the attempt collection.", nameof(attemptCount));
            if (retryCount != attemptCount - 1) throw new ArgumentException("Retry count must equal attempt count minus one.", nameof(retryCount));

            for (var index = 0; index < copy.Count; index++)
            {
                var attempt = copy[index];
                if (attempt == null) throw new ArgumentException("Attempt collection cannot contain null.", nameof(attempts));
                if (!string.Equals(attempt.PassId, passId, StringComparison.Ordinal) ||
                    attempt.PassOrder != passOrder ||
                    attempt.WorldSeed != worldSeed ||
                    attempt.AttemptOrdinal != index)
                    throw new ArgumentException("Attempt identity does not match its pass record.", nameof(attempts));
                if (attempt.StartedUtc < startedUtc)
                    throw new ArgumentException("An attempt cannot start before its pass.", nameof(attempts));
            }

            if (succeeded)
            {
                if (!copy[copy.Count - 1].Succeeded)
                    throw new ArgumentException("A successful pass must end with a successful attempt.", nameof(attempts));
                if (terminal || failureCode.Length != 0 || failureMessage.Length != 0 || finalRetryScopeId.Length != 0)
                    throw new ArgumentException("A successful pass cannot contain terminal failure fields.");
            }
            else
            {
                if (copy[copy.Count - 1].Succeeded)
                    throw new ArgumentException("A failed pass cannot end with a successful attempt.", nameof(attempts));
                if (failureCode.Length == 0)
                    throw new ArgumentException("A failed pass must contain a failure code.", nameof(failureCode));
            }

            PassId = passId;
            ClassName = className;
            PassOrder = passOrder;
            FailurePolicyToken = failurePolicyToken;
            WorldSeed = worldSeed;
            StartedUtc = startedUtc;
            DurationMilliseconds = durationMilliseconds;
            Attempts = new ReadOnlyCollection<WorldGenerationAttemptRecord>(copy);
            AttemptCount = attemptCount;
            RetryCount = retryCount;
            Succeeded = succeeded;
            Terminal = terminal;
            FailureCode = failureCode;
            FailureMessage = failureMessage;
            FinalRetryScopeId = finalRetryScopeId;
        }

        public string PassId { get; }
        public string ClassName { get; }
        public int PassOrder { get; }
        public string FailurePolicyToken { get; }
        public ulong WorldSeed { get; }
        public DateTimeOffset StartedUtc { get; }
        public long DurationMilliseconds { get; }
        public IReadOnlyList<WorldGenerationAttemptRecord> Attempts { get; }
        public int AttemptCount { get; }
        public int RetryCount { get; }
        public bool Succeeded { get; }
        public bool Terminal { get; }
        public string FailureCode { get; }
        public string FailureMessage { get; }
        public string FinalRetryScopeId { get; }
    }
}
