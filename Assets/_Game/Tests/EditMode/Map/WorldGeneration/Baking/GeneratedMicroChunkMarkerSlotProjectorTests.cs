using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Baking
{
    [TestFixture]
    [Category("MAP16_06")]
    public sealed class GeneratedMicroChunkMarkerSlotProjectorTests
    {
        [Test]
        public void MarkerSlotSetPublishesClusterActivitySpecialEventSlotsAndDigests()
        {
            var source = AcceptedSliceSet();
            var result = GeneratedMicroChunkMarkerSlotProjector.Project(source);

            Assert.That(result.Success, Is.True, Failures(result));
            var set = result.SlotSet;
            Assert.That(set.SourceSliceCount, Is.EqualTo(16));
            Assert.That(set.SourceCellCount, Is.EqualTo(1536));
            Assert.That(set.SourceLayerRecordCount, Is.EqualTo(10752));
            Assert.That(set.MarkerLayerRecordsScanned, Is.EqualTo(10752));
            Assert.That(set.MarkerLayerRecordsConsumed, Is.EqualTo(set.SlotCount));
            Assert.That(set.RequiredMarkerOwnerFamilyCount, Is.EqualTo(4));
            Assert.That(set.CoveredRequiredMarkerOwnerFamilyCount, Is.EqualTo(4));
            Assert.That(set.MissingRequiredMarkerOwnerFamilyCount, Is.Zero);
            Assert.That(set.Slots.Select(value => value.Kind), Does.Contain(
                GeneratedMarkerSlotKind.TerrainCluster));
            Assert.That(set.Slots.Select(value => value.Kind), Does.Contain(
                GeneratedMarkerSlotKind.Activity));
            Assert.That(set.Slots.Select(value => value.Kind), Does.Contain(
                GeneratedMarkerSlotKind.SpecialRegion));
            Assert.That(set.Slots.Select(value => value.Kind), Does.Contain(
                GeneratedMarkerSlotKind.EventOverlay));
            Assert.That(set.OptionalMarkerOwnerFamilies, Does.Contain(
                GeneratedMarkerSlotKind.Boundary));
            Assert.That(set.OptionalMarkerOwnerFamilies, Does.Contain(
                GeneratedMarkerSlotKind.RouteRecovery));
            Assert.That(set.CompatibleMultiMarkerCellCount, Is.GreaterThan(0));
            Assert.That(MarkerSlotProjectionDigest.IsLowerHexSha256(result.InputDigest), Is.True);
            Assert.That(MarkerSlotProjectionDigest.IsLowerHexSha256(result.OutputDigest), Is.True);

            TestContext.WriteLine("MAP16_06_EVIDENCE" +
                " source_slices=" + set.SourceSliceCount + "/16" +
                " source_cells=" + set.SourceCellCount + "/1536" +
                " source_layers=" + set.SourceLayerRecordCount + "/10752" +
                " marker_scanned=" + set.MarkerLayerRecordsScanned +
                " marker_consumed=" + set.MarkerLayerRecordsConsumed +
                " required=" + set.RequiredMarkerOwnerFamilyCount + "/" +
                    set.CoveredRequiredMarkerOwnerFamilyCount + "/" +
                    set.MissingRequiredMarkerOwnerFamilyCount +
                " optional=" + set.OptionalMarkerOwnerFamilies.Count +
                " slots=" + set.SlotCount +
                " stable_ids=" + set.SlotsWithStableLocalIdCount +
                " cell_refs=" + set.SlotsWithCellReferenceCount +
                " provenance=" + set.SlotsWithProvenanceCount +
                " layer_identity=" + set.SlotsPreservingSourceLayerIdentityCount +
                " socket_identity=" + set.SlotsWithSocketBandIdentityCount +
                " signature_traversal_identity=" +
                    set.SlotsPreservingSocketSignatureTraversalIdentityCount +
                " compatible_multi_marker_cells=" + set.CompatibleMultiMarkerCellCount +
                " duplicates=" + set.DuplicateSlotIdCount + "/" +
                    set.DuplicateOwnerKindSourceKeyCount +
                " orphan=" + set.OrphanMarkerRecordCount +
                " missing_provenance=" + set.MissingProvenanceCount +
                " input=" + set.InputDigest + " output=" + set.OutputDigest);
        }

        [Test]
        public void MarkerLayerRecordsProjectToStableSliceLocalSlotIds()
        {
            var result = GeneratedMicroChunkMarkerSlotProjector.Project(AcceptedSliceSet());
            Assert.That(result.Success, Is.True, Failures(result));
            var slots = result.SlotSet.Slots;

            Assert.That(slots, Has.Count.GreaterThan(0));
            Assert.That(slots.Select(value => value.Id.Value).Distinct().Count(),
                Is.EqualTo(slots.Count));
            Assert.That(slots.Select(value => value.StableToken), Is.EqualTo(
                slots.OrderBy(value => value).Select(value => value.StableToken)));
            Assert.That(slots.All(value => value.Id.ChunkIndex ==
                value.CellReference.ChunkIndex), Is.True);
            Assert.That(slots.All(value => value.Id.LocalX == value.CellReference.LocalX &&
                value.Id.LocalY == value.CellReference.LocalY), Is.True);
            Assert.That(slots.All(value => value.Id.Value.IndexOf(
                "SPAWN", StringComparison.OrdinalIgnoreCase) < 0), Is.True);

            var compatible = slots.GroupBy(value => value.CellReference.StableToken)
                .First(group => group.Select(value => value.Kind).Distinct().Count() > 1)
                .OrderBy(value => value).ToArray();
            Assert.That(compatible.Select(value => value.Kind).Distinct().Count(),
                Is.GreaterThan(1));
            Assert.That(compatible.Select(value => value.ProjectionOrdinal).Distinct().Count(),
                Is.EqualTo(compatible.Length));
        }

        [Test]
        public void SlotCellReferencesRoundTripToSectorChunkAndLocalCoordinates()
        {
            var result = GeneratedMicroChunkMarkerSlotProjector.Project(AcceptedSliceSet());
            Assert.That(result.Success, Is.True, Failures(result));

            foreach (var slot in result.SlotSet.Slots)
            {
                var slice = result.SourceSliceSet.Slices.Single(value =>
                    value.Id.Value == slot.CellReference.SourceSliceId);
                var cell = slice.Cells.Single(value =>
                    value.LocalCoordinate.X == slot.CellReference.LocalX &&
                    value.LocalCoordinate.Y == slot.CellReference.LocalY);
                Assert.That(slot.CellReference.ChunkIndex, Is.EqualTo(slice.ChunkIndex));
                Assert.That(slot.CellReference.SectorX, Is.EqualTo(
                    slice.SectorOrigin.X + slot.CellReference.LocalX));
                Assert.That(slot.CellReference.SectorY, Is.EqualTo(
                    slice.SectorOrigin.Y + slot.CellReference.LocalY));
                Assert.That(slot.CellReference.SectorX, Is.EqualTo(cell.SectorCoordinate.X));
                Assert.That(slot.CellReference.SectorY, Is.EqualTo(cell.SectorCoordinate.Y));
                Assert.That(slot.CellReference.IsComplete, Is.True);
            }
        }

        [Test]
        public void SlotProvenanceTracksSourceOwnerTaskLayerClaimAndSliceCell()
        {
            var result = GeneratedMicroChunkMarkerSlotProjector.Project(AcceptedSliceSet());
            Assert.That(result.Success, Is.True, Failures(result));

            foreach (var slot in result.SlotSet.Slots)
            {
                var slice = result.SourceSliceSet.Slices.Single(value =>
                    value.Id.Value == slot.CellReference.SourceSliceId);
                var cell = slice.Cells.Single(value =>
                    value.SectorCoordinate.X == slot.CellReference.SectorX &&
                    value.SectorCoordinate.Y == slot.CellReference.SectorY);
                var layer = cell.Layers.Single(value =>
                    value.Layer == slot.Provenance.SourceLayerKind);
                GeneratedMarkerSlotKind kind;
                GeneratedMarkerSlotOwner owner;
                string sourceTaskId;
                Assert.That(GeneratedMicroChunkMarkerSlotProjector.TryMapOwner(
                    layer.SourceOwner, out kind, out owner, out sourceTaskId), Is.True);
                Assert.That(slot.Kind, Is.EqualTo(kind));
                Assert.That(slot.Owner, Is.EqualTo(owner));
                Assert.That(slot.Provenance.SourceOwnerToken,
                    Is.EqualTo(layer.SourceOwner.ToString()));
                Assert.That(slot.Provenance.SourceTaskId, Is.EqualTo(sourceTaskId));
                Assert.That(slot.Provenance.SourceLayerToken, Is.EqualTo(layer.StableToken));
                Assert.That(slot.Provenance.SourceClaimOrEvidenceId, Is.EqualTo(layer.ClaimId));
                Assert.That(slot.Provenance.SourceProvenanceId, Is.EqualTo(layer.ProvenanceId));
                Assert.That(slot.Provenance.SourceCellToken, Is.EqualTo(layer.SourceCellToken));
                Assert.That(slot.Provenance.SourceSliceId, Is.EqualTo(slice.Id.Value));
                Assert.That(slot.Provenance.IsComplete, Is.True);
            }
        }

        [Test]
        public void DuplicateAndOrphanMarkersFailAtomicallyWithoutPartialSlotSet()
        {
            var source = AcceptedSliceSet();
            var projections = GeneratedMicroChunkMarkerSlotProjector.Scan(source).ToArray();
            var first = projections[0];
            var otherSlice = projections.First(value =>
                value.SourceSlice.ChunkIndex != first.SourceSlice.ChunkIndex);
            var missingProvenanceLayer = new GeneratedMicroChunkLayerRecord(
                first.SourceLayer.Layer, first.SourceLayer.CellKind,
                first.SourceLayer.SourceOwner, string.Empty, first.SourceLayer.Protection,
                first.SourceLayer.IsProtected, first.SourceLayer.ClaimId,
                first.SourceLayer.SourceCellToken);

            AssertAtomicFailure(GeneratedMicroChunkMarkerSlotProjector.Project(null),
                MarkerSlotProjectionFailureCode.MissingSourceSliceSet);
            AssertAtomicFailure(GeneratedMicroChunkMarkerSlotProjector.Project(source,
                    projections.Concat(new[] { first })),
                MarkerSlotProjectionFailureCode.DuplicateOwnerKindSourceKey,
                MarkerSlotProjectionFailureCode.DuplicateSlotId);
            AssertAtomicFailure(GeneratedMicroChunkMarkerSlotProjector.Project(source,
                    projections.Concat(new[] { new GeneratedMarkerSlotProjection(
                        first.SourceSlice, null, first.SourceLayer, first.Kind, first.Owner,
                        first.SourceTaskId) })),
                MarkerSlotProjectionFailureCode.MissingCellReference);
            AssertAtomicFailure(GeneratedMicroChunkMarkerSlotProjector.Project(source,
                    projections.Concat(new[] { new GeneratedMarkerSlotProjection(
                        first.SourceSlice, otherSlice.SourceCell, otherSlice.SourceLayer,
                        otherSlice.Kind, otherSlice.Owner, otherSlice.SourceTaskId) })),
                MarkerSlotProjectionFailureCode.OrphanCell);
            AssertAtomicFailure(GeneratedMicroChunkMarkerSlotProjector.Project(source,
                    projections.Concat(new[] { new GeneratedMarkerSlotProjection(
                        first.SourceSlice, first.SourceCell, missingProvenanceLayer,
                        first.Kind, first.Owner, first.SourceTaskId) })),
                MarkerSlotProjectionFailureCode.MissingProvenance,
                MarkerSlotProjectionFailureCode.SourceLayerMismatch);

            TestContext.WriteLine("MAP16_06_FAILURE_EVIDENCE" +
                " duplicate_owner_kind_source_key=1 duplicate_slot_id=1" +
                " orphan_cell=1 missing_cell_ref=1 missing_provenance=1 partial_sets=0");
        }

        [Test]
        public void MarkerProjectionPreservesSliceCellLayerSocketAndTraversalIdentities()
        {
            var source = AcceptedSliceSet();
            var result = GeneratedMicroChunkMarkerSlotProjector.Project(source);
            Assert.That(result.Success, Is.True, Failures(result));
            var set = result.SlotSet;

            Assert.That(set.SourceSliceSet, Is.SameAs(source));
            Assert.That(set.SourceSliceSetDigest, Is.EqualTo(source.OutputDigest));
            Assert.That(set.SourceSliceIds, Is.EqualTo(source.Slices.OrderBy(value => value)
                .Select(value => value.Id.Value)));
            Assert.That(set.SlotsWithStableLocalIdCount, Is.EqualTo(set.SlotCount));
            Assert.That(set.SlotsWithCellReferenceCount, Is.EqualTo(set.SlotCount));
            Assert.That(set.SlotsWithProvenanceCount, Is.EqualTo(set.SlotCount));
            Assert.That(set.SlotsPreservingSourceLayerIdentityCount, Is.EqualTo(set.SlotCount));
            Assert.That(set.SlotsPreservingSocketSignatureTraversalIdentityCount,
                Is.EqualTo(set.SlotCount));
            var expectedSocketIdentityCount = set.Slots.Count(slot =>
            {
                var slice = source.Slices.Single(value => value.Id.Value ==
                    slot.CellReference.SourceSliceId);
                return slice.SocketBands.Any(band => band.Cells.Any(cell =>
                    cell.SectorCoordinate.X == slot.CellReference.SectorX &&
                    cell.SectorCoordinate.Y == slot.CellReference.SectorY));
            });
            Assert.That(set.SlotsWithSocketBandIdentityCount,
                Is.EqualTo(expectedSocketIdentityCount));
            Assert.That(set.Slots.All(value =>
                !string.IsNullOrEmpty(value.Provenance.SourceSignatureIdentity) &&
                !string.IsNullOrEmpty(value.Provenance.SourceTraversalIdentity)), Is.True);
        }

        [Test]
        public void MarkerSlotDigestIsDeterministicAcrossRepeatReverseAndCulture()
        {
            var source = AcceptedSliceSet();
            var projections = GeneratedMicroChunkMarkerSlotProjector.Scan(source).ToArray();
            var first = GeneratedMicroChunkMarkerSlotProjector.Project(source, projections);
            var repeat = GeneratedMicroChunkMarkerSlotProjector.Project(source, projections);
            var reverse = GeneratedMicroChunkMarkerSlotProjector.Project(
                source, projections.Reverse().ToArray());
            Assert.That(first.Success, Is.True, Failures(first));
            Assert.That(repeat.Success, Is.True, Failures(repeat));
            Assert.That(reverse.Success, Is.True, Failures(reverse));
            Assert.That(repeat.InputDigest, Is.EqualTo(first.InputDigest));
            Assert.That(repeat.OutputDigest, Is.EqualTo(first.OutputDigest));
            Assert.That(reverse.InputDigest, Is.EqualTo(first.InputDigest));
            Assert.That(reverse.OutputDigest, Is.EqualTo(first.OutputDigest));

            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
                CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
                var culture = GeneratedMicroChunkMarkerSlotProjector.Project(
                    source, projections.Reverse().ToArray());
                Assert.That(culture.Success, Is.True, Failures(culture));
                Assert.That(culture.InputDigest, Is.EqualTo(first.InputDigest));
                Assert.That(culture.OutputDigest, Is.EqualTo(first.OutputDigest));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Test]
        public void ProjectorDoesNotCreateStableSpawnIdsRuntimeObjectsFilesTilemapsOrScenes()
        {
            var result = GeneratedMicroChunkMarkerSlotProjector.Project(AcceptedSliceSet());
            Assert.That(result.Success, Is.True, Failures(result));
            var set = result.SlotSet;

            Assert.That(set.StableSpawnIdCount, Is.Zero);
            Assert.That(set.RuntimeObjectSpawnCount, Is.Zero);
            Assert.That(set.CsvGeneratedFileCount, Is.Zero);
            Assert.That(set.JsonGeneratedFileCount, Is.Zero);
            Assert.That(set.TilemapBakeCount, Is.Zero);
            Assert.That(set.TilemapMutationCount, Is.Zero);
            Assert.That(set.SceneMutationCount, Is.Zero);
            Assert.That(set.PrefabMutationCount, Is.Zero);
            Assert.That(set.GameObjectMutationCount, Is.Zero);
            Assert.That(set.ProductionSeedApprovalCount, Is.Zero);
        }

        [Test]
        public void ProjectorDoesNotMutateSlicesCanvasPartitionOrAuthoringAssets()
        {
            var source = AcceptedSliceSet();
            var sourceInput = source.InputDigest;
            var sourceOutput = source.OutputDigest;
            var canvasOutput = source.SourceCanvasPlan.OutputDigest;
            var partitionOutput = source.SourcePartition.OutputDigest;
            var sliceTokens = source.Slices.Select(SliceToken).ToArray();

            var result = GeneratedMicroChunkMarkerSlotProjector.Project(source);
            Assert.That(result.Success, Is.True, Failures(result));
            Assert.That(source.InputDigest, Is.EqualTo(sourceInput));
            Assert.That(source.OutputDigest, Is.EqualTo(sourceOutput));
            Assert.That(source.SourceCanvasPlan.OutputDigest, Is.EqualTo(canvasOutput));
            Assert.That(source.SourcePartition.OutputDigest, Is.EqualTo(partitionOutput));
            Assert.That(source.Slices.Select(SliceToken), Is.EqualTo(sliceTokens));
            Assert.That(result.SlotSet.SourceSliceMutationCount, Is.Zero);
            Assert.That(result.SlotSet.CsvGeneratedFileCount, Is.Zero);
            Assert.That(result.SlotSet.JsonGeneratedFileCount, Is.Zero);
        }

        [Test]
        public void Map16HandoffKeepsMap16_07Locked()
        {
            var result = GeneratedMicroChunkMarkerSlotProjector.Project(AcceptedSliceSet());
            Assert.That(result.Success, Is.True, Failures(result));
            Assert.That(GeneratedMicroChunkMarkerSlotSet.DownstreamOwner,
                Is.EqualTo("MAP16_07_EXPORT_REPLAY_AND_DEBUG_GENERATED_TERRAIN"));
            Assert.That(GeneratedMicroChunkMarkerSlotSet.OpensDownstreamTask, Is.False);
            Assert.That(result.SlotSet.StableSpawnIdCount, Is.Zero);
            Assert.That(result.SlotSet.RuntimeObjectSpawnCount, Is.Zero);
        }

        private static GeneratedMicroChunkSliceSet AcceptedSliceSet()
        {
            var fixture = new ReferenceFinalRouteRecoveryFixture();
            var baselineRouteRequest = fixture.AcceptedRequest();
            var baselinePlan = baselineRouteRequest.CanvasPlan;
            var baselineRequest = baselinePlan.Request;
            var claims = baselinePlan.Cells.SelectMany(value => value.Winners).ToList();
            claims.Add(Claim("MAP16_06_TERRAIN_CLUSTER", 12, 20,
                FinalCanvasLayerKind.Terrain, FinalCanvasCellKind.Air,
                FinalCanvasSourceOwner.TerrainCluster,
                FinalCanvasClaimPriority.TerrainClusterPattern, "MAP11_TERRAIN_CLUSTER"));
            claims.Add(Claim("MAP16_06_ACTIVITY", 12, 20,
                FinalCanvasLayerKind.Marker, FinalCanvasCellKind.Marker,
                FinalCanvasSourceOwner.Activity,
                FinalCanvasClaimPriority.ActivityMarker, "MAP12_ACTIVITY"));
            claims.Add(Claim("MAP16_06_EVENT_OVERLAY", 13, 20,
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
            Assert.That(sliceResult.Success, Is.True, string.Join(";",
                sliceResult.Failures.Select(value => value.ToString())));
            return sliceResult.SliceSet;
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

        private static void AssertAtomicFailure(
            MarkerSlotProjectionResult result,
            params MarkerSlotProjectionFailureCode[] expectedCodes)
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.SlotSet, Is.Null);
            Assert.That(result.OutputDigest, Is.Empty);
            foreach (var code in expectedCodes)
                Assert.That(result.Failures.Select(value => value.Code), Does.Contain(code));
        }

        private static string SliceToken(GeneratedMicroChunkSliceRecord value) => string.Join("|",
            new[]
            {
                value.Id.Value, value.Signature.Digest, value.TraversalSummary.StableToken,
             }.Concat(value.Cells.Select(cell => cell.StableToken))
             .Concat(value.SocketBands.Select(band => band.StableToken))
             .Concat(value.SideSignatures.Select(signature =>
                 (signature.Side.HasValue ? signature.Side.Value.ToString() : "SLICE") +
                 "|" + signature.Digest)));

        private static string Join<T>(IEnumerable<T> values) => string.Join(";",
            values.Select(value => value == null ? "NULL" : value.ToString()));

        private static string Failures(MarkerSlotProjectionResult result) => result == null
            ? "NULL RESULT" : string.Join(";", result.Failures.Select(value => value.ToString()));
    }
}
