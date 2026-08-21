using System;
using System.Globalization;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum OptionalRegionAccessRule
    {
        Basic,
        Tool,
        Environment,
        Explosive,
        Hidden
    }

    public enum OptionalRewardTier
    {
        None,
        Low,
        Medium,
        High,
        Unique
    }

    public enum OptionalReturnPolicy
    {
        BacktrackToAttachment,
        ReturnGateToMandatory,
        SafeExitToMandatory
    }

    public readonly struct OptionalRegionDepth : IEquatable<OptionalRegionDepth>, IComparable<OptionalRegionDepth>
    {
        public OptionalRegionDepth(int value)
        {
            if (value < 1 || value > 4)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Optional region depth must be in the range 1..4.");
            }

            Value = value;
        }

        public int Value { get; }
        public bool IsValid => Value >= 1 && Value <= 4;

        public static bool TryCreate(int value, out OptionalRegionDepth result)
        {
            if (value < 1 || value > 4)
            {
                result = default(OptionalRegionDepth);
                return false;
            }

            result = new OptionalRegionDepth(value);
            return true;
        }

        public int CompareTo(OptionalRegionDepth other)
        {
            return Value.CompareTo(other.Value);
        }

        public bool Equals(OptionalRegionDepth other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is OptionalRegionDepth other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }

        public static bool operator ==(OptionalRegionDepth left, OptionalRegionDepth right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(OptionalRegionDepth left, OptionalRegionDepth right)
        {
            return !left.Equals(right);
        }
    }

    public static class OptionalRegionTokenCodec
    {
        public static bool TryParseAccessRule(string token, out OptionalRegionAccessRule value)
        {
            switch (token)
            {
                case "BASIC": value = OptionalRegionAccessRule.Basic; return true;
                case "TOOL": value = OptionalRegionAccessRule.Tool; return true;
                case "ENVIRONMENT": value = OptionalRegionAccessRule.Environment; return true;
                case "EXPLOSIVE": value = OptionalRegionAccessRule.Explosive; return true;
                case "HIDDEN": value = OptionalRegionAccessRule.Hidden; return true;
                default: value = default(OptionalRegionAccessRule); return false;
            }
        }

        public static string ToToken(OptionalRegionAccessRule value)
        {
            switch (value)
            {
                case OptionalRegionAccessRule.Basic: return "BASIC";
                case OptionalRegionAccessRule.Tool: return "TOOL";
                case OptionalRegionAccessRule.Environment: return "ENVIRONMENT";
                case OptionalRegionAccessRule.Explosive: return "EXPLOSIVE";
                case OptionalRegionAccessRule.Hidden: return "HIDDEN";
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        public static bool TryParseRewardTier(string token, out OptionalRewardTier value)
        {
            switch (token)
            {
                case "NONE": value = OptionalRewardTier.None; return true;
                case "LOW": value = OptionalRewardTier.Low; return true;
                case "MEDIUM": value = OptionalRewardTier.Medium; return true;
                case "HIGH": value = OptionalRewardTier.High; return true;
                case "UNIQUE": value = OptionalRewardTier.Unique; return true;
                default: value = default(OptionalRewardTier); return false;
            }
        }

        public static string ToToken(OptionalRewardTier value)
        {
            switch (value)
            {
                case OptionalRewardTier.None: return "NONE";
                case OptionalRewardTier.Low: return "LOW";
                case OptionalRewardTier.Medium: return "MEDIUM";
                case OptionalRewardTier.High: return "HIGH";
                case OptionalRewardTier.Unique: return "UNIQUE";
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        public static bool TryParseReturnPolicy(string token, out OptionalReturnPolicy value)
        {
            switch (token)
            {
                case "BACKTRACK": value = OptionalReturnPolicy.BacktrackToAttachment; return true;
                case "RETURN_GATE": value = OptionalReturnPolicy.ReturnGateToMandatory; return true;
                case "SAFE_EXIT": value = OptionalReturnPolicy.SafeExitToMandatory; return true;
                default: value = default(OptionalReturnPolicy); return false;
            }
        }

        public static string ToToken(OptionalReturnPolicy value)
        {
            switch (value)
            {
                case OptionalReturnPolicy.BacktrackToAttachment: return "BACKTRACK";
                case OptionalReturnPolicy.ReturnGateToMandatory: return "RETURN_GATE";
                case OptionalReturnPolicy.SafeExitToMandatory: return "SAFE_EXIT";
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
    }

    internal static class OptionalRegionValidation
    {
        public static void RequireValid(OptionalRegionId id, string parameterName)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("Optional region ID must be valid.", parameterName);
            }
        }

        public static void RequireValid(OptionalRegionDepth depth, string parameterName)
        {
            if (!depth.IsValid)
            {
                throw new ArgumentException("Optional region depth must be valid.", parameterName);
            }
        }

        public static void RequireAccessRule(OptionalRegionAccessRule value, string parameterName)
        {
            switch (value)
            {
                case OptionalRegionAccessRule.Basic:
                case OptionalRegionAccessRule.Tool:
                case OptionalRegionAccessRule.Environment:
                case OptionalRegionAccessRule.Explosive:
                case OptionalRegionAccessRule.Hidden:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(parameterName);
            }
        }

        public static void RequireRewardTier(OptionalRewardTier value, string parameterName)
        {
            switch (value)
            {
                case OptionalRewardTier.None:
                case OptionalRewardTier.Low:
                case OptionalRewardTier.Medium:
                case OptionalRewardTier.High:
                case OptionalRewardTier.Unique:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(parameterName);
            }
        }

        public static void RequireReturnPolicy(OptionalReturnPolicy value, string parameterName)
        {
            switch (value)
            {
                case OptionalReturnPolicy.BacktrackToAttachment:
                case OptionalReturnPolicy.ReturnGateToMandatory:
                case OptionalReturnPolicy.SafeExitToMandatory:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(parameterName);
            }
        }

        public static void RequireIndexCoordinateIdentity(int index, StarNight.Map.WorldGeneration.Domain.SectorCoord coordinate, string parameterName)
        {
            if (index < 0 || index >= StarNight.Map.WorldGeneration.Domain.WorldGenConstants.SectorCount)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }

            if (WorldGridIndex.ToCoordinate(index) != coordinate)
            {
                throw new ArgumentException("Sector index and coordinate must match.", parameterName);
            }
        }
    }
}
