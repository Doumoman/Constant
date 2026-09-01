using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Pipeline;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum WorldSectorSide
    {
        West,
        East,
        South,
        North,
    }

    public enum WorldEdgeOrientation
    {
        Horizontal,
        Vertical,
    }

    public enum WorldIntersectorFailureCode
    {
        MissingRequest,
        InvalidWorldPlan,
        InvalidDigest,
        InvalidTopology,
        EdgeCountMismatch,
        MissingSector,
        DuplicateSocketProjection,
        MissingCounterpartEndpoint,
        EndpointSideMismatch,
        AnchorOutOfBounds,
        AnchorNotOnSide,
        InvalidAperture,
        RouteFactMismatch,
        RouteSocketIncompatible,
        MandatoryRouteBlocked,
        ExternalSocketMismatch,
        DuplicateBoundaryBinding,
        BoundaryBindingUnknownEdge,
        BoundaryBindingMissing,
        BoundaryPairNotApproved,
        BoundaryProfileNotApproved,
        BoundaryOrientationMismatch,
        BoundaryWarningInsufficient,
        InvalidTraversalApron,
        EmptyEdgeSignature,
        FallbackCarveRequired,
        MutationClaim,
    }

    public readonly struct WorldIntersectorEdgeId :
        IEquatable<WorldIntersectorEdgeId>,
        IComparable<WorldIntersectorEdgeId>
    {
        public WorldIntersectorEdgeId(
            WorldSectorId firstSector,
            WorldSectorId secondSector,
            WorldEdgeOrientation orientation)
        {
            if (firstSector.CompareTo(secondSector) <= 0)
            {
                MinSector = firstSector;
                MaxSector = secondSector;
            }
            else
            {
                MinSector = secondSector;
                MaxSector = firstSector;
            }
            Orientation = orientation;
        }

        public WorldSectorId MinSector { get; }
        public WorldSectorId MaxSector { get; }
        public WorldEdgeOrientation Orientation { get; }

        public int CompareTo(WorldIntersectorEdgeId other)
        {
            var comparison = MinSector.CompareTo(other.MinSector);
            if (comparison != 0) return comparison;
            comparison = MaxSector.CompareTo(other.MaxSector);
            return comparison != 0 ? comparison : Orientation.CompareTo(other.Orientation);
        }

        public bool Equals(WorldIntersectorEdgeId other) => CompareTo(other) == 0;
        public override bool Equals(object obj) => obj is WorldIntersectorEdgeId other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                return ((MinSector.GetHashCode() * 397) ^ MaxSector.GetHashCode()) * 397 ^ (int)Orientation;
            }
        }

        public override string ToString() => string.Join("_", new[]
        {
            "EDGE", MinSector.Value.ToString("D3", CultureInfo.InvariantCulture),
            MaxSector.Value.ToString("D3", CultureInfo.InvariantCulture),
            Orientation == WorldEdgeOrientation.Horizontal ? "H" : "V",
        });

        public static bool operator ==(WorldIntersectorEdgeId left, WorldIntersectorEdgeId right) => left.Equals(right);
        public static bool operator !=(WorldIntersectorEdgeId left, WorldIntersectorEdgeId right) => !left.Equals(right);
    }

    public sealed class WorldSocketAnchor
    {
        public WorldSocketAnchor(int localX, int localY, int apertureSize)
        {
            LocalX = localX;
            LocalY = localY;
            ApertureSize = apertureSize;
        }

        public int LocalX { get; }
        public int LocalY { get; }
        public int ApertureSize { get; }
        public bool IsInBounds => LocalX >= 0 && LocalX < WorldPlanInput.SectorWidthTiles &&
                                  LocalY >= 0 && LocalY < WorldPlanInput.SectorHeightTiles;

        public bool IsOnSide(WorldSectorSide side)
        {
            switch (side)
            {
                case WorldSectorSide.West: return LocalX == 0;
                case WorldSectorSide.East: return LocalX == WorldPlanInput.SectorWidthTiles - 1;
                case WorldSectorSide.South: return LocalY == 0;
                case WorldSectorSide.North: return LocalY == WorldPlanInput.SectorHeightTiles - 1;
                default: return false;
            }
        }
    }

    public sealed class WorldTraversalApron
    {
        public WorldTraversalApron(int minX, int minY, int width, int height)
        {
            MinX = minX;
            MinY = minY;
            Width = width;
            Height = height;
        }

        public int MinX { get; }
        public int MinY { get; }
        public int Width { get; }
        public int Height { get; }
        public int CellCount => Width > 0 && Height > 0 ? Width * Height : 0;
        public bool IsInBounds => MinX >= 0 && MinY >= 0 && Width > 0 && Height > 0 &&
                                  MinX + Width <= WorldPlanInput.SectorWidthTiles &&
                                  MinY + Height <= WorldPlanInput.SectorHeightTiles;

        public bool Contains(WorldSocketAnchor anchor)
        {
            return anchor != null && anchor.LocalX >= MinX && anchor.LocalX < MinX + Width &&
                   anchor.LocalY >= MinY && anchor.LocalY < MinY + Height;
        }
    }

    public sealed class WorldSocketProjection : IComparable<WorldSocketProjection>
    {
        public WorldSocketProjection(
            WorldSectorId sectorId,
            WorldSectorSide side,
            WorldSocketAnchor anchor,
            bool explicitSocketEvidence,
            bool requiresMandatoryContinuity,
            bool requiresBoundaryBinding,
            string sourceOwner)
        {
            SectorId = sectorId;
            Side = side;
            Anchor = anchor;
            ExplicitSocketEvidence = explicitSocketEvidence;
            RequiresMandatoryContinuity = requiresMandatoryContinuity;
            RequiresBoundaryBinding = requiresBoundaryBinding;
            SourceOwner = sourceOwner ?? string.Empty;
        }

        public WorldSectorId SectorId { get; }
        public WorldSectorSide Side { get; }
        public WorldSocketAnchor Anchor { get; }
        public bool ExplicitSocketEvidence { get; }
        public bool RequiresMandatoryContinuity { get; }
        public bool RequiresBoundaryBinding { get; }
        public string SourceOwner { get; }

        public int CompareTo(WorldSocketProjection other)
        {
            if (other == null) return -1;
            var comparison = SectorId.CompareTo(other.SectorId);
            return comparison != 0 ? comparison : Side.CompareTo(other.Side);
        }
    }

    public sealed class WorldBoundaryBinding : IComparable<WorldBoundaryBinding>
    {
        private readonly ReadOnlyCollection<string> warningModalities;

        public WorldBoundaryBinding(
            WorldIntersectorEdgeId edgeId,
            string pairId,
            string profileId,
            string candidateId,
            IEnumerable<string> sourceWarningModalities,
            string sourceOwner)
        {
            EdgeId = edgeId;
            PairId = pairId ?? string.Empty;
            ProfileId = profileId ?? string.Empty;
            CandidateId = candidateId ?? string.Empty;
            warningModalities = new ReadOnlyCollection<string>((sourceWarningModalities ?? Array.Empty<string>())
                .Select(value => value ?? string.Empty)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray());
            SourceOwner = sourceOwner ?? string.Empty;
        }

        public WorldIntersectorEdgeId EdgeId { get; }
        public string PairId { get; }
        public string ProfileId { get; }
        public string CandidateId { get; }
        public IReadOnlyList<string> WarningModalities => warningModalities;
        public string SourceOwner { get; }

        public int CompareTo(WorldBoundaryBinding other) =>
            other == null ? -1 : EdgeId.CompareTo(other.EdgeId);
    }

    public sealed class WorldEdgeEndpoint
    {
        public WorldEdgeEndpoint(
            WorldSectorId sectorId,
            WorldSectorSide side,
            WorldSocketAnchor anchor,
            WorldTraversalApron apron,
            int routeType,
            AccessClass accessClass,
            bool explicitSocketEvidence,
            bool requiresMandatoryContinuity,
            bool isOpen,
            string sourceOwner)
        {
            SectorId = sectorId;
            Side = side;
            Anchor = anchor;
            Apron = apron;
            RouteType = routeType;
            AccessClass = accessClass;
            ExplicitSocketEvidence = explicitSocketEvidence;
            RequiresMandatoryContinuity = requiresMandatoryContinuity;
            IsOpen = isOpen;
            SourceOwner = sourceOwner ?? string.Empty;
        }

        public WorldSectorId SectorId { get; }
        public WorldSectorSide Side { get; }
        public WorldSocketAnchor Anchor { get; }
        public WorldTraversalApron Apron { get; }
        public int RouteType { get; }
        public AccessClass AccessClass { get; }
        public bool ExplicitSocketEvidence { get; }
        public bool RequiresMandatoryContinuity { get; }
        public bool IsOpen { get; }
        public string SourceOwner { get; }
    }

    public sealed class WorldEdgeRouteSignature
    {
        public WorldEdgeRouteSignature(
            bool compatible,
            bool mandatoryRoute,
            bool externalSocket,
            string canonicalDigest)
        {
            Compatible = compatible;
            MandatoryRoute = mandatoryRoute;
            ExternalSocket = externalSocket;
            CanonicalDigest = canonicalDigest ?? string.Empty;
        }

        public bool Compatible { get; }
        public bool MandatoryRoute { get; }
        public bool ExternalSocket { get; }
        public string CanonicalDigest { get; }
    }

    public sealed class WorldIntersectorEdge : IComparable<WorldIntersectorEdge>
    {
        private readonly ReadOnlyCollection<WorldEdgeEndpoint> endpoints;

        public WorldIntersectorEdge(
            WorldIntersectorEdgeId id,
            IEnumerable<WorldEdgeEndpoint> sourceEndpoints,
            WorldBoundaryBinding boundary,
            WorldEdgeRouteSignature routeSignature)
        {
            Id = id;
            endpoints = new ReadOnlyCollection<WorldEdgeEndpoint>((sourceEndpoints ?? Array.Empty<WorldEdgeEndpoint>())
                .Where(value => value != null)
                .OrderBy(value => value.SectorId)
                .ThenBy(value => value.Side)
                .ToArray());
            Boundary = boundary;
            RouteSignature = routeSignature;
            CanonicalDigest = WorldIntersectorDigest.ComputeEdge(this);
        }

        public WorldIntersectorEdgeId Id { get; }
        public WorldEdgeOrientation Orientation => Id.Orientation;
        public IReadOnlyList<WorldEdgeEndpoint> Endpoints => endpoints;
        public WorldBoundaryBinding Boundary { get; }
        public bool IsBoundary => Boundary != null;
        public WorldEdgeRouteSignature RouteSignature { get; }
        public string CanonicalDigest { get; }
        public int CompareTo(WorldIntersectorEdge other) => other == null ? -1 : Id.CompareTo(other.Id);
    }

    public sealed class WorldIntersectorBuildRequest
    {
        private readonly ReadOnlyCollection<WorldSocketProjection> socketProjections;
        private readonly ReadOnlyCollection<WorldBoundaryBinding> boundaryBindings;

        public WorldIntersectorBuildRequest(
            WorldPlanInput worldPlan,
            WorldSolveOrderResult solveOrder,
            IEnumerable<WorldSocketProjection> sourceSocketProjections,
            IEnumerable<WorldBoundaryBinding> sourceBoundaryBindings,
            string map14HandoffDigest,
            string boundaryAuthorityDigest,
            string publicationLabel,
            int newRngDrawCount = 0,
            int fallbackCarveCount = 0,
            int generatedFileWriteCount = 0,
            int tilemapMutationCount = 0,
            int sceneMutationCount = 0,
            int prefabMutationCount = 0,
            int gameObjectMutationCount = 0,
            int gameplaySpawnCount = 0,
            int sectorPlannerMutationCount = 0,
            int worldPlanMutationCount = 0)
        {
            WorldPlan = worldPlan;
            SolveOrder = solveOrder;
            socketProjections = new ReadOnlyCollection<WorldSocketProjection>(
                (sourceSocketProjections ?? Array.Empty<WorldSocketProjection>())
                .Where(value => value != null)
                .OrderBy(value => value)
                .ToArray());
            boundaryBindings = new ReadOnlyCollection<WorldBoundaryBinding>(
                (sourceBoundaryBindings ?? Array.Empty<WorldBoundaryBinding>())
                .Where(value => value != null)
                .OrderBy(value => value)
                .ToArray());
            Map14HandoffDigest = map14HandoffDigest ?? string.Empty;
            BoundaryAuthorityDigest = boundaryAuthorityDigest ?? string.Empty;
            PublicationLabel = publicationLabel ?? string.Empty;
            NewRngDrawCount = newRngDrawCount;
            FallbackCarveCount = fallbackCarveCount;
            GeneratedFileWriteCount = generatedFileWriteCount;
            TilemapMutationCount = tilemapMutationCount;
            SceneMutationCount = sceneMutationCount;
            PrefabMutationCount = prefabMutationCount;
            GameObjectMutationCount = gameObjectMutationCount;
            GameplaySpawnCount = gameplaySpawnCount;
            SectorPlannerMutationCount = sectorPlannerMutationCount;
            WorldPlanMutationCount = worldPlanMutationCount;
            CanonicalDigest = WorldIntersectorDigest.ComputeInput(this);
        }

        public WorldPlanInput WorldPlan { get; }
        public WorldSolveOrderResult SolveOrder { get; }
        public IReadOnlyList<WorldSocketProjection> SocketProjections => socketProjections;
        public IReadOnlyList<WorldBoundaryBinding> BoundaryBindings => boundaryBindings;
        public string Map14HandoffDigest { get; }
        public string BoundaryAuthorityDigest { get; }
        public string PublicationLabel { get; }
        public int NewRngDrawCount { get; }
        public int FallbackCarveCount { get; }
        public int GeneratedFileWriteCount { get; }
        public int TilemapMutationCount { get; }
        public int SceneMutationCount { get; }
        public int PrefabMutationCount { get; }
        public int GameObjectMutationCount { get; }
        public int GameplaySpawnCount { get; }
        public int SectorPlannerMutationCount { get; }
        public int WorldPlanMutationCount { get; }
        public string CanonicalDigest { get; }
    }

    public sealed class WorldIntersectorEdgePlan
    {
        private readonly ReadOnlyCollection<WorldIntersectorEdge> edges;

        internal WorldIntersectorEdgePlan(
            WorldIntersectorBuildRequest request,
            IEnumerable<WorldIntersectorEdge> sourceEdges,
            string outputDigest)
        {
            Request = request;
            edges = new ReadOnlyCollection<WorldIntersectorEdge>(sourceEdges.OrderBy(value => value).ToArray());
            OutputDigest = outputDigest ?? string.Empty;
        }

        public const int HorizontalEdgeCount = 156;
        public const int VerticalEdgeCount = 156;
        public const int InternalEdgeCount = 312;
        public const int EndpointCount = 624;
        public const string DownstreamOwner =
            "MAP15_03_IMPLEMENT_MULTI_SECTOR_SPECIAL_AND_CLUSTER_POLICY";
        public const bool OpensDownstreamTask = false;

        public WorldIntersectorBuildRequest Request { get; }
        public IReadOnlyList<WorldIntersectorEdge> Edges => edges;
        public string InputDigest => Request.CanonicalDigest;
        public string OutputDigest { get; }
        public int HorizontalCount => edges.Count(value => value.Orientation == WorldEdgeOrientation.Horizontal);
        public int VerticalCount => edges.Count(value => value.Orientation == WorldEdgeOrientation.Vertical);
        public int BoundaryCount => edges.Count(value => value.IsBoundary);
        public int EndpointActualCount => edges.Sum(value => value.Endpoints.Count);
        public int NewRngDrawCount => Request.NewRngDrawCount;
        public int FallbackCarveCount => Request.FallbackCarveCount;
        public int GeneratedFileWriteCount => Request.GeneratedFileWriteCount;
        public int TilemapMutationCount => Request.TilemapMutationCount;
        public int SceneMutationCount => Request.SceneMutationCount;
        public int PrefabMutationCount => Request.PrefabMutationCount;
        public int GameObjectMutationCount => Request.GameObjectMutationCount;
        public int GameplaySpawnCount => Request.GameplaySpawnCount;
        public int SectorPlannerMutationCount => Request.SectorPlannerMutationCount;
        public int WorldPlanMutationCount => Request.WorldPlanMutationCount;
    }

    public sealed class WorldIntersectorFailure :
        IComparable<WorldIntersectorFailure>,
        IEquatable<WorldIntersectorFailure>
    {
        public WorldIntersectorFailure(WorldIntersectorFailureCode code, string subject, string reason)
        {
            Code = code;
            Subject = subject ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public WorldIntersectorFailureCode Code { get; }
        public string Subject { get; }
        public string Reason { get; }

        public int CompareTo(WorldIntersectorFailure other)
        {
            if (other == null) return -1;
            var comparison = Code.CompareTo(other.Code);
            if (comparison != 0) return comparison;
            comparison = string.Compare(Subject, other.Subject, StringComparison.Ordinal);
            return comparison != 0 ? comparison : string.Compare(Reason, other.Reason, StringComparison.Ordinal);
        }

        public bool Equals(WorldIntersectorFailure other) => other != null && CompareTo(other) == 0;
        public override bool Equals(object obj) => Equals(obj as WorldIntersectorFailure);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(ToString());
        public override string ToString() => Code + "|" + Subject + "|" + Reason;
    }

    public sealed class WorldIntersectorBuildResult
    {
        private readonly ReadOnlyCollection<WorldIntersectorFailure> failures;

        private WorldIntersectorBuildResult(
            WorldIntersectorEdgePlan plan,
            IEnumerable<WorldIntersectorFailure> sourceFailures)
        {
            Plan = plan;
            failures = new ReadOnlyCollection<WorldIntersectorFailure>((sourceFailures ?? Array.Empty<WorldIntersectorFailure>())
                .Distinct()
                .OrderBy(value => value)
                .ToArray());
        }

        public bool Success => Plan != null && failures.Count == 0 &&
                               Plan.Edges.Count == WorldIntersectorEdgePlan.InternalEdgeCount;
        public WorldIntersectorEdgePlan Plan { get; }
        public IReadOnlyList<WorldIntersectorFailure> Failures => failures;
        public string InputDigest => Plan == null ? string.Empty : Plan.InputDigest;
        public string OutputDigest => Plan == null ? string.Empty : Plan.OutputDigest;

        internal static WorldIntersectorBuildResult Pass(WorldIntersectorEdgePlan plan) =>
            new WorldIntersectorBuildResult(plan, Array.Empty<WorldIntersectorFailure>());

        internal static WorldIntersectorBuildResult Fail(IEnumerable<WorldIntersectorFailure> failures) =>
            new WorldIntersectorBuildResult(null, failures);
    }

    public static class WorldIntersectorDigest
    {
        public static string ComputeInput(WorldIntersectorBuildRequest request)
        {
            if (request == null) return string.Empty;
            var lines = new List<string>
            {
                "WORLD_PLAN_INPUT|" + (request.WorldPlan == null ? string.Empty : request.WorldPlan.CanonicalDigest),
                "WORLD_SOLVE_OUTPUT|" + (request.SolveOrder == null ? string.Empty : request.SolveOrder.OutputDigest),
                "MAP14|" + Token(request.Map14HandoffDigest),
                "BOUNDARY_AUTHORITY|" + Token(request.BoundaryAuthorityDigest),
                "PUBLICATION|" + Token(request.PublicationLabel),
                string.Join("|", new[]
                {
                    "MUTATION", Number(request.NewRngDrawCount), Number(request.FallbackCarveCount),
                    Number(request.GeneratedFileWriteCount), Number(request.TilemapMutationCount),
                    Number(request.SceneMutationCount), Number(request.PrefabMutationCount),
                    Number(request.GameObjectMutationCount), Number(request.GameplaySpawnCount),
                    Number(request.SectorPlannerMutationCount), Number(request.WorldPlanMutationCount),
                }),
            };
            lines.AddRange(request.SocketProjections.Select(Projection));
            lines.AddRange(request.BoundaryBindings.Select(Boundary));
            return HashCanonicalText(string.Join("\n", lines));
        }

        public static string ComputeEdge(WorldIntersectorEdge edge)
        {
            if (edge == null) return string.Empty;
            var lines = new List<string> { "EDGE|" + edge.Id };
            lines.AddRange(edge.Endpoints.Select(Endpoint));
            lines.Add(edge.Boundary == null ? "BOUNDARY|NONE" : Boundary(edge.Boundary));
            lines.Add("ROUTE|" + Bool(edge.RouteSignature != null && edge.RouteSignature.Compatible) + "|" +
                      Bool(edge.RouteSignature != null && edge.RouteSignature.MandatoryRoute) + "|" +
                      Bool(edge.RouteSignature != null && edge.RouteSignature.ExternalSocket) + "|" +
                      (edge.RouteSignature == null ? string.Empty : edge.RouteSignature.CanonicalDigest));
            return HashCanonicalText(string.Join("\n", lines));
        }

        public static string ComputeOutput(
            WorldIntersectorBuildRequest request,
            IEnumerable<WorldIntersectorEdge> sourceEdges)
        {
            var lines = new List<string> { "INPUT|" + (request == null ? string.Empty : request.CanonicalDigest) };
            lines.AddRange((sourceEdges ?? Array.Empty<WorldIntersectorEdge>())
                .OrderBy(value => value)
                .Select(value => value.Id + "|" + value.CanonicalDigest));
            return HashCanonicalText(string.Join("\n", lines));
        }

        public static string ComputeRouteSignature(
            WorldIntersectorEdgeId edgeId,
            IEnumerable<WorldEdgeEndpoint> endpoints,
            bool compatible,
            bool mandatory,
            bool external)
        {
            var lines = new List<string>
            {
                "ROUTE|" + edgeId + "|" + Bool(compatible) + "|" + Bool(mandatory) + "|" + Bool(external),
            };
            lines.AddRange((endpoints ?? Array.Empty<WorldEdgeEndpoint>())
                .OrderBy(value => value.SectorId)
                .ThenBy(value => value.Side)
                .Select(Endpoint));
            return HashCanonicalText(string.Join("\n", lines));
        }

        public static string HashCanonicalText(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                var result = new StringBuilder(bytes.Length * 2);
                foreach (var item in bytes) result.Append(item.ToString("x2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }

        private static string Projection(WorldSocketProjection value)
        {
            return string.Join("|", new[]
            {
                "PROJECTION", Number(value.SectorId.Value), value.Side.ToString(), Anchor(value.Anchor),
                Bool(value.ExplicitSocketEvidence), Bool(value.RequiresMandatoryContinuity),
                Bool(value.RequiresBoundaryBinding), Token(value.SourceOwner),
            });
        }

        private static string Endpoint(WorldEdgeEndpoint value)
        {
            return string.Join("|", new[]
            {
                "ENDPOINT", Number(value.SectorId.Value), value.Side.ToString(), Anchor(value.Anchor),
                Apron(value.Apron), Number(value.RouteType), value.AccessClass.ToString(),
                Bool(value.ExplicitSocketEvidence), Bool(value.RequiresMandatoryContinuity),
                Bool(value.IsOpen), Token(value.SourceOwner),
            });
        }

        private static string Boundary(WorldBoundaryBinding value)
        {
            return string.Join("|", new[]
            {
                "BOUNDARY", value.EdgeId.ToString(), Token(value.PairId), Token(value.ProfileId),
                Token(value.CandidateId), string.Join(",", value.WarningModalities), Token(value.SourceOwner),
            });
        }

        private static string Anchor(WorldSocketAnchor value) => value == null
            ? "null"
            : Number(value.LocalX) + "," + Number(value.LocalY) + "," + Number(value.ApertureSize);

        private static string Apron(WorldTraversalApron value) => value == null
            ? "null"
            : Number(value.MinX) + "," + Number(value.MinY) + "," + Number(value.Width) + "," + Number(value.Height);

        private static string Token(string value)
        {
            var normalized = value ?? string.Empty;
            return normalized.Length.ToString(CultureInfo.InvariantCulture) + ":" + normalized;
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Bool(bool value) => value ? "1" : "0";
    }
}
