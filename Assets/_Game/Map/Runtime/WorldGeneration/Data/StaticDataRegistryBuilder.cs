using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class StaticDataRegistryBuilder
    {
        public StaticDataRegistryBuildResult Build(StaticDataRegistryInput input)
        {
            var errors = new List<StaticDataRegistryBuildError>();
            if (input == null)
            {
                AddMissingSetErrors(null, errors);
                errors.Add(Error(
                    StaticDataRegistryBuildErrorCode.UnsuccessfulForeignKeyResolution,
                    "Static data registry input is missing.",
                    definitionType: nameof(ForeignKeyResolutionResult)));
                return Failure(errors);
            }

            AddMissingSetErrors(input, errors);
            var resolution = input.ForeignKeyResolution;
            if (resolution == null || !resolution.Success || resolution.RecordIndex == null ||
                resolution.Errors.Count != 0)
            {
                errors.Add(Error(
                    StaticDataRegistryBuildErrorCode.UnsuccessfulForeignKeyResolution,
                    "Foreign-key resolution must be successful, publish an index, and contain zero errors.",
                    definitionType: nameof(ForeignKeyResolutionResult)));
            }

            var typedDefinitions = new List<TypedDefinition>();
            CollectDefinitions(input, typedDefinitions);

            Dictionary<ForeignKeyRecordIdentity, object> definitionsByIdentity = null;
            if (resolution != null && resolution.Success && resolution.RecordIndex != null &&
                resolution.Errors.Count == 0)
            {
                var indexState = ValidateIndex(resolution.RecordIndex, errors);
                definitionsByIdentity = ValidateTypedDefinitions(
                    typedDefinitions,
                    indexState.IdentitiesByRecord,
                    errors);
                ValidateForeignKeyGraph(resolution, indexState.IdentitySet, errors);
            }

            errors.Sort(StaticDataRegistryBuildError.Compare);
            if (errors.Count > 0)
            {
                return new StaticDataRegistryBuildResult(null, errors);
            }

            var reverseIndex = new StaticDataReverseIndex(resolution.References);
            var registry = new StaticDataRegistry(input, reverseIndex, definitionsByIdentity);
            return new StaticDataRegistryBuildResult(registry, errors);
        }

        private static void AddMissingSetErrors(
            StaticDataRegistryInput input,
            ICollection<StaticDataRegistryBuildError> errors)
        {
            AddMissing(input?.WorldRouteDefinitions, nameof(WorldRouteDefinitionSet), errors);
            AddMissing(input?.BiomeBoundaryDefinitions, nameof(BiomeBoundaryDefinitionSet), errors);
            AddMissing(input?.SpecialVillageDefinitions, nameof(SpecialVillageDefinitionSet), errors);
            AddMissing(input?.MicrochunkPopulationItemDefinitions,
                nameof(MicrochunkPopulationItemDefinitionSet), errors);
        }

        private static void AddMissing(
            object value,
            string definitionType,
            ICollection<StaticDataRegistryBuildError> errors)
        {
            if (value != null) return;
            errors.Add(Error(
                StaticDataRegistryBuildErrorCode.MissingDefinitionSet,
                "Required definition set is missing.",
                definitionType: definitionType));
        }

        private static IndexState ValidateIndex(
            ForeignKeyRecordIndex index,
            ICollection<StaticDataRegistryBuildError> errors)
        {
            var identitiesByRecord = new Dictionary<CsvParsedRecord, ForeignKeyRecordIdentity>(
                ReferenceComparer<CsvParsedRecord>.Instance);
            var identitySet = new HashSet<ForeignKeyRecordIdentity>(
                ReferenceComparer<ForeignKeyRecordIdentity>.Instance);
            var recordKeys = new HashSet<RecordKey>();
            ForeignKeyRecordIdentity previous = null;

            foreach (var identity in index.Records)
            {
                if (identity == null || identity.SourceRecord == null)
                {
                    errors.Add(Error(
                        StaticDataRegistryBuildErrorCode.ForeignKeyGraphMismatch,
                        "Foreign-key index contains a null record identity."));
                    continue;
                }

                var fileName = FileName(identity.SourceRecord);
                var valid = identitySet.Add(identity) &&
                            identitiesByRecord.TryAdd(identity.SourceRecord, identity) &&
                            recordKeys.Add(new RecordKey(identity.FileName, identity.RecordNumber)) &&
                            identity.RecordNumber == identity.SourceRecord.RecordNumber &&
                            string.Equals(identity.FileName, fileName, StringComparison.Ordinal) &&
                            (previous == null || CompareIdentities(previous, identity) < 0);
                if (!valid)
                {
                    errors.Add(Error(
                        StaticDataRegistryBuildErrorCode.ForeignKeyGraphMismatch,
                        "Foreign-key index has an unexpected, duplicate, or unstable file-record identity.",
                        identity.FileName,
                        identity.RecordNumber,
                        sourceLocation: SourceLocation(identity.SourceRecord)));
                }

                previous = identity;
            }

            return new IndexState(identitiesByRecord, identitySet);
        }

        private static Dictionary<ForeignKeyRecordIdentity, object> ValidateTypedDefinitions(
            IEnumerable<TypedDefinition> definitions,
            IReadOnlyDictionary<CsvParsedRecord, ForeignKeyRecordIdentity> identitiesByRecord,
            ICollection<StaticDataRegistryBuildError> errors)
        {
            var result = new Dictionary<ForeignKeyRecordIdentity, object>(
                ReferenceComparer<ForeignKeyRecordIdentity>.Instance);
            foreach (var entry in definitions)
            {
                if (entry.SourceRecord == null ||
                    !identitiesByRecord.TryGetValue(entry.SourceRecord, out var identity))
                {
                    errors.Add(Error(
                        StaticDataRegistryBuildErrorCode.DefinitionRecordMissingFromIndex,
                        "Typed definition source record does not belong to the foreign-key index.",
                        FileName(entry.SourceRecord),
                        entry.SourceRecord?.RecordNumber,
                        entry.DefinitionType,
                        SourceLocation(entry.SourceRecord)));
                    continue;
                }

                if (!result.TryAdd(identity, entry.Definition))
                {
                    errors.Add(Error(
                        StaticDataRegistryBuildErrorCode.DuplicateTypedDefinitionIdentity,
                        "More than one typed definition claims the same source identity.",
                        identity.FileName,
                        identity.RecordNumber,
                        entry.DefinitionType,
                        SourceLocation(entry.SourceRecord)));
                }
            }

            return result;
        }

        private static void ValidateForeignKeyGraph(
            ForeignKeyResolutionResult resolution,
            ISet<ForeignKeyRecordIdentity> identitySet,
            ICollection<StaticDataRegistryBuildError> errors)
        {
            ResolvedForeignKeyReference previous = null;
            foreach (var reference in resolution.References)
            {
                if (reference == null)
                {
                    errors.Add(Error(
                        StaticDataRegistryBuildErrorCode.ForeignKeyGraphMismatch,
                        "Foreign-key reference cannot be null."));
                    continue;
                }

                var sourceExists = identitySet.Contains(reference.SourceIdentity);
                var targetExists = identitySet.Contains(reference.TargetIdentity);
                var fieldExists = sourceExists && reference.SourceField != null &&
                                  reference.SourceIdentity.SourceRecord.Fields.Any(
                                      field => ReferenceEquals(field, reference.SourceField));
                var declaration = reference.SourceField?.Schema?.ForeignKey;
                var declarationMatches = declaration != null &&
                    string.Equals(declaration.TargetFileName, reference.TargetFileName, StringComparison.Ordinal) &&
                    string.Equals(declaration.TargetColumnName, reference.TargetColumnName, StringComparison.Ordinal);
                var sourceMatches = sourceExists &&
                    string.Equals(reference.SourceFileName, reference.SourceIdentity.FileName, StringComparison.Ordinal) &&
                    reference.SourceRecordNumber == reference.SourceIdentity.RecordNumber;
                var targetMatches = targetExists &&
                    string.Equals(reference.TargetFileName, reference.TargetIdentity.FileName, StringComparison.Ordinal) &&
                    resolution.RecordIndex.TryGet(
                        reference.TargetFileName,
                        reference.TargetColumnName,
                        reference.TargetValue,
                        out var lookupIdentity) &&
                    ReferenceEquals(lookupIdentity, reference.TargetIdentity);
                var valueMatches = ValueMatches(reference);
                var orderMatches = previous == null || CompareReferences(previous, reference) <= 0;

                if (!fieldExists || !declarationMatches || !sourceMatches ||
                    !targetMatches || !valueMatches || !orderMatches)
                {
                    errors.Add(Error(
                        StaticDataRegistryBuildErrorCode.ForeignKeyGraphMismatch,
                        "Resolved foreign-key graph is internally inconsistent or not ordinal-stable.",
                        reference.SourceFileName,
                        reference.SourceRecordNumber,
                        sourceLocation: fieldExists ? reference.SourceLocation : (CsvSourceLocation?)null));
                }

                previous = reference;
            }
        }

        private static bool ValueMatches(ResolvedForeignKeyReference reference)
        {
            var field = reference.SourceField;
            if (field == null || field.Value == null || field.Schema == null) return false;
            if (field.Schema.DataType == CsvSchemaDataType.Id)
            {
                return !reference.ListIndex.HasValue &&
                       string.Equals(field.Value.IdValue, reference.RawValue, StringComparison.Ordinal) &&
                       string.Equals(reference.RawValue, reference.TargetValue, StringComparison.Ordinal);
            }

            if (field.Schema.DataType != CsvSchemaDataType.IdList || !reference.ListIndex.HasValue)
            {
                return false;
            }

            var index = reference.ListIndex.Value;
            return index >= 0 && index < field.Value.IdListValue.Count &&
                   string.Equals(field.Value.IdListValue[index], reference.RawValue, StringComparison.Ordinal) &&
                   string.Equals(reference.RawValue, reference.TargetValue, StringComparison.Ordinal);
        }

        private static void CollectDefinitions(
            StaticDataRegistryInput input,
            ICollection<TypedDefinition> result)
        {
            var world = input.WorldRouteDefinitions;
            if (world != null)
            {
                Add(result, world.WorldProfiles.Values, item => item.SourceRecord);
                Add(result, world.GenerationProfiles.Values, item => item.SourceRecord);
                Add(result, world.GenerationPasses, item => item.SourceRecord);
                Add(result, world.RngStreams.Values, item => item.SourceRecord);
                Add(result, world.RouteMasks.Values, item => item.SourceRecord);
                Add(result, world.SocketBands.Values, item => item.SourceRecord);
                Add(result, world.EdgeSignatures.Values, item => item.SourceRecord);
                Add(result, world.EdgeSignatureCompatibilities, item => item.SourceRecord);
                Add(result, world.SectorRecipes.Values, item => item.SourceRecord);
                Add(result, world.SectorRecipeCells, item => item.SourceRecord);
                Add(result, world.SectorRecipePaths, item => item.SourceRecord);
                Add(result, world.SectorExternalSockets, item => item.SourceRecord);
                Add(result, world.SectorRecipePoolEntries, item => item.SourceRecord);
            }

            var biome = input.BiomeBoundaryDefinitions;
            if (biome != null)
            {
                Add(result, biome.BiomeTypes.Values, item => item.SourceRecord);
                Add(result, biome.BiomePatchRules.Values, item => item.SourceRecord);
                Add(result, biome.BoundaryProfiles.Values, item => item.SourceRecord);
                Add(result, biome.BoundaryPairRules.Values, item => item.SourceRecord);
                Add(result, biome.BoundaryChunks.Values, item => item.SourceRecord);
            }

            var special = input.SpecialVillageDefinitions;
            if (special != null)
            {
                Add(result, special.EventActivationRoutes.Values, item => item.SourceRecord);
                Add(result, special.SpecialMaps.Values, item => item.SourceRecord);
                Add(result, special.SpecialMapEntrySockets, item => item.SourceRecord);
                Add(result, special.SpecialMapFootprintCells, item => item.SourceRecord);
                Add(result, special.SpecialMapRewards, item => item.SourceRecord);
                Add(result, special.ShopArchetypes.Values, item => item.SourceRecord);
                Add(result, special.ShopInventoryRules, item => item.SourceRecord);
                Add(result, special.ShopkeeperSpecies.Values, item => item.SourceRecord);
                Add(result, special.VillageFacilities.Values, item => item.SourceRecord);
                Add(result, special.VillageLayouts.Values, item => item.SourceRecord);
                Add(result, special.VillageLayoutCells, item => item.SourceRecord);
                Add(result, special.VillageProfiles.Values, item => item.SourceRecord);
            }

            var micro = input.MicrochunkPopulationItemDefinitions;
            if (micro != null)
            {
                Add(result, micro.BatteryProfiles.Values, item => item.SourceRecord);
                Add(result, micro.MapElements.Values, item => item.SourceRecord);
                Add(result, micro.MapElementInteractions, item => item.SourceRecord);
                Add(result, micro.Microchunks.Values, item => item.SourceRecord);
                Add(result, micro.MicrochunkObjectSlots, item => item.SourceRecord);
                Add(result, micro.MicrochunkPoolEntries, item => item.SourceRecord);
                Add(result, micro.MicrochunkSockets, item => item.SourceRecord);
                Add(result, micro.MicrochunkTileCells, item => item.SourceRecord);
                Add(result, micro.MicrochunkVariantRules.Values, item => item.SourceRecord);
                Add(result, micro.PopulationProfiles.Values, item => item.SourceRecord);
                Add(result, micro.Prefabs.Values, item => item.SourceRecord);
                Add(result, micro.Resources.Values, item => item.SourceRecord);
                Add(result, micro.ResourceSpawnRules.Values, item => item.SourceRecord);
                Add(result, micro.SpawnPoolEntries, item => item.SourceRecord);
                Add(result, micro.SpecialItemSlots.Values, item => item.SourceRecord);
                Add(result, micro.TileCodes.Values, item => item.SourceRecord);
                Add(result, micro.ToolUpgrades, item => item.SourceRecord);
            }
        }

        private static void Add<T>(
            ICollection<TypedDefinition> result,
            IEnumerable<T> definitions,
            Func<T, CsvParsedRecord> sourceRecord)
        {
            foreach (var definition in definitions)
            {
                result.Add(new TypedDefinition(
                    definition,
                    typeof(T).Name,
                    sourceRecord(definition)));
            }
        }

        private static string FileName(CsvParsedRecord record)
        {
            if (record == null || record.Fields.Count == 0) return string.Empty;
            var fileName = record.Fields[0]?.Schema?.FileName ?? string.Empty;
            return record.Fields.All(field => field?.Schema != null &&
                string.Equals(field.Schema.FileName, fileName, StringComparison.Ordinal))
                ? fileName
                : string.Empty;
        }

        private static CsvSourceLocation? SourceLocation(CsvParsedRecord record)
        {
            return record?.SourceRecord?.StartLocation;
        }

        private static int CompareIdentities(
            ForeignKeyRecordIdentity left,
            ForeignKeyRecordIdentity right)
        {
            var comparison = StringComparer.Ordinal.Compare(left.FileName, right.FileName);
            return comparison != 0 ? comparison : left.RecordNumber.CompareTo(right.RecordNumber);
        }

        private static int CompareReferences(
            ResolvedForeignKeyReference left,
            ResolvedForeignKeyReference right)
        {
            var comparison = StringComparer.Ordinal.Compare(left.SourceFileName, right.SourceFileName);
            if (comparison != 0) return comparison;
            comparison = left.SourceRecordNumber.CompareTo(right.SourceRecordNumber);
            if (comparison != 0) return comparison;
            comparison = left.SourceColumnOrder.CompareTo(right.SourceColumnOrder);
            if (comparison != 0) return comparison;
            comparison = Nullable.Compare(left.ListIndex, right.ListIndex);
            return comparison != 0
                ? comparison
                : StringComparer.Ordinal.Compare(left.TargetValue, right.TargetValue);
        }

        private static StaticDataRegistryBuildError Error(
            StaticDataRegistryBuildErrorCode code,
            string message,
            string fileName = null,
            int? recordNumber = null,
            string definitionType = null,
            CsvSourceLocation? sourceLocation = null)
        {
            return new StaticDataRegistryBuildError(
                code, message, fileName, recordNumber, definitionType, sourceLocation);
        }

        private static StaticDataRegistryBuildResult Failure(
            List<StaticDataRegistryBuildError> errors)
        {
            errors.Sort(StaticDataRegistryBuildError.Compare);
            return new StaticDataRegistryBuildResult(null, errors);
        }

        private sealed class TypedDefinition
        {
            public TypedDefinition(object definition, string definitionType, CsvParsedRecord sourceRecord)
            {
                Definition = definition;
                DefinitionType = definitionType;
                SourceRecord = sourceRecord;
            }

            public object Definition { get; }
            public string DefinitionType { get; }
            public CsvParsedRecord SourceRecord { get; }
        }

        private sealed class IndexState
        {
            public IndexState(
                Dictionary<CsvParsedRecord, ForeignKeyRecordIdentity> identitiesByRecord,
                HashSet<ForeignKeyRecordIdentity> identitySet)
            {
                IdentitiesByRecord = identitiesByRecord;
                IdentitySet = identitySet;
            }

            public Dictionary<CsvParsedRecord, ForeignKeyRecordIdentity> IdentitiesByRecord { get; }
            public HashSet<ForeignKeyRecordIdentity> IdentitySet { get; }
        }

        private readonly struct RecordKey : IEquatable<RecordKey>
        {
            private readonly string fileName;
            private readonly int recordNumber;

            public RecordKey(string fileName, int recordNumber)
            {
                this.fileName = fileName;
                this.recordNumber = recordNumber;
            }

            public bool Equals(RecordKey other)
            {
                return recordNumber == other.recordNumber &&
                       string.Equals(fileName, other.fileName, StringComparison.Ordinal);
            }

            public override bool Equals(object obj) => obj is RecordKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return (StringComparer.Ordinal.GetHashCode(fileName) * 397) ^ recordNumber;
                }
            }
        }

        private sealed class ReferenceComparer<T> : IEqualityComparer<T>
            where T : class
        {
            public static readonly ReferenceComparer<T> Instance = new ReferenceComparer<T>();
            public bool Equals(T left, T right) => ReferenceEquals(left, right);
            public int GetHashCode(T value) => RuntimeHelpers.GetHashCode(value);
        }
    }
}
