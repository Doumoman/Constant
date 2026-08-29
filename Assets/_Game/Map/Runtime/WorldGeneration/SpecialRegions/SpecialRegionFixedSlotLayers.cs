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

namespace StarNight.Map.WorldGeneration.SpecialRegions
{
    public enum SpecialRegionFixedAccessKind
    {
        Entry = 1,
        Return = 2,
        Apron = 3,
    }

    public enum SpecialRegionSlotReplacementOperation
    {
        Clear = 1,
        Assign = 2,
    }

    public sealed class SpecialRegionSlotReplacementIntent
    {
        public SpecialRegionSlotReplacementIntent(
            SpecialRegionSlotId slotId,
            SpecialRegionSlotReplacementOperation operation,
            string occupantId,
            SpecialRegionSlotKind occupantKind)
        {
            SlotId = slotId;
            Operation = operation;
            OccupantId = occupantId ?? string.Empty;
            OccupantKind = occupantKind;
        }

        public SpecialRegionSlotId SlotId { get; }
        public SpecialRegionSlotReplacementOperation Operation { get; }
        public string OccupantId { get; }
        public SpecialRegionSlotKind OccupantKind { get; }

        public static SpecialRegionSlotReplacementIntent Clear(SpecialRegionSlotId slotId)
            => new SpecialRegionSlotReplacementIntent(
                slotId, SpecialRegionSlotReplacementOperation.Clear,
                string.Empty, default(SpecialRegionSlotKind));

        public static SpecialRegionSlotReplacementIntent Assign(
            SpecialRegionSlotId slotId,
            SpecialRegionSlotKind occupantKind,
            string occupantId)
            => new SpecialRegionSlotReplacementIntent(
                slotId, SpecialRegionSlotReplacementOperation.Assign, occupantId, occupantKind);
    }

    public sealed class SpecialRegionFixedCollisionCell
    {
        internal SpecialRegionFixedCollisionCell(
            string shellId,
            SpecialRegionAuthoredCoordinate source,
            SpecialRegionPlacedCoordinate placed)
        {
            ShellId = shellId ?? string.Empty;
            Source = source;
            Placed = placed;
            Coordinate = new SpecialRegionTileCoordinate(placed.WorldSector, placed.LocalTile);
        }

        public string ShellId { get; }
        public SpecialRegionAuthoredCoordinate Source { get; }
        public SpecialRegionPlacedCoordinate Placed { get; }
        public SpecialRegionTileCoordinate Coordinate { get; }
        public SpecialRegionLayerKind LayerKind => SpecialRegionLayerKind.FixedShell;
        public bool IsImmutable => true;
        public bool IsHardProtected => true;
        public bool OwnsCollision => true;
        public bool OwnsAccess => false;
    }

    public sealed class SpecialRegionFixedAccessBinding
    {
        internal SpecialRegionFixedAccessBinding(
            SpecialRegionFixedAccessKind accessKind,
            string portId,
            SpecialRegionSlotId slotId,
            SpecialRegionSlotKind slotKind,
            AccessClass accessClass,
            SpecialRegionTileCoordinate coordinate,
            SpecialRegionAuthoredCoordinate source,
            SpecialRegionPlacedCoordinate placed,
            bool hasSource)
        {
            AccessKind = accessKind;
            PortId = portId ?? string.Empty;
            SlotId = slotId;
            SlotKind = slotKind;
            AccessClass = accessClass;
            Coordinate = coordinate;
            Source = source;
            Placed = placed;
            HasSource = hasSource;
        }

        public SpecialRegionFixedAccessKind AccessKind { get; }
        public string PortId { get; }
        public SpecialRegionSlotId SlotId { get; }
        public SpecialRegionSlotKind SlotKind { get; }
        public AccessClass AccessClass { get; }
        public SpecialRegionTileCoordinate Coordinate { get; }
        public SpecialRegionAuthoredCoordinate Source { get; }
        public SpecialRegionPlacedCoordinate Placed { get; }
        public bool HasSource { get; }
        public bool IsImmutable => true;
        public bool IsHardProtected => true;
        public bool OwnsAccess => true;
        public bool OwnsCollision => false;
    }

    public sealed class SpecialRegionReplaceableSlotBinding
    {
        internal SpecialRegionReplaceableSlotBinding(
            SpecialRegionSiteSlotBinding source,
            SpecialRegionSlotReplacementIntent replacement)
        {
            SlotId = source.SlotId;
            Kind = source.Kind;
            Required = source.Required;
            PersistenceScope = source.PersistenceScope;
            PersistenceKey = source.PersistenceKey;
            Source = source.Source;
            Placed = source.Placed;
            Coordinate = new SpecialRegionTileCoordinate(source.Placed.WorldSector, source.Placed.LocalTile);
            Operation = replacement == null
                ? SpecialRegionSlotReplacementOperation.Clear
                : replacement.Operation;
            OccupantId = replacement == null ? string.Empty : replacement.OccupantId;
            OccupantKind = replacement == null || replacement.Operation == SpecialRegionSlotReplacementOperation.Clear
                ? default(SpecialRegionSlotKind)
                : replacement.OccupantKind;
            IdentityDigest = SpecialRegionFixedSlotLayerCanonicalDigest.ComputeSlotIdentity(this);
        }

        public SpecialRegionSlotId SlotId { get; }
        public SpecialRegionSlotKind Kind { get; }
        public bool Required { get; }
        public SpecialPersistenceScope PersistenceScope { get; }
        public SpecialPersistenceKey PersistenceKey { get; }
        public SpecialRegionAuthoredCoordinate Source { get; }
        public SpecialRegionPlacedCoordinate Placed { get; }
        public SpecialRegionTileCoordinate Coordinate { get; }
        public SpecialRegionSlotReplacementOperation Operation { get; }
        public string OccupantId { get; }
        public SpecialRegionSlotKind OccupantKind { get; }
        public string IdentityDigest { get; }
        public SpecialRegionLayerKind LayerKind => SpecialRegionLayerKind.ReplaceableSlot;
        public bool IsAssigned => Operation == SpecialRegionSlotReplacementOperation.Assign;
        public bool IsMarkerOnly => true;
        public bool IsEventMarkerOnly => Kind == SpecialRegionSlotKind.Event;
        public bool OccupantOwnsPersistence => false;
        public bool OwnsSolid => false;
        public bool OwnsCollision => false;
        public bool OwnsRoute => false;
        public bool OwnsAccess => false;
        public bool PerformsRuntimeMutation => false;
    }

