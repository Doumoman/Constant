using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using UnityEngine;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    public sealed class BiomePatchValidatorTests
    {
        private const ulong ViableWorldSeed = 0x0123456789ABCDF9UL;
        private BiomePatchExportResult export;
        private BiomeTypeDefinition[] biomes;
        private BiomePatchRuleDefinition[] rules;
        private BiomeBoundaryProfileDefinition[] profiles;
        private BiomeBoundaryPairRuleDefinition[] pairs;
        private BiomePatchValidator reused;
        private string baselineSignature;

        public static IEnumerable<TestCaseData> DeterminismCases
        {
            get
            {
                for (var index = 0; index < 150; index++)
                    yield return new TestCaseData(index).SetName(
                        "Validate_ViableDeterministicFifteenRulePublication_" +
                        index.ToString("D3", CultureInfo.InvariantCulture));
            }
        }

        public static IEnumerable<TestCaseData> RuleCases
        {
            get
            {
                foreach (BiomePatchValidationRule rule in Enum.GetValues(typeof(BiomePatchValidationRule)))
                    yield return new TestCaseData(rule).SetName("Rule_IndependentFailure_" + rule);
            }
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var cleanup = (PatchCleanupResult)InvokePrivateStatic(
                typeof(BiomePatchExporterTests), "BuildCleanupResult", Array.Empty<object>());
            var world = (GeneratedWorldData)InvokePrivateStatic(
                typeof(BiomePatchExporterTests), "CreateSourceWorld",
                new object[] { ViableWorldSeed, false, false });
            export = new BiomePatchExporter().Export(cleanup, world);
            Assert.That(export.Status, Is.EqualTo(BiomePatchExportStatus.Completed));

            var fixture = InvokePrivateStatic(
                typeof(IntrusionPlacerTests), "BuildFixture", new object[] { ViableWorldSeed, 24 });
            var definitions = Get(fixture, "Definitions");
            biomes = ((IEnumerable<BiomeTypeDefinition>)Get(definitions, "Biomes")).ToArray();
            rules = ((IEnumerable<BiomePatchRuleDefinition>)Get(definitions, "AllRules")).ToArray();
            profiles = ((IEnumerable<BiomeBoundaryProfileDefinition>)Get(definitions, "Profiles")).ToArray();
            pairs = ((IEnumerable<BiomeBoundaryPairRuleDefinition>)Get(definitions, "Pairs")).ToArray();
            reused = new BiomePatchValidator();
            var baseline = reused.Validate(export, biomes, rules, profiles, pairs);
            Assert.That(baseline.Status, Is.EqualTo(BiomePatchValidationStatus.Completed),
                baseline.Errors.Count == 0 ? ViolationSignature(baseline) : ErrorSignature(baseline));
            baselineSignature = ResultSignature(baseline);
        }

        [TestCaseSource(nameof(DeterminismCases))]
        public void Validate_ViableDeterministicFifteenRulePublication(int caseId)
        {
            var validator = (caseId & 1) == 0 ? new BiomePatchValidator() : reused;
            var biomeInput = (caseId & 2) == 0 ? biomes : biomes.Reverse();
            var ruleInput = (caseId & 4) == 0 ? rules : rules.Reverse();
            var profileInput = (caseId & 8) == 0 ? profiles : profiles.Reverse();
            var pairInput = (caseId & 16) == 0 ? pairs : pairs.Reverse();
            var result = validator.Validate(export, biomeInput, ruleInput, profileInput, pairInput);

            Assert.That(result.Status, Is.EqualTo(BiomePatchValidationStatus.Completed));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RetryRequired, Is.False);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Violations, Is.Empty);
            Assert.That(result.Diagnostics.RuleResults.Count, Is.EqualTo(15));
            Assert.That(result.Diagnostics.RuleResults.All(value => value.Passed), Is.True);
            Assert.That(ResultSignature(result), Is.EqualTo(baselineSignature));
            AssertViableDiagnostics(result.Diagnostics);
        }

        [Test]
        public void FrozenEnumsAndStatusContractAreExact()
        {
            CollectionAssert.AreEqual(new[]
            {
                "RequiredBiomeCoverage", "PatchDefinitionIdentity", "PatchSizeLimits",
                "PatchConnectivity", "PatchSeedContract", "NormalPatchCountRange",
                "PatchRuleCountRange", "SameRuleSeedDistance", "WorldEdgePolicy",
                "WorldShareLimits", "CoreSiteOwnership", "ReservationAssignment",
                "OwnershipExclusivity", "IntrusionBoundaryContract", "ExportReproducibility"
            }, Enum.GetNames(typeof(BiomePatchValidationRule)));
            CollectionAssert.AreEqual(
                new[] { "Completed", "ValidationRejected", "InvalidInput" },
                Enum.GetNames(typeof(BiomePatchValidationStatus)));
        }

        [Test]
        public void ViableFixturePublishesExactActualCounters()
        {
            var result = reused.Validate(export, biomes, rules, profiles, pairs);
            AssertViableDiagnostics(result.Diagnostics);
            Assert.That(result.Publication.SourceExport, Is.SameAs(export.Publication));
            Assert.That(result.Publication.Snapshot, Is.SameAs(export.Publication.SourceCleanup.Snapshot));
            Assert.That(result.Publication.WorldWithBiomeAssignments,
                Is.SameAs(export.Publication.WorldWithBiomeAssignments));
            TestContext.Progress.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "MAP04_09_VIABLE rules={0};violations={1};patches={2};roles={3}/{4}/{5};assigned={6};unassigned={7};patchSum={8};patchRows={9};worldRows={10};patchBytes={11};worldBytes={12}",
                result.Diagnostics.RuleResults.Count(value => value.Passed),
                result.Violations.Count,
                result.Diagnostics.PatchCount,
                result.Diagnostics.CorePatchCount,
                result.Diagnostics.SatellitePatchCount,
                result.Diagnostics.IntrusionPatchCount,
                result.Diagnostics.AssignedSectorCount,
                result.Diagnostics.UnassignedSectorCount,
                result.Diagnostics.PatchSectorSum,
                result.Diagnostics.PatchCsvRowCount,
                result.Diagnostics.WorldCsvRowCount,
                result.Diagnostics.PatchCsvByteCount,
                result.Diagnostics.WorldCsvByteCount));
        }

        [Test]
        public void PublicationBytesAndCollectionsAreDefensiveReadOnlyCopies()
        {
            var publication = reused.Validate(export, biomes, rules, profiles, pairs).Publication;
            var patchBytes = publication.GeneratedBiomePatchesCsv;
            var worldBytes = publication.GeneratedWorldSectorsCsv;
            patchBytes[0] = 0;
            worldBytes[0] = 0;
            Assert.That(publication.GeneratedBiomePatchesCsv[0], Is.EqualTo(0xEF));
            Assert.That(publication.GeneratedWorldSectorsCsv[0], Is.EqualTo(0xEF));
            Assert.That(publication.GeneratedBiomePatchesCsv,
                Is.Not.SameAs(publication.GeneratedBiomePatchesCsv));
            Assert.That(publication.GeneratedWorldSectorsCsv,
                Is.Not.SameAs(publication.GeneratedWorldSectorsCsv));
            AssertReadOnly(publication.PatchRows);
            AssertReadOnly(publication.Diagnostics.RuleResults);
            AssertReadOnly(publication.Diagnostics.Violations);
        }

        [Test]
        public void NullInputsAccumulateStableSortedStructuralErrors()
        {
            var first = new BiomePatchValidator().Validate(null, null, null, null, null);
            var second = new BiomePatchValidator().Validate(null, null, null, null, null);
            Assert.That(first.Status, Is.EqualTo(BiomePatchValidationStatus.InvalidInput));
            Assert.That(first.Publication, Is.Null);
            Assert.That(first.Diagnostics, Is.Null);
            Assert.That(first.Violations, Is.Empty);
            Assert.That(first.Errors.Count, Is.GreaterThanOrEqualTo(9));
            Assert.That(first.Errors.Select(value => value.Code), Is.Ordered);
            Assert.That(ErrorSignature(first), Is.EqualTo(ErrorSignature(second)));
        }

        [Test]
        public void IncompleteExportIsInvalidAndNeverRetryable()
        {
            var incomplete = new BiomePatchExporter().Export(null, null);
            var result = reused.Validate(incomplete, biomes, rules, profiles, pairs);
            Assert.That(result.Status, Is.EqualTo(BiomePatchValidationStatus.InvalidInput));
            Assert.That(result.RetryRequired, Is.False);
            Assert.That(result.Errors.Select(value => value.Code),
                Does.Contain(BiomePatchValidationErrorCode.ExportNotCompleted));
            Assert.That(result.Errors.Select(value => value.Code),
                Does.Contain(BiomePatchValidationErrorCode.MissingExportPublication));
        }

        [Test]
        public void MissingDuplicateUnexpectedAndInactiveDefinitionsAreStructural()
        {
            var inactive = CloneWith(biomes[0], "<Active>k__BackingField", false);
            var unexpected = CloneWith(biomes[0], "<BiomeId>k__BackingField", "BIO_UNEXPECTED");
            var input = biomes.Skip(2).Concat(new[] { biomes[2], inactive, unexpected }).ToArray();
            var result = reused.Validate(export, input, rules, profiles, pairs);
            Assert.That(result.Status, Is.EqualTo(BiomePatchValidationStatus.InvalidInput));
            var codes = result.Errors.Select(value => value.Code).ToArray();
            Assert.That(codes, Does.Contain(BiomePatchValidationErrorCode.MissingDefinition));
            Assert.That(codes, Does.Contain(BiomePatchValidationErrorCode.DuplicateDefinition));
            Assert.That(codes, Does.Contain(BiomePatchValidationErrorCode.UnexpectedDefinition));
            Assert.That(codes, Does.Contain(BiomePatchValidationErrorCode.InactiveDefinition));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(0f)]
        [TestCase(1.01f)]
        public void NonFiniteOrOutOfRangeShareIsStructural(float value)
        {
            var changed = rules.ToArray();
            changed[0] = CloneWith(changed[0], "<MaxWorldShare>k__BackingField", value);
            var result = reused.Validate(export, biomes, changed, profiles, pairs);
            Assert.That(result.Status, Is.EqualTo(BiomePatchValidationStatus.InvalidInput));
            Assert.That(result.Errors.Select(error => error.Code),
                Does.Contain(BiomePatchValidationErrorCode.InvalidShareDefinition));
        }

        [TestCaseSource(nameof(RuleCases))]
        public void Rule_IndependentFailure(BiomePatchValidationRule target)
        {
            var biomeInput = biomes.ToArray();
            var ruleInput = rules.ToArray();
            var profileInput = profiles.ToArray();
            var pairInput = pairs.ToArray();
            using (var restore = new RestoreScope())
            {
                MutateForRule(target, restore, ref biomeInput, ref ruleInput, ref profileInput, ref pairInput);
                var result = reused.Validate(export, biomeInput, ruleInput, profileInput, pairInput);
                Assert.That(result.Status, Is.EqualTo(BiomePatchValidationStatus.ValidationRejected),
                    result.Errors.Count == 0 ? ViolationSignature(result) : ErrorSignature(result));
                Assert.That(result.RetryRequired, Is.True);
                Assert.That(result.Publication, Is.Null);
                Assert.That(result.Diagnostics, Is.Not.Null);
                Assert.That(result.Diagnostics.RuleResults.Count, Is.EqualTo(15));
                Assert.That(result.Violations.Select(value => value.Rule), Does.Contain(target));
                Assert.That(result.Diagnostics.RuleResults.Single(value => value.Rule == target).Passed,
                    Is.False);
            }
        }

        [TestCaseSource(nameof(RuleCases))]
        public void Rule_ViablePassesEveryExactRule(BiomePatchValidationRule target)
        {
            var result = reused.Validate(export, biomes, rules, profiles, pairs);
            var rule = result.Diagnostics.RuleResults.Single(value => value.Rule == target);
            Assert.That(rule.Passed, Is.True);
            Assert.That(rule.ViolationCount, Is.Zero);
            Assert.That(rule.CheckedCount, Is.GreaterThanOrEqualTo(0));
        }

        [TestCase("en-US")]
        [TestCase("tr-TR")]
        [TestCase("ko-KR")]
        public void CultureAndShuffledDefinitionsDoNotChangeResult(string cultureName)
        {
            var previous = CultureInfo.CurrentCulture;
            var previousUi = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
                var result = new BiomePatchValidator().Validate(
                    export, biomes.Reverse(), rules.Reverse(), profiles.Reverse(), pairs.Reverse());
                Assert.That(ResultSignature(result), Is.EqualTo(baselineSignature));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
                CultureInfo.CurrentUICulture = previousUi;
            }
        }

        [Test]
        public void ParallelFreshValidatorsProduceIdenticalResults()
        {
            var jobs = Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
                new BiomePatchValidator().Validate(export, biomes, rules, profiles, pairs))).ToArray();
            Task.WaitAll(jobs);
            foreach (var job in jobs)
            {
                Assert.That(job.Result.Status, Is.EqualTo(BiomePatchValidationStatus.Completed));
                Assert.That(ResultSignature(job.Result), Is.EqualTo(baselineSignature));
            }
        }

        [Test]
        public void ValidationDoesNotMutateSourcesOrConsumeRngOrWriteFiles()
        {
            var before = SourceSignature();
            var first = reused.Validate(export, biomes, rules, profiles, pairs);
            var second = reused.Validate(export, biomes, rules, profiles, pairs);
            Assert.That(SourceSignature(), Is.EqualTo(before));
            Assert.That(first.Diagnostics.RngDrawCount, Is.Zero);
            Assert.That(first.Diagnostics.SourceMutationCount, Is.Zero);
            Assert.That(second.Diagnostics.RngDrawCount, Is.Zero);
            Assert.That(second.Diagnostics.SourceMutationCount, Is.Zero);
        }

        [Test]
        public void RuntimeValidationSurfaceHasNoRngClockFileUnityObjectReflectionOrMutableStaticDependency()
        {
            var types = new[]
            {
                typeof(BiomePatchValidationViolation), typeof(BiomePatchValidationError),
                typeof(BiomePatchValidationRuleResult), typeof(BiomePatchValidationDiagnostics),
                typeof(BiomePatchValidationPublication), typeof(BiomePatchValidationResult),
                typeof(BiomePatchValidator)
            };
            foreach (var type in types)
            {
                Assert.That(type.IsSealed, Is.True, type.FullName);
                Assert.That(type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(field => !field.IsLiteral && !field.IsInitOnly), Is.Empty, type.FullName);
                var surface = type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Select(member => member.ToString()).ToArray();
                Assert.That(surface.Any(value => value.Contains("DeterministicRng") ||
                    value.Contains("System.Random") || value.Contains("UnityEngine.Random") ||
                    value.Contains("UnityEditor") || value.Contains("UnityEngine.Object") ||
                    value.Contains("System.IO") || value.Contains("DateTime") ||
                    value.Contains("System.Reflection")), Is.False, type.FullName);
            }
        }

        private void MutateForRule(
            BiomePatchValidationRule target,
            RestoreScope restore,
            ref BiomeTypeDefinition[] biomeInput,
            ref BiomePatchRuleDefinition[] ruleInput,
            ref BiomeBoundaryProfileDefinition[] profileInput,
            ref BiomeBoundaryPairRuleDefinition[] pairInput)
        {
            switch (target)
            {
                case BiomePatchValidationRule.RequiredBiomeCoverage:
                    biomeInput[0] = CloneWith(biomeInput[0], "<MinCorePatchCount>k__BackingField", 2);
                    return;
                case BiomePatchValidationRule.PatchDefinitionIdentity:
                {
                    var patch = export.Publication.SourceCleanup.Snapshot.Patches[0];
                    var index = Array.FindIndex(ruleInput, value => value.PatchRuleId == patch.PatchRuleId);
                    var otherBiome = biomes.First(value => value.BiomeId != patch.BiomeId).BiomeId;
                    ruleInput[index] = CloneWith(ruleInput[index], "<BiomeId>k__BackingField", otherBiome);
                    return;
                }
                case BiomePatchValidationRule.PatchSizeLimits:
                {
                    var patch = export.Publication.SourceCleanup.Snapshot.Patches.First(value =>
                        value.Role == BiomePatchRole.Satellite && value.SectorCount > 1);
                    var index = Array.FindIndex(ruleInput, value => value.PatchRuleId == patch.PatchRuleId);
                    var clone = CloneWith(ruleInput[index], "<MinSectorCount>k__BackingField", 1);
                    ruleInput[index] = CloneWith(clone, "<MaxSectorCount>k__BackingField", 1);
                    return;
                }
                case BiomePatchValidationRule.PatchConnectivity:
                {
                    var patch = export.Publication.SourceCleanup.Snapshot.Patches.First(value =>
                        value.Role == BiomePatchRole.Satellite && value.SectorCount > 2);
                    var first = patch.Seeds[0].SectorIndex;
                    var second = patch.SectorIndices.First(value => Manhattan(first, value) > 1);
                    var values = new List<int> { first, second };
                    restore.SetField(patch, "sectorIndices",
                        new ReadOnlyCollection<int>(values));
                    restore.SetField(patch, "sectorIndexSet", new HashSet<int>(values));
                    return;
                }
                case BiomePatchValidationRule.PatchSeedContract:
                {
                    var patch = export.Publication.SourceCleanup.Snapshot.Patches.First(value =>
                        value.Role == BiomePatchRole.Satellite);
                    var outsideSectorIndex = Enumerable.Range(0, WorldGenConstants.SectorCount)
                        .First(value => !patch.ContainsSector(value));
                    restore.SetField(patch.Seeds[0], "<SectorIndex>k__BackingField", outsideSectorIndex);
                    return;
                }
                case BiomePatchValidationRule.NormalPatchCountRange:
                {
                    var index = Array.FindIndex(biomeInput, value =>
                        export.Publication.SourceCleanup.Snapshot.Patches.Count(patch =>
                            patch.Role != BiomePatchRole.Intrusion && patch.BiomeId == value.BiomeId) > 1);
                    biomeInput[index] = CloneWith(biomeInput[index], "<MaxPatchCount>k__BackingField", 1);
                    return;
                }
                case BiomePatchValidationRule.PatchRuleCountRange:
                {
                    var ruleId = export.Publication.SourceCleanup.Snapshot.Patches[0].PatchRuleId;
                    var index = Array.FindIndex(ruleInput, value => value.PatchRuleId == ruleId);
                    var clone = CloneWith(ruleInput[index], "<SeedCountMin>k__BackingField", 0);
                    ruleInput[index] = CloneWith(clone, "<SeedCountMax>k__BackingField", 0);
                    return;
                }
                case BiomePatchValidationRule.SameRuleSeedDistance:
                {
                    var grouped = export.Publication.SourceCleanup.Snapshot.Patches
                        .GroupBy(value => value.PatchRuleId).First(value => value.Count() > 1);
                    var index = Array.FindIndex(ruleInput, value => value.PatchRuleId == grouped.Key);
                    ruleInput[index] = CloneWith(ruleInput[index], "<MinSeedDistance>k__BackingField", 100);
                    return;
                }
                case BiomePatchValidationRule.WorldEdgePolicy:
                {
                    var patch = export.Publication.SourceCleanup.Snapshot.Patches.First(value =>
                        value.SectorIndices.Any(IsWorldEdge));
                    var index = Array.FindIndex(ruleInput, value => value.PatchRuleId == patch.PatchRuleId);
                    ruleInput[index] = CloneWith(ruleInput[index], "<CanTouchWorldEdge>k__BackingField", false);
                    return;
                }
                case BiomePatchValidationRule.WorldShareLimits:
                {
                    var biomeId = biomes[0].BiomeId;
                    for (var index = 0; index < ruleInput.Length; index++)
                        if (ruleInput[index].BiomeId == biomeId && ruleInput[index].PatchRole != "INTRUSION")
                            ruleInput[index] = CloneWith(
                                ruleInput[index], "<MaxWorldShare>k__BackingField", 0.01f);
                    return;
                }
                case BiomePatchValidationRule.CoreSiteOwnership:
                {
                    var binding = export.Publication.SourceCleanup.Snapshot.SiteBindings[0];
                    var otherBiome = biomes.First(value => value.BiomeId != binding.BiomeId).BiomeId;
                    restore.SetField(binding, "<BiomeId>k__BackingField", otherBiome);
                    return;
                }
                case BiomePatchValidationRule.ReservationAssignment:
                {
                    var snapshot = export.Publication.SourceCleanup.Snapshot;
                    var site = export.Publication.SourceCleanup.SourceIntrusion.Publication.SourceSiteSnapshot;
                    var index = site.Sectors.First(value => !value.IsReserved).Index;
                    var rows = snapshot.Sectors.ToList();
                    rows[index] = BiomeSectorOwnership.CreateUnassigned(index, Coord(index));
                    restore.SetField(snapshot, "sectors", new ReadOnlyCollection<BiomeSectorOwnership>(rows));
                    return;
                }
                case BiomePatchValidationRule.OwnershipExclusivity:
                {
                    var snapshot = export.Publication.SourceCleanup.Snapshot;
                    var source = snapshot.Sectors.First(value => value.IsAssigned);
                    var otherBiome = biomes.First(value => value.BiomeId != source.PrimaryBiomeId).BiomeId;
                    var rows = snapshot.Sectors.ToList();
                    rows[source.SectorIndex] = new BiomeSectorOwnership(
                        source.SectorIndex, source.Sector, source.PrimaryBiomeId, otherBiome,
                        source.PatchId.Value);
                    restore.SetField(snapshot, "sectors", new ReadOnlyCollection<BiomeSectorOwnership>(rows));
                    return;
                }
                case BiomePatchValidationRule.IntrusionBoundaryContract:
                    for (var index = 0; index < pairInput.Length; index++)
                    {
                        var allowed = pairInput[index].AllowedBoundaryProfileIds
                            .Where(value => value != "BOUND_TUNNEL").ToArray();
                        if (allowed.Length == pairInput[index].AllowedBoundaryProfileIds.Count) continue;
                        var clone = CloneWith(pairInput[index],
                            "<AllowedBoundaryProfileIds>k__BackingField",
                            new ReadOnlyCollection<string>(allowed.ToList()));
                        clone = CloneWith(clone,
                            "<BoundaryProfileWeights>k__BackingField",
                            new ReadOnlyCollection<int>(Enumerable.Repeat(1, allowed.Length).ToList()));
                        if (!allowed.Contains(clone.DefaultBoundaryProfileId))
                            clone = CloneWith(clone, "<DefaultBoundaryProfileId>k__BackingField", allowed[0]);
                        pairInput[index] = clone;
                    }
                    return;
                case BiomePatchValidationRule.ExportReproducibility:
                {
                    var field = typeof(BiomePatchExportPublication).GetField(
                        "generatedBiomePatchesCsv", BindingFlags.Instance | BindingFlags.NonPublic);
                    var bytes = (byte[])field.GetValue(export.Publication);
                    var original = bytes[0];
                    bytes[0] = 0;
                    restore.Add(() => bytes[0] = original);
                    return;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(target));
            }
        }

        private static T CloneWith<T>(T source, string fieldName, object value)
            where T : class
        {
            var clone = (T)typeof(object).GetMethod(
                "MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(source, null);
            var field = FindField(clone.GetType(), fieldName);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(clone, value);
            return clone;
        }

        private static FieldInfo FindField(Type type, string name)
        {
            for (var current = type; current != null; current = current.BaseType)
            {
                var field = current.GetField(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null) return field;
            }
            return null;
        }

        private static object InvokePrivateStatic(Type type, string name, object[] arguments)
        {
            var method = type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, name);
            return method.Invoke(null, arguments);
        }

        private static object Get(object instance, string property)
        {
            var value = instance.GetType().GetProperty(
                property, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(value, Is.Not.Null, property);
            return value.GetValue(instance);
        }

        private static void AssertViableDiagnostics(BiomePatchValidationDiagnostics diagnostics)
        {
            Assert.That(diagnostics.WorldSeed, Is.EqualTo(ViableWorldSeed));
            Assert.That(diagnostics.RuleResults.Count, Is.EqualTo(15));
            Assert.That(diagnostics.RuleResults.Count(value => value.Passed), Is.EqualTo(15));
            Assert.That(diagnostics.Violations, Is.Empty);
            Assert.That(diagnostics.PatchCount, Is.EqualTo(17));
            Assert.That(diagnostics.CorePatchCount, Is.EqualTo(4));
            Assert.That(diagnostics.SatellitePatchCount, Is.EqualTo(10));
            Assert.That(diagnostics.IntrusionPatchCount, Is.EqualTo(3));
            Assert.That(diagnostics.AssignedSectorCount, Is.EqualTo(165));
            Assert.That(diagnostics.UnassignedSectorCount, Is.EqualTo(4));
            Assert.That(diagnostics.PatchSectorSum, Is.EqualTo(165));
            Assert.That(diagnostics.RequiredBiomeCount, Is.EqualTo(4));
            Assert.That(diagnostics.CoreBindingCount, Is.EqualTo(4));
            Assert.That(diagnostics.DisconnectedPatchCount, Is.Zero);
            Assert.That(diagnostics.OverlapCount, Is.Zero);
            Assert.That(diagnostics.OrphanCount, Is.Zero);
            Assert.That(diagnostics.UnassignedNonReservedCount, Is.Zero);
            Assert.That(diagnostics.SiteMisownershipCount, Is.Zero);
            Assert.That(diagnostics.IntrusionInvalidCount, Is.Zero);
            Assert.That(diagnostics.PatchCsvRowCount, Is.EqualTo(17));
            Assert.That(diagnostics.WorldCsvRowCount, Is.EqualTo(169));
            Assert.That(diagnostics.PatchCsvByteCount, Is.EqualTo(1956));
            Assert.That(diagnostics.WorldCsvByteCount, Is.EqualTo(16380));
            Assert.That(diagnostics.RngDrawCount, Is.Zero);
            Assert.That(diagnostics.SourceMutationCount, Is.Zero);
        }

        private string SourceSignature()
        {
            var snapshot = export.Publication.SourceCleanup.Snapshot;
            return string.Join("|", snapshot.Patches.Select(value =>
                       value.Id.Value + ":" + value.BiomeId + ":" + value.PatchRuleId + ":" +
                       string.Join(",", value.SectorIndices))) + "#" +
                   string.Join("|", snapshot.Sectors.Select(value =>
                       value.SectorIndex + ":" + value.PrimaryBiomeId + ":" + value.SecondaryBiomeId + ":" +
                       (value.PatchId.HasValue ? value.PatchId.Value.Value : ""))) + "#" +
                   string.Join("|", snapshot.SiteBindings.Select(value =>
                       value.SiteReservationId.Value + ":" + value.PatchId.Value + ":" + value.BiomeId));
        }

        private static string ResultSignature(BiomePatchValidationResult result)
        {
            if (result.Status != BiomePatchValidationStatus.Completed)
                return result.Status + "#" + ErrorSignature(result) + "#" + ViolationSignature(result);
            return result.Status + "#" + string.Join("|", result.Diagnostics.RuleResults.Select(value =>
                       value.Rule + ":" + value.Passed + ":" + value.CheckedCount + ":" + value.ViolationCount)) +
                   "#" + result.Diagnostics.PatchCount + ":" + result.Diagnostics.AssignedSectorCount + ":" +
                   result.Diagnostics.UnassignedSectorCount + ":" + result.Diagnostics.PatchCsvByteCount + ":" +
                   result.Diagnostics.WorldCsvByteCount;
        }

        private static string ErrorSignature(BiomePatchValidationResult result)
        {
            return string.Join("|", result.Errors.Select(value =>
                value.Code + ":" + value.DefinitionId + ":" + value.SectorIndex + ":" + value.Message));
        }

        private static string ViolationSignature(BiomePatchValidationResult result)
        {
            return string.Join("|", result.Violations.Select(value =>
                value.Rule + ":" + value.BiomeId + ":" + value.PatchId + ":" + value.SectorIndex + ":" +
                value.Expected + ":" + value.Actual + ":" + value.Message));
        }

        private static SectorCoord Coord(int index)
        {
            return new SectorCoord(index % WorldGenConstants.SectorColumns,
                index / WorldGenConstants.SectorColumns);
        }

        private static int Manhattan(int left, int right)
        {
            return Math.Abs(left % 13 - right % 13) + Math.Abs(left / 13 - right / 13);
        }

        private static bool IsWorldEdge(int index)
        {
            var x = index % WorldGenConstants.SectorColumns;
            var y = index / WorldGenConstants.SectorColumns;
            return x == 0 || y == 0 || x == WorldGenConstants.SectorColumns - 1 ||
                   y == WorldGenConstants.SectorRows - 1;
        }

        private static void AssertReadOnly<T>(IReadOnlyList<T> values)
        {
            Assert.That(values, Is.InstanceOf<IList>());
            Assert.Throws<NotSupportedException>(() => ((IList)values).Add(default(T)));
        }

        private sealed class RestoreScope : IDisposable
        {
            private readonly List<Action> restore = new List<Action>();

            public void SetField(object target, string fieldName, object value)
            {
                var field = FindField(target.GetType(), fieldName);
                Assert.That(field, Is.Not.Null, fieldName);
                var previous = field.GetValue(target);
                field.SetValue(target, value);
                restore.Add(() => field.SetValue(target, previous));
            }

            public void Add(Action action)
            {
                restore.Add(action);
            }

            public void Dispose()
            {
                for (var index = restore.Count - 1; index >= 0; index--)
                    restore[index]();
            }
        }
    }
}
