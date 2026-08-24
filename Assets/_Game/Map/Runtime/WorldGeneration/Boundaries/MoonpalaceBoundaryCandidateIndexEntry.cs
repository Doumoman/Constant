using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceBoundaryCandidateIndexEntry
    {
        private readonly IReadOnlyList<MoonpalaceBoundaryCandidateDefinition> candidates;

        internal MoonpalaceBoundaryCandidateIndexEntry(
            MoonpalaceBoundaryCandidateKey key,
            IEnumerable<MoonpalaceBoundaryCandidateDefinition> candidates)
        {
            if (!key.IsDefined) throw new ArgumentException("Key is undefined.", nameof(key));
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));

            var copy = candidates.ToArray();
            if (copy.Length == 0 || copy.Any(candidate => candidate == null || candidate.Key != key))
            {
                throw new ArgumentException("An index entry requires matching non-null candidates.", nameof(candidates));
            }

            Key = key;
            this.candidates = new ReadOnlyCollection<MoonpalaceBoundaryCandidateDefinition>(copy);
        }

        public MoonpalaceBoundaryCandidateKey Key { get; }
        public IReadOnlyList<MoonpalaceBoundaryCandidateDefinition> Candidates => candidates;
    }
}
