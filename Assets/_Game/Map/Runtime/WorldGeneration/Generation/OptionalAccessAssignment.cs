using System;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class OptionalAccessAssignment
    {
        internal OptionalAccessAssignment(
            OptionalRegionId regionId,
            int regionOrdinal,
            int attachmentOrder,
            int mandatoryRouteSectorIndex,
            SectorCoord mandatoryRouteSector,
            int entrySectorIndex,
            SectorCoord entrySector,
            int entrySideFromMandatoryDx,
            int entrySideFromMandatoryDy,
            OptionalRegionAccessRule accessRule,
            OptionalAccessRequirement requirement,
            OptionalAccessTraversalKind traversalKind,
            OptionalAccessClue clue,
            int toolCostTier,
            int explosiveFuelCost,
            int hiddenClueDifficulty,
            bool requiresPartialRewardPreview)
        {
            if (!regionId.IsValid) throw new ArgumentException("Region ID must be valid.", nameof(regionId));
            if (regionOrdinal < 0 || regionOrdinal > 9999) throw new ArgumentOutOfRangeException(nameof(regionOrdinal));
            if (attachmentOrder < 0 || attachmentOrder > 9999) throw new ArgumentOutOfRangeException(nameof(attachmentOrder));
            RequireIndexCoordinateIdentity(mandatoryRouteSectorIndex, mandatoryRouteSector, nameof(mandatoryRouteSectorIndex));
            RequireIndexCoordinateIdentity(entrySectorIndex, entrySector, nameof(entrySectorIndex));

            var cardinal =
                (entrySideFromMandatoryDx == -1 && entrySideFromMandatoryDy == 0) ||
                (entrySideFromMandatoryDx == 1 && entrySideFromMandatoryDy == 0) ||
                (entrySideFromMandatoryDx == 0 && entrySideFromMandatoryDy == -1) ||
                (entrySideFromMandatoryDx == 0 && entrySideFromMandatoryDy == 1);
            if (!cardinal ||
                entrySector.X - mandatoryRouteSector.X != entrySideFromMandatoryDx ||
                entrySector.Y - mandatoryRouteSector.Y != entrySideFromMandatoryDy)
                throw new ArgumentException("Entry direction must preserve the cardinal attachment identity.");

            if (clue == null) throw new ArgumentNullException(nameof(clue));
            if (clue.RegionId != regionId || clue.AttachmentOrder != attachmentOrder)
                throw new ArgumentException("Clue identity must match the assignment.", nameof(clue));

            ValidateMatrix(
                accessRule, requirement, traversalKind, clue.Kind,
                toolCostTier, explosiveFuelCost, hiddenClueDifficulty,
                requiresPartialRewardPreview);

            RegionId = regionId;
            RegionOrdinal = regionOrdinal;
            AttachmentOrder = attachmentOrder;
            MandatoryRouteSectorIndex = mandatoryRouteSectorIndex;
            MandatoryRouteSector = mandatoryRouteSector;
            EntrySectorIndex = entrySectorIndex;
            EntrySector = entrySector;
            EntrySideFromMandatoryDx = entrySideFromMandatoryDx;
            EntrySideFromMandatoryDy = entrySideFromMandatoryDy;
            AccessRule = accessRule;
            Requirement = requirement;
            TraversalKind = traversalKind;
            Clue = clue;
            ToolCostTier = toolCostTier;
            ExplosiveFuelCost = explosiveFuelCost;
            HiddenClueDifficulty = hiddenClueDifficulty;
            RequiresPartialRewardPreview = requiresPartialRewardPreview;
        }

        public OptionalRegionId RegionId { get; }
        public int RegionOrdinal { get; }
        public int AttachmentOrder { get; }
        public int MandatoryRouteSectorIndex { get; }
        public SectorCoord MandatoryRouteSector { get; }
        public int EntrySectorIndex { get; }
        public SectorCoord EntrySector { get; }
        public int EntrySideFromMandatoryDx { get; }
        public int EntrySideFromMandatoryDy { get; }
        public OptionalRegionAccessRule AccessRule { get; }
        public OptionalAccessRequirement Requirement { get; }
        public OptionalAccessTraversalKind TraversalKind { get; }
        public OptionalAccessClue Clue { get; }
        public int ToolCostTier { get; }
        public int ExplosiveFuelCost { get; }
        public int HiddenClueDifficulty { get; }
        public bool RequiresPartialRewardPreview { get; }

        private static void RequireIndexCoordinateIdentity(int index, SectorCoord coordinate, string parameterName)
        {
            if (index < 0 || index >= WorldGenConstants.SectorCount)
                throw new ArgumentOutOfRangeException(parameterName);
            if (coordinate.X < 0 || coordinate.X >= WorldGenConstants.SectorColumns ||
                coordinate.Y < 0 || coordinate.Y >= WorldGenConstants.SectorRows ||
                coordinate.Y * WorldGenConstants.SectorColumns + coordinate.X != index)
                throw new ArgumentException("Sector index and coordinate must match.", parameterName);
        }

        private static void ValidateMatrix(
            OptionalRegionAccessRule accessRule,
            OptionalAccessRequirement requirement,
            OptionalAccessTraversalKind traversalKind,
            OptionalAccessClueKind clueKind,
            int toolCostTier,
            int explosiveFuelCost,
            int hiddenClueDifficulty,
            bool preview)
        {
            switch (accessRule)
            {
                case OptionalRegionAccessRule.Basic:
                    Require(requirement == OptionalAccessRequirement.None &&
                            traversalKind == OptionalAccessTraversalKind.OptionalBreak &&
                            clueKind == OptionalAccessClueKind.BasicOpening &&
                            toolCostTier == 0 && explosiveFuelCost == 0 && hiddenClueDifficulty == 0 && !preview);
                    return;
                case OptionalRegionAccessRule.Tool:
                    Require((requirement == OptionalAccessRequirement.Pickaxe ||
                             requirement == OptionalAccessRequirement.Shovel ||
                             requirement == OptionalAccessRequirement.Rope) &&
                            traversalKind == OptionalAccessTraversalKind.OptionalBreak &&
                            clueKind == OptionalAccessClueKind.ToolSurface &&
                            toolCostTier >= 1 && toolCostTier <= 4 &&
                            explosiveFuelCost == 0 && hiddenClueDifficulty == 0 && !preview);
                    return;
                case OptionalRegionAccessRule.Environment:
                    Require(requirement == OptionalAccessRequirement.Environment &&
                            traversalKind == OptionalAccessTraversalKind.OptionalBreak &&
                            clueKind == OptionalAccessClueKind.EnvironmentDevice &&
                            toolCostTier == 0 && explosiveFuelCost == 0 && hiddenClueDifficulty == 0 && !preview);
                    return;
                case OptionalRegionAccessRule.Explosive:
                    Require(requirement == OptionalAccessRequirement.Explosive &&
                            traversalKind == OptionalAccessTraversalKind.OptionalBreak &&
                            clueKind == OptionalAccessClueKind.ExplosiveRewardPreview &&
                            toolCostTier == 0 && explosiveFuelCost >= 1 && explosiveFuelCost <= 100 &&
                            hiddenClueDifficulty == 0 && preview);
                    return;
                case OptionalRegionAccessRule.Hidden:
                    Require(requirement == OptionalAccessRequirement.None &&
                            traversalKind == OptionalAccessTraversalKind.Hidden &&
                            (clueKind == OptionalAccessClueKind.HiddenCrack ||
                             clueKind == OptionalAccessClueKind.HiddenLight ||
                             clueKind == OptionalAccessClueKind.HiddenSound) &&
                            toolCostTier == 0 && explosiveFuelCost == 0 &&
                            hiddenClueDifficulty >= 1 && hiddenClueDifficulty <= 4 && !preview);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(accessRule));
            }
        }

        private static void Require(bool condition)
        {
            if (!condition) throw new ArgumentException("Optional access assignment does not match the rule consistency matrix.");
        }
    }
}
