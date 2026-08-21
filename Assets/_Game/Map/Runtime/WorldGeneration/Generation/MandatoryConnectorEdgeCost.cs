using System;

namespace StarNight.Map.WorldGeneration.Generation
{
    public readonly struct MandatoryConnectorEdgeCost : IEquatable<MandatoryConnectorEdgeCost>, IComparable<MandatoryConnectorEdgeCost>
    {
        public MandatoryConnectorEdgeCost(int manhattanDistance, int reservationOrderSpread, int kindPenalty, int sharedApproachPenalty)
        {
            if (manhattanDistance < 0) throw new ArgumentOutOfRangeException(nameof(manhattanDistance));
            if (reservationOrderSpread < 0) throw new ArgumentOutOfRangeException(nameof(reservationOrderSpread));
            if (kindPenalty < 0) throw new ArgumentOutOfRangeException(nameof(kindPenalty));
            if (sharedApproachPenalty < 0) throw new ArgumentOutOfRangeException(nameof(sharedApproachPenalty));
            ManhattanDistance = manhattanDistance;
            ReservationOrderSpread = reservationOrderSpread;
            KindPenalty = kindPenalty;
            SharedApproachPenalty = sharedApproachPenalty;
            TotalCost = checked(checked(checked(manhattanDistance * 1000) + checked(reservationOrderSpread * 10)) + checked(kindPenalty + sharedApproachPenalty));
        }

        public int ManhattanDistance { get; }
        public int ReservationOrderSpread { get; }
        public int KindPenalty { get; }
        public int SharedApproachPenalty { get; }
        public int TotalCost { get; }
        public bool Equals(MandatoryConnectorEdgeCost other) => ManhattanDistance == other.ManhattanDistance && ReservationOrderSpread == other.ReservationOrderSpread && KindPenalty == other.KindPenalty && SharedApproachPenalty == other.SharedApproachPenalty && TotalCost == other.TotalCost;
        public override bool Equals(object obj) => obj is MandatoryConnectorEdgeCost other && Equals(other);
        public int CompareTo(MandatoryConnectorEdgeCost other) => TotalCost.CompareTo(other.TotalCost);
        public override int GetHashCode()
        {
            unchecked { return (((ManhattanDistance * 397) ^ ReservationOrderSpread) * 397 ^ KindPenalty) * 397 ^ SharedApproachPenalty; }
        }
        public static bool operator ==(MandatoryConnectorEdgeCost left, MandatoryConnectorEdgeCost right) => left.Equals(right);
        public static bool operator !=(MandatoryConnectorEdgeCost left, MandatoryConnectorEdgeCost right) => !left.Equals(right);
    }
}
