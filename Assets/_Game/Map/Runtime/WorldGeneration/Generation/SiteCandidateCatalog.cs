using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SiteCandidateCatalog
    {
        private readonly IReadOnlyList<SiteCandidateGroup> siteGroups;
        private readonly IReadOnlyList<SiteCandidateGroup> groups;
        private readonly IReadOnlyDictionary<GroupKey, SiteCandidateGroup> groupsByKey;

        public SiteCandidateCatalog(
            ulong seed,
            string worldProfileId,
            string generationProfileId,
            SiteCandidateGroup startGroup,
            IEnumerable<SiteCandidateGroup> siteGroups)
        {
            ReservationValidation.RequireCanonicalId(worldProfileId, nameof(worldProfileId), false);
            ReservationValidation.RequireCanonicalId(generationProfileId, nameof(generationProfileId), false);
            if (startGroup == null) throw new ArgumentNullException(nameof(startGroup));
            if (siteGroups == null) throw new ArgumentNullException(nameof(siteGroups));
            if (startGroup.Kind != SiteReservationKind.Start ||
                !string.Equals(startGroup.SourceDefinitionId, worldProfileId, StringComparison.Ordinal) ||
                startGroup.RequiredInstanceOrdinal != 0)
                throw new ArgumentException("Start group identity must match the world profile.", nameof(startGroup));

            var sites = new List<SiteCandidateGroup>(siteGroups);
            if (sites.Count != 5)
                throw new ArgumentException("Exactly five special-site groups are required.", nameof(siteGroups));
            var bossCount = 0;
            var forgeCount = 0;
            var coreCount = 0;
            foreach (var group in sites)
            {
                if (group == null)
                    throw new ArgumentException("Site groups cannot contain null.", nameof(siteGroups));
                if (group.RequiredInstanceOrdinal != 0)
                    throw new ArgumentException("Fixed site groups require instance ordinal zero.", nameof(siteGroups));
                switch (group.Kind)
                {
                    case SiteReservationKind.Boss: bossCount++; break;
                    case SiteReservationKind.Forge: forgeCount++; break;
                    case SiteReservationKind.CoreResource: coreCount++; break;
                    default: throw new ArgumentException("Only Boss, Forge, and CoreResource site groups are allowed.", nameof(siteGroups));
                }
            }
            if (bossCount != 1 || forgeCount != 1 || coreCount != 3)
                throw new ArgumentException("Site groups must contain one Boss, one Forge, and three CoreResource groups.", nameof(siteGroups));

            sites.Sort(CompareGroups);
            var all = new List<SiteCandidateGroup>(sites.Count + 1) { startGroup };
            all.AddRange(sites);
            var byKey = new Dictionary<GroupKey, SiteCandidateGroup>();
            var total = 0;
            foreach (var group in all)
            {
                if (!byKey.TryAdd(new GroupKey(group.Kind, group.SourceDefinitionId, group.RequiredInstanceOrdinal), group))
                    throw new ArgumentException("Candidate group keys must be unique.", nameof(siteGroups));
                checked { total += group.Count; }
            }

            Seed = seed;
            WorldProfileId = worldProfileId;
            GenerationProfileId = generationProfileId;
            StartGroup = startGroup;
            this.siteGroups = new ReadOnlyCollection<SiteCandidateGroup>(sites);
            groups = new ReadOnlyCollection<SiteCandidateGroup>(all);
            groupsByKey = new ReadOnlyDictionary<GroupKey, SiteCandidateGroup>(byKey);
            TotalCandidateCount = total;
        }

        public ulong Seed { get; }
        public string WorldProfileId { get; }
        public string GenerationProfileId { get; }
        public SiteCandidateGroup StartGroup { get; }
        public IReadOnlyList<SiteCandidateGroup> SiteGroups => siteGroups;
        public IReadOnlyList<SiteCandidateGroup> Groups => groups;
        public int TotalCandidateCount { get; }

        public bool TryGetGroup(
            SiteReservationKind kind,
            string sourceDefinitionId,
            int requiredInstanceOrdinal,
            out SiteCandidateGroup group)
        {
            if (sourceDefinitionId == null) throw new ArgumentNullException(nameof(sourceDefinitionId));
            if (requiredInstanceOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(requiredInstanceOrdinal));
            return groupsByKey.TryGetValue(
                new GroupKey(kind, sourceDefinitionId, requiredInstanceOrdinal),
                out group);
        }

        private static int CompareGroups(SiteCandidateGroup left, SiteCandidateGroup right)
        {
            var priority = left.PlacementPriority.CompareTo(right.PlacementPriority);
            if (priority != 0) return priority;
            var source = string.Compare(left.SourceDefinitionId, right.SourceDefinitionId, StringComparison.Ordinal);
            return source != 0
                ? source
                : left.RequiredInstanceOrdinal.CompareTo(right.RequiredInstanceOrdinal);
        }

        private readonly struct GroupKey : IEquatable<GroupKey>
        {
            private readonly SiteReservationKind kind;
            private readonly string sourceDefinitionId;
            private readonly int requiredInstanceOrdinal;

            public GroupKey(
                SiteReservationKind kind,
                string sourceDefinitionId,
                int requiredInstanceOrdinal)
            {
                this.kind = kind;
                this.sourceDefinitionId = sourceDefinitionId;
                this.requiredInstanceOrdinal = requiredInstanceOrdinal;
            }

            public bool Equals(GroupKey other)
            {
                return kind == other.kind && requiredInstanceOrdinal == other.requiredInstanceOrdinal &&
                       string.Equals(sourceDefinitionId, other.sourceDefinitionId, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is GroupKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = (int)kind;
                    hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(sourceDefinitionId);
                    return (hash * 397) ^ requiredInstanceOrdinal;
                }
            }
        }
    }
}
