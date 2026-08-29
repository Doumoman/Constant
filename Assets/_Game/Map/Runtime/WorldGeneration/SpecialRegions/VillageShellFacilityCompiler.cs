using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Generation;

namespace StarNight.Map.WorldGeneration.SpecialRegions
{
    public static class VillageShellFacilityCompiler
    {
        public static VillageShellResult Compile(VillageShellCompileRequest request)
        {
            var errors = new List<VillageShellError>();
            if (request == null)
            {
                Add(errors, VillageShellErrorCode.MissingInput, "request", "Compile request is required.");
                return Failure(errors);
            }

            var bridge = request.Bridge;
            var entry = request.EntryBufferPlan;
            var fixedSlots = request.FixedSlotPlan;
            var definition = request.Definition;
            if (bridge == null) Add(errors, VillageShellErrorCode.MissingInput, "bridge", "Site bridge is required.");
            if (entry == null) Add(errors, VillageShellErrorCode.MissingInput, "entryBuffer", "Entry buffer plan is required.");
            if (fixedSlots == null) Add(errors, VillageShellErrorCode.MissingInput, "fixedSlots", "Fixed-slot plan is required.");
            if (definition == null) Add(errors, VillageShellErrorCode.MissingInput, "definition", "Village shell definition is required.");
            if (bridge == null || entry == null || fixedSlots == null || definition == null) return Failure(errors);

            ValidateSourceIdentity(request, errors);
            ValidateVillage(bridge, fixedSlots, errors);

            if (!TryShape(definition.Shape, out var expectedWidth, out var expectedHeight))
                Add(errors, VillageShellErrorCode.UnsupportedShape, "definition.shape", "Only 1x1, 2x1, and 1x2 layouts are supported.");
            else ValidateFootprint(bridge, expectedWidth, expectedHeight, errors);

            if (!IsStableToken(definition.LayoutId.Value))
                Add(errors, VillageShellErrorCode.NonCanonicalPublication, "definition.layoutId", "Layout ID must be an explicit stable token.");

            var projectedRoad = ValidateRoad(
                bridge, entry, fixedSlots, definition,
                expectedWidth, expectedHeight, errors);
            var facilityBindings = ValidateFacilities(
                bridge, fixedSlots, definition, projectedRoad, errors);

            if (errors.Count != 0) return Failure(errors);

            var plan = new VillageShellPlan(
                bridge, entry, fixedSlots, definition, projectedRoad, facilityBindings);
            if (string.IsNullOrEmpty(plan.CanonicalDigest) ||
                !string.Equals(plan.CanonicalDigest, VillageShellCanonicalDigest.Compute(plan), StringComparison.Ordinal))
            {
                Add(errors, VillageShellErrorCode.NonCanonicalPublication, "plan.digest", "Published plan is not canonical.");
                return Failure(errors);
            }

            return new VillageShellResult(plan, Array.Empty<VillageShellError>());
        }

        private static void ValidateSourceIdentity(
            VillageShellCompileRequest request,
            ICollection<VillageShellError> errors)
        {
            var bridge = request.Bridge;
            var entry = request.EntryBufferPlan;
            var fixedSlots = request.FixedSlotPlan;

            ValidateDigest(
                request.ExpectedBridgeDigest, bridge.CanonicalDigest,
                () => SpecialRegionSiteBridgeCanonicalDigest.Compute(bridge),
                "bridge.digest", errors);
            ValidateDigest(
                request.ExpectedEntryBufferDigest, entry.CanonicalDigest,
                () => SpecialRegionEntryBufferCanonicalDigest.Compute(entry),
                "entryBuffer.digest", errors);
            ValidateDigest(
                request.ExpectedFixedSlotDigest, fixedSlots.CanonicalDigest,
                () => SpecialRegionFixedSlotLayerCanonicalDigest.Compute(fixedSlots),
                "fixedSlots.digest", errors);

            if (entry.RegionId != bridge.RegionId || fixedSlots.RegionId != bridge.RegionId)
                Add(errors, VillageShellErrorCode.DigestMismatch, "sources.regionId", "Source region identities must match.");
            if (entry.ReservationId != bridge.ReservationId || fixedSlots.ReservationId != bridge.ReservationId)
                Add(errors, VillageShellErrorCode.DigestMismatch, "sources.reservationId", "Source reservation identities must match.");
            if (!string.Equals(entry.BridgeDigest, bridge.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(fixedSlots.BridgeDigest, bridge.CanonicalDigest, StringComparison.Ordinal))
                Add(errors, VillageShellErrorCode.DigestMismatch, "sources.bridgeDigest", "Downstream source bridge digests must match the current bridge.");
            if (!string.Equals(fixedSlots.EntryBufferDigest, entry.CanonicalDigest, StringComparison.Ordinal))
                Add(errors, VillageShellErrorCode.DigestMismatch, "sources.entryBufferDigest", "Fixed-slot source must bind the current entry buffer.");
        }

        private static void ValidateDigest(
            string expected,
            string published,
            Func<string> compute,
            string path,
            ICollection<VillageShellError> errors)
        {
            string computed;
            try { computed = compute(); }
            catch (Exception exception)
            {
                Add(errors, VillageShellErrorCode.DigestMismatch, path, "Source digest could not be recomputed: " + exception.GetType().Name + ".");
                return;
            }

            if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(published) ||
                !string.Equals(expected, published, StringComparison.Ordinal) ||
                !string.Equals(computed, published, StringComparison.Ordinal))
                Add(errors, VillageShellErrorCode.DigestMismatch, path, "Expected, published, and recomputed digests must match.");
        }

