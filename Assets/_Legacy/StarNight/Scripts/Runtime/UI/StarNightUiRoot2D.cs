#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.UI
{
    public enum StarNightUiRootKind
    {
        Hud = 0,
        Dialogue = 1
    }

    [DisallowMultipleComponent]
    public sealed class StarNightUiRoot2D : MonoBehaviour
    {
        [SerializeField] private StarNightUiRootKind kind =
            StarNightUiRootKind.Hud;

        public StarNightUiRootKind Kind => kind;

        public void Configure(StarNightUiRootKind rootKind)
        {
            kind = rootKind;
        }

        public static bool IsInsideUiRoot(Transform target)
        {
            for (Transform current = target;
                current != null;
                current = current.parent)
            {
                if (current.GetComponent<StarNightUiRoot2D>() != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

#endif
