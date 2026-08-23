using System;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public enum MicrochunkTraversalMovementKind
    {
        Flood,
        Walk,
        Jump,
        Drop,
        Climb,
        SocketEntry
    }

    public sealed class MicrochunkTraversalEdge
    {
        public const string FloodMovement = "FLOOD";
        public const string WalkMovement = "WALK";
        public const string JumpMovement = "JUMP";
        public const string DropMovement = "DROP";
        public const string ClimbMovement = "CLIMB";
        public const string SocketEntryMovement = "SOCKET_ENTRY";

        public MicrochunkLocalCoord SourceCoordinate { get; }
        public MicrochunkLocalCoord TargetCoordinate { get; }
        public MicrochunkLocalCoord Source => SourceCoordinate;
        public MicrochunkLocalCoord Target => TargetCoordinate;
        public string MovementKind { get; }
        public string MovementKindToken => MovementKind;
        public MicrochunkTraversalMovementKind MovementKindValue => ParseMovementKind(MovementKind);
        public int Cost { get; }

        public MicrochunkTraversalEdge(
            MicrochunkLocalCoord sourceCoordinate,
            MicrochunkLocalCoord targetCoordinate,
            string movementKind,
            int cost)
        {
            if (!IsSupportedMovementKind(movementKind))
            {
                throw new ArgumentException("Movement kind is not a supported exact token.", nameof(movementKind));
            }

            if (cost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cost));
            }

            SourceCoordinate = sourceCoordinate;
            TargetCoordinate = targetCoordinate;
            MovementKind = movementKind;
            Cost = cost;
        }

        public MicrochunkTraversalEdge(
            MicrochunkLocalCoord sourceCoordinate,
            MicrochunkLocalCoord targetCoordinate,
            MicrochunkTraversalMovementKind movementKind,
            int cost)
            : this(sourceCoordinate, targetCoordinate, ToMovementToken(movementKind), cost)
        {
        }

        public static bool IsSupportedMovementKind(string value)
        {
            return value == FloodMovement ||
                   value == WalkMovement ||
                   value == JumpMovement ||
                   value == DropMovement ||
                   value == ClimbMovement ||
                   value == SocketEntryMovement;
        }

        public static string ToMovementToken(MicrochunkTraversalMovementKind value)
        {
            switch (value)
            {
                case MicrochunkTraversalMovementKind.Flood: return FloodMovement;
                case MicrochunkTraversalMovementKind.Walk: return WalkMovement;
                case MicrochunkTraversalMovementKind.Jump: return JumpMovement;
                case MicrochunkTraversalMovementKind.Drop: return DropMovement;
                case MicrochunkTraversalMovementKind.Climb: return ClimbMovement;
                case MicrochunkTraversalMovementKind.SocketEntry: return SocketEntryMovement;
                default: throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        public static MicrochunkTraversalMovementKind ParseMovementKind(string value)
        {
            switch (value)
            {
                case FloodMovement: return MicrochunkTraversalMovementKind.Flood;
                case WalkMovement: return MicrochunkTraversalMovementKind.Walk;
                case JumpMovement: return MicrochunkTraversalMovementKind.Jump;
                case DropMovement: return MicrochunkTraversalMovementKind.Drop;
                case ClimbMovement: return MicrochunkTraversalMovementKind.Climb;
                case SocketEntryMovement: return MicrochunkTraversalMovementKind.SocketEntry;
                default: throw new ArgumentException("Movement kind is not a supported exact token.", nameof(value));
            }
        }
    }
}