    public sealed class SpecialRegionFixedSlotLayerPlan
    {
        private readonly ReadOnlyCollection<SpecialRegionFixedCollisionCell> fixedCollision;
        private readonly ReadOnlyCollection<SpecialRegionFixedAccessBinding> fixedAccess;
        private readonly ReadOnlyCollection<SpecialRegionReplaceableSlotBinding> replaceableSlots;
        private readonly ReadOnlyCollection<SpecialRegionOccupancyClaim> hardProtectedClaims;

        internal SpecialRegionFixedSlotLayerPlan(
            SpecialRegionId regionId,
            SpecialRegionKind regionKind,
            SiteReservationId reservationId,
            string contractDigest,
            string bridgeDigest,
            string entryBufferDigest,
            string collisionDigest,
            IEnumerable<SpecialRegionFixedCollisionCell> fixedCollision,
            IEnumerable<SpecialRegionFixedAccessBinding> fixedAccess,
            IEnumerable<SpecialRegionReplaceableSlotBinding> replaceableSlots,
            IEnumerable<SpecialRegionOccupancyClaim> hardProtectedClaims)
        {
            RegionId = regionId;
            RegionKind = regionKind;
            ReservationId = reservationId;
            ContractDigest = contractDigest ?? string.Empty;
            BridgeDigest = bridgeDigest ?? string.Empty;
            EntryBufferDigest = entryBufferDigest ?? string.Empty;
            CollisionDigest = collisionDigest ?? string.Empty;
            this.fixedCollision = new ReadOnlyCollection<SpecialRegionFixedCollisionCell>(
                (fixedCollision ?? Array.Empty<SpecialRegionFixedCollisionCell>())
                .Where(value => value != null).OrderBy(value => value.Coordinate)
                .ThenBy(value => value.ShellId, StringComparer.Ordinal).ToArray());
            this.fixedAccess = new ReadOnlyCollection<SpecialRegionFixedAccessBinding>(
                (fixedAccess ?? Array.Empty<SpecialRegionFixedAccessBinding>())
                .Where(value => value != null).OrderBy(value => value.Coordinate)
                .ThenBy(value => value.AccessKind).ThenBy(value => value.PortId, StringComparer.Ordinal).ToArray());
            this.replaceableSlots = new ReadOnlyCollection<SpecialRegionReplaceableSlotBinding>(
                (replaceableSlots ?? Array.Empty<SpecialRegionReplaceableSlotBinding>())
                .Where(value => value != null).OrderBy(value => value.Kind)
                .ThenBy(value => value.SlotId).ToArray());
            this.hardProtectedClaims = new ReadOnlyCollection<SpecialRegionOccupancyClaim>(
                (hardProtectedClaims ?? Array.Empty<SpecialRegionOccupancyClaim>())
                .Where(value => value != null).Select(CloneClaim)
                .OrderBy(value => value.OwnerId, StringComparer.Ordinal).ToArray());
            FixedCollisionDigest = SpecialRegionFixedSlotLayerCanonicalDigest.ComputeFixedCollision(this.fixedCollision);
            FixedAccessDigest = SpecialRegionFixedSlotLayerCanonicalDigest.ComputeFixedAccess(this.fixedAccess);
            ReplaceableSlotDigest = SpecialRegionFixedSlotLayerCanonicalDigest.ComputeReplaceableSlots(this.replaceableSlots);
            AssignmentDigest = SpecialRegionFixedSlotLayerCanonicalDigest.ComputeAssignments(this.replaceableSlots);
            ImmutableLayerDigest = SpecialRegionFixedSlotLayerCanonicalDigest.ComputeImmutableLayer(this);
            CanonicalDigest = SpecialRegionFixedSlotLayerCanonicalDigest.Compute(this);
        }

        public SpecialRegionId RegionId { get; }
        public SpecialRegionKind RegionKind { get; }
        public SiteReservationId ReservationId { get; }
        public string ContractDigest { get; }
        public string BridgeDigest { get; }
        public string EntryBufferDigest { get; }
        public string CollisionDigest { get; }
        public IReadOnlyList<SpecialRegionFixedCollisionCell> FixedCollision => fixedCollision;
        public IReadOnlyList<SpecialRegionFixedAccessBinding> FixedAccess => fixedAccess;
        public IReadOnlyList<SpecialRegionReplaceableSlotBinding> ReplaceableSlots => replaceableSlots;
        public IReadOnlyList<SpecialRegionOccupancyClaim> HardProtectedClaims => hardProtectedClaims;
        public string FixedCollisionDigest { get; }
        public string FixedAccessDigest { get; }
        public string ReplaceableSlotDigest { get; }
        public string AssignmentDigest { get; }
        public string ImmutableLayerDigest { get; }
        public string CanonicalDigest { get; }
        public int PlacementWriteCount => 0;
        public int SpawnCount => 0;
        public int DespawnCount => 0;
        public int TileMutationCount => 0;

        private static SpecialRegionOccupancyClaim CloneClaim(SpecialRegionOccupancyClaim source)
            => new SpecialRegionOccupancyClaim(
                source.OwnerId, source.OwnerKind, source.Cells,
                source.IsHardProtected, source.IsCommitted);
    }

    public sealed class SpecialRegionFixedSlotLayerCompileRequest
    {
        private readonly ReadOnlyCollection<SpecialRegionSlotReplacementIntent> replacements;

