using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Data;

namespace StarNight.Map.WorldGeneration.Generation
{
    public interface IWorldGenerationPass
    {
        string PassId { get; }
        string ClassName { get; }
        WorldGenerationPassResult Execute(WorldGenerationPassContext context);
    }

    public sealed class WorldGenerationPassContext
    {
        public WorldGenerationPassContext(
            ulong worldSeed,
            StaticDataRegistry staticData,
            GenerationProfileDefinition generationProfile,
            GenerationPassDefinition passDefinition,
            WorldGenerationArtifactStore inputs,
            WorldGenerationRngStreams rngStreams,
            int attemptOrdinal,
            string retryScopeId)
        {
            if (staticData == null) throw new ArgumentNullException(nameof(staticData));
            if (generationProfile == null) throw new ArgumentNullException(nameof(generationProfile));
            if (passDefinition == null) throw new ArgumentNullException(nameof(passDefinition));
            if (inputs == null) throw new ArgumentNullException(nameof(inputs));
            if (rngStreams == null) throw new ArgumentNullException(nameof(rngStreams));
            if (attemptOrdinal < 0) throw new ArgumentOutOfRangeException(nameof(attemptOrdinal));
            if (retryScopeId == null) throw new ArgumentNullException(nameof(retryScopeId));
            var declaredInputs = passDefinition.InputArtifacts == null
                ? null
                : passDefinition.InputArtifacts.OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (declaredInputs == null || !inputs.ArtifactIds.SequenceEqual(declaredInputs))
                throw new ArgumentException(
                    "Input artifacts must exactly match the pass definition.",
                    nameof(inputs));

            WorldSeed = worldSeed;
            StaticData = staticData;
            GenerationProfile = generationProfile;
            PassDefinition = passDefinition;
            Inputs = inputs;
            RngStreams = rngStreams;
            AttemptOrdinal = attemptOrdinal;
            RetryScopeId = retryScopeId;
        }

        public ulong WorldSeed { get; }
        public StaticDataRegistry StaticData { get; }
        public GenerationProfileDefinition GenerationProfile { get; }
        public GenerationPassDefinition PassDefinition { get; }
        public WorldGenerationArtifactStore Inputs { get; }
        public WorldGenerationRngStreams RngStreams { get; }
        public int AttemptOrdinal { get; }
        public string RetryScopeId { get; }
    }

    public sealed class WorldGenerationPassResult
    {
        private WorldGenerationPassResult(
            bool succeeded,
            IReadOnlyDictionary<string, object> outputs,
            string failureCode,
            string failureMessage,
            string retryScopeId)
        {
            Succeeded = succeeded;
            Outputs = outputs;
            FailureCode = failureCode;
            FailureMessage = failureMessage;
            RetryScopeId = retryScopeId;
        }

        public bool Succeeded { get; }
        public IReadOnlyDictionary<string, object> Outputs { get; }
        public string FailureCode { get; }
        public string FailureMessage { get; }
        public string RetryScopeId { get; }

        public static WorldGenerationPassResult Success(
            IEnumerable<KeyValuePair<string, object>> outputs)
        {
            if (outputs == null) throw new ArgumentNullException(nameof(outputs));
            var copy = new SortedDictionary<string, object>(StringComparer.Ordinal);
            foreach (var pair in outputs)
            {
                if (string.IsNullOrEmpty(pair.Key))
                    throw new ArgumentException("Output identifiers must be non-empty.", nameof(outputs));
                if (pair.Value == null)
                    throw new ArgumentException("Output values cannot be null.", nameof(outputs));
                if (copy.ContainsKey(pair.Key))
                    throw new ArgumentException("Output identifiers must be unique.", nameof(outputs));
                copy.Add(pair.Key, pair.Value);
            }
            return new WorldGenerationPassResult(
                true,
                new ReadOnlyDictionary<string, object>(copy),
                string.Empty,
                string.Empty,
                string.Empty);
        }

        public static WorldGenerationPassResult Success(string artifactId, object value)
        {
            return Success(new[] { new KeyValuePair<string, object>(artifactId, value) });
        }

        public static WorldGenerationPassResult Failure(
            string failureCode,
            string failureMessage,
            string retryScopeId = "")
        {
            if (string.IsNullOrEmpty(failureCode))
                throw new ArgumentException("Failure code must be non-empty.", nameof(failureCode));
            if (failureMessage == null) throw new ArgumentNullException(nameof(failureMessage));
            if (retryScopeId == null) throw new ArgumentNullException(nameof(retryScopeId));
            return new WorldGenerationPassResult(
                false,
                new ReadOnlyDictionary<string, object>(
                    new SortedDictionary<string, object>(StringComparer.Ordinal)),
                failureCode,
                failureMessage,
                retryScopeId);
        }
    }
}
