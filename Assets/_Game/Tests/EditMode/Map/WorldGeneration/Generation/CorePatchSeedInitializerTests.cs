using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    public sealed class CorePatchSeedInitializerTests
    {
        private static readonly DefinitionFixture CanonicalDefinitions = BuildDefinitions();

        public static IEnumerable CanonicalSeeds()
        {
            for (var index = 0; index < 100; index++)
            {
                var seed = index == 0 ? 0UL :
                    index == 1 ? 4660UL :
                    index == 2 ? ulong.MaxValue : (ulong)index;
                yield return new TestCaseData(seed).SetName(
                    "Initialize_CanonicalWorld_" + index.ToString("D3", CultureInfo.InvariantCulture));
            }
        }

        [TestCaseSource(nameof(CanonicalSeeds))]
        public void Initialize_CanonicalWorldPublishesExactCoreFootprints(ulong seed)
        {
            var source = CreateSnapshot(seed);
            var result = Initialize(source, CanonicalDefinitions);

            Assert.That(result.Status, Is.EqualTo(CorePatchInitializationStatus.Completed));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.RetryRequired, Is.False);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Publication.SourceSiteSnapshot, Is.SameAs(source));
            Assert.That(result.Publication.Snapshot.Seed, Is.EqualTo(seed));
            Assert.That(result.Publication.CorePatchCount, Is.EqualTo(4));
            Assert.That(result.Publication.CoreSeedCount, Is.EqualTo(4));
            Assert.That(result.Publication.CoreSiteBindingCount, Is.EqualTo(4));
            Assert.That(result.Publication.AssignedSectorCount, Is.EqualTo(4));
            Assert.That(result.Publication.UnassignedSectorCount, Is.EqualTo(165));
            Assert.That(result.Publication.Snapshot.IsComplete, Is.False);
            Assert.That(result.Diagnostics.RngDrawCount, Is.Zero);
            Assert.That(result.Diagnostics.SourceReservationCount, Is.EqualTo(4));
            Assert.That(result.Diagnostics.InputCoreSeedCount, Is.EqualTo(4));
            Assert.That(result.Diagnostics.CoreSeedCellCount, Is.EqualTo(4));
            Assert.That(result.Publication.Snapshot.Sectors, Has.Count.EqualTo(169));
            Assert.That(result.Publication.Snapshot.Sectors.All(item =>
                item.SecondaryBiomeId.Length == 0), Is.True);
            AssertCanonicalMappings(result.Publication.Snapshot);
        }

        [TestCase("RSV_02_SITE_MOON_SEAL_FORGE", "PATCHINST_CORE_RSV_02_SITE_MOON_SEAL_FORGE")]
        [TestCase("RSV_03_SITE_CASSIA_SAP_HEART", "PATCHINST_CORE_RSV_03_SITE_CASSIA_SAP_HEART")]
        [TestCase("RSV_04_SITE_DEEP_STAR_YEAST", "PATCHINST_CORE_RSV_04_SITE_DEEP_STAR_YEAST")]
        [TestCase("RSV_05_SITE_MOON_CORE_METEOR", "PATCHINST_CORE_RSV_05_SITE_MOON_CORE_METEOR")]
        public void CorePatchIdFactory_ProducesExactFrozenVectors(string sourceId, string expected)
        {
            var factory = new CorePatchIdFactory();
            var id = factory.CreateCorePatchId(new SiteReservationId(sourceId));
            Assert.That(id.Value, Is.EqualTo(expected));
            Assert.That(factory.TryCreateCorePatchId(new SiteReservationId(sourceId), out var second), Is.True);
            Assert.That(second, Is.EqualTo(id));
        }

        [Test]
        public void CorePatchIdFactory_RejectsInvalidDefault()
        {
            var factory = new CorePatchIdFactory();
            Assert.That(factory.TryCreateCorePatchId(default(SiteReservationId), out var id), Is.False);
            Assert.That(id.IsValid, Is.False);
            Assert.Throws<ArgumentException>(() =>
                factory.CreateCorePatchId(default(SiteReservationId)));
        }

        [Test]
        public void Initialize_NullInputsAccumulateAndPublishNothing()
        {
            var result = new CorePatchSeedInitializer().Initialize(null, null, null);
            Assert.That(result.Status, Is.EqualTo(CorePatchInitializationStatus.InvalidInput));
            Assert.That(result.Publication, Is.Null);
            Assert.That(result.Diagnostics, Is.Null);
            Assert.That(result.RetryRequired, Is.False);
            Assert.That(result.Errors.Select(item => item.Code), Does.Contain(
                CorePatchInitializationErrorCode.MissingSiteSnapshot));
            Assert.That(result.Errors.Select(item => item.Code), Does.Contain(
                CorePatchInitializationErrorCode.MissingBiomeTypes));
            Assert.That(result.Errors.Select(item => item.Code), Does.Contain(
                CorePatchInitializationErrorCode.MissingPatchRules));
        }

        [Test]
        public void Initialize_MissingRequiredDefinitionsIsAtomicInvalidInput()
        {
            var definitions = CanonicalDefinitions;
            var result = new CorePatchSeedInitializer().Initialize(
                CreateSnapshot(0),
                definitions.Biomes.Where(item => item.BiomeId != "BIO_CASSIA_ROOT"),
                definitions.Rules.Where(item => item.PatchRuleId != "PATCH_DOUGH_CORE"));

            AssertInvalid(result,
                CorePatchInitializationErrorCode.MissingRequiredBiomeType,
                CorePatchInitializationErrorCode.MissingRequiredPatchRule);
        }

        [Test]
        public void Initialize_NullAndDuplicateDefinitionsAreAccumulatedSortedAndDeduplicated()
        {
            var biomes = CanonicalDefinitions.Biomes.Concat(new[]
            {
                null, CanonicalDefinitions.Biomes[0], CanonicalDefinitions.Biomes[0]
            }).Reverse().ToArray();
            var rules = CanonicalDefinitions.Rules.Concat(new[]
            {
                null, CanonicalDefinitions.Rules[0], CanonicalDefinitions.Rules[0]
            }).Reverse().ToArray();

            var result = new CorePatchSeedInitializer().Initialize(CreateSnapshot(0), biomes, rules);

            AssertInvalid(result,
                CorePatchInitializationErrorCode.NullBiomeType,
                CorePatchInitializationErrorCode.DuplicateBiomeTypeId,
                CorePatchInitializationErrorCode.NullPatchRule,
                CorePatchInitializationErrorCode.DuplicatePatchRuleId);
            Assert.That(result.Errors.Count(item =>
                item.Code == CorePatchInitializationErrorCode.DuplicateBiomeTypeId), Is.EqualTo(1));
            Assert.That(result.Errors.Count(item =>
                item.Code == CorePatchInitializationErrorCode.DuplicatePatchRuleId), Is.EqualTo(1));
        }

        [TestCase(false, true, "CORE", 5, CorePatchInitializationErrorCode.InvalidBiomeType)]
        [TestCase(true, false, "CORE", 5, CorePatchInitializationErrorCode.InvalidPatchRule)]
        [TestCase(true, true, "SATELLITE", 5, CorePatchInitializationErrorCode.InvalidPatchRule)]
        [TestCase(true, true, "CORE", 0, CorePatchInitializationErrorCode.InvalidPatchRule)]
        public void Initialize_InvalidRequiredTypedDefinitionIsRejected(
            bool biomeActive,
            bool ruleActive,
            string patchRole,
            int cassiaMinimum,
            CorePatchInitializationErrorCode expected)
        {
            var definitions = BuildDefinitions(
                biomeActive, ruleActive, patchRole, cassiaMinimum);
            var result = Initialize(CreateSnapshot(0), definitions);
            AssertInvalid(result, expected);
        }

        [Test]
        public void Initialize_MismatchedCoreSeedDefinitionIdentityIsRejected()
        {
            var snapshot = CreateSnapshot(0, seedMutation: seeds =>
            {
                var original = seeds[1];
                seeds[1] = new CoreBiomeSeed(
                    original.SourceReservationId,
                    original.BiomeId,
                    original.CorePatchRuleId,
                    original.SeedSector,
                    6,
                    original.BufferRingSectors);
            });
            var result = Initialize(snapshot, CanonicalDefinitions);
            AssertInvalid(result,
                CorePatchInitializationErrorCode.InvalidCoreSeedSet,
                CorePatchInitializationErrorCode.DefinitionIdentityMismatch);
        }

        [Test]
        public void Initialize_SeedOutsideFootprintIsRejectedWithoutPartialPublication()
        {
            var snapshot = CreateSnapshot(0, seedMutation: seeds =>
            {
                var original = seeds[0];
                seeds[0] = new CoreBiomeSeed(
                    original.SourceReservationId,
                    original.BiomeId,
                    original.CorePatchRuleId,
                    new SectorCoord(9, 9),
                    original.MinimumCoreSectorCount,
                    original.BufferRingSectors);
            });
            var result = Initialize(snapshot, CanonicalDefinitions);
            AssertInvalid(result, CorePatchInitializationErrorCode.SeedOutsideSourceFootprint);
        }

        [Test]
        public void Initialize_UnexpectedReservationAndCoreSeedAreRejected()
        {
            var snapshot = CreateSnapshot(
                0,
                reservationMutation: reservations =>
                {
                    var old = reservations[5];
                    reservations[5] = CreateReservation(
                        5,
                        "RSV_05_SITE_OTHER_CORE",
                        "SITE_OTHER_CORE",
                        SiteReservationKind.CoreResource,
                        "BIO_MOON_CRATER",
                        old.Origin,
                        1);
                },
                seedMutation: seeds =>
                {
                    seeds[3] = new CoreBiomeSeed(
                        new SiteReservationId("RSV_05_SITE_OTHER_CORE"),
                        "BIO_MOON_CRATER",
                        "PATCH_CRATER_CORE",
                        new SectorCoord(10, 0),
                        5,
                        1);
                });
            var result = Initialize(snapshot, CanonicalDefinitions);
            AssertInvalid(result,
                CorePatchInitializationErrorCode.InvalidReservationSet,
                CorePatchInitializationErrorCode.MissingRequiredCoreSeed,
                CorePatchInitializationErrorCode.UnexpectedCoreSeed);
        }

        [Test]
        public void Initialize_GenericTwoCellFootprintSeedsAndOwnsEveryCell()
        {
            var source = CreateSnapshot(4660, forgeWidth: 2);
            var result = Initialize(source, CanonicalDefinitions);
            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            var forgeId = new SiteReservationId("RSV_02_SITE_MOON_SEAL_FORGE");
            Assert.That(result.Publication.Snapshot.TryGetSiteBinding(forgeId, out var binding), Is.True);
            Assert.That(binding.OccupiedSectorIndices, Is.EqualTo(new[] { 4, 5 }));
            Assert.That(result.Publication.Snapshot.TryGetPatch(binding.PatchId, out var patch), Is.True);
            Assert.That(patch.Seeds.Select(item => item.SectorIndex), Is.EqualTo(new[] { 4, 5 }));
            Assert.That(patch.SectorIndices, Is.EqualTo(new[] { 4, 5 }));
            Assert.That(result.Publication.AssignedSectorCount, Is.EqualTo(5));
            Assert.That(result.Publication.UnassignedSectorCount, Is.EqualTo(164));
            Assert.That(result.Diagnostics.CoreSeedCellCount, Is.EqualTo(5));
        }

        [Test]
        public void Initialize_DoesNotPrepaintMinimumBufferOrWitnessSectors()
        {
            var result = Initialize(CreateSnapshot(0), CanonicalDefinitions);
            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            Assert.That(result.Publication.Snapshot.Patches.Select(item => item.SectorCount),
                Is.EqualTo(new[] { 1, 1, 1, 1 }));
            Assert.That(result.Publication.AssignedSectorCount, Is.EqualTo(4));
            Assert.That(result.Publication.Snapshot.GetSector(21).IsAssigned, Is.False);
            Assert.That(result.Diagnostics.RngDrawCount, Is.Zero);
        }

        [Test]
        public void Initialize_IsStableAcrossOrderCultureAndInitializerReuse()
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                var initializer = new CorePatchSeedInitializer();
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                var first = initializer.Initialize(
                    CreateSnapshot(ulong.MaxValue, reverseInputs: true),
                    CanonicalDefinitions.Biomes.Reverse(),
                    CanonicalDefinitions.Rules.Reverse());
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                var second = initializer.Initialize(
                    CreateSnapshot(ulong.MaxValue),
                    CanonicalDefinitions.Biomes,
                    CanonicalDefinitions.Rules);
                Assert.That(LogicalSnapshot(first), Is.EqualTo(LogicalSnapshot(second)));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Test]
        public void Initialize_DefensivelyIsolatesCallerDefinitionCollections()
        {
            var biomes = CanonicalDefinitions.Biomes.ToArray();
            var rules = CanonicalDefinitions.Rules.ToArray();
            var result = new CorePatchSeedInitializer().Initialize(CreateSnapshot(0), biomes, rules);
            var before = LogicalSnapshot(result);
            Array.Reverse(biomes);
            Array.Clear(rules, 0, rules.Length);
            Assert.That(LogicalSnapshot(result), Is.EqualTo(before));
            Assert.That(result.Publication.CorePatchIds,
                Is.InstanceOf<ReadOnlyCollection<BiomePatchId>>());
            Assert.That(result.Diagnostics.SourceReservationIds,
                Is.InstanceOf<ReadOnlyCollection<SiteReservationId>>());
        }

        [Test]
        public void PublicSurfaceIsImmutableAndHasNoForbiddenDependency()
        {
            var types = new[]
            {
                typeof(CorePatchIdFactory), typeof(CorePatchInitializationError),
                typeof(CorePatchInitializationDiagnostics), typeof(CorePatchInitializationPublication),
                typeof(CorePatchInitializationResult), typeof(CorePatchSeedInitializer)
            };
            foreach (var type in types)
            {
                Assert.That(type.GetFields(BindingFlags.Public | BindingFlags.Instance), Is.Empty, type.Name);
                Assert.That(type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Any(property => property.SetMethod != null && property.SetMethod.IsPublic), Is.False, type.Name);
                Assert.That(type.GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Any(field => !field.IsLiteral && !field.IsInitOnly), Is.False, type.Name);
            }

            var method = typeof(CorePatchSeedInitializer).GetMethod("Initialize");
            var dependencyNames = method.GetParameters().Select(item => item.ParameterType.FullName ?? string.Empty)
                .Concat(new[] { method.ReturnType.FullName ?? string.Empty });
            Assert.That(dependencyNames.Any(name =>
                name.Contains("UnityEngine") || name.Contains("UnityEditor") ||
                name.Contains("System.Random") || name.Contains("DateTime") ||
                name.Contains("System.IO")), Is.False);
        }

        [Test]
        public void FrozenEnumsHaveExactOrder()
        {
            Assert.That(Enum.GetNames(typeof(CorePatchInitializationStatus)),
                Is.EqualTo(new[] { "Completed", "InvalidInput" }));
            Assert.That(Enum.GetNames(typeof(CorePatchInitializationErrorCode)), Is.EqualTo(new[]
            {
                "MissingSiteSnapshot", "InvalidSiteSnapshot", "InvalidReservationSet",
                "InvalidCoreSeedSet", "NullCoreSeed", "DuplicateCoreSeedSource",
                "MissingRequiredCoreSeed", "UnexpectedCoreSeed", "MissingSourceReservation",
                "InvalidSourceReservation", "SeedOutsideSourceFootprint", "SourceFootprintOverlap",
                "MissingBiomeTypes", "NullBiomeType", "DuplicateBiomeTypeId",
                "MissingRequiredBiomeType", "InvalidBiomeType", "MissingPatchRules",
                "NullPatchRule", "DuplicatePatchRuleId", "MissingRequiredPatchRule",
                "InvalidPatchRule", "DefinitionIdentityMismatch", "InvalidGeneratedPatchId",
                "DuplicateGeneratedPatchId", "InternalInvariantViolation"
            }));
        }

        private static CorePatchInitializationResult Initialize(
            SiteReservationSnapshot snapshot,
            DefinitionFixture definitions)
        {
            return new CorePatchSeedInitializer().Initialize(
                snapshot, definitions.Biomes, definitions.Rules);
        }

        private static void AssertCanonicalMappings(BiomePatchSnapshot snapshot)
        {
            Assert.That(snapshot.Patches.Select(item => item.Id.Value), Is.EqualTo(new[]
            {
                "PATCHINST_CORE_RSV_02_SITE_MOON_SEAL_FORGE",
                "PATCHINST_CORE_RSV_03_SITE_CASSIA_SAP_HEART",
                "PATCHINST_CORE_RSV_04_SITE_DEEP_STAR_YEAST",
                "PATCHINST_CORE_RSV_05_SITE_MOON_CORE_METEOR"
            }));
            Assert.That(snapshot.Patches.Select(item => item.BiomeId), Is.EqualTo(new[]
            {
                "BIO_ABANDONED_MILL", "BIO_CASSIA_ROOT", "BIO_MOON_DOUGH", "BIO_MOON_CRATER"
            }));
            Assert.That(snapshot.Patches.Select(item => item.PatchRuleId), Is.EqualTo(new[]
            {
                "PATCH_MILL_CORE", "PATCH_ROOT_CORE", "PATCH_DOUGH_CORE", "PATCH_CRATER_CORE"
            }));
            Assert.That(snapshot.SiteBindings.Select(item => item.SiteReservationId.Value),
                Is.EqualTo(new[]
                {
                    "RSV_02_SITE_MOON_SEAL_FORGE", "RSV_03_SITE_CASSIA_SAP_HEART",
                    "RSV_04_SITE_DEEP_STAR_YEAST", "RSV_05_SITE_MOON_CORE_METEOR"
                }));
            foreach (var patch in snapshot.Patches)
            {
                Assert.That(patch.Role, Is.EqualTo(BiomePatchRole.Core));
                foreach (var index in patch.SectorIndices)
                {
                    var ownership = snapshot.GetSector(index);
                    Assert.That(ownership.IsAssigned, Is.True);
                    Assert.That(ownership.PatchId, Is.EqualTo(patch.Id));
                    Assert.That(ownership.PrimaryBiomeId, Is.EqualTo(patch.BiomeId));
                }
            }
        }

        private static void AssertInvalid(
            CorePatchInitializationResult result,
            params CorePatchInitializationErrorCode[] codes)
        {
            Assert.That(result.Status, Is.EqualTo(CorePatchInitializationStatus.InvalidInput));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.RetryRequired, Is.False);
            Assert.That(result.Publication, Is.Null);
            Assert.That(result.Diagnostics, Is.Null);
            Assert.That(result.Errors, Is.Not.Empty);
            foreach (var code in codes)
                Assert.That(result.Errors.Select(item => item.Code), Does.Contain(code));
            Assert.That(result.Errors, Is.Ordered.Using<CorePatchInitializationError>(
                Comparer<CorePatchInitializationError>.Create(CompareErrors)));
        }

        private static int CompareErrors(
            CorePatchInitializationError left,
            CorePatchInitializationError right)
        {
            var value = left.Code.CompareTo(right.Code);
            if (value != 0) return value;
            value = string.Compare(left.SourceReservationId, right.SourceReservationId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = string.Compare(left.BiomeId, right.BiomeId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = string.Compare(left.PatchRuleId, right.PatchRuleId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = left.SectorIndex.CompareTo(right.SectorIndex);
            if (value != 0) return value;
            return string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }

        private static string LogicalSnapshot(CorePatchInitializationResult result)
        {
            Assert.That(result.Succeeded, Is.True, FormatErrors(result));
            var snapshot = result.Publication.Snapshot;
            return snapshot.Seed + "|" +
                   string.Join(";", snapshot.Patches.Select(patch =>
                       patch.Id.Value + ":" + patch.BiomeId + ":" + patch.PatchRuleId + ":" +
                       string.Join(",", patch.SectorIndices))) + "|" +
                   string.Join(";", snapshot.Sectors.Where(item => item.IsAssigned).Select(item =>
                       item.SectorIndex + ":" + item.PrimaryBiomeId + ":" + item.PatchId.Value.Value)) + "|" +
                   string.Join(";", snapshot.SiteBindings.Select(item =>
                       item.SiteReservationId.Value + ":" + item.PatchId.Value));
        }

        private static string FormatErrors(CorePatchInitializationResult result)
        {
            return string.Join("\n", result.Errors.Select(item =>
                item.Code + ":" + item.SourceReservationId + ":" + item.Message));
        }

        private static SiteReservationSnapshot CreateSnapshot(
            ulong seed,
            int forgeWidth = 1,
            bool reverseInputs = false,
            Action<List<SiteReservation>> reservationMutation = null,
            Action<List<CoreBiomeSeed>> seedMutation = null)
        {
            var reservations = new List<SiteReservation>
            {
                CreateReservation(0, "RSV_00_WORLD_MOONPALACE_V1", "WORLD_MOONPALACE_V1",
                    SiteReservationKind.Start, string.Empty, new SectorCoord(0, 0), 1),
                CreateReservation(1, "RSV_01_SITE_MOON_BOSS_VAULT", "SITE_MOON_BOSS_VAULT",
                    SiteReservationKind.Boss, "BIO_ABANDONED_MILL", new SectorCoord(2, 0), 1),
                CreateReservation(2, "RSV_02_SITE_MOON_SEAL_FORGE", "SITE_MOON_SEAL_FORGE",
                    SiteReservationKind.Forge, "BIO_ABANDONED_MILL", new SectorCoord(4, 0), forgeWidth),
                CreateReservation(3, "RSV_03_SITE_CASSIA_SAP_HEART", "SITE_CASSIA_SAP_HEART",
                    SiteReservationKind.CoreResource, "BIO_CASSIA_ROOT", new SectorCoord(6, 0), 1),
                CreateReservation(4, "RSV_04_SITE_DEEP_STAR_YEAST", "SITE_DEEP_STAR_YEAST",
                    SiteReservationKind.CoreResource, "BIO_MOON_DOUGH", new SectorCoord(8, 0), 1),
                CreateReservation(5, "RSV_05_SITE_MOON_CORE_METEOR", "SITE_MOON_CORE_METEOR",
                    SiteReservationKind.CoreResource, "BIO_MOON_CRATER", new SectorCoord(10, 0), 1),
                CreateReservation(6, "RSV_06_SITE_PRIMARY_VILLAGE", "SITE_PRIMARY_VILLAGE",
                    SiteReservationKind.Village, string.Empty, new SectorCoord(12, 0), 1)
            };
            reservationMutation?.Invoke(reservations);

            var seeds = new List<CoreBiomeSeed>
            {
                Seed(reservations[2], "BIO_ABANDONED_MILL", "PATCH_MILL_CORE", 4),
                Seed(reservations[3], "BIO_CASSIA_ROOT", "PATCH_ROOT_CORE", 5),
                Seed(reservations[4], "BIO_MOON_DOUGH", "PATCH_DOUGH_CORE", 5),
                Seed(reservations[5], "BIO_MOON_CRATER", "PATCH_CRATER_CORE", 5)
            };
            seedMutation?.Invoke(seeds);
            var sectors = CreateSectors(reservations);
            if (reverseInputs)
            {
                reservations.Reverse();
                seeds.Reverse();
                sectors.Reverse();
            }
            return new SiteReservationSnapshot(seed, reservations, sectors, seeds);
        }

        private static CoreBiomeSeed Seed(
            SiteReservation reservation,
            string biomeId,
            string ruleId,
            int minimum)
        {
            return new CoreBiomeSeed(
                reservation.ReservationId,
                biomeId,
                ruleId,
                reservation.OccupiedSectors.OrderBy(WorldGridIndex.ToIndex).First(),
                minimum,
                1);
        }

        private static SiteReservation CreateReservation(
            int order,
            string reservationId,
            string sourceId,
            SiteReservationKind kind,
            string biomeId,
            SectorCoord origin,
            int width)
        {
            var cells = Enumerable.Range(0, width).Select(localX =>
                new SiteFootprintCell(
                    localX, 0, kind == SiteReservationKind.Start ? "START" : "CORE",
                    biomeId, string.Empty, Array.Empty<SiteEntrySide>()));
            var footprint = new SiteFootprint(width, 1, SiteFootprintTransform.R0, cells);
            return new SiteReservation(
                new SiteReservationId(reservationId),
                kind,
                sourceId,
                origin,
                footprint,
                biomeId,
                order,
                Array.Empty<SiteEntryAnchor>());
        }

        private static List<SectorReservation> CreateSectors(
            IEnumerable<SiteReservation> reservations)
        {
            var occupied = new Dictionary<SectorCoord, Tuple<SiteReservation, SiteFootprintCell>>();
            foreach (var reservation in reservations)
                foreach (var coordinate in reservation.OccupiedSectors)
                {
                    Assert.That(reservation.TryGetFootprintCell(coordinate, out var cell), Is.True);
                    occupied.Add(coordinate, Tuple.Create(reservation, cell));
                }

            var result = new List<SectorReservation>(WorldGenConstants.SectorCount);
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var coordinate = WorldGridIndex.ToCoordinate(index);
                if (occupied.TryGetValue(coordinate, out var binding))
                {
                    result.Add(SectorReservation.CreateReserved(
                        index, coordinate, binding.Item1.ReservationId, binding.Item1.Kind,
                        binding.Item2.LocalX, binding.Item2.LocalY, binding.Item2.LocalRole));
                }
                else
                {
                    result.Add(SectorReservation.CreateUnreserved(index, coordinate));
                }
            }
            return result;
        }

        private static DefinitionFixture BuildDefinitions(
            bool biomeActive = true,
            bool ruleActive = true,
            string cassiaRole = "CORE",
            int cassiaMinimum = 5)
        {
            var specs = CreateSpecs();
            var biomeRows = new[]
            {
                BiomeRow("BIO_ABANDONED_MILL", biomeActive),
                BiomeRow("BIO_CASSIA_ROOT", biomeActive),
                BiomeRow("BIO_MOON_DOUGH", biomeActive),
                BiomeRow("BIO_MOON_CRATER", biomeActive)
            };
            var patchRows = new[]
            {
                PatchRow("PATCH_MILL_CORE", "BIO_ABANDONED_MILL", "CORE", 4, ruleActive),
                PatchRow("PATCH_ROOT_CORE", "BIO_CASSIA_ROOT", cassiaRole, cassiaMinimum, ruleActive),
                PatchRow("PATCH_DOUGH_CORE", "BIO_MOON_DOUGH", "CORE", 5, ruleActive),
                PatchRow("PATCH_CRATER_CORE", "BIO_MOON_CRATER", "CORE", 5, ruleActive)
            };
            var sources = specs.Select(spec => BuildSource(
                spec,
                spec.FileName == "biome_types.csv" ? biomeRows :
                spec.FileName == "biome_patch_rules.csv" ? patchRows :
                Array.Empty<string[]>())).ToArray();
            var result = new BiomeBoundaryDefinitionBuilder().Build(sources);
            if (!result.Success)
                throw new InvalidOperationException(string.Join("\n", result.Errors));
            return new DefinitionFixture(
                result.DefinitionSet.BiomeTypes.Values.ToArray(),
                result.DefinitionSet.BiomePatchRules.Values.ToArray());
        }

        private static string[] BiomeRow(string biomeId, bool active)
        {
            return new[]
            {
                biomeId, "NAME", "STAGE_MOON", "1", "1", "4", "1", "0", "12", "1",
                "THEME", "AUDIO", "MICRO", "RECIPE", "RESOURCE", "ELEMENT", "SITE_REQUIRED",
                active ? "1" : "0", string.Empty
            };
        }

        private static string[] PatchRow(
            string ruleId,
            string biomeId,
            string role,
            int minimum,
            bool active)
        {
            return new[]
            {
                ruleId, biomeId, role, minimum.ToString(CultureInfo.InvariantCulture), "18", "1", "1", "1",
                "1", "0", "1", "0", "0.35", "1", "1", "1", "1", "0.5",
                active ? "1" : "0", string.Empty
            };
        }

        private static BiomeBoundaryDefinitionSource BuildSource(
            FileSpec spec,
            IReadOnlyList<string[]> rows)
        {
            var schemaRows = spec.Columns.Select((column, index) => new CsvSchemaDictionaryRow(
                spec.FileName,
                (index + 1).ToString(CultureInfo.InvariantCulture),
                column.Name,
                column.DataType,
                index == 0 ? "1" : "0",
                index == 0 ? "1" : string.Empty,
                string.Empty,
                column.AllowedValues,
                string.Empty,
                string.Empty,
                index + 2));
            var catalog = new CsvSchemaCatalogBuilder().Build(schemaRows);
            if (!catalog.Success) throw new InvalidOperationException(string.Join("\n", catalog.Errors));
            var schema = catalog.Catalog.GetFile(spec.FileName);
            var csv = string.Join(",", spec.Columns.Select(column => column.Name));
            foreach (var row in rows)
                csv += "\n" + string.Join(",", row.Select(CsvCell));
            var read = new Rfc4180CsvReader().Read(
                new UTF8Encoding(false, true).GetBytes(csv), spec.FileName);
            var validation = new CsvHeaderAndFieldValidator().Validate(read, schema, spec.FileName);
            if (!validation.Success) throw new InvalidOperationException(string.Join("\n", validation.Errors));
            var keys = new CsvPrimaryKeyIndexBuilder().Build(schema, validation, spec.FileName);
            if (!keys.Success) throw new InvalidOperationException("Primary key fixture failed.");
            var parsed = new CsvScalarAndListParser().Parse(schema, validation, keys);
            if (!parsed.Success) throw new InvalidOperationException(string.Join("\n", parsed.Errors));
            return new BiomeBoundaryDefinitionSource(schema, parsed);
        }

        private static FileSpec[] CreateSpecs()
        {
            return new[]
            {
                File("biome_types.csv", "biome_id:ID", "display_name_ko:STRING", "stage_id:ID", "required:BOOL", "min_patch_count:INT", "max_patch_count:INT", "min_core_patch_count:INT", "preferred_altitude_min_sector_y:INT", "preferred_altitude_max_sector_y:INT", "growth_weight:FLOAT", "tile_theme_id:ID", "audio_profile_id:ID", "microchunk_pool_prefix:ID", "sector_recipe_pool_prefix:ID", "common_resource_pool_id:ID", "map_element_pool_id:ID", "required_special_map_ids:ID_LIST", "active:BOOL", "notes:STRING"),
                File("biome_patch_rules.csv", "patch_rule_id:ID", "biome_id:ID", "patch_role:ENUM", "min_sector_count:INT", "max_sector_count:INT", "min_seed_distance:INT", "seed_count_min:INT", "seed_count_max:INT", "seed_weight:FLOAT", "can_touch_world_edge:BOOL", "buffer_ring_sectors:INT", "allow_single_sector:BOOL", "max_world_share:FLOAT", "distance_weight:FLOAT", "altitude_weight:FLOAT", "noise_weight:FLOAT", "compactness_weight:FLOAT", "branchiness_target:FLOAT", "active:BOOL", "notes:STRING"),
                File("biome_boundary_profiles.csv", "boundary_profile_id:ID", "display_name_ko:STRING", "boundary_type:ENUM", "allowed_orientations:ENUM_LIST", "width_microchunks_min:INT", "width_microchunks_max:INT", "warning_microchunks_min:INT", "mandatory_route_allowed:BOOL", "tool_requirement:ENUM", "hard_border:BOOL", "active:BOOL", "notes:STRING"),
                File("biome_boundary_pair_rules.csv", "boundary_pair_rule_id:ID", "biome_a_id:ID", "biome_b_id:ID", "allowed_boundary_profile_ids:ID_LIST", "boundary_profile_weights:INT_LIST", "default_boundary_profile_id:ID", "transition_resource_pool_id:ID", "transition_element_pool_id:ID", "min_shared_edge_count:INT", "active:BOOL", "notes:STRING"),
                File("boundary_chunk_catalog.csv", "boundary_chunk_id:ID", "microchunk_id:ID", "biome_a_id:ID", "biome_b_id:ID", "boundary_profile_id:ID", "orientation:ENUM", "route_type:INT", "entry_edge_signature_id:ID", "exit_edge_signature_id:ID", "weight:INT", "reversible:BOOL", "active:BOOL", "notes:STRING")
            };
        }

        private static FileSpec File(string fileName, params string[] definitions)
        {
            return new FileSpec(fileName, definitions.Select(definition =>
            {
                var parts = definition.Split(':');
                var allowed = parts[0] == "patch_role" ? "CORE|SATELLITE|INTRUSION" :
                    parts[1] == "ENUM" || parts[1] == "ENUM_LIST" ? "ENUM_A|ENUM_B" : string.Empty;
                return new ColumnSpec(parts[0], parts[1], allowed);
            }).ToArray());
        }

        private static string CsvCell(string value)
        {
            return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0
                ? value
                : "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private sealed class DefinitionFixture
        {
            public DefinitionFixture(
                IReadOnlyList<BiomeTypeDefinition> biomes,
                IReadOnlyList<BiomePatchRuleDefinition> rules)
            {
                Biomes = biomes;
                Rules = rules;
            }

            public IReadOnlyList<BiomeTypeDefinition> Biomes { get; }
            public IReadOnlyList<BiomePatchRuleDefinition> Rules { get; }
        }

        private sealed class FileSpec
        {
            public FileSpec(string fileName, IReadOnlyList<ColumnSpec> columns)
            {
                FileName = fileName;
                Columns = columns;
            }

            public string FileName { get; }
            public IReadOnlyList<ColumnSpec> Columns { get; }
        }

        private sealed class ColumnSpec
        {
            public ColumnSpec(string name, string dataType, string allowedValues)
            {
                Name = name;
                DataType = dataType;
                AllowedValues = allowedValues;
            }

            public string Name { get; }
            public string DataType { get; }
            public string AllowedValues { get; }
        }
    }
}
