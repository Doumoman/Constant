using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public sealed class MicrochunkReachabilityPolicy
    {
        private static readonly string[] CanonicalMovementOrder =
        {
            MicrochunkTraversalEdge.FloodMovement,
            MicrochunkTraversalEdge.WalkMovement,
            MicrochunkTraversalEdge.JumpMovement,
            MicrochunkTraversalEdge.DropMovement,
            MicrochunkTraversalEdge.ClimbMovement,
            MicrochunkTraversalEdge.SocketEntryMovement
        };

        private readonly IReadOnlyList<string> climbMarkerCodes;
        private readonly IReadOnlyList<string> neighborOrdering;
        private readonly HashSet<string> climbMarkerLookup;
        private readonly Dictionary<string, int> neighborOrderLookup;

        public static MicrochunkReachabilityPolicy Default { get; } =
            new MicrochunkReachabilityPolicy(1, 2, 3, Array.Empty<string>(), CanonicalMovementOrder);

        public int MaximumJumpRise { get; }
        public int MaximumJumpHorizontalSpan { get; }
        public int MaximumDropDistance { get; }
        public IReadOnlyList<string> ClimbMarkerCodes => climbMarkerCodes;
        public IReadOnlyList<string> NeighborOrdering => neighborOrdering;
        public IReadOnlyList<string> DeterministicNeighborOrdering => neighborOrdering;

        public MicrochunkReachabilityPolicy(
            int maximumJumpRise,
            int maximumJumpHorizontalSpan,
            int maximumDropDistance,
            IEnumerable<string> climbMarkerCodes)
            : this(
                maximumJumpRise,
                maximumJumpHorizontalSpan,
                maximumDropDistance,
                climbMarkerCodes,
                CanonicalMovementOrder)
        {
        }

        public MicrochunkReachabilityPolicy(
            int maximumJumpRise,
            int maximumJumpHorizontalSpan,
            int maximumDropDistance,
            IEnumerable<string> climbMarkerCodes,
            IEnumerable<string> neighborOrdering)
        {
            if (maximumJumpRise < 0) throw new ArgumentOutOfRangeException(nameof(maximumJumpRise));
            if (maximumJumpHorizontalSpan < 0) throw new ArgumentOutOfRangeException(nameof(maximumJumpHorizontalSpan));
            if (maximumDropDistance < 0) throw new ArgumentOutOfRangeException(nameof(maximumDropDistance));
            if (climbMarkerCodes == null) throw new ArgumentNullException(nameof(climbMarkerCodes));
            if (neighborOrdering == null) throw new ArgumentNullException(nameof(neighborOrdering));

            MaximumJumpRise = maximumJumpRise;
            MaximumJumpHorizontalSpan = maximumJumpHorizontalSpan;
            MaximumDropDistance = maximumDropDistance;

            var markers = new List<string>();
            climbMarkerLookup = new HashSet<string>(StringComparer.Ordinal);
            foreach (var marker in climbMarkerCodes)
            {
                if (string.IsNullOrWhiteSpace(marker) || marker == "NONE")
                {
                    throw new ArgumentException("Climb marker codes must be non-empty tile-code tokens other than NONE.", nameof(climbMarkerCodes));
                }

                if (!climbMarkerLookup.Add(marker))
                {
                    throw new ArgumentException("Climb marker codes must be unique.", nameof(climbMarkerCodes));
                }

                markers.Add(marker);
            }
            markers.Sort(StringComparer.Ordinal);
            this.climbMarkerCodes = new ReadOnlyCollection<string>(markers);

            var order = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var movement in neighborOrdering)
            {
                if (!MicrochunkTraversalEdge.IsSupportedMovementKind(movement))
                {
                    throw new ArgumentException("Neighbor ordering contains an unsupported movement token.", nameof(neighborOrdering));
                }

                if (!seen.Add(movement))
                {
                    throw new ArgumentException("Neighbor ordering must not contain duplicates.", nameof(neighborOrdering));
                }

                order.Add(movement);
            }

            foreach (var movement in CanonicalMovementOrder)
            {
                if (seen.Add(movement)) order.Add(movement);
            }

            this.neighborOrdering = new ReadOnlyCollection<string>(order);
            neighborOrderLookup = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var index = 0; index < order.Count; index++)
            {
                neighborOrderLookup.Add(order[index], index);
            }
        }

        public bool IsClimbMarker(string markerCode)
        {
            return markerCode != null && climbMarkerLookup.Contains(markerCode);
        }

        public int GetNeighborOrder(string movementKind)
        {
            if (!MicrochunkTraversalEdge.IsSupportedMovementKind(movementKind))
            {
                throw new ArgumentException("Movement kind is not a supported exact token.", nameof(movementKind));
            }

            return neighborOrderLookup[movementKind];
        }
    }
}
