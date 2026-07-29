using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class CloudRanchInheritedSupply : MonoBehaviour
    {
        [SerializeField] private FableObject replacementWeight;
        [SerializeField] private GameObject magpieSafetyNet;

        public void Configure(FableObject weight, GameObject safetyNet)
        {
            replacementWeight = weight;
            magpieSafetyNet = safetyNet;
        }

        private void Start()
        {
            StarNightRunState run = StarNightRunState.Ensure();
            if (run.GetFlag("CH3_SUPPLY_SHORTAGE") && replacementWeight != null && replacementWeight.Body != null)
            {
                replacementWeight.Body.mass *= 1.5f;
                replacementWeight.Body.gravityScale *= 1.25f;
            }
            if (run.GetFlag("CH3_RESCUE_SUPPORT_REDUCED") && magpieSafetyNet != null)
            {
                magpieSafetyNet.SetActive(false);
            }
        }
    }
}
