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
    public sealed class SpecialRegionSiteSectorBinding
    {
        public SpecialRegionSiteSectorBinding(
            SpecialRegionSectorOffset sourceOffset,
            SpecialRegionSectorOffset placedOffset,
            SectorCoord worldSector,
            int sectorIndex,
            string localRole)
        {
            SourceOffset = sourceOffset;
            PlacedOffset = placedOffset;
            WorldSector = worldSector;
            SectorIndex = sectorIndex;
            LocalRole = localRole ?? string.Empty;
        }

        public SpecialRegionSectorOffset SourceOffset { get; }
        public SpecialRegionSectorOffset PlacedOffset { get; }
        public SectorCoord WorldSector { get; }
        public int SectorIndex { get; }
        public string LocalRole { get; }
    }

    public sealed class SpecialRegionSiteFixedShellBinding
    {
        public SpecialRegionSiteFixedShellBinding(
            string shellId,
            SpecialRegionAuthoredCoordinate source,
            SpecialRegionPlacedCoordinate placed)
        {
            ShellId = shellId ?? string.Empty;
            Source = source;
            Placed = placed;
        }

        public string ShellId { get; }
        public SpecialRegionAuthoredCoordinate Source { get; }
        public SpecialRegionPlacedCoordinate Placed { get; }
    }

    public sealed class SpecialRegionSiteSlotBinding
    {
        public SpecialRegionSiteSlotBinding(
            SpecialRegionSlotId slotId,
            SpecialRegionSlotKind kind,
            bool required,
            SpecialPersistenceScope persistenceScope,
            SpecialPersistenceKey persistenceKey,
            SpecialRegionAuthoredCoordinate source,
            SpecialRegionPlacedCoordinate placed)
        {
            SlotId = slotId;
            Kind = kind;
            Required = required;
            PersistenceScope = persistenceScope;
            PersistenceKey = persistenceKey;
            Source = source;
            Placed = placed;
        }

        public SpecialRegionSlotId SlotId { get; }
        public SpecialRegionSlotKind Kind { get; }
        public bool Required { get; }
        public SpecialPersistenceScope PersistenceScope { get; }
        public SpecialPersistenceKey PersistenceKey { get; }
        public SpecialRegionAuthoredCoordinate Source { get; }
        public SpecialRegionPlacedCoordinate Placed { get; }
    }

    public sealed class SpecialRegionSitePortBinding
    {
        public SpecialRegionSitePortBinding(
            string portId,
            SpecialRegionSlotId slotId,
            SpecialRegionSlotKind kind,
            AccessClass accessClass,
            SpecialPersistenceKey persistenceKey,
            string entrySocketId,
            SectorCoord anchorExteriorSector,
            SpecialRegionAuthoredCoordinate source,
            SpecialRegionPlacedCoordinate placed)
        {
            PortId = portId ?? string.Empty;
            SlotId = slotId;
            Kind = kind;
            AccessClass = accessClass;
            PersistenceKey = persistenceKey;
            EntrySocketId = entrySocketId ?? string.Empty;
            AnchorExteriorSector = anchorExteriorSector;
            Source = source;
            Placed = placed;
        }

        public string PortId { get; }
        public SpecialRegionSlotId SlotId { get; }
        public SpecialRegionSlotKind Kind { get; }
        public AccessClass AccessClass { get; }
        public SpecialPersistenceKey PersistenceKey { get; }
        public string EntrySocketId { get; }
        public SectorCoord AnchorExteriorSector { get; }
        public SpecialRegionAuthoredCoordinate Source { get; }
        public SpecialRegionPlacedCoordinate Placed { get; }
    }

    public sealed class SpecialRegionSiteBridge
    {
        private readonly ReadOnlyCollection<SpecialRegionSectorOffset> sourceFootprint;
        private readonly ReadOnlyCollection<SpecialRegionSectorOffset> placedFootprint;
        private readonly ReadOnlyCollection<SpecialRegionSiteSectorBinding> sectorBindings;
        private readonly ReadOnlyCollection<SpecialRegionSiteFixedShellBinding> fixedShellBindings;
        private readonly ReadOnlyCollection<SpecialRegionSiteSlotBinding> slotBindings;
        private readonly ReadOnlyCollection<SpecialRegionSitePortBinding> portBindings;

        internal SpecialRegionSiteBridge(
            SpecialRegionId regionId,
            SpecialRegionKind regionKind,
            SiteReservationId reservationId,
            SiteReservationKind reservationKind,
            string reservationSourceDefinitionId,
            SectorCoord origin,
            int width,
            int height,
            SiteFootprintTransform transform,
            IEnumerable<SpecialRegionSectorOffset> sourceFootprint,
            IEnumerable<SpecialRegionSectorOffset> placedFootprint,
            IEnumerable<SpecialRegionSiteSectorBinding> sectorBindings,
            IEnumerable<SpecialRegionSiteFixedShellBinding> fixedShellBindings,
            IEnumerable<SpecialRegionSiteSlotBinding> slotBindings,
            IEnumerable<SpecialRegionSitePortBinding> portBindings,
            string reservationIdentityDigest,
            string contractDigest)
        {
            RegionId = regionId;
            RegionKind = regionKind;
            ReservationId = reservationId;
            ReservationKind = reservationKind;
            ReservationSourceDefinitionId = reservationSourceDefinitionId ?? string.Empty;
            Origin = origin;
            Width = width;
            Height = height;
            Transform = transform;
            this.sourceFootprint = Freeze(sourceFootprint, (left, right) => left.CompareTo(right));
            this.placedFootprint = Freeze(placedFootprint, (left, right) => left.CompareTo(right));
            this.sectorBindings = Freeze(sectorBindings, CompareSector);
            this.fixedShellBindings = Freeze(fixedShellBindings, CompareShell);
            this.slotBindings = Freeze(slotBindings, (left, right) => left.SlotId.CompareTo(right.SlotId));
            this.portBindings = Freeze(portBindings,
                (left, right) => string.Compare(left.PortId, right.PortId, StringComparison.Ordinal));
            ReservationIdentityDigest = reservationIdentityDigest ?? string.Empty;
            ContractDigest = contractDigest ?? string.Empty;
        }

        public SpecialRegionId RegionId { get; }
        public SpecialRegionKind RegionKind { get; }
        public SiteReservationId ReservationId { get; }
        public SiteReservationKind ReservationKind { get; }
        public string ReservationSourceDefinitionId { get; }
        public SectorCoord Origin { get; }
        public int Width { get; }
        public int Height { get; }
        public SiteFootprintTransform Transform { get; }
        public IReadOnlyList<SpecialRegionSectorOffset> SourceFootprint => sourceFootprint;
        public IReadOnlyList<SpecialRegionSectorOffset> PlacedFootprint => placedFootprint;
        public IReadOnlyList<SpecialRegionSiteSectorBinding> SectorBindings => sectorBindings;
        public IReadOnlyList<SpecialRegionSiteFixedShellBinding> FixedShellBindings => fixedShellBindings;
        public IReadOnlyList<SpecialRegionSiteSlotBinding> SlotBindings => slotBindings;
        public IReadOnlyList<SpecialRegionSitePortBinding> PortBindings => portBindings;
        public string ReservationIdentityDigest { get; }
        public string ContractDigest { get; }
        public string CanonicalDigest { get; internal set; }

        private static ReadOnlyCollection<T> Freeze<T>(IEnumerable<T> source, Comparison<T> comparison)
        {
            var values = source.ToArray();
            Array.Sort(values, comparison);
            return new ReadOnlyCollection<T>(values);
        }

        private static int CompareSector(
            SpecialRegionSiteSectorBinding left,
            SpecialRegionSiteSectorBinding right)
        {
            var value = left.PlacedOffset.CompareTo(right.PlacedOffset);
            return value != 0 ? value : left.SourceOffset.CompareTo(right.SourceOffset);
        }

        private static int CompareShell(
            SpecialRegionSiteFixedShellBinding left,
            SpecialRegionSiteFixedShellBinding right)
        {
            var value = left.Placed.SectorOffset.CompareTo(right.Placed.SectorOffset);
            if (value != 0) return value;
            value = left.Placed.LocalTile.Y.CompareTo(right.Placed.LocalTile.Y);
            if (value != 0) return value;
            value = left.Placed.LocalTile.X.CompareTo(right.Placed.LocalTile.X);
            return value != 0 ? value : string.Compare(left.ShellId, right.ShellId, StringComparison.Ordinal);
        }
    }

    public enum SpecialRegionSiteBridgeErrorCode
    {
        MissingInput = 1,
        InvalidReservation = 2,
        ReservationNotFound = 3,
        ReservationIdMismatch = 4,
        UnsupportedKind = 5,
        KindMismatch = 6,
        UnsupportedFootprint = 7,
        FootprintMismatch = 8,
        MissingSectorRow = 9,
        SectorRowMismatch = 10,
        CoordinateOutOfRange = 11,
        TransformMismatch = 12,
        MissingEntryAnchor = 13,
        PortAnchorMismatch = 14,
        PortNotOnExteriorEdge = 15,
        ContractValidationFailed = 16,
        NonCanonicalPublication = 17,
    }

    public sealed class SpecialRegionSiteBridgeError :
        IEquatable<SpecialRegionSiteBridgeError>, IComparable<SpecialRegionSiteBridgeError>
    {
        public SpecialRegionSiteBridgeError(
            SpecialRegionSiteBridgeErrorCode code,
            string path,
            string detail)
        {
            Code = code;
            Path = path ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        public SpecialRegionSiteBridgeErrorCode Code { get; }
        public string Path { get; }
        public string Detail { get; }

        public int CompareTo(SpecialRegionSiteBridgeError other)
        {
            if (other == null) return -1;
            var value = Code.CompareTo(other.Code);
            if (value != 0) return value;
            value = string.Compare(Path, other.Path, StringComparison.Ordinal);
            return value != 0 ? value : string.Compare(Detail, other.Detail, StringComparison.Ordinal);
        }

        public bool Equals(SpecialRegionSiteBridgeError other)
            => other != null && Code == other.Code &&
               string.Equals(Path, other.Path, StringComparison.Ordinal) &&
               string.Equals(Detail, other.Detail, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as SpecialRegionSiteBridgeError);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Code;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Path);
                return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Detail);
            }
        }

        public override string ToString() => Code + "|" + Path + "|" + Detail;
    }

    public sealed class SpecialRegionSiteBridgeResult
    {
        private readonly ReadOnlyCollection<SpecialRegionSiteBridgeError> errors;

        internal SpecialRegionSiteBridgeResult(
            SpecialRegionSiteBridge bridge,
            IEnumerable<SpecialRegionSiteBridgeError> errors)
        {
            var values = errors.Where(value => value != null).Distinct().OrderBy(value => value).ToArray();
            this.errors = new ReadOnlyCollection<SpecialRegionSiteBridgeError>(values);
            Bridge = values.Length == 0 ? bridge : null;
            CanonicalDigest = Bridge == null ? string.Empty : Bridge.CanonicalDigest;
        }

        public bool Succeeded => Bridge != null && errors.Count == 0;
        public SpecialRegionSiteBridge Bridge { get; }
        public IReadOnlyList<SpecialRegionSiteBridgeError> Errors => errors;
        public string CanonicalDigest { get; }
    }

    public static class SpecialRegionSiteBridgeCompiler
    {
        public static SpecialRegionSiteBridgeResult Compile(
            SiteReservationSnapshot snapshot,
            SpecialRegionValidationResult contractValidation)
        {
            if (contractValidation == null)
                return Failure(SpecialRegionSiteBridgeErrorCode.MissingInput, "contractValidation");
            if (!contractValidation.IsValid)
                return ContractFailure(contractValidation);
            return CompileCore(snapshot, contractValidation.Contract, contractValidation, true);
        }

        public static SpecialRegionSiteBridgeResult Compile(
            SiteReservationPublication publication,
            SpecialRegionValidationResult contractValidation)
        {
            if (publication == null)
                return Failure(SpecialRegionSiteBridgeErrorCode.MissingInput, "publication");
            if (!IsCanonical(publication))
                return Failure(SpecialRegionSiteBridgeErrorCode.NonCanonicalPublication, "publication");
            return Compile(publication.Snapshot, contractValidation);
        }

        public static SpecialRegionSiteBridgeResult Compile(
            SiteReservationValidationResult reservationValidation,
            SpecialRegionValidationResult contractValidation)
        {
            if (reservationValidation == null)
                return Failure(SpecialRegionSiteBridgeErrorCode.MissingInput, "reservationValidation");
            if (!reservationValidation.Succeeded || reservationValidation.Publication == null ||
                !IsCanonical(reservationValidation.Publication))
                return Failure(SpecialRegionSiteBridgeErrorCode.NonCanonicalPublication, "reservationValidation");
            return Compile(reservationValidation.Publication.Snapshot, contractValidation);
        }

        public static SpecialRegionSiteBridgeResult Compile(
            SiteReservationSnapshot snapshot,
            SpecialRegionContract contract)
        {
            if (snapshot == null)
                return Failure(SpecialRegionSiteBridgeErrorCode.MissingInput, "snapshot");
            if (contract == null)
                return Failure(SpecialRegionSiteBridgeErrorCode.MissingInput, "contract");

            SpecialRegionValidationResult validation = null;
            if (snapshot.TryGetReservation(contract.ReservationId, out var reservation))
                validation = SpecialRegionValidator.Validate(contract, reservation);
            return CompileCore(snapshot, contract, validation, false);
        }

        public static SpecialRegionSiteBridgeResult Compile(
            SiteReservationPublication publication,
            SpecialRegionContract contract)
        {
            if (publication == null)
                return Failure(SpecialRegionSiteBridgeErrorCode.MissingInput, "publication");
            if (!IsCanonical(publication))
                return Failure(SpecialRegionSiteBridgeErrorCode.NonCanonicalPublication, "publication");
            return Compile(publication.Snapshot, contract);
        }

        private static SpecialRegionSiteBridgeResult CompileCore(
            SiteReservationSnapshot snapshot,
            SpecialRegionContract contract,
            SpecialRegionValidationResult validation,
            bool validationIsAuthority)
        {
            var errors = new List<SpecialRegionSiteBridgeError>();
            if (snapshot == null)
                Add(errors, SpecialRegionSiteBridgeErrorCode.MissingInput, "snapshot", "Snapshot is required.");
            if (contract == null)
                Add(errors, SpecialRegionSiteBridgeErrorCode.MissingInput, "contract", "Contract is required.");
            if (snapshot == null || contract == null) return new SpecialRegionSiteBridgeResult(null, errors);

            if (validation == null || !validation.IsValid)
            {
                if (validation == null)
                    Add(errors, SpecialRegionSiteBridgeErrorCode.ContractValidationFailed,
                        "contract", "No valid contract publication was supplied.");
                else
                    foreach (var error in validation.Errors)
                        Add(errors, SpecialRegionSiteBridgeErrorCode.ContractValidationFailed,
                            error.Path, error.Code.ToString());
            }
            else
            {
                var digest = SpecialRegionCanonicalDigest.Compute(contract);
                if (!string.Equals(validation.CanonicalDigest, digest, StringComparison.Ordinal) ||
                    (validationIsAuthority && !ReferenceEquals(validation.Contract, contract)))
                    Add(errors, SpecialRegionSiteBridgeErrorCode.NonCanonicalPublication,
                        "contractValidation", "Validated contract identity or digest differs.");
            }

            if (!contract.ReservationId.IsValid)
                Add(errors, SpecialRegionSiteBridgeErrorCode.InvalidReservation,
                    "reservationId", "Reservation ID is invalid.");
            if (!snapshot.TryGetReservation(contract.ReservationId, out var reservation))
            {
                Add(errors, SpecialRegionSiteBridgeErrorCode.ReservationNotFound,
                    "reservation", contract.ReservationId.Value);
                return new SpecialRegionSiteBridgeResult(null, errors);
            }
            if (reservation.ReservationId != contract.ReservationId)
                Add(errors, SpecialRegionSiteBridgeErrorCode.ReservationIdMismatch,
                    "reservation", "Typed reservation identity differs.");

            ValidateKinds(reservation, contract, errors);
            var sourceOffsets = ValidateFootprint(reservation, contract, errors);
            var sectorBindings = BindSectors(snapshot, reservation, sourceOffsets, errors);
            var fixedBindings = BindFixedShell(reservation, contract, errors);
            var slotBindings = BindSlots(reservation, contract, errors);
            var portBindings = BindPorts(reservation, contract, errors);

            if (errors.Count != 0) return new SpecialRegionSiteBridgeResult(null, errors);

            var placedOffsets = sectorBindings.Select(value => value.PlacedOffset).ToArray();
            var reservationDigest = SpecialRegionSiteBridgeCanonicalDigest.ComputeReservationIdentity(
                reservation, sectorBindings);
            var contractDigest = SpecialRegionCanonicalDigest.Compute(contract);
            var bridge = new SpecialRegionSiteBridge(
                contract.Id, contract.Kind, reservation.ReservationId, reservation.Kind,
                reservation.SourceDefinitionId, reservation.Origin,
                reservation.Footprint.Width, reservation.Footprint.Height,
                reservation.Footprint.Transform, sourceOffsets, placedOffsets,
                sectorBindings, fixedBindings, slotBindings, portBindings,
                reservationDigest, contractDigest);
            bridge.CanonicalDigest = SpecialRegionSiteBridgeCanonicalDigest.Compute(bridge);
            return new SpecialRegionSiteBridgeResult(bridge, Array.Empty<SpecialRegionSiteBridgeError>());
        }

        private static void ValidateKinds(
            SiteReservation reservation,
            SpecialRegionContract contract,
            ICollection<SpecialRegionSiteBridgeError> errors)
        {
            if (reservation.Kind == SiteReservationKind.Start ||
                contract.Kind == SpecialRegionKind.OptionalLandmark ||
                !IsSupported(reservation.Kind) || !IsSupported(contract.Kind))
            {
                Add(errors, SpecialRegionSiteBridgeErrorCode.UnsupportedKind,
                    "kind", reservation.Kind + "/" + contract.Kind);
                return;
            }

            if (!KindsMatch(reservation.Kind, contract.Kind))
                Add(errors, SpecialRegionSiteBridgeErrorCode.KindMismatch,
                    "kind", reservation.Kind + "/" + contract.Kind);
        }

        private static IReadOnlyList<SpecialRegionSectorOffset> ValidateFootprint(
            SiteReservation reservation,
            SpecialRegionContract contract,
            ICollection<SpecialRegionSiteBridgeError> errors)
        {
            var supplied = contract.Footprint == null
                ? Array.Empty<SpecialRegionSectorOffset>()
                : contract.Footprint.Offsets.ToArray();
            var unique = new HashSet<SpecialRegionSectorOffset>(supplied);
            if (supplied.Length == 0 || unique.Count != supplied.Length ||
                unique.Any(value => value.X < 0 || value.Y < 0))
            {
                Add(errors, SpecialRegionSiteBridgeErrorCode.UnsupportedFootprint,
                    "footprint", "Offsets must be unique, normalized, and non-negative.");
                return unique.OrderBy(value => value).ToArray();
            }

            var width = unique.Max(value => value.X) + 1;
            var height = unique.Max(value => value.Y) + 1;
            var full = ((width == 1 && height == 1 && unique.Count == 1) ||
                        (width == 2 && height == 1 && unique.Count == 2) ||
                        (width == 1 && height == 2 && unique.Count == 2)) &&
                       unique.Contains(new SpecialRegionSectorOffset(0, 0));
            for (var y = 0; y < height && full; y++)
            for (var x = 0; x < width; x++)
                if (!unique.Contains(new SpecialRegionSectorOffset(x, y))) full = false;
            if (!full)
                Add(errors, SpecialRegionSiteBridgeErrorCode.UnsupportedFootprint,
                    "footprint", width + "x" + height + "/" + unique.Count);

            if (reservation.Footprint == null || reservation.Footprint.Width != width ||
                reservation.Footprint.Height != height || reservation.Footprint.Cells.Count != unique.Count)
                Add(errors, SpecialRegionSiteBridgeErrorCode.FootprintMismatch,
                    "footprint", "Reservation dimensions or cell count differ.");

            var transformed = new HashSet<SpecialRegionSectorOffset>();
            foreach (var source in unique)
            {
                if (!SiteFootprintTransformer.TryTransformCoordinate(
                        width, height, reservation.Footprint.Transform, source.X, source.Y,
                        out var x, out var y))
                {
                    Add(errors, SpecialRegionSiteBridgeErrorCode.TransformMismatch,
                        "footprint/" + source, "Sector transform failed.");
                    continue;
                }
                transformed.Add(new SpecialRegionSectorOffset(x, y));
            }

            var placed = new HashSet<SpecialRegionSectorOffset>(reservation.Footprint.Cells.Select(
                value => new SpecialRegionSectorOffset(value.LocalX, value.LocalY)));
            if (!transformed.SetEquals(placed))
                Add(errors, SpecialRegionSiteBridgeErrorCode.TransformMismatch,
                    "footprint", "Transformed source offsets differ from final reservation cells.");
            return unique.OrderBy(value => value).ToArray();
        }

        private static IReadOnlyList<SpecialRegionSiteSectorBinding> BindSectors(
            SiteReservationSnapshot snapshot,
            SiteReservation reservation,
            IEnumerable<SpecialRegionSectorOffset> sourceOffsets,
            ICollection<SpecialRegionSiteBridgeError> errors)
        {
            var bindings = new List<SpecialRegionSiteSectorBinding>();
            var expectedIndices = new HashSet<int>();
            foreach (var source in sourceOffsets.OrderBy(value => value))
            {
                var coordinate = new SpecialRegionAuthoredCoordinate(source, new LocalTileCoord(0, 0));
                if (!SpecialRegionSiteCoordinateTransformer.TryProject(reservation, coordinate, out var placed))
                {
                    Add(errors, SpecialRegionSiteBridgeErrorCode.CoordinateOutOfRange,
                        "sector/" + source, "Projection failed.");
                    continue;
                }

                var index = WorldGridIndex.ToIndex(placed.WorldSector);
                expectedIndices.Add(index);
                var row = snapshot.GetSector(index);
                if (row == null)
                {
                    Add(errors, SpecialRegionSiteBridgeErrorCode.MissingSectorRow,
                        "sector/" + index, "Expected row is missing.");
                    continue;
                }

                reservation.Footprint.TryGetCell(
                    placed.SectorOffset.X, placed.SectorOffset.Y, out var cell);
                if (!row.IsReserved || !row.ReservationId.HasValue || !row.Kind.HasValue ||
                    row.ReservationId.Value != reservation.ReservationId || row.Kind.Value != reservation.Kind ||
                    row.Coordinate != placed.WorldSector || row.Index != index ||
                    row.LocalX != placed.SectorOffset.X || row.LocalY != placed.SectorOffset.Y ||
                    cell == null || !string.Equals(row.LocalRole, cell.LocalRole, StringComparison.Ordinal))
                {
                    Add(errors, SpecialRegionSiteBridgeErrorCode.SectorRowMismatch,
                        "sector/" + index, "Reserved row identity differs.");
                    continue;
                }

                bindings.Add(new SpecialRegionSiteSectorBinding(
                    source, placed.SectorOffset, placed.WorldSector, index, row.LocalRole));
            }

            foreach (var row in snapshot.Sectors)
            {
                if (!row.ReservationId.HasValue || row.ReservationId.Value != reservation.ReservationId) continue;
                if (!expectedIndices.Contains(row.Index))
                    Add(errors, SpecialRegionSiteBridgeErrorCode.SectorRowMismatch,
                        "sector/" + row.Index, "Reservation owns an orphan row.");
            }
            if (bindings.Count != expectedIndices.Count)
                Add(errors, SpecialRegionSiteBridgeErrorCode.MissingSectorRow,
                    "sectors", "Not every footprint sector produced one binding.");
            return bindings;
        }

        private static IReadOnlyList<SpecialRegionSiteFixedShellBinding> BindFixedShell(
            SiteReservation reservation,
            SpecialRegionContract contract,
            ICollection<SpecialRegionSiteBridgeError> errors)
        {
            var bindings = new List<SpecialRegionSiteFixedShellBinding>();
            foreach (var cell in contract.FixedShell)
            {
                if (cell == null)
                {
                    Add(errors, SpecialRegionSiteBridgeErrorCode.ContractValidationFailed,
                        "fixedShell", "Null cell.");
                    continue;
                }
                var source = new SpecialRegionAuthoredCoordinate(cell.SectorOffset, cell.Tile);
                if (!SpecialRegionSiteCoordinateTransformer.TryProject(reservation, source, out var placed))
                    Add(errors, SpecialRegionSiteBridgeErrorCode.CoordinateOutOfRange,
                        "fixedShell/" + cell.ShellId, "Projection failed.");
                else bindings.Add(new SpecialRegionSiteFixedShellBinding(cell.ShellId, source, placed));
            }
            return bindings;
        }

        private static IReadOnlyList<SpecialRegionSiteSlotBinding> BindSlots(
            SiteReservation reservation,
            SpecialRegionContract contract,
            ICollection<SpecialRegionSiteBridgeError> errors)
        {
            var bindings = new List<SpecialRegionSiteSlotBinding>();
            foreach (var slot in contract.Slots)
            {
                if (slot == null)
                {
                    Add(errors, SpecialRegionSiteBridgeErrorCode.ContractValidationFailed,
                        "slots", "Null slot.");
                    continue;
                }
                var source = new SpecialRegionAuthoredCoordinate(slot.SectorOffset, slot.Tile);
                if (!SpecialRegionSiteCoordinateTransformer.TryProject(reservation, source, out var placed))
                    Add(errors, SpecialRegionSiteBridgeErrorCode.CoordinateOutOfRange,
                        "slots/" + slot.Id.Value, "Projection failed.");
                else bindings.Add(new SpecialRegionSiteSlotBinding(
                    slot.Id, slot.Kind, slot.Required, slot.PersistenceScope,
                    slot.PersistenceKey, source, placed));
            }
            return bindings;
        }

        private static IReadOnlyList<SpecialRegionSitePortBinding> BindPorts(
            SiteReservation reservation,
            SpecialRegionContract contract,
            ICollection<SpecialRegionSiteBridgeError> errors)
        {
            var bindings = new List<SpecialRegionSitePortBinding>();
            var slots = contract.Slots.Where(value => value != null)
                .GroupBy(value => value.Id).ToDictionary(value => value.Key, value => value.First());
            foreach (var port in contract.Ports)
            {
                if (port == null)
                {
                    Add(errors, SpecialRegionSiteBridgeErrorCode.ContractValidationFailed,
                        "ports", "Null port.");
                    continue;
                }

                var path = "ports/" + port.PortId;
                var source = new SpecialRegionAuthoredCoordinate(port.SectorOffset, port.Tile, port.Side);
                if (!SpecialRegionSiteCoordinateTransformer.TryProject(reservation, source, out var placed))
                {
                    Add(errors, SpecialRegionSiteBridgeErrorCode.CoordinateOutOfRange,
                        path, "Projection failed.");
                    continue;
                }
                if (!IsExteriorEdge(placed.LocalTile, placed.Side))
                    Add(errors, SpecialRegionSiteBridgeErrorCode.PortNotOnExteriorEdge,
                        path, "Placed tile is not on its declared side.");

                var matchingAnchors = reservation.EntryAnchors.Where(anchor =>
                    anchor.ReservationId == reservation.ReservationId &&
                    anchor.FootprintSector == placed.WorldSector &&
                    placed.Side.HasValue && anchor.Side == placed.Side.Value &&
                    (port.Kind != SpecialRegionSlotKind.Return || anchor.ReturnPathRequired)).ToArray();
                if (reservation.EntryAnchors.Count == 0)
                    Add(errors, SpecialRegionSiteBridgeErrorCode.MissingEntryAnchor,
                        path, "Reservation has no entry anchor.");
                else if (matchingAnchors.Length != 1)
                    Add(errors, SpecialRegionSiteBridgeErrorCode.PortAnchorMismatch,
                        path, "Placed sector and side must identify exactly one anchor.");

                if (!slots.TryGetValue(port.SlotId, out var slot) ||
                    slot.Kind != port.Kind || slot.SectorOffset != port.SectorOffset || slot.Tile != port.Tile)
                    Add(errors, SpecialRegionSiteBridgeErrorCode.PortAnchorMismatch,
                        path, "Port and slot identity differ.");

                if (matchingAnchors.Length != 1 ||
                    !matchingAnchors[0].TryGetExteriorSector(out var exteriorSector)) continue;
                bindings.Add(new SpecialRegionSitePortBinding(
                    port.PortId, port.SlotId, port.Kind, port.AccessClass,
                    slot == null ? default(SpecialPersistenceKey) : slot.PersistenceKey,
                    matchingAnchors[0].EntrySocketId, exteriorSector, source, placed));
            }
            return bindings;
        }

        private static bool IsExteriorEdge(LocalTileCoord tile, SiteEntrySide? side)
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

        private static bool IsCanonical(SiteReservationPublication publication)
        {
            var snapshot = publication.Snapshot;
            if (snapshot == null || publication.ReservationCount != snapshot.Reservations.Count ||
                publication.ReservationIds.Count != snapshot.Reservations.Count ||
                publication.ReservedSectorCount != snapshot.Sectors.Count(value => value.IsReserved) ||
                publication.EntryAnchorCount != snapshot.EntryAnchors.Count ||
                publication.CoreSeedCount != snapshot.CoreBiomeSeeds.Count) return false;
            for (var index = 0; index < snapshot.Reservations.Count; index++)
                if (publication.ReservationIds[index] != snapshot.Reservations[index].ReservationId) return false;
            return true;
        }

        private static bool IsSupported(SiteReservationKind kind)
            => kind == SiteReservationKind.Village || kind == SiteReservationKind.CoreResource ||
               kind == SiteReservationKind.Forge || kind == SiteReservationKind.Boss;

        private static bool IsSupported(SpecialRegionKind kind)
            => kind == SpecialRegionKind.Village || kind == SpecialRegionKind.CoreResource ||
               kind == SpecialRegionKind.Forge || kind == SpecialRegionKind.Boss;

        private static bool KindsMatch(SiteReservationKind reservation, SpecialRegionKind region)
            => (reservation == SiteReservationKind.Village && region == SpecialRegionKind.Village) ||
               (reservation == SiteReservationKind.CoreResource && region == SpecialRegionKind.CoreResource) ||
               (reservation == SiteReservationKind.Forge && region == SpecialRegionKind.Forge) ||
               (reservation == SiteReservationKind.Boss && region == SpecialRegionKind.Boss);

        private static SpecialRegionSiteBridgeResult ContractFailure(SpecialRegionValidationResult validation)
        {
            var errors = validation.Errors.Select(error => new SpecialRegionSiteBridgeError(
                SpecialRegionSiteBridgeErrorCode.ContractValidationFailed,
                error.Path, error.Code.ToString()));
            return new SpecialRegionSiteBridgeResult(null, errors.Any() ? errors : new[]
            {
                new SpecialRegionSiteBridgeError(
                    SpecialRegionSiteBridgeErrorCode.ContractValidationFailed,
                    "contractValidation", "Validation did not publish a contract.")
            });
        }

        private static SpecialRegionSiteBridgeResult Failure(
            SpecialRegionSiteBridgeErrorCode code,
            string path)
            => new SpecialRegionSiteBridgeResult(null, new[]
            {
                new SpecialRegionSiteBridgeError(code, path, "Required canonical input was not supplied.")
            });

        private static void Add(
            ICollection<SpecialRegionSiteBridgeError> errors,
            SpecialRegionSiteBridgeErrorCode code,
            string path,
            string detail)
            => errors.Add(new SpecialRegionSiteBridgeError(code, path, detail));
    }

    public static class SpecialRegionSiteBridgeCanonicalDigest
    {
        public static string Compute(SpecialRegionSiteBridge bridge)
        {
            if (bridge == null) throw new ArgumentNullException(nameof(bridge));
            var material = new StringBuilder();
            Append(material, "region", bridge.RegionId.Value);
            Append(material, "regionKind", Number((int)bridge.RegionKind));
            Append(material, "reservation", bridge.ReservationId.Value);
            Append(material, "reservationKind", Number((int)bridge.ReservationKind));
            Append(material, "sourceDefinition", bridge.ReservationSourceDefinitionId);
            Append(material, "origin", Coordinate(bridge.Origin.X, bridge.Origin.Y));
            Append(material, "dimensions", Coordinate(bridge.Width, bridge.Height));
            Append(material, "transform", Number((int)bridge.Transform));
            Append(material, "reservationDigest", bridge.ReservationIdentityDigest);
            Append(material, "contractDigest", bridge.ContractDigest);
            foreach (var binding in bridge.SectorBindings)
                Append(material, "sector", Coordinate(binding.SourceOffset.X, binding.SourceOffset.Y) + "/" +
                    Coordinate(binding.PlacedOffset.X, binding.PlacedOffset.Y) + "/" +
                    Coordinate(binding.WorldSector.X, binding.WorldSector.Y) + "/" +
                    Number(binding.SectorIndex) + "/" + binding.LocalRole);
            foreach (var binding in bridge.FixedShellBindings)
                Append(material, "shell", binding.ShellId + "/" + Coordinates(binding.Source, binding.Placed));
            foreach (var binding in bridge.SlotBindings)
                Append(material, "slot", binding.SlotId.Value + "/" + Number((int)binding.Kind) + "/" +
                    (binding.Required ? "1" : "0") + "/" + Number((int)binding.PersistenceScope) + "/" +
                    binding.PersistenceKey.Value + "/" + Coordinates(binding.Source, binding.Placed));
            foreach (var binding in bridge.PortBindings)
                Append(material, "port", binding.PortId + "/" + binding.SlotId.Value + "/" +
                    Number((int)binding.Kind) + "/" + Number((int)binding.AccessClass) + "/" +
                    binding.PersistenceKey.Value + "/" + binding.EntrySocketId + "/" +
                    Coordinate(binding.AnchorExteriorSector.X, binding.AnchorExteriorSector.Y) + "/" +
                    Coordinates(binding.Source, binding.Placed));
            return Sha256(material.ToString());
        }

        public static string ComputeReservationIdentity(
            SiteReservation reservation,
            IEnumerable<SpecialRegionSiteSectorBinding> sectorBindings)
        {
            if (reservation == null) throw new ArgumentNullException(nameof(reservation));
            if (sectorBindings == null) throw new ArgumentNullException(nameof(sectorBindings));
            var material = new StringBuilder();
            Append(material, "reservation", reservation.ReservationId.Value);
            Append(material, "kind", Number((int)reservation.Kind));
            Append(material, "sourceDefinition", reservation.SourceDefinitionId);
            Append(material, "origin", Coordinate(reservation.Origin.X, reservation.Origin.Y));
            Append(material, "dimensions", Coordinate(reservation.Footprint.Width, reservation.Footprint.Height));
            Append(material, "transform", Number((int)reservation.Footprint.Transform));
            Append(material, "order", Number(reservation.ReservationOrder));
            foreach (var binding in sectorBindings.OrderBy(value => value.SectorIndex))
                Append(material, "sector", Number(binding.SectorIndex) + "/" + binding.LocalRole);
            foreach (var anchor in reservation.EntryAnchors)
                Append(material, "anchor", anchor.EntrySocketId + "/" +
                    Coordinate(anchor.FootprintSector.X, anchor.FootprintSector.Y) + "/" +
                    Number((int)anchor.Side) + "/" + (anchor.Required ? "1" : "0") + "/" +
                    (anchor.ReturnPathRequired ? "1" : "0"));
            return Sha256(material.ToString());
        }

        private static string Coordinates(
            SpecialRegionAuthoredCoordinate source,
            SpecialRegionPlacedCoordinate placed)
            => Coordinate(source.SectorOffset.X, source.SectorOffset.Y) + "/" +
               Coordinate(source.LocalTile.X, source.LocalTile.Y) + "/" + Side(source.Side) + "/" +
               Coordinate(placed.SectorOffset.X, placed.SectorOffset.Y) + "/" +
               Coordinate(placed.WorldSector.X, placed.WorldSector.Y) + "/" +
               Coordinate(placed.LocalTile.X, placed.LocalTile.Y) + "/" +
               Coordinate(placed.RegionTile.X, placed.RegionTile.Y) + "/" + Side(placed.Side);

        private static string Side(SiteEntrySide? side) => side.HasValue ? Number((int)side.Value) : "-";
        private static string Coordinate(int x, int y) => Number(x) + "," + Number(y);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
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