        private static void ValidateVillage(
            SpecialRegionSiteBridge bridge,
            SpecialRegionFixedSlotLayerPlan fixedSlots,
            ICollection<VillageShellError> errors)
        {
            if (bridge.RegionKind != SpecialRegionKind.Village || fixedSlots.RegionKind != SpecialRegionKind.Village)
                Add(errors, VillageShellErrorCode.NotVillage, "sources.regionKind", "Both source plans must identify a Village.");
        }

        private static bool TryShape(VillageLayoutShape shape, out int width, out int height)
        {
            width = 0;
            height = 0;
            switch (shape)
            {
                case VillageLayoutShape.OneByOne: width = 1; height = 1; return true;
                case VillageLayoutShape.TwoByOne: width = 2; height = 1; return true;
                case VillageLayoutShape.OneByTwo: width = 1; height = 2; return true;
                default: return false;
            }
        }

        private static void ValidateFootprint(
            SpecialRegionSiteBridge bridge,
            int expectedWidth,
            int expectedHeight,
            ICollection<VillageShellError> errors)
        {
            if (bridge.Width != expectedWidth || bridge.Height != expectedHeight)
                Add(errors, VillageShellErrorCode.ShapeMismatch, "bridge.shape", "Bridge dimensions do not match the explicit layout shape.");

            var expected = new HashSet<SpecialRegionSectorOffset>();
            for (var y = 0; y < expectedHeight; y++)
                for (var x = 0; x < expectedWidth; x++)
                    expected.Add(new SpecialRegionSectorOffset(x, y));

            var placed = new HashSet<SpecialRegionSectorOffset>(bridge.PlacedFootprint);
            var bound = new HashSet<SpecialRegionSectorOffset>(bridge.SectorBindings.Select(value => value.PlacedOffset));
            if (placed.Count != expected.Count || !placed.SetEquals(expected) ||
                bound.Count != expected.Count || !bound.SetEquals(expected))
                Add(errors, VillageShellErrorCode.ShapeMismatch, "bridge.footprint", "Bridge footprint must be one exact full rectangle.");
        }

