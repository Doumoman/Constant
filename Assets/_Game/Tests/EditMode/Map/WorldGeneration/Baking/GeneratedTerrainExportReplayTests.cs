using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Baking
{
    [TestFixture]
    [Category("MAP16_07")]
    public sealed class GeneratedTerrainExportReplayTests
    {
        private readonly List<string> temporaryRoots = new List<string>();
        private static SourcePair reference;

        [TearDown]
        public void RemoveTemporaryCsvFiles()
        {
            foreach (var root in temporaryRoots.OrderByDescending(value => value.Length))
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); }
                catch (Exception exception)
                {
                    Assert.Fail("MAP16_07 temporary CSV cleanup failed for " + root + ": " + exception);
                }
            }
            temporaryRoots.Clear();
        }

        [Test]
        public void ExportPacketPublishesPlanSliceCellSocketSlotCsvContractsAndDigests()
        {
            var source = ReferenceSource();
            var result = GeneratedTerrainCsvExporter.Build(source.Slices, source.Slots);

            Assert.That(result.Success, Is.True, Failures(result));
            var packet = result.Packet;
            Assert.That(packet.LogicalFileCount, Is.EqualTo(6));
            Assert.That(packet.Files.Select(value => value.FileName), Is.EqualTo(
                GeneratedTerrainCsvExporter.RequiredFileNames));
            Assert.That(packet.PlanRows, Has.Count.EqualTo(1));
            Assert.That(packet.SliceRows, Has.Count.EqualTo(16));
            Assert.That(packet.CellRows, Has.Count.EqualTo(1536));
            Assert.That(packet.LayerRecordCount, Is.EqualTo(10752));
            Assert.That(packet.SocketRows, Has.Count.EqualTo(64));
            Assert.That(packet.SlotRows, Has.Count.EqualTo(24));
            Assert.That(packet.Files.All(value => value.RowCount > 0 &&
                GeneratedTerrainExportDigest.IsLowerHexSha256(value.PayloadDigest)), Is.True);
            Assert.That(GeneratedTerrainExportDigest.IsLowerHexSha256(packet.ManifestDigest), Is.True);
            Assert.That(GeneratedTerrainExportDigest.IsLowerHexSha256(packet.PacketDigest), Is.True);
            Assert.That(packet.Manifest.SourceSliceSetDigest, Is.EqualTo(source.Slices.OutputDigest));
            Assert.That(packet.Manifest.SourceMarkerSlotSetDigest, Is.EqualTo(source.Slots.OutputDigest));

            TestContext.WriteLine("MAP16_07_EXPORT_EVIDENCE" +
                " source_slices=" + source.Slices.SliceCount + "/16" +
                " source_cells=" + source.Slices.TotalCellCount + "/1536" +
                " source_layers=" + source.Slices.TotalLayerRecordCount + "/10752" +
                " source_socket_signatures=" + source.Slices.SocketSideSignatureCount + "/64" +
                " source_slots=" + source.Slots.SlotCount + "/24" +
                " files=" + packet.LogicalFileCount + "/6" +
                " rows=1/1/" + packet.SliceRows.Count + "/" + packet.CellRows.Count + "/" +
                    packet.SocketRows.Count + "/" + packet.SlotRows.Count +
                " socket_bands=" + packet.SocketBandCount +
                " input=" + source.Slices.OutputDigest +
                " packet=" + packet.PacketDigest + " manifest=" + packet.ManifestDigest);
        }

        [Test]
        public void CsvExporterWritesDeterministicFilesWithStableHeaderOrder()
        {
            var packet = BuildPacket();
            var directory = NewExportDirectory();
            var result = GeneratedTerrainCsvExporter.Write(packet, directory);

            Assert.That(result.Success, Is.True, Failures(result));
            Assert.That(result.WrittenFileCount, Is.EqualTo(6));
            Assert.That(Directory.GetFiles(directory).Select(Path.GetFileName).OrderBy(value =>
                Array.IndexOf(GeneratedTerrainCsvExporter.RequiredFileNames.ToArray(), value)),
                Is.EqualTo(GeneratedTerrainCsvExporter.RequiredFileNames));
            foreach (var file in packet.Files)
            {
                var bytes = File.ReadAllBytes(Path.Combine(directory, file.FileName));
                Assert.That(bytes.Take(3), Is.Not.EqualTo(new byte[] { 0xef, 0xbb, 0xbf }));
                var text = File.ReadAllText(Path.Combine(directory, file.FileName));
                Assert.That(text, Is.EqualTo(file.Payload));
                Assert.That(text, Does.Not.Contain("\r"));
                Assert.That(text, Does.StartWith(file.Header + "\n"));
            }
        }

        [Test]
        public void ReplayVerifierRebuildsHashesFromExportedCsvWithoutAuthoringImport()
        {
            var packet = BuildPacket();
            var directory = NewExportDirectory();
            Assert.That(GeneratedTerrainCsvExporter.Write(packet, directory).Success, Is.True);

            var replay = GeneratedTerrainReplayVerifier.Verify(directory);

            Assert.That(replay.Success, Is.True, ReplayFailures(replay));
            Assert.That(replay.Files, Has.Count.EqualTo(6));
            Assert.That(replay.ManifestDigest, Is.EqualTo(packet.ManifestDigest));
            Assert.That(replay.ReplayDigest, Is.EqualTo(packet.PacketDigest));
            Assert.That(GeneratedTerrainExportDigest.IsLowerHexSha256(replay.ReplayDigest), Is.True);
            TestContext.WriteLine("MAP16_07_REPLAY_EVIDENCE files=6 replay=PASS digest=" +
                replay.ReplayDigest + " authoring_reverse_import_attempts=0");
        }

        [Test]
        public void CanvasAndSliceOverlaysCoverAllCellsSocketsAndSlots()
        {
            var packet = BuildPacket();
            var result = GeneratedTerrainDebugOverlay.Build(packet);

            Assert.That(result.Success, Is.True);
            var canvas = result.Canvas;
            Assert.That(canvas.Width, Is.EqualTo(48));
            Assert.That(canvas.Height, Is.EqualTo(32));
            Assert.That(canvas.Cells, Has.Count.EqualTo(1536));
            Assert.That(canvas.Slices, Has.Count.EqualTo(16));
            Assert.That(canvas.Slices.All(value => value.Width == 12 && value.Height == 8 &&
                value.Cells.Count == 96), Is.True);
            Assert.That(canvas.Slices.Sum(value => value.Cells.Count), Is.EqualTo(1536));
            Assert.That(canvas.Slots, Has.Count.EqualTo(24));
            Assert.That(canvas.Sockets, Has.Count.EqualTo(64));
            Assert.That(canvas.Sockets.Sum(value => value.BandCount), Is.EqualTo(packet.SocketBandCount));
            Assert.That(canvas.Legend.PassableCellCount +
                canvas.Cells.Count(value => !value.IsPassable), Is.EqualTo(1536));
            Assert.That(canvas.TextGrid.Count(value => value == '\n'), Is.EqualTo(32));
            Assert.That(canvas.Slices.All(value => value.TextGrid.Count(character => character == '\n') == 8),
                Is.True);

            TestContext.WriteLine("MAP16_07_OVERLAY_EVIDENCE canvas=1536/1536 slices=16/16" +
                " slice_cells=1536/1536 slots=24/24 sockets=" + canvas.Sockets.Count + "/64" +
                " socket_bands=" + packet.SocketBandCount + "/" + packet.SocketBandCount +
                " protected=" + canvas.Legend.ProtectedCellCount +
                " passable=" + canvas.Legend.PassableCellCount +
                " blocked=" + canvas.Legend.BlockedCellCount +
                " witness=" + canvas.Legend.WitnessCellCount);
        }

        [Test]
        public void ManifestRejectsMissingExtraTamperedOrMismatchedCsvFilesAtomically()
        {
            var missing = ExportFresh();
            File.Delete(Path.Combine(missing, GeneratedTerrainCsvExporter.CellsFileName));
            AssertRejected(missing, GeneratedTerrainExportFailureCode.MissingFile);

            var extra = ExportFresh();
            File.WriteAllText(Path.Combine(extra, "unexpected.csv"), "extra\n");
            AssertRejected(extra, GeneratedTerrainExportFailureCode.ExtraFile);

            var tampered = ExportFresh();
            var cellPath = Path.Combine(tampered, GeneratedTerrainCsvExporter.CellsFileName);
            var cellText = File.ReadAllText(cellPath);
            File.WriteAllText(cellPath, cellText.Substring(0, cellText.Length - 1) + "x\n");
            AssertRejected(tampered, GeneratedTerrainExportFailureCode.PayloadDigestMismatch);

            var mismatched = ExportFresh();
            var manifestPath = Path.Combine(mismatched, GeneratedTerrainCsvExporter.ManifestFileName);
            var manifestText = File.ReadAllText(manifestPath);
            var digest = BuildPacket().PacketDigest;
            var changed = (digest[0] == '0' ? "1" : "0") + digest.Substring(1);
            File.WriteAllText(manifestPath, manifestText.Replace(digest, changed));
            AssertRejected(mismatched, GeneratedTerrainExportFailureCode.PacketDigestMismatch);

            TestContext.WriteLine("MAP16_07_FAILURE_EVIDENCE missing=1 extra=1 tampered=1" +
                " mismatched=1 rejected=4 partial_success=0");
        }

        [Test]
        public void CellSocketSlotRowsPreserveCoordinatesLayerProvenanceAndDigests()
        {
            var source = ReferenceSource();
            var packet = BuildPacket();

            Assert.That(packet.CellRows.Select(value => value.SectorY * 48 + value.SectorX),
                Is.Ordered);
            Assert.That(packet.CellRows.Sum(value => value.LayerCount), Is.EqualTo(10752));
            Assert.That(packet.CellRows.All(value => value.LayerCount == 7 &&
                GeneratedTerrainExportDigest.IsLowerHexSha256(value.LayerDigest) &&
                GeneratedTerrainExportDigest.IsLowerHexSha256(value.WitnessDigest)), Is.True);
            Assert.That(packet.SocketRows.All(value =>
                GeneratedTerrainExportDigest.IsLowerHexSha256(value.SideSignature) &&
                GeneratedTerrainExportDigest.IsLowerHexSha256(value.SliceSignature) &&
                GeneratedTerrainExportDigest.IsLowerHexSha256(value.BandDigest)), Is.True);
            Assert.That(packet.SlotRows.All(value =>
                GeneratedTerrainExportDigest.IsLowerHexSha256(value.ProvenanceDigest) &&
                !string.IsNullOrEmpty(value.SourceLayerToken) &&
                !string.IsNullOrEmpty(value.SourceSignatureIdentity) &&
                !string.IsNullOrEmpty(value.SourceTraversalIdentity)), Is.True);
            foreach (var slot in packet.SlotRows)
            {
                var sourceSlot = source.Slots.Slots.Single(value => value.Id.Value == slot.SlotId);
                Assert.That(slot.SectorX, Is.EqualTo(sourceSlot.CellReference.SectorX));
                Assert.That(slot.SectorY, Is.EqualTo(sourceSlot.CellReference.SectorY));
                Assert.That(slot.SliceId, Is.EqualTo(sourceSlot.CellReference.SourceSliceId));
            }
        }

        [Test]
        public void ExportRoundTripIsStableAcrossRepeatReverseCultureAndTempPath()
        {
            var source = ReferenceSource();
            var first = GeneratedTerrainCsvExporter.Build(source.Slices, source.Slots);
            var repeat = GeneratedTerrainCsvExporter.Build(source.Slices, source.Slots);
            var reverse = GeneratedTerrainCsvExporter.Build(source.Slices, source.Slots,
                source.Slices.Slices.Reverse(), source.Slots.Slots.Reverse());
            Assert.That(first.Success && repeat.Success && reverse.Success, Is.True);
            Assert.That(repeat.Packet.PacketDigest, Is.EqualTo(first.Packet.PacketDigest));
            Assert.That(reverse.Packet.PacketDigest, Is.EqualTo(first.Packet.PacketDigest));

            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var culture = GeneratedTerrainCsvExporter.Build(source.Slices, source.Slots,
                    source.Slices.Slices.Reverse(), source.Slots.Slots.Reverse());
                Assert.That(culture.Success, Is.True, Failures(culture));
                Assert.That(culture.Packet.PacketDigest, Is.EqualTo(first.Packet.PacketDigest));
                Assert.That(culture.Packet.Files.Select(value => value.Payload), Is.EqualTo(
                    first.Packet.Files.Select(value => value.Payload)));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }

            var pathOne = NewExportDirectory();
            var pathTwo = NewExportDirectory();
            Assert.That(GeneratedTerrainCsvExporter.Write(first.Packet, pathOne).Success, Is.True);
            Assert.That(GeneratedTerrainCsvExporter.Write(first.Packet, pathTwo).Success, Is.True);
            Assert.That(GeneratedTerrainReplayVerifier.Verify(pathOne).ReplayDigest,
                Is.EqualTo(GeneratedTerrainReplayVerifier.Verify(pathTwo).ReplayDigest));
            TestContext.WriteLine("MAP16_07_DETERMINISM_EVIDENCE repeat=0 reverse=0 culture=0 temp_path=0");
        }

        [Test]
        public void ExporterDoesNotBakeTilemapsSpawnObjectsOrMutateScenesPrefabsGameObjects()
        {
            var source = ReferenceSource();
            var scene = SceneManager.GetActiveScene();
            var roots = scene.IsValid() ? scene.GetRootGameObjects().Select(value => value.GetInstanceID()).ToArray()
                : Array.Empty<int>();
            var wasDirty = scene.IsValid() && scene.isDirty;
            var packet = BuildPacket();
            var result = GeneratedTerrainCsvExporter.Write(packet, NewExportDirectory());

            Assert.That(result.Success, Is.True, Failures(result));
            Assert.That(scene.IsValid() ? scene.GetRootGameObjects().Select(value => value.GetInstanceID()) :
                Array.Empty<int>(), Is.EqualTo(roots));
            Assert.That(scene.IsValid() && scene.isDirty, Is.EqualTo(wasDirty));
            Assert.That(source.Slots.StableSpawnIdCount, Is.Zero);
            Assert.That(source.Slots.RuntimeObjectSpawnCount, Is.Zero);
            Assert.That(source.Slots.TilemapBakeCount, Is.Zero);
            Assert.That(source.Slots.TilemapMutationCount, Is.Zero);
            Assert.That(source.Slots.SceneMutationCount, Is.Zero);
            Assert.That(source.Slots.PrefabMutationCount, Is.Zero);
            Assert.That(source.Slots.GameObjectMutationCount, Is.Zero);
            Assert.That(GeneratedTerrainCsvExporter.Write(packet, Application.dataPath).Success, Is.False);
            TestContext.WriteLine("MAP16_07_MUTATION_EVIDENCE stable_spawn=0 runtime_objects=0" +
                " tilemap_bakes=0 tilemap_scene_prefab_gameobject=0/0/0/0 permanent_assets=0");
        }

        [Test]
        public void ExporterDoesNotMutateSourceSliceOrMarkerSlotPackets()
        {
            var source = ReferenceSource();
            var sliceInput = source.Slices.InputDigest;
            var sliceOutput = source.Slices.OutputDigest;
            var slotInput = source.Slots.InputDigest;
            var slotOutput = source.Slots.OutputDigest;
            var slices = source.Slices.Slices.Select(value => value.Id.Value + "|" +
                value.Signature.Digest).ToArray();
            var slots = source.Slots.Slots.Select(value => value.StableToken).ToArray();

            var packet = GeneratedTerrainCsvExporter.Build(source.Slices, source.Slots);
            Assert.That(packet.Success, Is.True, Failures(packet));
            Assert.That(GeneratedTerrainDebugOverlay.Build(packet.Packet).Success, Is.True);

            Assert.That(source.Slices.InputDigest, Is.EqualTo(sliceInput));
            Assert.That(source.Slices.OutputDigest, Is.EqualTo(sliceOutput));
            Assert.That(source.Slots.InputDigest, Is.EqualTo(slotInput));
            Assert.That(source.Slots.OutputDigest, Is.EqualTo(slotOutput));
            Assert.That(source.Slices.Slices.Select(value => value.Id.Value + "|" +
                value.Signature.Digest), Is.EqualTo(slices));
            Assert.That(source.Slots.Slots.Select(value => value.StableToken), Is.EqualTo(slots));
            Assert.That(source.Slots.SourceSliceMutationCount, Is.Zero);
            TestContext.WriteLine("MAP16_07_SOURCE_MUTATION_EVIDENCE slice=0 slot=0");
        }

        [Test]
        public void Map16HandoffKeepsMap16_08Locked()
        {
            Assert.That(GeneratedTerrainExportPacket.DownstreamOwner,
                Is.EqualTo("MAP16_08_MAP16_SLICE_AND_OUTPUT_EXIT_TESTS"));
            Assert.That(GeneratedTerrainExportPacket.OpensDownstreamTask, Is.False);
            Assert.That(GeneratedTerrainExportPacket.ReferencePublicationLabel,
                Is.EqualTo("REFERENCE GENERATED TERRAIN EXPORT"));
            Assert.That(ReferenceSource().Slices.ProductionSeedApprovalCount, Is.Zero);
        }

        private GeneratedTerrainExportPacket BuildPacket()
        {
            var source = ReferenceSource();
            var result = GeneratedTerrainCsvExporter.Build(source.Slices, source.Slots);
            Assert.That(result.Success, Is.True, Failures(result));
            return result.Packet;
        }

        private string NewExportDirectory()
        {
            var root = Path.Combine(Path.GetTempPath(), "map16_07_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            temporaryRoots.Add(root);
            return Path.Combine(root, "export");
        }

        private string ExportFresh()
        {
            var directory = NewExportDirectory();
            var result = GeneratedTerrainCsvExporter.Write(BuildPacket(), directory);
            Assert.That(result.Success, Is.True, Failures(result));
            return directory;
        }

        private static void AssertRejected(
            string directory, GeneratedTerrainExportFailureCode expected)
        {
            var result = GeneratedTerrainReplayVerifier.Verify(directory);
            Assert.That(result.Success, Is.False);
            Assert.That(result.ReplayDigest, Is.Empty);
            Assert.That(result.Failures.Select(value => value.Code), Does.Contain(expected),
                ReplayFailures(result));
        }

        private static SourcePair ReferenceSource()
        {
            if (reference != null) return reference;
            var fixture = new ReferenceFinalRouteRecoveryFixture();
            var baselineRouteRequest = fixture.AcceptedRequest();
            var baselinePlan = baselineRouteRequest.CanvasPlan;
            var baselineRequest = baselinePlan.Request;
            var claims = baselinePlan.Cells.SelectMany(value => value.Winners).ToList();
            claims.Add(Claim("MAP16_07_TERRAIN_CLUSTER", 12, 20,
                FinalCanvasLayerKind.Terrain, FinalCanvasCellKind.Air,
                FinalCanvasSourceOwner.TerrainCluster,
                FinalCanvasClaimPriority.TerrainClusterPattern, "MAP11_TERRAIN_CLUSTER"));
            claims.Add(Claim("MAP16_07_ACTIVITY", 12, 20,
                FinalCanvasLayerKind.Marker, FinalCanvasCellKind.Marker,
                FinalCanvasSourceOwner.Activity,
                FinalCanvasClaimPriority.ActivityMarker, "MAP12_ACTIVITY"));
            claims.Add(Claim("MAP16_07_EVENT_OVERLAY", 13, 20,
                FinalCanvasLayerKind.Marker, FinalCanvasCellKind.Marker,
                FinalCanvasSourceOwner.EventOverlay,
                FinalCanvasClaimPriority.EventMarker, "MAP12_EVENT_OVERLAY"));

            var canvasResult = SectorCanvasLayerFinalizer.Finalize(new FinalCanvasLayerRequest(
                baselineRequest.SectorId, baselineRequest.Width, baselineRequest.Height, claims,
                baselineRequest.Map15ExitApproved, baselineRequest.Map15ExitDigest,
                baselineRequest.WorldAssemblyDigest, baselineRequest.SectorOwnershipDigest,
                baselineRequest.BoundaryAuthorityDigest, baselineRequest.FixedCanvasAuthorityDigest,
                baselineRequest.PublicationLabel));
            Assert.That(canvasResult.Success, Is.True, Join(canvasResult.Failures));
            var densityResult = SectorCanvasProtectionDensityValidator.Validate(canvasResult.Plan);
            Assert.That(densityResult.Success, Is.True, Join(densityResult.Failures));
            var routeResult = SectorFinalRouteRecoveryValidator.Validate(
                new FinalRouteRecoveryRequest(canvasResult.Plan, densityResult.Report,
                    baselineRouteRequest.Anchors, baselineRouteRequest.DeclaredEdges,
                    baselineRouteRequest.PublicationLabel));
            Assert.That(routeResult.Success, Is.True, Join(routeResult.Failures));
            var partitionResult = SectorPatternChunkPartitioner.Partition(
                canvasResult.Plan, densityResult.Report, routeResult.Report);
            Assert.That(partitionResult.Success, Is.True, Join(partitionResult.Failures));
            var sliceResult = GeneratedMicroChunkSliceBuilder.Build(
                canvasResult.Plan, densityResult.Report, routeResult.Report,
                partitionResult.Partition);
            Assert.That(sliceResult.Success, Is.True, Join(sliceResult.Failures));
            var slotResult = GeneratedMicroChunkMarkerSlotProjector.Project(sliceResult.SliceSet);
            Assert.That(slotResult.Success, Is.True, Join(slotResult.Failures));
            Assert.That(slotResult.SlotSet.SlotCount, Is.EqualTo(24));
            reference = new SourcePair(sliceResult.SliceSet, slotResult.SlotSet);
            return reference;
        }

        private static FinalCanvasLayerClaim Claim(
            string claimId,
            int x,
            int y,
            FinalCanvasLayerKind layer,
            FinalCanvasCellKind cellKind,
            FinalCanvasSourceOwner owner,
            FinalCanvasClaimPriority priority,
            string provenanceId) => new FinalCanvasLayerClaim(
                claimId, new FinalCanvasCellCoordinate(x, y), layer, cellKind, owner, priority,
                FinalCanvasProtectionKind.None, false, provenanceId,
                GeneratedMicroChunkMarkerSlotSet.ReferencePublicationLabel);

        private static string Failures(GeneratedTerrainExportResult result) => result == null
            ? "NULL RESULT" : Join(result.Failures);
        private static string ReplayFailures(GeneratedTerrainReplayResult result) => result == null
            ? "NULL RESULT" : Join(result.Failures);
        private static string Join<T>(IEnumerable<T> values) => string.Join(";",
            values.Select(value => value == null ? "NULL" : value.ToString()));

        private sealed class SourcePair
        {
            public SourcePair(
                GeneratedMicroChunkSliceSet slices,
                GeneratedMicroChunkMarkerSlotSet slots)
            {
                Slices = slices; Slots = slots;
            }
            public GeneratedMicroChunkSliceSet Slices { get; }
            public GeneratedMicroChunkMarkerSlotSet Slots { get; }
        }
    }
}
