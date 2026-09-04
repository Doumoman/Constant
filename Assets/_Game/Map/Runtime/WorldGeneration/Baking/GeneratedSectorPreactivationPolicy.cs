using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Baking
{
    public enum GeneratedSectorDirectionHint
    {
        None = 0,
        Left = 1,
        Right = 2,
        Down = 3,
        Up = 4,
        LeftDown = 5,
        LeftUp = 6,
        RightDown = 7,
        RightUp = 8,
    }

    public sealed class GeneratedSectorPreactivationIntent :
        IComparable<GeneratedSectorPreactivationIntent>
    {
        internal GeneratedSectorPreactivationIntent(
            GeneratedSectorCoordinate coordinate,
            GeneratedSectorDirectionHint direction,
            string reason)
        {
            Coordinate = coordinate;
            Direction = direction;
            Reason = reason ?? string.Empty;
            StableToken = "PREACTIVATION_INTENT|" +
                (Coordinate == null ? "MISSING" : Coordinate.ToString()) + "|" +
                Direction.ToString().ToUpperInvariant() + "|" + Reason;
        }

        public GeneratedSectorCoordinate Coordinate { get; }
        public GeneratedSectorDirectionHint Direction { get; }
        public string Reason { get; }
        public string StableToken { get; }
        public int CompareTo(GeneratedSectorPreactivationIntent other) => other == null ? -1 :
            Coordinate.CompareTo(other.Coordinate);
    }

    public sealed class GeneratedSectorPreactivationPolicy
    {
        public GeneratedSectorPreactivationPolicy(
            double lowThreshold,
            double highThreshold,
            double hysteresis)
        {
            LowThreshold = lowThreshold;
            HighThreshold = highThreshold;
            Hysteresis = hysteresis;
            StableToken = string.Join("|", new[]
            {
                "PREACTIVATION_POLICY", Number(LowThreshold), Number(HighThreshold),
                Number(Hysteresis),
            });
            Digest = BakingCanonicalDigest.HashCanonicalLines(new[] { StableToken });
        }

        public double LowThreshold { get; }
        public double HighThreshold { get; }
        public double Hysteresis { get; }
        public bool IsValid => LowThreshold >= 0d && LowThreshold < 0.5d &&
            HighThreshold > 0.5d && HighThreshold <= 1d && LowThreshold < HighThreshold &&
            Hysteresis >= 0d && LowThreshold + Hysteresis < HighThreshold - Hysteresis &&
            GeneratedSectorWindowDigest.IsLowerHexSha256(Digest);
        public string StableToken { get; }
        public string Digest { get; }
        public static GeneratedSectorPreactivationPolicy Default { get; } =
            new GeneratedSectorPreactivationPolicy(0.12d, 0.88d, 0.04d);

        public IReadOnlyList<GeneratedSectorPreactivationIntent> Evaluate(
            GeneratedSectorCoordinate center,
            double localProgressX,
            double localProgressY,
            GeneratedSectorDirectionHint directionHint,
            bool latched)
        {
            if (!IsValid || center == null || !center.IsInWorld ||
                localProgressX < 0d || localProgressX > 1d ||
                localProgressY < 0d || localProgressY > 1d ||
                directionHint == GeneratedSectorDirectionHint.None)
                return new ReadOnlyCollection<GeneratedSectorPreactivationIntent>(
                    Array.Empty<GeneratedSectorPreactivationIntent>());

            var low = LowThreshold + (latched ? Hysteresis : 0d);
            var high = HighThreshold - (latched ? Hysteresis : 0d);
            var horizontal = localProgressX <= low
                ? GeneratedSectorDirectionHint.Left
                : localProgressX >= high ? GeneratedSectorDirectionHint.Right :
                    GeneratedSectorDirectionHint.None;
            var vertical = localProgressY <= low
                ? GeneratedSectorDirectionHint.Down
                : localProgressY >= high ? GeneratedSectorDirectionHint.Up :
                    GeneratedSectorDirectionHint.None;
            if (!HintMatches(directionHint, horizontal, vertical))
                return new ReadOnlyCollection<GeneratedSectorPreactivationIntent>(
                    Array.Empty<GeneratedSectorPreactivationIntent>());

            var intents = new List<GeneratedSectorPreactivationIntent>();
            if (horizontal != GeneratedSectorDirectionHint.None)
                Add(intents, center, horizontal, "HORIZONTAL_EDGE_THRESHOLD");
            if (vertical != GeneratedSectorDirectionHint.None)
                Add(intents, center, vertical, "VERTICAL_EDGE_THRESHOLD");
            if (horizontal != GeneratedSectorDirectionHint.None &&
                vertical != GeneratedSectorDirectionHint.None)
                Add(intents, center, Diagonal(horizontal, vertical),
                    "DIAGONAL_EDGE_THRESHOLD");
            return new ReadOnlyCollection<GeneratedSectorPreactivationIntent>(intents
                .Where(value => value.Coordinate.IsInWorld).Distinct(new IntentCoordinateComparer())
                .OrderBy(value => value).ToArray());
        }

        private static bool HintMatches(
            GeneratedSectorDirectionHint hint,
            GeneratedSectorDirectionHint horizontal,
            GeneratedSectorDirectionHint vertical)
        {
            switch (hint)
            {
                case GeneratedSectorDirectionHint.Left:
                case GeneratedSectorDirectionHint.Right:
                    return hint == horizontal;
                case GeneratedSectorDirectionHint.Down:
                case GeneratedSectorDirectionHint.Up:
                    return hint == vertical;
                case GeneratedSectorDirectionHint.LeftDown:
                    return horizontal == GeneratedSectorDirectionHint.Left &&
                           vertical == GeneratedSectorDirectionHint.Down;
                case GeneratedSectorDirectionHint.LeftUp:
                    return horizontal == GeneratedSectorDirectionHint.Left &&
                           vertical == GeneratedSectorDirectionHint.Up;
                case GeneratedSectorDirectionHint.RightDown:
                    return horizontal == GeneratedSectorDirectionHint.Right &&
                           vertical == GeneratedSectorDirectionHint.Down;
                case GeneratedSectorDirectionHint.RightUp:
                    return horizontal == GeneratedSectorDirectionHint.Right &&
                           vertical == GeneratedSectorDirectionHint.Up;
                default:
                    return false;
            }
        }

        private static void Add(
            ICollection<GeneratedSectorPreactivationIntent> intents,
            GeneratedSectorCoordinate center,
            GeneratedSectorDirectionHint direction,
            string reason)
        {
            var dx = direction == GeneratedSectorDirectionHint.Left ||
                     direction == GeneratedSectorDirectionHint.LeftDown ||
                     direction == GeneratedSectorDirectionHint.LeftUp ? -1 :
                direction == GeneratedSectorDirectionHint.Right ||
                direction == GeneratedSectorDirectionHint.RightDown ||
                direction == GeneratedSectorDirectionHint.RightUp ? 1 : 0;
            var dy = direction == GeneratedSectorDirectionHint.Down ||
                     direction == GeneratedSectorDirectionHint.LeftDown ||
                     direction == GeneratedSectorDirectionHint.RightDown ? -1 :
                direction == GeneratedSectorDirectionHint.Up ||
                direction == GeneratedSectorDirectionHint.LeftUp ||
                direction == GeneratedSectorDirectionHint.RightUp ? 1 : 0;
            intents.Add(new GeneratedSectorPreactivationIntent(
                new GeneratedSectorCoordinate(center.X + dx, center.Y + dy), direction, reason));
        }

        private static GeneratedSectorDirectionHint Diagonal(
            GeneratedSectorDirectionHint horizontal,
            GeneratedSectorDirectionHint vertical)
        {
            if (horizontal == GeneratedSectorDirectionHint.Left)
                return vertical == GeneratedSectorDirectionHint.Down
                    ? GeneratedSectorDirectionHint.LeftDown : GeneratedSectorDirectionHint.LeftUp;
            return vertical == GeneratedSectorDirectionHint.Down
                ? GeneratedSectorDirectionHint.RightDown : GeneratedSectorDirectionHint.RightUp;
        }

        private static string Number(double value) =>
            value.ToString("R", CultureInfo.InvariantCulture);

        private sealed class IntentCoordinateComparer :
            IEqualityComparer<GeneratedSectorPreactivationIntent>
        {
            public bool Equals(
                GeneratedSectorPreactivationIntent first,
                GeneratedSectorPreactivationIntent second) => ReferenceEquals(first, second) ||
                first != null && second != null && first.Coordinate.Equals(second.Coordinate);
            public int GetHashCode(GeneratedSectorPreactivationIntent value) =>
                value == null ? 0 : value.Coordinate.GetHashCode();
        }
    }
}
