#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.MoonPalace.RunGeneration;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.Rmap11
{
    [Category("RMAP11")]
    public sealed class RmapPatternPool500Tests
    {
        [Test]
        public void A13_A14_FinalPoolHas500UniqueActualGeometriesAndCompleteProvenance()
        {
            RmapPatternPool500Snapshot pool = RmapPatternPool500.BuildFinalPool();

            RmapPatternPool500.ValidateFinalPool(pool);
            Assert.That(pool.Candidates.Count, Is.EqualTo(500));
            Assert.That(pool.Candidates.Select(value => value.CandidateId).Distinct().Count(), Is.EqualTo(500));
            Assert.That(pool.Candidates.Select(value => value.BaseCells16).Distinct().Count(), Is.EqualTo(500));
            Assert.That(pool.Candidates.Count(value => value.PrimaryRole == RmapPatternPrimaryRole.VoidClear), Is.EqualTo(1));
            Assert.That(pool.Candidates.Count(value => value.IsInitialPool), Is.EqualTo(46));
            Assert.That(pool.Provenance.Count(value => value.SourceId.StartsWith("VIS01_MP_", StringComparison.Ordinal)),
                Is.GreaterThanOrEqualTo(500));
            Assert.That(pool.RoleSummary.Count, Is.EqualTo(10));
            Assert.That(pool.RoleSummary.All(value => pool.Candidates.Count(candidate => candidate.PrimaryRole == value.Role) == value.Actual), Is.True);
        }

        [Test]
        public void Fix25_ExactSourceMappingPreserves475AndMatchesTheIndependentPreview()
        {
            RmapPatternPool500Snapshot pool = RmapPatternPool500.BuildFinalPool();
            RmapPatternPool500Replacement[] replacements = RmapPatternPool500.Fix25Replacements.ToArray();
            string root = Directory.GetParent(Application.dataPath).FullName;
            string oldCsv = Path.Combine(root, "MapDesign/MCP/GENERATED/RMAP11/rmap11_final_pool.csv");
            string preview = Path.Combine(root, "MapDesign/MCP/INPUTS/R11FIX/preview500.csv");
            Assert.That(File.Exists(oldCsv), Is.True, "The RMAP11 V1 source snapshot is required for FIX25 verification.");
            Assert.That(File.Exists(preview), Is.True, "preview500 is an independent expected-array check, never a runtime catalog.");

            string[][] oldRows = File.ReadAllLines(oldCsv).Skip(1).Select(ParseQuotedCsv).ToArray();
            Assert.That(oldRows.Length, Is.EqualTo(500));
            foreach (RmapPatternPool500Replacement replacement in replacements)
            {
                RmapPatternPool500Entry actual = pool.Candidates[replacement.PoolIndex];
                string[] original = oldRows.Single(row => int.Parse(row[0]) == replacement.PoolIndex);
                Assert.That(original[1], Is.EqualTo(replacement.OldCandidateId));
                Assert.That(original[2], Is.EqualTo(replacement.OldBaseCells16));
                Assert.That(actual.BaseCells16, Is.EqualTo(replacement.NewBaseCells16));
                Assert.That(actual.CandidateId, Is.EqualTo("RMAP11_" + Sha256(replacement.NewBaseCells16).Substring(0, 12).ToUpperInvariant()));
                Assert.That(actual.CandidateId, Is.Not.EqualTo(replacement.OldCandidateId));
                Assert.That(actual.IsInitialPool, Is.False);
            }

            foreach (RmapPatternPool500Entry candidate in pool.Candidates.Where(candidate =>
                         !replacements.Any(replacement => replacement.PoolIndex == candidate.PoolIndex)))
            {
                string[] original = oldRows.Single(row => int.Parse(row[0]) == candidate.PoolIndex);
                Assert.That(candidate.CandidateId, Is.EqualTo(original[1]), "unchanged ID at pool " + candidate.PoolIndex);
                Assert.That(candidate.BaseCells16, Is.EqualTo(original[2]), "unchanged geometry at pool " + candidate.PoolIndex);
            }

            string[][] previewRows = File.ReadAllLines(preview).Skip(1).Select(line => line.Split(',')).ToArray();
            Assert.That(previewRows.Length, Is.EqualTo(500));
            foreach (string[] row in previewRows)
                Assert.That(pool.Candidates[int.Parse(row[1])].BaseCells16, Is.EqualTo(row[4]),
                    "preview geometry at atlas " + row[0] + " / pool " + row[1]);
        }

        private static string[] ParseQuotedCsv(string line) => Regex.Matches(line, "\\\"([^\\\"]*)\\\"")
            .Cast<Match>().Select(match => match.Groups[1].Value).ToArray();

        private static string Sha256(string value)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(value)).Select(valueByte =>
                    valueByte.ToString("x2")));
        }

        [Test]
        public void A14_FinalPoolCsvAndPortSafeDetailAreDeterministicAndTyped()
        {
            RmapPatternPool500Snapshot pool = RmapPatternPool500.BuildFinalPool();
            RmapPatternPool500Entry first = RmapPatternPool500.SelectPortSafeDetail(1107, 4);
            RmapPatternPool500Entry second = RmapPatternPool500.SelectPortSafeDetail(1107, 4);
            string csv = RmapPatternPool500.ExportFinalPoolCsv(pool);

            Assert.That(first.CandidateId, Is.EqualTo(second.CandidateId));
            Assert.That(first.IsInitialPool, Is.False);
            Assert.That(first.HasOnlyAirBorder, Is.True);
            Assert.That(first.Characteristics.SolidCount, Is.GreaterThan(0));
            Assert.That(csv.Split('\n').Length - 2, Is.EqualTo(500));
            Assert.That(pool.Candidates.All(value => value.BaseCells.Count == 16), Is.True);
        }

        [Test]
        public void ConsumerUsesFinalPoolAndAnEntryOutsideTheHistoricalFirst48()
        {
            RmapPatternPool500Snapshot pool = RmapPatternPool500.BuildFinalPool();
            RmapSmallRunPlan plan = RmapSmallRunHarness.Generate(new RmapSmallRunRequest(1107, 36, 24,
                RmapSmallRunRecipe.PortGalleryV1));

            Assert.That(plan.Success, Is.True, plan.FailureSummary);
            Assert.That(plan.PoolVersion, Is.EqualTo(RmapPatternPool500.DataVersion));
            Assert.That(plan.Chunks.SelectMany(chunk => chunk.Selections).All(selection =>
                pool.TryGetCandidate(selection.CandidateId, out RmapPatternPool500Entry _)), Is.True);
            Assert.That(plan.Chunks.SelectMany(chunk => chunk.Selections).Any(selection =>
                pool.TryGetCandidate(selection.CandidateId, out RmapPatternPool500Entry entry) && !entry.IsInitialPool), Is.True);
        }
    }
}
#endif
