using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class SunGardenMaruCommandEcho : MonoBehaviour
    {
        private void Start()
        {
            PlayForCurrentChapter();
        }

        public bool PlayForCurrentChapter()
        {
            StarNightRunState run = StarNightRunState.Ensure();
            if (run.GetFlag("CH5_MARU_COMMAND_ECHO_HEARD"))
            {
                return false;
            }

            run.SetFlag("CH5_MARU_COMMAND_ECHO_HEARD");
            run.SetNpcState("ReturnedSunSeed", StarNpcState.Calm);
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.ObjectInspected,
                actorId = "Maru",
                targetId = "MaruCommandEcho",
                detail = "마루가 헤매던 작은 해씨를 화분에 돌려놓고 반복했다: 모두 집으로. 아무도 잃지 않게.",
                helpedResident = true,
                witnessed = true
            });
            StarNightHUD.Instance?.Toast(
                "마루: “모두 집으로.”\n“아무도 잃지 않게.”\n작은 해씨를 화분에 돌려놓은 마루가 새벽별을 물고 떠난다.",
                8f);
            return true;
        }
    }
}
