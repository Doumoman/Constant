using System;
using System.Collections.Generic;
using StarNight.Map.WorldGeneration.Data;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class DeterministicRngStreamFactory
    {
        public DeterministicRngStreamFactory(WorldRouteDefinitionSet definitions)
        {
            Definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
            if (definitions.RngStreams == null)
            {
                throw new ArgumentException("RNG stream definitions are unavailable.", nameof(definitions));
            }
        }

        public DeterministicRngStreamFactory(StaticDataRegistry registry)
            : this(GetDefinitions(registry))
        {
        }

        public WorldRouteDefinitionSet Definitions { get; }

        public DeterministicRngStream Create(
            string streamId,
            ulong worldSeed,
            RngStreamScope scope)
        {
            var definition = GetDefinition(streamId);
            var initialState = DeterministicRngSeedDeriver.DeriveInitialState(
                worldSeed,
                definition,
                scope);
            return new DeterministicRngStream(initialState);
        }

        internal RngStreamDefinition GetDefinition(string streamId)
        {
            if (streamId == null)
            {
                throw new ArgumentNullException(nameof(streamId));
            }

            if (streamId.Length == 0)
            {
                throw new ArgumentException("RNG stream ID cannot be empty.", nameof(streamId));
            }

            if (!Definitions.RngStreams.TryGetValue(streamId, out var definition))
            {
                throw new KeyNotFoundException("RNG stream definition was not found: " + streamId);
            }

            DeterministicRngSeedDeriver.ValidateDefinition(definition);
            if (!string.Equals(definition.RngStreamId, streamId, StringComparison.Ordinal))
            {
                throw new ArgumentException("RNG stream key and definition ID do not match.", nameof(streamId));
            }

            return definition;
        }

        private static WorldRouteDefinitionSet GetDefinitions(StaticDataRegistry registry)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            return registry.WorldRouteDefinitions;
        }
    }
}
