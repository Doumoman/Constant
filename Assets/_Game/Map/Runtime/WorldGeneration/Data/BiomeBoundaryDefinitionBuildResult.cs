using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class BiomeBoundaryDefinitionBuildResult
    {
        private readonly ReadOnlyCollection<BiomeBoundaryDefinitionBuildError> errors;

        internal BiomeBoundaryDefinitionBuildResult(
            BiomeBoundaryDefinitionSet definitionSet,
            IEnumerable<BiomeBoundaryDefinitionBuildError> sourceErrors)
        {
            DefinitionSet = definitionSet;
            errors = new ReadOnlyCollection<BiomeBoundaryDefinitionBuildError>(
                new List<BiomeBoundaryDefinitionBuildError>(
                    sourceErrors ?? throw new ArgumentNullException(nameof(sourceErrors))));
            if (errors.Count > 0 && definitionSet != null)
            {
                throw new ArgumentException("A failed build cannot publish a definition set.");
            }
        }

        public bool Success => DefinitionSet != null && errors.Count == 0;
        public BiomeBoundaryDefinitionSet DefinitionSet { get; }
        public IReadOnlyList<BiomeBoundaryDefinitionBuildError> Errors => errors;
    }
}
