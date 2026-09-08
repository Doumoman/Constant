using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.MoonPalace.Visualization;

namespace StarNight.Map.WorldGeneration.MicroPatterns
{
    /// <summary>
    /// RMAP11's fixed, provenance-preserving 500 geometry pool.  The existing
    /// RMAP07 first pool remains addressable by its original IDs; the remaining
    /// slots are selected from RUN06/VIS01's actual 500 binary masks without
    /// treating a label or a transform as a different 16-cell geometry.
    /// </summary>
    public sealed class RmapPatternPool500Entry
    {
        internal RmapPatternPool500Entry(int poolIndex, string candidateId,
            IEnumerable<RmapPatternBaseCell> baseCells, RmapPatternPrimaryRole role,
            RmapPatternIntentTag tags, string sourceId, string sourceMaskReference,
            string originalReference, RmapPatternTransform transform, string ruleId,
            string selectionReason, bool isInitialPool,
            RmapPatternAutomaticCharacteristics characteristics, IEnumerable<string> tagConflicts)
        {
            PoolIndex = poolIndex;
            CandidateId = candidateId ?? throw new ArgumentNullException(nameof(candidateId));
            BaseCells = new ReadOnlyCollection<RmapPatternBaseCell>((baseCells ??
                throw new ArgumentNullException(nameof(baseCells))).ToArray());
            if (BaseCells.Count != RmapPatternCatalog.CellCount)
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
            Characteristics = characteristics ?? RmapPatternCatalog.CalculateCharacteristics(BaseCells);
            TagConflicts = new ReadOnlyCollection<string>((tagConflicts ?? Array.Empty<string>()).Distinct(
                StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        public int PoolIndex { get; }
        public string CandidateId { get; }
        public IReadOnlyList<RmapPatternBaseCell> BaseCells { get; }
        public string BaseCells16 => RmapPatternCatalog.SerializeBaseCells16(BaseCells);
        public RmapPatternPrimaryRole PrimaryRole { get; }
        public RmapPatternIntentTag IntentTags { get; }
        public string SourceId { get; }
        public string SourceMaskReference { get; }
        public string OriginalReference { get; }
        public RmapPatternTransform Transform { get; }
        public string RuleId { get; }
        public string SelectionReason { get; }
        public bool IsInitialPool { get; }
        public RmapPatternAutomaticCharacteristics Characteristics { get; }
        public IReadOnlyList<string> TagConflicts { get; }
        public bool HasOnlyAirBorder => Enumerable.Range(0, RmapPatternCatalog.Width).All(x =>
            GetCell(x, 0) == RmapPatternBaseCell.Air && GetCell(x, RmapPatternCatalog.Height - 1) == RmapPatternBaseCell.Air) &&
            Enumerable.Range(0, RmapPatternCatalog.Height).All(y =>
                GetCell(0, y) == RmapPatternBaseCell.Air && GetCell(RmapPatternCatalog.Width - 1, y) == RmapPatternBaseCell.Air);

        public RmapPatternBaseCell GetCell(int x, int y) => BaseCells[(y * RmapPatternCatalog.Width) + x];
    }

    public sealed class RmapPatternPool500SourceRecord
    {
        internal RmapPatternPool500SourceRecord(string sourceId, string sourceMaskReference,
            string originalReference, RmapPatternTransform transform, string finalCandidateId,
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
        public RmapPatternTransform Transform { get; }
        public string FinalCandidateId { get; }
        public string Decision { get; }
    }

    /// <summary>Authoritative RMAP11 user-revision mapping, expressed as data rather than a generated CSV.</summary>
    public sealed class RmapPatternPool500Replacement
    {
        internal RmapPatternPool500Replacement(int atlasNo, int poolIndex, string oldCandidateId,
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

    public sealed class RmapPatternPool500RoleSummary
    {
        internal RmapPatternPool500RoleSummary(RmapPatternPrimaryRole role, int target, int actual,
            string adjustmentReason)
        {
            Role = role; Target = target; Actual = actual; AdjustmentReason = adjustmentReason ?? string.Empty;
        }

        public RmapPatternPrimaryRole Role { get; }
        public int Target { get; }
        public int Actual { get; }
        public int Difference => Actual - Target;
        public string AdjustmentReason { get; }
    }

    public sealed class RmapPatternPool500Snapshot
    {
        internal RmapPatternPool500Snapshot(IEnumerable<RmapPatternPool500Entry> candidates,
            IEnumerable<RmapPatternPool500SourceRecord> provenance,
            IEnumerable<RmapPatternPool500RoleSummary> roleSummary, string digest)
        {
            Candidates = new ReadOnlyCollection<RmapPatternPool500Entry>((candidates ??
                Array.Empty<RmapPatternPool500Entry>()).ToArray());
            Provenance = new ReadOnlyCollection<RmapPatternPool500SourceRecord>((provenance ??
                Array.Empty<RmapPatternPool500SourceRecord>()).OrderBy(value => value.SourceId,
                StringComparer.Ordinal).ThenBy(value => value.Transform).ToArray());
            RoleSummary = new ReadOnlyCollection<RmapPatternPool500RoleSummary>((roleSummary ??
                Array.Empty<RmapPatternPool500RoleSummary>()).OrderBy(value => value.Role).ToArray());
            Digest = digest ?? string.Empty;
        }

        public IReadOnlyList<RmapPatternPool500Entry> Candidates { get; }
        public IReadOnlyList<RmapPatternPool500SourceRecord> Provenance { get; }
        public IReadOnlyList<RmapPatternPool500RoleSummary> RoleSummary { get; }
        public string Digest { get; }

        public bool TryGetCandidate(string candidateId, out RmapPatternPool500Entry candidate)
        {
            candidate = Candidates.FirstOrDefault(value => string.Equals(value.CandidateId, candidateId,
                StringComparison.Ordinal));
            return candidate != null;
        }
    }

    public static class RmapPatternPool500
    {
        public const int RequiredCandidateCount = 500;
        public const string DataVersion = "RMAP11_POOL500_V2_FIX25";
        public const string LegacySource =
            "Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/Visualization/MoonPalaceMicroPatternCandidateLibrary.cs";
        private static readonly Lazy<RmapPatternPool500Snapshot> FinalPool =
            new Lazy<RmapPatternPool500Snapshot>(BuildFinalPoolInternal);

        public static RmapPatternPool500Snapshot BuildFinalPool() => FinalPool.Value;

        public static IReadOnlyList<RmapPatternPool500Replacement> Fix25Replacements => Fix25;

        /// <summary>Returns a non-RMAP07, border-safe entry for the direct small-run consumer.</summary>
        public static RmapPatternPool500Entry SelectPortSafeDetail(int seed, int ordinal)
        {
            RmapPatternPool500Entry[] usable = BuildFinalPool().Candidates.Where(value => !value.IsInitialPool &&
                value.HasOnlyAirBorder && value.PrimaryRole != RmapPatternPrimaryRole.VoidClear &&
                value.Characteristics.SolidCount > 0).OrderBy(value => value.PoolIndex).ToArray();
            if (usable.Length == 0) throw new InvalidOperationException("RMAP11 has no port-safe final-pool detail candidate.");
            return usable[StableIndex(seed, ordinal, usable.Length)];
        }

        public static string ExportFinalPoolCsv(RmapPatternPool500Snapshot pool)
        {
            if (pool == null) throw new ArgumentNullException(nameof(pool));
            var text = new StringBuilder("PoolIndex,CandidateId,BaseCells16,PrimaryRole,SourceId,SourceMaskReference,OriginalReference,Transform,RuleId,SelectionReason,InitialPool,IntentTags,TagConflicts,ContextRequired,SolidCount,AirCount,OneWayCount\n");
            foreach (RmapPatternPool500Entry candidate in pool.Candidates)
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

        public static void ValidateFinalPool(RmapPatternPool500Snapshot pool)
        {
            if (pool == null) throw new ArgumentNullException(nameof(pool));
            if (pool.Candidates.Count != RequiredCandidateCount)
                throw new InvalidOperationException("RMAP11 requires exactly 500 final candidates.");
            if (pool.Candidates.Select(value => value.CandidateId).Distinct(StringComparer.Ordinal).Count() != RequiredCandidateCount ||
                pool.Candidates.Select(value => value.BaseCells16).Distinct(StringComparer.Ordinal).Count() != RequiredCandidateCount)
                throw new InvalidOperationException("RMAP11 candidate IDs and final 16-cell geometries must both be unique.");
            if (pool.Candidates.Any(value => value.BaseCells.Count != RmapPatternCatalog.CellCount) ||
                pool.Candidates.Any(value => value.BaseCells.Any(cell => cell < RmapPatternBaseCell.Air || cell > RmapPatternBaseCell.OneWayPlatform)))
                throw new InvalidOperationException("RMAP11 has an invalid typed base cell.");
            if (pool.Candidates.Count(value => value.PrimaryRole == RmapPatternPrimaryRole.VoidClear) != 1)
                throw new InvalidOperationException("RMAP11 requires exactly one VOID_CLEAR geometry.");
            if (Enum.GetValues(typeof(RmapPatternPrimaryRole)).Cast<RmapPatternPrimaryRole>().Any(role =>
                !pool.Candidates.Any(value => value.PrimaryRole == role)))
                throw new InvalidOperationException("RMAP11 requires an actual classified candidate for all ten roles.");
            if (pool.Candidates.Count(value => value.IsInitialPool) != RmapPatternCatalog.InitialPoolCount - 2)
                throw new InvalidOperationException("RMAP11 FIX25 must retain 46 untouched RMAP07 first-pool candidates.");
            if (pool.Provenance.Count(value => value.SourceId.StartsWith("VIS01_MP_", StringComparison.Ordinal)) <
                MoonPalaceMicroPatternCandidateLibrary.AcceptedCandidateCount)
                throw new InvalidOperationException("RMAP11 must keep a decision record for each RUN06/VIS01 source mask.");
        }

        private static RmapPatternPool500Snapshot BuildFinalPoolInternal()
        {
            RmapPatternCatalogSnapshot firstPool = RmapPatternCatalog.BuildInitialPool();
            MoonPalaceMicroPatternCandidateSet legacy = MoonPalaceMicroPatternCandidateLibrary.Build();
            var entries = new List<RmapPatternPool500Entry>();
            var geometryToEntry = new Dictionary<string, RmapPatternPool500Entry>(StringComparer.Ordinal);
            var provenance = new List<RmapPatternPool500SourceRecord>();

            foreach (RmapPatternCandidate candidate in firstPool.Candidates)
            {
                var entry = new RmapPatternPool500Entry(entries.Count, candidate.CandidateId, candidate.BaseCells,
                    candidate.PrimaryRole, candidate.IntentTags,
                    string.Join(";", candidate.Origins.Select(value => value.SourcePatternId).Distinct(StringComparer.Ordinal)),
                    string.Join(";", candidate.Origins.Select(value => value.SourceMaskReference).Distinct(StringComparer.Ordinal)),
                    string.Join(";", candidate.Origins.Select(value => value.OriginalReference).Distinct(StringComparer.Ordinal)),
                    RmapPatternTransform.R0, string.Empty, "RETAINED_RMAP07_FIRST_POOL", true,
                    candidate.Characteristics, candidate.TagConflicts);
                Add(entry, entries, geometryToEntry);
                foreach (RmapPatternOrigin origin in candidate.Origins)
                    provenance.Add(new RmapPatternPool500SourceRecord(origin.SourcePatternId, origin.SourceMaskReference,
                        origin.OriginalReference, origin.Transform, entry.CandidateId, "RETAINED_RMAP07_FIRST_POOL"));
            }

            var remaining = legacy.Candidates.Select(candidate => CreateLegacyDraft(candidate)).Where(candidate =>
                !geometryToEntry.ContainsKey(candidate.BaseCells16)).ToList();
            var targets = Targets();
            foreach (RmapPatternPrimaryRole role in Enum.GetValues(typeof(RmapPatternPrimaryRole)))
            {
                int needed = targets[role] - entries.Count(value => value.PrimaryRole == role);
                foreach (LegacyDraft draft in remaining.Where(value => value.PrimaryRole == role).OrderBy(value => value.Rank).ToArray())
                {
                    if (needed <= 0) break;
                    Add(ToEntry(entries.Count, draft, "RETAINED_ROLE_TARGET"), entries, geometryToEntry);
                    remaining.Remove(draft);
                    needed--;
                }
            }
            foreach (LegacyDraft draft in remaining.OrderBy(value => value.Rank).ToArray())
            {
                if (entries.Count == RequiredCandidateCount) break;
                Add(ToEntry(entries.Count, draft, "RETAINED_POOL_CAP_FILL"), entries, geometryToEntry);
                remaining.Remove(draft);
            }
            if (entries.Count != RequiredCandidateCount)
                throw new InvalidOperationException("RMAP11 could not reach exactly 500 unique final geometries from the approved sources.");

            ApplyFix25(entries, geometryToEntry, provenance);

            foreach (MoonPalaceMicroPatternCandidate candidate in legacy.Candidates)
            {
                RmapPatternBaseCell[] cells = LegacyCells(candidate);
                string geometry = RmapPatternCatalog.SerializeBaseCells16(cells);
                if (geometryToEntry.TryGetValue(geometry, out RmapPatternPool500Entry entry))
                {
                    string decision = entry.IsInitialPool ? "MERGED_WITH_RETAINED_RMAP07_16_CELL_GEOMETRY" :
                        "RETAINED_FINAL_POOL";
                    provenance.Add(new RmapPatternPool500SourceRecord(candidate.CandidateId, candidate.MaskU16Hex,
                        LegacySource, RmapPatternTransform.R0, entry.CandidateId, decision));
                }
                else
                {
                    provenance.Add(new RmapPatternPool500SourceRecord(candidate.CandidateId, candidate.MaskU16Hex,
                        LegacySource, RmapPatternTransform.R0, string.Empty,
                        "EXCLUDED_AFTER_FIXED_500_CAP_AND_RMAP07_RETENTION"));
                }
            }

            RmapPatternPool500RoleSummary[] summary = Enum.GetValues(typeof(RmapPatternPrimaryRole)).Cast<RmapPatternPrimaryRole>()
                .Select(role =>
                {
                    int actual = entries.Count(value => value.PrimaryRole == role);
                    int target = targets[role];
                    return new RmapPatternPool500RoleSummary(role, target, actual, actual == target
                        ? "INITIAL_A13_TARGET_MET_WITH_RETAINED_ACTUAL_GEOMETRY"
                        : "A13_TARGET_ADJUSTED_TO_ACTUAL_CLASSIFICATION_AFTER_RMAP07_RETENTION_AND_FIXED_500_CAP");
                }).ToArray();
            var result = new RmapPatternPool500Snapshot(entries, provenance, summary,
                Digest(entries.Select(value => value.PoolIndex.ToString(CultureInfo.InvariantCulture) + "|" +
                    value.CandidateId + "|" + value.BaseCells16 + "|" + value.PrimaryRole + "|" + value.SourceId)));
            ValidateFinalPool(result);
            return result;
        }

        private static void ApplyFix25(IList<RmapPatternPool500Entry> entries,
            IDictionary<string, RmapPatternPool500Entry> geometryToEntry,
            ICollection<RmapPatternPool500SourceRecord> provenance)
        {
            if (entries.Count != RequiredCandidateCount)
                throw new InvalidOperationException("RMAP11 FIX25 requires the unmodified V1 500-entry source pool.");

            foreach (RmapPatternPool500Replacement replacement in Fix25)
            {
                if (replacement.PoolIndex < 0 || replacement.PoolIndex >= entries.Count)
                    throw new InvalidOperationException("RMAP11 FIX25 has an out-of-range pool index: " + replacement.PoolIndex);

                RmapPatternPool500Entry old = entries[replacement.PoolIndex];
                if (!string.Equals(old.CandidateId, replacement.OldCandidateId, StringComparison.Ordinal) ||
                    !string.Equals(old.BaseCells16, replacement.OldBaseCells16, StringComparison.Ordinal) ||
                    old.IsInitialPool != replacement.OldInitialPool)
                    throw new InvalidOperationException("RMAP11 FIX25 source mismatch at atlas " + replacement.AtlasNo +
                        ": expected exact index, ID, cells, and initial-pool state.");

                if (!geometryToEntry.Remove(old.BaseCells16) || geometryToEntry.ContainsKey(replacement.NewBaseCells16))
                    throw new InvalidOperationException("RMAP11 FIX25 cannot preserve unique geometry at atlas " + replacement.AtlasNo);

                RmapPatternBaseCell[] cells = RmapPatternCatalog.ParseBaseCells16(replacement.NewBaseCells16).ToArray();
                var updated = new RmapPatternPool500Entry(old.PoolIndex,
                    "RMAP11_" + Hash(replacement.NewBaseCells16).Substring(0, 12).ToUpperInvariant(), cells,
                    RmapPatternCatalog.Classify(cells), old.IntentTags, old.SourceId, old.SourceMaskReference,
                    old.OriginalReference, old.Transform, "RMAP11_FIX25_ATLAS_" + replacement.AtlasNo,
                    "RMAP11_FIX25_USER_AUTHORIZED_REPLACEMENT", false,
                    RmapPatternCatalog.CalculateCharacteristics(cells), old.TagConflicts);
                entries[replacement.PoolIndex] = updated;
                geometryToEntry.Add(updated.BaseCells16, updated);
                provenance.Add(new RmapPatternPool500SourceRecord(old.SourceId, old.SourceMaskReference,
                    old.OriginalReference, old.Transform, updated.CandidateId,
                    "REPLACED_IN_RMAP11_FINAL_CONSUMER_BY_FIX25_ATLAS_" + replacement.AtlasNo));
            }
        }

        private static readonly RmapPatternPool500Replacement[] Fix25 =
        {
            new RmapPatternPool500Replacement(2, 11, "RMAP07_2CFD0D58C833", "AASSAAASASSASSAA", "AASSAAASAAAAAAAA", true),
            new RmapPatternPool500Replacement(7, 51, "RMAP11_C2E089D7CDB3", "ASSSASSSSAASAAAS", "ASSSASSSAAASAAAA", false),
            new RmapPatternPool500Replacement(8, 52, "RMAP11_3053D2016EF5", "ASSSAASSSASSSASS", "ASSSAASSAASSAAAA", false),
            new RmapPatternPool500Replacement(13, 57, "RMAP11_6DE407432111", "AASSAAASASSASSSA", "ASSSAAASAAAAAAAA", false),
            new RmapPatternPool500Replacement(14, 58, "RMAP11_E77354379393", "ASSSAASSASASASAA", "ASSSAASSAAASAAAA", false),
            new RmapPatternPool500Replacement(16, 60, "RMAP11_13BB86F8C0CA", "ASSSASSSSASSAAAS", "ASSSASSSAASSAAAA", false),
            new RmapPatternPool500Replacement(18, 62, "RMAP11_3373D2000288", "AASSSASSSASSAAAS", "AASSAASSAAASAAAA", false),
            new RmapPatternPool500Replacement(19, 3, "RMAP07_16A6ECEF6D87", "SSAASAAAASSAAASS", "SSAASAAAAAAAAAAA", true),
            new RmapPatternPool500Replacement(36, 77, "RMAP11_65268408D2A1", "SSSASAAAASASASAS", "SSSASAAAAAAAAAAA", false),
            new RmapPatternPool500Replacement(38, 79, "RMAP11_74296A9C3630", "SSAASAAASSAASSSA", "SSAASAAASAAAAAAA", false),
            new RmapPatternPool500Replacement(42, 83, "RMAP11_0DC100EF5173", "SSSASAAASAAASSSA", "SSSASAAASAAAAAAA", false),
            new RmapPatternPool500Replacement(44, 85, "RMAP11_E705215B3E59", "SSAASSSASSSASAAA", "SSSASSSASSAAAAAA", false),
            new RmapPatternPool500Replacement(46, 87, "RMAP11_8AE4B6B2B548", "SSAASAASASSSASSS", "SSAASSAAAAAAAAAA", false),
            new RmapPatternPool500Replacement(47, 88, "RMAP11_2C4A8DEF1659", "SSAASAASSSSSSAAA", "SSAASSAASAAAAAAA", false),
            new RmapPatternPool500Replacement(49, 90, "RMAP11_86D57CDB4651", "SSSSSSSASAAASSSS", "SSSSSSSASAAAAAAA", false),
            new RmapPatternPool500Replacement(173, 140, "RMAP11_3103BBE9C8B7", "SSSSSAAASAAASSSS", "SSSSSSAASAAASAAA", false),
            new RmapPatternPool500Replacement(174, 141, "RMAP11_D9D9613912DC", "SAAASSASSSSSSAAA", "SSSSSSAASSAASAAA", false),
            new RmapPatternPool500Replacement(182, 149, "RMAP11_C3D9C850E0F6", "SSSASSSASASSSAAA", "SSSSSSSASAAASAAA", false),
            new RmapPatternPool500Replacement(188, 155, "RMAP11_FAD37FE14D72", "SAAASAAASASASSAA", "SSSSSSSASSSASSAA", false),
            new RmapPatternPool500Replacement(190, 157, "RMAP11_5758E90A31B3", "SAAASSSASASASAAS", "SSSSSSSASSSASAAA", false),
            new RmapPatternPool500Replacement(197, 161, "RMAP11_249C06C89B32", "ASASASASASSSAAAS", "SSSSASSSASSSAAAS", false),
            new RmapPatternPool500Replacement(205, 169, "RMAP11_81FC8AFE293D", "SAASSASSSASSAAAS", "SSSSAASSAASSAAAS", false),
            new RmapPatternPool500Replacement(207, 171, "RMAP11_107C4AFAC963", "SAASSAASSSASAASS", "SSSSAASSAAASAAAS", false),
            new RmapPatternPool500Replacement(219, 183, "RMAP11_10BDEE2F1D49", "AAASASSSASASAAAS", "SSSSASSSAAASAAAS", false),
            new RmapPatternPool500Replacement(221, 185, "RMAP11_F58607A347F7", "ASSSASASSAASAASS", "SSSSASSSAASSAASS", false),
        };

        private static void Add(RmapPatternPool500Entry entry, ICollection<RmapPatternPool500Entry> entries,
            IDictionary<string, RmapPatternPool500Entry> geometryToEntry)
        {
            string geometry = entry.BaseCells16;
            if (geometryToEntry.ContainsKey(geometry))
                throw new InvalidOperationException("RMAP11 attempted to add a duplicate final 16-cell geometry.");
            entries.Add(entry);
            geometryToEntry.Add(geometry, entry);
        }

        private static RmapPatternPool500Entry ToEntry(int index, LegacyDraft draft, string reason) =>
            new RmapPatternPool500Entry(index, "RMAP11_" + Hash(draft.BaseCells16).Substring(0, 12).ToUpperInvariant(),
                draft.Cells, draft.PrimaryRole, RmapPatternIntentTag.OptionalOnly | RmapPatternIntentTag.QuietFill,
                draft.SourceId, draft.Mask, LegacySource, RmapPatternTransform.R0, string.Empty, reason, false,
                draft.Characteristics, Array.Empty<string>());

        private static LegacyDraft CreateLegacyDraft(MoonPalaceMicroPatternCandidate candidate)
        {
            RmapPatternBaseCell[] cells = LegacyCells(candidate);
            return new LegacyDraft(candidate.CandidateId, candidate.MaskU16Hex, candidate.Rank, cells,
                RmapPatternCatalog.Classify(cells), RmapPatternCatalog.CalculateCharacteristics(cells));
        }

        private static RmapPatternBaseCell[] LegacyCells(MoonPalaceMicroPatternCandidate candidate) =>
            Enumerable.Range(0, RmapPatternCatalog.CellCount).Select(index => (candidate.Mask & (1 << index)) != 0
                ? RmapPatternBaseCell.Solid : RmapPatternBaseCell.Air).ToArray();

        private static Dictionary<RmapPatternPrimaryRole, int> Targets() =>
            new Dictionary<RmapPatternPrimaryRole, int>
            {
                [RmapPatternPrimaryRole.SlopeRiseRight] = 55,
                [RmapPatternPrimaryRole.SlopeRiseLeft] = 55,
                [RmapPatternPrimaryRole.CeilingFlat] = 40,
                [RmapPatternPrimaryRole.CeilingRough] = 50,
                [RmapPatternPrimaryRole.WallLeft] = 35,
                [RmapPatternPrimaryRole.WallRight] = 35,
                [RmapPatternPrimaryRole.VoidClear] = 1,
                [RmapPatternPrimaryRole.SparseAirPlatform] = 95,
                [RmapPatternPrimaryRole.StandableLedge] = 100,
                [RmapPatternPrimaryRole.VerticalPassage] = 34,
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

        private sealed class LegacyDraft
        {
            public LegacyDraft(string sourceId, string mask, int rank, RmapPatternBaseCell[] cells,
                RmapPatternPrimaryRole primaryRole, RmapPatternAutomaticCharacteristics characteristics)
            {
                SourceId = sourceId; Mask = mask; Rank = rank; Cells = cells; PrimaryRole = primaryRole;
                Characteristics = characteristics;
            }
            public string SourceId { get; }
            public string Mask { get; }
            public int Rank { get; }
            public RmapPatternBaseCell[] Cells { get; }
            public string BaseCells16 => RmapPatternCatalog.SerializeBaseCells16(Cells);
            public RmapPatternPrimaryRole PrimaryRole { get; }
            public RmapPatternAutomaticCharacteristics Characteristics { get; }
        }
    }
}
