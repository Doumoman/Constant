using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Data;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SiteReservationSearchGroup
    {
        private readonly IReadOnlyList<SiteReservationSearchOption> options;

        public SiteReservationSearchGroup(
            SitePlacementKey key,
            SpecialMapDefinition specialMap,
            BiomeTypeDefinition primaryBiome,
            BiomePatchRuleDefinition corePatchRule,
            IEnumerable<SiteReservationSearchOption> options)
        {
            if (!key.IsValid) throw new ArgumentException("A valid placement key is required.", nameof(key));
            if (key.Kind == SiteReservationKind.Village)
                throw new ArgumentException("Village groups are outside reservation backtracking.", nameof(key));
            if (options == null) throw new ArgumentNullException(nameof(options));

            ValidateDefinitions(key, specialMap, primaryBiome, corePatchRule);
            var snapshot = new List<SiteReservationSearchOption>(options);
            foreach (var option in snapshot)
            {
                if (option == null)
                    throw new ArgumentException("Search options cannot contain null.", nameof(options));
                if (SitePlacementKey.FromPlacement(option.Placement) != key)
                    throw new ArgumentException("Every option placement key must match the group key.", nameof(options));
                if (key.Kind == SiteReservationKind.Start)
                {
                    if (option.Placement.Footprint.Transform != SiteFootprintTransform.R0 ||
                        option.FutureCoreAvailableSectorCount != -1)
                    {
                        throw new ArgumentException(
                            "Start options require R0 and an unavailable capacity estimate.", nameof(options));
                    }
                }
                else if (key.Kind == SiteReservationKind.Boss &&
                         option.FutureCoreAvailableSectorCount != -1)
                {
                    throw new ArgumentException(
                        "Boss options require an unavailable capacity estimate.", nameof(options));
                }
            }

            snapshot.Sort(CompareOptions);
            for (var index = 1; index < snapshot.Count; index++)
            {
                if (SameIdentity(snapshot[index - 1], snapshot[index]))
                    throw new ArgumentException("Search option identities must be unique.", nameof(options));
            }

            Key = key;
            SpecialMap = specialMap;
            PrimaryBiome = primaryBiome;
            CorePatchRule = corePatchRule;
            this.options = new ReadOnlyCollection<SiteReservationSearchOption>(snapshot);
        }

        public SitePlacementKey Key { get; }
        public SpecialMapDefinition SpecialMap { get; }
        public BiomeTypeDefinition PrimaryBiome { get; }
        public BiomePatchRuleDefinition CorePatchRule { get; }
        public IReadOnlyList<SiteReservationSearchOption> Options => options;
        public int OptionCount => options.Count;

        private static void ValidateDefinitions(
            SitePlacementKey key,
            SpecialMapDefinition specialMap,
            BiomeTypeDefinition primaryBiome,
            BiomePatchRuleDefinition corePatchRule)
        {
            if (key.Kind == SiteReservationKind.Start)
            {
                if (specialMap != null || primaryBiome != null || corePatchRule != null)
                    throw new ArgumentException("Start groups cannot receive typed site definitions.");
                return;
            }

            if (specialMap == null) throw new ArgumentNullException(nameof(specialMap));
            if (primaryBiome == null) throw new ArgumentNullException(nameof(primaryBiome));
            if (corePatchRule == null) throw new ArgumentNullException(nameof(corePatchRule));
            if (!specialMap.Active || specialMap.RequiredCount != 1 ||
                !string.Equals(specialMap.SpecialMapId, key.SourceDefinitionId, StringComparison.Ordinal) ||
                !SiteReservationTokenCodec.TryParseKind(specialMap.SiteRole, out var kind) || kind != key.Kind ||
                key.RequiredInstanceOrdinal != 0)
            {
                throw new ArgumentException("The special-map identity must match the group key.", nameof(specialMap));
            }
            if (!primaryBiome.Active ||
                !string.Equals(primaryBiome.BiomeId, specialMap.PrimaryBiomeId, StringComparison.Ordinal))
            {
                throw new ArgumentException("The primary biome must match the special map.", nameof(primaryBiome));
            }
            if (!corePatchRule.Active ||
                !string.Equals(corePatchRule.PatchRole, "CORE", StringComparison.Ordinal) ||
                !string.Equals(corePatchRule.BiomeId, primaryBiome.BiomeId, StringComparison.Ordinal))
            {
                throw new ArgumentException("The active Core rule must match the primary biome.", nameof(corePatchRule));
            }
        }

        private static int CompareOptions(
            SiteReservationSearchOption left,
            SiteReservationSearchOption right)
        {
            var origin = left.Placement.Candidate.OriginIndex.CompareTo(
                right.Placement.Candidate.OriginIndex);
            if (origin != 0) return origin;
            var transform = left.Placement.Footprint.Transform.CompareTo(
                right.Placement.Footprint.Transform);
            return transform != 0
                ? transform
                : left.Placement.Candidate.CandidateOrdinal.CompareTo(
                    right.Placement.Candidate.CandidateOrdinal);
        }

        private static bool SameIdentity(
            SiteReservationSearchOption left,
            SiteReservationSearchOption right) =>
            left.Placement.Candidate.OriginIndex == right.Placement.Candidate.OriginIndex &&
            left.Placement.Footprint.Transform == right.Placement.Footprint.Transform;
    }
}
