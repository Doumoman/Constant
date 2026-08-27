using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Baking
{
    public enum GeneratedSliceValidationErrorCode
    {
        UnvalidatedSliceSource = 1,
        InvalidSliceCount = 2,
        InvalidSliceCoord = 3,
        InvalidSliceCellCount = 4,
        SliceGapOrOverlap = 5,
        SliceMappingMismatch = 6,
        ProvenanceMismatch = 7,
        ForbiddenSliceTransform = 8,
        AuthoringGeneratedBoundaryViolation = 9,
    }

    public sealed class GeneratedSliceValidationError :
        IEquatable<GeneratedSliceValidationError>, IComparable<GeneratedSliceValidationError>
    {
        public GeneratedSliceValidationError(GeneratedSliceValidationErrorCode code, string path, string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }
        public GeneratedSliceValidationErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }
        public int CompareTo(GeneratedSliceValidationError other)
        {
            if (other == null) return -1;
            var code = ((int)Code).CompareTo((int)other.Code);
            if (code != 0) return code;
            var path = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return path != 0 ? path : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }
        public bool Equals(GeneratedSliceValidationError other) => other != null && Code == other.Code &&
            string.Equals(Path, other.Path, StringComparison.Ordinal) && string.Equals(Detail, other.Detail, StringComparison.Ordinal);
        public override bool Equals(object obj) => Equals(obj as GeneratedSliceValidationError);
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Code;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Path);
                return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Detail);
            }
        }
        public override string ToString() => Code + "|" + Path + "|" + Detail;
    }

    public sealed class GeneratedSliceValidationResult
    {
        private readonly ReadOnlyCollection<GeneratedSliceValidationError> errors;
        internal GeneratedSliceValidationResult(
            GeneratedSliceSet sliceSet,
            IEnumerable<GeneratedSliceValidationError> errors,
            string canonicalDigest)
        {
            var copy = errors.Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            this.errors = new ReadOnlyCollection<GeneratedSliceValidationError>(copy);
            SliceSet = copy.Length == 0 ? sliceSet : null;
            CanonicalDigest = copy.Length == 0 ? canonicalDigest ?? string.Empty : string.Empty;
        }
        public bool IsValid => SliceSet != null && errors.Count == 0;
        public GeneratedSliceSet SliceSet { get; }
        public IReadOnlyList<GeneratedSliceValidationError> Errors => errors;
        public string CanonicalDigest { get; }
    }

    public static class GeneratedSliceContractValidator
    {
        public static GeneratedSliceValidationResult Validate(GeneratedSliceSet sliceSet, SectorCanvasContract sourceCanvas)
        {
            var errors = new List<GeneratedSliceValidationError>();
            if (sliceSet == null)
            {
                Add(errors, GeneratedSliceValidationErrorCode.InvalidSliceCount, "sliceSet", "Slice set is required.");
                return new GeneratedSliceValidationResult(null, errors, string.Empty);
            }
            var canvasResult = SectorCanvasContractValidator.Validate(sourceCanvas);
            if (!canvasResult.IsValid)
                Add(errors, GeneratedSliceValidationErrorCode.ProvenanceMismatch, "canvas", "Source Canvas contract is invalid.");
            if (sourceCanvas == null || sourceCanvas.ValidationStamp == null ||
                sourceCanvas.ValidationStamp.State != SectorCanvasValidationState.Validated)
                Add(errors, GeneratedSliceValidationErrorCode.UnvalidatedSliceSource, "canvas.stamp", "Only a Validated Canvas can be projected.");
            if (sourceCanvas != null && sliceSet.SourceCanvasId != sourceCanvas.Id)
                Add(errors, GeneratedSliceValidationErrorCode.ProvenanceMismatch, "sourceCanvasId", "Slice set references a different Canvas.");
            if (sliceSet.BoundaryRole != GeneratedSliceBoundaryRole.GeneratedOutput)
                Add(errors, GeneratedSliceValidationErrorCode.AuthoringGeneratedBoundaryViolation, "boundaryRole",
                    "Generated Slice cannot be promoted to Authoring source.");
            if (sliceSet.Slices.Count != WorldGenConstants.MicroChunksPerSector)
                Add(errors, GeneratedSliceValidationErrorCode.InvalidSliceCount, "slices.count", sliceSet.Slices.Count.ToString());

            var canvasByIndex = sourceCanvas == null
                ? new Dictionary<int, SectorCanvasCell>()
                : sourceCanvas.Cells.Where(value => value != null).GroupBy(value => value.CanonicalIndex)
                    .ToDictionary(value => value.Key, value => value.First());
            var sliceIndices = new HashSet<int>();
            var canvasIndices = new HashSet<int>();
            var canvasDigest = canvasResult.IsValid ? canvasResult.CanonicalDigest : string.Empty;
            var stampDigest = sourceCanvas == null || sourceCanvas.ValidationStamp == null
                ? string.Empty
                : sourceCanvas.ValidationStamp.StableDigest;

            foreach (var slice in sliceSet.Slices)
            {
                if (slice == null)
                {
                    Add(errors, GeneratedSliceValidationErrorCode.InvalidSliceCoord, "slices", "Null slice.");
                    continue;
                }
                var slicePath = "slices/" + slice.Coordinate;
                var coordValid = slice.Coordinate.X >= 0 &&
                                 slice.Coordinate.X < WorldGenConstants.MicroChunkColumnsPerSector &&
                                 slice.Coordinate.Y >= 0 &&
                                 slice.Coordinate.Y < WorldGenConstants.MicroChunkRowsPerSector;
                if (!coordValid || !sliceIndices.Add(slice.Coordinate.CanonicalIndex))
                    Add(errors, GeneratedSliceValidationErrorCode.InvalidSliceCoord, slicePath,
                        "Slice coordinates must be unique 0..3 in canonical 4x4 space.");
                if (slice.Cells.Count != WorldGenConstants.TilesPerMicroChunk)
                    Add(errors, GeneratedSliceValidationErrorCode.InvalidSliceCellCount, slicePath + "/cells",
                        slice.Cells.Count.ToString());
                ValidateSliceProvenance(slice, sliceSet, canvasDigest, stampDigest, slicePath, errors);

                var localIndices = new HashSet<int>();
                foreach (var cell in slice.Cells)
                {
                    if (cell == null)
                    {
                        Add(errors, GeneratedSliceValidationErrorCode.InvalidSliceCellCount, slicePath + "/cells", "Null cell.");
                        continue;
                    }
                    var local = cell.LocalCoordinate;
                    var validLocal = local.X >= 0 && local.X < WorldGenConstants.MicroChunkWidthTiles &&
                                     local.Y >= 0 && local.Y < WorldGenConstants.MicroChunkHeightTiles;
                    if (!validLocal || !localIndices.Add(cell.CanonicalIndex))
                        Add(errors, GeneratedSliceValidationErrorCode.InvalidSliceCellCount,
                            slicePath + "/cells/" + local.X + "," + local.Y, "Local cells must be unique 12x8 coordinates.");
                    if (!coordValid || !validLocal) continue;

                    var canvasX = slice.Coordinate.X * WorldGenConstants.MicroChunkWidthTiles + local.X;
                    var canvasY = slice.Coordinate.Y * WorldGenConstants.MicroChunkHeightTiles + local.Y;
                    var canvasIndex = canvasY * WorldGenConstants.SectorWidthTiles + canvasX;
                    if (!canvasIndices.Add(canvasIndex))
                        Add(errors, GeneratedSliceValidationErrorCode.SliceGapOrOverlap, slicePath + "/cells/" + cell.CanonicalIndex,
                            "Canvas cell was projected more than once.");
                    if (!canvasByIndex.TryGetValue(canvasIndex, out var expected))
                    {
                        Add(errors, GeneratedSliceValidationErrorCode.SliceGapOrOverlap, slicePath + "/cells/" + cell.CanonicalIndex,
                            "Projected Canvas cell is missing.");
                        continue;
                    }
                    var projected = new SectorCanvasCell(new LocalTileCoord(canvasX, canvasY), cell.Layers, cell.Provenance);
                    if (!BakingCanonicalDigest.AreCellsEquivalent(expected, projected))
                    {
                        Add(errors, GeneratedSliceValidationErrorCode.SliceMappingMismatch,
                            slicePath + "/cells/" + cell.CanonicalIndex, "Cell value changed during projection.");
                        if (expected.Provenance == null || !expected.Provenance.Equals(cell.Provenance))
                            Add(errors, GeneratedSliceValidationErrorCode.ProvenanceMismatch,
                                slicePath + "/cells/" + cell.CanonicalIndex, "Cell provenance or persistence key was lost.");
                    }
                }
                for (var index = 0; index < WorldGenConstants.TilesPerMicroChunk; index++)
                    if (!localIndices.Contains(index))
                        Add(errors, GeneratedSliceValidationErrorCode.InvalidSliceCellCount,
                            slicePath + "/cells/" + index, "Missing local canonical index.");
            }

            for (var index = 0; index < WorldGenConstants.MicroChunksPerSector; index++)
                if (!sliceIndices.Contains(index))
                    Add(errors, GeneratedSliceValidationErrorCode.InvalidSliceCoord, "slices/" + index, "Missing slice index.");
            for (var index = 0; index < WorldGenConstants.TilesPerSector; index++)
                if (!canvasIndices.Contains(index))
                    Add(errors, GeneratedSliceValidationErrorCode.SliceGapOrOverlap, "canvas/" + index, "Canvas cell was not projected.");

            return errors.Count == 0
                ? new GeneratedSliceValidationResult(sliceSet, errors, ComputeDigest(sliceSet))
                : new GeneratedSliceValidationResult(null, errors, string.Empty);
        }

        private static void ValidateSliceProvenance(
            GeneratedMicroChunkSlice slice,
            GeneratedSliceSet set,
            string canvasDigest,
            string stampDigest,
            string path,
            ICollection<GeneratedSliceValidationError> errors)
        {
            var provenance = slice.Provenance;
            if (provenance == null || provenance.SourceCanvasId != set.SourceCanvasId ||
                !string.Equals(provenance.SourceCanvasDigest, canvasDigest, StringComparison.Ordinal) ||
                !string.Equals(provenance.SourceValidationStampDigest, stampDigest, StringComparison.Ordinal))
                Add(errors, GeneratedSliceValidationErrorCode.ProvenanceMismatch, path + "/provenance",
                    "Slice must preserve Canvas ID, digest, and validation stamp digest.");
            if (provenance == null || provenance.Transform != GeneratedSliceTransform.None)
                Add(errors, GeneratedSliceValidationErrorCode.ForbiddenSliceTransform, path + "/transform",
                    "Rotation, mirror, resampling, padding, and mutation are forbidden.");
        }

        private static string ComputeDigest(GeneratedSliceSet set)
        {
            var material = new StringBuilder();
            material.Append(set.SourceCanvasId.Value).Append('\n').Append((int)set.BoundaryRole).Append('\n');
            foreach (var slice in set.Slices)
            {
                material.Append(slice.Coordinate.X).Append(',').Append(slice.Coordinate.Y).Append('|')
                    .Append(slice.Provenance.SourceCanvasDigest).Append('|')
                    .Append(slice.Provenance.SourceValidationStampDigest).Append('|')
                    .Append((int)slice.Provenance.Transform).Append('\n');
                foreach (var cell in slice.Cells)
                {
                    var projectedX = slice.Coordinate.X * WorldGenConstants.MicroChunkWidthTiles + cell.LocalCoordinate.X;
                    var projectedY = slice.Coordinate.Y * WorldGenConstants.MicroChunkHeightTiles + cell.LocalCoordinate.Y;
                    material.Append(cell.LocalCoordinate.X).Append(',').Append(cell.LocalCoordinate.Y).Append('|')
                        .Append(BakingCanonicalDigest.CellSemantic(
                            new SectorCanvasCell(new LocalTileCoord(projectedX, projectedY), cell.Layers, cell.Provenance)))
                        .Append('\n');
                }
            }
            return BakingCanonicalDigest.Sha256(material.ToString());
        }

        private static void Add(
            ICollection<GeneratedSliceValidationError> errors,
            GeneratedSliceValidationErrorCode code,
            string path,
            string detail)
            => errors.Add(new GeneratedSliceValidationError(code, path, detail));
    }
}
