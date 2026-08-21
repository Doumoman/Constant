using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class UpDownConflictCandidate
    {
        public UpDownConflictCandidate(
            UpDownConflictId conflictId,
            VerticalGatewayId sourceGatewayId,
            SectorCoord coordinate,
            bool requiresUp,
            bool requiresDown,
            bool opensLeft,
            bool opensRight,
            bool isRoleEligible,
            bool isReserved,
            string reservationIdentity,
            string biomeIdentity,
            int checkedCost)
        {
            if (!conflictId.IsValid) throw new ArgumentException("Conflict identity must be valid.", nameof(conflictId));
            if (!sourceGatewayId.IsValid) throw new ArgumentException("Source gateway identity must be valid.", nameof(sourceGatewayId));
            if (reservationIdentity == null) throw new ArgumentNullException(nameof(reservationIdentity));
            if (biomeIdentity == null) throw new ArgumentNullException(nameof(biomeIdentity));
            if (checkedCost != 1 && checkedCost != 2 && checkedCost != 4 && checkedCost != 8)
                throw new ArgumentOutOfRangeException(nameof(checkedCost));
            ConflictId = conflictId;
            SourceGatewayId = sourceGatewayId;
            Coordinate = coordinate;
            RequiresUp = requiresUp;
            RequiresDown = requiresDown;
            OpensLeft = opensLeft;
            OpensRight = opensRight;
            IsRoleEligible = isRoleEligible;
            IsReserved = isReserved;
            ReservationIdentity = reservationIdentity;
            BiomeIdentity = biomeIdentity;
            CheckedCost = checkedCost;
            IsInsideBounds = coordinate.X >= 0 && coordinate.X < WorldGenConstants.SectorColumns &&
                             coordinate.Y >= 0 && coordinate.Y < WorldGenConstants.SectorRows;
            CanBeType4 = requiresUp && requiresDown && IsInsideBounds && isRoleEligible && !isReserved;
        }

        public UpDownConflictId ConflictId { get; }
        public VerticalGatewayId SourceGatewayId { get; }
        public SectorCoord Coordinate { get; }
        public bool RequiresUp { get; }
        public bool RequiresDown { get; }
        public bool OpensLeft { get; }
        public bool OpensRight { get; }
        public bool IsRoleEligible { get; }
        public bool IsReserved { get; }
        public string ReservationIdentity { get; }
        public string BiomeIdentity { get; }
        public int CheckedCost { get; }
        public bool IsInsideBounds { get; }
        public bool CanBeType4 { get; }
        public bool IsConflict => RequiresUp && RequiresDown && !CanBeType4;
    }
}
