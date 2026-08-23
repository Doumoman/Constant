using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public sealed class MicrochunkObjectSlotPoolDefinition
    {
        private readonly IReadOnlyList<MicrochunkSlotCategory> allowedCategories;

        public string PoolId { get; }
        public IReadOnlyList<MicrochunkSlotCategory> AllowedCategories => allowedCategories;
        public bool RequiredSlotsAllowed { get; }
        public bool OptionalSlotsAllowed { get; }
        public string Notes { get; }

        public MicrochunkObjectSlotPoolDefinition(
            string poolId,
            IEnumerable<MicrochunkSlotCategory> allowedCategories,
            bool requiredSlotsAllowed,
            bool optionalSlotsAllowed,
            string notes)
        {
            if (string.IsNullOrWhiteSpace(poolId))
            {
                throw new ArgumentException("Object-slot pool ID is required.", nameof(poolId));
            }
            if (allowedCategories == null) throw new ArgumentNullException(nameof(allowedCategories));

            var values = new List<MicrochunkSlotCategory>();
            var unique = new HashSet<MicrochunkSlotCategory>();
            foreach (var category in allowedCategories)
            {
                if (!Enum.IsDefined(typeof(MicrochunkSlotCategory), category))
                {
                    throw new ArgumentOutOfRangeException(nameof(allowedCategories));
                }
                if (!unique.Add(category))
                {
                    throw new ArgumentException("Allowed slot categories must be unique.", nameof(allowedCategories));
                }
                values.Add(category);
            }
            if (values.Count == 0)
            {
                throw new ArgumentException("At least one allowed slot category is required.", nameof(allowedCategories));
            }

            values.Sort();
            PoolId = poolId;
            this.allowedCategories = new ReadOnlyCollection<MicrochunkSlotCategory>(values);
            RequiredSlotsAllowed = requiredSlotsAllowed;
            OptionalSlotsAllowed = optionalSlotsAllowed;
            Notes = notes ?? string.Empty;
        }

        public bool AllowsCategory(MicrochunkSlotCategory category)
        {
            return allowedCategories.Contains(category);
        }
    }
}
