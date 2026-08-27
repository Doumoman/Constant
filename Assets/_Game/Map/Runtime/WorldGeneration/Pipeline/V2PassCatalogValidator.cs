using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace StarNight.Map.WorldGeneration.Pipeline
{
    public enum V2CatalogIssueCode
    {
        NullEntry,
        UnexpectedPassCount,
        DuplicatePassId,
        DuplicateOrder,
        DuplicateOutputArtifact,
        MissingInputArtifact,
        InputProducedTooLate,
        DependencyCycle,
        UnusedIntermediateOutput,
        InvalidFinalPass,
        InvalidFinalValidationEscalation,
    }

    public sealed class V2CatalogValidationIssue
    {
        public V2CatalogValidationIssue(V2CatalogIssueCode code, string detail)
        {
            Code = code;
            Detail = detail ?? string.Empty;
        }

        public V2CatalogIssueCode Code { get; }
        public string Detail { get; }
        public override string ToString() => Code + ":" + Detail;
    }

    public sealed class V2CatalogValidationResult
    {
        private readonly ReadOnlyCollection<V2CatalogValidationIssue> issues;

        internal V2CatalogValidationResult(IEnumerable<V2CatalogValidationIssue> source)
        {
            issues = new ReadOnlyCollection<V2CatalogValidationIssue>(source.ToArray());
        }

        public bool IsValid => issues.Count == 0;
        public IReadOnlyList<V2CatalogValidationIssue> Issues => issues;
    }

    public static class V2PassCatalogValidator
    {
        private const int ExpectedPassCount = 10;

        public static V2CatalogValidationResult Validate(IEnumerable<V2PassContract> contracts)
        {
            if (contracts == null) throw new ArgumentNullException(nameof(contracts));

            var source = contracts.ToArray();
            var issues = new List<V2CatalogValidationIssue>();
            if (source.Any(value => value == null))
            {
                issues.Add(new V2CatalogValidationIssue(V2CatalogIssueCode.NullEntry, "null"));
            }

            var entries = source.Where(value => value != null).OrderBy(value => value.Order).ToArray();
            if (entries.Length != ExpectedPassCount)
            {
                issues.Add(new V2CatalogValidationIssue(
                    V2CatalogIssueCode.UnexpectedPassCount,
                    entries.Length.ToString()));
            }

            AddDuplicateIssues(entries, issues);

            var producerByArtifact = new Dictionary<V2WorldGenerationArtifactId, V2PassContract>();
            foreach (var entry in entries)
            {
                foreach (var output in entry.OutputArtifactIds)
                {
                    if (!producerByArtifact.ContainsKey(output))
                    {
                        producerByArtifact.Add(output, entry);
                    }
                }
            }

            foreach (var entry in entries)
            {
                foreach (var input in entry.InputArtifactIds)
                {
                    if (input == V2WorldGenerationArtifactId.ApprovedMapBaseline)
                    {
                        continue;
                    }

                    if (!producerByArtifact.TryGetValue(input, out var producer))
                    {
                        issues.Add(new V2CatalogValidationIssue(
                            V2CatalogIssueCode.MissingInputArtifact,
                            entry.PassId + "<-" + input));
                    }
                    else if (producer.Order >= entry.Order)
                    {
                        issues.Add(new V2CatalogValidationIssue(
                            V2CatalogIssueCode.InputProducedTooLate,
                            entry.PassId + "<-" + producer.PassId));
                    }
                }
            }

            DetectCycles(entries, producerByArtifact, issues);
            DetectUnusedIntermediateOutputs(entries, issues);
            ValidateFinalPass(entries, issues);
            ValidateFinalEscalation(entries, issues);

            return new V2CatalogValidationResult(issues
                .OrderBy(value => value.Code)
                .ThenBy(value => value.Detail, StringComparer.Ordinal));
        }

        private static void AddDuplicateIssues(
            IReadOnlyCollection<V2PassContract> entries,
            ICollection<V2CatalogValidationIssue> issues)
        {
            AddDuplicates(
                entries.GroupBy(value => value.PassId).Where(group => group.Count() > 1),
                V2CatalogIssueCode.DuplicatePassId,
                issues);
            AddDuplicates(
                entries.GroupBy(value => value.Order).Where(group => group.Count() > 1),
                V2CatalogIssueCode.DuplicateOrder,
                issues);
            AddDuplicates(
                entries.SelectMany(value => value.OutputArtifactIds.Select(output => new { value, output }))
                    .GroupBy(value => value.output).Where(group => group.Count() > 1),
                V2CatalogIssueCode.DuplicateOutputArtifact,
                issues);
        }

        private static void AddDuplicates<TKey, TValue>(
            IEnumerable<IGrouping<TKey, TValue>> groups,
            V2CatalogIssueCode code,
            ICollection<V2CatalogValidationIssue> issues)
        {
            foreach (var group in groups)
            {
                issues.Add(new V2CatalogValidationIssue(code, group.Key.ToString()));
            }
        }

        private static void DetectCycles(
            IReadOnlyCollection<V2PassContract> entries,
            IReadOnlyDictionary<V2WorldGenerationArtifactId, V2PassContract> producerByArtifact,
            ICollection<V2CatalogValidationIssue> issues)
        {
            var dependencies = entries
                .GroupBy(value => value.PassId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .SelectMany(value => value.InputArtifactIds)
                        .Where(producerByArtifact.ContainsKey)
                        .Select(input => producerByArtifact[input].PassId)
                        .Distinct()
                        .ToArray());
            var visiting = new HashSet<V2WorldGenerationPassId>();
            var visited = new HashSet<V2WorldGenerationPassId>();

            foreach (var passId in dependencies.Keys)
            {
                if (HasCycle(passId, dependencies, visiting, visited))
                {
                    issues.Add(new V2CatalogValidationIssue(
                        V2CatalogIssueCode.DependencyCycle,
                        passId.ToString()));
                    return;
                }
            }
        }

        private static bool HasCycle(
            V2WorldGenerationPassId passId,
            IReadOnlyDictionary<V2WorldGenerationPassId, V2WorldGenerationPassId[]> dependencies,
            ISet<V2WorldGenerationPassId> visiting,
            ISet<V2WorldGenerationPassId> visited)
        {
            if (visited.Contains(passId)) return false;
            if (!visiting.Add(passId)) return true;

            if (dependencies.TryGetValue(passId, out var values))
            {
                foreach (var dependency in values)
                {
                    if (HasCycle(dependency, dependencies, visiting, visited)) return true;
                }
            }

            visiting.Remove(passId);
            visited.Add(passId);
            return false;
        }

        private static void DetectUnusedIntermediateOutputs(
            IReadOnlyList<V2PassContract> entries,
            ICollection<V2CatalogValidationIssue> issues)
        {
            if (entries.Count == 0) return;

            var finalOutputs = new HashSet<V2WorldGenerationArtifactId>(entries[entries.Count - 1].OutputArtifactIds);
            var consumed = new HashSet<V2WorldGenerationArtifactId>(
                entries.SelectMany(value => value.InputArtifactIds));
            foreach (var output in entries.SelectMany(value => value.OutputArtifactIds))
            {
                if (!finalOutputs.Contains(output) && !consumed.Contains(output))
                {
                    issues.Add(new V2CatalogValidationIssue(
                        V2CatalogIssueCode.UnusedIntermediateOutput,
                        output.ToString()));
                }
            }
        }

        private static void ValidateFinalPass(
            IReadOnlyList<V2PassContract> entries,
            ICollection<V2CatalogValidationIssue> issues)
        {
            if (entries.Count == 0 ||
                entries[entries.Count - 1].PassId != V2WorldGenerationPassId.MicroChunkSlice ||
                !entries[entries.Count - 1].OutputArtifactIds.SequenceEqual(new[]
                {
                    V2WorldGenerationArtifactId.GeneratedMicroChunkSlices,
                }))
            {
                issues.Add(new V2CatalogValidationIssue(
                    V2CatalogIssueCode.InvalidFinalPass,
                    entries.Count == 0 ? "empty" : entries[entries.Count - 1].PassId.ToString()));
            }
        }

        private static void ValidateFinalEscalation(
            IEnumerable<V2PassContract> entries,
            ICollection<V2CatalogValidationIssue> issues)
        {
            var validation = entries.FirstOrDefault(value =>
                value.PassId == V2WorldGenerationPassId.TileValidation);
            var expected = new[] { V2RetryScope.Pattern, V2RetryScope.Cluster, V2RetryScope.Footprint };
            if (validation == null ||
                validation.FailurePolicy != V2FailurePolicy.OrderedEscalation ||
                !validation.RetryEscalation.SequenceEqual(expected))
            {
                issues.Add(new V2CatalogValidationIssue(
                    V2CatalogIssueCode.InvalidFinalValidationEscalation,
                    validation == null ? "missing" : string.Join(",", validation.RetryEscalation)));
            }
        }
    }
}
