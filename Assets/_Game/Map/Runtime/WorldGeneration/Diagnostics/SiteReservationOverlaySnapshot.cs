using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.WorldGeneration.Diagnostics
{
    public sealed class SiteReservationOverlaySnapshot
    {
        private readonly IReadOnlyList<SiteReservationOverlayCell> cells;
        private readonly IReadOnlyList<SiteReservationOverlayDiagnosticRow> diagnosticRows;

        private SiteReservationOverlaySnapshot(
            ulong seed,
            IReadOnlyList<SiteReservationOverlayCell> sourceCells,
            IReadOnlyList<SiteReservationOverlayDiagnosticRow> sourceRows,
            int reservationCount,
            int reservedSectorCount,
            int entryArrowCount,
            int coreWitnessCount,
            int coreWitnessSectorCount,
            int passedValidationRuleCount)
        {
            Seed = seed;
            cells = sourceCells;
            diagnosticRows = sourceRows;
            ReservationCount = reservationCount;
            ReservedSectorCount = reservedSectorCount;
            EntryArrowCount = entryArrowCount;
            CoreWitnessCount = coreWitnessCount;
            CoreWitnessSectorCount = coreWitnessSectorCount;
            PassedValidationRuleCount = passedValidationRuleCount;
        }

        public ulong Seed { get; }
        public IReadOnlyList<SiteReservationOverlayCell> Cells => cells;
        public IReadOnlyList<SiteReservationOverlayDiagnosticRow> DiagnosticRows => diagnosticRows;
        public int Count => cells.Count;
        public int ReservationCount { get; }
        public int ReservedSectorCount { get; }
        public int EntryArrowCount { get; }
        public int CoreWitnessCount { get; }
        public int CoreWitnessSectorCount { get; }
        public int PassedValidationRuleCount { get; }

        public SiteReservationOverlayCell GetCell(int index)
        {
            if (!TryGetCell(index, out var cell))
                throw new ArgumentOutOfRangeException(nameof(index));
            return cell;
        }

        public SiteReservationOverlayCell GetCell(SectorCoord coordinate)
        {
            return GetCell(WorldGridIndex.ToIndex(coordinate));
        }

        public bool TryGetCell(int index, out SiteReservationOverlayCell cell)
        {
            if (index < 0 || index >= cells.Count)
            {
                cell = null;
                return false;
            }

            cell = cells[index];
            return true;
        }

        public static SiteReservationOverlaySnapshot Create(
            SiteReservationPublication publication,
            SiteReservationSearchDiagnostics searchDiagnostics,
            CoreCapacityFloodDiagnostics capacityDiagnostics,
            VillageReservationDiagnostics villageDiagnostics,
            SiteReservationValidationDiagnostics validationDiagnostics)
        {
            if (publication == null) throw new ArgumentNullException(nameof(publication));
            if (searchDiagnostics == null) throw new ArgumentNullException(nameof(searchDiagnostics));
            if (capacityDiagnostics == null) throw new ArgumentNullException(nameof(capacityDiagnostics));
            if (villageDiagnostics == null) throw new ArgumentNullException(nameof(villageDiagnostics));
            if (validationDiagnostics == null) throw new ArgumentNullException(nameof(validationDiagnostics));

            var sourceApproval = publication.SourceApproval;
            var sourceSnapshot = publication.Snapshot;
            if (sourceApproval == null || sourceSnapshot == null ||
                sourceApproval.CoreCapacityApproval == null ||
                sourceApproval.CoreCapacityApproval.SelectionPlan == null ||
                sourceApproval.Village == null)
                throw new ArgumentException("Publication approval identity is incomplete.", nameof(publication));

            ValidatePublication(publication, validationDiagnostics);
            ValidateSearch(sourceApproval.CoreCapacityApproval.SelectionPlan, searchDiagnostics);
            ValidateCapacity(sourceApproval.CoreCapacityApproval, capacityDiagnostics);
            ValidateVillage(sourceApproval.Village, villageDiagnostics);

            var entriesByIndex = CreateEntryLookup(sourceSnapshot);
            var witnessOwners = CreateWitnessOwnerLookup(publication);
            var copiedCells = new List<SiteReservationOverlayCell>(WorldGenConstants.SectorCount);
            var entryArrowCount = 0;
            for (var index = 0; index < WorldGenConstants.SectorCount; index++)
            {
                var sector = sourceSnapshot.GetSector(index);
                SiteReservation reservation = null;
                if (sector.IsReserved)
                {
                    if (!sector.ReservationId.HasValue ||
                        !sourceSnapshot.TryGetReservation(sector.ReservationId.Value, out reservation))
                        throw new ArgumentException("Reserved sector owner is missing.", nameof(publication));
                }

                SiteReservationId? witnessOwner = witnessOwners[index].HasValue
                    ? witnessOwners[index]
                    : null;
                if (reservation != null && witnessOwner.HasValue &&
                    reservation.ReservationId != witnessOwner.Value)
                    throw new ArgumentException("A reserved Core witness cell must belong to its witness owner.", nameof(publication));

                checked { entryArrowCount += entriesByIndex[index].Count; }
                copiedCells.Add(new SiteReservationOverlayCell(
                    sector,
                    reservation,
                    entriesByIndex[index],
                    witnessOwner));
            }

            if (entryArrowCount != 6)
                throw new ArgumentException("The overlay requires exactly six entry arrows.", nameof(publication));

            var rows = CreateDiagnosticRows(
                sourceApproval.CoreCapacityApproval.SelectionPlan,
                searchDiagnostics,
                capacityDiagnostics,
                villageDiagnostics,
                validationDiagnostics);
            var passedRules = 0;
            foreach (var rule in validationDiagnostics.Rules)
                if (rule.Passed) passedRules++;

            return new SiteReservationOverlaySnapshot(
                sourceSnapshot.Seed,
                new ReadOnlyCollection<SiteReservationOverlayCell>(copiedCells),
                new ReadOnlyCollection<SiteReservationOverlayDiagnosticRow>(rows),
                publication.ReservationCount,
                publication.ReservedSectorCount,
                entryArrowCount,
                sourceApproval.CoreCapacityApproval.CapacitySiteCount,
                sourceApproval.CoreCapacityApproval.TotalWitnessSectorCount,
                passedRules);
        }

        private static void ValidatePublication(
            SiteReservationPublication publication,
            SiteReservationValidationDiagnostics validation)
        {
            var snapshot = publication.Snapshot;
            if (publication.ReservationCount != 7 || publication.ReservedSectorCount != 8 ||
                publication.EntryAnchorCount != 6 || publication.CoreSeedCount != 4 ||
                snapshot.Reservations.Count != 7 || snapshot.Sectors.Count != WorldGenConstants.SectorCount ||
                snapshot.EntryAnchors.Count != 6 || snapshot.CoreBiomeSeeds.Count != 4)
                throw new ArgumentException("A completed 7/169/8/6/4 publication is required.", nameof(publication));

            for (var index = 0; index < 7; index++)
            {
                var requiredSource = GetRequiredSource(index);
                var reservation = snapshot.Reservations[index];
                if (reservation.ReservationOrder != index ||
                    !string.Equals(reservation.SourceDefinitionId, requiredSource, StringComparison.Ordinal) ||
                    !publication.TryGetReservationBySourceId(requiredSource, out var sourceReservation) ||
                    !ReferenceEquals(reservation, sourceReservation))
                    throw new ArgumentException("Publication sources must use the exact frozen order and identity.", nameof(publication));
                SiteReservationOverlayCell.GetSiteGlyph(reservation.SourceDefinitionId);
            }

            var passedRules = 0;
            foreach (var rule in validation.Rules)
                if (rule.Passed) passedRules++;
            if (validation.Rules.Count != 6 || passedRules != 6 || validation.ViolationCount != 0 ||
                validation.ReservationCount != 7 || validation.ReservedSectorCount != 8 ||
                validation.UnreservedSectorCount != 161 || validation.EntryAnchorCount != 6 ||
                validation.RequiredEntryCount != 6 || validation.NonVillageDistanceConstraintCount != 15 ||
                validation.VillageDistanceCheckCount != 6 || validation.CoreClusterCheckCount != 1 ||
                validation.CoreWitnessCount != 4 || validation.CoreWitnessSectorCount != 20 ||
                validation.CoreSeedCount != 4)
                throw new ArgumentException("Validation diagnostics do not describe the completed publication.", nameof(validation));
        }

        private static void ValidateSearch(
            SiteReservationSelectionPlan plan,
            SiteReservationSearchDiagnostics diagnostics)
        {
            if (plan.Steps.Count != 6 || diagnostics.Groups.Count != 6 ||
                plan.SelectedCount != 6 || diagnostics.DeepestSelectedDepth != 6)
                throw new ArgumentException("Search diagnostics require the completed six-step plan.", nameof(diagnostics));
            for (var index = 0; index < 6; index++)
                if (diagnostics.Groups[index].Key != plan.Steps[index].Key)
                    throw new ArgumentException("Search diagnostic keys must match the selected plan.", nameof(diagnostics));
        }

        private static void ValidateCapacity(
            CoreCapacityApproval approval,
            CoreCapacityFloodDiagnostics diagnostics)
        {
            if (approval.Witnesses.Count != 4 || approval.TotalWitnessSectorCount != 20 ||
                diagnostics.Sites.Count != 4 || diagnostics.CapacitySiteCount != 4 ||
                diagnostics.SelectedPlacementCount != 6 || diagnostics.TotalWitnessSectorCount != 20)
                throw new ArgumentException("Capacity diagnostics require the completed four-witness result.", nameof(diagnostics));

            var expectedFootprints = 0;
            foreach (var placement in approval.SelectionPlan.SelectedPlacements)
                checked { expectedFootprints += placement.OccupiedSectors.Count; }
            if (diagnostics.ReservedFootprintSectorCount != expectedFootprints)
                throw new ArgumentException("Capacity reserved-footprint count does not match the plan.", nameof(diagnostics));

            for (var index = 0; index < 4; index++)
            {
                var witness = approval.Witnesses[index];
                var site = diagnostics.Sites[index];
                if (!string.Equals(witness.Key.SourceDefinitionId, GetRequiredWitnessSource(index), StringComparison.Ordinal) ||
                    site.Key != witness.Key || site.WitnessSectorCount != witness.WitnessSectorIndices.Count ||
                    witness.WitnessSectorIndices.Count != 5)
                    throw new ArgumentException("Capacity witness identity or count is inconsistent.", nameof(diagnostics));
            }
        }

        private static void ValidateVillage(
            VillageReservationSelection selection,
            VillageReservationDiagnostics diagnostics)
        {
            if (!ReferenceEquals(selection.DistanceBucket, diagnostics.SelectedBucket) ||
                diagnostics.RngMethodCallCount != 3)
                throw new ArgumentException("Village diagnostics do not belong to the selected attempt.", nameof(diagnostics));
            var foundLayout = false;
            foreach (var layout in diagnostics.Layouts)
                if (string.Equals(layout.LayoutId, selection.Layout.VillageLayoutId, StringComparison.Ordinal))
                    foundLayout = true;
            if (!foundLayout)
                throw new ArgumentException("Village diagnostics do not contain the selected layout.", nameof(diagnostics));
        }

        private static List<SiteEntrySide>[] CreateEntryLookup(SiteReservationSnapshot snapshot)
        {
            var result = new List<SiteEntrySide>[WorldGenConstants.SectorCount];
            for (var index = 0; index < result.Length; index++) result[index] = new List<SiteEntrySide>();
            foreach (var entry in snapshot.EntryAnchors)
            {
                var index = WorldGridIndex.ToIndex(entry.FootprintSector);
                var sector = snapshot.GetSector(index);
                if (!sector.IsReserved || !sector.ReservationId.HasValue ||
                    sector.ReservationId.Value != entry.ReservationId || result[index].Contains(entry.Side))
                    throw new ArgumentException("Entry arrows must match unique occupied footprint edges.", nameof(snapshot));
                result[index].Add(entry.Side);
            }
            foreach (var sides in result) sides.Sort(CompareEntrySides);
            return result;
        }

        private static SiteReservationId?[] CreateWitnessOwnerLookup(
            SiteReservationPublication publication)
        {
            var result = new SiteReservationId?[WorldGenConstants.SectorCount];
            var witnesses = publication.SourceApproval.CoreCapacityApproval.Witnesses;
            var total = 0;
            for (var witnessIndex = 0; witnessIndex < witnesses.Count; witnessIndex++)
            {
                var witness = witnesses[witnessIndex];
                if (!string.Equals(witness.Key.SourceDefinitionId, GetRequiredWitnessSource(witnessIndex), StringComparison.Ordinal) ||
                    !publication.TryGetReservationBySourceId(witness.Key.SourceDefinitionId, out var owner) ||
                    witness.WitnessSectorIndices.Count != 5)
                    throw new ArgumentException("Core witness owner identity is invalid.", nameof(publication));
                foreach (var sectorIndex in witness.WitnessSectorIndices)
                {
                    if (sectorIndex < 0 || sectorIndex >= result.Length || result[sectorIndex].HasValue)
                        throw new ArgumentException("Core witness sectors must be in-grid and pairwise disjoint.", nameof(publication));
                    result[sectorIndex] = owner.ReservationId;
                    total++;
                }
            }
            if (total != 20)
                throw new ArgumentException("Core witness union must contain exactly twenty sectors.", nameof(publication));
            return result;
        }

        private static List<SiteReservationOverlayDiagnosticRow> CreateDiagnosticRows(
            SiteReservationSelectionPlan plan,
            SiteReservationSearchDiagnostics search,
            CoreCapacityFloodDiagnostics capacity,
            VillageReservationDiagnostics village,
            SiteReservationValidationDiagnostics validation)
        {
            var values = new long[16];
            checked
            {
                for (var reason = 0; reason < 5; reason++)
                    foreach (var group in search.Groups)
                        values[reason] += group.GetReasonCount((SiteReservationRejectionReason)reason);

                foreach (var layout in village.Layouts)
                {
                    values[5] += layout.EntryOutsideWorldCount;
                    values[6] += layout.FootprintOverlapCount;
                    values[7] += layout.ProtectedCoreWitnessCount;
                    values[8] += layout.BlocksExistingEntryApproachCount;
                    values[9] += layout.EntryApproachOccupiedCount;
                    values[10] += layout.OtherSiteDistanceTooSmallCount;
                    values[11] += layout.StartDistanceOutsideSelectedBucketCount;
                }

                foreach (var site in capacity.Sites) values[12] += site.CapacityShortfall;
                values[13] = validation.ViolationCount;
                foreach (var step in plan.Steps)
                {
                    values[14] += step.IncrementalCost.AltitudeUnits;
                    values[15] += step.IncrementalCost.FutureCoreCapacityUnits;
                }
            }

            var keys = new[]
            {
                "SEARCH_FOOTPRINT_OVERLAP",
                "SEARCH_BLOCKS_EXISTING_ENTRY_APPROACH",
                "SEARCH_ENTRY_APPROACH_OCCUPIED",
                "SEARCH_DISTANCE_CONSTRAINT",
                "SEARCH_CORE_CLUSTER",
                "VILLAGE_ENTRY_OUTSIDE_WORLD",
                "VILLAGE_FOOTPRINT_OVERLAP",
                "VILLAGE_PROTECTED_CORE_WITNESS",
                "VILLAGE_BLOCKS_EXISTING_ENTRY_APPROACH",
                "VILLAGE_ENTRY_APPROACH_OCCUPIED",
                "VILLAGE_OTHER_SITE_DISTANCE",
                "VILLAGE_START_BUCKET_DISTANCE",
                "CAPACITY_SHORTFALL",
                "VALIDATION_VIOLATIONS",
                "SELECTED_ALTITUDE_SOFT_UNITS",
                "SELECTED_CAPACITY_FORECAST_SOFT_UNITS"
            };
            var labels = new[]
            {
                "Search footprint overlap",
                "Search blocks existing entry approach",
                "Search entry approach occupied",
                "Search distance constraint",
                "Search Core cluster",
                "Village entry outside world",
                "Village footprint overlap",
                "Village protected Core witness",
                "Village blocks existing entry approach",
                "Village entry approach occupied",
                "Village other-site distance",
                "Village Start bucket distance",
                "Capacity shortfall",
                "Validation violations",
                "Selected altitude (SOFT COST, NOT REJECTION)",
                "Selected capacity forecast (SOFT COST, NOT REJECTION)"
            };

            var result = new List<SiteReservationOverlayDiagnosticRow>(16);
            for (var index = 0; index < 16; index++)
            {
                var diagnosticClass = index < 12
                    ? SiteReservationOverlayDiagnosticClass.CandidateRejection
                    : index < 14
                        ? SiteReservationOverlayDiagnosticClass.FinalGate
                        : SiteReservationOverlayDiagnosticClass.SoftCost;
                result.Add(new SiteReservationOverlayDiagnosticRow(
                    (SiteReservationOverlayDiagnosticKind)index,
                    diagnosticClass,
                    keys[index],
                    labels[index],
                    values[index]));
            }
            return result;
        }

        private static int CompareEntrySides(SiteEntrySide left, SiteEntrySide right)
        {
            return EntryOrder(left).CompareTo(EntryOrder(right));
        }

        private static int EntryOrder(SiteEntrySide side)
        {
            switch (side)
            {
                case SiteEntrySide.L: return 0;
                case SiteEntrySide.R: return 1;
                case SiteEntrySide.U: return 2;
                case SiteEntrySide.D: return 3;
                default: throw new ArgumentOutOfRangeException(nameof(side));
            }
        }

        private static string GetRequiredSource(int index)
        {
            switch (index)
            {
                case 0: return "WORLD_MOONPALACE_V1";
                case 1: return "SITE_MOON_BOSS_VAULT";
                case 2: return "SITE_MOON_SEAL_FORGE";
                case 3: return "SITE_CASSIA_SAP_HEART";
                case 4: return "SITE_DEEP_STAR_YEAST";
                case 5: return "SITE_MOON_CORE_METEOR";
                case 6: return "SITE_PRIMARY_VILLAGE";
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        private static string GetRequiredWitnessSource(int index)
        {
            switch (index)
            {
                case 0: return "SITE_MOON_SEAL_FORGE";
                case 1: return "SITE_CASSIA_SAP_HEART";
                case 2: return "SITE_DEEP_STAR_YEAST";
                case 3: return "SITE_MOON_CORE_METEOR";
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }
}
