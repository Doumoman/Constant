using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class SpecialVillageDefinitionBuildResult
    {
        private readonly ReadOnlyCollection<SpecialVillageDefinitionBuildError> errors;

        internal SpecialVillageDefinitionBuildResult(
            SpecialVillageDefinitionSet definitionSet,
            IEnumerable<SpecialVillageDefinitionBuildError> sourceErrors)
        {
            DefinitionSet = definitionSet;
            errors = new ReadOnlyCollection<SpecialVillageDefinitionBuildError>(
                new List<SpecialVillageDefinitionBuildError>(
                    sourceErrors ?? throw new ArgumentNullException(nameof(sourceErrors))));
            if (errors.Count > 0 && definitionSet != null)
            {
                throw new ArgumentException("A failed build cannot publish a definition set.");
            }
        }

        public bool Success => DefinitionSet != null && errors.Count == 0;
        public SpecialVillageDefinitionSet DefinitionSet { get; }
        public IReadOnlyList<SpecialVillageDefinitionBuildError> Errors => errors;
    }
}
