using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceBoundaryCandidateIndex
    {
        private static readonly IReadOnlyList<MoonpalaceBoundaryCandidateDefinition> EmptyCandidates =
            new ReadOnlyCollection<MoonpalaceBoundaryCandidateDefinition>(
                Array.Empty<MoonpalaceBoundaryCandidateDefinition>());

        private readonly IReadOnlyList<MoonpalaceBoundaryCandidateIndexEntry> entries;
        private readonly IReadOnlyList<MoonpalaceBoundaryCandidateKey> keys;
        private readonly IReadOnlyList<MoonpalaceBoundaryCandidateDefinition> candidates;
        private readonly IReadOnlyDictionary<MoonpalaceBoundaryCandidateKey,
            IReadOnlyList<MoonpalaceBoundaryCandidateDefinition>> candidatesByKey;

        internal MoonpalaceBoundaryCandidateIndex(IEnumerable<MoonpalaceBoundaryCandidateIndexEntry> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));

            var entryCopy = entries.ToArray();
            if (entryCopy.Any(entry => entry == null))
            {
                throw new ArgumentException("Index entries cannot contain null.", nameof(entries));
            }

            this.entries = new ReadOnlyCollection<MoonpalaceBoundaryCandidateIndexEntry>(entryCopy);
            keys = new ReadOnlyCollection<MoonpalaceBoundaryCandidateKey>(
                entryCopy.Select(entry => entry.Key).ToArray());
            candidates = new ReadOnlyCollection<MoonpalaceBoundaryCandidateDefinition>(
                entryCopy.SelectMany(entry => entry.Candidates).ToArray());
            candidatesByKey = new ReadOnlyDictionary<MoonpalaceBoundaryCandidateKey,
                IReadOnlyList<MoonpalaceBoundaryCandidateDefinition>>(
                entryCopy.ToDictionary(entry => entry.Key, entry => entry.Candidates));
        }

        public IReadOnlyList<MoonpalaceBoundaryCandidateIndexEntry> Entries => entries;
        public IReadOnlyList<MoonpalaceBoundaryCandidateKey> Keys => keys;
        public IReadOnlyList<MoonpalaceBoundaryCandidateDefinition> Candidates => candidates;
        public int Count => candidates.Count;

        public IReadOnlyList<MoonpalaceBoundaryCandidateDefinition> GetCandidates(
            MoonpalaceBoundaryCandidateKey key)
        {
            if (!key.IsDefined) throw new ArgumentException("Key is undefined.", nameof(key));
            return candidatesByKey.TryGetValue(key, out var matches) ? matches : EmptyCandidates;
        }

        public IReadOnlyList<MoonpalaceBoundaryCandidateDefinition> GetCandidates(MoonpalaceBiomePair pair)
        {
            RequirePair(pair);
            return Collect(entry => entry.Key.Pair == pair);
        }

        public IReadOnlyList<MoonpalaceBoundaryCandidateDefinition> GetCandidates(
            MoonpalaceBiomePair pair,
            MoonpalaceBoundaryOrientation orientation)
        {
            RequirePair(pair);
            RequireOrientation(orientation);
            return Collect(entry => entry.Key.Pair == pair && entry.Key.Orientation == orientation);
        }

        public IReadOnlyList<MoonpalaceBoundaryCandidateDefinition> GetCandidates(
            MoonpalaceBiomePair pair,
            MoonpalaceBoundaryProfileId profile,
            MoonpalaceBoundaryOrientation orientation)
        {
            RequirePair(pair);
            if (!profile.IsDefined) throw new ArgumentException("Profile is undefined.", nameof(profile));
            RequireOrientation(orientation);
            return Collect(entry =>
                entry.Key.Pair == pair &&
                entry.Key.Profile == profile &&
                entry.Key.Orientation == orientation);
        }

        public IReadOnlyList<MoonpalaceBoundaryCandidateDefinition> GetCandidates(
            MoonpalaceBiomePair pair,
            MoonpalaceBoundaryRouteRole routeRole)
        {
            RequirePair(pair);
            if (!routeRole.IsDefined) throw new ArgumentException("Route role is undefined.", nameof(routeRole));
            return Collect(entry => entry.Key.Pair == pair && entry.Key.RouteRole == routeRole);
        }

        private IReadOnlyList<MoonpalaceBoundaryCandidateDefinition> Collect(
            Func<MoonpalaceBoundaryCandidateIndexEntry, bool> predicate)
        {
            var matches = entries
                .Where(predicate)
                .SelectMany(entry => entry.Candidates)
                .ToArray();
            return matches.Length == 0
                ? EmptyCandidates
                : new ReadOnlyCollection<MoonpalaceBoundaryCandidateDefinition>(matches);
        }

        private static void RequirePair(MoonpalaceBiomePair pair)
        {
            if (!pair.IsDefined) throw new ArgumentException("Pair is undefined.", nameof(pair));
        }

        private static void RequireOrientation(MoonpalaceBoundaryOrientation orientation)
        {
            if (orientation != MoonpalaceBoundaryOrientation.Horizontal &&
                orientation != MoonpalaceBoundaryOrientation.Vertical)
            {
                throw new ArgumentOutOfRangeException(nameof(orientation));
            }
        }
    }
}