        private static List<VillageRoadCell> ValidateRoad(
            SpecialRegionSiteBridge bridge,
            SpecialRegionEntryBufferPlan entry,
            SpecialRegionFixedSlotLayerPlan fixedSlots,
            VillageShellDefinition definition,
            int expectedWidth,
            int expectedHeight,
            ICollection<VillageShellError> errors)
        {
            var supplied = definition.RoadCells.ToArray();
            if (supplied.Any(value => value == null))
                Add(errors, VillageShellErrorCode.MissingInput, "definition.road", "Road cells cannot contain null.");
            var road = supplied.Where(value => value != null).OrderBy(value => value.Order).ToArray();
            if (road.Length == 0)
                Add(errors, VillageShellErrorCode.InvalidRoad, "definition.road", "At least one explicit road cell is required.");

            if (road.Select(value => value.Order).Distinct().Count() != road.Length ||
                road.Where((value, index) => value.Order != index).Any())
                Add(errors, VillageShellErrorCode.InvalidRoad, "definition.road.order", "Road order must be unique and contiguous from zero.");

            var duplicates = road.GroupBy(value => value.RegionTile)
                .Where(group => group.Count() > 1).Select(group => group.Key).OrderBy(value => value.Y).ThenBy(value => value.X);
            foreach (var duplicate in duplicates)
                Add(errors, VillageShellErrorCode.InvalidRoad, "definition.road/" + Tile(duplicate), "Road coordinates must be unique.");

            var projected = new List<VillageRoadCell>();
            foreach (var cell in road)
            {
                if (TryProject(bridge, cell.RegionTile, out var placed))
                    projected.Add(new VillageRoadCell(cell.Order, cell.RegionTile, placed, true));
                else
                    Add(errors, VillageShellErrorCode.CoordinateOutOfRange,
                        "definition.road/" + Tile(cell.RegionTile), "Road coordinate is outside the placed region or cannot round-trip.");
            }

            for (var index = 1; index < road.Length; index++)
                if (!IsCardinal(road[index - 1].RegionTile, road[index].RegionTile))
                    Add(errors, VillageShellErrorCode.DisconnectedRoad,
                        "definition.road/" + road[index].Order, "Ordered road cells must be cardinal-adjacent.");

            var roadSet = new HashSet<LocalTileCoord>(road.Select(value => value.RegionTile));
            ValidateApronConnections(bridge, entry, road, roadSet, errors);
            ValidateSectorCoverage(roadSet, expectedWidth, expectedHeight, errors);
            ValidateSeam(road, definition.Shape, errors);

            var collision = new HashSet<LocalTileCoord>(fixedSlots.FixedCollision.Select(value => value.Placed.RegionTile));
            var facilitySlots = new HashSet<LocalTileCoord>(fixedSlots.ReplaceableSlots
                .Where(value => value.Kind == SpecialRegionSlotKind.Facility).Select(value => value.Placed.RegionTile));
            foreach (var cell in roadSet.Where(value => collision.Contains(value) || facilitySlots.Contains(value))
                         .OrderBy(value => value.Y).ThenBy(value => value.X))
                Add(errors, VillageShellErrorCode.RoadCollision, "definition.road/" + Tile(cell), "Road cannot overlap fixed collision or a Facility slot.");

            return projected;
        }

        private static void ValidateApronConnections(
            SpecialRegionSiteBridge bridge,
            SpecialRegionEntryBufferPlan entry,
            IReadOnlyList<VillageRoadCell> road,
            ISet<LocalTileCoord> roadSet,
            ICollection<VillageShellError> errors)
        {
            if (entry.EntryPort == null || entry.ReturnPort == null)
            {
                Add(errors, VillageShellErrorCode.MissingApronConnection, "entryBuffer.ports", "Entry and Return ports are required.");
                return;
            }

            var entryCells = ApronRegionCells(bridge, entry, entry.EntryPort.PortId);
            var returnCells = ApronRegionCells(bridge, entry, entry.ReturnPort.PortId);
            var entryConnected = roadSet.Any(entryCells.Contains);
            var returnConnected = roadSet.Any(returnCells.Contains);
            if (!entryConnected || road.Count == 0 || !entryCells.Contains(road[0].RegionTile))
                Add(errors, VillageShellErrorCode.MissingApronConnection, "definition.road.entry", "Road witness must begin in the Entry apron.");
            if (!returnConnected || road.Count == 0 || !returnCells.Contains(road[road.Count - 1].RegionTile))
                Add(errors, VillageShellErrorCode.MissingApronConnection, "definition.road.return", "Road witness must end in the Return apron.");
        }

        private static HashSet<LocalTileCoord> ApronRegionCells(
            SpecialRegionSiteBridge bridge,
            SpecialRegionEntryBufferPlan entry,
            string portId)
        {
            var values = new HashSet<LocalTileCoord>();
            foreach (var cell in entry.Aprons.Where(value => value != null &&
                         string.Equals(value.PortId, portId, StringComparison.Ordinal)).SelectMany(value => value.Cells))
                if (TryUnproject(bridge, cell, out var regionTile)) values.Add(regionTile);
            return values;
        }

