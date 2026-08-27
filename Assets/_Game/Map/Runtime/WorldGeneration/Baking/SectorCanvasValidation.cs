using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.SpecialRegions;

namespace StarNight.Map.WorldGeneration.Baking
{
    public enum SectorCanvasValidationErrorCode
    {
        InvalidId = 1,
        InvalidCanvasDimensions = 2,
        InvalidCanvasCell = 3,
        MissingOrDuplicateCanvasCell = 4,
        InvalidLayerSnapshot = 5,
        InvalidSourceRef = 6,
        ProtectedSourceLost = 7,
        InvalidValidationStamp = 8,
    }

    public sealed class SectorCanvasValidationError :
        IEquatable<SectorCanvasValidationError>, IComparable<SectorCanvasValidationError>
    {
        public SectorCanvasValidationError(SectorCanvasValidationErrorCode code, string path, string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }
        public SectorCanvasValidationErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }
        public int CompareTo(SectorCanvasValidationError other)
        {
            if (other == null) return -1;
            var code = ((int)Code).CompareTo((int)other.Code);
            if (code != 0) return code;
            var path = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return path != 0 ? path : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }
        public bool Equals(SectorCanvasValidationError other) => other != null && Code == other.Code &&
            string.Equals(Path, other.Path, StringComparison.Ordinal) && string.Equals(Detail, other.Detail, StringComparison.Ordinal);
        public override bool Equals(object obj) => Equals(obj as SectorCanvasValidationError);
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

    public sealed class SectorCanvasValidationResult
    {
        private readonly ReadOnlyCollection<SectorCanvasValidationError> errors;
        internal SectorCanvasValidationResult(
            SectorCanvasContract canvas,
            IEnumerable<SectorCanvasValidationError> errors,
            string canonicalDigest)
        {
            var copy = errors.Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            this.errors = new ReadOnlyCollection<SectorCanvasValidationError>(copy);
            Canvas = copy.Length == 0 ? canvas : null;
            CanonicalDigest = copy.Length == 0 ? canonicalDigest ?? string.Empty : string.Empty;
        }
        public bool IsValid => Canvas != null && errors.Count == 0;
        public SectorCanvasContract Canvas { get; }
        public IReadOnlyList<SectorCanvasValidationError> Errors => errors;
        public string CanonicalDigest { get; }
    }

    public static class SectorCanvasContractValidator
    {
        public static SectorCanvasValidationResult Validate(SectorCanvasContract canvas)
        {
            var errors = new List<SectorCanvasValidationError>();
            if (canvas == null)
            {
                Add(errors, SectorCanvasValidationErrorCode.InvalidCanvasCell, "canvas", "Canvas is required.");
                return new SectorCanvasValidationResult(null, errors, string.Empty);
            }

            if (!SpecialRegionValidator.IsStableId(canvas.Id.Value, "CANVAS_"))
                Add(errors, SectorCanvasValidationErrorCode.InvalidId, "id", canvas.Id.Value);
            if (canvas.Width != WorldGenConstants.SectorWidthTiles || canvas.Height != WorldGenConstants.SectorHeightTiles)
                Add(errors, SectorCanvasValidationErrorCode.InvalidCanvasDimensions, "dimensions",
                    canvas.Width + "x" + canvas.Height);

            ValidateCells(canvas, errors);
            ValidateStamp(canvas, errors);
            return errors.Count == 0
                ? new SectorCanvasValidationResult(canvas, errors, BakingCanonicalDigest.ComputeCanvas(canvas))
                : new SectorCanvasValidationResult(null, errors, string.Empty);
        }

