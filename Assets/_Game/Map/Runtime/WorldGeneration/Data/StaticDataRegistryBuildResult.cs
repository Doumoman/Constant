using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class StaticDataRegistryBuildResult
    {
        private readonly ReadOnlyCollection<StaticDataRegistryBuildError> errors;

        internal StaticDataRegistryBuildResult(
            StaticDataRegistry registry,
            IEnumerable<StaticDataRegistryBuildError> sourceErrors)
        {
            Registry = registry;
            errors = new ReadOnlyCollection<StaticDataRegistryBuildError>(
                new List<StaticDataRegistryBuildError>(
                    sourceErrors ?? throw new ArgumentNullException(nameof(sourceErrors))));
        }

        public bool Success => Registry != null && errors.Count == 0;
        public bool InputGatePassed => Success;
        public StaticDataRegistry Registry { get; }
        public IReadOnlyList<StaticDataRegistryBuildError> Errors => errors;
    }
}
