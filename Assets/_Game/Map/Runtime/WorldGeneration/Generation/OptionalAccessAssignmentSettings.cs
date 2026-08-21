using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalAccessAssignmentSettings
    {
        private readonly IReadOnlyList<OptionalRegionAccessRule> accessRulePattern;
        private readonly IReadOnlyList<OptionalAccessRequirement> toolRequirementPattern;
        private readonly IReadOnlyList<OptionalAccessClueKind> hiddenCluePattern;
        private readonly IReadOnlyList<int> toolCostTierByDepth;
        private readonly IReadOnlyList<int> explosiveFuelCostByDepth;
        private readonly IReadOnlyList<int> hiddenClueDifficultyByDepth;

        public OptionalAccessAssignmentSettings(
            IReadOnlyList<OptionalRegionAccessRule> accessRulePattern,
            IReadOnlyList<OptionalAccessRequirement> toolRequirementPattern,
            IReadOnlyList<OptionalAccessClueKind> hiddenCluePattern,
            IReadOnlyList<int> toolCostTierByDepth,
            IReadOnlyList<int> explosiveFuelCostByDepth,
            IReadOnlyList<int> hiddenClueDifficultyByDepth)
        {
            var accessRules = Copy(accessRulePattern, nameof(accessRulePattern), false);
            if (accessRules.Count == 0)
                throw new ArgumentException("Access rule pattern cannot be empty.", nameof(accessRulePattern));
            foreach (var value in accessRules) RequireAccessRule(value, nameof(accessRulePattern));

            var tools = Copy(toolRequirementPattern, nameof(toolRequirementPattern), false);
            if (tools.Count == 0)
                throw new ArgumentException("Tool requirement pattern cannot be empty.", nameof(toolRequirementPattern));
            foreach (var value in tools)
            {
                if (value != OptionalAccessRequirement.Pickaxe &&
                    value != OptionalAccessRequirement.Shovel &&
                    value != OptionalAccessRequirement.Rope)
                    throw new ArgumentOutOfRangeException(nameof(toolRequirementPattern));
            }

            var hiddenClues = Copy(hiddenCluePattern, nameof(hiddenCluePattern), false);
            if (hiddenClues.Count == 0)
                throw new ArgumentException("Hidden clue pattern cannot be empty.", nameof(hiddenCluePattern));
            foreach (var value in hiddenClues)
            {
                if (value != OptionalAccessClueKind.HiddenCrack &&
                    value != OptionalAccessClueKind.HiddenLight &&
                    value != OptionalAccessClueKind.HiddenSound)
                    throw new ArgumentOutOfRangeException(nameof(hiddenCluePattern));
            }

            var toolCosts = Copy(toolCostTierByDepth, nameof(toolCostTierByDepth), true);
            var explosiveCosts = Copy(explosiveFuelCostByDepth, nameof(explosiveFuelCostByDepth), true);
            var hiddenCosts = Copy(hiddenClueDifficultyByDepth, nameof(hiddenClueDifficultyByDepth), true);
            RequireDepthTable(toolCosts, 1, 4, nameof(toolCostTierByDepth));
            RequireDepthTable(explosiveCosts, 1, 100, nameof(explosiveFuelCostByDepth));
            RequireDepthTable(hiddenCosts, 1, 4, nameof(hiddenClueDifficultyByDepth));

            this.accessRulePattern = new ReadOnlyCollection<OptionalRegionAccessRule>(accessRules);
            this.toolRequirementPattern = new ReadOnlyCollection<OptionalAccessRequirement>(tools);
            this.hiddenCluePattern = new ReadOnlyCollection<OptionalAccessClueKind>(hiddenClues);
            this.toolCostTierByDepth = new ReadOnlyCollection<int>(toolCosts);
            this.explosiveFuelCostByDepth = new ReadOnlyCollection<int>(explosiveCosts);
            this.hiddenClueDifficultyByDepth = new ReadOnlyCollection<int>(hiddenCosts);
        }

        public IReadOnlyList<OptionalRegionAccessRule> AccessRulePattern => accessRulePattern;
        public IReadOnlyList<OptionalAccessRequirement> ToolRequirementPattern => toolRequirementPattern;
        public IReadOnlyList<OptionalAccessClueKind> HiddenCluePattern => hiddenCluePattern;
        public IReadOnlyList<int> ToolCostTierByDepth => toolCostTierByDepth;
        public IReadOnlyList<int> ExplosiveFuelCostByDepth => explosiveFuelCostByDepth;
        public IReadOnlyList<int> HiddenClueDifficultyByDepth => hiddenClueDifficultyByDepth;

        private static List<T> Copy<T>(IReadOnlyList<T> source, string parameterName, bool requireFour)
        {
            if (source == null) throw new ArgumentNullException(parameterName);
            var values = new List<T>(source.Count);
            for (var index = 0; index < source.Count; index++) values.Add(source[index]);
            if (requireFour && values.Count != 4)
                throw new ArgumentException("Depth tables require exactly four entries.", parameterName);
            return values;
        }

        private static void RequireDepthTable(IReadOnlyList<int> values, int minimum, int maximum, string parameterName)
        {
            if (values.Count != 4)
                throw new ArgumentException("Depth tables require exactly four entries.", parameterName);
            foreach (var value in values)
            {
                if (value < minimum || value > maximum)
                    throw new ArgumentOutOfRangeException(parameterName);
            }
        }

        private static void RequireAccessRule(OptionalRegionAccessRule value, string parameterName)
        {
            switch (value)
            {
                case OptionalRegionAccessRule.Basic:
                case OptionalRegionAccessRule.Tool:
                case OptionalRegionAccessRule.Environment:
                case OptionalRegionAccessRule.Explosive:
                case OptionalRegionAccessRule.Hidden:
                    return;
                default:
                    throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }
}
