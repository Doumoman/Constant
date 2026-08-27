using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.MicroPatterns
{
    public enum MicroPatternValidationErrorCode
    {
        MissingInput = 1,
        InvalidPatternId = 2,
        InvalidDimensions = 3,
        InvalidCellCount = 4,
        DuplicateCell = 5,
        MissingCell = 6,
        CellOutOfRange = 7,
        DuplicateLayerInstruction = 8,
        InvalidLayerOperation = 9,
        MissingPayload = 10,
        UnexpectedPayload = 11,
        InvalidPayloadId = 12,
        InvalidWeight = 13,
        MissingBiome = 14,
        DuplicateBiome = 15,
        UnknownBiome = 16,
        MissingTransform = 17,
        DuplicateTransform = 18,
        MissingR0 = 19,
        UnsupportedTransform = 20,
        InvalidProtectedPolicy = 21,
    }

    public sealed class MicroPatternValidationError :
        IEquatable<MicroPatternValidationError>,
        IComparable<MicroPatternValidationError>
    {
        public MicroPatternValidationError(
            MicroPatternValidationErrorCode code,
            string path,
            string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public MicroPatternValidationErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(MicroPatternValidationError other)
        {
            if (other == null) return -1;
            var comparison = ((int)Code).CompareTo((int)other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(MicroPatternValidationError other)
        {
            return other != null &&
                   Code == other.Code &&
                   string.Equals(Path, other.Path, StringComparison.Ordinal) &&
                   string.Equals(Detail, other.Detail, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MicroPatternValidationError);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Code;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Path);
                return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Detail);
            }
        }

        public override string ToString()
        {
            return Code + "|" + Path + "|" + Detail;
        }
    }

    public sealed class MicroPatternValidationResult
    {
        private readonly ReadOnlyCollection<MicroPatternValidationError> errors;

        internal MicroPatternValidationResult(
            MicroPatternDefinition definition,
            IEnumerable<MicroPatternValidationError> errors,
            string stableDigest)
        {
            var copy = errors
                .Where(value => value != null)
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            this.errors = new ReadOnlyCollection<MicroPatternValidationError>(copy);
            Definition = copy.Length == 0 ? definition : null;
            StableDigest = copy.Length == 0 ? stableDigest ?? string.Empty : string.Empty;
        }

        public bool IsValid => errors.Count == 0 && Definition != null;
        public MicroPatternDefinition Definition { get; }
        public IReadOnlyList<MicroPatternValidationError> Errors => errors;
        public string StableDigest { get; }
    }

    public static class MicroPatternValidator
    {
        public static MicroPatternValidationResult Validate(MicroPatternDefinition definition)
        {
            var errors = new List<MicroPatternValidationError>();
            if (definition == null)
            {
                Add(errors, MicroPatternValidationErrorCode.MissingInput, "definition", "Definition is required.");
                return new MicroPatternValidationResult(null, errors, string.Empty);
            }

            ValidateIdentityAndDimensions(definition, errors);
            ValidateCells(definition, errors);
            ValidateWeightAndBiomes(definition, errors);
            ValidateTransformsAndPolicy(definition, errors);

            if (errors.Count != 0)
            {
                return new MicroPatternValidationResult(null, errors, string.Empty);
            }

            return new MicroPatternValidationResult(
                definition,
                errors,
                MicroPatternCanonicalDigest.Compute(definition));
        }

        private static void ValidateIdentityAndDimensions(
            MicroPatternDefinition definition,
            ICollection<MicroPatternValidationError> errors)
        {
            if (!IsStableId(definition.Id.Value, "MP_"))
            {
                Add(errors, MicroPatternValidationErrorCode.InvalidPatternId, "id", definition.Id.Value);
            }

            if (definition.Width != MicroPatternDefinition.RequiredWidth ||
                definition.Height != MicroPatternDefinition.RequiredHeight)
            {
                Add(
                    errors,
                    MicroPatternValidationErrorCode.InvalidDimensions,
                    "dimensions",
                    Number(definition.Width) + "x" + Number(definition.Height));
            }
        }

        private static void ValidateCells(
            MicroPatternDefinition definition,
            ICollection<MicroPatternValidationError> errors)
        {
            if (definition.Cells.Count != MicroPatternDefinition.RequiredCellCount)
            {
                Add(
                    errors,
                    MicroPatternValidationErrorCode.InvalidCellCount,
                    "cells",
                    Number(definition.Cells.Count));
            }

            var seen = new HashSet<LocalTileCoord>();
            for (var listIndex = 0; listIndex < definition.Cells.Count; listIndex++)
            {
                var cell = definition.Cells[listIndex];
                if (cell == null)
                {
                    Add(
                        errors,
                        MicroPatternValidationErrorCode.MissingInput,
                        "cells[" + Number(listIndex) + "]",
                        "Cell is required.");
                    continue;
                }

                var coordinate = cell.Coordinate;
                var coordinatePath = CoordinatePath(coordinate);
                var inRange = coordinate.X >= 0 && coordinate.X < MicroPatternDefinition.RequiredWidth &&
                              coordinate.Y >= 0 && coordinate.Y < MicroPatternDefinition.RequiredHeight;
                if (!inRange)
                {
                    Add(
                        errors,
                        MicroPatternValidationErrorCode.CellOutOfRange,
                        coordinatePath,
                        "Expected x/y in 0..3.");
                }

                if (!seen.Add(coordinate))
                {
                    Add(
                        errors,
                        MicroPatternValidationErrorCode.DuplicateCell,
                        coordinatePath,
                        "Coordinate occurs more than once.");
                }

                ValidateInstructions(cell, coordinatePath, errors);
            }

            for (var y = 0; y < MicroPatternDefinition.RequiredHeight; y++)
            {
                for (var x = 0; x < MicroPatternDefinition.RequiredWidth; x++)
                {
                    var coordinate = new LocalTileCoord(x, y);
                    if (!seen.Contains(coordinate))
                    {
                        Add(
                            errors,
                            MicroPatternValidationErrorCode.MissingCell,
                            CoordinatePath(coordinate),
                            "Explicit cell is required.");
                    }
                }
            }
        }

        private static void ValidateInstructions(
            MicroPatternCell cell,
            string coordinatePath,
            ICollection<MicroPatternValidationError> errors)
        {
            var seenLayers = new HashSet<MicroPatternLayer>();
            for (var index = 0; index < cell.Instructions.Count; index++)
            {
                var instruction = cell.Instructions[index];
                if (instruction == null)
                {
                    Add(
                        errors,
                        MicroPatternValidationErrorCode.MissingInput,
                        coordinatePath + ".instructions[" + Number(index) + "]",
                        "Instruction is required.");
                    continue;
                }

                var instructionPath = coordinatePath + ".layer[" + Number((int)instruction.Layer) + "]";
                if (!seenLayers.Add(instruction.Layer))
                {
                    Add(
                        errors,
                        MicroPatternValidationErrorCode.DuplicateLayerInstruction,
                        instructionPath,
                        "Only one instruction per layer is allowed.");
                }

                if (!IsDefinedLayer(instruction.Layer) ||
                    !IsDefinedOperation(instruction.Operation) ||
                    !IsAllowed(instruction.Layer, instruction.Operation))
                {
                    Add(
                        errors,
                        MicroPatternValidationErrorCode.InvalidLayerOperation,
                        instructionPath,
                        Number((int)instruction.Operation));
                }

                if (DoesNotUsePayload(instruction.Operation))
                {
                    if (instruction.PayloadId.Length != 0)
                    {
                        Add(
                            errors,
                            MicroPatternValidationErrorCode.UnexpectedPayload,
                            instructionPath,
                            instruction.PayloadId);
                    }
                }
                else if (IsSetOperation(instruction.Operation))
                {
                    if (instruction.PayloadId.Length == 0)
                    {
                        Add(
                            errors,
                            MicroPatternValidationErrorCode.MissingPayload,
                            instructionPath,
                            "Set operation requires a payload ID.");
                    }
                    else if (!IsStableId(instruction.PayloadId, string.Empty))
                    {
                        Add(
                            errors,
                            MicroPatternValidationErrorCode.InvalidPayloadId,
                            instructionPath,
                            instruction.PayloadId);
                    }
                }
            }
        }

        private static void ValidateWeightAndBiomes(
            MicroPatternDefinition definition,
            ICollection<MicroPatternValidationError> errors)
        {
            if (definition.Weight < MicroPatternDefinition.MinimumWeight ||
                definition.Weight > MicroPatternDefinition.MaximumWeight)
            {
                Add(
                    errors,
                    MicroPatternValidationErrorCode.InvalidWeight,
                    "weight",
                    Number(definition.Weight));
            }

            if (definition.AllowedBiomes.Count == 0)
            {
                Add(
                    errors,
                    MicroPatternValidationErrorCode.MissingBiome,
                    "allowedBiomes",
                    "At least one biome is required.");
            }

            var seen = new HashSet<MoonpalaceBiomeId>();
            for (var index = 0; index < definition.AllowedBiomes.Count; index++)
            {
                var biome = definition.AllowedBiomes[index];
                var path = "allowedBiomes[" + Number(index) + "]";
                if (!biome.IsDefined)
                {
                    Add(
                        errors,
                        MicroPatternValidationErrorCode.UnknownBiome,
                        path,
                        "Undefined MoonpalaceBiomeId.");
                }

                if (!seen.Add(biome))
                {
                    Add(
                        errors,
                        MicroPatternValidationErrorCode.DuplicateBiome,
                        path,
                        biome.IsDefined ? biome.CanonicalId : "UNDEFINED");
                }
            }
        }

        private static void ValidateTransformsAndPolicy(
            MicroPatternDefinition definition,
            ICollection<MicroPatternValidationError> errors)
        {
            if (definition.AllowedTransforms.Count == 0)
            {
                Add(
                    errors,
                    MicroPatternValidationErrorCode.MissingTransform,
                    "allowedTransforms",
                    "At least one transform is required.");
            }

            var seen = new HashSet<MicroPatternTransform>();
            var hasR0 = false;
            for (var index = 0; index < definition.AllowedTransforms.Count; index++)
            {
                var transform = definition.AllowedTransforms[index];
                var path = "allowedTransforms[" + Number(index) + "]";
                if (!IsDefinedTransform(transform))
                {
                    Add(
                        errors,
                        MicroPatternValidationErrorCode.UnsupportedTransform,
                        path,
                        Number((int)transform));
                }

                if (!seen.Add(transform))
                {
                    Add(
                        errors,
                        MicroPatternValidationErrorCode.DuplicateTransform,
                        path,
                        Number((int)transform));
                }

                hasR0 |= transform == MicroPatternTransform.R0;
            }

            if (!hasR0)
            {
                Add(
                    errors,
                    MicroPatternValidationErrorCode.MissingR0,
                    "allowedTransforms",
                    "R0 is mandatory.");
            }

            if (definition.ProtectedPolicy != MicroPatternProtectedPolicy.ForceNoChange &&
                definition.ProtectedPolicy != MicroPatternProtectedPolicy.RejectCandidate)
            {
                Add(
                    errors,
                    MicroPatternValidationErrorCode.InvalidProtectedPolicy,
                    "protectedPolicy",
                    Number((int)definition.ProtectedPolicy));
            }
        }

        private static bool IsAllowed(MicroPatternLayer layer, MicroPatternOperation operation)
        {
            if (operation == MicroPatternOperation.NoChange) return IsDefinedLayer(layer);
            switch (layer)
            {
                case MicroPatternLayer.Geometry:
                    return operation == MicroPatternOperation.AddSolid ||
                           operation == MicroPatternOperation.CarveAir;
                case MicroPatternLayer.Surface:
                    return operation == MicroPatternOperation.SetSurface;
                case MicroPatternLayer.Affordance:
                    return operation == MicroPatternOperation.SetAffordance;
                case MicroPatternLayer.Material:
                    return operation == MicroPatternOperation.SetMaterial;
                case MicroPatternLayer.Hazard:
                    return operation == MicroPatternOperation.SetHazard;
                case MicroPatternLayer.Marker:
                    return operation == MicroPatternOperation.SetMarker;
                default:
                    return false;
            }
        }

        private static bool IsDefinedLayer(MicroPatternLayer layer)
        {
            return layer >= MicroPatternLayer.Geometry && layer <= MicroPatternLayer.Marker;
        }

        private static bool IsDefinedOperation(MicroPatternOperation operation)
        {
            return operation >= MicroPatternOperation.NoChange &&
                   operation <= MicroPatternOperation.SetMarker;
        }

        private static bool IsDefinedTransform(MicroPatternTransform transform)
        {
            return transform >= MicroPatternTransform.R0 && transform <= MicroPatternTransform.R180;
        }

        private static bool DoesNotUsePayload(MicroPatternOperation operation)
        {
            return operation == MicroPatternOperation.NoChange ||
                   operation == MicroPatternOperation.AddSolid ||
                   operation == MicroPatternOperation.CarveAir;
        }

        private static bool IsSetOperation(MicroPatternOperation operation)
        {
            return operation >= MicroPatternOperation.SetSurface &&
                   operation <= MicroPatternOperation.SetMarker;
        }

        private static bool IsStableId(string value, string requiredPrefix)
        {
            if (string.IsNullOrEmpty(value) ||
                (requiredPrefix.Length != 0 &&
                 (!value.StartsWith(requiredPrefix, StringComparison.Ordinal) ||
                  value.Length <= requiredPrefix.Length)))
            {
                return false;
            }

            if (value[0] < 'A' || value[0] > 'Z') return false;
            for (var index = 1; index < value.Length; index++)
            {
                var character = value[index];
                if ((character < 'A' || character > 'Z') &&
                    (character < '0' || character > '9') &&
                    character != '_')
                {
                    return false;
                }
            }

            return true;
        }

        private static string CoordinatePath(LocalTileCoord coordinate)
        {
            return "cells[" + Number(coordinate.X) + "," + Number(coordinate.Y) + "]";
        }

        private static string Number(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static void Add(
            ICollection<MicroPatternValidationError> errors,
            MicroPatternValidationErrorCode code,
            string path,
            string detail)
        {
            errors.Add(new MicroPatternValidationError(code, path, detail));
        }
    }
}
