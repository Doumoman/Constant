#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Folklore.P9;
using StarNight.Rooms;
using UnityEngine;

namespace StarNight.Campaign.P10
{
    [CreateAssetMenu(
        menuName = "StarNight/P10/First Branch Campaign Catalog",
        fileName = "P10_FirstBranchCampaignCatalog")]
    public sealed class P10CampaignCatalog : ScriptableObject
    {
        [SerializeField] private P10StageDefinition[] stages =
            Array.Empty<P10StageDefinition>();
        [SerializeField] private P10BranchFeelDefinition[] branchFeel =
            Array.Empty<P10BranchFeelDefinition>();

        public IReadOnlyList<P10StageDefinition> Stages => stages;
        public IReadOnlyList<P10BranchFeelDefinition> BranchFeel =>
            branchFeel;
        public bool BranchesAreMechanicallyDistinct
        {
            get
            {
                P10BranchFeelDefinition magpie =
                    FindBranch(P9BranchKind.MagpieBridge);
                P10BranchFeelDefinition dragon =
                    FindBranch(P9BranchKind.DragonPalace);
                return magpie != null
                    && dragon != null
                    && magpie.IsDistinctFrom(dragon);
            }
        }

        public void Configure(
            P10StageDefinition[] stageDefinitions,
            P10BranchFeelDefinition[] branchDefinitions)
        {
            stages = stageDefinitions
                ?? Array.Empty<P10StageDefinition>();
            branchFeel = branchDefinitions
                ?? Array.Empty<P10BranchFeelDefinition>();
        }

        public P10StageDefinition Find(P10StageId stageId)
        {
            for (int index = 0; index < stages.Length; index++)
            {
                P10StageDefinition definition = stages[index];
                if (definition != null
                    && definition.StageId == stageId)
                {
                    return definition;
                }
            }

            return null;
        }

        public P10BranchFeelDefinition FindBranch(
            P9BranchKind branch)
        {
            for (int index = 0; index < branchFeel.Length; index++)
            {
                P10BranchFeelDefinition definition =
                    branchFeel[index];
                if (definition != null
                    && definition.Branch == branch)
                {
                    return definition;
                }
            }

            return null;
        }

        public int CountForRegion(RoomRegion region)
        {
            int count = 0;
            for (int index = 0; index < stages.Length; index++)
            {
                if (stages[index] != null
                    && stages[index].Region == region)
                {
                    count++;
                }
            }

            return count;
        }

        public string[] ValidateCatalog()
        {
            List<string> issues = new List<string>();
            if (stages == null || stages.Length != 9)
            {
                issues.Add(
                    "The P10 catalog requires exactly nine stages.");
            }
            else
            {
                HashSet<P10StageId> ids = new HashSet<P10StageId>();
                for (int index = 0; index < stages.Length; index++)
                {
                    P10StageDefinition stage = stages[index];
                    if (stage == null)
                    {
                        issues.Add($"Stage definition {index} is null.");
                        continue;
                    }

                    if (!ids.Add(stage.StageId))
                    {
                        issues.Add(
                            $"Stage id {stage.StageId} is duplicated.");
                    }

                    if (string.IsNullOrWhiteSpace(
                            stage.CoreActionSentence)
                        || !stage.MainPathToolFree
                        || !stage.OptionalEventsNeverGateExit)
                    {
                        issues.Add(
                            $"{stage.StageId} lacks the one-sentence, "
                            + "tool-free optional-event contract.");
                    }
                }
            }

            if (CountForRegion(RoomRegion.MoonPalace) != 3
                || CountForRegion(RoomRegion.MagpieBridge) != 3
                || CountForRegion(RoomRegion.DragonPalace) != 3)
            {
                issues.Add(
                    "Moon Palace, Magpie Bridge, and Dragon Palace "
                    + "must each contain three stages.");
            }

            if (branchFeel == null
                || branchFeel.Length != 2
                || !BranchesAreMechanicallyDistinct)
            {
                issues.Add(
                    "The two branch feel profiles are not mechanically "
                    + "distinct.");
            }

            P10CampaignRouteProof proof =
                P10CampaignRouteProof.Evaluate(this);
            if (!proof.Passed)
            {
                issues.Add(
                    "Normal and cross-route topology proof failed.");
            }

            return issues.ToArray();
        }
    }
}

#endif
