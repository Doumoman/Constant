using System;
using System.Collections.Generic;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public static class MicrochunkObjectSlotValidator
    {
        public const string EmptySlotIdReason = "EMPTY_SLOT_ID";
        public const string DuplicateSlotIdReason = "DUPLICATE_SLOT_ID";
        public const string SlotAnchorOutOfBoundsReason = "SLOT_ANCHOR_OUT_OF_BOUNDS";
        public const string MissingTileCellForSlotAnchorReason = "MISSING_TILE_CELL_FOR_SLOT_ANCHOR";
        public const string UndefinedSlotCategoryReason = "UNDEFINED_SLOT_CATEGORY";
        public const string EmptyAllowedPoolIdReason = "EMPTY_ALLOWED_POOL_ID";
        public const string AllowedPoolIdNotFoundReason = "ALLOWED_POOL_ID_NOT_FOUND";
        public const string SlotCategoryNotAllowedByPoolReason = "SLOT_CATEGORY_NOT_ALLOWED_BY_POOL";
        public const string PoolDisallowsRequiredSlotReason = "POOL_DISALLOWS_REQUIRED_SLOT";
        public const string PoolDisallowsOptionalSlotReason = "POOL_DISALLOWS_OPTIONAL_SLOT";
        public const string UndefinedSlotOrientationReason = "UNDEFINED_SLOT_ORIENTATION";
        public const string NegativeForbiddenRadiusReason = "NEGATIVE_FORBIDDEN_RADIUS";
        public const string RequiredMarkerCodeNotAllowedReason = "REQUIRED_MARKER_CODE_NOT_ALLOWED";
        public const string RequiredMarkerMismatchReason = "REQUIRED_MARKER_MISMATCH";
        public const string BlockingTileCellAtSlotAnchorReason = "BLOCKING_TILE_CELL_AT_SLOT_ANCHOR";
        public const string MissingTileCellInSlotSafetyRadiusReason = "MISSING_TILE_CELL_IN_SLOT_SAFETY_RADIUS";
        public const string BlockingTileCellInSlotSafetyRadiusReason = "BLOCKING_TILE_CELL_IN_SLOT_SAFETY_RADIUS";
        public const string DuplicateSlotAnchorReason = "DUPLICATE_SLOT_ANCHOR";
        public const string SlotAnchorWithinForbiddenRadiusReason = "SLOT_ANCHOR_WITHIN_FORBIDDEN_RADIUS";

        public static MicrochunkObjectSlotValidationResult ValidateDefinition(
            MicrochunkDefinition definition,
            MicrochunkObjectSlotValidationPolicy policy)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            return ValidateSlots(
                definition.Id,
                definition.TileDataComplete,
                definition.TileCells,
                definition.ObjectSlots,
                policy);
        }

        public static MicrochunkObjectSlotValidationResult ValidateSlots(
            MicrochunkId microchunkId,
            bool tileDataComplete,
            IEnumerable<MicrochunkTileCell> tileCells,
            IEnumerable<MicrochunkObjectSlotDefinition> objectSlots,
            MicrochunkObjectSlotValidationPolicy policy)
        {
            if (!microchunkId.IsValid) throw new ArgumentException("A valid microchunk ID is required.", nameof(microchunkId));
            if (tileCells == null) throw new ArgumentNullException(nameof(tileCells));
            if (objectSlots == null) throw new ArgumentNullException(nameof(objectSlots));
            if (policy == null) throw new ArgumentNullException(nameof(policy));

            var cells = FreezeCells(tileCells);
            var slots = FreezeSlots(objectSlots);
            var violations = new List<MicrochunkObjectSlotValidationViolation>();

            ValidateDuplicateIds(microchunkId, slots, violations);
            ValidateDuplicateAnchors(microchunkId, slots, violations);
            ValidatePairSpacing(microchunkId, slots, violations);

            foreach (var slot in slots)
            {
                ValidateSlotMetadata(microchunkId, slot, policy, violations);
                ValidateSlotTiles(microchunkId, slot, tileDataComplete, cells, policy, violations);
            }

            return new MicrochunkObjectSlotValidationResult(slots.Count, violations);
        }

        private static Dictionary<MicrochunkLocalCoord, MicrochunkTileCell> FreezeCells(
            IEnumerable<MicrochunkTileCell> source)
        {
            var values = new Dictionary<MicrochunkLocalCoord, MicrochunkTileCell>();
            foreach (var cell in source)
            {
                if (cell == null) throw new ArgumentException("Tile cells cannot contain null.", nameof(source));
                if (values.ContainsKey(cell.Coordinate))
                {
                    throw new ArgumentException("Tile cell coordinates must be unique.", nameof(source));
                }
                values.Add(cell.Coordinate, cell);
            }
            return values;
        }

        private static List<MicrochunkObjectSlotDefinition> FreezeSlots(
            IEnumerable<MicrochunkObjectSlotDefinition> source)
        {
            var values = new List<MicrochunkObjectSlotDefinition>();
            foreach (var slot in source)
            {
                if (slot == null) throw new ArgumentException("Object slots cannot contain null.", nameof(source));
                values.Add(slot);
            }
            values.Sort(CompareSlots);
            return values;
        }

        private static int CompareSlots(
            MicrochunkObjectSlotDefinition left,
            MicrochunkObjectSlotDefinition right)
        {
            var comparison = string.Compare(left.SlotId, right.SlotId, StringComparison.Ordinal);
            if (comparison != 0) return comparison;
            comparison = left.Anchor.CompareTo(right.Anchor);
            if (comparison != 0) return comparison;
            comparison = left.Category.CompareTo(right.Category);
            if (comparison != 0) return comparison;
            return string.Compare(left.AllowedPoolId, right.AllowedPoolId, StringComparison.Ordinal);
        }

        private static void ValidateDuplicateIds(
            MicrochunkId microchunkId,
            IReadOnlyList<MicrochunkObjectSlotDefinition> slots,
            ICollection<MicrochunkObjectSlotValidationViolation> violations)
        {
            foreach (var group in slots.GroupBy(slot => slot.SlotId, StringComparer.Ordinal))
            {
                if (group.Count() < 2) continue;
                var ordered = group.OrderBy(slot => slot.Anchor.RowMajorIndex).ToArray();
                for (var leftIndex = 0; leftIndex < ordered.Length - 1; leftIndex++)
                for (var rightIndex = leftIndex + 1; rightIndex < ordered.Length; rightIndex++)
                {
                    Add(violations, microchunkId, ordered[leftIndex], DuplicateSlotIdReason,
                        ordered[rightIndex].Anchor, ordered[rightIndex].SlotId);
                }
            }
        }

        private static void ValidateDuplicateAnchors(
            MicrochunkId microchunkId,
            IReadOnlyList<MicrochunkObjectSlotDefinition> slots,
            ICollection<MicrochunkObjectSlotValidationViolation> violations)
        {
            foreach (var group in slots.GroupBy(slot => slot.Anchor))
            {
                var ordered = group.OrderBy(slot => slot, Comparer<MicrochunkObjectSlotDefinition>.Create(CompareSlots)).ToArray();
                for (var leftIndex = 0; leftIndex < ordered.Length - 1; leftIndex++)
                for (var rightIndex = leftIndex + 1; rightIndex < ordered.Length; rightIndex++)
                {
                    Add(violations, microchunkId, ordered[leftIndex], DuplicateSlotAnchorReason,
                        ordered[leftIndex].Anchor, ordered[rightIndex].SlotId);
                }
            }
        }

        private static void ValidatePairSpacing(
            MicrochunkId microchunkId,
            IReadOnlyList<MicrochunkObjectSlotDefinition> slots,
            ICollection<MicrochunkObjectSlotValidationViolation> violations)
        {
            for (var leftIndex = 0; leftIndex < slots.Count - 1; leftIndex++)
            for (var rightIndex = leftIndex + 1; rightIndex < slots.Count; rightIndex++)
            {
                var left = slots[leftIndex];
                var right = slots[rightIndex];
                if (left.Anchor == right.Anchor) continue;
                var distance = Math.Abs(left.Anchor.X - right.Anchor.X) + Math.Abs(left.Anchor.Y - right.Anchor.Y);
                if (distance <= left.ForbiddenRadiusTiles || distance <= right.ForbiddenRadiusTiles)
                {
                    Add(violations, microchunkId, left, SlotAnchorWithinForbiddenRadiusReason,
                        right.Anchor, right.SlotId);
                }
            }
        }

        private static void ValidateSlotMetadata(
            MicrochunkId microchunkId,
            MicrochunkObjectSlotDefinition slot,
            MicrochunkObjectSlotValidationPolicy policy,
            ICollection<MicrochunkObjectSlotValidationViolation> violations)
        {
            if (string.IsNullOrWhiteSpace(slot.SlotId))
            {
                Add(violations, microchunkId, slot, EmptySlotIdReason, null, string.Empty);
            }
            if (!Enum.IsDefined(typeof(MicrochunkSlotCategory), slot.Category))
            {
                Add(violations, microchunkId, slot, UndefinedSlotCategoryReason, null, string.Empty);
            }

            if (string.IsNullOrWhiteSpace(slot.AllowedPoolId))
            {
                Add(violations, microchunkId, slot, EmptyAllowedPoolIdReason, null, string.Empty);
            }
            else if (!policy.TryGetPool(slot.AllowedPoolId, out var pool))
            {
                Add(violations, microchunkId, slot, AllowedPoolIdNotFoundReason, null, string.Empty);
            }
            else
            {
                if (!pool.AllowsCategory(slot.Category))
                {
                    Add(violations, microchunkId, slot, SlotCategoryNotAllowedByPoolReason, null, string.Empty);
                }
                if (slot.Required && !pool.RequiredSlotsAllowed)
                {
                    Add(violations, microchunkId, slot, PoolDisallowsRequiredSlotReason, null, string.Empty);
                }
                if (!slot.Required && !pool.OptionalSlotsAllowed)
                {
                    Add(violations, microchunkId, slot, PoolDisallowsOptionalSlotReason, null, string.Empty);
                }
            }

            if (!Enum.IsDefined(typeof(MicrochunkObjectOrientation), slot.Orientation))
            {
                Add(violations, microchunkId, slot, UndefinedSlotOrientationReason, null, string.Empty);
            }
            if (slot.ForbiddenRadiusTiles < 0)
            {
                Add(violations, microchunkId, slot, NegativeForbiddenRadiusReason, null, string.Empty);
            }
            if (!string.IsNullOrEmpty(slot.RequiredMarkerCode) &&
                !policy.IsAllowedMarkerCode(slot.RequiredMarkerCode))
            {
                Add(violations, microchunkId, slot, RequiredMarkerCodeNotAllowedReason, slot.Anchor, string.Empty);
            }
        }

        private static void ValidateSlotTiles(
            MicrochunkId microchunkId,
            MicrochunkObjectSlotDefinition slot,
            bool tileDataComplete,
            IReadOnlyDictionary<MicrochunkLocalCoord, MicrochunkTileCell> cells,
            MicrochunkObjectSlotValidationPolicy policy,
            ICollection<MicrochunkObjectSlotValidationViolation> violations)
        {
            if (!MicrochunkLocalCoord.TryCreate(slot.Anchor.X, slot.Anchor.Y, out var anchor))
            {
                Add(violations, microchunkId, slot, SlotAnchorOutOfBoundsReason, null, string.Empty);
                return;
            }
            if (!cells.TryGetValue(anchor, out var anchorCell))
            {
                Add(violations, microchunkId, slot, MissingTileCellForSlotAnchorReason, anchor, string.Empty);
                return;
            }

            if (policy.IsBlocking(anchorCell))
            {
                Add(violations, microchunkId, slot, BlockingTileCellAtSlotAnchorReason, anchor, string.Empty);
            }
            if (!string.IsNullOrEmpty(slot.RequiredMarkerCode) &&
                !string.Equals(anchorCell.MarkerCode, slot.RequiredMarkerCode, StringComparison.Ordinal))
            {
                Add(violations, microchunkId, slot, RequiredMarkerMismatchReason, anchor, string.Empty);
            }

            for (var y = 0; y < MicrochunkConstants.HeightTiles; y++)
            for (var x = 0; x < MicrochunkConstants.WidthTiles; x++)
            {
                var distance = Math.Abs(anchor.X - x) + Math.Abs(anchor.Y - y);
                if (distance == 0 || distance > slot.ForbiddenRadiusTiles) continue;
                var coordinate = new MicrochunkLocalCoord(x, y);
                if (!cells.TryGetValue(coordinate, out var cell))
                {
                    if (tileDataComplete)
                    {
                        Add(violations, microchunkId, slot,
                            MissingTileCellInSlotSafetyRadiusReason, coordinate, string.Empty);
                    }
                }
                else if (policy.IsBlocking(cell))
                {
                    Add(violations, microchunkId, slot,
                        BlockingTileCellInSlotSafetyRadiusReason, coordinate, string.Empty);
                }
            }
        }

        private static void Add(
            ICollection<MicrochunkObjectSlotValidationViolation> violations,
            MicrochunkId microchunkId,
            MicrochunkObjectSlotDefinition slot,
            string reason,
            MicrochunkLocalCoord? coordinate,
            string comparedSlotId)
        {
            violations.Add(new MicrochunkObjectSlotValidationViolation(
                microchunkId,
                slot.SlotId,
                slot.Category,
                slot.AllowedPoolId,
                coordinate,
                comparedSlotId,
                reason));
        }
    }
}
