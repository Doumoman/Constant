using System;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class WorldGenerationExecutionResult
    {
        public WorldGenerationExecutionResult(
            WorldGenerationRootResult result,
            WorldGenerationExecutionRecord executionRecord)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
            ExecutionRecord = executionRecord ?? throw new ArgumentNullException(nameof(executionRecord));

            if (Result.Succeeded != ExecutionRecord.Succeeded ||
                !string.Equals(Result.LastCompletedPassId, ExecutionRecord.LastCompletedPassId, StringComparison.Ordinal))
                throw new ArgumentException("Root result and execution record are inconsistent.", nameof(executionRecord));

            if (!Result.Succeeded)
            {
                var terminal = Result.Issues.Single(item => item.Terminal);
                if (!string.Equals(terminal.PassId, ExecutionRecord.FailurePassId, StringComparison.Ordinal) ||
                    !string.Equals(terminal.Code, ExecutionRecord.FailureCode, StringComparison.Ordinal) ||
                    !string.Equals(terminal.Message, ExecutionRecord.FailureMessage, StringComparison.Ordinal))
                    throw new ArgumentException("Root failure and execution record are inconsistent.", nameof(executionRecord));
            }
        }

        public WorldGenerationRootResult Result { get; }
        public WorldGenerationExecutionRecord ExecutionRecord { get; }
    }
}
