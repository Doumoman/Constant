using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class StarRareStampUpgrade : MonoBehaviour, IStarNightInteractable
    {
        private bool claimed;
        public string Prompt => claimed ? "구름 우표를 이미 떼어 냈다" : "희귀 구름 우표 챙기기";

        public void Interact(StarNightPlayerAgent player)
        {
            if (claimed)
            {
                return;
            }

            StarNightRunState run = StarNightRunState.Ensure();
            if (run.Chapter.GateLoopEnabled &&
                (!run.Chapter.GateActivated || !run.GetFlag("CH4_RETURN_VAULT_OPENED")))
            {
                StarNightHUD.Instance?.Toast(
                    "희귀 우표는 별문 가동 뒤 심층 보관소를 직접 연 경우에만 회수할 수 있다.");
                return;
            }

            claimed = true;
            run.AddCounter("delivery.scent_discount");
            run.SetFlag("CH4_RARE_CLOUD_STAMP");
            run.Chapter.AddScent(run.ConsequenceResolver.ModifyScent(8f),
                "희귀 우표의 반짝임이 모든 수신함에 비쳤다", "CloudStamp");
            StarNightHUD.Instance?.Toast("구름 우표 획득 · 이후 배송 별냄새 28% 감소.", 5f);
            gameObject.SetActive(false);
        }
    }
}
