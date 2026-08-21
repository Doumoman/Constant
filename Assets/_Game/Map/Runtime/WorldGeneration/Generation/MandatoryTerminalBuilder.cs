using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryTerminalBuilder
    {
        private static readonly int[] RequiredRouteTypes = { 1, 2, 3 };
        private static readonly ExpectedReservation[] ExpectedReservations =
        {
            new ExpectedReservation(0, "RSV_00_WORLD_MOONPALACE_V1", "WORLD_MOONPALACE_V1", SiteReservationKind.Start),
            new ExpectedReservation(1, "RSV_01_SITE_MOON_BOSS_VAULT", "SITE_MOON_BOSS_VAULT", SiteReservationKind.Boss),
            new ExpectedReservation(2, "RSV_02_SITE_MOON_SEAL_FORGE", "SITE_MOON_SEAL_FORGE", SiteReservationKind.Forge),
            new ExpectedReservation(3, "RSV_03_SITE_CASSIA_SAP_HEART", "SITE_CASSIA_SAP_HEART", SiteReservationKind.CoreResource),
            new ExpectedReservation(4, "RSV_04_SITE_DEEP_STAR_YEAST", "SITE_DEEP_STAR_YEAST", SiteReservationKind.CoreResource),
            new ExpectedReservation(5, "RSV_05_SITE_MOON_CORE_METEOR", "SITE_MOON_CORE_METEOR", SiteReservationKind.CoreResource),
            new ExpectedReservation(6, "RSV_06_SITE_PRIMARY_VILLAGE", "SITE_PRIMARY_VILLAGE", SiteReservationKind.Village)
        };

        public MandatoryTerminalBuildResult Build(
            SiteReservationSnapshot siteSnapshot,
            BiomePatchValidationPublication biomePublication)
        {
            var errors = new List<MandatoryTerminalBuildError>();
            ValidateSiteSnapshot(siteSnapshot, errors);
            ValidateBiomePublication(biomePublication, errors);
            ValidateCrossSource(siteSnapshot, biomePublication, errors);

            var plans = errors.Count == 0
                ? BuildPlans(siteSnapshot, errors)
                : new List<TerminalPlan>();
            if (errors.Count != 0) return MandatoryTerminalBuildResult.Invalid(errors);

            var terminals = new List<MandatoryRouteTerminal>(plans.Count);
            foreach (var plan in plans)
            {
                terminals.Add(new MandatoryRouteTerminal(
                    plan.TerminalId, plan.Kind, plan.Order,
                    plan.Reservation.ReservationId, plan.Reservation.Kind,
                    plan.Reservation.SourceDefinitionId, plan.EntrySocketId,
                    plan.AnchorSector, plan.ApproachSector, plan.EntrySide,
                    RequiredRouteTypes, true, true));
            }

            var set = new MandatoryRouteTerminalSet(
                siteSnapshot.Seed, siteSnapshot, biomePublication, terminals);
            var reserved = 0;
            foreach (var sector in siteSnapshot.Sectors) if (sector.IsReserved) reserved++;
            var sharedApproaches = CountSharedApproaches(terminals);
            var diagnostics = new MandatoryTerminalBuildDiagnostics(
                siteSnapshot.Seed,
                siteSnapshot.Reservations.Count,
                reserved,
                biomePublication.Snapshot.Patches.Count,
                biomePublication.Snapshot.AssignedSectorCount,
                biomePublication.Snapshot.UnassignedSectorCount,
                terminals.Count,
                1,
                terminals.Count - 1,
                terminals.Count,
                terminals.Count,
                sharedApproaches,
                0,
                0);
            return MandatoryTerminalBuildResult.Completed(set, diagnostics);
        }

        internal static List<MandatoryTerminalBuildError> SortAndDedupeErrors(
            IEnumerable<MandatoryTerminalBuildError> source)
        {
            var values = new List<MandatoryTerminalBuildError>();
            foreach (var error in source) if (error != null) values.Add(error);
            values.Sort(CompareErrors);
            var result = new List<MandatoryTerminalBuildError>();
            foreach (var error in values)
                if (result.Count == 0 || CompareErrors(result[result.Count - 1], error) != 0)
                    result.Add(error);
            return result;
        }

        private static void ValidateSiteSnapshot(
            SiteReservationSnapshot snapshot,
            ICollection<MandatoryTerminalBuildError> errors)
        {
            if (snapshot == null)
            {
                Add(errors, MandatoryTerminalBuildErrorCode.MissingInput,
                    "SITE_SNAPSHOT", string.Empty, -1, "Site snapshot is required.");
                return;
            }

            if (snapshot.Sectors == null || snapshot.Sectors.Count != WorldGenConstants.SectorCount ||
                snapshot.CoreBiomeSeeds == null || snapshot.CoreBiomeSeeds.Count != 4)
                Add(errors, MandatoryTerminalBuildErrorCode.InvalidSiteSnapshot,
                    "SITE_SNAPSHOT", string.Empty, -1, "Site snapshot must contain 169 sectors and four Core seeds.");

            if (snapshot.Reservations == null || snapshot.Reservations.Count != ExpectedReservations.Length)
                Add(errors, MandatoryTerminalBuildErrorCode.ReservationCountMismatch,
                    Count(snapshot.Reservations), ExpectedReservations.Length.ToString(CultureInfo.InvariantCulture),
                    -1, "Reservation count must be exactly seven.");
            if (snapshot.EntryAnchors == null || snapshot.EntryAnchors.Count != 6)
                Add(errors, MandatoryTerminalBuildErrorCode.EntryCountMismatch,
                    Count(snapshot.EntryAnchors), "6", -1, "Entry count must be exactly six.");

            var reserved = 0;
            if (snapshot.Sectors != null)
                foreach (var sector in snapshot.Sectors) if (sector != null && sector.IsReserved) reserved++;
            if (reserved != 8)
                Add(errors, MandatoryTerminalBuildErrorCode.InvalidSiteSnapshot,
                    reserved.ToString(CultureInfo.InvariantCulture), "8", -1,
                    "Reserved sector count must be exactly eight.");

            var byOrder = new Dictionary<int, SiteReservation>();
            var prospectiveIds = new HashSet<string>(StringComparer.Ordinal);
            var reservationSockets = new HashSet<string>(StringComparer.Ordinal);
            if (snapshot.Reservations != null)
            {
                foreach (var reservation in snapshot.Reservations)
                {
                    if (reservation == null) continue;
                    if (!byOrder.TryAdd(reservation.ReservationOrder, reservation))
                        Add(errors, MandatoryTerminalBuildErrorCode.DuplicateTerminalIdentity,
                            reservation.ReservationOrder.ToString(CultureInfo.InvariantCulture), string.Empty, -1,
                            "Reservation order must be unique.");
                    foreach (var entry in reservation.EntryAnchors)
                    {
                        var identity = reservation.ReservationId.Value + "|" + entry.EntrySocketId;
                        if (!reservationSockets.Add(identity))
                            Add(errors, MandatoryTerminalBuildErrorCode.DuplicateTerminalIdentity,
                                reservation.ReservationId.Value, entry.EntrySocketId, -1,
                                "Reservation and socket identity must be unique.");
                    }
                }
            }

            foreach (var expected in ExpectedReservations)
            {
                if (!byOrder.TryGetValue(expected.Order, out var reservation))
                {
                    Add(errors, MandatoryTerminalBuildErrorCode.ReservationIdentityMismatch,
                        expected.ReservationId, string.Empty, -1, "Expected reservation order is missing.");
                    continue;
                }
                if (!string.Equals(reservation.ReservationId.Value, expected.ReservationId, StringComparison.Ordinal) ||
                    !string.Equals(reservation.SourceDefinitionId, expected.SourceDefinitionId, StringComparison.Ordinal) ||
                    reservation.Kind != expected.Kind)
                    Add(errors, MandatoryTerminalBuildErrorCode.ReservationIdentityMismatch,
                        reservation.ReservationId.Value, expected.ReservationId, -1,
                        "Reservation ID, source definition, kind, and order must match the approved P01 identity.");

                if (expected.Order == 0)
                {
                    if (reservation.EntryAnchors.Count != 0)
                        Add(errors, MandatoryTerminalBuildErrorCode.EntryCountMismatch,
                            reservation.ReservationId.Value, "0", -1, "Start must not contain entry anchors.");
                    if (!IsWorldSector(snapshot.StartAnchor) || snapshot.StartAnchor != reservation.Origin)
                        Add(errors, MandatoryTerminalBuildErrorCode.InvalidSiteSnapshot,
                            reservation.ReservationId.Value, string.Empty, -1, "Start anchor must be its world-bound origin.");
                    else
                    {
                        var sector = snapshot.GetSector(snapshot.StartAnchor);
                        if (!sector.IsReserved || !sector.ReservationId.HasValue ||
                            sector.ReservationId.Value != reservation.ReservationId)
                            Add(errors, MandatoryTerminalBuildErrorCode.InvalidSiteSnapshot,
                                reservation.ReservationId.Value, string.Empty, sector.Index,
                                "Start anchor must be occupied by the Start reservation.");
                    }
                    if (!prospectiveIds.Add("TERM_00_START"))
                        Add(errors, MandatoryTerminalBuildErrorCode.DuplicateTerminalIdentity,
                            "TERM_00_START", string.Empty, -1, "Terminal ID must be unique.");
                    continue;
                }

                if (reservation.EntryAnchors.Count != 1)
                {
                    Add(errors, MandatoryTerminalBuildErrorCode.EntryCountMismatch,
                        reservation.ReservationId.Value, "1", -1, "Each non-Start reservation requires exactly one entry.");
                    continue;
                }
                ValidateEntry(snapshot, reservation, reservation.EntryAnchors[0], errors);
                var terminalId = CreateSiteTerminalId(reservation, reservation.EntryAnchors[0]);
                if (!prospectiveIds.Add(terminalId.Value))
                    Add(errors, MandatoryTerminalBuildErrorCode.DuplicateTerminalIdentity,
                        terminalId.Value, string.Empty, -1, "Terminal ID must be unique.");
            }
        }

        private static void ValidateEntry(
            SiteReservationSnapshot snapshot,
            SiteReservation reservation,
            SiteEntryAnchor entry,
            ICollection<MandatoryTerminalBuildError> errors)
        {
            if (entry == null || entry.ReservationId != reservation.ReservationId ||
                !string.Equals(entry.EntrySocketId, "ENTRY_L", StringComparison.Ordinal) ||
                !reservation.TryGetFootprintCell(entry.FootprintSector, out _))
            {
                Add(errors, MandatoryTerminalBuildErrorCode.EntryIdentityMismatch,
                    reservation.ReservationId.Value, entry == null ? string.Empty : entry.EntrySocketId,
                    -1, "Entry reservation, socket, and footprint identity must match P01.");
                return;
            }
            if (!entry.Required || !entry.ReturnPathRequired || !RoutesAreExact(entry.AllowedRouteTypes))
                Add(errors, MandatoryTerminalBuildErrorCode.EntryIdentityMismatch,
                    reservation.ReservationId.Value, entry.EntrySocketId, WorldGridIndex.ToIndex(entry.FootprintSector),
                    "Entry flags and route types must be required, return-required, and exact 1|2|3.");
            if (!entry.TryGetExteriorSector(out var exterior) || !IsWorldSector(exterior))
            {
                Add(errors, MandatoryTerminalBuildErrorCode.EntryOutsideWorld,
                    reservation.ReservationId.Value, entry.EntrySocketId,
                    WorldGridIndex.ToIndex(entry.FootprintSector), "Entry exterior must remain inside the world.");
                return;
            }
            var exteriorRow = snapshot.GetSector(exterior);
            if (exteriorRow.IsReserved)
                Add(errors, MandatoryTerminalBuildErrorCode.EntryExteriorReserved,
                    reservation.ReservationId.Value,
                    exteriorRow.ReservationId.HasValue ? exteriorRow.ReservationId.Value.Value : string.Empty,
                    exteriorRow.Index, "Entry exterior must be an unreserved sector.");
        }

        private static void ValidateBiomePublication(
            BiomePatchValidationPublication publication,
            ICollection<MandatoryTerminalBuildError> errors)
        {
            if (publication == null)
            {
                Add(errors, MandatoryTerminalBuildErrorCode.MissingInput,
                    "BIOME_PUBLICATION", string.Empty, -1, "Biome publication is required.");
                return;
            }
            if (publication.SourceExport == null || publication.Snapshot == null || publication.Diagnostics == null ||
                MandatoryRouteTerminalSet.GetPublishedSiteSnapshot(publication) == null)
            {
                Add(errors, MandatoryTerminalBuildErrorCode.InvalidBiomePublication,
                    "BIOME_PUBLICATION", string.Empty, -1, "Approved P02 publication chain is incomplete.");
                return;
            }
            var diagnostics = publication.Diagnostics;
            var passedRules = 0;
            if (diagnostics.RuleResults != null)
                foreach (var rule in diagnostics.RuleResults) if (rule != null && rule.Passed) passedRules++;
            if (diagnostics.RuleResults == null || diagnostics.RuleResults.Count != 15 || passedRules != 15 ||
                diagnostics.Violations == null || diagnostics.Violations.Count != 0 ||
                diagnostics.PatchCount != 17 || diagnostics.AssignedSectorCount != 165 ||
                diagnostics.UnassignedSectorCount != 4 || publication.Snapshot.Patches.Count != 17 ||
                publication.Snapshot.AssignedSectorCount != 165 || publication.Snapshot.UnassignedSectorCount != 4)
                Add(errors, MandatoryTerminalBuildErrorCode.InvalidBiomePublication,
                    "BIOME_PUBLICATION", string.Empty, -1,
                    "Biome publication must be approved at 15/15 rules, 17 patches, and 165/4 sectors.");
        }

        private static void ValidateCrossSource(
            SiteReservationSnapshot siteSnapshot,
            BiomePatchValidationPublication biomePublication,
            ICollection<MandatoryTerminalBuildError> errors)
        {
            if (siteSnapshot == null || biomePublication == null || biomePublication.Snapshot == null ||
                biomePublication.Diagnostics == null) return;
            if (siteSnapshot.Seed != biomePublication.Snapshot.Seed ||
                siteSnapshot.Seed != biomePublication.Diagnostics.WorldSeed)
                Add(errors, MandatoryTerminalBuildErrorCode.WorldSeedMismatch,
                    siteSnapshot.Seed.ToString(CultureInfo.InvariantCulture),
                    biomePublication.Snapshot.Seed.ToString(CultureInfo.InvariantCulture), -1,
                    "Site and biome publications must share one world seed.");
            var publishedSite = MandatoryRouteTerminalSet.GetPublishedSiteSnapshot(biomePublication);
            if (publishedSite != null && !ReferenceEquals(publishedSite, siteSnapshot))
                Add(errors, MandatoryTerminalBuildErrorCode.SourceSnapshotMismatch,
                    "SITE_SNAPSHOT", "BIOME_SOURCE_SITE_SNAPSHOT", -1,
                    "Biome publication must preserve the exact input site snapshot reference.");
        }

        private static List<TerminalPlan> BuildPlans(
            SiteReservationSnapshot snapshot,
            ICollection<MandatoryTerminalBuildError> errors)
        {
            var result = new List<TerminalPlan>(7);
            foreach (var reservation in snapshot.Reservations)
            {
                if (reservation.ReservationOrder == 0)
                {
                    result.Add(new TerminalPlan(
                        new MandatoryRouteTerminalId("TERM_00_START"),
                        MandatoryRouteTerminalKind.Start, 0, reservation,
                        string.Empty, snapshot.StartAnchor, snapshot.StartAnchor, null));
                    continue;
                }
                var entry = reservation.EntryAnchors[0];
                if (!entry.TryGetExteriorSector(out var exterior))
                {
                    Add(errors, MandatoryTerminalBuildErrorCode.EntryOutsideWorld,
                        reservation.ReservationId.Value, entry.EntrySocketId, -1,
                        "Entry exterior must remain inside the world.");
                    continue;
                }
                result.Add(new TerminalPlan(
                    CreateSiteTerminalId(reservation, entry),
                    MandatoryRouteTerminalKind.SiteEntry,
                    reservation.ReservationOrder, reservation,
                    entry.EntrySocketId, entry.FootprintSector, exterior, entry.Side));
            }
            result.Sort((left, right) => left.Order.CompareTo(right.Order));
            return result;
        }

        private static MandatoryRouteTerminalId CreateSiteTerminalId(
            SiteReservation reservation,
            SiteEntryAnchor entry) =>
            new MandatoryRouteTerminalId(string.Concat(
                "TERM_", reservation.ReservationOrder.ToString("D2", CultureInfo.InvariantCulture), "_",
                reservation.SourceDefinitionId, "_", entry.EntrySocketId));

        private static bool RoutesAreExact(IReadOnlyList<int> values) =>
            values != null && values.Count == 3 && values[0] == 1 && values[1] == 2 && values[2] == 3;

        private static bool IsWorldSector(SectorCoord value) =>
            value.X >= 0 && value.X < WorldGenConstants.SectorColumns &&
            value.Y >= 0 && value.Y < WorldGenConstants.SectorRows;

        private static int CountSharedApproaches(IReadOnlyList<MandatoryRouteTerminal> terminals)
        {
            var counts = new Dictionary<SectorCoord, int>();
            foreach (var terminal in terminals)
            {
                if (terminal.Kind != MandatoryRouteTerminalKind.SiteEntry) continue;
                counts.TryGetValue(terminal.ApproachSector, out var count);
                counts[terminal.ApproachSector] = count + 1;
            }
            var shared = 0;
            foreach (var pair in counts) if (pair.Value > 1) shared++;
            return shared;
        }

        private static string Count<T>(IReadOnlyList<T> values) =>
            values == null ? "null" : values.Count.ToString(CultureInfo.InvariantCulture);

        private static void Add(
            ICollection<MandatoryTerminalBuildError> errors,
            MandatoryTerminalBuildErrorCode code,
            string firstId,
            string secondId,
            int sectorIndex,
            string message) =>
            errors.Add(new MandatoryTerminalBuildError(code, firstId, secondId, sectorIndex, message));

        private static int CompareErrors(MandatoryTerminalBuildError left, MandatoryTerminalBuildError right)
        {
            var value = left.Code.CompareTo(right.Code);
            if (value != 0) return value;
            value = string.Compare(left.FirstId, right.FirstId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = string.Compare(left.SecondId, right.SecondId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = left.SectorIndex.CompareTo(right.SectorIndex);
            if (value != 0) return value;
            return string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }

        private sealed class ExpectedReservation
        {
            public ExpectedReservation(int order, string reservationId, string sourceDefinitionId, SiteReservationKind kind)
            {
                Order = order;
                ReservationId = reservationId;
                SourceDefinitionId = sourceDefinitionId;
                Kind = kind;
            }
            public int Order { get; }
            public string ReservationId { get; }
            public string SourceDefinitionId { get; }
            public SiteReservationKind Kind { get; }
        }

        private sealed class TerminalPlan
        {
            public TerminalPlan(
                MandatoryRouteTerminalId terminalId,
                MandatoryRouteTerminalKind kind,
                int order,
                SiteReservation reservation,
                string entrySocketId,
                SectorCoord anchorSector,
                SectorCoord approachSector,
                SiteEntrySide? entrySide)
            {
                TerminalId = terminalId;
                Kind = kind;
                Order = order;
                Reservation = reservation;
                EntrySocketId = entrySocketId;
                AnchorSector = anchorSector;
                ApproachSector = approachSector;
                EntrySide = entrySide;
            }
            public MandatoryRouteTerminalId TerminalId { get; }
            public MandatoryRouteTerminalKind Kind { get; }
            public int Order { get; }
            public SiteReservation Reservation { get; }
            public string EntrySocketId { get; }
            public SectorCoord AnchorSector { get; }
            public SectorCoord ApproachSector { get; }
            public SiteEntrySide? EntrySide { get; }
        }
    }
}
