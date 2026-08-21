using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class ForeignKeyResolutionResult
    {
        private readonly ReadOnlyCollection<ResolvedForeignKeyReference> references;
        private readonly ReadOnlyCollection<ForeignKeyResolutionError> errors;

        internal ForeignKeyResolutionResult(
            ForeignKeyRecordIndex recordIndex,
            IEnumerable<ResolvedForeignKeyReference> sourceReferences,
            IEnumerable<ForeignKeyResolutionError> sourceErrors)
        {
            RecordIndex = recordIndex;
            references = new ReadOnlyCollection<ResolvedForeignKeyReference>(
                new List<ResolvedForeignKeyReference>(
                    sourceReferences ?? throw new ArgumentNullException(nameof(sourceReferences))));
            errors = new ReadOnlyCollection<ForeignKeyResolutionError>(
                new List<ForeignKeyResolutionError>(
                    sourceErrors ?? throw new ArgumentNullException(nameof(sourceErrors))));

            if (RecordIndex == null && references.Count > 0)
            {
                throw new ArgumentException(
                    "An input-gate failure cannot publish resolved references.");
            }
        }

        public bool InputGatePassed => RecordIndex != null;

        public bool Success => InputGatePassed && errors.Count == 0;

        public ForeignKeyRecordIndex RecordIndex { get; }

        public IReadOnlyList<ResolvedForeignKeyReference> References => references;

        public IReadOnlyList<ForeignKeyResolutionError> Errors => errors;
    }
}
