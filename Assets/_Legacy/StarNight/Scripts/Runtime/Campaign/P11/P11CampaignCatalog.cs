#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Rooms;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [CreateAssetMenu(
        menuName = "StarNight/P11/Common Region Campaign Catalog",
        fileName = "P11_CommonRegionCampaignCatalog")]
    public sealed class P11CampaignCatalog : ScriptableObject
    {
        [SerializeField] private P11StageDefinition[] stages =
            Array.Empty<P11StageDefinition>();

        public IReadOnlyList<P11StageDefinition> Stages => stages;
        public int StageCount => stages != null ? stages.Length : 0;

        public void Configure(P11StageDefinition[] definitions)
        {
            stages = definitions ?? Array.Empty<P11StageDefinition>();
        }

        public P11StageDefinition Find(P11StageId stageId)
        {
            if (stages == null)
            {
                return null;
            }

            for (int index = 0; index < stages.Length; index++)
            {
                P11StageDefinition stage = stages[index];
                if (stage != null && stage.StageId == stageId)
                {
                    return stage;
                }
            }

            return null;
        }

        public int CountForRegion(RoomRegion region)
        {
            return stages != null
                ? stages.Count(item =>
                    item != null && item.Region == region)
                : 0;
        }

        public string[] ValidateCatalog()
        {
            var issues = new List<string>();
            if (StageCount != 9)
            {
                issues.Add("P11 catalog must contain exactly nine stages.");
            }

            if (stages == null)
            {
                return issues.ToArray();
            }

            if (stages.Any(item => item == null))
            {
                issues.Add("P11 catalog contains a null stage.");
            }

            if (stages.Where(item => item != null)
                .GroupBy(item => item.StageId)
                .Any(group => group.Count() > 1))
            {
                issues.Add("P11 catalog contains duplicate stage ids.");
            }

            if (stages.Any(item =>
                    item != null
                    && string.IsNullOrWhiteSpace(
                        item.CoreActionSentence)))
            {
                issues.Add(
                    "Every P11 stage needs one core action sentence.");
            }

            P11CampaignRouteProof proof =
                P11CampaignRouteProof.Evaluate(this);
            if (!proof.Passed)
            {
                issues.Add("P11 route proof failed.");
            }

            return issues.ToArray();
        }
    }
}

#endif
