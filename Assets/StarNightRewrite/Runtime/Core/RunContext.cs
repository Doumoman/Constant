using UnityEngine;

namespace StarNight.Rewrite.Core
{
    [DefaultExecutionOrder(-900)]
    [DisallowMultipleComponent]
    public sealed class RunContext : MonoBehaviour
    {
        [SerializeField]
        private bool resetLoadoutOnAwake = true;

        [SerializeField]
        private RunLoadout loadout = new RunLoadout();

        public RunLoadout Loadout
        {
            get
            {
                loadout ??= new RunLoadout();
                return loadout;
            }
        }

        private void Awake()
        {
            if (resetLoadoutOnAwake)
            {
                Loadout.ResetForNewRun();
            }
        }
    }
}
