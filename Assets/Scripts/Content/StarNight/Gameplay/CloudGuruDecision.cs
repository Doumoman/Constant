using UnityEngine;

namespace StarFetchingNight
{
    public enum GuruDecisionMode
    {
        ReleaseAnchor,
        RebuildRainSystem
    }

    [DisallowMultipleComponent]
    public sealed class CloudGuruDecision : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private GuruDecisionMode mode;
        [SerializeField] private FableObject guru;
        [SerializeField] private GameObject anchorVisual;
        private bool used;

        public string Prompt => mode == GuruDecisionMode.ReleaseAnchor
            ? "구루의 닻을 풀어 주기"
            : "이동식 비구름 장치 완성하기";

        public void Configure(GuruDecisionMode decisionMode, FableObject guruTarget, GameObject anchor)
        {
            mode = decisionMode;
            guru = guruTarget;
            anchorVisual = anchor;
        }

        public void Interact(StarNightPlayerAgent player)
        {
            if (used)
            {
                return;
            }

            StarNightRunState run = StarNightRunState.Ensure();
            if (mode == GuruDecisionMode.ReleaseAnchor)
            {
                if (run.GetFlag("CH3_GURU_RELEASED"))
                {
                    return;
                }

                used = true;
                run.SetFlag("CH3_GURU_RELEASED");
                run.SetNpcState("Guru", StarNpcState.Autonomous);
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.GuruReleased,
                    actorId = "Player",
                    targetId = "Guru",
                    detail = "밧줄을 감옥이라고 판단해 구루의 닻을 풀었다",
                    witnessed = true
                });
                run.Chapter.AddScent(run.ConsequenceResolver.ModifyScent(10f),
                    "거대한 구름고래가 목장 위로 떠올랐다", "Guru");
                if (anchorVisual != null)
                {
                    anchorVisual.SetActive(false);
                }
                if (guru != null && guru.Body != null)
                {
                    guru.Body.gravityScale = -0.22f;
                    guru.Body.AddForce(Vector2.up * 5f, ForceMode2D.Impulse);
                }
                StarNightHUD.Instance?.Toast("구루는 자유롭게 떠올랐다. 아래 밭의 비가 멎기 시작한다.", 5f);
                return;
            }

            if (!run.GetFlag("CH3_GURU_RELEASED"))
            {
                StarNightHUD.Instance?.Toast("이 장치는 구루를 풀어 준 뒤 비를 대신 보내기 위한 것이다.");
                return;
            }
            bool enoughWind = run.Chapter.GateLoopEnabled
                ? run.Chapter.GateReady
                : run.Chapter.DepartureReady;
            if (!enoughWind)
            {
                StarNightHUD.Instance?.Toast(run.Chapter.GateLoopEnabled
                    ? "먼저 서로 다른 바람 두 개를 별문에 장착해야 한다."
                    : "먼저 비구름 세 덩어리로 수차를 충전해야 한다.");
                return;
            }

            used = true;
            run.SetFlag("CH3_RAIN_SYSTEM_REBUILT");
            run.SetFlag("CH3_DAMAGE_REPAIRED");
            bool guruReturns = !run.GetFlag("CH3_GURU_AWAKENED_FORCEFULLY");
            if (guruReturns)
            {
                run.SetFlag("CH3_GURU_CHOSE_RETURN");
                run.SetNpcState("Guru", StarNpcState.Calm);
            }
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.RainSystemRebuilt,
                actorId = "Player",
                targetId = "MobileRainSystem",
                detail = guruReturns
                    ? "이동식 비구름 장치를 만들자 구루가 스스로 목장으로 돌아왔다"
                    : "이동식 비구름 장치를 남겨 지친 구루가 떠날 수 있게 했다",
                helpedResident = true,
                witnessed = true
            });
            StarNightHUD.Instance?.Toast(guruReturns
                ? "새 수차를 본 구루가 스스로 낮은 구름길로 돌아왔다."
                : "새 수차가 비를 이어받았다. 지친 구루는 먼 구름으로 떠났다.", 6f);
        }
    }
}
