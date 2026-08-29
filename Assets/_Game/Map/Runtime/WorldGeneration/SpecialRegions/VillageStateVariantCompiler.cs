using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Map.WorldGeneration.Pipeline;

namespace StarNight.Map.WorldGeneration.SpecialRegions
{
    public static class VillageStateVariantCompiler
    {
        public static VillageStateVariantResult Compile(VillageStateVariantCompileRequest request)
        {
            var errors = new List<VillageStateVariantError>();
            if (request == null)
            {
                Add(errors, VillageStateVariantErrorCode.MissingInput, "request", "Compile request is required.");
                return Failure(errors);
            }

            var shell = request.VillageShellPlan;
            var definition = request.MarkerSetDefinition;
            if (shell == null) Add(errors, VillageStateVariantErrorCode.MissingInput, "villageShell", "Village shell plan is required.");
            if (definition == null) Add(errors, VillageStateVariantErrorCode.MissingInput, "markerSet", "Marker-set definition is required.");
            if (request.SourceRegionKind != SpecialRegionKind.Village)
                Add(errors, VillageStateVariantErrorCode.NotVillage, "sourceRegionKind", "Source region kind must be Village.");
            if (shell == null || definition == null) return Failure(errors);

            ValidateShell(request, errors);
            ValidateVariants(definition.RequestedVariants, errors);

            var facilities = shell.FacilityBindings
                .Where(value => value != null && value.Definition != null)
                .GroupBy(value => value.Definition.DefinitionId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            if (facilities.Count != shell.FacilityBindings.Count)
                Add(errors, VillageStateVariantErrorCode.ShellInvariantViolation,
                    "villageShell.facilities", "Facility binding identities must be non-null and unique.");

            var npc = ValidateNpcMarkers(definition, facilities, errors);
            var inventory = ValidateInventoryMarkers(definition, facilities, errors);
            var doors = ValidateDoorMarkers(definition, facilities, errors);
            ValidateIndividualTarget(definition, npc, errors);

            if (errors.Count != 0) return Failure(errors);

            var roadWitnessDigest = VillageStateVariantCanonicalDigest.ComputeRoadWitness(shell);
            var facilityCoordinateDigest = VillageStateVariantCanonicalDigest.ComputeFacilityCoordinates(shell);
            var facilityWitnessDigest = VillageStateVariantCanonicalDigest.ComputeFacilityWitnesses(shell);
            var snapshots = new List<VillageStateVariantSnapshot>();
            foreach (var state in VillageStateMarkerSetDefinition.CanonicalVariants())
            {
                var npcSnapshots = npc.Select(marker => new VillageNpcMarkerSnapshot(
                    marker, facilities[marker.FacilityBindingId].Slot.Placed,
                    NpcState(state, marker.MarkerId, definition.IndividualHostileTargetMarkerId)));
                var inventorySnapshots = inventory.Select(marker => new VillageInventoryMarkerSnapshot(
                    marker, facilities[marker.FacilityBindingId].Slot.Placed, InventoryState(state)));
                var doorSnapshots = doors.Select(marker => new VillageDoorMarkerSnapshot(
                    marker, facilities[marker.FacilityBindingId].Door, DoorState(state)));
                snapshots.Add(new VillageStateVariantSnapshot(
                    state, shell, roadWitnessDigest, facilityCoordinateDigest, facilityWitnessDigest,
                    definition.IndividualHostileTargetMarkerId,
                    npcSnapshots, inventorySnapshots, doorSnapshots));
            }

            ValidatePublishedMatrix(snapshots, definition, shell, errors);
            if (errors.Count != 0) return Failure(errors);

            var variantSet = new VillageStateVariantSet(
                shell, definition.IndividualHostileTargetMarkerId, snapshots);
            if (variantSet.Variants.Count != 5 || string.IsNullOrEmpty(variantSet.CanonicalDigest) ||
                !string.Equals(variantSet.CanonicalDigest,
                    VillageStateVariantCanonicalDigest.Compute(variantSet), StringComparison.Ordinal))
                Add(errors, VillageStateVariantErrorCode.NonCanonicalPublication,
                    "variantSet", "Variant set must publish exactly five canonical snapshots and a stable digest.");
            if (errors.Count != 0) return Failure(errors);
            return new VillageStateVariantResult(variantSet, Array.Empty<VillageStateVariantError>());
        }

        private static void ValidateShell(
            VillageStateVariantCompileRequest request,
            ICollection<VillageStateVariantError> errors)
        {
            var shell = request.VillageShellPlan;
            string canonical;
            string road;
            string facilities;
            string access;
            try
            {
                canonical = VillageShellCanonicalDigest.Compute(shell);
                road = VillageShellCanonicalDigest.ComputeRoad(shell.RoadCells);
                facilities = VillageShellCanonicalDigest.ComputeFacilities(shell.FacilityBindings);
                access = VillageShellCanonicalDigest.ComputeAccess(shell.FacilityBindings);
            }
            catch (Exception exception)
            {
                Add(errors, VillageStateVariantErrorCode.ShellInvariantViolation,
                    "villageShell", "Shell digests could not be recomputed: " + exception.GetType().Name + ".");
                return;
            }

            if (string.IsNullOrEmpty(request.ExpectedVillageShellDigest) ||
                !string.Equals(request.ExpectedVillageShellDigest, shell.CanonicalDigest, StringComparison.Ordinal) ||
                !string.Equals(canonical, shell.CanonicalDigest, StringComparison.Ordinal))
                Add(errors, VillageStateVariantErrorCode.DigestMismatch,
                    "villageShell.digest", "Expected, published, and recomputed Village shell digests must match.");
            if (!string.Equals(road, shell.RoadDigest, StringComparison.Ordinal) ||
                !string.Equals(facilities, shell.FacilityDigest, StringComparison.Ordinal) ||
                !string.Equals(access, shell.AccessDigest, StringComparison.Ordinal))
                Add(errors, VillageStateVariantErrorCode.ShellInvariantViolation,
                    "villageShell.componentDigests", "Road, Facility, and access component digests must be unchanged.");

            if (shell.RoadAccess == null || shell.RoadAccess.AccessClass != AccessClass.MandatoryNoTool ||
                shell.RoadAccess.Forward.Count != shell.RoadCells.Count ||
                shell.RoadAccess.Reverse.Count != shell.RoadCells.Count ||
                !SameRoad(shell.RoadAccess.Forward, shell.RoadCells) ||
                !SameRoad(shell.RoadAccess.Reverse, shell.RoadCells.Reverse()))
                Add(errors, VillageStateVariantErrorCode.ShellInvariantViolation,
                    "villageShell.roadWitness", "Ordered Entry/Return road witness identity must remain unchanged and bidirectional.");

            var roadCells = new HashSet<Domain.LocalTileCoord>(shell.RoadCells.Select(value => value.RegionTile));
            foreach (var binding in shell.FacilityBindings.Where(value => value != null))
            {
                var path = "villageShell.facilities/" +
                    (binding.Definition == null ? string.Empty : binding.Definition.DefinitionId);
                if (binding.Definition == null || binding.Slot == null || binding.Witness == null ||
                    binding.AccessClass != AccessClass.MandatoryNoTool || binding.AccessCells.Count < 2 ||
                    binding.ReverseAccessCells.Count != binding.AccessCells.Count ||
                    binding.Door != binding.AccessCells[0] ||
                    !roadCells.Contains(binding.AccessCells[binding.AccessCells.Count - 1].RegionTile) ||
                    !binding.ReverseAccessCells.SequenceEqual(binding.AccessCells.Reverse()))
                    Add(errors, VillageStateVariantErrorCode.ShellInvariantViolation,
                        path, "Facility slot/door/road-return witness identity must remain unchanged.");
            }
        }

        private static bool SameRoad(
            IEnumerable<VillageRoadCell> left,
            IEnumerable<VillageRoadCell> right)
            => left.Select(value => value.Order + "/" + value.RegionTile.X + "," + value.RegionTile.Y)
                .SequenceEqual(right.Select(value => value.Order + "/" + value.RegionTile.X + "," + value.RegionTile.Y));

        private static void ValidateVariants(
            IEnumerable<VillageStateKind> requested,
            ICollection<VillageStateVariantError> errors)
        {
            var values = (requested ?? Array.Empty<VillageStateKind>()).ToArray();
            foreach (var duplicate in values.GroupBy(value => value).Where(group => group.Count() > 1))
                Add(errors, VillageStateVariantErrorCode.DuplicateVariant,
                    "markerSet.variants/" + (int)duplicate.Key, "Variant kinds must be unique.");
            foreach (var expected in VillageStateMarkerSetDefinition.CanonicalVariants())
                if (values.Count(value => value == expected) != 1)
                    Add(errors, VillageStateVariantErrorCode.MissingVariant,
                        "markerSet.variants/" + expected, "Every canonical Village state is required exactly once.");
            foreach (var value in values.Where(value => !VillageStateMarkerSetDefinition.CanonicalVariants().Contains(value)))
                Add(errors, VillageStateVariantErrorCode.NonCanonicalPublication,
                    "markerSet.variants/" + (int)value, "Unsupported Village state kind.");
        }

        private static VillageNpcMarkerDefinition[] ValidateNpcMarkers(
            VillageStateMarkerSetDefinition definition,
            IReadOnlyDictionary<string, VillageFacilityBinding> facilities,
            ICollection<VillageStateVariantError> errors)
        {
            var supplied = definition.NpcMarkers.ToArray();
            if (supplied.Length == 0)
                Add(errors, VillageStateVariantErrorCode.MissingMarkerKind,
                    "markerSet.npc", "NPC marker collection must be non-empty.");
            if (supplied.Length < 2)
                Add(errors, VillageStateVariantErrorCode.InsufficientNpcMarkers,
                    "markerSet.npc", "At least two NPC markers are required.");
            if (supplied.Any(value => value == null))
                Add(errors, VillageStateVariantErrorCode.MissingInput,
                    "markerSet.npc", "NPC marker collection cannot contain null.");
            var values = supplied.Where(value => value != null).ToArray();
            ValidateMarkerIdentity(values.Select(value => Tuple.Create(value.MarkerId, value.FacilityBindingId)),
                "markerSet.npc", facilities, errors);
            return values.OrderBy(value => value.MarkerId, StringComparer.Ordinal).ToArray();
        }

        private static VillageInventoryMarkerDefinition[] ValidateInventoryMarkers(
            VillageStateMarkerSetDefinition definition,
            IReadOnlyDictionary<string, VillageFacilityBinding> facilities,
            ICollection<VillageStateVariantError> errors)
        {
            var supplied = definition.InventoryMarkers.ToArray();
            if (supplied.Length == 0)
                Add(errors, VillageStateVariantErrorCode.MissingMarkerKind,
                    "markerSet.inventory", "Inventory marker collection must be non-empty.");
            if (supplied.Any(value => value == null))
                Add(errors, VillageStateVariantErrorCode.MissingInput,
                    "markerSet.inventory", "Inventory marker collection cannot contain null.");
            var values = supplied.Where(value => value != null).ToArray();
            ValidateMarkerIdentity(values.Select(value => Tuple.Create(value.MarkerId, value.FacilityBindingId)),
                "markerSet.inventory", facilities, errors);
            return values.OrderBy(value => value.MarkerId, StringComparer.Ordinal).ToArray();
        }

        private static VillageDoorMarkerDefinition[] ValidateDoorMarkers(
            VillageStateMarkerSetDefinition definition,
            IReadOnlyDictionary<string, VillageFacilityBinding> facilities,
            ICollection<VillageStateVariantError> errors)
        {
            var supplied = definition.DoorMarkers.ToArray();
            if (supplied.Length == 0)
                Add(errors, VillageStateVariantErrorCode.MissingMarkerKind,
                    "markerSet.doors", "Door marker collection must be non-empty.");
            if (supplied.Any(value => value == null))
                Add(errors, VillageStateVariantErrorCode.MissingInput,
                    "markerSet.doors", "Door marker collection cannot contain null.");
            var values = supplied.Where(value => value != null).ToArray();
            ValidateMarkerIdentity(values.Select(value => Tuple.Create(value.MarkerId, value.FacilityBindingId)),
                "markerSet.doors", facilities, errors);
            foreach (var group in values.GroupBy(value => value.FacilityBindingId, StringComparer.Ordinal))
                if (group.Count() != 1)
                    Add(errors, VillageStateVariantErrorCode.DoorBindingMismatch,
                        "markerSet.doors/" + group.Key, "Each Facility binding must have exactly one door marker.");
            foreach (var facility in facilities)
                if (values.Count(value => string.Equals(value.FacilityBindingId, facility.Key, StringComparison.Ordinal)) != 1)
                    Add(errors, VillageStateVariantErrorCode.DoorBindingMismatch,
                        "markerSet.doors/" + facility.Key, "Every Facility binding must have exactly one door marker.");
            foreach (var marker in values)
                if (facilities.TryGetValue(marker.FacilityBindingId, out var binding) &&
                    marker.SourceDoorRegionTile != binding.Door.RegionTile)
                    Add(errors, VillageStateVariantErrorCode.DoorBindingMismatch,
                        "markerSet.doors/" + marker.MarkerId, "Door marker must reference the exact MAP13_04 door coordinate.");
            return values.OrderBy(value => value.MarkerId, StringComparer.Ordinal).ToArray();
        }

        private static void ValidateMarkerIdentity(
            IEnumerable<Tuple<string, string>> markers,
            string path,
            IReadOnlyDictionary<string, VillageFacilityBinding> facilities,
            ICollection<VillageStateVariantError> errors)
        {
            var values = markers.ToArray();
            foreach (var marker in values)
            {
                if (!IsStableToken(marker.Item1) || !IsStableToken(marker.Item2))
                    Add(errors, VillageStateVariantErrorCode.NonCanonicalPublication,
                        path + "/" + marker.Item1, "Marker and Facility binding IDs must be explicit stable tokens.");
                if (!facilities.ContainsKey(marker.Item2))
                    Add(errors, VillageStateVariantErrorCode.UnknownFacilityBinding,
                        path + "/" + marker.Item1, "Marker must reference an exact MAP13_04 Facility binding.");
            }
            foreach (var duplicate in values.GroupBy(value => value.Item1, StringComparer.Ordinal).Where(group => group.Count() > 1))
                Add(errors, VillageStateVariantErrorCode.DuplicateMarker,
                    path + "/" + duplicate.Key, "Marker IDs must be unique within their marker kind.");
        }

        private static void ValidateIndividualTarget(
            VillageStateMarkerSetDefinition definition,
            IEnumerable<VillageNpcMarkerDefinition> npc,
            ICollection<VillageStateVariantError> errors)
        {
            if (string.IsNullOrEmpty(definition.IndividualHostileTargetMarkerId))
            {
                Add(errors, VillageStateVariantErrorCode.MissingIndividualTarget,
                    "markerSet.individualTarget", "IndividualHostile target marker ID is required.");
                return;
            }
            if (npc.Count(value => string.Equals(value.MarkerId,
                    definition.IndividualHostileTargetMarkerId, StringComparison.Ordinal)) != 1)
                Add(errors, VillageStateVariantErrorCode.UnknownIndividualTarget,
                    "markerSet.individualTarget", "IndividualHostile target must reference exactly one existing NPC marker.");
        }

        private static void ValidatePublishedMatrix(
            IReadOnlyList<VillageStateVariantSnapshot> snapshots,
            VillageStateMarkerSetDefinition definition,
            VillageShellPlan shell,
            ICollection<VillageStateVariantError> errors)
        {
            if (snapshots.Count != 5 || snapshots.Select(value => value.StateKind).Distinct().Count() != 5)
                Add(errors, VillageStateVariantErrorCode.VariantMatrixMismatch,
                    "variantSet.matrix", "Exactly five unique canonical variants must be published.");
            foreach (var snapshot in snapshots)
            {
                if (snapshot.NpcMarkers.Count != definition.NpcMarkers.Count ||
                    snapshot.InventoryMarkers.Count != definition.InventoryMarkers.Count ||
                    snapshot.DoorMarkers.Count != definition.DoorMarkers.Count)
                    Add(errors, VillageStateVariantErrorCode.VariantMatrixMismatch,
                        "variantSet/" + snapshot.StateKind + "/counts", "Marker counts must remain unchanged.");

                foreach (var marker in snapshot.NpcMarkers)
                    if (marker.State != NpcState(snapshot.StateKind, marker.MarkerId,
                            definition.IndividualHostileTargetMarkerId))
                        Add(errors, VillageStateVariantErrorCode.VariantMatrixMismatch,
                            "variantSet/" + snapshot.StateKind + "/npc/" + marker.MarkerId,
                            "NPC state does not match the exact five-state matrix.");
                if (snapshot.InventoryMarkers.Any(value => value.State != InventoryState(snapshot.StateKind)) ||
                    snapshot.DoorMarkers.Any(value => value.State != DoorState(snapshot.StateKind)))
                    Add(errors, VillageStateVariantErrorCode.VariantMatrixMismatch,
                        "variantSet/" + snapshot.StateKind + "/markers", "Inventory or door state does not match the exact matrix.");

                if (!string.Equals(snapshot.VillageShellDigest, shell.CanonicalDigest, StringComparison.Ordinal) ||
                    !string.Equals(snapshot.RoadDigest, shell.RoadDigest, StringComparison.Ordinal) ||
                    !string.Equals(snapshot.FacilityDigest, shell.FacilityDigest, StringComparison.Ordinal) ||
                    !string.Equals(snapshot.AccessDigest, shell.AccessDigest, StringComparison.Ordinal) ||
                    snapshot.RoadCellCount != shell.RoadCells.Count ||
                    snapshot.FacilityBindingCount != shell.FacilityBindings.Count)
                    Add(errors, VillageStateVariantErrorCode.ShellInvariantViolation,
                        "variantSet/" + snapshot.StateKind + "/shell", "Every snapshot must preserve all MAP13_04 shell identities.");
            }

            var first = snapshots.FirstOrDefault();
            if (first == null) return;
            foreach (var snapshot in snapshots.Skip(1))
                if (!string.Equals(snapshot.RoadWitnessDigest, first.RoadWitnessDigest, StringComparison.Ordinal) ||
                    !string.Equals(snapshot.FacilityCoordinateDigest, first.FacilityCoordinateDigest, StringComparison.Ordinal) ||
                    !string.Equals(snapshot.FacilityWitnessDigest, first.FacilityWitnessDigest, StringComparison.Ordinal))
                    Add(errors, VillageStateVariantErrorCode.ShellInvariantViolation,
                        "variantSet/" + snapshot.StateKind + "/witnesses", "All ordered snapshot pairs must preserve coordinate and witness identities.");
        }

        private static VillageNpcMarkerState NpcState(
            VillageStateKind state,
            string markerId,
            string targetId)
        {
            switch (state)
            {
                case VillageStateKind.Normal: return VillageNpcMarkerState.Normal;
                case VillageStateKind.Friendly: return VillageNpcMarkerState.Friendly;
                case VillageStateKind.IndividualHostile:
                    return string.Equals(markerId, targetId, StringComparison.Ordinal)
                        ? VillageNpcMarkerState.Hostile : VillageNpcMarkerState.Normal;
                case VillageStateKind.AllHostile: return VillageNpcMarkerState.Hostile;
                case VillageStateKind.Evacuation: return VillageNpcMarkerState.Evacuated;
                default: throw new ArgumentOutOfRangeException(nameof(state));
            }
        }

        private static VillageInventoryMarkerState InventoryState(VillageStateKind state)
        {
            switch (state)
            {
                case VillageStateKind.Normal:
                case VillageStateKind.IndividualHostile: return VillageInventoryMarkerState.Standard;
                case VillageStateKind.Friendly: return VillageInventoryMarkerState.FriendlyAccess;
                case VillageStateKind.AllHostile: return VillageInventoryMarkerState.Unavailable;
                case VillageStateKind.Evacuation: return VillageInventoryMarkerState.Evacuated;
                default: throw new ArgumentOutOfRangeException(nameof(state));
            }
        }

        private static VillageDoorMarkerState DoorState(VillageStateKind state)
        {
            switch (state)
            {
                case VillageStateKind.Normal:
                case VillageStateKind.IndividualHostile: return VillageDoorMarkerState.Standard;
                case VillageStateKind.Friendly: return VillageDoorMarkerState.Welcome;
                case VillageStateKind.AllHostile: return VillageDoorMarkerState.Alert;
                case VillageStateKind.Evacuation: return VillageDoorMarkerState.Evacuated;
                default: throw new ArgumentOutOfRangeException(nameof(state));
            }
        }

        private static bool IsStableToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal)) return false;
            foreach (var character in value)
                if (!(character >= 'A' && character <= 'Z') && !(character >= 'a' && character <= 'z') &&
                    !(character >= '0' && character <= '9') && character != '_' && character != '-' && character != '.') return false;
            return true;
        }

        private static VillageStateVariantResult Failure(IEnumerable<VillageStateVariantError> errors)
            => new VillageStateVariantResult(null, errors);
        private static void Add(
            ICollection<VillageStateVariantError> errors,
            VillageStateVariantErrorCode code,
            string path,
            string detail)
            => errors.Add(new VillageStateVariantError(code, path, detail));
    }
}
