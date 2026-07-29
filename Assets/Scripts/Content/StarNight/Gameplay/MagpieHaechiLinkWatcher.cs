using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class MagpieHaechiLinkWatcher : MonoBehaviour
    {
        [SerializeField] private FableObject haechi;
        [SerializeField] private FableObject tetherPost;
        private RedThreadSystem thread;

        public void Configure(FableObject npc, FableObject post)
        {
            haechi = npc;
            tetherPost = post;
        }

        private void Start()
        {
            thread = StarNightRunState.Ensure().RedThread;
            thread.ConnectionCreated += OnConnectionCreated;
        }

        private void OnDestroy()
        {
            if (thread != null)
            {
                thread.ConnectionCreated -= OnConnectionCreated;
            }
        }

        private void OnConnectionCreated(RedThreadConnection connection)
        {
            StarNightRunState run = StarNightRunState.Ensure();
            if (run.GetFlag("CH2_HAECHI_RESOLVED") || !connection.Connects(haechi, tetherPost))
            {
                return;
            }

            run.SetFlag("CH2_HAECHI_RESOLVED");
            run.SetFlag("CH2_HAECHI_FORCED");
            run.SetFlag("CH2_HAECHI_TETHERED");
            run.SetNpcState("Haechi", StarNpcState.Dependent);
            connection.LockAsRepaired();
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.NpcForcedReturn,
                actorId = "Player",
                targetId = "Haechi",
                tool = FableVerb.Link,
                detail = "붉은 실로 해치를 정거장에 묶었다",
                witnessed = true
            });
            StarNightHUD.Instance?.Toast("붉은 실이 해치와 정거장을 묶었다. 라니는 이 장면을 안도하며 기록한다.", 5f);
        }
    }
}
