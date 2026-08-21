#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Campaign.P10
{
    [DisallowMultipleComponent]
    public sealed class P10BranchSupportState2D : MonoBehaviour
    {
        [SerializeField] private bool magpieNestRepaired;
        [SerializeField] private bool carpWaterwayRestored;

        public bool MagpieNestRepaired => magpieNestRepaired;
        public bool CarpWaterwayRestored => carpWaterwayRestored;
        public bool KnotSpiderSupportReady => magpieNestRepaired;
        public bool DragonGatekeeperSupportReady =>
            carpWaterwayRestored;
        public bool MainProgressBlocked => false;

        public bool Resolve(P10BranchEventKind eventKind)
        {
            switch (eventKind)
            {
                case P10BranchEventKind.RepairMagpieNest:
                    if (magpieNestRepaired)
                    {
                        return false;
                    }

                    magpieNestRepaired = true;
                    return true;
                case P10BranchEventKind.RestoreCarpWaterway:
                    if (carpWaterwayRestored)
                    {
                        return false;
                    }

                    carpWaterwayRestored = true;
                    return true;
                default:
                    return false;
            }
        }

        public void ResetForTests()
        {
            magpieNestRepaired = false;
            carpWaterwayRestored = false;
        }
    }
}

#endif
