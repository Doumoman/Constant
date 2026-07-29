using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class StoredSunlightSource : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private string sourceId = "stored-sun";
        [SerializeField] private string displayName = "작은 저장 햇빛";
        [SerializeField, Min(1)] private int charges = 1;
        [SerializeField] private bool rare;
        [SerializeField] private bool collected;
        [SerializeField] private SunGardenStoredLightRoute storedLightRoute;

        public bool Collected => collected;
        public string Prompt => collected
            ? $"{displayName}은 이미 비어 있다"
            : $"{displayName} 모으기";

        public void Configure(string id, string label, int amount = 1, bool isRare = false)
        {
            sourceId = id;
            displayName = label;
            charges = Mathf.Max(1, amount);
            rare = isRare;
        }

        public void ConfigureRoute(SunGardenStoredLightRoute route)
        {
            storedLightRoute = route;
        }

        public void Interact(StarNightPlayerAgent player)
        {
            if (collected)
            {
                StarNightHUD.Instance?.Toast("빛이 머물던 따뜻한 껍질만 남아 있다.");
                return;
            }

            collected = true;
            StarNightRunState run = StarNightRunState.Ensure();
            run.SunSeeds.AddCharges(charges, rare);
            float scent = run.ConsequenceResolver.ModifyScent(rare ? 9f : 3f);
            run.Chapter.AddScent(scent, $"{displayName}을 햇빛 씨앗에 담았다", sourceId);
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.SunlightCollected,
                actorId = "Player",
                targetId = sourceId,
                detail = rare
                    ? $"{displayName}을 희귀 햇빛 씨앗으로 보존했다"
                    : $"{displayName}을 모아 다른 생명을 깨울 빛으로 바꾸었다",
                scentDelta = scent,
                helpedResident = !rare,
                witnessed = true
            });
            bool routeCompleted = storedLightRoute != null &&
                                  storedLightRoute.RegisterCollection(sourceId);

            foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.color *= new Color(0.3f, 0.3f, 0.35f, 0.65f);
            }
            StarNightHUD.Instance?.Toast(routeCompleted
                ? "저장 햇빛 경로 완료 · 고른 빛을 별문에 심을 수 있다."
                : rare
                    ? $"희귀 햇빛 씨앗 +{charges} · 과열된 정원을 되살릴 수 있다."
                    : storedLightRoute != null && run.Chapter.GateLoopEnabled
                        ? $"저장 햇빛 {storedLightRoute.CollectedCount}/{storedLightRoute.RequiredSources} · 현재 씨앗 {run.SunSeeds.Charges}"
                        : $"저장 햇빛 +{charges} · 현재 {run.SunSeeds.Charges}");
        }
    }
}