        private static void ValidateSectorCoverage(
            ISet<LocalTileCoord> road,
            int width,
            int height,
            ICollection<VillageShellError> errors)
        {
            for (var sectorY = 0; sectorY < height; sectorY++)
                for (var sectorX = 0; sectorX < width; sectorX++)
                    if (!road.Any(value =>
                        value.X / WorldGenConstants.SectorWidthTiles == sectorX &&
                        value.Y / WorldGenConstants.SectorHeightTiles == sectorY))
                        Add(errors, VillageShellErrorCode.MissingSectorCoverage,
                            "definition.road/sector/" + sectorX + "," + sectorY,
                            "Central road must cross every active sector.");
        }

        private static void ValidateSeam(
            IReadOnlyList<VillageRoadCell> road,
            VillageLayoutShape shape,
            ICollection<VillageShellError> errors)
        {
            if (shape == VillageLayoutShape.OneByOne) return;
            var crossed = false;
            for (var index = 1; index < road.Count; index++)
            {
                var left = road[index - 1].RegionTile;
                var right = road[index].RegionTile;
                if (shape == VillageLayoutShape.TwoByOne && left.Y == right.Y &&
                    ((left.X == 47 && right.X == 48) || (left.X == 48 && right.X == 47))) crossed = true;
                if (shape == VillageLayoutShape.OneByTwo && left.X == right.X &&
                    ((left.Y == 31 && right.Y == 32) || (left.Y == 32 && right.Y == 31))) crossed = true;
            }
            if (!crossed)
                Add(errors, VillageShellErrorCode.MissingSeamCrossing, "definition.road.seam", "Central road must cross the internal sector seam with a cardinal pair.");
        }

