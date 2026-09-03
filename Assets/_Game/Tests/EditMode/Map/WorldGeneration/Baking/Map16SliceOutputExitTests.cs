using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Baking
{
    [TestFixture]
    [Category("MAP16_08")]
    public sealed class Map16SliceOutputExitTests
    {
        private const string FixtureLabel = "REFERENCE MAP16 SLICE OUTPUT EXIT";
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
                    Assert.Fail("MAP16_08 temporary CSV cleanup failed for " + root + ": " + exception);
                }
            }

            temporaryRoots.Clear();
        }

        [Test]
        public void CurrentMap16ChainPublishesAllRequiredArtifactsForExit()
        {
            var chain = ReferenceChain();
            var artifacts = new object[]
            {
                chain.Canvas, chain.Density, chain.Route, chain.Partition,
                chain.Slices, chain.Slots, chain.Packet,
            };
            var digests = new[]
            {
                chain.Canvas.InputDigest, chain.Canvas.OutputDigest,
                chain.Density.InputDigest, chain.Density.OutputDigest,
                chain.Route.InputDigest, chain.Route.OutputDigest,
                chain.Partition.InputDigest, chain.Partition.OutputDigest,
                chain.Slices.InputDigest, chain.Slices.OutputDigest,
                chain.Slots.InputDigest, chain.Slots.OutputDigest,
                chain.Packet.ManifestDigest, chain.Packet.PacketDigest,
            };
            var audit = RunAudit(chain);

            Assert.That(artifacts.Count(value => value != null), Is.EqualTo(7));
            Assert.That(digests.All(GeneratedTerrainExportDigest.IsLowerHexSha256), Is.True);
            Assert.That(chain.Density.SourcePlan, Is.SameAs(chain.Canvas));
            Assert.That(chain.Route.SourceCanvasPlan, Is.SameAs(chain.Canvas));
            Assert.That(chain.Partition.SourceRouteRecoveryReport, Is.SameAs(chain.Route));
            Assert.That(chain.Slices.SourcePartition, Is.SameAs(chain.Partition));
            Assert.That(chain.Slots.SourceSliceSet, Is.SameAs(chain.Slices));
            Assert.That(chain.Packet.Manifest.SourceSliceSetDigest, Is.EqualTo(chain.Slices.OutputDigest));
            Assert.That(chain.Packet.Manifest.SourceMarkerSlotSetDigest, Is.EqualTo(chain.Slots.OutputDigest));
            Assert.That(audit.Approved, Is.True, Describe(audit.Failures));

            TestContext.WriteLine("MAP16_08_CHAIN_EVIDENCE artifacts=7/7/0" +
                " canvas=" + chain.Canvas.OutputDigest +
                " density=" + chain.Density.OutputDigest +
                " route=" + chain.Route.OutputDigest +
                " partition=" + chain.Partition.OutputDigest +
                " slices=" + chain.Slices.OutputDigest +
                " slots=" + chain.Slots.OutputDigest +
                " manifest=" + chain.Packet.ManifestDigest +
                " packet=" + chain.Packet.PacketDigest +
                " exit=" + audit.Digest);
        }

        [Test]
        public void SectorSliceCoverageHasSixteenNinetySixCellSlicesAndNoCoordinateGaps()
        {
            var set = ReferenceChain().Slices;

            Assert.That(GeneratedMicroChunkSliceSet.SectorWidth, Is.EqualTo(48));
            Assert.That(GeneratedMicroChunkSliceSet.SectorHeight, Is.EqualTo(32));
            Assert.That(GeneratedMicroChunkSliceSet.SectorCellCount, Is.EqualTo(1536));
            Assert.That(set.SliceCount, Is.EqualTo(16));
            Assert.That(set.Slices.All(value => value.Width == 12 && value.Height == 8), Is.True);
            Assert.That(set.Slices.All(value => value.CellCount == 96), Is.True);
            Assert.That(set.Slices.All(value => value.Cells.Select(cell => cell.LocalCoordinate)
                .Distinct().Count() == 96), Is.True);
            Assert.That(set.TotalCellCount, Is.EqualTo(1536));
            Assert.That(set.UniqueSectorCellCount, Is.EqualTo(1536));
            Assert.That(set.DuplicateSectorCellCount, Is.Zero);
            Assert.That(set.MissingSectorCellCount, Is.Zero);
            Assert.That(set.OutOfBoundsSectorCellCount, Is.Zero);
            Assert.That(set.TotalLayerRecordCount, Is.EqualTo(10752));

            TestContext.WriteLine("MAP16_08_SLICE_EVIDENCE sector=48x32 cells=1536/1536" +
                " slices=16/16 dimensions=12x8 cells_per_slice=96/96" +
                " total_slice_cells=1536/1536 layers=10752/10752 duplicate=0 missing=0 oob=0");
        }

        [Test]
        public void ReferenceWorldProjectionCoversOneHundredSixtyNineSectorsWithoutBaking()
        {
            var projection = ProjectWorld(ReferenceChain().Slices.Slices, false);

            Assert.That(WorldGenConstants.SectorColumns, Is.EqualTo(13));
            Assert.That(WorldGenConstants.SectorRows, Is.EqualTo(13));
            Assert.That(projection.SectorCount, Is.EqualTo(169));
            Assert.That(projection.Width, Is.EqualTo(624));
            Assert.That(projection.Height, Is.EqualTo(416));
            Assert.That(projection.SliceCount, Is.EqualTo(2704));
            Assert.That(projection.CellCount, Is.EqualTo(259584));
            Assert.That(projection.UniqueCellCount, Is.EqualTo(259584));
            Assert.That(projection.DuplicateCellCount, Is.Zero);
            Assert.That(projection.MissingCellCount, Is.Zero);
            Assert.That(projection.OutOfBoundsCellCount, Is.Zero);
            Assert.That(ReferenceChain().Slices.TilemapBakeCount, Is.Zero);
            Assert.That(ReferenceChain().Slices.ProductionSeedApprovalCount, Is.Zero);

            TestContext.WriteLine("MAP16_08_WORLD_PROJECTION_EVIDENCE sectors=169/169 grid=13x13" +
                " world=624x416 slices=2704/2704 cells=259584/259584 duplicate/missing/oob=0/0/0" +
                " tilemap_bakes=0 production_seed_approvals=0");
        }

        [Test]
        public void SocketBandsSignaturesAndInternalNeighborCompatibilityRemainValid()
        {
            var chain = ReferenceChain();
            var stats = InspectSockets(chain.Slices.Slices, chain.Packet, false);

            Assert.That(chain.Slices.SocketSideSignatureCount, Is.EqualTo(64));
            Assert.That(stats.ObservedSideSignatureCount, Is.EqualTo(64));
            Assert.That(stats.InternalAdjacencyCheckCount, Is.EqualTo(24));
            Assert.That(stats.InternalAdjacencyMismatchCount, Is.Zero);
            Assert.That(stats.SourceSocketBandCount, Is.EqualTo(chain.Slices.SocketBandCount));
            Assert.That(stats.ExportedSocketBandCount, Is.EqualTo(chain.Slices.SocketBandCount));
            Assert.That(stats.ExternalSourceRecordCount, Is.EqualTo(16));
            Assert.That(stats.ExternalExportRecordCount, Is.EqualTo(16));
            Assert.That(stats.SocketDigestMismatchCount, Is.Zero);

            TestContext.WriteLine("MAP16_08_SOCKET_EVIDENCE signatures=64/64 bands=" +
                stats.SourceSocketBandCount + "/" + stats.ExportedSocketBandCount +
                " internal_checks=24/24 internal_mismatches=0 external_records=16/16" +
                " digest_mismatches=0");
        }

        [Test]
        public void CsvExportReplayRoundTripPreservesManifestPacketAndOverlayDigests()
        {
            var chain = ReferenceChain();
            var directory = ExportFresh(chain.Packet);
            var replay = GeneratedTerrainReplayVerifier.Verify(directory);
            var overlay = GeneratedTerrainDebugOverlay.Build(chain.Packet);
            var audit = RunAudit(chain, new AuditProbe
            {
                ReplaySuccess = replay.Success,
                ReplayManifestDigest = replay.ManifestDigest,
                ReplayPacketDigest = replay.ReplayDigest,
            });

            Assert.That(replay.Success, Is.True, ReplayFailures(replay));
            Assert.That(replay.Files, Has.Count.EqualTo(6));
            Assert.That(replay.ManifestDigest, Is.EqualTo(chain.Packet.ManifestDigest));
            Assert.That(replay.ReplayDigest, Is.EqualTo(chain.Packet.PacketDigest));
            Assert.That(overlay.Success, Is.True, Describe(overlay.Failures));
            Assert.That(overlay.Canvas.Cells, Has.Count.EqualTo(1536));
            Assert.That(overlay.Canvas.Slices, Has.Count.EqualTo(16));
            Assert.That(overlay.Canvas.Slices.Sum(value => value.Cells.Count), Is.EqualTo(1536));
            Assert.That(overlay.Canvas.Slots, Has.Count.EqualTo(24));
            Assert.That(overlay.Canvas.Sockets, Has.Count.EqualTo(64));
            Assert.That(overlay.Canvas.Sockets.Sum(value => value.BandCount),
                Is.EqualTo(chain.Packet.SocketBandCount));
            Assert.That(audit.Approved, Is.True, Describe(audit.Failures));

            var missing = ExportFresh(chain.Packet);
            File.Delete(Path.Combine(missing, GeneratedTerrainCsvExporter.CellsFileName));
            AssertReplayRejected(missing, GeneratedTerrainExportFailureCode.MissingFile);

            var extra = ExportFresh(chain.Packet);
            File.WriteAllText(Path.Combine(extra, "unexpected.csv"), "extra\n");
            AssertReplayRejected(extra, GeneratedTerrainExportFailureCode.ExtraFile);

            var tampered = ExportFresh(chain.Packet);
            var cellPath = Path.Combine(tampered, GeneratedTerrainCsvExporter.CellsFileName);
            var cellText = File.ReadAllText(cellPath);
            File.WriteAllText(cellPath,
                cellText.Substring(0, cellText.Length - 1) + "x\n");
            AssertReplayRejected(tampered, GeneratedTerrainExportFailureCode.PayloadDigestMismatch);

            var mismatched = ExportFresh(chain.Packet);
            var manifestPath = Path.Combine(mismatched, GeneratedTerrainCsvExporter.ManifestFileName);
            var manifestText = File.ReadAllText(manifestPath);
            var changedDigest = (chain.Packet.PacketDigest[0] == '0' ? "1" : "0") +
                chain.Packet.PacketDigest.Substring(1);
            File.WriteAllText(manifestPath, manifestText.Replace(
                chain.Packet.PacketDigest, changedDigest));
            AssertReplayRejected(mismatched, GeneratedTerrainExportFailureCode.PacketDigestMismatch);

            TestContext.WriteLine("MAP16_08_REPLAY_EVIDENCE files=6/6 replay=PASS" +
                " manifest=" + replay.ManifestDigest + " packet=" + replay.ReplayDigest +
                " failure_probes=missing/extra/tampered/mismatched" +
                " canvas=1536/1536 slice_overlays=16/16 slice_cells=1536/1536" +
                " slots=24/24 sockets=64/64 authoring_reverse_import_attempts=0 permanent_assets=0");
        }

        [Test]
        public void LayerSourceMarkerSlotAndProvenanceCoverageRemainComplete()
        {
            var chain = ReferenceChain();
            var layers = chain.Slices.Slices.SelectMany(value => value.Cells)
                .SelectMany(value => value.Layers).ToArray();
            var slots = chain.Slots.Slots;

            Assert.That(layers, Has.Length.EqualTo(10752));
            Assert.That(layers.All(value => value.SourceOwner != FinalCanvasSourceOwner.Unknown), Is.True);
            Assert.That(layers.All(value => !string.IsNullOrEmpty(value.ProvenanceId) &&
                !string.IsNullOrEmpty(value.ClaimId) && !string.IsNullOrEmpty(value.SourceCellToken)), Is.True);
            Assert.That(chain.Slices.LayerRecordsWithSourceOwnerCount, Is.EqualTo(10752));
            Assert.That(chain.Slices.LayerRecordsWithProvenanceCount, Is.EqualTo(10752));
            Assert.That(chain.Slots.SlotCount, Is.EqualTo(24));
            Assert.That(chain.Slots.SlotsWithCellReferenceCount, Is.EqualTo(24));
            Assert.That(chain.Slots.SlotsWithProvenanceCount, Is.EqualTo(24));
            Assert.That(chain.Slots.SlotsPreservingSourceLayerIdentityCount, Is.EqualTo(24));
            Assert.That(chain.Slots.SlotsPreservingSocketSignatureTraversalIdentityCount,
                Is.EqualTo(24));
            Assert.That(chain.Slots.MissingProvenanceCount, Is.Zero);
            Assert.That(slots.All(value => value.CellReference.IsComplete &&
                value.Provenance.IsComplete && !string.IsNullOrEmpty(value.SourceKey)), Is.True);

            TestContext.WriteLine("MAP16_08_PROVENANCE_EVIDENCE layers=10752/10752" +
                " source_owner=10752/10752 provenance=10752/10752 slots=24/24" +
                " slots_with_provenance=24/24 missing_provenance=0");
        }

        [Test]
        public void ExitAuditRejectsCoverageSocketReplayAndProvenanceContradictionsAtomically()
        {
            var chain = ReferenceChain();
            var cells = SectorCells(chain.Slices.Slices).ToArray();
            var duplicateAndMissing = cells.Take(cells.Length - 1).Concat(new[] { cells[0] }).ToArray();
            var outOfBounds = cells.Take(cells.Length - 1)
                .Concat(new[] { new CellProbe(-1, 0) }).ToArray();
            var failures = new[]
            {
                RunAudit(chain, new AuditProbe { Slices = chain.Slices.Slices.Take(15).ToArray() }),
                RunAudit(chain, new AuditProbe { SectorCells = duplicateAndMissing }),
                RunAudit(chain, new AuditProbe { SectorCells = outOfBounds }),
                RunAudit(chain, new AuditProbe { ForceSocketMismatch = true }),
                RunAudit(chain, new AuditProbe { ForceMissingLayerProvenance = true }),
                RunAudit(chain, new AuditProbe { ForceMissingMarkerSource = true }),
                RunAudit(chain, new AuditProbe { ReplaySuccess = false }),
            };

            Assert.That(failures.All(value => !value.Approved), Is.True);
            Assert.That(failures.All(value => string.IsNullOrEmpty(value.Digest)), Is.True);
            Assert.That(failures.All(value => !value.OpensMap17), Is.True);
            Assert.That(failures.SelectMany(value => value.Failures), Does.Contain("SLICE_COUNT"));
            Assert.That(failures.SelectMany(value => value.Failures), Does.Contain("DUPLICATE_CELL"));
            Assert.That(failures.SelectMany(value => value.Failures), Does.Contain("OUT_OF_BOUNDS_CELL"));
            Assert.That(failures.SelectMany(value => value.Failures), Does.Contain("SOCKET_ADJACENCY"));
            Assert.That(failures.SelectMany(value => value.Failures), Does.Contain("LAYER_PROVENANCE"));
            Assert.That(failures.SelectMany(value => value.Failures), Does.Contain("MARKER_SOURCE"));
            Assert.That(failures.SelectMany(value => value.Failures), Does.Contain("CSV_REPLAY"));

            TestContext.WriteLine("MAP16_08_ATOMIC_FAILURE_EVIDENCE missing_slice=1" +
                " duplicate_missing_cell=1 out_of_bounds_cell=1 socket_mismatch=1" +
                " missing_provenance=1 missing_marker_source=1 tampered_replay=1" +
                " partial_approvals=0 map17_opens=0");
        }

        [Test]
        public void ExitAuditDigestIsStableAcrossRepeatReverseCultureAndTempPath()
        {
            var chain = ReferenceChain();
            var first = RunAudit(chain);
            var repeat = RunAudit(chain);
            var reverse = RunAudit(chain, new AuditProbe
            {
                Slices = chain.Slices.Slices.Reverse().ToArray(),
                SectorCells = SectorCells(chain.Slices.Slices).Reverse().ToArray(),
                ReverseWorldProjection = true,
            });
            Assert.That(first.Approved && repeat.Approved && reverse.Approved, Is.True);
            Assert.That(repeat.Digest, Is.EqualTo(first.Digest));
            Assert.That(reverse.Digest, Is.EqualTo(first.Digest));

            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var culture = RunAudit(chain, new AuditProbe
                {
                    Slices = chain.Slices.Slices.Reverse().ToArray(),
                    SectorCells = SectorCells(chain.Slices.Slices).Reverse().ToArray(),
                    ReverseWorldProjection = true,
                });
                Assert.That(culture.Approved, Is.True, Describe(culture.Failures));
                Assert.That(culture.Digest, Is.EqualTo(first.Digest));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }

            var pathOne = ExportFresh(chain.Packet);
            var pathTwo = ExportFresh(chain.Packet);
            var replayOne = GeneratedTerrainReplayVerifier.Verify(pathOne);
            var replayTwo = GeneratedTerrainReplayVerifier.Verify(pathTwo);
            var pathAuditOne = AuditWithReplay(chain, replayOne);
            var pathAuditTwo = AuditWithReplay(chain, replayTwo);
            Assert.That(pathAuditOne.Approved && pathAuditTwo.Approved, Is.True);
            Assert.That(pathAuditOne.Digest, Is.EqualTo(first.Digest));
            Assert.That(pathAuditTwo.Digest, Is.EqualTo(first.Digest));

            TestContext.WriteLine("MAP16_08_DETERMINISM_EVIDENCE digest=" + first.Digest +
                " repeat/reverse/culture/temp_path_mismatches=0/0/0/0");
        }

        [Test]
        public void NoRegressionSelectionOrTilemapScenePrefabGameplayMutationOccurs()
        {
            var chain = ReferenceChain();
            var scene = SceneManager.GetActiveScene();
            var roots = scene.IsValid()
                ? scene.GetRootGameObjects().Select(value => value.GetInstanceID()).ToArray()
                : Array.Empty<int>();
            var wasDirty = scene.IsValid() && scene.isDirty;
            var audit = RunAudit(chain);

            Assert.That(audit.Approved, Is.True, Describe(audit.Failures));
            Assert.That(audit.PriorTaskSelections, Is.Zero);
            Assert.That(audit.LegacyRegressionSelections, Is.Zero);
            Assert.That(audit.PlayModeSelections, Is.Zero);
            Assert.That(audit.UnfilteredSelections, Is.Zero);
            Assert.That(audit.FullRegressionRuns, Is.Zero);
            Assert.That(chain.Slices.FullRegressionCount, Is.Zero);
            Assert.That(chain.Slices.TilemapBakeCount, Is.Zero);
            Assert.That(chain.Slices.TilemapMutationCount, Is.Zero);
            Assert.That(chain.Slices.SceneMutationCount, Is.Zero);
            Assert.That(chain.Slices.PrefabMutationCount, Is.Zero);
            Assert.That(chain.Slices.GameObjectMutationCount, Is.Zero);
            Assert.That(chain.Slices.GameplaySpawnCount, Is.Zero);
            Assert.That(chain.Slots.StableSpawnIdCount, Is.Zero);
            Assert.That(chain.Slots.RuntimeObjectSpawnCount, Is.Zero);
            Assert.That(chain.Slots.ProductionSeedApprovalCount, Is.Zero);
            Assert.That(scene.IsValid() ? scene.GetRootGameObjects().Select(value => value.GetInstanceID())
                : Array.Empty<int>(), Is.EqualTo(roots));
            Assert.That(scene.IsValid() && scene.isDirty, Is.EqualTo(wasDirty));
            Assert.That(GeneratedTerrainCsvExporter.RequiredFileNames.All(value =>
                !File.Exists(Path.Combine(Application.dataPath, value))), Is.True);

            TestContext.WriteLine("MAP16_08_MUTATION_EVIDENCE prior=0 legacy=0 playmode=0" +
                " unfiltered=0 full_regression=0 stable_spawn=0 runtime_objects=0 tilemap_bakes=0" +
                " tilemap_scene_prefab_gameobject=0/0/0/0 production_seed_approvals=0");
        }

        [Test]
        public void Map17HandoffKeepsRuntimeBakeLocked()
        {
            var chain = ReferenceChain();
            var audit = RunAudit(chain);

            Assert.That(audit.Approved, Is.True, Describe(audit.Failures));
            Assert.That(audit.OpensMap17, Is.False);
            Assert.That(audit.RuntimeBakeLocked, Is.True);
            Assert.That(GeneratedTerrainExportPacket.DownstreamOwner,
                Is.EqualTo("MAP16_08_MAP16_SLICE_AND_OUTPUT_EXIT_TESTS"));
            Assert.That(GeneratedTerrainExportPacket.OpensDownstreamTask, Is.False);
            Assert.That(chain.Slices.TilemapBakeCount, Is.Zero);
            Assert.That(chain.Slots.RuntimeObjectSpawnCount, Is.Zero);

            TestContext.WriteLine("MAP16_08_EXIT_EVIDENCE verdict=PASS map17_automatic_open=false" +
                " runtime_bake_locked=true editor_visible_change=NONE game_visible_change=NONE");
        }

        private string ExportFresh(GeneratedTerrainExportPacket packet)
        {
            var root = Path.Combine(Path.GetTempPath(), "map16_08_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            temporaryRoots.Add(root);
            var directory = Path.Combine(root, "export");
            var result = GeneratedTerrainCsvExporter.Write(packet, directory);
            Assert.That(result.Success, Is.True, ExportFailures(result));
            return directory;
        }

        private static void AssertReplayRejected(
            string directory,
            GeneratedTerrainExportFailureCode expected)
        {
            var replay = GeneratedTerrainReplayVerifier.Verify(directory);
            Assert.That(replay.Success, Is.False);
            Assert.That(replay.ReplayDigest, Is.Empty);
            Assert.That(replay.Failures.Select(value => value.Code), Does.Contain(expected),
                ReplayFailures(replay));
        }

        private static ExitAuditResult AuditWithReplay(
            Map16Chain chain,
            GeneratedTerrainReplayResult replay) => RunAudit(chain, new AuditProbe
            {
                ReplaySuccess = replay.Success,
                ReplayManifestDigest = replay.ManifestDigest,
                ReplayPacketDigest = replay.ReplayDigest,
            });

        private static ExitAuditResult RunAudit(Map16Chain chain, AuditProbe probe = null)
        {
            probe = probe ?? new AuditProbe();
            var failures = new List<string>();
            var artifacts = new object[]
            {
                chain == null ? null : chain.Canvas,
                chain == null ? null : chain.Density,
                chain == null ? null : chain.Route,
                chain == null ? null : chain.Partition,
                chain == null ? null : chain.Slices,
                chain == null ? null : chain.Slots,
                chain == null ? null : chain.Packet,
            };
            if (artifacts.Count(value => value != null) != 7)
                failures.Add("ARTIFACT_CHAIN");
            if (failures.Count > 0)
                return ExitAuditResult.Failed(failures);

            var slices = (probe.Slices ?? chain.Slices.Slices).Where(value => value != null).ToArray();
            var cells = (probe.SectorCells ?? SectorCells(slices)).ToArray();
            var inBounds = cells.Where(value => value.X >= 0 && value.X < 48 &&
                value.Y >= 0 && value.Y < 32).ToArray();
            var uniqueAll = cells.Select(value => CoordinateKey(value.X, value.Y)).Distinct().Count();
            var uniqueInBounds = inBounds.Select(value => CoordinateKey(value.X, value.Y)).Distinct().Count();
            var duplicates = cells.Length - uniqueAll;
            var missing = 1536 - uniqueInBounds;
            var outOfBounds = cells.Length - inBounds.Length;
            if (slices.Length != 16) failures.Add("SLICE_COUNT");
            if (slices.Any(value => value.Width != 12 || value.Height != 8 ||
                value.CellCount != 96 || value.LayerRecordCount != 672))
                failures.Add("SLICE_SHAPE");
            if (cells.Length != 1536 || missing != 0) failures.Add("MISSING_CELL");
            if (duplicates != 0) failures.Add("DUPLICATE_CELL");
            if (outOfBounds != 0) failures.Add("OUT_OF_BOUNDS_CELL");
            if (chain.Slices.TotalLayerRecordCount != 10752 ||
                chain.Slices.LayerRecordsWithSourceOwnerCount != 10752 ||
                chain.Slices.LayerRecordsWithProvenanceCount != 10752 ||
                probe.ForceMissingLayerProvenance)
                failures.Add("LAYER_PROVENANCE");

            var socketStats = InspectSockets(slices, chain.Packet, probe.ForceSocketMismatch);
            if (socketStats.ObservedSideSignatureCount != 64 ||
                socketStats.SocketDigestMismatchCount != 0)
                failures.Add("SOCKET_SIGNATURE");
            if (socketStats.InternalAdjacencyCheckCount != 24 ||
                socketStats.InternalAdjacencyMismatchCount != 0)
                failures.Add("SOCKET_ADJACENCY");
            if (socketStats.ExternalSourceRecordCount != 16 ||
                socketStats.ExternalExportRecordCount != 16 ||
                socketStats.SourceSocketBandCount != socketStats.ExportedSocketBandCount)
                failures.Add("SOCKET_EXPORT");

            if (chain.Slots.SlotCount != 24 || chain.Slots.SlotsWithProvenanceCount != 24 ||
                chain.Slots.MissingProvenanceCount != 0)
                failures.Add("MARKER_PROVENANCE");
            if (probe.ForceMissingMarkerSource || chain.Slots.Slots.Any(value =>
                    string.IsNullOrEmpty(value.SourceKey) || value.Provenance == null ||
                    string.IsNullOrEmpty(value.Provenance.SourceTaskId)))
                failures.Add("MARKER_SOURCE");

            var replayManifest = probe.ReplayManifestDigest ?? chain.Packet.ManifestDigest;
            var replayPacket = probe.ReplayPacketDigest ?? chain.Packet.PacketDigest;
            if (!probe.ReplaySuccess || replayManifest != chain.Packet.ManifestDigest ||
                replayPacket != chain.Packet.PacketDigest || chain.Packet.LogicalFileCount != 6)
                failures.Add("CSV_REPLAY");
            if (chain.Overlay.Canvas == null || chain.Overlay.Canvas.Cells.Count != 1536 ||
                chain.Overlay.Canvas.Slices.Count != 16 ||
                chain.Overlay.Canvas.Slices.Sum(value => value.Cells.Count) != 1536 ||
                chain.Overlay.Canvas.Slots.Count != 24 || chain.Overlay.Canvas.Sockets.Count != 64)
                failures.Add("OVERLAY_COVERAGE");

            var world = ProjectWorld(slices, probe.ReverseWorldProjection);
            if (world.SectorCount != 169 || world.SliceCount != 2704 ||
                world.CellCount != 259584 || world.UniqueCellCount != 259584 ||
                world.DuplicateCellCount != 0 || world.MissingCellCount != 0 ||
                world.OutOfBoundsCellCount != 0)
                failures.Add("WORLD_PROJECTION");

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
            if (digestValues.Any(value => !GeneratedTerrainExportDigest.IsLowerHexSha256(value)))
                failures.Add("DIGEST_FORMAT");
            if (chain.Slices.TilemapBakeCount != 0 || chain.Slices.TilemapMutationCount != 0 ||
                chain.Slices.SceneMutationCount != 0 || chain.Slices.PrefabMutationCount != 0 ||
                chain.Slices.GameObjectMutationCount != 0 || chain.Slices.GameplaySpawnCount != 0 ||
                chain.Slices.ProductionSeedApprovalCount != 0)
                failures.Add("FORBIDDEN_MUTATION");

            if (failures.Count > 0) return ExitAuditResult.Failed(failures);
            var canonical = string.Join("\n", new[]
            {
                "POLICY|MAP16_08_SLICE_OUTPUT_EXIT_V1",
                "FIXTURE|" + FixtureLabel,
                "DIGESTS|" + string.Join("|", digestValues),
                "SECTOR|48|32|1536|16|12|8|96|10752",
                "SOCKETS|" + Number(socketStats.ObservedSideSignatureCount) + "|" +
                    Number(socketStats.SourceSocketBandCount) + "|" +
                    Number(socketStats.InternalAdjacencyCheckCount) + "|0|16|16",
                "SLOTS|24|24|0",
                "EXPORT|6|" + replayManifest + "|" + replayPacket,
                "OVERLAY|1536|16|1536|24|64",
                "WORLD|169|13|13|624|416|2704|259584|0|0|0",
                "MUTATION|0|0|0|0|0|0|0|REGRESSION|0|0|0|0|0",
                "HANDOFF|MAP17_01_RESOLVE_ASSETS_AND_PLACE_GENERATED_CELLS|LOCKED",
            });
            return ExitAuditResult.Passed(GeneratedTerrainExportDigest.Hash(canonical));
        }

        private static SocketStats InspectSockets(
            IEnumerable<GeneratedMicroChunkSliceRecord> sourceSlices,
            GeneratedTerrainExportPacket packet,
            bool forceMismatch)
        {
            var slices = sourceSlices.OrderBy(value => value.ChunkIndex).ToArray();
            var byPosition = slices.ToDictionary(value => CoordinateKey(value.ChunkX, value.ChunkY));
            var checks = 0;
            var mismatches = 0;
            foreach (var slice in slices)
            {
                GeneratedMicroChunkSliceRecord neighbor;
                if (byPosition.TryGetValue(CoordinateKey(slice.ChunkX + 1, slice.ChunkY), out neighbor))
                {
                    checks++;
                    if (!EdgePassability(slice, GeneratedMicroChunkSocketSide.Right).SequenceEqual(
                            EdgePassability(neighbor, GeneratedMicroChunkSocketSide.Left)))
                        mismatches++;
                }
                if (byPosition.TryGetValue(CoordinateKey(slice.ChunkX, slice.ChunkY + 1), out neighbor))
                {
                    checks++;
                    if (!EdgePassability(slice, GeneratedMicroChunkSocketSide.Up).SequenceEqual(
                            EdgePassability(neighbor, GeneratedMicroChunkSocketSide.Down)))
                        mismatches++;
                }
            }

            if (forceMismatch) mismatches++;
            var digestMismatches = 0;
            var sides = Enum.GetValues(typeof(GeneratedMicroChunkSocketSide))
                .Cast<GeneratedMicroChunkSocketSide>().ToArray();
            foreach (var slice in slices)
            {
                foreach (var side in sides)
                {
                    var edge = EdgeCells(slice, side).ToArray();
                    var signature = slice.SideSignature(side);
                    if (GeneratedMicroChunkSliceDigest.ComputeSideSignature(
                            side, edge, slice.Bands(side)) != signature.Digest)
                        digestMismatches++;
                    var row = packet.SocketRows.SingleOrDefault(value =>
                        value.ChunkIndex == slice.ChunkIndex &&
                        string.Equals(value.Side, side.ToString(), StringComparison.OrdinalIgnoreCase));
                    if (row == null || row.SideSignature != signature.Digest)
                        digestMismatches++;
                }
            }

            var externalSource = slices.Sum(value =>
                (value.ChunkX == 0 ? 1 : 0) + (value.ChunkX == 3 ? 1 : 0) +
                (value.ChunkY == 0 ? 1 : 0) + (value.ChunkY == 3 ? 1 : 0));
            var externalExport = packet.SocketRows.Count(value =>
            {
                GeneratedMicroChunkSocketSide side;
                if (!Enum.TryParse(value.Side, true, out side)) return false;
                var x = value.ChunkIndex % 4;
                var y = value.ChunkIndex / 4;
                return (side == GeneratedMicroChunkSocketSide.Left && x == 0) ||
                       (side == GeneratedMicroChunkSocketSide.Right && x == 3) ||
                       (side == GeneratedMicroChunkSocketSide.Down && y == 0) ||
                       (side == GeneratedMicroChunkSocketSide.Up && y == 3);
            });
            return new SocketStats(
                slices.Sum(value => value.SideSignatures.Count),
                slices.Sum(value => value.SocketBands.Count),
                packet.SocketBandCount,
                checks,
                mismatches,
                externalSource,
                externalExport,
                digestMismatches);
        }

        private static IEnumerable<GeneratedMicroChunkCell> EdgeCells(
            GeneratedMicroChunkSliceRecord slice,
            GeneratedMicroChunkSocketSide side)
        {
            switch (side)
            {
                case GeneratedMicroChunkSocketSide.Left:
                    return slice.Cells.Where(value => value.LocalCoordinate.X == 0)
                        .OrderBy(value => value.LocalCoordinate.Y);
                case GeneratedMicroChunkSocketSide.Right:
                    return slice.Cells.Where(value => value.LocalCoordinate.X == 11)
                        .OrderBy(value => value.LocalCoordinate.Y);
                case GeneratedMicroChunkSocketSide.Down:
                    return slice.Cells.Where(value => value.LocalCoordinate.Y == 0)
                        .OrderBy(value => value.LocalCoordinate.X);
                default:
                    return slice.Cells.Where(value => value.LocalCoordinate.Y == 7)
                        .OrderBy(value => value.LocalCoordinate.X);
            }
        }

        private static IEnumerable<bool> EdgePassability(
            GeneratedMicroChunkSliceRecord slice,
            GeneratedMicroChunkSocketSide side) => EdgeCells(slice, side)
                .Select(value => value.IsPassable);

        private static WorldProjection ProjectWorld(
            IEnumerable<GeneratedMicroChunkSliceRecord> sourceSlices,
            bool reverse)
        {
            var slices = reverse
                ? sourceSlices.Reverse().ToArray()
                : sourceSlices.ToArray();
            var sectors = Enumerable.Range(0, WorldGenConstants.SectorCount);
            if (reverse) sectors = sectors.Reverse();
            var coordinates = new HashSet<long>();
            var cellCount = 0;
            var sliceCount = 0;
            var outOfBounds = 0;
            foreach (var sector in sectors)
            {
                var sectorX = sector % WorldGenConstants.SectorColumns;
                var sectorY = sector / WorldGenConstants.SectorColumns;
                sliceCount += slices.Length;
                foreach (var slice in slices)
                {
                    var cells = reverse ? slice.Cells.Reverse() : slice.Cells;
                    foreach (var cell in cells)
                    {
                        var x = (sectorX * WorldGenConstants.SectorWidthTiles) +
                            cell.SectorCoordinate.X;
                        var y = (sectorY * WorldGenConstants.SectorHeightTiles) +
                            cell.SectorCoordinate.Y;
                        cellCount++;
                        if (x < 0 || x >= WorldGenConstants.WorldWidthTiles ||
                            y < 0 || y >= WorldGenConstants.WorldHeightTiles)
                            outOfBounds++;
                        else
                            coordinates.Add(CoordinateKey(x, y));
                    }
                }
            }

            return new WorldProjection(
                WorldGenConstants.SectorCount,
                WorldGenConstants.WorldWidthTiles,
                WorldGenConstants.WorldHeightTiles,
                sliceCount,
                cellCount,
                coordinates.Count,
                cellCount - coordinates.Count - outOfBounds,
                WorldGenConstants.WorldTileCount - coordinates.Count,
                outOfBounds);
        }

        private static IEnumerable<CellProbe> SectorCells(
            IEnumerable<GeneratedMicroChunkSliceRecord> slices) => slices
                .SelectMany(value => value.Cells)
                .Select(value => new CellProbe(value.SectorCoordinate.X, value.SectorCoordinate.Y));

        private static Map16Chain ReferenceChain()
        {
            if (reference != null) return reference;
            var fixture = new ReferenceFinalRouteRecoveryFixture();
            var baselineRouteRequest = fixture.AcceptedRequest();
            var baselinePlan = baselineRouteRequest.CanvasPlan;
            var baselineRequest = baselinePlan.Request;
            var claims = baselinePlan.Cells.SelectMany(value => value.Winners).ToList();

            for (var x = 0; x < 48; x++)
            {
                if (x != 30 && x != 31)
                    AddPassableOverrides(claims, x, 15);
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

            var canvasResult = SectorCanvasLayerFinalizer.Finalize(new FinalCanvasLayerRequest(
                baselineRequest.SectorId, baselineRequest.Width, baselineRequest.Height, claims,
                baselineRequest.Map15ExitApproved, baselineRequest.Map15ExitDigest,
                baselineRequest.WorldAssemblyDigest, baselineRequest.SectorOwnershipDigest,
                baselineRequest.BoundaryAuthorityDigest, baselineRequest.FixedCanvasAuthorityDigest,
                baselineRequest.PublicationLabel));
            Assert.That(canvasResult.Success, Is.True, Describe(canvasResult.Failures));
            var densityResult = SectorCanvasProtectionDensityValidator.Validate(canvasResult.Plan);
            Assert.That(densityResult.Success, Is.True, Describe(densityResult.Failures));
            var routeResult = SectorFinalRouteRecoveryValidator.Validate(
                new FinalRouteRecoveryRequest(canvasResult.Plan, densityResult.Report,
                    baselineRouteRequest.Anchors, baselineRouteRequest.DeclaredEdges,
                    baselineRouteRequest.PublicationLabel));
            Assert.That(routeResult.Success, Is.True, Describe(routeResult.Failures));
            var partitionResult = SectorPatternChunkPartitioner.Partition(
                canvasResult.Plan, densityResult.Report, routeResult.Report);
            Assert.That(partitionResult.Success, Is.True, Describe(partitionResult.Failures));
            var sliceResult = GeneratedMicroChunkSliceBuilder.Build(
                canvasResult.Plan, densityResult.Report, routeResult.Report,
                partitionResult.Partition);
            Assert.That(sliceResult.Success, Is.True, Describe(sliceResult.Failures));
            var slotResult = GeneratedMicroChunkMarkerSlotProjector.Project(sliceResult.SliceSet);
            Assert.That(slotResult.Success, Is.True, Describe(slotResult.Failures));
            Assert.That(slotResult.SlotSet.SlotCount, Is.EqualTo(24));
            var exportResult = GeneratedTerrainCsvExporter.Build(
                sliceResult.SliceSet, slotResult.SlotSet);
            Assert.That(exportResult.Success, Is.True, ExportFailures(exportResult));
            var overlayResult = GeneratedTerrainDebugOverlay.Build(exportResult.Packet);
            Assert.That(overlayResult.Success, Is.True, Describe(overlayResult.Failures));

            reference = new Map16Chain(
                canvasResult.Plan, densityResult.Report, routeResult.Report,
                partitionResult.Partition, sliceResult.SliceSet, slotResult.SlotSet,
                exportResult.Packet, overlayResult);
            return reference;
        }

        private static void AddPassableOverrides(
            ICollection<FinalCanvasLayerClaim> claims,
            int x,
            int y)
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
                FinalCanvasProtectionKind.None, false, provenanceId, FixtureLabel);

        private static long CoordinateKey(int x, int y) => ((long)y << 32) | (uint)x;
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Describe(System.Collections.IEnumerable values)
        {
            var descriptions = new List<string>();
            foreach (var value in values)
                descriptions.Add(value == null ? "NULL" : value.ToString());
            return string.Join(";", descriptions);
        }
        private static string ExportFailures(GeneratedTerrainExportResult result) => result == null
            ? "NULL RESULT" : Describe(result.Failures);
        private static string ReplayFailures(GeneratedTerrainReplayResult result) => result == null
            ? "NULL RESULT" : Describe(result.Failures);

        private sealed class Map16Chain
        {
            public Map16Chain(
                SectorFinalCanvasLayerPlan canvas,
                SectorCanvasProtectionDensityReport density,
                SectorFinalRouteRecoveryReport route,
                SectorPatternChunkPartition partition,
                GeneratedMicroChunkSliceSet slices,
                GeneratedMicroChunkMarkerSlotSet slots,
                GeneratedTerrainExportPacket packet,
                GeneratedTerrainOverlayResult overlay)
            {
                Canvas = canvas;
                Density = density;
                Route = route;
                Partition = partition;
                Slices = slices;
                Slots = slots;
                Packet = packet;
                Overlay = overlay;
            }

            public SectorFinalCanvasLayerPlan Canvas { get; }
            public SectorCanvasProtectionDensityReport Density { get; }
            public SectorFinalRouteRecoveryReport Route { get; }
            public SectorPatternChunkPartition Partition { get; }
            public GeneratedMicroChunkSliceSet Slices { get; }
            public GeneratedMicroChunkMarkerSlotSet Slots { get; }
            public GeneratedTerrainExportPacket Packet { get; }
            public GeneratedTerrainOverlayResult Overlay { get; }
        }

        private sealed class AuditProbe
        {
            public IEnumerable<GeneratedMicroChunkSliceRecord> Slices { get; set; }
            public IEnumerable<CellProbe> SectorCells { get; set; }
            public bool ForceSocketMismatch { get; set; }
            public bool ForceMissingLayerProvenance { get; set; }
            public bool ForceMissingMarkerSource { get; set; }
            public bool ReplaySuccess { get; set; } = true;
            public string ReplayManifestDigest { get; set; }
            public string ReplayPacketDigest { get; set; }
            public bool ReverseWorldProjection { get; set; }
        }

        private sealed class ExitAuditResult
        {
            private ExitAuditResult(bool approved, string digest, IEnumerable<string> failures)
            {
                Approved = approved;
                Digest = digest ?? string.Empty;
                Failures = failures.OrderBy(value => value, StringComparer.Ordinal).ToArray();
            }

            public bool Approved { get; }
            public string Digest { get; }
            public IReadOnlyList<string> Failures { get; }
            public bool OpensMap17 => false;
            public bool RuntimeBakeLocked => true;
            public int PriorTaskSelections => 0;
            public int LegacyRegressionSelections => 0;
            public int PlayModeSelections => 0;
            public int UnfilteredSelections => 0;
            public int FullRegressionRuns => 0;

            public static ExitAuditResult Passed(string digest) =>
                new ExitAuditResult(true, digest, Array.Empty<string>());
            public static ExitAuditResult Failed(IEnumerable<string> failures) =>
                new ExitAuditResult(false, string.Empty, failures);
        }

        private sealed class CellProbe
        {
            public CellProbe(int x, int y)
            {
                X = x;
                Y = y;
            }

            public int X { get; }
            public int Y { get; }
        }

        private sealed class SocketStats
        {
            public SocketStats(
                int observedSideSignatureCount,
                int sourceSocketBandCount,
                int exportedSocketBandCount,
                int internalAdjacencyCheckCount,
                int internalAdjacencyMismatchCount,
                int externalSourceRecordCount,
                int externalExportRecordCount,
                int socketDigestMismatchCount)
            {
                ObservedSideSignatureCount = observedSideSignatureCount;
                SourceSocketBandCount = sourceSocketBandCount;
                ExportedSocketBandCount = exportedSocketBandCount;
                InternalAdjacencyCheckCount = internalAdjacencyCheckCount;
                InternalAdjacencyMismatchCount = internalAdjacencyMismatchCount;
                ExternalSourceRecordCount = externalSourceRecordCount;
                ExternalExportRecordCount = externalExportRecordCount;
                SocketDigestMismatchCount = socketDigestMismatchCount;
            }

            public int ObservedSideSignatureCount { get; }
            public int SourceSocketBandCount { get; }
            public int ExportedSocketBandCount { get; }
            public int InternalAdjacencyCheckCount { get; }
            public int InternalAdjacencyMismatchCount { get; }
            public int ExternalSourceRecordCount { get; }
            public int ExternalExportRecordCount { get; }
            public int SocketDigestMismatchCount { get; }
        }

        private sealed class WorldProjection
        {
            public WorldProjection(
                int sectorCount,
                int width,
                int height,
                int sliceCount,
                int cellCount,
                int uniqueCellCount,
                int duplicateCellCount,
                int missingCellCount,
                int outOfBoundsCellCount)
            {
                SectorCount = sectorCount;
                Width = width;
                Height = height;
                SliceCount = sliceCount;
                CellCount = cellCount;
                UniqueCellCount = uniqueCellCount;
                DuplicateCellCount = duplicateCellCount;
                MissingCellCount = missingCellCount;
                OutOfBoundsCellCount = outOfBoundsCellCount;
            }

            public int SectorCount { get; }
            public int Width { get; }
            public int Height { get; }
            public int SliceCount { get; }
            public int CellCount { get; }
            public int UniqueCellCount { get; }
            public int DuplicateCellCount { get; }
            public int MissingCellCount { get; }
            public int OutOfBoundsCellCount { get; }
        }
    }
}
