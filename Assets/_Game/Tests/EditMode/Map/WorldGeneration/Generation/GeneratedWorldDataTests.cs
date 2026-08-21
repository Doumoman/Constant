using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Generation
{
    public sealed class GeneratedWorldDataTests
    {
        private const string ExpectedHeader =
            "seed,sector_x,sector_y,sector_role,primary_biome_id,secondary_biome_id,patch_id,route_mask_id,special_site_instance_id,boundary_profile_id,sector_recipe_id,shortest_distance_from_start,mandatory_graph_node";

        [Test]
        public void GeneratedSectorRole_HasExactOrderedValues()
        {
            CollectionAssert.AreEqual(
                new[] { "Unassigned", "Mandatory", "Type0", "ReservedSite", "InactiveBuffer" },
                Enum.GetNames(typeof(GeneratedSectorRole)));
            CollectionAssert.AreEqual(
                new[] { 0, 1, 2, 3, 4 },
                Enum.GetValues(typeof(GeneratedSectorRole)).Cast<GeneratedSectorRole>().Select(value => (int)value));
            Assert.That(typeof(MandatoryRouteGraph).Assembly, Is.SameAs(typeof(GeneratedWorldData).Assembly));
            Assert.That(typeof(MandatoryRouteGraphValidator).Assembly, Is.SameAs(typeof(GeneratedWorldData).Assembly));
        }

        [TestCase(GeneratedSectorRole.Unassigned, "UNASSIGNED")]
        [TestCase(GeneratedSectorRole.Mandatory, "MANDATORY")]
        [TestCase(GeneratedSectorRole.Type0, "TYPE0")]
        [TestCase(GeneratedSectorRole.ReservedSite, "RESERVED_SITE")]
        [TestCase(GeneratedSectorRole.InactiveBuffer, "INACTIVE_BUFFER")]
        public void Serialize_MapsRoleToExactToken(GeneratedSectorRole role, string token)
        {
            var cells = CreateCells();
            cells[0] = CreateCell(0, new SectorCoord(0, 0), role: role);

            var fields = FirstDataRow(new GeneratedWorldData(1, cells)).Split(',');

            Assert.That(fields[3], Is.EqualTo(token));
        }

        [Test]
        public void SectorCell_RejectsUndefinedRole()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateCell(0, new SectorCoord(0, 0), role: (GeneratedSectorRole)999));
        }

        [Test]
        public void CreateUnassigned_UsesExactNeutralDefaults()
        {
            var coordinate = new SectorCoord(4, 7);
            var cell = SectorCell.CreateUnassigned(95, coordinate);

            Assert.That(cell.Index, Is.EqualTo(95));
            Assert.That(cell.Coordinate, Is.EqualTo(coordinate));
            Assert.That(cell.Role, Is.EqualTo(GeneratedSectorRole.Unassigned));
            Assert.That(cell.PrimaryBiomeId, Is.Empty);
            Assert.That(cell.SecondaryBiomeId, Is.Empty);
            Assert.That(cell.PatchId, Is.Empty);
            Assert.That(cell.RouteMaskId, Is.Empty);
            Assert.That(cell.SpecialSiteInstanceId, Is.Empty);
            Assert.That(cell.BoundaryProfileId, Is.Empty);
            Assert.That(cell.SectorRecipeId, Is.Empty);
            Assert.That(cell.ReservationId, Is.Empty);
            Assert.That(cell.ShortestDistanceFromStart, Is.EqualTo(-1));
            Assert.That(cell.MandatoryGraphNode, Is.False);
        }

        [Test]
        public void SectorCell_PreservesStringsWithoutNormalization()
        {
            var cell = CreateCell(
                0,
                new SectorCoord(0, 0),
                primaryBiomeId: "  Biome_Å  ",
                secondaryBiomeId: "SECONDARY_i",
                patchId: "Patch_ß",
                routeMaskId: "Route_İ",
                specialSiteInstanceId: "Site_é",
                boundaryProfileId: "Boundary_Ｅ",
                sectorRecipeId: "Recipe_가",
                reservationId: "Reservation_나");

            Assert.That(cell.PrimaryBiomeId, Is.EqualTo("  Biome_Å  "));
            Assert.That(cell.SecondaryBiomeId, Is.EqualTo("SECONDARY_i"));
            Assert.That(cell.PatchId, Is.EqualTo("Patch_ß"));
            Assert.That(cell.RouteMaskId, Is.EqualTo("Route_İ"));
            Assert.That(cell.SpecialSiteInstanceId, Is.EqualTo("Site_é"));
            Assert.That(cell.BoundaryProfileId, Is.EqualTo("Boundary_Ｅ"));
            Assert.That(cell.SectorRecipeId, Is.EqualTo("Recipe_가"));
            Assert.That(cell.ReservationId, Is.EqualTo("Reservation_나"));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public void SectorCell_RejectsNullString(int nullPosition)
        {
            var values = new[] { "a", "b", "c", "d", "e", "f", "g", "h" };
            values[nullPosition] = null;

            Assert.Throws<ArgumentNullException>(() => new SectorCell(
                0,
                new SectorCoord(0, 0),
                GeneratedSectorRole.Unassigned,
                values[0],
                values[1],
                values[2],
                values[3],
                values[4],
                values[5],
                values[6],
                values[7],
                -1,
                false));
        }

        [TestCase(-1)]
        [TestCase(WorldGenConstants.SectorCount)]
        public void SectorCell_RejectsOutOfRangeIndex(int index)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SectorCell.CreateUnassigned(index, new SectorCoord(0, 0)));
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(WorldGenConstants.SectorColumns, 0)]
        [TestCase(0, WorldGenConstants.SectorRows)]
        public void SectorCell_RejectsOutOfRangeCoordinate(int x, int y)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SectorCell.CreateUnassigned(0, new SectorCoord(x, y)));
        }

        [Test]
        public void SectorCell_IsSealedAndHasNoPublicSettersOrFields()
        {
            var type = typeof(SectorCell);

            Assert.That(type.IsSealed, Is.True);
            Assert.That(type.GetFields(BindingFlags.Instance | BindingFlags.Public), Is.Empty);
            Assert.That(type.GetProperties().All(property => property.SetMethod == null), Is.True);
        }

        [Test]
        public void GeneratedWorldData_RequiresNonNullCollection()
        {
            Assert.Throws<ArgumentNullException>(() => new GeneratedWorldData(0, null));
        }

        [Test]
        public void GeneratedWorldData_RequiresExactly169Cells()
        {
            var cells = CreateCells();
            cells.RemoveAt(cells.Count - 1);

            Assert.Throws<ArgumentException>(() => new GeneratedWorldData(0, cells));
        }

        [Test]
        public void GeneratedWorldData_RejectsExtraCell()
        {
            var cells = CreateCells();
            cells.Add(SectorCell.CreateUnassigned(0, new SectorCoord(0, 0)));

            Assert.Throws<ArgumentException>(() => new GeneratedWorldData(0, cells));
        }

        [Test]
        public void GeneratedWorldData_RejectsNullCell()
        {
            var cells = CreateCells();
            cells[80] = null;

            Assert.Throws<ArgumentException>(() => new GeneratedWorldData(0, cells));
        }

        [Test]
        public void GeneratedWorldData_RejectsDuplicateIndex()
        {
            var cells = CreateCells();
            cells[1] = SectorCell.CreateUnassigned(0, new SectorCoord(1, 0));

            Assert.Throws<ArgumentException>(() => new GeneratedWorldData(0, cells));
        }

        [Test]
        public void GeneratedWorldData_RejectsDuplicateCoordinate()
        {
            var cells = CreateCells();
            cells[1] = SectorCell.CreateUnassigned(1, new SectorCoord(0, 0));

            Assert.Throws<ArgumentException>(() => new GeneratedWorldData(0, cells));
        }

        [Test]
        public void GeneratedWorldData_AcceptsCompleteNonArithmeticIndexCoordinateMapping()
        {
            var cells = new List<SectorCell>(WorldGenConstants.SectorCount);
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var coordinateOrdinal = WorldGenConstants.SectorCount - 1 - index;
                cells.Add(SectorCell.CreateUnassigned(
                    index,
                    new SectorCoord(
                        coordinateOrdinal % WorldGenConstants.SectorColumns,
                        coordinateOrdinal / WorldGenConstants.SectorColumns)));
            }

            var world = new GeneratedWorldData(0, cells);

            Assert.That(world.GetCell(0).Coordinate, Is.EqualTo(new SectorCoord(12, 12)));
        }

        [Test]
        public void GeneratedWorldData_SortsSnapshotByIndex()
        {
            var cells = CreateCells();
            cells.Reverse();

            var world = new GeneratedWorldData(0, cells);

            CollectionAssert.AreEqual(Enumerable.Range(0, WorldGenConstants.SectorCount), world.Cells.Select(cell => cell.Index));
        }

        [Test]
        public void GeneratedWorldData_SnapshotsCallerCollection()
        {
            var cells = CreateCells();
            var original = cells[0];
            var world = new GeneratedWorldData(0, cells);

            cells[0] = CreateCell(0, new SectorCoord(0, 0), primaryBiomeId: "changed");
            cells.Clear();

            Assert.That(world.GetCell(0), Is.SameAs(original));
            Assert.That(world.Cells.Count, Is.EqualTo(WorldGenConstants.SectorCount));
        }

        [Test]
        public void GeneratedWorldData_ExposesReadOnlyCells()
        {
            var world = new GeneratedWorldData(0, CreateCells());
            var collection = (ICollection<SectorCell>)world.Cells;

            Assert.That(collection.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => collection.Add(world.Cells[0]));
        }

        [Test]
        public void GeneratedWorldData_ProvidesStableIndexAndCoordinateLookup()
        {
            var world = new GeneratedWorldData(123, CreateCells());
            var coordinate = new SectorCoord(5, 6);
            var expected = world.Cells.Single(cell => cell.Coordinate == coordinate);

            Assert.That(world.Seed, Is.EqualTo(123UL));
            Assert.That(world.GetCell(expected.Index), Is.SameAs(expected));
            Assert.That(world.GetCell(coordinate), Is.SameAs(expected));
            Assert.That(world.TryGetCell(expected.Index, out var byIndex), Is.True);
            Assert.That(byIndex, Is.SameAs(expected));
            Assert.That(world.TryGetCell(coordinate, out var byCoordinate), Is.True);
            Assert.That(byCoordinate, Is.SameAs(expected));
        }

        [Test]
        public void GeneratedWorldData_InvalidLookupsDoNotResolve()
        {
            var world = new GeneratedWorldData(0, CreateCells());

            Assert.That(world.TryGetCell(-1, out var byIndex), Is.False);
            Assert.That(byIndex, Is.Null);
            Assert.That(world.TryGetCell(new SectorCoord(-1, 0), out var byCoordinate), Is.False);
            Assert.That(byCoordinate, Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() => world.GetCell(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => world.GetCell(new SectorCoord(-1, 0)));
        }

        [Test]
        public void Serializer_HasExactFilenameAndHeader()
        {
            Assert.That(GeneratedWorldDataCsvSerializer.FileName, Is.EqualTo("generated_world_sectors.csv"));
            Assert.That(GeneratedWorldDataCsvSerializer.Header, Is.EqualTo(ExpectedHeader));
            Assert.That(ExpectedHeader.Split(',').Length, Is.EqualTo(13));
        }

        [Test]
        public void Serialize_HeaderTemplatePrefixIsExact210BytesAndKnownHash()
        {
            var bytes = GeneratedWorldDataCsvSerializer.Serialize(new GeneratedWorldData(0, CreateCells()));
            var prefix = bytes.Take(210).ToArray();

            using (var sha256 = SHA256.Create())
            {
                var hash = BitConverter.ToString(sha256.ComputeHash(prefix)).Replace("-", string.Empty).ToLowerInvariant();
                Assert.That(prefix.Length, Is.EqualTo(210));
                Assert.That(hash, Is.EqualTo("0721cfa4acb6bfb2d85e04ee295960a63844e4c5c72648f9e9cdb5d260aebf59"));
                Assert.That(Encoding.UTF8.GetString(prefix, 3, prefix.Length - 3), Is.EqualTo(ExpectedHeader + "\r\n"));
            }
        }

        [Test]
        public void Serialize_WritesSingleLeadingUtf8Bom()
        {
            var bytes = GeneratedWorldDataCsvSerializer.Serialize(new GeneratedWorldData(0, CreateCells()));

            Assert.That(bytes.Take(3), Is.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }));
            Assert.That(FindSequence(bytes, new byte[] { 0xEF, 0xBB, 0xBF }), Is.EqualTo(new[] { 0 }));
        }

        [Test]
        public void Serialize_UsesCrLfFor170RecordsAndOneFinalCrLf()
        {
            var text = SerializeText(new GeneratedWorldData(0, CreateCells()));
            var records = text.Split(new[] { "\r\n" }, StringSplitOptions.None);

            Assert.That(records.Length, Is.EqualTo(171));
            Assert.That(records[170], Is.Empty);
            Assert.That(text.EndsWith("\r\n", StringComparison.Ordinal), Is.True);
            Assert.That(text.EndsWith("\r\n\r\n", StringComparison.Ordinal), Is.False);
            Assert.That(HasBareNewline(text), Is.False);
        }

        [TestCase(0UL, "0")]
        [TestCase(ulong.MaxValue, "18446744073709551615")]
        public void Serialize_WritesSeedAsInvariantUnsignedDecimal(ulong seed, string expected)
        {
            var fields = FirstDataRow(new GeneratedWorldData(seed, CreateCells())).Split(',');

            Assert.That(fields[0], Is.EqualTo(expected));
        }

        [Test]
        public void Serialize_WritesInvariantSignedIntegerUnderNonEnglishCulture()
        {
            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var cells = CreateCells();
                cells[0] = CreateCell(0, new SectorCoord(0, 0), distance: -1234);

                var fields = FirstDataRow(new GeneratedWorldData(1234567, cells)).Split(',');

                Assert.That(fields[0], Is.EqualTo("1234567"));
                Assert.That(fields[11], Is.EqualTo("-1234"));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }

        [TestCase(false, "0")]
        [TestCase(true, "1")]
        public void Serialize_WritesBooleanAsZeroOrOne(bool value, string expected)
        {
            var cells = CreateCells();
            cells[0] = CreateCell(0, new SectorCoord(0, 0), mandatoryGraphNode: value);

            var fields = FirstDataRow(new GeneratedWorldData(0, cells)).Split(',');

            Assert.That(fields[12], Is.EqualTo(expected));
        }

        [Test]
        public void Serialize_WritesUnresolvedIdsAsEmptyFields()
        {
            var fields = FirstDataRow(new GeneratedWorldData(0, CreateCells())).Split(',');

            Assert.That(fields.Length, Is.EqualTo(13));
            for (var index = 4; index <= 10; index++)
            {
                Assert.That(fields[index], Is.Empty);
            }
        }

        [Test]
        public void Serialize_WritesAssignedIdsExactly()
        {
            var cells = CreateCells();
            cells[0] = CreateCell(
                0,
                new SectorCoord(0, 0),
                primaryBiomeId: "Biome_A",
                secondaryBiomeId: "Biome_B",
                patchId: "Patch_C",
                routeMaskId: "Route_D",
                specialSiteInstanceId: "Site_E",
                boundaryProfileId: "Boundary_F",
                sectorRecipeId: "Recipe_G");

            var fields = FirstDataRow(new GeneratedWorldData(0, cells)).Split(',');

            CollectionAssert.AreEqual(
                new[] { "Biome_A", "Biome_B", "Patch_C", "Route_D", "Site_E", "Boundary_F", "Recipe_G" },
                fields.Skip(4).Take(7));
        }

        [Test]
        public void Serialize_Rfc4180EscapesComma()
        {
            AssertSerializedTextContains("Biome,A", "\"Biome,A\"");
        }

        [Test]
        public void Serialize_Rfc4180EscapesAndDoublesQuotes()
        {
            AssertSerializedTextContains("Biome \"A\"", "\"Biome \"\"A\"\"\"");
        }

        [Test]
        public void Serialize_Rfc4180EscapesCrLfInsideField()
        {
            AssertSerializedTextContains("Biome\r\nA", "\"Biome\r\nA\"");
        }

        [Test]
        public void Serialize_RepeatedCallsAreByteIdentical()
        {
            var world = new GeneratedWorldData(987654321, CreateCells());

            CollectionAssert.AreEqual(
                GeneratedWorldDataCsvSerializer.Serialize(world),
                GeneratedWorldDataCsvSerializer.Serialize(world));
        }

        [Test]
        public void Serialize_ShuffledInputIsByteIdentical()
        {
            var ascending = CreateCells();
            var descending = CreateCells();
            descending.Reverse();

            CollectionAssert.AreEqual(
                GeneratedWorldDataCsvSerializer.Serialize(new GeneratedWorldData(7, ascending)),
                GeneratedWorldDataCsvSerializer.Serialize(new GeneratedWorldData(7, descending)));
        }

        [Test]
        public void Serialize_ReturnsIsolatedByteArrays()
        {
            var world = new GeneratedWorldData(0, CreateCells());
            var first = GeneratedWorldDataCsvSerializer.Serialize(world);
            var expectedFirstByte = first[0];
            first[0] = 0;

            var second = GeneratedWorldDataCsvSerializer.Serialize(world);

            Assert.That(second[0], Is.EqualTo(expectedFirstByte));
            Assert.That(first, Is.Not.SameAs(second));
        }

        [Test]
        public void Serialize_OrdersRowsByCellIndex()
        {
            var cells = CreateCells();
            cells[0] = CreateCell(0, new SectorCoord(0, 0), primaryBiomeId: "IndexZero");
            cells[168] = CreateCell(168, new SectorCoord(12, 12), primaryBiomeId: "Index168");
            cells.Reverse();

            var records = SerializeText(new GeneratedWorldData(0, cells))
                .Split(new[] { "\r\n" }, StringSplitOptions.None);

            Assert.That(records[1], Does.Contain("IndexZero"));
            Assert.That(records[169], Does.Contain("Index168"));
        }

        [Test]
        public void Serialize_DoesNotEmitIndexReservationOrExtraColumns()
        {
            var cells = CreateCells();
            cells[0] = CreateCell(0, new SectorCoord(0, 0), reservationId: "RESERVATION_MUST_NOT_APPEAR");

            var text = SerializeText(new GeneratedWorldData(0, cells));
            var fields = text.Split(new[] { "\r\n" }, StringSplitOptions.None)[1].Split(',');

            Assert.That(fields.Length, Is.EqualTo(13));
            Assert.That(ExpectedHeader, Does.Not.Contain("index"));
            Assert.That(ExpectedHeader, Does.Not.Contain("reservation"));
            Assert.That(text, Does.Not.Contain("RESERVATION_MUST_NOT_APPEAR"));
        }

        [Test]
        public void RuntimeTypesHaveNoUnityOrFileSystemSurface()
        {
            var runtimeTypes = new[]
            {
                typeof(GeneratedSectorRole),
                typeof(SectorCell),
                typeof(GeneratedWorldData),
                typeof(GeneratedWorldDataCsvSerializer)
            };

            var publicSurface = runtimeTypes
                .SelectMany(type => type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                .Select(member => member.ToString())
                .ToArray();

            Assert.That(publicSurface.Any(value => value.Contains("UnityEditor")), Is.False);
            Assert.That(publicSurface.Any(value => value.Contains("UnityEngine")), Is.False);
            Assert.That(publicSurface.Any(value => value.Contains("System.IO")), Is.False);
            Assert.That(typeof(GeneratedWorldDataCsvSerializer).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(method => method.Name.Contains("Write") || method.Name.Contains("Save")), Is.False);
        }

        private static List<SectorCell> CreateCells()
        {
            var cells = new List<SectorCell>(WorldGenConstants.SectorCount);
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                cells.Add(SectorCell.CreateUnassigned(
                    index,
                    new SectorCoord(
                        index % WorldGenConstants.SectorColumns,
                        index / WorldGenConstants.SectorColumns)));
            }

            return cells;
        }

        private static SectorCell CreateCell(
            int index,
            SectorCoord coordinate,
            GeneratedSectorRole role = GeneratedSectorRole.Unassigned,
            string primaryBiomeId = "",
            string secondaryBiomeId = "",
            string patchId = "",
            string routeMaskId = "",
            string specialSiteInstanceId = "",
            string boundaryProfileId = "",
            string sectorRecipeId = "",
            string reservationId = "",
            int distance = -1,
            bool mandatoryGraphNode = false)
        {
            return new SectorCell(
                index,
                coordinate,
                role,
                primaryBiomeId,
                secondaryBiomeId,
                patchId,
                routeMaskId,
                specialSiteInstanceId,
                boundaryProfileId,
                sectorRecipeId,
                reservationId,
                distance,
                mandatoryGraphNode);
        }

        private static string SerializeText(GeneratedWorldData world)
        {
            var bytes = GeneratedWorldDataCsvSerializer.Serialize(world);
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        }

        private static string FirstDataRow(GeneratedWorldData world)
        {
            return SerializeText(world).Split(new[] { "\r\n" }, StringSplitOptions.None)[1];
        }

        private static void AssertSerializedTextContains(string value, string expectedEscaped)
        {
            var cells = CreateCells();
            cells[0] = CreateCell(0, new SectorCoord(0, 0), primaryBiomeId: value);

            Assert.That(SerializeText(new GeneratedWorldData(0, cells)), Does.Contain(expectedEscaped));
        }

        private static int[] FindSequence(byte[] source, byte[] sequence)
        {
            var positions = new List<int>();
            for (var index = 0; index <= source.Length - sequence.Length; index++)
            {
                var matches = true;
                for (var sequenceIndex = 0; sequenceIndex < sequence.Length; sequenceIndex++)
                {
                    if (source[index + sequenceIndex] != sequence[sequenceIndex])
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                {
                    positions.Add(index);
                }
            }

            return positions.ToArray();
        }

        private static bool HasBareNewline(string text)
        {
            for (var index = 0; index < text.Length; index++)
            {
                if (text[index] == '\n' && (index == 0 || text[index - 1] != '\r'))
                {
                    return true;
                }

                if (text[index] == '\r' && (index + 1 >= text.Length || text[index + 1] != '\n'))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
