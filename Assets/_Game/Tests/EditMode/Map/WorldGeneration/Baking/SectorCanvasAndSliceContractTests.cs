using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Baking;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.SpecialRegions;
using UnityEngine;

namespace StarNight.Map.Tests.EditMode.WorldGeneration.Baking
{
    [TestFixture]
    [Category("MAP09_06")]
    public sealed class SectorCanvasAndSliceContractTests
    {
        [Test]
        public void SourceAndLayerEnumsAreExact()
        {
            Assert.That(Enum.GetNames(typeof(CanvasSourceKind)), Is.EqualTo(new[]
            {
                "Boundary", "SpecialRegion", "TerrainCluster", "MicroPattern", "Activity", "EventOverlay", "Cleanup",
            }));
            Assert.That(Enum.GetNames(typeof(SectorCanvasLayerKind)), Is.EqualTo(new[]
            {
                "Solid", "Background", "Surface", "Affordance", "Material", "Hazard", "Marker", "Owner",
            }));
            Assert.That(Enum.GetNames(typeof(SectorCanvasValidationState)),
                Is.EqualTo(new[] { "Unvalidated", "Validated" }));
        }

        [Test]
        public void ValidCanvasPublishesAll1536CanonicalCellsAndDigest()
        {
            var canvas = CreateCanvas();
            var result = SectorCanvasContractValidator.Validate(canvas);
            Assert.That(result.IsValid, Is.True, Join(result));
            Assert.That(canvas.Width, Is.EqualTo(48));
            Assert.That(canvas.Height, Is.EqualTo(32));
            Assert.That(canvas.Cells, Has.Count.EqualTo(1536));
            Assert.That(canvas.Cells.Select(value => value.CanonicalIndex), Is.EqualTo(Enumerable.Range(0, 1536)));
            Assert.That(result.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
        }

        [Test]
        public void CanvasRejectsWrongDimensionsMissingAndDuplicateCells()
        {
            var canvas = CreateCanvas();
            var cells = canvas.Cells.Take(canvas.Cells.Count - 1).Concat(new[] { canvas.Cells[0] });
            var invalid = new SectorCanvasContract(canvas.Id, 47, 33, cells, canvas.ValidationStamp);
            var result = SectorCanvasContractValidator.Validate(invalid);
            AssertError(result, SectorCanvasValidationErrorCode.InvalidCanvasDimensions);
            AssertError(result, SectorCanvasValidationErrorCode.MissingOrDuplicateCanvasCell);
        }

        [Test]
        public void EveryLayerPayloadMustBeStableIdOrExplicitEmpty()
        {
            var canvas = CreateCanvas();
            var cells = canvas.Cells.ToArray();
            cells[0] = new SectorCanvasCell(cells[0].Coordinate,
                new SectorCanvasLayerSnapshot(
                    new ResolvedLayerValue("AIR", false), ResolvedLayerValue.Empty, ResolvedLayerValue.Empty,
                    ResolvedLayerValue.Empty, ResolvedLayerValue.Empty, ResolvedLayerValue.Empty,
                    ResolvedLayerValue.Empty, ResolvedLayerValue.FromId("TC_CANVAS_OWNER")),
                new SectorCanvasProvenance(new[] { OwnerSource() }));
            var invalid = Unvalidated(canvas.Id, cells);
            AssertError(SectorCanvasContractValidator.Validate(invalid),
                SectorCanvasValidationErrorCode.InvalidLayerSnapshot);
        }

        [Test]
        public void DuplicateOwnerAndInvalidSourceAreRejected()
        {
            var canvas = CreateCanvas();
            var cells = canvas.Cells.ToArray();
            var duplicateOwner = new CanvasSourceRef(CanvasSourceKind.Cleanup, "CLEANUP_OWNER", 99, false,
                new[] { SectorCanvasLayerKind.Owner });
            cells[1] = new SectorCanvasCell(cells[1].Coordinate, cells[1].Layers,
                new SectorCanvasProvenance(new[] { OwnerSource(), duplicateOwner }));
            var invalid = Unvalidated(canvas.Id, cells);
            AssertError(SectorCanvasContractValidator.Validate(invalid),
                SectorCanvasValidationErrorCode.InvalidLayerSnapshot);
        }

        [Test]
        public void ProtectedOwnerReplacementIsRejected()
        {
            var canvas = CreateCanvas();
            var cells = canvas.Cells.ToArray();
            var layers = cells[2].Layers;
            cells[2] = new SectorCanvasCell(cells[2].Coordinate,
                new SectorCanvasLayerSnapshot(layers.Solid, layers.Background, layers.Surface, layers.Affordance,
                    layers.Material, layers.Hazard, layers.Marker, ResolvedLayerValue.FromId("OTHER_OWNER")),
                cells[2].Provenance);
            AssertError(SectorCanvasContractValidator.Validate(Unvalidated(canvas.Id, cells)),
                SectorCanvasValidationErrorCode.ProtectedSourceLost);
        }

        [Test]
        public void ValidatedStampBindsCatalogSourcesCellsAndRuleset()
        {
            var canvas = CreateCanvas();
            Assert.That(canvas.ValidationStamp.PassCatalogDigest, Is.EqualTo(V2PassCatalog.StableDigest));
            Assert.That(canvas.ValidationStamp.LayerCatalogDigest, Is.EqualTo(GenerationLayerCatalog.StableDigest));
            Assert.That(canvas.ValidationStamp.SourceArtifactSetDigest,
                Is.EqualTo(BakingCanonicalDigest.ComputeSourceArtifactSet(canvas.Cells)));
            Assert.That(canvas.ValidationStamp.ResolvedCellsDigest,
                Is.EqualTo(BakingCanonicalDigest.ComputeResolvedCells(canvas.Cells)));
            Assert.That(canvas.ValidationStamp.StableDigest, Does.Match("^[0-9a-f]{64}$"));
        }

        [Test]
        public void MismatchedValidatedStampIsRejectedButEmptyUnvalidatedStampIsValid()
        {
            var canvas = CreateCanvas();
            var badStamp = new SectorCanvasValidationStamp(SectorCanvasValidationState.Validated,
                V2PassCatalog.StableDigest, GenerationLayerCatalog.StableDigest,
                canvas.ValidationStamp.SourceArtifactSetDigest, new string('f', 64), new string('e', 64));
            AssertError(SectorCanvasContractValidator.Validate(
                    new SectorCanvasContract(canvas.Id, canvas.Width, canvas.Height, canvas.Cells, badStamp)),
                SectorCanvasValidationErrorCode.InvalidValidationStamp);
            Assert.That(SectorCanvasContractValidator.Validate(Unvalidated(canvas.Id, canvas.Cells)).IsValid, Is.True);
        }

        [Test]
        public void ValidSliceSetProjectsExact4By4Of12By8WithoutMutation()
        {
            var canvas = CreateCanvas();
            var slices = CreateSlices(canvas);
            var result = GeneratedSliceContractValidator.Validate(slices, canvas);
            Assert.That(result.IsValid, Is.True, Join(result));
            Assert.That(slices.Slices, Has.Count.EqualTo(16));
            Assert.That(slices.Slices.Select(value => value.Coordinate.CanonicalIndex),
                Is.EqualTo(Enumerable.Range(0, 16)));
            Assert.That(slices.Slices.All(value => value.Cells.Count == 96), Is.True);
            Assert.That(result.CanonicalDigest, Does.Match("^[0-9a-f]{64}$"));
        }

        [Test]
        public void UnvalidatedCanvasCannotFeedSlices()
        {
            var validated = CreateCanvas();
            var unvalidated = Unvalidated(validated.Id, validated.Cells);
            var slices = CreateSlices(unvalidated);
            AssertError(GeneratedSliceContractValidator.Validate(slices, unvalidated),
                GeneratedSliceValidationErrorCode.UnvalidatedSliceSource);
        }

        [Test]
        public void MissingSliceAndCellProduceExactCoverageErrors()
        {
            var canvas = CreateCanvas();
            var source = CreateSlices(canvas);
            var first = new GeneratedMicroChunkSlice(source.Slices[0].Coordinate,
                source.Slices[0].Cells.Take(95), source.Slices[0].Provenance);
            var slices = new[] { first }.Concat(source.Slices.Skip(1).Take(14));
            var invalid = new GeneratedSliceSet(canvas.Id, slices, GeneratedSliceBoundaryRole.GeneratedOutput);
            var result = GeneratedSliceContractValidator.Validate(invalid, canvas);
            AssertError(result, GeneratedSliceValidationErrorCode.InvalidSliceCount);
            AssertError(result, GeneratedSliceValidationErrorCode.InvalidSliceCellCount);
            AssertError(result, GeneratedSliceValidationErrorCode.SliceGapOrOverlap);
        }

        [Test]
        public void SliceCellMutationAndProvenanceLossAreRejected()
        {
            var canvas = CreateCanvas();
            var source = CreateSlices(canvas);
            var first = source.Slices[0];
            var cells = first.Cells.ToArray();
            var original = cells[0];
            var changedLayers = new SectorCanvasLayerSnapshot(original.Layers.Solid, original.Layers.Background,
                original.Layers.Surface, original.Layers.Affordance, original.Layers.Material,
                original.Layers.Hazard, ResolvedLayerValue.FromId("MARKER_CHANGED"), original.Layers.Owner);
            cells[0] = new GeneratedSliceCell(original.LocalCoordinate, changedLayers,
                new SectorCanvasProvenance(new[] { OwnerSource() }));
            var changed = new GeneratedMicroChunkSlice(first.Coordinate, cells, first.Provenance);
            var set = new GeneratedSliceSet(canvas.Id, new[] { changed }.Concat(source.Slices.Skip(1)),
                GeneratedSliceBoundaryRole.GeneratedOutput);
            var result = GeneratedSliceContractValidator.Validate(set, canvas);
            AssertError(result, GeneratedSliceValidationErrorCode.SliceMappingMismatch);
            AssertError(result, GeneratedSliceValidationErrorCode.ProvenanceMismatch);
        }

        [TestCase(GeneratedSliceTransform.Rotate90)]
        [TestCase(GeneratedSliceTransform.MirrorX)]
        [TestCase(GeneratedSliceTransform.MirrorY)]
        [TestCase(GeneratedSliceTransform.Resample)]
        [TestCase(GeneratedSliceTransform.Pad)]
        public void SliceTimeTransformsAreForbidden(GeneratedSliceTransform transform)
        {
            var canvas = CreateCanvas();
            var source = CreateSlices(canvas);
            var first = source.Slices[0];
            var provenance = new GeneratedSliceProvenance(canvas.Id,
                SectorCanvasContractValidator.Validate(canvas).CanonicalDigest,
                canvas.ValidationStamp.StableDigest, transform);
            var changed = new GeneratedMicroChunkSlice(first.Coordinate, first.Cells, provenance);
            var set = new GeneratedSliceSet(canvas.Id, new[] { changed }.Concat(source.Slices.Skip(1)),
                GeneratedSliceBoundaryRole.GeneratedOutput);
            AssertError(GeneratedSliceContractValidator.Validate(set, canvas),
                GeneratedSliceValidationErrorCode.ForbiddenSliceTransform);
        }

        [Test]
        public void GeneratedSliceCannotBecomeAuthoringSource()
        {
            var canvas = CreateCanvas();
            var source = CreateSlices(canvas);
            var invalid = new GeneratedSliceSet(canvas.Id, source.Slices, GeneratedSliceBoundaryRole.AuthoringSource);
            AssertError(GeneratedSliceContractValidator.Validate(invalid, canvas),
                GeneratedSliceValidationErrorCode.AuthoringGeneratedBoundaryViolation);
        }

        [Test]
        public void BoundaryAndSpecialPersistenceProvenanceSurviveProjection()
        {
            var canvas = CreateCanvas();
            var slices = CreateSlices(canvas);
            var projected = slices.Slices[0].Cells[0];
            Assert.That(projected.Provenance.Sources.Select(value => value.Kind),
                Does.Contain(CanvasSourceKind.Boundary));
            Assert.That(projected.Provenance.PersistenceKeys.Select(value => value.Value),
                Does.Contain("SR_STATE_VILLAGE_REWARD_TREASURE"));
            Assert.That(GeneratedSliceContractValidator.Validate(slices, canvas).IsValid, Is.True);
        }

        [Test]
        public void CollectionsAreDefensiveAndDigestsIgnoreInputOrder()
        {
            var canvas = CreateCanvas();
            var reversedCanvas = CreateCanvas(canvas.Cells.Reverse());
            Assert.That(SectorCanvasContractValidator.Validate(reversedCanvas).CanonicalDigest,
                Is.EqualTo(SectorCanvasContractValidator.Validate(canvas).CanonicalDigest));
            var list = CreateSlices(canvas).Slices.Reverse().ToList();
            var set = new GeneratedSliceSet(canvas.Id, list, GeneratedSliceBoundaryRole.GeneratedOutput);
            list.Clear();
            Assert.That(set.Slices, Has.Count.EqualTo(16));
            Assert.That(set.Slices, Is.Not.InstanceOf<List<GeneratedMicroChunkSlice>>());
            Assert.That(GeneratedSliceContractValidator.Validate(set, canvas).IsValid, Is.True);
        }

        [Test]
        public void NegativeSliceErrorsAreSortedDeduplicatedAndPublishNothing()
        {
            var canvas = CreateCanvas();
            var result = GeneratedSliceContractValidator.Validate(
                new GeneratedSliceSet(canvas.Id, Array.Empty<GeneratedMicroChunkSlice>(),
                    GeneratedSliceBoundaryRole.AuthoringSource), canvas);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.SliceSet, Is.Null);
            Assert.That(result.CanonicalDigest, Is.Empty);
            Assert.That(result.Errors, Is.Ordered);
            Assert.That(result.Errors.Distinct().Count(), Is.EqualTo(result.Errors.Count));
        }

