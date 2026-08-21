using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SiteCandidateEnumerationResult
    {
        private readonly IReadOnlyList<SiteCandidateEnumerationError> errors;

        public SiteCandidateEnumerationResult(
            SiteCandidateCatalog catalog,
            IEnumerable<SiteCandidateEnumerationError> errors)
        {
            if (errors == null) throw new ArgumentNullException(nameof(errors));
            var snapshot = new List<SiteCandidateEnumerationError>(errors);
            foreach (var error in snapshot)
            {
                if (error == null)
                    throw new ArgumentException("Errors cannot contain null.", nameof(errors));
            }
            snapshot.Sort((left, right) =>
            {
                var source = string.Compare(
                    left.SourceDefinitionId,
                    right.SourceDefinitionId,
                    StringComparison.Ordinal);
                if (source != 0) return source;
                var code = left.ErrorCode.CompareTo(right.ErrorCode);
                return code != 0
                    ? code
                    : string.Compare(left.Message, right.Message, StringComparison.Ordinal);
            });
            if ((catalog == null) == (snapshot.Count == 0))
                throw new ArgumentException("Success requires a catalog and failure requires errors.", nameof(errors));

            Catalog = catalog;
            this.errors = new ReadOnlyCollection<SiteCandidateEnumerationError>(snapshot);
        }

        public bool Succeeded => Catalog != null;
        public SiteCandidateCatalog Catalog { get; }
        public IReadOnlyList<SiteCandidateEnumerationError> Errors => errors;
    }
}
