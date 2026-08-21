using System;
using System.Collections.Generic;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    internal sealed class SiteReservationSnapshotPublisher
    {
        private static readonly string[] ReservationIds =
        {
            "RSV_00_WORLD_MOONPALACE_V1",
            "RSV_01_SITE_MOON_BOSS_VAULT",
            "RSV_02_SITE_MOON_SEAL_FORGE",
            "RSV_03_SITE_CASSIA_SAP_HEART",
            "RSV_04_SITE_DEEP_STAR_YEAST",
            "RSV_05_SITE_MOON_CORE_METEOR",
            "RSV_06_SITE_PRIMARY_VILLAGE"
        };

        public SiteReservationPublication Publish(
            ulong worldSeed,
            VillageReservationApproval approval,
            IReadOnlyDictionary<string, SpecialMapDefinition> specialMaps)
        {
            if (approval == null) throw new ArgumentNullException(nameof(approval));
            if (specialMaps == null) throw new ArgumentNullException(nameof(specialMaps));

            var reservations = new List<SiteReservation>(7);
            var placements = approval.CoreCapacityApproval.SelectionPlan.SelectedPlacements;
            for (var order = 0; order < placements.Count; order++)
            {
                var placement = placements[order];
                var reservationId = new SiteReservationId(ReservationIds[order]);
                var anchors = new List<SiteEntryAnchor>();
                foreach (var entry in placement.Entries)
                {
                    anchors.Add(new SiteEntryAnchor(
                        reservationId,
                        entry.EntrySocketId,
                        entry.FootprintSector,
                        entry.Side,
                        entry.AllowedRouteTypes,
                        entry.Required,
                        entry.ReturnPathRequired));
                }

                var sourceId = placement.Candidate.SourceDefinitionId;
                var primaryBiomeId = order == 0
                    ? string.Empty
                    : specialMaps[sourceId].PrimaryBiomeId;
                reservations.Add(new SiteReservation(
                    reservationId,
                    placement.Candidate.Kind,
                    sourceId,
                    placement.Candidate.Origin,
                    placement.Footprint,
                    primaryBiomeId,
                    order,
                    anchors));
            }

            reservations.Add(CreateVillage(approval));
            var sectors = CreateSectorTable(reservations);
            var coreSeeds = CreateCoreSeeds(approval, reservations);
            var snapshot = new SiteReservationSnapshot(worldSeed, reservations, sectors, coreSeeds);
            return new SiteReservationPublication(approval, snapshot);
        }

        private static SiteReservation CreateVillage(VillageReservationApproval approval)
        {
            var selection = approval.Village;
            var candidate = selection.Candidate;
            var reservationId = new SiteReservationId(ReservationIds[6]);
            var entryFootprint = WorldGridIndex.ToCoordinate(candidate.EntryFootprintSectorIndex);
            var cells = new List<SiteFootprintCell>();
            for (var localY = 0; localY < candidate.FootprintHeightSectors; localY++)
            {
                for (var localX = 0; localX < candidate.FootprintWidthSectors; localX++)
                {
                    var sector = new SectorCoord(candidate.Origin.X + localX, candidate.Origin.Y + localY);
                    var sides = sector == entryFootprint
                        ? new[] { candidate.EntrySide }
                        : Array.Empty<SiteEntrySide>();
                    cells.Add(new SiteFootprintCell(
                        localX, localY, "VILLAGE", string.Empty, string.Empty, sides));
                }
            }

            var footprint = new SiteFootprint(
                candidate.FootprintWidthSectors,
                candidate.FootprintHeightSectors,
                SiteFootprintTransform.R0,
                cells);
            var template = selection.EntryTemplate;
            var anchor = new SiteEntryAnchor(
                reservationId,
                template.EntrySocketId,
                entryFootprint,
                candidate.EntrySide,
                template.AllowedRouteTypes,
                template.Required,
                template.ReturnPathRequired);
            return new SiteReservation(
                reservationId,
                SiteReservationKind.Village,
                selection.SpecialMap.SpecialMapId,
                candidate.Origin,
                footprint,
                string.Empty,
                6,
                new[] { anchor });
        }

        private static IReadOnlyList<SectorReservation> CreateSectorTable(
            IReadOnlyList<SiteReservation> reservations)
        {
            var owners = new Dictionary<int, SectorOwner>();
            foreach (var reservation in reservations)
            {
                foreach (var sector in reservation.OccupiedSectors)
                {
                    var index = WorldGridIndex.ToIndex(sector);
                    if (!reservation.TryGetFootprintCell(sector, out var cell))
                        throw new InvalidOperationException("Reservation footprint lookup is inconsistent.");
                    owners.Add(index, new SectorOwner(reservation, cell));
                }
            }

            var sectors = new List<SectorReservation>(WorldGenConstants.SectorCount);
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var coordinate = WorldGridIndex.ToCoordinate(index);
                if (owners.TryGetValue(index, out var owner))
                {
                    sectors.Add(SectorReservation.CreateReserved(
                        index,
                        coordinate,
                        owner.Reservation.ReservationId,
                        owner.Reservation.Kind,
                        owner.Cell.LocalX,
                        owner.Cell.LocalY,
                        owner.Cell.LocalRole));
                }
                else
                {
                    sectors.Add(SectorReservation.CreateUnreserved(index, coordinate));
                }
            }
            return sectors;
        }

        private static IReadOnlyList<CoreBiomeSeed> CreateCoreSeeds(
            VillageReservationApproval approval,
            IReadOnlyList<SiteReservation> reservations)
        {
            var result = new List<CoreBiomeSeed>(4);
            for (var index = 0; index < approval.CoreCapacityApproval.Witnesses.Count; index++)
            {
                var witness = approval.CoreCapacityApproval.Witnesses[index];
                var reservation = reservations[index + 2];
                result.Add(new CoreBiomeSeed(
                    reservation.ReservationId,
                    witness.BiomeId,
                    witness.CorePatchRuleId,
                    WorldGridIndex.ToCoordinate(witness.SeedSectorIndex),
                    witness.MinimumCoreSectorCount,
                    witness.BufferRingSectors));
            }
            return result;
        }

        private sealed class SectorOwner
        {
            public SectorOwner(SiteReservation reservation, SiteFootprintCell cell)
            {
                Reservation = reservation;
                Cell = cell;
            }

            public SiteReservation Reservation { get; }
            public SiteFootprintCell Cell { get; }
        }
    }
}
