using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class PolarisTruthArchive : MonoBehaviour, IStarNightInteractable
    {
        public string Prompt => "닫힌 관측실에서 라니와 마루의 최초 임무 확인";

        public void Interact(StarNightPlayerAgent player)
        {
            ExecuteForTests();
        }

        public bool ExecuteForTests()
        {
            PolarisFinaleState finale = StarNightRunState.Ensure().GetComponent<PolarisFinaleState>();
            if (finale == null || !finale.InspectObservatory())
            {
                StarNightHUD.Instance?.Toast("다섯 정거장의 기록을 먼저 모두 확인해야 한다.");
                return false;
            }
            StarNightHUD.Instance?.Toast(finale.BuildEvaluationAndRebuttal(), 9f);
            return true;
        }
    }
}
