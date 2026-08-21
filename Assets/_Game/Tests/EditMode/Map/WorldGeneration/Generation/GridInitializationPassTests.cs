using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Generation
{
    public sealed class GridInitializationPassTests
    {
        [Test]
        public void WorldConstants_DefineExactThirteenByThirteenGrid()
        {
            Assert.That(WorldGenConstants.SectorColumns, Is.EqualTo(13));
            Assert.That(WorldGenConstants.SectorRows, Is.EqualTo(13));
            Assert.That(WorldGenConstants.SectorCount, Is.EqualTo(169));
        }

        [TestCase(0, 0, 0)]
        [TestCase(12, 0, 12)]
        [TestCase(6, 6, 84)]
        [TestCase(0, 12, 156)]
        [TestCase(12, 12, 168)]
        public void ToIndex_UsesExactRowMajorMapping(int x, int y, int expected)
        {
            Assert.That(WorldGridIndex.ToIndex(new SectorCoord(x, y)), Is.EqualTo(expected));
        }

        [TestCase(0, 0, 0)]
        [TestCase(12, 12, 0)]
        [TestCase(84, 6, 6)]
        [TestCase(156, 0, 12)]
        [TestCase(168, 12, 12)]
        public void ToCoordinate_UsesBottomLeftOrigin(int index, int expectedX, int expectedY)
        {
            Assert.That(WorldGridIndex.ToCoordinate(index), Is.EqualTo(new SectorCoord(expectedX, expectedY)));
        }

        [Test]
        public void GridIndex_RoundTripsEveryCoordinateAndIndex()
        {
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var coordinate = WorldGridIndex.ToCoordinate(index);
                Assert.That(WorldGridIndex.ToIndex(coordinate), Is.EqualTo(index));
                Assert.That(coordinate.X, Is.InRange(0, WorldGenConstants.SectorColumns - 1));
                Assert.That(coordinate.Y, Is.InRange(0, WorldGenConstants.SectorRows - 1));
            }
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(WorldGenConstants.SectorColumns, 0)]
        [TestCase(0, WorldGenConstants.SectorRows)]
        public void ToIndex_RejectsOutOfRangeCoordinate(int x, int y)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => WorldGridIndex.ToIndex(new SectorCoord(x, y)));
        }

        [TestCase(-1)]
        [TestCase(WorldGenConstants.SectorCount)]
        public void ToCoordinate_RejectsOutOfRangeIndex(int index)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => WorldGridIndex.ToCoordinate(index));
        }

        [TestCase(0, -1, 1, 13, -1)]
        [TestCase(12, 11, -1, 25, -1)]
        [TestCase(84, 83, 85, 97, 71)]
        [TestCase(156, -1, 157, -1, 143)]
        [TestCase(168, 167, -1, -1, 155)]
        public void NeighborGetters_ReturnExactKnownTopology(
            int index,
            int left,
            int right,
            int up,
            int down)
        {
            Assert.That(WorldGridIndex.GetLeftIndex(index), Is.EqualTo(left));
            Assert.That(WorldGridIndex.GetRightIndex(index), Is.EqualTo(right));
            Assert.That(WorldGridIndex.GetUpIndex(index), Is.EqualTo(up));
            Assert.That(WorldGridIndex.GetDownIndex(index), Is.EqualTo(down));
        }

        [TestCase(-1)]
        [TestCase(WorldGenConstants.SectorCount)]
        public void NeighborGetters_RejectOutOfRangeIndex(int index)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => WorldGridIndex.GetLeftIndex(index));
            Assert.Throws<ArgumentOutOfRangeException>(() => WorldGridIndex.GetRightIndex(index));
            Assert.Throws<ArgumentOutOfRangeException>(() => WorldGridIndex.GetUpIndex(index));
            Assert.Throws<ArgumentOutOfRangeException>(() => WorldGridIndex.GetDownIndex(index));
        }

        [Test]
        public void SectorNeighborIndices_PreservesExactValues()
        {
            var entry = new SectorNeighborIndices(84, 83, 85, 97, 71);

            Assert.That(entry.Index, Is.EqualTo(84));
            Assert.That(entry.LeftIndex, Is.EqualTo(83));
            Assert.That(entry.RightIndex, Is.EqualTo(85));
            Assert.That(entry.UpIndex, Is.EqualTo(97));
            Assert.That(entry.DownIndex, Is.EqualTo(71));
            Assert.That(entry.ValidNeighborCount, Is.EqualTo(4));
        }

        [Test]
        public void SectorNeighborIndices_NoNeighborIsExactMinusOne()
        {
            Assert.That(SectorNeighborIndices.NoNeighbor, Is.EqualTo(-1));
        }

        [TestCase(0, 2)]
        [TestCase(12, 2)]
        [TestCase(84, 4)]
        [TestCase(156, 2)]
        [TestCase(168, 2)]
        public void SectorNeighborIndices_CountsValidNeighbors(int index, int expected)
        {
            Assert.That(CreateNeighbor(index).ValidNeighborCount, Is.EqualTo(expected));
        }

        [TestCase(-1)]
        [TestCase(WorldGenConstants.SectorCount)]
        public void SectorNeighborIndices_RejectsOutOfRangeOwner(int index)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SectorNeighborIndices(index, -1, -1, -1, -1));
        }

        [TestCase("left", -2)]
        [TestCase("right", -2)]
        [TestCase("up", -2)]
        [TestCase("down", -2)]
        [TestCase("left", WorldGenConstants.SectorCount)]
        [TestCase("right", WorldGenConstants.SectorCount)]
        [TestCase("up", WorldGenConstants.SectorCount)]
        [TestCase("down", WorldGenConstants.SectorCount)]
        public void SectorNeighborIndices_RejectsInvalidNeighborRange(string position, int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateWithSlot(position, value));
        }

        [TestCase("left")]
        [TestCase("right")]
        [TestCase("up")]
        [TestCase("down")]
        public void SectorNeighborIndices_RejectsSelfNeighbor(string position)
        {
            Assert.Throws<ArgumentException>(() => CreateWithSlot(position, 84));
        }

        [TestCase(83, 83, -1, -1)]
        [TestCase(83, -1, 83, -1)]
        [TestCase(83, -1, -1, 83)]
        [TestCase(-1, 85, 85, -1)]
        public void SectorNeighborIndices_RejectsDuplicateValidNeighbors(
            int left,
            int right,
            int up,
            int down)
        {
            Assert.Throws<ArgumentException>(() =>
                new SectorNeighborIndices(84, left, right, up, down));
        }

        [Test]
        public void SectorNeighborIndices_IsSealedAndReadOnly()
        {
            var type = typeof(SectorNeighborIndices);

            Assert.That(type.IsSealed, Is.True);
            Assert.That(type.GetFields(BindingFlags.Instance | BindingFlags.Public), Is.Empty);
            Assert.That(type.GetProperties().All(property => property.SetMethod == null), Is.True);
        }

        [Test]
        public void Pass_HasExactIdsAndNoInstanceState()
        {
            Assert.That(GridInitializationPass.PassId, Is.EqualTo("PASS_GRID"));
            Assert.That(GridInitializationPass.OutputArtifactId, Is.EqualTo("GRID"));
            Assert.That(typeof(GridInitializationPass).IsSealed, Is.True);
            Assert.That(typeof(GridInitializationPass).GetConstructor(Type.EmptyTypes), Is.Not.Null);
            Assert.That(typeof(GridInitializationPass).GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic), Is.Empty);
        }

        [TestCase(0UL)]
        [TestCase(ulong.MaxValue)]
        public void Execute_StoresSeedAndCreatesExactCounts(ulong seed)
        {
            var result = new GridInitializationPass().Execute(seed);

            Assert.That(result.WorldData.Seed, Is.EqualTo(seed));
            Assert.That(result.WorldData.Cells.Count, Is.EqualTo(WorldGenConstants.SectorCount));
            Assert.That(result.Neighbors.Count, Is.EqualTo(WorldGenConstants.SectorCount));
        }

        [Test]
        public void Execute_CreatesCellsInYOuterXInnerOrder()
        {
            var result = new GridInitializationPass().Execute(1);

            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var expected = new SectorCoord(
                    index % WorldGenConstants.SectorColumns,
                    index / WorldGenConstants.SectorColumns);
                Assert.That(result.WorldData.Cells[index].Index, Is.EqualTo(index));
                Assert.That(result.WorldData.Cells[index].Coordinate, Is.EqualTo(expected));
            }
        }

        [Test]
        public void Execute_CreatesExactNeutralCellDefaults()
        {
            foreach (var cell in new GridInitializationPass().Execute(2).WorldData.Cells)
            {
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
        }

        [TestCase(0, -1, 1, 13, -1)]
        [TestCase(12, 11, -1, 25, -1)]
        [TestCase(84, 83, 85, 97, 71)]
        [TestCase(156, -1, 157, -1, 143)]
        [TestCase(168, 167, -1, -1, 155)]
        public void Execute_CreatesExactKnownNeighbors(
            int index,
            int left,
            int right,
            int up,
            int down)
        {
            var entry = new GridInitializationPass().Execute(3).GetNeighbors(index);

            Assert.That(entry.LeftIndex, Is.EqualTo(left));
            Assert.That(entry.RightIndex, Is.EqualTo(right));
            Assert.That(entry.UpIndex, Is.EqualTo(up));
            Assert.That(entry.DownIndex, Is.EqualTo(down));
        }

        [Test]
        public void Execute_CreatesExactNeighborFormulaForEveryCell()
        {
            var result = new GridInitializationPass().Execute(3);

            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var x = index % WorldGenConstants.SectorColumns;
                var y = index / WorldGenConstants.SectorColumns;
                var entry = result.GetNeighbors(index);
                Assert.That(entry.Index, Is.EqualTo(index));
                Assert.That(entry.LeftIndex, Is.EqualTo(x == 0 ? -1 : index - 1));
                Assert.That(entry.RightIndex, Is.EqualTo(
                    x == WorldGenConstants.SectorColumns - 1 ? -1 : index + 1));
                Assert.That(entry.UpIndex, Is.EqualTo(
                    y == WorldGenConstants.SectorRows - 1 ? -1 : index + WorldGenConstants.SectorColumns));
                Assert.That(entry.DownIndex, Is.EqualTo(
                    y == 0 ? -1 : index - WorldGenConstants.SectorColumns));
            }
        }

        [Test]
        public void Execute_CreatesExactGlobalTopologyCounts()
        {
            var entries = new GridInitializationPass().Execute(4).Neighbors;
            var cornerCount = 0;
            var boundaryNonCornerCount = 0;
            var interiorCount = 0;
            var directedEdges = 0;
            var undirectedEdges = new HashSet<string>(StringComparer.Ordinal);

            foreach (var entry in entries)
            {
                var coordinate = WorldGridIndex.ToCoordinate(entry.Index);
                var horizontalBoundary = coordinate.X == 0 || coordinate.X == WorldGenConstants.SectorColumns - 1;
                var verticalBoundary = coordinate.Y == 0 || coordinate.Y == WorldGenConstants.SectorRows - 1;
                if (horizontalBoundary && verticalBoundary)
                {
                    cornerCount++;
                    Assert.That(entry.ValidNeighborCount, Is.EqualTo(2));
                }
                else if (horizontalBoundary || verticalBoundary)
                {
                    boundaryNonCornerCount++;
                    Assert.That(entry.ValidNeighborCount, Is.EqualTo(3));
                }
                else
                {
                    interiorCount++;
                    Assert.That(entry.ValidNeighborCount, Is.EqualTo(4));
                }

                foreach (var neighbor in EnumerateValid(entry))
                {
                    directedEdges++;
                    undirectedEdges.Add(Math.Min(entry.Index, neighbor) + ":" + Math.Max(entry.Index, neighbor));
                }
            }

            Assert.That(cornerCount, Is.EqualTo(4));
            Assert.That(boundaryNonCornerCount, Is.EqualTo(44));
            Assert.That(interiorCount, Is.EqualTo(121));
            Assert.That(directedEdges, Is.EqualTo(624));
            Assert.That(undirectedEdges.Count, Is.EqualTo(312));
        }

        [Test]
        public void Execute_TopologyIsReciprocal()
        {
            var result = new GridInitializationPass().Execute(5);

            foreach (var entry in result.Neighbors)
            {
                if (entry.LeftIndex != SectorNeighborIndices.NoNeighbor)
                {
                    Assert.That(result.GetNeighbors(entry.LeftIndex).RightIndex, Is.EqualTo(entry.Index));
                }

                if (entry.RightIndex != SectorNeighborIndices.NoNeighbor)
                {
                    Assert.That(result.GetNeighbors(entry.RightIndex).LeftIndex, Is.EqualTo(entry.Index));
                }

                if (entry.UpIndex != SectorNeighborIndices.NoNeighbor)
                {
                    Assert.That(result.GetNeighbors(entry.UpIndex).DownIndex, Is.EqualTo(entry.Index));
                }

                if (entry.DownIndex != SectorNeighborIndices.NoNeighbor)
                {
                    Assert.That(result.GetNeighbors(entry.DownIndex).UpIndex, Is.EqualTo(entry.Index));
                }
            }
        }

        [Test]
        public void Execute_TopologyIsOneConnectedComponent()
        {
            var result = new GridInitializationPass().Execute(6);
            var visited = new HashSet<int> { 0 };
            var pending = new Queue<int>();
            pending.Enqueue(0);

            while (pending.Count > 0)
            {
                foreach (var neighbor in EnumerateValid(result.GetNeighbors(pending.Dequeue())))
                {
                    if (visited.Add(neighbor))
                    {
                        pending.Enqueue(neighbor);
                    }
                }
            }

            Assert.That(visited.Count, Is.EqualTo(WorldGenConstants.SectorCount));
        }

        [TestCase(0UL)]
        [TestCase(0x0123456789ABCDEFUL)]
        public void Execute_RepeatedCallsAreDeterministic(ulong seed)
        {
            var reusedPass = new GridInitializationPass();
            var expected = reusedPass.Execute(seed);
            var expectedBytes = GeneratedWorldDataCsvSerializer.Serialize(expected.WorldData);
            var expectedHash = ComputeSha256(expectedBytes);

            for (var iteration = 0; iteration < 100; iteration++)
            {
                var actual = iteration % 2 == 0
                    ? reusedPass.Execute(seed)
                    : new GridInitializationPass().Execute(seed);
                var actualBytes = GeneratedWorldDataCsvSerializer.Serialize(actual.WorldData);

                CollectionAssert.AreEqual(expectedBytes, actualBytes);
                CollectionAssert.AreEqual(expectedHash, ComputeSha256(actualBytes));
                AssertNeighborSequencesEqual(expected.Neighbors, actual.Neighbors);
            }
        }

        [Test]
        public void Execute_DifferentSeedOnlyChangesStoredSeed()
        {
            var first = new GridInitializationPass().Execute(10);
            var second = new GridInitializationPass().Execute(11);

            Assert.That(first.WorldData.Seed, Is.Not.EqualTo(second.WorldData.Seed));
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                AssertCellsEqualExceptSeed(first.WorldData.Cells[index], second.WorldData.Cells[index]);
            }

            AssertNeighborSequencesEqual(first.Neighbors, second.Neighbors);
        }

        [Test]
        public void Result_RejectsNullWorldData()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GridInitializationResult(null, CreateNeighbors()));
        }

        [Test]
        public void Result_RejectsNullNeighborCollection()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new GridInitializationResult(CreateWorld(), null));
        }

        [TestCase(-1)]
        [TestCase(1)]
        public void Result_RejectsNeighborCountMismatch(int delta)
        {
            var neighbors = CreateNeighbors();
            if (delta < 0)
            {
                neighbors.RemoveAt(neighbors.Count - 1);
            }
            else
            {
                neighbors.Add(CreateNeighbor(0));
            }

            Assert.Throws<ArgumentException>(() =>
                new GridInitializationResult(CreateWorld(), neighbors));
        }

        [Test]
        public void Result_RejectsNullNeighborEntry()
        {
            var neighbors = CreateNeighbors();
            neighbors[84] = null;

            Assert.Throws<ArgumentException>(() =>
                new GridInitializationResult(CreateWorld(), neighbors));
        }

        [Test]
        public void Result_RejectsDuplicateAndMissingNeighborIndex()
        {
            var neighbors = CreateNeighbors();
            neighbors[84] = CreateNeighbor(83);

            Assert.Throws<ArgumentException>(() =>
                new GridInitializationResult(CreateWorld(), neighbors));
        }

        [Test]
        public void Result_AcceptsCallerOrderIndependentNeighborsAndSortsThem()
        {
            var neighbors = CreateNeighbors();
            neighbors.Reverse();

            var result = new GridInitializationResult(CreateWorld(), neighbors);

            CollectionAssert.AreEqual(
                Enumerable.Range(0, WorldGenConstants.SectorCount),
                result.Neighbors.Select(entry => entry.Index));
        }

        [Test]
        public void Result_SnapshotsCallerNeighborCollection()
        {
            var neighbors = CreateNeighbors();
            var original = neighbors[0];
            var result = new GridInitializationResult(CreateWorld(), neighbors);

            neighbors[0] = CreateNeighbor(1);
            neighbors.Clear();

            Assert.That(result.GetNeighbors(0), Is.SameAs(original));
            Assert.That(result.Neighbors.Count, Is.EqualTo(WorldGenConstants.SectorCount));
        }

        [Test]
        public void Result_ExposesReadOnlyNeighbors()
        {
            var result = new GridInitializationResult(CreateWorld(), CreateNeighbors());
            var collection = (ICollection<SectorNeighborIndices>)result.Neighbors;

            Assert.That(collection.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => collection.Add(CreateNeighbor(0)));
        }

        [TestCase(0, 0)]
        [TestCase(12, 0)]
        [TestCase(6, 6)]
        [TestCase(0, 12)]
        [TestCase(12, 12)]
        public void Result_ProvidesStableIndexAndCoordinateLookup(int x, int y)
        {
            var result = new GridInitializationResult(CreateWorld(), CreateNeighbors());
            var coordinate = new SectorCoord(x, y);
            var index = y * WorldGenConstants.SectorColumns + x;

            Assert.That(result.GetNeighbors(index), Is.SameAs(result.GetNeighbors(coordinate)));
            Assert.That(result.TryGetNeighbors(index, out var entry), Is.True);
            Assert.That(entry, Is.SameAs(result.GetNeighbors(index)));
        }

        [TestCase(-1)]
        [TestCase(WorldGenConstants.SectorCount)]
        public void Result_InvalidIndexLookupsDoNotResolve(int index)
        {
            var result = new GridInitializationResult(CreateWorld(), CreateNeighbors());

            Assert.That(result.TryGetNeighbors(index, out var entry), Is.False);
            Assert.That(entry, Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() => result.GetNeighbors(index));
        }

        [TestCase(-1, 0)]
        [TestCase(0, WorldGenConstants.SectorRows)]
        public void Result_InvalidCoordinateLookupThrows(int x, int y)
        {
            var result = new GridInitializationResult(CreateWorld(), CreateNeighbors());

            Assert.Throws<ArgumentOutOfRangeException>(() => result.GetNeighbors(new SectorCoord(x, y)));
        }

        [Test]
        public void Result_RejectsWorldWhoseIndicesDoNotMatchCoordinates()
        {
            Assert.Throws<ArgumentException>(() =>
                new GridInitializationResult(CreateWorld(true), CreateNeighbors()));
        }

        [Test]
        public void Result_RejectsTopologyThatDoesNotMatchGrid()
        {
            var neighbors = CreateNeighbors();
            neighbors[84] = new SectorNeighborIndices(84, 82, 85, 97, 71);

            Assert.Throws<ArgumentException>(() =>
                new GridInitializationResult(CreateWorld(), neighbors));
        }

        [Test]
        public void Result_IsSealedAndReadOnly()
        {
            var type = typeof(GridInitializationResult);

            Assert.That(type.IsSealed, Is.True);
            Assert.That(type.GetFields(BindingFlags.Instance | BindingFlags.Public), Is.Empty);
            Assert.That(type.GetProperties().All(property => property.SetMethod == null), Is.True);
        }

        [Test]
        public void GridRuntimeSurface_HasNoUnityFileSystemOrRngDependency()
        {
            var types = new[]
            {
                typeof(WorldGridIndex),
                typeof(SectorNeighborIndices),
                typeof(GridInitializationResult),
                typeof(GridInitializationPass)
            };
            var surface = types
                .SelectMany(type => type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                .Select(member => member.ToString())
                .ToArray();

            Assert.That(surface.Any(value => value.Contains("UnityEditor")), Is.False);
            Assert.That(surface.Any(value => value.Contains("UnityEngine")), Is.False);
            Assert.That(surface.Any(value => value.Contains("System.IO")), Is.False);
            Assert.That(surface.Any(value => value.Contains(nameof(DeterministicRngStream))), Is.False);
            Assert.That(surface.Any(value => value.Contains(nameof(WorldGenerationRngStreams))), Is.False);
        }

        private static SectorNeighborIndices CreateWithSlot(string position, int value)
        {
            var left = 83;
            var right = 85;
            var up = 97;
            var down = 71;
            switch (position)
            {
                case "left": left = value; break;
                case "right": right = value; break;
                case "up": up = value; break;
                case "down": down = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(position));
            }

            return new SectorNeighborIndices(84, left, right, up, down);
        }

        private static GeneratedWorldData CreateWorld(bool reverseCoordinates = false)
        {
            var cells = new List<SectorCell>(WorldGenConstants.SectorCount);
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var coordinateIndex = reverseCoordinates
                    ? WorldGenConstants.SectorCount - 1 - index
                    : index;
                cells.Add(SectorCell.CreateUnassigned(
                    index,
                    new SectorCoord(
                        coordinateIndex % WorldGenConstants.SectorColumns,
                        coordinateIndex / WorldGenConstants.SectorColumns)));
            }

            return new GeneratedWorldData(123, cells);
        }

        private static List<SectorNeighborIndices> CreateNeighbors()
        {
            var entries = new List<SectorNeighborIndices>(WorldGenConstants.SectorCount);
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                entries.Add(CreateNeighbor(index));
            }

            return entries;
        }

        private static SectorNeighborIndices CreateNeighbor(int index)
        {
            var x = index % WorldGenConstants.SectorColumns;
            var y = index / WorldGenConstants.SectorColumns;
            return new SectorNeighborIndices(
                index,
                x == 0 ? -1 : index - 1,
                x == WorldGenConstants.SectorColumns - 1 ? -1 : index + 1,
                y == WorldGenConstants.SectorRows - 1 ? -1 : index + WorldGenConstants.SectorColumns,
                y == 0 ? -1 : index - WorldGenConstants.SectorColumns);
        }

        private static IEnumerable<int> EnumerateValid(SectorNeighborIndices entry)
        {
            var values = new[] { entry.LeftIndex, entry.RightIndex, entry.UpIndex, entry.DownIndex };
            return values.Where(value => value != SectorNeighborIndices.NoNeighbor);
        }

        private static void AssertNeighborSequencesEqual(
            IReadOnlyList<SectorNeighborIndices> first,
            IReadOnlyList<SectorNeighborIndices> second)
        {
            Assert.That(first.Count, Is.EqualTo(second.Count));
            for (var index = 0; index < first.Count; index++)
            {
                Assert.That(first[index].Index, Is.EqualTo(second[index].Index));
                Assert.That(first[index].LeftIndex, Is.EqualTo(second[index].LeftIndex));
                Assert.That(first[index].RightIndex, Is.EqualTo(second[index].RightIndex));
                Assert.That(first[index].UpIndex, Is.EqualTo(second[index].UpIndex));
                Assert.That(first[index].DownIndex, Is.EqualTo(second[index].DownIndex));
            }
        }

        private static void AssertCellsEqualExceptSeed(SectorCell first, SectorCell second)
        {
            Assert.That(first.Index, Is.EqualTo(second.Index));
            Assert.That(first.Coordinate, Is.EqualTo(second.Coordinate));
            Assert.That(first.Role, Is.EqualTo(second.Role));
            Assert.That(first.PrimaryBiomeId, Is.EqualTo(second.PrimaryBiomeId));
            Assert.That(first.SecondaryBiomeId, Is.EqualTo(second.SecondaryBiomeId));
            Assert.That(first.PatchId, Is.EqualTo(second.PatchId));
            Assert.That(first.RouteMaskId, Is.EqualTo(second.RouteMaskId));
            Assert.That(first.SpecialSiteInstanceId, Is.EqualTo(second.SpecialSiteInstanceId));
            Assert.That(first.BoundaryProfileId, Is.EqualTo(second.BoundaryProfileId));
            Assert.That(first.SectorRecipeId, Is.EqualTo(second.SectorRecipeId));
            Assert.That(first.ReservationId, Is.EqualTo(second.ReservationId));
            Assert.That(first.ShortestDistanceFromStart, Is.EqualTo(second.ShortestDistanceFromStart));
            Assert.That(first.MandatoryGraphNode, Is.EqualTo(second.MandatoryGraphNode));
        }

        private static byte[] ComputeSha256(byte[] value)
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(value);
            }
        }
    }
}