        public SpecialRegionFixedSlotLayerCompileRequest(
            SpecialRegionValidationResult contractValidation,
            string expectedContractDigest,
            SpecialRegionSiteBridge bridge,
            string expectedBridgeDigest,
            SpecialRegionEntryBufferPlan entryBufferPlan,
            string expectedEntryBufferDigest,
            SpecialRegionPlacementCollisionPlan collisionPlan,
            string expectedCollisionDigest,
            IEnumerable<SpecialRegionSlotReplacementIntent> replacements = null)
        {
            ContractValidation = contractValidation;
            ExpectedContractDigest = expectedContractDigest ?? string.Empty;
            Bridge = bridge;
            ExpectedBridgeDigest = expectedBridgeDigest ?? string.Empty;
            EntryBufferPlan = entryBufferPlan;
            ExpectedEntryBufferDigest = expectedEntryBufferDigest ?? string.Empty;
            CollisionPlan = collisionPlan;
            ExpectedCollisionDigest = expectedCollisionDigest ?? string.Empty;
            var supplied = replacements == null
                ? Array.Empty<SpecialRegionSlotReplacementIntent>()
                : replacements.ToArray();
            this.replacements = new ReadOnlyCollection<SpecialRegionSlotReplacementIntent>(
                supplied.Where(value => value != null).ToArray());
            SuppliedNullReplacementCount = supplied.Count(value => value == null);
        }

        public SpecialRegionValidationResult ContractValidation { get; }
        public string ExpectedContractDigest { get; }
        public SpecialRegionSiteBridge Bridge { get; }
        public string ExpectedBridgeDigest { get; }
        public SpecialRegionEntryBufferPlan EntryBufferPlan { get; }
        public string ExpectedEntryBufferDigest { get; }
        public SpecialRegionPlacementCollisionPlan CollisionPlan { get; }
        public string ExpectedCollisionDigest { get; }
        public IReadOnlyList<SpecialRegionSlotReplacementIntent> Replacements => replacements;
        internal int SuppliedNullReplacementCount { get; }
    }

    public enum SpecialRegionFixedSlotLayerErrorCode
    {
        MissingInput = 1,
        ContractDigestMismatch = 2,
        BridgeDigestMismatch = 3,
        EntryBufferDigestMismatch = 4,
        CollisionDigestMismatch = 5,
        InvalidFixedCell = 6,
        InvalidAccessBinding = 7,
        FixedAccessOverlap = 8,
        DuplicateFixedOwner = 9,
        InvalidReplaceableSlot = 10,
        ReplaceableKindMismatch = 11,
        SlotLayerOverlap = 12,
        PersistenceKeyMismatch = 13,
        PersistenceScopeMismatch = 14,
        NonCanonicalPublication = 15,
    }

