#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;

namespace StarNight.Population.P7
{
    public sealed class P7EconomyGateSummary
    {
        internal P7EconomyGateSummary(
            int sampleCount,
            double averageMainPathGold,
            int purchaseFailures,
            int riskRewardFailures)
        {
            SampleCount = sampleCount;
            AverageMainPathGold = averageMainPathGold;
            PurchaseFailures = purchaseFailures;
            RiskRewardFailures = riskRewardFailures;
        }

        public int SampleCount { get; }
        public double AverageMainPathGold { get; }
        public int PurchaseFailures { get; }
        public int RiskRewardFailures { get; }
        public bool Passed =>
            SampleCount > 0
            && AverageMainPathGold >= P7EconomyRules.BigGoldValue
            && PurchaseFailures == 0
            && RiskRewardFailures == 0;
    }

    public static class P7EconomyGateEvaluator
    {
        public static P7EconomyGateSummary Evaluate(
            IReadOnlyList<P7PopulationResult> results)
        {
            if (results == null || results.Count == 0)
            {
                return new P7EconomyGateSummary(0, 0d, 1, 1);
            }

            int purchaseFailures = 0;
            int riskRewardFailures = 0;
            var accepted = new List<P7PopulationPlan>(results.Count);
            for (int index = 0; index < results.Count; index++)
            {
                P7PopulationResult result = results[index];
                if (result == null || !result.Accepted)
                {
                    purchaseFailures++;
                    riskRewardFailures++;
                    continue;
                }

                accepted.Add(result.Plan);
                if (!result.Validation.MainPathPurchaseSatisfied)
                {
                    purchaseFailures++;
                }

                if (!result.Validation.OptionalRiskRewardSatisfied)
                {
                    riskRewardFailures++;
                }
            }

            double average = accepted.Count == 0
                ? 0d
                : accepted.Average(plan =>
                    (double)plan.MainPathGoldAvailableForShop);
            return new P7EconomyGateSummary(
                results.Count,
                average,
                purchaseFailures,
                riskRewardFailures);
        }
    }
}

#endif
