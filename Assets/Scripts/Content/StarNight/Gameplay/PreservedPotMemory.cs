using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class PreservedPotMemory : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private bool inspected;

        public string Prompt => "라니가 멈춰 둔 작은 화분 살피기";

        public void Interact(StarNightPlayerAgent player)
        {
            if (inspected)
            {
                StarNightHUD.Instance?.Toast("꽃은 시들지도 피지도 않은 채, 떠나던 날의 빛 안에 갇혀 있다.");
                return;
            }

            inspected = true;
            StarNightRunState run = StarNightRunState.Ensure();
            run.SetFlag("CH5_RANI_PRESERVED_POT_FOUND");
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.PreservedPotFound,
                actorId = "Player",
                targetId = "RaniSiblingPot",
                detail = "라니가 동생이 떠난 날의 시든 꽃을 버리지도 새로 심지도 못한 채 시간을 멈춰 둔 화분을 발견했다",
                witnessed = true
            });
            StarNightHUD.Instance?.Toast(
                "라니는 시든 꽃을 버리지도, 새 씨앗을 심지도 못했다. 정원은 슬픔을 보존하기 위해 잠들어 있었다.", 8f);
        }
    }
}
