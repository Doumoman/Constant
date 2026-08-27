using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.MicroPatterns
{
    public enum MicroPatternCellSchemaErrorCode
    {
        MissingInputFile = 1,
        InvalidBom = 2,
        HeaderMismatch = 3,
        RowFieldCountMismatch = 4,
        InvalidCatalogField = 5,
        DuplicatePatternId = 6,
        OrphanCellRow = 7,
        MissingCellRows = 8,
        InvalidCoordinate = 9,
        MissingCell = 10,
        DuplicateCellLayer = 11,
        UnknownLayer = 12,
        UnknownOperation = 13,
        LayerOperationMismatch = 14,
        MissingPayload = 15,
        UnexpectedPayload = 16,
        InvalidPayload = 17,
        DomainValidationFailed = 18,
        AtomicPublishRejected = 19,
        CsvSyntaxError = 20,
    }

    public sealed class MicroPatternCellSchemaError :
        IEquatable<MicroPatternCellSchemaError>,
        IComparable<MicroPatternCellSchemaError>
    {
        public MicroPatternCellSchemaError(
            MicroPatternCellSchemaErrorCode code,
            string sourceFile,
            int recordNumber,
            string patternId,
            int? x,
            int? y,
            string layer,
            string field,
            string detail)
        {
            Code = code;
            SourceFile = sourceFile ?? string.Empty;
            RecordNumber = recordNumber;
            PatternId = patternId ?? string.Empty;
            X = x;
            Y = y;
            Layer = layer ?? string.Empty;
            Field = field ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public MicroPatternCellSchemaErrorCode Code { get; }
        public string SourceFile { get; }
        public int RecordNumber { get; }
        public string PatternId { get; }
        public int? X { get; }
        public int? Y { get; }
        public string Layer { get; }
        public string Field { get; }
        public string Detail { get; }

        public int CompareTo(MicroPatternCellSchemaError other)
        {
            if (other == null) return -1;
            var comparison = string.Compare(SourceFile, other.SourceFile, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = RecordNumber.CompareTo(other.RecordNumber);
            if (comparison != 0) return comparison;
            comparison = string.Compare(PatternId, other.PatternId, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = CompareNullable(X, other.X);
            if (comparison != 0) return comparison;
            comparison = CompareNullable(Y, other.Y);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Layer, other.Layer, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Field, other.Field, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(MicroPatternCellSchemaError other)
        {
            return other != null && CompareTo(other) == 0;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MicroPatternCellSchemaError);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Code;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(SourceFile);
                hash = (hash * 397) ^ RecordNumber;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(PatternId);
                hash = (hash * 397) ^ X.GetHashCode();
                hash = (hash * 397) ^ Y.GetHashCode();
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Layer);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Field);
                return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Detail);
            }
        }

        public override string ToString()
        {
            return Code + "|" + SourceFile + "|record=" + RecordNumber + "|pattern=" + PatternId +
                   "|x=" + Number(X) + "|y=" + Number(Y) + "|layer=" + Layer +
                   "|field=" + Field + "|" + Detail;
        }

        private static int CompareNullable(int? left, int? right)
        {
            if (!left.HasValue) return right.HasValue ? -1 : 0;
            return right.HasValue ? left.Value.CompareTo(right.Value) : 1;
        }

        private static string Number(int? value)
        {
            return value.HasValue
                ? value.Value.ToString(CultureInfo.InvariantCulture)
                : string.Empty;
        }
    }

    public sealed class MicroPatternCellSchemaResult
    {
        private readonly ReadOnlyCollection<MicroPatternCellSchemaError> errors;

        internal MicroPatternCellSchemaResult(
            MicroPatternAuthoringCatalog catalog,
            IEnumerable<MicroPatternCellSchemaError> sourceErrors)
        {
            var ordered = (sourceErrors ?? throw new ArgumentNullException(nameof(sourceErrors)))
                .Where(value => value != null)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            errors = new ReadOnlyCollection<MicroPatternCellSchemaError>(ordered);
            if (ordered.Length > 0 && catalog != null)
            {
                throw new ArgumentException("A failed schema build cannot publish a catalog.");
            }

            Catalog = ordered.Length == 0 ? catalog : null;
        }

        public bool Success => errors.Count == 0;
        public bool Published => Catalog != null;
        public bool IsHeaderOnly => Success && Catalog == null;
        public MicroPatternAuthoringCatalog Catalog { get; }
        public IReadOnlyList<MicroPatternCellSchemaError> Errors => errors;
        public string StableDigest => Catalog == null ? string.Empty : Catalog.StableDigest;
    }

    public sealed class MicroPatternCellSchemaBuilder
    {
        private static readonly MicroPatternLayer[] CanonicalLayers =
            (MicroPatternLayer[])Enum.GetValues(typeof(MicroPatternLayer));

        public MicroPatternCellSchemaResult Build(
            IEnumerable<MicroPatternCatalogRowV2> sourceCatalogRows,
            IEnumerable<MicroPatternCellRowV2> sourceCellRows)
        {
            if (sourceCatalogRows == null) throw new ArgumentNullException(nameof(sourceCatalogRows));
            if (sourceCellRows == null) throw new ArgumentNullException(nameof(sourceCellRows));

            var catalogRows = sourceCatalogRows.ToArray();
            var cellRows = sourceCellRows.ToArray();
            if (catalogRows.Length == 0 && cellRows.Length == 0)
            {
                return new MicroPatternCellSchemaResult(null, Array.Empty<MicroPatternCellSchemaError>());
            }

            var errors = new List<MicroPatternCellSchemaError>();
            var parsedCatalog = ParseCatalogRows(catalogRows, errors);
            var parsedCells = ParseCellRows(cellRows, parsedCatalog.Keys, errors);
            var definitions = BuildDefinitions(parsedCatalog, parsedCells, errors);

            if (errors.Count > 0)
            {
                errors.Add(Error(
                    MicroPatternCellSchemaErrorCode.AtomicPublishRejected,
                    string.Empty, 0, string.Empty, null, null, string.Empty,
                    "catalog", "One or more accumulated errors rejected publication."));
                return new MicroPatternCellSchemaResult(null, errors);
            }

            return new MicroPatternCellSchemaResult(
                new MicroPatternAuthoringCatalog(definitions),
                errors);
        }

        private static Dictionary<string, ParsedCatalogRow> ParseCatalogRows(
            IEnumerable<MicroPatternCatalogRowV2> rows,
            ICollection<MicroPatternCellSchemaError> errors)
        {
            var parsed = new Dictionary<string, ParsedCatalogRow>(StringComparer.Ordinal);
            var seenPatternIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                if (row == null)
                {
                    errors.Add(Error(MicroPatternCellSchemaErrorCode.InvalidCatalogField,
                        string.Empty, 0, string.Empty, null, null, string.Empty,
                        "row", "Catalog row is required."));
                    continue;
                }

                var rowValid = true;
                if (!IsPatternId(row.PatternId))
                {
                    AddCatalogError(errors, row, "pattern_id", row.PatternId);
                    rowValid = false;
                }
                else if (!seenPatternIds.Add(row.PatternId))
                {
                    errors.Add(Error(MicroPatternCellSchemaErrorCode.DuplicatePatternId,
                        row.SourceFile, row.RecordNumber, row.PatternId, null, null,
                        string.Empty, "pattern_id", "Pattern ID occurs more than once."));
                    continue;
                }

                if (!TryParsePositiveInteger(row.SelectionWeight, out var weight) ||
                    weight < MicroPatternDefinition.MinimumWeight ||
                    weight > MicroPatternDefinition.MaximumWeight)
                {
                    AddCatalogError(errors, row, "selection_weight", row.SelectionWeight);
                    rowValid = false;
                }

                var biomes = ParseBiomes(row, errors, ref rowValid);
                var transforms = ParseTransforms(row, errors, ref rowValid);
                if (!MicroPatternCellTokenCodec.TryParseProtectedPolicy(
                        row.ProtectedPolicy, out var protectedPolicy))
                {
                    AddCatalogError(errors, row, "protected_policy", row.ProtectedPolicy);
                    rowValid = false;
                }

                if (rowValid)
                {
                    parsed.Add(row.PatternId, new ParsedCatalogRow(
                        row, weight, biomes, transforms, protectedPolicy));
                }
            }

            return parsed;
        }

        private static IReadOnlyList<MoonpalaceBiomeId> ParseBiomes(
            MicroPatternCatalogRowV2 row,
            ICollection<MicroPatternCellSchemaError> errors,
            ref bool rowValid)
        {
            var result = new List<MoonpalaceBiomeId>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var token in SplitExactList(row.BiomeIds))
            {
                if (token.Length == 0 || !seen.Add(token) ||
                    !MicroPatternCellTokenCodec.TryParseBiome(token, out var biome))
                {
                    AddCatalogError(errors, row, "biome_ids", token);
                    rowValid = false;
                }
                else
                {
                    result.Add(biome);
                }
            }

            if (result.Count == 0)
            {
                AddCatalogError(errors, row, "biome_ids", row.BiomeIds);
                rowValid = false;
            }

            return result;
        }

        private static IReadOnlyList<MicroPatternTransform> ParseTransforms(
            MicroPatternCatalogRowV2 row,
            ICollection<MicroPatternCellSchemaError> errors,
            ref bool rowValid)
        {
            var result = new List<MicroPatternTransform>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var token in SplitExactList(row.AllowedTransforms))
            {
                if (token.Length == 0 || !seen.Add(token) ||
                    !MicroPatternCellTokenCodec.TryParseTransform(token, out var transform))
                {
                    AddCatalogError(errors, row, "allowed_transforms", token);
                    rowValid = false;
                }
                else
                {
                    result.Add(transform);
                }
            }

            if (result.Count == 0)
            {
                AddCatalogError(errors, row, "allowed_transforms", row.AllowedTransforms);
                rowValid = false;
            }

            return result;
        }

        private static Dictionary<string, PatternCells> ParseCellRows(
            IEnumerable<MicroPatternCellRowV2> rows,
            IEnumerable<string> knownPatternIds,
            ICollection<MicroPatternCellSchemaError> errors)
        {
            var known = new HashSet<string>(knownPatternIds, StringComparer.Ordinal);
            var byPattern = new Dictionary<string, PatternCells>(StringComparer.Ordinal);
            var seenLayers = new HashSet<CellLayerKey>();
            foreach (var row in rows)
            {
                if (row == null)
                {
                    errors.Add(Error(MicroPatternCellSchemaErrorCode.InvalidCatalogField,
                        string.Empty, 0, string.Empty, null, null, string.Empty,
                        "row", "Cell row is required."));
                    continue;
                }

                var patternValid = IsPatternId(row.PatternId);
                if (!patternValid)
                {
                    errors.Add(CellError(MicroPatternCellSchemaErrorCode.InvalidCatalogField,
                        row, null, null, "pattern_id", row.PatternId));
                }

                var xValid = TryParseCoordinate(row.LocalX, out var x);
                var yValid = TryParseCoordinate(row.LocalY, out var y);
                var inRange = xValid && yValid && x >= 0 && x < 4 && y >= 0 && y < 4;
                if (!inRange)
                {
                    errors.Add(CellError(MicroPatternCellSchemaErrorCode.InvalidCoordinate,
                        row, xValid ? x : (int?)null, yValid ? y : (int?)null,
                        "coordinate", "Expected exact invariant x/y in 0..3."));
                }

                var layerValid = MicroPatternCellTokenCodec.TryParseLayer(row.Layer, out var layer);
                if (!layerValid)
                {
                    errors.Add(CellError(MicroPatternCellSchemaErrorCode.UnknownLayer,
                        row, xValid ? x : (int?)null, yValid ? y : (int?)null,
                        "layer", row.Layer));
                }

                var operationValid = MicroPatternCellTokenCodec.TryParseOperation(
                    row.Operation, out var operation);
                if (!operationValid)
                {
                    errors.Add(CellError(MicroPatternCellSchemaErrorCode.UnknownOperation,
                        row, xValid ? x : (int?)null, yValid ? y : (int?)null,
                        "operation", row.Operation));
                }

                var ownerKnown = patternValid && known.Contains(row.PatternId);
                if (patternValid && !ownerKnown)
                {
                    errors.Add(CellError(MicroPatternCellSchemaErrorCode.OrphanCellRow,
                        row, xValid ? x : (int?)null, yValid ? y : (int?)null,
                        "pattern_id", "No catalog row owns this cell."));
                }

                if (ownerKnown && inRange)
                {
                    GetPatternCells(byPattern, row.PatternId).Coordinates.Add(new CellCoordinate(x, y));
                }

                if (layerValid && operationValid && !IsAllowed(layer, operation))
                {
                    errors.Add(CellError(MicroPatternCellSchemaErrorCode.LayerOperationMismatch,
                        row, xValid ? x : (int?)null, yValid ? y : (int?)null,
                        "operation", row.Operation + " is not valid for " + row.Layer + "."));
                }

                var payloadValid = ValidatePayload(row, operationValid, operation, xValid, x,
                    yValid, y, errors);
                if (!ownerKnown || !inRange || !layerValid || !operationValid ||
                    !IsAllowed(layer, operation) || !payloadValid)
                {
                    continue;
                }

                var key = new CellLayerKey(row.PatternId, x, y, layer);
                if (!seenLayers.Add(key))
                {
                    errors.Add(CellError(MicroPatternCellSchemaErrorCode.DuplicateCellLayer,
                        row, x, y, "layer", "Only one row per pattern/x/y/layer is allowed."));
                    continue;
                }

                GetPatternCells(byPattern, row.PatternId).Instructions[key.Coordinate]
                    .Add(layer, new MicroPatternInstruction(layer, operation, row.PayloadId));
            }

            return byPattern;
        }

        private static List<MicroPatternDefinition> BuildDefinitions(
            IReadOnlyDictionary<string, ParsedCatalogRow> catalog,
            IReadOnlyDictionary<string, PatternCells> cellsByPattern,
            ICollection<MicroPatternCellSchemaError> errors)
        {
            var definitions = new List<MicroPatternDefinition>();
            foreach (var pair in catalog.OrderBy(value => value.Key, StringComparer.Ordinal))
            {
                if (!cellsByPattern.TryGetValue(pair.Key, out var patternCells) ||
                    patternCells.Coordinates.Count == 0)
                {
                    errors.Add(Error(MicroPatternCellSchemaErrorCode.MissingCellRows,
                        pair.Value.Source.SourceFile, pair.Value.Source.RecordNumber, pair.Key,
                        null, null, string.Empty, "cells", "Catalog pattern has no cell rows."));
                    continue;
                }

                var hasMissing = false;
                for (var y = 0; y < 4; y++)
                {
                    for (var x = 0; x < 4; x++)
                    {
                        if (patternCells.Coordinates.Contains(new CellCoordinate(x, y))) continue;
                        hasMissing = true;
                        errors.Add(Error(MicroPatternCellSchemaErrorCode.MissingCell,
                            pair.Value.Source.SourceFile, pair.Value.Source.RecordNumber, pair.Key,
                            x, y, string.Empty, "coordinate", "Explicit 4x4 cell is missing."));
                    }
                }

                if (hasMissing) continue;

                var cells = new List<MicroPatternCell>(16);
                for (var y = 0; y < 4; y++)
                {
                    for (var x = 0; x < 4; x++)
                    {
                        var coordinate = new CellCoordinate(x, y);
                        var explicitInstructions = patternCells.Instructions[coordinate];
                        var normalized = CanonicalLayers.Select(layer =>
                            explicitInstructions.TryGetValue(layer, out var instruction)
                                ? instruction
                                : new MicroPatternInstruction(layer, MicroPatternOperation.NoChange));
                        cells.Add(new MicroPatternCell(new LocalTileCoord(x, y), normalized));
                    }
                }

                var source = pair.Value;
                var definition = new MicroPatternDefinition(
                    new MicroPatternId(pair.Key), 4, 4, cells, source.Weight,
                    source.Biomes, source.Transforms, source.ProtectedPolicy);
                var validation = MicroPatternValidator.Validate(definition);
                if (!validation.IsValid)
                {
                    foreach (var domainError in validation.Errors)
                    {
                        errors.Add(Error(MicroPatternCellSchemaErrorCode.DomainValidationFailed,
                            source.Source.SourceFile, source.Source.RecordNumber, pair.Key,
                            null, null, string.Empty, domainError.Path, domainError.ToString()));
                    }
                }
                else
                {
                    definitions.Add(validation.Definition);
                }
            }

            return definitions;
        }

        private static bool ValidatePayload(
            MicroPatternCellRowV2 row,
            bool operationValid,
            MicroPatternOperation operation,
            bool xValid,
            int x,
            bool yValid,
            int y,
            ICollection<MicroPatternCellSchemaError> errors)
        {
            if (!operationValid) return false;
            var xValue = xValid ? x : (int?)null;
            var yValue = yValid ? y : (int?)null;
            if (operation == MicroPatternOperation.NoChange ||
                operation == MicroPatternOperation.AddSolid ||
                operation == MicroPatternOperation.CarveAir)
            {
                if (row.PayloadId.Length == 0) return true;
                errors.Add(CellError(MicroPatternCellSchemaErrorCode.UnexpectedPayload,
                    row, xValue, yValue, "payload_id", row.PayloadId));
                return false;
            }

            if (row.PayloadId.Length == 0)
            {
                errors.Add(CellError(MicroPatternCellSchemaErrorCode.MissingPayload,
                    row, xValue, yValue, "payload_id", "Set operation requires a payload."));
                return false;
            }

            if (!IsStablePayload(row.PayloadId))
            {
                errors.Add(CellError(MicroPatternCellSchemaErrorCode.InvalidPayload,
                    row, xValue, yValue, "payload_id", row.PayloadId));
                return false;
            }

            return true;
        }

        private static bool IsAllowed(MicroPatternLayer layer, MicroPatternOperation operation)
        {
            if (operation == MicroPatternOperation.NoChange) return true;
            switch (layer)
            {
                case MicroPatternLayer.Geometry:
                    return operation == MicroPatternOperation.AddSolid ||
                           operation == MicroPatternOperation.CarveAir;
                case MicroPatternLayer.Surface: return operation == MicroPatternOperation.SetSurface;
                case MicroPatternLayer.Affordance: return operation == MicroPatternOperation.SetAffordance;
                case MicroPatternLayer.Material: return operation == MicroPatternOperation.SetMaterial;
                case MicroPatternLayer.Hazard: return operation == MicroPatternOperation.SetHazard;
                case MicroPatternLayer.Marker: return operation == MicroPatternOperation.SetMarker;
                default: return false;
            }
        }

        private static bool IsPatternId(string value)
        {
            if (string.IsNullOrEmpty(value) || !value.StartsWith("MP_", StringComparison.Ordinal) ||
                value.Length == 3)
            {
                return false;
            }

            for (var index = 3; index < value.Length; index++)
            {
                var character = value[index];
                if ((character < 'A' || character > 'Z') &&
                    (character < '0' || character > '9') && character != '_')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsStablePayload(string value)
        {
            if (string.IsNullOrEmpty(value) || value[0] < 'A' || value[0] > 'Z') return false;
            for (var index = 1; index < value.Length; index++)
            {
                var character = value[index];
                if ((character < 'A' || character > 'Z') &&
                    (character < '0' || character > '9') && character != '_')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryParsePositiveInteger(string value, out int result)
        {
            if (string.IsNullOrEmpty(value) || value.Any(character => character < '0' || character > '9'))
            {
                result = default;
                return false;
            }

            return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out result);
        }

        private static bool TryParseCoordinate(string value, out int result)
        {
            if (string.IsNullOrEmpty(value) ||
                value.Any(character => character < '0' || character > '9'))
            {
                result = default;
                return false;
            }

            return int.TryParse(value, NumberStyles.None,
                CultureInfo.InvariantCulture, out result);
        }

        private static IEnumerable<string> SplitExactList(string value)
        {
            return string.IsNullOrEmpty(value) ? new[] { string.Empty } : value.Split('|');
        }

        private static PatternCells GetPatternCells(
            IDictionary<string, PatternCells> values,
            string patternId)
        {
            if (!values.TryGetValue(patternId, out var result))
            {
                result = new PatternCells();
                values.Add(patternId, result);
            }

            return result;
        }

        private static void AddCatalogError(
            ICollection<MicroPatternCellSchemaError> errors,
            MicroPatternCatalogRowV2 row,
            string field,
            string detail)
        {
            errors.Add(Error(MicroPatternCellSchemaErrorCode.InvalidCatalogField,
                row.SourceFile, row.RecordNumber, row.PatternId, null, null,
                string.Empty, field, detail));
        }

        private static MicroPatternCellSchemaError CellError(
            MicroPatternCellSchemaErrorCode code,
            MicroPatternCellRowV2 row,
            int? x,
            int? y,
            string field,
            string detail)
        {
            return Error(code, row.SourceFile, row.RecordNumber, row.PatternId,
                x, y, row.Layer, field, detail);
        }

        private static MicroPatternCellSchemaError Error(
            MicroPatternCellSchemaErrorCode code,
            string sourceFile,
            int recordNumber,
            string patternId,
            int? x,
            int? y,
            string layer,
            string field,
            string detail)
        {
            return new MicroPatternCellSchemaError(code, sourceFile, recordNumber,
                patternId, x, y, layer, field, detail);
        }

        private sealed class ParsedCatalogRow
        {
            public ParsedCatalogRow(
                MicroPatternCatalogRowV2 source,
                int weight,
                IReadOnlyList<MoonpalaceBiomeId> biomes,
                IReadOnlyList<MicroPatternTransform> transforms,
                MicroPatternProtectedPolicy protectedPolicy)
            {
                Source = source;
                Weight = weight;
                Biomes = biomes;
                Transforms = transforms;
                ProtectedPolicy = protectedPolicy;
            }

            public MicroPatternCatalogRowV2 Source { get; }
            public int Weight { get; }
            public IReadOnlyList<MoonpalaceBiomeId> Biomes { get; }
            public IReadOnlyList<MicroPatternTransform> Transforms { get; }
            public MicroPatternProtectedPolicy ProtectedPolicy { get; }
        }

        private sealed class PatternCells
        {
            public PatternCells()
            {
                Coordinates = new HashSet<CellCoordinate>();
                Instructions = new Dictionary<CellCoordinate,
                    Dictionary<MicroPatternLayer, MicroPatternInstruction>>();
                for (var y = 0; y < 4; y++)
                {
                    for (var x = 0; x < 4; x++)
                    {
                        Instructions.Add(new CellCoordinate(x, y),
                            new Dictionary<MicroPatternLayer, MicroPatternInstruction>());
                    }
                }
            }

            public HashSet<CellCoordinate> Coordinates { get; }
            public Dictionary<CellCoordinate,
                Dictionary<MicroPatternLayer, MicroPatternInstruction>> Instructions { get; }
        }

        private readonly struct CellCoordinate : IEquatable<CellCoordinate>
        {
            public CellCoordinate(int x, int y)
            {
                X = x;
                Y = y;
            }

            public int X { get; }
            public int Y { get; }
            public bool Equals(CellCoordinate other) => X == other.X && Y == other.Y;
            public override bool Equals(object obj) => obj is CellCoordinate other && Equals(other);
            public override int GetHashCode() => unchecked((X * 397) ^ Y);
        }

        private readonly struct CellLayerKey : IEquatable<CellLayerKey>
        {
            public CellLayerKey(string patternId, int x, int y, MicroPatternLayer layer)
            {
                PatternId = patternId;
                Coordinate = new CellCoordinate(x, y);
                Layer = layer;
            }

            public string PatternId { get; }
            public CellCoordinate Coordinate { get; }
            public MicroPatternLayer Layer { get; }
            public bool Equals(CellLayerKey other) =>
                string.Equals(PatternId, other.PatternId, StringComparison.Ordinal) &&
                Coordinate.Equals(other.Coordinate) && Layer == other.Layer;
            public override bool Equals(object obj) => obj is CellLayerKey other && Equals(other);
            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = StringComparer.Ordinal.GetHashCode(PatternId);
                    hash = (hash * 397) ^ Coordinate.GetHashCode();
                    return (hash * 397) ^ (int)Layer;
                }
            }
        }
    }
}
