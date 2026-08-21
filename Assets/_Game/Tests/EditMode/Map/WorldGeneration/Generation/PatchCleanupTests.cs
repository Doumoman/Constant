using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Generation;
using UnityEngine;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    public sealed class PatchCleanupTests
    {
        private const ulong ViableWorldSeed = 0x0123456789ABCDF9UL;
        private const int ViableAttempt = 24;
        private IntrusionPlacementResult intrusion;
        private IReadOnlyList<BiomeTypeDefinition> biomes;
        private IReadOnlyList<BiomePatchRuleDefinition> rules;
        private string expectedSignature;
        private string sourceSignature;
        private PatchCleanup reused;

        public static IEnumerable<TestCaseData> DeterminismCases
        {
            get
            {
                for (var index = 0; index < 120; index++)
                    yield return new TestCaseData(index).SetName(
                        "Clean_ViableDeterministicConservation_" +
                        index.ToString("D3", CultureInfo.InvariantCulture));
            }
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var fixtureMethod = typeof(IntrusionPlacerTests).GetMethod(
                "BuildFixture", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(fixtureMethod, Is.Not.Null);
            var fixture = fixtureMethod.Invoke(null, new object[] { ViableWorldSeed, ViableAttempt });
            var fixtureType = fixture.GetType();
            var growth = (MultiSeedBiomeGrowthResult)Get(fixtureType, fixture, "Growth");
            var profile = (GenerationProfileDefinition)Get(fixtureType, fixture, "Profile");
            var rng = (DeterministicRngStream)Get(fixtureType, fixture, "ContinuedRng");
            var definitions = Get(fixtureType, fixture, "Definitions");
            var definitionsType = definitions.GetType();
            biomes = ((IEnumerable<BiomeTypeDefinition>)Get(definitionsType, definitions, "Biomes")).ToArray();
            rules = ((IEnumerable<BiomePatchRuleDefinition>)Get(definitionsType, definitions, "AllRules")).ToArray();
            var profiles = (IEnumerable<BiomeBoundaryProfileDefinition>)Get(definitionsType, definitions, "Profiles");
            var pairs = (IEnumerable<BiomeBoundaryPairRuleDefinition>)Get(definitionsType, definitions, "Pairs");

            intrusion = new IntrusionPlacer().Place(
                growth, profile, biomes, rules, profiles, pairs, rng);
            Assert.That(intrusion.Status, Is.EqualTo(IntrusionPlacementStatus.Completed));
            Assert.That(intrusion.Diagnostics.RngDrawCountAfter, Is.EqualTo(1912UL));
            sourceSignature = SnapshotSignature(intrusion.Publication.Snapshot);
            reused = new PatchCleanup();
            var baseline = reused.Clean(intrusion, biomes, rules);
            Assert.That(baseline.Status, Is.EqualTo(PatchCleanupStatus.Completed),
                baseline.Errors.Count == 0 ? string.Empty : baseline.Errors[0].Message);
            expectedSignature = ResultSignature(baseline);
        }

        [TestCaseSource(nameof(DeterminismCases))]
        public void Clean_ViableDeterministicConservation(int caseId)
        {
            var cleanup = (caseId & 1) == 0 ? new PatchCleanup() : reused;
            var orderedBiomes = (caseId & 2) == 0 ? biomes : biomes.Reverse();
            var orderedRules = (caseId & 4) == 0 ? rules : rules.Reverse();
            var result = cleanup.Clean(intrusion, orderedBiomes, orderedRules);

            Assert.That(result.Status, Is.EqualTo(PatchCleanupStatus.Completed));
            Assert.That(result.Errors, Is.Empty);
            Assert.That(ResultSignature(result), Is.EqualTo(expectedSignature));
            Assert.That(SnapshotSignature(intrusion.Publication.Snapshot), Is.EqualTo(sourceSignature));
            AssertConservation(result);
        }

        [Test]
        public void FrozenEnumsAndScoreUseExactLexicographicOrder()
        {
            CollectionAssert.AreEqual(
                new[] { "Completed", "InvalidInput", "RetryRequired" },
                Enum.GetNames(typeof(PatchCleanupStatus)));
            CollectionAssert.AreEqual(new[]
            {
                "MissingIntrusionResult", "IntrusionNotCompleted", "MissingPublication",
                "MissingDiagnostics", "MissingBiomeTypes", "MissingPatchRules",
                "InvalidSourceSnapshot", "InvalidDefinition", "NoSafeCleanupMove",
                "CleanupStepLimitExceeded", "InternalInvariantViolation"
            }, Enum.GetNames(typeof(PatchCleanupErrorCode)));
            CollectionAssert.AreEqual(
                new[] { "CheckerboardCollapse", "NeckCollapse", "NeckWiden" },
                Enum.GetNames(typeof(PatchCleanupMoveKind)));

            Assert.That(new PatchCleanupScore(0, 9, 99), Is.LessThan(new PatchCleanupScore(1, 0, 0)));
            Assert.That(new PatchCleanupScore(0, 0, 99), Is.LessThan(new PatchCleanupScore(0, 1, 0)));
            Assert.That(new PatchCleanupScore(0, 0, 1), Is.LessThan(new PatchCleanupScore(0, 0, 2)));
        }

        [Test]
        public void Clean_NullInputsAccumulateSortedStableErrors()
        {
            var result = new PatchCleanup().Clean(null, null, null);
            Assert.That(result.Status, Is.EqualTo(PatchCleanupStatus.InvalidInput));
            Assert.That(result.Publication, Is.Null);
            Assert.That(result.Diagnostics, Is.Null);
            Assert.That(result.Errors.Select(value => value.Code), Is.Ordered);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(PatchCleanupErrorCode.MissingIntrusionResult));
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(PatchCleanupErrorCode.MissingBiomeTypes));
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(PatchCleanupErrorCode.MissingPatchRules));
        }

        [Test]
        public void Clean_ViableIntegrationPublishesExactProtectionAndCleanupEvidence()
        {
            var result = new PatchCleanup().Clean(intrusion, biomes, rules);
            Assert.That(result.Status, Is.EqualTo(PatchCleanupStatus.Completed));
            AssertConservation(result);
            Assert.That(result.Diagnostics.WorldSeed, Is.EqualTo(ViableWorldSeed));
            Assert.That(result.Diagnostics.SourceRngDrawCount, Is.EqualTo(1912UL));
            Assert.That(result.Diagnostics.FinalRngDrawCount, Is.EqualTo(1912UL));
            Assert.That(result.Diagnostics.RngMethodCallCount, Is.Zero);
            Assert.That(result.Diagnostics.RngRawDrawCount, Is.Zero);
            Assert.That(result.Diagnostics.FinalActionableCheckerboardCount, Is.Zero);
            Assert.That(result.Diagnostics.FinalActionableNeckCount, Is.Zero);
            Assert.That(result.Diagnostics.StepLimit, Is.EqualTo(676));
            Assert.That(result.Diagnostics.MoveCount, Is.LessThanOrEqualTo(676));
            Assert.That(result.Diagnostics.Moves.All(value => value.ScoreAfter < value.ScoreBefore), Is.True);
            Assert.That(result.Diagnostics.Moves.Select(value => value.Sequence),
                Is.EqualTo(Enumerable.Range(0, result.Diagnostics.MoveCount)));
            Assert.That(result.Diagnostics.OverlapViolationCount, Is.Zero);
            Assert.That(result.Diagnostics.OrphanOwnershipCount, Is.Zero);
            Assert.That(result.Diagnostics.DisconnectedPatchCount, Is.Zero);
            Assert.That(result.Diagnostics.SiteMisownershipCount, Is.Zero);
            Assert.That(result.Diagnostics.ProtectedOwnershipChangeCount, Is.Zero);
            Assert.That(result.Diagnostics.SourceMutationCount, Is.Zero);
            AssertReadOnly(result.Publication.Moves);
            AssertReadOnly(result.Diagnostics.Moves);
            Debug.Log(string.Format(
                CultureInfo.InvariantCulture,
                "MAP04_07_VIABLE initial={0}/{1}/{2};final={3}/{4}/{5};protected={6};moves={7};patches={8};assigned={9};unassigned={10};rng={11}->{12}",
                result.Diagnostics.InitialScore.CheckerboardCount,
                result.Diagnostics.InitialScore.NeckCount,
                result.Diagnostics.InitialScore.CrossPatchUndirectedEdgeCount,
                result.Diagnostics.FinalScore.CheckerboardCount,
                result.Diagnostics.FinalScore.NeckCount,
                result.Diagnostics.FinalScore.CrossPatchUndirectedEdgeCount,
                result.Diagnostics.ProtectedAnomalyCount,
                result.Diagnostics.MoveCount,
                result.Publication.TotalPatchCount,
                result.Publication.AssignedSectorCount,
                result.Publication.UnassignedSectorCount,
                result.Diagnostics.SourceRngDrawCount,
                result.Diagnostics.FinalRngDrawCount));
        }

        [Test]
        public void Clean_SyntheticExactCheckerboardCollapsesBackToConnectedSource()
        {
            var synthetic = CreateCheckerboardResult(out var center);
            var result = new PatchCleanup().Clean(synthetic, biomes, rules);
            Assert.That(result.Status, Is.EqualTo(PatchCleanupStatus.Completed));
            Assert.That(result.Diagnostics.InitialActionableCheckerboardCount, Is.EqualTo(1));
            Assert.That(result.Diagnostics.FinalActionableCheckerboardCount, Is.Zero);
            Assert.That(result.Diagnostics.MoveCount, Is.EqualTo(1));
            Assert.That(result.Diagnostics.Moves[0].Kind, Is.EqualTo(PatchCleanupMoveKind.CheckerboardCollapse));
            Assert.That(result.Diagnostics.Moves[0].CenterSectorIndex, Is.EqualTo(center));
            Assert.That(result.Diagnostics.Moves[0].MovedSectorIndex, Is.EqualTo(center));
            Assert.That(SnapshotSignature(result.Publication.Snapshot), Is.EqualTo(sourceSignature));
            Assert.That(SnapshotSignature(synthetic.Publication.Snapshot), Is.Not.EqualTo(sourceSignature));
        }

        [TestCase("en-US")]
        [TestCase("tr-TR")]
        public void Clean_CultureFreshAndReusedInstancesAreLogicalByteEquivalent(string cultureName)
        {
            var previous = CultureInfo.CurrentCulture;
            var previousUi = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
                var fresh = new PatchCleanup().Clean(intrusion, biomes.Reverse(), rules.Reverse());
                var reusedResult = reused.Clean(intrusion, biomes, rules);
                Assert.That(ResultSignature(fresh), Is.EqualTo(expectedSignature));
                Assert.That(ResultSignature(reusedResult), Is.EqualTo(expectedSignature));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
                CultureInfo.CurrentUICulture = previousUi;
            }
        }

        [Test]
        public void RuntimeCleanupSurfaceHasNoRngClockFileUnityObjectOrMutableStaticDependency()
        {
            var types = new[]
            {
                typeof(PatchCleanupError), typeof(PatchCleanupScore),
                typeof(PatchCleanupMoveRecord), typeof(PatchCleanupDiagnostics),
                typeof(PatchCleanupPublication), typeof(PatchCleanupResult), typeof(PatchCleanup)
            };
            foreach (var type in types)
            {
                if (type.IsClass) Assert.That(type.IsSealed, Is.True, type.FullName);
                Assert.That(type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(field => !field.IsLiteral && !field.IsInitOnly), Is.Empty, type.FullName);
                var surface = type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Select(member => member.ToString()).ToArray();
                Assert.That(surface.Any(value => value.Contains("DeterministicRng") ||
                    value.Contains("System.Random") || value.Contains("UnityEngine.Random") ||
                    value.Contains("UnityEditor") || value.Contains("UnityEngine.Object") ||
                    value.Contains("System.IO") || value.Contains("DateTime")), Is.False, type.FullName);
            }
        }

        [Test]
        [Timeout(600000)]
        public void Clean_OriginalFortyFourProducerConformantSnapshotsAvoidInvalid()
        {
            var previous = CultureInfo.CurrentCulture;
            var previousUi = CultureInfo.CurrentUICulture;
            Map04ExitTests.CleanupContractAudit baseline;
            Map04ExitTests.CleanupContractAudit alternate;
            try
            {
                var harness = new Map04ExitTests();
                harness.OneTimeSetUp();
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
                baseline = harness.AuditCleanupContract(false, false);
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
                alternate = harness.AuditCleanupContract(true, true);
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
                CultureInfo.CurrentUICulture = previousUi;
            }

            var matrixFailures = baseline.LegacyRows
                .Where(value => !value.Contains("|matrix=NONE|"))
                .ToArray();
            Debug.Log(string.Format(CultureInfo.InvariantCulture,
                "MAP04_11_CLEANUP_AUDIT_PRECHECK legacy={0} completed={1} handoff={2} invalid={3} " +
                "cleanupRejected={4} sourceMutation={5} matrixFailures={6} firstMatrixFailure={7}",
                baseline.LegacyRows.Count, baseline.Completed, baseline.Handoff, baseline.Invalid,
                baseline.CleanupRejectedWitnesses, baseline.SourceMutationCount,
                matrixFailures.Length, matrixFailures.FirstOrDefault() ?? "_"));
            Assert.That(baseline.LegacyRows, Has.Count.EqualTo(44));
            Assert.That(baseline.LegacyRows.All(value => value.Contains("|matrix=NONE|")), Is.True);
            Assert.That(baseline.LegacyRows.All(value =>
                value.Contains("intrusion=Completed") &&
                value.Contains("|intrusionPublication=1|cleanupPublication=0")), Is.True);
            Assert.That(baseline.Completed + baseline.Handoff, Is.EqualTo(1000));
            Assert.That(baseline.Completed, Is.GreaterThan(0));
            Assert.That(baseline.Handoff, Is.GreaterThan(0));
            Assert.That(baseline.Invalid, Is.Zero);
            Assert.That(baseline.CleanupRejectedWitnesses, Is.Zero);
            Assert.That(baseline.SourceMutationCount, Is.Zero);
            Assert.That(alternate.LegacyRows, Is.EqualTo(baseline.LegacyRows));
            Assert.That(alternate.LegacyLedgerDigest, Is.EqualTo(baseline.LegacyLedgerDigest));
            Assert.That(alternate.Completed, Is.EqualTo(baseline.Completed));
            Assert.That(alternate.Handoff, Is.EqualTo(baseline.Handoff));
            Assert.That(alternate.Invalid, Is.Zero);
            Assert.That(alternate.CleanupRejectedWitnesses, Is.Zero);
            Assert.That(alternate.SourceMutationCount, Is.Zero);

            var malformedBiomes = biomes.Skip(1).ToArray();
            var malformedFresh = new PatchCleanup().Clean(intrusion, malformedBiomes, rules);
            var malformedReused = reused.Clean(
                intrusion, malformedBiomes.Reverse(), rules.Reverse());
            Assert.That(malformedFresh.Status, Is.EqualTo(PatchCleanupStatus.InvalidInput));
            Assert.That(malformedFresh.Errors.Select(value => value.Code),
                Does.Contain(PatchCleanupErrorCode.InvalidSourceSnapshot));
            Assert.That(malformedFresh.Publication, Is.Null);
            Assert.That(ResultSignature(malformedReused), Is.EqualTo(ResultSignature(malformedFresh)));
            Debug.Log(string.Format(CultureInfo.InvariantCulture,
                "MAP04_11_CLEANUP_AUDIT legacy={0} completed={1} handoff={2} invalid={3} " +
                "cleanupRejected={4} sourceMutation={5} digest={6}",
                baseline.LegacyRows.Count, baseline.Completed, baseline.Handoff, baseline.Invalid,
                baseline.CleanupRejectedWitnesses, baseline.SourceMutationCount,
                baseline.LegacyLedgerDigest));
        }

        private static object Get(Type type, object instance, string property)
        {
            var value = type.GetProperty(property, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(value, Is.Not.Null, property);
            return value.GetValue(instance);
        }

        private IntrusionPlacementResult CreateCheckerboardResult(out int center)
        {
            var source = intrusion.Publication.Snapshot;
            var protectedMask = new bool[169];
            foreach (var row in intrusion.Publication.SourceSiteSnapshot.Sectors)
                if (row.IsReserved) protectedMask[row.Index] = true;
            foreach (var patch in source.Patches)
            {
                foreach (var seed in patch.Seeds) protectedMask[seed.SectorIndex] = true;
                if (patch.Role != BiomePatchRole.Intrusion) continue;
                foreach (var index in patch.SectorIndices)
                {
                    protectedMask[index] = true;
                    foreach (var neighbor in Neighbors(index)) protectedMask[neighbor] = true;
                }
            }
            foreach (var binding in source.SiteBindings)
                foreach (var index in binding.OccupiedSectorIndices) protectedMask[index] = true;

            var ruleLookup = rules.ToDictionary(value => value.PatchRuleId, StringComparer.Ordinal);
            BiomePatch target = null;
            center = -1;
            for (var y = 1; y < 12 && target == null; y++)
            for (var x = 1; x < 12 && target == null; x++)
            {
                var index = (y * 13) + x;
                var patch = source.Patches.SingleOrDefault(value => value.ContainsSector(index));
                if (patch == null || patch.Role == BiomePatchRole.Intrusion ||
                    patch.SectorCount <= ruleLookup[patch.PatchRuleId].MinSectorCount) continue;
                var plus = new[] { index, index - 1, index + 1, index - 13, index + 13 };
                if (plus.Any(value => protectedMask[value]) || plus.Any(value => !patch.ContainsSector(value))) continue;
                target = patch;
                center = index;
            }
            Assert.That(target, Is.Not.Null, "The viable P03 fixture must expose one unprotected same-patch plus.");
            var selectedCenter = center;

            var donor = source.Patches.First(value =>
                value.Role != BiomePatchRole.Intrusion && value.Id != target.Id &&
                value.SectorCount < ruleLookup[value.PatchRuleId].MaxSectorCount &&
                value.SectorCount < 59);
            var targetSectors = target.SectorIndices.Where(value => value != selectedCenter).ToArray();
            var donorSectors = donor.SectorIndices.Concat(new[] { selectedCenter }).ToArray();
            var patches = source.Patches.Select(value =>
                value.Id == target.Id
                    ? new BiomePatch(value.Id, value.BiomeId, value.PatchRuleId, value.Role, value.Seeds, targetSectors)
                    : value.Id == donor.Id
                        ? new BiomePatch(value.Id, value.BiomeId, value.PatchRuleId, value.Role, value.Seeds, donorSectors)
                        : value).ToArray();
            var rows = source.Sectors.Select(value => value.SectorIndex == selectedCenter
                ? new BiomeSectorOwnership(
                    selectedCenter, value.Sector, donor.BiomeId, string.Empty, donor.Id)
                : value).ToArray();
            var snapshot = new BiomePatchSnapshot(source.Seed, patches, rows, source.SiteBindings);

            var publicationConstructor = typeof(IntrusionPlacementPublication).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[]
                {
                    typeof(MultiSeedBiomeGrowthPublication), typeof(BiomePatchSnapshot),
                    typeof(IEnumerable<IntrusionPlacementRecord>)
                }, null);
            Assert.That(publicationConstructor, Is.Not.Null);
            var publication = (IntrusionPlacementPublication)publicationConstructor.Invoke(new object[]
            {
                intrusion.Publication.SourceGrowth, snapshot, intrusion.Publication.Intrusions
            });
            var completed = typeof(IntrusionPlacementResult).GetMethod(
                "Completed", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(completed, Is.Not.Null);
            return (IntrusionPlacementResult)completed.Invoke(
                null, new object[] { publication, intrusion.Diagnostics });
        }

        private static IEnumerable<int> Neighbors(int index)
        {
            var x = index % 13;
            var y = index / 13;
            if (x > 0) yield return index - 1;
            if (x < 12) yield return index + 1;
            if (y < 12) yield return index + 13;
            if (y > 0) yield return index - 13;
        }

        private static void AssertConservation(PatchCleanupResult result)
        {
            Assert.That(result.Publication.TotalPatchCount, Is.EqualTo(17));
            Assert.That(result.Publication.CorePatchCount, Is.EqualTo(4));
            Assert.That(result.Publication.IntrusionPatchCount, Is.EqualTo(3));
            Assert.That(result.Publication.AssignedSectorCount, Is.EqualTo(165));
            Assert.That(result.Publication.UnassignedSectorCount, Is.EqualTo(4));
            Assert.That(result.Diagnostics.InitialPatchCount, Is.EqualTo(17));
            Assert.That(result.Diagnostics.FinalPatchCount, Is.EqualTo(17));
            Assert.That(result.Diagnostics.InitialAssignedSectorCount, Is.EqualTo(165));
            Assert.That(result.Diagnostics.FinalAssignedSectorCount, Is.EqualTo(165));
            Assert.That(result.Diagnostics.InitialUnassignedSectorCount, Is.EqualTo(4));
            Assert.That(result.Diagnostics.FinalUnassignedSectorCount, Is.EqualTo(4));
            Assert.That(result.Publication.Snapshot.Patches.All(patch =>
                patch.Role == BiomePatchRole.Intrusion ? patch.SectorCount == 1 : patch.SectorCount >= 2), Is.True);
            Assert.That(result.Publication.Snapshot.Sectors.All(value =>
                value.SecondaryBiomeId.Length == 0), Is.True);
            Assert.That(result.Publication.Snapshot.SiteBindings.Zip(
                result.Publication.SourceIntrusion.Publication.Snapshot.SiteBindings,
                (left, right) => ReferenceEquals(left, right)).All(value => value), Is.True);
        }

        private static string SnapshotSignature(BiomePatchSnapshot snapshot)
        {
            return string.Join("|", snapshot.Patches.Select(patch =>
                patch.Id.Value + ":" + patch.BiomeId + ":" + patch.PatchRuleId + ":" +
                patch.Role + ":" + string.Join(",", patch.SectorIndices))) + "#" +
                string.Join("|", snapshot.Sectors.Select(value =>
                    value.IsAssigned
                        ? value.SectorIndex + ":" + value.PrimaryBiomeId + ":" + value.PatchId.Value.Value
                        : value.SectorIndex + ":_"));
        }

        private static string ResultSignature(PatchCleanupResult result)
        {
            if (!result.Succeeded)
                return result.Status + ":" + string.Join(",", result.Errors.Select(value => value.Code));
            return SnapshotSignature(result.Publication.Snapshot) + "#" +
                string.Join("|", result.Publication.Moves.Select(value =>
                    value.Sequence + ":" + value.Kind + ":" + value.CenterSectorIndex + ":" +
                    value.MovedSectorIndex + ":" + value.DonorPatchId.Value + ":" +
                    value.TargetPatchId.Value + ":" +
                    value.ScoreBefore.CheckerboardCount + "," + value.ScoreBefore.NeckCount + "," +
                    value.ScoreBefore.CrossPatchUndirectedEdgeCount + ">" +
                    value.ScoreAfter.CheckerboardCount + "," + value.ScoreAfter.NeckCount + "," +
                    value.ScoreAfter.CrossPatchUndirectedEdgeCount));
        }

        private static void AssertReadOnly<T>(IReadOnlyList<T> values)
        {
            Assert.That(values, Is.InstanceOf<IList>());
            Assert.Throws<NotSupportedException>(() => ((IList)values).Add(default(T)));
        }
    }
}