        [Test]
        public void ProductionScopeContainsNoComposerSlicerFileRngOrUnityLifecycle()
        {
            var root = Path.GetFullPath(Path.Combine(Application.dataPath,
                "_Game/Map/Runtime/WorldGeneration/Baking"));
            var source = string.Join("\n", Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .OrderBy(value => value, StringComparer.Ordinal).Select(File.ReadAllText));
            foreach (var forbidden in new[]
                     {
                         "UnityEditor", "UnityEngine", "System.IO", "System.Random", "Random.", "MonoBehaviour",
                         "SectorCanvasComposer", "TileValidator", "GeneratedSliceWriter", "StreamingService", "SaveData",
                         "StageMapGenerator", "GridWorld", "RoomTemplate", "RoomGridTransform", "TileMutationService",
                         "SectorRecipeResolver",
                     })
                Assert.That(source, Does.Not.Contain(forbidden), forbidden);
        }

        private static SectorCanvasContract CreateCanvas(IEnumerable<SectorCanvasCell> input = null)
        {
            var cells = (input ?? CreateCells()).ToArray();
            var stamp = new SectorCanvasValidationStamp(SectorCanvasValidationState.Validated,
                V2PassCatalog.StableDigest,
                GenerationLayerCatalog.StableDigest,
                BakingCanonicalDigest.ComputeSourceArtifactSet(cells),
                BakingCanonicalDigest.ComputeResolvedCells(cells),
                new string('e', 64));
            return new SectorCanvasContract(new SectorCanvasId("CANVAS_LIVE_BASELINE"),
                WorldGenConstants.SectorWidthTiles, WorldGenConstants.SectorHeightTiles, cells, stamp);
        }

