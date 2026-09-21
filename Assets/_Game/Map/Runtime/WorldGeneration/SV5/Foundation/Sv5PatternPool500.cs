using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.SV5.Foundation;

namespace StarNight.Map.WorldGeneration.MicroPatterns
{
    /// <summary>
    /// SV5's fixed, provenance-preserving 500 geometry pool.  The existing
    /// SV5 first pool remains addressable by its original IDs; the remaining
    /// slots are selected from SV5's deterministic 500 binary masks without
    /// treating a label or a transform as a different 16-cell geometry.
    /// </summary>
    public sealed class Sv5PatternPool500Entry
    {
        internal Sv5PatternPool500Entry(int poolIndex, string candidateId,
            IEnumerable<Sv5PatternBaseCell> baseCells, Sv5PatternPrimaryRole role,
            Sv5PatternIntentTag tags, string sourceId, string sourceMaskReference,
            string originalReference, Sv5PatternTransform transform, string ruleId,
            string selectionReason, bool isInitialPool,
            Sv5PatternAutomaticCharacteristics characteristics, IEnumerable<string> tagConflicts)
        {
            PoolIndex = poolIndex;
            CandidateId = candidateId ?? throw new ArgumentNullException(nameof(candidateId));
            BaseCells = new ReadOnlyCollection<Sv5PatternBaseCell>((baseCells ??
                throw new ArgumentNullException(nameof(baseCells))).ToArray());
            if (BaseCells.Count != Sv5PatternCatalog.CellCount)
                throw new ArgumentException("A pool entry requires exactly 16 cells.", nameof(baseCells));
            PrimaryRole = role;
            IntentTags = tags;
            SourceId = sourceId ?? string.Empty;
            SourceMaskReference = sourceMaskReference ?? string.Empty;
            OriginalReference = originalReference ?? string.Empty;
            Transform = transform;
            RuleId = ruleId ?? string.Empty;
            SelectionReason = selectionReason ?? string.Empty;
            IsInitialPool = isInitialPool;
            Characteristics = characteristics ?? Sv5PatternCatalog.CalculateCharacteristics(BaseCells);
            TagConflicts = new ReadOnlyCollection<string>((tagConflicts ?? Array.Empty<string>()).Distinct(
                StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        public int PoolIndex { get; }
        public string CandidateId { get; }
        public IReadOnlyList<Sv5PatternBaseCell> BaseCells { get; }
        public string BaseCells16 => Sv5PatternCatalog.SerializeBaseCells16(BaseCells);
        public Sv5PatternPrimaryRole PrimaryRole { get; }
        public Sv5PatternIntentTag IntentTags { get; }
        public string SourceId { get; }
        public string SourceMaskReference { get; }
        public string OriginalReference { get; }
        public Sv5PatternTransform Transform { get; }
        public string RuleId { get; }
        public string SelectionReason { get; }
        public bool IsInitialPool { get; }
        public Sv5PatternAutomaticCharacteristics Characteristics { get; }
        public IReadOnlyList<string> TagConflicts { get; }
        public bool HasOnlyAirBorder => Enumerable.Range(0, Sv5PatternCatalog.Width).All(x =>
            GetCell(x, 0) == Sv5PatternBaseCell.Air && GetCell(x, Sv5PatternCatalog.Height - 1) == Sv5PatternBaseCell.Air) &&
            Enumerable.Range(0, Sv5PatternCatalog.Height).All(y =>
                GetCell(0, y) == Sv5PatternBaseCell.Air && GetCell(Sv5PatternCatalog.Width - 1, y) == Sv5PatternBaseCell.Air);

        public Sv5PatternBaseCell GetCell(int x, int y) => BaseCells[(y * Sv5PatternCatalog.Width) + x];
    }

    public sealed class Sv5PatternPool500SourceRecord
    {
        internal Sv5PatternPool500SourceRecord(string sourceId, string sourceMaskReference,
            string originalReference, Sv5PatternTransform transform, string finalCandidateId,
            string decision)
        {
            SourceId = sourceId ?? string.Empty;
            SourceMaskReference = sourceMaskReference ?? string.Empty;
            OriginalReference = originalReference ?? string.Empty;
            Transform = transform;
            FinalCandidateId = finalCandidateId ?? string.Empty;
            Decision = decision ?? string.Empty;
        }

        public string SourceId { get; }
        public string SourceMaskReference { get; }
        public string OriginalReference { get; }
        public Sv5PatternTransform Transform { get; }
        public string FinalCandidateId { get; }
        public string Decision { get; }
    }

    /// <summary>Authoritative SV5 user-revision mapping, expressed as data rather than a generated CSV.</summary>
    public sealed class Sv5PatternPool500Replacement
    {
        internal Sv5PatternPool500Replacement(int atlasNo, int poolIndex, string oldCandidateId,
            string oldBaseCells16, string newBaseCells16, bool oldInitialPool)
        {
            AtlasNo = atlasNo;
            PoolIndex = poolIndex;
            OldCandidateId = oldCandidateId;
            OldBaseCells16 = oldBaseCells16;
            NewBaseCells16 = newBaseCells16;
            OldInitialPool = oldInitialPool;
        }

        public int AtlasNo { get; }
        public int PoolIndex { get; }
        public string OldCandidateId { get; }
        public string OldBaseCells16 { get; }
        public string NewBaseCells16 { get; }
        public bool OldInitialPool { get; }
    }

    public sealed class Sv5PatternPool500RoleSummary
    {
        internal Sv5PatternPool500RoleSummary(Sv5PatternPrimaryRole role, int target, int actual,
            string adjustmentReason)
        {
            Role = role; Target = target; Actual = actual; AdjustmentReason = adjustmentReason ?? string.Empty;
        }

        public Sv5PatternPrimaryRole Role { get; }
        public int Target { get; }
        public int Actual { get; }
        public int Difference => Actual - Target;
        public string AdjustmentReason { get; }
    }

    public sealed class Sv5PatternPool500Snapshot
    {
        internal Sv5PatternPool500Snapshot(IEnumerable<Sv5PatternPool500Entry> candidates,
            IEnumerable<Sv5PatternPool500SourceRecord> provenance,
            IEnumerable<Sv5PatternPool500RoleSummary> roleSummary, string digest)
        {
            Candidates = new ReadOnlyCollection<Sv5PatternPool500Entry>((candidates ??
                Array.Empty<Sv5PatternPool500Entry>()).ToArray());
            Provenance = new ReadOnlyCollection<Sv5PatternPool500SourceRecord>((provenance ??
                Array.Empty<Sv5PatternPool500SourceRecord>()).OrderBy(value => value.SourceId,
                StringComparer.Ordinal).ThenBy(value => value.Transform).ToArray());
            RoleSummary = new ReadOnlyCollection<Sv5PatternPool500RoleSummary>((roleSummary ??
                Array.Empty<Sv5PatternPool500RoleSummary>()).OrderBy(value => value.Role).ToArray());
            Digest = digest ?? string.Empty;
        }

        public IReadOnlyList<Sv5PatternPool500Entry> Candidates { get; }
        public IReadOnlyList<Sv5PatternPool500SourceRecord> Provenance { get; }
        public IReadOnlyList<Sv5PatternPool500RoleSummary> RoleSummary { get; }
        public string Digest { get; }

        public bool TryGetCandidate(string candidateId, out Sv5PatternPool500Entry candidate)
        {
            candidate = Candidates.FirstOrDefault(value => string.Equals(value.CandidateId, candidateId,
                StringComparison.Ordinal));
            return candidate != null;
        }
    }

    public static class Sv5PatternPool500
    {
        public const int RequiredCandidateCount = 500;
        public const string DataVersion = "SV5_POOL500_V2_FIX25";
        public const string SourceDefinition =
            "Assets/_Game/Map/Runtime/WorldGeneration/SV5/Foundation/Sv5PatternMaskLibrary.cs";
        private static readonly Lazy<Sv5PatternPool500Snapshot> FinalPool =
            new Lazy<Sv5PatternPool500Snapshot>(BuildFinalPoolInternal);

        public static Sv5PatternPool500Snapshot BuildFinalPool() => FinalPool.Value;

        public static IReadOnlyList<Sv5PatternPool500Replacement> Fix25Replacements => Fix25;

        /// <summary>Returns a non-SV5, border-safe entry for the direct small-run consumer.</summary>
        public static Sv5PatternPool500Entry SelectPortSafeDetail(int seed, int ordinal)
        {
            Sv5PatternPool500Entry[] usable = BuildFinalPool().Candidates.Where(value => !value.IsInitialPool &&
                value.HasOnlyAirBorder && value.PrimaryRole != Sv5PatternPrimaryRole.VoidClear &&
                value.Characteristics.SolidCount > 0).OrderBy(value => value.PoolIndex).ToArray();
            if (usable.Length == 0) throw new InvalidOperationException("SV5 has no port-safe final-pool detail candidate.");
            return usable[StableIndex(seed, ordinal, usable.Length)];
        }

        public static string ExportFinalPoolCsv(Sv5PatternPool500Snapshot pool)
        {
            if (pool == null) throw new ArgumentNullException(nameof(pool));
            var text = new StringBuilder("PoolIndex,CandidateId,BaseCells16,PrimaryRole,SourceId,SourceMaskReference,OriginalReference,Transform,RuleId,SelectionReason,InitialPool,IntentTags,TagConflicts,ContextRequired,SolidCount,AirCount,OneWayCount\n");
            foreach (Sv5PatternPool500Entry candidate in pool.Candidates)
            {
                text.Append(Csv(candidate.PoolIndex.ToString(CultureInfo.InvariantCulture), candidate.CandidateId,
                    candidate.BaseCells16, candidate.PrimaryRole.ToString(), candidate.SourceId,
                    candidate.SourceMaskReference, candidate.OriginalReference, candidate.Transform.ToString(),
                    candidate.RuleId, candidate.SelectionReason, candidate.IsInitialPool ? "true" : "false",
                    candidate.IntentTags.ToString(), string.Join(";", candidate.TagConflicts),
                    string.Join(";", candidate.Characteristics.ContextRequired),
                    candidate.Characteristics.SolidCount.ToString(CultureInfo.InvariantCulture),
                    candidate.Characteristics.AirCount.ToString(CultureInfo.InvariantCulture),
                    candidate.Characteristics.OneWayCount.ToString(CultureInfo.InvariantCulture))).Append('\n');
            }
            return text.ToString();
        }

        public static void ValidateFinalPool(Sv5PatternPool500Snapshot pool)
        {
            if (pool == null) throw new ArgumentNullException(nameof(pool));
            if (pool.Candidates.Count != RequiredCandidateCount)
                throw new InvalidOperationException("SV5 requires exactly 500 final candidates.");
            if (pool.Candidates.Select(value => value.CandidateId).Distinct(StringComparer.Ordinal).Count() != RequiredCandidateCount ||
                pool.Candidates.Select(value => value.BaseCells16).Distinct(StringComparer.Ordinal).Count() != RequiredCandidateCount)
                throw new InvalidOperationException("SV5 candidate IDs and final 16-cell geometries must both be unique.");
            if (pool.Candidates.Any(value => value.BaseCells.Count != Sv5PatternCatalog.CellCount) ||
                pool.Candidates.Any(value => value.BaseCells.Any(cell => cell < Sv5PatternBaseCell.Air || cell > Sv5PatternBaseCell.OneWayPlatform)))
                throw new InvalidOperationException("SV5 has an invalid typed base cell.");
            if (pool.Candidates.Count(value => value.PrimaryRole == Sv5PatternPrimaryRole.VoidClear) != 1)
                throw new InvalidOperationException("SV5 requires exactly one VOID_CLEAR geometry.");
            if (Enum.GetValues(typeof(Sv5PatternPrimaryRole)).Cast<Sv5PatternPrimaryRole>().Any(role =>
                !pool.Candidates.Any(value => value.PrimaryRole == role)))
                throw new InvalidOperationException("SV5 requires an actual classified candidate for all ten roles.");
            if (pool.Candidates.Count(value => value.IsInitialPool) != Sv5PatternCatalog.InitialPoolCount - 2)
                throw new InvalidOperationException("SV5 FIX25 must retain 46 untouched SV5 first-pool candidates.");
            if (pool.Provenance.Count(value => value.SourceId.StartsWith("SV5_MP_", StringComparison.Ordinal)) <
                Sv5PatternMaskLibrary.AcceptedCandidateCount)
                throw new InvalidOperationException("SV5 must keep a decision record for each source mask.");
        }

        private static Sv5PatternPool500Snapshot BuildFinalPoolInternal()
        {
            Sv5PatternCatalogSnapshot firstPool = Sv5PatternCatalog.BuildInitialPool();
            Sv5PatternMaskCandidateSet sourceSet = Sv5PatternMaskLibrary.Build();
            var entries = new List<Sv5PatternPool500Entry>();
            var geometryToEntry = new Dictionary<string, Sv5PatternPool500Entry>(StringComparer.Ordinal);
            var provenance = new List<Sv5PatternPool500SourceRecord>();

            foreach (Sv5PatternCandidate candidate in firstPool.Candidates)
            {
                var entry = new Sv5PatternPool500Entry(entries.Count, candidate.CandidateId, candidate.BaseCells,
                    candidate.PrimaryRole, candidate.IntentTags,
                    string.Join(";", candidate.Origins.Select(value => value.SourcePatternId).Distinct(StringComparer.Ordinal)),
                    string.Join(";", candidate.Origins.Select(value => value.SourceMaskReference).Distinct(StringComparer.Ordinal)),
                    string.Join(";", candidate.Origins.Select(value => value.OriginalReference).Distinct(StringComparer.Ordinal)),
                    Sv5PatternTransform.R0, string.Empty, "RETAINED_SV507_FIRST_POOL", true,
                    candidate.Characteristics, candidate.TagConflicts);
                Add(entry, entries, geometryToEntry);
                foreach (Sv5PatternOrigin origin in candidate.Origins)
                    provenance.Add(new Sv5PatternPool500SourceRecord(origin.SourcePatternId, origin.SourceMaskReference,
                        origin.OriginalReference, origin.Transform, entry.CandidateId, "RETAINED_SV507_FIRST_POOL"));
            }

            var remaining = sourceSet.Candidates.Select(candidate => CreateCandidateDraft(candidate)).Where(candidate =>
                !geometryToEntry.ContainsKey(candidate.BaseCells16)).ToList();
            var targets = Targets();
            foreach (Sv5PatternPrimaryRole role in Enum.GetValues(typeof(Sv5PatternPrimaryRole)))
            {
                int needed = targets[role] - entries.Count(value => value.PrimaryRole == role);
                foreach (CandidateDraft draft in remaining.Where(value => value.PrimaryRole == role).OrderBy(value => value.Rank).ToArray())
                {
                    if (needed <= 0) break;
                    Add(ToEntry(entries.Count, draft, "RETAINED_ROLE_TARGET"), entries, geometryToEntry);
                    remaining.Remove(draft);
                    needed--;
                }
            }
            foreach (CandidateDraft draft in remaining.OrderBy(value => value.Rank).ToArray())
            {
                if (entries.Count == RequiredCandidateCount) break;
                Add(ToEntry(entries.Count, draft, "RETAINED_POOL_CAP_FILL"), entries, geometryToEntry);
                remaining.Remove(draft);
            }
            if (entries.Count != RequiredCandidateCount)
                throw new InvalidOperationException("SV5 could not reach exactly 500 unique final geometries from the approved sources.");

            ApplyFix25(entries, geometryToEntry, provenance);

            foreach (Sv5PatternMaskCandidate candidate in sourceSet.Candidates)
            {
                Sv5PatternBaseCell[] cells = CandidateCells(candidate);
                string geometry = Sv5PatternCatalog.SerializeBaseCells16(cells);
                if (geometryToEntry.TryGetValue(geometry, out Sv5PatternPool500Entry entry))
                {
                    string decision = entry.IsInitialPool ? "MERGED_WITH_RETAINED_SV507_16_CELL_GEOMETRY" :
                        "RETAINED_FINAL_POOL";
                    provenance.Add(new Sv5PatternPool500SourceRecord(candidate.CandidateId, candidate.MaskU16Hex,
                        SourceDefinition, Sv5PatternTransform.R0, entry.CandidateId, decision));
                }
                else
                {
                    provenance.Add(new Sv5PatternPool500SourceRecord(candidate.CandidateId, candidate.MaskU16Hex,
                        SourceDefinition, Sv5PatternTransform.R0, string.Empty,
                        "EXCLUDED_AFTER_FIXED_500_CAP_AND_SV507_RETENTION"));
                }
            }

            Sv5PatternPool500RoleSummary[] summary = Enum.GetValues(typeof(Sv5PatternPrimaryRole)).Cast<Sv5PatternPrimaryRole>()
                .Select(role =>
                {
                    int actual = entries.Count(value => value.PrimaryRole == role);
                    int target = targets[role];
                    return new Sv5PatternPool500RoleSummary(role, target, actual, actual == target
                        ? "INITIAL_A13_TARGET_MET_WITH_RETAINED_ACTUAL_GEOMETRY"
                        : "A13_TARGET_ADJUSTED_TO_ACTUAL_CLASSIFICATION_AFTER_SV507_RETENTION_AND_FIXED_500_CAP");
                }).ToArray();
            var result = new Sv5PatternPool500Snapshot(entries, provenance, summary,
                Digest(entries.Select(value => value.PoolIndex.ToString(CultureInfo.InvariantCulture) + "|" +
                    value.CandidateId + "|" + value.BaseCells16 + "|" + value.PrimaryRole + "|" + value.SourceId)));
            ValidateFinalPool(result);
            return result;
        }

        private static void ApplyFix25(IList<Sv5PatternPool500Entry> entries,
            IDictionary<string, Sv5PatternPool500Entry> geometryToEntry,
            ICollection<Sv5PatternPool500SourceRecord> provenance)
        {
            if (entries.Count != RequiredCandidateCount)
                throw new InvalidOperationException("SV5 FIX25 requires the unmodified V1 500-entry source pool.");

            foreach (Sv5PatternPool500Replacement replacement in Fix25)
            {
                if (replacement.PoolIndex < 0 || replacement.PoolIndex >= entries.Count)
                    throw new InvalidOperationException("SV5 FIX25 has an out-of-range pool index: " + replacement.PoolIndex);

                Sv5PatternPool500Entry old = entries[replacement.PoolIndex];
                if (!string.Equals(old.CandidateId, replacement.OldCandidateId, StringComparison.Ordinal) ||
                    !string.Equals(old.BaseCells16, replacement.OldBaseCells16, StringComparison.Ordinal) ||
                    old.IsInitialPool != replacement.OldInitialPool)
                    throw new InvalidOperationException("SV5 FIX25 source mismatch at atlas " + replacement.AtlasNo +
                        ": expected exact index, ID, cells, and initial-pool state.");

                if (!geometryToEntry.Remove(old.BaseCells16) || geometryToEntry.ContainsKey(replacement.NewBaseCells16))
                    throw new InvalidOperationException("SV5 FIX25 cannot preserve unique geometry at atlas " + replacement.AtlasNo);

                Sv5PatternBaseCell[] cells = Sv5PatternCatalog.ParseBaseCells16(replacement.NewBaseCells16).ToArray();
                var updated = new Sv5PatternPool500Entry(old.PoolIndex,
                    "SV5_" + Hash(replacement.NewBaseCells16).Substring(0, 12).ToUpperInvariant(), cells,
                    Sv5PatternCatalog.Classify(cells), old.IntentTags, old.SourceId, old.SourceMaskReference,
                    old.OriginalReference, old.Transform, "SV5_FIX25_ATLAS_" + replacement.AtlasNo,
                    "SV5_FIX25_USER_AUTHORIZED_REPLACEMENT", false,
                    Sv5PatternCatalog.CalculateCharacteristics(cells), old.TagConflicts);
                entries[replacement.PoolIndex] = updated;
                geometryToEntry.Add(updated.BaseCells16, updated);
                provenance.Add(new Sv5PatternPool500SourceRecord(old.SourceId, old.SourceMaskReference,
                    old.OriginalReference, old.Transform, updated.CandidateId,
                    "REPLACED_IN_SV511_FINAL_CONSUMER_BY_FIX25_ATLAS_" + replacement.AtlasNo));
            }
        }

        private static readonly Sv5PatternPool500Replacement[] Fix25 =
        {
            new Sv5PatternPool500Replacement(2, 11, "SV5_2CFD0D58C833", "AASSAAASASSASSAA", "AASSAAASAAAAAAAA", true),
            new Sv5PatternPool500Replacement(7, 51, "SV5_C2E089D7CDB3", "ASSSASSSSAASAAAS", "ASSSASSSAAASAAAA", false),
            new Sv5PatternPool500Replacement(8, 52, "SV5_3053D2016EF5", "ASSSAASSSASSSASS", "ASSSAASSAASSAAAA", false),
            new Sv5PatternPool500Replacement(13, 57, "SV5_6DE407432111", "AASSAAASASSASSSA", "ASSSAAASAAAAAAAA", false),
            new Sv5PatternPool500Replacement(14, 58, "SV5_E77354379393", "ASSSAASSASASASAA", "ASSSAASSAAASAAAA", false),
            new Sv5PatternPool500Replacement(16, 60, "SV5_13BB86F8C0CA", "ASSSASSSSASSAAAS", "ASSSASSSAASSAAAA", false),
            new Sv5PatternPool500Replacement(18, 62, "SV5_3373D2000288", "AASSSASSSASSAAAS", "AASSAASSAAASAAAA", false),
            new Sv5PatternPool500Replacement(19, 3, "SV5_16A6ECEF6D87", "SSAASAAAASSAAASS", "SSAASAAAAAAAAAAA", true),
            new Sv5PatternPool500Replacement(36, 77, "SV5_65268408D2A1", "SSSASAAAASASASAS", "SSSASAAAAAAAAAAA", false),
            new Sv5PatternPool500Replacement(38, 79, "SV5_74296A9C3630", "SSAASAAASSAASSSA", "SSAASAAASAAAAAAA", false),
            new Sv5PatternPool500Replacement(42, 83, "SV5_0DC100EF5173", "SSSASAAASAAASSSA", "SSSASAAASAAAAAAA", false),
            new Sv5PatternPool500Replacement(44, 85, "SV5_E705215B3E59", "SSAASSSASSSASAAA", "SSSASSSASSAAAAAA", false),
            new Sv5PatternPool500Replacement(46, 87, "SV5_8AE4B6B2B548", "SSAASAASASSSASSS", "SSAASSAAAAAAAAAA", false),
            new Sv5PatternPool500Replacement(47, 88, "SV5_2C4A8DEF1659", "SSAASAASSSSSSAAA", "SSAASSAASAAAAAAA", false),
            new Sv5PatternPool500Replacement(49, 90, "SV5_86D57CDB4651", "SSSSSSSASAAASSSS", "SSSSSSSASAAAAAAA", false),
            new Sv5PatternPool500Replacement(173, 140, "SV5_3103BBE9C8B7", "SSSSSAAASAAASSSS", "SSSSSSAASAAASAAA", false),
            new Sv5PatternPool500Replacement(174, 141, "SV5_D9D9613912DC", "SAAASSASSSSSSAAA", "SSSSSSAASSAASAAA", false),
            new Sv5PatternPool500Replacement(182, 149, "SV5_C3D9C850E0F6", "SSSASSSASASSSAAA", "SSSSSSSASAAASAAA", false),
            new Sv5PatternPool500Replacement(188, 155, "SV5_FAD37FE14D72", "SAAASAAASASASSAA", "SSSSSSSASSSASSAA", false),
            new Sv5PatternPool500Replacement(190, 157, "SV5_5758E90A31B3", "SAAASSSASASASAAS", "SSSSSSSASSSASAAA", false),
            new Sv5PatternPool500Replacement(197, 161, "SV5_249C06C89B32", "ASASASASASSSAAAS", "SSSSASSSASSSAAAS", false),
            new Sv5PatternPool500Replacement(205, 169, "SV5_81FC8AFE293D", "SAASSASSSASSAAAS", "SSSSAASSAASSAAAS", false),
            new Sv5PatternPool500Replacement(207, 171, "SV5_107C4AFAC963", "SAASSAASSSASAASS", "SSSSAASSAAASAAAS", false),
            new Sv5PatternPool500Replacement(219, 183, "SV5_10BDEE2F1D49", "AAASASSSASASAAAS", "SSSSASSSAAASAAAS", false),
            new Sv5PatternPool500Replacement(221, 185, "SV5_F58607A347F7", "ASSSASASSAASAASS", "SSSSASSSAASSAASS", false),
        };

        private static void Add(Sv5PatternPool500Entry entry, ICollection<Sv5PatternPool500Entry> entries,
            IDictionary<string, Sv5PatternPool500Entry> geometryToEntry)
        {
            string geometry = entry.BaseCells16;
            if (geometryToEntry.ContainsKey(geometry))
                throw new InvalidOperationException("SV5 attempted to add a duplicate final 16-cell geometry.");
            entries.Add(entry);
            geometryToEntry.Add(geometry, entry);
        }

        private static Sv5PatternPool500Entry ToEntry(int index, CandidateDraft draft, string reason) =>
            new Sv5PatternPool500Entry(index, "SV5_" + Hash(draft.BaseCells16).Substring(0, 12).ToUpperInvariant(),
                draft.Cells, draft.PrimaryRole, Sv5PatternIntentTag.OptionalOnly | Sv5PatternIntentTag.QuietFill,
                draft.SourceId, draft.Mask, SourceDefinition, Sv5PatternTransform.R0, string.Empty, reason, false,
                draft.Characteristics, Array.Empty<string>());

        private static CandidateDraft CreateCandidateDraft(Sv5PatternMaskCandidate candidate)
        {
            Sv5PatternBaseCell[] cells = CandidateCells(candidate);
            return new CandidateDraft(candidate.CandidateId, candidate.MaskU16Hex, candidate.Rank, cells,
                Sv5PatternCatalog.Classify(cells), Sv5PatternCatalog.CalculateCharacteristics(cells));
        }

        private static Sv5PatternBaseCell[] CandidateCells(Sv5PatternMaskCandidate candidate) =>
            Enumerable.Range(0, Sv5PatternCatalog.CellCount).Select(index => (candidate.Mask & (1 << index)) != 0
                ? Sv5PatternBaseCell.Solid : Sv5PatternBaseCell.Air).ToArray();

        private static Dictionary<Sv5PatternPrimaryRole, int> Targets() =>
            new Dictionary<Sv5PatternPrimaryRole, int>
            {
                [Sv5PatternPrimaryRole.SlopeRiseRight] = 55,
                [Sv5PatternPrimaryRole.SlopeRiseLeft] = 55,
                [Sv5PatternPrimaryRole.CeilingFlat] = 40,
                [Sv5PatternPrimaryRole.CeilingRough] = 50,
                [Sv5PatternPrimaryRole.WallLeft] = 35,
                [Sv5PatternPrimaryRole.WallRight] = 35,
                [Sv5PatternPrimaryRole.VoidClear] = 1,
                [Sv5PatternPrimaryRole.SparseAirPlatform] = 95,
                [Sv5PatternPrimaryRole.StandableLedge] = 100,
                [Sv5PatternPrimaryRole.VerticalPassage] = 34,
            };

        private static int StableIndex(int seed, int ordinal, int limit)
        {
            unchecked { return (int)((uint)(seed * 1103515245 + ordinal * 12345) % (uint)limit); }
        }

        private static string Digest(IEnumerable<string> lines)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(DataVersion + "\n" + string.Join("\n", lines ?? Array.Empty<string>()));
                return string.Concat(sha.ComputeHash(bytes).Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        private static string Hash(string value)
        {
            using (var sha = SHA256.Create())
            {
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty)).Select(byteValue =>
                    byteValue.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        private static string Csv(params string[] fields) => string.Join(",", fields.Select(field => "\"" +
            (field ?? string.Empty).Replace("\"", "\"\"") + "\""));

        private sealed class CandidateDraft
        {
            public CandidateDraft(string sourceId, string mask, int rank, Sv5PatternBaseCell[] cells,
                Sv5PatternPrimaryRole primaryRole, Sv5PatternAutomaticCharacteristics characteristics)
            {
                SourceId = sourceId; Mask = mask; Rank = rank; Cells = cells; PrimaryRole = primaryRole;
                Characteristics = characteristics;
            }
            public string SourceId { get; }
            public string Mask { get; }
            public int Rank { get; }
            public Sv5PatternBaseCell[] Cells { get; }
            public string BaseCells16 => Sv5PatternCatalog.SerializeBaseCells16(Cells);
            public Sv5PatternPrimaryRole PrimaryRole { get; }
            public Sv5PatternAutomaticCharacteristics Characteristics { get; }
        }
    }
}
