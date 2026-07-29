using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class StarNightCheckpoint : MonoBehaviour
    {
        [SerializeField] private string checkpointName = "달등불";
        private bool activated;

        public void Configure(string label)
        {
            checkpointName = label;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            StarNightJourneyNavigation navigation = other.GetComponentInParent<StarNightJourneyNavigation>();
            if (navigation == null)
            {
                return;
            }

            navigation.SetCheckpoint(transform.position + Vector3.up * 1.2f);
            if (!activated)
            {
                activated = true;
                StarNightHUD.Instance?.Toast($"{checkpointName}에 불을 밝혔다.");
            }
        }
    }
}
