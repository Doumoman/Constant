using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class PolarisInheritedDisplay : MonoBehaviour
    {
        [SerializeField] private GameObject stableSun;
        [SerializeField] private GameObject tiredSun;
        [SerializeField] private GameObject fireDamage;
        [SerializeField] private GameObject restoredPot;
        [SerializeField] private GameObject stableRoute;
        [SerializeField] private GameObject overgrownRoute;
        [SerializeField] private GameObject burnedRoute;

        public void Configure(GameObject stableSunObject, GameObject tiredSunObject,
            GameObject fireDamageObject, GameObject restoredPotObject,
            GameObject stableRouteObject, GameObject overgrownRouteObject, GameObject burnedRouteObject)
        {
            stableSun = stableSunObject;
            tiredSun = tiredSunObject;
            fireDamage = fireDamageObject;
            restoredPot = restoredPotObject;
            stableRoute = stableRouteObject;
            overgrownRoute = overgrownRouteObject;
            burnedRoute = burnedRouteObject;
        }

        private void Start()
        {
            StarNightRunState run = StarNightRunState.Ensure();
            SetActive(stableSun, run.GetFlag("CH5_HAOREUM_NATURAL_WAKE"));
            SetActive(tiredSun, run.GetFlag("CH5_SUN_AWAKENED_FORCEFULLY"));
            SetActive(fireDamage, run.GetFlag("CH5_GARDEN_FIRE") && !run.GetFlag("CH5_GARDEN_RESTORED"));
            SetActive(restoredPot, run.GetFlag("CH5_GARDEN_RESTORED"));
            SetActive(stableRoute, run.GetFlag("CH5_STAR_PATH_TREE_STABLE"));
            SetActive(overgrownRoute, run.GetFlag("CH5_STAR_PATH_TREE_OVERGROWN"));
            SetActive(burnedRoute, run.GetFlag("CH5_STAR_PATH_TREE_BURNED"));
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }
    }
}
