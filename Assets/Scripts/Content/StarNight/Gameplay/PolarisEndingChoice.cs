using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class PolarisEndingChoice : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private PolarisEndingType endingType;

        public PolarisEndingType EndingType => endingType;
        public string Prompt => endingType switch
        {
            PolarisEndingType.PathCutter => "별지기의 가위로 마루와 중심별의 연결 자르기",
            PolarisEndingType.NewLeash => "붉은 실로 마루의 새 목줄을 나에게 연결하기",
            PolarisEndingType.ClosedUniverse => "중심별을 마루에게 돌려보내고 떠나기",
            PolarisEndingType.StarRoad => "편지·실·도장·빛으로 라니를 마루에게 연결하기",
            _ => "결말 선택"
        };

        public void Configure(PolarisEndingType value)
        {
            endingType = value;
        }

        public void Interact(StarNightPlayerAgent player)
        {
            ExecuteForTests();
        }

        public bool ExecuteForTests()
        {
            PolarisFinaleState finale = StarNightRunState.Ensure().GetComponent<PolarisFinaleState>();
            if (finale == null)
            {
                return false;
            }
            if (endingType == PolarisEndingType.StarRoad && !finale.StarRoadAvailable)
            {
                StarNightHUD.Instance?.Toast(
                    $"별길을 연결할 단서가 부족하다 · {finale.BuildStarRoadRequirements()}", 7f);
                return false;
            }
            if (!finale.TryChooseEnding(endingType))
            {
                StarNightHUD.Instance?.Toast("아직 중심별에 먼저 도달하지 못했다.");
                return false;
            }

            StarNightHUD hud = StarNightHUD.Instance;
            if (hud == null)
            {
                hud = FindFirstObjectByType<StarNightHUD>();
            }
            hud?.ShowFinalEnding(
                PolarisFinaleState.EndingTitle(endingType),
                PolarisFinaleState.EndingBody(endingType));
            return true;
        }
    }
}
