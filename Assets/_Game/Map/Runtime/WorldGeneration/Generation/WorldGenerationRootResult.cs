using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class WorldGenerationRootIssue
    {
        public WorldGenerationRootIssue(
            string passId,
            string code,
            string message,
            int attemptOrdinal,
            string retryScopeId,
            bool terminal)
        {
            if (passId == null) throw new ArgumentNullException(nameof(passId));
            if (string.IsNullOrEmpty(code)) throw new ArgumentException("Issue code must be non-empty.", nameof(code));
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (attemptOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(attemptOrdinal));
            if (retryScopeId == null) throw new ArgumentNullException(nameof(retryScopeId));
            PassId = passId;
            Code = code;
            Message = message;
            AttemptOrdinal = attemptOrdinal;
            RetryScopeId = retryScopeId;
            Terminal = terminal;
        }

        public string PassId { get; }
        public string Code { get; }
        public string Message { get; }
        public int AttemptOrdinal { get; }
        public string RetryScopeId { get; }
        public bool Terminal { get; }

        internal WorldGenerationRootIssue AsTerminal()
        {
            return new WorldGenerationRootIssue(
                PassId, Code, Message, AttemptOrdinal, RetryScopeId, true);
        }
    }

    public sealed class WorldGenerationRootResult
    {
        internal WorldGenerationRootResult(
            bool succeeded,
            WorldGenerationArtifactStore artifacts,
            IEnumerable<WorldGenerationRootIssue> issues,
            string lastCompletedPassId)
        {
            if (artifacts == null) throw new ArgumentNullException(nameof(artifacts));
            if (issues == null) throw new ArgumentNullException(nameof(issues));
            if (lastCompletedPassId == null) throw new ArgumentNullException(nameof(lastCompletedPassId));
            Succeeded = succeeded;
            Artifacts = artifacts;
            Issues = new ReadOnlyCollection<WorldGenerationRootIssue>(
                new List<WorldGenerationRootIssue>(issues));
            LastCompletedPassId = lastCompletedPassId;
        }

        public bool Succeeded { get; }
        public WorldGenerationArtifactStore Artifacts { get; }
        public IReadOnlyList<WorldGenerationRootIssue> Issues { get; }
        public string LastCompletedPassId { get; }
    }
}
