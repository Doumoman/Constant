using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.WorldGeneration.Diagnostics
{
    public enum SiteReservationOverlayDiagnosticClass
    {
        CandidateRejection,
        FinalGate,
        SoftCost
    }

    public enum SiteReservationOverlayDiagnosticKind
    {
        SearchFootprintOverlap,
        SearchBlocksExistingEntryApproach,
        SearchEntryApproachOccupied,
        SearchDistanceConstraint,
        SearchCoreCluster,
        VillageEntryOutsideWorld,
        VillageFootprintOverlap,
        VillageProtectedCoreWitness,
        VillageBlocksExistingEntryApproach,
        VillageEntryApproachOccupied,
        VillageOtherSiteDistance,
        VillageStartBucketDistance,
        CapacityShortfall,
        ValidationViolations,
        SelectedAltitudeSoftUnits,
        SelectedCapacityForecastSoftUnits
    }

    public sealed class SiteReservationOverlayDiagnosticRow
    {
        internal SiteReservationOverlayDiagnosticRow(
            SiteReservationOverlayDiagnosticKind kind,
            SiteReservationOverlayDiagnosticClass diagnosticClass,
            string key,
            string label,
            long value)
        {
            if (!Enum.IsDefined(typeof(SiteReservationOverlayDiagnosticKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (!Enum.IsDefined(typeof(SiteReservationOverlayDiagnosticClass), diagnosticClass))
                throw new ArgumentOutOfRangeException(nameof(diagnosticClass));
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("A diagnostic key is required.", nameof(key));
            if (string.IsNullOrEmpty(label)) throw new ArgumentException("A diagnostic label is required.", nameof(label));
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));

            Kind = kind;
            Class = diagnosticClass;
            Key = key;
            Label = label;
            Value = value;
        }

        public SiteReservationOverlayDiagnosticKind Kind { get; }
        public SiteReservationOverlayDiagnosticClass Class { get; }
        public string Key { get; }
        public string Label { get; }
        public long Value { get; }
    }

    public sealed class SiteReservationOverlayCell
    {
        private readonly IReadOnlyList<SiteEntrySide> entrySides;

        internal SiteReservationOverlayCell(
            SectorReservation sector,
            SiteReservation reservation,
            IEnumerable<SiteEntrySide> sourceEntrySides,
            SiteReservationId? coreWitnessOwnerId)
        {
            if (sector == null) throw new ArgumentNullException(nameof(sector));
            if (sourceEntrySides == null) throw new ArgumentNullException(nameof(sourceEntrySides));
            if (sector.Index < 0 || sector.Index >= WorldGenConstants.SectorCount ||
                sector.Coordinate != WorldGridIndex.ToCoordinate(sector.Index))
                throw new ArgumentException("Sector identity must match the frozen world grid.", nameof(sector));

            if (sector.IsReserved)
            {
                if (reservation == null || !sector.ReservationId.HasValue || !sector.Kind.HasValue ||
                    reservation.ReservationId != sector.ReservationId.Value ||
                    reservation.Kind != sector.Kind.Value ||
                    !reservation.TryGetFootprintCell(sector.Coordinate, out var footprintCell) ||
                    footprintCell.LocalX != sector.LocalX || footprintCell.LocalY != sector.LocalY ||
                    !string.Equals(footprintCell.LocalRole, sector.LocalRole, StringComparison.Ordinal))
                    throw new ArgumentException("Reserved sector projection does not match its reservation.", nameof(reservation));
            }
            else if (reservation != null || sector.ReservationId.HasValue || sector.Kind.HasValue ||
                     sector.LocalX != -1 || sector.LocalY != -1 ||
                     !string.IsNullOrEmpty(sector.LocalRole))
            {
                throw new ArgumentException("Unreserved sector projection contains reserved data.", nameof(sector));
            }

            var seen = new HashSet<SiteEntrySide>();
            foreach (var side in sourceEntrySides)
            {
                SiteReservationTokenCodec.ToToken(side);
                if (!seen.Add(side))
                    throw new ArgumentException("Entry sides must be unique.", nameof(sourceEntrySides));
            }

            var orderedSides = new List<SiteEntrySide>(4);
            var canonicalSides = new[]
            {
                SiteEntrySide.L, SiteEntrySide.R, SiteEntrySide.U, SiteEntrySide.D
            };
            foreach (var side in canonicalSides)
                if (seen.Contains(side)) orderedSides.Add(side);

            if (orderedSides.Count > 0 && !sector.IsReserved)
                throw new ArgumentException("Only reserved footprint cells can own entry arrows.", nameof(sourceEntrySides));
            if (coreWitnessOwnerId.HasValue && !coreWitnessOwnerId.Value.IsValid)
                throw new ArgumentException("Core witness owner must be valid.", nameof(coreWitnessOwnerId));

            Index = sector.Index;
            Coordinate = sector.Coordinate;
            IsReserved = sector.IsReserved;
            ReservationId = sector.ReservationId;
            Kind = sector.Kind;
            SourceDefinitionId = reservation == null ? string.Empty : reservation.SourceDefinitionId;
            LocalX = sector.LocalX;
            LocalY = sector.LocalY;
            LocalRole = sector.LocalRole;
            IsCoreWitness = coreWitnessOwnerId.HasValue;
            CoreWitnessOwnerId = coreWitnessOwnerId;
            entrySides = new ReadOnlyCollection<SiteEntrySide>(orderedSides);
            SiteGlyph = reservation == null ? string.Empty : GetSiteGlyph(reservation.SourceDefinitionId);
            CellLabel = IsReserved
                ? string.Format(CultureInfo.InvariantCulture, "{0}\n{1},{2}", SiteGlyph, LocalX, LocalY)
                : IsCoreWitness ? "+" : string.Empty;
            Tooltip = CreateTooltip();
        }

        public int Index { get; }
        public SectorCoord Coordinate { get; }
        public bool IsReserved { get; }
        public SiteReservationId? ReservationId { get; }
        public SiteReservationKind? Kind { get; }
        public string SourceDefinitionId { get; }
        public int LocalX { get; }
        public int LocalY { get; }
        public string LocalRole { get; }
        public bool IsCoreWitness { get; }
        public SiteReservationId? CoreWitnessOwnerId { get; }
        public IReadOnlyList<SiteEntrySide> EntrySides => entrySides;
        public string SiteGlyph { get; }
        public string CellLabel { get; }
        public string Tooltip { get; }

        internal static string GetSiteGlyph(string sourceDefinitionId)
        {
            switch (sourceDefinitionId)
            {
                case "WORLD_MOONPALACE_V1": return "A";
                case "SITE_MOON_BOSS_VAULT": return "B";
                case "SITE_MOON_SEAL_FORGE": return "F";
                case "SITE_CASSIA_SAP_HEART": return "C";
                case "SITE_DEEP_STAR_YEAST": return "Y";
                case "SITE_MOON_CORE_METEOR": return "M";
                case "SITE_PRIMARY_VILLAGE": return "V";
                default: throw new ArgumentException("Unknown site source definition ID.", nameof(sourceDefinitionId));
            }
        }

        private string CreateTooltip()
        {
            var reservationToken = ReservationId.HasValue ? ReservationId.Value.Value : "NONE";
            var sourceToken = IsReserved ? SourceDefinitionId : "NONE";
            var kindToken = Kind.HasValue ? SiteReservationTokenCodec.ToToken(Kind.Value) : "NONE";
            var localToken = IsReserved
                ? string.Format(CultureInfo.InvariantCulture, "{0},{1} / Role {2}", LocalX, LocalY, LocalRole)
                : "NONE";
            var entryToken = entrySides.Count == 0 ? "NONE" : JoinEntryTokens(entrySides);
            var witnessToken = CoreWitnessOwnerId.HasValue ? CoreWitnessOwnerId.Value.Value : "NONE";
            return string.Format(
                CultureInfo.InvariantCulture,
                "Sector: {0} / Index {1}\n" +
                "Reservation: {2}\n" +
                "Source/Kind: {3} / {4}\n" +
                "Local: {5}\n" +
                "Entry: {6}\n" +
                "Core Witness: {7}",
                Coordinate,
                Index,
                reservationToken,
                sourceToken,
                kindToken,
                localToken,
                entryToken,
                witnessToken);
        }

        private static string JoinEntryTokens(IReadOnlyList<SiteEntrySide> sides)
        {
            var tokens = new string[sides.Count];
            for (var index = 0; index < sides.Count; index++)
                tokens[index] = SiteReservationTokenCodec.ToToken(sides[index]);
            return string.Join("|", tokens);
        }
    }
}
