using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class ContentVersionHashResult
    {
        private readonly ReadOnlyCollection<ContentVersionHashError> errors;

        internal ContentVersionHashResult(
            ContentVersionHash hash,
            IEnumerable<ContentVersionHashError> sourceErrors)
        {
            Hash = hash;
            errors = new ReadOnlyCollection<ContentVersionHashError>(
                new List<ContentVersionHashError>(sourceErrors ??
                    throw new ArgumentNullException(nameof(sourceErrors))));
            if ((Hash == null) == (errors.Count == 0))
            {
                throw new ArgumentException(
                    "A content hash result must publish either one hash or one-or-more errors.");
            }
        }

        public bool Success => Hash != null && errors.Count == 0;
        public ContentVersionHash Hash { get; }
        public IReadOnlyList<ContentVersionHashError> Errors => errors;
    }
}
