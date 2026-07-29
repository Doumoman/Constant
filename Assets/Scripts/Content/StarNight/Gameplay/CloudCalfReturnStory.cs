using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class CloudCalfReturnStory : MonoBehaviour
    {
        [SerializeField] private FableObject calf;
        [SerializeField] private Transform motherSide;

        public void Configure(FableObject calfTarget, Transform returnPoint)
        {
            calf = calfTarget;
            motherSide = returnPoint;
        }

        private void Start()
        {
            PlayForCurrentChapter();
        }

        public bool PlayForCurrentChapter()
        {
            StarNightRunState run = StarNightRunState.Ensure();
            if (run.GetFlag("CH3_CALF_RETURN_WITNESSED"))
            {
                return false;
            }

            if (calf != null && motherSide != null)
            {
                calf.transform.position = motherSide.position;
                if (calf.Body != null)
                {
                    calf.Body.linearVelocity = Vector2.zero;
                }
            }

            run.SetFlag("CH3_CALF_RETURN_WITNESSED");
            run.SetNpcState("CloudCalf", StarNpcState.Calm);
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.CalfReturnedByMaru,
                actorId = "Maru",
                targetId = "CloudCalf",
                detail = "마루가 목장 밖을 떠돌던 새끼 고래를 물어 어미 곁에 내려놓고 바람별을 가져갔다",
                helpedResident = true,
                witnessed = true
            });
            StarNightHUD.Instance?.Toast(
                "마루가 길 잃은 새끼 고래를 어미 곁에 내려놓았다. 바람별만 물고 다음 길로 사라진다.",
                7f);
            return true;
        }
    }
}
