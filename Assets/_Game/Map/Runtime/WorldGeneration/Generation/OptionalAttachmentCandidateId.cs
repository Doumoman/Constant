using System;
using System.Globalization;

namespace StarNight.Map.WorldGeneration.Generation
{
    public readonly struct OptionalAttachmentCandidateId : IEquatable<OptionalAttachmentCandidateId>, IComparable<OptionalAttachmentCandidateId>
    {
        private const string Prefix = "OPT_ATTACH_";
        private readonly string value;

        public OptionalAttachmentCandidateId(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (!TryParseOrdinal(value, out _))
            {
                throw new ArgumentException("Candidate IDs must match ^OPT_ATTACH_[0-9]{4}$.", nameof(value));
            }

            this.value = value;
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => TryParseOrdinal(value, out _);

        public static bool TryCreate(string value, out OptionalAttachmentCandidateId result)
        {
            if (!TryParseOrdinal(value, out _))
            {
                result = default(OptionalAttachmentCandidateId);
                return false;
            }

            result = new OptionalAttachmentCandidateId(value);
            return true;
        }

        public static OptionalAttachmentCandidateId FromOrdinal(int ordinal)
        {
            if (ordinal < 0 || ordinal > 9999)
            {
                throw new ArgumentOutOfRangeException(nameof(ordinal));
            }

            return new OptionalAttachmentCandidateId(
                Prefix + ordinal.ToString("D4", CultureInfo.InvariantCulture));
        }

        public bool TryGetOrdinal(out int ordinal)
        {
            return TryParseOrdinal(value, out ordinal);
        }

        public int CompareTo(OptionalAttachmentCandidateId other)
        {
            return string.Compare(Value, other.Value, StringComparison.Ordinal);
        }

        public bool Equals(OptionalAttachmentCandidateId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is OptionalAttachmentCandidateId other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)2166136261;
                var text = Value;
                for (var index = 0; index < text.Length; index++)
                {
                    hash = (hash ^ text[index]) * 16777619;
                }

                return hash;
            }
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(OptionalAttachmentCandidateId left, OptionalAttachmentCandidateId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(OptionalAttachmentCandidateId left, OptionalAttachmentCandidateId right)
        {
            return !left.Equals(right);
        }

        private static bool TryParseOrdinal(string candidate, out int ordinal)
        {
            ordinal = 0;
            if (candidate == null || candidate.Length != Prefix.Length + 4 ||
                !candidate.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return false;
            }

            for (var index = Prefix.Length; index < candidate.Length; index++)
            {
                var character = candidate[index];
                if (character < '0' || character > '9')
                {
                    ordinal = 0;
                    return false;
                }

                ordinal = (ordinal * 10) + character - '0';
            }

            return true;
        }
    }
}