        private static void ValidateCells(
            SectorCanvasContract canvas,
            ICollection<SectorCanvasValidationError> errors)
        {
            if (canvas.Cells.Count != WorldGenConstants.TilesPerSector)
                Add(errors, SectorCanvasValidationErrorCode.MissingOrDuplicateCanvasCell, "cells.count", canvas.Cells.Count.ToString());
            var indices = new HashSet<int>();
            foreach (var cell in canvas.Cells)
            {
                if (cell == null)
                {
                    Add(errors, SectorCanvasValidationErrorCode.InvalidCanvasCell, "cells", "Null cell.");
                    continue;
                }
                var path = "cells/" + cell.Coordinate.X + "," + cell.Coordinate.Y;
                if (cell.Coordinate.X < 0 || cell.Coordinate.X >= WorldGenConstants.SectorWidthTiles ||
                    cell.Coordinate.Y < 0 || cell.Coordinate.Y >= WorldGenConstants.SectorHeightTiles)
                    Add(errors, SectorCanvasValidationErrorCode.InvalidCanvasCell, path, "Coordinate is outside 48x32.");
                if (!indices.Add(cell.CanonicalIndex))
                    Add(errors, SectorCanvasValidationErrorCode.MissingOrDuplicateCanvasCell, path, "Duplicate canonical index.");
                ValidateLayers(cell, path, errors);
                ValidateSources(cell, path, errors);
            }
            for (var index = 0; index < WorldGenConstants.TilesPerSector; index++)
                if (!indices.Contains(index))
                    Add(errors, SectorCanvasValidationErrorCode.MissingOrDuplicateCanvasCell, "cells/" + index, "Missing canonical index.");
        }

        private static void ValidateLayers(
            SectorCanvasCell cell,
            string path,
            ICollection<SectorCanvasValidationError> errors)
        {
            if (cell.Layers == null)
            {
                Add(errors, SectorCanvasValidationErrorCode.InvalidLayerSnapshot, path, "Layer snapshot is required.");
                return;
            }
            foreach (SectorCanvasLayerKind layer in Enum.GetValues(typeof(SectorCanvasLayerKind)))
            {
                var value = cell.Layers.Get(layer);
                var valid = value.IsExplicitEmpty
                    ? value.StableId.Length == 0
                    : SpecialRegionValidator.IsStableToken(value.StableId);
                if (!valid || (layer == SectorCanvasLayerKind.Solid && value.StableId == "AIR"))
                    Add(errors, SectorCanvasValidationErrorCode.InvalidLayerSnapshot,
                        path + "/" + layer, "Payload must be a stable ID or explicit empty; AIR must be explicit empty.");
            }
        }

        private static void ValidateSources(
            SectorCanvasCell cell,
            string path,
            ICollection<SectorCanvasValidationError> errors)
        {
            if (cell.Provenance == null)
            {
                Add(errors, SectorCanvasValidationErrorCode.InvalidSourceRef, path, "Provenance is required.");
                return;
            }
            var semantics = new HashSet<string>(StringComparer.Ordinal);
            var owners = 0;
            foreach (var source in cell.Provenance.Sources)
            {
                if (source == null)
                {
                    Add(errors, SectorCanvasValidationErrorCode.InvalidSourceRef, path, "Null source.");
                    continue;
                }
                var layers = new HashSet<SectorCanvasLayerKind>();
                var sourceValid = Enum.IsDefined(typeof(CanvasSourceKind), source.Kind) &&
                                  SpecialRegionValidator.IsStableToken(source.StableId) && source.PassOrder >= 0 &&
                                  source.OwnedLayers.Count != 0;
                foreach (var layer in source.OwnedLayers)
                {
                    if (!Enum.IsDefined(typeof(SectorCanvasLayerKind), layer) || !layers.Add(layer)) sourceValid = false;
                    if (layer == SectorCanvasLayerKind.Owner) owners++;
                }
                if (!semantics.Add(BakingCanonicalDigest.SourceSemantic(source))) sourceValid = false;
                if (!sourceValid)
                    Add(errors, SectorCanvasValidationErrorCode.InvalidSourceRef, path + "/sources/" + source.StableId,
                        "Source kind, ID, pass order, layers, and identity must be valid and unique.");
                if (source.IsProtected && source.OwnedLayers.Count == 0)
                    Add(errors, SectorCanvasValidationErrorCode.ProtectedSourceLost, path + "/sources/" + source.StableId,
                        "Protected source has no preserved layer provenance.");
                if (source.IsProtected && source.OwnedLayers.Contains(SectorCanvasLayerKind.Owner) &&
                    !string.Equals(cell.Layers.Owner.StableId, source.StableId, StringComparison.Ordinal))
                    Add(errors, SectorCanvasValidationErrorCode.ProtectedSourceLost, path + "/owner",
                        "Protected owner ID was replaced.");
            }
            if (owners != 1 || cell.Layers.Owner.IsExplicitEmpty)
                Add(errors, SectorCanvasValidationErrorCode.InvalidLayerSnapshot, path + "/owner",
                    "Exactly one source must own the resolved Owner layer.");

            var keys = new HashSet<SpecialPersistenceKey>();
            foreach (var key in cell.Provenance.PersistenceKeys)
                if (!SpecialRegionValidator.IsStableId(key.Value, "SR_STATE_") || !keys.Add(key))
                    Add(errors, SectorCanvasValidationErrorCode.InvalidSourceRef, path + "/persistence/" + key.Value,
                        "Persistence provenance keys must be stable and unique.");
        }

