using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Data;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SiteReservationValidator
    {
        private const string StartId = "WORLD_MOONPALACE_V1";
        private const string VillageId = "SITE_PRIMARY_VILLAGE";
        private const string VillageProfileId = "VIL_MOON_PRIMARY";
        private const string EntryId = "ENTRY_L";

        private static readonly ExpectedSite[] ExpectedSites =
        {
            new ExpectedSite(StartId, SiteReservationKind.Start, string.Empty, 1, 1, 0, 0, string.Empty),
            new ExpectedSite("SITE_MOON_BOSS_VAULT", SiteReservationKind.Boss,
                "BIO_ABANDONED_MILL", 2, 1, 4, 2, "FIXED"),
            new ExpectedSite("SITE_MOON_SEAL_FORGE", SiteReservationKind.Forge,
                "BIO_ABANDONED_MILL", 1, 1, 2, 2, "FIXED"),
            new ExpectedSite("SITE_CASSIA_SAP_HEART", SiteReservationKind.CoreResource,
                "BIO_CASSIA_ROOT", 1, 1, 2, 3, "FIXED"),
            new ExpectedSite("SITE_DEEP_STAR_YEAST", SiteReservationKind.CoreResource,
                "BIO_MOON_DOUGH", 1, 1, 2, 3, "FIXED"),
            new ExpectedSite("SITE_MOON_CORE_METEOR", SiteReservationKind.CoreResource,
                "BIO_MOON_CRATER", 1, 1, 2, 3, "FIXED"),
            new ExpectedSite(VillageId, SiteReservationKind.Village,
                string.Empty, 1, 1, 0, 2, "VILLAGE_LAYOUT")
        };

        private static readonly string[] CoreBiomes =
        {
            "BIO_ABANDONED_MILL", "BIO_CASSIA_ROOT", "BIO_MOON_DOUGH", "BIO_MOON_CRATER"
        };

        private static readonly string[] CoreRules =
        {
            "PATCH_MILL_CORE", "PATCH_ROOT_CORE", "PATCH_DOUGH_CORE", "PATCH_CRATER_CORE"
        };

        private static readonly int[] CoreMinimums = { 4, 5, 5, 5 };

        public SiteReservationValidationResult ValidateAndPublish(
            ulong worldSeed,
            VillageReservationApproval approval,
            IEnumerable<SpecialMapDefinition> specialMaps,
            IEnumerable<SpecialMapFootprintCellDefinition> footprintCells,
            IEnumerable<SpecialMapEntrySocketDefinition> entrySockets)
        {
            try
            {
                return ValidateAndPublishCore(
                    worldSeed, approval, specialMaps, footprintCells, entrySockets);
            }
            catch
            {
                return Invalid(new[]
                {
                    Error(SiteReservationValidationErrorCode.InternalInvariantViolation,
                        string.Empty, string.Empty, -1,
                        "Site reservation validation encountered an internal invariant failure.")
                });
            }
        }

        private static SiteReservationValidationResult ValidateAndPublishCore(
            ulong worldSeed,
            VillageReservationApproval approval,
            IEnumerable<SpecialMapDefinition> specialMaps,
            IEnumerable<SpecialMapFootprintCellDefinition> footprintCells,
            IEnumerable<SpecialMapEntrySocketDefinition> entrySockets)
        {
            var errors = new List<SiteReservationValidationError>();
            var input = SnapshotInputs(
                approval, specialMaps, footprintCells, entrySockets, errors);
            ValidateApprovalIdentity(input, errors);
            ValidateDefinitionIdentity(input, errors);
            if (errors.Count != 0) return Invalid(errors);

            var violations = new List<SiteReservationValidationViolation>();
            EvaluateRequiredCounts(input, violations);
            EvaluateWorldBounds(input, violations);
            EvaluateOverlap(input, violations);
            EvaluateDistances(input, violations);
            EvaluateEntries(input, violations);
            EvaluateCapacity(input, violations);
            var canonical = CanonicalizeViolations(violations);
            var diagnostics = CreateDiagnostics(input, canonical);

            if (canonical.Count != 0)
            {
                return new SiteReservationValidationResult(
                    SiteReservationValidationStatus.ValidationRejected,
                    null,
                    diagnostics,
                    canonical,
                    Array.Empty<SiteReservationValidationError>());
            }

            SiteReservationPublication publication;
            try
            {
                publication = new SiteReservationSnapshotPublisher().Publish(
                    worldSeed, approval, input.MapsById);
            }
            catch
            {
                return Invalid(new[]
                {
                    Error(SiteReservationValidationErrorCode.InternalInvariantViolation,
                        string.Empty, string.Empty, -1,
                        "Site reservation publication encountered an internal invariant failure.")
                });
            }

            return new SiteReservationValidationResult(
                SiteReservationValidationStatus.Completed,
                publication,
                diagnostics,
                Array.Empty<SiteReservationValidationViolation>(),
                Array.Empty<SiteReservationValidationError>());
        }

        private static InputSnapshot SnapshotInputs(
            VillageReservationApproval approval,
            IEnumerable<SpecialMapDefinition> specialMaps,
            IEnumerable<SpecialMapFootprintCellDefinition> footprintCells,
            IEnumerable<SpecialMapEntrySocketDefinition> entrySockets,
            ICollection<SiteReservationValidationError> errors)
        {
            if (approval == null)
                errors.Add(Error(SiteReservationValidationErrorCode.MissingApproval,
                    string.Empty, string.Empty, -1, "A Village reservation approval is required."));

            var maps = Snapshot(specialMaps, SiteReservationValidationErrorCode.MissingSpecialMaps,
                "Special-map definitions are required.", errors);
            var cells = Snapshot(footprintCells, SiteReservationValidationErrorCode.MissingFootprintCells,
                "Special-map footprint cells are required.", errors);
            var entries = Snapshot(entrySockets, SiteReservationValidationErrorCode.MissingEntrySockets,
                "Special-map entry sockets are required.", errors);

            var mapsById = new Dictionary<string, SpecialMapDefinition>(StringComparer.Ordinal);
            foreach (var map in maps)
            {
                if (map == null)
                {
                    errors.Add(Error(SiteReservationValidationErrorCode.NullSpecialMap,
                        string.Empty, string.Empty, -1, "Special-map definitions cannot contain null."));
                    continue;
                }
                var id = CanonicalOrEmpty(map.SpecialMapId);
                if (id.Length == 0 || !mapsById.TryAdd(id, map))
                    errors.Add(Error(SiteReservationValidationErrorCode.DuplicateSpecialMapId,
                        id, string.Empty, -1, "Special-map IDs must be canonical and unique."));
            }

            var cellsByMap = new Dictionary<string, List<SpecialMapFootprintCellDefinition>>(StringComparer.Ordinal);
            var cellKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var cell in cells)
            {
                if (cell == null)
                {
                    errors.Add(Error(SiteReservationValidationErrorCode.NullFootprintCell,
                        string.Empty, string.Empty, -1, "Footprint cells cannot contain null."));
                    continue;
                }
                var id = CanonicalOrEmpty(cell.SpecialMapId);
                var child = CoordinateId(cell.LocalSectorX, cell.LocalSectorY);
                var key = id + "|" + child;
                if (id.Length == 0 || !cellKeys.Add(key))
                    errors.Add(Error(SiteReservationValidationErrorCode.DuplicateFootprintCell,
                        id, child, -1, "Footprint-cell composite keys must be unique."));
                if (!cellsByMap.TryGetValue(id, out var values))
                {
                    values = new List<SpecialMapFootprintCellDefinition>();
                    cellsByMap[id] = values;
                }
                values.Add(cell);
            }

            var entriesByMap = new Dictionary<string, List<SpecialMapEntrySocketDefinition>>(StringComparer.Ordinal);
            var entryKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                if (entry == null)
                {
                    errors.Add(Error(SiteReservationValidationErrorCode.NullEntrySocket,
                        string.Empty, string.Empty, -1, "Entry sockets cannot contain null."));
                    continue;
                }
                var id = CanonicalOrEmpty(entry.SpecialMapId);
                var child = CanonicalOrEmpty(entry.EntrySocketId);
                var key = id + "|" + child;
                if (id.Length == 0 || child.Length == 0 || !entryKeys.Add(key))
                    errors.Add(Error(SiteReservationValidationErrorCode.DuplicateEntrySocket,
                        id, child, -1, "Entry-socket composite keys must be unique."));
                if (!entriesByMap.TryGetValue(id, out var values))
                {
                    values = new List<SpecialMapEntrySocketDefinition>();
                    entriesByMap[id] = values;
                }
                values.Add(entry);
            }

            return new InputSnapshot(
                approval, maps, cells, entries, mapsById, cellsByMap, entriesByMap);
        }

        private static List<T> Snapshot<T>(
            IEnumerable<T> source,
            SiteReservationValidationErrorCode missingCode,
            string message,
            ICollection<SiteReservationValidationError> errors)
        {
            if (source == null)
            {
                errors.Add(Error(missingCode, string.Empty, string.Empty, -1, message));
                return new List<T>();
            }
            return new List<T>(source);
        }

        private static void ValidateApprovalIdentity(
            InputSnapshot input,
            ICollection<SiteReservationValidationError> errors)
        {
            var approval = input.Approval;
            if (approval == null) return;
            var capacity = approval.CoreCapacityApproval;
            var plan = capacity == null ? null : capacity.SelectionPlan;
            if (capacity == null || plan == null || approval.Village == null ||
                plan.Steps == null || plan.SelectedPlacements == null ||
                plan.Steps.Count != 6 || plan.SelectedPlacements.Count != 6 ||
                plan.SelectedCount != 6 || capacity.Witnesses == null ||
                capacity.Witnesses.Count != 4 || capacity.CapacitySiteCount != 4 ||
                approval.ExistingSiteCount != 6 || approval.CapacityWitnessCount != 4 ||
                approval.TotalSelectedSiteCount != 7)
            {
                errors.Add(Error(SiteReservationValidationErrorCode.InvalidApproval,
                    string.Empty, string.Empty, -1,
                    "The approval must have the frozen six-step, four-witness, one-Village shape."));
                return;
            }

            for (var index = 0; index < 6; index++)
            {
                var step = plan.Steps[index];
                var placement = plan.SelectedPlacements[index];
                var expected = ExpectedSites[index];
                if (step == null || placement == null || placement.Candidate == null ||
                    placement.Footprint == null || step.Depth != index ||
                    step.Option == null || !ReferenceEquals(step.Option.Placement, placement) ||
                    step.Key != new SitePlacementKey(expected.Kind, expected.Id, 0) ||
                    SitePlacementKey.FromPlacement(placement) != step.Key ||
                    placement.Candidate.RequiredInstanceOrdinal != 0 ||
                    !string.Equals(placement.Candidate.SourceDefinitionId, expected.Id, StringComparison.Ordinal) ||
                    placement.Candidate.Kind != expected.Kind ||
                    placement.Candidate.CandidateOrdinal < 0 ||
                    !IsWorld(placement.Candidate.Origin) ||
                    WorldGridIndex.ToIndex(placement.Candidate.Origin) != placement.Candidate.OriginIndex)
                {
                    errors.Add(Error(SiteReservationValidationErrorCode.SelectionIdentityMismatch,
                        expected.Id, string.Empty, -1,
                        "A selected placement does not match the frozen key, depth, order, or origin identity."));
                    continue;
                }

                var seen = new HashSet<int>();
                foreach (var sector in placement.OccupiedSectors)
                {
                    if (!IsWorld(sector) || !seen.Add(WorldGridIndex.ToIndex(sector)))
                        errors.Add(Error(SiteReservationValidationErrorCode.SelectionIdentityMismatch,
                            expected.Id, string.Empty, -1,
                            "Selected occupied sectors must be unique world-grid coordinates."));
                }
                if (placement.OccupiedSectors.Count != placement.Footprint.Cells.Count)
                    errors.Add(Error(SiteReservationValidationErrorCode.SelectionIdentityMismatch,
                        expected.Id, string.Empty, -1,
                        "Selected occupied-sector and footprint-cell counts must match."));
            }

            ValidateVillageIdentity(input, errors);
            ValidateCapacityIdentity(input, errors);
        }

        private static void ValidateVillageIdentity(
            InputSnapshot input,
            ICollection<SiteReservationValidationError> errors)
        {
            var village = input.Approval.Village;
            var profile = village.Profile;
            var map = village.SpecialMap;
            var layout = village.Layout;
            var bucket = village.DistanceBucket;
            var candidate = village.Candidate;
            var template = village.EntryTemplate;
            var valid = profile != null && map != null && layout != null && bucket != null &&
                        candidate != null && template != null &&
                        string.Equals(profile.VillageProfileId, VillageProfileId, StringComparison.Ordinal) &&
                        string.Equals(profile.WorldProfileId, StartId, StringComparison.Ordinal) && profile.Active &&
                        profile.FacilityCountMin == 5 && profile.FacilityCountMax == 6 &&
                        profile.MaximumSectorCount == 2 &&
                        string.Equals(map.SpecialMapId, VillageId, StringComparison.Ordinal) &&
                        string.Equals(candidate.VillageProfileId, VillageProfileId, StringComparison.Ordinal) &&
                        string.Equals(candidate.SpecialMapId, VillageId, StringComparison.Ordinal) &&
                        string.Equals(candidate.LayoutId, layout.VillageLayoutId, StringComparison.Ordinal) &&
                        candidate.BucketOrdinal == bucket.BucketOrdinal &&
                        bucket.Contains(candidate.StartDistance) &&
                        layout.Active && candidate.FootprintWidthSectors == layout.FootprintWidthSectors &&
                        candidate.FootprintHeightSectors == layout.FootprintHeightSectors &&
                        candidate.OriginIndex == WorldGridIndex.ToIndex(candidate.Origin) &&
                        candidate.OccupiedSectorIndices.Count ==
                        candidate.FootprintWidthSectors * candidate.FootprintHeightSectors &&
                        string.Equals(template.SpecialMapId, VillageId, StringComparison.Ordinal) &&
                        string.Equals(template.EntrySocketId, EntryId, StringComparison.Ordinal) &&
                        template.Required && template.ReturnPathRequired && ExactRoutes(template.AllowedRouteTypes);
            if (!valid)
                errors.Add(Error(SiteReservationValidationErrorCode.VillageIdentityMismatch,
                    VillageId, EntryId, -1,
                    "The Village profile, layout, bucket, candidate, or entry identity is inconsistent."));
        }

        private static void ValidateCapacityIdentity(
            InputSnapshot input,
            ICollection<SiteReservationValidationError> errors)
        {
            var approval = input.Approval.CoreCapacityApproval;
            var plan = approval.SelectionPlan;
            var total = 0;
            for (var index = 0; index < 4; index++)
            {
                var witness = approval.Witnesses[index];
                var expectedPlacement = plan.SelectedPlacements[index + 2];
                if (witness == null || witness.Key != SitePlacementKey.FromPlacement(expectedPlacement) ||
                    !string.Equals(witness.BiomeId, CoreBiomes[index], StringComparison.Ordinal) ||
                    !string.Equals(witness.CorePatchRuleId, CoreRules[index], StringComparison.Ordinal) ||
                    witness.MinimumCoreSectorCount != CoreMinimums[index] ||
                    witness.BufferRingSectors != 1 || witness.RequiredWitnessSectorCount != 5 ||
                    witness.WitnessSectorIndices == null || witness.WitnessSectorIndices.Count != 5 ||
                    witness.FootprintSectorIndices == null || witness.MandatoryBufferSectorIndices == null)
                {
                    errors.Add(Error(SiteReservationValidationErrorCode.CapacityIdentityMismatch,
                        ExpectedSites[index + 2].Id, string.Empty, -1,
                        "A Core capacity witness does not match its frozen source identity."));
                    continue;
                }
                checked { total += witness.WitnessSectorIndices.Count; }
                var owner = SortedIndices(expectedPlacement.OccupiedSectors);
                if (!SequenceEqual(owner, witness.FootprintSectorIndices) ||
                    witness.SeedSectorIndex != owner[0])
                    errors.Add(Error(SiteReservationValidationErrorCode.CapacityIdentityMismatch,
                        ExpectedSites[index + 2].Id, string.Empty, witness.SeedSectorIndex,
                        "A Core capacity witness does not preserve its source footprint and seed."));
            }
            if (total != 20 || approval.TotalWitnessSectorCount != 20)
                errors.Add(Error(SiteReservationValidationErrorCode.CapacityIdentityMismatch,
                    string.Empty, string.Empty, -1,
                    "Core capacity witness sector totals must equal twenty."));
        }

        private static void ValidateDefinitionIdentity(
            InputSnapshot input,
            ICollection<SiteReservationValidationError> errors)
        {
            foreach (var pair in input.MapsById)
                if (FindExpected(pair.Key) == null)
                    errors.Add(Error(SiteReservationValidationErrorCode.UnexpectedSpecialMap,
                        pair.Key, string.Empty, -1, "An unexpected special-map definition was supplied."));

            for (var index = 1; index < ExpectedSites.Length; index++)
            {
                var expected = ExpectedSites[index];
                if (!input.MapsById.TryGetValue(expected.Id, out var map))
                {
                    errors.Add(Error(SiteReservationValidationErrorCode.MissingRequiredSpecialMap,
                        expected.Id, string.Empty, -1, "A required special-map definition is missing."));
                    continue;
                }
                if (!MatchesMap(map, expected))
                    errors.Add(Error(SiteReservationValidationErrorCode.InvalidSpecialMap,
                        expected.Id, string.Empty, -1,
                        "A required special-map definition violates the frozen identity contract."));

                if (!input.CellsByMap.TryGetValue(expected.Id, out var cells) ||
                    cells.Count != expected.Width * expected.Height)
                    errors.Add(Error(SiteReservationValidationErrorCode.MissingRequiredFootprintCell,
                        expected.Id, string.Empty, -1,
                        "A required special-map footprint has incomplete source coverage."));
                else
                    ValidateCells(expected, cells, errors);

                if (!input.EntriesByMap.TryGetValue(expected.Id, out var entries) || entries.Count != 1)
                    errors.Add(Error(SiteReservationValidationErrorCode.MissingRequiredEntrySocket,
                        expected.Id, EntryId, -1,
                        "Exactly one required ENTRY_L source socket is required."));
                else
                    ValidateEntryDefinition(expected, entries[0], errors);
            }

            foreach (var pair in input.CellsByMap)
                if (FindExpected(pair.Key) == null || string.Equals(pair.Key, StartId, StringComparison.Ordinal))
                    errors.Add(Error(SiteReservationValidationErrorCode.UnexpectedFootprintCell,
                        CanonicalOrEmpty(pair.Key), string.Empty, -1,
                        "A footprint cell has an unexpected parent definition."));
            foreach (var pair in input.EntriesByMap)
                if (FindExpected(pair.Key) == null || string.Equals(pair.Key, StartId, StringComparison.Ordinal))
                    errors.Add(Error(SiteReservationValidationErrorCode.UnexpectedEntrySocket,
                        CanonicalOrEmpty(pair.Key), string.Empty, -1,
                        "An entry socket has an unexpected parent definition."));

            if (input.Maps.Count != 6)
                errors.Add(Error(SiteReservationValidationErrorCode.DefinitionIdentityMismatch,
                    string.Empty, string.Empty, -1, "The source inventory must contain exactly six special maps."));
            if (input.Cells.Count != 7)
                errors.Add(Error(SiteReservationValidationErrorCode.DefinitionIdentityMismatch,
                    string.Empty, string.Empty, -1, "The source inventory must contain exactly seven footprint cells."));
            if (input.Entries.Count != 6)
                errors.Add(Error(SiteReservationValidationErrorCode.DefinitionIdentityMismatch,
                    string.Empty, string.Empty, -1, "The source inventory must contain exactly six entry sockets."));

            if (input.Approval == null || input.Approval.CoreCapacityApproval == null ||
                input.Approval.CoreCapacityApproval.SelectionPlan == null) return;
            var placements = input.Approval.CoreCapacityApproval.SelectionPlan.SelectedPlacements;
            if (placements.Count != 6) return;
            if (placements[0].Footprint.Width != 1 || placements[0].Footprint.Height != 1 ||
                placements[0].Footprint.Cells.Count != 1 || placements[0].Entries.Count != 0)
                errors.Add(Error(SiteReservationValidationErrorCode.SelectionIdentityMismatch,
                    StartId, string.Empty, -1, "Start must use one footprint cell and no source entry."));
            for (var index = 1; index < 6; index++)
            {
                if (input.MapsById.TryGetValue(ExpectedSites[index].Id, out var map) &&
                    input.CellsByMap.TryGetValue(ExpectedSites[index].Id, out var cells) &&
                    input.EntriesByMap.TryGetValue(ExpectedSites[index].Id, out var entries))
                    ValidatePlacementDefinitionIdentity(placements[index], map, cells, entries, errors);
            }

            if (input.MapsById.TryGetValue(VillageId, out var villageMap) && input.Approval != null)
            {
                if (!ReferenceEquals(input.Approval.Village.SpecialMap, villageMap))
                    errors.Add(Error(SiteReservationValidationErrorCode.DefinitionIdentityMismatch,
                        VillageId, string.Empty, -1,
                        "The Village approval must reference the supplied special-map definition."));
                if (input.EntriesByMap.TryGetValue(VillageId, out var villageEntries) &&
                    villageEntries.Count == 1 &&
                    !ReferenceEquals(input.Approval.Village.EntryTemplate, villageEntries[0]))
                    errors.Add(Error(SiteReservationValidationErrorCode.DefinitionIdentityMismatch,
                        VillageId, EntryId, -1,
                        "The Village approval must reference the supplied entry template."));
            }
        }

        private static void ValidateCells(
            ExpectedSite expected,
            IReadOnlyList<SpecialMapFootprintCellDefinition> cells,
            ICollection<SiteReservationValidationError> errors)
        {
            var coordinates = new HashSet<string>(StringComparer.Ordinal);
            foreach (var cell in cells)
            {
                var child = CoordinateId(cell.LocalSectorX, cell.LocalSectorY);
                var valid = cell.LocalSectorX >= 0 && cell.LocalSectorX < expected.Width &&
                            cell.LocalSectorY >= 0 && cell.LocalSectorY < expected.Height &&
                            SitePlacementKey.IsCanonicalId(cell.LocalRole) &&
                            (cell.RequiredPrimaryBiomeId.Length == 0 ||
                             SitePlacementKey.IsCanonicalId(cell.RequiredPrimaryBiomeId)) &&
                            (cell.FixedSectorRecipeId.Length == 0 ||
                             SitePlacementKey.IsCanonicalId(cell.FixedSectorRecipeId)) &&
                            ValidSideTokens(cell.RequiredOpenSides);
                if (!valid)
                    errors.Add(Error(SiteReservationValidationErrorCode.InvalidFootprintCell,
                        expected.Id, child, -1,
                        "A footprint cell has invalid coordinates, IDs, or required-open sides."));
                coordinates.Add(child);
            }
            for (var y = 0; y < expected.Height; y++)
                for (var x = 0; x < expected.Width; x++)
                    if (!coordinates.Contains(CoordinateId(x, y)))
                        errors.Add(Error(SiteReservationValidationErrorCode.MissingRequiredFootprintCell,
                            expected.Id, CoordinateId(x, y), -1,
                            "A required footprint coordinate is missing."));
        }

        private static void ValidateEntryDefinition(
            ExpectedSite expected,
            SpecialMapEntrySocketDefinition entry,
            ICollection<SiteReservationValidationError> errors)
        {
            if (entry == null ||
                !string.Equals(entry.SpecialMapId, expected.Id, StringComparison.Ordinal) ||
                !string.Equals(entry.EntrySocketId, EntryId, StringComparison.Ordinal) ||
                entry.LocalSectorX != 0 || entry.LocalSectorY != 0 ||
                !string.Equals(entry.Side, "L", StringComparison.Ordinal) ||
                !entry.Required || !entry.ReturnPathRequired || !ExactRoutes(entry.AllowedRouteTypes))
                errors.Add(Error(SiteReservationValidationErrorCode.InvalidEntrySocket,
                    expected.Id, EntryId, -1,
                    "The required ENTRY_L source socket violates the frozen contract."));
        }

        private static void ValidatePlacementDefinitionIdentity(
            FootprintPlacement placement,
            SpecialMapDefinition map,
            IReadOnlyList<SpecialMapFootprintCellDefinition> cells,
            IReadOnlyList<SpecialMapEntrySocketDefinition> entries,
            ICollection<SiteReservationValidationError> errors)
        {
            var sourceId = placement.Candidate.SourceDefinitionId;
            if (placement.Footprint.Width != map.FootprintWidthSectors ||
                placement.Footprint.Height != map.FootprintHeightSectors ||
                placement.Footprint.Cells.Count != cells.Count || placement.Entries.Count != entries.Count)
            {
                errors.Add(Error(SiteReservationValidationErrorCode.DefinitionIdentityMismatch,
                    sourceId, string.Empty, -1,
                    "A selected placement cardinality does not match its typed definition."));
                return;
            }

            foreach (var source in cells)
            {
                if (!SiteFootprintTransformer.TryTransformCoordinate(
                        map.FootprintWidthSectors, map.FootprintHeightSectors,
                        placement.Footprint.Transform, source.LocalSectorX, source.LocalSectorY,
                        out var x, out var y) || !placement.Footprint.TryGetCell(x, y, out var actual) ||
                    !string.Equals(actual.LocalRole, source.LocalRole, StringComparison.Ordinal) ||
                    !string.Equals(actual.RequiredPrimaryBiomeId, source.RequiredPrimaryBiomeId, StringComparison.Ordinal) ||
                    !string.Equals(actual.FixedSectorRecipeId, source.FixedSectorRecipeId, StringComparison.Ordinal) ||
                    !TransformedSidesEqual(source.RequiredOpenSides, placement.Footprint.Transform,
                        actual.RequiredOpenSides))
                    errors.Add(Error(SiteReservationValidationErrorCode.DefinitionIdentityMismatch,
                        sourceId, CoordinateId(source.LocalSectorX, source.LocalSectorY), -1,
                        "A transformed footprint cell differs from its typed source definition."));
            }

            foreach (var source in entries)
            {
                FootprintPlacementEntry actual = null;
                foreach (var candidate in placement.Entries)
                    if (string.Equals(candidate.EntrySocketId, source.EntrySocketId, StringComparison.Ordinal))
                        actual = candidate;
                var valid = SiteFootprintTransformer.TryTransformCoordinate(
                                map.FootprintWidthSectors, map.FootprintHeightSectors,
                                placement.Footprint.Transform, source.LocalSectorX, source.LocalSectorY,
                                out var x, out var y) &&
                            SiteReservationTokenCodec.TryParseEntrySide(source.Side, out var sourceSide) &&
                            SiteFootprintTransformer.TryTransformSide(
                                placement.Footprint.Transform, sourceSide, out var side) &&
                            actual != null && actual.LocalX == x && actual.LocalY == y &&
                            actual.FootprintSector == new SectorCoord(
                                placement.Candidate.Origin.X + x, placement.Candidate.Origin.Y + y) &&
                            actual.Side == side && SequenceEqual(actual.AllowedRouteTypes, source.AllowedRouteTypes) &&
                            actual.Required == source.Required &&
                            actual.ReturnPathRequired == source.ReturnPathRequired;
                if (!valid)
                    errors.Add(Error(SiteReservationValidationErrorCode.DefinitionIdentityMismatch,
                        sourceId, CanonicalOrEmpty(source.EntrySocketId), -1,
                        "A transformed entry differs from its typed source definition."));
            }
        }

        private static void EvaluateRequiredCounts(
            InputSnapshot input,
            ICollection<SiteReservationValidationViolation> violations)
        {
            var counts = new Dictionary<SiteReservationKind, int>();
            var sources = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var placement in input.Approval.CoreCapacityApproval.SelectionPlan.SelectedPlacements)
            {
                AddCount(counts, placement.Candidate.Kind);
                AddCount(sources, placement.Candidate.SourceDefinitionId);
            }
            AddCount(counts, SiteReservationKind.Village);
            AddCount(sources, input.Approval.Village.Candidate.SpecialMapId);

            var expectedCounts = new Dictionary<SiteReservationKind, int>
            {
                { SiteReservationKind.Start, 1 },
                { SiteReservationKind.Boss, 1 },
                { SiteReservationKind.Forge, 1 },
                { SiteReservationKind.CoreResource, 3 },
                { SiteReservationKind.Village, 1 }
            };
            foreach (var expected in expectedCounts)
            {
                counts.TryGetValue(expected.Key, out var actual);
                if (actual != expected.Value)
                    violations.Add(Violation(
                        SiteReservationValidationViolationCode.RequiredCountMismatch,
                        SiteReservationValidationRule.RequiredSiteCounts,
                        SiteReservationTokenCodec.ToToken(expected.Key), string.Empty, -1,
                        actual, expected.Value,
                        "A required reservation kind has the wrong instance count."));
            }
            foreach (var expected in ExpectedSites)
            {
                sources.TryGetValue(expected.Id, out var actual);
                if (actual == 0)
                    violations.Add(Violation(
                        SiteReservationValidationViolationCode.MissingRequiredReservation,
                        SiteReservationValidationRule.RequiredSiteCounts,
                        expected.Id, string.Empty, -1, 0, 1,
                        "A required reservation source is missing."));
                else if (actual != 1)
                    violations.Add(Violation(
                        SiteReservationValidationViolationCode.RequiredCountMismatch,
                        SiteReservationValidationRule.RequiredSiteCounts,
                        expected.Id, string.Empty, -1, actual, 1,
                        "A required reservation source has the wrong instance count."));
            }
            foreach (var source in sources)
                if (FindExpected(source.Key) == null)
                    violations.Add(Violation(
                        SiteReservationValidationViolationCode.UnexpectedReservation,
                        SiteReservationValidationRule.RequiredSiteCounts,
                        source.Key, string.Empty, -1, source.Value, 0,
                        "An unexpected reservation source was selected."));
        }

        private static void EvaluateWorldBounds(
            InputSnapshot input,
            ICollection<SiteReservationValidationViolation> violations)
        {
            foreach (var placement in input.Approval.CoreCapacityApproval.SelectionPlan.SelectedPlacements)
            {
                var source = placement.Candidate.SourceDefinitionId;
                if (!IsWorld(placement.Candidate.Origin))
                    violations.Add(Violation(
                        SiteReservationValidationViolationCode.FootprintOutsideWorld,
                        SiteReservationValidationRule.WorldBounds,
                        source, string.Empty, -1, -1, WorldGenConstants.SectorCount,
                        "A reservation origin is outside the world grid."));
                foreach (var sector in placement.OccupiedSectors)
                {
                    var sectorIndex = IsWorld(sector) ? WorldGridIndex.ToIndex(sector) : -1;
                    if (!IsWorld(sector))
                        violations.Add(Violation(
                            SiteReservationValidationViolationCode.FootprintOutsideWorld,
                            SiteReservationValidationRule.WorldBounds,
                            source, string.Empty, -1, -1, WorldGenConstants.SectorCount,
                            "A reservation footprint sector is outside the world grid."));
                    else if (!placement.TryGetFootprintCell(sector, out var cell) ||
                             sector != new SectorCoord(
                                 placement.Candidate.Origin.X + cell.LocalX,
                                 placement.Candidate.Origin.Y + cell.LocalY))
                        violations.Add(Violation(
                            SiteReservationValidationViolationCode.FootprintIdentityMismatch,
                            SiteReservationValidationRule.WorldBounds,
                            source, string.Empty, sectorIndex, -1, sectorIndex,
                            "A footprint sector does not equal origin plus local coordinates."));
                }
                foreach (var entry in placement.Entries)
                    if (!IsWorld(entry.FootprintSector) || !IsWorld(entry.ExteriorSector))
                        violations.Add(Violation(
                            SiteReservationValidationViolationCode.EntryOutsideWorld,
                            SiteReservationValidationRule.WorldBounds,
                            source, entry.EntrySocketId, -1, -1, WorldGenConstants.SectorCount,
                            "An entry footprint or exterior sector is outside the world grid."));
            }

            var candidate = input.Approval.Village.Candidate;
            if (!IsWorld(candidate.Origin))
                violations.Add(Violation(
                    SiteReservationValidationViolationCode.FootprintOutsideWorld,
                    SiteReservationValidationRule.WorldBounds,
                    VillageId, string.Empty, -1, -1, WorldGenConstants.SectorCount,
                    "The Village origin is outside the world grid."));
            foreach (var index in candidate.OccupiedSectorIndices)
                if (index < 0 || index >= WorldGenConstants.SectorCount)
                    violations.Add(Violation(
                        SiteReservationValidationViolationCode.FootprintOutsideWorld,
                        SiteReservationValidationRule.WorldBounds,
                        VillageId, string.Empty, -1, index, WorldGenConstants.SectorCount - 1,
                        "A Village footprint sector is outside the world grid."));
            if (candidate.EntryFootprintSectorIndex < 0 ||
                candidate.EntryFootprintSectorIndex >= WorldGenConstants.SectorCount ||
                candidate.EntryExteriorSectorIndex < 0 ||
                candidate.EntryExteriorSectorIndex >= WorldGenConstants.SectorCount)
                violations.Add(Violation(
                    SiteReservationValidationViolationCode.EntryOutsideWorld,
                    SiteReservationValidationRule.WorldBounds,
                    VillageId, EntryId, -1, -1, WorldGenConstants.SectorCount,
                    "The Village entry footprint or exterior is outside the world grid."));

            foreach (var witness in input.Approval.CoreCapacityApproval.Witnesses)
                foreach (var index in witness.WitnessSectorIndices)
                    if (index < 0 || index >= WorldGenConstants.SectorCount)
                        violations.Add(Violation(
                            SiteReservationValidationViolationCode.FootprintOutsideWorld,
                            SiteReservationValidationRule.WorldBounds,
                            witness.Key.SourceDefinitionId, string.Empty, -1,
                            index, WorldGenConstants.SectorCount - 1,
                            "A Core capacity witness sector is outside the world grid."));
        }

        private static void EvaluateOverlap(
            InputSnapshot input,
            ICollection<SiteReservationValidationViolation> violations)
        {
            var owners = new Dictionary<int, string>();
            var sum = 0;
            foreach (var placement in input.Approval.CoreCapacityApproval.SelectionPlan.SelectedPlacements)
            {
                checked { sum += placement.OccupiedSectors.Count; }
                foreach (var sector in placement.OccupiedSectors)
                    AddOwner(owners, WorldGridIndex.ToIndex(sector), placement.Candidate.SourceDefinitionId, violations);
            }
            var village = input.Approval.Village.Candidate;
            checked { sum += village.OccupiedSectorIndices.Count; }
            foreach (var index in village.OccupiedSectorIndices)
                AddOwner(owners, index, VillageId, violations);

            if (owners.Count != sum)
                violations.Add(Violation(
                    SiteReservationValidationViolationCode.FootprintOverlap,
                    SiteReservationValidationRule.FootprintOverlap,
                    string.Empty, string.Empty, -1, owners.Count, sum,
                    "The occupied-sector union does not conserve reservation footprint counts."));

            foreach (var placement in input.Approval.CoreCapacityApproval.SelectionPlan.SelectedPlacements)
            {
                foreach (var entry in placement.Entries)
                {
                    var exterior = WorldGridIndex.ToIndex(entry.ExteriorSector);
                    if (owners.TryGetValue(exterior, out var owner))
                        violations.Add(Violation(
                            SiteReservationValidationViolationCode.EntryApproachOccupied,
                            SiteReservationValidationRule.FootprintOverlap,
                            placement.Candidate.SourceDefinitionId, owner, exterior, 1, 0,
                            "An entry exterior sector is occupied by a reservation footprint."));
                }
            }
            if (owners.TryGetValue(village.EntryExteriorSectorIndex, out var villageExteriorOwner))
                violations.Add(Violation(
                    SiteReservationValidationViolationCode.EntryApproachOccupied,
                    SiteReservationValidationRule.FootprintOverlap,
                    VillageId, villageExteriorOwner, village.EntryExteriorSectorIndex, 1, 0,
                    "The Village entry exterior sector is occupied by a reservation footprint."));
        }

        private static void AddOwner(
            IDictionary<int, string> owners,
            int sectorIndex,
            string source,
            ICollection<SiteReservationValidationViolation> violations)
        {
            if (owners.TryGetValue(sectorIndex, out var first))
                violations.Add(Violation(
                    SiteReservationValidationViolationCode.FootprintOverlap,
                    SiteReservationValidationRule.FootprintOverlap,
                    first, source, sectorIndex, 2, 1,
                    "Two reservation footprints occupy the same sector."));
            else
                owners.Add(sectorIndex, source);
        }

        private static void EvaluateDistances(
            InputSnapshot input,
            ICollection<SiteReservationValidationViolation> violations)
        {
            var placements = input.Approval.CoreCapacityApproval.SelectionPlan.SelectedPlacements;
            var indexResult = new SiteDistanceIndexBuilder().Build(placements);
            var policyResult = new SiteDistancePolicyBuilder().BuildRequiredSitePolicy(
                StartId, input.Maps);
            if (!indexResult.Succeeded || !policyResult.Succeeded)
            {
                violations.Add(Violation(
                    SiteReservationValidationViolationCode.FootprintIdentityMismatch,
                    SiteReservationValidationRule.DistanceConstraints,
                    string.Empty, string.Empty, -1, 0, 15,
                    "The non-Village distance index or policy could not be reconstructed."));
            }
            else
            {
                var evaluation = indexResult.Index.Evaluate(policyResult.Policy);
                if (!evaluation.Succeeded)
                    violations.Add(Violation(
                        SiteReservationValidationViolationCode.FootprintIdentityMismatch,
                        SiteReservationValidationRule.DistanceConstraints,
                        string.Empty, string.Empty, -1, 0, 15,
                        "The non-Village distance policy could not be evaluated."));
                else
                    foreach (var item in evaluation.Violations)
                        violations.Add(Violation(
                            SiteReservationValidationViolationCode.DistanceBelowMinimum,
                            SiteReservationValidationRule.DistanceConstraints,
                            item.First.SourceDefinitionId, item.Second.SourceDefinitionId,
                            WorldGridIndex.ToIndex(item.FirstClosestSector),
                            item.ActualDistance, item.MinimumDistance,
                            "A non-Village footprint distance is below its required minimum."));
            }

            var village = input.Approval.Village;
            var candidate = village.Candidate;
            var villageIndices = candidate.OccupiedSectorIndices;
            var startDistance = MinimumDistance(
                villageIndices, SortedIndices(placements[0].OccupiedSectors));
            if (!village.DistanceBucket.Contains(startDistance) ||
                startDistance != candidate.StartDistance)
                violations.Add(Violation(
                    SiteReservationValidationViolationCode.VillageDistanceBucketMismatch,
                    SiteReservationValidationRule.DistanceConstraints,
                    VillageId, StartId, candidate.OriginIndex,
                    startDistance, village.DistanceBucket.MinDistanceInclusive,
                    "The Village-to-Start footprint distance is outside its selected bucket."));

            for (var index = 1; index < placements.Count; index++)
            {
                var distance = MinimumDistance(villageIndices, SortedIndices(placements[index].OccupiedSectors));
                if (distance < 2)
                    violations.Add(Violation(
                        SiteReservationValidationViolationCode.DistanceBelowMinimum,
                        SiteReservationValidationRule.DistanceConstraints,
                        VillageId, placements[index].Candidate.SourceDefinitionId,
                        candidate.OriginIndex, distance, 2,
                        "The Village footprint is too close to another special site."));
            }

            var minimumX = int.MaxValue;
            var maximumX = int.MinValue;
            var minimumY = int.MaxValue;
            var maximumY = int.MinValue;
            for (var index = 3; index <= 5; index++)
                foreach (var sector in placements[index].OccupiedSectors)
                {
                    minimumX = Math.Min(minimumX, sector.X);
                    maximumX = Math.Max(maximumX, sector.X);
                    minimumY = Math.Min(minimumY, sector.Y);
                    maximumY = Math.Max(maximumY, sector.Y);
                }
            var width = maximumX - minimumX + 1;
            var height = maximumY - minimumY + 1;
            if (width <= 4 && height <= 4)
                violations.Add(Violation(
                    SiteReservationValidationViolationCode.CoreClusterViolation,
                    SiteReservationValidationRule.DistanceConstraints,
                    ExpectedSites[3].Id, ExpectedSites[5].Id, -1,
                    Math.Max(width, height), 5,
                    "The three Core-resource sites are confined to a four-by-four bounding box."));
        }

        private static void EvaluateEntries(
            InputSnapshot input,
            ICollection<SiteReservationValidationViolation> violations)
        {
            var occupied = AllOccupied(input);
            var placements = input.Approval.CoreCapacityApproval.SelectionPlan.SelectedPlacements;
            if (placements[0].Entries.Count != 0)
                violations.Add(Violation(
                    SiteReservationValidationViolationCode.EntryIdentityMismatch,
                    SiteReservationValidationRule.EntryAnchors,
                    StartId, string.Empty, -1, placements[0].Entries.Count, 0,
                    "Start must not publish a special-site entry anchor."));

            for (var index = 1; index < placements.Count; index++)
            {
                var placement = placements[index];
                if (placement.Entries.Count != 1)
                {
                    violations.Add(Violation(
                        SiteReservationValidationViolationCode.MissingRequiredEntry,
                        SiteReservationValidationRule.EntryAnchors,
                        placement.Candidate.SourceDefinitionId, EntryId, -1,
                        placement.Entries.Count, 1,
                        "A required special site must have exactly one entry anchor."));
                    continue;
                }
                EvaluateEntry(
                    placement.Candidate.SourceDefinitionId,
                    placement.Entries[0].EntrySocketId,
                    placement.Entries[0].FootprintSector,
                    placement.Entries[0].ExteriorSector,
                    placement.Entries[0].Side,
                    placement.Entries[0].AllowedRouteTypes,
                    placement.Entries[0].Required,
                    placement.Entries[0].ReturnPathRequired,
                    occupied,
                    placement.OccupiedSectors,
                    violations);
            }

            var candidate = input.Approval.Village.Candidate;
            EvaluateEntry(
                VillageId,
                input.Approval.Village.EntryTemplate.EntrySocketId,
                WorldGridIndex.ToCoordinate(candidate.EntryFootprintSectorIndex),
                WorldGridIndex.ToCoordinate(candidate.EntryExteriorSectorIndex),
                candidate.EntrySide,
                input.Approval.Village.EntryTemplate.AllowedRouteTypes,
                input.Approval.Village.EntryTemplate.Required,
                input.Approval.Village.EntryTemplate.ReturnPathRequired,
                occupied,
                Coordinates(candidate.OccupiedSectorIndices),
                violations);
        }

        private static void EvaluateEntry(
            string sourceId,
            string entryId,
            SectorCoord footprintSector,
            SectorCoord exteriorSector,
            SiteEntrySide side,
            IReadOnlyList<int> routes,
            bool required,
            bool returnPathRequired,
            ISet<int> occupied,
            IReadOnlyList<SectorCoord> ownFootprint,
            ICollection<SiteReservationValidationViolation> violations)
        {
            var footprintIndex = WorldGridIndex.ToIndex(footprintSector);
            var exteriorIndex = WorldGridIndex.ToIndex(exteriorSector);
            if (!string.Equals(entryId, EntryId, StringComparison.Ordinal) || !required || !returnPathRequired)
                violations.Add(Violation(
                    SiteReservationValidationViolationCode.EntryIdentityMismatch,
                    SiteReservationValidationRule.EntryAnchors,
                    sourceId, CanonicalOrEmpty(entryId), footprintIndex, 0, 1,
                    "An entry socket ID or required-return flag is incorrect."));
            if (!ExactRoutes(routes))
                violations.Add(Violation(
                    SiteReservationValidationViolationCode.EntryRouteTypeMismatch,
                    SiteReservationValidationRule.EntryAnchors,
                    sourceId, EntryId, footprintIndex,
                    routes == null ? 0 : routes.Count, 3,
                    "An entry route set must equal unique ascending 1, 2, 3."));
            SiteReservationTokenCodec.GetDelta(side, out var deltaX, out var deltaY);
            if (exteriorSector != new SectorCoord(
                    footprintSector.X + deltaX, footprintSector.Y + deltaY))
                violations.Add(Violation(
                    SiteReservationValidationViolationCode.EntryIdentityMismatch,
                    SiteReservationValidationRule.EntryAnchors,
                    sourceId, EntryId, footprintIndex, exteriorIndex, -1,
                    "An entry exterior is not exactly one side step from its footprint cell."));
            if (!Contains(ownFootprint, footprintSector))
                violations.Add(Violation(
                    SiteReservationValidationViolationCode.EntryIdentityMismatch,
                    SiteReservationValidationRule.EntryAnchors,
                    sourceId, EntryId, footprintIndex, 0, 1,
                    "An entry anchor does not belong to its own footprint."));
            if (Contains(ownFootprint, exteriorSector))
                violations.Add(Violation(
                    SiteReservationValidationViolationCode.EntryFacesOwnFootprint,
                    SiteReservationValidationRule.EntryAnchors,
                    sourceId, EntryId, exteriorIndex, 1, 0,
                    "An entry side faces back into its own footprint."));
            if (occupied.Contains(exteriorIndex))
                violations.Add(Violation(
                    SiteReservationValidationViolationCode.EntryExteriorOccupied,
                    SiteReservationValidationRule.EntryAnchors,
                    sourceId, EntryId, exteriorIndex, 1, 0,
                    "An entry exterior sector is occupied."));
        }

        private static void EvaluateCapacity(
            InputSnapshot input,
            ICollection<SiteReservationValidationViolation> violations)
        {
            var witnesses = input.Approval.CoreCapacityApproval.Witnesses;
            var claimed = new Dictionary<int, string>();
            var village = new HashSet<int>(input.Approval.Village.Candidate.OccupiedSectorIndices);
            for (var index = 0; index < 4; index++)
            {
                var source = ExpectedSites[index + 2].Id;
                if (index >= witnesses.Count || witnesses[index] == null)
                {
                    violations.Add(Violation(
                        SiteReservationValidationViolationCode.MissingCapacityWitness,
                        SiteReservationValidationRule.CoreCapacity,
                        source, string.Empty, -1, 0, 1,
                        "A required Core capacity witness is missing."));
                    continue;
                }
                var witness = witnesses[index];
                var validIdentity = witness.Key == new SitePlacementKey(ExpectedSites[index + 2].Kind, source, 0) &&
                                    string.Equals(witness.BiomeId, CoreBiomes[index], StringComparison.Ordinal) &&
                                    string.Equals(witness.CorePatchRuleId, CoreRules[index], StringComparison.Ordinal) &&
                                    witness.MinimumCoreSectorCount == CoreMinimums[index] &&
                                    witness.BufferRingSectors == 1 &&
                                    witness.RequiredWitnessSectorCount == 5 &&
                                    witness.WitnessSectorIndices.Count == 5 &&
                                    witness.AvailableConnectedSectorCount >= 5 &&
                                    ContainsAll(witness.WitnessSectorIndices, witness.FootprintSectorIndices) &&
                                    ContainsAll(witness.WitnessSectorIndices, witness.MandatoryBufferSectorIndices);
                if (!validIdentity)
                    violations.Add(Violation(
                        SiteReservationValidationViolationCode.CapacityWitnessIdentityMismatch,
                        SiteReservationValidationRule.CoreCapacity,
                        source, string.Empty, witness.SeedSectorIndex,
                        witness.WitnessSectorIndices.Count, 5,
                        "A Core capacity witness violates its identity, inclusion, or target contract."));
                if (!CardinalConnected(witness.WitnessSectorIndices))
                    violations.Add(Violation(
                        SiteReservationValidationViolationCode.CapacityWitnessDisconnected,
                        SiteReservationValidationRule.CoreCapacity,
                        source, string.Empty, witness.SeedSectorIndex,
                        ConnectedCount(witness.WitnessSectorIndices), witness.WitnessSectorIndices.Count,
                        "A Core capacity witness is not cardinal-connected."));
                foreach (var sector in witness.WitnessSectorIndices)
                {
                    if (claimed.TryGetValue(sector, out var owner))
                        violations.Add(Violation(
                            SiteReservationValidationViolationCode.CapacityWitnessOverlap,
                            SiteReservationValidationRule.CoreCapacity,
                            owner, source, sector, 2, 1,
                            "Two Core capacity witnesses claim the same sector."));
                    else
                        claimed.Add(sector, source);
                    if (village.Contains(sector))
                        violations.Add(Violation(
                            SiteReservationValidationViolationCode.CapacityWitnessBlockedByVillage,
                            SiteReservationValidationRule.CoreCapacity,
                            source, VillageId, sector, 1, 0,
                            "The Village footprint blocks a protected Core capacity witness."));
                }
            }
        }

        private static SiteReservationValidationDiagnostics CreateDiagnostics(
            InputSnapshot input,
            IReadOnlyList<SiteReservationValidationViolation> violations)
        {
            var rules = new List<SiteReservationRuleResult>(6);
            for (var index = 0; index < 6; index++)
            {
                var rule = (SiteReservationValidationRule)index;
                var count = 0;
                foreach (var violation in violations)
                    if (violation.Rule == rule) count++;
                var measured = 0;
                var expected = 0;
                switch (rule)
                {
                    case SiteReservationValidationRule.RequiredSiteCounts:
                        measured = 7;
                        expected = 7;
                        break;
                    case SiteReservationValidationRule.WorldBounds:
                        measured = WorldGenConstants.SectorCount;
                        expected = WorldGenConstants.SectorCount;
                        break;
                    case SiteReservationValidationRule.FootprintOverlap:
                        measured = AllOccupied(input).Count;
                        expected = OccupiedCount(input);
                        break;
                    case SiteReservationValidationRule.DistanceConstraints:
                        measured = Math.Max(0, 22 - count);
                        expected = 22;
                        break;
                    case SiteReservationValidationRule.EntryAnchors:
                        measured = Math.Max(0, EntryCount(input) - count);
                        expected = 6;
                        break;
                    case SiteReservationValidationRule.CoreCapacity:
                        measured = Math.Max(0, WitnessSectorCount(input) - count);
                        expected = 20;
                        break;
                }
                rules.Add(new SiteReservationRuleResult(
                    rule,
                    count == 0,
                    count,
                    measured,
                    expected,
                    count == 0 ? RulePassMessage(rule) : RuleFailureMessage(rule)));
            }

            var reserved = AllOccupied(input).Count;
            return new SiteReservationValidationDiagnostics(
                rules,
                7,
                reserved,
                WorldGenConstants.SectorCount - reserved,
                EntryCount(input),
                RequiredEntryCount(input),
                15,
                6,
                1,
                input.Approval.CoreCapacityApproval.Witnesses.Count,
                WitnessSectorCount(input),
                4,
                violations.Count);
        }

        private static string RulePassMessage(SiteReservationValidationRule rule)
        {
            switch (rule)
            {
                case SiteReservationValidationRule.RequiredSiteCounts:
                    return "All required reservation counts are exact.";
                case SiteReservationValidationRule.WorldBounds:
                    return "All reservation coordinates are within the frozen world grid.";
                case SiteReservationValidationRule.FootprintOverlap:
                    return "Reservation footprints and entry approaches are disjoint.";
                case SiteReservationValidationRule.DistanceConstraints:
                    return "All non-Village, Village, and Core-cluster distance checks pass.";
                case SiteReservationValidationRule.EntryAnchors:
                    return "All required entry anchors are outward, open, and route-compatible.";
                case SiteReservationValidationRule.CoreCapacity:
                    return "All four Core capacity witnesses preserve five connected sectors.";
                default:
                    throw new ArgumentOutOfRangeException(nameof(rule));
            }
        }

        private static string RuleFailureMessage(SiteReservationValidationRule rule)
        {
            switch (rule)
            {
                case SiteReservationValidationRule.RequiredSiteCounts:
                    return "One or more required reservation counts are invalid.";
                case SiteReservationValidationRule.WorldBounds:
                    return "One or more reservation coordinates violate the world grid.";
                case SiteReservationValidationRule.FootprintOverlap:
                    return "One or more footprints or entry approaches conflict.";
                case SiteReservationValidationRule.DistanceConstraints:
                    return "One or more distance constraints are not satisfied.";
                case SiteReservationValidationRule.EntryAnchors:
                    return "One or more entry anchors violate their frozen contract.";
                case SiteReservationValidationRule.CoreCapacity:
                    return "One or more Core capacity witnesses are invalid.";
                default:
                    throw new ArgumentOutOfRangeException(nameof(rule));
            }
        }

        private static IReadOnlyList<SiteReservationValidationViolation> CanonicalizeViolations(
            IEnumerable<SiteReservationValidationViolation> source)
        {
            var values = new List<SiteReservationValidationViolation>(source);
            values.Sort(CompareViolations);
            var unique = new List<SiteReservationValidationViolation>();
            foreach (var value in values)
                if (unique.Count == 0 || CompareViolations(unique[unique.Count - 1], value) != 0)
                    unique.Add(value);
            return new ReadOnlyCollection<SiteReservationValidationViolation>(unique);
        }

        private static int CompareViolations(
            SiteReservationValidationViolation left,
            SiteReservationValidationViolation right)
        {
            var value = left.Rule.CompareTo(right.Rule);
            if (value != 0) return value;
            value = left.Code.CompareTo(right.Code);
            if (value != 0) return value;
            value = string.Compare(left.FirstId, right.FirstId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = string.Compare(left.SecondId, right.SecondId, StringComparison.Ordinal);
            if (value != 0) return value;
            value = left.SectorIndex.CompareTo(right.SectorIndex);
            if (value != 0) return value;
            value = left.MeasuredValue.CompareTo(right.MeasuredValue);
            if (value != 0) return value;
            value = left.ExpectedValue.CompareTo(right.ExpectedValue);
            return value != 0 ? value : string.Compare(left.Message, right.Message, StringComparison.Ordinal);
        }

        private static SiteReservationValidationResult Invalid(
            IEnumerable<SiteReservationValidationError> errors) =>
            new SiteReservationValidationResult(
                SiteReservationValidationStatus.InvalidInput,
                null,
                null,
                Array.Empty<SiteReservationValidationViolation>(),
                errors);

        private static SiteReservationValidationError Error(
            SiteReservationValidationErrorCode code,
            string definitionId,
            string childId,
            int sectorIndex,
            string message) =>
            new SiteReservationValidationError(
                code, definitionId, childId, sectorIndex, message);

        private static SiteReservationValidationViolation Violation(
            SiteReservationValidationViolationCode code,
            SiteReservationValidationRule rule,
            string firstId,
            string secondId,
            int sectorIndex,
            int measured,
            int expected,
            string message) =>
            new SiteReservationValidationViolation(
                code, rule, firstId, secondId, sectorIndex, measured, expected, message);

        private static bool MatchesMap(SpecialMapDefinition map, ExpectedSite expected)
        {
            return map != null && map.Active && map.RequiredCount == 1 &&
                   string.Equals(map.SpecialMapId, expected.Id, StringComparison.Ordinal) &&
                   string.Equals(map.SiteRole, SiteReservationTokenCodec.ToToken(expected.Kind), StringComparison.Ordinal) &&
                   string.Equals(map.PrimaryBiomeId, expected.BiomeId, StringComparison.Ordinal) &&
                   map.FootprintWidthSectors == expected.Width &&
                   map.FootprintHeightSectors == expected.Height &&
                   map.MinGraphDistanceFromStart == expected.StartDistance &&
                   map.MinGraphDistanceToOtherCoreSites == expected.OtherDistance &&
                   string.Equals(map.GenerationMode, expected.GenerationMode, StringComparison.Ordinal) &&
                   ExactRoutes(map.AllowedEntryRouteTypes);
        }

        private static bool ExactRoutes(IReadOnlyList<int> routes) =>
            routes != null && routes.Count == 3 &&
            routes[0] == 1 && routes[1] == 2 && routes[2] == 3;

        private static bool ValidSideTokens(IReadOnlyList<string> tokens)
        {
            if (tokens == null) return false;
            var seen = new HashSet<SiteEntrySide>();
            foreach (var token in tokens)
                if (!SiteReservationTokenCodec.TryParseEntrySide(token, out var side) || !seen.Add(side))
                    return false;
            return true;
        }

        private static bool TransformedSidesEqual(
            IReadOnlyList<string> source,
            SiteFootprintTransform transform,
            IReadOnlyList<SiteEntrySide> actual)
        {
            if (source == null || actual == null || source.Count != actual.Count) return false;
            var expected = new List<SiteEntrySide>();
            foreach (var token in source)
            {
                if (!SiteReservationTokenCodec.TryParseEntrySide(token, out var sourceSide) ||
                    !SiteFootprintTransformer.TryTransformSide(transform, sourceSide, out var side))
                    return false;
                expected.Add(side);
            }
            expected.Sort();
            for (var index = 0; index < expected.Count; index++)
                if (expected[index] != actual[index]) return false;
            return true;
        }

        private static int MinimumDistance(
            IReadOnlyList<int> first,
            IReadOnlyList<int> second)
        {
            var best = int.MaxValue;
            foreach (var firstIndex in first)
            {
                var firstSector = WorldGridIndex.ToCoordinate(firstIndex);
                foreach (var secondIndex in second)
                {
                    var secondSector = WorldGridIndex.ToCoordinate(secondIndex);
                    best = Math.Min(best,
                        Math.Abs(firstSector.X - secondSector.X) +
                        Math.Abs(firstSector.Y - secondSector.Y));
                }
            }
            return best;
        }

        private static IReadOnlyList<int> SortedIndices(IReadOnlyList<SectorCoord> sectors)
        {
            var values = new List<int>(sectors.Count);
            foreach (var sector in sectors) values.Add(WorldGridIndex.ToIndex(sector));
            values.Sort();
            return values;
        }

        private static IReadOnlyList<SectorCoord> Coordinates(IReadOnlyList<int> indices)
        {
            var result = new List<SectorCoord>(indices.Count);
            foreach (var index in indices) result.Add(WorldGridIndex.ToCoordinate(index));
            return result;
        }

        private static bool SequenceEqual<T>(IReadOnlyList<T> first, IReadOnlyList<T> second)
        {
            if (first == null || second == null || first.Count != second.Count) return false;
            var comparer = EqualityComparer<T>.Default;
            for (var index = 0; index < first.Count; index++)
                if (!comparer.Equals(first[index], second[index])) return false;
            return true;
        }

        private static bool ContainsAll(IReadOnlyList<int> superset, IReadOnlyList<int> subset)
        {
            if (superset == null || subset == null) return false;
            var values = new HashSet<int>(superset);
            foreach (var item in subset)
                if (!values.Contains(item)) return false;
            return true;
        }

        private static bool CardinalConnected(IReadOnlyList<int> indices) =>
            indices != null && indices.Count > 0 && ConnectedCount(indices) == indices.Count;

        private static int ConnectedCount(IReadOnlyList<int> indices)
        {
            if (indices == null || indices.Count == 0) return 0;
            var remaining = new HashSet<int>(indices);
            var queue = new Queue<int>();
            queue.Enqueue(indices[0]);
            remaining.Remove(indices[0]);
            var count = 0;
            while (queue.Count != 0)
            {
                var current = queue.Dequeue();
                count++;
                var coordinate = WorldGridIndex.ToCoordinate(current);
                AddConnected(coordinate.X - 1, coordinate.Y, remaining, queue);
                AddConnected(coordinate.X + 1, coordinate.Y, remaining, queue);
                AddConnected(coordinate.X, coordinate.Y - 1, remaining, queue);
                AddConnected(coordinate.X, coordinate.Y + 1, remaining, queue);
            }
            return count;
        }

        private static void AddConnected(
            int x,
            int y,
            ISet<int> remaining,
            Queue<int> queue)
        {
            if (x < 0 || x >= WorldGenConstants.SectorColumns ||
                y < 0 || y >= WorldGenConstants.SectorRows) return;
            var index = WorldGridIndex.ToIndex(new SectorCoord(x, y));
            if (remaining.Remove(index)) queue.Enqueue(index);
        }

        private static HashSet<int> AllOccupied(InputSnapshot input)
        {
            var result = new HashSet<int>();
            foreach (var placement in input.Approval.CoreCapacityApproval.SelectionPlan.SelectedPlacements)
                foreach (var sector in placement.OccupiedSectors)
                    result.Add(WorldGridIndex.ToIndex(sector));
            foreach (var index in input.Approval.Village.Candidate.OccupiedSectorIndices)
                result.Add(index);
            return result;
        }

        private static int OccupiedCount(InputSnapshot input)
        {
            var count = input.Approval.Village.Candidate.OccupiedSectorIndices.Count;
            foreach (var placement in input.Approval.CoreCapacityApproval.SelectionPlan.SelectedPlacements)
                checked { count += placement.OccupiedSectors.Count; }
            return count;
        }

        private static int EntryCount(InputSnapshot input)
        {
            var count = 1;
            foreach (var placement in input.Approval.CoreCapacityApproval.SelectionPlan.SelectedPlacements)
                checked { count += placement.Entries.Count; }
            return count;
        }

        private static int RequiredEntryCount(InputSnapshot input)
        {
            var count = input.Approval.Village.EntryTemplate.Required ? 1 : 0;
            foreach (var placement in input.Approval.CoreCapacityApproval.SelectionPlan.SelectedPlacements)
                foreach (var entry in placement.Entries)
                    if (entry.Required) count++;
            return count;
        }

        private static int WitnessSectorCount(InputSnapshot input)
        {
            var count = 0;
            foreach (var witness in input.Approval.CoreCapacityApproval.Witnesses)
                checked { count += witness.WitnessSectorIndices.Count; }
            return count;
        }

        private static bool Contains(IReadOnlyList<SectorCoord> values, SectorCoord value)
        {
            foreach (var item in values)
                if (item == value) return true;
            return false;
        }

        private static void AddCount<TKey>(IDictionary<TKey, int> counts, TKey key)
        {
            if (counts.TryGetValue(key, out var count)) counts[key] = count + 1;
            else counts.Add(key, 1);
        }

        private static bool IsWorld(SectorCoord coordinate) =>
            coordinate.X >= 0 && coordinate.X < WorldGenConstants.SectorColumns &&
            coordinate.Y >= 0 && coordinate.Y < WorldGenConstants.SectorRows;

        private static string CanonicalOrEmpty(string value) =>
            SitePlacementKey.IsCanonicalId(value) ? value : string.Empty;

        private static string CoordinateId(int x, int y)
        {
            var first = x < 0 ? "N" + (-x) : x.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var second = y < 0 ? "N" + (-y) : y.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return "CELL_" + first + "_" + second;
        }

        private static ExpectedSite FindExpected(string id)
        {
            foreach (var expected in ExpectedSites)
                if (string.Equals(expected.Id, id, StringComparison.Ordinal)) return expected;
            return null;
        }

        private sealed class ExpectedSite
        {
            public ExpectedSite(
                string id,
                SiteReservationKind kind,
                string biomeId,
                int width,
                int height,
                int startDistance,
                int otherDistance,
                string generationMode)
            {
                Id = id;
                Kind = kind;
                BiomeId = biomeId;
                Width = width;
                Height = height;
                StartDistance = startDistance;
                OtherDistance = otherDistance;
                GenerationMode = generationMode;
            }

            public string Id { get; }
            public SiteReservationKind Kind { get; }
            public string BiomeId { get; }
            public int Width { get; }
            public int Height { get; }
            public int StartDistance { get; }
            public int OtherDistance { get; }
            public string GenerationMode { get; }
        }

        private sealed class InputSnapshot
        {
            public InputSnapshot(
                VillageReservationApproval approval,
                IReadOnlyList<SpecialMapDefinition> maps,
                IReadOnlyList<SpecialMapFootprintCellDefinition> cells,
                IReadOnlyList<SpecialMapEntrySocketDefinition> entries,
                IReadOnlyDictionary<string, SpecialMapDefinition> mapsById,
                IReadOnlyDictionary<string, List<SpecialMapFootprintCellDefinition>> cellsByMap,
                IReadOnlyDictionary<string, List<SpecialMapEntrySocketDefinition>> entriesByMap)
            {
                Approval = approval;
                Maps = maps;
                Cells = cells;
                Entries = entries;
                MapsById = mapsById;
                CellsByMap = cellsByMap;
                EntriesByMap = entriesByMap;
            }

            public VillageReservationApproval Approval { get; }
            public IReadOnlyList<SpecialMapDefinition> Maps { get; }
            public IReadOnlyList<SpecialMapFootprintCellDefinition> Cells { get; }
            public IReadOnlyList<SpecialMapEntrySocketDefinition> Entries { get; }
            public IReadOnlyDictionary<string, SpecialMapDefinition> MapsById { get; }
            public IReadOnlyDictionary<string, List<SpecialMapFootprintCellDefinition>> CellsByMap { get; }
            public IReadOnlyDictionary<string, List<SpecialMapEntrySocketDefinition>> EntriesByMap { get; }
        }
    }
}
