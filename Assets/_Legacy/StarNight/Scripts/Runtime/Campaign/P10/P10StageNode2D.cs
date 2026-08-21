#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Campaign.P10
{
    [DisallowMultipleComponent]
    public sealed class P10StageNode2D : MonoBehaviour
    {
        [SerializeField] private P10StageDefinition definition;
        [SerializeField] private P10CampaignDirector2D director;
        [SerializeField] private P10StageEnvironment2D environment;
        [SerializeField] private bool bossDefeated;

        public P10StageDefinition Definition => definition;
        public P10StageId StageId =>
            definition != null
                ? definition.StageId
                : P10StageId.None;
        public P10CampaignDirector2D Director => director;
        public P10StageEnvironment2D Environment => environment;
        public bool BossDefeated => bossDefeated;
        public bool MainPathToolFree =>
            definition != null && definition.MainPathToolFree;
        public bool OptionalEventsNeverGateExit =>
            definition != null
            && definition.OptionalEventsNeverGateExit;
        public bool ThemePresentationReady =>
            environment != null
            && environment.ThemePresentationReady;
        public bool ExitAvailable =>
            definition != null
            && (!definition.IsBossStage || bossDefeated);

        public void Configure(
            P10StageDefinition stageDefinition,
            P10CampaignDirector2D campaignDirector,
            P10StageEnvironment2D stageEnvironment)
        {
            definition = stageDefinition;
            director = campaignDirector;
            environment = stageEnvironment;
            bossDefeated = definition == null
                || !definition.IsBossStage;
        }

        public bool TryEnter()
        {
            return director != null
                && director.TryEnterStage(StageId);
        }

        public bool TryComplete()
        {
            return ExitAvailable
                && director != null
                && director.CurrentStage == StageId
                && director.TryCompleteCurrentStage();
        }

        public void MarkBossDefeated()
        {
            bossDefeated = true;
        }

        public void ResetForTests()
        {
            bossDefeated = definition == null
                || !definition.IsBossStage;
        }
    }
}

#endif