        private static void ValidateStamp(
            SectorCanvasContract canvas,
            ICollection<SectorCanvasValidationError> errors)
        {
            var stamp = canvas.ValidationStamp;
            if (stamp == null || !Enum.IsDefined(typeof(SectorCanvasValidationState), stamp.State))
            {
                Add(errors, SectorCanvasValidationErrorCode.InvalidValidationStamp, "stamp", "A defined stamp is required.");
                return;
            }
            if (stamp.State == SectorCanvasValidationState.Unvalidated)
            {
                if (new[]
                    {
                        stamp.PassCatalogDigest, stamp.LayerCatalogDigest, stamp.SourceArtifactSetDigest,
                        stamp.ResolvedCellsDigest, stamp.ValidationRulesetVersion,
                    }.Any(value => value.Length != 0))
                    Add(errors, SectorCanvasValidationErrorCode.InvalidValidationStamp, "stamp", "Unvalidated stamp cannot contain approval digests.");
                return;
            }

            var digests = new[]
            {
                stamp.PassCatalogDigest, stamp.LayerCatalogDigest, stamp.SourceArtifactSetDigest,
                stamp.ResolvedCellsDigest, stamp.ValidationRulesetVersion,
            };
            if (digests.Any(value => !IsSha256(value)) ||
                !string.Equals(stamp.PassCatalogDigest, V2PassCatalog.StableDigest, StringComparison.Ordinal) ||
                !string.Equals(stamp.LayerCatalogDigest, GenerationLayerCatalog.StableDigest, StringComparison.Ordinal) ||
                !string.Equals(stamp.SourceArtifactSetDigest,
                    BakingCanonicalDigest.ComputeSourceArtifactSet(canvas.Cells), StringComparison.Ordinal) ||
                !string.Equals(stamp.ResolvedCellsDigest,
                    BakingCanonicalDigest.ComputeResolvedCells(canvas.Cells), StringComparison.Ordinal))
            {
                Add(errors, SectorCanvasValidationErrorCode.InvalidValidationStamp, "stamp",
                    "Validated stamp digests must be complete and match current catalogs, sources, and cells.");
            }
        }

        internal static bool IsSha256(string value)
        {
            if (value == null || value.Length != 64) return false;
            foreach (var character in value)
                if ((character < '0' || character > '9') && (character < 'a' || character > 'f')) return false;
            return true;
        }

        private static void Add(
            ICollection<SectorCanvasValidationError> errors,
            SectorCanvasValidationErrorCode code,
            string path,
            string detail)
            => errors.Add(new SectorCanvasValidationError(code, path, detail));
    }
}
