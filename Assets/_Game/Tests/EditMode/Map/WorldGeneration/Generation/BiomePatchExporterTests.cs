using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using UnityEngine;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    public sealed class BiomePatchExporterTests
    {
        private const ulong ViableWorldSeed = 0x0123456789ABCDF9UL;
        private const int ViableAttempt = 24;
        private PatchCleanupResult cleanup;
        private GeneratedWorldData sourceWorld;
        private BiomePatchExporter reused;
        private byte[] expectedPatchBytes;
        private byte[] expectedWorldBytes;
        private string cleanupSignature;
        private string worldSignature;

        public static IEnumerable<TestCaseData> DeterminismCases
        {
            get
            {
                for (var index = 0; index < 120; index++)
                    yield return new TestCaseData(index).SetName(
                        "Export_ViableDeterministicAtomicArtifacts_" +
                        index.ToString("D3", CultureInfo.InvariantCulture));
            }
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            cleanup = BuildCleanupResult();
            sourceWorld = CreateSourceWorld(ViableWorldSeed, false, false);
            reused = new BiomePatchExporter();
            var baseline = reused.Export(cleanup, sourceWorld);
            Assert.That(baseline.Status, Is.EqualTo(BiomePatchExportStatus.Completed),
                baseline.Errors.Count == 0 ? string.Empty : baseline.Errors[0].Message);
            expectedPatchBytes = baseline.Publication.GeneratedBiomePatchesCsv;
            expectedWorldBytes = baseline.Publication.GeneratedWorldSectorsCsv;
            cleanupSignature = SnapshotSignature(cleanup.Publication.Snapshot);
            worldSignature = WorldSignature(sourceWorld);
        }

        [TestCaseSource(nameof(DeterminismCases))]
        public void Export_ViableDeterministicAtomicArtifacts(int caseId)
        {
            var exporter = (caseId & 1) == 0 ? new BiomePatchExporter() : reused;
            var cells = sourceWorld.Cells.ToList();
            if ((caseId & 2) != 0) cells.Reverse();
            var world = new GeneratedWorldData(sourceWorld.Seed, cells);
            var result = exporter.Export(cleanup, world);

            Assert.That(result.Status, Is.EqualTo(BiomePatchExportStatus.Completed));
            Assert.That(result.Errors, Is.Empty);
            CollectionAssert.AreEqual(expectedPatchBytes, result.Publication.GeneratedBiomePatchesCsv);
            CollectionAssert.AreEqual(expectedWorldBytes, result.Publication.GeneratedWorldSectorsCsv);
            Assert.That(SnapshotSignature(cleanup.Publication.Snapshot), Is.EqualTo(cleanupSignature));
            Assert.That(WorldSignature(sourceWorld), Is.EqualTo(worldSignature));
            AssertViableConservation(result.Publication);
        }

        [Test]
        public void FrozenEnumsAndCsvContractAreExact()
        {
            CollectionAssert.AreEqual(
                new[] { "Completed", "InvalidInput" },
                Enum.GetNames(typeof(BiomePatchExportStatus)));
            CollectionAssert.AreEqual(new[]
            {
                "MissingCleanupResult", "CleanupNotCompleted", "MissingCleanupPublication",
                "MissingCleanupDiagnostics", "MissingSourceWorld", "SeedMismatch",
                "InvalidPatchSnapshot", "InvalidSourceWorld",
                "ConflictingExistingBiomeAssignment", "SerializationFailure",
                "InternalInvariantViolation"
            }, Enum.GetNames(typeof(BiomePatchExportErrorCode)));
            Assert.That(GeneratedBiomePatchCsvSerializer.FileName,
                Is.EqualTo("generated_biome_patches.csv"));
            Assert.That(GeneratedBiomePatchCsvSerializer.Header, Is.EqualTo(
                "seed,patch_instance_id,biome_id,patch_role,seed_sector_x,seed_sector_y,sector_count,min_x,min_y,max_x,max_y,perimeter_edges,special_map_instance_ids"));
            Assert.That(GeneratedBiomePatchCsvSerializer.Header.Split(',').Length, Is.EqualTo(13));
        }

        [Test]
        public void Export_NullInputsAccumulateSortedStableErrors()
        {
            var first = new BiomePatchExporter().Export(null, null);
            var second = new BiomePatchExporter().Export(null, null);
            Assert.That(first.Status, Is.EqualTo(BiomePatchExportStatus.InvalidInput));
            Assert.That(first.Publication, Is.Null);
            Assert.That(first.Errors.Select(value => value.Code), Is.Ordered);
            Assert.That(first.Errors.Select(value => value.Code), Is.EqualTo(new[]
            {
                BiomePatchExportErrorCode.MissingCleanupResult,
                BiomePatchExportErrorCode.MissingSourceWorld
            }));
            Assert.That(ErrorSignature(first), Is.EqualTo(ErrorSignature(second)));
        }

        [Test]
        public void Export_IncompleteCleanupReportsAllAvailableStructuralErrors()
        {
            var invalidCleanup = new PatchCleanup().Clean(null, null, null);
            var result = new BiomePatchExporter().Export(invalidCleanup, sourceWorld);
            Assert.That(result.Status, Is.EqualTo(BiomePatchExportStatus.InvalidInput));
            Assert.That(result.Publication, Is.Null);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(BiomePatchExportErrorCode.CleanupNotCompleted));
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(BiomePatchExportErrorCode.MissingCleanupPublication));
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(BiomePatchExportErrorCode.MissingCleanupDiagnostics));
        }

        [Test]
        public void Export_ViablePublishesExactCountsBytesAndHashes()
        {
            var result = new BiomePatchExporter().Export(cleanup, sourceWorld);
            Assert.That(result.Status, Is.EqualTo(BiomePatchExportStatus.Completed));
            AssertViableConservation(result.Publication);
            Assert.That(result.Publication.PatchRows.Sum(value => value.SectorCount), Is.EqualTo(165));
            Assert.That(result.Publication.WorldWithBiomeAssignments.Cells.Count(value =>
                value.PrimaryBiomeId.Length != 0), Is.EqualTo(165));
            Assert.That(result.Publication.WorldWithBiomeAssignments.Cells.All(value =>
                value.SecondaryBiomeId.Length == 0), Is.True);
            Debug.Log(string.Format(
                CultureInfo.InvariantCulture,
                "MAP04_08_VIABLE rows={0};assigned={1};unassigned={2};patchBytes={3};worldBytes={4};patchSha={5};worldSha={6}",
                result.Publication.PatchRowCount,
                result.Publication.AssignedSectorCount,
                result.Publication.UnassignedSectorCount,
                result.Publication.GeneratedBiomePatchesCsv.Length,
                result.Publication.GeneratedWorldSectorsCsv.Length,
                Sha256(result.Publication.GeneratedBiomePatchesCsv),
                Sha256(result.Publication.GeneratedWorldSectorsCsv)));
        }

        [Test]
        public void Export_WorldOverlayPreservesEveryNonBiomeField()
        {
            var decorated = CreateSourceWorld(ViableWorldSeed, true, false);
            var before = WorldSignature(decorated);
            var result = new BiomePatchExporter().Export(cleanup, decorated);
            Assert.That(result.Status, Is.EqualTo(BiomePatchExportStatus.Completed));
            for (var index = 0; index < 169; index++)
            {
                var source = decorated.Cells[index];
                var output = result.Publication.WorldWithBiomeAssignments.Cells[index];
                Assert.That(output.Index, Is.EqualTo(source.Index));
                Assert.That(output.Coordinate, Is.EqualTo(source.Coordinate));
                Assert.That(output.Role, Is.EqualTo(source.Role));
                Assert.That(output.RouteMaskId, Is.EqualTo(source.RouteMaskId));
                Assert.That(output.SpecialSiteInstanceId, Is.EqualTo(source.SpecialSiteInstanceId));
                Assert.That(output.BoundaryProfileId, Is.EqualTo(source.BoundaryProfileId));
                Assert.That(output.SectorRecipeId, Is.EqualTo(source.SectorRecipeId));
                Assert.That(output.ReservationId, Is.EqualTo(source.ReservationId));
                Assert.That(output.ShortestDistanceFromStart, Is.EqualTo(source.ShortestDistanceFromStart));
                Assert.That(output.MandatoryGraphNode, Is.EqualTo(source.MandatoryGraphNode));
            }
            Assert.That(WorldSignature(decorated), Is.EqualTo(before));
        }

        [Test]
        public void Export_PatchRowsMatchSnapshotSeedBoundsPerimeterAndBindings()
        {
            var publication = new BiomePatchExporter().Export(cleanup, sourceWorld).Publication;
            Assert.That(publication.PatchRows.Select(value => value.PatchInstanceId.Value), Is.Ordered);
            foreach (var row in publication.PatchRows)
            {
                Assert.That(cleanup.Publication.Snapshot.TryGetPatch(row.PatchInstanceId, out var patch), Is.True);
                var seed = patch.Seeds.Min(value => value.SectorIndex);
                Assert.That(row.Seed, Is.EqualTo(ViableWorldSeed));
                Assert.That(row.BiomeId, Is.EqualTo(patch.BiomeId));
                Assert.That(row.PatchRole, Is.EqualTo(patch.Role));
                Assert.That(row.SeedSectorX, Is.EqualTo(seed % 13));
                Assert.That(row.SeedSectorY, Is.EqualTo(seed / 13));
                Assert.That(row.SectorCount, Is.EqualTo(patch.SectorCount));
                Assert.That(row.MinX, Is.EqualTo(patch.SectorIndices.Min(value => value % 13)));
                Assert.That(row.MinY, Is.EqualTo(patch.SectorIndices.Min(value => value / 13)));
                Assert.That(row.MaxX, Is.EqualTo(patch.SectorIndices.Max(value => value % 13)));
                Assert.That(row.MaxY, Is.EqualTo(patch.SectorIndices.Max(value => value / 13)));
                Assert.That(row.PerimeterEdges, Is.EqualTo(Perimeter(patch)));
                var expectedSites = cleanup.Publication.Snapshot.SiteBindings
                    .Where(value => value.PatchId == patch.Id)
                    .Select(value => value.SiteReservationId.Value)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal);
                Assert.That(row.SpecialMapInstanceIds.Select(value => value.Value), Is.EqualTo(expectedSites));
            }
        }

        [Test]
        public void PatchCsv_HasExactBomCrLfFinalRecordAndThirteenColumns()
        {
            var bytes = new BiomePatchExporter().Export(cleanup, sourceWorld)
                .Publication.GeneratedBiomePatchesCsv;
            Assert.That(bytes.Take(3), Is.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }));
            var text = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
            Assert.That(text.StartsWith(GeneratedBiomePatchCsvSerializer.Header + "\r\n", StringComparison.Ordinal), Is.True);
            Assert.That(text.EndsWith("\r\n", StringComparison.Ordinal), Is.True);
            Assert.That(text.EndsWith("\r\n\r\n", StringComparison.Ordinal), Is.False);
            Assert.That(HasBareNewline(text), Is.False);
            var records = text.Split(new[] { "\r\n" }, StringSplitOptions.None);
            Assert.That(records.Length, Is.EqualTo(19));
            Assert.That(records[18], Is.Empty);
            Assert.That(records.Take(18).All(value => value.Split(',').Length == 13), Is.True);
        }

        [Test]
        public void PatchCsv_UsesInvariantMaxUlongRoleAndCanonicalPipeList()
        {
            var row = new GeneratedBiomePatchRow(
                ulong.MaxValue, new BiomePatchId("PATCH_Z"), "BIOME_A", BiomePatchRole.Core,
                0, 0, 1, 0, 0, 0, 0, 4,
                new[] { new SiteReservationId("SITE_Z"), new SiteReservationId("SITE_A") });
            var text = Encoding.UTF8.GetString(
                GeneratedBiomePatchCsvSerializer.Serialize(new[] { row }), 3,
                GeneratedBiomePatchCsvSerializer.Serialize(new[] { row }).Length - 3);
            var fields = text.Split(new[] { "\r\n" }, StringSplitOptions.None)[1].Split(',');
            Assert.That(fields[0], Is.EqualTo("18446744073709551615"));
            Assert.That(fields[3], Is.EqualTo("CORE"));
            Assert.That(fields[12], Is.EqualTo("SITE_A|SITE_Z"));
            AssertReadOnly(row.SpecialMapInstanceIds);
        }

        [TestCase("plain", "plain")]
        [TestCase("a,b", "\"a,b\"")]
        [TestCase("a\"b", "\"a\"\"b\"")]
        [TestCase("a\r\nb", "\"a\r\nb\"")]
        public void PatchCsv_Rfc4180FieldEscapingIsExact(string value, string expected)
        {
            var method = typeof(GeneratedBiomePatchCsvSerializer).GetMethod(
                "AppendField", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            var builder = new StringBuilder();
            method.Invoke(null, new object[] { builder, value });
            Assert.That(builder.ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void WorldCsv_IsExactlyTheExistingSerializerOutput()
        {
            var publication = new BiomePatchExporter().Export(cleanup, sourceWorld).Publication;
            CollectionAssert.AreEqual(
                GeneratedWorldDataCsvSerializer.Serialize(publication.WorldWithBiomeAssignments),
                publication.GeneratedWorldSectorsCsv);
            var text = Encoding.UTF8.GetString(
                publication.GeneratedWorldSectorsCsv, 3,
                publication.GeneratedWorldSectorsCsv.Length - 3);
            Assert.That(text.Split(new[] { "\r\n" }, StringSplitOptions.None).Length, Is.EqualTo(171));
            Assert.That(text.StartsWith(GeneratedWorldDataCsvSerializer.Header + "\r\n", StringComparison.Ordinal), Is.True);
        }

        [Test]
        public void Publication_ByteArraysAndRowsAreIsolatedReadOnlyCopies()
        {
            var publication = new BiomePatchExporter().Export(cleanup, sourceWorld).Publication;
            var patchBytes = publication.GeneratedBiomePatchesCsv;
            var worldBytes = publication.GeneratedWorldSectorsCsv;
            patchBytes[0] = 0;
            worldBytes[0] = 0;
            Assert.That(publication.GeneratedBiomePatchesCsv[0], Is.EqualTo(0xEF));
            Assert.That(publication.GeneratedWorldSectorsCsv[0], Is.EqualTo(0xEF));
            Assert.That(publication.GeneratedBiomePatchesCsv, Is.Not.SameAs(publication.GeneratedBiomePatchesCsv));
            Assert.That(publication.GeneratedWorldSectorsCsv, Is.Not.SameAs(publication.GeneratedWorldSectorsCsv));
            AssertReadOnly(publication.PatchRows);
        }

        [TestCase("en-US")]
        [TestCase("tr-TR")]
        public void Export_CultureFreshAndReusedInstancesAreByteIdentical(string cultureName)
        {
            var previous = CultureInfo.CurrentCulture;
            var previousUi = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
                var fresh = new BiomePatchExporter().Export(cleanup, sourceWorld);
                var reusedResult = reused.Export(cleanup, sourceWorld);
                CollectionAssert.AreEqual(expectedPatchBytes, fresh.Publication.GeneratedBiomePatchesCsv);
                CollectionAssert.AreEqual(expectedPatchBytes, reusedResult.Publication.GeneratedBiomePatchesCsv);
                CollectionAssert.AreEqual(expectedWorldBytes, fresh.Publication.GeneratedWorldSectorsCsv);
                CollectionAssert.AreEqual(expectedWorldBytes, reusedResult.Publication.GeneratedWorldSectorsCsv);
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
                CultureInfo.CurrentUICulture = previousUi;
            }
        }

        [Test]
        public void Export_ParallelFreshCallsAreByteIdentical()
        {
            var jobs = Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
                new BiomePatchExporter().Export(cleanup, sourceWorld))).ToArray();
            Task.WaitAll(jobs);
            foreach (var job in jobs)
            {
                Assert.That(job.Result.Status, Is.EqualTo(BiomePatchExportStatus.Completed));
                CollectionAssert.AreEqual(expectedPatchBytes, job.Result.Publication.GeneratedBiomePatchesCsv);
                CollectionAssert.AreEqual(expectedWorldBytes, job.Result.Publication.GeneratedWorldSectorsCsv);
            }
        }

        [Test]
        public void Export_RejectsSeedMismatchWithoutPublication()
        {
            var result = new BiomePatchExporter().Export(
                cleanup, CreateSourceWorld(ViableWorldSeed + 1, false, false));
            Assert.That(result.Status, Is.EqualTo(BiomePatchExportStatus.InvalidInput));
            Assert.That(result.Publication, Is.Null);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(BiomePatchExportErrorCode.SeedMismatch));
        }

        [Test]
        public void Export_RejectsNonRowMajorSourceWorld()
        {
            var result = new BiomePatchExporter().Export(
                cleanup, CreateSourceWorld(ViableWorldSeed, false, true));
            Assert.That(result.Status, Is.EqualTo(BiomePatchExportStatus.InvalidInput));
            Assert.That(result.Publication, Is.Null);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(BiomePatchExportErrorCode.InvalidSourceWorld));
        }

        [Test]
        public void Export_RejectsConflictingExistingAssignmentAtomically()
        {
            var cells = sourceWorld.Cells.ToList();
            var assignedIndex = cleanup.Publication.Snapshot.Sectors.First(value => value.IsAssigned).SectorIndex;
            var source = cells[assignedIndex];
            cells[assignedIndex] = new SectorCell(
                source.Index, source.Coordinate, source.Role, "WRONG", string.Empty, string.Empty,
                source.RouteMaskId, source.SpecialSiteInstanceId, source.BoundaryProfileId,
                source.SectorRecipeId, source.ReservationId, source.ShortestDistanceFromStart,
                source.MandatoryGraphNode);
            var result = new BiomePatchExporter().Export(
                cleanup, new GeneratedWorldData(ViableWorldSeed, cells));
            Assert.That(result.Status, Is.EqualTo(BiomePatchExportStatus.InvalidInput));
            Assert.That(result.Publication, Is.Null);
            Assert.That(result.Errors.Single(value =>
                value.Code == BiomePatchExportErrorCode.ConflictingExistingBiomeAssignment).SectorIndex,
                Is.EqualTo(assignedIndex));
        }

        [Test]
        public void RuntimeExportSurfaceHasNoRngClockFileUnityObjectReflectionOrMutableStaticDependency()
        {
            var types = new[]
            {
                typeof(BiomePatchExportError), typeof(GeneratedBiomePatchRow),
                typeof(GeneratedBiomePatchCsvSerializer), typeof(BiomePatchExportPublication),
                typeof(BiomePatchExportResult), typeof(BiomePatchExporter)
            };
            foreach (var type in types)
            {
                if (type.IsClass && !(type.IsAbstract && type.IsSealed))
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

        private static PatchCleanupResult BuildCleanupResult()
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
            var biomes = ((IEnumerable<BiomeTypeDefinition>)Get(definitionsType, definitions, "Biomes")).ToArray();
            var rules = ((IEnumerable<BiomePatchRuleDefinition>)Get(definitionsType, definitions, "AllRules")).ToArray();
            var profiles = (IEnumerable<BiomeBoundaryProfileDefinition>)Get(definitionsType, definitions, "Profiles");
            var pairs = (IEnumerable<BiomeBoundaryPairRuleDefinition>)Get(definitionsType, definitions, "Pairs");
            var intrusion = new IntrusionPlacer().Place(
                growth, profile, biomes, rules, profiles, pairs, rng);
            Assert.That(intrusion.Status, Is.EqualTo(IntrusionPlacementStatus.Completed));
            var result = new PatchCleanup().Clean(intrusion, biomes, rules);
            Assert.That(result.Status, Is.EqualTo(PatchCleanupStatus.Completed));
            return result;
        }

        private static object Get(Type type, object instance, string property)
        {
            var value = type.GetProperty(property, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(value, Is.Not.Null, property);
            return value.GetValue(instance);
        }

        private static GeneratedWorldData CreateSourceWorld(ulong seed, bool decorated, bool reverseCoordinates)
        {
            var cells = new List<SectorCell>(169);
            for (var index = 0; index < 169; index++)
            {
                var coordinateIndex = reverseCoordinates ? 168 - index : index;
                var coordinate = new SectorCoord(coordinateIndex % 13, coordinateIndex / 13);
                cells.Add(new SectorCell(
                    index,
                    coordinate,
                    decorated ? (GeneratedSectorRole)(index % 5) : GeneratedSectorRole.Unassigned,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    decorated ? "ROUTE_" + (index % 4).ToString(CultureInfo.InvariantCulture) : string.Empty,
                    decorated ? "SITE_" + index.ToString(CultureInfo.InvariantCulture) : string.Empty,
                    decorated ? "BOUNDARY_" + (index % 3).ToString(CultureInfo.InvariantCulture) : string.Empty,
                    decorated ? "RECIPE_" + index.ToString(CultureInfo.InvariantCulture) : string.Empty,
                    decorated ? "RESERVATION_" + index.ToString(CultureInfo.InvariantCulture) : string.Empty,
                    decorated ? index - 40 : -1,
                    decorated && (index % 2 == 0)));
            }
            return new GeneratedWorldData(seed, cells);
        }

        private static int Perimeter(BiomePatch patch)
        {
            var sectors = new HashSet<int>(patch.SectorIndices);
            var count = 0;
            foreach (var index in sectors)
            {
                var x = index % 13;
                var y = index / 13;
                if (x == 0 || !sectors.Contains(index - 1)) count++;
                if (x == 12 || !sectors.Contains(index + 1)) count++;
                if (y == 0 || !sectors.Contains(index - 13)) count++;
                if (y == 12 || !sectors.Contains(index + 13)) count++;
            }
            return count;
        }

        private static void AssertViableConservation(BiomePatchExportPublication publication)
        {
            Assert.That(publication.BiomePatchFileName, Is.EqualTo("generated_biome_patches.csv"));
            Assert.That(publication.WorldSectorFileName, Is.EqualTo("generated_world_sectors.csv"));
            Assert.That(publication.PatchRowCount, Is.EqualTo(17));
            Assert.That(publication.WorldSectorRowCount, Is.EqualTo(169));
            Assert.That(publication.AssignedSectorCount, Is.EqualTo(165));
            Assert.That(publication.UnassignedSectorCount, Is.EqualTo(4));
            Assert.That(publication.SourceCleanup.Snapshot.Seed, Is.EqualTo(ViableWorldSeed));
            Assert.That(publication.SourceWorld.Seed, Is.EqualTo(ViableWorldSeed));
            Assert.That(publication.WorldWithBiomeAssignments.Seed, Is.EqualTo(ViableWorldSeed));
        }

        private static string ErrorSignature(BiomePatchExportResult result)
        {
            return string.Join("|", result.Errors.Select(value =>
                value.Code + ":" + value.DefinitionId + ":" + value.SectorIndex + ":" + value.Message));
        }

        private static string SnapshotSignature(BiomePatchSnapshot snapshot)
        {
            return string.Join("|", snapshot.Patches.Select(patch =>
                patch.Id.Value + ":" + patch.BiomeId + ":" + string.Join(",", patch.SectorIndices))) + "#" +
                string.Join("|", snapshot.Sectors.Select(value =>
                    value.IsAssigned
                        ? value.SectorIndex + ":" + value.PrimaryBiomeId + ":" + value.PatchId.Value.Value
                        : value.SectorIndex + ":_"));
        }

        private static string WorldSignature(GeneratedWorldData world)
        {
            return world.Seed.ToString(CultureInfo.InvariantCulture) + "#" +
                string.Join("|", world.Cells.Select(value =>
                    value.Index + ":" + value.Coordinate.X + "," + value.Coordinate.Y + ":" +
                    value.Role + ":" + value.PrimaryBiomeId + ":" + value.SecondaryBiomeId + ":" +
                    value.PatchId + ":" + value.RouteMaskId + ":" + value.SpecialSiteInstanceId + ":" +
                    value.BoundaryProfileId + ":" + value.SectorRecipeId + ":" + value.ReservationId + ":" +
                    value.ShortestDistanceFromStart + ":" + value.MandatoryGraphNode));
        }

        private static string Sha256(byte[] bytes)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static bool HasBareNewline(string text)
        {
            for (var index = 0; index < text.Length; index++)
            {
                if (text[index] == '\n' && (index == 0 || text[index - 1] != '\r')) return true;
                if (text[index] == '\r' && (index + 1 >= text.Length || text[index + 1] != '\n')) return true;
            }
            return false;
        }

        private static void AssertReadOnly<T>(IReadOnlyList<T> values)
        {
            Assert.That(values, Is.InstanceOf<IList>());
            Assert.Throws<NotSupportedException>(() => ((IList)values).Add(default(T)));
        }
    }
}