        private static SectorCanvasContract Unvalidated(SectorCanvasId id, IEnumerable<SectorCanvasCell> cells)
            => new SectorCanvasContract(id, WorldGenConstants.SectorWidthTiles, WorldGenConstants.SectorHeightTiles,
                cells, new SectorCanvasValidationStamp(SectorCanvasValidationState.Unvalidated, "", "", "", "", ""));

        private static IEnumerable<SectorCanvasCell> CreateCells()
        {
            for (var y = 0; y < WorldGenConstants.SectorHeightTiles; y++)
            for (var x = 0; x < WorldGenConstants.SectorWidthTiles; x++)
            {
                var first = x == 0 && y == 0;
                var sources = new List<CanvasSourceRef> { OwnerSource() };
                var keys = new List<SpecialPersistenceKey>();
                if (first)
                {
                    sources.Add(new CanvasSourceRef(CanvasSourceKind.Boundary, "BOUNDARY_CRATER_ROOT", 20, true,
                        new[] { SectorCanvasLayerKind.Background }));
                    sources.Add(new CanvasSourceRef(CanvasSourceKind.SpecialRegion, "SR_VILLAGE", 25, false,
                        new[] { SectorCanvasLayerKind.Marker }));
                    keys.Add(new SpecialPersistenceKey("SR_STATE_VILLAGE_REWARD_TREASURE"));
                }
                yield return new SectorCanvasCell(new LocalTileCoord(x, y),
                    new SectorCanvasLayerSnapshot(
                        ResolvedLayerValue.FromId("SOLID_STONE"),
                        first ? ResolvedLayerValue.FromId("BG_BOUNDARY") : ResolvedLayerValue.Empty,
                        ResolvedLayerValue.Empty,
                        ResolvedLayerValue.Empty,
                        ResolvedLayerValue.FromId("MAT_STONE"),
                        ResolvedLayerValue.Empty,
                        first ? ResolvedLayerValue.FromId("MARKER_SPECIAL") : ResolvedLayerValue.Empty,
                        ResolvedLayerValue.FromId("TC_CANVAS_OWNER")),
                    new SectorCanvasProvenance(sources, keys));
            }
        }

