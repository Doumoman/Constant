using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.WorldGeneration.SpecialRegions
{
    public readonly struct SpecialRegionTileCoordinate :
        IEquatable<SpecialRegionTileCoordinate>, IComparable<SpecialRegionTileCoordinate>
    {
        public SpecialRegionTileCoordinate(SectorCoord worldSector, LocalTileCoord localTile)
        {
            WorldSector = worldSector;
            LocalTile = localTile;
        }

        public SectorCoord WorldSector { get; }
        public LocalTileCoord LocalTile { get; }

        public int CompareTo(SpecialRegionTileCoordinate other)
        {
            var value = WorldSector.Y.CompareTo(other.WorldSector.Y);
            if (value != 0) return value;
            value = WorldSector.X.CompareTo(other.WorldSector.X);
            if (value != 0) return value;
            value = LocalTile.Y.CompareTo(other.LocalTile.Y);
            return value != 0 ? value : LocalTile.X.CompareTo(other.LocalTile.X);
        }

        public bool Equals(SpecialRegionTileCoordinate other)
            => WorldSector == other.WorldSector && LocalTile == other.LocalTile;

        public override bool Equals(object obj)
            => obj is SpecialRegionTileCoordinate other && Equals(other);

        public override int GetHashCode()
        {
            unchecked { return (WorldSector.GetHashCode() * 397) ^ LocalTile.GetHashCode(); }
        }

        public override string ToString()
            => WorldSector.X + "," + WorldSector.Y + "/" + LocalTile.X + "," + LocalTile.Y;

        public static bool operator ==(SpecialRegionTileCoordinate left, SpecialRegionTileCoordinate right)
            => left.Equals(right);

        public static bool operator !=(SpecialRegionTileCoordinate left, SpecialRegionTileCoordinate right)
            => !left.Equals(right);
    }

    public sealed class SpecialRegionEntryApron
    {
        private readonly ReadOnlyCollection<SpecialRegionTileCoordinate> cells;

        public SpecialRegionEntryApron(
            string portId,
            SectorCoord worldSector,
            LocalTileCoord minimum,
            int width,
            int height,
            IEnumerable<SpecialRegionTileCoordinate> cells)
        {
            PortId = portId ?? string.Empty;
            WorldSector = worldSector;
            Minimum = minimum;
            Width = width;
            Height = height;
            this.cells = Freeze(cells);
        }

        public string PortId { get; }
        public SectorCoord WorldSector { get; }
        public LocalTileCoord Minimum { get; }
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<SpecialRegionTileCoordinate> Cells => cells;

        private static ReadOnlyCollection<SpecialRegionTileCoordinate> Freeze(
            IEnumerable<SpecialRegionTileCoordinate> source)
            => new ReadOnlyCollection<SpecialRegionTileCoordinate>(
                (source ?? Array.Empty<SpecialRegionTileCoordinate>()).OrderBy(value => value).ToArray());
    }

    public enum SpecialRegionQuietChunkRole
    {
        Before = 1,
        After = 2,
    }

    public sealed class SpecialRegionQuietChunkPlacement
    {
        public SpecialRegionQuietChunkPlacement(
            ClusterChunkCoord sourceChunk,
            SectorCoord worldSector,
            ClusterChunkCoord sectorLocalChunk)
        {
            SourceChunk = sourceChunk;
            WorldSector = worldSector;
            SectorLocalChunk = sectorLocalChunk;
        }

        public ClusterChunkCoord SourceChunk { get; }
        public SectorCoord WorldSector { get; }
        public ClusterChunkCoord SectorLocalChunk { get; }
    }

    public sealed class SpecialRegionQuietBufferPlacement
    {
        private readonly ReadOnlyCollection<SpecialRegionQuietChunkPlacement> chunks;

        public SpecialRegionQuietBufferPlacement(
            string placementId,
            SpecialRegionQuietChunkRole role,
            TerrainClusterQuietBufferCandidate candidate,
            IEnumerable<SpecialRegionQuietChunkPlacement> chunks)
        {
            PlacementId = placementId ?? string.Empty;
            Role = role;
            Candidate = candidate;
            this.chunks = new ReadOnlyCollection<SpecialRegionQuietChunkPlacement>(
                (chunks ?? Array.Empty<SpecialRegionQuietChunkPlacement>())
                .Where(value => value != null)
                .OrderBy(value => value.SourceChunk)
                .ThenBy(value => value.WorldSector.Y)
                .ThenBy(value => value.WorldSector.X)
                .ThenBy(value => value.SectorLocalChunk)
                .ToArray());
        }

        public string PlacementId { get; }
        public SpecialRegionQuietChunkRole Role { get; }
        public TerrainClusterQuietBufferCandidate Candidate { get; }
        public IReadOnlyList<SpecialRegionQuietChunkPlacement> Chunks => chunks;
    }

    public sealed class SpecialRegionQuietChunkBinding
    {
        internal SpecialRegionQuietChunkBinding(
            string placementId,
            SpecialRegionQuietChunkRole role,
            string quietBufferId,
            string candidateDigest,
            ClusterChunkCoord sourceChunk,
            SectorCoord worldSector,
            ClusterChunkCoord sectorLocalChunk,
            TerrainClusterQuietBufferChunkEvidence evidence)
        {
            PlacementId = placementId;
            Role = role;
            QuietBufferId = quietBufferId;
            CandidateDigest = candidateDigest;
            SourceChunk = sourceChunk;
            WorldSector = worldSector;
            SectorLocalChunk = sectorLocalChunk;
            MinimumTile = new LocalTileCoord(
                sectorLocalChunk.X * WorldGenConstants.MicroChunkWidthTiles,
                sectorLocalChunk.Y * WorldGenConstants.MicroChunkHeightTiles);
            MaximumTile = new LocalTileCoord(
                MinimumTile.X + WorldGenConstants.MicroChunkWidthTiles - 1,
                MinimumTile.Y + WorldGenConstants.MicroChunkHeightTiles - 1);
            SolidCount = evidence == null ? 0 : evidence.SolidCount;
            AirCount = evidence == null ? 0 : evidence.AirCount;
            BaselineCoordinateCount = evidence == null ? 0 : evidence.BaselineCoordinateCount;
        }

        public string PlacementId { get; }
        public SpecialRegionQuietChunkRole Role { get; }
        public string QuietBufferId { get; }
        public string CandidateDigest { get; }
        public ClusterChunkCoord SourceChunk { get; }
        public SectorCoord WorldSector { get; }
        public ClusterChunkCoord SectorLocalChunk { get; }
        public LocalTileCoord MinimumTile { get; }
        public LocalTileCoord MaximumTile { get; }
        public int SolidCount { get; }
        public int AirCount { get; }
        public int BaselineCoordinateCount { get; }
    }

    public sealed class SpecialRegionEntryPortBinding
    {
        private readonly ReadOnlyCollection<int> routeTypes;

        internal SpecialRegionEntryPortBinding(
            SpecialRegionSitePortBinding source,
            SiteEntryAnchor anchor,
            IEnumerable<int> routeTypes)
        {
            PortId = source.PortId;
            SlotId = source.SlotId;
            Kind = source.Kind;
            AccessClass = source.AccessClass;
            EntrySocketId = source.EntrySocketId;
            Placed = source.Placed;
            AnchorExteriorSector = source.AnchorExteriorSector;
            AnchorRequired = anchor.Required;
            ReturnPathRequired = anchor.ReturnPathRequired;
            this.routeTypes = new ReadOnlyCollection<int>(
                (routeTypes ?? Array.Empty<int>()).Distinct().OrderBy(value => value).ToArray());
        }

        public string PortId { get; }
        public SpecialRegionSlotId SlotId { get; }
        public SpecialRegionSlotKind Kind { get; }
        public AccessClass AccessClass { get; }
        public string EntrySocketId { get; }
        public SpecialRegionPlacedCoordinate Placed { get; }
        public SectorCoord AnchorExteriorSector { get; }
        public bool AnchorRequired { get; }
        public bool ReturnPathRequired { get; }
        public IReadOnlyList<int> RouteTypes => routeTypes;
    }

    public sealed class SpecialRegionBidirectionalWitness
    {
        private readonly ReadOnlyCollection<string> forwardSegments;
        private readonly ReadOnlyCollection<string> returnSegments;
        private readonly ReadOnlyCollection<int> entryRouteTypes;
        private readonly ReadOnlyCollection<int> returnRouteTypes;

        internal SpecialRegionBidirectionalWitness(
            IEnumerable<string> forwardSegments,
            IEnumerable<string> returnSegments,
            IEnumerable<int> entryRouteTypes,
            IEnumerable<int> returnRouteTypes)
        {
            this.forwardSegments = Freeze(forwardSegments);
            this.returnSegments = Freeze(returnSegments);
            this.entryRouteTypes = Freeze(entryRouteTypes);
            this.returnRouteTypes = Freeze(returnRouteTypes);
        }

        public IReadOnlyList<string> ForwardSegments => forwardSegments;
        public IReadOnlyList<string> ReturnSegments => returnSegments;
        public IReadOnlyList<int> EntryRouteTypes => entryRouteTypes;
        public IReadOnlyList<int> ReturnRouteTypes => returnRouteTypes;
        public bool HasBeforeQuietToInteriorPath => forwardSegments.Count == 4;
        public bool HasInteriorToAfterQuietPath => returnSegments.Count == 4;
        public bool IsBidirectional => HasBeforeQuietToInteriorPath && HasInteriorToAfterQuietPath;
        public int SyntheticEdgeCount => 0;
        public int TeleportCount => 0;
        public int CarveCount => 0;
        public int ToolRequirementCount => 0;
        public int OneWayEdgeCount => 0;
        public bool ClaimsRuntimePhysics => false;

        private static ReadOnlyCollection<string> Freeze(IEnumerable<string> source)
            => new ReadOnlyCollection<string>((source ?? Array.Empty<string>()).Select(value => value ?? string.Empty).ToArray());

        private static ReadOnlyCollection<int> Freeze(IEnumerable<int> source)
            => new ReadOnlyCollection<int>((source ?? Array.Empty<int>()).Distinct().OrderBy(value => value).ToArray());
    }

    public sealed class SpecialRegionEntryBufferPlan
    {
        private readonly ReadOnlyCollection<SpecialRegionEntryApron> aprons;
        private readonly ReadOnlyCollection<SpecialRegionQuietChunkBinding> quietChunks;

        internal SpecialRegionEntryBufferPlan(
            SpecialRegionSiteBridge bridge,
            SpecialRegionEntryPortBinding entryPort,
            SpecialRegionEntryPortBinding returnPort,
            IEnumerable<SpecialRegionEntryApron> aprons,
            IEnumerable<SpecialRegionQuietChunkBinding> quietChunks,
            SpecialRegionBidirectionalWitness witness)
        {
            RegionId = bridge.RegionId;
            ReservationId = bridge.ReservationId;
            BridgeDigest = bridge.CanonicalDigest;
            EntryPort = entryPort;
            ReturnPort = returnPort;
            this.aprons = new ReadOnlyCollection<SpecialRegionEntryApron>(
                aprons.OrderBy(value => value.PortId, StringComparer.Ordinal).ToArray());
            this.quietChunks = new ReadOnlyCollection<SpecialRegionQuietChunkBinding>(
                quietChunks.OrderBy(value => value.Role).ThenBy(value => value.SourceChunk).ToArray());
            Witness = witness;
        }

        public SpecialRegionId RegionId { get; }
        public SiteReservationId ReservationId { get; }
        public string BridgeDigest { get; }
        public SpecialRegionEntryPortBinding EntryPort { get; }
        public SpecialRegionEntryPortBinding ReturnPort { get; }
        public IReadOnlyList<SpecialRegionEntryApron> Aprons => aprons;
        public IReadOnlyList<SpecialRegionQuietChunkBinding> QuietChunks => quietChunks;
        public SpecialRegionBidirectionalWitness Witness { get; }
        public int SelectedCandidateCount => 0;
        public int PlacementWriteCount => 0;
        public string CanonicalDigest { get; internal set; }
    }

    public sealed class SpecialRegionEntryBufferCompileRequest
    {
        public SpecialRegionEntryBufferCompileRequest(
            SpecialRegionSiteBridge bridge,
            string expectedBridgeDigest,
            string entryPortId,
            SiteEntryAnchor entryAnchor,
            SpecialRegionEntryApron entryApron,
            string returnPortId,
            SiteEntryAnchor returnAnchor,
            SpecialRegionEntryApron returnApron,
            SpecialRegionQuietBufferPlacement beforePlacement,
            SpecialRegionQuietBufferPlacement afterPlacement)
        {
            Bridge = bridge;
            ExpectedBridgeDigest = expectedBridgeDigest ?? string.Empty;
            EntryPortId = entryPortId ?? string.Empty;
            EntryAnchor = entryAnchor;
            EntryApron = entryApron;
            ReturnPortId = returnPortId ?? string.Empty;
            ReturnAnchor = returnAnchor;
            ReturnApron = returnApron;
            BeforePlacement = beforePlacement;
            AfterPlacement = afterPlacement;
        }

        public SpecialRegionSiteBridge Bridge { get; }
        public string ExpectedBridgeDigest { get; }
        public string EntryPortId { get; }
        public SiteEntryAnchor EntryAnchor { get; }
        public SpecialRegionEntryApron EntryApron { get; }
        public string ReturnPortId { get; }
        public SiteEntryAnchor ReturnAnchor { get; }
        public SpecialRegionEntryApron ReturnApron { get; }
        public SpecialRegionQuietBufferPlacement BeforePlacement { get; }
        public SpecialRegionQuietBufferPlacement AfterPlacement { get; }
    }

    public enum SpecialRegionEntryBufferErrorCode
    {
        MissingInput = 1,
        BridgeDigestMismatch = 2,
        InvalidPortPair = 3,
        InvalidMandatoryAccess = 4,
        InvalidApron = 5,
        ApronBlocked = 6,
        InvalidQuietCandidate = 7,
        QuietChunkMismatch = 8,
        BufferOverlap = 9,
        MissingBidirectionalWitness = 10,
        InvalidOwner = 11,
        InvalidClaim = 12,
        HardProtectedCollision = 13,
        AmbiguousSamePriority = 14,
        NonCanonicalPublication = 15,
    }

    public sealed class SpecialRegionEntryBufferError :
        IEquatable<SpecialRegionEntryBufferError>, IComparable<SpecialRegionEntryBufferError>
    {
        public SpecialRegionEntryBufferError(
            SpecialRegionEntryBufferErrorCode code, string path, string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public SpecialRegionEntryBufferErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(SpecialRegionEntryBufferError other)
        {
            if (other == null) return -1;
            var value = Code.CompareTo(other.Code);
            if (value != 0) return value;
            value = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return value != 0 ? value : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(SpecialRegionEntryBufferError other)
            => other != null && Code == other.Code &&
               string.Equals(Path, other.Path, StringComparison.Ordinal) &&
               string.Equals(Detail, other.Detail, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as SpecialRegionEntryBufferError);

        public override int GetHashCode()
        {
            unchecked
            {
                var value = (int)Code;
                value = (value * 397) ^ StringComparer.Ordinal.GetHashCode(Path);
                return (value * 397) ^ StringComparer.Ordinal.GetHashCode(Detail);
            }
        }

        public override string ToString() => Code + "|" + Path + "|" + Detail;
    }

    public sealed class SpecialRegionEntryBufferResult
    {
        private readonly ReadOnlyCollection<SpecialRegionEntryBufferError> errors;

        internal SpecialRegionEntryBufferResult(
            SpecialRegionEntryBufferPlan plan,
            IEnumerable<SpecialRegionEntryBufferError> errors)
        {
            var values = (errors ?? Array.Empty<SpecialRegionEntryBufferError>())
                .Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            this.errors = new ReadOnlyCollection<SpecialRegionEntryBufferError>(values);
            Plan = values.Length == 0 ? plan : null;
            CanonicalDigest = Plan == null ? string.Empty : Plan.CanonicalDigest;
        }

        public bool Succeeded => Plan != null && errors.Count == 0;
        public SpecialRegionEntryBufferPlan Plan { get; }
        public IReadOnlyList<SpecialRegionEntryBufferError> Errors => errors;
        public string CanonicalDigest { get; }
    }

    public static class SpecialRegionEntryBufferCompiler
    {
        public static SpecialRegionEntryBufferResult Compile(SpecialRegionEntryBufferCompileRequest request)
        {
            var errors = new List<SpecialRegionEntryBufferError>();
            if (request == null)
                return Failure(SpecialRegionEntryBufferErrorCode.MissingInput, "request");
            if (request.Bridge == null)
                return Failure(SpecialRegionEntryBufferErrorCode.MissingInput, "bridge");

            ValidateBridge(request, errors);
            var entry = FindPort(request.Bridge, request.EntryPortId, SpecialRegionSlotKind.Entry, "entryPort", errors);
            var returned = FindPort(request.Bridge, request.ReturnPortId, SpecialRegionSlotKind.Return, "returnPort", errors);
            ValidatePortPair(entry, returned, request, errors);
            ValidateAnchor(entry, request.EntryAnchor, request.Bridge.ReservationId, false, "entryAnchor", errors);
            ValidateAnchor(returned, request.ReturnAnchor, request.Bridge.ReservationId, true, "returnAnchor", errors);
            ValidateApron(request.Bridge, entry, request.EntryApron, "entryApron", errors);
            ValidateApron(request.Bridge, returned, request.ReturnApron, "returnApron", errors);
            ValidateApronUnion(request.EntryApron, request.ReturnApron, errors);

            var before = ValidatePlacement(
                request.BeforePlacement, SpecialRegionQuietChunkRole.Before,
                entry, request.EntryAnchor, ClusterPortKind.Exit, "before", errors);
            var after = ValidatePlacement(
                request.AfterPlacement, SpecialRegionQuietChunkRole.After,
                returned, request.ReturnAnchor, ClusterPortKind.Entry, "after", errors);

            ValidateBufferOverlap(request, before, after, errors);
            if (request.BeforePlacement != null && request.AfterPlacement != null &&
                string.Equals(request.BeforePlacement.PlacementId, request.AfterPlacement.PlacementId, StringComparison.Ordinal))
                Add(errors, SpecialRegionEntryBufferErrorCode.QuietChunkMismatch,
                    "placements", "Before and After placement identities must be distinct.");

            if (errors.Count != 0) return new SpecialRegionEntryBufferResult(null, errors);

            var entryRoutes = request.EntryAnchor.AllowedRouteTypes.Intersect(
                request.BeforePlacement.Candidate.CompatibleRouteTypes).OrderBy(value => value).ToArray();
            var returnRoutes = request.ReturnAnchor.AllowedRouteTypes.Intersect(
                request.AfterPlacement.Candidate.CompatibleRouteTypes).OrderBy(value => value).ToArray();
            var witness = new SpecialRegionBidirectionalWitness(
                new[] { "BeforeQuiet", "EntrySocket", "EntryApron", "RegionInterior" },
                new[] { "RegionInterior", "ReturnApron", "ReturnSocket", "AfterQuiet" },
                entryRoutes, returnRoutes);
            if (!witness.IsBidirectional || entryRoutes.Length == 0 || returnRoutes.Length == 0)
                return Failure(SpecialRegionEntryBufferErrorCode.MissingBidirectionalWitness, "witness");

            var plan = new SpecialRegionEntryBufferPlan(
                request.Bridge,
                new SpecialRegionEntryPortBinding(entry, request.EntryAnchor, entryRoutes),
                new SpecialRegionEntryPortBinding(returned, request.ReturnAnchor, returnRoutes),
                new[] { Clone(request.EntryApron), Clone(request.ReturnApron) },
                before.Bindings.Concat(after.Bindings), witness);
            plan.CanonicalDigest = SpecialRegionEntryBufferCanonicalDigest.Compute(plan);
            if (string.IsNullOrEmpty(plan.CanonicalDigest))
                return Failure(SpecialRegionEntryBufferErrorCode.NonCanonicalPublication, "plan");
            return new SpecialRegionEntryBufferResult(plan, Array.Empty<SpecialRegionEntryBufferError>());
        }

        private static void ValidateBridge(
            SpecialRegionEntryBufferCompileRequest request,
            ICollection<SpecialRegionEntryBufferError> errors)
        {
            string computed;
            try { computed = SpecialRegionSiteBridgeCanonicalDigest.Compute(request.Bridge); }
            catch (Exception)
            {
                Add(errors, SpecialRegionEntryBufferErrorCode.BridgeDigestMismatch,
                    "bridge", "Bridge digest could not be recomputed.");
                return;
            }
            if (string.IsNullOrEmpty(request.ExpectedBridgeDigest) ||
                !string.Equals(request.ExpectedBridgeDigest, request.Bridge.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(computed, request.Bridge.CanonicalDigest, StringComparison.Ordinal))
                Add(errors, SpecialRegionEntryBufferErrorCode.BridgeDigestMismatch,
                    "bridge.digest", "Expected, published, and recomputed bridge digests must match.");
        }

        private static SpecialRegionSitePortBinding FindPort(
            SpecialRegionSiteBridge bridge,
            string portId,
            SpecialRegionSlotKind kind,
            string path,
            ICollection<SpecialRegionEntryBufferError> errors)
        {
            var byKind = bridge.PortBindings.Where(value => value.Kind == kind).ToArray();
            var matches = byKind.Where(value => string.Equals(value.PortId, portId, StringComparison.Ordinal)).ToArray();
            if (byKind.Length != 1 || matches.Length != 1)
            {
                Add(errors, SpecialRegionEntryBufferErrorCode.InvalidPortPair,
                    path, "Exactly one selected " + kind + " bridge port is required.");
                return null;
            }
            return matches[0];
        }

        private static void ValidatePortPair(
            SpecialRegionSitePortBinding entry,
            SpecialRegionSitePortBinding returned,
            SpecialRegionEntryBufferCompileRequest request,
            ICollection<SpecialRegionEntryBufferError> errors)
        {
            if (entry == null || returned == null) return;
            if (string.Equals(entry.PortId, returned.PortId, StringComparison.Ordinal) || entry.SlotId == returned.SlotId)
                Add(errors, SpecialRegionEntryBufferErrorCode.InvalidPortPair,
                    "ports", "Entry and Return identities must be distinct.");
            if (entry.AccessClass != AccessClass.MandatoryNoTool || returned.AccessClass != AccessClass.MandatoryNoTool)
                Add(errors, SpecialRegionEntryBufferErrorCode.InvalidMandatoryAccess,
                    "ports.access", "Both ports must be MandatoryNoTool.");
            if (!IsExterior(entry.Placed.LocalTile, entry.Placed.Side) ||
                !IsExterior(returned.Placed.LocalTile, returned.Placed.Side))
                Add(errors, SpecialRegionEntryBufferErrorCode.InvalidPortPair,
                    "ports.edge", "Both placed port tiles must be on their declared exterior edge.");
            if (entry.AnchorExteriorSector != ExpectedExterior(entry.Placed) ||
                returned.AnchorExteriorSector != ExpectedExterior(returned.Placed))
                Add(errors, SpecialRegionEntryBufferErrorCode.InvalidPortPair,
                    "ports.exteriorSector", "Bridge exterior sectors must be exact.");
        }

        private static void ValidateAnchor(
            SpecialRegionSitePortBinding port,
            SiteEntryAnchor anchor,
            SiteReservationId reservationId,
            bool requireReturn,
            string path,
            ICollection<SpecialRegionEntryBufferError> errors)
        {
            if (port == null) return;
            if (anchor == null)
            {
                Add(errors, SpecialRegionEntryBufferErrorCode.InvalidPortPair, path, "Anchor is missing.");
                return;
            }
            SectorCoord exterior;
            if (anchor.ReservationId != reservationId ||
                !string.Equals(anchor.EntrySocketId, port.EntrySocketId, StringComparison.Ordinal) ||
                anchor.FootprintSector != port.Placed.WorldSector || !port.Placed.Side.HasValue ||
                anchor.Side != port.Placed.Side.Value || !anchor.TryGetExteriorSector(out exterior) ||
                exterior != port.AnchorExteriorSector || !anchor.Required ||
                (requireReturn && !anchor.ReturnPathRequired))
                Add(errors, SpecialRegionEntryBufferErrorCode.InvalidPortPair,
                    path, "Reservation, socket, sector, side, and requirement evidence must match the bridge.");
            if (anchor.AllowedRouteTypes == null || anchor.AllowedRouteTypes.Count == 0)
                Add(errors, SpecialRegionEntryBufferErrorCode.InvalidPortPair,
                    path + ".routeTypes", "Anchor route evidence is empty.");
        }

        private static void ValidateApron(
            SpecialRegionSiteBridge bridge,
            SpecialRegionSitePortBinding port,
            SpecialRegionEntryApron apron,
            string path,
            ICollection<SpecialRegionEntryBufferError> errors)
        {
            if (port == null) return;
            if (apron == null)
            {
                Add(errors, SpecialRegionEntryBufferErrorCode.MissingInput, path, "Apron is missing.");
                return;
            }
            var unique = new HashSet<SpecialRegionTileCoordinate>(apron.Cells);
            var expectedCount = (long)apron.Width * apron.Height;
            if (!string.Equals(apron.PortId, port.PortId, StringComparison.Ordinal) ||
                apron.WorldSector != port.Placed.WorldSector || apron.Width < 4 || apron.Height < 4 ||
                expectedCount <= 0 || expectedCount > int.MaxValue || unique.Count != expectedCount ||
                apron.Cells.Count != unique.Count)
                Add(errors, SpecialRegionEntryBufferErrorCode.InvalidApron,
                    path, "Apron identity, dimensions, or unique cell count is invalid.");

            var rectangular = true;
            for (var y = 0; y < apron.Height && rectangular; y++)
                for (var x = 0; x < apron.Width; x++)
                    if (!unique.Contains(new SpecialRegionTileCoordinate(
                            apron.WorldSector, new LocalTileCoord(apron.Minimum.X + x, apron.Minimum.Y + y))))
                    {
                        rectangular = false;
                        break;
                    }
            if (!rectangular || unique.Any(value => value.WorldSector != apron.WorldSector || !IsLocalTile(value.LocalTile)) ||
                !bridge.SectorBindings.Any(value => value.WorldSector == apron.WorldSector))
                Add(errors, SpecialRegionEntryBufferErrorCode.InvalidApron,
                    path + ".cells", "Apron must be a rectangle inside one placed footprint sector.");

            var portTile = new SpecialRegionTileCoordinate(port.Placed.WorldSector, port.Placed.LocalTile);
            var inward = new SpecialRegionTileCoordinate(port.Placed.WorldSector, Inward(port.Placed.LocalTile, port.Placed.Side));
            if (!unique.Contains(portTile) || !unique.Contains(inward))
                Add(errors, SpecialRegionEntryBufferErrorCode.InvalidApron,
                    path + ".contact", "Apron must contain the port tile and immediate inward neighbor.");

            if (bridge.FixedShellBindings.Any(value => unique.Contains(
                    new SpecialRegionTileCoordinate(value.Placed.WorldSector, value.Placed.LocalTile))))
                Add(errors, SpecialRegionEntryBufferErrorCode.ApronBlocked,
                    path + ".fixedShell", "Apron overlaps fixed shell evidence.");
            if (bridge.SlotBindings.Any(value => value.Kind != SpecialRegionSlotKind.Entry &&
                    value.Kind != SpecialRegionSlotKind.Return && unique.Contains(
                        new SpecialRegionTileCoordinate(value.Placed.WorldSector, value.Placed.LocalTile))))
                Add(errors, SpecialRegionEntryBufferErrorCode.ApronBlocked,
                    path + ".slots", "Apron overlaps a non-port slot.");
        }

        private static void ValidateApronUnion(
            SpecialRegionEntryApron entry,
            SpecialRegionEntryApron returned,
            ICollection<SpecialRegionEntryBufferError> errors)
        {
            if (entry == null || returned == null) return;
            var all = new HashSet<SpecialRegionTileCoordinate>(entry.Cells.Concat(returned.Cells));
            if (all.Count == 0) return;
            var visited = new HashSet<SpecialRegionTileCoordinate>();
            var queue = new Queue<SpecialRegionTileCoordinate>();
            queue.Enqueue(all.First());
            while (queue.Count != 0)
            {
                var current = queue.Dequeue();
                if (!visited.Add(current)) continue;
                foreach (var next in Neighbors(current)) if (all.Contains(next) && !visited.Contains(next)) queue.Enqueue(next);
            }
            if (visited.Count != all.Count)
                Add(errors, SpecialRegionEntryBufferErrorCode.InvalidApron,
                    "aprons.union", "Entry and Return apron union must be four-neighbor connected.");
        }

        private sealed class PlacementValidation
        {
            public readonly List<SpecialRegionQuietChunkBinding> Bindings = new List<SpecialRegionQuietChunkBinding>();
            public readonly HashSet<SpecialRegionTileCoordinate> Tiles = new HashSet<SpecialRegionTileCoordinate>();
        }

        private static PlacementValidation ValidatePlacement(
            SpecialRegionQuietBufferPlacement placement,
            SpecialRegionQuietChunkRole role,
            SpecialRegionSitePortBinding regionPort,
            SiteEntryAnchor anchor,
            ClusterPortKind contactPortKind,
            string path,
            ICollection<SpecialRegionEntryBufferError> errors)
        {
            var output = new PlacementValidation();
            if (placement == null || placement.Candidate == null)
            {
                Add(errors, SpecialRegionEntryBufferErrorCode.MissingInput, path, "Quiet placement or candidate is missing.");
                return output;
            }
            var candidate = placement.Candidate;
            var expectedUse = role == SpecialRegionQuietChunkRole.Before
                ? TerrainClusterQuietBufferUse.BeforeLandmark : TerrainClusterQuietBufferUse.AfterLandmark;
            if (placement.Role != role || !IsCanonicalId(placement.PlacementId) ||
                candidate.ActiveChunkCount != 2 || candidate.ActiveChunks.Count != 2 ||
                candidate.ActiveChunks.Distinct().Count() != 2 ||
                !candidate.SupportedUses.Contains(expectedUse) ||
                !candidate.CompatiblePacingRoles.Contains(PacingRole.Quiet) ||
                !candidate.CompatibleAccessClasses.Contains(AccessClass.MandatoryNoTool) ||
                candidate.RewardRoleCount != 0 || candidate.MarkerCount != 0 || candidate.HazardCount != 0 ||
                candidate.ProtectedWriteCount != 0 || candidate.ProtectedValueChangeCount != 0 ||
                string.IsNullOrEmpty(candidate.CanonicalDigest))
                Add(errors, SpecialRegionEntryBufferErrorCode.InvalidQuietCandidate,
                    path + ".candidate", "Candidate must preserve the exact two-chunk MAP11 Quiet evidence.");

            if (placement.Chunks.Count != 2 || placement.Chunks.Select(value => value.SourceChunk).Distinct().Count() != 2 ||
                !new HashSet<ClusterChunkCoord>(placement.Chunks.Select(value => value.SourceChunk))
                    .SetEquals(candidate.ActiveChunks))
                Add(errors, SpecialRegionEntryBufferErrorCode.QuietChunkMismatch,
                    path + ".chunks", "Exactly the candidate's two active chunks must be placed whole.");
            if (placement.Chunks.Count == 2 &&
                (!AreAdjacent(candidate.ActiveChunks[0], candidate.ActiveChunks[1]) ||
                 !AreAdjacent(placement.Chunks[0], placement.Chunks[1])))
                Add(errors, SpecialRegionEntryBufferErrorCode.QuietChunkMismatch,
                    path + ".adjacency", "The source candidate and placed chunks must remain cardinal neighbors.");

            var occupiedChunkCells = new HashSet<string>(StringComparer.Ordinal);
            foreach (var chunk in placement.Chunks)
            {
                if (!IsWorldSector(chunk.WorldSector) || chunk.SectorLocalChunk.X < 0 ||
                    chunk.SectorLocalChunk.X >= WorldGenConstants.SectorWidthTiles / WorldGenConstants.MicroChunkWidthTiles ||
                    chunk.SectorLocalChunk.Y < 0 ||
                    chunk.SectorLocalChunk.Y >= WorldGenConstants.SectorHeightTiles / WorldGenConstants.MicroChunkHeightTiles ||
                    !occupiedChunkCells.Add(chunk.WorldSector.X + "," + chunk.WorldSector.Y + "/" +
                        chunk.SectorLocalChunk.X + "," + chunk.SectorLocalChunk.Y))
                    Add(errors, SpecialRegionEntryBufferErrorCode.QuietChunkMismatch,
                        path + ".chunks", "Placed chunk coordinates must be unique and inside a 48x32 sector.");

                var evidence = candidate.ChunkEvidence.SingleOrDefault(value => value.Chunk == chunk.SourceChunk);
                if (evidence == null || evidence.SolidCount <= 0 || evidence.AirCount <= 0 ||
                    evidence.BaselineCoordinateCount <= 0 || !candidate.BaselineCoveredChunks.Contains(chunk.SourceChunk))
                    Add(errors, SpecialRegionEntryBufferErrorCode.InvalidQuietCandidate,
                        path + ".evidence", "Each active chunk needs terrain and baseline coverage evidence.");
                output.Bindings.Add(new SpecialRegionQuietChunkBinding(
                    placement.PlacementId, role, candidate.QuietBufferId, candidate.CanonicalDigest,
                    chunk.SourceChunk, chunk.WorldSector, chunk.SectorLocalChunk, evidence));
            }

            foreach (var cell in candidate.LocalCanvas.TileCells.Where(
                         value => value.State == ClusterChunkMaskState.Active && candidate.ActiveChunks.Contains(value.OwningChunk)))
            {
                SpecialRegionTileCoordinate mapped;
                if (!TryMap(placement, cell.Coordinate, out mapped) || !output.Tiles.Add(mapped))
                    Add(errors, SpecialRegionEntryBufferErrorCode.QuietChunkMismatch,
                        path + ".tiles", "Every active candidate tile must map exactly once.");
            }
            if (output.Tiles.Count != candidate.ActiveChunkCount * WorldGenConstants.MicroChunkWidthTiles *
                WorldGenConstants.MicroChunkHeightTiles)
                Add(errors, SpecialRegionEntryBufferErrorCode.QuietChunkMismatch,
                    path + ".tiles", "Whole active chunk tile coverage is required.");

            ProjectedClusterPort contactPort;
            if (!candidate.RoleSocketContract.TryGetPrimaryPort(contactPortKind, out contactPort) || contactPort == null ||
                regionPort == null || anchor == null)
            {
                Add(errors, SpecialRegionEntryBufferErrorCode.MissingBidirectionalWitness,
                    path + ".contact", "Primary contact port evidence is missing.");
                return output;
            }
            SpecialRegionTileCoordinate mappedContact;
            var expectedContact = ExteriorAdjacent(regionPort.Placed);
            if (!TryMap(placement, contactPort.CompiledCoordinate, out mappedContact) || mappedContact != expectedContact ||
                ToSiteSide(contactPort.CompiledOutwardSide) != SiteReservationTokenCodec.GetOpposite(regionPort.Placed.Side.Value))
                Add(errors, SpecialRegionEntryBufferErrorCode.MissingBidirectionalWitness,
                    path + ".contact", "Exactly one candidate contact port must meet the region exterior-adjacent tile.");
            var contactChunks = placement.Chunks.Count(value =>
                value.WorldSector == expectedContact.WorldSector &&
                value.SectorLocalChunk == new ClusterChunkCoord(
                    expectedContact.LocalTile.X / WorldGenConstants.MicroChunkWidthTiles,
                    expectedContact.LocalTile.Y / WorldGenConstants.MicroChunkHeightTiles));
            if (contactChunks != 1)
                Add(errors, SpecialRegionEntryBufferErrorCode.QuietChunkMismatch,
                    path + ".contactChunk", "Exactly one placed chunk may contain the contact tile.");
            if (!anchor.AllowedRouteTypes.Intersect(candidate.CompatibleRouteTypes).Any() ||
                !contactPort.CompatibleRouteTypes.Intersect(anchor.AllowedRouteTypes).Any())
                Add(errors, SpecialRegionEntryBufferErrorCode.MissingBidirectionalWitness,
                    path + ".routeTypes", "Anchor, candidate, and contact port route sets must intersect.");
            return output;
        }

        private static void ValidateBufferOverlap(
            SpecialRegionEntryBufferCompileRequest request,
            PlacementValidation before,
            PlacementValidation after,
            ICollection<SpecialRegionEntryBufferError> errors)
        {
            var footprint = new HashSet<SpecialRegionTileCoordinate>();
            foreach (var sector in request.Bridge.SectorBindings)
                for (var y = 0; y < WorldGenConstants.SectorHeightTiles; y++)
                    for (var x = 0; x < WorldGenConstants.SectorWidthTiles; x++)
                        footprint.Add(new SpecialRegionTileCoordinate(sector.WorldSector, new LocalTileCoord(x, y)));
            var apron = new HashSet<SpecialRegionTileCoordinate>(
                (request.EntryApron == null ? Array.Empty<SpecialRegionTileCoordinate>() : request.EntryApron.Cells)
                .Concat(request.ReturnApron == null ? Array.Empty<SpecialRegionTileCoordinate>() : request.ReturnApron.Cells));
            if (before.Tiles.Overlaps(footprint) || after.Tiles.Overlaps(footprint) ||
                before.Tiles.Overlaps(apron) || after.Tiles.Overlaps(apron) || before.Tiles.Overlaps(after.Tiles))
                Add(errors, SpecialRegionEntryBufferErrorCode.BufferOverlap,
                    "buffers", "Footprint, aprons, Before Quiet, and After Quiet may not collide.");
        }

        private static bool TryMap(
            SpecialRegionQuietBufferPlacement placement,
            LocalTileCoord source,
            out SpecialRegionTileCoordinate mapped)
        {
            mapped = default(SpecialRegionTileCoordinate);
            var sourceChunk = new ClusterChunkCoord(
                source.X / WorldGenConstants.MicroChunkWidthTiles,
                source.Y / WorldGenConstants.MicroChunkHeightTiles);
            var chunk = placement.Chunks.SingleOrDefault(value => value.SourceChunk == sourceChunk);
            if (chunk == null) return false;
            mapped = new SpecialRegionTileCoordinate(
                chunk.WorldSector,
                new LocalTileCoord(
                    chunk.SectorLocalChunk.X * WorldGenConstants.MicroChunkWidthTiles +
                    source.X % WorldGenConstants.MicroChunkWidthTiles,
                    chunk.SectorLocalChunk.Y * WorldGenConstants.MicroChunkHeightTiles +
                    source.Y % WorldGenConstants.MicroChunkHeightTiles));
            return IsLocalTile(mapped.LocalTile);
        }

        private static bool AreAdjacent(ClusterChunkCoord left, ClusterChunkCoord right)
            => Math.Abs(left.X - right.X) + Math.Abs(left.Y - right.Y) == 1;

        private static bool AreAdjacent(
            SpecialRegionQuietChunkPlacement left,
            SpecialRegionQuietChunkPlacement right)
        {
            var columns = WorldGenConstants.SectorWidthTiles / WorldGenConstants.MicroChunkWidthTiles;
            var rows = WorldGenConstants.SectorHeightTiles / WorldGenConstants.MicroChunkHeightTiles;
            var leftX = left.WorldSector.X * columns + left.SectorLocalChunk.X;
            var leftY = left.WorldSector.Y * rows + left.SectorLocalChunk.Y;
            var rightX = right.WorldSector.X * columns + right.SectorLocalChunk.X;
            var rightY = right.WorldSector.Y * rows + right.SectorLocalChunk.Y;
            return Math.Abs(leftX - rightX) + Math.Abs(leftY - rightY) == 1;
        }

        private static SpecialRegionEntryApron Clone(SpecialRegionEntryApron source)
            => new SpecialRegionEntryApron(
                source.PortId, source.WorldSector, source.Minimum, source.Width, source.Height, source.Cells);

        private static IEnumerable<SpecialRegionTileCoordinate> Neighbors(SpecialRegionTileCoordinate value)
        {
            var directions = new[] { new[] { -1, 0 }, new[] { 1, 0 }, new[] { 0, -1 }, new[] { 0, 1 } };
            foreach (var direction in directions)
            {
                var sectorX = value.WorldSector.X;
                var sectorY = value.WorldSector.Y;
                var x = value.LocalTile.X + direction[0];
                var y = value.LocalTile.Y + direction[1];
                if (x < 0) { sectorX--; x = WorldGenConstants.SectorWidthTiles - 1; }
                else if (x >= WorldGenConstants.SectorWidthTiles) { sectorX++; x = 0; }
                if (y < 0) { sectorY--; y = WorldGenConstants.SectorHeightTiles - 1; }
                else if (y >= WorldGenConstants.SectorHeightTiles) { sectorY++; y = 0; }
                yield return new SpecialRegionTileCoordinate(new SectorCoord(sectorX, sectorY), new LocalTileCoord(x, y));
            }
        }

        private static SpecialRegionTileCoordinate ExteriorAdjacent(SpecialRegionPlacedCoordinate placed)
        {
            var side = placed.Side.Value;
            var exterior = ExpectedExterior(placed);
            switch (side)
            {
                case SiteEntrySide.L:
                    return new SpecialRegionTileCoordinate(exterior,
                        new LocalTileCoord(WorldGenConstants.SectorWidthTiles - 1, placed.LocalTile.Y));
                case SiteEntrySide.R:
                    return new SpecialRegionTileCoordinate(exterior, new LocalTileCoord(0, placed.LocalTile.Y));
                case SiteEntrySide.D:
                    return new SpecialRegionTileCoordinate(exterior,
                        new LocalTileCoord(placed.LocalTile.X, WorldGenConstants.SectorHeightTiles - 1));
                default:
                    return new SpecialRegionTileCoordinate(exterior, new LocalTileCoord(placed.LocalTile.X, 0));
            }
        }

        private static SectorCoord ExpectedExterior(SpecialRegionPlacedCoordinate placed)
        {
            SiteReservationTokenCodec.GetDelta(placed.Side.Value, out var x, out var y);
            return new SectorCoord(placed.WorldSector.X + x, placed.WorldSector.Y + y);
        }

        private static LocalTileCoord Inward(LocalTileCoord tile, SiteEntrySide? side)
        {
            switch (side)
            {
                case SiteEntrySide.L: return new LocalTileCoord(tile.X + 1, tile.Y);
                case SiteEntrySide.R: return new LocalTileCoord(tile.X - 1, tile.Y);
                case SiteEntrySide.D: return new LocalTileCoord(tile.X, tile.Y + 1);
                case SiteEntrySide.U: return new LocalTileCoord(tile.X, tile.Y - 1);
                default: return tile;
            }
        }

        private static bool IsExterior(LocalTileCoord tile, SiteEntrySide? side)
        {
            if (!side.HasValue) return false;
            switch (side.Value)
            {
                case SiteEntrySide.L: return tile.X == 0;
                case SiteEntrySide.R: return tile.X == WorldGenConstants.SectorWidthTiles - 1;
                case SiteEntrySide.D: return tile.Y == 0;
                case SiteEntrySide.U: return tile.Y == WorldGenConstants.SectorHeightTiles - 1;
                default: return false;
            }
        }

        private static SiteEntrySide ToSiteSide(ClusterPortSide side)
        {
            switch (side)
            {
                case ClusterPortSide.L: return SiteEntrySide.L;
                case ClusterPortSide.R: return SiteEntrySide.R;
                case ClusterPortSide.U: return SiteEntrySide.U;
                case ClusterPortSide.D: return SiteEntrySide.D;
                default: throw new ArgumentOutOfRangeException(nameof(side));
            }
        }

        private static bool IsLocalTile(LocalTileCoord value)
            => value.X >= 0 && value.X < WorldGenConstants.SectorWidthTiles &&
               value.Y >= 0 && value.Y < WorldGenConstants.SectorHeightTiles;

        private static bool IsWorldSector(SectorCoord value)
            => value.X >= 0 && value.X < WorldGenConstants.SectorColumns &&
               value.Y >= 0 && value.Y < WorldGenConstants.SectorRows;

        private static bool IsCanonicalId(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 128 || value.Trim() != value) return false;
            return value.All(character => (character >= 'a' && character <= 'z') ||
                (character >= 'A' && character <= 'Z') || (character >= '0' && character <= '9') ||
                character == '.' || character == '_' || character == ':' || character == '-');
        }

        private static SpecialRegionEntryBufferResult Failure(
            SpecialRegionEntryBufferErrorCode code, string path)
            => new SpecialRegionEntryBufferResult(null, new[]
            {
                new SpecialRegionEntryBufferError(code, path, "Required canonical input was not supplied.")
            });

        private static void Add(
            ICollection<SpecialRegionEntryBufferError> errors,
            SpecialRegionEntryBufferErrorCode code,
            string path,
            string detail)
            => errors.Add(new SpecialRegionEntryBufferError(code, path, detail));
    }

    public static class SpecialRegionEntryBufferCanonicalDigest
    {
        public static string Compute(SpecialRegionEntryBufferPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var value = new StringBuilder();
            Append(value, "region", plan.RegionId.Value);
            Append(value, "reservation", plan.ReservationId.Value);
            Append(value, "bridge", plan.BridgeDigest);
            AppendPort(value, "entry", plan.EntryPort);
            AppendPort(value, "return", plan.ReturnPort);
            foreach (var apron in plan.Aprons)
            {
                Append(value, "apron", apron.PortId + "/" + Coordinate(apron.WorldSector.X, apron.WorldSector.Y) +
                    "/" + Coordinate(apron.Minimum.X, apron.Minimum.Y) + "/" + Coordinate(apron.Width, apron.Height));
                foreach (var cell in apron.Cells) Append(value, "apronCell", cell.ToString());
            }
            foreach (var chunk in plan.QuietChunks)
                Append(value, "quietChunk", chunk.PlacementId + "/" + Number((int)chunk.Role) + "/" +
                    chunk.QuietBufferId + "/" + chunk.CandidateDigest + "/" +
                    Coordinate(chunk.SourceChunk.X, chunk.SourceChunk.Y) + "/" +
                    Coordinate(chunk.WorldSector.X, chunk.WorldSector.Y) + "/" +
                    Coordinate(chunk.SectorLocalChunk.X, chunk.SectorLocalChunk.Y) + "/" +
                    Coordinate(chunk.SolidCount, chunk.AirCount) + "/" + Number(chunk.BaselineCoordinateCount));
            foreach (var segment in plan.Witness.ForwardSegments) Append(value, "forward", segment);
            foreach (var segment in plan.Witness.ReturnSegments) Append(value, "returnPath", segment);
            return Sha256(value.ToString());
        }

        private static void AppendPort(StringBuilder value, string name, SpecialRegionEntryPortBinding port)
        {
            Append(value, name, port.PortId + "/" + port.SlotId.Value + "/" + Number((int)port.Kind) +
                "/" + Number((int)port.AccessClass) + "/" + port.EntrySocketId + "/" +
                Coordinate(port.Placed.WorldSector.X, port.Placed.WorldSector.Y) + "/" +
                Coordinate(port.Placed.LocalTile.X, port.Placed.LocalTile.Y) + "/" +
                string.Join(",", port.RouteTypes.Select(Number)));
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Coordinate(int x, int y) => Number(x) + "," + Number(y);
        private static void Append(StringBuilder value, string name, string field)
            => value.Append(name).Append('=').Append(field ?? string.Empty).Append('\n');

        private static string Sha256(string material)
        {
            using (var sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(new UTF8Encoding(false).GetBytes(material))
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }
    }
}
