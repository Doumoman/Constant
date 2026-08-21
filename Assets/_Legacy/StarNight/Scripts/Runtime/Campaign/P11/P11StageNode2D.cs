#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    public sealed class P11StageNode2D : MonoBehaviour
    {
        [SerializeField] private P11StageDefinition definition;
        [SerializeField] private P11CampaignDirector2D director;
        [SerializeField] private P11StageEnvironment2D environment;
        [SerializeField] private bool bossDefeated;

        public P11StageDefinition Definition => definition;
        public P11StageId StageId =>
            definition != null
                ? definition.StageId
                : P11StageId.None;
        public P11CampaignDirector2D Director => director;
        public P11StageEnvironment2D Environment => environment;
        public bool BossDefeated => bossDefeated;
        public bool MainPathToolFree =>
            definition != null && definition.MainPathToolFree;
        public bool OptionalStoryNeverGatesExit =>
            definition != null
            && definition.OptionalStoryNeverGatesExit;
        public bool ExitAvailable =>
            definition != null
            && (!definition.IsBossStage || bossDefeated);

        public void Configure(
            P11StageDefinition stageDefinition,
            P11CampaignDirector2D campaignDirector,
            P11StageEnvironment2D stageEnvironment)
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
