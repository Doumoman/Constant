#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StarNight.Campaign.P12
{
    [CreateAssetMenu(
        menuName = "StarNight/P12/Starless Sea Challenge Catalog",
        fileName = "P12_StarlessSeaChallengeCatalog")]
    public sealed class P12ChallengeCatalog : ScriptableObject
    {
        [SerializeField] private P12StageDefinition[] stages =
            Array.Empty<P12StageDefinition>();

        public IReadOnlyList<P12StageDefinition> Stages => stages;
        public int StageCount => stages != null ? stages.Length : 0;

        public void Configure(P12StageDefinition[] definitions)
        {
            stages = definitions ?? Array.Empty<P12StageDefinition>();
        }

        public P12StageDefinition Find(P12StageId stageId)
        {
            if (stages == null)
            {
                return null;
            }

            for (int index = 0; index < stages.Length; index++)
            {
                P12StageDefinition stage = stages[index];
                if (stage != null && stage.StageId == stageId)
                {
                    return stage;
                }
            }

            return null;
        }

        public int CountForSegment(P12ChallengeSegment segment)
        {
            return stages != null
                ? stages.Count(item =>
                    item != null && item.Segment == segment)
                : 0;
        }

        public string[] ValidateCatalog()
        {
            var issues = new List<string>();
            if (StageCount != 12)
            {
                issues.Add(
                    "P12 catalog must contain exactly twelve stages.");
            }

            if (stages == null)
            {
                return issues.ToArray();
            }

            if (stages.Any(item => item == null))
            {
                issues.Add("P12 catalog contains a null stage.");
            }

            if (stages.Where(item => item != null)
                .GroupBy(item => item.StageId)
                .Any(group => group.Count() > 1))
            {
                issues.Add("P12 catalog contains duplicate stage ids.");
            }

            if (stages.Any(item =>
                    item != null
                    && string.IsNullOrWhiteSpace(
                        item.CoreActionSentence)))
            {
                issues.Add(
                    "Every P12 stage needs one core action sentence.");
            }

            P12ChallengeRouteProof proof =
                P12ChallengeRouteProof.Evaluate(this);
            if (!proof.Passed)
            {
                issues.Add("P12 route proof failed.");
            }

            return issues.ToArray();
        }
    }
}

#endif
