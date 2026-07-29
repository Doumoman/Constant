using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class MagpieOldBridgeSwitch : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private GameObject shortcutBarrier;
        [SerializeField] private SpriteRenderer ropeVisual;
        [SerializeField] private GateRouteObjective routeObjective;
        private bool cut;

        public string Prompt => cut ? "옛 물류 실 다시 묶기" : "옛 물류 실을 끊어 지름길 열기";

        public void Configure(GameObject barrier, SpriteRenderer visual)
        {
            shortcutBarrier = barrier;
            ropeVisual = visual;
        }

        public void ConfigureRouteObjective(GateRouteObjective objective)
        {
            routeObjective = objective;
        }

        public void Interact(StarNightPlayerAgent player)
        {
            StarNightRunState run = StarNightRunState.Ensure();
            if (cut && run.Chapter.GateLoopEnabled && run.Chapter.GateReady)
            {
                StarNightHUD.Instance?.Toast(
                    "별문 2/2가 고정된 뒤에는 물류 다리의 대체 닻을 설치할 시간이 없다.", 4.5f);
                return;
            }

            cut = !cut;
            if (shortcutBarrier != null)
            {
                shortcutBarrier.SetActive(!cut);
            }
            if (ropeVisual != null)
            {
                ropeVisual.color = cut ? new Color(0.35f, 0.2f, 0.24f, 0.45f) : new Color(0.92f, 0.12f, 0.3f);
            }

            if (cut)
            {
                run.SetFlag("CH2_OLD_BRIDGE_CUT");
                if (run.Chapter.GateLoopEnabled)
                {
                    routeObjective?.Complete();
                }
                run.Chapter.AddScent(12f, "오래된 물류 실이 큰 소리를 내며 끊어졌다", "OldBridge");
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.OldBridgeCut,
                    actorId = "Player",
                    targetId = "OldLogisticsBridge",
                    detail = "옛 물류 다리를 끊어 지름길을 열었다",
                    causedAccident = true,
                    witnessed = true
                });
                StarNightHUD.Instance?.Toast(run.Chapter.GateLoopEnabled
                    ? "옛 물류 닻을 별문용으로 전용했다. 빠르지만, 대체 닻을 놓지 않으면 다음 행성의 상자가 줄어든다."
                    : "낡은 물류 실이 끊어져 아래 지름길이 열렸다. 다음 행성의 상자는 오지 않을 것이다.", 5f);
            }
            else
            {
                run.SetFlag("CH2_OLD_BRIDGE_RESTORED");
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.OldBridgeRestored,
                    actorId = "Player",
                    targetId = "OldLogisticsBridge",
                    detail = "떠나기 전 옛 물류 다리를 다시 연결했다",
                    helpedResident = true,
                    witnessed = true
                });
                StarNightHUD.Instance?.Toast(run.Chapter.GateLoopEnabled
                    ? "GateReady 전에 대체 닻을 설치했다. 낡은 닻 기여는 남고 다음 물류도 다시 건널 수 있다."
                    : "물류 실을 다시 묶었다. 지름길은 닫혔지만 다음 상자는 건널 수 있다.", 5f);
            }
        }
    }
}
