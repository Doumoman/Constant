using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class StaticDataReverseIndex
    {
        private static readonly IReadOnlyList<ResolvedForeignKeyReference> Empty =
            new ReadOnlyCollection<ResolvedForeignKeyReference>(
                Array.Empty<ResolvedForeignKeyReference>());

        private readonly IReadOnlyDictionary<ForeignKeyRecordIdentity, IReadOnlyList<ResolvedForeignKeyReference>> incomingByTarget;
        private readonly IReadOnlyDictionary<ForeignKeyRecordIdentity, IReadOnlyList<ResolvedForeignKeyReference>> outgoingBySource;
        private readonly IReadOnlyDictionary<TargetKey, IReadOnlyList<ResolvedForeignKeyReference>> incomingByTargetValue;

        internal StaticDataReverseIndex(IEnumerable<ResolvedForeignKeyReference> sourceReferences)
        {
            var incoming = new Dictionary<ForeignKeyRecordIdentity, List<ResolvedForeignKeyReference>>(
                ReferenceComparer<ForeignKeyRecordIdentity>.Instance);
            var outgoing = new Dictionary<ForeignKeyRecordIdentity, List<ResolvedForeignKeyReference>>(
                ReferenceComparer<ForeignKeyRecordIdentity>.Instance);
            var byValue = new Dictionary<TargetKey, List<ResolvedForeignKeyReference>>();

            foreach (var reference in sourceReferences ??
                     throw new ArgumentNullException(nameof(sourceReferences)))
            {
                Add(incoming, reference.TargetIdentity, reference);
                Add(outgoing, reference.SourceIdentity, reference);
                Add(byValue, new TargetKey(
                    reference.TargetFileName,
                    reference.TargetColumnName,
                    reference.TargetValue), reference);
            }

            incomingByTarget = Freeze(incoming, ReferenceComparer<ForeignKeyRecordIdentity>.Instance);
            outgoingBySource = Freeze(outgoing, ReferenceComparer<ForeignKeyRecordIdentity>.Instance);
            incomingByTargetValue = Freeze(byValue, EqualityComparer<TargetKey>.Default);
        }

        public IReadOnlyList<ResolvedForeignKeyReference> GetIncoming(
            ForeignKeyRecordIdentity targetIdentity)
        {
            if (targetIdentity == null) return Empty;
            return incomingByTarget.TryGetValue(targetIdentity, out var references)
                ? references
                : Empty;
        }

        public IReadOnlyList<ResolvedForeignKeyReference> GetOutgoing(
            ForeignKeyRecordIdentity sourceIdentity)
        {
            if (sourceIdentity == null) return Empty;
            return outgoingBySource.TryGetValue(sourceIdentity, out var references)
                ? references
                : Empty;
        }

        public IReadOnlyList<ResolvedForeignKeyReference> GetIncoming(
            string targetFileName,
            string targetColumnName,
            string targetValue)
        {
            if (targetFileName == null || targetColumnName == null || targetValue == null)
            {
                return Empty;
            }

            return incomingByTargetValue.TryGetValue(
                    new TargetKey(targetFileName, targetColumnName, targetValue),
                    out var references)
                ? references
                : Empty;
        }

        private static void Add<TKey>(
            IDictionary<TKey, List<ResolvedForeignKeyReference>> dictionary,
            TKey key,
            ResolvedForeignKeyReference reference)
        {
            if (!dictionary.TryGetValue(key, out var values))
            {
                values = new List<ResolvedForeignKeyReference>();
                dictionary.Add(key, values);
            }

            values.Add(reference);
        }

        private static IReadOnlyDictionary<TKey, IReadOnlyList<ResolvedForeignKeyReference>> Freeze<TKey>(
            IDictionary<TKey, List<ResolvedForeignKeyReference>> source,
            IEqualityComparer<TKey> comparer)
        {
            var result = new Dictionary<TKey, IReadOnlyList<ResolvedForeignKeyReference>>(comparer);
            foreach (var pair in source)
            {
                result.Add(pair.Key, new ReadOnlyCollection<ResolvedForeignKeyReference>(pair.Value));
            }

            return new ReadOnlyDictionary<TKey, IReadOnlyList<ResolvedForeignKeyReference>>(result);
        }

        private readonly struct TargetKey : IEquatable<TargetKey>
        {
            private readonly string fileName;
            private readonly string columnName;
            private readonly string value;

            public TargetKey(string fileName, string columnName, string value)
            {
                this.fileName = fileName;
                this.columnName = columnName;
                this.value = value;
            }

            public bool Equals(TargetKey other)
            {
                return string.Equals(fileName, other.fileName, StringComparison.Ordinal) &&
                       string.Equals(columnName, other.columnName, StringComparison.Ordinal) &&
                       string.Equals(value, other.value, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is TargetKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = StringComparer.Ordinal.GetHashCode(fileName);
                    hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(columnName);
                    return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(value);
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
