using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.WorldData;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    /// <summary>Operational defaults, not user-quoted probabilities. Runtime never reads MCP files.</summary>
    public sealed class Sv5DiversityProfile
    {
        public const string Salt = "SV5_DIVERSITY_V1";
        public const string Metric = "HALF_OPEN_BOUNDS_EMPTY_GAP_MANHATTAN";
        public Sv5DiversityProfile(bool enabled = true, int radius = 36,
            IEnumerable<int> weights = null, string version = "1.0.0")
        {
            if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius));
            Enabled = enabled; Radius = radius;
            Version = Sv5SpaceGraphAuthoringProfile.Require(version, nameof(version));
            var values = (weights ?? new[] { 100, 70, 49, 34 }).ToArray();
            if (values.Length != 4 || values.Any(w => w < 0 || w > 100))
                throw new ArgumentException("Exactly four percentages in [0,100] are required.", nameof(weights));
            Weights = Array.AsReadOnly(values);
            var aliases = new SortedDictionary<string,string>(StringComparer.Ordinal);
            foreach (string family in new[] { "CAVE_BAND", "LIBRARY_STACK", "CANYON_SHAFT", "HIGH_HALL",
                "FARM_TERRACE", "RANCH_YARD", "LAKE_CHAMBER", "JUMP_RESERVE" }) aliases.Add(family, family);
            foreach (string variant in new[] { "A", "B", "C", "D", "E", "F" }) aliases.Add("ORDINARY_ROOM_" + variant, "ORDINARY_ROOM");
            Aliases = new ReadOnlyDictionary<string,string>(aliases);
            Digest = Sv5WorldDefinition.Hash(Id + "|" + Version + "|" + Enabled + "|" +
                Radius.ToString(CultureInfo.InvariantCulture) + "|" + Metric + "|" + Salt + "|" +
                string.Join(",", values.Select(v => v.ToString(CultureInfo.InvariantCulture))) +
                "|CORE_EXEMPT|ORDINARY_ROOM_EXEMPT|" + string.Join(";", aliases.Select(p => p.Key + "=" + p.Value)));
        }
        public string Id => "SV5_DIVERSITY_SOFT_V1";
        public string Version { get; }
        public bool Enabled { get; }
        public int Radius { get; }
        public IReadOnlyList<int> Weights { get; }
        public IReadOnlyDictionary<string,string> Aliases { get; }
        public string Digest { get; }
        public string FamilyKey(string family) => Aliases.TryGetValue(family, out string key) ? key :
            Sv5SpaceGraphAuthoringProfile.Require(family, nameof(family));
        public bool Exempt(string family, bool core) => core || FamilyKey(family) == "ORDINARY_ROOM";
        public int Weight(string family, bool core, int neighbors) => !Enabled || Exempt(family, core) ?
            100 : Weights[Math.Min(3, Math.Max(0, neighbors))];
    }

    /// <summary>A subdivision must retain its original independent place, not invent a new spawn.</summary>
    public sealed class Sv5FormationPart
    {
        public Sv5FormationPart(string originPlaceId, string formationId, string family, Sv5SpaceBounds bounds,
            bool core = false, string partId = "WHOLE", string variantId = "", string displayName = "")
        {
            OriginPlaceId = Sv5SpaceGraphAuthoringProfile.Require(originPlaceId, nameof(originPlaceId));
            FormationId = Sv5SpaceGraphAuthoringProfile.Require(formationId, nameof(formationId));
            if (OriginPlaceId != FormationId) throw new ArgumentException("Independent places cannot be merged into another formation.");
            Family = Sv5SpaceGraphAuthoringProfile.Require(family, nameof(family));
            PartId = Sv5SpaceGraphAuthoringProfile.Require(partId, nameof(partId));
            Bounds = bounds; Core = core; VariantId = variantId; DisplayName = displayName;
        }
        public string OriginPlaceId { get; }
        public string FormationId { get; }
        public string Family { get; }
        public string PartId { get; }
        public string VariantId { get; }
        public string DisplayName { get; }
        public Sv5SpaceBounds Bounds { get; }
        public bool Core { get; }
        public static Sv5FormationPart FromPlace(Sv5SpacePlace p) =>
            new Sv5FormationPart(p.Id, p.FormationId, p.Family, p.Bounds, p.Kind == Sv5SpacePlaceKind.Core);
    }

    public sealed class Sv5DiversityPair
    {
        internal Sv5DiversityPair(Sv5FormationPart[] a, Sv5FormationPart[] b, Sv5DiversityProfile profile)
        {
            First = a[0].FormationId; Second = b[0].FormationId; Family = profile.FamilyKey(a[0].Family);
            var nearest = a.SelectMany(x => b.Select(y => new { A = x.Bounds, B = y.Bounds,
                Gap = Sv5SpaceDiversity.Gap(x.Bounds, y.Bounds) })).OrderBy(p => p.Gap)
                .ThenBy(p => p.A.ToString(), StringComparer.Ordinal).ThenBy(p => p.B.ToString(), StringComparer.Ordinal).First();
            FirstBounds = nearest.A; SecondBounds = nearest.B; Gap = nearest.Gap; Near = Gap <= profile.Radius;
            Exclusion = a[0].Core || b[0].Core ? "FIXED_CORE" : profile.Exempt(Family, false) ? "ORDINARY_CONNECTION_SPACE" : "";
        }
        public string First { get; }
        public string Second { get; }
        public string Family { get; }
        public Sv5SpaceBounds FirstBounds { get; }
        public Sv5SpaceBounds SecondBounds { get; }
        public int Gap { get; }
        public bool Near { get; }
        public string Exclusion { get; }
        public bool EligibleNear => Near && Exclusion.Length == 0;
        internal string Token => First + "|" + Second + "|" + Family + "|" + FirstBounds + "|" + SecondBounds + "|" + Gap + "|" + Near + "|" + Exclusion;
    }

    public sealed class Sv5PlacementCandidate
    {
        public Sv5PlacementCandidate(Sv5SpaceBounds bounds, int sector, int sectorOffset, ulong rank)
        { Bounds = bounds; Sector = sector; SectorOffset = sectorOffset; Rank = rank; }
        public Sv5SpaceBounds Bounds { get; }
        public int Sector { get; }
        public int SectorOffset { get; }
        public ulong Rank { get; }
    }

    public sealed class Sv5DiversityDecision
    {
        internal Sv5DiversityDecision(Sv5SpaceFamilySpec spec, ulong seed, Sv5PlacementCandidate candidate,
            IEnumerable<string> neighbors, int weight, int roll, string outcome)
        {
            RequestId = "SV5_REQUEST_" + spec.Ordinal.ToString("00", CultureInfo.InvariantCulture);
            FormationId = Sv5SpaceDiversity.PlaceId(spec, seed, candidate.Bounds);
            Family = spec.Family; Candidate = candidate; Weight = weight; Roll = roll; Outcome = outcome;
            Neighbors = Array.AsReadOnly(neighbors.OrderBy(v => v, StringComparer.Ordinal).ToArray());
        }
        public string RequestId { get; }
        public string FormationId { get; }
        public string Family { get; }
        public Sv5PlacementCandidate Candidate { get; }
        public IReadOnlyList<string> Neighbors { get; }
        public int Weight { get; }
        public int Roll { get; }
        public string Outcome { get; }
        public bool Selected => Outcome != "SOFT_REJECTED";
        public bool Fallback => Outcome == "MIN_REPEAT_LEGAL_FALLBACK";
        internal string Token => RequestId + "|" + FormationId + "|" + Family + "|" + Candidate.Bounds + "|" +
            Candidate.SectorOffset + "|" + Candidate.Rank.ToString(CultureInfo.InvariantCulture) + "|" +
            string.Join(";", Neighbors) + "|" + Weight + "|" + Roll + "|" + Outcome;
    }

    public sealed class Sv5DiversitySelection
    {
        internal Sv5DiversitySelection(Sv5PlacementCandidate chosen, IEnumerable<Sv5DiversityDecision> trace)
        { Chosen = chosen; Trace = Array.AsReadOnly(trace.ToArray()); }
        public Sv5PlacementCandidate Chosen { get; }
        public IReadOnlyList<Sv5DiversityDecision> Trace { get; }
        public string Reason => Chosen == null ? "NO_LEGAL_CANDIDATE" : Trace.Last().Outcome;
    }

    public sealed class Sv5DiversityPlan
    {
        internal Sv5DiversityPlan(ulong seed, Sv5DiversityProfile profile, IEnumerable<Sv5SpacePlace> places,
            IEnumerable<Sv5DiversityDecision> decisions)
        {
            Profile = profile; Parts = Array.AsReadOnly(places.Select(Sv5FormationPart.FromPlace).OrderBy(p => p.FormationId, StringComparer.Ordinal).ToArray());
            Pairs = Sv5SpaceDiversity.Pairs(Parts, profile);
            Decisions = Array.AsReadOnly((decisions ?? Array.Empty<Sv5DiversityDecision>()).ToArray());
            Digest = Sv5WorldDefinition.Hash(profile.Digest + "|" + seed.ToString(CultureInfo.InvariantCulture) + "\n" +
                string.Join("\n", Parts.Select(p => p.FormationId + "|" + profile.FamilyKey(p.Family) + "|" + p.Bounds + "|" + p.Core)) + "\n" +
                string.Join("\n", Pairs.Select(p => p.Token)) + "\n" + string.Join("\n", Decisions.Select(d => d.Token)));
        }
        public Sv5DiversityProfile Profile { get; }
        public IReadOnlyList<Sv5FormationPart> Parts { get; }
        public IReadOnlyList<Sv5DiversityPair> Pairs { get; }
        public IReadOnlyList<Sv5DiversityDecision> Decisions { get; }
        public string Digest { get; }
        public int EligibleNearPairs => Pairs.Count(p => p.EligibleNear);
        public string Observation => !Pairs.Any(p => p.Exclusion.Length == 0) ? "NO_ELIGIBLE_REPEAT" : "MEASURED_INDEPENDENT_FORMATIONS";
    }

    public static class Sv5SpaceDiversity
    {
        public static int Gap(Sv5SpaceBounds a, Sv5SpaceBounds b) =>
            Math.Max(0, Math.Max(b.X - a.MaxXExclusive, a.X - b.MaxXExclusive)) +
            Math.Max(0, Math.Max(b.Y - a.MaxYExclusive, a.Y - b.MaxYExclusive));

        public static int Roll(ulong seed, int ordinal, string familyKey) =>
            (int)(ulong.Parse(Sv5WorldDefinition.Hash(Sv5DiversityProfile.Salt + "|" + seed.ToString(CultureInfo.InvariantCulture) +
                "|" + ordinal.ToString(CultureInfo.InvariantCulture) + "|" + familyKey).Substring(0, 16),
                NumberStyles.HexNumber, CultureInfo.InvariantCulture) % 100UL);

        public static string PlaceId(Sv5SpaceFamilySpec spec, ulong seed, Sv5SpaceBounds bounds) =>
            "SV5_PLACE_" + spec.Ordinal.ToString("00", CultureInfo.InvariantCulture) + "_" +
            Sv5WorldDefinition.Hash(seed.ToString(CultureInfo.InvariantCulture) + "|" + spec.StableToken + "|" + bounds)
                .Substring(0,16).ToUpperInvariant();

        private static Sv5FormationPart[][] Formations(IEnumerable<Sv5FormationPart> parts, Sv5DiversityProfile profile)
        {
            var groups = parts.GroupBy(p => p.FormationId, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => g.OrderBy(p => p.PartId, StringComparer.Ordinal).ToArray()).ToArray();
            foreach (var g in groups)
                if (g.Select(p => profile.FamilyKey(p.Family)).Distinct(StringComparer.Ordinal).Count() != 1 ||
                    g.Select(p => p.Core).Distinct().Count() != 1 || g.Select(p => p.PartId).Distinct().Count() != g.Length)
                    throw new ArgumentException("FORMATION_IDENTITY_CONFLICT|" + g[0].FormationId);
            return groups;
        }

        public static IReadOnlyList<Sv5DiversityPair> Pairs(IEnumerable<Sv5FormationPart> parts, Sv5DiversityProfile profile)
        {
            var groups = Formations(parts, profile); var output = new List<Sv5DiversityPair>();
            for (int i = 0; i < groups.Length; i++) for (int j = i + 1; j < groups.Length; j++)
                if (profile.FamilyKey(groups[i][0].Family) == profile.FamilyKey(groups[j][0].Family))
                    output.Add(new Sv5DiversityPair(groups[i], groups[j], profile));
            return output.AsReadOnly();
        }

        // Candidate enumeration and hard legality are unchanged; only a request-scoped soft roll is added.
        public static Sv5DiversitySelection Select(Sv5SpaceFamilySpec spec, ulong seed,
            IEnumerable<Sv5PlacementCandidate> candidates, IEnumerable<Sv5FormationPart> placed,
            Sv5DiversityProfile profile, Func<Sv5PlacementCandidate,bool> legal = null)
        {
            var formations = Formations(placed, profile);
            string family = profile.FamilyKey(spec.Family);
            int roll = Roll(seed, spec.Ordinal, family);
            var trace = new List<Sv5DiversityDecision>(); Sv5DiversityDecision best = null;
            foreach (var c in candidates.OrderBy(c => c.SectorOffset).ThenBy(c => c.Rank).ThenBy(c => c.Bounds.Y).ThenBy(c => c.Bounds.X))
            {
                if (legal != null && !legal(c)) continue;
                var neighbors = formations.Where(g => !g[0].Core && profile.FamilyKey(g[0].Family) == family &&
                    g.Min(p => Gap(c.Bounds, p.Bounds)) <= profile.Radius).Select(g => g[0].FormationId).ToArray();
                int weight = profile.Weight(family, false, neighbors.Length);
                var d = new Sv5DiversityDecision(spec, seed, c, neighbors, weight, roll,
                    roll < weight ? (!profile.Enabled ? "POLICY_OFF_FIRST_LEGAL" : weight == 100 ? "WEIGHT_100_FIRST_LEGAL" : "SOFT_ACCEPTED") : "SOFT_REJECTED");
                trace.Add(d);
                if (d.Selected) return new Sv5DiversitySelection(c, trace);
                if (best == null || neighbors.Length < best.Neighbors.Count) best = d;
            }
            if (best == null) return new Sv5DiversitySelection(null, trace);
            trace.Add(new Sv5DiversityDecision(spec, seed, best.Candidate, best.Neighbors, best.Weight, roll, "MIN_REPEAT_LEGAL_FALLBACK"));
            return new Sv5DiversitySelection(best.Candidate, trace);
        }

        public static Sv5SpaceGraphAuthoringProfile RepeatProfile() => new Sv5SpaceGraphAuthoringProfile(
            "SV5_DIVERSITY_REPEAT_V1", "1.0.0", Sv5SpaceGraphAuthoringProfile.RepresentativeV1().Families.Concat(new[] {
                new Sv5SpaceFamilySpec(16, "CAVE_BAND", Sv5SpacePlaceKind.Large, 60, 24, "SV5_24_CAVE"),
                new Sv5SpaceFamilySpec(32, "CAVE_BAND", Sv5SpacePlaceKind.Large, 60, 24, "SV5_24_CAVE") }));
    }
}
