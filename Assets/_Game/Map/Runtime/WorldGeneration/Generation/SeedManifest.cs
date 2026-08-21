using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SeedManifest
    {
        public const string GridCheckpointNotes = "MAP02_GRID_CHECKPOINT_V1";

        private readonly IReadOnlyList<string> failureRuleIds;

        public SeedManifest(
            string worldProfileId,
            ulong seed,
            string contentVersionHash,
            string generationProfileId,
            string generatorBuildId,
            bool approved,
            DateTimeOffset generationStartedUtc,
            int generationDurationMilliseconds,
            int retryCountTotal,
            IEnumerable<string> failureRuleIds,
            string notes)
        {
            if (worldProfileId == null) throw new ArgumentNullException(nameof(worldProfileId));
            if (contentVersionHash == null) throw new ArgumentNullException(nameof(contentVersionHash));
            if (generationProfileId == null) throw new ArgumentNullException(nameof(generationProfileId));
            if (generatorBuildId == null) throw new ArgumentNullException(nameof(generatorBuildId));
            if (failureRuleIds == null) throw new ArgumentNullException(nameof(failureRuleIds));
            if (notes == null) throw new ArgumentNullException(nameof(notes));
            if (worldProfileId.Length == 0)
                throw new ArgumentException("World profile identifier must be non-empty.", nameof(worldProfileId));
            if (generationProfileId.Length == 0)
                throw new ArgumentException("Generation profile identifier must be non-empty.", nameof(generationProfileId));
            if (generatorBuildId.Length == 0)
                throw new ArgumentException("Generator build identifier must be non-empty.", nameof(generatorBuildId));
            if (!IsLowercaseSha256(contentVersionHash))
                throw new ArgumentException("Content version hash must be lowercase 64-character hexadecimal.", nameof(contentVersionHash));
            if (generationStartedUtc.Offset != TimeSpan.Zero)
                throw new ArgumentException("Generation start must use an exact UTC offset.", nameof(generationStartedUtc));
            if (generationDurationMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(generationDurationMilliseconds));
            if (retryCountTotal < 0)
                throw new ArgumentOutOfRangeException(nameof(retryCountTotal));

            var failures = new List<string>();
            foreach (var failureRuleId in failureRuleIds)
            {
                if (failureRuleId == null)
                    throw new ArgumentException("Failure rule identifiers cannot contain null.", nameof(failureRuleIds));
                if (failureRuleId.Length == 0)
                    throw new ArgumentException("Failure rule identifiers must be non-empty.", nameof(failureRuleIds));
                if (failureRuleId.IndexOf('|') >= 0)
                    throw new ArgumentException("Failure rule identifiers cannot contain the list delimiter.", nameof(failureRuleIds));
                failures.Add(failureRuleId);
            }

            WorldProfileId = worldProfileId;
            Seed = seed;
            ContentVersionHash = contentVersionHash;
            GenerationProfileId = generationProfileId;
            GeneratorBuildId = generatorBuildId;
            Approved = approved;
            GenerationStartedUtc = generationStartedUtc;
            GenerationDurationMilliseconds = generationDurationMilliseconds;
            RetryCountTotal = retryCountTotal;
            this.failureRuleIds = new ReadOnlyCollection<string>(failures);
            Notes = notes;
        }

        public string WorldProfileId { get; }
        public ulong Seed { get; }
        public string ContentVersionHash { get; }
        public string GenerationProfileId { get; }
        public string GeneratorBuildId { get; }
        public bool Approved { get; }
        public DateTimeOffset GenerationStartedUtc { get; }
        public int GenerationDurationMilliseconds { get; }
        public int RetryCountTotal { get; }
        public IReadOnlyList<string> FailureRuleIds => failureRuleIds;
        public string Notes { get; }

        internal bool IsGridCheckpoint()
        {
            return !Approved &&
                   failureRuleIds.Count == 0 &&
                   string.Equals(Notes, GridCheckpointNotes, StringComparison.Ordinal);
        }

        internal bool HasSameFields(SeedManifest other)
        {
            if (other == null ||
                !string.Equals(WorldProfileId, other.WorldProfileId, StringComparison.Ordinal) ||
                Seed != other.Seed ||
                !string.Equals(ContentVersionHash, other.ContentVersionHash, StringComparison.Ordinal) ||
                !string.Equals(GenerationProfileId, other.GenerationProfileId, StringComparison.Ordinal) ||
                !string.Equals(GeneratorBuildId, other.GeneratorBuildId, StringComparison.Ordinal) ||
                Approved != other.Approved ||
                GenerationStartedUtc != other.GenerationStartedUtc ||
                GenerationDurationMilliseconds != other.GenerationDurationMilliseconds ||
                RetryCountTotal != other.RetryCountTotal ||
                !string.Equals(Notes, other.Notes, StringComparison.Ordinal) ||
                failureRuleIds.Count != other.failureRuleIds.Count)
                return false;

            for (var index = 0; index < failureRuleIds.Count; index++)
            {
                if (!string.Equals(failureRuleIds[index], other.failureRuleIds[index], StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        private static bool IsLowercaseSha256(string value)
        {
            if (value.Length != 64) return false;
            foreach (var character in value)
            {
                if ((character < '0' || character > '9') &&
                    (character < 'a' || character > 'f'))
                    return false;
            }
            return true;
        }
    }
}
