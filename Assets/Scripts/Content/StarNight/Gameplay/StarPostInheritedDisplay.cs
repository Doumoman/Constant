using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class StarPostInheritedDisplay : MonoBehaviour
    {
        [SerializeField] private GameObject dryInk;
        [SerializeField] private GameObject wetLetters;
        [SerializeField] private GameObject rainShortcut;
        [SerializeField] private GameObject cloudStamp;

        public void Configure(GameObject dry, GameObject wet, GameObject shortcut, GameObject stamp)
        {
            dryInk = dry;
            wetLetters = wet;
            rainShortcut = shortcut;
            cloudStamp = stamp;
        }

        private void Start()
        {
            StarNightRunState run = StarNightRunState.Ensure();
            if (dryInk != null) dryInk.SetActive(run.GetFlag("CH4_DRY_INK"));
            if (wetLetters != null) wetLetters.SetActive(run.GetFlag("CH4_WET_INK"));
            if (rainShortcut != null) rainShortcut.SetActive(run.GetFlag("CH4_RAIN_SHORTCUT"));
            if (cloudStamp != null) cloudStamp.SetActive(run.GetFlag("CH4_CLOUD_STAMP_AVAILABLE"));
        }
    }
}