    public sealed class SpecialRegionFixedSlotLayerError :
        IEquatable<SpecialRegionFixedSlotLayerError>, IComparable<SpecialRegionFixedSlotLayerError>
    {
        public SpecialRegionFixedSlotLayerError(
            SpecialRegionFixedSlotLayerErrorCode code,
            string path,
            string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public SpecialRegionFixedSlotLayerErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(SpecialRegionFixedSlotLayerError other)
        {
            if (other == null) return -1;
            var value = Code.CompareTo(other.Code);
            if (value != 0) return value;
            value = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return value != 0 ? value : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(SpecialRegionFixedSlotLayerError other)
            => other != null && Code == other.Code &&
               string.Equals(Path, other.Path, StringComparison.Ordinal) &&
               string.Equals(Detail, other.Detail, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as SpecialRegionFixedSlotLayerError);

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

    public sealed class SpecialRegionFixedSlotLayerResult
    {
        private readonly ReadOnlyCollection<SpecialRegionFixedSlotLayerError> errors;

        internal SpecialRegionFixedSlotLayerResult(
            SpecialRegionFixedSlotLayerPlan plan,
            IEnumerable<SpecialRegionFixedSlotLayerError> errors)
        {
            var values = (errors ?? Array.Empty<SpecialRegionFixedSlotLayerError>())
                .Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            this.errors = new ReadOnlyCollection<SpecialRegionFixedSlotLayerError>(values);
            Plan = values.Length == 0 ? plan : null;
            CanonicalDigest = Plan == null ? string.Empty : Plan.CanonicalDigest;
        }

        public bool Succeeded => Plan != null && errors.Count == 0;
        public SpecialRegionFixedSlotLayerPlan Plan { get; }
        public IReadOnlyList<SpecialRegionFixedSlotLayerError> Errors => errors;
        public string CanonicalDigest { get; }
    }

    public static class SpecialRegionFixedSlotLayerCompiler
    {
        public static SpecialRegionFixedSlotLayerResult Compile(
            SpecialRegionFixedSlotLayerCompileRequest request)
        {
            if (request == null)
                return Failure(SpecialRegionFixedSlotLayerErrorCode.MissingInput, "request");

            var errors = new List<SpecialRegionFixedSlotLayerError>();
            ValidatePublications(request, errors);
            if (errors.Count != 0) return new SpecialRegionFixedSlotLayerResult(null, errors);

            var fixedCollision = CompileFixedCollision(request, errors);
            var fixedAccess = CompileFixedAccess(request, errors);
            ValidateFixedSeparation(fixedCollision, fixedAccess, errors);
            var replacements = ValidateReplacements(request, errors);
            var replaceable = CompileReplaceableSlots(request, replacements, fixedCollision, fixedAccess, errors);
            var claims = ValidateCollisionEvidence(request.CollisionPlan, fixedCollision, fixedAccess, errors);

            if (errors.Count != 0) return new SpecialRegionFixedSlotLayerResult(null, errors);
            var plan = new SpecialRegionFixedSlotLayerPlan(
                request.Bridge.RegionId,
                request.Bridge.RegionKind,
                request.Bridge.ReservationId,
                request.ContractValidation.CanonicalDigest,
                request.Bridge.CanonicalDigest,
                request.EntryBufferPlan.CanonicalDigest,
                request.CollisionPlan.CanonicalDigest,
                fixedCollision,
                fixedAccess,
                replaceable,
                claims);
            if (!string.Equals(
                    plan.CanonicalDigest,
                    SpecialRegionFixedSlotLayerCanonicalDigest.Compute(plan),
                    StringComparison.Ordinal))
            {
                Add(errors, SpecialRegionFixedSlotLayerErrorCode.NonCanonicalPublication,
                    "plan", "Layer plan digest did not reproduce.");
            }
            return new SpecialRegionFixedSlotLayerResult(errors.Count == 0 ? plan : null, errors);
        }

        private static void ValidatePublications(
            SpecialRegionFixedSlotLayerCompileRequest request,
            ICollection<SpecialRegionFixedSlotLayerError> errors)
        {
            if (request.ContractValidation == null || !request.ContractValidation.IsValid ||
                request.ContractValidation.Contract == null)
            {
                Add(errors, SpecialRegionFixedSlotLayerErrorCode.MissingInput,
                    "contractValidation", "A valid MAP09 contract publication is required.");
            }
            else
            {
                var digest = SpecialRegionCanonicalDigest.Compute(request.ContractValidation.Contract);
                if (!EqualsDigest(digest, request.ContractValidation.CanonicalDigest) ||
                    !EqualsDigest(digest, request.ExpectedContractDigest))
                    Add(errors, SpecialRegionFixedSlotLayerErrorCode.ContractDigestMismatch,
                        "contractValidation", "Expected, published, and recomputed contract digests must match.");
            }

            if (request.Bridge == null)
            {
                Add(errors, SpecialRegionFixedSlotLayerErrorCode.MissingInput,
                    "bridge", "A MAP13_01 placed bridge is required.");
            }
            else
            {
                var digest = SpecialRegionSiteBridgeCanonicalDigest.Compute(request.Bridge);
                if (!EqualsDigest(digest, request.Bridge.CanonicalDigest) ||
                    !EqualsDigest(digest, request.ExpectedBridgeDigest))
                    Add(errors, SpecialRegionFixedSlotLayerErrorCode.BridgeDigestMismatch,
                        "bridge", "Expected, published, and recomputed bridge digests must match.");

                if (request.ContractValidation != null && request.ContractValidation.IsValid &&
                    (request.Bridge.RegionId != request.ContractValidation.Contract.Id ||
                     request.Bridge.ReservationId != request.ContractValidation.Contract.ReservationId ||
                     request.Bridge.RegionKind != request.ContractValidation.Contract.Kind ||
                     !EqualsDigest(request.Bridge.ContractDigest, request.ContractValidation.CanonicalDigest)))
                {
                    Add(errors, SpecialRegionFixedSlotLayerErrorCode.ContractDigestMismatch,
                        "bridge.contract", "Bridge identity must reference the exact MAP09 publication.");
                }
            }

            if (request.EntryBufferPlan == null)
            {
                Add(errors, SpecialRegionFixedSlotLayerErrorCode.MissingInput,
                    "entryBufferPlan", "A MAP13_02 entry-buffer plan is required.");
            }
            else
            {
                var digest = SpecialRegionEntryBufferCanonicalDigest.Compute(request.EntryBufferPlan);
                if (!EqualsDigest(digest, request.EntryBufferPlan.CanonicalDigest) ||
                    !EqualsDigest(digest, request.ExpectedEntryBufferDigest))
                    Add(errors, SpecialRegionFixedSlotLayerErrorCode.EntryBufferDigestMismatch,
                        "entryBufferPlan", "Expected, published, and recomputed entry-buffer digests must match.");
                if (request.Bridge != null &&
                    (request.EntryBufferPlan.RegionId != request.Bridge.RegionId ||
                     request.EntryBufferPlan.ReservationId != request.Bridge.ReservationId ||
                     !EqualsDigest(request.EntryBufferPlan.BridgeDigest, request.Bridge.CanonicalDigest)))
                    Add(errors, SpecialRegionFixedSlotLayerErrorCode.EntryBufferDigestMismatch,
                        "entryBufferPlan.bridge", "Entry-buffer identity must reference the exact bridge.");
            }

            if (request.CollisionPlan == null)
            {
                Add(errors, SpecialRegionFixedSlotLayerErrorCode.MissingInput,
                    "collisionPlan", "A MAP13_02 collision plan is required.");
            }
            else
            {
                var digest = SpecialRegionPlacementCollisionCanonicalDigest.Compute(request.CollisionPlan);
                if (!EqualsDigest(digest, request.CollisionPlan.CanonicalDigest) ||
                    !EqualsDigest(digest, request.ExpectedCollisionDigest))
                    Add(errors, SpecialRegionFixedSlotLayerErrorCode.CollisionDigestMismatch,
                        "collisionPlan", "Expected, published, and recomputed collision digests must match.");
            }

            if (request.SuppliedNullReplacementCount != 0)
                Add(errors, SpecialRegionFixedSlotLayerErrorCode.InvalidReplaceableSlot,
                    "replacements", "Null replacement intents are not canonical.");
        }

        private static List<SpecialRegionFixedCollisionCell> CompileFixedCollision(
            SpecialRegionFixedSlotLayerCompileRequest request,
            ICollection<SpecialRegionFixedSlotLayerError> errors)
        {
            var result = new List<SpecialRegionFixedCollisionCell>();
            var contract = request.ContractValidation.Contract;
            if (request.Bridge.FixedShellBindings.Count != contract.FixedShell.Count)
                Add(errors, SpecialRegionFixedSlotLayerErrorCode.InvalidFixedCell,
                    "fixedCollision", "Every MAP09 FixedShell cell must be projected exactly once.");

            var coordinates = new HashSet<SpecialRegionTileCoordinate>();
            var matched = new HashSet<int>();
            foreach (var binding in request.Bridge.FixedShellBindings)
            {
                var matches = contract.FixedShell.Select((value, index) => new { value, index })
                    .Where(value => value.value != null &&
                        string.Equals(value.value.ShellId, binding.ShellId, StringComparison.Ordinal) &&
                        value.value.SectorOffset == binding.Source.SectorOffset &&
                        value.value.Tile == binding.Source.LocalTile).ToArray();
                if (matches.Length != 1 || !matched.Add(matches.Length == 1 ? matches[0].index : -1) ||
                    !TryValidateProjection(request.Bridge, binding.Source, binding.Placed))
                {
                    Add(errors, SpecialRegionFixedSlotLayerErrorCode.InvalidFixedCell,
                        "fixedCollision/" + binding.ShellId,
                        "FixedShell source and placed projection must match the MAP09/MAP13_01 identity.");
                    continue;
                }

                var cell = new SpecialRegionFixedCollisionCell(binding.ShellId, binding.Source, binding.Placed);
                if (!coordinates.Add(cell.Coordinate))
                    Add(errors, SpecialRegionFixedSlotLayerErrorCode.DuplicateFixedOwner,
                        "fixedCollision/" + cell.Coordinate, "Fixed collision coordinates must be unique.");
                result.Add(cell);
            }

            if (matched.Count != contract.FixedShell.Count)
                Add(errors, SpecialRegionFixedSlotLayerErrorCode.InvalidFixedCell,
                    "fixedCollision", "FixedShell projection did not preserve exact cardinality.");
            return result;
        }

        private static List<SpecialRegionFixedAccessBinding> CompileFixedAccess(
            SpecialRegionFixedSlotLayerCompileRequest request,
            ICollection<SpecialRegionFixedSlotLayerError> errors)
        {
            var result = new Dictionary<SpecialRegionTileCoordinate, SpecialRegionFixedAccessBinding>();
            var selectedPorts = new[]
            {
                request.EntryBufferPlan.EntryPort,
                request.EntryBufferPlan.ReturnPort,
            };

            foreach (var apron in request.EntryBufferPlan.Aprons)
            {
                if (apron == null || apron.Cells.Count == 0)
                {
                    Add(errors, SpecialRegionFixedSlotLayerErrorCode.InvalidAccessBinding,
                        "fixedAccess/apron", "Published aprons must be non-empty.");
                    continue;
                }
                var port = selectedPorts.SingleOrDefault(value => value != null &&
                    string.Equals(value.PortId, apron.PortId, StringComparison.Ordinal));
                if (port == null)
                {
                    Add(errors, SpecialRegionFixedSlotLayerErrorCode.InvalidAccessBinding,
                        "fixedAccess/apron/" + apron.PortId, "Apron must belong to the selected Entry/Return pair.");
                    continue;
                }

                foreach (var cell in apron.Cells)
                {
                    if (!IsValidTile(cell) || result.ContainsKey(cell))
                    {
                        Add(errors, SpecialRegionFixedSlotLayerErrorCode.DuplicateFixedOwner,
                            "fixedAccess/" + cell, "FixedAccess coordinates must be unique across aprons.");
                        continue;
                    }
                    result.Add(cell, new SpecialRegionFixedAccessBinding(
                        SpecialRegionFixedAccessKind.Apron, apron.PortId, port.SlotId,
                        port.Kind, port.AccessClass, cell,
                        default(SpecialRegionAuthoredCoordinate),
                        default(SpecialRegionPlacedCoordinate), false));
                }
            }

            foreach (var port in selectedPorts)
            {
                if (port == null ||
                    (port.Kind != SpecialRegionSlotKind.Entry && port.Kind != SpecialRegionSlotKind.Return) ||
                    port.AccessClass != AccessClass.MandatoryNoTool)
                {
                    Add(errors, SpecialRegionFixedSlotLayerErrorCode.InvalidAccessBinding,
                        "fixedAccess/port", "Entry/Return ports must preserve MandatoryNoTool authority.");
                    continue;
                }
                var bridgePort = request.Bridge.PortBindings.SingleOrDefault(value => value != null &&
                    string.Equals(value.PortId, port.PortId, StringComparison.Ordinal));
                var coordinate = new SpecialRegionTileCoordinate(port.Placed.WorldSector, port.Placed.LocalTile);
                if (bridgePort == null || bridgePort.SlotId != port.SlotId || bridgePort.Kind != port.Kind ||
                    bridgePort.AccessClass != port.AccessClass || bridgePort.Placed != port.Placed ||
                    !TryValidateProjection(request.Bridge, bridgePort.Source, bridgePort.Placed) ||
                    !result.ContainsKey(coordinate))
                {
                    Add(errors, SpecialRegionFixedSlotLayerErrorCode.InvalidAccessBinding,
                        "fixedAccess/port/" + port.PortId,
                        "Port, matching slot, bridge coordinate, and internal apron must agree.");
                    continue;
                }
                result[coordinate] = new SpecialRegionFixedAccessBinding(
                    port.Kind == SpecialRegionSlotKind.Entry
                        ? SpecialRegionFixedAccessKind.Entry
                        : SpecialRegionFixedAccessKind.Return,
                    port.PortId, port.SlotId, port.Kind, port.AccessClass,
                    coordinate, bridgePort.Source, bridgePort.Placed, true);
            }
            return result.Values.ToList();
        }

        private static void ValidateFixedSeparation(
            IEnumerable<SpecialRegionFixedCollisionCell> fixedCollision,
            IEnumerable<SpecialRegionFixedAccessBinding> fixedAccess,
            ICollection<SpecialRegionFixedSlotLayerError> errors)
        {
            var collision = new HashSet<SpecialRegionTileCoordinate>(
                fixedCollision.Select(value => value.Coordinate));
            foreach (var access in fixedAccess)
                if (collision.Contains(access.Coordinate))
                    Add(errors, SpecialRegionFixedSlotLayerErrorCode.FixedAccessOverlap,
                        "fixedAccess/" + access.Coordinate,
                        "FixedCollision and FixedAccess ownership may not overlap.");
        }

        private static Dictionary<SpecialRegionSlotId, SpecialRegionSlotReplacementIntent> ValidateReplacements(
            SpecialRegionFixedSlotLayerCompileRequest request,
            ICollection<SpecialRegionFixedSlotLayerError> errors)
        {
            var result = new Dictionary<SpecialRegionSlotId, SpecialRegionSlotReplacementIntent>();
            var bridgeSlots = request.Bridge.SlotBindings.ToDictionary(value => value.SlotId);
            foreach (var replacement in request.Replacements)
            {
                if (replacement.SlotId.Value.Length == 0 || !result.TryAdd(replacement.SlotId, replacement))
                {
                    Add(errors, SpecialRegionFixedSlotLayerErrorCode.InvalidReplaceableSlot,
                        "replacements/" + replacement.SlotId.Value,
                        "Replacement intent must identify one unique slot.");
                    continue;
                }
                if (!bridgeSlots.TryGetValue(replacement.SlotId, out var source) ||
                    !IsReplaceableKind(source.Kind))
                {
                    Add(errors, SpecialRegionFixedSlotLayerErrorCode.ReplaceableKindMismatch,
                        "replacements/" + replacement.SlotId.Value,
                        "Entry/Return and unknown slots are not replaceable.");
                    continue;
                }
                if (!Enum.IsDefined(typeof(SpecialRegionSlotReplacementOperation), replacement.Operation) ||
                    (replacement.Operation == SpecialRegionSlotReplacementOperation.Clear &&
                     (replacement.OccupantId.Length != 0 ||
                      replacement.OccupantKind != default(SpecialRegionSlotKind))) ||
                    (replacement.Operation == SpecialRegionSlotReplacementOperation.Assign &&
                     (!SpecialRegionValidator.IsStableToken(replacement.OccupantId) ||
                      replacement.OccupantKind != source.Kind)))
                {
                    Add(errors, SpecialRegionFixedSlotLayerErrorCode.ReplaceableKindMismatch,
                        "replacements/" + replacement.SlotId.Value,
                        "Assign requires an exact-kind stable occupant; Clear carries no occupant.");
                }
            }
            return result;
        }

        private static List<SpecialRegionReplaceableSlotBinding> CompileReplaceableSlots(
            SpecialRegionFixedSlotLayerCompileRequest request,
            IReadOnlyDictionary<SpecialRegionSlotId, SpecialRegionSlotReplacementIntent> replacements,
            IEnumerable<SpecialRegionFixedCollisionCell> fixedCollision,
            IEnumerable<SpecialRegionFixedAccessBinding> fixedAccess,
            ICollection<SpecialRegionFixedSlotLayerError> errors)
        {
            var result = new List<SpecialRegionReplaceableSlotBinding>();
            var occupied = new HashSet<SpecialRegionTileCoordinate>(
                fixedCollision.Select(value => value.Coordinate));
            occupied.UnionWith(fixedAccess.Select(value => value.Coordinate));
            var slotCoordinates = new HashSet<SpecialRegionTileCoordinate>();
            var contractSlots = request.ContractValidation.Contract.Slots.ToDictionary(value => value.Id);
            var persistence = request.ContractValidation.Contract.Persistence.ToDictionary(value => value.Key);

            foreach (var source in request.Bridge.SlotBindings)
            {
                if (source.Kind == SpecialRegionSlotKind.Entry || source.Kind == SpecialRegionSlotKind.Return)
                    continue;
                if (!IsReplaceableKind(source.Kind) ||
                    !contractSlots.TryGetValue(source.SlotId, out var authored) ||
                    authored.Kind != source.Kind || authored.Required != source.Required ||
                    authored.PersistenceScope != source.PersistenceScope ||
                    authored.PersistenceKey != source.PersistenceKey ||
                    authored.SectorOffset != source.Source.SectorOffset ||
                    authored.Tile != source.Source.LocalTile ||
                    !TryValidateProjection(request.Bridge, source.Source, source.Placed))
                {
                    Add(errors, SpecialRegionFixedSlotLayerErrorCode.InvalidReplaceableSlot,
                        "replaceableSlots/" + source.SlotId.Value,
                        "Slot provenance must exactly match MAP09 and MAP13_01.");
                    continue;
                }

                ValidatePersistence(request.Bridge.RegionId, source, persistence, errors);
                replacements.TryGetValue(source.SlotId, out var replacement);
                var binding = new SpecialRegionReplaceableSlotBinding(source, replacement);
                if (!slotCoordinates.Add(binding.Coordinate))
                    Add(errors, SpecialRegionFixedSlotLayerErrorCode.InvalidReplaceableSlot,
                        "replaceableSlots/" + source.SlotId.Value,
                        "Replaceable slot coordinates must be unique.");
                if (occupied.Contains(binding.Coordinate))
                    Add(errors, SpecialRegionFixedSlotLayerErrorCode.SlotLayerOverlap,
                        "replaceableSlots/" + source.SlotId.Value,
                        "Replaceable slots may not overlap FixedCollision or FixedAccess.");
                result.Add(binding);
            }

            if (result.Count != request.Bridge.SlotBindings.Count(value => IsReplaceableKind(value.Kind)))
                Add(errors, SpecialRegionFixedSlotLayerErrorCode.InvalidReplaceableSlot,
                    "replaceableSlots", "All five replaceable slot kinds must preserve exact bridge cardinality.");
            return result;
        }

        private static void ValidatePersistence(
            SpecialRegionId regionId,
            SpecialRegionSiteSlotBinding source,
            IReadOnlyDictionary<SpecialPersistenceKey, SpecialPersistenceBinding> persistence,
            ICollection<SpecialRegionFixedSlotLayerError> errors)
        {
            if (source.PersistenceKey.Value.Length == 0)
            {
                if (source.Required && source.Kind == SpecialRegionSlotKind.Reward)
                    Add(errors, SpecialRegionFixedSlotLayerErrorCode.PersistenceKeyMismatch,
                        "replaceableSlots/" + source.SlotId.Value,
                        "Required Reward slots need a stable key.");
                return;
            }

            var expectedScope = ExpectedScope(source.Kind);
            if (source.PersistenceScope != expectedScope)
                Add(errors, SpecialRegionFixedSlotLayerErrorCode.PersistenceScopeMismatch,
                    "replaceableSlots/" + source.SlotId.Value,
                    "Persistence scope must match slot kind.");
            var expectedKey = SpecialPersistenceKey.ForSlot(
                regionId, source.PersistenceScope, source.SlotId);
            if (!persistence.TryGetValue(source.PersistenceKey, out var authored) ||
                authored.SlotId != source.SlotId || authored.Scope != source.PersistenceScope ||
                source.PersistenceKey != expectedKey)
                Add(errors, SpecialRegionFixedSlotLayerErrorCode.PersistenceKeyMismatch,
                    "replaceableSlots/" + source.SlotId.Value,
                    "Slot key must preserve one MAP09 persistence binding.");
        }

        private static IReadOnlyList<SpecialRegionOccupancyClaim> ValidateCollisionEvidence(
            SpecialRegionPlacementCollisionPlan collisionPlan,
            IEnumerable<SpecialRegionFixedCollisionCell> fixedCollision,
            IEnumerable<SpecialRegionFixedAccessBinding> fixedAccess,
            ICollection<SpecialRegionFixedSlotLayerError> errors)
        {
            var fixedCells = new HashSet<SpecialRegionTileCoordinate>(
                fixedCollision.Select(value => value.Coordinate));
            var accessCells = new HashSet<SpecialRegionTileCoordinate>(
                fixedAccess.Select(value => value.Coordinate));
            var layerCells = new HashSet<SpecialRegionTileCoordinate>(fixedCells);
            layerCells.UnionWith(accessCells);
            var coverage = layerCells.ToDictionary(value => value, value => 0);
            var accepted = new HashSet<string>(collisionPlan.AcceptedOwnerIds, StringComparer.Ordinal);
            var claims = new List<SpecialRegionOccupancyClaim>();

            foreach (var claim in collisionPlan.Claims)
            {
                var intersects = claim.Cells.Any(layerCells.Contains);
                if (!intersects) continue;
                var touchesFixed = claim.Cells.Any(fixedCells.Contains);
                var touchesAccess = claim.Cells.Any(accessCells.Contains);
                if (!claim.IsHardProtected || !accepted.Contains(claim.OwnerId) ||
                    claim.Cells.Any(value => !layerCells.Contains(value)) ||
                    (touchesFixed && touchesAccess))
                {
                    Add(errors, SpecialRegionFixedSlotLayerErrorCode.NonCanonicalPublication,
                        "collisionPlan/" + claim.OwnerId,
                        "Layer claims must be accepted, HardProtected, exact, and separated by ownership layer.");
                    continue;
                }
                foreach (var cell in claim.Cells) coverage[cell]++;
                claims.Add(claim);
            }

            foreach (var cell in coverage.Where(value => value.Value != 1))
                Add(errors, SpecialRegionFixedSlotLayerErrorCode.DuplicateFixedOwner,
                    "collisionPlan/" + cell.Key,
                    "Every FixedCollision/FixedAccess cell needs exactly one HardProtected owner.");
            return claims;
        }

        private static bool TryValidateProjection(
            SpecialRegionSiteBridge bridge,
            SpecialRegionAuthoredCoordinate source,
            SpecialRegionPlacedCoordinate placed)
        {
            return SpecialRegionSiteCoordinateTransformer.TryProject(
                       bridge.Width, bridge.Height, bridge.Transform, bridge.Origin,
                       source, out var expected) && expected == placed &&
                   bridge.PlacedFootprint.Contains(placed.SectorOffset);
        }

        private static bool IsValidTile(SpecialRegionTileCoordinate value)
            => value.WorldSector.X >= 0 && value.WorldSector.X < WorldGenConstants.SectorColumns &&
               value.WorldSector.Y >= 0 && value.WorldSector.Y < WorldGenConstants.SectorRows &&
               value.LocalTile.X >= 0 && value.LocalTile.X < WorldGenConstants.SectorWidthTiles &&
               value.LocalTile.Y >= 0 && value.LocalTile.Y < WorldGenConstants.SectorHeightTiles;

        private static bool IsReplaceableKind(SpecialRegionSlotKind kind)
            => kind == SpecialRegionSlotKind.Facility || kind == SpecialRegionSlotKind.Npc ||
               kind == SpecialRegionSlotKind.Enemy || kind == SpecialRegionSlotKind.Event ||
               kind == SpecialRegionSlotKind.Reward;

        private static SpecialPersistenceScope ExpectedScope(SpecialRegionSlotKind kind)
        {
            if (kind == SpecialRegionSlotKind.Reward) return SpecialPersistenceScope.Reward;
            if (kind == SpecialRegionSlotKind.Enemy || kind == SpecialRegionSlotKind.Event)
                return SpecialPersistenceScope.Encounter;
            return SpecialPersistenceScope.Slot;
        }

        private static bool EqualsDigest(string left, string right)
            => !string.IsNullOrEmpty(left) &&
               string.Equals(left, right, StringComparison.Ordinal);

        private static SpecialRegionFixedSlotLayerResult Failure(
            SpecialRegionFixedSlotLayerErrorCode code,
            string path)
            => new SpecialRegionFixedSlotLayerResult(null, new[]
            {
                new SpecialRegionFixedSlotLayerError(code, path, "Required input is missing."),
            });

        private static void Add(
            ICollection<SpecialRegionFixedSlotLayerError> errors,
            SpecialRegionFixedSlotLayerErrorCode code,
            string path,
            string detail)
            => errors.Add(new SpecialRegionFixedSlotLayerError(code, path, detail));
    }

    public static class SpecialRegionFixedSlotLayerCanonicalDigest
    {
        public static string Compute(SpecialRegionFixedSlotLayerPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var value = new StringBuilder();
            Append(value, "region", plan.RegionId.Value);
            Append(value, "regionKind", Number((int)plan.RegionKind));
            Append(value, "reservation", plan.ReservationId.Value);
            Append(value, "contract", plan.ContractDigest);
            Append(value, "bridge", plan.BridgeDigest);
            Append(value, "entryBuffer", plan.EntryBufferDigest);
            Append(value, "collision", plan.CollisionDigest);
            Append(value, "fixedCollision", ComputeFixedCollision(plan.FixedCollision));
            Append(value, "fixedAccess", ComputeFixedAccess(plan.FixedAccess));
            Append(value, "replaceableSlots", ComputeReplaceableSlots(plan.ReplaceableSlots));
            Append(value, "assignments", ComputeAssignments(plan.ReplaceableSlots));
            foreach (var claim in plan.HardProtectedClaims)
            {
                Append(value, "claim", claim.OwnerId + "/" + Number((int)claim.OwnerKind) + "/" +
                    Flag(claim.IsHardProtected) + "/" + Flag(claim.IsCommitted));
                foreach (var cell in claim.Cells) Append(value, "claimCell", claim.OwnerId + "/" + cell);
            }
            return Sha256(value.ToString());
        }

        public static string ComputeFixedCollision(
            IEnumerable<SpecialRegionFixedCollisionCell> cells)
        {
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            var value = new StringBuilder();
            foreach (var cell in cells.Where(item => item != null)
                         .OrderBy(item => item.Coordinate).ThenBy(item => item.ShellId, StringComparer.Ordinal))
                Append(value, "fixed", cell.ShellId + "/" +
                    Coordinates(cell.Source, cell.Placed) + "/" + Flag(cell.IsHardProtected));
            return Sha256(value.ToString());
        }

        public static string ComputeFixedAccess(
            IEnumerable<SpecialRegionFixedAccessBinding> bindings)
        {
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            var value = new StringBuilder();
            foreach (var binding in bindings.Where(item => item != null)
                         .OrderBy(item => item.Coordinate).ThenBy(item => item.AccessKind)
                         .ThenBy(item => item.PortId, StringComparer.Ordinal))
                Append(value, "access", Number((int)binding.AccessKind) + "/" + binding.PortId + "/" +
                    binding.SlotId.Value + "/" + Number((int)binding.SlotKind) + "/" +
                    Number((int)binding.AccessClass) + "/" + binding.Coordinate + "/" +
                    Flag(binding.HasSource) + "/" +
                    (binding.HasSource ? Coordinates(binding.Source, binding.Placed) : "-"));
            return Sha256(value.ToString());
        }

        public static string ComputeReplaceableSlots(
            IEnumerable<SpecialRegionReplaceableSlotBinding> bindings)
        {
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            var value = new StringBuilder();
            foreach (var binding in bindings.Where(item => item != null)
                         .OrderBy(item => item.Kind).ThenBy(item => item.SlotId))
                Append(value, "slot", SlotIdentityMaterial(binding));
            return Sha256(value.ToString());
        }

        public static string ComputeAssignments(
            IEnumerable<SpecialRegionReplaceableSlotBinding> bindings)
        {
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            var value = new StringBuilder();
            foreach (var binding in bindings.Where(item => item != null)
                         .OrderBy(item => item.Kind).ThenBy(item => item.SlotId))
                Append(value, "assignment", binding.SlotId.Value + "/" +
                    Number((int)binding.Operation) + "/" +
                    Number((int)binding.OccupantKind) + "/" + binding.OccupantId);
            return Sha256(value.ToString());
        }

        public static string ComputeImmutableLayer(SpecialRegionFixedSlotLayerPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            var value = new StringBuilder();
            Append(value, "contract", plan.ContractDigest);
            Append(value, "bridge", plan.BridgeDigest);
            Append(value, "entryBuffer", plan.EntryBufferDigest);
            Append(value, "collision", plan.CollisionDigest);
            Append(value, "fixedCollision", plan.FixedCollisionDigest);
            Append(value, "fixedAccess", plan.FixedAccessDigest);
            Append(value, "replaceableSlots", plan.ReplaceableSlotDigest);
            return Sha256(value.ToString());
        }

        public static string ComputeSlotIdentity(SpecialRegionReplaceableSlotBinding binding)
        {
            if (binding == null) throw new ArgumentNullException(nameof(binding));
            return Sha256(SlotIdentityMaterial(binding));
        }

        private static string SlotIdentityMaterial(SpecialRegionReplaceableSlotBinding binding)
            => binding.SlotId.Value + "/" + Number((int)binding.Kind) + "/" + Flag(binding.Required) + "/" +
               Number((int)binding.PersistenceScope) + "/" + binding.PersistenceKey.Value + "/" +
               Coordinates(binding.Source, binding.Placed);

        private static string Coordinates(
            SpecialRegionAuthoredCoordinate source,
            SpecialRegionPlacedCoordinate placed)
            => Coordinate(source.SectorOffset.X, source.SectorOffset.Y) + "/" +
               Coordinate(source.LocalTile.X, source.LocalTile.Y) + "/" + Side(source.Side) + "/" +
               Coordinate(placed.SectorOffset.X, placed.SectorOffset.Y) + "/" +
               Coordinate(placed.WorldSector.X, placed.WorldSector.Y) + "/" +
               Coordinate(placed.LocalTile.X, placed.LocalTile.Y) + "/" +
               Coordinate(placed.RegionTile.X, placed.RegionTile.Y) + "/" + Side(placed.Side);

        private static string Side(SiteEntrySide? side)
            => side.HasValue ? Number((int)side.Value) : "-";

        private static string Coordinate(int x, int y) => Number(x) + "," + Number(y);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        private static string Flag(bool value) => value ? "1" : "0";
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