        private static CanvasSourceRef OwnerSource()
            => new CanvasSourceRef(CanvasSourceKind.TerrainCluster, "TC_CANVAS_OWNER", 30, true,
                new[] { SectorCanvasLayerKind.Solid, SectorCanvasLayerKind.Owner });

        private static GeneratedSliceSet CreateSlices(SectorCanvasContract canvas)
        {
            var canvasResult = SectorCanvasContractValidator.Validate(canvas);
            var slices = new List<GeneratedMicroChunkSlice>();
            for (var sliceY = 0; sliceY < WorldGenConstants.MicroChunkRowsPerSector; sliceY++)
            for (var sliceX = 0; sliceX < WorldGenConstants.MicroChunkColumnsPerSector; sliceX++)
            {
                var cells = new List<GeneratedSliceCell>();
                for (var localY = 0; localY < WorldGenConstants.MicroChunkHeightTiles; localY++)
                for (var localX = 0; localX < WorldGenConstants.MicroChunkWidthTiles; localX++)
                {
                    var canvasX = sliceX * WorldGenConstants.MicroChunkWidthTiles + localX;
                    var canvasY = sliceY * WorldGenConstants.MicroChunkHeightTiles + localY;
                    var source = canvas.Cells[canvasY * WorldGenConstants.SectorWidthTiles + canvasX];
                    cells.Add(new GeneratedSliceCell(new LocalTileCoord(localX, localY), source.Layers, source.Provenance));
                }
                slices.Add(new GeneratedMicroChunkSlice(new GeneratedSliceCoord(sliceX, sliceY), cells,
                    new GeneratedSliceProvenance(canvas.Id, canvasResult.CanonicalDigest,
                        canvas.ValidationStamp.StableDigest, GeneratedSliceTransform.None)));
            }
            return new GeneratedSliceSet(canvas.Id, slices, GeneratedSliceBoundaryRole.GeneratedOutput);
        }

        private static void AssertError(SectorCanvasValidationResult result, SectorCanvasValidationErrorCode code)
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(code), Join(result));
        }

        private static void AssertError(GeneratedSliceValidationResult result, GeneratedSliceValidationErrorCode code)
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Select(value => value.Code), Does.Contain(code), Join(result));
        }

        private static string Join(SectorCanvasValidationResult result)
            => string.Join("\n", result.Errors.Select(value => value.ToString()));
        private static string Join(GeneratedSliceValidationResult result)
            => string.Join("\n", result.Errors.Select(value => value.ToString()));
    }
}
