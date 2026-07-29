using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class MaruNpcTarget : MonoBehaviour
    {
        [SerializeField] private string npcId = "Resident";
        [SerializeField] private string displayName = "주민";
        [SerializeField, Min(0f)] private float targetPriority = 12f;
        [SerializeField] private bool taken;

        public string NpcId => npcId;
        public string DisplayName => displayName;
        public float TargetPriority => targetPriority;
        public bool Taken => taken;

        public void Configure(string id, string label, float priority = 12f)
        {
            npcId = string.IsNullOrWhiteSpace(id) ? gameObject.name : id;
            displayName = string.IsNullOrWhiteSpace(label) ? gameObject.name : label;
            targetPriority = Mathf.Max(0f, priority);
        }

        public bool TryTake()
        {
            if (taken)
            {
                return false;
            }

            taken = true;
            StarNightRunState.Instance?.SetNpcState(npcId, StarNpcState.Missing);
            gameObject.SetActive(false);
            return true;
        }
    }
}
