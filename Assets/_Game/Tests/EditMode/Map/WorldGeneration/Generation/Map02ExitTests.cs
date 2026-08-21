using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Diagnostics;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using UnityEngine;

namespace StarNight.Map.Tests.WorldGeneration.Generation
{
    public sealed class Map02ExitTests
    {
        private const string GenerationProfileId = "GEN_MOONPALACE_V1";
        private const string WorldProfileId = "WORLD_MOONPALACE_V1";
        private const string BuildId = "MAP02_EXIT_TEST";
        private const ulong KnownWorldSeed = 0x0123456789ABCDEFUL;
        private const string StaticSectorHash =
            "94ea893d55e80e4ec0a5a4758b7d84bd8e999942064d3205600e0f5a8a1bd13b";
        private static readonly DateTimeOffset StartUtc =
            new DateTimeOffset(2026, 8, 12, 0, 0, 0, TimeSpan.Zero);

        [Test]
        public void FrozenWorldDimensionsAreExact()
        {
            Assert.That(WorldGenConstants.WorldWidthTiles, Is.EqualTo(624));
            Assert.That(WorldGenConstants.WorldHeightTiles, Is.EqualTo(416));
            Assert.That(WorldGenConstants.SectorWidthTiles, Is.EqualTo(48));
            Assert.That(WorldGenConstants.SectorHeightTiles, Is.EqualTo(32));
            Assert.That(WorldGenConstants.SectorColumns, Is.EqualTo(13));
            Assert.That(WorldGenConstants.SectorRows, Is.EqualTo(13));
            Assert.That(WorldGenConstants.SectorCount, Is.EqualTo(169));
        }

        [TestCase(0, 0, 0)]
        [TestCase(12, 0, 12)]
        [TestCase(6, 6, 84)]
        [TestCase(0, 12, 156)]
        [TestCase(12, 12, 168)]
        public void RowMajorIndexMappingIsExact(int x, int y, int expectedIndex)
        {
            var coordinate = new SectorCoord(x, y);
            Assert.That(WorldGridIndex.ToIndex(coordinate), Is.EqualTo(expectedIndex));
            Assert.That(WorldGridIndex.ToCoordinate(expectedIndex), Is.EqualTo(coordinate));
        }

        [Test]
        public void GridContainsEveryExactNeutralCellOnce()
        {
            var result = Grid(4660);
            var indices = new HashSet<int>();
            var coordinates = new HashSet<SectorCoord>();
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var cell = result.WorldData.Cells[index];
                Assert.That(indices.Add(cell.Index), Is.True, "duplicate index " + cell.Index);
                Assert.That(coordinates.Add(cell.Coordinate), Is.True, "duplicate coordinate " + cell.Coordinate);
                Assert.That(cell.Index, Is.EqualTo(index));
                Assert.That(cell.Coordinate, Is.EqualTo(new SectorCoord(index % 13, index / 13)));
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

            Assert.That(indices, Is.EquivalentTo(Enumerable.Range(0, 169)));
            Assert.That(coordinates.Count, Is.EqualTo(169));
        }

        [TestCase(0UL)]
        [TestCase(4660UL)]
        [TestCase(ulong.MaxValue)]
        public void GridPreservesSeedAndExactRowMajorOrientation(ulong seed)
        {
            var result = Grid(seed);
            Assert.That(result.WorldData.Seed, Is.EqualTo(seed));
            Assert.That(result.WorldData.Cells[0].Coordinate, Is.EqualTo(new SectorCoord(0, 0)));
            Assert.That(result.WorldData.Cells[156].Coordinate, Is.EqualTo(new SectorCoord(0, 12)));
            Assert.That(result.WorldData.Cells[168].Coordinate, Is.EqualTo(new SectorCoord(12, 12)));
        }

        [TestCase(0, -1, 1, 13, -1)]
        [TestCase(12, 11, -1, 25, -1)]
        [TestCase(84, 83, 85, 97, 71)]
        [TestCase(156, -1, 157, -1, 143)]
        [TestCase(168, 167, -1, -1, 155)]
        public void KnownNeighborTuplesAreExact(
            int index,
            int left,
            int right,
            int up,
            int down)
        {
            var neighbors = Grid(0).GetNeighbors(index);
            Assert.That(neighbors.LeftIndex, Is.EqualTo(left));
            Assert.That(neighbors.RightIndex, Is.EqualTo(right));
            Assert.That(neighbors.UpIndex, Is.EqualTo(up));
            Assert.That(neighbors.DownIndex, Is.EqualTo(down));
        }

        [Test]
        public void NeighborDegreeCategoriesAreExact()
        {
            var result = Grid(0);
            Assert.That(result.Neighbors.Count(item => item.ValidNeighborCount == 2), Is.EqualTo(4));
            Assert.That(result.Neighbors.Count(item => item.ValidNeighborCount == 3), Is.EqualTo(44));
            Assert.That(result.Neighbors.Count(item => item.ValidNeighborCount == 4), Is.EqualTo(121));
        }

        [Test]
        public void NeighborLinksAreReciprocalWithExactDirectedAndUndirectedCounts()
        {
            var result = Grid(0);
            var directed = 0;
            var undirected = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < 169; index++)
            {
                foreach (var neighbor in NeighborValues(result.GetNeighbors(index)))
                {
                    if (neighbor == SectorNeighborIndices.NoNeighbor)
                    {
                        continue;
                    }

                    directed++;
                    var low = Math.Min(index, neighbor);
                    var high = Math.Max(index, neighbor);
                    undirected.Add(low.ToString(CultureInfo.InvariantCulture) + ":" +
                                   high.ToString(CultureInfo.InvariantCulture));
                    Assert.That(NeighborValues(result.GetNeighbors(neighbor)), Does.Contain(index));
                }
            }

            Assert.That(directed, Is.EqualTo(624));
            Assert.That(undirected.Count, Is.EqualTo(312));
        }

