using UnityEngine;

namespace StarNight.Character.Live.Movement
{
    /// <summary>
    /// RMAP03 fixture and runtime surfaces explicitly declare whether their
    /// physical collider may provide a corner-grab anchor.  This is a local
    /// classification only; it does not implement one-way traversal, damage,
    /// destruction, doors, or piston gameplay.
    /// </summary>
    public sealed class CharacterLiveGrabSurface : MonoBehaviour
    {
        public enum SurfaceKind
        {
            StaticSafe,
            DestructibleSafe,
            MovingSafe,
            OneWay,
            Hazard,
            Crushing,
            Decoration
        }

        [SerializeField] private SurfaceKind kind = SurfaceKind.StaticSafe;
        [SerializeField] private bool currentlyDangerous;

        public SurfaceKind Kind
        {
            get { return kind; }
        }

        public bool IsGrabAllowed
        {
            get
            {
                return !currentlyDangerous && (kind == SurfaceKind.StaticSafe ||
                    kind == SurfaceKind.DestructibleSafe || kind == SurfaceKind.MovingSafe);
            }
        }

        public bool IsMovingSafeSolid
        {
            get { return kind == SurfaceKind.MovingSafe && IsGrabAllowed; }
        }

        public void Configure(SurfaceKind surfaceKind, bool dangerous = false)
        {
            kind = surfaceKind;
            currentlyDangerous = dangerous;
        }

        public void SetDangerous(bool dangerous)
        {
            currentlyDangerous = dangerous;
        }
    }
}
