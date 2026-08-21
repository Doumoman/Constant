using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class StaticDataRegistry
    {
        private readonly IReadOnlyDictionary<RecordKey, ForeignKeyRecordIdentity> recordsByIdentity;
        private readonly IReadOnlyDictionary<ForeignKeyRecordIdentity, object> typedDefinitions;

        internal StaticDataRegistry(
            StaticDataRegistryInput input,
            StaticDataReverseIndex reverseIndex,
            IDictionary<ForeignKeyRecordIdentity, object> sourceTypedDefinitions)
        {
            WorldRouteDefinitions = input.WorldRouteDefinitions;
            BiomeBoundaryDefinitions = input.BiomeBoundaryDefinitions;
            SpecialVillageDefinitions = input.SpecialVillageDefinitions;
            MicrochunkPopulationItemDefinitions = input.MicrochunkPopulationItemDefinitions;
            ForeignKeyResolution = input.ForeignKeyResolution;
            RecordIndex = ForeignKeyResolution.RecordIndex;
            Records = RecordIndex.Records;
            References = ForeignKeyResolution.References;
            ReverseIndex = reverseIndex;

            var records = new Dictionary<RecordKey, ForeignKeyRecordIdentity>();
            foreach (var identity in Records)
            {
                records.Add(new RecordKey(identity.FileName, identity.RecordNumber), identity);
            }

            recordsByIdentity = new ReadOnlyDictionary<RecordKey, ForeignKeyRecordIdentity>(records);
            typedDefinitions = new ReadOnlyDictionary<ForeignKeyRecordIdentity, object>(
                new Dictionary<ForeignKeyRecordIdentity, object>(
                    sourceTypedDefinitions,
                    ReferenceComparer<ForeignKeyRecordIdentity>.Instance));
        }

        public WorldRouteDefinitionSet WorldRouteDefinitions { get; }
        public BiomeBoundaryDefinitionSet BiomeBoundaryDefinitions { get; }
        public SpecialVillageDefinitionSet SpecialVillageDefinitions { get; }
        public MicrochunkPopulationItemDefinitionSet MicrochunkPopulationItemDefinitions { get; }
        public ForeignKeyResolutionResult ForeignKeyResolution { get; }
        public ForeignKeyRecordIndex RecordIndex { get; }
        public IReadOnlyList<ForeignKeyRecordIdentity> Records { get; }
        public IReadOnlyList<ResolvedForeignKeyReference> References { get; }
        public StaticDataReverseIndex ReverseIndex { get; }
        public IReadOnlyDictionary<ForeignKeyRecordIdentity, object> TypedDefinitions => typedDefinitions;

        public bool TryGetRecord(
            string fileName,
            int recordNumber,
            out ForeignKeyRecordIdentity identity)
        {
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));
            return recordsByIdentity.TryGetValue(new RecordKey(fileName, recordNumber), out identity);
        }

        public bool TryGetReferencedPrimaryKey(
            string fileName,
            string columnName,
            string value,
            out ForeignKeyRecordIdentity identity)
        {
            return RecordIndex.TryGet(fileName, columnName, value, out identity);
        }

        public bool TryGetTypedDefinition(
            ForeignKeyRecordIdentity identity,
            out object definition)
        {
            if (identity == null)
            {
                definition = null;
                return false;
            }

            return typedDefinitions.TryGetValue(identity, out definition);
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

            public override bool Equals(object obj)
            {
                return obj is RecordKey other && Equals(other);
            }

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
