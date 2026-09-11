using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using StarNight.Map.WorldGeneration.SpecialRegions;
using StarNight.Map.WorldGeneration.WorldData;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum Sv5SpacePlaceKind { Core = 1, Large = 2, Ordinary = 3 }
    public enum Sv5SpaceConnectionKind { CoreProgression = 1, OptionalBranch = 2, VillageInterior = 3 }
    public enum Sv5SpaceCrossingKind { Join = 1, ConditionalGate = 2, Separated = 3, Pending = 4 }
    public enum Sv5SpaceReservationKind
    {
        CoreProtected = 1,
        CoreRoute = 2,
        PlannedFootprint = 3,
        CorridorCenterline = 4,
        CorridorClearance = 5,
        PortAperture = 6,
        ConditionalGate = 7,
    }

    public readonly struct Sv5SpaceBounds : IEquatable<Sv5SpaceBounds>
    {
        public Sv5SpaceBounds(int x, int y, int width, int height)
        {
            if (x < 0 || y < 0 || width < 1 || height < 1 || x + width > 624 || y + height > 416)
                throw new ArgumentOutOfRangeException(nameof(x), "Bounds must fit the 624x416 half-open world.");
            X = x; Y = y; Width = width; Height = height;
        }

        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
        public int MaxXExclusive => X + Width;
        public int MaxYExclusive => Y + Height;
        public bool Contains(RmapSpecialWorldPoint point) => point.X >= X && point.X < MaxXExclusive &&
            point.Y >= Y && point.Y < MaxYExclusive;
        public bool Equals(Sv5SpaceBounds other) => X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
        public override bool Equals(object obj) => obj is Sv5SpaceBounds other && Equals(other);
        public override int GetHashCode() { unchecked { return (((X * 397) ^ Y) * 397 ^ Width) * 397 ^ Height; } }
        public override string ToString() => X.ToString(CultureInfo.InvariantCulture) + "," +
            Y.ToString(CultureInfo.InvariantCulture) + "," + Width.ToString(CultureInfo.InvariantCulture) + "," +
            Height.ToString(CultureInfo.InvariantCulture);
    }

    public sealed class Sv5SpaceGraphAuthoringProfile
    {
        private readonly ReadOnlyCollection<Sv5SpaceFamilySpec> families;

        public Sv5SpaceGraphAuthoringProfile(string id, string version,
            IEnumerable<Sv5SpaceFamilySpec> sourceFamilies)
        {
            Id = Require(id, nameof(id));
            Version = Require(version, nameof(version));
            families = new ReadOnlyCollection<Sv5SpaceFamilySpec>((sourceFamilies ??
                Array.Empty<Sv5SpaceFamilySpec>()).Where(value => value != null)
                .OrderBy(value => value.Ordinal).ToArray());
            if (families.Count < 8 || families.Count(value => value.Kind == Sv5SpacePlaceKind.Large) < 4 ||
                families.Count(value => value.Kind == Sv5SpacePlaceKind.Ordinary) < 4)
                throw new ArgumentException("A profile needs several large and ordinary place families.", nameof(sourceFamilies));
            Digest = RmapWorldDefinition.Hash("SV5_SPACE_PROFILE_V1\n" + Id + "\n" + Version + "\n" +
                string.Join("\n", families.Select(value => value.StableToken)));
        }

        public string Id { get; }
        public string Version { get; }
        public IReadOnlyList<Sv5SpaceFamilySpec> Families => families;
        public string Digest { get; }

        public static Sv5SpaceGraphAuthoringProfile RepresentativeV1() => new Sv5SpaceGraphAuthoringProfile(
            "SV5_REPRESENTATIVE_DISTRIBUTED", "1.0.0", new[]
            {
                new Sv5SpaceFamilySpec(0, "CAVE_BAND", Sv5SpacePlaceKind.Large, 60, 24, "SV5_24_CAVE"),
                new Sv5SpaceFamilySpec(1, "LIBRARY_STACK", Sv5SpacePlaceKind.Large, 36, 32, "SV5_21_LIBRARY"),
                new Sv5SpaceFamilySpec(2, "CANYON_SHAFT", Sv5SpacePlaceKind.Large, 24, 32, "SV5_26_CANYON"),
                new Sv5SpaceFamilySpec(3, "HIGH_HALL", Sv5SpacePlaceKind.Large, 36, 16, "SV5_28_HALL"),
                new Sv5SpaceFamilySpec(4, "FARM_TERRACE", Sv5SpacePlaceKind.Large, 36, 24, "SV5_31_FARM"),
                new Sv5SpaceFamilySpec(5, "RANCH_YARD", Sv5SpacePlaceKind.Large, 24, 24, "SV5_29_RANCH"),
                new Sv5SpaceFamilySpec(6, "LAKE_CHAMBER", Sv5SpacePlaceKind.Large, 36, 24, "SV5_30_LAKE"),
                new Sv5SpaceFamilySpec(7, "JUMP_RESERVE", Sv5SpacePlaceKind.Large, 24, 32, "SV5_13_JUMP_CONTRACT"),
                new Sv5SpaceFamilySpec(8, "ORDINARY_ROOM_A", Sv5SpacePlaceKind.Ordinary, 12, 8, "SV5_08_INFILL"),
                new Sv5SpaceFamilySpec(9, "ORDINARY_ROOM_B", Sv5SpacePlaceKind.Ordinary, 16, 8, "SV5_08_INFILL"),
                new Sv5SpaceFamilySpec(10, "ORDINARY_ROOM_C", Sv5SpacePlaceKind.Ordinary, 12, 12, "SV5_08_INFILL"),
                new Sv5SpaceFamilySpec(11, "ORDINARY_ROOM_D", Sv5SpacePlaceKind.Ordinary, 20, 8, "SV5_08_INFILL"),
                new Sv5SpaceFamilySpec(12, "ORDINARY_ROOM_E", Sv5SpacePlaceKind.Ordinary, 12, 8, "SV5_08_INFILL"),
                new Sv5SpaceFamilySpec(13, "ORDINARY_ROOM_F", Sv5SpacePlaceKind.Ordinary, 16, 12, "SV5_08_INFILL"),
            });

        internal static string Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A stable value is required.", name);
            return value.Trim();
        }
    }

    public sealed class Sv5SpaceFamilySpec
    {
        public Sv5SpaceFamilySpec(int ordinal, string family, Sv5SpacePlaceKind kind, int width, int height,
            string futureOwner)
        {
            if (ordinal < 0 || width < 7 || height < 5) throw new ArgumentOutOfRangeException(nameof(ordinal));
            if (kind == Sv5SpacePlaceKind.Core) throw new ArgumentOutOfRangeException(nameof(kind));
            Ordinal = ordinal;
            Family = Sv5SpaceGraphAuthoringProfile.Require(family, nameof(family));
            Kind = kind;
            Width = width;
            Height = height;
            FutureOwner = Sv5SpaceGraphAuthoringProfile.Require(futureOwner, nameof(futureOwner));
        }

        public int Ordinal { get; }
        public string Family { get; }
        public Sv5SpacePlaceKind Kind { get; }
        public int Width { get; }
        public int Height { get; }
        public string FutureOwner { get; }
        public string StableToken => Ordinal.ToString(CultureInfo.InvariantCulture) + "|" + Family + "|" + Kind + "|" +
            Width.ToString(CultureInfo.InvariantCulture) + "x" + Height.ToString(CultureInfo.InvariantCulture) + "|" + FutureOwner;
    }

    public sealed class Sv5SpacePlace : IComparable<Sv5SpacePlace>
    {
        internal Sv5SpacePlace(string id, string family, Sv5SpacePlaceKind kind, Sv5SpaceBounds bounds,
            string coreSiteId, string futureOwner, int distributionSector)
        {
            Id = Sv5SpaceGraphAuthoringProfile.Require(id, nameof(id));
            Family = Sv5SpaceGraphAuthoringProfile.Require(family, nameof(family));
            Kind = kind;
            Bounds = bounds;
            CoreSiteId = coreSiteId ?? string.Empty;
            FutureOwner = Sv5SpaceGraphAuthoringProfile.Require(futureOwner, nameof(futureOwner));
            DistributionSector = distributionSector;
        }

        public string Id { get; }
        public string Family { get; }
        public Sv5SpacePlaceKind Kind { get; }
        public Sv5SpaceBounds Bounds { get; }
        public string CoreSiteId { get; }
        public string FutureOwner { get; }
        public int DistributionSector { get; }
        public string Readiness => Kind == Sv5SpacePlaceKind.Core ? "PRESERVED_CORE" : "PLANNED_SHELL";
        public int CompareTo(Sv5SpacePlace other) => other == null ? 1 : string.Compare(Id, other.Id, StringComparison.Ordinal);
    }

    public sealed class Sv5SpacePort : IComparable<Sv5SpacePort>
    {
        private readonly ReadOnlyCollection<RmapSpecialWorldPoint> boundaryCells;

        internal Sv5SpacePort(string id, string placeId, IEnumerable<RmapSpecialWorldPoint> cells,
            RmapSpecialWorldPoint anchor, RmapWorldGraphDirection direction, string flow, string condition,
            string sourceAccessId, string sourceNodeId, string status)
        {
            Id = Sv5SpaceGraphAuthoringProfile.Require(id, nameof(id));
            PlaceId = Sv5SpaceGraphAuthoringProfile.Require(placeId, nameof(placeId));
            boundaryCells = new ReadOnlyCollection<RmapSpecialWorldPoint>((cells ?? Array.Empty<RmapSpecialWorldPoint>())
                .Distinct().OrderBy(value => value).ToArray());
            if (boundaryCells.Count == 0 || !boundaryCells.Contains(anchor))
                throw new ArgumentException("A port anchor must be one of its boundary cells.", nameof(anchor));
            Anchor = anchor;
            Direction = direction;
            Flow = Sv5SpaceGraphAuthoringProfile.Require(flow, nameof(flow));
            Condition = Sv5SpaceGraphAuthoringProfile.Require(condition, nameof(condition));
            SourceAccessId = sourceAccessId ?? string.Empty;
            SourceNodeId = sourceNodeId ?? string.Empty;
            Status = Sv5SpaceGraphAuthoringProfile.Require(status, nameof(status));
        }

        public string Id { get; }
        public string PlaceId { get; }
        public IReadOnlyList<RmapSpecialWorldPoint> BoundaryCells => boundaryCells;
        public RmapSpecialWorldPoint Anchor { get; }
        public RmapWorldGraphDirection Direction { get; }
        public string Flow { get; }
        public string Condition { get; }
        public string SourceAccessId { get; }
        public string SourceNodeId { get; }
        public string Status { get; }
        public int CompareTo(Sv5SpacePort other) => other == null ? 1 : string.Compare(Id, other.Id, StringComparison.Ordinal);
    }

    public sealed class Sv5SpaceConnection : IComparable<Sv5SpaceConnection>
    {
        private readonly ReadOnlyCollection<RmapSpecialWorldPoint> centerline;
        private readonly ReadOnlyCollection<RmapSpecialWorldPoint> envelope;
        private readonly ReadOnlyCollection<RmapSpecialWorldPoint> apertureCells;

        internal Sv5SpaceConnection(string id, Sv5SpaceConnectionKind kind, string fromPortId, string toPortId,
            string fromPlaceId, string toPlaceId, RmapWorldGraphDirection direction, string flow, string condition,
            string sourceGraphEdgeId, string selectionState, IEnumerable<RmapSpecialWorldPoint> sourceCenterline,
            IEnumerable<RmapSpecialWorldPoint> sourceEnvelope,
            IEnumerable<RmapSpecialWorldPoint> sourceApertureCells)
        {
            Id = Sv5SpaceGraphAuthoringProfile.Require(id, nameof(id));
            Kind = kind;
            FromPortId = Sv5SpaceGraphAuthoringProfile.Require(fromPortId, nameof(fromPortId));
            ToPortId = Sv5SpaceGraphAuthoringProfile.Require(toPortId, nameof(toPortId));
            FromPlaceId = Sv5SpaceGraphAuthoringProfile.Require(fromPlaceId, nameof(fromPlaceId));
            ToPlaceId = Sv5SpaceGraphAuthoringProfile.Require(toPlaceId, nameof(toPlaceId));
            Direction = direction;
            Flow = Sv5SpaceGraphAuthoringProfile.Require(flow, nameof(flow));
            Condition = Sv5SpaceGraphAuthoringProfile.Require(condition, nameof(condition));
            SourceGraphEdgeId = sourceGraphEdgeId ?? string.Empty;
            SelectionState = Sv5SpaceGraphAuthoringProfile.Require(selectionState, nameof(selectionState));
            centerline = new ReadOnlyCollection<RmapSpecialWorldPoint>((sourceCenterline ??
                Array.Empty<RmapSpecialWorldPoint>()).ToArray());
            envelope = new ReadOnlyCollection<RmapSpecialWorldPoint>((sourceEnvelope ??
                Array.Empty<RmapSpecialWorldPoint>()).Distinct().OrderBy(value => value).ToArray());
            apertureCells = new ReadOnlyCollection<RmapSpecialWorldPoint>((sourceApertureCells ??
                Array.Empty<RmapSpecialWorldPoint>()).Distinct().OrderBy(value => value).ToArray());
            if (centerline.Count < 2) throw new ArgumentException("A connection needs a cardinal centerline.", nameof(sourceCenterline));
            for (var index = 1; index < centerline.Count; index++)
                if (Math.Abs(centerline[index].X - centerline[index - 1].X) +
                    Math.Abs(centerline[index].Y - centerline[index - 1].Y) != 1)
                    throw new ArgumentException("Connection centerlines must be cardinally continuous.", nameof(sourceCenterline));
            if (centerline.Any(value => !envelope.Contains(value)) || apertureCells.Any(value => !envelope.Contains(value)))
                throw new ArgumentException("Centerline and aperture cells must be members of the accepted envelope.", nameof(sourceEnvelope));
        }

        public string Id { get; }
        public Sv5SpaceConnectionKind Kind { get; }
        public string FromPortId { get; }
        public string ToPortId { get; }
        public string FromPlaceId { get; }
        public string ToPlaceId { get; }
        public RmapWorldGraphDirection Direction { get; }
        public string Flow { get; }
        public string Condition { get; }
        public string SourceGraphEdgeId { get; }
        public string SelectionState { get; }
        public IReadOnlyList<RmapSpecialWorldPoint> Centerline => centerline;
        public IReadOnlyList<RmapSpecialWorldPoint> Envelope => envelope;
        public IReadOnlyList<RmapSpecialWorldPoint> ApertureCells => apertureCells;
        public int CompareTo(Sv5SpaceConnection other) => other == null ? 1 : string.Compare(Id, other.Id, StringComparison.Ordinal);
    }

    public sealed class Sv5SpaceBoundaryFace : IComparable<Sv5SpaceBoundaryFace>
    {
        internal Sv5SpaceBoundaryFace(RmapSpecialWorldPoint first, RmapSpecialWorldPoint second)
        {
            if (Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y) != 1)
                throw new ArgumentException("A blocking face must join cardinally adjacent cells.", nameof(second));
            if (first.CompareTo(second) <= 0) { First = first; Second = second; }
            else { First = second; Second = first; }
        }
        public RmapSpecialWorldPoint First { get; }
        public RmapSpecialWorldPoint Second { get; }
        public string StableToken => First + ">" + Second;
        public int CompareTo(Sv5SpaceBoundaryFace other) => other == null ? 1 :
            string.Compare(StableToken, other.StableToken, StringComparison.Ordinal);
    }

    public sealed class Sv5SpaceGate : IComparable<Sv5SpaceGate>
    {
        private readonly ReadOnlyCollection<string> contactIds;
        private readonly ReadOnlyCollection<RmapSpecialWorldPoint> blockingCells;
        private readonly ReadOnlyCollection<Sv5SpaceBoundaryFace> blockingFaces;

        internal Sv5SpaceGate(string id, string boundaryId, IEnumerable<string> sourceContactIds,
            IEnumerable<RmapSpecialWorldPoint> sourceBlockingCells,
            IEnumerable<Sv5SpaceBoundaryFace> sourceBlockingFaces, RmapSpecialWorldPoint sideAAnchor,
            RmapSpecialWorldPoint sideBAnchor, RmapWorldGraphDirection direction, string flow, string predicate,
            Sv5SpaceGatePredicate typedPredicate, string sourceConnectionId, string sourceRouteId,
            string sourcePortId, string targetPortId, Sv5SpaceCrossingKind crossing, string sealedState,
            string openState)
        {
            Id = Sv5SpaceGraphAuthoringProfile.Require(id, nameof(id));
            BoundaryId = Sv5SpaceGraphAuthoringProfile.Require(boundaryId, nameof(boundaryId));
            contactIds = new ReadOnlyCollection<string>((sourceContactIds ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray());
            blockingCells = new ReadOnlyCollection<RmapSpecialWorldPoint>((sourceBlockingCells ??
                Array.Empty<RmapSpecialWorldPoint>()).Distinct().OrderBy(value => value).ToArray());
            blockingFaces = new ReadOnlyCollection<Sv5SpaceBoundaryFace>((sourceBlockingFaces ??
                Array.Empty<Sv5SpaceBoundaryFace>()).Where(value => value != null)
                .GroupBy(value => value.StableToken, StringComparer.Ordinal).Select(value => value.First())
                .OrderBy(value => value).ToArray());
            if (contactIds.Count == 0 || blockingCells.Count + blockingFaces.Count == 0)
                throw new ArgumentException("A planned gate needs contacts and full-width blocking geometry.", nameof(sourceContactIds));
            SideAAnchor = sideAAnchor; SideBAnchor = sideBAnchor; Direction = direction;
            Flow = Sv5SpaceGraphAuthoringProfile.Require(flow, nameof(flow));
            Predicate = Sv5SpaceGraphAuthoringProfile.Require(predicate, nameof(predicate));
            TypedPredicate = typedPredicate ?? throw new ArgumentNullException(nameof(typedPredicate));
            SourceConnectionId = Sv5SpaceGraphAuthoringProfile.Require(sourceConnectionId, nameof(sourceConnectionId));
            SourceRouteId = Sv5SpaceGraphAuthoringProfile.Require(sourceRouteId, nameof(sourceRouteId));
            SourcePortId = Sv5SpaceGraphAuthoringProfile.Require(sourcePortId, nameof(sourcePortId));
            TargetPortId = Sv5SpaceGraphAuthoringProfile.Require(targetPortId, nameof(targetPortId));
            Crossing = crossing;
            SealedState = Sv5SpaceGraphAuthoringProfile.Require(sealedState, nameof(sealedState));
            OpenState = Sv5SpaceGraphAuthoringProfile.Require(openState, nameof(openState));
        }
        public string Id { get; }
        public string BoundaryId { get; }
        public IReadOnlyList<string> ContactIds => contactIds;
        public string ContactId => contactIds[0];
        public RmapSpecialWorldPoint World => SideAAnchor;
        public IReadOnlyList<RmapSpecialWorldPoint> BlockingCells => blockingCells;
        public IReadOnlyList<Sv5SpaceBoundaryFace> BlockingFaces => blockingFaces;
        public RmapSpecialWorldPoint SideAAnchor { get; }
        public RmapSpecialWorldPoint SideBAnchor { get; }
        public RmapWorldGraphDirection Direction { get; }
        public string Flow { get; }
        public string Predicate { get; }
        public Sv5SpaceGatePredicate TypedPredicate { get; }
        public string SourceConnectionId { get; }
        public string SourceRouteId { get; }
        public string SourcePortId { get; }
        public string TargetPortId { get; }
        public Sv5SpaceCrossingKind Crossing { get; }
        public string SealedState { get; }
        public string OpenState { get; }
        public bool PlannedBarrierVerified => blockingCells.Count + blockingFaces.Count > 0 &&
            !SideAAnchor.Equals(SideBAnchor) && (string.Equals(Flow, "ONE_WAY", StringComparison.Ordinal) ||
            string.Equals(Flow, "BIDIRECTIONAL", StringComparison.Ordinal)) && TypedPredicate != null &&
            !string.IsNullOrWhiteSpace(SourceConnectionId) && !string.IsNullOrWhiteSpace(SourceRouteId) &&
            !string.IsNullOrWhiteSpace(SourcePortId) && !string.IsNullOrWhiteSpace(TargetPortId);
        public bool RuntimeVerified => false;
        public int CompareTo(Sv5SpaceGate other) => other == null ? 1 : string.Compare(Id, other.Id, StringComparison.Ordinal);
    }

    public sealed class Sv5SpaceReservationCell : IComparable<Sv5SpaceReservationCell>
    {
        internal Sv5SpaceReservationCell(RmapSpecialWorldPoint world, Sv5SpaceReservationKind kind, string ownerId,
            string semantics)
        {
            World = world; Kind = kind; OwnerId = ownerId; Semantics = semantics;
        }
        public RmapSpecialWorldPoint World { get; }
        public Sv5SpaceReservationKind Kind { get; }
        public string OwnerId { get; }
        public string Semantics { get; }
        public int MicroChunkX => World.X / 12;
        public int MicroChunkY => World.Y / 8;
        public int PatternX => World.X / 4;
        public int PatternY => World.Y / 4;
        public int CompareTo(Sv5SpaceReservationCell other)
        {
            if (other == null) return 1;
            int value = World.CompareTo(other.World);
            if (value != 0) return value;
            value = Kind.CompareTo(other.Kind);
            return value != 0 ? value : string.Compare(OwnerId, other.OwnerId, StringComparison.Ordinal);
        }
    }

    public sealed class Sv5SpaceContactDecision : IComparable<Sv5SpaceContactDecision>
    {
        internal Sv5SpaceContactDecision(Sv5RouteContactPair source, string splitNodeId,
            Sv5SpaceCrossingKind crossing, string predicate, string boundaryId, bool coverageChecked,
            bool checkedState, string detail)
        {
            Source = source; SplitNodeId = splitNodeId; Crossing = crossing; Predicate = predicate;
            BoundaryId = boundaryId ?? string.Empty; CoverageChecked = coverageChecked;
            LogicalStateTransitionChecked = checkedState; Detail = detail;
        }
        public Sv5RouteContactPair Source { get; }
        public string SplitNodeId { get; }
        public Sv5SpaceCrossingKind Crossing { get; }
        public string Predicate { get; }
        public string BoundaryId { get; }
        public bool CoverageChecked { get; }
        public bool LogicalStateTransitionChecked { get; }
        public string Detail { get; }
        public string GeometryState => "PLANNED_RESERVATION";
        public string PlayerState => "PENDING";
        public int CompareTo(Sv5SpaceContactDecision other) => other == null ? 1 : Source.CompareTo(other.Source);
    }

    public sealed class Sv5SpaceProjectionOrderProof : IComparable<Sv5SpaceProjectionOrderProof>
    {
        internal Sv5SpaceProjectionOrderProof(RmapWorldGraphProof goalProof, int reachableStates,
            int transitions, int reverseReachableStates, IEnumerable<string> sourceDeadEnds)
        {
            GoalProof = goalProof;
            ReachableStates = reachableStates;
            Transitions = transitions;
            ReverseReachableStates = reverseReachableStates;
            DeadEnds = new ReadOnlyCollection<string>((sourceDeadEnds ?? Array.Empty<string>()).OrderBy(value => value,
                StringComparer.Ordinal).ToArray());
        }
        public RmapWorldGraphProof GoalProof { get; }
        public int ReachableStates { get; }
        public int Transitions { get; }
        public int ReverseReachableStates { get; }
        public IReadOnlyList<string> DeadEnds { get; }
        public bool Success => GoalProof != null && GoalProof.Success && ReachableStates == ReverseReachableStates && DeadEnds.Count == 0;
        public int CompareTo(Sv5SpaceProjectionOrderProof other) => other == null ? 1 :
            string.Compare(GoalProof.ProofId, other.GoalProof.ProofId, StringComparison.Ordinal);
    }

    public sealed class Sv5SpaceGraphPlan
    {
        internal Sv5SpaceGraphPlan(Sv5CoreReservationPlan core, ulong seed, Sv5SpaceGraphAuthoringProfile profile,
            IEnumerable<Sv5SpacePlace> sourcePlaces, IEnumerable<Sv5SpacePort> sourcePorts,
            IEnumerable<Sv5SpaceConnection> sourceConnections, IEnumerable<Sv5SpaceGate> sourceGates,
            IEnumerable<Sv5SpaceReservationCell> sourceReservations,
            IEnumerable<Sv5SpaceContactDecision> sourceContacts,
            IEnumerable<Sv5SpaceProjectionOrderProof> sourceProofs,
            IEnumerable<Sv5SpaceGateStateCheck> sourceGateStateChecks, IEnumerable<string> sourceDiagnostics)
        {
            Core = core ?? throw new ArgumentNullException(nameof(core));
            Seed = seed;
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            Places = Freeze(sourcePlaces);
            Ports = Freeze(sourcePorts);
            Connections = Freeze(sourceConnections);
            Gates = Freeze(sourceGates);
            Reservations = Freeze(sourceReservations);
            ContactDecisions = Freeze(sourceContacts);
            ProjectionProofs = Freeze(sourceProofs);
            GateStateChecks = Freeze(sourceGateStateChecks);
            Diagnostics = new ReadOnlyCollection<string>((sourceDiagnostics ?? Array.Empty<string>()).Where(value =>
                !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal).OrderBy(value => value,
                StringComparer.Ordinal).ToArray());
            GeometryStateReady = false;
            PlayerVerified = false;
            InfillPendingTileCount = 624 * 416 - Reservations.Select(value => value.World).Distinct().Count();
            PhysicalMovement = Sv5SpacePhysicalMovement.Analyze(Core, Connections, ContactDecisions, Gates);
            PhysicalProduct = PhysicalMovement.Product;
            Segments = Sv5SpacePhysicalProduct.BuildSegments(Connections, Gates);
            Digest = RmapWorldDefinition.Hash(string.Join("\n", CanonicalLines()));
        }

        public Sv5CoreReservationPlan Core { get; }
        public ulong Seed { get; }
        public Sv5SpaceGraphAuthoringProfile Profile { get; }
        public IReadOnlyList<Sv5SpacePlace> Places { get; }
        public IReadOnlyList<Sv5SpacePort> Ports { get; }
        public IReadOnlyList<Sv5SpaceConnection> Connections { get; }
        public IReadOnlyList<Sv5SpaceGate> Gates { get; }
        public IReadOnlyList<Sv5SpaceReservationCell> Reservations { get; }
        public IReadOnlyList<Sv5SpaceContactDecision> ContactDecisions { get; }
        public IReadOnlyList<Sv5SpaceProjectionOrderProof> ProjectionProofs { get; }
        public IReadOnlyList<Sv5SpaceGateStateCheck> GateStateChecks { get; }
        public Sv5SpacePhysicalMovementPlan PhysicalMovement { get; }
        public Sv5SpacePhysicalProductPlan PhysicalProduct { get; }
        public IReadOnlyList<Sv5SpaceSegment> Segments { get; }
        public IReadOnlyList<string> Diagnostics { get; }
        public bool GeometryStateReady { get; }
        public bool PlayerVerified { get; }
        public int InfillPendingTileCount { get; }
        public string Digest { get; }
        public bool Success => Diagnostics.Count == 0 && Places.Count(value => value.Kind == Sv5SpacePlaceKind.Core) == 8 &&
            Places.Count(value => value.Kind == Sv5SpacePlaceKind.Large) >= 4 &&
            Places.Count(value => value.Kind == Sv5SpacePlaceKind.Ordinary) >= 4 &&
            Core.Sites.Count == 8 && Core.CoreCells.Count == 2432 &&
            ContactDecisions.Count != 0 && ContactDecisions.All(value => value.CoverageChecked &&
                value.LogicalStateTransitionChecked) && Gates.All(value => value.PlannedBarrierVerified) &&
            GateStateChecks.Count >= Gates.Count * 2 && GateStateChecks.All(value => value.Success) &&
            PhysicalMovement.Success && PhysicalProduct.Success && ProjectionProofs.Count == 6 && ProjectionProofs.All(value => value.Success) &&
            InfillPendingTileCount > 0;

        private IEnumerable<string> CanonicalLines()
        {
            yield return "SV5_SPACE_GRAPH_PLAN_FIX03_V1";
            yield return Core.RouteSource.Definition.Digest;
            yield return Core.Digest;
            yield return Core.RouteSource.Graph.Digest;
            yield return Seed.ToString(CultureInfo.InvariantCulture);
            yield return Profile.Digest;
            foreach (Sv5SpacePlace value in Places) yield return "place|" + L(value.Id) + L(value.Family) +
                value.Kind + "|" + value.Bounds + "|" + L(value.CoreSiteId) + L(value.FutureOwner) + value.DistributionSector;
            foreach (Sv5SpacePort value in Ports) yield return "port|" + L(value.Id) + L(value.PlaceId) +
                Points(value.BoundaryCells) + "|" + value.Anchor + "|" + value.Direction + "|" + L(value.Flow) +
                L(value.Condition) + L(value.SourceAccessId) + L(value.SourceNodeId) + L(value.Status);
            foreach (Sv5SpaceConnection value in Connections) yield return "connection|" + L(value.Id) +
                value.Kind + "|" + L(value.FromPortId) + L(value.ToPortId) + L(value.FromPlaceId) + L(value.ToPlaceId) +
                value.Direction + "|" + L(value.Flow) + L(value.Condition) + L(value.SourceGraphEdgeId) +
                L(value.SelectionState) + Points(value.Centerline) + "|" + Points(value.Envelope) + "|" +
                Points(value.ApertureCells);
            foreach (Sv5SpaceGate value in Gates) yield return "gate|" + L(value.Id) + L(value.BoundaryId) +
                Strings(value.ContactIds) + "|" + Points(value.BlockingCells) + "|" +
                Strings(value.BlockingFaces.Select(item => item.StableToken)) + "|" + value.SideAAnchor + "|" +
                value.SideBAnchor + "|" + value.Direction + "|" + L(value.Flow) + L(value.Predicate) +
                L(value.TypedPredicate.StableToken) + L(value.SourceConnectionId) + L(value.SourceRouteId) +
                L(value.SourcePortId) + L(value.TargetPortId) + value.Crossing + "|" + L(value.SealedState) +
                L(value.OpenState);
            foreach (Sv5SpaceReservationCell value in Reservations) yield return "reservation|" + value.World + "|" +
                value.Kind + "|" + value.OwnerId + "|" + value.Semantics;
            foreach (Sv5SpaceContactDecision value in ContactDecisions) yield return "contact|" +
                L(value.Source.CanonicalPayload) + L(value.SplitNodeId) + value.Crossing + "|" + L(value.Predicate) +
                L(value.BoundaryId) + (value.CoverageChecked ? "1" : "0") + "|" +
                (value.LogicalStateTransitionChecked ? "1" : "0") + "|" + L(value.GeometryState) +
                L(value.PlayerState) + L(value.Detail);
            foreach (Sv5SpaceProjectionOrderProof value in ProjectionProofs) yield return "proof|" +
                value.GoalProof.ProofId + "|" + value.ReachableStates + "|" + value.Transitions + "|" +
                value.ReverseReachableStates + "|" + Strings(value.GoalProof.RequestedOrder.Select(item =>
                    item.ToString())) + "|" + Strings(value.GoalProof.Actions) + "|" + Strings(value.DeadEnds);
            foreach (Sv5SpaceGateStateCheck value in GateStateChecks) yield return "gate-state-check|" +
                L(value.Id) + L(value.GateId) + L(value.ConnectionId) + L(value.SourcePortId) +
                L(value.TargetPortId) + value.ResourceMask + "|" + (value.ForgeMade ? "1" : "0") +
                (value.SealOpen ? "1" : "0") + (value.BossComplete ? "1" : "0") +
                (value.ExpectedOpen ? "1" : "0") + (value.ActualOpen ? "1" : "0") +
                (value.SourceAnchorReachable ? "1" : "0") + (value.TargetPortReachable ? "1" : "0") +
                (value.SealedCutVerified ? "1" : "0") + (value.OpenPathVerified ? "1" : "0") + "|" +
                value.CheckedCells + "|" + value.CheckedFaces + "|" + L(value.Evidence);
            yield return "physical-movement|" + PhysicalMovement.SemanticDigest + "|" +
                (PhysicalMovement.Success ? "1" : "0");
            yield return "physical-product|" + PhysicalProduct.SemanticDigest + "|" +
                (PhysicalProduct.Success ? "1" : "0");
            foreach (Sv5SpaceSegment segment in Segments)
                yield return "segment|" + segment.Id + "|" + segment.ConnectionId + "|" + segment.RegionId + "|" +
                    segment.Kind + "|" + segment.Source + ">" + segment.Target + "|" + segment.GateId + "|" +
                    string.Join(";",segment.Centerline) + "|" + string.Join(";",segment.ApertureCells) + "|" +
                    (segment.Predicate == null ? "ACTIONLESS" : segment.Predicate.StableToken);
        }

        private static string L(string value)
        {
            string text = value ?? string.Empty;
            return text.Length.ToString(CultureInfo.InvariantCulture) + ":" + text + "|";
        }
        private static string Points(IEnumerable<RmapSpecialWorldPoint> values) => Strings((values ??
            Array.Empty<RmapSpecialWorldPoint>()).Select(value => value.ToString()));
        private static string Strings(IEnumerable<string> values) => string.Join(string.Empty, (values ??
            Array.Empty<string>()).Select(L));

        private static ReadOnlyCollection<T> Freeze<T>(IEnumerable<T> values) where T : IComparable<T> =>
            new ReadOnlyCollection<T>((values ?? Array.Empty<T>()).Where(value => value != null).OrderBy(value => value).ToArray());
    }
}
