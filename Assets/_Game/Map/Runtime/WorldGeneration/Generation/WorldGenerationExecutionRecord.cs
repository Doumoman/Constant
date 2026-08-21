using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class WorldGenerationExecutionRecord
    {
        public WorldGenerationExecutionRecord(
            string generationProfileId,
            string worldProfileId,
            ulong worldSeed,
            string inclusivePassId,
            DateTimeOffset startedUtc,
            long durationMilliseconds,
            IEnumerable<WorldGenerationPassExecutionRecord> passes,
            int passCount,
            int attemptCount,
            int retryCountTotal,
            bool succeeded,
            string lastCompletedPassId,
            string failurePassId,
            string failureCode,
            string failureMessage)
        {
            if (generationProfileId == null) throw new ArgumentNullException(nameof(generationProfileId));
            if (worldProfileId == null) throw new ArgumentNullException(nameof(worldProfileId));
            if (inclusivePassId == null) throw new ArgumentNullException(nameof(inclusivePassId));
            WorldGenerationExecutionRecordValidation.RequireUtc(startedUtc, nameof(startedUtc));
            WorldGenerationExecutionRecordValidation.RequireDuration(durationMilliseconds, nameof(durationMilliseconds));
            if (passes == null) throw new ArgumentNullException(nameof(passes));
            if (lastCompletedPassId == null) throw new ArgumentNullException(nameof(lastCompletedPassId));
            if (failurePassId == null) throw new ArgumentNullException(nameof(failurePassId));
            if (failureCode == null) throw new ArgumentNullException(nameof(failureCode));
            if (failureMessage == null) throw new ArgumentNullException(nameof(failureMessage));

            var copy = new List<WorldGenerationPassExecutionRecord>(passes);
            if (copy.Any(item => item == null))
                throw new ArgumentException("Pass collection cannot contain null.", nameof(passes));
            if (passCount != copy.Count)
                throw new ArgumentException("Pass count does not match the pass collection.", nameof(passCount));
            if (attemptCount != copy.Sum(item => item.AttemptCount))
                throw new ArgumentException("Attempt count does not match the pass records.", nameof(attemptCount));
            if (retryCountTotal != copy.Sum(item => item.RetryCount))
                throw new ArgumentException("Retry count does not match the pass records.", nameof(retryCountTotal));

            var previousOrder = -1;
            foreach (var pass in copy)
            {
                if (pass.WorldSeed != worldSeed)
                    throw new ArgumentException("Pass seed does not match the root record.", nameof(passes));
                if (pass.StartedUtc < startedUtc)
                    throw new ArgumentException("A pass cannot start before the root execution.", nameof(passes));
                if (pass.PassOrder <= previousOrder)
                    throw new ArgumentException("Pass records must use exact increasing execution order.", nameof(passes));
                previousOrder = pass.PassOrder;
            }

            var expectedLastCompleted = copy.LastOrDefault(item => item.Succeeded)?.PassId ?? string.Empty;
            if (!string.Equals(expectedLastCompleted, lastCompletedPassId, StringComparison.Ordinal))
                throw new ArgumentException("Last completed pass does not match the pass records.", nameof(lastCompletedPassId));

            if (succeeded)
            {
                if (failurePassId.Length != 0 || failureCode.Length != 0 || failureMessage.Length != 0)
                    throw new ArgumentException("A successful execution cannot contain failure fields.");
                if (copy.Any(item => item.Terminal))
                    throw new ArgumentException("A successful execution cannot contain a terminal pass.", nameof(passes));
            }
            else
            {
                if (failureCode.Length == 0)
                    throw new ArgumentException("A failed execution must contain a failure code.", nameof(failureCode));
                var terminalPass = copy.LastOrDefault(item => item.Terminal);
                if (terminalPass != null &&
                    (!string.Equals(terminalPass.PassId, failurePassId, StringComparison.Ordinal) ||
                     !string.Equals(terminalPass.FailureCode, failureCode, StringComparison.Ordinal) ||
                     !string.Equals(terminalPass.FailureMessage, failureMessage, StringComparison.Ordinal)))
                    throw new ArgumentException("Terminal pass failure does not match the root failure fields.", nameof(passes));
            }

            GenerationProfileId = generationProfileId;
            WorldProfileId = worldProfileId;
            WorldSeed = worldSeed;
            InclusivePassId = inclusivePassId;
            StartedUtc = startedUtc;
            DurationMilliseconds = durationMilliseconds;
            Passes = new ReadOnlyCollection<WorldGenerationPassExecutionRecord>(copy);
            PassCount = passCount;
            AttemptCount = attemptCount;
            RetryCountTotal = retryCountTotal;
            Succeeded = succeeded;
            LastCompletedPassId = lastCompletedPassId;
            FailurePassId = failurePassId;
            FailureCode = failureCode;
            FailureMessage = failureMessage;
        }

        public string GenerationProfileId { get; }
        public string WorldProfileId { get; }
        public ulong WorldSeed { get; }
        public string InclusivePassId { get; }
        public DateTimeOffset StartedUtc { get; }
        public long DurationMilliseconds { get; }
        public IReadOnlyList<WorldGenerationPassExecutionRecord> Passes { get; }
        public int PassCount { get; }
        public int AttemptCount { get; }
        public int RetryCountTotal { get; }
        public bool Succeeded { get; }
        public string LastCompletedPassId { get; }
        public string FailurePassId { get; }
        public string FailureCode { get; }
        public string FailureMessage { get; }
    }
}
