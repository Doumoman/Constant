using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class WorldGenerationAttemptRecord
    {
        public WorldGenerationAttemptRecord(
            string passId,
            int passOrder,
            int attemptOrdinal,
            string retryScopeId,
            ulong worldSeed,
            DateTimeOffset startedUtc,
            long durationMilliseconds,
            bool succeeded,
            string failureCode,
            string failureMessage,
            string returnedRetryScopeId)
        {
            WorldGenerationExecutionRecordValidation.RequireNonEmpty(passId, nameof(passId));
            if (passOrder < 0) throw new ArgumentOutOfRangeException(nameof(passOrder));
            if (attemptOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(attemptOrdinal));
            if (retryScopeId == null) throw new ArgumentNullException(nameof(retryScopeId));
            WorldGenerationExecutionRecordValidation.RequireUtc(startedUtc, nameof(startedUtc));
            WorldGenerationExecutionRecordValidation.RequireDuration(durationMilliseconds, nameof(durationMilliseconds));
            if (failureCode == null) throw new ArgumentNullException(nameof(failureCode));
            if (failureMessage == null) throw new ArgumentNullException(nameof(failureMessage));
            if (returnedRetryScopeId == null) throw new ArgumentNullException(nameof(returnedRetryScopeId));

            if (succeeded)
            {
                if (failureCode.Length != 0 || failureMessage.Length != 0 || returnedRetryScopeId.Length != 0)
                    throw new ArgumentException("A successful attempt cannot contain failure fields.");
            }
            else if (failureCode.Length == 0)
            {
                throw new ArgumentException("A failed attempt must contain a failure code.", nameof(failureCode));
            }

            PassId = passId;
            PassOrder = passOrder;
            AttemptOrdinal = attemptOrdinal;
            RetryScopeId = retryScopeId;
            WorldSeed = worldSeed;
            StartedUtc = startedUtc;
            DurationMilliseconds = durationMilliseconds;
            Succeeded = succeeded;
            FailureCode = failureCode;
            FailureMessage = failureMessage;
            ReturnedRetryScopeId = returnedRetryScopeId;
        }

        public string PassId { get; }
        public int PassOrder { get; }
        public int AttemptOrdinal { get; }
        public string RetryScopeId { get; }
        public ulong WorldSeed { get; }
        public DateTimeOffset StartedUtc { get; }
        public long DurationMilliseconds { get; }
        public bool Succeeded { get; }
        public string FailureCode { get; }
        public string FailureMessage { get; }
        public string ReturnedRetryScopeId { get; }
    }

    internal static class WorldGenerationExecutionRecordValidation
    {
        public static void RequireNonEmpty(string value, string parameterName)
        {
            if (value == null) throw new ArgumentNullException(parameterName);
            if (value.Length == 0) throw new ArgumentException("Value must be non-empty.", parameterName);
        }

        public static void RequireUtc(DateTimeOffset value, string parameterName)
        {
            if (value.Offset != TimeSpan.Zero)
                throw new ArgumentException("Timestamp must use the UTC offset.", parameterName);
        }

        public static void RequireDuration(long durationMilliseconds, string parameterName)
        {
            if (durationMilliseconds < 0) throw new ArgumentOutOfRangeException(parameterName);
        }

        public static long ToDurationMilliseconds(TimeSpan elapsed)
        {
            if (elapsed < TimeSpan.Zero)
                throw new InvalidOperationException("The injected clock returned a negative elapsed time.");
            return elapsed.Ticks / TimeSpan.TicksPerMillisecond;
        }
    }
}
