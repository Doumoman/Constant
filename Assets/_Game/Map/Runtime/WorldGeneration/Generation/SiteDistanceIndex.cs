using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class SiteDistanceIndex
    {
        private readonly IReadOnlyList<SitePlacementKey> keys;
        private readonly IReadOnlyList<SiteDistanceRecord> records;
        private readonly IReadOnlyDictionary<SitePlacementKey, bool> keyLookup;
        private readonly IReadOnlyDictionary<SitePlacementPairKey, SiteDistanceRecord> recordLookup;

        internal SiteDistanceIndex(
            IEnumerable<SitePlacementKey> keys,
            IEnumerable<SiteDistanceRecord> records)
        {
            var keySnapshot = new List<SitePlacementKey>(keys ?? throw new ArgumentNullException(nameof(keys)));
            keySnapshot.Sort();
            var keyMap = new Dictionary<SitePlacementKey, bool>();
            foreach (var key in keySnapshot) keyMap.Add(key, true);

            var recordSnapshot = new List<SiteDistanceRecord>(
                records ?? throw new ArgumentNullException(nameof(records)));
            recordSnapshot.Sort(CompareRecords);
            var pairMap = new Dictionary<SitePlacementPairKey, SiteDistanceRecord>();
            foreach (var record in recordSnapshot)
            {
                if (record == null)
                    throw new ArgumentException("Records cannot contain null.", nameof(records));
                pairMap.Add(new SitePlacementPairKey(record.First, record.Second), record);
            }

            this.keys = new ReadOnlyCollection<SitePlacementKey>(keySnapshot);
            this.records = new ReadOnlyCollection<SiteDistanceRecord>(recordSnapshot);
            keyLookup = new ReadOnlyDictionary<SitePlacementKey, bool>(keyMap);
            recordLookup = new ReadOnlyDictionary<SitePlacementPairKey, SiteDistanceRecord>(pairMap);
        }

        public IReadOnlyList<SitePlacementKey> Keys => keys;
        public IReadOnlyList<SiteDistanceRecord> Records => records;
        public int PlacementCount => keys.Count;
        public int PairCount => records.Count;

        public bool Contains(SitePlacementKey key) => key.IsValid && keyLookup.ContainsKey(key);

        public bool TryGetDistance(
            SitePlacementKey first,
            SitePlacementKey second,
            out int distance)
        {
            if (!first.IsValid || !second.IsValid || !Contains(first) || !Contains(second))
            {
                distance = -1;
                return false;
            }
            if (first == second)
            {
                distance = 0;
                return true;
            }
            if (recordLookup.TryGetValue(new SitePlacementPairKey(first, second), out var record))
            {
                distance = record.Distance;
                return true;
            }
            distance = -1;
            return false;
        }

        public bool TryGetRecord(
            SitePlacementKey first,
            SitePlacementKey second,
            out SiteDistanceRecord record)
        {
            if (!first.IsValid || !second.IsValid || first == second)
            {
                record = null;
                return false;
            }
            return recordLookup.TryGetValue(new SitePlacementPairKey(first, second), out record);
        }

        public SiteDistanceEvaluationResult Evaluate(SiteDistancePolicy policy)
        {
            if (policy == null)
            {
                return SiteDistanceEvaluationResult.Failure(new[]
                {
                    Error(SiteDistanceErrorCode.MissingPolicy, string.Empty, string.Empty,
                        "A site-distance policy is required.")
                });
            }

            var errors = new List<SiteDistanceError>();
            foreach (var policyKey in policy.Keys)
            {
                if (!Contains(policyKey))
                {
                    errors.Add(Error(SiteDistanceErrorCode.MissingPolicyKey,
                        policyKey.SourceDefinitionId, string.Empty,
                        "The index is missing a policy key."));
                }
            }
            foreach (var indexKey in keys)
            {
                var found = false;
                foreach (var policyKey in policy.Keys)
                {
                    if (indexKey == policyKey)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    errors.Add(Error(SiteDistanceErrorCode.UnexpectedIndexKey,
                        indexKey.SourceDefinitionId, string.Empty,
                        "The index contains a key outside the policy."));
                }
            }
            if (errors.Count != 0) return SiteDistanceEvaluationResult.Failure(errors);

            var violations = new List<SiteDistanceViolation>();
            foreach (var constraint in policy.Constraints)
            {
                if (!TryGetRecord(constraint.First, constraint.Second, out var record))
                {
                    errors.Add(Error(SiteDistanceErrorCode.MissingDistanceRecord,
                        constraint.First.SourceDefinitionId,
                        constraint.Second.SourceDefinitionId,
                        "A required distance record is missing."));
                    continue;
                }
                if (record.Distance < constraint.MinimumDistance)
                {
                    violations.Add(new SiteDistanceViolation(
                        constraint.RuleKind,
                        constraint.First,
                        constraint.Second,
                        record.Distance,
                        constraint.MinimumDistance,
                        record.FirstClosestSector,
                        record.SecondClosestSector));
                }
            }
            return errors.Count == 0
                ? SiteDistanceEvaluationResult.Success(violations)
                : SiteDistanceEvaluationResult.Failure(errors);
        }

        private static SiteDistanceError Error(
            SiteDistanceErrorCode code,
            string first,
            string second,
            string message) => new SiteDistanceError(code, first, second, -1, message);

        private static int CompareRecords(SiteDistanceRecord left, SiteDistanceRecord right)
        {
            var first = left.First.CompareTo(right.First);
            return first != 0 ? first : left.Second.CompareTo(right.Second);
        }
    }
}
