using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class WorldRouteDefinitionBuildResult
    {
        private readonly ReadOnlyCollection<WorldRouteDefinitionBuildError> errors;

        internal WorldRouteDefinitionBuildResult(
            WorldRouteDefinitionSet definitionSet,
            IEnumerable<WorldRouteDefinitionBuildError> sourceErrors)
        {
            DefinitionSet = definitionSet;
            errors = new ReadOnlyCollection<WorldRouteDefinitionBuildError>(
                new List<WorldRouteDefinitionBuildError>(
                    sourceErrors ?? throw new ArgumentNullException(nameof(sourceErrors))));

            if (errors.Count > 0 && definitionSet != null)
            {
                throw new ArgumentException("A failed build cannot publish a definition set.");
            }
        }

        public bool Success => DefinitionSet != null && errors.Count == 0;
        public WorldRouteDefinitionSet DefinitionSet { get; }
        public IReadOnlyList<WorldRouteDefinitionBuildError> Errors => errors;
    }
}
