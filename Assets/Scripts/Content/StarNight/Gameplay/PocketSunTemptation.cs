using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class PocketSunTemptation : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private bool taken;

        public string Prompt => taken
            ? "비어 있는 해바라기 꼭대기"
            : "마루의 첫 명령 원본과 주머니 해님 가져가기 · 정원 전체를 깨울 위험";

        public void Interact(StarNightPlayerAgent player)
        {
            if (taken)
            {
                return;
            }

            StarNightRunState run = StarNightRunState.Ensure();
            if (run.Chapter.GateLoopEnabled &&
                !run.GetFlag("CH5_STOPPED_ROOM_ENTERED"))
            {
                StarNightHUD.Instance?.Toast(
                    "별문을 켠 뒤 해바라기 입구를 직접 열어야 원본 기록에 닿을 수 있다.");
                return;
            }

            taken = true;
            run.SetFlag("CH5_POCKET_SUN_TAKEN");
            run.SetFlag("CH5_MARU_ORIGINAL_COMMAND_FOUND");
            run.SetFlag("STARPATH_MARU_ORIGINAL_COMMAND_KNOWN");
            run.SetFlag("CH5_FINAL_LIGHT_SUPPORT");
            run.SunSeeds.AddCharges(2);
            run.Heat.AddHeat(42f, "해바라기 꼭대기의 강한 광원을 주머니에 넣음", "PocketSun");
            float scent = run.ConsequenceResolver.ModifyScent(22f);
            run.Chapter.AddScent(scent, "주머니 해님의 빛이 정원 구석까지 퍼졌다", "PocketSun");
            foreach (SunGrowthState growth in FindObjectsByType<SunGrowthState>(FindObjectsSortMode.None))
            {
                if (growth.Kind == SunGrowthKind.SleepingCreature ||
                    growth.Kind == SunGrowthKind.GardenPlant)
                {
                    growth.ApplySunlight();
                }
            }
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.PocketSunTaken,
                actorId = "Player",
                targetId = "SunflowerPeak",
                detail = "최초 명령 원본 ‘모두 집으로, 아무도 잃지 않게’를 확인하고 최종전용 주머니 해님을 챙겼다",
                scentDelta = scent,
                causedAccident = true,
                witnessed = true
            });
            StarNightHUD.Instance?.Toast(
                "마루의 최초 명령 원본을 찾았다. 주머니 해님 +2 · 최종 관측소의 빛 보조가 열렸지만 정원 전체가 반응한다!",
                7f);
        }
    }
}