        private static List<VillageFacilityBinding> ValidateFacilities(
            SpecialRegionSiteBridge bridge,
            SpecialRegionFixedSlotLayerPlan fixedSlots,
            VillageShellDefinition definition,
            IEnumerable<VillageRoadCell> projectedRoad,
            ICollection<VillageShellError> errors)
        {
            var supplied = definition.Facilities.ToArray();
            if (supplied.Any(value => value == null))
                Add(errors, VillageShellErrorCode.MissingInput, "definition.facilities", "Facility definitions cannot contain null.");
            var facilities = supplied.Where(value => value != null)
                .OrderBy(value => value.Kind).ThenBy(value => value.DefinitionId, StringComparer.Ordinal).ToArray();

            ValidateFacilityCounts(facilities, errors);
            ValidateFacilityIdentities(facilities, errors);

            var slots = fixedSlots.ReplaceableSlots.Where(value => value.Kind == SpecialRegionSlotKind.Facility)
                .OrderBy(value => value.SlotId).ToArray();
            if ((slots.Length != 5 && slots.Length != 6) || slots.Length != facilities.Length)
                Add(errors, VillageShellErrorCode.FacilitySlotMismatch, "fixedSlots.facilities", "Facility slots and definitions must have the same exact total of five or six.");

            var slotById = slots.GroupBy(value => value.SlotId).ToDictionary(group => group.Key, group => group.First());
            var witnesses = definition.AccessWitnesses.Where(value => value != null).ToArray();
            if (definition.AccessWitnesses.Any(value => value == null))
                Add(errors, VillageShellErrorCode.MissingInput, "definition.accessWitnesses", "Access witnesses cannot contain null.");
            foreach (var group in witnesses.GroupBy(value => value.FacilityDefinitionId, StringComparer.Ordinal).Where(group => group.Count() > 1))
                Add(errors, VillageShellErrorCode.InvalidAccessWitness, "definition.access/" + group.Key, "Each facility must have one witness.");
            foreach (var group in witnesses.GroupBy(value => value.WitnessId, StringComparer.Ordinal).Where(group => group.Count() > 1))
                Add(errors, VillageShellErrorCode.InvalidAccessWitness, "definition.accessWitnessId/" + group.Key, "Witness IDs must be unique.");

            var witnessByFacility = witnesses.GroupBy(value => value.FacilityDefinitionId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            if (witnesses.Length != facilities.Length)
                Add(errors, VillageShellErrorCode.InvalidAccessWitness, "definition.accessWitnesses", "Witness count must equal Facility count.");

            var roadSet = new HashSet<LocalTileCoord>(projectedRoad.Select(value => value.RegionTile));
            var fixedCollision = new HashSet<LocalTileCoord>(fixedSlots.FixedCollision.Select(value => value.Placed.RegionTile));
            var facilityCells = new HashSet<LocalTileCoord>(slots.Select(value => value.Placed.RegionTile));
            var doors = new HashSet<LocalTileCoord>();
            var bindings = new List<VillageFacilityBinding>();

            foreach (var facility in facilities)
            {
                var path = "definition.facilities/" + facility.DefinitionId;
                if (!slotById.TryGetValue(facility.SlotId, out var slot))
                {
                    Add(errors, VillageShellErrorCode.FacilitySlotMismatch, path + "/slot", "Definition must bind an exact MAP13_03 Facility slot.");
                    continue;
                }

                ValidateAssignment(facility, slot, path, errors);
                if (!doors.Add(facility.DoorRegionTile))
                    Add(errors, VillageShellErrorCode.InvalidDoor, path + "/door", "Facility doors must be unique.");
                if (!TryProject(bridge, facility.DoorRegionTile, out var door))
                    Add(errors, VillageShellErrorCode.CoordinateOutOfRange, path + "/door", "Door is outside the placed region.");
                else if (!IsCardinal(slot.Placed.RegionTile, facility.DoorRegionTile) ||
                         fixedCollision.Contains(facility.DoorRegionTile) || facilityCells.Contains(facility.DoorRegionTile))
                    Add(errors, VillageShellErrorCode.InvalidDoor, path + "/door", "Door must be cardinal-adjacent to its slot and collision-free.");

                if (!witnessByFacility.TryGetValue(facility.DefinitionId, out var witness))
                {
                    Add(errors, VillageShellErrorCode.InvalidAccessWitness, path + "/access", "Facility access witness is required.");
                    continue;
                }
                if (!IsStableToken(witness.WitnessId))
                    Add(errors, VillageShellErrorCode.NonCanonicalPublication, path + "/access/id", "Witness ID must be an explicit stable token.");

                var projected = ValidateAccessWitness(
                    bridge, facility, witness, roadSet, fixedCollision, facilityCells, path, errors);
                if (TryProject(bridge, facility.DoorRegionTile, out door) && projected.Count == witness.Cells.Count)
                    bindings.Add(new VillageFacilityBinding(facility, slot, door, witness, projected));
            }

            foreach (var witness in witnesses.Where(value => !facilities.Any(facility =>
                         string.Equals(facility.DefinitionId, value.FacilityDefinitionId, StringComparison.Ordinal))))
                Add(errors, VillageShellErrorCode.InvalidAccessWitness,
                    "definition.access/" + witness.WitnessId, "Witness references an unknown facility definition.");

            return bindings;
        }

        private static void ValidateFacilityCounts(
            IReadOnlyCollection<VillageFacilityDefinition> facilities,
            ICollection<VillageShellError> errors)
        {
            var kitchens = facilities.Count(value => value.Kind == VillageFacilityKind.Kitchen &&
                value.Requirement == VillageFacilityRequirement.Required);
            var repairs = facilities.Count(value => value.Kind == VillageFacilityKind.Repair &&
                value.Requirement == VillageFacilityRequirement.Required);
            var optional = facilities.Count(value => value.Kind == VillageFacilityKind.Optional &&
                value.Requirement == VillageFacilityRequirement.Optional);
            if (kitchens != 1) Add(errors, VillageShellErrorCode.MissingKitchen, "definition.facilities.kitchen", "Exactly one required Kitchen is required.");
            if (repairs != 1) Add(errors, VillageShellErrorCode.MissingRepair, "definition.facilities.repair", "Exactly one required Repair is required.");
            if (optional != 3 && optional != 4) Add(errors, VillageShellErrorCode.InvalidOptionalCount, "definition.facilities.optional", "Exactly three or four optional facilities are required.");
            if (facilities.Count != kitchens + repairs + optional || (facilities.Count != 5 && facilities.Count != 6))
                Add(errors, VillageShellErrorCode.InvalidOptionalCount, "definition.facilities", "Only the exact Kitchen, Repair, and Optional matrix is allowed.");
        }

        private static void ValidateFacilityIdentities(
            IEnumerable<VillageFacilityDefinition> facilities,
            ICollection<VillageShellError> errors)
        {
            foreach (var facility in facilities)
            {
                if (!IsStableToken(facility.DefinitionId) || !IsStableToken(facility.SlotId.Value))
                    Add(errors, VillageShellErrorCode.NonCanonicalPublication,
                        "definition.facilities/" + facility.DefinitionId, "Definition and slot IDs must be explicit stable tokens.");
            }
            foreach (var group in facilities.GroupBy(value => value.DefinitionId, StringComparer.Ordinal).Where(group => group.Count() > 1))
                Add(errors, VillageShellErrorCode.DuplicateFacility, "definition.facilities/" + group.Key, "Facility definition IDs must be unique.");
            foreach (var group in facilities.GroupBy(value => value.SlotId).Where(group => group.Count() > 1))
                Add(errors, VillageShellErrorCode.DuplicateFacility, "definition.facilitySlots/" + group.Key.Value, "Each Facility slot may be bound once.");
        }

        private static void ValidateAssignment(
            VillageFacilityDefinition facility,
            SpecialRegionReplaceableSlotBinding slot,
            string path,
            ICollection<VillageShellError> errors)
        {
            var requiredKind = facility.Kind == VillageFacilityKind.Kitchen || facility.Kind == VillageFacilityKind.Repair;
            var semanticRequired = facility.Requirement == VillageFacilityRequirement.Required;
            if (requiredKind != semanticRequired ||
                (facility.Kind == VillageFacilityKind.Optional && facility.Requirement != VillageFacilityRequirement.Optional))
                Add(errors, VillageShellErrorCode.FacilitySlotMismatch, path + "/requirement", "Required/optional meaning must be explicit and consistent with Facility kind.");

            if (semanticRequired && !slot.IsAssigned)
                Add(errors, VillageShellErrorCode.RequiredFacilityClear, path + "/assignment", "Kitchen and Repair cannot use Clear intent.");
            if (slot.IsAssigned)
            {
                if (slot.OccupantKind != SpecialRegionSlotKind.Facility || string.IsNullOrEmpty(slot.OccupantId) ||
                    !string.Equals(slot.OccupantId, facility.OccupantId, StringComparison.Ordinal))
                    Add(errors, VillageShellErrorCode.FacilitySlotMismatch, path + "/assignment", "Explicit definition assignment must match the MAP13_03 slot assignment.");
            }
            else if (!string.IsNullOrEmpty(facility.OccupantId))
                Add(errors, VillageShellErrorCode.FacilitySlotMismatch, path + "/assignment", "An explicit empty optional slot cannot name an occupant.");
        }

        private static List<SpecialRegionPlacedCoordinate> ValidateAccessWitness(
            SpecialRegionSiteBridge bridge,
            VillageFacilityDefinition facility,
            VillageFacilityAccessWitness witness,
            ISet<LocalTileCoord> road,
            ISet<LocalTileCoord> fixedCollision,
            ISet<LocalTileCoord> facilityCells,
            string path,
            ICollection<VillageShellError> errors)
        {
            var cells = witness.Cells;
            var projected = new List<SpecialRegionPlacedCoordinate>();
            if (cells.Count < 2)
                Add(errors, VillageShellErrorCode.InvalidAccessWitness, path + "/access", "Witness must contain door then road, with zero or more path cells.");
            if (cells.Count > 0 && cells[0] != facility.DoorRegionTile)
                Add(errors, VillageShellErrorCode.InvalidAccessWitness, path + "/access/door", "Witness must begin at the explicit door.");

            var seen = new HashSet<LocalTileCoord>();
            for (var index = 0; index < cells.Count; index++)
            {
                var cell = cells[index];
                if (!seen.Add(cell))
                    Add(errors, VillageShellErrorCode.InvalidAccessWitness, path + "/access/" + index, "Witness cannot duplicate or backtrack through a cell.");
                if (index > 0 && !IsCardinal(cells[index - 1], cell))
                    Add(errors, VillageShellErrorCode.InvalidAccessWitness, path + "/access/" + index, "Witness cells must be cardinal-adjacent.");
                if (!TryProject(bridge, cell, out var placed))
                    Add(errors, VillageShellErrorCode.CoordinateOutOfRange, path + "/access/" + index, "Witness cell is outside the placed region.");
                else projected.Add(placed);
                if (index < cells.Count - 1 && (fixedCollision.Contains(cell) || facilityCells.Contains(cell)))
                    Add(errors, VillageShellErrorCode.InvalidAccessWitness, path + "/access/" + index, "Door/path cannot overlap fixed collision or a Facility slot.");
            }

            if (cells.Count == 0 || !road.Contains(cells[cells.Count - 1]))
                Add(errors, VillageShellErrorCode.FacilityCannotReturnToRoad, path + "/access/road", "Witness must terminate on an exact central-road cell.");
            return projected;
        }

        private static bool TryProject(
            SpecialRegionSiteBridge bridge,
            LocalTileCoord regionTile,
            out SpecialRegionPlacedCoordinate placed)
        {
            placed = default(SpecialRegionPlacedCoordinate);
            var maxX = (long)bridge.Width * WorldGenConstants.SectorWidthTiles;
            var maxY = (long)bridge.Height * WorldGenConstants.SectorHeightTiles;
            if (regionTile.X < 0 || regionTile.Y < 0 || regionTile.X >= maxX || regionTile.Y >= maxY) return false;
            var sectorX = regionTile.X / WorldGenConstants.SectorWidthTiles;
            var sectorY = regionTile.Y / WorldGenConstants.SectorHeightTiles;
            var offset = new SpecialRegionSectorOffset(sectorX, sectorY);
            var local = new LocalTileCoord(
                regionTile.X % WorldGenConstants.SectorWidthTiles,
                regionTile.Y % WorldGenConstants.SectorHeightTiles);
            var worldX = (long)bridge.Origin.X + sectorX;
            var worldY = (long)bridge.Origin.Y + sectorY;
            if (worldX < int.MinValue || worldX > int.MaxValue || worldY < int.MinValue || worldY > int.MaxValue) return false;
            var world = new SectorCoord((int)worldX, (int)worldY);
            if (!bridge.SectorBindings.Any(value => value.PlacedOffset == offset && value.WorldSector == world)) return false;
            placed = new SpecialRegionPlacedCoordinate(offset, world, local, regionTile);
            return TryUnproject(bridge, new SpecialRegionTileCoordinate(world, local), out var roundTrip) && roundTrip == regionTile;
        }

        private static bool TryUnproject(
            SpecialRegionSiteBridge bridge,
            SpecialRegionTileCoordinate coordinate,
            out LocalTileCoord regionTile)
        {
            regionTile = default(LocalTileCoord);
            var sectorX = (long)coordinate.WorldSector.X - bridge.Origin.X;
            var sectorY = (long)coordinate.WorldSector.Y - bridge.Origin.Y;
            if (sectorX < 0 || sectorY < 0 || sectorX >= bridge.Width || sectorY >= bridge.Height ||
                coordinate.LocalTile.X < 0 || coordinate.LocalTile.X >= WorldGenConstants.SectorWidthTiles ||
                coordinate.LocalTile.Y < 0 || coordinate.LocalTile.Y >= WorldGenConstants.SectorHeightTiles) return false;
            var x = sectorX * WorldGenConstants.SectorWidthTiles + coordinate.LocalTile.X;
            var y = sectorY * WorldGenConstants.SectorHeightTiles + coordinate.LocalTile.Y;
            if (x > int.MaxValue || y > int.MaxValue) return false;
            regionTile = new LocalTileCoord((int)x, (int)y);
            return true;
        }

        private static bool IsCardinal(LocalTileCoord left, LocalTileCoord right)
            => Math.Abs((long)left.X - right.X) + Math.Abs((long)left.Y - right.Y) == 1L;

        private static bool IsStableToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal)) return false;
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (!(character >= 'A' && character <= 'Z') && !(character >= 'a' && character <= 'z') &&
                    !(character >= '0' && character <= '9') && character != '_' && character != '-' && character != '.') return false;
            }
            return true;
        }

        private static string Tile(LocalTileCoord value) => value.X + "," + value.Y;
        private static VillageShellResult Failure(IEnumerable<VillageShellError> errors)
            => new VillageShellResult(null, errors);
        private static void Add(
            ICollection<VillageShellError> errors,
            VillageShellErrorCode code,
            string path,
            string detail)
            => errors.Add(new VillageShellError(code, path, detail));
    }
}