        [Test]
        public void GridIsOneConnectedComponent()
        {
            var result = Grid(0);
            var visited = new HashSet<int> { 0 };
            var pending = new Queue<int>();
            pending.Enqueue(0);
            while (pending.Count > 0)
            {
                foreach (var neighbor in NeighborValues(result.GetNeighbors(pending.Dequeue())))
                {
                    if (neighbor != SectorNeighborIndices.NoNeighbor && visited.Add(neighbor))
                    {
                        pending.Enqueue(neighbor);
                    }
                }
            }

            Assert.That(visited.Count, Is.EqualTo(169));
        }

        [Test]
        public void GridHasNoWrapDiagonalSelfOrDuplicateNeighbors()
        {
            var result = Grid(0);
            for (var index = 0; index < 169; index++)
            {
                var origin = WorldGridIndex.ToCoordinate(index);
                var valid = NeighborValues(result.GetNeighbors(index))
                    .Where(value => value != SectorNeighborIndices.NoNeighbor)
                    .ToArray();
                Assert.That(valid.Distinct().Count(), Is.EqualTo(valid.Length));
                foreach (var neighbor in valid)
                {
                    Assert.That(neighbor, Is.Not.EqualTo(index));
                    var target = WorldGridIndex.ToCoordinate(neighbor);
                    Assert.That(Math.Abs(target.X - origin.X) + Math.Abs(target.Y - origin.Y), Is.EqualTo(1));
                }
            }
        }

