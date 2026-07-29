using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class SunGardenInheritedDisplay : MonoBehaviour
    {
        [SerializeField] private GameObject raniSilence;
        [SerializeField] private GameObject argumentHeat;
        [SerializeField] private GameObject teleportShortcut;
        [SerializeField] private GameObject shadeGreenhouse;
        [SerializeField] private GameObject maruLetterTrail;
        [SerializeField] private GameObject sorterDebris;

        public void Configure(GameObject silence, GameObject argument, GameObject shortcut,
            GameObject shade, GameObject maruTrail, GameObject debris)
        {
            raniSilence = silence;
            argumentHeat = argument;
            teleportShortcut = shortcut;
            shadeGreenhouse = shade;
            maruLetterTrail = maruTrail;
            sorterDebris = debris;
        }

        private void Start()
        {
            StarNightRunState run = StarNightRunState.Ensure();
            SetActive(raniSilence, run.GetFlag("CH5_RANI_SILENCE"));
            SetActive(argumentHeat, run.GetFlag("CH5_RANI_ARGUMENT_ECHO"));
            SetActive(teleportShortcut, run.GetFlag("CH5_TELEPORT_CORE_SHORTCUT"));
            SetActive(shadeGreenhouse, run.GetFlag("CH5_SHADE_GREENHOUSE"));
            SetActive(maruLetterTrail, run.GetFlag("CH5_MARU_KNOWS_LETTER"));
            SetActive(sorterDebris, run.GetFlag("CH5_SORTER_DEBRIS_FLAMMABLE"));
        }

        private static void SetActive(GameObject target, bool value)
        {
            if (target != null)
            {
                target.SetActive(value);
            }
        }
    }
}
