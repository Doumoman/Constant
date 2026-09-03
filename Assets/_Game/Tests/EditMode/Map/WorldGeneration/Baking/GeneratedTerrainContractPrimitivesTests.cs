using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.SectorPlanning;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Baking
{
    [TestFixture]
    [Category("MAP16_09")]
    public sealed class GeneratedTerrainContractPrimitivesTests
    {
        private const string FixtureLabel = "REFERENCE MAP16 SLICE OUTPUT EXIT";
        private const string CanvasGolden = "450645c1f7ea6f326ffb21c569bdff83b19e2c456de03dbf7770487eb8c9738d";
        private const string DensityGolden = "549469a22af5f75f64fb14155647d84a66e85c5ad6b6ca260af55d805e88c43b";
        private const string RouteGolden = "9fa02be125385fb575331812435dc01f9be316f8c518f16b9e4fc3482c497c25";
        private const string PartitionGolden = "56352472c3da4777a56e75c1012588c0fbbfa93064559ed134ee8e5d598c45b5";
        private const string SliceGolden = "deaf94c9cbb323342911f13bcf2d14f3e8715abbea4f8450b78d35d5a189a882";
        private const string SlotGolden = "13a0e6733db9266b1e3bddc8d26dee54776ac6eb2d934a19bc2e408eda405737";
        private const string ManifestGolden = "557ee873aaea69efccde5cddcf3cc1bc84ba2c77522e65f0aa75bf0e0e0fa202";
        private const string PacketGolden = "fed5b33ad83e7577998f9c3f7b604653ecb380f5d469f66c69570f72fd454189";
        private const string ExitGolden = "78d3046d62608494fb1306ff4e57a0b2d4b36eafc3a5e7e19cb8f399c3ca29f0";
        private readonly List<string> temporaryRoots = new List<string>();
        private static Map16Chain reference;

        [TearDown]
        public void RemoveTemporaryCsvFiles()
        {
            foreach (var root in temporaryRoots.OrderByDescending(value => value.Length))
            {
                try
                {
                    if (Directory.Exists(root)) Directory.Delete(root, true);
                }
                catch (Exception exception)
                {
                    Assert.Fail("MAP16_09 temporary CSV cleanup failed for " + root + ": " + exception);
                }
            }
            temporaryRoots.Clear();
        }

        [Test]
        public void GeometrySnapshotDerivesSectorChunkPatternWorldLayerAndRecordCounts()
        {
            GeneratedTerrainGeometrySnapshot snapshot;
            IReadOnlyList<string> failures;
            Assert.That(GeneratedTerrainGeometrySnapshot.TryCreate(out snapshot, out failures), Is.True,
                string.Join(";", failures));
            Assert.That(snapshot, Is.Not.Null);
            Assert.That(failures, Is.Empty);
            Assert.That(snapshot.SectorWidth, Is.EqualTo(48));
                Assert.That(snapshot.SectorHeight, Is.EqualTo(32));
                Assert.That(snapshot.SectorCellCount, Is.EqualTo(1536));
                Assert.That(snapshot.MicroChunkWidth, Is.EqualTo(12));
                Assert.That(snapshot.MicroChunkHeight, Is.EqualTo(8));
                Assert.That(snapshot.MicroChunkCellCount, Is.EqualTo(96));
                Assert.That(snapshot.ChunkGridWidth, Is.EqualTo(4));
                Assert.That(snapshot.ChunkGridHeight, Is.EqualTo(4));
                Assert.That(snapshot.ChunkCount, Is.EqualTo(16));
                Assert.That(snapshot.MicroPatternWidth, Is.EqualTo(4));
                Assert.That(snapshot.MicroPatternHeight, Is.EqualTo(4));
                Assert.That(snapshot.PatternsPerChunkX, Is.EqualTo(3));
                Assert.That(snapshot.PatternsPerChunkY, Is.EqualTo(2));
                Assert.That(snapshot.WorldSectorColumns, Is.EqualTo(13));
                Assert.That(snapshot.WorldSectorRows, Is.EqualTo(13));
                Assert.That(snapshot.WorldSectorCount, Is.EqualTo(169));
                Assert.That(snapshot.WorldWidth, Is.EqualTo(624));
                Assert.That(snapshot.WorldHeight, Is.EqualTo(416));
                Assert.That(snapshot.WorldCellCount, Is.EqualTo(259584));
                Assert.That(snapshot.WorldProjectedSliceCount, Is.EqualTo(2704));
                Assert.That(snapshot.LayersPerFinalCanvasCell, Is.EqualTo(7));
                Assert.That(snapshot.SectorLayerRecordCount, Is.EqualTo(10752));
                Assert.That(snapshot.ChunkRotationAllowed, Is.False);
            Assert.That(snapshot.CanonicalLines, Has.Count.EqualTo(7));
        }

        [Test]
        public void BakingCanonicalDigestHashesLfUtf8NoBomLowerHexAndValidatesHex()
        {
            const string expected = "f3220283d05d1ff2ae350cfe9e0e367cb5aef46e10efb203c8a53c678e2218c8";
            Assert.That(BakingCanonicalDigest.HashCanonicalText("alpha\nbeta\ngamma"), Is.EqualTo(expected));
            Assert.That(BakingCanonicalDigest.HashCanonicalText("alpha\r\nbeta\rgamma"), Is.EqualTo(expected));
            Assert.That(BakingCanonicalDigest.HashCanonicalLines(new[] { "alpha", "beta", "gamma" }),
                Is.EqualTo(expected));
            Assert.That(BakingCanonicalDigest.NormalizeLineEndingsToLf("a\r\nb\rc"), Is.EqualTo("a\nb\nc"));
            Assert.That(BakingCanonicalDigest.Utf8NoBomEncoding.GetPreamble(), Is.Empty);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(expected), Is.True);
            Assert.That(BakingCanonicalDigest.IsLowerHexSha256(expected.ToUpperInvariant()), Is.False);
            Assert.Throws<ArgumentNullException>(() => BakingCanonicalDigest.HashCanonicalText(null));
            Assert.Throws<ArgumentNullException>(() => BakingCanonicalDigest.NormalizeLineEndingsToLf(null));
            Assert.Throws<ArgumentNullException>(() => BakingCanonicalDigest.HashCanonicalLines(null));
        }

        [Test]
        public void GeneratedTerrainGeometrySnapshotReplacesDuplicatedSerializationLiterals()
        {
            var chain = ReferenceChain();
            Assert.That(WorldAssemblyOverlayExport.WorldWidthTiles,
                    Is.EqualTo(GeneratedTerrainGeometrySnapshot.CanonicalWorldWidth));
                Assert.That(WorldAssemblyOverlayExport.WorldHeightTiles,
                    Is.EqualTo(GeneratedTerrainGeometrySnapshot.CanonicalWorldHeight));
                Assert.That(WorldAssemblyOverlayExport.WorldSectorCount,
                    Is.EqualTo(GeneratedTerrainGeometrySnapshot.CanonicalWorldSectorCount));
                Assert.That(SectorPatternChunkPartition.SectorWidth,
                    Is.EqualTo(GeneratedTerrainGeometrySnapshot.CanonicalSectorWidth));
                Assert.That(SectorPatternChunkPartition.MicroChunkWidth,
                    Is.EqualTo(GeneratedTerrainGeometrySnapshot.CanonicalMicroChunkWidth));
                Assert.That(GeneratedMicroChunkSliceSet.ChunkCount,
                    Is.EqualTo(GeneratedTerrainGeometrySnapshot.CanonicalChunkCount));
                Assert.That(chain.Packet.Manifest.SectorCellCount,
                    Is.EqualTo(GeneratedTerrainGeometrySnapshot.CanonicalSectorCellCount));
            Assert.That(chain.Packet.Manifest.MicroChunkCellCount,
                Is.EqualTo(GeneratedTerrainGeometrySnapshot.CanonicalMicroChunkCellCount));

            var root = ProjectRoot();
            StringAssert.DoesNotContain("GEOMETRY|48|32|1536|4|4|16|12|8|96|4",
                File.ReadAllText(Path.Combine(root,
                    "Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedTerrainCsvExporter.cs")));
            StringAssert.DoesNotContain("CONSTANTS|48|32|1536|12|8|96|4|4|16|4|4|3|2|7|ROTATION|",
                File.ReadAllText(Path.Combine(root,
                    "Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedMicroChunkSliceSet.cs")));
        }

        [Test]
        public void Map16DigestClassesDelegateOnlyFinalHashPrimitiveAndKeepGoldenOutputs()
        {
            const string sample = "MAP16_09\r\nCANONICAL\rTEXT";
            var expected = BakingCanonicalDigest.HashCanonicalText(sample);
            Assert.That(FinalCanvasLayerDigest.HashCanonicalText(sample), Is.EqualTo(expected));
            Assert.That(ProtectionDensityDigest.HashCanonicalText(sample), Is.EqualTo(expected));
            Assert.That(FinalRouteRecoveryDigest.HashCanonicalText(sample), Is.EqualTo(expected));
            Assert.That(PatternChunkPartitionDigest.HashCanonicalText(sample), Is.EqualTo(expected));
            Assert.That(GeneratedMicroChunkSliceDigest.HashCanonicalText(sample), Is.EqualTo(expected));
            Assert.That(MarkerSlotProjectionDigest.HashCanonicalText(sample), Is.EqualTo(expected));
            Assert.That(GeneratedTerrainExportDigest.Hash(sample), Is.EqualTo(expected));

            var chain = ReferenceChain();
            CollectionAssert.AreEqual(new[]
            {
                CanvasGolden, DensityGolden, RouteGolden, PartitionGolden, SliceGolden,
                SlotGolden, ManifestGolden, PacketGolden, ExitGolden,
            }, new[]
            {
                chain.Canvas.OutputDigest, chain.Density.OutputDigest, chain.Route.OutputDigest,
                chain.Partition.OutputDigest, chain.Slices.OutputDigest, chain.Slots.OutputDigest,
                chain.Packet.ManifestDigest, chain.Packet.PacketDigest, ExitDigest(chain),
            });
        }

        [Test]
        public void CsvExportReplayHeadersRowsManifestsAndDigestsRemainByteCompatible()
        {
            var chain = ReferenceChain();
            var expectedNames = new[]
            {
                "generated_terrain_manifest.csv", "generated_terrain_plan.csv",
                "generated_terrain_slices.csv", "generated_terrain_cells.csv",
                "generated_terrain_sockets.csv", "generated_terrain_slots.csv",
            };
            var expectedHeaders = new[]
            {
                GeneratedTerrainCsvExporter.ManifestHeader, GeneratedTerrainCsvExporter.PlanHeader,
                GeneratedTerrainCsvExporter.SlicesHeader, GeneratedTerrainCsvExporter.CellsHeader,
                GeneratedTerrainCsvExporter.SocketsHeader, GeneratedTerrainCsvExporter.SlotsHeader,
            };
            CollectionAssert.AreEqual(expectedNames, GeneratedTerrainCsvExporter.RequiredFileNames);
            CollectionAssert.AreEqual(expectedNames, chain.Packet.Files.Select(value => value.FileName));
            CollectionAssert.AreEqual(expectedHeaders, chain.Packet.Files.Select(value =>
                value.Payload.Substring(0, value.Payload.IndexOf('\n'))));

            var directory = NewExportDirectory();
            var write = GeneratedTerrainCsvExporter.Write(chain.Packet, directory);
            Assert.That(write.Success, Is.True, Describe(write.Failures));
            var replay = GeneratedTerrainReplayVerifier.Verify(directory);
            Assert.That(replay.Success, Is.True, Describe(replay.Failures));
            Assert.That(replay.ManifestDigest, Is.EqualTo(ManifestGolden));
            Assert.That(replay.ReplayDigest, Is.EqualTo(PacketGolden));
            Assert.That(Directory.GetFiles(directory, "*.csv").Length, Is.EqualTo(6));
        }

        [Test]
        public void ContractPrimitiveChangesAreDeterministicAcrossRepeatReverseCultureAndLineEndings()
        {
            var first = ReferenceChain();
            var repeat = BuildChain(false);
            var reverse = BuildChain(true);
            CollectionAssert.AreEqual(OutputDigests(first), OutputDigests(repeat));
            CollectionAssert.AreEqual(OutputDigests(first), OutputDigests(reverse));

            var reverseExport = GeneratedTerrainCsvExporter.Build(first.Slices, first.Slots,
                first.Slices.Slices.Reverse(), first.Slots.Slots.Reverse());
            Assert.That(reverseExport.Success, Is.True, Describe(reverseExport.Failures));
            Assert.That(reverseExport.Packet.PacketDigest, Is.EqualTo(first.Packet.PacketDigest));

            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                CollectionAssert.AreEqual(OutputDigests(first), OutputDigests(BuildChain(true)));
                Assert.That(BakingCanonicalDigest.HashCanonicalText("a\r\nb\rc"),
                    Is.EqualTo(BakingCanonicalDigest.HashCanonicalText("a\nb\nc")));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }

        [Test]
        public void ObservationHighFindingsAreCoveredWithoutChangingCsvOwnerTokensOrFailures()
        {
            AssertMapping(FinalCanvasSourceOwner.TerrainCluster, GeneratedMarkerSlotKind.TerrainCluster,
                GeneratedMarkerSlotOwner.TerrainCluster, "MAP11_TERRAIN_CLUSTER");
            AssertMapping(FinalCanvasSourceOwner.Activity, GeneratedMarkerSlotKind.Activity,
                GeneratedMarkerSlotOwner.Activity, "MAP12_ACTIVITY");
            AssertMapping(FinalCanvasSourceOwner.SpecialRegion, GeneratedMarkerSlotKind.SpecialRegion,
                GeneratedMarkerSlotOwner.SpecialRegion, "MAP13_SPECIAL_REGION");
            AssertMapping(FinalCanvasSourceOwner.EventOverlay, GeneratedMarkerSlotKind.EventOverlay,
                GeneratedMarkerSlotOwner.EventOverlay, "MAP12_EVENT_OVERLAY");
            AssertMapping(FinalCanvasSourceOwner.Boundary, GeneratedMarkerSlotKind.Boundary,
                GeneratedMarkerSlotOwner.Boundary, "MAP15_02_BOUNDARY");
            AssertMapping(FinalCanvasSourceOwner.MandatoryRoute, GeneratedMarkerSlotKind.RouteRecovery,
                GeneratedMarkerSlotOwner.RouteRecovery, "MAP16_03_ROUTE_RECOVERY");

            var failure = GeneratedTerrainCsvExporter.Build(null, null);
            Assert.That(failure.Success, Is.False);
            Assert.That(failure.Packet, Is.Null);
            Assert.That(failure.Failures, Is.Not.Empty);
            Assert.That(failure.WrittenFileCount, Is.Zero);
            Assert.That(typeof(GeneratedTerrainExportFailure).Name, Is.EqualTo("GeneratedTerrainExportFailure"));
        }

        [Test]
        public void NoTilemapScenePrefabGameObjectSpawnGeneratedAssetOrAuthoringMutation()
        {
            var chain = ReferenceChain();
            Assert.That(chain.Slices.GeneratedFileWriteCount, Is.Zero);
                Assert.That(chain.Slices.GeneratedAssetWriteCount, Is.Zero);
                Assert.That(chain.Slices.StableSpawnIdCount, Is.Zero);
                Assert.That(chain.Slices.TilemapBakeCount, Is.Zero);
                Assert.That(chain.Slices.TilemapMutationCount, Is.Zero);
                Assert.That(chain.Slices.SceneMutationCount, Is.Zero);
                Assert.That(chain.Slices.PrefabMutationCount, Is.Zero);
                Assert.That(chain.Slices.GameObjectMutationCount, Is.Zero);
                Assert.That(chain.Slices.GameplaySpawnCount, Is.Zero);
                Assert.That(chain.Slices.ProductionSeedApprovalCount, Is.Zero);
                Assert.That(chain.Slots.RuntimeObjectSpawnCount, Is.Zero);
                Assert.That(chain.Slots.StableSpawnIdCount, Is.Zero);
                Assert.That(chain.Slots.TilemapBakeCount, Is.Zero);
                Assert.That(chain.Slots.TilemapMutationCount, Is.Zero);
                Assert.That(chain.Slots.SceneMutationCount, Is.Zero);
                Assert.That(chain.Slots.PrefabMutationCount, Is.Zero);
                Assert.That(chain.Slots.GameObjectMutationCount, Is.Zero);
            Assert.That(chain.Slots.ProductionSeedApprovalCount, Is.Zero);
        }

        [Test]
        public void BacklogAmendmentInstallsMap16_09AndKeepsMap17_01Locked()
        {
            var root = ProjectRoot();
            var taskPath = Path.Combine(root,
                "MapDesign/MCP/TASKS/MAP16_09_CONSOLIDATE_GENERATED_TERRAIN_CONTRACT_PRIMITIVES.md");
            var master = File.ReadAllText(Path.Combine(root, "MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md"));
            var status = File.ReadAllText(Path.Combine(root, "MapDesign/MCP/06_IMPLEMENTATION_STATUS.md"));
            Assert.That(File.Exists(taskPath), Is.True);
            StringAssert.Contains("MAP16_09_CONSOLIDATE_GENERATED_TERRAIN_CONTRACT_PRIMITIVES", master);
            Assert.That(status.Contains(
                    "| MAP16_09_CONSOLIDATE_GENERATED_TERRAIN_CONTRACT_PRIMITIVES | CURRENT |") ||
                status.Contains(
                    "| MAP16_09_CONSOLIDATE_GENERATED_TERRAIN_CONTRACT_PRIMITIVES | COMPLETE |"),
                Is.True, "MAP16_09 must be CURRENT while executing or COMPLETE after PASS finalization.");
            StringAssert.Contains("| MAP17_01_RESOLVE_ASSETS_AND_PLACE_GENERATED_CELLS | LOCKED |", status);
            StringAssert.DoesNotContain("| MAP17_01_RESOLVE_ASSETS_AND_PLACE_GENERATED_CELLS | CURRENT |", status);
        }

        private string NewExportDirectory()
        {
            var root = Path.Combine(Path.GetTempPath(), "map16_09_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            temporaryRoots.Add(root);
            return Path.Combine(root, "export");
        }

        private static Map16Chain ReferenceChain()
        {
            if (reference == null) reference = BuildChain(false);
            return reference;
        }

        private static Map16Chain BuildChain(bool reverseClaims)
        {
            var fixture = new ReferenceFinalRouteRecoveryFixture();
            var baselineRouteRequest = fixture.AcceptedRequest();
            var baselinePlan = baselineRouteRequest.CanvasPlan;
            var baselineRequest = baselinePlan.Request;
            var claims = baselinePlan.Cells.SelectMany(value => value.Winners).ToList();

            for (var x = 0; x < 48; x++)
            {
                if (x != 30 && x != 31) AddPassableOverrides(claims, x, 15);
            }
            AddPassableOverrides(claims, 20, 7);
            AddPassableOverrides(claims, 11, 5);
            claims.Add(Claim("MAP16_08_TERRAIN_CLUSTER", 12, 20,
                FinalCanvasLayerKind.Terrain, FinalCanvasCellKind.Air,
                FinalCanvasSourceOwner.TerrainCluster,
                FinalCanvasClaimPriority.TerrainClusterPattern, "MAP11_TERRAIN_CLUSTER"));
            claims.Add(Claim("MAP16_08_ACTIVITY", 12, 20,
                FinalCanvasLayerKind.Marker, FinalCanvasCellKind.Marker,
                FinalCanvasSourceOwner.Activity,
                FinalCanvasClaimPriority.ActivityMarker, "MAP12_ACTIVITY"));
            claims.Add(Claim("MAP16_08_EVENT_OVERLAY", 13, 20,
                FinalCanvasLayerKind.Marker, FinalCanvasCellKind.Marker,
                FinalCanvasSourceOwner.EventOverlay,
                FinalCanvasClaimPriority.EventMarker, "MAP12_EVENT_OVERLAY"));
            if (reverseClaims) claims.Reverse();

            var canvasResult = SectorCanvasLayerFinalizer.Finalize(new FinalCanvasLayerRequest(
                baselineRequest.SectorId, baselineRequest.Width, baselineRequest.Height, claims,
                baselineRequest.Map15ExitApproved, baselineRequest.Map15ExitDigest,
                baselineRequest.WorldAssemblyDigest, baselineRequest.SectorOwnershipDigest,
                baselineRequest.BoundaryAuthorityDigest, baselineRequest.FixedCanvasAuthorityDigest,
                baselineRequest.PublicationLabel));
            Assert.That(canvasResult.Success, Is.True, Describe(canvasResult.Failures));
            var densityResult = SectorCanvasProtectionDensityValidator.Validate(canvasResult.Plan);
            Assert.That(densityResult.Success, Is.True, Describe(densityResult.Failures));
            var routeResult = SectorFinalRouteRecoveryValidator.Validate(new FinalRouteRecoveryRequest(
                canvasResult.Plan, densityResult.Report, baselineRouteRequest.Anchors,
                baselineRouteRequest.DeclaredEdges, baselineRouteRequest.PublicationLabel));
            Assert.That(routeResult.Success, Is.True, Describe(routeResult.Failures));
            var partitionResult = SectorPatternChunkPartitioner.Partition(
                canvasResult.Plan, densityResult.Report, routeResult.Report);
            Assert.That(partitionResult.Success, Is.True, Describe(partitionResult.Failures));
            var sliceResult = GeneratedMicroChunkSliceBuilder.Build(canvasResult.Plan,
                densityResult.Report, routeResult.Report, partitionResult.Partition);
            Assert.That(sliceResult.Success, Is.True, Describe(sliceResult.Failures));
            var slotResult = GeneratedMicroChunkMarkerSlotProjector.Project(sliceResult.SliceSet);
            Assert.That(slotResult.Success, Is.True, Describe(slotResult.Failures));
            var exportResult = GeneratedTerrainCsvExporter.Build(sliceResult.SliceSet, slotResult.SlotSet);
            Assert.That(exportResult.Success, Is.True, Describe(exportResult.Failures));
            return new Map16Chain(canvasResult.Plan, densityResult.Report, routeResult.Report,
                partitionResult.Partition, sliceResult.SliceSet, slotResult.SlotSet, exportResult.Packet);
        }

        private static void AddPassableOverrides(ICollection<FinalCanvasLayerClaim> claims, int x, int y)
        {
            claims.Add(Claim("MAP16_08_OPEN_TERRAIN_" + x + "_" + y, x, y,
                FinalCanvasLayerKind.Terrain, FinalCanvasCellKind.Air,
                FinalCanvasSourceOwner.MicroPattern,
                FinalCanvasClaimPriority.TerrainClusterPattern, FixtureLabel));
            claims.Add(Claim("MAP16_08_OPEN_AFFORDANCE_" + x + "_" + y, x, y,
                FinalCanvasLayerKind.Affordance, FinalCanvasCellKind.Traversable,
                FinalCanvasSourceOwner.MicroPattern,
                FinalCanvasClaimPriority.TerrainClusterPattern, FixtureLabel));
            claims.Add(Claim("MAP16_08_OPEN_MATERIAL_" + x + "_" + y, x, y,
                FinalCanvasLayerKind.Material, FinalCanvasCellKind.None,
                FinalCanvasSourceOwner.MicroPattern,
                FinalCanvasClaimPriority.TerrainClusterPattern, FixtureLabel));
        }

        private static FinalCanvasLayerClaim Claim(string claimId, int x, int y,
            FinalCanvasLayerKind layer, FinalCanvasCellKind cellKind,
            FinalCanvasSourceOwner owner, FinalCanvasClaimPriority priority,
            string provenanceId) => new FinalCanvasLayerClaim(
                claimId, new FinalCanvasCellCoordinate(x, y), layer, cellKind, owner, priority,
                FinalCanvasProtectionKind.None, false, provenanceId, FixtureLabel);

        private static void AssertMapping(FinalCanvasSourceOwner sourceOwner,
            GeneratedMarkerSlotKind expectedKind, GeneratedMarkerSlotOwner expectedOwner,
            string expectedTask)
        {
            GeneratedMarkerSlotKind kind;
            GeneratedMarkerSlotOwner owner;
            string task;
            Assert.That(GeneratedMicroChunkMarkerSlotProjector.TryMapOwner(
                sourceOwner, out kind, out owner, out task), Is.True);
            Assert.That(kind, Is.EqualTo(expectedKind));
            Assert.That(owner, Is.EqualTo(expectedOwner));
            Assert.That(task, Is.EqualTo(expectedTask));
        }

        private static string[] OutputDigests(Map16Chain chain) => new[]
        {
            chain.Canvas.OutputDigest, chain.Density.OutputDigest, chain.Route.OutputDigest,
            chain.Partition.OutputDigest, chain.Slices.OutputDigest, chain.Slots.OutputDigest,
            chain.Packet.ManifestDigest, chain.Packet.PacketDigest, ExitDigest(chain),
        };

        private static string ExitDigest(Map16Chain chain)
        {
            var digestValues = new[]
            {
                chain.Canvas.InputDigest, chain.Canvas.OutputDigest,
                chain.Density.InputDigest, chain.Density.OutputDigest,
                chain.Route.InputDigest, chain.Route.OutputDigest,
                chain.Partition.InputDigest, chain.Partition.OutputDigest,
                chain.Slices.InputDigest, chain.Slices.OutputDigest,
                chain.Slots.InputDigest, chain.Slots.OutputDigest,
                chain.Packet.ManifestDigest, chain.Packet.PacketDigest,
            };
            var canonical = string.Join("\n", new[]
            {
                "POLICY|MAP16_08_SLICE_OUTPUT_EXIT_V1",
                "FIXTURE|" + FixtureLabel,
                "DIGESTS|" + string.Join("|", digestValues),
                "SECTOR|48|32|1536|16|12|8|96|10752",
                "SOCKETS|64|" + Number(chain.Slices.SocketBandCount) + "|24|0|16|16",
                "SLOTS|24|24|0",
                "EXPORT|6|" + chain.Packet.ManifestDigest + "|" + chain.Packet.PacketDigest,
                "OVERLAY|1536|16|1536|24|64",
                "WORLD|169|13|13|624|416|2704|259584|0|0|0",
                "MUTATION|0|0|0|0|0|0|0|REGRESSION|0|0|0|0|0",
                "HANDOFF|MAP17_01_RESOLVE_ASSETS_AND_PLACE_GENERATED_CELLS|LOCKED",
            });
            return GeneratedTerrainExportDigest.Hash(canonical);
        }

        private static string ProjectRoot() => Directory.GetParent(Application.dataPath).FullName;
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Describe(System.Collections.IEnumerable values)
        {
            var result = new List<string>();
            foreach (var value in values) result.Add(value == null ? "NULL" : value.ToString());
            return string.Join(";", result);
        }

        private sealed class Map16Chain
        {
            public Map16Chain(SectorFinalCanvasLayerPlan canvas,
                SectorCanvasProtectionDensityReport density,
                SectorFinalRouteRecoveryReport route,
                SectorPatternChunkPartition partition,
                GeneratedMicroChunkSliceSet slices,
                GeneratedMicroChunkMarkerSlotSet slots,
                GeneratedTerrainExportPacket packet)
            {
                Canvas = canvas;
                Density = density;
                Route = route;
                Partition = partition;
                Slices = slices;
                Slots = slots;
                Packet = packet;
            }

            public SectorFinalCanvasLayerPlan Canvas { get; }
            public SectorCanvasProtectionDensityReport Density { get; }
            public SectorFinalRouteRecoveryReport Route { get; }
            public SectorPatternChunkPartition Partition { get; }
            public GeneratedMicroChunkSliceSet Slices { get; }
            public GeneratedMicroChunkMarkerSlotSet Slots { get; }
            public GeneratedTerrainExportPacket Packet { get; }
        }
    }
}