        [TestCase(0UL)]
        [TestCase(4660UL)]
        [TestCase(ulong.MaxValue)]
        public void SectorCsvHasExactEnvelopeForBoundarySeeds(ulong seed)
        {
            var bytes = SectorBytes(seed);
            Assert.That(bytes.Take(3), Is.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }));
            var text = Utf8Text(bytes);
            Assert.That(text.Contains("\n") && !text.Replace("\r\n", string.Empty).Contains("\n"), Is.True);
            Assert.That(text.EndsWith("\r\n", StringComparison.Ordinal), Is.True);
            var rows = text.Split(new[] { "\r\n" }, StringSplitOptions.None);
            Assert.That(rows.Length, Is.EqualTo(171));
            Assert.That(rows[0], Is.EqualTo(GeneratedWorldDataCsvSerializer.Header));
            Assert.That(rows.Skip(1).Take(169).All(row => row.Split(',').Length == 13), Is.True);
            Assert.That(rows[170], Is.Empty);
        }

        [Test]
        public void SectorCsvHeaderPrefixIsExact()
        {
            var prefix = SectorBytes(0).Take(210).ToArray();
            Assert.That(prefix.Length, Is.EqualTo(210));
            Assert.That(Sha256(prefix), Is.EqualTo(
                "0721cfa4acb6bfb2d85e04ee295960a63844e4c5c72648f9e9cdb5d260aebf59"));
        }

        [Test]
        public void SectorCsvRowsAreExactNeutralRowMajorData()
        {
            var rows = Utf8Text(SectorBytes(4660))
                .Split(new[] { "\r\n" }, StringSplitOptions.None);
            for (var index = 0; index < 169; index++)
            {
                var fields = rows[index + 1].Split(',');
                Assert.That(fields[0], Is.EqualTo("4660"));
                Assert.That(fields[1], Is.EqualTo((index % 13).ToString(CultureInfo.InvariantCulture)));
                Assert.That(fields[2], Is.EqualTo((index / 13).ToString(CultureInfo.InvariantCulture)));
                Assert.That(fields[3], Is.EqualTo("UNASSIGNED"));
                Assert.That(fields.Skip(4).Take(7).All(value => value.Length == 0), Is.True);
                Assert.That(fields[11], Is.EqualTo("-1"));
                Assert.That(fields[12], Is.EqualTo("0"));
            }
        }

        [Test]
        public void SectorCsvStaticSampleHasExactIdentity()
        {
            var bytes = SectorBytes(4660);
            Assert.That(bytes.Length, Is.EqualTo(5865));
            Assert.That(Sha256(bytes), Is.EqualTo(StaticSectorHash));
        }

        [TestCase("RNG_WORLD_SITE", "", "60D4B46EBF6EF00D", "F627BD56683B33FC", "4CA318D8E4EA97BA")]
        [TestCase("RNG_BIOME_PATCH", "PASS_BIOME", "98BC23250806566B", "D2E329C4A736E686", "F63F41F61CC1B52C")]
        [TestCase("RNG_ROUTE", "PASS_ROUTE", "8EDC9EB9BA0977DC", "CA6E229CF519975D", "2289076DA3C2FFE2")]
        [TestCase("RNG_TYPE0", "PASS_TYPE0", "570969677634D631", "3F79615689D9D77E", "8A8D7006920CD2E8")]
        [TestCase("RNG_SECTOR_RECIPE", "6,6", "08D7C54EF3F843DE", "612FB5C8F12DDB0A", "DD0D4A17DDF66EA1")]
        [TestCase("RNG_POPULATION", "6,6", "36D00A33DAED7549", "472FBC58241A8307", "93591B6C5B950D32")]
        public void RequiredRngKnownVectorsAreExact(
            string streamId,
            string identity,
            string initial,
            string first,
            string second)
        {
            var stream = CreateRequiredStream(RngStreams(), streamId, KnownWorldSeed, identity);
            Assert.That(stream.InitialState, Is.EqualTo(ParseHex(initial)));
            Assert.That(stream.NextUInt64(), Is.EqualTo(ParseHex(first)));
            Assert.That(stream.NextUInt64(), Is.EqualTo(ParseHex(second)));
        }

        [Test]
        public void RequiredRngCatalogHasExactSixScopes()
        {
            Assert.That(WorldGenerationRngStreams.RequiredCatalog.Count, Is.EqualTo(6));
            Assert.That(WorldGenerationRngStreams.RequiredCatalog["RNG_WORLD_SITE"], Is.EqualTo(RngResetScope.World));
            Assert.That(WorldGenerationRngStreams.RequiredCatalog["RNG_BIOME_PATCH"], Is.EqualTo(RngResetScope.Pass));
            Assert.That(WorldGenerationRngStreams.RequiredCatalog["RNG_ROUTE"], Is.EqualTo(RngResetScope.Pass));
            Assert.That(WorldGenerationRngStreams.RequiredCatalog["RNG_TYPE0"], Is.EqualTo(RngResetScope.Pass));
            Assert.That(WorldGenerationRngStreams.RequiredCatalog["RNG_SECTOR_RECIPE"], Is.EqualTo(RngResetScope.Sector));
            Assert.That(WorldGenerationRngStreams.RequiredCatalog["RNG_POPULATION"], Is.EqualTo(RngResetScope.Spawn));
        }

        [Test]
        public void RngCreationOrderDoesNotChangeAnyRequiredSequence()
        {
            var forward = CreateRequiredStreams(RngStreams(), 91, false);
            var reverse = CreateRequiredStreams(RngStreams(), 91, true);
            foreach (var id in forward.Keys)
            {
                Assert.That(Draw(forward[id], 8), Is.EqualTo(Draw(reverse[id], 8)), id);
            }
        }

        [TestCase("RNG_WORLD_SITE")]
        [TestCase("RNG_BIOME_PATCH")]
        [TestCase("RNG_ROUTE")]
        [TestCase("RNG_TYPE0")]
        [TestCase("RNG_SECTOR_RECIPE")]
        [TestCase("RNG_POPULATION")]
        public void ExtraDrawsOnOneRngStreamDoNotAlterTheOtherFive(string extraStreamId)
        {
            var baseline = CreateRequiredStreams(RngStreams(), 92, false);
            var changed = CreateRequiredStreams(RngStreams(), 92, true);
            Draw(changed[extraStreamId], 100);
            foreach (var id in baseline.Keys.Where(id => id != extraStreamId))
            {
                Assert.That(Draw(changed[id], 8), Is.EqualTo(Draw(baseline[id], 8)), id);
            }
        }

        [Test]
        public void RngConsumptionCannotChangeGridOutput()
        {
            var expected = SectorBytes(4660);
            var streams = CreateRequiredStreams(RngStreams(), 4660, false);
            foreach (var stream in streams.Values)
            {
                Draw(stream, 100);
            }

            Assert.That(SectorBytes(4660), Is.EqualTo(expected));
        }

        [TestCase(0UL)]
        [TestCase(4660UL)]
        [TestCase(ulong.MaxValue)]
        public void RecordedRootCheckpointHasExactIdentity(ulong seed)
        {
            var execution = CreateRoot().ExecuteThroughRecorded(
                GenerationProfileId, seed, GridInitializationPass.PassId);
            Assert.That(execution.Result.Succeeded, Is.True);
            Assert.That(execution.Result.LastCompletedPassId, Is.EqualTo(GridInitializationPass.PassId));
            Assert.That(execution.Result.Artifacts.ArtifactIds, Is.EqualTo(new[] { GridInitializationPass.OutputArtifactId }));
            Assert.That(execution.Result.Artifacts.Get<GridInitializationResult>(
                GridInitializationPass.OutputArtifactId).WorldData.Seed, Is.EqualTo(seed));
            Assert.That(execution.ExecutionRecord.PassCount, Is.EqualTo(1));
            Assert.That(execution.ExecutionRecord.AttemptCount, Is.EqualTo(1));
            Assert.That(execution.ExecutionRecord.RetryCountTotal, Is.Zero);
            Assert.That(execution.ExecutionRecord.InclusivePassId, Is.EqualTo(GridInitializationPass.PassId));
            Assert.That(execution.ExecutionRecord.WorldSeed, Is.EqualTo(seed));
        }

        [Test]
        public void ExecuteThroughProjectionInvokesPassExactlyOnce()
        {
            var pass = new CountingGridPass();
            var result = CreateRoot(pass).ExecuteThrough(
                GenerationProfileId, 4660, GridInitializationPass.PassId);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(pass.InvocationCount, Is.EqualTo(1));
        }

        [Test]
        public void FailingPassRecordsStableDeterministicIdentity()
        {
            var firstPass = new CountingGridPass(true);
            var secondPass = new CountingGridPass(true);
            var first = CreateRoot(firstPass).ExecuteThroughRecorded(
                GenerationProfileId, 7, GridInitializationPass.PassId);
            var second = CreateRoot(secondPass).ExecuteThroughRecorded(
                GenerationProfileId, 7, GridInitializationPass.PassId);
            Assert.That(first.Result.Succeeded, Is.False);
            Assert.That(first.ExecutionRecord.FailurePassId, Is.EqualTo(GridInitializationPass.PassId));
            Assert.That(first.ExecutionRecord.FailureCode, Is.EqualTo("PASS_FAILED"));
            Assert.That(first.ExecutionRecord.FailureMessage, Is.EqualTo("expected failure"));
            Assert.That(first.ExecutionRecord.PassCount, Is.EqualTo(1));
            Assert.That(first.ExecutionRecord.AttemptCount, Is.EqualTo(1));
            Assert.That(first.ExecutionRecord.Passes[0].Attempts[0].FailureCode, Is.EqualTo("EXPECTED_FAILURE"));
            Assert.That(second.ExecutionRecord.FailurePassId, Is.EqualTo(first.ExecutionRecord.FailurePassId));
            Assert.That(second.ExecutionRecord.FailureCode, Is.EqualTo(first.ExecutionRecord.FailureCode));
            Assert.That(second.ExecutionRecord.FailureMessage, Is.EqualTo(first.ExecutionRecord.FailureMessage));
            Assert.That(firstPass.InvocationCount, Is.EqualTo(1));
            Assert.That(secondPass.InvocationCount, Is.EqualTo(1));
        }

        [Test]
        public void RootPlanPrevalidationInvokesNoPass()
        {
            var pass = new CountingGridPass();
            var execution = CreateRoot(pass, definitionClassName: "WrongClass")
                .ExecuteThroughRecorded(GenerationProfileId, 9, GridInitializationPass.PassId);
            Assert.That(execution.Result.Succeeded, Is.False);
            Assert.That(execution.ExecutionRecord.PassCount, Is.Zero);
            Assert.That(execution.ExecutionRecord.AttemptCount, Is.Zero);
            Assert.That(execution.ExecutionRecord.RetryCountTotal, Is.Zero);
            Assert.That(pass.InvocationCount, Is.Zero);
        }

        [Test]
        public void ClockSchedulesChangeOnlyDiagnosticTiming()
        {
            var first = CreateRoot(clock: new ManualClock(StartUtc, TimeSpan.FromMilliseconds(1)))
                .ExecuteThroughRecorded(GenerationProfileId, 4660, GridInitializationPass.PassId);
            var second = CreateRoot(clock: new ManualClock(StartUtc.AddDays(1), TimeSpan.FromMilliseconds(17)))
                .ExecuteThroughRecorded(GenerationProfileId, 4660, GridInitializationPass.PassId);
            var firstBytes = GeneratedWorldDataCsvSerializer.Serialize(
                first.Result.Artifacts.Get<GridInitializationResult>(GridInitializationPass.OutputArtifactId).WorldData);
            var secondBytes = GeneratedWorldDataCsvSerializer.Serialize(
                second.Result.Artifacts.Get<GridInitializationResult>(GridInitializationPass.OutputArtifactId).WorldData);
            Assert.That(secondBytes, Is.EqualTo(firstBytes));
            Assert.That(second.Result.Succeeded, Is.EqualTo(first.Result.Succeeded));
            Assert.That(second.ExecutionRecord.WorldSeed, Is.EqualTo(first.ExecutionRecord.WorldSeed));
            Assert.That(second.ExecutionRecord.Passes.Select(item => item.PassId),
                Is.EqualTo(first.ExecutionRecord.Passes.Select(item => item.PassId)));
            Assert.That(second.ExecutionRecord.DurationMilliseconds,
                Is.Not.EqualTo(first.ExecutionRecord.DurationMilliseconds));
            Assert.That(second.ExecutionRecord.StartedUtc, Is.Not.EqualTo(first.ExecutionRecord.StartedUtc));
        }

        [Test]
        public void RecorderProducesExactGridCheckpointManifest()
        {
            var execution = CreateRoot().ExecuteThroughRecorded(
                GenerationProfileId, 4660, GridInitializationPass.PassId);
            var bundle = new SeedReplayRecorder().Record(execution, Hash(), BuildId);
            var manifest = bundle.Manifest;
            Assert.That(manifest.WorldProfileId, Is.EqualTo(WorldProfileId));
            Assert.That(manifest.Seed, Is.EqualTo(4660));
            Assert.That(manifest.ContentVersionHash, Is.EqualTo(Hash().Hex));
            Assert.That(manifest.GenerationProfileId, Is.EqualTo(GenerationProfileId));
            Assert.That(manifest.GeneratorBuildId, Is.EqualTo(BuildId));
            Assert.That(manifest.Approved, Is.False);
            Assert.That(manifest.GenerationStartedUtc, Is.EqualTo(execution.ExecutionRecord.StartedUtc));
            Assert.That(manifest.GenerationDurationMilliseconds,
                Is.EqualTo(execution.ExecutionRecord.DurationMilliseconds));
            Assert.That(manifest.RetryCountTotal, Is.Zero);
            Assert.That(manifest.FailureRuleIds, Is.Empty);
            Assert.That(manifest.Notes, Is.EqualTo(SeedManifest.GridCheckpointNotes));
            var header = SeedManifestCsvSerializer.SerializeHeaderOnly();
            Assert.That(header.Length, Is.EqualTo(184));
            Assert.That(Sha256(header), Is.EqualTo(
                "fb45bfbb905f165b4702515484b97c83232fca9aa7bf775dd46cc52421761b0c"));
        }

        [Test]
        public void ReplayBundleHasExactTwoFileIdentityAndDirectory()
        {
            var bundle = Bundle(4660);
            Assert.That(bundle.FileNames, Is.EqualTo(new[]
            {
                "seed_manifest.csv",
                "generated_world_sectors.csv"
            }));
            Assert.That(bundle.RelativeDirectory,
                Is.EqualTo("GeneratedWorlds/WORLD_MOONPALACE_V1/0000000000004660"));
            Assert.That(Sha256(bundle.GeneratedWorldSectorsBytes), Is.EqualTo(StaticSectorHash));
        }

        [Test]
        public void RecorderConsumesExistingExecutionWithoutReexecutionOrFilesystem()
        {
            var pass = new CountingGridPass();
            var execution = CreateRoot(pass).ExecuteThroughRecorded(
                GenerationProfileId, 4660, GridInitializationPass.PassId);
            Assert.That(pass.InvocationCount, Is.EqualTo(1));
            var before = Directory.GetCurrentDirectory();
            var bundle = new SeedReplayRecorder().Record(execution, Hash(), BuildId);
            Assert.That(pass.InvocationCount, Is.EqualTo(1));
            Assert.That(Directory.GetCurrentDirectory(), Is.EqualTo(before));
            Assert.That(bundle.FileNames.Count, Is.EqualTo(2));
        }

        [Test]
        public void PublisherAtomicallyPublishesLoadsAndReplacesExactTwoFiles()
        {
            WithTempRoot(root =>
            {
                var publisher = new SeedReplayPublisher();
                var bundle = Bundle(4660);
                var published = publisher.Publish(root, bundle);
                var loaded = publisher.Load(root, WorldProfileId, 4660);
                var replaced = publisher.Publish(root, bundle);
                var destination = Path.Combine(
                    root,
                    bundle.RelativeDirectory.Replace('/', Path.DirectorySeparatorChar));
                var names = Directory.GetFiles(destination)
                    .Select(Path.GetFileName)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray();
                Assert.That(names, Is.EqualTo(new[]
                {
                    "generated_world_sectors.csv",
                    "seed_manifest.csv"
                }));
                Assert.That(Directory.GetDirectories(destination), Is.Empty);
                Assert.That(File.Exists(destination + ".staging"), Is.False);
                Assert.That(Directory.Exists(destination + ".staging"), Is.False);
                Assert.That(File.Exists(destination + ".backup"), Is.False);
                Assert.That(Directory.Exists(destination + ".backup"), Is.False);
                Assert.That(published.SeedManifestBytes, Is.EqualTo(bundle.SeedManifestBytes));
                Assert.That(loaded.GeneratedWorldSectorsBytes, Is.EqualTo(bundle.GeneratedWorldSectorsBytes));
                Assert.That(replaced.GeneratedWorldSectorsBytes, Is.EqualTo(bundle.GeneratedWorldSectorsBytes));
                Assert.That(new SeedReplayPlayer(CreateRoot()).Verify(loaded, Hash(), BuildId).Succeeded, Is.True);
            });
        }

        [Test]
        public void PlayerReplaysExactlyOnce()
        {
            var pass = new CountingGridPass();
            var result = new SeedReplayPlayer(CreateRoot(pass)).Verify(Bundle(4660), Hash(), BuildId);
            Assert.That(result.Succeeded, Is.True, result.Code + ": " + result.Message);
            Assert.That(pass.InvocationCount, Is.EqualTo(1));
        }

        [TestCase("hash")]
        [TestCase("build")]
        public void PlayerPreconditionFailuresInvokeNoPass(string mutation)
        {
            var pass = new CountingGridPass();
            var result = mutation == "hash"
                ? new SeedReplayPlayer(CreateRoot(pass)).Verify(Bundle(4660), Hash(1), BuildId)
                : new SeedReplayPlayer(CreateRoot(pass)).Verify(Bundle(4660), Hash(), BuildId + "_OTHER");
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Code, Is.EqualTo(mutation == "hash"
                ? SeedReplayVerificationResult.ContentHashMismatchCode
                : SeedReplayVerificationResult.GeneratorBuildMismatchCode));
            Assert.That(pass.InvocationCount, Is.Zero);
        }

        [Test]
        public void ReplayIgnoresTimingButPreservesStaticArtifactIdentity()
        {
            var bundle = Bundle(
                4660,
                new ManualClock(StartUtc, TimeSpan.FromMilliseconds(1)));
            var pass = new CountingGridPass();
            var player = new SeedReplayPlayer(CreateRoot(
                pass,
                new ManualClock(StartUtc.AddYears(1), TimeSpan.FromMilliseconds(31))));
            var result = player.Verify(bundle, Hash(), BuildId);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(pass.InvocationCount, Is.EqualTo(1));
            Assert.That(Sha256(bundle.GeneratedWorldSectorsBytes), Is.EqualTo(StaticSectorHash));
        }

        [Test]
        public void StaticSectorIdentitySurvivesOneHundredMixedFreshAndReusedRuns()
        {
            var reusedGrid = new GridInitializationPass();
            var reusedRoot = CreateRoot(clock: new ManualClock());
            var reusedPlayer = new SeedReplayPlayer(CreateRoot(clock: new ManualClock(
                StartUtc.AddDays(2), TimeSpan.FromMilliseconds(7))));
            for (var iteration = 0; iteration < 100; iteration++)
            {
                var grid = iteration % 2 == 0 ? reusedGrid.Execute(4660) : new GridInitializationPass().Execute(4660);
                var bytes = GeneratedWorldDataCsvSerializer.Serialize(grid.WorldData);
                Assert.That(Sha256(bytes), Is.EqualTo(StaticSectorHash), "grid " + iteration);

                var forward = CreateRequiredStreams(RngStreams(), 4660, false);
                var reverse = CreateRequiredStreams(RngStreams(), 4660, true);
                foreach (var id in forward.Keys)
                {
                    Assert.That(forward[id].NextUInt64(), Is.EqualTo(reverse[id].NextUInt64()),
                        "rng " + id + " / " + iteration);
                }

                var root = iteration % 2 == 0 ? reusedRoot : CreateRoot(clock: new ManualClock());
                var execution = root.ExecuteThroughRecorded(
                    GenerationProfileId, 4660, GridInitializationPass.PassId);
                var bundle = new SeedReplayRecorder().Record(execution, Hash(), BuildId);
                Assert.That(Sha256(bundle.GeneratedWorldSectorsBytes), Is.EqualTo(StaticSectorHash),
                    "record " + iteration);
                var player = iteration % 2 == 0
                    ? reusedPlayer
                    : new SeedReplayPlayer(CreateRoot(clock: new ManualClock()));
                var verification = player.Verify(bundle, Hash(), BuildId);
                Assert.That(verification.Succeeded, Is.True,
                    "replay " + iteration + " / " + verification.Code);
            }
        }

        [TestCase("en-US")]
        [TestCase("tr-TR")]
        public void StaticGenerationIdentityIsCultureInvariant(string cultureName)
        {
            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                var culture = CultureInfo.GetCultureInfo(cultureName);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                Assert.That(Sha256(SectorBytes(4660)), Is.EqualTo(StaticSectorHash));
                var route = RngStreams().CreateRoute(KnownWorldSeed, "PASS_ROUTE");
                Assert.That(route.InitialState, Is.EqualTo(ParseHex("8EDC9EB9BA0977DC")));
                Assert.That(route.NextUInt64(), Is.EqualTo(ParseHex("CA6E229CF519975D")));
                var bundle = Bundle(4660);
                Assert.That(bundle.RelativeDirectory,
                    Is.EqualTo("GeneratedWorlds/WORLD_MOONPALACE_V1/0000000000004660"));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }

        [Test]
        public void SourceCollectionsRecordsBundleBytesAndOverlaySnapshotsAreIsolated()
        {
            var sourceCells = Grid(4660).WorldData.Cells.ToList();
            var world = new GeneratedWorldData(4660, sourceCells);
            sourceCells.Clear();
            Assert.That(world.Cells.Count, Is.EqualTo(169));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<SectorCell>)world.Cells).Add(world.Cells[0]));

            var execution = CreateRoot().ExecuteThroughRecorded(
                GenerationProfileId, 4660, GridInitializationPass.PassId);
            Assert.Throws<NotSupportedException>(() =>
                ((IList<WorldGenerationPassExecutionRecord>)execution.ExecutionRecord.Passes)
                .Add(execution.ExecutionRecord.Passes[0]));

            var bundle = new SeedReplayRecorder().Record(execution, Hash(), BuildId);
            var firstBytes = bundle.GeneratedWorldSectorsBytes;
            firstBytes[0] = 0;
            Assert.That(bundle.GeneratedWorldSectorsBytes[0], Is.EqualTo(0xEF));

            var snapshot = WorldTopologyOverlaySnapshot.Create(Grid(4660));
            Assert.Throws<NotSupportedException>(() =>
                ((IList<WorldTopologyOverlayCell>)snapshot.Cells).Add(snapshot.Cells[0]));
            Assert.That(snapshot.Count, Is.EqualTo(169));
        }

        [Test]
        public void OverlaySnapshotCopiesExactGridIdentity()
        {
            var snapshot = WorldTopologyOverlaySnapshot.Create(Grid(4660));
            Assert.That(snapshot.Seed, Is.EqualTo(4660));
            Assert.That(snapshot.Count, Is.EqualTo(169));
            for (var index = 0; index < 169; index++)
            {
                var cell = snapshot.GetCell(index);
                Assert.That(cell.Index, Is.EqualTo(index));
                Assert.That(cell.Coordinate, Is.EqualTo(new SectorCoord(index % 13, index / 13)));
                Assert.That(cell.Role, Is.EqualTo(GeneratedSectorRole.Unassigned));
                Assert.That(cell.RoleToken, Is.EqualTo("UNASSIGNED"));
                Assert.That(cell.RoleGlyph, Is.EqualTo("U"));
            }
        }

        [TestCase(0, 24f, 428f)]
        [TestCase(84, 216f, 236f)]
        [TestCase(168, 408f, 44f)]
        public void OverlayRectsKeepDataYUpAndVisualTopDown(
            int index,
            float expectedX,
            float expectedY)
        {
            var rect = WorldTopologyOverlayGui.GetCellRect(index);
            Assert.That(rect.x, Is.EqualTo(expectedX));
            Assert.That(rect.y, Is.EqualTo(expectedY));
            Assert.That(rect.width, Is.EqualTo(32));
            Assert.That(rect.height, Is.EqualTo(32));
            Assert.That(WorldTopologyOverlayGui.PanelPixelWidth, Is.EqualTo(440));
            Assert.That(WorldTopologyOverlayGui.PanelPixelHeight, Is.EqualTo(564));
            Assert.That(WorldTopologyOverlayGui.GridPixelWidth, Is.EqualTo(416));
            Assert.That(WorldTopologyOverlayGui.GridPixelHeight, Is.EqualTo(416));
        }

        [TestCase(0)]
        [TestCase(84)]
        [TestCase(168)]
        public void OverlayTooltipsAreExactForRequiredCells(int index)
        {
            var cell = WorldTopologyOverlaySnapshot.Create(Grid(4660)).GetCell(index);
            var coordinate = WorldGridIndex.ToCoordinate(index);
            var neighbors = Grid(4660).GetNeighbors(index);
            var expected = string.Format(
                CultureInfo.InvariantCulture,
                "Sector: {0} / Index {1}\n" +
                "World Tiles: X {2}..{3} / Y {4}..{5}\n" +
                "Role: UNASSIGNED\n" +
                "Neighbors: L={6} R={7} U={8} D={9}",
                coordinate,
                index,
                coordinate.X * 48,
                coordinate.X * 48 + 47,
                coordinate.Y * 32,
                coordinate.Y * 32 + 31,
                neighbors.LeftIndex,
                neighbors.RightIndex,
                neighbors.UpIndex,
                neighbors.DownIndex);
            Assert.That(cell.Tooltip, Is.EqualTo(expected));
        }

        [TestCase(40f, 444f, true, 0)]
        [TestCase(232f, 252f, true, 84)]
        [TestCase(424f, 60f, true, 168)]
        [TestCase(24f, 44f, true, 156)]
        [TestCase(440f, 44f, false, -1)]
        public void OverlayHitTestUsesExactInclusiveExclusiveBounds(
            float x,
            float y,
            bool expectedHit,
            int expectedIndex)
        {
            var hit = WorldTopologyOverlayGui.TryHitTest(new Vector2(x, y), out var index);
            Assert.That(hit, Is.EqualTo(expectedHit));
            Assert.That(index, Is.EqualTo(expectedIndex));
        }

        [Test]
        public void OverlayComponentUsesOnlyExplicitTransientSnapshotState()
        {
            var gameObject = new GameObject("MAP02_08_Exit_Overlay")
            {
                hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
            };
            try
            {
                var overlay = gameObject.AddComponent<WorldTopologyOverlay>();
                Assert.That(overlay.HasSnapshot, Is.False);
                overlay.SetSnapshot(Grid(4660));
                Assert.That(overlay.HasSnapshot, Is.True);
                Assert.That(overlay.Snapshot.Seed, Is.EqualTo(4660));
                overlay.ClearSnapshot();
                Assert.That(overlay.HasSnapshot, Is.False);
                Assert.That(overlay.Snapshot, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static GridInitializationResult Grid(ulong seed)
        {
            return new GridInitializationPass().Execute(seed);
        }

        private static byte[] SectorBytes(ulong seed)
        {
            return GeneratedWorldDataCsvSerializer.Serialize(Grid(seed).WorldData);
        }

        private static int[] NeighborValues(SectorNeighborIndices neighbors)
        {
            return new[]
            {
                neighbors.LeftIndex,
                neighbors.RightIndex,
                neighbors.UpIndex,
                neighbors.DownIndex
            };
        }

        private static string Utf8Text(byte[] bytes)
        {
            return new UTF8Encoding(false, true).GetString(bytes, 3, bytes.Length - 3);
        }

        private static string Sha256(byte[] bytes)
        {
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(bytes))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }

        private static ulong ParseHex(string value)
        {
            return ulong.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        private static ulong[] Draw(DeterministicRngStream stream, int count)
        {
            var values = new ulong[count];
            for (var index = 0; index < count; index++)
            {
                values[index] = stream.NextUInt64();
            }

            return values;
        }

        private static WorldGenerationRngStreams RngStreams()
        {
            var definitions = new SortedDictionary<string, RngStreamDefinition>(StringComparer.Ordinal);
            foreach (var definition in RequiredRngDefinitions())
            {
                definitions.Add(definition.RngStreamId, definition);
            }

            var set = (WorldRouteDefinitionSet)FormatterServices.GetUninitializedObject(
                typeof(WorldRouteDefinitionSet));
            SetAutoProperty(
                set,
                "RngStreams",
                new ReadOnlyDictionary<string, RngStreamDefinition>(definitions));
            return new WorldGenerationRngStreams(set);
        }

        private static Dictionary<string, DeterministicRngStream> CreateRequiredStreams(
            WorldGenerationRngStreams streams,
            ulong seed,
            bool reverse)
        {
            var ids = new[]
            {
                "RNG_WORLD_SITE",
                "RNG_BIOME_PATCH",
                "RNG_ROUTE",
                "RNG_TYPE0",
                "RNG_SECTOR_RECIPE",
                "RNG_POPULATION"
            };
            var result = new Dictionary<string, DeterministicRngStream>(StringComparer.Ordinal);
            foreach (var id in reverse ? ids.Reverse() : ids)
            {
                result.Add(id, CreateRequiredStream(streams, id, seed, RequiredIdentity(id)));
            }

            return result;
        }

        private static DeterministicRngStream CreateRequiredStream(
            WorldGenerationRngStreams streams,
            string streamId,
            ulong seed,
            string identity)
        {
            switch (streamId)
            {
                case "RNG_WORLD_SITE":
                    return streams.CreateWorldSite(seed);
                case "RNG_BIOME_PATCH":
                    return streams.CreateBiomePatch(seed, identity);
                case "RNG_ROUTE":
                    return streams.CreateRoute(seed, identity);
                case "RNG_TYPE0":
                    return streams.CreateType0(seed, identity);
                case "RNG_SECTOR_RECIPE":
                    return streams.CreateSectorRecipe(seed, new SectorCoord(6, 6));
                case "RNG_POPULATION":
                    return streams.CreatePopulation(seed, identity);
                default:
                    throw new ArgumentOutOfRangeException(nameof(streamId));
            }
        }

        private static string RequiredIdentity(string streamId)
        {
            switch (streamId)
            {
                case "RNG_WORLD_SITE": return string.Empty;
                case "RNG_BIOME_PATCH": return "PASS_BIOME";
                case "RNG_ROUTE": return "PASS_ROUTE";
                case "RNG_TYPE0": return "PASS_TYPE0";
                case "RNG_SECTOR_RECIPE": return "6,6";
                case "RNG_POPULATION": return "6,6";
                default: throw new ArgumentOutOfRangeException(nameof(streamId));
            }
        }

        private static SeedReplayBundle Bundle(
            ulong seed,
            IWorldGenerationClock clock = null)
        {
            var execution = CreateRoot(clock: clock).ExecuteThroughRecorded(
                GenerationProfileId,
                seed,
                GridInitializationPass.PassId);
            return new SeedReplayRecorder().Record(execution, Hash(), BuildId);
        }

        private static WorldGenerationRoot CreateRoot(
            IWorldGenerationPass pass = null,
            IWorldGenerationClock clock = null,
            string definitionClassName = null)
        {
            var implementation = pass ?? new GridInitializationPassAdapter();
            var passDefinition = Definition<GenerationPassDefinition>(
                Pair("GenerationProfileId", (object)GenerationProfileId),
                Pair("PassOrder", 0),
                Pair("PassId", GridInitializationPass.PassId),
                Pair("ClassName", definitionClassName ?? implementation.ClassName),
                Pair("RngStreamId", string.Empty),
                Pair("InputArtifacts", ReadOnlyStrings()),
                Pair("OutputArtifacts", ReadOnlyStrings(GridInitializationPass.OutputArtifactId)),
                Pair("FailurePolicy", "FAIL_WORLD"),
                Pair("MaxRetryCount", 0),
                Pair("Enabled", true),
                Pair("Notes", string.Empty));
            var generationProfile = Definition<GenerationProfileDefinition>(
                Pair("GenerationProfileId", (object)GenerationProfileId),
                Pair("WorldProfileId", WorldProfileId),
                Pair("Active", true));
            var worldProfile = Definition<WorldProfileDefinition>(
                Pair("WorldProfileId", (object)WorldProfileId),
                Pair("Active", true));
            var definitions = Construct<WorldRouteDefinitionSet>(
                new[] { worldProfile },
                new[] { generationProfile },
                new[] { passDefinition },
                RequiredRngDefinitions(),
                Array.Empty<SectorRouteMaskDefinition>(),
                Array.Empty<SocketBandDefinition>(),
                Array.Empty<EdgeSignatureDefinition>(),
                Array.Empty<EdgeSignatureCompatibilityDefinition>(),
                Array.Empty<SectorRecipeDefinition>(),
                Array.Empty<SectorRecipeCellDefinition>(),
                Array.Empty<SectorRecipePathDefinition>(),
                Array.Empty<SectorExternalSocketDefinition>(),
                Array.Empty<SectorRecipePoolEntryDefinition>());
            var staticData = (StaticDataRegistry)FormatterServices.GetUninitializedObject(
                typeof(StaticDataRegistry));
            SetAutoProperty(staticData, "WorldRouteDefinitions", definitions);
            return new WorldGenerationRoot(
                staticData,
                new WorldGenerationPassRegistry(new[] { implementation }),
                clock ?? new ManualClock());
        }

        private static RngStreamDefinition[] RequiredRngDefinitions()
        {
            return new[]
            {
                Rng("RNG_WORLD_SITE", "A13C9E0B2F1044D1", "WORLD"),
                Rng("RNG_BIOME_PATCH", "B7A91D33E40C5F82", "PASS"),
                Rng("RNG_ROUTE", "C00FEE12AB341901", "PASS"),
                Rng("RNG_TYPE0", "D15EA5E007A4C883", "PASS"),
                Rng("RNG_SECTOR_RECIPE", "E9931A70C2D520F4", "SECTOR"),
                Rng("RNG_POPULATION", "F123456789ABCDEF", "SPAWN")
            };
        }

        private static RngStreamDefinition Rng(string id, string salt, string scope)
        {
            return Definition<RngStreamDefinition>(
                Pair("RngStreamId", (object)id),
                Pair("SaltHex", CreateHex(salt)),
                Pair("ResetScope", scope),
                Pair("DescriptionKo", "MAP02 exit"),
                Pair("Active", true));
        }

        private static CsvHexValue CreateHex(string value)
        {
            var bytes = Enumerable.Range(0, value.Length / 2)
                .Select(index => byte.Parse(
                    value.Substring(index * 2, 2),
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture))
                .ToArray();
            var constructor = typeof(CsvHexValue).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(string), typeof(IEnumerable<byte>) },
                null);
            Assert.That(constructor, Is.Not.Null);
            return (CsvHexValue)constructor.Invoke(new object[] { value, bytes });
        }

        private static ContentVersionHash Hash(byte offset = 0)
        {
            var bytes = Enumerable.Range(0, 32)
                .Select(value => (byte)(value + offset))
                .ToArray();
            var constructor = typeof(ContentVersionHash).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(IEnumerable<byte>) },
                null);
            Assert.That(constructor, Is.Not.Null);
            return (ContentVersionHash)constructor.Invoke(new object[] { bytes });
        }

        private static IReadOnlyList<string> ReadOnlyStrings(params string[] values)
        {
            return new ReadOnlyCollection<string>(new List<string>(values));
        }

        private static T Definition<T>(params KeyValuePair<string, object>[] values)
        {
            var definition = (T)FormatterServices.GetUninitializedObject(typeof(T));
            foreach (var pair in values)
            {
                SetAutoProperty(definition, pair.Key, pair.Value);
            }

            return definition;
        }

        private static T Construct<T>(params object[] arguments)
        {
            return (T)Activator.CreateInstance(
                typeof(T),
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                arguments,
                CultureInfo.InvariantCulture);
        }

        private static void SetAutoProperty(object target, string propertyName, object value)
        {
            var field = target.GetType().GetField(
                "<" + propertyName + ">k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, propertyName);
            field.SetValue(target, value);
        }

        private static KeyValuePair<string, object> Pair(string key, object value)
        {
            return new KeyValuePair<string, object>(key, value);
        }

        private static void WithTempRoot(Action<string> action)
        {
            var root = Path.GetFullPath(Path.Combine(
                Path.GetTempPath(),
                "StarNight_MAP02_08_" + Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(root);
            try
            {
                action(root);
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }

        private sealed class CountingGridPass : IWorldGenerationPass
        {
            private readonly bool fail;

            public CountingGridPass(bool fail = false)
            {
                this.fail = fail;
            }

            public string PassId => GridInitializationPass.PassId;
            public string ClassName => nameof(CountingGridPass);
            public int InvocationCount { get; private set; }

            public WorldGenerationPassResult Execute(WorldGenerationPassContext context)
            {
                InvocationCount++;
                return fail
                    ? WorldGenerationPassResult.Failure("EXPECTED_FAILURE", "expected failure")
                    : WorldGenerationPassResult.Success(
                        GridInitializationPass.OutputArtifactId,
                        new GridInitializationPass().Execute(context.WorldSeed));
            }
        }

        private sealed class ManualClock : IWorldGenerationClock
        {
            private readonly DateTimeOffset startUtc;
            private readonly TimeSpan elapsedPerTimestamp;
            private int utcCalls;
            private long timestampCalls;

            public ManualClock()
                : this(StartUtc, TimeSpan.FromMilliseconds(1))
            {
            }

            public ManualClock(DateTimeOffset startUtc, TimeSpan elapsedPerTimestamp)
            {
                this.startUtc = startUtc;
                this.elapsedPerTimestamp = elapsedPerTimestamp;
            }

            public DateTimeOffset GetUtcNow()
            {
                return startUtc.AddSeconds(utcCalls++);
            }

            public long GetTimestamp()
            {
                return timestampCalls++;
            }

            public TimeSpan GetElapsedTime(long startTimestamp, long endTimestamp)
            {
                return TimeSpan.FromTicks(
                    (endTimestamp - startTimestamp) * elapsedPerTimestamp.Ticks);
            }
        }
    }
}
