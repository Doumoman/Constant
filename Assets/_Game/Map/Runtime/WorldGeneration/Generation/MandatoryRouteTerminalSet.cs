using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryRouteTerminalSet
    {
        private readonly IReadOnlyList<MandatoryRouteTerminal> terminals;
        private readonly IReadOnlyDictionary<MandatoryRouteTerminalId, MandatoryRouteTerminal> byId;
        private readonly IReadOnlyDictionary<SiteReservationId, MandatoryRouteTerminal> byReservation;

        internal MandatoryRouteTerminalSet(
            ulong worldSeed,
            SiteReservationSnapshot sourceSiteSnapshot,
            BiomePatchValidationPublication sourceBiomePublication,
            IEnumerable<MandatoryRouteTerminal> terminals)
        {
            SourceSiteSnapshot = sourceSiteSnapshot ?? throw new ArgumentNullException(nameof(sourceSiteSnapshot));
            SourceBiomePublication = sourceBiomePublication ?? throw new ArgumentNullException(nameof(sourceBiomePublication));
            if (terminals == null) throw new ArgumentNullException(nameof(terminals));
            if (sourceSiteSnapshot.Seed != worldSeed || sourceBiomePublication.Snapshot.Seed != worldSeed)
                throw new ArgumentException("Source world seeds must match.");
            if (!ReferenceEquals(GetPublishedSiteSnapshot(sourceBiomePublication), sourceSiteSnapshot))
                throw new ArgumentException("Biome publication must preserve the exact site snapshot.");

            var values = new List<MandatoryRouteTerminal>(terminals);
            values.Sort((left, right) => left.TerminalOrder.CompareTo(right.TerminalOrder));
            if (values.Count != 7) throw new ArgumentException("Exactly seven terminals are required.", nameof(terminals));

            var idLookup = new Dictionary<MandatoryRouteTerminalId, MandatoryRouteTerminal>();
            var reservationLookup = new Dictionary<SiteReservationId, MandatoryRouteTerminal>();
            MandatoryRouteTerminal start = null;
            for (var index = 0; index < values.Count; index++)
            {
                var terminal = values[index];
                if (terminal == null || terminal.TerminalOrder != index)
                    throw new ArgumentException("Terminal orders must be exact 0..6.", nameof(terminals));
                if (!idLookup.TryAdd(terminal.TerminalId, terminal) ||
                    !reservationLookup.TryAdd(terminal.ReservationId, terminal))
                    throw new ArgumentException("Terminal IDs and reservation identities must be unique.", nameof(terminals));
                if (terminal.Kind == MandatoryRouteTerminalKind.Start)
                {
                    if (start != null) throw new ArgumentException("Exactly one Start terminal is required.", nameof(terminals));
                    start = terminal;
                }
                else
                {
                    var sector = sourceSiteSnapshot.GetSector(terminal.ApproachSector);
                    if (sector.IsReserved)
                        throw new ArgumentException("Site-entry approaches must be unreserved.", nameof(terminals));
                }
            }
            if (start == null || values.FindAll(value => value.Kind == MandatoryRouteTerminalKind.SiteEntry).Count != 6)
                throw new ArgumentException("One Start and six SiteEntry terminals are required.", nameof(terminals));

            WorldSeed = worldSeed;
            StartTerminal = start;
            this.terminals = new ReadOnlyCollection<MandatoryRouteTerminal>(values);
            byId = new ReadOnlyDictionary<MandatoryRouteTerminalId, MandatoryRouteTerminal>(idLookup);
            byReservation = new ReadOnlyDictionary<SiteReservationId, MandatoryRouteTerminal>(reservationLookup);
        }

        public ulong WorldSeed { get; }
        public SiteReservationSnapshot SourceSiteSnapshot { get; }
        public BiomePatchValidationPublication SourceBiomePublication { get; }
        public MandatoryRouteTerminal StartTerminal { get; }
        public IReadOnlyList<MandatoryRouteTerminal> Terminals => terminals;
        public int TerminalCount => terminals.Count;
        public int SiteEntryTerminalCount => terminals.Count - 1;
        public bool TryGet(MandatoryRouteTerminalId id, out MandatoryRouteTerminal terminal) => byId.TryGetValue(id, out terminal);
        public bool TryGetByReservation(SiteReservationId id, out MandatoryRouteTerminal terminal) => byReservation.TryGetValue(id, out terminal);

        internal static SiteReservationSnapshot GetPublishedSiteSnapshot(BiomePatchValidationPublication publication)
        {
            if (publication == null || publication.SourceExport == null || publication.SourceExport.SourceCleanup == null ||
                publication.SourceExport.SourceCleanup.SourceIntrusion == null ||
                publication.SourceExport.SourceCleanup.SourceIntrusion.Publication == null)
                return null;
            return publication.SourceExport.SourceCleanup.SourceIntrusion.Publication.SourceSiteSnapshot;
        }
    }
}
