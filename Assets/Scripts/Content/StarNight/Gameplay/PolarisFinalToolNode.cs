using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class PolarisFinalToolNode : MonoBehaviour, IStarNightInteractable
    {
        [SerializeField] private FableVerb requiredVerb;
        [SerializeField] private SpriteRenderer marker;

        public FableVerb RequiredVerb => requiredVerb;
        public string Prompt => $"{PolarisFinaleState.VerbDisplayName(requiredVerb)}로 별길 복구";

        public void Configure(FableVerb verb, SpriteRenderer targetMarker)
        {
            requiredVerb = verb;
            marker = targetMarker;
        }

        public void Interact(StarNightPlayerAgent player)
        {
            if (player == null)
            {
                return;
            }
            if (player.SelectedTool != requiredVerb)
            {
                StarNightHUD.Instance?.Toast(
                    $"지금 필요한 도구는 {PolarisFinaleState.VerbDisplayName(requiredVerb)}다. R로 도구를 바꾸자.");
                return;
            }
            ExecuteForTests(player.SelectedTool);
        }

        public bool ExecuteForTests(FableVerb verb)
        {
            PolarisFinaleState finale = StarNightRunState.Ensure().GetComponent<PolarisFinaleState>();
            if (finale == null || !finale.TryRestore(verb))
            {
                string expected = finale != null
                    ? PolarisFinaleState.VerbDisplayName(finale.ExpectedVerb)
                    : PolarisFinaleState.VerbDisplayName(requiredVerb);
                StarNightHUD.Instance?.Toast($"별길의 순서가 맞지 않는다. 먼저 {expected}가 필요하다.");
                return false;
            }

            if (marker != null)
            {
                marker.color = new Color(0.45f, 0.92f, 1f);
                marker.transform.localScale *= 1.18f;
            }
            StarNightHUD.Instance?.Toast(
                $"{PolarisFinaleState.VerbDisplayName(verb)} 복구 완료 · {finale.RestorationStep}/{finale.RestorationRequired}",
                4f);
            return true;
        }
    }
}
