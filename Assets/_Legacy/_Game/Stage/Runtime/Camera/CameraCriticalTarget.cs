#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Stage.CameraSystem
{
    public enum CameraCriticalTargetKind
    {
        Objective,
        HazardTelegraph,
        Npc,
        Exit,
    }

    [DisallowMultipleComponent]
    public sealed class CameraCriticalTarget : MonoBehaviour
    {
        [SerializeField] private CameraCriticalTargetKind kind;

        public CameraCriticalTargetKind Kind => kind;

        public void Configure(CameraCriticalTargetKind targetKind)
        {
            kind = targetKind;
        }

        public bool IsInside(Rect criticalFrame)
        {
            return criticalFrame.Contains(transform.position);
        }
    }
}

#endif
