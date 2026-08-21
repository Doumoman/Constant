using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalRewardTierSettings
    {
        private readonly IReadOnlyList<int> tierMinimumScores;

        public OptionalRewardTierSettings(
            int depthWeight,
            int explosiveFuelDivisor,
            IReadOnlyList<int> tierMinimumScores)
        {
            if (depthWeight < 1 || depthWeight > 100)
                throw new ArgumentOutOfRangeException(nameof(depthWeight));
            if (explosiveFuelDivisor < 1 || explosiveFuelDivisor > 100)
                throw new ArgumentOutOfRangeException(nameof(explosiveFuelDivisor));
            if (tierMinimumScores == null)
                throw new ArgumentNullException(nameof(tierMinimumScores));

            var values = new List<int>(tierMinimumScores.Count);
            for (var index = 0; index < tierMinimumScores.Count; index++)
                values.Add(tierMinimumScores[index]);
            if (values.Count != 4)
                throw new ArgumentException("Tier minimum scores require exactly four entries.", nameof(tierMinimumScores));
            if (values[0] != 0)
                throw new ArgumentException("The Low tier minimum score must be exactly zero.", nameof(tierMinimumScores));
            for (var index = 0; index < values.Count; index++)
            {
                if (values[index] < 0 || values[index] > 1000000)
                    throw new ArgumentOutOfRangeException(nameof(tierMinimumScores));
                if (index > 0 && values[index] <= values[index - 1])
                    throw new ArgumentException("Tier minimum scores must be strictly increasing.", nameof(tierMinimumScores));
            }

            DepthWeight = depthWeight;
            ExplosiveFuelDivisor = explosiveFuelDivisor;
            this.tierMinimumScores = new ReadOnlyCollection<int>(values);
        }

        public int DepthWeight { get; }
        public int ExplosiveFuelDivisor { get; }
        public IReadOnlyList<int> TierMinimumScores => tierMinimumScores;
    }
}
