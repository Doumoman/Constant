using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvPrimaryKey : IEquatable<CsvPrimaryKey>, IComparable<CsvPrimaryKey>
    {
        private readonly ReadOnlyCollection<string> components;

        public CsvPrimaryKey(IEnumerable<string> components)
        {
            if (components == null)
            {
                throw new ArgumentNullException(nameof(components));
            }

            var copiedComponents = new List<string>();
            foreach (var component in components)
            {
                if (component == null)
                {
                    throw new ArgumentException(
                        "A CSV primary-key component cannot be null.",
                        nameof(components));
                }

                copiedComponents.Add(component);
            }

            if (copiedComponents.Count == 0)
            {
                throw new ArgumentException(
                    "A CSV primary key must contain at least one component.",
                    nameof(components));
            }

            this.components = new ReadOnlyCollection<string>(copiedComponents);
        }

        public IReadOnlyList<string> Components => components;

        public bool Equals(CsvPrimaryKey other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (components.Count != other.components.Count)
            {
                return false;
            }

            for (var index = 0; index < components.Count; index++)
            {
                if (!string.Equals(
                        components[index],
                        other.components[index],
                        StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is CsvPrimaryKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = 17;
                foreach (var component in components)
                {
                    hashCode = (hashCode * 31) ^ StringComparer.Ordinal.GetHashCode(component);
                }

                return hashCode;
            }
        }

        public int CompareTo(CsvPrimaryKey other)
        {
            if (ReferenceEquals(other, null))
            {
                return 1;
            }

            var sharedCount = Math.Min(components.Count, other.components.Count);
            for (var index = 0; index < sharedCount; index++)
            {
                var comparison = StringComparer.Ordinal.Compare(
                    components[index],
                    other.components[index]);
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return components.Count.CompareTo(other.components.Count);
        }
    }
}
