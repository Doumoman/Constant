using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class MicrochunkPopulationItemDefinitionBuildResult
    {
        private readonly ReadOnlyCollection<MicrochunkPopulationItemDefinitionBuildError> errors;

        internal MicrochunkPopulationItemDefinitionBuildResult(
            MicrochunkPopulationItemDefinitionSet definitionSet,
            IEnumerable<MicrochunkPopulationItemDefinitionBuildError> sourceErrors)
        {
            DefinitionSet = definitionSet;
            errors = new ReadOnlyCollection<MicrochunkPopulationItemDefinitionBuildError>(
                new List<MicrochunkPopulationItemDefinitionBuildError>(
                    sourceErrors ?? throw new ArgumentNullException(nameof(sourceErrors))));
            if (errors.Count > 0 && definitionSet != null)
            {
                throw new ArgumentException("A failed build cannot publish a definition set.");
            }
        }

        public bool Success => DefinitionSet != null && errors.Count == 0;
        public MicrochunkPopulationItemDefinitionSet DefinitionSet { get; }
        public IReadOnlyList<MicrochunkPopulationItemDefinitionBuildError> Errors => errors;
    }
}
